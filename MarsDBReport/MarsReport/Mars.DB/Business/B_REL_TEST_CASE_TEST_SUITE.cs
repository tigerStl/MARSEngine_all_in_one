
using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;


using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.Objects;
using System.Linq;
using System.Transactions;

namespace Mars.Business
{
    public class B_REL_TEST_CASE_TEST_SUITE : REL_TEST_CASE_TEST_SUITEDTO
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(B_REL_TEST_CASE_TEST_SUITE));
        internal const string cnst_TABLE_NAME_REL_TS_TC = "REL_TEST_CASE_TEST_SUITE";
        internal const string cnst_SEQ = "REL_TEST_CASE_TEST_SUITE_SEQ";

        public long getRelTestCasteTestSuite(string strDBIdx, MarsEntities objDbCntx = null)
        {
            MarsEntities marsEntities = objDbCntx == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDbCntx;
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL("REL_TEST_CASE_TEST_SUITE_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());
        }

        public bool UpdateTSTCRelations(string strDBIdx, long testSuiteId, ObservableCollection<B_TEST_CASE> lstTSInfo, ref string strError)
        {
            Logger.Info("UpdateTSTCRelations", string.Format("Test suiteId:[{0}], testcase Count:[{1}]", testSuiteId, lstTSInfo == null ? 0 : lstTSInfo.Count));
            if (lstTSInfo == null) return true;
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                using (TransactionScope trans = new TransactionScope())
                {
                    /// steps: /// using less Database accessing
                    /// 1 delete all unched test suite test cases relations
                    /// 2 insert un exsists 
                    List<long> lstToBeDeltedTestCaseIds = lstTSInfo.Where(p => !p.IsSelected).Select(p => p.TEST_CASE_ID).ToList();

                    if (lstToBeDeltedTestCaseIds.Count > 0)
                    {
                        string strSqlDel = string.Format("DELETE {0} WHERE TEST_CASE_ID  IN ({1}) AND TEST_SUITE_ID={2}", cnst_TABLE_NAME_REL_TS_TC, string.Join(",", lstToBeDeltedTestCaseIds), testSuiteId);
                        if (marsEntities.Database.Connection.State == System.Data.ConnectionState.Closed)
                            marsEntities.Database.Connection.Open();
                        using (DbCommand dbCmd = marsEntities.Database.Connection.CreateCommand())
                        {
                            dbCmd.CommandText = strSqlDel;
                            dbCmd.ExecuteNonQuery();
                        }
                    }

                    List<long> lstToBeInsertTC = lstTSInfo.Where(p => p.IsSelected).Select(p => p.TEST_CASE_ID).ToList();
                    var qNotExists = (from q in marsEntities.REL_TEST_CASE_TEST_SUITE
                                      where
                                         lstToBeInsertTC.Contains(q.TEST_CASE_ID ?? -1)
                                        && q.TEST_SUITE_ID == testSuiteId
                                      select q.TEST_CASE_ID).ToList();
                    List<long> lstTcNewIds = lstToBeInsertTC.Where(p => !qNotExists.Contains(p)).ToList();
                    if (lstTcNewIds != null)
                    {
                        foreach (var itm in lstTcNewIds)
                        {
                            REL_TEST_CASE_TEST_SUITEDTO objRelDto = new REL_TEST_CASE_TEST_SUITEDTO();
                            objRelDto.TEST_CASE_ID = itm;
                            objRelDto.TEST_SUITE_ID = testSuiteId;
                            objRelDto.RELATIONSHIP_ID = BoHelper.GetIdBySeqName(cnst_SEQ, marsEntities);
                            marsEntities.REL_TEST_CASE_TEST_SUITE.Add(objRelDto.ToEntity());
                        }
                    }
                    marsEntities.SaveChanges();
                    trans.Complete();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateTSTCRelations", strError = string.Format("Exception when update test suite [{0}] and testcases \r\nException:[{1}]", testSuiteId, e.Message), e);
                return false;
            }


        }

        public bool BuildTSTCRelationsByIds(string strDBIdx, MarsEntities crntDbCntx, long tsId, IEnumerable<long> tcIds, ref string strError)
        {
            Logger.logBegin("BuildTSTCRelationsByIds", string.Format("Test suite:[{0}] TC Ids:[{1}]", tsId, tcIds));
            try
            {
                if (tcIds == null) return true;
                if (tsId <= 0) return true;
                if (crntDbCntx == null)
                {
                    Logger.Error("BuildTSTCRelationsByIds", strError = "Database context is null");
                    return false;
                }

                crntDbCntx.Set<REL_TEST_CASE_TEST_SUITE>();
                foreach (long lTCId in tcIds)
                {
                    REL_TEST_CASE_TEST_SUITE objTSTCRel = new REL_TEST_CASE_TEST_SUITE();
                    objTSTCRel.RELATIONSHIP_ID = getRelTestCasteTestSuite(strDBIdx,crntDbCntx);
                    objTSTCRel.TEST_SUITE_ID = tsId;
                    objTSTCRel.TEST_CASE_ID = lTCId;
                    crntDbCntx.REL_TEST_CASE_TEST_SUITE.Add(objTSTCRel);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("BuildTSTCRelationsByIds", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }
    }
}
