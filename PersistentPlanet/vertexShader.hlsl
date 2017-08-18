cbuffer PerObject: register(b0)
{
	float4x4 WorldViewProj;
};

float4 main(float4 position : POSITION) : SV_POSITION
{
	return mul(position, WorldViewProj);
}