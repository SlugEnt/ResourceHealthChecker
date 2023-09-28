# ResourceHealthChecker
Enables an API or Service to perform its own internal health check of critical resources such as file systems, databases, RabbitMQ Servers and more, during the entire lifetime of the application.

For API services, (If using SlugEnt.APIInfo) it provides the ability to provide a visual status page of the API or Service.  

It logs using Microsoft ILogger framework, so a log analysis tool can be used to alert or report on errors as well.


## Getting Started
Add the nuget package SlugEnt.ResourceHealthChecker 
The package is most useful when used with the https://github.com/SlugEnt/APIInfo to gather and report on API issues

### Supported HealthChecker Plugins
Optionally, add additional Health Checker libraries to Check File Systems, Databases and other items.
- SlugEnt.ResourceHealthChecker.FileSystem
- SlugEnt.ResourceHealthChecker.SQLServer
- SlugEnt.ResourceHealthChecker.RabbitMQ - Not fully working at the moment



### Insert into Code

```
// Add Health check Processor into Service Registry of available services
.AddSingleton<HealthCheckProcessor>()
.AddHostedService<HealthCheckerBackgroundProcessor>())
```

### Setup HealthChecker
There are 2 ways to setup the Health Checker.  The first is you can manually create the objects and entries.  The second is you can configure them in the AppSettings.Json file and the HealthCheckers will automatically be created.

#### (Option 1) Manually Configuring the Health Checkers in Code

Next in your main application loop you need to setup the Health Checker.

```
public async Task ExecuteAsync(CancellationToken cancellationToken = default)
{
	Console.WriteLine("Starting the exec cycle for the app");

	// Acquire the Cancellation token so we can can request the health checker to stop if necessary
	cancellationToken.Register(StopService);
	TimeSpan sleepTime = TimeSpan.FromSeconds(5);

	//Retrieve the HealthCheckProcessor
	HealthCheckProcessor healthCheckProcessor = _serviceProvider.GetService<HealthCheckProcessor>();

	// Finish configuring it!
	// This is where you put any Configuration changes the the Health Checker, such as setting the Check Interval.
	// This says to run the HealthCheckProcessor every 7 seconds.  You will want to keep this fairly short.  Each
	// HealthChecker has its own internal interval where the check is Actually performed.  So, we might be checking to 
	// see if any health checks need to be checked every 7 seconds, but an individual HealthChecker may say it only needs
	// to be checked every 60 seconds.  
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
	string connStr = "server=podmanb.slug.local;Database=AdventureWorks2019;User Id=AdvWorksUser;Password=Test;";
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
		_logger.LogCritical("Initial Health Startup Status is  [ {HealthCheckStatus} ].  Application is being shut down", healthCheckStatus.ToString());
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

#### (option 2) Configuring in AppSettings.Json
This is the preferred way.  Check out the AppSettings.JSON in the sample project.  But here is a small slice of what a Configuration can look like:
```
{
  "ResourceHealthChecker": {
    "CheckIntervalMS":  5000,
    "ConfigHealthChecks": [
      {
        "Type": "FileSystem",
        "Name": "Temp Read Folder",
		"IsEnabled": true,
        "Config": {
          "CheckInterval": 55,
          "FolderPath": "C:\\temp",
          "CheckIsReadable": true,
          "CheckIsWriteable": false,
          "ReadFileName": ""
		  }
      },
      {
        "Type": "FileSystem",
        "Name": "Temp Write Folder",
        "Config": {
          "FolderPath": "C:\\temp",
          "CheckIsReadable": false,
          "CheckIsWriteable": true,
          "ReadFileName": ""
        }
      },
      {
        "Type": "SQL",
        "Name": "Sample DB",
		"IsEnabled": false,
        "Config": {
          "CheckReadTable": true,
          "CheckWriteTable": true,
          "ConnectionString": "",
          "ReadTable": "",
          "WriteTable": ""
        }
      }
    ]
  }
}
```

The only other thing you need to do in code is make sure you add these classes to the ServiceProvider configuration so it can create the objects.
```
using IHost host = Host.CreateDefaultBuilder(args)
	// Add our custom config from above to the default configuration
	.ConfigureAppConfiguration(config => {
		config.AddConfiguration(configuration);
	})
	.UseSerilog()
	.ConfigureServices((_, services) =>

		// The main program     
		services.AddTransient<App>()

			// Add Health check Processor to available services
			.AddSingleton<HealthCheckProcessor>()
			.AddHostedService<HealthCheckerBackgroundProcessor>()

			// Eventually do something else like single call to add all Healthcheckers
			.AddTransient<IFileSystemHealthChecker,HealthCheckerFileSystem>()
			.AddTransient<ISQLServerHealthChecker, HealthCheckerSQLServer>()
			.AddTransient<IFileSystemHealthChecker, HealthCheckerFileSystem>()
	)
.Build();
```

