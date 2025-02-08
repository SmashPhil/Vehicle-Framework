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

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class AerialVehicleInFlight : DynamicDrawnWorldObject, IVehicleWorldObject
	{
		private static readonly Texture2D ViewQuestCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/ViewQuest", true);

		public const float ReconFlightSpeed = 5;
		public const float TransitionTakeoff = 0.025f;
		public const float PctPerTick = 0.001f;
		public const int TicksPerValidateFlightPath = 60;

		protected static readonly SimpleCurve climbRateCurve = new()
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
		public ThingOwner<VehiclePawn> innerContainer;

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
		private Material material;

		protected List<Graphic_Rotator> rotatorGraphics = [];

		public AerialVehicleInFlight()
		{
			innerContainer = new ThingOwner<VehiclePawn>(this, false, LookMode.Reference);
		}

		public override string Label => vehicle.Label;

		public virtual bool IsPlayerControlled => vehicle.Faction == Faction.OfPlayer;

		public float Elevation => 0;// vehicle.CompVehicleLauncher.inFlight ? elevation : 0;

		public float ElevationChange { get; protected set; }

		public float Rate => vehicle.CompVehicleLauncher.ClimbRateStat * 
			climbRateCurve.Evaluate(Elevation / vehicle.CompVehicleLauncher.MaxAltitude);

		public int TicksTillLandingElevation => Mathf.RoundToInt((Elevation - 
			(vehicle.CompVehicleLauncher.LandingAltitude / 2)) / Rate);

		protected virtual Rot8 FullRotation => Rot8.North;

		protected virtual float RotatorSpeeds => 59;

		/// <summary>
		/// Vehicle is in-flight towards destination. This includes skyfaller animations 
		/// where the vehicle has not yet been spawned, but is no longer on the world map.
		/// </summary>
		public bool Flying => vehicle.CompVehicleLauncher.inFlight;

		public bool CanDismount => false;

		public override Vector3 DrawPos
		{
			get
			{
				Vector3 nodePos = flightPath.First.GetCenter(this);
				if (position == nodePos)
				{
					return position;
				}
				return Vector3.Slerp(position, nodePos, transition);
			}
		}

		// For WITab readouts related to vehicles
		public IEnumerable<VehiclePawn> Vehicles
		{
			get
			{
				yield return vehicle;
			}
		}

		// All pawns will be in the AerialVehicle at all times.
		public IEnumerable<Pawn> DismountedPawns
		{
			get
			{
				yield break;
			}
		}

		[Obsolete]
		public virtual Material VehicleMat
		{
			get
			{
				if (vehicle is null)
				{
					return Material;
				}
				vehicleMat ??= new Material(vehicle.VehicleGraphic.MatAtFull(FullRotation))
				{
					shader = ShaderDatabase.WorldOverlayTransparentLit,
					renderQueue = WorldMaterials.WorldObjectRenderQueue
				};
				return vehicleMat;
			}
		}

		[Obsolete]
		public virtual Material VehicleMatNonLit
		{
			get
			{
				if (vehicle is null)
				{
					return Material;
				}
				vehicleMatNonLit ??= new Material(vehicle.VehicleGraphic.MatAtFull(FullRotation))
				{
					shader = ShaderDatabase.WorldOverlayTransparent,
					renderQueue = WorldMaterials.WorldObjectRenderQueue
				};
				return vehicleMatNonLit;
			}
		}

		public override Material Material
		{
			get
			{
				if (material == null)
				{
					material = MaterialPool.MatFrom(VehicleTex.CachedTextureIconPaths.TryGetValue(vehicle.VehicleDef, 
						VehicleTex.DefaultVehicleIconTexPath), ShaderDatabase.WorldOverlayTransparentLit, 
						WorldMaterials.WorldObjectRenderQueue);
				}
				return material;
			}
		}

		public virtual void Initialize()
		{
			position = base.DrawPos;
			rotatorGraphics = vehicle.graphicOverlay.AllOverlays.Where(g => g.Graphic is Graphic_Rotator)
				.Select(g => g.Graphic).Cast<Graphic_Rotator>().ToList();
		}

		public virtual Vector3 DrawPosAhead(int ticksAhead)
		{
			return Vector3.Slerp(position, flightPath.First.GetCenter(this), transition + speedPctPerTick * ticksAhead);
		}

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
				if (VehicleMod.settings.main.dynamicWorldDrawing && transitionPct <= 0)
				{
					Vector3 normalized = DrawPos.normalized;
					Vector3 direction = Vector3.Cross(normalized, rotateTexture ? directionFacing : Vector3.down);
					Quaternion quat = Quaternion.LookRotation(direction, normalized) * Quaternion.Euler(0f, 90f, 0f);
					Vector3 size = new Vector3(averageTileSize * 0.7f * drawPct, 1, averageTileSize * 0.7f * drawPct);

					Matrix4x4 matrix = default;
					matrix.SetTRS(DrawPos + normalized * TransitionTakeoff, quat, size);
					//Graphics.DrawMesh(MeshPool.plane10, matrix, VehicleMat, WorldCameraManager.WorldLayer);
					//RenderGraphicOverlays(normalized, direction, size);
				}
				else
				{
					WorldHelper.DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, 0.015f, Material);
				}
			}
		}

		protected virtual void RenderGraphicOverlays(Vector3 normalized, Vector3 direction, Vector3 size)
		{
			foreach (GraphicOverlay graphicOverlay in vehicle.graphicOverlay.AllOverlays)
			{
				Material material = graphicOverlay.Graphic.MatAt(FullRotation);
				float quatRotation = 90;
				if (graphicOverlay.Graphic is Graphic_Rotator rotator)
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

			if (ShowRelatedQuests)
			{
				List<Quest> quests = Find.QuestManager.QuestsListForReading;
				foreach (Quest quest in quests)
				{
					if (!quest.hidden && !quest.Historical && !quest.dismissed && quest.QuestLookTargets.Contains(this))
					{
						yield return new Command_Action
						{
							defaultLabel = "CommandViewQuest".Translate(quest.name),
							defaultDesc = "CommandViewQuestDesc".Translate(),
							icon = ViewQuestCommandTex,
							action = delegate ()
							{
								Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests, true);
								((MainTabWindow_Quests)MainButtonDefOf.Quests.TabWindow).Select(quest);
							}
						};
					}
				}
				quests = null;
			}

			if (IsPlayerControlled)
			{
				if (vehicle.CompFueledTravel != null)
				{
					yield return new Gizmo_RefuelableFuelTravel(vehicle.CompFueledTravel, false);
					foreach (Gizmo fuelGizmo in vehicle.CompFueledTravel.DevModeGizmos())
					{
						yield return fuelGizmo;
					}
				}
				if (vehicle.CompVehicleLauncher.ControlInFlight)
				{
					Command_Action launchCommand = new Command_Action()
					{
						defaultLabel = "CommandLaunchGroup".Translate(),
						defaultDesc = "CommandLaunchGroupDesc".Translate(),
						icon = VehicleTex.LaunchCommandTex,
						alsoClickIfOtherInGroupClicked = false,
						action = delegate ()
						{
							LaunchTargeter.BeginTargeting(vehicle, ChoseTargetOnMap, this, true, VehicleTex.TargeterMouseAttachment, false, null,
								(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => 
								vehicle.CompVehicleLauncher.launchProtocol.TargetingLabelGetter(target, Tile, path, fuelCost));
						}
					};
					if (!vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out string disableReason))
					{
						launchCommand.Disabled = true;
						launchCommand.disabledReason = disableReason;
					}
					yield return launchCommand;
				}
				if (DebugSettings.ShowDevGizmos)
				{
					yield return new Command_Action
					{
						defaultLabel = "Debug: Land at Nearest Player Settlement",
						action = delegate ()
						{
							Debugging.DebugLandAerialVehicle(this);
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
				else if (vehicle.CompFueledTravel != null && Ext_Math.SphericalDistance(pos, WorldHelper.GetTilePos(target.Tile)) > 
					vehicle.CompVehicleLauncher.MaxLaunchDistance || fuelCost > vehicle.CompFueledTravel.Fuel)
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
				float maxGlideDistance = Mathf.Abs(Elevation / ElevationChange) * PctPerTick * vehicle.CompVehicleLauncher.FlightSpeed.Clamp(0, 5);
				float sphericalDistance = Ext_Math.SphericalDistance(pos, WorldHelper.GetTilePos(target.Tile));
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
			this.recon = recon;
			OrderFlyToTiles(LaunchTargeter.FlightPath, DrawPos, arrivalAction: arrivalAction);
		}

		public override void Tick()
		{
			base.Tick();
			if (vehicle.CompVehicleLauncher.inFlight)
			{
				MoveForward();
				TickRotators();
				SpendFuel();

				if (vehicle.CompFueledTravel?.Fuel <= 0)
				{
					InitiateCrashEvent(null, "VF_IncidentCrashedSiteReason_OutOfFuel".Translate());
				}
				//ChangeElevation();
			}
			//if (Find.TickManager.TicksGame % TicksPerValidateFlightPath == 0)
			//{
			//	flightPath.VerifyFlightPath();
			//}
		}

		protected void ChangeElevation()
		{
			int altSign = flightPath.AltitudeDirection;
			float elevationChange = vehicle.CompVehicleLauncher.ClimbRateStat * climbRateCurve.Evaluate(elevation / 
				vehicle.CompVehicleLauncher.MaxAltitude);
			ElevationChange = elevationChange;
			if (elevationChange < 0)
			{
				altSign = 1;
			}
			elevation += elevationChange * altSign;
			elevation = elevation.Clamp(AltitudeMeter.MinimumAltitude, AltitudeMeter.MaximumAltitude);
			if (!vehicle.CompVehicleLauncher.AnyFlightControl)
			{
				InitiateCrashEvent(null, "VF_IncidentCrashedSiteReason_FlightControl".Translate());
			}
			else if (elevation <= AltitudeMeter.MinimumAltitude && !vehicle.CompVehicleLauncher.ControlledDescent)
			{
				InitiateCrashEvent(null, "VF_IncidentCrashedSiteReason_FlightControl".Translate());
			}
		}

		public virtual void SpendFuel()
		{
			if (vehicle.CompFueledTravel != null && 
				vehicle.CompFueledTravel.FuelCondition.HasFlag(FuelConsumptionCondition.Flying))
			{
				float amount = vehicle.CompFueledTravel.ConsumptionRatePerTick * 
					vehicle.CompVehicleLauncher.FuelConsumptionWorldMultiplier;
				vehicle.CompFueledTravel.ConsumeFuel(amount);
			}
		}

		public virtual void TakeDamage(DamageInfo damageInfo, IntVec2 cell)
		{
			vehicle.TakeDamage(damageInfo, cell);
		}

		public void InitiateCrashEvent(WorldObject culprit, params string[] reasons)
		{
			vehicle.CompVehicleLauncher.inFlight = false;
			Tile = WorldHelper.GetNearestTile(DrawPos);
			ResetPosition(WorldHelper.GetTilePos(Tile));
			flightPath.ResetPath();
			AirDefensePositionTracker.DeregisterAerialVehicle(this);
			IncidentWorker_ShuttleDowned.Execute(this, reasons, culprit: culprit);
		}

		public virtual void MoveForward()
		{
			if (flightPath.Empty)
			{
				Log.Error($"{this} in flight with empty FlightPath.  Grounding to current Tile.");
				ResetPosition(Find.WorldGrid.GetTileCenter(Tile));
				vehicle.CompVehicleLauncher.inFlight = false;
				AirDefensePositionTracker.DeregisterAerialVehicle(this);
			}
			else
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
						if (vehicle.Faction.IsPlayer)
						{
							Messages.Message("VF_AerialVehicleArrived".Translate(vehicle.LabelShort), 
								MessageTypeDefOf.NeutralEvent);
						}
						LandAtTile(flightPath.First.tile);

						//if (Elevation <= vehicle.CompVehicleLauncher.LandingAltitude)
						//{

						//}
						//else if (flightPath.Path.Count <= 1 && vehicle.CompVehicleLauncher.Props.circleToLand)
						//{
						//	Vector3 newPos = DrawPos;
						//	SetCircle(flightPath.First.tile);
						//	InitializeNextFlight(newPos);
						//}
					}
				}
			}
		}

		public void LandAtTile(int tile)
		{
			Tile = tile;
			ResetPosition(Find.WorldGrid.GetTileCenter(Tile));
			if (arrivalAction is AerialVehicleArrivalAction action)
			{
				action.Arrived(this, tile);
			}
			vehicle.CompVehicleLauncher.inFlight = false;
			AirDefensePositionTracker.DeregisterAerialVehicle(this);
		}

		public virtual void TickRotators()
		{
			foreach (Graphic_Rotator rotator in rotatorGraphics)
			{
				//hardcoded to 59° per tick to still allow room for eye to capture rotation
				vehicle.graphicOverlay.rotationRegistry[rotator.RegistryKey] += PropellerTakeoff.MaxRotationStep;
			}
		}

		public void OrderFlyToTiles(List<FlightNode> flightPath, Vector3 origin, 
			AerialVehicleArrivalAction arrivalAction = null)
		{
			if (flightPath.NullOrEmpty() || flightPath.Any(node => node.tile < 0))
			{
				return;
			}
			FlightNode flightNode = flightPath.FirstOrDefault();
			if (arrivalAction != null)
			{
				this.arrivalAction = arrivalAction;
			}
			this.flightPath.NewPath(flightPath);
			InitializeNextFlight(origin);
			List<AirDefense> flyoverDefenses = AirDefensePositionTracker.GetNearbyObjects(this, speedPctPerTick);
			AirDefensePositionTracker.RegisterAerialVehicle(this, flyoverDefenses);
			vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleOrdered].ExecuteEvents();
		}

		private void ResetPosition(Vector3 position)
		{
			this.position = position;
			transition = 0;
		}

		public void SwitchToCaravan()
		{
			bool autoSelect = false;
			if (Find.WorldSelector.SelectedObjects.Contains(this))
			{
				autoSelect = true;
			}
			innerContainer.Remove(vehicle);
			VehicleCaravan vehicleCaravan = CaravanHelper.MakeVehicleCaravan([vehicle], vehicle.Faction, Tile, true);
			if (!Destroyed)
			{
				Destroy();
			}

			if (autoSelect)
			{
				Find.WorldSelector.Select(vehicleCaravan, playSound: false);
			}
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
			Vector3 center = flightPath.First.GetCenter(this);
			if (position == center) //If position is still at origin, set speed to instantaneous
			{
				speedPctPerTick = 1;
				return;
			}
			float tileDistance = Mathf.Clamp(Ext_Math.SphericalDistance(position, center), 0.00001f, float.MaxValue); //Clamp tile distance to PctPerTick
			float flightSpeed = recon ? ReconFlightSpeed : vehicle.CompVehicleLauncher.FlightSpeed;
			speedPctPerTick = (PctPerTick / tileDistance) * flightSpeed.Clamp(0, 99999);
		}

		private void InitializeFacing()
		{
			Vector3 tileLocation = flightPath.First.GetCenter(this).normalized;
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
						Vector3 nextNodePosition = flightPath[i].GetCenter(this);
						LaunchTargeter.DrawTravelPoint(nodePosition, nextNodePosition);
						nodePosition = nextNodePosition;
					}
					LaunchTargeter.DrawTravelPoint(nodePosition, flightPath.Last.GetCenter(this));
				}
				else if (flightPath.Path.Count == 1)
				{
					LaunchTargeter.DrawTravelPoint(DrawPos, flightPath.First.GetCenter(this));
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
			Scribe_References.Look(ref vehicle, nameof(vehicle), true);

			Scribe_Deep.Look(ref flightPath, nameof(flightPath), [this]);
			Scribe_Deep.Look(ref arrivalAction, nameof(arrivalAction));
			Scribe_Values.Look(ref transition, nameof(transition));
			Scribe_Values.Look(ref position, nameof(position));

			//Scribe_Values.Look(ref elevation, "elevation");
			Scribe_Values.Look(ref recon, nameof(recon));

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				// No need to save container, vehicle is already saved. HoldingOwner is necessary for Vehicle's ParentHolder to
				// point to the aerial vehicle for WorldPawnGC and misc. world map handling.
				innerContainer.TryAdd(vehicle, canMergeWithExistingStacks: false);
			}
		}

		public override void SpawnSetup()
		{
			base.SpawnSetup();

			foreach (VehiclePawn vehicle in Vehicles)
			{
				vehicle.RegisterEvents();
			}

			if (flightPath != null && !flightPath.Path.NullOrEmpty())
			{
				//Needs new list instance to avoid clearing before reset.
				//This is only necessary for resetting with saved flight path due to flight being uninitialized from load.
				OrderFlyToTiles(flightPath.Path.ToList(), DrawPos, arrivalAction: arrivalAction);
			}
		}

		void IThingHolder.GetChildHolders(List<IThingHolder> outChildren)
		{
			outChildren.AddRange(vehicle.handlers);
		}

		ThingOwner IThingHolder.GetDirectlyHeldThings()
		{
			return vehicle.inventory.innerContainer;
		}

		public static AerialVehicleInFlight Create(VehiclePawn vehicle, int tile)
		{
			AerialVehicleInFlight aerialVehicle = (AerialVehicleInFlight)WorldObjectMaker.MakeWorldObject(WorldObjectDefOfVehicles.AerialVehicle);
			aerialVehicle.vehicle = vehicle;
			aerialVehicle.Tile = tile;
			aerialVehicle.SetFaction(vehicle.Faction);
			aerialVehicle.Initialize();
			aerialVehicle.innerContainer.TryAddOrTransfer(vehicle, canMergeWithExistingStacks: false);
			Find.WorldObjects.Add(aerialVehicle);
			return aerialVehicle;
		}
	}
}
