using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class PatternData : IExposable
	{
		public Color color = Color.white;
		public Color colorTwo = Color.white;
		public Color colorThree = Color.white;

		public float tiles = 1;
		public Vector2 displacement = Vector2.zero;
		
		public PatternDef pattern;

		private string patternDef = "Default";

		public PatternData()
		{
		}

		public PatternData(VehiclePawn vehicle) : this(vehicle.DrawColor, vehicle.DrawColorTwo, vehicle.DrawColorThree, vehicle.Pattern, vehicle.Displacement, vehicle.Tiles)
		{
		}

		public PatternData(GraphicDataRGB graphicData) : this(graphicData.color, graphicData.colorTwo, graphicData.colorThree, graphicData.pattern, graphicData.displacement, graphicData.tiles)
		{
		}

		public PatternData(Color color, Color colorTwo, Color colorThree, PatternDef pattern, Vector2 displacement, float tiles)
		{
			this.color = color;
			this.colorTwo = colorTwo;
			this.colorThree = colorThree;
			this.pattern = pattern;
			this.displacement = displacement;
			this.tiles = tiles;
		}

		public static implicit operator GraphicDataRGB(PatternData patternData)
		{
			return new GraphicDataRGB()
			{
				color = patternData.color,
				colorTwo = patternData.colorTwo,
				colorThree = patternData.colorThree,
				tiles = patternData.tiles,
				displacement = patternData.displacement,
				pattern = patternData.pattern
			};
		}

		public static implicit operator PatternData(GraphicDataRGB graphicDataRGB)
		{
			return new PatternData()
			{
				color = graphicDataRGB.color,
				colorTwo = graphicDataRGB.colorTwo,
				colorThree = graphicDataRGB.colorThree,
				tiles = graphicDataRGB.tiles,
				displacement = graphicDataRGB.displacement,
				pattern = graphicDataRGB.pattern
			};
		}

		public virtual void ExposeDataPostDefDatabase()
		{
			if (!patternDef.NullOrEmpty())
			{
				pattern = DefDatabase<PatternDef>.GetNamed(patternDef);
				pattern ??= PatternDefOf.Default;
			}
		}

		public override string ToString()
		{
			return $"Pattern Data: Color={color} ColorTwo={colorTwo} ColorThree={colorThree} Pattern={patternDef} Displacement={displacement} Tiles={tiles}";
		}

		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				patternDef = pattern?.defName ?? "Default";
			}
			Scribe_Values.Look(ref tiles, "tiles", 1);
			Scribe_Values.Look(ref displacement, "displacement", Vector2.zero);
			Scribe_Values.Look(ref color, "color", Color.white);
			Scribe_Values.Look(ref colorTwo, "colorTwo", Color.white);
			Scribe_Values.Look(ref colorThree, "colorThree", Color.white);
			Scribe_Values.Look(ref patternDef, "patternDef");
		}
	}
}
