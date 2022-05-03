// Created By: SmashPhil
// Date: 3 FEB 2021
/*
   CutoutComplex mask support from RimWorld with 3 supported masking colors
   Red | Green | Blue
*/
Shader "Custom/ShaderRGB"
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
			"PreviewType" = "Plane"
		}
		ZWrite Off
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

			float4 _MainTexColor;
			float4 _MaskTexColor;

			float3 maskChannels;

			float4 redMask;
			float4 greenMask;
			float4 blueMask;

			float4 finalColor;

			float4 _ColorOne : _ColorOne;
			float4 _ColorTwo : _ColorTwo;
			float4 _ColorThree : _ColorThree;

			fixed4 frag(v2f i) : SV_Target
			{
				_MainTexColor = tex2D(_MainTex, i.uv);
				_MaskTexColor = tex2D(_MaskTex, i.uv);

				// (-R+1)(-G+1)(-B+1)
				maskChannels.xyz = (-_MaskTexColor.xyz) + float3(1.0, 1.0, 1.0);

				// Mask.R * _ColorOne + (-R+1)
				redMask = (_MaskTexColor.xxxx * _ColorOne + maskChannels.xxxx);
				// Mask.G * _ColorTwo + (-G+1)
				greenMask = (_MaskTexColor.yyyy * _ColorTwo + maskChannels.yyyy);
				// Mask.B * _ColorThree + (-B+1)
				blueMask = (_MaskTexColor.zzzz * _ColorThree + maskChannels.zzzz);

				finalColor = _MainTexColor * redMask;
				finalColor = finalColor * greenMask;
				finalColor = finalColor * blueMask;

				if (finalColor.a <= 0.5)
				{
					finalColor.a = 0;
				}

				return finalColor;
			}
			ENDCG
		}
	}
	Fallback "Custom/CutoutComplex"
}