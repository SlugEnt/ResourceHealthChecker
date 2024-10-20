namespace ResourceHealthChecker
{
    /// <summary>
    /// Tells the status current of the Health Checker lifetime.
    /// </summary>
    public enum EnumHealthCheckProcessorStage
    {
        /// <summary>
        /// Processor has been constructed
        /// </summary>
        Constructed = 0,

        /// <summary>
        /// Processor is initializing
        /// </summary>
        Initializing = 10,

        /// <summary>
        /// Processor initialization is completed
        /// </summary>
        Initialized = 20,

        /// <summary>
        /// Processor has been started, but no processing done yet.
        /// </summary>
        Started = 30,

        /// <summary>
        /// Processor is in its main loop, where it is checking.
        /// </summary>
        Processing = 50,

        /// <summary>
        /// Processor was started, but there are no checks defined and thus will not continue running.
        /// </summary>
        NoChecksToRun = 252,

        /// <summary>
        /// Processor has checks, but an error has prevented it from starting.
        /// </summary>
        FailedToStart = 253,

        /// <summary>
        /// The processor has completed running and been asked to stop.
        /// </summary>
        Finished = 254,
    }
}