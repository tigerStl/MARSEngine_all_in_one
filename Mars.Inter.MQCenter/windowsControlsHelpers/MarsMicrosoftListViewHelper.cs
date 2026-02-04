using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Mars.message.Inter.MQCenter.objectTypeMapping;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    internal class MarsMicrosoftListViewHelper
    {
        /// <summary>
        /// 获得listbox， listview 和treeview等相关的操作信息
        /// </summary>
        /// <param name="c"></param>
        /// <param name="pt"></param>
        /// <param name="strMarsObjectType"></param>
        /// <param name="strError"></param>
        /// <param name="strText"></param>
        /// <param name="lstOfColumns"></param>
        /// <param name="strHitColumnCaption"></param>
        /// <param name="isClickOnHeader"></param>
        /// <param name="isUsePath"></param>
        /// <returns></returns>
        internal bool GetListViewInfo(Control c, System.Drawing.Point pt, 
            string strMarsObjectType, 
            ref string strError, ref string strText, 
            List<string> lstOfColumns, ref string strHitColumnCaption, 
            ref bool isClickOnHeader, ref bool isUsePath)
        {
            MarsLoggerSimple.logBegin("GetListViewInfo", $"{c?.GetType()}|at|{pt}");
            bool isOk = false;
            DetectClick(c, pt.X, pt.Y, strMarsObjectType, ref isOk, ref strError, ref strText, lstOfColumns, ref strHitColumnCaption, ref isClickOnHeader,ref isUsePath);
            MarsLoggerSimple.logEnd("GetListViewInfo", $"{strText}|{lstOfColumns.ToArray()}|{strHitColumnCaption}|isClickOnHeader|{isClickOnHeader}");
            return isOk;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="control"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strText"></param>
        /// <param name="lstOfColumns"></param>
        /// <param name="strHitColumnCaption">对于listview，是当前column的header caption，对于Treeview，返回node的path</param>
        /// <param name="isClickOnHeader"></param>
        public void DetectClick(Control control, int x, int y,
            string strMarsObjectType, 
            ref bool isOk, ref string strError, 
            ref string strText, List<string> lstOfColumns, 
            ref string strHitColumnCaption, ref bool isClickOnHeader, ref bool isUsePath)
        {
            if (control is ListView listView)
            {
                isUsePath = false;
                HandleListViewClick(listView, x, y, ref isOk, ref strError, ref strText, lstOfColumns, ref strHitColumnCaption, ref isClickOnHeader);
            }
            else if (control is ListBox listBox)
            {
                isUsePath = false;
                HandleListBoxClick(listBox, x, y, ref isOk, ref strError, ref strText, lstOfColumns, ref strHitColumnCaption, ref isClickOnHeader);
            }
            else if (control is TreeView treeView)
            {
                isUsePath = true;
                HandleTreeViewClick(treeView, x, y, ref isOk, ref strError, ref strText, lstOfColumns, ref strHitColumnCaption, ref isClickOnHeader);
            }
            else
            {
                switch (strMarsObjectType)
                {
                    case MarsObjectTypeMappings.cnst_swf_Tree:
                        /// 需要处理不同第三开发模式的tree，目前假定只有
                        /// 
                        isOk = (new MarsTreeViewOperation()).GetTreeNodeInfoForRecordAndReplayByPoint(control, new System.Drawing.Point(x, y), ref strText, ref strHitColumnCaption, ref strError);
                        isUsePath = isOk ? true : false;
                        return;
                    case MarsObjectTypeMappings.cnst_swf_ListView:
                        break;
                    default:
                        isOk = false;
                        break;
                }
                isOk = false;
                MarsLoggerSimple.Error("DetectClick", strError = "Control is not ListView, ListBox, or TreeView.");
            }
        }

        private  void HandleListViewClick(ListView listView, int x, int y, ref bool isOk, ref string strError, ref string strText, List<string> lstOfColumns,
            ref string strHitColumnCaption, ref bool isClickOnHeader)
        {
            foreach (ColumnHeader item in listView.Columns)
            {                
                lstOfColumns.Add(item.Text);
            }
            System.Drawing.Point pt = listView.PointToClient(new System.Drawing.Point(x, y));
            var hitTest = listView.HitTest(pt);
            isClickOnHeader = false;
            if (hitTest.Item != null)
            {
                // 点击了列表项
                strText = hitTest.Item.Text;
                isOk = true;
                if (hitTest.SubItem != null)
                {
                    var columnIndex = hitTest.Item.SubItems.IndexOf(hitTest.SubItem);
                    strHitColumnCaption = hitTest.SubItem.Text;                    
                }
                
            }
            else if (x >= 0 && y >= 0 && listView.View == View.Details)
            {
                // 检查是否点击了列头
                int columnIndex = GetClickedColumnIndex(listView, x, y, ref strError);
                if (columnIndex >= 0)
                {
                    isClickOnHeader = true;
                    isOk = true;

                    //Console.WriteLine($"Header Clicked: Column {columnIndex}");
                    SimulateHeaderClick(listView, columnIndex);
                }
                else
                {
                    isOk = false;
                    strError = $"can't find column head from point|{pt}";
                }
            }
            else
            {
                isOk = false;
                strError = $"can't find item or header from point(client) {pt}";
            }
        }

        private void HandleListBoxClick(ListBox listBox, int x, int y, ref bool isOk, ref string strError, ref string strText, List<string> lstOfColumns, ref string strHitColumnCaption, ref bool isClickOnHeader)
        {
            int index = listBox.IndexFromPoint(x, y);
            isClickOnHeader = false;
            if (index >= 0)
            {
                strText = listBox.Items[index]?.ToString();                
            }
            else
            {
                strError = $"Please click at a validate row";
                MarsLoggerSimple.Error("HandleListBoxClick", strError);
            }
        }

       /// <summary>
       /// 获得点击treenode的信息
       /// </summary>
       /// <param name="treeView"></param>
       /// <param name="x"></param>
       /// <param name="y"></param>
       /// <param name="isOk"></param>
       /// <param name="strError"></param>
       /// <param name="strText"></param>
       /// <param name="lstOfColumns"></param>
       /// <param name="nodePath"></param>
       /// <param name="isClickOnHeader"></param>
        private void HandleTreeViewClick(TreeView treeView, int x, int y, ref bool isOk, ref string strError, ref string strText, 
            List<string> lstOfColumns, ref string nodePath, ref bool isClickOnHeader)
        {
            TreeNode node = treeView.GetNodeAt(x, y);
            if (node != null)
            {
                strText = node.Text;
                nodePath = GetNodePath(node);
                isOk = true;
            }
            else
            {
                strError = $"Please click at a validate row";
                MarsLoggerSimple.Error("HandleTreeViewClick", strError);
                isOk = false;
            }
        }

        private string GetNodePath(TreeNode node)
        {
            // 递归获取节点的路径
            if (node.Parent == null)
            {
                return node.Text;
            }
            return GetNodePath(node.Parent) + "/" + node.Text;
        }

        private int GetClickedColumnIndex(ListView listView, int x, int y,ref string strError             )
        {
            try
            {
                PropertyInfo headerRectProp = typeof(ListView).GetProperty("HeaderRect", BindingFlags.NonPublic | BindingFlags.Instance);
                if (headerRectProp != null)
                {
                    var headerRect = (Rectangle)headerRectProp.GetValue(listView);
                    if (headerRect.Contains(x, y))
                    {
                        int columnWidth = 0;
                        for (int i = 0; i < listView.Columns.Count; i++)
                        {
                            columnWidth += listView.Columns[i].Width;
                            if (x <= columnWidth)
                            {
                                return i;
                            }
                        }
                    }
                }
                strError = $"Point is not inside of a column header";
                MarsLoggerSimple.Error("GetClickedColumnIndex", strError);
                return -1;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetClickedColumnIndex", strError = ex.Message, ex);
                return -1;
            }            
        }

        private void SimulateHeaderClick(ListView listView, int columnIndex)
        {
            try
            {
                MethodInfo onColumnClickMethod = typeof(ListView).GetMethod("OnColumnClick", BindingFlags.NonPublic | BindingFlags.Instance);
                if (onColumnClickMethod != null)
                {
                    ColumnClickEventArgs args = new ColumnClickEventArgs(columnIndex);
                    onColumnClickMethod.Invoke(listView, new object[] { args });
                    Console.WriteLine($"Simulated Header Click on Column {columnIndex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error simulating header click: {ex.Message}");
            }
        }
    }
}
