using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;

using System.Transactions;

namespace Mars.Business
{
    public class B_TEST_SUITE : T_TEST_SUITEDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_TEST_SUITE));
        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
            }
        }

        public long APPLICATION_ID { get; set; }
        public string APP_SHORT_NAME { get; set; }
        public string VERSION { get; set; }

        #region ForMapping
        public long SRC_MAPPING_TESTSUITEID { get; set; }
        #endregion

        public List<B_TEST_SUITE> GetTestSuite(string strDBIdx)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx); ;
            List<B_TEST_SUITE> TestSuite = new List<B_TEST_SUITE>();
            var testSuite = (from c in marsEntities.T_TEST_SUITE
                             join d in marsEntities.REL_APP_TESTSUITE on c.TEST_SUITE_ID equals d.TEST_SUITE_ID
                             orderby c.TEST_SUITE_NAME
                             select new { c.TEST_SUITE_ID, c.TEST_SUITE_NAME, c.TEST_SUITE_DESCRIPTION, d.APPLICATION_ID });
            string strError = "";
            bool isOk = false;
            foreach (var regTestSuite in testSuite)
            {
                B_TEST_SUITE newTestSuite = new B_TEST_SUITE();
                newTestSuite.TEST_SUITE_ID = regTestSuite.TEST_SUITE_ID;
                newTestSuite.TEST_SUITE_NAME = regTestSuite.TEST_SUITE_NAME;
                newTestSuite.TEST_SUITE_DESCRIPTION = regTestSuite.TEST_SUITE_DESCRIPTION;
                B_REGISTERED_APPS currentApp = B_REGISTERED_APPS.GetApplicationByAppIdFromCache(strDBIdx,regTestSuite.APPLICATION_ID, ref strError, ref isOk);
                if (currentApp != null)
                {
                    newTestSuite.APP_SHORT_NAME = currentApp.APP_SHORT_NAME;// marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).APP_SHORT_NAME;
                    newTestSuite.VERSION = currentApp.VERSION;// marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).VERSION;
                }
                //newTestSuite.APP_SHORT_NAME = marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).APP_SHORT_NAME;
                //newTestSuite.VERSION = marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).VERSION;
                TestSuite.Add(newTestSuite);
            }
            return TestSuite;
        }

        public List<B_TEST_SUITE> GetTestSuiteOwneredByProj(string strDBIdx, long projectId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_TEST_SUITE> TestSuite = new List<B_TEST_SUITE>();
            var testSuite = from c in marsEntities.REL_TEST_SUIT_PROJECT
                            from ts in marsEntities.T_TEST_SUITE
                            where
                                ts.TEST_SUITE_ID == c.TEST_SUITE_ID
                            && c.PROJECT_ID == projectId
                            select ts;
            foreach (var regTestSuite in testSuite)
            {
                B_TEST_SUITE newTestSuite = new B_TEST_SUITE();
                newTestSuite.TEST_SUITE_ID = regTestSuite.TEST_SUITE_ID;
                newTestSuite.TEST_SUITE_NAME = regTestSuite.TEST_SUITE_NAME;
                newTestSuite.TEST_SUITE_DESCRIPTION = regTestSuite.TEST_SUITE_DESCRIPTION;
                TestSuite.Add(newTestSuite);
            }
            return TestSuite;
        }

        public List<B_TEST_SUITE> GetTestSuite(string strDBIdx, long projectId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_TEST_SUITE> TestSuite = new List<B_TEST_SUITE>();
            var testSuite = (from c in marsEntities.T_TEST_SUITE
                             join d in marsEntities.REL_APP_TESTSUITE on c.TEST_SUITE_ID equals d.TEST_SUITE_ID
                             where (from e in marsEntities.REL_APP_PROJ where e.PROJECT_ID == projectId select e.APPLICATION_ID).Contains(d.APPLICATION_ID)
                             orderby c.TEST_SUITE_NAME
                             select new { c.TEST_SUITE_ID, c.TEST_SUITE_NAME, c.TEST_SUITE_DESCRIPTION, d.APPLICATION_ID });


            string strError = "";
            bool isOk = false;
            List<REL_TEST_SUIT_PROJECTDTO> lstTSPrj = B_REL_TEST_SUIT_PROJECT.GetCached(strDBIdx);

            foreach (var regTestSuite in testSuite)
            {
                B_TEST_SUITE newTestSuite = new B_TEST_SUITE();
                newTestSuite.TEST_SUITE_ID = regTestSuite.TEST_SUITE_ID;
                newTestSuite.TEST_SUITE_NAME = regTestSuite.TEST_SUITE_NAME;
                newTestSuite.TEST_SUITE_DESCRIPTION = regTestSuite.TEST_SUITE_DESCRIPTION;
                B_REGISTERED_APPS currentApp = B_REGISTERED_APPS.GetApplicationByAppIdFromCache(strDBIdx,regTestSuite.APPLICATION_ID, ref strError, ref isOk);
                if (currentApp != null)
                {
                    newTestSuite.APP_SHORT_NAME = currentApp.APP_SHORT_NAME; //( cachedApps == null?"":  marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).APP_SHORT_NAME;
                    newTestSuite.VERSION = currentApp.VERSION;//marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).VERSION;
                }
                if (lstTSPrj != null)
                {
                    newTestSuite.IsSelected = lstTSPrj.FirstOrDefault(x => x.PROJECT_ID == projectId && x.TEST_SUITE_ID == regTestSuite.TEST_SUITE_ID) != null ? true : false;
                    //newTestSuite.IsSelected = (marsEntities.REL_TEST_SUIT_PROJECT.FirstOrDefault(x => x.PROJECT_ID == projectId && x.TEST_SUITE_ID == regTestSuite.TEST_SUITE_ID) != null ? true : false);
                }
                TestSuite.Add(newTestSuite);
            }
            return TestSuite;
        }

        public List<B_TEST_SUITE> GetMappedTestSuite(string strDBIdx, long projectId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_TEST_SUITE> TestSuite = new List<B_TEST_SUITE>();
            /// Changed by Tiger for filter dupilicated records
            /// 
            Logger.Info("GetMappedTestSuite", string.Format("begin with project id:[{0}]", projectId));
            var testSuite = (from c in marsEntities.T_TEST_SUITE
                             join d in marsEntities.REL_TEST_SUIT_PROJECT on c.TEST_SUITE_ID equals d.TEST_SUITE_ID
                             join e in marsEntities.T_TEST_PROJECT on d.PROJECT_ID equals e.PROJECT_ID
                             where e.PROJECT_ID == projectId
                             orderby c.TEST_SUITE_NAME
                             select c).Distinct();

            foreach (T_TEST_SUITE regTestSuite in testSuite)
            {
                B_TEST_SUITE newTestSuite = new B_TEST_SUITE();
                newTestSuite.TEST_SUITE_ID = regTestSuite.TEST_SUITE_ID;
                newTestSuite.TEST_SUITE_NAME = regTestSuite.TEST_SUITE_NAME;
                newTestSuite.TEST_SUITE_DESCRIPTION = regTestSuite.TEST_SUITE_DESCRIPTION;
                TestSuite.Add(newTestSuite);
            }
            Logger.logEnd("GetMappedTestSuite");
            return TestSuite;
        }

        public long getTestSuiteId(string strDBIdx, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx == null ? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : dbCntx;
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long relationId = (long)marsEntities.GETNEXT_VAL("T_TEST_SUITE_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());
        }

        public long getTestSuiteId(string strDBIdx, string testSuiteName)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            long testSuiteId = marsEntities.T_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_NAME == testSuiteName).TEST_SUITE_ID;
            return testSuiteId;

        }

        public bool TestSuiteExists(string strDBIdx, string testSuiteName, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx == null ? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : dbCntx;
            var testSuite = (from c in marsEntities.T_TEST_SUITE
                             where c.TEST_SUITE_NAME.ToUpper() == testSuiteName.ToUpper()
                             select c);
            if (testSuite != null && testSuite.Count() > 0)
            {
                return true;
            }
            return false;
        }

        public List<B_TEST_SUITE> GetApplicationTestSuite(string strDBIdx, long applicationId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_TEST_SUITE> TestSuite = new List<B_TEST_SUITE>();
            var allTestSuite = (from c in marsEntities.T_TEST_SUITE
                                join d in marsEntities.REL_APP_TESTSUITE on c.TEST_SUITE_ID equals d.TEST_SUITE_ID
                                where d.APPLICATION_ID == applicationId
                                group c by new
                                {
                                    TEST_SUITE_ID = c.TEST_SUITE_ID,
                                    TEST_SUITE_NAME = c.TEST_SUITE_NAME,
                                    TEST_SUITE_DESCRIPTION = c.TEST_SUITE_DESCRIPTION,
                                    APPLICATION_ID = d.APPLICATION_ID
                                } into ListTestSuite
                                orderby ListTestSuite.Key.TEST_SUITE_NAME
                                select ListTestSuite);

            string strError = "";
            bool isOk = false;
            foreach (var regTestSuite in allTestSuite)
            {
                B_TEST_SUITE newTestSuite = new B_TEST_SUITE();
                newTestSuite.TEST_SUITE_ID = regTestSuite.Key.TEST_SUITE_ID;
                newTestSuite.TEST_SUITE_NAME = regTestSuite.Key.TEST_SUITE_NAME;
                newTestSuite.TEST_SUITE_DESCRIPTION = regTestSuite.Key.TEST_SUITE_DESCRIPTION;
                B_REGISTERED_APPS currentApp = B_REGISTERED_APPS.GetApplicationByAppIdFromCache(strDBIdx, regTestSuite.Key.APPLICATION_ID, ref strError, ref isOk);
                if (currentApp != null)
                {
                    newTestSuite.APP_SHORT_NAME = currentApp.APP_SHORT_NAME;// marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).APP_SHORT_NAME;
                    newTestSuite.VERSION = currentApp.VERSION;// marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).VERSION;
                }
                //newTestSuite.APP_SHORT_NAME = marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.Key.APPLICATION_ID).APP_SHORT_NAME;
                //newTestSuite.VERSION = marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.Key.APPLICATION_ID).VERSION;
                TestSuite.Add(newTestSuite);
            }
            return TestSuite;
        }

        public bool DeleteTestSuiteById(string strDBIdx, long lTestSuiteId, ref string strError)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            string strTestSName = "";
            try
            {
                using (var scope = new TransactionScope())
                {
                    var testSuite = (from t in marsEntities.T_TEST_SUITE
                                     where t.TEST_SUITE_ID == lTestSuiteId
                                     select t).FirstOrDefault();
                    strTestSName = testSuite.TEST_SUITE_NAME;
                    var relAppTestSuite = (from a in marsEntities.REL_APP_TESTSUITE
                                           where a.TEST_SUITE_ID == testSuite.TEST_SUITE_ID
                                           select a);
                    foreach (var a in relAppTestSuite)
                    {
                        marsEntities.REL_APP_TESTSUITE.Remove(a);
                    }

                    var relProjTestSuite = (from r in marsEntities.REL_TEST_SUIT_PROJECT
                                            where r.TEST_SUITE_ID == testSuite.TEST_SUITE_ID
                                            select r);
                    foreach (var r in relProjTestSuite)
                    {
                        marsEntities.REL_TEST_SUIT_PROJECT.Remove(r);
                    }

                    var relTestCaseTestSuite = (from r in marsEntities.REL_TEST_CASE_TEST_SUITE
                                                where r.TEST_SUITE_ID == testSuite.TEST_SUITE_ID
                                                select r);
                    foreach (var r in relTestCaseTestSuite)
                    {
                        marsEntities.REL_TEST_CASE_TEST_SUITE.Remove(r);
                    }

                    marsEntities.T_TEST_SUITE.Remove(testSuite);
                    marsEntities.SaveChanges();

                    scope.Complete();
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("DeleteTestSuiteById", strError = string.Format("Exception:[{0}] When delete test suite:[1] ,StackTrace:\r\n{2}", e.Message, strTestSName, e.StackTrace));
                return false;
            }

        }

        public bool AddXmlObj(string strDBIdx, MarsEntities dbCntx, ref string strError)
        {
            Logger.logBegin("AddXmlObj");
            try
            {
                if (TestSuiteExists(strDBIdx,this.TEST_SUITE_NAME, dbCntx))
                    this.TEST_SUITE_NAME += "_imp_xml";
                this.TEST_SUITE_ID = getTestSuiteId(strDBIdx, dbCntx);
                dbCntx.Set<T_TEST_SUITE>();
                dbCntx.T_TEST_SUITE.Add(this.ToEntity());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("AddXmlObj", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("AddXmlObj");
            }
        }
    }


}
