namespace SlugEnt.ResourceHealthChecker
{
    public enum EnumHealthCheckerType
    {
        /// <summary>
        /// Is A Database Check
        /// </summary>
        Database = 0,

        /// <summary>
        /// Is a File System Check
        /// </summary>
        FileSystem = 1,

        /// <summary>
        /// Is a Rabbit MQ check
        /// </summary>
        RabbitMQ = 2,

        /// <summary>
        /// Is a Redis Check
        /// </summary>
        Redis = 3,

        /// <summary>
        /// Is an External API Check
        /// </summary>
        ExternalAPI = 4,

        /// <summary>
        /// Is a Dummy Check
        /// </summary>
        Dummy = 254,
    }
}