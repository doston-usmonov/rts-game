Shader "Custom/TerrainShader"
{
    Properties
    {
        _MainTex ("Asosiy Tekstura", 2D) = "white" {}
        _BumpMap ("Normal Xarita", 2D) = "bump" {}
        _NamlikMap ("Namlik Teksturasi", 2D) = "white" {}
        _QorMap ("Qor Teksturasi", 2D) = "white" {}
        _MuzMap ("Muz Teksturasi", 2D) = "white" {}
        _LoyMap ("Loy Teksturasi", 2D) = "white" {}
        
        _NamlikKoef ("Namlik Koeffitsienti", Range(0,1)) = 0
        _QorKoef ("Qor Koeffitsienti", Range(0,1)) = 0
        _MuzKoef ("Muz Koeffitsienti", Range(0,1)) = 0
        _LoyKoef ("Loy Koeffitsienti", Range(0,1)) = 0
        
        _Glossiness ("Yaltiroqlik", Range(0,1)) = 0.5
        _Metallic ("Metallik", Range(0,1)) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _NamlikMap;
        sampler2D _QorMap;
        sampler2D _MuzMap;
        sampler2D _LoyMap;
        
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };
        
        half _Glossiness;
        half _Metallic;
        float _NamlikKoef;
        float _QorKoef;
        float _MuzKoef;
        float _LoyKoef;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Asosiy tekstura
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            
            // Qo'shimcha teksturalar
            fixed4 namlik = tex2D(_NamlikMap, IN.uv_MainTex);
            fixed4 qor = tex2D(_QorMap, IN.uv_MainTex);
            fixed4 muz = tex2D(_MuzMap, IN.uv_MainTex);
            fixed4 loy = tex2D(_LoyMap, IN.uv_MainTex);
            
            // Teksturalarni aralashtirish
            c = lerp(c, namlik, _NamlikKoef);
            c = lerp(c, qor, _QorKoef);
            c = lerp(c, muz, _MuzKoef);
            c = lerp(c, loy, _LoyKoef);
            
            // Normal xaritani qo'llash
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            
            // Material xususiyatlarini o'rnatish
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            
            // Qor va muz uchun yaltiroqlikni o'zgartirish
            o.Smoothness = lerp(o.Smoothness, 0.8, _QorKoef);
            o.Smoothness = lerp(o.Smoothness, 0.9, _MuzKoef);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
