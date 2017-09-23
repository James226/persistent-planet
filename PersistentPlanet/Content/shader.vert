#version 450

layout (location = 0) in vec3 in_Position;
layout (location = 1) in vec2 in_TexCoord;
layout (location = 2) in vec3 in_Normal;

layout (binding = 0) uniform PerFrame
{
    mat4 View;
    mat4 Projection;
};

layout (binding = 1) uniform PerObject
{
    mat4 World;
};

layout (location = 0) out vec2 out_TexCoord;
layout (location = 1) out vec3 out_Normal;

out gl_PerVertex
{
    vec4 gl_Position;
};

void main() {
    out_TexCoord = in_TexCoord;
    out_Normal = normalize(in_Normal * mat3(World));
    gl_Position = Projection * View * World * vec4(in_Position.xyz, 1.0);
}
