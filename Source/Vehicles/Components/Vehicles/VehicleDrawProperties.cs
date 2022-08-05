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
		public Vector2 selectionBracketsOffset = Vector2.zero;

		public Rot8 displayRotation = Rot8.East;
		//Same concept as display coord and size. Fit to settings window
		public Vector2 displayOffset = Vector2.zero;
		public Vector2? displayOffsetNorth;
		public Vector2? displayOffsetEast;
		public Vector2? displayOffsetSouth;
		public Vector2? displayOffsetWest;

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

		public Vector3 DisplayOffsetForRot(Rot4 rot)
		{
			switch (rot.AsInt)
			{
				case 0:
					{
						Vector3? vector = displayOffsetNorth;
						if (vector == null)
						{
							return displayOffset;
						}
						return vector.GetValueOrDefault();
					}
				case 1:
					{
						Vector3? vector = displayOffsetEast;
						if (vector == null)
						{
							return displayOffset;
						}
						return vector.GetValueOrDefault();
					}
				case 2:
					{
						Vector3? vector = displayOffsetSouth;
						if (vector == null)
						{
							return displayOffset;
						}
						return vector.GetValueOrDefault();
					}
				case 3:
					{
						Vector3? vector = displayOffsetWest;
						if (vector == null)
						{
							return displayOffset;
						}
						return vector.GetValueOrDefault();
					}
				default:
					return displayOffset;
			}
		}
	}
}
