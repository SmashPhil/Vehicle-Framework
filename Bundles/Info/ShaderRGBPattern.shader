// Created By: SmashPhil
// Date: 3 FEB 2021
/*
   Multi-mask shader providing support for overlaying masks to create pattern like effects without
   needing to create additional masks with the patterns built in. Simplifies the pattern creation
   process while also adding support for an additional 3rd color (BLUE) for masking.
*/
Shader "Custom/ShaderRGBPattern"
{
	Properties
	{
		_MainTex("Main", 2D) = "white" {}
		_MaskTex("Mask", 2D) = "white" {}
		_PatternTex("Pattern", 2D) = "white" {}
		_Replace("Replace", Float) = 0
		_TileNum("TileNum", Float) = 1
		_ScaleX("ScaleX", Float) = 1
		_ScaleY("ScaleY", Float) = 1
		_DisplacementX("DisplacementX", Float) = 0
		_DisplacementY("DisplacementY", Float) = 0
		_ColorOne("ColorOne", Color) = (1,1,1,1)
		_ColorTwo("ColorTwo", Color) = (1,1,1,1)
		_ColorThree("ColorThree", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent-100" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
		Pass
		{
			Tags { "IGNOREPROJECTOR" = "true" "QUEUE" = "Transparent-100" "RenderType" = "Transparent" }
			CGPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _MaskTex;
			sampler2D _PatternTex;
			float4 _PatternTex_ST;

			float _Replace;
			float _TileNum;
			float _ScaleX;
			float _ScaleY;
			float _DisplacementX;
			float _DisplacementY;

			float4 _MainTexColor;
			float4 _MaskTexColor;
			float4 _PatternTexColor;
			float4 _ReplaceTexColor;

			float redChannel;
			float3 patternChannels;

			float4 redMask;
			float4 greenMask;
			float4 blueMask;

			float4 redPattern;
			float4 greenPattern;
			float4 bluePattern;

			float4 finalColor;

			float4 _ColorOne : _ColorOne;
			float4 _ColorTwo : _ColorTwo;
			float4 _ColorThree : _ColorThree;

			fixed4 frag(v2f i) : SV_Target {
				_MainTexColor = tex2D(_MainTex, i.uv);
				_MaskTexColor = tex2D(_MaskTex, i.uv);
				
				float2 newUV = TRANSFORM_TEX(float2((i.uv.x + _DisplacementX) * _TileNum * _ScaleX, (i.uv.y + _DisplacementY) * _TileNum * _ScaleY), _PatternTex);
				_PatternTexColor = tex2D(_PatternTex, newUV) * _MaskTexColor.rrrr;
				_ReplaceTexColor = tex2D(_PatternTex, newUV);
				// (-R+1)(-G+1)(-B+1)
				patternChannels.xyz = (-_PatternTexColor.xyz) + float3(1.0, 1.0, 1.0);

				if (_Replace == 1){
					fixed4 antiMask = _MainTexColor * (1 - _MaskTexColor.r);
					finalColor = _MainTexColor * _PatternTexColor + antiMask;
				}
				else{
					// Pattern.R * _ColorOne + (-R+1)
					redPattern = (_PatternTexColor.rrrr * _ColorOne + patternChannels.xxxx);
					// Pattern.G * _ColorTwo + (-G+1)
					greenPattern = (_PatternTexColor.gggg * _ColorTwo + patternChannels.yyyy);
					// Pattern.B * _ColorThree + (-B+1)
					bluePattern = (_PatternTexColor.bbbb * _ColorThree + patternChannels.zzzz);

					finalColor = _MainTexColor * redPattern;
					finalColor = finalColor * greenPattern;
					finalColor = finalColor * bluePattern;
				}
				if (finalColor.a <= 0.05)
				{
					finalColor.a = 0;
				}
				
				return finalColor;
			}
			ENDCG
		}
	}
}
