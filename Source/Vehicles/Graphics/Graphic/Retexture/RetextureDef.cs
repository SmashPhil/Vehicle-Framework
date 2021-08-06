using System.Collections.Generic;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class RetextureDef : Def
	{
		public GraphicDataRGB graphicData;

#pragma warning disable IDE0044 // Add readonly modifier
		private VehicleDef vehicle = null;
#pragma warning restore IDE0044 // Add readonly modifier

		public List<FactionDef> factions = new List<FactionDef>();

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string error in base.ConfigErrors())
			{
				yield return error;
			}
			if (vehicle is null)
			{
				yield return "<field>vehicle</field> must be specified for a valid retexture.".ConvertRichText();
			}
		}

		public override void ResolveReferences()
		{
			factions ??= new List<FactionDef>();
		}
	}
}
