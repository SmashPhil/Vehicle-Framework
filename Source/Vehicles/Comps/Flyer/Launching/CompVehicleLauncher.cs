using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class CompVehicleLauncher : VehicleComp
	{
		public static SimpleCurve ClimbRateCurve = new SimpleCurve()
		{
			new CurvePoint(0, -5),
			new CurvePoint(0.15f, -2.5f),
			new CurvePoint(0.25f, -1f),
			new CurvePoint(0.35f, -0.25f),
			new CurvePoint(0.45f, 0),
			new CurvePoint(0.5f, 0.25f),
			new CurvePoint(0.75f, 0.45f),
			new CurvePoint(0.8f, 0.75f),
			new CurvePoint(0.9f, 0.95f),
			new CurvePoint(1, 1)
		};

		public LaunchProtocol launchProtocol;

		public float fuelEfficiencyWorldModifier;
		public float flySpeedModifier;
		public float rateOfClimbModifier;
		public int maxAltitudeModifier;
		public int landingAltitudeModifier;

		private DeploymentTimer timer;

		public bool inFlight = false;

		public bool Roofed => Vehicle.Position.Roofed(Vehicle.Map);
		public bool AnyLeftToLoad => Vehicle.cargoToLoad.NotNullAndAny();
		public VehiclePawn Vehicle => parent as VehiclePawn;
		public CompProperties_VehicleLauncher Props => props as CompProperties_VehicleLauncher;

		public float FlySpeed => flySpeedModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.flySpeed), Props.flySpeed);
		public float FuelEfficiencyWorld => fuelEfficiencyWorldModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.fuelEfficiencyWorld), Props.fuelEfficiencyWorld);
		public int FixedMaxDistance => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.fixedLaunchDistanceMax), Props.fixedLaunchDistanceMax);
		public bool SpaceFlight => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.spaceFlight), Props.spaceFlight);
		public float RateOfClimb => rateOfClimbModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.rateOfClimb), Props.rateOfClimb);
		public int MaxAltitude => maxAltitudeModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.maxAltitude), Props.maxAltitude);
		public int LandingAltitude => landingAltitudeModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.landingAltitude), Props.landingAltitude);
		public bool ControlInFlight => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.controlInFlight), Props.controlInFlight);
		public int ReconDistance => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.reconDistance), Props.reconDistance);

		public virtual bool ControlledDescent => ClimbRateStat >= 0;

		public virtual bool AnyFlightControl { get; private set; }

		public virtual float ClimbRateStat
		{
			get
			{
				bool flight = launchProtocol.CanLaunchNow && FlySpeed > 0 && (!Vehicle.CompFueledTravel?.EmptyTank ?? true);
				if (!flight)
				{
					return ClimbRateCurve.Evaluate(0);
				}
				float flightControl = Vehicle.statHandler.StatEfficiency(VehicleStatDefOf.FlightControl);
				AnyFlightControl = flightControl > 0;
				float flightSpeed = Vehicle.statHandler.StatEfficiency(VehicleStatDefOf.FlightSpeed);
				return ClimbRateCurve.Evaluate(Mathf.Min(flightControl, flightSpeed)) * RateOfClimb;
			}
		}

		public IEnumerable<VehicleTurret> StrafeTurrets
		{
			get
			{
				if (Vehicle?.CompCannons is null)
				{
					Log.Error($"Cannot retrieve <property>StrafeTurrets</property> with no <type>CompCannons</type> comp.");
					yield break;
				}
				foreach (VehicleTurret turret in Vehicle.CompCannons.Cannons.Where(t => Props.strafing.turrets.Contains(t.key) || Props.strafing.turrets.Contains(t.groupKey)))
				{
					yield return turret;
				}
			}
		}

		public int MaxLaunchDistance
		{
			get
			{
				if (FixedMaxDistance > 0)
				{
					return FixedMaxDistance;
				}
				return int.MaxValue;
			}
		}

		public void SetTimedDeployment()
		{
			timer.Reset();
		}

		public ShuttleLaunchStatus GetShuttleStatus(GlobalTargetInfo mouseTarget, Vector3 origin)
		{
			if (FixedMaxDistance > 0 && LaunchTargeter.TotalDistance > FixedMaxDistance)
			{
				return ShuttleLaunchStatus.Invalid;
			}
			else
			{
				if (!mouseTarget.IsValid || LaunchTargeter.TotalFuelCost > Vehicle.CompFueledTravel.Fuel)
				{
					return ShuttleLaunchStatus.Invalid;
				}
				else if (FuelNeededToLaunchAtDist(origin, mouseTarget.Tile) > (Vehicle.CompFueledTravel.Fuel - LaunchTargeter.TotalFuelCost))
				{
					return ShuttleLaunchStatus.NoReturnTrip;
				}
				return ShuttleLaunchStatus.Valid;
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}

			if (launchProtocol is null)
			{
				Log.ErrorOnce($"No launch protocols for {Vehicle}. At least 1 must be included in order to initiate takeoff.", Vehicle.thingIDNumber);
				yield break;
			}
			var command = launchProtocol.LaunchCommand;
			if (!launchProtocol.CanLaunchNow)
			{
				command.Disable(launchProtocol.FailLaunchMessage);
			}
			if (FlySpeed <= 0)
			{
				command.Disable("Vehicles_NoFlySpeed".Translate());
			}
			if (Roofed)
			{
				command.Disable("CommandLaunchGroupFailUnderRoof".Translate());
			}
			if (Vehicle.vPather.Moving)
			{
				command.Disable("Vehicles_CannotLaunchWhileMoving".Translate(Vehicle.LabelShort));
			}
			if (SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(VehicleDef), "vehicleMovementPermissions", Vehicle.VehicleDef.vehicleMovementPermissions) > VehiclePermissions.NotAllowed && (!Vehicle.CanMoveFinal || Vehicle.Angle != 0))
			{
				command.Disable("Vehicles_CannotMove".Translate(Vehicle.LabelShort));
			}
			if (!VehicleMod.settings.debug.debugDraftAnyShip && !Vehicle.PawnCountToOperateFullfilled)
			{
				command.Disable("Vehicles_NotEnoughToOperate".Translate());
			}
			if (Vehicle.CompFueledTravel != null && Vehicle.CompFueledTravel.EmptyTank)
			{
				command.Disable("VehicleLaunchOutOfFuel".Translate());
			}
			yield return command;
		}

		public override string CompInspectStringExtra()
		{
			if (Vehicle.PawnCountToOperateFullfilled && AnyLeftToLoad)
			{
				return "NotReadyForLaunch".Translate() + ": " + "TransportPodInGroupHasSomethingLeftToLoad".Translate().CapitalizeFirst() + ".";
			}
			return "ReadyForLaunch".Translate();
		}

		public void TryLaunch(int destinationTile, AerialVehicleArrivalAction arrivalAction, bool recon = false)
		{
			if (!Vehicle.Spawned)
			{
				Log.Error("Tried to launch " + Vehicle + ", but it's unspawned.");
				return;
			}
			List<FlightNode> flightPath = LaunchTargeter.FlightPath;
			if (flightPath.LastOrDefault().tile != destinationTile)
			{
				flightPath.Add(new FlightNode(destinationTile, null));
			}
			Vehicle.CompVehicleLauncher.inFlight = true;
			VehicleSkyfaller_Leaving vehicleLeaving = (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(Props.skyfallerLeaving, Vehicle);
			vehicleLeaving.arrivalAction = arrivalAction;
			vehicleLeaving.vehicle = Vehicle;
			vehicleLeaving.flightPath = new List<FlightNode>(flightPath);
			vehicleLeaving.orderRecon = recon;
			GenSpawn.Spawn(vehicleLeaving, Vehicle.Position, Vehicle.Map, Vehicle.CompVehicleLauncher.launchProtocol.landingProperties.forcedRotation ?? Vehicle.Rotation, WipeMode.Vanish);

			if (Vehicle.Spawned)
			{
				Vehicle.DeSpawn(DestroyMode.Vanish);
			}
			Find.WorldPawns.PassToWorld(Vehicle);

			CameraJumper.TryHideWorld();
		}

		public float FuelNeededToLaunchAtDist(Vector3 origin, int destination)
		{
			float tileDistance = Ext_Math.SphericalDistance(origin, WorldHelper.GetTilePos(destination));
			return FuelNeededToLaunchAtDist(tileDistance);
		}

		public float FuelNeededToLaunchAtDist(float tileDistance)
		{
			float speedPctPerTick = (AerialVehicleInFlight.PctPerTick / tileDistance) * FlySpeed;
			float amount = Vehicle.CompFueledTravel.ConsumptionRatePerTick / FuelEfficiencyWorld;
			return amount * (1f / speedPctPerTick);
		}

		public virtual void InitializeLaunchProtocols(bool regenerateProtocols)
		{
			if (regenerateProtocols)
			{
				launchProtocol = (LaunchProtocol)Activator.CreateInstance(Props.launchProtocol.GetType(), new object[] { Props.launchProtocol, Vehicle });
			}
			launchProtocol.ResolveProperties(Props.launchProtocol);
		}

		public override void PostLoad()
		{
			InitializeLaunchProtocols(false);
		}

		public override void CompTick()
		{
			base.CompTick();
			timer.Tick(Vehicle);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			inFlight = false;
			if (!respawningAfterLoad)
			{
				fuelEfficiencyWorldModifier = 0;
			}
			InitializeLaunchProtocols(!respawningAfterLoad);
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Deep.Look(ref launchProtocol, "launchProtocol");
			Scribe_Values.Look(ref flySpeedModifier, "flySpeedModifier");
			Scribe_Values.Look(ref fuelEfficiencyWorldModifier, "fuelEfficiencyWorldModifier");
			Scribe_Values.Look(ref rateOfClimbModifier, "rateOfClimbModifier");
			Scribe_Values.Look(ref maxAltitudeModifier, "maxAltitudeModifier");
			Scribe_Values.Look(ref landingAltitudeModifier, "landingAltitudeModifier");

			Scribe_Values.Look(ref inFlight, "inFlight");
			Scribe_Values.Look(ref timer, "timer");
		}

		public struct DeploymentTimer
		{
			private int ticksLeft;
			private bool enabled;

			public DeploymentTimer(int ticksLeft, bool enabled)
			{
				this.ticksLeft = ticksLeft;
				this.enabled = enabled;
			}

			public static DeploymentTimer Default => new DeploymentTimer(0, false);

			public void Reset()
			{
				ticksLeft = Mathf.RoundToInt(VehicleMod.settings.main.delayDeployOnLanding * 60);
				enabled = true;
			}

			public void Tick(VehiclePawn vehicle)
			{
				ticksLeft--;
				if (enabled)
				{
					if (ticksLeft <= 0)
					{
						enabled = false;
						vehicle.DisembarkAll();
					}
				}
			}

			public static DeploymentTimer FromString(string entry)
			{
				entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
				string[] data = entry.Split(new char[] { ',' });

				try
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					int ticksLeft = Convert.ToInt32(data[0], invariantCulture);
					bool enabled = Convert.ToBoolean(data[1], invariantCulture);
					return new DeploymentTimer(ticksLeft, enabled);
				}
				catch (Exception ex)
				{
					SmashLog.Error($"{entry} is not a valid <struct>DeploymentTimer</struct> format. Exception: {ex}");
					return Default;
				}
			}

			public override string ToString()
			{
				return $"({ticksLeft},{enabled})";
			}
		}
	}
}
