#version 400

in vec2 vertexPos;
in vec2 pixelWorldPos;

out vec4 outputColor;


// https://stackoverflow.com/a/17479300 (next 4 groups of functions (9 total))

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


void main()
{
    
    vec2 worldPos1 = vec2(floor(pixelWorldPos.x * 1), floor(pixelWorldPos.y * 1));
    vec2 worldPos2 = vec2(floor(pixelWorldPos.x * 0.5), floor(pixelWorldPos.y * 0.5));
    vec2 worldPos3 = vec2(floor(pixelWorldPos.x * 0.25), floor(pixelWorldPos.y * 0.25));
    vec2 worldPos4 = vec2(floor(pixelWorldPos.x * 0.125), floor(pixelWorldPos.y * 0.125));
    vec2 worldPos5 = vec2(floor(pixelWorldPos.x * 0.0625), floor(pixelWorldPos.y * 0.0625));
    vec2 worldPos6 = vec2(floor(pixelWorldPos.x * 0.03125), floor(pixelWorldPos.y * 0.03125));
    vec2 worldPos7 = vec2(floor(pixelWorldPos.x * 0.015625), floor(pixelWorldPos.y * 0.015625));
    vec2 worldPos8 = vec2(floor(pixelWorldPos.x * 0.0078125), floor(pixelWorldPos.y * 0.0078125));

    vec4 baseColor = vec4(0.498, 0.498, 0.498, 1);

    float valueOc1 = random(worldPos1);
    float valueOc2 = random(worldPos2);
    float valueOc3 = random(worldPos3);
    float valueOc4 = random(worldPos4);
    float valueOc5 = random(worldPos5);
    float valueOc6 = random(worldPos6);
    float valueOc7 = random(worldPos7);
    float valueOc8 = random(worldPos8);



    float value = (valueOc1 + valueOc2 + valueOc3 + valueOc4 + valueOc5 + valueOc6 + valueOc7 + valueOc8) * 0.125;


    outputColor = baseColor * value;

}
