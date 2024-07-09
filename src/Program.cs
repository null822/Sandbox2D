using System;
using System.Threading;
using OpenTK.Windowing.Common.Input;
using Sandbox2D.Maths;
using Sandbox2D.Maths.Quadtree;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = OpenTK.Windowing.Common.Input.Image;
using static Sandbox2D.Util;

namespace Sandbox2D;

public static class Program
{
    private static readonly RenderManager RenderManager = new (Constants.InitialScreenSize.X, Constants.InitialScreenSize.Y, "Sandbox2D");
    
    private static void Main(string[] args)
    {
        Console.Clear();
        
        var image = (Image<Rgba32>)SixLabors.ImageSharp.Image.Load("assets/icon.png");
        
        var pixels = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        
        var icon = new WindowIcon(new Image(1024, 1024, pixels));
        
        RenderManager.Icon = icon;
        RenderManager.VSync = Constants.Vsync;
        
        if (args.Length == 1)
        {
            RenderManager.SetWorldAction(WorldAction.Load, args[0]);
        }
        
        // start up game logic
        var gameLogicThread = new Thread(GameManager.Run)
        {
            Name = "Logic Thread",
            IsBackground = true,
        };
        gameLogicThread.Start();
        
        Thread.Sleep(100);
        
        // start the RenderManager
        RenderManager.Run();
        
    }
    
    /// <summary>
    /// Makes sure everything works.
    /// </summary>
    public static void SystemChecks()
    {
        var setRemoveHash = new DynamicArray<int>([12, 32, 42]).Hash();
        var clearHash = new DynamicArray<int>([]).Hash();
        var sortHash = new DynamicArray<int>([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]).Hash();

        const int batchLength = 2;
        
        bool success;
        
        Log("log text", OutputSource.Test);
        Debug("debug text", OutputSource.Test);
        Warn("warn text", OutputSource.Test);
        Error("error text", OutputSource.Test);

        const int mh = 64;
        
        var a1 = new Vec2<long>(69420, 123456);
        var b1 = QuadtreeUtil.Interleave(a1, mh);
        var c1 = QuadtreeUtil.Deinterleave(b1, mh);
        
        var a2 = new Vec2<long>(420, 123);
        var b2 = QuadtreeUtil.Interleave(a2, mh);
        
        Assert(a1, c1, "Interleave and re- Deinterleave");
        Assert(b1 > b2, true, "Interleaved Size Relation");
        
        var v1 = new Vec2<int>(13,   2);
        var v2 = new Vec2<int>(13, -64);
        Assert(v1.Quadrant(v2), -1, "Vec2 Quadrant");
        
        var r1 = new Range2D(-2, -2, 2, 4);
        var r2 = new Range2D(0, 1, 3, 3);
        Assert(r1.Overlap(r2), new Range2D(0, 1, 2, 3), "Range2D Overlap");
        
        var r3 = new Range2D(-2, -2, 4, 4);
        var r4 = new Range2D(5, 5, 8, 8);
        Assert(r3.Overlaps(r4), false, "Range2D Overlaps");
        
        var r5 = new Range2D(198, 255, 255, 295);
        var r6 = new Range2D(198, 266, 198, 266);
        Assert(r5.Contains(r6), true, "Range2D Contains");
        
        var dynamicArray = new DynamicArray<int>(batchLength, true);
        dynamicArray.Add(12);
        dynamicArray.Add(69);
        dynamicArray.Add(42);
        dynamicArray.Remove(1);
        Assert(dynamicArray.Add(32), 1, "DynamicArray Vacancy Fill");
        Assert(dynamicArray.Hash(), setRemoveHash, "DynamicArray Set/Remove");
        
        dynamicArray.Clear();
        Assert(dynamicArray.Hash(), clearHash, "DynamicArray Clear");
        
        dynamicArray.Add(0); // 0
        dynamicArray.Add(1); // 1
        dynamicArray.Add(2); // 2
        dynamicArray.Add(3); // 3
        dynamicArray.Add(4); // 4
        dynamicArray.Add(5); // 5
        dynamicArray.Add(6); // 6
        dynamicArray.Add(7); // 7
        dynamicArray[4] = 10;
        dynamicArray[7] = 11;
        dynamicArray.Remove(5);
        dynamicArray.Add(12);
        var dynamicArrayMirror = new DynamicArray<int>(batchLength);
        var modifications = new ArrayModification<int>[batchLength];
        dynamicArrayMirror.EnsureCapacity(dynamicArray.Length);
        while (modifications.Length != 0)
        {
            modifications = dynamicArray.GetModifications();
            foreach (var m in modifications)
            {
                dynamicArrayMirror[m.Index] = m.Value;
            }
        }
        success = Assert(dynamicArray.Hash(), dynamicArrayMirror.Hash(), "DynamicArray Modifications");
        if (!success)
        {
            Error($"Original ({dynamicArray.Hash()})");
            for (var i = 0; i < dynamicArray.Length; i++)
            {
                Warn($"[{i}] = {dynamicArray[i]}");
            }
            
            Error($"Mirror ({dynamicArrayMirror.Hash()})");
            for (var i = 0; i < dynamicArrayMirror.Length; i++)
            {
                Warn($"[{i}] = {dynamicArrayMirror[i]}");
            }
        }
        
        dynamicArray.Dispose();
        dynamicArray = new DynamicArray<int>(batchLength, true);

        dynamicArray.Add(9);
        dynamicArray.Add(0);
        dynamicArray.Add(1);
        dynamicArray.Add(2);
        dynamicArray.Add(5);
        dynamicArray.Add(6);
        dynamicArray.Add(3);
        dynamicArray.Add(4);
        dynamicArray.Add(7);
        dynamicArray.Add(8);
        
        dynamicArray.Sort();
        success = Assert(dynamicArray.Hash(), sortHash, "DynamicArray Sort");
        if (!success)
        {
            for (var i = 0; i < dynamicArray.Length; i++)
            {
                Warn($"[{i}] = {dynamicArray[i]}");
            }
        }
        
        dynamicArray.Dispose();
        dynamicArrayMirror.Dispose();
    }
    
    /// <summary>
    /// Gets and returns the current <see cref="RenderManager"/>.
    /// </summary>
    public static RenderManager Get()
    {
        return RenderManager;
    }
    
}