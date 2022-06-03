using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.ResourceHealthChecker
{
	public class HealthCheckConfigDatabase : IHealthCheckConfig
	{
		public HealthCheckConfigDatabase (string name, string connectionString) {

		}


		/// <summary>
		/// Friendly name for this database connection
		/// </summary>
		public string Name { get; set; }


		/// <summary>
		/// Database Connection string
		/// </summary>
		public string ConnectionString { get; set; }
	}
}
