﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ResourceHealthChecker.SqlServer;
using SlugEnt.ResourceHealthChecker.RabbitMQ;
using SlugEnt.ResourceHealthChecker.SqlServer;

namespace SlugEnt.ResourceHealthChecker.SampleConsole;

/// <summary>
///     Application
/// </summary>
internal class App
{
    private readonly ILogger<App>     _logger;
    private readonly IServiceProvider _serviceProvider;
    private          IConfiguration   _configuration;


    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="services"></param>
    /// <param name="logger"></param>
    public App(IConfiguration configuration, IServiceProvider services, ILogger<App> logger)
    {
        _configuration   = configuration;
        _serviceProvider = services;
        _logger          = logger;
    }


    /// <summary>
    ///     App processing cycle
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting the exec cycle for the app");

        cancellationToken.Register(StopService);
        TimeSpan sleepTime = TimeSpan.FromSeconds(5);

        //Retrieve the HealthCheckProcessor
        HealthCheckProcessor? healthCheckProcessor = _serviceProvider.GetService<HealthCheckProcessor>();
        if (healthCheckProcessor == null)
        {
            throw new ApplicationException("HealthCheckProcessor Service could not be located.");
        }

        // We are now reading the file System Checks from the appsettings.json file.
        /*
        //  File System Checker
        ILogger<HealthCheckerFileSystem>? hcfs = _serviceProvider.GetService<ILogger<HealthCheckerFileSystem>>();
        if (hcfs == null)
        {
            throw new ApplicationException("Unable to locate service HealthCheckerFileSystem");
        }

        HealthCheckerConfigFileSystem config = new()
        {
            CheckIsWriteable = false,
            CheckIsReadable  = true,
            FolderPath       = @"C:\temp\HCR",
            IsEnabled        = false,
        };
        HealthCheckerFileSystem fileSystemA = new(hcfs, "Temp Folder Read - disabled", config);

        HealthCheckerConfigFileSystem config2 = new()
        {
            CheckIsWriteable = true,
            CheckIsReadable  = true,
            FolderPath       = @"C:\temp\HCW"
        };
        HealthCheckerFileSystem fileSystemB = new(hcfs, "Windows Folder ReadWrite", config2);
        healthCheckProcessor.AddCheckItem(fileSystemA);
        healthCheckProcessor.AddCheckItem(fileSystemB);
        */


        // SQL Server Checker
        // We have 2 SQL Checks, both are disabled.  One is read from the Appsettings.json and one is setup here in code.
        string connStr = _configuration.GetConnectionString("AdventureDB");
        if (String.IsNullOrEmpty(connStr))
        {
            throw new ApplicationException("Cannot find connection string for AdventureWorks Database");
        }

        HealthCheckerConfigSQLServer dbConfig = new(connStr, "Production.Location");
        dbConfig.ConnectionString = connStr;
        ILogger<HealthCheckerSQLServer>? hcsqlLogger = _serviceProvider.GetService<ILogger<HealthCheckerSQLServer>>();
        if (hcsqlLogger == null)
        {
            throw new ApplicationException("Unable to locate Logger for HealthCheckerSQLServer");
        }

        HealthCheckerSQLServer sqlServer = new(hcsqlLogger, "Adventure Works", dbConfig);

        // Because we are adding this thru code, we need to set the IsReady manually.
        sqlServer.IsReady = true;
        healthCheckProcessor.AddCheckItem(sqlServer);


        // RabbitMQ Checker
        /*
        string mqConnection = _configuration["MQ"];
        if (mqConnection == null)
        {
            throw new ApplicationException("Unable to locate MQ Configuration");
        }

        HealthCheckerConfigRabbitMQ mqConfig = new()
        {
            URL = mqConnection
        };
        ILogger<HealthCheckerRabbitMQ>? mqLogger = _serviceProvider.GetService<ILogger<HealthCheckerRabbitMQ>>();
        if (mqLogger == null)
        {
            throw new ApplicationException("Unable to locate the HealthCheckerRabbitMQ Logger service");
        }

        HealthCheckerRabbitMQ mqchecker = new(mqLogger, "Cloud AMQP Test", mqConfig);
        healthCheckProcessor.AddCheckItem(mqchecker);
        */

        // Ready to do first check!  We wait for it to finish so we can halt further application startup if it initially fails.
        await healthCheckProcessor.Start();

        // Exit if the Health Check has failed on start;
        EnumHealthStatus healthCheckStatus = healthCheckProcessor.Status;
        if (healthCheckStatus != EnumHealthStatus.Healthy)
        {
            _logger.LogCritical("Initial Health Startup Status is  [ {HealthCheckStatus} ].  Application is being shut down", healthCheckStatus.ToString());
            return;
        }


        while (!cancellationToken.IsCancellationRequested)
        {
            // This is the apps main code logic

            // Lets just check the overall status
            EnumHealthStatus healthStatus = healthCheckProcessor.Status;
            Console.WriteLine("The current status of all checks is: " + healthStatus);

            await Task.Delay(sleepTime, cancellationToken);
        }
    }


    private void BuildConfiguration()
    {
        IConfigurationBuilder? builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", true, true);
        _configuration = builder.Build();
    }


    private void StopService() { }
}