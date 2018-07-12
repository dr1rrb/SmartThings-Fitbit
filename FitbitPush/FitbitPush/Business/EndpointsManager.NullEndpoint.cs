using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using FitbitPush.Logging;

namespace FitbitPush.Business
{
	public partial class EndpointsManager
	{
		private class NullEndpoint : IPushChannel
		{
			private readonly string _locationId;
			private readonly IScheduler _timeProvider;
			private readonly DateTimeOffset _createdOn;

			public NullEndpoint(string locationId, IScheduler timeProvider)
			{
				_locationId = locationId;
				_timeProvider = timeProvider;

				_createdOn = timeProvider.Now;
			}

			public bool ShouldBeRevalidated() 
				=> _createdOn + Constants.InvalidEndpointDuration < _timeProvider.Now;

			public async Task Send<T>(CancellationToken ct, T notification) 
				=> Logger.Log(this).Info("Cannot send notification since the channel is currently invalid.");
		}
	}
}