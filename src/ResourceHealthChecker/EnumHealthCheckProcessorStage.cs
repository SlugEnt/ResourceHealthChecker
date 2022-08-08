namespace ResourceHealthChecker
{
	public enum EnumHealthCheckProcessorStage
	{
		Constructed = 0,

		Initializing = 10,

		Initialized = 20,

		Started = 30,

		Processing = 50,

		FailedToStart = 253,

		Finished = 254,


	}
}
