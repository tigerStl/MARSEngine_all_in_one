using com.Mars.Constants;
using com.Mars.TestFrame.Application;
using com.Mars.TestFrame.TestObjects;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.KeyWords.KeyWordObject
{
    /** default validate class **/
    public class KeyWordsValidate : IKeyWordParse
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeyWordsValidate));

        protected E_KeywordParameterID ParameterRequireInfo = E_KeywordParameterID.e_Keyword_Parameter_None;

        protected string currentDBIdx = "MarsEntities";
        public KeyWordsValidate(string strDBIdx)
        {
            currentDBIdx = strDBIdx;
            InitialData();
        }

        protected virtual void InitialData()
        {

        }
        protected bool getKeywordObject(string strApplicationShortName, string strPegWindowName, string strObjHappyName, ref ERROR_CODE eCode, ref TestObject objTest)
        {
            Logger.logBegin("getKeywordObject");
            try
            {
                TargetApplicationInfo objApp = TargetApplicationsManagement.GetApplicationByShortName(strApplicationShortName);
                if (objApp == null)
                {
                    eCode = ERROR_CODE._KEYWORDS_PARSE_NO_SUCHAPPLICATION_SHORTNAME;
                    return false;
                }

                /*** get peg windows information by short name and pegwindows name ***/
                List<TestPegwindowObject> lstPegs = TestObjectsManagement.GetPegwindowsByValues(currentDBIdx, strPegWindowName, strApplicationShortName);
                if ((lstPegs == null) || (lstPegs.Count == 0))
                {
                    eCode = ERROR_CODE._KEYWORDS_PARSE_NO_SUCH_PEGWINDOW;
                    Logger.Error("IsRightFormatForObject", string.Format(ERROR_INFO.GET_ERROR_STR(eCode), strPegWindowName, strApplicationShortName));
                    return false;
                }

                /*** get objects ***/
                objTest = lstPegs[0].GetChildrenObjctsByName(strObjHappyName);
                if (objTest == null)
                {
                    eCode = ERROR_CODE._KEYWORDS_PARSE_NO_SUCH_OBJECT_UNDER_PEGWINDOW;
                    Logger.Error("IsRightFormatForObject", string.Format(ERROR_INFO.GET_ERROR_STR(eCode), strApplicationShortName, strPegWindowName, strObjHappyName));
                    return false;
                }
                return false;
            }
            finally
            {
                Logger.logEnd("getKeywordObject");
            }
        }

        public virtual bool IsRightFormatForObject(string strApplicationShortName, string strPegWindowName, string strObjHappyName, ref ERROR_CODE eCode)
        {
            Logger.logBegin("IsRightFormatForObject");
            /*** ***/
            try
            {
                TestObject objTest = null;
                bool isExists = getKeywordObject(strApplicationShortName, strPegWindowName, strObjHappyName, ref eCode, ref objTest);
                return isExists;
            }
            catch (MarsExceptions eM)
            {
                eCode = (ERROR_CODE)eM.ErrorId;
                Logger.Error("IsRightFormatForObject", eM.Message, eM);
                return false;
            }
            finally
            {
                Logger.logEnd("IsRightFormatForObject");
            }
        }

        public virtual bool IsRightFormatForRowAndColumn(string strApplicationShortName, string strPegWindowName, string strObjHappyName, string strRC, ref ERROR_CODE eCode)
        {
            return true;
        }

        public virtual bool IsRightFormatForDataUnderScript(string strValue_RC, ref ERROR_CODE eCode)
        {
            return true;
        }

        public virtual E_KeywordParameterID IsParameterRequired()
        {
            return this.ParameterRequireInfo;
        }
    }

    public class FillEditKeyWordsValidate : KeyWordsValidate
    {
        public FillEditKeyWordsValidate(string strDBIdx) : base(strDBIdx)
        {

        }
    }

    public class KeywordWithoutObject:KeyWordsValidate
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeywordWithoutObject));

        public KeywordWithoutObject(string strDBIdx) : base(strDBIdx)
        {

        }
        protected override void InitialData()
        {
            this.ParameterRequireInfo = E_KeywordParameterID.e_Keyword_Parameter_RC | E_KeywordParameterID.e_Keyword_Parameter_Value ;
        }
    }
    public class KeywordRequireValue:KeyWordsValidate
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeywordRequireValue));

        public KeywordRequireValue(string strDBIdx) : base(strDBIdx)
        {

        }
        protected override void InitialData()
        {
            this.ParameterRequireInfo = E_KeywordParameterID.e_Keyword_Parameter_Value;
        }
    }
}
