Shader "Unlit/Visualize"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        // LOD 100

        Pass
        {
			Name "DEFERRED"

			Tags{"LightMode" = "Deferred"}

			Stencil{
				Comp Always
				Pass Replace
				Ref 128
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog
			#pragma target 5.0

            #include "UnityCG.cginc"
			#include "../Cginc/Quaternion.cginc"

			struct VertexData {
				float3 vertex;
				float3 normal;
				float2 uv;
				float4 tangent;
			};
			struct InstanceData {
				float3 position;
				float4 rotation;
				float scale;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv		: TEXCOORD0;
				float3 normal	: TEXCOORD1;
				float4 tangent	: TEXCOORD2;
			};
			struct pOut {
				half4 diffuse           : SV_Target0; // RT0: diffuse color (rgb), occlusion (a)
				half4 spec_smoothness   : SV_Target1; // RT1: spec color (rgb), smoothness (a)
				half4 normal            : SV_Target2; // RT2: normal (rgb), --unused, very low precision-- (a) 
				half4 emission          : SV_Target3; // RT3: emission (rgb), --unused-- (a)
			};

			StructuredBuffer<uint> _Indices;
			StructuredBuffer<VertexData> _vData;
			StructuredBuffer<InstanceData> _iData;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Offset;

			v2f vert(uint vid : SV_VertexID, uint iid : SV_InstanceID)
			{
				uint idx = _Indices[vid];
				float4 pos = float4(_vData[idx].vertex, 1.0);
				float2 uv = _vData[idx].uv;
				float3 normal = _vData[idx].normal;
				float4 tangent = _vData[idx].tangent;
				float4 rotation = _iData[iid].rotation;

				pos.xyz *= _iData[iid].scale;
				pos.xyz = rotateWithQuaternion(pos.xyz, rotation);
				pos.xyz += _iData[iid].position;

				normal = rotateWithQuaternion(normal, rotation);

				v2f o;
				o.vertex = mul(UNITY_MATRIX_VP, pos);
				o.uv = uv;
				o.normal = normal;
				o.tangent = tangent;
				return o;
			}

			pOut frag(v2f i)
			{
				pOut o;
				o.diffuse = tex2D(_MainTex, i.uv);
				o.spec_smoothness = 0.2;
				o.normal = float4(i.normal*0.5 + 0.5, 0.0);
				o.emission = 0.0;
				return o;
			}

            //struct appdata
            //{
            //    float4 vertex : POSITION;
            //    float2 uv : TEXCOORD0;
            //};

            //struct v2f
            //{
            //    float2 uv : TEXCOORD0;
            //    UNITY_FOG_COORDS(1)
            //    float4 vertex : SV_POSITION;
            //};

            //sampler2D _MainTex;
            //float4 _MainTex_ST;

            //v2f vert (appdata v)
            //{
            //    v2f o;
            //    o.vertex = UnityObjectToClipPos(v.vertex);
            //    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            //    UNITY_TRANSFER_FOG(o,o.vertex);
            //    return o;
            //}

            //fixed4 frag (v2f i) : SV_Target
            //{
            //    // sample the texture
            //    fixed4 col = tex2D(_MainTex, i.uv);
            //    // apply fog
            //    UNITY_APPLY_FOG(i.fogCoord, col);
            //    return col;
            //}
            ENDCG
        }
    }
}
