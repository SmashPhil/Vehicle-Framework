using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Reflection;
using Verse;

namespace SmashTools.Xml
{
	public static class ClassLoader
	{
		public static void Distribute<T>(XmlNode rootNode, T instance)
		{
			FieldInfo[] fields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (FieldInfo fieldInfo in fields)
			{
				string name = fieldInfo.Name;
				XmlNode childNode = rootNode[name];
				if (childNode != null)
				{
					object value = GenGeneric.InvokeStaticGenericMethod(typeof(DirectXmlToObject), fieldInfo.FieldType, nameof(DirectXmlToObject.ObjectFromXml), childNode, true);
					fieldInfo.SetValue(instance, value);
				}
			}
		}
	}
}
