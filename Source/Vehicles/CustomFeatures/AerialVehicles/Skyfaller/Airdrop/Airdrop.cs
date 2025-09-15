using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Vehicles;

public class Airdrop : Building, IThingHolder, IOpenable
{
	public ThingOwner<Thing> innerContainer = [];

	bool IOpenable.CanOpen => true;

	int IOpenable.OpenTicks => 180;

	void IOpenable.Open()
	{
		Destroy();
	}

	void IThingHolder.GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, innerContainer);
	}

	ThingOwner IThingHolder.GetDirectlyHeldThings()
	{
		return innerContainer;
	}

	private void DropAllContents()
	{
		if (!innerContainer.NullOrEmpty())
		{
			for (int i = innerContainer.InnerListForReading.Count - 1; i >= 0; i--)
			{
				Thing thing = innerContainer[i];
				innerContainer.TryDrop(thing, Position, Map, ThingPlaceMode.Near, out _, ItemDropped);
			}
		}
		return;

		static void ItemDropped(Thing droppedThing, int _)
		{
			if (droppedThing.def.IsPleasureDrug)
				droppedThing.SetForbiddenIfOutsideHomeArea();
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
		Scribe_Deep.Look(ref innerContainer, nameof(innerContainer), this);
	}
}