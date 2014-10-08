cbuffer ConstantBufferChangesOnResize : register(b0)
{
	matrix cameraProjection;
};

cbuffer ConstantBufferChangesEveryFrame : register(b1)
{
	matrix cameraView;
};

cbuffer ConstantBufferChangesEveryPrim : register (b2)
{
	matrix modelWorldPosition;
	float4 meshColor;
	float4 diffuseColor;
	float4 specularColor;
	float  specularExponent;
};

// Per-vertex data used as input to the vertex shader.
struct VertexShaderInput
{
	float3 position : POSITION;	//float4 position : POSITION;	
	float3 normal : NORMAL;		//float4 normal : NORMAL;		
	float2 textureUV : TEXCOORD0;
};

// Per-pixel color data passed through the pixel shader.
struct PixelShaderInput
{
	float4 position : SV_POSITION;	//	float4 position : SV_POSITION;
	float2 textureUV : TEXCOORD0;	//	float4 color : COLOR0;
	float4 diffuseColor : TEXCOORD1;//						
};

// Simple shader to do vertex processing on the GPU.
PixelShaderInput main(VertexShaderInput input)
{
	PixelShaderInput output;
	float4 pos = float4(input.position, 1.0f);

	// Transform the vertex position into projected space.
	pos = mul(pos, modelWorldPosition);
	pos = mul(pos, cameraView);
	pos = mul(pos, cameraProjection);
	output.position = pos;

	// Fill out the pixel shader input and pass it.
	output.position = pos;
	output.textureUV = input.textureUV;
	output.diffuseColor = diffuseColor;

	return output;
}