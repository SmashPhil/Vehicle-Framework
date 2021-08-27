using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace Vehicles
{
	/// <summary>
	/// Draw Properties for multiple UI dialogs and ModSettings
	/// </summary>
	public class VehicleDrawProperties
	{
		public Vector2 selectionBracketsOffset;

		//Dialog ColorPicker coord
		public Vector2 upgradeUICoord;
		public Vector2 upgradeUISize;

		public Vector2 colorPickerUICoord;
		public Vector2 colorPickerUISize;

		//Same concept as display coord and size. Fit to settings window
		public Vector2 settingsUICoord;
		public Vector2 settingsUISize;

		public string loadCargoTexPath = string.Empty;
		public string cancelCargoTexPath = string.Empty;

		public List<GraphicData> graphicOverlays = new List<GraphicData>();

		public List<Graphic> OverlayGraphics { get; set; } = new List<Graphic>();

		public void PostDefDatabase()
		{
			foreach (GraphicData graphicData in graphicOverlays)
			{
				Graphic graphic = graphicData.Graphic;
				OverlayGraphics.Add(graphic);
			}
		}

		public class GraphicOverlay
		{
			public GraphicDataLayered graphicData;
		}
	}
}
