using System;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehicleStatCategoryDef : Def
	{
		public string formatString;
		public EfficiencyOperationType operationType;
		public Type workerClass;
		public int priority = 999;

		public VehicleStatWorker Worker { get; private set; }

		public override void ResolveReferences()
		{
			base.ResolveReferences();
			Worker = (VehicleStatWorker)Activator.CreateInstance(workerClass);
			Worker.statDef = this;
		}

		public override IEnumerable<string> ConfigErrors()
		{
			foreach (string error in base.ConfigErrors())
			{
				yield return error;
			}
			if (workerClass is null)
			{
				yield return $"<field>workerClass</field> cannot be null.".ConvertRichText();
			}
		}

		private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) 
		{
			while (toCheck != null && toCheck != typeof(object)) 
			{
				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (generic == cur) 
				{
					return true;
				}
				toCheck = toCheck.BaseType;
			}
			return false;
		}
	}
}
