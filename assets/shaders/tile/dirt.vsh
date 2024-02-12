#version 400 core

in ivec2 worldPos;

out vec2 pixelWorldPos;
out vec2 vertexPos;

uniform float scale;
uniform float renderScale;
uniform vec2 translation;
uniform vec2 screenSize;

void main(void)
{
    // calculate center
    vec2 center = screenSize / 2.0;
    
    // calculate screenPos
    vec2 screenPos = (((worldPos + 1) * renderScale) + translation - center) * scale + center;
    screenPos = vec2(screenPos.x, screenSize.y - screenPos.y);

    // calculate vertexPos
    vertexPos = screenPos / screenSize * 2 - 1;
    vertexPos.y = -vertexPos.y;
    
    pixelWorldPos = worldPos + 1;
    
    gl_Position = vec4(vertexPos, 0.0, 1.0);
    
}
