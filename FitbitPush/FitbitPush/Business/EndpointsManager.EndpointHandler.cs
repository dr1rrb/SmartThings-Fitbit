using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FitbitPush.Logging;
using FitbitPush.Model.SmartThings;
using Newtonsoft.Json;

namespace FitbitPush.Business
{
	public partial class EndpointsManager
	{
		private class EndpointHandler : IPushChannel
		{
			private readonly Uri _uri;
			private readonly HttpClient _client;

			public EndpointHandler(Endpoint endpoint, HttpClient client)
			{
				_uri = new Uri(Path.Combine(endpoint.Uri, "push"));
				_client = client;

				CreationDate = DateTimeOffset.Now;
			}

			public DateTimeOffset CreationDate { get; }

			public async Task Send<T>(CancellationToken ct, T notification)
			{
				try
				{
					await _client.PostAsync(_uri, new StringContent(JsonConvert.SerializeObject(notification), Encoding.UTF8, "application/json"), ct);
				}
				catch (Exception e)
				{
					this.Log().Error("Failed to forward notification to smartthinhgs", e);
				}
			}
		}
	}
}