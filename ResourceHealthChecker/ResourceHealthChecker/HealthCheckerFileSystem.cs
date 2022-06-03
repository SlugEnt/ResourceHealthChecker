using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlugEnt.ResourceHealthChecker
{
	/// <summary>
	/// Performs a Health Check on a File System
	/// </summary>
	public class HealthCheckerFileSystem : AbstractHealthChecker
	{
		private ILogger<HealthCheckerFileSystem> _logger;
		private EnumHealthStatus                         _statusRead    = EnumHealthStatus.Failed;
		private EnumHealthStatus                         _statusWrite   = EnumHealthStatus.Failed;
		private EnumHealthStatus                         _statusOverall = EnumHealthStatus.Failed;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="descriptiveName">A Descriptive name for this checker</param>
		/// <param name="path">Fully Qualified file system path to check</param>
		/// <param name="checkIsReadable">If checker should check for Reading permission</param>
		/// <param name="checkisWriteable">If checker should check for Writing permission</param>
		public HealthCheckerFileSystem (ILogger<HealthCheckerFileSystem> logger,string descriptiveName, string path, bool checkIsReadable = true, bool checkisWriteable = true) : base(descriptiveName, EnumHealthCheckerType.FileSystem,
		                                                                                              new HealthCheckerConfigFileSystem()) {
			HealthCheckerConfigFileSystem Config = new HealthCheckerConfigFileSystem();
			FileSystemConfig.CheckIsReadable = checkIsReadable;
			FileSystemConfig.CheckIsWriteble = checkisWriteable;
			FileSystemConfig.FolderPath = path;
			_logger = logger;
			CheckerName = "File System Permissions Checker";
			_logger.LogDebug("Health Checker File System Object Constructed:  [" + descriptiveName + "]  Path: [" + path + "]");
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

			string message;

			sb.Append("<p>Is Readable Check: ");
			if ( FileSystemConfig.CheckIsReadable )
				sb.Append(_statusRead.ToString());
			else
				sb.Append(" Not Requested");
			sb.Append("</p>");

			sb.Append("<p>Is Writeable Check: ");
			if (FileSystemConfig.CheckIsWriteble)
				sb.Append(_statusWrite.ToString());
			else
				sb.Append(" Not Requested");
			sb.Append("</p>");

		}


		/// <summary>
		/// Performs a file system health Check
		/// </summary>
		/// <returns></returns>
		protected override async Task<(EnumHealthStatus, string)> PerformHealthCheck()
		{
			// Go to the folder and attempt to perform actions
			string message = "";


			if ( FileSystemConfig.CheckIsWriteble ) {
				bool fileWritten = false;
				string fullPath = "";
				try {
					// Try to create a file.
					string fileName = "HealthChecker_" + new Guid().ToString();
					fullPath = Path.Join(FileSystemConfig.FolderPath, fileName);
					File.WriteAllText(fullPath, "Testing");
					fileWritten = true;
					File.Delete(fullPath);
					_statusWrite = EnumHealthStatus.Healthy;
				}
				catch ( Exception ex ) {
					if ( fileWritten ) {
						_logger.LogError("File Written, but unable to be deleted - " + fullPath);
						_statusWrite = EnumHealthStatus.Degraded;
						message = "Able to Write to folder, but not delete";
					}
					else {
						_statusWrite = EnumHealthStatus.Failed;
						message = "Unable to write to folder";
					}
				}
			}
			else
				_statusWrite = EnumHealthStatus.NotRequested;


			if ( FileSystemConfig.CheckIsReadable ) {
				if ( Directory.Exists(FileSystemConfig.FolderPath) )
					_statusRead = EnumHealthStatus.Healthy;
				else {
					message = "Unable to locate folder to check";
					_statusRead = EnumHealthStatus.Failed;
				}
			}
			else
				_statusRead = EnumHealthStatus.NotRequested;


			if ( _statusWrite > EnumHealthStatus.Healthy || _statusRead > EnumHealthStatus.Healthy ) {
				if ( _statusRead > EnumHealthStatus.Healthy ) _statusOverall = _statusRead;
				if ( _statusWrite > _statusOverall ) _statusOverall = _statusWrite;
			}
			else { _statusOverall = EnumHealthStatus.Healthy;}


			_logger.LogDebug("HealthCheckerConfigFile [ " + Name + " ] Status: " + _statusOverall);
			return (_statusOverall, message);
		}
	}
}
