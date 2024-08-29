#version 330

out vec4 outputColor;

in vec3 color;

void main()
{
    outputColor = vec4(color.xyz, 1);
}
