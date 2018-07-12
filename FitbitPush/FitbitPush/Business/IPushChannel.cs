using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FitbitPush.Business
{
	public interface IPushChannel
	{
		Task Send<T>(CancellationToken ct, T notifications);
	}
}