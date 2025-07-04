using System;
using Verse;

namespace Vehicles.UnitTesting;

public readonly struct ScopeWindow : IDisposable
{
  private readonly Window window;

  public ScopeWindow(Window window)
  {
    this.window = window;
  }

  void IDisposable.Dispose()
  {
    window.Close(doCloseSound: false);
  }
}