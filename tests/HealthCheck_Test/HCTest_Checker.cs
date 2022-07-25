using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlugEnt.ResourceHealthChecker;

namespace HealthCheck_Test
{
	public  class HCTest_Checker : AbstractHealthChecker
	{
		public  HCTest_Checker (ILogger<HCTest_Checker> logger, string name, HCTest_Config config) : base(name, EnumHealthCheckerType.Dummy, config, logger)
		{
		
			CheckerName = "Dummy Checker";
			IsReady = true;
		}

		public override void DisplayHTML(StringBuilder sb) {
			sb.Append("<p>HCTest_Checker</p>");
		}

		/// <summary>
		/// Displays the Full information
		/// </summary>
		public override string FullTitle
		{
			get
			{
				string access = "";
				

				return access + " | " + ShortTitle + "  -->  " + ((HCTest_Config) Config).ExpectedMessageOutput;
				
			}
		}



		protected override async Task<(EnumHealthStatus, string)> PerformHealthCheck(CancellationToken stoppingToken) {
			HCTest_Config hcConfig = (HCTest_Config)Config;

			if ( hcConfig.RunDelay > 0 ) await Task.Delay(hcConfig.RunDelay,stoppingToken);


			// We use this to force different Status return codes
			if ( hcConfig.RunNumberResult > 0 ) {
				EnumHealthStatus newStatus = (EnumHealthStatus)hcConfig.RunNumberResult;
				return (newStatus, newStatus.ToString());
			}
			

			if ( hcConfig.ExpectedOutput != EnumHealthStatus.NotCheckedYet && hcConfig.ExpectedOutput != EnumHealthStatus.Unknown)
				return (hcConfig.ExpectedOutput, hcConfig.ExpectedMessageOutput);
			else
				return (EnumHealthStatus.Healthy, hcConfig.ExpectedMessageOutput);
		}
	}
}
