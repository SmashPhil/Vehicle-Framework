using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.Defs;

namespace Vehicles
{
	public class CompFueledTravel : VehicleAIComp
	{
		private const float EfficiencyTickMultiplier = 1 / 60000f;
		private const float CellOffsetIntVec3ToVector3 = 0.5f;
		private const float PowerGainRate = 1f;

		private float fuel;
		private bool terminateMotes = false;
		private Vector3 motePosition;
		private float offsetX;
		private float offsetZ;

		private CompPower connectedPower;
		private bool postLoadReconnect;
		public float dischargeRate;
		private float fuelCost;
		private float fuelCapacity;

		public CompProperties_FueledTravel Props => (CompProperties_FueledTravel)props;
		public float ConsumptionRatePerTick => FuelEfficiency * EfficiencyTickMultiplier;
		public float Fuel => fuel;
		public float FuelPercent => Fuel / FuelCapacity;
		public bool EmptyTank => Fuel <= 0f;
		public bool FullTank => fuel == FuelCapacity;
		public int FuelCountToFull => Mathf.CeilToInt(FuelCapacity - Fuel);

		private bool Charging => connectedPower != null && !FullTank && connectedPower.PowerNet.CurrentStoredEnergy() > PowerGainRate;
		public VehiclePawn Vehicle => parent as VehiclePawn;
		public FuelConsumptionCondition FuelCondition => Props.fuelConsumptionCondition;
		public Gizmo FuelCountGizmo => new Gizmo_RefuelableFuelTravel { refuelable = this };

		public float FuelEfficiency
		{
			get
			{
				return fuelCost;
			}
			set
			{
				fuelCost = value;
				if (fuelCost < 0)
				{
					fuelCost = 0f;
				}
			}
		}

		public float FuelCapacity
		{
			get
			{
				return fuelCapacity;
			}
			set
			{
				fuelCapacity = value;
				if (fuelCapacity < 0)
				{
					fuelCapacity = 0f;
				}

				if (fuel > fuelCapacity)
				{
					fuel = fuelCapacity;
				}
			}
		}

		public bool SatisfiesFuelConsumptionConditional
		{
			get
			{
				bool caravanMoving = Vehicle.IsWorldPawn() && Vehicle.GetVehicleCaravan() is VehicleCaravan caravan && caravan.vPather.MovingNow;

				return FuelCondition switch
				{
					FuelConsumptionCondition.Drafted => Vehicle.Drafted || caravanMoving,
					FuelConsumptionCondition.Flying => false, //Add flying condition
					FuelConsumptionCondition.FlyingOrDrafted => Vehicle.Drafted || caravanMoving,//Add flying as well
					FuelConsumptionCondition.Moving => Vehicle.vPather.MovingNow || caravanMoving,
					FuelConsumptionCondition.Always => true,
					_ => throw new NotImplementedException("FuelCondition " + FuelCondition + " Not Yet Implemented"),
				};
			}
		}

		public Thing ClosestFuelAvailable(Pawn pawn)
		{
			if (Props.electricPowered)
			{
				return null;
			}
			Predicate<Thing> validator = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, false) && x.def == Props.fuelType;
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(Props.fuelType), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn,
				false), 9999f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
		}

		public void Refuel(List<Thing> fuelThings)
		{
			int num = FuelCountToFull;
			while(num > 0 && fuelThings.Count > 0)
			{
				Thing thing = fuelThings.Pop();
				int num2 = Mathf.Min(num, thing.stackCount);
				Refuel(num2);
				thing.SplitOff(num2).Destroy(DestroyMode.Vanish);
				num -= num2;
			}
		}

		public void Refuel(float amount)
		{
			if(fuel >= FuelCapacity)
				return;
			fuel += amount;
			if(fuel >= FuelCapacity)
			{
				fuel = FuelCapacity;
			}
		}

		/// <summary>
		/// Only for Incident spawning / AI spawning. Will randomize fuel levels later (REDO)
		/// </summary>
		private void RefuelHalfway()
		{
			fuel = FuelCapacity / 2;
		}

		public void ConsumeFuel(float amount)
		{
			if (fuel <= 0f)
			{
				return;
			}
			fuel -= amount;
			if(fuel <= 0f)
			{
				fuel = 0f;
				parent.BroadcastCompSignal("RanOutOfFuel");
			}
		}

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			if(!(parent is Pawn))
			{
				Log.Error("ThingComp CompFueledTravel is ONLY meant for use with Pawns. - Smash Phil");
			}
		}

		public override void PostDraw()
		{
			base.PostDraw();
			if(EmptyTank)
			{
				parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.OutOfFuel);
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}

			if(Find.Selector.SingleSelectedThing == parent)
			{
				yield return FuelCountGizmo;
			}

			if (Props.electricPowered)
			{
				yield return new Command_Toggle
				{
					hotKey = KeyBindingDefOf.Command_TogglePower,
					icon = VehicleTex.FlickerIcon,
					defaultLabel = "VehicleElectricFlick".Translate(),
					defaultDesc = "VehicleElectricFlickDesc".Translate(),
					isActive = (() => Charging),
					toggleAction = delegate()
					{
						if(!Charging)
						{
							TryConnectPower();
						}
						else
						{
							DisconnectPower();
						}
					}
				};
			}

			foreach (Gizmo gizmo in DevModeGizmos())
			{
				yield return gizmo;
			}
		}

		public IEnumerable<Gizmo> DevModeGizmos()
		{
			if (Prefs.DevMode)
			{
				yield return new Command_Action
				{
					defaultLabel = "Debug: Set fuel to 0",
					action = delegate ()
					{
						fuel = 0f;
						parent.BroadcastCompSignal("Refueled");
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "Debug: Set fuel to 0.1",
					action = delegate ()
					{
						fuel = 0.1f;
						parent.BroadcastCompSignal("Refueled");
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "Debug: Set fuel to half",
					action = delegate ()
					{
						fuel = FuelCapacity / 2;
						parent.BroadcastCompSignal("Refueled");
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "Debug: Set fuel to max",
					action = delegate ()
					{
						fuel = FuelCapacity;
						parent.BroadcastCompSignal("Refueled");
					}
				};
			}
		}

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			yield return new FloatMenuOption("Refuel".Translate().ToString(),
				delegate ()
				{
					Job job = new Job(JobDefOf_Vehicles.RefuelVehicle, parent, ClosestFuelAvailable(selPawn));
					selPawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder);
				}, MenuOptionPriority.Default, null, null, 0f, null, null);
		}

		public override void CompTick()
		{
			base.CompTick();
			if (SatisfiesFuelConsumptionConditional)
			{
				ConsumeFuel(ConsumptionRatePerTick);
				if(!terminateMotes && !Props.motesGenerated.NullOrEmpty())
				{
					if(Find.TickManager.TicksGame % Props.TicksToSpawnMote == 0)
						DrawMotes();
				}
				if (EmptyTank && !VehicleMod.settings.debug.debugDraftAnyShip)
				{
					Vehicle.drafter.Drafted = false;
				}
			}
			else if (Props.electricPowered && !Charging)
			{
				ConsumeFuel(ConsumptionRatePerTick * 0.1f);
			}

			if (Props.electricPowered && Charging && Find.TickManager.TicksGame % Props.ticksPerCharge == 0)
			{
				AccessTools.Method(typeof(PowerNet), "ChangeStoredEnergy").Invoke(connectedPower.PowerNet, new object[] { -PowerGainRate });
				Refuel(PowerGainRate);
			}
		}

		public override void CompTickRare()
		{
			base.CompTickRare();
			if (Vehicle.Spawned)
			{
				if (!FullTank)
				{
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle, ReservationType.Refuel);
				}
				else
				{
					Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.Refuel);
				}
			}

			if (Vehicle.vPather.Moving)
			{
				DisconnectPower();
			}
		}

		public bool TryConnectPower()
		{
			if (Props.electricPowered)
			{
				foreach (IntVec3 cell in Vehicle.InhabitedCells(1))
				{
					Thing building = Vehicle.Map.thingGrid.ThingAt(cell, ThingCategory.Building);
					if(building != null)
					{
						CompPower powerSource = building.TryGetComp<CompPower>();
						if(powerSource != null && powerSource.TransmitsPowerNow)
						{
							connectedPower = powerSource;
							break;
						}
					}
				}
			}
			return connectedPower is null;
		}

		public void DisconnectPower()
		{
			connectedPower = null;
		}

		public void DrawMotes()
		{
			foreach(OffsetMote offset in Props.motesGenerated)
			{
				for(int i = 0; i < offset.NumTimesSpawned; i++)
				{
					try
					{
						Pair<float, float> moteOffset = RenderHelper.TurretDrawOffset(Vehicle.Rotation.AsAngle + Vehicle.Angle, offset.xOffset, offset.zOffset, out Pair<float,float> rotationOffset);
						offsetX = moteOffset.First;
						offsetZ = moteOffset.Second;

						motePosition = new Vector3(parent.Position.x + offsetX + CellOffsetIntVec3ToVector3, parent.Position.y, parent.Position.z + offsetZ + CellOffsetIntVec3ToVector3);
					
						MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(Props.MoteDisplayed, null);
						mote.exactPosition = motePosition;
						mote.Scale = 1f;
						mote.rotationRate = 15f;
						float moteAngle = offset.predeterminedAngleVector is null ? (Vehicle.Map.components.First(x => x is WindDirectional) as WindDirectional).WindDirection : (float)offset.predeterminedAngleVector;
						float moteSpeed = offset.windAffected ? Rand.Range(0.5f, 3.5f) * Vehicle.Map.windManager.WindSpeed : offset.moteThrownSpeed;
						mote.SetVelocity(moteAngle, moteSpeed);
						RenderHelper.ThrowMoteEnhanced(motePosition, parent.Map, mote);
					}
					catch(Exception ex)
					{
						Log.Error(string.Concat(new object[]
						{
							"Exception thrown while trying to display ",
							Props.MoteDisplayed.defName,
							" Terminating MoteDraw Method from ",
							parent.LabelShort,
							" Exception: ",
							ex.Message
						}));
						terminateMotes = true;
						return;
					}
				}
			} 
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if(!respawningAfterLoad)
			{
				InitializeProperties();
				dischargeRate = ConsumptionRatePerTick * 0.1f;

				if (Vehicle.Faction != Faction.OfPlayer)
				{
					RefuelHalfway();
				}
			}

			if (postLoadReconnect)
			{
				TryConnectPower();
			}
		}

		public void InitializeProperties()
		{
			fuelCost = Props.fuelConsumptionRate;
			fuelCapacity = Props.fuelCapacity;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();

			Scribe_Values.Look(ref fuelCost, "fuelCost");
			Scribe_Values.Look(ref fuelCapacity, "fuelCapacity");
			Scribe_Values.Look(ref fuel, "fuel");

			Scribe_Values.Look(ref dischargeRate, "dischargeRate");
			if(Scribe.mode == LoadSaveMode.Saving)
				postLoadReconnect = Charging;
			Scribe_Values.Look(ref postLoadReconnect, "postLoadReconnect", false, true);
		}
	}
}
