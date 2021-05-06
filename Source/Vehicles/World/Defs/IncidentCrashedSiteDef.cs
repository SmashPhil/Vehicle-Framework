using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public class IncidentCrashedSiteDef : IncidentDef
	{
		[MustTranslate]
		public List<string> letterTexts;

		public List<ThingCategoryDef> floatingItems;

		public override void PostLoad()
		{
			base.PostLoad();
			if (letterTexts.NullOrEmpty())
			{
				letterTexts = new List<string>() { letterText };
			}
		}
	}
}
