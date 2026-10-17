using System;
using Verse;
using Waylith.UnityCompat;

namespace SmashTools.Rendering;

internal readonly struct WindowStackContext : IDisposable
{
  private readonly UIRoot root;
  private readonly WindowStack previous;
  private readonly int previousUiWidth;
  private readonly int previousUiHeight;
  private readonly int previousScreenWidth;
  private readonly int previousScreenHeight;
  private readonly int previousWindowWidth;
  private readonly int previousWindowHeight;
  private readonly bool previousFullScreen;

  public WindowStackContext(WindowStack windowStack, int width, int height)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
    root = Find.UIRoot;
    previous = root?.windows;
    UnityScreenState screen = UnityRuntime.Current!.Screen;
    previousUiWidth = UI.screenWidth;
    previousUiHeight = UI.screenHeight;
    previousScreenWidth = screen.Width;
    previousScreenHeight = screen.Height;
    previousWindowWidth = screen.WindowWidth;
    previousWindowHeight = screen.WindowHeight;
    previousFullScreen = screen.FullScreen;

    root?.windows = windowStack;
    UI.screenWidth = width;
    UI.screenHeight = height;
    UnityRuntime.Current.ConfigureScreen(width, height);
    UnityRuntime.Current.ConfigureScreenWindow(width, height);
  }

  void IDisposable.Dispose()
  {
    UI.screenWidth = previousUiWidth;
    UI.screenHeight = previousUiHeight;
    UnityRuntime.Current!.ConfigureScreen(previousScreenWidth, previousScreenHeight, previousFullScreen);
    UnityRuntime.Current.ConfigureScreenWindow(previousWindowWidth, previousWindowHeight);
    root?.windows = previous;
  }
}
