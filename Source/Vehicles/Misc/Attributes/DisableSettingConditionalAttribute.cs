using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Vehicles
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class DisableSettingConditionalAttribute : Attribute
	{
		private const string Unassigned = "Unassigned";

		/// <summary>
		/// Field or property declaring type
		/// </summary>
		public Type MemberType { get; set; }

		/// <summary>
		/// Setting is disabled if property is equal or not equal to objects
		/// </summary>
		public string Property { get; set; }

		/// <summary>
		/// Setting is disabled if field is equal or not equal to objects
		/// </summary>
		public string Field { get; set; }

		public object DisableIfEqualTo { get; set; } = Unassigned;
		public object DisableIfNotEqualTo { get; set; } = Unassigned;

		/// <summary>
		/// Setting is disabled if this mod is not found in the active mod list
		/// </summary>
		public string MayRequire { get; set; }

		/// <summary>
		/// Setting is disabled if none of these mods are found in the active mod list
		/// </summary>
		public string[] MayRequireAny { get; set; }

		/// <summary>
		/// Setting is disabled if one of these mods is not found in the active mod list
		/// </summary>
		public string[] MayRequireAll { get; set; }

		public bool PropertyDisabled(VehicleDef vehicleDef, out string tooltip)
		{
			tooltip = string.Empty;
			if (Property.NullOrEmpty())
			{
				return false;
			}
			if (MemberType == null)
			{
				Log.Warning($"DisableSettingConditional has Property name, not null MemberType. MemberType property must be included for PropertyInfo to be evaluated.");
				return false;
			}
			PropertyInfo propertyInfo = AccessTools.Property(MemberType, Property);
			if (propertyInfo == null || propertyInfo.GetGetMethod() == null)
			{
				Log.Error($"PropertyInfo for {MemberType}.{Property} not found.");
				return false;
			}
			object parent = Parent(vehicleDef, propertyInfo);
			if (parent != null || propertyInfo.GetGetMethod().IsStatic)
			{
				if (!DisableIfEqualTo.Equals(Unassigned))
				{
					object value = propertyInfo.GetValue(parent);
					tooltip = BuildDisabledReport(propertyInfo, "=", value);
					return value.Equals(DisableIfEqualTo);
				}
				if (!DisableIfNotEqualTo.Equals(Unassigned))
				{
					object value = propertyInfo.GetValue(parent);
					tooltip = BuildDisabledReport(propertyInfo, "≠", value);
					return !value.Equals(DisableIfNotEqualTo);
				}
			}
			return false;
		}

		public bool FieldDisabled(VehicleDef vehicleDef, out string tooltip)
		{
			tooltip = string.Empty;
			if (Field.NullOrEmpty())
			{
				return false;
			}
			if (MemberType == null)
			{
				Log.Warning($"DisableSettingConditional has Field name, not null MemberType. MemberType property must be included for FieldInfo to be evaluated.");
				return false;
			}
			FieldInfo fieldInfo = AccessTools.Field(MemberType, Field);
			if (fieldInfo == null)
			{
				Log.Error($"FieldInfo for {MemberType}.{Field} not found.");
				return false;
			}
			object parent = Parent(vehicleDef, fieldInfo);
			if (parent != null || fieldInfo.IsStatic)
			{
				if (!DisableIfEqualTo.Equals(Unassigned))
				{
					if (!SettingsCache.TryGetValue(vehicleDef, fieldInfo, out object value))
					{
						value = fieldInfo.GetValue(parent);
					}
					tooltip = BuildDisabledReport(fieldInfo, "=", value);
					return value.Equals(DisableIfEqualTo);
				}
				if (!DisableIfNotEqualTo.Equals(Unassigned))
				{
					if (!SettingsCache.TryGetValue(vehicleDef, fieldInfo, out object value))
					{
						value = fieldInfo.GetValue(parent);
					}
					tooltip = BuildDisabledReport(fieldInfo, "≠", value);
					return !value.Equals(DisableIfNotEqualTo);
				}
			}
			return false;
		}

		public string BuildDisabledReport(MemberInfo memberInfo, string comparisonLabel, object value)
		{
			string memberName = memberInfo.Name;
			if (memberInfo.TryGetAttribute<PostToSettingsAttribute>(out var attribute))
			{
				memberName = attribute.ResolvedLabel();
			}
			return $"{memberName} {comparisonLabel} {value}";
		}

		private static object Parent(VehicleDef vehicleDef, MemberInfo memberInfo)
		{
			object parent = IterateTypesForParent(vehicleDef, memberInfo);
			if (parent == null && !vehicleDef.comps.NullOrEmpty())
			{
				foreach (CompProperties comp in vehicleDef.comps)
				{
					parent = IterateTypesForParent(comp, memberInfo);
					if (parent != null)
					{
						return parent;
					}
				}
			}
			return parent;
		}

		private static object IterateTypesForParent(object parent, MemberInfo memberInfo)
		{
			if (parent == null)
			{
				return null;
			}
			foreach (FieldInfo fieldInfo in parent.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (fieldInfo == memberInfo)
				{
					return parent;
				}
				if (fieldInfo.TryGetAttribute(out PostToSettingsAttribute settings) && settings.ParentHolder)
				{
					object value = fieldInfo.GetValue(parent);
					object recursiveParent = IterateTypesForParent(value, fieldInfo);
					if (recursiveParent != null)
					{
						return recursiveParent;
					}
				}
			}
			return null;
		}
	}
}
