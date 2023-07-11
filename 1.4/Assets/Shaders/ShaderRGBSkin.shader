Shader "VehicleFramework/ShaderRGBSkin"
{
    Properties
	{
		_MainTex("Main", 2D) = "white" {}
		_MaskTex("Mask", 2D) = "white" {}
		_SkinTex("Skin", 2D) = "white" {}
		_TileNum("TileNum", Float) = 1
		_ScaleX("ScaleX", Float) = 1
		_ScaleY("ScaleY", Float) = 1
		_DisplacementX("DisplacementX", Float) = 0
		_DisplacementY("DisplacementY", Float) = 0
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
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata 
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f 
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v) 
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _SkinTex;
			float4 _SkinTex_ST;

			float _TileNum;
			float _ScaleX;
			float _ScaleY;
			float _DisplacementX;
			float _DisplacementY;

			float4 _MainTexColor;
			float4 _MaskTexColor;
			float4 _SkinTexColor;

			float4 finalColor;

			fixed4 frag(v2f i) : SV_Target 
			{
				_MainTexColor = tex2D(_MainTex, i.uv);
				_MaskTexColor = tex2D(_MaskTex, i.uv);
				finalColor = _MainTexColor;

				float2 newUV = TRANSFORM_TEX(float2((i.uv.x + _DisplacementX) * _TileNum * _ScaleX, (i.uv.y + _DisplacementY) * _TileNum * _ScaleY), _SkinTex);
				_SkinTexColor = tex2D(_SkinTex, newUV) * _MaskTexColor.rrrr;

				float u = _MaskTexColor.r;
				float v = _MaskTexColor.g;
				float w = _MaskTexColor.b;
				float x = 1 - u - v - w;

				finalColor *= _SkinTexColor * u + _SkinTexColor * v + _SkinTexColor * w + float4(1,1,1,1) * x;

				clip(finalColor.a - 0.5f);
				return finalColor;
			}
			ENDCG
		}
	}
	Fallback "VehicleFramework/ShaderRGB"
}
