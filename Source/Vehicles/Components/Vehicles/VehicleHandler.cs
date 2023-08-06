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

		public int uniqueID = -1;
		public VehiclePawn vehicle;

		public VehicleHandler()
		{
			if (handlers is null)
			{
				handlers = new ThingOwner<Pawn>(this, false, LookMode.Deep);
			}
		}

		public VehicleHandler(VehiclePawn vehicle) : this()
		{
			uniqueID = VehicleIdManager.Instance.GetNextHandlerId();
			this.vehicle = vehicle;
		}

		public VehicleHandler(VehiclePawn vehicle, VehicleRole newRole) : this(vehicle)
		{
			role = new VehicleRole(newRole);
			roleKey = role.key;
		}

		public IThingHolder ParentHolder => vehicle;

		public float OverlayPawnBodyAngle => role.pawnRenderer.AngleFor(vehicle.FullRotation);

		public Rot4 PawnRotation => role.pawnRenderer?.RotFor(vehicle.FullRotation) ?? Rot4.South;

		public bool RequiredForMovement => role.handlingTypes.HasFlag(HandlingTypeFlags.Movement);

		public bool RoleFulfilled
		{
			get
			{
				bool minRequirement = role != null && handlers.Count >= role.slotsToOperate;
				if (!minRequirement)
				{
					return false;
				}
				int operationalCount = 0;
				foreach (Pawn pawn in handlers)
				{
					if (CanOperateRole(pawn))
					{
						operationalCount++;
					}
				}
				return operationalCount >= role.slotsToOperate;
			}
		}

		public bool AreSlotsAvailable
		{
			get
			{
				bool reservation = vehicle.Map?.GetCachedMapComponent<VehicleReservationManager>().CanReserve<VehicleHandler, VehicleHandlerReservation>(vehicle, null, this) ?? true;
				return role != null && reservation && handlers.Count < role.slots;
			}
		}

		public static bool operator ==(VehicleHandler lhs, VehicleHandler rhs)
		{
			if (lhs is null)
			{
				return rhs is null;
			}
			return lhs.Equals(rhs);
		}

		public static bool operator !=(VehicleHandler lhs, VehicleHandler rhs)
		{
			return !(lhs == rhs);
		}

		public static bool operator ==(VehicleHandler lhs, IThingHolder rhs)
		{
			if (!(rhs is VehicleHandler handler))
			{
				return false;
			}
			return lhs == handler;
		}

		public static bool operator !=(VehicleHandler lhs, IThingHolder rhs)
		{
			return !(lhs == rhs);
		}

		public bool CanOperateRole(Pawn pawn)
		{
			if (role.handlingTypes > HandlingTypeFlags.None)
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

		public void Tick()
		{
			handlers.ThingOwnerTick();
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
			return roleKey;
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

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				handlers.contentsLookMode = vehicle.IsWorldPawn() ? LookMode.Reference : LookMode.Deep; //Reference save on world map since pawns will be deep saved in WorldPawns.pawnsAlive
			}
			Scribe_Deep.Look(ref handlers, nameof(handlers), new object[] { this });
			
			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				role = vehicle.VehicleDef.properties.roles.FirstOrDefault(role => role.key == roleKey);
				if (role is null)
				{
					Log.Error($"Could not load VehicleRole from {roleKey}. Was role removed or name changed?");
				}
			}
		}
	}
}
