using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class Dialog_GiveVehicleName : Window
	{
		private const float MarginSize = 15f;

		private string curName;

		private readonly VehiclePawn vehicle;

		public Dialog_GiveVehicleName(VehiclePawn vehicle)
		{
			forcePause = true;
			doCloseX = true;
			closeOnClickedOutside = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
			curName = vehicle.Label;
			this.vehicle = vehicle;
		}

		protected virtual int MaxNameLength
		{
			get
			{
				return 28;
			}
		}

		private Name CurVehicleName
		{
			get
			{
				return new NameSingle(curName, false);
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
			string text = CurVehicleName.ToString().Replace(" '' ", " ");
			Widgets.Label(new Rect(15f, 15f, 500f, 50f), text);

			Text.Font = GameFont.Small;
			string text2 = Widgets.TextField(new Rect(15f, 50f, inRect.width - 15f - 15f, 35f), curName);

			if (text2.Length < MaxNameLength)
			{
				curName = text2;
			}
			Rect buttonRect = new Rect(MarginSize, inRect.height - 35f - MarginSize, (inRect.width / 2) - MarginSize, 35f);
			if (Widgets.ButtonText(buttonRect, "OkName".Translate()) || flag)
			{
				AcceptanceReport acceptanceReport = NameIsValid(curName);
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
					if(string.IsNullOrEmpty(curName))
					{
						curName = vehicle.Name.ToStringFull;
					}
					vehicle.Name = CurVehicleName;
					Find.WindowStack.TryRemove(this, true);
					string msg = "VehicleGainsName".Translate(curName, vehicle.Named("Vehicle")).AdjustedFor(vehicle, "Vehicle");
					Messages.Message(msg, vehicle, MessageTypeDefOf.PositiveEvent, false);
				}
			}
			buttonRect.x += buttonRect.width;
			if (Widgets.ButtonText(buttonRect, "RemoveName".Translate()) || flag)
			{
				vehicle.Name = null;
				Find.WindowStack.TryRemove(this, true);
			}
		}
	}
}
