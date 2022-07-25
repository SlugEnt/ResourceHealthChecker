using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace SlugEnt.ResourceHealthChecker.RabbitMQ {
	/// <summary>
	/// Provides Health Checking on a RabbitMQ instance
	/// </summary>
	public class HealthCheckerRabbitMQ : AbstractHealthChecker {

		public HealthCheckerRabbitMQ (ILogger<HealthCheckerRabbitMQ> logger, string descriptiveName, HealthCheckerConfigRabbitMQ mqConfig) : base(
			descriptiveName, EnumHealthCheckerType.RabbitMQ, mqConfig, logger) {

			_logger = logger;
			CheckerName = "Rabbit MQ Health Checker";
			Config = mqConfig;

			try {
				if ( MQConfig.URL != string.Empty )
					DeConstructURL();
				else
					ConstructURL();

				IsReady = true;
			}
			catch ( Exception ex ) {
				_logger.LogCritical("Health Checker {HealthChecker} constructor encountered configuration issues.  Health Check will not run.  Error: {ErrorMsg}" , ShortTitle, ex.Message);

			}

		}



		/// <summary>
		/// Builds the URL from the Config Components
		/// </summary>
		protected void ConstructURL () {
			string errorMsg = "RabbitMQ Health Check config is invalid.  ";

			string url;

			if ( MQConfig.IsEncrypted )
				url = "amqps://";
			else
				url = "amqp://";

			MQConfig.URL = url + MQConfig.User + ":" + MQConfig.Password + "@" + MQConfig.ServerIP + "/" + MQConfig.Instance;

			// Validate all component parts received.
			if ( MQConfig.User == string.Empty ) throw new ArgumentException(errorMsg + "No Username provided in the config");
			if (MQConfig.Password == string.Empty) throw new ArgumentException(errorMsg + "No Password provided in the config");
			if (MQConfig.ServerIP == string.Empty) throw new ArgumentException(errorMsg + "No Server IP provided in the config");
			if (MQConfig.Instance == string.Empty) throw new ArgumentException(errorMsg + "No Instance name provided in the config");
		}



		/// <summary>
		/// Takes the URL and converts it into its component parts.
		/// </summary>
		/// <exception cref="ArgumentException"></exception>
		protected void DeConstructURL () {
			string errMsgStart = "The Rabbit MQ URL provided is in an inproper format.  ";

			// IF URL provided break it down into component parts
			string urlLower = MQConfig.URL.ToLower();
			if (urlLower.StartsWith("amqp")) MQConfig.IsEncrypted = false;
			else if (urlLower.StartsWith("amqps"))
				MQConfig.IsEncrypted = true;
			else
				throw new ArgumentException(
					"The Rabbit MQ URL provided is in an inproper format.  Could not find the amqp or amqps keyword at start of URL");

			int index;
			if (MQConfig.IsEncrypted)
				index = 8;
			else
				index = 7;

			// Determine the username
			string urlString = MQConfig.URL [index..];
			int indexUser = urlString.IndexOf(':');
			if ( indexUser < 0 ) throw new ArgumentException(errMsgStart + "Could not find username - missing colon");
			MQConfig.User = urlString [0..indexUser];

			urlString = urlString [++indexUser..];
			int indexPassword = urlString.IndexOf('@');
			if (indexPassword < 0) throw new ArgumentException(errMsgStart + "Could not find password - missing @");
			MQConfig.Password = urlString [0..indexPassword];

			urlString = urlString [++indexPassword..];
			int indexServer = urlString.IndexOf('/');
			if (indexServer < 0) throw new ArgumentException(errMsgStart + "Could not find Server - missing slash ");
			MQConfig.ServerIP = urlString [..indexServer];
			
			
			MQConfig.Instance = urlString [++indexServer..];


		}

		/// <summary>
		/// The configuration for this Health Checker.
		/// </summary>
		public HealthCheckerConfigRabbitMQ MQConfig {
			get { return (HealthCheckerConfigRabbitMQ)this.Config; }
		}


		/// <summary>
		/// Displays as HTML
		/// </summary>
		/// <param name="sb"></param>
		public override void DisplayHTML (StringBuilder sb) {
			sb.Append("<p>  MQ Instance: " + MQConfig.Instance + "</p>");
			sb.Append("<p>  Server:  " + MQConfig.ServerIP + "</p>");

			sb.Append("<h4>  Health Checks</h4>");

		}


		/// <summary>
		/// Displays the MQ information
		/// </summary>
		public override string FullTitle
		{
			get
			{
				return ShortTitle + "  -->  " + MQConfig.Instance;
			}
		}



		/// <summary>
		/// Check to ensure Connection to MQ is possible.
		/// </summary>
		/// <param name="stoppingToken"></param>
		/// <returns></returns>
		protected override async Task<(EnumHealthStatus, string)> PerformHealthCheck (CancellationToken stoppingToken) {
			try {
				var factory = new ConnectionFactory
				{
					Uri = new Uri(MQConfig.URL),
				};

				using var connection = factory.CreateConnection();
				using var channel = connection.CreateModel();
			}
			catch ( Exception ex ) {
				return (EnumHealthStatus.Failed, ex.Message);
			}

			return (EnumHealthStatus.Healthy,"");
		}
	}
}