cbuffer PerObject: register(b0)
{
	float4x4 WorldViewProj;
};

struct Input
{
	float4 position : POSITION;
	float3 normal : NORMAL;
};

struct Output
{
	float4 position : SV_POSITION;
	float3 normal : NORMAL;
};

Output main(Input input)
{
	Output output;
	output.position = mul(input.position, WorldViewProj);
	// Calculate the normal vector against the world matrix only.
	//output.normal = mul(input.normal, (float3x3)worldMatrix);

	// Normalize the normal vector.
	output.normal = normalize(input.normal);
	return output;
}