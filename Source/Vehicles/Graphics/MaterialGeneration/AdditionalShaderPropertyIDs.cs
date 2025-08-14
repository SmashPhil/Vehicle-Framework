using UnityEngine;

namespace Vehicles;

public static class AdditionalShaderPropertyIDs
{
	private const string PatternTexName = "_PatternTex";

	private const string MainTexName = "_MainTex";
	private const string ColorOneName = "_ColorOne";
	private const string ColorThreeName = "_ColorThree";
	private const string SkinTexName = "_SkinTex";
	private const string TileNumName = "_TileNum";
	private const string DisplacementXName = "_DisplacementX";
	private const string DisplacementYName = "_DisplacementY";
	private const string ScaleXName = "_ScaleX";
	private const string ScaleYName = "_ScaleY";

	public static readonly int MainTex = Shader.PropertyToID(MainTexName);
	public static readonly int PatternTex = Shader.PropertyToID(PatternTexName);
	public static readonly int ColorOne = Shader.PropertyToID(ColorOneName);
	public static readonly int ColorThree = Shader.PropertyToID(ColorThreeName);
	public static readonly int SkinTex = Shader.PropertyToID(SkinTexName);
	public static readonly int TileNum = Shader.PropertyToID(TileNumName);
	public static readonly int DisplacementX = Shader.PropertyToID(DisplacementXName);
	public static readonly int DisplacementY = Shader.PropertyToID(DisplacementYName);
	public static readonly int ScaleX = Shader.PropertyToID(ScaleXName);
	public static readonly int ScaleY = Shader.PropertyToID(ScaleYName);
}