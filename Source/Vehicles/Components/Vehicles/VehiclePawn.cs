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
	public class VehiclePawn : Pawn, IInspectable, IEventManager<VehicleEventDef>
	{
		//Saved components
		public Vehicle_PathFollower vPather;
		public VehicleStatHandler statHandler;

		[Unsaved]
		public Vehicle_DrawTracker vDrawer;
		[Unsaved]
		public VehicleAI vehicleAI;
		[Unsaved]
		public VehicleGraphicOverlay graphicOverlay;
		[Unsaved]
		public VehicleSustainers sustainers;
		
		public SharedJob sharedJob;

		public PatternData patternData;
		public RetextureDef retexture;

		public List<Bill_BoardVehicle> bills = new List<Bill_BoardVehicle>();
		public List<VehicleHandler> handlers = new List<VehicleHandler>();

		public bool currentlyFishing = false;
		public bool draftStatusChanged = false;
		public bool beached = false;
		public bool showAllItemsOnMap = false;

		private float angle = 0f; /* -45 is left, 45 is right : relative to Rot4 direction*/

		private Graphic_Vehicle graphicInt;

		private SelfOrderingList<ThingComp> cachedComps = new SelfOrderingList<ThingComp>();
		private List<ThingComp> compTickers = new List<ThingComp>();

		private CompVehicleTurrets compVehicleTurrets;
		private CompFueledTravel compFuel;
		private CompUpgradeTree compUpgradeTree;
		private CompVehicleLauncher compVehicleLauncher;

		public List<TransferableOneWay> cargoToLoad;
		private bool outOfFoodNotified = false;

		public VehicleMovementStatus movementStatus = VehicleMovementStatus.Online;
		public NavigationCategory navigationCategory = NavigationCategory.Opportunistic;

		public PatternData patternToPaint;

		internal VehicleComponent HighlightedComponent { get; set; }
		public CellRect Hitbox { get; private set; }
		public float CachedAngle { get; set; }

		private List<VehicleHandler> HandlersWithPawnRenderer { get; set; }

		public IntVec3 FollowerCell { get; protected set; }

		public Dictionary<VehicleEventDef, EventTrigger> EventRegistry { get; set; }

		public VehiclePermissions MovementPermissions => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), nameof(VehicleDef.vehicleMovementPermissions), VehicleDef.vehicleMovementPermissions);
		public bool CanMove => GetStatValue(VehicleStatDefOf.MoveSpeed) > 0.1f && MovementPermissions > VehiclePermissions.NotAllowed && movementStatus == VehicleMovementStatus.Online;
		public bool CanMoveFinal => CanMove && (PawnCountToOperateFullfilled || VehicleMod.settings.debug.debugDraftAnyShip);
		public bool PawnCountToOperateFullfilled => PawnCountToOperateLeft <= 0;

		public VehicleDef VehicleDef => def as VehicleDef;

		public bool Nameable => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), nameof(VehicleDef.nameable), VehicleDef.nameable);

		public bool CanPaintNow => patternToPaint != null;

		public override Vector3 DrawPos => Drawer.DrawPos;

		public VehiclePawn()
		{
		}

		public CompVehicleTurrets CompVehicleTurrets
		{
			get
			{
				if (compVehicleTurrets is null)
				{
					compVehicleTurrets = GetSortedComp<CompVehicleTurrets>();
				}
				return compVehicleTurrets;
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

		public Rot8 FullRotation
		{
			get
			{
				return new Rot8(Rotation, Angle);
			}
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
					if (retexture != null)
					{
						graphicData.CopyFrom(retexture.graphicData);
					}
					else
					{
						graphicData.CopyFrom(VehicleDef.graphicData);
					}
					graphicData.color = patternData.color;
					graphicData.colorTwo = patternData.colorTwo;
					graphicData.colorThree = patternData.colorThree;
					graphicData.tiles = patternData.tiles;
					graphicData.displacement = patternData.displacement;
					graphicData.pattern = patternData.patternDef;
					graphicInt = graphicData.Graphic as Graphic_Vehicle;
				}
				return graphicInt;
			}
		}

		public override Color DrawColor
		{
			get
			{
				return Pattern?.properties?.colorOne ?? patternData.color;
			}
			set
			{
				patternData.color = value;
			}
		}

		public new Color DrawColorTwo
		{
			get
			{
				return Pattern?.properties?.colorTwo ?? patternData.colorTwo;
			}
			set
			{
				patternData.colorTwo = value;
			}
		}

		public Color DrawColorThree
		{
			get
			{
				return Pattern?.properties?.colorThree ?? patternData.colorThree;
			}
			set
			{
				patternData.colorThree = value;
			}
		}

		public Vector2 Displacement
		{
			get
			{
				return patternData.displacement;
			}
			set
			{
				patternData.displacement = value;
			}
		}

		public float Tiles
		{
			get
			{
				return patternData.tiles;
			}
			set
			{
				patternData.tiles = value;
			}
		}

		public PatternDef Pattern
		{
			get
			{
				return patternData.patternDef ?? VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(VehicleDef.defName, VehicleGraphic.DataRGB)?.patternDef ?? PatternDefOf.Default;
			}
			set
			{
				patternData.patternDef = value;
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
				if (handlers != null && handlers.Count > 0)
				{
					foreach (VehicleHandler handler in handlers)
					{
						if (handler.handlers != null && handler.handlers.Count > 0)
						{
							pawnsOnShip.AddRange(handler.handlers);
						}
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
				List<Pawn> passengers = new List<Pawn>();
				if (!handlers.NullOrEmpty())
				{
					foreach (VehicleHandler handler in handlers)
					{
						if(handler.role.handlingTypes.NullOrEmpty())
						{
							passengers.AddRange(handler.handlers);
						}
					}
				}
				return passengers;
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

		public IEnumerable<IntVec3> InhabitedCells(int expandedBy = 0)
		{
			return InhabitedCellsProjected(Position, FullRotation, def.Size, expandedBy);
		}

		public IEnumerable<IntVec3> InhabitedCellsProjected(IntVec3 projectedCell, Rot8 rot, IntVec2? size = null, int expandedBy = 0)
		{
			size ??= def.Size;
			int sizeX = size.Value.x;
			int sizeZ = size.Value.z;
			if (!rot.IsValid)
			{
				int largestSize = Mathf.Max(sizeX, sizeZ);
				sizeX = largestSize;
				sizeZ = largestSize;
			}
			else if (rot.IsHorizontal)
			{
				sizeX = size.Value.z;
				sizeZ = size.Value.x;
			}
			return CellRect.CenteredOn(projectedCell, sizeX, sizeZ).ExpandedBy(expandedBy).Cells; //REDO FOR DIAGONALS
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

		public float GetStatValue(VehicleStatDef statDef)
		{
			//Needs caching in VehicleStatHandler
			return statDef.Worker.GetValue(this);
		}

		public void MarkForRecache(VehicleStatDef statDef)
		{
			throw new NotImplementedException();
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			var drawVehicle = new Task( delegate()
			{
				Drawer.DrawAt(drawLoc);
				foreach (VehicleHandler handler in HandlersWithPawnRenderer)
				{
					handler.RenderPawns();
				}
			});
			drawVehicle.RunSynchronously();
			statHandler.DrawHitbox(HighlightedComponent);
		}

		public void DrawAt(Vector3 drawLoc, float angle, bool flip = false)
		{
			bool northSouthRotation = VehicleGraphic.EastDiagonalRotated || VehicleGraphic.WestDiagonalRotated;
			var drawVehicle = new Task( delegate()
			{
				Drawer.renderer.RenderPawnAt(drawLoc, angle, northSouthRotation);
				foreach (VehicleHandler handler in HandlersWithPawnRenderer)
				{
					handler.RenderPawns();
				}
			});
			drawVehicle.RunSynchronously();
		}

		public void ResetRenderStatus()
		{
			HandlersWithPawnRenderer = handlers.Where(h => h.role.pawnRenderer != null).ToList();
		}

		public override void Notify_ColorChanged()
		{
			ResetGraphicCache();
			Drawer.renderer.graphics.ResolveAllGraphics();
			base.Notify_ColorChanged();
		}

		internal void ResetGraphicCache()
		{
			if (UnityData.IsInMainThread)
			{
				ResetMaskCache();
				var cannonComp = CompVehicleTurrets;
				if (cannonComp != null)
				{
					foreach (VehicleTurret cannon in cannonComp.turrets)
					{
						cannon.ResolveCannonGraphics(patternData, true);
					}
				}
			}
		}

		private void ResetMaskCache()
		{
			graphicInt = null;
			VehicleDef.graphicData.Init();
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
			if (MovementPermissions > VehiclePermissions.NotAllowed)
			{
				foreach (Gizmo gizmo in this.VehicleGizmos())
				{
					yield return gizmo;
				}
			}

			foreach (Type type in VehicleDef.designatorTypes)
			{
				Designator designator = DesignatorCache.Get(type);
				if (designator != null)
				{
					yield return designator;
				}
			}

			if (!cargoToLoad.NullOrEmpty())
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
						icon = VehicleDef.CancelCargoIcon,
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
					defaultLabel = "VF_LoadCargo".Translate(),
					icon = VehicleDef.LoadCargoIcon,
					action = delegate ()
					{
						Find.WindowStack.Add(new Dialog_LoadCargo(this));
					}
				};
				yield return loadShip;
			}

			if (!Drafted)
			{
				Command_Action unloadAll = new Command_Action
				{
					defaultLabel = "VF_DisembarkAllPawns".Translate(),
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
						Command_Action_PawnDrawer unloadAction = new Command_Action_PawnDrawer();
						unloadAction.defaultLabel = "VF_DisembarkSinglePawn".Translate(currentPawn.LabelShort);
						unloadAction.pawn = currentPawn;
						unloadAction.action = delegate ()
						{
							DisembarkPawn(currentPawn);
						};
						yield return unloadAction;
					}
				}
				if (SettingsCache.TryGetValue(VehicleDef, typeof(VehicleProperties), nameof(VehicleProperties.fishing), VehicleDef.properties.fishing) && Compatibility_VEFishing.Active)
				{
					Command_Toggle fishing = new Command_Toggle
					{
						defaultLabel = "VF_StartFishing".Translate(),
						defaultDesc = "VF_StartFishingDesc".Translate(),
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
			if (this.GetLord()?.LordJob is LordJob_FormAndSendVehicles formCaravanLordJob)
			{
				Command_Action forceCaravanLeave = new Command_Action
				{
					defaultLabel = "ForceLeaveCaravan".Translate(),
					defaultDesc = "ForceLeaveCaravanDesc".Translate(),
					icon = VehicleTex.CaravanIcon,
					activateSound = SoundDefOf.Tick_Low,
					action = delegate ()
					{
						formCaravanLordJob.ForceCaravanLeave();
						Messages.Message("ForceLeaveConfirmation".Translate(), MessageTypeDefOf.TaskCompletion);
					}
				};
				yield return forceCaravanLeave;

				Command_Action cancelCaravan = new Command_Action
				{
					defaultLabel = "CommandCancelFormingCaravan".Translate(),
					defaultDesc = "CommandCancelFormingCaravanDesc".Translate(),
					icon = TexCommand.ClearPrioritizedWork,
					activateSound = SoundDefOf.Tick_Low,
					action = delegate ()
					{
						CaravanFormingUtility.StopFormingCaravan(formCaravanLordJob.lord);
					},
					hotKey = KeyBindingDefOf.Designator_Cancel
				};
				yield return cancelCaravan;
			}

			foreach (ThingComp comp in AllComps)
			{
				foreach (Gizmo gizmo in comp.CompGetGizmosExtra())
				{
					yield return gizmo;
				}
			}

			if (Prefs.DevMode && Spawned)
			{
				yield return new Command_Action
				{
					defaultLabel = "Destroy Random Component",
					action = delegate ()
					{
						VehicleComponent component = statHandler.components.Where(component => !component.props.categories.Contains(VehicleStatDefOf.BodyIntegrity)).RandomOrDefault();
						component ??= statHandler.components.RandomElement();
						component.TakeDamage(this, new DamageInfo(DamageDefOf.Bite, float.MaxValue), Position);
						Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleTookDamage(this);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "Damage Random Component",
					action = delegate ()
					{
						VehicleComponent component = statHandler.components.RandomElement();
						component.TakeDamage(this, new DamageInfo(DamageDefOf.Bite, component.health * 0.5f), Position);
						Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleTookDamage(this);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "Heal All Components",
					action = delegate ()
					{
						statHandler.components.ForEach(c => c.HealComponent(float.MaxValue));
						Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleRepaired(this);
					}
				};
				yield return new Command_Action()
				{
					defaultLabel = "Give Random Pawn MentalState",
					action = delegate()
					{
						if (AllPawnsAboard.TryRandomElement(out Pawn result))
						{
							foreach (MentalStateDef mentalState in DefDatabase<MentalStateDef>.AllDefs)
							{
								if (result.mindState.mentalStateHandler.TryStartMentalState(mentalState, "testing"))
								{
									break;
								}
								else
								{
									Log.Warning($"Failed to execute {mentalState} inside vehicles.");
								}
							}
						}
					}
				};
				yield return new Command_Action()
				{
					defaultLabel = "Kill Random Pawn",
					action = delegate ()
					{
						Pawn pawn = AllPawnsAboard.RandomElementWithFallback(null);
						pawn?.Kill(null);
					}
				};
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
				if (handler.AreSlotsAvailable)
				{
					VehicleReservationManager reservationManager = Map.GetCachedMapComponent<VehicleReservationManager>();
					FloatMenuOption opt = new FloatMenuOption("EnterVehicle".Translate(LabelShort, handler.role.label, (handler.role.slots - (handler.handlers.Count +
						reservationManager.GetReservation<VehicleHandlerReservation>(this)?.ClaimantsOnHandler(handler) ?? 0)).ToString()), delegate ()
					{
						Job job = new Job(JobDefOf_Vehicles.Board, this);
						GiveLoadJob(selPawn, handler);
						selPawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
						if (!selPawn.Spawned)
						{
							return;
						}
						reservationManager.Reserve<VehicleHandler, VehicleHandlerReservation>(this, selPawn, selPawn.CurJob, handler);
					});
					yield return opt;
				}
			}
			if (statHandler.NeedsRepairs)
			{
				yield return new FloatMenuOption("VF_RepairVehicle".Translate(LabelShort), delegate ()
				{
					Job job = new Job(JobDefOf_Vehicles.RepairVehicle, this);
					selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				});
			}
			if (patternToPaint != null)
			{
				yield return new FloatMenuOption("VF_PaintVehicle".Translate(LabelShort), delegate ()
				{
					Job job = new Job(JobDefOf_Vehicles.PaintVehicle, this);
					selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				});
			}
		}

		//Todo - clean up
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

		public bool HasPawn(Pawn pawn)
		{
			return AllPawnsAboard.Contains(pawn);
		}

		public virtual void TakeDamage(DamageInfo dinfo, IntVec2 cell, bool explosive = false)
		{
			statHandler.TakeDamage(dinfo, cell, explosive);
		}

		///Divert damage calculations to <see cref="VehicleStatHandler"/>
		public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			statHandler.TakeDamage(dinfo);
			absorbed = true;
		}

		public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)
		{
			Kill(dinfo, spawnWreckage: false);
		}

		public virtual void Kill(DamageInfo? dinfo, DestroyMode destroyMode = DestroyMode.KillFinalize, bool spawnWreckage = false)
		{
			IntVec3 position = PositionHeld;
			Rot4 rotation = Rotation;

			Map map = Map;
			Map mapHeld = MapHeld;
			bool spawned = Spawned;
			bool worldPawn = this.IsWorldPawn();
			Caravan caravan = this.GetCaravan();
			ThingDef vehicleDef = VehicleDef.buildDef;

			if (Current.ProgramState == ProgramState.Playing)
			{
				Find.Storyteller.Notify_PawnEvent(this, AdaptationEvent.Died, null);
			}
			if (dinfo != null && dinfo.Value.Instigator != null)
			{
				if (dinfo.Value.Instigator is Pawn pawn)
				{
					RecordsUtility.Notify_PawnKilled(this, pawn);
				}
			}

			if (this.GetLord() != null)
			{
				this.GetLord().Notify_PawnLost(this, PawnLostCondition.IncappedOrKilled, dinfo);
			}
			if (spawned)
			{
				DropAndForbidEverything(false);
				if (destroyMode == DestroyMode.Deconstruct)
				{
					SoundDefOf.Building_Deconstructed.PlayOneShot(new TargetInfo(Position, map, false));
				}
				else if (destroyMode == DestroyMode.KillFinalize)
				{
					DoDestroyEffects(map);
				}
			}

			VehicleBuilding wreckage = (VehicleBuilding)ThingMaker.MakeThing(vehicleDef);
			wreckage.SetFactionDirect(Faction);
			wreckage.vehicle = this;
			wreckage.HitPoints = wreckage.MaxHitPoints / 10;

			meleeVerbs.Notify_PawnKilled();
			if (spawned)
			{
				if (map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterOceanDeep || map.terrainGrid.TerrainAt(position) == TerrainDefOf.WaterDeep)
				{
					StringBuilder downWithShipString = new StringBuilder();
					bool pawnsLostAtSea = false;
					foreach (Pawn pawn in AllPawnsAboard)
					{
						if (HediffHelper.AttemptToDrown(pawn))
						{
							pawnsLostAtSea = true;
							downWithShipString.AppendLine(pawn.LabelCap);
							//pawn.Destroy(DestroyMode.Vanish);
						}
						else
						{
							DisembarkPawn(pawn);
						}
					}
					string desc = pawnsLostAtSea ? "VF_BoatSunkDesc".Translate(LabelShort) : "VF_BoatSunkWithPawnsDesc".Translate(LabelShort, downWithShipString.ToString());
					Find.LetterStack.ReceiveLetter("VF_BoatSunk".Translate(), desc, LetterDefOf.NegativeEvent, new TargetInfo(Position, map, false), null, null);
					Destroy(DestroyMode.KillFinalize);
					return;
				}
				else
				{
					Destroy(DestroyMode.KillFinalize);
				}

				if (spawnWreckage)
				{
					GenSpawn.Spawn(wreckage, position, map, rotation, WipeMode.FullRefund);
				}
			}
			return;
		}

		protected virtual void DoDestroyEffects(Map map)
		{
			if (VehicleDef.buildDef.building.destroyEffecter != null)
			{
				Effecter effecter = VehicleDef.buildDef.building.destroyEffecter.Spawn(Position, map, 1f);
				effecter.Trigger(new TargetInfo(Position, map), TargetInfo.Invalid);
				effecter.Cleanup();
				return;
			}
			SoundDef destroySound = GetDestroySound();
			if (destroySound != null)
			{
				destroySound.PlayOneShot(new TargetInfo(Position, map));
			}
			foreach (IntVec3 intVec in this.OccupiedRect())
			{
				int num = VehicleDef.buildDef.building.isNaturalRock ? 1 : Rand.RangeInclusive(3, 5);
				for (int i = 0; i < num; i++)
				{
					FleckMaker.ThrowDustPuffThick(intVec.ToVector3Shifted(), map, Rand.Range(1.5f, 2f), Color.white);
				}
			}
			if (Find.CurrentMap == map)
			{
				float num2 = VehicleDef.buildDef.building.destroyShakeAmount;
				if (num2 < 0f)
				{
					num2 = VehicleDef.buildDef.shakeAmountPerAreaCurve?.Evaluate(VehicleDef.buildDef.Size.Area) ?? 0;
				}
				CompLifespan compLifespan = GetSortedComp<CompLifespan>();
				if (compLifespan == null || compLifespan.age < compLifespan.Props.lifespanTicks)
				{
					Find.CameraDriver.shaker.DoShake(num2);
				}
			}
		}

		public virtual SoundDef GetDestroySound()
		{
			if (!VehicleDef.buildDef.building.destroySound.NullOrUndefined())
			{
				return VehicleDef.buildDef.building.destroySound;
			}
			
			if (VehicleDef.buildDef.CostList.NullOrEmpty() || !VehicleDef.buildDef.CostList[0].thingDef.IsStuff || VehicleDef.buildDef.CostList[0].thingDef.stuffProps.categories.NullOrEmpty())
			{
				return null;
			}
			StuffCategoryDef stuffCategoryDef = VehicleDef.buildDef.CostList[0].thingDef.stuffProps.categories[0];

			switch (VehicleDef.buildDef.building.buildingSizeCategory)
			{
				case BuildingSizeCategory.None:
					{
						int area = VehicleDef.buildDef.Size.Area;
						if (area <= 1 && !stuffCategoryDef.destroySoundSmall.NullOrUndefined())
						{
							return stuffCategoryDef.destroySoundSmall;
						}
						if (area <= 4 && !stuffCategoryDef.destroySoundMedium.NullOrUndefined())
						{
							return stuffCategoryDef.destroySoundMedium;
						}
						if (!stuffCategoryDef.destroySoundLarge.NullOrUndefined())
						{
							return stuffCategoryDef.destroySoundLarge;
						}
						break;
					}
				case BuildingSizeCategory.Small:
					if (!stuffCategoryDef.destroySoundSmall.NullOrUndefined())
					{
						return stuffCategoryDef.destroySoundSmall;
					}
					break;
				case BuildingSizeCategory.Medium:
					if (!stuffCategoryDef.destroySoundMedium.NullOrUndefined())
					{
						return stuffCategoryDef.destroySoundMedium;
					}
					break;
				case BuildingSizeCategory.Large:
					if (!stuffCategoryDef.destroySoundLarge.NullOrUndefined())
					{
						return stuffCategoryDef.destroySoundLarge;
					}
					break;
			}
			return null;
		}

		public void AddComp(ThingComp comp)
		{
			cachedComps.Add(comp);
		}

		public void RemoveComp(ThingComp comp)
		{
			cachedComps.Remove(comp);
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

		public void AddHandlers(List<VehicleHandler> handlerList)
		{
			if (handlerList.NullOrEmpty())
				return;
			foreach(VehicleHandler handler in handlerList)
			{
				VehicleHandler existingHandler = handlers.FirstOrDefault(h => h == handler);
				if(existingHandler != null)
				{
					existingHandler.role.turretIds.AddRange(handler.role.turretIds);
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
			return handlers.FindAll(x => x.role.handlingTypes.NotNullAndAny(h => h == handlingTypeFlag) && (handlingTypeFlag != HandlingTypeFlags.Cannon || (!x.role.turretIds.NullOrEmpty() && x.role.turretIds.Contains(cannonKey))));
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
			if (Nameable)
			{
				Find.WindowStack.Add(new Dialog_GiveVehicleName(this));
			}
		}
		
		public void ChangeColor()
		{
			Dialog_ColorPicker.OpenColorPicker(this, delegate (Color colorOne, Color colorTwo, Color colorThree, PatternDef patternDef, Vector2 displacement, float tiles)
			{
				patternToPaint = new PatternData(colorOne, colorTwo, colorThree, patternDef, displacement, tiles);
			});
		}

		public void SetColor()
		{
			if (!CanPaintNow)
			{
				return;
			}

			patternData.Copy(patternToPaint);

			DrawColor = patternData.color;
			DrawColorTwo = patternData.colorTwo;
			DrawColorThree = patternData.colorThree;
			Notify_ColorChanged();
			CompVehicleTurrets?.turrets.ForEach(c => c.ResolveCannonGraphics(patternData, true));
			
			patternToPaint = null;
		}

		public virtual void InspectOpen()
		{
			VehicleInfoCard.Init(this);
		}

		public virtual void InspectClose()
		{
			VehicleInfoCard.Clear();
		}

		public virtual void DrawInspectDialog(Rect rect)
		{
			VehicleInfoCard.Draw(rect);
		}

		public virtual float DoInspectPaneButtons(float x)
		{
			Rect rect = new Rect(x, 0f, Extra.IconBarDim, Extra.IconBarDim);
			float usedWidth = 0;
			if (Nameable)
			{
				usedWidth += rect.width;
				TooltipHandler.TipRegionByKey(rect, "VF_RenameVehicleTooltip");
				if (Widgets.ButtonImage(rect, VehicleTex.Rename))
				{
					Rename();
				}
				rect.x -= rect.width;
			}
			if (VehicleGraphic.Shader.SupportsRGBMaskTex())
			{
				usedWidth += rect.width;
				TooltipHandler.TipRegionByKey(rect, "VF_RecolorTooltip");
				if (Widgets.ButtonImage(rect, VehicleTex.Recolor))
				{
					ChangeColor();
				}
			}
			return usedWidth;
		}

		public void MultiplePawnFloatMenuOptions(List<Pawn> pawns)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			VehicleReservationManager reservationManager = Map.GetCachedMapComponent<VehicleReservationManager>();
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
					reservationManager.Reserve<VehicleHandler, VehicleHandlerReservation>(this, p, job, handler);
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
				r += reservationManager.GetReservation<VehicleHandlerReservation>(this)?.ClaimantsOnHandler(h) ?? 0;
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
				Bill_BoardVehicle bill = bills.FirstOrDefault(x => x.pawnToBoard == pawn);
				if (!(bill is null))
				{
					bill.handler = handler;
					return;
				}
			}
			bills.Add(new Bill_BoardVehicle(pawn, handler));
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
			if (bills != null && bills.Count > 0)
			{
				Bill_BoardVehicle bill = bills.FirstOrDefault(x => x.pawnToBoard == pawnToBoard);
				if (bill != null)
				{
					if (pawnToBoard.IsWorldPawn())
					{
						Log.Error("Tried boarding vehicle with world pawn. Use Notify_BoardedCaravan instead.");
						return;
					}
					if (pawnToBoard.Spawned)
					{
						pawnToBoard.DeSpawn(DestroyMode.Vanish);
					}
					EventRegistry[VehicleEventDefOf.PawnEntered].ExecuteEvents();
					if (bill.handler.handlers.TryAddOrTransfer(pawnToBoard, true))
					{
						if (pawnToBoard != null)
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
					else if (pawnToBoard.holdingOwner != null)
					{
						pawnToBoard.holdingOwner.TryTransferToContainer(pawnToBoard, bill.handler.handlers);
					}
					bills.Remove(bill);
				}
			}
		}

		public void DisembarkPawn(Pawn pawn)
		{
			if (!pawn.Spawned)
			{
				CellRect occupiedRect = this.OccupiedRect().ExpandedBy(1);
				IntVec3 loc = Position;
				if (occupiedRect.EdgeCells.Where(c => GenGrid.InBounds(c, Map) && GenGrid.Standable(c, Map) && !c.GetThingList(Map).NotNullAndAny(t => t is Pawn)).TryRandomElement(out IntVec3 newLoc))
				{
					loc = newLoc;
				}
				EventRegistry[VehicleEventDefOf.PawnExited].ExecuteEvents();
				GenSpawn.Spawn(pawn, loc, MapHeld);
				if (!GenGrid.Standable(loc, Map))
				{
					pawn.pather.TryRecoverFromUnwalkablePosition(false);
				}
				if (this.GetLord() is Lord lord)
				{
					if (pawn.GetLord() is Lord otherLord)
					{
						otherLord.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
					}
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
				bills = new List<Bill_BoardVehicle>();
			}

			navigationCategory = VehicleDef.defaultNavigation;

			if (VehicleDef.properties.roles != null && VehicleDef.properties.roles.Count > 0)
			{
				foreach (VehicleRole role in VehicleDef.properties.roles)
				{
					handlers.Add(new VehicleHandler(this, role));
				}
			}

			RecacheComponents();
		}

		protected virtual void RecacheComponents()
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

		protected virtual void RegenerateUnsavedComponents()
		{
			vehicleAI = new VehicleAI(this);
			vDrawer = new Vehicle_DrawTracker(this);
			graphicOverlay = new VehicleGraphicOverlay(this);
			sustainers ??= new VehicleSustainers(this);
		}

		public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			if (vPather.curPath != null)
			{
				vPather.curPath.DrawPath(this);
			}
			RenderHelper.DrawLinesBetweenTargets(this, jobs.curJob, jobs.jobQueue);
		}

		public virtual bool DeconstructibleBy(Faction faction)
		{
			return DebugSettings.godMode || Faction == faction;
		}

		public override TipSignal GetTooltip()
		{
			return base.GetTooltip();
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

		private void InitializeHitbox()
		{
			Hitbox = CellRect.CenteredOn(IntVec3.Zero, VehicleDef.Size.x, VehicleDef.Size.z);
			statHandler.InitializeHitboxCells();
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			this.RegisterEvents();
			EventRegistry[VehicleEventDefOf.Spawned].ExecuteEvents();
			sharedJob ??= new SharedJob();
			if (!respawningAfterLoad)
			{
				vPather.ResetToCurrentPosition();
			}
			if (Faction != Faction.OfPlayer)
			{
				drafter.Drafted = true;
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
			Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleSpawned(this);
			ResetRenderStatus();
		}

		public float VehicleMovedPercent()
		{
			if (vPather is null)
			{
				return 0f;
			}
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
			EventRegistry[VehicleEventDefOf.Despawned].ExecuteEvents();
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
			DisembarkAll();
			sustainers.Cleanup();
			EventRegistry[VehicleEventDefOf.Destroyed].ExecuteEvents();
			base.Destroy(mode);
		}

		public override void Tick()
		{
			BaseTickOptimized();
			TickAllComps();
			if (Spawned)
			{
				vPather.PatherTick();
				sustainers.Tick();
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

		protected override void ReceiveCompSignal(string signal)
		{
			if (signal == CompSignals.RanOutOfFuel)
			{
				vPather.StopDead();
			}
		}

		protected virtual void BaseTickOptimized()
		{
			if (Find.TickManager.TicksGame % 250 == 0)
			{
				TickRare();
			}
			bool suspended = Suspended;
			if (!suspended)
			{
				if (Spawned)
				{
					stances.StanceTrackerTick();
					verbTracker.VerbsTick();
					natives.NativeVerbsTick();
					jobs.JobTrackerTick();
					Drawer.VehicleDrawerTick();
					rotationTracker.RotationTrackerTick();
					interactions?.InteractionsTrackerTick();
				}
				equipment?.EquipmentTrackerTick();
				apparel?.ApparelTrackerTick();

				caller?.CallTrackerTick();
				skills?.SkillsTick();
				abilities?.AbilitiesTick();
				inventory?.InventoryTrackerTick();
				drafter?.DraftControllerTick();
				relations?.RelationsTrackerTick();

				if (ModsConfig.RoyaltyActive)
				{
					royalty?.RoyaltyTrackerTick();
				}
				ageTracker.AgeTick();
				records.RecordsTick();
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref vPather, nameof(vPather), new object[] { this });
			Scribe_Deep.Look(ref statHandler, nameof(statHandler), new object[] { this });
			Scribe_Deep.Look(ref sharedJob, nameof(sharedJob));

			Scribe_Values.Look(ref angle, nameof(angle));

			Scribe_Deep.Look(ref patternData, nameof(patternData));
			Scribe_Defs.Look(ref retexture, nameof(retexture));
			Scribe_Deep.Look(ref patternToPaint, nameof(patternToPaint));

			Scribe_Values.Look(ref movementStatus, nameof(movementStatus), VehicleMovementStatus.Online);
			Scribe_Values.Look(ref navigationCategory, nameof(navigationCategory), NavigationCategory.Opportunistic);
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
