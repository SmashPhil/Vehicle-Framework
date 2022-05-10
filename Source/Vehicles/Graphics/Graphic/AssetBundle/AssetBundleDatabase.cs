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
	/// <remarks>
	/// Q: "Why don't you just use RimWorld's content loader to load the asset bundle? It's supported right?"
	/// A: Yes, but it does not support versioning.. meaning if a later version of Unity is used in a future update that requires a rebuild of all asset bundles,
	/// I may not be able to support that previous version. AssetBundles on older versions of Unity might not load properly and vice verse. When Ludeon decides to
	/// support versioning for AssetBundles, I can move to using that instead.
	/// </remarks>
	[LoadedEarly]
	[IsMainThread]
	public static class AssetBundleDatabase
	{
		/// <summary>
		/// AssetBundle version loader
		/// </summary>
		private static readonly Dictionary<string, string> bundleBuildVersionManifest = new Dictionary<string, string>()
		{
			{"1.3", "2019.4.30f1"}
		};

		private static readonly Dictionary<string, UnityEngine.Object> assetLookup = new Dictionary<string, UnityEngine.Object>();

		private static readonly List<string> loadFoldersChecked = new List<string>();

		private static readonly string VehicleAssetBundlePath = @"Assets\vehicleassets";

		private static readonly string CutoutComplexRGBPath = Path.Combine("Assets", "Shaders", "ShaderRGB.shader");
		private static readonly string CutoutComplexPatternPath = Path.Combine("Assets", "Shaders", "ShaderRGBPattern.shader");
		private static readonly string CutoutComplexSkinPath = Path.Combine("Assets", "Shaders", "ShaderRGBSkin.shader");

		private static readonly string MouseHandOpenPath = "Assets/Textures/MouseHandOpen.png";
		private static readonly string MouseHandClosedPath = "Assets/Textures/MouseHandClosed.png";

		public static readonly AssetBundle VehicleAssetBundle;

		public static readonly Shader CutoutComplexRGB;
		public static readonly Shader CutoutComplexPattern;
		public static readonly Shader CutoutComplexSkin;

		public static readonly Texture2D MouseHandOpen;
		public static readonly Texture2D MouseHandClosed;

		static AssetBundleDatabase()
		{
			if (!UnityData.IsInMainThread)
			{
				SmashLog.Error($"Static Constructor was not called on main thread for type <type>AssetBundleDatabase</type> which has attribute <attribute>IsMainThread</attribute>. Use <attribute>StaticConstructorOnStartup</attribute instead.");
			}
			string version = $"{VersionControl.CurrentMajor}.{VersionControl.CurrentMinor}";
			if (bundleBuildVersionManifest.TryGetValue(version, out string currentVersion))
			{
				if (currentVersion != Application.unityVersion)
				{
					Log.Warning($"{VehicleHarmony.LogLabel} Unity Version {Application.unityVersion} does not match registered version for AssetBundles being loaded. Please report it on the workshop page so that I may update the UnityVersion supported for this AssetBundle.");
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

						CutoutComplexRGB = LoadAsset<Shader>(CutoutComplexRGBPath);
						CutoutComplexPattern = LoadAsset<Shader>(CutoutComplexPatternPath);
						CutoutComplexSkin = LoadAsset<Shader>(CutoutComplexSkinPath);
						MouseHandOpen = LoadAsset<Texture2D>(MouseHandOpenPath);
						MouseHandClosed = LoadAsset<Texture2D>(MouseHandClosedPath);
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
		public static T LoadAsset<T>(string path) where T : UnityEngine.Object
		{
			if (assetLookup.TryGetValue(path, out var asset))
			{
				return (T)asset;
			}
			var unityObject = VehicleAssetBundle.LoadAsset(path);
			if (unityObject is null)
			{
				SmashLog.Error($"Unable to load asset of type <type>{typeof(T)}</type> from path=<text>\"{path}\"</text>");
				return null;
			}
			if (!(unityObject is T))
			{
				SmashLog.Error($"Asset has loaded successfully from path=<text>\"{path}\"</text> but is not of type <type>{typeof(T)}</type>. Actual type is <type>{unityObject.GetType()}</type>.");
				return null;
			}
			assetLookup.Add(path, unityObject);
			return (T)unityObject;
		}

		/// <summary>
		/// <paramref name="shader"/> supports AssetBundle shaders implementing RGB or RGB Pattern masks
		/// </summary>
		/// <param name="shader"></param>
		public static bool SupportsRGBMaskTex(this Shader shader)
		{
			return shader == CutoutComplexPattern || shader == CutoutComplexSkin || shader == CutoutComplexRGB;
		}
	}
}
