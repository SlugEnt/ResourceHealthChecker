using System.IO.Abstractions;
using NUnit.Framework;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SlugEnt;
using SlugEnt.ResourceHealthChecker;

namespace HeachCheck_Test
{
	using XFS = MockUnixSupport;

	public class Tests {
		private UniqueKeys _uniqueKey;
		private ILogger<HealthCheckerFileSystem> _logger;


		[OneTimeSetUp]
		public void InitialSetUp () {
			_uniqueKey = new UniqueKeys(".");

			// The Health Checkers cannot create a logger themselves, so we create it for them.
			_logger = Mock.Of<ILogger<HealthCheckerFileSystem>>();
		}


		[SetUp]
		public void Setup () {
			
		}
		


		[Test]
		public async Task WriteBasicSuccess ()
		{
			// Setup
			CancellationToken cancellationToken = CancellationToken.None;
			MockFileSystem fileSystem = new MockFileSystem();

			string dirName = _uniqueKey.GetKey("Write");
			IDirectoryInfo directoryInfo = fileSystem.Directory.CreateDirectory(dirName);


			HealthCheckerConfigFileSystem config = new HealthCheckerConfigFileSystem()
			{
				CheckIsWriteble = true,
				CheckIsReadable = false,
				FolderPath = directoryInfo.FullName,
			};
			HealthCheckerFileSystem healthChecker = new HealthCheckerFileSystem(fileSystem, _logger, dirName, config);
			await healthChecker.CheckHealth(cancellationToken);

			// check the Status
			Assert.AreEqual(EnumHealthStatus.Healthy,healthChecker.StatusWrite, "A10");
		}
	}
}