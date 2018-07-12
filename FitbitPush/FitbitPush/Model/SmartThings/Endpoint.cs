using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FitbitPush.Model.SmartThings
{
	public class Endpoint
	{
		public Location Location { get; set; }

		[JsonProperty("uri")]
		public string Uri { get; set; }

		[JsonProperty("base_url")]
		public string Base { get; set; }

		[JsonProperty("url")]
		public string InstallationPath { get; set; }
	}
}