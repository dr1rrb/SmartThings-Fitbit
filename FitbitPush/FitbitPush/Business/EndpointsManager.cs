using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using FitbitPush.Model.SmartThings;
using Newtonsoft.Json;
using AsyncLock = FitbitPush.Utils.AsyncLock;

namespace FitbitPush.Business
{
	public partial class EndpointsManager : IDisposable
	{
		private const int _maxErrorCount = 5;

		private readonly AsyncLock _endpointsGate = new AsyncLock();
		public readonly CancellationDisposable _token = new CancellationDisposable();
		private readonly HttpClient _client;
		private readonly IScheduler _timeProvider;

		private ImmutableDictionary<string, IPushChannel> _endpoints = ImmutableDictionary<string, IPushChannel>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);
		private int _errorCount = 0;

		public EndpointsManager(string authToken, IScheduler timeProvider)
		{
			_timeProvider = timeProvider;
			_client = new HttpClient
			{
				DefaultRequestHeaders =
				{
					{"Authorization", $"Bearer {authToken}"}
				}
			};
		}

		public async Task<IPushChannel> FindEndpoint(CancellationToken ct, string locationId)
		{
			var endpoints = _endpoints;
			if (endpoints == null)
			{
				// endpoints was erased, we are in an invalid state (too much exceptions)
				return null;
			}

			if (endpoints.TryGetValue(locationId, out var endpoint)
				&& IsValid(endpoint))
			{
				return endpoint;
			}

			ct = _token.Token; // Do not abort a load operation (and keep the lock locked)

			using (await _endpointsGate.LockAsync(ct))
			{
				if (endpoints.TryGetValue(locationId, out endpoint)
					&& IsValid(endpoint))
				{
					return endpoint;
				}

				endpoints = _endpoints = await LoadEndpoints(ct);

				if (endpoints.TryGetValue(locationId, out endpoint)) // No needs to check if 'endpoint' is valid here
				{
					return endpoint;
				}
				else
				{
					endpoint = new NullEndpoint(locationId, _timeProvider);
					_endpoints = endpoints.Add(locationId, endpoint);

					return endpoint;
				}
			}
		}

		private bool IsValid(IPushChannel channel) 
			=> !(channel is NullEndpoint nullEndpoint) || !nullEndpoint.ShouldBeRevalidated();

		private async Task<ImmutableDictionary<string, IPushChannel>> LoadEndpoints(CancellationToken ct)
		{
			var uri = "https://graph.api.smartthings.com/api/smartapps/endpoints";
			var response = await _client.GetAsync(new Uri(uri), ct);

			if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				return null;
			}
			else if (!response.IsSuccessStatusCode && Interlocked.Increment(ref _errorCount) >= _maxErrorCount)
			{
				return null;
			}

			var data = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
			var endpoints = JsonConvert.DeserializeObject<Endpoint[]>(data);

			if (!endpoints.Any())
			{
				return null;
			}

			_errorCount = 0;
			return endpoints.ToImmutableDictionary(e => e.Location.Id, e => new EndpointHandler(e, _client) as IPushChannel, StringComparer.OrdinalIgnoreCase);
		}

		public void Dispose() 
			=> _token.Dispose();
	}
}