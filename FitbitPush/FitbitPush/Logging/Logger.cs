using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;

namespace FitbitPush.Logging
{
	public static class Logger
	{
		private static readonly AsyncLocal<TraceWriter> _log = new AsyncLocal<TraceWriter>();

		public static IDisposable Set(TraceWriter log)
		{
			_log.Value = log;

			return Disposable.Create(log.Flush);
		}

		public static ILogger Log(this object owner)
		{
			var trace = _log.Value;
			return trace == null
				? ConsoleLogger.Instance
				: new TraceWriterLogger(trace, owner.GetType().Name);
		}
	}
}