﻿using System;
using System.Threading;
using Math2D;
using Math2D.Quadtree;
using OpenTK.Windowing.Common.Input;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = OpenTK.Windowing.Common.Input.Image;
using static Sandbox2D.Util;

namespace Sandbox2D;

public static class Program
{
    /// <summary>
    /// The entrypoint
    /// </summary>
    /// <param name="args">Runtime arguments:
    /// <code>
    /// [0] = path to .qdt save file to load on start
    /// </code></param>
    private static int Main(string[] args)
    {
        var fCol = Console.ForegroundColor;
        var bCol = Console.BackgroundColor;
        
        try
        {
            Console.Clear();
            
            GlobalVariables.RenderManager.Icon = LoadIcon($"{GlobalVariables.AssetDirectory}/icon.png", (64, 64));
            GlobalVariables.RenderManager.VSync = Constants.Vsync;
            
            if (args.Length == 1)
            {
                RenderManager.SetWorldAction(WorldAction.Load, args[0]);
            }

            // run the system checks
            Log("===============[ SYSTEM CHECKS  ]===============", "Load");
            SystemChecks();

            Log("===============[    STARTUP     ]===============", "Load");

            // start the logic thread
            var gameLogicThread = new Thread(GameManager.Run)
            {
                Name = "Logic Thread",
                IsBackground = true
            };
            gameLogicThread.Start();

            // start the "join" the render thread
            GlobalVariables.RenderManager.Run();

            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            
            Console.ForegroundColor = fCol;
            Console.BackgroundColor = bCol;
        }
        
        return -1;
    }
    
    /// <summary>
    /// Loads an image, resizes it, and stores it in a <see cref="WindowIcon"/>.
    /// </summary>
    /// <param name="path">the path to the image to load</param>
    /// <param name="size">the resolution to resize to</param>
    /// <returns>The resized <see cref="WindowIcon"/></returns>
    private static WindowIcon LoadIcon(string path, Vec2<int> size)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
        
        image.Mutate(x => x.Resize(size.X, size.Y, KnownResamplers.Lanczos3));
        
        var pixels = new byte[4 * image.Width * image.Height];
        image.CopyPixelDataTo(pixels);
        
        return new WindowIcon(new Image(size.X, size.Y, pixels));
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
        
        Log("log text", "Test");
        Debug("debug text", "Test");
        Warn("warn text", "Test");
        Error("error text", "Test");
        
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
        
        for (var i = 0; i < 128; i++)
        {
            dynamicArray.Add(13);
        }
        
        dynamicArray.Remove(1);
        Assert(dynamicArray.Add(32), 1L, "DynamicArray Vacancy Fill");
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
        var modifications = new DynamicArray<ArrayModification<int>>(batchLength, false, false);
        dynamicArrayMirror.EnsureCapacity(dynamicArray.Length);
        
        dynamicArray.GetModifications(modifications);
        for (var i = 0; i < modifications.Length; i++)
        {
            var m = modifications[i];
            dynamicArrayMirror[m.Index] = m.Value;
        }
        
        success = Assert(dynamicArray.Hash(), dynamicArrayMirror.Hash(), "DynamicArray Modifications");
        if (!success)
        {
            Error($"         Original ({dynamicArray.Hash()})");
            for (var i = 0; i < dynamicArray.Length; i++)
            {
                Warn($"         [{i}] = {dynamicArray[i]}");
            }
            
            Error($"         Mirror ({dynamicArrayMirror.Hash()})");
            for (var i = 0; i < dynamicArrayMirror.Length; i++)
            {
                Warn($"         [{i}] = {dynamicArrayMirror[i]}");
            }
        }
        dynamicArrayMirror.Dispose();

        dynamicArray.RemoveEnd(5);
        Assert(dynamicArray.Length, 5L, "DynamicArray RemoveEnd");
        dynamicArray.Clear();
        Assert(dynamicArray.Length, 0L, "DynamicArray Shrink");
        
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
    }
    
}
