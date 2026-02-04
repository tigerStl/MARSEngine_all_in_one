#if !MESSAGESVC_FROM_GUI
extern alias clientWCF;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
#else
using Route2NSEx.src.Marquis.systemUtil;
#endif


using System;
using System.Linq;
using Mars.message.Business;
using Mars.message.Dto;
using Mars.message.DataLayer;
using System.Configuration;
using Mars.AutoTestingDriver.ErrorMessage;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.AutoTestingDriver.SystemUtil;
using System.Drawing;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.baseInterfaceAndClass;
using Mars.AutoTestingDriver.MarsUISupport;
using Mars.AutoTestingDriver.ApiIntegratedHelper;
#if _EngineDriver
using MarsEnginer.windowsWrapper.SystemUtil;
#else
#endif

using System.Collections.Generic;

namespace Mars.AutoTestingDriver.ExecuteTestcase.keywordOp
{
    internal class MarsMemoryVaiableTable
    {
        private static Dictionary<string, object> MemoryVariables = new Dictionary<string, object>();
        public static void AddObject(string strMemoryVarIdx, object v)
        {
            if (MemoryVariables.ContainsKey(strMemoryVarIdx))
                MemoryVariables[strMemoryVarIdx] = v;
            else MemoryVariables.Add(strMemoryVarIdx,v);
        }

        public static object GetMarsVariable(string strMemoryVarIdx)
        {
            if (!MemoryVariables.ContainsKey(strMemoryVarIdx)) return null;
            return MemoryVariables[strMemoryVarIdx];
        }
    }

    internal class MarsKeywordParaItem
    {
        public string para;
        public string value;

        protected bool isOk = false;
        public bool IsOk {
            get => isOk;
            set => isOk = value;
        }
        public MarsKeywordParaItem(string strItm)
        {
            DealWithOneItem(strItm);
        }

        protected virtual void DealWithOneItem(string strItm, string defaultSeparator =":")
        {
            try
            {
                string[] arrItms = strItm.Split(new string[] { defaultSeparator }, StringSplitOptions.None);

                para = arrItms[0];
                value = arrItms.Length >= 2 ? arrItms[1] : null;
                isOk = true;
            }
            catch
            {
                isOk = false;
            }
        }
        public MarsKeywordParaItem()
        {

        }
    }
    internal class MarsKeywordParaNestedItem: MarsKeywordParaItem
    {
        public List<KeyValuePair<string,string>> nestedItems=new List<KeyValuePair<string, string>>();
        /// <summary>
        /// strValue should be (xx:xx):(xxx:xx)
        /// </summary>
        /// <param name="strPara"></param>
        /// <param name="strValue"></param>
        public MarsKeywordParaNestedItem(string strPara, string strValue)
        {
            para = strPara;
            value = strValue;
            DealWithOneItem(value, ")");
        }
        protected override void DealWithOneItem(string strItm, string defaultSeparator = ":")
        {
            var lstOfParaV = strItm.Split(new string[] { defaultSeparator },StringSplitOptions.None);
            foreach (var itm in lstOfParaV)
            {
                if (string.IsNullOrEmpty(itm)) continue;
                string itmWithoutBrack = itm.Replace("(", "");
                var itmsForNested = itmWithoutBrack.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (itmsForNested.Length <= 0) continue;
                if (itmsForNested.Length == 1)
                {
                    nestedItems.Add(new KeyValuePair<string, string>(itmsForNested[0], ""));
                }
                else
                {
                    nestedItems.Add(new KeyValuePair<string, string>(itmsForNested[0], itmsForNested[1]));
                }
            }
        }
    }
    /**
     * ²âÊÔ²½ÖèµÄ²ÎÊý¹ÜÀí
     * 1£¬Í¨³£²ÎÊý·ÖÀàÓÃ¡°£»¡±·Ö¿ª£¬ÄÚ²¿Èç¹ûÓÐÐ¡Ïî£¬ÓÃ¡°£º¡±·Ö¿ª
     * */
    internal class MarsKeywordParameter
    {
        protected string currentParamaterSetting = null;
        protected System.Collections.Generic.List<MarsKeywordParaItem> Parameters=new System.Collections.Generic.List<MarsKeywordParaItem>();

        protected bool isValidate;

        public const string cnst_para_idx_nowait = "No_Wait";
        public const string cnst_para_idx_Output = "Output";
        public const string cnst_para_idx_PID = "PID";
        public const string cnst_para_idx_Input = "Input";
        public const string cnst_para_idx_WindowByClass = "Mars_windowsByClass";

        public MarsKeywordParameter(string strParaFromTestStep,string strSpara = ";")
        {
            if (string.IsNullOrEmpty(strParaFromTestStep)) return;
            currentParamaterSetting = strParaFromTestStep;

            var lstPara = strParaFromTestStep.Split(new char[] { ';' });
            dealWithParaItems(lstPara);
        }

        public MarsKeywordParaItem GetSpecialParaExists(string strParaIdx)
        {
            return Parameters.Where(p => string.Compare(p.para, strParaIdx, true) == 0).FirstOrDefault();
        }


        public IEnumerable<MarsKeywordParaItem> IndexSpecialParas(string paraNameToIndex)
        {
            if (!isValidate) return null;
            return Parameters.Where(p => string.Compare(paraNameToIndex, p.para, true) == 0);
        }

        protected virtual void dealWithParaItems(string[] lstPara)
        {
            bool isOk = false;
            foreach (var itm in lstPara)
            {
                if (string.IsNullOrEmpty(itm)) continue;
                if (string.Compare(cnst_para_idx_nowait, itm, true) == 0)
                {
                    MarsKeywordParaItem keywordParaItem = dealWithOneParaItem(itm, ref isOk);
                    if ((!isOk) || (keywordParaItem == null))
                    {
                        isValidate = false;
                        return;
                    }
                    Parameters.Add(keywordParaItem);
                    continue;
                }
                if (itm.StartsWith(cnst_para_idx_Output+":", StringComparison.OrdinalIgnoreCase))
                {
                    //MarsKeywordOutputParaSettings output = new MarsKeywordOutputParaSettings();
                    string nestedValues = itm.Substring((cnst_para_idx_Output+":").Length);
                    MarsKeywordParaNestedItem output = new MarsKeywordParaNestedItem(cnst_para_idx_Output, nestedValues);
                    Parameters.Add(output);
                }else if (itm.StartsWith(cnst_para_idx_Input+":", StringComparison.OrdinalIgnoreCase))
                {
                    string inputValues = itm.Substring((cnst_para_idx_Input + ":").Length);
                    MarsKeywordParaNestedItem input = new MarsKeywordParaNestedItem(cnst_para_idx_Input, inputValues);
                    Parameters.Add(input);
                }else
                {
                    // ÆÕÍ¨±äÁ¿
                    MarsKeywordParaItem keywordParaItem = dealWithOneParaItem(itm, ref isOk);
                    if ((!isOk) || (keywordParaItem == null))
                    {
                        isValidate = false;
                        return;
                    }
                    Parameters.Add(keywordParaItem);
                    continue;
                }
            }
        }

        protected virtual MarsKeywordParaItem dealWithOneParaItem(string paraItem , ref bool isOk)
        {
            if (string.IsNullOrEmpty(paraItem))
            {
                isOk = false;
                return null;
            }
            try
            {
                isOk = true;
                var rslt = new MarsKeywordParaItem(paraItem);
                if (rslt == null)
                {
                    isOk = false;
                    return null;
                }
                if (!rslt.IsOk)
                {
                    isOk = false;
                    return null;
                }
                return rslt;
            }catch(Exception e)
            {
                isOk = false;
                return null;
            }
        }
    }
    /// <summary>
    /// ²âÊÔ²½ÖèµÄoutputÊä³ö¸ñÊ½¡£
    /// Èç¹û²ÉÓÃparameter×÷Îª±íÊ¾£¬½«Ê¹ÓÃ
    /// Ê¾ÀýÈçÏÂ£º
    /// Output:(PID:SMTID):(MainHandle:MAINHANDL)....;
    /// </summary>
    //internal class MarsKeywordOutputParaSettings : MarsKeywordParameter
    //{
    
    //    public MarsKeywordOutputParaSettings(string strParaFromTestStep, string strSpara = ";")
    //    {

    //    }

    //    /// <summary>
    //    /// one sample item is outPut:(PID:SMTID):(xxxx:xxx):().....
    //    /// </summary>
    //    /// <param name="paraItem"></param>
    //    /// <param name="isOk"></param>
    //    /// <returns></returns>
    //    protected override MarsKeywordParaItem dealWithOneParaItem(string paraItem, ref bool isOk)
    //    {
    //        int iFirst = paraItem.IndexOf(':');
    //        if (iFirst <= -1)
    //        {
    //            isOk = false;
    //            return null;
    //        }
    //        string strKey = paraItem.Substring(0, iFirst); /// return like outPut
    //        if (string.IsNullOrEmpty(strKey))
    //        {
    //            isOk = false;
    //            return null;
    //        }
    //        string strSub = paraItem.Substring(iFirst );
    //        string[] arrOutputs = strSub.Split(new string[] { ":(" }, StringSplitOptions.None);
    //        foreach(var outputItm in arrOutputs)
    //        {
    //            if (string.IsNullOrEmpty(outputItm)) continue;
    //            /// each item should like pid:smtid)
    //            /// 
                
    //        }
    //        return null;
    //    }

        
    //}




    internal class MarsCompareDataDealingFunction
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsCompareDataDealingFunction));

        internal string funcName;

        internal string[] paras;

        internal string dealOneRow(string strData, ref bool isOk, ref string strError)
        {
            if (string.IsNullOrEmpty(funcName))
            {
                return strData;
            }
            string text = funcName.ToUpper();
            string text2 = text;
            if (text2 != null && text2 == "SUBSTR")
            {
                return marsSubStr(strData, ref isOk, ref strError);
            }
            return strData;
        }

        private string marsSubStr(string strData, ref bool isOk, ref string strError)
        {
            isOk = false;
            if (string.IsNullOrEmpty(strData))
            {
                strError = "parameter for substring is null or emptyr";
                return strData;
            }
            string[] array = null;
            if (paras[0] == "\\ ")
            {
                array = strData.Split(new char[1]
                {
                ' '
                }, StringSplitOptions.RemoveEmptyEntries);
            }
            else if (paras[0] == "\\tab")
            {
                array = strData.Split(new char[1]
                {
                '\t'
                }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                if (!(paras[0] == "\\;"))
                {
                    strError = $"splitor should be \\space|\\tab|\\;, but it is:[{paras[1]}]";
                    Logger.Error("\t", strError, 71, "marsSubStr");
                    return strData;
                }
                array = strData.Split(new char[1]
                {
                ';'
                }, StringSplitOptions.RemoveEmptyEntries);
            }
            if (paras.Length != 2)
            {
                return strData;
            }
            int result;
            if (!int.TryParse(paras[1], out result))
            {
                MLogger logger = Logger;
                object[] args = paras;
                logger.Error("\t", strError = string.Format("SubStr should like splitor:number.but the second is not a number. [{0}]", args), 78, "marsSubStr");
                return strData;
            }
            if (result >= array.Length)
            {
                Logger.Error("\t", strError = $"idx number is greater than returned. \r\n[{strData}]", 83, "marsSubStr");
                return strData;
            }
            Logger.Info("marsSubStr", $"source:[{array[result]}], idx:[{result}] from source:[{strData}]", 86, "marsSubStr");
            isOk = true;
            return array[result];
        }

        internal static MarsCompareDataDealingFunction GetInstance(string strFuncInfo)
        {
            if (string.IsNullOrEmpty(strFuncInfo))
            {
                return null;
            }
            string[] array = strFuncInfo.Split(':');
            if (array.Length < 1)
            {
                return null;
            }
            MarsCompareDataDealingFunction marsCompareDataDealingFunction = new MarsCompareDataDealingFunction();
            marsCompareDataDealingFunction.funcName = array[0];
            if (array.Length > 1)
            {
                marsCompareDataDealingFunction.paras = new string[array.Length - 1];
                Array.Copy(array, 1, marsCompareDataDealingFunction.paras, 0, marsCompareDataDealingFunction.paras.Length);
            }
            else
            {
                marsCompareDataDealingFunction.paras = null;
            }
            return marsCompareDataDealingFunction;
        }

        internal static string FixParaWithFuncPreFix(string strPara, ref string fixedPara)
        {
            if (string.IsNullOrEmpty(strPara))
            {
                fixedPara = strPara;
                return null;
            }
            if (strPara.ToUpper().StartsWith("SUBSTR:"))
            {
                int num = strPara.IndexOf(";");
                if (num >= 0)
                {
                    fixedPara = strPara.Substring(num + 1);
                    return strPara.Substring(0, num);
                }
                fixedPara = strPara;
                return null;
            }
            fixedPara = strPara;
            return null;
        }
    }


    public class KeywordOpAgent
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MLogger));

        private static string[] autoCheckErrorKeywordsFromConfig = null;

        private static bool isAutoCheckKeywordRead = false;
        public static string[] AutoCheckErrorKeywordsFromConfig
        {
            get
            {
                if (!isAutoCheckKeywordRead)
                {
                    isAutoCheckKeywordRead = true;
                    string text = ConfigurationManager.AppSettings["autoCheckErrorKeywords"];
                    if (string.IsNullOrEmpty(text))
                    {
                        return autoCheckErrorKeywordsFromConfig = null;
                    }
                    autoCheckErrorKeywordsFromConfig = text.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                }
                return autoCheckErrorKeywordsFromConfig;
            }
        }


        private static bool isKeywordsAutoCheckErrorRequired(string strKeyword)
        {
            if (string.IsNullOrEmpty(strKeyword))
            {
                return false;
            }
            if (autoCheckErrorKeywordsFromConfig == null)
            {
                return false;
            }
            return autoCheckErrorKeywordsFromConfig.Any((string p) => string.Compare(p, strKeyword, true) == 0);
        }

        public static TestStepErrorCheckSetting applicationAttachedExtraSettings = null;

        public static TestStepErrorCheckSetting InitExtraRequirementObj(string strExtra)
        {

            Logger.logBegin("InitExtraRequirementObj");
            try
            {
                JavaScriptSerializer j = new JavaScriptSerializer();
                var rslt = j.Deserialize<TestStepErrorCheckSetting>(strExtra);
                return rslt;
            }
            catch (Exception e)
            {
                Logger.Error("InitExtraRequirementObj", e.Message, e);
                return null;
            }
        }

        private static bool IsCurrentApplicationRequiresErrorCheck()
        {
            if (applicationAttachedExtraSettings == null) return false;
            return applicationAttachedExtraSettings.autoError;
        }

        private static string BuildAutoChckAttachInfo()
        {
            if (applicationAttachedExtraSettings == null) return "";
            return System.Text.Json.JsonSerializer.Serialize(applicationAttachedExtraSettings);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stepObject"></param>
        /// <param name="stepData"></param>
        /// <param name="stepsFromDB"></param>
        /// <param name="strMode"></param>
        /// <param name="strError"></param>
        /// <param name="writebackObjectName"></param>
        /// <param name="writebackData"></param>
        /// <param name="strVarType"></param>
        /// <param name="strActualInput"></param>
        /// <param name="isVar">
        /// </param>
        /// <param name="isSkipped"></param>
        /// <returns></returns>
        internal static bool DoTestStep(B_V_OBJECT_SNAPSHOT stepObject, TEST_DATA_SETTINGDTO stepData,
            V_TEST_STEPS_FULLVISIONDTO stepsFromDB, string strMode,
            com.Mars.Constants.Mars_applicationTyp.MARS_APPTYPE appTyp,
            AutoErrorCheck autoErrorCheckInfo,             
            ref string strParaWithFunc,
            ref string strError,
            ref string writebackObjectName,
            ref string writebackData,
            ref string strVarType,
            ref string strActualInput,
            ref bool isVar,
            ref bool isSkipped,
            ref string strAdv,
            ref string strStackInfo,
            ref string strSnapShotFilePath, 
            bool isPreviewKeyword = false,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName,
            int iLoopIdx= -1,
            KeywordExecuteCallBack dataSetBackCallBack = null,
            bool isAttachUIAAHwnd=false)
        {
            if (!isPreviewKeyword)
                Logger.logBegin("DoTestStep", string.Format("StepId:[{6}], object:[{0}] keywordId:[{1}] Data:[{2}] PEG:[{3}] obj:[{4}] objId:[{5}]", stepObject == null ? "" : stepObject.OBJECT_HAPPY_NAME,
                    stepsFromDB == null ? "N/A" : stepsFromDB.KEY_WORD_NAME, stepData,
                    stepObject == null ? "N/A" : stepObject.PEG_QUICK_ACCESS,
                    stepObject == null ? "N/A" : stepObject.QUICK_ACCESS,
                    stepObject == null ? "N/A" : stepObject.OBJECT_ID + "",
                    stepsFromDB == null ? -1 : stepsFromDB.STEPS_ID));
            else
            {
                Logger.logBegin("DoTestStep", "preview mode");
                Logger.logBegin("DoTestStep", string.Format("StepId:[{6}], object:[{0}] keywordId:[{1}] Data:[{2}] PEG:[{3}] obj:[{4}] objId:[{5}]", stepObject == null ? "" : stepObject.OBJECT_HAPPY_NAME,
                stepsFromDB == null ? "N/A" : stepsFromDB.KEY_WORD_NAME, stepData,
                stepObject == null ? "N/A" : stepObject.PEG_QUICK_ACCESS,
                stepObject == null ? "N/A" : stepObject.QUICK_ACCESS,
                stepObject == null ? "N/A" : stepObject.OBJECT_ID + "",
                stepsFromDB == null ? -1 : stepsFromDB.STEPS_ID));
            }
            string strKeyword = "";
            bool isOk = false;

#if _demoLicense
            string formatString = "yyyyMMdd";
            string sample = "20260901";
            DateTime dt = DateTime.ParseExact(sample, formatString, null);
            Random _x = new Random();
            if ((DateTime.Now > dt) && ((_x.Next() % 2) == 0))
            {
                Application.Exit();
                return false;
            }
#endif

            try
            {
                //if (!isPreviewKeyword) { 
                /// Ëã·¨£º
                /// 1, keyword ÊÇ·ñÊÇÎÞÐëGUIÏà¹Ø£¬Èç¹ûÊÇ£¬ÔòÓÃNonGUIµÄ·½·¨
                ///                 
                isSkipped = false;
                bool isGUIKeyword = false;
                if (!isPreviewKeyword)
                {
                    if (stepsFromDB.KEY_WORD_ID > 0)
                    {
                        isGUIKeyword = CheckKeywordTypeGUIKeyword(stepsFromDB.KEY_WORD_ID, strDBIdx, ref strKeyword, ref isOk, ref strError);
                    }
                    else
                    {
                        strKeyword = stepsFromDB.KEY_WORD_NAME;
                        if (string.IsNullOrEmpty(strKeyword))
                        {
                            isOk = false;
                            strError = "No keyword name or Keyword id is passed";
                            Logger.Error("DoTestStep", strError);
                            return false;
                        }
                        isOk = !KeyWordsOPForNonGUI.Non_GUIKeyword.ContainsKey(stepsFromDB.KEY_WORD_NAME.Trim().ToUpper());
                        isGUIKeyword = isOk;
                    }
                    if (!isOk) return false;
                }
                else
                {
                    if (!(string.Compare(strKeyword = stepsFromDB.KEY_WORD_NAME, "previewObject", true) == 0))
                    {
                        strError = string.Format("[{0}] is not previewobject", stepsFromDB.KEY_WORD_NAME);
                        Logger.Error("DoTestStep", strError);
                        return false;
                    }
                    isGUIKeyword = true;
                }

                if ((stepData != null) && ((stepData.DATA_DIRECTION == 4) || (string.Compare("skip", stepData.DATA_VALUE, true) == 0)))
                {
                    Logger.Info("DoTestStep", strError = string.Format("Keyword:[{0}] is skipped as configed", strKeyword));
                    isSkipped = true;
                    return true;
                }

                //ÐèÒªÐÞÕý Êý¾Ý
                string strData = writebackObjectName = stepData == null ? null : stepData.DATA_VALUE;
                string strDataFixed = strData;

                //ÊÇ·ñÓÐ¼ÓÃÜµÄ
                if ((stepsFromDB != null) && (!string.IsNullOrEmpty(stepsFromDB.COLUMN_ROW_SETTING))
                    && (
                        (stepsFromDB.COLUMN_ROW_SETTING.ToUpper().IndexOf("MARSPWD") >= 0)
                    ||  (stepsFromDB.COLUMN_ROW_SETTING.ToUpper().IndexOf("MARSENCODE") >= 0)
                    ))
                {
                    //DECODE THE DATA 
                    try
                    {
                        strDataFixed = Mars.message.Securities.MarsEncodePwd.DecodeString(strDataFixed);
                    }catch(Exception e)
                    {
                        strError = $"{strDataFixed} is not encoded";
                        strAdv = "Contact Marquis";
                        strStackInfo = MarsErrorStacks.StackTraceDump();
                        return false;
                    }
                }

                if (MarsWindowsAPIsExtend.RegularTest("^" + B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL + ":", strData))
                {
                    writebackObjectName = strData.Substring(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL.Length + 1);
                    int iMode = 2;
                    if (string.Compare("Base", strMode, true) == 0) iMode = 1;
                    if (!BoHelper.GetModalVariableInfo(writebackObjectName, (short)iMode, ref strError, ref strDataFixed,strDBIdx))
                    {
                        return false;
                    }
                    isVar = true;
                    Logger.Info("DoTestStep", $"Modal var:[{strData}]--->[{strDataFixed}]");
                    strVarType = B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL;
                }
                if (MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^" + B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL, strData))
                {
                    writebackObjectName = strData.Substring(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL.Length + 1);
                    if (!BoHelper.GetGlobalVariableInfo(writebackObjectName, ref strError, ref strDataFixed,strDBIdx))
                    {

                        return false;
                    }
                    isVar = true;
                    strVarType = B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL;
                }
                if (MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^" + B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP, strData))
                {
                    writebackObjectName = strData.Substring(B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP.Length + 1);
                    if (!BoHelper.GetLoopVariableInfo(writebackObjectName, ref strError, ref strDataFixed, strDBIdx))
                    {

                        return false;
                    }
                    isVar = true;
                    strVarType = B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP;
                }

                strError = "";
                strActualInput = strDataFixed;
                if (APIEngineHelper.IsKeywordAPIIntegrated(strKeyword))
                {
                    strActualInput = stepData == null ? "" : stepData.API_RUNTIME_CONFIG;
                }

                MARSDealResult dealResult = new MARSDealResult();
                dealResult.CheckObjectWaitingTime = autoErrorCheckInfo == null ? -1 : autoErrorCheckInfo.waitTime;

                string fixedPara = "";
                strParaWithFunc = MarsCompareDataDealingFunction.FixParaWithFuncPreFix(stepsFromDB.COLUMN_ROW_SETTING, ref fixedPara);
                //strActualInput = dealResult.ActualInputData= stepData == null ? null : stepData.DATA_VALUE;
                if (!isGUIKeyword)
                {
                    isOk = KeyWordsOPForNonGUI.RunKeywordByKeywordName(stepsFromDB.STEPS_ID,
                        strKeyword, stepObject, stepsFromDB.COLUMN_ROW_SETTING,
                        strActualInput, //stepData==null?"":stepData.DATA_VALUE, 
                        stepData==null?"": stepData.API_RUNTIME_CONFIG,
                        appTyp,
                        ref strError,
                        ref dealResult,
                        strDBIdx,
                        dataSetBackCallBack,
                        isAttachUIAAHwnd);
                     return isOk;
                }

                /// 消息模式，GUI类的
                ///           
                string strAttachInfo = "";
                ConsoleLog.IntimeLog_KeywordTitle("\tgoing to run guikeyword [{0}] strDBIdx:[{1}]", strKeyword,strDBIdx);
                if (IsCurrentApplicationRequiresErrorCheck())
                {
                    strAttachInfo = BuildAutoChckAttachInfo();
                }
                else
                {
                    bool isAutoCheckError = isKeywordsAutoCheckErrorRequired(strKeyword);
                   
                    if (isAutoCheckError)
                    {
                        strAttachInfo = $"MarsAutoCheckError:backGroudColor:{Color.Yellow.Name};{Color.Red.Name}";
                    }
                }

                if (isAttachUIAAHwnd)
                {
                    /// 需要附加UIA Hwnd
                    /// 
                    var hwnd = MARSUIAppSideVariables.GetCurrentUIAPegHwnd(ref isOk, ref strError,ref strAdv, ref strStackInfo);
                    if (!isOk)
                    {
                        Logger.Error("DoTestStep", $"GetCurrentUIAPegHwnd failed for keyword:[{strKeyword}] with error:[{strError}]");
                        return false;
                    }   

                }

                /// 判断是不是api相关的keyword，如果是

                isOk = KeywordOpForGUI.RunKeywordByKeywordName(
                    stepsFromDB.RUN_ORDER,
                    strKeyword,
                    stepObject,
                    stepsFromDB.COLUMN_ROW_SETTING, strDataFixed,
                    stepData == null ? "" : stepData.API_RUNTIME_CONFIG,
                    strAttachInfo,
                    appTyp,
                    strDBIdx,
                    ref strError,
                    ref dealResult,
                    isAttachUIAAHwnd);
                strAdv = dealResult == null ? "" : dealResult.Advice;
                strStackInfo = dealResult == null ? "" : dealResult.StackInfo;
                strSnapShotFilePath = dealResult.snapshotFilePath;
                if ((!isOk) || (dealResult == null) || (string.Compare(dealResult.ResultMessage, "SUCCESS", true) != 0))
                {

                    Logger.Info("\t", string.Format("{0}-{1}-[{2}] return Fails with Error:[{3}]", strKeyword, stepsFromDB.COLUMN_ROW_SETTING, stepData == null ? null : stepData.DATA_VALUE,
                        dealResult == null ? "null" : strError = string.Format("{0} {1}", dealResult.ResultMessage, dealResult.ErrorMessage)));
                    return false;
                }
                else
                {
                    writebackData = dealResult.ReturnedData;
                    Logger.Info("\t", $"4 returned data:{writebackData}");
                    Logger.Info("\t", string.Format("{0}-{1}-[{2}] finished", strKeyword, stepsFromDB.COLUMN_ROW_SETTING, stepData == null ? null : stepData.DATA_VALUE));
                    return true;
                }
            }
            catch (Exception e)
            {
                string tmpError = string.Format("Keyword:[{0}] Failed, with Exception:[{1}] stackTrace:{2}", strKeyword, strError = e.Message, e.StackTrace);
                ConsoleLog.IntimeLog_keywordSub("Keyword:[{0}] Failed, with Exception:[{1}] stackTrace:{2}", strKeyword, strError = e.Message, e.StackTrace);
                //Console.WriteLine("Exception :[{0}] stack:[{1}]",e.Message, e.StackTrace);
                Logger.Error("DoTestStep", tmpError);
                return false;
            }
            finally
            {
                /// ÈÃÆÁ±£Ö®ÀàÊ§Ð§
                //MarsWindowsAPIsExtend.SimulateInputString((char)(VirtualKeyStates.VK_NUMLOCK)+"");
                //MarsWindowsAPIsExtend.SimulateInputString(VirtualKeyStates.VK_NUMLOCK + "");
                Logger.logEnd("DoTestStep", string.Format("[{0}] returns [{1}]", strKeyword, isOk));
            }
        }


        private static bool CheckKeywordTypeGUIKeyword(long lKeywordId, string strDBIdx,ref string strKeyword, ref bool isOk, ref string strError)
        {
            strKeyword = B_KEYWORD.GetKeywordName(lKeywordId, ref isOk, ref strError, strDBIdx) ?? "";
            KeyWordsOPForNonGUI.currentDBIdx = strDBIdx;
            if (!isOk) return false;
            return !KeyWordsOPForNonGUI.Non_GUIKeyword.ContainsKey(strKeyword.Trim().ToUpper());
        }

    }
}
