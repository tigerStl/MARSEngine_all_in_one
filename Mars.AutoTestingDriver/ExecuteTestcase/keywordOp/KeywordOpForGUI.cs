extern alias clientWCF;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using com.Mars.Constants;
using Mars.AutoTestingDriver.DataTolerance;
using Mars.AutoTestingDriver.ErrorMessage;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.baseInterfaceAndClass;
using Mars.AutoTestingDriver.injector;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.AutoTestingDriver.Properties;
using Mars.AutoTestingDriver.webSupport;
using Mars.message.Business;
using Mars.message.Inter.MQCenter.keywordOperation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using Mars.AutoTestingDriver.WebHelpers;
using System.ServiceModel.Configuration;
using System.Windows.Forms;
using Mars.AutoTestingDriver.OcrHelper;
using Mars.AutoTestingDriver.MarsImage;
using Mars.message.DataLayer;
using Mars.Inter.MQCenter.MSAASupport;
//using MarsEnginer.windowsWrapper.SystemUtil;
using Mars.AutoTestingDriver.MarsUISupport;
using Mars.message.Utility;
using Mars.Inter.MQCenter.MarsUtility;
using Mars.message.windowsWrapper.SystemUtil;
using Mars.AutoTestingDriver.ApiIntegratedHelper;
using Newtonsoft.Json.Linq;

namespace Mars.AutoTestingDriver.ExecuteTestcase.keywordOp
{

    internal class CsvToDataTable
    {
        public DataTable ConvertCsvToDataTable(string strCSV)
        {
            //reading all the lines(rows) from the file.
            string[] rows = strCSV.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            DataTable dtData = new DataTable();
            string[] rowValues = null;
            DataRow dr = dtData.NewRow();

            //Creating columns
            if (rows.Length > 0)
            {
                foreach (string columnName in rows[0].Split(new string[] { "[::]" },StringSplitOptions.None))
                    dtData.Columns.Add(columnName);
            }

            //Creating row for each line.(except the first line, which contain column names)
            for (int row = 1; row < rows.Length; row++)
            {
                rowValues = rows[row].Split(new string[] { "[::]" }, StringSplitOptions.None);
                dr = dtData.NewRow();
                dr.ItemArray = rowValues;
                dtData.Rows.Add(dr);
            }

            return dtData;
        }

        public void ShowData(DataTable dtData)
        {
            if (dtData != null && dtData.Rows.Count > 0)
            {
                foreach (DataColumn dc in dtData.Columns)
                {
                    Console.Write(dc.ColumnName + " ");
                }
                Console.WriteLine("\n-----------------------------------------------");

                foreach (DataRow dr in dtData.Rows)
                {
                    foreach (var item in dr.ItemArray)
                    {
                        Console.Write(item.ToString() + "      ");
                    }
                    Console.Write("\n");
                }
                Console.ReadKey();
            }
        }
    }


    internal class KeywordBatchCaptureInfo
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeywordBatchCaptureInfo));

        private const string cnst_batch_capture = CaptureValueForSwfTable.cnst_batch_toVar;
        private const string cnst_batch_end = CaptureValueForSwfTable.cnst_batch_toVarEnd;
        internal string pegName { get; set; }
        internal string objName { get; set; }
        // the above two properties were used to locate current Object
        internal bool isBatchMode { get; set; }
        internal string varName { get; set; }
        internal bool isBatchModeByPara(string strPara)
        {

            if (string.IsNullOrEmpty(strPara)) return false;
            Regex rx = new Regex(cnst_batch_capture);
            var match = rx.Match(strPara);

            if (!match.Success) return false;
            varName = match.Groups[0].Value;
            return isBatchMode = true;
        }

        internal bool isBatchEnd(string strPara)
        {
            if (string.IsNullOrEmpty(strPara)) return false;
            Regex rx = new Regex(cnst_batch_end);
            return rx.IsMatch(strPara);
        }

        internal string returnedFromKeyword { get; set; }
        internal DataTable dataTableFromKeyword { get; set; }
        internal bool convertToDT(ref string strError,ref string strStack)
        {
            CsvToDataTable dtOp = new CsvToDataTable();
            try
            {
                dataTableFromKeyword = dtOp.ConvertCsvToDataTable(returnedFromKeyword);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("convertToDT", e.Message, strStack=e.StackTrace);
                strError = Resources.mars_keyword_captureAndCompare_cannot_convertCSV;
                return false;
            }
            
        }
        internal string currentJSONReturnFromCaptureAndCompare { get; set; }
    }

    public class MARSClientCaptureAndCompare
    {
        public const string cnst_allrows = @"^ALLROWS;\S+";
        public string parameter { get; set; }
        public string columnName { get; set; }
        public bool isAllRows { get; set; }
        internal MARSClientCaptureAndCompare(string strPara)
        {
            parameter = strPara;
            analystPara();
        }
        private void analystPara()
        {
            Regex rx = new Regex(cnst_allrows,RegexOptions.IgnoreCase);
            var m = rx.Match(this.parameter);
            if (!m.Success)
            {
                isAllRows = false;
                return;
            }
            columnName = m.Groups[0].Value.Substring("ALLROWS;".Length);
            isAllRows = true;
        }

        internal string GetDataFromDataTable(DataTable dt, ref bool isOk, ref string strError, ref string strStack)
        {
            if (dt == null)
            {
                isOk = false;
                strError = Resources.mars_keyword_captureAndCompare_datatable_null;
                strStack = Environment.StackTrace;
                return "";
            }
            int idx = -1;
            for (int i=0;i<dt.Columns.Count;i++)
            {
                string c = dt.Columns[i].Caption;
                if (MarsWindowsAPIsExtend.RegularTest(columnName, c))
                {
                    idx = i;
                    break;
                }
            }
            if (idx<0)
            {
                strStack = Environment.StackTrace;
                strError = string.Format(Resources.mars_keyword_captureAndCompare_no_column_caption, columnName);
                isOk = false;
                return "";
            }
            string strResult = "";
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var r = dt.Rows[i];
                string strCell = r[idx] == null ? "" : r[idx].ToString();
                if (i == 0) strResult = strCell;
                else strResult = $"{strResult}\r\n{strCell}";
            }
            isOk = true;
            return strResult;
        }
    }

    public class MemoryVariableInfo
    {
        public string keyOfVariable;
        public string values;
        public int status;//0-init, 1-used,2-toremove
    }

    public class NonCaptureParaMgr
    {
        public const string cnst_FromMem = "FromMem:.*(;){0,1}";

        public bool dealWithPrefixPara(string strParameter, ref string strParaNoPrefix, ref string varIdx)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(cnst_FromMem);
            strParaNoPrefix = strParameter;
            var m = r.Match(strParameter);
            if (!m.Success)
            {
                return false;
            }
            strParaNoPrefix = strParameter.Replace(m.Value, "");
            int iPos = m.Value.IndexOf(":");
            if (iPos < 0)
            {
                strParaNoPrefix = strParameter;
                return false;
            }
            varIdx = m.Value.Substring(iPos+1);
            return true;
        }
    }

    public class CaptureParaMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(CaptureParaMgr));
        public const string cnst_toMem = "ToMem:.*(;){0,1}";
        /// <summary>
        /// Add value to memory variable store, if key already exists, overwrite it.
        /// </summary>
        /// <param name="strKey"></param>
        /// <param name="strValue"></param>
        /// <returns></returns>
        public static string AddValueToMemory(string strKey, string strValue)
        {
            if (globalMemoryData.ContainsKey(strKey))
            {
                globalMemoryData.Remove(strKey);
            }
            globalMemoryData.Add(strKey, new MemoryVariableInfo());
            var memvar = globalMemoryData[strKey];
            memvar.keyOfVariable = strKey;
            memvar.status = 0;
            memvar.values = strValue;
            Logger.Debug("AddValueToMemory", $"add key|{strKey}|{memvar.values}");
            return strKey;
        }

        public static string GetPreFixOfParameter(string strPara, ref bool isWithPrefix, ref string noPreFixPara)
        {
            if (string.IsNullOrEmpty(strPara))
            {
                isWithPrefix = false;
                return null;
            }
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(cnst_toMem);
            
            System.Text.RegularExpressions.Match m = r.Match(strPara);
            if (!m.Success)
            {
                isWithPrefix = false;
                return null;
            }
            noPreFixPara = strPara.Replace(m.Value, "");

            int iPos = m.Value.IndexOf(":");
            if (iPos<0)
            {
                isWithPrefix = false;
                return null;
            }
            string strKey = m.Value.Substring(iPos + 1);
            
            if (globalMemoryData.ContainsKey(strKey))
            {
                globalMemoryData.Remove(strKey);
            }
            globalMemoryData.Add(strKey, new MemoryVariableInfo());
            var memvar = globalMemoryData[strKey];
            memvar.keyOfVariable = strKey;
            memvar.status = 0;
            memvar.values = m.Value.Substring(iPos+1);
            Logger.Debug("GetPreFixOfParameter", $"get key|{strKey}|{memvar.values}");
            isWithPrefix = true;
            return strKey ;
        }

        public static bool GetVariableByIdx(string strIdx, ref string strValue, ref string strError,string strMemType="GLOBAL")
        {
            if (string.IsNullOrEmpty(strIdx))
            {
                strError = "No variable is set, please check parameter setting or capture variable first";
                return false;
            }
            if (!globalMemoryData.ContainsKey(strIdx))
            {
                strError = $"Capture Variable [{strIdx}] first before use the variable.";
                return false;
            }
            var v = globalMemoryData[strIdx];
            if (v == null) {
                strError = $"No data for [{strIdx}] is in Memory";
                return false;
            }
            strValue = globalMemoryData[strIdx].values;
            return true;
        }

        public static Dictionary<string, MemoryVariableInfo> globalMemoryData = new Dictionary<string, MemoryVariableInfo>(); // 
        public static Dictionary<string, MemoryVariableInfo> currentTestCaseMemoryData = new Dictionary<string, MemoryVariableInfo>() ; //z暂时 所有的数据都是global的
    }

    public class KeywordOpForGUI
    {
        public static bool IsInjected = false;
        public const string cnst_previewobject      = "PREVIEWOBJECT";
        public const string cnst_autoCheckError     = "AUTOCHECKERROR";
        public const int    cnst_autocheckError_id  = -1;
        public const string cnst_launchApplication  = "LAUNCHAPPLICATION";        
        public const string cnst_clickMunuIcon      = "CLICKMENUICON";
        public const string cnst_selectMenuItem     = "SELECTMENUITEM";
        public const string cnst_fetchAllControls   = "_FETCHALLCONTROLS";
        public const string cnst_createNewObjectsFromSpyer = "CREATENEWOBJECTSFROMSPYER";
        public const string cnst_ConnectToWebView   = "CONNECTTOWEBVIEW";

        public static Dictionary<string, MarsKeywordOperation> GUIKeyword = new Dictionary<string, MarsKeywordOperation>()
        {
            {"ADDDays"                  , MARSKEYWORD_AddDays },
            {APIEngineHelper.CNST_KEYWORD_API_SET_VARIABLE.ToUpper(), MARSKEYWORD_APISetVariable},
            {cnst_autoCheckError        , MARSKEYWORD_AutoCheckError },
            {"CAPTUREANDCOMPARE"        , MARSKEYWORD_CaptureAndCompare},
            {"CAPTUREANDCOMPAREBYKEY"   , MARSKEYWORD_CaptureAndCompareByKey},
            {"CAPTUREVALUE"             , MARSKEYWORD_CaptureValue},
            {cnst_createNewObjectsFromSpyer, MARSKEYWORD_createNewObjectsFromSpyer },
            {"CHECKERROR"               , MARSKEYWORD_CheckError},
            {"CLICKAT"                  , MARSKEYWORD_ClickAt},
            {"CLICKBUTTON"              , MARSKEYWORD_ClickButton},
            {cnst_clickMunuIcon         , MARSKEYWORD_ClickMenuIcon},
            {"CLICKPOPUPMENUITEM"       , MARSKEYWORD_ClickPopupMenuItem},
            {"CLOSEWINDOW"              , MARSKEYWORD_CloseWindow},
            {"CLICKRADIOBUTTON"         , MARSKEYWORD_ClickRadioButton},
            {cnst_ConnectToWebView      , MARSKEYWORD_ConnectToWebView},
            {"DISMISS"                  , MARSKEYWORD_Dismiss},
            {APIEngineHelper.CNST_KEYWORD_EXTRACT_DATA_FROM_API.ToUpper() , MARSKEYWORD_ExtractDataFromAPI},
            {APIEngineHelper.CNST_KEYWORD_EXTRACT_ARRAY_FROM_API.ToUpper(), MARSKEYWORD_ExtractArrayFromAPI},
            {"FILLEDIT"                 , MARSKEYWORD_FillEdit},
            {"FILLTABLE"                , MARSKEYWORD_FillTable},
            {"INSERTROW"                , MARSKEYWORD_InsertRow},
            {APIEngineHelper.CNST_KEYWORD_INVOKE_API.ToUpper(), MARSKEYWORD_INVOKEAPI},
            //{"LAUNCHAPPLICATION"        , MARSKEYWORD_LaunchApplication },
            {cnst_launchApplication     , MARSKEYWORD_LaunchApplication },
            {"MAXIMIZEWINDOW"           , MARSKEYWORD_MaximizeWindow },
            {"OCRIMAGE"                 , MARSKEYWORD_OCRIMAGE },
            {"PEGWINDOW"                , MARSKEYWORD_PegWindow},
            {cnst_previewobject         , MARSKEYWORD_PreviewObject },
            {"PRESSKEYS"                , MARSKEYWORD_PressKey},
            {"SCROLLDOWN"               , MARSKEYWORD_ScrollDown},
            {"SCROLLUP"                 , MARSKEYWORD_ScrollUp},
            {"SCROLLLEFT"               , MARSKEYWORD_ScrollLeft},
            {"SCROLLGRIDTOLEFT"         , MARSKEYWORD_ScrollLeft},
            {"SCROLLRIGHT"              , MARSKEYWORD_ScrollRight},
            {"SCROLLGRIDTORIGHT"        , MARSKEYWORD_ScrollRight},
            {"SCROLLWINDOW"             , MARSKEYWORD_ScrollWindow},
            {"SEARCHANDCLICK"           , MARSKEYWORD_SearchAndClick},
            {"SEARCHANDUPDATE"          , MARSKEYWORD_SearchAndUpdate},
            {"SEARCHANDUPDATETABLE"     , MARSKEYWORD_SearchAndUpdate},
            {"SELECTLISTITEM"           , MARSKEYWORD_SELECTLISTITEM},
            {"SELECTDROPDOWN"           , MARSKEYWORD_SelectDropDown},
            {cnst_selectMenuItem        , MARSKEYWORD_SelectMenuItem},
            {"SELECTTAB"                , MARSKEYWORD_SelectTab},
            {"SETBOX"                   , MARSKEYWORD_SetBox},
            {"SETSPLITTER"              , MARSKEYWORD_SetSplitter},
            {"SNAPSHOT"                 , MARSKEYWORD_SnapShot},
            {"VERIFYVALUE"              , MARSKEYWORD_VerifyValue},
            {"WAITMFACODE"              , MARSKEYWORD_WaitMFACode },
            {"WAITUNTIL"                , MARSKEYWORD_WaitUntil},
            {"WEBSWITCHTOROOT"          , MARSKEYWORD_SwitchToRoot},
            {"_STARTOBJECTSPY"          , MARSKEYWORD_StartObjectSpy},
            {"_RELOADKEYWORD_TYPE_MAP"  , MARSKEYWORD_ReloadKeyword_type_Map },
            {cnst_fetchAllControls      , MARSKEYWORD_FetchAllControls },
        };



        private static MLogger Logger = MLogger.GetLogger(typeof(KeywordOpForGUI));

        internal static bool RunKeywordByKeywordName(long runOrdId,
            string strKeyword,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strParameter,
            string strData,
            string strAPIRunTimeConfig,
            string strAttachInfo,
            Mars_applicationTyp.MARS_APPTYPE appTyp,
            string strDbIdx,
            ref string strError,
            ref MARSDealResult dealResult,
            bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("RunKeywordByKeywordName", string.Format("Keyword:[{0}] Parameter:[{1}] Data:[{2}]", strKeyword, strParameter, strData));
            string strKeywordUpper = strKeyword == null ? "" : strKeyword.ToUpper();
            if (!GUIKeyword.Keys.Any(p => string.Compare(p, strKeywordUpper, true) == 0))
            {
                strError = string.Format("no supported keyword:[{0}]", strKeyword);
                return false;
            }
            return GUIKeyword[strKeywordUpper](runOrdId, strParameter??"", strData, strAPIRunTimeConfig, stepObject, strAttachInfo, ref strError, 
                    ref dealResult, appTyp, strDbIdx, isAttachUIAAHwnd:isAttachUIAAHwnd);
        }

        /// <summary>
        /// the method should run once per test storyboard, force engine reload the config file
        /// </summary>
        /// <returns></returns>
        public static bool RefreshDefaultKeywordTypeMapping()
        {
            Logger.logBegin("RunKeywordByKeywordName");
            try
            {
                /// just create a fake info
                /// 
                MARSDealResult tmpRslt = new MARSDealResult();
                string strError = "";
                MARSKEYWORD_ReloadKeyword_type_Map(-0xff, "", "","", null, "", ref strError, ref tmpRslt);
                return true;
            }
            catch (Exception e)
            {
                return true;
            }
            finally
            {
                
                Logger.logEnd("RunKeywordByKeywordName");
                
            }
        }

        internal static bool IsAGuiKeywordName(string strKeywordName)
        {
            if (string.IsNullOrEmpty(strKeywordName)) return false;
            foreach (var itm in GUIKeyword.Keys)
            {
                if (itm == null) continue;
                if (strKeywordName.Equals(itm, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool MARSKEYWORD_FetchAllControls(long lStepId, string strParaMeter, string strData, string strApiRunTimeConfig, B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo, ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_FetchAllControls", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = true;
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp(cnst_fetchAllControls, lStepId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <param name="isAttachUIAAHwnd"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_INVOKEAPI(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
                    B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
                    ref MARSDealResult dealResult,
                    Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
                    string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_INVOKEAPI", string.Format("Parameter:[{0}] Data:[{1}] runOrdId:[{2}]", strParaMeter, strData, runOrdId));
            
            try
            {
                // Initialize dealResult if null
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }

                // Prepare ref parameters for APIEngineInvokeImpl.InvokeAPI
                string resultMessage = "";
                string errorMessage = "";
                string returnedData = "";
                string stackInfo = "";
                string advice = "";
                DateTime askTime = DateTime.Now;
                DateTime ackTime = DateTime.Now;

                // Delegate to APIEngineInvokeImpl.InvokeAPI
                bool result = APIEngineInvokeImpl.InvokeAPI(runOrdId, strApiRunTimeConfig, ref strError,
                    ref resultMessage, ref errorMessage, ref returnedData,
                    ref stackInfo, ref advice, ref askTime, ref ackTime);

                // Set the returned values to dealResult
                dealResult.ResultMessage = resultMessage;
                dealResult.ErrorMessage = errorMessage;
                dealResult.ReturnedData = returnedData;
                dealResult.StackInfo = stackInfo;
                dealResult.Advice = advice;
                dealResult.AskTime = askTime;
                dealResult.AckTime = ackTime;
                
                Logger.logEnd("MARSKEYWORD_INVOKEAPI");
                return result;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSKEYWORD_INVOKEAPI: {ex.Message}";
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.ErrorMessage = strError;
                dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                dealResult.Advice = "Please check API Object's settings";
                dealResult.AckTime = DateTime.Now;
                Logger.Error("MARSKEYWORD_INVOKEAPI", ex.Message, ex);
                Logger.logEnd("MARSKEYWORD_INVOKEAPI");
                return false;
            }
        }

        /// <summary>
        /// Sets a variable before api INVOCKED. 就是将api支持的variable 和value放到内存中，供api调用时候使用。对象是 APIEngineVariableMgr
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter">api variable的index</param>
        /// <param name="strData"></param>
        /// <param name="strApiRunTimeConfig"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <param name="isAttachUIAAHwnd"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_APISetVariable(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
                    B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
                    ref MARSDealResult dealResult,
                    Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
                    string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_APISetVariable", string.Format("Parameter:[{0}] Data:[{1}] RunOrdId:[{2}]", strParaMeter, strData, runOrdId));

            try
            {
                // Initialize dealResult if null
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }

                // Step 1: Validate parameter (variable name)
                if (string.IsNullOrEmpty(strParaMeter))
                {
                    strError = "Parameter (variable name) is empty";
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = strError;
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.Advice = "Please provide a variable name in the Parameter field";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("MARSKEYWORD_APISetVariable", strError);
                    Logger.logEnd("MARSKEYWORD_APISetVariable");
                    return false;
                }

                string variableName = strParaMeter.Trim();
                Logger.Info("MARSKEYWORD_APISetVariable", string.Format("Variable name: [{0}]", variableName));

                // Step 2: Validate and process strApiRunTimeConfig (value)
                if (string.IsNullOrEmpty(strApiRunTimeConfig))
                {
                    strError = "strApiRunTimeConfig (value) is empty";
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = strError;
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.Advice = "Please provide a value in strApiRunTimeConfig. It can be 'fromMem:VARIABLE_NAME' or a direct string value";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("MARSKEYWORD_APISetVariable", strError);
                    Logger.logEnd("MARSKEYWORD_APISetVariable");
                    return false;
                }

                string value = strApiRunTimeConfig.Trim();
                
                // Step 2.1: Check if strApiRunTimeConfig starts with "fromMem:"
                if (value.StartsWith("fromMem:", StringComparison.OrdinalIgnoreCase))
                {
                    string memoryVariableName = value.Substring(8); // Remove "fromMem:" prefix
                    if (string.IsNullOrEmpty(memoryVariableName))
                    {
                        strError = "Memory variable name is empty after 'fromMem:' prefix";
                        dealResult.ResultMessage = $"FAILED,{strError}";
                        dealResult.ErrorMessage = strError;
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = "Please provide a valid variable name after 'fromMem:'";
                        dealResult.AckTime = DateTime.Now;
                        Logger.Error("MARSKEYWORD_APISetVariable", strError);
                        Logger.logEnd("MARSKEYWORD_APISetVariable");
                        return false;
                    }

                    // Get value from CaptureParaMgr
                    string memValue = "";
                    string memError = "";
                    bool memResult = CaptureParaMgr.GetVariableByIdx(memoryVariableName, ref memValue, ref memError);
                    
                    if (!memResult)
                    {
                        strError = $"Failed to get memory variable '{memoryVariableName}' from CaptureParaMgr: {memError}";
                        dealResult.ResultMessage = $"FAILED,{strError}";
                        dealResult.ErrorMessage = strError;
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = $"Please ensure that memory variable '{memoryVariableName}' has been set before using APISetVariable";
                        dealResult.AckTime = DateTime.Now;
                        Logger.Error("MARSKEYWORD_APISetVariable", strError);
                        Logger.logEnd("MARSKEYWORD_APISetVariable");
                        return false;
                    }

                    if (string.IsNullOrEmpty(memValue))
                    {
                        strError = $"Memory variable '{memoryVariableName}' exists but has no value";
                        dealResult.ResultMessage = $"FAILED,{strError}";
                        dealResult.ErrorMessage = strError;
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = $"Please ensure that memory variable '{memoryVariableName}' has a valid value";
                        dealResult.AckTime = DateTime.Now;
                        Logger.Error("MARSKEYWORD_APISetVariable", strError);
                        Logger.logEnd("MARSKEYWORD_APISetVariable");
                        return false;
                    }

                    value = memValue;
                    Logger.Info("MARSKEYWORD_APISetVariable", string.Format("Retrieved value from memory variable '{0}': [{1}]", memoryVariableName, value));
                }

                Logger.Info("MARSKEYWORD_APISetVariable", string.Format("Value to set: [{0}]", value));

                // Step 3: Set variable using APIEngineVariableMgr
                bool result = APIEngineVariableMgr.SetVariable(variableName, value, ref strError);

                if (!result)
                {
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = strError;
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.Advice = "Please check the variable name and value format. Value can be 'fromMem:VARIABLE_NAME' or a direct string";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("MARSKEYWORD_APISetVariable", strError);
                    Logger.logEnd("MARSKEYWORD_APISetVariable");
                    return false;
                }

                // Success
                dealResult.ResultMessage = "SUCCESS";
                dealResult.AckTime = DateTime.Now;
                Logger.Info("MARSKEYWORD_APISetVariable", string.Format("Variable '{0}' set successfully", variableName));
                Logger.logEnd("MARSKEYWORD_APISetVariable");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSKEYWORD_APISetVariable: {ex.Message}";
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.ErrorMessage = strError;
                dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                dealResult.Advice = "Please check the variable name and value format";
                dealResult.AckTime = DateTime.Now;
                Logger.Error("MARSKEYWORD_APISetVariable", ex.Message, ex);
                Logger.logEnd("MARSKEYWORD_APISetVariable");
                return false;
            }
        }

        /// <summary>
        /// Extracts data from API response using JSONPath
        /// </summary>
        /// <param name="runOrdId">The test run order ID</param>
        /// <param name="strParaMeter">JSONPath expression to extract data</param>
        /// <param name="strData">Data parameter, usually in format "ToMem:API_TRADE_ID"</param>
        /// <param name="strApiRunTimeConfig">API runtime configuration (not used in this method)</param>
        /// <param name="stepObject">Step object snapshot</param>
        /// <param name="strAttachInfo">Attach info</param>
        /// <param name="strError">Error message output</param>
        /// <param name="dealResult">Deal result output</param>
        /// <param name="appTyp">Application type</param>
        /// <param name="strDBIdx">Database index</param>
        /// <param name="dataSetBackCallBack">Data set callback</param>
        /// <param name="isAttachUIAAHwnd">Whether to attach UIAA hwnd</param>
        /// <returns>True if successful, false otherwise</returns>
        private static bool MARSKEYWORD_ExtractDataFromAPI(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
                    B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo, ref string strError,
                    ref MARSDealResult dealResult,
                    Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
                    string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ExtractDataFromAPI", string.Format("Parameter:[{0}] Data:[{1}] RunOrdId:[{2}]", strParaMeter, strData, runOrdId));
            
            try
            {
                // Step 1: Parse strData to get memory variable index
                string memoryIndex = null;
                
                // Check if strData starts with "ToMem:"
                if (!string.IsNullOrEmpty(strApiRunTimeConfig) && strApiRunTimeConfig.StartsWith("ToMem:", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract memory index from "ToMem:VARIABLE_NAME" format using CaptureParaMgr
                    bool isWithPrefix = false;
                    string noPreFixPara = "";
                    memoryIndex = CaptureParaMgr.GetPreFixOfParameter(strApiRunTimeConfig, ref isWithPrefix, ref noPreFixPara);
                    
                    if (!isWithPrefix || string.IsNullOrEmpty(memoryIndex))
                    {
                        strError = $"Invalid strData format. Expected format: 'ToMem:VARIABLE_NAME', but got: '{strApiRunTimeConfig}'";
                        if (dealResult == null)
                        {
                            dealResult = new MARSDealResult();
                        }
                        dealResult.ResultMessage = $"FAILED,{strError}";
                        dealResult.ErrorMessage = strError;
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = "Please check the Data parameter format. It should be 'ToMem:VARIABLE_NAME'";
                        dealResult.AckTime = DateTime.Now;
                        Logger.Error("MARSKEYWORD_ExtractDataFromAPI", strError);
                        Logger.logEnd("MARSKEYWORD_ExtractDataFromAPI");
                        return false;
                    }
                }
                else
                {
                    // If strData doesn't start with "ToMem:", use strData itself as memoryIndex
                    // No need to get object from CaptureParaMgr
                    memoryIndex = strApiRunTimeConfig?.Trim();
                    
                    if (string.IsNullOrEmpty(memoryIndex))
                    {
                        strError = "strData is empty";
                        if (dealResult == null)
                        {
                            dealResult = new MARSDealResult();
                        }
                        dealResult.ResultMessage = $"FAILED,{strError}";
                        dealResult.ErrorMessage = strError;
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = "Please provide a memory variable name in the Data parameter";
                        dealResult.AckTime = DateTime.Now;
                        Logger.Error("MARSKEYWORD_ExtractDataFromAPI", strError);
                        Logger.logEnd("MARSKEYWORD_ExtractDataFromAPI");
                        return false;
                    }
                    
                    Logger.Info("MARSKEYWORD_ExtractDataFromAPI", string.Format("Using strData directly as memoryIndex: [{0}] (not ToMem: format)", memoryIndex));
                }

                Logger.Info("MARSKEYWORD_ExtractDataFromAPI", string.Format("Memory index: [{0}]", memoryIndex));

                // Initialize dealResult if null
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }

                // Prepare ref parameters for APIEngineInvokeImpl.ExtractDataFromAPI
                string resultMessage = "";
                string errorMessage = "";
                string returnedData = "";
                string stackInfo = "";
                string advice = "";
                DateTime askTime = DateTime.Now;
                DateTime ackTime = DateTime.Now;

                // Step 2: Delegate to APIEngineInvokeImpl.ExtractDataFromAPI
                string extractedMemoryIndex;
                bool result = APIEngineInvokeImpl.ExtractDataFromAPI(runOrdId, strParaMeter, strApiRunTimeConfig, 
                    out extractedMemoryIndex, ref strError,
                    ref resultMessage, ref errorMessage, ref returnedData,
                    ref stackInfo, ref advice, ref askTime, ref ackTime);
                
                // Set the returned values to dealResult
                dealResult.ResultMessage = resultMessage;
                dealResult.ErrorMessage = errorMessage;
                dealResult.ReturnedData = returnedData;
                dealResult.StackInfo = stackInfo;
                dealResult.Advice = advice;
                dealResult.AskTime = askTime;
                dealResult.AckTime = ackTime;

                // Verify that the extracted memory index matches
                if (result && !string.Equals(memoryIndex, extractedMemoryIndex, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("MARSKEYWORD_ExtractDataFromAPI", string.Format("Warning: Memory index mismatch. CaptureParaMgr: [{0}], APIEngineInvokeImpl: [{1}]", 
                        memoryIndex, extractedMemoryIndex));
                }

                if (!result)
                {
                    Logger.logEnd("MARSKEYWORD_ExtractDataFromAPI");
                    return false;
                }

                // Step 3: Store the extracted value in globalMemoryData
                string extractedValueStr = returnedData;
                if (CaptureParaMgr.globalMemoryData.ContainsKey(memoryIndex))
                {
                    var memVar = CaptureParaMgr.globalMemoryData[memoryIndex];
                    memVar.values = extractedValueStr;
                    memVar.status = 0; // 0 typically means success
                    Logger.Info("MARSKEYWORD_ExtractDataFromAPI", string.Format("Value stored in globalMemoryData with key: [{0}], value: [{1}]", memoryIndex, extractedValueStr));
                }
                else
                {
                    strError = $"Memory variable '{memoryIndex}' was not initialized. Please ensure 'ToMem:{memoryIndex}' format is correct";
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = strError;
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.Advice = "The memory variable should be initialized by CaptureParaMgr.GetPreFixOfParameter";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("MARSKEYWORD_ExtractDataFromAPI", strError);
                    Logger.logEnd("MARSKEYWORD_ExtractDataFromAPI");
                    return false;
                }

                Logger.Info("MARSKEYWORD_ExtractDataFromAPI", string.Format("Data extraction completed successfully. Value: [{0}]", extractedValueStr));
                Logger.logEnd("MARSKEYWORD_ExtractDataFromAPI");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSKEYWORD_ExtractDataFromAPI: {ex.Message}";
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.ErrorMessage = strError;
                dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                dealResult.Advice = "Please check the API response data and JSONPath expression";
                dealResult.AckTime = DateTime.Now;
                Logger.Error("MARSKEYWORD_ExtractDataFromAPI", ex.Message, ex);
                Logger.logEnd("MARSKEYWORD_ExtractDataFromAPI");
                return false;
            }
        }

        /// <summary>
        /// json的array中有两种模式，一种是简单类型的array，比如["a","b","c"]，另一种是复杂类型的array，比如[{"key1":"value1", "key2":"value2"},{"key1":"value1","key2":"value2"}]
        /// 对于第二种类型的array，需要用户在parameter中指定需要PairedKeys,在金融数据中，通常一个交易会有至少一个key来唯一标识或者排序。如cashflow的date。所以，parameter是由两个部分组成，
        /// 第一部分是array的排序的key的jsonpath，第二部分需要取值的jsonpath。如:$.cashflow.StartDate::$.cashflow.Notional.表示获取notional，按照starte排序。
        /// 第二部分是可以选的，如果只有一个，就表示选择第一个jsonpath。
        /// 为避免歧义，使用::分隔两个部分。
        /// API的数据和ExtratDataFromAPI一样，放在globalMemoryData中。
        /// 
        /// </summary>
        /// <param name="lStepId"></param>
        /// <param name="strParaMeter">$.CashFlows[?(@.AssetId=={fromMem:API_PAYSIDE_ASSETID})].Flows[*].StartDate::Flows
        /// 
        /// </param>
        /// <param name="strData">通常是</param>
        /// <param name="strApiRunTimeConfig"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <param name="isAttachUIAAHwnd"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_ExtractArrayFromAPI(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig, B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo, ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            int iMark = new Random().Next(10000);
            Logger.logBegin("MARSKEYWORD_ExtractArrayFromAPI", string.Format("{3}|Parameter:[{0}] Data:[{1}] RunOrdId:[{2}]", strParaMeter, strData, runOrdId, iMark));

            try
            {
                // Step 1: Parse strData to get memory variable index using CaptureParaMgr
                bool isWithPrefix = false;
                string noPreFixPara = "";
                string memoryIndex = CaptureParaMgr.GetPreFixOfParameter(strApiRunTimeConfig, ref isWithPrefix, ref noPreFixPara);
                
                if (!isWithPrefix || string.IsNullOrEmpty(memoryIndex))
                {
                    if (!string.IsNullOrEmpty(strApiRunTimeConfig))
                    {
                        /// 可以直接给一个对象名称
                        memoryIndex = strApiRunTimeConfig;
                    }
                    else
                    {
                        strError = $"Invalid strData format. Expected format: 'ToMem:VARIABLE_NAME', but got: '{strApiRunTimeConfig}'";
                        if (dealResult == null)
                        {
                            dealResult = new MARSDealResult();
                        }
                        dealResult.ResultMessage = $"FAILED,{strError}";
                        dealResult.ErrorMessage = strError;
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = "Please check the Data parameter format. It should be 'ToMem:VARIABLE_NAME'";
                        dealResult.AckTime = DateTime.Now;
                        Logger.Error("MARSKEYWORD_ExtractArrayFromAPI", strError);
                        Logger.logEnd("MARSKEYWORD_ExtractArrayFromAPI");
                        return false;
                    }
                }

                Logger.Info("MARSKEYWORD_ExtractArrayFromAPI", string.Format("Memory index extracted: [{0}]", memoryIndex));

                // Initialize dealResult if null
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }

                // Step 1.5: Process strParaMeter to replace {fromMem:VARIABLE_NAME} patterns
                string processedStrParaMeter = strParaMeter;
                if (!string.IsNullOrEmpty(strParaMeter) && strParaMeter.Contains("{"))
                {
                    // Find all {fromMem:VARIABLE_NAME} patterns
                    System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\{fromMem:([^}]+)\}");
                    var matches = regex.Matches(strParaMeter);
                    
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        string fullMatch = match.Value; // e.g., "{fromMem:API_PAYSIDE_ASSETID}"
                        string variableName = match.Groups[1].Value; // e.g., "API_PAYSIDE_ASSETID"
                        
                        // Get value from CaptureParaMgr
                        string memValue = "";
                        string memError = "";
                        bool memResult = CaptureParaMgr.GetVariableByIdx(variableName, ref memValue, ref memError);
                        
                        if (!memResult)
                        {
                            strError = $"Failed to get memory variable '{variableName}' from CaptureParaMgr: {memError}";
                            dealResult.ResultMessage = $"FAILED,{strError}";
                            dealResult.ErrorMessage = strError;
                            dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                            dealResult.Advice = $"Please ensure that memory variable '{variableName}' has been set before using ExtractArrayFromAPI";
                            dealResult.AckTime = DateTime.Now;
                            Logger.Error("MARSKEYWORD_ExtractArrayFromAPI", strError);
                            Logger.logEnd("MARSKEYWORD_ExtractArrayFromAPI");
                            return false;
                        }

                        if (string.IsNullOrEmpty(memValue))
                        {
                            strError = $"Memory variable '{variableName}' exists but has no value";
                            dealResult.ResultMessage = $"FAILED,{strError}";
                            dealResult.ErrorMessage = strError;
                            dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                            dealResult.Advice = $"Please ensure that memory variable '{variableName}' has a valid value";
                            dealResult.AckTime = DateTime.Now;
                            Logger.Error("MARSKEYWORD_ExtractArrayFromAPI", strError);
                            Logger.logEnd("MARSKEYWORD_ExtractArrayFromAPI");
                            return false;
                        }
                        
                        // Replace {fromMem:VARIABLE_NAME} with actual value
                        processedStrParaMeter = processedStrParaMeter.Replace(fullMatch, $"'{memValue}'");
                        Logger.Info("MARSKEYWORD_ExtractArrayFromAPI", string.Format("Replaced '{0}' with '{1}' in strParaMeter", fullMatch, memValue));
                    }
                    
                    Logger.Info("MARSKEYWORD_ExtractArrayFromAPI", string.Format("Original strParaMeter: [{0}], Processed: [{1}]", strParaMeter, processedStrParaMeter));
                }

                // Prepare ref parameters for APIEngineInvokeImpl.ExtractArrayFromAPI
                string resultMessage = "";
                string errorMessage = "";
                string returnedData = "";
                string stackInfo = "";
                string advice = "";
                DateTime askTime = DateTime.Now;
                DateTime ackTime = DateTime.Now;

                // Step 2: Delegate to APIEngineInvokeImpl.ExtractArrayFromAPI
                string extractedMemoryIndex;
                bool result = APIEngineInvokeImpl.ExtractArrayFromAPI(runOrdId, processedStrParaMeter, strApiRunTimeConfig,
                    out extractedMemoryIndex, ref strError,
                    ref resultMessage, ref errorMessage, ref returnedData,
                    ref stackInfo, ref advice, ref askTime, ref ackTime);

                // Set the returned values to dealResult
                dealResult.ResultMessage = resultMessage;
                dealResult.ErrorMessage = errorMessage;
                dealResult.ReturnedData = returnedData;
                dealResult.StackInfo = stackInfo;
                dealResult.Advice = advice;
                //dealResult.AskTime = askTime;
                dealResult.AckTime = ackTime;

                // Verify memory index matches
                if (!string.Equals(memoryIndex, extractedMemoryIndex, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("MARSKEYWORD_ExtractArrayFromAPI", string.Format("Warning: Memory index mismatch. CaptureParaMgr: [{0}], APIEngineInvokeImpl: [{1}]",
                        memoryIndex, extractedMemoryIndex));
                }

                if (!result)
                {
                    Logger.logEnd("MARSKEYWORD_ExtractArrayFromAPI");
                    return false;
                }

                /// Step 3: Store the extracted array value in globalMemoryData
                /// if !isWithPrefix then return directly
                string extractedValueStr = returnedData;
                if (!isWithPrefix)
                {
                    dealResult.ResultMessage = "SUCCESS";
                    dealResult.ReturnedData = returnedData;
                    dealResult.AckTime = DateTime.Now;
                    Logger.logEnd("MARSKEYWORD_ExtractArrayFromAPI", $"{iMark}|return TRUE|{returnedData.Length}");
                    return true;
                }
                CaptureParaMgr.AddValueToMemory(memoryIndex, returnedData);
                dealResult.ResultMessage = "SUCCESS";
                dealResult.ReturnedData = returnedData;
                dealResult.AckTime = DateTime.Now;
                
                Logger.logEnd("MARSKEYWORD_ExtractArrayFromAPI", $"{iMark}|return true");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSKEYWORD_ExtractArrayFromAPI: {ex.Message}";
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.ErrorMessage = strError;
                dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                dealResult.Advice = "Please check the API response data and JSONPath expression";
                dealResult.AckTime = DateTime.Now;
                Logger.Error("MARSKEYWORD_ExtractArrayFromAPI", ex.Message, ex);
                Logger.logEnd("MARSKEYWORD_ExtractArrayFromAPI");
                return false;
            }
        }


        private static bool MARSKEYWORD_ReloadKeyword_type_Map(long lStepId, string strParaMeter, string strData, string strApiRunTimeConfig, B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo, ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ReloadKeyword_type_Map", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            //bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            //if (!isOk) return false;
            bool isOk = true;
            if (stepObject == null) return true;
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("_RELOADKEYWORD_TYPE_MAP", lStepId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }


        private static bool MARSKEYWORD_SelectTab(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SelectTab", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                return MARSWebDriver.GetInstance().WebSelectTab("SELECTTAB",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult
                    );
            }
            
            if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP) || (isOk = Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.ISUsingMARSUI(dictPegProperties, ref strError)))
            {
                return MarsMARSUIHelper.MARSUI_SelectTab(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME,
                    strAttachInfo, stepObject.PEG_NAME ?? "", stepObject.OBJECT_HAPPY_NAME ?? "", ref strError, ref dealResult);
            }


            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SELECTTAB", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        private static bool MARSKEYWORD_SetBox(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SetBox", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            var isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
            if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE) || (isForWEB))
            {
                return MARSWebDriver.GetInstance().WebSetBox("SetBox",
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult,
                        isForWEB);
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SETBOX", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        private static bool MARSKEYWORD_SetSplitter(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SetSplitter", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SETSPLITTER", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        private static bool MARSKEYWORD_PreviewObject(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_PreviewObject", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            if (stepObject == null)
            {
                Logger.Error("MARSKEYWORD_PreviewObject", strError = "object for preview is NULL");
                return false;
            }
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            if (appTyp== Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                //not implemented
                return true;
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("PREVIEWOBJECT", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        private static bool MARSKEYWORD_PressKey(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_PresssKey", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();

            /// 如果没有操作对象，则判断是否有parameter，且为current_pos, current_position
            /// 
            if ((string.IsNullOrEmpty(stepObject.PEG_QUICK_ACCESS)) && ((!string.IsNullOrEmpty(strParaMeter)) &&
                ("CURRENT_POS".Equals(strParaMeter, StringComparison.OrdinalIgnoreCase) ||
                 "CURRENT_POSITION".Equals(strParaMeter, StringComparison.OrdinalIgnoreCase)
                )
            ))
            {
                Logger.Info("MARSKEYWORD_PresssKey", $"no object mode, just presskes|{strData}|{strParaMeter}");
                System.Threading.Thread.Sleep(200);
                System.Windows.Forms.SendKeys.SendWait(strData);                
                System.Threading.Thread.Sleep(200);
                dealResult.ResultMessage = "SUCCESS";
                dealResult.AskTime = DateTime.Now;
                dealResult.ReturnedData = strData;
                return true;
            }

            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            ///添加对无障碍接口的支持
            ///
            
            //if (isOk = MarsMSAASupport.MARSMSAAHelper.IsUsingMSAA(dictPegProperties, dictObjProperties, ref strError))
            if (isOk = Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.ISUsingMARSUI(dictObjProperties ?? dictPegProperties, ref strError))
            {
                /// old version,使用了Iaccessible接口的方式
                /// 
                return MarsMARSUIHelper.MARSUI_PressKeys(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult);
                //return MarsMSAASupport.MARSMSAAHelper.MARSStandard_PressKey(stepId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                //    stepObject.TYPE_NAME,
                //    strAttachInfo,
                //    stepObject.PEG_NAME ?? "",
                //    stepObject.OBJECT_HAPPY_NAME ?? "",
                //    ref strError,
                //    ref dealResult);
            }

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                return MARSWebDriver.GetInstance().WebPressKey("PressKey",
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("PRESSKEYS", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }
        
        private static bool MARSKEYWORD_WaitUntil(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_WaitUntil", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));

            string strDataSrc = strData ?? "";

            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            int iWaitTime = 200;
            int.TryParse(strParaMeter, out iWaitTime);
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("WAITUNTIL", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult,
                iWaitTime);
            return isOk;
        }


        private static bool MARSKEYWORD_VerifyValue(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_VerifyValue", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));

            string strDataSrc = strData ?? "";

            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("VERIFYVALUE", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            //判断是否存在tolerance
            if (MarsVerifyValueToleranceHost.IsTorleranceFuncRequired(strDataSrc))
            {
                if (dealResult == null)
                {
                    strError = "no data return from Application MARS agent.";
                    return false;
                }
                isOk = MarsVerifyValueToleranceHost.VerifyValueWithTorleranceFunc(strDataSrc, dealResult.ReturnedData, ref strError);
                if (!isOk)
                    dealResult.ResultMessage = strError;
                return isOk;
            }

            return isOk;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter">如果strParaMeter 由：DefaultAgent,那么从DefaultAgent调用snapshot方法</param>
        /// <param name="strData">存储的文件名称</param>
        /// <param name="strApiRunTimeConfig"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <param name="isAttachUIAAHwnd"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_SnapShot(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SnapShot", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            var isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
            if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE) || (isForWEB))
            {
                return MARSWebDriver.GetInstance().WebSnapShot("SNAPSHOT",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult,
                    isForWEB
                    );
            }

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP || (isOk = Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.ISUsingMARSUI(dictPegProperties, ref strError)))
            {
                return MarsMARSUIHelper.MARSUI_Snapshot(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME,
                    strAttachInfo, stepObject.PEG_NAME ?? "", stepObject.OBJECT_HAPPY_NAME ?? "", ref strError, ref dealResult);
            }

            if (!string.IsNullOrEmpty(strParaMeter))
            {
                if (strParaMeter.Equals("DefaultAgent", StringComparison.OrdinalIgnoreCase))
                {
                    // call default agent's snapshot implementation
                    var ok = Mars.AutoTestingDriver.AISupport.AgentSupport.AgentKeywordDelegate.Snapshot(
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
                    return ok;
                }
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SNAPSHOT", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        private static bool MARSKEYWORD_OCRIMAGE(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_OCRIMAGE", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("OCRIMAGE", runOrdId, dictPegProperties, 
                dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            if (isOk)
            {
                if (!System.IO.File.Exists(dealResult.snapshotFilePath))
                {
                    strError = $"File exists|{dealResult.snapshotFilePath}";
                    dealResult.ErrorMessage = strError ;
                    dealResult.ResultMessage = "FAILED";
                    dealResult.ReturnedData = "FAILED";
                    isOk = false;
                    return isOk;
                }
                string strAdv = "", strStack = "";
                string ocrText = (new MARSOcrHelper()).ConvertBmpToText(dealResult.snapshotFilePath, ref isOk, ref strError, ref strAdv, ref strStack);
                if (isOk)
                {
                    //dealResult.ResultMessage = ;
                    dealResult.ReturnedData = ocrText;
                    return isOk;
                }
                Logger.Error("OCRIMAGE", strError);
                dealResult.ReturnedData = "FAILED";
                dealResult.ErrorMessage= strError ;
                dealResult.ResultMessage = "FAILED";
                return isOk;
            }
            return isOk;
        }
        

        private static bool MARSKEYWORD_SelectMenuItem(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SelectMenuItem", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = false;

            /// 2025-11-10添加 参数 POPUP_FROM_CUR_POS
            /// 
            int offsetX = 0, offsetY = 10;
            if (MarsMARSUIHelper.IsUIAAPopupMenuParameter(strParaMeter, ref offsetX, ref offsetY))
            {
                string strReturnedData = "", strAdv="", strStack="";
                isOk = MarsMARSUIHelper.Performance_SelectMenuItemPopup(strData, offsetX, offsetY, ref strReturnedData, ref strError, ref strAdv, ref strStack );
                dealResult.AckTime = DateTime.Now;
                dealResult.ReturnedData = strReturnedData;
                if (!isOk)
                {
                    dealResult.ResultMessage = "FAILED";
                    dealResult.ErrorMessage = strError;
                }
                else
                {
                    dealResult.ResultMessage = "SUCCESS";
                }
                return isOk;
            }

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                if (!isOk) return false;

                return MARSWebDriver.GetInstance().WebSelectMenuItem("SelectMenuItem",
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
            }

            /// MFC的应用，可能会有弹出菜单。这里要对弹出菜单进行处理
            if (isOk = MarsMARSUIHelper.ISUsingMARSUI(dictPegProperties, ref strError) || (appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP))
            {
                if (stepObject != null && !string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
                {
                    isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                }
                return MarsMARSUIHelper.MARSUI_SelectMenuItem(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME,
                    strAttachInfo, stepObject.PEG_NAME ?? "", stepObject.OBJECT_HAPPY_NAME ?? "", ref strError, ref dealResult);
            }

            if (stepObject == null || string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
            {
                if (stepObject == null)
                    stepObject = new B_V_OBJECT_SNAPSHOT();
                //stepObject.QUICK_ACCESS = "swfname:=_toolbarsDockAreaTop.*\r\nindex:=0";
                string strQUICK_ACCESS = "swfname:=_toolbarsDockAreaTop.*\r\nindex:=0";
                dictObjProperties = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(strQUICK_ACCESS, ref isOk);
                if (!isOk)
                    strError = "Format for object is wrong";
                stepObject.TYPE_NAME = "swfToolBar";
            }
            else
            {
                isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            }

            
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SELECTMENUITEM", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }
        /// <summary>
        /// 无障碍接口的支持：2025-9-14
        /// 对于swfTable,且对象中含有winTable，mfcTable，StandardTable， control_Id的，将都通过无障碍接口实现
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_SearchAndClick(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.
            MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SearchAndClick", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            ///添加对无障碍接口的支持
            ///
            if (isOk=MarsMSAASupport.MARSMSAAHelper.IsUsingMSAA(dictPegProperties, dictObjProperties, ref strError))
            {
                return MarsMSAASupport.MARSMSAAHelper.MARSStandard_SearchAndClick("SEARCHANDCLICK", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult);
            }

            var isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
            if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)||(isForWEB))
            {
                return MARSWebDriver.GetInstance().WebSearchAndClick("SearchAndClick", runOrdId, dictPegProperties,
                    dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME, strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "", ref strError, ref dealResult);
            }

            if (isAttachUIAAHwnd)
            {
                string strAdv = "", strStack = "";  
                var hwnd = MARSUIAppSideVariables.GetCurrentUIAPegHwnd(ref isOk, ref strError, ref strAdv, ref strStack);
                /// 给pegiwndow添加_FromUIAAHwnd属性
                dictPegProperties[MarsConstants.cnst_pegProperty_hwnd_fromuiaa] = hwnd.ToString();
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SEARCHANDCLICK", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            if (isAttachUIAAHwnd && isOk)
            {
                /// 因为控件处于一个ocx中，无法使用进程内的鼠标事件，需要进程外的方式来实现点击
                /// 
                string strAdv = "", strStack = "";
                isOk = ClickEventForNonInternalProcRequire(dealResult, ref strError, ref strAdv, ref strStack);
                if (!isOk)
                {
                    Logger.Error("MARSKEYWORD_SearchAndClick", $"{strError}\r\n{strAdv}\r\n{strStack}");
                }
            }
            return isOk;
        }

        private static bool ClickEventForNonInternalProcRequire(MARSDealResult dealResult, ref string strError, ref string strAdv, ref string strStack)
        {
            // dealResult.ReturnedData format: LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK;x:y
            // Example: "LEFT_CLICK;120:250"
            try
            {
                if (dealResult == null || string.IsNullOrEmpty(dealResult.ReturnedData))
                {
                    strError = "No returned data for click event.";
                    return false;
                }

                // Split by ';' to separate click type(s) and coordinates
                var parts = dealResult.ReturnedData.Split(';');
                if (parts.Length != 2)
                {
                    strError = $"ReturnedData format error: {dealResult.ReturnedData}";
                    return false;
                }

                // Parse click type(s)
                var clickTypes = parts[0].Split('|');
                // Parse coordinates
                var coords = parts[1].Split(':');
                if (coords.Length != 4 || !int.TryParse(coords[0], out int x) || !int.TryParse(coords[1], out int y))
                {
                    strError = $"Coordinate format error: {parts[1]}";
                    return false;
                }

                // For each click type, perform the corresponding mouse event
                foreach (var clickType in clickTypes)
                {
                    switch (clickType.Trim().ToUpperInvariant())
                    {
                        case "LEFT_CLICK":
                            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);
                            MarsWindowsAPIsExtend.LeftMouseClick(x, y);                            
                            break;
                        case "LEFT_DBL_CLICK":
                            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);
                            MarsWindowsAPIsExtend.LeftMouseDblClick(x, y);
                            break;
                        case "RIGHT_CLICK":
                            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);
                            MarsWindowsAPIsExtend.RightMouseClick(x, y);
                            break;
                        case "DRAW_RECT":
                            //int w, h;
                            //if (coords.Length != 4 || !int.TryParse(coords[2], out w) || !int.TryParse(coords[3], out h))
                            //{
                            //    strError = $"DRAW_RECT coordinate format error: {parts[1]}";
                            //    Logger.Error("ClickEventForNonInternalProcRequire", strError);
                            //    return false;
                            //}
                            //XorDrawing.DrawXorRectangleOnDeskTop(new MarsWindowsAPIs.RECT() { Left =x, Top = y, Bottom = y+h, Right= x+w}, ref strError);// x, y, w, h);
                            break;
                        default:
                            strAdv += $"Unknown click type: {clickType}; ";
                            strError = $"unSupported Mouse event|{clickType}";
                            strStack = Environment.StackTrace;
                            return false;                            
                    }
                }
                System.Threading.Thread.Sleep(500);
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in ClickEventForNonInternalProcRequire: {ex.Message}";
                strStack = ex.StackTrace;
                return false;
            }
        }


        private static bool MARSKEYWORD_SELECTLISTITEM(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SELECTLISTITEM", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                return MARSWebDriver.GetInstance().WebSelectListItem("FILLEDIT",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult
                    );
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SELECTLISTITEM", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        private static bool MARSKEYWORD_SearchAndUpdate(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SearchAndUpdate", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));


            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP 
                || (isOk = Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.ISUsingMARSUI(dictPegProperties, ref strError)))
            {
                return MarsMARSUIHelper.MARSUI_SearchAndUpdate(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME,
                    strAttachInfo, stepObject.PEG_NAME ?? "", stepObject.OBJECT_HAPPY_NAME ?? "", ref strError, ref dealResult);
            }


            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SEARCHANDUPDATE", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        private static bool MARSKEYWORD_SelectDropDown(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_SelectDropDown", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            var isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
            if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)||(isForWEB))
            {
                return MARSWebDriver.GetInstance().WebSelectDropDown("SelectDropDown",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult,
                    isForWEB
                    );
            }

           
            if (((isOk = MarsMSAASupport.MARSMSAAHelper.IsUsingMSAA(dictPegProperties, dictObjProperties, ref strError))||(appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP))
                && (!isAttachUIAAHwnd))
            {
                /// old version,使用了Iaccessible接口的方式
                /// 
                return MarsMARSUIHelper.MARSUI_SelectDropdown(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult);
            }

            if (isAttachUIAAHwnd)
            {
                string strAdv = "", strStack = "";
                var hwnd = MARSUIAppSideVariables.GetCurrentUIAPegHwnd(ref isOk, ref strError, ref strAdv, ref strStack);
                /// 给pegiwndow添加_FromUIAAHwnd属性
                dictPegProperties[MarsConstants.cnst_pegProperty_hwnd_fromuiaa] = hwnd.ToString();
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("SELECTDROPDOWN", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        /// <summary>
        /// 切换到root，不需要对象
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_SwitchToRoot(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities"
            , KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            ///注意，pegwindow需要与处理，暂时没实现
            /// 
            Logger.logBegin("MARSKEYWORD_SwitchToRoot", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            if (string.Compare(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS) == 0)
            {
                bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                if (!isOk) return false;

                if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
                {
                    return MARSWebDriver.GetInstance().WebPegWindow("Pegwindow",
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
                }

                isOk = InjectorMessageAgent.DealWithKeyword_Pegwindow(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo, stepObject.PEG_NAME, stepObject.OBJECT_HAPPY_NAME,
                ref strError, ref dealResult);
                return isOk;
            }
            else
            {
                Logger.Info("MARSKEYWORD_PegWindow", "fix peg mode");
                bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                if (!isOk) return false;

                if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
                {
                    return MARSWebDriver.GetInstance().WebPegWindow("Pegwindow",
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
                }

                isOk = InjectorMessageAgent.DealWithKeyword_Pegwindow(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME, stepObject.OBJECT_HAPPY_NAME,
                ref strError, ref dealResult);
                return isOk;
            }
        }

        private static bool MARSKEYWORD_PegWindow(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities"
            , KeywordExecuteCallBack dataSetBackCallBack = null , bool isAttachUIAAHwnd = false)
        {
            ///注意，pegwindow需要与处理，暂时没实现
            /// 
            Logger.logBegin("MARSKEYWORD_PegWindow", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            if (string.Compare(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS) == 0)
            {
                bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                if (!isOk) return false;

                /// 添加对UIA的支持
                /// 
                ///添加对MarsUI的支持 - 如果使用了catalog且value==MarsUI
                if (isOk = Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.ISUsingMARSUI(dictPegProperties, ref strError))
                {
                    return MarsMARSUIHelper.MARSUI_Pegwindow(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);

                    /// old way, using iAccessible interface, the current way is mixed ,but marjor is UIA
                    //return Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.MARSUI_Pegwindow(stepId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    //    stepObject.TYPE_NAME,
                    //    strAttachInfo,
                    //    stepObject.PEG_NAME ?? "",
                    //    stepObject.OBJECT_HAPPY_NAME ?? "",
                    //    ref strError,
                    //    ref dealResult);
                }

                ///添加对无障碍接口的支持 - 如果keyword是standard类别
                if (isOk = MarsMSAASupport.MARSMSAAHelper.IsUsingMSAA(dictPegProperties, dictObjProperties, ref strError))
                {
                    return MarsMSAASupport.MARSMSAAHelper.MARSStandard_Pegwindow(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
                }



                if (appTyp== Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
                {
                    return MARSWebDriver.GetInstance().WebPegWindow("Pegwindow", 
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
                }

                isOk = InjectorMessageAgent.DealWithKeyword_Pegwindow(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo, stepObject.PEG_NAME, stepObject.OBJECT_HAPPY_NAME,
                ref strError, ref dealResult);
                return isOk;
            }
            else
            {
                Logger.Info("MARSKEYWORD_PegWindow", "fix peg mode");
                bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                if (!isOk) return false;
                var isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
                if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)||(isForWEB))
                {
                    return MARSWebDriver.GetInstance().WebPegWindow("Pegwindow",
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
                }

                isOk = InjectorMessageAgent.DealWithKeyword_Pegwindow(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME, stepObject.OBJECT_HAPPY_NAME,
                ref strError, ref dealResult);
                return isOk;
            }
        }

        private static bool MARSKEYWORD_AutoCheckError(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfox,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", 
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_AutoCheckError", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = false;
            string strAttachInfo = "";
            if (stepObject == null || string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
            {
                if (stepObject == null)
                    stepObject = new B_V_OBJECT_SNAPSHOT();
                //stepObject.QUICK_ACCESS = "swfname:=_errorTree";
                string strQUICK_ACCESS = "swfname:=_errorTree\r\nindex:=0";
                dictObjProperties = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(strQUICK_ACCESS, ref isOk);
                if (!isOk)
                    strError = "Format for object is wrong";
                stepObject.TYPE_NAME = "SwfTreeView";
                strAttachInfo = "Default";
            }
            else
            {
                isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            }
            if (!isOk) return false;

            Logger.Info("MARSKEYWORD_AutoCheckError", string.Format("\tParent:[{0}] TargetObj:[{1}]",
                MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.Dic2String(dictPegProperties),
                MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.Dic2String(dictObjProperties)));

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("AutoCheckError",
                runOrdId,
                dictPegProperties,
                dictObjProperties,
                strParaMeter,
                strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            Logger.logEnd("MARSKEYWORD_AutoCheckError", $"autocheckError return {isOk}"+(string.IsNullOrEmpty(strError)?"":" -"+strError));
            return isOk;
        }

        private static bool MARSKEYWORD_CheckError(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfox,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities",
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_CheckError", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = false;
            string strAttachInfo = "";
            if (stepObject == null || string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
            {
                if (stepObject == null)
                    stepObject = new B_V_OBJECT_SNAPSHOT();
                //stepObject.QUICK_ACCESS = "swfname:=_errorTree";
                string strQUICK_ACCESS = "swfname:=_errorTree\r\nindex:=0";
                dictObjProperties = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(strQUICK_ACCESS, ref isOk);
                if (!isOk)
                    strError = "Format for object is wrong";
                stepObject.TYPE_NAME = "SwfTreeView";
                strAttachInfo = "Default";
            }
            else
            {
                isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            }
            if (!isOk) return false;

            Logger.Info("MARSKEYWORD_CheckError", string.Format("\tParent:[{0}] TargetObj:[{1}]",
                MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.Dic2String(dictPegProperties),
                MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.Dic2String(dictObjProperties)));

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CheckError", 
                runOrdId, 
                dictPegProperties, 
                dictObjProperties, 
                strParaMeter, 
                strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }

        private static bool MARSKEYWORD_createNewObjectsFromSpyer(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities", 
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_createNewObjectsFromSpyer", string.Format("Parameter:[{0}] Data:[{1}] attachInfo:[{2}]", strParaMeter, strData, strAttachInfo));
            //System.Web.Script.Serialization.JavaScriptSerializer javaScript = new System.Web.Script.Serialization.JavaScriptSerializer();

            return false; 
        }

        private static string GetPreFixOfParaForCapture(string strPara,ref bool isPrefixExists, ref string strNewPara)
        {
            return CaptureParaMgr.GetPreFixOfParameter(strPara, ref isPrefixExists, ref strNewPara);

            //if (string.IsNullOrEmpty(strPara))
            //{
            //    isPrefixExists = false;
            //    return null;
            //}
            //int iPos = strPara.IndexOf(CaptureParaMgr.cnst_toMem+";");
            //int iLen = (CaptureParaMgr.cnst_toMem + ";").Length;
            //if (iPos<0)
            //{
            //    iLen = CaptureParaMgr.cnst_toMem.Length;
            //    iPos = strPara.IndexOf(CaptureParaMgr.cnst_toMem);
            //    if (iPos<0)
            //    {
            //        isPrefixExists = false;
            //        return null;
            //    }
            //}
            //strNewPara = strPara.Substring(iPos + iLen);
            //isPrefixExists = true;
            //return CaptureParaMgr.cnst_toMem;
        }

        private static bool MARSKEYWORD_CaptureValue(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities", 
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_CaptureValue", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            bool isPrefixExists = false;
            string strNewPara = "";
            string strVarKeyIndex = GetPreFixOfParaForCapture(strData, ref isPrefixExists, ref strNewPara); // i.e alias
            if ((isPrefixExists)&&(!string.IsNullOrEmpty(strNewPara)))
            {
                strData = strNewPara;
            }
            try
            {

                if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
                {
                    return MARSWebDriver.GetInstance().WebCaptureValue("CAPTUREVALUE",
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult
                        );
                }

                if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP)
                    || (isOk = MarsMSAASupport.MARSMSAAHelper.IsUsingMSAA(dictPegProperties, dictObjProperties, ref strError)))
                {
                    return MarsMARSUIHelper.MARSUI_CaptureValue(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
                }

                isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CAPTUREVALUE", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult);

                return isOk;
            }
            finally
            {
                if (isOk && isPrefixExists)
                {
                    if (!CaptureParaMgr.globalMemoryData.ContainsKey(strVarKeyIndex))
                    {
                        // that should not happen, but for 
                        Logger.Error("MARSKEYWORD_CaptureValue", $"Can't find memory variable :[{strVarKeyIndex}]");
                    }
                    else
                    {
                        CaptureParaMgr.globalMemoryData[strVarKeyIndex].values = dealResult.ReturnedData;
                        Logger.Info("MARSKEYWORD_CaptureValue", $"Capture value to memory variables|{strVarKeyIndex}|values|{dealResult.ReturnedData}");
                    }
                }
                Logger.logEnd("MARSKEYWORD_CaptureValue");
            }
        }

        private static KeywordBatchCaptureInfo u_currentCaptureAndCmpBatchStatus = new KeywordBatchCaptureInfo();
        /// <summary>
        /// 2026-1-7 
        ///     添加无对象支持（因为API对象的抽取的数据放在MemVar中，因此，需转换Memvar到普通的UI的CaptureAndCompare中）
        ///     因此，这种情况是
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strApiRunTimeConfig"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <param name="isAttachUIAAHwnd"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_CaptureAndCompare(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities", 
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_CaptureAndCompare", string.Format("Parameter:[{0}] Data:[{1}] Name:[{2}:{3}] peg:[{4}].[{5}] ",
                strParaMeter, strData, stepObject.OBJECT_HAPPY_NAME, stepObject.OBJECT_ID, stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS));
            bool isOK = true;
            try
            {
                
                if (string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
                {
                    // no object case
                    Logger.Info("MARSKEYWORD_CaptureAndCompare", "No object case - try to get from memory variable");

                    if (!B_KEYWORD.ExceptForKeywordWithoutObj("CAPTUREANDCOMPARE", strParaMeter, ref strError))
                    {
                        dealResult.ResultMessage = "FAILED";
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ErrorMessage = strError;
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = "Contact Marsquis";
                        dealResult.ReturnedData = "";
                        Logger.Error("MARSKEYWORD_CaptureAndCompare", strError);
                        Logger.logEnd("MARSKEYWORD_CaptureAndCompare", "return false");
                        return false;
                    }

                    bool isPrefixExists = false;
                    string strNewPara = "", strPreFix="";
                    NonCaptureParaMgr paraMgr = new NonCaptureParaMgr();
                    isOK = paraMgr.dealWithPrefixPara(strParaMeter, ref strPreFix, ref strNewPara); // i.e alias
                    if (!isOK)
                    {
                        dealResult.ResultMessage = "FAILED";
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ErrorMessage = strError = $"Parameter |{strParaMeter}| is not right, the right format is 'FromMem:Variable_name'";
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = "Contact Marsquis";
                        dealResult.ReturnedData = "";
                        Logger.Error("MARSKEYWORD_CaptureAndCompare", strError);
                        Logger.logEnd("MARSKEYWORD_CaptureAndCompare", "return false");
                        return false;
                    }
                    string strValueData = "";
                    isOK = CaptureParaMgr.GetVariableByIdx(strNewPara, ref strValueData, ref strError);
                    if (!isOK)
                    {
                        dealResult.ResultMessage = $"FAILED,{strError}";
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ErrorMessage = strError ;
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.Advice = "Please correct test case with right paramter and ensure that objects has been created.";
                        dealResult.ReturnedData = "";
                        Logger.Error("MARSKEYWORD_CaptureAndCompare", strError);
                        Logger.logEnd("MARSKEYWORD_CaptureAndCompare", "return false");
                        return false;
                    }
                    dealResult.ResultMessage = "SUCCESS";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ReturnedData = strValueData;
                    dealResult.ActualInputData = strParaMeter;

                    return true;
                }

                Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
                Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
                isOK = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, 
                    stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                if (!isOK)
                {
                    Logger.Error("MARSKEYWORD_CaptureAndCompare",$"AlystObjectQuickAccessToPegAndObj failed with error [{strError}] - peg quick access:[{stepObject.PEG_QUICK_ACCESS}] object is:[{stepObject.QUICK_ACCESS}]" );
                    return false;
                }
                var isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
                Logger.Info("MARSKEYWORD_CaptureAndCompare", $"isforWEB|{isForWEB}");
                if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE) || (isForWEB))
                {
                    return MARSWebDriver.GetInstance().WebCaptureAndCompare("CAPTUREANDCOMPARE",
                        runOrdId,
                        dictPegProperties,
                        dictObjProperties,
                        strParaMeter,
                        strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult
                        );
                }

                if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP)
                    || (isOK = MarsMSAASupport.MARSMSAAHelper.IsUsingMSAA(dictPegProperties, dictObjProperties, ref strError)))
                {
                    return MarsMARSUIHelper.MARSUI_CaptureValue(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                        stepObject.TYPE_NAME,
                        strAttachInfo,
                        stepObject.PEG_NAME ?? "",
                        stepObject.OBJECT_HAPPY_NAME ?? "",
                        ref strError,
                        ref dealResult);
                }
                /// 判断是不是batch模式
                /// 
                bool isInitBatch = false;
                if (!u_currentCaptureAndCmpBatchStatus.isBatchMode)
                {                    
                    if (u_currentCaptureAndCmpBatchStatus.isBatchModeByPara(strParaMeter))
                    {
                        ///then, it is para
                        ///
                        u_currentCaptureAndCmpBatchStatus.pegName = stepObject.TYPE_NAME;
                        u_currentCaptureAndCmpBatchStatus.objName = stepObject.OBJECT_HAPPY_NAME;
                        isInitBatch = true;
                    }
                    // normal mode
                }
                else if (u_currentCaptureAndCmpBatchStatus.isBatchEnd(strParaMeter))
                {
                    u_currentCaptureAndCmpBatchStatus.isBatchMode = false;
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ReturnedData = "Batch End Success";
                    return true;
                    
                }else
                {
                    // should get data from datatable
                    // 说明是取数阶段，直接从datatable中获得
                    // 只处理allrows:ABC
                    MARSClientCaptureAndCompare capPara = new MARSClientCaptureAndCompare(strParaMeter);
                    if (capPara.isAllRows)
                    {
                        // 获得数据
                        string strStack = "";
                        string strDataFromDataTable = capPara.GetDataFromDataTable(u_currentCaptureAndCmpBatchStatus.dataTableFromKeyword,
                            ref isOK, ref strError, ref strStack);
                        if (isOK)
                        {
                            dealResult.ResultMessage = "OK";
                            dealResult.AckTime = DateTime.Now;
                            dealResult.ReturnedData = strDataFromDataTable;
                            return true;
                        }
                    }
                }
                                
                isOK = InjectorMessageAgent.DealWithKeyword_GUIOp("CAPTUREANDCOMPARE", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult);
                if (dealResult != null) {
                    Logger.Info("\t", $"5 returned data:{dealResult.ReturnedData}");
                    
                }
                if (!isOK)
                {
                    u_currentCaptureAndCmpBatchStatus.isBatchMode = false;
                }
                else
                {                    
                    if (isInitBatch)
                    {
                        // 返回的数据是csv，需要存到memory中
                        // 每个cell的数据不应该有\r\n
                        u_currentCaptureAndCmpBatchStatus.returnedFromKeyword = dealResult.ReturnedData;
                        string strStack = "";
                        bool isCSVOK = u_currentCaptureAndCmpBatchStatus.convertToDT(ref strError, ref strStack);
                        if (!isCSVOK) { isOK = false; }
                    }
                    
                }
                return isOK;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_CaptureAndCompare");
            }
        }

        private static bool MARSKEYWORD_CaptureAndCompareByKey(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_CaptureAndCompareByKey", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CAPTUREANDCOMPAREBYKEY", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }


        private static bool MARSKEYWORD_CloseWindow(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities",
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_CloseWindow", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            if (string.IsNullOrEmpty(strData) && string.IsNullOrEmpty(strParaMeter))
            {
                strData = "byPos:-18:16";
            }
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CLOSEWINDOW", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }


        private static bool MARSKEYWORD_ClickRadioButton(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ClickRadioButton", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CLICKRADIOBUTTON", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }
        private static bool MARSKEYWORD_ClickAt(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities", 
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ClickAt", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();

            /// 点击偏移量是相对于当前鼠标位置的
            if (InputHelper.cnstClickAtOffset_fromCurpos.Equals(strParaMeter, StringComparison.OrdinalIgnoreCase))
            {
                // special handle for click at from current position
                return InputHelper.DoClickAtOffset_fromCurpos("ClickAt", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult);
            }

            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) {
                strError = strError ?? "Object Indentifiers settings are wrong";
                Logger.Error("MARSKEYWORD_ClickAt", strError);
                
                return false;
            }
            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                return MARSWebDriver.GetInstance().WebClickAt("CLICKAT", runOrdId, dictPegProperties,
                    dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME, strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "", ref strError, ref dealResult);
            }
            var isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
            if (isForWEB)
            {
                return MARSWebDriver.GetInstance().WebClickAt("CLICKAT",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult,
                    true
                    );
            }
            

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CLICKAT", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }


        private static bool MARSKEYWORD_ClickButton(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ClickButton", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            string strKey = "";
            // Check if dictObjProperties contains 'swfiamgefile' key (case insensitive)
            if (ContainsSwfImageFileKey(dictObjProperties, ref strKey))
            {
                Logger.Info("MARSKEYWORD_ClickButton", "Found swfiamgefile key in object properties, processing image object");
                string strFileWithPath = "";
                isOk = ProcessSwfImageFile(strDBIdx, stepObject, ref strError, ref strFileWithPath);
                if (!isOk) return false;
                
                MarsImageKeywordOp imageKeywordOp = new MarsImageKeywordOp();
                dictObjProperties[strKey] = strFileWithPath;

                isOk = imageKeywordOp.ClickButton(dictPegProperties, dictObjProperties, strParaMeter, strData,
                    "", // COMMENT
                    ref strError, ref dealResult);
                if (!isOk) return false;
                return true;
            }

            /// 这里有两种情况，对于standard mfc app，使用msaa接口进行处理，但是有些mfc app中调用了afx的控件加载了wpf和dotnet的控件
            /// 也就是isAttachUIAAHwnd，需要使用.net framework的模式来处理
            if (((appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP)
                ||(isOk = MarsMSAASupport.MARSMSAAHelper.IsUsingMSAA(dictPegProperties, dictObjProperties, ref strError)))
                && (!isAttachUIAAHwnd))
            {
                /// 这里
                return MarsMARSUIHelper.MARSUI_ClickButton(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult);
            }

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                return MARSWebDriver.GetInstance().WebClickButton("CLICKBUTTON", runOrdId, dictPegProperties,
                    dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME, strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME?? "", ref strError, ref dealResult);
            }
            var isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
            if (isForWEB)
            {
                return MARSWebDriver.GetInstance().WebClickButton("CLICKBUTTON",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult,
                    true
                    );
            }

            if (isAttachUIAAHwnd)
            {
                string strAdv = "", strStack = "";
                var hwnd = MARSUIAppSideVariables.GetCurrentUIAPegHwnd(ref isOk, ref strError, ref strAdv, ref strStack);
                /// 给pegiwndow添加_FromUIAAHwnd属性
                dictPegProperties[MarsConstants.cnst_pegProperty_hwnd_fromuiaa] = hwnd.ToString();
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CLICKBUTTON", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }

        private static bool MARSKEYWORD_MaximizeWindow(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_MaximizeWindow", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                return MARSWebDriver.GetInstance().WebMaximizeWindow("MaximizedWindow",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult
                    );
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("MAXIMIZEWINDOW", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        /// <summary>
        /// 点击toolbar上的icon
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_ClickMenuIcon(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ClickMenuIcon", string.Format("Parameters:[{0}] Data:[{1}]", strParaMeter, strData));
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = false;
            if (stepObject == null || string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
            {
                if (stepObject == null)
                    stepObject = new B_V_OBJECT_SNAPSHOT();
                //stepObject.QUICK_ACCESS = "swfname:=_toolbarsDockAreaTop.*\r\nindex:=0";
                string strQUICK_ACCESS = "swfname:=_toolbarsDockAreaTop.*\r\nindex:=0";
                dictObjProperties = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(strQUICK_ACCESS, ref isOk);
                if (!isOk)
                    strError = "Format for object is wrong";
                stepObject.TYPE_NAME = "swfToolBar";
            }
            else
            {
                isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            }
            if (!isOk) return false;

            if (isOk = Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.ISUsingMARSUI(dictPegProperties, ref strError) || (MarsMARSUIHelper.ISUsingMARSUI(dictObjProperties, ref strError)))
            {
                return MarsMARSUIHelper.MARSUI_ClickMenuIcon(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME,
                    strAttachInfo, stepObject.PEG_NAME ?? "", stepObject.OBJECT_HAPPY_NAME ?? "", ref strError, ref dealResult);
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CLICKMENUICON", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }

        private static bool MARSKEYWORD_ScrollUp(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP
            , string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            return MARSKEYWORD_Scroll(runOrdId, strParaMeter, strData, strApiRunTimeConfig,
                stepObject,
                ref strError,
                ref dealResult,
                "SCROLLUP");
        }
        private static bool MARSKEYWORD_ScrollDown(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            return MARSKEYWORD_Scroll(runOrdId, strParaMeter, strData, strApiRunTimeConfig,
                stepObject,
                ref strError,
                ref dealResult,
                "SCROLLDOWN");
        }
        private static bool MARSKEYWORD_ScrollLeft(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            return MARSKEYWORD_Scroll(runOrdId, strParaMeter, strData, strApiRunTimeConfig,
                stepObject,
                ref strError,
                ref dealResult,
                "SCROLLLEFT");
        }
        private static bool MARSKEYWORD_ScrollRight(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities", 
            KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            return MARSKEYWORD_Scroll(runOrdId, strParaMeter, strData, strApiRunTimeConfig,
                stepObject,
                ref strError,
                ref dealResult,
                "SCROLLRIGHT");
        }

        private static bool MARSKEYWORD_ScrollWindow(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, 
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
                Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
                bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                if (!isOk) return false;

                return MARSWebDriver.GetInstance().WebScrollWindow("SCROLLWINDOW",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult
                    );
            }
            else
            return MARSKEYWORD_Scroll(runOrdId, strParaMeter, strData, strApiRunTimeConfig,
                stepObject,
                ref strError,
                ref dealResult,
                "SCROLLWINDOW");
        }


        private static bool MARSKEYWORD_Scroll(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            ref string strError,
            ref MARSDealResult dealResult,
            string strScorllCommand)
        {
            Logger.logBegin("MARSKEYWORD_Scroll", string.Format("{2} Parameter:[{0}] Data:[{1}]",
                strParaMeter,
                strData,
                strScorllCommand));
            try
            {
                Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
                Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
                bool isOk = false;
                if (stepObject == null || string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
                {
                    if (stepObject == null)
                        stepObject = new B_V_OBJECT_SNAPSHOT();
                    if (dictPegProperties.Keys.Count == 0)
                    {
                        dictPegProperties.Add("SwfName", "Desktop");
                        dictPegProperties.Add("Text", "((Misys ){0,1}(FusionCapital ){0,1}Summit( FT){0,1})(?!(.*Login)).*");
                        dictPegProperties.Add("index", "0");
                    }

                    //stepObject.QUICK_ACCESS = "swfname:=PaneLayout\r\nindex:=0";
                    string strQUICK_ACCESS = "swfname:=PaneLayout\r\nindex:=0";
                    dictObjProperties = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(strQUICK_ACCESS, ref isOk);

                    stepObject.TYPE_NAME = "SWFWINDOW";
                }
                else
                {
                    isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
                }
                if (!isOk) return false;

                isOk = InjectorMessageAgent.DealWithKeyword_GUIOp(strScorllCommand, runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                    stepObject.TYPE_NAME,
                    "",
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult);

                return isOk;
            }
            finally
            {
                Logger.logEnd("MARSKEYWORD_Scroll");
            }

        }

        public static bool _StartObjectSpyAgent(string strData)
        {
            string strError = "";
            MARSDealResult dealResult = new MARSDealResult();
            return MARSKEYWORD_StartObjectSpy(0,"", strData,"", null, "", ref strError, ref dealResult);
        }
        private static bool MARSKEYWORD_StartObjectSpy(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_StartObjectSpy", $"parameter:{strParaMeter} strData:{strData}");

            MARSKeywordWebHelpers.SetCurrentWebStepMode(strData??strParaMeter);
            if (MARSKeywordWebHelpers.currentWebStepMode== MARSStep_WebConnectionMode._noSet)
            {
                strError = $"parameter or data should be |{MARSKeywordWebHelpers.cnst_mode_byMARSAgent}| or |{MARSKeywordWebHelpers.cnst_mode_bySelenium}|";
                dealResult = new MARSDealResult()
                {
                    ReturnedData = strAttachInfo,
                    ResultMessage = "FAILED",
                    ErrorMessage = strError,                    
                    Advice = "Please ensure test steps setting right",                    
                };
                return false;                
            }

            bool isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("_STARTOBJECTSPY", runOrdId, null, null, strParaMeter, strData,
                "",
                strAttachInfo,
                "",
                "",
                ref strError,
                ref dealResult);
            Logger.logEnd("MARSKEYWORD_StartObjectSpy");
            return isOk;
        }
        /// <summary>
        /// 链接到web页面，有些应用中包含webbrowser或者webview，通过object quickaccess去找。
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_ConnectToWebView(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ConnectToWebView", $"parameter:{strParaMeter} strData:{strData}");

            MARSKeywordWebHelpers.SetCurrentWebStepMode(strData ?? strParaMeter);
            dealResult = new()
            {
                AskTime = DateTime.Now,
                ActualInputData = strAttachInfo,
                Advice =""
            };
            if (MARSKeywordWebHelpers.currentWebStepMode == MARSStep_WebConnectionMode._noSet)
            {   
                dealResult.AckTime = DateTime.Now;
                dealResult.Advice = $"Please ensure that only {MARSKeywordWebHelpers.cnst_mode_bySelenium}|{MARSKeywordWebHelpers.cnst_mode_byMARSAgent} is supported.";
                strError = dealResult.ErrorMessage = $"The step parameter or data setting is wrong|{dealResult.Advice}";
                Logger.Error("MARSKEYWORD_ConnectToWebView",strError);
                dealResult.ResultMessage = "FAILED";
                return false;
            }
            /// 如何设置成功，那么，通过webhelper connect to webview
            /// 
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = false;
            if (stepObject == null || string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
            {
                dealResult.Advice = $"Please ensure that the webview belongs to an parent objects";
                strError = dealResult.ErrorMessage = $"No peg window is set. |{dealResult.Advice}";
                Logger.Error("MARSKEYWORD_ConnectToWebView", strError);
                dealResult.ResultMessage = "FAILED";
                return false;
            }
            else
            {
                isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            }
            if (!isOk) return false;
            string strAdv = "";
            isOk = MARSKeywordWebHelpers.ConnectToWebView(dictPegProperties, dictObjProperties, MARSKeywordWebHelpers.currentWebStepMode, MARSKeywordWebHelpers.currentMarsSeleniumWarp.remote_debugger_port,
                ref strError, ref strAdv);
            if (!isOk)
            {
                dealResult.Advice = strAdv;
                dealResult.AckTime = DateTime.Now;
                dealResult.ResultMessage = "FAILED";
                dealResult.ErrorMessage = strError;
                return false;
            }

            dealResult.AckTime = DateTime.Now;
            dealResult.ResultMessage = "SUCCESS";
            dealResult.ErrorMessage  = "SUCCESS";
            Logger.logEnd("MARSKEYWORD_StartObjectSpy");
            return isOk;
        }
        

        private static bool MARSKEYWORD_Dismiss(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject, string strAttachInfo,
            ref string strError, ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_Dismiss", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            bool isOk = false;
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();

            MarsKeywordParameter disMissPara = new MarsKeywordParameter(strParaMeter);
            IntPtr currentPId= IntPtr.Zero;

            var inputPara = disMissPara.GetSpecialParaExists(MarsKeywordParameter.cnst_para_idx_Input);
            if (inputPara != null)
            {
                MarsKeywordParaNestedItem inputNestedItm = inputPara as MarsKeywordParaNestedItem;
                if (inputNestedItm == null)
                {
                    Logger.Error("MARSKEYWORD_Dismiss", strError = "Please check Dismiss keyword's 'Input' Clause of parameter");
                    dealResult.AckTime = new System.DateTime();
                    dealResult.ErrorMessage = strError;
                    dealResult.Advice = strError;
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.ResultMessage = "FAILED";
                    return isOk = false;
                }
                foreach(var inputItm in inputNestedItm.nestedItems)
                {
                    if (inputItm.Equals(default(KeyValuePair<string, string>)))
                    {
                        continue;
                    }
                    if ((!string.IsNullOrEmpty(inputItm.Key))&&(string.Compare(MarsKeywordParameter.cnst_para_idx_PID, inputItm.Key, true) == 0))
                    {
                        string pidVariableName = inputItm.Value;
                        var pidFromMemoryTable = MarsMemoryVaiableTable.GetMarsVariable(pidVariableName);
                        currentPId = pidFromMemoryTable is IntPtr?(IntPtr)pidFromMemoryTable : IntPtr.Zero;
                    }
                }
                string inputVariableName = inputPara.value;

                MarsMemoryVaiableTable.GetMarsVariable(inputVariableName);
            }
            var windowByClassName = disMissPara.GetSpecialParaExists(MarsKeywordParameter.cnst_para_idx_WindowByClass);
            if (windowByClassName != null)
            {
                #region 通过进程ID，然后依据windows窗口的类进行处理相关的窗口
                /// 通过进程的id，
                /// 寻找子窗口中具有该类的
                /// 寻找子窗口中具有ok，yes等button的
                /// 
                #endregion
            }
            if (!string.IsNullOrEmpty(strParaMeter))
            {
                if (string.Compare(strParaMeter, "Mars_windowsByClass", true) == 0)
                {
                    //可能是系统的弹出窗口
                    var lstWindowWithClassName = MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.GetWindows();
                    var lstTargetWins = lstWindowWithClassName.Where(p => (!string.IsNullOrEmpty(p.Value)) && (string.Compare(strData, p.Value, true) == 0))
                            .ToList();
                    if (lstTargetWins.Count != 1)
                    {
                        strError = $"find [{lstTargetWins.Count}] window(s) for {strData}";
                        Logger.Error("MARSKEYWORD_Dismiss", strError);
                        isOk = false;
                        dealResult.AckTime = new System.DateTime();
                        dealResult.ErrorMessage = strError;
                        dealResult.ActualInputData = "";
                        //dealResult.
                        return true;
                    }
                    //var condition = new PropertyCondition(AutomationElementIdentifiers.ClassNameProperty,
                    //        "Intermediate D3D Window");
                    //var element = AutomationElement.RootElement.FindAll(
                    //TreeScope.Children, condition);
                }
            }

            if (stepObject == null || string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
            {
                if (stepObject == null)
                    stepObject = new B_V_OBJECT_SNAPSHOT();
                dictPegProperties.Add("OBJECT CLASS", "#32770");
                dictPegProperties.Add("index", "0");
                string QUICK_ACCESS = "text:=OK|Yes|Ok|YES\r\nindex:=0";
                if (!string.IsNullOrEmpty(strData))
                {
                    if (!strData.Contains(":="))
                    {
                        QUICK_ACCESS = string.Format("text:={0}", strData);
                    }
                    else
                    {
                        QUICK_ACCESS = strData;
                    }
                }

                dictObjProperties = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(QUICK_ACCESS, ref isOk);
                if (!isOk)
                    strError = "Format for object is wrong";
                stepObject.TYPE_NAME = "SWFBUTTON";
            }
            else
            {
                isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            }
            if (!isOk) return false;

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("DISMISS", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }

        private static bool MARSKEYWORD_LaunchApplication(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult, Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_LaunchApplication", string.Format("Parameters:[{0}] Data:[{1}]", strParaMeter, strData));
            ///该keyword存在两种情况，有操作对象 或者没有，如果没有，则采用默认模式
            /// 
            bool isOk = false;
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            if (stepObject == null || string.IsNullOrEmpty(stepObject.QUICK_ACCESS))
            {
                if (stepObject == null)
                    stepObject = new B_V_OBJECT_SNAPSHOT();
                //stepObject.QUICK_ACCESS = "swfname:=_toolbarsDockAreaTop.*\r\nindex:=0";
                string strQUICK_ACCESS = "swfname:=_toolbarsDockAreaTop.*\r\nindex:=0";
                dictObjProperties = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(strQUICK_ACCESS, ref isOk);
                if (!isOk)
                    strError = "Format for object is wrong";
                stepObject.TYPE_NAME = "swfToolBar";
            }
            else
            {
                isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties,
                    ref dictObjProperties, ref strError);
            }
            if (!isOk) return false;
            System.Threading.Thread.Sleep(1000);
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("LAUNCHAPPLICATION", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }

        private static bool MARSKEYWORD_ClickPopupMenuItem(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_ClickPopupMenuItem", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            ///算法：1，分析pegwindows的identification结构和目标对象的结构
            /// 2，将定位数据传给message center并且等待
            /// 3，如果
            /// 
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            strError = "";
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("CLICKPOPUPMENUITEM", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }

        private static bool MARSKEYWORD_InsertRow(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult
            , Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities"
            , KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_InsertRow", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            ///算法：1，分析pegwindows的identification结构和目标对象的结构
            /// 2，将定位数据传给message center并且等待
            /// 3，如果
            /// 
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            strError = "";
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("INSERTROW", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }


        private static bool MARSKEYWORD_FillTable(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult
            , Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities"
            , KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_FillTable", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            ///算法：1，分析pegwindows的identification结构和目标对象的结构
            /// 2，将定位数据传给message center并且等待
            /// 3，如果
            /// 
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            strError = "";
            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("FILLTABLE", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);

            return isOk;
        }

        private static bool MARSKEYWORD_AddDays(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult,
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
            string strDBIdx = "marsentities", KeywordExecuteCallBack dataSetBackCallBack = null,bool isAttachUIAAHwnd=false 
            )
        {
            Logger.logBegin("MARSKEYWORD_AddDays", string.Format("Parameter:[{0}] Data:[{1}]", strParaMeter, strData));
            ///算法：1，分析pegwindows的identification结构和目标对象的结构
            /// 2，将定位数据传给message center并且等待
            /// 3，如果
            /// 
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("ADDDAYS", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

        private static bool DealwithPrefixOfParameter(string strParameter, ref string strParaNoPrefix, ref string varIdx)
        {
            strParaNoPrefix = strParameter;
            NonCaptureParaMgr nonCapMgr = new NonCaptureParaMgr();

            return nonCapMgr.dealWithPrefixPara(strParameter, ref strParaNoPrefix, ref varIdx);
        }

        private static bool MARSKEYWORD_WaitMFACode(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult
            , Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, string strDBIdx = "marsentities"
            , KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_WaitMFACode", string.Format("Parameter:[{0}] Data:[{1}] appType:[{2}]",
                strParaMeter, strData, appTyp));
            ///
            /// 
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;
            string strNewpara = "", strVarIdx = "", sourceData = strData;

            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                return MARSWebDriver.GetInstance().WebWaitMFACode("WAITMFACODE",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult
                    );
            }
            isOk = false;
            strError = "The keyword WAITMFACODE is ONLY available for WEB application";
            return false;
        }

        private static bool IsObjectForWebApplication(Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties)
        {
            if (dictObjProperties == null) return false;
            bool isExistWebType = dictObjProperties.Keys.Any(p => MARSWebDriver.WebObject_types_index.Any(x => x.Equals(p, StringComparison.OrdinalIgnoreCase))); 
            if (!isExistWebType) {
                isExistWebType = dictPegProperties.Keys.Any(p => MARSWebDriver.WebObject_types_index.Any(x => x.Equals(p, StringComparison.OrdinalIgnoreCase)));
            }
            return isExistWebType;
        }

        /// <summary>
        /// Checks if dictObjProperties contains 'swfiamgefile' key (case insensitive)
        /// </summary>
        /// <param name="dictObjProperties">Dictionary containing object properties</param>
        /// <returns>True if swfiamgefile key is found</returns>
        private static bool ContainsSwfImageFileKey(Dictionary<string, string> dictObjProperties, ref string imageKey)
        {
            if (dictObjProperties == null)
                return false;

            // Check for 'swfiamgefile' key (case insensitive)
            var swfImageFileKey = dictObjProperties.Keys.FirstOrDefault(k => 
                string.Equals(k, "swfimagefile", StringComparison.OrdinalIgnoreCase));
            imageKey = swfImageFileKey;
            return swfImageFileKey != null;
        }

        /// <summary>
        /// Processes the swfiamgefile key by creating MARSImageObjectHelper and calling REST API
        /// </summary>
        /// <param name="stepObject">Step object containing OBJECT_ID and other properties</param>
        /// <returns>True if image processing was successful</returns>
        private static bool ProcessSwfImageFile(string strDBIdx, B_V_OBJECT_SNAPSHOT stepObject,
            ref string strError,
            ref string strFielWithPath)
        {
            if (stepObject == null)
            {
                strError = "No test object (stepObject == null).";
                return false;
            }
            try
            {
                Logger.Info("ProcessSwfImageFile", "Processing swfiamgefile key");
                if (stepObject.APPLICATION_ID == null)
                {
                    Logger.Error("ProcessSwfImageFile", strError = "No application id is set");
                    return false;
                }
                // Create MARSImageObjectHelper instance
                using (var imageHelper = new MARSImageObjectHelper(new MarsRESTfulApiClient(strDBIdx).webURLPreFix))
                {
                    // Get OBJECT_ID from stepObject
                    long objectId = stepObject.OBJECT_ID;
                    string objectName = stepObject.OBJECT_HAPPY_NAME ?? "";
                    
                    // Get application ID - try to get from stepObject, otherwise use a default
                    // In a real implementation, this should be passed from the calling context
                    long applicationId = stepObject.APPLICATION_ID.Value; // Default application ID
                    
                    // Try to get application ID from stepObject if available
                    // Note: This assumes APPLICATION_ID property exists in B_V_OBJECT_SNAPSHOT
                    // If not available, the calling method should pass the application ID as a parameter
                    
                    Logger.Info("ProcessSwfImageFile", $"Calling GetObjectImagePath with objectId: {objectId}, objectName: {objectName}, applicationId: {applicationId}");
                    //string strFielWithPath = "";
                    // Call REST API
                    bool isOk = imageHelper.GetObjectImagePath(strDBIdx, objectId, objectName, applicationId, ref strFielWithPath, ref strError);
                    
                    if (isOk)
                    {
                        Logger.Info("ProcessSwfImageFile", $"Successfully retrieved image path:{ strFielWithPath}");
                        return true;
                    }
                    else
                    {
                        Logger.Error("ProcessSwfImageFile", $"Failed to get image path: {strError}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ProcessSwfImageFile", $"Exception occurred while processing swfiamgefile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断是否存在图形变量，如果存在，先从服务器获得文件，存在本地 /imageObject/object_id.png，然后将文件名称添加到swfimageFile后面，
        /// 然后获得整个屏幕的截图，然后在截图中查找图形
        /// </summary>
        /// <param name="runOrdId"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="stepObject"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="appTyp"></param>
        /// <param name="strDBIdx"></param>
        /// <param name="dataSetBackCallBack"></param>
        /// <returns></returns>
        private static bool MARSKEYWORD_FillEdit(long runOrdId, string strParaMeter, string strData, string strApiRunTimeConfig,
            B_V_OBJECT_SNAPSHOT stepObject,
            string strAttachInfo,
            ref string strError,
            ref MARSDealResult dealResult
            , Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP, 
              string strDBIdx = "marsentities"
            , KeywordExecuteCallBack dataSetBackCallBack = null, bool isAttachUIAAHwnd = false)
        {
            Logger.logBegin("MARSKEYWORD_FillEdit", string.Format("Parameter:[{0}] Data:[{1}] appType:[{2}]", 
                strParaMeter, strData, appTyp));
            ///算法：1，分析pegwindows的identification结构和目标对象的结构
            /// 2，将定位数据传给message center并且等待
            /// 3，如果
            /// 
            Dictionary<string, string> dictPegProperties = new Dictionary<string, string>();
            Dictionary<string, string> dictObjProperties = new Dictionary<string, string>();
            bool isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(stepObject.PEG_QUICK_ACCESS, stepObject.QUICK_ACCESS, ref dictPegProperties, ref dictObjProperties, ref strError);
            if (!isOk) return false;

            string  strNewpara = "" ,strVarIdx ="", sourceData= strData;
            
            // Check if dictObjProperties contains 'swfiamgefile' key (case insensitive)
            if (ContainsSwfImageFileKey(dictObjProperties, ref strVarIdx))
            {
                Logger.Info("MARSKEYWORD_FillEdit", "Found swfiamgefile key in object properties, processing image object");
                string strFileWithPath = "";
                isOk = ProcessSwfImageFile(strDBIdx, stepObject, ref strError, ref strFileWithPath);
                if (!isOk) return false;
                /// strVarIdx 就是key
                MarsImageKeywordOp imageKeywordOp = new MarsImageKeywordOp();
                dictObjProperties[strVarIdx] = strFileWithPath;

                isOk = imageKeywordOp.FillEdit(dictPegProperties, dictObjProperties, strParaMeter, strData,
                    "", // COMMENT
                    ref strError, ref dealResult);
                if (!isOk) return false;
                return true;
            }
            
            bool isVarPrefixExist = false;
            if (isVarPrefixExist = DealwithPrefixOfParameter(strData, ref strNewpara, ref strVarIdx))
            {
                strParaMeter = strNewpara;
                if (string.IsNullOrEmpty(strVarIdx))
                {
                    Logger.Error("MARSKEYWORD_FillEdit", strError = $"Memory variable is required, but parameter [{sourceData}] setting is wrong.");
                    return false;                    
                }
                // get var idx from global
                string strNewData = "";
                if (CaptureParaMgr.GetVariableByIdx(strVarIdx, ref strNewData, ref strError))
                {
                    Logger.Info("");
                    strData = strNewData;
                }
                else
                {
                    Logger.Error("MARSKEYWORD_FillEdit", strError = $"Can't find [{strVarIdx}] from Memory variable table");
                    return false;
                }
            }

            if (((appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP) 
                || (isOk = Mars.Inter.MQCenter.MSAASupport.MarsMARSUIHelper.ISUsingMARSUI(dictPegProperties, ref strError)))
                && (!isAttachUIAAHwnd))
            {
                return MarsMARSUIHelper.MARSUI_FillEdit(runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData, stepObject.TYPE_NAME,
                    strAttachInfo, stepObject.PEG_NAME ?? "", stepObject.OBJECT_HAPPY_NAME ?? "", ref strError, ref dealResult);
            }

            bool isForWEB = false;            
            if (appTyp == Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
            {
                return MARSWebDriver.GetInstance().WebFillEdit("FILLEDIT",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult
                    );
            }
            isForWEB = IsObjectForWebApplication(dictPegProperties, dictObjProperties);
            if (isForWEB) 
            {
                return MARSWebDriver.GetInstance().WebFillEdit("FILLEDIT",
                    runOrdId,
                    dictPegProperties,
                    dictObjProperties,
                    strParaMeter,
                    strData,
                    stepObject.TYPE_NAME,
                    strAttachInfo,
                    stepObject.PEG_NAME ?? "",
                    stepObject.OBJECT_HAPPY_NAME ?? "",
                    ref strError,
                    ref dealResult,
                    true
                    );
            }

            if (isAttachUIAAHwnd)
            {
                string strAdv = "", strStack = "";
                var hwnd = MARSUIAppSideVariables.GetCurrentUIAPegHwnd(ref isOk, ref strError, ref strAdv, ref strStack);
                /// 给pegiwndow添加_FromUIAAHwnd属性
                dictPegProperties[MarsConstants.cnst_pegProperty_hwnd_fromuiaa] = hwnd.ToString();
            }

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp("FILLEDIT", runOrdId, dictPegProperties, dictObjProperties, strParaMeter, strData,
                stepObject.TYPE_NAME,
                strAttachInfo,
                stepObject.PEG_NAME ?? "",
                stepObject.OBJECT_HAPPY_NAME ?? "",
                ref strError,
                ref dealResult);
            return isOk;
        }

    }
}
