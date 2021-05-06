using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class DockedBoat : WorldObject, IThingHolder, ILoadReferenceable
	{
		public ThingOwner<VehiclePawn> dockedBoats = new ThingOwner<VehiclePawn>();

		private Material cachedMaterial;

		private List<Pawn> tmpSavedBoats = new List<Pawn>();

		public override Material Material
		{
			get
			{
				if(cachedMaterial is null)
				{
					Color color;
					if(Faction != null)
						color = Faction.Color;
					else
						color = Color.white;
					cachedMaterial = MaterialPool.MatFrom(def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.WorldObjectRenderQueue);
				}
				return cachedMaterial;
			}
		}

		private int TotalAvailableSeats
		{
			get
			{
				int num = 0;
				foreach(VehiclePawn b in dockedBoats)
				{
					num += b.SeatsAvailable;
				}
				return num;
			}
		}

		public void Notify_CaravanArrived(Caravan caravan)
		{
			if(caravan.PawnsListForReading.Where(p => !p.IsBoat()).Count() > TotalAvailableSeats)
			{
				Messages.Message("CaravanMustHaveEnoughSpaceOnShip".Translate(), this, MessageTypeDefOf.RejectInput, false);
				return;
			}
			caravan.pawns.TryAddRangeOrTransfer(dockedBoats);
			List<Pawn> boats = caravan.PawnsListForReading.Where(p => p.IsBoat()).ToList();
			foreach (Pawn p in caravan.pawns)
			{
				if (!p.IsBoat())
				{
					for (int i = p.inventory.innerContainer.Count - 1; i >= 0; i--)
					{
						Thing t = p.inventory.innerContainer[i];
						p.inventory.innerContainer.TryTransferToContainer(t, boats.Find(x => !MassUtility.IsOverEncumbered(x)).inventory.innerContainer, true);
					}
				}
			}
			CaravanHelper.ToggleDocking(caravan, false);
			Find.WorldObjects.Remove(this);
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
		{
			foreach(FloatMenuOption o in base.GetFloatMenuOptions(caravan))
			{
				yield return o;
			}
			foreach(FloatMenuOption f in CaravanArrivalAction_DockedBoats.GetFloatMenuOptions(caravan, this))
			{
				yield return f;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();

			if(Scribe.mode == LoadSaveMode.Saving)
			{
				tmpSavedBoats.Clear();
				tmpSavedBoats.AddRange(dockedBoats.InnerListForReading);
				dockedBoats.RemoveAll(x => x is Pawn);
				dockedBoats.RemoveAll(x => x.Destroyed);
			}
			Scribe_Collections.Look(ref tmpSavedBoats, "tmpSavedBoats", LookMode.Reference);
			Scribe_Deep.Look(ref dockedBoats, "dockedBoats", new object[]
			{
				this
			});

			if(Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving)
			{
				for (int j = 0; j < tmpSavedBoats.Count; j++)
				{
					dockedBoats.TryAdd(tmpSavedBoats[j], false);
				}
				tmpSavedBoats.Clear();
			}
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return dockedBoats;
		}
	}
}
