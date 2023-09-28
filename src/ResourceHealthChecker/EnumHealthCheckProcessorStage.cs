namespace ResourceHealthChecker
{
    public enum EnumHealthCheckProcessorStage
    {
        Constructed = 0,

        Initializing = 10,

        Initialized = 20,

        Started = 30,

        Processing = 50,

        NoChecksToRun = 252,

        FailedToStart = 253,

        Finished = 254,
    }
}