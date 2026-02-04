using com.Mars.Constants;
using com.Mars.TestFrame.Application;
using com.Mars.TestFrame.TestObjects;
#if _Datafrom_Database
using Mars.DataLayer;
using Mars.Dto;
#endif
using MarsTestFrame.CommuniteServer;
using MarsTestFrame.SourceCode.com.Mars.KeyWords.KeyWordObject;
using MarsTestFrame.SourceCode.com.Mars.TCDataSource;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace MarsTestFrame.com.Mars.TestConfigObjects
{

    public sealed class QuickAccessMgr
    {
        public static string ConvertQuickAccessIdToQtpObjectFormat(string strQuickAccessOrignal, string strObjectType)
        {
            if (string.IsNullOrEmpty(strQuickAccessOrignal)) return "";
            
            string[] arrURLWithout13And10 = strQuickAccessOrignal.Split(new string[] { "\n\r", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            string strresult = "";
            for (int i=0;i<arrURLWithout13And10.Length;i++)
            {
                if (i == 0)
                    strresult = "\""+ arrURLWithout13And10[i]+"\"";
                else
                {
                    strresult = strresult + ",\"" + arrURLWithout13And10[i] + "\"";
                }
            }
            if (string.IsNullOrEmpty(strObjectType)) return strresult;
            return string.Format("{0}({1})", strObjectType,strresult);
        }
    }

    [Serializable]
    public class ConfigObjectBase
    {
        public const string CNST_MULTIPLE_LEVEL_ATTACH = "ATTACH-";
        public const string CNST_MULTIPLE_LEVEL_END = "-MARS-END";

        public static string DealWithObjectURLWithAttach(string strURL,string strWindowTyp,ref string strAttach)
        {
            if (strURL == null) return "";
            if (strWindowTyp == "") return strURL;
            string strPre = strURL.Replace(CNST_MULTIPLE_LEVEL_ATTACH, "");
            string[] arrURL = strPre.Split(new string[] { CNST_MULTIPLE_LEVEL_END },StringSplitOptions.RemoveEmptyEntries);
            if (arrURL == null) return "";
            if (arrURL.Length <= 1)
            {
                strAttach = "" ;
                string[] arrIds = arrURL[0].Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                string strMultId = string.Format("{2}{1}(\"{0}\")", string.Join("\",\"", arrIds), strWindowTyp, strAttach == null ? "" : strAttach.Length == 0 ? "" : (strAttach));
                return strMultId;
            }
            else
            {
                strAttach = arrURL[0];
                string[] arrIds = arrURL[1].Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                string strMultId = string.Format("{2}{1}(\"{0}\")", string.Join("\",\"", arrIds), strWindowTyp, strAttach == null ? "" : strAttach.Length == 0 ? "" : (strAttach));
                return strMultId;
            }
        }

        public override string ToString()
        {
            return "";
        }

        protected virtual bool IsMultipleLevelString()
        {
            return false;
        }

        public static string GetCurrentApplicationPrefixForQuickAccess(string strDefault)
        {
            string strCurrentTestApplicationType = AppConfigReader.GetCurrentRuntimeApplicationType();
            if ((!string.IsNullOrEmpty(strCurrentTestApplicationType)) && (string.Compare(strCurrentTestApplicationType, "java", true) == 0))
            {
                return "JavaWindow";
            }
            return strDefault;
        }

    }
    public class BatchConfigObject : ConfigObjectBase
    {
        #region properties
        protected string mstrAction;
        protected string mstrTCFilePath;
        protected string mstrTCSheetName;
        protected string mstrID;
        protected string mstrPrefixId;
        protected string mstrRunResult;
        #endregion properties

#if _Datafrom_Database
        protected bool isDataFromDB;
        public BatchConfigObject() : base()
        {
            isDataFromDB = false;//data from xls files
        }
#endif

        public string Action /*** run/Skip ***/
        {
            get { return this.mstrAction; }
            set { this.mstrAction = value; }
        }
        public string TCFilePath
        {
            get { return this.mstrTCFilePath; }
            set { this.mstrTCFilePath = value; }
        }
        public string TCSheetName
        {
            get { return this.mstrTCSheetName; }
            set { this.mstrTCSheetName = value; }
        }
        public string TestSuiteID
        {
            get { return this.mstrID; }
            set { this.mstrID = value; }
        }

        public string PreParentId { get { return this.mstrPrefixId; }
            set { this.mstrPrefixId = value; }
        }

        public string RunResult
        {
            get { return this.mstrRunResult; }
            set { this.mstrRunResult = value; } 
        }

        public override string ToString()
        {
            return string.Format("Action=[{0}], TCFilePath=[{1}], TCSheet=[{2}]", this.Action, this.TCFilePath, this.TCSheetName);
        }
    }

#if _Datafrom_Database
    public class BatchConfigObjectFromDB: BatchConfigObject
    {
        /// <summary>
        /// properties contains key means the item is from database
        /// </summary>
        protected long? testSuiteKeyId;
        protected long? testCaseKeyId;
#if v_useNameId
        private long currentAppId;
        public long CurrentTestAppId
        {
            get { return currentAppId; }
            set { currentAppId = value; }
        }
#endif
        protected V_STORYBOARD_TEST_FULLVISIONDTO m_storyBoardObjectRef;
#if v_16AndUp
        protected string dataSetName;
        public string DataSetName
        {
            get { return dataSetName; }
            set
            {
                dataSetName = value;
            }
        }
#endif

        public BatchConfigObjectFromDB():base()
        {
            this.isDataFromDB = true;
        }

        public long? TestSuiteKeyID {
            get { return this.testSuiteKeyId; }
            set { this.testSuiteKeyId = value; }
        }
        public long? TestCaseKeyId
        {
            get { return this.testCaseKeyId; }
            set { this.testCaseKeyId = value; }
        }

        public V_STORYBOARD_TEST_FULLVISIONDTO AssignedStoryObject
        {
            get { return this.m_storyBoardObjectRef; }
            set { this.m_storyBoardObjectRef = value; }
        }
    }
#endif
    #region subActions' class


    [Serializable]
    public class SubTestStepInfo : ConfigObjectBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(SubTestStepInfo));
        protected List<ConfigObjectBase> SubActions = new List<ConfigObjectBase>();

        public string Keyword{get;set;}
        
        internal virtual List<ConfigObjectBase> GetSubActions()
        {
            return SubActions;
        }

        internal virtual ERROR_CODE BuildSubActionList(int iLoop)
        {
            return ERROR_CODE._NO_ERROR;
        }

        public static SubTestStepInfo CreateSubTestStepInfoFactory(string strKeyword, string strValue, string strRCInfo, ref ERROR_CODE eCde)
        {
            switch (strKeyword.ToUpper())
            {
                case SystemConstant.CNST_SUBACTION_KEYWORD_IF:
                    IFTestStepInfo objIF = new IFTestStepInfo();
                    eCde = IFTestStepInfo.ParseSubObject(ref objIF, strKeyword, strValue);
                    if (eCde != ERROR_CODE._NO_ERROR) return null;
                    else return objIF;
                case SystemConstant.CNST_SUBACTION_KEYWORD_CALL:
                    return null;
                case SystemConstant.CNST_SUBACTION_KEYWORD_BUSINESSALLOCATION:
                case SystemConstant.CNST_SUBACTION_KEYWORD_DEALERALLOCATION:
                    return null;
                default:
                    eCde = ERROR_CODE._COMPILER_SUBACTION_KEYWORD_IS_NOT_A_SUBACTION_PARA_1;
                    Logger.Error("CreateSubTestStepInfoFactory", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strKeyword));
                    return null;   
            }
        }

        public virtual ERROR_CODE CloneObject2SeviceObj(SubTestInfo4Services objTarget)
        {
            ERROR_CODE eCde =ERROR_CODE._NO_ERROR;                  
            return eCde;
        }
    }    

    [DataContract]
    public class IFSubItemInfo:ConfigObjectBase
    {
        [DataMember]
        public string PropertyName;
        [DataMember]
        public string OperationMark;
        [DataMember]
        public string Value2Compare;
        [DataMember]
        public string AssociatedAction;
        [DataMember]
        public string ReturnValue;
        [DataMember]
        public string SubKeyword;
        [DataMember]
        public string SubObjectAndOthers;

        public IFSubItemInfo4Services Clone2ServiceObject()
        {
            IFSubItemInfo4Services objIFSubItem = new IFSubItemInfo4Services();
            objIFSubItem.AssociatedAction = this.AssociatedAction;
            objIFSubItem.OperationMark = this.OperationMark;
            objIFSubItem.PropertyName = this.PropertyName;
            objIFSubItem.ReturnValue = this.ReturnValue;
            objIFSubItem.SubKeyword = this.SubKeyword;
            objIFSubItem.SubObjectAndOthers = this.SubObjectAndOthers;
            objIFSubItem.Value2Compare = this.Value2Compare;
            return objIFSubItem;
        }
    }

    [Serializable]
    public class IFTestStepInfo : SubTestStepInfo
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(IFTestStepInfo));

        internal List<IFSubItemInfo> mlstSubIfItmInfo;

        internal override List<ConfigObjectBase> GetSubActions()
        {
            return base.GetSubActions();
        }

        internal override ERROR_CODE BuildSubActionList(int iLoop)
        {
            //protected List<ConfigObjectBase> SubActions
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR ;
            SubActions.Clear();
            int iRunID = 0;
            foreach(IFSubItemInfo objIFItm in this.mlstSubIfItmInfo)
            {                
                TestStep objTestStp = new TestStep();
                objTestStp.Keyword = objIFItm.SubKeyword;
                objTestStp.ObjectName = objIFItm.SubObjectAndOthers;
                objTestStp.Loop = iLoop;
                objTestStp.Row_Column = "";
                objTestStp.Value = "";
                objTestStp.RunID = iRunID++;
                SubActions.Add(objTestStp);
            }
            return eCde;
        }

        internal static ERROR_CODE ParseSubObject(ref IFTestStepInfo objIFSubAction, string strKeyword, string strValue)
        {
            objIFSubAction.Keyword = strKeyword;
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;

            string[] arrValueItems = strValue.Split(new string[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
            List<IFSubItemInfo> lstItemInfo = new List<IFSubItemInfo>();
            foreach (string strItm in arrValueItems)
            {
                string strItm2Parse = strItm.Replace("\r", "");
                Regex objReg = new Regex(SystemConstant.CNST_IF_REGQULOR_PARSE);
                string[] arrInfo = objReg.Split(strItm2Parse);
                if (arrInfo.Length != 7)
                {
                    eCde = ERROR_CODE._KEYWORDS_IF_FORMATTER_SETTING_ERROR_PARA_1;
                    Logger.Error("ParseSubObject", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strValue));
                    return eCde;
                }
                IFSubItemInfo objIfSubItem = new IFSubItemInfo();
                //@"(>|<|=|>=|<=){1}(\d+)\?return\=(true|false):(\S+)\[(\S+)\]"
                objIfSubItem.PropertyName = arrInfo[0];
                objIfSubItem.OperationMark = arrInfo[1];
                objIfSubItem.Value2Compare = arrInfo[2];
                objIfSubItem.ReturnValue = arrInfo[3];
                objIfSubItem.SubKeyword = arrInfo[4];
                objIfSubItem.SubObjectAndOthers = arrInfo[5];

                lstItemInfo.Add(objIfSubItem);
            }

            objIFSubAction.mlstSubIfItmInfo = lstItemInfo;
            return ERROR_CODE._NO_ERROR;
        }

        public override ERROR_CODE CloneObject2SeviceObj(SubTestInfo4Services objTarget)
        {
            /** **/
            ERROR_CODE eCde =ERROR_CODE._NO_ERROR;
            if (objTarget == null) return eCde;

            if (!(objTarget is IFSubTestInfo4Services))
            {
                eCde = ERROR_CODE._SERVICE_ERROR_IF_SUBOBJECT_REQUIRED_PARA_1;
                Logger.Error("CloneObject2ServiceObj", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), objTarget.GetType().ToString()));
                return eCde;
            }
            if (this.SubActions == null) return eCde;

            objTarget.keyword = SystemConstant.CNST_SUBACTION_KEYWORD_IF;

            IFSubTestInfo4Services objIFtarget = (IFSubTestInfo4Services)objTarget;
            objIFtarget.SubIFItems = new List<IFSubItemInfo4Services>() ;                        
            for (int i = 0; i < this.mlstSubIfItmInfo.Count; i++)
            {
                IFSubItemInfo objIFSubItm = ((IFSubItemInfo)(this.mlstSubIfItmInfo[i]));                
                IFSubItemInfo4Services obj4Service = objIFSubItm.Clone2ServiceObject() ;
                objIFtarget.SubIFItems.Add(obj4Service) ;
            }

            /** clone sub Actions **/
            objTarget.SubActions = new List<TestStep4Services>();            
            for (int i = 0; i < this.SubActions.Count;i++ )
            {
                if (!(SubActions[i] is TestStep))
                {
                    continue;
                }
                TestStep objStep = (TestStep)SubActions[i];
                TestStep4Services objStep4Service = new TestStep4Services() ;
                objStep.CloneToService(objStep4Service);
                objTarget.SubActions.Add(objStep4Service);
            }
            return eCde;
        }
    }

    #endregion //sub Actions' class

    [Serializable]
    public class TestStep : ConfigObjectBase
    {
        private MLogger Logger = MLogger.GetLogger(typeof(TestStep));
        #region member Var
        private string mstrKeyWord;
        private string mstrObject;
        private string mstrRCParameter;
        private string mstrValue;
        private string mstrQuickAccess;
        private string mstrComment;
        private int miLoop = -1;
        private int miRunID = -1;
        private long mlAssignedStepId = -1;
        /// <summary>
        /// 有些对象无法通过两层对象模型完成，因此，需要在pegwindow中添加parentAttach信息。以便在后期拼装
        /// </summary>
        private string mstrParentAttachInfo = null;

        private List<string> mlstData = null;
        protected List<TargetApplicationInfo> ApplicationListRunOn = null;
        #endregion

        #region property
        public int RunID{get{return this.miRunID ;}set{this.miRunID=value ;}} 
        public string Keyword { get { return this.mstrKeyWord; } set { this.mstrKeyWord = value; } }
        public string ObjectName { get { return this.mstrObject; } set { this.mstrObject = value; } }
        public string Row_Column { get { return this.mstrRCParameter; } set { this.mstrRCParameter = value; } }
        public string Value { get { return this.mstrValue; } set { this.mstrValue = value; } }
        public string QuickAccess {
            get {
                return this.mstrQuickAccess;
            }
            set {
                this.mstrQuickAccess = value;
                IsMultipleLevelString();
            }
        }

        protected override bool IsMultipleLevelString()
        {
            if (string.IsNullOrWhiteSpace(mstrQuickAccess)) return false;
            if (!mstrQuickAccess.StartsWith(CNST_MULTIPLE_LEVEL_ATTACH))
            {
                return false;
            }
            //获得中间的数据
            int iPos = mstrQuickAccess.IndexOf(CNST_MULTIPLE_LEVEL_END);
            if (iPos <= 0)
            {
                return false;
            }
            mstrParentAttachInfo = mstrQuickAccess.Substring(CNST_MULTIPLE_LEVEL_ATTACH.Length, iPos - CNST_MULTIPLE_LEVEL_ATTACH.Length);
            mstrQuickAccess = mstrQuickAccess.Substring(iPos + CNST_MULTIPLE_LEVEL_END.Length);
            return true;
        }

        public string ParentAttachInfo 
        {
            get{ return mstrParentAttachInfo; }
            set {
                mstrParentAttachInfo = value;
            }
        }
        public string Comment { get { return this.mstrComment; } set { this.mstrComment = value; } }
        public int Loop { get { return this.miLoop; } set { this.miLoop = value; } }
        public long AssignedStepdId { get { return mlAssignedStepId; } set { mlAssignedStepId = value; } }
        public List<TestPegwindowObject> Pegwindows = null;
        public KeyWordObjectInfo KeyWordFuntion = null;
        public string ObjectFullpath = null;

        public SubTestStepInfo SubActionObject = null ;
        #endregion

        #region subActions segment
        public bool isSubActionStep()
        {
            
            return SubActionObject == null;
        }

        public ERROR_CODE ParseSubAction(int iCurrentLoop)
        {
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            SubTestStepInfo objSubAction = SubTestStepInfo.CreateSubTestStepInfoFactory(this.Keyword, this.Value, this.Row_Column, ref eCde);
            if (eCde != ERROR_CODE._NO_ERROR) return eCde;
            eCde = objSubAction.BuildSubActionList(iCurrentLoop);
            if (eCde == ERROR_CODE._NO_ERROR)
                SubActionObject = objSubAction;
            else
                SubActionObject = null;
            return eCde;
        }
        #endregion //subActions Segment

        /** copy value to another Test Step object **/
        internal virtual void CloneTo(TestStep objTarget)
        {
            if (objTarget == null) return;
            objTarget.Keyword = this.Keyword;
            objTarget.ObjectName = this.ObjectName;
            objTarget.Row_Column = this.Row_Column;
            objTarget.Value = this.Value;
            objTarget.QuickAccess = this.QuickAccess;
            objTarget.Comment = this.Comment;
            objTarget.Loop = this.Loop;
            objTarget.RunID = this.RunID;
            if (this.Pegwindows!=null)
            {
                objTarget.Pegwindows = new List<TestPegwindowObject>();
                for(int i=0 ;i<this.Pegwindows.Count ;i++)
                {
                    objTarget.Pegwindows.Add(Pegwindows[i]);
                }
            }
            objTarget.KeyWordFuntion = this.KeyWordFuntion;
            objTarget.ObjectFullpath = this.ObjectFullpath;
        }

        internal virtual void CloneToService(TestStep4Services objServiceObj)
        {
            if (objServiceObj == null) return;
            objServiceObj.Comment = this.Comment;
            objServiceObj.Keyword = this.Keyword;
            objServiceObj.Loop = this.Loop;
            objServiceObj.ObjectName = this.ObjectName;

            if (string.Compare("pegwindow", this.Keyword, true)==0)
            {
                Logger.Info("CloneToService",string.Format("Pegwindow with object quick access:[{0}],fullPath:[{1}]",this.QuickAccess, this.ObjectFullpath));
                
            }            
            //objServiceObj.ParentAttachInfo = this.ParentAttachInfo;
            objServiceObj.QuickAccess = string.IsNullOrEmpty(this.QuickAccess)?this.ObjectFullpath:this.QuickAccess;
            objServiceObj.Row_Column = this.Row_Column;
            objServiceObj.Value = this.Value;
            objServiceObj.RunID = this.RunID;
            //Logger.Info("--ServerQuick--", string.Format("Client quick:[{0}], server:FullPath:[{1}], Server quick:[{2}]", objServiceObj.QuickAccess, this.ObjectFullpath, this.QuickAccess));
            if (this.SubActionObject is IFTestStepInfo)
            {
                IFSubTestInfo4Services objIFSubs = new IFSubTestInfo4Services();
                this.SubActionObject.CloneObject2SeviceObj(objIFSubs);
                objServiceObj.SubTestInfo = objIFSubs;
            }
            //else
            //{
            //    //objServiceObj.
            //}
        }

        public ERROR_CODE BuildObjectFullPath(string strDBIdx,ref string strErrorInfo)
        {
            Logger.logBegin("BuildObjectFullPath");
            try
            {
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
                if ((mstrQuickAccess != null) && (mstrQuickAccess.Trim().Length != 0))
                {
                    ObjectFullpath = mstrQuickAccess;
                }
                else
                {
                    string[] arrPegsName = GetPegwindowNames();
                    if ((arrPegsName == null) || (arrPegsName.Length == 0))
                    {
                        eCde = ERROR_CODE._COMPILER_NO_PEGWINDOW_FOR_TESTSTEP_FIND;
                        Logger.Error("BuildObjectFullPath", strErrorInfo=ERROR_INFO.GET_ERROR_STR(eCde));
                        return eCde;
                    }
                    /** get Object Identify information **/
                    ConfigObjectBase objTestObjInfo = TestObjectsManagement.GetObjectInfomationByPegwindow(
                        strDBIdx,
                        ApplicationListRunOn[0].ApplicationShortName, Pegwindows[0].ObjectName, this.ObjectName);
                    if (objTestObjInfo is TestObject)
                    {
                        /** check whether Runtime windows information is applied **/
                        TestPegwindowObject objRuntimePeg = GetRuntimePegInfo();
                        if (((TestObject)objTestObjInfo).IsPegwindowObject())
                        {
                            if (objRuntimePeg == null)
                                ObjectFullpath = string.Format("{0}", Pegwindows[0].FullPathAccess);
                            else
                                ObjectFullpath = string.Format("{0}", objRuntimePeg.FullPathAccess);
                        }
                        else
                        {
                            if (objRuntimePeg == null)
                                ObjectFullpath = string.Format("{0}.{1}", Pegwindows[0].FullPathAccess, ((TestObject)objTestObjInfo).FullPathAccess);
                            else
                                ObjectFullpath = string.Format("{0}.{1}", objRuntimePeg.FullPathAccess, ((TestObject)objTestObjInfo).FullPathAccess);
                        }
                        mstrQuickAccess = string.IsNullOrEmpty(mstrQuickAccess) ? ObjectFullpath : mstrQuickAccess;
                        Logger.Info("---BuildObjectFullPath.QuickAccess---", string.Format("FullPath:[{0}], Quick:[{1}]", ObjectFullpath, mstrQuickAccess));
                    }
                    else
                    {
                        eCde = ERROR_CODE._COMPILER_NO_OBJECT_INDENTIFY_INFO;
                        string strErr = string.Format(ERROR_INFO.GET_ERROR_STR(eCde), this.mstrObject, arrPegsName[0]);
                        Logger.Error("BuildObjectFullPath", strErrorInfo=strErr);
                        return eCde;
                    }
                }
                return ERROR_CODE._NO_ERROR;
            }
            finally
            {
                Logger.logEnd("BuildObjectFullPath");
            }

        }

        private TestPegwindowObject GetRuntimePegInfo()
        {
            Logger.logBegin("IsUseRuntime");
            try
            {
                if (Pegwindows == null) return null;
                foreach(TestPegwindowObject objPeg in Pegwindows)
                {
                    if (objPeg == null) continue;
                    TestPegwindowObject objPegRun = objPeg.GetRunTimePegWindow();
                    if (objPegRun == null) continue;
                    return objPegRun;
                }
                return null;
            }
            finally
            {
                Logger.logEnd("IsUseRunTime");
            }
        }

        public ERROR_CODE ValidateStepSetting()
        {
            Logger.logBegin("ValidateStepSetting");
            try
            {
                if (KeyWordFuntion == null)
                {
                    Logger.Info("ValidateStepSetting", MarsTestFrame.Properties.Resources.HINT_KEYWORD_VALIDATE_NO_VALIDATECLASS_RETURN_TRUE);
                    return ERROR_CODE._NO_ERROR;
                }
                if (KeyWordFuntion.ParseInstance == null)
                {
                    Logger.Info("ValidateStepSetting", MarsTestFrame.Properties.Resources.HINT_KEYWORD_VALIDATE_NO_VALIDATECLASS_RETURN_TRUE);
                    return ERROR_CODE._NO_ERROR;
                }
                E_KeywordParameterID eKeyWordPara = KeyWordFuntion.ParseInstance.IsParameterRequired();
                bool isSettingCorrect = true;
                string[] arrAppshortNames = new string[] { };
                ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
                if ((eKeyWordPara & E_KeywordParameterID.e_Keyword_Parameter_object) == E_KeywordParameterID.e_Keyword_Parameter_object)
                {
                    /** GET pegwindow name **/
                    string[] arrPegsName = GetPegwindowNames();
                    if ((arrPegsName == null) || (arrPegsName.Length == 0))
                    {
                        eCde = ERROR_CODE._COMPILER_NO_PEGWINDOW_FOR_TESTSTEP_FIND;
                        Logger.Error("ValidateStepSetting", ERROR_INFO.GET_ERROR_STR(eCde));
                        return eCde;
                    }
                    /** check object is required by keyword **/
                    isSettingCorrect = isSettingCorrect && ((eCde = CheckApplicationsValidate(ref arrAppshortNames)) == ERROR_CODE._NO_ERROR);
                    if (isSettingCorrect)
                    {
                        isSettingCorrect = KeyWordFuntion.ParseInstance.IsRightFormatForObject(arrAppshortNames[0], arrPegsName[0], this.ObjectName, ref eCde);
                        if (!isSettingCorrect)
                        {
                            return eCde;
                        }
                        isSettingCorrect = true;
                    }
                    else
                    {
                        return eCde;
                    }
                }
                if (((eKeyWordPara & E_KeywordParameterID.e_Keyword_Parameter_RC) == E_KeywordParameterID.e_Keyword_Parameter_RC) && (isSettingCorrect))
                {
                    /** RC should not be empty **/
                    if (this.Row_Column == null)
                    {
                        eCde = ERROR_CODE._COMPILER_NO_RC;
                        string strError = string.Format(ERROR_INFO.GET_ERROR_STR(eCde), string.Format("keyword:[{0}], object:[{1}]", this.Keyword, this.ObjectName));
                        Logger.Error("ValidateStepSetting", strError);
                        return eCde;
                    }
                }

                return ERROR_CODE._NO_ERROR;
            }
            finally
            {
                Logger.logEnd("ValidateStepSetting");
            }
        }

        protected string[] GetPegwindowNames()
        {
            Logger.logBegin("GetPegwindowNames");
            try
            {
                List<string> lstPegsName = new List<string>();
                if (this.Pegwindows==null)
                {
                    Logger.Info("GetPegwindowNames","No pegWindows....");
                    return lstPegsName.ToArray();
                }
                foreach (TestPegwindowObject objPeg in this.Pegwindows)
                {
                    lstPegsName.Add(objPeg.ObjectName);
                }
                return lstPegsName.ToArray();
            }
            finally
            {
                Logger.logEnd("GetPegwindowNames");
            }
        }

        private ERROR_CODE CheckApplicationsValidate(ref string[] arrApplicationShorts)
        {
            Logger.logBegin("CheckApplicationsValidate");
            try
            {
                string[] arrApplications = this.GetApplicationShortName();
                if (arrApplications == null)
                {
                    return ERROR_CODE._NO_ERROR;
                }
                if (arrApplications.Length != 1)
                {
                    Logger.Error("CheckApplicationsValidate", ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_APPLICATON_LENTH_NOT_1));
                    return ERROR_CODE._COMPILER_APPLICATON_LENTH_NOT_1;
                }

                return ERROR_CODE._NO_ERROR;
            }
            finally
            {
                Logger.logEnd("CheckApplicationsValidate");
            }

        }



        public void AddApplicationInfo(string strShortName, TargetApplicationInfo appObj)
        {
            Logger.logBegin("AddApplicationInfo");
            if (ApplicationListRunOn == null) ApplicationListRunOn = new List<TargetApplicationInfo>();
            if (strShortName == null) return;
            foreach (TargetApplicationInfo objApp in ApplicationListRunOn)
            {
                if (objApp == null) continue;
                if (string.Compare(strShortName, objApp.ApplicationShortName, true) == 0) return;
            }
            ApplicationListRunOn.Add(appObj);
            Logger.logEnd("AddApplicationInfo");
        }

        public string[] GetApplicationShortName()
        {
            if (ApplicationListRunOn == null) return null;
            List<string> lstResult = new List<string>();
            foreach (TargetApplicationInfo objApplication in ApplicationListRunOn)
            {
                lstResult.Add(objApplication.ApplicationShortName);
            }
            return lstResult.ToArray();
        }

        public void AddPegWindowInfo(TestPegwindowObject objPeg)
        {
            Logger.logBegin("AddPegWindowInfo");
            if (Pegwindows == null) Pegwindows = new List<TestPegwindowObject>();
            foreach (TestPegwindowObject objPegIn in Pegwindows)
            {
                if (objPegIn == null) continue;
                if (string.Compare(objPeg.ObjectName, objPegIn.ObjectName, true) == 0) return;
            }
            Pegwindows.Add(objPeg);

            Logger.logEnd("AddPegWindowInfo");
        }

        public void AddPegWindowInfo(List<TestPegwindowObject> lstPeg)
        {
            Pegwindows = lstPeg;
        }

        public TestPegwindowObject[] GetPegWindows()
        {
            return this.Pegwindows==null?null:this.Pegwindows.ToArray<TestPegwindowObject>();
        }


        public void InitializeDataList()
        {
            Logger.logBegin("InitializeDataList");
            if (mlstData == null)
                mlstData = new List<string>();
            mlstData.Clear();
            Logger.logEnd("InitializeDataList");
        }

        public void AddOneItemToDataList(string strItem)
        {
            Logger.logBegin("AddOneItemToDataList");
            if (mlstData == null)
                InitializeDataList();
            mlstData.Add(strItem);
            Logger.logEnd("AddOneItemToDataList");
        }

        public override string ToString()
        {
            return string.Format("\tKeyWord:[{0}],\r\n\tObject:[{1}],\r\n\tRC:[{2}],\r\n\tValue:[{3}],\r\n\tComment:[{4}]", this.Keyword, this.ObjectName, this.Row_Column, this.Value, this.Comment);
        }

        internal string GetKeyWordsDef()
        {
            Logger.logBegin("GetKeyWordsDef");
            if (this.KeyWordFuntion == null) return "";
            string strScript = KeyWordFuntion.GetKeywordDefScript();
            Logger.logEnd("GetKeyWordsDef");
            return strScript;
        }

        internal ERROR_CODE BuildPegWindowObjectFullPath(List<TestPegwindowObject> lstPegs)
        {
            Logger.logBegin("BuildPegWindowObjectFullPath");

            if (!string.IsNullOrEmpty(mstrQuickAccess)) return ERROR_CODE._NO_ERROR;
            if (lstPegs == null) return ERROR_CODE._NO_ERROR;
            if (lstPegs.Count == 0) return ERROR_CODE._NO_ERROR;
            if (string.IsNullOrEmpty(lstPegs[0].QuickAccessString))
            {
                Logger.Error("BuildPegWindowObjectFullPath", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_PEGWINDOW_IDENTIFIERINFO), this.ObjectName));
                return ERROR_CODE._COMPILER_NO_PEGWINDOW_IDENTIFIERINFO;
            }

            /** more than one pegwindows are available. but until 2015-Mar. no support for that **/
            string[] arrPegSubItems = lstPegs[0].QuickAccessString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string strMultId = string.Format("\"{0}\"", string.Join("\",\"", arrPegSubItems));

            this.mstrQuickAccess = string.Format("Window({0})", strMultId);
            lstPegs[0].FullPathAccess = this.mstrQuickAccess;

            Logger.logEnd("BuildPegWindowObjectFullPath");
            return ERROR_CODE._NO_ERROR;
        }

        internal List<ConfigObjectBase> GetSubActions()
        {
            if (SubActionObject == null) return null;
            return SubActionObject.GetSubActions();
        }
    }

    

    public class TestObject : ConfigObjectBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestObject));

        #region members
        protected string mstrFullPathAccess = "";
        #endregion

        #region properties
        public string ObjectName;
        public string ObjectType;
        public string QuickAccessString;
        public string ParentPegInfo;
        public string Description;

        public string FullPathAccess { get { return mstrFullPathAccess; } set { this.mstrFullPathAccess = value; } }
        #endregion

        public bool IsPegwindowObject()
        {
            return (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEGWINDOW, true) == 0) 
#if _ver1_5
                || (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_WPF,true)==0)
                || (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_STANDARD,true)==0)
                || (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_DIALOG, true) == 0)
                || (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_SWFWINDOW, true) == 0)
                //老虎8-3，2017添加
                || (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_JAVAWINDOW, true)==0)
#endif
                ;
        }

        public static bool IsPegwindowObject(string strObjType)
        {
            return string.Compare(strObjType, SystemConstant.CNST_RESERVED_KEYWORD_PEGWINDOW, true) == 0
#if _ver1_5
                || (string.Compare(strObjType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_WPF, true) == 0)
                || (string.Compare(strObjType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_STANDARD, true) == 0)
                || (string.Compare(strObjType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_DIALOG, true) == 0)
                || (string.Compare(strObjType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_SWFWINDOW, true) == 0)
                //老虎8-3，2017添加
                || (string.Compare(strObjType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_JAVAWINDOW, true) == 0)
#endif
                ;
        }

        

        public ERROR_CODE ValidateParameters()
        {
            throw new NotImplementedException();
        }
        
       


        public static bool IsMultipleLevelString(ref string strQuickAccessStr, ref string strParentPegInfo)
        {
            string strSrc = strQuickAccessStr;
            try
            {
                Logger.logBegin("IsMultipleLevelString", string.Format("QuickAcc:[{0}]", strQuickAccessStr));

                if (string.IsNullOrWhiteSpace(strQuickAccessStr)) return false;
                if (!strQuickAccessStr.StartsWith(CNST_MULTIPLE_LEVEL_ATTACH)) return false;
                //获得中间的数据
                int iPos = strQuickAccessStr.IndexOf(CNST_MULTIPLE_LEVEL_END);
                if (iPos <= 0)
                {
                    return false;
                }
                strParentPegInfo = strSrc.Substring(CNST_MULTIPLE_LEVEL_ATTACH.Length, iPos - CNST_MULTIPLE_LEVEL_ATTACH.Length);
                strQuickAccessStr = strSrc.Substring(iPos + CNST_MULTIPLE_LEVEL_END.Length);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("IsMultipleLevelString", string.Format("object:[{2}-{1}]\r\nExceptoin:[{0}]", e.Message, strQuickAccessStr, strQuickAccessStr));
                return false;
            }
            finally {
                Logger.logEnd("IsMultipleLevelString");
            }
        }

        /// <summary>
        /// 判断是否是多层次的
        /// </summary>
        /// <returns></returns>
        protected override bool IsMultipleLevelString()
        {
            QuickAccessString = QuickAccessString ?? "";
            bool isMultpleStr = IsMultipleLevelString(ref QuickAccessString, ref ParentPegInfo);
            return isMultpleStr;

            //Modified on 1-23-2017 by tiger
            //the code should be reusable

            //if (string.IsNullOrWhiteSpace(QuickAccessString)) return false;
            //if (!QuickAccessString.StartsWith(CNST_MULTIPLE_LEVEL_ATTACH))
            //{
            //    return false;
            //}
            ////获得中间的数据
            //int iPos = QuickAccessString.IndexOf(CNST_MULTIPLE_LEVEL_END);
            //if (iPos <= 0)
            //{
            //    return false;
            //}
            //ParentPegInfo = QuickAccessString.Substring(CNST_MULTIPLE_LEVEL_ATTACH.Length, iPos - CNST_MULTIPLE_LEVEL_ATTACH.Length);
            //QuickAccessString = QuickAccessString.Substring(iPos + CNST_MULTIPLE_LEVEL_END.Length);
            //return true;
        }

        public static string BuildPegQuickAcessStringByPegQuickAndType(string strQuick, string strWindowsType)
        {
            Logger.logBegin("BuildPegQuickAcessStringByPegQuickAndType", string.Format("Quick:[{0}], type:[{1}]", strQuick, strWindowsType));
            string strQuickAccess = strQuick.Replace("\r", "");
            string[] arrIds = strQuickAccess.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string strMultId = string.Format("\"{0}\"", string.Join("\",\"", arrIds));
            string strPegWinPrefix = "Window";
            string strQuickTmp = strQuick, strParentPegInfo="";
            bool isMultpleStr = IsMultipleLevelString(ref strQuickTmp, ref strParentPegInfo);

            if (string.Compare(strWindowsType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_WPF, true) == 0)
            {
                strPegWinPrefix = "WPFWindow";
            }
            if (string.Compare(strWindowsType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_DIALOG, true) == 0)
            {
                strPegWinPrefix = "Dialog";
            }
            if (string.Compare(strWindowsType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_SWFWINDOW, true) == 0)
            {
                strPegWinPrefix = "SwfWindow";
            }
            if (string.Compare(strWindowsType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_JAVAWINDOW, true) == 0)
            {
                strPegWinPrefix = SystemConstant.CNST_RESERVED_KEYWORD_PEG_JAVAWINDOW;
            }
            string strResult = "";
            if (!isMultpleStr)
                strResult = string.Format("{1}({0})", strMultId, strPegWinPrefix);
            else
            {
                strResult = string.Format("{2}{1}({0})", strMultId, strPegWinPrefix, strParentPegInfo ?? "");
            }
            return strResult;
        }

        public ERROR_CODE BuildPegQuickAcessString()
        {
            Logger.logBegin("BuildPegQuickAcessString",string.Format("Type:[{0}]", ObjectType));
            if (!string.IsNullOrEmpty(this.mstrFullPathAccess)) return ERROR_CODE._NO_ERROR;
            bool isMultipleLevel = false;
            if (IsPegwindowObject())
            {
                //判断是否是多level模式，如果是多level模式，将quickAccessString的东西分离成多level模式
                isMultipleLevel = IsMultipleLevelString();

                string strQuickAccess = QuickAccessString.Replace("\r", "");
                string[] arrIds = strQuickAccess.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                string strMultId = string.Format("\"{0}\"", string.Join("\",\"", arrIds));
#if _ver1_5
                string strPegWinPrefix = "Window";

                if (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_WPF, true) == 0)
                {
                    strPegWinPrefix = "WPFWindow";
                }
                if (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_DIALOG, true) == 0)
                {
                    strPegWinPrefix = "Dialog";
                }
                if (string.Compare(ObjectType, SystemConstant.CNST_RESERVED_KEYWORD_PEG_SWFWINDOW, true) == 0)
                {
                    strPegWinPrefix = "SwfWindow";
                }
                if(string.Compare(ObjectType,SystemConstant.CNST_RESERVED_KEYWORD_PEG_JAVAWINDOW,true)==0)
                {
                    strPegWinPrefix = SystemConstant.CNST_RESERVED_KEYWORD_PEG_JAVAWINDOW;
                }

                string strCurrentTestApplicationType = GetCurrentApplicationPrefixForQuickAccess(strPegWinPrefix);
                //string strCurrentTestApplicationType = AppConfigReader.GetCurrentRuntimeApplicationType();
                //if ((!string.IsNullOrEmpty(strCurrentTestApplicationType))&&(string.Compare(strCurrentTestApplicationType,"java",true)==0))
                //{
                //    strPegWinPrefix = "JavaWindow";
                //}
                if (!isMultipleLevel)
                    this.mstrFullPathAccess = string.Format("{1}({0})", strMultId, strPegWinPrefix);
                else
                {
                    this.mstrFullPathAccess = string.Format("{2}{1}({0})", strMultId, strPegWinPrefix, ParentPegInfo??"");
                }
                Logger.Info("BuildPegQuickAcessString", this.mstrFullPathAccess);
#else
                this.mstrFullPathAccess = string.Format("Window({0})", strMultId);
#endif
                return ERROR_CODE._NO_ERROR;
            }
            else
            {
                string strQuickAccess = QuickAccessString==null?"":QuickAccessString.Replace("\r", "");
                string[] arrIds = strQuickAccess.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                string strMultId = string.Format("\"{0}\"", string.Join("\",\"", arrIds));
                this.mstrFullPathAccess = string.Format("{1}({0})", strMultId, this.ObjectType);
                Logger.Info("BuildPegQuickAcessString", string.Format("Non peg access,[{0}]", this.mstrFullPathAccess));
                return ERROR_CODE._NO_ERROR;
            }
        }

        public virtual TestObject Clone()
        {
            Logger.logBegin("Clone");

            TestObject objNew = new TestObject();
            Logger.Info("Clone", ToString());
            objNew.ObjectType = this.ObjectType;
            objNew.ObjectName = this.ObjectName;
            objNew.QuickAccessString = this.QuickAccessString;
            objNew.FullPathAccess = this.FullPathAccess;
            objNew.Description = this.Description;
            Logger.logEnd("Clone");

            return objNew;
        }

        public override string ToString()
        {
            return string.Format(
                "{0}\r\n\t{1}\r\n\t{2}\r\n\t{3}\r\n\t{4}",
                TigerMarsUtil.GetParameter("Object Name", this.ObjectName),
                TigerMarsUtil.GetParameter("Object Type", this.ObjectType),
                TigerMarsUtil.GetParameter("Quick AccessString", this.QuickAccessString),
                TigerMarsUtil.GetParameter("Description", this.Description),
                TigerMarsUtil.GetParameter("FullPath Access", this.FullPathAccess)
            );
        }
    }

    public class TestRuntimePegwindow
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestRuntimePegwindow));
        public string RuntimePegWindowName { get; set; }
        public TestObject RuntimePegwindow { get; set; }
        public static bool isRuntimePegWindow(string strValueToCheck)
        {
            Logger.logBegin("isRuntimePegWindow");
            string strPart = string.Format("^{0}", SystemConstant.CNST_ENHANCE_PEG_RUNTIME_PREFIX);
            bool isMatch = TigerMarsUtil.RegularTest(strPart, strValueToCheck);
            Logger.logEnd("isRuntimePegWindow");
            return isMatch;
        }
        public static string GetRuntimePegInfo(string strValueToCheck)
        {
            Logger.logBegin("GetRuntimePegInfo");
            string strResult = strValueToCheck.Replace(SystemConstant.CNST_ENHANCE_PEG_RUNTIME_PREFIX, "");
            Logger.logEnd("GetRuntimePegInfo");
            return strResult;
        }
    }

    public class TestPegwindowObject : TestObject
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestPegwindowObject));
        public List<ConfigObjectBase> ChildrenObjects = null;

        private TestRuntimePegwindow RuntimePegInfo = null;

        public TestObject GetChildrenObjctsByName(string strObjName)
        {
            Logger.logBegin("GetChildrenObjctsByName");
            foreach (ConfigObjectBase objChild in this.ChildrenObjects)
            {
                if (!(objChild is TestObject)) continue;
                if (string.Compare((objChild as TestObject).ObjectName, strObjName, true) == 0)
                {
                    return objChild as TestObject;
                }
            }
            return null;
        }

       
        public TestRuntimePegwindow GetRunTimePegInfo()
        {
            return RuntimePegInfo;
        }
        public TestPegwindowObject GetRunTimePegWindow()
        {
            return RuntimePegInfo == null ? null : (TestPegwindowObject)(RuntimePegInfo.RuntimePegwindow);
        }

        public void SetRuntimePegInfo(string strRunTimePegName, TestPegwindowObject objPeg)
        {
            if (objPeg == null) return;
            RuntimePegInfo = new TestRuntimePegwindow();
            RuntimePegInfo.RuntimePegWindowName = strRunTimePegName;
            RuntimePegInfo.RuntimePegwindow = objPeg;
        }

        public override TestObject Clone()
        {
            Logger.logBegin("Clone");
            TestPegwindowObject objNew = new TestPegwindowObject();
            objNew.ObjectName = this.ObjectName;
            objNew.ObjectType = this.ObjectType;
            objNew.QuickAccessString = this.QuickAccessString;
            objNew.FullPathAccess = this.FullPathAccess;
            objNew.Description = this.Description;
            if (this.ChildrenObjects != null)
            {
                objNew.ChildrenObjects = new List<ConfigObjectBase>();
                foreach (ConfigObjectBase objItem in this.ChildrenObjects)
                {
                    objNew.ChildrenObjects.Add(objItem);
                }
            }
            Logger.logEnd("Clone");
            return objNew;
        }


    }

    public class TestDataObject : ConfigObjectBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestDataObject));

        #region Properties
        public string DataObjectName;
        public List<string> TestData = new List<string>();
        #endregion

#if _Datafrom_Database
        private List<TEST_DATA_SETTINGDTO> assignedObjectsList=new List<TEST_DATA_SETTINGDTO>();

        internal void FetchDataToLog()
        {
            foreach(var itm in assignedObjectsList)
            {
                Logger.Info("TestDataObject",string.Format("DATA_SETTING_ID:[{0}] DATA_SUMMARY_ID:[{1}] DATA_VALUE:[{2}] LOOP_ID:[{3}] Pool_id:[{4}] Steps_id:[{5}]", itm.DATA_SETTING_ID,itm.DATA_SUMMARY_ID,itm.DATA_VALUE,itm.LOOP_ID,itm.POOL_ID,itm.STEPS_ID));
            }
        }

        protected void InsertAssobject(int iIdx, TEST_DATA_SETTINGDTO objDataSettingFromDB)
        {
            if (objDataSettingFromDB==null)
            {
                Logger.Warnning("InsertAssobject", "objDataSettingFromDB == null");
                return;
            }
            while(assignedObjectsList.Count<iIdx)
            {
                assignedObjectsList.Add(null);
            }
            assignedObjectsList[iIdx-1] = objDataSettingFromDB;
        }
        protected void InsertDataList(int iIdx, string strData)
        {
            if (iIdx < 1 ) return;
            while (TestData.Count < iIdx)
            {
                TestData.Add("");
            }
            TestData[iIdx - 1] = strData;
        }
        internal void InsertNewDataValue(TEST_DATA_SETTINGDTO objDataFromDB)
        {
            /// insert data 
            /// 
            if (objDataFromDB == null)
            {
                Logger.Warnning("InsertNewDataValue", "objDataFromDB is null");
                return;
            }
            int iIdx = objDataFromDB.LOOP_ID == null ? -1 : (int)objDataFromDB.LOOP_ID; 
            if (iIdx < 1)
            {
                Logger.Error("InsertNewDataValue", string.Format("Database Data error, Loop Id shouldnot be less than 0, but it is :[{0}]", iIdx));
                return;
            }
            InsertDataList(iIdx, objDataFromDB.DATA_VALUE);
            InsertAssobject(iIdx, objDataFromDB);
        }

        internal int GetAssignedStpId(int iLoopId)
        {
            if ((iLoopId >= this.assignedObjectsList.Count)||(iLoopId<0)) return int.MinValue;
            return (int)assignedObjectsList[iLoopId].STEPS_ID;
        }
#endif

        public string GetSpecialColumnData(int iColumn, ref ERROR_CODE eCde)
        {
            Logger.logBegin("GetSpecialColumnData");
            if (TestData == null)
            {
                eCde = ERROR_CODE._TCDATA_NO_DATA_FIND;
                Logger.Error("GetSpecialColumnData", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), DataObjectName));
                return null;
            }
            if ((iColumn < 0) || (iColumn >= this.TestData.Count))
            {
                eCde = ERROR_CODE._TCDATA_DATA_COLUMN_EXCEED;
                Logger.Error("GetSpecialColumnData", ERROR_INFO.GET_ERROR_STR(eCde));
                return null;
            }

            string strResult = TestData[iColumn];
            Logger.logEnd(string.Format("GetSpecialColumnData -{0}", TigerMarsUtil.GetParameter("value", strResult)));
            return strResult;
        }

        internal int GetColumnCount()
        {
            Logger.logBegin("GetColumnCount");
            Logger.logEnd("GetColumnCount");
            return TestData == null ? int.MinValue : TestData.Count;
        }

        
    }
#if _Datafrom_Database
    internal class TestCaseData: TCDataFile
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestCaseData));

        public readonly V_STORYBOARD_TEST_FULLVISIONDTO TestStoryBoardRef;
        private string currentDBIdx = "MarEntities";
        /// <summary>
        /// override the methods from parents, as it will get data from database
        /// </summary>
        /// <returns></returns>
        public override ERROR_CODE loadTestCase()
        {
            return GetDataFromDatabaseByStoryBoardAssigned(currentDBIdx);
        }

        private T_TEST_DATA_SUMMARYDTO currentDataSummary = null;
        private IList<T_SHARED_OBJECT_POOLDTO> assignedObjectPool = null;
        //private IList<TEST_DATA_SETTINGDTO> dataAssignedToTestCase = null;
        private IList<KeyValuePair<TEST_DATA_SETTINGDTO, string>> dataWithObjectNameList = null;
        //private IList<T_REGISTED_OBJECTDTO> objectsForTestSteps = null;
                

        private ERROR_CODE GetDataFromDatabaseByStoryBoardAssigned(string strDBIdx)
        {
            
            if (TestStoryBoardRef == null)
            {
                Logger.Error("GetDataFromDatabaseByStoryBoardAssigned",ERROR_INFO.GET_ERROR_STR(ERROR_CODE._TCDATA_DATA_DB_NO_STORYBOARD_INFO_0));
                return ERROR_CODE._TCDATA_DATA_DB_NO_STORYBOARD_INFO_0;
            }

            /// get data based on story board id and data set id
            /// Load Data Summary
            /// 
            Logger.Info("GetDataFromDatabaseByStoryBoardAssigned", string.Format(@"try to get data from data set for 
                STORYBOARD_DETAIL_ID:[{0}] 
                TEST_CASE_ID:[{1}],
                RUN_ORDER:[{2}]",
                TestStoryBoardRef.STORYBOARD_DETAIL_ID, TestStoryBoardRef.TEST_CASE_ID, TestStoryBoardRef.RUN_ORDER));
            currentDataSummary = BoHelper.GetDataSummaryByStoryBoardIdTestCaseIDRunorder(strDBIdx,TestStoryBoardRef.STORYBOARD_DETAIL_ID, TestStoryBoardRef.TEST_CASE_ID, TestStoryBoardRef.RUN_ORDER);
            
            if (currentDataSummary == null)
            {
                assignedObjectPool = null;
                dataWithObjectNameList = null;                 
            }
            else 
            {
                //Logger.Info("GetDataFromDatabaseByStoryBoardAssigned", string.Format("Get DataSummary_id:[{0}]", currentDataSummary.));
                //assignedObjectPool = BoHelper.GetSharedObjectPoolInfoByDataSummaryId(currentDataSummary.DATA_SUMMARY_ID);
                /// Get designed Data from Table 
                /// 
                try
                {
                    dataWithObjectNameList = BoHelper.GetAssignedTestDataByTestCaseID(strDBIdx,TestStoryBoardRef.TEST_CASE_ID, currentDataSummary.DATA_SUMMARY_ID);
                    ///将数据进行skip的处理，如果data_direction有skip标记，就将data改为skip
                    /// added:12-20-2017
                    /// tiger
                    foreach(var itm in dataWithObjectNameList)                    
                    {
                        TEST_DATA_SETTINGDTO objDt = null;
                        if (itm.Equals(default(KeyValuePair<TEST_DATA_SETTINGDTO, string>))) continue;
                        objDt = itm.Key;
                        if (objDt == null) continue;
                        if (((objDt.DATA_DIRECTION??0)&4)==4)
                        {
                            Logger.Info("GetDataFromDatabaseByStoryBoardAssigned", string.Format("One row [stepid:{0}] is set to skip, value:[{1}] is replaced by SKIP", objDt.STEPS_ID,objDt.DATA_VALUE));
                            objDt.DATA_VALUE = "SKIP";
                        }
                    }
                }
                catch (Exception e)
                {
                    dataWithObjectNameList = null;
                    Logger.Error("GetDataFromDatabaseByStoryBoardAssigned",string.Format("Exception:[{0}], trace:[{1}]",e.Message,e.StackTrace),e);
                }
                
            }
            ///  dataWithObjectNameList should be orgnized like old style
            /// 
            PackData2BaseMode();

            return ERROR_CODE._NO_ERROR;
        }

        private string combineKeysForDataIndex(KeyValuePair<TEST_DATA_SETTINGDTO, string> objSrc)
        {
            return string.Format(string.Format("[{0}]_[{1}],", objSrc.Value, objSrc.Key.STEPS_ID));
        }

        private void PackData2BaseMode()
        {
            Logger.logBegin("PackData2BaseMode");
            Hashtable hsTmpDataSet = new Hashtable();
            TestDataObject objTestDataObj = null;
            if (dataWithObjectNameList == null) return ;
            foreach (KeyValuePair < TEST_DATA_SETTINGDTO, string> objDataItem  in dataWithObjectNameList)
            {
                
                if (objDataItem.Equals(default(KeyValuePair<TEST_DATA_SETTINGDTO, string>))) continue;
                string strDataIdx = combineKeysForDataIndex(objDataItem);
                //if (!hsTmpDataSet.ContainsKey(string.Format("[{0}]_[{1}],",objDataItem.Value, objDataItem.Key.STEPS_ID)))
                if (!hsTmpDataSet.ContainsKey(strDataIdx))                    
                {
                    objTestDataObj = new TestDataObject();
                    objTestDataObj.DataObjectName = objDataItem.Value;
                    mlstDataObject.Add(objTestDataObj);
                    hsTmpDataSet.Add(string.Format("[{0}]_[{1}],", objDataItem.Value, objDataItem.Key.STEPS_ID), objTestDataObj);
                }
                else
                {
                    //if (((TestDataObject)hsTmpDataSet[objDataItem.Value]).)
                    objTestDataObj = (TestDataObject)hsTmpDataSet[strDataIdx];
                }
                objTestDataObj.InsertNewDataValue(objDataItem.Key);
            }
            hsTmpDataSet.Clear();
            hsTmpDataSet = null;
            Logger.logEnd("PackData2BaseMode"); 
        }

        public TestCaseData(string strDBIdx, V_STORYBOARD_TEST_FULLVISIONDTO objStoryBoardRef):base()
        {
            currentDBIdx = strDBIdx;
            TestStoryBoardRef = objStoryBoardRef;
        }

#if !_Datafrom_Database
        public override ERROR_CODE GetOneCellValueFromData(int iLoopId, string strCellIdx, ref string strResult, string strSheetName = CNST_DATA_SHEETNAME)
#else
        public override ERROR_CODE GetOneCellValueFromData(int iLoopId, string strCellIdx, ref string strResult, int iStepId=-1,string strSheetName = CNST_DATA_SHEETNAME)
#endif
        {
            ///base.GetOneCellValueFromData
            /// 
            Logger.Info("GetOneCellValueFromData",string.Format("iLoopId:[{0}],strCellIdx:[{1}], iStepId:[{2}], strSheetName:[{3}]", iLoopId, strCellIdx, iStepId,strSheetName));
            ERROR_CODE eCde;
            try
            {
                //mlstDataObject.Where(p => string.Compare(((TestDataObject)p).DataObjectName, strCellIdx, true) == 0).FirstOrDefault();
                foreach (ConfigObjectBase objItem in this.mlstDataObject)
                {
                    if (objItem == null) continue;
                    TestDataObject objCurrentItm = (TestDataObject)objItem;
                    if (objCurrentItm.DataObjectName == null) continue;
                    if (string.Compare(objCurrentItm.DataObjectName, strCellIdx, true) != 0) continue;

                    /// iLoopId starts from 0
                    /// 
                    if (objCurrentItm.TestData == null)
                    {
                        Logger.Warnning("GetOneCellValueFromData", "TestData is null, empty string is returned");
                        strResult = "";
                        return ERROR_CODE._NO_ERROR;
                    }
                    if ((objCurrentItm.TestData.Count < iLoopId) || (iLoopId < 0))
                    {
                        eCde = ERROR_CODE._TCDATA_DATA_NO_SPECIAL_CELLDATA_PARA_2;
                        Logger.Error("GetOneCellValueFromData", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strCellIdx, iLoopId));
                        return eCde;
                    }
                    if (iStepId == -1)
                        strResult = objCurrentItm.TestData[iLoopId - 1];
                    else
                    {
                        if (iStepId != objCurrentItm.GetAssignedStpId(iLoopId - 1))
                            continue;
                        strResult = objCurrentItm.TestData[iLoopId - 1];
                    }
                    Logger.Info("GetOneCellValueFromData", string.Format("Get data [{1}] for [{0}] ", strCellIdx, strResult));
                    return eCde = ERROR_CODE._NO_ERROR;

                }
                if (SystemConstant.CNST_XLS_DATAFIELD_SYSTEM_RUNMARK.CompareTo(strCellIdx) == 0)
                {
                    strResult = "run";
                    Logger.Info("GetOneCellValueFromData", SystemConstant.CNST_XLS_DATAFIELD_SYSTEM_RUNMARK);
                    return eCde = ERROR_CODE._NO_ERROR;
                }
                eCde = ERROR_CODE._TCDATA_DATA_NO_SPECIAL_CELLDATA_PARA_2;
                Logger.Error("GetOneCellValueFromData", string.Format(ERROR_INFO.GET_ERROR_STR(eCde), strCellIdx, iLoopId));
                return eCde;
            }
            catch(Exception e)
            {
                eCde = ERROR_CODE._TCDATA_DATA_GETDATA_DB_EXCEPTION_PARA_3;
                Logger.Error("GetOneCellValueFromData",string.Format(ERROR_INFO.GET_ERROR_STR(eCde),e.Message, iStepId, strCellIdx));
                return eCde;
            }
            finally
            {
                Logger.logEnd("GetOneCellValueFromData");
            }
            
        }

    }

   

    [Serializable]
    public class TestStepsFromDB : TestStep
    {
        private static MLogger logger = MLogger.GetLogger(typeof(TestStepsFromDB));
        [DataMember]
        public V_TEST_STEPS_FULLVISIONDTO TestStepsFullVisionDTO { get; set; }

        internal override void CloneTo(TestStep objTarget)
        {
            if (objTarget == null) return;
            base.CloneTo(objTarget);
            objTarget.AssignedStepdId = this.TestStepsFullVisionDTO.STEPS_ID;
            if (objTarget is TestStepsFromDB)
            {
                ((TestStepsFromDB)objTarget).TestStepsFullVisionDTO = this.TestStepsFullVisionDTO;
            }
        }

        internal static TestStepsFromDB CreateObjectFromDBStepInfo(V_TEST_STEPS_FULLVISIONDTO objSrc)
        {
            if (objSrc == null) return null;
            TestStepsFromDB objResult = new TestStepsFromDB();
            objResult.Keyword = objSrc.KEY_WORD_NAME;
            objResult.ObjectName = objSrc.OBJECT_HAPPY_NAME;
            objResult.Row_Column = objSrc.COLUMN_ROW_SETTING;
            objResult.Value = objSrc.VALUE_SETTING;
            objResult.Comment = objSrc.COMMENTINFO;
            objResult.RunID =(int)objSrc.RUN_ORDER;

            objResult.TestStepsFullVisionDTO = objSrc;
            logger.Info("CreateObjectFromDBStepInfo", objResult.ToString());

            if (string.IsNullOrEmpty(objResult.Keyword)) return null;
            return objResult;
        }

        internal override void CloneToService(TestStep4Services objServiceObj)
        {
            base.CloneToService(objServiceObj);
            objServiceObj.AssignedTestStepId = this.AssignedStepdId;
        }

        public override string ToString()
        {
            
            return string.Format("{0}{1}",base.ToString(),this.TestStepsFullVisionDTO==null?"": string.Format("TestStepsFullVisionDTO:[objectId:{0} KEY_WORD_NAME:[{1}] stepid:[{2}], OBJECT_HAPPY_NAME:[{3}]]", this.TestStepsFullVisionDTO.OBJECT_ID, this.TestStepsFullVisionDTO.KEY_WORD_NAME,this.TestStepsFullVisionDTO.STEPS_ID,this.TestStepsFullVisionDTO.OBJECT_HAPPY_NAME));
        }
    }
#endif
    }
