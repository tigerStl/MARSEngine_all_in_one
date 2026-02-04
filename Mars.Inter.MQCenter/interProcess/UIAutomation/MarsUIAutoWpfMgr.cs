using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;

namespace Mars.Inter.MQCenter.interProcess.UIAutomation
{
    public class MarsUIAutoWpfMgr
    {

        public static List<MarsSpiedObjInfoAI> GetAllUIElement()
        {
            MarsLoggerSimple.logBegin("GetAllUIElement");
            // 获取桌面窗口的根元素
            AutomationElement rootElement = AutomationElement.RootElement;

            // 获取所有子控件信息
            var childrenInfo = UIAutomationHelper.GetAllChildren(rootElement);
            MarsLoggerSimple.logEnd("GetAllUIElement", $"totalGet|{childrenInfo?.Count}");
            return childrenInfo;
        }
    }

    public class UIAutomationHelper
    {
        // 遍历所有子控件
        public static List<MarsSpiedObjInfoAI> GetAllChildren(AutomationElement root)
        {
            List<MarsSpiedObjInfoAI> childrenInfo = new List<MarsSpiedObjInfoAI>();
            WalkControlElements(root, "", "", childrenInfo);
            return childrenInfo;
        }

        // 递归遍历控件
        private static void WalkControlElements(AutomationElement element, string namePath, string typePath, List<MarsSpiedObjInfoAI> childrenInfo)
        {
            bool isWindowPatternAvailable = element.TryGetCurrentPattern(WindowPattern.Pattern, out _);
            // 获取控件的基本信息
            var objInfo = new MarsSpiedObjInfoAI
            {
                objectName = element.Current.Name,
                objectType = element.Current.LocalizedControlType,
                objectNamePath = namePath + "/" + element.Current.Name,
                objectTypePath = typePath + "/" + element.Current.LocalizedControlType,
                Text = element.Current.Name,
                isChildWindow = isWindowPatternAvailable, // 使用 TryGetCurrentPattern 的结果
                isOwnedWindow = isWindowPatternAvailable && element.Current.IsOffscreen,
                objectRect = new System.Drawing.Rectangle(
                    (int)element.Current.BoundingRectangle.X,
                    (int)element.Current.BoundingRectangle.Y,
                    (int)element.Current.BoundingRectangle.Width,
                    (int)element.Current.BoundingRectangle.Height)
            };
            MarsLoggerSimple.Info("WalkControlElements", objInfo.ToString());

            // 如果是 Table 控件，获取列信息
            if (element.Current.LocalizedControlType == "table")
            {
                objInfo.DataTableColumns = GetTableColumns(element);
            }

            // 如果是 ComboBox 控件，获取列表项
            if (element.Current.LocalizedControlType == "combo box")
            {
                objInfo.ListItems = GetComboBoxItems(element);
            }

            childrenInfo.Add(objInfo);

            // 递归遍历子控件
            var children = element.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
            foreach (AutomationElement child in children)
            {
                WalkControlElements(child, objInfo.objectNamePath, objInfo.objectTypePath, childrenInfo);
            }
        }

        // 获取 Table 控件的列信息
        private static List<MarsObjectColumnInfo> GetTableColumns(AutomationElement tableElement)
        {
            var columns = new List<MarsObjectColumnInfo>();

            // 获取 Table 控件的 Grid Pattern
            if (tableElement.TryGetCurrentPattern(GridPattern.Pattern, out var gridPatternObj))
            {
                var gridPattern = (GridPattern)gridPatternObj;

                // 获取列头
                if (tableElement.TryGetCurrentPattern(TablePattern.Pattern, out var tablePatternObj))
                {
                    var tablePattern = (TablePattern)tablePatternObj;
                    var columnHeaders = tablePattern.Current.GetColumnHeaders();

                    foreach (AutomationElement header in columnHeaders)
                    {
                        columns.Add(new MarsObjectColumnInfo
                        {
                            ColumnName = header.Current.Name
                        });
                    }
                }
            }

            return columns;
        }

        // 获取 ComboBox 控件的列表项
        private static List<string> GetComboBoxItems(AutomationElement comboBoxElement)
        {
            var items = new List<string>();

            // 展开 ComboBox
            if (comboBoxElement.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandCollapsePatternObj))
            {
                var expandCollapsePattern = (ExpandCollapsePattern)expandCollapsePatternObj;
                expandCollapsePattern.Expand();

                // 获取列表项
                var listItems = comboBoxElement.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
                foreach (AutomationElement item in listItems)
                {
                    items.Add(item.Current.Name);
                }

                // 收起 ComboBox
                expandCollapsePattern.Collapse();
            }

            return items;
        }
    }

}
