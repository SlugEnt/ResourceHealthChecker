
namespace SlugEnt.ResourceHealthChecker.RabbitMQ;

	/// <summary>
	/// Stores the configuration information for connecting to an MQ instance.  It will accept a full URL OR the individual components and build the url from them.
	/// </summary>
	public class HealthCheckerConfigRabbitMQ : HealthCheckConfigBase, IConfigHealthChecksConfig {
		/// <summary>
		/// The full URL to connect to.  The engine uses the URL OR the user, password, server and instance to build a URL
		/// </summary>
		public string URL { get; set; } = "";

		/// <summary>
		/// UserName to connect as
		/// </summary>
		public string User { get; set; } = "";

		/// <summary>
		/// Password of User
		/// </summary>
		public string Password { get; set; } = "";

		/// <summary>
		/// Server IP or Hostname
		/// </summary>
		public string ServerIP { get; set; } = "";

		/// <summary>
		/// The instance of MQ on the server
		/// </summary>
		public string Instance { get; set; } = "";

		/// <summary>
		/// If true will use an AMQPS connection otherwise an unencrypted AMQP connection.  
		/// </summary>
		public bool IsEncrypted { get; set; }

	}


