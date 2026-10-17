using System;
using UnityEngine;

namespace SmashTools.Rendering;

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
