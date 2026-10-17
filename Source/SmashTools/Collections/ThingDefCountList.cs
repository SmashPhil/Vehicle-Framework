using System.Collections;
using System.Collections.Generic;
using CoreLib.Performance;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace SmashTools;

/// <summary>
/// Wrapper class for list of <see cref="ThingDefCountClass"/>, often used for recipes or jobs.
/// </summary>
public sealed class ThingDefCountList : IPoolable, IEnumerable<ThingDefCountClass>
{
	private readonly List<ThingDefCountClass> items = [];

	private ThingDefCountClass lastUsedCountClass;

	bool IPoolable.InPool { get; set; }

	public int Count => items.Count;

	public List<ThingDefCountClass> InnerListForReading => items;

	[MustUseReturnValue]
	public ThingDefCountClass Find(ThingDef thingDef)
	{
		if (lastUsedCountClass?.thingDef == thingDef)
			return lastUsedCountClass;

		foreach (ThingDefCountClass countClass in items)
		{
			if (countClass.thingDef == thingDef)
			{
				lastUsedCountClass = countClass;
				return countClass;
			}
		}
		return null;
	}

	public void Add(ThingDefCountClass countClass)
	{
		lastUsedCountClass = countClass;
		items.Add(countClass);
	}

	public void Reset()
	{
		foreach (ThingDefCountClass thingDefCount in items)
		{
			thingDefCount.thingDef = null;
			thingDefCount.stuff = null;
			thingDefCount.count = 0;
			thingDefCount.color = null;
			thingDefCount.chance = null;
			thingDefCount.quality = QualityCategory.Awful;
			SimplePool<ThingDefCountClass>.Return(thingDefCount);
		}

		lastUsedCountClass = null;
		items.Clear();
	}

	public List<ThingDefCountClass>.Enumerator GetEnumerator()
	{
		return items.GetEnumerator();
	}

	IEnumerator<ThingDefCountClass> IEnumerable<ThingDefCountClass>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}