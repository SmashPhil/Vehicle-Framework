using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class VehicleTex
	{
		public const string DefaultVehicleIconTexPath = "UI/Icons/DefaultVehicleIcon";

		public const string DefaultShuttleIconTexPath = "UI/Icons/DefaultPlaneIcon";

		public const string DefaultBoatIconTexPath = "UI/Icons/DefaultBoatIcon";

		public static readonly Texture2D DraftVehicle = ContentFinder<Texture2D>.Get("UI/Gizmos/DraftVehicle");

		public static readonly Texture2D HaltVehicle = ContentFinder<Texture2D>.Get("UI/Gizmos/HaltVehicle");

		public static readonly Texture2D UnloadAll = ContentFinder<Texture2D>.Get("UI/Gizmos/UnloadAll");

		public static readonly Texture2D UnloadPassenger = ContentFinder<Texture2D>.Get("UI/Gizmos/UnloadPawn");

		public static readonly Texture2D UnloadIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/UnloadArrow");

		public static readonly Texture2D Anchor = ContentFinder<Texture2D>.Get("UI/Gizmos/Anchor");

		public static readonly Texture2D Rename = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

		public static readonly Texture2D Recolor = ContentFinder<Texture2D>.Get("UI/ColorTools/Paintbrush");

		public static readonly Texture2D Drop = ContentFinder<Texture2D>.Get("UI/Buttons/Drop");

		public static readonly Texture2D FishingIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/FishingGizmo");

		public static readonly Texture2D CaravanIcon = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan");

		public static readonly Texture2D[] PackCargoIcon = new Texture2D[] { ContentFinder<Texture2D>.Get("UI/Gizmos/StartLoadBoat"),
																			 ContentFinder<Texture2D>.Get("UI/Gizmos/StartLoadAerial"),
																			 ContentFinder<Texture2D>.Get("UI/Gizmos/StartLoadVehicle"),
																			 BaseContent.BadTex};

		public static readonly Texture2D[] CancelPackCargoIcon = new Texture2D[] { ContentFinder<Texture2D>.Get("UI/Gizmos/CancelLoadBoat"),
																				   ContentFinder<Texture2D>.Get("UI/Gizmos/CancelLoadAerial"),
																				   ContentFinder<Texture2D>.Get("UI/Gizmos/CancelLoadVehicle"),
																				   BaseContent.BadTex};

		public static readonly Texture2D FormCaravanVehicle = ContentFinder<Texture2D>.Get("UI/Gizmos/FormCaravanVehicle");

		/// <summary>
		/// Unused - Use it if you want
		/// </summary>
		public static readonly Texture2D FormCaravanBoat = ContentFinder<Texture2D>.Get("UI/Gizmos/FormCaravanBoat");

		/// <summary>
		/// Unused - Use it if you want
		/// </summary>
		public static readonly Texture2D FormCaravanAerial = ContentFinder<Texture2D>.Get("UI/Gizmos/FormCaravanAerial");

		public static readonly Texture2D AmmoBG = ContentFinder<Texture2D>.Get("UI/Gizmos/AmmoBoxBG");

		public static readonly Texture2D ReloadIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/Reload");

		public static readonly Texture2D AutoTargetIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/AutoTarget");

		public static readonly Texture2D HaltIcon = ContentFinder<Texture2D>.Get("UI/Commands/Halt");

		public static readonly List<Texture2D> FireIcons = ContentFinder<Texture2D>.GetAllInFolder("Things/Special/Fire").ToList();

		public static readonly Texture2D WarningIcon = ContentFinder<Texture2D>.Get("UI/Icons/WarningIcon");

		public static readonly Texture2D FullBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.35f, 0.35f, 0.2f));

		public static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(Color.black);

		public static readonly Texture2D TargetLevelArrow = ContentFinder<Texture2D>.Get("UI/Misc/BarInstantMarkerRotated");

		public static readonly Texture2D SwitchLeft = ContentFinder<Texture2D>.Get("UI/ColorTools/SwitchLeft");

		public static readonly Texture2D SwitchRight = ContentFinder<Texture2D>.Get("UI/ColorTools/SwitchRight");

		public static readonly Texture2D ReverseIcon = ContentFinder<Texture2D>.Get("UI/ColorTools/SwapColors");

		public static readonly Texture2D FlickerIcon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower");

		public static readonly Texture2D DismissTex = ContentFinder<Texture2D>.Get("UI/Commands/DismissShuttle");
		
		public static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment");
		
		public static readonly Texture2D LaunchCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

		public static readonly Texture2D ResetPage = ContentFinder<Texture2D>.Get("UI/Settings/ResetPage");

		public static readonly Texture2D ResetAll = ContentFinder<Texture2D>.Get("UI/Settings/ResetAll");

		public static readonly Texture2D Settings = ContentFinder<Texture2D>.Get("UI/Settings/Settings");

		public static readonly Texture2D ExportSettings = ContentFinder<Texture2D>.Get("UI/Settings/ExportSettings");

		public static readonly Texture2D TradeCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/Trade");

		public static readonly Texture2D OfferGiftsCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/OfferGifts");

		public static readonly Texture2D AltitudeMeter = ContentFinder<Texture2D>.Get("UI/Gizmos/AltitudeMeter");

		public static readonly Texture2D TradeArrow = ContentFinder<Texture2D>.Get("UI/Widgets/TradeArrow");

		public static readonly Texture2D ColorPicker = ContentFinder<Texture2D>.Get("UI/ColorTools/ColorCog");

		public static readonly Texture2D ColorHue = ContentFinder<Texture2D>.Get("UI/ColorTools/ColorHue");

		public static readonly Texture2D BlankPattern = ContentFinder<Texture2D>.Get("Graphics/Patterns/Default/Blank");

		public static readonly Texture2D LeftArrow = ContentFinder<Texture2D>.Get("UI/Icons/ArrowLeft");

		public static readonly Texture2D RightArrow = ContentFinder<Texture2D>.Get("UI/Icons/ArrowRight");

		public static readonly Material LandingTargeterMat = MaterialPool.MatFrom("UI/Icons/LandingTargeter", ShaderDatabase.Transparent);

		public static readonly Material RangeCircle_ExtraWide = MaterialPool.MatFrom("UI/RangeField_ExtraWide", ShaderDatabase.MoteGlow);

		public static readonly Material RangeCircle_Wide = MaterialPool.MatFrom("UI/RangeField_Wide", ShaderDatabase.MoteGlow);

		public static readonly Material RangeCircle_Mid = MaterialPool.MatFrom("UI/RangeField_Mid", ShaderDatabase.MoteGlow);

		public static readonly Material RangeCircle_Close = MaterialPool.MatFrom("UI/RangeField_Close", ShaderDatabase.MoteGlow);

		public static readonly Texture2D DefaultVehicleIcon = ContentFinder<Texture2D>.Get(DefaultVehicleIconTexPath);

		public static readonly Dictionary<VehicleDef, Texture2D> CachedTextureIcons = new Dictionary<VehicleDef, Texture2D>();

		public static readonly Dictionary<VehicleDef, string> CachedTextureIconPaths = new Dictionary<VehicleDef, string>();

		public static readonly Dictionary<(VehicleDef, Rot4), Texture2D> CachedVehicleTextures = new Dictionary<(VehicleDef, Rot4), Texture2D>();

		public static readonly Dictionary<VehicleDef, Graphic_Vehicle> CachedGraphics = new Dictionary<VehicleDef, Graphic_Vehicle>();

		private static readonly Dictionary<string, Texture2D> cachedTextureFilepaths = new Dictionary<string, Texture2D>();

		static VehicleTex()
		{
			StringBuilder tasks = new StringBuilder();
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				tasks.Clear();
				tasks.AppendLine($"Generating TextureCache for {vehicleDef.defName}");
				try
				{
					tasks.Append("Creating icon...");
					string iconFilePath = vehicleDef.properties.iconTexPath;
					if (iconFilePath.NullOrEmpty())
					{
						switch (vehicleDef.vehicleType)
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
						}
					}
					tasks.AppendLine("Icon created");
					tasks.AppendLine("Creating BodyGraphicData and cached graphics...");
					if (vehicleDef.graphicData is GraphicDataRGB graphicDataRGB)
					{
						Texture2D tex;
						var graphicData = new GraphicDataRGB();
						graphicData.CopyFrom(graphicDataRGB);
						Graphic_Vehicle graphic = graphicData.Graphic as Graphic_Vehicle;
						tasks.AppendLine("Setting TextureCache...");
						SetTextureCache(vehicleDef, graphicData);
						tasks.AppendLine("Finalized TextureCache");
						if (cachedTextureFilepaths.ContainsKey(iconFilePath))
						{
							tex = cachedTextureFilepaths[iconFilePath];
						}
						else
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
						SmashLog.Error($"Unable to create GraphicData of type <type>{vehicleDef.graphicData?.GetType().ToStringSafe() ?? "Null"} for {vehicleDef.defName}.\n{tasks}");
					}
				}
				catch (Exception ex)
				{
					Log.Error($"Exception thrown while trying to generate cached textures. Exception=\"{ex.Message}\"\n-----------------Tasks-----------------\n{tasks}");
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
			texNorth ??= ContentFinder<Texture2D>.Get(graphicData.texPath, true);
			if (!texNorth)
			{
				throw new Exception($"Unable to locate north texture for {vehicleDef}");
			}
			Texture2D texEast = ContentFinder<Texture2D>.Get(graphicData.texPath + "_east", false);
			Texture2D texSouth = ContentFinder<Texture2D>.Get(graphicData.texPath + "_south", false);
			Texture2D texWest = ContentFinder<Texture2D>.Get(graphicData.texPath + "_west", false);

			CachedVehicleTextures[(vehicleDef, Rot4.North)] = texNorth;
			if (texEast != null)
			{
				CachedVehicleTextures[(vehicleDef, Rot4.East)] = texEast;
			}
			if (texSouth != null)
			{
				CachedVehicleTextures[(vehicleDef, Rot4.South)] = texSouth;
			}
			if (texWest != null)
			{
				CachedVehicleTextures[(vehicleDef, Rot4.West)] = texWest;
			}
		}
	}
}