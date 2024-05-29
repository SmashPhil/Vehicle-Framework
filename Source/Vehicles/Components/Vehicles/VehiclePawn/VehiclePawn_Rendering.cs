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
using Mono.Security;

namespace Vehicles
{
	/// <summary>
	/// Rendering & Graphics
	/// </summary>
	public partial class VehiclePawn
	{
		[Unsaved]
		private Vehicle_DrawTracker vDrawer;
		[Unsaved]
		public VehicleGraphicOverlay graphicOverlay;

		public PatternData patternData;
		public RetextureDef retexture;

		private float angle = 0f; /* -45 is left, 45 is right : relative to Rot4 direction*/

		private Graphic_Vehicle graphicInt;
		public PatternData patternToPaint;

		private bool crashLanded;

		public float CachedAngle { get; set; }

		private List<VehicleHandler> HandlersWithPawnRenderer { get; set; }

		public bool CanPaintNow => patternToPaint != null;

		public bool Nameable => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), nameof(VehicleDef.nameable), VehicleDef.nameable);

		public override Vector3 DrawPos => Drawer.DrawPos;

		public (Vector3 drawPos, float rotation) DrawData => (DrawPos, this.CalculateAngle(out _));

		public ThingWithComps Thing => this;

		public bool CrashLanded
		{
			get
			{
				return crashLanded;
			}
			set
			{
				if (crashLanded == value)
				{
					return;
				}
				crashLanded = value;
				if (!crashLanded)
				{
					if (!VehicleDef.graphicData.drawRotated)
					{
						Rotation = VehicleDef.defaultPlacingRot;
					}
					Angle = 0;
				}
			}
		}

		public float Angle
		{
			get
			{
				if (!VehicleMod.settings.main.allowDiagonalRendering || !VehicleDef.properties.diagonalRotation)
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
					if (Destroyed && !RGBMaterialPool.GetAll(this).NullOrEmpty())
					{
						Log.Error($"Reinitializing RGB Materials but {this} has already been destroyed and the cache was not cleared for this entry. This may result in a memory leak.");
						RGBMaterialPool.Release(this);
					}

					GraphicDataRGB graphicData = new GraphicDataRGB();
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

					if (graphicData.shaderType.Shader.SupportsRGBMaskTex())
					{
						RGBMaterialPool.CacheMaterialsFor(this);
						graphicData.Init(this);
						graphicInt = graphicData.Graphic as Graphic_Vehicle;
						RGBMaterialPool.SetProperties(this, patternData, graphicInt.TexAt, graphicInt.MaskAt);
					}
					else
					{
						graphicInt = ((GraphicData)graphicData).Graphic as Graphic_Vehicle; //Triggers vanilla Init call for normal material caching
					}
				}
				return graphicInt;
			}
		}

		public int MaterialCount => 8;

		public PatternDef PatternDef => Pattern;

		string IMaterialCacheTarget.Name => $"{VehicleDef}_{this}";

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

		public Vector3 OverlayCenter
		{
			get
			{
				float movePercent = Drawer.tweener.MovedPercent();
				return GenThing.TrueCenter(Position, Rotation, VehicleDef.Size, VehicleDef.Altitude);
			}
		}

		public IEnumerable<AnimationDriver> Animations
		{
			get
			{
				if (CompVehicleLauncher != null)
				{
					foreach (AnimationDriver animationDriver in compVehicleLauncher.Animations)
					{
						yield return animationDriver;
					}
				}
			}
		}

		public Vector3 TrueCenter()
		{
			return TrueCenter(Position);
		}

		public Vector3 TrueCenter(IntVec3 cell, float? altitude = null)
		{
			float altitudeValue = altitude ?? VehicleDef.Altitude;
			Vector3 result = cell.ToVector3ShiftedWithAltitude(altitudeValue);
			IntVec2 size = VehicleDef.Size;
			Rot8 rot = Rotation; //Switch to FullRotation when diagonal hitboxes are implemented
			if (size.x != 1 || size.z != 1)
			{
				if (rot.IsHorizontal)
				{
					int x = size.x;
					size.x = size.z;
					size.z = x;
				}
				switch (rot.AsInt)
				{
					case 0:
					case 2:
						if (size.x % 2 == 0)
						{
							result.x += 0.5f;
						}
						if (size.z % 2 == 0)
						{
							result.z += 0.5f;
						}
						break;
					case 1:
					case 3:
						if (size.x % 2 == 0)
						{
							result.x += 0.5f;
						}
						if (size.z % 2 == 0)
						{
							result.z -= 0.5f;
						}
						break;
				}
			}
			return result;
		}

		public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
		{
			if (phase == DrawPhase.Draw)
			{
				Draw();
			}
		}

		public virtual void Draw()
		{
			if (this.AnimationLocked()) return;

			if (VehicleDef.drawerType == DrawerType.RealtimeOnly)
			{
				Vector3 drawPos = DrawPos;
				float rotation = this.CalculateAngle(out _);
				DrawAt(drawPos, FullRotation, rotation, compDraw: false);
			}
			Comps_PostDraw();
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			Drawer.DrawAt(drawLoc);
			foreach (VehicleHandler handler in HandlersWithPawnRenderer)
			{
				handler.RenderPawns();
			}
			statHandler.DrawHitbox(HighlightedComponent); //Must be rendered with the vehicle or the field edges will not render quickly enough
		}

		/// <summary>
		/// Called from skyfaller and launch protocol classes when vehicle is unspawned
		/// </summary>
		/// <param name="drawLoc"></param>
		/// <param name="rotation"></param>
		/// <param name="flip"></param>
		public virtual void DrawAt(Vector3 drawLoc, Rot8 rot, float extraRotation, bool flip = false, bool compDraw = true)
		{
			bool northSouthRotation = VehicleGraphic.EastDiagonalRotated && (FullRotation == Rot8.NorthEast || FullRotation == Rot8.SouthEast) ||
				(VehicleGraphic.WestDiagonalRotated && (FullRotation == Rot8.NorthWest || FullRotation == Rot8.SouthWest));
			Drawer.renderer.RenderPawnAt(drawLoc, extraRotation, northSouthRotation);

			//TODO - consolidate rendering to VehicleRenderer
			foreach (VehicleHandler handler in HandlersWithPawnRenderer)
			{
				handler.RenderPawns();
			}
			if (compDraw) //Temp fix till I get to cleaning up these 3 Draw methods
			{
				Comps_PostDrawUnspawned(drawLoc, rot, extraRotation);
			}
			statHandler.DrawHitbox(HighlightedComponent); //Must be rendered with the vehicle or the field edges will not render quickly enough
		}

		public virtual void Comps_PostDrawUnspawned(Vector3 drawLoc, Rot8 rot, float rotation)
		{
			if (AllComps != null)
			{
				foreach (ThingComp thingComp in AllComps)
				{
					if (thingComp is VehicleComp vehicleComp)
					{
						vehicleComp.PostDrawUnspawned(drawLoc, rot, rotation);
					}
				}
			}
		}

		public virtual void DrawExplosiveWicks(Vector3 drawLoc, Rot8 rot)
		{
			for (int i = 0; i < explosives.Count; i++)
			{
				TimedExplosion timedExplosion = explosives[i];
				if (timedExplosion.Active)
				{
					timedExplosion.DrawAt(drawLoc, rot);
				}
			}
		}

		public new void ProcessPostTickVisuals(int ticksPassed, CellRect viewRect)
		{
			if (!Suspended && Spawned)
			{
				if (Current.ProgramState != ProgramState.Playing || viewRect.Contains(Position))
				{
					Drawer.ProcessPostTickVisuals(ticksPassed);
				}
				rotationTracker.ProcessPostTickVisuals(ticksPassed);
			}
		}

		public void ResetRenderStatus()
		{
			HandlersWithPawnRenderer = handlers.Where(h => h.role.pawnRenderer != null).ToList();
		}

		public override void Notify_ColorChanged()
		{
			ResetGraphicCache();
			Drawer.renderer.graphics.ResolveAllGraphics();
			graphicOverlay.Notify_ColorChanged();
			base.Notify_ColorChanged();
		}

		internal void ResetGraphicCache()
		{
			if (UnityData.IsInMainThread)
			{
				RGBMaterialPool.SetProperties(this, patternData);
				foreach (ThingComp thingComp in AllComps)
				{
					if (thingComp is VehicleComp vehicleComp)
					{
						vehicleComp.Notify_ColorChanged();
					}
				}
			}
		}

		public void UpdateRotationAndAngle()
		{
			UpdateRotation();
			UpdateAngle();
		}

		public void UpdateRotation()
		{
			if (vehiclePather.nextCell == Position)
			{
				return;
			}
			if (!VehicleDef.rotatable)
			{
				Rotation = VehicleDef.defaultPlacingRot;
				return;
			}
			IntVec3 intVec = vehiclePather.nextCell - Position;
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
			if (vehiclePather.Moving)
			{
				IntVec3 c = vehiclePather.nextCell - Position;
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

		public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			if (vehiclePather.curPath != null && vehiclePather.curPath.NodesLeftCount > 0)
			{
				vehiclePather.curPath.DrawPath(this);
			}
			RenderHelper.DrawLinesBetweenTargets(this, jobs.curJob, jobs.jobQueue);

			if (!cargoToLoad.NullOrEmpty())
			{
				foreach (TransferableOneWay transferable in cargoToLoad)
				{
					if (transferable.HasAnyThing)
					{
						GenDraw.DrawLineBetween(DrawPos, transferable.AnyThing.DrawPos);
					}
				}
			}
		}

		public override TipSignal GetTooltip()
		{
			return base.GetTooltip();
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Type type in VehicleDef.designatorTypes)
			{
				Designator designator = DesignatorCache.Get(type);
				if (designator != null)
				{
					//yield return designator;
				}
			}

			if (Faction != Faction.OfPlayer && !DebugSettings.ShowDevGizmos)
			{
				yield break;
			}

			if (MovementPermissions > VehiclePermissions.NotAllowed)
			{
				foreach (Gizmo gizmo in ignition.GetGizmos())
				{
					yield return gizmo;
				}
			}

			if (!cargoToLoad.NullOrEmpty())
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
			else
			{
				Command_Action loadVehicle = new Command_Action
				{
					defaultLabel = "VF_LoadCargo".Translate(),
					icon = VehicleDef.LoadCargoIcon,
					action = delegate ()
					{
						Find.WindowStack.Add(new Dialog_LoadCargo(this));
					}
				};
				yield return loadVehicle;
			}

			if (FishingCompatibility.Active && SettingsCache.TryGetValue(VehicleDef, typeof(VehicleProperties), nameof(VehicleProperties.fishing), VehicleDef.properties.fishing))
			{
				Command_Toggle fishing = new Command_Toggle
				{
					defaultLabel = "VF_StartFishing".Translate(),
					defaultDesc = "VF_StartFishingDesc".Translate(),
					icon = VehicleTex.FishingIcon,
					isActive = () => currentlyFishing,
					toggleAction = delegate ()
					{
						currentlyFishing = !currentlyFishing;
					}
				};
				yield return fishing;
			}

			Command_Action flagForLoading = new Command_Action
			{
				defaultLabel = "VF_HaulPawnToVehicle".Translate(),
				icon = VehicleTex.HaulPawnToVehicle,
				action = delegate ()
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					HaulTargeter.BeginTargeting(new TargetingParameters()
					{
						canTargetPawns = true,
						canTargetBuildings = false,
						neverTargetHostileFaction = true,
						canTargetItems = false,
						thingCategory = ThingCategory.Pawn,
						validator = delegate (TargetInfo target)
						{
							if (!target.HasThing)
							{
								return false;
							}
							if (target.Thing is Pawn pawn)
							{
								if (pawn is VehiclePawn)
								{
									return false;
								}
								return pawn.Faction == Faction.OfPlayer || pawn.IsColonist || pawn.IsColonyMech || pawn.IsSlaveOfColony || pawn.IsPrisonerOfColony;
							}
							return false;
						}
					}, delegate (LocalTargetInfo target)
					{
						if (target.Thing is Pawn pawn && pawn.IsColonistPlayerControlled && !pawn.Downed)
						{
							VehicleHandler handler = pawn.IsColonistPlayerControlled ? NextAvailableHandler() : handlers.FirstOrDefault(handler => handler.AreSlotsAvailable && handler.role.handlingTypes == HandlingTypeFlags.None);
							PromptToBoardVehicle(pawn, handler);
							return;
						}
						TransferableOneWay transferable = new TransferableOneWay()
						{
							things = new List<Thing>() { target.Thing },
						};
						transferable.AdjustTo(1);
						cargoToLoad.Add(transferable);
						Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(this, ReservationType.LoadVehicle);
					}, this);
				}
			};
			yield return flagForLoading;

			if (!Drafted)
			{
				Command_Action unloadAll = new Command_Action
				{
					defaultLabel = "VF_DisembarkAllPawns".Translate(),
					icon = VehicleTex.UnloadAll,
					action = delegate ()
					{
						DisembarkAll();
					},
					hotKey = KeyBindingDefOf.Misc2
				};
				yield return unloadAll;
				bool exitBlocked = !SurroundingCells.NotNullAndAny(cell => cell.Walkable(Map));
				if (exitBlocked)
				{
					unloadAll.Disable("VF_DisembarkNoExit".Translate());
				}
				foreach (VehicleHandler handler in handlers)
				{
					for (int i = 0; i < handler.handlers.Count; i++)
					{
						Pawn currentPawn = handler.handlers.InnerListForReading[i];
						Command_Action_PawnDrawer unloadAction = new Command_Action_PawnDrawer();
						unloadAction.defaultLabel = "VF_DisembarkSinglePawn".Translate(currentPawn.LabelShort);
						unloadAction.groupable = false;
						unloadAction.pawn = currentPawn;
						unloadAction.action = delegate ()
						{
							DisembarkPawn(currentPawn);
						};
						if (exitBlocked)
						{
							unloadAction.Disable("VF_DisembarkNoExit".Translate());
						}
						yield return unloadAction;
					}
				}
			}
			if (this.GetLord()?.LordJob is LordJob_FormAndSendVehicles formCaravanLordJob)
			{
				Command_Action forceCaravanLeave = new Command_Action
				{
					defaultLabel = "VF_ForceLeaveCaravan".Translate(),
					defaultDesc = "VF_ForceLeaveCaravanDesc".Translate(),
					icon = VehicleTex.CaravanIcon,
					activateSound = SoundDefOf.Tick_Low,
					action = delegate ()
					{
						formCaravanLordJob.ForceCaravanLeave();
						Messages.Message("VF_ForceLeaveConfirmation".Translate(), MessageTypeDefOf.TaskCompletion);
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

			if (DebugSettings.ShowDevGizmos && Spawned)
			{
				yield return new Command_Action
				{
					defaultLabel = "Destroy Component",
					action = delegate ()
					{
						var options = new List<FloatMenuOption>();
						foreach (VehicleComponent component in statHandler.components)
						{
							options.Add(new FloatMenuOption(component.props.label, delegate ()
							{
								component.TakeDamage(this, new DamageInfo(DamageDefOf.Vaporize, float.MaxValue), ignoreArmor: true);
								Notify_TookDamage();
							}));
						}
						if (!options.NullOrEmpty())
						{
							Find.WindowStack.Add(new FloatMenu(options));
						}
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "Damage Component",
					action = delegate ()
					{
						var options = new List<FloatMenuOption>();
						foreach (VehicleComponent component in statHandler.components)
						{
							options.Add(new FloatMenuOption(component.props.label, delegate ()
							{
								component.TakeDamage(this, new DamageInfo(DamageDefOf.Vaporize, component.health * Rand.Range(0.1f, 1)), ignoreArmor: true);
								Notify_TookDamage();
							}));
						}
						if (!options.NullOrEmpty())
						{
							Find.WindowStack.Add(new FloatMenu(options));
						}
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "Explode Component",
					action = delegate ()
					{
						var options = new List<FloatMenuOption>();
						foreach (VehicleComponent component in statHandler.components)
						{
							if (component.props.GetReactor<Reactor_Explosive>() is Reactor_Explosive reactorExplosive)
							{
								options.Add(new FloatMenuOption(component.props.label, delegate ()
								{
									reactorExplosive.Explode(this, component, new DamageInfo(DamageDefOf.Bomb, component.health * 0.5f));
								}));
							}
						}
						if (!options.NullOrEmpty())
						{
							Find.WindowStack.Add(new FloatMenu(options));
						}
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
				yield return new Command_Action
				{
					defaultLabel = "Recache All Stats",
					action = delegate ()
					{
						statHandler.MarkAllDirty();
					}
				};
				yield return new Command_Action()
				{
					defaultLabel = "Give Random Pawn MentalState",
					action = delegate ()
					{
						if (AllPawnsAboard.TryRandomElement(out Pawn result))
						{
							foreach (MentalStateDef mentalState in DefDatabase<MentalStateDef>.AllDefsListForReading)
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
				yield return new Command_Action()
				{
					defaultLabel = "Flash OccupiedRect",
					action = delegate ()
					{
						if (vehiclePather.Moving)
						{
							IntVec3 prevCell = Position;
							Rot8 rot = FullRotation;
							HashSet<IntVec3> cellsToHighlight = new HashSet<IntVec3>();
							foreach (IntVec3 cell in vehiclePather.curPath.NodesReversed)
							{
								if (prevCell != cell) rot = Rot8.DirectionFromCells(prevCell, cell);
								if (!rot.IsValid) rot = Rot8.North;
								foreach (IntVec3 occupiedCell in this.VehicleRect(cell, rot).Cells)
								{
									if (occupiedCell.InBounds(Map) && cellsToHighlight.Add(occupiedCell))
									{
										Map.debugDrawer.FlashCell(occupiedCell, 0.95f, duration: 180);
									}
								}
								prevCell = cell;
							}
						}
						else
						{
							CellRect occupiedRect = this.OccupiedRect();
							foreach (IntVec3 cell in occupiedRect)
							{
								if (cell.InBounds(Map))
								{
									Map.debugDrawer.FlashCell(cell, 0.95f, duration: 180);
								}
							}
							CellRect vehicleRect = this.VehicleRect();
							foreach (IntVec3 cell in vehicleRect)
							{
								if (cell.InBounds(Map))
								{
									Map.debugDrawer.FlashCell(cell, 0, duration: 180);
								}
							}
						}
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
            if (!IdeoAllowsBoarding(selPawn))
            {
                yield return new FloatMenuOption("VF_CantEnterVehicle_IdeoligionForbids".Translate(), null);
				yield break;
            }
            foreach (ThingComp thingComp in AllComps)
			{
				if (thingComp is VehicleComp vehicleComp)
				{
					foreach (FloatMenuOption floatMenuOption in vehicleComp.CompFloatMenuOptions())
					{
						yield return floatMenuOption;
					}
				}
			}
			foreach (VehicleHandler handler in handlers)
			{
				if (handler.AreSlotsAvailable)
				{
					VehicleReservationManager reservationManager = Map.GetCachedMapComponent<VehicleReservationManager>();
					FloatMenuOption opt = new FloatMenuOption("VF_EnterVehicle".Translate(LabelShort, handler.role.label, (handler.role.slots - (handler.handlers.Count +
						reservationManager.GetReservation<VehicleHandlerReservation>(this)?.ClaimantsOnHandler(handler) ?? 0)).ToString()), delegate ()
						{
							PromptToBoardVehicle(selPawn, handler);
						});
					yield return opt;
				}
			}
		}

		public void PromptToBoardVehicle(Pawn pawn, VehicleHandler handler)
		{
			if (handler == null)
			{
				Messages.Message("VF_HandlerNotEnoughRoom".Translate(pawn, this), MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			Job job = new Job(JobDefOf_Vehicles.Board, this);
			GiveLoadJob(pawn, handler);
			pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
			if (!pawn.Spawned)
			{
				return;
			}
			Map.GetCachedMapComponent<VehicleReservationManager>().Reserve<VehicleHandler, VehicleHandlerReservation>(this, pawn, pawn.CurJob, handler);
		}

		public bool IdeoAllowsBoarding(Pawn selPawn)
		{
			if (!ModsConfig.IdeologyActive)
			{ 
				return true; 
			}

            switch (this.VehicleDef.vehicleType)
			{
				case VehicleType.Air:
					if(!IdeoUtility.DoerWillingToDo(HistoryEventDefOf_Vehicles.VF_BoardAirVehicle, selPawn))
					{
						return false;
					}
					break;
                case VehicleType.Sea:
                    if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf_Vehicles.VF_BoardSeaVehicle, selPawn))
                    {
                        return false;
                    }
                    break;
                case VehicleType.Land:
                    if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf_Vehicles.VF_BoardLandVehicle, selPawn))
                    {
                        return false;
                    }
                    break;
                case VehicleType.Universal:
                    if (!IdeoUtility.DoerWillingToDo(HistoryEventDefOf_Vehicles.VF_BoardUniversalVehicle, selPawn))
                    {
                        return false;
                    }
                    break;

            }
			return true;
		}


		public void ChangeColor()
		{
			Dialog_ColorPicker.OpenColorPicker(this, delegate (Color colorOne, Color colorTwo, Color colorThree, PatternDef patternDef, Vector2 displacement, float tiles)
			{
				patternToPaint = new PatternData(colorOne, colorTwo, colorThree, patternDef, displacement, tiles);
				if (DebugSettings.godMode)
				{
					SetColor();
				}
			});
		}

		public void Rename()
		{
			if (Nameable)
			{
				Find.WindowStack.Add(new Dialog_GiveVehicleName(this));
			}
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
				rect.x -= rect.width;
				usedWidth += rect.width;
				{
					TooltipHandler.TipRegionByKey(rect, "VF_RenameVehicleTooltip");
					if (Widgets.ButtonImage(rect, VehicleTex.Rename))
					{
						Rename();
					}
				}
			}
			if (VehicleMod.settings.main.useCustomShaders && VehicleGraphic.Shader.SupportsRGBMaskTex())
			{
				rect.x -= rect.width;
				usedWidth += rect.width;
				{
					TooltipHandler.TipRegionByKey(rect, "VF_RecolorTooltip");
					if (Widgets.ButtonImage(rect, VehicleTex.Recolor))
					{
						ChangeColor();
					}
				}
			}
			if (Prefs.DevMode)
			{
				rect.x -= rect.width;
				usedWidth += rect.width;
				{
					if (Widgets.ButtonImage(rect, VehicleTex.Settings))
					{
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						options.Add(new FloatMenuOption("Tweak Values", delegate ()
						{
							Find.WindowStack.Add(new EditWindow_TweakFields(this));
						}));
						if (CompVehicleLauncher != null)
						{
							options.Add(new FloatMenuOption("Open in animator", OpenInAnimator));
						}
						if (!options.NullOrEmpty())
						{
							Find.WindowStack.Add(new FloatMenu(options));
						}
						else
						{
							Messages.Message($"{this} doesn't have any configuration options available.", MessageTypeDefOf.RejectInput, historical: false);
						}
					}
				}
			}
			return usedWidth;
		}

		public void OpenInAnimator()
		{
			Dialog_GraphEditor dialog_GraphEditor = new Dialog_GraphEditor(this, false);
			//dialog_GraphEditor.LogReport = VehicleMod.settings.debug.debugLogging;
			Find.WindowStack.Add(dialog_GraphEditor);
		}

		public void MultiplePawnFloatMenuOptions(List<Pawn> pawns)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();

			if (pawns.Any(pawn => !IdeoAllowsBoarding(pawn)))
			{
				options.Add(new FloatMenuOption("VF_CantEnterVehicle_IdeoligionForbids".Translate(), null));

			}
			else
			{
				VehicleReservationManager reservationManager = Map.GetCachedMapComponent<VehicleReservationManager>();
				FloatMenuOption opt1 = new FloatMenuOption("VF_BoardVehicleGroup".Translate(LabelShort), delegate ()
				{
					List<IntVec3> cells = this.OccupiedRect().Cells.ToList();
					foreach (Pawn p in pawns)
					{
						if (cells.Contains(p.Position))
						{
							continue;
						}
						VehicleHandler handler = p.IsColonistPlayerControlled ? NextAvailableHandler() : handlers.FirstOrDefault(handler => handler.AreSlotsAvailable && handler.role.handlingTypes == HandlingTypeFlags.None);
						PromptToBoardVehicle(p, handler);
					}
				}, MenuOptionPriority.Default, null, null, 0f, null, null);
				FloatMenuOption opt2 = new FloatMenuOption("VF_BoardVehicleGroupFail".Translate(LabelShort), null, MenuOptionPriority.Default, null, null, 0f, null, null)
				{
					Disabled = true
				};
				int r = 0;
				foreach (VehicleHandler h in handlers)
				{
					r += reservationManager.GetReservation<VehicleHandlerReservation>(this)?.ClaimantsOnHandler(h) ?? 0;
				}
				options.Add(pawns.Count + r > SeatsAvailable ? opt2 : opt1);
			}
			FloatMenuMulti floatMenuMap = new FloatMenuMulti(options, pawns, this, pawns[0].LabelCap, Verse.UI.MouseMapPosition())
			{
				givesColonistOrders = true
			};
			Find.WindowStack.Add(floatMenuMap);
		}
	}
}
