#nullable enable
using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Maths;

namespace Sandbox2D.Graphics.Renderables;

public class FontRenderable : Renderable
{
    private static readonly Vec2<int> GlyphSize = (5, 9);
    private static readonly Vec2<int> TextureSize = (64, 96);
    
    private float _scale;

    private readonly Texture _texture;
    
    // geometry arrays
    private readonly List<float> _vertices = 
    [
         0.5f,  0.5f, 0.0f, // top right
         0.5f, -0.5f, 0.0f, // bottom right
        -0.5f, -0.5f, 0.0f, // bottom left
        -0.5f,  0.5f, 0.0f  // top left
    ];
    private readonly List<uint> _indices = 
    [
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    ];
    
    
    public FontRenderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw) : base(shader, hint)
    {
        // update the vao (creates it, in this case)
        UpdateVao(hint);
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        
        var texCoordLocation = shader.GetAttribLocation("aTexCoord");
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        
        // set up uniforms
        var scaleLocation = GL.GetUniformLocation(Shader.Handle, "scale");
        GL.Uniform1(scaleLocation, 1, ref _scale);
        
        _texture = Textures.Font;
        _texture.Use(TextureUnit.Texture0);
    }
    
    public override void Render(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category) || !ShouldRender)
            return;

        base.Render(category);
        
        GL.BindVertexArray(VertexArrayObject);
        
        // enable transparency
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        _texture.Use(TextureUnit.Texture0);
        Shader.Use();
        
        Shader.SetFloat("scale", _scale);

        GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
        
        // disable transparency
        GL.Disable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);

    }
    
    public sealed override void UpdateVao(BufferUsageHint? hint = null,
        RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category))
            return;

        base.UpdateVao(hint, category);
        
        // bind vao
        GL.BindVertexArray(VertexArrayObject);
        
        // bind/update vbo
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(float), _vertices.ToArray(), Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Count * sizeof(uint), _indices.ToArray(), Hint);
    }

    /// <summary>
    /// Sets the text. Does not update the VAO
    /// </summary>
    /// <param name="text">text to display</param>
    /// <param name="tl">the top left vertex, in screen coordinates</param>
    /// <param name="size">the size of the text</param>
    /// <param name="center"></param>
    public void SetText(string text, Vec2<int> tl, float size, bool center = true)
    {
        _scale = size;
        
        var screenSize = GameManager.ScreenSize;
        var screenCenter = screenSize / 2;

        var textScreenSize = new Vec2<float>(text.Length * (GlyphSize.X + 1) * size, GlyphSize.Y * size);
        
        var tlScreen = (Vec2<int>)((Vec2<float>)tl / size) + screenCenter + 
                       (center 
                           ? new Vec2<int>((int)(-textScreenSize.X / 2f), (int)(textScreenSize.Y / 2f)) 
                           : new Vec2<int>(0));

        // flip coordinate the correct way
        tlScreen = new Vec2<int>(tlScreen.X, screenSize.Y-tlScreen.Y);
        
        var glyphArea = (Vec2<int>)((Vec2<float>)(GlyphSize + (1, 1)) * new Vec2<float>(_scale));

        for (var i = 0; i < text.Length; i++)
        {
            AddGlyph(text[i], tlScreen + new Vec2<int>(i * glyphArea.X, 0));
        }
        
        GeometryAdded();
    }
    
    /// <summary>
    /// Adds a glyph to the geometry. Does not update the VAO
    /// </summary>
    /// <param name="glyph">the character to add</param>
    /// <param name="tl">the top left vertex, in screen coordinates</param>
    private void AddGlyph(char glyph, Vec2<float> tl)
    {
        // get the next available index in _vertices
        var indexOffset = (uint)(_vertices.Count / 5f);

        var glyphScreenSize = (Vec2<float>)GlyphSize * new Vec2<float>(_scale);

        var texCoords = CharToGlyphCoords(glyph);
        
        var tlVert = Util.ScreenToVertexCoords(tl);
        var brVert = Util.ScreenToVertexCoords(tl + glyphScreenSize);
        
        // add the new vertices
        _vertices.AddRange(new []
        {
            brVert.X, -tlVert.Y, 0.0f, texCoords.Br.X, texCoords.Tl.Y, // right top
            brVert.X, -brVert.Y, 0.0f, texCoords.Br.X, texCoords.Br.Y, // right bottom
            tlVert.X, -brVert.Y, 0.0f, texCoords.Tl.X, texCoords.Br.Y, // left  bottom
            tlVert.X, -tlVert.Y, 0.0f, texCoords.Tl.X, texCoords.Tl.Y  // left  top
        });
        
        // create and add the new indices
        _indices.AddRange(new []
        {
            indexOffset + 0, indexOffset + 1, indexOffset + 3,   // first triangle
            indexOffset + 1, indexOffset + 2, indexOffset + 3    // second triangle
        });
    }

    private static (Vec2<float> Tl, Vec2<float> Br) CharToGlyphCoords(char character)
    {
        var index = character switch
        {
            'a' =>  0,
            'b' =>  1,
            'c' =>  2,
            'd' =>  3,
            'e' =>  4,
            'f' =>  5,
            'g' =>  6,
            'h' =>  7,
            'i' =>  8,
            'j' =>  9,
            'k' => 10,
            'l' => 11,
            'm' => 12,
            'n' => 13,
            'o' => 14,
            'p' => 15,
            'q' => 16,
            'r' => 17,
            's' => 18,
            't' => 19,
            'u' => 20,
            'v' => 21,
            'w' => 22,
            'x' => 23,
            'y' => 24,
            'z' => 25,
            'A' => 26,
            'B' => 27,
            'C' => 28,
            'D' => 29,
            'E' => 30,
            'F' => 31,
            'G' => 32,
            'H' => 33,
            'I' => 34,
            'J' => 35,
            'K' => 36,
            'L' => 37,
            'M' => 38,
            'N' => 39,
            'O' => 40,
            'P' => 41,
            'Q' => 42,
            'R' => 43,
            'S' => 44,
            'T' => 45,
            'U' => 46,
            'V' => 47,
            'W' => 48,
            'X' => 49,
            'Y' => 50,
            'Z' => 51,
            '0' => 52,
            '1' => 53,
            '2' => 54,
            '3' => 55,
            '4' => 56,
            '5' => 57,
            '6' => 58,
            '7' => 59,
            '8' => 60,
            '9' => 61,
            '!' => 62,
            '@' => 63,
            '#' => 64,
            '$' => 65,
            '%' => 66,
            '^' => 67,
            '&' => 68,
            '*' => 69,
            '?' => 70,
            ':' => 71,
            ';' => 72,
            '.' => 73,
            ',' => 74,
           '\'' => 75,
           '\"' => 76,
            '(' => 77,
            ')' => 78,
            '[' => 79,
            ']' => 80,
            '{' => 81,
            '}' => 82,
            '<' => 83,
            '>' => 84,
            '+' => 85,
            '-' => 86,
            '=' => 87,
            '_' => 88,
            '/' => 89,
           '\\' => 90,
            '|' => 91,
            '`' => 92,
            '~' => 93,
            ' ' => 94,
            _   => 95
        };
        
        var index2D = new Vec2<int>(index % 12, (int)Math.Floor(index / 12f));
        
        
        var texCoordTl = (Vec2<float>)index2D * (Vec2<float>)GlyphSize / (Vec2<float>)TextureSize;
        
        // flip the vertex position correctly
        texCoordTl = new Vec2<float>(texCoordTl.X, -texCoordTl.Y);
        
        var brOffset = (Vec2<float>)GlyphSize / (Vec2<float>)TextureSize;
        var texCoordBr = texCoordTl + brOffset;
        
        var offset = new Vec2<float>(0, -(GlyphSize.Y / (float)TextureSize.Y));

        return (texCoordTl + offset, texCoordBr + offset);
    }

    /// <summary>
    /// Resets the geometry of this renderable. Does not update the VAO
    /// </summary>
    public override void ResetGeometry(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category))
            return;

        base.ResetGeometry(category);
        
        _vertices.Clear();
        _indices.Clear();
    }
    
}