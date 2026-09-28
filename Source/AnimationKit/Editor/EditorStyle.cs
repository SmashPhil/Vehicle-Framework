using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmashTools;
using UnityEngine;
using Verse;

namespace AnimationKit.Editor;

internal static class EditorStyle
{
  public static void DrawBackground(Rect rect)
  {
    Widgets.DrawBoxSolidWithOutline(rect, StyleUtils.BackgroundDopesheetColor, StyleUtils.SeparatorColor);
  }

  public static void DrawBackgroundDark(Rect rect)
  {
    Widgets.DrawBoxSolidWithOutline(rect, StyleUtils.BackgroundCurvesColor, StyleUtils.SeparatorColor);
  }

  public static void DoSeparatorHorizontal(float x, float y, float length)
  {
    UIElements.DrawLineHorizontal(x, y, length, StyleUtils.SeparatorColor);
  }

  public static void DoSeparatorVertical(float x, float y, float height)
  {
    UIElements.DrawLineVertical(x, y, height, StyleUtils.SeparatorColor);
  }
}
