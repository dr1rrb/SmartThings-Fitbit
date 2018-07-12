using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FitbitPush.Business;
using FitbitPush.Model.Fitbit;
using FitbitPush.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace FitbitPush
{
	public static class Push
	{
		private static ImmutableDictionary<string, EndpointsManager> _endpointsCache = ImmutableDictionary<string, EndpointsManager>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

		[FunctionName("Push")]
		public static async Task<HttpResponseMessage> Run(
			[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "push")]
			HttpRequestMessage req,
			TraceWriter log, 
			CancellationToken ct)
		{
			try
			{
				var verifyCode = req
					.GetQueryNameValuePairs()
					.FirstOrDefault(q => q.Key.Equals("verify", StringComparison.Ordinal))
					.Value;

				if (verifyCode.HasValue())
				{
					return verifyCode.Equals(Constants.FitbitVerificationCode, StringComparison.Ordinal)
						? new HttpResponseMessage(HttpStatusCode.NoContent)
						: new HttpResponseMessage(HttpStatusCode.NotFound);
				}

				var notificationsContent = await req.Content.ReadAsStringAsync();
				var notifications = JsonConvert.DeserializeObject<Notification[]>(notificationsContent);
				var notificationsForwarding = notifications
					.GroupBy(notif => notif.SubscriptionId?.Substring(1), StringComparer.Ordinal)
					.Select(notifs =>
					{
						string accessToken, locationId;
						var parts = notifs.Key?.Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
						if (parts.Length == 2)
						{
							try
							{
								accessToken = IdEncoder.Decode(parts[0]).ToString("D");
								locationId = IdEncoder.Decode(parts[1]).ToString("D");
							}
							catch (Exception e)
							{
								log.Error($"Invalid subscription id: {notifs.Key}", e);
								accessToken = locationId = null;
							}
						}
						else
						{
							log.Error("Invalid number of parts, don't forward the notifications.");
							accessToken = locationId = null;
						}


						return new { accessToken, locationId, notifs };
					})
					.Where(x => x.accessToken.HasValue() && x.locationId.HasValue())
					.Select(x => Send(ct, log, x.accessToken, x.locationId, x.notifs));

				// TODO: Run async
				await Task.WhenAll(notificationsForwarding);

				return req.CreateResponse(HttpStatusCode.NoContent);
			}
			catch (Exception e)
			{
				log.Error("Failed", e);

				throw;
			}
		}

		private static async Task Send(CancellationToken ct, TraceWriter log, string token, string location, IEnumerable<Notification> notifications)
		{
			var endpoints = ImmutableInterlocked.GetOrAdd(ref _endpointsCache, token, t => new EndpointsManager(t, Scheduler.Default));
			var endpoint = await endpoints.FindEndpoint(ct, location);

			if (endpoint == null)
			{
				log.Error($"No endpoint found for location '{location}' using token '{token}'.");
			}
			else
			{
				await endpoint.Send(ct, notifications);
			}
		}
	}
}
