using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.AutoTestingDriver.SystemUtil.DataStructure;
using Mars.message.Inter.MQCenter.HttpRestService;
using Mars.Inter.MQCenter.Properties;
using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Mars.Inter.MQCenter.DataStructure;
using Mars.message.windowsWrapper.SystemUtil;
using Mars.Inter.MQCenter.ThirdPartComponent.Infragistics;

namespace Mars.message.Inter.MQCenter.keywordOperation
{

    /// <summary>
    /// 当有captureAndCompare 为table时候，第一条创建该对象，保留columns,和rows
    /// </summary>
    public class MARS_CaptureTableCache
    {
        public bool isBatchMode { get;set; }
        public string currentTableShortName { get; set; }
        public string currentTableParenetName { get; set; }
        public List<MARSColumnsInfo> columns { get; set; }
        public object Rows;
        public string varName { get; set; }
        public string targetColumn { get; set; }
        public string targetVar { get; set; }

        public DataTable dataTable = new DataTable();
        public bool IsRightBatchMode(string strPara)
        {
            varName = null;
            isBatchMode = false;
            Regex r = new Regex(CaptureValueForSwfTable.cnst_batch_toVar);
            Match match = r.Match(strPara);
            if (!match.Success) return false;
            varName = match.Groups[0].Value;
            return isBatchMode = true;
        }

        public bool IsEndBatch(string strPara)
        {
            Regex r = new Regex(CaptureValueForSwfTable.cnst_batch_toVarEnd);
            
            bool isEndMode = r.IsMatch(strPara);
            if (!isEndMode) return false;
            isBatchMode=false;
            return true;
        }

        internal static string DataTableSystemTextJson(DataTable dataTable)
        {
            if (dataTable == null)
            {
                return string.Empty;
            }

            var data = dataTable.Rows.OfType<DataRow>()
                        .Select(row => dataTable.Columns.OfType<DataColumn>()
                            .ToDictionary(col => col.ColumnName, c => row[c]));

            return System.Text.Json.JsonSerializer.Serialize(data);
        }

        public string ToJsonString()
        {
            return DataTableSystemTextJson(this.dataTable);
        }

		/// <summary>
		/// 解析参数，当前仅支持格式：ALLROWS;columnName
		/// 设置 isBatchMode=false，并将 columnName 放入 columns 列表
		/// </summary>
		/// <param name="strPara">参数字符串，例如：ALLROWS;Deal No.</param>
		/// <param name="isok">返回是否成功</param>
		/// <param name="strError">返回错误信息</param>
		/// <returns>成功返回当前实例，失败返回 null</returns>
		public static MARS_CaptureTableCache AnlaystParameter(string strPara, ref bool isok, ref string strError)
		{
			isok = false;
			strError = null;
			try
			{
				if (string.IsNullOrWhiteSpace(strPara))
				{
					strError = "Parameter is empty";
					return null;
				}

				string[] parts = strPara.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2)
				{
					strError = $"Unsupported parameter format: {strPara}. Expect: ALLROWS;columnName";
					return null;
				}

				string mode = parts[0].Trim();
				string columnName = parts[1].Trim();

				if (!mode.Equals("ALLROWS", StringComparison.OrdinalIgnoreCase))
				{
					strError = $"Only ALLROWS mode is supported currently, got: {mode}";
					return null;
				}

				if (string.IsNullOrEmpty(columnName))
				{
					strError = "columnName is empty";
					return null;
				}

				// create instance and populate
				var inst = new MARS_CaptureTableCache();
				inst.isBatchMode = false;
                inst.targetColumn = columnName;
                inst.columns = new List<MARSColumnsInfo>();
				
				isok = true;
				return inst;
			}
			catch (Exception ex)
			{
				strError = ex.Message;
				return null;
			}
		}

        internal int CheckTargetColumnInfo(ref string strError, ref bool isOk)
        {
            if (string.IsNullOrEmpty(this.targetColumn))
            {
                strError = "Target column is not set";
                isOk = false;
                return -1;
            }
            var c = this.columns.Where(p => MarsWindowsAPIsExtend.RegularTest(this.targetColumn, p.columnCaption)
                || MarsWindowsAPIsExtend.RegularTest(this.targetColumn, p.columnKey))
                .ToList();
            if (c.Count > 1)
            {
                strError = $"multiple columns are found {this.targetColumn}| in |{string.Join(";", c)}";
                isOk = false;
                return -1;
            }
            if (c.Count == 0)
            {
                strError = $"no such column is found|{this.targetColumn}| in |{string.Join(";", this.columns.Select(p => p.columnCaption))}";
                isOk = false;
                return -1;
            }
            isOk = true;
            return c[0].idxOfKey;
        }
    }

    class ConditonSubParameterInfo
    {
        internal string filter;
        internal string conditionColumn;
        internal int    columnKeyId;

        internal string columnCaption = "";
        internal string columnKey     = ""; 

        internal bool ParseOneItem(string strSrc)
        {
            int iEqualPos = strSrc.IndexOf("=");
            conditionColumn = strSrc.Substring(0, iEqualPos);
            filter = strSrc.Substring(iEqualPos + 1);
            if ((string.IsNullOrEmpty(conditionColumn)) || (string.IsNullOrEmpty(filter))) return false;
            return true;
        }

        internal string cellValue;

        internal bool isMatched()
        {
            if (string.IsNullOrEmpty(this.filter)) return false;
            if (string.IsNullOrEmpty(cellValue))   return false;
            return Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(this.filter, cellValue) ;
        }
    }
    class ConditionParameterInfo
    {
        internal List<ConditonSubParameterInfo> subParameters = new List<ConditonSubParameterInfo>();
        internal ConditonSubParameterInfo targetColumnInfo = new ConditonSubParameterInfo();
        internal string currentParameter;
        //internal string conditionColumn;
        internal string targetColulmn;
        internal string filter;
        internal ConditionParameterInfo(string strPara)
        {
            currentParameter = strPara;
        }
        /// <summary>
        /// 多条件模式
        /// CONDITIONALL:@COLUMN1=DATA@COLUMN2=DATA2@.....;TARGETCOLUMN_NAME
        /// </summary>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal bool isRightFormat(ref string strError)
        {
            if (string.IsNullOrEmpty(currentParameter))
            {
                strError = "empty Condtion parameter";
                return false;
            }

            int iPos = currentParameter.IndexOf(":");
            if (iPos < 0)
            {
                strError = string.Format("condition string is with wrong fomatter. [{0}]", currentParameter);
                return false;
            }
            string conditionPrefix = currentParameter.Substring(0, iPos);
            if (string.Compare("CONDITIONALL", conditionPrefix, true) != 0)
            {
                strError = string.Format("condition prefix string is wrong. [{0} subed]", currentParameter);
                return false;
            }            

            int iPosSemiComma = currentParameter.LastIndexOf(";");
            
            string strFilterWithColumn = currentParameter.Substring(iPos + 1, iPosSemiComma - iPos - 1);
            /// right now, only @COLUMN1=DATA@COLUMN2=DATA2@..... left. 
            /// or just one row
            /// 
            string[] subConditions = strFilterWithColumn.Split(new char[] { '@' });
            subParameters.Clear();
            foreach(var itm in subConditions)
            {
                if (string.IsNullOrEmpty(itm)) continue;
                ConditonSubParameterInfo oneSubPara = new ConditonSubParameterInfo();
                if (!oneSubPara.ParseOneItem(itm))
                {
                    simpleLog.MarsLoggerSimple.Error("isRightFormat",strError = @"Condition format is wrong. [{currentParameter}]");
                    return false;
                }
                subParameters.Add(oneSubPara);
            }

            //int iEqualPos = strFilterWithColumn.IndexOf("=");
            //conditionColumn = strFilterWithColumn.Substring(0, iEqualPos);
            //filter = strFilterWithColumn.Substring(iEqualPos + 1);
            targetColulmn = currentParameter.Substring(iPosSemiComma + 1);
            targetColumnInfo.conditionColumn = targetColulmn;

            //if (string.IsNullOrEmpty(conditionColumn) || string.IsNullOrEmpty(filter)
            //    || string.IsNullOrEmpty(targetColulmn))
            //{
            //    strError = string.Format("string is wrong wiht one part is empty or null. [{0}]", currentParameter);
            //    return false;
            //}
            if ((subParameters.Count == 0)||(string.IsNullOrEmpty(targetColulmn)))
            {
                simpleLog.MarsLoggerSimple.Error("isRightFormat", strError = @"no condition is set or target column is not set");
                return false;
            }
            return true;
        }

        public string getConditionColumnsName()
        {
            return subParameters == null ? "" : string.Join(";", subParameters.Select(p => p.conditionColumn));
        }
    }

    public abstract class CaptureValue
    {
        public abstract string CaptureValueFromControl(object oSourceControl, string strParameter, string strPegName, string strObjName,
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack,
            string strKeyword="CaptureValue");
    }

    /// <summary>
    /// 通过memberIndex获得对象
    /// </summary>
    public class CaptureCommonValue : CaptureValue
    {
        public string memberIndex { get; set; }
        public override string CaptureValueFromControl(object oSourceControl, string strParameter, string strPegName, 
            string strObjName, ref bool isOk, ref string strError, ref string strAdv, ref string strStack, 
            string strKeyword = "CaptureValue")
        {
            simpleLog.MarsLoggerSimple.logBegin("CaptureValueFromControl", $"memberIndex|{memberIndex}|para|{strParameter}|keyword|{strKeyword}");
            if (string.IsNullOrEmpty(memberIndex))
            {
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError="member index is null, please set the member index first");
                isOk = false;
                strAdv = "Internal Error, please contact Marquis";
                return null;
            }
            if (oSourceControl == null)
            {
                isOk = false;
                strError = $"No object is passed, looks, no such object is found.";
                simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return null;
            }
            if (oSourceControl is System.Windows.Forms.Control)
            {
                System.Windows.Forms.Control c = oSourceControl as System.Windows.Forms.Control;
                bool isMemberExist = false;
                string strTargetMemberInfo = "";
                ReflectorForCSharp fl = new ReflectorForCSharp();
                try
                {
                    if (c.InvokeRequired)
                    {
                        c.Invoke(new Action(() => {                            
                            strTargetMemberInfo = fl.GetMember<string>(c, memberIndex, ref isMemberExist);                            
                        }));
                    }
                    else
                    {
                        strTargetMemberInfo = fl.GetMember<string>(c, memberIndex, ref isMemberExist);
                    }
                    if (!isMemberExist)
                    {
                        isOk = false;
                        strError = $"No such member |{memberIndex}| exists in type |{oSourceControl.GetType()}";
                        strAdv = "Please try to set different member name or parameter info for Test step";
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError);
                        return null;
                    }
                    isOk = true;
                    return strTargetMemberInfo;
                }
                catch(Exception e)
                {
                    isOk = false;
                    strError = $"internal Error when get |{memberIndex}| from type |{oSourceControl.GetType()}";
                    strAdv = "Please check the log file for details";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError);
                    return null;
                }
            }
            else
            {
                object o = ReflectorForCSharp.GetMember(oSourceControl, memberIndex);
                if (o == null)
                {
                    isOk = false;
                    strError = $"Can't find text property of object [{strPegName}].[{strObjName}]. \r\nPlease make sure that the |{memberIndex}| belongs to |{oSourceControl.GetType()}";
                    simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError);
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }
                isOk = true;
                return o.ToString();
            }
        }

    }

    internal class CaptureValueForSwfEdit : CaptureValue
    {
        public override string CaptureValueFromControl(object oSourceControl, string strParameter, string strPegName, string strObjName, 
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack, string strKeyword = "CaptureValue")
        {
            if (oSourceControl == null)
            {
                isOk = false;
                strError = "Passing null object to a function";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return null;
            }
            object o = ReflectorForCSharp.GetMember(oSourceControl, "Text");
            if (o == null)
            {
                isOk = false;
                strError = $"Can't find text property of object [{strPegName}].[{strObjName}]";//string.Format("No Text member for that object:[{0}]",oSourceControl.GetType().ToString());
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return null;
            }
            isOk = true;
            return o.ToString();
        }
    }
    
    internal class CaptureValueForSwfLabel : CaptureValue
    {
        public override string CaptureValueFromControl(object oSourceControl, string strParameter, string strPegName, string strObjName, 
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack, string strKeyword = "CaptureValue")
        {
            if (oSourceControl == null)
            {
                isOk     = false;
                strError = "Passing null object to a function";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv   = "Contact Marquis";
                return   null;
            }
            ///wait for 1 minutes until the length is greate than 0
            /// 
            long lstart = DateTime.Now.Ticks;
            long lend = lstart;
            object o = null;
            while (((lend - lstart) / TimeSpan.TicksPerSecond) < 60)
            {
                o = ReflectorForCSharp.GetMember(oSourceControl, "Text");
                if (o == null)
                {
                    isOk            = false;
                    strError        = $"Can't find text property of object [{strPegName}].[{strObjName}]";//string.Format("No Text member for that object:[{0}]",oSourceControl.GetType().ToString());
                    StackFrame stck = (new StackFrame());
                    strStack        = MarsErrorStacks.StackTraceDump();
                    strAdv          = "Contact Marquis";
                    return null;
                }
                else
                {
                    if (!string.IsNullOrEmpty(o.ToString()))
                    {
                        return o.ToString();
                    }
                }
                System.Threading.Thread.Sleep(50);
                lend = DateTime.Now.Ticks;
            }
            isOk = true;
            return o == null ? "" : o.ToString();
        }
    }

    internal class CaptureValueForSwfComboboxInfra : CaptureValue
    {
        public override string CaptureValueFromControl(object oSourceControl, string strParameter, string strPegName, string strObjName, ref bool isOk, ref string strError, ref string strAdv, ref string strStack, string strKeyword = "CaptureValue")
        {
            if (oSourceControl == null)
            {
                isOk     = false;
                strError = "Passing null to a function";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv   = "Contact Marquis";
                return   null;
            }
            string strTypes = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
            //"Infragistics.Win.UltraWinEditors.UltraComboEditor",
            //"Summit.Framework.View.DDownControl",
            bool isCommonInfraEditor = false;
            if (strTypes.Contains("Infragistics.Win.UltraWinEditors.UltraComboEditor")
                || (strTypes.Contains("Infragistics.Win.UltraWinGrid.UltraCombo"))
                || (string.Compare(oSourceControl.GetType().ToString(), "Infragistics.Win.UltraWinEditors.UltraComboEditor", true) == 0)
                || (isCommonInfraEditor=(oSourceControl.GetType().FullName.IndexOf("Infragistics.Win.UltraWinEditors.")>=0)))
            {
                bool isObjectExists = false;
                object ot = ReflectorForCSharp.GetMember(oSourceControl, "Text", ref isObjectExists);
                if (isObjectExists)
                {
                    bool isWrong = false;
                    if (isCommonInfraEditor)
                    {
                        // try to get caption
                        ot = ReflectorForCSharp.GetMember(oSourceControl, "Caption", ref isObjectExists);
                        if (isObjectExists)
                        {
                            isOk = false;
                            strError = $"Both object properties Text and Caption are NULL|{oSourceControl.GetType().FullName}";// "No Text member exists for Infragistics.Win.UltraWinEditors.UltraComboEditor, not a version from Infragistics?";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            return null;
                        }
                        else
                            isWrong = true;
                    }
                    if (isWrong)
                    {
                        isOk = false;
                        strError = $"Object property Text is NULL|{oSourceControl.GetType().FullName}";// "No Text member exists for Infragistics.Win.UltraWinEditors.UltraComboEditor, not a version from Infragistics?";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return null;
                    }
                }
                 
                isOk = true;
                if (ot == null)
                {
                    return "";
                }
                return ot.ToString();
            }
            if (oSourceControl is System.Windows.Forms.ComboBox)
            {
                System.Windows.Forms.ComboBox oc = oSourceControl as System.Windows.Forms.ComboBox;
                isOk = true;
                return oc.Text;
            }

            List<MarsKeywordOP> lstOfKeywordOp = null;
            KeywordControlTypeMapping keywordMapInfo = KeywordControlTypeMappingMgmt.GetKeywordOp(strKeyword);
            if (keywordMapInfo !=null)
            {
                lstOfKeywordOp = keywordMapInfo.keyworkdOP;
            }
            isOk = false;
            strError = $"CaptureValue and CaptureAndCompare do not support object type for [{strPegName}].[{strObjName}]|{oSourceControl.GetType().FullName}|";//string.Format("Unsupported types [{0}] for capturevalue keyword", oSourceControl.GetType().ToString());
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Mars supports Infragistics, WinForm and WPF controls.";
            return null;
        }
    }

    internal class CaptureValueForSwfListView : CaptureValue
    {
        public override string CaptureValueFromControl(object oSourceControl, string strParameter, string strPegName, string strObjName, ref bool isOk, ref string strError, ref string strAdv, ref string strStack, string strKeyword = "CaptureValue")
        {
            if (oSourceControl == null)
            {
                strError = "Passing null object to a function";//"control parameter is null";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            string strTyps = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
            if ((oSourceControl as Control) == null)
            {
                simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError = string.Format("object is not control, but :[{0}]", strTyps));
                strError = $"Object [{strPegName}].[{strObjName}] is not a Control";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            if (strTyps.Contains("UltraListView"))
            {
                return new MarsListViewOperation().CaptureValues((Control)oSourceControl, strParameter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
            }
            else
            {
                strError = string.Format("Only UltraListView is supported for Capturevalue/CaptureAndCompare, but it is:[{0}]", strTyps);
                simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError);
                strError = "Only standard ListView and UltraListView are supported for CaptureValue/CaptureAndCompare";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure control is of either ListView or UltraListview type.";
                isOk = false;
                return null;
            }
        }
    }

    internal class CaptureValueForSwfStatusBar : CaptureValue
    {
        public override string CaptureValueFromControl(object oSourceControl, string strCapturePara, string strPegName, string strObjName, ref bool isOk, ref string strError, ref string strAdv, ref string strStack, string strKeyword = "CaptureValue")
        {
            string strTypes = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
            bool isNotExists = false;
            if (strTypes.Contains("Infragistics.Win.UltraWinStatusBar.UltraStatusBar"))
            {
                /** code fromQTP
                 * DATA_TC = objDes.object.Panels.item(0).text
			        Data_TC = RTrim(Data_TC)*/
                object panels = ReflectorForCSharp.GetMember(oSourceControl, "Panels", ref isNotExists);
                if (isNotExists)
                {
                    simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError = string.Format("No panels exists in [{0}]", strTypes));
                    strError = $"Control [{strPegName}].[{strObjName}] does not have Panel property";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure control is a UltraStatusBar";
                    isOk = false;
                    return null;
                }
                object[] oAllInPanels = ReflectorForCSharp.GetMemberByType<object[]>(panels, "All");
                if ((oAllInPanels == null) || (oAllInPanels[0] == null))
                {
                    isOk = true;
                    return "";
                }

                object otxt = ReflectorForCSharp.GetMember(oAllInPanels[0], "Text", ref isNotExists);
                if (isNotExists)
                {
                    simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError = string.Format("No Text exists in [{0}]", oAllInPanels[0].GetType()));
                    strError = $"Can't find text property of object [{strPegName}].[{strObjName}]";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    isOk = false;
                    return null;
                }
                isOk = true;
                return otxt == null ? "" : otxt.ToString();
            }
            if (oSourceControl is System.Windows.Forms.StatusBar)
            {
                System.Windows.Forms.StatusBar bar = (System.Windows.Forms.StatusBar)oSourceControl;
                isOk = true;
                if (bar.Panels.Count > 0)
                {
                    return bar.Panels[0].ToString();
                }
                return null;
            }
            simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError = string.Format("unsupported type:[{0}]", oSourceControl == null ? "" : oSourceControl.GetType().ToString()));
            strError = "Only standard Infragistics status bar is supported";
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Make sure control is a UltraStatusBar";
            isOk = false;
            return null;
        }
    }

    
    /// <summary>
    /// 条件补充：CONDITIONALL:Status=Inactive|Aborted|Failed;Job
    /// 多条件：使用@或者回车
    /// CONDITIONALL:@Status=Inactive|Aborted|Failed@Document Active=dkjflsd|jsfjd|;Job
    /// 或者CONDITIONALL:status=Inacti|Aborted|Failed
    /// Document Active=dkjflsd|jsfjd
    /// grouped 表模式
    ///     GROUPEDALL;[FIELDNAME:FIELDNAME1...][value1:value2......];SubTableTARGETFILED
    ///     DATA格式:OBJECTNAME
    ///     如果
    /// ;Job
    /// on 11/20/2024 增加获得rowcount
    ///    需要对某些表进行重复操作，因此需要获得该表有多少行，从而通过loop进行处理
    ///    parameter为 _property_count
    /// </summary>
    public class CaptureValueForSwfTable : CaptureValue
    {
        internal static MARS_CaptureTableCache currentTableInfo = null;// new MARS_CaptureTableCache();

        private const string cnst_condition_prefix          = "^CONDITIONALL:";
        private const string cnst_grouped_all               = "GROUPEDALL;";
        private const string cnst_grouped_all_paraformat    = @"GROUPEDALL;\[.*\]\[.*\];\S+(\s+){0,}\S+";
        private const string cnst_csv_all                   = "CSV_ALL"; // 2021-10-19添加，将所有的行作为csv
        private const string cnst_rowlimit                  = "ROWS_LIMIT";
        private const string cnst_dynamicCol                = "__dynamic_\\d+$";
        private const string cnst_number_range              = "^(-?\\d+):(-?\\d*);.*"; /// for row number range, added on 8-29-2024
        public  const string cnst_all_headers               = "_MARSALLHeaders" ;
        public  const string cnst_image_button              = "_MARSIMAGETOTEXT";
        public  const string cnst_property_count            = "_property_count" ;
        //public  const string cnst_sortHeaders               = InfragisticsGridHelper.cnst_sortHeaders;

        //basic idea for batch to var is to capture all data in one datatable, until end
        public const string cnst_batch_toVar = @"^BATCHCAPTURE:\S+"; // 2023/5/31 
        public const string cnst_batch_toVarEnd = @"^BATCHCAPTUREND;"; // end

       

        public override string CaptureValueFromControl(object oSourceControl, string strCapturePara, string strPegName, string strObjName, 
            ref bool isOk, ref string strError,
            ref string strAdv, ref string strStack, string strKeyword = "CaptureValue")
        {
            simpleLog.MarsLoggerSimple.logBegin("CaptureValueFromControl", $"Para|{strCapturePara}|pegName|{strPegName}|objName|{strObjName}|keyword|{strKeyword}|");
            try
            {
                string strTypes = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
                simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"begin {strTypes}");

                bool isSortHeader = InfragisticsGridHelper.isSortHeaderModifer(strCapturePara, ref strCapturePara);

                if (strTypes.Contains("Infragistics.Win.UltraWinGrid.UltraGrid")
                    || (string.Compare("Infragistics.Win.UltraWinGrid.UltraGrid", oSourceControl.GetType().ToString()) == 0))
                {
                    if (isSortHeader)
                    {
                        /// 需要点击header，然后排序
                        /// 
                        simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", "sortHader modal");
                        return InfragisticsGridHelper.SortHeaderByClick(oSourceControl, strCapturePara, strPegName, strObjName,
                                ref isOk, ref strError, ref strAdv, ref strStack);
                    }

                    if (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^ALLROWS;", strCapturePara))
                    {

                        if (currentTableInfo == null)
                        {
                            simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"ALLROWS|{strCapturePara}|currentTableInfo == null");
                            return GetAllRowDataForInfragistics(oSourceControl, strCapturePara, strPegName, strObjName, 
                                ref isOk, ref strError, ref strAdv, ref strStack);
                        }
                        else
                        {
                            if (!currentTableInfo.isBatchMode)
                                return GetAllRowDataForInfragistics(oSourceControl, strCapturePara, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                            else
                                return GetAllRowDataFromBatchVar(strCapturePara, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        }
                    }
                    if ((new MARS_CaptureTableCache().IsRightBatchMode(strCapturePara)) || (new MARS_CaptureTableCache().IsEndBatch(strCapturePara)))
                    {
                        simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"mode|BatchMode|{strCapturePara}|");
                        return GetAllRowDataForInfragistics(oSourceControl, strCapturePara, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    }

                    /// 10-28-2024 added: to support _MARSALLHEASERS
                    /// 
                    if (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest($"^{cnst_all_headers}", strCapturePara))
                    {
                        simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"mode|{cnst_all_headers}|");
                        return GetAllTableHeaders(oSourceControl, strCapturePara, strPegName, strObjName,
                                ref isOk, ref strError, ref strAdv, ref strStack);
                    }

                    /// 11/21/2024 added, to support _property_count
                    /// _property_count
                    if (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest($"^{cnst_property_count}", strCapturePara))
                    {
                        simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"model|{cnst_property_count}");
                        return GetRowCount(oSourceControl, strCapturePara, strPegName, strObjName,
                                ref isOk, ref strError, ref strAdv, ref strStack);
                    }

                    //2019-07-03 添加condition处理 典型应用实例CONDITIONALL:Status=Inactive|Aborted|Failed;Job
                    if (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(cnst_condition_prefix, strCapturePara))
                    {
                        simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"mode|condition prefix|{cnst_condition_prefix}|");
                        return GetCondtionRowDataForInfragistics(oSourceControl, strCapturePara, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    }

                    if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^" + cnst_rowlimit, strCapturePara))
                    {
                        simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"mode|row limit,only for grouped table|{cnst_rowlimit}");
                        return GetRowLimitRowDataForInfragistics(oSourceControl, strCapturePara, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    }

                    if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^" + cnst_csv_all, strCapturePara))
                    {
                        simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"mode|csv all|{cnst_csv_all}");
                        return GetAllTableToCsv(oSourceControl, strCapturePara, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    }

                    if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(cnst_number_range, strCapturePara))
                    {
                        simpleLog.MarsLoggerSimple.Info("CaptureValueFromControl", $"mode|number range|{cnst_number_range}");
                        return GetAllRowDataForInfragistics(oSourceControl, strCapturePara, strPegName, strObjName,
                                ref isOk, ref strError, ref strAdv, ref strStack);
                    }
                    //if (Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(cnst_grouped_all, strCapturePara))
                    //{//
                    //    return GetGroupedAllTablesForInfragistics(oSourceControl, strCapturePara, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    //}
                }
                strError = $"CaptureAndCompare or CaptureValue can't be applied to the control type|{strTypes}| for now.";
                isOk = false;
                strAdv = $"Please contact Marquis, and update to the version which can support |{strTypes}|";
                strStack = MarsErrorStacks.StackTraceDump();
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("CaptureValueFromControl");
            }
        }

        
        //private string GetGroupedAllTablesForInfragistics(object oSourceControl, string strCapturePara, string strPegName, string strObjName, ref bool isOk, ref string strError,
        //    ref string strAdv, ref string strStack)
        //{
        //    ///

        //    bool isGroupedParaFormatRight(string strPara, ref string strE, List<MarsKeyValues<string, string>> GroupedColumnsAndValues,
        //        ref string strTargetColumn,
        //        ref string strAdvT, ref string strStackT)
        //    {
        //        if (string.IsNullOrEmpty(strPara))
        //        {
        //            strE = "Parameter of keyword capturevalue or CaptureAndCompare is empty.";
        //            strAdvT = "Please check Parameter and try again.";
        //            strStackT = Environment.StackTrace;
        //            return false;
        //        }
        //        /// 結構
        //        ///   GROUPEDALL;[FIELDNAME:FIELDNAME1...][value1:value2......];SubTableTARGETFILED
        //        if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(cnst_grouped_all_paraformat, strPara))
        //        {
        //            strE = "Format of Parameter of Keyword capturevalue or CaptureAndCompare is wrong.";
        //            strAdvT = "Please check Paramter's format and try again.";
        //            strStackT = Environment.StackTrace;
        //            return false;
        //        }
        //        string[] arrPara = strPara.Split(new string[] {";" },StringSplitOptions.None);
        //        if (arrPara.Length != 3)
        //        {
        //            /// 有可能有";"在上面的和數據
        //            /// 
        //            strE = "Format of Parameter of Keyword capturevalue or CaptureAndCompare is wrong. Perhaps ';' exists in value parts";
        //            strAdvT = "Please check Paramter's format and try again.";
        //            strStackT = Environment.StackTrace;
        //            return false;
        //        }
        //        string[] arrGroupedColunNameAndValues = arrPara[1].Split(new string[] { "][" }, StringSplitOptions.None);
        //        if (arrGroupedColunNameAndValues.Length != 2)
        //        {
        //            strE = "Format of Parameter of Keyword capturevalue or CaptureAndCompare is wrong. Perhaps ';' exists in value parts";
        //            strAdvT = "Please check Paramter's format and try again.";
        //            strStackT = Environment.StackTrace;
        //            return false;
        //        }
        //        string strGroupedColumn = arrGroupedColunNameAndValues[0].Replace("[", "");
        //        string strGroupedValues = arrGroupedColunNameAndValues[1].Replace("]", ""); //目前支持一个column
        //        GroupedColumnsAndValues.Add(new MarsKeyValues<string, string>(strGroupedColumn, 
        //            strGroupedValues));

        //        strTargetColumn = arrPara[2];
        //        return true;
        //    }
        //    simpleLog.MarsLoggerSimple.logBegin("GetGroupedAllTablesForInfragistics", $"para:[{strCapturePara}], Peg:[{strPegName}] obj:[{strObjName}]");
            
        //    List<MarsKeyValues<string, string>> lstGroupedFieldAndvalue = new List<MarsKeyValues<string, string>>();
        //    try
        //    {
        //        string strTargetColumnFromPara = "";
        //        isOk = isGroupedParaFormatRight(strCapturePara, ref strError, lstGroupedFieldAndvalue, ref strTargetColumnFromPara, ref strAdv, ref strStack);
        //        if (!isOk)
        //        {
        //            simpleLog.MarsLoggerSimple.Error("GetGroupedAllTablesForInfragistics", strError, strStack);
        //            return null;
        //        }

        //        /// 逐步获得相关的数据，并且capture
        //        /// 
        //        MarsReflectCompare<int> afunc = (int v1, int v2, ref bool xx, ref string ss) =>
        //        {
        //            xx = true;
        //            return v1 - v2;
        //        };
        //        MarsTableOperation tableOp = new MarsTableOperation();
        //        ReflectorForCSharp of = new ReflectorForCSharp();
        //        object olstRows = ReflectorForCSharp.GetMember(oSourceControl, "Rows");
        //        if (!of.WaitUntilMembersGreater<int>(olstRows, "Count", 0, ref strError, afunc))
        //        {
        //            return null;
        //        }
        //        int iCount = ReflectorForCSharp.GetMemberByType<int>(olstRows, "Count");
        //        int iLevel = 0;
        //        List<string> lstGroupHead = new List<string>();
        //        object oCurrentRow = tableOp.FindAndExpandGroupedRow(olstRows, iLevel, lstGroupHead, oSourceControl, ref strError, ref strAdv, ref strStack, ref isOk);
        //        if (!isOk) return null;
        //    }
        //    finally
        //    {
        //        simpleLog.MarsLoggerSimple.logEnd("GetGroupedAllTablesForInfragistics");   
        //    }            
        //}
        /// <summary>
        /// para格式示例：ROWS_LIMIT:0:0;[Main]Value
        /// </summary>
        /// <param name="oSourceControl"></param>
        /// <param name="strCapturePara"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private string GetRowLimitRowDataForInfragistics(object oSourceControl, string strCapturePara, string strPegName, string strObjName, ref bool isOk, ref string strError,
            ref string strAdv, ref string strStack)
        {
            bool isParaFormatRight(string strPara, ref string strE, ref int istart, ref int iEnd, List<string> strGroupNames,
                ref string strTargetColumn,
                ref string strAdvT, ref string strStackT)
            {
                /**
                 * 算法
                 * 1, 首先去掉前缀 limitrows
                 * 2，获得起止row
                 * 3，分析columns
                 * */
                string strParaNoPreFix = strPara.Substring(cnst_rowlimit.Length + 1);
                int iPos = strParaNoPreFix.IndexOf(";");
                bool isOkTmp = true;
                string strStartAndEnd = strParaNoPreFix.Substring(0, iPos);
                string[] arrStartAndEnd = strStartAndEnd.Split(':');
                if ((arrStartAndEnd == null) || (arrStartAndEnd.Length <= 0))
                {
                    strE = "no start row and end row information.";
                    StackFrame stck = (new StackFrame());
                    strStackT = MarsErrorStacks.StackTraceDump();
                    strAdvT = "";
                    isOkTmp = false;
                    return false;
                }
                isOkTmp = int.TryParse(arrStartAndEnd[0], out istart);
                if (!isOkTmp)
                {
                    strE = string.Format("first row should be a number:[{0}]", arrStartAndEnd[0]);
                    StackFrame stck = (new StackFrame());
                    strStackT = MarsErrorStacks.StackTraceDump();
                    strAdvT = "";
                    return false;
                }
                //默认没有后面的一个
                iEnd = int.MaxValue;
                if (arrStartAndEnd.Length == 2)
                {
                    if (!int.TryParse(arrStartAndEnd[1], out iEnd))
                    {
                        strE = string.Format("Row end is not a number:[{0}]", arrStartAndEnd[1]);
                        StackFrame stck = (new StackFrame());
                        strStackT = MarsErrorStacks.StackTraceDump();
                        strAdvT = "";
                        return isOkTmp = false;
                    }
                }
                else
                {
                    strE = string.Format("Only two numbers are required.[{0}]", strStartAndEnd);
                    StackFrame stck = (new StackFrame());
                    strStackT = MarsErrorStacks.StackTraceDump();
                    strAdvT = "";
                    return isOkTmp = false;
                }
                string strColumns = strParaNoPreFix.Substring(iPos + 1);
                ///获得所有的group
                ///
                iPos = strColumns.IndexOf(']');
                if (iPos < 0)
                {
                    //s说明是普通模式
                    strE = string.Format("column part is with wrong format:[{0}] without '[]'", strColumns);
                    return isOkTmp = false;
                }
                strTargetColumn = strColumns.Substring(iPos + 1);
                if (!strColumns.StartsWith("["))
                {
                    strE = string.Format("column part is with wrong format:[{0}] without starting with '['", strColumns);
                    return isOkTmp = false;
                }
                string strHeaders = strColumns.Substring(1, iPos - 1);
                string[] arrHeaders = strHeaders.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                strGroupNames.AddRange(arrHeaders);
                return true;
            }

            simpleLog.MarsLoggerSimple.logBegin("GetRowLimitRowDataForInfragistics", strCapturePara);
            try
            {
                string strResult = "", strErrorTmp = "", strTargetCol = "";
                int istartRow = 0, iEndRow = 0;
                List<String> lstGroupHead = new List<string>();

                isOk = isParaFormatRight(strCapturePara, ref strErrorTmp, ref istartRow, ref iEndRow, lstGroupHead, ref strTargetCol, ref strAdv, ref strStack);
                if (!isOk)
                {
                    strError = strErrorTmp;
                    return null;
                }
                /**
                 * 算法，先定位最后一个有效的group header
                 * 然后判断是否存在指定的column Name
                 * 判断是否有足够的行数
                 * 读取数据
                 * */
                //wait until 
                MarsReflectCompare<int> afunc = (int v1, int v2, ref bool xx, ref string ss) =>
                {
                    xx = true;
                    return v1 - v2;
                };
                MarsTableOperation tableOp = new MarsTableOperation();
                ReflectorForCSharp of = new ReflectorForCSharp();
                object olstRows = ReflectorForCSharp.GetMember(oSourceControl, "Rows");
                if (!of.WaitUntilMembersGreater<int>(olstRows, "Count", 0, ref strError, afunc))
                {
                    return null;
                }
                int iCount = ReflectorForCSharp.GetMemberByType<int>(olstRows, "Count");
                int iLevel = 0;
                object oCurrentRow = tableOp.FindAndExpandGroupedRow(olstRows, iLevel, lstGroupHead, oSourceControl, ref strError, ref strAdv, ref strStack, ref isOk);
                if (!isOk) return null;
                if (oCurrentRow == null)
                {
                    simpleLog.MarsLoggerSimple.Error("GetRowLimitRowDataForInfragistics", strError = string.Format("No group Row find by caption path:[{0}]", lstGroupHead));
                    return null;
                }
                string strKey = "";
                int iTargetColumnIdx = -1;
                if (!tableOp.GetColumnKeyForInfragisticsGrid(oSourceControl, strTargetCol, strPegName, strObjName, ref strKey, ref iTargetColumnIdx, ref strError,
                    ref strAdv, ref strStack))
                {
                    simpleLog.MarsLoggerSimple.Error("GetRowLimitRowDataForInfragistics", strError);
                    return null;
                }
                //获得所有的row，从grouped开始
                object oAllRowFromGroup = ReflectorForCSharp.GetMember(oCurrentRow, "Rows");
                object[] oAllInRows = ReflectorForCSharp.GetMemberByType<object[]>(oAllRowFromGroup, "All");
                int iSubRowCnt = ReflectorForCSharp.GetMemberByType<int>(oAllRowFromGroup, "Count");
                istartRow = istartRow < 0 ? 0 : istartRow;
                iEndRow = iEndRow == int.MaxValue ? iSubRowCnt - 1 : (iEndRow >= iSubRowCnt ? iSubRowCnt - 1 : iEndRow);
                bool isNotExists = false;

                for (int i = istartRow; i <= iEndRow; i++)
                {
                    object itmRow = oAllInRows[i];
                    if (itmRow == null) continue;
                    object oCell = tableOp.GetCellFromOneRow(itmRow, iTargetColumnIdx, ref isOk, ref strError, ref strAdv, ref strStack);
                    if (!(isOk && (oCell != null)))
                    {
                        simpleLog.MarsLoggerSimple.Error("GetRowLimitRowDataForInfragistics", strError);
                        return null;
                    }
                    string strTmpCellText = of.GetMember<string>(oCell, "Text", ref isNotExists);
                    if (isNotExists)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", strError = "Object property [Text]'s value is NULL in Cell.");//"No Text property exists in Cell");                        
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        isOk = false;
                        return null;
                    }
                    if (string.IsNullOrEmpty(strResult)) strResult = strTmpCellText;
                    else
                    {
                        if ((strTmpCellText.StartsWith("179") && strTmpCellText.Length > 20)
                            ||(strTmpCellText.Equals("-1.79769313486232E+308",StringComparison.OrdinalIgnoreCase)))
                            strTmpCellText = "";
                        strResult += ("\r\n" + strTmpCellText);
                    }
                }

                return strResult;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetRowLimitRowDataForInfragistics");
            }

        }

        /// <summary>
        /// CONDITIONALL:Status=Inactive|Aborted|Failed;Job
        /// </summary>
        /// <param name="oSourceControl"></param>
        /// <param name="strCapturePara">should be CONDITIONALL:Column name or caption=Inactive|Aborted|Failed;target coloumn name or caption</param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private string GetCondtionRowDataForInfragistics(object oSourceControl, string strCapturePara, string strPegName, string strObjName, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            /**
             * 算法：
             * 1,先进行格式分析获得相关的数据
             * 2,等待Grid 加载
             * 2,获得columns信息 
             * */
            ConditionParameterInfo cndParaInfo = new ConditionParameterInfo(strCapturePara);
            isOk = cndParaInfo.isRightFormat(ref strError);
            if (!isOk) return null;
            int iRowCnt = 0;
            object oRows = null;
            bool isGridReady = IsDataGridRowReady(oSourceControl, 15, ref oRows, ref iRowCnt, ref strError, ref strAdv, ref strStack);
            if (!isGridReady)
            {
                isOk = false;
                return null;
            }
            //获取columns
            int iConditionColumn = -1, iTargetColumnId = -1;
            string strConditionColumnKey = "", strTargetColumnKey = "";
            string strErrorTmp = "";
            string strAdvTmp = "", strStackTmp = "";
#if _NET4
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
            ((System.Windows.Forms.Control)oSourceControl).Invoke(
#endif      
            new Action(() =>
            {
                isGridReady = GetColumnKeyForInfragisticsGrid(oSourceControl, strPegName, cndParaInfo.subParameters, 
                    strObjName, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                if (isGridReady)
                {
                    //tiger 修改，多条件模式
                    //获得目标的column info
                    isGridReady = GetColumnKeyForInfragisticsGrid(oSourceControl,strPegName, new List<ConditonSubParameterInfo>() { cndParaInfo.targetColumnInfo}, 
                        strObjName, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);

                    //isGridReady = GetColumnKeyForInfragisticsGrid(oSourceControl, cndParaInfo.conditionColumn, strPegName, strObjName, ref strTargetColumnKey, ref iTargetColumnId, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                }
            }));

            if (!isGridReady)
            {
                strError = string.Format("No such columns [{0}] or [{1}], sourceError:[{2}]", cndParaInfo.getConditionColumnsName(), cndParaInfo.targetColumnInfo.conditionColumn, strErrorTmp);
                strAdv = strAdvTmp;
                strStack = strStackTmp;
                isOk = false;
                return null;
            }

            object[] oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
            string strResult = "";
            int iFndCnt = 0;
            for (int i = 0; i < (oAllRows == null ? -1 : oAllRows.Length); i++)
            {
                object oOneRow = oAllRows[i];
                if (oOneRow == null)
                {
                    strResult += "\r\n";
                    continue;
                }
                object oCellsCollection = ReflectorForCSharp.GetMember(oOneRow, "Cells"); //Infragistics.Win.UltraWinGrid.CellsCollection
                if (oCellsCollection == null)
                {
                    strResult += "\r\n";
                    continue;
                }
                object[] allCells = ReflectorForCSharp.GetMemberByType<object[]>(oCellsCollection, "All");
                if (allCells == null)
                {
                    strResult += "\r\n";
                    continue;
                }
                /// 多条件模式
                /// 直接从将数据放到对象中
                /// cndParaInfo
                /// 
                bool isRowMatched = false;
                for (int j=0;j< cndParaInfo.subParameters.Count; j++)
                {
                    isRowMatched = true;
                    ///**
                    ///算法
                    ///1，判断是否越界
                    ///2，如果越界返回错误
                    ///3，获得cell的数据到对象中
                    ///4，调用对象的判断，如果证券
                    int iCellIdx = cndParaInfo.subParameters[j].columnKeyId;
                    if ((iCellIdx<0)||(iCellIdx>= allCells.Length))
                    {
                        strError = $"column [{cndParaInfo.subParameters[j].conditionColumn}]'s index [{iCellIdx}] is greater than the total cells [{allCells.Length}] or less than 0";
                        simpleLog.MarsLoggerSimple.Error("\t", strError);
                        strAdv = "Contact Marquis";
                        strStack = MarsErrorStacks.StackTraceDump();
                        isOk = false;
                        return "";
                    }
                    string strConditionCellText = ReflectorForCSharp.GetMemberByType<string>(allCells[iCellIdx], "Text");
                    cndParaInfo.subParameters[j].cellValue = strConditionCellText;
                    isRowMatched = cndParaInfo.subParameters[j].isMatched();
                    if (!isRowMatched) break;
                }
                // 不满足条件，跳过改行
                if (!isRowMatched) continue;
                // 获得目标的cell数据
                string strTargetTxt = ReflectorForCSharp.GetMemberByType<string>(allCells[cndParaInfo.targetColumnInfo.columnKeyId], "Text");
                if (iFndCnt <= 0)
                {
                    strResult = strTargetTxt;                    
                }
                else
                {
                    strResult = string.Format("{0}\r\n{1}", strResult, strTargetTxt ?? "");
                }
                iFndCnt++;

                #region //old codes,單條件
                /*
                //check cells length against target colidx and condition column idx
                if ((allCells.Length <= iConditionColumn) || (allCells.Length <= iTargetColumnId))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = $"Object Index is greater or equal to number of objects for [{strPegName}].[{strObjName}]");// string.Format("Only [{0}] cells returns, but cell index is :[{1}]", allCells.Length, iColIdx));) ;
                    strAdv = "Check index in Object identification.";
                    strStack = MarsErrorStacks.StackTraceDump();
                    isOk = false;
                    return "";
                }
                //获得condtion part
                if (allCells[iConditionColumn] != null)
                {
                    string strConditionCellText = ReflectorForCSharp.GetMemberByType<string>(allCells[iConditionColumn], "Text");
                    if ((string.Compare(cndParaInfo.filter, strConditionCellText, true) == 0)
                        || (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(cndParaInfo.filter, strConditionCellText)))
                    {
                        string strTargetTxt = ReflectorForCSharp.GetMemberByType<string>(allCells[iTargetColumnId], "Text");
                        if (iFndCnt == 0)
                        {
                            strResult = strTargetTxt ?? "";
                        }
                        else
                            strResult = string.Format("{0}\r\n{1}", strResult, strTargetTxt ?? "");
                        iFndCnt++;
                    }
                    else
                    {   //不是condition行
                        continue;
                    }
                }
                else
                {
                    continue;
                }
                */
                #endregion
            }

            isOk = true;
            return strResult;

        }

        private string GetAllTableToCsv(object oSourceControl, string strCapturePara, string strPegName, string strObjName, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetAllTableToCsv", $"para:[{strCapturePara}], peg-[{strPegName}], obj-[{strObjName}]");
            try
            {
                int iColIdx = -1;
                bool isOkTmp = false;
                string strErrorTmp = "";
                string strAdvTmp = "", strStackTmp = "";
                bool isNotExists = false;
                int iRowCount = -1;
                object oRows = null;
                long lstart = DateTime.Now.Ticks, lnow = lstart;
                isOkTmp = true;
                while (((iRowCount <= 0) && (((lnow - lstart) / TimeSpan.TicksPerSecond) < 15)))
                {
                    
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                    new Action(() =>
                    {
                        oRows = ReflectorForCSharp.GetMember(oSourceControl, "Rows", ref isNotExists);
                        if (isNotExists)
                        {
                            isOkTmp = false;
                            simpleLog.MarsLoggerSimple.Error("\t", strErrorTmp = "No Rows exists in Grid, Wrong Infragistics version?");
                            strErrorTmp = "Object does not contain rows property";
                            strAdvTmp = "Make sure object is a UltraGrid";
                            strStackTmp = MarsErrorStacks.StackTraceDump();
                            return;
                        }
                        iRowCount = ReflectorForCSharp.GetMemberByType<int>(oRows, "Count");
                    }
                    )
                    );
                    if (!isOkTmp) break;
                    lnow = DateTime.Now.Ticks;
                    System.Threading.Thread.Sleep(100);
                }

                isOk = isOkTmp;
                strError = strErrorTmp;
                strAdv = strAdvTmp;
                strStack = strStackTmp;
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("Row count:[{0}]", iRowCount));
                if (!isOk) return "";

                List<MarsColumnForTableInfo> allColumns = (new MarsTableOperation()).GetTableColumnNames(oSourceControl, strPegName, strObjName, ref isOk, ref strError,
                     ref strAdv, ref strStack);
                if (!isOk)
                {
                    return "";
                }
                /// 循环从rows中获得所有数据
                /// 
                var allCells = GetDataFromTables(oRows, ref isOk, ref strError, ref strAdv, ref strStack);
                if (!isOk) return "";

                /// convert all to csv with header
                /// 
                var headVisible = allColumns.Where(p => !p.isHidden).ToList();
                var allHeadVisibleColsName = headVisible.Select(p=>p.columnName).ToList();
                string strHead = string.Join(",", headVisible.Select(p => p.columnName).ToArray());

                List<string> lstAllRows = new List<string>();
                for (int i = 0; i < allCells.Count; i++)
                {
                    var oneRowCells = allCells[i];
                    var allVisibleCells = oneRowCells.Where(p => allHeadVisibleColsName.Contains(p.colName)).ToArray();
                    string strOneRow = string.Join(",", allVisibleCells.Select(p=>p.cellDisplayString));
                    lstAllRows.Add(strOneRow);
                }

                ///combined together
                ///
                isOk = true;
                string strRsult = strHead + "\r\n" + string.Join("\r\n", lstAllRows);
                simpleLog.MarsLoggerSimple.Info("\t",$"return csv:[{strRsult}]");
                return strRsult;
            }catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("GetAllTableToCsv", strError = e.Message, strStack = e.StackTrace);
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetAllTableToCsv", $"returns [{isOk}], error:[{strError}]");
            }
        }
        internal List<List<MarsTableCells>> GetDataFromTables(object rows, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetDataFromTables");
            List<List<MarsTableCells>> lstRslt = new List<List<MarsTableCells>>();
            try
            {
                object[] oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(rows, "All");
                string strResult = "";
                for (int i = 0; i < (oAllRows == null ? -1 : oAllRows.Length); i++)
                {
                    object oOneRow = oAllRows[i];
                    if (oOneRow == null)
                    {
                        strResult += "\r\n";
                        continue;
                    }

                    object isFiltered = ReflectorForCSharp.GetMember(oOneRow, "IsFilteredOut");
                    if ((isFiltered == null))
                    {
                        simpleLog.MarsLoggerSimple.Warnning("\t", "IsFilteredOut is null");
                        continue;
                    }
                    bool filtered = (bool)isFiltered;
                    if (filtered) continue;

                    List<MarsTableCells> oneRowCells = new List<MarsTableCells>();

                    object oCellsCollection = ReflectorForCSharp.GetMember(oOneRow, "Cells"); //Infragistics.Win.UltraWinGrid.CellsCollection
                    if (oCellsCollection == null)
                    {
                        strResult += "\r\n";
                        continue;
                    }
                    object[] allCells = ReflectorForCSharp.GetMemberByType<object[]>(oCellsCollection, "All");
                    for (int iColIdx = 0; iColIdx < allCells.Length; iColIdx++)
                    {
                        MarsTableCells oneCell = new MarsTableCells();
                        if (allCells[iColIdx] != null)
                        {
                            string strCellText = ReflectorForCSharp.GetMemberByType<string>(allCells[iColIdx], "Text");
                            if (!string.IsNullOrEmpty(strCellText))
                            {
                                if (strCellText.StartsWith("179") && (strCellText.Length > 15))
                                    strCellText = ""; // summit FT中为0的数据
                                if (strCellText.StartsWith("-179") && (strCellText.Length > 15))
                                    strCellText = ""; // summit FT中为0的数据
                                if (strCellText.StartsWith("-1.79") && (strCellText.Length > 15))
                                    strCellText = ""; // summit FT中为0的数据
                                if (strCellText.StartsWith("1.79") && (strCellText.Length > 15))
                                    strCellText = ""; // summit FT中为0的数据
                            }
                            oneCell.cellDisplayString = strCellText;
                            try
                            {
                                var v = ReflectorForCSharp.GetMember(allCells[iColIdx], "Value");
                                oneCell.cellDataValue = v == null ? null : v.ToString();
                            }
                            catch (Exception ce)
                            {
                                simpleLog.MarsLoggerSimple.Error("\t", $"get Cell value error, :[{ce.Message}]", ce.StackTrace);
                            }
                            oneCell.colOrd = iColIdx;
                            try
                            {
                                var col = ReflectorForCSharp.GetMember(allCells[iColIdx], "Column");
                                var tmpH = ReflectorForCSharp.GetMember(col, "Header");
                                var n = ReflectorForCSharp.GetMember(tmpH, "Caption");
                                oneCell.colName = n == null ? "N/A" : n.ToString();
                            }
                            catch(Exception de)
                            {
                                simpleLog.MarsLoggerSimple.Error("\t", $"get Cell column error, :[{de.Message}]", de.StackTrace);
                            }
                            oneRowCells.Add(oneCell);
                        }
                        else
                        {
                            if (iColIdx == 0) strResult = "\r\n";
                            else
                                strResult = string.Format("{0}\r\n", strResult);
                        }
                    }
                    lstRslt.Add(oneRowCells);
                }
                isOk = true;
                return lstRslt;
            }
            catch (Exception e)
            {
                isOk = false;
                simpleLog.MarsLoggerSimple.Error("GetDataFromTables", strError = e.Message, strStack = e.StackTrace);
                strAdv = "Contact Marquis";
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetDataFromTables");
            }
        }

        private string GetAllRowDataFromBatchVar(string strCapturePara, string strPegName, string strObjName,
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("GetAllRowDataFromBatchVar", $"[{iMark}] [{strCapturePara}], [{strPegName}]-[{strObjName}]");
            if (!currentTableInfo.isBatchMode)
            { // should never come here
                strError = Resources.mars_mq_caputre_batch_wrong_call_all_grid;
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = Resources.mars_contact_to_marquis;
                isOk = false;
                return "";
            }
            /// get data from datatable
            /// 
            string[] arrCmd = strCapturePara.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            string strColName = "";

            //MARSClientCaptureAndCompare

            try
            {
                if (arrCmd.Length == 2)
                {
                    strColName = arrCmd[1];
                }
                if (string.IsNullOrEmpty(strColName))
                {
                    simpleLog.MarsLoggerSimple.Error("GetAllRowDataFromBatchVar", $"[{iMark}] " + (strError = string.Format("Wrong format of captureValue/CaptureAndCompare for grid. ALLROWS;columnName is required, but [{0}]", strCapturePara)));
                    strError = "Incorrect format for grid cell location.";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "See user manual for correct grid location use";
                    isOk = false;
                    return "";
                }
                Regex rx = new Regex(strColName);
                string strColnames = string.Join(";", currentTableInfo.columns.Select(p => $"{p.columnKey}-{p.columnCaption}"));
                var colInfo = currentTableInfo.columns
                    .Select((element, index) => new { element, index })
                    .Where(p => (p != null)
                        && (p.element != null)
                        && (rx.IsMatch(p.element.columnKey ?? "") || rx.IsMatch(p.element.columnCaption ?? "")))
                    .ToList();
                if ((colInfo == null) || (colInfo.Count == 0))
                {
                    strError = string.Format(Resources.mars_mq_capture_batch_no_such_column, strColName, strColnames);
                    simpleLog.MarsLoggerSimple.Error("GetAllRowDataFromBatchVar", $"[{iMark}] " + strError);

                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure column name setting is right";
                    isOk = false;
                    return "";
                }
                if (colInfo.Count > 1)
                {
                    strError = string.Format(Resources.mars_mq_capture_batch_morethan_one_column_found, strColName);
                    simpleLog.MarsLoggerSimple.Error("GetAllRowDataFromBatchVar", $"[{iMark}] " + strError);

                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure column name setting is right";
                    isOk = false;
                    return "";
                }
                string strRslt = "";

                for (int i = 0; i < currentTableInfo.dataTable.Rows.Count; i++)
                {
                    var r = currentTableInfo.dataTable.Rows[i];
                    int iColIdx = colInfo[0].index;
                    var c = r[iColIdx];
                    string strC = c == null ? "" : c.ToString();
                    if (i == 0) strRslt = strC;
                    else
                        strRslt = $"{strRslt}\r\n{strC}";
                }
                isOk = true;
                return strRslt;
            }catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("GetAllRowDataFromBatchVar", $"[{iMark}], [{e.Message}]\r\n{e.StackTrace}");
                isOk = false;
                strAdv = "Contact with Marquis";
                strError = Resources.mars_mq_capture_batch_cant_get_all_data_from_table;
                strStack = e.StackTrace;
                return "";
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetAllRowDataFromBatchVar", $"{iMark}-{isOk}-[{strError}]");
            }
        }

        private bool IsDynamicCol(string strPara, ref int iColId)
        {
            simpleLog.MarsLoggerSimple.logBegin("IsDynamicCol", $"{strPara}");
            try
            {            
                if (string.IsNullOrEmpty(strPara)) return false;
                Regex rx = new Regex(cnst_dynamicCol);
                var m = rx.Match(strPara);
                if (!m.Success) return false;
                int iPos = strPara.LastIndexOf("_");
                if (iPos < 0) return false;

                string tmpIdx = strPara.Substring(iPos + 1);
                if (int.TryParse(tmpIdx, out iColId)) return true;
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("IsDynamicCol");
            }
        }

        private bool IsRowRangeMode(string strPara, ref int iStartRowNum, ref int iEndRowNum)
        {
            simpleLog.MarsLoggerSimple.logBegin("IsRowRangeMode", $"para|{strPara}");
            try
            {
                if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(cnst_number_range, strPara)) return false;
                Match match = Regex.Match(strPara, cnst_number_range);

                if (match.Success)
                {
                    // Extract the numbers
                    string firstNumber = match.Groups[1].Value;
                    string secondNumber = match.Groups[2].Value;

                    bool isOK = int.TryParse(firstNumber, out iStartRowNum);
                    if (string.IsNullOrEmpty(secondNumber)) {
                        iEndRowNum = int.MaxValue;
                    }else  
                        isOK = int.TryParse(secondNumber, out iEndRowNum) && isOK;
                    simpleLog.MarsLoggerSimple.Info("IsRowRangeMode", $"firstNumber|{firstNumber}|second|{secondNumber}");
                    return isOK;
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Info("IsRowRangeMode",$"not match|{cnst_number_range}|{strPara}");
                    return false;
                }
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("IsRowRangeMode", $"{e.Message}|{e.StackTrace}", e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("IsRowRangeMode", $"returns|{iStartRowNum}|{iEndRowNum}");
            }
        }

        /// <summary>
        /// 获得table的所有header的caption
        /// </summary>
        /// <param name="oSourceControl"></param>
        /// <param name="strCapturePara"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <returns></returns>
        private string GetAllTableHeaders(object oSourceControl, string strCapturePara, string strPegName, string strObjName,
                    ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("GetAllTableHeaders", $"[{iMark}] [{strCapturePara}], [{strPegName}]-[{strObjName}]");
            if (oSourceControl == null)
            {
                strError = "Please ensure the object is visible";
                isOk = false;
                strAdv = strError;
                strStack = MarsErrorStacks.StackTraceDump(); 
                return null;
            }
            System.Windows.Forms.Control c = oSourceControl as System.Windows.Forms.Control;
            if (c == null)
            {
                strError = "Please ensure the object is a GUI element and visible";
                isOk = false;
                strAdv = strError;
                strStack = MarsErrorStacks.StackTraceDump();
                return null;
            }
            try
            {
                bool isOkTmp = false;
                string strAdvTmp = "", strStackTmp = "", strErrorTmp = "";
                List<MARSColumnsInfo> lstColumns = new List<MARSColumnsInfo>();
                c.Invoke(new Action(() =>
                {
                    lstColumns = GetAllColumnsInfo(oSourceControl, strPegName, strObjName, ref isOkTmp, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                }));
                if (!isOkTmp)
                {
                    strError = strErrorTmp;
                    strAdv = strAdvTmp;
                    strStack = strStackTmp;
                    isOk = isOkTmp;
                    return null;
                }
                if (lstColumns == null)
                {
                    isOk = false;
                    strError = "No Columns returns, please ensure the table is loaed well";
                    strAdv = "please ensure the table is loaed well";
                    strStack = MarsErrorStacks.StackTraceDump();
                    return null;
                }

                /// sorted by char
                /// 
                var lstColumnCaptionsSorted = lstColumns.Select(p=>p.columnCaption)
                        .OrderBy(p=>p)
                        .ToList();
                if ((lstColumnCaptionsSorted == null)||(lstColumnCaptionsSorted.Count<=0))
                {
                    isOk = false;
                    strError = "Can't fetch all columns caption";
                    strAdv = "please ensure the table is loaed well";
                    strStack = MarsErrorStacks.StackTraceDump();
                    return "";
                }
                isOk = true;
                return string.Join("\r\n", lstColumnCaptionsSorted);
            }
            catch(Exception e)
            {
                strError = "MARS has exceptions when MARS is fetching table headers";
                simpleLog.MarsLoggerSimple.Error("GetAllTableHeaders", $"{strError}|{e.Message}" , e);
                strAdv = "Please check error and Contact Marquis";
                strStack = e.StackTrace;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetAllTableHeaders",$"returns|{isOk}");
            }            
        }

        /// <summary>
        /// 获得grid的row count
        /// </summary>
        /// <param name="oSourceControl"></param>
        /// <param name="strCapturePara"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <returns></returns>
        private string GetRowCount(object oSourceControl, string strCapturePara, string strPegName, string strObjName,
                    ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("GetRowCount", $"[{iMark}] [{strCapturePara}], [{strPegName}]-[{strObjName}]");
            if (oSourceControl == null)
            {
                strError = "Please ensure the object is visible";
                isOk = false;
                strAdv = strError;
                strStack = MarsErrorStacks.StackTraceDump();
                return null;
            }
            System.Windows.Forms.Control c = oSourceControl as System.Windows.Forms.Control;
            if (c == null)
            {
                strError = "Please ensure the object is a GUI element and visible";
                isOk = false;
                strAdv = strError;
                strStack = MarsErrorStacks.StackTraceDump();
                return null;
            }
            int iRowCnt = 0;
            try
            {
                string strAdvTmp = "", strStackTmp = "", strErrorTmp = "";
                List<MARSColumnsInfo> lstColumns = new List<MARSColumnsInfo>();
                
                
                object oRows = null;
                bool isOkTmp = IsDataGridRowReady(oSourceControl, 15, ref oRows, ref iRowCnt, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                
                if (!isOkTmp)
                {
                    strError = strErrorTmp;
                    strAdv = strAdvTmp;
                    strStack = strStackTmp;
                    isOk = isOkTmp;
                    simpleLog.MarsLoggerSimple.Error("GetRowCount", strError, strStack);
                    return null;
                }
                
                return iRowCnt +"";
            }
            catch (Exception e)
            {
                strError = "MARS has exceptions when MARS is fetching table headers";
                simpleLog.MarsLoggerSimple.Error("GetRowCount", $"{strError}|{e.Message}", e);
                strAdv = "Please check error and Contact Marquis";
                strStack = e.StackTrace;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetRowCount", $"returns|{isOk}");
            }
        }

       
        /// <summary>
        /// 获得指定column的所有的数据
        /// </summary>
        /// <param name="oSourceControl"></param>
        /// <param name="strCapturePara"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private string GetAllRowDataForInfragistics(object oSourceControl, string strCapturePara, string strPegName, string strObjName, 
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack )
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("GetAllRowDataForInfragistics", $"[{iMark}] [{strCapturePara}], [{strPegName}]-[{strObjName}]");

            string[] arrCmd = strCapturePara.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            string strColName = "";

            try
            {
                if (arrCmd.Length == 2)
                {
                    strColName = arrCmd[1];
                }
                if (currentTableInfo == null)
                {
                    currentTableInfo = new MARS_CaptureTableCache();
                    if (!currentTableInfo.IsRightBatchMode(strCapturePara))
                    {
                        simpleLog.MarsLoggerSimple.Info("GetAllRowDataForInfragistics", string.Format(Resources.mars_mq_capture_batch_format_error, strCapturePara));
                    }
                }
                if (currentTableInfo.isBatchMode)
                {
                    if (currentTableInfo.IsEndBatch(strError))
                    {
                        currentTableInfo.isBatchMode = false;
                        isOk = true;
                        currentTableInfo = null;
                        return "BATCH_END SUCCESS";
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(strColName))
                    {
                        simpleLog.MarsLoggerSimple.Error("GetAllRowDataForInfragistics", $"[{iMark}] " + (strError = string.Format("Wrong format of captureValue/CaptureAndCompare for grid. ALLROWS;columnName is required, but [{0}]", strCapturePara)));
                        strError = "Incorrect format for grid cell location.";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "See user manual for correct grid location use";
                        isOk = false;
                        return "";
                    }
                }

                /// ADD dynamic row, like 
                /// allRows;__dynamic_n
                /// 
                int iColId = -1;
                bool isDynamicCol = IsDynamicCol(strColName, ref iColId);
                simpleLog.MarsLoggerSimple.Info("GetAllRowDataForInfragistics", $"iColId|{iColId}|{isDynamicCol}");
                if (iColId < 0) isDynamicCol = false;
                /// 是否是行range模式, added on 8-29-2024
                ///                 
                int iStartRowNum=-1, iEndRowNum=-1;
                bool isRowRangeMode = IsRowRangeMode(strCapturePara, ref iStartRowNum,ref iEndRowNum);
                if (isRowRangeMode)
                {
                    if (iEndRowNum < 0)
                    {
                        iEndRowNum = 0;
                    }
                }
                //获得列名
                string strKey = "";
                int iColIdx = -1;
                bool isOkTmp = false;
                string strErrorTmp = "";
                string strAdvTmp = "", strStackTmp = "";
                bool isNotExists = false;
                int iRowCount = -1;
                object oRows = null;
                long lstart = DateTime.Now.Ticks, lnow = lstart;

                Control tmpc = (System.Windows.Forms.Control)oSourceControl;
                
                /// 有时候，系统加载较慢，因此这里检测是否已经加载完毕，通过
                while ((iRowCount <= 0) && (((lnow - lstart) / TimeSpan.TicksPerSecond) < 15))
                {
                    isOkTmp = true;

                    System.Threading.Thread.Sleep(50);
#if _NET4

                    ((System.Windows.Forms.Control)oSourceControl).Invoke(//System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                    ((System.Windows.Forms.Control)oSourceControl).Invoke(
#endif
                    new Action(() =>
                    {
                        oRows = ReflectorForCSharp.GetMember(oSourceControl, "Rows", ref isNotExists);
                        if (isNotExists)
                        {
                            isOkTmp = false;
                            simpleLog.MarsLoggerSimple.Error("\t", strErrorTmp = "No Rows exists in Grid, Wrong Infragistics version?");
                            strErrorTmp = "Object does not contain rows property";
                            strAdvTmp = "Make sure object is a UltraGrid";
                            strStackTmp = MarsErrorStacks.StackTraceDump();
                            return;
                        }
                        iRowCount = ReflectorForCSharp.GetMemberByType<int>(oRows, "Count");
                    }
                    ));
                    if (!isOkTmp) break;
                    lnow = DateTime.Now.Ticks;
                }
                isOk = isOkTmp;
                strError = strErrorTmp;
                strAdv = strAdvTmp;
                strStack = strStackTmp;
                List<MARSColumnsInfo> lstCols = new List<MARSColumnsInfo>();
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("Row count:[{0}]", iRowCount));
                if (!isOk) return "";

                /// added o 20241218 to make sure the windows is read by sending wm_paint
                IntPtr messageStub = IntPtr.Zero;
                MarsWindowsAPIs.SendMessage(tmpc.Handle, (int)WM.PAINT, 0, ref messageStub);

#if _NET4
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                ((System.Windows.Forms.Control)oSourceControl).Invoke(
#endif
                new Action(() =>
                {
                    if (currentTableInfo.isBatchMode)
                    {
                        lstCols = GetAllColumnsInfo(oSourceControl, strPegName, strObjName, ref isOkTmp,
                            ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                        if (isOkTmp)
                        {
                            for (int icoltmp = 0; icoltmp < lstCols.Count; icoltmp++)
                            {
                                var tmpColDT = currentTableInfo.dataTable.Columns.Add();
                                tmpColDT.Caption = $"[{lstCols[icoltmp].columnCaption}]:[{lstCols[icoltmp].columnKey}].[{lstCols[icoltmp].idxOfKey}]";
                            }
                        }
                        currentTableInfo.columns = lstCols;
                    }
                    else if (isDynamicCol)
                    {
                        simpleLog.MarsLoggerSimple.Info("GetAllRowDataForInfragistics", $"isDynamicCol|{isDynamicCol}|{iColId}|");
                        isOkTmp = GetColumnKeyForInfragisticsGrid(oSourceControl, strColName, 
                            strPegName, strObjName, 
                            ref strKey, ref iColIdx, ref strErrorTmp, ref strAdvTmp, ref strStackTmp,
                            iColId);
                    }
                    else
                        isOkTmp = GetColumnKeyForInfragisticsGrid(oSourceControl, strColName, strPegName, strObjName, ref strKey, ref iColIdx, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                }));

                isOk = isOkTmp;
                strError = strErrorTmp;
                strAdv = strAdvTmp;
                strStack = strStackTmp;
                if (!isOk) return "";

                object[] oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
                string strResult = "";
                int iStartTargetRow = 0, iEndTargetRowNumber = (oAllRows == null ? -1 : oAllRows.Length);
                if (isRowRangeMode)
                {
                    /// 如果 istartRowNum 小于0， 那么就是从最后一行获得指定的iEndRowNum数量
                    /// 
                    if (iStartRowNum < 0)
                    {
                        if ((iEndTargetRowNumber == int.MaxValue) || (iEndTargetRowNumber <= 0))
                        {
                            strError = $"Second number |{iEndTargetRowNumber}| of last row mode should greater than 0 and be validated number.";
                            strAdv = "Please change Settings and try again";
                            strStack = Environment.StackTrace;
                            simpleLog.MarsLoggerSimple.Error("GetAllRowDataForInfragistics", strError);
                            isOk = false;
                            return "";
                        }
                        iStartTargetRow = oAllRows.Length - iEndRowNum;
                        if (iStartTargetRow < 0)
                        {
                            /// 说明没有足够的行
                            /// 
                            iStartTargetRow = 0;
                            simpleLog.MarsLoggerSimple.Info("GetAllRowDataForInfragistics", $"not enough rows exists");
                        }
                        iEndTargetRowNumber = oAllRows.Length;
                        simpleLog.MarsLoggerSimple.Info("GetAllRowDataForInfragistics", $"last from mode, from|{iStartTargetRow}|to|{iEndTargetRowNumber}");
                    }
                    else
                    {
                        iStartTargetRow = iStartRowNum;
                        iEndTargetRowNumber = ((iEndRowNum == int.MaxValue) || (iEndRowNum >= oAllRows.Length)) ? iEndTargetRowNumber : iEndRowNum;
                        simpleLog.MarsLoggerSimple.Info("GetAllRowDataForInfragistics", $"normal row range mode,from|{iStartTargetRow}|to|{iEndTargetRowNumber}");
                    }
                    simpleLog.MarsLoggerSimple.Info("GetAllRowDataForInfragistics", $"isRowRangeMode, adjust start and end|row.lenght|{oAllRows.Length}|iStart-iEnd|{iStartTargetRow}|{iEndTargetRowNumber}|");
                }
                for (int iRow = iStartTargetRow; iRow < iEndTargetRowNumber; iRow++)
                {
                    object oOneRow = oAllRows[iRow];
                    if (oOneRow == null)
                    {
                        strResult += "\r\n";
                        continue;
                    }

                    object isFiltered = ReflectorForCSharp.GetMember(oOneRow, "IsFilteredOut");
                    if ((isFiltered == null))
                    {
                        simpleLog.MarsLoggerSimple.Warnning("\t", "IsFilteredOut is null");
                        continue;
                    }
                    bool filtered = (bool)isFiltered;
                    if (filtered) continue;

                    object oCellsCollection = ReflectorForCSharp.GetMember(oOneRow, "Cells"); //Infragistics.Win.UltraWinGrid.CellsCollection
                    if (oCellsCollection == null)
                    {
                        strResult += "\r\n";
                        continue;
                    }
                    object[] allCells = ReflectorForCSharp.GetMemberByType<object[]>(oCellsCollection, "All");
                    if (allCells == null)
                    {
                        strResult += "\r\n";
                        continue;
                    }
                    DataRow curRow4Batch = null;

                    int iStartCellIdx = iColIdx, iEndCellIdx = iColIdx;
                    bool isAllCells = false;
                    if (currentTableInfo.isBatchMode)
                    {
                        iStartCellIdx = 0;
                        iEndCellIdx = currentTableInfo.columns.Count-1;//allCells.Length - 1;
                        isAllCells = true;
                        curRow4Batch = currentTableInfo.dataTable.NewRow();
                    }
                    //int iColIdxTmp =0
                    for (int iColIdxTmp = iStartCellIdx; iColIdxTmp <= iEndCellIdx; iColIdxTmp++)
                    {
                        if (!isAllCells)
                        {
                            iColIdx = iColIdxTmp; 
                            if (allCells.Length <= iColIdx)
                            {
                                simpleLog.MarsLoggerSimple.Error("\t", strError = $"Object Index is greater or equal to number of objects for [{strPegName}].[{strObjName}]");// string.Format("Only [{0}] cells returns, but cell index is :[{1}]", allCells.Length, iColIdx));) ;
                                strAdv = "Check index in Object identification.";
                                strStack = MarsErrorStacks.StackTraceDump();
                                isOk = false;
                                return "";
                            }
                        }
                        else
                        {
                            iColIdx = currentTableInfo.columns[iColIdxTmp].idxOfKey;
                        }
                        if (allCells[iColIdx] != null)
                        {
                            string strCellText = ReflectorForCSharp.GetMemberByType<string>(allCells[iColIdx], "Text");
                            if (!string.IsNullOrEmpty(strCellText))
                            {
                                if (strCellText.StartsWith("179") && (strCellText.Length > 15))
                                    strCellText = ""; // summit FT中为0的数据
                                if (strCellText.StartsWith("-179") && (strCellText.Length > 15))
                                    strCellText = ""; // summit FT中为0的数据
                                if (strCellText.StartsWith("-1.79") && (strCellText.Length > 15))
                                    strCellText = ""; // summit FT中为0的数据
                                if (strCellText.StartsWith("1.79") && (strCellText.Length > 15))
                                    strCellText = ""; // summit FT中为0的数据

                                if (("IDays".Equals(strColName ?? "", StringComparison.OrdinalIgnoreCase))
                                    || ("Days".Equals(strColName ?? "", StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (strCellText.Equals("-200000")) strCellText = "";
                                }
                            }
                            if ((isAllCells) && (curRow4Batch != null))
                            {
                                curRow4Batch[iColIdxTmp] = strCellText;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(strCellText))
                                {
                                    if ((iRow- iStartTargetRow) == 0) strResult = "";
                                    else strResult = $"\r\n{strResult}";
                                }
                                else
                                {
                                    if ((iRow- iStartTargetRow) == 0) strResult = strCellText;
                                    else strResult = $"{strResult}\r\n{strCellText}";
                                }
                            }
                        }
                        else
                        {
                            if (iRow == 0) strResult = "\r\n";
                            else
                                strResult = string.Format("{0}\r\n", strResult);
                        }
                    }
                    if (currentTableInfo.isBatchMode)
                    {
                        currentTableInfo.dataTable.Rows.Add(curRow4Batch);
                    }                       
                }
                if (currentTableInfo.isBatchMode)
                {
                    strResult = "";
                    string strHeader = "";
                    for (int ic =0; ic< currentTableInfo.dataTable.Columns.Count; ic++)
                    {
                        string strHeaderTmp = $"[{currentTableInfo.columns[ic].columnKey}]-[{currentTableInfo.columns[ic].columnCaption}]";
                        if (ic == 0) strHeader = strHeaderTmp;
                        else strHeader = $"{strHeader}[::]{strHeaderTmp}";
                    }
                    for (int ir = 0; ir < currentTableInfo.dataTable.Rows.Count; ir++)
                    {
                        string strRowAsCvs = "";
                        DataRow r = currentTableInfo.dataTable.Rows[ir];
                        for (int ic = 0; ic < currentTableInfo.dataTable.Columns.Count; ic++)
                        {
                            string strCell = (r[ic] == null ? "" : r[ic].ToString());
                            if (ic == 0)
                                strRowAsCvs = strCell;
                            else
                                strRowAsCvs = $"{strRowAsCvs}[::]{strCell}";
                        }
                        if (ir == 0)
                        {
                            strResult = strRowAsCvs;
                        }
                        else
                        {
                            strResult = $"{strResult}\r\n{strRowAsCvs}";
                        }
                    }
                    strResult = $"{strHeader}\r\n{strResult}";
                    //strResult = currentTableInfo.ToJsonString();
                    simpleLog.MarsLoggerSimple.Info("GetAllRowDataForInfragistics", strResult);
                }
                isOk = true;
                return strResult;
            }catch(Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetAllRowDataForInfragistics", ex.Message, ex.StackTrace);
                throw ex;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetAllRowDataForInfragistics", iMark+"");
            }
        }

        private bool GetColumnKeyForInfragisticsGrid(object oSourceControl, string strPegName, List<ConditonSubParameterInfo> condintionColumns, string strObjName, ref string strError, ref string strAdv, ref string strStack)
        {
            MarsTableOperation tableOp = new MarsTableOperation();
            bool isOk = false;
            ArrayList arrcolumns = tableOp.GetColumnsFromInfraGrid(oSourceControl, strPegName, strObjName, ref strError, ref isOk, ref strAdv, ref strStack);
            if ((!isOk) || (arrcolumns == null))
            {
                if (string.IsNullOrEmpty(strError))
                {
                    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = $"can't get columns from [{strPegName}].[{strObjName}]");
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Please check parameter of the test step";
                    return false;
                }
                return false;
            }
            foreach(var itm in condintionColumns)
            {
                if (itm == null) continue;
                tableOp.GetColumnByIdx(arrcolumns, itm.conditionColumn, ref itm.columnCaption, ref itm.columnKey,
                    ref itm.columnKeyId, ref strError, ref strAdv, ref strStack, ref isOk);
                if (!isOk) return false;
            }
            return true;
        }

        private List<MARSColumnsInfo> GetAllColumnsInfo(object oSourceControl, string strPegName, string strObjName, ref bool isOk, 
             ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("GetAllColumnsInfo", $"{iMark}-[{strPegName}].[{strObjName}]");
            try
            {
                List<MARSColumnsInfo> lstRslt = (new MarsTableOperation()).GetAllColumnsInfo(oSourceControl, strPegName, strObjName,ref isOk, ref strError,
                    ref strAdv, ref strStack);
                return lstRslt;
            }catch(Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetAllColumnsInfo",$"{iMark},{ex.Message}",ex.StackTrace );
                strAdv = Resources.mars_contact_to_marquis;
                strError = string.Format(Resources.mars_mq_capture_batch_exception_when_get_colmns, strPegName, strObjName);
                strStack = ex.StackTrace;
                isOk = false;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetAllColumnsInfo", $"[{iMark}]");
            }
        }

        private bool GetColumnKeyForInfragisticsGrid(object oSourceControl, string strColName, 
            string strPegName, string strObjName, 
            ref string strKey, ref int idx, ref string strError, 
            ref string strAdv, ref string strStack,
            int currentIdx=-1)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetColumnKeyForInfragisticsGrid", string.Format("columnName:[{0}]", strColName));
            return (new MarsTableOperation()).GetColumnKeyForInfragisticsGrid(oSourceControl, strColName, strPegName, strObjName, ref strKey, ref idx, ref strError, ref strAdv, ref strStack, currentIdx);
            #region code moved to other class
            //string strCapExpFixed = strColName.Replace(" ", @"\s"); //空格无法在正则表达式中正确处理
            //bool isNoMemberExists = false;
            //object oDisplayLayout = ReflectorForCSharp.GetMember(oSourceControl, "DisplayLayout", ref isNoMemberExists);
            //if (isNoMemberExists)
            //{
            //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid",strError = "no DisplayLayout for Grid, wrong Ultragrid version?" );
            //    return false;
            //}
            //object oBands = ReflectorForCSharp.GetMember(oDisplayLayout,"Bands", ref isNoMemberExists);
            //if (isNoMemberExists)
            //{
            //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Bands for Grid.DisplayLayout, wrong Ultragrid version?");
            //    return false;
            //}
            //object oLstFromBands = ReflectorForCSharp.GetMember(oBands, "List", ref isNoMemberExists);
            //if (isNoMemberExists)
            //{
            //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no List for Grid.DisplayLayout.Bands, wrong Ultragrid version?");
            //    return false;
            //}
            //if (!(oLstFromBands is ArrayList))
            //{
            //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", 
            //        strError = string.Format("List from Grid.DisplayLayout.Bands should be ArrayList, it is [{0}]. wrong Ultragrid version?", oLstFromBands.GetType().ToString()));
            //    return false;
            //}
            ////the first item contains columns
            //object BandsContainsColumn = (oLstFromBands as ArrayList)[0];
            //object ColumnsInBand0 = ReflectorForCSharp.GetMember(BandsContainsColumn, "Columns", ref isNoMemberExists);
            //if (isNoMemberExists)
            //{
            //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Columns for Grid.DisplayLayout.Bands[0], wrong Ultragrid version?");
            //    return false;
            //}
            //object olstColumns = ReflectorForCSharp.GetMember(ColumnsInBand0, "List", ref isNoMemberExists);
            //if (isNoMemberExists)
            //{
            //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid", strError = "no Columns for Grid.DisplayLayout.Bands[0].Columns, wrong Ultragrid version?");
            //    return false;
            //}
            //if ((!(olstColumns is ArrayList))||(olstColumns==null))
            //{
            //    simpleLog.MarsLoggerSimple.Error("GetColumnKeyForInfragisticsGrid",
            //        strError = string.Format("List from Grid.DisplayLayout.Bands[0].Columns should be ArrayList, it is [{0}]. wrong Ultragrid version?", olstColumns.GetType().ToString()));
            //    return false;
            //}
            //ArrayList lstColumns = olstColumns as ArrayList;
            //string strAllKeys = "";
            //int idxTmp = -1;

            //for (int i = 0; i < lstColumns.Count; i++)
            //{
            //    object oColumnItm = lstColumns[i];
            //    if (oColumnItm == null) continue;
            //    object oHeader = ReflectorForCSharp.GetMember(oColumnItm, "Header");

            //    simpleLog.MarsLoggerSimple.Info("\t",string.Format("Header returns:[{0}], total columns:[{1}] - cur:[{2}]", oHeader==null?"NULL":oHeader.ToString(), lstColumns.Count, i));

            //    if (oHeader == null) continue;
            //    bool isHidden = ReflectorForCSharp.GetMemberByType<bool>(oHeader, "Hidden");
            //    if (isHidden) continue;
            //    string caption = ReflectorForCSharp.GetMemberByType<string>(oHeader, "Caption");
            //    string strKeyTmp = ReflectorForCSharp.GetMemberByType<string>(oColumnItm, "Key");
            //    idxTmp = ReflectorForCSharp.GetMemberByType<int>(oColumnItm, "Index");

            //    strAllKeys = string.Format("{0};[{1}]-[{2}]", strAllKeys, caption,strKeyTmp);

            //    if ((Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCapExpFixed, caption))
            //        || (Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCapExpFixed, strKeyTmp))
            //        || (string.Compare(strColName, caption,true) == 0)
            //        || (string.Compare(strColName, strKeyTmp,true) == 0)
            //        )
            //    {
            //        strKey = strKeyTmp;
            //        idx = idxTmp;
            //        return true;
            //    }
            //}
            //strError = string.Format("Can't find column [{1}] from all keys [{0}]", strAllKeys, strColName);
            //return false;
            #endregion //code moved to other class
        }

        internal bool IsDataGridRowReady(object oSourceControl, int ToleranceTime, ref object oRows, ref int iRowCnt, ref string strError, ref string strAdv, ref string strStack)
        {
            int iRowCount = -1;
            //object oRows = null;
            long lstart = DateTime.Now.Ticks, lnow = lstart;
            bool isOkTmp = false, isNotExists = false;
            string strErrorTmp = "", strAdvTmp = "", strStackTmp = "";
            object oRowsTmp = null;
            /// 有时候，系统加载较慢，因此这里检测是否已经加载完毕，通过
            while ((iRowCount <= 0) && (((lnow - lstart) / TimeSpan.TicksPerSecond) < ToleranceTime))
            {
                isOkTmp = true;
                System.Threading.Thread.Sleep(100);
#if _NET4
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                ((System.Windows.Forms.Control)oSourceControl).Invoke(
#endif
                new Action(() =>
                {
                    oRowsTmp = ReflectorForCSharp.GetMember(oSourceControl, "Rows", ref isNotExists);
                    if (isNotExists)
                    {
                        isOkTmp = false;
                        simpleLog.MarsLoggerSimple.Error("\t", strErrorTmp = "Object property [Rows]'s value is NULL in Grid");//"No Rows exists in Grid, Wrong Infragistics version?");
                        strAdvTmp = "Make sure object is a UltraGrid";
                        strStackTmp = MarsErrorStacks.StackTraceDump();
                        return;
                    }
                    iRowCount = ReflectorForCSharp.GetMemberByType<int>(oRowsTmp, "Count");
                }
                ));
                if (!isOkTmp)
                {
                    strError = strErrorTmp;
                    strAdv = strAdvTmp;
                    strStack = strStackTmp;
                    return false;
                }
                oRows = oRowsTmp;
                lnow = DateTime.Now.Ticks;
                iRowCnt = iRowCount;
            }
            return true; // no choice. 可能就是空表
        }

        /// <summary>
        /// result format: MKey-是处理后的对象的名称的列表。如果 strObjectStoreNameInfo = CASH_FLOW,那么可能的数据是：
        /// CASH_FLOW_[2014-12-01]_[2014-12-31],这样可以和其他测试周期的属相比较
        ///
        /// </summary>
        /// <param name="oSourceControl">原始对象</param>
        /// <param name="rowRange">AllRows or others</param>
        /// <param name="keysToFetch">like: start time:end time</param>
        /// <param name="targetColumnName">最终需要获取的列名</param>
        /// <param name="strObjectStoreNameInfo">目标对象名，存入数据库对象名称的浅醉</param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal List<MarsKeyValues<string, string>> CaptureValueFromControl(object oSourceControl, string rowRange,
            List<string> keysToFetch,
            string targetColumnName,
            string strObjectStoreNameInfo,
            string strPegName, string strObjName,
            ref bool isOk,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("CaptureValueFromControl", string.Format("rowRange:[{0}] keys:[{1} TargetColumn:[{2}]]",
                rowRange,
                keysToFetch?.ToList(),
                targetColumnName));

            List<MarsKeyValues<string, string>> lstResult = new List<MarsKeyValues<string, string>>();

            try
            {
                string strTypes = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
                if (!((strTypes.Contains("Infragistics.Win.UltraWinGrid.UltraGrid")
                    || (string.Compare("Infragistics.Win.UltraWinGrid.UltraGrid", oSourceControl.GetType().ToString()) == 0))))
                {
                    strError = string.Format("Not a Infragistics DataTable:[{0}]", strTypes);
                    isOk = false;
                    return null;
                }
                int iRowCount = -1;
                object oRow = null;
                if (!(isOk = IsDataGridRowReady(oSourceControl, 15, ref oRow, ref iRowCount, ref strError, ref strAdv, ref strStack)))
                {
                    return lstResult;
                }
                Dictionary<string, MarsKeyValues<string, int>> DicKeysColumsnAndItsId = new Dictionary<string, MarsKeyValues<string, int>>();
                MarsKeyValues<string, int> targetColumnAndItsId = new MarsKeyValues<string, int>("", -1);
                bool isOkTmp = false;
                string strErrorTmp = "";
                string strAdvTmp = "", strStackTmp = "";
                #region get all columns info into dictionary DicKeysColumsnAndItsId and targetColumnAndItsId
                for (int i = 0; i <= keysToFetch.Count; i++)
                {
                    string strKeyToSearch = "";
                    if (i <= keysToFetch.Count - 1)
                    {
                        strKeyToSearch = keysToFetch[i];
                    }
                    else
                    {
                        strKeyToSearch = targetColumnName;
                    }

                    string strKeyId = "";
                    int iColIdx = -1;
#if _NET4
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                    ((System.Windows.Forms.Control)oSourceControl).Invoke(
#endif
                new Action(() =>
                {
                    isOkTmp = GetColumnKeyForInfragisticsGrid(oSourceControl, strKeyToSearch, strPegName, strObjName, ref strKeyId, ref iColIdx, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                }));
                    if (!isOkTmp)
                    {
                        isOk = false;
                        strError = strErrorTmp;
                        strAdv = strAdvTmp;
                        strStack = strStackTmp;
                        return lstResult;
                    }
                    MarsKeyValues<string, int> FetchColmnInfo = null;
                    if (i <= keysToFetch.Count - 1)
                    {
                        if (DicKeysColumsnAndItsId.ContainsKey(strKeyToSearch))
                        {
                            FetchColmnInfo = DicKeysColumsnAndItsId[strKeyToSearch];
                            if (FetchColmnInfo == null)
                            {
                                DicKeysColumsnAndItsId[strKeyToSearch] = FetchColmnInfo = new MarsKeyValues<string, int>(strKeyId, -1);
                            }
                        }
                        else
                        {
                            DicKeysColumsnAndItsId.Add(strKeyToSearch, FetchColmnInfo = new MarsKeyValues<string, int>("", -1));
                        }
                        FetchColmnInfo.MKey = strKeyId;
                        FetchColmnInfo.MValue = iColIdx;
                    }
                    else
                    {
                        targetColumnAndItsId.MKey = strKeyId;
                        targetColumnAndItsId.MValue = iColIdx;
                    }
                }

                #endregion

                bool isNotExists = false;
                object oRows = ReflectorForCSharp.GetMember(oSourceControl, "Rows", ref isNotExists);
                object[] oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
                string strResult = "";
                for (int i = 0; i < (oAllRows == null ? -1 : oAllRows.Length); i++)
                {
                    object oOneRow = oAllRows[i];

                    if (oOneRow == null)
                    {
                        strResult += "\r\n";
                        continue;
                    }
                    object oCellsCollection = ReflectorForCSharp.GetMember(oOneRow, "Cells"); //Infragistics.Win.UltraWinGrid.CellsCollection
                    if (oCellsCollection == null)
                    {
                        strResult += "\r\n";
                        continue;
                    }
                    object[] allCells = ReflectorForCSharp.GetMemberByType<object[]>(oCellsCollection, "All");
                    if (allCells == null)
                    {
                        strResult += "\r\n";
                        continue;
                    }
                    string strTmpDataKey = string.Format("{0}_", strObjectStoreNameInfo);
                    string strDataToReturnRowKey = strTmpDataKey;
                    string strCellText = "";
                    List<MarsKeyValues<string, string>> tmpChild = new List<MarsKeyValues<string, string>>();
                    #region 获得所有的 key的信息
                    for (int j = 0; j < DicKeysColumsnAndItsId.Keys.Count; j++)
                    {
                        int iColIdx = DicKeysColumsnAndItsId[DicKeysColumsnAndItsId.Keys.ElementAt(j)].MValue;
                        if (allCells.Length <= iColIdx)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Only [{0}] cells returns, but cell index is :[{1}]", allCells.Length, iColIdx));
                            isOk = false;
                            return null;
                        }
                        if (allCells[iColIdx] != null)
                        {
                            strCellText = ReflectorForCSharp.GetMemberByType<string>(allCells[iColIdx], "Text");
                        }
                        else
                        {
                            strCellText = "";
                        }
                        tmpChild.Add(new MarsKeyValues<string, string>(DicKeysColumsnAndItsId.Keys.ElementAt(j),strCellText));
                        strDataToReturnRowKey = string.Format("{0}_[{2}]", strDataToReturnRowKey, DicKeysColumsnAndItsId.Keys.ElementAt(j), strCellText);
                        //build object name and value 
                    }
                    #endregion 获得所有的 key的信息
                    if (allCells[targetColumnAndItsId.MValue] != null)
                    {
                        string strTargetValue = ReflectorForCSharp.GetMemberByType<string>(allCells[targetColumnAndItsId.MValue], "Text");
                        MarsKeyValues<string, string> oneRowResult = new MarsKeyValues<string, string>(strDataToReturnRowKey, strTargetValue);
                        oneRowResult.Children = new List<MarsKeyValues<string, string>>();
                        oneRowResult.Children.AddRange(tmpChild);
                        lstResult.Add(oneRowResult);
                    }
                    else
                    {
                        MarsKeyValues<string, string> oneRowResult = new MarsKeyValues<string, string>(strDataToReturnRowKey, "");
                        oneRowResult.Children = new List<MarsKeyValues<string, string>>();
                        oneRowResult.Children.AddRange(tmpChild);
                        lstResult.Add(oneRowResult);
                    }
                    tmpChild.Clear();
                }
                isOk = true;
                return lstResult;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", strError = e.Message, e);
                strAdv = "Make sure object is a UltraGrid";
                strStack = e.StackTrace;
                isOk = false;
                return null;
            }
        }
    }

    /// <summary>
    /// 转换imagebutton（简单的图片）为Text
    /// </summary>
    public class CaptureValueForSwfButton : CaptureValue
    {
        public override string CaptureValueFromControl(object oSourceControl, string strParameter, string strPegName, string strObjName, 
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack, string strKeyword = "CaptureValue")
        {
            simpleLog.MarsLoggerSimple.logBegin("CaptureValueForSwfButton.CaptureValueFromControl", $"{strParameter}|{strPegName}|{strObjName}|{strKeyword}");
            string strReturnedData = "";
            try
            {
                string strTypeName = oSourceControl.GetType().FullName;
                var inst = MarsObjectTypeMappingManagment.GetObjTypeMappingInst(ref isOk, ref strError, ref strStack);
                if ((!isOk) || (inst == null))
                {
                    strAdv = "please ensure that the configure file is accessble.";
                    simpleLog.MarsLoggerSimple.Error("CaptureValueForSwfButton.CaptureValueFromControl", $"{strError}|{strAdv}|{strStack}");
                    return null;
                }
                /// 从文件中获得
                /// 
                isOk = inst.FindNodeByPegNameAndType(oSourceControl, strTypeName, strPegName, ref strError,ref strAdv, ref strReturnedData);
                if (isOk)
                {
                    return strReturnedData;
                }
                return null;
            }
            catch(Exception e)
            {
                strError = $"There is an exception when Capture data from object.";
                simpleLog.MarsLoggerSimple.Error("CaptureValueFromControl", $"{strError}|{e.Message}\r\n{e.StackTrace}");
                strAdv = "Please check log file for details";
                isOk = false;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("CaptureValueFromControl", $"data returns|{strReturnedData}");
            }
        }
    }
}
