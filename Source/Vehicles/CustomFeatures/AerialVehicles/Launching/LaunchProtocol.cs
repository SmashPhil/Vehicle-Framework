using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using LudeonTK;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public abstract class LaunchProtocol : IExposable
	{
		protected VehiclePawn vehicle;

		protected bool drawOverlays = true;
		protected bool drawMotes = true;

		protected int ticksPassed;
		protected float effectsToThrow;
		protected LaunchType launchType;

		private Map map;
		protected IntVec3 position = IntVec3.Invalid;

		protected List<Graphic>[] cachedOverlayGraphics;
		protected List<GraphicDataLayered>[] cachedOverlayGraphicDatas;

		protected Material cachedShadowMaterial;
		private static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();

		/* -- Xml Input -- */

		protected int maxFlightNodes = int.MaxValue;

		/* ---------------- */

		/// <summary>
		/// Include only for XML initialization
		/// </summary>
		public LaunchProtocol()
		{
		}

		/// <summary>
		/// Copy over from XML values for instances
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="vehicle"></param>
		public LaunchProtocol(LaunchProtocol reference, VehiclePawn vehicle)
		{
			this.vehicle = vehicle;

			maxFlightNodes = reference.maxFlightNodes;
		}

		public VehiclePawn Vehicle => vehicle;

		public Vector3 DrawPos { get; protected set; }

		public IntVec3 Position => position;

		public float Angle { get; protected set; }

		public int TicksPassed => ticksPassed;

		public Map Map => map ?? vehicle.Map;

		protected abstract int TotalTicks_Takeoff { get; }

		protected abstract int TotalTicks_Landing { get; }

		public abstract LaunchProtocolProperties CurAnimationProperties { get; }

		public abstract LaunchProtocolProperties LandingProperties { get; }

		public abstract LaunchProtocolProperties LaunchProperties { get; }

		/// <summary>
		/// Maximum number of flight nodes able to be selected in LaunchTargeter
		/// </summary>
		public virtual int MaxFlightNodes => maxFlightNodes;

		public virtual float TimeInAnimation
		{
			get
			{
				int ticks = CurAnimationProperties.maxTicks;
				if (ticks <= 0)
				{
					return 0;
				}
				return (float)ticksPassed / ticks;
			}
		}

		public virtual IEnumerable<AnimationDriver> Animations
		{
			get
			{
				yield return new AnimationDriver(AnimationEditorTags.Takeoff, AnimationEditorTick_Takeoff, Draw, TotalTicks_Takeoff, () => OrderProtocol(LaunchType.Takeoff));
				yield return new AnimationDriver(AnimationEditorTags.Landing, AnimationEditorTick_Landing, Draw, TotalTicks_Landing, () => OrderProtocol(LaunchType.Landing));
			}
		}

		protected virtual Material ShadowMaterial
		{
			get
			{
				if (cachedShadowMaterial == null && !vehicle.CompVehicleLauncher.Props.shadow.NullOrEmpty())
				{
					cachedShadowMaterial = MaterialPool.MatFrom(vehicle.CompVehicleLauncher.Props.shadow, ShaderDatabase.Transparent);
				}
				return cachedShadowMaterial;
			}
		}

		/// <summary>
		/// Message displayed to user when CanLaunchNow returns false
		/// </summary>
		public virtual string FailLaunchMessage => "VF_AerialVehicleLaunchNotValid".Translate();

		/// <summary>
		/// Conditions in which shuttle can initiate takeoff
		/// </summary>
		public virtual bool CanLaunchNow
		{
			get
			{
				if (vehicle.Spawned)
				{
					if (Ext_Vehicles.IsRoofed(vehicle.Position, vehicle.Map))
					{
						return false;
					}
				}
				return !LaunchRestricted;
			}
		}

		/// <summary>
		/// Launch gizmo for specific takeoff versions
		/// </summary>
		public virtual Command_ActionHighlighter LaunchCommand
		{
			get
			{
				Command_ActionHighlighter takeoffCommand = new Command_ActionHighlighter
				{
					defaultLabel = "CommandLaunchGroup".Translate(),
					defaultDesc = "CommandLaunchGroupDesc".Translate(),
					icon = VehicleTex.LaunchCommandTex,
					alsoClickIfOtherInGroupClicked = false,
					action = delegate ()
					{
						if (vehicle.CompVehicleLauncher.AnyLeftToLoad)
						{
							Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmSendNotCompletelyLoadedPods".Translate(vehicle.LabelCapNoCount), new Action(StartChoosingDestination), false, null));
							return;
						}
						StartChoosingDestination();
					}
				};
				if (vehicle.Spawned && LaunchProperties.restriction != null)
				{
					takeoffCommand.mouseOver = () => LaunchProperties.restriction.DrawRestrictionsTargeter(vehicle, vehicle.Map, vehicle.Position, vehicle.Rotation);
				}
				return takeoffCommand;
			}
		}

		/// <summary>
		/// Restrictions placed on launching only. Landing restrictions are validated through the <see cref="LandingTargeter"/>
		/// </summary>
		public virtual bool LaunchRestricted => LaunchProperties.restriction != null && !LaunchProperties.restriction.CanStartProtocol(vehicle, vehicle.Map, vehicle.Position, vehicle.Rotation);

		public virtual bool LandingRestricted(Map map, IntVec3 position, Rot4 rotation) => false;

		public abstract LaunchProtocolProperties GetProperties(LaunchType launchType, Rot4 rot);

		/// <summary>
		/// Takeoff animation has finished
		/// </summary>
		public abstract bool FinishedAnimation(VehicleSkyfaller skyfaller);

		public (Vector3 drawPos, float rotation) Draw(Vector3 drawPos, float rotation)
		{

			(Vector3 drawPos, float rotation) result = (drawPos, rotation);
			ShadowData shadowData = ShadowData.CreateFrom(vehicle);
			switch (launchType)
			{
				case LaunchType.Landing:
					(result.drawPos, result.rotation, shadowData) = AnimateLanding(result.drawPos, result.rotation, shadowData);
					break;
				case LaunchType.Takeoff:
					(result.drawPos, result.rotation, shadowData) = AnimateTakeoff(result.drawPos, result.rotation, shadowData);
					break;
			}
			result.drawPos.y = AltitudeLayer.Skyfaller.AltitudeFor();
			Rot8 rot = CurAnimationProperties.forcedRotation ?? vehicle.Rotation;
			vehicle.DrawAt(result.drawPos, rot, result.rotation);
			(DrawPos, Angle) = result;
			if (VehicleMod.settings.main.aerialVehicleEffects)
			{
				DrawOverlays(result.drawPos, result.rotation);
			}
			if (!shadowData.Invalid)
			{
				Color shadowColor = Color.white;
				shadowColor.a = shadowData.alpha;
				IntVec3 position = this.position.IsValid || !vehicle.Spawned ? this.position : vehicle.Position;
				DrawShadow(position.ToVector3Shifted(), shadowData.width, shadowData.height, shadowColor);
			}
			return result;
		}

		protected void DrawShadow(Vector3 drawPos, float width, float height, Color color)
		{
			Material shadowMaterial = ShadowMaterial;
			if (shadowMaterial is null)
			{
				return;
			}
			Vector3 shadowSpot = drawPos;
			if (!CurAnimationProperties.lockShadowX)
			{
				shadowSpot.x = DrawPos.x;
			}
			if (!CurAnimationProperties.lockShadowZ)
			{
				shadowSpot.z = DrawPos.z;
			}
			DrawShadow(shadowSpot, CurAnimationProperties.forcedRotation ?? vehicle.Rotation, shadowMaterial, width, height, color);
		}

		private void DrawShadow(Vector3 pos, Rot4 rot, Material material, float width, float height, Color color)
		{
			pos.y = AltitudeLayer.Shadows.AltitudeFor();
			Vector3 s = new Vector3(width, 1f, height);
			
			shadowPropertyBlock.SetColor(ShaderPropertyIDs.Color, color);
			Matrix4x4 matrix = default;
			matrix.SetTRS(pos, rot.AsQuat, s);
			Graphics.DrawMesh(MeshPool.plane10Back, matrix, material, 0, null, 0, shadowPropertyBlock);
		}

		/// <summary>
		/// Landing animation when vehicle is entering map through flight
		/// </summary>
		/// <param name="drawPos"
		/// <param name="rotation"></param>
		protected virtual (Vector3 drawPos, float rotation, ShadowData shadowData) AnimateLanding(Vector3 drawPos, float rotation, ShadowData shadowData)
		{
			return (drawPos, rotation, shadowData);
		}

		/// <summary>
		/// Takeoff animation when vehicle is leaving map through flight
		/// </summary>
		/// <param name="drawPos"
		/// <param name="rotation"></param>
		protected virtual (Vector3 drawPos, float rotation, ShadowData shadowData) AnimateTakeoff(Vector3 drawPos, float rotation, ShadowData shadowData)
		{
			return (drawPos, rotation, shadowData);
		}

		/// <summary>
		/// Tick method for <see cref="AnimationManager"/> with total ticks passed since start.
		/// </summary>
		/// <param name="ticksPassed"></param>
		protected virtual int AnimationEditorTick_Landing(int ticksPassed)
		{
			this.ticksPassed = ticksPassed;
			TickMotes();
			return 0;
		}

		protected virtual int AnimationEditorTick_Takeoff(int ticksPassed)
		{
			this.ticksPassed = ticksPassed;
			TickMotes();
			return 0;
		}

		protected virtual void DrawOverlays(Vector3 drawPos, float rotation)
		{
			if (drawOverlays && !CurAnimationProperties.additionalTextures.NullOrEmpty())
			{
				for (int i = 0; i < CurAnimationProperties.additionalTextures.Count; i++)
				{
					GraphicDataLayered graphicData = CurAnimationProperties.additionalTextures[i];
					if (graphicData.Graphic is Graphic_Animate animationGraphic)
					{
						animationGraphic.DrawWorkerAnimated(drawPos, Rot4.North, ticksPassed, rotation);
					}
					else
					{
						graphicData.Graphic.DrawWorker(drawPos, Rot4.North, null, null, rotation);
					}
				}
			}
		}

		protected virtual void TickMotes()
		{
			if (!CurAnimationProperties.fleckData.NullOrEmpty())
			{
				foreach (FleckData fleckData in CurAnimationProperties.fleckData)
				{
					if (fleckData.runOutOfStep || (TimeInAnimation > 0 && TimeInAnimation < 1))
					{
						effectsToThrow = TryThrowFleck(fleckData, TimeInAnimation, effectsToThrow);
					}
				}
			}
			if (!CurAnimationProperties.fleckOneShots.NullOrEmpty())
			{
				foreach (FleckOneShot fleckOneShot in CurAnimationProperties.fleckOneShots)
				{
					if (fleckOneShot.emitAtTick == TicksPassed)
					{
						ThrowFleck(fleckOneShot);
					}
				}
			}
		}

		/// <summary>
		/// Ticker method called from related Skyfaller
		/// </summary>
		public void Tick()
		{
			vehicle.Tick();

			switch (launchType)
			{
				case LaunchType.Landing:
					TickLanding();
					break;
				case LaunchType.Takeoff:
					TickTakeoff();
					break;
			}
		}

		/// <summary>
		/// Ticker for landing
		/// </summary>
		protected virtual void TickLanding()
		{
			ticksPassed++;
			TickEvents();
			if (VehicleMod.settings.main.aerialVehicleEffects)
			{
				TickMotes();
			}
		}

		/// <summary>
		/// Ticker for taking off
		/// </summary>
		protected virtual void TickTakeoff()
		{
			ticksPassed++;
			TickEvents();
			if (VehicleMod.settings.main.aerialVehicleEffects)
			{
				TickMotes();
			}
		}

		protected virtual void TickEvents()
		{
			if (!CurAnimationProperties.events.NullOrEmpty())
			{
				for (int i = 0; i < CurAnimationProperties.events.Count; i++)
				{
					AnimationEvent<LaunchProtocol> @event = CurAnimationProperties.events[i];
					try
					{
						if (@event.EventFrame(TimeInAnimation))
						{
							@event.method.Invoke(null, this);
						}
					}
					catch (Exception ex)
					{
						Log.Error($"Exception thrown ticking animation event {@event?.method} for {Vehicle}.\nException={ex}");
					}
				}
			}
		}

		/* ---------- Animation Events ---------- */

		private static void SetMoteStatus(LaunchProtocol launchProtocol, bool active)
		{
			launchProtocol.drawMotes = active;
		}

		private static void SetOverlayStatus(LaunchProtocol launchProtocol, bool active)
		{
			launchProtocol.drawOverlays = active;
		}

		private static void SetComponentHealth(LaunchProtocol launchProtocol, string key, float health)
		{
			launchProtocol.vehicle.statHandler.SetComponentHealth(key, health);
		}

		/* ---------- Animation Events ---------- */

		/// <summary>
		/// Set map, root position of vehicle, and rotation to initiate
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="rot"></param>
		/// <param name="map"></param>
		public virtual void Prepare(Map map, IntVec3 position, Rot4 rot)
		{
			this.map = map;
			this.position = position;
			vehicle.Rotation = rot;
		}

		/// <summary>
		/// Release map and root position to prevent repeated use in un-prepared protocols
		/// </summary>
		public virtual void Release()
		{
			map = null;
			position = IntVec3.Invalid;
		}

		/// <summary>
		/// Set Tick Count for manual control over skyfaller time. Needs to be called after <seealso cref="OrderProtocol(bool)"/>
		/// </summary>
		/// <param name="ticks"></param>
		public virtual void SetTickCount(int ticks)
		{
			ticksPassed = ticks;
		}

		/// <summary>
		/// Initialize variables and setup for animation
		/// </summary>
		protected virtual void PreAnimationSetup()
		{
			ticksPassed = 0;
			TickEvents(); //Trigger events at t=0 before next frame
		}

		protected virtual float TryThrowFleck(FleckData fleckData, float t, float count)
		{
			float frequency = fleckData.frequency.Evaluate(t);
			count += frequency / 60;
			int motesToThrow = Mathf.FloorToInt(count);
			count -= motesToThrow;
			for (int i = 0; i < motesToThrow; i++)
			{
				ThrowFleck(fleckData, t);
			}
			return count;
		}

		public void ThrowFleck(FleckData fleckData, float t)
		{
			float size = fleckData.size?.Evaluate(t) ?? 1;
			float? airTime = fleckData.airTime?.Evaluate(t);
			float? speed = fleckData.speed?.Evaluate(t);
			float? rotationRate = fleckData.rotationRate?.Evaluate(t);
			float angle = fleckData.angle.RandomInRange;

			Vector3 origin = position.ToVector3Shifted();
			if (!fleckData.lockFleckX)
			{
				origin.x = DrawPos.x;
			}
			if (!fleckData.lockFleckZ)
			{
				origin.z = DrawPos.z;
			}
			if (fleckData.drawOffset != null)
			{
				origin = origin.PointFromAngle(fleckData.drawOffset.Evaluate(t), angle);
			}

			if (!fleckData.xFleckPositionCurve.NullOrEmpty())
			{
				origin.x += fleckData.xFleckPositionCurve.Evaluate(t);
			}
			if (!fleckData.zFleckPositionCurve.NullOrEmpty())
			{
				origin.z += fleckData.zFleckPositionCurve.Evaluate(t);
			}

			origin += fleckData.originOffset;

			if (fleckData.originOffsetRange != null)
			{
				Vector3 offsetFrom = fleckData.originOffsetRange.from;
				Vector3 offsetTo = fleckData.originOffsetRange.to;
				float offsetRangeX = offsetFrom.x;
				if (offsetFrom.x != offsetTo.x)
				{
					offsetRangeX = Rand.Range(offsetFrom.x, offsetTo.x);
				}
				float offsetRangeY = offsetFrom.y;
				if (offsetFrom.y != offsetTo.y)
				{
					offsetRangeY = Rand.Range(offsetFrom.y, offsetTo.y);
				}
				float offsetRangeZ = offsetFrom.z;
				if (offsetFrom.z != offsetTo.z)
				{
					offsetRangeZ = Rand.Range(offsetFrom.z, offsetTo.z);
				}

				origin += new Vector3(offsetRangeX, offsetRangeY, offsetRangeZ);
			}

			origin.y = fleckData.def.altitudeLayer.AltitudeFor();
			ThrowFleck(fleckData.def, origin, Map, size, airTime, angle, speed, rotationRate);
		}

		public void ThrowFleck(FleckOneShot fleckOneShot)
		{
			float size = fleckOneShot.size?.RandomInRange ?? 1;
			float? airTime = fleckOneShot.airTime?.RandomInRange;
			float? speed = fleckOneShot.speed?.RandomInRange;
			float? rotationRate = fleckOneShot.rotationRate?.RandomInRange;
			float angle = fleckOneShot.angle.RandomInRange;

			Vector3 origin = position.ToVector3Shifted();
			if (!fleckOneShot.lockFleckX)
			{
				origin.x = DrawPos.x;
			}
			if (!fleckOneShot.lockFleckZ)
			{
				origin.z = DrawPos.z;
			}
			
			origin += fleckOneShot.originOffset;

			if (fleckOneShot.originOffsetRange != null)
			{
				Vector3 offsetFrom = fleckOneShot.originOffsetRange.from;
				Vector3 offsetTo = fleckOneShot.originOffsetRange.to;
				float offsetRangeX = offsetFrom.x;
				if (offsetFrom.x != offsetTo.x)
				{
					offsetRangeX = Rand.Range(offsetFrom.x, offsetTo.x);
				}
				float offsetRangeY = offsetFrom.y;
				if (offsetFrom.y != offsetTo.y)
				{
					offsetRangeY = Rand.Range(offsetFrom.y, offsetTo.y);
				}
				float offsetRangeZ = offsetFrom.z;
				if (offsetFrom.z != offsetTo.z)
				{
					offsetRangeZ = Rand.Range(offsetFrom.z, offsetTo.z);
				}

				origin += new Vector3(offsetRangeX, offsetRangeY, offsetRangeZ);
			}

			origin.y = fleckOneShot.def.altitudeLayer.AltitudeFor();
			ThrowFleck(fleckOneShot.def, origin, Map, size, airTime, angle, speed, rotationRate);
		}

		public static void ThrowFleck(FleckDef fleckDef, Vector3 loc, Map map, float size, float? airTime, float? angle, float? speed, float? rotationRate)
		{
			Rand.PushState();
			try
			{
				FleckCreationData fleckCreationData = FleckMaker.GetDataStatic(loc, map, fleckDef, size);
				if (rotationRate.HasValue) fleckCreationData.rotationRate = rotationRate.Value * (Rand.Value < 0.5f ? 1 : -1);
				if (speed.HasValue) fleckCreationData.velocitySpeed = speed.Value;
				if (angle.HasValue) fleckCreationData.velocityAngle = angle.Value;
				if (airTime.HasValue) fleckCreationData.airTimeLeft = airTime.Value;
				map.flecks.CreateFleck(fleckCreationData);
			}
			finally
			{
				Rand.PopState();
			}
		}

		/// <summary>
		/// Set initial vars for landing / takeoff
		/// </summary>
		/// <param name="landing"></param>
		public virtual void OrderProtocol(LaunchType launchType)
		{
			this.launchType = launchType;
			PreAnimationSetup();
		}

		/// <summary>
		/// FloatMenuOptions at <paramref name="tile"/> on world map based on launchProtocol
		/// </summary>
		/// <param name="tile"></param>
		public virtual IEnumerable<FloatMenuOption> GetFloatMenuOptionsAt(int tile)
		{
			if (AerialVehicleArrivalAction_FormVehicleCaravan.CanFormCaravanAt(vehicle, tile) && !Find.WorldObjects.AnyMapParentAt(tile))
			{
				yield return FloatMenuOption_LandCaravanEmptyTile(tile);
			}
			else if (Find.WorldObjects.MapParentAt(tile) is MapParent mapParent)
			{
				if (CanLandInSpecificCell(mapParent))
				{
					yield return FloatMenuOption_LandInsideMap(mapParent, tile);
				}
				else if (!mapParent.HasMap && AerialVehicleCompatibility.CanLandIn(mapParent))
				{
					foreach (FloatMenuOption option in AerialVehicleArrivalAction_LoadMap.GetFloatMenuOptions(vehicle, this, mapParent))
					{
						yield return option;
					}
				}
				if (vehicle.CompVehicleLauncher.ControlInFlight)
				{
					//yield return FloatMenuOption_ReconMap(parent, tile);
				}
				if (vehicle.CompVehicleLauncher.ControlInFlight && vehicle.CompVehicleTurrets != null) //REDO - strafe specific properties
				{
					//yield return FloatMenuOption_StrafeMap(parent, tile);
				}
			}
			if (Find.WorldObjects.SettlementAt(tile) is Settlement settlement)
			{
				if (settlement.Visitable)
				{
					foreach (FloatMenuOption option in FloatMenuOption_LandAtSettlement(settlement, tile))
					{
						yield return option;
					}
				}

				foreach (FloatMenuOption option in AerialVehicleArrivalAction_AttackSettlement.GetFloatMenuOptions(vehicle, this, settlement))
				{
					yield return option;
				}
			}
			if (Find.WorldObjects.SiteAt(tile) is Site site && !site.HasMap)
			{
				foreach (FloatMenuOption option in AerialVehicleArrivalAction_LoadMap.GetFloatMenuOptions(vehicle, this, site))
				{
					yield return option;
				}
			}
		}

		protected virtual FloatMenuOption FloatMenuOption_LandCaravanEmptyTile(int tile)
		{
			return new FloatMenuOption("FormCaravanHere".Translate(), delegate ()
			{
				if (vehicle.Spawned)
				{
					vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_FormVehicleCaravan(vehicle));
				}
				else
				{
					AerialVehicleInFlight aerialVehicle = AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle);
					aerialVehicle.OrderFlyToTiles(LaunchTargeter.FlightPath, aerialVehicle.DrawPos, new AerialVehicleArrivalAction_FormVehicleCaravan(vehicle));
				}
			}, MenuOptionPriority.Default, null, null, 0f, null, null);
		}

		protected virtual FloatMenuOption FloatMenuOption_LandInsideMap(MapParent mapParent, int tile)
		{
			return new FloatMenuOption("LandInExistingMap".Translate(vehicle.Label), delegate ()
			{
				Current.Game.CurrentMap = mapParent.Map;
				CameraJumper.TryHideWorld();
				LandingTargeter.Instance.BeginTargeting(vehicle, action: delegate (LocalTargetInfo target, Rot4 rot)
				{
					if (vehicle.Spawned)
					{
						vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_LandSpecificCell(vehicle, mapParent, tile, target.Cell, rot));
					}
					else
					{
						AerialVehicleInFlight aerialVehicle = AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle);
						aerialVehicle.arrivalAction = new AerialVehicleArrivalAction_LandSpecificCell(vehicle, mapParent, tile, target.Cell, rot);
						aerialVehicle.OrderFlyToTiles(LaunchTargeter.FlightPath, aerialVehicle.DrawPos, new AerialVehicleArrivalAction_LandSpecificCell(vehicle, mapParent, tile, target.Cell, rot));
						vehicle.CompVehicleLauncher.inFlight = true;
						CameraJumper.TryShowWorld();
					}
				}, allowRotating: vehicle.VehicleDef.rotatable, targetValidator: (targetInfo) => !Ext_Vehicles.IsRoofRestricted(vehicle.VehicleDef, targetInfo.Cell, mapParent.Map));
			}, MenuOptionPriority.Default, null, null, 0f, null, null);
		}

		protected virtual FloatMenuOption FloatMenuOption_ReconMap(MapParent mapParent, int tile)
		{
			if (mapParent.EnterCooldownBlocksEntering())
			{
				return new FloatMenuOption($"{"VF_AerialReconSite".Translate(mapParent.Label)} ({"EnterCooldownBlocksEntering".Translate()})", null);
			}
			return new FloatMenuOption("VF_AerialReconSite".Translate(mapParent.Label), delegate ()
			{
				if (vehicle.Spawned)
				{
					vehicle.CompVehicleLauncher.TryLaunch(mapParent.Tile, null, true);
				}
				else
				{
					AerialVehicleInFlight aerialVehicle = AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle);
					List<FlightNode> flightPath = new List<FlightNode>(LaunchTargeter.FlightPath);
					aerialVehicle.OrderFlyToTiles(flightPath, aerialVehicle.DrawPos);
					aerialVehicle.flightPath.ReconCircleAt(mapParent.Tile);
					vehicle.CompVehicleLauncher.inFlight = true;
				}
			});
		}

		protected virtual FloatMenuOption FloatMenuOption_StrafeMap(MapParent mapParent, int tile)
		{
			return new FloatMenuOption("VF_StrafeRun".Translate(), delegate ()
			{
				if (vehicle.Spawned)
				{
					LaunchTargeter.ContinueTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(ChoseWorldTarget), vehicle.Map.Tile, true, VehicleTex.TargeterMouseAttachment, true, null,
						(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => TargetingLabelGetter(target, tile, path, fuelCost));
				}
				else
				{
					AerialVehicleInFlight aerialVehicle = AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle);
					LaunchTargeter.Instance.ContinueTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(aerialVehicle.ChoseTargetOnMap), aerialVehicle, true, VehicleTex.TargeterMouseAttachment, false, null,
						(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => vehicle.CompVehicleLauncher.launchProtocol.TargetingLabelGetter(target, aerialVehicle.Tile, path, fuelCost));
				}
				CameraJumper.TryShowWorld();
				LaunchTargeter.Instance.RegisterActionOnTile(tile, new AerialVehicleArrivalAction_StrafeMap(vehicle, mapParent));
			}, MenuOptionPriority.Default, null, null, 0f, null, null);
		}

		protected virtual IEnumerable<FloatMenuOption> FloatMenuOption_LandAtSettlement(Settlement settlement, int tile)
		{
			if (settlement.Faction.def.techLevel <= TechLevel.Industrial)
			{
				yield return new FloatMenuOption("VF_LandVehicleHere".Translate(), delegate ()
				{
					if (vehicle.Spawned)
					{
						vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_FormVehicleCaravan(vehicle));
					}
					else
					{
						AerialVehicleInFlight aerialVehicle = AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle);
						aerialVehicle.OrderFlyToTiles(LaunchTargeter.FlightPath, aerialVehicle.DrawPos, new AerialVehicleArrivalAction_VisitSettlement(vehicle, settlement));
					}
				}, MenuOptionPriority.Default, null, null, 0f, null, null);
			}

			if (AerialVehicleArrivalAction_Trade.CanTradeWith(vehicle, settlement))
			{
				yield return new FloatMenuOption("TradeWith".Translate(settlement.Label), delegate ()
				{
					if (vehicle.Spawned)
					{
						vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_Trade(vehicle, settlement));
					}
					else
					{
						AerialVehicleInFlight aerialVehicle = AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle);
						aerialVehicle.OrderFlyToTiles(LaunchTargeter.FlightPath, aerialVehicle.DrawPos, new AerialVehicleArrivalAction_Trade(vehicle, settlement));
					}
				});
			}

			if (AerialVehicleArrivalAction_OfferGifts.CanOfferGiftsTo(vehicle, settlement))
			{
				yield return new FloatMenuOption("OfferGifts".Translate(settlement.Label), delegate ()
				{
					if (vehicle.Spawned)
					{
						vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_OfferGifts(vehicle, settlement));
					}
					else
					{
						AerialVehicleInFlight aerialVehicle = AerialVehicleLaunchHelper.GetOrMakeAerialVehicle(vehicle);
						aerialVehicle.OrderFlyToTiles(LaunchTargeter.FlightPath, aerialVehicle.DrawPos, new AerialVehicleArrivalAction_OfferGifts(vehicle, settlement));
					}
				});
			}
		}

		/// <summary>
		/// Begin choosing destination target for aerial vehicle
		/// </summary>
		public virtual void StartChoosingDestination()
		{
			CameraJumper.TryJump(CameraJumper.GetWorldTarget(vehicle));
			Find.WorldSelector.ClearSelection();
			int tile = vehicle.Map.Tile;
			LaunchTargeter.BeginTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(ChoseWorldTarget), vehicle.Map.Tile, true, VehicleTex.TargeterMouseAttachment, true, null,
				(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => TargetingLabelGetter(target, tile, path, fuelCost));
		}

		/// <summary>
		/// Get label on <paramref name="target"/> when targeting
		/// </summary>
		/// <param name="target"></param>
		/// <param name="path"></param>
		/// <param name="fuelCost"></param>
		/// <param name="launchAction"></param>
		public virtual string TargetingLabelGetter(GlobalTargetInfo target, int tile, List<FlightNode> path, float fuelCost)
		{
			if (!target.IsValid)
			{
				return null;
			}
			if (vehicle.CompFueledTravel != null && fuelCost > vehicle.CompFueledTravel.Fuel)
			{
				GUI.color = TexData.RedReadable;
				return "VF_NotEnoughFuel".Translate();
			}
			else if (vehicle.CompFueledTravel != null && target.IsValid && vehicle.CompVehicleLauncher.FuelNeededToLaunchAtDist(WorldHelper.GetTilePos(tile), target.Tile)  > (vehicle.CompFueledTravel.Fuel - fuelCost))
			{
				GUI.color = TexData.YellowReadable;
				return "VF_NoFuelReturnTrip".Translate();
			}
			IEnumerable<FloatMenuOption> source = GetFloatMenuOptionsAt(target.Tile);
			if (!source.Any())
			{
				return string.Empty;
			}
			if (source.Count() == 1)
			{
				if (source.First().Disabled)
				{
					GUI.color = TexData.RedReadable;
				}
				return source.First().Label;
			}
			MapParent mapVehicle;
			if ((mapVehicle = (target.WorldObject as MapParent)) != null)
			{
				return "ClickToSeeAvailableOrders_WorldObject".Translate(mapVehicle.LabelCap);
			}
			return "ClickToSeeAvailableOrders_Empty".Translate();
		}

		/// <summary>
		/// Last check when world target has been chosen
		/// </summary>
		/// <param name="target"></param>
		/// <param name="tile"></param>
		/// <param name="maxLaunchDistance"></param>
		/// <param name="launchAction"></param>
		public virtual bool ChoseWorldTarget(GlobalTargetInfo target, Vector3 pos, Func<GlobalTargetInfo, Vector3, Action<int, AerialVehicleArrivalAction, bool>, bool> validator, 
			Action<int, AerialVehicleArrivalAction, bool> launchAction)
		{
			map = vehicle.Map;
			return validator(target, pos, launchAction);
		}

		private bool ChoseWorldTarget(GlobalTargetInfo target, float fuelCost)
		{
			bool Validator(GlobalTargetInfo target, Vector3 pos, Action<int, AerialVehicleArrivalAction, bool> launchAction)
			{
				if (!target.IsValid)
				{
					Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				else if (Ext_Math.SphericalDistance(pos, WorldHelper.GetTilePos(target.Tile)) > vehicle.CompVehicleLauncher.MaxLaunchDistance || (vehicle.CompFueledTravel != null && fuelCost > vehicle.CompFueledTravel.Fuel))
				{
					Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				IEnumerable<FloatMenuOption> source = GetFloatMenuOptionsAt(target.Tile);
				if (!source.Any())
				{
					if (!WorldVehiclePathGrid.Instance.Passable(target.Tile, vehicle.VehicleDef))
					{
						Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
						return false;
					}
					launchAction(target.Tile, null, false);
					if (CurAnimationProperties.forcedRotation.HasValue)
					{
						vehicle.Rotation = CurAnimationProperties.forcedRotation.Value;
					}
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
						if (CurAnimationProperties.forcedRotation.HasValue)
						{
							vehicle.Rotation = CurAnimationProperties.forcedRotation.Value;
						}
						return true;
					}
					return false;
				}
			};
			return ChoseWorldTarget(target, WorldHelper.GetTilePos(vehicle.Map.Tile), Validator, new Action<int, AerialVehicleArrivalAction, bool>(vehicle.CompVehicleLauncher.TryLaunch));
		}

		public static bool CanLandInSpecificCell(MapParent mapParent)
		{
			return mapParent != null && mapParent.Spawned && mapParent.HasMap && (!mapParent.EnterCooldownBlocksEntering() ||
				FloatMenuAcceptanceReport.WithFailMessage("MessageEnterCooldownBlocksEntering".Translate(mapParent.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true))));
		}

		public virtual void ResolveProperties(LaunchProtocol reference)
		{
			int launchTypeCount = Enum.GetNames(typeof(LaunchType)).Count();
			cachedOverlayGraphicDatas = new List<GraphicDataLayered>[launchTypeCount];
			cachedOverlayGraphics = new List<Graphic>[launchTypeCount];
		}

		public virtual void ExposeData()
		{
			Scribe_References.Look(ref vehicle, nameof(vehicle), true);
			Scribe_Values.Look(ref ticksPassed, nameof(ticksPassed), 0);
			Scribe_Values.Look(ref effectsToThrow, nameof(effectsToThrow), 0);
			
			Scribe_Values.Look(ref drawOverlays, nameof(drawOverlays), true);
			Scribe_Values.Look(ref drawMotes, nameof(drawMotes), true);

			Scribe_Values.Look(ref launchType, nameof(launchType));
			Scribe_Values.Look(ref maxFlightNodes, nameof(maxFlightNodes), int.MaxValue);

			Scribe_References.Look(ref map, nameof(map));
			Scribe_Values.Look(ref position, nameof(position), defaultValue: IntVec3.Invalid);
		}

		[DebugAction(category = VehicleHarmony.VehiclesLabel, name = "Lock Camera to Thing", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void LockCameraToThing()
		{
			Map map = Find.CurrentMap;
			if (map == null)
			{
				Log.Error($"Attempting to use LockCameraToThing with null map.");
				return;
			}
			IntVec3 cell = UI.MouseCell();
			if (cell.InBounds(map))
			{
				List<Thing> thingList = map.thingGrid.ThingsListAtFast(cell);
				if (!thingList.NullOrEmpty())
				{
					List<FloatMenuOption> options = new List<FloatMenuOption>();
					foreach (Thing thing in thingList)
					{
						options.Add(new FloatMenuOption(thing.Label, delegate ()
						{
							CameraAttacher.Create(thing);
						}));
					}
					Find.WindowStack.Add(new FloatMenu(options));
				}
			}
		}

		public enum LaunchType : uint
		{
			Landing = 0,
			Takeoff = 1
		}
	}
}
