using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.Animations
{
  public class Dialog_PropertySelect : Window
  {
    private const float EntryHeight = 28;
    private const float SubPropertyPadding = 15;

    private static readonly Color backgroundColor = new ColorInt(56, 56, 56).ToColor;
    private static readonly Color backgroundOutlineColor = new ColorInt(74, 74, 74).ToColor;

    private readonly IAnimator animator;
    private readonly AnimationClip animation;

    private Vector2 scrollPos;

    private Vector2 position;
    private Action<AnimationPropertyParent> propertyAdded;

    private List<string> propertyListOrder = [];
    private Dictionary<string, List<AnimationPropertyParent>> properties = [];
    private bool[] expandedContainers;

    public Dialog_PropertySelect(IAnimator animator, AnimationClip animation, Vector2 position,
      Action<AnimationPropertyParent> propertyAdded = null)
    {
      this.animator = animator;
      this.animation = animation;
      this.position = position;
      this.propertyAdded = propertyAdded;

      this.closeOnClickedOutside = true;
      this.absorbInputAroundWindow = false;
      this.preventCameraMotion = false;
      this.doWindowBackground = false;
      this.layer = WindowLayer.Super;
    }

    private float WindowHeight { get; set; }

    public override Vector2 InitialSize => new Vector2(300, 350);

    protected override float Margin => 0;

    public override void PreOpen()
    {
      HashSet<AnimationPropertyParent> existingProperties =
        animation.properties != null ? [.. animation.properties] : [];
      foreach (AnimationPropertyParent container in
        AnimationPropertyRegistry.GetAnimationProperties(animator))
      {
        if (!existingProperties.Contains(container))
        {
          string key = container.Identifier != null ?
            $"{container.Type.Name} ({container.Identifier})" :
            container.Type.Name;
          if (!properties.ContainsKey(key))
          {
            propertyListOrder.Add(key);
          }
          properties.AddOrAppend(key, container);
        }
      }
      expandedContainers = new bool[properties.Count];
      base.PreOpen();
    }

    public override void Notify_ClickOutsideWindow()
    {
      base.Notify_ClickOutsideWindow();
      Close();
    }

    private void RecalculateHeight()
    {
      float height = 0;
      for (int i = 0; i < properties.Count; i++)
      {
        string propertiesKey = propertyListOrder[i];
        height += EntryHeight;
        if (expandedContainers[i])
        {
          height += properties[propertiesKey].Count * EntryHeight;
        }
      }
      WindowHeight = height;
    }

    protected override void SetInitialSizeAndPosition()
    {
      if (position.x + InitialSize.x > UI.screenWidth)
      {
        position.x = UI.screenWidth - InitialSize.x;
      }
      if (position.y + InitialSize.y > UI.screenHeight)
      {
        position.y = UI.screenHeight - InitialSize.y;
      }
      windowRect = new Rect(position.x, position.y, InitialSize.x, InitialSize.y);
    }

    public override void DoWindowContents(Rect inRect)
    {
      Widgets.DrawBoxSolidWithOutline(inRect, backgroundColor, backgroundOutlineColor,
        outlineThickness: 2);

      using var textBlock = new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, false);

      Rect outRect = inRect;
      Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16, WindowHeight);
      Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
      Rect rowRect = new Rect(inRect.x, inRect.y, inRect.width, EntryHeight).ContractedBy(3);
      for (int i = 0; i < properties.Count; i++)
      {
        string propertiesKey = propertyListOrder[i];
        bool expanded = expandedContainers[i];
        rowRect.SplitVertically(EntryHeight, out Rect checkboxRect, out Rect fileLabelRect);

        Widgets.Label(fileLabelRect, propertiesKey);
        if (UIElements.CollapseButton(checkboxRect.ContractedBy(2), ref expanded))
        {
          expandedContainers[i] = expanded;
          SoundDefOf.Click.PlayOneShotOnCamera(null);
        }
        rowRect.y += rowRect.height;

        if (!expanded) continue;

        List<AnimationPropertyParent> containers = properties[propertiesKey];
        foreach (AnimationPropertyParent container in containers)
        {
          Rect propertyParentRect = new(fileLabelRect.x + SubPropertyPadding, rowRect.y,
            fileLabelRect.width - SubPropertyPadding, fileLabelRect.height);
          if (DrawProperty(propertyParentRect, container))
          {
            break;
          }
          rowRect.y += rowRect.height;
        }
      }
      Widgets.EndScrollView();
    }

    private bool DrawProperty(Rect rect, AnimationPropertyParent container)
    {
      Widgets.Label(rect, container.Label);
      if (AddPropertyButton(rect))
      {
        animation.properties.Add(container);
        propertyAdded?.Invoke(container);
        Close();
        return true;
      }
      return false;
    }

    private bool AddPropertyButton(Rect rect)
    {
      float size = rect.height;
      Rect buttonRect = new Rect(rect.xMax - size, rect.y, size, size);
      return Widgets.ButtonImage(buttonRect.ContractedBy(2), TexButton.Plus);
    }
  }
}