﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    public class VehicleCaravan : Caravan
    {
        public VehicleCaravan() : base()
        {
            vPather = new VehicleCaravan_PathFollower(this);
            vTweener = new VehicleCaravan_Tweener(this);
        }

        public override Vector3 DrawPos => vTweener.TweenedPos;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (Find.WorldSelector.SingleSelectedObject == this)
	        {
		        yield return new Gizmo_CaravanInfo(this);
	        }
	        foreach (Gizmo gizmo in base.GetGizmos().Where(g => g is Command_Action && (g as Command_Action).defaultLabel != "Dev: Mental break" 
                && (g as Command_Action).defaultLabel != "Dev: Make random pawn hungry" && (g as Command_Action).defaultLabel != "Dev: Kill random pawn" 
                && (g as Command_Action).defaultLabel != "Dev: Harm random pawn" && (g as Command_Action).defaultLabel != "Dev: Down random pawn"
                && (g as Command_Action).defaultLabel != "Dev: Plague on random pawn" && (g as Command_Action).defaultLabel != "Dev: Teleport to destination"))
	        {
		        yield return gizmo;
	        }

	        if (IsPlayerControlled)
	        {
		        if (Find.WorldSelector.SingleSelectedObject == this)
		        {
			        yield return SettleInEmptyTileUtility.SettleCommand(this);
		        }
		        if (Find.WorldSelector.SingleSelectedObject == this)
		        {
			        if (PawnsListForReading.Count((Pawn x) => x.IsColonist) >= 2)
			        {
				        yield return new Command_Action
				        {
					        defaultLabel = "CommandSplitCaravan".Translate(),
					        defaultDesc = "CommandSplitCaravanDesc".Translate(),
					        icon = SplitCommand,
					        hotKey = KeyBindingDefOf.Misc5,
					        action = delegate()
					        {
						        Find.WindowStack.Add(new Dialog_SplitCaravan(this));
					        }
				        };
			        }
		        }
		        if (vPather.Moving)
		        {
			        yield return new Command_Toggle
			        {
				        hotKey = KeyBindingDefOf.Misc1,
				        isActive = (() => vPather.Paused),
				        toggleAction = delegate()
				        {
					        if (!vPather.Moving)
					        {
						        return;
					        }
					        vPather.Paused = !vPather.Paused;
				        },
				        defaultDesc = "CommandToggleCaravanPauseDesc".Translate(2f.ToString("0.#"), 0.3f.ToStringPercent()),
				        icon = TexCommand.PauseCaravan,
				        defaultLabel = "CommandPauseCaravan".Translate()
			        };
		        }
		        if (CaravanMergeUtility.ShouldShowMergeCommand)
		        {
			        yield return CaravanMergeUtility.MergeCommand(this);
		        }
		        foreach (Gizmo gizmo2 in this.forage.GetGizmos())
		        {
			        yield return gizmo2;
		        }

		        foreach (WorldObject worldObject in Find.WorldObjects.ObjectsAt(base.Tile))
		        {
			        foreach (Gizmo gizmo3 in worldObject.GetCaravanGizmos(this))
			        {
				        yield return gizmo3;
			        }
		        }
	        }
	        if (Prefs.DevMode)
	        {
                //REDO
		        //yield return new Command_Action
		        //{
			       // defaultLabel = "Dev: Mental break",
			       // action = delegate()
			       // {
				      //  if ((from x in PawnsListForReading
				      //  where x.RaceProps.Humanlike && !x.InMentalState
				      //  select x).TryRandomElement(out Pawn pawn))
				      //  {
					     //   pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_Sad, null, false, false, null, false);
				      //  }
			       // }
		        //};
		        //yield return new Command_Action
		        //{
			       // defaultLabel = "Dev: Make random pawn hungry",
			       // action = delegate()
			       // {
				      //  Pawn pawn;
				      //  if ((from x in this.PawnsListForReading
				      //  where x.needs.food != null
				      //  select x).TryRandomElement(out pawn))
				      //  {
					     //   pawn.needs.food.CurLevelPercentage = 0f;
				      //  }
			       // }
		        //};
		        //yield return new Command_Action
		        //{
			       // defaultLabel = "Dev: Kill random pawn",
			       // action = delegate()
			       // {
				      //  Pawn pawn;
				      //  if (this.PawnsListForReading.TryRandomElement(out pawn))
				      //  {
					     //   pawn.Kill(null, null);
					     //   Messages.Message("Dev: Killed " + pawn.LabelShort, this, MessageTypeDefOf.TaskCompletion, false);
				      //  }
			       // }
		        //};
		        //yield return new Command_Action
		        //{
			       // defaultLabel = "Dev: Harm random pawn",
			       // action = delegate()
			       // {
				      //  Pawn pawn;
				      //  if (this.PawnsListForReading.TryRandomElement(out pawn))
				      //  {
					     //   DamageInfo dinfo = new DamageInfo(DamageDefOf.Scratch, 10f, 999f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
					     //   pawn.TakeDamage(dinfo);
				      //  }
			       // }
		        //};
		        //yield return new Command_Action
		        //{
			       // defaultLabel = "Dev: Down random pawn",
			       // action = delegate()
			       // {
				      //  Pawn pawn;
				      //  if ((from x in this.PawnsListForReading
				      //  where !x.Downed
				      //  select x).TryRandomElement(out pawn))
				      //  {
					     //   HealthUtility.DamageUntilDowned(pawn, true);
					     //   Messages.Message("Dev: Downed " + pawn.LabelShort, this, MessageTypeDefOf.TaskCompletion, false);
				      //  }
			       // }
		        //};
		        //yield return new Command_Action
		        //{
			       // defaultLabel = "Dev: Plague on random pawn",
			       // action = delegate()
			       // {
				      //  Pawn pawn;
				      //  if ((from x in this.PawnsListForReading
				      //  where !x.Downed
				      //  select x).TryRandomElement(out pawn))
				      //  {
					     //   Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.Plague, pawn, null);
					     //   hediff.Severity = HediffDefOf.Plague.stages[1].minSeverity - 0.001f;
					     //   pawn.health.AddHediff(hediff, null, null, null);
					     //   Messages.Message("Dev: Gave advanced plague to " + pawn.LabelShort, this, MessageTypeDefOf.TaskCompletion, false);
				      //  }
			       // }
		        //};
		        yield return new Command_Action
		        {
			        defaultLabel = "Vehicle Dev: Teleport to destination",
			        action = delegate()
			        {
				        base.Tile = vPather.Destination;
				        vPather.StopDead();
			        }
		        };
	        }
            if(HelperMethods.HasBoat(this) && (Find.World.CoastDirectionAt(Tile).IsValid || HelperMethods.RiverIsValid(Tile, PawnsListForReading.Where(x => HelperMethods.IsBoat(x)).ToList())))
            {
                if(!vPather.Moving && !PawnsListForReading.Any(x => !HelperMethods.IsBoat(x)))
                {
                    Command_Action dock = new Command_Action();
                    dock.icon = TexCommandVehicles.Anchor;
                    dock.defaultLabel = Find.WorldObjects.AnySettlementBaseAt(Tile) ? "CommandDockShip".Translate() : "CommandDockShipDisembark".Translate();
                    dock.defaultDesc = Find.WorldObjects.AnySettlementBaseAt(Tile) ? "CommandDockShipDesc".Translate(Find.WorldObjects.SettlementBaseAt(Tile)) : "CommandDockShipObjectDesc".Translate();
                    dock.action = delegate ()
                    {
                        List<WorldObject> objects = Find.WorldObjects.ObjectsAt(Tile).ToList();
                        if(!objects.All(x => x is Caravan))
                            HelperMethods.ToggleDocking(this, true);
                        else
                            HelperMethods.SpawnDockedBoatObject(this);
                    };

                    yield return dock;
                }
                else if (!vPather.Moving && PawnsListForReading.Any(x => !HelperMethods.IsBoat(x)))
                {
                    Command_Action undock = new Command_Action();
                    undock.icon = TexCommandVehicles.UnloadAll;
                    undock.defaultLabel = "CommandUndockShip".Translate();
                    undock.defaultDesc = "CommandUndockShipDesc".Translate(Label);
                    undock.action = delegate ()
                    {
                        HelperMethods.ToggleDocking(this, false);
                    };

                    yield return undock;
                }
            }
        }

        public void Notify_VehicleTeleported()
        {
            vTweener.ResetTweenedPosToRoot();
            vPather.Notify_Teleported_Int();
        }

        public override void Notify_MemberDied(Pawn member)
        {
            if(!Spawned)
            {
                Log.Error("Caravan member died in an unspawned caravan. Unspawned caravans shouldn't be kept for more than a single frame.", false);
            }
            if(!PawnsListForReading.Any(x => HelperMethods.IsVehicle(x) && !x.Dead && x.GetComp<CompVehicle>().AllPawnsAboard.Any((Pawn y) => y != member && IsOwner(y))))
            {
                RemovePawn(member);
                if (Faction == Faction.OfPlayer)
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelAllCaravanColonistsDied".Translate(), "LetterAllCaravanColonistsDied".Translate(Name).CapitalizeFirst(), LetterDefOf.NegativeEvent, new GlobalTargetInfo(Tile), null, null);
                }
                pawns.Clear();
                Find.WorldObjects.Remove(this);
            }
            else
            {
                member.Strip();
                RemovePawn(member);
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (stringBuilder.Length != 0)
                stringBuilder.AppendLine();
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int num5 = 0;
            int numS = 0;
            foreach(Pawn ship in PawnsListForReading.Where(x => HelperMethods.IsVehicle(x)))
            {
                numS++;
                foreach(Pawn p in ship.GetComp<CompVehicle>().AllPawnsAboard)
                {
                    if(p.IsColonist)
                        num++;
                    if(p.RaceProps.Animal)
                        num2++;
                    if(p.IsPrisoner)
                        num3++;
                    if(p.Downed)
                        num4++;
                    if(p.InMentalState)
                        num5++;
                }
            }
            foreach(Pawn p in PawnsListForReading.Where(x => !HelperMethods.IsVehicle(x)))
            {
                if (p.IsColonist)
                    num++;
                if (p.RaceProps.Animal)
                    num2++;
                if (p.IsPrisoner)
                    num3++;
                if (p.Downed)
                    num4++;
                if (p.InMentalState)
                    num5++;
            }

            if (numS > 1)
            {
                Dictionary<Thing, int> vehicleCounts = new Dictionary<Thing, int>();
                foreach(Pawn p in PawnsListForReading.Where(x => HelperMethods.IsVehicle(x)))
                {
                    if(vehicleCounts.ContainsKey(p))
                    {
                        vehicleCounts[p]++;
                    }
                    else
                    {
                        vehicleCounts.Add(p, 1);
                    }
                }

                foreach(KeyValuePair<Thing, int> vehicles in vehicleCounts)
                {
                    stringBuilder.Append($"{vehicles.Value} {vehicles.Key.LabelCap}");
                }
            }
            stringBuilder.Append(", " + "CaravanColonistsCount".Translate(num, (num != 1) ? Faction.OfPlayer.def.pawnsPlural : Faction.OfPlayer.def.pawnSingular));
            if (num2 == 1)
                stringBuilder.Append(", " + "CaravanAnimal".Translate());
            else if (num2 > 1)
                stringBuilder.Append(", " + "CaravanAnimalsCount".Translate(num2));
            if (num3 == 1)
                stringBuilder.Append(", " + "CaravanPrisoner".Translate());
            else if (num3 > 1)
                stringBuilder.Append(", " + "CaravanPrisonersCount".Translate(num3));
            stringBuilder.AppendLine();
            if (num5 > 0)
                stringBuilder.Append("CaravanPawnsInMentalState".Translate(num5));
            if (num4 > 0)
            {
                if (num5 > 0)
                {
                    stringBuilder.Append(", ");
                }
                stringBuilder.Append("CaravanPawnsDowned".Translate(num4));
            }
            if (num5 > 0 || num4 > 0)
            {
                stringBuilder.AppendLine();
            }

            if(vPather.Moving)
            {
                if (vPather.ArrivalAction != null)
                    stringBuilder.Append(vPather.ArrivalAction.ReportString);
                else if (HelperMethods.HasBoat(this))
                    stringBuilder.Append("CaravanSailing".Translate());
                else
                    stringBuilder.Append("CaravanTraveling".Translate());
            }
            else
            {
                Settlement settlementBase = CaravanVisitUtility.SettlementVisitedNow(this);
                if (!(settlementBase is null))
                    stringBuilder.Append("CaravanVisiting".Translate(settlementBase.Label));
                else
                    stringBuilder.Append("CaravanWaiting".Translate());
            }
            if (vPather.Moving)
            {
                float num6 = (float)CaravanArrivalTimeEstimator.EstimatedTicksToArrive(this, true) / 60000f;
                stringBuilder.AppendLine();
                stringBuilder.Append("CaravanEstimatedTimeToDestination".Translate(num6.ToString("0.#")));
            }
            if (AllOwnersDowned)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("AllCaravanMembersDowned".Translate());
            }
            else if (AllOwnersHaveMentalBreak)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("AllCaravanMembersMentalBreak".Translate());
            }
            else if (ImmobilizedByMass)
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("CaravanImmobilizedByMass".Translate());
            }
            if (needs.AnyPawnOutOfFood(out string text))
            {
                stringBuilder.AppendLine();
                stringBuilder.Append("CaravanOutOfFood".Translate());
                if (!text.NullOrEmpty())
                {
                    stringBuilder.Append(" ");
                    stringBuilder.Append(text);
                    stringBuilder.Append(".");
                }
            }
            if (!vPather.MovingNow)
            {
                int usedBedCount = beds.GetUsedBedCount();
                stringBuilder.AppendLine();
                stringBuilder.Append(CaravanBedUtility.AppendUsingBedsLabel("CaravanResting".Translate(), usedBedCount));
            }
            else
            {
                string inspectStringLine = carryTracker.GetInspectStringLine();
                if (!inspectStringLine.NullOrEmpty())
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append(inspectStringLine);
                }
                string inBedForMedicalReasonsInspectStringLine = beds.GetInBedForMedicalReasonsInspectStringLine();
                if (!inBedForMedicalReasonsInspectStringLine.NullOrEmpty())
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append(inBedForMedicalReasonsInspectStringLine);
                }
            }
            return stringBuilder.ToString();
        }

        public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			if (IsPlayerControlled && vPather.curPath != null)
			{
				vPather.curPath.DrawPath(this);
			}
		}

        public override void PostRemove()
        {
            base.PostRemove();
            vPather.StopDead();
        }

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            vTweener.ResetTweenedPosToRoot();
        }

        public override void Tick()
        {
            base.Tick();
            vPather.PatherTick();
            vTweener.TweenerTick();
        }

        public VehicleCaravan_PathFollower vPather;
        public VehicleCaravan_Tweener vTweener;

        private static readonly Texture2D SplitCommand = ContentFinder<Texture2D>.Get("UI/Commands/SplitCaravan", true);
    }
}