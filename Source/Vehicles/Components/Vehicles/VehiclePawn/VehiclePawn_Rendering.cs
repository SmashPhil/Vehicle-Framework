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

		public override void Draw()
		{
			if (this.AnimationLocked()) return;

			if (VehicleDef.drawerType == DrawerType.RealtimeOnly)
			{
				Vector3 drawPos = DrawPos;
				float rotation = this.CalculateAngle(out _);
				DrawAt(drawPos, rotation, compDraw: false);
			}
			Comps_PostDraw();
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			Drawer.DrawAt(drawLoc);
			foreach (VehicleHandler handler in HandlersWithPawnRenderer)
			{
				handler.RenderPawns();
			}
			statHandler.DrawHitbox(HighlightedComponent);
		}

		/// <summary>
		/// Called from skyfaller and launch protocol classes when vehicle is unspawned
		/// </summary>
		/// <param name="drawLoc"></param>
		/// <param name="rotation"></param>
		/// <param name="flip"></param>
		public virtual void DrawAt(Vector3 drawLoc, float rotation, bool flip = false, bool compDraw = true)
		{
			bool northSouthRotation = VehicleGraphic.EastDiagonalRotated && (FullRotation == Rot8.NorthEast || FullRotation == Rot8.SouthEast) ||
				(VehicleGraphic.WestDiagonalRotated && (FullRotation == Rot8.NorthWest || FullRotation == Rot8.SouthWest));
			Drawer.renderer.RenderPawnAt(drawLoc, rotation, northSouthRotation);
			foreach (VehicleHandler handler in HandlersWithPawnRenderer)
			{
				handler.RenderPawns();
			}
			if (compDraw) //Temp fix till I get to cleaning up these 3 Draw methods
			{
				Comps_PostDrawUnspawned(drawLoc, rotation);
			}
		}

		public virtual void Comps_PostDrawUnspawned(Vector3 drawLoc, float rotation)
		{
			if (AllComps != null)
			{
				foreach (ThingComp thingComp in AllComps)
				{
					if (thingComp is VehicleComp vehicleComp)
					{
						vehicleComp.PostDrawUnspawned(drawLoc, rotation);
					}
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

		public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			if (vPather.curPath != null && vPather.curPath.NodesLeftCount > 0)
			{
				vPather.curPath.DrawPath(this);
			}
			RenderHelper.DrawLinesBetweenTargets(this, jobs.curJob, jobs.jobQueue);
		}

		public override TipSignal GetTooltip()
		{
			return base.GetTooltip();
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			if (MovementPermissions > VehiclePermissions.NotAllowed)
			{
				foreach (Gizmo gizmo in ignition.GetGizmos())
				{
					yield return gizmo;
				}
			}

			foreach (Type type in VehicleDef.designatorTypes)
			{
				Designator designator = DesignatorCache.Get(type);
				if (designator != null)
				{
					//yield return designator;
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
						ignition.Drafted = false;
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
				if (SettingsCache.TryGetValue(VehicleDef, typeof(VehicleProperties), nameof(VehicleProperties.fishing), VehicleDef.properties.fishing) && FishingCompatibility.Active)
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

			if (Prefs.DevMode && DebugSettings.godMode && Spawned)
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
								component.TakeDamage(this, new DamageInfo(DamageDefOf.Vaporize, float.MaxValue));
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
								component.TakeDamage(this, new DamageInfo(DamageDefOf.Vaporize, component.health * Rand.Range(0.1f, 1)));
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
						if (vPather.Moving)
						{
							IntVec3 prevCell = Position;
							Rot8 rot = FullRotation;
							HashSet<IntVec3> cellsToHighlight = new HashSet<IntVec3>();
							foreach (IntVec3 cell in vPather.curPath.NodesReversed)
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
			foreach (VehicleHandler handler in handlers)
			{
				if (handler.AreSlotsAvailable)
				{
					VehicleReservationManager reservationManager = Map.GetCachedMapComponent<VehicleReservationManager>();
					FloatMenuOption opt = new FloatMenuOption("VF_EnterVehicle".Translate(LabelShort, handler.role.label, (handler.role.slots - (handler.handlers.Count +
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
					Job job = new Job(JobDefOf_Vehicles.Board, this);
					VehicleHandler handler = p.IsColonistPlayerControlled ? NextAvailableHandler() : handlers.FirstOrDefault(handler => handler.AreSlotsAvailable && handler.role.handlingTypes == HandlingTypeFlags.None);
					GiveLoadJob(p, handler);
					reservationManager.Reserve<VehicleHandler, VehicleHandlerReservation>(this, p, job, handler);
					p.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
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
			FloatMenuMulti floatMenuMap = new FloatMenuMulti(options, pawns, this, pawns[0].LabelCap, Verse.UI.MouseMapPosition())
			{
				givesColonistOrders = true
			};
			Find.WindowStack.Add(floatMenuMap);
		}
	}
}
