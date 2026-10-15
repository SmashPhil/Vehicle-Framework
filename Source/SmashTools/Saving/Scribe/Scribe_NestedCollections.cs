using System.Xml;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;

namespace SmashTools
{
	public class Scribe_NestedCollections
	{
		public static void Look<K, K2, V>(ref Dictionary<K, Dictionary<K2, V>> dict, string label, LookMode keyLookMode, LookMode innerKeyLookMode, LookMode innerValueLookMode)
		{
			if (keyLookMode == LookMode.Reference || innerKeyLookMode == LookMode.Reference || innerValueLookMode == LookMode.Reference)
			{
				Log.Warning($"Scribe_NestedCollections LookMode.Reference not yet supported.");
				return;
			}
			K key;
			List<K2> innerKeys = new List<K2>();
			List<V> innerValues = new List<V>();
			if (Scribe.EnterNode(label))
			{
				try
				{
					if (Scribe.mode == LoadSaveMode.Saving && dict is null)
					{
						Scribe.saver.WriteAttribute("IsNull", "True");
						return;
					}
					if (Scribe.mode == LoadSaveMode.LoadingVars)
					{
						XmlAttribute xmlAttribute = Scribe.loader.curXmlParent.Attributes["IsNull"];
						if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
						{
							dict = null;
						}
						else
						{
							dict = new Dictionary<K, Dictionary<K2, V>>();
						}
					}
					if (Scribe.mode == LoadSaveMode.Saving && dict != null)
					{
						foreach (var outerPair in dict)
						{
							if (Scribe.EnterNode("li"))
							{
								try
								{
									key = outerPair.Key;
									switch (keyLookMode)
									{
										case LookMode.Value:
											Scribe_Values.Look(ref key, "key");
											break;
										case LookMode.Def:
											if (key is Def def)
											{
												Scribe_Defs.Look(ref def, "def");
											}
											break;
										case LookMode.Deep:
											Scribe_Deep.Look(ref key, "key");
											break;
										case LookMode.Reference:
											if (key is ILoadReferenceable referenceable)
											{
												Scribe_References.Look(ref referenceable, "referenceable");
											}
											else
											{
												Log.Error("Cannot use LookMode.Reference with non ILoadReferenceable object");
											}
											break;
									}
									innerKeys.Clear();
									innerValues.Clear();
									innerKeys.AddRange(outerPair.Value.Keys);
									innerValues.AddRange(outerPair.Value.Values);
									Scribe_Collections.Look(ref innerKeys, "innerKeys", innerKeyLookMode);
									if (typeof(V) == typeof(object) || typeof(V) == typeof(SavedField<object>))
									{
										Scribe_ObjectCollection.Look(ref innerValues, "innerValues");
									}
									else
									{
										Scribe_Collections.Look(ref innerValues, "innerValues", innerValueLookMode);
									}
								}
								finally
								{
									Scribe.ExitNode();
								}
							}
								
						}
						key = default;
						innerKeys = null;
						innerValues = null;
					}
					else if (Scribe.mode == LoadSaveMode.LoadingVars)
					{
						XmlNode curXmlParent = Scribe.loader.curXmlParent;
						XmlAttribute xmlAttribute = curXmlParent.Attributes["IsNull"];
						if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
						{
							if (keyLookMode == LookMode.Reference)
							{
								Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(null, null);
							}
							dict = null;
							return;
						}
						dict = new Dictionary<K, Dictionary<K2, V>>();
						foreach (XmlNode fieldSettingsItem in curXmlParent.ChildNodes)
						{
							key = default;
							switch (keyLookMode)
							{
								case LookMode.Value:
									key = ScribeExtractor.ValueFromNode<K>(fieldSettingsItem.ChildNodes[0], default);
									break;
								case LookMode.Deep:
									key = ScribeExtractor.SaveableFromNode<K>(fieldSettingsItem.ChildNodes[0], new object[] { });
									break;
								case LookMode.Def:
									key = ScribeExtractor.DefFromNodeUnsafe<K>(fieldSettingsItem.ChildNodes[0]);
									break;
								case LookMode.BodyPart:
									key = (K)(object)ScribeExtractor.BodyPartFromNode(fieldSettingsItem.ChildNodes[0], "0", null);
									break;
								case LookMode.LocalTargetInfo:
									key = (K)(object)ScribeExtractor.LocalTargetInfoFromNode(fieldSettingsItem.ChildNodes[0], "0", LocalTargetInfo.Invalid);
									break;
								case LookMode.TargetInfo:
									key = (K)(object)ScribeExtractor.TargetInfoFromNode(fieldSettingsItem.ChildNodes[0], "0", TargetInfo.Invalid);
									break;
								case LookMode.GlobalTargetInfo:
									key = (K)(object)ScribeExtractor.GlobalTargetInfoFromNode(fieldSettingsItem.ChildNodes[0], "0", GlobalTargetInfo.Invalid);
									break;
								case LookMode.Reference:
									string referenceId = fieldSettingsItem.ChildNodes[0].InnerText;
									Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(new List<string>() { referenceId }, "");
									break;
							}
							ExtractToLists(fieldSettingsItem, ref innerKeys, ref innerValues, innerKeyLookMode, innerValueLookMode);
							dict.Add(key, innerKeys.Zip(innerValues, (k, v) => new { k, v }).ToDictionary(d => d.k, d => d.v));
						}
					}
					if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs && (keyLookMode == LookMode.Reference || innerKeyLookMode == LookMode.Reference || innerValueLookMode == LookMode.Reference))
					{
						
					}
					return;
				}
				finally
				{
					Scribe.ExitNode();
				}
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				dict = null;
			}
		}

		public static void Look<K, V>(ref Dictionary<K, List<V>> dict, string label, LookMode keyLookMode, LookMode valueLookMode)
		{
			K key;
			List<K> keys = new List<K>();
			List<V> values = new List<V>();
			if (Scribe.EnterNode(label))
			{
				try
				{
					if (Scribe.mode == LoadSaveMode.Saving && dict is null)
					{
						Scribe.saver.WriteAttribute("IsNull", "True");
						return;
					}
					if (Scribe.mode == LoadSaveMode.LoadingVars)
					{
						XmlAttribute xmlAttribute = Scribe.loader.curXmlParent.Attributes["IsNull"];
						if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
						{
							dict = null;
						}
						else
						{
							dict = new Dictionary<K, List<V>>();
						}
					}
					if (Scribe.mode == LoadSaveMode.Saving && dict != null)
					{
						foreach (var pair in dict)
						{
							if (Scribe.EnterNode("li"))
							{
								try
								{
									key = pair.Key;
									switch (keyLookMode)
									{
										case LookMode.Value:
											Scribe_Values.Look(ref key, "key");
											break;
										case LookMode.Def:
											if (key is Def def)
											{
												Scribe_Defs.Look(ref def, "key");
											}
											break;
										case LookMode.Deep:
											Scribe_Deep.Look(ref key, "key");
											break;
										case LookMode.Reference:
											if (key is ILoadReferenceable referenceable)
											{
												Scribe_References.Look(ref referenceable, "referenceable");
											}
											else
											{
												Log.Error("Cannot use LookMode.Reference with non ILoadReferenceable object");
											}
											break;
									}
									values.Clear();
									values.AddRange(pair.Value);
									Scribe_Collections.Look(ref values, "values", valueLookMode);
								}
								finally
								{
									Scribe.ExitNode();
								}
							}
						}
						key = default;
						values = null;
					}
					else if (Scribe.mode == LoadSaveMode.LoadingVars)
					{
						XmlNode curXmlParent = Scribe.loader.curXmlParent;
						XmlAttribute xmlAttribute = curXmlParent.Attributes["IsNull"];
						if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
						{
							if (keyLookMode == LookMode.Reference)
							{
								Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(null, null);
							}
							dict = null;
							return;
						}
						dict = new Dictionary<K, List<V>>();
						foreach (XmlNode xmlNode in curXmlParent.ChildNodes)
						{
							key = default;
							switch (keyLookMode)
							{
								case LookMode.Value:
									key = ScribeExtractor.ValueFromNode<K>(xmlNode.ChildNodes[0], default);
									break;
								case LookMode.Deep:
									key = ScribeExtractor.SaveableFromNode<K>(xmlNode.ChildNodes[0], new object[] { });
									break;
								case LookMode.Def:
									key = ScribeExtractor.DefFromNodeUnsafe<K>(xmlNode.ChildNodes[0]);
									break;
								case LookMode.BodyPart:
									key = (K)(object)ScribeExtractor.BodyPartFromNode(xmlNode.ChildNodes[0], "0", null);
									break;
								case LookMode.LocalTargetInfo:
									key = (K)(object)ScribeExtractor.LocalTargetInfoFromNode(xmlNode.ChildNodes[0], "0", LocalTargetInfo.Invalid);
									break;
								case LookMode.TargetInfo:
									key = (K)(object)ScribeExtractor.TargetInfoFromNode(xmlNode.ChildNodes[0], "0", TargetInfo.Invalid);
									break;
								case LookMode.GlobalTargetInfo:
									key = (K)(object)ScribeExtractor.GlobalTargetInfoFromNode(xmlNode.ChildNodes[0], "0", GlobalTargetInfo.Invalid);
									break;
								case LookMode.Reference:
									string referenceId = xmlNode.ChildNodes[0].InnerText;
									Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(new List<string>() { referenceId }, "");
									break;
							}
							ExtractList(xmlNode.ChildNodes[1].ChildNodes, ref values, valueLookMode);
							Scribe_Collections.Look(ref values, "values", valueLookMode);
							dict.Add(key, values);
						}
					}
					if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs && (keyLookMode == LookMode.Reference || valueLookMode == LookMode.Reference))
					{
						
					}
					return;
				}
				finally
				{
					Scribe.ExitNode();
				}
			}
			else if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				dict = null;
			}
		}

		public static void Look<K, V>(ref Dictionary<K, HashSet<V>> dict, string label, LookMode keyLookMode, LookMode valueLookMode)
		{
			var listDict = new Dictionary<K, List<V>>();
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				foreach (var pair in dict)
				{
					listDict.Add(pair.Key, pair.Value.ToList());
				}
			}
			Look(ref listDict, label, keyLookMode, valueLookMode);
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				dict = new Dictionary<K, HashSet<V>>();
				foreach (var pair in listDict)
				{
					dict.Add(pair.Key, pair.Value.ToHashSet());
				}
			}
		}

		private static void ExtractToLists<K, V>(XmlNode parentNode, ref List<K> keys, ref List<V> values, LookMode keyLookMode, LookMode valueLookMode)
		{
			ExtractList(parentNode.ChildNodes[1].ChildNodes, ref keys, keyLookMode);
			ExtractList(parentNode.ChildNodes[2].ChildNodes, ref values, valueLookMode);
		}

		private static void ExtractList<T>(XmlNodeList nodeList, ref List<T> list, LookMode lookMode)
		{
			list.Clear();
			foreach (XmlNode node in nodeList)
			{
				switch (lookMode)
				{ 
					case LookMode.Value:
						{
							T obj = ScribeExtractor.ValueFromNode<T>(node, default);
							list.Add(obj);
						}
						break;
					case LookMode.Def:
						{
							T obj = ScribeExtractor.DefFromNodeUnsafe<T>(node);
							list.Add(obj);
						}
						break;
					case LookMode.Deep:
						{
							T obj = ScribeExtractor.SaveableFromNode<T>(node, new object[] { });
							list.Add(obj);
						}
						break;
					default:
						{
							T obj = (T)ObjectValueExtractor.ValueFromNode(node);
							list.Add(obj);
						}
						break;
				}
			}
		}
	}
}
