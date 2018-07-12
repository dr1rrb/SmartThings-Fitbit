using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FitbitPush.Utils
{
	/// <summary>
	/// An asynchronous lock, that can be used in conjuction with C# async/await
	/// </summary>
	public sealed class AsyncLock
	{
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		/// <summary>
		/// Acquires the lock, then provides a disposable to release it.
		/// </summary>
		/// <param name="ct">A cancellation token to cancel the lock</param>
		/// <returns>An IDisposable instance that allows the release of the lock.</returns>
		public async Task<IDisposable> LockAsync(CancellationToken ct)
		{
			await _semaphore.WaitAsync(ct);

			return new Handle(_semaphore);
		}

		private class Handle : IDisposable
		{
			private SemaphoreSlim _semaphore;

			public Handle(SemaphoreSlim semaphore)
			{
				_semaphore = semaphore;
			}

			public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
		}
	}
}