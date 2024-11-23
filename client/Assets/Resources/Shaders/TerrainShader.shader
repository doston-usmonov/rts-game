Shader "Custom/TerrainShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _NamlikMap ("Wet Texture", 2D) = "white" {}
        _QorMap ("Snow Texture", 2D) = "white" {}
        _MuzMap ("Ice Texture", 2D) = "white" {}
        _LoyMap ("Mud Texture", 2D) = "white" {}
        
        _NamlikKoef ("Wet Blend", Range(0,1)) = 0
        _QorKoef ("Snow Blend", Range(0,1)) = 0
        _MuzKoef ("Ice Blend", Range(0,1)) = 0
        _LoyKoef ("Mud Blend", Range(0,1)) = 0
        
        _QoshimchaNormalXarita ("Additional Normal Map", 2D) = "bump" {}
        _NormalAralashKoef ("Normal Blend", Range(0,1)) = 0
        
        // Yangi xususiyatlar
        _Roughness ("Roughness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0
        _AmbientOcclusion ("Ambient Occlusion", Range(0,1)) = 1
        _HeightScale ("Height Scale", Range(0,1)) = 0.1
        
        // Muhit ta'siri
        _MuhitTasiri ("Environmental Effect", Range(0,1)) = 1
        _HaroratTasiri ("Temperature Effect", Range(-1,1)) = 0
        _ShamolTasiri ("Wind Effect", Range(0,1)) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        #pragma multi_compile_fog
        #pragma multi_compile _ _PARALLAXMAP

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _NamlikMap;
        sampler2D _QorMap;
        sampler2D _MuzMap;
        sampler2D _LoyMap;
        sampler2D _QoshimchaNormalXarita;
        
        float _NamlikKoef;
        float _QorKoef;
        float _MuzKoef;
        float _LoyKoef;
        float _NormalAralashKoef;
        float _Roughness;
        float _Metallic;
        float _AmbientOcclusion;
        float _HeightScale;
        float _MuhitTasiri;
        float _HaroratTasiri;
        float _ShamolTasiri;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
            float3 worldPos;
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Shamol ta'siri
            float windEffect = _ShamolTasiri * sin(_Time.y + v.vertex.x * 0.5);
            v.vertex.xyz += v.normal * windEffect * 0.1;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Parallax mapping
            float height = tex2D(_MainTex, IN.uv_MainTex).a;
            float2 parallaxOffset = ParallaxOffset(height, _HeightScale, IN.viewDir);
            IN.uv_MainTex += parallaxOffset;
            IN.uv_BumpMap += parallaxOffset;
            
            // Asosiy tekstura
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            
            // Qo'shimcha teksturalar
            fixed4 wet = tex2D(_NamlikMap, IN.uv_MainTex);
            fixed4 snow = tex2D(_QorMap, IN.uv_MainTex);
            fixed4 ice = tex2D(_MuzMap, IN.uv_MainTex);
            fixed4 mud = tex2D(_LoyMap, IN.uv_MainTex);
            
            // Harorat ta'siri
            float tempEffect = _HaroratTasiri * 0.5 + 0.5; // -1..1 dan 0..1 ga o'tkazish
            float snowFactor = saturate(_QorKoef + (1 - tempEffect) * 0.5);
            float iceFactor = saturate(_MuzKoef + (1 - tempEffect) * 0.3);
            
            // Teksturalarni aralashtirish
            c = lerp(c, wet, _NamlikKoef * _MuhitTasiri);
            c = lerp(c, snow, snowFactor * _MuhitTasiri);
            c = lerp(c, ice, iceFactor * _MuhitTasiri);
            c = lerp(c, mud, _LoyKoef * _MuhitTasiri);
            
            o.Albedo = c.rgb;
            
            // Normal xaritalarni aralashtirish
            fixed3 normal1 = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            fixed3 normal2 = UnpackNormal(tex2D(_QoshimchaNormalXarita, IN.uv_BumpMap));
            o.Normal = lerp(normal1, normal2, _NormalAralashKoef);
            
            // Material xususiyatlari
            o.Metallic = _Metallic;
            o.Smoothness = lerp(_Roughness, 0.9, iceFactor * _MuhitTasiri); // Muz yuzasi silliqroq
            o.Occlusion = _AmbientOcclusion;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
