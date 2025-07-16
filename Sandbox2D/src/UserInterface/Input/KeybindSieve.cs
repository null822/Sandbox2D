using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Math2D;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox2D.UserInterface.Input;

public class KeybindSieve : IDisposable
{
    private const int KeyCount = (int)Key.LastKeyboardKey + 1;
    
    private readonly List<int> _activeStages = [];
    private readonly SieveStage[] _stages = new SieveStage[KeyCount];
    
    private readonly Dictionary<string, int> _names = [];
    private readonly DynamicArray<Action> _actions = new();
    private int _keybindCount;
    
    private BitArray _sieveKeybinds = new(0);
    
    public KeybindSieve()
    {
        for (var i = 0; i < (int)Key.LastKeyboardKey + 1; i++)
        {
            _stages[i] = new SieveStage();
        }
    }
    
    public void Add(string name, Key key, KeybindKeyType type, Action action) => Add(name, [(key, type)], action);
    public void Add(string name, (Key Key, KeybindKeyType Type)[] keys, Action action)
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
    
    public void Call(InputTimeline timeline)
    {
        if (_sieveKeybinds.Length != _keybindCount)
        {
            _sieveKeybinds = new BitArray(_keybindCount);
        }
        
        _sieveKeybinds.SetAll(false);
        
        foreach (var i in _activeStages)
        {
            var discards = _stages[i].GetDiscards(timeline[(Key)i]);
            foreach (var discard in discards)
            {
                _sieveKeybinds[discard] = true;
            }
        }
        
        for (var i = 0; i < _keybindCount; i++)
        {
            if (_sieveKeybinds[i]) continue;
            
            var action = _actions[i];
            var task = Task.Factory.StartNew(action);
            try
            {
                task.Wait();
            }
            catch (Exception e)
            {
                throw e.InnerException ?? e;
            }
        }
    }
    
    private class SieveStage
    {
        private readonly List<int> _enabledDiscards = [];
        private readonly List<int> _disabledDiscards = [];
        private readonly List<int> _risingEdgeDiscards = [];
        private readonly List<int> _fallingEdgeDiscards = [];
        private readonly List<int> _repeatDiscards = [];
        
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
            _repeatDiscards.Remove(keybindIndex);
        }
        
        public bool IsActive()
        {
            return _enabledDiscards.Count != 0 ||
                   _disabledDiscards.Count != 0 ||
                   _risingEdgeDiscards.Count != 0 ||
                   _fallingEdgeDiscards.Count != 0 ||
                   _repeatDiscards.Count != 0;
        }
        
        public List<int> GetDiscards(InputState input)
        {
            return input.Type switch
            {
                InputStateType.Pressed => _enabledDiscards,
                InputStateType.Released => _disabledDiscards,
                InputStateType.RisingEdge => _risingEdgeDiscards,
                InputStateType.FallingEdge => _fallingEdgeDiscards,
                InputStateType.Repeat => _repeatDiscards,
            };
            
        }
        
    }

    public void Dispose()
    {
        _actions.Clear();
        _names.Clear();
        _activeStages.Clear();
        _keybindCount = 0;
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
