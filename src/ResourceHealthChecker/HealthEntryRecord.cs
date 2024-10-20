using System;

namespace SlugEnt.ResourceHealthChecker
{
    /// <summary>
    /// Defines a Health Entry record that records information about a Health Check
    /// </summary>
    public class HealthEntryRecord
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="healthStatus">The status of the health check</param>
        /// <param name="message">Message to be stored about the health check</param>
        public HealthEntryRecord(EnumHealthStatus healthStatus,
                                 string message = "")
        {
            StarteDateTimeOffset = DateTimeOffset.Now;
            HealthStatus         = healthStatus;
            Message              = message;
            Increment();
        }


        /// <summary>
        /// The status this health record represents
        /// </summary>
        public EnumHealthStatus HealthStatus { get; protected set; }

        /// <summary>
        /// When this chain of checks with this status was first created
        /// </summary>
        public DateTimeOffset StarteDateTimeOffset { get; protected set; }


        /// <summary>
        /// The last time this health record was updated
        /// </summary>
        public DateTimeOffset LastDateTimeOffset { get; protected set; }


        /// <summary>
        /// The original message associated with this record
        /// </summary>
        public string Message { get; protected set; }


        /// <summary>
        /// Number of times this status has been consecutively recorded
        /// </summary>
        public long Count { get; protected set; }


        /// <summary>
        /// Increments the number of times by 1 that this health record has consecutively been recorded.
        /// </summary>
        public void Increment()
        {
            Count++;
            LastDateTimeOffset = DateTimeOffset.Now;
        }
    }
}