using SlugEnt.ResourceHealthChecker;

namespace HealthCheck_Test
{
	public  class HCTest_Config : HealthCheckConfigBase
	{
		/// <summary>
		/// This is the Health Status that will be output from the PerformHealthCheck method.
		/// </summary>
		public EnumHealthStatus ExpectedOutput { get; set; }

		public string ExpectedMessageOutput { get; set; } = "";

		/// <summary>
		/// Amount of MS to simulate we are running.
		/// </summary>
		public int RunDelay { get; set; } = 0;

		/// <summary>
		/// If zero, run normally,
		/// Otherwise Set Status = this value
		/// </summary>
		public int RunNumberResult { get; set; } = 0;

	}
}
