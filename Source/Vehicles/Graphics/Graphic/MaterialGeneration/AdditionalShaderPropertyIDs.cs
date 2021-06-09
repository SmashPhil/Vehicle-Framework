using UnityEngine;

namespace Vehicles
{
	public static class AdditionalShaderPropertyIDs
	{
		private static readonly string PatternTexName = "_PatternTex";
		private static readonly string ColorOneName = "_ColorOne";
		private static readonly string ColorThreeName = "_ColorThree";
		private static readonly string ReplaceTextureName = "_Replace";
		private static readonly string TileNumName = "_TileNum";
		private static readonly string DisplacementXName = "_DisplacementX";
		private static readonly string DisplacementYName = "_DisplacementY";
		private static readonly string ScaleXName = "_ScaleX";
		private static readonly string ScaleYName = "_ScaleY";

		public static int PatternTex = Shader.PropertyToID(PatternTexName);
		public static int ColorOne = Shader.PropertyToID(ColorOneName);
		public static int ColorThree = Shader.PropertyToID(ColorThreeName);
		public static int ReplaceTexture = Shader.PropertyToID(ReplaceTextureName);
		public static int TileNum = Shader.PropertyToID(TileNumName);
		public static int DisplacementX = Shader.PropertyToID(DisplacementXName);
		public static int DisplacementY = Shader.PropertyToID(DisplacementYName);
		public static int ScaleX = Shader.PropertyToID(ScaleXName);
		public static int ScaleY = Shader.PropertyToID(ScaleYName);
	}
}
