using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
using System.Xml;

namespace Vehicles
{
    public class StatUpgrade : UpgradeNode 
    {
        public Dictionary<StatUpgradeCategory, float> values = new Dictionary<StatUpgradeCategory, float>();

        public StatUpgrade()
        {
        }

        public StatUpgrade(StatUpgrade reference, VehiclePawn parent) : base(reference, parent)
        {
            values = reference.values;
        }

        public override string UpgradeIdName => "StatUpgrade";

        public override void Upgrade(VehiclePawn vehicle)
        {
            try
            {
                foreach(KeyValuePair<StatUpgradeCategory, float> stat in values)
                {
                    switch(stat.Key)
                    {
                        case StatUpgradeCategory.Armor:
                            vehicle.GetCachedComp<CompVehicle>().ArmorPoints += stat.Value;
                            break;
                        case StatUpgradeCategory.Speed:
                            vehicle.GetCachedComp<CompVehicle>().MoveSpeedModifier += stat.Value;
                            break;
                        case StatUpgradeCategory.CargoCapacity:
                            vehicle.GetCachedComp<CompVehicle>().CargoCapacity += stat.Value;
                            break;
                        case StatUpgradeCategory.FuelConsumptionRate:
                            vehicle.GetCachedComp<CompFueledTravel>().FuelEfficiency += stat.Value;
                            break;
                        case StatUpgradeCategory.FuelCapacity:
                            vehicle.GetCachedComp<CompFueledTravel>().FuelCapacity += stat.Value;
                            break;
                        default:
                            throw new NotImplementedException("StatUpgrade Not Valid");
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error($"Unable to add stat values to {vehicle.LabelShort}. Report on Boats workshop page. \nException: {ex.Message} \nStackTrace: {ex.StackTrace}");
                return;
            }

            vehicle.GetCachedComp<CompVehicle>().Props.buildDef.soundBuilt?.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map, false));
        }

        public override void Refund(VehiclePawn vehicle)
        {
            foreach(KeyValuePair<StatUpgradeCategory, float> stat in values)
            {
                switch(stat.Key)
                {
                    case StatUpgradeCategory.Armor:
                        vehicle.GetCachedComp<CompVehicle>().ArmorPoints -= stat.Value;
                        break;
                    case StatUpgradeCategory.Speed:
                        vehicle.GetCachedComp<CompVehicle>().MoveSpeedModifier -= stat.Value;
                        break;
                    case StatUpgradeCategory.CargoCapacity:
                        vehicle.GetCachedComp<CompVehicle>().CargoCapacity -= stat.Value;
                        break;
                    case StatUpgradeCategory.FuelConsumptionRate:
                        vehicle.GetCachedComp<CompFueledTravel>().FuelEfficiency -= stat.Value;
                        break;
                    case StatUpgradeCategory.FuelCapacity:
                        vehicle.GetCachedComp<CompFueledTravel>().FuelCapacity -= stat.Value;
                        break;
                    default:
                        throw new NotImplementedException("StatUpgrade Not Valid");
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref values, "values", LookMode.Value, LookMode.Value);
        }
    }
}
