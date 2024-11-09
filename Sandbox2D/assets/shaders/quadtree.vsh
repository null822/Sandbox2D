#version 430 core

#ifdef GL_ARB_gpu_shader_int64

#extension GL_ARB_gpu_shader_int64: enable

#define BIT_DEPTH 64
#define INT64 int64_t
#define UINT64 uint64_t
#define VEC64 i64vec2
#define UVEC64 u64vec2

#else

#define BIT_DEPTH 32
#define INT64 int
#define UINT64 uint
#define VEC64 ivec2
#define UVEC64 uvec2

#endif

#define MIN64 INT64(INT64(-1) << (BIT_DEPTH - 1))
#define MAX64 INT64(-(MIN64 + 1))

in vec3 aPosition;

uniform vec2 ScreenSize; // the size of the screen, in pixels
uniform double Scale; // current zoom (size multiplier)
uniform VEC64 Translation; // current translation from the center of `RenderRoot`, rounded to the nearest integer
uniform vec2 SubTranslation; // decimal part of the current translation from the center of `RenderRoot`
uniform int MaxHeight; // the amount of height levels in the quadtree to be rendered

out smooth vec2 ScreenCoords;

// converts vertex coordinates to screen cordinates
vec2 VertexToScreenCoords(vec2 vertexCoords)
{
    vertexCoords = vec2(vertexCoords.x, -vertexCoords.y);
    vertexCoords += 1.0;
    vertexCoords /= 2.0;
    
    vertexCoords *= ScreenSize;
    return vertexCoords;
}

// converts screen coordinates to vertex cordinates
vec2 ScreenToVertexCoords(vec2 screenCoords)
{
    screenCoords /= ScreenSize;
    screenCoords *= 2.0;
    screenCoords -= 1.0;
    screenCoords = vec2(screenCoords.x, -screenCoords.y);
    
    return screenCoords;
}

vec2 WorldToScreenCoords(VEC64 worldCoords)
{
    dvec2 untranslated;
    
    if ((worldCoords.x >= 0 && MAX64 - worldCoords.x < Translation.x) || (worldCoords.x < 0 && worldCoords.x - MIN64 > Translation.x))
    {
        untranslated.x = (worldCoords.x * Scale) + (Translation.x * Scale);
    }
    else
    {
        untranslated.x = ((worldCoords.x + Translation.x) * Scale);
    }
    
    if ((worldCoords.y >= 0 && MAX64 - worldCoords.y < Translation.y) || (worldCoords.y < 0 && worldCoords.y - MIN64 > Translation.y))
    {
        untranslated.y = (worldCoords.y * Scale) + (Translation.y * Scale);
    }
    else
    {
        untranslated.y = ((worldCoords.y + Translation.y) * Scale);
    }
    
    vec2 screenCoords = vec2(untranslated + (SubTranslation * Scale));
    screenCoords = vec2(screenCoords.x, -screenCoords.y);
    screenCoords += ScreenSize / 2.0;
    
    return screenCoords;
}

void main(void)
{
    const INT64 minDistanceWorld = INT64(-1) << (MaxHeight - 1);
    const INT64 maxDistanceWorld = -(minDistanceWorld + 1);
    const vec2 minPos = ScreenToVertexCoords(WorldToScreenCoords(VEC64(minDistanceWorld)));
    const vec2 maxPos = ScreenToVertexCoords(WorldToScreenCoords(VEC64(maxDistanceWorld)) + vec2(vec2(1, -1) * Scale)); // add 1 to world coords to make both min and max inclusive (for clamping)
    
    #define CLAMP true
    
    vec2 vertexPosition = CLAMP ? clamp(aPosition.xy, minPos, maxPos) : aPosition.xy;
    
    ScreenCoords = VertexToScreenCoords(vertexPosition);
    
    gl_Position = vec4(vertexPosition, 0.0, 1.0);
}
