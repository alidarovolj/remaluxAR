Shader "Unlit/SegmentationShader"
{
    Properties
    {
        // This property is unused by the shader logic, but it is required to prevent
        // the ARCameraBackground component from throwing errors in the editor.
        _MainTex ("Camera Feed (Unused)", 2D) = "white" {}
        _MaskTex ("Segmentation Mask", 2D) = "black" {}
        _PaintColor ("Paint Color", Color) = (1,0,0,1)
        _TargetColor("Target Color", Color) = (0,1,0,1)
        _ColorThreshold("Color Threshold", Range(0, 1)) = 0.01
        _ShowSegmentation ("Show Segmentation", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

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

            sampler2D _MaskTex;
            fixed4 _PaintColor;
            fixed4 _TargetColor;
            float _ColorThreshold;
            float _ShowSegmentation;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                if (_ShowSegmentation < 0.5)
                {
                    return fixed4(0, 0, 0, 0); // Fully transparent
                }

                fixed4 maskColor = tex2D(_MaskTex, i.uv);
                
                float colorDiff = distance(maskColor.rgb, _TargetColor.rgb);

                if (colorDiff < _ColorThreshold)
                {
                    // Return the paint color. Alpha is controlled by the color picker.
                    return _PaintColor;
                }
                else
                {
                    // Otherwise, be fully transparent
                    return fixed4(0, 0, 0, 0);
                }
            }
            ENDCG
        }
    }
} 