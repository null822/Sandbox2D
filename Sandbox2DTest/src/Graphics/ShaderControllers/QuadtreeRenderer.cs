using Math2D;
using Math2D.Quadtree;
using OpenTK.Graphics.OpenGL4;
using Sandbox2D;
using Sandbox2D.Graphics;
using Sandbox2D.Graphics.ShaderControllers;
using Sandbox2DTest.World;
using Vector2 = OpenTK.Mathematics.Vector2;

namespace Sandbox2DTest.Graphics.ShaderControllers;

public class QuadtreeRenderer : ShaderController, IDisposable
{
    private readonly MainRenderManager _renderManager;
    
    private readonly int _vao = GL.GenVertexArray();
    private readonly int _vbo = GL.GenBuffer();
    private readonly int _ebo = GL.GenBuffer();
    
    private Vec2<long> _translation = (0, 0);
    private Vector2 _subTranslation = Vector2.Zero;
    private double _scale = 1;
    
    private int _gpuMaxHeight;
    
    /// <summary>
    /// The buffer that contains the <see cref="Quadtree{T}.Tree"/> array of the <see cref="Quadtree{T}"/> to be rendered
    /// </summary>
    private readonly DynamicBuffer _tree;
    
    /// <summary>
    /// The buffer that contains the <see cref="Quadtree{T}.Data"/> array of the <see cref="Quadtree{T}"/> to be rendered
    /// </summary>
    private readonly DynamicBuffer _data;
    
    
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
    
    public QuadtreeRenderer(int gpuGpuMaxHeight, MainRenderManager renderManager, ShaderProgram shader, BufferUsageHint hint = BufferUsageHint.StaticDraw)
        : base(shader, hint)
    {
        _renderManager = renderManager;
        
        if (gpuGpuMaxHeight > _renderManager.MaxGpuQtHeight) throw new Exception($"Invalid Max Height for Quadtree. Was: {gpuGpuMaxHeight}, Range: 2-{_renderManager.MaxGpuQtHeight}.");
        _gpuMaxHeight = gpuGpuMaxHeight;
        
        _tree = new DynamicBuffer(hint);
        _data = new DynamicBuffer(hint);
        
        // update the vao (creates it, in this case)
        UpdateVao();
        
        // set up vertex coordinates
        var vertexLocation = Shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(int), 0);
    }

    public override void Invoke()
    {
        // bind vao / ssbos
        GL.BindVertexArray(_vao);
        _tree.Bind(BufferRangeTarget.ShaderStorageBuffer, 0);
        _data.Bind(BufferRangeTarget.ShaderStorageBuffer, 1);
        
        // use the shader
        Shader.Use();
        
        // set the uniforms
        if (_renderManager.Using64BitQt) Shader.Set("Translation", _translation);
        else Shader.Set("Translation", (Vec2<int>)_translation);
        Shader.Set("SubTranslation", _subTranslation);
        Shader.Set("Scale", _scale);
        Shader.Set("ScreenSize", (Vec2<float>)_renderManager.ScreenSize);
        Shader.Set("MaxHeight", _gpuMaxHeight);
        
        // render the geometry
        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }

    private void UpdateVao()
    {
        // bind VAO
        GL.BindVertexArray(_vao);
        
        // bind/update VBO
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(int), _vertices, Hint);
        
        // bind/update EBO (must be done after VBO)
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, Hint);
    }

    /// <summary>
    /// Updates the geometry, modifying the existing buffers
    /// </summary>
    /// <param name="tree">the modifications to the tree section to upload</param>
    /// <param name="data">the modifications to the tree section to upload</param>
    /// <param name="treeLength">the total length of the tree section</param>
    /// <param name="dataLength">the total length of the data section</param>
    /// <param name="treeIndex">index within the tree modifications to start uploading at</param>
    /// <param name="dataIndex">index within the data modifications to start uploading at</param>
    /// <param name="renderRoot">the root node for rendering</param>
    public (int TreeIndex, int DataIndex) SetGeometry(
        DynamicArray<ArrayModification<QuadtreeNode>> tree, DynamicArray<ArrayModification<Tile>> data,
        long treeLength, long dataLength,
        int treeIndex, int dataIndex,
        QuadtreeNode renderRoot)
    {
        _tree.Resize((int)treeLength * QuadtreeNode.SerializeLength);
        var newTreeIndex = _tree.Modify(tree, treeIndex);
        _tree.Set(renderRoot, 0);
        
        _data.Resize((int)dataLength * Tile.SerializeLength);
        var newDataIndex = _data.Modify(data, dataIndex);
        
        return (newTreeIndex, newDataIndex);
    }
    
    public void SetTransform(Vec2<decimal> translation, double scale)
    {
        long min;
        long max;
        
        if (_renderManager.Using64BitQt)
        {
            min = long.MinValue;
            max = long.MaxValue;
        }
        else
        {
            min = int.MinValue;
            max = int.MaxValue;
        }
        
        _translation = new Vec2<long>(
            (long)Math.Clamp(translation.X, min, max),
            (long)Math.Clamp(translation.Y, min, max));
        
        _subTranslation = ((Vec2<float>)(translation - (Vec2<decimal>)_translation)).ToVector2();
        _scale = scale;
    }
    
    public override void ResetGeometry()
    {
        _tree.ResetGeometry();
        _data.ResetGeometry();
    }

    public void SetMaxHeight(int maxHeight)
    {
        _gpuMaxHeight = maxHeight;
    }
    
    public void Dispose()
    {
        Shader.Dispose();
        
        _tree.Dispose();
        _data.Dispose();
        GL.DeleteBuffer(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
    }
}
