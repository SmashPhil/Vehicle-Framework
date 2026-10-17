using System;
using System.Collections.Generic;

namespace SmashTools;

public class Dialog_RadioButtonMenu : Dialog_ToggleMenu
{
  public Dialog_RadioButtonMenu(string label, List<Toggle> toggles, Action postClose = null) :
    base(label, toggles, postClose)
  {
  }

  protected override void DrawToggle(Toggle toggle)
  {
    if (toggle.Disabled)
    {
      GUIState.Disable();
    }

    try
    {
      bool value = lister.RadioButton(toggle.DisplayName, toggle.Active);
      toggle.Active = value;
    }
    finally
    {
      GUIState.Enable();
    }
  }
}