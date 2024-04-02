using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Dialog_NodeSettings : SingleWindow
	{
		private const float ModSettingsDefaultWidth = 900;

		private const float ModSettingsDefaultHeight = 700;

		private Vector2 windowSize = new Vector2(10, 10);

		private Vector2 drawLoc = new Vector2(0, 0);

		public Dialog_NodeSettings(VehicleDef def, UpgradeNode node, Vector2 origin)
		{
			VehicleDef = def;
			UpgradeNode = node;

			closeOnClickedOutside = true;
			closeOnAnyClickOutside = true;
			closeOnCancel = true;
			doCloseX = true;
			
			float width = 400;
			int rows = 3;// Mathf.Max(1, Mathf.FloorToInt(UpgradeNode.ListerCount / 2f));
			float height = rows * 100 + 50;
			windowSize = new Vector2(width, height);
			drawLoc = origin;

			Lister = new Listing_Settings(SettingsPage.Upgrades);
		}

		public VehicleDef VehicleDef { get; set; }

		public UpgradeNode UpgradeNode { get; set; }

		public Listing_Settings Lister { get; set; }

		public override Vector2 InitialSize => windowSize;

		public override void PostClose()
		{
			base.PostClose();
			VehicleMod.selectedNode = null;
		}

		protected override void SetInitialSizeAndPosition()
		{
			float additionalX = (Verse.UI.screenWidth - ModSettingsDefaultWidth) / 2;
			float additionalY = (Verse.UI.screenHeight - ModSettingsDefaultHeight) / 2;
			windowRect = new Rect(drawLoc.x + additionalX, drawLoc.y + additionalY, InitialSize.x, InitialSize.y);
			windowRect = windowRect.Rounded();
		}

		public override void DoWindowContents(Rect inRect)
		{
			Lister.Begin(inRect, 2);
			UpgradeNode.SettingsWindow(VehicleDef, Lister);
			Lister.End();
		}
	}
}
