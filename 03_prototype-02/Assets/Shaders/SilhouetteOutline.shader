// A flat-colour outline around an object's silhouette, for objects with see-through gaps
// (like the wire-mesh trash can) where a plain enlarged back-face shell would show through
// the gaps and fill the whole object in.
//
// Drawn on a copy of the object's mesh whose normals are smoothed (shared by every vertex at
// the same spot). The first pass marks the object's whole silhouette in the stencil buffer
// without drawing anything; the second pushes the surface out by _Width along the normals
// and draws its back faces in _BaseColor everywhere outside that silhouette.
Shader "Custom/SilhouetteOutline"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 0.15, 0.15, 1)
        _Width("Width (metres)", Float) = 0.012
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+460" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Width;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes v, float width)
        {
            Varyings o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
            positionWS += normalize(TransformObjectToWorldNormal(v.normalOS)) * width;
            o.positionCS = TransformWorldToHClip(positionWS);
            return o;
        }
        ENDHLSL

        // 1: mark the silhouette (no colour, no depth).
        Pass
        {
            Name "SilhouetteMask"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest Always
            ColorMask 0
            Stencil { Ref 128 WriteMask 128 Comp Always Pass Replace }

            HLSLPROGRAM
            #pragma vertex MaskVert
            #pragma fragment MaskFrag
            #pragma multi_compile_instancing
            Varyings MaskVert(Attributes v) { return Vert(v, 0); }
            half4 MaskFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        // 2: the enlarged shell's back faces, only outside the silhouette.
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }
            Cull Front
            Stencil { Ref 128 ReadMask 128 Comp NotEqual }

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing
            Varyings OutlineVert(Attributes v) { return Vert(v, _Width); }
            half4 OutlineFrag(Varyings i) : SV_Target { return _BaseColor; }
            ENDHLSL
        }
    }
}
