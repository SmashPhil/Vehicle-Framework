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
		public Vehicle_DrawTracker vDrawer;
		[Unsaved]
		public VehicleGraphicOverlay graphicOverlay;

		public PatternData patternData;
		public RetextureDef retexture;

		private float angle = 0f; /* -45 is left, 45 is right : relative to Rot4 direction*/

		private Graphic_Vehicle graphicInt;
		public PatternData patternToPaint;

		public float CachedAngle { get; set; }
		private List<VehicleHandler> HandlersWithPawnRenderer { get; set; }

		public bool CanPaintNow => patternToPaint != null;

		public bool Nameable => SettingsCache.TryGetValue(VehicleDef, typeof(VehicleDef), nameof(VehicleDef.nameable), VehicleDef.nameable);

		public override Vector3 DrawPos => vDrawer.DrawPos;

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
				float movePercent = vDrawer.tweener.MovedPercent();
				return GenThing.TrueCenter(Position, Rotation, VehicleDef.Size, VehicleDef.Altitude);
			}
		}

		public Vector3 TrueCenter
		{
			get
			{
				Vector3 result = Position.ToVector3ShiftedWithAltitude(VehicleDef.Altitude);
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
							if (size.x % 2 == 0)
							{
								result.x += 0.5f;
							}
							if (size.z % 2 == 0)
							{
								result.z -= 0.5f;
							}
							break;
						case 2:
							if (size.x % 2 == 0)
							{
								result.x -= 0.5f;
							}
							if (size.z % 2 == 0)
							{
								result.z -= 0.5f;
							}
							break;
						case 3:
							if (size.x % 2 == 0)
							{
								result.x -= 0.5f;
							}
							if (size.z % 2 == 0)
							{
								result.z += 0.5f;
							}
							break;
					}
				}
				return result;
			}
		}

		public override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			var drawVehicle = new Task(delegate ()
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
			var drawVehicle = new Task(delegate ()
			{
				Drawer.renderer.RenderPawnAt(drawLoc, angle, northSouthRotation);
				foreach (VehicleHandler handler in HandlersWithPawnRenderer)
				{
					handler.RenderPawns();
				}
			});
			drawVehicle.RunSynchronously();
		}

		public new void ProcessPostTickVisuals(int ticksPassed, CellRect viewRect)
		{
			if (!Suspended && Spawned)
			{
				if (Current.ProgramState != ProgramState.Playing || viewRect.Contains(base.Position))
				{
					vDrawer.ProcessPostTickVisuals(ticksPassed);
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
					defaultLabel = "Destroy Component",
					action = delegate ()
					{
						var options = new List<FloatMenuOption>();
						foreach (VehicleComponent component in statHandler.components)
						{
							options.Add(new FloatMenuOption(component.props.label, delegate ()
							{
								component.TakeDamage(this, new DamageInfo(DamageDefOf.Bite, float.MaxValue));
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
								component.TakeDamage(this, new DamageInfo(DamageDefOf.Bite, component.health * Rand.Range(0.1f, 1)));
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
				yield return new Command_Action()
				{
					defaultLabel = "Give Random Pawn MentalState",
					action = delegate ()
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
				//yield return new FloatMenuOption("VF_RepairVehicle".Translate(LabelShort), delegate ()
				//{
				//	Job job = new Job(JobDefOf_Vehicles.RepairVehicle, this);
				//	selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				//});
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

		public void ChangeColor()
		{
			Dialog_ColorPicker.OpenColorPicker(this, delegate (Color colorOne, Color colorTwo, Color colorThree, PatternDef patternDef, Vector2 displacement, float tiles)
			{
				patternToPaint = new PatternData(colorOne, colorTwo, colorThree, patternDef, displacement, tiles);
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
				usedWidth += rect.width;
				TooltipHandler.TipRegionByKey(rect, "VF_RenameVehicleTooltip");
				if (Widgets.ButtonImage(rect, VehicleTex.Rename))
				{
					Rename();
				}
				rect.x -= rect.width;
			}
			if (VehicleMod.settings.main.useCustomShaders && VehicleGraphic.Shader.SupportsRGBMaskTex())
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
