Shader "Custom/ReflectiveDecal" {

    Properties {

        _Color ("Main Color", Color) = (1,1,1,1)

        _ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)

        _MainTex ("Base (RGB) RefStrength (A)", 2D) = "white" {}

        _DecalTex ("Decal (RGB) Transparency (A)", 2D) = "" {}

        _Cube ("Reflection Cubemap", Cube) = "_Skybox" { TexGen CubeReflect }

    }

    SubShader {

        LOD 200

        Tags { "RenderType"="Opaque" }

       

    CGPROGRAM

    #pragma surface surf Lambert

 

    sampler2D _MainTex;

    sampler2D _DecalTex;

    samplerCUBE _Cube;

 

    fixed4 _Color;

    fixed4 _ReflectColor;

 

    struct Input {

        float2 uv_MainTex;

        float2 uv_DecalTex;

        float3 worldRefl;

    };

 

    void surf (Input IN, inout SurfaceOutput o) {

        fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);

        fixed4 c = tex * _Color;

        o.Albedo = c.rgb;

       

        fixed4 reflcol = texCUBE (_Cube, IN.worldRefl);

        reflcol *= tex.a;

        o.Emission = reflcol.rgb * _ReflectColor.rgb;

        o.Alpha = reflcol.a * _ReflectColor.a;

    }

    ENDCG
	        Pass{

            Name "DECAL"

            Blend SrcAlpha OneMinusSrcAlpha

            AlphaTest Greater 0.1

            SetTexture [_DecalTex] {

           

            }

        }
    }

       

    FallBack "Mobile/Reflective/VertexLit"
}
