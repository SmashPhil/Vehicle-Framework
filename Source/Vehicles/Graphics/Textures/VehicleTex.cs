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

		public static readonly Texture2D UnloadAll = ContentFinder<Texture2D>.Get("UI/Gizmos/UnloadAll");

		public static readonly Texture2D UnloadPassenger = ContentFinder<Texture2D>.Get("UI/Gizmos/UnloadPawn");

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

		public static readonly Dictionary<VehicleDef, Texture2D> CachedTextureIcons = new Dictionary<VehicleDef, Texture2D>();

		public static readonly Dictionary<Pair<VehicleDef, Rot8>, Texture2D> CachedVehicleTextures = new Dictionary<Pair<VehicleDef, Rot8>, Texture2D>();

		public static readonly Dictionary<VehicleDef, Graphic_Vehicle> CachedGraphics = new Dictionary<VehicleDef, Graphic_Vehicle>();

		private static readonly Dictionary<string, Texture2D> cachedTextureFilepaths = new Dictionary<string, Texture2D>();

		#if BETA
		internal static readonly Texture2D BetaButtonIcon = ContentFinder<Texture2D>.Get("Beta/BetaButton");

		internal static readonly Texture2D DiscordIcon = ContentFinder<Texture2D>.Get("Beta/discordIcon");

		internal static readonly Texture2D GithubIcon = ContentFinder<Texture2D>.Get("Beta/githubIcon");

		internal static readonly Texture2D SteamIcon = ContentFinder<Texture2D>.Get("Beta/steamIcon");
		#endif

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
							cachedTextureFilepaths.Add(iconFilePath, tex);
						}
						tasks.AppendLine("Finalizing caching");
						CachedGraphics.Add(vehicleDef, graphic);
						CachedTextureIcons.Add(vehicleDef, tex);
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

		public static Texture2D VehicleTexture(VehicleDef def, Rot8 rot)
		{
			return CachedVehicleTextures.TryGetValue(new Pair<VehicleDef, Rot8>(def, rot), CachedVehicleTextures[new Pair<VehicleDef, Rot8>(def, Rot8.North)]);
		}

		private static void SetTextureCache(VehicleDef vehicleDef, GraphicDataRGB graphicData)
		{
			var textureArray = new Texture2D[Graphic_RGB.MatCount];
			textureArray[0] = ContentFinder<Texture2D>.Get(graphicData.texPath + "_north", false);
			textureArray[0] ??= ContentFinder<Texture2D>.Get(graphicData.texPath, false);
			textureArray[1] = ContentFinder<Texture2D>.Get(graphicData.texPath + "_east", false);
			textureArray[2] = ContentFinder<Texture2D>.Get(graphicData.texPath + "_south", false);
			textureArray[3] = ContentFinder<Texture2D>.Get(graphicData.texPath + "_west", false);
			textureArray[4] = ContentFinder<Texture2D>.Get(graphicData.texPath + "_northEast", false);
			textureArray[5] = ContentFinder<Texture2D>.Get(graphicData.texPath + "_southEast", false);
			textureArray[6] = ContentFinder<Texture2D>.Get(graphicData.texPath + "_southWest", false);
			textureArray[7] = ContentFinder<Texture2D>.Get(graphicData.texPath + "_northWest", false);
			
			if (textureArray[0] is null)
			{
				if (textureArray[2] != null)
				{
					textureArray[0] = textureArray[2];
				}
				else if (textureArray[1] != null)
				{
					textureArray[0] = textureArray[1];
				}
				else if (textureArray[3] != null)
				{
					textureArray[0] = textureArray[3];
				}
			}
			if (textureArray[0] is null)
			{
				Log.Error($"Failed to find any textures at {graphicData.texPath} while constructing texture cache.");
				return;
			}
			if (textureArray[2] is null)
			{
				textureArray[2] = textureArray[0];
			}
			if (textureArray[1] is null)
			{
				if (textureArray[3] != null)
				{
					textureArray[1] = textureArray[3];
				}
				else
				{
					textureArray[1] = textureArray[0];
				}
			}
			if (textureArray[3] is null)
			{
				if (textureArray[1] != null)
				{
					textureArray[3] = textureArray[1];
				}
				else
				{
					textureArray[3] = textureArray[0];
				}
			}

			if (textureArray[4] is null)
			{
				textureArray[4] = textureArray[0];
			}
			if (textureArray[5] is null)
			{
				textureArray[5] = textureArray[2];
			}
			if(textureArray[6] is null)
			{
				textureArray[6] = textureArray[2];
			}
			if(textureArray[7] is null)
			{
				textureArray[7] = textureArray[0];
			}

			for (int i = 0; i < 8; i++)
			{
				CachedVehicleTextures.Add(new Pair<VehicleDef, Rot8>(vehicleDef, new Rot8(i)), textureArray[i]);
			}
		}
	}
}