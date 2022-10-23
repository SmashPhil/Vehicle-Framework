using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public abstract class LaunchProtocol : IExposable
	{
		protected VehiclePawn vehicle;
		
		protected Vector3 drawPos;
		protected int ticksPassed;
		protected bool landing;

		protected Map currentMap;
		protected Map targetMap;

		protected List<Graphic> cachedLaunchGraphics;
		protected List<Graphic> cachedLandingGraphics;

		protected List<GraphicDataLayered> cachedLaunchGraphicDatas;
		protected List<GraphicDataLayered> cachedLandingGraphicDatas;

		/* -- Xml Input -- */

		protected int maxFlightNodes = int.MaxValue;

		public LaunchProtocolProperties landingProperties;
		public LaunchProtocolProperties launchProperties;

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

			landingProperties = reference.landingProperties;
			launchProperties = reference.launchProperties;
		}

		public int TicksPassed => ticksPassed;

		/// <summary>
		/// Check if launch protocol is set for landing or takeoff
		/// </summary>
		public bool IsLanding => landing;

		/// <summary>
		/// Message displayed to user when CanLaunchNow returns false
		/// </summary>
		public abstract string FailLaunchMessage { get; }

		/// <summary>
		/// Conditions in which shuttle can initiate takeoff
		/// </summary>
		/// <returns></returns>
		public abstract bool CanLaunchNow { get; }

		/// <summary>
		/// Maximum number of flight nodes able to be selected in LaunchTargeter
		/// </summary>
		public virtual int MaxFlightNodes => maxFlightNodes;

		public virtual float TimeInAnimation
		{
			get
			{
				if (landing)
				{
					return (float)ticksPassed / landingProperties.maxTicks;
				}
				else
				{
					return (float)ticksPassed / launchProperties.maxTicks;
				}
			}
		}

		public virtual Vector3 DrawPos
		{
			get
			{
				return drawPos;
			}
		}

		public List<GraphicDataLayered> LaunchGraphicDatas
		{
			get
			{
				if (cachedLaunchGraphicDatas.NullOrEmpty() && !(launchProperties?.additionalLaunchTextures.NullOrEmpty() ?? true))
				{
					cachedLaunchGraphicDatas = new List<GraphicDataLayered>();
					foreach (GraphicDataLayered graphicData in launchProperties.additionalLaunchTextures)
					{
						GraphicDataLayered graphicDataNew = new GraphicDataLayered();
						cachedLaunchGraphicDatas.Add(graphicDataNew);
						graphicDataNew.CopyFrom(graphicData);
					}
				}
				return cachedLaunchGraphicDatas;
			}
		}

		public List<GraphicDataLayered> LandingGraphicDatas
		{
			get
			{
				if (cachedLandingGraphicDatas.NullOrEmpty() && !(landingProperties?.additionalLandingTextures.NullOrEmpty() ?? true))
				{
					cachedLandingGraphicDatas = new List<GraphicDataLayered>();
					foreach (GraphicDataLayered graphicData in landingProperties.additionalLandingTextures)
					{
						GraphicDataLayered graphicDataNew = new GraphicDataLayered();
						cachedLandingGraphicDatas.Add(graphicDataNew);
						graphicDataNew.CopyFrom(graphicData);
					}
				}
				return cachedLandingGraphicDatas;
			}
		}

		public List<Graphic> LaunchGraphics
		{
			get
			{
				if (cachedLaunchGraphics.NullOrEmpty() && !LaunchGraphicDatas.NullOrEmpty())
				{
					cachedLaunchGraphics = new List<Graphic>();
					foreach (GraphicDataLayered graphicData in LaunchGraphicDatas)
					{
						cachedLaunchGraphics.Add(graphicData.Graphic);
					}
				}
				return cachedLaunchGraphics;
			}
		}

		public List<Graphic> LandingGraphics
		{
			get
			{
				if (cachedLandingGraphics.NullOrEmpty() && !LandingGraphicDatas.NullOrEmpty())
				{
					cachedLandingGraphics = new List<Graphic>();
					foreach (GraphicDataLayered graphicData in LandingGraphicDatas)
					{
						cachedLandingGraphics.Add(graphicData.Graphic);
					}
				}
				return cachedLandingGraphics;
			}
		}

		/// <summary>
		/// Speed of skyfaller upon launching
		/// </summary>
		protected virtual float CurrentSpeed
		{
			get
			{
				if (landing)
				{
					if (landingProperties?.speedCurve is null)
					{
						return landingProperties?.speed ?? 0.5f;
					}
					return landingProperties.speedCurve.Evaluate(ticksPassed) * landingProperties.speed;
				}
				if (launchProperties?.speedCurve is null)
				{
					return launchProperties?.speed ?? 0.5f;
				}
				return launchProperties.speedCurve.Evaluate(ticksPassed) * launchProperties.speed;
			}
		}

		/// <summary>
		/// Launch gizmo for specific takeoff versions
		/// </summary>
		public abstract Command_Action LaunchCommand { get; }

		/// <summary>
		/// Takeoff animation has finished
		/// </summary>
		/// <returns></returns>
		public virtual bool FinishedTakeoff(VehicleSkyfaller skyfaller)
		{
			return ticksPassed >= launchProperties.maxTicks;
		}

		/// <summary>
		/// Landing animation is finished
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public virtual bool FinishedLanding(VehicleSkyfaller skyfaller)
		{
			return ticksPassed <= 0;
		}

		/// <summary>
		/// Landing animation when vehicle is entering map through flight
		/// </summary>
		/// <param name="flip"></param>
		/// <returns>Position rendered on the map</returns>
		public abstract Vector3 AnimateLanding(float layer, bool flip);

		/// <summary>
		/// Takeoff animation when vehicle is leaving map through flight
		/// </summary>
		/// <returns>Position rendered on the map</returns>
		public abstract Vector3 AnimateTakeoff(float layer, bool flip);

		/// <summary>
		/// Takeoff animation for additional textures specified in properties on launch
		/// </summary>
		public virtual void DrawAdditionalLaunchTextures(float layer)
		{
			if (!LaunchGraphics.NullOrEmpty())
			{
				for (int i = 0; i < LaunchGraphics.Count; i++)
				{
					Graphic graphic = LaunchGraphics[i];
					Vector3 texPosition = new Vector3(DrawPos.x, layer, DrawPos.z);
					if (graphic is Graphic_Animate animationGraphic)
					{
						animationGraphic.DrawWorkerAnimated(texPosition, Rot4.North, ticksPassed, 0f);
					}
					else
					{
						graphic.DrawWorker(texPosition, Rot4.North, null, null, 0f);
					}
				}
			}
		}

		/// <summary>
		/// Takeoff animation for additional textures specified in properties on launch
		/// </summary>
		public virtual void DrawAdditionalLandingTextures(float layer)
		{
			if (!LandingGraphics.NullOrEmpty())
			{
				for (int i = 0; i < LandingGraphics.Count; i++)
				{
					Graphic graphic = LandingGraphics[i];
					Vector3 texPosition = new Vector3(DrawPos.x, layer, DrawPos.z);
					if (graphic is Graphic_Animate animationGraphic)
					{
						animationGraphic.DrawWorkerAnimated(texPosition, Rot4.North, ticksPassed, 0f);
					}
					else
					{
						graphic.DrawWorker(texPosition, Rot4.North, null, null, 0f);
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
			if (IsLanding)
			{
				TickLanding();
			}
			else
			{
				TickTakeoff();
			}
		}

		/// <summary>
		/// Ticker for landing
		/// </summary>
		protected virtual void TickLanding()
		{
			ticksPassed--;
		}

		/// <summary>
		/// Ticker for taking off
		/// </summary>
		protected virtual void TickTakeoff()
		{
			ticksPassed++;
		}

		/// <summary>
		/// Set starting Draw Position for leaving
		/// </summary>
		/// <param name="pos"></param>
		public virtual void SetPositionLeaving(Vector3 pos, Rot4 rot, Map map)
		{
			drawPos = pos;
			vehicle.Rotation = rot;
			currentMap = map;
		}
		
		/// <summary>
		/// Set starting Draw Position for arrival
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="rot"></param>
		/// <param name="map"></param>
		public virtual void SetPositionArriving(Vector3 pos, Rot4 rot, Map map)
		{
			drawPos = pos;
			vehicle.Rotation = rot;
			targetMap = map;
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
			ticksPassed = landing ? landingProperties.maxTicks : 0;
		}

		/// <summary>
		/// Set initial vars for landing / takeoff
		/// </summary>
		/// <param name="landing"></param>
		public virtual void OrderProtocol(bool landing)
		{
			this.landing = landing;
			PreAnimationSetup();
		}

		/// <summary>
		/// Additional Drawer method for LandingTargeter
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="rot"></param>
		public virtual void DrawLandingTarget(IntVec3 cell, Rot4 rot)
		{
		}

		/// <summary>
		/// FloatMenuOptions at <paramref name="tile"/> on world map based on launchProtocol
		/// </summary>
		/// <param name="tile"></param>
		public abstract IEnumerable<FloatMenuOption> GetFloatMenuOptionsAt(int tile);

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
			if (fuelCost > vehicle.CompFueledTravel.Fuel)
			{
				GUI.color = TexData.RedReadable;
				return "VehicleNotEnoughFuel".Translate();
			}
			else if (target.IsValid && vehicle.CompVehicleLauncher.FuelNeededToLaunchAtDist(WorldHelper.GetTilePos(tile), target.Tile)  > (vehicle.CompFueledTravel.Fuel - fuelCost))
			{
				GUI.color = TexData.YellowReadable;
				return "VehicleNoFuelReturnTrip".Translate();
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
		/// Begin choosing destination target for aerial vehicle
		/// </summary>
		public virtual void StartChoosingDestination()
		{
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
			currentMap = vehicle.Map;
			targetMap = Find.WorldObjects.MapParentAt(target.Tile)?.Map;
			return validator(target, pos, launchAction);
		}

		protected bool ChoseWorldTarget(GlobalTargetInfo target, float fuelCost)
		{
			bool Validator(GlobalTargetInfo target, Vector3 pos, Action<int, AerialVehicleArrivalAction, bool> launchAction)
			{
				if (!target.IsValid)
				{
					Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
					return false;
				}
				else if (Ext_Math.SphericalDistance(pos, WorldHelper.GetTilePos(target.Tile)) > vehicle.CompVehicleLauncher.MaxLaunchDistance || fuelCost > vehicle.CompFueledTravel.Fuel)
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
					if (landingProperties.forcedRotation.HasValue && !landing)
					{
						vehicle.Rotation = landingProperties.forcedRotation.Value;
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
						if (landingProperties.forcedRotation.HasValue && !landing)
						{
							vehicle.Rotation = landingProperties.forcedRotation.Value;
						}
						return true;
					}
					return false;
				}
			};
			return ChoseWorldTarget(target, WorldHelper.GetTilePos(vehicle.Map.Tile), Validator, new Action<int, AerialVehicleArrivalAction, bool>(vehicle.CompVehicleLauncher.TryLaunch));
		}

		public virtual void ResolveProperties(LaunchProtocol reference)
		{
			launchProperties = reference.launchProperties;
			landingProperties = reference.landingProperties;
		}

		public virtual void ExposeData()
		{
			Scribe_References.Look(ref vehicle, nameof(vehicle), true);
			Scribe_Values.Look(ref drawPos, nameof(drawPos));
			Scribe_Values.Look(ref ticksPassed, nameof(ticksPassed));

			Scribe_Values.Look(ref landing, nameof(landing));
			Scribe_Values.Look(ref maxFlightNodes, nameof(maxFlightNodes), int.MaxValue);

			Scribe_References.Look(ref currentMap, nameof(currentMap));
			Scribe_References.Look(ref targetMap, nameof(targetMap));

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				LaunchProtocol defProtocol = vehicle.GetComp<CompVehicleLauncher>().Props.launchProtocol;
				launchProperties = defProtocol.launchProperties;
				landingProperties = defProtocol.landingProperties;
			}
		}

		public static bool CanLandInSpecificCell(MapParent mapParent)
		{
			return mapParent != null && mapParent.Spawned && mapParent.HasMap && (!mapParent.EnterCooldownBlocksEntering() || 
				FloatMenuAcceptanceReport.WithFailMessage("MessageEnterCooldownBlocksEntering".Translate(mapParent.EnterCooldownTicksLeft().ToStringTicksToPeriod(true, false, true, true))));
		}

		public static void ThrowRocketExhaust(Vector3 vector, Map map, float size, float angle, float velocity)
		{
			vector += size * new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f);
			if (!vector.InBounds(map))
			{
				return;
			}
			MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(MoteDefOf.Mote_RocketExhaust, null);
			moteThrown.Scale = Rand.Range(4f, 6f) * size;
			moteThrown.rotationRate = Rand.Range(-3f, 3f);
			moteThrown.exactPosition = vector;
			moteThrown.SetVelocity(angle, velocity);
			GenSpawn.Spawn(moteThrown, vector.ToIntVec3(), map, WipeMode.Vanish);
		}

		public static void ThrowRocketExhaustLong(Vector3 vector, Map map, float size)
		{
			vector += size * new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f);
			if (!vector.InBounds(map))
			{
				return;
			}
			MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(MoteDefOf.Mote_RocketExhaust_Long, null);
			moteThrown.Scale = Rand.Range(4f, 6f) * size;
			moteThrown.rotationRate = Rand.Range(-3f, 3f);
			moteThrown.exactPosition = vector;
			moteThrown.SetVelocity(Rand.Range(0f, 360f), 0.12f);
			GenSpawn.Spawn(moteThrown, vector.ToIntVec3(), map, WipeMode.Vanish);
		}

		public static void ThrowRocketSmokeLong(Vector3 vector, Map map, float size)
		{
			vector += size * new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f);
			if (!vector.InBounds(map))
			{
				return;
			}
			float angle = Rand.Range(0f, 360f);
			MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(MoteDefOf.Mote_RocketSmoke_Long, null);
			moteThrown.Scale = Rand.Range(4f, 6f) * size;
			moteThrown.rotationRate = Rand.Range(-4f, 4f);
			moteThrown.exactPosition = vector;
			moteThrown.SetVelocity(angle, Rand.Range(5, 10));
			GenSpawn.Spawn(moteThrown, vector.ToIntVec3(), map, WipeMode.Vanish);
		}

		public static void ThrowMoteLong(ThingDef mote, Vector3 vector, Map map, float size, float angle, float speed)
		{
			vector += size * new Vector3(Rand.Value - 0.5f, 0f, Rand.Value - 0.5f);
			if (!vector.InBounds(map))
			{
				return;
			}
			MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(mote, null);
			moteThrown.Scale = Rand.Range(4f, 6f) * size;
			moteThrown.rotationRate = Rand.Range(-4f, 4f);
			moteThrown.exactPosition = vector;
			moteThrown.SetVelocity(angle, speed);
			GenSpawn.Spawn(moteThrown, vector.ToIntVec3(), map, WipeMode.Vanish);
		}

		public class MoteInfo : IExposable
		{
			public ThingDef moteDef;
			public FloatRange angle;
			public FloatRange speed;
			public FloatRange size;

			public MoteInfo()
			{
			}

			public MoteInfo(ThingDef moteDef, FloatRange angle, FloatRange speed, FloatRange size)
			{
				this.moteDef = moteDef ?? MoteDefOf.Mote_RocketSmoke_Long;
				this.angle = angle;
				this.speed = speed;
				this.size = size;
			}

			public void ExposeData()
			{
				Scribe_Defs.Look(ref moteDef, "moteDef");
				Scribe_Values.Look(ref angle, "angle");
				Scribe_Values.Look(ref speed, "speed");
				Scribe_Values.Look(ref size, "size");
			}
		}
	}
}
