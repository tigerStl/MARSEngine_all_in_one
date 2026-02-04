using Mars.message.Business;
using Mars.message.Dto;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mars.message.DataLayer
{
    public class MarsDBGlobe_Cache
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsDBGlobe_Cache));
        internal const string CACHED_KEY_KEYWORDS = "GLOBAL_CACHE_KEYWORD";
        internal const string CACHED_KEY_APP_TESTCASE = "GLOBAL_CACHE_APP_TESTCASE";
        internal const string CACHED_KEY_APP_OBJECTS = "GLOBAL_CACHE_APP_OBJECTS";
        internal const string CACHED_KEY_OBJ_SNAPSHOT = "GLOBAL_CACHE_OBJECT_SNAPSHOTS";

        internal const string CACHED_KEY_SYSTEM_LOOKUP = "SYSTEM_LOOK_UP";

        public static string default_cache_app = null;
        public static string currentDBIDX      = ""  ;

        private static Dictionary<string, Mars_CachedObjects_Base> globalCacheObjects = new Dictionary<string, Mars_CachedObjects_Base>();

        internal static bool IsCached(string strCachedKey)
        {
            //Logger.Info("IsCached",string.Format("To check Cached status:[{0}]", strCachedKey));
            return globalCacheObjects.ContainsKey(strCachedKey);
        }

        //private static bool GetCachedDataByKey<T>(string strCachedKey, ref T objResult)
        //{
        //    //Logger.Info("GetCachedDataByKey", string.Format("Try to get cached data:[{0}] as Type :[{1}]", strCachedKey, typeof(T)));
        //    Mars_CachedObjects_Base objCached = globalCacheObjects[strCachedKey];
        //    if (objCached == null) return false;

        //    objResult = objCached.GetCachedObjctAs<T>();
        //    return objResult == null;
        //}

        public static Mars_CachedObjects_Base GetCacheObjectsByKey(string strCacheKey)
        {
            if (globalCacheObjects == null) return null;
            if (!globalCacheObjects.ContainsKey(strCacheKey)) return null;
            return globalCacheObjects[strCacheKey];
        }

        public static void InitCache(string strDBIdx)
        {
            Logger.logBegin("InitCache");
            currentDBIDX = strDBIdx;
            globalCacheObjects.Add(CACHED_KEY_KEYWORDS, InitKeywordGlobalCache(strDBIdx));
            globalCacheObjects.Add(CACHED_KEY_APP_TESTCASE, InitApp_TestcaseGlobalCache(strDBIdx));
            if (!string.IsNullOrEmpty(default_cache_app))
            {
                globalCacheObjects.Add(CACHED_KEY_APP_OBJECTS, InitApp_ObjectGlobalCache(default_cache_app));
            }
            globalCacheObjects.Add(CACHED_KEY_SYSTEM_LOOKUP, InitSystemLookUp_Cache(strDBIdx));
            Logger.logEnd("InitCache");
        }

        public static void UpdateAppTestCaseCache()
        {
            MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_TESTCASE).RefreshCache(MarsDBGlobe_Cache.currentDBIDX);
        }
        public static void UpdateObjectsCache()
        {
            MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS).RefreshCache(MarsDBGlobe_Cache.currentDBIDX);
        }

        private static Mars_CachedObjects_Base InitApp_TestcaseGlobalCache(string strDBIdx)
        {
            Logger.Info("InitKeywordGlobalCache", "Begin");
            Mars_Cahed_ApplicationWithTestCase objKeyCache = new Mars_Cahed_ApplicationWithTestCase(CACHED_KEY_APP_TESTCASE);
            objKeyCache.InitCache(strDBIdx);
            objKeyCache.IsInitialized = true;
            return objKeyCache;
        }

        private static Mars_CachedObjects_Base InitKeywordGlobalCache(string strDBIdx)
        {
            Logger.logBegin("InitKeywordGlobalCache", $"{strDBIdx}");
            try
            {
                Mars_Cached_KeyWords objKeyCache = new Mars_Cached_KeyWords(CACHED_KEY_KEYWORDS);
                objKeyCache.InitCache(strDBIdx);
                objKeyCache.IsInitialized = true;
                return objKeyCache;
            }
            finally
            {
                Logger.logEnd("InitKeywordGlobalCache");
            }
            
        }

        private static Mars_CachedObjects_Base InitSystemLookUp_Cache(string strDBIdx)
        {
            Logger.Info("InitSystemLookUp_Cache", "Begin");
            try
            {
                Mars_CachedObjects_Base objLookUpCache = new Mars_Cached_Lookup(CACHED_KEY_SYSTEM_LOOKUP);
                objLookUpCache.InitCache(strDBIdx);
                objLookUpCache.IsInitialized = true;
                return objLookUpCache;
            }
            catch (Exception e)
            {
                Logger.Error("InitSystemLookUp_Cache", string.Format("Exception:{0}, stackTrack:[{1}]", e.Message, e.StackTrace), e);
                return null;
            }
        }

        private static Mars_CachedObjects_Base InitApp_ObjectGlobalCache(string appShortName = "")
        {
            string strAppShortNameToCache = "";
            Logger.Info("InitApp_ObjectGlobalCache", string.Format("ShortAppName:[{0}]", strAppShortNameToCache = string.IsNullOrWhiteSpace(appShortName) ? default_cache_app : appShortName));
            Mars_Cached_Apps_Objects objCachedAppObjs = new Mars_Cached_Apps_Objects(CACHED_KEY_APP_OBJECTS, appShortName);
            objCachedAppObjs.InitCache(MarsDBGlobe_Cache.currentDBIDX);
            objCachedAppObjs.IsInitialized = true;
            return objCachedAppObjs;
        }

        public static bool updateApplicationCache(string strDBIdx, ref string strError)
        {
            return B_REGISTERED_APPS.InitCacheApplications(strDBIdx,ref strError);
            //return objApplicationCache.RefreshCache();
        }

    }

    public abstract class Mars_CachedObjects_Base
    {

        protected string cachedKey;
        public string CachedKey
        {
            get { return cachedKey; }
            set { cachedKey = value; }
        }

        public bool IsInitialized = false;

        protected object cachedObject; //list or Hash
        public object CachedObject
        {
            get { return cachedObject; }
            set { cachedObject = value; }
        }
        public Mars_CachedObjects_Base(string strCachedKey)
        {
            cachedKey = strCachedKey;
        }

        public virtual bool checkCachedObjectType<T>()
        {
            return cachedObject == null ? false : cachedObject.GetType() is T;
        }

        public abstract T GetCachedObjctAs<T>(string strDBIdx, List<long> objectKeys = null);
        public abstract T GetCachedObjctByNameAs<T>(List<string> objectKeys = null);
        public abstract bool DoseMainKeyContainsKey(long lKey);
        public abstract object GetObjectByChildId(long lChildId);

        public Type GetObjectType()
        {
            if (this.cachedObject == null) return null;
            return cachedObject.GetType();
        }

        public virtual Dictionary<T1, List<T2>> GetCachedObjectAs<T1, T2>(List<long> objectKeys = null)
        {
            return null;
        }

        public abstract bool InitCache(string strDBIdx);
        public abstract bool RefreshCache(string strDBIdx);


        public virtual void IncreaseAccessingCount(object objIdentifier = null)
        {
            return;
        }

        public virtual List<T> GetCachedObjectByObject<T>(Func<T, bool> where)
        {
            return null;
        }

    }

    internal class Mars_Cached_Lookup : Mars_CachedObjects_Base
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(Mars_Cached_Lookup));

        public Mars_Cached_Lookup(string strKey) : base(strKey)
        {

        }

        public override bool DoseMainKeyContainsKey(long lKey)
        {
            List<B_SYSTEM_LOOKUP> objLst = (List<B_SYSTEM_LOOKUP>)this.cachedObject;
            if (objLst == null) return false;
            return objLst.Any(p => p.LOOKUP_ID == lKey);
        }

        public override T GetCachedObjctAs<T>(string strDBIdx, List<long> objectKeys = null)
        {
            if (objectKeys == null)
                return (T)this.cachedObject;
            List<B_SYSTEM_LOOKUP> objLst = (List<B_SYSTEM_LOOKUP>)this.cachedObject;
            if (typeof(T) != typeof(IEnumerable<B_SYSTEM_LOOKUP>))
            {
                Logger.Error("GetCachedObjctAs", string.Format("Only support type:[{0}], but the type is:[{1}]", typeof(IEnumerable<B_SYSTEM_LOOKUP>), typeof(T)));
                return default(T);
            }
            var query = from rslt in objLst
                        where objectKeys.Contains(rslt.LOOKUP_ID)
                        select rslt;
            return (T)query;
        }

        public override T GetCachedObjctByNameAs<T>(List<string> objectKeys = null)
        {
            if (typeof(T) != typeof(B_SYSTEM_LOOKUP))
            {
                Logger.Error("GetCachedObjctByNameAs", string.Format("Only type [{0}] is supported, but it is:[{1}]", typeof(B_SYSTEM_LOOKUP), typeof(T)));
                return default(T);
            }
            if (typeof(T) != typeof(IEnumerable<B_SYSTEM_LOOKUP>))
            {
                Logger.Error("GetCachedObjctAs", string.Format("Only support type:[{0}], but the type is:[{1}]", typeof(IEnumerable<B_SYSTEM_LOOKUP>), typeof(T)));
                return default(T);
            }
            List<B_SYSTEM_LOOKUP> objLst = (List<B_SYSTEM_LOOKUP>)this.cachedObject;
            var query = from rslt in objLst
                        where objectKeys.Contains(rslt.DISPLAY_NAME)
                        select rslt;
            return (T)query;
        }

        public override object GetObjectByChildId(long lChildId)
        {
            //throw new NotImplementedException();
            List<B_SYSTEM_LOOKUP> objLst = (List<B_SYSTEM_LOOKUP>)this.cachedObject;
            B_SYSTEM_LOOKUP objItm = objLst.FirstOrDefault(p => p.LOOKUP_ID == lChildId);

            return objItm;
        }

        public override bool InitCache(string strDBIdx)
        {
            B_SYSTEM_LOOKUP objLookUp = new B_SYSTEM_LOOKUP();
            this.cachedObject = objLookUp.GetSystemLookup(strDBIdx);
            return true;
        }

        public override bool RefreshCache(string strDBIdx)
        {
            return InitCache(strDBIdx);
        }
    }

    internal class Mars_Cached_KeyWords : Mars_CachedObjects_Base
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(Mars_Cached_KeyWords));
        public Mars_Cached_KeyWords(string strKey) : base(strKey)
        {
        }

        public override T GetCachedObjctByNameAs<T>(List<string> objectKeys = null)
        {


            if (objectKeys == null || objectKeys.Count == 0)
                return (T)cachedObject;
            List<string> objectKeysLower = objectKeys.ConvertAll(d => d.ToLower());
            if (typeof(T) != typeof(IEnumerable<KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>>))
            {
                Logger.Error("GetCachedObjedAs", string.Format("Can't support type:[{0}]", typeof(T)));
                return default(T);
            }
            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> objResult = (Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>)this.cachedObject;
            var tmpQuery = (from c in objResult
                            where objectKeysLower.Contains(c.Key.KEY_WORD_NAME.ToLower())
                            select c);
            return (T)tmpQuery;
        }

        public override T GetCachedObjctAs<T>(string strDBIdx, List<long> objectKeys = null)
        {
            if (objectKeys == null || objectKeys.Count == 0)
                return (T)cachedObject;
            //Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>        
            if (typeof(T) != typeof(IEnumerable<KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>>))
            {
                Logger.Error("GetCachedObjedAs", string.Format("Can't support type:[{0}]", typeof(T)));
                return default(T);
            }
            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> objResult = (Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>)this.cachedObject;
            var tmpQuery = from c in objResult
                           where objectKeys.Contains(c.Key.KEY_WORD_ID)
                           select c;
            return (T)tmpQuery;

        }
        private static bool IsErrorCacheShew = false;
        public override bool InitCache(string strDBIdx)
        {
            /// init keywords cache
            /// 
            //try
            //{

#if !_forWebClient
            B_KEYWORD objKeywrd = new B_KEYWORD();
            this.cachedObject = objKeywrd.LoadAllKeywords(strDBIdx);
#else
            bool isOk = false;
            string strError = "";
            this.cachedObject = (new MarsRESTfulApiClient(strDBIdx)).LoadAllKeywords(ref isOk, ref strError);
            if (!isOk)
            {
                return false;
            }
#endif
            return true;
            //}
            //catch (Exception e)
            //{
            //    this.cachedObject = null;
            //    string strError = "";
            //    Logger.Error("InitCache", strError = string.Format("Exceptions:[{0}] \r\nstackTrace:[{1}]",e.Message,e.StackTrace),e);
            //    if (!IsErrorCacheShew)
            //    {
            //        IsErrorCacheShew = true;
            //        System.Windows.Forms.MessageBox.Show(strError, "ERROR", System.Windows.Forms.MessageBoxButtons.OK);
            //    }
            //    return false;
            //}

        }

        public override bool RefreshCache(string strDBIdx)
        {
            Logger.Info("RefreshCache", "Begin to recall InitCache");
            return InitCache(strDBIdx);
        }

        public override bool DoseMainKeyContainsKey(long lKey)
        {
            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> objRslt = (Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>)this.cachedObject;
            if (objRslt == null) return false;
            return objRslt.Keys.Any(p => p.KEY_WORD_ID == lKey);
        }

        public override object GetObjectByChildId(long lChildId)
        {
            //Doesn't support
            return null;
        }
    }

    internal class Mars_Cahed_ApplicationWithTestCase : Mars_CachedObjects_Base
    {
        protected static MLogger Logger = MLogger.GetLogger(typeof(Mars_Cahed_ApplicationWithTestCase));
        public Mars_Cahed_ApplicationWithTestCase(string strKey) : base(strKey)
        {
            Logger.Info("Mars_Cahed_ApplicationWithTestCase", string.Format("Create cache for :[{0}]", strKey));
        }

        public override T GetCachedObjctByNameAs<T>(List<string> objectKeys = null)
        {
            return default(T);
        }

        public override T GetCachedObjctAs<T>(string strDBIdx, List<long> objectKeys = null)
        {
            return (T)cachedObject;
        }

        public override Dictionary<T1, List<T2>> GetCachedObjectAs<T1, T2>(List<long> objectKeys = null)
        {
            return (Dictionary<T1, List<T2>>)cachedObject;
        }

        public override bool InitCache(string strDBIdx)
        {
            try
            {
                B_REL_APP_TESTCASE appTCObj = new B_REL_APP_TESTCASE();
                cachedObject = appTCObj.LoadAllAppsWithTestCase(strDBIdx);
                return true;
            }
            catch (Exception e)
            {
                this.cachedObject = null;
                Logger.Error("InitCache", string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        public override bool RefreshCache(string strDBIdx)
        {
            return InitCache(strDBIdx);
        }

        public override bool DoseMainKeyContainsKey(long lKey)
        {
            Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>> objTmp = (Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>>)this.cachedObject;
            if (objTmp == null) return false;
            return objTmp.Keys.Any(p => p.APPLICATION_ID == lKey);
        }

        public override object GetObjectByChildId(long lChildId)
        {
            Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>> objTmp = (Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>>)this.cachedObject;
            if (objTmp == null) return null;
            foreach (T_REGISTERED_APPSDTO appItm in objTmp.Keys)
            {
                if (objTmp[appItm].Any(p => p.TEST_CASE_ID == lChildId))
                    return objTmp[appItm].FirstOrDefault(p => p.TEST_CASE_ID == lChildId);
            }
            return null;
        }
    }

    internal class Mars_Cached_Apps_Objects : Mars_CachedObjects_Base
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(Mars_Cached_Apps_Objects));
        private int maxCacheSlot = 5;
        private int iCurrentCachedSlot = 0;
        protected string[] CachedAppShortNames = null;
        //#if v_16AndUp
        //        Dictionary<MarsDynamicCacheDetailInfo, B_REGISTERED_APPS> CachedAppMangerInfo = new Dictionary<MarsDynamicCacheDetailInfo, B_REGISTERED_APPS>();
        //#else
        Dictionary<MarsDynamicCacheDetailInfo, T_REGISTERED_APPSDTO> CachedAppMangerInfo = new Dictionary<MarsDynamicCacheDetailInfo, T_REGISTERED_APPSDTO>();
        //#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strKey"></param>
        /// <param name="strAppShorts">sepearated by ; if more than one applications are added</param>
        public Mars_Cached_Apps_Objects(string strKey, string strAppShorts) : base(strKey)
        {
            CachedAppShortNames = string.IsNullOrWhiteSpace(strAppShorts) ? null : strAppShorts.Split(';');
        }



        public override T GetCachedObjctAs<T>(string strDBIdx, List<long> objectKeys = null)
        {
            Logger.logBegin("GetCachedObjctAs");
            if (objectKeys == null)
                return (T)this.cachedObject;
#if v_16AndUp
            if (typeof(T) != typeof(IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>>))
#else
            if (typeof(T) != typeof(IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>>))
#endif
            {
                Logger.Error("GetCachedObjedAs", string.Format("Unsupported type cast for cached objects:[{0}]", typeof(T)));
                return default(T);
            }
#if v_16AndUp
            Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> dicResult = (Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>)this.cachedObject;
#else
            Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> dicResult = (Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>)this.cachedObject;
#endif
            //dicResult.Keys.Any(appid => objectKeys.Contains(appid.APPLICATION_ID))
            IEnumerable<long> notExist = from appInfo in objectKeys
                                         where !objectKeys.All(p => dicResult.Keys.Any(x => x.APPLICATION_ID == p))
                                         select appInfo;
            List<long> lstNeedAttach = new List<long>();
            foreach (long itm in objectKeys)
            {
                if (dicResult.Keys.Any(p => p.APPLICATION_ID == itm))
                {
                    continue;
                }
                lstNeedAttach.Add(itm);
            }
            //List <long> lstNeedAttach = new List<long>(notExist);
            if (lstNeedAttach.Count > 0)
            {
                /// refresh cached object
                /// steps:
                /// 1, get objects to be delete
                /// 2, reget data from database
                /// 3, add it to database
                /// 4, return result 
                /// 
                /// 1, get objects to be delete
                /// 
                //#if v_16AndUp
                //                IOrderedEnumerable<KeyValuePair<MarsDynamicCacheDetailInfo, B_REGISTERED_APPS>> lstEnum = CachedAppMangerInfo.OrderByDescending(p => p.Key, new MarsOrderCompareByAccessCount());
                //#else
                IOrderedEnumerable<KeyValuePair<MarsDynamicCacheDetailInfo, T_REGISTERED_APPSDTO>> lstEnum = CachedAppMangerInfo.OrderByDescending(p => p.Key, new MarsOrderCompareByAccessCount());
                //#endif

                int iDelSlot = iCurrentCachedSlot + lstNeedAttach.Count - maxCacheSlot;
                var lsttmp = lstEnum.ToList();
                if (iDelSlot > 0)
                {
                    int iDeleted = 0;
                    while ((iDeleted < iDelSlot) && (lsttmp.Count > 0) && (dicResult.Keys.Count > 0))
                    {
                        var oneItem = lsttmp.ElementAt(0);
                        dicResult.Remove(oneItem.Value);
                        CachedAppMangerInfo.Remove(oneItem.Key);
                        lsttmp.RemoveAt(0);
                        iDeleted += 1;
                    }

                }
                /// load data from database based on lstNeedAttach
                /// 
                AddApp_Objects2CacheByAppIds(strDBIdx, lstNeedAttach);
            }
#if v_16AndUp
            var resultQuery = from rslt in (Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>)this.cachedObject
                              where objectKeys.Contains(rslt.Key.APPLICATION_ID)
                              select rslt;
#else
            var resultQuery = from rslt in (Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>)this.cachedObject
                              where objectKeys.Contains(rslt.Key.APPLICATION_ID)
                              select rslt;
#endif
            Logger.logEnd("GetCachedObjctAs");
            return (T)(resultQuery);
        }

        private void AddApp_Objects2CacheByAppIds(string strDBIdx, List<long> lstAppIds)
        {
            Logger.Info("AddApp_Objects2CacheByAppIds", string.Format("AppIds:[{0}]", lstAppIds));
            B_REGISTED_OBJECT objDB = new B_REGISTED_OBJECT();
#if v_16AndUp
            Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> objAppObjects = objDB.GetObjectByAppIds(strDBIdx,lstAppIds);
            foreach (T_REGISTERED_APPSDTO objApps in objAppObjects.Keys)
            {
                ((Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>)this.cachedObject).Add(objApps, objAppObjects[objApps]);
                CachedAppMangerInfo.Add(new MarsDynamicCacheDetailInfo(), objApps);
            }
#else
            Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> objAppObjects = objDB.GetObjectByAppIds(lstAppIds);
            foreach (T_REGISTERED_APPSDTO objApps in objAppObjects.Keys) {
                ((Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>)this.cachedObject).Add(objApps, objAppObjects[objApps]);
                CachedAppMangerInfo.Add(new MarsDynamicCacheDetailInfo(), objApps);
            }
#endif
            this.iCurrentCachedSlot = objAppObjects.Keys.ToList().Count;
            if (this.maxCacheSlot < this.iCurrentCachedSlot)
                this.maxCacheSlot = this.iCurrentCachedSlot;
            Logger.logEnd("AddApp_Objects2CacheByAppIds");
        }

        class MarsOrderCompareByAccessCount : IComparer<MarsDynamicCacheDetailInfo>
        {
            public int Compare(MarsDynamicCacheDetailInfo x, MarsDynamicCacheDetailInfo y)
            {
                int x1 = x == null ? -1 : x.AccessedCount;
                int y1 = y == null ? -1 : y.AccessedCount;
                return x1 - y1;
            }
        }

        //private List<T_REGISTERED_APPSDTO> CalculateAccessTime()
        //{

        //}

        public override bool InitCache(string strDBIdx)
        {
            B_REGISTED_OBJECT objDB = new B_REGISTED_OBJECT();
#if v_16AndUp
            Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> objAppObjects = objDB.GetObjectByAppNames(this.CachedAppShortNames,strDBIdx);
#else
            Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> objAppObjects = objDB.GetObjectByAppNames(this.CachedAppShortNames);
#endif
            cachedObject = objAppObjects;
            iCurrentCachedSlot = objAppObjects.Keys.Count;
            return true;
        }

        public override bool RefreshCache(string strDBIdx)
        {
            return InitCache(strDBIdx);
        }



        public override void IncreaseAccessingCount(object objIdentifier = null)
        {

        }

        public override List<T> GetCachedObjectByObject<T>(Func<T, bool> where)
        {
#if v_16AndUp
            if (typeof(T) != typeof(B_REGISTED_OBJECT))
            {
                return null;
            }

#else
            if (typeof(T) !=typeof(T_REGISTED_OBJECTDTO))
            {
                return null;
            }
#endif
            return null;
        }

        public override T GetCachedObjctByNameAs<T>(List<string> objectKeys = null)
        {
            return default(T);
        }

        public override bool DoseMainKeyContainsKey(long lKey)
        {
#if v_16AndUp
            return ((Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>)this.CachedObject).Any(p => p.Key.APPLICATION_ID == lKey);
#else
            return ((Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>)this.CachedObject).Any(p=>p.Key.APPLICATION_ID==lKey);
#endif
        }


        public override object GetObjectByChildId(long lChildId)
        {
#if v_16AndUp
            Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> objTmp = (Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>)this.CachedObject;
#else
            Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> objTmp = (Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>)this.CachedObject;
#endif
            foreach (T_REGISTERED_APPSDTO objItm in objTmp.Keys)
            {
                if (objTmp[objItm].Any(p => p.OBJECT_ID == lChildId))
                {
                    return objTmp[objItm].FirstOrDefault(p => p.OBJECT_ID == lChildId);
                }
            }
            return null;
        }
    }

    internal class MarsDynamicCacheDetailInfo
    {
        private DateTime addedTime = DateTime.Now;
        public DateTime AddedTime
        {
            get { return addedTime; }
            set { addedTime = value; }
        }

        private int accessedCount = 0;
        public int AccessedCount
        {
            get { return accessedCount; }
            //set { accessedCount=value ; }
        }

        public void IncreaseAccesscount()
        {
            accessedCount++;
        }
    }
}
