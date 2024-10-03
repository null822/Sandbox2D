#version 430

in vec2 glyphCoord;
out vec4 outputColor;

uniform ivec2 glyphAtlasSize;
uniform ivec2 glyphSize;
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
    uvec2 strCoord = uvec2(uint(glyphCoord.x), uint(glyphCoord.y));

    uint lineIndex = lineIndexes[strCoord.y];
    uint nextLineIndex = lineIndexes[strCoord.y + 1];
    if (lineIndex + strCoord.x >= nextLineIndex) {
        outputColor = vec4(0, 0, 0, 0);
        return;
    }
    uint glyphIndex = string[lineIndex + strCoord.x];
    
    vec2 glyphTexCoord = vec2(glyphIndex % glyphAtlasSize.x, glyphIndex / glyphAtlasSize.x) / glyphAtlasSize;
    vec2 pixelTexCoord = vec2(glyphCoord.x - strCoord.x, glyphCoord.y - strCoord.y) / glyphAtlasSize;
    
    glyphTexCoord = vec2(glyphTexCoord.x, 1-glyphTexCoord.y);
    pixelTexCoord = vec2(pixelTexCoord.x, 1-pixelTexCoord.y);
    
    vec2 glyph = glyphTexCoord + pixelTexCoord;
    
    vec4 glyphSample = texture(texture0, glyph);

    if (glyphSample.a == 0) {
        outputColor = vec4(0, 0, 0, 0);
    } else {
        outputColor = vec4(color, 1);
    }
    
}
