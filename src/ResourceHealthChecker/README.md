# ResourceHealthChecker
Enables an API or Service to perform its own internal health check of critical resources such as file systems, databases, etc during the entire lifetime of the application.

For API services, (If using SlugEnt.APIInfo) it provides the ability to provide a visual status page of the API or Service.  

It logs using Microsoft ILogger framework, so a log analysis tool can be used to alert or report on errors as well.


## Getting Started
Add the nuget package SlugEnt.ResourceHealthChecker 

Optionally, add additional Health Checker libraries to Check File Systems, Databases and other items.

### Insert into Code

```
	public static async Task Main (string [] args) {
#if DEBUG
	Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
	                                      .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
#else
		Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
	                                      .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)

#endif
										  .Enrich.FromLogContext()
										  .WriteTo.Console()
										  .CreateLogger();
		Log.Information("Starting SampleConsoleApp");

				
		using IHost host = Host.CreateDefaultBuilder(args)
		                     .UseSerilog()
		                     .ConfigureServices((_, services) =>

		                     // The main program     
		                     services.AddTransient<App>()

		                     // Add Health check Processor to available services
		                     .AddSingleton<HealthCheckProcessor>()
		                     .AddHostedService<HealthCheckerBackgroundProcessor>())
		                       .Build();
```

The two important lines are the 
```
	.AddSingleton<HealthCheckProcessor>()
	.AddHostedService<HealthCheckerBackgroundProcessor>())
```

Next in your main application loop you need to setup the Health Checker.
```
		public async Task ExecuteAsync(CancellationToken cancellationToken = default)
		{
			Console.WriteLine("Starting the exec cycle for the app");

			// Acquire the Cancellation token so we can can request the health checker to stop
			cancellationToken.Register(StopService);
			TimeSpan sleepTime = TimeSpan.FromSeconds(5);

			//Retrieve the HealthCheckProcessor
			HealthCheckProcessor healthCheckProcessor = _serviceProvider.GetService<HealthCheckProcessor>();

			// Finish configuring it!
			// This is where you put any Configuration changes the the Health Checker, such as setting the Check Interval
			healthCheckProcessor.CheckIntervalMS = 7000;


			// Now we can configure and add any specialized checkers.
			// Add - File System Checker.  We are only going to check that we can read from the directory.
			ILogger<HealthCheckerFileSystem> hcfs = _serviceProvider.GetService<ILogger<HealthCheckerFileSystem>>();
			HealthCheckerConfigFileSystem config = new HealthCheckerConfigFileSystem()
			{
				CheckIsWriteble = false,
				CheckIsReadable = true,
				FolderPath = @"C:\temp\HCR",
			};
			HealthCheckerFileSystem fileSystemA = new HealthCheckerFileSystem(hcfs, "Temp Folder Read",config );

			// Add another File System Checker, but this one we will check that we can read and write.
			HealthCheckerConfigFileSystem config2 = new HealthCheckerConfigFileSystem()
			{
				CheckIsWriteble = true,
				CheckIsReadable = true,
				FolderPath = @"C:\temp\HCW",
			};
			HealthCheckerFileSystem fileSystemB = new HealthCheckerFileSystem(hcfs, "Windows Folder ReadWrite", config2);

			// Add the actual checkers to the CheckProcessor loop
			healthCheckProcessor.AddCheckItem(fileSystemA);
			healthCheckProcessor.AddCheckItem(fileSystemB);


			// Add a SQL Server Checker.
			string connStr = "***REMOVED***";
			HealthCheckerConfigSQLServer dbConfig = new HealthCheckerConfigSQLServer(connStr);
			dbConfig.ConnectionString = connStr;
			ILogger<HealthCheckerSQLServer> hcsqlLogger = _serviceProvider.GetService<ILogger<HealthCheckerSQLServer>>();
			HealthCheckerSQLServer sqlServer = new HealthCheckerSQLServer(hcsqlLogger, "Adventure Works", dbConfig);
			healthCheckProcessor.AddCheckItem(sqlServer);


			// Ready to do first check!  We wait for it to finish so we can halt further application startup if it initially fails.
			// So the start check really is no different than other checks, except typically you might want to exit right away if the 
			// checks are failing..
			await healthCheckProcessor.Start();

			// Exit if the Health Check has failed on start;
			EnumHealthStatus healthCheckStatus = healthCheckProcessor.Status;
			if ( healthCheckStatus != EnumHealthStatus.Healthy ) {
				_logger.LogCritical("Initial Health Startup Status is: " + healthCheckStatus.ToString() + "  Application is being shut down." );
				return;
			}


			// This is just an example of an application loop.  In a real application you often times may not want to stop it, but let it keep going in the
			// hope that the error will self correct and then things are good again.  You also might want to react and not execute parts of the application	
			// Due to the failed health check, but leave it running so when the error is corrected it can continue on...
			while ( !cancellationToken.IsCancellationRequested ) {
				// This is the apps main code logic

				// Lets just check the overall status
				EnumHealthStatus healthStatus = healthCheckProcessor.Status;
				Console.WriteLine("The current status of all checks is: " + healthStatus.ToString());

				await Task.Delay(sleepTime, cancellationToken);
			}
		}
```
