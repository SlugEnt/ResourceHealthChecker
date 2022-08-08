
using Microsoft.Extensions.Configuration;
using SlugEnt.ResourceHealthChecker.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlugEnt.ResourceHealthChecker
{
	/// <summary>
	/// Interface for a HealthChecker
	/// </summary>
	public interface IHealthChecker
	{
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
		public HealthCheckConfigBase Config { get; set; }

		/// <summary>
		/// Current Status of this Health Checker
		/// </summary>
		public EnumHealthStatus Status { get; }

		/// <summary>
		/// When Status last checked
		/// </summary>
		public DateTimeOffset LastStatusCheck { get; }


		/// <summary>
		/// Scheduled time for next health check
		/// </summary>
		public DateTimeOffset NextStatusCheck { get; }


		/// <summary>
		/// List of recent Health Checks
		/// </summary>
		public List<HealthEntryRecord> HealthEntries { get; }

		/// <summary>
		/// Returns true if the Health Checker is currently running
		/// </summary>
		public bool IsRunning { get; }


		/// <summary>
		/// Name of the Class of Checker
		/// </summary>
		public string CheckerName { get; set; }


		/// <summary>
		/// The Title / Name of the Health Checker
		/// </summary>
		public string ShortTitle { get; }


		/// <summary>
		/// The full name of this checker - includes the details about the item being checked
		/// </summary>
		public string FullTitle { get; }


		/// <summary>
		/// Performs the health check specific to the given HealthChecker.  
		/// </summary>
		/// <returns></returns>
		public Task CheckHealth(CancellationToken token, bool force = false);

		/// <summary>
		/// Displays this Health Checks information as HTML
		/// </summary>
		/// <param name="sb"></param>
		public void DisplayHTML(StringBuilder sb);


		/// <summary>
		/// Allows configuration of the Item from the IConfiguration system
		/// </summary>
		public void SetupFromConfig(IConfiguration configuration, string configurationSectionRoot);
	}
}
