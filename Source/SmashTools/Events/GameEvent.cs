using System;
using JetBrains.Annotations;

namespace SmashTools;

[PublicAPI]
public static class GameEvent
{
  /// <summary>
  /// New game is being initialized.
  /// </summary>
  public static event Action OnNewGame;

  /// <summary>
  /// Existing save file is being loaded.
  /// </summary>
  public static event Action OnLoadGame;

  /// <summary>
  /// MainMenu scene has been loaded.
  /// </summary>
  public static event Action OnMainMenu;

  /// <summary>
  /// Game is about to be disposed.
  /// </summary>
  /// <remarks>
  /// This occurs before the world and maps are unloaded via MemoryUtility.
  /// </remarks>
  public static event Action OnGameDisposing;

  /// <summary>
  /// Game has been disposed.
  /// </summary>
  public static event Action OnGameDisposed;

  /// <summary>
  /// World and all maps are about to be unloaded.
  /// </summary>
  public static event Action OnWorldUnloading;

  /// <summary>
  /// World and all maps have finished being removed.
  /// </summary>
  public static event Action OnWorldRemoved;

  /// <summary>
  /// DefDatabase implied defs are currently in the pre-resolve stage.
  /// </summary>
  /// <remarks>Now is the time to add any implied defs from mods.</remarks>
  public static event Action<bool> OnGenerateImpliedDefs;

  internal static void RaiseOnNewGame()
  {
    OnNewGame?.Invoke();
  }

  internal static void RaiseOnLoadGame()
  {
    OnLoadGame?.Invoke();
  }

  internal static void RaiseOnMainMenu()
  {
    OnMainMenu?.Invoke();
  }

  internal static void RaiseOnGameDisposing()
  {
    OnGameDisposing?.Invoke();
  }

  internal static void RaiseOnGameDisposed()
  {
    OnGameDisposed?.Invoke();
  }

  internal static void RaiseOnWorldUnloading()
  {
    OnWorldUnloading?.Invoke();
  }

  internal static void RaiseOnWorldRemoved()
  {
    OnWorldRemoved?.Invoke();
  }

  internal static void RaiseOnGenerateImpliedDefs(bool hotReload)
  {
    OnGenerateImpliedDefs?.Invoke(hotReload);
  }
}