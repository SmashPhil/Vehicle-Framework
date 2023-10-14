using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class Airdrop : Building, IThingHolder
	{
		private static readonly List<Thing> tmpInventoryDropper = new List<Thing>();

		public ThingOwner<Thing> innerContainer = new ThingOwner<Thing>();

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return innerContainer;
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			tmpInventoryDropper.AddRange(innerContainer);
			{
				for (int i = 0; i < tmpInventoryDropper.Count; i++)
				{
					Thing thing = tmpInventoryDropper[i];
					innerContainer.TryDrop(thing, Position, Map, ThingPlaceMode.Near, out _, delegate (Thing droppedThing, int unused)
					{
						if (droppedThing.def.IsPleasureDrug)
						{
							droppedThing.SetForbiddenIfOutsideHomeArea();
						}
					});
				}
			}
			tmpInventoryDropper.Clear();
			base.Destroy(mode);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref innerContainer, nameof(innerContainer), new object[] { this });
		}
	}
}
