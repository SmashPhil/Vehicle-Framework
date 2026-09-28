using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public sealed class AnimationClip : IAnimationFile
	{
		public const string DefaultAnimName = "Untitled";
		public const string FileExtension = ".rwa"; // RimWorld Animation
		public const int DefaultFrameCount = 60;

		public int frameCount = DefaultFrameCount;

		public int cycleOffset;

		private Guid guid;

		public List<AnimationPropertyParent> properties = [];
		public List<AnimationEvent> events = [];

		public Guid Guid => guid;

		public string FilePath { get; set; }

		public string FileName { get; set; }

		public string FileNameWithExtension => FileName + FileExtension;

		public void SetFrame()
		{
			frameCount = DefaultFrameCount;
			if (properties.Count > 0)
			{
				int max = 0;
				foreach (AnimationPropertyParent propertyParent in properties)
				{
					int propertyMax = -1;
					foreach (AnimationProperty property in propertyParent.Properties)
					{
						propertyMax = MaxFrame(property);
					}

					if (propertyMax > max)
					{
						max = propertyMax;
					}
				}

				if (max >= 0)
				{
					frameCount = max;
				}
			}
		}

		public void RecacheFrameCount()
		{
			frameCount = DefaultFrameCount;
			if (properties.Count > 0)
			{
				int max = 0;
				foreach (AnimationPropertyParent propertyParent in properties)
				{
					int propertyMax = -1;
					foreach (AnimationProperty property in propertyParent.Properties)
					{
						propertyMax = MaxFrame(property);
					}

					if (propertyMax > max)
					{
						max = propertyMax;
					}
				}

				if (max >= 0)
				{
					frameCount = max;
				}
			}
		}

		private static int MaxFrame(AnimationProperty property)
		{
			if (property.curve.PointsCount > 0)
			{
				return property.curve.points.Max(keyFrame => keyFrame.frame);
			}
			return -1;
		}

		internal void ValidateEventOrder()
		{
			events.Sort();
		}

		public static AnimationClip CreateEmpty()
		{
			AnimationClip animationClip = new AnimationClip();
			animationClip.FileName = AnimationLoader.GetAvailableName(AnimationLoader.Cache<AnimationClip>.GetAll()
				.Select(clip => clip.FileName), DefaultAnimName);
			animationClip.guid = Guid.NewGuid();
			return animationClip;
		}

		void IAnimationFile.ResolveReferences()
		{
			if (!properties.NullOrEmpty())
			{
				foreach (AnimationPropertyParent propertyParent in properties)
				{
					propertyParent.ResolveReferences();
				}
			}
		}

		void IXmlExport.Export()
		{
			ValidateEventOrder();

			XmlExporter.WriteObject(nameof(frameCount), frameCount);
			XmlExporter.WriteObject(nameof(cycleOffset), cycleOffset);
			XmlExporter.WriteObject(nameof(guid), guid);

			XmlExporter.WriteCollection(nameof(properties), properties);
			XmlExporter.WriteCollection(nameof(events), events);
		}

		public static implicit operator bool(AnimationClip clip)
		{
			return clip != null;
		}
	}
}
