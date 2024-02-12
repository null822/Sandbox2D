#nullable enable
using OpenTK.Graphics.OpenGL4;
using Sandbox2D.Graphics.Registry;

namespace Sandbox2D.Graphics.Renderables.GuiRenderables;

public class GuiCheckboxRenderable : GuiElementRenderable
{
    
    public GuiCheckboxRenderable(Shader shader, BufferUsageHint hint = BufferUsageHint.StaticDraw) : base(shader, hint)
    {
        
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

    protected override bool IsInCategory(RenderableCategory category)
    {
        return base.IsInCategory(category) || category == RenderableCategory.Checkbox;
    }
    
    
    
}