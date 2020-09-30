using System;
using System.Linq;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using UnityEngine;
using Vehicles.Defs;
using HarmonyLib;

namespace Vehicles
{
    public class CompFueledTravel : VehicleComp
    {
        private float fuel;
        private bool terminateMotes = false;
        private Vector3 motePosition;
        private float offsetX;
        private float offsetZ;
        private const float cellOffsetIntVec3ToVector3 = 0.5f;
        public CompProperties_FueledTravel Props => (CompProperties_FueledTravel)props;
        public float ConsumptionRatePerTick => FuelEfficiency / 60000f;
        public float Fuel => fuel;
        public float FuelPercent => fuel / FuelCapacity;
        public bool EmptyTank => fuel <= 0f;
        public bool FullTank => fuel == FuelCapacity;
        public int FuelCountToFull => Mathf.CeilToInt(FuelCapacity - fuel);

        private bool Charging => connectedPower != null && !FullTank && connectedPower.PowerNet.CurrentStoredEnergy() > PowerGainRate;
        private CompPower connectedPower;
        private bool postLoadReconnect;

        private const float PowerGainRate = 1f;
        public float dischargeRate;

        public VehiclePawn Pawn => parent as VehiclePawn;
        public FuelConsumptionCondition FuelCondition => Props.fuelConsumptionCondition;

        private float fuelCost;
        private float fuelCapacity;
        public float FuelEfficiency
        {
            get
            {
                return fuelCost;
            }
            set
            {
                if (value < 0)
                    fuelCost = 0f;
                fuelCost = value;
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
                if (value < 0)
                    fuelCapacity = 0f;
                fuelCapacity = value;
                if (fuel > fuelCapacity)
                    fuel = fuelCapacity;
            }
        }


        public bool SatisfiesFuelConsumptionConditional
        {
            get
            {
                if(Pawn.IsWorldPawn() && Pawn.GetVehicleCaravan() != null)
                {
                    if(Pawn.GetVehicleCaravan().vPather.MovingNow)
                    {
                        return true;
                    }
                    return false;
                }

                switch(FuelCondition)
                {
                    case FuelConsumptionCondition.Drafted:
                        if (Pawn.Drafted)
                            return true;
                        break;
                    case FuelConsumptionCondition.Moving:
                        if (Pawn.vPather.MovingNow)
                            return true;
                        break;
                    case FuelConsumptionCondition.Always:
                        return true;
                    default:
                        throw new NotImplementedException("FuelCondition " + FuelCondition + " Not Yet Implemented");
                }
                return false;
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
            Log.Message("REFUELING");
            fuel = FuelCapacity / 2;
        }

        public void ConsumeFuel(float amount)
        {
            if(fuel <= 0f)
                return;
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
            if(Find.Selector.SingleSelectedThing == parent)
            {
                yield return new Gizmo_RefuelableFuelTravel
                {
                    refuelable = this
                };
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

            if (Prefs.DevMode)
	        {
		        yield return new Command_Action
		        {
			        defaultLabel = "Debug: Set fuel to 0",
			        action = delegate()
			        {
				        fuel = 0f;
				        parent.BroadcastCompSignal("Refueled");
			        }
		        };
		        yield return new Command_Action
		        {
			        defaultLabel = "Debug: Set fuel to 0.1",
			        action = delegate()
			        {
				        fuel = 0.1f;
				        parent.BroadcastCompSignal("Refueled");
			        }
		        };
		        yield return new Command_Action
		        {
			        defaultLabel = "Debug: Set fuel to max",
			        action = delegate()
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
                if (EmptyTank && !VehicleMod.settings.debugDraftAnyShip)
                {
                    Pawn.drafter.Drafted = false;
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
            if (Pawn.Spawned)
            {
                if (!FullTank)
                {
                    Pawn.Map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Pawn, VehicleRequest.Refuel);
                }
                else
                {
                    Pawn.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Pawn, VehicleRequest.Refuel);
                }
            }

            if (Pawn.vPather.Moving)
            {
                DisconnectPower();
            }
        }

        public bool TryConnectPower()
        {
            if (Props.electricPowered)
            {
                foreach (IntVec3 cell in Pawn.InhabitedCells(1))
                {
                    Thing building = Pawn.Map.thingGrid.ThingAt(cell, ThingCategory.Building);
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
                        SPTuple2<float, float> moteOffset = HelperMethods.ShipDrawOffset(Pawn, offset.xOffset, offset.zOffset, out SPTuple2<float,float> rotationOffset);
                        offsetX = moteOffset.First;
                        offsetZ = moteOffset.Second;

                        motePosition = new Vector3(parent.Position.x + offsetX + cellOffsetIntVec3ToVector3, parent.Position.y, parent.Position.z + offsetZ + cellOffsetIntVec3ToVector3);
                    
                        MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(Props.MoteDisplayed, null);
                        mote.exactPosition = motePosition;
                        mote.Scale = 1f;
                        mote.rotationRate = 15f;
                        float moteAngle = offset.predeterminedAngleVector is null ? (Pawn.Map.components.First(x => x is WindDirectional) as WindDirectional).WindDirection : (float)offset.predeterminedAngleVector;
                        float moteSpeed = offset.windAffected ? Rand.Range(0.5f, 3.5f) * Pawn.Map.windManager.WindSpeed : offset.moteThrownSpeed;
                        mote.SetVelocity(moteAngle, moteSpeed);
                        HelperMethods.ThrowMoteEnhanced(motePosition, parent.Map, mote);
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
        
        public float GetRelativeAngle(float mapDirectionalAngle)
        {
            switch(Pawn.Rotation.AsInt)
            {
                case 1:
                    if (HelperMethods.ShipAngle(Pawn) != 0)
                        return mapDirectionalAngle - (90 + HelperMethods.ShipAngle(Pawn));
                    return mapDirectionalAngle - 90;
                case 2:
                    return mapDirectionalAngle - 180;
                case 3:
                    if (HelperMethods.ShipAngle(Pawn) != 0)
                        return mapDirectionalAngle - (270 + HelperMethods.ShipAngle(Pawn));
                    return mapDirectionalAngle - 270;
                default:
                    return mapDirectionalAngle;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if(!respawningAfterLoad)
            {
                InitializeProperties();
                dischargeRate = ConsumptionRatePerTick * 0.1f;

                if (Pawn.Faction != Faction.OfPlayer)
                    RefuelHalfway();
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
