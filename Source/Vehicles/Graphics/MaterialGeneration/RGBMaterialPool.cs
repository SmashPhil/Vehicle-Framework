#define UI_SHADER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using LudeonTK;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Object = UnityEngine.Object;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles;

[PublicAPI]
[StaticConstructorOnStartup]
public static class RGBMaterialPool
{
  // UI Only shows Rot4 rotations, there's no need to allocate for diagonals
  private const int MaxUiArraySize = 4;

  private static readonly Dictionary<IMaterialCacheTarget, Material[]> Cache = [];
  private static readonly Dictionary<IMaterialCacheTarget, Material[]> UiCache = [];

  // TODO - Quickest way to get set up with ui shaders, may need revisit down the road.
  private static readonly Dictionary<Shader, Shader> ShaderToUiShader = [];

  public static event Action<IMaterialCacheTarget> OnTargetCached;
  public static event Action<IMaterialCacheTarget> OnTargetRemoved;

  static RGBMaterialPool()
  {
    ShaderToUiShader[VehicleShaderTypeDefOf.CutoutComplexRGB.Shader] = ShaderDatabase.LoadShader("ShaderRGBUI");
    ShaderToUiShader[VehicleShaderTypeDefOf.CutoutComplexPattern.Shader] =
      ShaderDatabase.LoadShader("ShaderRGBPatternUI");
    ShaderToUiShader[VehicleShaderTypeDefOf.CutoutComplexSkin.Shader] = ShaderDatabase.LoadShader("ShaderRGBSkinUI");
  }

  public static int Count => Cache.Count;

  public static int TotalMaterials => Cache.Values.Sum(mats => mats.Length);

  // For testing only
  internal static List<IMaterialCacheTarget> AllCacheTargets => Cache.Keys.ToList();

  public static bool TargetCached(IMaterialCacheTarget target)
  {
    return Cache.ContainsKey(target);
  }

  public static Material[] GetAll(IMaterialCacheTarget target)
  {
    return Cache.TryGetValue(target);
  }

  public static Material GetUi(IMaterialCacheTarget target, Rot4 rot)
  {
    if (IsFeatureEnabled(BlitTexturePortraits))
    {
      return Get(target, rot);
    }
#if !UI_SHADER
    Material[] materials = Cache.TryGetValue(target);
#else
    Material[] materials = UiCache.TryGetValue(target);
#endif
    if (materials.Length == 0)
    {
      Trace.Fail("Trying to get ui material with 0 materials cached.");
      return null;
    }
    int index = materials.Length switch
    {
      // Single
      1 => 0,
      // Graphic_Multi with rotated diagonals
      4 => Rot8.ToRot4(rot).AsInt,
      // Graphic_Multi with diagonal textures
      8 => rot.AsInt,
      _ => throw new IndexOutOfRangeException(
        $"Trying to fetch texture from cache for rotation {rot}. Materials cached = {materials.Length}")
    };
    return materials[index];
  }

  public static Material Get(IMaterialCacheTarget target, Rot8 rot)
  {
    if (Cache.TryGetValue(target, out Material[] materials))
    {
      int index = materials.Length switch
      {
        // Single
        1 => 0,
        // Graphic_Multi with rotated diagonals
        4 => Rot8.ToRot4(rot).AsInt,
        // Graphic_Multi with diagonal textures
        8 => rot.AsInt,
        _ => throw new IndexOutOfRangeException(
          $"Trying to fetch texture from cache for rotation {rot}. Materials cached = {materials.Length}")
      };
      return materials[index];
    }
    return null;
  }

  public static void CacheMaterialsFor(IMaterialCacheTarget target, int renderQueue = 0,
    List<ShaderParameter> shaderParameters = null)
  {
    CacheMaterialsFor(target, target.PatternDef, renderQueue: renderQueue,
      shaderParameters: shaderParameters);
  }

  public static void CacheMaterialsFor(IMaterialCacheTarget target, PatternDef patternDef,
    int renderQueue = 0, List<ShaderParameter> shaderParameters = null)
  {
    if (Cache.ContainsKey(target) || patternDef == null)
      return;

    var materials = new Material[target.MaterialCount];
    var uiMaterials = new Material[Mathf.Min(MaxUiArraySize, target.MaterialCount)];
    for (int i = 0; i < materials.Length; i++)
    {
      Rot8 rot = new(i);
      Material material = new(patternDef.ShaderTypeDef.Shader)
      {
        name = target.Name + rot.ToStringNamed(),
        mainTexture = null,
        color = Color.clear,
      };
      Material uiMaterial = new(patternDef.ShaderTypeDef.Shader)
      {
        name = target.Name + rot.ToStringNamed(),
        mainTexture = null,
        color = Color.clear,
      };

      if (renderQueue != 0)
      {
        material.renderQueue = renderQueue;
        uiMaterial.renderQueue = renderQueue;
      }

      if (!shaderParameters.NullOrEmpty())
      {
        foreach (ShaderParameter shaderParameter in shaderParameters)
        {
          shaderParameter.Apply(material);
          shaderParameter.Apply(uiMaterial);
        }
      }
      materials[i] = material;
      if (i < MaxUiArraySize)
        uiMaterials[i] = uiMaterial;
    }

    Cache.Add(target, materials);
    UiCache.Add(target, uiMaterials);

    OnTargetCached?.Invoke(target);
  }

  public static void SetPropertyBlock(IMaterialCacheTarget target, PatternData patternData,
    Texture2D mainTex, Texture2D maskTex, Rot8 rot)
  {
    if (!Cache.ContainsKey(target))
    {
      Log.Error(
        $"Materials for {target} have not been created. Out of sequence material editing.");
      return;
    }
    MaterialPropertyBlock block = target.PropertyBlock;
    Assert.IsNotNull(block);
    block.Clear();

    block.SetTexture(AdditionalShaderPropertyIDs.MainTex, mainTex);

    if (patternData.patternDef != PatternDefOf.Default)
    {
      float tiles = patternData.tiles;
      if (patternData.patternDef.properties.tiles.TryGetValue("All", out float allTiles))
      {
        tiles *= allTiles;
      }

      if (!Mathf.Approximately(tiles, 0))
      {
        block.SetFloat(AdditionalShaderPropertyIDs.TileNum, tiles);
      }

      if (patternData.patternDef.properties.equalize)
      {
        float scaleX = 1;
        float scaleY = 1;
        if (mainTex.width > mainTex.height)
        {
          scaleY = (float)mainTex.height / mainTex.width;
        }
        else
        {
          scaleX = (float)mainTex.width / mainTex.height;
        }

        block.SetFloat(AdditionalShaderPropertyIDs.ScaleX, scaleX);
        block.SetFloat(AdditionalShaderPropertyIDs.ScaleY, scaleY);
      }

      if (patternData.patternDef.properties.dynamicTiling)
      {
        block.SetFloat(AdditionalShaderPropertyIDs.DisplacementX,
          patternData.displacement.x);
        block.SetFloat(AdditionalShaderPropertyIDs.DisplacementY,
          patternData.displacement.y);
      }
    }

    Texture2D patternTex = patternData.patternDef[rot];


    if (patternData.patternDef.ShaderTypeDef == VehicleShaderTypeDefOf.CutoutComplexSkin)
    {
      // Null reverts to original tex. Default would calculate to red
      target.PropertyBlock.SetTexture(AdditionalShaderPropertyIDs.SkinTex, patternTex);
    }
    else if (patternData.patternDef.ShaderTypeDef == VehicleShaderTypeDefOf.CutoutComplexPattern)
    {
      // Default to full red mask for full ColorOne pattern
      target.PropertyBlock.SetTexture(AdditionalShaderPropertyIDs.PatternTex, patternTex);
    }

    if (maskTex != null)
    {
      target.PropertyBlock.SetTexture(ShaderPropertyIDs.MaskTex, maskTex);
    }

    target.PropertyBlock.SetColor(AdditionalShaderPropertyIDs.ColorOne, patternData.color);
    target.PropertyBlock.SetColor(ShaderPropertyIDs.ColorTwo, patternData.colorTwo);
    target.PropertyBlock.SetColor(AdditionalShaderPropertyIDs.ColorThree, patternData.colorThree);
  }

  public static void SetProperties(IMaterialCacheTarget target, PatternData patternData,
    Func<Rot8, Texture2D> mainTexGetter = null, Func<Rot8, Texture2D> maskTexGetter = null)
  {
    if (!Cache.TryGetValue(target, out Material[] materials) ||
      !UiCache.TryGetValue(target, out Material[] uiMaterials))
    {
      Log.Error(
        $"Materials for {target} have not been created. Out of sequence material editing.");
      return;
    }

    for (int i = 0; i < materials.Length; i++)
    {
      Rot8 rot = new(i);

      Material material = materials[i];

      material.SetColor(AdditionalShaderPropertyIDs.ColorOne, patternData.color);
      material.SetColor(ShaderPropertyIDs.ColorTwo, patternData.colorTwo);
      material.SetColor(AdditionalShaderPropertyIDs.ColorThree, patternData.colorThree);

      Texture2D mainTex = material.mainTexture as Texture2D;
      if (mainTexGetter != null)
      {
        mainTex = mainTexGetter(rot);
      }
      material.mainTexture = mainTex;
      if (!mainTex)
      {
        Trace.Fail("Trying to set material properties with no main tex");
        continue;
      }

      Texture2D maskTex = maskTexGetter?.Invoke(rot);
      if (maskTex)
        material.SetTexture(ShaderPropertyIDs.MaskTex, maskTex);

      if (patternData.patternDef != PatternDefOf.Default)
      {
        float tiles = patternData.tiles;
        if (patternData.patternDef.properties.tiles.TryGetValue("All", out float allTiles))
        {
          tiles *= allTiles;
        }

        if (!Mathf.Approximately(tiles, 0))
        {
          material.SetFloat(AdditionalShaderPropertyIDs.TileNum, tiles);
        }

        if (patternData.patternDef.properties.equalize)
        {
          float scaleX = 1;
          float scaleY = 1;
          if (mainTex.width > mainTex.height)
          {
            scaleY = (float)mainTex.height / mainTex.width;
          }
          else
          {
            scaleX = (float)mainTex.width / mainTex.height;
          }

          material.SetFloat(AdditionalShaderPropertyIDs.ScaleX, scaleX);
          material.SetFloat(AdditionalShaderPropertyIDs.ScaleY, scaleY);
        }

        if (patternData.patternDef.properties.dynamicTiling)
        {
          material.SetFloat(AdditionalShaderPropertyIDs.DisplacementX,
            patternData.displacement.x);
          material.SetFloat(AdditionalShaderPropertyIDs.DisplacementY,
            patternData.displacement.y);
        }
      }

      if (patternData.patternDef.ShaderTypeDef.Shader != material.shader)
      {
        material.shader = patternData.patternDef.ShaderTypeDef.Shader;
      }

      Texture2D patternTex = patternData.patternDef[rot];
      if (patternData.patternDef.ShaderTypeDef == VehicleShaderTypeDefOf.CutoutComplexSkin)
      {
        // Null reverts to original tex. Default would calculate to red
        material.SetTexture(AdditionalShaderPropertyIDs.SkinTex, patternTex);
      }
      else if (patternData.patternDef.ShaderTypeDef ==
        VehicleShaderTypeDefOf.CutoutComplexPattern)
      {
        // Default to full red mask for full ColorOne pattern
        material.SetTexture(AdditionalShaderPropertyIDs.PatternTex, patternTex);
      }

      if (i < MaxUiArraySize)
      {
        Material uiMaterial = uiMaterials[i];
        uiMaterial.shader = material.shader;
        if (uiMaterial.shader && ShaderToUiShader.TryGetValue(uiMaterial.shader, out Shader uiShader))
        {
          uiMaterial.shader = uiShader;
        }
        uiMaterial.CopyPropertiesFromMaterial(material);
      }
    }
  }

  public static void Release(IMaterialCacheTarget target)
  {
    if (Cache.TryGetValue(target, out Material[] materials))
    {
      foreach (Material material in materials)
      {
        Object.Destroy(material);
      }

      Cache.Remove(target);
      UiCache.Remove(target);
      OnTargetRemoved?.Invoke(target);
      GraphicDatabaseRGB.Remove(target);
      Debug.Message($"Removed {target} from RGBMaterialPool and cleared all entries.");
    }
  }

  public static void DestroyAll()
  {
    foreach ((_, Material[] materials) in Cache)
    {
      foreach (Material material in materials)
      {
        Object.Destroy(material);
      }
    }

    Cache.Clear();
    UiCache.Clear();
  }

  [DebugOutput(VehicleHarmony.VehiclesLabel)]
  internal static void LogAllMaterials()
  {
    StringBuilder report = new();
    report.AppendLine($"----- Outputting Cache (Targets={Cache.Count} " +
      $"Total={Cache.Values.Sum(arr => arr.Length)}) -----");
    report.AppendLine($"Vanilla Material Count: " +
      $"{((Dictionary<Material, MaterialRequest>)AccessTools.Field(typeof(MaterialPool),
        "matDictionaryReverse").GetValue(null)).Count}");

    foreach ((IMaterialCacheTarget target, Material[] materials) in Cache)
    {
      report.AppendLine($"Target={target} Materials=\n" +
        $"{string.Join("\n", materials.Select(material => material.name))}");
    }

    report.AppendLine("----- End of Cache Output -----");

    Log.Message(report.ToString());
  }
}