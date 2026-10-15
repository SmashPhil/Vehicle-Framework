using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

public static class Ext_IList
{
	/// <summary>
	/// Populate array with <paramref name="value"/> up to <paramref name="count"/> times.
	/// </summary>
	/// <remarks>Overwrites existing values, as list may or may not be pre-filled and of a smaller size than <paramref name="count"/></remarks>
	public static void Populate<T>(this IList<T> list, T value, int count)
	{
		list.Clear();
		for (int i = 0; i < count; ++i)
		{
			list.Add(value);
		}
	}

	/// <summary>
	/// Pop random value from List
	/// </summary>
	public static T PopRandom<T>(this IList<T> list)
	{
		if (!list.NotNullAndAny())
		{
			return default;
		}
		Rand.PushState();
		int index = Rand.Range(0, list.Count);
		T item = list.PopAt(index);
		Rand.PopState();
		return item;
	}

	/// <summary>
	/// Pop element at <paramref name="index"/>
	/// </summary>
	public static T PopAt<T>(this IList<T> list, int index)
	{
		T r = list[index];
		list.RemoveAt(index);
		return r;
	}

	/// <summary>
	/// Grabs the next item in the list, given a reference item as the current position
	/// </summary>
	public static T Next<T>(this IList<T> list, T current)
	{
		int indexOf = list.IndexOf(current) + 1;
		if (indexOf >= list.Count)
		{
			indexOf = 0;
		}
		return list[indexOf];
	}

	/// <summary>
	/// Split list on item T and shift to front
	/// </summary>
	public static List<T> ReorderOn<T>(this List<T> list, T item)
	{
		int index = list.IndexOf(item);
		if (index < 0)
		{
			Log.Error($"Unable to ReorderOn list, item does not exist.");
			return list;
		}
		var frontList = list.GetRange(0, index);
		var backList = list.GetRange(index + 1, list.Count - 1);
		var newList = new List<T>(backList);
		newList.AddRange(frontList);
		return newList;
	}

	/// <summary>
	/// Check if index is within bounds of list
	/// </summary>
	public static bool OutOfBounds<T>(this IList<T> list, int index)
	{
		return index < 0 || index >= list.Count;
	}

	[ContractAnnotation("list:null => false;")]
	public static bool NotNullAndAny<T>(this List<T> list, Predicate<T> predicate = null)
	{
		if (list == null)
			return false;
		return predicate is null ? list.Count > 0 : list.Exists(predicate);
	}
}