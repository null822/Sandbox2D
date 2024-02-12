#version 330

in vec2 texCoord;

out vec4 outputColor;

uniform sampler2D texture0;

void main()
{
    vec4 textureSample = texture(texture0, texCoord);
    
    outputColor = textureSample;
}