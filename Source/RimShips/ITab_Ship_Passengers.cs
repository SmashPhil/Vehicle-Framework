using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Vehicles.UI
{
    public class ITab_Ship_Passengers : ITab
    {
        public ITab_Ship_Passengers()
        {
            this.size = new Vector2(520f, 450f);
            this.labelKey = "TabPassengers";
        }

        public override bool IsVisible
        {
            get
            {
                return !(base.SelPawn.Faction is null) && !(base.SelPawn.TryGetComp<CompVehicle>() is null) && !base.SelPawn.GetComp<CompVehicle>().beached;
            }
        }

        private float SpecificNeedsTabWidth => this.specificNeedsTabForPawn.DestroyedOrNull() ? 0f : NeedsCardUtility.GetSize(this.specificNeedsTabForPawn).x;
        private List<Pawn> Passengers
        {
            get
            {
                return base.SelPawn.TryGetComp<CompVehicle>() is null ? new List<Pawn>() : base.SelPawn.GetComp<CompVehicle>().Passengers;
            }
        }

        private List<Pawn> AllAboard
        {
            get
            {
                return base.SelPawn.TryGetComp<CompVehicle>() is null ? new List<Pawn>() : base.SelPawn.GetComp<CompVehicle>().AllPawnsAboard;
            }
        }
        private List<ShipHandler> Handlers
        {
            get
            {
                return base.SelPawn.TryGetComp<CompVehicle>()?.handlers;
            }
        }

        protected override void FillTab()
        {
            this.EnsureSpecificNeedsTabForPawnValid();

            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
            float num = 0f;
            bool flag = false;
            foreach(ShipHandler handler in Handlers)
            {
                Widgets.ListSeparator(ref num, viewRect.width, handler.role.label);
                foreach(Pawn pawn in handler.handlers.InnerListForReading)
                {
                    if(!pawn.IsColonist)
                        continue;
                    ITab_Ship_Passengers.DoRow(ref num, viewRect, rect, scrollPosition, pawn, ref specificNeedsTabForPawn);
                }
            }

            foreach(Pawn pawn in Passengers)
            {
                if(!pawn.IsColonist)
                {
                    if(!flag)
                    {
                        Widgets.ListSeparator(ref num, viewRect.width, "CaravanPrisonersAndAnimals".Translate());
                        flag = true;
                    }
                    ITab_Ship_Passengers.DoRow(ref num, viewRect, rect, scrollPosition, pawn, ref specificNeedsTabForPawn);
                }
            }
            if(Event.current.type is EventType.Layout)
                scrollViewHeight = num + 30f;
            Widgets.EndScrollView();
        }

        private static void DoRow(ref float curY, Rect viewRect, Rect scrollOutRect, Vector2 scrollPosition, Pawn pawn, ref Pawn specificNeedsTabForPawn)
        {
            float num = scrollPosition.y - 50f;
            float num2 = scrollPosition.y + scrollOutRect.height;
            if(curY > num && curY < num2)
            {
                DoRow(new Rect(0f, curY, viewRect.width, 50f), pawn, ref specificNeedsTabForPawn);
            }
            curY += 50f;
        }

        private static void DoRow(Rect rect, Pawn pawn, ref Pawn specificNeedsTabForPawn)
        {
            GUI.BeginGroup(rect);
            Rect rect2 = rect.AtZero();
            Widgets.InfoCardButton(rect2.width - 24f, (rect.height - 24f) / 2f, pawn);
            rect2.width -= 24f;
            if(!pawn.Dead)
            {
                OpenSpecificTabButton(rect2, pawn, ref specificNeedsTabForPawn);
                rect2.width -= 24f;
            }
            Widgets.DrawHighlightIfMouseover(rect2);
            Rect rect3 = new Rect(4f, (rect.height - 27f) / 2f, 27f, 27f);
            Widgets.ThingIcon(rect3, pawn, 1f);
            Rect bgRect = new Rect(rect3.xMax + 4f, 16f, 100f, 18f);
            GenMapUI.DrawPawnLabel(pawn, bgRect, 1f, 100f, null, GameFont.Small, false, false);

            tmpNeeds.Clear();
            List<Need> allNeeds = pawn.needs.AllNeeds;
            foreach (Need n in allNeeds)
            {
                if (n.def.showForCaravanMembers) // Change for all needs?
                    tmpNeeds.Add(n);
            }
            PawnNeedsUIUtility.SortInDisplayOrder(tmpNeeds);

            float xMax = bgRect.xMax;
            foreach(Need need in tmpNeeds)
            {
                int maxThresholdMarkers = 0;
                bool doTooltip = true;
                Rect rect4 = new Rect(xMax, 0f, 100f, 50f);
                Need_Mood mood = need as Need_Mood;
                if(!(mood is null))
                {
                    maxThresholdMarkers = 1;
                    doTooltip = false;
                    //TooltipHandler.TipRegion(rect4, new TipSignal(() => CaravanNeedsTabUtility.CustomMoodNeed)) //Add better way to make stringbuilder
                }
                need.DrawOnGUI(rect4, maxThresholdMarkers, 10f, false, doTooltip);
                xMax = rect4.xMax;
            }
            
            if(pawn.Downed)
            {
                GUI.color = new Color(1f, 0f, 0f, 0.5f);
                Widgets.DrawLineHorizontal(0f, rect.height / 2f, rect.width);
                GUI.color = Color.white;
            }
            GUI.EndGroup();
        }

        private static void OpenSpecificTabButton(Rect rowRect, Pawn p, ref Pawn specificTabForPawn)
        {
            Color baseColor = (p != specificTabForPawn) ? Color.white : Color.green;
            Color mouseoverColor = (p != specificTabForPawn) ? GenUI.MouseoverColor : new Color(0f, 0.5f, 0f);
            Rect rect = new Rect(rowRect.width - 24f, (rowRect.height - 24f) / 2f, 24f, 24f);
            
            if(Widgets.ButtonImage(rect, CaravanThingsTabUtility.SpecificTabButtonTex, baseColor, mouseoverColor))
            {
                if(p == specificTabForPawn)
                {
                    specificTabForPawn = null;
                    SoundDefOf.TabClose.PlayOneShotOnCamera(null);
                }
                else
                {
                    specificTabForPawn = p;
                    SoundDefOf.TabOpen.PlayOneShotOnCamera(null);
                }
            }
            TooltipHandler.TipRegion(rect, "OpenSpecificTabButtonTip".Translate());
            GUI.color = Color.white;
        }

        protected override void UpdateSize()
        {
            this.EnsureSpecificNeedsTabForPawnValid();
            base.UpdateSize();

            this.size = ITab_Ship_Passengers.GetSize(AllAboard, this.PaneTopY, true);
            this.size.y = Mathf.Max(this.size.y, NeedsCardUtility.FullSize.y);
        }

        private static Vector2 GetSize(List<Pawn> pawns, float paneTopY, bool doNeeds = true)
        {
            float num = 100f;
            if (doNeeds)
            {
                num += (float)ITab_Ship_Passengers.MaxNeedsCount(pawns) * 100f;
            }
            num += 24f;
            Vector2 result;
            result.x = 103f + num + 16f;
            result.y = Mathf.Min(550f, paneTopY - 30f);
            return result;
        }

        private static int MaxNeedsCount(List<Pawn> pawns)
        {
            int num = 0;
            foreach(Pawn p in pawns)
            {
                List<Need> pawnNeeds = new List<Need>();
                foreach (Need need in p.needs.AllNeeds)
                {
                    if(need.def.showForCaravanMembers)
                    {
                        pawnNeeds.Add(need);
                    }
                }
                num = Mathf.Max(num, pawnNeeds.Count);
            }
            return num;
        }

        protected override void ExtraOnGUI()
        {
            this.EnsureSpecificNeedsTabForPawnValid();
            base.ExtraOnGUI();
            Pawn localSpecificNeedsTabForPawn = this.specificNeedsTabForPawn;
            if(!(localSpecificNeedsTabForPawn is null))
            {
                Rect tabRect = base.TabRect;
                float specificNeedsTabWidth = this.SpecificNeedsTabWidth;
                Rect rect = new Rect(tabRect.xMax - 1f, tabRect.yMin, specificNeedsTabWidth, tabRect.height);
                Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
                {
                    if (localSpecificNeedsTabForPawn.DestroyedOrNull())
                        return;
                    NeedsCardUtility.DoNeedsMoodAndThoughts(rect.AtZero(), localSpecificNeedsTabForPawn, ref this.thoughtScrollPosition);
                    if (Widgets.CloseButtonFor(rect.AtZero()))
                    {
                        this.specificNeedsTabForPawn = null;
                        SoundDefOf.TabClose.PlayOneShotOnCamera(null);
                    }
                }, true, false, 1f);
            }
        }

        public override void Notify_ClearingAllMapsMemory()
        {
            base.Notify_ClearingAllMapsMemory();
            this.specificNeedsTabForPawn = null;
        }
        private void EnsureSpecificNeedsTabForPawnValid()
        {
            if(!(this.specificNeedsTabForPawn is null) && (this.specificNeedsTabForPawn.Destroyed || !AllAboard.Contains(specificNeedsTabForPawn)))
            {
                this.specificNeedsTabForPawn = null;
            }
        }


        private Vector2 scrollPosition;

        private float scrollViewHeight;

        private Pawn specificNeedsTabForPawn;

        private static List<Need> tmpNeeds = new List<Need>();

        private Vector2 thoughtScrollPosition;
    }
}