// CRT display simulation shader for ZX Spectrum
// Effects: barrel distortion, scanlines, phosphor RGB mask, vignette

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

matrix MatrixTransform;

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 OutputSize;      // e.g. (704, 608)

// Tunable strengths: 0 = off, 1 = full
float ScanlineStrength;     // darkness of every other output row  (default 0.40)
float PhosphorStrength;     // per-channel dimming between triads   (default 0.70)
float BarrelDistortion;     // barrel/pincushion warp amount        (default 0.12)
float VignetteStrength;     // corner darkening                     (default 0.30)

struct VertexInput
{
    float4 Position : POSITION0;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct PixelInput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

PixelInput MainVS(VertexInput input)
{
    PixelInput output;
    output.Position = mul(input.Position, MatrixTransform);
    output.Color    = input.Color;
    output.TexCoord = input.TexCoord;
    return output;
}

// Barrel/pincushion distortion: bends the UV coordinates outward.
float2 BarrelWarp(float2 uv)
{
    float2 cc   = uv - 0.5;
    float  dist = dot(cc, cc);
    return uv + cc * (dist * BarrelDistortion);
}

float4 MainPS(PixelInput input) : COLOR
{
    float2 uv = BarrelWarp(input.TexCoord);

    // Kill pixels that fell outside the screen after warping.
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
        return float4(0.0, 0.0, 0.0, 1.0);

    float4 color = tex2D(SpriteTextureSampler, uv) * input.Color;

    // --- Scanlines ---
    // Darken every other *output* row.  Using uv * OutputSize.y converts
    // back to output-pixel space so the lines are exactly 1 px thick at 2x.
    float row = fmod(floor(uv.y * OutputSize.y), 2.0);
    color.rgb *= lerp(1.0, 1.0 - ScanlineStrength, row);

    // --- Phosphor RGB mask ---
    // Cycle R/G/B triads across output columns.
    float col3 = fmod(floor(uv.x * OutputSize.x), 3.0);
    float dim = 1.0 - PhosphorStrength * 0.35;
    float3 mask;
    mask.r = (col3 < 1.0) ? 1.0 : dim;
    mask.g = (col3 >= 1.0 && col3 < 2.0) ? 1.0 : dim;
    mask.b = (col3 >= 2.0) ? 1.0 : dim;
    color.rgb *= mask;

    // --- Vignette ---
    // Use the *unwarped* UV so the vignette tracks the screen corners.
    float2 v = input.TexCoord * 2.0 - 1.0;
    color.rgb *= 1.0 - saturate(dot(v, v) * VignetteStrength);

    return color;
}

technique SpriteDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader  = compile PS_SHADERMODEL MainPS();
    }
};
