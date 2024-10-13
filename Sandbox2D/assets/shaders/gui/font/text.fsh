#version 430

in vec2 glyphCoord;
out vec4 outputColor;

uniform ivec2 glyphAtlasSize;
uniform vec3 color;

uniform sampler2D texture0;

layout(std430, binding = 0) buffer charBuffer
{
    uint string[];
};

layout(std430, binding = 1) buffer lineBuffer
{
    uint lineIndexes[];
};

void main()
{
    uvec2 glyphIndex = uvec2(uint(glyphCoord.x), uint(glyphCoord.y));
    
    uint lineIndex = lineIndexes[glyphIndex.y];
    uint nextLineIndex = lineIndexes[glyphIndex.y + 1];
    if (lineIndex + glyphIndex.x >= nextLineIndex) {
        outputColor = vec4(0, 0, 0, 0);
        return;
    }
    uint charIndex = string[lineIndex + glyphIndex.x];
    
    vec2 glyphTexIndex = vec2(charIndex % glyphAtlasSize.x, charIndex / glyphAtlasSize.x) / glyphAtlasSize;
    vec2 subGlyphTexCoord = vec2(glyphCoord.x - glyphIndex.x, glyphCoord.y - glyphIndex.y + 8) / glyphAtlasSize;
    
    glyphTexIndex = vec2(glyphTexIndex.x, 1-glyphTexIndex.y);
    subGlyphTexCoord = vec2(subGlyphTexCoord.x, 1-subGlyphTexCoord.y);
    
    vec2 glyphTexCoord = glyphTexIndex + subGlyphTexCoord;
    glyphTexCoord = vec2(glyphTexCoord.x, glyphTexCoord.y);
    
    vec4 glyphSample = texture(texture0, glyphTexCoord);
    
    outputColor = glyphSample * vec4(color, 1);
}
