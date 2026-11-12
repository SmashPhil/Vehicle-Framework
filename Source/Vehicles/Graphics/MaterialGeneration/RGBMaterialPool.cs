using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLib.Performance;
using HarmonyLib;
using JetBrains.Annotations;
using LudeonTK;
using SmashTools;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;
using static Vehicles.Config.FeatureFlags;
using Object = UnityEngine.Object;
using Trace = SmashTools.Trace;
using UiMaterialMap = System.Collections.Generic.Dictionary<UnityEngine.Shader, UnityEngine.Material[]>;

namespace Vehicles;

// TODO 1.8 - Rename for style convention
[PublicAPI]
[StaticConstructorOnStartup]
public static class RGBMaterialPool
{
  // UI Only shows Rot4 rotations, there's no need to allocate for diagonals
  private const int MaxUiArraySize = Rot8.WestInt + 1;

  private static readonly Dictionary<IMaterialCacheTarget, Material[]> Cache = [];
  private static readonly Dictionary<IMaterialCacheTarget, UiMaterialMap> UiCache = [];

  public static event Action<IMaterialCacheTarget> OnTargetCached;
  public static event Action<IMaterialCacheTarget> OnTargetRemoved;

  public delegate Texture2D TextureGetter(Rot8 rot);

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

  private static Material[] GetOrCreateUiMaterials(IMaterialCacheTarget target, Shader source)
  {
    if (!ShaderDatabase.TryGetUIShader(source, out Shader uiShader))
    {
      Log.Error($"No UI shader found for {source.name}");
      return null;
    }
    UiMaterialMap map = UiCache.GetOrInsert(target);
    if (!map.TryGetValue(uiShader, out Material[] materials))
    {
      materials = CreateUiMaterials(target, uiShader);
      map[uiShader] = materials;
    }
    return materials;
  }

  public static Material PrepareUiMaterial(IMaterialCacheTarget target, Rot4 rot, PatternData data)
  {
    Assert.IsTrue(UnityThread.IsInMainThread);
    if (IsFeatureEnabled(BlitTexturePortraits))
    {
      return Get(target, rot);
    }
    Shader shader = data.patternDef.ShaderTypeDef.Shader;
    Material[] materials = GetOrCreateUiMaterials(target, shader);
    if (materials is not { Length: > 0 })
    {
      Trace.Fail("Trying to get ui material with no materials cached.");
      return null;
    }
    int index = materials.Length switch
    {
      // Single
      1 => 0,
      // Graphic_Multi
      4 => rot.AsInt,
      _ => throw new IndexOutOfRangeException(
        $"Trying to fetch texture from cache for rotation {rot}. Materials cached = {materials.Length}")
    };
    Material material = materials[index];
    SetMaterialProperties(material, data, new Rot8(index));
    return material;
  }

  private static Material[] CreateUiMaterials(IMaterialCacheTarget target, Shader shader)
  {
    if (!Cache.TryGetValue(target, out Material[] materials))
    {
      Log.Error("Trying to copy ui materials from uncached target.");
      return null;
    }
    int size = Math.Min(materials.Length, MaxUiArraySize);
    Material[] uiMaterials = new Material[size];
    for (int i = 0; i < size; i++)
    {
      Material material = materials[i];
      Material uiMaterial = new(material)
      {
        name = $"{material.name}_ui",
        shader = shader
      };
      uiMaterial.CopyPropertiesFromMaterial(material);
      uiMaterials[i] = uiMaterial;
    }
    return uiMaterials;
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
    for (int i = 0; i < materials.Length; i++)
    {
      Rot8 rot = new(i);
      Material material = new(patternDef.ShaderTypeDef.Shader)
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
        foreach (ShaderParameter shaderParameter in shaderParameters)
        {
          shaderParameter.Apply(material);
        }
      }
      materials[i] = material;
    }
    Cache.Add(target, materials);
    GetOrCreateUiMaterials(target, patternDef.ShaderTypeDef.Shader);
    OnTargetCached?.Invoke(target);
  }

  public static void SetProperties(IMaterialCacheTarget target, PatternData patternData)
  {
    if (!Cache.TryGetValue(target, out Material[] materials))
    {
      Trace.Fail($"Materials for {target} have not been created. Out of sequence material editing.");
      return;
    }

    for (int i = 0; i < materials.Length; i++)
    {
      Rot8 rot = new(i);
      Material material = materials[i];
      if (patternData.patternDef.ShaderTypeDef.Shader != material.shader)
      {
        material.shader = patternData.patternDef.ShaderTypeDef.Shader;
      }
      SetMaterialProperties(material, patternData, rot);
    }
  }

  public static void SetTextures(IMaterialCacheTarget target, TextureGetter getMainTex, TextureGetter getMaskTex)
  {
    if (!Cache.TryGetValue(target, out Material[] materials))
    {
      Trace.Fail($"Materials for {target} have not been created. Out of sequence material editing.");
      return;
    }

    for (int i = 0; i < target.MaterialCount; i++)
    {
      Rot8 rot = new(i);
      Material material = materials[rot.AsInt];
      Texture2D mainTex = getMainTex(rot);
      Texture2D maskTex = getMaskTex(rot);
      material.mainTexture = mainTex;
      material.SetTexture(ShaderPropertyIDs.MaskTex, maskTex);
      if (UiCache.TryGetValue(target, out UiMaterialMap map))
      {
        foreach (Material[] uiMaterials in map.Values)
        {
          if (rot.AsInt >= uiMaterials.Length)
            continue;

          Material uiMaterial = uiMaterials[rot.AsInt];
          uiMaterial.mainTexture = mainTex;
          uiMaterial.SetTexture(ShaderPropertyIDs.MaskTex, maskTex);
        }
      }
    }
  }

  private static void SetMaterialProperties(Material material, PatternData data, Rot8 rot)
  {
    material.SetColor(AdditionalShaderPropertyIDs.ColorOne, data.color);
    material.SetColor(ShaderPropertyIDs.ColorTwo, data.colorTwo);
    material.SetColor(AdditionalShaderPropertyIDs.ColorThree, data.colorThree);

    Texture2D mainTex = material.mainTexture as Texture2D;
    if (!mainTex)
    {
      Trace.Fail("Trying to set material properties with no main tex");
      return;
    }

    float tiles = data.tiles;
    if (data.patternDef.properties.tiles.TryGetValue("All", out float allTiles))
    {
      tiles *= allTiles;
    }
    material.SetFloat(AdditionalShaderPropertyIDs.TileNum, tiles);

    float scaleX = 1;
    float scaleY = 1;
    if (data.patternDef.properties.equalize)
    {
      if (mainTex.width > mainTex.height)
      {
        scaleY = (float)mainTex.height / mainTex.width;
      }
      else
      {
        scaleX = (float)mainTex.width / mainTex.height;
      }
    }
    material.SetFloat(AdditionalShaderPropertyIDs.ScaleX, scaleX);
    material.SetFloat(AdditionalShaderPropertyIDs.ScaleY, scaleY);

    float displacementX = 0;
    float displacementY = 0;
    if (data.patternDef.properties.dynamicTiling)
    {
      displacementX = data.displacement.x;
      displacementY = data.displacement.y;
    }
    material.SetFloat(AdditionalShaderPropertyIDs.DisplacementX, displacementX);
    material.SetFloat(AdditionalShaderPropertyIDs.DisplacementY, displacementY);

    Texture2D patternTex = data.patternDef[rot];
    if (data.patternDef.ShaderTypeDef == VehicleShaderTypeDefOf.CutoutComplexSkin)
    {
      // Null reverts to original tex. Default would calculate to red
      material.SetTexture(AdditionalShaderPropertyIDs.SkinTex, patternTex);
    }
    else if (data.patternDef.ShaderTypeDef == VehicleShaderTypeDefOf.CutoutComplexPattern)
    {
      // Default to full red mask for full ColorOne pattern
      material.SetTexture(AdditionalShaderPropertyIDs.PatternTex, patternTex);
    }
  }

  public static void SetPropertyBlock(IMaterialCacheTarget target, PatternData data,
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

    if (data.patternDef != PatternDefOf.Default)
    {
      float tiles = data.tiles;
      if (data.patternDef.properties.tiles.TryGetValue("All", out float allTiles))
      {
        tiles *= allTiles;
      }
      block.SetFloat(AdditionalShaderPropertyIDs.TileNum, tiles);

      float scaleX = 1;
      float scaleY = 1;
      if (data.patternDef.properties.equalize)
      {
        if (mainTex.width > mainTex.height)
        {
          scaleY = (float)mainTex.height / mainTex.width;
        }
        else
        {
          scaleX = (float)mainTex.width / mainTex.height;
        }
      }
      block.SetFloat(AdditionalShaderPropertyIDs.ScaleX, scaleX);
      block.SetFloat(AdditionalShaderPropertyIDs.ScaleY, scaleY);

      float displacementX = 0;
      float displacementY = 0;
      if (data.patternDef.properties.dynamicTiling)
      {
        displacementX = data.displacement.x;
        displacementY = data.displacement.y;
      }
      block.SetFloat(AdditionalShaderPropertyIDs.DisplacementX, displacementX);
      block.SetFloat(AdditionalShaderPropertyIDs.DisplacementY, displacementY);
    }

    Texture2D patternTex = data.patternDef[rot];
    if (data.patternDef.ShaderTypeDef == VehicleShaderTypeDefOf.CutoutComplexSkin)
    {
      // Null reverts to original tex. Default would calculate to red
      target.PropertyBlock.SetTexture(AdditionalShaderPropertyIDs.SkinTex, patternTex);
    }
    else if (data.patternDef.ShaderTypeDef == VehicleShaderTypeDefOf.CutoutComplexPattern)
    {
      // Default to full red mask for full ColorOne pattern
      target.PropertyBlock.SetTexture(AdditionalShaderPropertyIDs.PatternTex, patternTex);
    }

    if (maskTex != null)
    {
      target.PropertyBlock.SetTexture(ShaderPropertyIDs.MaskTex, maskTex);
    }

    target.PropertyBlock.SetColor(AdditionalShaderPropertyIDs.ColorOne, data.color);
    target.PropertyBlock.SetColor(ShaderPropertyIDs.ColorTwo, data.colorTwo);
    target.PropertyBlock.SetColor(AdditionalShaderPropertyIDs.ColorThree, data.colorThree);
  }

  public static void Release(IMaterialCacheTarget target)
  {
    if (Cache.Remove(target, out Material[] materials))
    {
      DestroyAll(materials);
      if (UiCache.Remove(target, out UiMaterialMap uiCache))
      {
        DestroyAll(uiCache);
      }
      OnTargetRemoved?.Invoke(target);
      GraphicDatabaseRGB.Remove(target);
      Debug.Message($"Removed {target} from RGBMaterialPool and cleared all entries.");
    }
  }

  public static void DestroyAll()
  {
    foreach ((_, Material[] materials) in Cache)
    {
      DestroyAll(materials);
    }
    foreach ((_, UiMaterialMap cache) in UiCache)
    {
      DestroyAll(cache);
    }
    Cache.Clear();
    UiCache.Clear();
  }

  private static void DestroyAll(Material[] materials)
  {
    foreach (Material material in materials)
    {
      Object.Destroy(material);
    }
  }

  private static void DestroyAll(UiMaterialMap cache)
  {
    foreach ((_, Material[] materials) in cache)
    {
      foreach (Material material in materials)
      {
        Object.Destroy(material);
      }
    }
  }

  [DebugOutput(VehicleHarmony.VehiclesLabel)]
  internal static void LogAllMaterials()
  {
    StringBuilder report = new();
    report.AppendLine($"----- Outputting Cache (Targets={Cache.Count} " +
      $"Total={Cache.Values.Sum(arr => arr.Length)}) -----");
    report.AppendLine($"Vanilla Material Count: " +
      $"{((Dictionary<Material, MaterialRequest>)AccessTools.Field(typeof(MaterialPool),
        "matDictionaryReverse").GetValue(null))!.Count}");

    foreach ((IMaterialCacheTarget target, Material[] materials) in Cache)
    {
      report.AppendLine($"Target={target} Materials=\n" +
        $"{string.Join("\n", materials.Select(material => material.name))}");
    }

    report.AppendLine("----- End of Cache Output -----");

    Log.Message(report.ToString());
  }
}