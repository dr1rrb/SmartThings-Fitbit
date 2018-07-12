using System;
using System.Linq;
using System.Threading.Tasks;

namespace FitbitPush.Utils
{
	public static class Extensions
	{
		public static bool IsEmpty(this string value)
			=> string.IsNullOrWhiteSpace(value);

		public static bool HasValue(this string value)
			=> !string.IsNullOrWhiteSpace(value);
	}
}