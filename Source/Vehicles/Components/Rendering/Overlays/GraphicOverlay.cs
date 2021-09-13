using UnityEngine;
using Verse;

namespace Vehicles
{
	public class GraphicOverlay
	{
		public Graphic graphic;
		public float rotation = 0;

		public GraphicOverlay(Graphic graphic, float rotation)
		{
			this.graphic = graphic;
			this.rotation = rotation;
		}
	}
}
