Shader "Overtone/WaveformBall"
{
    Properties
    {
        _MainTex ("Base Ball", 2D) = "white" {}
        _WaveColor ("Wave Color", Color) = (1,1,1,0.8)
        _Phase ("Phase", Float) = 0
        _Freq ("Frequency", Float) = 3
        _WaveType ("Wave Type", Float) = 0
        _Amp ("Amplitude", Float) = 0.22
        _LineWidth ("Line Width (normalized)", Float) = 0.02
        _HaloWidth ("Halo Width (normalized)", Float) = 0.06
        _BallRadiusUV ("Ball Radius in UV", Float) = 0.45
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _WaveColor;
            float _Phase;
            float _Freq;
            float _WaveType;
            float _Amp;
            float _LineWidth;
            float _HaloWidth;
            float _BallRadiusUV;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float getWave(float waveType, float freq, float t)
            {
                float pi2 = 6.28318;
                if (waveType < 0.5)
                    return sin(freq * pi2 * t);
                if (waveType < 1.5)
                    return 2.0 * frac(freq * t) - 1.0;
                if (waveType < 2.5)
                    return 2.0 * abs(2.0 * frac(freq * t + 0.25) - 1.0) - 1.0;
                return step(0, sin(freq * pi2 * t)) * 2.0 - 1.0;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 base = tex2D(_MainTex, i.uv) * i.color;

                // Map UV to ball-centered coords, normalized so ball edge = 1.0
                float2 centered = (i.uv - 0.5) / _BallRadiusUV;
                float dist = length(centered);

                // Only draw wave inside the ball
                if (dist > 1.0 || base.a < 0.01)
                    return base;

                // Sample the wave at multiple nearby X offsets and use minimum distance.
                // This ensures vertical transitions (sawtooth resets, square edges)
                // are drawn at the same thickness as horizontal segments.
                float baseT = (centered.x * 0.5 + 0.5) + _Phase;
                float halfStep = 0.5 / (_Freq * 16.0);
                float waveDist = 1e9;

                for (int s = -3; s <= 3; s++)
                {
                    float ox = s * halfStep;
                    float tA = baseT + ox * 0.5;
                    float tB = baseT + (ox + halfStep) * 0.5;
                    float yA = _Amp * getWave(_WaveType, _Freq, tA);
                    float yB = _Amp * getWave(_WaveType, _Freq, tB);

                    // Distance from pixel to line segment (ox, yA) -> (ox+halfStep, yB)
                    float2 segDir = float2(halfStep, yB - yA);
                    float2 toP = float2(-ox, centered.y - yA);
                    float segLenSq = dot(segDir, segDir);
                    float proj = clamp(dot(toP, segDir) / segLenSq, 0.0, 1.0);
                    float2 closest = toP - proj * segDir;
                    waveDist = min(waveDist, length(closest));
                }

                // Crisp main line with thin AA
                float lineAlpha = saturate((_LineWidth - waveDist + 0.005) / 0.005);
                lineAlpha *= _WaveColor.a;

                // Soft halo glow
                float haloAlpha = saturate(1.0 - waveDist / _HaloWidth) * 0.16;

                float totalAlpha = max(lineAlpha, haloAlpha);

                // Blend wave over base
                float3 result = lerp(base.rgb, _WaveColor.rgb, totalAlpha);
                return float4(result, base.a);
            }
            ENDCG
        }
    }
}
