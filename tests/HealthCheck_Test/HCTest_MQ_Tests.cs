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
using SlugEnt.ResourceHealthChecker.RabbitMQ;

#pragma warning disable CS8618
namespace HealthCheck_Test
{
    [TestFixture]
    public class HCTest_MQ_Tests
    {
        private ILogger<HealthCheckerRabbitMQ> _logger;
        private CancellationToken              _cancellationToken;


        [OneTimeSetUp]
        public void InitialSetUp()
        {
            // The Health Checkers cannot create a logger themselves, so we create it for them.
            _logger = Mock.Of<ILogger<HealthCheckerRabbitMQ>>();

            _cancellationToken = CancellationToken.None;
        }



        [Test]
        [TestCase("amqp://user:password@server/instance", false)]
        [TestCase("amqp://user:password@server/instance", false)]
        public void DeconstructMQ_URL_SUCCESS(string url,
                                              bool isEncrypted)
        {
            // A. Setup
            HealthCheckerConfigRabbitMQ configMQ = new()
            {
                URL = url,
            };


            // B.  Test
            HealthCheckerRabbitMQ mq = new(_logger, "mq", configMQ);


            // C. Validate
            Assert.That(isEncrypted == mq.MQConfig.IsEncrypted, "A10");
            Assert.That("user" == mq.MQConfig.User, "A20");
            Assert.That("password" == mq.MQConfig.Password, "A30");
            Assert.That("server" == mq.MQConfig.ServerIP, "A40");
            Assert.That("instance" == mq.MQConfig.Instance, "A50");
        }


        [Test]
        [TestCase(true,
                     "user",
                     "password",
                     "localhost",
                     "instanceA",
                     false)]
        [TestCase(false,
                     "user",
                     "password",
                     "localhost",
                     "instanceA",
                     false)]
        [TestCase(false,
                     "",
                     "password",
                     "localhost",
                     "instanceA",
                     true)]
        [TestCase(false,
                     "user",
                     "",
                     "localhost",
                     "instanceA",
                     true)]
        [TestCase(false,
                     "user",
                     "password",
                     "",
                     "instanceA",
                     true)]
        [TestCase(false,
                     "user",
                     "password",
                     "localhost",
                     "",
                     true)]
        public async Task Construct_URL_SUCCESS(bool isEnctrypted,
                                                string user,
                                                string password,
                                                string server,
                                                string instance,
                                                bool throwsError)
        {
            HealthCheckerConfigRabbitMQ configMQ = new()
            {
                User        = user,
                Password    = password,
                ServerIP    = server,
                Instance    = instance,
                IsEncrypted = isEnctrypted,
            };


            // B & C.  Test & Validate
            if (!throwsError)
            {
                HealthCheckerRabbitMQ mq = new(_logger, "mq", configMQ);
                string                exptectedURL;
                if (isEnctrypted)
                    exptectedURL = "amqps://";
                else
                    exptectedURL = "amqp://";

                exptectedURL = exptectedURL + user + ":" + password + "@" + server + "/" + instance;

                Assert.That(exptectedURL, Is.EqualTo(mq.MQConfig.URL), "A10");
            }
            else
            {
                HealthCheckerRabbitMQ mq = new(_logger, "mq", configMQ);
                Assert.That(mq.IsReady, Is.False, "A20");

                // Try to run Health Check
                await mq.CheckHealth(_cancellationToken);
                Assert.That(EnumHealthStatus.NotReady == mq.Status, "A30:");
            }
        }
    }
}