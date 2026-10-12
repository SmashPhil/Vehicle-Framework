using System;
using Verse;
using UnityEngine;

namespace UpdateLogTool
{
	public class AnchorSegment : TaggedSegment
	{
		public override (string, string) Tags => ("<anchor>", "</anchor>");

		public override int HeightOccupied(UpdateLog log, string fullText)
		{
			Text.Anchor = (TextAnchor)Enum.Parse(typeof(TextAnchor), fullText, true);
			return 0;
		}

		public override void SegmentAction(Listing_Rich lister, string innerText)
		{
			Text.Anchor = (TextAnchor)Enum.Parse(typeof(TextAnchor), innerText, true);
		}
	}
}
