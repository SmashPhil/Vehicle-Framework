using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public class Dialog_DefDropdown : Dialog_ItemDropdown<Def>
	{
		public Dialog_DefDropdown(Rect rect, Type defType, Action<Def> onDefPicked, Func<Def, bool> isSelected)
			: base(rect, DefsOfType(defType), onDefPicked, DefName, isSelected: isSelected)
		{
		}

		private static string DefName(Def def)
		{
			return def.defName;
		}

		private static List<Def> DefsOfType(Type defType)
		{
			List<Def> defs = new List<Def>();
			defs.AddRange(GenDefDatabase.GetAllDefsInDatabaseForDef(defType));
			return defs;
		}
	}
}
