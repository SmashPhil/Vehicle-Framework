using RimWorld;
using SmashTools;
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
			forcePause = false;
			doCloseX = true;
			closeOnClickedOutside = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
			closeOnAccept = true;
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
			if (name.Length == 0)
			{
				return false;
			}
			return true;
		}

		public override void OnAcceptKeyPressed()
		{
			AcceptName();
		}

		public override void DoWindowContents(Rect inRect)
		{
			using TextBlock fontSize = new(GameFont.Medium);
			
			string curLabel = CurVehicleName.ToString().Replace(" '' ", " ");
			Widgets.Label(new Rect(15f, 15f, 500f, 50f), curLabel);

			Text.Font = GameFont.Small;
			string label = Widgets.TextField(new Rect(15f, 50f, inRect.width - 15f - 15f, 35f), curName);

			if (label.Length < MaxNameLength)
			{
				curName = label;
			}
			Rect buttonRect = new Rect(MarginSize, inRect.height - 35f - MarginSize, (inRect.width / 2) - MarginSize, 35f);
			if (Widgets.ButtonText(buttonRect, "VF_OkName".Translate()))
			{
				AcceptName();
			}
			buttonRect.x += buttonRect.width;
			if (Widgets.ButtonText(buttonRect, "VF_RemoveName".Translate()))
			{
				vehicle.Name = null;
				Close();
			}
		}

		private void AcceptName()
		{
			AcceptanceReport acceptanceReport = NameIsValid(curName);
			if (!acceptanceReport.Accepted)
			{
				if (acceptanceReport.Reason.NullOrEmpty())
				{
					Messages.Message("VF_InvalidName".Translate(), MessageTypeDefOf.RejectInput, false);
				}
				else
				{
					Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
				}
				return;
			}

			if (string.IsNullOrEmpty(curName))
			{
				curName = vehicle.Name.ToStringFull;
			}
			vehicle.Name = CurVehicleName;
			Close();
		}
	}
}
