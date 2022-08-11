using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Vehicles
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class ActionOnSettingsInputAttribute : Attribute
	{
		public MethodInfo Method { get; private set; }

		public ActionOnSettingsInputAttribute(Type type, string methodName)
		{
			Method = AccessTools.Method(type, methodName);
		}

		public void Invoke()
		{
			Method.Invoke(null, new object[] { });
		}

		public static void InvokeIfApplicable(FieldInfo field)
		{
			if (field.TryGetAttribute<ActionOnSettingsInputAttribute>() is ActionOnSettingsInputAttribute actionOnSettings)
			{
				actionOnSettings.Invoke();
			}
		}
	}
}
