using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Mars.AutoTestingDriver.MarsUISupport
{
    /// <summary>
    /// Mars Spied Object Info - 存储通过UIA技术获取的对象信息
    /// </summary>
    public class MarsSpiedObjInfo
    {
        /// <summary>
        /// UIA元素
        /// </summary>
        public AutomationElement Element { get; set; }

        /// <summary>
        /// 元素名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 元素类名
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 元素AutomationId
        /// </summary>
        public string AutomationId { get; set; } = "";

        /// <summary>
        /// 元素控制类型
        /// </summary>
        public string ControlType { get; set; } = "";

        /// <summary>
        /// 元素框架ID
        /// </summary>
        public string FrameworkId { get; set; } = "";

        /// <summary>
        /// 元素文本内容
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// 元素进程ID
        /// </summary>
        public int ProcessId { get; set; } = 0;

        /// <summary>
        /// 元素窗口句柄
        /// </summary>
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;

        /// <summary>
        /// 元素边界矩形
        /// </summary>
        public System.Windows.Rect Bounds { get; set; }

        /// <summary>
        /// 元素路径
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// 元素层级
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// 从UIA元素创建MarsSpiedObjInfo
        /// </summary>
        /// <param name="element">UIA元素</param>
        /// <param name="level">层级</param>
        /// <param name="path">路径</param>
        /// <returns>MarsSpiedObjInfo对象</returns>
        public static MarsSpiedObjInfo FromAutomationElement(AutomationElement element, int level = 0, string path = "")
        {
            if (element == null) return null;

            var info = new MarsSpiedObjInfo
            {
                Element = element,
                Level = level,
                Path = path
            };

            try
            {
                info.Name = element.Current.Name ?? "";
                info.ClassName = element.Current.ClassName ?? "";
                info.AutomationId = element.Current.AutomationId ?? "";
                info.ControlType = element.Current.ControlType.ProgrammaticName ?? "";
                info.FrameworkId = element.Current.FrameworkId ?? "";
                info.ProcessId = element.Current.ProcessId;
                info.WindowHandle = new IntPtr(element.Current.NativeWindowHandle);
                info.Bounds = element.Current.BoundingRectangle;
            }
            catch (Exception)
            {
                // 忽略获取属性时的异常
            }

            return info;
        }

        /// <summary>
        /// 检查元素是否匹配指定的属性
        /// </summary>
        /// <param name="propertyName">属性名</param>
        /// <param name="propertyValue">属性值</param>
        /// <returns>是否匹配</returns>
        public bool MatchesProperty(string propertyName, string propertyValue)
        {
            if (string.IsNullOrEmpty(propertyName) || string.IsNullOrEmpty(propertyValue))
                return false;

            string elementValue = "";
            switch (propertyName.ToLower())
            {
                case "name":
                    elementValue = Name;
                    break;
                case "classname":
                case "class":
                    elementValue = ClassName;
                    break;
                case "automationid":
                    elementValue = AutomationId;
                    break;
                case "controltype":
                    elementValue = ControlType;
                    break;
                case "frameworkid":
                    elementValue = FrameworkId;
                    break;
                case "text":
                    elementValue = Text;
                    break;
                default:
                    return false;
            }

            return string.Equals(elementValue, propertyValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取元素的详细信息字符串
        /// </summary>
        /// <returns>详细信息</returns>
        public string GetDetails()
        {
            var details = new StringBuilder();
            details.AppendLine($"Name: {Name}");
            details.AppendLine($"ClassName: {ClassName}");
            details.AppendLine($"AutomationId: {AutomationId}");
            details.AppendLine($"ControlType: {ControlType}");
            details.AppendLine($"FrameworkId: {FrameworkId}");
            details.AppendLine($"Text: {Text}");
            details.AppendLine($"ProcessId: {ProcessId}");
            details.AppendLine($"WindowHandle: 0x{WindowHandle:X}");
            details.AppendLine($"Level: {Level}");
            details.AppendLine($"Path: {Path}");
            return details.ToString();
        }
    }
}
