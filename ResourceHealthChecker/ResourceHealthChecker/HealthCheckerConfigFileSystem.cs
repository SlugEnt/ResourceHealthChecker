using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.ResourceHealthChecker
{
	/// <summary>
	/// Configuration settings for the FileSystem health check.
	/// </summary>
	public class HealthCheckerConfigFileSystem : IHealthCheckConfig
	{
		/// <summary>
		/// The fully qualified path to be checked
		/// </summary>
		public string FolderPath { get; set; }

		/// <summary>
		/// If true then the Health Check requires that the folder be readable by the process
		/// </summary>
		public bool CheckIsReadable { get; set; }

		/// <summary>
		/// If true then the Health Check requires that the folder be writeable by the process
		/// </summary>
		public bool CheckIsWriteble { get; set; }
	}
}
