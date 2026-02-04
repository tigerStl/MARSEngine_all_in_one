using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mars.message.windowsWrapper.SystemUtil;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Inter.MQCenter.interProcess;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// .NET Framework WinForm控件处理辅助类
    /// 专门用于处理纯WinForm控件，直接生成MarsSpiedObjectInfo对象列表
    /// </summary>
    public class WinFormControlHelper
    {
        /// <summary>
        /// 从WinForm控件构建MarsSpiedObjectInfo对象列表（包括所有父对象和子对象）
        /// </summary>
        /// <param name="control">起始控件</param>
        /// <param name="targetHwnd">目标窗口句柄</param>
        /// <returns>MarsSpiedObjectInfo对象列表，第一个是根节点</returns>
        public static List<MarsSpiedObjectInfo> BuildMarsObjectsFromControl(Control control, IntPtr targetHwnd)
        {
            MarsLoggerSimple.logBegin("BuildMarsObjectsFromControl");

            try
            {
                if (control == null)
                {
                    MarsLoggerSimple.Warnning("BuildMarsObjectsFromControl", "Control is null");
                    return new List<MarsSpiedObjectInfo>();
                }

                // 1. 构建父对象链（向上到null，找到根控件）
                var rootControl = GetRootControl(control);
                
                if (rootControl == null)
                {
                    MarsLoggerSimple.Warnning("BuildMarsObjectsFromControl", "Root control is null");
                    return new List<MarsSpiedObjectInfo>();
                }

                // 2. 从根控件开始，构建完整的MarsSpiedObjectInfo树
                var rootMarsObject = CreateMarsSpiedObjectInfo(rootControl, targetHwnd);
                if (rootMarsObject == null)
                {
                    MarsLoggerSimple.Warnning("BuildMarsObjectsFromControl", "Failed to create root MarsSpiedObjectInfo");
                    return new List<MarsSpiedObjectInfo>();
                }

                // 3. 递归构建所有子对象
                BuildChildrenMarsObjects(rootMarsObject, rootControl, targetHwnd);

                // 4. 收集所有MarsSpiedObjectInfo对象
                var allObjects = new List<MarsSpiedObjectInfo>();
                CollectAllMarsObjects(rootMarsObject, allObjects);

                MarsLoggerSimple.logEnd("BuildMarsObjectsFromControl", 
                    $"Built {allObjects.Count} MarsSpiedObjectInfo objects from root: {rootControl.GetType().FullName}");

                return allObjects;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildMarsObjectsFromControl", 
                    $"Error building MarsSpiedObjectInfo from control: {ex.Message}", ex);
                return new List<MarsSpiedObjectInfo>();
            }
            finally
            {
                MarsLoggerSimple.logEnd("BuildMarsObjectsFromControl");
            }
        }

        /// <summary>
        /// 获取控件的根控件（向上遍历到Parent为null）
        /// </summary>
        /// <param name="control">起始控件</param>
        /// <returns>根控件</returns>
        private static Control GetRootControl(Control control)
        {
            if (control == null) return null;

            Control current = control;
            while (current.Parent != null)
            {
                current = current.Parent;
            }

            return current;
        }

        /// <summary>
        /// 从WinForm控件创建MarsSpiedObjectInfo
        /// </summary>
        /// <param name="control">WinForm控件</param>
        /// <param name="targetHwnd">目标窗口句柄</param>
        /// <returns>MarsSpiedObjectInfo对象</returns>
        private static MarsSpiedObjectInfo CreateMarsSpiedObjectInfo(Control control, IntPtr targetHwnd)
        {
            if (control == null) return null;

            try
            {
                var marsInfo = new MarsSpiedObjectInfo
                {
                    objectName = control.Name ?? "",
                    objectType = control.GetType().FullName,
                    Text = control.Text ?? "",
                    x = control.Left,
                    y = control.Top,
                    w = control.Width,
                    h = control.Height,
                    relatedX = control.Left,
                    relatedY = control.Top,
                    isVisible = control.Visible,
                    referenceToObj = control,
                    hwnd = control.Handle.ToInt64(),
                    children = new List<MarsSpiedObjectInfo>()
                };

                // 设置对象路径信息
                marsInfo.objectNamePath = control.Name ?? "";
                marsInfo.objectTypePath = control.GetType().FullName;

                // 如果有父控件，设置父窗口UUID
                if (control.Parent != null)
                {
                    // 父窗口的UUID将在构建树时设置
                }

                MarsLoggerSimple.Info("CreateMarsSpiedObjectInfo", 
                    $"Created MarsSpiedObjectInfo for: {marsInfo.objectName}[{marsInfo.objectType}]");

                return marsInfo;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateMarsSpiedObjectInfo", 
                    $"Error creating MarsSpiedObjectInfo from control: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 递归构建所有子对象的MarsSpiedObjectInfo
        /// </summary>
        /// <param name="parentMarsObject">父MarsSpiedObjectInfo</param>
        /// <param name="parentControl">父WinForm控件</param>
        /// <param name="targetHwnd">目标窗口句柄</param>
        private static void BuildChildrenMarsObjects(MarsSpiedObjectInfo parentMarsObject, Control parentControl, IntPtr targetHwnd)
        {
            if (parentMarsObject == null || parentControl == null) return;
            if (parentControl.Controls == null || parentControl.Controls.Count == 0) return;

            try
            {
                if (parentMarsObject.children == null)
                {
                    parentMarsObject.children = new List<MarsSpiedObjectInfo>();
                }

                foreach (Control childControl in parentControl.Controls)
                {
                    if (childControl == null) continue;

                    // 创建子对象的MarsSpiedObjectInfo
                    var childMarsObject = CreateMarsSpiedObjectInfo(childControl, targetHwnd);
                    if (childMarsObject == null) continue;

                    // 设置父子关系
                    childMarsObject.PegWindUUID = parentMarsObject.obj_uuid;
                    childMarsObject.Pegwindow = parentMarsObject;
                    
                    // 构建对象路径
                    childMarsObject.objectNamePath = BuildObjectNamePath(parentMarsObject, childControl);
                    childMarsObject.objectTypePath = BuildObjectTypePath(parentMarsObject, childControl);

                    // 添加到父对象的子对象列表
                    parentMarsObject.children.Add(childMarsObject);

                    // 递归构建子对象的子对象
                    BuildChildrenMarsObjects(childMarsObject, childControl, targetHwnd);
                }

                MarsLoggerSimple.Info("BuildChildrenMarsObjects", 
                    $"Built {parentMarsObject.children.Count} children for: {parentMarsObject.objectName}[{parentMarsObject.objectType}]");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildChildrenMarsObjects", 
                    $"Error building children MarsSpiedObjectInfo: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 构建对象名称路径
        /// </summary>
        /// <param name="parentMarsObject">父MarsSpiedObjectInfo</param>
        /// <param name="childControl">子控件</param>
        /// <returns>对象名称路径</returns>
        private static string BuildObjectNamePath(MarsSpiedObjectInfo parentMarsObject, Control childControl)
        {
            if (parentMarsObject == null || string.IsNullOrEmpty(parentMarsObject.objectNamePath))
            {
                return childControl.Name ?? "";
            }

            return $"{parentMarsObject.objectNamePath};{childControl.Name ?? ""}";
        }

        /// <summary>
        /// 构建对象类型路径
        /// </summary>
        /// <param name="parentMarsObject">父MarsSpiedObjectInfo</param>
        /// <param name="childControl">子控件</param>
        /// <returns>对象类型路径</returns>
        private static string BuildObjectTypePath(MarsSpiedObjectInfo parentMarsObject, Control childControl)
        {
            if (parentMarsObject == null || string.IsNullOrEmpty(parentMarsObject.objectTypePath))
            {
                return childControl.GetType().FullName;
            }

            return $"{parentMarsObject.objectTypePath};{childControl.GetType().FullName}";
        }

        /// <summary>
        /// 递归收集所有MarsSpiedObjectInfo对象（包括根节点和所有子节点）
        /// </summary>
        /// <param name="marsObject">当前MarsSpiedObjectInfo</param>
        /// <param name="allObjects">收集到的所有对象列表</param>
        private static void CollectAllMarsObjects(MarsSpiedObjectInfo marsObject, List<MarsSpiedObjectInfo> allObjects)
        {
            if (marsObject == null || allObjects == null) return;

            // 添加当前对象
            allObjects.Add(marsObject);

            // 递归添加所有子对象
            if (marsObject.children != null && marsObject.children.Count > 0)
            {
                foreach (var child in marsObject.children)
                {
                    CollectAllMarsObjects(child, allObjects);
                }
            }
        }

        /// <summary>
        /// 获取从指定控件到根控件的所有父控件路径
        /// </summary>
        /// <param name="control">起始控件</param>
        /// <returns>父控件列表（从根到当前控件）</returns>
        public static List<Control> GetParentChain(Control control)
        {
            var parentChain = new List<Control>();

            try
            {
                Control current = control;

                // 向上遍历到根
                while (current != null)
                {
                    parentChain.Insert(0, current); // 插入到开头，保证顺序从根到当前
                    current = current.Parent;
                }

                MarsLoggerSimple.Info("GetParentChain", 
                    $"Built parent chain with {parentChain.Count} controls");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetParentChain", 
                    $"Error building parent chain: {ex.Message}", ex);
            }

            return parentChain;
        }

        /// <summary>
        /// 检查控件是否是.NET Framework WinForm控件（非WPF嵌入）
        /// </summary>
        /// <param name="control">控件</param>
        /// <returns>是否为纯WinForm控件</returns>
        public static bool IsPureWinFormControl(Control control)
        {
            if (control == null) return false;

            try
            {
                // 检查控件类型是否来自System.Windows.Forms命名空间
                var controlType = control.GetType();
                return controlType.Namespace != null && 
                       controlType.Namespace.StartsWith("System.Windows.Forms", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
