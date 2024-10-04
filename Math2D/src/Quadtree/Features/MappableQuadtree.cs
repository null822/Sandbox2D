using System.Text;
using Math2D.Quadtree.FeatureNodeTypes;
using static Math2D.Quadtree.NodeType;
using static Math2D.Quadtree.QuadtreeUtil;
using static Math2D.BitUtil;

namespace Math2D.Quadtree.Features;

public class MappableQuadtree<T> : QuadtreeFeature<T> where T : IQuadtreeElement<T>, IFeatureElementColor
{
    public override Quadtree<T> Base { get; }
    
    private readonly DynamicArray<QuadtreeNode> _tree;
    private readonly DynamicArray<T> _data;
    
    public MappableQuadtree(Quadtree<T> quadtree)
    {
        Base = quadtree;
        
        _tree = Tree;
        _data = Data;
    }
    
    
    /// <summary>
    /// Converts this <see cref="Quadtree{T}"/> into an SVG.
    /// </summary>
    /// <param name="scale">the scale of the SVG</param>
    /// <returns>A string containing the SVG</returns>
    public string GetSvgMap(decimal scale)
    {
        // create the viewbox, which is 2x as large as the actual svg
        var minX =   Dimensions.MinX * scale * 2m;
        var maxY = -(Dimensions.MinY * scale * 2m);
        var maxX =   Dimensions.MaxX * scale * 2m;
        var minY = -(Dimensions.MaxY * scale * 2m);
        var w = maxX - minX + 1;
        var h = maxY - minY + 1;
        
        var svgString = new StringBuilder(
            $"<svg viewBox=\"{minX} {minY} {w} {h}\">"
        );
        
        var maxZValue = Pow2Min1U128(MaxHeight * 2);
        UInt128 zValue = 0;
        var pos = Dimensions.Bl;
        var height = MaxHeight;
        var nodeRef = 0L;
        var path = new long[MaxHeight];
        
        while (true)
        {
            var node = _tree[nodeRef];
            
            // if the node is a branch node, step down into it without changing the z-value, and continue
            if (node.Type == Branch)
            {
                nodeRef = node.Ref0;
                height--;
                // update the path
                path[height] = nodeRef;
                
                continue;
            }
            
            // if the node is a leaf node, add it to the svg
            svgString.Append(GetNodeSvgRect(pos, height, scale, nodeRef));
            
            // jump to the next node
            pos = GetNextNodePos(pos, zValue, height);
            (zValue, height, nodeRef) = GetNextNode(zValue, height, maxZValue, ref path);
            if (nodeRef == -1) break; // exit if we went through the entire quadtree
        }
        
        svgString.Append("<svg/>");
        
        var str = svgString.ToString();
        svgString.Clear();
        return str;
    }
    
    /// <summary>
    /// Converts a node (specified by z-value and height) into an SVG rect element
    /// </summary>
    /// <param name="pos">the position of the bottom left coordinate of the node</param>
    /// <param name="height">the height of the node</param>
    /// <param name="scale">the scale of the SVG</param>
    /// <param name="nodeRef">[optional] a reference within <see cref="_tree"/> to the node</param>
    /// <returns>a string containing the SVG rect</returns>
    private string GetNodeSvgRect(Vec2<long> pos, int height, decimal scale, long nodeRef)
    {
        var value = _data[_tree[nodeRef].GetValueRef()];
        var fillColor = value.GetColor();
        
        var r = NodeRangeFromPos(pos, height);
        var minX =  r.MinX * scale;
        var minY = -r.MaxY * scale;
        var maxX = ( r.MaxX - 0) * scale;
        var maxY = (-r.MinY - 0) * scale;
        var w = maxX - minX + 1 * scale;
        var h = maxY - minY + 1 * scale;
        
        return $"<rect style=\"" +
               $"fill:{fillColor};fill-opacity:{1.0};" +
               $"stroke:{(fillColor.Grayscale > 0 ? Color.Black : Color.White)};stroke-width:{Math.Min(w / 64m, 1 * scale)}\" " +
               $"width=\"{w}\" height=\"{h}\" " +
               $"x=\"{minX}\" y=\"{minY}\"/>";
    }
    
    
    
}