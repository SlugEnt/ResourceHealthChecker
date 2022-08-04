using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlugEnt.ResourceHealthChecker;
using Microsoft.Extensions.DependencyInjection;
using ResourceHealthChecker.SqlServer;
using SlugEnt.ResourceHealthChecker.RabbitMQ;
using SlugEnt.ResourceHealthChecker.SqlServer;

namespace SlugEnt.ResourceHealthChecker.SampleConsole
{
	internal class App
	{
		IConfiguration _configuration;
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<App> _logger;


		public App (IConfiguration configuration, IServiceProvider services, ILogger<App> logger) {
			_configuration = configuration;
			_serviceProvider = services;
			_logger = logger;
		}


		public async Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
			System.Console.WriteLine("Starting the exec cycle for the app");

			cancellationToken.Register(StopService);
			TimeSpan sleepTime = TimeSpan.FromSeconds(5);

			//Retrieve the HealthCheckProcessor
			HealthCheckProcessor? healthCheckProcessor = _serviceProvider.GetService<HealthCheckProcessor>();
			if ( healthCheckProcessor == null ) throw new ApplicationException("HealthCheckProcessor Service could not be located.");

			// Finish configuring it!
			healthCheckProcessor.CheckIntervalMS = 7000;



			//  File System Checker
			ILogger<HealthCheckerFileSystem>? hcfs = _serviceProvider.GetService<ILogger<HealthCheckerFileSystem>>();
			if ( hcfs == null ) throw new ApplicationException("Unable to locate service HealthCheckerFileSystem");
			HealthCheckerConfigFileSystem config = new ()
			{
				CheckIsWriteble = false,
				CheckIsReadable = true,
				FolderPath = @"C:\temp\HCR",
			};
			HealthCheckerFileSystem fileSystemA = new (hcfs, "Temp Folder Read",config );
			 
			HealthCheckerConfigFileSystem config2 = new ()
			{
				CheckIsWriteble = true,
				CheckIsReadable = true,
				FolderPath = @"C:\temp\HCW",
			};
			HealthCheckerFileSystem fileSystemB = new (hcfs, "Windows Folder ReadWrite", config2);
			healthCheckProcessor.AddCheckItem(fileSystemA);
			healthCheckProcessor.AddCheckItem(fileSystemB);



			// SQL Server Checker
			string connStr = _configuration ["ConnectionStrings:AdventureDB"];
			if ( connStr == null ) throw new ApplicationException("Cannot find connection string for AdventureWorks Database");

			HealthCheckerConfigSQLServer dbConfig = new (connStr,"Production.Location");
			dbConfig.ConnectionString = connStr;
			ILogger<HealthCheckerSQLServer>? hcsqlLogger = _serviceProvider.GetService<ILogger<HealthCheckerSQLServer>>();
			if ( hcsqlLogger == null ) throw new ApplicationException("Unable to locate Logger for HealthCheckerSQLServer");
			HealthCheckerSQLServer sqlServer = new (hcsqlLogger, "Adventure Works", dbConfig);
			healthCheckProcessor.AddCheckItem(sqlServer);



			// RabbitMQ Checker
			string mqConnection = _configuration ["MQ"];
			if ( mqConnection == null ) throw new ApplicationException("Unable to locate MQ Configuration");
			HealthCheckerConfigRabbitMQ mqConfig = new ()
			{
				URL = mqConnection,
			};
			ILogger <HealthCheckerRabbitMQ>? mqLogger = _serviceProvider.GetService<ILogger<HealthCheckerRabbitMQ>>();
			if ( mqLogger == null ) throw new ApplicationException("Unable to locate the HealthCheckerRabbitMQ Logger service");
			HealthCheckerRabbitMQ mqchecker = new (mqLogger, "Cloud AMQP Test", mqConfig);
			healthCheckProcessor.AddCheckItem(mqchecker);


			// Ready to do first check!  We wait for it to finish so we can halt further application startup if it initially fails.
			await healthCheckProcessor.Start();

			// Exit if the Health Check has failed on start;
			EnumHealthStatus healthCheckStatus = healthCheckProcessor.Status;
			if ( healthCheckStatus != EnumHealthStatus.Healthy ) {
				_logger.LogCritical("Initial Health Startup Status is  [ {HealthCheckStatus} ].  Application is being shut down", healthCheckStatus.ToString());
				return;
			}


			while ( !cancellationToken.IsCancellationRequested ) {
				// This is the apps main code logic

				// Lets just check the overall status
				EnumHealthStatus healthStatus = healthCheckProcessor.Status;
				System.Console.WriteLine("The current status of all checks is: " + healthStatus.ToString());

				await Task.Delay(sleepTime, cancellationToken);
			}
		}


		private void StopService () {

		}


		void BuildConfiguration()
		{
			var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
			                                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
			_configuration = builder.Build();
		}
		
	}
}
