using Verse;
using UnityEngine;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class MaterialPresets
	{
		public static readonly Material SelectionBracketMat = MaterialPool.MatFrom("UI/Overlays/SelectionBracket", ShaderDatabase.MetaOverlay);
	}
}
