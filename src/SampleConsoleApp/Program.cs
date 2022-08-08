using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResourceHealthChecker;
using SlugEnt.ResourceHealthChecker.SampleConsole;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SlugEnt.ResourceHealthChecker;
using SlugEnt.ResourceHealthChecker.SqlServer;


namespace SlugEnt.ResourceHealthChecker.SampleConsole {


	public class Program {
		private static ILogger Logger;
		private static IConfiguration _configuration;


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
			Logger = Log.Logger;
			Log.Information("Starting {AppName}", Assembly.GetExecutingAssembly().GetName().Name);
			


			// Get Sensitive Appsettings.json file location
			string sensitiveAppSettings = Environment.GetEnvironmentVariable("AppSettingSensitiveFolder");



			// 1.B.  We keep the AppSettings file in the root App folder on the servers so it never gets overwritten
			string versionPath = Directory.GetCurrentDirectory();
			DirectoryInfo appRootDirectoryInfo = Directory.GetParent(versionPath);
			string appRoot = appRootDirectoryInfo.FullName;
			Console.WriteLine("Running from Directory:  " + appRoot);

			// Load Environment Specific App Setting file
			string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			string appSettingFileName = $"appsettings." + environmentName + ".json";
			string appSettingEnvFile = Path.Join(appRoot, appSettingFileName);
			DisplayAppSettingStatus(appSettingEnvFile);


			// Load the Sensitive AppSettings.JSON file.
			string sensitiveFileName = Assembly.GetExecutingAssembly().GetName().Name + "_AppSettingsSensitive.json";
			string sensitiveSettingFile = Path.Join(sensitiveAppSettings, sensitiveFileName);

			
			DisplayAppSettingStatus(sensitiveSettingFile);



			// Add our custom AppSettings.JSON files
			IConfigurationRoot configuration = new ConfigurationBuilder().AddJsonFile(appSettingEnvFile, true, true).AddJsonFile(sensitiveSettingFile, true, true).Build();


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


			// Run the Main App. Do NOT AWAIT call
#pragma warning disable CS4014
			host.RunAsync();
			App app = host.Services.GetRequiredService<App>();
			await app.ExecuteAsync();
			Log.CloseAndFlush();
#pragma warning restore
		}


		/// <summary>
		/// Logs whether a given AppSettings file was found to exist.
		/// </summary>
		/// <param name="appSettingFileName"></param>
		private static void DisplayAppSettingStatus(string appSettingFileName)
		{
			if (File.Exists(appSettingFileName))
				Logger.Information("AppSettings File was located.  {AppSettingsFile}", appSettingFileName);
			else
				Logger.Warning("AppSettings File was not found.  {AppSettingsFile}", appSettingFileName);
		}
	}
}