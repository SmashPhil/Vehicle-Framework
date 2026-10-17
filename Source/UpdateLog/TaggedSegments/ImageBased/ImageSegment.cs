using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace UpdateLogTool
{
	public class ImageSegment : ContainerSegment
	{
		public override (string, string) Tags => ("<img", "</img>");

		public override void SegmentAction(Listing_Rich lister, string innerText)
		{
			string url = GetInnerText(innerText);
			if (lister.CurrentLog is UpdateLog log)
			{
				if (log.cachedTextures.TryGetValue(url, out Texture2D texture))
				{
					RenderImage(lister, innerText, texture);
				}
				else if (log.cachedDownloadedTextures.TryGetValue(url, out WebTexture webTexture))
				{
					//Only render if download has finished
					if (webTexture.Status == DownloadStatus.Success)
					{
						RenderImage(lister, innerText, webTexture.texture);
					}
					else
					{
						RenderLoadingPlaceholder(lister, innerText, WebTexture.StringStatus(url, webTexture.Status));
					}
				}
				else
				{
					Log.ErrorOnce($"Failed to retrieve cached texture for {url}.", url.GetHashCode());
				}
			}
		}

		protected virtual void RenderImage(Listing_Rich lister, string innerText, Texture2D texture)
		{
			Lookup lookup = ContainerAttributes(innerText);
			int width = lookup.Get(TagNames.Width, Mathf.RoundToInt(Dialog_NewUpdate.DialogWidth));
			int height = HeightOccupied(lister.CurrentLog, innerText);
			lister.DrawTexture(texture, width, height);
		}

		protected virtual void RenderLoadingPlaceholder(Listing_Rich lister, string innerText, string loadingMessage)
		{
			Lookup lookup = ContainerAttributes(innerText);
			int width = lookup.Get(TagNames.Width, Mathf.RoundToInt(Dialog_NewUpdate.DialogWidth));
			int height = HeightOccupied(lister.CurrentLog, innerText);
			lister.DrawLoadingPlaceholder(width, height, loadingMessage);
		}
	}
}
