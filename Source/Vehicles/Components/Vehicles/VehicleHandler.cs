using System.Collections.Generic;
using Verse;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class VehicleHandler : IExposable, ILoadReferenceable, IThingHolder
	{
		public ThingOwner<Pawn> handlers;

		public VehicleRole role;

		private List<Pawn> tempSavedPawns = new List<Pawn>();

		public int uniqueID = -1;
		public VehiclePawn vehiclePawn;
		
		public VehicleHandler()
		{
			if(handlers is null)
			{
				handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
			}
		}

		public VehicleHandler(VehiclePawn vehiclePawn)
		{
			uniqueID = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextHandlerId();
			this.vehiclePawn = vehiclePawn;
			if(handlers is null)
			{
				handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
			}
		}

		public VehicleHandler(VehiclePawn vehiclePawn, VehicleRole newRole)
		{
			List<Pawn> newHandlers = new List<Pawn>();
			uniqueID = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextHandlerId();
			this.vehiclePawn = vehiclePawn;
			role = new VehicleRole(newRole);
			if (handlers is null)
			{
				handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
			}
			if((newHandlers?.Count ?? 0) > 0)
			{
				foreach(Pawn p in newHandlers)
				{
					if(p.Spawned) { p.DeSpawn(); }
					if(p.holdingOwner != null) { p.holdingOwner = null; }
					if (!p.IsWorldPawn()) { Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide); }
				}
				handlers.TryAddRangeOrTransfer(newHandlers);
			}
		}

		public bool AreSlotsAvailable
		{
			get
			{
				bool reservation = vehiclePawn.Map?.GetCachedMapComponent<VehicleReservationManager>().CanReserve<VehicleHandler, VehicleHandlerReservation>(vehiclePawn, null, this) ?? true;
				return role != null &&  reservation && handlers.Count < role.slots;
			}
		}

		public static bool operator ==(VehicleHandler obj1, VehicleHandler obj2) => obj1.Equals(obj2);

		public static bool operator !=(VehicleHandler obj1, VehicleHandler obj2) => !(obj1 == obj2);

		public override bool Equals(object obj)
		{
			return obj is VehicleHandler handler && Equals(handler);
		}

		public bool Equals(VehicleHandler obj2)
		{
			return obj2?.role.key == role.key;
		}

		public override string ToString()
		{
			return $"{role.label}: {handlers.Count}/{role.slots}";
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
			Scribe_References.Look(ref vehiclePawn, "vehiclePawn");
			Scribe_Deep.Look(ref role, "role");

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				tempSavedPawns.Clear();
				tempSavedPawns.AddRange(handlers.InnerListForReading);
				handlers.RemoveAll(x => x is Pawn);
				handlers.RemoveAll(x => x.Destroyed);
			}

			Scribe_Collections.Look(ref tempSavedPawns, "tempSavedPawns", LookMode.Reference);
			Scribe_Deep.Look(ref handlers, "handlers", new object[]
			{
				this
			});

			if (Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving)
			{
				for (int j = 0; j < tempSavedPawns.Count; j++)
				{
					handlers.TryAdd(tempSavedPawns[j], true);
				}
				tempSavedPawns.Clear();
			}
		}

		public string GetUniqueLoadID()
		{
			return $"VehicleHandler_{uniqueID}";
		}

		public IThingHolder ParentHolder => vehiclePawn;

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return handlers;
		}
	}
}
