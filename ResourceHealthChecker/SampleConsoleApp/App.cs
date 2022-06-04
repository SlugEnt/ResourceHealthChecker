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
using SlugEnt.ResourceHealthChecker.SqlServer;

namespace SampleConsoleApp
{
	internal class App
	{
		IConfiguration _configuration = null;
		private IServiceProvider _serviceProvider;
		private readonly ILogger<App> _logger;


		public App (IConfiguration configuration, IServiceProvider services, ILogger<App> logger) {
			_configuration = configuration;
			_serviceProvider = services;
			_logger = logger;
		}


		public async Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
			Console.WriteLine("Starting the exec cycle for the app");

			cancellationToken.Register(StopService);
			TimeSpan sleepTime = TimeSpan.FromSeconds(5);

			//Retrieve the HealthCheckProcessor
			HealthCheckProcessor healthCheckProcessor = _serviceProvider.GetService<HealthCheckProcessor>();

			// Finish configuring it!
			healthCheckProcessor.CheckIntervalMS = 7000;


			//  File System Checker
			ILogger<HealthCheckerFileSystem> hcfs = _serviceProvider.GetService<ILogger<HealthCheckerFileSystem>>();
			HealthCheckerFileSystem fileSystemA = new HealthCheckerFileSystem(hcfs, "Temp Folder Read", @"C:\temp", true,false);
			HealthCheckerFileSystem fileSystemB = new HealthCheckerFileSystem(hcfs, "Windows Folder ReadWrite", @"C:\windows", true, false);
			healthCheckProcessor.AddCheckItem(fileSystemA);
			healthCheckProcessor.AddCheckItem(fileSystemB);


			// SQL Server Checker
			string connStr = "server=podmanb.slug.local;Database=AdventureWorks2019;User Id=AdvWorksUser;Password=Test;";
			HealthCheckerConfigSQLServer dbConfig = new HealthCheckerConfigSQLServer(connStr,"Person.Person");
			dbConfig.ConnectionString = connStr;
			ILogger<HealthCheckerSQLServer> hcsqlLogger = _serviceProvider.GetService<ILogger<HealthCheckerSQLServer>>();
			HealthCheckerSQLServer sqlServer = new HealthCheckerSQLServer(hcsqlLogger, "Adventure Works", dbConfig);
			healthCheckProcessor.AddCheckItem(sqlServer);


			// Ready to do first check!  We wait for it to finish so we can halt further application startup if it initially fails.
			await healthCheckProcessor.Start();

			// Exit if the Health Check has failed on start;
			EnumHealthStatus healthCheckStatus = healthCheckProcessor.Status;
			if ( healthCheckStatus != EnumHealthStatus.Healthy ) {
				_logger.LogCritical("Initial Health Startup Status is: " + healthCheckStatus.ToString() + "  Application is being shut down." );
				return;
			}


			while ( !cancellationToken.IsCancellationRequested ) {
				// This is the apps main code logic

				// Lets just check the overall status
				EnumHealthStatus healthStatus = healthCheckProcessor.Status;
				Console.WriteLine("The current status of all checks is: " + healthStatus.ToString());

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
