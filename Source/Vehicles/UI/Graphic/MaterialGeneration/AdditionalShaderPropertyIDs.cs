using UnityEngine;

namespace Vehicles
{
	public static class AdditionalShaderPropertyIDs
	{
		private static readonly string PatternTexName = "_PatternTex";

		private static readonly string ColorOneName = "_ColorOne";

		private static readonly string ColorThreeName = "_ColorThree";

		private static readonly string ReplaceTextureName = "_Replace";

		public static int PatternTex = Shader.PropertyToID(PatternTexName);

		public static int ColorOne = Shader.PropertyToID(ColorOneName);

		public static int ColorThree = Shader.PropertyToID(ColorThreeName);

		public static int ReplaceTexture = Shader.PropertyToID(ReplaceTextureName);
	}
}
