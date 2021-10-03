Shader "UI/UIGrabPassInvert"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // Draw after all opaque geometry
        Tags { "Queue" = "Transparent" }

        // Grab the screen behind the object into _BackgroundTexture
        GrabPass
        {
            "_BackgroundTexture"
        }

        // Render the object with the texture generated above, and invert the colors
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float2 mTexUV: TEXCOORD1;
                float4 pos : SV_POSITION;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v) {
                v2f o;
                // use UnityObjectToClipPos from UnityCG.cginc to calculate 
                // the clip-space of the vertex
                o.pos = UnityObjectToClipPos(v.vertex);

                // use ComputeGrabScreenPos function from UnityCG.cginc
                // to get the correct texture coordinate
                o.grabPos = ComputeGrabScreenPos(o.pos);

                o.mTexUV = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            sampler2D _BackgroundTexture;

            float4 frag(v2f i) : SV_Target
            {
                float4 bgcolor = tex2Dproj(_BackgroundTexture, i.grabPos);
                float4 col = tex2D(_MainTex, i.mTexUV);
                return saturate((1-bgcolor)*col.a + bgcolor * (1-col.a));
            }
            ENDCG
        }

    }
}