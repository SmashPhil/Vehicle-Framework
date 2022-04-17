using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public static class Ext_Settings
	{
		/// <summary>
		/// Get all fields containing PostToSettings attribute in <paramref name="type"/>
		/// </summary>
		/// <param name="type"></param>
		public static List<FieldInfo> GetPostSettingsFields(this Type type)
		{
			return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => Attribute.IsDefined(f, typeof(PostToSettingsAttribute))).ToList();
		}

		/// <summary>
		/// Combine integers together from version string for ordering
		/// </summary>
		/// <param name="version"></param>
		public static int CombineVersionString(string version)
		{
			string rawVersion = new string(version.Where(c => char.IsDigit(c)).ToArray());
			if (int.TryParse(rawVersion, out int order))
			{
				return order;
			}
			Log.Error($"Unable to parse {version} as raw value following Major.Minor.Revision sequence.");
			return 0;
		}
	}
}
