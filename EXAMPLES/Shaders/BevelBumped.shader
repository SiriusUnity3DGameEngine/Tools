﻿Shader "Bevel/Bumped" {
	Properties{
	_MainTex("Base texture", 2D) = "white" {}
	_BumpMap("Normal Map", 2D) = "bump" {}
	[Toggle(CLIP_EDGES)] _CLIP("Clip Edges", Float) = 0
	}

		Category{
		Tags{
		"Queue" = "Geometry"
		"IgnoreProjector" = "True"
		"RenderType" = "Opaque"
		"LightMode" = "ForwardBase"
		"DisableBatching" = "True"
		"UVtype" = "Normal"
		"Solution" = "Bevel"
	}

		SubShader
	{
		Pass
	{


		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fwdbase
#pragma target 3.0
#include "UnityLightingCommon.cginc" 
#include "Lighting.cginc"
#include "UnityCG.cginc"
#include "AutoLight.cginc"
#include "Assets/Tools/SHARED/VertexDataProcessInclude.cginc"

#pragma multi_compile  ___ CLIP_EDGES
#pragma multi_compile  ___ MODIFY_BRIGHTNESS 
#pragma multi_compile  ___ COLOR_BLEED

		sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	sampler2D _BumpMap;

	struct v2f {
		float4 pos : SV_POSITION;
		float4 vcol : COLOR0;
		float3 worldPos : TEXCOORD0;
		float3 normal : TEXCOORD1;
		float2 texcoord : TEXCOORD2;
		float4 edge : TEXCOORD3;
		float3 snormal: TEXCOORD4;
		SHADOW_COORDS(5)
		float3 viewDir: TEXCOORD6;
		float3 edgeNorm0 : TEXCOORD7;
		float3 edgeNorm1 : TEXCOORD8;
		float3 edgeNorm2 : TEXCOORD9;
		float4 wTangent : TEXCOORD10;

	};
	v2f vert(appdata_full v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.normal.xyz = UnityObjectToWorldNormal(v.normal);


		o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
		o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;

		o.texcoord = v.texcoord.xy;
		o.vcol = v.color;
		o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, v.texcoord.w); //v.texcoord1;
		o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

		float3 deEdge = 1 - o.edge.xyz;

		o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
		o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
		o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

		o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

		TRANSFER_SHADOW(o);

		return o;
	}



	float4 frag(v2f i) : SV_Target
	{

	i.viewDir.xyz = normalize(i.viewDir.xyz);

	float4 col = tex2D(_MainTex, i.texcoord);

	float3 tnormal = UnpackNormal(tex2D(_BumpMap, i.texcoord));

	float weight;
	float3 normal = DetectSmoothEdge(i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight); //(i.edge.xyz);

	float deWeight = 1 - weight;
	col = col*deWeight + i.vcol*weight;

	float3 preNorm = normal;

	applyTangent (normal, tnormal,  i.wTangent);
	
	normal = normal*deWeight + preNorm*weight;

#if CLIP_EDGES
	clip(dot(i.viewDir.xyz, normal));
#endif
	
	float shadow = SHADOW_ATTENUATION(i);

	float dotprod = dot(i.viewDir.xyz, normal);					
	float3 reflected = normalize(i.viewDir.xyz - 2 * (dotprod)*normal);
	float dott = max(0.01, dot(_WorldSpaceLightPos0, -reflected));

	col.rgb *= ((max(0, dot(normal, _WorldSpaceLightPos0.xyz))
		* shadow)*_LightColor0 
		)*(1 - col.a);

	col.a += 0.01;

	float power = pow(col.a,8 );

	col.rgb += (pow(dott, 4096 * power)*(_LightColor0.rgb 
		)* power * 8 * shadow
		)
		*col.a ;


	return 
	col;

	}
		ENDCG
	}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
	}
}