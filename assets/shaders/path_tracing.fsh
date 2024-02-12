#version 430

in vec3 position;

out vec4 outputColor;

uniform float scale;
uniform vec2 translation;
uniform vec2 screenSize;

uniform sampler2D texture0;

struct QuadTreeStruct
{
    uint code;
    uint depth;

    uint id;
};

layout(std430, binding = 0) buffer aQTElements
{
    QuadTreeStruct world[];
};


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


// the maximum depth of the quad tree, or, half the code length in bits
const uint tree_depth = 16;

// wether to display errors or not
const bool show_errors = false;

// converts vertex coordinates to screen cordinates
uvec2 vertex_to_screen_coordinates(vec2 vertexCoords) {
    
    // add 1 tp vertexCoords, to get it to a 0-2 range
    vertexCoords += 1.0;
    
    // divide screenCoords by 2, to get it to a 0-1 range
    vertexCoords /= 2.0;
    
    // multiply screenCoords by screenSize
    vertexCoords *= screenSize;
    
    // convert to uvec2, and return
    return uvec2(vertexCoords);
}

// converts screen corrdinates to world coordinates
vec2 screen_to_world_coordinates(vec2 screenCoords) {
    
    vec2 center = screenSize / 2.0;
    vec2 value = ((screenCoords - center) / scale) + translation + center;
    
    return value;
}

// returns the significant part of a code, given a depth
uint get_code_part(uint code, uint depth) {
    return code >> (depth * 2u) & 0x3u;
}

// interleaves 2D coordinates
uint interleave(uint x, uint y)
{
    uint code = 0;
    
    for(int i = 0; i < tree_depth; i++) {
        code |= (x >> (tree_depth-1-i) & 0x1u) << i*2;
        code |= (y >> (tree_depth-1-i) & 0x1u) << i*2+1;
    }
    
    return code;
}

// finds the index within _world for the specified coordinates
QuadTreeStruct get_tile(ivec2 coords)
{
    const int size = (0x1 << tree_depth) / 2;
    
    // if the coords are out of bounds, return an error of 1
    if (coords.x >= size || coords.y > size || coords.x < -size || coords.y <= -size) {
        return QuadTreeStruct(0, tree_depth+1, 0);
    }
    
    uvec2 u_coords = uvec2(uint(coords.x + size), uint(-coords.y + size));
    
    // calculate the code for the coordinates
    uint required_code = interleave(u_coords.x, u_coords.y);

    uint current_index = 0;
    
    // for each depth level,
    for (uint d = 0; d <= tree_depth; d++) {

        // get what the code part must look like
        const uint required_part = get_code_part(required_code, d);

        bool correct_part;
        uint index_offset = 0;
        
        do {
            // get the element at the index
            const QuadTreeStruct element = world[current_index + index_offset];

            // check the next part
            const uint part = get_code_part(element.code, d);

            correct_part = part == required_part;

            // if the part is correct and the element does not go deeper, it is the correct element, so return it
            if (element.depth <= d+1 && correct_part) {
                return element;
            }

            // and move the index to the next part
            index_offset++;
            
            // if we have looped through the entire world and not found the index, return an error of 2
            if (element.depth > tree_depth) {
                return QuadTreeStruct(0, tree_depth+2, 0);
            }
            
            // while the part is incorrect, repeat
        } while (!correct_part);
        
        // we found the required code part, add the index offset to the current index
        current_index += index_offset-1;
    }

    // we found the correct index, so return it
    return world[current_index];

}

void main()
{
    float error_max = 2;
    float id_max = 2;
    
    vec2 world_pos = screen_to_world_coordinates(vertex_to_screen_coordinates(position.xy));
    ivec2 pixel_world_pos = ivec2(int(floor(world_pos.x)), int(floor(world_pos.y)));
    
    QuadTreeStruct qts = get_tile(pixel_world_pos);
    
    bool isLine = false;
    
    if (abs(pixel_world_pos.x) <= 0.5 / scale || abs(pixel_world_pos.y) <= 0.5 / scale) {
        isLine = true;
    }

    uint error = 0;
    
    if (qts.depth > tree_depth) {
        error = (qts.depth - tree_depth);
    }

    if (error != 0) {
        
        if (!show_errors) {
            outputColor = vec4(isLine, 0, 0, 1);
            return;
        }
        
        float errorScaled = float(error) / error_max;
        
        outputColor = vec4(float(isLine) / 4.0 + error, error, 0, 1);
        return;
        
    }
    
    vec2 texCoord = (vec2(pixel_world_pos % ivec2(8, 8) + vec2(qts.id * 8, floor(qts.id * 8 / 256))) + vec2(0, 1)) / 256.0;
    
    texCoord.y = 1-texCoord.y;
    
    vec4 textureSample = texture(texture0, texCoord);
    
//    outputColor = mix(textureSample, vec4(isLine, 0, 0, 1), 0.5);
    outputColor = textureSample;
    
}
