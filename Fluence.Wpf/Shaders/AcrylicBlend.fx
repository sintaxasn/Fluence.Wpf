sampler2D BackgroundSampler : register(s0);
sampler2D ForegroundSampler : register(s1);
float Mode : register(c0); // 0 = luminosity, 1 = color

float Lum(float3 c) { return dot(c, float3(0.30, 0.59, 0.11)); }

float3 ClipColor(float3 c)
{
    float l = Lum(c);
    float lo = min(c.r, min(c.g, c.b));
    float hi = max(c.r, max(c.g, c.b));
    if (lo < 0.0) c = l + (c - l) * (l / max(l - lo, 0.000001));
    if (hi > 1.0) c = l + (c - l) * ((1.0 - l) / max(hi - l, 0.000001));
    return saturate(c);
}

float3 SetLum(float3 c, float l)
{
    return ClipColor(c + (l - Lum(c)));
}

float4 Main(float2 uv : TEXCOORD) : COLOR
{
    float4 b = tex2D(BackgroundSampler, uv);
    float4 f = tex2D(ForegroundSampler, uv);
    float3 cb = b.a > 0.0 ? b.rgb / b.a : float3(0.0, 0.0, 0.0);
    float3 cf = f.a > 0.0 ? f.rgb / f.a : float3(0.0, 0.0, 0.0);
    float3 blended = Mode < 0.5
        ? SetLum(cb, Lum(cf))
        : SetLum(cf, Lum(cb));
    float3 premul = blended * f.a * b.a
        + cf * f.a * (1.0 - b.a)
        + cb * b.a * (1.0 - f.a);
    float alpha = f.a + b.a - f.a * b.a;
    return float4(premul, alpha);
}
