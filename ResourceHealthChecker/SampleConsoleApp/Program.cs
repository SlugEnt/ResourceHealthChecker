using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResourceHealthChecker;
using SampleConsoleApp;
using Serilog;
using Serilog.Events;
using SlugEnt.ResourceHealthChecker;


public class Program {

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


		// Run the Main App.
		host.Run();
		App app = host.Services.GetRequiredService<App>();
		await app.ExecuteAsync();
		Log.CloseAndFlush();
	}
}