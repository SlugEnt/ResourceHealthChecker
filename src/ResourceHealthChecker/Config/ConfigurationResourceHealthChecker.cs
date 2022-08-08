using System.Collections.Generic;

namespace SlugEnt.ResourceHealthChecker.Config;

/// <summary>
/// This is the root IConfiguration class for the ResourceHealthChecker.
/// </summary>
public class ConfigurationResourceHealthChecker
	{
		/// <summary>
		/// All of the Health Checks
		/// </summary>
		public List<ConfigurationHealthChecks> ConfigHealthChecks { get; set; }

		/// <summary>
		/// How often the Health Check Processor will cycle thru all the Health Checkers to SEE if anything needs to be checked.  In Milliseconds
		/// </summary>
		public int CheckIntervalMS { get; set; }
	}

