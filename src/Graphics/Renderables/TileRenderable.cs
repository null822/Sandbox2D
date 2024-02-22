using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Maths;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace Sandbox2D.Graphics.Renderables;

public class TileRenderable : Renderable
{
    private static Vector2 _translation = Vector2.Zero;
    private static float _scale = 1;

    private static Vec2<long> _vertexOffset = new (0);
    private static float _renderScale = 1;

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
    private readonly List<int> _vertices = [];
    private readonly List<uint> _indices = [];
    
    public TileRenderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw) : base(shader, hint)
    {
        // update the vao (creates it, in this case)
        UpdateVao(hint);
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("worldPos");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribIPointer(vertexLocation, 2, VertexAttribIntegerType.Int, 2 * sizeof(int), 0);
        
        // set up uniforms
        var scaleLocation = GL.GetUniformLocation(Shader.Handle, "scale");
        GL.Uniform1(scaleLocation, 1, ref _scale);
        
        var renderScaleLocation = GL.GetUniformLocation(Shader.Handle, "renderScale");
        GL.Uniform1(renderScaleLocation, 1, ref _renderScale);
        
        var translationLocation = GL.GetUniformLocation(Shader.Handle, "translation");
        GL.Uniform2(translationLocation, ref _translation);
        
        Vector2 screenSize = Program.Get().ClientSize;
        var screenSizeLocation = GL.GetUniformLocation(Shader.Handle, "screenSize");
        GL.Uniform2(screenSizeLocation, ref screenSize);
        
    }

    public override void Render(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category) || !ShouldRender)
            return;

        if (_prevFrameGlobalUpdatedSinceLastFrame)
            _globalUpdatedSinceLastFrame = false;
        
        
        // update the VAO of all TileRenderables if necessary
        if (_globalUpdatedSinceLastFrame)
            UpdateVao();
        
        base.Render(category);
        
        GL.BindVertexArray(VertexArrayObject);
        Shader.Use();
        
        // set the uniforms
        Shader.SetFloat("scale", _scale);
        Shader.SetFloat("renderScale", _renderScale);
        Shader.SetVector2("translation", _translation);
        Shader.SetVector2("screenSize", Program.Get().ClientSize);
        
        // Util.Log($"{Indices.Count / 3f} Triangles");
        
        GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);
        
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
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(int), _vertices.ToArray(), Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Count * sizeof(uint), _indices.ToArray(), Hint);
    }

    /// <summary>
    /// Adds a quad to the geometry. Does not update the VAO
    /// </summary>
    /// <param name="tl">the top left vertex</param>
    /// <param name="br">the bottom right vertex</param>
    public void AddQuad(Vec2<long> tl, Vec2<long> br)
    {
        var tlInt = (Vec2<int>)((Vec2<decimal>)(tl + _vertexOffset) / (decimal)_renderScale);
        var brInt = (Vec2<int>)((Vec2<decimal>)(br + _vertexOffset) / (decimal)_renderScale);
        
        // get the next available index in Vertices
        var indexOffset = (uint)(_vertices.Count / 2f);
        
        // add the new vertices
        _vertices.AddRange(new []
        {
            brInt.X, tlInt.Y, // right top
            brInt.X, brInt.Y, // right bottom
            tlInt.X, brInt.Y, // left  bottom
            tlInt.X, tlInt.Y  // left  top
        });
        
        // create and add the new indices
        _indices.AddRange(new []
        {
            indexOffset + 0, indexOffset + 1, indexOffset + 3,   // first triangle
            indexOffset + 1, indexOffset + 2, indexOffset + 3    // second triangle
        });
        
        GeometryAdded();
    }

    public static void SetTransform(Vec2<decimal> translation, float scale)
    {
        var minCorner = Util.ScreenToWorldCoords(new Vec2<int>(0));
        var maxCorner = Util.ScreenToWorldCoords(GameManager.ScreenSize);
        
        var regionSize = maxCorner - minCorner;

        var regionSizeX = regionSize.X;
        var regionSizeY = regionSize.Y;
        
        if (minCorner.X == long.MinValue && maxCorner.X == long.MaxValue)
        {
            regionSizeX = long.MaxValue;
        }
        if (minCorner.Y == long.MinValue && maxCorner.Y == long.MaxValue)
        {
            regionSizeY = long.MaxValue;
        }
        
        regionSize = new Vec2<long>(
            Math.Max(regionSizeX, int.MaxValue / 16),
            Math.Max(regionSizeY, int.MaxValue / 16));
        
        
        decimal renderScaleX = 1;
        decimal renderScaleY = 1;
        
        if (regionSize.X is > int.MaxValue or < int.MinValue)
        {
            renderScaleX = regionSize.X / (decimal)int.MaxValue;
        }
        if (regionSize.Y is > int.MaxValue or < int.MinValue)
        {
            renderScaleY = regionSize.Y / (decimal)int.MaxValue;
        }
        
        _renderScale = (float)Math.Max(renderScaleX, renderScaleY);
        
        // Util.Log($"rs: {_renderScale}");
        
        _scale = scale;
        
        
        var renderOffsetX = (long)Math.Round(translation.X / regionSize.X) * regionSize.X;
        var renderOffsetY = (long)Math.Round(translation.Y / regionSize.Y) * regionSize.Y;
        _vertexOffset = new Vec2<long>(renderOffsetX, renderOffsetY);

        // calculate the new translation to counteract the offset to the vertices
        _translation = translation - (Vec2<decimal>)_vertexOffset;

        _globalUpdatedSinceLastFrame = true;
    }
    
    public override void ResetGeometry(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category))
            return;
        
        base.ResetGeometry(category);
        
        _vertices.Clear();
        _indices.Clear();
    }

    protected override bool IsInCategory(RenderableCategory category)
    {
        return base.IsInCategory(category) || category == RenderableCategory.Tile;
    }


}
