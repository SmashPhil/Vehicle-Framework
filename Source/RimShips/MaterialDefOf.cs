using Verse;
using UnityEngine;

namespace Vehicles
{
    [StaticConstructorOnStartup]
    public static class MaterialDefOf
    {
        public static readonly Material SelectionBracketMat = MaterialPool.MatFrom("UI/Overlays/SelectionBracket", ShaderDatabase.MetaOverlay);
    }
}
