using System;
using System.Linq;
using System.Threading.Tasks;

namespace FitbitPush
{
	public static class Constants
	{
		/// <summary>
		/// The fitbit's verification code
		/// </summary>
		public const string FitbitVerificationCode = "48ce5226c355d35dfbe5bfc63b6e5b7e53c4d29aa05e7f775300d0aab4d2eea3";

		/// <summary>
		/// The delay at which an invalid enpoint should be revalidated (usually a missing endpoint for a given location)
		/// </summary>
		public static TimeSpan InvalidEndpointDuration { get; } = TimeSpan.FromDays(1);
	}
}