using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	[HeaderTitle(Label = nameof(CompVehicleLauncher))]
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

		[GraphEditable]
		public LaunchProtocol launchProtocol;

		public float fuelEfficiencyWorldModifier;
		public float flightSpeedModifier;
		public float rateOfClimbModifier;
		public int maxAltitudeModifier;
		public int landingAltitudeModifier;

		private DeploymentTimer timer;

		public bool inFlight = false;

		public virtual bool AnyFlightControl { get; private set; }

		public bool Roofed => Vehicle.Position.Roofed(Vehicle.Map);
		public bool AnyLeftToLoad => Vehicle.cargoToLoad.NotNullAndAny();

		public CompProperties_VehicleLauncher Props => props as CompProperties_VehicleLauncher;

		public float FlightSpeed => flightSpeedModifier + Vehicle.GetStatValue(VehicleStatDefOf.FlightSpeed);
		public float FuelConsumptionWorldMultiplier => fuelEfficiencyWorldModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.fuelConsumptionWorldMultiplier), Props.fuelConsumptionWorldMultiplier);
		public int FixedMaxDistance => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.fixedLaunchDistanceMax), Props.fixedLaunchDistanceMax);
		public bool SpaceFlight => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.spaceFlight), Props.spaceFlight);
		public float RateOfClimb => rateOfClimbModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.rateOfClimb), Props.rateOfClimb);
		public int MaxAltitude => maxAltitudeModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.maxAltitude), Props.maxAltitude);
		public int LandingAltitude => landingAltitudeModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.landingAltitude), Props.landingAltitude);
		public bool ControlInFlight => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.controlInFlight), Props.controlInFlight);
		public int ReconDistance => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), nameof(Props.reconDistance), Props.reconDistance);

		public virtual bool ControlledDescent => ClimbRateStat >= 0;

		public override bool TickByRequest => true;

		public override IEnumerable<AnimationDriver> Animations => launchProtocol.Animations;

		public virtual float ClimbRateStat
		{
			get
			{
				bool flight = launchProtocol.CanLaunchNow && FlightSpeed > 0 && (!Vehicle.CompFueledTravel?.EmptyTank ?? true);
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
				if (Vehicle?.CompVehicleTurrets is null)
				{
					Log.Error($"Cannot retrieve <property>StrafeTurrets</property> with no <type>CompCannons</type> comp.");
					yield break;
				}
				foreach (VehicleTurret turret in Vehicle.CompVehicleTurrets.turrets.Where(t => Props.strafing.turrets.Contains(t.key) || Props.strafing.turrets.Contains(t.groupKey)))
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
			StartTicking();
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
			if (!CanLaunchWithCargoCapacity(out string disableReason))
			{
				command.Disable(disableReason);
			}
			yield return command;
		}

		public bool CanLaunchWithCargoCapacity(out string disableReason)
		{
			disableReason = null;
			if (Vehicle.Spawned)
			{
				if (Vehicle.vPather.Moving)
				{
					disableReason = "VF_CannotLaunchWhileMoving".Translate(Vehicle.LabelShort);
				}
				else if (Roofed)
				{
					disableReason = "CommandLaunchGroupFailUnderRoof".Translate();
				}
			}

			if (SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(VehicleDef), nameof(VehicleDef.vehicleMovementPermissions), Vehicle.VehicleDef.vehicleMovementPermissions) > VehiclePermissions.NotAllowed)
			{
				if (!Vehicle.CanMoveFinal || Vehicle.Angle != 0)
				{
					disableReason = "VF_CannotLaunchImmobile".Translate(Vehicle.LabelShort);
				}
			}
			else
			{
				float capacity = Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity);
				if (MassUtility.InventoryMass(Vehicle) > capacity)
				{
					disableReason = "VF_CannotLaunchOverEncumbered".Translate(Vehicle.LabelShort);
				}
			}

			if (!VehicleMod.settings.debug.debugDraftAnyVehicle && !Vehicle.CanMoveWithOperators)
			{
				disableReason = "VF_NotEnoughToOperate".Translate();
			}
			else if (Vehicle.CompFueledTravel != null && Vehicle.CompFueledTravel.EmptyTank)
			{
				disableReason = "VF_LaunchOutOfFuel".Translate();
			}
			else if(FlightSpeed <= 0)
			{
				disableReason = "VF_NoFlightSpeed".Translate();
			}

			if (!launchProtocol.CanLaunchNow)
			{
				disableReason = launchProtocol.FailLaunchMessage;
			}

			return disableReason.NullOrEmpty();
		}

		public override string CompInspectStringExtra()
		{
			if (Vehicle.CanMoveWithOperators && AnyLeftToLoad)
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
			Vehicle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
			VehicleSkyfaller_Leaving vehicleLeaving = (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(Props.skyfallerLeaving, Vehicle);
			vehicleLeaving.arrivalAction = arrivalAction;
			vehicleLeaving.vehicle = Vehicle;
			vehicleLeaving.flightPath = new List<FlightNode>(flightPath);
			vehicleLeaving.orderRecon = recon;
			GenSpawn.Spawn(vehicleLeaving, Vehicle.Position, Vehicle.Map, Vehicle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.forcedRotation ?? Vehicle.Rotation, WipeMode.Vanish);

			if (Vehicle.Spawned)
			{
				Vehicle.DeSpawn(DestroyMode.Vanish);
			}
			CameraJumper.TryHideWorld();
			Vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLaunch].ExecuteEvents();
		}

		[DebugAction(VehicleHarmony.VehiclesLabel)]
		private static void DoGCPass()
		{
			HarmonyLib.AccessTools.Field(typeof(WorldPawnGC), "lastSuccessfulGCTick").SetValue(Find.WorldPawns.gc, -1);
		}

		public float FuelNeededToLaunchAtDist(Vector3 origin, int destination)
		{
			float tileDistance = Ext_Math.SphericalDistance(origin, WorldHelper.GetTilePos(destination));
			return FuelNeededToLaunchAtDist(tileDistance);
		}

		public float FuelNeededToLaunchAtDist(float tileDistance)
		{
			float speedPctPerTick = (AerialVehicleInFlight.PctPerTick / tileDistance) * FlightSpeed;
			float amount = Vehicle.CompFueledTravel.ConsumptionRatePerTick * FuelConsumptionWorldMultiplier;
			return amount / speedPctPerTick;
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
			timer.Tick(Vehicle);
			if (timer.Expired)
			{
				StopTicking();
			}
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
			Scribe_Deep.Look(ref launchProtocol, nameof(launchProtocol));
			Scribe_Values.Look(ref flightSpeedModifier, nameof(flightSpeedModifier));
			Scribe_Values.Look(ref fuelEfficiencyWorldModifier, nameof(fuelEfficiencyWorldModifier));
			Scribe_Values.Look(ref rateOfClimbModifier, nameof(rateOfClimbModifier));
			Scribe_Values.Look(ref maxAltitudeModifier, nameof(maxAltitudeModifier));
			Scribe_Values.Look(ref landingAltitudeModifier, nameof(landingAltitudeModifier));

			Scribe_Values.Look(ref inFlight, nameof(inFlight));
			Scribe_Values.Look(ref timer, nameof(timer));
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

			public bool Expired => !enabled || ticksLeft <= 0;

			public void Reset()
			{
				ticksLeft = Mathf.RoundToInt(VehicleMod.settings.main.delayDeployOnLanding * 60);
				enabled = true;
			}

			/// <summary>
			/// Tick DeploymentTimer for delayed disembarkation 
			/// </summary>
			/// <param name="vehicle"></param>
			/// <returns></returns>
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
