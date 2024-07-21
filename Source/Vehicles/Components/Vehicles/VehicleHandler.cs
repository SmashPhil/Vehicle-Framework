using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	/// <summary>
	/// Handles instance behavior of a vehicle's role.
	/// </summary>
	public class VehicleHandler : IExposable, ILoadReferenceable, IThingHolderPawnOverlayer
	{
		public ThingOwner<Pawn> handlers;

		private string roleKey;
		public VehicleRole role;

		public int uniqueID = -1;
		public VehiclePawn vehicle;

		public VehicleHandler()
		{
			handlers ??= new ThingOwner<Pawn>(this, false, LookMode.Deep);
		}

		public VehicleHandler(VehiclePawn vehicle) : this()
		{
			uniqueID = VehicleIdManager.Instance.GetNextHandlerId();
			this.vehicle = vehicle;
		}

		public VehicleHandler(VehiclePawn vehicle, VehicleRole role) : this(vehicle)
		{
			this.role = new VehicleRole(role); //Role must be instance based for upgrades to modify data
			roleKey = role.key;
		}

		public IThingHolder ParentHolder => vehicle;

		Rot4 IThingHolderPawnOverlayer.PawnRotation => role.PawnRenderer?.RotFor(vehicle.FullRotation) ?? Rot4.South;

		float IThingHolderWithDrawnPawn.HeldPawnDrawPos_Y => vehicle.DrawPos.y + role.PawnRenderer.LayerFor(vehicle.FullRotation);

		float IThingHolderWithDrawnPawn.HeldPawnBodyAngle => role.PawnRenderer.AngleFor(vehicle.FullRotation);

		PawnPosture IThingHolderWithDrawnPawn.HeldPawnPosture => PawnPosture.LayingInBedFaceUp;

		bool IThingHolderPawnOverlayer.ShowBody => role.PawnRenderer.showBody;

		public bool RequiredForMovement => role.HandlingTypes.HasFlag(HandlingTypeFlags.Movement);

		public bool RoleFulfilled
		{
			get
			{
				bool minRequirement = role != null && handlers.Count >= role.SlotsToOperate;
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
				return operationalCount >= role.SlotsToOperate;
			}
		}

		public bool AreSlotsAvailable
		{
			get
			{
				bool reservation = vehicle.Map?.GetCachedMapComponent<VehicleReservationManager>().CanReserve<VehicleHandler, VehicleHandlerReservation>(vehicle, null, this) ?? true;
				return role != null && reservation && handlers.Count < role.Slots;
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
			if (role.HandlingTypes > HandlingTypeFlags.None)
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

		public void RenderPawns(Rot8 rot)
		{
			if (role.PawnRenderer != null)
			{
				foreach (Pawn pawn in handlers)
				{
					Vector3 position = vehicle.DrawPos + role.PawnRenderer.DrawOffsetFor(rot);
					Rot4 bodyFacing = role.PawnRenderer.RotFor(rot);
					pawn.Drawer.renderer.RenderPawnAt(position, rotOverride: bodyFacing);
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
				//Deep save if inner pawns are not world pawns, as they will not be saved in the WorldPawns list
				handlers.contentsLookMode = (handlers.InnerListForReading.FirstOrDefault()?.IsWorldPawn() ?? false) ? LookMode.Reference : LookMode.Deep;
			}
			Scribe_Deep.Look(ref handlers, nameof(handlers), new object[] { this });
			
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				role = vehicle.VehicleDef.CreateRole(roleKey);
				if (role is null)
				{
					Log.Error($"Unable to load role={roleKey}. Creating empty role to avoid game-breaking issues.");
					role ??= new VehicleRole()
					{
						key = $"{roleKey}_INVALID",
						label = $"{roleKey} (INVALID)",
					};
				}
			}
		}
	}
}
