using System;
using System.Linq;
using System.Text;
using Math2D;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Sandbox2D.Managers;
using Sandbox2D.Registry_;

namespace Sandbox2D.Graphics.ShaderControllers;

public class TextRenderer : ShaderController
{
    private readonly int _vertexArrayObject = GL.GenVertexArray();
    private readonly int _vertexBufferObject = GL.GenBuffer();
    private readonly int _elementBufferObject = GL.GenBuffer();
    
    private static readonly Vec2<int> GlyphAtlasSize = (32, 8);
    private static readonly Vec2<int> GlyphSize = (6, 10);
    
    private Vector3 _color = Vector3.One;
    
    private readonly int _charBuffer;
    private readonly int _lineBuffer;
    
    private readonly Texture _glyphTexture;
    
    // geometry arrays
    private float[] _vertices = 
    [
        0.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 0.0f, 0.0f, 0.0f
    ];
    private readonly uint[] _indices = 
    [
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    ];
    
    public TextRenderer(ShaderProgram shader, BufferUsageHint hint = BufferUsageHint.StaticDraw)
        : base(shader, hint)
    {
        _charBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _charBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<uint>(), Hint);
        
        _lineBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _lineBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, 0, Array.Empty<uint>(), Hint);
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        
        // update the vao (creates it, in this case)
        UpdateVao();
        
        Shader.Use();
        
        // set up coords
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        
        var texCoordLocation = shader.GetAttribLocation("aGlyphCoord");
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        
        Shader.Set("glyphAtlasSize", GlyphAtlasSize.ToVector2i());
        
        _glyphTexture = GlRegistry.Texture.Get("font");
        // _glyphTexture.Use(TextureUnit.Texture0);
    }
    
    public override void Invoke()
    {
        // bind vao/buffers
        GL.BindVertexArray(_vertexArrayObject);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _charBuffer);
        
        // bind vao/buffers
        GL.BindVertexArray(_vertexArrayObject);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, _lineBuffer);
        
        // apply the glyph texture
        _glyphTexture.Use(TextureUnit.Texture0);
        
        // use the shader
        Shader.Use();
        
        // apply the uniforms
        Shader.Set("color", _color);
        
        // render the geometry
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        
        // unbind the buffer
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }

    private void UpdateVao()
    {
        // bind vao
        GL.BindVertexArray(_vertexArrayObject);
        
        // bind/update vbo
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, Hint);
    }
    
    public void SetText(string text, Vec2<float> tl, float size, Vec2<int> screenSize)
    {
        var lines = text.Split('\n');
        var lineSize = new Vec2<int>(lines.Select(line => line.Length).Max(), lines.Length);
        
        var br = new Vec2<float>(tl.X + lineSize.X * GlyphSize.X * size, tl.Y + lineSize.Y * GlyphSize.Y * size);
        
        var geometry = new Range2Df(
            RenderManager.ScreenToVertexCoords(tl, screenSize), 
            RenderManager.ScreenToVertexCoords(br, screenSize));
        
        _vertices = 
        [
            ..((Vec2<float>)geometry.Tr).ToArray(), lineSize.X, 0.0f,        // right top
            ..((Vec2<float>)geometry.Br).ToArray(), lineSize.X, lineSize.Y,  // right bottom
            ..((Vec2<float>)geometry.Bl).ToArray(), 0.0f,       lineSize.Y,  // left bottom
            ..((Vec2<float>)geometry.Tl).ToArray(), 0.0f,       0.0f         // left top
        ];
        
        var textUtf8 = Encoding.UTF8.GetBytes(text.Replace("\n", "")).Select(b => (uint)b).ToArray();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _charBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, textUtf8.Length * sizeof(uint), textUtf8, Hint);
        
        var lineIndexes = new uint[lines.Length + 1];
        var len = 0u;
        for (var i = 0; i < lines.Length; i++)
        {
            lineIndexes[i] = len;
            len += (uint)lines[i].Length;
        }
        lineIndexes[^1] = len;
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _lineBuffer);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, lineIndexes.Length * sizeof(uint), lineIndexes, Hint);
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        
        UpdateVao();
    }
    
    
    public void SetColor(Color color)
    {
        _color = color.ToVector3();
    }
    
    /// <summary>
    /// Resets the geometry of this renderable. Does not update the VAO
    /// </summary>
    public override void ResetGeometry()
    {
        _vertices = [
            0.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f
        ];
    }
}