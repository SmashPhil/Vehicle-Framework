using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SmashTools;

public class UndoManager
{
  private readonly Stack<UndoItem> undoStack = [];

  public bool Disabled { get; internal set; }

  public void StartOperation(Action action, Action undo)
  {
    undoStack.Push(new UndoItem(action, undo));
  }

  public void UndoOperation()
  {
    Assert.IsTrue(undoStack.Count > 0);
    UndoItem item = undoStack.Pop();
    item.undo.Invoke();
  }

  public void Clear()
  {
    undoStack.Clear();
  }

  private struct UndoItem
  {
    public Action action;
    public Action undo;

    public UndoItem(Action action, Action undo)
    {
      this.action = action;
      this.undo = undo;
    }
  }
}

public struct UndoDisable : IDisposable
{
  private UndoManager manager;

  public UndoDisable(UndoManager manager)
  {
    this.manager = manager;
    manager.Disabled = true;
  }

  public void Dispose()
  {
    manager.Disabled = false;
  }
}