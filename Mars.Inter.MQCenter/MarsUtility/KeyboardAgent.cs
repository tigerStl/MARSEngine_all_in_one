using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.MarsUtility
{
    /// <summary>
    /// 键盘事件结构
    /// </summary>
    internal class KeyEvent
    {
        public string Key { get; set; }
        public bool IsDown { get; set; }
    }

    public class KeyboardAgent
    {
        /// <summary>
        /// 使用SendInput API发送键盘输入，对MFC控件更可靠
        /// </summary>
        /// <param name="strData">要发送的键盘数据</param>
        /// <param name="strError">错误信息</param>
        /// <returns>是否成功</returns>
        public static bool SendKeysWithSendInput(string strData, ref string strError)
        {
            try
            {
                if (string.IsNullOrEmpty(strData))
                {
                    strError = "Input data is null or empty";
                    return false;
                }

                // 解析键盘输入字符串，支持特殊键组合
                var keySequence = ParseKeySequence(strData);
                if (keySequence.Count == 0)
                {
                    strError = "No valid keys found in input data";
                    return false;
                }

                // 使用SendInput发送键盘事件
                foreach (var keyEvent in keySequence)
                {
                    if (!SendKeyEvent(keyEvent))
                    {
                        strError = $"Failed to send key event: {keyEvent.Key}";
                        return false;
                    }
                    System.Threading.Thread.Sleep(10); // 小延时确保按键顺序
                }

                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in SendKeysWithSendInput: {ex.Message}";
                return false;
            }
        }


        /// <summary>
        /// 处理SendKeys转义字符
        /// </summary>
        private static string ProcessSendKeysEscapes(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 重要：不要在这里替换花括号内的特殊键，否则会把 {END} 变成 "END" 字母
            // 从而无法被后续的 ParseSendKeysFormat 识别为特殊键。
            // 这里保持输入原样返回，特殊键交给 ParseSendKeysFormat 处理。
            return input;
        }



        /// <summary>
        /// 获取SendKeys键名
        /// </summary>
        private static string GetSendKeysKeyName(string key)
        {
            switch (key.ToUpper())
            {
                case "ENTER":
                    return "ENTER";
                case "TAB":
                    return "TAB";
                case "ESC":
                case "ESCAPE":
                    return "ESC";
                case "SPACE":
                    return "SPACE";
                case "BACKSPACE":
                case "BS":
                    return "BACKSPACE";
                case "DELETE":
                case "DEL":
                    return "DELETE";
                case "HOME":
                    return "HOME";
                case "END":
                    return "END";
                case "UP":
                    return "UP";
                case "DOWN":
                    return "DOWN";
                case "LEFT":
                    return "LEFT";
                case "RIGHT":
                    return "RIGHT";
                case "F1":
                case "F2":
                case "F3":
                case "F4":
                case "F5":
                case "F6":
                case "F7":
                case "F8":
                case "F9":
                case "F10":
                case "F11":
                case "F12":
                    return key;
                default:
                    return key;
            }
        }

        /// <summary>
        /// 获取虚拟键码
        /// </summary>
        private static string GetVirtualKey(string key)
        {
            switch (key.ToUpper())
            {
                case "CTRL":
                case "CONTROL":
                    return "VK_CONTROL";
                case "ALT":
                    return "VK_MENU";
                case "SHIFT":
                    return "VK_SHIFT";
                case "ENTER":
                    return "VK_RETURN";
                case "TAB":
                    return "VK_TAB";
                case "ESC":
                case "ESCAPE":
                    return "VK_ESCAPE";
                case "SPACE":
                    return "VK_SPACE";
                case "BACKSPACE":
                    return "VK_BACK";
                case "DELETE":
                    return "VK_DELETE";
                case "HOME":
                    return "VK_HOME";
                case "END":
                    return "VK_END";
                case "UP":
                    return "VK_UP";
                case "DOWN":
                    return "VK_DOWN";
                case "LEFT":
                    return "VK_LEFT";
                case "RIGHT":
                    return "VK_RIGHT";
                case "F1":
                    return "VK_F1";
                case "F2":
                    return "VK_F2";
                case "F3":
                    return "VK_F3";
                case "F4":
                    return "VK_F4";
                case "F5":
                    return "VK_F5";
                case "F6":
                    return "VK_F6";
                case "F7":
                    return "VK_F7";
                case "F8":
                    return "VK_F8";
                case "F9":
                    return "VK_F9";
                case "F10":
                    return "VK_F10";
                case "F11":
                    return "VK_F11";
                case "F12":
                    return "VK_F12";
                default:
                    if (key.Length == 1)
                    {
                        return ((int)key[0]).ToString(); // 返回ASCII码
                    }
                    return "VK_SPACE"; // 默认返回空格
            }
        }

        /// <summary>
        /// 解析键盘输入字符串，支持SendKeys转义字符和特殊键组合
        /// </summary>
        private static List<KeyEvent> ParseKeySequence(string input)
        {
            var keyEvents = new List<KeyEvent>();

            if (string.IsNullOrEmpty(input))
                return keyEvents;

            // 处理SendKeys转义字符
            input = ProcessSendKeysEscapes(input);

            // 检查是否包含SendKeys转义字符或花括号按键（如 {END}、{HOME}）
            if (input.Contains("^") || input.Contains("%") || input.Contains("+") || input.Contains("{"))
            {
                // 处理SendKeys格式的组合键，如 ^c (Ctrl+C), %{F4} (Alt+F4)
                keyEvents = ParseSendKeysFormat(input);
            }
            else if (input.Contains("+"))
            {
                // 处理标准格式的组合键，如Ctrl+C
                var parts = input.Split('+');
                if (parts.Length == 2)
                {
                    var modifier = parts[0].Trim().ToUpper();
                    var key = parts[1].Trim();

                    // 按下修饰键
                    if (modifier == "CTRL" || modifier == "CONTROL")
                    {
                        keyEvents.Add(new KeyEvent { Key = "VK_CONTROL", IsDown = true });
                    }
                    else if (modifier == "ALT")
                    {
                        keyEvents.Add(new KeyEvent { Key = "VK_MENU", IsDown = true });
                    }
                    else if (modifier == "SHIFT")
                    {
                        keyEvents.Add(new KeyEvent { Key = "VK_SHIFT", IsDown = true });
                    }

                    // 按下主键
                    keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key), IsDown = true });
                    keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key), IsDown = false });

                    // 释放修饰键
                    if (modifier == "CTRL" || modifier == "CONTROL")
                    {
                        keyEvents.Add(new KeyEvent { Key = "VK_CONTROL", IsDown = false });
                    }
                    else if (modifier == "ALT")
                    {
                        keyEvents.Add(new KeyEvent { Key = "VK_MENU", IsDown = false });
                    }
                    else if (modifier == "SHIFT")
                    {
                        keyEvents.Add(new KeyEvent { Key = "VK_SHIFT", IsDown = false });
                    }
                }
            }
            else
            {
                // 处理普通字符
                foreach (char c in input)
                {
                    keyEvents.Add(new KeyEvent { Key = GetVirtualKey(c.ToString()), IsDown = true });
                    keyEvents.Add(new KeyEvent { Key = GetVirtualKey(c.ToString()), IsDown = false });
                }
            }

            return keyEvents;
        }


        /// <summary>
        /// 发送单个键盘事件
        /// </summary>
        private static bool SendKeyEvent(KeyEvent keyEvent)
        {
            try
            {
                byte vk = 0;

                if (keyEvent.Key.StartsWith("VK_"))
                {
                    // 处理虚拟键码
                    switch (keyEvent.Key)
                    {
                        case "VK_CONTROL":
                            vk = 0x11;
                            break;
                        case "VK_MENU":
                            vk = 0x12;
                            break;
                        case "VK_SHIFT":
                            vk = 0x10;
                            break;
                        case "VK_RETURN":
                            vk = 0x0D;
                            break;
                        case "VK_TAB":
                            vk = 0x09;
                            break;
                        case "VK_ESCAPE":
                            vk = 0x1B;
                            break;
                        case "VK_SPACE":
                            vk = 0x20;
                            break;
                        case "VK_BACK":
                            vk = 0x08;
                            break;
                        case "VK_DELETE":
                            vk = 0x2E;
                            break;
                        case "VK_HOME":
                            vk = 0x24;
                            break;
                        case "VK_END":
                            vk = 0x23;
                            break;
                        case "VK_UP":
                            vk = 0x26;
                            break;
                        case "VK_DOWN":
                            vk = 0x28;
                            break;
                        case "VK_LEFT":
                            vk = 0x25;
                            break;
                        case "VK_RIGHT":
                            vk = 0x27;
                            break;
                        case "VK_F1":
                            vk = 0x70;
                            break;
                        case "VK_F2":
                            vk = 0x71;
                            break;
                        case "VK_F3":
                            vk = 0x72;
                            break;
                        case "VK_F4":
                            vk = 0x73;
                            break;
                        case "VK_F5":
                            vk = 0x74;
                            break;
                        case "VK_F6":
                            vk = 0x75;
                            break;
                        case "VK_F7":
                            vk = 0x76;
                            break;
                        case "VK_F8":
                            vk = 0x77;
                            break;
                        case "VK_F9":
                            vk = 0x78;
                            break;
                        case "VK_F10":
                            vk = 0x79;
                            break;
                        case "VK_F11":
                            vk = 0x7A;
                            break;
                        case "VK_F12":
                            vk = 0x7B;
                            break;
                    }
                }
                else if (int.TryParse(keyEvent.Key, out int asciiCode))
                {
                    vk = (byte)asciiCode;
                }
                else
                {
                    vk = 0x20; // 默认空格
                }

                int flags = keyEvent.IsDown ? 0 : 0x0002; // KEYEVENTF_KEYUP
                MarsWindowsAPIs.keybd_event(vk, 0, (uint)flags, 0);

                return true;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("SendKeyEvent", $"Failed to send key event: {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// 解析SendKeys格式的输入，如 ^c, %{F4}, +{TAB} 等
        /// </summary>
        private static List<KeyEvent> ParseSendKeysFormat(string input)
        {
            var keyEvents = new List<KeyEvent>();
            var i = 0;

            while (i < input.Length)
            {
                char current = input[i];

                if (current == '^')
                {
                    // Ctrl键组合，如 ^c
                    i++; // 跳过 ^
                    if (i < input.Length)
                    {
                        char key = input[i];
                        // 按下Ctrl
                        keyEvents.Add(new KeyEvent { Key = "VK_CONTROL", IsDown = true });
                        // 按下主键
                        keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key.ToString()), IsDown = true });
                        keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key.ToString()), IsDown = false });
                        // 释放Ctrl
                        keyEvents.Add(new KeyEvent { Key = "VK_CONTROL", IsDown = false });
                    }
                }
                else if (current == '%')
                {
                    // Alt键组合，如 %{F4}
                    i++; // 跳过 %
                    if (i < input.Length && input[i] == '{')
                    {
                        // 处理 {F4} 格式
                        var endIndex = input.IndexOf('}', i);
                        if (endIndex > i)
                        {
                            var key = input.Substring(i + 1, endIndex - i - 1);
                            // 按下Alt
                            keyEvents.Add(new KeyEvent { Key = "VK_MENU", IsDown = true });
                            // 按下主键
                            keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key), IsDown = true });
                            keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key), IsDown = false });
                            // 释放Alt
                            keyEvents.Add(new KeyEvent { Key = "VK_MENU", IsDown = false });
                            i = endIndex;
                        }
                    }
                    else if (i < input.Length)
                    {
                        char key = input[i];
                        // 按下Alt
                        keyEvents.Add(new KeyEvent { Key = "VK_MENU", IsDown = true });
                        // 按下主键
                        keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key.ToString()), IsDown = true });
                        keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key.ToString()), IsDown = false });
                        // 释放Alt
                        keyEvents.Add(new KeyEvent { Key = "VK_MENU", IsDown = false });
                    }
                }
                else if (current == '+')
                {
                    // Shift键组合，如 +{TAB}
                    i++; // 跳过 +
                    if (i < input.Length && input[i] == '{')
                    {
                        // 处理 {TAB} 格式
                        var endIndex = input.IndexOf('}', i);
                        if (endIndex > i)
                        {
                            var key = input.Substring(i + 1, endIndex - i - 1);
                            // 按下Shift
                            keyEvents.Add(new KeyEvent { Key = "VK_SHIFT", IsDown = true });
                            // 按下主键
                            var vkKey = GetVirtualKey(key.Trim().ToUpperInvariant());
                            keyEvents.Add(new KeyEvent { Key = vkKey, IsDown = true });
                            keyEvents.Add(new KeyEvent { Key = vkKey, IsDown = false });
                            // 释放Shift
                            keyEvents.Add(new KeyEvent { Key = "VK_SHIFT", IsDown = false });
                            i = endIndex;
                        }
                    }
                    else if (i < input.Length)
                    {
                        char key = input[i];
                        // 按下Shift
                        keyEvents.Add(new KeyEvent { Key = "VK_SHIFT", IsDown = true });
                        // 按下主键
                        keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key.ToString()), IsDown = true });
                        keyEvents.Add(new KeyEvent { Key = GetVirtualKey(key.ToString()), IsDown = false });
                        // 释放Shift
                        keyEvents.Add(new KeyEvent { Key = "VK_SHIFT", IsDown = false });
                    }
                }
                else if (current == '{')
                {
                    // 处理花括号内的特殊键，如 {ENTER}, {TAB}
                    var endIndex = input.IndexOf('}', i);
                    if (endIndex > i)
                    {
                        var key = input.Substring(i + 1, endIndex - i - 1);
                        // 将 {END}、{HOME} 等规范化为虚拟键
                        var vk = GetVirtualKey(key);
                        keyEvents.Add(new KeyEvent { Key = vk, IsDown = true });
                        keyEvents.Add(new KeyEvent { Key = vk, IsDown = false });
                        i = endIndex;
                    }
                }
                else
                {
                    // 普通字符
                    keyEvents.Add(new KeyEvent { Key = GetVirtualKey(current.ToString()), IsDown = true });
                    keyEvents.Add(new KeyEvent { Key = GetVirtualKey(current.ToString()), IsDown = false });
                }

                i++;
            }

            return keyEvents;
        }

    }
}
