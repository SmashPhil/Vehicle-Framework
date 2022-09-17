using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class VehicleHandler : IExposable, ILoadReferenceable, IThingHolderPawnOverlayer
	{
		public ThingOwner<Pawn> handlers;

		private string roleKey;
		public VehicleRole role;

		private List<Pawn> tempSavedPawns = new List<Pawn>();

		public int uniqueID = -1;
		public VehiclePawn vehicle;
		
		public VehicleHandler()
		{
			if (handlers is null)
			{
				handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
			}
		}

		public VehicleHandler(VehiclePawn vehicle)
		{
			uniqueID = VehicleIdManager.Instance.GetNextHandlerId();
			this.vehicle = vehicle;
			if (handlers is null)
			{
				handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
			}
		}

		public VehicleHandler(VehiclePawn vehicle, VehicleRole newRole)
		{
			List<Pawn> newHandlers = new List<Pawn>();
			uniqueID = VehicleIdManager.Instance.GetNextHandlerId();
			this.vehicle = vehicle;
			role = new VehicleRole(newRole);
			roleKey = role.key;
			if (handlers is null)
			{
				handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
			}
			if ((newHandlers?.Count ?? 0) > 0)
			{
				foreach (Pawn p in newHandlers)
				{
					if (p.Spawned) 
					{ 
						p.DeSpawn(); 
					}
					if (p.holdingOwner != null) 
					{ 
						p.holdingOwner = null; 
					}
					if (!p.IsWorldPawn()) 
					{ 
						Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide); 
					}
				}
				handlers.TryAddRangeOrTransfer(newHandlers);
			}
		}

		public IThingHolder ParentHolder => vehicle;

		public float OverlayPawnBodyAngle => role.pawnRenderer.AngleFor(vehicle.FullRotation);

		public Rot4 PawnRotation => role.pawnRenderer?.RotFor(vehicle.FullRotation) ?? Rot4.South;

		public bool RequiredForMovement => role.handlingTypes.NotNullAndAny(h => h.HasFlag(HandlingTypeFlags.Movement));

		public bool RoleFulfilled => role != null && handlers.Count >= role.slotsToOperate;

		public bool AreSlotsAvailable
		{
			get
			{
				bool reservation = vehicle.Map?.GetCachedMapComponent<VehicleReservationManager>().CanReserve<VehicleHandler, VehicleHandlerReservation>(vehicle, null, this) ?? true;
				return role != null && reservation && handlers.Count < role.slots;
			}
		}

		public static bool operator ==(VehicleHandler obj1, VehicleHandler obj2) => obj1?.Equals(obj2) ?? (obj1 is null && obj2 is null);

		public static bool operator !=(VehicleHandler obj1, VehicleHandler obj2) => !(obj1 == obj2);

		public bool CanOperateRole(Pawn pawn)
		{
			if (!role.handlingTypes.NullOrEmpty())
			{
				bool manipulation = pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
				bool downed = pawn.Downed;
				bool dead = pawn.Dead;
				bool isCrazy = pawn.InMentalState;
				bool prisoner = pawn.IsPrisoner;
				return manipulation && !downed && !dead && !isCrazy && !prisoner;
			}
			return true;
		}

		public void RenderPawns()
		{
			if (role.pawnRenderer != null)
			{
				foreach (Pawn pawn in handlers)
				{
					pawn.Drawer.renderer.RenderPawnAt(vehicle.DrawPos + role.pawnRenderer.DrawOffsetFor(vehicle.FullRotation), role.pawnRenderer.RotFor(vehicle.FullRotation));
				}
			}
		}

		public override bool Equals(object obj)
		{
			return obj is VehicleHandler handler && Equals(handler);
		}

		public bool Equals(VehicleHandler obj2)
		{
			return obj2?.roleKey == roleKey;
		}

		public override string ToString()
		{
			return role.label;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public string GetUniqueLoadID()
		{
			return $"VehicleHandler_{uniqueID}";
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return handlers;
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref uniqueID, nameof(uniqueID), -1);
			Scribe_References.Look(ref vehicle, nameof(vehicle), true);
			
			Scribe_Values.Look(ref roleKey, nameof(role), forceSave: true);
			//Scribe_Deep.Look(ref role, "role");

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				tempSavedPawns.Clear();
				tempSavedPawns.AddRange(handlers.InnerListForReading);
				handlers.RemoveAll(x => x is Pawn);
				handlers.RemoveAll(x => x.Destroyed);
			}

			Scribe_Collections.Look(ref tempSavedPawns, nameof(tempSavedPawns), LookMode.Reference);
			Scribe_Deep.Look(ref handlers, nameof(handlers), new object[]
			{
				this
			});
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				role = vehicle.VehicleDef.properties.roles.FirstOrDefault(role => role.key == roleKey);
				if (role is null)
				{
					Log.Error($"Could not load VehicleRole from {roleKey}. Was role removed or name changed?");
				}
			}
			if (Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving)
			{
				for (int j = 0; j < tempSavedPawns.Count; j++)
				{
					handlers.TryAdd(tempSavedPawns[j], true);
				}
				tempSavedPawns.Clear();
			}
		}
	}
}
