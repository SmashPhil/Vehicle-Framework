using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Animations
{
  [StaticConstructorOnStartup]
  public class Dialog_AnimationEditor : Window, IHighPriorityOnGUI
  {
    private List<TabRecord> tabs = new List<TabRecord>();
    private DialogTab dialogTab = DialogTab.Controller;

    public IAnimator animator;

    public AnimationLayer animLayer;
    public AnimationController controller;

    private AnimationControllerEditor controllerEditor;
    private AnimationClipEditor clipEditor;

    //IMGUI
    //public static readonly int s_SliderHash;

    static Dialog_AnimationEditor()
    {
      //s_SliderHash = (int)AccessTools.Field(typeof(GUI), nameof(s_SliderHash)).GetValue(null);
    }

    public Dialog_AnimationEditor(IAnimator animator)
    {
      SetWindowProperties();
      InitializeTabs();
      Dialog_MethodSelector.InitStaticEventMethods();

      this.animator = animator;
      controllerEditor = new AnimationControllerEditor(this);
      clipEditor = new AnimationClipEditor(this);
    }

    private AnimationEditor ActiveTab
    {
      get
      {
        return dialogTab switch
        {
          DialogTab.Animator   => clipEditor,
          DialogTab.Controller => controllerEditor,
          _                    => throw new NotImplementedException(),
        };
      }
    }

    private bool UnsavedChanges { get; set; }

    public float EditorMargin => base.Margin;

    public override Vector2 InitialSize
    {
      get { return new Vector2(UI.screenWidth * 0.75f, UI.screenHeight * 0.75f); }
    }

    public override void PostOpen()
    {
      base.PostOpen();
      LoadAnimator(animator);
    }

    private void SetWindowProperties()
    {
      this.resizeable = true;
      this.doCloseX = true;
      this.closeOnAccept = false;
      this.closeOnClickedOutside = false;
      this.closeOnCancel = false;
      this.absorbInputAroundWindow = false;
      this.preventCameraMotion = true;
      //this.forcePause = true;
    }

    public void ChangeMade()
    {
      UnsavedChanges = true;
    }

    private void LoadAnimator(IAnimator animator)
    {
      if (CameraView.InUse)
      {
        CameraView.Close();
      }
      this.animator = animator;
      controller = animator.Manager?.controller;
      if (!controller || controller.layers.NullOrEmpty())
      {
        controller = AnimationController.EmptyController();
      }
      Assert.IsNotNull(controller);

      animLayer = controller.layers.FirstOrDefault();
      Assert.IsNotNull(animLayer);

      controllerEditor.AnimatorLoaded(animator);
      clipEditor.AnimatorLoaded(animator);
    }

    public override void PostClose()
    {
      base.PostClose();
      CameraView.Close();
      controllerEditor.OnClose();
      clipEditor.OnClose();
    }

    public override void WindowUpdate()
    {
      base.WindowUpdate();
      controllerEditor.Update();
      clipEditor.Update();
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
          Find.WindowStack.Add(new Dialog_Confirm($"You have unsaved changes. Close anyways?",
            delegate() { Close(); }));
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
      tabs = new List<TabRecord>();
      tabs.Add(new TabRecord("ST_ControllerWindow".Translate(), delegate()
      {
        dialogTab = DialogTab.Controller;
        ActiveTab.OnTabOpen();
      }, () => dialogTab == DialogTab.Controller));
      tabs.Add(new TabRecord("ST_AnimationWindow".Translate(), delegate()
      {
        dialogTab = DialogTab.Animator;
        ActiveTab.OnTabOpen();
      }, () => dialogTab == DialogTab.Animator));
    }

    public override void DoWindowContents(Rect inRect)
    {
      ResetControlFocus();

      using (new TextBlock(GameFont.Small))
      {
        Rect tabRect = new Rect(inRect.x, inRect.y + TabDrawer.TabHeight, inRect.width, 32);
        TabDrawer.DrawTabs(tabRect, tabs);
        inRect.yMin += tabRect.height;

        GUI.enabled = animator != null;
        {
          ActiveTab.Draw(inRect);
        }
        GUI.enabled = true;
      }
    }

    private void ResetControlFocus()
    {
      if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return ||
        Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Escape))
      {
        UI.UnfocusCurrentControl();
      }
    }

    private enum DialogTab
    {
      Animator,
      Controller
    }
  }
}