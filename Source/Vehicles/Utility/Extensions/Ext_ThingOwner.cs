using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public static class Ext_ThingOwner
	{
		/// <summary>
		/// Uncached Count check with conditional predicate
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="predicate"></param>
		public static int CountWhere<T>(this ThingOwner<T> list, Predicate<T> predicate) where T : Thing
		{
			int count = 0;
			foreach (T item in list)
			{
				if (predicate(item)) { count++; }
			}
			return count;
		}
	}
}
