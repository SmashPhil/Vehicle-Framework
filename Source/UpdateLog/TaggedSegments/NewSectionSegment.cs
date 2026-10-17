using System;
using Verse;

namespace UpdateLogTool
{
	public class NewSectionSegment : TaggedSegment
	{
		public override (string, string) Tags => ("<s/>", null);

		public override void SegmentAction(Listing_Rich lister, string innerText)
		{
		}
	}
}
