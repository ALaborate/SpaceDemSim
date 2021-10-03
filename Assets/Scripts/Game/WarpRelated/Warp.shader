Shader "Warp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RenderingResult ("Texture", 2D) = "white" {}
        _Directions ("Directions", Float) = 5
        _Samples ("Samples over direction", Float) = 3
        _DistancePerSecond ("Disatance per second", Float) = 0.5

        _FadeToScreenDuration ("Time of fade between bufer and screen", Float) = 1.0
        _TimeOfEnd ("Time of end of using bufer", Float) = 0
        _CurrentTime ("Current script time", Float) = 0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            sampler2D _MainTex;
            sampler2D _RenderingResult;
            float _Directions;
            float _Samples;
            float _DistancePerSecond;
            float _FadeToScreenDuration;
            float _TimeOfEnd;
            float _CurrentTime;

            static const float PI = 3.141592653589793;
            static const float PIx2 = 6.283185307179586;
            float4 frag (v2f i) : SV_Target
            {
                float4 col = 0;
                if(_TimeOfEnd < 0){
                    //do stuff
                    float maxDist = _DistancePerSecond * unity_DeltaTime.x;
                    float distInc = maxDist / _Samples;
                    float angleInc = PIx2/_Directions;
                    col = tex2D(_RenderingResult, i.uv);
                    float colBrightness = length(col);
                    [loop] for(float phi=0; phi<=PIx2; phi+=angleInc){
                        [loop] for(float r=distInc; r<=maxDist; r+=distInc){
                            float2 displacement = float2(r*sin(phi), r*cos(phi));
                            float4 nCol = tex2D(_RenderingResult, i.uv + displacement);
                            float nColBrightness = length(nCol);
                            if(nColBrightness > colBrightness){
                                col = nCol;
                                colBrightness = nColBrightness;
                            }
                        }
                    }
                } else {
                    col = lerp(tex2D(_RenderingResult, i.uv), tex2D(_MainTex, i.uv), saturate((_CurrentTime - _TimeOfEnd)/_FadeToScreenDuration));
                }
                
                
                return col;
            }
            ENDCG
        }
    }
}
