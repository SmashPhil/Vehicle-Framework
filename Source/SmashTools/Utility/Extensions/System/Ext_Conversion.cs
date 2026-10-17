using System;
using System.Collections.Generic;
using Verse;

namespace SmashTools
{
	public static class Ext_Conversion
	{
		public static Pair<T1, T2> ToPair<T1, T2>(this Tuple<T1, T2> tuple)
		{
			return new Pair<T1, T2>(tuple.Item1, tuple.Item2);
		}

		public static Tuple<T1, T2> ToTuple<T1, T2>(this Pair<T1, T2> pair)
		{
			return new Tuple<T1, T2>(pair.First, pair.Second);
		}
	}
}
