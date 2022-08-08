using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.ResourceHealthChecker;

	/// <summary>
	/// This is the root IConfiguration class for the ResourceHealthChecker.
	/// </summary>
	public class ConfigResourceHealthChecker
	{
		/// <summary>
		/// All of the Health Checks
		/// </summary>
		public List<ConfigHealthChecks> ConfigHealthChecks { get; set; }
	}

