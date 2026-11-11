#version 450

#include "MaskGate.glsl"

layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _ColorOne;      // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _ColorTwo;   // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _ColorThree;   // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex;

void main()
{
    vec2 uv = vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy;
    vec4 mainTexColor = texture(_MainTex, uv);
    vec4 maskTexColor = texture(_MaskTex, uv);
    maskTexColor.rgb *= MaskGate(maskTexColor);

    vec4 finalColor = mainTexColor;
    float u = maskTexColor.r;
    float v = maskTexColor.g;
    float w = maskTexColor.b;
    float x = 1.0 - u - v - w;

    finalColor *= uMaterial._ColorOne * u + uMaterial._ColorTwo * v + uMaterial._ColorThree * w + vec4(1.0) * x;
    
    if (finalColor.a < 0.1)
        discard;
    
    outColor = finalColor;
}
