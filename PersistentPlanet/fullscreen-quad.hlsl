Texture2D shaderTexture;
SamplerState SampleType;

struct VS_Output
{
	float4 Pos : SV_POSITION;
	float2 Tex : TEXCOORD0;
};

VS_Output VS(uint id : SV_VertexID)
{
	VS_Output Output;
	Output.Tex = float2(id & 1, id >> 1);
	Output.Pos = float4((Output.Tex.x - 0.5f) * 2, -(Output.Tex.y - 0.5f) * 2, 0, 1);
	return Output;
}

float4 PS(VS_Output input) : SV_TARGET
{
	float4 color = shaderTexture.Sample(SampleType, input.Tex);

	float4 outputColor = color;
	outputColor.r = (color.r * 0.393) + (color.g * 0.769) + (color.b * 0.189);
	outputColor.g = (color.r * 0.349) + (color.g * 0.686) + (color.b * 0.168);
	outputColor.b = (color.r * 0.272) + (color.g * 0.534) + (color.b * 0.131);

	return outputColor;

	//return shaderTexture.Sample(SampleType, input.Tex);
}