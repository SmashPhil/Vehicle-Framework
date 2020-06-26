using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles
{
    public class VehicleCaravan : Caravan
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if(HelperMethods.HasBoat(this) && (Find.World.CoastDirectionAt(Tile).IsValid || HelperMethods.RiverIsValid(Tile, PawnsListForReading.Where(x => HelperMethods.IsBoat(x)).ToList())))
            {
                if(!pather.Moving && !PawnsListForReading.Any(x => !HelperMethods.IsBoat(x)))
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
                else if (!pather.Moving && PawnsListForReading.Any(x => !HelperMethods.IsBoat(x)))
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

            if(pather.Moving)
            {
                if (!(pather.ArrivalAction is null))
                    stringBuilder.Append(pather.ArrivalAction.ReportString);
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
            if (pather.Moving)
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
            if (!pather.MovingNow)
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
    }
}
