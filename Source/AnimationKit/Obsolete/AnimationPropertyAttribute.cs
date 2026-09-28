using System;
using System.Collections.Generic;
using System.Linq;

namespace SmashTools.Animations
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class AnimationPropertyAttribute : Attribute
	{
		public string Name { get; set; }
	}
}
