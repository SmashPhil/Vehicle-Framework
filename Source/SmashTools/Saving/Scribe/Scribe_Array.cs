using System;
using System.Collections.Generic;
using System.Xml;
using CoreLib;
using RimWorld.Planet;
using Verse;

namespace SmashTools;

public static class Scribe_Array
{
  public static void Look<T>(ref T[] array, string label, bool saveDestroyedThings = false,
    LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
  {
    if (lookMode == LookMode.Undefined && !Scribe_Universal.TryResolveLookMode(typeof(T), out lookMode))
    {
      Trace.Fail($"LookArray call with type {typeof(T)} must have lookMode set explicitly.");
      return;
    }

    if (Scribe.EnterNode(label))
    {
      try
      {
        if (Scribe.mode == LoadSaveMode.Saving)
        {
          if (array == null)
          {
            Scribe.saver.WriteAttribute("IsNull", "True");
            return;
          }
          Scribe.saver.WriteAttribute("Size", array.Length.ToString());
          for (int i = 0; i < array.Length; i++)
          {
            T item = array[i];
            if (lookMode == LookMode.Value)
            {
              Scribe_Values.Look(ref item, "li", forceSave: true);
            }
            else if (lookMode == LookMode.LocalTargetInfo)
            {
              LocalTargetInfo localTargetInfo = (LocalTargetInfo)(object)item;
              Scribe_TargetInfo.Look(ref localTargetInfo, saveDestroyedThings, "li");
            }
            else if (lookMode == LookMode.TargetInfo)
            {
              TargetInfo targetInfo = (TargetInfo)(object)item;
              Scribe_TargetInfo.Look(ref targetInfo, saveDestroyedThings, "li");
            }
            else if (lookMode == LookMode.GlobalTargetInfo)
            {
              GlobalTargetInfo globalTargetInfo = (GlobalTargetInfo)(object)item;
              Scribe_TargetInfo.Look(ref globalTargetInfo, saveDestroyedThings, "li");
            }
            else if (lookMode == LookMode.Def)
            {
              Def def = (Def)(object)item;
              Scribe_Defs.Look(ref def, "li");
            }
            else if (lookMode == LookMode.BodyPart)
            {
              BodyPartRecord bodyPartRecord = (BodyPartRecord)(object)item;
              Scribe_BodyParts.Look(ref bodyPartRecord, "li", null);
            }
            else if (lookMode == LookMode.Deep)
            {
              Scribe_Deep.Look(ref item, saveDestroyedThings, "li", ctorArgs);
            }
            else if (lookMode == LookMode.Reference)
            {
              if (item != null && item is not ILoadReferenceable)
              {
                string typeName = item != null ? item?.GetType()?.Name : "Null";
                throw new InvalidOperationException($"Cannot save reference to {typeName} item if it is not ILoadReferenceable");
              }
              ILoadReferenceable loadReferenceable = item as ILoadReferenceable;
              Scribe_References.Look(ref loadReferenceable, "li", saveDestroyedThings);
            }
          }
          return;
        }
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
          XmlNode curXmlParent = Scribe.loader.curXmlParent;
          XmlAttribute isNullAttribute = curXmlParent.Attributes["IsNull"];
          if (isNullAttribute != null && isNullAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
          {
            if (lookMode == LookMode.Reference)
            {
              Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(null, null);
            }
            array = null;
            return;
          }
          XmlAttribute sizeAttribute = curXmlParent.Attributes["Size"];
          int size;
          if (sizeAttribute == null)
          {
            Trace.Fail($"Size attribute for array not found. Defaulting to size of xml items listed.");
            size = curXmlParent.ChildNodes.Count;
          }
          else
          {
            size = Convert.ToInt32(sizeAttribute.Value);
          }
          array = new T[size];
          if (lookMode == LookMode.Reference)
          {
            List<string> refList = new List<string>(size);
            foreach (XmlNode node in curXmlParent.ChildNodes)
            {
              refList.Add(node.InnerText);
            }
            Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(refList, "");
          }
          else
          {
            int i = 0;
            foreach (XmlNode node in curXmlParent.ChildNodes)
            {
              T item = lookMode switch
              {
                LookMode.Value => ScribeExtractor.ValueFromNode(node, default(T)),
                LookMode.Deep => ScribeExtractor.SaveableFromNode<T>(node, ctorArgs),
                // LookMode.Reference handled above, must pass to CrossReferences and resolve at a later scribe step
                LookMode.Def => ScribeExtractor.DefFromNodeUnsafe<T>(node),
                LookMode.LocalTargetInfo => (T)(object)ScribeExtractor.LocalTargetInfoFromNode(node, i.ToString(),
                  LocalTargetInfo.Invalid),
                LookMode.TargetInfo => (T)(object)ScribeExtractor.TargetInfoFromNode(node, i.ToString(),
                  TargetInfo.Invalid),
                LookMode.GlobalTargetInfo => (T)(object)ScribeExtractor.GlobalTargetInfoFromNode(node, i.ToString(),
                  GlobalTargetInfo.Invalid),
                LookMode.BodyPart => (T)(object)ScribeExtractor.BodyPartFromNode(node, i.ToString(), null),
                // Default case includes Undefined
                _ => throw new NotImplementedException(),
              };
              array[i] = item;
              i++;
            }
          }
        }
        else if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
        {
          if (lookMode == LookMode.Reference)
          {
            array = [.. Scribe.loader.crossRefs.TakeResolvedRefList<T>("")];
          }
          else
          {
            if (lookMode == LookMode.LocalTargetInfo || lookMode == LookMode.TargetInfo ||
              lookMode == LookMode.GlobalTargetInfo)
            {
              for (int i = 0; i < array.Length; i++)
              {
                array[i] = lookMode switch
                {
                  LookMode.LocalTargetInfo => (T)(object)ScribeExtractor.ResolveLocalTargetInfo(
                    (LocalTargetInfo)(object)array[i], i.ToString()),
                  LookMode.TargetInfo => (T)(object)ScribeExtractor.ResolveTargetInfo(
                  (TargetInfo)(object)array[i], i.ToString()),
                  LookMode.GlobalTargetInfo => (T)(object)ScribeExtractor.ResolveGlobalTargetInfo(
                    (GlobalTargetInfo)(object)array[i], i.ToString()),
                  _ => throw new InvalidOperationException()
                };

              }
            }
          }
        }
        return;
      }
      finally
      {
        Scribe.ExitNode();
      }
    }
    if (Scribe.mode == LoadSaveMode.LoadingVars)
    {
      if (lookMode == LookMode.Reference)
      {
        Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(null, label);
      }
      array = null;
    }
  }
}