using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlugEnt.ResourceHealthChecker
{
	public interface IHealthChecker {
		/// <summary>
		/// Name of this health checker
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The type of Health Check this is
		/// </summary>
		public EnumHealthCheckerType HealthCheckerType { get; set; }

		/// <summary>
		/// The configuration for this Health Checker
		/// </summary>
		public IHealthCheckConfig Config { get; set; }

		public EnumHealthStatus Status { get; }

		public DateTimeOffset LastStatusCheck { get; }

		public DateTimeOffset NextStatusCheck { get; }

		public List<HealthEntryRecord> HealthEntries { get;  }

		/// <summary>
		/// Returns true if the Health Checker is currently running
		/// </summary>
		public bool IsRunning { get; }


		/// <summary>
		/// Name of the Class of Checker
		/// </summary>
		public string CheckerName { get; set; }


		/// <summary>
		/// Performs the health check specific to the given HealthChecker.  
		/// </summary>
		/// <returns></returns>
		public Task CheckHealth (CancellationToken token,bool force = false);

		public void DisplayHTML (StringBuilder sb);
	}
}
