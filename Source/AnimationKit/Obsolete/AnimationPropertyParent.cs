using SmashTools.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationPropertyParent : IXmlExport, ISelectableUI, IEnumerable<AnimationProperty>
  {
		private string identifier;
		private string label;
		private string name;
		private Type type;
		
		private readonly List<AnimationProperty> properties = [];

		/// <summary>
		/// Path from IAnimator to field. Denotes the entire hierarchal path to object parent
		/// eg. IAnimator->ClassContainer->Vector3.x
		/// </summary>
		private readonly List<ObjectPath> hierarchyPath = [];

		public AnimationPropertyParent()
		{
		}

		private AnimationPropertyParent(string identifier, string label, string name, Type type, 
			List<ObjectPath> hierarchyPath)
		{
			this.identifier = identifier;
			this.label = label;
			this.name = name;
			this.type = type;
			this.hierarchyPath = hierarchyPath;
			IsIndexer = hierarchyPath.Any(path => path.IsIndexer);
		}

		public string Identifier => identifier;

		public string Label => label;

		public string LabelWithIdentifier => Identifier != null ? $"{Label} ({Identifier})" : Label;

		public string Name => name;

		public Type Type => type;

		public bool IsValid => !properties.NullOrEmpty();

		public bool IsSingle => properties.Count == 1;

		public List<AnimationProperty> Properties => properties;

		public bool IsIndexer { get; private set; }

		internal void SetSingle(AnimationProperty property)
		{
			if (IsSingle) properties[0] = property;
			else Add(property);
		}

		internal void Add(AnimationProperty property)
		{
			properties.Add(property);
		}

		public void EvaluateFrame(IAnimationObject obj, int frame)
		{
			for (int i = 0; i < properties.Count; i++)
			{
				properties[i].Evaluate(obj, frame);
			}
		}

		public bool AllKeyFramesAt(int frame)
		{
			if (!IsValid) return false;

			foreach (AnimationProperty property in properties)
			{
				if (!property.curve.KeyFrameAt(frame))
				{
					return false;
				}
			}
			return true;
		}

		public bool AnyKeyFrameAt(int frame)
		{
			foreach (AnimationProperty property in properties)
			{
				if (property.curve.KeyFrameAt(frame))
				{
					return true;
				}
			}
			return false;
		}

		internal void ResolveReferences()
		{
			foreach (AnimationProperty property in properties)
			{
				property.ResolveReferences();
			}
		}

		public IAnimationObject ObjectFromHierarchy(IAnimator animator)
		{
			object parent = animator;
			for (int i = 0; i < hierarchyPath.Count; i++)
			{
				ObjectPath path = hierarchyPath[i];
				parent = path.GetValue(parent);
				if (parent == null) return null;
			}
			return parent as IAnimationObject;
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteElement(nameof(identifier), identifier);
			XmlExporter.WriteElement(nameof(label), label);
			XmlExporter.WriteElement(nameof(name), name);
			XmlExporter.WriteElement(nameof(type), GenTypes.GetTypeNameWithoutIgnoredNamespaces(type));
			
			XmlExporter.WriteCollection(nameof(properties), properties);
			XmlExporter.WriteCollection(nameof(hierarchyPath), hierarchyPath);
		}

		public IEnumerator<AnimationProperty> GetEnumerator()
		{
			return properties.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public static AnimationPropertyParent Create(string identifier, string label, FieldInfo fieldInfo, 
			List<ObjectPath> hierarchyPath)
		{
			return new AnimationPropertyParent(identifier, label, fieldInfo.Name, fieldInfo.DeclaringType, hierarchyPath);
		}
	}
}
