extern alias clientWCF;
using com.Mars.Constants;
using com.Mars.TestFrame.Application;
using com.Mars.TestFrame.TestObjects;
using Mars.DataLayer;
using Mars.Dto;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls;
using MarsTestFrame.SourceCode.com.Mars.KeyWords;
using MarsTestFrame.SourceCode.com.Mars.KeyWords.KeyWordObject;
//using MarsTestFrame.SourceCode.com.Mars.QTP;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.systemUtil;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MarsTestFrame.SourceCode.com.Mars.Compiler
{
    public delegate void OnCompileOneStepEvent(ConfigObjectBase objCurrentStep) ;
    public delegate void AfterCompileOneStepEvent(ConfigObjectBase objCurrentStep, ERROR_CODE eCde, String strErrorInfo) ;
    public delegate ERROR_CODE OnCompileListEvent(List<ConfigObjectBase> lstSteps, ref int iErrorStep, ref TestStep objErrorStep, ref string strErrorInfo,string strCurrentAppShortName = null, string strCurrentPegObj = null , bool isSubMode = false,
            OnCompileOneStepEvent funcCompileOneEventImpl=null, AfterCompileOneStepEvent funcAfterCompileEventImpl=null) ;
#if _Datafrom_Database
    public delegate string OnRequireCurrentApplicationIDEvent();

    public class TargetApplicationFromdB:TargetApplicationInfo
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TargetApplicationFromdB));

        private T_REGISTERED_APPSDTO assignedApplicationDTO;

        public T_REGISTERED_APPSDTO AssignedApplicationDTO {
            get { return this.assignedApplicationDTO; }
            set { this.assignedApplicationDTO = value; }
        }

        public static TargetApplicationFromdB FetchApplicationFromDB(string strApplicationID)
        {
            Logger.logBegin("FetchApplicationFromDB");
            // Get data from Database
            long applicationID = -1;
            try
            {
                if (long.TryParse(strApplicationID, out applicationID))
                {
                    List<T_REGISTERED_APPSDTO> lstRsult = BoHelper.GetApplicationById("TEMPSTUB",applicationID);

                    if (lstRsult == null)
                    {
                        Logger.Error("FetchApplicationFromDB", string.Format("NO application info is returned, for application ID:[{0}]", strApplicationID));
                        return null;
                    }
                    if (lstRsult.Count == 0)
                    {
                        Logger.Error("FetchApplicationFromDB", string.Format("NO application info is returned, for application ID:[{0}]", strApplicationID));
                        return null;
                    }
                    return converApplicationDTO2TargetApplication(lstRsult[0]);
                }
                else
                {
                    Logger.Error("FetchApplicationFromDB", string.Format("ApplicationID:[{0}] is not a number", strApplicationID));
                    return null;
                }

            }
            finally
            {
                Logger.logEnd("FetchApplicationFromDB");
            }
           
        }

        private static TargetApplicationFromdB converApplicationDTO2TargetApplication(T_REGISTERED_APPSDTO objDTO)
        {
            if (objDTO == null) return null;
            TargetApplicationFromdB objApplicationDB = new TargetApplicationFromdB();
            objApplicationDB.AssignedApplicationDTO = objDTO;

            objApplicationDB.ApplicationShortName = objDTO.APP_SHORT_NAME;
            objApplicationDB.ApplicationType = objDTO.APPLICATION_TYPE_ID+"";
            objApplicationDB.ExtraPopupMenuCount = objDTO.EXTRAPOPUPMENU;
            objApplicationDB.ExtraRequirement = objDTO.EXTRAREQUIREMENT;
            objApplicationDB.Indentifier = objDTO.PROCESS_IDENTIFIER;
            objApplicationDB.Command = objDTO.STARTER_COMMAND;
            objApplicationDB.ObjectFilePath = "FROM_DB";
            objApplicationDB.Path = objDTO.STARTER_PATH;

            return objApplicationDB;
        }
    }

#endif
    public class TestCaseCompilerMainEntry
    {
        /*** 
         * 核心类
         * 接收steps列表
         * 验证列表是否正确
         * 生成可以自行代码
         * 
         * ***/
        private static MLogger Logger = MLogger.GetLogger(typeof(TestCaseCompilerMainEntry));
        //private QTPManagement mobjQTP = new QTPManagement();

#if _Datafrom_Database
        public OnRequireCurrentApplicationIDEvent onRequireCurrentApplicationIDHandler;
#endif

        private void AgentCompileOneStepEventHandler(ConfigObjectBase objSteps,OnCompileOneStepEvent funcOn)
        {
            if (funcOn == null) return;
            funcOn(objSteps);
        }
        private void AgentAfterCompileOneStepEvent(ConfigObjectBase objStep, AfterCompileOneStepEvent funcAfterImpl, ERROR_CODE eCde, string strErrorInfo)
        {
            if (funcAfterImpl == null) return;
            funcAfterImpl(objStep, eCde, strErrorInfo);
        }

        public ERROR_CODE preComplierTestSteps(List<ConfigObjectBase> lstSteps, ref int iErrorStep, ref TestStep objErrorStep, ref string strErrorInfo,string strCurrentAppShortName = null, string strCurrentPegObj = null , bool isSubMode = false,
            OnCompileOneStepEvent funcCompileOneEventImpl=null, AfterCompileOneStepEvent funcAfterCompileEventImpl=null)
        {
            Logger.logBegin("preComplierTestSteps");
            ERROR_CODE eCode = ERROR_CODE._NO_ERROR;

            TargetApplicationInfo objCurrentApplication = null;
            List<TestPegwindowObject> lstCurrentPegs = null;
            Stack<ConfigObjectBase> objSubActionStep = new Stack<ConfigObjectBase>();

            int iCurrentLoop = 0;
            if (lstSteps == null)
            {
                Logger.Error("preComplierTestSteps", string.Format("ErrorCode:[{0}],ErrorMsg:[{1}]", ERROR_CODE._COMPILER_NO_STEPS, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_STEPS)));
                return ERROR_CODE._COMPILER_NO_STEPS;
            }

            if (isSubMode)
            {
                objCurrentApplication = TargetApplicationsManagement.GetRegApplicationByStepValue(strCurrentAppShortName);
                if ((objCurrentApplication != null) && (!string.IsNullOrEmpty(strCurrentPegObj)))
                {
                    lstCurrentPegs = TestObjectsManagement.GetPegwindowsByValues(strCurrentPegObj, objCurrentApplication.ApplicationShortName);
                    if (lstCurrentPegs==null)
                    {
                        eCode = ERROR_CODE._COMPILER_SUBACTION_NO_PEGININFORMATION_PARA_2;
                        iErrorStep = -(int)eCode;
                        objErrorStep = null;
                        Logger.Error("preCompilerTestSteps", string.Format(ERROR_INFO.GET_ERROR_STR(eCode), strCurrentAppShortName, strCurrentPegObj));
                        return eCode;
                    }
                }
            }

            try
            {
                int iStep = 0;
                KeyWordObjectInfo objKeyFunc = null;
                
                #region For Loop
                foreach (ConfigObjectBase objStep in lstSteps)
                {
                    iStep++;
                    if (!(objStep is TestStep))
                    {
                        iErrorStep = -(int)ERROR_CODE._COMPILER_TEST_STEP_OBJECT_EXPECT;
                        objErrorStep = null;
                        Logger.Error("preComplierTestSteps", string.Format("ErrorCode:[{0}],ErrorMsg:[{1}]", ERROR_CODE._COMPILER_TEST_STEP_OBJECT_EXPECT, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_TEST_STEP_OBJECT_EXPECT)));
                        return ERROR_CODE._COMPILER_TEST_STEP_OBJECT_EXPECT;
                    }
                    if (string.IsNullOrEmpty(((TestStep)objStep).Keyword)) continue;
                    //if ((((TestStep)objStep).Keyword == null) || (((TestStep)objStep).Keyword == "")) continue;

                    AgentCompileOneStepEventHandler(objStep, funcCompileOneEventImpl);
                    ((TestStep)objStep).Loop = iCurrentLoop;
                    /*** 
                     * 获得keywords
                     * 
                     */
                    if (string.Compare(((TestStep)objStep).Keyword, SystemConstant.CNST_RESERVED_KEYWORD_SETCURRENTAPPLICATION, true) == 0)
                    {
                        /** 是否当前应用设置正确 **/
                        objCurrentApplication = TargetApplicationsManagement.GetRegApplicationByStepValue(((TestStep)objStep).Value);

                        objKeyFunc = KeyWordsMainEntry.GetInstance().GetKeyWordFunctionByName(((TestStep)objStep).Keyword);
                        if (objKeyFunc == null)
                        {
                            Logger.Error("preComplierTestSteps", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION), ((TestStep)objStep).Keyword));
                            iErrorStep = iStep;
                            objErrorStep = (TestStep)objStep;
                            strErrorInfo = string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION), ((TestStep)objStep).Keyword);
                            
                            /***Codes should move to Client Side***/
                            //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION, strErrorInfo);
                            return ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION;
                            //throw new MarsExceptions((int)ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION), ((TestStep)objStep).Keyword));
                        }
                        ((TestStep)objStep).AddApplicationInfo(objCurrentApplication.ApplicationShortName, objCurrentApplication);
                        ((TestStep)objStep).KeyWordFuntion = objKeyFunc;
                        lstCurrentPegs = null;

                        /** Code should move to Clients side **/
                        //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._NO_ERROR, "");
                        continue;
                    }
                    /** peg window **/
                    if (string.Compare(((TestStep)objStep).Keyword, SystemConstant.CNST_RESERVED_KEYWORD_PEGWINDOW, true) == 0)
                    {
                        if (objCurrentApplication == null)
                        {
                            try
                            {
#if _Datafrom_Database
                                objCurrentApplication = getCurrentApplicationInfo();
#else
                                objCurrentApplication = TargetApplicationsManagement.GetDefaultApplication();
#endif
                            }
                            catch (MarsExceptions eM)
                            {
                                Logger.Error("preCompilerTestSteps", eM.Message, eM);
                                
                                strErrorInfo = string.Format("No default Appliation, with exception:{0}", eM.Message);
                                iErrorStep = ((TestStep)objStep).RunID;
                                objErrorStep = (TestStep)objStep;

                                /** Codes should move to client side **/
                                //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._COMPILER_NO_DEFAULT_APPLICATION_INFO, string.Format("No default Appliation, with exception:{0}", eM.Message));                                
                                return ERROR_CODE._COMPILER_NO_DEFAULT_APPLICATION_INFO;
                            }
                            catch (Exception e)
                            {
                                ERROR_CODE eC = ERROR_CODE._COMPILER_UNKNOW_ERROR_GET_DEFAULT_APP;
                                Logger.Error("preComplierTestSteps", string.Format(ERROR_INFO.GET_ERROR_STR(eC), e.Message), e);

                                strErrorInfo = string.Format("No default Appliation, with exception:{0}", e.Message);
                                iErrorStep = ((TestStep)objStep).RunID;
                                objErrorStep = (TestStep)objStep;

                                /** Code should move to Client side **/
                                //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, eC, string.Format("No default Appliation, with exception:{0}", e.Message));
                                return eC;
                            }
                        }
                        lstCurrentPegs = TestObjectsManagement.GetPegwindowsByValues(((TestStep)objStep).ObjectName, objCurrentApplication.ApplicationShortName);
                        if ((lstCurrentPegs == null) || (lstCurrentPegs.Count == 0))
                        {
                            strErrorInfo = string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO), ((TestStep)objStep).ObjectName, objCurrentApplication.ApplicationShortName) ;
                            iErrorStep = ((TestStep)objStep).RunID;
                            objErrorStep = (TestStep)objStep;
                            Logger.Error("preComplierTestSteps", strErrorInfo);

                            /*** code should move to Client Side ***/
                            //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO, strErrorInfo);
                            return ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO;
                        }
                        /** runtime Pegwindow **/
                        bool isRuntimePeg = ((TestStep)objStep).Value==null?false: TestRuntimePegwindow.isRuntimePegWindow(((TestStep)objStep).Value);
                        if (isRuntimePeg)
                        {
                            string strRuntimePeg = TestRuntimePegwindow.GetRuntimePegInfo(((TestStep)objStep).Value);
                            /** get the Runtime Pegwindow info **/
                            List<TestPegwindowObject> lstRuntimePeg = TestObjectsManagement.GetPegwindowsByValues(strRuntimePeg, objCurrentApplication.ApplicationShortName);
                            if ((lstRuntimePeg == null) || (lstRuntimePeg.Count != 1))
                            {
                                Logger.Error("preComplierTestSteps", strErrorInfo = string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO), strRuntimePeg, objCurrentApplication.ApplicationShortName));
                                objErrorStep = (TestStep)objStep;
                                iErrorStep = objErrorStep.RunID;

                                /**Code should move to client side**/
                                //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO, strErrorInfo);
                                return ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO;
                            }
                            /*** set runtime infor to Pegwindow object  **/
                            for (int i = 0; i < lstCurrentPegs.Count; i++)
                            {
                                TestPegwindowObject objPeg = lstCurrentPegs[i];
                                objPeg.SetRuntimePegInfo(strRuntimePeg, lstRuntimePeg[0]);
                            }
                        }

                        objKeyFunc = KeyWordsMainEntry.GetInstance().GetKeyWordFunctionByName(((TestStep)objStep).Keyword);
                        if (objKeyFunc == null)
                        {
                            strErrorInfo = string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION), ((TestStep)objStep).Keyword);
                            objErrorStep = (TestStep)objStep ;
                            iErrorStep = objErrorStep.RunID ;

                            //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION, strErrorInfo = string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION), ((TestStep)objStep).Keyword));
                            return ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION;
                            //throw new MarsExceptions((int)ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION, strErrorInfo);
                        }
                        ((TestStep)objStep).AddApplicationInfo(objCurrentApplication.ApplicationShortName, objCurrentApplication);
                        ((TestStep)objStep).KeyWordFuntion = objKeyFunc;
#if _Datafrom_Database
                        if (string.IsNullOrEmpty(((TestStep)objStep).QuickAccess))
                        {
                            ((TestStep)objStep).QuickAccess = ((TestStepsFromDB)objStep).TestStepsFullVisionDTO.QUICK_ACCESS ;
                        }
                        
                        if (string.IsNullOrEmpty(((TestStep)objStep).Row_Column))
                            ((TestStep)objStep).Row_Column = "";

                        strErrorInfo = "";
                        eCode = ((TestStep)objStep).BuildObjectFullPath(ref strErrorInfo);
                        if (eCode != ERROR_CODE._NO_ERROR)
                        {
                            AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, eCode, string.Format("Can't Build objectFullPath for the object:{0}", ((TestStep)objStep).ObjectName));
                            objErrorStep = (TestStep)objStep;
                            iErrorStep = objErrorStep.RunID;
                            return eCode;
                        }
#endif


                        //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._NO_ERROR, "");

                        continue;
                    }

                    /*** all other test steps ***/
                    objKeyFunc = KeyWordsMainEntry.GetInstance().GetKeyWordFunctionByName(((TestStep)objStep).Keyword);
                    if (objKeyFunc == null)
                    {
                        objErrorStep = (TestStep)objStep;
                        iErrorStep = objErrorStep.RunID;
                        strErrorInfo = string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION), ((TestStep)objStep).Keyword);
                        Logger.Error("preCompiler", strErrorInfo);

                        throw new MarsExceptions((int)ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_NO_SUCH_FUNCTION), ((TestStep)objStep).Keyword));
                    }
                    
                    ((TestStep)objStep).AddApplicationInfo(objCurrentApplication == null ? null : objCurrentApplication.ApplicationShortName, objCurrentApplication);
                    ((TestStep)objStep).KeyWordFuntion = objKeyFunc;
                    ((TestStep)objStep).Pegwindows = lstCurrentPegs;

                    /*** to check wether the keyword needs object information ***/
                    eCode = ((TestStep)objStep).ValidateStepSetting();
                    if (eCode != ERROR_CODE._NO_ERROR)
                    {
                        objErrorStep = (TestStep)objStep;
                        iErrorStep = objErrorStep.RunID;
                        //AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, eCode, "Can't Validate the step setting.");
                        return eCode;
                    }
                    if (!string.IsNullOrEmpty(((TestStep)objStep).ObjectName))
                    {
                        strErrorInfo = "";
                        eCode = ((TestStep)objStep).BuildObjectFullPath(ref strErrorInfo);
                        if (eCode != ERROR_CODE._NO_ERROR)
                        {
                            AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, eCode, string.Format("Can't Build objectFullPath for the object:{0}", ((TestStep)objStep).ObjectName));
                            objErrorStep = (TestStep)objStep;
                            iErrorStep = objErrorStep.RunID;
                            return eCode;
                        }
                    }
#if _Datafrom_Database
                    if (string.IsNullOrEmpty(((TestStep)objStep).Row_Column))
                        ((TestStep)objStep).Row_Column = "";
#endif
                    bool isSubAction = IsSubActionKeyword(((TestStep)objStep).Keyword);
                    if (isSubAction)
                    {
                        /** Deal with sub actions **/
                        eCode = DealWithSubActions((TestStep)objStep, iCurrentLoop);
                        if (eCode != ERROR_CODE._NO_ERROR)
                        {
                            AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, eCode, string.Format("Can't alyst the sub-actions:{0}", ((TestStep)objStep).Keyword));
                            objErrorStep = (TestStep)objStep;
                            iErrorStep = objErrorStep.RunID;
                            return eCode;
                        }

                        objSubActionStep.Push(objStep);
                    }
#if !LoopVar
                    /** Loop deal **/
                    if (string.Compare(((TestStep)objStep).Keyword, SystemConstant.CNST_RESERVED_KEYWORD_LOOP, true) == 0)
                    {
                        AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._NO_ERROR, "");
                        iCurrentLoop++;
                    }
                    if (string.Compare(((TestStep)objStep).Keyword, SystemConstant.CNST_RESERVED_KEYWORD_ENDLOOP, true) == 0)
                    {
                        AgentAfterCompileOneStepEvent(objStep, funcAfterCompileEventImpl, ERROR_CODE._NO_ERROR, "");
                        iCurrentLoop--;
                    }
#endif
                    /** Loop deal end **/
                }
#endregion //For Loop

                /** deal with sub actions **/
                ConfigObjectBase objSubAction = null;
                while ((objSubActionStep.Count>0)&&((objSubAction = objSubActionStep.Pop()) != null))
                {
                    if (!(objSubAction is TestStep)) continue;
                    string[] strAppShortName = ((TestStep)objSubAction).GetApplicationShortName();
                    if (strAppShortName == null) continue;
                    string strPegWindowsName=null ;
                    string strCurrentError = "";
                    TestPegwindowObject[] arrCurrentPegObjects = ((TestStep)objSubAction).GetPegWindows();
                    if (arrCurrentPegObjects.Length > 0)
                    {
                        strPegWindowsName = arrCurrentPegObjects[0].ObjectName;
                    }
                    List<ConfigObjectBase> lstSubActionSteps = ((TestStep)objSubAction).GetSubActions();
                    if (lstSubActionSteps == null) continue;

                    eCode = this.preComplierTestSteps(lstSubActionSteps, ref iErrorStep, ref objErrorStep, ref strCurrentError,strAppShortName[0], strPegWindowsName, true);
                    if (eCode != ERROR_CODE._NO_ERROR) return eCode;
                }

            }
            catch (MarsExceptions eMars)
            {
                Logger.Error("preComplierTestSteps", "Exceptions:"+eMars.Message);
                return (ERROR_CODE)eMars.ErrorId;
            }
            catch (Exception e)
            {
                Logger.Error("preComplierTestSteps", e.Message);
                return ERROR_CODE._COMPILER_SUPPORTERROR_PARA_1;
            }
            Logger.logEnd("preComplierTestSteps");
            return eCode;
        }
#if _Datafrom_Database
        private TargetApplicationInfo getCurrentApplicationInfo()
        {
            Logger.logBegin("getCurrentApplicationInfo");
            if (this.onRequireCurrentApplicationIDHandler == null)
            {
                Logger.Warnning("getCurrentApplicationInfo", "no onRequireCurrentApplicationIDHandler is assigned, try to get data from configuration file");
                return TargetApplicationsManagement.GetDefaultApplication();
            }

            string strApplicationId = this.onRequireCurrentApplicationIDHandler();
            TargetApplicationInfo objApplication = TargetApplicationFromdB.FetchApplicationFromDB(strApplicationId);
            Logger.logEnd("getCurrentApplicationInfo");
            //throw new Exception("");
            return objApplication;
        }
#endif
        private ERROR_CODE DealWithSubActions(TestStep objStep,int iLoop)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (objStep == null)
            {
                eCde = ERROR_CODE._COMPILER_SUBACTION_OBJECT_ISNULL_PARA_0;
                Logger.Error("DealWithSubActions", ERROR_INFO.GET_ERROR_STR(eCde));
                return ERROR_CODE._COMPILER_SUBACTION_OBJECT_ISNULL_PARA_0;
            }
            /** check the formatter **/
            eCde = CheckSubActionsFormatter(objStep);
            if (eCde != ERROR_CODE._NO_ERROR) return eCde;

            /** Create sub actions object **/
            eCde = objStep.ParseSubAction(iLoop);

            return eCde;
        }

        private ERROR_CODE CheckSubActionsFormatter(TestStep objStep)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            switch (objStep.Keyword.ToUpper())
            {
                case SystemConstant.CNST_SUBACTION_KEYWORD_CALL:
                    /** a file name should exist with test sheet name **/
                    eCde = CheckSubActionCallFormatter(objStep.Value);
                    if (eCde != ERROR_CODE._NO_ERROR)
                        Logger.Error("CheckSubActionsFormatter", string.Format("Check Testsuite name or test sheet name exists :{0}", objStep.Value));
                    return eCde;
                case SystemConstant.CNST_SUBACTION_KEYWORD_DEALERALLOCATION:
                    string strValue = string.Format("{0};{1}", SystemConstant.CNST_ALLOCATION_TESTSUITENAME, SystemConstant.CNST_DEALERALLOCATION_TESTCASE);
                    eCde = CheckSubActionCallFormatter(strValue);
                    if (eCde != ERROR_CODE._NO_ERROR)
                        Logger.Error("CheckSubActionsFormatter", string.Format("Check Testsuite name or test sheet name exists :{0}", strValue));
                    return eCde;
                case SystemConstant.CNST_SUBACTION_KEYWORD_BUSINESSALLOCATION:
                    strValue = string.Format("{0};{1}", SystemConstant.CNST_ALLOCATION_TESTSUITENAME, SystemConstant.CNST_BUSINESSALLOCATION_TESTCASE);
                    eCde = CheckSubActionCallFormatter(strValue);
                    if (eCde != ERROR_CODE._NO_ERROR)
                        Logger.Error("CheckSubActionsFormatter", string.Format("Check Testsuite name or test sheet name exists :{0}", strValue));
                    return eCde;
                case SystemConstant.CNST_SUBACTION_KEYWORD_IF:
                    /**IF
                        Usage:
                           Object: HappyName for a Table
                           RC: not used
                           Value: multiple express split by return.
                           Description: Check feature of the table, and execute suitable branch. 
                              For Example:
                                IF  TRADE_FINDE_TABLE  [BLANK] RowCount>1?{return=false;clickbutton[TRADE_FINDER_OK]}
                                                               RowCount=0?{return=true;clickbutton[TRADE_FINDER_CANL]}
                          i.e. OK button will click when TRADE_FINDE_TABLE.RowCount>1.  **/
                    eCde = CheckSubActionIfFormatter(objStep.Value);
                    return eCde;
                default:
                    eCde = ERROR_CODE._COMPILER_SUBACTION_KEYWORD_IS_NOT_A_SUBACTION_PARA_1;
                    Logger.Error("CheckSubActionsFormatter", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), objStep.Keyword));
                    return eCde;
            }
            
        }

        private ERROR_CODE CheckSubActionIfFormatter(string strValueOfIf)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            string[] arrIfValues = strValueOfIf.Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
            if(arrIfValues.Length==0)
            {
                eCde = ERROR_CODE._KEYWORDS_IF_FORMATTER_NO_VALUE_PARA_0;
                Logger.Error("CheckSubActionIfFormatter", ERROR_INFO.GET_ERROR_STR(eCde));
                return eCde;
            }

            for (int i = 0; i < arrIfValues.Length;i++ )
            {
                if (!TigerMarsUtil.RegularTest(SystemConstant.CNST_ENHANCE_IF_FORMATTER_REGULOR_EX, arrIfValues[i]))
                {
                    eCde = ERROR_CODE._KEYWORDS_IF_FORMATTER_SETTING_ERROR_PARA_1;
                    Logger.Error("CheckSubActionIfFormatter", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), arrIfValues[i]));
                    return eCde;
                }
            }

            return eCde;
        }

        private ERROR_CODE CheckSubActionCallFormatter(string strFileNameWithSheetName)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            string[] arrValues = strFileNameWithSheetName.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            if (arrValues.Length != 2)
            {
                eCde = ERROR_CODE._KEYWORDS_CALL_FORMATTER_PARA_1;
                Logger.Error("CheckSubActionCallFormatter", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strFileNameWithSheetName));
                return eCde;
            }

            TCObjects objTempObjects = new TCObjects();
            objTempObjects.XlsFileNameWithPath = arrValues[0];
            objTempObjects.CurrentRunName = arrValues[1];
            eCde = objTempObjects.loadTestCase();
            return eCde;
        }

        private bool IsSubActionKeyword(string strKeyword)
        {
            foreach (string strItem in SystemConstant.CNST_ARR_KEYWORD_SUBACTIONS)
            {
                if (string.Compare(strItem, strKeyword, true) == 0) 
                    return true;
            }
            return false;
        }

#if _tigerQTPHost
        internal ERROR_CODE InitTestCase()
        {
            Logger.logBegin("InitTestCase");
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            if (mobjQTP == null)
            {
                eCde = ERROR_CODE._QTP_ERROR_INSTANCE_NULL;
                Logger.Error("InitTestCase", ERROR_INFO.GET_ERROR_STR(eCde));
                return eCde;
            }
            /** get libraries information and Addins information **/
            List<string> lstLibraries = AppConfigReader.GetConfigUFTInitScripts();
            List<string> lstAddins = AppConfigReader.GetConfigCurrentUFTAddins();

            /** add fullpath**/
            //Assembly
            string strCurrentPath = this.GetType().Assembly.Location;
            string strFilePath = TigerMarsUtil.GetPathWithoutFileName(strCurrentPath);

            //List<string> lstLibrariesEx = new List<string>();
            for (int i = 0; i < lstLibraries.Count; i++)
            {
                string strPathRelated = lstLibraries[i];
                strPathRelated = Path.Combine(strFilePath, "..", strPathRelated);
                lstLibraries[i] = strPathRelated;
            }

            eCde = this.mobjQTP.CreateAQtpTest(lstLibraries, lstAddins);

            /*** Create temporary Test case ***/
            //C:\work\marquis\Bins\testCode
            //this.mobjQTP.CreateAQtpTest(null);
            // start services for client

            return eCde;
        }

        internal ERROR_CODE InsertRunnableTestScript(StringBuilder strRunnableScript)
        {
            Logger.logBegin("InsertRunnableTestScript");

            ERROR_CODE eCde = this.mobjQTP.InsertRunnableTestScript(strRunnableScript);
            if (eCde != ERROR_CODE._NO_ERROR) return eCde;
            Logger.logEnd("InsertRunnableTestScript");
            return ERROR_CODE._NO_ERROR;
        }

        internal ERROR_CODE RunTest()
        {
            Logger.logBegin("RunTest");

            mobjQTP.RunTest();

            Logger.logEnd("RunTest");
            return ERROR_CODE._NO_ERROR;
        }

        internal Thread GetThread()
        {
            return this.mobjQTP.mobjThreadHost;
        }
#endif
    }
}
