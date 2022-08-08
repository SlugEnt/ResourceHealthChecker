namespace SlugEnt.ResourceHealthChecker.Config;

/// <summary>
/// The base configuration of 
/// </summary>
public abstract class ConfigHealthChecker : IConfigurationHealthCheckConfig
{
	/// <summary>
	/// How often to check the resource - In Seconds
	/// </summary>
	public int CheckInterval { get;set;}


	/// <summary>
	///   Whether the Check Service should be activated
	/// </summary>
	public bool IsEnabled { get; set; } = true;
}