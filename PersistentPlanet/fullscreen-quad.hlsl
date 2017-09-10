Texture2D shaderTexture;
SamplerState SampleType;

struct VS_Output
{
	float4 Pos : SV_POSITION;
	float2 Tex : TEXCOORD0;
	float2 texCoord1 : TEXCOORD1;
	float2 texCoord2 : TEXCOORD2;
	float2 texCoord3 : TEXCOORD3;
	float2 texCoord4 : TEXCOORD4;
	float2 texCoord5 : TEXCOORD5;
	float2 texCoord6 : TEXCOORD6;
	float2 texCoord7 : TEXCOORD7;
	float2 texCoord8 : TEXCOORD8;
	float2 texCoord9 : TEXCOORD9;
};

VS_Output VS(uint id : SV_VertexID)
{
	VS_Output Output;
	float texelSize = 1.0f / 1280.0f;
	Output.Tex = float2(id & 1, id >> 1);
	Output.Pos = float4((Output.Tex.x - 0.5f) * 2, -(Output.Tex.y - 0.5f) * 2, 0, 1);

	Output.texCoord1 = Output.Tex + float2(texelSize * -4.0f, 0.0f);
	Output.texCoord2 = Output.Tex + float2(texelSize * -3.0f, 0.0f);
	Output.texCoord3 = Output.Tex + float2(texelSize * -2.0f, 0.0f);
	Output.texCoord4 = Output.Tex + float2(texelSize * -1.0f, 0.0f);
	Output.texCoord5 = Output.Tex + float2(texelSize *  0.0f, 0.0f);
	Output.texCoord6 = Output.Tex + float2(texelSize *  1.0f, 0.0f);
	Output.texCoord7 = Output.Tex + float2(texelSize *  2.0f, 0.0f);
	Output.texCoord8 = Output.Tex + float2(texelSize *  3.0f, 0.0f);
	Output.texCoord9 = Output.Tex + float2(texelSize *  4.0f, 0.0f);


	return Output;
}

float4 PS(VS_Output input) : SV_TARGET
{
	float weight0, weight1, weight2, weight3, weight4;
	float normalization;
	float4 color;

	// Create the weights that each neighbor pixel will contribute to the blur.
	weight0 = 1.0f;
	weight1 = 0.9f;
	weight2 = 0.55f;
	weight3 = 0.18f;
	weight4 = 0.1f;

	// Create a normalized value to average the weights out a bit.
	normalization = (weight0 + 2.0f * (weight1 + weight2 + weight3 + weight4));

	// Normalize the weights.
	weight0 = weight0 / normalization;
	weight1 = weight1 / normalization;
	weight2 = weight2 / normalization;
	weight3 = weight3 / normalization;
	weight4 = weight4 / normalization;

	// Initialize the color to black.
	color = float4(0.0f, 0.0f, 0.0f, 0.0f);

	// Add the nine horizontal pixels to the color by the specific weight of each.
	color += shaderTexture.Sample(SampleType, input.texCoord1) * weight4;
	color += shaderTexture.Sample(SampleType, input.texCoord2) * weight3;
	color += shaderTexture.Sample(SampleType, input.texCoord3) * weight2;
	color += shaderTexture.Sample(SampleType, input.texCoord4) * weight1;
	color += shaderTexture.Sample(SampleType, input.texCoord5) * weight0;
	color += shaderTexture.Sample(SampleType, input.texCoord6) * weight1;
	color += shaderTexture.Sample(SampleType, input.texCoord7) * weight2;
	color += shaderTexture.Sample(SampleType, input.texCoord8) * weight3;
	color += shaderTexture.Sample(SampleType, input.texCoord9) * weight4;

	// Set the alpha channel to one.
	color.a = 1.0f;

	return color;
	
	/*float4 color = shaderTexture.Sample(SampleType, input.Tex);

	float4 outputColor = color;
	outputColor.r = (color.r * 0.393) + (color.g * 0.769) + (color.b * 0.189);
	outputColor.g = (color.r * 0.349) + (color.g * 0.686) + (color.b * 0.168);
	outputColor.b = (color.r * 0.272) + (color.g * 0.534) + (color.b * 0.131);

	return outputColor;*/

	//return shaderTexture.Sample(SampleType, input.Tex);
}