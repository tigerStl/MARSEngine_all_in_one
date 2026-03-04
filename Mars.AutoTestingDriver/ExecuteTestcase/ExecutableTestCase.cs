
extern alias clientWCF;
using clientWCF::MarsTestFrame.CommuniteServer;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using clientWCF::TestFrameMonitor.Server.ServiceContracts;
using com.Mars.Constants;
using Mars.AutoTestingDriver.DataTolerance;
using Mars.AutoTestingDriver.db;
using Mars.AutoTestingDriver.ErrorMessage;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.AutoTestingDriver.SystemUtil;
using Mars.message.Business;
//using MarsTestFrame.DataLayer;
using Mars.message.Dto;
using System.Collections.Generic;

using MarsTestFrame.SourceCode.com.Mars.BusinessLogic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using MarsEnginer.windowsWrapper.SystemUtil;
using Mars.message.DataLayer;
using System.Windows.Forms;
using Mars.message.Utility;
using Mars.AutoTestingDriver.ExecuteStoryboard;
using System.Threading;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.baseInterfaceAndClass;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using Mars.AutoTestingDriver.dotnetCore;
using WebSocketSharp;
using Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess;
using ManagedInjector;
using Mars.AutoTestingDriver.MarsUISupport;
using Mars.message.Inter.MQCenter.interProcess;
using System.Windows.Automation;
using Mars.AutoTestingDriver.ApiIntegratedHelper;

namespace Mars.AutoTestingDriver.ExecuteTestcase
{

    public delegate void OnTestStepExectionDoneEvent(ExecutableTestCaseStep currentStep, bool isOk,string strActualDataInput, string strRsult);
    public delegate void OnTestcaseIsDoneEvent(bool isOk, string strResult);
    public class TestStepExecutionRecorder
    {

        protected class testStepResult
        {
            bool testResult;
            string testInfo;
        }
        protected Dictionary<ExecutableTestCaseStep, testStepResult> testRecorder = new Dictionary<ExecutableTestCaseStep, testStepResult>();

        //private List<ExecutableTestCaseStep> _testSteps;
        //public List<ExecutableTestCaseStep> testSteps { get => _testSteps; set {
        //        _testSteps = value;
        //        if (_testSteps != null)
        //        {
        //            testRecorder.Clear();
        //            _testSteps.ForEach(p =>
        //            {
        //                testRecorder.Add(p, new testStepResult());
        //            });
        //        }
        //    } }
        public OnTestStepExectionDoneEvent onTestStepExectionDoneHanlder;
        public OnTestcaseIsDoneEvent onTestcaseIsDoneHandler;
    }

    public class TestStepRecorder
    {
        #region test step execution report
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public bool isTestStepOk { get; set; }

        public string ord { get; set; }
        public string Keyword { get; set; }
        public string ObjectName { get; set; }
        public string ObjectIDs { get; set; }
        public string Parameter { get; set; }
        public string TestData { get; set; }
        public string OperationData { get; set; }
        public string TestResult { get; set; }
        #endregion

        public static List<TestStepRecorder> InitFromExecutableTestCaseStep(List<ExecutableTestCaseStep> src)
        {
            if (src == null) return null;
            List<TestStepRecorder> rsltLst = new List<TestStepRecorder>();
            foreach(var itm in src)
            {
                if (itm == null) continue;
                var rcdr = FromTestStepExecutionRecorder(itm);
                if (rcdr == null) continue;
                rsltLst.Add(rcdr);
            }
            return rsltLst;
        }

        private static TestStepRecorder FromTestStepExecutionRecorder(ExecutableTestCaseStep srcObj)
        {
            if (srcObj == null) return null;
            TestStepRecorder rslt = new TestStepRecorder()
            {
                ord = srcObj.RunId + "",
                Keyword = srcObj.Keyword,
                ObjectName = srcObj.ObjectName,
                ObjectIDs = srcObj.StepObject == null ? null : srcObj.StepObject.QUICK_ACCESS,
                Parameter = srcObj.StepsFromDB == null ? null : srcObj.StepsFromDB.COLUMN_ROW_SETTING,
                TestData = srcObj.StepData == null ? null: srcObj.StepData.DATA_VALUE
            };
            return rslt;
        }
    }

    public class TestStepExecutionRecorder_TMP: TestStepExecutionRecorder
    {
        private const string testStepReportFileName = "tempTestReport.html";
        private const string cnst_reportDataDirectory = "reportData";
        private const string cnst_js_dataFileStart = "<!--MarsReplaceMark-->";
        private const string cnst_js_dataFileEnd = "<!--MarsReplaceMark end-->";
        public string JsName { get; set; }
        private List<ExecutableTestCaseStep> allSteps;
        private List<TestStepRecorder> stepsReports;
        public List<ExecutableTestCaseStep> AllSteps { get => allSteps; 
            set
            {
                if (allSteps != value)
                {
                    allSteps = value;
                    stepsReports = TestStepRecorder.InitFromExecutableTestCaseStep(allSteps);// new List<TestStepRecorder>();

                }
            }
        }
        private static string reportDiretory = null;
        private static string templateHtmlFileNameWithPath = null;
        
        
        public TestStepExecutionRecorder_TMP() : base()
        {
            onTestStepExectionDoneHanlder += OnTestStepExectionDoneImpl;
            onTestcaseIsDoneHandler += onTestcaseIsDoneImpl;

            InitReportDir();
        }

        public static bool IsTempTestCaseReady()
        {
            var pth = typeof(TestStepExecutionRecorder_TMP).Assembly.Location;
            var JSPath = System.IO.Path.GetDirectoryName(pth);
            JSPath = System.IO.Path.Combine(JSPath, "htmlReport");// testStepReportFileName);
            var strRptTemplateFile = System.IO.Path.Combine(JSPath, testStepReportFileName) ;
            var reportDataPth = Path.Combine(JSPath, cnst_reportDataDirectory);
            if ((!File.Exists(strRptTemplateFile)) || (!Directory.Exists(reportDataPth))) return false;

            reportDiretory = reportDataPth;
            templateHtmlFileNameWithPath = strRptTemplateFile;
            return true;
        }

        private void InitReportDir()
        {
            
        }
        
        private void OnTestStepExectionDoneImpl(ExecutableTestCaseStep currentStep, bool isOk,string strActualDataInput, string strRsult)
        {
            /// 算法：
            /// 1，修改
            /// 
            var targetStp = allSteps.Where(p => p != null && p.RunId == currentStep.RunId).FirstOrDefault();
            if (targetStp == null) return;
            var rcdr = this.stepsReports.Where(p => p != null && string.Compare(currentStep.RunId + "", p.ord) == 0).FirstOrDefault();
            if (rcdr == null) return;

            rcdr.endTime = DateTime.Now;
            rcdr.isTestStepOk = isOk;
            rcdr.TestResult = strRsult;
        }
        private void onTestcaseIsDoneImpl(bool isOk, string strResult)
        {
            /// 算法：
            /// 1，创建临时的jsdata文件（先删除）
            /// 2，修改template的js文件名
            /// 3，用window.open打开template的html
            /// 
            //var pth = typeof(TestStepExecutionRecorder_TMP).Assembly.Location;
            //var JSPath = System.IO.GetDirectoryRoot(pth);
            //JSPath = System.IO.Path.Combine(JSPath, testStepReportFileName);
            //var reportDataPth = Path.Combine(JSPath, cnst_reportDataDirectory);
            //if ((!File.Exists(JSPath))||(!Directory.Exists(reportDataPth)))
            //{
            //    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsErrorMessageBox($"no such File [{JSPath}] \r\n Or folder:[{reportDataPth}],\r\ncan't generate Tes report.", null, null, null,
            //        "Contact Marquis", null, null);
            //    return;
            //}

            /// 2，修改template的js文件名
            /// 
            string accntName = Environment.UserName;
            accntName = MarsWindowsAPIsExtend.FixFolderName(accntName);
            
            string strTimeTail = DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss");
            string strNewDataJSFileName = $"test_{accntName}_{strTimeTail}.js";
            strNewDataJSFileName = System.IO.Path.Combine(reportDiretory, strNewDataJSFileName);

            bool isRptOk = WriteJsON2DataFile(strNewDataJSFileName);
            if (!isRptOk)
            {
                MessageBox.Show($"Can't create file {strNewDataJSFileName}.\r\nContact Marquis");
                return ;
            }
            //change 
            string strNewHtmlFileName = "";
            if (!CreateNewTemplateReportFile(strNewDataJSFileName, ref strNewHtmlFileName))
            {
                MessageBox.Show($"Can't create a new HTML for {strNewDataJSFileName}.\r\nContact Marquis.", "Message", MessageBoxButtons.OK,MessageBoxIcon.Information);
                return;
            }

            Process.Start("Chrome", strNewHtmlFileName);
        }



        private bool CreateNewTemplateReportFile(string strDataFileName,ref string strNewHtmlFileName)
        {
            try
            {
                string templateHtmlContent = File.ReadAllText(templateHtmlFileNameWithPath);
                int iReplaceStartPos = templateHtmlContent.IndexOf(cnst_js_dataFileStart);
                int iReplaceEndPos = templateHtmlContent.IndexOf(cnst_js_dataFileEnd);
                if (iReplaceEndPos <= iReplaceStartPos) return false;
                if ((iReplaceStartPos <= 0) || (iReplaceEndPos) <= 0) return false;

                string strHtmlHeadPart = templateHtmlContent.Substring(0, iReplaceStartPos);
                string strHtmlTailPart = templateHtmlContent.Substring(iReplaceEndPos + cnst_js_dataFileEnd.Length + 1);
                string strHtmlJS2LoadData = $"<script type=\"text/javascript\" src=\"{strDataFileName}\"></script>";

                string fixedAccntName = MarsWindowsAPIsExtend.FixFolderName(Environment.UserName);
                string tmpRptFileName = $"tempTestReport_{fixedAccntName}_{DateTime.Now.ToString("yyyy-MM-ddHHmmss")}.html";
                strNewHtmlFileName = System.IO.Path.GetDirectoryName(templateHtmlFileNameWithPath);
                strNewHtmlFileName = System.IO.Path.Combine(strNewHtmlFileName, tmpRptFileName);
                if (File.Exists(strNewHtmlFileName))
                {
                    try
                    {
                        File.Delete(strNewHtmlFileName);
                    }
                    catch (Exception e) { }
                }
                string strNewContent = $"{strHtmlHeadPart}{strHtmlJS2LoadData}{strHtmlTailPart}";
                File.WriteAllText(strNewHtmlFileName, strNewContent);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception:{e.Message}\r\n{e.StackTrace}");
                return false;
            }
        }

        private bool WriteJsON2DataFile(string strFileName)
        {
            if (File.Exists(strFileName))
            {
                try
                {
                    File.Delete(strFileName);
                }
                catch (Exception e)
                {

                }

            }
            try
            {
                StreamWriter w = new StreamWriter(strFileName);
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                string strRsltJSON = js.Serialize(stepsReports);
                string str2Write = $"var dataSet={strRsltJSON};";
                w.WriteLine(str2Write);
                w.Flush();
                w.Close();
                return true;
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }

    public class AutoErrorCheck
    {
        public const string cnst_defaultErrorObj = "defaultErrorObj";
        public const string cnst_waitTime = "defaultCheckError_time";
        public const string cnst_autoCheckErrorKeyword = "autoCheckErrorKeywords";

        internal string checkErrorQuickAccess;
        internal int waitTime; //ºÁÃë
        internal string autoCheckErrorKeywords {
            get => autoCheckErrorKeywordsFromConfig;
            set
            {
                autoCheckErrorKeywordsFromConfig = value;
                if (string.IsNullOrEmpty(value))
                {
                    keywords = null; }
                else
                {
                    keywords = autoCheckErrorKeywordsFromConfig.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }
        private string autoCheckErrorKeywordsFromConfig;
        private string[] keywords;

        public bool IsAutoErrorChck
        {
            get => (!string.IsNullOrWhiteSpace(checkErrorQuickAccess)) && (waitTime > 0) && (!string.IsNullOrWhiteSpace(autoCheckErrorKeywords));
        }

        internal bool isKeyRequiresAutoCheck(string strCurrentKeyword)
        {
            if (string.IsNullOrEmpty(strCurrentKeyword)) return false;
            if (keywords == null) return false;

            return keywords.Any(p => p.Equals(strCurrentKeyword, StringComparison.OrdinalIgnoreCase));
        }
    }

    internal class MarsIfElseRunJump
    {
        internal string KeywordName;
        internal int stepId;
        internal int endStepsId;
        internal bool? isSkip = null;
    }

    internal class MarsLoopRunJump
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsLoopRunJump));
        internal int loopBeginRunOrder;
        internal int loopEndRunOrder;
        //internal int loopEndStepIdx;// 执行for循环时的loop的index值
        internal string[] loopStrings;
        private int _currentIdx;
        internal int CurrentIdx {
            get =>_currentIdx;
            set
            {
                _currentIdx = value;
                string strStack = MarsErrorStacks.StackTraceDump(new StackTrace(true));
                Logger.Debug("CurrentIdx", strStack);
            }
        }
        internal int loopIterationCount = -1;
        /// <summary>
        /// 11/21/24 添加
        /// 如果loopVarMode=2，表示是一个整数，loop使用asnumber为参数
        /// </summary>
        internal MarsVar_Type loopVarMode = MarsVar_Type.varT_normal;// default is 0,表示常规loop var，1 表示status var，采用loopvars作为数据

        internal List<MarsVarBasic> loopVars;
        internal MarsLoopRunJump()
        {
            loopBeginRunOrder = -1;
            loopEndRunOrder = -1;
            _currentIdx = -1;
        }
        /// <summary>
        /// 11/22/24修改
        ///     对于ITeration 模式，需要判断当前的行号位置是否到了最后一行
        /// </summary>
        /// <returns></returns>
        internal bool anyNoMoreItems()
        {
            if (this.loopVarMode == MarsVar_Type.VarT_userMemIteration)
            {
                return this._currentIdx < this.loopIterationCount - 1;
            }
            else
            {
                int oldIdx = _currentIdx;

                bool isMovable = MoveDataIdxToAvailable();
                _currentIdx = oldIdx;
                return isMovable;
            }
        }

        public bool AreAnyMoreLoopVars()
        {
            switch (loopVarMode)
            {
                case MarsVar_Type.varT_normal:
                    return loopStrings == null ? false : loopStrings.Length > 0;
                case MarsVar_Type.varT_Sattus:                    
                    for (int i = 0; i < (loopVars == null ? -1 : loopVars.Count); i++)
                    {
                        var itm = loopVars[i];
                        if (itm == null) continue;
                        if (!(itm is MarsStatusVar)) continue;
                        MarsStatusVar itmStatus = (MarsStatusVar)itm;
                        for (int iIdx = 0; iIdx < itmStatus.varItems.Count; iIdx++)
                        {
                            /// 先将数据指针移到当前位置                            
                            if (iIdx < CurrentIdx) continue;
                            string strStatus = itmStatus.varItems[iIdx].Value;
                            if (string.IsNullOrEmpty(strStatus)) continue;
                            if (string.Compare("1", strStatus, true) == 0)
                            {
                                return true;
                            }
                        }                        
                    }
                    return false;
                case MarsVar_Type.VarT_userMemIteration:
                    return loopIterationCount > 0;
                default:
                    return false;
            }
        }

        internal bool MoveDataIdxToAvailable()
        {
            Logger.Debug("MoveDataIdxToAvailable", $"MoveDataIdxToAvailable begin|{loopVarMode}|{_currentIdx}|{loopIterationCount}");
            if (loopVarMode==(int)MarsVar_Type.varT_normal)
            {
                _currentIdx++;
                if (loopStrings == null)
                {
                    return false;
                }
                return !(_currentIdx >= loopStrings.Length);
            }else if (loopVarMode == MarsVar_Type.VarT_userMemIteration)
            {
                _currentIdx++;
                return _currentIdx <= this.loopIterationCount - 1;
            }
            else
            {
                int iIdx = -1;
                bool isLocated = false;
                for (int i = 0; i < (loopVars == null ? -1 : loopVars.Count); i++)
                {
                    var itm = loopVars[i];
                    if (itm == null) continue;
                    if (!(itm is MarsStatusVar)) continue;
                    MarsStatusVar itmStatus = (MarsStatusVar)itm;
                    for (int j = 0; j < itmStatus.varItems.Count; j++)
                    {
                        /// 先将数据指针移动到当前位置
                        iIdx++;
                        if (iIdx <= CurrentIdx) continue;
                        string strStatus = itmStatus.varItems[j].Value;
                        if (string.IsNullOrEmpty(strStatus)) continue;
                        if (string.Compare("1", strStatus, true) == 0)
                        {
                            isLocated = true;
                            break;
                        }
                    }
                    if (isLocated) break;
                }
                if (isLocated)
                {
                    CurrentIdx = iIdx;
                    return true;
                }
                return false;
            }
        }

        internal int GetStuatusVarCount()
        {
            if (loopVars == null) return -1;
            return loopVars.Sum(p => p == null ? 0 : p.GetVarRowsCount());
        }


        internal string GetCurrentStatusLoopVar(ref bool isOk,ref string strError)
        {
            Logger.logBegin("GetCurrentStatusLoopVar", $"loopvarMode|{loopVarMode}|vars|{loopVars}");
            if (loopVarMode == MarsVar_Type.VarT_userMemIteration)
            {
                isOk = true;
                return CurrentIdx + "";
            }

            if (loopVarMode == MarsVar_Type.varT_normal)
            {
                if (loopStrings==null)
                {
                    Logger.Error("GetCurrentStatusLoopVar", strError = $"NO loop data is set");
                    isOk = false;
                    return null;
                }    
                if ((_currentIdx<0)||(_currentIdx>= loopStrings.Length))
                {
                    Logger.Error("GetCurrentStatusLoopVar", strError = $"No more items for current Loop|{_currentIdx}| or item index is invalidate|{_currentIdx}");
                    isOk = false;
                    return null;
                }
                isOk = true;
                return loopStrings[_currentIdx];
            }            
            
            if ((loopVarMode != MarsVar_Type.varT_Sattus)||(loopVars==null))
            {
                Logger.Error("GetCurrentStatusLoopVar", $"not|{MarsVar_Type.varT_Sattus}|or|loopVars is|{loopVars}|");
                isOk = false;
                return null;
            }
            int iIdx = 0, preIdx=0;
            for (int i=0;i< loopVars.Count; i++)
            {
                var itm = loopVars[i];
                if ((itm as MarsStatusVar)==null)
                {
                    isOk = false;
                    Logger.Error("GetCurrentStatusLoopVar", strError = "One Item in Loop controller is not status type of variable");
                    return null;
                }
                MarsStatusVar itmStatus = (MarsStatusVar)itm;
                preIdx = iIdx;
                iIdx += (itmStatus.varItems == null ? 0 : itmStatus.varItems.Count);
                if (iIdx > CurrentIdx)
                {
                    /// 即 在其中的某个节点
                    ///
                    int iSubPos = iIdx - CurrentIdx;
                    var targetItm = itmStatus.varItems[itmStatus.varItems.Count - iSubPos];
                    if (targetItm.Equals(default(KeyValuePair<string, string>)))
                    {
                        isOk = false;
                        Logger.Error("GetCurrentStatusLoopVar", strError = $"No avaialbe data for #{CurrentIdx}", MarsErrorStacks.StackTraceDump(new StackTrace(true)));
                        return null;
                    }
                    isOk = true;
                    return targetItm.Key;
                }
            }
            Logger.Error("GetCurrentStatusLoopVar" , strError =$"Index [{CurrentIdx}] exccedes total count");
            isOk = false;
            return null;
        }
    }
    internal class MarsEndLoop
    {
        const string cnst_end_at = "ENDAT:";
        internal int endLoopAt;
        string sourceData;
        internal int endLoopRunOrd
        {
            get;
            set;
        }
        public MarsEndLoop(int currentEndLoopOrd)
        {
            endLoopRunOrd = currentEndLoopOrd;
        }
        public bool ParseData(string strData, ref string strError)
        {
            sourceData = strData;
            endLoopAt = -1;
            if (string.IsNullOrEmpty(sourceData))
            {
                endLoopAt = int.MaxValue;
                return true;
            }
            if (!sourceData.Trim().ToUpper().StartsWith(cnst_end_at))
            {
                strError = "Data for endloop should start with 'EndAt:' or empty";
                return false;
            }
            string strtmp = sourceData.Substring(cnst_end_at.Length);
            if (!int.TryParse(strtmp, out endLoopAt))
            {
                endLoopAt = -1;
                strError = $"if Data for endloop starts with 'EndAt:' then, format should be :EndAt:whole-int, but the data is [{sourceData}]";
                return false;
            }
            return true;
        }
    }    


    public class ExecutableTestCaseStep
    {
        #region  related database Objects
        internal V_TEST_STEPS_FULLVISIONDTO StepsFromDB;
        internal B_V_OBJECT_SNAPSHOT StepObject;
        internal B_V_OBJECT_SNAPSHOT RuntimeObj = null; // for pegwindow only, tiger, 
        private TEST_DATA_SETTINGDTO stepData = null;
        #endregion
        internal TEST_DATA_SETTINGDTO StepData
        {
            get
            {
                return stepData;
            }
            set
            {
                stepData = value;
                if (value == null) return;
                ExtraDataRequirement.DataSrc = stepData.DATA_VALUE;
            }
        }

        internal void SetData(string strNewValue)
        {
            if (stepData == null)
                stepData = new TEST_DATA_SETTINGDTO();
            stepData.DATA_VALUE = strNewValue;
        }

        internal string GetData()
        {
            return stepData == null ? "" : stepData.DATA_VALUE;
        }

        internal string GetPara()
        {
            return this.Row_Column;
        }
        internal void setPara(string strNewPara)
        {
            if (StepsFromDB == null)
                StepsFromDB = new V_TEST_STEPS_FULLVISIONDTO();
            StepsFromDB.COLUMN_ROW_SETTING = strNewPara;
        }

        internal void SetObjectDetailInfo(string strObjDtl)
        {
            if (StepObject == null)
                StepObject = new B_V_OBJECT_SNAPSHOT();
            StepObject.QUICK_ACCESS = strObjDtl;
        }

        #region test step execution report
        #endregion

        TestStepDataPrepare ExtraDataRequirement = new TestStepDataPrepare();
        public string DATA_VALUE
        {
            get
            {
                return ExtraDataRequirement.DataExtractPrefix;
            }
        }

        internal static TestStep4Services convertToWcfData(ExecutableTestCaseStep objStp)
        {
            return new TestStep4Services()
            {
                AssignedTestStepId = objStp.TestStepId,
                ObjectName = objStp.ObjectName,
                Comment = objStp.Comment,
                RunID = objStp.RunId,
                Keyword = objStp.Keyword,
                Row_Column = objStp.Row_Column,
                Value = "", // it shold change later
                QuickAccess = "",
                QuickAccessFull = "",
                Loop = 1,
                ParentAttachInfo = ""
            };
        }

        private const string cnst_tolarence_round = "MARS_ROUND";

        //public static bool IsAStepWithTolarenceFunc(string strComment)
        //{
        //    if (string.IsNullOrEmpty(strComment)) return false;
        //    string strUpper = strComment.ToUpper();
        //    if (strUpper.StartsWith(cnst_tolarence_round))
        //    {

        //    }
        //}

        public bool IsDataValueStarsWith(string strStarts)
        {
            if (StepData == null) return false;
            if (StepData.DATA_VALUE == null) return false;
            return StepData.DATA_VALUE.ToUpper().StartsWith(strStarts);
        }


        public bool ISDataNeedRefresh()
        {
            if (StepData == null) return false;
            if (StepData.DATA_VALUE == null) return false;
            if ((string.Compare(this.Keyword, SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREVALUE, true) == 0)
                || (string.Compare(this.Keyword, SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREANDCOMPARE, true) == 0)
                || (string.Compare(this.Keyword, SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREANDCOMPAREBYKEY, true) == 0)
                )
            {
                return false;
            }
            return StepData.DATA_VALUE.ToUpper().StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL) ||
                StepData.DATA_VALUE.ToUpper().StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL) ||
                StepData.DATA_VALUE.ToUpper().StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_MODAL) ||
                StepData.DATA_VALUE.ToUpper().StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ)
                ;

        }

        

        //public bool FixDataForVariable(ref string strError)
        //{
        //    string strVar = "" ;
        //    B_SYSTEM_LOOKUP varOp = new B_SYSTEM_LOOKUP();
        //    List<string> lstVarIdx = new List<string>();
        //    Dictionary<string, string> dicVarInfo = new Dictionary<string, string>();
        //    if (ISDataNeedRefresh()&&(StepData.DATA_VALUE.ToUpper().StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL)))
        //    {
        //        strVar = StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL.Length + 1);
        //        lstVarIdx.Add(strVar);
        //        if (!BoHelper.GetGlobalVariableInfo(lstVarIdx, ref strError, ref dicVarInfo))
        //        {
        //            return false;
        //        }
        //        if (dicVarInfo.Keys.Contains(strVar))
        //        {
        //            this.StepData.DATA_VALUE = (string)dicVarInfo[strVar];
        //            return true;
        //        }
        //        else
        //        {
        //            strError = string.Format("can't find Var:[{0}]", strVar );
        //            return false;
        //        }                
        //    }
        //    if (ISDataNeedRefresh() && (StepData.DATA_VALUE.ToUpper().StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_MODAL)))
        //    {
        //        strVar = StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_MODAL.Length + 1);
        //        lstVarIdx.Add(strVar);
        //        if (!BoHelper.GetModalVariableInfo
        //    }
        //}

        public int TestStepId
        {
            get
            {
                if (StepsFromDB == null) return -1;
                return (int)StepsFromDB.STEPS_ID;
            }
        }

        public string Row_Column
        {
            get
            {
                if (StepsFromDB == null) return "";
                return StepsFromDB.COLUMN_ROW_SETTING;
            }
            set
            {
                if (StepsFromDB == null) return;
                StepsFromDB.COLUMN_ROW_SETTING = value;
            }
        }

        public int RunId
        {
            get
            {
                if (StepsFromDB == null) return -1;
                return (int)StepsFromDB.RUN_ORDER;
            }
        }

        public string Keyword
        {
            get
            {
                if (StepsFromDB == null) return "";
                return StepsFromDB.KEY_WORD_NAME;
            }
        }
        public long KeywordId
        {
            get
            {
                if (StepsFromDB == null) return -1;
                return StepsFromDB.KEY_WORD_ID;
            }
        }

        public string ObjectName
        {
            get
            {
                if (StepsFromDB == null) return "";
                return StepsFromDB.OBJECT_HAPPY_NAME;
            }
        }

        public long Object_Name_Id
        {
            get
            {
                if (StepsFromDB == null) return -1;
                return StepsFromDB.OBJECT_NAME_ID ?? -1;
            }
        }
        public string Comment
        {
            get
            {
                if (StepsFromDB == null) return "";
                return StepsFromDB.COMMENTINFO;
            }
            set
            {

            }
        }
    }

    class ExecutableTestCaseMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ExecutableTestCaseMgr));

        public string currentDBIdx = MarsEntitiesExtends.cnst_default_dbName;

        public static void NotifyWebAppOneTestCaseIsDone(bool isOk, string strError, long? storyboardDetailId)
        {
            Logger.logBegin("NotifyWebAppOneTestCaseIsDone", $"isOk [{isOk}] [{strError}]");
#if _forWebClient
            (new Thread(new ThreadStart(async () => {
                try
                {
                    strError = isOk ? "SUCESS" : strError;
                    string strDetialId = storyboardDetailId == null ? "" : storyboardDetailId + "";
                    /// http://localhost:56421/StoryBoard/UpdateStoryboardStatus?sessionId=uuid12175&type=step&isok=False&stepId=1&message=success&storyboardId=1&lSchema=5 

                    string strURL = $"{MarsGlobarVar.MARS_WEB_HOST}&type=testCase&isok={isOk}&stepId={strDetialId}" +
                    $"&message={strError}&storyboardId={MarsGlobarVar.MARS_current_StoryboardId}&lSchema={MarsGlobarVar.MARS_CURRENT_DB}";

                    strURL = Uri.EscapeUriString(strURL);
                    Console.WriteLine(strURL);
                    System.Net.Http.HttpClient clnt = new System.Net.Http.HttpClient();
                    var rspn = await clnt.GetAsync(strURL);
                    Logger.Info("NotifyWebapplicationTestStepsIsDone", $"{strURL} returns {rspn.StatusCode}");
                    Console.WriteLine($"{strURL} return {rspn.StatusCode}");
                }
                catch (Exception e)
                {
                    Logger.Error("NotifyWebapplicationTestStepsIsDone", e.Message, e.StackTrace);
                }
            })))
                .Start();
#endif
        }

        public static void NotifyWebApplicationTestStoryBoardDetailIsDone(string strURLSRC)
        {
            int iMark = new Random().Next();
            Logger.logBegin($"{iMark}|NotifyWebApplicationTestStoryBoardDetailIsDone", strURLSRC);
            try
            {
#if _forWebClient
                string strURL = Uri.EscapeUriString(strURLSRC);
                Logger.Info($"{iMark}|NotifyWebApplicationTestStoryBoardDetailIsDone", strURL);
                System.Net.Http.HttpClient clnt = new System.Net.Http.HttpClient();
                var rspn = clnt.GetAsync(strURL).GetAwaiter().GetResult();
                Logger.Info("NotifyWebapplicationTestStepsIsDone", $"{strURL} returns {rspn.StatusCode}");
                Console.WriteLine($"{strURL} return {rspn.StatusCode}");
#endif
            }
            catch (Exception e)
            {
                Logger.Error($"{iMark}|NotifyWebApplicationTestStoryBoardDetailIsDone",e.Message,e);
            }
            finally
            {
#if _forWebClient

#endif
                Logger.logEnd($"{iMark}|NotifyWebApplicationTestStoryBoardDetailIsDone");
            }
        }

        public static void NotifyWebapplicationTestStepsIsDone(bool isOk, string strError, long stpId)
        {
            Logger.logBegin("NotifyWebapplicationTestStepsIsDone", $"isOk [{isOk}] [{strError}]");
#if _forWebClient
            (new Thread(new ThreadStart(async () => {
                try
                {
                    string strURL = $"{MarsGlobarVar.MARS_WEB_HOST}&type=step&isok={isOk}&stepId={stpId}&message={strError}" +
                    $"&storyboardId={MarsGlobarVar.MARS_current_StoryboardId}&lSchema={MarsGlobarVar.MARS_CURRENT_DB}";
                    strURL = Uri.EscapeUriString(strURL);
                    Console.WriteLine(strURL);
                    System.Net.Http.HttpClient clnt = new System.Net.Http.HttpClient();
                    var rspn = await clnt.GetAsync(strURL);
                    Logger.Info("NotifyWebapplicationTestStepsIsDone", $"{strURL} returns {rspn.StatusCode}") ;
                    Console.WriteLine($"{strURL} return {rspn.StatusCode}");
                }
                catch (Exception e)
                {
                    Logger.Error("NotifyWebapplicationTestStepsIsDone", e.Message, e.StackTrace);
                }
            })))
                .Start();
#endif
        }

        #region cached objects
        /// <summary>
        /// testcaseid, application id, list test stps
        /// </summary>
        private static Dictionary<long, Dictionary<long, List<ExecutableTestCaseStep>>> cachedTestCase = new Dictionary<long, Dictionary<long, List<ExecutableTestCaseStep>>>();
        public static Dictionary<long, Dictionary<long, List<ExecutableTestCaseStep>>> CachedTestCase
        {
            get
            {
                return cachedTestCase;
            }
        }
        #endregion

        private long currentTestCaseId;
        public long CurrentTestCaseId
        {
            get
            {
                return currentTestCaseId;
            }
            set
            {
                currentTestCaseId = value;
            }
        }

        private long currentDataSetId;
        public long CurrentDatasetId
        {
            get
            {
                return currentDataSetId;
            }
            set
            {
                currentDataSetId = value;
            }
        }

        public AutoErrorCheck AutoCheckErrorSet { get; internal set; }

        private bool IsLoopModeForTestCase(List<ExecutableTestCaseStep> lstTestStps, out List<ExecutableTestCaseStep> loopStp)
        {
            if (lstTestStps == null)
            {
                loopStp = null;
                return false;
            }
            var firstLoop = lstTestStps.FirstOrDefault(p => string.Compare("loop", p == null ? "" : p.Keyword, true) == 0);
            if (firstLoop == null)
            {
                loopStp = null;
                return false;
            }
            var endLoop = lstTestStps.FirstOrDefault(p => p.RunId > firstLoop.RunId && "endLoop".Equals(p.Keyword, StringComparison.OrdinalIgnoreCase));
            if (endLoop == null)
            {
                loopStp = null;
                return false;
            }
            loopStp = lstTestStps.Where(p => (p.RunId >= firstLoop.RunId) && (p.RunId <= endLoop.RunId)).ToList();
            return loopStp != null;
        }


        ///¾Ö²¿×ƒÁ¿î„e±í
        ///¸Ã¶ÔÏó°üº¬ËùÓÐµÄloop¶ÔÏó
        private Dictionary<string, List<string>> loopVarsWithValues = new Dictionary<string, List<string>>();

        private string GetLoopVarNameIdx(string strLoopDataForStep)
        {
            return strLoopDataForStep == null ? null : strLoopDataForStep.Substring(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP.Length + 1);
        }

        private bool IsIgnoreComment(string strCmmnt)
        {
            if (string.IsNullOrEmpty(strCmmnt)) return false;
            if (strCmmnt.IndexOf(MarsConstants.CNST_COMMENT_IGNORE_ERROR, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strSrcLoopData"></param>
        /// <param name="iCount"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <returns></returns>
        private bool checkedLoopAsNumberData(string strSrcLoopData, ref int iCount, ref string strError, ref string strAdv,ref string strStack, string strPara="AsNumber")
        {
            Logger.logBegin("checkedLoopAsNumberData", $"data|{strSrcLoopData}");
            bool isOk = true;
            try
            {
                NonCaptureParaMgr paraCheck = new NonCaptureParaMgr();
                string strVariableName = "", strIdx = "";
                isOk = paraCheck.dealWithPrefixPara(strSrcLoopData, ref strIdx, ref strVariableName);
                if (!isOk)
                {
                    strAdv = "Please change the format of data like 'FromMem:A_Variable_From_previous_steps'";
                    strStack = MarsErrorStacks.StackTraceDump(new StackTrace(true));
                    Logger.Error("checkedLoopAsNumberData", strError);
                    return false;
                }
                /// 从memory的对象中找数据
                /// 
                string strValue = "";
                isOk = CaptureParaMgr.GetVariableByIdx(strVariableName, ref strValue, ref strError);
                if (!isOk)
                {
                    strAdv = $"Please make sure that the variable |{strVariableName}| has been set by previous test steps";
                    strStack = MarsErrorStacks.StackTraceDump(new StackTrace(true));
                    Logger.Error("checkedLoopAsNumberData", strError);
                    return false;
                }
                /// strValue should be a int
                /// 
                strValue = strValue ?? "";
                isOk = int.TryParse(strValue, out iCount);
                if (!isOk)
                {
                    strAdv = $"Please make sure that the variable |{strVariableName}| has been set as an int by previous test steps";
                    strStack = MarsErrorStacks.StackTraceDump(new StackTrace(true));
                    Logger.Error("checkedLoopAsNumberData", strError);
                    return false;
                }
                return true;
            }catch(Exception e)
            {
                strAdv = $"Please check the data setting, format should like {NonCaptureParaMgr.cnst_FromMem}, \r\nand make sure that the variable has been set already until the step is executed.";
                strStack = MarsErrorStacks.StackTraceDump(new StackTrace(true));
                Logger.Error("checkedLoopAsNumberData", strError);
                iCount = -1;
                return false;
            }
            finally
            {
                Logger.logEnd("checkedLoopAsNumberData", $"returns|{isOk}|{iCount}");
            }
        }
        /// <summary>
        /// 获取当前的step是在哪个循环中，主循环还是子循环
        /// </summary>
        /// <param name="iRunOrd"></param>
        /// <returns></returns>
        private MarsLoopRunJump getCurrentLoopRunJump(int iRunOrd, MarsLoopRunJump mainLoopJump, MarsLoopRunJump subLoopJump)
        {
            Logger.logBegin("getCurrentLoopRunJump", $"current Runord|{iRunOrd}");
            try
            {
                if (mainLoopJump == null) return null;
                if (subLoopJump == null)
                {
                    if ((iRunOrd > mainLoopJump.loopBeginRunOrder) && (iRunOrd <= mainLoopJump.loopEndRunOrder)) return mainLoopJump;
                    ///说明该step不在循环中
                    return null;
                }
                //说明在循环外
                if ((iRunOrd < mainLoopJump.loopBeginRunOrder)||(iRunOrd>mainLoopJump.loopEndRunOrder)) return null;
                /// 是否在子循环中
                /// 
                if ((iRunOrd > subLoopJump.loopBeginRunOrder) && (iRunOrd <= subLoopJump.loopEndRunOrder)) return subLoopJump;
                return mainLoopJump;
            }
            finally
            {
                Logger.logEnd("getCurrentLoopRunJump");
            }
        }


        /// <summary>
        /// 处理循环中需要的数据，循环过程中需要从循环变量中获取数据
        /// 算法：
        /// 1， 判断是否需要从内存变量中获取
        /// 2， 如果需要，
        /// </summary>
        /// <param name="strSrcLoopData"></param>
        /// <param name="stepsData"></param>
        /// <param name="loopJump"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private bool dealWithLoopVars(int stepRunOrd, string strSrcLoopData, TEST_DATA_SETTINGDTO stepsData, MarsLoopRunJump mainloopJump,
            MarsLoopRunJump subLoopJump,
            ref string strError, ref string strAdv) 
        {
            if (string.IsNullOrEmpty(strSrcLoopData)) return true;
            /// 判断是否从variable中来
            /// 
            var currentLoopJump = getCurrentLoopRunJump(stepRunOrd, mainloopJump, subLoopJump);
            if (currentLoopJump == null)
            {
                // 说明不在循环内
                Logger.Error("dealWithLoopVars", strError = $"current step order |{stepRunOrd}| doesn't belong to any Loop");
                strAdv = "Please check test step and ensure that the step belongs to a loop, or change data cell";
                return true;
            }

            string strDataNoPreFix = "",strVarIdx ="",strVarCmmd ="";
            
            if (currentLoopJump.CurrentIdx >= 1) return true; //说明循环已经开始
            bool isFrmVar = MarsVarDataPrefix.IsVariableFormat(strSrcLoopData, ref strDataNoPreFix, ref strVarIdx, ref strVarCmmd);
            /// 如果不是循环变量，直接将指针移动到下一个数据，需要判断是移动主循环的还是子循环的
            if (!isFrmVar)
            {
                currentLoopJump.MoveDataIdxToAvailable();
                return true;
            }
            
            /// 从对象变量中找数据
            /// 
            List<MarsVarBasic> varListsTmp = new List<MarsVarBasic>();
            strError = $"Can't find [{strVarIdx}] from variable table";

            isFrmVar = MarsVarDataPrefix.GetVariable(strVarIdx, strVarCmmd, ref varListsTmp);
            if (!isFrmVar) return false; // 无法在变量中找到指定变量
            List<MarsVarBasic> varLists = new List<MarsVarBasic>();
            varLists.AddRange(varListsTmp);
            if (varLists == null) return false;
            try
            {
                foreach (var itm in varLists)
                {
                    if (itm == null) continue;
                    if (!(itm is MarsStatusVar))
                    {
                        strError = "Only status variable is supported currently";
                        return false;
                    }
                    if (currentLoopJump.loopVars == null)
                    {
                        currentLoopJump.loopVars = new List<MarsVarBasic>();
                    }
                    currentLoopJump.loopVars.Add(itm as MarsStatusVar);
                }
                currentLoopJump.loopVarMode = MarsVar_Type.varT_Sattus;
                currentLoopJump.MoveDataIdxToAvailable();
                //loopJump.currentIdx = 0;
                return true;
            }catch(Exception e)
            {
                Logger.Error("dealWithLoopVars", strError = e.Message, e);
                strAdv = $"Get Exception|{e.Message} Please check test step and ensure that the step belongs to a loop";
                return false;
            }
        }

        private int GetResumeNextStep(List<ExecutableTestCaseStep> lstTestStps, int currentRunId)
        {
            var resumeNextStp = (from stp in lstTestStps
                     where string.Compare(stp.Keyword, "ResumeNext", true) == 0
                     select stp).FirstOrDefault();
            if (resumeNextStp == null) return int.MinValue;

            if ((resumeNextStp != null) && (resumeNextStp.StepData != null))
            {
                if (resumeNextStp.StepData.DATA_DIRECTION != null)
                {
                    if (resumeNextStp.StepData.DATA_DIRECTION == 4)
                        MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder = -1;
                }
                else if ((resumeNextStp.StepData.DATA_VALUE != null) && (resumeNextStp.StepData.DATA_VALUE.Equals("skip", StringComparison.OrdinalIgnoreCase)))
                    MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder = -1;
            }

            return resumeNextStp.RunId;
        }

       /// <summary>
       /// time:09-03, 
       /// topic: add nagetive test
       /// desc: 1, for nagetive testing, there are 3 cases, if the test case under nagetive testing
       ///         i.  clickMenuIcon, if the menu is not availe, then that is right
       ///         ii, check the title of special panel or label, some special strings exists
       ///         iii,checkerror should return true
       ///       2, paramter of each test step will indicate the current mode.
       /// Date: 11-25-24
       /// Topic: add sub-loop for a workaround of nested loop
       /// </summary>
       /// <param name="mntrCnt"></param>
       /// <param name="lAppId"></param>
       /// <param name="lTestcaseId"></param>
       /// <param name="testStepInfo"></param>
       /// <param name="lstNxtTestStps"></param>
       /// <param name="isIgnoreError"></param>
       /// <param name="strMode"></param>
       /// <param name="recodeMgr"></param>
       /// <param name="appTyp"></param>
       /// <param name="strError"></param>
       /// <param name="strAdv"></param>
       /// <param name="strStack"></param>
       /// <param name="strLastPeg"></param>
       /// <param name="strLastObjectName"></param>
       /// <param name="strLastTestStep"></param>
       /// <param name="iLastStepNum"></param>
       /// <param name="hasError"></param>
       /// <param name="isVerifyValueSkipper"></param>
       /// <param name="isNotRequiredToWriteBackToDB"></param>
       /// <param name="teststepDoneCallBack"></param>
       /// <param name="teststepsListDoneCallBack"></param>
       /// <returns></returns>

        internal bool RunTestStepOneByOne(string strCurMarsAccount,
            ref IMonitorService mntrCnt,
            ref long lAppId,
            long lTestcaseId,
            MARSRecoverMgr testStepInfo, //List<ExecutableTestCaseStep> lstTestStps,
            List<ExecutableTestCaseStep> lstNxtTestStps,
            bool isIgnoreError,
            string strMode,
            StoryboardDBRecordMgr recodeMgr,
            ref Mars_applicationTyp.MARS_APPTYPE appTyp,
            ref string strError,
            ref string strAdv,
            ref string strStack,
            ref string strLastPeg,
            ref string strLastObjectName,
            ref string strLastTestStep,
            ref int iLastStepNum,
            ref bool hasError,
            bool isVerifyValueSkipper = false,
            bool isNotRequiredToWriteBackToDB = false,
            OnTestStepExectionDoneEvent teststepDoneCallBack = null,
            OnTestcaseIsDoneEvent teststepsListDoneCallBack = null
            )
        {
            Logger.logBegin("RunTestStepOneByOne", string.Format("verifyvalue skipper:[{0}], strMode:[{1}]", isVerifyValueSkipper, strMode));

            bool isOk = false;
            if (testStepInfo == null)
            {
                Logger.Error("RunTestStepOneByOne", "No test steps info is passed, just return true");
                Logger.logEnd("RunTestStepOneByOne");
                return true;
            }
            
            var lstTestStps = testStepInfo.currentSteps;
            Logger.Info("\t", lstTestStps == null? $"total:null steps":$"total:[{lstTestStps.Count}] steps");
            if (lstTestStps == null) return true;
            if (lstTestStps.Count <= 0) return true;

            string strWriteBackName = "";
            // 当前步骤错误时截取的全屏或窗口截图路径
            string strFullScreenSnapshotPath = null;
            //int iResumeNextRunOrder = -1; /// 如果该变量大于-1表示存在resumenext
            MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder = -1;
            #region get resumeNext id
            //var resumeNextStp = (from stp in lstTestStps
            //                     where string.Compare(stp.Keyword, "ResumeNext", true) == 0
            //                     select stp).FirstOrDefault();

            //MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder = resumeNextStp == null ? int.MinValue : resumeNextStp.RunId;
            //if ((resumeNextStp != null) && (resumeNextStp.StepData != null))
            //{
            //    if (resumeNextStp.StepData.DATA_DIRECTION != null)
            //    {
            //        if (resumeNextStp.StepData.DATA_DIRECTION == 4)
            //            MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder = -1;
            //    }
            //    else if ((resumeNextStp.StepData.DATA_VALUE != null) && (resumeNextStp.StepData.DATA_VALUE.Equals("skip", StringComparison.OrdinalIgnoreCase)))
            //        MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder = -1;
            //}
            #endregion
            MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder = GetResumeNextStep(lstTestStps, -1);
            
            bool hadErrors = false;
            List<MarsIfElseRunJump> ifelseSkipStepsId = new List<MarsIfElseRunJump>();
            //Stack<int> ifelseSkipIdsStack = new Stack<int>();
            /**
             * 2019-07-06 增加 loop 处理
             * 处理思路：
             * 1、先判断是否有 loop 关键字
             * 2、如果有，获取 loop 变量列表，然后构建一个循环，设置当前的对象
             * 3、...
             */
            List<string> lstLoopVar = new List<string>(),
               lstSubLoopVar = new List<string>();
            List<ExecutableTestCaseStep> loopStp = null;
            List<ExecutableTestCaseStep> subLoopStp= null;
            bool isLoopMode = IsLoopModeForTestCase(lstTestStps, out loopStp); //´Óloopµ½endloop
            MarsLoopRunJump loopJump = new MarsLoopRunJump(),
                subLoopJump = new MarsLoopRunJump();
            MarsEndLoop loopendObj = new MarsEndLoop(-1), 
                subLoopEndObj = new MarsEndLoop(-1);

            //将数据发送到 monitor
            List<TestStep4Services> lstWcfData = new List<TestStep4Services>();
            foreach (var itm in lstTestStps)
            {
                lstWcfData.Add(ExecutableTestCaseStep.convertToWcfData(itm));
            }
            if (mntrCnt != null)
                mntrCnt.OnClientTestCaseListChangeEvent(lstWcfData);

            int iLoopCnt = 1, iCurLoop = 0;
            string strCurLoopVar = "";  /// 该值仅在loop或者endloop时候变化
            string strCurSubLoopVar = ""; /// 该值仅在sub-loop或者endsubloop时候变化
            bool isdotNetFrameworkEngineRequired = false;

                if (isLoopMode)
                {
                    // 获取该 step 的数据
                    Logger.Info("\t", string.Format("try to get loop var:[{0}]", loopStp[0].StepData));
                    string strData = loopStp[0].StepData == null ? null : loopStp[0].StepData.DATA_VALUE; // 有可能 loopData 为空，为即时模式
                string strLoopValue = "";
                loopJump.loopBeginRunOrder = loopStp[0].RunId;
                loopJump.loopEndRunOrder   = loopStp[loopStp.Count - 1].RunId;
                string strLoopDataWithoutPrefix = "";
                if ((!string.IsNullOrEmpty(strData)) && (strData.ToUpper().StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP)))
                {
                    strLoopDataWithoutPrefix = strData.Substring(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP.Length + 1);
                    if (!string.IsNullOrEmpty(strLoopDataWithoutPrefix))
                    {
                        if (BoHelper.GetLoopVaribleInfo(strLoopDataWithoutPrefix, ref strError, ref strLoopValue, recodeMgr.getCurrentDBIdx()))
                        {
                            strLoopValue = strLoopValue ?? "";
                            string[] arrLoopVar = strLoopValue.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            lstLoopVar.AddRange(arrLoopVar);
                            //iLoopCnt = lstLoopVar.Count;

                            //同时将loopvar添加到变量表中
                            loopVarsWithValues.Add(strLoopDataWithoutPrefix, lstLoopVar);

                            loopJump.CurrentIdx = 0;
                            loopJump.loopStrings = lstLoopVar.ToArray();
                        }
                    }
                }
                /// 此时还没有正式运行，如果变量是全局的，可能这里找不到数据，因为是第一个 test case
                /// 因此，需要等到 Loop 运行时再去获取数据

            }
            //hasError = false;
            string strFirstError = "";//保留第一次出现问题的错误信息

            try
            {
                /// 如果是重运行模式，需要首先找到重新启动的 peg
                /// 运行 peg，然后再进行 test step 处理
                /// 
                if (testStepInfo != null)
                {
                    testStepInfo.GetPegwindowForRestart();
                }

                /// ask engine to load latest mapping file
                /// added on 4-5-2023
                /// 
                if (appTyp != Mars_applicationTyp.MARS_APPTYPE.WEB_IE)
                    KeywordOpForGUI.RefreshDefaultKeywordTypeMapping();

                //string strRslt = "";
                for (iCurLoop = 0; iCurLoop < iLoopCnt; iCurLoop++)
                {
                    testStepInfo.loopId = iCurLoop;

                    loopVarsWithValues.Clear();//
                    if (lstLoopVar.Count > iCurLoop)
                    {
                        strCurLoopVar = lstLoopVar[iCurLoop];
                    }

                    if (mntrCnt != null)
                    {
                        mntrCnt.OnCurrentLoopChangeEvent(iCurLoop);
                    }
                    int iCurrentTestStepIdx = 0;
                    bool isLoopJump = false, // 判断loop是否结束
                        isSubLoopJump = false;
                    while (iCurrentTestStepIdx < lstTestStps.Count)                    
                    {
                        try
                        {
                            var itmStp = lstTestStps[iCurrentTestStepIdx++];
                            testStepInfo.currentStep = itmStp;
                            if (itmStp == null) continue;

                            #region check parameter mode
                            MARSExtPara currentParaMode = MARSExtPara.checkParaType(itmStp.Row_Column);
                            itmStp.Row_Column = currentParaMode.paraAfterExtract;
                            #endregion // check parameter mode

                            bool isSkipFromRestore = false;
                            if (testStepInfo.isRestoreMode)
                            {
                                if (testStepInfo.latestPegwindow != null)
                                {
                                    if (itmStp.RunId < testStepInfo.latestPegwindow.RunId)
                                    {
                                        isSkipFromRestore = true;
                                    }
                                    else
                                    {
                                        //需要运行pegwindow
                                        if ((itmStp.RunId > testStepInfo.latestPegwindow.RunId) && (itmStp.RunId < testStepInfo.restoredFrom.RunId))
                                            isSkipFromRestore = true;
                                    }
                                }
                                if (isSkipFromRestore) {
                                    if (mntrCnt != null)
                                    {
                                        mntrCnt.BeforeClientRunTestStepEvent(ExecutableTestCaseStep.convertToWcfData(itmStp));
                                        mntrCnt.SkipCurrentStep();
                                        isSkipFromRestore = false;                                        
                                    }
                                    continue;//因为上次已经运行过，因此，重复运行时不做记录
                                }
                            }
                            ///
                            Logger.Info("RunTestStepOneByOne", string.Format("keyword:[{0}]-obj:[{1}]-runOrd:{2}", itmStp.Keyword, itmStp.ObjectName, itmStp.RunId));
                            if (mntrCnt != null)
                            {
                                if (wcfClient.WcfClientAgent.IsWcfOffLine())
                                {
                                    wcfClient.WcfClientAgent.ReconnectTo();
                                    mntrCnt = wcfClient.WcfClientAgent.MonitorWcfClient;
                                }
                                mntrCnt.BeforeClientRunTestStepEvent(ExecutableTestCaseStep.convertToWcfData(itmStp));
                            }

                            iLastStepNum = itmStp.RunId;
                            strLastObjectName = itmStp.ObjectName;

                            if (string.Compare("pegwindow", itmStp.Keyword ?? "", true) == 0)
                            {
                                strLastPeg = itmStp.Keyword;
                                Logger.Info("RunTestStepOneByOne", "find pegwindow");
                                ///在wpf core模式中，有多个进程组， 2025,3.21 需要对进程重新处理
                                ///
                                if ((itmStp.StepsFromDB != null) 
                                    && (!string.IsNullOrEmpty(itmStp.StepsFromDB.COLUMN_ROW_SETTING)) 
                                    && (itmStp.StepsFromDB.COLUMN_ROW_SETTING.IndexOf(MarsKeywordBase.cnst_pegwindow_para_reHost) >= 0))
                                {
                                    int iWaitSeconds = MarsKeywordBase.GetWaitForSecondsForRehostToApp(itmStp.StepsFromDB.COLUMN_ROW_SETTING, ref isOk);
                                    if (!isOk)
                                    {
                                        strError = $"Format for parameter|{MarsKeywordBase.cnst_pegwindow_para_reHost} is |{MarsKeywordBase.cnst_pegwindow_para_reHost}:number| or {MarsKeywordBase.cnst_pegwindow_para_reHost}";
                                        strAdv = "Please ensure the test step setting is right";
                                        Logger.Error("RunTestStepOneByOne", strError);
                                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", strLastTestStep,
                                            strAdv, "N/A", strStack,
                                            StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                                        return false;
                                    }
                                    else {
                                        Logger.Info("RunTestStepOneByOne", $"pegwindow|find wait for seconds|{iWaitSeconds}");
                                    }
                                    isOk = ReHostToApp(strCurMarsAccount, itmStp.StepsFromDB,appTyp, 
                                        KeyWordsOPForNonGUI.CurrentApplicationStartPath,ref strError, ref strAdv, ref strStack, iWaitSeconds);
                                    if (!isOk)
                                    {
                                        Logger.Error("RunTestStepOneByOne", strError);
                                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", strLastTestStep,
                                            strAdv, "N/A", strStack,
                                            StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                                        return false;
                                    }
                                }
                            }

                            if (hadErrors && (itmStp.RunId <= MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder))
                            {
                                Logger.Info("RunTestStepOneByOne", string.Format("Errors but resumen next exists on step no.[{1}] Current Step Number:[{0}]", itmStp.RunId,
                                    MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder));
                                if (mntrCnt != null)
                                {
                                    //if (mntrCnt.)
                                    mntrCnt.SkipCurrentStep();
                                }
                                
                                //如果到
                                if (itmStp.RunId== MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder)
                                {
                                    int nxtResumeNxtOrd = GetResumeNextStep(lstTestStps, MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder);
                                    Logger.Info("\t", $"try to get next resumenext,returns [{nxtResumeNxtOrd}]");
                                    if (nxtResumeNxtOrd >= 0)
                                    {
                                        ///说明找到
                                        ///
                                        MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder = nxtResumeNxtOrd;
                                        MarsGlobalStatusMgr.resumeNextStatus.hasExceptionsPrevious = true;
                                        MarsGlobalStatusMgr.resumeNextStatus.statusId = 1;
                                    }
                                }
                                continue; // just ignore steps before resume next
                            }

                            if (itmStp.StepObject == null)
                            {
                                itmStp.StepObject = new B_V_OBJECT_SNAPSHOT();
                                //if (string.IsNullOrEmpty(itmStp.StepsFromDB.QUICK_ACCESS))
                            }
                            //for log
                            if (ifelseSkipStepsId != null)
                            {
                                foreach (var itmskip in ifelseSkipStepsId)
                                {
                                    if (itmskip == null) continue;
                                    Logger.Info("FindIF", string.Format("run ord:[0] keyword:[{1}] isskip:[{2}]", itmskip.stepId, itmskip.KeywordName, itmskip.isSkip));
                                }
                            }
                            if (IsIfElseJumpSkip((int)itmStp.StepsFromDB.RUN_ORDER, ifelseSkipStepsId))
                            {
                                Logger.Info("RunTestStepOneByOne", string.Format("Skip IF-Else-IfEnd, run_order [{0}]", itmStp.RunId));
                                if (mntrCnt != null)
                                {
                                    mntrCnt.SkipCurrentStep();
                                }
                                continue;
                            }

                            TestStepRunningRecorder stpRecor = new TestStepRunningRecorder()
                            {
                                assignedStepId = itmStp.TestStepId,
                                EndTime = DateTime.Now,
                                CauseReason = "",
                                LoopId = 0,
                                RunResult = 1,
                                StartTime = DateTime.Now
                            };

                            if ((itmStp.StepData != null) && ((itmStp.StepData.DATA_DIRECTION == 4) || (string.Compare("skip", itmStp.StepData.DATA_VALUE, true) == 0)))
                            {
                                strLastTestStep = $"{itmStp.Keyword}({itmStp.ObjectName},{itmStp.Row_Column ?? "N/A"},[{itmStp.StepData.DATA_VALUE} <= {"SKIP"}])";
                                //skipped, continue ;
                                if (teststepDoneCallBack != null)
                                {
                                    teststepDoneCallBack(itmStp, true, null, "SKIPPED");
                                }

                                continue;
                            }
                            if (!isdotNetFrameworkEngineRequired)
                            {
                                isdotNetFrameworkEngineRequired = CheckTestStepRequiresNewFrameEngineFromThenOn(itmStp);
                            }
                            if (isdotNetFrameworkEngineRequired)
                            {
                                isOk = LaunchDotNetFrameworkEngine(ref strError, ref strAdv, ref strStack);
                                if (!isOk) return false;
                                
                            }

                            if (!isNotRequiredToWriteBackToDB)
                            {
                                if (recodeMgr.CreateStepLog(itmStp.TestStepId, stpRecor, itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE) < 0)
                                {
                                    strError = "Can't create Test step Log Record.";
                                    strAdv = "";
                                    strStack = MarsErrorStacks.StackTraceDump(new StackTrace(true));// $"at File:{stck.GetFileName()}, {stck.GetMethod()} {stck.GetFileLineNumber()}";
                                    Logger.Info("\t", strStack);
                                    Console.WriteLine(strStack);
                                    strLastTestStep = $"{itmStp.Keyword}({itmStp.ObjectName},{itmStp.Row_Column ?? "N/A"},[{itmStp.StepData.DATA_VALUE}])";
                                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", strLastTestStep, 
                                        strAdv, "N/A", strStack,
                                        StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                                    if (teststepDoneCallBack != null)
                                    {
                                        teststepDoneCallBack(itmStp, false, null, strError);
                                    }
                                    return false;
                                }
                            }
                            //}
                            string strDataReturned = "";
                            string strVarType = "";
                            bool isVar = false,
                                isSkpped = false,
                                isDataSourceChanged;
                            isDataSourceChanged = false;

                            #region if-else-ifend

                            if (string.Compare("if", itmStp.Keyword, true) == 0)
                            {
                                Logger.Info("FindIF", itmStp.StepsFromDB.RUN_ORDER + " AND CLEAN PREVIOUS IF STACK");
                                ifelseSkipStepsId = new List<MarsIfElseRunJump>();

                                MarsIfElseRunJump ifJump = null, elseJump = null, ifEndJump = null;
                                ifelseSkipStepsId.Add(ifJump = new MarsIfElseRunJump()
                                {
                                    KeywordName = "if",
                                    stepId = (int)itmStp.StepsFromDB.RUN_ORDER,
                                    isSkip = false
                                });
                                //build if run structure
                                var marselse = (from q in lstTestStps
                                                where q.RunId >= itmStp.StepsFromDB.RUN_ORDER
                                                && string.Compare("else", q.Keyword, true) == 0
                                                select q)
                                                .FirstOrDefault();

                                if (marselse != null)
                                {
                                    ifelseSkipStepsId.Add(elseJump = new MarsIfElseRunJump()
                                    {
                                        KeywordName = marselse.Keyword,
                                        stepId = (int)marselse.StepsFromDB.RUN_ORDER,
                                        isSkip = null
                                    });
                                    ifJump.endStepsId = (int)marselse.StepsFromDB.RUN_ORDER - 1;
                                }
                                Logger.Info("FindIF", string.Format("find else at:[{0}]", marselse == null ? "N/A" : marselse.StepsFromDB.RUN_ORDER + ""));
                                var marsIfEnd = (from q in lstTestStps
                                                 where q.RunId >= itmStp.StepsFromDB.RUN_ORDER
                                                 && string.Compare("ifend", q.Keyword, true) == 0
                                                 select q)
                                                .FirstOrDefault();
                                if (marsIfEnd == null)
                                {
                                    strError = "IF keyword structure is wrong. Each If Keyword needs a paired IFEnd";
                                    recodeMgr.UpdateCurrentTestStepResult(1, itmStp.TestStepId, 2, string.Format("{0} {1}", string.Format("{0} {1}", strError, strDataReturned), strDataReturned), "", null);
                                    if (mntrCnt != null)
                                    {
                                        mntrCnt.AfterClientRunTestStepEvent("", 0, strError);
                                    }
                                    if (teststepDoneCallBack != null)
                                    {
                                        teststepDoneCallBack(itmStp, false, null, strError);
                                    }
                                    return false;
                                }
                                else
                                {
                                    Logger.Info("FindIF", string.Format("find ifend at:[{0}]", marsIfEnd == null ? "N/A" : marsIfEnd.StepsFromDB.RUN_ORDER + ""));
                                    ifelseSkipStepsId.Add(ifEndJump = new MarsIfElseRunJump()
                                    {
                                        KeywordName = marsIfEnd.Keyword,
                                        stepId = (int)marsIfEnd.StepsFromDB.RUN_ORDER,
                                        endStepsId = (int)marsIfEnd.StepsFromDB.RUN_ORDER,
                                        isSkip = true
                                    });
                                    if (elseJump == null)
                                    {
                                        ifJump.endStepsId = (int)marsIfEnd.StepsFromDB.RUN_ORDER;
                                    }
                                    else
                                    {
                                        elseJump.endStepsId = (int)marsIfEnd.StepsFromDB.RUN_ORDER;
                                        ifJump.endStepsId = elseJump.stepId;
                                    }
                                }
                            }

                            if (string.Compare("ifend", itmStp.Keyword, true) == 0)
                            {
                                ifelseSkipStepsId.Clear();
                            }
                            #endregion //if-else-ifend
                            ///判断是stepData是否有前缀
                            /// 
                            string strOrgLoopData = ""; //原始的loop设置数据                                                        
                            //int loop_idx = 0;
                            /// loop或者是sub-loop                            
                            if ((string.Compare("loop", itmStp.Keyword, true) == 0)
                                ||(SystemConstant.CNST_RESERVED_KEYWORD_SUBLOOP.Equals(itmStp.Keyword, StringComparison.OrdinalIgnoreCase)))
                            {
                                Logger.Info("RunTestStepOneByOne", $"loop|subloop data|{itmStp.Keyword}|[{itmStp.StepData}]");
                                bool isSubLoop = SystemConstant.CNST_RESERVED_KEYWORD_SUBLOOP.Equals(itmStp.Keyword, StringComparison.OrdinalIgnoreCase);
                                MarsLoopRunJump tmpLoopJump = isSubLoop ? subLoopJump : loopJump;

                                /// 首先判断是否使用变量
                                /// 
                                if (!string.IsNullOrEmpty(itmStp.DATA_VALUE))
                                {
                                    string strVarIdx = "", dataNoPrefix = "", varCmmd = "", strVarTyp = "NoRequired";
                                    List<MarsVarBasic> dataFromVariableTable = new List<MarsVarBasic>();
                                    isVar = MarsVarDataPrefix.IsVariableFormat(itmStp.DATA_VALUE, ref dataNoPrefix, ref strVarIdx, ref varCmmd);
                                    if (isVar)
                                    {
                                        //MarsVariableTable.getVariableDetailByIdx(strVarIdx,)
                                        isOk = MarsVarDataPrefix.GetVariable(strVarIdx, strVarTyp, ref dataFromVariableTable);
                                        if ((isOk)&&(dataFromVariableTable!=null)&&(dataFromVariableTable.Count>=1)&&(dataFromVariableTable.FirstOrDefault() is MarsStatusVar statusVar))
                                        {
                                            tmpLoopJump.loopVarMode = MarsVar_Type.varT_Sattus;
                                            tmpLoopJump.loopVars = dataFromVariableTable;

                                        }
                                    }
                                }

                                if (!isSubLoop)
                                {
                                    lstLoopVar.Clear();
                                    isLoopJump = false;
                                    loopendObj = null;
                                    /// 如果多个loop，非嵌套模式
                                    /// 算法：
                                    /// 1，获取最近的loopend
                                    /// 2, 修正loopJump对象的值
                                    /// 3，重新获取loopvar                           
                                    ///
                                    var lpEnd = lstTestStps.Where(p => (p.RunId > itmStp.RunId) && (p.Keyword != null) && (p.Keyword.Equals("endLoop", StringComparison.OrdinalIgnoreCase)))
                                        .FirstOrDefault();
                                    #region check Loop pair
                                    if (lpEnd == null)
                                    {
                                        strError = $"No [endLoop] keyword is paired with #[{itmStp.RunId}, loop]";
                                        strStack = Environment.StackTrace;
                                        strAdv = "please check test case and make sure loop-endloop are paired";
                                        isOk = false;
                                        if (!isNotRequiredToWriteBackToDB)
                                            recodeMgr.UpdateCurrentTestStepResult(1, itmStp.TestStepId, 2, string.Format("{0} {1}", string.Format("{0} {1}", strError, strDataReturned), strDataReturned), "", null);
                                        if (mntrCnt != null)
                                        {
                                            mntrCnt.AfterClientRunTestStepEvent("", 0, strError);
                                        }

                                        if (teststepDoneCallBack != null)
                                        {
                                            teststepDoneCallBack(itmStp, false, null, strError);
                                        }

                                        return false;
                                    }
                                    #endregion
                                    //loopJump.CurrentIdx = 0;
                                    //loopJump.MoveDataIdxToAvailable();
                                    loopJump.loopBeginRunOrder = itmStp.RunId;
                                    loopJump.loopEndRunOrder = lpEnd.RunId;
                                }
                                else
                                {
                                    lstSubLoopVar.Clear();
                                    isSubLoopJump = false;
                                    subLoopEndObj = null;

                                    var lpSubEnd = lstTestStps.Where(p => (p.RunId > itmStp.RunId) && (p.Keyword != null) 
                                            && (p.Keyword.Equals(SystemConstant.CNST_RESERVED_KEYWORD_ENDSUBLOOP, StringComparison.OrdinalIgnoreCase)))
                                        .FirstOrDefault();
                                    #region check sub Loop pair
                                    if (lpSubEnd == null)
                                    {
                                        strError = $"No |{SystemConstant.CNST_RESERVED_KEYWORD_ENDSUBLOOP}| keyword is paired with #[{itmStp.RunId}|{itmStp.Keyword}|]";
                                        strStack = Environment.StackTrace;
                                        strAdv = "please check test case and make sure subLoop-endSubLoop are paired";
                                        isOk = false;
                                        if (!isNotRequiredToWriteBackToDB)
                                            recodeMgr.UpdateCurrentTestStepResult(1, itmStp.TestStepId, 2, string.Format("{0} {1}", string.Format("{0} {1}", strError, strDataReturned), strDataReturned), "", null);
                                        if (mntrCnt != null)
                                        {
                                            mntrCnt.AfterClientRunTestStepEvent("", 0, strError);
                                        }

                                        if (teststepDoneCallBack != null)
                                        {
                                            teststepDoneCallBack(itmStp, false, null, strError);
                                        }

                                        return false;
                                    }
                                    #endregion
                                    subLoopJump.CurrentIdx = 0;
                                    subLoopJump.loopBeginRunOrder = itmStp.RunId;
                                    subLoopJump.loopEndRunOrder = lpSubEnd.RunId;
                                }
                                var currentLoopJump = isSubLoop ? subLoopJump : loopJump;
                                /// loop的数据来源有两个，一个是loop，另外一个是内存变量表中获取
                                /// 因此首先需要判断是否是内存变量还是loop变量
                                /// 2-14-2022
                                /// 因为是loopkeyword，所以需要在这里初始化strCurLoopVar或者strCurSubLoopVar的值

                                if (itmStp.StepData != null)
                                {
                                    strOrgLoopData = itmStp.StepData.DATA_VALUE;
                                    itmStp.StepData.DATA_VALUE = strCurLoopVar;
                                    //loopJump.currentIdx = 0;
                                    strLastTestStep = $"{itmStp.Keyword}({itmStp.ObjectName},{itmStp.Row_Column ?? "N/A"},[{itmStp.StepData.DATA_VALUE} <<== {strCurLoopVar}])";
                                    /// reload 只能处理LoopVar
                                    if ("reload".Equals(itmStp.Row_Column ?? "", StringComparison.OrdinalIgnoreCase))
                                    {
                                        #region reload part
                                        Logger.logBegin("RunTestStepOneByOne-reload");

                                        //ÖØÐÂ¼ÓÔØ
                                        string strLoopDataWithoutPrefix = string.Empty;
                                        int commaPos = strOrgLoopData.IndexOf(":");
                                        string varCmd = strOrgLoopData.Substring(0,commaPos);
                                        if (commaPos >= 0)
                                        {
                                            strLoopDataWithoutPrefix = strOrgLoopData.Substring(commaPos + 1);
                                        }
                                        
                                        //strOrgLoopData.Substring(SystemConstant.CNST_RESERVED_VARIABLE_FROMLOOP.Length + 1);
                                        if ((!string.IsNullOrEmpty(strLoopDataWithoutPrefix))&&("Loop_Var".Equals(varCmd, StringComparison.OrdinalIgnoreCase)))
                                        {  
                                            /// 只处理loop_var
                                            string strLoopValueTmp = "";
                                            if (BoHelper.GetLoopVaribleInfo(strLoopDataWithoutPrefix, ref strError, ref strLoopValueTmp, recodeMgr.getCurrentDBIdx()))
                                            {
                                                if (!string.IsNullOrEmpty(strLoopValueTmp))
                                                {
                                                    string[] arrLoopVar = strLoopValueTmp.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                                    if (isSubLoop)
                                                    {
                                                        lstSubLoopVar.AddRange(arrLoopVar);
                                                        //iLoopCnt = lstLoopVar.Count;//新模式，该句没有必要

                                                        //同时将loopvar添加到变量表中
                                                        loopVarsWithValues.Add(strLoopDataWithoutPrefix, lstLoopVar);
                                                        subLoopJump.CurrentIdx = 0;
                                                        subLoopJump.loopStrings = lstLoopVar.ToArray();
                                                        strCurSubLoopVar = subLoopJump.GetCurrentStatusLoopVar(ref isOk, ref strError);
                                                        if (!isOk)
                                                        {
                                                            Logger.Error("RunTestStepOneByOne-GetLoopVaribleInfo,subLoop", strError);
                                                            return false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        lstLoopVar.AddRange(arrLoopVar);
                                                        //iLoopCnt = lstLoopVar.Count;//新模式，该句没有必要

                                                        //同时将loopvar添加到变量表中
                                                        loopVarsWithValues.Add(strLoopDataWithoutPrefix, lstLoopVar);
                                                        loopJump.CurrentIdx = 0;
                                                        loopJump.loopStrings = lstLoopVar.ToArray();
                                                        strCurLoopVar = loopJump.GetCurrentStatusLoopVar(ref isOk, ref strError);
                                                        if (!isOk)
                                                        {
                                                            Logger.Error("RunTestStepOneByOne-GetLoopVaribleInfo,loop", strError);
                                                            return false;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // 处理其他类型的参数 和normal 模式一样
                                            //isOk = dealWithLoopVars(itmStp.RunId, strOrgLoopData, itmStp.StepData, loopJump, subLoopJump, ref strError, ref strAdv);
                                            //if (!isOk)
                                            //{
                                            //    Logger.Error("RunTestStepOneByOne", $"dealWithLoopVars Error in reload mode |{strError}|");
                                            //    strAdv = "Contact Marquis";
                                            //    strStack = MarsErrorStacks.StackTraceDump();
                                            //    return false;
                                            //}

                                            if (currentLoopJump.loopVarMode == MarsVar_Type.varT_Sattus)
                                            {
                                                currentLoopJump.MoveDataIdxToAvailable();
                                                if (currentLoopJump.CurrentIdx >= 0)
                                                {
                                                    /// Ö±½ÓÌøµ½loopend
                                                    /// 
                                                    strCurLoopVar = currentLoopJump.GetCurrentStatusLoopVar(ref isOk, ref strError);
                                                    if (!isOk)
                                                    {
                                                        strAdv = "Contact Marquis";
                                                        return false;
                                                    }
                                                    itmStp.StepData.DATA_VALUE = strCurLoopVar;
                                                }
                                            }
                                        }
                                        #endregion
                                    }
                                    else if ("asnumber".Equals(itmStp.Row_Column??"", StringComparison.OrdinalIgnoreCase))
                                    {
                                        #region grid count, or loop by count
                                        Logger.Debug("para check, ", "asnumber trigger");
                                        /// if asnumber exists, then data should be fromMem
                                        /// 
                                        int iCount = -1;
                                        isOk = checkedLoopAsNumberData(itmStp.DATA_VALUE,ref iCount, ref strError, ref strAdv, ref strStack, itmStp.Row_Column);
                                        if (!isOk)
                                        {
                                            Logger.Error("RunTestStepOneByOne", $"checkedLoopAsNumberData generate errors|{strError}");
                                            return false;
                                        }
                                        if (isSubLoop)
                                        {
                                            /// ÐÞ¸Äloopcontrol
                                            /// 
                                            subLoopJump.loopVarMode = MarsVar_Type.VarT_userMemIteration;
                                            subLoopJump.loopIterationCount = iCount;

                                            subLoopJump.CurrentIdx = 0;
                                            strCurSubLoopVar = "0";
                                            Logger.Info("RunTestStepOneByOne", $"sub loop|asNumber mode|Extract data|{itmStp.DATA_VALUE}|get count|{iCount}");
                                        }
                                        else
                                        {
                                            /// ÐÞ¸Äloopcontrol
                                            /// 
                                            loopJump.loopVarMode = MarsVar_Type.VarT_userMemIteration;
                                            loopJump.loopIterationCount = iCount;

                                            loopJump.CurrentIdx = 0;
                                            strCurLoopVar = "0";
                                            strCurSubLoopVar = null;
                                            Logger.Info("RunTestStepOneByOne", $"Main loop|asNumber mode|Extract data|{itmStp.DATA_VALUE}|get count|{iCount}");
                                        }
                                        #endregion
                                    }
                                    else 
                                    {   /// normal model
                                        isOk = dealWithLoopVars(itmStp.RunId, strOrgLoopData, itmStp.StepData, loopJump,subLoopJump, ref strError, ref strAdv);
                                        if (!isOk)
                                        {
                                            Logger.Error("RunTestStepOneByOne",strError);
                                            strAdv = "Contact Marquis";
                                            strStack = MarsErrorStacks.StackTraceDump();
                                            return false;
                                        }
                                        
                                        if (currentLoopJump.loopVarMode == MarsVar_Type.varT_Sattus)
                                        {
                                            if (currentLoopJump.CurrentIdx >= 0)
                                            {
                                                /// Ö±½ÓÌøµ½loopend
                                                /// 
                                                strCurLoopVar = currentLoopJump.GetCurrentStatusLoopVar(ref isOk, ref strError);
                                                if (!isOk)
                                                {
                                                    strAdv = "Contact Marquis";
                                                    return false;
                                                }
                                                itmStp.StepData.DATA_VALUE = strCurLoopVar;
                                            }
                                        }
                                    }
                                }
                                /// 判断是否开始就没有数据
                                /// 
                                
                                switch (tmpLoopJump.loopVarMode)
                                {
                                    case MarsVar_Type.varT_Sattus:
                                        int iTtlCnt = tmpLoopJump.GetStuatusVarCount();
                                        if (iTtlCnt == 0)
                                        {
                                            if (!isSubLoop)
                                                isLoopJump = true;
                                            else
                                            {
                                                isSubLoopJump = true;
                                            }
                                            /*continue*/
                                        }
                                        else
                                        {
                                            //tmpLoopJump.CurrentIdx = 0;
                                        }
                                        break;
                                    case MarsVar_Type.VarT_userMemIteration:
                                        /// 如果循环结束所有的行，接结束
                                        /// 
                                        if (!isSubLoop)
                                            isLoopJump = tmpLoopJump.CurrentIdx > tmpLoopJump.loopIterationCount - 1;
                                        else isSubLoopJump = tmpLoopJump.CurrentIdx > tmpLoopJump.loopIterationCount - 1;
                                        break;
                                    default:
                                        if ((lstLoopVar == null) || (lstLoopVar.Count == 0))
                                        {
                                            Logger.Info("RunTestStepOneByOne", $"no loop variable is available ,end loop is [{loopJump.loopEndRunOrder}]");                                            
                                            isLoopJump = true;
                                            //iCurrentTestStepIdx = loopJump.loopEndRunOrder;

                                            continue;
                                        }
                                        else
                                        {
                                            Logger.Info("RunTestStepOneByOne", string.Join(",", lstLoopVar));
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                /// 非loop、subloop关键字，那么，如果需要loop或subloop的数据，
                                /// 需要判断从哪个loop中拿
                                strLastTestStep = itmStp.StepData == null ? $"{itmStp.Keyword}({itmStp.ObjectName},{itmStp.Row_Column ?? "N/A"},[N/A])" :
                                    $"{itmStp.Keyword}({itmStp.ObjectName},{itmStp.Row_Column ?? "N/A"},[{itmStp.StepData.DATA_VALUE}])";
                                /**
                                 * 如果是loopvar，需要在loop循环中才需要替换
                                 * 在
                                 */
                                if ((isLoopMode)
                                    && (MarsWindowsAPIsExtend.RegularTest("^" + B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP, itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE))
                                    && (itmStp.RunId >= loopJump.loopBeginRunOrder)
                                    && (itmStp.RunId <= loopJump.loopEndRunOrder)
                                    )
                                {
                                    bool isInSubLoop = IsCurrentStepInSubLoop(itmStp.RunId, subLoopJump);
                                    MarsLoopRunJump tmpLoopJump = isInSubLoop ? subLoopJump : loopJump;
                                    if (itmStp.StepData != null)
                                    {
                                        /// 需要loop变量，或者loop序号。在有子循环的模式中，strCurLoop
                                        strOrgLoopData = itmStp.StepData.DATA_VALUE;
                                        strLastTestStep = $"{itmStp.Keyword}({itmStp.ObjectName},{itmStp.Row_Column ?? "N/A"},[{itmStp.StepData.DATA_VALUE} <<== {strCurLoopVar}])";

                                        //switch (loopJump.loopVarMode) {
                                        switch (tmpLoopJump.loopVarMode) { 
                                            case MarsVar_Type.varT_normal:
                                                #region varT_normal 处理     
                                                Logger.Debug("\t=====\t", $"var type|{MarsVar_Type.varT_normal}|is in sub?|{isInSubLoop}");
                                                if (tmpLoopJump.loopVarMode == (int)MarsVar_Type.varT_normal)
                                                {
                                                    if (tmpLoopJump.CurrentIdx >= tmpLoopJump.loopStrings.Length)
                                                    {
                                                        Logger.Error("RunTestStepOneByOne", (strError = "loop index is greater than loop control number") + "\r\n" + strLastTestStep);
                                                        strAdv = "Contarct Marquis";
                                                        strStack = MarsErrorStacks.StackTraceDump();
                                                        if (teststepDoneCallBack != null)
                                                        {
                                                            teststepDoneCallBack(itmStp, false, null, strError);
                                                        }
                                                        return false;
                                                    }
                                                    string loopVarNameIdx = GetLoopVarNameIdx(strOrgLoopData);
                                                    if ((!string.IsNullOrEmpty(loopVarNameIdx)) && (SystemConstant.CNST_RESERVED_LOOP_IDX.Equals(loopVarNameIdx)))
                                                    {
                                                        itmStp.StepData.DATA_VALUE = tmpLoopJump.CurrentIdx + "";
                                                    }
                                                    else
                                                        itmStp.StepData.DATA_VALUE = tmpLoopJump.loopStrings[loopJump.CurrentIdx]; // strCurLoopVar;
                                                    Logger.Debug("\t=====\t", $"var type|{MarsVar_Type.varT_normal}|is in sub?|{isInSubLoop}|{itmStp.StepData.DATA_VALUE}|index|{tmpLoopJump.CurrentIdx}");
                                                }
                                                #endregion
                                                break;
                                            case MarsVar_Type.varT_Sattus:
                                                itmStp.StepData.DATA_VALUE = isInSubLoop? strCurSubLoopVar:strCurLoopVar;
                                                break;
                                            case MarsVar_Type.VarT_userMemIteration:
                                                #region useMem
                                                /// 采用内存循环变量
                                                /// 
                                                //itmStp.GetPara() = loopJump.currentIdx+"";

                                                #endregion
                                                break;
                                            default:
                                                strError = "No supported variable type";
                                                strAdv = "Contact Marquis";
                                                strStack = MarsErrorStacks.StackTraceDump();
                                                Logger.Error("RunTestStepOneByOne",strError, strStack);
                                                return false;
                                                
                                        }
                                        isDataSourceChanged = true;
                                    }
                                }
                            }
                            #region keyword:removeVariable 预处理
                            /// 预处理removevariable的数据
                            /// µ±Ç°
                            if (string.Compare("RemoveVariable", itmStp.Keyword, true) == 0)
                            {
                                ///Ä¿Ç°Ö»Ö§³ÖloopÖÐ
                                ///
                                MarsVarBasic targetVarInfo = null;
                                int targetVarIdx = -1;
                                bool isInSubLoop = IsCurrentStepInSubLoop(itmStp.RunId, subLoopJump);
                                MarsLoopRunJump tmpLoopJump = isInSubLoop ? subLoopJump : loopJump;
                                Logger.Debug("\t======\t", $"RemoveVariable has switched to |is InSubLoop|{isInSubLoop}");
                                isOk = prepareRemoveVariable(strCurLoopVar, itmStp,
                                    tmpLoopJump, //loopJump, 
                                    ref strError, ref targetVarInfo,ref targetVarIdx);
                                if ((!isOk)||(targetVarInfo==null)||(targetVarInfo as MarsStatusVar==null))
                                {
                                    strAdv = "Contact Marquis";
                                    strStack = Environment.StackTrace;
                                    return false;
                                }
                                /// ¹¹½¨
                                /// 
                                MarsStatusVar targetStatus = targetVarInfo as MarsStatusVar;
                                isOk = targetStatus.Update(targetVarIdx, "0");
                                if (!isOk)
                                {
                                    strError = " Can't update variable, index could out of the range";
                                    strAdv = "Contact Marquis";
                                    strStack = Environment.StackTrace;
                                    return false;
                                }
                                /// 无法用通用模式处理数据，故而在这里更新数据库
                                /// 
                                if (!targetStatus.synchronizeToDB(currentDBIdx,ref strError))
                                {
                                    strAdv = "Contact Marquis";
                                    strStack = Environment.StackTrace;
                                    return false;
                                }
                            }

                            #endregion

                            #region keyword "fillTable" 预处理, 11/22/24增加
                            if ("fillTable".Equals(itmStp.Keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                bool isInSubLoop = IsCurrentStepInSubLoop(itmStp.RunId, subLoopJump);
                                if (!isInSubLoop)
                                    isOk = PreProcessKeywordFillTable(itmStp, loopJump, ref strError, ref strAdv, ref strStack);
                                else
                                    isOk = PreProcessKeywordFillTable(itmStp, subLoopJump, ref strError, ref strAdv, ref strStack);
                                if (!isOk)
                                {
                                    Logger.Error("\t", $"PreProcessKeywordFillTable has error|{strError}");
                                    return false;
                                }
                            }
                            #endregion

                            #region 执行当前test step
                            string strParaWithFunc = "";
                            string strActualInput = "";
                            string strSnapshotPath = "";
                            long? oldApplicationId = itmStp.StepObject.APPLICATION_ID;
                            string strCommnt = itmStp.Comment;
                            bool isCommntIgnoreError = IsIgnoreComment(strCommnt);
                            isOk = KeywordOpAgent.DoTestStep(itmStp.StepObject, itmStp.StepData, itmStp.StepsFromDB,
                                strMode, appTyp,
                                AutoCheckErrorSet,                                
                                ref strParaWithFunc,
                                ref strError,
                                ref strWriteBackName,
                                ref strDataReturned,
                                ref strVarType,
                                ref strActualInput,
                                ref isVar,
                                ref isSkpped,
                                ref strAdv,
                                ref strStack,
                                ref strSnapshotPath,
                                strDBIdx: currentDBIdx,
                                iLoopIdx: loopJump == null ? -1 : loopJump.CurrentIdx, 
                                isAttachUIAAHwnd: isdotNetFrameworkEngineRequired);
                            long? newApplicationId = itmStp.StepObject.APPLICATION_ID;
                            Logger.Info("\t", $"4 returned data:{strDataReturned}");
                            #endregion

                            if (!isOk)
                            {
                                ///需要全屏幕截取，同时将信息存到记录中
                                ///
                                strFullScreenSnapshotPath =
                                    Mars.AutoTestingDriver.MarsHelpers.MarsScreenHelper
                                        .CaptureProcessGuiToBmp(MARSTestProcess.CurrentTestProcessId);

                                if (isCommntIgnoreError)
                                    isOk = true;
                            }

                            //Notify web application, that test step is done
                            NotifyWebapplicationTestStepsIsDone(isOk, strError, itmStp.StepsFromDB==null?-1: itmStp.StepsFromDB.RUN_ORDER);

                            if ((newApplicationId != null) && (oldApplicationId != newApplicationId))
                            {
                                lAppId = newApplicationId.Value;
                                //需要调整appType
                                if (KeyWordsOPForNonGUI.CurrentApplicationStartPath != null)
                                    appTyp = Mars_applicationTyp.GetAppTypeViaShort(KeyWordsOPForNonGUI.CurrentApplicationStartPath.APPLICATION_TYPE_ID);

                            }
                            byte[] arrData = null;
                            #region endloop 处理
                            /// 在endloop过程中，如果是 userIteration模式，则需要将current index+1                          
                            if (("endLoop".Equals(itmStp.Keyword, StringComparison.OrdinalIgnoreCase))
                                ||(SystemConstant.CNST_RESERVED_KEYWORD_ENDSUBLOOP.Equals(itmStp.Keyword, StringComparison.OrdinalIgnoreCase)))
                            {
                                Logger.Debug("RunTestStepOneByOne-endloop", "enter end loop");
                                bool isInSubLoop = IsCurrentStepInSubLoop(itmStp.RunId, subLoopJump);
                                #region resumenext处理                                 
                                if (itmStp.RunId> MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder)
                                {
                                    /// 如果resumenxt 在循环内，那么，相当于continue，需要重置resumenext
                                    ///
                                    if (hadErrors)
                                        hadErrors = false;
                                }
                                Logger.Info("\t", $"endloop with resumeNext [{itmStp.RunId}] - [{MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder}] has Error:[{hadErrors}]");
                                #endregion
                                if (isLoopJump)
                                {
                                    Logger.Debug("RunTestStepOneByOne-endloop", "isLoopJump true");
                                    isLoopJump = false;                                   
                                    loopJump = new MarsLoopRunJump();
                                    if (teststepDoneCallBack != null)
                                    {
                                        teststepDoneCallBack(itmStp, true, itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE, "SUCCESS");
                                    }
                                    continue;
                                }
                                else
                                {
                                    // 完成了一次 loop
                                    /// 算法：
                                    /// 1, 判断是否最后一次，如果不是，转 2
                                    /// 2, 设置 iCurrentTestStepIdx 为启动的 id，continue
                                    /// 3, 说明 loop 已经结束，清理 loopJump
                                    /// endloop 可以使用 endat:number
                                    /// 11/22/24 
                                    ///     增加 userIteration 模式
                                    /// 
                                    //loopJump.currentIdx++;
                                    Logger.Debug("RunTestStepOneByOne-endloop", "after on loop");
                                    if (!string.IsNullOrEmpty(itmStp.DATA_VALUE))
                                    {
                                        Logger.Debug("RunTestStepOneByOne-endloop", $"itmStp.DATA_VALUE|{loopendObj.endLoopRunOrd}|{itmStp.RunId}");
                                        if (loopendObj.endLoopRunOrd != itmStp.RunId)
                                        {
                                            loopendObj = new MarsEndLoop(itmStp.RunId);
                                            if (!loopendObj.ParseData(itmStp.DATA_VALUE, ref strError))
                                            {
                                                strAdv = "Please check test step's data";
                                                strStack = Environment.StackTrace;
                                                isOk = false;
                                            }
                                            else
                                            {
                                                if (loopendObj.endLoopAt <= loopJump.CurrentIdx)
                                                {
                                                    loopJump.CurrentIdx = loopJump.loopStrings.Length + 1;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (loopendObj.endLoopAt <= loopJump.CurrentIdx)
                                            {
                                                loopJump.CurrentIdx = loopJump.loopStrings.Length + 1;
                                            }
                                        }
                                    }
                                    else {
                                        Logger.Debug("RunTestStepOneByOne-endloop", "string.IsNullOrEmpty(itmStp.DATA_VALUE) true");
                                    }
                                    if (isOk)
                                    {
                                        MarsLoopRunJump tmpLoopForEndOp = isInSubLoop?subLoopJump:loopJump;
                                       
                                        //if (loopJump.currentIdx >= loopJump.loopStrings.Length)
                                        if (!tmpLoopForEndOp.anyNoMoreItems())
                                        {
                                            Logger.Debug("RunTestStepOneByOne-endloop", "anyNoMoreItems false");
                                            ///最后一次,同普通的test step
                                            ///
                                            if (isInSubLoop)
                                                subLoopJump = new MarsLoopRunJump();
                                            else
                                                loopJump = new MarsLoopRunJump();
                                        }
                                        else
                                        {
                                            Logger.Debug("RunTestStepOneByOne-endloop", "anyNoMoreItems true");
                                            tmpLoopForEndOp.MoveDataIdxToAvailable();
                                            Logger.Debug("RunTestStepOneByOne-endloop", $"MoveDataIdxToAvailable begin|{tmpLoopForEndOp.loopVarMode}|{tmpLoopForEndOp.CurrentIdx}|{tmpLoopForEndOp.loopIterationCount}");
                                            //var idx = lstTestStps.Select((p, index) => new { runOrd = p.RunId, index })
                                            //.FirstOrDefault(p => p.runOrd == loopJump.loopBeginRunOrder + 1).index;
                                            //上面应该不会出错
                                            //iCurrentTestStepIdx = idx + 1;
                                            iCurrentTestStepIdx = tmpLoopForEndOp.loopBeginRunOrder;
                                            bool tmpOk = true;
                                            strCurLoopVar = tmpLoopForEndOp.GetCurrentStatusLoopVar(ref tmpOk, ref strError);
                                            continue;
                                        }
                                    }
                                }

                            }
                            #endregion

                            if (isSkpped)
                            {
                                if (mntrCnt != null)
                                    mntrCnt.SkipCurrentStep();
                                if ((string.Compare("loop", itmStp.Keyword, true) == 0)
                                    ||(SystemConstant.CNST_RESERVED_KEYWORD_SUBLOOP.Equals(itmStp.Keyword, StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (itmStp.StepData != null)
                                        itmStp.StepData.DATA_VALUE = strOrgLoopData;
                                }
                                else
                                {
                                    if ((isLoopMode) && (MarsWindowsAPIsExtend.RegularTest("^" + B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP, itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE)))
                                    {
                                        if (itmStp.StepData != null)
                                            itmStp.StepData.DATA_VALUE = strOrgLoopData;
                                    }
                                    if (isDataSourceChanged)
                                    {
                                        if (itmStp.StepData != null)
                                            itmStp.StepData.DATA_VALUE = strOrgLoopData;
                                    }
                                }
                                continue;
                            }
                            #region ifelseend 
                            if (string.Compare("if", itmStp.Keyword, true) == 0)
                            {
                                //Logger.Info("FindIF", string.Format("'if' returns [{0}] jumpMgr Count is:[{1}]", isOk, ifelseSkipStepsId.Count));
                                foreach (var itm in ifelseSkipStepsId)
                                {
                                    if (itm == null) continue;
                                    if (itm.KeywordName.Equals("IF", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // if 段，如果返回true，那么if段的skip为false，如果返回false，if段的step应该跳过
                                        itm.isSkip = !isOk;
                                        Logger.Info("FindIF", string.Format("'if' returns [{0}] if skip change to [{1}]", isOk, itm.isSkip));
                                    }
                                    else if (itm.KeywordName.Equals("ELSE", StringComparison.OrdinalIgnoreCase))
                                    {
                                        itm.isSkip = isOk;
                                        Logger.Info("FindIF", string.Format("'if' returns [{0}] else skip change to [{1}]", isOk, itm.isSkip));
                                    } // endif 无需修改
                                }
                                #region to be deleted

                                //if (!isOk)
                                //{
                                //    // 说明if返回错误，如果有else，就转else""
                                //    foreach(var itm in ifelseSkipStepsId)
                                //    {
                                //        if (itm == null) continue;
                                //        if (itm.KeywordName.Equals("IF", StringComparison.OrdinalIgnoreCase))
                                //        {
                                //            itm.isSkip = true;
                                //        }
                                //        else itm.isSkip = false;
                                //    }
                                //}
                                //else
                                //{
                                //    // 说明if返回正确， 只执行if的部分
                                //    foreach(var itm in ifelseSkipStepsId)
                                //    {
                                //        if (itm == null) continue;
                                //        if (itm.KeywordName.Equals("IF", StringComparison.OrdinalIgnoreCase))
                                //        {
                                //            itm.isSkip = false;
                                //        }
                                //        else itm.isSkip = true;
                                //    }
                                //}

                                //ifelseSkipStepsId[0].isSkip = !isOk;
                                //if (ifelseSkipStepsId.Count == 2)
                                //{
                                //    ifelseSkipStepsId[1].isSkip = !isOk;
                                //}
                                //else
                                //{
                                //    if (ifelseSkipStepsId.Count == 3)
                                //    {
                                //        ifelseSkipStepsId[1].isSkip = isOk;
                                //        ifelseSkipStepsId[2].isSkip = isOk;
                                //    }
                                //}
                                #endregion

                                if (!isNotRequiredToWriteBackToDB)
                                    recodeMgr.UpdateCurrentTestStepResult(1, itmStp.TestStepId, isOk ? 1 : 2, "if keyword, just contorl flow",
                                        strActualInput, null, currentDBIdx);
                                if (teststepDoneCallBack != null)
                                {
                                    teststepDoneCallBack(itmStp, true, itmStp.DATA_VALUE == null ? null : itmStp.StepData.DATA_VALUE, "if keyword, just contorl flow");
                                }
                                continue;
                            }
                            #endregion //ifelseend
                            if (string.Compare("checkError", itmStp.Keyword, true) == 0 && !isOk)
                            {
                                int iPathPos = strDataReturned.IndexOf(";");
                                string strReturnedFileName = "";
                                if (iPathPos >= 0)
                                {
                                    strReturnedFileName = strDataReturned.Substring(0, iPathPos);
                                    if (!File.Exists(strReturnedFileName))
                                    {
                                        recodeMgr.UpdateCurrentTestStepResult(1, itmStp.TestStepId, 2,
                                            strError = $"no such file exists after checkError returns false:[{iPathPos}]", strActualInput, null,
                                            currentDBIdx);
                                        strAdv = "";
                                        StackFrame stck = new StackFrame();
                                        strStack = $"at File:{stck.GetFileName()}, {stck.GetMethod()} {stck.GetFileLineNumber()}";
                                        isOk = false;
                                    }
                                    else
                                    {
                                        arrData = recodeMgr.GetFileToBytes(strReturnedFileName, ref isOk, ref strError);
                                    }
                                    strDataReturned = strDataReturned.Substring(iPathPos + 1);
                                }
                            }

                            if ((string.Compare("Snapshot", itmStp.Keyword, true) == 0) && (isOk))
                            {
                                Logger.Info("\t...", $"snapshot|{strDataReturned}|");
                                if (!System.IO.File.Exists(strDataReturned))
                                {
                                    Logger.Info("\t...", $"snapshot|no cuch data file|{strDataReturned}|");
                                    recodeMgr.UpdateCurrentTestStepResult(1, itmStp.TestStepId, 2, strError = string.Format("no such file exists after snapshot:[{0}]", strDataReturned),
                                        strActualInput, null, currentDBIdx);
                                    strAdv = "";
                                    StackFrame stck = new StackFrame();
                                    strStack = $"at File:{stck.GetFileName()}, {stck.GetMethod()} {stck.GetFileLineNumber()}";
                                    isOk = false;
                                }
                                else
                                {
                                    arrData = recodeMgr.GetFileToBytes(strDataReturned, ref isOk, ref strError);
                                }
                            }
                            Logger.Info("\t", $"3 returned data:{strDataReturned}");

                            if ( (string.Compare("loop", itmStp.Keyword, true) == 0) 
                                || (string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_SUBLOOP, itmStp.Keyword, true)==0)
                                || (isDataSourceChanged))
                            {
                                if (itmStp.StepData != null)
                                    itmStp.StepData.DATA_VALUE = strOrgLoopData;
                            }
                            else
                            {
                                if ((isLoopMode) && (MarsWindowsAPIsExtend.RegularTest("^" + B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP,
                                        itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE)))
                                {
                                    //需要loop var
                                    if (itmStp.StepData != null)
                                        itmStp.StepData.DATA_VALUE = strOrgLoopData;
                                }
                            }
                            Logger.Info("\t", $"2 returned data:{strDataReturned}");
                            if (isOk)
                            {
                                //先判断是否需要进行检查error
                                if ((this.AutoCheckErrorSet.IsAutoErrorChck) && (!isNotRequiredToWriteBackToDB))
                                {
                                    string strDataReturnedTmp = "";
                                    isOk = DealCheckErrorByAuto(lstTestStps, itmStp.Keyword, itmStp.RunId, strMode,
                                    appTyp,
                                    AutoCheckErrorSet,
                                    ref strError,
                                    ref strAdv,
                                    ref strStack,
                                    ref strSnapshotPath,
                                    ref strDataReturnedTmp,
                                    mntrCnt,
                                    currentDBIdx);
                                    if (!isOk)
                                    {
                                        strError = $"an error is arised ,\"{strError}\" with error log:{strDataReturnedTmp} after run {itmStp.Keyword}";
                                        Logger.Error("RunTestStepOneByOne", strError);
                                    }

                                    if ((!string.IsNullOrEmpty(strSnapshotPath))
                                    && (System.IO.File.Exists(strSnapshotPath)))
                                    {
                                        bool isOKtmp = false;
                                        string tmpError = "";
                                        if (!string.IsNullOrEmpty(strDataReturnedTmp))
                                            strDataReturned = strDataReturnedTmp;
                                        arrData = recodeMgr.GetFileToBytes(strSnapshotPath, ref isOKtmp, ref tmpError);
                                    }
                                }
                            }

                            if ((!isOk))
                            {
                                #region //error when deal with keyword
                                if (!isNotRequiredToWriteBackToDB)
                                {
                                    byte[] picinfo = null;
                                    // 如果有全屏/窗口截图路径，则读取为字节数组用于错误截图
                                    if (!string.IsNullOrEmpty(strFullScreenSnapshotPath)
                                        && System.IO.File.Exists(strFullScreenSnapshotPath))
                                    {
                                        bool isOKtmpFs = false;
                                        string tmpErrorFs = "";
                                        picinfo = recodeMgr.GetFileToBytes(strFullScreenSnapshotPath, ref isOKtmpFs, ref tmpErrorFs);
                                    }

                                    recodeMgr.UpdateCurrentTestStepResult(1,
                                        itmStp.TestStepId,
                                        2,
                                        string.Format("result:[{0} {1}]", string.Format("{0} {1}", strError, strDataReturned), strDataReturned),
                                        strActualInput, picinfo,
                                        currentDBIdx);
                                }
                                if (teststepDoneCallBack != null)
                                {
                                    teststepDoneCallBack(itmStp, false,
                                        itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE,
                                        string.Format("result:[{0} {1}]", string.Format("{0} {1}", strError, strDataReturned), strDataReturned));
                                }
                                ///算法：
                                /// 1，通知monitor
                                /// 2，回写数据库执行状态
                                /// 
                                if (mntrCnt != null)
                                {
                                    /// 判断是否存在 resumeNext,如果存在
                                    if (itmStp.RunId < MarsGlobalStatusMgr.resumeNextStatus.resumeNextRunOrder)
                                    {
                                        Logger.Error("RunTestStepOneByOne", "Ignore, as resumenext is enabled");
                                        hadErrors = true;
                                        try
                                        {
                                            mntrCnt.SkipCurrentStep();
                                        }

                                        catch (Exception e)
                                        {
                                            Logger.Error("RunTestStepOneByOne", string.Format("type:[{0}] exception:[{1}]", e.GetType(), e.Message), e);
                                        }
                                        if (teststepDoneCallBack != null)
                                        {
                                            teststepDoneCallBack(itmStp, false,
                                                itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE,
                                                "Ignore, as resumenext is enabled");
                                        }
                                        continue;
                                    }

                                    if ((itmStp.StepData != null) && ((itmStp.StepData.DATA_DIRECTION == 4) || (string.Compare("skip", itmStp.StepData.DATA_VALUE, true) == 0)))
                                    {
                                        if (mntrCnt != null)
                                            mntrCnt.SkipCurrentStep();

                                    }
                                    else
                                    {
                                        if (mntrCnt != null)
                                            mntrCnt.AfterClientRunTestStepEvent(strError, 0, strError);

                                    }
                                }
                                //if (string.Compare("verifyvalue", itmStp.Keyword, true) == 0)
                                //{
                                //    if (isVerifyValueSkipper)
                                //    {
                                //        continue;
                                //    }
                                //}
                                if (!isIgnoreError)
                                {
                                    ///判断是否存在resumeNext，如果存在就直接jump
                                    ///            
                                    if (mntrCnt != null)
                                    {
                                        mntrCnt.OnOneLoopIsDone();
                                    }
                                    if (teststepDoneCallBack != null)
                                    {
                                        teststepDoneCallBack(itmStp, false,
                                            itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE,
                                            strError);
                                    }
                                    return false;
                                }
                                else
                                { //忽略错误模式，需要保留第一条错误
                                    if (!hasError)
                                    {
                                        Logger.Info("\t", string.Format("[{0}] return false, with error:[{1}]", itmStp.Keyword, strError));
                                        hasError = true;
                                        strFirstError = strError;
                                        if (teststepDoneCallBack != null)
                                        {
                                            teststepDoneCallBack(itmStp, false,
                                                itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE,
                                                strError);
                                        }
                                    }
                                }
                                #endregion 
                            }
                            else
                            {
                                /// 如果loop等开始就没有数据，需要判断是否还有循环数据
                                if (string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_LOOP, itmStp.Keyword, true) == 0)
                                {
                                    Logger.Info($"Keyword|{SystemConstant.CNST_RESERVED_KEYWORD_LOOP}", "begin to check if is the first time of ");
                                    if (!isSubLoopJump)
                                    {
                                        if (!loopJump.AreAnyMoreLoopVars())
                                        {
                                            isLoopJump = true;
                                            ///Ö±½ÓÌøµ½¡°endLoop"
                                            ///
                                            iCurrentTestStepIdx = loopJump.loopEndRunOrder-1;
                                        }
                                    }
                                    else
                                    {
                                        if (!subLoopJump.AreAnyMoreLoopVars())
                                        {
                                            isLoopJump = true;
                                            ///Ö±½ÓÌøµ½¡°endsubLoop"
                                            iCurrentTestStepIdx = subLoopJump.loopEndRunOrder-1;
                                        }
                                    }
                                }

                                /// 对API相关的keyword，需要做特殊处理
                                /// 1，将Actual-input-data 换为config的值
                                /// 2，将填写return-data
                                /// 
                                if (APIEngineHelper.IsKeywordAPIIntegrated(itmStp.Keyword)) { 
                                    
                                }

                                //处理正确
                                if ((string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREVALUE, itmStp.Keyword, true) == 0)
                                    || (string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREANDCOMPARE, itmStp.Keyword, true) == 0)
                                    || (string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREANDCOMPAREBYKEY, itmStp.Keyword, true) == 0)
                                    )
                                {
                                    Logger.Info("\t", $"current keyword after done:[{itmStp.Keyword}]，{itmStp.Row_Column}");
                                    Logger.Info("\t", $"1 returned data:{strDataReturned}");

                                    // 是否是batchmode

                                    string tmpError = "";
                                    bool isSaveAsByte = false;
                                    if ((itmStp.StepsFromDB != null) && (!string.IsNullOrWhiteSpace(itmStp.StepsFromDB.COLUMN_ROW_SETTING)))
                                    {
                                        isSaveAsByte = MarsWindowsAPIsExtend.RegularTest("CSV_ALL", itmStp.StepsFromDB.COLUMN_ROW_SETTING);
                                    }

                                    if (teststepDoneCallBack != null)
                                    {
                                        teststepDoneCallBack(itmStp, true,
                                            itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE,
                                            strDataReturned);
                                    }

                                    var lstOpType = MarsParametersOp.GetOpType(itmStp.Row_Column, ref tmpError);
                                    if (lstOpType != null)
                                    {
                                        string strDataReturnedTmp = strDataReturned;
                                        foreach (var itm in lstOpType)
                                        {
                                            if (itm == null) continue;
                                            strDataReturnedTmp = itm.dealWithData(strDataReturnedTmp);
                                            Logger.Info("\t", $"substr func, before:[{strDataReturned}], after:[{strDataReturnedTmp}]");
                                        }
                                        strDataReturned = strDataReturnedTmp;
                                    }
                                    //if ((!string.IsNullOrEmpty(itmStp.Comment))&&())
                                    Logger.Info("\t", $"0 returned data:{strDataReturned}");
                                    if (isVar)
                                    {
                                        int iVarType = recodeMgr.getVariableType(strVarType);
                                        if ((iVarType > 0) && (!isNotRequiredToWriteBackToDB))
                                            if (!recodeMgr.updateVariableValue(strWriteBackName, strDataReturned, 1,
                                                iVarType, ref strError, strMode, currentDBIdx))
                                            {
                                                Logger.Error("\t", $"can't updateVariableValue with error:[{strError}]-[{currentDBIdx}]");
                                                strStack = Environment.StackTrace;
                                                return false;
                                            }
                                    }
                                    if ((string.Compare(SystemConstant.CNST_RESERVED_KEYWORD_CAPTUREANDCOMPAREBYKEY, itmStp.Keyword, true) == 0)
                                        && (!isNotRequiredToWriteBackToDB))
                                    {
                                        //按照Key的比较
                                        //recodeMgr.StoreData4ForTestReport_Steps(strWriteBackName, strDataReturned, 1,lTestcaseId, itmStp.TestStepId);
                                        //recodeMgr.StoreData4ForTestCompareByKeyReport_Steps(strWriteBackName, strDataReturned, 1, lTestcaseId, itmStp.TestStepId,
                                        //    1, strError, ref strError,
                                        //    currentDBIdx);
                                        /// 2024 12 19 compare by key 当做普通captureCompare处理，因为数据已经附加了key的信息
                                        recodeMgr.StoreData4ForTestReport_Steps(strWriteBackName, strDataReturned, 1, lTestcaseId, itmStp.TestStepId,
                                                strParaWithFunc, ref isOk, ref strError,
                                                currentDBIdx);
                                    }
                                    else
                                    {
                                        /// 在loop模式中，如果data的设置是seqLoop，则需要进行特殊处理。seqloop的格式是SEQLOOP:EXCEPTION_TRADESERVERNAME_$
                                        /// 存储的变量名称，然后加序号
                                        if ((itmStp.StepData != null)
                                            && (!string.IsNullOrEmpty(itmStp.StepData.DATA_VALUE))
                                            && (itmStp.StepData.DATA_VALUE.StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_SEQLOOP)))
                                        {
                                            string strTmpWriteBack = itmStp.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_SEQLOOP.Length + 1);
                                            //remove $
                                            strWriteBackName = strTmpWriteBack.Replace("$", "") + loopJump.CurrentIdx;
                                        }

                                        if (!isNotRequiredToWriteBackToDB)
                                            recodeMgr.StoreData4ForTestReport_Steps(strWriteBackName, strDataReturned, 1, lTestcaseId, itmStp.TestStepId,
                                                strParaWithFunc, ref isOk, ref strError,
                                                currentDBIdx);
                                        if (!isOk)
                                        {
                                            if (teststepDoneCallBack != null)
                                            {
                                                teststepDoneCallBack(itmStp, false,
                                                    itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE,
                                                    $"{strError}\r\n{strDataReturned}");
                                            }
                                            return false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (string.Compare("SnapShot", itmStp.Keyword, true) == 0)
                                    {

                                    }
                                }
                                //else
                                Logger.Info("\t", $"-1 returned data:{strDataReturned}-[{arrData}]|{isNotRequiredToWriteBackToDB}|");
                                if (APIEngineHelper.IsKeywordAPIIntegrated(itmStp.Keyword))
                                {
                                    Logger.Info("\t", $"API integrated keyword|{itmStp.Keyword}| do not update test step result");
                                    recodeMgr.UpdateCurrentTestStepResultForAPI(1, itmStp.TestStepId, 1, "SUCCESS", strActualInput, strDataReturned,currentDBIdx, 
                                        strAdv, strStack);
                                }
                                else if (!isNotRequiredToWriteBackToDB)
                                    recodeMgr.UpdateCurrentTestStepResult(1, itmStp.TestStepId, 1, "SUCCESS", strActualInput, arrData, currentDBIdx, strAdv, strStack);
                                if (mntrCnt != null)
                                {
                                    if ((itmStp.StepData != null) && ((itmStp.StepData.DATA_DIRECTION == 4) || (string.Compare("skip", itmStp.StepData == null ? "" : itmStp.StepData.DATA_VALUE, true) == 0)))
                                    {
                                        mntrCnt.SkipCurrentStep();
                                    }
                                    else
                                        mntrCnt.AfterClientRunTestStepEvent("ok", 1, "ok");
                                }
                                if (teststepDoneCallBack != null)
                                {
                                    teststepDoneCallBack(itmStp, true,
                                        itmStp.StepData == null ? null : itmStp.StepData.DATA_VALUE,
                                        string.IsNullOrEmpty(strDataReturned) ? "SUCCESS" : strDataReturned);
                                }
                                Logger.Info("\t", "before call dealwithPreview");
                        
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error("\t",strError = e.Message, strStack = e.StackTrace);
                            strAdv = "Contact Marquis";
                            if (wcfClient.WcfClientAgent.IsWcfOffLine())
                            {
                                wcfClient.WcfClientAgent.ReconnectTo();
                                mntrCnt = wcfClient.WcfClientAgent.MonitorWcfClient;
                            }
                            if (mntrCnt != null)
                            {
                                mntrCnt.OnOneLoopIsDone();
                            }
                            return false;
                        }
                        finally
                        {

                            if (wcfClient.WcfClientAgent.IsWcfOffLine())
                            {
                                wcfClient.WcfClientAgent.ReconnectTo();
                                mntrCnt = wcfClient.WcfClientAgent.MonitorWcfClient;
                            }

                        }
                    }
                    if (mntrCnt != null)
                    {
                        mntrCnt.OnOneLoopIsDone();
                    }

                }
            }
            finally
            {
                if (teststepsListDoneCallBack != null)
                {
                    teststepsListDoneCallBack(isOk, isOk ? "SUCCESS" : strFirstError);
                }
                Logger.Info("RunTestStepOneByOne", $"end with [{isOk}]");
            }
            if (hasError/* && (!isOk)*/)
            {
                strError = strFirstError;
            }
            return isOk;
        }

        private static bool LaunchDotNetFrameworkEngine( ref string strError, ref string strAdv, ref string strStack)
        {
            /// 需要将framework engine加载到当前进程中
            /// mj4.Injector.Launch(arrP[0].MainWindowHandle, System.IO.Path.Combine(strPath, "MarsInterMQCenter.Any.dll"),
            /// tmpNamespace, //"Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                                                    //"StartMonitorThread", "Normal");

            if (MARSTestProcess.CurrentTestProcessId <= 0)
            {
                strError = "no current test process id is available";
                strAdv = "please check Target application is started";
                strStack = Environment.StackTrace;
                Logger.Error("LaunchDotNetFrameworkEngine", $"{strError}|{strAdv}\r\n{strStack}");
                return false;
            }
            if (IsProcessHasMARSEngine(MARSTestProcess.CurrentTestProcessId))
            {
                Logger.Info("CheckTestStepRequiresFrameEngineFromThenOn", $"process id|{MARSTestProcess.CurrentTestProcessId}|has attached engine");
                return true;
            }
            Process p = Process.GetProcessById(MARSTestProcess.CurrentTestProcessId);
            if (p.MainWindowHandle == IntPtr.Zero)
            {
                strError = "no main window handle is available for current test process";
                strAdv = "please check Target application is started";
                strStack = Environment.StackTrace;
                Logger.Error("LaunchDotNetFrameworkEngine", $"{strError}|{strAdv}\r\n{strStack}");
                return false;
            }
            string strPath = typeof(ExecutableTestCaseStep).Assembly.Location;
            strPath = System.IO.Path.GetDirectoryName(strPath);
            Injector.Launch(p.MainWindowHandle, $"{strPath}\\MarsInterMQCenter.dll", "Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc",
                "StartMonitorThread", "Normal");
            return true;
        }

        private bool CheckTestStepRequiresNewFrameEngineFromThenOn(ExecutableTestCaseStep itmStp)
        {
            if (itmStp == null) return false;
            string strPara = itmStp.GetPara();
            if (string.IsNullOrEmpty(strPara)) return false;
            if (!strPara.StartsWith(MarsKeywordBase.cnst_marsaddins, StringComparison.OrdinalIgnoreCase)) return false;
            
            /// 修改parameter
            /// 
            strPara = strPara.Substring(MarsKeywordBase.cnst_marsaddins.Length);
            itmStp.setPara(strPara);
            
            return true;

            
            //return true;
        }



        /// <summary>
        /// 打开processid，获得所有的模块
        /// 检查模块是否包括MARSEngine的dll MarsInterMQCenter
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public static bool IsProcessHasMARSEngine(int processId)
        {
            Process p = Process.GetProcessById(processId);
            if (p == null) return false;
            List<string> lstName = new List<string>();
            for (int i = 0;i < p.Modules.Count; i++)
            {
                ProcessModule pm = p.Modules[i];
                if (pm == null) continue;
                lstName.Add($"{pm.ModuleName}|{pm.FileName}");
                try
                {
                    if (pm.ModuleName.Equals("MarsInterMQCenter.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Info("IsProcessHasMARSEngine", $"process id|{processId}|found module|{pm.ModuleName}|path|{pm.FileName}|");
                        return true;
                    }
                    if (pm.ModuleName.Equals("ManagedInjector64-4.0.dll", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Info("IsProcessHasMARSEngine", $"process id|{processId}|found module|{pm.ModuleName}|path|{pm.FileName}|");
                        return true;
                    }
                }catch(Exception e)
                {

                }
            }
            return false;
        }

        private bool ReHostToApp(string strCurMarsAccount, V_TEST_STEPS_FULLVISIONDTO stepsFromDB, Mars_applicationTyp.MARS_APPTYPE appTyp, 
            B_REGISTERED_APPS currentTestApp, ref string strError, ref string strAdv, ref string strStack, int iWaitForSeconds = 10)
        {
            Logger.logBegin("ReHostToApp", $"apptyp|{appTyp}|currentTestApp|{currentTestApp?.APP_SHORT_NAME}|{currentTestApp?.APPLICATION_TYPE_ID}");
            switch (appTyp)
            {
                case Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP:
                case Mars_applicationTyp.MARS_APPTYPE.MARS_WEB:
                case Mars_applicationTyp.MARS_APPTYPE.MARS_WEB_DOJO:
                    //暂时所有的都返回true
                    return true;
                case Mars_applicationTyp.MARS_APPTYPE.MARS_CORE_WPF:
                    //需要重新注入到目标进程
                    if ((!string.IsNullOrEmpty(stepsFromDB.COLUMN_ROW_SETTING)) && (stepsFromDB.COLUMN_ROW_SETTING.IndexOf(MarsKeywordBase.cnst_pegwindow_para_reHost) >= 0))
                    {
                        bool isOk = MarsCoreAppInterfaceManagement.HostToTargetApplication(strCurMarsAccount, 
                            currentTestApp.PROCESS_IDENTIFIER, 
                            ref strError, ref strAdv, ref strStack,
                            waitSeconds:iWaitForSeconds);
                        if (isOk)
                        {
                            stepsFromDB.COLUMN_ROW_SETTING = stepsFromDB.COLUMN_ROW_SETTING.Replace(MarsKeywordBase.cnst_pegwindow_para_reHost, "");
                            if (stepsFromDB.COLUMN_ROW_SETTING.StartsWith(";"))
                            {
                                stepsFromDB.COLUMN_ROW_SETTING = stepsFromDB.COLUMN_ROW_SETTING.Substring(1);
                            }
                        }
                        return isOk;

                    }
                    // do nothing, 
                    return true;
                default:
                    return true;
            }
        }

        private bool IsCurrentStepInSubLoop(int runId, MarsLoopRunJump subLoopJump)
        {
            if (subLoopJump == null) return false;
            return (runId >= subLoopJump.loopBeginRunOrder) && (runId<=subLoopJump.loopEndRunOrder);
        }

        /// <summary>
        /// 11/22/24Ôö¼Ó
        ///     预处理filltable，主要预处理循环变量等
        /// </summary>
        /// <param name="itmStp"></param>
        /// <exception cref="NotImplementedException"></exception>
        private bool PreProcessKeywordFillTable(ExecutableTestCaseStep itmStp, MarsLoopRunJump loopInfo,ref string strError, ref string strAdv, ref string strStack)
        {            
            Logger.logBegin("PreProcessKeywordFillTable", itmStp==null?"No step info, NULL":$"{itmStp.Keyword}({itmStp.GetPara()})|{itmStp.GetData()})|currrentId|{loopInfo.CurrentIdx}|{loopInfo.loopIterationCount}");
            try
            {
                string para = itmStp.GetPara();
                if (string.IsNullOrEmpty(para)) return true;
                
                if (para.Trim().StartsWith(SystemConstant.CNST_RESERVED_BYLOOPITERATION))
                {
                    ///说明是使用 SystemConstant.CNST_RESERVED_BYLOOPITERATION
                    ///
                    if (loopInfo == null)
                    {
                        strError = $"parameter|{SystemConstant.CNST_RESERVED_BYLOOPITERATION}| should be inside of a loop";
                        strAdv = $"Please check test case, and ensure that current test step with |{SystemConstant.CNST_RESERVED_BYLOOPITERATION}| is inside a loop or reset the parameter if no Loop is required";
                        strStack = MarsErrorStacks.StackTraceDump();
                        Logger.Error("PreProcessKeywordFillTable", $"{strError}|\r\n{strStack}");
                        return false;
                    }
                    int firstPartReplacePos = para.IndexOf(";");
                    if (firstPartReplacePos == -1)
                    {
                        strError = $"for |{SystemConstant.CNST_RESERVED_BYLOOPITERATION}| mode, format is |{SystemConstant.CNST_RESERVED_BYLOOPITERATION};.....";
                        strAdv = $"please change the format for |{SystemConstant.CNST_RESERVED_BYLOOPITERATION}|.";
                        strStack = MarsErrorStacks.StackTraceDump();
                        Logger.Error("PreProcessKeywordFillTable", $"{strError}|\r\n{strStack}");
                        return false;
                    }
                    
                    /// 从loop数据中获取循环数据，然后替换该模式
                    /// 
                    if (loopInfo.loopVarMode == MarsVar_Type.VarT_userMemIteration)
                    {
                        string strNewPara = para.Substring(firstPartReplacePos);
                        para = $"{SystemConstant.CNST_RESERVED_BYLOOPITERATION}:{loopInfo.CurrentIdx}{strNewPara}";
                        Logger.Debug("PreProcessKeywordFillTable", $"going to replace current idx to |{loopInfo.CurrentIdx}|new para|{para}");
                        itmStp.StepsFromDB.COLUMN_ROW_SETTING = para;
                        return true;
                    }
                    else
                    {
                        Logger.Info("PreProcessKeywordFillTable", $"{loopInfo.loopVarMode} is not supported");
                        return true ;
                    }
                }
                else
                {
                    Logger.Info("PreProcessKeywordFillTable",$"para|{para},not starts with|{SystemConstant.CNST_RESERVED_BYLOOPITERATION}");
                    /// 暂时没有其他的
                    return true;
                }
            }
            finally
            {
                Logger.logEnd("PreProcessKeywordFillTable");
            }
        }

        private bool prepareRemoveVariable(string strCurrentData, ExecutableTestCaseStep itmStp, MarsLoopRunJump loopJump, ref string strError,
            ref MarsVarBasic targetVarInfo,
            ref int innerIdx)
        {
            Logger.logBegin("prepareRemoveVariable", strCurrentData);
            /// 首先找到该step的数据信息是那个变量
            /// 
            if (itmStp == null)
            {
                strError = "Parameter is null";
                return false;
            }
            if (itmStp.StepData == null)
            {
                strError = "No step data";
                return false;
            }
            StatusVariablePara statusVariable = StatusVariablePara.GetVariableParaInst(itmStp.StepData.DATA_VALUE);
            if (statusVariable == null)
            {
                strError = "Format of status var is wrong.";
                return false;
            }
            /// 依据jump的信息修改指定值的状态
            /// 
            bool isFind = false;
            //int innerIdx = -1;
            targetVarInfo = MarsVariableTable.getVariableDetailByIdx(statusVariable.AliasOfVar, loopJump.CurrentIdx, ref isFind, ref strError, ref innerIdx);
            if (!isFind) return false;
            return true;
        }

        private static T Clone<T>(T obj)
        {
            T ret = default(T);
            if (obj != null)
            {
                XmlSerializer cloner = new XmlSerializer(typeof(T));
                MemoryStream stream = new MemoryStream();
                cloner.Serialize(stream, obj);
                stream.Seek(0, SeekOrigin.Begin);
                ret = (T)cloner.Deserialize(stream);
            }
            return ret;
        }
        private bool DealCheckErrorByAuto(List<ExecutableTestCaseStep> lstTestSteps, string strCurrentKeyword, int runId, string strMode,
            com.Mars.Constants.Mars_applicationTyp.MARS_APPTYPE appTyp,
            AutoErrorCheck autoCheckErrorSet,
            ref string strError,
            ref string strAdv,
            ref string strStack,
            ref string strSnapshotPath,
            ref string strDataReturned,
            IMonitorService mntrCnt = null,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("DealCheckErrorByAuto", $"------currentKeyword:{strCurrentKeyword}, runOrd:{runId}, " +
                $"autoCheckInfo.object:{autoCheckErrorSet.checkErrorQuickAccess}, is to Autochck:{autoCheckErrorSet.IsAutoErrorChck} ");
            //判断是不是需要autocheckError
            if (!autoCheckErrorSet.isKeyRequiresAutoCheck(strCurrentKeyword)) return true;
            //获取该对象的pegwindow
            var pegTarget = lstTestSteps
                .Where(p => (p.RunId < runId) && (string.Compare(p.Keyword, "pegwindow", true) == 0))
                .LastOrDefault();
            if (pegTarget == null)
            {
                return true;
            }
            var currentStep = lstTestSteps
                .Where(p => (p.RunId == runId))
                .LastOrDefault();
            if (currentStep == null)
            {
                return false;
            }
            //创建一个fake的B_V_OBJECT_SNAPSHOT 
            MARSDealResult dealResult = new MARSDealResult();
            ExecutableTestCaseStep tmpCheckErrorStep = new ExecutableTestCaseStep();

            tmpCheckErrorStep.StepsFromDB = new V_TEST_STEPS_FULLVISIONDTO();

            tmpCheckErrorStep.StepObject = currentStep.StepObject == null ? null : currentStep.StepObject.ShallowCopy();
            tmpCheckErrorStep.StepObject.QUICK_ACCESS = AutoCheckErrorSet.checkErrorQuickAccess;
            tmpCheckErrorStep.StepObject.TYPE_NAME = "SwfTree";
            tmpCheckErrorStep.StepsFromDB.APPLICATION_ID = currentStep.StepsFromDB.APPLICATION_ID;
            tmpCheckErrorStep.StepsFromDB.COLUMN_ROW_SETTING = currentStep.StepsFromDB.COLUMN_ROW_SETTING;
            tmpCheckErrorStep.StepsFromDB.COMMENTINFO = currentStep.StepsFromDB.COLUMN_ROW_SETTING;
            tmpCheckErrorStep.StepsFromDB.ENUM_TYPE = currentStep.StepsFromDB.ENUM_TYPE;
            tmpCheckErrorStep.StepsFromDB.IS_RUNNABLE = currentStep.StepsFromDB.IS_RUNNABLE;
            tmpCheckErrorStep.StepsFromDB.KEY_WORD_ID = KeywordOpForGUI.cnst_autocheckError_id;// currentStep.StepsFromDB.KEY_WORD_ID;
            tmpCheckErrorStep.StepsFromDB.KEY_WORD_NAME = KeywordOpForGUI.cnst_autoCheckError;
            tmpCheckErrorStep.StepsFromDB.OBJECT_HAPPY_NAME = currentStep.StepsFromDB.OBJECT_HAPPY_NAME;
            tmpCheckErrorStep.StepsFromDB.OBJECT_ID = currentStep.StepsFromDB.OBJECT_ID;
            tmpCheckErrorStep.StepsFromDB.OBJECT_NAME_ID = currentStep.StepsFromDB.OBJECT_NAME_ID;
            tmpCheckErrorStep.StepsFromDB.OBJECT_TYPE = currentStep.StepsFromDB.OBJECT_TYPE;
            tmpCheckErrorStep.StepsFromDB.QUICK_ACCESS = AutoCheckErrorSet.checkErrorQuickAccess;// currentStep.StepsFromDB.QUICK_ACCESS;
            tmpCheckErrorStep.StepsFromDB.RUN_ORDER = currentStep.StepsFromDB.RUN_ORDER;
            tmpCheckErrorStep.StepsFromDB.STEPS_ID = currentStep.StepsFromDB.STEPS_ID;
            tmpCheckErrorStep.StepsFromDB.TEST_CASE_ID = currentStep.StepsFromDB.TEST_CASE_ID;
            tmpCheckErrorStep.StepsFromDB.TEST_CASE_NAME = currentStep.StepsFromDB.TEST_CASE_NAME;
            tmpCheckErrorStep.StepsFromDB.TYPE_NAME = currentStep.StepsFromDB.TYPE_NAME;
            tmpCheckErrorStep.StepsFromDB.VALUE_SETTING = currentStep.StepsFromDB.VALUE_SETTING;
            tmpCheckErrorStep.StepData = currentStep.StepData;
            tmpCheckErrorStep.Comment = currentStep.Comment;

            string strParaWithFunc = "";
            string strWriteBackName = "",
                    strVarType = "",
                    strActualInput = "";

            bool isVar = false, isSkpped = false,
            isOk = KeywordOpAgent.DoTestStep(tmpCheckErrorStep.StepObject, tmpCheckErrorStep.StepData,
                tmpCheckErrorStep.StepsFromDB,
                        strMode,
                        appTyp,
                        autoCheckErrorSet,
                        ref strParaWithFunc,
                        ref strError,
                        ref strWriteBackName,
                        ref strDataReturned,
                        ref strVarType,
                        ref strActualInput,
                        ref isVar,
                        ref isSkpped,
                        ref strAdv,
                        ref strStack,
                        ref strSnapshotPath,
                        false,
                        strDBIdx);
            if ((!string.IsNullOrEmpty(strDataReturned)) && (!string.IsNullOrEmpty(strSnapshotPath)) && (System.IO.File.Exists(strSnapshotPath)))
                strAdv += strDataReturned;
            Console.WriteLine($"DealCheckErrorByAuto return {isOk} with strError:{strError}");
            return isOk;
        }

        private void DealWithPreView(List<ExecutableTestCaseStep> lstTestStps,
            List<ExecutableTestCaseStep> lstNxtTestStps,
            string strCurrentKeyword,
            int runId, string strMode,
            com.Mars.Constants.Mars_applicationTyp.MARS_APPTYPE appTyp,
            IMonitorService mntrCnt = null,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName
            )
        {
            Logger.logBegin("DealWithPreView", string.Format("run order is [{0}] keyword is:[{1}]", runId, strCurrentKeyword));
            if (!IsPreviewEnableKeyword(strCurrentKeyword == null ? "" : strCurrentKeyword.ToUpper())) return; //过滤非界面操作的keyword

            try
            {
                bool isFromNextStps = false;
                var nextPreviewStep = lstTestStps.Where(p => (p != null) && (p.RunId > runId))
                    .Where(p => (IsPreviewEnableKeyword(p.Keyword.ToUpper())))
                    .Where((p =>
                       ((p.StepObject != null))
                           || ((string.Compare("LaunchApplication", p.Keyword, true) == 0) && (p.StepObject == null))
                           || ((string.Compare("ClickMenuIcon", p.Keyword, true) == 0) && (p.StepObject == null))
                           || ((string.Compare(KeywordOpForGUI.cnst_selectMenuItem, p.Keyword, true) == 0) && (p.StepObject == null))
                      )
               ).FirstOrDefault();
                if (nextPreviewStep == null)
                {
                    //可能是最后一个teststp， 也可能是没有其他的，
                    if (lstNxtTestStps == null) return;
                    nextPreviewStep = lstNxtTestStps
                     .Where(p => (IsPreviewEnableKeyword(p.Keyword.ToUpper())))
                     .Where((p =>
                        ((p.StepObject != null))
                            || ((string.Compare("LaunchApplication", p.Keyword, true) == 0) && (p.StepObject == null))
                            || ((string.Compare("ClickMenuIcon", p.Keyword, true) == 0) && (p.StepObject == null))
                            || ((string.Compare(KeywordOpForGUI.cnst_selectMenuItem, p.Keyword, true) == 0) && (p.StepObject == null))
                       )
                      ).FirstOrDefault();
                    if (nextPreviewStep == null) return;
                    isFromNextStps = true;
                }
                //获取该对象的pegwindow
                var pegTarget = isFromNextStps ?
                    lstNxtTestStps
                    .Where(p => (p.RunId <= nextPreviewStep.RunId) && (string.Compare(p.Keyword, "pegwindow", true) == 0))
                    .LastOrDefault() :
                    lstTestStps
                    .Where(p => (p.RunId < nextPreviewStep.RunId) && (string.Compare(p.Keyword, "pegwindow", true) == 0))
                    .LastOrDefault();
                if (pegTarget == null)
                {
                    return;
                }
                if (
                        ((string.Compare(nextPreviewStep.Keyword, KeywordOpForGUI.cnst_launchApplication, true) == 0)
                        && ((nextPreviewStep.StepObject == null) || (string.IsNullOrEmpty(nextPreviewStep.StepObject.QUICK_ACCESS))))
                        || ((string.Compare(nextPreviewStep.Keyword, KeywordOpForGUI.cnst_clickMunuIcon, true) == 0)
                        && ((nextPreviewStep.StepObject == null) || (string.IsNullOrEmpty(nextPreviewStep.StepObject.QUICK_ACCESS))))
                        || ((string.Compare(nextPreviewStep.Keyword, KeywordOpForGUI.cnst_selectMenuItem, true) == 0)
                        && ((nextPreviewStep.StepObject == null) || (string.IsNullOrEmpty(nextPreviewStep.StepObject.QUICK_ACCESS))))
                    )
                {
                    nextPreviewStep.StepObject = new B_V_OBJECT_SNAPSHOT();
                    nextPreviewStep.StepObject.QUICK_ACCESS = "swfname:=_toolbarsDockAreaTop.*\r\nindex:=0";
                    nextPreviewStep.StepObject.PEG_QUICK_ACCESS = pegTarget.StepObject.QUICK_ACCESS;
                    //bool isOk = false;
                    //Dictionary<string, string> dictObjProperties = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(nextPreviewStep.StepObject.QUICK_ACCESS, ref isOk);
                    //if (!isOk)
                    //    strError = "Format for object is wrong";
                    nextPreviewStep.StepObject.TYPE_NAME = "swfToolBar";
                }

                if (mntrCnt != null)
                {
                    mntrCnt.BeforeClientRunTestStepEvent(ExecutableTestCaseStep.convertToWcfData(nextPreviewStep));
                    mntrCnt.AfterClientRunTestStepEvent("", 2, "");
                }

                //创建一个fake的B_V_OBJECT_SNAPSHOT 
                string strError = "", strAdv = "", strStack = "";
                MARSDealResult dealResult = new MARSDealResult();
                //isOk = KeyWordsOPForNonGUI.RunKeywordByKeywordName(stepsFromDB.STEPS_ID,
                //            strKeyword, stepObject, stepsFromDB.COLUMN_ROW_SETTING,
                //            strActualInput, //stepData==null?"":stepData.DATA_VALUE, 
                //            ref strError,
                //            ref dealResult);
                Logger.Info("DealWithPreView", string.Format("para for preview:[step object:{0}]", nextPreviewStep.StepObject));
                string strWriteBackName = "",
                    strDataReturned = "",
                    strVarType = "",
                    strActualInput = "";
                bool isVar = false, isSkpped = false;

                ExecutableTestCaseStep tmpPreViewStep = new ExecutableTestCaseStep();

                tmpPreViewStep.StepsFromDB = new V_TEST_STEPS_FULLVISIONDTO();
                //tmpPreViewStep = Clone<ExecutableTestCaseStep>(nextPreviewStep);
                //tmpPreViewStep.StepData = Clone<TEST_DATA_SETTINGDTO>(nextPreviewStep.StepData);
                //tmpPreViewStep.StepsFromDB = Clone<V_TEST_STEPS_FULLVISIONDTO>(nextPreviewStep.StepsFromDB);
                //tmpPreViewStep.StepsFromDB.KEY_WORD_NAME = KeywordOpForGUI.cnst_previewobject;
                tmpPreViewStep.StepObject = nextPreviewStep.StepObject;
                tmpPreViewStep.StepsFromDB.APPLICATION_ID = nextPreviewStep.StepsFromDB.APPLICATION_ID;
                tmpPreViewStep.StepsFromDB.COLUMN_ROW_SETTING = nextPreviewStep.StepsFromDB.COLUMN_ROW_SETTING;
                tmpPreViewStep.StepsFromDB.COMMENTINFO = nextPreviewStep.StepsFromDB.COLUMN_ROW_SETTING;
                tmpPreViewStep.StepsFromDB.ENUM_TYPE = nextPreviewStep.StepsFromDB.ENUM_TYPE;
                tmpPreViewStep.StepsFromDB.IS_RUNNABLE = nextPreviewStep.StepsFromDB.IS_RUNNABLE;
                tmpPreViewStep.StepsFromDB.KEY_WORD_ID = nextPreviewStep.StepsFromDB.KEY_WORD_ID;
                tmpPreViewStep.StepsFromDB.KEY_WORD_NAME = KeywordOpForGUI.cnst_previewobject;
                tmpPreViewStep.StepsFromDB.OBJECT_HAPPY_NAME = nextPreviewStep.StepsFromDB.OBJECT_HAPPY_NAME;
                tmpPreViewStep.StepsFromDB.OBJECT_ID = nextPreviewStep.StepsFromDB.OBJECT_ID;
                tmpPreViewStep.StepsFromDB.OBJECT_NAME_ID = nextPreviewStep.StepsFromDB.OBJECT_NAME_ID;
                tmpPreViewStep.StepsFromDB.OBJECT_TYPE = nextPreviewStep.StepsFromDB.OBJECT_TYPE;
                tmpPreViewStep.StepsFromDB.QUICK_ACCESS = nextPreviewStep.StepsFromDB.QUICK_ACCESS;
                tmpPreViewStep.StepsFromDB.RUN_ORDER = nextPreviewStep.StepsFromDB.RUN_ORDER;
                tmpPreViewStep.StepsFromDB.STEPS_ID = nextPreviewStep.StepsFromDB.STEPS_ID;
                tmpPreViewStep.StepsFromDB.TEST_CASE_ID = nextPreviewStep.StepsFromDB.TEST_CASE_ID;
                tmpPreViewStep.StepsFromDB.TEST_CASE_NAME = nextPreviewStep.StepsFromDB.TEST_CASE_NAME;
                tmpPreViewStep.StepsFromDB.TYPE_NAME = nextPreviewStep.StepsFromDB.TYPE_NAME;
                tmpPreViewStep.StepsFromDB.VALUE_SETTING = nextPreviewStep.StepsFromDB.VALUE_SETTING;
                tmpPreViewStep.StepData = nextPreviewStep.StepData;
                tmpPreViewStep.Comment = nextPreviewStep.Comment;

                string strParaWithFunc = "", strSnapshotPath = "";
                bool isOk = KeywordOpAgent.DoTestStep(tmpPreViewStep.StepObject, tmpPreViewStep.StepData, tmpPreViewStep.StepsFromDB,
                            strMode,
                            appTyp,
                            AutoCheckErrorSet,
                            ref strParaWithFunc,
                            ref strError,
                            ref strWriteBackName,
                            ref strDataReturned,
                            ref strVarType,
                            ref strActualInput,
                            ref isVar,
                            ref isSkpped,
                            ref strAdv,
                            ref strStack,
                            ref strSnapshotPath,
                            true,
                            strDBIdx);

                //bool isOk = KeywordOpForGUI.RunKeywordByKeywordName(
                //        nextPreviewStep.StepData.STEPS_ID,
                //        KeywordOpForGUI.cnst_previewobject,
                //        nextPreviewStep.StepObject,
                //        nextPreviewStep.Row_Column,
                //        nextPreviewStep.DATA_VALUE,
                //        ref strError,
                //        ref dealResult);
                ConsoleLog.IntimeLog("preview object result is [{0}]", isOk + "");
                if (mntrCnt != null)
                {
                    mntrCnt.AfterClientRunTestStepEvent("", 2, "");
                }
            }
            catch (Exception e)
            {
                string strError = "";
                ConsoleLog.IntimeLog_keywordSub("Keyword:[PreviewObject] Failed, with Exception:[{0}] stackTrace:{1}", strError = e.Message, e.StackTrace);
                //Console.WriteLine("Exception :[{0}] stack:[{1}]",e.Message, e.StackTrace);
                return;
            }
        }

        private bool IsPreviewEnableKeyword(string keyword)
        {
            var previewKeyChecks = KeywordOpForGUI.GUIKeyword
                .Select(p => p.Key)
                .Where(p => (string.Compare(p, keyword, true) == 0)
                //&&(string.Compare("pegwindow", p, true)!=0)   如果是peg，需要判断是否有一个edit等ready                
                && (string.Compare("dismiss", p, true) != 0) //不处理dismiss
                )
                .FirstOrDefault();
            return previewKeyChecks != null;
        }

        private bool IsIfElseJumpSkip(int runOrder, List<MarsIfElseRunJump> ifelseSkipStepsId)
        {

            try
            {
                if (ifelseSkipStepsId == null) return false;
                if (ifelseSkipStepsId.Count <= 0) return false;

                foreach (var itm in ifelseSkipStepsId)
                {
                    if (itm == null) continue;
                    if ((runOrder >= itm.stepId) && (runOrder <= itm.endStepsId))
                    {
                        Logger.Info("IsIfElseJumpSkip", $"runorder {runOrder} in '{itm.KeywordName}'-[{itm.stepId}-{itm.endStepsId}]");
                        return itm.isSkip ?? false;
                    }
                    Logger.Info("IsIfElseJumpSkip", $"runorder {runOrder} in '{itm.KeywordName}'-[{itm.stepId}-{itm.endStepsId}], [{itm.isSkip}]");
                }
                return false;
                /*
                if (ifelseSkipStepsId[0].isSkip == true)
                {
                    if ((runOrder >= ifelseSkipStepsId[0].stepId) 
                        && (runOrder <= ifelseSkipStepsId[0].endStepsId)) 
                        return isReturn = true;
                }
                else
                {
                    if (ifelseSkipStepsId.Count == 2)
                    {
                        return isReturn = false;
                    }
                    if ((runOrder >= ifelseSkipStepsId[1].stepId) && (runOrder <= ifelseSkipStepsId[1].endStepsId))
                        return isReturn = true;
                }

                return isReturn = false;
                */
            }
            finally
            {
                //if (isUseIf)
                //    Logger.Info("IsIfElseJumpSkip", );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mntrCnt"></param>
        /// <param name="lstTestStps"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal bool MatchObjectQuickAccessInfo(IMonitorService mntrCnt, long lAppId, List<ExecutableTestCaseStep> lstTestStps,
            bool isShowErrorDialog,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            Logger.logBegin("MatchObjectQuickAccessInfo", string.Format("Test steps to match:[{0}]", lstTestStps == null ? 0 : lstTestStps.Count));
            ///算法：
            /// 1，判断对象的cache中是否存在 applicationid的cache，如果没有 从数据库中导入 并且load到内存中
            /// 2，从内存中获取数据到ExecutableTestCaseStep中的quick Access中
            /// 
            bool isOk = false;
            //#if !_forWebClient
            List<B_V_OBJECT_SNAPSHOT> lstObjInfo = TestObjectManagement.GetObjectInfoByAppId(lAppId, ref isOk, ref strError,
                ref strAdv, ref strStack, isShowErrorDialog, this.currentDBIdx);
            //#else
            //            List<B_V_OBJECT_SNAPSHOT> lstObjInfo = (new MarsRESTfulApiClient()).GetObjectInfoByAppId(lAppId, ref isOk, ref strError);
            //#endif
            if (!isOk) return false;
            ///判断是否每个测试用例存在对应的object 信息 如果不存在 说明存在某些对象没有数据
            /// 
            ExecutableTestCaseStep currentPegStep = null;
            B_V_OBJECT_SNAPSHOT currentPeg = null;

            //foreach (var itmStp in lstTestStps)
            for (int i = 0; i < lstTestStps.Count; i++)
            {
                var itmStp = lstTestStps[i];
                if (itmStp == null) continue;
                bool isPeg = B_KEYWORD.IsKeywordPegwindow(itmStp.KeywordId, ref strError, ref isOk, this.currentDBIdx);
                if (isPeg)//peg window
                {
                    currentPegStep = itmStp;

                    var objStp = from q in lstObjInfo
                                 where q.OBJECT_NAME_ID == itmStp.Object_Name_Id
                                 && q.OBJECT_HAPPY_NAME == q.PEG_NAME
                                 select q;

                    if (objStp.FirstOrDefault() == null)
                    {
                        Logger.Error("MatchObjectQuickAccessInfo", strError = string.Format("Such object doesn't exists:objectName|{0}|",
                            itmStp.ObjectName));
                        strAdv = $"Please verify that the object|{itmStp.ObjectName}| is present in the test application.";
                        strStack = MarsErrorStacks.StackTraceDump();
                        if (mntrCnt != null)
                        {
                            mntrCnt.OnClientTestCompilerEndEvent(true, strError, new List<TestStep4Services>() { ExecutableTestCaseStep.convertToWcfData(itmStp) });
                        }
                        return false;
                    }
                    currentPeg = itmStp.StepObject = objStp.FirstOrDefault();
                    if (itmStp.RuntimeObj != null)
                    {
                        if (itmStp.StepObject == null)
                        {
                            itmStp.StepObject = itmStp.RuntimeObj;
                        }
                        else
                        {
                            Logger.Info("\t", string.Format("runtime object detected:[{0}], object identification would change from:[{1}] to [{2}]",
                                currentPeg.PEG_NAME, currentPeg.QUICK_ACCESS, itmStp.RuntimeObj.QUICK_ACCESS
                                ));
                            itmStp.StepObject.QUICK_ACCESS = itmStp.RuntimeObj.QUICK_ACCESS;
                        }
                    }
                }
                else
                {

                    bool isNotRequireObjKeyword = B_KEYWORD.IsKeywordNotRequireObject(itmStp.KeywordId, ref isOk, this.currentDBIdx);
                    if (!isOk)
                    {
                        ///no such keyword
                        /// 
                        Logger.Error("MatchObjectQuickAccessInfo", strError = string.Format("No such Keyword:[{0}]", itmStp.Keyword));
                        strAdv = $"Please verify that the keyword|{itmStp.Keyword}| is available.";
                        strStack = MarsErrorStacks.StackTraceDump();
                        return false;
                    }
                    if (isNotRequireObjKeyword && (itmStp.Object_Name_Id == -1)) continue;
                    if (APIEngineHelper.IsKeywordAPIIntegrated(itmStp.Keyword)) continue;
                    var o = (from os in lstObjInfo
                             where os.OBJECT_TYPE == currentPeg.PEG_NAME
                             && os.OBJECT_NAME_ID == itmStp.Object_Name_Id
                             select os).FirstOrDefault();
                    if (o == null)
                    {
                        isOk = B_KEYWORD.ExceptForKeywordWithoutObj(itmStp.Keyword, itmStp.Row_Column, ref strError);
                        if (!isOk)
                        {
                            Logger.Error("MatchObjectQuickAccessInfo", strError = string.Format("Such object doesn't exists:[object Name|{0}|] for testcase|{1}|run order|{2}|Try to make sure the object exists in application",
                                itmStp.ObjectName,
                                //itmStp.StepsFromDB==null?-1:itmStp.StepsFromDB.TEST_CASE_ID,
                                itmStp.StepsFromDB == null ? "NULL" : itmStp.StepsFromDB.TEST_CASE_NAME,
                                itmStp.RunId));
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = $"Please verify that the object|{itmStp.ObjectName}| is present in the test application.";
                            if (mntrCnt != null)
                            {
                                mntrCnt.OnClientTestCompilerEndEvent(true, strError, new List<TestStep4Services>() { ExecutableTestCaseStep.convertToWcfData(itmStp) });
                            }
                            return false;
                        }
                    }
                    itmStp.StepObject = o;
                    if ((currentPegStep != null) && (currentPegStep.RuntimeObj != null))
                    {
                        if (itmStp.StepObject != null)
                        {
                            itmStp.StepObject.PEG_QUICK_ACCESS = currentPegStep.RuntimeObj.QUICK_ACCESS;
                        }
                    }
                    Logger.Info("\t", string.Format("keyword:[{0}] HappName:[{4}] run order:[{1}] peg:[{2}] obj:[{3}] objectId:[{5}]",
                        itmStp.Keyword,
                        itmStp.RunId,
                        o == null ? "N/A" : o.PEG_QUICK_ACCESS,
                        o == null ? "N/A" : o.QUICK_ACCESS,
                        o == null ? "N/A" : o.OBJECT_HAPPY_NAME,
                        o == null ? -1 : o.OBJECT_ID));
                }
            }
            return true;

        }

        internal bool MatchRunTimeObjects(long lAppId, IMonitorService mntrCnt, List<ExecutableTestCaseStep> lstTestStps,bool isShowErrorDialog,
            ref string strError, ref string strAdv, ref string strStack)
        {
            if (lstTestStps == null)
            {
                Logger.logBegin("MatchRunTimeObjects", $"appid:{lAppId}, TestStep is null");
            }
            else
                Logger.logBegin("MatchRunTimeObjects", $"appid:{lAppId}, stepsCount:{lstTestStps.Count}");
            try
            {
                if (lstTestStps == null) return true;
                bool isOk = false;
                //#if !_forWebClient
                List<B_V_OBJECT_SNAPSHOT> lstObjInfo = TestObjectManagement.GetObjectInfoByAppId(lAppId,
                    ref isOk, ref strError, ref strAdv, ref strStack, isShowErrorDialog,
                    this.currentDBIdx);
                //#else
                //                List<B_V_OBJECT_SNAPSHOT> lstObjInfo = (new MarsRESTfulApiClient()).GetObjectInfoByAppId(lAppId, ref isOk, ref strError);
                //#endif
                if (!isOk) return false;

                var runtimeObjs = (from p in lstTestStps
                                   where (p.StepData != null)
                                   && (!string.IsNullOrEmpty(p.StepData.DATA_VALUE))
                                   && p.StepData.DATA_VALUE.StartsWith(B_TEST_DATA_SETTING.CNST_ENHANCE_PEG_RUNTIME_PREFIX)
                                   select new
                                   {
                                       runTimeObjName = p.StepData.DATA_VALUE.Substring(B_TEST_DATA_SETTING.CNST_ENHANCE_PEG_RUNTIME_PREFIX.Length),
                                       stpObj = p
                                   })
                                  .GroupBy(x => x.runTimeObjName)
                                  .ToDictionary(z => z.Key, z => z.ToList());

                foreach (var itm in runtimeObjs.Keys)
                {
                    if (string.IsNullOrEmpty(itm)) continue;
                    var lstStepsFromRuntimeObjDic = runtimeObjs[itm];
                    if (lstStepsFromRuntimeObjDic == null) continue;

                    ///get peg object information from 
                    var objPegToReplace = lstObjInfo
                        .Where(p => (p != null) && (p.OBJECT_HAPPY_NAME == p.OBJECT_TYPE) // makesure that is a peg window
                            && string.Compare(p.OBJECT_HAPPY_NAME, itm, true) == 0)
                        .FirstOrDefault();
                    if (objPegToReplace == null)
                    {
                        strError = string.Format("no such peg runtime object [{0}] in application:[{1}]", itm, lAppId);
                        strAdv = "";
                        StackFrame stck = new StackFrame();
                        strStack = $"at File:{stck.GetFileName()}, {stck.GetMethod()} {stck.GetFileLineNumber()}";
                        return false;
                    }
                    foreach (var itmStp in lstStepsFromRuntimeObjDic)
                    {
                        if (itmStp == null) continue;

                        itmStp.stpObj.RuntimeObj = objPegToReplace;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("MatchRunTimeObjects", strError = string.Format("Exception:[{0}], \r\n{1}", e.Message, e.StackTrace), e);
                strAdv = "";
                StackFrame stck = new StackFrame();
                strStack = $"at File:{stck.GetFileName()}, {stck.GetMethod()} {stck.GetFileLineNumber()}";
                return false;
            }
            finally
            {
                Logger.logEnd("MatchRunTimeObjects");
            }
        }

        internal bool InstallDataSetToTestStep(IMonitorService mntrCnt, long? lDBSetId,
            long lTestCase, List<ExecutableTestCaseStep> lstTestStps,
            string strTestMode,
            ref string strError)
        {
            Logger.logBegin("InstallDataSetToTestStep", string.Format("Dataset Id:[{0}] Test caseId:[{1}]", lDBSetId, lTestCase));
            try
            {
                if (lDBSetId == null) return true;
                bool isOk = false;
#if !_forWebClient
                IList<KeyValuePair<long?, TEST_DATA_SETTINGDTO>> lstDataWithStp = B_TEST_DATA_SETTING.GetTestDataByTestCaseIDAndDataSetId(lTestCase, lDBSetId ?? -1,this.currentDBIdx);
#else
                IList<KeyValuePair<long?, TEST_DATA_SETTINGDTO>> lstDataWithStp = (new MarsRESTfulApiClient(this.currentDBIdx)).GetTestDataByTestCaseIDAndDataSetId(lTestCase, lDBSetId ?? -1, ref isOk, ref strError);
#endif
                KeyValuePair<long?, TEST_DATA_SETTINGDTO> tmpData = default(KeyValuePair<long?, TEST_DATA_SETTINGDTO>);

                List<ExecutableTestCaseStep> lstToRefreshData = new List<ExecutableTestCaseStep>();

                foreach (var itm in lstTestStps)
                {
                    if (itm == null) continue;
                    itm.StepData = null; /// set to default ;
                    if ((tmpData = lstDataWithStp.Where(l => l.Key == itm.TestStepId).FirstOrDefault()).Equals(default(KeyValuePair<long?, TEST_DATA_SETTINGDTO>))) continue;
                    itm.StepData = tmpData.Value;
                }

                ///将在test step时候处理
                ///
                #region variable for global, local and modal
                /*
                List<string> lstGlobal = lstTestStps.Where(p => p.ISDataNeedRefresh() && p.IsDataValueStarsWith(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL))
                    //.Select(p=>p.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL.Length+1))
                    .Select(p => p.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL.Length + 1))
                    .ToList();
                List<string> lstModal = lstTestStps.Where(p => p.ISDataNeedRefresh() && p.IsDataValueStarsWith(SystemConstant.CNST_RESERVED_VARIABLE_MODAL))
                    //.Select(p => p.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_MODAL.Length + 1))
                    .Select(p => p.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_MODAL.Length + 1))
                    .ToList();
                List<string> lstLocal = lstTestStps.Where(p => p.ISDataNeedRefresh() && p.IsDataValueStarsWith(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL))
                    //.Select(p => p.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL.Length + 1))
                    .Select(p => p.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL.Length + 1))
                    .ToList();

                //Dictionary<string, List<TEST_DATA_SETTINGDTO>> lstStepsIdWithRuntimePeg = lstTestStps
                //    .Where(p => (string.Compare(p.Keyword, "Pegwindow", true) == 0) && (B_TEST_DATA_SETTING.IsRuntimeObj(p.StepData)))
                //    .GroupBy(p => p.StepData.DATA_VALUE)
                //    .ToDictionary(p=>p.Key, x=>x.ToList());

                Dictionary<string, string> dicVarInfo = new Dictionary<string, string>();
                if (lstGlobal.Count>0)
                {
                    if (!BoHelper.GetGlobalVariableInfo(lstGlobal, ref strError, ref dicVarInfo))
                    {
                        return false;
                    }
                    // 处理'today'等系统函数
                    //DealWithTodayAndOtherMarsGlobalFunc(lstGlobal, dicVarInfo);

                    foreach(var itm in lstTestStps.Where(p => p.ISDataNeedRefresh() && p.IsDataValueStarsWith(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL)))
                    {
                        //string strValueIdx = itm.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL.Length + 1);
                        string strValueIdx = itm.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_GLOBAL.Length + 1);

                        if (strValueIdx == null) continue;
                        if (dicVarInfo.ContainsKey(strValueIdx))
                        {
                            itm.StepData.DATA_VALUE = dicVarInfo[strValueIdx];
                        }
                    }
                }
                dicVarInfo.Clear();
                if (lstLocal.Count > 0)
                {
                    if (!BoHelper.GetLocalVariableInfo(lstLocal, ref strError, ref dicVarInfo))
                    {
                        return false;
                    }
                    foreach (var itm in lstTestStps.Where(p => p.ISDataNeedRefresh() && p.IsDataValueStarsWith(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL)))
                    {
                        //string strValueIdx = itm.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL.Length + 1);
                        string strValueIdx = itm.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_LOCAL.Length + 1);
                        if (strValueIdx == null) continue;
                        if (dicVarInfo.ContainsKey(strValueIdx))
                        {
                            itm.StepData.DATA_VALUE = dicVarInfo[strValueIdx];
                        }
                    }
                }
                dicVarInfo.Clear();
                if (lstModal.Count>0)
                {
                    short iMode = 2;
                    if (string.Compare("Base", strTestMode, true) == 0) iMode = 1;
                    if (!BoHelper.GetModalVariableInfo(lstModal, iMode, ref strError, ref dicVarInfo))
                    {
                        return false;
                    }
                    foreach (var itm in lstTestStps.Where(p => p.ISDataNeedRefresh() && p.IsDataValueStarsWith(SystemConstant.CNST_RESERVED_VARIABLE_MODAL)))
                    {
                        //string strValueIdx = itm.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_MODAL.Length + 1);
                        string strValueIdx = itm.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_MODAL.Length + 1);
                        if (strValueIdx == null) continue;
                        if (dicVarInfo.ContainsKey(strValueIdx))
                        {
                            itm.StepData.DATA_VALUE = dicVarInfo[strValueIdx];
                        }
                    }
                }
                */
                #endregion

                #region data for seq
                Dictionary<ExecutableTestCaseStep, string> lstSeq = lstTestStps.Where(p => p.ISDataNeedRefresh() && p.IsDataValueStarsWith(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ + ":"))
                    .Select(p => new
                    {
                        k = p,
                        //v = p.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ.Length + 1)
                        v = p.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ.Length + 1)
                    })
                    .ToDictionary(t => t.k, t => t.v);
                if ((lstSeq != null) && (lstSeq.Keys.Count > 0))
                {

#if !_forWebClient
                    int iN = -1;
#else
                    long iN = -1;
#endif
                    foreach (var k in lstSeq.Keys)
                    {
                        if (k == null) continue;
#if !_forWebClient
                        isOk = BoHelper.GetBussinessSeq(ref iN, ref strError);
#else
                        isOk = (new MarsRESTfulApiClient(this.currentDBIdx)).GetBussinessSeq(ref iN, ref isOk, ref strError);
#endif
                        if (!isOk)
                        {
                            Logger.Error("InstallDataSetToTestStep", strError = string.Format("Can't get seq var info for teststep id:[{0}] with error:\r\n{1}", k.TestStepId, strError));
                            return false;
                        }

                        //if ((k.StepData.DATA_VALUE!=null)&&(!k.StepData.DATA_VALUE.EndsWith("$")))
                        if ((k.DATA_VALUE != null) && (!k.DATA_VALUE.EndsWith("$")))
                        {
                            if (!k.StepData.DATA_VALUE.StartsWith(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ)) continue;

                            //string strTmpFmt = k.StepData.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ.Length + 1);
                            string strTmpFmt = k.DATA_VALUE.Substring(SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ.Length + 1);
                            if (string.IsNullOrEmpty(strTmpFmt))
                            {
                                k.StepData.DATA_VALUE = SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ + ":" + iN;
                            }
                            else
                            {
                                try
                                {
                                    k.StepData.DATA_VALUE = string.Format(strTmpFmt, iN);
                                }
                                catch (Exception e)
                                {
                                    strError = string.Format("Seq variable is in wrong format:{0}\r\n{1}", k.StepData.DATA_VALUE, e.Message);
                                    Logger.Error("InstallDataSetToTestStep", strError);
                                    return false;
                                }

                            }
                        }
                        else
                            k.StepData.DATA_VALUE = SystemConstant.CNST_RESERVED_VARIABLE_FROMSEQ + ":" + iN;
                    }
                }
                #endregion
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InstallDataSetToTestStep", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("InstallDataSetToTestStep");
            }
        }

        internal void DealWithTodayAndOtherMarsGlobalFunc(List<string> lstGlobal, Dictionary<string, string> dicVarInfo)
        {
            var today = lstGlobal.Where(p => string.Compare("today", p, true) == 0).FirstOrDefault();
            if (!string.IsNullOrEmpty(today))
            {
                var dicK = dicVarInfo.Keys.Where(p => string.Compare("today", p, true) == 0).FirstOrDefault();
                if (!string.IsNullOrEmpty(dicK))
                {
                    dicVarInfo[dicK] = DateTime.Now.ToString("MM/dd/yyyy");
                }
                else
                {
                    dicVarInfo.Add("TODAY", DateTime.Now.ToString("MM/dd/yyyy"));
                }
            }
        }

#if _forWebClient
        internal bool LoadTestCase(IMonitorService mntrCnt, long lAppId, List<ExecutableTestCaseStep> lstTestStps, ref string strError, ref string strStack,
            ref string strAdv, string strDBIdx)
#else
        internal bool LoadTestCase(IMonitorService mntrCnt, long lAppId, List<ExecutableTestCaseStep> lstTestStps, ref string strError, ref string strStack, 
            ref string strAdv,string strDBIdx)
        //internal bool LoadTestCase(IMonitorService mntrCnt, long lAppId, List<ExecutableTestCaseStep> lstTestStps, ref string strError)
#endif
        {
            Logger.logBegin("LoadTestCase", string.Format("tsId:[{0}], data set Id:[{1}]", currentTestCaseId, currentDataSetId));

            try
            {
                bool isOk = false;
                if ((!cachedTestCase.ContainsKey(currentTestCaseId))
                    || (cachedTestCase[currentTestCaseId] == null)
                    || (!cachedTestCase[currentTestCaseId].ContainsKey(lAppId)))
                {
                    //Load test case from database 
#if !_forWebClient
                    List<V_TEST_STEPS_FULLVISIONDTO> lstSteps = B_V_TEST_STEPS_FULLVISIONDTO.GetTestStepsByTestCaseID(currentTestCaseId, lAppId,
                        this.currentDBIdx);
#else

                    List<V_TEST_STEPS_FULLVISIONDTO> lstSteps = (new MarsRESTfulApiClient(strDBIdx)).GetTestStepsByTestCaseID(currentTestCaseId, lAppId,
                        ref isOk, ref strError,
                    ref strStack, ref strAdv, this.currentDBIdx);
#endif
                    if (lstSteps == null) return false;
                    //if (!isOk) return false;

                    if (lstTestStps != null)
                        lstTestStps.Clear();

                    foreach (var itm in lstSteps)
                    {
                        if (itm == null) continue;
                        lstTestStps.Add(new ExecutableTestCaseStep()
                        {
                            StepsFromDB = itm
                        });
                    }
                    if (!cachedTestCase.ContainsKey(currentTestCaseId))
                    {
                        cachedTestCase.Add(currentTestCaseId, new Dictionary<long, List<ExecutableTestCaseStep>>());
                    }
                    var tmpAppListStep = cachedTestCase[currentTestCaseId];
                    if (tmpAppListStep == null)
                        tmpAppListStep = new Dictionary<long, List<ExecutableTestCaseStep>>();
                    if (!tmpAppListStep.ContainsKey(lAppId))
                        tmpAppListStep.Add(lAppId, lstTestStps);
                    else
                    {
                        tmpAppListStep[lAppId] = lstTestStps;
                    }
                    //tmpAppListStep.Add(lAppId, lstTestStps);
                    //cachedTestCase.Add(currentTestCaseId, tmpAppListStep);
                }
                else
                {
                    var tmpDicAppTestCase = cachedTestCase[currentTestCaseId];

                    lstTestStps.Clear();
                    lstTestStps.AddRange(cachedTestCase[currentTestCaseId][lAppId]);
                    //lstTestStps = cachedTestCase[currentTestCaseId];
                }


                ///将数据放到monitor上
                /// 
                /// 
                List<TestStep4Services> lstWcfData = new List<TestStep4Services>();
                foreach (var itm in lstTestStps)
                {
                    lstWcfData.Add(ExecutableTestCaseStep.convertToWcfData(itm));
                }
                if (mntrCnt != null)
                    mntrCnt.OnClientTestCaseListChangeEvent(lstWcfData);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("LoadTestCase", string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("LoadTestCase");
            }
        }

    
    }
}
