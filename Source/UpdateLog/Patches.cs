using CoreLib.Performance;
using SmashTools;
using Verse;

namespace UpdateLogTool;

[StaticConstructorOnStartup]
internal static class UpdateLogEvents
{
  // Character Editor stalls for a few seconds on the main menu's first frame. Delay the update log for a small
  // amount so CE finishes its blocking operation first, otherwise it will pop up before the main menu draws and
  // then trigger CE to run when the update log is closed, causing people to think the update log is what's freezing
  // the main menu for a few seconds.
  private const int DelayUpdateLog = 500; // ms

  static UpdateLogEvents()
  {
    GameEvent.OnMainMenu += UpdateOnStartup;
    GameEvent.OnNewGame += UpdateOnNewGame;
    GameEvent.OnNewGame += UpdateOnGameInit;
    GameEvent.OnLoadGame += UpdateOnLoadedGame;
    GameEvent.OnLoadGame += UpdateOnGameInit;
  }

  private static void UpdateOnStartup()
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      new Debouncer(() => UpdateHandler.CheckUpdates(UpdateFor.Startup), DelayUpdateLog).Invoke();
    });
  }

  private static void UpdateOnGameInit()
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      new Debouncer(() => UpdateHandler.CheckUpdates(UpdateFor.GameInit), DelayUpdateLog).Invoke();
    });
  }

  private static void UpdateOnNewGame()
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      new Debouncer(() => UpdateHandler.CheckUpdates(UpdateFor.NewGame), DelayUpdateLog).Invoke();
    });
  }

  private static void UpdateOnLoadedGame()
  {
    LongEventHandler.ExecuteWhenFinished(delegate
    {
      new Debouncer(() => UpdateHandler.CheckUpdates(UpdateFor.LoadedGame), DelayUpdateLog).Invoke();
    });
  }
}