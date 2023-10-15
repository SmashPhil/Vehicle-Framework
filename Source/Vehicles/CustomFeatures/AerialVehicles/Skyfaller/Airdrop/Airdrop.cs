using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class Airdrop : Building, IThingHolder, IOpenable
	{
		private static readonly List<Thing> tmpInventoryDropper = new List<Thing>();

		public ThingOwner<Thing> innerContainer = new ThingOwner<Thing>();

		public bool CanOpen => true;

		public int OpenTicks => 180;

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return innerContainer;
		}

		private void DropAllContents()
		{
			if (!innerContainer.NullOrEmpty())
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
			}
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			DropAllContents();
			base.Destroy(mode);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref innerContainer, nameof(innerContainer), new object[] { this });
		}

		public void Open()
		{
			Destroy();
		}
	}
}
