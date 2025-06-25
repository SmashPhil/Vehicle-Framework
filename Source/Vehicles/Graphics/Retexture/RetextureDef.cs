using System.Collections.Generic;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class RetextureDef : Def
	{
		public GraphicDataRGB graphicData;

		//TODO - Add faction specific retextures available for NPC generation
		//public List<FactionDef> factions = new List<FactionDef>();

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string error in base.ConfigErrors())
			{
				yield return error;
			}
		}

		public override void ResolveReferences()
		{
			//factions ??= new List<FactionDef>();
		}
	}
}
