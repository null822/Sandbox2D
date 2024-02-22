#nullable enable
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;
using Sandbox2D.Maths;

namespace Sandbox2D.Graphics.Renderables;

public class GuiRenderable : Renderable
{
    // geometry arrays
    protected readonly List<float> Vertices = [];
    protected readonly List<uint> Indices = [];
    
    
    public GuiRenderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw) : base(shader, hint)
    {
        // update the vao (creates it, in this case)
        UpdateVao(hint);
        
        // set up vertex coords
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    }
    
    public override void Render(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category) || !ShouldRender)
            return;

        base.Render(category);
        
        GL.BindVertexArray(VertexArrayObject);
        Shader.Use();
        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
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
        GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Count * sizeof(float), Vertices.ToArray(), Hint);
        
        // bind/update ebo (must be done after vbo)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Count * sizeof(uint), Indices.ToArray(), Hint);
    }
    
    /// <summary>
    /// Adds a quad to the geometry. Does not update the VAO
    /// </summary>
    /// <param name="tl">the top left vertex</param>
    /// <param name="br">the bottom right vertex</param>
    /// <remarks>the coordinates are a modified version of screen coords, where (0, 0) is in the center of the screen</remarks>
    public void AddQuad(Vec2<int> tl, Vec2<int> br)
    {
        // get the next available index in _vertices
        var indexOffset = (uint)(Vertices.Count / 3f);

        var center = GameManager.ScreenSize / 2;

        var vertexTl = Util.ScreenToVertexCoords((Vec2<int>)((Vec2<float>)tl * Constants.GuiScale) + center);
        var vertexBr = Util.ScreenToVertexCoords((Vec2<int>)((Vec2<float>)br * Constants.GuiScale) + center);
        
        // add the new vertices
        Vertices.AddRange(new []
        {
            vertexBr.X, vertexTl.Y, 0.0f, // right top
            vertexBr.X, vertexBr.Y, 0.0f, // right bottom
            vertexTl.X, vertexBr.Y, 0.0f, // left  bottom
            vertexTl.X, vertexTl.Y, 0.0f  // left  top
        });
        
        // create and add the new indices
        Indices.AddRange(new []
        {
            indexOffset + 0, indexOffset + 1, indexOffset + 3,   // first triangle
            indexOffset + 1, indexOffset + 2, indexOffset + 3    // second triangle
        });
        
        GeometryAdded();
    }

    /// <summary>
    /// Resets the geometry of this renderable. Does not update the VAO
    /// </summary>
    public override void ResetGeometry(RenderableCategory category = RenderableCategory.All)
    {
        if (!IsInCategory(category))
            return;

        base.ResetGeometry(category);
        
        Vertices.Clear();
        Indices.Clear();
    }

    protected override bool IsInCategory(RenderableCategory category)
    {
        return base.IsInCategory(category) || category == RenderableCategory.Gui;
    }
}