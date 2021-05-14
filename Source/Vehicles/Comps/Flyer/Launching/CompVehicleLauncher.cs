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
		public List<LaunchProtocol> launchProtocols;

		public float fuelEfficiencyWorldModifier;
		public float flySpeedModifier;
		public float rateOfClimbModifier;
		public int maxAltitudeModifier;
		public int landingAltitudeModifier;

		private DeploymentTimer timer;

		public LaunchProtocol SelectedLaunchProtocol { get; set; }

		public bool Roofed => Vehicle.Position.Roofed(Vehicle.Map);
		public bool AnyLeftToLoad => Vehicle.cargoToLoad.NotNullAndAny();
		public VehiclePawn Vehicle => parent as VehiclePawn;
		public CompProperties_VehicleLauncher Props => props as CompProperties_VehicleLauncher;

		public float FlySpeed => flySpeedModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), "flySpeed", Props.flySpeed);
		public float FuelEfficiencyWorld => fuelEfficiencyWorldModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), "fuelEfficiencyWorld", Props.fuelEfficiencyWorld);
		public int FixedMaxDistance => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), "fixedLaunchDistanceMax",  Props.fixedLaunchDistanceMax);
		public float RateOfClimb => rateOfClimbModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), "rateOfClimb", Props.rateOfClimb);
		public int MaxAltitude => maxAltitudeModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), "maxAltitude", Props.maxAltitude);
		public int LandingAltitude => landingAltitudeModifier + SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), "landingAltitude", Props.landingAltitude);
		public bool ControlInFlight => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), "controlInFlight", Props.controlInFlight);
		public int ReconDistance => SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(CompProperties_VehicleLauncher), "reconDistance", Props.reconDistance);

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

			if (!launchProtocols.NotNullAndAny())
			{
				Log.ErrorOnce($"No launch protocols for {Vehicle}. At least 1 must be included in order to initiate takeoff.", Vehicle.thingIDNumber);
				yield break;
			}
			foreach (LaunchProtocol protocol in launchProtocols)
			{
				var command = protocol.LaunchCommand;
				if (!protocol.CanLaunchNow)
				{
					command.Disable(protocol.FailLaunchMessage);
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
			List<int> flightPath = LaunchTargeter.FlightPath;
			if (flightPath.LastOrDefault() != destinationTile)
			{
				flightPath.Add(destinationTile);
			}
			Vehicle.inFlight = true;
			VehicleSkyfaller_Leaving vehicleLeaving = (VehicleSkyfaller_Leaving)ThingMaker.MakeThing(Props.skyfallerLeaving);
			vehicleLeaving.arrivalAction = arrivalAction;
			vehicleLeaving.vehicle = Vehicle;
			vehicleLeaving.launchProtocol = SelectedLaunchProtocol;
			vehicleLeaving.flightPath = new List<int>(flightPath);
			vehicleLeaving.orderRecon = recon;
			GenSpawn.Spawn(vehicleLeaving, Vehicle.Position, Vehicle.Map, Vehicle.Rotation, WipeMode.Vanish);

			if (Vehicle.Spawned)
			{
				Vehicle.DeSpawn(DestroyMode.Vanish);
			}
			Find.WorldPawns.PassToWorld(Vehicle);

			CameraJumper.TryHideWorld();
		}

		public float FuelNeededToLaunchAtDist(Vector3 origin, int destination)
		{
			float tileDistance = Ext_Math.SphericalDistance(origin, Find.WorldGrid.GetTileCenter(destination));
			return FuelNeededToLaunchAtDist(tileDistance);
		}

		public float FuelNeededToLaunchAtDist(float tileDistance)
		{
			float speedPctPerTick = (AerialVehicleInFlight.PctPerTick / tileDistance) * FlySpeed;
			float amount = Vehicle.CompFueledTravel.ConsumptionRatePerTick / FuelEfficiencyWorld;
			return amount * (1f / speedPctPerTick);
		}

		public virtual void InitializeLaunchProtocols(bool respawningAfterLoad)
		{
			if (!respawningAfterLoad)
			{
				launchProtocols = new List<LaunchProtocol>();
				if (Props.launchProtocols.NotNullAndAny())
				{
					foreach (var protocol in Props.launchProtocols)
					{
						LaunchProtocol newProtocol = (LaunchProtocol)Activator.CreateInstance(protocol.GetType(), new object[] { protocol, Vehicle });
						launchProtocols.Add(newProtocol);
					}
				}
			}
			foreach (LaunchProtocol protocol in launchProtocols)
			{
				LaunchProtocol matchingProtocol = Props.launchProtocols.FirstOrDefault(l => l.GetType() == protocol.GetType());
				protocol.ResolveProperties(matchingProtocol);
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			timer.Tick(Vehicle);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				fuelEfficiencyWorldModifier = 0;
			}
			InitializeLaunchProtocols(respawningAfterLoad);
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Collections.Look(ref launchProtocols, "launchProtocols");
			Scribe_Values.Look(ref flySpeedModifier, "flySpeedModifier");
			Scribe_Values.Look(ref fuelEfficiencyWorldModifier, "fuelEfficiencyWorldModifier");
			Scribe_Values.Look(ref rateOfClimbModifier, "rateOfClimbModifier");
			Scribe_Values.Look(ref maxAltitudeModifier, "maxAltitudeModifier");
			Scribe_Values.Look(ref landingAltitudeModifier, "landingAltitudeModifier");

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
					Log.Error($"{entry} is not a valid <struct>DeploymentTimer</struct> format. Exception: {ex}");
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
