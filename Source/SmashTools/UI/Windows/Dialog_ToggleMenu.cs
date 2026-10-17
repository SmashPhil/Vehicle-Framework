using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SmashTools
{
  public class Dialog_ToggleMenu : Window
  {
    protected readonly Listing_Standard lister;

    private readonly string label;
    private readonly List<Toggle> toggles;
    private readonly Action postClose;

    public Dialog_ToggleMenu(string label, List<Toggle> toggles, Action postClose = null)
    {
      this.label = label;
      this.toggles = toggles.OrderBy(rb => rb.Category).ToList();
      this.postClose = postClose;

      doCloseX = true;
      onlyOneOfTypeAllowed = true;
      absorbInputAroundWindow = true;
      lister = new Listing_Standard(GameFont.Small)
      {
        ColumnWidth = 300
      };
    }

    public override Vector2 InitialSize => new(UI.screenWidth, UI.screenHeight);

    public override void PostClose()
    {
      postClose?.Invoke();
    }

    protected virtual void DrawToggle(Toggle toggle)
    {
      bool checkOn = toggle.Active;

      if (toggle.Disabled)
        GUIState.Disable();
      lister.CheckboxLabeled(toggle.DisplayName, ref checkOn);
      GUIState.Enable();

      toggle.Active = checkOn;
    }

    public override void DoWindowContents(Rect rect)
    {
      using (new TextBlock(GameFont.Medium, TextAnchor.UpperCenter))
      {
        Widgets.Label(rect, label);
        rect.yMin += 30;
      }

      lister.Begin(rect);
      string category = string.Empty;
      foreach (Toggle toggle in toggles)
      {
        if (toggle.Category != category)
        {
          category = toggle.Category;
          lister.Header(category, ListingExtension.BannerColor, GameFont.Medium,
            TextAnchor.MiddleCenter);
        }
        DrawToggle(toggle);
      }
      lister.End();
    }
  }
}