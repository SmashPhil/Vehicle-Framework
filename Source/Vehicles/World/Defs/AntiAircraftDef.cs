using System;
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
		public AirDefenseProperties properties = new AirDefenseProperties();

		public int ticksBetweenShots = 20;

		public Type antiAircraftWorker;
		public TechLevel minTechLevel = TechLevel.Industrial;
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
				yield return "<field<accuracy</field> must be a float value between 0 and 1.".ConvertRichText();
			}
			if (explosionGraphic != null && explosionGraphic.graphicClass == typeof(Graphic_Animate) && framesForExplosion < 0)
			{
				yield return "using <type>Graphic_Animate</type> class requires <field>framesPerExplosion</field> to be populated".ConvertRichText();
			}
			if (antiAircraftWorker is null)
			{
				yield return "<field>antiAircraftWorker</field> cannot be null.".ConvertRichText();
			}
		}

		public class AirDefenseProperties
		{
			public float distance = 4;
			public IntRange altitude = new IntRange(1000, 10000);
			public int arc = 30;
			public IntRange buildings = new IntRange(1, 4);
		}
	}
}
