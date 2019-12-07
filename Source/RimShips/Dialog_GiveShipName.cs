using RimWorld;
using UnityEngine;
using Verse;

namespace RimShips.Build
{
    public class Dialog_GiveShipName : Window
    {
        public Dialog_GiveShipName(Pawn pawn)
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.curName = pawn.Label;
            ship = pawn;
        }

        protected virtual int MaxNameLength
        {
            get
            {
                return 28;
            }
        }

        private Name CurShipName
        {
            get
            {
                return new NameSingle(this.curName, false);
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(280f, 175f);
            }
        }

        protected virtual AcceptanceReport NameIsValid(string name)
        {
            if(name.Length == 0)
            {
                return false;
            }
            return true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            bool flag = false;
            if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                flag = true;
                Event.current.Use();
            }
            //GUI.SetNextControlName("RenameField");
            string text = this.CurShipName.ToString().Replace(" '' ", " ");
            Widgets.Label(new Rect(15f, 15f, 500f, 50f), text);

            Text.Font = GameFont.Small;
            string text2 = Widgets.TextField(new Rect(15f, 50f, inRect.width - 15f - 15f, 35f), this.curName);

            if (text2.Length < this.MaxNameLength)
            {
                this.curName = text2;
            }
            if(Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 15f, inRect.width - 15f - 15f, 35f), "OK", true, false, true) || flag)
            {
                AcceptanceReport acceptanceReport = this.NameIsValid(this.curName);
                if(!acceptanceReport.Accepted)
                {
                    if(acceptanceReport.Reason.NullOrEmpty())
                    {
                        Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                    else
                    {
                        Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
                    }
                }
                else
                {
                    if(string.IsNullOrEmpty(this.curName))
                    {
                        this.curName = this.ship.Name.ToStringFull;
                    }
                    this.ship.Name = this.CurShipName;
                    Find.WindowStack.TryRemove(this, true);
                    string msg = "ShipGainsName".Translate(this.curName, this.ship.Named("SHIP")).AdjustedFor(this.ship, "SHIP");
                    Messages.Message(msg, this.ship, MessageTypeDefOf.PositiveEvent, false);
                }
            }
        }

        private string curName;

        private Pawn ship;
    }
}
