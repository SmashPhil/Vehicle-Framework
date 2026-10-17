using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SmashTools;

public static class GUIState
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Disable()
	{
		GUI.enabled = false;
		GUI.color = UIElements.InactiveColor;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Enable()
	{
		GUI.enabled = true;
		GUI.color = Color.white;
	}

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Disabler DisableIf(bool disable)
  {
    return new Disabler(disable);
  }

  public readonly struct Disabler : IDisposable
	{
		private readonly bool prevState;
		private readonly Color prevColor;

		public Disabler()
		{
			prevState = GUI.enabled;
			prevColor = GUI.color;
      Disable();
    }

    public Disabler(bool disable)
    {
      prevState = GUI.enabled;
      prevColor = GUI.color;
      if (disable)
      {
        Disable();
      }
    }

    void IDisposable.Dispose()
		{
			GUI.enabled = prevState;
			GUI.color = prevColor;
		}
	}
}