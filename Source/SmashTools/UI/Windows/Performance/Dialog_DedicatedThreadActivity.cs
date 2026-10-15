using System;
using System.Collections.Generic;
using CoreLib.Performance;
using UnityEngine;
using Verse;

namespace SmashTools;

public class Dialog_DedicatedThreadActivity : Window
{
  private const float AsyncActionEntryHeight = 30;
  private const int QueueLimit = 50;

  private DedicatedThread dedicatedThread;
  private Func<DedicatedThread> dedicatedThreadGetter;

  private List<AsyncAction> actionsSnapshot = [];


  private Vector2 scrollPos;
  private Rect viewRect;

  public Dialog_DedicatedThreadActivity(DedicatedThread dedicatedThread)
  {
    this.dedicatedThread = dedicatedThread;

    SetWindowProperties();
  }

  public Dialog_DedicatedThreadActivity(Func<DedicatedThread> dedicatedThreadGetter)
  {
    this.dedicatedThreadGetter = dedicatedThreadGetter;

    SetWindowProperties();
  }

  public override Vector2 InitialSize => new Vector2(600, 400);

  public DedicatedThread DedicatedThread
  {
    get
    {
      if (dedicatedThread != null)
      {
        return dedicatedThread;
      }
      else if (dedicatedThreadGetter != null)
      {
        return dedicatedThreadGetter();
      }

      return null;
    }
  }

  private void SetWindowProperties()
  {
    this.resizeable = true;
    this.doCloseX = true;
    this.closeOnClickedOutside = false;
    this.draggable = true;
    this.absorbInputAroundWindow = false;
    this.preventCameraMotion = false;
  }

  public override void PostOpen()
  {
    base.PostOpen();
  }

  private void RecalculateViewRect(Rect rect, int entryCount)
  {
    viewRect = rect;
    viewRect.height = entryCount * AsyncActionEntryHeight;
  }

  public override void DoWindowContents(Rect inRect)
  {
    DedicatedThread dedicatedThread = DedicatedThread;
    if (dedicatedThread == null) return;

    using TextBlock textFont = new(GameFont.Medium);

    Rect labelRect = inRect.ContractedBy(5);
    labelRect.height = 32;

    actionsSnapshot.Clear();
    dedicatedThread.Snapshot(actionsSnapshot);
    int count = Mathf.Min(actionsSnapshot.Count, QueueLimit);

    Widgets.Label(labelRect,
      $"DedicatedThread #{dedicatedThread.id} (Count={actionsSnapshot.Count})");

    Text.Font = GameFont.Small;

    Rect activityRect = inRect;
    activityRect.yMin = labelRect.yMax + 5;
    activityRect.height -= 5;
    Widgets.DrawMenuSection(activityRect);

    Rect outRect = activityRect.ContractedBy(2);

    // Begin ScrollView
    Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
    for (int i = 0; i < count; i++)
    {
      AsyncAction asyncAction = actionsSnapshot[i];
      Rect entryRect = viewRect;
      entryRect.y = i * AsyncActionEntryHeight;
      entryRect.height = AsyncActionEntryHeight;

      Widgets.Label(entryRect, $"{i}. {asyncAction.GetType().Name}");
      Widgets.DrawLineHorizontal(entryRect.x, entryRect.yMax, entryRect.width);
    }

    Widgets.EndScrollView();
    // End ScrollView

    RecalculateViewRect(outRect, count);
  }
}