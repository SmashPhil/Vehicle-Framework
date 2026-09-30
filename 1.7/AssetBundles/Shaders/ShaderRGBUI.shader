Shader "VehicleFramework/ShaderRGBUI"
{
  Properties
  {
	_MainTex("Base (RGB)", 2D) = "white" {}
	_MaskTex("Albedo (RGB)", 2D) = "white" {}
	_ColorOne("ColorOne", Color) = (1,1,1,1)
	_ColorTwo("ColorTwo", Color) = (1,1,1,1)
	_ColorThree("ColorThree", Color) = (1,1,1,1)
  }
  SubShader
  {
	Tags 
	{ 
	  "IgnoreProjector" = "true" 
	  "Queue" = "Transparent-100"
	  "RenderType" = "Transparent"
	}
	Pass
	{
	  Blend SrcAlpha OneMinusSrcAlpha
	  ZClip On 
	  ZTest Always 
	  ZWrite Off
      Cull Off
	  CGPROGRAM

	  #pragma vertex vert
	  #pragma fragment frag
	  #include "UnityCG.cginc"

	  sampler2D _GUIClipTexture;
      float4x4 unity_GUIClipTextureMatrix;

	  sampler2D _MainTex;
	  sampler2D _MaskTex;

	  uniform float4 _MainTexColor;
	  uniform float4 _MaskTexColor;

	  uniform float4 _ColorOne : _ColorOne;
	  uniform float4 _ColorTwo : _ColorTwo;
	  uniform float4 _ColorThree : _ColorThree;

	  float4 finalColor;

	  struct appdata
	  {
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	  };

	  struct v2f
	  {
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
		float2 clipUV : TEXCOORD1;
	  };

	  v2f vert(appdata v)
	  {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		float3 eyePos = UnityObjectToViewPos(v.vertex);
		o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
		return o;
	  }

	  fixed4 frag(v2f i) : SV_Target
	  {
		_MainTexColor = tex2D(_MainTex, i.uv);
		_MaskTexColor = tex2D(_MaskTex, i.uv);
		finalColor = _MainTexColor;

		float u = _MaskTexColor.r;
		float v = _MaskTexColor.g;
		float w = _MaskTexColor.b;
		float x = 1 - u - v - w;

		finalColor *= _ColorOne * u + _ColorTwo * v + _ColorThree * w + float4(1,1,1,1) * x;
		finalColor.a *= tex2D(_GUIClipTexture, i.clipUV).a;
		clip(finalColor.a - 0.1f);
		return finalColor;
	  }
	  ENDCG
	}
  }
  Fallback "VehicleFramework/ShaderRGB"
}