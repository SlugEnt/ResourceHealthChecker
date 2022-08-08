using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.ResourceHealthChecker;

	public class ConfigHealthChecks
	{
		/// <summary>
		/// The Type of Health Check
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// The Name of the Health Check
		/// </summary>
		public string Name { get; set; }

		//public string Config { get; set; }

		/// <summary>
		/// The Health Check Specific configuration of this health check
		/// </summary>
		//public IConfigHealthChecksConfig Config { get; set; }
	}

