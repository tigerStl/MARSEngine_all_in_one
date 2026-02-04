extern alias clientWCF;
using com.Mars.Constants;
using com.Mars.KeyWords.KeyWordObject;
using MarsTestFrame.SourceCode.systemUtil;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.KeyWords.KeyWordObject
{

    public interface IKeywordsLoader
    {
        Hashtable LoadKeywords();
    }
    
    public sealed class KeyWordAdapterFactory
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeyWordAdapterFactory));
        public static IKeywordsLoader GetKeywordsLoader(TestKeywSource eAdpt)
        {
            Logger.logBegin("GetKeywordsLoader");
            try
            {
                switch (eAdpt)
                {
                    case TestKeywSource.TKS_From_ConfigFile: return new ConfigKeyWordsLoader();
                    default: return null;
                }
            }
            finally
            {
                Logger.logEnd("GetKeywordsLoader");
            }      
            
        }

    }

    class ConfigKeyWordsLoader :IKeywordsLoader
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ConfigKeyWordsLoader));
        public Hashtable LoadKeywords()
        {
            Logger.logBegin("LoadKeywords");
            Hashtable hstKeyWords = new Hashtable();
            try
            {
                KeyWordsConfigCollection lstKeywords = AppConfigReader.GetKeyWordsList();
                for (int i = 0; i < lstKeywords.Count;i++ )
                {
                    KeyWordsElement objKeyWordElement = lstKeywords[i];
                    if (objKeyWordElement == null) continue;
                    /*** convert to Keywords object ***/
                    KeyWordObjectInfo objKeyWord = new KeyWordObjectInfo();
                    objKeyWord.KeywordName = objKeyWordElement.KeywordName;
                    objKeyWord.ParameterParseClassName = objKeyWordElement.ParseClass;
                    objKeyWord.RunFrom = KeyWordRunFrom.GetRunFromInstance(objKeyWordElement.RunFrom);
                    (objKeyWord.AppliedApplications = new KeyWordAppliedApplication()).ApplicationNames = objKeyWordElement.AppliedApps;
                    ERROR_CODE eCde = objKeyWord.Validate();
                    if (eCde == ERROR_CODE._NO_ERROR)
                    {
                        if (!hstKeyWords.ContainsKey(objKeyWord.KeywordName.ToUpper()))
                            hstKeyWords.Add(objKeyWord.KeywordName.ToUpper(), objKeyWord);
                    }
                }
                return hstKeyWords;
            }
            finally
            {
                Logger.logEnd("LoadKeywords");
            }            
        }       
    }
}
