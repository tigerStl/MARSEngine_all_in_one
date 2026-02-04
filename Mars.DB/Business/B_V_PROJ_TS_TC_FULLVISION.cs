using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mars.message.Business
{
    public class B_V_PROJ_TS_TC_FULLVISION : V_PROJ_TS_TC_FULLVISIONDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_V_PROJ_TS_TC_FULLVISION));

        public static List<V_PROJ_TS_TC_FULLVISIONDTO> GetProjectById(string strDBIdx, long lProjId)
        {
            Logger.logBegin("V_PROJ_TS_TC_FULLVISIONDTO", string.Format("project id:[{0}]", lProjId));
            MarsEntities objCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var q = (from p in objCntx.V_PROJ_TS_TC_FULLVISION
                         where p.PROJECT_ID == lProjId
                         select p);
                List<V_PROJ_TS_TC_FULLVISIONDTO> lstResult = q.ToDTOs().ToList();
                return lstResult;
            }
            catch (Exception e)
            {
                Logger.Error("GetProjectById", string.Format("Exception:[{0}],stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return null;
            }
        }

        public static List<V_PROJ_TS_TC_FULLVISIONDTO> GetAllTestProjInfo(
            string strDBIdx ,//= MarsEntitiesExtends.cnst_default_dbName
            ref string strError, ref bool isOk, long luserId = -1
            )
        {
            Logger.Info("GetAllTestProjInfo", string.Format("begin, with userId=[{0}/-1 means all]", luserId));
            MarsEntities objCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            try
            {
                List<V_PROJ_TS_TC_FULLVISIONDTO> lstResult;
                var q = (from p in objCntx.V_PROJ_TS_TC_FULLVISION
                         select p);

                Logger.Info("GetAllTestProjInfo", string.Format("Query for V_PROJ_TS_TC_FULLVISION:[{0}]", q.ToString()));
                isOk = true;
                if ((q == null) || (q.ToList() == null) || (q.ToList().Count == 0))
                    return new List<V_PROJ_TS_TC_FULLVISIONDTO>();
                lstResult = q.OrderBy(z => z.PROJECT_NAME).ToDTOs().ToList();

                return lstResult;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetAllTestProjInfo", strError = string.Format("Exception:[{0}],stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return null;
            }
        }

        public static List<B_TEST_SUITE> FilterByProjIdAndConver2SimpleTestSuite(long lPorj, List<V_PROJ_TS_TC_FULLVISIONDTO> lstSource)
        {
            Logger.Info("FilterByProjIdAndConver2TestSuite", string.Format("begin, Porject it to filter:[{0}] sourceCount:[{1}]", lPorj, lstSource == null ? -1 : lstSource.Count));
            var q = (from z in lstSource
                     where z.PROJECT_ID == lPorj
                     select z).OrderBy(p => p.TEST_SUITE_NAME);

            if (q == null) return null;
            List<B_TEST_SUITE> lstRsult = new List<B_TEST_SUITE>();
            foreach (var itm in q)
            {
                if (itm == null) continue;
                if (itm.TEST_SUITE_ID == -1) continue;
                B_TEST_SUITE objRslt = new B_TEST_SUITE();
                objRslt.APPLICATION_ID = -1;
                objRslt.APP_SHORT_NAME = null;
                objRslt.TEST_SUITE_DESCRIPTION = itm.TEST_SUITE_DESCRIPTION;
                objRslt.TEST_SUITE_ID = itm.TEST_SUITE_ID;
                objRslt.TEST_SUITE_NAME = itm.TEST_SUITE_NAME;
                objRslt.VERSION = null;

                lstRsult.Add(objRslt);
            }
            lstRsult = lstRsult.GroupBy(p => p.TEST_SUITE_ID).Select(g => g.First()).ToList();
            return lstRsult;
        }

        public static List<B_TEST_CASE> FilterByProjTSIdAndConvert2SimpleTestCase(long lPorj, long lTSId, List<V_PROJ_TS_TC_FULLVISIONDTO> lstSource)
        {
            //Logger.Info("FilterByProjTSIdAndConvert2SimpleTestCase", string.Format("begin, Porject it to filter:[{0}],testSuiteId:[{2}] sourceCount:[{1}]", lPorj, lstSource == null ? -1 : lstSource.Count, lTSId));
            var q = (from z in lstSource
                     where z.PROJECT_ID == lPorj
                     && z.TEST_SUITE_ID == lTSId
                     select z).OrderBy(p => p.TEST_CASE_NAME);
            if (q == null) return null;
            List<B_TEST_CASE> lstTestCase = new List<B_TEST_CASE>();
            foreach (var itm in q)
            {
                if (itm.TEST_CASE_ID == -1) continue;
                B_TEST_CASE objTc = new B_TEST_CASE();
                objTc.APPLICATION_ID = -1;
                objTc.APP_SHORT_NAME = null;
                objTc.TEST_CASE_ID = itm.TEST_CASE_ID;
                objTc.TEST_CASE_NAME = itm.TEST_CASE_NAME;
                objTc.TEST_STEP_DESCRIPTION = itm.TEST_STEP_DESCRIPTION;

                lstTestCase.Add(objTc);
            }
            lstTestCase = lstTestCase.GroupBy(p => p.TEST_CASE_ID).Select(g => g.First()).ToList();
            return lstTestCase;
        }

        public static List<B_LINKED_DATA_SHEET> FilterByProjTSIdTCIdAndConvert2SimpleDataSheet(long lPorj, long lTSId, long lTcId, List<V_PROJ_TS_TC_FULLVISIONDTO> lstSource)
        {
            //Logger.Info("FilterByProjTSIdAndConvert2SimpleTestCase", string.Format("begin, Porject it to filter:[{0}],testSuiteId:[{2}],test case Id:[{3}], sourceCount:[{1}]", lPorj, lstSource == null ? -1 : lstSource.Count, lTSId, lTcId));
            var q = (from z in lstSource
                     where z.PROJECT_ID == lPorj
                     && z.TEST_SUITE_ID == lTSId
                     && z.TEST_CASE_ID == lTcId
                     select z).OrderBy(p => p.TEST_CASE_NAME);
            if (q == null) return null;
            List<B_LINKED_DATA_SHEET> lstData = new List<B_LINKED_DATA_SHEET>();
            foreach (var itm in q)
            {
                B_LINKED_DATA_SHEET objLstDataSht = new B_LINKED_DATA_SHEET();
                objLstDataSht.DataItemDescription = itm.DATASET_DESCRIPTION;
                objLstDataSht.DataItemName = itm.DATA_ALIAS;
                objLstDataSht.Id = itm.DATA_SUMMARY_ID;
                objLstDataSht.IsSelected = true;

                lstData.Add(objLstDataSht);
            }
            lstData = lstData.GroupBy(p => p.Id).Select(g => g.First()).OrderBy(o => o.DataItemName).ToList();
            return lstData;
        }
    }
}
