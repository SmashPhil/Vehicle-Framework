using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Handles instance behavior of a vehicle's role.
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class VehicleRoleHandler : IExposable, ILoadReferenceable, IThingHolderPawnOverlayer,
	                                  IParallelRenderer, IComparable<VehicleRoleHandler>
	{
		/// <summary>
		/// Comfort pawn receives when placed inside vehicle cargo.
		/// </summary>
		public const float ComfortInsideCargo = 0.15f;

		/// <summary>
		/// innerContainer for role instance
		/// </summary>
		public ThingOwner<Pawn> thingOwner;

		private string roleKey;
		public VehicleRole role;

		public int uniqueID = -1;
		public VehiclePawn vehicle;

		public VehicleRoleHandler()
		{
			thingOwner = new ThingOwner<Pawn>(this, false);
		}

		public VehicleRoleHandler(VehiclePawn vehicle) : this()
		{
			uniqueID = VehicleIdManager.Instance.GetNextHandlerId();
			this.vehicle = vehicle;
		}

		public VehicleRoleHandler(VehiclePawn vehicle, VehicleRole role) : this(vehicle)
		{
			this.role = new VehicleRole(role); //Role must be instance based for upgrades to modify data
			roleKey = role.key;
		}

		bool IParallelRenderer.IsDirty { get; set; }

		public IThingHolder ParentHolder => vehicle;

		Rot4 IThingHolderPawnOverlayer.PawnRotation =>
			role.PawnRenderer?.RotFor(vehicle.FullRotation) ?? Rot4.South;

		float IThingHolderWithDrawnPawn.HeldPawnDrawPos_Y =>
			vehicle.DrawPos.y + role.PawnRenderer.LayerFor(vehicle.FullRotation);

		float IThingHolderWithDrawnPawn.HeldPawnBodyAngle =>
			role.PawnRenderer.AngleFor(vehicle.FullRotation);

		PawnPosture IThingHolderWithDrawnPawn.HeldPawnPosture => PawnPosture.LayingInBedFaceUp;

		bool IThingHolderPawnOverlayer.ShowBody => role.PawnRenderer.showBody;

		public bool RequiredForMovement => (role.HandlingTypes & HandlingType.Movement) != 0;

		public bool RoleFulfilled
		{
			get
			{
				bool minRequirement = role != null && thingOwner.Count >= role.SlotsToOperate;
				if (!minRequirement)
				{
					return false;
				}
				int operationalCount = 0;
				foreach (Pawn pawn in thingOwner)
				{
					if (CanOperateRole(pawn))
					{
						operationalCount++;
					}
				}
				return operationalCount >= role.SlotsToOperate;
			}
		}

		public bool AreSlotsAvailable => role != null && thingOwner.Count < role.Slots;

		public bool AreSlotsAvailableAndReservable
		{
			get
			{
				if (vehicle.Map == null)
					return AreSlotsAvailable;

				return AreSlotsAvailable && vehicle.Map.GetCachedMapComponent<VehicleReservationManager>()
				 .CanReserve<VehicleRoleHandler, VehicleHandlerReservation>(vehicle, null, this);
			}
		}

		public void DoTick()
		{
			thingOwner.DoTick();
		}

		public void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData,
			bool forceDraw = false)
		{
			foreach (Pawn pawn in thingOwner)
			{
				Rot4 rotOverride = role.PawnRenderer.RotFor(transformData.orientation);
				Vector3 offset = role.PawnRenderer.DrawOffsetFor(transformData.orientation);
				pawn.Drawer.renderer.DynamicDrawPhaseAt(phase, transformData.position + offset,
					rotOverride: rotOverride, neverAimWeapon: true);
			}
		}

		public bool CanOperateRole(Pawn pawn)
		{
			if (!CanOperateRole(pawn, role.HandlingTypes))
				return false;

			return pawn.Faction == vehicle.Faction || role.HandlingTypes == HandlingType.None;
		}

		public static bool CanOperateRole(Pawn pawn, HandlingType handlingType)
		{
			if (handlingType == HandlingType.None)
				return true;

			if ((handlingType & HandlingType.Turret) != 0 && pawn.WorkTagIsDisabled(WorkTags.Violent))
				return false;

			if (!pawn.RaceProps.ToolUser)
				return false;

			if (pawn.Downed || pawn.Dead || pawn.InMentalState)
				return false;

			if (pawn.IsPrisoner || pawn.IsColonyMech)
				return false;

			if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
				return false;

			if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Consciousness))
				return false;

			return true;
		}

		public override string ToString()
		{
			return roleKey;
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
			return thingOwner;
		}

		public void ExposeData()
		{
			Scribe_Values.Look(ref uniqueID, nameof(uniqueID), -1);
			Scribe_References.Look(ref vehicle, nameof(vehicle), true);
			Scribe_Values.Look(ref roleKey, nameof(role), forceSave: true);

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				//Deep save if inner pawns are not world pawns, as they will not be saved in the WorldPawns list
				thingOwner.contentsLookMode =
					(thingOwner.InnerListForReading.FirstOrDefault()?.IsWorldPawn() ?? false) ?
						LookMode.Reference :
						LookMode.Deep;
			}
			Scribe_Deep.Look(ref thingOwner, nameof(thingOwner), this);

			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				role = vehicle.VehicleDef.CreateRole(roleKey);
				if (role is null)
				{
					Log.Error(
						$"Unable to load role={roleKey}. Creating empty role to avoid game-breaking issues.");
					role ??= new VehicleRole
					{
						key = $"{roleKey}_INVALID",
						label = $"{roleKey} (INVALID)",
					};
				}
			}
		}

		int IComparable<VehicleRoleHandler>.CompareTo(VehicleRoleHandler other)
		{
			if (other is null)
				return -1;

			int lhsPriority = GetPriority(role.HandlingTypes);
			int rhsPriority = GetPriority(other.role.HandlingTypes);

			// Descending order, higher priority comes first
			return rhsPriority.CompareTo(lhsPriority);

			static int GetPriority(HandlingType type)
			{
				int priority = 0;
				if ((type & HandlingType.Movement) != 0)
					priority += 10;
				if ((type & HandlingType.Turret) != 0)
					priority += 1;
				return priority;
			}
		}
	}
}