using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResourceHealthChecker;
using SlugEnt.ResourceHealthChecker.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlugEnt.ResourceHealthChecker;

/// <summary>
/// Manages all Health Checks for an application 
/// </summary>
public class HealthCheckProcessor
{
    private readonly List<IHealthChecker>          _healthCheckerList;
    private readonly ILogger<HealthCheckProcessor> _logger;
    private          int                           _checkIntervalMS = 5000;
    private          Action<int>                   _actionCheckInterval;
    private          bool                          _isStartingUp;
    private          IConfiguration                _configuration;

    public HealthCheckProcessor() { }


    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="checkIntervalInSeconds"></param>
    public HealthCheckProcessor(ILogger<HealthCheckProcessor> logger, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _healthCheckerList = new List<IHealthChecker>();
        _logger            = logger;
        _configuration     = configuration;
        ProcessingStage    = EnumHealthCheckProcessorStage.Constructed;

        // Process the configuration to add any Health Checks configured from Configuration
        try
        {
            ConfigurationResourceHealthChecker configurationResourceHealthChecker = new();
            configurationResourceHealthChecker = configuration.GetSection("ResourceHealthChecker").Get<ConfigurationResourceHealthChecker>();


            // Loop thru all the Health Checks defined in the Configuration
            if (configurationResourceHealthChecker != null)
            {
                for (int i = 0; i < configurationResourceHealthChecker.ConfigHealthChecks.Count; i++)
                {
                    IHealthChecker healthChecker = null;

                    string                    configRoot = "ResourceHealthChecker:ConfigHealthChecks:" + i.ToString();
                    ConfigurationHealthChecks hc         = configurationResourceHealthChecker.ConfigHealthChecks[i];

                    string typeLower = hc.Type.ToLower();
                    if (typeLower == "filesystem")
                    {
                        healthChecker = (IHealthChecker)serviceProvider.GetService<IFileSystemHealthChecker>();
                        if (healthChecker == null)
                            logger.LogError("Unable to find a Services Instance for a FileSystemHealthChecker.  Ensure it has been added to the Services scope");
                    }
                    else if (typeLower == "sql")
                    {
                        healthChecker = (IHealthChecker)serviceProvider.GetService<ISQLServerHealthChecker>();
                        if (healthChecker == null)
                            logger.LogError("Unable to find a Services Instance for a SQLServerHealthChecker.  Ensure it has been added to the Services scope");
                    }

                    if (healthChecker == null)
                        throw new ApplicationException("A required HealthChecker was not able to be loaded from the Services Scope.");

                    // Set common properties of all health checkers from the config.
                    healthChecker.Name      = hc.Name;      // configuration.GetSection(configRoot + ":Name").Get<string>();
                    healthChecker.IsEnabled = hc.IsEnabled; //configuration.GetSection(configRoot + ":IsEnabled").Get<bool>();


                    // Finish setup by calling Individual HealthChecker Config sections
                    healthChecker.SetupFromConfig(configuration, configRoot);

                    // set to Ready.
                    healthChecker.IsReady = true;

                    AddCheckItem(healthChecker);
                }

                // Get the HealthChecker Interval 
                this.CheckIntervalMS = configurationResourceHealthChecker.CheckIntervalMS;
            }

            // There are no health checks so set to ready.
            else
            {
                // Not sure what we should set to - there is nothing to check.
                ProcessingStage = EnumHealthCheckProcessorStage.Started;
            }
        }
        catch (Exception ex)
        {
            ProcessingStage = EnumHealthCheckProcessorStage.FailedToStart;
            _logger.LogError("{Class} has encountered a configuration error while trying to add HealthChecks via Configuration.", ex);
            throw;
        }
    }


    /// <summary>
    /// Returns a list of all the health checkers.
    /// </summary>
    public List<IHealthChecker> HealthCheckers
    {
        get { return _healthCheckerList; }
    }


    /// <summary>
    /// Tells at what point in the life cycle of the Health Processor it is currently at.
    /// </summary>
    public EnumHealthCheckProcessorStage ProcessingStage { get; private set; }


    /// <summary>
    /// How often the check processor runs in milliseconds.  Note the actual timer loop cycle is in HealthCheckerBackgroundProcessor. It reads this value so it can be changed dynamically by the owner app.
    /// </summary>
    public int CheckIntervalMS
    {
        get { return _checkIntervalMS; }
        set
        {
            _checkIntervalMS = value;
            if (_actionCheckInterval != null)
                _actionCheckInterval(_checkIntervalMS);
        }
    }


    /// <summary>
    /// Allows defining of the 
    /// </summary>
    public Action<int> SetCheckIntervalAction
    {
        set { _actionCheckInterval = value; }
    }


    /// <summary>
    /// Adds a Health Check item
    /// </summary>
    /// <param name="healthChecker"></param>
    public void AddCheckItem(IHealthChecker healthChecker)
    {
        _healthCheckerList.Add(healthChecker);
        if (healthChecker.IsEnabled)
            _logger.LogInformation("HealthChecker Added:  [ {HealthChecker} ]", healthChecker.ShortTitle);
        else
        {
            _logger.LogWarning("Disabled HealthChecker Added:  [ {HealthChecker} ]", healthChecker.ShortTitle);
        }
    }


    /// <summary>
    /// Performs all Health Checks 
    /// </summary>
    /// <returns></returns>
#pragma warning disable CS1998
#pragma warning disable CS4014
    public async Task CheckHealth()
    {
        // Make sure we are in a valid state
        if (ProcessingStage == EnumHealthCheckProcessorStage.Constructed | ProcessingStage == EnumHealthCheckProcessorStage.Finished)
        {
            _logger.LogDebug("Check Health did not check anything due to HealthCheckProcessor not being in a valid checking stage");
            return;
        }

        _logger.LogDebug("Starting HealthCheckProcessor cycle");
        foreach (var healthChecker in _healthCheckerList)
        {
            // We do not await the call, we want to kick it off and let it do its thing.
            healthChecker.CheckHealth(CancellationToken);
        }
    }
#pragma warning restore CS1998
#pragma warning restore CS4014



    /// <summary>
    /// Returns the overall status of all the Health Checks.  The status returned will be the the most severe of all of the Health Checks.  So one service degraded will result in overall status of degraded.
    /// </summary>
    public EnumHealthStatus Status
    {
        get
        {
            //if ( !IsStarted && _isStartingUp) return EnumHealthStatus.Unknown;

            EnumHealthStatus status = EnumHealthStatus.NotCheckedYet;
            foreach (IHealthChecker healthChecker in _healthCheckerList)
            {
                if (healthChecker.Status > status)
                    status = healthChecker.Status;
            }

            return status;
        }
    }


    /// <summary>
    /// Starts the checking process.
    /// Still True?????   Note this only sets the IsStarted boolean to true, the BackgroundProcessor once it sees IsStarted = true then starts the actual process.
    /// </summary>
    public async Task Start()
    {
        _logger.LogDebug("HealthCheckProcessor Start Method has been entered");
        _isStartingUp   = true;
        ProcessingStage = EnumHealthCheckProcessorStage.Initializing;
        await CheckHealth();

        // So, the checks might be ongoing still.  We continue checking the Status until it's Healthy OR InitialStartup Time is exceeded
        int      sleepTime   = 3000;
        int      maxWaitTime = 30000;
        DateTime maxWait     = DateTime.Now.AddMilliseconds(maxWaitTime);
        while (true)
        {
            Thread.Sleep(sleepTime);
            if (Status == EnumHealthStatus.Healthy)
            {
                ProcessingStage = EnumHealthCheckProcessorStage.Processing;
                IsStarted       = true;
                break;
            }

            if (DateTime.Now > maxWait)
            {
                ProcessingStage = EnumHealthCheckProcessorStage.FailedToStart;
                break;
            }

            await CheckHealth();
        }

        _logger.LogDebug("HealthCheckProcessor Startup is exiting with a status of {HealthCheckProcessorStatus}", ProcessingStage.ToString());
    }


    /// <summary>
    /// True if the CheckProcessor should be started or is ready to start.
    /// </summary>
    public bool IsStarted { get; private set; }


    /// <summary>
    /// Displays the health of all Health Checkers
    /// </summary>
    /// <returns></returns>
    public StringBuilder DisplayFull()
    {
        StringBuilder sb = new(2048);
        sb.Append("<html>");
        foreach (IHealthChecker healthChecker in _healthCheckerList)
        {
            string color = GetStatusColor(healthChecker.Status);
            sb.Append("<hr style=\"width: 50 %; text - align:left; margin - left:0\">");
            sb.Append("<H2 style=\"color:" +
                      color +
                      ";\">" +
                      healthChecker.CheckerName +
                      ":   " +
                      healthChecker.Name +
                      "    [" +
                      healthChecker.Status.ToString() +
                      "]</H2>");
            healthChecker.DisplayHTML(sb);
        }

        sb.Append("</html>");
        return sb;
    }


    /// <summary>
    /// Returns the color that is associated with the HealthStatus passed in
    /// </summary>
    /// <param name="healthStatus"></param>
    /// <returns></returns>
    public static string GetStatusColor(EnumHealthStatus healthStatus)
    {
        string color = "grey";
        if (healthStatus == EnumHealthStatus.Healthy)
            color = "green";
        else if (healthStatus == EnumHealthStatus.Degraded)
            color = "orange";
        else if (healthStatus == EnumHealthStatus.Failed)
            color = "red";
        return color;
    }



    /// <summary>
    /// Sets the cancellation Token to be used during asynchronous calls
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}