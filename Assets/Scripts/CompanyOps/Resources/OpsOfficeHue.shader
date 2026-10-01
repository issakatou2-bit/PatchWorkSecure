Shader "PatchWorkSecure/OfficeHue"
{
    Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Hue("Hue",Float)=160 _Saturation("Saturation",Float)=.8 _Brightness("Brightness",Float)=1.05 _StencilComp("Stencil Comparison",Float)=8 _Stencil("Stencil ID",Float)=0 _StencilOp("Stencil Operation",Float)=0 _StencilWriteMask("Stencil Write Mask",Float)=255 _StencilReadMask("Stencil Read Mask",Float)=255 _ColorMask("Color Mask",Float)=15 }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
            sampler2D _MainTex;float4 _TextureSampleAdd,_ClipRect;float _Hue,_Saturation,_Brightness;
            v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float4 frag(v2f i):SV_Target
            {
                float4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb=LinearToGammaSpace(c.rgb);
                #endif
                float a=radians(_Hue),co=cos(a),si=sin(a);
                float3x3 hue=float3x3(.213+.787*co-.213*si,.715-.715*co-.715*si,.072-.072*co+.928*si,.213-.213*co+.143*si,.715+.285*co+.140*si,.072-.072*co-.283*si,.213-.213*co-.787*si,.715-.715*co+.715*si,.072+.928*co+.072*si);
                c.rgb=mul(hue,c.rgb);float gray=dot(c.rgb,float3(.2126,.7152,.0722));c.rgb=saturate(lerp(gray.xxx,c.rgb,_Saturation)*_Brightness);
                #ifndef UNITY_COLORSPACE_GAMMA
                c.rgb=GammaToLinearSpace(c.rgb);
                #endif
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
