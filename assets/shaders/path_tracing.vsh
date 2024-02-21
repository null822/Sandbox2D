#version 430 core

in vec3 aPosition;

out vec3 position;

uniform float scale;
uniform float renderScale;
uniform vec2 translation;
uniform vec2 screenSize;


void main(void)
{
    position = aPosition;
    
    gl_Position = vec4(aPosition, 1.0);
}
