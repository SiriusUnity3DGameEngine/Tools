﻿Shader "Playtime Painter/Editor/Brush/Blit" {
	Properties{}

	Category{
		Tags{ 
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"LightMode" = "ForwardBase"
		}

		Blend SrcAlpha OneMinusSrcAlpha 
		ColorMask RGB
		Cull off
		ZTest off
		ZWrite off

		SubShader{
			Pass{

				CGPROGRAM
				#include "PlaytimePainter_cg.cginc"

				#pragma multi_compile  BRUSH_2D BRUSH_SQUARE  BRUSH_3D  BRUSH_3D_TEXCOORD2   BRUSH_DECAL
				#pragma multi_compile  ___ BLIT_MODE_COPY

				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#include "UnityCG.cginc"
				#include "UnityLightingCommon.cginc"

				#if BRUSH_2D || BRUSH_DECAL || BRUSH_SQUARE
				struct v2f {
					float4 pos : POSITION;
					float4 texcoord : TEXCOORD0;  
				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);   
					o.texcoord = brushTexcoord (v.texcoord.xy, v.vertex);
					return o;
				}
				#endif

				 #if BRUSH_3D || BRUSH_3D_TEXCOORD2

				struct v2f {
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD0;  
					float3 worldPos : TEXCOORD1;
				};


				v2f vert(appdata_full v) {

					v2f o;
					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
	
					#if BRUSH_3D_TEXCOORD2
					v.texcoord.xy = v.texcoord2.xy;
					#endif

					// ATLASED CALCULATION
					float atY = floor(v.texcoord.z / _qcPp_brushAtlasSectionAndRows.z);
					float atX = v.texcoord.z - atY * _qcPp_brushAtlasSectionAndRows.z;
					v.texcoord.xy = (float2(atX, atY) + v.texcoord.xy) / _qcPp_brushAtlasSectionAndRows.z
						* _qcPp_brushAtlasSectionAndRows.w + v.texcoord.xy * (1 - _qcPp_brushAtlasSectionAndRows.w);

					o.worldPos = worldPos.xyz;

					float2 tmp;

					worldPos.xyz = _qcPp_RTcamPosition.xyz;
					worldPos.z+=100;
					worldPos.xy+= (v.texcoord.xy*_qcPp_brushEditedUVoffset.xy+_qcPp_brushEditedUVoffset.zw-0.5)*256;

					v.vertex = mul(unity_WorldToObject, float4(worldPos.xyz,v.vertex.w));

					o.pos = UnityObjectToClipPos( v.vertex );

					o.texcoord.xy = ComputeScreenPos(o.pos);

					return o;
				}
				#endif

				float4 frag(v2f i) : COLOR{
	
					#if BLIT_MODE_COPY
	 				_qcPp_brushColor = tex2Dlod(_qcPp_SourceTexture, float4(i.texcoord.xy, 0, 0));
					#endif

					#if BRUSH_3D  ||   BRUSH_3D_TEXCOORD2
					float alpha = prepareAlphaSphere (i.texcoord, i.worldPos);
					
					clip(alpha - 0.000001);
					#endif

					#if BRUSH_2D
					float alpha = prepareAlphaSmooth (i.texcoord);
					#endif

					#if BRUSH_SQUARE
					float alpha = prepareAlphaSquare(i.texcoord);
					#endif

					#if BRUSH_DECAL
					float2 decalUV =i.texcoord.zw+0.5;
					float Height = tex2D(_VolDecalHeight, decalUV).a;
					float4 overlay = tex2D(_VolDecalOverlay, decalUV);
					float4 dest =  tex2Dlod(_qcPp_DestBuffer, float4(i.texcoord.xy, 0, 0));
					float alpha = saturate((Height-dest.a) * 8*_DecalParameters.y-0.01);

					float4 col = tex2Dlod(_qcPp_DestBuffer, float4(i.texcoord.xy, 0, 0));

					float changeColor = _DecalParameters.z;
					_qcPp_brushColor = overlay*overlay.a +  (_qcPp_brushColor*changeColor + col*(1-changeColor))*(1-overlay.a);

					_qcPp_brushColor.a = Height;
					#endif

					_qcPp_brushColor.a = alpha;


		
					return  _qcPp_brushColor;

				}
				ENDCG
			}
		}
	}
}
