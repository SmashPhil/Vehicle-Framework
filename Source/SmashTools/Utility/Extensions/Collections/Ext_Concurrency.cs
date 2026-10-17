using System.Collections.Concurrent;

namespace SmashTools
{
	public static class Ext_Concurrency
	{
		public static bool NullOrEmpty<T>(this ConcurrentBag<T> bag)
		{
			return bag is null || bag.Count == 0;
		}

		public static bool NullOrEmpty<T>(this ConcurrentSet<T> set)
		{
			return set is null || set.Count == 0;
		}
	}
}
