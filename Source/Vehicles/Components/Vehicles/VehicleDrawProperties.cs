using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Draw Properties for multiple UI dialogs and ModSettings
	/// </summary>
	public class VehicleDrawProperties
	{
		public Rot8 displayRotation = Rot8.East;

		public Vector2 selectionBracketsOffset;

		//Same concept as display coord and size. Fit to settings window
		public Vector2 displayOffset = Vector2.zero;
		public float displaySizeMultiplier = 1;

		public string loadCargoTexPath = string.Empty;
		public string cancelCargoTexPath = string.Empty;

		public List<GraphicDataOverlay> graphicOverlays = new List<GraphicDataOverlay>();

		public List<GraphicOverlay> OverlayGraphics { get; set; } = new List<GraphicOverlay>();

		public void PostDefDatabase()
		{
			foreach (GraphicDataOverlay graphicOverlay in graphicOverlays)
			{
				Graphic graphic = graphicOverlay.graphicData.Graphic;
				OverlayGraphics.Add(new GraphicOverlay(graphic, graphicOverlay.rotation));
			}
		}
	}
}
