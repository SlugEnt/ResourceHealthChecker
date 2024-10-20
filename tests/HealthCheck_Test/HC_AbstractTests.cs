using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SlugEnt;
using SlugEnt.ResourceHealthChecker;


#pragma warning disable CS8618
namespace HealthCheck_Test
{
    [TestFixture]
    public class HC_AbstractTests
    {
        private ILogger<HCTest_Checker> _logger;
        private CancellationToken       _cancellationToken;


        [OneTimeSetUp]
        public void InitialSetUp()
        {
            // The Health Checkers cannot create a logger themselves, so we create it for them.
            _logger = Mock.Of<ILogger<HCTest_Checker>>();

            _cancellationToken = CancellationToken.None;
        }


        [SetUp]
        public void Setup() { }


        /// <summary>
        /// Tests that the Status returned by the PerformHealth Check routines is actually set.
        /// </summary>
        /// <returns></returns>
        [Test]
        [TestCase(EnumHealthStatus.Healthy)]
        [TestCase(EnumHealthStatus.Failed)]
        [TestCase(EnumHealthStatus.Degraded)]
        [TestCase(EnumHealthStatus.Unknown)]
        [TestCase(EnumHealthStatus.NotRequested)]
        [TestCase(EnumHealthStatus.NotCheckedYet)]
        public async Task PerformHealth_StatusSet(EnumHealthStatus expectedStatus)
        {
            // A. Setup
            HCTest_Config config = new()
            {
                ExpectedMessageOutput = "Ooops",
                ExpectedOutput        = expectedStatus,
            };

            HCTest_Checker checker = new(_logger, "Dummy", config);


            // B. Test
            await checker.CheckHealth(_cancellationToken);


            // C. Validate
            if (expectedStatus != EnumHealthStatus.NotCheckedYet && expectedStatus != EnumHealthStatus.Unknown)
                Assert.That(expectedStatus, Is.EqualTo(checker.Status), "A10:");

            // NotCheckedYet should never be returned.  Unknown is returned.
            else
                Assert.That(EnumHealthStatus.Healthy, Is.EqualTo(checker.Status), "A20:");
        }



        /// <summary>
        /// Tests that the Status returned by the PerformHealth Check routines is actually set.
        /// </summary>
        /// <returns></returns>
#pragma warning disable CS4014
#pragma warning disable CS1998
        [Test]
        public async Task IsRunningIsSet_WhileRunning()
        {
            // A. Setup
            HCTest_Config config = new()
            {
                ExpectedMessageOutput = "",
                ExpectedOutput        = EnumHealthStatus.Healthy,
                RunDelay              = 100,
            };

            HCTest_Checker checker = new(_logger, "Dummy", config);


            // B. Test

            checker.CheckHealth(_cancellationToken);


            // C. Validate
            Assert.That(checker.IsRunning, Is.True, "A10");
        }
#pragma warning restore CS4014
#pragma warning restore CS1998


        /// <summary>
        /// Tests that the Status returned by the PerformHealth Check routines is actually set.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task IsRunningFalse_WhenFinished()
        {
            // A. Setup
            HCTest_Config config = new()
            {
                ExpectedMessageOutput = "",
                ExpectedOutput        = EnumHealthStatus.Healthy,
                RunDelay              = 100,
            };

            HCTest_Checker checker = new(_logger, "Dummy", config);


            // B. Test
#pragma warning  disable CS4014
            checker.CheckHealth(_cancellationToken);
#pragma warning restore CS4014

            // C. Validate
            Assert.That(checker.IsRunning, Is.True, "A10");
            await Task.Delay(config.RunDelay + 50);
            Assert.That(checker.IsRunning, Is.False, "A20");
        }



        /// <summary>
        /// When a Health Check is run, if the result is the same as the prior result, then we increment the Counter on the HealthRecord.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task MultipleSameCheckResults_OneHealthRecord()
        {
            // A. Setup

            CancellationToken cancellationToken = CancellationToken.None;
            HCTest_Config config = new()
            {
                ExpectedMessageOutput = "",
                ExpectedOutput        = EnumHealthStatus.Healthy,
            };

            HCTest_Checker checker = new(_logger, "Dummy", config);


            // B. Test
            await checker.CheckHealth(_cancellationToken);

            // Need to manipulate Next Run Time
            checker.NextStatusCheck = DateTimeOffset.Now.AddHours(-1);

            // Check 2 more times
            await checker.CheckHealth(cancellationToken);
            checker.NextStatusCheck = DateTimeOffset.Now.AddHours(-1);

            await checker.CheckHealth(cancellationToken);


            // C. Validate

            // check the Status
            Assert.That(EnumHealthStatus.Healthy, Is.EqualTo(checker.Status), "A10");

            // Make sure 1 entry in HealthRecords.
            Assert.That(1, Is.EqualTo(checker.HealthEntries.Count), "A20");

            // First HealthEntry is Health
            Assert.That(EnumHealthStatus.Healthy, Is.EqualTo(checker.HealthEntries[0].HealthStatus), "A30");

            // First HealthEntry status count = 3
            Assert.That(checker.HealthEntries[0].Count, Is.EqualTo(3), "A40");
        }



        /// <summary>
        /// When a Health Check is run, if the result is the same as the prior result, then we increment the Counter on the HealthRecord.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task MultipleDifferentCheckResults_MultipleHealthRecords()
        {
            // A. Setup

            CancellationToken cancellationToken = CancellationToken.None;
            HCTest_Config config = new()
            {
                ExpectedMessageOutput = "",
                ExpectedOutput        = EnumHealthStatus.Healthy,
            };

            HCTest_Checker checker = new(_logger, "Dummy", config);


            // B. Test
            await checker.CheckHealth(_cancellationToken);

            // Need to manipulate Next Run time and expected status
            checker.NextStatusCheck = DateTimeOffset.Now.AddHours(-1);
            config.RunNumberResult  = (int)EnumHealthStatus.Degraded;
            await checker.CheckHealth(cancellationToken);

            // Need to manipulate Next Run time and expected status
            checker.NextStatusCheck = DateTimeOffset.Now.AddHours(-1);
            config.RunNumberResult  = (int)EnumHealthStatus.Healthy;
            await checker.CheckHealth(cancellationToken);


            // C. Validate

            // check the Status
            Assert.That(checker.Status, Is.EqualTo(EnumHealthStatus.Healthy), "A10");

            // Make sure 3 entries in HealthRecords.
            Assert.That(checker.HealthEntries.Count, Is.EqualTo(3), "A20");

            // First and 3rd HealthEntry is Healthy
            Assert.That(checker.HealthEntries[0].HealthStatus, Is.EqualTo(EnumHealthStatus.Healthy), "A30");
            Assert.That(checker.HealthEntries[2].HealthStatus, Is.EqualTo(EnumHealthStatus.Healthy), "A31");

            // Second is degraded
            Assert.That(checker.HealthEntries[1].HealthStatus, Is.EqualTo(EnumHealthStatus.Degraded), "A32");

            // First HealthEntry status count for all 3 is 1
            Assert.That(checker.HealthEntries[0].Count, Is.EqualTo(1), "A40");
            Assert.That(checker.HealthEntries[1].Count, Is.EqualTo(1), "A41");
            Assert.That(checker.HealthEntries[2].Count, Is.EqualTo(1), "A42");
        }


        /// <summary>
        /// Make sure Health Checker Type is set
        /// </summary>
        /// <returns></returns>
        [Test]
        public void HealthCheckerType_IsSet()
        {
            // A. Setup
            HCTest_Config config = new()
            {
                ExpectedMessageOutput = "",
                ExpectedOutput        = EnumHealthStatus.Healthy,
            };

            HCTest_Checker checker = new(_logger, "Dummy", config);


            // B. Test
            // - No Testing needed


            // C. Validate
            Assert.That(checker.HealthCheckerType, Is.EqualTo(EnumHealthCheckerType.Dummy), "A10");
        }



        /// <summary>
        /// Ensures the NextStatusCheck has been set to a time greater than current after running a health check
        /// </summary>
        /// <param name="expectedStatus"></param>
        /// <returns></returns>
        [Test]
        [TestCase(EnumHealthStatus.Healthy)]
        [TestCase(EnumHealthStatus.Failed)]
        [TestCase(EnumHealthStatus.Degraded)]
        [TestCase(EnumHealthStatus.Unknown)]
        [TestCase(EnumHealthStatus.NotRequested)]
        [TestCase(EnumHealthStatus.NotCheckedYet)]
        public async Task NextStatusCheck_IsSet(EnumHealthStatus expectedStatus)
        {
            // A. Setup
            HCTest_Config config = new()
            {
                ExpectedMessageOutput = "",
                ExpectedOutput        = expectedStatus,
            };

            HCTest_Checker checker = new(_logger, "Dummy", config);

            DateTimeOffset currentNextStatus = checker.NextStatusCheck;


            // B. Test
            await checker.CheckHealth(_cancellationToken);


            // C.  Validate
            // Next status check has been set.
            //Assert.Greater(checker.NextStatusCheck, DateTimeOffset.Now, "A10");
            Assert.That(checker.NextStatusCheck, Is.GreaterThan(currentNextStatus), "A20");
        }


        [Test]
        public async Task DisabledStatus_Check()
        {
            // A. Setup
            HCTest_Config config = new()
            {
                ExpectedMessageOutput = "",
                ExpectedOutput        = EnumHealthStatus.Disabled,
            };


            // By default checks are enabled
            HCTest_Checker checker = new(_logger, "Dummy", config);
            checker.IsEnabled = false;

            DateTimeOffset currentNextStatus = checker.NextStatusCheck;


            // B. Test
            await checker.CheckHealth(_cancellationToken);


            // C.  Validate
            // Next status check has been set.
            Assert.That(EnumHealthStatus.Disabled, Is.EqualTo(checker.Status), "A10:");
        }
    }
}