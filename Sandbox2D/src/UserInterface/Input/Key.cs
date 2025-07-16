using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Sandbox2D.UserInterface.Input;

/// <summary>
/// Represents a key. Represents a merged version of <see cref="Keys"/> and <see cref="MouseButton"/>.
/// </summary>
public enum Key : short
{
    /// <summary>An unknown key</summary>
    Unknown = -0x1,
    
    /// <summary>The lowest mouse button available</summary>
    FirstMouseKey = 0x0000,
    /// <summary>The highest mouse button available</summary>
    LastMouseKey = 0x0007,
    /// <summary>The lowest keyboard key available</summary>
    FirstKeyboardKey = 0x0020,
    /// <summary>The highest keyboard key available</summary>
    LastKeyboardKey = 0x015C,
    /// <summary>The lowest key available</summary>
    FirstKey = 0x000,
    /// <summary>The highest key available</summary>
    LastKey = 0x15C,
    
    /// <summary>The left mouse button</summary>
    LeftMouse = 0x0000,
    /// <summary>The right mouse button</summary>
    RightMouse = 0x0001,
    /// <summary>The middle mouse button</summary>
    MiddleMouse = 0x0002,
    
    /// <summary>The first mouse button</summary>
    Mouse1 = 0x0000,
    /// <summary>The second mouse button</summary>
    Mouse2 = 0x0001,
    /// <summary>The third mouse button</summary>
    Mouse3 = 0x0002,
    /// <summary>The fourth mouse button</summary>
    Mouse4 = 0x0003,
    /// <summary>The fifth mouse button</summary>
    Mouse5 = 0x0004,
    /// <summary>The sixth mouse button</summary>
    Mouse6 = 0x0005,
    /// <summary>The seventh mouse button</summary>
    Mouse7 = 0x0006,
    /// <summary>The eighth mouse button</summary>
    Mouse8 = 0x0007,
    
    /// <summary>The spacebar key</summary>
    Space = 0x0020,
    /// <summary>The apostrophe key</summary>
    Apostrophe = 0x0027,
    /// <summary>The comma key</summary>
    Comma = 0x002C,
    /// <summary>The minus key</summary>
    Minus = 0x002D,
    /// <summary>The period key</summary>
    Period = 0x002E,
    /// <summary>The slash key</summary>
    Slash = 0x002F,
    /// <summary>The 0 key</summary>
    D0 = 0x0030,
    /// <summary>The 1 key</summary>
    D1 = 0x0031,
    /// <summary>The 2 key</summary>
    D2 = 0x0032,
    /// <summary>The 3 key</summary>
    D3 = 0x0033,
    /// <summary>The 4 key</summary>
    D4 = 0x0034,
    /// <summary>The 5 key</summary>
    D5 = 0x0035,
    /// <summary>The 6 key</summary>
    D6 = 0x0036,
    /// <summary>The 7 key</summary>
    D7 = 0x0037,
    /// <summary>The 8 key</summary>
    D8 = 0x0038,
    /// <summary>The 9 key</summary>
    D9 = 0x0039,
    /// <summary>The semicolon key</summary>
    Semicolon = 0x003B,
    /// <summary>The equal key</summary>
    Equal = 0x003D,
    /// <summary>The A key</summary>
    A = 0x0041,
    /// <summary>The B key</summary>
    B = 0x0042,
    /// <summary>The C key</summary>
    C = 0x0043,
    /// <summary>The D key</summary>
    D = 0x0044,
    /// <summary>The E key</summary>
    E = 0x0045,
    /// <summary>The F key</summary>
    F = 0x0046,
    /// <summary>The G key</summary>
    G = 0x0047,
    /// <summary>The H key</summary>
    H = 0x0048,
    /// <summary>The I key</summary>
    I = 0x0049,
    /// <summary>The J key</summary>
    J = 0x004A,
    /// <summary>The K key</summary>
    K = 0x004B,
    /// <summary>The L key</summary>
    L = 0x004C,
    /// <summary>The M key</summary>
    M = 0x004D,
    /// <summary>The N key</summary>
    N = 0x004E,
    /// <summary>The O key</summary>
    O = 0x004F,
    /// <summary>The P key</summary>
    P = 0x0050,
    /// <summary>The Q key</summary>
    Q = 0x0051,
    /// <summary>The R key</summary>
    R = 0x0052,
    /// <summary>The S key</summary>
    S = 0x0053,
    /// <summary>The T key</summary>
    T = 0x0054,
    /// <summary>The U key</summary>
    U = 0x0055,
    /// <summary>The V key</summary>
    V = 0x0056,
    /// <summary>The W key</summary>
    W = 0x0057,
    /// <summary>The X key</summary>
    X = 0x0058,
    /// <summary>The Y key</summary>
    Y = 0x0059,
    /// <summary>The Z key</summary>
    Z = 0x005A,
    /// <summary>The left bracket(opening bracket) key</summary>
    LeftBracket = 0x005B,
    /// <summary>The backslash</summary>
    Backslash = 0x005C,
    /// <summary>The right bracket(closing bracket) key</summary>
    RightBracket = 0x005D,
    /// <summary>The grave accent key</summary>
    GraveAccent = 0x0060,
    /// <summary>The escape key</summary>
    Escape = 0x0100,
    /// <summary>The enter key</summary>
    Enter = 0x0101,
    /// <summary>The tab key</summary>
    Tab = 0x0102,
    /// <summary>The backspace key</summary>
    Backspace = 0x0103,
    /// <summary>The insert key</summary>
    Insert = 0x0104,
    /// <summary>The delete key</summary>
    Delete = 0x0105,
    /// <summary>The right arrow key</summary>
    RightArrow = 0x0106,
    /// <summary>The left arrow key</summary>
    LeftArrow = 0x0107,
    /// <summary>The down arrow key</summary>
    DownArrow = 0x0108,
    /// <summary>The up arrow key</summary>
    UpArrow = 0x0109,
    /// <summary>The page up key</summary>
    PageUp = 0x010A,
    /// <summary>The page down key</summary>
    PageDown = 0x010B,
    /// <summary>The home key</summary>
    Home = 0x010C,
    /// <summary>The end key</summary>
    End = 0x010D,
    /// <summary>The caps lock key</summary>
    CapsLock = 0x0118,
    /// <summary>The scroll lock key</summary>
    ScrollLock = 0x0119,
    /// <summary>The num lock key</summary>
    NumLock = 0x011A,
    /// <summary>The print screen key</summary>
    PrintScreen = 0x011B,
    /// <summary>The pause key</summary>
    Pause = 0x011C,
    /// <summary>The F1 key</summary>
    F1 = 0x0122,
    /// <summary>The F2 key</summary>
    F2 = 0x0123,
    /// <summary>The F3 key</summary>
    F3 = 0x0124,
    /// <summary>The F4 key</summary>
    F4 = 0x0125,
    /// <summary>The F5 key</summary>
    F5 = 0x0126,
    /// <summary>The F6 key</summary>
    F6 = 0x0127,
    /// <summary>The F7 key</summary>
    F7 = 0x0128,
    /// <summary>The F8 key</summary>
    F8 = 0x0129,
    /// <summary>The F9 key</summary>
    F9 = 0x012A,
    /// <summary>The F10 key</summary>
    F10 = 0x012B,
    /// <summary>The F11 key</summary>
    F11 = 0x012C,
    /// <summary>The F12 key</summary>
    F12 = 0x012D,
    /// <summary>The F13 key</summary>
    F13 = 0x012E,
    /// <summary>The F14 key</summary>
    F14 = 0x012F,
    /// <summary>The F15 key</summary>
    F15 = 0x0130,
    /// <summary>The F16 key</summary>
    F16 = 0x0131,
    /// <summary>The F17 key</summary>
    F17 = 0x0132,
    /// <summary>The F18 key</summary>
    F18 = 0x0133,
    /// <summary>The F19 key</summary>
    F19 = 0x0134,
    /// <summary>The F20 key</summary>
    F20 = 0x0135,
    /// <summary>The F21 key</summary>
    F21 = 0x0136,
    /// <summary>The F22 key</summary>
    F22 = 0x0137,
    /// <summary>The F23 key</summary>
    F23 = 0x0138,
    /// <summary>The F24 key</summary>
    F24 = 0x0139,
    /// <summary>The F25 key</summary>
    F25 = 0x013A,
    /// <summary>The 0 key on the key pad</summary>
    KeyPad0 = 0x0140,
    /// <summary>The 1 key on the key pad</summary>
    KeyPad1 = 0x0141,
    /// <summary>The 2 key on the key pad</summary>
    KeyPad2 = 0x0142,
    /// <summary>The 3 key on the key pad</summary>
    KeyPad3 = 0x0143,
    /// <summary>The 4 key on the key pad</summary>
    KeyPad4 = 0x0144,
    /// <summary>The 5 key on the key pad</summary>
    KeyPad5 = 0x0145,
    /// <summary>The 6 key on the key pad</summary>
    KeyPad6 = 0x0146,
    /// <summary>The 7 key on the key pad</summary>
    KeyPad7 = 0x0147,
    /// <summary>The 8 key on the key pad</summary>
    KeyPad8 = 0x0148,
    /// <summary>The 9 key on the key pad</summary>
    KeyPad9 = 0x0149,
    /// <summary>The decimal key on the key pad</summary>
    KeyPadDecimal = 0x014A,
    /// <summary>The divide key on the key pad</summary>
    KeyPadDivide = 0x014B,
    /// <summary>The multiply key on the key pad</summary>
    KeyPadMultiply = 0x014C,
    /// <summary>The subtract key on the key pad</summary>
    KeyPadSubtract = 0x014D,
    /// <summary>The add key on the key pad</summary>
    KeyPadAdd = 0x014E,
    /// <summary>The enter key on the key pad</summary>
    KeyPadEnter = 0x014F,
    /// <summary>The equal key on the key pad</summary>
    KeyPadEqual = 0x0150,
    /// <summary>The left shift key</summary>
    LeftShift = 0x0154,
    /// <summary>The left control key</summary>
    LeftControl = 0x0155,
    /// <summary>The left alt key</summary>
    LeftAlt = 0x0156,
    /// <summary>The left super key</summary>
    LeftSuper = 0x0157,
    /// <summary>The right shift key</summary>
    RightShift = 0x0158,
    /// <summary>The right control key</summary>
    RightControl = 0x0159,
    /// <summary>The right alt key</summary>
    RightAlt = 0x015A,
    /// <summary>The right super key</summary>
    RightSuper = 0x015B,
    /// <summary>The menu key</summary>
    Menu = 0x015C,
}
