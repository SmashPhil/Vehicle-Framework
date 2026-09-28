using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AnimationKit.Editor;

internal class StyleUtils
{
  internal static readonly Color BackgroundLightColor = new ColorInt(63, 63, 63).ToColor;
  internal static readonly Color BackgroundDopesheetColor = new ColorInt(56, 56, 56).ToColor;
  internal static readonly Color BackgroundCurvesColor = new ColorInt(40, 40, 40).ToColor;
  internal static readonly Color SeparatorColor = new ColorInt(35, 35, 35).ToColor;

  internal static readonly Color ButtonColor = new ColorInt(88, 88, 88).ToColor;
  internal static readonly Color ButtonPressedColor = new ColorInt(70, 96, 124).ToColor;

  internal static readonly Color SelectBoxFillColor = new ColorInt(85, 145, 245, 15).ToColor;
  internal static readonly Color SelectBoxBorderColor = new ColorInt(125, 175, 245, 75).ToColor;
}
