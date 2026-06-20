using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;

namespace SmashTools.Animations
{
  public class AnimationClipEditor : AnimationEditor
  {
    private const float MinLeftWindowSize = 300;
    private const float MinRightWindowSize = 250;

    private const float WidgetBarHeight = 24;
    private const float KeyframeSize = 20;
    private const float FrameInputWidth = 50;
    private const float TabWidth = 110;
    private const float PropertyEntryHeight = 24;
    private const float PropertyBtnWidth = 180;
    private const float ParamCheckboxSize = 20;
    private const float CurveNoWeightDist = 50;
    private const float TangentSlopeMargin = 0.25f;

    private const float FrameBarPadding = 40;
    private const float FrameBarTickRenderingPadding = FrameBarPadding * 2 + 50;
    private const float CollapseFrameDistance = 100;
    private const float MaxFrameZoom = 1000;
    private const float ZoomRate = 0.01f;

    private const int InitialTickInterval = 1;
    private const float InitialCurveTickInterval = 0.1f;
    private const float MaxCurveSpacing = CollapseFrameDistance / InitialCurveTickInterval / 2.5f;

    private const int DefaultFrameCount = 60;
    private const float SecondsPerFrame = 1 / 60f;
    private const float DefaultAxisCount = 100;

    private const float MaxExtraScrollDistance = 5000;

    private readonly Texture2D skipToBeginningTexture =
      ContentFinder<Texture2D>.Get("SmashTools/VideoReturnToBeginning");

    private readonly Texture2D skipToPreviousTexture =
      ContentFinder<Texture2D>.Get("SmashTools/VideoReturnToPrevious");

    private readonly Texture2D skipToNextTexture =
      ContentFinder<Texture2D>.Get("SmashTools/VideoSkipToNext");

    private readonly Texture2D skipToEndTexture =
      ContentFinder<Texture2D>.Get("SmashTools/VideoSkipToEnd");

    private readonly Texture2D animationEventTexture =
      ContentFinder<Texture2D>.Get("SmashTools/AnimationEvent");

    private readonly Texture2D
      keyFrameTexture = ContentFinder<Texture2D>.Get("SmashTools/KeyFrame");

    private readonly Texture2D addAnimationEventTexture =
      ContentFinder<Texture2D>.Get("SmashTools/AddEvent");

    private readonly Texture2D addKeyFrameTexture =
      ContentFinder<Texture2D>.Get("SmashTools/AddKeyFrame");

    private readonly Color propertyExpandedNameColor = new ColorInt(123, 123, 123).ToColor;
    private readonly Color propertyLabelHighlightColor = new ColorInt(255, 255, 255, 10).ToColor;
    private readonly Color itemSelectedColor = new ColorInt(87, 133, 217).ToColor;

    private readonly Color animationEventBarColor = new ColorInt(49, 49, 49).ToColor;
    private readonly Color animationKeyFrameBarColor = new ColorInt(47, 47, 47).ToColor;
    private readonly Color animationKeyFrameBarFadeColor = new ColorInt(40, 40, 40).ToColor;
    private readonly Color curveTopColor = new ColorInt(40, 40, 40, 25).ToColor;
    private readonly Color curveTopFadeColor = new ColorInt(0, 0, 0, 25).ToColor;

    private readonly Color frameTimeBarColor = new ColorInt(40, 64, 75).ToColor;
    private readonly Color frameTimeBarColorDisabled = new ColorInt(10, 10, 10, 100).ToColor;
    private readonly Color frameTickColor = new ColorInt(140, 140, 140).ToColor;
    private readonly Color frameBarHighlightColor = new ColorInt(255, 255, 255, 5).ToColor;
    private readonly Color frameBarHighlightMinorColor = new ColorInt(255, 255, 255, 2).ToColor;
    private readonly Color frameBarHighlightOutlineColor = new ColorInt(68, 68, 68).ToColor;
    private readonly Color frameBarCurveColor = new ColorInt(73, 73, 73).ToColor;
    private readonly Color curveAxisColor = new ColorInt(93, 93, 93).ToColor;

    private readonly Color keyFrameColor = new ColorInt(153, 153, 153).ToColor;
    private readonly Color keyFrameTopColor = new ColorInt(108, 108, 108).ToColor;
    private readonly Color keyFrameHighlightColor = new ColorInt(200, 200, 200).ToColor;

    private readonly Color frameLineMajorDopesheetColor = new ColorInt(75, 75, 75).ToColor;
    private readonly Color frameLineMinorDopesheetColor = new ColorInt(66, 66, 66).ToColor;
    private readonly Color frameLineCurvesColor = new ColorInt(51, 51, 51).ToColor;

    private readonly KeyFrameSelector keyFrameSelector = new();
    private readonly Selector selector = new();

    private readonly List<AnimationPropertyParent> propertiesToRemove =
      new List<AnimationPropertyParent>();

    private readonly HashSet<(int index, int frame)> framesToDraw =
      new HashSet<(int index, int frame)>();

    private readonly HashSet<(int index, int frame)> parentFramesToDraw =
      new HashSet<(int index, int frame)>();

    private AnimationClip animation;
    private float zoomX = 1;
    private float zoomY = 1;

    private int frame = 0;
    private bool isPlaying;
    private int tickInterval = InitialTickInterval;
    private float curveTickInterval = InitialCurveTickInterval;

    private readonly Dictionary<AnimationPropertyParent, bool> propertyExpanded =
      new Dictionary<AnimationPropertyParent, bool>();

    private readonly Dictionary<ParameterInfo, string> inputBuffers =
      new Dictionary<ParameterInfo, string>();

    private Dialog_CameraView previewWindow;

    private float leftWindowSize = MinLeftWindowSize;

    private Vector2 panelScrollPos;
    private float extraPanelWidth;

    private Vector2 frameScrollPos;
    private float extraFrameHeight;

    private float realTimeToTick;

    private KeyFrameDragHandler keyFrameDragger = new KeyFrameDragHandler();

    private Vector2 dragPos;
    private DragItem dragging = DragItem.None;

    private EditTab tab = EditTab.Dopesheet;

    public AnimationClipEditor(Dialog_AnimationEditor parent) : base(parent)
    {
    }

    private bool UnsavedChanges { get; set; }

    private bool MouseOverSelectableArea { get; set; }

    private float ExtraPadding { get; set; }

    public float FrameBarWidth => EditorWidth - FrameBarPadding * 2;

    private int FrameCountShown { get; set; }

    private int Frame
    {
      get { return frame; }
      set
      {
        if (frame == value) return;

        frame = value;
      }
    }

    private int FrameCount
    {
      get
      {
        if (animation == null || animation.frameCount <= 0)
        {
          return DefaultFrameCount;
        }
        return animation.frameCount;
      }
    }

    private float FrameTickMarkSpacing
    {
      get
      {
        float spacing = CollapseFrameDistance /
          Mathf.Lerp(TickInterval / 2f, TickInterval, ZoomFrames % 1f);
        if (TickInterval == InitialTickInterval)
        {
          spacing /=
            2.5f; //Return to being factor of 2, with max frame distance of 250 at 1 tick interval
        }
        return spacing;
      }
    }

    private float CurveAxisSpacing
    {
      get
      {
        float spacing = CollapseFrameDistance /
          Mathf.Lerp(CurveTickInterval / 2f, CurveTickInterval, ZoomCurve % 1f);
        if (CurveTickInterval == InitialCurveTickInterval)
        {
          spacing /=
            2.5f; //Return to being factor of 2, with max frame distance of 250 at 1 tick interval
        }
        return spacing;
      }
    }

    private int TickInterval
    {
      get
      {
        Assert.IsTrue(tickInterval > 0);
        return tickInterval;
      }
    }

    private float CurveTickInterval
    {
      get
      {
        Assert.IsTrue(curveTickInterval > 0);
        return curveTickInterval;
      }
    }

    private float ZoomFrames
    {
      get { return zoomX; }
      set
      {
        if (zoomX != value)
        {
          zoomX = Mathf.Clamp(value, 1, MaxFrameZoom);
          RecalculateTickInterval();
        }
      }
    }

    private float ZoomCurve
    {
      get { return zoomY; }
      set
      {
        if (zoomY != value)
        {
          zoomY = Mathf.Clamp(value, 1, MaxFrameZoom);
          RecalculateCurveTickInterval();
        }
      }
    }

    public float EditorWidth
    {
      get { return FrameTickMarkSpacing * FrameCount + FrameBarPadding * 2; }
    }

    public bool IsPlaying
    {
      get { return isPlaying; }
      private set
      {
        if (isPlaying != value)
        {
          isPlaying = value;
          SoundDefOf.Clock_Stop.PlayOneShotOnCamera();
        }
      }
    }

    private void ChangeMade()
    {
      parent.ChangeMade();
      UnsavedChanges = true;
    }

    public override void AnimatorLoaded(IAnimator animator)
    {
      base.AnimatorLoaded(animator);

      if (previewWindow != null && previewWindow.IsOpen)
      {
        previewWindow.Close();
      }
      if (parent?.animator?.Manager != null)
      {
        previewWindow = new Dialog_CameraView(DisableCameraView, () => animator.DrawPos,
          new Vector2(parent.windowRect.xMax - 50, parent.windowRect.yMax - 50));
        if (animator is Thing thing)
        {
          CameraJumper.TryJump(thing, mode: CameraJumper.MovementMode.Cut);
        }
        CameraView.Start(orthographicSize: CameraView.animationSettings.orthographicSize);
        Find.Selector.ClearSelection();
      }
    }

    public override void Update()
    {
      if (IsPlaying)
      {
        if (Mathf.Abs(Time.deltaTime - SecondsPerFrame) < SecondsPerFrame * 0.1f)
        {
          realTimeToTick += SecondsPerFrame;
        }
        else
        {
          realTimeToTick += Time.deltaTime;
        }
        if (realTimeToTick >= SecondsPerFrame)
        {
          Frame++;
          realTimeToTick -= SecondsPerFrame;
          if (Frame >= FrameCount)
          {
            Frame = 0;
          }
        }
      }

      if (previewWindow != null && previewWindow.IsOpen)
      {
        parent.animator.Manager?.SetFrame(animation, frame);
      }
    }

    public override void OnClose()
    {
      if (previewWindow != null && previewWindow.IsOpen)
      {
        previewWindow.Close();
      }
    }

    public override void OnGUIHighPriority()
    {
      base.OnGUIHighPriority();
      if (KeyBindingDefOf.TogglePause.KeyDownEvent)
      {
        Event.current.Use();
        IsPlaying = !IsPlaying;
      }
      if (!IsPlaying && Event.current != null && Event.current.type == EventType.KeyDown)
      {
        int increment = 1;
        if (ShiftClick) increment = 10;
        if (ControlClick) increment = 100;
        if (Event.current.keyCode == KeyCode.LeftArrow)
        {
          frame -= increment;
        }
        if (Event.current.keyCode == KeyCode.RightArrow)
        {
          frame += increment;
        }
      }
    }

    public override void Save()
    {
      if (animation)
      {
        AnimationLoader.Save(animation);
      }
    }

    public override void CopyToClipboard()
    {
    }

    public override void Paste()
    {
    }

    public override void Escape()
    {
    }

    public override void Delete()
    {
      if (keyFrameSelector.AnyKeyFrameSelected)
      {
        foreach ((AnimationProperty property, int frame) in keyFrameSelector.selPropKeyFrames)
        {
          property.curve.Remove(frame);
        }
        keyFrameSelector.ClearSelectedKeyFrames();
      }
      if (selector.AnySelected<AnimationEvent>())
      {
        animation.events.RemoveAll(selector.IsSelected);
        animation.ValidateEventOrder();
        selector.DeselectAll<AnimationEvent>();
      }
    }

    public override void Draw(Rect rect)
    {
      rect.SplitVertically(leftWindowSize, out Rect leftRect, out Rect rightRect);

      DrawAnimatorSectionLeft(leftRect);
      DrawAnimatorSectionRight(rightRect);
    }

    private void DrawAnimatorSectionLeft(Rect rect)
    {
      DrawBackground(rect);

      if (parent.animator == null)
      {
        DisableGUI(hardDisable: true);
      }


#region TimelineButtons

      if (animation == null)
      {
        DisableGUI();
      }

      bool previewInGame = previewWindow != null && previewWindow.IsOpen;
      string previewLabel = "ST_PreviewAnimation".Translate();
      float width = Text.CalcSize(previewLabel).x;
      Rect toggleRect = new Rect(rect.x, rect.y, width + 20, WidgetBarHeight);
      if (previewWindow == null)
      {
        GUIState.Disable();
      }
      if (ToggleText(toggleRect, previewLabel, "ST_PreviewAnimationTooltip".Translate(),
        previewInGame))
      {
        if (previewInGame)
        {
          previewWindow.Close();
        }
        else
        {
          CameraView.ResetSize();
          Find.WindowStack.Add(previewWindow);
        }
      }
      GUIState.Enable();

      DoSeparatorHorizontal(rect.x, rect.y + WidgetBarHeight, rect.width);

      DoSeparatorHorizontal(rect.x, rect.yMax, rect.width);

      DoSeparatorVertical(rect.xMax, rect.y, rect.height);

      Rect buttonRect = new Rect(toggleRect.xMax, rect.y, WidgetBarHeight, WidgetBarHeight);
      if (AnimationButton(buttonRect, skipToBeginningTexture,
        "ST_SkipFrameBeginningTooltip".Translate()))
      {
        Frame = 0;
      }
      DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
      buttonRect.x += 1;

      buttonRect.x += buttonRect.width;
      if (AnimationButton(buttonRect, skipToPreviousTexture,
        "ST_SkipFramePreviousTooltip".Translate()))
      {
        SkipKeyFrame(-1);
      }
      DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
      buttonRect.x += 1;

      buttonRect.x += buttonRect.width;
      if (AnimationButton(buttonRect, IsPlaying ? CameraView.pauseTexture : CameraView.playTexture,
        IsPlaying ? "ST_PauseAnimationTooltip".Translate() : "ST_PlayAnimationTooltip".Translate()))
      {
        IsPlaying = !IsPlaying;
      }
      DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
      buttonRect.x += 1;

      buttonRect.x += buttonRect.width;
      if (AnimationButton(buttonRect, skipToNextTexture, "ST_SkipFrameNextTooltip".Translate()))
      {
        SkipKeyFrame(1);
      }
      DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
      buttonRect.x += 1;

      buttonRect.x += buttonRect.width;
      if (AnimationButton(buttonRect, skipToEndTexture, "ST_SkipFrameEndTooltip".Translate()))
      {
        Frame = FrameCount;
      }
      DoSeparatorVertical(buttonRect.x, buttonRect.y, buttonRect.height);
      DoSeparatorVertical(buttonRect.xMax, buttonRect.y, buttonRect.height);

      Rect frameNumberRect =
        new Rect(rect.xMax - FrameInputWidth, rect.y, FrameInputWidth, buttonRect.height)
         .ContractedBy(2);
      string nullBuffer = null;
      int tmpFrame = Frame;
      Widgets.TextFieldNumeric(frameNumberRect, ref tmpFrame, ref nullBuffer);
      Frame = tmpFrame;
      CheckTextFieldControlFocus(frameNumberRect);

#endregion TimelineButtons


#region ClipControls

      Rect animButtonRect = new Rect(rect.xMax - buttonRect.height, buttonRect.yMax,
        buttonRect.height, buttonRect.height);
      if (AnimationButton(animButtonRect, addAnimationEventTexture,
        "ST_AddAnimationEvent".Translate()))
      {
        AnimationEvent newEvent = new AnimationEvent();
        newEvent.frame = Frame;
        animation.events.Add(newEvent);
        animation.ValidateEventOrder();
        ChangeMade();
      }
      DoSeparatorVertical(animButtonRect.x, animButtonRect.y, animButtonRect.height);
      animButtonRect.x -= 1;

      animButtonRect.x -= animButtonRect.height;

      if (AnimationButton(animButtonRect, addKeyFrameTexture, "ST_AddKeyFrame".Translate()))
      {
        foreach (AnimationPropertyParent propertyParent in animation.properties)
        {
          AddKeyFramesForParent(propertyParent);
        }
        animation.RecacheFrameCount();
        ChangeMade();
      }

      DoSeparatorVertical(animButtonRect.x, animButtonRect.y, animButtonRect.height);
      animButtonRect.x -= 1;

      EnableGUI(); //Enable so animation clip can be selected

      Rect animClipDropdownRect = new Rect(rect.x, animButtonRect.y, 200, buttonRect.height);
      Rect animClipSelectRect = new Rect(
        parent.windowRect.x + parent.EditorMargin + animClipDropdownRect.x,
        parent.windowRect.y + parent.EditorMargin + animClipDropdownRect.yMax, DropdownWidth, 500);
      string animLabel = animation?.FileName ?? "[No Clip]";
      string animPath = animation?.FilePath ?? string.Empty;
      if (Dropdown(animClipDropdownRect, animLabel, animPath))
      {
        Find.WindowStack.Add(new Dialog_AnimationClipLister(parent.animator, animClipSelectRect,
          animation,
          createItem: ("ST_CreateNewClip", AnimationClip.CreateEmpty),
          onFilePicked: LoadAnimation));
      }
      DoSeparatorVertical(animClipDropdownRect.xMax, animClipDropdownRect.y,
        animClipDropdownRect.height);

      DoSeparatorHorizontal(animClipDropdownRect.x, animClipDropdownRect.yMax, rect.width);

#endregion ClipControls

      Rect leftPanelRect = new Rect(rect.x, animClipDropdownRect.yMax, rect.width,
        rect.height - animClipDropdownRect.yMax);
      if (selector.AnySelected<AnimationEvent>())
      {
        DrawAnimationEventFields(leftPanelRect);
      }
      else
      {
        DrawPropertyEntries(leftPanelRect);
      }

      if (animation == null)
      {
        DisableGUI();
      }

      Rect tabRect = new Rect(rect.xMax - TabWidth - 24, rect.yMax - WidgetBarHeight, TabWidth,
        WidgetBarHeight);
      DoSeparatorHorizontal(tabRect.xMax, tabRect.y, 24);
      if (ToggleText(tabRect, "ST_CurvesTab".Translate(), null, tab == EditTab.Curves))
      {
        FlipTab();
      }
      tabRect.x -= tabRect.width;
      if (ToggleText(tabRect, "ST_DopesheetTab".Translate(), null, tab == EditTab.Dopesheet))
      {
        FlipTab();
      }
      DoSeparatorHorizontal(rect.x, tabRect.y, rect.x + tabRect.x);

      EnableGUI(hardEnable: true);

      DoResizerButton(rect, ref leftWindowSize, MinLeftWindowSize, MinRightWindowSize);

      void FlipTab()
      {
        tab = tab switch
        {
          EditTab.Dopesheet => EditTab.Curves,
          EditTab.Curves    => EditTab.Dopesheet,
          _                 => throw new NotImplementedException(),
        };
        keyFrameDragger.Configure(tab == EditTab.Dopesheet ? KeyframeSize : 0);
      }
    }

    private void SkipKeyFrame(int offset)
    {
      if (animation == null)
      {
        return;
      }
      if (offset == 0)
      {
        return;
      }

      int closestKeyFrame = -1;
      float minDiff = float.MaxValue;
      foreach (AnimationPropertyParent propertyParent in animation.properties)
      {
        foreach (AnimationProperty property in propertyParent)
        {
          foreach (KeyFrame keyFrame in property.curve.points)
          {
            if (offset > 0 && keyFrame.frame <= Frame) //Skip Forward & not at current frame
            {
              continue;
            }
            if (offset < 0 && keyFrame.frame >= Frame) //Skip Backward & not at current frame
            {
              continue;
            }

            float diff = Mathf.Abs(keyFrame.frame - Frame + offset);
            if (diff < minDiff)
            {
              closestKeyFrame = keyFrame.frame;
              minDiff = diff;
            }
          }
        }
      }
      if (closestKeyFrame >= 0)
      {
        Frame = closestKeyFrame;
      }
    }

    private void DrawAnimationEventFields(Rect rect)
    {
      Rect rowRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, PropertyEntryHeight);

      if (selector.GetSelected<AnimationEvent>().FirstOrDefault() is not AnimationEvent
        animationEvent)
      {
        return;
      }

      string label;
      string tooltip = string.Empty;
      if (animationEvent.method?.method == null)
      {
        label = $"[{"ST_NoFunctionSelected".Translate()}]";
      }
      else
      {
        label = Dialog_MethodSelector.MethodName(animationEvent.method.method);
        tooltip = Dialog_MethodSelector.FullMethodSignature(animationEvent.method.method);
      }
      Rect methodDropdownRect = new Rect(rowRect.x, rowRect.y, rowRect.width,
        Mathf.Max(rowRect.height, Text.CalcHeight(label, rowRect.width)));
      if (Dropdown(methodDropdownRect, label, tooltip))
      {
        Rect methodSelectRect = new Rect(parent.windowRect.x + parent.EditorMargin + rowRect.x,
          parent.windowRect.y + parent.EditorMargin + rowRect.yMax, DropdownWidth, 500);
        Find.WindowStack.Add(new Dialog_MethodSelector(parent.animator, methodSelectRect,
          animationEvent, onMethodPicked: AddMethodToEvent));
      }
      rowRect.y = methodDropdownRect.yMax;
      if (animationEvent.method?.method != null)
      {
        ParameterInfo[] parameters = animationEvent.method.method.GetParameters();
        animationEvent.method.args ??= new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
          ParameterInfo parameter = parameters[i];
          bool injectedArg = animationEvent.method.InjectedCount > i;

          object value = null;
          if (!animationEvent.method.args.OutOfBounds(i))
          {
            value = animationEvent.method.args[i];
          }
          Text.Anchor = TextAnchor.MiddleLeft;

          rowRect.SplitVertically(rect.width * 0.3f, out Rect labelRect, out Rect inputRect);
          inputRect.xMin += rect.width * 0.2f;
          Widgets.Label(labelRect, parameter.Name);

          if (injectedArg)
          {
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(inputRect, "ST_Injected".Translate());
          }
          else
          {
            ParameterInput(inputRect, animationEvent, parameter, ref value);
            animationEvent.method.args[i] = value;
          }

          Text.Anchor = TextAnchor.MiddleLeft;

          rowRect.y += PropertyEntryHeight + 5;
        }
      }
    }

    private void ParameterInput(Rect rect, AnimationEvent animationEvent, ParameterInfo parameter,
      ref object value)
    {
      //Checkbox
      if (parameter.ParameterType == typeof(bool))
      {
        Rect checkboxRect =
          new Rect(rect.xMax - rect.height, rect.y, rect.height, rect.height).ContractedBy(
            (24 - ParamCheckboxSize) / 2);
        bool checkOn =
          value == null ?
            false :
            (bool)value; //Might be boxed to null for initial value if arg defaults weren't set
        Widgets.Checkbox(checkboxRect.position, ref checkOn, size: ParamCheckboxSize);
        value = checkOn;
        return;
      }

      //Input field for types parseable as single-line strings
      if (ParseHelper.HandlesType(parameter.ParameterType))
      {
        if (!inputBuffers.ContainsKey(parameter))
        {
          inputBuffers[parameter] = null;
        }
        string buffer = inputBuffers[parameter];
        InputBox(rect, parameter.ParameterType, ref value, ref buffer);
        inputBuffers[parameter] = buffer;
        return;
      }

      //DefNames
      if (parameter.ParameterType.IsSubclassOf(typeof(Def)))
      {
        Text.Anchor = TextAnchor.MiddleCenter;
        Def selectedDef = value as Def;
        string name = selectedDef?.defName ?? "NULL";
        if (Dropdown(rect, name, string.Empty))
        {
          Rect defDropdownRect = new Rect(parent.windowRect.x + parent.EditorMargin + rect.x,
            parent.windowRect.y + parent.EditorMargin + rect.yMax, DropdownWidth, 500);
          Find.WindowStack.Add(new Dialog_DefDropdown(defDropdownRect, parameter.ParameterType,
            (Def def) => SetDefParameter(animationEvent, parameter, def),
            (Def def) => selectedDef == def));
        }
        return;
      }

      //All other types are unsupported, they must be serializable as strings
      //or ResolvedMethod will be unable to parse them as arguments
      Text.Anchor = TextAnchor.MiddleCenter;
      Widgets.Label(rect, "ST_UnsupportedType".Translate());
    }

    private void SetDefParameter(AnimationEvent animationEvent, ParameterInfo settingParameter,
      Def def)
    {
      ParameterInfo[] parameters = animationEvent.method.method.GetParameters();
      for (int i = 0; i < parameters.Length; i++)
      {
        ParameterInfo parameter = parameters[i];
        if (parameter == settingParameter)
        {
          animationEvent.method.args[i] = def;
        }
      }
    }

    private void AddMethodToEvent(MethodInfo method)
    {
      if (selector.GetSelected<AnimationEvent>().FirstOrDefault() is AnimationEvent animationEvent)
      {
        animationEvent.method = new DynamicDelegate(method);
      }
    }

    private void DrawPropertyEntries(Rect rect)
    {
      if (animation == null) return;

      //Add KeyframeSize to keep keyframe bars aligned with their properties
      Rect rowRect = new Rect(rect.x + 5, rect.y + KeyframeSize, rect.width - 10,
        PropertyEntryHeight);
      Rect fullPropertyRect = rowRect;

      float collapseBtnSize = rowRect.height;
      float propertyBtnSize = rowRect.height;
      float expandedIndent = collapseBtnSize;

      bool lightBg = false;
      foreach (AnimationPropertyParent propertyParent in animation.properties)
      {
        if (selector.IsSelected(propertyParent))
        {
          Widgets.DrawBoxSolid(rowRect, itemSelectedColor);
        }
        else if (lightBg)
        {
          Widgets.DrawBoxSolid(rowRect, backgroundLightColor);
        }

        Rect selectParentRect = new Rect(rowRect.x + collapseBtnSize, rowRect.y, rowRect.width -
          PropertyEntryHeight * 3 -
          propertyBtnSize * 2 - collapseBtnSize, PropertyEntryHeight);
        if (Widgets.ButtonInvisible(selectParentRect, doMouseoverSound: false))
        {
          selector.Select(propertyParent, clear: !Input.GetKey(KeyCode.LeftControl));
        }

        Rect collapseBtnRect =
          new Rect(rowRect.x, rowRect.y, collapseBtnSize, collapseBtnSize).ContractedBy(3);
        bool expanded = propertyExpanded.TryGetValue(propertyParent, false);
        if (!propertyParent.IsSingle && UIElements.CollapseButton(collapseBtnRect, ref expanded,
          keyFrameColor,
          keyFrameHighlightColor))
        {
          propertyExpanded[propertyParent] = expanded;

          if (expanded)
          {
            SoundDefOf.TabOpen.PlayOneShotOnCamera();
          }
          else
          {
            SoundDefOf.TabClose.PlayOneShotOnCamera();
          }
        }
        else if (propertyParent.IsSingle)
        {
          float inputBoxWidth = PropertyEntryHeight * 3;
          Rect inputRect = new Rect(rowRect.xMax - collapseBtnSize - inputBoxWidth, rowRect.y,
            inputBoxWidth,
            rowRect.height);
          KeyFrameInput(inputRect, propertyParent.Properties[0]);
        }

        Rect propertyParentBtnRect = new Rect(rowRect.xMax - collapseBtnRect.width, rowRect.y,
          propertyBtnSize,
          propertyBtnSize).ContractedBy(6);
        Color propertyBtnColor = propertyParent.IsSingle && tab == EditTab.Curves ?
          propertyParent.Properties[0].Color :
          keyFrameColor;
        if (Widgets.ButtonImage(propertyParentBtnRect, keyFrameTexture, propertyBtnColor,
          keyFrameHighlightColor))
        {
          List<FloatMenuOption> options = new List<FloatMenuOption>();
          var removePropsOption = new FloatMenuOption("ST_RemoveProperties".Translate(), delegate()
          {
            propertiesToRemove.Add(propertyParent);
            ChangeMade();
          });
          options.Add(removePropsOption);

          var addKeyOption = new FloatMenuOption("ST_AddKey".Translate(), delegate()
          {
            AddKeyFramesForParent(propertyParent);
            ChangeMade();
          });
          addKeyOption.Disabled = propertyParent.AllKeyFramesAt(Frame);
          options.Add(addKeyOption);

          var removeKeyOption = new FloatMenuOption("ST_RemoveKey".Translate(), delegate()
          {
            RemoveKeyFramesForParent(propertyParent);
            ChangeMade();
          });
          removeKeyOption.Disabled = !propertyParent.AnyKeyFrameAt(Frame);
          options.Add(removeKeyOption);

          Find.WindowStack.Add(new FloatMenu(options));
        }

        Rect propertyParentRect = new Rect(collapseBtnRect.xMax, rowRect.y,
          rowRect.width - collapseBtnSize - propertyBtnSize,
          rowRect.height);
        if (Mouse.IsOver(propertyParentRect))
        {
          Widgets.DrawBoxSolid(propertyParentRect, propertyLabelHighlightColor);
        }
        Widgets.Label(propertyParentRect,
          propertyParent.IsIndexer ? propertyParent.LabelWithIdentifier : propertyParent.Label);

        if (expanded)
        {
          foreach (AnimationProperty property in propertyParent.Properties)
          {
            lightBg = !lightBg;
            rowRect.y += rowRect.height;
            fullPropertyRect.height += rowRect.height;

            if (selector.IsSelected(property))
            {
              Widgets.DrawBoxSolid(rowRect, itemSelectedColor);
            }
            else if (lightBg)
            {
              Widgets.DrawBoxSolid(rowRect, backgroundLightColor);
            }

            Rect selectPropertyRect = new Rect(rowRect.x + collapseBtnSize, rowRect.y,
              rowRect.width - PropertyEntryHeight *
              3 - propertyBtnSize * 2 - collapseBtnSize, PropertyEntryHeight);
            if (Widgets.ButtonInvisible(selectPropertyRect, doMouseoverSound: false))
            {
              selector.Select(property, clear: !Input.GetKey(KeyCode.LeftControl));
            }

            Rect propertyBtnRect = new Rect(rowRect.xMax - collapseBtnRect.width, rowRect.y,
                propertyBtnSize, propertyBtnSize)
             .ContractedBy(6);
            if (Widgets.ButtonImage(propertyBtnRect, keyFrameTexture,
              tab == EditTab.Curves ? property.Color : keyFrameColor,
              keyFrameHighlightColor))
            {
              List<FloatMenuOption> options = new List<FloatMenuOption>();
              var removePropsOption = new FloatMenuOption("ST_RemoveProperties".Translate(),
                delegate()
                {
                  propertiesToRemove.Add(propertyParent);
                  ChangeMade();
                });
              options.Add(removePropsOption);

              var addKeyOption = new FloatMenuOption("ST_AddKey".Translate(), delegate()
              {
                float curValue = property.curve[Frame];
                property.curve.Add(Frame, curValue);
                ChangeMade();
              });
              addKeyOption.Disabled = property.curve.KeyFrameAt(Frame);
              options.Add(addKeyOption);

              var removeKeyOption = new FloatMenuOption("ST_RemoveKey".Translate(), delegate()
              {
                property.curve.Remove(Frame);
                ChangeMade();
              });
              removeKeyOption.Disabled = !property.curve.KeyFrameAt(Frame);
              options.Add(removeKeyOption);

              Find.WindowStack.Add(new FloatMenu(options));
            }

            float inputBoxWidth = PropertyEntryHeight * 3;
            Rect inputRect = new Rect(rowRect.xMax - collapseBtnSize - inputBoxWidth, rowRect.y,
              inputBoxWidth, rowRect.height);
            KeyFrameInput(inputRect, property);

            GUI.color = propertyExpandedNameColor;
            Rect propertyRect = new Rect(propertyParentRect.x + expandedIndent, rowRect.y,
              rowRect.width - collapseBtnSize -
              propertyBtnSize, rowRect.height);
            if (Mouse.IsOver(propertyRect))
            {
              Widgets.DrawBoxSolid(propertyRect, propertyLabelHighlightColor);
            }
            Widgets.Label(propertyRect, $"{propertyParent.Label}.{property.Label}");
            GUI.color = Color.white;
          }
        }
        lightBg = !lightBg;
        rowRect.y += rowRect.height;
        fullPropertyRect.height += rowRect.height;
      }
      rowRect.y += PropertyEntryHeight / 2; //Extra padding for add property btn

      RemoveFlaggedProperties();

      Rect propertyButtonRect = new Rect(rect.xMax / 2 - PropertyBtnWidth / 2, rowRect.yMax,
        PropertyBtnWidth, WidgetBarHeight);
      if (ButtonText(propertyButtonRect, "ST_AddProperty".Translate()))
      {
        Vector2 propertyDropdownPosition = new Vector2(
          parent.windowRect.x + parent.EditorMargin + propertyButtonRect.xMax + 2,
          parent.windowRect.y + parent.EditorMargin + propertyButtonRect.y + 1);
        Find.WindowStack.Add(new Dialog_PropertySelect(parent.animator, animation,
          propertyDropdownPosition,
          propertyAdded: InjectKeyFramesNewProperty));
      }

      if (Input.GetMouseButton(0) && !Mouse.IsOver(fullPropertyRect) &&
        !Mouse.IsOver(propertyButtonRect) && Mouse.IsOver(rect))
      {
        selector.DeselectAll<AnimationPropertyParent>();
        selector.DeselectAll<AnimationProperty>();
      }
    }

    private void KeyFrameInput(Rect inputRect, AnimationProperty property)
    {
      inputRect = inputRect.ContractedBy(2);
      string nullBuffer = null;
      switch (property.PropType)
      {
        case AnimationProperty.PropertyType.Float:
        {
          float value = property.curve[Frame];
          float valueBefore = value;
          Widgets.TextFieldNumeric(inputRect, ref value, ref nullBuffer, float.MinValue,
            float.MaxValue);
          if (!Mathf.Approximately(value, valueBefore))
          {
            property.curve.Set(Frame, value);
            animation.RecacheFrameCount();
            ChangeMade();
          }
        }
        break;
        case AnimationProperty.PropertyType.Int:
        {
          int value = Mathf.RoundToInt(property.curve[Frame]);
          int valueBefore = value;
          Widgets.TextFieldNumeric(inputRect, ref value, ref nullBuffer, float.MinValue,
            float.MaxValue);
          if (value != valueBefore)
          {
            property.curve.Set(Frame, value);
            animation.RecacheFrameCount();
            ChangeMade();
          }
        }
        break;
        case AnimationProperty.PropertyType.Bool:
        {
          //bool value = Mathf.Approximately(propertyParent.Single.curve.Evaluate(frame / FrameCount), 1);
          //Widgets.Checkbox(inputBox, ref value, float.MinValue, float.MaxValue);
          animation.RecacheFrameCount();
          ChangeMade();
        }
        break;
      }
      CheckTextFieldControlFocus(inputRect);
    }

    private void DrawAnimatorSectionRight(Rect rect)
    {
      if (parent.animator == null)
      {
        DisableGUI(hardDisable: true);
      }

      Widgets.BeginGroup(rect);

      Rect editorRect = rect.AtZero();

      ExtraPadding = 0;
      if (EditorWidth < editorRect.width)
      {
        ExtraPadding =
          editorRect.width - EditorWidth; //Pad all the way to the edge of the screen if necessary
      }

      FrameCountShown =
        Mathf.CeilToInt((FrameBarWidth + ExtraPadding + extraPanelWidth + FrameBarPadding) /
          FrameTickMarkSpacing);

#region RightPanel

      if (animation == null)
      {
        DisableGUI();
      }

      Rect editorOutRect =
        new Rect(editorRect.x, editorRect.y, editorRect.width, editorRect.height);

      if (dragging == DragItem.None)
      {
        //TryResetExtraScrollSize(editorOutRect, panelScrollPos, editorViewRect, ref extraScrollSize);
      }

      //Area where clicking will select / unselect objects in the animator
      Rect selectableRect = new Rect(editorOutRect.x, editorRect.y + WidgetBarHeight,
        editorOutRect.width, editorOutRect.height - WidgetBarHeight - 16);
      MouseOverSelectableArea = Mouse.IsOver(selectableRect);

      float viewWidth = Mathf.Clamp(EditorWidth, editorRect.width, EditorWidth);
      Vector2 viewRectSize = new Vector2(viewWidth + extraPanelWidth, editorOutRect.height - 16);
      Rect editorViewRect = new Rect(editorOutRect.position, viewRectSize);

      Rect visibleRect = GetVisibleRect(editorOutRect, panelScrollPos, editorViewRect);
      UIElements.BeginScrollView(editorOutRect, ref panelScrollPos, editorViewRect,
        showHorizontalScrollbar: GUI.enabled, showVerticalScrollbar: false);
      Vector2 groupPos = rect.position - visibleRect.position - new Vector2(0, 32);

      //Should still render editor background + frame ticks underneath scrollbar
      //if editorViewRect height is +16 when scrollview is started, it will pad enough to start scrolling
      editorViewRect.height += 16;

      Vector2 panelScrollT = GetScrollPosNormalized(editorOutRect, panelScrollPos, editorViewRect);

      //Frame Bar
      Rect frameBarRect = DrawFrameBar(visibleRect, editorViewRect, panelScrollT);
      Rect animationEventBarRect = new Rect(editorViewRect.x, frameBarRect.yMax,
        editorViewRect.width, WidgetBarHeight);
      DrawAnimationEventMarkers(animationEventBarRect);

#region EditorPanel

      Rect frameOutRect = new Rect(editorOutRect.x, animationEventBarRect.yMax,
        editorViewRect.width, editorOutRect.height);
      Rect frameViewRect = new Rect(frameOutRect.x, frameOutRect.y, frameOutRect.width,
        frameOutRect.height + extraFrameHeight);
      Rect visibleFrameRect = GetVisibleRect(frameOutRect, frameScrollPos, frameViewRect);

      Rect scrollbarRect = new Rect(visibleRect.xMax + GUI.skin.horizontalScrollbar.margin.left,
        frameOutRect.y, GUI.skin.verticalScrollbar.fixedWidth, frameViewRect.height);
      float size = Mathf.Min(visibleRect.height, frameViewRect.height);
      //frameScrollPos.y = GUI.VerticalScrollbar(scrollbarRect, frameScrollPos.y, size, 0f, frameViewRect.height, GUI.skin.verticalScrollbar);
      UIElements.BeginScrollView(frameOutRect, ref frameScrollPos, frameViewRect,
        showHorizontalScrollbar: false, showVerticalScrollbar: true);

      frameViewRect.width += 16;

      Rect blendRect = new Rect(editorViewRect.x, animationEventBarRect.yMax + visibleFrameRect.y,
        editorViewRect.width, fadeHeight);
      switch (tab)
      {
        case EditTab.Dopesheet:
        {
          float blendY = DrawBlend(blendRect, animationKeyFrameBarFadeColor,
            animationKeyFrameBarColor);

          Rect keyFrameTopBarRect = new Rect(frameViewRect.x, blendY, frameViewRect.width,
            KeyframeSize - fadeSize);
          Widgets.DrawBoxSolid(keyFrameTopBarRect, animationKeyFrameBarColor);

          Rect dopeSheetRect = new Rect(frameViewRect.x, keyFrameTopBarRect.yMax,
            frameViewRect.width, frameViewRect.height - keyFrameTopBarRect.yMax);
          DrawBackground(dopeSheetRect);

          DrawDopesheetFrameTicks(dopeSheetRect);

          //Rect dragRect = DragRect(groupPos, snapX: FrameTickMarkSpacing, snapY: PropertyEntryHeight, snapPaddingX: FrameBarPadding);

          DrawKeyFrameMarkers(dopeSheetRect, out bool keyFrameSelected);

          if (DragWindow(dopeSheetRect, DragItem.KeyFrameWindow, button: 2))
          {
            SetDragPos(expandVertical: false);
          }
          if (dragging == DragItem.None && !keyFrameSelected &&
            SelectionBox(groupPos, visibleRect, dopeSheetRect, out Rect dragRect,
              snapX: FrameTickMarkSpacing,
              snapY: PropertyEntryHeight /*, snapPaddingX: FrameBarPadding*/))
          {
          }
        }
        break;
        case EditTab.Curves:
        {
          Rect curveBackgroundRect = new Rect(frameViewRect.x, animationEventBarRect.yMax,
            frameViewRect.width, frameViewRect.height - animationEventBarRect.height);
          DrawBackgroundDark(curveBackgroundRect);
          DrawCurvesFrameTicks(curveBackgroundRect);

          DrawBlend(blendRect, curveTopFadeColor, curveTopColor);

          float yT = GetScrollPosNormalized(editorRect, frameScrollPos, frameViewRect).y;
          Rect curveFrameBarRect = new Rect(visibleRect.x, curveBackgroundRect.y, FrameBarPadding,
            curveBackgroundRect.height);
          DrawAxis(curveFrameBarRect, yT, visibleRect);

          //Rect dragRect = DragRect(groupPos);

          Rect curvesRect = new Rect(frameBarRect.x, 0, FrameBarWidth, curveBackgroundRect.height);
          DrawCurves(curvesRect, visibleRect, groupPos, out bool keyFrameSelected);

          if (DragWindow(curveBackgroundRect, DragItem.KeyFrameWindow, button: 2))
          {
            SetDragPos();
          }
          if (dragging == DragItem.None && !keyFrameSelected &&
            SelectionBox(groupPos, visibleRect, curveBackgroundRect, out Rect dragRect))
          {
          }
        }
        break;
      }
      UIElements.EndScrollView(false);

#endregion EditorPanel

      if (GUI.enabled)
      {
        float frameLinePos = frameBarRect.x + Frame * FrameTickMarkSpacing;
        UIElements.DrawLineVertical(frameLinePos, frameBarRect.y, 2000, Color.white);
      }

      UIElements.EndScrollView(false);

#endregion RightPanel

      void SetDragPos(bool horizontal = true, bool vertical = true, bool expandHorizontal = true,
        bool expandVertical = true)
      {
        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        Vector2 mouseDiff = dragPos - mousePos;
        dragPos = mousePos;

        if (horizontal)
        {
          float xT = GetScrollPosNormalized(editorOutRect, panelScrollPos, editorViewRect).x;

          panelScrollPos.x += mouseDiff.x;

          if (expandHorizontal)
          {
            if (Mathf.Approximately(xT, 1))
            {
              extraPanelWidth += mouseDiff.x;
            }
          }
        }
        if (vertical)
        {
          float yT = GetScrollPosNormalized(frameOutRect, frameScrollPos, frameViewRect).y;

          bool approx0 = Mathf.Approximately(yT, 0);
          bool approx1 = Mathf.Approximately(yT, 1);
          if (expandVertical && (approx0 || (approx1 && mouseDiff.y > 0)))
          {
            extraFrameHeight += mouseDiff.y;
          }
          else
          {
            frameScrollPos.y -= mouseDiff.y;

            if (expandVertical && approx1)
            {
              extraFrameHeight -= mouseDiff.y;
            }
          }
        }

        extraPanelWidth = Mathf.Clamp(extraPanelWidth, 0, MaxExtraScrollDistance);
        extraFrameHeight = Mathf.Clamp(extraFrameHeight, 0, MaxExtraScrollDistance);
      }

      Widgets.EndGroup();

      frameScrollPos.x = panelScrollPos.x;
      panelScrollPos.y = frameScrollPos.y;

      if (GUI.enabled && Mouse.IsOver(rect) && Event.current.type == EventType.ScrollWheel)
      {
        float value = Event.current.delta.y * ZoomRate;

        bool horizontal = Input.GetKey(KeyCode.LeftControl);
        bool vertical = Input.GetKey(KeyCode.LeftShift);
        if (!horizontal && !vertical)
        {
          ZoomFrames += value;
          ZoomCurve += value;
          Event.current.Use();
        }
        else if (horizontal)
        {
          ZoomFrames += value;
          Event.current.Use();
        }
        else if (vertical)
        {
          ZoomCurve += value;
          Event.current.Use();
        }
      }

      EnableGUI(hardEnable: true);
    }

    private Rect DrawFrameBar(Rect visibleRect, Rect viewRect, Vector2 scrollT)
    {
      Color frameBarColor = frameTimeBarColor;
      Color frameBarPaddingColor = frameTimeBarColorDisabled;
      if (!GUI.enabled)
      {
        frameBarColor = backgroundDopesheetColor;
        frameBarPaddingColor = backgroundDopesheetColor;
      }

      //Left padding
      Rect leftFrameBarPadding = new Rect(viewRect.x, viewRect.y, FrameBarPadding, WidgetBarHeight);
      Widgets.DrawBoxSolid(leftFrameBarPadding, frameBarColor);
      Widgets.DrawBoxSolid(leftFrameBarPadding, frameBarPaddingColor);
      UIElements.DrawLineVertical(leftFrameBarPadding.xMax - 1, leftFrameBarPadding.y,
        leftFrameBarPadding.height, frameTickColor);

      float rightPaddingWidth = FrameBarPadding + ExtraPadding + extraPanelWidth;

      //FrameBar
      Rect frameBarRect = new Rect(leftFrameBarPadding.xMax, viewRect.y,
        FrameBarWidth + rightPaddingWidth, WidgetBarHeight);
      DoFrameSlider(visibleRect, frameBarRect, scrollT);

      //Right padding
      Rect rightFrameBarPadding = new Rect(FrameBarWidth + FrameBarPadding, viewRect.y,
        rightPaddingWidth, WidgetBarHeight);
      Widgets.DrawBoxSolid(rightFrameBarPadding, frameTimeBarColorDisabled);
      UIElements.DrawLineVertical(rightFrameBarPadding.x, rightFrameBarPadding.y,
        rightFrameBarPadding.height, frameTickColor);

      DoFrameSliderHandle(frameBarRect);

      DoSeparatorHorizontal(viewRect.x, frameBarRect.yMax, viewRect.width);
      frameBarRect.yMax += 1;

      return frameBarRect;
    }

    private void DoFrameSlider(Rect visibleRect, Rect viewRect, Vector2 scrollT)
    {
      Widgets.DrawBoxSolid(viewRect, frameTimeBarColor);

      Widgets.BeginGroup(viewRect);

      Text.Anchor = TextAnchor.MiddleLeft;
      Text.Font = GameFont.Tiny;
      var color = GUI.color;
      GUI.color = frameTickColor;

      float height = viewRect.height * 0.65f;

      //How many ticks should be rendered in the visible rect
      int ticksShown =
        Mathf.RoundToInt((visibleRect.width + FrameBarTickRenderingPadding) / FrameTickMarkSpacing);
      float startTickUnbound = Mathf.Lerp(-1, FrameCountShown - ticksShown, scrollT.x);
      int startTick = Mathf.RoundToInt(Mathf.Clamp(startTickUnbound, 0, FrameCountShown))
       .RoundTo(TickInterval);
      //Max tick value to render, represents range from start to total w/ padding for backwards looping sub-ticks
      int maxTicksShown = startTick + ticksShown + TickInterval;

      for (int i = startTick; i <= maxTicksShown; i += TickInterval)
      {
        float tickMarkPos = i * FrameTickMarkSpacing;

        float tickHeight;
        if (i % NextTickInterval() == 0)
        {
          tickHeight = height;
        }
        else if (i % TickInterval == 0)
        {
          tickHeight = Mathf.Lerp(height, height / 2, ZoomFrames % 1);
        }
        else
        {
          tickHeight = Mathf.Lerp(height / 2, height / 4, ZoomFrames % 1);
        }

        UIElements.DrawLineVertical(tickMarkPos, viewRect.yMax, -tickHeight, frameTickColor);

        if (i > 0 && TickInterval > 1)
        {
          tickHeight = height / 4;

          float subTickPos;
          int subTickCount = SubTickCount();
          for (int n = 1; n <= subTickCount; n++)
          {
            subTickPos = tickMarkPos -
              ((float)n / (subTickCount + 1) * TickInterval * FrameTickMarkSpacing);
            UIElements.DrawLineVertical(subTickPos, viewRect.yMax, -tickHeight, frameTickColor);
          }
        }

        if (i % TickInterval == 0)
        {
          Rect labelRect = new Rect(tickMarkPos, viewRect.y, FrameTickMarkSpacing * TickInterval,
            viewRect.height).ContractedBy(3);
          Widgets.Label(labelRect, TimeStamp(i));
        }
      }

      GUI.color = color;
      Text.Font = GameFont.Small;

      Widgets.EndGroup();

      DoSeparatorVertical(viewRect.x, viewRect.y, viewRect.height);
      DoSeparatorVertical(viewRect.xMax, viewRect.y, viewRect.height);
    }

    private void DoFrameSliderHandle(Rect rect)
    {
      if (DragWindow(rect, DragItem.FrameBar))
      {
        Frame = FrameAtMousePos(rect);
      }
    }

    private void DrawDopesheetFrameTicks(Rect rect)
    {
      if (!GUI.enabled)
      {
        return;
      }

      Widgets.BeginGroup(rect);
      {
        GUI.color = frameLineMajorDopesheetColor;

        float tickMarkPos;
        for (int i = 0; i <= FrameCountShown; i += TickInterval)
        {
          tickMarkPos = FrameBarPadding + i * FrameTickMarkSpacing;
          UIElements.DrawLineVertical(tickMarkPos, 0, rect.height - 1,
            frameLineMajorDopesheetColor);

          float subTickPos;
          int subTickCount = SubTickCount();
          for (int n = 1; n <= subTickCount; n++)
          {
            subTickPos = tickMarkPos +
              ((float)n / (subTickCount + 1)) * TickInterval * FrameTickMarkSpacing;
            UIElements.DrawLineVertical(subTickPos, 0, rect.height - 1,
              frameLineMinorDopesheetColor);
          }
        }

        GUI.color = Color.white;
      }
      Widgets.EndGroup();
    }

    private bool DisableCameraView()
    {
      if (parent.animator is Thing thing)
      {
        return !thing.Spawned;
      }
      return parent.animator == null;
    }

    private void RemoveFlaggedProperties()
    {
      foreach (AnimationPropertyParent propertyParent in propertiesToRemove)
      {
        animation.properties.Remove(propertyParent);
      }
      propertiesToRemove.Clear();
    }

    private int FrameAtMousePos(Rect rect)
    {
      return Mathf.RoundToInt(Mathf.Clamp(
        (Event.current.mousePosition.x - rect.x) / rect.width * FrameCountShown, 0,
        FrameCountShown));
    }

    private void RecalculateTickInterval()
    {
      int power = Mathf.FloorToInt(ZoomFrames) - 2;
      if (power < 0)
      {
        tickInterval = InitialTickInterval;
        return;
      }
      // 5 * 2^n for even spacing in powers of 5 while remaining scalable for large animations
      int interval = 5 * Ext_Math.PowTwo(power);
      tickInterval = Mathf.Clamp(interval, 5, int.MaxValue);
    }

    private void RecalculateCurveTickInterval()
    {
      int power = Mathf.FloorToInt(ZoomCurve) - 2;
      if (power < 0)
      {
        curveTickInterval = InitialCurveTickInterval;
        return;
      }
      // 0.5 * 2^n for similar scalability as TickInterval, with lower initial values for finer control in animation curves
      float interval = 0.5f * Ext_Math.PowTwo(power);
      curveTickInterval = Mathf.Clamp(interval, 0.5f, float.MaxValue);
    }

    private void LoadAnimation(AnimationClip animationClip)
    {
      propertyExpanded.Clear();
      animation = animationClip;
      if (!animationClip)
      {
        Messages.Message($"Unable to load animation file.", MessageTypeDefOf.RejectInput);
      }
    }

    private string TimeStamp(int frame)
    {
      int seconds = frame / 60;
      int frames = frame % 60;
      return $"{seconds}:{frames:00}";
    }

    private string AxisStamp(float axis)
    {
      return $"{axis:####0.###}";
    }

    private void DrawCurvesFrameTicks(Rect rect)
    {
      if (!GUI.enabled)
      {
        return;
      }
      Widgets.BeginGroup(rect);
      {
        GUI.color = frameLineMajorDopesheetColor;

        float tickMarkPos;
        for (int i = 0; i <= FrameCountShown; i += TickInterval)
        {
          tickMarkPos = FrameBarPadding + i * FrameTickMarkSpacing;
          UIElements.DrawLineVertical(tickMarkPos, 0, rect.height - 1, frameLineCurvesColor);
        }

        GUI.color = Color.white;
      }
      Widgets.EndGroup();
    }

    private void DrawAnimationEventMarkers(Rect rect)
    {
      Widgets.DrawBoxSolid(rect, animationEventBarColor);

      if (!GUI.enabled)
      {
        return;
      }

      bool clickedOutside = Input.GetMouseButtonDown(0);

      foreach (AnimationEvent animationEvent in animation.events)
      {
        GUI.color = selector.IsSelected(animationEvent) ? itemSelectedColor : Color.white;
        Rect eventRect = DopesheetIconRect(rect.y, animationEvent.frame).ContractedBy(5, 3);
        eventRect.y -= 3;

        GUI.DrawTexture(eventRect, animationEventTexture);
        if (Input.GetMouseButtonDown(0) && Mouse.IsOver(eventRect))
        {
          clickedOutside = false;
          selector.Select(animationEvent, clear: SingleSelect);
          inputBuffers.Clear();
        }
        GUI.color = Color.white;
      }
      if (clickedOutside && MouseOverSelectableArea)
      {
        selector.DeselectAll<AnimationEvent>();
      }
    }

    private void DrawKeyFrameMarkers(Rect rect, out bool keyFrameSelected)
    {
      keyFrameSelected = false;

      if (!GUI.enabled)
      {
        return;
      }

      bool clickedOutside = Input.GetMouseButtonDown(0);

      // Handles dragging multiple keyframes together, allowing for batch movement
      Vector4 selectRectBounds =
        new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

      framesToDraw.Clear();
      Rect rowRect = new Rect(rect.x, rect.y, rect.width, PropertyEntryHeight);
      float parentIconY = rowRect.y - KeyframeSize; //Next bar up
      foreach (AnimationPropertyParent propertyParent in animation.properties)
      {
        float propertyIconY = rowRect.y;

        parentFramesToDraw.Clear();
        Widgets.DrawBoxSolidWithOutline(rowRect.ContractedBy(1), frameBarHighlightColor,
          frameBarHighlightOutlineColor);
        if (propertyParent.IsSingle)
        {
          var curve = propertyParent.Properties[0].curve;
          if (curve != null && !curve.points.NullOrEmpty())
          {
            for (int i = 0; i < curve.points.Count; i++)
            {
              KeyFrame keyFrame = curve.points[i];
              framesToDraw.Add((i, keyFrame.frame));
              if (KeyFrameButton(rowRect.y, keyFrame.frame, keyFrameColor,
                keyFrameSelector.IsSelected(propertyParent.Properties[0], keyFrame.frame)))
              {
                keyFrameSelector.SelectFrame(propertyParent, keyFrame.frame);
              }
            }
          }
        }
        else
        {
          Widgets.DrawBoxSolidWithOutline(rowRect.ContractedBy(2), frameBarHighlightColor,
            frameBarHighlightOutlineColor);

          bool expanded = propertyExpanded.TryGetValue(propertyParent, false);
          foreach (AnimationProperty property in propertyParent.Properties)
          {
            if (expanded)
            {
              rowRect.y += rowRect.height;
              Widgets.DrawBoxSolidWithOutline(rowRect.ContractedBy(1), frameBarHighlightMinorColor,
                frameBarHighlightOutlineColor);
            }

            for (int i = 0; i < property.curve.points.Count; i++)
            {
              KeyFrame keyFrame = property.curve.points[i];
              framesToDraw.Add((i, keyFrame.frame));
              parentFramesToDraw.Add((i, keyFrame.frame));
              if (expanded)
              {
                if (KeyFrameButton(rowRect.y, keyFrame.frame, keyFrameColor,
                  keyFrameSelector.IsSelected(property, keyFrame.frame)))
                {
                  keyFrameSelected = true;
                  keyFrameSelector.SelectFrame(property, keyFrame.frame);
                }
              }
            }
          }
        }

        foreach ((int index, int frame) in parentFramesToDraw)
        {
          if (KeyFrameButton(propertyIconY, frame, keyFrameColor,
            keyFrameSelector.IsSelected(propertyParent, frame)))
          {
            keyFrameSelected = true;
            keyFrameSelector.SelectFrame(propertyParent, frame);
          }
        }

        rowRect.y += rowRect.height;
      }

      foreach ((int index, int frame) in framesToDraw)
      {
        if (KeyFrameButton(parentIconY, frame, keyFrameTopColor,
          keyFrameSelector.IsSelected(frame)))
        {
          keyFrameSelected = true;
          keyFrameSelector.SelectAll(animation, frame);
        }
      }

      if (clickedOutside && MouseOverSelectableArea)
      {
        keyFrameSelector.ClearSelectedKeyFrames();
      }

      if (keyFrameSelector.AnyKeyFrameSelected)
      {
        // Converting from (x, y, xMax, yMax) to (x, y, width, height)
        Rect dragRect = new Rect(selectRectBounds.x, selectRectBounds.y,
          selectRectBounds.z - selectRectBounds.x, selectRectBounds.w - selectRectBounds.y);
        if (keyFrameSelector.selPropKeyFrames.Count > 1 && keyFrameSelector.selPropKeyFrames
         .Any(pair => pair.property != keyFrameSelector.selPropKeyFrames[0].property))
        {
          Widgets.DrawBoxSolid(dragRect, selectBoxFillColor);
        }
        // TODO - DragWindow needs better method of 'starting' the drag so it can wait for the threshold to be reached
        if (DragWindow(dragRect, SetDragItem, IsDragging,
          dragStarted: StartDragging, dragStopped: StopDragging))
        {
          Vector2 diff = keyFrameDragger.MouseDiff;

          foreach ((AnimationProperty property, int index) in keyFrameSelector.selPropKeyFrames)
          {
            //int drag_frame = originalKeyFrame.frame + Mathf.RoundToInt(diff.x / FrameTickMarkSpacing);
            //float value = originalKeyFrame.value + diff.y / CurveAxisSpacing;
            //(int frame, float value) = AnimationGraph.ScreenPosToGraphCoord(rect, visibleRect, mousePos, 
            //	property.curve.RangeX, scrollY, CurveAxisSpacing);

            // Equivalent to Mathf.Clamp(0, rect.width / FrameTickMarkSpacing, value)
            // This lets us avoid casting int -> float -> int unless it's actually past max bounds
            //if (drag_frame < 0) drag_frame = 0;
            //else if (drag_frame >= rect.width / FrameTickMarkSpacing) drag_frame = Mathf.FloorToInt(rect.width / FrameTickMarkSpacing);

            //property.curve.points[index] = new KeyFrame(drag_frame, value);
          }
        }

        void SetDragItem()
        {
        }

        bool IsDragging()
        {
          return false;
          //return keyFrameDragger.Contains(()) && dragging == DragItem.KeyFrameHandle;
        }

        void StartDragging()
        {
          keyFrameDragger.Start();
          dragging = DragItem.KeyFrameHandle;
          dragPos = Input.mousePosition;
        }

        void StopDragging()
        {
          //draggingKeyFrame.property = null;
          //draggingKeyFrame.index = -1;
          //dragging = DragItem.None;
          //resort = true;
        }
      }

      bool KeyFrameButton(float y, int frame, Color color, bool selected)
      {
        bool result = false;
        GUI.color = selected ? itemSelectedColor : color;
        Rect keyFrameRect = DopesheetIconRect(y, frame).ContractedBy(4);
        GUI.DrawTexture(keyFrameRect, keyFrameTexture);

        if (selected)
        {
          // Form selection rect based on the area between all selected KeyFrames
          if (keyFrameRect.xMin < selectRectBounds.x) selectRectBounds.x = keyFrameRect.xMin;
          if (keyFrameRect.yMin < selectRectBounds.y) selectRectBounds.y = keyFrameRect.yMin;
          if (keyFrameRect.xMax > selectRectBounds.z) selectRectBounds.z = keyFrameRect.xMax;
          if (keyFrameRect.yMax > selectRectBounds.w) selectRectBounds.w = keyFrameRect.yMax;
        }

        if (Input.GetMouseButtonDown(0) && Mouse.IsOver(keyFrameRect))
        {
          result = true;
          clickedOutside = false;
          if (SingleSelect)
          {
            keyFrameSelector.ClearSelectedKeyFrames();
          }
        }
        GUI.color = Color.white;

        return result;
      }
    }

    private Rect DopesheetIconRect(float y, int frame)
    {
      float tickMarkPos = FrameBarPadding + frame * FrameTickMarkSpacing;
      return new Rect(tickMarkPos - PropertyEntryHeight / 2 + 0.5f, y, PropertyEntryHeight,
        PropertyEntryHeight);
    }

    /// <returns>If any KeyFrame handle is currently being dragged</returns>
    private void DrawCurves(Rect rect, Rect visibleRect, Vector2 groupPos,
      out bool keyFrameSelected)
    {
      keyFrameSelected = false;
      if (!GUI.enabled)
      {
        return;
      }

      if (!selector.AnySelected<AnimationPropertyParent>() &&
        !selector.AnySelected<AnimationProperty>())
      {
        foreach (AnimationPropertyParent propertyParent in animation.properties)
        {
          DrawPropertyParent(propertyParent, ref keyFrameSelected);
        }
      }
      else
      {
        foreach (AnimationPropertyParent propertyParent in selector
         .GetSelected<AnimationPropertyParent>())
        {
          DrawPropertyParent(propertyParent, ref keyFrameSelected);
        }
        foreach (AnimationProperty property in selector.GetSelected<AnimationProperty>())
        {
          DrawProperty(property, ref keyFrameSelected);
        }
      }

      void DrawPropertyParent(AnimationPropertyParent propertyParent, ref bool keyFrameSelected)
      {
        //If neither statements are true, propertyParent is not valid so it shouldn't render anyways
        if (propertyParent.IsSingle)
        {
          DrawProperty(propertyParent.Properties[0], ref keyFrameSelected);
        }
        else if (!propertyParent.Properties.NullOrEmpty())
        {
          foreach (AnimationProperty property in propertyParent.Properties)
          {
            if (!selector.GetSelected<AnimationPropertyParent>().Contains(property))
            {
              DrawProperty(property, ref keyFrameSelected);
            }
          }
        }
      }

      void DrawProperty(AnimationProperty property, ref bool keyFrameSelected)
      {
        AnimationGraph.DrawAnimationCurve(rect, visibleRect, property.curve, property.Color,
          CurveAxisSpacing);
        DragHandle(rect, visibleRect, groupPos, property, ref keyFrameSelected);
      }
    }

    private void DrawAxis(Rect rect, float yT, Rect visibleRect)
    {
      if (!GUI.enabled)
      {
        return;
      }

      Text.Anchor = TextAnchor.LowerRight;
      Text.Font = GameFont.Tiny;
      GUI.color = curveAxisColor;

      DrawAxisTick(0, 0);

      float tick = CurveTickInterval;
      int tickMarks = CurveTickMarks(rect);
      for (int i = 1; i < tickMarks; i++)
      {
        // Position is inverted, UI y axis is top down
        DrawAxisTick(CurveTickInterval * i, -tick);
        DrawAxisTick(-CurveTickInterval * i, tick);
        tick += CurveTickInterval;
      }

      GUI.color = Color.white;
      Text.Font = GameFont.Small;
      Text.Anchor = TextAnchor.UpperLeft;

      void DrawAxisTick(float value, float tick)
      {
        float height = CurveAxisSpacing * CurveTickInterval;

        float tickMarkPos = rect.height / 2 + tick * CurveAxisSpacing;
        UIElements.DrawLineHorizontal(rect.x, tickMarkPos, rect.width, frameBarCurveColor);

        Rect labelUpRect =
          new Rect(rect.x, tickMarkPos - height, rect.width, height).ContractedBy(3);
        Widgets.Label(labelUpRect, AxisStamp(value));
      }
    }

    private int CurveTickMarks(Rect rect)
    {
      return Mathf.CeilToInt(rect.height / (CurveAxisSpacing * CurveTickInterval));
    }

#region DragHandling

    private void DragHandle(Rect rect, Rect visibleRect, Vector2 groupPos,
      AnimationProperty property,
      ref bool keyFrameSelected)
    {
      using var textBlock = new TextBlock(Color.white);

      bool resort = false;
      List<KeyFrame> points = property.curve.points;
      for (int i = 0; i < points.Count; i++)
      {
        KeyFrame keyFrame = points[i];
        int x = keyFrame.frame;
        float y = property.curve[x];

        Vector2 dragHandlePos = AnimationGraph.GraphCoordToScreenPos(rect, new Vector2(x, y),
          property.curve.RangeX, CurveAxisSpacing);
        float size = keyFrameSelector.IsSelected(property, x) ?
          AnimationGraph.DragHandleSize * 1.25f :
          AnimationGraph.DragHandleSize;
        Rect texRect = new Rect(dragHandlePos.x - size / 2,
          dragHandlePos.y - size / 2,
          size, size);

        GUI.color = Mouse.IsOver(texRect) ?
          property.Color.AddNoAlpha(0.1f, 0.1f, 0.1f) :
          property.Color;
        GUI.DrawTexture(texRect, keyFrameTexture);
        GUI.color = keyFrameSelector.IsSelected(property, x) ? Color.white : Color.black;
        GUI.DrawTexture(texRect.ContractedBy(AnimationGraph.DragHandleSize / 4), keyFrameTexture);
        GUI.color = Color.white;

        // Size = 20x20 with the same centering as texRect
        Rect handleRect = new Rect(texRect.center.x, texRect.center.y, 0, 0).ExpandedBy(10);
        if (LeftClickDown && Mouse.IsOver(handleRect))
        {
          keyFrameSelector.SelectFrame(property, keyFrame.frame);
          keyFrameSelected = true;
          Event.current.Use();
        }

        if (keyFrameSelector.IsSelected(property, keyFrame.frame))
        {
          if (!points.OutOfBounds(i - 1))
          {
            DrawTangentHandle(texRect, dragHandlePos, property, i, false);
          }
          if (!points.OutOfBounds(i + 1))
          {
            DrawTangentHandle(texRect, dragHandlePos, property, i, true);
          }
        }

        //if (DragWindow(texRect, ref dragPos, SetDragItem, IsDragging, 
        //	dragStarted: StartDragging, dragStopped: StopDragging) && Matches(property, i))
        //{
        //	Vector2 diff = keyFrameDragger.MouseDiff;
        //	float scrollY = frameScrollPos.y;

        //	int frame = originalKeyFrame.frame + Mathf.RoundToInt(diff.x / FrameTickMarkSpacing);
        //	float value = originalKeyFrame.value + diff.y / CurveAxisSpacing;
        //	//(int frame, float value) = AnimationGraph.ScreenPosToGraphCoord(rect, visibleRect, mousePos, 
        //	//	property.curve.RangeX, scrollY, CurveAxisSpacing);

        //	// Equivalent to Mathf.Clamp(0, rect.width / FrameTickMarkSpacing, value)
        //	// This lets us avoid casting int -> float -> int unless it's actually past max bounds
        //	if (frame < 0) frame = 0;
        //	else if (frame >= rect.width / FrameTickMarkSpacing) frame = Mathf.FloorToInt(rect.width / FrameTickMarkSpacing);

        //	property.curve.points[i] = new KeyFrame(frame, value);
        //}

        //void SetDragItem()
        //{
        //}

        //bool IsDragging()
        //{
        //	return Matches(property, i) && dragging == DragItem.KeyFrameHandle;
        //}

        //void StartDragging()
        //{
        //	originalKeyFrame = (property.curve.points[i].frame, property.curve.points[i].value);
        //	keyFrameDragPos = Input.mousePosition;

        //	draggingKeyFrame.Add((property, i));
        //	dragging = DragItem.KeyFrameHandle;
        //}

        //void StopDragging()
        //{
        //	draggingKeyFrame.Clear();
        //	dragging = DragItem.None;
        //	resort = true;
        //}
      }

      //bool Matches(AnimationProperty property, int index)
      //{
      //	return keyFrameDragger.Contains(property, index);
      //}

      void DrawTangentHandle(Rect texRect, Vector2 dragHandlePos, AnimationProperty property, int i,
        bool forward)
      {
        KeyFrame keyFrame = points[i];
        float weight = forward ? keyFrame.outWeight : keyFrame.inWeight;
        float tangent = forward ? keyFrame.outTangent : keyFrame.inTangent;
        float value = property.curve[keyFrame.frame];
        Vector2 otherDragHandlePos = AnimationGraph.GraphCoordToScreenPos(rect,
          new Vector2(keyFrame.frame, value),
          property.curve.RangeX, CurveAxisSpacing);
        Rect prevRect = new Rect(otherDragHandlePos.x - AnimationGraph.DragHandleSize / 2,
          otherDragHandlePos.y - AnimationGraph.DragHandleSize / 2,
          AnimationGraph.DragHandleSize, AnimationGraph.DragHandleSize);

        float distance = forward ? CurveNoWeightDist : -CurveNoWeightDist;
        if ((keyFrame.weightedMode == WeightedMode.In && !forward) ||
          (keyFrame.weightedMode == WeightedMode.Out && forward) ||
          keyFrame.weightedMode == WeightedMode.Both)
        {
          distance = Mathf.Lerp(dragHandlePos.x, otherDragHandlePos.x, weight);
        }

        float s2 = tangent * tangent;
        // { d / sqrt(1 + s^2), (s * d) / sqrt(1 + s^2) }
        float dx = distance / Mathf.Sqrt(1 + s2);
        Vector2 tangentPos = new Vector2(dx + dragHandlePos.x, -tangent * dx + dragHandlePos.y);

        Rect tangentRect = new Rect(tangentPos - texRect.size / 2, texRect.size);
        Widgets.DrawLine(texRect.center, tangentRect.center, Color.white, 0.5f);
        GUI.DrawTexture(tangentRect.ContractedBy(2), keyFrameTexture);

        if (DragWindow(tangentRect.ExpandedBy(2), OnTangentDrag, IsTangentDrag,
          dragStarted: OnStartTangentDrag, dragStopped: OnStopTangentDrag, button: 0))
        {
          Vector2 mousePos = MouseUIPos(groupPos - visibleRect.position);
          float num = mousePos.y - texRect.center.y;
          float denom = mousePos.x - texRect.center.x;
          float s;
          if (Mathf.Abs(denom) < TangentSlopeMargin)
          {
            s = num > 0 ? float.PositiveInfinity : float.NegativeInfinity;
          }
          else
          {
            s = num / denom;
          }
          // Defaulted to non-broken free-smooth, will add presets later
          points[i] = new KeyFrame(keyFrame.frame, keyFrame.value, -s, -s);
        }

        void OnTangentDrag()
        {
          Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
          Vector2 diff = dragPos - mousePos;
          dragPos = mousePos;
        }

        bool IsTangentDrag()
        {
          return keyFrameSelector.draggingTangent.property == property &&
            keyFrameSelector.draggingTangent.index == i &&
            keyFrameSelector.draggingTangent.forward == forward;
        }

        void OnStartTangentDrag()
        {
          dragging = DragItem.TangentHandle;
          keyFrameSelector.draggingTangent = (property, i, forward);
        }

        void OnStopTangentDrag()
        {
          dragging = DragItem.None;
          keyFrameSelector.draggingTangent = (null, 0, false);
        }
      }

      if (resort) property.curve.points.Sort();
    }

    private bool DragWindow(Rect rect, DragItem dragItem, int button = 0)
    {
      return DragWindow(rect, SetDragItem, IsDragging,
        dragStarted: StartDragging, dragStopped: StopDragging, button: button);

      void SetDragItem()
      {
        dragging = dragItem;
      }

      bool IsDragging()
      {
        return dragging == dragItem;
      }

      void StartDragging()
      {
        dragging = dragItem;
        dragPos = Input.mousePosition;
      }

      void StopDragging()
      {
        dragging = DragItem.None;
      }
    }

#endregion DragHandling

    private void InjectKeyFramesNewProperty(AnimationPropertyParent propertyParent)
    {
      foreach (AnimationProperty property in propertyParent.Properties)
      {
        Inject(property);
      }

      void Inject(AnimationProperty property)
      {
        property.curve.Set(0, 0);
        property.curve.Set(FrameCount, 0);
        ChangeMade();
      }
    }

    private void AddKeyFramesForParent(AnimationPropertyParent propertyParent)
    {
      foreach (AnimationProperty property in propertyParent)
      {
        float curValue = property.curve[Frame];
        property.curve.Add(Frame, curValue);
      }
      ChangeMade();
    }

    private void RemoveKeyFramesForParent(AnimationPropertyParent propertyParent)
    {
      foreach (AnimationProperty property in propertyParent.Properties)
      {
        property.curve.Remove(Frame);
      }
      ChangeMade();
    }

    private int SubTickCount()
    {
      if (TickInterval == 1)
      {
        return 0;
      }
      else if (TickInterval == 5)
      {
        return 4;
      }
      return 9;
    }

    private int NextTickInterval()
    {
      if (TickInterval == 1)
      {
        return 5;
      }
      return TickInterval * 2;
    }

    private enum DragItem
    {
      None,
      FrameBar,
      KeyFrameWindow,
      KeyFrameHandle,
      TangentHandle
    }

    private enum EditTab
    {
      Dopesheet,
      Curves
    }

    private class KeyFrameSelector
    {
      public (AnimationProperty property, int index, bool forward) draggingTangent;

      public List<(AnimationProperty property, int frame)> selPropKeyFrames =
        new List<(AnimationProperty property, int frame)>();

      public bool AnyKeyFrameSelected => selPropKeyFrames.Count > 0;

      public bool IsSelected(int frame)
      {
        return selPropKeyFrames.Any(selection => selection.frame == frame);
      }

      public bool IsSelected(AnimationPropertyParent propertyParent, int frame)
      {
        if (propertyParent == null || !propertyParent.IsValid)
        {
          return false;
        }
        foreach (AnimationProperty property in propertyParent.Properties)
        {
          if (IsSelected(property, frame))
          {
            return true;
          }
        }
        return false;
      }

      public bool IsSelected(AnimationProperty property, int frame)
      {
        if (property == null || !property.IsValid)
        {
          return false;
        }
        return selPropKeyFrames.Contains((property, frame));
      }

      public void SelectAll(AnimationClip clip, int frame)
      {
        if (!Input.GetKey(KeyCode.LeftControl))
        {
          ClearSelectedKeyFrames();
        }
        foreach (AnimationPropertyParent propertyParent in clip.properties)
        {
          SelectFrame(propertyParent, frame, clear: false);
        }
      }

      public void SelectFrame(AnimationPropertyParent propertyParent, int frame, bool clear = true)
      {
        if (clear && !Input.GetKey(KeyCode.LeftControl))
        {
          ClearSelectedKeyFrames();
        }
        foreach (AnimationProperty property in propertyParent.Properties)
        {
          SelectFrame(property, frame, clear: false, sort: false);
        }
        selPropKeyFrames.SortBy(selProp => selProp.frame);
      }

      public void SelectFrame(AnimationProperty property, int frame, bool clear = true,
        bool sort = true)
      {
        if (clear && !Input.GetKey(KeyCode.LeftControl))
        {
          ClearSelectedKeyFrames();
        }
        if (selPropKeyFrames.Contains((property, frame)))
        {
          return;
        }
        selPropKeyFrames.Add((property, frame));
      }

      public void ClearSelectedKeyFrames()
      {
        selPropKeyFrames.Clear();
      }
    }

    private class KeyFrameDragHandler
    {
      // (property, index), originalValue
      private readonly Dictionary<(AnimationProperty property, int index), (int frame, float value)>
        innerContainer =
          new Dictionary<(AnimationProperty property, int index), (int frame, float value)>();

      private Vector2 clickPos;

      private float dragThreshold = 0;

      public Vector2 ClickPos => clickPos;

      public Vector2 MouseDiff =>
        new Vector2(Input.mousePosition.x, Input.mousePosition.y) - ClickPos;

      public bool ReachedThreshold { get; private set; }

      private bool Dragging { get; set; }

      public void Update()
      {
        if (!Dragging)
        {
          ReachedThreshold = false;
          if (Input.GetMouseButtonDown(0))
          {
            clickPos = Input.mousePosition;
          }
          if (Input.GetMouseButton(0) &&
            Mathf.Abs(clickPos.magnitude - Input.mousePosition.magnitude) >= dragThreshold)
          {
            // Once started, only canceling the drag will reset
            ReachedThreshold = true;
          }
        }
      }

      public void Configure(float dragThreshold)
      {
        this.dragThreshold = dragThreshold;
      }

      public void Start()
      {
        Dragging = true;
      }

      public void Clear()
      {
        clickPos = Vector2.zero;
        innerContainer.Clear();
        ReachedThreshold = false;
        Dragging = false;
      }

      public void Record(AnimationProperty property, int index, int frame, float originalValue)
      {
        innerContainer[(property, index)] = (frame, originalValue);
      }

      public bool Contains(AnimationProperty property, int index)
      {
        return innerContainer.ContainsKey((property, index));
      }

      public bool TryGetOriginalValue(AnimationProperty property, int index, out int frame,
        out float originalValue)
      {
        frame = -1;
        originalValue = float.MinValue;
        if (innerContainer.TryGetValue((property, index), out var cache))
        {
          frame = cache.frame;
          originalValue = cache.value;
          return true;
        }
        return false;
      }
    }
  }
}