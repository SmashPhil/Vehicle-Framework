using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace Vehicles
{
    public class IngredientFilter : IExposable
    {

		public bool IsFixedIngredient
		{
			get
			{
				return filter.AllowedDefCount == 1;
			}
		}

		public ThingDef FixedIngredient
		{
			get
			{
				if (!IsFixedIngredient)
				{
					Log.Error("Called for SingleIngredient on an IngredientCount that is not IsSingleIngredient: " + this, false);
				}
				return filter.AnyAllowedDef;
			}
		}

		public string Summary
		{
			get
			{
				return count + "x " + filter.Summary;
			}
		}

		public bool StuffableDef(ThingDef def)
        {
			if(!stuffableDefs.Contains(def) && !nonStuffableDefs.Contains(def))
            {
				bool stuffable = ((List<StuffCategoryDef>)AccessTools.Field(typeof(ThingFilter), "stuffCategoriesToAllow").GetValue(filter)).AnyNullified(sc => def.stuffCategories.Contains(sc));
				if (stuffable)
					stuffableDefs.Add(def);
				else
					nonStuffableDefs.Add(def);
            }
			return filter.Allows(def) && stuffableDefs.Contains(def);
        }

		public ThingDefCountClass CountClass
        {
            get
            {
				if(IsFixedIngredient)
					return new ThingDefCountClass(FixedIngredient, count);
				return new ThingDefCountClass(filter.AllowedThingDefs.First(), count);
            }
        }

		public void ResolveReferences()
		{
			filter.ResolveReferences();
		}

		public void ExposeData()
        {
			Scribe_Values.Look(ref count, "count");
			Scribe_Deep.Look(ref filter, "filter");
        }

		public override string ToString()
		{
			return "(" + Summary + ")";
		}

		internal HashSet<ThingDef> stuffableDefs = new HashSet<ThingDef>();
		private HashSet<ThingDef> nonStuffableDefs = new HashSet<ThingDef>();

		public ThingFilter filter = new ThingFilter();

		public int count = 1;
	}
}
