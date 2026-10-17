using System.Collections.Generic;
using System.Linq;
using Verse;

namespace UpdateLogTool;

public static class UpdateHandler
{
  // TODO 1.8 - Can switch to single log field and remove expectations of multiple mods using this
  public static readonly HashSet<UpdateLog> modUpdates = [];

  public static readonly HashSet<UpdateLog> updateList = [];

  public static void LoadUpdateLog(ModContentPack mod)
  {
    UpdateLog log = FileReader.LoadUpdateLog(mod);
    if (log == null)
    {
      Log.Error($"Unable to load update log from {mod.Name}");
      return;
    }
    modUpdates.Add(log);
    SegmentParser.ParseAndCreateSegments();
    SegmentParser.GenerateRegexText();
  }

  public static UpdateLog UpdateLogData(ModContentPack mod)
  {
    return modUpdates.FirstOrDefault(m => m.Mod == mod);
  }
  
  public static void CheckUpdates(UpdateFor updating)
  {
    updateList.Clear();
    foreach (UpdateLog log in modUpdates)
    {
      if (log.UpdateData.updateOn == updating)
      {
        if (log.UpdateData.update)
        {
          log.NotifyModUpdated();
          if (!log.UpdateData.description.NullOrEmpty())
          {
            updateList.Add(log);
          }
        }
        log.SaveUpdateStatus();
      }
    }
    if (updateList.Any())
    {
      Find.WindowStack.Add(new Dialog_NewUpdate(updateList));
    }
  }
}