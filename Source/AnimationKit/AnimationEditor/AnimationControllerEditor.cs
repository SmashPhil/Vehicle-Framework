using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Verse.Sound;
using ParamType = SmashTools.Animations.AnimationParameter.ParamType;
using StateType = SmashTools.Animations.AnimationState.StateType;

namespace SmashTools.Animations
{
  public class AnimationControllerEditor : AnimationEditor
  {
    private const float MinLeftWindowSize = TabWidth * 2 + TopBarIconSize;
    private const float MinRightWindowSize = 300;
    private const int GridSize = 150;

    private const float WidgetBarHeight = 24;
    private const float LayerItemHeight = 38;
    private const float LayerInputWidth = 75;
    private const float ParameterItemHeight = 32;
    private const float ParameterInputWidth = 100;
    private const float TransitionLineWidth = 2;
    private const float TransitionAnchorSpacing = 5;
    private const float TabWidth = 110;
    public const int GridSquareSize = 20;
    private const float TopBarIconSize = WidgetBarHeight;
    private const float HighlightPadding = 2;

    public const int StateWidth = 16;
    private const int StateHeight = 4;
    private const int StateHeightSmall = 3;

    private const float ZoomRate = 0.03f;

    private readonly Texture2D stateTex = ContentFinder<Texture2D>.Get("SmashTools/Square");
    private readonly Texture2D stateMachineTex = ContentFinder<Texture2D>.Get("SmashTools/Hexagon");
    private readonly Texture2D eyeTex = ContentFinder<Texture2D>.Get("SmashTools/Eye");

    private readonly Texture2D
      eyeStrikedTex = ContentFinder<Texture2D>.Get("SmashTools/EyeStriked");

    private readonly Color lineDarkColor = new ColorInt(25, 25, 25).ToColor;
    private readonly Color lineLightColor = new ColorInt(35, 35, 35).ToColor;

    private readonly Color topBarFadeColor = new ColorInt(40, 40, 40).ToColor;
    private readonly Color entryStateColor = new ColorInt(20, 110, 50).ToColor;
    private readonly Color defaultStateColor = new ColorInt(185, 105, 25).ToColor;
    private readonly Color exitStateColor = new ColorInt(150, 25, 25).ToColor;
    private readonly Color anyStateColor = new ColorInt(90, 160, 140).ToColor;
    private readonly Color stateColor = new ColorInt(75, 75, 75).ToColor;

    private readonly Color highlightColor = Widgets.HighlightStrongBgColor.ToTransparent(0.75f);

    private readonly Selector selector = new Selector();
    private readonly Clipboard clipboard = new Clipboard();
    private readonly QuickSearchFilter parameterFilter = new QuickSearchFilter();

    private readonly Vector2 gridSize =
      new Vector2(GridSize * GridSquareSize, GridSize * GridSquareSize);

    private bool hideLeftWindow = false;
    private float leftWindowSize = MinLeftWindowSize;
    private bool initialized = false;

    private float zoom = 2;
    private Vector2 scrollPos;
    private Vector2 dragPos;

    private DragItem dragging;
    private LeftSection leftSectionTab = LeftSection.Layers;

    private IntVec2 mouseGridPos;
    private IntVec2 clickedGridPos;

    private IntVec2 draggingStateOrigPos;
    private bool draggingState;
    private AnimationState makingTransitionFrom;

    // Input
    private string motionSpeedBuffer;
    private string cycleOffsetBuffer;

    public AnimationControllerEditor(Dialog_AnimationEditor parent) : base(parent)
    {
      scrollPos = new Vector2(gridSize.x / 2f, gridSize.y / 2f);
    }

    private bool UnsavedChanges { get; set; }

    private AnimationLayer EditingLayer { get; set; }

    private AnimationParameter EditingParameter { get; set; }

    private bool HideLeftWindow
    {
      get { return hideLeftWindow; }
      set
      {
        if (value == hideLeftWindow) return;

        if (value) SoundDefOf.TabClose.PlayOneShotOnCamera();
        else SoundDefOf.TabOpen.PlayOneShotOnCamera();

        hideLeftWindow = value;
      }
    }

    private Vector2 MousePosLeftWindowAdjust
    {
      get
      {
        // (TODO) +8 to direction as temporary measure to keep mouse + selection box
        // aligned. The selection box is actually being rendered 8 pixels to the left
        Vector2 windowAdjust = new Vector2(HideLeftWindow ? 0 : leftWindowSize + 8,
          WidgetBarHeight + TabDrawer.TabHeight);
        return windowAdjust;
      }
    }

    private GameFont Font
    {
      get
      {
        if (zoom < 1.5f) return GameFont.Medium;
        if (zoom < 2) return GameFont.Small;
        return GameFont.Tiny;
      }
    }

    private float MaxZoom(Rect rect) => gridSize.y / rect.size.y;

    public override void OnTabOpen()
    {
      base.OnTabOpen();
    }

    public override void ResetToCenter()
    {
      base.ResetToCenter();
      initialized = false;
      zoom = 2;
    }

    private void ChangeMade()
    {
      parent.ChangeMade();
      UnsavedChanges = true;
    }

    public override void Save()
    {
      if (parent.controller)
      {
        AnimationLoader.Save(parent.controller);
      }
    }

    public override void CopyToClipboard()
    {
      if (selector.AnyStatesSelected)
      {
        CopySelectedStates();
      }
    }

    public override void Paste()
    {
      if (!clipboard.IsEmpty)
      {
        PasteState();
      }
    }

    public override void Delete()
    {
      if (selector.AnySelected)
      {
        DeleteSelection();
      }
    }

    public override void Escape()
    {
      StopMakingTransition();
    }


    public override void Draw(Rect rect)
    {
      if (parent.animLayer == null)
      {
        parent.animLayer = parent.controller.layers.FirstOrDefault();
        Assert.IsNotNull(parent.animLayer);
      }

      if (HideLeftWindow)
      {
        DrawControllerSectionRight(rect);
      }
      else
      {
        rect.SplitVertically(leftWindowSize, out Rect leftRect, out Rect rightRect);

        DrawControllerSectionLeft(leftRect);
        DrawControllerSectionRight(rightRect);
      }
    }

#region Left Section

    private void DrawControllerSectionLeft(Rect rect)
    {
      DrawBackground(rect);

      Rect tabRect = new Rect(rect.x, rect.y, TabWidth, WidgetBarHeight);
      if (ToggleText(tabRect, "ST_Layers".Translate(), null, leftSectionTab == LeftSection.Layers))
      {
        leftSectionTab = LeftSection.Layers;
      }
      tabRect.x += tabRect.width;
      //if (ToggleText(tabRect, "ST_Parameters".Translate(), null, leftSectionTab == LeftSection.Parameters))
      //{
      //	leftSectionTab = LeftSection.Parameters;
      //}

      Rect toggleVisibilityRect =
        new Rect(rect.xMax - WidgetBarHeight, rect.y, WidgetBarHeight, WidgetBarHeight)
         .ContractedBy(2);
      if (!HideLeftWindow && Widgets.ButtonImage(toggleVisibilityRect, eyeTex))
      {
        HideLeftWindow = true;
      }

      DoSeparatorHorizontal(rect.x, rect.y, rect.width);
      DoSeparatorHorizontal(rect.x, tabRect.yMax, rect.width);

      switch (leftSectionTab)
      {
        case LeftSection.Layers:
          DrawLayersTab(rect);
          break;
        case LeftSection.Parameters:
          DrawParametersTab(rect);
          break;
      }

      if (selector.AnyStatesSelected)
      {
        float extraPanelHeight = rect.height * 0.65f;
        Rect extraPanelRect =
          new Rect(rect.x, rect.yMax - extraPanelHeight, rect.width, extraPanelHeight);
        DoSeparatorHorizontal(rect.x, extraPanelRect.y - 1, rect.width);
        DrawBackground(extraPanelRect);
        DrawStateProperties(extraPanelRect);
      }
      if (selector.AnyTransitionsSelected)
      {
        float extraPanelHeight = rect.height * 0.65f;
        Rect extraPanelRect =
          new Rect(rect.x, rect.yMax - extraPanelHeight, rect.width, extraPanelHeight);
        DoSeparatorHorizontal(rect.x, extraPanelRect.y - 1, rect.width);
        DrawBackground(extraPanelRect);
        DrawTransitionProperties(extraPanelRect);
      }

      rect.yMin += tabRect.height;

      DoResizerButton(rect, ref leftWindowSize, MinLeftWindowSize, MinRightWindowSize);
    }

    private void DrawStateProperties(Rect rect)
    {
      rect = rect.ContractedBy(4);

      AnimationState state = selector.SelectedStates.FirstOrDefault();
      if (state.Type == StateType.Entry || state.Type == StateType.Exit) return;

      Rect nameRect = new Rect(rect.x, rect.y + WidgetBarHeight, rect.width, WidgetBarHeight);
      state.name = Widgets.TextField(nameRect, state.name);
      nameRect.y += 5;

      DoSeparatorHorizontal(nameRect.x, nameRect.y, nameRect.width);

      nameRect.y += WidgetBarHeight;
      nameRect.SplitVertically(nameRect.width * 0.45f, out Rect labelRect, out Rect inputRect);

      // Motion
      GUI.enabled = AnimationLoader.Cache<AnimationClip>.Count > 0;
      Widgets.Label(labelRect, "ST_MotionFile".Translate());
      if (Dropdown(inputRect, state.clip?.FileName ?? string.Empty, state.clip?.FilePath))
      {
        Rect dropdownRect = new Rect(parent.windowRect.x + parent.EditorMargin + nameRect.x,
          parent.windowRect.y + parent.EditorMargin + inputRect.yMax, DropdownWidth, 500);
        Find.WindowStack.Add(new Dialog_AnimationClipLister(parent.animator, dropdownRect,
          state.clip,
          onFilePicked: delegate(AnimationClip clip) { state.clip = clip; },
          createItem: new Dialog_ItemDropdown<AnimationClip>.CreateItemButton("None", () => null)));
      }
      GUI.enabled = true;

      // Speed
      nameRect.y += WidgetBarHeight;
      nameRect.SplitVertically(nameRect.width * 0.65f, out Rect speedLabelRect,
        out Rect speedInputRect);
      Widgets.Label(speedLabelRect, "ST_MotionSpeed".Translate());
      Widgets.TextFieldNumeric(speedInputRect, ref state.speed, ref motionSpeedBuffer);

      // Looping
      nameRect.y += WidgetBarHeight;

      if (state.clip != null)
      {
        DoSeparatorHorizontal(nameRect.x, nameRect.y, nameRect.width);
        nameRect.y += 2;

        UIElements.CheckboxLabeled(nameRect, "ST_Loop".Translate(), ref state.loop);
        nameRect.y += WidgetBarHeight;

        GUI.enabled = state.loop;
        using (new TextBlock(GUI.enabled ? Color.white : Widgets.InactiveColor))
        {
          Widgets.TextFieldNumericLabeled(nameRect, "ST_CycleOffset".Translate(),
            ref state.clip.cycleOffset, ref cycleOffsetBuffer);
          nameRect.y += WidgetBarHeight;
        }
        GUI.enabled = true;

        DoSeparatorHorizontal(nameRect.x, nameRect.y, nameRect.width);
        nameRect.y += 2;
      }

      // Multiplier

      // Motion Time
      // Mirror
      // Cycle Offset
      // Write Defaults
      // Transitions
    }

    private void DrawTransitionProperties(Rect rect)
    {
      using var textBlock = new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, false);

      rect = rect.ContractedBy(4);

      Rect fieldRect = new Rect(rect.x, rect.y + WidgetBarHeight, rect.width, WidgetBarHeight);

      AnimationTransition transition = selector.SelectedTransitions.LastOrDefault();
      if (transition.FromState.Type == StateType.Entry || transition.ToState.Type == StateType.Exit)
      {
        Widgets.Label(fieldRect, "ST_NoControllerParameters".Translate());
        return;
      }

      // Conditions
      Widgets.Label(fieldRect, "ST_Conditions".Translate());
      DoSeparatorHorizontal(fieldRect.x, fieldRect.yMax, fieldRect.width);

      fieldRect.y += 5;

      foreach (AnimationCondition condition in transition.conditions)
      {
        fieldRect.y += fieldRect.height;
        condition.DrawConditionInput(fieldRect);
      }

      fieldRect.y += fieldRect.height;

      Color baseColor = GUI.enabled ? Color.white : Widgets.InactiveColor;
      Color mouseOverColor = GUI.enabled ? GenUI.MouseoverColor : Widgets.InactiveColor;
      Rect addConditionBtnRect = new Rect(fieldRect.xMax - WidgetBarHeight - 5, fieldRect.y,
        WidgetBarHeight, WidgetBarHeight);
      if (Widgets.ButtonImage(addConditionBtnRect.ContractedBy(2), TexButton.Plus, baseColor,
        mouseOverColor, GUI.enabled))
      {
        transition.AddCondition();
      }
    }

    private void DrawLayersTab(Rect rect)
    {
      Rect buttonBarRect = new Rect(rect.x, rect.y + WidgetBarHeight, rect.width, WidgetBarHeight);
      Rect addLayerBtnRect = new Rect(buttonBarRect.xMax - WidgetBarHeight - 5, buttonBarRect.y,
        WidgetBarHeight, WidgetBarHeight);
      if (Widgets.ButtonImage(addLayerBtnRect.ContractedBy(2), TexButton.Plus))
      {
        parent.controller.AddLayer("New Layer");
      }
      DoSeparatorHorizontal(rect.x, buttonBarRect.yMax, rect.width);

      Rect paddingRect = new Rect(rect.x, buttonBarRect.yMax, rect.width, 5).ContractedBy(1);
      Widgets.DrawBoxSolid(paddingRect, backgroundDopesheetColor.Subtract255NoAlpha(5, 5, 5));

      Rect layerRect = new Rect(rect.x, buttonBarRect.yMax, rect.width, LayerItemHeight);
      foreach (AnimationLayer layer in parent.controller.layers)
      {
        if (parent.animLayer == layer)
        {
          Widgets.DrawBoxSolid(layerRect, backgroundDopesheetColor.Add255NoAlpha(10, 10, 10));
        }
        Rect dragHandleRect =
          new Rect(layerRect.x, layerRect.y, layerRect.height, layerRect.height);
        Rect labelRect = new Rect(dragHandleRect.xMax, layerRect.y + 5,
          layerRect.width - dragHandleRect.width - LayerInputWidth, WidgetBarHeight);

        Text.Anchor = TextAnchor.MiddleLeft;
        if (EditingLayer == layer)
        {
          layer.name = Widgets.TextField(labelRect, layer.name);
          // Clicking anywhere outside of the text field control will confirm edit
          if (Input.GetMouseButtonDown(0) && !Mouse.IsOver(labelRect))
          {
            ConfirmLayerEdit();
          }
          // Return keys confirm edit
          if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
          {
            ConfirmLayerEdit();
          }
        }
        else
        {
          Widgets.Label(labelRect, layer.name);
          if (Widgets.ButtonInvisible(layerRect))
          {
            // Double clicking layer (even late) will begin name edit
            if (parent.animLayer == layer)
            {
              EditingLayer = layer;
            }
            parent.animLayer = layer;
          }
        }

        DoSeparatorHorizontal(layerRect.x, layerRect.yMax, layerRect.width);
        layerRect.y += layerRect.height;
      }
    }

    private void ConfirmLayerEdit()
    {
      EditingLayer.name = AnimationLoader.GetAvailableName(parent.controller.layers
       .Where(layer => layer != EditingLayer)
       .Select(layer => layer.name), EditingLayer.name);
      EditingLayer = null;
    }

    private void ConfirmParameterEdit()
    {
      //EditingParameter.Name = AnimationLoader.GetAvailableName(parent.controller.parameters.Where(param => param != EditingParameter)
      //																					 .Select(param => param.Name), EditingParameter.Name);
      EditingParameter = null;
    }

    private void DrawParametersTab(Rect rect)
    {
      Rect buttonBarRect = new Rect(rect.x, rect.y + WidgetBarHeight, rect.width, WidgetBarHeight);

      Rect searchBarRect = new Rect(buttonBarRect.x + 10, buttonBarRect.y,
        buttonBarRect.width - WidgetBarHeight * 2 - 20, WidgetBarHeight).ContractedBy(2);
      Rect addParameterBtnRect = new Rect(buttonBarRect.xMax - WidgetBarHeight - 5, buttonBarRect.y,
        WidgetBarHeight, WidgetBarHeight);

      parameterFilter.Text = Widgets.TextField(searchBarRect, parameterFilter.Text);

      // TODO - Runtime parameters are currently disabled. All parameters must be created through
      // via AnimationParameterDef
      GUIState.Disable();
      GUI.DrawTexture(addParameterBtnRect.ContractedBy(2), TexButton.Plus);
      if (Widgets.ButtonInvisible(addParameterBtnRect))
      {
        //List<FloatMenuOption> options = new List<FloatMenuOption>
        //{
        //	new FloatMenuOption(ParamType.Float.ToString(), delegate ()
        //	{
        //		AnimationParameter param = new AnimationParameter();
        //		param.Name = AnimationLoader.GetAvailableName(parent.controller.parameters.Select(p => p.Name), "New Float");
        //		param.Type = ParamType.Float;
        //		parent.controller.parameters.Add(param);
        //	}),
        //	new FloatMenuOption(ParamType.Int.ToString(), delegate ()
        //	{
        //		AnimationParameter param = new AnimationParameter();
        //		param.Name = AnimationLoader.GetAvailableName(parent.controller.parameters.Select(p => p.Name), "New Int");
        //		param.Type = ParamType.Int;
        //		parent.controller.parameters.Add(param);
        //	}),
        //	new FloatMenuOption(ParamType.Bool.ToString(), delegate ()
        //	{
        //		AnimationParameter param = new AnimationParameter();
        //		param.Name = AnimationLoader.GetAvailableName(parent.controller.parameters.Select(p => p.Name), "New Bool");
        //		param.Type = ParamType.Bool;
        //		parent.controller.parameters.Add(param);
        //	}),
        //	new FloatMenuOption(ParamType.Trigger.ToString(), delegate ()
        //	{
        //		AnimationParameter param = new AnimationParameter();
        //		param.Name = AnimationLoader.GetAvailableName(parent.controller.parameters.Select(p => p.Name), "New Trigger");
        //		param.Type = ParamType.Trigger;
        //		parent.controller.parameters.Add(param);
        //	}),
        //};
        //Find.WindowStack.Add(new FloatMenu(options));
      }
      GUIState.Enable();

      DoSeparatorHorizontal(rect.x, buttonBarRect.yMax, rect.width);

      Text.Anchor = TextAnchor.MiddleLeft;
      Rect parameterRect = new Rect(rect.x, buttonBarRect.yMax, rect.width, ParameterItemHeight);
      foreach (AnimationParameter parameter in parent.controller.parameters)
      {
        using var textBlock = new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, false);

        Rect dragHandleRect = new Rect(parameterRect.x, parameterRect.y, parameterRect.height,
          parameterRect.height);

        Rect entryRect = new Rect(parameterRect.xMax - 5 - ParameterInputWidth, parameterRect.y,
          ParameterInputWidth, parameterRect.height).ContractedBy(2);
        Rect labelRect = new Rect(dragHandleRect.xMax, parameterRect.y,
            parameterRect.width - dragHandleRect.width - entryRect.width - 5, parameterRect.height)
         .ContractedBy(2);

        Text.Anchor = TextAnchor.MiddleLeft;
        if (EditingParameter == parameter)
        {
          //parameter.Name = Widgets.TextField(labelRect, parameter.Name);
          // Clicking anywhere outside of the text field control will confirm edit
          if (Input.GetMouseButtonDown(0) && !Mouse.IsOver(labelRect))
          {
            ConfirmParameterEdit();
          }
          // Return keys confirm edit
          if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
          {
            ConfirmParameterEdit();
          }
        }
        else
        {
          Widgets.Label(labelRect, parameter.Name);
          //if (Widgets.ButtonInvisible(labelRect))
          //{
          //	// Double clicking parameter will begin name edit
          //	EditingParameter = parameter;
          //}

          parameter.DrawInput(entryRect);
        }

        parameterRect.y += parameterRect.height;
      }
    }

#endregion Left Section


#region Right Section

    private void DrawControllerSectionRight(Rect rect)
    {
      Rect gridRect = rect;
      // Needs padding for TopBar or its UI events will be intercepted by grid UI events.
      gridRect.yMin += WidgetBarHeight;

      Widgets.BeginGroup(gridRect);
      gridRect = gridRect.AtZero(); // Sets position to top left of GUI group
      Rect viewRect = new Rect(gridRect.x, gridRect.y, gridSize.x, gridSize.y);

      Vector2 mousePos = MouseUIPos(gridRect.position);
      mousePos -= MousePosLeftWindowAdjust;
      mouseGridPos = GridPosition(gridRect, viewRect, mousePos);

      if (!initialized)
      {
        //Start at center of grid
        SetScrollPosNormalized(gridRect, ref scrollPos, viewRect, new Vector2(0.5f, 0.5f));
        initialized = true;
      }
      Vector2 windowAdjust = new(HideLeftWindow ? 0 : leftWindowSize,
        WidgetBarHeight + TabDrawer.TabHeight);
      Rect visibleRect = GetVisibleRect(gridRect, scrollPos, viewRect);
      visibleRect.position -= MousePosLeftWindowAdjust;

      Vector2 groupPos = gridRect.position;
      UIElements.BeginScrollView(gridRect, ref scrollPos, viewRect, showHorizontalScrollbar: false,
        showVerticalScrollbar: false);
      {
        DrawBackgroundDark(viewRect);

        Rect blendRect = new Rect(gridRect.x, gridRect.yMax, gridRect.width, WidgetBarHeight);
        DrawBlend(blendRect, topBarFadeColor, backgroundCurvesColor);

        DrawGrid(viewRect);

        Rect dragRect = DragRect(gridRect.position - visibleRect.position);
        DrawAnimationStates(gridRect, viewRect, dragRect);

        if (RightClickUp && Mouse.IsOver(visibleRect))
        {
          clickedGridPos = mouseGridPos;
          FloatMenuOption newStateOption =
            new FloatMenuOption("ST_CreateState".Translate(), CreateNewState);
          FloatMenuOption newSubStateOption =
            new FloatMenuOption("ST_CreateSubState".Translate(), CreateNewState);
          newSubStateOption.Disabled = true;
          FloatMenuOption pasteOption = new FloatMenuOption("ST_Paste".Translate(), PasteState);
          pasteOption.Disabled = clipboard.IsEmpty;

          List<FloatMenuOption> options = new List<FloatMenuOption>()
          {
            newStateOption,
            newSubStateOption,
            pasteOption
          };
          Find.WindowStack.Add(new FloatMenu(options));
        }

        if (!draggingState)
        {
          SelectionBox(groupPos, visibleRect, viewRect, out _);
        }
      }
      UIElements.EndScrollView(false);
      Widgets.EndGroup();

      DrawTopBar(rect);

      if (Mouse.IsOver(gridRect) && Event.current.type == EventType.ScrollWheel)
      {
        float value = Event.current.delta.y * ZoomRate;
        zoom = Mathf.Clamp(zoom + value, 1, MaxZoom(gridRect));
        Event.current.Use();
      }

      if (!draggingSelectionBox && DragWindow(gridRect, DragItem.Grid, button: 2))
      {
        SetDragPos();
      }

      void SetDragPos()
      {
        Vector2 mousePos = new Vector2(UI.MousePositionOnUI.x, UI.MousePositionOnUI.y);
        Vector2 mouseDiff = dragPos - mousePos;
        dragPos = mousePos;
        scrollPos += new Vector2(mouseDiff.x, -mouseDiff.y);
      }
    }

    private void DrawTopBar(Rect rect)
    {
      Rect topBarRect = new Rect(rect.x, rect.y, rect.width, WidgetBarHeight);
      DrawBackground(topBarRect);

      if (HideLeftWindow)
      {
        Rect toggleVisibilityRect =
          new Rect(rect.x, rect.y, WidgetBarHeight, WidgetBarHeight).ContractedBy(2);
        if (Widgets.ButtonImage(toggleVisibilityRect, eyeStrikedTex))
        {
          HideLeftWindow = false;
        }
      }
      else
      {
        // TODO - show layer heirarchy
      }
    }

    private void DrawGrid(Rect viewRect)
    {
      int increment = 1; // Mathf.FloorToInt((zoom + 3) % 3);
      float centerX = viewRect.x + viewRect.width / 2;
      float centerY = viewRect.y + viewRect.height / 2;

      UIElements.DrawLineVertical(centerX, viewRect.y, viewRect.height, GetColor(0));
      int lines = Mathf.RoundToInt(viewRect.width / (GridSquareSize / zoom) / 2);
      for (int i = 1; i < lines; i += increment)
      {
        UIElements.DrawLineVertical(centerX + GridSquareSize * i * (1f / zoom), viewRect.y,
          viewRect.height, GetColor(i));
        UIElements.DrawLineVertical(centerX - GridSquareSize * i * (1f / zoom), viewRect.y,
          viewRect.height, GetColor(i));
      }

      UIElements.DrawLineHorizontal(viewRect.x, centerY, viewRect.width, GetColor(0));
      lines = Mathf.RoundToInt(viewRect.height / (GridSquareSize / zoom) / 2);
      for (int i = 1; i < lines; i += increment)
      {
        UIElements.DrawLineHorizontal(viewRect.x, centerY + GridSquareSize * i * (1f / zoom),
          viewRect.width, GetColor(i));
        UIElements.DrawLineHorizontal(viewRect.x, centerY - GridSquareSize * i * (1f / zoom),
          viewRect.width, GetColor(i));
      }

      Color GetColor(int lineNumber)
      {
        return lineNumber % 10 == 0 ? lineDarkColor : lineLightColor;
      }
    }

    /// <returns>Animation state was right clicked</returns>
    private void DrawAnimationStates(Rect outRect, Rect viewRect, Rect dragRect)
    {
      if (!parent.controller || parent.animLayer == null || parent.animLayer.states.NullOrEmpty())
      {
        return;
      }
      bool selected = false;
      Vector2 mousePos = StatePosition(viewRect, mouseGridPos);

      foreach (AnimationState state in parent.animLayer.states)
      {
        Vector2 sizeFrom = SizeFor(state.Type);
        Vector2 positionFrom = StatePosition(viewRect, state.position);
        //positionFrom.x -= TransitionAnchorSpacing;
        Rect stateRectFrom = new Rect(positionFrom, sizeFrom);
        if (Mouse.IsOver(stateRectFrom))
        {
          mousePos = stateRectFrom.center;
        }

        foreach (AnimationTransition transition in state.transitions)
        {
          Vector2 sizeTo = SizeFor(transition.ToState.Type);
          Vector2 positionTo = StatePosition(viewRect, transition.ToState.position);
          //positionTo.x += TransitionAnchorSpacing;
          Rect stateRectTo = new Rect(positionTo, sizeTo);
          Color color = selector.IsSelected(transition) ? highlightColor : Color.white;

          Widgets.DrawLine(stateRectFrom.center, stateRectTo.center, color, TransitionLineWidth);
          if (TransitionArrows(stateRectFrom.center, stateRectTo.center, color))
          {
            selected = true;
            selector.Select(transition,
              clear: !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl));
          }
        }
      }

      if (makingTransitionFrom != null)
      {
        Vector2 size = SizeFor(makingTransitionFrom.Type);
        Vector2 position = StatePosition(viewRect, makingTransitionFrom.position);
        Rect stateRect = new Rect(position, size);

        Widgets.DrawLine(stateRect.center, mousePos, Color.white, TransitionLineWidth + 1);
        TransitionArrows(stateRect.center, mousePos, Color.white);
      }

      foreach (AnimationState state in parent.animLayer.states)
      {
        Vector2 size = SizeFor(state.Type);
        Vector2 position = StatePosition(viewRect, state.position);
        Rect stateRect = new Rect(position, size);

        if (selector.IsSelected(state))
        {
          Rect highlightRect = stateRect.ExpandedBy(HighlightPadding);
          GUI.color = highlightColor;
          GUI.DrawTexture(highlightRect, stateTex);
        }

        GUI.color = GetStateColor(state);
        GUI.DrawTexture(stateRect, stateTex);
        GUI.color = Color.white;

        if (zoom < 5)
        {
          Text.Anchor = TextAnchor.MiddleCenter;
          Text.Font = Font;
          Widgets.Label(stateRect, state.name);
        }
        // Any left / right click event can trigger select, but LeftDown and RightUp also have additional actions
        if (Mouse.IsOver(stateRect) &&
          (LeftClickDown || RightClickDown || LeftClickUp || RightClickUp))
        {
          selector.Select(state,
            clear: !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl));
          selected = true;

          clickedGridPos = mouseGridPos;

          if (LeftClickDown)
          {
            if (makingTransitionFrom != null)
            {
              ConfirmTransition(state);
            }
            draggingState = true;
            draggingStateOrigPos = state.position;
          }
          else if (RightClickUp)
          {
            draggingState = false;
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (state.Type != StateType.Exit)
            {
              FloatMenuOption makeTransitionOption =
                new FloatMenuOption("ST_MakeTransition".Translate(), () => SetTransition(state));
              options.Add(makeTransitionOption);
            }
            if (!state.IsPermanent)
            {
              FloatMenuOption newSubStateOption =
                new FloatMenuOption("ST_SetStateDefault".Translate(), CreateNewState);
              newSubStateOption.Disabled = state.Type == StateType.Default;
              options.Add(newSubStateOption);

              FloatMenuOption copyOption =
                new FloatMenuOption("ST_Copy".Translate(), CopySelectedStates);
              options.Add(copyOption);

              FloatMenuOption deleteOption =
                new FloatMenuOption("ST_Delete".Translate(), () => DeleteState(state));
              options.Add(deleteOption);
            }
            if (!options.NullOrEmpty())
            {
              Find.WindowStack.Add(new FloatMenu(options));
            }
          }

          Event.current.Use();
        }
        else if (!draggingState && draggingSelectionBox)
        {
          if (dragRect.Overlaps(stateRect, true))
          {
            selector.Select(state, clear: false);
            selected = true;

            foreach (AnimationTransition transition in state.transitions)
            {
              if (selector.IsSelected(transition.ToState))
              {
                selector.Select(transition, clear: false);
              }
              else
              {
                selector.Unselect(transition);
              }
            }
          }
          else
          {
            selector.Unselect(state);
          }
        }
      }

      if (!Input.GetMouseButton(0))
      {
        draggingState = false;
      }
      if (LeftClickDown || RightClickDown)
      {
        StopMakingTransition();
      }
      if (draggingState)
      {
        foreach (AnimationState state in selector.SelectedStates)
        {
          IntVec2 gridPosDiff = mouseGridPos - clickedGridPos;
          state.position = draggingStateOrigPos + gridPosDiff;
        }
      }
      // Any click that is non-selecting should clear selection
      if (LeftClickDown && Mouse.IsOver(viewRect) && !selected)
      {
        selector.ClearSelectedStates();
        selector.ClearSelectedTransitions();
      }

      static bool
        TransitionArrows(Vector2 from, Vector2 to,
          Color color) // TODO - enable multiple transitions
      {
        float size = 14;

        Vector2 point = Vector2.Lerp(from, to, 0.5f);

        // invert y values since UI is top to bottom
        float rotation = Ext_Math.AngleToPoint(from.x, -from.y, to.x, -to.y) - 90;
        bool clicked = false;

        Rect buttonRect = new Rect(point.x - size / 2f, point.y - size / 2f, size, size);
        Matrix4x4 matrix = GUI.matrix;
        {
          UI.RotateAroundPivot(rotation, buttonRect.center);
          GUI.color = color;
          GUI.DrawTexture(buttonRect, TexButton.Play);
          GUI.color = Color.white;
        }
        GUI.matrix = matrix;

        if (Input.GetMouseButtonDown(0) && Mouse.IsOver(buttonRect))
        {
          clicked = true;
        }

        return clicked;
      }
    }

    private Vector2 SizeFor(StateType stateType)
    {
      return stateType switch
      {
        StateType.None => new Vector2(StateWidth * GridSquareSize / zoom,
          StateHeight * GridSquareSize / zoom),
        StateType.Entry => new Vector2(StateWidth * GridSquareSize / zoom,
          StateHeightSmall * GridSquareSize / zoom),
        StateType.Default => new Vector2(StateWidth * GridSquareSize / zoom,
          StateHeight * GridSquareSize / zoom),
        StateType.Exit => new Vector2(StateWidth * GridSquareSize / zoom,
          StateHeightSmall * GridSquareSize / zoom),
        StateType.Any => new Vector2(StateWidth * GridSquareSize / zoom,
          StateHeight * GridSquareSize / zoom),
        _ => new Vector2(StateWidth * GridSquareSize / zoom, StateHeight * GridSquareSize / zoom),
      };
    }

    private Vector2 StatePosition(Rect viewRect, IntVec2 gridPos)
    {
      float rectX = gridPos.x * GridSquareSize / zoom + viewRect.width / 2;
      float rectY = -gridPos.z * GridSquareSize / zoom + viewRect.height / 2;
      return new Vector2(rectX, rectY);
    }

    private IntVec2 GridPosition(Rect outRect, Rect viewRect, Vector2 pos)
    {
      Rect visibleRect = GetVisibleRect(outRect, scrollPos, viewRect);
      Vector2 finalPos = visibleRect.position + pos;
      Vector2 posT = finalPos / viewRect.size;
      int gridX = Mathf.RoundToInt(Mathf.Lerp(-GridSize / 2, GridSize / 2, posT.x) * zoom);
      int gridY = Mathf.RoundToInt(Mathf.Lerp(-GridSize / 2, GridSize / 2, posT.y) * zoom);
      return new IntVec2(gridX, -gridY); //Invert Y - UI is top to bottom | Grid is bottom to top
    }

    private Color GetStateColor(AnimationState state)
    {
      return state.Type switch
      {
        StateType.None    => stateColor,
        StateType.Entry   => entryStateColor,
        StateType.Default => defaultStateColor,
        StateType.Exit    => exitStateColor,
        StateType.Any     => anyStateColor,
        _                 => stateColor
      };
    }

    private bool DragWindow(Rect rect, DragItem dragItem, int button = 0)
    {
      return DragWindow(rect, SetDragItem, IsDragging, dragStarted: StartDragging,
        dragStopped: StopDragging, button: button);

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
        dragPos = Input.mousePosition;
        dragging = dragItem;
      }

      void StopDragging()
      {
        dragging = DragItem.None;
      }
    }

#endregion Right Section

#region Utils

    private void SetTransition(AnimationState from)
    {
      makingTransitionFrom = from;
    }

    private void ConfirmTransition(AnimationState target)
    {
      if (target.Type == StateType.Entry || target.Type == StateType.Any)
      {
        return;
      }
      makingTransitionFrom.AddTransition(target);
      StopMakingTransition();
    }

    private void StopMakingTransition()
    {
      makingTransitionFrom = null;
    }

    private void DeleteSelection()
    {
      if (selector.AnyTransitionsSelected)
      {
        foreach (AnimationTransition transition in selector.SelectedTransitions)
        {
          DeleteTransition(transition);
        }
        selector.ClearSelectedTransitions();
      }
      if (selector.AnyStatesSelected)
      {
        foreach (AnimationState state in selector.SelectedStates)
        {
          if (!state.IsPermanent)
          {
            DeleteState(state);
          }
        }
        selector.ClearSelectedStates();
      }
    }

    private void CreateNewState()
    {
      string name =
        AnimationLoader.GetAvailableName(parent.animLayer.states.Select(state => state.name),
          "New State");
      parent.animLayer.AddState(name, clickedGridPos);
    }

    private void PasteState()
    {
      foreach (AnimationState state in clipboard.GetCopiedStates())
      {
        AnimationState newState = state.CreateCopy(parent.animLayer);
        parent.animLayer.states.Add(newState);
      }
    }

    private void DeleteState(AnimationState state)
    {
      state.Dispose();
      parent.animLayer.states.Remove(state);
    }

    private void DeleteTransition(AnimationTransition transition)
    {
      transition.Dispose();
    }

    private void CopySelectedStates()
    {
      if (!selector.AnyStatesSelected)
      {
        return;
      }

      clipboard.CopyToClipboard(selector.SelectedStates);
    }

#endregion Utils

    private enum DragItem
    {
      None,
      Grid,
    }

    private enum LeftSection
    {
      Layers,
      Parameters
    }

    private class Clipboard
    {
      private HashSet<AnimationState> copiedStates = new HashSet<AnimationState>();

      public bool IsEmpty => copiedStates.Count == 0;

      public void CopyToClipboard(IEnumerable<AnimationState> states)
      {
        ClearClipboard();
        copiedStates.AddRange(states);
      }

      public IEnumerable<AnimationState> GetCopiedStates()
      {
        return copiedStates;
      }

      public void ClearClipboard()
      {
        copiedStates.Clear();
      }
    }

    private class Selector
    {
      private HashSet<AnimationState> selectedStates = new HashSet<AnimationState>();
      private HashSet<AnimationTransition> selectedTransitions = new HashSet<AnimationTransition>();

      public bool AnySelected => AnyStatesSelected || AnyTransitionsSelected;

      public bool AnyStatesSelected => selectedStates.Any();

      public bool AnyTransitionsSelected => selectedTransitions.Any();

      public HashSet<AnimationState> SelectedStates => selectedStates;

      public HashSet<AnimationTransition> SelectedTransitions => selectedTransitions;

      public bool IsSelected(AnimationState state)
      {
        return selectedStates.Contains(state);
      }

      public bool IsSelected(AnimationTransition transition)
      {
        return selectedTransitions.Contains(transition);
      }

      public void Select(AnimationState state, bool clear = true)
      {
        if (clear)
        {
          ClearSelectedStates();
        }
        selectedStates.Add(state);
      }

      public void Select(AnimationTransition transition, bool clear = true)
      {
        if (clear)
        {
          ClearSelectedTransitions();
        }
        selectedTransitions.Add(transition);
      }

      public void Unselect(AnimationState state)
      {
        selectedStates.Remove(state);
      }

      public void Unselect(AnimationTransition transition)
      {
        selectedTransitions.Remove(transition);
      }

      public void ClearSelectedStates()
      {
        selectedStates.Clear();
      }

      public void ClearSelectedTransitions()
      {
        selectedTransitions.Clear();
      }
    }
  }
}