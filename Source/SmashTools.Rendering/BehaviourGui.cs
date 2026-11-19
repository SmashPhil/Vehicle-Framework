using System;
using UnityEngine;

namespace SmashTools.Windowing;

internal readonly struct BehaviourGui : IDisposable
{
  public BehaviourGui(int instanceId)
  {
    GUILayoutUtility.BeginBehaviourGUI(instanceId);
  }

  public void Dispose()
  {
    GUILayoutUtility.EndBehaviourGUI();
  }
}
