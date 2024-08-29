#version 330

in vec2 position;
in float state;

out vec4 outputColor;

void main()
{
    outputColor = state == 0 ? vec4(1, 0, 0, 1) : vec4(0, 1, 0, 1);
    
}
