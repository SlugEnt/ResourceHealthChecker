namespace SlugEnt.ResourceHealthChecker.Config;

/// <summary>
/// This is the base object for a HealthCheck.
/// </summary>
public abstract class HealthCheckConfigBase
{
    /// <summary>
    /// How often the Check should be performed in seconds
    /// </summary>
    public int CheckInterval { get; set; } = 60;

    /// <summary>
    /// Whether the Check is enabled or not.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}