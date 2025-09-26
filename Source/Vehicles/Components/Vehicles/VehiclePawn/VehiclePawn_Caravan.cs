using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Vehicles;

public partial class VehiclePawn
{
	private static readonly AccessTools.FieldRef<TransferableOneWay, int> CountToTransferFieldRef =
		AccessTools.FieldRefAccess<TransferableOneWay, int>("countToTransfer");

	public List<TransferableOneWay> cargoToLoad;

	public IntVec3 FollowerCell { get; protected set; }

	public override void ExitMap(bool allowedToJoinOrCreateCaravan, Rot4 exitDir)
	{
		base.ExitMap(allowedToJoinOrCreateCaravan, exitDir);
		foreach (Pawn pawn in AllPawnsAboard)
		{
			pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ExitedMap);
		}
	}

	/// <summary>
	/// Adds <paramref name="thing"/> to this vehicle's inventory, transferring it from its current container if necessary.
	/// </summary>
	/// <param name="thing">The entity to add or transfer.</param>
	/// <returns>
	/// The number of units (stack count) actually added or transferred into this vehicle's inventory.
	/// </returns>
	/// <remarks>
	/// This overload adds the entire stack by delegating to <see cref="AddOrTransfer(Verse.Thing, int)"/>
	/// with <c>thing.stackCount</c>.
	/// </remarks>
	/// <seealso cref="AddOrTransfer(Verse.Thing, int)"/>
	public int AddOrTransfer([NotNull] Thing thing)
	{
		return AddOrTransfer(thing, thing.stackCount);
	}

	/// <summary>
	/// Adds up to <paramref name="count"/> units of <paramref name="thing"/> to this vehicle's inventory, transferring
	/// from its current container if needed.
	/// </summary>
	/// <param name="thing">The entity to add or transfer.</param>
	/// <param name="count">The maximum number of units to add. Must be a positive integer.</param>
	/// <returns>
	/// The stack count added or transferred into the vehicle's inventory.
	/// </returns>
	/// <remarks>
	/// <para>
	/// On success, raises the <c>CargoAdded</c> event. If a matching <c>TransferableOneWay</c> exists
	/// in <c>cargoToLoad</c>, decreases its <c>CountToTransfer</c> by the amount moved and removes it when
	/// the remaining count is less than or equal to zero.
	/// </para>
	/// <para>
	/// Any exceptions thrown by the underlying container (<c>inventory.innerContainer</c>) are propagated to the caller.
	/// </para>
	/// </remarks>
	public int AddOrTransfer([NotNull] Thing thing, int count)
	{
		Pawn pawn = thing as Pawn;
		if (pawn != null)
		{
			if (pawn.Spawned)
				pawn.DeSpawn();
			if (pawn.IsWorldPawn())
				Find.WorldPawns.RemovePawn(pawn);
		}

		int result = inventory.innerContainer.TryAddOrTransfer(thing, count);

		EventRegistry[VehicleEventDefOf.CargoAdded].ExecuteEvents();
		if (pawn != null)
		{
			EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
		}

		TransferableOneWay transferable =
			TransferableUtility.TransferableMatchingDesperate(thing, cargoToLoad, TransferAsOneMode.Normal);
		if (transferable != null)
		{
			CountToTransferFieldRef.Invoke(transferable) = transferable.CountToTransfer - count;
			if (transferable.CountToTransfer <= 0)
			{
				cargoToLoad.Remove(transferable);
			}
		}
		return result;
	}

	/// <summary>
	/// Removes <paramref name="thing"/> from this vehicle's inventory by taking its entire stack.
	/// </summary>
	/// <param name="thing">The entity to remove.</param>
	/// <returns>
	/// The removed instance (or a newly split stack) from this vehicle's inventory.
	/// </returns>
	/// <remarks>
	/// This overload delegates to <see cref="TakeFromInventory(Verse.Thing, int)"/> with <c>thing.stackCount</c>.
	/// </remarks>
	/// <seealso cref="TakeFromInventory(Verse.Thing, int)"/>
	public Thing TakeFromInventory(Thing thing)
	{
		return inventory.innerContainer.Take(thing, thing.stackCount);
	}

	/// <summary>
	/// Removes up to <paramref name="count"/> units of <paramref name="thing"/> from this vehicle's inventory.
	/// </summary>
	/// <param name="thing">The entity to remove.</param>
	/// <param name="count">The maximum number of units to remove. Must be a positive integer.</param>
	/// <returns>
	/// The removed instance (or a newly split stack) from this vehicle's inventory.
	/// </returns>
	/// <remarks>
	/// On success, raises the <c>CargoRemoved</c> event after removal.
	/// </remarks>
	/// <exception cref="System.ArgumentException">Thrown if <paramref name="count"/> is less than or equal to zero.</exception>
	public Thing TakeFromInventory(Thing thing, int count)
	{
		Thing removedThing = inventory.innerContainer.Take(thing, count);
		EventRegistry[VehicleEventDefOf.CargoRemoved].ExecuteEvents();
		return removedThing;
	}

	public void RecalculateFollowerCell()
	{
		IntVec3 result = IntVec3.Invalid;
		if (Map != null)
		{
			Rot8 rot = FullRotation;
			for (int i = 0; i < 8; i++)
			{
				result = CalculateOffset(rot);
				if (result.InBounds(Map))
				{
					break;
				}
				rot.Rotate(RotationDirection.Clockwise);
			}
		}
		FollowerCell = result;
	}

	private IntVec3 CalculateOffset(Rot8 rot)
	{
		int offset = VehicleDef.Size.z; //Not reduced by half to avoid overlaps with vehicle tracks
		int offsetX = Mathf.CeilToInt(offset * Mathf.Cos(Mathf.Deg2Rad * 45));
		int offsetZ = Mathf.CeilToInt(offset * Mathf.Sin(Mathf.Deg2Rad * 45));
		IntVec3 root = PositionHeld;
		return rot.AsByte switch
		{
			Rot8.NorthInt     => new IntVec3(root.x, root.y, root.z - offset),
			Rot8.EastInt      => new IntVec3(root.x - offset, root.y, root.z),
			Rot8.SouthInt     => new IntVec3(root.x, root.y, root.z + offset),
			Rot8.WestInt      => new IntVec3(root.x + offset, root.y, root.z),
			Rot8.NorthEastInt => new IntVec3(root.x - offsetX, root.y, root.z - offsetZ),
			Rot8.SouthEastInt => new IntVec3(root.x - offsetX, root.y, root.z + offsetZ),
			Rot8.SouthWestInt => new IntVec3(root.x + offsetX, root.y, root.z + offsetZ),
			Rot8.NorthWestInt => new IntVec3(root.x + offsetX, root.y, root.z - offsetZ),
			_                 => throw new NotImplementedException()
		};
	}
}