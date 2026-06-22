using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace AnimationKit.Editor;

[StaticConstructorOnStartup]
public class Dialog_AnimationEditor : Window, IHighPriorityOnGUI
{
  private List<TabRecord> tabs = [];
  private DialogTab dialogTab = DialogTab.Controller;

  public AnimationLayer curLayer;
  public AnimationController curController;

  //private AnimationControllerEditor controllerEditor;
  private AnimationClipEditor clipEditor;

  private enum DialogTab
  {
    Animator,
    Controller
  }

  public Dialog_AnimationEditor()
  {
    SetWindowProperties();
    InitializeTabs();
    //Dialog_MethodSelector.InitStaticEventMethods();

    //controllerEditor = new AnimationControllerEditor(this);
    clipEditor = new AnimationClipEditor(this);
  }

  private AnimationEditor ActiveTab
  {
    get
    {
      return dialogTab switch
      {
        DialogTab.Animator => clipEditor,
        DialogTab.Controller => clipEditor /*controllerEditor*/,
        _ => throw new NotImplementedException(),
      };
    }
  }

  private bool UnsavedChanges { get; set; }

  public override Vector2 InitialSize => new(UI.screenWidth * 0.75f, UI.screenHeight * 0.75f);

  public override void PostOpen()
  {
    base.PostOpen();
  }

  private void SetWindowProperties()
  {
    resizeable = true;
    doCloseX = true;
    closeOnAccept = false;
    closeOnClickedOutside = false;
    closeOnCancel = false;
    absorbInputAroundWindow = false;
    preventCameraMotion = true;
    //this.forcePause = true;
  }

  public void ChangeMade()
  {
    UnsavedChanges = true;
  }

  public override void PostClose()
  {
    base.PostClose();
    CameraView.Close();
    //controllerEditor.OnClose();
    //clipEditor.OnClose();
  }

  public override void WindowUpdate()
  {
    base.WindowUpdate();
    //controllerEditor.Update();
    //clipEditor.Update();
  }

  public void OnGUIHighPriority()
  {
    if (Input.GetKeyDown(KeyCode.F))
    {
      ActiveTab.ResetToCenter();
    }
    if (KeyBindingDefOf.Cancel.KeyDownEvent)
    {
      Event.current.Use();
      if (UnsavedChanges)
      {
        Find.WindowStack.Add(new Dialog_Confirm("You have unsaved changes. Close anyways?",
          () => Close()));
      }
      else
      {
        Close();
      }
    }
    ActiveTab.OnGUIHighPriority();
  }

  private void InitializeTabs()
  {
    tabs =
    [
      new TabRecord("ST_ControllerWindow".Translate(), delegate
      {
        dialogTab = DialogTab.Controller;
        ActiveTab.OnTabOpen();
      }, () => dialogTab == DialogTab.Controller),
      new TabRecord("ST_AnimationWindow".Translate(), delegate
      {
        dialogTab = DialogTab.Animator;
        ActiveTab.OnTabOpen();
      }, () => dialogTab == DialogTab.Animator)
    ];
  }

  public override void DoWindowContents(Rect inRect)
  {
    ResetControlFocus();

    using TextBlock block = new(GameFont.Small);
    Rect tabRect = new(inRect.x, inRect.y + TabDrawer.TabHeight, inRect.width, TabDrawer.TabHeight);
    TabDrawer.DrawTabs(tabRect, tabs);
    inRect.yMin += tabRect.height;
    ActiveTab.DoWindowContents(inRect);
  }

  private static void ResetControlFocus()
  {
    if (Event.current is not { type: EventType.KeyDown } ev)
      return;

    if (ev.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape)
    {
      UI.UnfocusCurrentControl();
    }
  }

  [DebugAction]
  private static void OpenAnimator()
  {
    Find.WindowStack.Add(new Dialog_AnimationEditor());
  }
}