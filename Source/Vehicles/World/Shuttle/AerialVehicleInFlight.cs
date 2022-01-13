using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public class AerialVehicleInFlight : DynamicDrawnWorldObject
	{
		public const float ExpandingResize = 35f;
		public const float TransitionTakeoff = 0.025f;
		public const float PctPerTick = 0.001f;

		private static StringBuilder tmpSettleFailReason = new StringBuilder();

		protected static readonly SimpleCurve ClimbRateCurve = new SimpleCurve()
		{
			{
				new CurvePoint(0, 0.65f),
				true
			},
			{
				new CurvePoint(0.05f, 1),
				true
			},
			{
				new CurvePoint(0.95f, 1),
				true
			},
			{
				new CurvePoint(1, 0.15f),
				true
			}
		};

		public VehiclePawn vehicle;

		public AerialVehicleArrivalAction arrivalAction;

		protected internal FlightPath flightPath;

		internal float transition;
		public float elevation;
		public bool recon;
		private float transitionSize = 0f;
		private float speedPctPerTick;

		public Vector3 directionFacing;
		public Vector3 position;

		private Material vehicleMat;
		private Material vehicleMatNonLit;

		protected List<Graphic_Rotator> rotatorGraphics = new List<Graphic_Rotator>();

		public override string Label => vehicle.Label;

		public virtual bool IsPlayerControlled => vehicle.Faction == Faction.OfPlayer;

		public override Vector3 DrawPos => Vector3.Slerp(position, Find.WorldGrid.GetTileCenter(flightPath.First.tile), transition);

		public float Elevation => vehicle.CompVehicleLauncher.inFlight ? elevation : 0;

		public float ElevationChange { get; protected set; }

		public float Rate => vehicle.CompVehicleLauncher.ClimbRateStat * ClimbRateCurve.Evaluate(Elevation / vehicle.CompVehicleLauncher.MaxAltitude);

		public int TicksTillLandingElevation => Mathf.RoundToInt((Elevation - (vehicle.CompVehicleLauncher.LandingAltitude / 2)) / Rate);

		protected virtual Rot8 FullRotation => Rot8.North;

		protected virtual float RotatorSpeeds => 59;

		private Material VehicleMat
		{
			get
			{
				if (vehicle is null)
				{
					return Material;
				}
				vehicleMat ??= new Material(vehicle.VehicleGraphic.MatAt(FullRotation, vehicle.Pattern))
				{
					shader = ShaderDatabase.WorldOverlayTransparentLit,
					renderQueue = WorldMaterials.WorldObjectRenderQueue
				};
				return vehicleMat;
			}
		}

		private Material VehicleMatNonLit
		{
			get
			{
				if (vehicle is null)
				{
					return Material;
				}
				vehicleMatNonLit ??= new Material(vehicle.VehicleGraphic.MatAt(FullRotation, vehicle.Pattern))
				{
					shader = ShaderDatabase.WorldOverlayTransparent,
					renderQueue = WorldMaterials.WorldObjectRenderQueue
				};
				return vehicleMatNonLit;
			}
		}

		public virtual void Initialize()
		{
			position = base.DrawPos;
			rotatorGraphics = vehicle.graphicOverlay.graphics.Where(g => g.graphic is Graphic_Rotator).Select(g => g.graphic).Cast<Graphic_Rotator>().ToList();
		}

		public virtual Vector3 DrawPosAhead(int ticksAhead) => Vector3.Slerp(position, Find.WorldGrid.GetTileCenter(flightPath.First.tile), transition + speedPctPerTick * ticksAhead);

		public override void Draw()
		{
			if (!this.HiddenBehindTerrainNow())
			{
				float averageTileSize = Find.WorldGrid.averageTileSize;
				float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;
				
				if (transitionSize < 1)
				{
					transitionSize += TransitionTakeoff * (int)Find.TickManager.CurTimeSpeed;
				}
				float drawPct = (1 + (transitionPct * Find.WorldCameraDriver.AltitudePercent * ExpandingResize)) * transitionSize;
				if (directionFacing == default)
				{
					InitializeFacing();
				}
				bool rotateTexture = vehicle.CompVehicleLauncher.Props.faceDirectionOfTravel;
				if (transitionPct <= 0)
				{
					Vector3 normalized = DrawPos.normalized;
					Vector3 direction = Vector3.Cross(normalized, rotateTexture ? directionFacing : Vector3.down);
					Quaternion quat = Quaternion.LookRotation(direction, normalized) * Quaternion.Euler(0f, 90f, 0f);
					Vector3 size = new Vector3(averageTileSize * 0.7f * drawPct, 1, averageTileSize * 0.7f * drawPct);

					Matrix4x4 matrix = default;
					matrix.SetTRS(DrawPos + normalized * TransitionTakeoff, quat, size);
					Graphics.DrawMesh(MeshPool.plane10, matrix, VehicleMat, WorldCameraManager.WorldLayer);
					RenderGraphicOverlays(normalized, direction, size);
				}
				else
				{
					Rect rect = ExpandableWorldObjectsUtility.ExpandedIconScreenRect(this);
					if (ExpandingIconFlipHorizontal)
					{
						rect.x = rect.xMax;
						rect.width *= -1f;
					}
					if (Event.current.type != EventType.Repaint)
					{
						return;
					}
					Matrix4x4 matrix = GUI.matrix;
					if (rotateTexture)
					{
						Verse.UI.RotateAroundPivot(Quaternion.LookRotation(Find.WorldGrid.GetTileCenter(flightPath.First.tile) - position).eulerAngles.y, rect.center);
					}
					GenUI.DrawTextureWithMaterial(rect, VehicleTex.VehicleTexture(vehicle.VehicleDef, Rot8.North), VehicleMatNonLit);
					GUI.matrix = matrix;
				}
			}
		}

		protected virtual void RenderGraphicOverlays(Vector3 normalized, Vector3 direction, Vector3 size)
		{
			foreach (GraphicOverlay graphicOverlay in vehicle.graphicOverlay.graphics)
			{
				Material material = graphicOverlay.graphic.MatAt(FullRotation);
				float quatRotation = 90;
				if (graphicOverlay.graphic is Graphic_Rotator rotator)
				{
					quatRotation += vehicle.graphicOverlay.rotationRegistry[rotator.RegistryKey];
				}
				Quaternion quat = Quaternion.LookRotation(direction, normalized) * Quaternion.Euler(0, quatRotation, 0);
				Matrix4x4 matrix = default;
				matrix.SetTRS(DrawPos + normalized * TransitionTakeoff, quat, size);
				Graphics.DrawMesh(MeshPool.plane10, matrix, material, WorldCameraManager.WorldLayer);
			}
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}

			if (IsPlayerControlled)
			{
				if (vehicle.CompFueledTravel != null)
				{
					yield return vehicle.CompFueledTravel.FuelCountGizmo;
					foreach (Gizmo fuelGizmo in vehicle.CompFueledTravel.DevModeGizmos())
					{
						yield return fuelGizmo;
					}
				}
				if (!vehicle.CompVehicleLauncher.inFlight && Find.WorldObjects.SettlementAt(Tile) is Settlement settlement2)
				{
					yield return GizmoHelper.AerialVehicleTradeCommand(this, settlement2.Faction, settlement2.TraderKind);
				}
				if (vehicle.CompVehicleLauncher.ControlInFlight || !vehicle.CompVehicleLauncher.inFlight)
				{
					if (vehicle.CompFueledTravel.EmptyTank)
					{
						Command_Action glideCommand = new Command_Action()
						{
							defaultLabel = "CommandGlideLand".Translate(),
							defaultDesc = "CommandGlidLandDesc".Translate(),
							icon = VehicleTex.TradeCommandTex,
							alsoClickIfOtherInGroupClicked = false,
							action = delegate ()
							{
								LaunchTargeter.Instance.BeginTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(GlideToTargetOnMap), this, true, VehicleTex.TargeterMouseAttachment, false, null,
									(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => vehicle.CompVehicleLauncher.launchProtocol.TargetingLabelGetter(target, Tile, path, fuelCost));
							}
						};
						yield return glideCommand;
					}
					else
					{
						Command_Action launchCommand = new Command_Action()
						{
							defaultLabel = "CommandLaunchGroup".Translate(),
							defaultDesc = "CommandLaunchGroupDesc".Translate(),
							icon = VehicleTex.LaunchCommandTex,
							alsoClickIfOtherInGroupClicked = false,
							action = delegate ()
							{
								LaunchTargeter.Instance.BeginTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(ChoseTargetOnMap), this, true, VehicleTex.TargeterMouseAttachment, false, null,
									(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => vehicle.CompVehicleLauncher.launchProtocol.TargetingLabelGetter(target, Tile, path, fuelCost));
							}
						};
						yield return launchCommand;
					}
				}
				if (!vehicle.CompVehicleLauncher.inFlight)
				{
					foreach (Settlement settlement in Find.WorldObjects.ObjectsAt(flightPath.First.tile).Where(o => o is Settlement).Cast<Settlement>())
					{
						yield return GizmoHelper.ShuttleTradeCommand(this, settlement);
						if (WorldHelper.CanOfferGiftsTo(this, settlement))
						{
							yield return new Command_Action
							{
								defaultLabel = "CommandOfferGifts".Translate(),
								defaultDesc = "CommandOfferGiftsDesc".Translate(),
								icon = VehicleTex.OfferGiftsCommandTex,
								action = delegate()
								{
									Pawn playerNegotiator = WorldHelper.FindBestNegotiator(vehicle, null, null);
									Find.WindowStack.Add(new Dialog_Trade(playerNegotiator, settlement, true));
								}
							};
						}
					}
					Command_Settle commandSettle = new Command_Settle
					{
						defaultLabel = "CommandSettle".Translate(),
						defaultDesc = "CommandSettleDesc".Translate(),
						icon = SettleUtility.SettleCommandTex,
						action = delegate ()
						{
							SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
							void settleHere()
							{
								SettlementVehicleUtility.Settle(this);
							};
							SettlementProximityGoodwillUtility.CheckConfirmSettle(Tile, settleHere);
						}
					};
					if (!TileFinder.IsValidTileForNewSettlement(Tile, tmpSettleFailReason))
					{
						commandSettle.Disable(tmpSettleFailReason.ToString());
					}
					else if (SettleUtility.PlayerSettlementsCountLimitReached)
					{
						if (Prefs.MaxNumberOfPlayerSettlements > 1)
						{
							commandSettle.Disable("CommandSettleFailReachedMaximumNumberOfBases".Translate());
						}
						else
						{
							commandSettle.Disable("CommandSettleFailAlreadyHaveBase".Translate());
						}
					}
					yield return commandSettle;
				}
				if (Prefs.DevMode)
				{
					yield return new Command_Action
					{
						defaultLabel = "Debug: Land at Nearest Player Settlement",
						action = delegate ()
						{
							List<Settlement> playerSettlements = Find.WorldObjects.Settlements.Where(s => s.Faction == Faction.OfPlayer).ToList();
							Settlement nearestSettlement = playerSettlements.MinBy(s => Ext_Math.SphericalDistance(s.DrawPos, DrawPos));
							
							LaunchProtocol launchProtocol = vehicle.CompVehicleLauncher.launchProtocol;
							Rot4 vehicleRotation = launchProtocol.landingProperties.forcedRotation ?? Rot4.Random;
							IntVec3 cell = CellFinderExtended.RandomCenterCell(nearestSettlement.Map, (IntVec3 cell) => !MapHelper.VehicleBlockedInPosition(vehicle, Current.Game.CurrentMap, cell, vehicleRotation));
							VehicleSkyfaller_Arriving skyfaller = (VehicleSkyfaller_Arriving)ThingMaker.MakeThing(vehicle.CompVehicleLauncher.Props.skyfallerIncoming);
							skyfaller.vehicle = vehicle;

							GenSpawn.Spawn(skyfaller, cell, nearestSettlement.Map, vehicleRotation);
							Destroy();
						}
					};
					yield return new Command_Action
					{
						defaultLabel = "Debug: Initiate Crash Event",
						action = delegate ()
						{
							InitiateCrashEvent(null);
						}
					};
				}
			}
		}

		public virtual bool ChoseTargetOnMap(GlobalTargetInfo target, float fuelCost)
		{
			bool Validator(GlobalTargetInfo target, Vector3 pos, Action<int, AerialVehicleArrivalAction, bool> launchAction)
			{
				if (!target.IsValid)
				{
					Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				else if (Ext_Math.SphericalDistance(pos, Find.WorldGrid.GetTileCenter(target.Tile)) > vehicle.CompVehicleLauncher.MaxLaunchDistance || fuelCost > vehicle.CompFueledTravel.Fuel)
				{
					Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				IEnumerable<FloatMenuOption> source = vehicle.CompVehicleLauncher.launchProtocol.GetFloatMenuOptionsAt(target.Tile);
				if (!source.Any())
				{
					if (!WorldVehiclePathGrid.Instance.Passable(target.Tile, vehicle.VehicleDef))
					{
						Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
						return false;
					}
					launchAction(target.Tile, null, false);
					return true;
				}
				else
				{
					if (source.Count() != 1)
					{
						Find.WindowStack.Add(new FloatMenuTargeter(source.ToList()));
						return false;
					}
					if (!source.First().Disabled)
					{
						source.First().action();
						return true;
					}
					return false;
				}
			};
			return vehicle.CompVehicleLauncher.launchProtocol.ChoseWorldTarget(target, DrawPos, Validator, NewDestination);
		}

		public virtual bool GlideToTargetOnMap(GlobalTargetInfo target, float fuelCost)
		{
			bool Validator(GlobalTargetInfo target, Vector3 pos, Action<int, AerialVehicleArrivalAction, bool> launchAction)
			{
				float maxGlideDistance = Mathf.Abs(Elevation / ElevationChange) * PctPerTick * vehicle.CompVehicleLauncher.FlySpeed.Clamp(0, 5);
				float sphericalDistance = Ext_Math.SphericalDistance(pos, Find.WorldGrid.GetTileCenter(target.Tile));
				if (!target.IsValid)
				{
					Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				else if (sphericalDistance > vehicle.CompVehicleLauncher.MaxLaunchDistance || sphericalDistance > maxGlideDistance)
				{
					Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				IEnumerable<FloatMenuOption> source = vehicle.CompVehicleLauncher.launchProtocol.GetFloatMenuOptionsAt(target.Tile);
				if (!source.Any())
				{
					if (!WorldVehiclePathGrid.Instance.Passable(target.Tile, vehicle.VehicleDef))
					{
						Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
						return false;
					}
					launchAction(target.Tile, null, false);
					return true;
				}
				else
				{
					if (source.Count() != 1)
					{
						Find.WindowStack.Add(new FloatMenuTargeter(source.ToList()));
						return false;
					}
					if (!source.First().Disabled)
					{
						source.First().action();
						return true;
					}
					return false;
				}
			};
			return vehicle.CompVehicleLauncher.launchProtocol.ChoseWorldTarget(target, DrawPos, Validator, NewDestination);
		}

		public void NewDestination(int destinationTile, AerialVehicleArrivalAction arrivalAction, bool recon = false)
		{
			vehicle.CompVehicleLauncher.inFlight = true;
			this.arrivalAction = arrivalAction;
			this.recon = recon;
			OrderFlyToTiles(LaunchTargeter.FlightPath, DrawPos, arrivalAction);
		}

		public override void Tick()
		{
			base.Tick();
			if (vehicle.CompVehicleLauncher.inFlight)
			{
				MoveForward();
				TickRotators();
				SpendFuel();
				ChangeElevation();
			}
		}

		protected void ChangeElevation()
		{
			int altSign = flightPath.AltitudeDirection;
			float elevationChange = vehicle.CompVehicleLauncher.ClimbRateStat * ClimbRateCurve.Evaluate(elevation / vehicle.CompVehicleLauncher.MaxAltitude);
			ElevationChange = elevationChange;
			if (elevationChange < 0)
			{
				altSign = 1;
			}
			elevation += elevationChange * altSign;
			elevation = elevation.Clamp(AltitudeMeter.MinimumAltitude, AltitudeMeter.MaximumAltitude);
			if (!vehicle.CompVehicleLauncher.AnyFlightControl)
			{
				InitiateCrashEvent(null);
			}
			else if (elevation <= AltitudeMeter.MinimumAltitude && !vehicle.CompVehicleLauncher.ControlledDescent)
			{
				InitiateCrashEvent(null);
			}
		}

		public virtual void SpendFuel()
		{
			if (vehicle.CompFueledTravel != null)
			{
				float amount = vehicle.CompFueledTravel.ConsumptionRatePerTick / vehicle.CompVehicleLauncher.FuelEfficiencyWorld;
				vehicle.CompFueledTravel.ConsumeFuel(amount);
			}
		}

		public virtual void TakeDamage(DamageInfo damageInfo, IntVec3 cell, bool explosive)
		{
			vehicle.TakeDamage(damageInfo, cell, explosive);
		}

		public void InitiateCrashEvent(WorldObject worldObject)
		{
			vehicle.CompVehicleLauncher.inFlight = false;
			Tile = WorldHelper.GetNearestTile(DrawPos);
			ResetPosition(Find.WorldGrid.GetTileCenter(Tile));
			flightPath.ResetPath();
			AirDefensePositionTracker.DeregisterAerialVehicle(this);
			(VehicleIncidentDefOf.BlackHawkDown.Worker as IncidentWorker_ShuttleDowned).TryExecuteEvent(this, worldObject);
		}

		public virtual void MoveForward()
		{
			transition += speedPctPerTick;
			if (transition >= 1)
			{
				if (flightPath.Path.Count > 1)
				{
					Vector3 newPos = DrawPos;
					int ticksLeft = Mathf.RoundToInt(1 / speedPctPerTick);
					flightPath.NodeReached(ticksLeft > TicksTillLandingElevation && !recon);
					if (Spawned)
					{
						InitializeNextFlight(newPos);
					}
				}
				else 
				{
					if (Elevation <= vehicle.CompVehicleLauncher.LandingAltitude)
					{
						Messages.Message("VehicleAerialArrived".Translate(vehicle.LabelShort), MessageTypeDefOf.NeutralEvent);
						Tile = flightPath.First.tile;
						if (arrivalAction is AerialVehicleArrivalAction action)
						{
							action.Arrived(flightPath.First.tile);
							if (action.DestroyOnArrival)
							{
								Destroy();
							}
						}
						vehicle.CompVehicleLauncher.inFlight = false;
						AirDefensePositionTracker.DeregisterAerialVehicle(this);
					}
					else if (flightPath.Path.Count <= 1 && vehicle.CompVehicleLauncher.Props.circleToLand)
					{
						Vector3 newPos = DrawPos;
						SetCircle(flightPath.First.tile);
						InitializeNextFlight(newPos);
					}
				}
			}
		}

		public virtual void TickRotators()
		{
			foreach (Graphic_Rotator rotator in rotatorGraphics)
			{
				vehicle.graphicOverlay.rotationRegistry[rotator.RegistryKey] += rotator.MaxRotationSpeed;
			}
		}

		public void OrderFlyToTiles(List<FlightNode> flightPath, Vector3 origin, AerialVehicleArrivalAction arrivalAction = null)
		{
			if (flightPath.NullOrEmpty() || flightPath.Any(node => node.tile < 0))
			{
				return;
			}
			if (arrivalAction != null)
			{
				this.arrivalAction = arrivalAction;
			}
			this.flightPath.NewPath(flightPath);
			InitializeNextFlight(origin);
			var flyoverDefenses = AirDefensePositionTracker.CheckNearbyObjects(this, speedPctPerTick)?.ToHashSet() ?? new HashSet<AirDefense>();
			AirDefensePositionTracker.RegisterAerialVehicle(this, flyoverDefenses);
		}

		private void ResetPosition(Vector3 position)
		{
			this.position = position;
			transition = 0;
		}

		private void InitializeNextFlight(Vector3 position)
		{
			vehicle.CompVehicleLauncher.inFlight = true;
			ResetPosition(position);
			SetSpeed();
			InitializeFacing();
		}

		private void SetSpeed()
		{
			float tileDistance = Ext_Math.SphericalDistance(position, Find.WorldGrid.GetTileCenter(flightPath.First.tile));
			speedPctPerTick = (PctPerTick / tileDistance) * vehicle.CompVehicleLauncher.FlySpeed.Clamp(0, 5);
		}

		private void InitializeFacing()
		{
			Vector3 tileLocation = Find.WorldGrid.GetTileCenter(flightPath.First.tile).normalized;
			directionFacing = (DrawPos - tileLocation).normalized;
		}

		public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			DrawFlightPath();
		}

		public void DrawFlightPath()
		{
			if (!LaunchTargeter.Instance.IsTargeting)
			{
				if (flightPath.Path.Count > 1)
				{
					Vector3 nodePosition = DrawPos;
					for (int i = 0; i < flightPath.Path.Count; i++)
					{
						Vector3 nextNodePosition = Find.WorldGrid.GetTileCenter(flightPath[i].tile);
						LaunchTargeter.DrawTravelPoint(nodePosition, nextNodePosition);
						nodePosition = nextNodePosition;
					}
					LaunchTargeter.DrawTravelPoint(nodePosition, Find.WorldGrid.GetTileCenter(flightPath.Last.tile));
				}
				else if (flightPath.Path.Count == 1)
				{
					LaunchTargeter.DrawTravelPoint(DrawPos, Find.WorldGrid.GetTileCenter(flightPath.First.tile));
				}
			}
		}

		public void SetCircle(int tile)
		{
			flightPath.PushCircleAt(tile);
		}

		public void GenerateMapForRecon(int tile)
		{
			if (flightPath.InRecon && Find.WorldObjects.MapParentAt(tile) is MapParent mapParent && !mapParent.HasMap)
			{
				LongEventHandler.QueueLongEvent(delegate ()
				{
					Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, null);
					TaggedString label = "LetterLabelCaravanEnteredEnemyBase".Translate();
					TaggedString text = "LetterTransportPodsLandedInEnemyBase".Translate(mapParent.Label).CapitalizeFirst();
					if (mapParent is Settlement settlement)
					{
						SettlementUtility.AffectRelationsOnAttacked(settlement, ref text);
					}
					if (!mapParent.HasMap)
					{
						Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
						PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(map.mapPawns.AllPawns, ref label, ref text, "LetterRelatedPawnsInMapWherePlayerLanded".Translate(Faction.OfPlayer.def.pawnsPlural), true, true);
					}
					Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, vehicle, mapParent.Faction, null, null, null);
					Current.Game.CurrentMap = map;
					CameraJumper.TryHideWorld();
				}, "GeneratingMap", false, null, true);
			}
		}

		public override void PostMake()
		{
			base.PostMake();
			flightPath = new FlightPath(this);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref vehicle, "vehicle");

			Scribe_Deep.Look(ref flightPath, "flightPath", new object[] { this });

			Scribe_Deep.Look(ref arrivalAction, "arrivalAction");
			Scribe_Values.Look(ref speedPctPerTick, "speedPctPerTick");

			Scribe_Values.Look(ref transition, "transition");
			Scribe_Values.Look(ref elevation, "elevation");
			Scribe_Values.Look(ref recon, "recon");
			Scribe_Values.Look(ref directionFacing, "directionFacing");
			Scribe_Values.Look(ref position, "position");
		}
	}
}
