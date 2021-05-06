using System;
using Verse;
using UnityEngine;

namespace Vehicles
{
	#if BETA
	public class OpenBeta_GameComponent : GameComponent
	{
		public readonly Action cachedButtonDraw;

		private static readonly int BetaShortHash = "Vehicles - Beta#2".GetHashCode();

		public OpenBeta_GameComponent(Game game)
		{
			cachedButtonDraw = new Action(DrawButton);
		}

		public override void GameComponentOnGUI()
		{
			base.GameComponentOnGUI();
			Vector2 pos = new Vector2(Verse.UI.screenWidth - 100, 20);
			Find.WindowStack.ImmediateWindow(BetaShortHash, new Rect(pos.x, pos.y, 80, 80), WindowLayer.GameUI, cachedButtonDraw, false, false, 0);
		}

		public void DrawButton()
		{
			var color = GUI.color;
			Rect buttonRect = new Rect(0, 0, 80, 80);
			if (Mouse.IsOver(buttonRect))
			{
				GUI.color = GenUI.MouseoverColor;
			}
			GUI.DrawTexture(buttonRect, VehicleTex.BetaButtonIcon);
			if (Widgets.ButtonInvisible(buttonRect))
			{
				VehicleHarmony.OpenBetaDialog();
			}
			GUI.color = color;
		}
	}
	#endif
}
