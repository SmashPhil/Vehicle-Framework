using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;

namespace Vehicles
{
	public partial class VehiclePawn
	{
		public List<TransferableOneWay> cargoToLoad;
		private bool outOfFoodNotified = false;
		public bool showAllItemsOnMap = false;

		public IntVec3 FollowerCell { get; protected set; }

		public bool HasNegotiator
		{
			get
			{
				Pawn pawn = WorldHelper.FindBestNegotiator(this);
				return pawn != null && !pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled;
			}
		}

		public void AddOrTransfer(Thing thing, int count, Pawn holder = null)
		{
			if (holder != null)
			{
				holder.carryTracker.innerContainer.TryTransferToContainer(thing, inventory.innerContainer, count, true);
			}
			else
			{
				inventory.innerContainer.TryAdd(Thing, count);
			}
			EventRegistry[VehicleEventDefOf.CargoAdded].ExecuteEvents();
		}

		public void TakeFromInventory()
		{
			EventRegistry[VehicleEventDefOf.CargoRemoved].ExecuteEvents();
		}

		protected IntVec3 CalculateOffset(Rot8 rot)
		{
			int offset = VehicleDef.Size.z; //Not reduced by half to avoid overlaps with vehicle tracks
			int offsetX = Mathf.CeilToInt(offset * Mathf.Cos(Mathf.Deg2Rad * 45));
			int offsetZ = Mathf.CeilToInt(offset * Mathf.Sin(Mathf.Deg2Rad * 45));
			IntVec3 root = PositionHeld;
			return rot.AsByte switch
			{
				Rot8.NorthInt => new IntVec3(root.x, root.y, root.z - offset),
				Rot8.EastInt => new IntVec3(root.x - offset, root.y, root.z),
				Rot8.SouthInt => new IntVec3(root.x, root.y, root.z + offset),
				Rot8.WestInt => new IntVec3(root.x + offset, root.y, root.z),
				Rot8.NorthEastInt => new IntVec3(root.x - offsetX, root.y, root.z - offsetZ),
				Rot8.SouthEastInt => new IntVec3(root.x - offsetX, root.y, root.z + offsetZ),
				Rot8.SouthWestInt => new IntVec3(root.x + offsetX, root.y, root.z + offsetZ),
				Rot8.NorthWestInt => new IntVec3(root.x + offsetX, root.y, root.z - offsetZ),
				_ => throw new NotImplementedException()
			};
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
	}
}
