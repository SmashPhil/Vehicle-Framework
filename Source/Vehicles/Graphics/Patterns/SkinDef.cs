using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
  public class SkinDef : PatternDef
  {
    public override ShaderTypeDef ShaderTypeDef => VehicleShaderTypeDefOf.CutoutComplexSkin;
  }
}