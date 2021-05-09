#if UNITY_EDITOR
using UnityEditor;

namespace Vehicles
{
	public class ShaderGeneration
	{
		[MenuItem("Assets/Build AssetBundles")]
		static void BuildAllAssetBundles()
		{
			BuildPipeline.BuildAssetBundles("Assets/ShadersRGB", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
		}
	}
}
#endif