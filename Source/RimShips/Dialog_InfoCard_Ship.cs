using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using RimShips.Defs;
using Harmony;

namespace RimShips.UI
{
    public class Dialog_InfoCard_Ship : Window
    {
        public Dialog_InfoCard_Ship(Thing thing)
        {
            this.thing = thing;
            this.tab = InfoCardTab.Stats;
            this.Setup();
        }

        private Pawn Pawn => this.thing as Pawn;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(950f, 760f);
            }
        }
        protected override float Margin => 0f;

        private void Setup()
        {
            this.forcePause = true;
            this.doCloseButton = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.soundAppear = SoundDefOf.InfoCard_Open;
            this.soundClose = SoundDefOf.InfoCard_Close;

            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InfoCard, KnowledgeAmount.Total);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect);
            rect = rect.ContractedBy(18f);
            rect.height = 34f;
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, this.thing.LabelCapNoCount);
            Rect rect2 = new Rect(inRect);
            rect2.yMin = rect.yMax;
            rect2.yMax -= 38f;
            Rect rect3 = rect2;
            rect3.yMin += 45f;
            List<TabRecord> list = new List<TabRecord>();
            TabRecord item = new TabRecord("TabStats".Translate(), delegate ()
            {
                this.tab = InfoCardTab.Stats;
            }, this.tab == InfoCardTab.Stats);
            list.Add(item);

            TabRecord item2 = new TabRecord("TabHealth".Translate(), delegate ()
            {
                this.tab = InfoCardTab.Health;
            }, this.tab == InfoCardTab.Health);
            list.Add(item2);

            TabRecord item3 = new TabRecord("TabRecords".Translate(), delegate ()
            {
                this.tab = InfoCardTab.Records;
            }, this.tab == InfoCardTab.Records);
            list.Add(item3);

            TabDrawer.DrawTabs(rect3, list, 200f);
            this.FillCard(rect3.ContractedBy(18f));
        }

        private void FillCard(Rect cardRect)
        {
            switch(this.tab)
            {
                case InfoCardTab.Stats:
                    this.DrawStatsWorker(cardRect);
                    break;
                case InfoCardTab.Health:
                    cardRect.yMin += 8f;
                    HealthCardUtility.DrawPawnHealthCard(cardRect, (Pawn)this.thing, false, false, null);
                    break;
                case InfoCardTab.Records:
                    RecordsCardUtility.DrawRecordsCard(cardRect, (Pawn)this.thing);
                    break;
            }
        }

        private void DrawStatsWorker(Rect rect)
        {
            Rect rect2 = new Rect(rect);
            rect2.width *= 0.5f;
            Rect rect3 = new Rect(rect);
            rect3.x = rect2.xMax;
            rect3.width = rect.xMax - rect3.x;
            Text.Font = GameFont.Small;
            Rect viewRect = new Rect(0f, 0f, rect2.width - 16f, listHeight);
            Widgets.BeginScrollView(rect2, ref scrollPosition, viewRect, true);
            float num = 0f;
            string b = null;
            cachedDrawEntries = this.StatsToDraw(this.thing).ToList();
            this.FinalizeCachedDrawEntries();
            mousedOverEntry = null;
            foreach(StatDrawEntry stat in cachedDrawEntries)
            {
                if(stat.category.LabelCap != b)
                {
                    Widgets.ListSeparator(ref num, viewRect.width, stat.category.LabelCap);
                    b = stat.category.LabelCap;
                }
                num += stat.Draw(8f, num, viewRect.width - 8f, selectedEntry == stat, delegate
                {
                    selectedEntry = stat;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }, delegate
                {
                    mousedOverEntry = stat;
                }, scrollPosition, rect2);
            }
            listHeight = num + 100f;
            Widgets.EndScrollView();
            Rect rect4 = rect3.ContractedBy(10f);
            GUI.BeginGroup(rect4);
            StatDrawEntry statDrawEntry;
            if((statDrawEntry = selectedEntry) is null)
            {
                statDrawEntry = mousedOverEntry ?? cachedDrawEntries.FirstOrDefault<StatDrawEntry>();
            }
            StatDrawEntry statDrawEntry2 = statDrawEntry;
            if(statDrawEntry2 != null)
            {
                StatRequest optionalReq;
                if(statDrawEntry2.hasOptionalReq)
                {
                    optionalReq = statDrawEntry2.optionalReq;
                }
                else if(this.thing != null)
                {
                    optionalReq = StatRequest.For(this.thing);
                }
                else
                {
                    optionalReq = StatRequest.ForEmpty();
                }
                string explanation = statDrawEntry2.GetExplanationText(optionalReq);
                Rect rect5 = rect4.AtZero();
                Widgets.Label(rect5, explanation);
            }
            GUI.EndGroup();
        }
        
        private IEnumerable<StatDrawEntry> StatsToDraw(Thing thing)
        {
            yield return new StatDrawEntry(StatCategoryDefOf_Ships.BasicsShip, "Description".Translate(), string.Empty, 99999, string.Empty)
            {
                overrideReportText = thing.DescriptionFlavor
            };
            foreach (StatDef stat in from st in DefDatabase<StatDef>.AllDefs
                                     where st.category == StatCategoryDefOf_Ships.BasicsShip
                                     select st)
            {
                yield return new StatDrawEntry(stat.category, stat);
            }

        }
        private void FinalizeCachedDrawEntries()
        {
            cachedDrawEntries = (from sde in this.cachedDrawEntries
                                orderby sde.category.displayOrder, sde.DisplayPriorityWithinCategory descending, sde.LabelCap
                                select sde).ToList<StatDrawEntry>();
        }

        private string GetExplanationTextShip(StatDrawEntry sde, StatRequest optionalReq)
        {
            if(!sde.overrideReportText.NullOrEmpty())
            {
                return sde.overrideReportText;
            }
            if(sde is null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(sde.stat.LabelCap);
            sb.AppendLine();
            sb.AppendLine(sde.stat.description);
            sb.AppendLine();
            if(!optionalReq.Empty)
            {
                sb.AppendLine(sde.stat.Worker.GetExplanationFull(optionalReq, Traverse.Create(sde).Field("numberSense").GetValue<ToStringNumberSense>(), Traverse.Create(sde).Field("value").GetValue<float>()));
            }
            return sb.ToString().TrimEndNewlines();
        }

        private string GetExplanationFullShip(StatWorker sw, StatRequest req, ToStringNumberSense numSense, float value)
        {
            if (sw.IsDisabledFor(req.Thing))
            {
                return "StatsReport_PermanentlyDisabled".Translate();
            }
            return String.Empty;
        }

        private Thing thing;

        private InfoCardTab tab;

        private float listHeight;

        private Vector2 scrollPosition;

        private StatDrawEntry mousedOverEntry;

        private StatDrawEntry selectedEntry;

        private List<StatDrawEntry> cachedDrawEntries;
        private enum InfoCardTab : byte
        {
            Stats,
            Health,
            Records
        }
    }
}
