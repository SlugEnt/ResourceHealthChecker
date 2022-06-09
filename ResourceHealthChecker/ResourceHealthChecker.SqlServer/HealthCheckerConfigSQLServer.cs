using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugEnt.ResourceHealthChecker;

namespace ResourceHealthChecker.SqlServer
{
	/// <summary>
	/// Used to specific configuration information for connecting to a specific SQL Instance.
	/// </summary>
	public class HealthCheckerConfigSQLServer : IHealthCheckConfig {

		/// <summary>
		/// Creates a SQL Server HealthChecker configuration object.
		/// </summary>
		/// <param name="connectionString"></param>
		/// <param name="readTable">By default is set to SlugEntHealthCheck</param>
		/// <param name="writeTable">By default is set to SlugEntHealthCheck</param>
		public HealthCheckerConfigSQLServer (string connectionString, string readTable = "Production.Location", string writeTable = "") {
			ConnectionString = connectionString;

			if (readTable != string.Empty)
				ReadTable = readTable;

			if (writeTable != string.Empty)
				WriteTable = writeTable;
		}


		public bool CheckReadTable { get; set; } = true;
		public bool CheckWriteTable { get; set; } = true;


		/// <summary>
		/// The full connection string to use to connect to DB.  If this is anything other than empty then this is used to connect.
		/// </summary>
		public string ConnectionString { get; set; } = "";

		public string Database { get; set; } = "";
		/// <summary>
		/// Name of the table used to validate that Select works (Read)
		/// </summary>
		public string ReadTable { get; set; } = "";

		/// <summary>
		/// Name of table used that Insert Works (Write)
		/// </summary>
		public string WriteTable { get; set; } = "SlugEntHealthCheck";

		/// <summary>
		/// UserName to connect as.  Ignored if ConnectionString is specified.
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// Password to ues.  Ignored if ConnectionString is specified.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Server to connect to.  Ignored if ConnectionString is specified.
		/// </summary>
		public string Server { get; set; }

		/// <summary>
		/// Instance to connect to.  Ignored if ConnectionString is specified.
		/// </summary>
		public string Instance { get; set; }

		/// <summary>
		/// Port to connect as.  Ignored if ConnectionString is specified.
		/// </summary>
		public string Port { get; set; }

		/// <summary>
		/// Options to use.  Ignored if ConnectionString is specified.
		/// </summary>
		public string Options { get; set; }
	}
}
