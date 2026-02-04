using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;

namespace Mars.message.Inter.MQCenter.keywordOperation
{
    internal abstract class ClickPopupMenuItem
    {
        internal abstract bool PerformClickPopupMenuItem(object c, string strParameter, string strData, string strPegName, string strObjName, ref string strError, ref string strAdv, ref string strStack);
        internal abstract bool PerformClickPopupMenuItemFromPopupbase(object c, string strParameter, string strData, string strPegName, string strObjName, ref string strError, ref string strAdv, ref string strStack);
    }

    internal class ClickPopupMenuItemForInfragistics : ClickPopupMenuItem
    {
        internal override bool PerformClickPopupMenuItemFromPopupbase(object c, string strParameter, string strData, string strPegName, string strObjName, ref string strError, ref string strAdv, ref string strStack)
        {//
            // for Infragistics.Win.UltraWinToolbars.PopupControlBase
            simpleLog.MarsLoggerSimple.logBegin("PerformClickPopupMenuItemFromPopupbase", "for Infragistics.Win.UltraWinToolbars.PopupControlBase");
            try
            {
                var oControls = ReflectorForCSharp.GetMember(c, "Controls");
                if ((oControls == null))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "no Controls in Infragistics.Win.UltraWinToolbars.PopupControlBase");
                    return false;
                }
                var oInnerList = ReflectorForCSharp.GetMember(oControls, "InnerList");
                if ((oInnerList == null) || (!(oInnerList is ArrayList)))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "no InnerList is ArrayList");
                    return false;
                }
                var arrInnerList = oInnerList as ArrayList;
                for (int i = 0; i < arrInnerList.Count; i++)
                {
                    var itm = arrInnerList[i];
                    if (itm == null) continue;
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("arrInnerList[i] type is :[{0}]", itm.GetType()));

                }
                return true;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("PerformClickPopupMenuItemFromPopupbase", strError = string.Format("Exception :[{0}]", e.Message), e);
                return false;
            }

        }

        internal override bool PerformClickPopupMenuItem(object c, string strParameter, string strData, string strPegName, string strObjName, ref string strError, ref string strAdv, ref string strStack)
        {
            int iMode = 0;
            bool isNotExists = false;

            ///wait until rows is load
            long lstart = DateTime.Now.Ticks;
            long lend = lstart;
            bool isOk = true;

            //MarsReflectCheckToContinue<T>(T x1, ref bool isOk, ref string strError, T x2=default(T));
            object oRows = ReflectorForCSharp.WaitUntilMemberExist<object>(c, "Rows", ref isOk, ref strError);
            if (!isOk)
            {
                return false;
            }
            int iRowCount = ReflectorForCSharp.WaitUntilMemberExist<int>(oRows, "Count", ref isOk, ref strError);
            if (!isOk)
                return false;
            if (!string.IsNullOrEmpty(strParameter))
                iMode = 1;
            if (iRowCount <= 0)
            {
                iMode = 0;
            }
            System.Windows.Forms.Control cGrid = (System.Windows.Forms.Control)c;
            windowsWrapper.SystemUtil.MarsWindowsAPIs.RECT rect;
#if _NET4
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
            cGrid.Invoke(
#endif
            new Action(() =>
            {
                cGrid.Focus();
                //active the grid
                windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowRect(cGrid.Handle, out rect);
                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
            }));
            // STRPARAMETER format :];columnName
            string[] arrRc = strParameter.Split(';');
            string strColumnName = arrRc[1];

            MarsTableOperation infraTable = new MarsTableOperation();
            string strColumnKey = "";
            int strColumnIdx = -1;
            if (!infraTable.GetColumnKeyForInfragisticsGrid(cGrid, strColumnName, strPegName, strObjName, ref strColumnKey, ref strColumnIdx, ref strError, ref strAdv, ref strStack))
            {
                return false;
            }
            int iRowIdx = -1;
            if (string.IsNullOrEmpty(arrRc[0]))
            {
                iRowIdx = iRowCount - 1;
            }
            else
            {
                int.TryParse(arrRc[0], out iRowIdx);
                iRowIdx = iRowIdx - 1;
                iRowIdx = iRowIdx < 0 ? 0 : iRowIdx;
            }
            //get target row
            object oTargetRow = infraTable.GetTargetRowFromRows(oRows, iRowIdx, ref isOk, ref strError, ref strAdv, ref strStack);
            if (!isOk)
            {
                return false;
            }
            //get cell 
            object oCell = infraTable.GetCellFromOneRow(oTargetRow, strColumnIdx, ref isOk, ref strError, ref strAdv, ref strStack);
            if (!(isOk && (oCell != null)))
            {
                strError += " No such Cell or Cell is null";
                isOk = false;
                return false;
            }
            //get UIElement
            ReflectorForCSharp reflector = new ReflectorForCSharp();
            object rowUI = reflector.CallMethod(oTargetRow, "GetUIElement", new Type[] { }, ref isNotExists, null);
            object oCellUIElment = reflector.CallMethod(oCell, "GetUIElement", new Type[] { }, ref isNotExists, null);
            if (oCellUIElment != null)
            {
                System.Drawing.Rectangle oRect = reflector.GetMember<System.Drawing.Rectangle>(oCellUIElment, "Rect");

                System.Drawing.Point pt = default(System.Drawing.Point);
                //if (cGrid.Parent == null)
                pt = cGrid.PointToScreen(new System.Drawing.Point(oRect.Left + oRect.Width / 2, oRect.Top + oRect.Height / 2));
                //else
                //pt = cGrid.Parent.PointToScreen(new System.Drawing.Point(oRect.Left + oRect.Width / 2, oRect.Top + oRect.Height / 2));
                //pt = cGrid.PointToScreen(new System.Drawing.Point(oRect.Left + oRect.Width / 2, oRect.Top + oRect.Height / 2));
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("Position:[{0}], new point:[{1}]", oRect, pt));
                // to call up popupmenu
                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(pt.X, pt.Y);
            }
            else
            {
                if (rowUI != null)
                {
                    //row select model
                    System.Drawing.Rectangle oRect = reflector.GetMember<System.Drawing.Rectangle>(rowUI, "Rect");
                    System.Drawing.Point pt = default(System.Drawing.Point);
                    //if (cGrid.Parent==null)
                    pt = cGrid.PointToScreen(new System.Drawing.Point(oRect.Left + oRect.Width / 2, oRect.Top + oRect.Height / 2));
                    //else
                    //    pt = cGrid.Parent.PointToScreen(new System.Drawing.Point(oRect.Left + oRect.Width / 2, oRect.Top + oRect.Height / 2));
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("rowUI!=null Position:[{0}], new point:[{1}]", oRect, pt));
                    // to call up popupmenu
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(pt.X, pt.Y);
                }
                else
                {
                    strError = "Can't find UIElement from Row and Cell";
                    return isOk = false;
                }
            }
            System.Threading.Thread.Sleep(1000);

            //获得所有窗口信息，用于测试
            //simpleLog.MarsLoggerSimple.Info("\t", "----------------------");
            //List<KeyValuePair<IntPtr, string>> lstWndsInfo= windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.GetWindows();
            //foreach(var itm in lstWndsInfo)
            //{
            //    if (itm.Equals(default(KeyValuePair<IntPtr, string>))) continue ;
            //    simpleLog.MarsLoggerSimple.Info("\t",string.Format("window:[{0}]-[{1}]", itm.Key, itm.Value));
            //}


            string[] arrMenuItem = strData.Split(';');
            int iLevel = 0;
            while (iLevel < arrMenuItem.Length)
            {
                int iKeyDownCount = -1;
                if (int.TryParse(arrMenuItem[iLevel], out iKeyDownCount))
                {
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("Level:[{0}] iKeydownCount:[{1}]", iLevel, iKeyDownCount));
                    for (int i = 0; i < iKeyDownCount; i++)
                    {

                        System.Windows.Forms.SendKeys.SendWait("{DOWN}");
                        System.Threading.Thread.Sleep(10);
                    }
                    if (iLevel == arrMenuItem.Length - 1)
                    {
                        //System.Threading.Thread.Sleep(5000);
                        System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                        return isOk = true;
                    }
                    else
                    {
                        //System.Threading.Thread.Sleep(5000);
                        System.Windows.Forms.SendKeys.SendWait("{RIGHT}");
                    }

                    iLevel++;
                }
                else
                {
                    strError = string.Format("Only number(s) is supported,but the input is :[{0}]", strData);
                    return isOk = false;
                }
            }
            strError = "Unknow error for PerformClickPopupMenuItem";
            return isOk = false;
        }
    }
}
