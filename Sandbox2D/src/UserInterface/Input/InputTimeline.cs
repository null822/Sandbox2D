using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Math2D;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox2D.UserInterface.Input;

public class InputTimeline : IEnumerable<Input>
{
    /// <summary>
    /// Whether to write to buffer A or B (A = false)
    /// </summary>
    private bool _buffer;
    
    private readonly List<Input> _actionsA = [];
    private readonly List<Input> _actionsB = [];
    
    private readonly InputState[] _state = new InputState[(int)Key.LastKeyboardKey + 1];
    
    private List<Input> ReadingBuffer => _buffer ? _actionsA : _actionsB;
    private List<Input> WritingBuffer => _buffer ? _actionsB : _actionsA;
    
    public InputState this [Key key] => _state[(int)key];
    
    public bool IsPressed(Key key) => this[key].Type is 
        InputStateType.Pressed or InputStateType.RisingEdge or InputStateType.Repeat;
    public bool IsReleased(Key key) => this[key].Type is 
        InputStateType.Released or InputStateType.FallingEdge;
    
    public Vec2<float> MousePos { get; private set; }
    public Vec2<float> MouseScroll { get; private set; }
    public Vec2<float> MouseScrollDelta { get; private set; }
    
    /// <summary>
    /// Swaps which buffer is the writing buffer, clears the new writing buffer, and updates the
    /// <see cref="_state"/> using the values in the new reading buffer.
    /// </summary>
    public void SwapBuffers()
    {
        _buffer = !_buffer;
        WritingBuffer.Clear();

        for (var i = 0; i < _state.Length; i++)
        {
            _state[i].Repetitions = 0;

            if (_state[i].Type == InputStateType.RisingEdge)
                _state[i].Type = InputStateType.Pressed;
            if (_state[i].Type == InputStateType.Repeat)
                _state[i].Type = InputStateType.Pressed;
            if (_state[i].Type == InputStateType.FallingEdge)
                _state[i].Type = InputStateType.Released;
        }
        MouseScrollDelta = (0, 0);

        foreach (var input in ReadingBuffer)
        {
            var key = (int)input.Key;

            switch (input.Type)
            {
                case InputType.Key:
                    _state[key].Type = (InputStateType)input.Action;
                    _state[key].Repetitions++;
                    break;
                case InputType.MousePos:
                    MousePos = new Vec2<float>(input.X, input.Y);
                    break;
                case InputType.MouseScroll:
                    MouseScrollDelta = new Vec2<float>(input.X, input.Y);
                    break;
            }
        }
        MouseScroll += MouseScrollDelta;
    }
    
    public void AddKeyboardKey(Keys key, int scancode, InputAction action, KeyModifiers modifiers) =>
        AddKey((Key)key, scancode, action, modifiers);
    public void AddMouseButton(MouseButton key, InputAction action, KeyModifiers modifiers) =>
        AddKey((Key)key, 0, action, modifiers);
    
    public void AddKey(Key key, int scancode, InputAction action, KeyModifiers modifiers) =>
        Add(new Input(key, scancode, (byte)action, (byte)modifiers));
    
    public void AddMousePos(double x, double y) => 
        Add(new Input((float)x, (float)y, InputType.MousePos));
    
    public void AddMouseScroll(double x, double y) => 
        Add(new Input((float)x, (float)y, InputType.MouseScroll));

    /// <summary>
    /// Adds an <see cref="Input"/> to the current writing buffer
    /// </summary>
    /// <param name="input"></param>
    public void Add(Input input)
    {
        WritingBuffer.Add(input);
    }
    
    public IEnumerator<Input> GetEnumerator() => ReadingBuffer.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[StructLayout(LayoutKind.Explicit, Size = 9)]
public struct Input
{
    public Input(Key key, int scancode, byte action, byte modifiers)
    {
        Type = InputType.Key;
        Key = key;
        Scancode = scancode;
        Action = action;
        Modifiers = modifiers;
    }
    
    public Input(float x, float y, InputType type)
    {
        Type = type;
        X = x;
        Y = y;
    }
    
    [FieldOffset(0)]
    public InputType Type; // 1 byte
    
    // Key
    [FieldOffset(1)]
    public Key Key; // 2 bytes
    [FieldOffset(3)]
    public int Scancode; // 4 bytes
    [FieldOffset(7)]
    public byte Action; // 1 byte
    [FieldOffset(8)]
    public byte Modifiers; // 1 byte
    
    // mouse move / scroll
    [FieldOffset(1)]
    public float X; // 4 bytes
    [FieldOffset(5)]
    public float Y; // 4 bytes
    
    public Vec2<float> Vec => (X, Y);
}

public enum InputType : byte
{
    Key,
    MousePos,
    MouseScroll,
}

[StructLayout(LayoutKind.Explicit, Size = 2)]
public struct InputState(InputStateType type, byte repetitions)
{
    [FieldOffset(0)]
    public InputStateType Type = type;
    [FieldOffset(1)]
    public byte Repetitions = repetitions;

    public override string ToString()
    {
        return $"{Type} x{Repetitions}";
    }
}

public enum InputStateType : byte
{
    /// <summary>
    /// The key is held down as of at least last frame.
    /// </summary>
    Pressed = 3,
    /// <summary>
    /// The key is not held down as of at least last frame.
    /// </summary>
    Released = 4,
    
    
    /// <summary>
    /// The key was pressed this frame.
    /// </summary>
    RisingEdge = 1,
    /// <summary>
    /// The key was released this frame.
    /// </summary>
    FallingEdge = 0,
    
    /// <summary>
    /// The key was held down until it repeated this frame.
    /// </summary>
    Repeat = 2,
}