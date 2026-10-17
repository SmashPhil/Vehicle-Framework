using System;
using Verse;

namespace UpdateLogTool
{
	public class FontSegment : TaggedSegment
	{
		public override (string, string) Tags => ("<font>", "</font>");

		public override int HeightOccupied(UpdateLog log, string fullText)
		{
			Text.Font = (GameFont)Enum.Parse(typeof(GameFont), fullText, true);
			return 0;
		}

		public override void SegmentAction(Listing_Rich lister, string innerText)
		{
			Text.Font = (GameFont)Enum.Parse(typeof(GameFont), innerText, true);
		}
	}
}
