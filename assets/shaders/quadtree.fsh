#version 430

in vec2 WorldPos;
out vec4 outputColor;

uniform int MaxHeight; // the amount of height levels in the quadtree to be rendered

//uniform sampler2D texture0;

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

uint Unsign(int i)
{
    uint size = 0x1u << MaxHeight;
    uint halfSize = size / 2;
    
    uint u;
    
    if (i < 0)
        u = uint(i + int(halfSize));
    else
        u = uint(i + halfSize);
    
    return u;
}

// interleaves 2D coordinates
uint Interleave(uvec2 coords)
{
    uint x = coords.x;
    uint y = coords.y;

    x = (x & ~0xFF00FF00u) | (x & 0xFF00FF00u) << 8;
    x = (x & ~0xF0F0F0F0u) | (x & 0xF0F0F0F0u) << 4;
    x = (x & ~0xCCCCCCCCu) | (x & 0xCCCCCCCCu) << 2;
    x = (x & ~0xAAAAAAAAu) | (x & 0xAAAAAAAAu) << 1;

    y = (y & ~0xFF00FF00u) | (y & 0xFF00FF00u) << 8;
    y = (y & ~0xF0F0F0F0u) | (y & 0xF0F0F0F0u) << 4;
    y = (y & ~0xCCCCCCCCu) | (y & 0xCCCCCCCCu) << 2;
    y = (y & ~0xAAAAAAAAu) | (y & 0xAAAAAAAAu) << 1;
    y <<= 1;
    
    return x | y;
}

uint Interleave(ivec2 coords) {

    uvec2 u = uvec2(Unsign(coords.x), Unsign(coords.y));
    
    return Interleave(u);
}

int GetNodeRef(QuadtreeNode node, uint index) {
    
    if (node.Type != Branch) {
        return -16;
    }
    
    switch (index) {
        case 0:
            return int(node.Ref0L);
        case 1:
            return int(node.Ref1L);
        case 2:
            return int(node.Ref2L);
        case 3:
            return int(node.Ref3L);
    }
    return -17;
}

uint GetTileId(Tile tile) {
    return tile.Lower >> 16;
}

int GetNode(ivec2 coords) {
    
    // calculate the position's z-value
    uint zValue = Interleave(coords);
    
    // start at the render root, not the acutal root, since the actual root may encompass an area too large for 32-bit z-values
    int nodeRef = 0;
    QuadtreeNode node = Tree[0];
    
    for (int height = MaxHeight-1; height >= 0; height--)
    {
        // if we found a leaf node, exit the loop and return it
        if (node.Type == Leaf) break;
        
        // extract the relevant 2-bit section from the z-value
        uint zPart = (zValue >> (2*height)) & 0x3u;
        
        // set the current node to the node within at index `zPart`
        nodeRef = GetNodeRef(node, zPart);
        
        // error handling
        if (nodeRef < 0) return nodeRef;
        if (nodeRef == 0) return -3;
        if (nodeRef > Tree.length()) return -2;

        // get the next node
        node = Tree[nodeRef];
    }
    
    // if we have not found a leaf node, return an error
    if (Tree[nodeRef].Type == Branch) return -1;
    
    return nodeRef;
}

void main()
{
    const uint maxDistance = 0x1u << (MaxHeight - 1);
    
    if (abs(WorldPos.x) > maxDistance || abs(WorldPos.y) > maxDistance) {
        outputColor = vec4(1, 1, 1, 1);
        return;
    }
    ivec2 tileWorldPos = ivec2(int(floor(WorldPos.x)), int(floor(WorldPos.y)));
    
    int nodeRef = GetNode(tileWorldPos);
    
    // error display
    if (nodeRef < 0) {
        uint error = -nodeRef;
        outputColor = vec4(1, ((error / 16) % 16) / 16f, (error % 16) / 16f, 1);
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
    
    outputColor = vec4(((outVal / 256) % 16) / 16f, ((outVal / 16) % 16) / 16f, (outVal % 16) / 16f, 1);
}
