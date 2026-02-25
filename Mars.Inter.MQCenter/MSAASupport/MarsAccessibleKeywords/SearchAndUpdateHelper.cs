using Mars.Inter.MQCenter.MarsUtility;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.keywordOperation;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using Accessibility;
using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;
using Mars.message.Inter.MQCenter.interProcess;


namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    /// <summary>
    /// SearchAndUpdate的参数解析结果类，包含目标列信息，以及条件列信息，
    /// 也可以用在data的解析。
    /// </summary>
    public class MARS_SearchAndUpdateBasic
    {
        public string targetColumnName { get; set; }
        public List<MARSColumnsInfo> conditinColumns { get; set; } = new List<MARSColumnsInfo>();
    }
    public class MARS_SearchAndUpdateTable: MARS_SearchAndUpdateBasic
    {
    
        public List<MARSColumnsInfo> columns { get; set; } = new List<MARSColumnsInfo>();

        /// <summary>
        /// 检查condition列和target列是否在columns中都存在，返回target列的索引
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal bool CheckColumns()
        {
            if (columns == null || columns.Count <= 0) return false;
            var targetCol = columns.FirstOrDefault(p => string.Equals(p.columnKey, targetColumnName, StringComparison.OrdinalIgnoreCase));
            if (targetCol == null) return false;
            // conditinColumns必须全部在columns中存在
            foreach (var conditionCol in conditinColumns)
            {
                var col = columns.FirstOrDefault(p => string.Equals(p.columnKey, conditionCol.columnKey, StringComparison.OrdinalIgnoreCase));
                if (col == null) return false;
            }
            return true;
        }


    }

    public class SearchAndUpdateHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="keywordName"></param>
        /// <param name="targetElement"></param>
        /// <param name="pegName"></param>
        /// <param name="objName"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter">[conditionCol1:conditionCol2....];targetCol 例如：[Name];value  </param>
        /// <param name="strData">[conditionCol1Value:conditionCol2Value....];targetValue 例如：[FIGI];1234</param>        
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        internal static bool SearchAndUpdate(string keywordName, AutomationElement targetElement,
            string pegName, string objName,
            Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("SearchAndUpdate", $"{keywordName}|{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}|({pegName}.{objName}, {strParaMeter}, {strData})");
            bool isOk = false;
            try
            {
                if (dealResult == null)
                    dealResult = new MARSDealResult();

                MARS_SearchAndUpdateTable tableInfo = AnlaystParameter(strParaMeter, ref isOk, ref strError);
                if ((!isOk) || (tableInfo == null))
                {
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }
                /// 说明没有设置条件列，暂时不处理，因为没有合适的实例，后续如果有需要，可以增加功能
                if (tableInfo.conditinColumns.Count <= 0)
                {
                    isOk = false;
                    strError = $"no condition column specified, current version not support, please specify condition column in parameter";
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }
                /// 处理数据 [conditionCol1Value:conditionCol2Value....];targetValue
                MARS_SearchAndUpdateBasic opDataInfo = AnlaystParameter(strData, ref isOk, ref strError);
                if ((!isOk) || (opDataInfo == null))
                {
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|No Data is Set|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }
                /// 判断data对象中，condition列的数量必须和parameter中condition列的数量一致，并且顺序一致
                /// 
                int iCondtionDataColumnCnt = opDataInfo.conditinColumns==null?0: opDataInfo.conditinColumns.Count;
                int iCondtionColumnCnt = tableInfo.conditinColumns.Count;
                if (iCondtionDataColumnCnt != iCondtionColumnCnt)
                {
                    isOk = false;
                    strError = $"condition column count in data is not consistent with parameter, condition column count in parameter={iCondtionColumnCnt}, condition column count in data={iCondtionDataColumnCnt}";
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                /// 1，获得targetElement的handle
                int hwnd = targetElement.Current.NativeWindowHandle;
                /// 判断dictObjProperties中是否有 winclass的key， 
                isOk = DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "winclass", out string winclass);
                if (!isOk)
                {
                    /// 必须要有winclass
                    strError = $"FAILED, can not find winclass in obj properties";
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = strError;
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }
                /// 3，如果是CSCtrlGrille，从handle获得IAccessible接口
                /// 
                MARSAccessibleProvider accessibleProvider = new MARSAccessibleProvider();
                var targetAcc = accessibleProvider.GetAccessibleObject(new IntPtr(hwnd)) as IAccessible;
                if (targetAcc == null)
                {
                    strError = $"FAILED, can not get IAccessible from hwnd={hwnd}";
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = strError;
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                /// 4，使用IAccessible接口获得子对象，rolename必须是row，第一行是标题行（默认，如果不存在表头，需要在dictObjProperties中指定header=false）
                string roleNmae = MARSAccessibleProvider.GetRoleName(targetAcc);
                string nodeName = targetAcc.get_accName(0);
                string nodeValue = targetAcc.get_accValue(0);
#if DEBUG
                MarsLoggerSimple.Info("SearchAndUpdate", $"{iMark}|targetAcc role={roleNmae}|name:{nodeName}|value:{nodeValue}");
#endif
                int childCount = targetAcc.accChildCount;
                object[] children = new object[childCount];
                int obtained = MARSAccessibleProvider.AccessibleChildren(targetAcc, 0, childCount, children, out int nObtained);
                IAccessible targetTable = null;
                for (int i = 0; i < nObtained; i++)
                {
                    var child = children[i];
                    if (child is IAccessible childAcc)
                    {
                        string childRoleName = MARSAccessibleProvider.GetRoleName(childAcc);
                        string childNodeName = childAcc.get_accName(0);
                        string childNodeValue = childAcc.get_accValue(0);
#if DEBUG
                        MarsLoggerSimple.Info("SearchAndUpdate", $"{iMark}|targetAcc role={childRoleName}|name:{childNodeName}|value:{childNodeValue}");
#endif
                        if ("Table".Equals(childRoleName, StringComparison.OrdinalIgnoreCase))
                        {
                            targetTable = childAcc;
                            break;
                        }
                    }
                }

                if (targetTable == null)
                {
                    strError = $"Can't find table from {objName}|{roleNmae}|{nodeName}|{nodeValue}|";
                    isOk = false;
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}", Environment.StackTrace);
                    dealResult.ErrorMessage = $"FAILED, {strError}";
                    dealResult.ResultMessage = $"FAILED, {strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                /// 获得table的row
                /// 
                childCount = targetTable.accChildCount;
                children = new object[childCount];
                obtained = MARSAccessibleProvider.AccessibleChildren(targetTable, 0, childCount, children, out nObtained);
                bool isFirstRowObj = true;
                //List<string> currentCol = new List<string>();
                int iTargetIdx = -1;
                string strColumnWithAllRowsData = "";
                string strRowText = "";
                string conditionValues = string.Join(";",opDataInfo.conditinColumns);
                IAccessible targetCell = null;
                bool isTargetRowFound = false;
                string providerDesc = targetTable.accDescription[0];
                //bool isUsingIndex = providerDesc.IndexOf("MSAA Proxy", StringComparison.OrdinalIgnoreCase) >= 0;
                int left, top, width, height;
                for (int i = 0; i < nObtained; i++)
                {
                    var child = children[i];
                    if (child is IAccessible childAcc)
                    {
                        string childRoleName = MARSAccessibleProvider.GetRoleName(childAcc);
                        string childNodeName = childAcc.get_accName(0);
                        string childNodeValue = childAcc.get_accValue(0);
                        
#if DEBUG
                        MarsLoggerSimple.Info("SearchAndUpdate", $"{iMark}|childAcc role={childRoleName}|name:{childNodeName}|value:{childNodeValue}");
#endif
                        if ("row".Equals(childRoleName, StringComparison.OrdinalIgnoreCase))
                        {
                            /// 说明是 行对象
                            /// 
                            if (isFirstRowObj)
                            {
                                int iHeaderCnt = childAcc.accChildCount;
                                object[] header = new object[iHeaderCnt];
                                int obtainedHeader = MARSAccessibleProvider.AccessibleChildren(childAcc, 0, iHeaderCnt, header, out int nObtainedHeader);
#if DEBUG
                                MarsLoggerSimple.Info("SearchAndUpdate", $"{iMark}|find objects|{nObtainedHeader}--{iHeaderCnt}|");
#endif
                                // 获得column信息
                                for (int j = 0; j < nObtainedHeader; j++)
                                {
                                    if (header[j] is int coli)
                                    {
                                        /// 可能是child，也可能是value，name，优先判断value
                                        /// 
                                        var colv = childAcc.get_accValue(coli);
                                        var coln = childAcc.get_accName(coli);
                                        var col = childAcc.get_accChild(coli);
                                        if (!string.IsNullOrEmpty(colv))
                                        {
                                            //currentCol.Add(colv);
                                            tableInfo.columns.Add(new message.Inter.MQCenter.ThirdPartComponent.Infragistics.MARSColumnsInfo()
                                            {
                                                idxOfKey = coli,
                                                columnCaption = colv,
                                                columnKey = colv
                                            });
                                        }
                                        else if (!string.IsNullOrEmpty(coln))
                                        {
                                            //currentCol.Add(coln);
                                            tableInfo.columns.Add(new message.Inter.MQCenter.ThirdPartComponent.Infragistics.MARSColumnsInfo()
                                            {
                                                idxOfKey = coli,
                                                columnCaption = coln,
                                                columnKey = coln
                                            });
                                        }
                                        else
                                        {
                                            /// 暂时说明是错误，记录
                                            /// 
                                            isOk = false;
                                            strError = $"can't find column information from MARSVALUE OR MARSNAME";
                                            MarsLoggerSimple.Error("SearchAndUpdate", $"NOTICE!!!!!!!!{strError}");
                                            dealResult.ErrorMessage = strError;
                                            dealResult.ReturnedData = strError;
                                            dealResult.ResultMessage = $"FAILED,{strError}";

                                            return false;
                                        }

                                    }
                                }
                                isFirstRowObj = false;
                                /// 映射条件列索引和目标列索引
                                if (!tableInfo.CheckColumns())
                                {
                                    isOk = false;
                                    strError = $"condition column or target column not exist in table header, please check|target column={tableInfo.targetColumnName}|condition columns={string.Join(",", tableInfo.conditinColumns.Select(p => p.columnKey))}";
                                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                                    dealResult.ErrorMessage = strError;
                                    dealResult.ResultMessage = $"FAILED,{strError}";
                                    dealResult.AckTime = DateTime.Now;
                                    return false;
                                }

                                foreach (var condCol in tableInfo.conditinColumns)
                                {
                                    var matchedCol = tableInfo.columns.FirstOrDefault(p => string.Equals(p.columnKey, condCol.columnKey, StringComparison.OrdinalIgnoreCase));
                                    condCol.idxOfKey = matchedCol?.idxOfKey ?? -1;
                                }

                                var targetCol = tableInfo.columns.FirstOrDefault(p => string.Equals(p.columnKey, tableInfo.targetColumnName, StringComparison.OrdinalIgnoreCase));
                                iTargetIdx = targetCol?.idxOfKey ?? -1;
                                if (iTargetIdx < 0)
                                {
                                    strError = $"can't find target column index from header, target column={tableInfo.targetColumnName}";
                                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                                    dealResult.ErrorMessage = strError;
                                    dealResult.ResultMessage = $"FAILED,{strError}";
                                    dealResult.AckTime = DateTime.Now;
                                    return false;
                                }
                            }
                            else
                            {
                                /// 不是第一行，说明是数据行
                                /// 
                                int iDataCnt = childAcc.accChildCount;
                                object[] data = new object[iDataCnt];
                                int obtainedData = MARSAccessibleProvider.AccessibleChildren(childAcc, 0, iDataCnt, data, out int nObtainedData);

                                try
                                {
                                    /// 依据条件的列信息，获得目标列的值，同时和data中目标列的值进行比较，如果匹配，就继续下一个条件列
                                    /// 只要有一个条件列不匹配，就说明当前行不匹配，不需要继续比较，直接处理下一行
                                    /// 如果全部匹配，就说明当前行是目标行，可以进行更新操作，这时，需要把目标列对象的位置获取，用鼠标点击，然后键盘进行数据输入
                                    /// 这里有个问题，如果目标行不在viewport中，如何进行scroll？
                                    /// 
                                    bool isAllMatched = true;
                                    strRowText = "";

                                    for (int ii = 0; ii < tableInfo.conditinColumns.Count; ii++)
                                    {
                                        int conditionColIdx = tableInfo.conditinColumns[ii].idxOfKey;
                                        string conditionData = opDataInfo.conditinColumns[ii].columnKey;
                                        bool gotConditionCell = TryGetCellFromRow(childAcc, conditionColIdx, out var conditionCell, out var cellText);
                                        
                                        strRowText = $"{strRowText}|{cellText}";
                                        MarsLoggerSimple.Info("SearchAndUpdate", $"{iMark}|current condition column value={cellText}|conditionData={conditionData}|{strRowText}");
                                        /// 可能是正则表达式
                                        if (!gotConditionCell || !MarsWindowsAPIsExtend.RegularTest(conditionData, cellText ?? ""))
                                        {
                                            isAllMatched = false;
                                            break;
                                        }
                                    }
                                    if (!isAllMatched)
                                    {
                                        MarsLoggerSimple.Info("SearchAndUpdate", $"{iMark}|current row not matched|{conditionValues}|{strRowText}");
                                        continue;
                                    }

                                    // 当前行命中条件，获取目标列 cell
                                    bool gotTargetCell = TryGetCellFromRow(childAcc, iTargetIdx, out targetCell, out var targetCellText);
                                    if (!gotTargetCell )
                                    {
                                        strError = $"matched row found but cannot get target cell by column index={iTargetIdx}|targetColumn={tableInfo.targetColumnName}";
                                        MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}|{conditionValues}|{strRowText}");
                                        dealResult.ErrorMessage = strError;
                                        dealResult.ResultMessage = $"FAILED,{strError}";
                                        dealResult.AckTime = DateTime.Now;
                                        return false;
                                    }
                                    // 先确保目标行进入 viewport
                                    isOk = MARSAccessibleHelper.EnsureAccessibleObjInViewport(childAcc);
                                    if (!isOk)
                                    {
                                        strError = $"can't scroll to target row, maybe the control does not support scrolling or some error happened during scrolling, please check the log for details";
                                        MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}|\r\n{conditionValues}|{strRowText}");
                                        dealResult.ErrorMessage = strError;
                                        dealResult.ResultMessage = $"FAILED,{strError}";
                                        dealResult.AckTime = DateTime.Now;
                                        return false;
                                    }

                                    if (targetCell == null)
                                    {
                                        //说明需要直接调用 accLocation
                                        childAcc.accLocation(out left, out top, out width, out height, iTargetIdx);
                                    }
                                    else
                                    {

                                        // 再确保目标cell进入 viewport
                                        isOk = MARSAccessibleHelper.EnsureAccessibleObjInViewport(targetCell);
                                        if (!isOk)
                                        {
                                            strError = $"can't scroll to target cell, maybe the control does not support scrolling or some error happened during scrolling, please check the log for details";
                                            MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}|\r\n{conditionValues}|{strRowText}|targetCell={targetCellText}");
                                            dealResult.ErrorMessage = strError;
                                            dealResult.ResultMessage = $"FAILED,{strError}";
                                            dealResult.AckTime = DateTime.Now;
                                            return false;
                                        }

                                        /// 确保目标cell可操作
                                        isOk = MARSAccessibleHelper.IsAccessibleObjReady(targetCell, 10);
                                        if (!isOk)
                                        {
                                            strError = $"target cell is not ready for operation, maybe some error happened during scrolling or refreshing, please check the log for details";
                                            MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}|\r\n{conditionValues}|{strRowText}");
                                            dealResult.ErrorMessage = strError;
                                            dealResult.ResultMessage = $"FAILED,{strError}";
                                            dealResult.AckTime = DateTime.Now;
                                            return false;
                                        }


                                        targetCell.accLocation(out left, out top, out width, out height, 0);
                                    }
                                    isOk = MARSAccessibleHelper.OperateAccessibleObjByRect(left, top, width, height, opDataInfo.targetColumnName, true);
                                    if (!isOk)
                                    {
                                        strError = $"operate target row failed, maybe some error happened during operation, please check the log for details";
                                        MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}|\r\n{conditionValues}|{strRowText}");
                                        dealResult.ErrorMessage = strError;
                                        dealResult.ResultMessage = $"FAILED,{strError}";
                                        dealResult.AckTime = DateTime.Now;
                                        return false;
                                    }
                                    /// 发送 tab，表示确认
                                    /// 
                                    MARSAccessibleHelper.OperateAccessibleObjByRect(left, top, width, height, "{tab}", false);
                                    /// 目标列操作成功后，继续获取目标列的值，判断是否更新成功（获得刚才那个cell）
                                    /// 

                                    isTargetRowFound = true;
                                    break;
                                }
                                catch (Exception e)
                                {
                                    strError = $"getting data from target idx genereate exception:{e.Message}";
                                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}|{Environment.StackTrace}");
                                    dealResult.ErrorMessage = strError;
                                    dealResult.ResultMessage = $"FAILED,{strError}";
                                    dealResult.AckTime = DateTime.Now;
                                    dealResult.ActualInputData = $"strData,@column|{iTargetIdx}";
                                    return false;
                                }
                            }
                        }

                        if (i == 0)
                        {
                            /// 说明是第一行，默认是表头行，如果表头行不存在，需要在参数中指定header=false
                            /// 判断 tableInfo中 condiontionColumns和target列都必须在表头中存在
                            if (!tableInfo.CheckColumns())
                            {
                                isOk = false;
                                strError = $"condition column or target column not exist in table header, please check|target column={tableInfo.targetColumnName}|condition columns={string.Join(",", tableInfo.conditinColumns.Select(p => p.columnKey))}";
                                MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                                dealResult.ErrorMessage = strError;
                                dealResult.ResultMessage = $"FAILED,{strError}";
                                dealResult.AckTime = DateTime.Now;
                                return false;
                            }
                        }
                        else
                        {
                            /// 判断每行的数据
                        }
                    }
                }

                if (!isTargetRowFound)
                {
                    isOk = false;
                    strError = $"can't find matched target row by condition columns|{conditionValues}";
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                dealResult.ReturnedData = strColumnWithAllRowsData;
                dealResult.ResultMessage = "SUCCESS";
                dealResult.AckTime = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                MarsLoggerSimple.Error("SearchAndUpdate", $"{iMark}|{ex.Message}", ex);
                dealResult.ErrorMessage = $"FAILED, {strError}";
                dealResult.ResultMessage = $"FAILED, {strError}";
                dealResult.AckTime = DateTime.Now;
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("CaptureValueTable", $"{iMark}|returns|{isOk}");
            }

        }

        private static bool TryGetCellFromRow(IAccessible rowAcc, int cellIdx, out IAccessible cellAcc, out string cellText)
        {
            cellAcc = null;
            cellText = string.Empty;
            if (rowAcc == null || cellIdx < 0) return false;

            try
            {
                object rawCellObj = rowAcc.get_accChild(cellIdx);
                if (rawCellObj is IAccessible accCell)
                {
                    cellAcc = accCell;
                    try { cellText = accCell.get_accValue(0); } catch { }
                    if (string.IsNullOrEmpty(cellText))
                    {
                        try { cellText = accCell.get_accName(0); } catch { }
                    }
                }
                else
                {
                    try { cellText = rowAcc.get_accValue(cellIdx); } catch { }
                    if (string.IsNullOrEmpty(cellText))
                    {
                        try { cellText = rowAcc.get_accName(cellIdx); } catch { }
                    }
                }
            }
            catch
            {
                return false;
            }

            return (cellAcc != null) || !string.IsNullOrEmpty(cellText);
        }

        private static List<IAccessible> GetIAccessibleChildren(IAccessible accessible, ref bool isOk , ref string strError)
        {
            var children = new List<IAccessible>();
            try
            {               
                // 获取子对象
                int childCount = 0;
                try
                {
                    childCount = accessible.accChildCount;
                }
                catch
                {
                    return children;
                }

                if (childCount <= 0) return children;

                object[] childObjects = new object[childCount];
                int obtained = MARSAccessibleProvider.AccessibleChildren(accessible, 0, childCount, childObjects, out int nObtained);
                int iRole = MARSAccessibleProvider.Get_Role(accessible);//.get_accRole(0) as int;
                if (obtained != 0 || nObtained <= 0) return children;

                for (int i = 0; i < nObtained; i++)
                {
                    var childObj = childObjects[i];
                    if (childObj is not IAccessible)
                    {
                        if (childObj is int childIdx)
                        {
                            if ((iRole == MARSAccessibleConstans.ROLE_SYSTEM_PAGETABLIST)
                                || (iRole == MARSAccessibleConstans.ROLE_SYSTEM_PAGETAB))
                            {
                                // 这些控件的子对象是索引，需要特殊处理
                                var rect = MARSAccessibleProvider.getPageSubItemRect(accessible, childIdx, ref isOk, ref strError);
                                var subItmName = accessible.get_accName(childIdx);
                                FlashControlHelper.FlashRect(rect);
                                continue;
                            }
                            var childItm = accessible.get_accChild(childIdx);
                            if (childItm is IAccessible cAcc)
                            {
                                children.Add(cAcc);
                                /// 没有实现accLocation的对象，直接忽略
                                /// 
                                MarsLoggerSimple.Error("GetIAccessibleChildren", $"==========NOT IMPLEMENT======\r\nProcessing child index {childIdx} of role {iRole}\r\n|{Environment.StackTrace} ");
                            }
                        }
                        continue;
                    }
                    else
                    {
                        children.Add(childObj as IAccessible);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetIAccessibleChildren", $"Error getting IAccessible children: {ex.Message}", ex);
            }

            return children;
        }


        private static IAccessible FindTableContentControl(AutomationElement targetElement, ref bool isOk, ref string strError)
        {
            MarsLoggerSimple.logBegin("FindTableContentControl", $"FindTableContentControl|{targetElement.Current.Name}|{targetElement.Current.ControlType.ProgrammaticName}");
            try
            {
                var targetAccess = MarsMARSUIHelper.GetIAccessibleFromAutomationElement(targetElement);
                if (targetAccess == null)
                {
                    isOk = false;
                    strError = $"Can't get IAccessible from target element";
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{strError}|{targetElement.Current.Name}|{targetElement.Current.ControlType.ProgrammaticName}");
                    return null;
                }

                var accessProvide = new MARSAccessibleProvider();
                var tbl = accessProvide.FindTableFromUIAParentByHwnd(new IntPtr(targetElement.Current.NativeWindowHandle));
                if (tbl == null)
                {
                    /// 应该判断是否 targetElement本身是tablepattern或者grid pattern，但是，暂时不处理，因为没有合适的实例
                    isOk = false;
                    strError = $"Can't get hosted Table from target element. Target element could be gridpattern or tablepattern, but not implement";
                    MarsLoggerSimple.Error("SearchAndUpdate", $"{strError}|{targetElement.Current.Name}|{targetElement.Current.ControlType.ProgrammaticName}");
                    return null;
                }
                return tbl;
            }catch(Exception ex)
            {
                isOk = false;
                strError = $"FindTableContentControl generate exception:{ex.Message}";
                MarsLoggerSimple.Error("SearchAndUpdate", $"{strError}|{Environment.StackTrace}", ex);
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("FindTableContentControl", $"FindTableContentControl|returns|{isOk}");
            }
        }

        /// <summary>
        /// 解析参数，获得目标列信息，以及所有列的信息
        /// </summary>
        /// <param name="strParaMeter">[conditionCol1:conditionCol2....];targetCol 例如：[Name];value</param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static MARS_SearchAndUpdateTable AnlaystParameter(string strParaMeter, ref bool isOk, ref string strError)
        {
            MarsLoggerSimple.logBegin("AnlaystParameter", $"AnlaystParameter|strParaMeter={strParaMeter}");
            try
            {
                MARS_SearchAndUpdateTable tableInfo = new MARS_SearchAndUpdateTable();
                string[] arrPara = strParaMeter.Split(';');
                if (arrPara.Length != 2)
                {
                    isOk = false;
                    strError = $"parameter format error, should be [conditionCol1:conditionCol2....];targetCol, but current is {strParaMeter}";
                    MarsLoggerSimple.Error("AnlaystParameter", $"{strError}");
                    return null;
                }
                tableInfo.targetColumnName = arrPara[1].Trim();
                string conditionCols = arrPara[0].Trim();
                if (conditionCols.StartsWith("[") && conditionCols.EndsWith("]"))
                {
                    conditionCols = conditionCols.Substring(1, conditionCols.Length - 2);
                    string[] arrConditionCols = conditionCols.Split(':');
                    foreach (var col in arrConditionCols)
                    {
                        tableInfo.conditinColumns.Add(new MARSColumnsInfo() { columnKey = col.Trim(), columnCaption = col.Trim() });
                    }
                }
                else
                {
                    isOk = false;
                    strError = $"parameter format error, condition columns should be wrapped with [], but current is {conditionCols}";
                    MarsLoggerSimple.Error("AnlaystParameter", $"{strError}");
                    return null;
                }
                isOk = true;
                return tableInfo;
            }
            catch (Exception ex)
            {
                isOk = false;
                strError = $"AnlaystParameter generate exception:{ex.Message}";
                MarsLoggerSimple.Error("AnlaystParameter", $"{strError}|{Environment.StackTrace}", ex);
                return null;
            }
        }
    }
}
