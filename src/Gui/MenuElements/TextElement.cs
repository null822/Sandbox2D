using System;
using System.Text;
using ElectroSim.Content;
using ElectroSim.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Sandbox2D.Maths;

namespace Sandbox2D.Gui.MenuElements;

public class TextElement : MenuElement
{
    private readonly string _text;
    private readonly ScalableValue _fontSize;

    public TextElement(ScalableValue2 pos, ScalableValue2 size, string text, ScalableValue fontSize, Action click = null)
        : base(pos, size, click)
    {
        _text = text;
        _fontSize = fontSize;
    }

    protected override void RenderContents(SpriteBatch spriteBatch, Vector2 pos, Vector2 size)
    {
        var fontSize = (float)_fontSize.Get();


        var maxWidth = Math.Max(size.X - Constants.MenuBackgroundZoom, 10);

        var font = FontRegistry.GetFont("consolas");

        var wrappedText = WrapLines(_text, maxWidth, font, fontSize);

        spriteBatch.DrawString(
            font,
            wrappedText,
            pos + new Vector2(Constants.MenuElementPadding),
            Color.White,
            0,
            new Vector2(0),
            fontSize,
            SpriteEffects.None,
            1);

    }
    
    
    private static string WrapLines(string originalText, float maxWidth, BitmapFont font, float scale)
    {
        
        var newText = new StringBuilder();

        var lines = originalText.Split("\n");

        foreach (var line in lines)
        {
            var lineLength = GetLineLength(line, font, scale);

            if (lineLength < maxWidth)
            {
                newText.Append(line + "\n");
                continue;
            }

            var workingLine = line;
            
            while (lineLength > maxWidth)
            {
                var ratio = maxWidth / lineLength;

                var splitIndex = (int)(workingLine.Length * ratio);
                
                var lastSpace = workingLine[..splitIndex].LastIndexOf(' ');

                splitIndex = lastSpace != -1 ? lastSpace : splitIndex;

                newText.Append(workingLine[..splitIndex] + "\n");
                workingLine = workingLine[(lastSpace == -1 ? splitIndex : splitIndex+1)..];
                    
                lineLength = GetLineLength(workingLine, font, scale);
            }
            newText.Append(workingLine + "\n");
        }
        
        
        var newTextString = newText.ToString();
        newText.Clear();
        

        return newTextString;
    }
    
    private static double GetLineLength(string line, BitmapFont font, float scale)
    {
        return font.MeasureString(line).Width * scale;
    }

}