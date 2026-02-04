using com.Mars.Constants;
using MarsTestFrame.SourceCode.com.Mars.KeyWords.KeyWordObject;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.KeyWords
{
    public class KeyWordsMainEntry
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeyWordsMainEntry));
        private static KeyWordsMainEntry Instance = null;

        #region member section
        protected Hashtable mhstAllKeywords = null;
        protected TestKeywSource meTKSCurrent = TestKeywSource.TKS_Not_Init;
        #endregion


        private KeyWordsMainEntry()
        {
           
        }

        public static KeyWordsMainEntry GetInstance()
        {
            Logger.logBegin("GetInstance");
            if (Instance == null) Instance = new KeyWordsMainEntry();
            Logger.logEnd("GetInstance");
            return Instance;
        }

        public Hashtable getAllKeyWords(string strDBIdx)
        {
            if (meTKSCurrent==TestKeywSource.TKS_Not_Init)
            {
                /*** ***/
                meTKSCurrent = AppConfigReader.GetKeyWordsFrom();
            }
            /** Get adapter **/
            IKeywordsLoader objKeyWordsLoader = KeyWordAdapterFactory.GetKeywordsLoader(meTKSCurrent);
            if (objKeyWordsLoader == null)
            {
                throw new MarsExceptions((int)ERROR_CODE._APP_WRONG_VALUE_SETTING_KEYWORD_FROM, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._APP_WRONG_VALUE_SETTING_KEYWORD_FROM), meTKSCurrent));
            }
            mhstAllKeywords=objKeyWordsLoader.LoadKeywords(strDBIdx);

            return mhstAllKeywords ;
        }

        public static Hashtable GetAllKeyWords(string strDBIdx)
        {
            Logger.logBegin("GetAllKeyWords");
            KeyWordsMainEntry objInstance = GetInstance();
            Hashtable hstResult = objInstance.getAllKeyWords(strDBIdx);
            Logger.logEnd("GetAllKeyWords");
            return hstResult;
        }

        public KeyWordObjectInfo GetKeyWordFunctionByName(string strDBIdx,string strKeywordName)
        {
            Logger.logBegin("GetKeyWordFunctionByName" );
            Logger.Info("GetKeyWordFunctionByName",TigerMarsUtil.GetParameter("KeywordName",strKeywordName));
            if (mhstAllKeywords == null)
            {
                getAllKeyWords(strDBIdx);
            }
            if (mhstAllKeywords.ContainsKey(strKeywordName.ToUpper()))
            {
                return (KeyWordObjectInfo)mhstAllKeywords[strKeywordName.ToUpper()];
            }
            return null;
        }
    }
}
