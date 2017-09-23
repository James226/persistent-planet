#version 450

layout (binding = 2) uniform sampler2D sampler_Color;

layout (location = 0) in vec2 in_TexCoord;
layout (location = 1) in vec3 in_Normal;

layout (location = 0) out vec4 out_Color;

void main() {
    vec4 textureColor = texture(sampler_Color, in_TexCoord);

	vec3 lightDir = vec3(.5, -.5, -.5);
	float lightIntensity;
	vec4 diffuseColor = vec4(.8, .4, .5, 1);

	// Set the default output color to the ambient light value for all pixels.
	out_Color = vec4(.2, .2, .2, 1);

	// Calculate the amount of light on this pixel.
	lightIntensity = clamp(dot(in_Normal, lightDir), 0.0, 1.0);

	if (lightIntensity > 0.0f)
	{
		// Determine the final diffuse color based on the diffuse color and the amount of light intensity.
		out_Color += (diffuseColor * lightIntensity);
	}

	out_Color = clamp(out_Color, 0.0, 1.0);

	out_Color = out_Color * textureColor;
}
