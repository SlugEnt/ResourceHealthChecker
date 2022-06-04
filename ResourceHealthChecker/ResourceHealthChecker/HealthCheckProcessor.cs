using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace SlugEnt.ResourceHealthChecker
{
	/// <summary>
	/// Manages all Health Checks 
	/// </summary>
	public class HealthCheckProcessor
	{
		private List<IHealthChecker>          _healthCheckerList;
		private ILogger<HealthCheckProcessor> _logger;
		private int _checkIntervalMS = 5000;
		private Action<int> _actionCheckInterval;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="checkIntervalInSeconds"></param>
		public HealthCheckProcessor(ILogger<HealthCheckProcessor> logger)
		{
			_healthCheckerList = new List<IHealthChecker>();
			_logger = logger;
		}


		/// <summary>
		/// How often the check processor runs in milliseconds.  Note the actual timer loop cycle is in HealthCheckerBackgroundProcessor. It reads this value so it cab be changed dynamically by the owner app.
		/// </summary>
		public int CheckIntervalMS {
			get { return _checkIntervalMS;}
			set {
				_checkIntervalMS = value;
				if (_actionCheckInterval != null)
					_actionCheckInterval(_checkIntervalMS);

			} }


		public Action<int> SetCheckIntervalAction {
			set { _actionCheckInterval = value; }
		}


		/// <summary>
		/// Adds a Health Check item
		/// </summary>
		/// <param name="healthChecker"></param>
		public void AddCheckItem (IHealthChecker healthChecker) {
			_healthCheckerList.Add(healthChecker);
			_logger.LogInformation("HealthChecker added: " + healthChecker.CheckerName  + " [ " + healthChecker.Name + " ]");
		}
		

		/// <summary>
		/// Performs all Health Checks 
		/// </summary>
		/// <returns></returns>
		public async Task CheckHealth () {
			_logger.LogDebug("Starting HealthCheckProcessor cycle");
			foreach ( var healthChecker in _healthCheckerList ) {
				// We do not await the call, we want to kick it off and let it do its thing.
				healthChecker.CheckHealth();
			}
		}



		/// <summary>
		/// Returns the overall status of all the Health Checks.  The status returned will be the the most severe of all of the Health Checks.  So one service degraded will result in overall status of degraded.
		/// </summary>
		public EnumHealthStatus Status {
			get {
				EnumHealthStatus status = EnumHealthStatus.Healthy;
				foreach ( IHealthChecker healthChecker in _healthCheckerList ) {
					if ( healthChecker.Status > status ) status = healthChecker.Status;
				}

				return status;
			}
		}


		/// <summary>
		/// Starts the checking process.
		/// Still True?????   Note this only sets the IsStarted boolean to true, the BackgroundProcessor once it sees IsStarted = true then starts the actual process.
		/// </summary>
		public async Task Start () {
			await CheckHealth();

			// So, the checks might be ongoing still.  We continue checking the Status until it's Healthy OR InitialStartup Time is exceeded
			int sleepTime = 100;
			int maxWaitTime = 30000;
			DateTime maxWait = DateTime.Now.AddMilliseconds(maxWaitTime);
			while ( true ) {
				Thread.Sleep(sleepTime);
				if ( Status == EnumHealthStatus.Healthy ) {
					IsStarted = true;
					break;
				}
				if ( DateTime.Now > maxWait ) break;
			}
			
		}


		/// <summary>
		/// True if the CheckProcessor should be started or is ready to start.
		/// </summary>
		public bool IsStarted { get; private set; }


		/// <summary>
		/// Displays the health of all Health Checkers
		/// </summary>
		/// <returns></returns>
		public StringBuilder DisplayFull () {
			StringBuilder sb = new(2048);
			sb.Append("<html>");
			foreach ( IHealthChecker healthChecker in _healthCheckerList ) {
				string color = "grey";
				if ( healthChecker.Status == EnumHealthStatus.Healthy ) color = "green";
				else if ( healthChecker.Status == EnumHealthStatus.Degraded ) color = "orange";
				else if ( healthChecker.Status == EnumHealthStatus.Failed ) color = "red";
				sb.Append("<hr style=\"width: 50 %; text - align:left; margin - left:0\">");
				sb.Append("<H2 style=\"color:" + color + ";\">" + healthChecker.CheckerName + ":   " + healthChecker.Name + "    [" + healthChecker.Status.ToString() + "]</H2>");
				healthChecker.DisplayHTML(sb);
			}

			sb.Append("</html>");
			return sb;

		}
	}
}
