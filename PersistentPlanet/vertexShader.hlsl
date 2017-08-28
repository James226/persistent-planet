cbuffer Global: register(b0)
{
	float4x4 ViewProj;
};

cbuffer Object
{
	float4x4 WorldMatrix;
};

struct Input
{
	float4 position : POSITION;
	float2 tex : TEXCOORD0;
	float3 normal : NORMAL;
};

struct Output
{
	float4 position : SV_POSITION;
	float2 tex : TEXCOORD0;
	float3 normal : NORMAL;
};

Output main(Input input)
{
	Output output;
	input.position.w = 1.0f;
	output.position = mul(input.position, WorldMatrix);
	output.position = mul(output.position, ViewProj);

	output.tex = input.tex;

	// Calculate the normal vector against the world matrix only.
	output.normal = mul(input.normal, (float3x3)WorldMatrix);

	// Normalize the normal vector.
	output.normal = normalize(input.normal);
	return output;
}