#version 330 core

in vec3 aPosition;
in float aState;

out vec2 position;
out float state;

void main(void)
{
    position = aPosition.xy;
    state = aState;
    
    gl_Position = vec4(aPosition, 1.0);
}
