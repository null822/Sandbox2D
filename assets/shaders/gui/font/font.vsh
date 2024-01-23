#version 330 core

in vec3 aPosition;
in vec2 aTexCoord;

out vec2 texCoord;

uniform float scale;


void main(void)
{
    texCoord = aTexCoord;

    gl_Position = vec4(aPosition * scale, 1.0);
}