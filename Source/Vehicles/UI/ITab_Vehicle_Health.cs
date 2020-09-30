using RimWorld;
using UnityEngine;

namespace Vehicles.UI
{
    public class ITab_Vehicle_Health : ITab
    {
        public ITab_Vehicle_Health()
        {
            this.size = new Vector2(460f, 450f);
            this.labelKey = "TabBoatHealth";
        }

        public VehiclePawn SelVehicle => SelPawn as VehiclePawn;
        public override bool IsVisible => !SelVehicle.GetCachedComp<CompVehicle>()?.beached ?? false;

        protected override void FillTab()
        {
            /*GUI.color = Color.white;
            Rect rect = new Rect(0f, 20f, this.size.x*0.375f, this.size.y - 20f).Rounded();
            Rect rect2 = new Rect(rect.xMax, 20f, this.size.x - rect.width, this.size.y - 20f);
            Widgets.DrawMenuSection(rect);
            List<TabRecord> list = new List<TabRecord>();
            list.Add(new TabRecord("HealthOverview".Translate(), delegate ()
            {
                capacityTab = true;
            }, !capacityTab));
            TabDrawer.DrawTabs(rect, list, 200f);
            rect = rect.ContractedBy(9f)*/

            HealthCardUtility.DrawPawnHealthCard(new Rect(0f, TopPadding, size.x, size.y - IconSize), SelVehicle, false, false, SelThing);
        }

        private static bool capacityTab = true;

        private const float TopPadding = 20f;

        private const float IconSize = 20f;
    }
}
