#ifndef ALLFORONE_DEATH_DISINTEGRATION
#define ALLFORONE_DEATH_DISINTEGRATION

static const float2 BurnSlope = float2(.38, .40);

float AshNoise(float2 cell)
{
    float3 p = frac(float3(cell.xyx) * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float PaperNoise(float2 uv)
{
    float2 cell = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(AshNoise(cell), AshNoise(cell + float2(1, 0)), f.x),
        lerp(AshNoise(cell + float2(0, 1)), AshNoise(cell + 1), f.x), f.y);
}

float BurnThreshold(float2 uv)
{
    // Two scales of noise make the hot front split into tongues before it breaks away.
    return .02 + dot(1 - saturate(uv), BurnSlope) +
        .13 * PaperNoise(uv * float2(10, 7)) + .045 * PaperNoise(uv * 62);
}

float4 ApplyDeathDisintegration(float4 pixel, float2 dissolveUV, float progress)
{
    if (progress <= 0) return pixel;
    float edge = BurnThreshold(dissolveUV) - progress;
    float fiber = PaperNoise(dissolveUV * float2(91, 46) + float2(0, -progress * 23));
    float charred = 1 - smoothstep(.036, .13, edge);
    float heat = (1 - smoothstep(.012, .072, edge)) * lerp(.47, 1, fiber);
    float hotCore = (1 - smoothstep(.004, .022, edge)) * lerp(.77, 1, fiber);
    float4 burn = pixel;
    burn.rgb = lerp(burn.rgb, float3(.095, .018, .007), charred * .88);
    burn.rgb = lerp(burn.rgb, lerp(float3(.81, .064, .006), float3(1, .42, .026), fiber), heat);
    burn.rgb = lerp(burn.rgb, float3(1, .92, .55), hotCore);
    burn.a *= smoothstep(0, .008, edge);
    pixel = lerp(pixel, burn, smoothstep(0, .07, progress));
    return pixel;
}

// Find a small shard from the original silhouette by tracing a rising ash path backwards.
float FlyingAsh(float2 uv, float progress, out float2 sourceUV)
{
    const float grid = 34;
    const float2 averageDrift = float2(.05, 1.03);
    const float releaseBias = .9;
    const float emberLifetime = .12;
    float ageEstimate = (progress - releaseBias + dot(uv, BurnSlope)) / (1 + dot(averageDrift, BurnSlope));
    float2 cell = floor((uv - averageDrift * ageEstimate) * grid);
    float opacity = 0;
    sourceUV = uv;

    [loop]
    for (int row = -2; row <= 2; row++)
    {
        [loop]
        for (int column = -2; column <= 2; column++)
        {
            float2 tile = cell + float2(column, row);
            float seed = AshNoise(tile);
            float2 center = (tile + float2(.25 + seed * .5, .5)) / grid;
            float release = releaseBias - dot(center, BurnSlope) + (seed - .5) * .037;
            float age = progress - release;
            if (seed < .26 || age <= 0 || age >= emberLifetime || any(center < 0) || any(center > 1)) continue;

            // The tile is only the emission point. A stable seed gives each ember its own flight.
            float wind = AshNoise(tile + float2(37, 17));
            float rise = AshNoise(tile + float2(11, 53));
            float phase = seed * 6.283185;
            float side = lerp(-.30, .40, wind) * age;
            side += .008 * (sin(phase + age * lerp(13, 28, rise)) - sin(phase));
            float lift = lerp(.70, 1.32, rise) * age;
            float2 delta = (uv - center - float2(side, lift)) * grid;
            if (dot(delta, delta) > .85) continue;
            float turn = seed * 6.28 + age * 10;
            float2 local = float2(dot(delta, float2(cos(turn), sin(turn))),
                dot(delta, float2(-sin(turn), cos(turn))));
            float radius = lerp(.085, .24, seed * seed);
            float grain = length(local * float2(1.35, .69));
            float core = 1 - smoothstep(radius * .58, radius, grain);
            float halo = (1 - smoothstep(radius, radius * 2.3, grain)) * .25;
            float flicker = .74 + .26 * sin(age * 67 + seed * 71);
            float alpha = (core + halo * (1 - core)) * smoothstep(0, .022, age) *
                (1 - smoothstep(.05, emberLifetime, age)) * flicker * (1 - smoothstep(.90, 1, progress));
            if (alpha > opacity)
            {
                opacity = alpha;
                sourceUV = center;
            }
        }
    }
    return opacity;
}

float4 CompositeAsh(float4 body, float4 source, float alpha)
{
    float ashAlpha = source.a * alpha * (1 - body.a);
    float combinedAlpha = body.a + ashAlpha;
    float3 coloredAsh = source.rgb * .10 + lerp(float3(.72, .095, .006), float3(1, .83, .34), alpha);
    return float4((body.rgb * body.a + coloredAsh * ashAlpha) / max(combinedAlpha, .0001), combinedAlpha);
}

#endif
