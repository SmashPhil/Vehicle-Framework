using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Verse;
using UnityEngine;
using UpdateLog;

namespace Vehicles
{
	#if BETA
	public class Dialog_BetaWindow : Window
	{
		private const int PreviewImageHeight = 200;
		private const int BarIconSize = 25;

		private readonly string betaDescription;

		private readonly List<DescriptionData> segments = new List<DescriptionData>();

		public Dialog_BetaWindow()
		{
			doCloseX = true;
			forcePause = true;
			closeOnClickedOutside = false;
			absorbInputAroundWindow = true;
			try
			{
				betaDescription = File.ReadAllText(Path.Combine(VehicleHarmony.VehicleMMD.RootDir.FullName, "About", "BetaDescription.txt"));
				segments = EnhancedText.ParseDescriptionData(betaDescription).ToList();
			}
			catch (Exception ex)
			{
				throw new IOException($"Failed to read in BetaDescription");
			}
		}

		public override Vector2 InitialSize => new Vector2(600, 740);

		public override void DoWindowContents(Rect inRect)
		{
			var color = GUI.color;
			var anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			var font = Text.Font;
			Text.Font = GameFont.Medium;

			Texture2D previewImage = VehicleHarmony.VehicleMMD.PreviewImage;

			float pWidth = previewImage?.width ?? 0;
			float pHeight = previewImage?.height ?? 0;
			float imageWidth = ((float)pWidth / pHeight) * PreviewImageHeight;
			Rect previewRect = new Rect(inRect)
			{
				x = (inRect.width - imageWidth) / 2,
				height = PreviewImageHeight,
				width = imageWidth
			};
			if (previewImage != null)
			{
				GUI.DrawTexture(previewRect, previewImage);
			}

			Rect modLabelRect = new Rect(inRect)
			{
				y = previewRect.y + previewRect.height + 5,
				height = Text.CalcHeight(VehicleHarmony.VehicleMMD.Name, inRect.width)
			};
			Widgets.Label(modLabelRect, VehicleHarmony.VehicleMMD.Name);

			Widgets.DrawLineHorizontal(0, modLabelRect.y + modLabelRect.height, modLabelRect.width);

			Rect iconBarRect = new Rect(inRect.width - BarIconSize, modLabelRect.y, BarIconSize, BarIconSize);
			if (Mouse.IsOver(iconBarRect))
			{
				GUI.color = GenUI.MouseoverColor;
			}
			if (Widgets.ButtonInvisible(iconBarRect))
			{
				Application.OpenURL("https://github.com/SmashPhil/Vehicles");
			}
			Widgets.DrawTextureFitted(iconBarRect, VehicleTex.GithubIcon, 1);
			iconBarRect.x -= BarIconSize + 10;
			GUI.color = color;
			if (Mouse.IsOver(iconBarRect))
			{
				GUI.color = GenUI.MouseoverColor;
			}
			if (Widgets.ButtonInvisible(iconBarRect))
			{
				Application.OpenURL("https://discord.gg/zXDyfWQ");
			}
			Widgets.DrawTextureFitted(iconBarRect, VehicleTex.DiscordIcon, 1);
			iconBarRect.x -= BarIconSize + 10;
			GUI.color = color;
			if (Mouse.IsOver(iconBarRect))
			{
				GUI.color = GenUI.MouseoverColor;
			}
			if (Widgets.ButtonInvisible(iconBarRect))
			{
				Application.OpenURL("https://steamcommunity.com/sharedfiles/filedetails/?id=2356577528");
			}
			Widgets.DrawTextureFitted(iconBarRect, VehicleTex.SteamIcon, 1);
			GUI.color = color;

			float descY = modLabelRect.y + modLabelRect.height + 5;

			Rect lowerRect = new Rect(inRect.x, descY, inRect.width, inRect.height - descY);

			Listing_Rich lister = new Listing_Rich();
			lister.Begin(lowerRect);
			
			foreach (DescriptionData segment in segments)
			{
				if (segment.tag is TaggedSegment tag)
				{
					tag.SegmentAction(lister, segment.text);
				}
				else
				{
					lister.RichText(segment);
				}
			}

			lister.End();

			Text.Anchor = anchor;
			Text.Font = font;
			GUI.color = color;
		}
	}
	#endif
}
