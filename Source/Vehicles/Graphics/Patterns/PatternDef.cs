using System.Collections.Generic;
using JetBrains.Annotations;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class PatternDef : Def, IMaterialCacheTarget
{
  public string path;
  public PatternProperties properties;

  private TextureWrapMode wrapMode = TextureWrapMode.Repeat;

  public List<VehicleDef> exclusiveFor;

  private Texture2D[] patterns;

  private bool IsDefault => this == PatternDefOf.Default;

  public virtual ShaderTypeDef ShaderTypeDef => VehicleShaderTypeDefOf.CutoutComplexPattern;

  public int MaterialCount => 4;

  PatternDef IMaterialCacheTarget.PatternDef => this;

  public string Name => $"{modContentPack.Name}_{defName}";

  public MaterialPropertyBlock PropertyBlock { get; private set; }

  public Texture2D this[Rot8 rot]
  {
    get
    {
      if (patterns is null)
      {
        RecacheTextures();
        Assert.IsNotNull(patterns);
      }
      return patterns[rot.AsInt];
    }
  }

  public bool ValidFor(VehicleDef def)
  {
    return exclusiveFor.NullOrEmpty() || exclusiveFor.Contains(def);
  }

  private void RecacheTextures()
  {
    patterns = new Texture2D[8];

    string[] paths =
    [
      path + "_north", path + "_east", path + "_south", path + "_west",
      path + "_northEast", path + "_southEast", path + "_southWest", path + "_northWest"
    ];

    patterns[0] = ContentFinder<Texture2D>.Get(path, false);
    patterns[0] ??= ContentFinder<Texture2D>.Get(paths[0], false);

    if (patterns[0] is null)
    {
      SmashLog.Error($"Unable to find Texture2D for <field>path</field> at {path}.");
      return;
    }
    if (IsDefault)
    {
      patterns[1] = patterns[0];
      patterns[2] = patterns[0];
      patterns[3] = patterns[0];
      patterns[4] = patterns[0];
      patterns[5] = patterns[0];
      patterns[6] = patterns[0];
      patterns[7] = patterns[0];
      return;
    }

    for (int i = 1; i < 8; i++)
    {
      patterns[i] = ContentFinder<Texture2D>.Get(paths[i], false);
    }

    if (!patterns[1])
    {
      patterns[1] = patterns[0].Rotate(270);
    }
    if (!patterns[2])
    {
      patterns[2] = patterns[0].Rotate(180);
    }
    if (!patterns[3])
    {
      patterns[3] = patterns[0].Rotate(90);
    }

    if (!patterns[4])
    {
      patterns[4] = patterns[0];
    }
    if (!patterns[5])
    {
      patterns[5] = patterns[2];
    }
    if (!patterns[6])
    {
      patterns[6] = patterns[2];
    }
    if (!patterns[7])
    {
      patterns[7] = patterns[2];
    }

    for (int i = 0; i < patterns.Length; i++)
    {
      Texture2D texture = patterns[i];
      if (texture.wrapMode != wrapMode)
      {
        patterns[i] = Ext_Texture.WrapTexture(texture, wrapMode);
        //Replace and destroy original textures in ContentFinder to free up memory
        if (Ext_Texture.TryReplaceInContentFinder(paths[i], patterns[i]))
        {
          Debug.Message($"[{modContentPack.Name}] Wrapping and destroying {paths[i]}");
        }
      }
    }
  }

  public override IEnumerable<string> ConfigErrors()
  {
    foreach (string error in base.ConfigErrors())
    {
      yield return error;
    }

    if (path.NullOrEmpty())
    {
      yield return "<field>path</field> must be a valid texture path.".ConvertRichText();
    }
    if (properties?.tiles != null)
    {
      foreach (KeyValuePair<string, float> tileData in properties.tiles)
      {
        if (tileData.Value == 0)
        {
          yield return
            $"key <field>{tileData.Key}</field> in <field>tiles</field> should not be set to 0. This will result in odd coloring of the pattern."
             .ConvertRichText();
        }
      }
    }
  }

  public override void ResolveReferences()
  {
    properties ??= new PatternProperties();
    properties.tiles ??= new Dictionary<string, float>();

    if (IsDefault)
    {
      properties.IsDefault = true;
    }
  }

  internal static void GenerateMaterials()
  {
    if (VehicleMod.settings.main.useCustomShaders)
    {
      foreach (PatternDef patternDef in DefDatabase<PatternDef>.AllDefsListForReading)
      {
        patternDef.RecacheTextures();
        patternDef.PropertyBlock ??= new MaterialPropertyBlock();
        RGBMaterialPool.CacheMaterialsFor(patternDef);
      }
    }
    else
    {
      PatternDefOf.Default.RecacheTextures();
    }
  }
}