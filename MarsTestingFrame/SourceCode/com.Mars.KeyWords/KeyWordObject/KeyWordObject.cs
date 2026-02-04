using com.Mars.Constants;
using com.Mars.TestFrame.Application;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.KeyWords.KeyWordObject
{
    public class KeyWordObjectInfo : ConfigObjectBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeyWordObjectInfo));

        #region properties
        public string KeywordName;
        public KeyWordAppliedApplication AppliedApplications = new KeyWordAppliedApplication();
        public KeyWordRunFrom RunFrom = null;
        public string ParameterParseClassName = null;
        public IKeyWordParse ParseInstance = null;
        #endregion

        public ERROR_CODE Validate(string strDBIdx)
        {
            Logger.logBegin("Validate");
            try
            {
                /*** to check whether the setting information is right ***/
                ERROR_CODE eCde = ValidateApplicationsSetting();
                if (eCde != ERROR_CODE._NO_ERROR)
                { return eCde; }
                eCde = ValidateParseClass(strDBIdx);
                if (eCde!=ERROR_CODE._NO_ERROR)
                { return eCde; }
                //eCde = ValidateConceptStr();
                return eCde;
            }
            finally
            {
                Logger.logEnd("Validate");
            }
        }

        private ERROR_CODE ValidateConceptStr()
        {
            Logger.logBegin("ValidateConceptStr");
            /** get From Adapter **/
            if (this.RunFrom == null) return ERROR_CODE._KEYWORDS_UNSUPPORT_RUN_FROM;
            this.RunFrom.InitConceptScript(this.KeywordName);
            
            Logger.logEnd("ValidateConceptStr");
            return ERROR_CODE._NO_ERROR;
        }

        private ERROR_CODE ValidateApplicationsSetting()
        {
            Logger.logBegin("ValidateApplicationsSetting");
            try
            {
                string[] arrStrApps = AppliedApplications.ApplicationNames.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string strAppShortName in arrStrApps)
                {
                    if (string.Compare(SystemConstant.CNST_APPCONFIG_KEYWORDS_APP_VALUE_ALL, strAppShortName, true) == 0)
                    {
                        /** get all application **/
                        return LoadAllApplications();
                    }

                    TargetApplicationInfo objApplication = TargetApplicationsManagement.GetApplicationByShortName(strAppShortName);
                    if (objApplication==null)
                    {
                        throw new MarsExceptions((int)ERROR_CODE._KEYWORDS_SETTING_NO_SUCHAPPLICATION_SHORTNAME, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_SETTING_NO_SUCHAPPLICATION_SHORTNAME), strAppShortName));
                    }
                    AppliedApplications.AddApplicationInfo(strAppShortName, objApplication);
                }
                return ERROR_CODE._NO_ERROR;
            }
            finally
            {
                Logger.logEnd("ValidateApplicationsSetting");
            }
            
        }

        private ERROR_CODE LoadAllApplications()
        {
            List<TargetApplicationInfo> lstAllApps = TargetApplicationsManagement.GetAllApplications();
            if (lstAllApps==null) return ERROR_CODE._NO_ERROR ;
            foreach (TargetApplicationInfo objApp in lstAllApps)
            {
                if (objApp == null) continue;
                AppliedApplications.AddApplicationInfo(objApp.ApplicationShortName, objApp);
            }
            return ERROR_CODE._NO_ERROR;
        }

        private ERROR_CODE ValidateParseClass(string strDBIdx)
        {
            Logger.logBegin("ValidateParseClass");
            if ((this.ParameterParseClassName == null) || (this.ParameterParseClassName == ""))
            {
                /*** default validate class is used ***/
                ParseInstance = new KeyWordsValidate(strDBIdx);
                return ERROR_CODE._NO_ERROR;
            }
            if (ParseInstance == null)
            {
                /** reflection by class name **/
                try
                {
                    object objParse = Assembly.GetExecutingAssembly().CreateInstance(this.ParameterParseClassName);
                    //object objParse = typeof(KeyWordObject).Assembly.CreateInstance(this.ParameterParseClassName);
                    if (objParse == null)
                    {
                        ERROR_CODE eCode = ERROR_CODE._KEYWORDS_PARSE_REFLECTION_NULL;
                        Logger.Error("ValidateParseClass", string.Format(ERROR_INFO.GET_ERROR_STR(eCode), this.ParameterParseClassName));
                        return eCode;
                    }
                    if (!(objParse is KeyWordsValidate))
                    {
                        Logger.Error("ValidateParseClass", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_PARSE_CONFIGED_OBJECT_TYPE_WRONG), this.ParameterParseClassName));
                        return ERROR_CODE._KEYWORDS_PARSE_CONFIGED_OBJECT_TYPE_WRONG;
                    }
                    ParseInstance = (IKeyWordParse)objParse;
                    return ERROR_CODE._NO_ERROR;
                }
                catch (Exception e)
                {
                    Logger.Error("ValidateParseClass", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_PARSE_REFLECTION), this.ParameterParseClassName, e.Message), e);
                    return ERROR_CODE._KEYWORDS_PARSE_REFLECTION;
                }
                finally
                {
                    Logger.logEnd("ValidateParseClass");
                }
            }
            else
                return ERROR_CODE._NO_ERROR;
            
        }

        internal string GetKeywordDefScript()
        {
            if (this.RunFrom == null) return "";
            return RunFrom.ConceptScript;
        }

        internal static Keyword_RunType GetKeywordRunType(string strKeyWordName)
        {
            Keyword_RunType eRunType = Keyword_RunType._NORMAL_RUNTYPE;
            if (string.Compare(SystemConstant.CNST_SUBACTION_KEYWORD_IF, strKeyWordName, true) == 0)
                return Keyword_RunType._SUBACTION_RUNTYPE_SELECTION;
            foreach(string strKeyName in SystemConstant.CNST_ARR_KEYWORD_SUBACTIONS)
            {
                if (string.Compare(strKeyWordName, strKeyWordName, true) == 0) return Keyword_RunType._NORMAL_RUNTYPE;
            }
            return eRunType;
        }
    }

    public class KeyWordAppliedApplication
    {
        #region property
        public string ApplicationNames;
        public Hashtable ApplicationsTable = new Hashtable();
        #endregion

        public void AddApplicationInfo(string strApplicationShortName, TargetApplicationInfo objApp)
        {
            if (ApplicationsTable.ContainsKey(strApplicationShortName))
            {
                return;
            }
            ApplicationsTable.Add(strApplicationShortName, objApp);
        }
    }

    public class KeyWordRunFrom
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeyWordRunFrom));
        protected TestClassStatus CurrentStatus=TestClassStatus.TCS_Not_Init;
        public static KeyWordRunFrom GetRunFromInstance(string strAdapterKey)
        {
            Logger.logBegin("GetRunFromInstance");
            KeyWordRunFrom objResult = null;
            if ((string.Compare(SystemConstant.CNST_APPCONFIG_KEYWORDS_RUN_FROM_QTP, strAdapterKey, true)==0)
                ||(string.Compare(SystemConstant.CNST_APPCONFIG_KEYWORDS_RUN_FROM_UTF, strAdapterKey,true)==0))
            {
                Logger.Info("GetRunFromInstance", SystemConstant.CNST_APPCONFIG_KEYWORDS_RUN_FROM_UTF);
                objResult = new KeywordRunFromQTP();
                objResult.RunFrom = strAdapterKey;
                return objResult;
            }
            objResult = new KeyWordRunFrom();
            objResult.RunFrom = strAdapterKey;
            return objResult ;
        }

        #region property
        public string RunFrom;
        #endregion
        public string ConceptScript;

        public virtual ERROR_CODE InitConceptScript(string strKeyword)
        {
            CurrentStatus = TestClassStatus.TCS_Initilized;
            return ERROR_CODE._NO_ERROR;
        }

        public virtual string GetRunnableScript(string strKeyword, string strObject, string RC, string strData, ref string strDataFromTC)
        {
            return "";
        }
    }

    /*** run from QTP ***/
    public class KeywordRunFromQTP : KeyWordRunFrom
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeywordRunFromQTP));

        public override ERROR_CODE InitConceptScript(string strKeyword)
        {
            Logger.logBegin("InitConceptScript");
            if (this.CurrentStatus != TestClassStatus.TCS_Not_Init)
            {
                Logger.Info("InitConceptScript", string.Format(MarsTestFrame.Properties.Resources.HINT_CLASS_INITED, "KeywordRunFromQTP"));
                return ERROR_CODE._NO_ERROR;
            }
            string strKeywordsIndex = string.Format(SystemConstant.CNST_KEYWORD_CONCEPT_PREFIX_QTP, strKeyword.ToUpper());            
            string strConcept = Properties.KeywordsDef.ResourceManager.GetString(strKeywordsIndex);
            if ((strConcept==null)||(strConcept==""))
            {
                /*** no such keyword's definition found ***/
                this.ConceptScript = null;
                Logger.Error("InitConceptScript", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._KEYWORDS_NO_SUCH_KEYWORDS_CONCEPT_FOUND),strKeyword));
                return ERROR_CODE._KEYWORDS_NO_SUCH_KEYWORDS_CONCEPT_FOUND;
            }
            this.ConceptScript = strConcept;
            Logger.logEnd("InitConceptScript");
            this.CurrentStatus = TestClassStatus.TCS_Initilized;
            return ERROR_CODE._NO_ERROR;
        }

        public override string GetRunnableScript(string strKeyword, string strObject, string RC, string strData, ref string strDataFromTC)
        {
            Logger.logBegin("GetRunnableScript");
            Logger.logEnd("GetRunnableScript");
            return null;
        }
    }


}
