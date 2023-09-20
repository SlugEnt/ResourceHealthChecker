using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlugEnt.ResourceHealthChecker.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlugEnt.ResourceHealthChecker
{
    /// <summary>
    /// The base functionality all HealthCheckers inherit from.
    /// </summary>
    public abstract class AbstractHealthChecker : IHealthChecker
    {
        protected        EnumHealthStatus        _status;
        private          DateTimeOffset          _lastStatusCheck;
        private          DateTimeOffset          _nextStatusCheck;
        private          bool                    _isRunning;
        private          bool                    _isReady;
        private readonly List<HealthEntryRecord> _healthRecords;
        protected        ILogger                 _logger;
        private          bool                    _isEnabled;


        /// <summary>
        /// Constructor used during Configuration from AppSettings.
        /// </summary>
        /// <param name="logger"></param>
        public AbstractHealthChecker(ILogger logger)
        {
            _logger          = logger;
            _status          = EnumHealthStatus.NotCheckedYet;
            _lastStatusCheck = DateTimeOffset.Now;
            _nextStatusCheck = DateTimeOffset.Now;
            _healthRecords   = new List<HealthEntryRecord>();
            _isRunning       = false;
            _isReady         = false;
            _isEnabled       = true;
        }


        /// <summary>
        /// Normal Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="healthCheckConfig"></param>
        /// <param name="logger"></param>
        public AbstractHealthChecker(string name, EnumHealthCheckerType type, HealthCheckConfigBase healthCheckConfig, ILogger logger) : this(logger)
        {
            Name              = name;
            Config            = healthCheckConfig;
            _isEnabled        = healthCheckConfig.IsEnabled;
            HealthCheckerType = type;
        }


        /// <summary>
        /// Name of this specific Health Checker.  Like DB name or Redis Name or something that identifies specifically what it is checking
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// The type of thing this Health Checker checks
        /// </summary>
        public EnumHealthCheckerType HealthCheckerType { get; set; }


        /// <summary>
        /// Configuration object for this HealthChecker
        /// </summary>
        public HealthCheckConfigBase Config { get; set; }


        /// <summary>
        /// If true, the Health Checker has passed initial setup and is ready to perform health Checks.  False, indicates some type of startup or config issues
        /// </summary>
        public bool IsReady
        {
            get { return _isReady; }
            set { _isReady = value; }
        }


        /// <summary>
        /// If true the Health Checks will be run.  If false, then no health checking is done.
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (!value)
                {
                    _isEnabled = false;
                    Status     = EnumHealthStatus.Disabled;
                    _logger.LogInformation("HealthChecker has been disabled:  [ {HealthChecker} ]", ShortTitle);
                }
                else
                {
                    _isEnabled = true;
                    Status     = EnumHealthStatus.Unknown;
                    _logger.LogInformation("HealthChecker has been enabled:  [ {HealthChecker} ]", ShortTitle);
                }
            }
        }


        /// <summary>
        /// Current Status of this health checker.  This takes into account all checks this health checker performs
        /// </summary>
        public EnumHealthStatus Status
        {
            get { return _status; }
            protected set { _status = value; }
        }


        /// <summary>
        /// Last time the status was checked, ie, the time the Status entry was last updated
        /// </summary>
        public DateTimeOffset LastStatusCheck
        {
            get { return _lastStatusCheck; }
        }


        /// <summary>
        /// When the next health check for this item should be checked.
        /// </summary>
        public DateTimeOffset NextStatusCheck
        {
            get { return _nextStatusCheck; }
            internal set { _nextStatusCheck = value; }
        }


        /// <summary>
        /// List of the last X health checks.  There is an upper limit to how many we keep.
        /// </summary>
        public List<HealthEntryRecord> HealthEntries
        {
            get { return _healthRecords; }
        }


        /// <summary>
        /// If true the Health Checker is still running and should not be run again.
        /// </summary>
        public bool IsRunning
        {
            get { return _isRunning; }
        }


        /// <summary>
        /// Name of the Class of Checker
        /// </summary>
        public string CheckerName { get; set; }


        /// <summary>
        /// The maximum allowed number of entries in the HealthEntries list
        /// </summary>
        public int MaxHealthEntries { get; set; } = 100;


        /// <summary>
        /// The maximum age of an entry in the HealthEntries List
        /// </summary>
        public int MaxHealthDays { get; set; } = 375;


        /// <summary>
        /// The full title of the Health Checker.  Usually type + name
        /// </summary>
        public abstract string FullTitle { get; }


        /// <summary>
        /// Executes the Health Check routine for the specific Health Checker (Query database, Write File, etc)  Should return true if everything worked
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        protected abstract Task<(EnumHealthStatus, string)> PerformHealthCheck(CancellationToken stoppingToken);


        /// <summary>
        /// Runs the health check if necessary
        /// </summary>
        /// <param name="force"></param>
        public async Task CheckHealth(CancellationToken token, bool force = false)
        {
            bool needToCheck = force;


            if (IsRunning)
                return;

            if (!IsEnabled)
                return;


            // See if we are supposed to run a health check
            if (DateTimeOffset.Now > NextStatusCheck)
                needToCheck = true;

            if (!needToCheck)
                return;

            _isRunning = true;

            EnumHealthStatus newStatus;
            string           message;

            // If this is first check, set to Unknown so we know we are checking it.
            if (Status == EnumHealthStatus.NotCheckedYet)
                Status = EnumHealthStatus.Unknown;


            // Ensure the checker is ready.
            if (IsReady)
                (newStatus, message) = await PerformHealthCheck(token);
            else
            {
                newStatus = EnumHealthStatus.NotReady;
                message   = "Health Checker never completed initial configuration or setup successfully.  Health Check cannot be run.";
            }

            if (newStatus != _status)
            {
                HealthEntryRecord healthEntryRecord = new(newStatus, message);
                _healthRecords.Add(healthEntryRecord);
                _status = newStatus;

                // Lets log it.
                if (newStatus == EnumHealthStatus.Healthy)
                {
                    _logger.LogWarning("Health Check: {HealthChecker} has changed status to [{HealthStatus}]", FullTitle, newStatus);
                }
                else if (newStatus == EnumHealthStatus.Failed)
                    _logger.LogError("Health Check: {HealthChecker} has changed status to [{HealthStatus}] --> {HealthDetails}", FullTitle, newStatus, message);
                else if (newStatus == EnumHealthStatus.Degraded)
                    _logger.LogWarning("Health Check: {HealthChecker} has changed status to [{HealthStatus}]", FullTitle, newStatus);
                else if (newStatus == EnumHealthStatus.Unknown)
                    _logger.LogWarning("Health Check: {HealthChecker} has changed status to [{HealthStatus}]", FullTitle, newStatus);
                else
                    _logger.LogWarning("Health Check: {HealthChecker} has changed status to [{HealthStatus}]", FullTitle, newStatus);
            }
            else
            {
                _healthRecords[^1].Increment();
            }


            // Set next check interval
            _nextStatusCheck = DateTimeOffset.Now.AddSeconds(Config.CheckInterval);


            // See if Age or Capacity limits have been reached on the health records list and remove any that meet criteria.
            if (_healthRecords.Count > MaxHealthEntries)
            {
                while (_healthRecords.Count > MaxHealthEntries)
                {
                    _healthRecords.RemoveAt(0);
                }

                DateTimeOffset agingDate     = DateTimeOffset.Now.AddDays(-1 * MaxHealthDays);
                int            lastIndex     = -1;
                bool           keepSearching = true;
                int            index         = 0;
                while (keepSearching)
                {
                    if (_healthRecords[index].LastDateTimeOffset < agingDate)
                        lastIndex = index;
                    break;
                }

                if (lastIndex != -1)
                    _healthRecords.RemoveRange(0, lastIndex);
            }

            _lastStatusCheck = DateTimeOffset.Now;
            _isRunning       = false;
        }


        /// <summary>
        /// This method should provide the HTML text that displays the results of this Health Check
        /// </summary>
        /// <returns></returns>
        public abstract void DisplayHTML(StringBuilder sb);


        /// <summary>
        /// Reads the common properties of Config items for the ConfigurationHealthChecks configuration objects
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="configurationSectionRoot"></param>
        public virtual void SetupFromConfig(IConfiguration configuration, string configurationSectionRoot)
        {
            this.Config.CheckInterval = configuration.GetSection(configurationSectionRoot + ":Config:CheckInterval").Get<int>();
            this.Config.IsEnabled     = configuration.GetSection(configurationSectionRoot + ":Config:IsEnabled").Get<bool>();
        }


        /// <summary>
        /// Displays a Short Title for this Checker
        /// </summary>
        /// <returns></returns>
        public string ShortTitle
        {
            get { return CheckerName + " [" + Name + "]"; }
        }
    }
}