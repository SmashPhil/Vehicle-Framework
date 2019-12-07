using Verse;
using UnityEngine;

namespace RimShips
{
    [StaticConstructorOnStartup]
    public static class MaterialDefOf
    {
        public static readonly Material SelectionBracketMat = MaterialPool.MatFrom("UI/Overlays/SelectionBracket", ShaderDatabase.MetaOverlay);
    }
}
