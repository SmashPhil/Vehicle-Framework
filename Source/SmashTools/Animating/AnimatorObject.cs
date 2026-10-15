using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SmashTools
{
	public class AnimatorObject
	{
		public object parent;
		public FieldInfo fieldInfo;
		public string category;
		public string prefix;

		public AnimatorObject(object parent, FieldInfo fieldInfo, string category, string prefix)
		{
			this.parent = parent;
			this.fieldInfo = fieldInfo;
			this.category = category;
			this.prefix = prefix;
		}

		public string DisplayName => prefix.NullOrEmpty() ? fieldInfo.Name : $"{prefix}.{fieldInfo.Name}";

		public LinearCurve Curve => parent != null ? fieldInfo.GetValue(parent) as LinearCurve : null;

		public void SetCurve(LinearCurve curve)
		{
			fieldInfo.SetValue(parent, curve);
		}
	}
}
