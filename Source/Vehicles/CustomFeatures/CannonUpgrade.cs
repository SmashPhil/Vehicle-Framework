using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    public class CannonUpgrade : UpgradeNode
    {
        public Dictionary<CannonHandler, VehicleRole> cannonUpgrades = new Dictionary<CannonHandler, VehicleRole>();

        public Dictionary<CannonHandler, VehicleHandler> cannonsUnlocked = new Dictionary<CannonHandler, VehicleHandler>();

        public CannonUpgrade()
        {
        }

        public CannonUpgrade(CannonUpgrade reference, VehiclePawn parent) : base(reference, parent)
        {
            cannonUpgrades = reference.cannonUpgrades;
            foreach(KeyValuePair<CannonHandler, VehicleRole> cu in cannonUpgrades)
            {
                cannonsUnlocked.Add(new CannonHandler(parent, cu.Key), new VehicleHandler(parent, cu.Value));
            }
        }

        public override string UpgradeIdName => "CannonUpgrade";

        public override void OnInit()
        {
            base.OnInit();
            if(cannonsUnlocked is null)
                cannonsUnlocked = new Dictionary<CannonHandler, VehicleHandler>();

            foreach(KeyValuePair<CannonHandler,VehicleHandler> cannon in cannonsUnlocked)
            {
                if(cannon.Key.uniqueID < 0)
                {
                    cannon.Key.uniqueID = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextCannonId();
                }
                if(cannon.Value.uniqueID < 0)
                {
                    cannon.Value.uniqueID = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextHandlerId();
                }
            }
        }

        public override void Upgrade(VehiclePawn vehicle)
        {
            vehicle.GetCachedComp<CompCannons>().AddCannons(cannonsUnlocked.Keys.ToList());
            vehicle.GetCachedComp<CompVehicle>().AddHandlers(cannonsUnlocked.Values.ToList());
        }

        public override void Refund(VehiclePawn vehicle)
        {
            vehicle.GetCachedComp<CompCannons>().RemoveCannons(cannonsUnlocked.Keys.ToList());
            vehicle.GetCachedComp<CompVehicle>().RemoveHandlers(cannonsUnlocked.Values.ToList());
        }

        public override void DrawExtraOnGUI(Rect rect)
        {
            Rect displayRect = rect;
            rect.width = 1f;
            rect.height = 1f;
            parent.DrawCannonTextures(displayRect, cannonsUnlocked.Keys.OrderBy(c => c.drawLayer), parent.selectedMask, true, parent.DrawColor, parent.DrawColorTwo);
            parent.DrawCannonTextures(displayRect, parent.GetCachedComp<CompUpgradeTree>().upgradeList.Where(x => x is CannonUpgrade && x.upgradeActive && !cannonsUnlocked.Keys.ToList().NullOrEmpty()).Cast<CannonUpgrade>().SelectMany(y => y.cannonsUnlocked.Keys).OrderBy(o => o.drawLayer), parent.selectedMask, true, parent.DrawColor, parent.DrawColorTwo);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref cannonsUnlocked, "cannonsUnlocked", LookMode.Deep, LookMode.Deep);
        }
    }
}
