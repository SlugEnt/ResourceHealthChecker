namespace SlugEnt.ResourceHealthChecker.Config;

/// <summary>
/// This object assists with reading the setup of the Health Checks from the IConfiguration
/// </summary>
public class ConfigurationHealthChecks
	{
		/// <summary>
		/// The Type of Health Check
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// The Name of the Health Check
		/// </summary>
		public string Name { get; set; }
	
	}

