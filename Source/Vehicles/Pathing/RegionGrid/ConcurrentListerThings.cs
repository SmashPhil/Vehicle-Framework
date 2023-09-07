using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public sealed class ConcurrentListerThings
	{
		public ListerThingsUse use;

		private ConcurrentDictionary<ThingDef, ConcurrentSet<Thing>> listsByDef = new ConcurrentDictionary<ThingDef, ConcurrentSet<Thing>>(ThingDefComparer.Instance);
		private ConcurrentSet<Thing>[] listsByGroup;
		
		private static readonly ConcurrentSet<Thing> emptySet = new ConcurrentSet<Thing>();

		public ConcurrentListerThings(ListerThingsUse use)
		{
			this.use = use;
			listsByGroup = new ConcurrentSet<Thing>[ThingListGroupHelper.AllGroups.Length];
			listsByGroup[2] = new ConcurrentSet<Thing>();
		}

		public IEnumerable<Thing> AllThings
		{
			get
			{
				return listsByGroup[2].Keys;
			}
		}

		public ConcurrentSet<Thing> ThingsInGroup(ThingRequestGroup group)
		{
			return ThingsMatching(ThingRequest.ForGroup(group));
		}

		public ConcurrentSet<Thing> ThingsOfDef(ThingDef def)
		{
			return ThingsMatching(ThingRequest.ForDef(def));
		}

		public ConcurrentSet<Thing> ThingsMatching(ThingRequest req)
		{
			if (req.singleDef != null)
			{
				if (!listsByDef.TryGetValue(req.singleDef, out ConcurrentSet<Thing> result))
				{
					return emptySet;
				}
				return result;
			}
			else
			{
				if (req.group == ThingRequestGroup.Undefined)
				{
					throw new InvalidOperationException("Invalid ThingRequest " + req);
				}
				if (use == ListerThingsUse.Region && !req.group.StoreInRegion())
				{
					Log.ErrorOnce("Tried to get things in group " + req.group + " in a region, but this group is never stored in regions. Most likely a global query should have been used.", 1968735132);
					return emptySet;
				}
				return listsByGroup[(int)req.group] ?? emptySet;
			}
		}

		public bool Contains(Thing t)
		{
			return AllThings.Contains(t);
		}

		public void Add(Thing t)
		{
			if (!EverListable(t.def, use))
			{
				return;
			}
			if (!listsByDef.TryGetValue(t.def, out ConcurrentSet<Thing> list))
			{
				list = new ConcurrentSet<Thing>();
				listsByDef.TryAdd(t.def, list);
			}
			list.Add(t);
			foreach (ThingRequestGroup thingRequestGroup in ThingListGroupHelper.AllGroups)
			{
				if ((use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(t.def))
				{
					ConcurrentSet<Thing> groupList = listsByGroup[(int)thingRequestGroup];
					if (groupList == null)
					{
						groupList = new ConcurrentSet<Thing>();
						listsByGroup[(int)thingRequestGroup] = groupList;
					}
					groupList.Add(t);
				}
			}
		}

		public void Remove(Thing t)
		{
			if (!EverListable(t.def, use))
			{
				return;
			}
			listsByDef[t.def].Remove(t);
			ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
			for (int i = 0; i < allGroups.Length; i++)
			{
				ThingRequestGroup thingRequestGroup = allGroups[i];
				if ((use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(t.def))
				{
					listsByGroup[i].Remove(t);
				}
			}
		}

		public static bool EverListable(ThingDef def, ListerThingsUse use)
		{
			return (def.category != ThingCategory.Mote || (def.drawGUIOverlay && use != ListerThingsUse.Region)) && (def.category != ThingCategory.Projectile || use != ListerThingsUse.Region);
		}

		public void Clear()
		{
			listsByDef.Clear();
			for (int i = 0; i < listsByGroup.Length; i++)
			{
				if (listsByGroup[i] != null)
				{
					listsByGroup[i].Clear();
				}
			}
		}
	}
}
