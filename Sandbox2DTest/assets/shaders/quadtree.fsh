#version 430

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

#define INT32 int

in smooth vec2 ScreenCoords;

uniform vec2 ScreenSize; // the size of the screen, in pixels
uniform double Scale; // current zoom (size multiplier)
uniform VEC64 Translation; // current translation from the center of `RenderRoot`, rounded to the nearest integer
uniform vec2 SubTranslation; // decimal part of the current translation from the center of `RenderRoot`
uniform int MaxHeight; // the amount of height levels in the quadtree to be rendered

out vec4 outputColor;


struct QuadtreeNode
{
    uint Type;
    
    uint Ref0L;
    uint Ref0U;
    uint Ref1L;
    uint Ref1U;
    uint Ref2L;
    uint Ref2U;
    uint Ref3L;
    uint Ref3U;
};

struct Tile
{
    uint Upper;
    uint Lower;
};

layout(std430, binding = 0) buffer TreeBuffer
{
    QuadtreeNode Tree[];
};

layout(std430, binding = 1) buffer DataBuffer
{
    Tile Data[];
};

const int Branch = 0;
const int Leaf = 1;

UINT64 Unsign(INT64 i, int b)
{
    UINT64 mask = b == BIT_DEPTH ? ~UINT64(0) : ~(~UINT64(0) << b);
    
    return (UINT64(i) & mask) ^ (UINT64(0x1u) << (b-1));
}

UVEC64 Unsign(VEC64 coords, int b)
{
    return UVEC64(Unsign(coords.x, b), Unsign(coords.y, b));
}

INT32 GetNodeRef(QuadtreeNode node, uint index)
{
    if (node.Type != Branch) {
        return -16;
    }
    
    // Node ref positions:
    // 2 | 3
    // --+--
    // 0 | 1
    
    switch (index) {
        case 0: return INT32(node.Ref0L); // -X -Y | BL |
        case 1: return INT32(node.Ref1L); // +X -Y | BR |
        case 2: return INT32(node.Ref2L); // -X +Y | TL |
        case 3: return INT32(node.Ref3L); // +X +Y | TR |
    }
    
    return -17;
}

uint GetTileId(Tile tile)
{
    return tile.Lower >> 16;
}
    
INT32 GetNode(VEC64 coords)
{
    // map the coords to unsigned integers
    UVEC64 uCoords = Unsign(coords, MaxHeight);
    
    // start at the render root, not the acutal root
    INT32 nodeRef = 0;
    QuadtreeNode node = Tree[0];
    
    // for every height level below the max render height
    for (int height = MaxHeight-1; height >= 0; height--)
    {
        // if we found a leaf node, exit the loop and return it
        if (node.Type == Leaf) break;
        
        // calculate the index into the next (branch) node
        uint xBit = uint(uCoords.x >> height) & 0x1u;
        uint yBit = uint(uCoords.y >> height) & 0x1u;
        uint zPart = (yBit << 1) | xBit;
        
        // set the current node to the node within at index `zPart`
        nodeRef = GetNodeRef(node, zPart);
        
        // error handling
        if (nodeRef < 0) return nodeRef;
        if (nodeRef == 0) return -3;
        if (nodeRef > INT32(Tree.length())) return -2;
        
        // get the next node
        node = Tree[int(nodeRef)];
    }
    
    // if we have not found a leaf node, return an error
    if (node.Type == Branch) return -1;
    
    return nodeRef;
}


// converts screen coordinates to world coordinates, rounded to the nearest integer
VEC64 ScreenToWorldCoords(vec2 screenCoords)
{
    screenCoords -= ScreenSize / 2.0;
    screenCoords = vec2(screenCoords.x, -screenCoords.y);
    
    // flooring prevents rounding inconsistencies between +/- values when converting to integers
    dvec2 untranslated = floor((screenCoords / Scale) - SubTranslation);
    
    if (untranslated.x > double(MAX64) || untranslated.x < double(MIN64) || untranslated.y > double(MAX64) || untranslated.y < double(MIN64))
    {
        return VEC64(untranslated - vec2(Translation));
    }
    return VEC64(untranslated) - Translation;
}

void main()
{
    VEC64 worldCoords = ScreenToWorldCoords(ScreenCoords);
    INT32 nodeRef = GetNode(worldCoords);
    
    // error display
    if (nodeRef < 0) {
        int error = -int(nodeRef);
        outputColor = vec4(1, ((error / 256) % 256) / 256.0, (error % 256) / 256.0, 1);
        return;
    }
    
    Tile tile = Data[Tree[nodeRef].Ref0L];
    uint id = GetTileId(tile);
    
    uint outVal = 0;
    
    switch (id) {
        case 0: {
            outVal = 0;
            break;
        }
        case 1: {
            outVal = 1;
            break;
        }
        case 2: {
            outVal = 2;
            break;
        }
        case 3: {
            uint color = tile.Upper & 0x00ffffffu;
            outputColor = vec4((color >> 16) / 256.0, ((color >> 8) & 0xffu) / 256.0, (color & 0xffu) / 256.0, 1);
            return;
        }
        
    }
    
    outputColor = vec4(((outVal / 256) % 16) / 16.0, ((outVal / 16) % 16) / 16.0, (outVal % 16) / 16.0, 1);
}
