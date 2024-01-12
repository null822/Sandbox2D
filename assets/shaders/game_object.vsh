#version 330 core

in vec2 worldPos;

out vec2 pixelWorldPos;
out vec2 vertexPos;

uniform float scale;
uniform vec2 translation;
uniform vec2 screenSize;

void main(void)
{
    
    // calculate center
    vec2 center = screenSize / 2f;

    // calulate screenPos
    vec2 screenPos = (worldPos - 0.5 + translation - center) * scale + center;

    // calculate vertexPos
    vertexPos = screenPos / screenSize * 2 - 1;
    vertexPos.y = -vertexPos.y;
    
    pixelWorldPos = worldPos;
    
    gl_Position = vec4(vertexPos, 0.0, 1.0);
    
}
