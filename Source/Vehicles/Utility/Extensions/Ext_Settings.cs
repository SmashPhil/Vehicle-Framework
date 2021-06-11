using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

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
			int[] ints = version.Split('.').Select(int.Parse).ToArray();
			int value = 0;
			foreach (int digit in ints)
			{
				value = Convert.ToInt32($"{value}{digit}");
			}
			return value;
		}
	}
}
