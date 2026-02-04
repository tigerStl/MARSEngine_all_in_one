using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Inter.MQCenter.objectSpy;

namespace Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support
{
    /// <summary>
    /// MarsObjSpyForm的扩展方法，添加WPF支持
    /// 这个扩展方法可以在不修改原始MarsObjSpyForm的情况下添加WPF功能
    /// </summary>
    public static class MarsObjSpyFormExtensions
    {
        /// <summary>
        /// 加载WPF可视树到MarsObjSpyForm
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <param name="targetControlId">目标控件ID</param>
        public static void LoadWpfVisualTree(this MarsObjSpyForm spyForm, IntPtr targetControlId = default(IntPtr))
        {
            MarsObjSpyFormWpfIntegration.LoadWpfVisualTreeToSpyForm(spyForm, targetControlId);
        }

        /// <summary>
        /// 加载混合可视树（Windows Forms + WPF）
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <param name="targetControlId">目标控件ID</param>
        public static void LoadMixedVisualTree(this MarsObjSpyForm spyForm, IntPtr targetControlId = default(IntPtr))
        {
            MarsObjSpyFormWpfIntegration.LoadMixedVisualTree(spyForm, targetControlId);
        }

        /// <summary>
        /// 获取WPF可视树对象列表
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <returns>WPF可视树对象列表</returns>
        public static List<MarsSpiedObjectInfo> GetWpfVisualTreeObjects(this MarsObjSpyForm spyForm)
        {
            return WpfVisualTreeInspector.GetAllTopLevelWindowsAsMarsObjects();
        }

        /// <summary>
        /// 获取TreeView控件的安全访问方法
        /// 由于treeView1可能是私有字段，这里提供一个安全的访问方法
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <returns>TreeView控件</returns>
        public static System.Windows.Forms.TreeView GetTreeView(this MarsObjSpyForm spyForm)
        {
            try
            {
                // 使用反射获取treeView1字段
                var field = typeof(MarsObjSpyForm).GetField("treeView1", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    return field.GetValue(spyForm) as System.Windows.Forms.TreeView;
                }
                
                // 如果字段不存在，尝试获取属性
                var property = typeof(MarsObjSpyForm).GetProperty("treeView1", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (property != null)
                {
                    return property.GetValue(spyForm) as System.Windows.Forms.TreeView;
                }
                
                MarsLoggerSimple.Error("GetTreeView", "Could not find treeView1 field or property in MarsObjSpyForm");
                return null;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetTreeView", $"Error accessing TreeView: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 安全地调用reloadObjects方法
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <param name="objects">要加载的对象列表</param>
        public static void ReloadObjects(this MarsObjSpyForm spyForm, List<MarsSpiedObjectInfo> objects)
        {
            try
            {
                // 使用反射调用私有方法reloadObjects
                var method = typeof(MarsObjSpyForm).GetMethod("reloadObjects", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(spyForm, new object[] { objects });
                }
                else
                {
                    MarsLoggerSimple.Error("ReloadObjects", "Could not find reloadObjects method in MarsObjSpyForm");
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("ReloadObjects", $"Error calling reloadObjects: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查MarsObjSpyForm是否支持WPF功能
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <returns>是否支持WPF功能</returns>
        public static bool SupportsWpf(this MarsObjSpyForm spyForm)
        {
            try
            {
                // 检查是否有TreeView控件
                var treeView = spyForm.GetTreeView();
                return treeView != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取MarsObjSpyForm的版本信息
        /// </summary>
        /// <param name="spyForm">MarsObjSpyForm实例</param>
        /// <returns>版本信息字符串</returns>
        public static string GetVersion(this MarsObjSpyForm spyForm)
        {
            try
            {
                var assembly = typeof(MarsObjSpyForm).Assembly;
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}