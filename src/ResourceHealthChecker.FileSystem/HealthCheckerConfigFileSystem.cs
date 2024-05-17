using SlugEnt.ResourceHealthChecker.Config;

namespace SlugEnt.ResourceHealthChecker;

/// <summary>
/// Configuration settings for the FileSystem health check.
/// </summary>
public class HealthCheckerConfigFileSystem : HealthCheckConfigBase, IConfigurationHealthCheckConfig, IHealthCheckerFileSystem
{
    /// <summary>
    /// The fully qualified path to be checked
    /// </summary>
    public string FolderPath { get; set; } = "";

    /// <summary>
    /// If true then the Health Check requires that the folder be readable by the process
    /// </summary>
    public bool CheckIsReadable { get; set; } = true;

    /// <summary>
    /// If true then the Health Check requires that the folder be writeable by the process
    /// </summary>
    public bool CheckIsWriteable { get; set; } = true;


    /// <summary>
    /// The name of the file used to conduct a read validation
    /// </summary>
    public string ReadFileName { get; set; } = "SlugEntHealthCheck.txt";


    /// <summary>
    /// In many cases Write means Read also.  If this property is true, if it can write, then it is assumed it can read.
    /// The explicit Read case will be omitted.  This is also useful in cases where files are automatically removed from
    /// a directory and thus our ReadFileName might be missing
    /// </summary>
    public bool AssumeReadableIfWriteable { get; set; } = true;
}