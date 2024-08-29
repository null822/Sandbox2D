#version 330 core

in vec3 aPosition;

out vec2 position;

void main(void)
{
    position = aPosition.xy;
    gl_Position = vec4(aPosition, 1.0);
}
