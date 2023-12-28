using System;
using ElectroSim.Maths;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox2D.Maths;

namespace ElectroSim.Gui;

public class MenuElement
{
    private readonly ScalableValue2 _pos;
    private readonly ScalableValue2 _size;
    protected bool Hover;
    protected readonly Action ClickAction;
    
    protected MenuElement(ScalableValue2 pos, ScalableValue2 size, Action clickAction = null)
    {
        _pos = pos;
        _size = size;
        ClickAction = clickAction;
        
        Hover = false;
    }

    public void Render(SpriteBatch spriteBatch, Vector2 pos)
    {
        RenderContents(spriteBatch, pos + _pos, _size);
    }
    
    public bool Click()
    {
        if (!Hover || ClickAction == null)
            return false;

        ClickAction.Invoke();
        return true;
    }
    
    
    public bool CheckHover(Vector2 mousePos)
    {
        var pos = _pos.Get();
        var size = _size.Get();

        var collision = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
        
        Hover = collision.Contains(mousePos);
        return Hover;
    }

    protected virtual void RenderContents(SpriteBatch spriteBatch, Vector2 pos, Vector2 size)
    {
    }

    public ScalableValue2 GetPos()
    {
        return _pos;
    }
    
    public ScalableValue2 GetSize()
    {
        return _size;
    }
    
}