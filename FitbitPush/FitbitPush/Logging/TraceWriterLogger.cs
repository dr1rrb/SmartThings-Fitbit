using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;

namespace FitbitPush.Logging
{
	public sealed class TraceWriterLogger : ILogger
	{
		private readonly TraceWriter _trace;
		private readonly string _source;

		public TraceWriterLogger(TraceWriter trace, string source)
		{
			_trace = trace;
			_source = source;
		}

		public void Debug(string message) => _trace.Verbose(message, _source);
		public void Info(string message) => _trace.Info(message, _source);
		public void Warning(string message) => _trace.Warning(message, _source);
		public void Error(string message, Exception ex = null) => _trace.Error(message, ex, _source);
	}
}