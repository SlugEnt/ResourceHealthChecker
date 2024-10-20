using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SlugEnt;
using SlugEnt.ResourceHealthChecker;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Abstractions.TestingHelpers;

#pragma warning disable CS8618

namespace HealthCheck_Test
{
    public class HCFileSystem_Tests
    {
        private UniqueKeys                       _uniqueKey;
        private ILogger<HealthCheckerFileSystem> _logger;
        private string                           _rootPath = "";


        [OneTimeSetUp]
        public void InitialSetUp()
        {
            _uniqueKey = new UniqueKeys(".");

            // The Health Checkers cannot create a logger themselves, so we create it for them.
            _logger = Mock.Of<ILogger<HealthCheckerFileSystem>>();
        }


        [SetUp]
        public void Setup()
        {
            // Setup the root file system.
            string rootPath = @"C:\temp\ResourceHealthCheckerTests";
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            _rootPath = rootPath;
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, true);
        }


        /// <summary>
        /// Checking Write Status only works
        /// </summary>
        /// <param name="isDirectoryWriteable"></param>
        /// <param name="expectedHealthStatus"></param>
        /// <returns></returns>
        [Test]
        [TestCase(EnumHealthStatus.Healthy)]
        [TestCase(EnumHealthStatus.Failed)]
        public async Task WriteBasicSuccess(EnumHealthStatus expectedHealthStatus)
        {
            // A. Setup
            CancellationToken cancellationToken = CancellationToken.None;
            MockFileSystem    fileSystem        = new();

            string         dirName       = _uniqueKey.GetKey("Write." + expectedHealthStatus.ToString());
            IDirectoryInfo directoryInfo = fileSystem.Directory.CreateDirectory(dirName);

            if (expectedHealthStatus == EnumHealthStatus.Failed)
                fileSystem.File.SetAttributes(dirName, FileAttributes.ReadOnly);


            HealthCheckerConfigFileSystem config = new()
            {
                CheckIsWriteable = true,
                CheckIsReadable  = false,
                FolderPath       = directoryInfo.FullName,
            };
            HealthCheckerFileSystem healthChecker = new(fileSystem,
                                                        _logger,
                                                        dirName,
                                                        config);


            // B. Test
            await healthChecker.CheckHealth(cancellationToken);


            // C. Validate

            // check the Status
            Assert.That(expectedHealthStatus == healthChecker.StatusWrite, "A10");
            Assert.That(expectedHealthStatus == healthChecker.Status, "A20");
        }



        /// <summary>
        /// checking Read status on a directory without permissions returns failure.  This is a specific scenario:
        /// The HealthCheck default file does not exist.  So we are going after a random file in the directory.
        /// </summary>
        /// <param name="expectedHealthStatus"></param>
        /// <returns></returns>
        [Test]
        [TestCase(EnumHealthStatus.Failed)]
        public async Task ReadHealth_NoPermissionToRequestedDirectory_ReturnsFailure(EnumHealthStatus expectedHealthStatus)
        {
            // A. Setup
            CancellationToken cancellationToken = CancellationToken.None;
            string            dirName           = _uniqueKey.GetKey("Read." + expectedHealthStatus.ToString());

            HealthCheckerConfigFileSystem config = new()
            {
                CheckIsWriteable = false,
                CheckIsReadable  = true,
                FolderPath       = dirName,
            };


            var mock = new Mock<IFileSystem>();
            mock.Setup(x => x.Directory.Exists(config.FolderPath)).Throws(new UnauthorizedAccessException("Permission Denied"));
            IFileSystem fileSystem = mock.Object;
            fileSystem.Directory.CreateDirectory(dirName);

            HealthCheckerFileSystem healthChecker = new(fileSystem,
                                                        _logger,
                                                        dirName,
                                                        config);


            // B. Test
            await healthChecker.CheckHealth(cancellationToken);


            // C. Validate

            // check the Status
            Assert.That(expectedHealthStatus == healthChecker.StatusRead, "A10");
            Assert.That(expectedHealthStatus == healthChecker.Status, "A20");
        }



        /// <summary>
        /// checking Read status on a directory with permissions with HealthCheck File Returns Healthy. 
        /// </summary>
        /// <param name="expectedHealthStatus"></param>
        /// <returns></returns>
        [Test]
        [TestCase(EnumHealthStatus.Healthy)]
        public async Task ReadHealth_HealthCheckFileExists_ReturnsHealthy(EnumHealthStatus expectedHealthStatus)
        {
            // A. Setup
            CancellationToken cancellationToken = CancellationToken.None;


            // Setup this test
            string dirName = _uniqueKey.GetKey("Read." + expectedHealthStatus.ToString());
            string testDir = Path.Join(_rootPath, dirName);
            if (!Directory.Exists(testDir))
                Directory.CreateDirectory(testDir);


            HealthCheckerConfigFileSystem config = new()
            {
                CheckIsWriteable = false,
                CheckIsReadable  = true,
                FolderPath       = testDir,
            };


            // Create the file
            string fullName = Path.Join(testDir, config.ReadFileName);
            File.WriteAllBytes(fullName,
                               new byte[]
                               {
                                   23, 25
                               });


            // B. Test
            HealthCheckerFileSystem healthChecker = new(_logger, testDir, config);
            await healthChecker.CheckHealth(cancellationToken);


            // C. Validate

            // check the Status
            Assert.That(expectedHealthStatus == healthChecker.StatusRead, "A10:  " + healthChecker.HealthEntries[0].Message);
            Assert.That(expectedHealthStatus == healthChecker.Status, "A20");
        }



        /// <summary>
        /// Checking Read status on a directory without permissions returns failure
        /// </summary>
        /// <param name="expectedHealthStatus"></param>
        /// <returns></returns>
        [Test]
        [TestCase(EnumHealthStatus.Failed)]
        public async Task Read_NoDirectoryPermission_ReturnsFailure(EnumHealthStatus expectedHealthStatus)
        {
            // A. Setup
            CancellationToken cancellationToken = CancellationToken.None;
            string            dirName           = _uniqueKey.GetKey("Read." + expectedHealthStatus.ToString());

            HealthCheckerConfigFileSystem config = new()
            {
                CheckIsWriteable = false,
                CheckIsReadable  = true,
                FolderPath       = dirName,
            };


            MockFileSystem fileSystem1 = new();

            var mock = new Mock<IFileSystem>();
            mock.Setup(x => x.Directory.GetFiles(It.IsAny<string>())).Throws(new UnauthorizedAccessException("Permission Denied"));
            mock.Setup(x => x.Directory.Exists(It.IsAny<string>())).Returns(true);
            mock.Setup(x => x.Directory.CreateDirectory(It.IsAny<string>()));
            mock.Setup(x => x.File.Exists(Path.Join(config.FolderPath, config.ReadFileName))).Returns(false);

            //mock.Setup(x => x.Directory.Exists(dirName));
            mock.Setup(x => x.File.Open(config.ReadFileName, It.IsAny<FileMode>())).Throws(new UnauthorizedAccessException("Permission Denied"));

            IFileSystem fileSystem = mock.Object;

            //MockFileSystem fileSystem = new MockFileSystem();
            fileSystem.File.Create(config.ReadFileName);


            //			if (expectedHealthStatus == EnumHealthStatus.Healthy)
            fileSystem.Directory.CreateDirectory(dirName);

            if (fileSystem.Directory.Exists(dirName))
                Console.WriteLine("yes");
            /* This should work, but looks like System.IO.Abstractions does not have any Security stuff built in.
                        DirectorySecurity directorySecurity = fileSystem.Directory.GetAccessControl(dirName);
                        // Just for seeing what the rules are...
                        foreach (FileSystemAccessRule acr in directorySecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
                        {
                            Console.WriteLine("{0} | {1} | {2} | {3} | {4}", acr.IdentityReference.Value, acr.FileSystemRights, acr.InheritanceFlags, acr.PropagationFlags, acr.AccessControlType);
                            directorySecurity.RemoveAccessRule(acr);
                        }



                        // This does not work.
                        if ( expectedHealthStatus == EnumHealthStatus.Failed ) {
                            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                            directorySecurity.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.ReadAndExecute, AccessControlType.Deny));
                            directorySecurity.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Write, AccessControlType.Deny));
                        }
                        // Save all directory security changes
                        fileSystem.Directory.SetAccessControl(dirName, directorySecurity);
            */


            HealthCheckerFileSystem healthChecker = new(fileSystem,
                                                        _logger,
                                                        dirName,
                                                        config);


            // B. Test
            await healthChecker.CheckHealth(cancellationToken);


            // C. Validate

            // check the Status
            Assert.That(expectedHealthStatus == healthChecker.StatusRead, "A10");
            Assert.That(expectedHealthStatus == healthChecker.Status, "A20");
        }
    }
}
#pragma warning restore