using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace UpdateLogTool
{
	public class GifSegment : AnimatedSegment
	{
		private static readonly IntVec2 defaultGifSize = new IntVec2(10, 10);

		public override (string, string) Tags => ("<gif", "</gif>");

		protected override int DefaultFramesPerSecond => 30;

		protected override IEnumerable<(string name, Type type)> Attributes
		{
			get
			{
				foreach (var attribute in base.Attributes)
				{
					yield return attribute;
				}
				yield return (TagNames.FPS, typeof(int));
				yield return (TagNames.Size, typeof(IntVec2));
			}
		}

		protected override void RenderImage(Listing_Rich lister, string innerText, Texture2D texture)
		{
			Lookup lookup = ContainerAttributes(innerText);
			int width = lookup.Get(TagNames.Width, Mathf.RoundToInt(Dialog_NewUpdate.DialogWidth));
			int height = HeightOccupied(lister.CurrentLog, innerText);
			IntVec2 size = lookup.Get(TagNames.Size, defaultGifSize);
			int fps = lookup.Get(TagNames.FPS, 30);
			lister.DrawGif(texture, width, height, size, fps: fps);
		}
	}
}
