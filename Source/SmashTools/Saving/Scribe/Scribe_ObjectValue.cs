using System;
using System.Xml;
using System.Collections.Generic;
using Verse;

namespace SmashTools
{
	public static class Scribe_ObjectValue
	{
		public static void Look(ref object obj, string label, bool forceSave = false)
		{
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				if (obj.GetType() == typeof(TargetInfo))
				{
					Log.Error("Saving a TargetInfo " + label + " with Scribe_Values. TargetInfos must be saved with Scribe_TargetInfo.");
					return;
				}
				if (obj is Thing)
				{
					Log.Error("Using Scribe_Values with a Thing reference " + label + ". Use Scribe_References or Scribe_Deep instead.");
					return;
				}
				if (obj is IExposable)
				{
					Log.Error("Using Scribe_Values with a IExposable reference " + label + ". Use Scribe_References or Scribe_Deep instead.");
					return;
				}
				if (obj is Def)
				{
					Log.Error("Using Scribe_Values with a Def " + label + ". Use Scribe_Defs instead.");
					return;
				}
				object defaultValue = obj.GetDefaultValue();
				if (forceSave || (obj is null && defaultValue != null) || (obj != null && !obj.Equals(defaultValue)))
				{
					if (obj is null)
					{
						if (!Scribe.EnterNode(label))
						{
							return;
						}
						try
						{
							Scribe.saver.WriteAttribute("IsNull", "True");
							return;
						}
						finally
						{
							Scribe.ExitNode();
						}
					}
					if (Scribe.EnterNode(label))
					{
						try
						{
							List<Pair<string, string>> attributes = new List<Pair<string, string>>()
							{
								InnerTypeAttributePair(obj),
								SavingTypeAttributePair(obj)
							};
							Scribe.saver.WriteElementWithAttributes(obj.ToString(), attributes);
						}
						finally
						{
							Scribe.ExitNode();
						}
					}
					return;
				}
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				obj = ObjectValueExtractor.ValueFromNode(Scribe.loader.curXmlParent[label]);
			}
		}

		public static Type RetrieveObjectType(object obj)
		{
			if (obj is INestedType nestedType)
			{
				return nestedType.InnerType;
			}
			return obj.GetType();
		}

		public static Pair<string, string> InnerTypeAttributePair(object obj)
		{
			return new Pair<string, string>("Type", RetrieveObjectType(obj).ToString());
		}

		public static Pair<string, string> SavingTypeAttributePair(object obj)
		{
			return new Pair<string, string>("SavedField", (obj.GetType() == typeof(SavedField<object>)).ToString());
		}
	}
}
