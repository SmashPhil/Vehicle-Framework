using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class DirectionalProtocolProperties
	{
		[GraphEditable(Prefix = AnimationEditorTags.Vertical)]
		public LaunchProtocolProperties vertical;
		[GraphEditable(Prefix = AnimationEditorTags.Horizontal)]
		public LaunchProtocolProperties horizontal;
	}
}
