using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using LudeonTK;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using Object = UnityEngine.Object;

namespace Vehicles;

public static class RGBMaterialPool
{
  private static readonly Dictionary<IMaterialCacheTarget, Material[]> cache = [];

  public static event Action<IMaterialCacheTarget> OnTargetCached;
  public static event Action<IMaterialCacheTarget> OnTargetRemoved;

  public static int Count => cache.Count;

  public static int TotalMaterials => cache.Values.Sum(mats => mats.Length);

  // For testing only
  internal static List<IMaterialCacheTarget> AllCacheTargets => cache.Keys.ToList();

  public static bool TargetCached(IMaterialCacheTarget target)
  {
    return cache.ContainsKey(target);
  }

  public static Material[] GetAll(IMaterialCacheTarget target)
  {
    return cache.TryGetValue(target);
  }

  public static Material Get(IMaterialCacheTarget target, Rot8 rot)
  {
    if (cache.TryGetValue(target, out Material[] materials))
    {
      if (rot.AsInt >= materials.Length)
      {
        Log.Error(
          $"Attempting to fetch material out of bounds. Target={target} Rot8={rot}. " +
          $"Max count for {target} is {target.MaterialCount}");
        return null;
      }

      return materials[rot.AsInt];
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
    int renderQueue = 0,
    List<ShaderParameter> shaderParameters = null)
  {
    if (cache.ContainsKey(target) || patternDef == null)
      return;

    Material[] materials = new Material[target.MaterialCount];
    for (int i = 0; i < materials.Length; i++)
    {
      Rot8 rot = new Rot8(i);
      Material material = new Material(patternDef.ShaderTypeDef.Shader)
      {
        name = target.Name + rot.ToStringNamed(),
        mainTexture = null,
        color = Color.clear,
      };

      if (renderQueue != 0)
      {
        material.renderQueue = renderQueue;
      }

      if (!shaderParameters.NullOrEmpty())
      {
        for (int p = 0; p < shaderParameters.Count; p++)
        {
          shaderParameters[p].Apply(material);
        }
      }

      materials[i] = material;
    }

    cache.Add(target, materials);
    OnTargetCached?.Invoke(target);
  }

  public static void SetPropertyBlock(IMaterialCacheTarget target, PatternData patternData,
    Texture2D mainTex, Texture2D maskTex, Rot8 rot)
  {
    if (!cache.ContainsKey(target))
    {
      Log.Error(
        $"Materials for {target} have not been created. Out of sequence material editing.");
      return;
    }
    MaterialPropertyBlock block = target.PropertyBlock;
    Assert.IsNotNull(block);
    block.Clear();

    block.SetTexture("_MainTex", mainTex);

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
    if (!cache.TryGetValue(target, out Material[] materials))
    {
      Log.Error(
        $"Materials for {target} have not been created. Out of sequence material editing.");
      return;
    }

    for (int i = 0; i < materials.Length; i++)
    {
      Material material = materials[i];

      material.SetColor(AdditionalShaderPropertyIDs.ColorOne, patternData.color);
      material.SetColor(ShaderPropertyIDs.ColorTwo, patternData.colorTwo);
      material.SetColor(AdditionalShaderPropertyIDs.ColorThree, patternData.colorThree);

      Rot8 rot = new(i);
      Texture2D mainTex = material.mainTexture as Texture2D;
      if (mainTexGetter != null)
      {
        mainTex = mainTexGetter(rot);
      }

      Texture2D maskTex = maskTexGetter?.Invoke(rot);
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
        //Null reverts to original tex. Default would calculate to red
        material.SetTexture(AdditionalShaderPropertyIDs.SkinTex, patternTex);
      }
      else if (patternData.patternDef.ShaderTypeDef ==
        VehicleShaderTypeDefOf.CutoutComplexPattern)
      {
        //Default to full red mask for full ColorOne pattern
        material.SetTexture(AdditionalShaderPropertyIDs.PatternTex, patternTex);
      }

      material.mainTexture = mainTex;
      if (maskTex != null)
      {
        material.SetTexture(ShaderPropertyIDs.MaskTex, maskTex);
      }

      material.SetColor(AdditionalShaderPropertyIDs.ColorOne, patternData.color);
      material.SetColor(ShaderPropertyIDs.ColorTwo, patternData.colorTwo);
      material.SetColor(AdditionalShaderPropertyIDs.ColorThree, patternData.colorThree);
    }
  }

  public static void Release(IMaterialCacheTarget target)
  {
    if (cache.TryGetValue(target, out Material[] materials))
    {
      foreach (Material material in materials)
      {
        Object.Destroy(material);
      }

      cache.Remove(target);
      OnTargetRemoved?.Invoke(target);
      GraphicDatabaseRGB.Remove(target);
      Debug.Message(
        $"<success>{VehicleHarmony.LogLabel}</success> Removed {target} from RGBMaterialPool and " +
        $"cleared all entries.");
    }
  }

  public static void DestroyAll()
  {
    foreach ((_, Material[] materials) in cache)
    {
      foreach (Material material in materials)
      {
        Object.Destroy(material);
      }
    }

    cache.Clear();
  }

  [DebugOutput(VehicleHarmony.VehiclesLabel)]
  internal static void LogAllMaterials()
  {
    StringBuilder report = new();
    report.AppendLine($"----- Outputting Cache (Targets={cache.Count} " +
      $"Total={cache.Values.Sum(arr => arr.Length)}) -----");
    report.AppendLine($"Vanilla Material Count: " +
      $"{((Dictionary<Material, MaterialRequest>)AccessTools.Field(typeof(MaterialPool),
        "matDictionaryReverse").GetValue(null)).Count}");

    foreach ((IMaterialCacheTarget target, Material[] materials) in cache)
    {
      report.AppendLine($"Target={target} Materials=\n" +
        $"{string.Join("\n", materials.Select(material => material.name))}");
    }

    report.AppendLine($"----- End of Cache Output -----");

    Log.Message(report.ToString());
  }
}