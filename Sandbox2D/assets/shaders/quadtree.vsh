#version 430 core

in vec3 aPosition;

uniform float Scale; // current zoom (size multiplier)
uniform vec2 Translation; // current translation from the center of `RenderRoot`
uniform vec2 ScreenSize; // the size of the screen, in pixels

out vec2 WorldPos;

// converts vertex coordinates to screen cordinates
uvec2 VertexToScreenCoordinates(vec2 vertexCoords) {

    // add 1 tp vertexCoords, to get it to a 0-2 range
    vertexCoords += 1.0;

    // divide screenCoords by 2, to get it to a 0-1 range
    vertexCoords /= 2.0;

    // multiply screenCoords by screenSize
    vertexCoords *= ScreenSize;

    // convert to uvec2, and return
    return uvec2(vertexCoords);
}

// converts screen corrdinates to world coordinates
vec2 ScreenToWorldCoordinates(vec2 screenCoords) {

    vec2 center = ScreenSize / 2.0;
    return ((screenCoords - center) / Scale) + Translation + center;
}

void main(void)
{
    // calculate the world pos
    WorldPos = ScreenToWorldCoordinates(VertexToScreenCoordinates(aPosition.xy));
    
    gl_Position = vec4(aPosition, 1.0);
}
