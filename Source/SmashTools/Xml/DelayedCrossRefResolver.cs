using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SmashTools.Xml;

// Ludeon restricts cross-reference resolving to the def loading process, meaning 
// parsing specific xml files outside def loading will try to resolve cross-references
// immediately.  This is not viable for Animation files which load early so the cache
// can be accessible during def loading. Register cross-references here which will persist
// until ProjectStartup does a pass.
public static class DelayedCrossRefResolver
{
  private static readonly List<Wanter> wanters = [];

  public static bool Resolved { get; private set; }

  public static void Register<T>(T obj, Type defType, FieldInfo fieldInfo, string defName)
  {
    if (Resolved)
    {
      Log.Error("CrossRefs have already been resolved. You can load defs directly.");
      return;
    }
    wanters.Add(new WanterForObject(obj, defType, defName));
    throw new NotImplementedException();
  }

  public static void RegisterIndex(Array array, Type defType, string defName, int index)
  {
    if (Resolved)
    {
      Log.Error("CrossRefs have already been resolved. You can load defs directly.");
      return;
    }
    wanters.Add(new WanterForIndex(array, defType, defName, index));
  }

  internal static void ResolveAll()
  {
    if (Resolved)
    {
      Trace.Fail("Cannot resolve delayed cross references again. This is only meant to gather def " +
                 "references in non-def xml files and resolve them when DefDatabase has been loaded.");
      return;
    }
    foreach (Wanter wanter in wanters)
    {
      try
      {
        if (!wanter.TryResolve())
        {
          Log.Error($"Unable to resolve cross reference {wanter.defName}");
        }
      }
      catch (Exception ex)
      {
        Log.Error($"Exception thrown resolving cross reference {wanter.defName}\nException={ex}");
      }
    }
    Clear();
    Resolved = true;
  }

  private static void Clear()
  {
    wanters.Clear();
  }

  private abstract class Wanter
  {
    protected readonly Type defType;
    public readonly string defName;

    protected Wanter(Type defType, string defName)
    {
      this.defType = defType;
      this.defName = defName;
    }

    public abstract bool TryResolve();
  }

  private class WanterForObject : Wanter
  {
    public readonly object wanter;

    public WanterForObject(object wanter, Type defType, string defName) : base(defType, defName)
    {
      this.wanter = wanter;
    }

    public override bool TryResolve()
    {
      throw new NotImplementedException();
    }
  }

  private class WanterForIndex : Wanter
  {
    private readonly Array wanter;
    private readonly int index;

    public WanterForIndex(Array wanter, Type defType, string defName, int index) : base(defType, defName)
    {
      this.wanter = wanter;
      this.index = index;
    }

    public override bool TryResolve()
    {
      Def def = GenDefDatabase.GetDef(defType, defName);
      wanter.SetValue(def, index);
      return def != null;
    }
  }
}