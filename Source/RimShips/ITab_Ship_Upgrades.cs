using System;
using UnityEngine;
using RimWorld;
using Verse;

namespace RimShips.UI
{
    public class ITab_Ship_Upgrades : ITab
    {
        public ITab_Ship_Upgrades()
        {
            this.size = new UnityEngine.Vector2(460f, 450f);
            this.labelKey = "TabUpgrades";
        }

        public override bool IsVisible => !this.SelPawnForUpgrades.GetComp<CompShips>().beached && this.SelPawnForUpgrades.Faction == Faction.OfPlayer;

        private Pawn SelPawnForUpgrades
        {
            get
            {
                if (!(base.SelPawn is null) && !(base.SelPawn.TryGetComp<CompShips>() is null))
                {
                    return base.SelPawn;
                }
                throw new InvalidOperationException("Upgrade tab on non-pawn ship " + base.SelThing);
            }
        }

        protected override void FillTab()
        {
            
        }

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;

        private const float TopPadding = 20f;

        public static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        public static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private const float ThingIconSize = 28f;

        private const float ThingRowheight = 28f;

        private const float ThingLeftX = 36f;

        private const float StandardLineHeight = 22f;
    }
}
