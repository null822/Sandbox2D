﻿#version 400 core

in ivec2 worldPos;

out vec2 pixelWorldPos;
out vec2 vertexPos;
out vec3 color;

uniform float scale;
uniform float renderScale;
uniform vec2 translation;
uniform vec2 screenSize;

// https://stackoverflow.com/a/17479300 (next 9 functions)

// A single iteration of Bob Jenkins' One-At-A-Time hashing algorithm.
uint hash( uint x ) {
    x += ( x << 10u );
    x ^= ( x >>  6u );
    x += ( x <<  3u );
    x ^= ( x >> 11u );
    x += ( x << 15u );
    return x;
}
// Compound versions of the hashing algorithm I whipped together.
uint hash( uvec2 v ) { return hash( v.x ^ hash(v.y)                         ); }
uint hash( uvec3 v ) { return hash( v.x ^ hash(v.y) ^ hash(v.z)             ); }
uint hash( uvec4 v ) { return hash( v.x ^ hash(v.y) ^ hash(v.z) ^ hash(v.w) ); }

// Construct a float with half-open range [0:1] using low 23 bits.
// All zeroes yields 0.0, all ones yields the next smallest representable value below 1.0.
float floatConstruct( uint m ) {
    const uint ieeeMantissa = 0x007FFFFFu; // binary32 mantissa bitmask
    const uint ieeeOne      = 0x3F800000u; // 1.0 in IEEE binary32

    m &= ieeeMantissa;                     // Keep only mantissa bits (fractional part)
    m |= ieeeOne;                          // Add fractional part to 1.0

    float  f = uintBitsToFloat( m );       // Range [1:2]
    return f - 1.0;                        // Range [0:1]
}



// Pseudo-random value in half-open range [0:1].
float random( float x ) { return floatConstruct(hash(floatBitsToUint(x))); }
float random( vec2  v ) { return floatConstruct(hash(floatBitsToUint(v))); }
float random( vec3  v ) { return floatConstruct(hash(floatBitsToUint(v))); }
float random( vec4  v ) { return floatConstruct(hash(floatBitsToUint(v))); }


void main(void)
{
    // calculate center
    vec2 center = screenSize / 2f;
    
    // calculate screenPos
    vec2 screenPos = (((worldPos + 1) * renderScale) + translation - center) * scale + center;
    screenPos = vec2(screenPos.x, screenSize.y - screenPos.y);

    // calculate vertexPos
    vertexPos = screenPos / screenSize * 2 - 1;
    vertexPos.y = -vertexPos.y;
    
    pixelWorldPos = worldPos + 1;
    
    color = vec3(random(pixelWorldPos), random(pixelWorldPos + 0.01), random(pixelWorldPos + 0.02));
    
    gl_Position = vec4(vertexPos, 0.0, 1.0);
    
}