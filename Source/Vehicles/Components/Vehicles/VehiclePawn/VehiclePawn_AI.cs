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
		[Unsaved]
		public VehicleAI vehicleAI;
		
		public Vehicle_PathFollower vPather;
		public Vehicle_IgnitionController ignition;

		public SharedJob sharedJob;
		public bool currentlyFishing = false;

		public virtual bool DeconstructibleBy(Faction faction)
		{
			return DebugSettings.godMode || Faction == faction;
		}

		//Todo - clean up and add gizmo for claiming to VehiclePawn class
		public virtual bool ClaimableBy(Faction faction)
		{
			if (!def.Claimable)
			{
				return false;
			}
			if (Faction != null)
			{
				if (Faction == faction)
				{
					return false;
				}
				if (faction == Faction.OfPlayer)
				{
					if (Faction == Faction.OfInsects)
					{
						if (HiveUtility.AnyHivePreventsClaiming(this))
						{
							return false;
						}
					}
					else
					{
						if (Faction == Faction.OfMechanoids)
						{
							return false;
						}
						if (Spawned && AnyHostileToolUserOfFaction(Faction))
						{
							return false;
						}
					}
				}
			}
			else if (Spawned && Map.ParentFaction != null && Map.ParentFaction != Faction.OfPlayer && Map.ParentFaction.def.humanlikeFaction && AnyHostileToolUserOfFaction(Map.ParentFaction))
			{
				return false;
			}
			return true;

			bool AnyHostileToolUserOfFaction(Faction faction)
			{
				if (!Spawned)
				{
					return false;
				}
				List<Pawn> list = Map.mapPawns.SpawnedPawnsInFaction(faction);
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].RaceProps.ToolUser && GenHostility.IsPotentialThreat(list[i]))
					{
						return true;
					}
				}
				return false;
			}
		}

		public new void Notify_Teleported(bool endCurrentJob = true, bool resetTweenedPos = true)
		{
			if (resetTweenedPos)
			{
				Drawer.tweener.ResetTweenedPosToRoot();
			}
			vPather.Notify_Teleported();
			if (endCurrentJob && jobs != null && jobs.curJob != null)
			{
				jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
		}

		public virtual bool CanDraft()
		{
			foreach (ThingComp thingComp in AllComps)
			{
				if (thingComp is VehicleComp vehicleComp && !vehicleComp.CanDraft(out string failReason))
				{
					Messages.Message(failReason, MessageTypeDefOf.RejectInput);
					return false;
				}
			}
			if (!CanMoveFinal)
			{
				Messages.Message("VF_NotEnoughToOperate".Translate(), MessageTypeDefOf.RejectInput);
			}
			return true;
		}

		//REDO
		public IEnumerable<VehicleComp> GetAllAIComps()
		{
			foreach (VehicleComp comp in cachedComps.Where(c => c.GetType().IsAssignableFrom(typeof(VehicleComp))).Cast<VehicleComp>())
			{
				yield return comp;
			}
		}

		public int TotalAllowedFor(JobDef jobDef)
		{
			if (!VehicleMod.settings.main.multiplePawnsPerJob)
			{
				return 1;
			}

			foreach (VehicleJobLimitations jobLimit in VehicleDef.properties.vehicleJobLimitations)
			{
				if (jobLimit.defName == jobDef.defName)
				{
					return jobLimit.maxWorkers;
				}
			}
			return 1;
		}

		public void BeachShip()
		{
			movementStatus = VehicleMovementStatus.Offline;
			beached = true;
		}

		public void RemoveBeachedStatus()
		{
			movementStatus = VehicleMovementStatus.Online;
			beached = false;
		}
	}
}
