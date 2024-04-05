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
		private readonly Dictionary<string, float> innerLookup = new Dictionary<string, float>();
		private readonly VehicleGraphicOverlay vehicleGraphicOverlay;

		public ExtraRotationRegistry(VehicleGraphicOverlay vehicleGraphicOverlay)
		{
			this.vehicleGraphicOverlay = vehicleGraphicOverlay;
		}

		public float this[string key]
		{
			get
			{
				return innerLookup.TryGetValue(key.ToUpperInvariant(), 0);
			}
			set
			{
				innerLookup[key.ToUpperInvariant()] = value;
			}
		}

		public void UpdateRegistry(float addRotation)
		{
			foreach (GraphicOverlay graphicOverlay in vehicleGraphicOverlay.Overlays)
			{
				if (graphicOverlay.data.graphicData.Graphic is Graphic_Rotator graphicRotator)
				{
					this[graphicRotator.RegistryKey] += graphicRotator.ModifyIncomingRotation(addRotation);
				}
			}
		}

		public void Reset()
		{
			if (!vehicleGraphicOverlay.Overlays.NullOrEmpty())
			{
				foreach (GraphicOverlay graphicOverlay in vehicleGraphicOverlay.Overlays)
				{
					if (graphicOverlay.data.graphicData.Graphic is Graphic_Rotator graphicRotator)
					{
						this[graphicRotator.RegistryKey] = graphicOverlay.data.rotation;
					}
				}
			}
		}
	}
}
