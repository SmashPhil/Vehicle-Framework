using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;
using Vehicles.AI;
using Vehicles.Lords;
using Vehicles.Defs;

namespace Vehicles
{
	public class VehiclePawn : Pawn
	{
		public Vehicle_PathFollower vPather;
		public Vehicle_DrawTracker vDrawer;
		public VehicleAI vehicleAI;
		public VehicleStatHandler statHandler;

		public PatternDef pattern;
		private Color color1 = Color.white;
		private Color color2 = Color.white;
		private Color color3 = Color.white;

		public List<Bill_BoardShip> bills = new List<Bill_BoardShip>();
		public List<VehicleHandler> handlers = new List<VehicleHandler>();

		public bool currentlyFishing = false;
		public bool draftStatusChanged = false;
		public bool beached = false;
		public bool showAllItemsOnMap = false;

		private float angle = 0f; /* -45 is left, 45 is right : relative to Rot4 direction*/
		public bool inFlight = false;

		private float cargoCapacity;
		private float armorPoints;
		private float moveSpeedModifier;

		private Graphic_Vehicle graphicInt;

		private SelfOrderingList<ThingComp> cachedComps = new SelfOrderingList<ThingComp>();
		private List<ThingComp> compTickers = new List<ThingComp>();

		private CompCannons compCannons;
		private CompFueledTravel compFuel;
		private CompUpgradeTree compUpgradeTree;
		private CompVehicleLauncher compVehicleLauncher;

		public List<TransferableOneWay> cargoToLoad;
		private bool outOfFoodNotified = false;

		public VehicleMovementStatus movementStatus = VehicleMovementStatus.Online;
		public NavigationCategory navigationCategory = NavigationCategory.Opportunistic;

		internal VehicleComponent HighlightedComponent { get; set; }
		public CellRect Hitbox { get; private set; }
		public float CachedAngle { get; set; }

		public bool CanMove => ActualMoveSpeed > 0.1f && SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), "vehicleMovementPermissions", VehicleDef.vehicleMovementPermissions) >= VehiclePermissions.DriverNeeded && movementStatus == VehicleMovementStatus.Online;
		public bool CanMoveFinal => CanMove && (PawnCountToOperateFullfilled || VehicleMod.settings.debug.debugDraftAnyShip);
		public bool PawnCountToOperateFullfilled => PawnCountToOperateLeft <= 0;

		public VehicleDef VehicleDef => def as VehicleDef;

		public float StatMoveSpeed => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), "speed", VehicleDef.speed);
		public float StatArmor => 1;// SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), "armor", VehicleDef.armor);
		public float StatCargo => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), "cargoCapacity", VehicleDef.cargoCapacity);
		public bool StatNameable => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), "nameable", VehicleDef.nameable);

		public override Vector3 DrawPos => Drawer.DrawPos;

		public CompCannons CompCannons
		{
			get
			{
				if (compCannons is null)
				{
					compCannons = GetSortedComp<CompCannons>();
				}
				return compCannons;
			}
		}

		public CompFueledTravel CompFueledTravel
		{
			get
			{
				if (compFuel is null)
				{
					compFuel = GetSortedComp<CompFueledTravel>();
				}
				return compFuel;
			}
		}

		public CompUpgradeTree CompUpgradeTree
		{
			get
			{
				if (compUpgradeTree is null)
				{
					compUpgradeTree = GetSortedComp<CompUpgradeTree>();
				}
				return compUpgradeTree;
			}
		}

		public CompVehicleLauncher CompVehicleLauncher
		{
			get
			{
				if (compVehicleLauncher is null)
				{
					compVehicleLauncher = GetSortedComp<CompVehicleLauncher>();
				}
				return compVehicleLauncher;
			}
		}

		public float ActualMoveSpeed
		{
			get
			{
				float baseSpeed = (StatMoveSpeed + MoveSpeedModifier);
				baseSpeed *= VehicleDef.properties.overweightSpeedCurve.Evaluate(MassUtility.InventoryMass(this) / CargoCapacity); //Weight decrease
				baseSpeed *= statHandler.StatEfficiency(VehicleStatCategoryDefOf.StatCategoryMovement); //Component Efficiency
				baseSpeed *= (this.SlowSpeed() ? 0.5f : 1f); //Lord jobs
				return baseSpeed;
			}
		}

		public string ActualMoveSpeedCalcString
		{
			get
			{
				return $"Speed of movement in cells per second.\n\n{"StatsReport_BaseValue".Translate()}: {StatMoveSpeed + MoveSpeedModifier}";
			}
		}

		public float ArmorPoints
		{
			get
			{
				return armorPoints + StatArmor;
			}
			set
			{
				armorPoints = value;
				if (armorPoints < 0)
				{
					armorPoints = 0f;
				}
			}
		}

		public float CargoCapacity
		{
			get
			{
				return cargoCapacity;
			}
			set
			{
				cargoCapacity = value;
				if (cargoCapacity < 0)
				{
					cargoCapacity = 0f;
				}
			}
		}

		public float MoveSpeedModifier
		{
			get
			{
				return moveSpeedModifier;
			}
			set
			{
				moveSpeedModifier = value;
				if (moveSpeedModifier < 0)
				{
					moveSpeedModifier = 0;
				}
			}
		}

		public Rot8 FullRotation
		{
			get
			{
				return new Rot8(Rotation, Angle);
			}
		}

		public IEnumerable<IntVec3> InhabitedCells(int expandedBy = 0)
		{
			if (Angle == 0)
			{
				return CellRect.CenteredOn(Position, def.Size.x, def.Size.z).ExpandedBy(expandedBy).Cells;
			}
			return CellRect.CenteredOn(Position, def.Size.x, def.Size.z).ExpandedBy(expandedBy).Cells; //REDO FOR DIAGONALS
		}

		public float Angle
		{
			get
			{
				if (!VehicleDef.properties.diagonalRotation)
				{
					return 0f;
				}
				return angle;
			}
			set
			{
				if (value == angle)
				{
					return;
				}
				angle = value;
			}
		}

		public new Vehicle_DrawTracker Drawer
		{
			get
			{
				if (vDrawer is null)
				{
					vDrawer = new Vehicle_DrawTracker(this);
				}
				return vDrawer;
			}
		}

		public Graphic_Vehicle VehicleGraphic
		{
			get
			{
				if (graphicInt is null)
				{
					var graphicData = new GraphicDataRGB();
					graphicData.CopyFrom(ageTracker.CurKindLifeStage.bodyGraphicData as GraphicDataRGB);
					graphicData.color = DrawColor;
					graphicData.colorTwo = DrawColorTwo;
					graphicData.colorThree = DrawColorThree;
					graphicData.pattern = pattern;
					graphicInt = graphicData.Graphic as Graphic_Vehicle;
				}
				return graphicInt;
			}
		}

		public override Color DrawColor
		{
			get
			{
				return color1;
			}
			set
			{
				color1 = value;
			}
		}

		public new Color DrawColorTwo
		{
			get
			{
				return color2;
			}
			set
			{
				color2 = value;
			}
		}

		public Color DrawColorThree
		{
			get
			{
				return color3;
			}
			set
			{
				color3 = value;
			}
		}

		public bool MovementHandlerAvailable
		{
			get
			{
				foreach (VehicleHandler handler in this.handlers)
				{
					if (handler.role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement) && handler.handlers.Count < handler.role.slotsToOperate)
					{
						return false;
					}
				}
				if (CompFueledTravel != null && CompFueledTravel.Fuel <= 0f)
					return false;
				return true;
			}
		}

		public bool HasNegotiator
		{
			get
			{
				Pawn pawn = WorldHelper.FindBestNegotiator(this);
				return pawn != null && !pawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled;
			}
		}

		public int PawnCountToOperate
		{
			get
			{
				int pawnCount = 0;
				foreach (VehicleRole r in VehicleDef.properties.roles)
				{
					if (r.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement))
						pawnCount += r.slotsToOperate;
				}
				return pawnCount;
			}
		}

		public int PawnCountToOperateLeft
		{
			get
			{
				int pawnsMounted = 0;
				foreach(VehicleHandler handler in handlers.Where(h => h.role.handlingTypes.Contains(HandlingTypeFlags.Movement)))
				{
					pawnsMounted += handler.handlers.Count;
				}
				return PawnCountToOperate - pawnsMounted;
			}
		}

		public List<Pawn> AllPawnsAboard
		{
			get
			{
				List<Pawn> pawnsOnShip = new List<Pawn>();
				if (!(handlers is null) && handlers.Count > 0)
				{
					foreach (VehicleHandler handler in handlers)
					{
						if (!(handler.handlers is null) && handler.handlers.Count > 0) pawnsOnShip.AddRange(handler.handlers);
					}
				}

				return pawnsOnShip;
			}
		}

		public List<Pawn> AllCrewAboard
		{
			get
			{
				List<Pawn> crewOnShip = new List<Pawn>();
				if (!(handlers is null))
				{
					foreach (VehicleHandler handler in handlers)
					{
						if (handler.role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Movement))
						{
							crewOnShip.AddRange(handler.handlers);
						}
					}
				}
				return crewOnShip;
			}
		}

		public List<Pawn> AllCannonCrew
		{
			get
			{
				List<Pawn> weaponCrewOnShip = new List<Pawn>();
				foreach(VehicleHandler handler in handlers)
				{
					if (handler.role.handlingTypes.NotNullAndAny(h => h == HandlingTypeFlags.Cannon))
					{
						weaponCrewOnShip.AddRange(handler.handlers);
					}
				}
				return weaponCrewOnShip;
			}
		}

		public List<Pawn> Passengers
		{
			get
			{
				List<Pawn> passengersOnShip = new List<Pawn>();
				if(!(handlers is null))
				{
					foreach(VehicleHandler handler in handlers)
					{
						if(handler.role.handlingTypes.NullOrEmpty())
						{
							passengersOnShip.AddRange(handler.handlers);
						}
					}
				}
				return passengersOnShip;
			}
		}

		public List<Pawn> AllCapablePawns
		{
			get
			{
				List<Pawn> pawnsOnShip = new List<Pawn>();
				if(!(handlers is null) && handlers.Count > 0)
				{
					foreach (VehicleHandler handler in handlers)
					{
						if (!(handler.handlers is null) && handler.handlers.Count > 0) pawnsOnShip.AddRange(handler.handlers);
					}
				}
				pawnsOnShip = pawnsOnShip.Where(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))?.ToList();
				return pawnsOnShip ?? new List<Pawn>() { };
			}
		}

		public int SeatsAvailable
		{
			get
			{
				int x = 0;
				foreach(VehicleHandler handler in handlers)
				{
					x += handler.role.slots - handler.handlers.Count;
				}
				return x;
			}
		}

		public int TotalSeats
		{
			get
			{
				int x = 0;
				foreach(VehicleHandler handler in handlers)
				{
					x += handler.role.slots;
				}
				return x;
			}
		}

		public IEnumerable<IntVec3> SurroundingCells
		{
			get
			{
				return this.OccupiedRect().ExpandedBy(1).EdgeCells;
			}
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			var drawVehicle = new Task(() => Drawer.DrawAt(drawLoc));
			drawVehicle.RunSynchronously();
			statHandler.DrawHitbox(HighlightedComponent);
		}

		public void DrawAt(Vector3 drawLoc, float angle, bool flip = false)
		{
			var drawVehicle = new Task(() => Drawer.renderer.RenderPawnAt(drawLoc, angle));
			drawVehicle.RunSynchronously();
		}

		public override void Notify_ColorChanged()
		{
			ResetGraphicCache();
			Drawer.renderer.graphics.ResolveAllGraphics();
			base.Notify_ColorChanged();
		}

		internal void ResetGraphicCache()
		{
			if(UnityData.IsInMainThread)
			{
				ResetMaskCache();
				var cannonComp = CompCannons;
				if (cannonComp != null)
				{
					foreach(VehicleTurret cannon in cannonComp.Cannons)
					{
						cannon.ResolveCannonGraphics(this, true);
					}
				}
			}
		}

		private void ResetMaskCache()
		{
			graphicInt = null;
			AccessTools.Field(typeof(GraphicData), "cachedGraphic").SetValue(ageTracker.CurKindLifeStage.bodyGraphicData, null);
		}

		public void UpdateRotationAndAngle()
		{
			UpdateRotation();
			UpdateAngle();
		}

		public void UpdateRotation()
		{
			if (vPather.nextCell == Position)
			{
				return;
			}
			IntVec3 intVec = vPather.nextCell - Position;
			if (intVec.x > 0)
			{
				Rotation = Rot4.East;
			}
			else if (intVec.x < 0)
			{
				Rotation = Rot4.West;
			}
			else if (intVec.z > 0)
			{
				Rotation = Rot4.North;
			}
			else
			{
				Rotation = Rot4.South;
			}
		}

		public void UpdateAngle()
		{
			if (vPather.Moving)
			{
				IntVec3 c = vPather.nextCell - Position;
				if (c.x > 0 && c.z > 0)
				{
					angle = -45f;
				}
				else if (c.x > 0 && c.z < 0)
				{
					angle = 45f;
				}
				else if (c.x < 0 && c.z < 0)
				{
					angle = -45f;
				}
				else if (c.x < 0 && c.z > 0)
				{
					angle = 45f;
				}
				else
				{
					angle = 0f;
				}
			}
		}

		public override void DrawGUIOverlay()
		{
			Drawer.ui.DrawPawnGUIOverlay();
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			if (CanMove)
			{
				foreach (Gizmo gizmo in GizmoHelper.DraftGizmos(drafter))
				{
					yield return gizmo;
				}
			}
			if (!Dead)
			{
				if(!cargoToLoad.NullOrEmpty())
				{
					if (!cargoToLoad.NotNullAndAny(x => x.AnyThing != null && x.CountToTransfer > 0 && !inventory.innerContainer.Contains(x.AnyThing)))
					{
						cargoToLoad.Clear();
					}
					else
					{
						Command_Action cancelLoad = new Command_Action
						{
							defaultLabel = "DesignatorCancel".Translate(),
							icon = VehicleTex.CancelPackCargoIcon,
							action = delegate ()
							{
								Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(this, ReservationType.LoadVehicle);
								cargoToLoad.Clear();
							}
						};
						yield return cancelLoad;
					}
				}
				else
				{
					Command_Action loadShip = new Command_Action
					{
						defaultLabel = "LoadShip".Translate(),
						icon = VehicleTex.PackCargoIcon,
						action = delegate ()
						{
							Find.WindowStack.Add(new Dialog_LoadCargo(this));
						}
					};
					yield return loadShip;
				}
			}

			if (Prefs.DevMode)
			{
				yield return new Command_Action
				{
					defaultLabel = "Heal All Components",
					action = delegate ()
					{
						statHandler.components.ForEach(c => c.HealComponent(float.MaxValue));
					}
				};
			}

			foreach (ThingComp comp in AllComps)
			{
				foreach (Gizmo gizmo in comp.CompGetGizmosExtra())
				{
					yield return gizmo;
				}
			}

			if(!Dead)
			{
				if (!Drafted)
				{
					Command_Action unloadAll = new Command_Action
					{
						defaultLabel = "DisembarkAll".Translate(),
						icon = VehicleTex.UnloadAll,
						action = delegate ()
						{
							DisembarkAll();
							drafter.Drafted = false;
						},
						hotKey = KeyBindingDefOf.Misc2
					};
					yield return unloadAll;
				
					foreach (VehicleHandler handler in handlers)
					{
						for (int i = 0; i < handler.handlers.Count; i++)
						{
							Pawn currentPawn = handler.handlers.InnerListForReading[i];
							Command_Action unload = new Command_Action();
							unload.defaultLabel = "DisembarkSingle".Translate(currentPawn.LabelShort);
							unload.icon = VehicleTex.UnloadPassenger;
							unload.action = delegate ()
							{
								DisembarkPawn(currentPawn);
							};
							yield return unload;
						}
					}
					if (SettingsCache.TryGetValue(VehicleDef, typeof(VehicleProperties), "fishing", VehicleDef.properties.fishing) && FishingCompatibility.fishingActivated)
					{
						Command_Toggle fishing = new Command_Toggle
						{
							defaultLabel = "BoatFishing".Translate(),
							defaultDesc = "BoatFishingDesc".Translate(),
							icon = VehicleTex.FishingIcon,
							isActive = (() => currentlyFishing),
							toggleAction = delegate ()
							{
								currentlyFishing = !currentlyFishing;
							}
						};
						yield return fishing;
					}
				}
				if (this.GetLord()?.LordJob is LordJob_FormAndSendVehicles)
				{
					Command_Action forceCaravanLeave = new Command_Action
					{
						defaultLabel = "ForceLeaveCaravan".Translate(),
						defaultDesc = "ForceLeaveCaravanDesc".Translate(),
						icon = VehicleTex.CaravanIcon,
						action = delegate ()
						{
							(this.GetLord().LordJob as LordJob_FormAndSendVehicles).ForceCaravanLeave = true;
							Messages.Message("ForceLeaveConfirmation".Translate(), MessageTypeDefOf.TaskCompletion);
						}
					};
					yield return forceCaravanLeave;
				}
			}
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
		{
			if (selPawn is null)
			{
				yield break;
			}

			if (!selPawn.RaceProps.ToolUser)
			{
				yield break;
			}
			if (!selPawn.CanReserveAndReach(this, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, false))
			{
				yield break;
			}
			if (movementStatus is VehicleMovementStatus.Offline)
			{
				yield break;
			}
			foreach (VehicleHandler handler in handlers)
			{
				if(handler.AreSlotsAvailable)
				{
					FloatMenuOption opt = new FloatMenuOption("EnterVehicle".Translate(LabelShort, handler.role.label, (handler.role.slots - (handler.handlers.Count + 
						Map.GetCachedMapComponent<VehicleReservationManager>().GetReservation<VehicleHandlerReservation>(this)?.ClaimantsOnHandler(handler) ?? 0)).ToString()), 
					delegate ()
					{
						Job job = new Job(JobDefOf_Vehicles.Board, this);
						GiveLoadJob(selPawn, handler);
						selPawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
						if (!selPawn.Spawned)
						{
							return;
						}
						selPawn.Map.GetCachedMapComponent<VehicleReservationManager>().Reserve<VehicleHandler, VehicleHandlerReservation>(this, selPawn, selPawn.CurJob, handler, 999);
					}, MenuOptionPriority.Default, null, null, 0f, null, null);
					yield return opt;
				}
			}
			if(statHandler.NeedsRepairs)
			{
				yield return new FloatMenuOption("RepairVehicle".Translate(LabelShort),
				delegate ()
				{
					Job job = new Job(JobDefOf_Vehicles.RepairVehicle, this);
					selPawn.jobs.TryTakeOrderedJob(job, JobTag.MiscWork);
				}, MenuOptionPriority.Default, null, null, 0f, null, null);
			}
		}

		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = true;
		}

		public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
		{
			IntVec3 position = PositionHeld;
			Rot4 rotation = Rotation;

			Map map = Map;
			Map mapHeld = MapHeld;
			bool flag = Spawned;
			bool worldPawn = this.IsWorldPawn();
			Caravan caravan = this.GetCaravan();
			ThingDef vehicleDef = VehicleDef.buildDef;

			VehicleBuilding thing = (VehicleBuilding)ThingMaker.MakeThing(vehicleDef);
			thing.SetFactionDirect(Faction);

			if (Current.ProgramState == ProgramState.Playing)
			{
				Find.Storyteller.Notify_PawnEvent(this, AdaptationEvent.Died, null);
			}
			if (flag && dinfo != null && dinfo.Value.Def.ExternalViolenceFor(this))
			{
				LifeStageUtility.PlayNearestLifestageSound(this, (LifeStageAge ls) => ls.soundDeath, 1f);
			}
			if (dinfo != null && dinfo.Value.Instigator != null)
			{
				Pawn pawn = dinfo.Value.Instigator as Pawn;
				if(pawn != null)
				{
					RecordsUtility.Notify_PawnKilled(this, pawn);
				}
			}

			if (this.GetLord() != null)
			{
				this.GetLord().Notify_PawnLost(this, PawnLostCondition.IncappedOrKilled, dinfo);
			}
			if (flag)
			{
				DropAndForbidEverything(false);
			}

			thing.vehicleReference = this;
			thing.HitPoints = thing.MaxHitPoints / 10;
			meleeVerbs.Notify_PawnKilled();
			if (flag)
			{
				if (map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterOceanDeep || map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterDeep)
				{
					IntVec3 lookCell = Position;
					string textPawnList = "";
					foreach (Pawn p in AllPawnsAboard)
					{
						textPawnList += p.LabelShort + ". ";
					}
					Find.LetterStack.ReceiveLetter("ShipSunkLabel".Translate(), "ShipSunkDeep".Translate(LabelShort, textPawnList), LetterDefOf.NegativeEvent, new TargetInfo(lookCell, map, false), null, null);
					Destroy();
					return;
				}
				else
				{
					Find.LetterStack.ReceiveLetter("ShipSunkLabel".Translate(), "ShipSunkShallow".Translate(LabelShort), LetterDefOf.NegativeEvent, new TargetInfo(position, map, false), null, null);
					DisembarkAll();
					Destroy();
				}
				Thing t = GenSpawn.Spawn(thing, position, map, rotation, WipeMode.FullRefund, false);
			}
			if (this.GetAerialVehicle() is AerialVehicleInFlight aerialVehicle)
			{
				aerialVehicle.shouldCrash = true;
			}
			return;
		}
		
		public T GetSortedComp<T>() where T : ThingComp
		{
			for (int i = 0; i < cachedComps.Count; i++)
			{
				if (cachedComps[i] is T t)
				{
					cachedComps.CountIndex(i);
					return t;
				}
			}
			return default;
		}

		//REDO
		public IEnumerable<VehicleComp> GetAllAIComps()
		{
			foreach (VehicleComp comp in cachedComps.Where(c => c.GetType().IsAssignableFrom(typeof(VehicleComp))).Cast<VehicleComp>())
			{
				yield return comp;
			}
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

		public int TotalAllowedFor(JobDef jobDef)
		{
			if(VehicleDef.properties.vehicleJobLimitations.NotNullAndAny(v => v.defName == jobDef.defName))
			{
				return VehicleDef.properties.vehicleJobLimitations.Find(v => v.defName == jobDef.defName).maxWorkers;
			}
			return 1;
		}

		public void AddHandlers(List<VehicleHandler> handlerList)
		{
			if (handlerList.NullOrEmpty())
				return;
			foreach(VehicleHandler handler in handlerList)
			{
				VehicleHandler existingHandler = handlers.FirstOrDefault(h => h == handler);
				if(existingHandler != null)
				{
					existingHandler.role.cannonIds.AddRange(handler.role.cannonIds);
				}
				else
				{
					var handlerPermanent = new VehicleHandler(this, handler.role);
					handlers.Add(handlerPermanent);
				}
			}
		}

		public void RemoveHandlers(List<VehicleHandler> handlerList)
		{
			if (handlerList.NullOrEmpty())
				return;
			foreach(VehicleHandler handler in handlerList)
			{
				VehicleHandler vehicleHandler = handlers.FirstOrDefault(h => h == handler);
			}
		}

		public int AverageSkillOfCapablePawns(SkillDef skill)
		{
			int value = 0;
			foreach(Pawn p in AllCapablePawns)
				value += p.skills.GetSkill(skill).Level;
			value /= AllCapablePawns.Count;
			return value;
		}

		public List<VehicleHandler> GetAllHandlersMatch(HandlingTypeFlags? handlingTypeFlag, string cannonKey = "")
		{
			if (handlingTypeFlag is null)
			{
				return handlers.Where(h => h.role.handlingTypes.NullOrEmpty()).ToList();
			}
			return handlers.FindAll(x => x.role.handlingTypes.NotNullAndAny(h => h == handlingTypeFlag) && (handlingTypeFlag != HandlingTypeFlags.Cannon || (!x.role.cannonIds.NullOrEmpty() && x.role.cannonIds.Contains(cannonKey))));
		}

		public List<VehicleHandler> GetPriorityHandlers(HandlingTypeFlags? handlingTypeFlag = null)
		{
			return handlers.Where(h => h.role.handlingTypes.NotNullAndAny() && (handlingTypeFlag is null || h.role.handlingTypes.Contains(handlingTypeFlag.Value))).ToList();
		}

		public VehicleHandler GetHandlersMatch(Pawn pawn)
		{
			return handlers.FirstOrDefault(x => x.handlers.Contains(pawn));
		}

		public VehicleHandler NextAvailableHandler(Predicate<HandlingTypeFlags> flag = null, bool priorityHandlers = false)
		{
			IEnumerable<VehicleHandler> prioritizedHandlers = priorityHandlers ? handlers.Where(h => h.role.handlingTypes.NotNullAndAny()) : handlers;
			IEnumerable<VehicleHandler> filteredHandlers = flag is null ? prioritizedHandlers : prioritizedHandlers.Where(h => h.role.handlingTypes.NotNullAndAny(ht => flag(ht)));
			foreach (VehicleHandler handler in filteredHandlers)
			{
				if(handler.AreSlotsAvailable)
				{
					return handler;
				}
			}
			return null;
		}

		public void Rename()
		{
			if(StatNameable)
			{
				Find.WindowStack.Add(new Dialog_GiveVehicleName(this));
			}
		}
		
		public void ChangeColor()
		{
			Find.WindowStack.Add(new Dialog_ColorPicker(this));
		}

		public void MultiplePawnFloatMenuOptions(List<Pawn> pawns)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			FloatMenuOption opt1 = new FloatMenuOption("BoardShipGroup".Translate(LabelShort), delegate ()
			{
				List<IntVec3> cells = this.OccupiedRect().Cells.ToList();
				foreach (Pawn p in pawns)
				{
					if (cells.Contains(p.Position))
					{
						continue;
					}
					Job job = new Job(JobDefOf_Vehicles.Board, this);
					VehicleHandler handler = p.IsColonistPlayerControlled ? NextAvailableHandler() : handlers.FirstOrDefault(h => h.AreSlotsAvailable && h.role.handlingTypes.NullOrEmpty());
					GiveLoadJob(p, handler);
					Map.GetCachedMapComponent<VehicleReservationManager>().Reserve<VehicleHandler, VehicleHandlerReservation>(this, p, job, handler, 999);
					p.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
				}
			}, MenuOptionPriority.Default, null, null, 0f, null, null);
			FloatMenuOption opt2 = new FloatMenuOption("BoardShipGroupFail".Translate(LabelShort), null, MenuOptionPriority.Default, null, null, 0f, null, null)
			{
				Disabled = true
			};
			int r = 0;
			foreach(VehicleHandler h in handlers)
			{
				r += Map.GetCachedMapComponent<VehicleReservationManager>().GetReservation<VehicleHandlerReservation>(this)?.ClaimantsOnHandler(h) ?? 0;
			}
			options.Add(pawns.Count + r > SeatsAvailable ? opt2 : opt1);
			FloatMenuMulti floatMenuMap = new FloatMenuMulti(options, pawns, this, pawns[0].LabelCap, Verse.UI.MouseMapPosition())
			{
				givesColonistOrders = true
			};
			Find.WindowStack.Add(floatMenuMap);
		}

		public void GiveLoadJob(Pawn pawn, VehicleHandler handler)
		{
			if (bills != null && bills.Count > 0)
			{
				Bill_BoardShip bill = bills.FirstOrDefault(x => x.pawnToBoard == pawn);
				if (!(bill is null))
				{
					bill.handler = handler;
					return;
				}
			}
			bills.Add(new Bill_BoardShip(pawn, handler));
		}

		public void Notify_BoardedCaravan(Pawn pawnToBoard, ThingOwner handler)
		{
			if (!pawnToBoard.IsWorldPawn())
			{
				Log.Warning("Tried boarding Caravan with non-worldpawn");
			}

			if (pawnToBoard.holdingOwner != null)
			{
				pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, handler);
			}
			else
			{
				handler.TryAdd(pawnToBoard);
			}
		}

		public void Notify_Boarded(Pawn pawnToBoard, Map map = null)
		{
			if(bills != null && bills.Count > 0)
			{
				Bill_BoardShip bill = bills.FirstOrDefault(x => x.pawnToBoard == pawnToBoard);
				if(bill != null)
				{
					if(pawnToBoard.IsWorldPawn())
					{
						Log.Error("Tried boarding ship with world pawn. Use Notify_BoardedCaravan instead.");
						return;
					}
					if (pawnToBoard.Spawned)
					{
						pawnToBoard.DeSpawn(DestroyMode.Vanish);
					}
					if (bill.handler.handlers.TryAdd(pawnToBoard, true))
					{
						if(pawnToBoard != null)
						{
							if (map != null)
							{
								map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawnToBoard);
							}
							else
							{
								Map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawnToBoard);
							}
							Find.WorldPawns.PassToWorld(pawnToBoard, PawnDiscardDecideMode.Decide);
						}
					}
					else if(pawnToBoard.holdingOwner != null)
					{
						pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, bill.handler.handlers);
					}
					bills.Remove(bill);
				}
			}
		}

		public void DisembarkPawn(Pawn pawn)
		{
			if(!pawn.Spawned)
			{
				CellRect occupiedRect = this.OccupiedRect().ExpandedBy(1);
				IntVec3 loc = Position;
				Rand.PushState();
				IntVec3 newLoc = occupiedRect.EdgeCells.Where(c => GenGrid.InBounds(c, Map) && GenGrid.Standable(c, Map) && !c.GetThingList(Map).NotNullAndAny(t => t is Pawn)).RandomElementWithFallback(Position);
				Rand.PopState();
				if (occupiedRect.EdgeCells.Contains(newLoc))
				{
					loc = newLoc;
				}
				else if(!GenGrid.Standable(Position, Map))
				{
					Messages.Message("RejectDisembarkInvalidTile".Translate(), MessageTypeDefOf.RejectInput, false);
					return;
				}
				GenSpawn.Spawn(pawn, loc, MapHeld);
				if (this.GetLord() is Lord lord)
				{
					lord.AddPawn(pawn);
				}
			}
			RemovePawn(pawn);
			if (!AllPawnsAboard.NotNullAndAny() && outOfFoodNotified)
			{
				outOfFoodNotified = false;
			}
		}

		public void DisembarkAll()
		{
			var pawnsToDisembark = new List<Pawn>(AllPawnsAboard);
			if( !(pawnsToDisembark is null) && pawnsToDisembark.Count > 0)
			{
				if(this.GetCaravan() != null && !Spawned)
				{
					List<VehicleHandler> handlerList = handlers;
					for(int i = 0; i < handlerList.Count; i++)
					{
						VehicleHandler handler = handlerList[i];
						handler.handlers.TryTransferAllToContainer(this.GetCaravan().pawns, false);
					}
					return;
				}
				foreach(Pawn p in pawnsToDisembark)
				{
					DisembarkPawn(p);
				}
			}
		}

		public void RemovePawn(Pawn pawn)
		{
			for (int i = 0; i < handlers.Count; i++)
			{
				VehicleHandler handler = handlers[i];
				if (handler.handlers.Remove(pawn))
				{
					return;
				}
			}
		}

		//REDO
		public void DeadPawnReplace(Pawn pawn)
		{
			//NEEDS IMPLEMENTATION
			/*foreach(ShipHandler h in handlers)
			{
				if(h.handlers.InnerListForReading.Contains(pawn))
				{
					
				}
			}*/
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

		private void TrySatisfyPawnNeeds()
		{
			if(Spawned || this.IsCaravanMember())
			{
				foreach (Pawn p in AllPawnsAboard)
				{
					TrySatisfyPawnNeeds(p);
				}
			}
		}

		private void TrySatisfyPawnNeeds(Pawn pawn)
		{
			if(pawn.Dead) return;
			List<Need> allNeeds = pawn.needs.AllNeeds;
			int tile = this.IsCaravanMember() ? this.GetCaravan().Tile : Map.Tile;

			for(int i = 0; i < allNeeds.Count; i++)
			{
				Need need = allNeeds[i];
				if(need is Need_Rest)
				{
					if(CaravanNightRestUtility.RestingNowAt(tile) || GetHandlersMatch(pawn).role.handlingTypes.NullOrEmpty())
					{
						TrySatisfyRest(pawn, need as Need_Rest);
					}
				}
				else if (need is Need_Food)
				{
					if(!CaravanNightRestUtility.RestingNowAt(tile))
						TrySatisfyFood(pawn, need as Need_Food);
				}
				else if (need is Need_Chemical)
				{
					if(!CaravanNightRestUtility.RestingNowAt(tile))
						TrySatisfyChemicalNeed(pawn, need as Need_Chemical);
				}
				else if (need is Need_Joy)
				{
					if (!CaravanNightRestUtility.RestingNowAt(tile))
						TrySatisfyJoyNeed(pawn, need as Need_Joy);
				}
				else if(need is Need_Comfort)
				{
					need.CurLevel = 0.5f;
				}
				else if(need is Need_Outdoors)
				{
					need.CurLevel = 0.25f;
				}
			}
		}

		private void TrySatisfyRest(Pawn pawn, Need_Rest rest)
		{
			Building_Bed bed = (Building_Bed)inventory.innerContainer.InnerListForReading.Find(x => x is Building_Bed); // Reserve?
			float restValue = bed is null ? 0.75f : bed.GetStatValue(StatDefOf.BedRestEffectiveness, true);
			restValue *= pawn.GetStatValue(StatDefOf.RestRateMultiplier, true);
			if(restValue > 0)
				rest.CurLevel += 0.0057142857f * restValue;
		}

		private void TrySatisfyFood(Pawn pawn, Need_Food food)
		{
			if(food.CurCategory < HungerCategory.Hungry)
				return;

			if(TryGetBestFood(pawn, out Thing thing, out Pawn owner))
			{
				food.CurLevel += thing.Ingested(pawn, food.NutritionWanted);
				if(thing.Destroyed)
				{
					owner.inventory.innerContainer.Remove(thing);
					if(this.IsCaravanMember())
					{
						this.GetCaravan().RecacheImmobilizedNow();
						this.GetCaravan().RecacheDaysWorthOfFood();
					}
				}
				if(!outOfFoodNotified && !TryGetBestFood(pawn, out _, out Pawn _))
				{
					Messages.Message("ShipOutOfFood".Translate(LabelShort), this, MessageTypeDefOf.NegativeEvent, false);
					outOfFoodNotified = true;
				}
			}
		}

		public bool TryGetBestFood(Pawn forPawn, out Thing food, out Pawn owner)
		{
			List<Thing> list = inventory.innerContainer.InnerListForReading;
			Thing thing = null;
			float num = 0f;
			foreach (Thing foodItem in list)
			{
				if (CanEatForNutrition(foodItem, forPawn))
				{
					float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(foodItem, forPawn);
					if (thing is null || foodScore > num)
					{
						thing = foodItem;
						num = foodScore;
					}
				}
			}
			if (this.IsCaravanMember())
			{
				foreach(Thing foodItem2 in CaravanInventoryUtility.AllInventoryItems(this.GetCaravan()))
				{
					if(CanEatForNutrition(foodItem2, forPawn))
					{
						float foodScore = CaravanPawnsNeedsUtility.GetFoodScore(foodItem2, forPawn);
						if(thing is null || foodScore > num)
						{
							thing = foodItem2;
							num = foodScore;
						}
					}
				}
			}
			
			if(thing != null)
			{
				food = thing;
				owner = this.IsCaravanMember() ? CaravanInventoryUtility.GetOwnerOf(this.GetCaravan(), thing) : this;
				return true;
			}
			food = null;
			owner = null;
			return false;
		}

		private void TrySatisfyChemicalNeed(Pawn pawn, Need_Chemical chemical)
		{
			if (chemical.CurCategory >= DrugDesireCategory.Satisfied)
			{
				return;
			}

			if (TryGetDrugToSatisfyNeed(pawn, chemical, out Thing drug, out Pawn owner))
			{
				IngestDrug(pawn, drug, owner);
			}
		}

		public void IngestDrug(Pawn pawn, Thing drug, Pawn owner)
		{
			float num = drug.Ingested(pawn, 0f);
			Need_Food food = pawn.needs.food;
			if(food != null)
			{
				food.CurLevel += num;
			}
			if(drug.Destroyed)
			{
				owner.inventory.innerContainer.Remove(drug);
			}
		}
		public bool TryGetDrugToSatisfyNeed(Pawn forPawn, Need_Chemical chemical, out Thing drug, out Pawn owner)
		{
			Hediff_Addiction addictionHediff = chemical.AddictionHediff;
			if(addictionHediff is null)
			{
				drug = null;
				owner = null;
				return false;
			}
			List<Thing> list = inventory.innerContainer.InnerListForReading;

			Thing thing = null;
			foreach(Thing t in list)
			{
				if(t.IngestibleNow && t.def.IsDrug)
				{
					CompDrug compDrug = t.TryGetComp<CompDrug>();
					if(compDrug != null && compDrug.Props.chemical != null)
					{
						if(compDrug.Props.chemical.addictionHediff == addictionHediff.def)
						{
							if(forPawn.drugs is null || forPawn.drugs.CurrentPolicy[t.def].allowedForAddiction || forPawn.story is null || forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
							{
								thing = t;
								break;
							}
						}
					}
				}
			}
			if (this.IsCaravanMember())
			{
				foreach (Thing t in CaravanInventoryUtility.AllInventoryItems(this.GetCaravan()))
				{
					if (t.IngestibleNow && t.def.IsDrug)
					{
						CompDrug compDrug = t.TryGetComp<CompDrug>();
						if (compDrug != null && compDrug.Props.chemical != null)
						{
							if (compDrug.Props.chemical.addictionHediff == addictionHediff.def)
							{
								if (forPawn.drugs is null || forPawn.drugs.CurrentPolicy[t.def].allowedForAddiction || forPawn.story is null || forPawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0)
								{
									thing = t;
									break;
								}
							}
						}
					}
				}
			}
			if(thing != null)
			{
				drug = thing;
				owner = this.IsCaravanMember() ? CaravanInventoryUtility.GetOwnerOf(this.GetCaravan(), thing) : this;
				return true;
			}
			drug = null;
			owner = null;
			return false;
		}

		public static bool CanEatForNutrition(Thing item, Pawn forPawn)
		{
			return item.IngestibleNow && item.def.IsNutritionGivingIngestible && forPawn.WillEat(item, null) && item.def.ingestible.preferability > FoodPreferability.NeverForNutrition &&
				(!item.def.IsDrug || !forPawn.IsTeetotaler()) && (!forPawn.RaceProps.Humanlike || forPawn.needs.food.CurCategory >= HungerCategory.Starving || item.def.ingestible.preferability >
				FoodPreferability.DesperateOnlyForHumanlikes);
		}

		private void TrySatisfyJoyNeed(Pawn pawn, Need_Joy joy)
		{
			if(pawn.IsHashIntervalTick(1250))
			{
				float num = vPather.MovingNow ? 4E-05f : 4E-3f; //Incorporate 'shifts'
				if (num <= 0f)
					return;
				num *= 1250f;
				List<JoyKindDef> tmpJoyList = GetAvailableJoyKindsFor(pawn);
				if (!tmpJoyList.TryRandomElementByWeight((JoyKindDef x) => 1f - Mathf.Clamp01(pawn.needs.joy.tolerances[x]), out JoyKindDef joyKind))
					return;
				joy.GainJoy(num, joyKind);
				tmpJoyList.Clear();
			}
		}

		public List<JoyKindDef> GetAvailableJoyKindsFor(Pawn p)
		{
			List<JoyKindDef> outJoyKinds = new List<JoyKindDef>();
			if (!p.needs.joy.tolerances.BoredOf(JoyKindDefOf.Meditative))
				outJoyKinds.Add(JoyKindDefOf.Meditative);
			if(!p.needs.joy.tolerances.BoredOf(JoyKindDefOf.Social))
			{
				int num = 0;
				foreach(Pawn targetpawn in AllPawnsAboard)
				{
					if(!targetpawn.Downed && targetpawn.RaceProps.Humanlike && !targetpawn.InMentalState)
					{
						num++;
					}
				}
				if (num >= 2)
					outJoyKinds.Add(JoyKindDefOf.Social);
			}
			return outJoyKinds;
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
				bills = new List<Bill_BoardShip>();
			}

			navigationCategory = VehicleDef.defaultNavigation;

			if (VehicleDef.properties.roles != null && VehicleDef.properties.roles.Count > 0)
			{
				foreach (VehicleRole role in VehicleDef.properties.roles)
				{
					handlers.Add(new VehicleHandler(this, role));
				}
			}

			cachedComps = new SelfOrderingList<ThingComp>(AllComps);
			compTickers.Clear();
			foreach (ThingComp comp in AllComps)
			{
				if (comp.GetType().GetMethod("CompTick").MethodImplemented())
				{
					compTickers.Add(comp);
				}
			}
		}

		private void InitializeStats()
		{
			armorPoints = StatArmor;
			cargoCapacity = StatCargo;
			moveSpeedModifier = 0f;
		}

		public override void DrawExtraSelectionOverlays()
		{
			if (vPather.curPath != null)
			{
				vPather.curPath.DrawPath(this);
			}
			RenderHelper.DrawLinesBetweenTargets(this, jobs.curJob, jobs.jobQueue);
		}

		public override TipSignal GetTooltip()
		{
			return base.GetTooltip();
		}

		public override void PostMapInit()
		{
			base.PostMapInit();
			vPather.TryResumePathingAfterLoading();
		}

		public virtual void PostGenerationSetup()
		{
			InitializeVehicle();
			InitializeStats();
			ageTracker.AgeBiologicalTicks = 0;
			ageTracker.AgeChronologicalTicks = 0;
			ageTracker.BirthAbsTicks = 0;
			health.Reset();
			statHandler.InitializeComponents();
		}

		private void InitializeHitbox()
		{
			Hitbox = CellRect.CenteredOn(IntVec3.Zero, VehicleDef.Size.x, VehicleDef.Size.z);
			statHandler.InitializeHitboxCells();
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				vPather.ResetToCurrentPosition();
				if (DrawColor == Color.white)
				{
					DrawColor = ageTracker.CurKindLifeStage.bodyGraphicData.color;
				}
				if (DrawColorTwo == Color.white)
				{
					DrawColorTwo = ageTracker.CurKindLifeStage.bodyGraphicData.colorTwo;
				}
				if (DrawColorThree == Color.white)
				{
					DrawColorThree = (ageTracker.CurKindLifeStage.bodyGraphicData as GraphicDataRGB).colorThree;
				}
			}
			else
			{
				cachedComps = new SelfOrderingList<ThingComp>(AllComps);
				compTickers.Clear();
				foreach (ThingComp comp in AllComps)
				{
					if (comp.GetType().GetMethod("CompTick").MethodImplemented())
					{
						compTickers.Add(comp);
					}
				}
			}
			if (Faction != Faction.OfPlayer)
			{
				drafter.Drafted = true;
				CompCannons cannonComp = CompCannons;
				if(cannonComp != null)
				{
					foreach(var cannon in cannonComp.Cannons)
					{
						cannon.autoTargeting = true;
						cannon.AutoTarget = true;
					}
				}
			}
			inFlight = false;
			ResetGraphicCache();
			Drawer.Notify_Spawned();
			InitializeHitbox();
			Map.GetCachedMapComponent<VehiclePositionManager>().ClaimPosition(this);
			Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleSpawned(this);
		}

		public float VehicleMovedPercent()
		{
			if (!vPather.Moving)
			{
				return 0f;
			}
			if (vPather.BuildingBlockingNextPathCell() != null)
			{
				return 0f;
			}
			if (vPather.NextCellDoorToWaitForOrManuallyOpen() != null)
			{
				return 0f;
			}
			if (vPather.WillCollideWithPawnOnNextPathCell())
			{
				return 0f;
			}
			return 1f - vPather.nextCellCostLeft / vPather.nextCellCostTotal;
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			Map.GetCachedMapComponent<VehiclePositionManager>().ReleaseClaimed(this);
			Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(this);
			base.DeSpawn(mode);
			if (vPather != null)
			{
				vPather.StopDead();
			}
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			if (Spawned)
			{
				Map.GetCachedMapComponent<VehiclePositionManager>().ReleaseClaimed(this);
				Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(this);
				Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleDespawned(this);
			}
			base.Destroy(mode);
		}

		public override void Tick()
		{
			BaseTickOptimized();
			TickAllComps();
			if (Spawned)
			{
				vPather.PatherTick();
			}
			if (Faction != Faction.OfPlayer)
			{
				vehicleAI?.AITick();
			}
			if (this.IsHashIntervalTick(150) && AllPawnsAboard.Any())
			{
				TrySatisfyPawnNeeds();
			}
		}

		protected virtual void TickAllComps()
		{
			if (!compTickers.NullOrEmpty())
			{
				foreach (ThingComp comp in compTickers)
				{
					comp.CompTick();
				}
			}
		}

		protected virtual void BaseTickOptimized()
		{
			if (Find.TickManager.TicksGame % 250 == 0)
			{
				TickRare();
				statHandler.explosionsAffectingVehicle.Clear();
			}
			bool suspended = Suspended;
			if (!suspended)
			{
				if (Spawned)
				{
					stances.StanceTrackerTick();
					verbTracker.VerbsTick();
				}
				if (Spawned)
				{
					natives.NativeVerbsTick();
				}
				if (Spawned)
				{
					jobs.JobTrackerTick();
				}
				if (Spawned)
				{
					Drawer.VehicleDrawerTick();
					rotationTracker.RotationTrackerTick();
				}
				health.HealthTick();
				if (!Dead)
				{
					//mindState.MindStateTick();
					//carryTracker.CarryHandsTick();
				}
			}
			if (!suspended)
			{
				if (equipment != null)
				{
					equipment.EquipmentTrackerTick();
				}
				if (apparel != null)
				{
					apparel.ApparelTrackerTick();
				}
				if (interactions != null && Spawned)
				{
					interactions.InteractionsTrackerTick();
				}
				if (caller != null)
				{
					caller.CallTrackerTick();
				}
				if (skills != null)
				{
					skills.SkillsTick();
				}
				if (abilities != null)
				{
					abilities.AbilitiesTick();
				}
				if (inventory != null)
				{
					inventory.InventoryTrackerTick();
				}
				if (drafter != null)
				{
					drafter.DraftControllerTick();
				}
				if (relations != null)
				{
					relations.RelationsTrackerTick();
				}
				if (royalty != null && ModsConfig.RoyaltyActive)
				{
					royalty.RoyaltyTrackerTick();
				}
				ageTracker.AgeTick();
				records.RecordsTick();
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref vPather, "vPather", new object[] { this });
			Scribe_Deep.Look(ref statHandler, "statHandler", new object[] { this });

			Scribe_Values.Look(ref angle, "angle");
			Scribe_Values.Look(ref inFlight, "inFlight");

			Scribe_Values.Look(ref color1, "color1", Color.white);
			Scribe_Values.Look(ref color2, "color2", Color.white);

			Scribe_Defs.Look(ref pattern, "pattern");

			Scribe_Values.Look(ref movementStatus, "movingStatus", VehicleMovementStatus.Online);
			Scribe_Values.Look(ref navigationCategory, "navigationCategory", NavigationCategory.Opportunistic);
			Scribe_Values.Look(ref currentlyFishing, "currentlyFishing", false);
			Scribe_Values.Look(ref showAllItemsOnMap, "showAllItemsOnMap");

			Scribe_Collections.Look(ref cargoToLoad, "cargoToLoad");

			Scribe_Values.Look(ref armorPoints, "armorPoints");
			Scribe_Values.Look(ref cargoCapacity, "cargoCapacity");
			Scribe_Values.Look(ref moveSpeedModifier, "moveSpeed");

			Scribe_Collections.Look(ref handlers, "handlers", LookMode.Deep);
			Scribe_Collections.Look(ref bills, "bills", LookMode.Deep);
		}
	}
}
