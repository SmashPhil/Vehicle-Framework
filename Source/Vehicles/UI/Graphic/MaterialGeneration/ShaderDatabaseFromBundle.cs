using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class ShaderDatabaseFromBundle
	{
		private static readonly Dictionary<string, Shader> lookup = new Dictionary<string, Shader>();

		private static readonly string AssetBundlePath = $@"{VehicleMod.settings.Mod.Content.RootDir}\Shaders\RGBShaderBundle";

		public static readonly AssetBundle bundle;

		public static readonly Shader CutoutComplexRGB;

		public static readonly Shader CutoutComplexPattern;

		static ShaderDatabaseFromBundle()
		{
			try
			{
				bundle = AssetBundle.LoadFromFile(AssetBundlePath);
				if (bundle is null) throw new NullReferenceException();

				CutoutComplexRGB = LoadAssetBundleShader("Assets/Shaders/ShaderRGB.shader");
				CutoutComplexPattern = LoadAssetBundleShader("Assets/Shaders/ShaderRGBPattern.shader");
			}
			catch (Exception)
			{
				SmashLog.Error($"Unable to load AssetBundle at <text>{AssetBundlePath}</text>");
			}
		}

		public static Shader LoadAssetBundleShader(string path)
		{
			if (lookup.TryGetValue(path, out Shader shader))
			{
				return shader;
			}
			return (Shader)bundle.LoadAsset(path);
		}

		public static bool SupportsRGBMaskTex(this Shader shader)
		{
			return shader == CutoutComplexPattern || shader == CutoutComplexRGB;
		}
	}
}
