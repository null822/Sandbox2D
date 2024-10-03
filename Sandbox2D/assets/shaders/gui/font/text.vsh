#version 430 core

in vec3 aPosition;
in vec2 aGlyphCoord;

out vec2 glyphCoord;


void main(void)
{

    glyphCoord = aGlyphCoord;
    
//    vec2 pos = vertexToScreenCoordinates(aPosition.xy);
//    
//    vec2 gPosition = (pos - topLeft) / size * glyphSize;
//    
//    glpyhPosition = gPosition / screenSize;
    
    gl_Position = vec4(aPosition, 1.0);
}