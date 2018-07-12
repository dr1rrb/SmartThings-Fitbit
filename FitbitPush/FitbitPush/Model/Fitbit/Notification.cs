using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FitbitPush.Model.Fitbit
{
	public class Notification
	{
		[JsonProperty("subscriptionId")]
		public string SubscriptionId { get; set; }

		[JsonProperty("collectionType")]
		public string CollectionType { get; set; }

		[JsonProperty("date")]
		public string Date { get; set; }

		[JsonProperty("ownerId")]
		public string OwnerId { get; set; }

		[JsonProperty("ownerType")]
		public string OwnerType { get; set; }
	}
}