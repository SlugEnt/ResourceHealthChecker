﻿namespace SlugEnt.ResourceHealthChecker
{
    /// <summary>
    /// Identifies the state of a given service or function
    /// </summary>
    public enum EnumHealthStatus
    {
        /// <summary>
        /// Initial Status.  It has not had a real check performed yet.
        /// </summary>
        NotCheckedYet = 10,


        /// <summary>
        /// This health check is currently disabled.
        /// </summary>
        Disabled = 19,

        /// <summary>
        /// The Health Check was not requested so it does not count
        /// </summary>
        NotRequested = 20,

        /// <summary>
        /// Service / API is Healthy and completely working
        /// </summary>
        Healthy = 50,

        /// <summary>
        /// No checks have been run, so the status is not yet known.
        /// </summary>
        Unknown = 100,



        /// <summary>
        /// Service is working suboptimally, Generally means it is accessible, but maybe permissions issues or something else is preventing a Healthy status
        /// </summary>
        Degraded = 200,


        /// <summary>
        /// The Health Checker never completed initial setup or configuration successfully.
        /// </summary>
        NotReady = 250,


        /// <summary>
        /// Service is completely failed.
        /// </summary>
        Failed = 254,
    }
}