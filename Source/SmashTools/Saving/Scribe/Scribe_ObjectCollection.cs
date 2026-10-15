using System.Xml;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmashTools
{
	public static class Scribe_ObjectCollection
	{
		public static void Look<T>(ref List<T> list, string label, bool forceSave = true)
		{
			var objectList = list.Cast<object>().ToList();
			Look(ref objectList, label, forceSave);
			list = objectList.Cast<T>().ToList();
		}

		public static void Look(ref List<object> list, string label, bool forceSave = true)
		{
			if (Scribe.EnterNode(label))
			{
				try
				{
					if (Scribe.mode == LoadSaveMode.Saving)
					{
						if (list is null)
						{
							Scribe.saver.WriteAttribute("IsNull", "True");
							return;
						}
						for(int i = 0; i < list.Count; i++)
						{
							var obj = list[i];
							Scribe_ObjectValue.Look(ref obj, "li", forceSave);
						}
					}
					else if (Scribe.mode == LoadSaveMode.LoadingVars)
					{
						XmlNode curXmlParent = Scribe.loader.curXmlParent;
						XmlAttribute xmlAttribute = curXmlParent.Attributes["IsNull"];
						if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
						{
							list = null;
							return;
						}
						list = new List<object>(curXmlParent.ChildNodes.Count);
						foreach (XmlNode childNode in curXmlParent.ChildNodes)
						{
							object item = ObjectValueExtractor.ValueFromNode(childNode);
							list.Add(item);
						}
					}
				}
				finally
				{
					Scribe.ExitNode();
				}
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				list = null;
			}
		}
	}
}
