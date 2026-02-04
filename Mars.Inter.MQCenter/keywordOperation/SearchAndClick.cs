using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Drawing;
using System.Collections.Generic;
using Mars.message.windowsWrapper.SystemUtil;
using System.Collections;


namespace Mars.message.Inter.MQCenter.keywordOperation
{
    internal abstract class SearchAndClick
    {
        public abstract bool SearchAndClickFromControl(object oSourceControl, string strParameter, string strData, string strPegName, string strObjName, ref string strError, ref string strAdv, ref string strStack);
    }

    internal enum searchAndClickParaMode
    {
        _wrongFormat = -0x01,
        _normal = 0x01,
        _marsAddins,
        _marsLoopAddins,
        _marsAddinsMultilple,
        _marsAddinsGrouped,
        _marsLastRow,
        _marsFirstRow,
        _marsRowNum,
        _marsMultipleCondWithIndx
    }

    internal class MARSInfragistics_GridColumn
    {
        internal string columnCaption;
        internal string columnKey;
        internal int columnIdx;
        internal object columnObj;
    }
    /// <summary>
    /// searchandClick的参数处理
    /// </summary>
    internal class SearchAndClickParaWithData
    {
        internal searchAndClickParaMode currentType;
        internal string SearchAndCickData = null;
        internal static SearchAndClickParaWithData GetParaInstanceFactory(string strParaFromTestStep)
        {
            if (MarsWindowsAPIsExtend.RegularTest(SearchAndClickForInfragisticsGrid.CNST_SEARCHANDCLICK_GROUPED, strParaFromTestStep))
            {
                return new SearchAndClickParaWithData_Grouped(strParaFromTestStep);
            }
            return null;
        }

        internal virtual bool IsDataFormatRight()
        {
            return true;
        }
        internal virtual bool ParseData()
        {
            return true;
        }
        internal virtual bool ParsePara()
        {
            return true;
        }
    }

    internal class SearchAndClickParaWithData_Grouped : SearchAndClickParaWithData
    {
        internal string[] RowGroupedData = null;
        internal string[] subTableCellsData = null;

        internal string[] GroupedColumns = null;
        internal string[] subTableCheckColumns = null;

        internal int[] subTableColumnsIdx = null;
        internal string[] subTableColumnsKey = null;
        internal string[] subTableColumnsCaption = null;

        internal string ParaSource = "";

        public SearchAndClickParaWithData_Grouped(string strPara) : base()
        {
            currentType = searchAndClickParaMode._marsAddinsGrouped;
            ParaSource = strPara;
        }

        internal bool dealWithSearchAndClick(object oSourceControl,
            object oRows,
            string strData,
            string strPegName, string strObjName,
            string strClickCommand,
            ref string strError, ref string strAdv, ref string strStack,
            bool isCheckingHidden = true)
        {
            simpleLog.MarsLoggerSimple.logBegin("dealWithSearchAndClick", $"strData {strData}");
            bool isNotExist = false;
            try
            {
                MarsTableOperation tbOp = new MarsTableOperation();
                bool isOkTmp = false;
                string strTmpCols = string.Join(":", this.GroupedColumns);
                strTmpCols = $"[{strTmpCols}]";
                SearchAndClickPara_Multiple tmpMulitpleDt = new SearchAndClickPara_Multiple(strTmpCols);

                ///算法：
                ///1，先获得是否存在 column Name 【group的】
                ///2，循环获得row and all
                ///

                string strKey = "";
                int iColIdx = -1;
                string strErrorTmp = "",
                    strAdvTmp = "",
                    strStackTmp = "";
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                    new Action(() => {
                        isOkTmp = tbOp.GetColumnKeyForInfragisticsGrid(oSourceControl,
                            tmpMulitpleDt,
                            strPegName, strObjName,
                            ref strKey, ref iColIdx, ref strErrorTmp, ref strAdvTmp, ref strStackTmp, isCheckingHidden);
                    })
                    );
                if (!isOkTmp)
                {
                    strError = strErrorTmp;
                    strAdv = strAdvTmp;
                    strStack = strStackTmp;
                    return false;
                }

                // 2 获得rows
                object[] oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
                int iCnt = oAllRows == null ? -1 : oAllRows.Length;
                simpleLog.MarsLoggerSimple.Info("dealWithSearchAndClick", $"find rows:{iCnt}");
                object oTargetRow = null, oTargetCell = null;
                int iEndRow = ((oAllRows == null ? -1 : oAllRows.Length));
                //string strResult = "";
                string strAllTxt = "";

                //3, 判断是否有匹配的
                List<object> allTargetRows = new List<object>();

                for (int i = 0; i < iEndRow; i++)
                {
                    object oneRow = oAllRows[i];
                    if (oneRow == null) continue;
                    //获得cells
                    //object oCellsCollection = ReflectorForCSharp.GetMember(oneRow, "Cells"); //Infragistics.Win.UltraWinGrid.CellsCollection
                    //if (oCellsCollection == null)
                    //{
                    //    continue;
                    //}
                    //object[] allCells = ReflectorForCSharp.GetMemberByType<object[]>(oCellsCollection, "All");
                    //if (allCells == null)
                    //{
                    //    continue;
                    //}
                    ///// 判断是否存在指定的cell
                    ///// 
                    //if ((iColIdx >= allCells.Length)||(allCells[iColIdx]==null))
                    //{
                    //    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = $"only [{allCells.Length}] cells, but the column [{strTmpCols}]'s idx is [{iColIdx}]");
                    //    strAdv = "Contact Marquis";
                    //    strStack = Environment.StackTrace;
                    //    return false;
                    //}
                    //第一层访问 “description" 属性

                    object desc = ReflectorForCSharp.GetMember(oTargetCell = oneRow, "Description", ref isNotExist);
                    if ((isNotExist) || (desc == null))
                    {
                        strError = $"No description property or member from type {oneRow.GetType()}";
                        strAdv = "Contact Marquis";
                        strStack = Environment.StackTrace;
                        return false;
                    }
                    strAllTxt = string.IsNullOrEmpty(strAllTxt) ? desc.ToString() : $"{strAllTxt};{desc.ToString()}";
                    if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(this.RowGroupedData[0], desc.ToString()))
                    {
                        simpleLog.MarsLoggerSimple.Info("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick",
                            $"compared [{desc.ToString()}] to [{this.RowGroupedData[0]}] no matched, continue");
                        continue;
                    }
                    allTargetRows.Add(oneRow);
                }
                if (allTargetRows.Count == 0)
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = $"[{this.RowGroupedData[0]}] is not found in [{strAllTxt}]");
                    strAdv = "Please fix the test case's dataset and try again";
                    strStack = Environment.StackTrace;
                    return false;
                }
                if (allTargetRows.Count > 1)
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = $"[{this.RowGroupedData[0]}] finds more than one rows in [{strAllTxt}]");
                    strAdv = "Please fix the test case's dataset and try again";
                    strStack = Environment.StackTrace;
                    return false;
                }
                ///找到groupedrows
                ///先将该row expand，然后重复获得rows，conolumns，逐行扫描
                ///
                object expanded = null;
                isNotExist = false;
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    expanded = ReflectorForCSharp.GetMember(allTargetRows[0], "Expanded", ref isNotExist);
                }));
                if ((isNotExist) || (expanded == null))
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = $"[Description] is not found from type :[{allTargetRows[0].ToString()}]");
                    strAdv = "Contact Marquis";
                    strStack = Environment.StackTrace;
                    return false;
                }

                ///设置为expand 为true
                ///
                ReflectorForCSharp rflct = new ReflectorForCSharp();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() => {
                    isOkTmp = rflct.SetMemberValue(true, allTargetRows[0], "Expanded", ref strErrorTmp, ref strStackTmp);
                    System.Threading.Thread.Sleep(200);
                }));

                if (!isOkTmp)
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = strErrorTmp, strStack = strStackTmp);
                    strAdv = "Contact Marquis";

                    return false;
                }
                /// 假定所有的row展开了
                /// 
                System.Threading.Thread.Sleep(200);
                ///通过循环
                ///
                long n = DateTime.Now.Ticks, p = n;
                object subRows = null;
                object[] subRowsOfAll = null;
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    while (((p = DateTime.Now.Ticks) - n) < (5 * TimeSpan.TicksPerSecond))
                    {
                        subRows = ReflectorForCSharp.GetMember(allTargetRows[0], "Rows");
                        try
                        {
                            if (subRows == null)
                            {
                                System.Threading.Thread.Sleep(200);
                                continue;
                            }
                            //object oAllOfSubRows = ReflectorForCSharp.GetMember(subRows, "All", ref isNotExist);
                            //if (isNotExist)
                            //{
                            //    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = $"No 'All' is found from type [{subRows.ToString()}]", strStack = Environment.StackTrace);
                            //    strAdv = "Contact Marquis";

                            //    return false;
                            //}
                            object[] arrAll = ReflectorForCSharp.GetMemberByType<object[]>(subRows, "All");
                            if ((arrAll == null) || (arrAll.Length <= 0))
                            {
                                System.Threading.Thread.Sleep(200);
                                continue;
                            }
                            subRowsOfAll = arrAll;
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Threading.Thread.Sleep(200);
                        }
                        finally
                        {

                        }


                    };
                }
                ));
                System.Threading.Thread.Sleep(200);


                if ((subRowsOfAll == null) || (subRowsOfAll.Length <= 0))
                {
                    strError = $"No available rows find for Grouped Grid, type is [{allTargetRows[0].ToString()}]. ";
                    strStack = Environment.StackTrace;
                    strAdv = "Contact Marquis";
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError);
                    return false;
                }
                /// 判断是否有合适的columns
                /// 
                bool isOk = true;
                object bandOfGroupedRow = ReflectorForCSharp.GetMember(allTargetRows[0], "Band", ref isNotExist);
                //object bandOfGroupedRow = tbOp.GetBandFromDisplayLayout(allTargetRows[0], ref strError, ref isOk);
                if (bandOfGroupedRow == null)
                {
                    strStack = Environment.StackTrace;
                    strAdv = "Make sure the Grid is grouped";
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick",
                        strError = $"No 'Band' property or member exists in [{allTargetRows[0].ToString()}]", strStack);
                    return false;
                }
                /// 获取所有的columns的
                /// 
                bool isNoMemberExists = false;
                object ColumnsInBand0 = ReflectorForCSharp.GetMember(bandOfGroupedRow, "Columns", ref isNoMemberExists);
                if (isNoMemberExists)
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = "no Columns for Grid.DisplayLayout.Bands[0], wrong Ultragrid version?");
                    strError = "Object does not contain columns for DisplayLayout property";
                    strStack = Environment.StackTrace;
                    strAdv = "Make sure object is a UltraGrid";
                    isOk = false;
                    return false;
                }
                object olstColumns = ReflectorForCSharp.GetMember(ColumnsInBand0, "List", ref isNoMemberExists);
                if (isNoMemberExists)
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = "no Columns for Grid.DisplayLayout.Bands[0].Columns, wrong Ultragrid version?");
                    strError = "Object does not contain List for Columns property";

                    strStack = Environment.StackTrace;
                    strAdv = "Make sure object is a UltraGrid";
                    isOk = false;
                    return false;
                }
                if ((!(olstColumns is ArrayList)) || (olstColumns == null))
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick",
                        strError = string.Format("List from Grid.DisplayLayout.Bands[0].Columns should be ArrayList, it is [{0}]. wrong Ultragrid version?", olstColumns.GetType().ToString()));
                    strError = "Member \"List\"'s type is not ArrayList ";
                    strStack = Environment.StackTrace;
                    strAdv = "Contact Marquis";
                    isOk = false;
                    return false;
                }
                ArrayList allColumns = olstColumns as ArrayList;

                if ((!isOk) || (allColumns == null))
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick",
                        strError, strStack);
                    return false;
                }
                /// 比较column，同时将columns的id放到相应的位置
                /// 
                string strAllKeys = "";
                this.subTableColumnsIdx = new int[this.subTableCheckColumns.Length];
                this.subTableColumnsKey = new string[this.subTableCheckColumns.Length];
                this.subTableColumnsCaption = new string[this.subTableCheckColumns.Length];

                for (int i = 0; i < this.subTableCheckColumns.Length; i++)
                {
                    this.subTableColumnsIdx[i] = -1;
                }
                for (int i = 0; i < allColumns.Count; i++)
                {
                    object oColumnItm = allColumns[i];
                    if (oColumnItm == null) continue;

                    object oHeader = ReflectorForCSharp.GetMember(oColumnItm, "Header");
                    if (oHeader == null) continue;
                    bool isHidden = ReflectorForCSharp.GetMemberByType<bool>(oHeader, "Hidden");
                    if (isHidden) continue;
                    string caption = ReflectorForCSharp.GetMemberByType<string>(oHeader, "Caption");
                    string strKeyTmp = ReflectorForCSharp.GetMemberByType<string>(oColumnItm, "Key");

                    int idxTmp = ReflectorForCSharp.GetMemberByType<int>(oColumnItm, "Index");
                    strAllKeys = string.Format("{0};[{1}]-[{2}]", strAllKeys, caption, strKeyTmp);

                    for (int j = 0; j < this.subTableCheckColumns.Length; j++)
                    {
                        if (this.subTableColumnsIdx[j] != -1) continue; // 说明已经处理了

                        string strCapExpFixed = this.subTableCheckColumns[j];
                        string strColName = this.subTableCheckColumns[j];


                        //simpleLog.MarsLoggerSimple.Info("\t", $"all keys:{strAllKeys}");
                        if ((Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCapExpFixed, caption))
                            || (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCapExpFixed, strKeyTmp))
                            || (string.Compare(strColName, caption, true) == 0)
                            || (string.Compare(strColName, strKeyTmp, true) == 0)
                            )
                        {
                            this.subTableColumnsIdx[j] = idxTmp;
                            this.subTableColumnsCaption[j] = caption;
                            this.subTableColumnsKey[j] = strKeyTmp;
                        }
                    }
                }
                for (int i = 0; i < this.subTableColumnsIdx.Length; i++)
                {
                    if (this.subTableColumnsIdx[i] <= 0)
                    {
                        strError = $"No column [{this.subTableCheckColumns[i]}] is found from [{strAllKeys}] ";
                        simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick",
                            strError);
                        strAdv = "Correct column caption and try again";
                        strStack = Environment.StackTrace;
                        return false;
                    }
                }
                /// 逐行扫描数据
                /// 
                ReflectorForCSharp reflector = new ReflectorForCSharp();
                List<object> selectedRows = new List<object>();
                bool isRowAllMatched = true;
                object objTargetCell = null;
                for (int i = 0; i < subRowsOfAll.Length; i++)
                {
                    object subItm = subRowsOfAll[i];
                    if (subItm == null) continue;
                    object oCellsCollection = ReflectorForCSharp.GetMember(subItm, "Cells", ref isNotExist);
                    if (isNotExist)
                    {
                        simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick",
                            strError = $"No Cells member from [{subItm.GetType()}]");
                        strAdv = "Contact Marquis";
                        strStack = Environment.StackTrace;
                        return false;
                    }
                    object[] allCells = ReflectorForCSharp.GetMemberByType<object[]>(oCellsCollection, "All");
                    if (allCells == null)
                    {
                        simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick",
                           strError = $"No 'All' member from [{oCellsCollection.GetType()}]");
                        strAdv = "Contact Marquis";
                        strStack = Environment.StackTrace;
                        return false;
                    }

                    isRowAllMatched = true;

                    for (int k = 0; k < this.subTableColumnsIdx.Length; k++)
                    {
                        int cellIdx = this.subTableColumnsIdx[k];
                        string strDataToCompare = this.subTableCellsData[k];
                        try
                        {
                            object oCell = allCells[cellIdx];
                            if (oCell == null)
                            {
                                isRowAllMatched = false;
                                break;
                            }

                            string strCellText = ReflectorForCSharp.GetMemberByType<string>(oCell, "Text");
                            object oCellValue = ReflectorForCSharp.GetMemberByType<string>(oCell, "Value");
                            string strCellValue = oCellValue == null ? "" : oCellValue.ToString();
                            if (string.IsNullOrEmpty(strCellText))
                            {
                                if (!string.IsNullOrEmpty(strDataToCompare))
                                {
                                    isRowAllMatched = false;
                                    break;
                                }
                            }
                            else
                            {
                                if (!(windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strDataToCompare, strCellText)
                                    || (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strDataToCompare, strCellValue))))
                                {
                                    isRowAllMatched = false;
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            isRowAllMatched = false;
                            break;
                        }
                    }

                    if (!isRowAllMatched)
                    {
                        continue;
                    }
                    objTargetCell = allCells[this.subTableColumnsIdx[0]];
                    selectedRows.Add(subItm);
                }

                if (selectedRows.Count <= 0)
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick", strError = "no matched row is found.");
                    strStack = Environment.StackTrace;
                    strAdv = "Change the data and try again";
                    return false;
                }
                if (selectedRows.Count > 1)
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickParaWithData_Grouped.dealWithSearchAndClick",
                        strError = $"More than one rows, matched count [{selectedRows.Count}], are found.");
                    strStack = Environment.StackTrace;
                    strAdv = "Change the data and try again";
                    return false;
                }
                oTargetRow = selectedRows[0];
                /// 获得UIElement 然后点击
                /// 

                if (string.Compare("SCROLL", strClickCommand, true) == 0)
                {
                    //滚动该行

                    var ActiveRowScrollRegion = ReflectorForCSharp.GetMember(oSourceControl, "ActiveRowScrollRegion", ref isNotExist);
                    if ((ActiveRowScrollRegion == null) || isNotExist)
                    {
                        string strTyps = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
                        simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't find ActiveRowScrollRegion from grid [{0}], wrong version?", strTyps));
                        strError = "Object property [ActiveRowScrollRegion] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    //
                    reflector.CallMethodByParaType(ActiveRowScrollRegion, "ScrollRowIntoView", new Type[] { oTargetRow.GetType() }, new object[] { oTargetRow });
                    System.Threading.Thread.Sleep(1000);
                    IntPtr lpdwResult;
                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                        //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)oSourceControl),((System.Windows.Forms.Control)oSourceControl).Handle),
                        ((System.Windows.Forms.Control)oSourceControl).Handle,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                        1000,
                        out lpdwResult);
                    return true;
                }
                if ((string.Compare("SCROLL_H", strClickCommand, true) == 0) || (string.Compare("SCROLL_COL", strClickCommand, true) == 0))
                {

                    var ActiveColScrollRegion = ReflectorForCSharp.GetMember(oSourceControl, "ActiveColScrollRegion", ref isNotExist);
                    if ((ActiveColScrollRegion == null) || isNotExist)
                    {
                        string strTyps = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
                        simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't find ActiveRowScrollRegion from grid [{0}], wrong version?", strTyps));
                        strError = "Object property [ActiveRowScrollRegion] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    //
                    try
                    {
                        reflector.CallMethodByParaType(ActiveColScrollRegion, "ScrollColIntoView", new Type[] { tbOp.targetColumnForScrollCol.GetType() }, new object[] { tbOp.targetColumnForScrollCol });
                    }
                    catch (Exception e)
                    {
                        simpleLog.MarsLoggerSimple.Error("\tSearchAndClickFromControl", $"exception when call ScrollColIntoView\r\n:{e.Message}", e);
                        strError = e.Message;
                        strStack = e.StackTrace;
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    System.Threading.Thread.Sleep(1000);
                    IntPtr lpdwResult;
                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                        //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)oSourceControl),((System.Windows.Forms.Control)oSourceControl).Handle),
                        ((System.Windows.Forms.Control)oSourceControl).Handle,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                        1000,
                        out lpdwResult);
                    return true;
                }

                /// clickAT预处理
                /// 
                SearchAndClickAction_ClickAt clickAt = null;
                if ((!string.IsNullOrEmpty(strClickCommand)) 
                    && ((strClickCommand.StartsWith(SearchAndClickForInfragisticsGrid.CNST_L_CLICKAT, StringComparison.OrdinalIgnoreCase))
                    ||(strClickCommand.StartsWith(SearchAndClickForInfragisticsGrid.CNST_CLICK_ROW_HEADER, StringComparison.OrdinalIgnoreCase)))
                    )
                {
                    clickAt = SearchAndClickAction_ClickAt.Parse(strClickCommand, ref strError, ref strAdv, ref strStack);
                    strClickCommand = SearchAndClickForInfragisticsGrid.CNST_L_CLICKAT;
                    if (strClickCommand.StartsWith(SearchAndClickForInfragisticsGrid.CNST_CLICK_ROW_HEADER, StringComparison.OrdinalIgnoreCase))
                    {
                        strClickCommand = SearchAndClickForInfragisticsGrid.CNST_CLICK_ROW_HEADER;
                    }
                    if (clickAt == null)
                    {
                        simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError);
                        return false;
                    }
                }

                object oCellUIElment = reflector.CallMethod(oTargetCell, "GetUIElement", new Type[] { }, ref isNotExist, null);
                object oRowUIElement = null;
                if ((oCellUIElment == null) || (isNotExist))
                {
                    oCellUIElment = reflector.CallMethod(oTargetRow, "GetUIElement", new Type[] { }, ref isNotExist, null);
                }
                bool isWithError = false;

                if (clickAt != null)
                {
                    bool isRowUIExists = true;
                    // clickat mode or click header Mode
                    oCellUIElment = reflector.CallMethod(oTargetRow, "GetUIElement", new Type[] { }, ref isRowUIExists, null);
                    if (oCellUIElment == null)
                        isWithError = true;
                }
                if ((oCellUIElment == null) || (isNotExist))
                {
                    if (isWithError)
                    {
                        strError = "Object property UIElement is null";// "Can't get UIElment for both cell and row";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                }
                System.Drawing.Rectangle oRect      = reflector.GetMember<System.Drawing.Rectangle>(oCellUIElment, "Rect");
                System.Drawing.Rectangle clipRect   = reflector.GetMember<System.Drawing.Rectangle>(oCellUIElment, "ClipRect");
                if (oRect.Equals(default(System.Drawing.Rectangle)))
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError = "Object Rectangle property is null");//"No Rect object return,Wrong Infragistics Version?");
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                System.Drawing.Point pt = (oSourceControl as System.Windows.Forms.Control).PointToScreen(new System.Drawing.Point(oRect.X + oRect.Width / 2, oRect.Y + oRect.Height / 2));
                //string strClickCommand = strAction.ToUpper().Replace("ACTION:", "");
                switch (string.IsNullOrEmpty(strClickCommand) ? SearchAndClickForInfragisticsGrid.CNST_NOACTION : strClickCommand.ToUpper())
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
                    case SearchAndClickForInfragisticsGrid.CNST_L_CLICKAT:
                    case SearchAndClickForInfragisticsGrid.CNST_CLICK_ROW_HEADER:
                        pt = clickAt.CalcPos(oSourceControl, oRect, ref isOkTmp, ref strError, ref strAdv, ref strStack);
                        if (!isOkTmp) return false;
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                        break;
                    case SearchAndClickForInfragisticsGrid.CNST_NOACTION:
                    default:
                        break;
                }
                return true;

            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("dealWithSearchAndClick", e.Message, e);
                strError = $"Exception:{e.Message}";
                strStack = e.StackTrace;
                strAdv = "Contact Marquis";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("dealWithSearchAndClick");
            }
        }


        internal override bool IsDataFormatRight()
        {
            if (string.IsNullOrEmpty(this.SearchAndCickData)) return false;
            if (MarsWindowsAPIsExtend.RegularTest(SearchAndClickForInfragisticsGrid.CNST_SEARCHANDCLICK_GROUPED_DATA, SearchAndCickData)) return true;
            return false;
        }

        internal override bool ParsePara()
        {
            GroupedColumns = new string[] { };
            subTableCheckColumns = new string[] { };
            bool isOk = ParseDataAndPara(ParaSource, ref GroupedColumns, ref subTableCheckColumns);
            return isOk;
        }

        internal bool ParseDataAndPara(string strParaOrDataTmp, ref string[] arrPart1, ref string[] arrPart2)
        {
            //format is @"\[.*\]:\[(\S+:|\S+){1,}\]";
            int iPos = strParaOrDataTmp.IndexOf("GroupSearch;");
            string strParaOrData = strParaOrDataTmp;
            if (iPos != -1)
                strParaOrData = strParaOrDataTmp.Substring("GroupSearch;".Length + iPos);
            string[] arrStrTmpData = strParaOrData.Split(new string[] { "][" }, StringSplitOptions.None);
            /// 第一部分是grouped的信息，第二部分是子表信息
            /// 
            if (arrStrTmpData.Length < 2) return false;
            iPos = arrStrTmpData[0].IndexOf('[');
            if (iPos < 0) return false;
            string tmpGroup = arrStrTmpData[0].Substring(iPos + 1);
            arrPart1 = new string[] { tmpGroup };

            iPos = arrStrTmpData[1].LastIndexOf(']');
            string strSubTableCellValues = arrStrTmpData[1].Substring(0, iPos);
            arrPart2 = strSubTableCellValues.Split(new string[] { ":" }, StringSplitOptions.None);

            return true;
        }

        internal override bool ParseData()
        {
            bool isOk = IsDataFormatRight();
            if (!isOk) return false;

            return ParseDataAndPara(this.SearchAndCickData, ref this.RowGroupedData, ref this.subTableCellsData);

            ////format is @"\[.*\]:\[(\S+:|\S+){1,}\]";
            //string[] arrStrTmpData = this.SearchAndCickData.Split(new string[] { "];[" }, StringSplitOptions.None);
            ///// 第一部分是grouped的信息，第二部分是子表信息
            ///// 
            //if (arrStrTmpData.Length < 2) return false;
            //int iPos = arrStrTmpData[0].IndexOf('[');
            //if (iPos < 0) return false;
            //string tmpGroup = arrStrTmpData[0].Substring(iPos + 1);
            //RowGroupedData = new string[] { tmpGroup };

            //iPos = arrStrTmpData[1].LastIndexOf(']');
            //string strSubTableCellValues = arrStrTmpData[1].Substring(0, iPos);
            //subTableCellsData = strSubTableCellValues.Split(new string[] { ":"},StringSplitOptions.None);

            //return true;
        }
    }

    /// <summary>
    /// 参数格式  [COLNAME1:COLNAME2:COLNAME3:COLNAME4:COLNAME5:....]
    /// DATA格式 data1;data2;data3;data4;data5;.........
    /// 参数格式
    ///  public const string CNST_SEARCHANDCLICK_MULTIPLE = @"MultipleSearch;\[\S+\];Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
    ///  public const string CNST_SEARCHANDCLICK_MLTPL_COND_IND = @"MultipleCondSearchWithIndx;\[\S+\];Index:1;Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
    ///  public const string CNST_SEARCHANDCLICK_MAXROW = @"MultipleLastSearch;\[\S+\];Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
    /// </summary>
    internal class SearchAndClickPara_Multiple : SearchAndClickParaWithData
    {
        internal string[] columns = null;
        internal bool isFormatRight = false;
        internal List<MARSInfragistics_GridColumn> columnsFromControl = new List<MARSInfragistics_GridColumn>();
        internal List<string> searchingDataList = new List<string>();
        internal List<object> matchedRows = new List<object>();
        internal object targetCell = null;
        internal int targetRowIdx = -1; // 如果不考虑行号，则为-1，否则为具体的满足条件的行号
        internal SearchAndClickPara_Multiple(string strParaWithBrackets, searchAndClickParaMode currrentMode= searchAndClickParaMode._marsAddinsMultilple)
        {
            currentType = currrentMode==searchAndClickParaMode._marsMultipleCondWithIndx? searchAndClickParaMode._marsMultipleCondWithIndx:
                searchAndClickParaMode._marsAddinsMultilple;
            isFormatRight = ParsePara(strParaWithBrackets);
        }

        internal bool ParseIndexValue(string strParaForIndex, ref string strError)
        {
            if (currentType != searchAndClickParaMode._marsMultipleCondWithIndx)
            {
                simpleLog.MarsLoggerSimple.Error("ParseIndexValue", strError = $"ParseIndexValue method should only be applied for _marsMultipleCondWithIndx, but current type is|{currentType}");
                return false;
            }
            int idx = strParaForIndex.IndexOf("index:", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                simpleLog.MarsLoggerSimple.Error("ParseIndexValue", strError =$"no index:xx part in parameter|{strParaForIndex}");
                return false;
            }
            string strIndexValue = strParaForIndex.Substring("index:".Length).Trim();
            if (string.IsNullOrEmpty(strIndexValue))
            {
                simpleLog.MarsLoggerSimple.Error("ParseIndexValue", strError = $"Index mode should contains `index:xx`|{strIndexValue}");
                return false;
            }
            if (!int.TryParse(strIndexValue ,out targetRowIdx))
            {
                simpleLog.MarsLoggerSimple.Error("ParseIndexValue", strError = $"index of Index mode should be number |{strIndexValue}|{strIndexValue}|");
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strParaWithBrackets">只有column和[]</param>
        /// <returns></returns>
        internal virtual bool ParsePara(string strParaWithBrackets)
        {
            if (string.IsNullOrEmpty(strParaWithBrackets)) return false;
            string tmpPara = strParaWithBrackets.Trim();
            if (!((tmpPara.StartsWith("[") && (tmpPara.EndsWith("]"))))) return false;
            tmpPara = tmpPara.Substring(1, tmpPara.Length - 2);
            columns = tmpPara.Split(new string[] { ":" }, StringSplitOptions.None);
            columnsFromControl.Clear();
            searchingDataList.Clear();
            matchedRows.Clear();
            return true;
        }

        internal void putColumnInfoWithIdx(int idx, object targetColumn, string strCaption, string strKey)
        {
            columnsFromControl.Add(new MARSInfragistics_GridColumn {
                columnCaption = strCaption,
                columnIdx = idx,
                columnObj = targetColumn,
                columnKey = strKey
            });
        }

        public override string ToString()
        {
            if (columns != null)
                return string.Join(",", columns);
            return "";
        }

        internal void setCompareData(string strData)
        {
            if (string.IsNullOrEmpty(strData)) return;
            try
            {
                searchingDataList.AddRange(strData.Split(new string[] { ";" }, StringSplitOptions.None));
            }
            catch (Exception)
            {

            }

        }

        internal bool IsDataMatch(string strCellText, int icolIdx)
        {
            if (icolIdx >= searchingDataList.Count) return false;
            if (string.IsNullOrEmpty(searchingDataList[icolIdx]))
                return string.IsNullOrEmpty(strCellText);
            return windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(searchingDataList[icolIdx], strCellText);
        }

        internal void addSelectedRow(object oOneRow)
        {
            matchedRows.Add(oOneRow);
        }


    }
    
    internal class SearchAndClickPara_Multiple_Indx: SearchAndClickPara_Multiple
    {
        internal int[] columnsIdx = null;
        internal SearchAndClickPara_Multiple_Indx(string strParaWithBrackets) : base(strParaWithBrackets)
        {
            if (isFormatRight)
            {
                columnsIdx = new int[columns.Length];
                for (int i = 0; i < columnsIdx.Length; i++)
                    columnsIdx[i] = -1;
            }
        }
        internal bool IsColumnIdxReady()
        {
            if (columnsIdx == null) return false;
            for (int i = 0; i < columnsIdx.Length; i++)
            {
                if (columnsIdx[i] < 0) return false;
            }
            return true;
        }
        internal void setColumnIdx(int iCol, int idx)
        {
            if ((columnsIdx == null) || (iCol >= columnsIdx.Length) || (iCol < 0)) return;
            columnsIdx[iCol] = idx;
        }
    }

    /// <summary>
    /// FOR CLICK_AT AND CLICK_ROW_HEADER
    /// </summary>
    internal class SearchAndClickAction_ClickAt
    {
        internal string sourcePara;
        public bool isClickHeader = false;
        int x, y;

        internal static SearchAndClickAction_ClickAt Parse(string strPara, ref string strError, ref string strAdv, ref string strStack)
        {
            if (string.IsNullOrEmpty(strPara)) return null;
            //if (!strPara.StartsWith(SearchAndClickForInfragisticsGrid.CNST_SEARCHANDCLICK_FORMStringComparison.OrdinalIgnoreCase)) {
            //    strError = $"parameter should start with [{SearchAndClickForInfragisticsGrid.CNST_SEARCHANDCLICK_FORMAT}], but it is [{strPara}]";
            //    strStack = MarsErrorStacks.StackTraceDump();
            //    strAdv = "Please check the parameter of steps";
            //    simpleLog.MarsLoggerSimple.Error("Parse", strError);
            //    return null;
            //}
            SearchAndClickAction_ClickAt rslt = new SearchAndClickAction_ClickAt();
            string[] arrExt = strPara.Split(':');
            if (string.Compare(arrExt[0], SearchAndClickForInfragisticsGrid.CNST_CLICK_ROW_HEADER, true) == 0)
            {
                rslt.isClickHeader = true;
            }
            rslt.sourcePara = strPara;
            simpleLog.MarsLoggerSimple.Info("SearchAndClickAction_ClickAt.Parse", $"is clickHeader Mode?|{rslt.isClickHeader}|");
            if (arrExt.Length != 3)
            {
                if (rslt.isClickHeader)
                {
                    // using default values
                    rslt.y = +5;
                    rslt.x = +5;
                    return rslt;
                }
                else
                {
                    strError = $"[{SearchAndClickForInfragisticsGrid.CNST_SEARCHANDCLICK_FORMAT}]'s format should be L|R_ClickAt:X:Y, but it is [{strPara}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Please check the parameter of steps";
                    simpleLog.MarsLoggerSimple.Error("Parse", strError);
                    return null;
                }
            }
            else
            {
                int tmpx, tmpy;
                if ((int.TryParse(arrExt[1], out tmpx)) && (int.TryParse(arrExt[2], out tmpy)))
                {
                    
                    rslt.x = tmpx;//,
                    rslt.y = tmpy;

                    return rslt;
                }
                else
                {
                    strError = $"[{SearchAndClickForInfragisticsGrid.CNST_SEARCHANDCLICK_FORMAT}]'s format should be L|R_ClickAt:X:Y (x,y are numbers), but it is [{strPara}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Please check the parameter of steps";
                    simpleLog.MarsLoggerSimple.Error("Parse", strError);
                    return null;
                }
            }
        }

        internal Point CalcPos(object oGrid, Rectangle oRect, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            if ((oGrid == null) || ((oGrid as System.Windows.Forms.Control) == null))
            {
                strError = "Grid object is null";
                strAdv = "please contact Marquis";
                strStack = strStack = MarsErrorStacks.StackTraceDump();
                return Point.Empty;
            }
            System.Windows.Forms.Control c = oGrid as System.Windows.Forms.Control;
            try
            {
                int tmpX = x + oRect.X + oRect.Width;
                int tmpY = y + oRect.Y;
                if (this.isClickHeader)
                {
                    tmpX = oRect.X + x;
                    tmpY = oRect.Y + y;
                }
                return c.PointToScreen(new Point(tmpX, tmpY));
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("CalcPos", strError = $"{e.Message}", e);
                strStack = e.StackTrace;
                strAdv = "Contact Marquis";
                isOk = true;
                return Point.Empty;
            }
        }
    }

    internal class SearchAndClickForInfragisticsGrid : SearchAndClick
    {
        public const string CNST_NOACTION = "NO_ACTION";
        public const string CNST_LEFT_CLICK = "LEFT_CLICK";
        public const string CNST_LEFT_DBL_CLICK = "LEFT_DBL_CLICK";
        public const string CNST_RIGHT_CLICK = "RIGHT_CLICK";
        public const string CNST_L_CLICKAT = "L_CLICKAT";
        public const string CNST_CLICK_ROW_HEADER = "L_CLICKROWHEADER";

        public const string CNST_SEARCHANDCLICK_FORMAT = @"MarsAddins;\S.*;Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLLLEFT_DBL_CLICK|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
        public const string CNST_SEARCHANDCLICK_LOOP_FORMAT = @"LoopAddins;\S.*;Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
        public const string CNST_SEARCHANDCLICK_MULTIPLE = @"MultipleSearch;\[\S+\];Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
        public const string CNST_SEARCHANDCLICK_MLTPL_COND_IND = @"MultipleCondSearchWithIndx;\[\S+\];Index:1;Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
        // data for MultipleSearch : data1;data2...
        public const string CNST_SEARCHANDCLICK_MAXROW = @"MultipleLastSearch;\[\S+\];Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
        public const string CNST_SEARCHANDCLICK_GROUPED = @"^GroupSearch;\[.*\]\[.*\];Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
        public const string CNST_SEARCHANDCLICK_ROWNUM  = @"^ROW_NUM;\d+;Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";

        public const string CNST_SEARCHANDCLICK_GROUPED_DATA = @"\[.*\]\[(\S+:|\S+){1,}\]";

        public static searchAndClickParaMode getCurrentParameterMode(string strPara)
        {
            try
            {
                if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(CNST_SEARCHANDCLICK_FORMAT, strPara))
                {
                    return searchAndClickParaMode._marsAddins;
                } else if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(CNST_SEARCHANDCLICK_LOOP_FORMAT, strPara))
                {
                    return searchAndClickParaMode._marsLoopAddins;
                } else if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(CNST_SEARCHANDCLICK_MULTIPLE, strPara))
                {
                    return searchAndClickParaMode._marsAddinsMultilple;
                } else if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(CNST_SEARCHANDCLICK_GROUPED, strPara))
                {
                    return searchAndClickParaMode._marsAddinsGrouped;
                } else if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(CNST_SEARCHANDCLICK_MAXROW, strPara))
                {
                    return searchAndClickParaMode._marsLastRow;
                } else if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(CNST_SEARCHANDCLICK_ROWNUM, strPara))
                {
                    return searchAndClickParaMode._marsRowNum;
                } else if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(CNST_SEARCHANDCLICK_MLTPL_COND_IND, strPara))
                {
                    return searchAndClickParaMode._marsMultipleCondWithIndx;
                }
                else
                    return searchAndClickParaMode._normal;
            } catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("getCurrentParameterMode", $"can't find mode|{strPara}|{e.Message}",e);
                return searchAndClickParaMode._wrongFormat;
            }
        }

        internal class SearchAndClickParamterInfo
        {
            internal const string cnst_reg = @"^RowNumber\:\d+\-[a-zA-Z0-9 :+\\-]+";
            internal int SearchMode;
            internal int RowNumber;
            internal string TargetColumnName;
            internal searchAndClickParaMode currrentMode = searchAndClickParaMode._marsAddins;
            internal static SearchAndClickParamterInfo IsRowNumberMode(string strPara,
                string strPegName, string strObjName, searchAndClickParaMode crntMode, string strData,
                ref bool isOk, ref string strError, ref string strStack, ref string strAdv)
            {
                simpleLog.MarsLoggerSimple.logBegin("IsRowNumberMode", string.Format("strPara:[{0}]", strPara));
                isOk = false;

                //只处理RowNO:3-ColumnToLocate 部分
                if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(cnst_reg, strPara))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "Incorrect format for grid cell location");// string.Format("strPara:[{0}] doesn't match:[{1}]", strPara, cnst_reg));
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "See user manual for correct grid location use";
                    return new SearchAndClickParamterInfo();
                }
                try
                {
                    //simpleLog.MarsLoggerSimple.Info("\t", string.Format("length:[{0}] strPara Lenght:[{1}]", "Rownumber:".Length, strPara.Length));
                    string strParaWithoutPrefix = strPara.Substring("Rownumber:".Length);
                    int iFirstDashPos = strParaWithoutPrefix.IndexOf('-');
                    //simpleLog.MarsLoggerSimple.Info("\t", string.Format("FirstDashPos:[{0}]", iFirstDashPos));
                    string strRowNumber = strParaWithoutPrefix.Substring(0, iFirstDashPos).Trim();
                    //simpleLog.MarsLoggerSimple.Info("\t", string.Format("strRowNumber:[{0}]", strRowNumber));
                    int iRowNumber;
                    if (!int.TryParse(strRowNumber, out iRowNumber))
                    {
                        simpleLog.MarsLoggerSimple.Error("IsRowNumberMode",
                            strError = "Incorrect format for grid cell location");//string.Format("Wrong format for number part:[src-{0}], number-[{1}]", strPara,strRowNumber));
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "See user manual for correct grid location use";
                        return null;
                    }
                    string strTargetColumn = strParaWithoutPrefix.Substring(iFirstDashPos + 1);

                    if (string.IsNullOrEmpty(strTargetColumn))
                    {
                        simpleLog.MarsLoggerSimple.Error("IsRowNumberMode",
                            string.Format("Target column should not be empty:[src-{0}]", strPara));
                        strError = "Keyword [SearchAndClick] does not support empty parameter";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Check the SearchAndClick and its parameter, see user manual";
                        return null;
                    }

                    isOk = true;
                    if (crntMode == searchAndClickParaMode._marsLoopAddins)
                    {
                        int tmpRowNumber;
                        if (!int.TryParse(strData, out tmpRowNumber))
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", strError = $"Incorrect format for Loop var parameters, a number is required, but [{strData}] found");
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Check Loop_var from DB";
                            return null;
                        }
                        isOk = true;
                        return new SearchAndClickParamterInfo()
                        {
                            currrentMode = crntMode,
                            RowNumber = tmpRowNumber,
                            SearchMode = 2
                        };
                    }
                    else
                    {
                        SearchAndClickParamterInfo result = new SearchAndClickParamterInfo()
                        {
                            SearchMode = 1,
                            RowNumber = iRowNumber,
                            TargetColumnName = strTargetColumn
                        };
                        return result;
                    }
                }
                catch (Exception e)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = $"Error while SearchAndClicking for a control [{strObjName}]", e);
                    strStack = e.StackTrace;
                    strAdv = "Unidentified error. If this continues, contact Marquis";
                    isOk = false;
                    return null;
                }

            }
        }



        /**
         * 2019 12 11 增加Scroll参数
         * */
        public override bool SearchAndClickFromControl(object oSourceControl, string strRC, string strData, 
            string strPegName, string strObjName, 
            ref string strError, ref string strAdv, ref string strStack)
        {
            /// 2021,3,22增加L_clickAT action
            /// 用来对应下拉列表,x:y用相对位置，从右边开始
            /// 

            /// LoopAddins;Our SSI Status;Action:L_ClickAt:-6:-6           

            simpleLog.MarsLoggerSimple.logBegin("SearchAndClickFromControl", string.Format("strRC:{0} strData:{1}", strRC, strData));
            try
            {
                string strTmpRC = strRC == null ? "" : strRC.Replace(" ", @"\s");
                searchAndClickParaMode currentMode = SearchAndClickForInfragisticsGrid.getCurrentParameterMode(strTmpRC);
                simpleLog.MarsLoggerSimple.Info("\t", $"currentMode after getCurrentParameterMode:[{currentMode}]");
                //if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(CNST_SEARCHANDCLICK_FORMAT, strTmpRC))
                if (currentMode == searchAndClickParaMode._wrongFormat)
                {
                    strError = string.Format("RC should be match format:[{0}], but it is :[{1}]", CNST_SEARCHANDCLICK_FORMAT, strRC);
                    strStack = Environment.StackTrace;
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError);
                    return false;
                }

                /// 修改后，loop模式和原模式结构类似，新增clickAt的Action
                string[] arrRCs = strRC.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                string strColumnName = "", // arrRCs[1],
                       strAction = null;   // arrRCs[2];
                int iRowNum = -1;
                switch (currentMode)
                {
                    case searchAndClickParaMode._marsRowNum:// only two para,after split by ';'
                        if (!int.TryParse(arrRCs[1], out iRowNum)){
                            strError = $"First parameter should be number if parameter is about rownumber. But it is :{strRC}";
                            strStack = Environment.StackTrace;
                            simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError);
                            return false;
                        }
                        strColumnName = "__ROW NUMBER MODE__";
                        strAction = arrRCs[2];
                        break;
                    case searchAndClickParaMode._marsMultipleCondWithIndx:
                        strColumnName = arrRCs[1];
                        strAction = arrRCs[3];
                        break;
                    default:
                        strColumnName = arrRCs[1];                        
                        strAction = arrRCs[2];
                        break;
                }

                string strClickCommand = strAction.ToUpper().Replace("ACTION:", "");

                SearchAndClickPara_Multiple conditionColumns_multiple = null;
                SearchAndClickParaWithData currentParaAndData = null;
                /// 20210805 albert mentioned that the last one could be latest version
                if ((currentMode == searchAndClickParaMode._marsAddinsMultilple)
                    || (currentMode == searchAndClickParaMode._marsLastRow)
                    || (currentMode == searchAndClickParaMode._marsMultipleCondWithIndx)
                    )
                {
                    conditionColumns_multiple = new SearchAndClickPara_Multiple(strColumnName, currentMode);
                    if (!conditionColumns_multiple.isFormatRight)
                    {
                        strError = $"SearchAndClick with multiple columns' parameter format is wrong. Format should be '[columnName1:columnName2:....]'";
                        strAdv = "Please change the parameter to right format ";
                        strStack = Environment.StackTrace;
                        simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError);
                        return false;
                    }
                    if (currentMode == searchAndClickParaMode._marsMultipleCondWithIndx)
                    {
                        bool isRightIndexMode = conditionColumns_multiple.ParseIndexValue(arrRCs[2], ref strError);
                        if (!isRightIndexMode)
                        {
                            strAdv = "Please ensure index setting is right";
                            strStack = Environment.StackTrace;
                            simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError);
                            return false;
                        }
                    }
                    currentParaAndData = conditionColumns_multiple;
                }

                
                
                if (currentParaAndData == null)
                {
                    currentParaAndData = SearchAndClickParaWithData.GetParaInstanceFactory(strRC);
                    if ((currentParaAndData != null) &&
                        (currentParaAndData.currentType == searchAndClickParaMode._marsAddinsGrouped))
                    {
                        currentParaAndData.SearchAndCickData = strData;
                        //处理 strData
                        if (!currentParaAndData.IsDataFormatRight())
                        {
                            strError = $"DataFormat [{strData}] of searchAndClick is wrong";
                            strStack = Environment.StackTrace;
                            strAdv = $"Change and fix SearchAndClick's Data -[{strData}]";
                            return false;
                        }
                        if (!(currentParaAndData as SearchAndClickParaWithData_Grouped).ParsePara())
                        {
                            strError = $"Parameter format [{strRC}] of searchAndClick is wrong";
                            strAdv = $"Change and fix SearchAndClick's Parameter -[{strRC}]";
                            strStack = Environment.StackTrace;
                            return false;
                        }
                        currentParaAndData.ParseData();

                    }
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
                        simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError = string.Format("unsupported type of object [{0}], only Misys.OpicsPlus.Framework.PresentationLayer.SecondaryWindows.ControlGridPanel and UltraGrid are supported", strTypes));
                        strError = $"Keyword SearchAndClick does not support object type for [{strObjName}]|{strTypes}";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Mars supports Infragistics, WinForm and WPF controls";
                        return false;
                    }
                }
                bool isNotExists = false;
                object oRows = ReflectorForCSharp.GetMember(oGrid, "Rows", ref isNotExists);
                if (isNotExists)
                {
                    strError = "Object property [Row] is NULL";// string.Format("No Rows exists in [{0}], wrong infragistis version?", strTypes);
                    strStack = $"no [Row] in type [{strTypes}]\r\n{MarsErrorStacks.StackTraceDump()}";
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
                    lend = DateTime.Now.Ticks;
                }
                if (iCount <= 0)
                {
                    strError = "Object property [Count]'s value is 0";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure the grid is available in screen and has data";
                    return false;
                }
                string strKey = "";
                bool isOkTmp = false;

                string strErrorTmp = "";
                string strAdvTmp = "", strStackTmp = "";
                int iColIdx = -1;

                /// 2019 新需求
                /// 可以指定行号，然后指定列名定位cell， 如 MarsAddins;RowNO:3-ColumnToLocate;Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK)
                /// loop 模式@"LoopAddins;\S.*;Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKAT:\S.*)";，其中data是行号
                /// 2021 新需求，grouped rows， parameter的示例:GroupSearch;[Pricing Group]:[Style:Trade Type:Option Style:Model Type:Model$];Action:Left_click
                ///     data的示例：[Pricing Group :  \(.*\)]:[YIELD:BASKOP:AMER:YIELD:LOGN]
                /// 2023 add new requirement about select row by click row header  action is :LEFT_CLICK_ROWHEADER
                bool isModeCheckOk = false;

                simpleLog.MarsLoggerSimple.Info("\t", string.Format("para to check mode:[{0}]", strColumnName));
                SearchAndClickParamterInfo searchParaInfo = SearchAndClickParamterInfo.IsRowNumberMode(strColumnName,
                    strPegName, strObjName, currentMode, strData,
                    ref isModeCheckOk, ref strError, ref strStack, ref strAdv);
                simpleLog.MarsLoggerSimple.Info("\t", $"is rowNumberMode? [{isModeCheckOk}]");
                //if (searchParaInfo == null) return false; 

                int iStartRow = 0, iEndRow = -1;
                if (isModeCheckOk && (searchParaInfo != null))
                {
                    strColumnName = searchParaInfo.TargetColumnName;
                    iStartRow = searchParaInfo.RowNumber;
                    iEndRow = iStartRow + 1;
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("row location mode, startRow:[{0}]-EndRow:[{1}]", iStartRow, iEndRow));
                }

                // 保留目标列
                MarsTableOperation tbOp = (new MarsTableOperation());
                tbOp.targetColumnForScrollCol = null;

                if (currentMode == searchAndClickParaMode._marsAddinsGrouped)
                {
                    if ((currentParaAndData as SearchAndClickParaWithData_Grouped) == null)
                    {
                        strError = "Grouped reference Object is null.";
                        strAdv = "Contact Marquis";
                        strStack = Environment.StackTrace;
                        return false;
                    }
                    return (currentParaAndData as SearchAndClickParaWithData_Grouped)
                        .dealWithSearchAndClick(oSourceControl, oRows, strData, strPegName, strObjName, strClickCommand,
                        ref strError, ref strAdv, ref strStack, false);
                }

#if _NET4
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                (oSourceControl as System.Windows.Forms.Control).Invoke(
#endif
                new Action(() =>
                {
                    if ((currentMode == searchAndClickParaMode._marsAddinsMultilple)
                    || (currentMode == searchAndClickParaMode._marsLastRow)
                    || (currentMode == searchAndClickParaMode._marsMultipleCondWithIndx))
                    {
                        if (conditionColumns_multiple == null)
                        {
                            strErrorTmp = "Can't get the right multiple parameter object when searching mode is for multiple columns";
                            strAdvTmp = "Contact Marquis";
                            strStackTmp = Environment.StackTrace;
                            isOkTmp = false;
                        }
                        else
                        {
                            isOkTmp = tbOp.GetColumnKeyForInfragisticsGrid(oSourceControl,
                                conditionColumns_multiple,
                                strPegName, strObjName,
                                ref strKey, ref iColIdx, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                        }
                    }
                    else
                        isOkTmp = tbOp.GetColumnKeyForInfragisticsGrid(oSourceControl, strColumnName, strPegName, strObjName, ref strKey, ref iColIdx, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);

                }));

                if ((currentMode != searchAndClickParaMode._marsAddinsMultilple)
                    && (currentMode != searchAndClickParaMode._marsLastRow)
                    && (currentMode != searchAndClickParaMode._marsMultipleCondWithIndx))
                {
                    if (tbOp.targetColumnForScrollCol == null)
                    {
                        simpleLog.MarsLoggerSimple.Error("\tSearchAndClickFromControl", "no targetColumnForScrollCol is returned");
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info("\tSearchAndClickFromControl", $"target column type:{tbOp.targetColumnForScrollCol.GetType()}");
                    }
                }
                else
                {
                    // 设置需要处理的数据
                    conditionColumns_multiple.setCompareData(strData);
                }

                strError = strErrorTmp;
                strAdv = strAdvTmp;
                strStack = strStackTmp;

                if (!isOkTmp) return false;
                object[] oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
                int iCnt = oAllRows == null ? -1 : oAllRows.Length;
                simpleLog.MarsLoggerSimple.Info("SearchAndClickFromControl", $"find rows:{iCnt}");
                object oTargetRow = null, oTargetCell = null;
                iEndRow = iEndRow == -1 ? ((oAllRows == null ? -1 : oAllRows.Length)) : iEndRow;
                //string strResult = "";
                string strAllTxt = "";

                if (currentMode == searchAndClickParaMode._marsLoopAddins)
                {
                    int iTmpStart = -1;
                    if (!int.TryParse(strData, out iTmpStart))
                    {
                        simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError = $"Loop idx is not a number [{strData}]");
                        strAdv = "Contact Marquis";
                        strStack = MarsErrorStacks.StackTraceDump();
                        return false;
                    }
                    iStartRow = iTmpStart;
                    iEndRow = iStartRow + 1;
                }
                bool isContinue = true;

                int i = iStartRow;
                if (currentMode == searchAndClickParaMode._marsLastRow)
                {
                    i = iEndRow - 1;
                    isContinue = (iEndRow > iStartRow) && (i > 0);

                }
                while (isContinue)
                {
                    try
                    {
                        //for (int i = iStartRow; i < iEndRow; i++)
                        //for (int i = iStartRow; i < (oAllRows == null ? -1 : oAllRows.Length); i++)
                        //{
                        simpleLog.MarsLoggerSimple.Info("\tSearchAndClickFromControl", $"current row no:[{i}]");
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
                        int iRowIdx = 0;
                        if ((currentMode == searchAndClickParaMode._marsAddinsMultilple)
                            || (currentMode == searchAndClickParaMode._marsLastRow)
                            || (currentMode == searchAndClickParaMode._marsMultipleCondWithIndx))
                        {
                            bool isRowMatched = true;
                            #region 获得多个compare列的数据
                            for (int icol = 0; icol < conditionColumns_multiple.columnsFromControl.Count; icol++)
                            {
                                var multipleColInfo = conditionColumns_multiple.columnsFromControl[icol];
                                if (multipleColInfo == null)
                                {
                                    strError = $"the #{icol + 1}column dosn't set";
                                    strAdv = "check Test step setting, and try again";
                                    strStack = Environment.StackTrace;
                                    simpleLog.MarsLoggerSimple.Error("\tSearchAndClickFromControl", strError);
                                    return false;
                                }
                                string strCellText = ReflectorForCSharp.GetMemberByType<string>(allCells[multipleColInfo.columnIdx], "Text");
                                bool isMatched = conditionColumns_multiple.IsDataMatch(strCellText, icol);
                                simpleLog.MarsLoggerSimple.Info("\t", $"to compare data:from grid [{strCellText}]-[{conditionColumns_multiple.searchingDataList[icol]}]");
                                if (!isMatched)
                                {
                                    simpleLog.MarsLoggerSimple.Info("SearchAndClickFromControl", $"'{strCellText}-[{icol}]' is not match--{strData}");
                                    isRowMatched = false;
                                    break;
                                }

                            }
                            if (isRowMatched)
                            {
                                iRowIdx += 1;
                                conditionColumns_multiple.addSelectedRow(oOneRow);
                                conditionColumns_multiple.targetCell = allCells[iColIdx];
                                if (currentMode == searchAndClickParaMode._marsLastRow)
                                {
                                    if (conditionColumns_multiple.matchedRows.Count >= 1)
                                    {
                                        simpleLog.MarsLoggerSimple.Info("\t", $"matched rows:[{conditionColumns_multiple.matchedRows.Count}]");
                                        //isRowMatched = true;
                                        break;//说明找到了
                                    }
                                }
                                else if (currentMode == searchAndClickParaMode._marsMultipleCondWithIndx)
                                {
                                    if (iRowIdx == conditionColumns_multiple.targetRowIdx)
                                    {
                                        simpleLog.MarsLoggerSimple.Info("\t", $"find target row by index:[{conditionColumns_multiple.targetRowIdx}]");
                                        break;//说明找到了
                                    }
                                }
                            }
                            
                            if ((conditionColumns_multiple.matchedRows.Count > 1)&&(currentMode!=searchAndClickParaMode._marsMultipleCondWithIndx))
                            {
                                strError = $"More than one rows match the [{strData}] for columns :[{conditionColumns_multiple.ToString()}]";
                                strAdv = "Change and fix paramters or data of related test steps.";
                                strStack = Environment.StackTrace;
                                return false;
                            }
                            continue;
                            #endregion
                        }

                        if (allCells.Length <= iColIdx)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Only [{0}] cells returns, but cell index is :[{1}]", allCells.Length, iColIdx));
                            return false;
                        }
                        if (allCells[iColIdx] != null)
                        {
                            if (currentMode == searchAndClickParaMode._marsLoopAddins)
                            {
                                // 如果是loop模式，data是行号，因此，第一行即是需要行
                                oTargetRow = oOneRow;
                                oTargetCell = allCells[iColIdx];
                                break;
                            }

                            string strCellText = ReflectorForCSharp.GetMemberByType<string>(allCells[iColIdx], "Text");
                            strAllTxt = $"{strAllTxt};[{strCellText}]";
                            if (isModeCheckOk && (searchParaInfo != null))
                            {
                                oTargetRow = oOneRow;
                                oTargetCell = allCells[iColIdx];
                                simpleLog.MarsLoggerSimple.Info("\t", $"isModeCheckOk Find #{i} currentMode:[{currentMode}] {strAllTxt}");
                                if (searchParaInfo.SearchMode == 1)
                                    break;
                            }
                            else
                            {
                                if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strData, strCellText) 
                                    || (string.Compare(strData, strCellText, true) == 0))
                                {
                                    oTargetRow = oOneRow;
                                    oTargetCell = allCells[iColIdx];
                                    simpleLog.MarsLoggerSimple.Info("\t", $"Find #{i} currentMode:[{currentMode}] -strData [{strData}] -[{strCellText}] - All [{strAllTxt}]");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (currentMode == searchAndClickParaMode._marsLoopAddins)
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        if (currentMode == searchAndClickParaMode._marsLastRow)
                        {
                            i-=1;
                            isContinue = i >= 0;
                        }
                        else
                        {
                            i+=1;
                            isContinue = i < iEndRow;
                        }
                    }
                }

                if ((currentMode == searchAndClickParaMode._marsAddinsMultilple)
                    || (currentMode == searchAndClickParaMode._marsLastRow)
                    || (currentMode == searchAndClickParaMode._marsMultipleCondWithIndx))
                {
                    if ((conditionColumns_multiple.matchedRows == null)
                        || (conditionColumns_multiple.matchedRows.Count <= 0))
                    {
                        oTargetRow = null;
                        oTargetCell = null;
                    }
                    else
                    {
                        oTargetRow = conditionColumns_multiple.matchedRows[0];
                        oTargetCell = conditionColumns_multiple.targetCell;
                        simpleLog.MarsLoggerSimple.Info("\t", $"matchedRows[0] {oTargetRow} currentMode:[{currentMode}]");
                    }
                }

                if (oTargetCell == null || oTargetRow == null)
                {
                    strError = "Can't find target cell"; //string.Format("no such value is found:[{0}]",strData);
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure grid cell identifiacation is correct";
                    simpleLog.MarsLoggerSimple.Info("\t", strAllTxt);
                    return false;
                }

                ReflectorForCSharp reflector = new ReflectorForCSharp();
                if ((string.Compare("SCROLL", strClickCommand, true) == 0)
                    ||(currentMode==searchAndClickParaMode._marsLastRow)
                    ||(currentMode == searchAndClickParaMode._marsMultipleCondWithIndx))

                {
                    //滚动该行
                    bool isNotExist = false;
                    var ActiveRowScrollRegion = ReflectorForCSharp.GetMember(oSourceControl, "ActiveRowScrollRegion", ref isNotExist);
                    if ((ActiveRowScrollRegion == null) || isNotExist)
                    {
                        string strTyps = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
                        simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't find ActiveRowScrollRegion from grid [{0}], wrong version?", strTyps));
                        strError = "Object property [ActiveRowScrollRegion] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    //
                    reflector.CallMethodByParaType(ActiveRowScrollRegion, "ScrollRowIntoView", new Type[] { oTargetRow.GetType() }, new object[] { oTargetRow });
                    System.Threading.Thread.Sleep(1000);
                    IntPtr lpdwResult;
                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                        //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)oSourceControl),((System.Windows.Forms.Control)oSourceControl).Handle),
                        ((System.Windows.Forms.Control)oSourceControl).Handle,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                        1000,
                        out lpdwResult);
                    if ((currentMode != searchAndClickParaMode._marsLastRow) 
                        &&(currentMode != searchAndClickParaMode._marsMultipleCondWithIndx))
                        return true;
                }
                if ((string.Compare("SCROLL_H", strClickCommand, true) == 0) || (string.Compare("SCROLL_COL", strClickCommand, true) == 0))
                {
                    bool isNotExist = false;
                    var ActiveColScrollRegion = ReflectorForCSharp.GetMember(oSourceControl, "ActiveColScrollRegion", ref isNotExist);
                    if ((ActiveColScrollRegion == null) || isNotExist)
                    {
                        string strTyps = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
                        simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't find ActiveRowScrollRegion from grid [{0}], wrong version?", strTyps));
                        strError = "Object property [ActiveRowScrollRegion] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    //
                    try
                    {
                        reflector.CallMethodByParaType(ActiveColScrollRegion, "ScrollColIntoView", new Type[] { tbOp.targetColumnForScrollCol.GetType() }, new object[] { tbOp.targetColumnForScrollCol });
                    }
                    catch (Exception e)
                    {
                        simpleLog.MarsLoggerSimple.Error("\tSearchAndClickFromControl", $"exception when call ScrollColIntoView\r\n:{e.Message}", e);
                        strError = e.Message;
                        strStack = e.StackTrace;
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    System.Threading.Thread.Sleep(1000);
                    IntPtr lpdwResult;
                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                        //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)oSourceControl),((System.Windows.Forms.Control)oSourceControl).Handle),
                        ((System.Windows.Forms.Control)oSourceControl).Handle,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                        1000,
                        out lpdwResult);
                    return true;
                }

                /// clickAT预处理
                /// 
                SearchAndClickAction_ClickAt clickAt = null;
                if ((!string.IsNullOrEmpty(strClickCommand))
                    && ((strClickCommand.StartsWith(CNST_L_CLICKAT, StringComparison.OrdinalIgnoreCase))
                    ||(strClickCommand.StartsWith(CNST_CLICK_ROW_HEADER, StringComparison.OrdinalIgnoreCase))
                    )
                    )
                {
                    clickAt = SearchAndClickAction_ClickAt.Parse(strClickCommand, ref strError, ref strAdv, ref strStack);
                    strClickCommand = CNST_L_CLICKAT;
                    if (strClickCommand.StartsWith(CNST_CLICK_ROW_HEADER, StringComparison.OrdinalIgnoreCase))
                        strClickCommand = CNST_CLICK_ROW_HEADER;
                    if (clickAt == null)
                    {
                        simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError);
                        return false;
                    }
                }

                object oCellUIElment = reflector.CallMethod(oTargetCell, "GetUIElement", new Type[] { }, ref isNotExists, null);
                if ((oCellUIElment == null) || (isNotExists))
                {
                    simpleLog.MarsLoggerSimple.Info("\t", "cell's UIElement is null");
                    oCellUIElment = reflector.CallMethod(oTargetRow, "GetUIElement", new Type[] { }, ref isNotExists, null);
                }
                if ((oCellUIElment == null) || (isNotExists))
                {
                    strError = "Object property UIElement is null";// "Can't get UIElment for both cell and row";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                System.Drawing.Rectangle oRect = reflector.GetMember<System.Drawing.Rectangle>(oCellUIElment, "Rect");
                simpleLog.MarsLoggerSimple.Info("\t", $"get Rect:[{oRect}]");
                System.Drawing.Rectangle clipRect = reflector.GetMember<System.Drawing.Rectangle>(oCellUIElment, "ClipRect");
                simpleLog.MarsLoggerSimple.Info("\t", $"get ClipRect:[{clipRect}]");
                if (oRect.Equals(default(System.Drawing.Rectangle)))
                {
                    simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError = "Object Rectangle property is null");//"No Rect object return,Wrong Infragistics Version?");
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                System.Drawing.Point pt = (oGrid as System.Windows.Forms.Control).PointToScreen(new System.Drawing.Point(oRect.X + oRect.Width / 2, oRect.Y + oRect.Height / 2));
                simpleLog.MarsLoggerSimple.Info("\t", $"get screen Point:[{pt}]");
                //string strClickCommand = strAction.ToUpper().Replace("ACTION:", "");
                switch (string.IsNullOrEmpty(strClickCommand) ? CNST_NOACTION : strClickCommand.ToUpper())
                {
                    case CNST_LEFT_CLICK:
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                        break;
                    case CNST_LEFT_DBL_CLICK:
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                        System.Threading.Thread.Sleep(50);
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                        break;
                    case CNST_RIGHT_CLICK:
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(pt.X, pt.Y);
                        break;
                    case CNST_L_CLICKAT:
                    case CNST_CLICK_ROW_HEADER:
                        pt = clickAt.CalcPos(oGrid, oRect, ref isOkTmp, ref strError, ref strAdv, ref strStack);
                        if (!isOkTmp) return false;
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                            new Action(() => {
                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                            })
                            );
                        
                        break;
                    case CNST_NOACTION:
                    default:
                        break;
                }
                return true;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("SearchAndClickFromControl", strError =e.Message, e);//"No Rect object return,Wrong Infragistics Version?");
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
        }
    }
}
