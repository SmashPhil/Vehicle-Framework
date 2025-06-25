using System;
using System.Collections.Generic;
using System.Text;
using SmashTools;
using UnityEngine;
using Verse;

namespace Vehicles;

[StaticConstructorOnStartup]
public static class VehicleTex
{
  internal const string DefaultVehicleIconTexPath = "UI/Icons/DefaultVehicleIcon";
  private const string DefaultShuttleIconTexPath = "UI/Icons/DefaultPlaneIcon";
  private const string DefaultBoatIconTexPath = "UI/Icons/DefaultBoatIcon";

#region Gizmos

  public static readonly Texture2D DefaultVehicleIcon =
    ContentFinder<Texture2D>.Get(DefaultVehicleIconTexPath);

  public static readonly Texture2D DraftVehicle =
    ContentFinder<Texture2D>.Get("UI/Gizmos/DraftVehicle");

  public static readonly Texture2D HaltVehicle =
    ContentFinder<Texture2D>.Get("UI/Gizmos/HaltVehicle");

  public static readonly Texture2D
    UnloadAll = ContentFinder<Texture2D>.Get("UI/Gizmos/UnloadAll");

  public static readonly Texture2D UnloadIcon =
    ContentFinder<Texture2D>.Get("UI/Gizmos/UnloadArrow");

  public static readonly Texture2D StashVehicle =
    ContentFinder<Texture2D>.Get("UI/Gizmos/StashVehicle");

  public static readonly Texture2D FishingIcon =
    ContentFinder<Texture2D>.Get("UI/Gizmos/FishingGizmo");

  public static readonly Texture2D HaulPawnToVehicle =
    ContentFinder<Texture2D>.Get("UI/Gizmos/HaulPawnToVehicle");

  public static readonly Texture2D DeployVehicle =
    ContentFinder<Texture2D>.Get("UI/Gizmos/Gizmo_DeployVehicle");

  public static readonly Texture2D UndeployVehicle =
    ContentFinder<Texture2D>.Get("UI/Gizmos/Gizmo_UndeployVehicle");

  public static readonly Texture2D[] PackCargoIcon =
  [
    ContentFinder<Texture2D>.Get("UI/Gizmos/StartLoadBoat"),
    ContentFinder<Texture2D>.Get("UI/Gizmos/StartLoadAerial"),
    ContentFinder<Texture2D>.Get("UI/Gizmos/StartLoadVehicle"),
    BaseContent.BadTex
  ];

  public static readonly Texture2D[] CancelPackCargoIcon =
  [
    ContentFinder<Texture2D>.Get("UI/Gizmos/CancelLoadBoat"),
    ContentFinder<Texture2D>.Get("UI/Gizmos/CancelLoadAerial"),
    ContentFinder<Texture2D>.Get("UI/Gizmos/CancelLoadVehicle"),
    BaseContent.BadTex
  ];

  public static readonly Texture2D FormCaravanVehicle =
    ContentFinder<Texture2D>.Get("UI/Gizmos/FormCaravanVehicle");

  public static readonly Texture2D RepairVehicles =
    ContentFinder<Texture2D>.Get("UI/Gizmos/Gizmo_RepairVehicles");

  public static readonly Texture2D ReloadIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/Reload");

  public static readonly Texture2D AutoTargetIcon =
    ContentFinder<Texture2D>.Get("UI/Gizmos/AutoTarget");

  public static readonly Texture2D HaltIcon = ContentFinder<Texture2D>.Get("UI/Commands/Halt");

#endregion Gizmos


#region ColorTools

  public static readonly Texture2D SwitchLeft =
    ContentFinder<Texture2D>.Get("UI/ColorTools/SwitchLeft");

  public static readonly Texture2D SwitchRight =
    ContentFinder<Texture2D>.Get("UI/ColorTools/SwitchRight");

  public static readonly Texture2D ReverseIcon =
    ContentFinder<Texture2D>.Get("UI/ColorTools/SwapColors");

  public static readonly Texture2D Recolor =
    ContentFinder<Texture2D>.Get("UI/ColorTools/Paintbrush");

  public static readonly Texture2D ColorPicker =
    ContentFinder<Texture2D>.Get("UI/ColorTools/ColorCog");

  public static readonly Texture2D ColorHue =
    ContentFinder<Texture2D>.Get("UI/ColorTools/ColorHue");

  public static readonly Texture2D LeftArrow = ContentFinder<Texture2D>.Get("UI/Icons/ArrowLeft");
  public static readonly Texture2D RightArrow = ContentFinder<Texture2D>.Get("UI/Icons/ArrowRight");

#endregion ColorTools


#region Settings

  public static readonly Texture2D ResetPage =
    ContentFinder<Texture2D>.Get("UI/Settings/ResetPage");

  public static readonly Texture2D
    Settings = ContentFinder<Texture2D>.Get("UI/Settings/Settings");

#endregion

#region WorldMap

  public static readonly Texture2D RoutePlanner =
    ContentFinder<Texture2D>.Get("UI/Gizmos/VehicleRoutePlanner");

#endregion

  public static readonly Dictionary<VehicleDef, Texture2D> CachedTextureIcons = [];
  public static readonly Dictionary<VehicleDef, string> CachedTextureIconPaths = [];
  private static readonly Dictionary<(VehicleDef, Rot4), Texture2D> CachedVehicleTextures = [];
  private static readonly Dictionary<VehicleDef, Graphic_Vehicle> CachedGraphics = [];
  private static readonly Dictionary<string, Texture2D> cachedTextureFilepaths = [];

  static VehicleTex()
  {
    StringBuilder tasks = new();
    foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefsListForReading)
    {
      tasks.Clear();
      tasks.AppendLine($"Generating TextureCache for {vehicleDef.defName}");
      try
      {
        tasks.Append("Creating icon...");
        string iconFilePath = vehicleDef.properties.iconTexPath;
        if (iconFilePath.NullOrEmpty())
        {
          switch (vehicleDef.type)
          {
            case VehicleType.Land:
              iconFilePath = DefaultVehicleIconTexPath;
            break;
            case VehicleType.Sea:
              iconFilePath = DefaultBoatIconTexPath;
            break;
            case VehicleType.Air:
              iconFilePath = DefaultShuttleIconTexPath;
            break;
            default:
              iconFilePath = DefaultVehicleIconTexPath;
            break;
          }
        }
        tasks.AppendLine("Icon created");
        tasks.AppendLine("Creating BodyGraphicData and cached graphics...");
        if (vehicleDef.graphicData is not null)
        {
          GraphicDataRGB graphicData = vehicleDef.graphicData;
          Graphic_Vehicle graphic = graphicData.Graphic as Graphic_Vehicle;
          tasks.AppendLine("Setting TextureCache...");
          SetTextureCache(vehicleDef, graphicData);
          tasks.AppendLine("Finalized TextureCache");
          if (!cachedTextureFilepaths.TryGetValue(iconFilePath, out Texture2D tex))
          {
            tex = ContentFinder<Texture2D>.Get(iconFilePath);
            cachedTextureFilepaths[iconFilePath] = tex;
          }
          tasks.AppendLine("Finalizing caching");
          CachedGraphics[vehicleDef] = graphic;
          CachedTextureIcons[vehicleDef] = tex;
          CachedTextureIconPaths[vehicleDef] = iconFilePath;
        }
        else
        {
          SmashLog.Error(
            $"Unable to create GraphicData of type <type>{vehicleDef.graphicData?.GetType().ToStringSafe() ?? "Null"} for {vehicleDef.defName}.\n{tasks}");
        }
      }
      catch (Exception ex)
      {
        Log.Error(
          $"Exception thrown while trying to generate cached textures. Exception=\"{ex}\"\n-----------------Tasks-----------------\n{tasks}");
      }
    }
  }

  public static Texture2D VehicleTexture(VehicleDef def, Rot4 rot, out float rotate)
  {
    rotate = 0;
    if (CachedVehicleTextures.TryGetValue((def, rot), out Texture2D texture))
    {
      return texture;
    }
    rotate = rot.AsAngle;
    return CachedVehicleTextures[(def, Rot4.North)];
  }

  private static void SetTextureCache(VehicleDef vehicleDef, GraphicDataRGB graphicData)
  {
    Texture2D texNorth = ContentFinder<Texture2D>.Get(graphicData.texPath + "_north", false);
    texNorth ??= ContentFinder<Texture2D>.Get(graphicData.texPath);
    if (!texNorth)
    {
      throw new Exception($"Unable to locate north texture for {vehicleDef}");
    }
    Texture2D texEast = ContentFinder<Texture2D>.Get(graphicData.texPath + "_east", false);
    Texture2D texSouth = ContentFinder<Texture2D>.Get(graphicData.texPath + "_south", false);
    Texture2D texWest = ContentFinder<Texture2D>.Get(graphicData.texPath + "_west", false);

    CachedVehicleTextures[(vehicleDef, Rot4.North)] = texNorth;
    if (texEast)
    {
      CachedVehicleTextures[(vehicleDef, Rot4.East)] = texEast;
    }
    if (texSouth)
    {
      CachedVehicleTextures[(vehicleDef, Rot4.South)] = texSouth;
    }
    if (texWest)
    {
      CachedVehicleTextures[(vehicleDef, Rot4.West)] = texWest;
    }
  }
}