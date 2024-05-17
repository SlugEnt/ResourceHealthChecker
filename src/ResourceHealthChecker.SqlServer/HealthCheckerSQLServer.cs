using SlugEnt.ResourceHealthChecker;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResourceHealthChecker;
using ResourceHealthChecker.SqlServer;

namespace SlugEnt.ResourceHealthChecker.SqlServer;

/// <summary>
/// Provides Health Checking services on a SQL Database
/// </summary>
public class HealthCheckerSQLServer : AbstractHealthChecker, IHealthCheckerSQLServer
{
    //private ILogger<HealthCheckerSQLServer> _logger;
    private string           _connectionString;
    private EnumHealthStatus _statusRead    = EnumHealthStatus.Failed;
    private EnumHealthStatus _statusWrite   = EnumHealthStatus.Failed;
    private EnumHealthStatus _statusOverall = EnumHealthStatus.Failed;


    /// <summary>
    /// Constructor used when building object from Configuration
    /// </summary>
    /// <param name="logger"></param>
    public HealthCheckerSQLServer(ILogger<HealthCheckerSQLServer> logger) : base(logger)
    {
        CheckerName = "SQL Server Availability Checker";
        Config      = new HealthCheckerConfigSQLServer();
    }


    /// <summary>
    /// Constructs a SQL Server Health Checker
    /// </summary>
    /// <param name="logger">Where to Log to</param>
    /// <param name="descriptiveName">Name of this health checker</param>
    /// <param name="sqlConfig">The SQL Configuration file for the Health Checker</param>
    /// <exception cref="ApplicationException"></exception>
    public HealthCheckerSQLServer(ILogger<HealthCheckerSQLServer> logger, string descriptiveName, HealthCheckerConfigSQLServer sqlConfig) :
        base(descriptiveName, EnumHealthCheckerType.Database, sqlConfig, logger)
    {
        if (sqlConfig.ConnectionString == string.Empty)
            throw new ApplicationException("At the moment only Connection Strings are supported.  Must supply a connection string");

        _logger     = logger;
        CheckerName = "SQL Server Availability Checker";
        Config      = sqlConfig;


        BuildConnectionString(sqlConfig.ConnectionString);

        _logger.LogDebug("SQL Constructed Connection String:  [ {SQLConnection} ]", _connectionString);

        IsReady = true;
    }


    /// <summary>
    /// Builds the connection String, appending some items as necessary
    /// </summary>
    /// <param name="currentConnectionString"></param>
    private void BuildConnectionString(string currentConnectionString)
    {
        // Lets build a complete Connection string we can use.
        SqlConnectionStringBuilder sqlBuilder = new(currentConnectionString);
        sqlBuilder["TrustServerCertificate"] = true;
        sqlBuilder["Connect Timeout"]        = 5;

        // Store some values from Connection string into Config
        _connectionString = sqlBuilder.ConnectionString;

        if (SQLConfig.Database == string.Empty)
            SQLConfig.Database = sqlBuilder.InitialCatalog;
        if (SQLConfig.UserName == string.Empty)
            SQLConfig.UserName = sqlBuilder.UserID;
        if (SQLConfig.Server == string.Empty)
            SQLConfig.Server = sqlBuilder.DataSource;
        SQLConfig.ConnectionString = _connectionString;
    }


    /// <summary>
    ///  A synonym of the abstract classes Config property.
    /// </summary>
    public HealthCheckerConfigSQLServer SQLConfig
    {
        get { return (HealthCheckerConfigSQLServer)this.Config; }
    }


    /// <summary>
    /// Displays the SQL Server information
    /// </summary>
    public override string FullTitle
    {
        get
        {
            string access = "";
            if (SQLConfig.CheckReadTable)
                access = "Read";
            if (SQLConfig.CheckWriteTable)
                access += "Write";

            return access + " | " + ShortTitle + "  -->  " + SQLConfig.Server + ":" + SQLConfig.Database;
        }
    }



    /// <summary>
    /// Displays the Status in HTML Format.
    /// </summary>
    /// <param name="sb"></param>
    /// <exception cref="NotImplementedException"></exception>
    public override void DisplayHTML(StringBuilder sb)
    {
        sb.Append("<p>SQL Database: " + SQLConfig.Database + "</p>");
        sb.Append("<p>  Server:  " + SQLConfig.Server + "</p>");


        sb.Append("<h4>  Health Checks</h4>");
        if (SQLConfig.CheckReadTable)
        {
            string color = HealthCheckProcessor.GetStatusColor(_statusRead);
            sb.Append("<p style =\"color:" + color + ";\">" + "  Read Table:  " + SQLConfig.ReadTable + "  [ " + _statusRead.ToString() + " ]");
        }
        else
            sb.Append("<p>  Read Table:  Not Requested");

        sb.Append("</p>");

        if (SQLConfig.CheckWriteTable)
        {
            string color = HealthCheckProcessor.GetStatusColor(_statusWrite);
            sb.Append("<p style =\"color:" + color + ";\">" + "  Write Table:  " + SQLConfig.WriteTable + "  [ " + _statusWrite.ToString() + " ]");
        }
        else
            sb.Append("<p>  Write Table:  Not Requested");

        sb.Append("</p>");
    }


    /// <summary>
    /// Performs the actual Health Check.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected override async Task<(EnumHealthStatus, string)> PerformHealthCheck(CancellationToken stoppingToken)
    {
        string msg = "";

        if (SQLConfig.CheckReadTable)
        {
            try
            {
                using (SqlConnection conn = new(_connectionString))
                {
                    await conn.OpenAsync(stoppingToken);
                    string     sql        = "Select Top 1 * From " + SQLConfig.ReadTable;
                    SqlCommand sqlCommand = new(sql, conn);

                    await sqlCommand.ExecuteNonQueryAsync(stoppingToken);
                    _statusRead = EnumHealthStatus.Healthy;
                }
            }
            catch (Exception ex)
            {
                msg         = "Read Msg: " + ex.Message;
                _statusRead = EnumHealthStatus.Failed;
            }
        }


        if (SQLConfig.CheckWriteTable)
        {
            try
            {
                using (SqlConnection conn = new(_connectionString))
                {
                    await conn.OpenAsync(stoppingToken);
                    int    id        = 0;
                    string sqlInsert = "Insert Into " + SQLConfig.WriteTable + " VALUES ('" + DateTime.Now + "'); " + " SELECT CONVERT(int,scope_identity())";


                    // Insert
                    SqlCommand sqlCommand = new(sqlInsert, conn);
                    id = (int)await sqlCommand.ExecuteScalarAsync(stoppingToken);


                    // Delete
                    string sqlDelete = "DELETE FROM " + SQLConfig.WriteTable + " WHERE Id=" + id.ToString();
                    sqlCommand = new(sqlDelete, conn);
                    await sqlCommand.ExecuteScalarAsync(stoppingToken);

                    _statusWrite = EnumHealthStatus.Healthy;
                }
            }
            catch (Exception ex)
            {
                msg          += "  |  Write Msg: " + ex.Message;
                _statusWrite =  EnumHealthStatus.Failed;
            }
        }


        // Figure out the overall status
        if (_statusWrite > EnumHealthStatus.Healthy || _statusRead > EnumHealthStatus.Healthy)
        {
            if (_statusRead > EnumHealthStatus.Healthy)
                _statusOverall = _statusRead;
            if (_statusWrite > _statusOverall)
                _statusOverall = _statusWrite;
        }
        else
        {
            _statusOverall = EnumHealthStatus.Healthy;
        }


        return (_statusOverall, msg);
    }


    /// <summary>
    /// Reads the SQL Specific properties of Config items for the ConfigurationHealthChecks configuration objects
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configurationSectionRoot"></param>
    public override void SetupFromConfig(IConfiguration configuration, string configurationSectionRoot)
    {
        base.SetupFromConfig(configuration, configurationSectionRoot);

        // Read the File Specific config
        this.SQLConfig.ConnectionString = configuration.GetSection(configurationSectionRoot + ":Config:ConnectionString").Get<string>();
        this.SQLConfig.ReadTable        = configuration.GetSection(configurationSectionRoot + ":Config:ReadFileName").Get<string>();
        this.SQLConfig.WriteTable       = configuration.GetSection(configurationSectionRoot + ":Config:WriteFileName").Get<string>();
        this.SQLConfig.CheckReadTable   = configuration.GetSection(configurationSectionRoot + ":Config:CheckReadTable").Get<bool>();
        this.SQLConfig.CheckWriteTable  = configuration.GetSection(configurationSectionRoot + ":Config:CheckWriteTable").Get<bool>();
        IsReady                         = true;
    }
}