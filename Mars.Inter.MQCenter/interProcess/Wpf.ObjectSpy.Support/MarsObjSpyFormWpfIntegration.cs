using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Inter.MQCenter.objectSpy;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// MarsObjSpyForm与WPF可视树检查器的集成类
    /// 提供在Windows Forms环境中使用WPF可视树检查器的方法
    /// </summary>
    public class MarsObjSpyFormWpfIntegration
    {
        /// <summary>
        /// 在MarsObjSpyForm中加载WPF可视树
        /// 这个方法可以直接替换或补充现有的reloadObjects方法
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <param name="targetControlId">目标控件ID（可选）</param>
        public static void LoadWpfVisualTreeToSpyForm(MarsObjSpyForm spyForm, IntPtr targetControlId = default(IntPtr))
        {
            if (spyForm == null) return;

            MarsLoggerSimple.logBegin("LoadWpfVisualTreeToSpyForm");

            try
            {
                // 方法1：直接加载到TreeView（推荐）
                LoadWpfTreeDirectlyToTreeView(spyForm, targetControlId);

                // 方法2：转换为MarsSpiedObjectInfo后加载（兼容现有代码）
                // LoadWpfTreeAsMarsObjects(spyForm, targetControlId);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("LoadWpfVisualTreeToSpyForm",
                    $"Error loading WPF visual tree to spy form: {ex.Message}", ex);
            }

            MarsLoggerSimple.logEnd("LoadWpfVisualTreeToSpyForm");
        }

        /// <summary>
        /// 方法1：直接将WPF可视树加载到TreeView中
        /// 这是最简单直接的方法
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <param name="targetControlId">目标控件ID</param>
        private static void LoadWpfTreeDirectlyToTreeView(MarsObjSpyForm spyForm, IntPtr targetControlId)
        {
            try
            {
                // 使用WpfVisualTreeInspector直接加载到TreeView
                var treeView = MarsObjSpyFormExtensions.GetTreeView(spyForm);
                if (treeView != null)
                {
                    // 获取WPF可视树
                    var wpfWindows = WpfVisualTreeInspector.GetAllTopLevelWindows();
                    // 转换WPF对象为Mars对象
                    var marsObjects = WpfVisualTreeAdapter.ConvertWpfTreeToMarsObjects(wpfWindows);
                    WpfVisualTreeInspector.LoadWpfTreeToTreeView(treeView, marsObjects, targetControlId);
                    spyForm.SetAllObjects(marsObjects);
                }

                MarsLoggerSimple.Info("LoadWpfTreeDirectlyToTreeView",
                   "Successfully loaded WPF visual tree directly to TreeView");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("LoadWpfTreeDirectlyToTreeView",
                    $"Error loading WPF tree directly to TreeView: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 方法2：将WPF可视树转换为MarsSpiedObjectInfo后加载
        /// 这种方法与现有的reloadObjects方法完全兼容
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <param name="targetControlId">目标控件ID</param>
        private static void LoadWpfTreeAsMarsObjects(MarsObjSpyForm spyForm, IntPtr targetControlId)
        {
            try
            {
                // 获取WPF可视树并转换为MarsSpiedObjectInfo格式
                var marsObjects = WpfVisualTreeInspector.GetAllTopLevelWindowsAsMarsObjects();

                if (marsObjects != null && marsObjects.Count > 0)
                {
                    // 使用现有的reloadObjects方法加载
                    MarsObjSpyFormExtensions.ReloadObjects(spyForm, marsObjects);

                    MarsLoggerSimple.Info("LoadWpfTreeAsMarsObjects",
                        $"Successfully loaded {marsObjects.Count} WPF objects as Mars objects");
                }
                else
                {
                    MarsLoggerSimple.Info("LoadWpfTreeAsMarsObjects",
                        "No WPF objects found to load");
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("LoadWpfTreeAsMarsObjects",
                    $"Error loading WPF tree as Mars objects: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 混合加载：同时加载Windows Forms控件和WPF控件
        /// 这样可以显示完整的应用程序界面结构
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <param name="targetControlId">目标控件ID</param>
        public static void LoadMixedVisualTree(MarsObjSpyForm spyForm, IntPtr targetControlId = default(IntPtr))
        {
            if (spyForm == null) return;

            MarsLoggerSimple.logBegin("LoadMixedVisualTree");

            try
            {
                // 清空现有TreeView
                var treeView = MarsObjSpyFormExtensions.GetTreeView(spyForm);
                if (treeView != null)
                {
                    treeView.BeginUpdate();
                    treeView.Nodes.Clear();
                }

                // 1. 加载Windows Forms控件（使用现有方法）
                var winformObjects = MarsWinformSpy.getCurrentAllObjects(null, false, false);
                if (winformObjects != null && winformObjects.Count > 0)
                {
                    foreach (var obj in winformObjects)
                    {
                        var node = CreateTreeNodeFromMarsObject(obj, targetControlId);
                        if (node != null && treeView != null)
                        {
                            treeView.Nodes.Add(node);
                        }
                    }
                }

                // 2. 加载WPF控件
                var wpfObjects = WpfVisualTreeInspector.GetAllTopLevelWindowsAsMarsObjects();
                if (wpfObjects != null && wpfObjects.Count > 0)
                {
                    foreach (var obj in wpfObjects)
                    {
                        var node = CreateTreeNodeFromMarsObject(obj, targetControlId);
                        if (node != null)
                        {
                            // 为WPF对象添加特殊标识
                            node.Text = "[WPF] " + node.Text;
                            node.ForeColor = System.Drawing.Color.Purple;
                            if (treeView != null)
                            {
                                treeView.Nodes.Add(node);
                            }
                        }
                    }
                }

                MarsLoggerSimple.Info("LoadMixedVisualTree",
                    $"Loaded {winformObjects?.Count ?? 0} WinForm objects and {wpfObjects?.Count ?? 0} WPF objects");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("LoadMixedVisualTree",
                    $"Error loading mixed visual tree: {ex.Message}", ex);
            }
            finally
            {
                if (spyForm.GetTreeView() != null)
                {
                    spyForm.GetTreeView().EndUpdate();
                }
            }

            MarsLoggerSimple.logEnd("LoadMixedVisualTree");
        }

        /// <summary>
        /// 创建TreeNode（复制自MarsObjSpyForm的CreateNodeFromObjInfo方法）
        /// </summary>
        /// <param name="marsObject">MarsSpiedObjectInfo对象</param>
        /// <param name="targetControlId">目标控件ID</param>
        /// <returns>TreeNode对象</returns>
        private static TreeNode CreateTreeNodeFromMarsObject(MarsSpiedObjectInfo marsObject, IntPtr targetControlId)
        {
            if (marsObject == null) return null;

            try
            {
                var node = new TreeNode(marsObject.getDisplayId() ?? "N/A");
                node.Tag = marsObject;

                // 设置节点样式
                if (!marsObject.isVisible)
                {
                    node.ForeColor = System.Drawing.Color.Red;
                    node.NodeFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Italic);
                }

                // 如果是目标控件，高亮显示
                if (targetControlId != IntPtr.Zero && marsObject.referenceToObj != null)
                {
                    // 对于WPF对象，这里可能需要特殊处理
                    // 因为WPF对象没有Handle属性
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
        /// 使用示例和测试方法
        /// </summary>
        public static class UsageExamples
        {
            /// <summary>
            /// 示例：在MarsObjSpyForm中显示WPF可视树
            /// </summary>
            public static void ExampleLoadWpfTree()
            {
                try
                {
                    // 获取或创建MarsObjSpyForm实例
                    var spyForm = MarsObjSpyForm.getInstance(null);

                    // 方法1：直接加载WPF可视树
                    spyForm.LoadWpfVisualTree();

                    // 方法2：加载混合可视树（Windows Forms + WPF）
                    // spyForm.LoadMixedVisualTree();

                    // 显示窗体
                    spyForm.Show();
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Error("ExampleLoadWpfTree",
                        $"Error in example: {ex.Message}", ex);
                }
            }

            /// <summary>
            /// 示例：获取WPF对象并手动处理
            /// </summary>
            public static void ExampleGetWpfObjects()
            {
                try
                {
                    // 获取WPF可视树对象
                    var wpfObjects = WpfVisualTreeInspector.GetAllTopLevelWindowsAsMarsObjects();

                    MarsLoggerSimple.Info("ExampleGetWpfObjects",
                        $"Found {wpfObjects.Count} WPF objects");

                    // 遍历WPF对象
                    foreach (var obj in wpfObjects)
                    {
                        MarsLoggerSimple.Info("ExampleGetWpfObjects",
                            $"WPF Object: {obj.getDisplayId()} - {obj.objectType}");

                        // 可以进一步处理每个对象
                        ProcessWpfObject(obj);
                    }
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Error("ExampleGetWpfObjects",
                        $"Error in example: {ex.Message}", ex);
                }
            }

            /// <summary>
            /// 处理单个WPF对象
            /// </summary>
            /// <param name="wpfObject">WPF对象</param>
            private static void ProcessWpfObject(MarsSpiedObjectInfo wpfObject)
            {
                // 这里可以添加特定的WPF对象处理逻辑
                // 例如：检查对象类型、提取特定属性等

                if (wpfObject.objectType.Contains("Button"))
                {
                    MarsLoggerSimple.Info("ProcessWpfObject",
                        $"Found WPF Button: {wpfObject.objectName} - {wpfObject.Text}");
                }
                else if (wpfObject.objectType.Contains("TextBox"))
                {
                    MarsLoggerSimple.Info("ProcessWpfObject",
                        $"Found WPF TextBox: {wpfObject.objectName} - {wpfObject.Text}");
                }
            }
        }
    }
}