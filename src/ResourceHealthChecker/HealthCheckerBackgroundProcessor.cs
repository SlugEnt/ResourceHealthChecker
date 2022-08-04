using System;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlugEnt.ResourceHealthChecker;


namespace ResourceHealthChecker
{
	/// <summary>
	/// Background Health Check Processor, that controls the checking of health checks.
	/// </summary>
	public class HealthCheckerBackgroundProcessor : BackgroundService, IHealthCheckerBackgroundProcessor
	{
		private readonly ILogger<HealthCheckerBackgroundProcessor> _logger;
		private readonly IServiceProvider _serviceProvider;
		private readonly HealthCheckProcessor _healthCheckProcessor;
		private EnumHealthStatus _lastHealthStatus = EnumHealthStatus.Unknown;
		private int _lastStatusCount = 0;
		private int _sleepTime = 5000;
		private readonly Action<int> _setSleepAction;

		/// <summary>
		/// Constructs the API Background Processor
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="serviceProvider"></param>
		public HealthCheckerBackgroundProcessor(ILogger<HealthCheckerBackgroundProcessor> logger, IServiceProvider serviceProvider, IConfiguration configuration)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
			_healthCheckProcessor = _serviceProvider.GetService<HealthCheckProcessor>();

			// Set Sleep Time Action.  HealthCheckProcessor will call this anytime the sleep time changes.
			_setSleepAction = SetSleepTime;
			_healthCheckProcessor.SetCheckIntervalAction = _setSleepAction;

			// Initially set the sleep time from the HealthCheckProcessor in case we were delayed getting started and missed the change before we had callback setup
			_sleepTime = _healthCheckProcessor.CheckIntervalMS;
		}



		/// <summary>
		/// Main Processing loop for background health checks
		/// </summary>
		/// <param name="stoppingToken"></param>
		/// <returns></returns>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			stoppingToken.Register(StopService);

			// Now that we have the Cancellation token - give it to HealthCheckProcessor
			_healthCheckProcessor.CancellationToken = stoppingToken;

			// Wait for HealthCheckProcessor Startup to occur
			int loopCtr = 0;
			while ( !stoppingToken.IsCancellationRequested ) {
				loopCtr++;
				if ( _healthCheckProcessor.ProcessingStage != EnumHealthCheckProcessorStage.Processing ) {
					if (loopCtr > 6 ) 
						_logger.LogWarning("Still Waiting for HealthCheckProcessor to enter the post startup phase.");
					await Task.Delay(5000, stoppingToken);
				}
				if ( _healthCheckProcessor.ProcessingStage > EnumHealthCheckProcessorStage.Processing ) return;
			}


			// Begin processing
			while (!stoppingToken.IsCancellationRequested)
			{
				try {
#pragma warning disable CS4014
					_healthCheckProcessor.CheckHealth();
					_lastStatusCount++;
#pragma warning restore CS4014
					// We sleep for a second to allow health checks to run then check the status.  Note, some may still be running, especially if they are erroring, there status will be updated next run.
					await Task.Delay(1000, stoppingToken);

					EnumHealthStatus currentStatus = _healthCheckProcessor.Status;

					if ( currentStatus != _lastHealthStatus ) {
						string msg = "  It was previously in a " + _lastHealthStatus.ToString() + " state for " + _lastStatusCount + " Health Cycle Checks.";
						if ( currentStatus == EnumHealthStatus.Healthy )
							_logger.LogWarning("Health Status has returned to a {HealthStatus} State.  Previously was {PriorHealthStatus} for {CycleCount} health check cycles", currentStatus.ToString(), _lastHealthStatus.ToString(),_lastStatusCount);
						else if ( currentStatus == EnumHealthStatus.Failed )
							_logger.LogCritical("Health Status has changed to a {HealthStatus} State.  Previously was {PriorHealthStatus} for {CycleCount} health check cycles", currentStatus.ToString(), _lastHealthStatus.ToString(), _lastStatusCount);
						else if ( currentStatus == EnumHealthStatus.Degraded )
							_logger.LogError("Health Status has changed to a {HealthStatus} State.  Previously was {PriorHealthStatus} for {CycleCount} health check cycles", currentStatus.ToString(), _lastHealthStatus.ToString(), _lastStatusCount);
						else if ( currentStatus == EnumHealthStatus.Unknown )
							_logger.LogError(
								"Health Status is {HealthStatus}.  This should be short term upon initial application start.  If it does not change shortly, then something is wrong.", currentStatus.ToString());

						_lastHealthStatus = currentStatus;
						_lastStatusCount = 1;
					}

					// Sleep for cycle time.
					await Task.Delay(_sleepTime, stoppingToken);
				}
				catch ( Exception ex ) {
					if (stoppingToken.IsCancellationRequested) 
						_logger.LogDebug("Health Checker Background Processor has been requested to shut down.");
					else
						_logger.LogError(ex,"Unhandled error in the HealthCheckerBackgroundProcessor loop.  Error was: {ErrorMsg}" , ex.Message);
				}
			}

		}


		private void StopService()
		{

		}

		

		/// <summary>
		/// Sets the sleep time amount in ms.
		/// </summary>
		/// <param name="sleepMS"></param>
		public void SetSleepTime (int sleepMS) {
			_sleepTime = sleepMS;
		}
	}
}
