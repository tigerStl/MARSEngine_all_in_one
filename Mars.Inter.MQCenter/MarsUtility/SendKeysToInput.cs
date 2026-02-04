
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mars.Inter.MQCenter.MarsUtility
{

    public static class SendKeysToInput
    {
        // Public entry
        public static void Send(string sequence, int perKeyDelayMs = 0)
        {
            if (string.IsNullOrEmpty(sequence)) return;

            var parser = new Parser(sequence);
            Token token;
            while ((token = parser.Next()).Type != TokenType.EOF)
            {
                switch (token.Type)
                {
                    case TokenType.SpecialKey:
                        SendSpecial(token.SpecialKey, token.Mods, token.Repeat, perKeyDelayMs);
                        break;
                    case TokenType.Text:
                        SendText(token.Text, perKeyDelayMs);
                        break;
                    default:
                        break;
                }
            }
        }

        // ===== Core send helpers =====
        private static void SendSpecial(VK vk, Modifiers mods, int repeat, int delay)
        {
            // Press modifiers down
            var modDown = new List<VK>();
            if (mods.HasFlag(Modifiers.SHIFT)) { KeyDown(VK.SHIFT); modDown.Add(VK.SHIFT); }
            if (mods.HasFlag(Modifiers.CTRL)) { KeyDown(VK.CONTROL); modDown.Add(VK.CONTROL); }
            if (mods.HasFlag(Modifiers.ALT)) { KeyDown(VK.MENU); modDown.Add(VK.MENU); }

            for (int i = 0; i < repeat; i++)
            {
                KeyDown(vk);
                KeyUp(vk);
                if (delay > 0) Thread.Sleep(delay);
            }

            // Release modifiers (reverse order)
            for (int i = modDown.Count - 1; i >= 0; i--)
                KeyUp(modDown[i]);
        }

        private static void SendText(string text, int delay)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Use Unicode input (not affected by layout)
            var inputs = new List<INPUT>(text.Length * 2);
            foreach (var ch in text)
            {
                inputs.Add(new INPUT
                {
                    type = 1, // INPUT_KEYBOARD
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = ch,
                            dwFlags = (uint)KEYEVENTF.UNICODE,
                            time = 0,
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                });
                inputs.Add(new INPUT
                {
                    type = 1,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = ch,
                            dwFlags = (uint)(KEYEVENTF.UNICODE | KEYEVENTF.KEYUP),
                            time = 0,
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                });
                if (delay > 0) { SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>()); inputs.Clear(); Thread.Sleep(delay); }
            }
            if (inputs.Count > 0)
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        }

        private static void KeyDown(VK vk)
        {
            var input = new INPUT
            {
                type = 1,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)vk,
                        wScan = (ushort)MapVirtualKey((uint)vk, 0),
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        private static void KeyUp(VK vk)
        {
            var input = new INPUT
            {
                type = 1,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)vk,
                        wScan = (ushort)MapVirtualKey((uint)vk, 0),
                        dwFlags = (uint)KEYEVENTF.KEYUP,
                        time = 0,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        // ===== Parser =====
        private enum TokenType { SpecialKey, Text, EOF }

        [Flags]
        private enum Modifiers { NONE = 0, SHIFT = 1, CTRL = 2, ALT = 4 }

        private sealed class Token
        {
            public TokenType Type;
            public string Text;             // for Text
            public VK SpecialKey;           // for SpecialKey
            public int Repeat = 1;          // repeat count for SpecialKey
            public Modifiers Mods = Modifiers.NONE; // modifiers applying to this token
            public static readonly Token End = new Token { Type = TokenType.EOF };
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;
            private Modifiers _pendingMods = Modifiers.NONE;
            private readonly Queue<Token> _buffer = new Queue<Token>();

            private static readonly Dictionary<string, VK> KeyMap = new(StringComparer.OrdinalIgnoreCase)
            {
                // Navigation & edit
                ["END"] = VK.END,
                ["HOME"] = VK.HOME,
                ["DEL"] = VK.DELETE,
                ["DELETE"] = VK.DELETE,
                ["INS"] = VK.INSERT,
                ["INSERT"] = VK.INSERT,
                ["BS"] = VK.BACK,
                ["BACKSPACE"] = VK.BACK,
                ["TAB"] = VK.TAB,
                ["ENTER"] = VK.RETURN,
                ["RETURN"] = VK.RETURN,
                ["ESC"] = VK.ESCAPE,
                ["ESCAPE"] = VK.ESCAPE,

                // Arrows & paging
                ["LEFT"] = VK.LEFT,
                ["RIGHT"] = VK.RIGHT,
                ["UP"] = VK.UP,
                ["DOWN"] = VK.DOWN,
                ["PGUP"] = VK.PRIOR,
                ["PGDN"] = VK.NEXT,
                ["PRIOR"] = VK.PRIOR, // Page Up
                ["NEXT"] = VK.NEXT,   // Page Down

                // Function keys
                ["F1"] = VK.F1,
                ["F2"] = VK.F2,
                ["F3"] = VK.F3,
                ["F4"] = VK.F4,
                ["F5"] = VK.F5,
                ["F6"] = VK.F6,
                ["F7"] = VK.F7,
                ["F8"] = VK.F8,
                ["F9"] = VK.F9,
                ["F10"] = VK.F10,
                ["F11"] = VK.F11,
                ["F12"] = VK.F12,

                // Misc
                ["SPACE"] = VK.SPACE,
                ["APPS"] = VK.APPS,
                ["MENU"] = VK.MENU,     // Alt (rare)
                ["LWIN"] = VK.LWIN,
                ["RWIN"] = VK.RWIN,
                ["PRINTSCREEN"] = VK.SNAPSHOT,
                ["PRTSC"] = VK.SNAPSHOT
            };

            public Parser(string s) { _s = s; _i = 0; }

            public Token Next()
            {
                if (_buffer.Count > 0)
                    return _buffer.Dequeue();
                if (_i >= _s.Length) return Token.End;

                // 1) Modifiers before a token: + ^ %
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c == '+') { _pendingMods |= Modifiers.SHIFT; _i++; }
                    else if (c == '^') { _pendingMods |= Modifiers.CTRL; _i++; }
                    else if (c == '%') { _pendingMods |= Modifiers.ALT; _i++; }
                    else break;
                }

                // If modifiers were specified, allow optional whitespace before the next token
                // without emitting a text token, so "+ {HOME}" still applies Shift to {HOME}.
                if (_pendingMods != Modifiers.NONE)
                {
                    while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
                }

                // 1.1) Grouping e.g. +(ABC) applies SHIFT to all tokens inside parentheses
                if (_i < _s.Length && _s[_i] == '(' && _pendingMods != Modifiers.NONE)
                {
                    int depth = 0;
                    int groupStart = _i + 1;
                    int j = _i;
                    while (j < _s.Length)
                    {
                        if (_s[j] == '(') depth++;
                        else if (_s[j] == ')')
                        {
                            depth--;
                            if (depth == 0)
                                break;
                        }
                        j++;
                    }
                    if (j >= _s.Length)
                    {
                        // unmatched, treat '(' as text
                        return EmitText(_s.Substring(_i++));
                    }
                    string inside = _s.Substring(groupStart, j - groupStart);
                    _i = j + 1; // move past ')'

                    // Parse inside with a nested parser, apply current modifiers to each token
                    var inner = new Parser(inside);
                    Token t;
                    while ((t = inner.Next()).Type != TokenType.EOF)
                    {
                        if (t.Type == TokenType.SpecialKey)
                        {
                            t.Mods |= _pendingMods;
                        }
                        _buffer.Enqueue(t);
                    }
                    // modifiers consumed by the group
                    _pendingMods = Modifiers.NONE;
                    // return next token from buffer or continue parsing
                    return Next();
                }

                if (_i >= _s.Length) return Token.End;

                // 2) Special key in braces: {KEY [repeat]}
                if (_s[_i] == '{')
                {
                    int close = _s.IndexOf('}', _i + 1);
                    if (close < 0)
                    {
                        // treat the rest as text if unmatched
                        return EmitText(_s.Substring(_i));
                    }
                    string inside = _s.Substring(_i + 1, close - _i - 1).Trim();
                    _i = close + 1;

                    // Split optional repeat: "{DEL 3}"
                    string keyName = inside;
                    int repeat = 1;
                    int sp = inside.LastIndexOf(' ');
                    if (sp > 0)
                    {
                        string maybeNum = inside.Substring(sp + 1).Trim();
                        if (int.TryParse(maybeNum, out int n) && n > 0)
                        {
                            repeat = n;
                            keyName = inside.Substring(0, sp).Trim();
                        }
                    }

                    if (!KeyMap.TryGetValue(keyName, out var vk))
                    {
                        // Unknown {token} => send literal text of "{token}"
                        var t = new Token { Type = TokenType.Text, Text = "{" + inside + "}" };
                        _pendingMods = Modifiers.NONE; // modifiers only apply to "next token" per SendKeys semantics
                        return t;
                    }

                    var tok = new Token
                    {
                        Type = TokenType.SpecialKey,
                        SpecialKey = vk,
                        Repeat = repeat,
                        Mods = _pendingMods
                    };
                    _pendingMods = Modifiers.NONE;
                    return tok;
                }

                // 3) Plain text chunk until next modifier or '{'
                int start = _i;
                while (_i < _s.Length && _s[_i] != '{' && _s[_i] != '+' && _s[_i] != '^' && _s[_i] != '%')
                    _i++;

                var text = _s.Substring(start, _i - start);
                return EmitText(text);
            }

            private Token EmitText(string text)
            {
                var tok = new Token { Type = TokenType.Text, Text = text };
                // Per SendKeys: pending modifiers are for "next key" only; text won't be modified here.
                _pendingMods = Modifiers.NONE;
                return tok;
            }
        }

        // ===== Win32 interop =====
        private enum KEYEVENTF : uint
        {
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            UNICODE = 0x0004,
            SCANCODE = 0x0008
        }

        private enum VK : ushort
        {
            BACK = 0x08, TAB = 0x09, RETURN = 0x0D, SHIFT = 0x10, CONTROL = 0x11, MENU = 0x12, // Alt
            PAUSE = 0x13, CAPITAL = 0x14, ESCAPE = 0x1B, SPACE = 0x20, PRIOR = 0x21, NEXT = 0x22,
            END = 0x23, HOME = 0x24, LEFT = 0x25, UP = 0x26, RIGHT = 0x27, DOWN = 0x28,
            SNAPSHOT = 0x2C, INSERT = 0x2D, DELETE = 0x2E,
            LWIN = 0x5B, RWIN = 0x5C, APPS = 0x5D,
            F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74, F6 = 0x75,
            F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type; // 1 = INPUT_KEYBOARD
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern nint GetMessageExtraInfo();
    }


}
