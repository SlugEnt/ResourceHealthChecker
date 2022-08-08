using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResourceHealthChecker;

namespace SlugEnt.ResourceHealthChecker
{
	/// <summary>
	/// Performs a Health Check on a File System
	/// </summary>
	public class HealthCheckerFileSystem : AbstractHealthChecker, IFileSystemHealthChecker, IHealthChecker
	{
		private readonly IFileSystem _fileSystem;	
		private EnumHealthStatus                         _statusRead    = EnumHealthStatus.Failed;
		private EnumHealthStatus                         _statusWrite   = EnumHealthStatus.Failed;
		private EnumHealthStatus                         _statusOverall = EnumHealthStatus.Failed;


		/// <summary>
		/// Constructor used when building object from Configuration
		/// </summary>
		/// <param name="logger"></param>
		public HealthCheckerFileSystem (ILogger<HealthCheckerFileSystem> logger) : base(logger) {
			_fileSystem = new FileSystem();
			CheckerName = "File System Permissions Checker";
			Config = new HealthCheckerConfigFileSystem();
		}

		/// <summary>
		/// Constructs a new File System Health Checker.
		/// </summary>
		/// <param name="fileSystem">If mocking the file system, then this is the mock File System, otherwise it is the real file system</param>
		/// <param name="logger">Where logs are sent</param>
		/// <param name="descriptiveName">Name for this File System Check - usually indicates what is being checked, for instance, Web Downloads</param>
		/// <param name="config">The Health checker Config for the file System Check.</param>
		public HealthCheckerFileSystem (IFileSystem fileSystem, ILogger<HealthCheckerFileSystem> logger, string descriptiveName, HealthCheckerConfigFileSystem config) : base (descriptiveName,EnumHealthCheckerType.FileSystem, config, logger) {
			_fileSystem = fileSystem;
			CheckerName = "File System Permissions Checker";
			IsReady = true;
		}


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="descriptiveName"></param>
		/// <param name="config"></param>
		public HealthCheckerFileSystem(ILogger<HealthCheckerFileSystem> logger, string descriptiveName, HealthCheckerConfigFileSystem config) : this (new FileSystem(),logger,descriptiveName, config)
		{
		}
		
		
		/// <summary>
		/// The Status of the Reach Check
		/// </summary>
		public EnumHealthStatus StatusRead {
			get { return _statusRead; }
			private set { _statusRead = value; }
		}


		/// <summary>
		/// The Status of the Write Check
		/// </summary>
		public EnumHealthStatus StatusWrite
		{
			get { return _statusWrite; }
			private set { _statusWrite = value; }
		}


		/// <summary>
		/// Displays the File Path information
		/// </summary>
		public override string FullTitle {
			get {
				string access = "";
				if ( FileSystemConfig.CheckIsReadable ) access = "Read";
				if ( FileSystemConfig.CheckIsWriteable ) access += "Write";

				return access + " | " + ShortTitle + "  -->  " + FileSystemConfig.FolderPath;
			}
		}


		/// <summary>
		///  A synonym of the abstract classes Config property.
		/// </summary>
		public HealthCheckerConfigFileSystem FileSystemConfig {
			get { return (HealthCheckerConfigFileSystem) this.Config; }
		}


		/// <summary>
		/// The output of this Checker in HTML Format
		/// </summary>
		/// <param name="sb"></param>
		public override void DisplayHTML(StringBuilder sb) {
			sb.Append("<p>FilePath: " + FileSystemConfig.FolderPath + "</p>");

			sb.Append("<p>Is Readable Check: ");
			if ( FileSystemConfig.CheckIsReadable )
				sb.Append(_statusRead.ToString());
			else
				sb.Append(" Not Requested");
			sb.Append("</p>");

			sb.Append("<p>Is Writeable Check: ");
			if (FileSystemConfig.CheckIsWriteable)
				sb.Append(_statusWrite.ToString());
			else
				sb.Append(" Not Requested");
			sb.Append("</p>");

		}



		/// <summary>
		/// This is the Write Folder Status Check 
		/// </summary>
		/// <param name="checkRead"></param>
		/// <returns></returns>
		private (EnumHealthStatus, string) WriteFileTest (bool checkRead = false) {
			bool fileWritten = false;
			string fullPath = "";
			string message = "";
			EnumHealthStatus status;

			try {
				// Try to create a file.
				string fileName = "HealthChecker_" + Guid.NewGuid();
				fullPath = Path.Join(FileSystemConfig.FolderPath, fileName);
				_fileSystem.File.WriteAllText(fullPath, "Testing");
				fileWritten = true;

				// If checkread is set, we will attempt to read the file.
				if ( checkRead ) {
					_fileSystem.File.ReadAllBytes(fullPath);
				}

				_fileSystem.File.Delete(fullPath);
				status = EnumHealthStatus.Healthy;
			}
			catch ( Exception ex ) {
				if ( fileWritten ) {
					_logger.LogError("File Written, but unable to be deleted  [ {FilePath} ]" , fullPath);
					status = EnumHealthStatus.Degraded;
					message = "Able to Write to folder, but not delete.  Error:" + ex.Message;
				}
				else {
					status = EnumHealthStatus.Failed;
					message = "Unable to write to folder.  Error: " + ex.Message;
				}
			}
			return (status, message); 
		}



		/// <summary>
		/// Performs a file system health Check
		/// </summary>
		/// <returns></returns>
#pragma		warning disable CS1998
		protected override async Task<(EnumHealthStatus, string)> PerformHealthCheck(CancellationToken stoppingToken)
		{
			// Go to the folder and attempt to perform actions
			string message = "";


			// Write File
			if ( FileSystemConfig.CheckIsWriteable ) {
				(_statusWrite, message) = WriteFileTest();
			}
			else
				_statusWrite = EnumHealthStatus.NotRequested;

			
			if ( FileSystemConfig.CheckIsReadable ) {
				try {
					if ( _fileSystem.Directory.Exists(FileSystemConfig.FolderPath) ) {
						// Look For readfile.  If it exists, read 1st byte.  Otherwise try to read first byte from any file.
						string fileName = Path.Join(FileSystemConfig.FolderPath, FileSystemConfig.ReadFileName);
						if ( !_fileSystem.File.Exists(fileName) ) {
							// Find a file if we can
							string [] files = _fileSystem.Directory.GetFiles(FileSystemConfig.FolderPath);
							if ( files.Length > 0 )
								fileName = files [0];
							else {
								fileName = string.Empty;
							}
						}

						// Read a byte from file
						if ( fileName != string.Empty ) {
							using ( Stream fs = (Stream)_fileSystem.FileStream.Create(fileName, FileMode.Open) )
								for ( int i = 0; i < 1; i++ )
									fs.ReadByte();
							_statusRead = EnumHealthStatus.Healthy;
						}

						// Since we cannot determine if we actually have Read access we return Degraded with message
						else {
							_statusRead = EnumHealthStatus.Degraded;
							message = "No file found to read.  Cannot firmly determine if Read permission to files is enabled. See documentation for more info";
						}
						
					}
					else {
						message = "Unable to locate folder to check";
						_statusRead = EnumHealthStatus.Failed;
					}
				}
				catch ( Exception ex ) {
					_statusRead = EnumHealthStatus.Failed;
					message = ex.Message;
				}
			}
			else
				_statusRead = EnumHealthStatus.NotRequested;


			if ( _statusWrite > EnumHealthStatus.Healthy || _statusRead > EnumHealthStatus.Healthy ) {
				if ( _statusRead > EnumHealthStatus.Healthy ) _statusOverall = _statusRead;
				if ( _statusWrite > _statusOverall ) _statusOverall = _statusWrite;
			}
			else { _statusOverall = EnumHealthStatus.Healthy;}


			return (_statusOverall, message);
		}


		/// <summary>
		/// Reads the common properties of Config items for the ConfigHealthChecks configuration objects
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="configurationSectionRoot"></param>
		public override void SetupFromConfig (IConfiguration configuration, string configurationSectionRoot) {
			base.SetupFromConfig(configuration, configurationSectionRoot);

			// Read the File Specific config
			this.FileSystemConfig.FolderPath = configuration.GetSection(configurationSectionRoot + ":Config:FolderPath").Get<string>();
			this.FileSystemConfig.ReadFileName = configuration.GetSection(configurationSectionRoot + ":Config:ReadFileName").Get<string>();
			this.FileSystemConfig.CheckIsReadable = configuration.GetSection(configurationSectionRoot + ":Config:CheckIsReadable").Get<bool>();
			this.FileSystemConfig.CheckIsWriteable = configuration.GetSection(configurationSectionRoot + ":Config:CheckIsWriteable").Get<bool>();
			IsReady = true;
		}
#pragma warning restore
	}

}
