namespace SlugEnt.ResourceHealthChecker;

/// <summary>
/// The base configuration of 
/// </summary>
public abstract class ConfigHealthChecker : IConfigHealthChecksConfig
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