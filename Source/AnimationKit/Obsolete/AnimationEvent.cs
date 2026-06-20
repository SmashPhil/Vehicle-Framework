using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SmashTools.Animations
{
	public class AnimationEvent : IXmlExport, ISelectableUI, IComparable<AnimationEvent>
	{
		public int frame;
		public DynamicDelegate method;

		int IComparable<AnimationEvent>.CompareTo(AnimationEvent other)
		{
			if (frame < other.frame) return -1;
			if (frame > other.frame) return 1;
			return 0;
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(frame), frame);
			XmlExporter.WriteObject(nameof(method), method);
		}
	}
}
