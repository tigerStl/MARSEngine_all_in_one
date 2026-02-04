using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;

#if !_pythonInterface
using Route2NSEx.src.Marquis.systemUtil;
#endif

using System;
using System.Collections.Generic;
using System.Linq;

#if _marsLog
using Logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.Business
{
    public class B_V_OBJECT_SNAPSHOT : V_OBJECT_SNAPSHOTDTO
    {
#if !_marsLog
        private static MLogger Logger = MLogger.GetLogger(typeof(B_V_OBJECT_SNAPSHOT));
#endif
        public B_V_OBJECT_SNAPSHOT ShallowCopy()
        {
            return (B_V_OBJECT_SNAPSHOT)this.MemberwiseClone();
        }
#if !_noEntities
        internal List<B_V_OBJECT_SNAPSHOT> FetchObjsByAppIds(List<long?> lstAppIds, MarsEntities dbCntx, ref bool isOk, ref string strError,
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("FetchObjsByAppIds");
            try
            {
                MarsEntities dbCurrnt = dbCntx == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx) : dbCntx;
                var obj = from o in dbCurrnt.V_OBJECT_SNAPSHOT
                          where lstAppIds.Contains(o.APPLICATION_ID)
                          select o;
                isOk = true;
                List<B_V_OBJECT_SNAPSHOT> lstObj = new List<B_V_OBJECT_SNAPSHOT>();
                if (obj != null)
                {
                    foreach (var itm in obj)
                    {
                        lstObj.Add(ConvertFromDTO(itm));
                    }
                    return lstObj;
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                Logger.Error("FetchObjsByAppIds", strError = string.Format("Exception:[{0}],stackTrace:\r\n", e.Message, e.StackTrace), e);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("FetchObjsByAppIds");
            }
        }

        public V_OBJECT_SNAPSHOTDTO GetCurrentPegInfoByAppIdAndPegName(string strDBIdx, string strPegwindowName, long lAppId, ref bool isOk, ref string strError, MarsEntities objDBCntx = null)
        {
            Logger.logBegin("GetCurrentPegInfoByAppIdAndPegName", string.Format("PegName:[{0}] AppId:[{1}]", strPegwindowName, lAppId));
            try
            {
                MarsEntities objcntx = objDBCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
                var peg = (from q in objcntx.V_OBJECT_SNAPSHOT
                           where q.PEG_NAME == strPegwindowName
                           && q.APPLICATION_ID == lAppId
                           && q.OBJECT_HAPPY_NAME == strPegwindowName
                           select q).FirstOrDefault();

                if (peg == null)
                {
                    strError = string.Format("No such Peg Object exists [{0}] for application Id:[{1}]", strPegwindowName, lAppId);
                    Logger.Error("GetCurrentPegInfoByAppIdAndPegName", strError);
                    return null;
                }
                isOk = true;
                return peg.ToDTO();
            }
            catch (Exception e)
            {
                Logger.Error("GetCurrentPegInfoByAppIdAndPegName", strError = string.Format("Exception:[{0}]", e.Message), e);
                isOk = false;
                return null;
            }
        }

        public V_OBJECT_SNAPSHOTDTO getDefaultErrorObjectForPegByAppId(string strPegwindowName, long lAppId, ref bool isOk, ref string strError, short errorCheckMark = 1, MarsEntities objDBCntx = null)
        {
            Logger.logBegin("getDefaultErrorObjectForPegByAppId", string.Format("Pegwindow name:[{0}], appId:[{1}]", strPegwindowName, lAppId));
            try
            {
                MarsEntities dbCntx = objDBCntx ?? new MarsEntities();
                var o = (from q in dbCntx.V_OBJECT_SNAPSHOT
                         where q.PEG_NAME == strPegwindowName
                         && q.APPLICATION_ID == lAppId
                         && q.IS_CHECKERROR_OBJ == errorCheckMark
                         select q)
                        .FirstOrDefault();
                if (o == null)
                {
                    isOk = false;
                    Logger.Info("getDefaultErrorObjectForPegByAppId", string.Format("no such error check object for peg:[{0}] app:[{1}], null is returned", strPegwindowName, lAppId));
                    return null;
                }
                isOk = true;
                return o.ToDTO();
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("getDefaultErrorObjectForPegByAppId", strError = string.Format("Exception :[{0}]", e.Message), e);
                return null;
            }


        }

        public long refreshAllCachedObjects(string strDBIdx, ref string strMsg)
        {
            List<long> appIds = CachedObjectInfoByDictionary.Keys.ToList();
            long cntObj = 0;
            bool isOk = false;
            string strError = "";
            string strTmpMsg = "";

            appIds.ForEach(p => {
                CachedObjectInfoByDictionary.Remove(p);
                var lst = GetObjectInfoByAppId(p, ref isOk, ref strError, strDBIdx);
                int icnt = lst == null ? 0 : lst.Count;
                strTmpMsg += ((string.IsNullOrEmpty(strTmpMsg) ? "" : "\r\n") + $"updated appid:{p} and {icnt} objects reloaded");
                cntObj += lst.Count;

            });
            strMsg = strTmpMsg;
            return cntObj;
        }

        private static Dictionary<long, List<B_V_OBJECT_SNAPSHOT>> CachedObjectInfoByDictionary = new Dictionary<long, List<B_V_OBJECT_SNAPSHOT>>();
#if !_forWebSvc
        public static List<B_V_OBJECT_SNAPSHOT> GetObjectInfoByAppId(long lAppId, ref bool isOk, ref string strError, 
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
#else
        public List<B_V_OBJECT_SNAPSHOT> GetObjectInfoByAppId(long lAppId, ref bool isOk, ref string strError,
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            Logger.logBegin("GetObjectInfoByAppId", string.Format("Application:[{0}]", lAppId));
            try
            {
                if (!CachedObjectInfoByDictionary.ContainsKey(lAppId))
                {
                    //get data from database
                    B_V_OBJECT_SNAPSHOT objSnp = new B_V_OBJECT_SNAPSHOT();
                    List<B_V_OBJECT_SNAPSHOT> lstObj = objSnp.FetchObjsByAppIds(new List<long?>() { lAppId }, null, ref isOk, ref strError,
                        strDBIdx);
                    if (!isOk) return null;
                    CachedObjectInfoByDictionary.Add(lAppId, lstObj);
                }
                isOk = true;
                return CachedObjectInfoByDictionary[lAppId];
            }
            finally
            {
                Logger.logEnd("GetObjectInfoByAppId");
            }
        }
#endif


#if _forWebSvc
        public List<B_V_OBJECT_SNAPSHOT> GetObjectInfoByAppIdAndObjIds(string dbIdx, string strAppId, string[] objIds, ref bool isOk, ref string strError, ref string strStack, ref string strAdv)
        {
            Logger.logBegin("GetObjectInfoByAppIdAndObjIds");
            try
            {
                int iAppId = -1;
                if (!int.TryParse(strAppId, out iAppId))
                {
                    strStack = Environment.StackTrace;
                    strAdv = "Contact Marquis";
                    isOk = false;
                    strError = $"Application Id [{strAppId}] is not an Int";
                    return null;
                }
                List<long?> lstIds = new List<long?>();
                foreach(var itm in objIds)
                {
                    if (string.IsNullOrEmpty(itm)) continue;
                    long tmp;
                    if (long.TryParse(itm,out tmp))
                    {
                        lstIds.Add(tmp);
                    }
                }

                MarsEntities objcntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB: dbIdx);
                var o = from p in objcntx.V_OBJECT_SNAPSHOT
                            //where lstIds.Contains(p.OBJECT_ID)
                        where lstIds.Contains(p.OBJECT_NAME_ID)
                        && p.APPLICATION_ID == iAppId                        
                        select p;

                List<B_V_OBJECT_SNAPSHOT> result = new List<B_V_OBJECT_SNAPSHOT>();
                foreach (var itm in o.ToList())
                {
                    if (itm == null) continue;
                    var pegInfo = B_V_OBJECT_SNAPSHOT.ConvertFromDTO(itm);
                    if (pegInfo == null) continue;
                    result.Add(pegInfo);
                }
                isOk = true;
                return result;
            }
            catch(Exception e)
            {
                Logger.Error("GetObjectInfoByAppIdAndObjIds", strError = e.Message, strStack = e.StackTrace);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("GetObjectInfoByAppIdAndObjIds");
            }
        }

        public List<B_V_OBJECT_SNAPSHOT> GetObjectsOfPeg(string appShortName, string pegName, string dbIdx, ref bool isOk, ref string strError, ref string strStack)
        {
            Logger.logBegin("GetObjectsOfPeg", $"appshortName:[{appShortName}] pegName:[{pegName}] dbIdx:[{dbIdx}]");
            try
            {
                MarsEntities objcntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB: dbIdx);
                var o = from p in objcntx.V_OBJECT_SNAPSHOT
                        from a in objcntx.T_REGISTERED_APPS
                        where p.OBJECT_TYPE.Equals(pegName, StringComparison.OrdinalIgnoreCase)
                        && a.APPLICATION_ID == p.APPLICATION_ID
                        && a.APP_SHORT_NAME.Equals(appShortName, StringComparison.OrdinalIgnoreCase)
                        select p;
                List<B_V_OBJECT_SNAPSHOT> result = new List<B_V_OBJECT_SNAPSHOT>();
                foreach (var itm in o.ToList())
                {
                    if (itm == null) continue;
                    var pegInfo = B_V_OBJECT_SNAPSHOT.ConvertFromDTO(itm);
                    if (pegInfo == null) continue;
                    result.Add(pegInfo);
                }
                isOk = true;
                return result;
            }
            catch (Exception e)
            {
                Logger.Error("GetObjectsOfPeg", strError = e.Message, strStack = e.StackTrace);
                isOk = false;
                return null;
            }
            finally {
                Logger.logEnd("GetObjectsOfPeg");
            }
            
        }
#endif

#if !_forWebSvc
        public static List<B_V_OBJECT_SNAPSHOT> GetPegWindowByAppShortNameDB(string strAppShortName, string strDBIdx,
            ref bool isOk, ref string strError, ref string strStack)
#else
        public List<B_V_OBJECT_SNAPSHOT> GetPegWindowByAppShortNameDB(string strAppShortName, string strDBIdx,
            ref bool isOk, ref string strError, ref string strStack)
#endif
        {
            Logger.logBegin("GetPegWindowByAppShortNameDB", $"shortName:{strAppShortName}, dbIdx:[{strDBIdx}]");
            try
            {
#if !_forWebSvc
                isOk = false;
                return null ;
#else
                MarsEntities objcntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
                var o = from obj in objcntx.V_OBJECT_SNAPSHOT
                        from app in objcntx.T_REGISTERED_APPS
                        where strAppShortName.ToUpper() == app.APP_SHORT_NAME.ToUpper()
                         && app.APPLICATION_ID == obj.APPLICATION_ID
                         && obj.PEG_NAME == obj.OBJECT_HAPPY_NAME
                        select obj;
         
                List<B_V_OBJECT_SNAPSHOT> result = new List<B_V_OBJECT_SNAPSHOT>();
                foreach (var itm in o.ToList())
                {
                    if (itm == null) continue;
                    var pegInfo = B_V_OBJECT_SNAPSHOT.ConvertFromDTO(itm);
                    if (pegInfo == null) continue;
                    result.Add(pegInfo);
                }
                isOk = true;
                return result;
#endif
            }
            catch (Exception e)
            {
                Logger.Error("updateStatusData", $"{strError = e.Message}\r\n{e.StackTrace}");
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("GetPegWindowByAppShortNameDB");
            }
        }


        private static B_V_OBJECT_SNAPSHOT ConvertFromDTO(V_OBJECT_SNAPSHOT objSrc)
        {
            B_V_OBJECT_SNAPSHOT objRslt = new B_V_OBJECT_SNAPSHOT();
            objRslt.APPLICATION_ID = objSrc.APPLICATION_ID;
            objRslt.COMMENT = objSrc.COMMENT;
            objRslt.ENUM_TYPE = objSrc.ENUM_TYPE;
            objRslt.OBJECT_HAPPY_NAME = objSrc.OBJECT_HAPPY_NAME;
            objRslt.OBJECT_ID = objSrc.OBJECT_ID;
            objRslt.OBJECT_NAME_ID = objSrc.OBJECT_NAME_ID;
            objRslt.OBJECT_TYPE = objSrc.OBJECT_TYPE;
            objRslt.PEG_ID = objSrc.PEG_ID;
            objRslt.PEG_NAME = objSrc.PEG_NAME;
            objRslt.PEG_QUICK_ACCESS = objSrc.PEG_QUICK_ACCESS;
            objRslt.QUICK_ACCESS = objSrc.QUICK_ACCESS;
            objRslt.TYPE_ID = objSrc.TYPE_ID;
            objRslt.TYPE_NAME = objSrc.TYPE_NAME;

            return objRslt;
        }

        


    }
}
