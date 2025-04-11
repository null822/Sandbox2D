using System;
using System.Xml;
using Math2D;
using Sandbox2D.Registry_;
using Sandbox2D.UserInterface.Elements;

namespace Sandbox2D.UserInterface;

public class Gui
{
    private readonly BodyElement _body;
    
    public Gui(XmlDocument guiFile)
    {
        var bodies = guiFile.GetElementsByTagName("body");
        
        while (true)
        {
            if (bodies.Count == 0) break;
            var body = bodies[0];
            
            if (body == null) break;
            _body = Registry.GuiElement.Create("body", body) as BodyElement;
            
            if (_body == null) break;
            return;
        }
        throw new Exception("GUI source does not contain a valid <body> tag");
    }
    
    public Gui(string xmlPath) : this(LoadXml(xmlPath)) { }
    
    private static XmlDocument LoadXml(string path)
    {
        var guiXml = new XmlDocument();
        guiXml.Load(path);
        return guiXml;
    }

    /// <summary>
    /// Renders the <see cref="Gui"/> to the screen.
    /// </summary>
    public void Render() => _body.Render(new Vec2<int>());

    /// <summary>
    /// Update's the <see cref="Gui"/>'s logic
    /// </summary>
    public void Update() => _body.Update();
}
