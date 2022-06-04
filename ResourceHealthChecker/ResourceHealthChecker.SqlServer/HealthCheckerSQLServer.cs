using SlugEnt.ResourceHealthChecker;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ResourceHealthChecker.SqlServer;

namespace SlugEnt.ResourceHealthChecker.SqlServer
{
	public class HealthCheckerSQLServer : AbstractHealthChecker {
		//private ILogger<HealthCheckerSQLServer> _logger;
		private string _connectionString;


		public HealthCheckerSQLServer (ILogger<HealthCheckerSQLServer> logger, string descriptiveName, HealthCheckerConfigSQLServer sqlConfig) : base(
			descriptiveName, EnumHealthCheckerType.Database, sqlConfig) {

			if ( sqlConfig.ConnectionString == string.Empty )
				throw new ApplicationException("At the moment only Connection Strings are supported.  Must supply a connection string");

			_logger = logger;
			CheckerName = "SQL Server Availability Checker";
			Config = sqlConfig;


			// Lets build a complete Connection string we can use.
			SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder(sqlConfig.ConnectionString);
			sqlBuilder ["TrustServerCertificate"] = true;
			sqlBuilder ["Connect Timeout"] = 2000; 
			_connectionString = sqlBuilder.ConnectionString;


			string msg;
			if ( sqlConfig.ConnectionString != string.Empty )
				msg = sqlConfig.ConnectionString;
			else
				msg = sqlConfig.Server + "/" + sqlConfig.Instance; 
			_logger.LogDebug("Health Checker SQL Server Instance Constructed:  [" + descriptiveName + "] -  " + msg);
		}


		/// <summary>
		///  A synonym of the abstract classes Config property.
		/// </summary>
		public HealthCheckerConfigSQLServer SQLConfig
		{
			get { return (HealthCheckerConfigSQLServer)this.Config; }
		}


		/// <summary>
		/// Displays the Status in HTML Format.
		/// </summary>
		/// <param name="sb"></param>
		/// <exception cref="NotImplementedException"></exception>
		public override void DisplayHTML(StringBuilder sb)
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// Performs the actual Health Check.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		protected override async Task<(EnumHealthStatus, string)> PerformHealthCheck() {
			string msg = "";
			EnumHealthStatus status;

			if ( SQLConfig.ReadTable != string.Empty ) {
				try {
					using ( SqlConnection conn = new SqlConnection(_connectionString) ) {
						conn.Open();
						string sql = "Select Top 1 * From " + SQLConfig.ReadTable;
						SqlCommand sqlCommand = new SqlCommand(sql, conn);
						
						await sqlCommand.ExecuteNonQueryAsync();
						status = EnumHealthStatus.Healthy;
					}
				}
				catch ( Exception ex ) {
					msg = ex.Message;
					status = EnumHealthStatus.Failed;
				}

				return (status, msg);
			}

			return (EnumHealthStatus.Healthy, string.Empty);
		}
	}
}
