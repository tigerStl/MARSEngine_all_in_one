

using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Objects;
using System.Linq;

namespace Mars.message.Business
{
    public class B_REL_APP_TESTCASE : REL_APP_TESTCASEDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_REL_APP_TESTCASE));

        public long getRelTestCaseAppId(string strDBIdx, MarsEntities objDbCntx = null)
        {
            MarsEntities marsEntities = objDbCntx == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDbCntx;
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));

#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("REL_APP_TESTCASE_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());
        }

        #region deprecated method
        public List<long> GetApplicationsIds(string strDBIdx, string testCaseName)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<long> appIds = new List<long>();
            var relAppTestCase = (from c in marsEntities.REL_APP_TESTCASE
                                  join d in marsEntities.T_TEST_CASE_SUMMARY on c.TEST_CASE_ID equals d.TEST_CASE_ID
                                  where d.TEST_CASE_NAME == testCaseName
                                  orderby c.APPLICATION_ID
                                  select c);

            foreach (var bAppRel in relAppTestCase)
            {
                long appId = (long)bAppRel.APPLICATION_ID;
                if (!appIds.Contains(appId))
                    appIds.Add(appId);
            }
            return appIds;
        }
        #endregion //deprecated method




        public List<long> GetApplicationsIdsByTCId(string strDBIdx, long tcId)
        {

            List<long> appIds = new List<long>();
            Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>> objAppTcs = 
                MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_TESTCASE).GetCachedObjctAs<Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>>>(strDBIdx);

            foreach (var AppDto in objAppTcs.Keys)
            {
                if (objAppTcs[AppDto].Exists(p => p.TEST_CASE_ID == tcId))
                    if (!appIds.Contains(AppDto.APPLICATION_ID))
                        appIds.Add(AppDto.APPLICATION_ID);
            }
            return appIds;
        }

        internal object LoadAllAppsWithTestCase(string strDBIDX)
        {
            Logger.logBegin("LoadAllAppsWithTestCase");
            Logger.Info("LoadAllAppsWithTestCase", "Create GetMarsEntitiesInstance");
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIDX);
            Logger.Info("LoadAllAppsWithTestCase", "begin to query");
            var query = from rel in marsEntities.REL_APP_TESTCASE
                        join tc in marsEntities.T_TEST_CASE_SUMMARY on rel.TEST_CASE_ID equals tc.TEST_CASE_ID
                        join app in marsEntities.T_REGISTERED_APPS on rel.APPLICATION_ID equals app.APPLICATION_ID
                        orderby rel.APPLICATION_ID
                        select new
                        {
                            mars_app = app,
                            mars_tc = tc
                        };
            Dictionary<T_REGISTERED_APPS, List<T_TEST_CASE_SUMMARY>> queryGrouped = query.GroupBy(x => x.mars_app, x => x.mars_tc).ToDictionary(x => x.Key, x => x.ToList());
            Logger.Info("LoadAllAppsWithTestCase", "finished query");
            Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>> dicAppTCDtos = new Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>>();
            foreach (T_REGISTERED_APPS objApp in queryGrouped.Keys)
            {
                T_REGISTERED_APPSDTO objAppDto = T_REGISTERED_APPSAssembler.ToDTO(objApp);
                dicAppTCDtos.Add(objAppDto, T_TEST_CASE_SUMMARYAssembler.ToDTOs(queryGrouped[objApp].ToList()));
            }
            Logger.Info("LoadAllAppsWithTestCase", "finished installment");
            return dicAppTCDtos;
        }

        public bool CreateAppWithTCId(string strDBIdx, long iTestCaseId, List<long> lstAppIds, MarsEntities dbCntx, ref string strError)
        {
            Logger.logBegin("CreateAppWithTCId", string.Format("TestCaseId:[{0}], appIds:[{1}]", iTestCaseId, lstAppIds == null ? "N/A" : String.Join(",", lstAppIds)));
            try
            {
                dbCntx.Set<REL_APP_TESTCASE>();
                foreach (long lAppId in lstAppIds)
                {
                    long lRelKey = getRelTestCaseAppId(strDBIdx,dbCntx);
                    REL_APP_TESTCASE objTmp = new REL_APP_TESTCASE();
                    objTmp.RELATIONSHIP_ID = lRelKey;
                    objTmp.TEST_CASE_ID = iTestCaseId;
                    objTmp.APPLICATION_ID = lAppId;

                    dbCntx.REL_APP_TESTCASE.Add(objTmp);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateAppWithTCId", string.Format("Exception :[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("CreateAppWithTCId");
            }
        }

        public static bool CreateRelation(long tEST_CASE_ID, long aPPLICATION_ID, DbCommand dbCmmd, ref string strError)
        {
            try
            {
                dbCmmd.Parameters.Clear();
                bool isOk = false;
                long lRelId = BoHelper.GetBussinessSeq("REL_APP_TESTCASE_SEQ", dbCmmd, ref strError, ref isOk);
                if (!isOk)
                {
                    return false;
                }
                dbCmmd.Parameters.Clear();
                string strSql = string.Format("INSERT INTO REL_APP_TESTCASE(RELATIONSHIP_ID,APPLICATION_ID,TEST_CASE_ID) VALUES ({0},{1},{2})",
                    lRelId, aPPLICATION_ID, tEST_CASE_ID);
                dbCmmd.CommandText = strSql;
                int iCnt = dbCmmd.ExecuteNonQuery();
                Logger.Info("CreateRelation", string.Format("Created [{0}] recorders {1}", iCnt, lRelId));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateRelation", strError = string.Format("Excetpion:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                return false;
            }
        }
    }
}
