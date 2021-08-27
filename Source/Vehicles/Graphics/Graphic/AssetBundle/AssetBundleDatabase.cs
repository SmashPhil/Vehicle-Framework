using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;
using SmashTools.Xml;

namespace Vehicles
{
	/// <summary>
	/// AssetBundle loader
	/// </summary>
	[StaticConstructorOnStartup]
	public static class AssetBundleDatabase
	{
		/// <summary>
		/// AssetBundle version loader
		/// </summary>
		private static readonly Dictionary<string, string> bundleBuildVersionManifest = new Dictionary<string, string>()
		{
			{"1.3", "2019.4.26f1"}
		};

		private static readonly Dictionary<string, Shader> shaderLookup = new Dictionary<string, Shader>();
		private static readonly Dictionary<string, Texture2D> textureLookup = new Dictionary<string, Texture2D>();

		private static readonly List<string> loadFoldersChecked = new List<string>();

		private static readonly string VehicleAssetBundlePath = @"Assets\vehicleassets";

		public static readonly AssetBundle VehicleAssetBundle;

		public static readonly Shader CutoutComplexRGB;

		public static readonly Shader CutoutComplexPattern;

		public static readonly Texture2D MouseHandOpen;

		public static readonly Texture2D MouseHandClosed;

		static AssetBundleDatabase()
		{
			string version = $"{VersionControl.CurrentMajor}.{VersionControl.CurrentMinor}";
			if (bundleBuildVersionManifest.TryGetValue(version, out string currentVersion))
			{
				if (currentVersion != Application.unityVersion)
				{
					Log.Warning($"{VehicleHarmony.LogLabel} Unity Version {Application.unityVersion} does not match registered version for AssetBundles being loaded. You may encounter problems. Please notify report it on the workshop page so that I may update the UnityVersion supported for this AssetBundle.");
				}
			}
			else
			{
				SmashLog.Warning($"{VehicleHarmony.LogLabel} Unable to locate cached UnityVersion: {version}. This mod might not support this version.");
			}
			
			List<string> loadFolders = FilePaths.ModFoldersForVersion(VehicleMod.settings.Mod.Content);
			try
			{
				loadFoldersChecked.Clear();
				foreach (string folder in loadFolders)
				{
					loadFoldersChecked.Add(folder);
					string versionFilePath = Path.Combine(VehicleMod.settings.Mod.Content.RootDir, folder, VehicleAssetBundlePath);
					if (File.Exists(versionFilePath))
					{
						VehicleAssetBundle = AssetBundle.LoadFromFile(versionFilePath);
						if (VehicleAssetBundle is null)
						{
							SmashLog.Error($"Unable to load <type>VehicleAssetBundle</type> asset at {versionFilePath}");
							throw new IOException();
						}

						CutoutComplexRGB = LoadAssetBundleShader("Assets/Shaders/ShaderRGB.shader");
						CutoutComplexPattern = LoadAssetBundleShader("Assets/Shaders/ShaderRGBPattern.shader");
						return;
					}
				}
				throw new IOException("Unable to find ShaderBundle asset in any load folder.");
			}
			catch (Exception ex)
			{
				SmashLog.Error($"Unable to load AssetBundle.\nException = {ex.Message}\nFoldersSearched={loadFoldersChecked.ToCommaList()}");
			}
			finally
			{
				SmashLog.Message($"{VehicleHarmony.LogLabel} Importing additional assets. UnityVersion={Application.unityVersion} Status: {AssetBundleLoadMessage(VehicleAssetBundle)}");
			}
		}

		/// <summary>
		/// Status message
		/// </summary>
		/// <param name="assetBundle"></param>
		private static string AssetBundleLoadMessage(AssetBundle assetBundle) => assetBundle != null ? "<success>successfully loaded.</success>" : "<error>failed to load.</error>";

		/// <summary>
		/// Shader load from AssetBundle
		/// </summary>
		/// <param name="path"></param>
		public static Shader LoadAssetBundleShader(string path)
		{
			if (shaderLookup.TryGetValue(path, out Shader shader))
			{
				return shader;
			}
			return (Shader)VehicleAssetBundle.LoadAsset(path);
		}

		/// <summary>
		/// Texture load from AssetBundle
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static Texture2D LoadAssetBundleTexture(string path)
		{
			if (textureLookup.TryGetValue(path, out Texture2D texture))
			{
				return texture;
			}
			return (Texture2D)VehicleAssetBundle.LoadAsset(path);
		}

		/// <summary>
		/// <paramref name="shader"/> supports AssetBundle shaders implementing RGB or RGB Pattern masks
		/// </summary>
		/// <param name="shader"></param>
		/// <returns></returns>
		public static bool SupportsRGBMaskTex(this Shader shader)
		{
			return shader == CutoutComplexPattern || shader == CutoutComplexRGB;
		}
	}
}
