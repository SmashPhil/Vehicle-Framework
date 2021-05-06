using System.Collections.Generic;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class AntiAircraftDef : WorldObjectDef
	{
		public float damage;
		public float accuracy;
		public int maxAltitude = 10000;

		public GraphicData explosionGraphic;
		public int framesForExplosion = -1;

		public float drawSizeMultiplier = 1;

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string s in base.ConfigErrors())
			{
				yield return s;
			}
			
			if (accuracy < 0 || accuracy > 1)
			{
				yield return "accuracy must be a float value between 0 and 1.";
			}
			if (explosionGraphic != null && explosionGraphic.graphicClass == typeof(Graphic_Animate) && framesForExplosion < 0)
			{
				yield return "using <type>Graphic_Animate</type> class requires <field>framesPerExplosion</field> to be populated".ConvertRichText();
			}
		}
	}
}
