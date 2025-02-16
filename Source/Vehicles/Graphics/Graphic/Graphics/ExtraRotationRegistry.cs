using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Case-insensitive wrapper class for safe dictionary lookups and registration for rotations specific to <seealso cref="Graphic_Rotator"/> graphics
	/// </summary>
	public class ExtraRotationRegistry
	{
		private readonly Dictionary<int, float> innerLookup = new Dictionary<int, float>();
		private readonly GraphicOverlayRenderer vehicleGraphicOverlay;

		public ExtraRotationRegistry(GraphicOverlayRenderer vehicleGraphicOverlay)
		{
			this.vehicleGraphicOverlay = vehicleGraphicOverlay;
		}

		public float this[int key]
		{
			get
			{
				return innerLookup.TryGetValue(key, 0);
			}
			set
			{
				innerLookup[key] = value;
			}
		}

		public void UpdateRegistry(float addRotation)
		{
			foreach (GraphicOverlay graphicOverlay in vehicleGraphicOverlay.AllOverlaysListForReading)
			{
				if (graphicOverlay.Graphic is Graphic_Rotator graphicRotator)
				{
					this[graphicRotator.RegistryKey] += graphicRotator.ModifyIncomingRotation(addRotation);
				}
			}
		}

		public void Reset()
		{
			if (!vehicleGraphicOverlay.AllOverlaysListForReading.NullOrEmpty())
			{
				foreach (GraphicOverlay graphicOverlay in vehicleGraphicOverlay.AllOverlaysListForReading)
				{
					if (graphicOverlay.Graphic is Graphic_Rotator graphicRotator)
					{
						this[graphicRotator.RegistryKey] = graphicOverlay.data.rotation;
					}
				}
			}
		}
	}
}
