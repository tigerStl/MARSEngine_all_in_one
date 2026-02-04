using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using static Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support.WpfVisualTreeInspector;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// WPF可视树适配器，用于将WPF可视对象转换为与Windows Forms TreeView兼容的MarsSpiedObjectInfo格式
    /// </summary>
    public class WpfVisualTreeAdapter
    {
        /// <summary>
        /// 将WpfVisualObjectInfo转换为MarsSpiedObjectInfo，使其能够放入Windows Forms的TreeView中
        /// </summary>
        /// <param name="wpfObject">WPF可视对象信息</param>
        /// <returns>转换后的MarsSpiedObjectInfo对象</returns>
        public static MarsSpiedObjectInfo ConvertToMarsSpiedObjectInfo(WpfVisualObjectInfo wpfObject)
        {
            if (wpfObject == null) return null;

            try
            {
                var marsObject = new MarsSpiedObjectInfo
                {
                    // 基本属性映射
                    objectName = wpfObject.Name,
                    objectNamePath = wpfObject.NamePath,
                    objectType = wpfObject.Type,
                    objectTypePath = wpfObject.TypePath,
                    Text = wpfObject.Text,
                    isVisible = wpfObject.IsVisible,
                    isEnabled = wpfObject.IsEnabled, // 添加启用状态
                    isChildWindow = false, // WPF对象默认为false
                    isOwnedWindow = false, // WPF对象默认为false
                    
                    // 位置信息映射
                    x = wpfObject.Position.X,
                    y = wpfObject.Position.Y,
                    w = wpfObject.Position.Width,
                    h = wpfObject.Position.Height,
                    relatedX = wpfObject.Position.X,
                    relatedY = wpfObject.Position.Y,
                    
                    // 索引和层级信息
                    index = wpfObject.Index,
                    zorder = wpfObject.ZOrder,
                    
                    // 子节点总数信息
                    allChildrenCount = wpfObject.AllChildrenCount,
                    
                    // Snoop风格信息映射
                    dependencyProperties = wpfObject.DependencyProperties,
                    events = wpfObject.Events,
                    style = wpfObject.Style,
                    template = wpfObject.Template,
                    resources = wpfObject.Resources,
                    bindings = wpfObject.Bindings,
                    triggers = wpfObject.Triggers,
                    renderInfo = wpfObject.RenderInfo,
                    layoutInfo = wpfObject.LayoutInfo,
                    inputInfo = wpfObject.InputInfo,
                    focusInfo = wpfObject.FocusInfo,
                    visibilityInfo = wpfObject.VisibilityInfo,
                    transformInfo = wpfObject.TransformInfo,
                    animationInfo = wpfObject.AnimationInfo,
                    contextInfo = wpfObject.ContextInfo,
                    debugInfo = wpfObject.DebugInfo,
                    
                    // 控制类型映射
                    controlMarsType = GetMarsControlType(wpfObject.Type),
                    
                    // 设置对象引用（如果是WPF元素）
                    referenceToObj = GetWpfElementReference(wpfObject),
                    
                    // 初始化子对象列表
                    children = new List<MarsSpiedObjectInfo>()
                };

                // 递归转换子对象
                if (wpfObject.Children != null && wpfObject.Children.Count > 0)
                {
                    foreach (var child in wpfObject.Children)
                    {
                        var childMarsObject = ConvertToMarsSpiedObjectInfo(child);
                        if (childMarsObject != null)
                        {
                            marsObject.children.Add(childMarsObject);
                        }
                    }
                }

                return marsObject;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ConvertToMarsSpiedObjectInfo", 
                    $"Error converting WPF object to MarsSpiedObjectInfo: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 将WPF可视树转换为MarsSpiedObjectInfo列表，用于直接加载到TreeView
        /// </summary>
        /// <param name="wpfWindows">WPF顶级窗口列表</param>
        /// <returns>转换后的MarsSpiedObjectInfo列表</returns>
        public static List<MarsSpiedObjectInfo> ConvertWpfTreeToMarsObjects(List<WpfVisualObjectInfo> wpfWindows)
        {
            var marsObjects = new List<MarsSpiedObjectInfo>();

            try
            {
                foreach (var window in wpfWindows)
                {
                    var marsObject = ConvertToMarsSpiedObjectInfo(window);
                    if (marsObject != null)
                    {
                        marsObjects.Add(marsObject);
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ConvertWpfTreeToMarsObjects", 
                    $"Error converting WPF tree to Mars objects: {ex.Message}", ex);
            }

            return marsObjects;
        }

        /// <summary>
        /// 获取Mars控制类型标识
        /// </summary>
        /// <param name="wpfType">WPF类型名称</param>
        /// <returns>Mars控制类型</returns>
        private static string GetMarsControlType(string wpfType)
        {
            if (string.IsNullOrEmpty(wpfType)) return "wpfunknown";

            // 根据WPF类型映射到Mars控制类型
            if (wpfType.Contains("Window"))
                return "wpfwindow";
            else if (wpfType.Contains("Button"))
                return "wpfbutton";
            else if (wpfType.Contains("TextBox"))
                return "wpftextbox";
            else if (wpfType.Contains("Label"))
                return "wpflabel";
            else if (wpfType.Contains("CheckBox"))
                return "wpfcheckbox";
            else if (wpfType.Contains("RadioButton"))
                return "wpfradiobutton";
            else if (wpfType.Contains("ComboBox"))
                return "wpfcombobox";
            else if (wpfType.Contains("ListBox"))
                return "wpflistbox";
            else if (wpfType.Contains("DataGrid"))
                return "wpfdatagrid";
            else if (wpfType.Contains("TreeView"))
                return "wpftreeview";
            else if (wpfType.Contains("Menu"))
                return "wpfmenu";
            else if (wpfType.Contains("ToolBar"))
                return "wpftoolbar";
            else if (wpfType.Contains("StatusBar"))
                return "wpfstatusbar";
            else if (wpfType.Contains("TabControl"))
                return "wpftabcontrol";
            else if (wpfType.Contains("GroupBox"))
                return "wpfgroupbox";
            else if (wpfType.Contains("Panel"))
                return "wpfpanel";
            else if (wpfType.Contains("Grid"))
                return "wpfgrid";
            else if (wpfType.Contains("StackPanel"))
                return "wpfstackpanel";
            else if (wpfType.Contains("Canvas"))
                return "wpfcanvas";
            else if (wpfType.Contains("DockPanel"))
                return "wpfdockpanel";
            else if (wpfType.Contains("WrapPanel"))
                return "wpfwrappanel";
            else if (wpfType.Contains("Border"))
                return "wpfborder";
            else if (wpfType.Contains("ScrollViewer"))
                return "wpfscrollviewer";
            else if (wpfType.Contains("Image"))
                return "wpfimage";
            else if (wpfType.Contains("TextBlock"))
                return "wpftextblock";
            else
                return "wpfcontrol";
        }

        /// <summary>
        /// 获取WPF元素引用（如果可能的话）
        /// </summary>
        /// <param name="wpfObject">WPF可视对象信息</param>
        /// <returns>WPF元素引用或null</returns>
        private static object GetWpfElementReference(WpfVisualObjectInfo wpfObject)
        {
            if (wpfObject == null)
            {
                return null;
            }

            try
            {
                // 从RefObject获取WPF元素引用
                return wpfObject.RefObject;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetWpfElementReference", 
                    $"Error getting WPF element reference: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 创建TreeNode用于TreeView显示
        /// </summary>
        /// <param name="marsObject">MarsSpiedObjectInfo对象</param>
        /// <param name="targetControlId">目标控件ID（用于高亮显示）</param>
        /// <returns>TreeNode对象</returns>
        public static System.Windows.Forms.TreeNode CreateTreeNodeFromMarsObject(MarsSpiedObjectInfo marsObject, IntPtr targetControlId = default(IntPtr))
        {
            if (marsObject == null) return null;

            try
            {
                // 获取类别标签
                var typeLabel = GetTypeLabel(marsObject.objectType);
                
                // 构建显示文本：格式为 (all children count)-[Type]:Object Name
                var objectName = marsObject.getDisplayId() ?? "N/A";
                var displayText = $"({marsObject.allChildrenCount})-[{typeLabel}]:{objectName}";
                
                var node = new System.Windows.Forms.TreeNode(displayText);
                node.Tag = marsObject;

                // 设置节点样式
                if (!marsObject.isVisible || !marsObject.isEnabled)
                {
                    // 如果不可见或禁用，显示为灰色
                    node.ForeColor = System.Drawing.Color.Gray;
                    node.NodeFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Italic);
                }
                else if (!marsObject.isVisible)
                {
                    // 如果只是不可见，显示为红色
                    node.ForeColor = System.Drawing.Color.Red;
                    node.NodeFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Italic);
                }
                else if (!marsObject.isEnabled)
                {
                    // 如果只是禁用，显示为深灰色
                    node.ForeColor = System.Drawing.Color.DarkGray;
                    node.NodeFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Regular);
                }

                // 如果是目标控件，高亮显示
                if (targetControlId != IntPtr.Zero && marsObject.referenceToObj != null)
                {
                    // 这里需要根据实际情况判断是否为目标控件
                    // 由于WPF对象没有Handle，这里暂时跳过
                }

                // 递归创建子节点
                if (marsObject.children != null && marsObject.children.Count > 0)
                {
                    foreach (var child in marsObject.children)
                    {
                        var childNode = CreateTreeNodeFromMarsObject(child, targetControlId);
                        if (childNode != null)
                        {
                            node.Nodes.Add(childNode);
                        }
                    }
                }

                return node;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CreateTreeNodeFromMarsObject", 
                    $"Error creating TreeNode: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 根据WPF类型获取简化的类别标签
        /// </summary>
        /// <param name="fullTypeName">完整的类型名称</param>
        /// <returns>简化的类别标签</returns>
        private static string GetTypeLabel(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return "unknown";

            try
            {
                // 提取类型名称（去掉命名空间）
                var typeName = fullTypeName.Split('.').LastOrDefault() ?? fullTypeName;
                
                // 转换为小写并处理特殊情况
                var label = typeName.ToLower();
                
                // 特殊类型映射
                switch (label)
                {
                    case "window":
                        return "window";
                    case "button":
                        return "button";
                    case "textbox":
                        return "textbox";
                    case "textblock":
                        return "textblock";
                    case "label":
                        return "label";
                    case "checkbox":
                        return "checkbox";
                    case "radiobutton":
                        return "radio";
                    case "combobox":
                        return "combo";
                    case "listbox":
                        return "listbox";
                    case "listview":
                        return "listview";
                    case "treeview":
                        return "treeview";
                    case "datagrid":
                        return "datagrid";
                    case "grid":
                        return "grid";
                    case "stackpanel":
                        return "stack";
                    case "wrappanel":
                        return "wrap";
                    case "dockpanel":
                        return "dock";
                    case "canvas":
                        return "canvas";
                    case "border":
                        return "border";
                    case "groupbox":
                        return "group";
                    case "tabcontrol":
                        return "tab";
                    case "tabitem":
                        return "tabitem";
                    case "menuitem":
                        return "menu";
                    case "toolbar":
                        return "toolbar";
                    case "statusbar":
                        return "status";
                    case "progressbar":
                        return "progress";
                    case "slider":
                        return "slider";
                    case "scrollviewer":
                        return "scroll";
                    case "image":
                        return "image";
                    case "mediaelement":
                        return "media";
                    case "webrowser":
                        return "web";
                    case "frame":
                        return "frame";
                    case "page":
                        return "page";
                    case "usercontrol":
                        return "usercontrol";
                    case "contentcontrol":
                        return "content";
                    case "headeredcontentcontrol":
                        return "header";
                    case "itemscontrol":
                        return "items";
                    case "control":
                        return "control";
                    case "frameworkelement":
                        return "element";
                    case "uielement":
                        return "ui";
                    case "visual":
                        return "visual";
                    default:
                        // 对于未知类型，返回简化的类型名
                        return typeName.Length > 10 ? typeName.Substring(0, 10) : typeName;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetTypeLabel", 
                    $"Error getting type label for '{fullTypeName}': {ex.Message}", ex);
                return "unknown";
            }
        }

        /// <summary>
        /// 将WPF可视树直接加载到TreeView中
        /// </summary>
        /// <param name="treeView">目标TreeView控件</param>
        /// <param name="wpfWindows">WPF顶级窗口列表</param>
        /// <param name="targetControlId">目标控件ID（可选）</param>
        public static void LoadWpfTreeToTreeView(System.Windows.Forms.TreeView treeView, List<MarsSpiedObjectInfo> marsObjects, IntPtr targetControlId = default(IntPtr))
        {
            if (treeView == null || marsObjects == null) return;

            try
            {
                treeView.BeginUpdate();
                treeView.Nodes.Clear();

                // 创建TreeNode并添加到TreeView
                foreach (var marsObject in marsObjects)
                {
                    var node = CreateTreeNodeFromMarsObject(marsObject, targetControlId);
                    if (node != null)
                    {
                        treeView.Nodes.Add(node);
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("LoadWpfTreeToTreeView", 
                    $"Error loading WPF tree to TreeView: {ex.Message}", ex);
            }
            finally
            {
                treeView.EndUpdate();
            }
        }

        /// <summary>
        /// 扩展WpfVisualObjectInfo类，添加WPF元素引用
        /// </summary>
        public class WpfVisualObjectInfoWithReference : WpfVisualObjectInfo
        {
            /// <summary>
            /// WPF元素引用
            /// </summary>
            public DependencyObject WpfElement { get; set; }

            /// <summary>
            /// 从WpfVisualObjectInfo创建带引用的版本
            /// </summary>
            /// <param name="original">原始WpfVisualObjectInfo</param>
            /// <param name="wpfElement">WPF元素引用</param>
            /// <returns>带引用的WpfVisualObjectInfo</returns>
            public static WpfVisualObjectInfoWithReference FromOriginal(WpfVisualObjectInfo original, DependencyObject wpfElement)
            {
                if (original == null) return null;

                var withRef = new WpfVisualObjectInfoWithReference
                {
                    Name = original.Name,
                    NamePath = original.NamePath,
                    Type = original.Type,
                    TypePath = original.TypePath,
                    Position = original.Position,
                    Text = original.Text,
                    IsVisible = original.IsVisible,
                    IsEnabled = original.IsEnabled,
                    AutomationId = original.AutomationId,
                    Uid = original.Uid,
                    Tag = original.Tag,
                    Index = original.Index,
                    ZOrder = original.ZOrder,
                    WpfElement = wpfElement
                };

                // 复制子对象
                foreach (var child in original.Children)
                {
                    withRef.Children.Add(child);
                }

                return withRef;
            }
        }

        /// <summary>
        /// 将带引用的WpfVisualObjectInfo转换为MarsSpiedObjectInfo
        /// </summary>
        /// <param name="wpfObjectWithRef">带引用的WPF可视对象信息</param>
        /// <returns>转换后的MarsSpiedObjectInfo对象</returns>
        public static MarsSpiedObjectInfo ConvertToMarsSpiedObjectInfoWithReference(WpfVisualObjectInfoWithReference wpfObjectWithRef)
        {
            if (wpfObjectWithRef == null) return null;

            var marsObject = ConvertToMarsSpiedObjectInfo(wpfObjectWithRef);
            if (marsObject != null)
            {
                // 设置WPF元素引用
                marsObject.referenceToObj = wpfObjectWithRef.WpfElement;
            }

            return marsObject;
        }
    }
}