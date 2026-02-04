using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mars.Business
{
    public class B_V_OBJECT_SNAPSHOT : V_OBJECT_SNAPSHOTDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_V_OBJECT_SNAPSHOT));

        public B_V_OBJECT_SNAPSHOT ShallowCopy()
        {
            return (B_V_OBJECT_SNAPSHOT)this.MemberwiseClone();
        }

        internal List<B_V_OBJECT_SNAPSHOT> FetchObjsByAppIds(List<long?> lstAppIds, MarsEntities dbCntx, ref bool isOk, ref string strError,
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("FetchObjsByAppIds");
            try
            {
                MarsEntities dbCurrnt = dbCntx == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : dbCntx;
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

        public V_OBJECT_SNAPSHOTDTO GetCurrentPegInfoByAppIdAndPegName(string strDBIdx, string strPegwindowName, long lAppId, ref bool isOk, ref string strError, MarsEntities objDBCntx = null)
        {
            Logger.logBegin("GetCurrentPegInfoByAppIdAndPegName", string.Format("PegName:[{0}] AppId:[{1}]", strPegwindowName, lAppId));
            try
            {
                MarsEntities objcntx = objDBCntx ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
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

            appIds.ForEach(p=> {
                CachedObjectInfoByDictionary.Remove(p);
                var lst = GetObjectInfoByAppId(p, ref isOk, ref strError , strDBIdx);
                int icnt = lst == null ? 0 : lst.Count;
                strTmpMsg += ((string.IsNullOrEmpty(strTmpMsg)?"":"\r\n")+$"updated appid:{p} and {icnt} objects reloaded");
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


    }
}
