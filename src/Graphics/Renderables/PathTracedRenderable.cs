using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Maths;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace Sandbox2D.Graphics.Renderables;

public class PathTracedRenderable : Renderable
{
    private Vector2 _translation = Vector2.Zero;
    private float _scale = 1;
    
    private readonly Texture _dynTilemap;

    private Vec2<long> _vertexOffset = new (0);
    private float _renderScale = 1;
    
    private QuadTreeStruct[] _geometry = [
        // new QuadTreeStruct(   0b_00_00, 2, 2),
        // new QuadTreeStruct(0b_00_01_00, 3, 3),
        // new QuadTreeStruct(0b_01_01_00, 3, 3),
        // new QuadTreeStruct(0b_10_01_00, 3, 3),
        // new QuadTreeStruct(0b_11_01_00, 3, 3),
        // new QuadTreeStruct(   0b_10_00, 2, 2),
        // new QuadTreeStruct(   0b_11_00, 2, 2),
        // new QuadTreeStruct(      0b_01, 1, 1),
        // new QuadTreeStruct(      0b_10, 1, 1),
        // new QuadTreeStruct(0b_00_00_11, 3, 3),
        // new QuadTreeStruct(0b_01_00_11, 3, 3),
        // new QuadTreeStruct(0b_10_00_11, 3, 3),
        // new QuadTreeStruct(0b_11_00_11, 3, 3),
        // new QuadTreeStruct(   0b_01_11, 2, 2),
        // new QuadTreeStruct(0b_00_10_11, 3, 3),
        // new QuadTreeStruct(0b_01_10_11, 3, 3),
        // new QuadTreeStruct(0b_10_10_11, 3, 3),
        // new QuadTreeStruct(0b_11_10_11, 3, 3),
        // new QuadTreeStruct(   0b_11_11, 2, 2)
    ];
    
    /// <summary>
    /// The SSBO that contains the world geometry to be rendered
    /// </summary>
    protected readonly int ShaderStorageBufferObject = GL.GenBuffer();


    
    /// <summary>
    /// Describes if ALL TileRenderables have been updated.
    /// If set to true, the VAO will automatically update upon render for every TileRenderable.
    /// </summary>
    private static bool _globalUpdatedSinceLastFrame;
    
    /// <summary>
    /// Describes if ALL TileRenderables have been updated.
    /// If set to true, the VAO will automatically update upon render for every TileRenderable.
    /// </summary>
    private static bool _prevFrameGlobalUpdatedSinceLastFrame;
    
    // geometry arrays
    private readonly float[] _vertices =
    [
         1f,  1f, 0.0f, // top right
         1f, -1f, 0.0f, // bottom right
        -1f, -1f, 0.0f, // bottom left
        -1f,  1f, 0.0f  // top left
    ];
    private readonly uint[] _indices =
    [
        0, 1, 3,   // first triangle
        1, 2, 3    // second triangle
    ];
    
    
    public PathTracedRenderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw) : base(shader, hint)
    {
        // update the vao (creates it, in this case)
        UpdateVao(hint);
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(int), 0);

        int loc;
        
        // set up uniforms
        loc = GL.GetUniformLocation(Shader.Handle, "scale");
        GL.Uniform1(loc, 1, ref _scale);
        
        loc = GL.GetUniformLocation(Shader.Handle, "translation");
        GL.Uniform2(loc, ref _translation);
        
        Vector2 screenSize = Program.Get().ClientSize;
        loc = GL.GetUniformLocation(Shader.Handle, "screenSize");
        GL.Uniform2(loc, ref screenSize);
        
        _dynTilemap = Textures.DynTilemap;
        _dynTilemap.Use(TextureUnit.Texture0);
    }

    public override void Render(RenderableCategory category = RenderableCategory.All)
    {
        
        if (!IsInCategory(category) || !ShouldRender)
            return;

        if (_prevFrameGlobalUpdatedSinceLastFrame)
            _globalUpdatedSinceLastFrame = false;
        
        
        // update the VAO if necessary
        if (_globalUpdatedSinceLastFrame)
            UpdateVao();
        
        base.Render(category);
        
        // bind vao / ssbo
        GL.BindVertexArray(VertexArrayObject);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, ShaderStorageBufferObject);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ShaderStorageBufferObject);
        
        _dynTilemap.Use(TextureUnit.Texture0);
        Shader.Use();
        
        // set the uniforms
        Shader.SetFloat("scale", _scale);
        Shader.SetVector2("translation", _translation);
        Shader.SetVector2("screenSize", Program.Get().ClientSize);

        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        
        var error = GL.GetError();
        
        if (error != ErrorCode.NoError)
        {
            Util.Error($"OpenGL Error {error.ToString()}");
        }


        _prevFrameGlobalUpdatedSinceLastFrame = _globalUpdatedSinceLastFrame;
    }

    public sealed override void UpdateVao(BufferUsageHint? hint = null, RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category))
            return;

        base.UpdateVao(hint, category);
        
        // bind vao
        GL.BindVertexArray(VertexArrayObject);
        
        // bind/update vbo
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(int), _vertices, Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, Hint);
        
        // bind/update ssbo
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ShaderStorageBufferObject);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, _geometry.Length * Marshal.SizeOf(new QuadTreeStruct()), _geometry, Hint);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, ShaderStorageBufferObject);
    }
    
    /// <summary>
    /// Sets the new geometry and updates the VAO
    /// </summary>
    /// <param name="geometry"></param>
    public void SetGeometry(QuadTreeStruct[] geometry)
    {
        var newGeometry = new QuadTreeStruct[geometry.Length+1];
        
        geometry.CopyTo(newGeometry, 0);

        newGeometry[^1] = new QuadTreeStruct(0, Constants.RenderDepth + 1, 0);
        
        _geometry = newGeometry;
        
        UpdateVao();
        
        ShouldRender = true;
    }

    public void SetTransform(Vec2<decimal> translation, float scale)
    {
        _translation = translation;
        _scale = scale;

        _globalUpdatedSinceLastFrame = true;
    }
    
    public override void ResetGeometry(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category))
            return;
        
        base.ResetGeometry(category);
        
        _geometry = [];
    }
    
    protected override bool IsInCategory(RenderableCategory category)
    {
        return base.IsInCategory(category) || category == RenderableCategory.Pt;
    }


}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct QuadTreeStruct
{
    public uint code;
    public uint depth;
    
    public uint id;
    
    
    public QuadTreeStruct(uint code, uint depth, uint id)
    {
        this.code = code;
        this.depth = depth;
        this.id = id;
    }
    
}
