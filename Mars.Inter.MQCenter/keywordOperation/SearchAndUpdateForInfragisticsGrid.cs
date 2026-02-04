using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;
using MarsUFTAddins.IMars.tiger;
using System;

namespace Mars.message.Inter.MQCenter.keywordOperation
{
    class SearchAndUpdateForInfragisticsGrid
    {
        internal const string cnst_rowNUmMode = "RowNumber";
        enum e_marsUpdateMode
        {
            e_unknow,
            e_likeFillTable,
            e_update_the_same_cell,
            e_update_other_cell
        }

        private string[] GetOpMode(string strData, ref e_marsUpdateMode e_OpMode)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetOpMode", $"{strData} to be anlyst");
            try
            {
                string[] arrData = (strData ?? "").Split(';');
                simpleLog.MarsLoggerSimple.Info("\t", $"len after split:[{arrData.Length}]");
                if (arrData.Length == 1)
                {
                    e_OpMode = e_marsUpdateMode.e_likeFillTable;
                    return new string[] { arrData[0], arrData[0], "" };
                }
                if (arrData.Length != 2)
                {
                    e_OpMode = e_marsUpdateMode.e_unknow;
                    return arrData;
                }
                if (arrData[1].StartsWith("[") && arrData[1].EndsWith("]"))
                {
                    string[] arrSubData = arrData[1].Split('-');

                    if (arrSubData.Length != 2)
                    {
                        e_OpMode = e_marsUpdateMode.e_unknow;
                        return arrData;
                    }
                    e_OpMode = e_marsUpdateMode.e_update_other_cell;
                    string[] arrResult = new string[] { arrData[0], arrSubData[1].Replace("]", ""), arrSubData[0].Replace("[", "") };
                    return arrResult;
                }

                e_OpMode = e_marsUpdateMode.e_update_the_same_cell;
                return new string[] { arrData[0], arrData[1], "" };
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetOpMode", $"return {e_OpMode}");
            }
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strX">X的模式</param>
        /// <param name="strY"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <param name="strAttachInfo"></param>
        /// <returns></returns>
        internal bool ClickAt(object oSourceControl, string strParaMeter, string strX,string strY, 
            string strPegName, string strObjName, 
            ref string strError, ref string strAdv, ref string strStack, string strAttachInfo)
        {
            simpleLog.MarsLoggerSimple.logBegin("ClickAt", $"strParaMeter:[{strParaMeter}], strX:[{strX}], strY:[{strY}], strPegName:[{strPegName}], strObjName:[{strObjName}]");
            if (string.IsNullOrEmpty(strY))
            {
                //应该不会发生，但是为避免错误，再次坚持
                strError = "Y parameter is Wrong, it should start with 'Rownumber or #' following ':'";
                strAdv = "Please check the test step data setting";
                strStack = MarsErrorStacks.StackTraceDump();
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }
            int iPos = strY.IndexOf(":");
            string tmpY = strY.Substring(iPos + 1);
            int y;
            if (!int.TryParse(tmpY, out y))
            {
                strError = $"Y parameter is Wrong, format should be Rownumber|#:number, but it is: [{strY}]";
                strAdv = "Please check the test step data setting";
                strStack = MarsErrorStacks.StackTraceDump();
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }
            int x;
            if (!int.TryParse(strX, out x))
            {
                strError = $"X parameter is Wrong, x should be a number, but it is: [{strX}]";
                strAdv = "Please check the test step data setting";
                strStack = MarsErrorStacks.StackTraceDump();
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }

            /// 算法
            /// 1，获得行数
            /// 2，获得行的heigh，从第一行开始
            /// 
            #region 转换为 control
            string strTypes = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
            object oGrid = null;
            if (strTypes.Contains("Misys.OpicsPlus.Framework.PresentationLayer.SecondaryWindows.ControlGridPanel"))
            {
                oGrid = ReflectorForCSharp.GetMember(oSourceControl, "Grid");
            }
            else
            {
                if (strTypes.Contains("UltraGrid"))
                {
                    oGrid = oSourceControl;
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndUpdate", string.Format("unsupported type of object [{0}], only Misys.OpicsPlus.Framework.PresentationLayer.SecondaryWindows.ControlGridPanel and UltraGrid are supported", strTypes));
                    strError = $"Keyword SearchAndUpDate does not support object type for [{strObjName}]| {strTypes}";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
            }
            #endregion
            System.Windows.Forms.Control c = oGrid as System.Windows.Forms.Control;

            bool isNotExists = false;
            object oRows = ReflectorForCSharp.GetMember(oGrid, "Rows", ref isNotExists);
            if (isNotExists)
            {
                strError = string.Format("No Rows exists in [{0}], wrong infragistis version?", strTypes);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }

            int iCount = -1;
            #region 获得row count
            long lStart = DateTime.Now.Ticks;
            long lend = lStart;            
            while ((lend - lStart) / TimeSpan.TicksPerSecond < 60)
            {
                iCount = ReflectorForCSharp.GetMemberByType<int>(oRows, "Count");
                if (iCount <= 0)
                    System.Threading.Thread.Sleep(50);
                else
                    break;
            }
            if (iCount <= 0)
            {
                strError = "no data in Grid";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            #endregion


            if (iCount <= y)
            {
                strError = string.Format($"Table only have [{iCount}] rows, but test step wants to click at row number:[{y}]");
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Please check test step settings";
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }

            int iHeight = 0;
            object[] oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
            MarsTableOperation grdOp = new MarsTableOperation();
            bool isOk = false;
            object displayLayout = grdOp.GetDisplayLayoutFromGrid(c, ref strError, ref isOk);
            if ((displayLayout == null) || (!isOk))
            {
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Please contact Marquis";
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }

            object bands= grdOp.GetBandFromDisplayLayout(displayLayout, ref strError, ref isOk);
            if ((!isOk)||(bands==null))
            {
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Please contact Marquis";
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }

            object header = grdOp.GetHeaderFromBand(bands, ref strError, ref isOk );
            if ((!isOk) || (bands == null))
            {
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Please contact Marquis";
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }
            int iHeaderHeight = grdOp.GetHeaderHeight(header, ref strError, ref isOk);
            if ((!isOk)||(iHeaderHeight<-1))
            {
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Please contact Marquis";
                simpleLog.MarsLoggerSimple.Error("ClickAt", strError);
                return false;
            }

            System.Threading.Thread.Sleep(50);
            string strTmpError = "",
                strStackTmp="", strAcvTmp="";
            bool isOkTmp = false;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
            {
                // 没有考虑该行是否是可见行
                for (int i = 0; i <= y; i++)
                {
                    int tmpHeight = grdOp.GetRowHeight(oAllRows[i], ref strTmpError, ref isOk);
                    if ((!isOk) || (tmpHeight < -1))
                    {
                        strTmpError = $"Can't find height for #[{i}] row. Row object type is :[{oAllRows[i].GetType()}]";
                        simpleLog.MarsLoggerSimple.Error("ClickAt", strTmpError);
                        strStackTmp = MarsErrorStacks.StackTraceDump();
                        strAcvTmp = "Please contact Marquis";
                        isOkTmp = false;
                        return;
                    }
                    if (i == y)
                    {
                        tmpHeight /= 2;
                    }
                    iHeight += (tmpHeight < 0 ? 0 : tmpHeight);
                }
                isOkTmp = true;
            }));
            if (!isOkTmp)
            {
                strError = strTmpError;
                strStack = strStackTmp;
                strAdv = strAcvTmp;
                isOk = false;
                return false;
            }

            /// 计算位置
            /// 
            System.Drawing.Point pt = c.PointToScreen(new System.Drawing.Point(x, iHeight));
            string strAction = SearchAndClickForInfragisticsGrid.CNST_LEFT_CLICK;
            if (!string.IsNullOrEmpty(strParaMeter))
            {
                strAction = strParaMeter.ToUpper();
            }
            switch (strAction)
            {
                case SearchAndClickForInfragisticsGrid.CNST_LEFT_CLICK:
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                    break;
                case SearchAndClickForInfragisticsGrid.CNST_LEFT_DBL_CLICK:
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                    System.Threading.Thread.Sleep(50);
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                    break;
                case SearchAndClickForInfragisticsGrid.CNST_RIGHT_CLICK:
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(pt.X, pt.Y);
                    break;
            }
            return true;
            
        }

        /// <summary>
        /// There are three modes for data, 
        /// 1, filltable
        /// 2, update the same cell
        /// 3, update the other cell
        /// </summary>
        /// <param name="oSourceControl"></param>
        /// <param name="strRC"></param>
        /// <param name="strData"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal bool SearchAndUpdate(object oSourceControl, string strRC, string strData, string strPegName, string strObjName, ref string strError, ref string strAdv, ref string strStack, string strAttachInfo = null)
        {
            //// strparameter format "DYNAMICROW;_currentPercentColumn;1"
            /// Data format demo : 2/17/2014;[Notional:-30000]
            string[] arrPara = strRC.Split(new char[] { ';' });
            string strErrorWrongFormat = string.Format("parameter format is wrong, should be [rownumber|DYNAMICROW];columnName;inputMode. but it is [{0}]", strRC);
            string strErrorWrongDataFmt = string.Format("Data format should be: dataToSearch;[TargetColumnName-DataToFill] or dataToSearch or dataToSearch;NewData");
            if (arrPara.Length != 3)
            {
                strError = strErrorWrongFormat;
                return false;
            }
            string strTypes = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
            object oGrid = null;
            if (strTypes.Contains("Misys.OpicsPlus.Framework.PresentationLayer.SecondaryWindows.ControlGridPanel"))
            {
                oGrid = ReflectorForCSharp.GetMember(oSourceControl, "Grid");
            }
            else
            {
                if (strTypes.Contains("UltraGrid"))
                {
                    oGrid = oSourceControl;
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndUpdate", string.Format("unsupported type of object [{0}], only Misys.OpicsPlus.Framework.PresentationLayer.SecondaryWindows.ControlGridPanel and UltraGrid are supported", strTypes));
                    strError = $"Keyword SearchAndUpDate does not support object type for [{strObjName}]| {strTypes}";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
            }
            System.Windows.Forms.Control c = oGrid as System.Windows.Forms.Control;

            e_marsUpdateMode e_CurrentMode = e_marsUpdateMode.e_unknow;
            ///0-searchedData
            ///1-updated data 
            ///2-other columnName
            string[] arrData = GetOpMode(strData, ref e_CurrentMode);

            if (e_CurrentMode == e_marsUpdateMode.e_unknow)
            {
                strError = strErrorWrongDataFmt + " but the data is " + strData;
                simpleLog.MarsLoggerSimple.Error("\t", strError);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            string strDataToSearch = arrData[0];

            bool isNotExists = false;
            object oRows = ReflectorForCSharp.GetMember(oGrid, "Rows", ref isNotExists);
            if (isNotExists)
            {
                strError = string.Format("No Rows exists in [{0}], wrong infragistis version?", strTypes);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            
            long lStart = DateTime.Now.Ticks;
            long lend = lStart;
            int iCount = -1;
            while ((lend - lStart) / TimeSpan.TicksPerSecond < 60)
            {
                iCount = ReflectorForCSharp.GetMemberByType<int>(oRows, "Count");
                if (iCount <= 0)
                    System.Threading.Thread.Sleep(50);
                else
                    break;
            }
            if (iCount <= 0)
            {
                strError = "no data in Grid";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }

            string strKey = "", strKeyForTarget = "";
            bool isOkTmp = false;

            if (e_CurrentMode == e_marsUpdateMode.e_update_other_cell)
            {
                strKeyForTarget = arrData[2];
            }

            string strErrorTmp = "";
            string strAdvTmp = "", strStackTmp = "";
            int iColIdx = -1, iColTargetIdx = -1;

            string strColumnName = arrPara[1];
#if _NET4
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
            (oSourceControl as System.Windows.Forms.Control).Invoke(
#endif
            new Action(() =>
            {
                isOkTmp = (new MarsTableOperation()).GetColumnKeyForInfragisticsGrid(oSourceControl, strColumnName, strPegName, strObjName, ref strKey, ref iColIdx, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("GetColumnKeyForInfragisticsGrid returns [{0}] col:[{1}] iColIdx-{2} strKey-[{3}]", isOkTmp, strColumnName, iColIdx, strKey));
                if ((isOkTmp) && (e_CurrentMode == e_marsUpdateMode.e_update_other_cell))
                {
                    isOkTmp = (new MarsTableOperation()).GetColumnKeyForInfragisticsGrid(oSourceControl, arrData[2], strPegName, strObjName, ref strKeyForTarget, ref iColTargetIdx, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("GetColumnKeyForInfragisticsGrid 2 returns [{0}] col:[{1}] iColIdx-{2} strKey-[{3}]", isOkTmp, strColumnName, iColIdx, strKey));
                }
                else
                {
                    strKeyForTarget = strKey;
                    iColTargetIdx = iColIdx;
                }
            }));

            strError = strErrorTmp;
            strAdv = strAdvTmp;
            strStack = strStackTmp;
            if (!isOkTmp) return false;

            object[] oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
            object oTargetRow = null, oTargetCell = null;

            int iTargetRowNumber = -1, iStartRowNumber = 0;
            if (string.IsNullOrEmpty(arrPara[0]) || ((string.Compare("DYNAMICROW", arrPara[0], true) == 0)))
            {
                oTargetRow = oAllRows[oAllRows.Length - 1];
                iTargetRowNumber = oAllRows.Length;
            }
            else
            {
                if (!int.TryParse(arrPara[0], out iTargetRowNumber))
                {
                    strError = strErrorWrongFormat;
                    return false;
                }
                if ((iTargetRowNumber <= 0) || (iTargetRowNumber > oAllRows.Length))
                {
                    strError = string.Format("Row number is :[{0}] but total rows are :[{1}],row number begins 1.", iTargetRowNumber, oAllRows.Length);
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                iStartRowNumber = iTargetRowNumber - 1;
                oTargetRow = oAllRows[iTargetRowNumber - 1];
            }
            for (int i = iStartRowNumber; i < iTargetRowNumber; i++)
            {
                object oOneRow = oAllRows[i];
                if (oOneRow == null)
                {
                    continue;
                }
                object oCellsCollection = ReflectorForCSharp.GetMember(oOneRow, "Cells"); //Infragistics.Win.UltraWinGrid.CellsCollection
                if (oCellsCollection == null)
                {
                    continue;
                }
                object[] allCells = ReflectorForCSharp.GetMemberByType<object[]>(oCellsCollection, "All");
                if (allCells == null)
                {
                    continue;
                }
                if (allCells.Length <= iColIdx)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Only [{0}] cells returns, but cell index is :[{1}]", allCells.Length, iColIdx));
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                if (allCells[iColIdx] != null)
                {
                    string strCellText = ReflectorForCSharp.GetMemberByType<string>(allCells[iColIdx], "Text");
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("Get cell Text [{0}] - iColIdx-{1}", strCellText, iColIdx));
                    if ((e_CurrentMode == e_marsUpdateMode.e_update_other_cell)
                        || (e_CurrentMode == e_marsUpdateMode.e_update_the_same_cell))
                    {
                        if (!(windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strDataToSearch, strCellText)
                            || (string.Compare(strDataToSearch, strCellText, true) == 0)))
                        {
                            continue;
                        }
                    }

                    /// find the cell
                    /// 
                    oTargetRow = oOneRow;
                    oTargetCell = allCells[iColTargetIdx];

                    ReflectorForCSharp reflector = new ReflectorForCSharp();
                    ///将依据parameter， 将列或者行放到可视区域
                    ///
                    if ((!string.IsNullOrEmpty(strAttachInfo)))
                    {
                        if (string.Compare(strAttachInfo, "Column", true) == 0)
                        {
                            object oActiveColScrollRegion = ReflectorForCSharp.GetMember(oSourceControl, "ActiveColScrollRegion", ref isNotExists);
                            if (isNotExists)
                            {
                                simpleLog.MarsLoggerSimple.Error("SearchAndUpdate", strError = "Object property [ActiveRowScrollRegion]'s value is NULL in Grid");// "No ActiveRowScrollRegion in Grid, wrong infragistics version?");
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "Contact Marquis";
                                return false;
                            }
                            object oActiveRowScrollRegion = ReflectorForCSharp.GetMember(oSourceControl, "ActiveRowScrollRegion", ref isNotExists);
                            if (isNotExists)
                            {
                                simpleLog.MarsLoggerSimple.Error("SearchAndUpdate", strError = "Object property [ActiveRowScrollRegion]'s value is NULL in Grid");// "No ActiveRowScrollRegion in Grid, wrong infragistics version?");
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "Contact Marquis";
                                return false;
                            }

                            if (c.InvokeRequired)
                            {
                                c.Invoke(new Action(() =>
                                {
                                    reflector.CallMethodByTypes(oActiveColScrollRegion, "ScrollCellIntoView", new object[] { oTargetCell, oActiveRowScrollRegion, false });
                                }));
                            }
                            else
                            {
                                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                                //{
                                reflector.CallMethodByTypes(oActiveColScrollRegion, "ScrollCellIntoView", new object[] { oTargetCell, oActiveRowScrollRegion, false });
                            }
                            System.Threading.Thread.Sleep(1000);
                            //}));

                        }
                        else
                        {
                            object oActiveRowScrollRegion = ReflectorForCSharp.GetMember(oSourceControl, "ActiveRowScrollRegion", ref isNotExists);
                            if (isNotExists)
                            {
                                simpleLog.MarsLoggerSimple.Error("SearchAndUpdate", strError = "Object property [ActiveRowScrollRegion]'s value is NULL in Grid");// "No ActiveRowScrollRegion in Grid, wrong infragistics version?");
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "Contact Marquis";
                                return false;
                            }

                            if (!reflector.SetMemberValue(oOneRow, oActiveRowScrollRegion, "FirstRow", ref strError, ref strStack))
                            {
                                //isOk = false;
                                simpleLog.MarsLoggerSimple.Error("SearchAndUpdate", strError = string.Format("Error when get FirstRow from Grid, [{0}]", strError));
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "Contact Marquis";
                                return false;
                            }
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                    //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() => {
                    if (c.InvokeRequired)
                    {
                        c.Invoke(new Action(() =>
                        {
                            isOkTmp = reflector.SetMemberValue(true, oTargetCell, "Activated", ref strErrorTmp, ref strStackTmp);
                        }));
                    }
                    else
                        isOkTmp = reflector.SetMemberValue(true, oTargetCell, "Activated", ref strErrorTmp, ref strStackTmp);
                    if (!isOkTmp)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", string.Format("Can't set Active:[{0}]", strErrorTmp));
                    }
                    //}));
                    object oCellUIElment = null;
                    if ((c != null) && (c.InvokeRequired))
                    {
                        c.Invoke(new Action(() =>
                        {
                            oCellUIElment = reflector.CallMethod(oTargetCell, "GetUIElement", new Type[] { }, ref isNotExists, null);
                        }));
                    }
                    else oCellUIElment = reflector.CallMethod(oTargetCell, "GetUIElement", new Type[] { }, ref isNotExists, null);
                    if ((oCellUIElment == null) || (isNotExists))
                    {
                        oCellUIElment = reflector.CallMethod(oTargetRow, "GetUIElement", new Type[] { }, ref isNotExists, null);
                    }
                    if ((oCellUIElment == null) || (isNotExists))
                    {
                        strError = "Object property UIElement is null";//"Can't get UIElment for both cell and row";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    System.Drawing.Rectangle oRect = reflector.GetMember<System.Drawing.Rectangle>(oCellUIElment, "Rect");
                    System.Drawing.Rectangle clipRect = reflector.GetMember<System.Drawing.Rectangle>(oCellUIElment, "ClipRect");
                    if (oRect.Equals(default(System.Drawing.Rectangle)))
                    {
                        simpleLog.MarsLoggerSimple.Error("SearchAndUpdate", strError = "Object Rectangle property is null");//"No Rect object return,Wrong Infragistics Version?") ;
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }

                    System.Windows.Forms.Control c4CellUIElement = ReflectorForCSharp.GetMemberByType<System.Windows.Forms.Control>(oCellUIElment, "Control");
                    if (c4CellUIElement != null)
                    {
                        if (c4CellUIElement.InvokeRequired)
                        {
#if _NET4
                            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                            c4CellUIElement.Invoke(
#endif
                            new Action(() =>
                            {
                                c4CellUIElement.Focus();
                            }));
                        }
                        else
                        {
                            c4CellUIElement.Focus();
                        }
                    }

                    System.Drawing.Point pt = (oGrid as System.Windows.Forms.Control).PointToScreen(new System.Drawing.Point(oRect.X + oRect.Width / 2, oRect.Y + oRect.Height / 2));
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                    System.Threading.Thread.Sleep(10);
                    System.Windows.Forms.SendKeys.SendWait("{HOME}");
                    simpleLog.MarsLoggerSimple.Info("\t", "begin to delete all text");
                    for (int j = 0; j < 50; j++)
                    {
                        System.Threading.Thread.Sleep(10);
                        System.Windows.Forms.SendKeys.SendWait("{DEL}");
                    }
                    if (c4CellUIElement != null)
                    {
                        if (c4CellUIElement.InvokeRequired)
                        {
#if _NET4
                            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                            c4CellUIElement.Invoke(
#endif
                            new Action(() =>
                            {
                                System.Windows.Forms.SendKeys.SendWait(arrData[1]);
                                System.Windows.Forms.SendKeys.SendWait("{TAB}");
                            }));
                            return true;
                        }
                    }
                    System.Windows.Forms.SendKeys.SendWait(arrData[1]);
                    System.Windows.Forms.SendKeys.SendWait("{TAB}");

                    return true;
                }
            }
            strError = string.Format("No data find [{0}] from column [{1}]", strDataToSearch, strColumnName);
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Contact Marquis";
            simpleLog.MarsLoggerSimple.Error("\t", strError);
            return false;
        }

        
    }
}
