using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
#if !_noEntities
using Route2NSEx.src.Marquis.systemUtil;
#endif

using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;

#if _marsLog
using Logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.Business
{
    public class B_KEYWORD : T_KEYWORDDTO
    {
#if !_marsLog
        private static MLogger Logger = MLogger.GetLogger(typeof(B_KEYWORD));
#endif
        private static long _staticTypeId = 20; // represents id of static keywords, those that  should be available to ant object
                                                //public long KEY_WORD_ID { get; set; }
        public const string CNST_KEYWORD_CAPTUREANDCOMPARE = "CaptureAndCompare";

        //public String KEY_WORD_NAME { get; set; }

        // public String DESCRIPTION { get; set; }

        // public String ENTRY_IN_DATA_FILE { get; set; }


        //public List<Int64> T_DIC_RELATION_KEYWORD_RELATION_ID { get; set; }
        private static Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> CachedKeywords = null;
        public static bool IsKeywordPegwindow(long lKeyId, ref string strError, ref bool isOk,
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("IsKeywordPegwindow", string.Format("keyword id:[{0}]", lKeyId));
            try
            {
                if (CachedKeywords == null)
                {
#if !_forWebClient
                    CachedKeywords = (new B_KEYWORD()).LoadAllKeywords(strDBIdx);
#else
                    CachedKeywords = (new MarsRESTfulApiClient(strDBIdx)).LoadAllKeywords(ref isOk, ref strError);
                    if (!isOk)
                    {
                        return false;
                    }
#endif
                }
                var k = (from z in CachedKeywords.Keys
                         where string.Compare(z.KEY_WORD_NAME, "Pegwindow", true) == 0
                         && z.KEY_WORD_ID == lKeyId
                         select z).FirstOrDefault();
                isOk = true;
                if (k == null)
                {
                    strError = string.Format("Key [{0}] is not Pegwindow", lKeyId);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("IsKeywordPegwindow", strError = string.Format("Exception:[{0}]", e.Message), e);
                isOk = false;
                return false;
            }

        }

        protected readonly static List<string> _keywordNotRequireObject = new List<string> {
            "AssertValue",
            "CheckError",
            "ClickAt",
            "ClickMenuIcon",
            "CopyExcelRangeToClipboard",
            "DBCompare",
            "QueryDataFromDataSource",
            "Dismiss",
            "ExecuteCommand",
            "LaunchApplication",
            "LoadVariables",
            "KillApplication",
            "Comment",
            "IF",
            "ELSE",
            "IFEND",
            "LOOP",
            "PressKeys",
            "Sub_Loop", 
            "EndLoop",
            "End_Sub_Loop",
            "RemovePage",
            "RemoveVariable",
            "ResumeNext",
            "SelectMenuItem",
            "SetDataFile",
            "SetDefaultDataFile",
            "ScrollWindow",
            "StartApplication",
            "WaitForSeconds",            
            "WaitUntil",
            "WebSwitchToRoot",
            "WebTestDialog",
            "ConnectToDevice"
        };

        public static List<string> KeywordNotRequireObject { get { return _keywordNotRequireObject; } }
        public static bool IsKeywordNotRequireObject(string strKeywordName)
        {
            return _keywordNotRequireObject.Any(p => string.Compare(p, strKeywordName, true) == 0);
        }
        public static bool IsKeywordNotRequireObject(long lKeywordId, ref bool isOk, string strDBIdx)// = MarsEntitiesExtends.cnst_default_dbName)
        {
            if (CachedKeywords == null)
            {
#if !_forWebClient
                CachedKeywords = (new B_KEYWORD()).LoadAllKeywords(strDBIdx);
#else
                string strError = "";
                CachedKeywords = (new MarsRESTfulApiClient(strDBIdx)).LoadAllKeywords(ref isOk, ref strError);
                if (!isOk)
                {
                    return false;
                }
#endif
            }
            var k = (from z in CachedKeywords.Keys
                     where z.KEY_WORD_ID == lKeywordId
                     select z).FirstOrDefault();

            if (!(isOk = k != null)) return false;
            return IsKeywordNotRequireObject(k.KEY_WORD_NAME);
        }
        /// <summary>
        /// 无对象keyword的例外处理。当CaptureAndCompare的参数startwith FromMem:时候，可以不需要object的参数
        /// </summary>
        /// <param name="strKeyword"></param>
        /// <param name="strParameter"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public static bool ExceptForKeywordWithoutObj(string strKeyword ,string strParameter, ref string strError)
        {
            if (!CNST_KEYWORD_CAPTUREANDCOMPARE.Equals(strKeyword, StringComparison.OrdinalIgnoreCase)) {
                strError = $"keyword |{strKeyword}| requires an object ";
                return false;
            }
            if (string.IsNullOrEmpty(strParameter)) {
                strError = $"keyword |{strKeyword}| should has parameters|for example:FromMem:Variable_name| if no object is set";
                return false;
            }
            if (!strParameter.StartsWith("FromMem:", StringComparison.OrdinalIgnoreCase))
            {
                strError = $"keyword |{strKeyword}| 's parameter should startwith 'FromMem:' if no Object is set";
                return false;
            }
            return true;
        }

#if !_noEntities
        public List<B_KEYWORD> GetKeywords(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_KEYWORD> keywordList = new List<B_KEYWORD>();
            List<Int64> keywordRelList;

            var keywords = (from c in marsEntities.T_KEYWORD
                            orderby c.KEY_WORD_ID
                            select c);

            foreach (T_KEYWORD keyword in keywords)
            {
                B_KEYWORD keywordData = new B_KEYWORD();

                keywordData.KEY_WORD_ID = keyword.KEY_WORD_ID;
                keywordData.KEY_WORD_NAME = keyword.KEY_WORD_NAME;
                keywordData.ENTRY_IN_DATA_FILE = keyword.ENTRY_IN_DATA_FILE;

                var keywordRels = (from c in marsEntities.T_DIC_RELATION_KEYWORD
                                   where c.KEY_WORD_ID == keyword.KEY_WORD_ID
                                   orderby c.KEY_WORD_ID
                                   select c);
                keywordRelList = new List<Int64>();
                foreach (T_DIC_RELATION_KEYWORD keywordRel in keywordRels)
                {
                    long typeId = (long)keywordRel.TYPE_ID;
                    keywordRelList.Add(typeId);
                }
                keywordData.T_DIC_RELATION_KEYWORD_RELATION_ID = keywordRelList;
                keywordList.Add(keywordData);
            }
            return keywordList;
        }


        public long GetKeywordId(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("T_KEYWORD_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }

        //public Dictionary<long,string> GetKeyWordNames()
        //{
        //    MarsEntities marsEntities =BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
        //    Dictionary<long, string> keywordList = new Dictionary<long, string>();

        //    var keywords = (from c in marsEntities.T_KEYWORD
        //                    orderby c.KEY_WORD_ID
        //                    select c).Distinct();

        //    foreach (T_KEYWORD keyword in keywords)
        //    {
        //        keywordList.Add(keyword.KEY_WORD_ID, keyword.KEY_WORD_NAME);
        //    }
        //    return keywordList;
        //}
        public Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> GetAllKeywordsFromCache()
        {
            Logger.logBegin("GetAllKeywordsFromCache");
            if (MarsDBGlobe_Cache.IsCached(MarsDBGlobe_Cache.CACHED_KEY_KEYWORDS))
            {
                Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> keywordList = new Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>();
                IEnumerable<KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>> enumKeyWords = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_KEYWORDS)
                    .GetCachedObjctByNameAs<IEnumerable<KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>>>();
                Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> keywordDic = enumKeyWords.ToDictionary(p => p.Key, p => p.Value);
                var keywords = (from c in keywordDic.Keys
                                select c).Distinct().OrderBy(p => p.KEY_WORD_NAME);
                foreach (T_KEYWORDDTO objKeyword in keywords)
                {
                    keywordList.Add(objKeyword, keywordDic[objKeyword]);
                }
                return keywordList;
            }
            else
                return null;
        }

        public Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> GetKeyWordNames(List<string> keywordNames)
        {
            List<string> keywordNamesLower = keywordNames.ConvertAll(d => d.ToLower());

            if (MarsDBGlobe_Cache.IsCached(MarsDBGlobe_Cache.CACHED_KEY_KEYWORDS))
            {
                Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> keywordList = new Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>();
                IEnumerable<KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>> enumKeyWords = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_KEYWORDS)
                    .GetCachedObjctByNameAs<IEnumerable<KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>>>(keywordNames);
                Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> keywordDic = enumKeyWords.ToDictionary(p => p.Key, p => p.Value);
                var keywords = (from c in keywordDic.Keys
                                    // where keywordNames.Contains(c.KEY_WORD_NAME)
                                where keywordNamesLower.Contains(c.KEY_WORD_NAME.ToLower())
                                select c).Distinct().OrderBy(p => p.KEY_WORD_NAME);
                foreach (T_KEYWORDDTO objKeyword in keywords)
                {
                    keywordList.Add(objKeyword, keywordDic[objKeyword]);
                }
                return keywordList;
            }
            else
                return null;

#region old codes
            //MarsEntities marsEntities =BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            //Dictionary<long, string> keywordList = new Dictionary<long, string>();

            //var keywords = (from c in marsEntities.T_KEYWORD
            //                where keywordNames.Contains(c.KEY_WORD_NAME)
            //                orderby c.KEY_WORD_ID
            //                select c).Distinct();

            //foreach (T_KEYWORD keyword in keywords)
            //{
            //    keywordList.Add(keyword.KEY_WORD_ID, keyword.KEY_WORD_NAME);
            //}
            //return keywordList;
#endregion // old codes

        }
#if !_forWebClient
        public Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> LoadAllKeywords(string strDBIdx )//= MarsEntitiesExtends.cnst_default_dbName)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            var keywordsList = from k in marsEntities.T_KEYWORD
                               from k_dic in marsEntities.T_DIC_RELATION_KEYWORD
                               where k.KEY_WORD_ID == k_dic.KEY_WORD_ID
                               select new
                               {
                                   keyword_obj = k,
                                   rel_Dic = k_dic
                               };
            /// T_KEYWORDAssembler,T_DIC_RELATION_KEYWORDAssembler
            Dictionary<T_KEYWORD, List<T_DIC_RELATION_KEYWORD>> lst = keywordsList
                .GroupBy(x => x.keyword_obj, x => x.rel_Dic)
                .ToDictionary(x => x.Key,
                              x => x.ToList()
                              );
            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> lstDtos = new Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>();
            foreach (T_KEYWORD objKey in lst.Keys)
            {
                lstDtos.Add(T_KEYWORDAssembler.ToDTO(objKey), T_DIC_RELATION_KEYWORDAssembler.ToDTOs(lst[objKey]));

            }
            lstDtos.OrderBy(p => p.Key.KEY_WORD_NAME);
            return lstDtos;
        }
#endif
        public Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> GetKeyWordNamesByObjectTypeId(List<long> objectTypeIds)
        {
            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> keywordDic = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_KEYWORDS)
                    .GetCachedObjctByNameAs<Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>>(); ;

            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> keywordList = new Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>();
            foreach (T_KEYWORDDTO objKeywordDto in keywordDic.Keys)
            {
                if (keywordDic[objKeywordDto] == null) continue;
                if (keywordDic[objKeywordDto].Exists(p => objectTypeIds.Contains(p.TYPE_ID ?? -1) || p.TYPE_ID == _staticTypeId))
                {
                    if (!(keywordList.Keys.Any(p => p.KEY_WORD_ID == objKeywordDto.KEY_WORD_ID)))
                        keywordList.Add(objKeywordDto, keywordDic[objKeywordDto]);
                }
            }
#region old Code

            //MarsEntities marsEntities =BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            //Dictionary<long, string> keywordList = new Dictionary<long, string>();
            //var keywordRels = (from c in marsEntities.T_DIC_RELATION_KEYWORD
            //                   where (objectTypeIds.Contains((long)c.TYPE_ID) || c.TYPE_ID == _staticTypeId)
            //                   orderby c.KEY_WORD_ID
            //                   select c);
            //var keywordRelList = new List<long>();
            //foreach (T_DIC_RELATION_KEYWORD keywordRel in keywordRels)
            //{
            //    long keyWordId = (long)keywordRel.KEY_WORD_ID;
            //    if(!keywordRelList.Contains(keyWordId))
            //        keywordRelList.Add(keyWordId);
            //}

            //var keywords = (from c in marsEntities.T_KEYWORD
            //                where keywordRelList.Contains((long)c.KEY_WORD_ID)
            //                orderby c.KEY_WORD_NAME
            //                select c);

            //foreach (T_KEYWORD keyword in keywords)
            //{
            //    keywordList.Add(keyword.KEY_WORD_ID, keyword.KEY_WORD_NAME);
            //}
#endregion old code
            return keywordList;
        }
        private static Dictionary<long?, List<long?>> CachedKeywordAndItsOpTypeList = null;
        public List<long> GetTypeId(long keyword, string strDBIdx)// = MarsEntitiesExtends.cnst_default_dbName)
        {
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if (CachedKeywordAndItsOpTypeList == null)
                {
                    var keyinfo = from k in marsEntities.T_DIC_RELATION_KEYWORD
                                  orderby k.KEY_WORD_ID
                                  select k;
                    CachedKeywordAndItsOpTypeList = keyinfo.GroupBy(p => p.KEY_WORD_ID, p => p.TYPE_ID).ToDictionary(p => p.Key, p => p.ToList());
                }
                if (CachedKeywordAndItsOpTypeList.Keys.Contains(keyword))
                {
                    List<long?> lT = CachedKeywordAndItsOpTypeList[keyword];
                    if (lT != null)
                    {
                        return (lT.Where(p => p.HasValue).Select(p => p.Value).ToList());
                    }
                    ///如果是null，可能是新修改，需要从数据库中再次查询
                    /// 
                }

                ///没有key的，以及没有type的    
                /// 
                var keywordRels = from key in marsEntities.T_DIC_RELATION_KEYWORD
                                  where key.KEY_WORD_ID == keyword
                                  orderby key.KEY_WORD_ID
                                  select key;
                if (keywordRels != null)
                {
                    List<long?> lRslt = keywordRels.Select(p => p.TYPE_ID).ToList();
                    if (lRslt != null)
                    {
                        CachedKeywordAndItsOpTypeList.Add(keyword, lRslt);
                        return lRslt.Where(p => p.HasValue).Select(p => p.Value).ToList();
                    }
                    else
                        CachedKeywordAndItsOpTypeList.Add(keyword, null);

                }
                return new List<long>();
                //List<long> lTypeId = new List<long>();
                //var keywordRels = (from c in marsEntities.T_DIC_RELATION_KEYWORD
                //                   where c.KEY_WORD_ID == keyword
                //                   orderby c.KEY_WORD_ID
                //                   select c);

                //foreach (T_DIC_RELATION_KEYWORD keywordRel in keywordRels)
                //{
                //    long typeId = (long)keywordRel.TYPE_ID;
                //    lTypeId.Add(typeId);
                //}
                //return lTypeId;
            }
            catch (Exception e)
            {
                Logger.Error("GetTypeId", string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace));
                return new List<long>();
            }
            finally
            {
                //Logger.logEnd();
            }
        }

        private static List<T_KEYWORDDTO> KeywordsCached = null;
#if !_forWebClient
        private void InitKeywordsCache(MarsEntities dbCntx, ref bool isOk, ref string strError,
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
#else
        private void InitKeywordsCache(ref bool isOk, ref string strError, 
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            try
            {
#if !_forWebClient
                dbCntx = dbCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var q = from k in dbCntx.T_KEYWORD
                        select k;
                (KeywordsCached = q.ToDTOs()).OrderBy(p => p.KEY_WORD_NAME);
                isOk = true;
#else
                KeywordsCached = null;
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                Logger.Info("InitKeywordsCache", $"CachedKeywords==null?null:{CachedKeywords.Count}");
                if (CachedKeywords == null)
                {
                    CachedKeywords = (new MarsRESTfulApiClient(strDBIdx)).LoadAllKeywords(ref isOk, ref strError);
                    if (!isOk)
                    {
                        return;
                    }
                }
                isOk = false;
                if (CachedKeywords.Keys == null)
                {
                    strError = "no data returned from CachedKeywords";
                    return;
                }
                isOk = true;
                KeywordsCached = CachedKeywords.Keys.OrderBy(p => p.KEY_WORD_NAME).ToList();
#endif
            }
            catch (Exception e)
            {
                Logger.Error("InitKeywordsCache", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace));
                KeywordsCached = null;
                isOk = false;
            }
        }
        public T_KEYWORDDTO GetKeywordByName(string strDBIdx,string strKeyword, MarsEntities dbCntx, ref bool isOk, ref string strError, bool isForce2Refresh = false)
        {
            Logger.Info("GetKeywordByName", string.Format("begin, try to find keyword:[{0}]", strKeyword));
            if ((KeywordsCached == null) || (isForce2Refresh))
            {
#if !_forWebClient
                InitKeywordsCache(dbCntx, ref isOk, ref strError, strDBIdx);
#else
                InitKeywordsCache(ref isOk, ref strError,strDBIdx);
#endif
                if (!isOk)
                {
                    Logger.Error("GetKeywordByName", string.Format("Error when call InitKeywordsCache\r\n{0}", strError));
                    return null;
                }
            }
            T_KEYWORDDTO objTargetKeyword = KeywordsCached.Where(p => string.Compare(p.KEY_WORD_NAME, strKeyword, true) == 0).FirstOrDefault();
            if (objTargetKeyword == null)
            {
                Logger.Error("GetKeywordByName", strError = string.Format("No such Keyword:[{0}]", strKeyword));
                isOk = false;
                return null;
            }
            isOk = true;
            return objTargetKeyword;
        }
#endif
        public static B_KEYWORD ConverfromDto(T_KEYWORDDTO objDto)
        {
            if (objDto == null) return null;
            return new B_KEYWORD()
            {
                DESCRIPTION = objDto.DESCRIPTION,
                ENTRY_IN_DATA_FILE = objDto.ENTRY_IN_DATA_FILE,
                KEY_WORD_ID = objDto.KEY_WORD_ID,
                KEY_WORD_NAME = objDto.KEY_WORD_NAME,
                KEY_WORD_POSITION_ID = objDto.KEY_WORD_POSITION_ID,
                T_DIC_RELATION_KEYWORD_RELATION_ID = objDto.T_DIC_RELATION_KEYWORD_RELATION_ID,
                T_TEST_STEPS_STEPS_ID = objDto.T_TEST_STEPS_STEPS_ID
            };
        }
#if !_noEntities
        public static string GetKeywordName(long lKeywordId, ref bool isOk, ref string strError, string strDBIdx ) //= MarsEntitiesExtends.cnst_default_dbName)
        {

            if (KeywordsCached == null)
            {
#if !_forWebClient
                (new B_KEYWORD()).InitKeywordsCache(null, ref isOk, ref strError, strDBIdx);
#else
                (new B_KEYWORD()).InitKeywordsCache(ref isOk, ref strError, strDBIdx);
#endif
            }
            if (KeywordsCached == null)
            {
                isOk = false;
                strError = "Can't load keyword information to cache";
                return null;
            }
            var k = KeywordsCached.Where(p => p.KEY_WORD_ID == lKeywordId).FirstOrDefault();
            if (k == null)
            {
                isOk = false;
                strError = string.Format("no such keyword -[{0}]", lKeywordId);
                return null;
            }
            isOk = true;
            return k.KEY_WORD_NAME;
        }
#endif
    }

}
