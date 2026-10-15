using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools;

public class Dialog_ActionList : Dialog_ToggleMenu
{
  public Dialog_ActionList(string label, List<Toggle> toggles, Action postClose = null) : base(
    label, toggles, postClose)
  {
  }

  protected override void DrawToggle(Toggle toggle)
  {
    if (toggle.Disabled)
      GUIState.Disable();

    if (lister.ClickableLabel(toggle.DisplayName))
    {
      toggle.Active = true;
      Close();
    }

    GUIState.Enable();
  }
}