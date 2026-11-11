#version 450

#include "MaskGate.glsl"

layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _SkinTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
    float _TileNum; // @default = 1.0
    float _ScaleX; // @default = 1.0
    float _ScaleY; // @default = 1.0
    float _DisplacementX; // @default = 0.0
    float _DisplacementY; // @default = 0.0
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex;
layout(set = 2, binding = 3) uniform sampler2D _SkinTex;

void main()
{
    vec2 uv = vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy;
    vec4 mainTexColor = texture(_MainTex, uv);
    vec4 maskTexColor = texture(_MaskTex, uv);
    maskTexColor.rgb *= MaskGate(maskTexColor);
    
    vec4 finalColor = mainTexColor;

    vec2 scaledUV = vec2(
        (uv.x + uMaterial._DisplacementX) * uMaterial._TileNum * uMaterial._ScaleX,
        (uv.y + uMaterial._DisplacementY) * uMaterial._TileNum * uMaterial._ScaleY
    );

    vec2 newUV = scaledUV * uMaterial._SkinTex_ST.zw + uMaterial._SkinTex_ST.xy;

    vec4 skinTexColor = texture(_SkinTex, newUV) * vec4(maskTexColor.r);

    float u = maskTexColor.r;
    float v = maskTexColor.g;
    float w = maskTexColor.b;
    float x = 1.0 - u - v - w;

    finalColor *= skinTexColor * u + skinTexColor * v + skinTexColor * w + vec4(1.0) * x;

    if (finalColor.a < 0.1)
        discard;

    outColor = finalColor;
}