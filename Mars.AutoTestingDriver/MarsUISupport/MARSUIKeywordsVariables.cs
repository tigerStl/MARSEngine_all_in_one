using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using Accessibility;

namespace Mars.AutoTestingDriver.MarsUISupport
{
    /// <summary>
    /// MarsUI Keywords Variables - 存储MarsUI关键字操作中的变量和状态
    /// </summary>
    public class MARSUIKeywordsVariables
    {
        /// <summary>
        /// 当前PEG窗口对象 - 可以是UIA element或IAccessible对象
        /// </summary>
        public object currentPegwindow { get; set; }

        /// <summary>
        /// 当前PEG窗口的路径信息
        /// </summary>
        public string path { get; set; } = "";

        /// <summary>
        /// 当前PEG窗口的UIA元素（如果使用UIA）
        /// </summary>
        public AutomationElement CurrentUIAElement
        {
            get
            {
                if (currentPegwindow is AutomationElement element)
                    return element;
                return null;
            }
        }

        /// <summary>
        /// 当前PEG窗口的IAccessible对象（如果使用IAccessible）
        /// </summary>
        public IAccessible CurrentIAccessible
        {
            get
            {
                if (currentPegwindow is IAccessible accessible)
                    return accessible;
                return null;
            }
        }

        /// <summary>
        /// 判断当前PEG窗口是否为UIA元素
        /// </summary>
        public bool IsUIAElement => currentPegwindow is AutomationElement;

        /// <summary>
        /// 判断当前PEG窗口是否为IAccessible对象
        /// </summary>
        public bool IsIAccessible => currentPegwindow is IAccessible;

        /// <summary>
        /// 获取当前PEG窗口的句柄
        /// </summary>
        public IntPtr GetWindowHandle()
        {
            if (IsUIAElement && CurrentUIAElement != null)
            {
                try
                {
                    return new IntPtr(CurrentUIAElement.Current.NativeWindowHandle);
                }
                catch
                {
                    return IntPtr.Zero;
                }
            }
            else if (IsIAccessible && CurrentIAccessible != null)
            {
                try
                {
                    // 对于IAccessible，需要通过其他方式获取句柄
                    // 这里可能需要根据具体实现来调整
                    return IntPtr.Zero;
                }
                catch
                {
                    return IntPtr.Zero;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 获取当前PEG窗口的进程ID
        /// </summary>
        public int GetProcessId()
        {
            if (IsUIAElement && CurrentUIAElement != null)
            {
                try
                {
                    return CurrentUIAElement.Current.ProcessId;
                }
                catch
                {
                    return 0;
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取当前PEG窗口的名称
        /// </summary>
        public string GetWindowName()
        {
            if (IsUIAElement && CurrentUIAElement != null)
            {
                try
                {
                    return CurrentUIAElement.Current.Name ?? "";
                }
                catch
                {
                    return "";
                }
            }
            else if (IsIAccessible && CurrentIAccessible != null)
            {
                try
                {
                    return CurrentIAccessible.get_accName(0) ?? "";
                }
                catch
                {
                    return "";
                }
            }
            return "";
        }

        /// <summary>
        /// 获取当前PEG窗口的类名
        /// </summary>
        public string GetClassName()
        {
            if (IsUIAElement && CurrentUIAElement != null)
            {
                try
                {
                    return CurrentUIAElement.Current.ClassName ?? "";
                }
                catch
                {
                    return "";
                }
            }
            return "";
        }

        /// <summary>
        /// 获取当前PEG窗口的AutomationId
        /// </summary>
        public string GetAutomationId()
        {
            if (IsUIAElement && CurrentUIAElement != null)
            {
                try
                {
                    return CurrentUIAElement.Current.AutomationId ?? "";
                }
                catch
                {
                    return "";
                }
            }
            return "";
        }

        /// <summary>
        /// 设置当前PEG窗口为UIA元素
        /// </summary>
        /// <param name="element">UIA元素</param>
        /// <param name="elementPath">元素路径</param>
        public void SetUIAElement(AutomationElement element, string elementPath = "")
        {
            currentPegwindow = element;
            path = elementPath;
        }

        /// <summary>
        /// 设置当前PEG窗口为IAccessible对象
        /// </summary>
        /// <param name="accessible">IAccessible对象</param>
        /// <param name="elementPath">元素路径</param>
        public void SetIAccessible(IAccessible accessible, string elementPath = "")
        {
            currentPegwindow = accessible;
            path = elementPath;
        }

        /// <summary>
        /// 清除当前PEG窗口
        /// </summary>
        public void Clear()
        {
            currentPegwindow = null;
            path = "";
        }

        /// <summary>
        /// 检查当前PEG窗口是否有效
        /// </summary>
        public bool IsValid()
        {
            return currentPegwindow != null && !string.IsNullOrEmpty(path);
        }

        /// <summary>
        /// 获取当前PEG窗口的详细信息字符串
        /// </summary>
        public string GetDetails()
        {
            if (!IsValid())
                return "No valid PEG window";

            var details = new StringBuilder();
            details.AppendLine($"Path: {path}");
            details.AppendLine($"Type: {(IsUIAElement ? "UIA Element" : IsIAccessible ? "IAccessible" : "Unknown")}");
            
            if (IsUIAElement)
            {
                details.AppendLine($"Name: {GetWindowName()}");
                details.AppendLine($"ClassName: {GetClassName()}");
                details.AppendLine($"AutomationId: {GetAutomationId()}");
                details.AppendLine($"ProcessId: {GetProcessId()}");
                details.AppendLine($"Handle: 0x{GetWindowHandle():X}");
            }
            else if (IsIAccessible)
            {
                details.AppendLine($"Name: {GetWindowName()}");
            }

            return details.ToString();
        }
    }

   
}
