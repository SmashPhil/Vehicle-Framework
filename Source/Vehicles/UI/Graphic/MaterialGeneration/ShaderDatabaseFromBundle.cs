using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class ShaderDatabaseFromBundle
	{
		private static readonly Dictionary<string, Shader> lookup = new Dictionary<string, Shader>();

		private static readonly string AssetBundlePath = $@"{VehicleMod.settings.Mod.Content.RootDir}/Shaders/RGBShaderBundle";

		public static readonly AssetBundle bundle = AssetBundle.LoadFromFile(AssetBundlePath);

		public static readonly Shader CutoutComplexRGB = LoadAssetBundleShader("Assets/Shaders/ShaderRGB.shader");

		public static readonly Shader CutoutComplexPattern = LoadAssetBundleShader("Assets/Shaders/ShaderRGBPattern.shader");

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
