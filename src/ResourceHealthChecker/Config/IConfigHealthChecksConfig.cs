namespace SlugEnt.ResourceHealthChecker;

/// <summary>
/// Interface that describes the Configuration of an Health Check
/// </summary>
public interface IConfigHealthChecksConfig {
	/// <summary>
	/// How often the HealthCheckProcessor should check the health of this resource
	/// </summary>
	public int CheckInterval { get; set; }
	
	/// <summary>
	/// True if the Health Check is enabled
	/// </summary>
	public bool IsEnabled { get; set; }
}