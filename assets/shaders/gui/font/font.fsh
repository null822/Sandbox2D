#version 330

in vec2 texCoord;

out vec4 outputColor;

uniform sampler2D texture0;

void main()
{
    vec4 textureSample = texture(texture0, texCoord);
    
    bool state = textureSample == vec4(1, 1, 1, 1);
    
    outputColor = state ? vec4(0.5, 0.5, 0.5, 1) : vec4(0, 0, 0, 0);
}