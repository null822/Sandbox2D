﻿using System;
using System.Collections;
using System.Collections.Generic;
using Math2D;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox2D.UserInterface.Keybinds;

public class KeybindSieve
{
    private const int KeyCount = (int)Key.LastKeyboardKey + 1;
    
    private readonly List<int> _activeStages = [];
    private readonly SieveStage[] _stages = new SieveStage[KeyCount];
    
    private readonly Dictionary<string, int> _names = [];
    private readonly DynamicArray<Action> _actions = new();
    private int _keybindCount;
    
    public KeybindSieve()
    {
        for (var i = 0; i < (int)Key.LastKeyboardKey + 1; i++)
        {
            _stages[i] = new SieveStage();
        }
    }
    
    public void Add(string name, Action action, (Key Key, KeybindKeyType Type)[] keys)
    {
        if (_names.ContainsKey(name))
            throw new Exception($"Keybind \'{name}\' already exists");
        
        var keybindIndex = (int)_actions.Length;
        
        _names.Add(name, keybindIndex);
        _actions.Add(action);
        
        foreach (var key in keys)
        {
            var keyId = (int)key.Key;
            _stages[keyId].Add(keybindIndex, key.Type);
            // activate the stage if it is not already active
            if (!_activeStages.Contains(keyId))
                _activeStages.Add(keyId);
        }
        
        _keybindCount++;
    }
    
    public void Remove(string name)
    {
        if (!_names.Remove(name, out var keybindIndex))
            throw new Exception($"Keybind \'{name}\' does not exist");
        
        _actions.Remove(keybindIndex);
        
        for (var i = 0; i < KeyCount; i++)
        {
            _stages[i].Remove(keybindIndex);
            // deactivate the stage if it is empty
            if (!_stages[i].IsActive())
                _activeStages.Remove(i);
        }
        
        _keybindCount--;
    }
    
    public void Change(string name, (Key Key, KeybindKeyType Type)[] newKeys)
    {
        if (!_names.TryGetValue(name, out var keybindIndex))
            throw new Exception($"Keybind \'{name}\' does not exist");
        
        for (var i = 0; i < KeyCount; i++)
        {
            _stages[i].Remove(keybindIndex);
        }
        foreach (var key in newKeys)
        {
            _stages[(int)key.Key].Add(keybindIndex, key.Type);
        }
    }
    
    public void Call(MouseState mouse, KeyboardState keyboard)
    {
        // all the keybinds. false = enabled and true = discarded
        var sieveKeybinds = new BitArray(_keybindCount);
        
        foreach (var i in _activeStages)
        {
            var keyState = i switch
            {
                < (int)Key.LastMouseKey + 1 => mouse.IsButtonDown((MouseButton)i),
                < (int)Key.LastKeyboardKey + 1 => keyboard.IsKeyDown((Keys)i),
                _ => false
            };
            var prevKeyState = i switch
            {
                < (int)Key.LastMouseKey + 1 => mouse.WasButtonDown((MouseButton)i),
                < (int)Key.LastKeyboardKey + 1 => keyboard.WasKeyDown((Keys)i),
                _ => false
            };
            
            var discards = _stages[i].GetDiscards(prevKeyState, keyState);
            foreach (var discard in discards)
            {
                sieveKeybinds[discard] = true;
            }
        }
        
        for (var i = 0; i < _keybindCount; i++)
        {
            if (!sieveKeybinds[i])
                _actions[i].Invoke();
        }
    }
    
    private class SieveStage
    {
        private readonly List<int> _enabledDiscards = [];
        private readonly List<int> _disabledDiscards = [];
        private readonly List<int> _risingEdgeDiscards = [];
        private readonly List<int> _fallingEdgeDiscards = [];
        
        public void Add(int keybindIndex, KeybindKeyType type)
        {
            switch (type)
            {
                case KeybindKeyType.Enabled:
                    _disabledDiscards.Add(keybindIndex);
                    _risingEdgeDiscards.Add(keybindIndex);
                    _fallingEdgeDiscards.Add(keybindIndex);
                    break;
                case KeybindKeyType.Disabled:
                    _enabledDiscards.Add(keybindIndex);
                    _risingEdgeDiscards.Add(keybindIndex);
                    _fallingEdgeDiscards.Add(keybindIndex);
                    break;
                case KeybindKeyType.RisingEdge:
                    _enabledDiscards.Add(keybindIndex);
                    _disabledDiscards.Add(keybindIndex);
                    _fallingEdgeDiscards.Add(keybindIndex);
                    break;
                case KeybindKeyType.FallingEdge:
                    _enabledDiscards.Add(keybindIndex);
                    _disabledDiscards.Add(keybindIndex);
                    _risingEdgeDiscards.Add(keybindIndex);
                    break;
            }
        }
        
        public void Remove(int keybindIndex)
        {
            _enabledDiscards.Remove(keybindIndex);
            _disabledDiscards.Remove(keybindIndex);
            _risingEdgeDiscards.Remove(keybindIndex);
            _fallingEdgeDiscards.Remove(keybindIndex);
        }
        
        public bool IsActive()
        {
            return _enabledDiscards.Count != 0 ||
                   _disabledDiscards.Count != 0 ||
                   _risingEdgeDiscards.Count != 0 ||
                   _fallingEdgeDiscards.Count != 0;
        }
        
        public int[] GetDiscards(bool prevKeyState, bool keyState)
        {
            return ((prevKeyState, keyState) switch
            {
                (true, true) => _enabledDiscards,
                (false, false) => _disabledDiscards,
                (false, true) => _risingEdgeDiscards,
                (true, false) => _fallingEdgeDiscards
            }).ToArray();
            
        }
        
    }
}

public enum KeybindKeyType : byte
{
    Disabled,
    Enabled,
    RisingEdge,
    FallingEdge,
    Ambiguous
}