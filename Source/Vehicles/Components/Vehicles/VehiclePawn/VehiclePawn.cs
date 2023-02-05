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
	public partial class VehiclePawn : Pawn, IInspectable, IAnimationTarget, IEventManager<VehicleEventDef>
	{
		public Dictionary<VehicleEventDef, EventTrigger> EventRegistry { get; set; }

		public VehicleDef VehicleDef => def as VehicleDef;

		public VehiclePawn()
		{
		}

		public Rot8 FullRotation
		{
			get
			{
				return new Rot8(Rotation, Angle);
			}
		}

		public void MarkForRecache(VehicleStatDef statDef)
		{
			throw new NotImplementedException();
		}

		public Pawn FindPawnWithBestStat(StatDef stat, Predicate<Pawn> pawnValidator = null)
		{
			Pawn pawn = null;
			float num = -1f;
			List<Pawn> pawnsListForReading = AllPawnsAboard;
			for (int i = 0; i < pawnsListForReading.Count; i++)
			{
				Pawn pawn2 = pawnsListForReading[i];
				if (!pawn2.Dead && !pawn2.Downed && !pawn2.InMentalState && CaravanUtility.IsOwner(pawn2, Faction) && !stat.Worker.IsDisabledFor(pawn2) && (pawnValidator is null || pawnValidator(pawn2)))
				{
					float statValue = pawn2.GetStatValue(stat, true);
					if (pawn == null || statValue > num)
					{
						pawn = pawn2;
						num = statValue;
					}
				}
			}
			return pawn;
		}

		public int AverageSkillOfCapablePawns(SkillDef skill)
		{
			if (AllCapablePawns.Count == 0)
			{
				return 0;
			}
			int value = 0;
			foreach (Pawn p in AllCapablePawns)
			{
				value += p.skills.GetSkill(skill).Level;
			}
			value /= AllCapablePawns.Count;
			return value;
		}

		private void InitializeVehicle()
		{
			if (handlers != null && handlers.Count > 0)
			{
				return;
			}
			if (cargoToLoad is null)
			{
				cargoToLoad = new List<TransferableOneWay>();
			}
			if (bills is null)
			{
				bills = new List<Bill_BoardVehicle>();
			}

			//navigationCategory = VehicleDef.defaultNavigation;

			if (VehicleDef.properties.roles != null && VehicleDef.properties.roles.Count > 0)
			{
				foreach (VehicleRole role in VehicleDef.properties.roles)
				{
					handlers.Add(new VehicleHandler(this, role));
				}
			}

			RecacheComponents();
		}

		public override void PostMapInit()
		{
			vPather.TryResumePathingAfterLoading();
		}

		public virtual void PostGenerationSetup()
		{
			InitializeVehicle();
			ageTracker.AgeBiologicalTicks = 0;
			ageTracker.AgeChronologicalTicks = 0;
			ageTracker.BirthAbsTicks = 0;
			health.Reset();
			statHandler.InitializeComponents();
		}

		/// <summary>
		/// Executes after vehicle has been loaded into the game
		/// </summary>
		/// <remarks>Called regardless if vehicle is spawned or unspawned. Responsible for important variables being set that may be called even for unspawned vehicles</remarks>
		protected virtual void PostLoad()
		{
			RegenerateUnsavedComponents();
			RecacheComponents();
			foreach (VehicleComp comp in AllComps.Where(t => t is VehicleComp))
			{
				comp.PostLoad();
			}
		}

		protected virtual void RegenerateUnsavedComponents()
		{
			vehicleAI = new VehicleAI(this);
			vDrawer = new Vehicle_DrawTracker(this);
			graphicOverlay = new VehicleGraphicOverlay(this);
			sustainers ??= new VehicleSustainers(this);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			this.RegisterEvents(); //Must register before comps call SpawnSetup to allow comps to access Registry
			base.SpawnSetup(map, respawningAfterLoad);
			EventRegistry[VehicleEventDefOf.Spawned].ExecuteEvents();
			sharedJob ??= new SharedJob();
			if (!respawningAfterLoad)
			{
				vPather.ResetToCurrentPosition();
			}
			if (Faction != Faction.OfPlayer)
			{
				ignition.Drafted = true;
				CompVehicleTurrets turretComp = CompVehicleTurrets;
				if (turretComp != null)
				{
					foreach (VehicleTurret turret in turretComp.turrets)
					{
						turret.autoTargeting = true;
						turret.AutoTarget = true;
					}
				}
			}
			ResetGraphicCache();
			Drawer.Notify_Spawned();
			InitializeHitbox();
			Map.GetCachedMapComponent<VehiclePositionManager>().ClaimPosition(this);
			Map.GetCachedMapComponent<VehicleRegionUpdateCatalog>().Notify_VehicleSpawned(this);
			Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleSpawned(this);
			ResetRenderStatus();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref vPather, nameof(vPather), new object[] { this });
			Scribe_Deep.Look(ref ignition, nameof(ignition), new object[] { this });
			Scribe_Deep.Look(ref statHandler, nameof(statHandler), new object[] { this });
			Scribe_Deep.Look(ref sharedJob, nameof(sharedJob));

			Scribe_Values.Look(ref angle, nameof(angle));

			Scribe_Deep.Look(ref patternData, nameof(patternData));
			Scribe_Defs.Look(ref retexture, nameof(retexture));
			Scribe_Deep.Look(ref patternToPaint, nameof(patternToPaint));

			Scribe_Values.Look(ref movementStatus, nameof(movementStatus), VehicleMovementStatus.Online);
			//Scribe_Values.Look(ref navigationCategory, nameof(navigationCategory), NavigationCategory.Opportunistic);
			Scribe_Values.Look(ref currentlyFishing, nameof(currentlyFishing), false);
			Scribe_Values.Look(ref showAllItemsOnMap, nameof(showAllItemsOnMap));

			Scribe_Collections.Look(ref cargoToLoad, nameof(cargoToLoad));

			Scribe_Collections.Look(ref handlers, nameof(handlers), LookMode.Deep);
			Scribe_Collections.Look(ref bills, nameof(bills), LookMode.Deep);

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				PostLoad();
			}
		}
	}
}
