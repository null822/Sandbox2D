#version 400

in vec2 vertexPos;
in vec2 pixelWorldPos;
in vec3 color;

out vec4 outputColor;


void main()
{
    outputColor = vec4(color, 1);
}
