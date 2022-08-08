namespace SlugEnt.ResourceHealthChecker.Config;

public abstract class HealthCheckConfigBase
{
	/// <summary>
	/// How often the Check should be performed in seconds
	/// </summary>
	public int CheckInterval { get; set; } = 60;

	public bool IsEnabled { get; set; } = true;
}

