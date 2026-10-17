using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	public static class Ext_object
	{
		/// <summary>
		/// Retrieve default value of this object. If <paramref name="obj"/> implements <see cref="IDefaultValue"/> then the default value will be retrieved via the interface
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		public static object GetDefaultValue<T>(this T obj)
		{
			if (obj is IDefaultValue defaultValue)
			{
				return defaultValue.DefaultValue;
			}
			return obj?.GetType().GetDefaultValue();
		}
	}
}
