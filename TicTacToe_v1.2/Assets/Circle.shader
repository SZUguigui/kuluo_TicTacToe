Shader "UI/CircleRing"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _InnerRadius ("Inner Radius", Range(0, 1)) = 0.5
        _OuterRadius ("Outer Radius", Range(0, 1)) = 0.7
        _Smoothness ("Smoothness", Range(0, 0.1)) = 0.01
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            fixed4 _Color;
            float _InnerRadius;
            float _OuterRadius;
            float _Smoothness;
            
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // ���㵽���ĵľ���
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.texcoord, center);
                
                // ʹ��smoothstep����ƽ������
                float innerAlpha = smoothstep(_InnerRadius - _Smoothness, _InnerRadius + _Smoothness, dist);
                float outerAlpha = 1.0 - smoothstep(_OuterRadius - _Smoothness, _OuterRadius + _Smoothness, dist);
                
                // �������Բ����͸����
                float alpha = innerAlpha * outerAlpha;
                
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}