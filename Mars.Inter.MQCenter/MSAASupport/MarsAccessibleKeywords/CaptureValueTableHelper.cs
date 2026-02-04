using Mars.Inter.MQCenter.MarsUtility;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using Accessibility;
using Mars.message.Inter.MQCenter.keywordOperation;
using System.Text.Json.Serialization;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    public class CaptureValueTableHelper
    {
        /// <summary>
        /// 捕获表格数值，使用IAccessible接口
        /// </summary>
        /// <param name="keywordName">该方法可以给captureValue，CaptureAndCompare使用，所以用keyword标识</param>
        /// <param name="targetElement"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="strParaMeter">格式：ALLROWS;</param>
        /// <param name="strData"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool CaptureValueTable(string keywordName, AutomationElement targetElement, 
            string pegName, string objName,
            Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, 
            string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("CaptureValueTable", $"{keywordName}|{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}|({pegName}.{objName}, {strParaMeter}, {strData})");
            bool isOk = false;
            try
            {
                if (dealResult == null)
                    dealResult = new MARSDealResult();

                MARS_CaptureTableCache tableInfo = MARS_CaptureTableCache.AnlaystParameter(strParaMeter, ref isOk, ref strError);
                if ((!isOk) || (tableInfo == null))
                {
                    MarsLoggerSimple.Error("CaptureValueTable", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                /// 算法：
                /// 1，获得targetElement的handle
                /// 2, 判断dictObjProperties中是否有 winclass的key， 判断值是否为CSCtrlGrille---注意，可能是systemlistview32
                /// 3，如果是CSCtrlGrille，从handle获得IAccessible接口
                /// 4，使用IAccessible接口获得子对象，rolename必须是row，第一行是标题行（默认，如果不存在表头，需要在dictObjProperties中指定header=false）
                /// 5, 遍历第一行的子对象（很可能是整数，通过索引模式获得各个列）
                /// 6，解析strPara，获得需要捕获的列名，在和#5获得的列名进行匹配，获得列索引
                /// 7，遍历所有行，获得对应列的值，拼接成字符串
                /// 

                /// 1，获得targetElement的handle
                int hwnd = targetElement.Current.NativeWindowHandle;
                /// 判断dictObjProperties中是否有 winclass的key， 
                isOk = DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "winclass", out string winclass);
                if (!isOk)
                {
                    /// 必须要有winclass
                    strError = $"FAILED, can not find winclass in obj properties";
                    MarsLoggerSimple.Error("CaptureValueTable", $"{iMark}|{strError}");
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
                    MarsLoggerSimple.Error("CaptureValueTable", $"{iMark}|{strError}");
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
                MarsLoggerSimple.Info("CaptureValueTable", $"{iMark}|targetAcc role={roleNmae}|name:{nodeName}|value:{nodeValue}");
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
                        MarsLoggerSimple.Info("CaptureValueTable", $"{iMark}|targetAcc role={childRoleName}|name:{childNodeName}|value:{childNodeValue}");
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
                    MarsLoggerSimple.Error("CaptureValueTable", $"{iMark}|{strError}", Environment.StackTrace);
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
                for (int i = 0; i < nObtained; i++)
                {
                    var child = children[i];
                    if (child is IAccessible childAcc)
                    {
                        string childRoleName = MARSAccessibleProvider.GetRoleName(childAcc);
                        string childNodeName = childAcc.get_accName(0);
                        string childNodeValue = childAcc.get_accValue(0);
#if DEBUG
                        MarsLoggerSimple.Info("CaptureValueTable", $"{iMark}|childAcc role={childRoleName}|name:{childNodeName}|value:{childNodeValue}");
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
                                MarsLoggerSimple.Info("CaptureValueTable", $"{iMark}|find objects|{nObtainedHeader}--{iHeaderCnt}|");
#endif
                                // 获得column信息
                                for (int j =0;j< nObtainedHeader; j++)
                                {
                                    if (header[j] is int coli)
                                    {
                                        /// 可能是child，也可能是value，name，优先判断value
                                        /// 
                                        var colv = childAcc.get_accValue(coli);
                                        var coln = childAcc.get_accValue(coli);
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
                                            MarsLoggerSimple.Error("CaptureValueTable", $"NOTICE!!!!!!!!{strError}");
                                            dealResult.ErrorMessage = strError;
                                            dealResult.ReturnedData = strError;
                                            dealResult.ResultMessage = $"FAILED,{strError}";
                                            return false;
                                        }

                                    }
                                }
                                isFirstRowObj = false;

                                /// 判断是否存在指定的column
                                /// 
                                iTargetIdx = tableInfo.CheckTargetColumnInfo(ref strError, ref isOk);
                                if ((!isOk) || (iTargetIdx < 0))
                                {
                                    MarsLoggerSimple.Error("CaptureValueTable", $"{iMark}|{strError}");
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

                                /// 这里未必正确，因为索引值未必就是获取的值。
                                //if (iTargetIdx >= nObtainedData)
                                //{
                                //    /// 说明数据列不够
                                //    /// 
                                //    isOk = false;
                                //    strError = $"data row not enough column, target idx={iTargetIdx}|total={nObtainedData}";
                                //    MarsLoggerSimple.Error("CaptureValueTable", $"{iMark}|{strError}");
                                //    dealResult.ErrorMessage = strError;
                                //    dealResult.ResultMessage = $"FAILED,{strError}";
                                //    dealResult.AckTime = DateTime.Now;
                                //    return false;
                                //}
                                try
                                {
                                    var cellV = childAcc.get_accValue(iTargetIdx);
                                    if (string.IsNullOrEmpty(strColumnWithAllRowsData))
                                    {
                                        strColumnWithAllRowsData = cellV;
                                    }
                                    else
                                    {
                                        strColumnWithAllRowsData = $"{strColumnWithAllRowsData}\r\n{cellV}";
                                    }
                                }catch (Exception e)
                                {
                                    strError = $"getting data from target idx genereate exception:{e.Message}";
                                    MarsLoggerSimple.Error("CaptureValueTable", $"{iMark}|{strError}|{Environment.StackTrace}");
                                    dealResult.ErrorMessage = strError;
                                    dealResult.ResultMessage = $"FAILED,{strError}";
                                    dealResult.AckTime = DateTime.Now;
                                    dealResult.ActualInputData = $"strData,@column|{iTargetIdx}";
                                    return false;
                                }
                            }
                        }
                    }
                }
                
                dealResult.ReturnedData = strColumnWithAllRowsData;
                dealResult.ResultMessage = "SUCCESS";
                dealResult.AckTime = DateTime.Now;
                return true; 
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                MarsLoggerSimple.Error("CaptureValueTable", $"{iMark}|{ex.Message}", ex);
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
    }
}
