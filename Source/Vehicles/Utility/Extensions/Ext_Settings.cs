using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Vehicles
{
	public static class Ext_Settings
	{
		public static List<FieldInfo> GetPostSettingsFields(this Type type)
		{
			return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => Attribute.IsDefined(f, typeof(PostToSettingsAttribute))).ToList();
		}
	}
}
