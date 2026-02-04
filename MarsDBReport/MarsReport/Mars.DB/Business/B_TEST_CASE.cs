

using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Objects;
using System.Linq;

namespace Mars.Business
{
    public class B_TEST_CASE : T_TEST_CASE_SUMMARYDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_TEST_CASE));

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
        private List<long> ApplicationIds = new List<long>();
        public string APP_SHORT_NAME { get; set; }
        public string VERSION { get; set; }

        public void AddNewApplicationId(long lAppId)
        {
            if (ApplicationIds.Exists(p => p == lAppId)) return;
            ApplicationIds.Add(lAppId);
        }

        public List<B_TEST_CASE> GetTestCase(string strDBIdx)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx); ;
            List<B_TEST_CASE> TestCase = new List<B_TEST_CASE>();
            var testCase = (from c in marsEntities.T_TEST_CASE_SUMMARY
                            orderby c.TEST_CASE_NAME
                            select c);

            foreach (T_TEST_CASE_SUMMARY regTestCase in testCase)
            {
                B_TEST_CASE newTestCase = new B_TEST_CASE();
                newTestCase.TEST_CASE_ID = regTestCase.TEST_CASE_ID;
                newTestCase.TEST_CASE_NAME = regTestCase.TEST_CASE_NAME;
                TestCase.Add(newTestCase);
            }
            return TestCase;
        }

        public List<B_TEST_CASE> GetTestCasesBelong2Ts(string strDBIdx, long lTSId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx); ;
            List<B_TEST_CASE> TestCase = new List<B_TEST_CASE>();
            var testcase = from tc in marsEntities.T_TEST_CASE_SUMMARY
                           from ts in marsEntities.REL_TEST_CASE_TEST_SUITE
                           where ts.TEST_CASE_ID == tc.TEST_CASE_ID
                           && ts.TEST_SUITE_ID == lTSId
                           select tc;
            B_TEST_CASE newTestCase = null;
            foreach (var tmp in testcase)
            {
                T_TEST_CASE_SUMMARY regTestCase = tmp;
                newTestCase = TestCase.Where(p => p.TEST_CASE_ID == regTestCase.TEST_CASE_ID).FirstOrDefault();
                if (newTestCase == null)
                {
                    newTestCase = new B_TEST_CASE();
                    newTestCase.TEST_CASE_ID = regTestCase.TEST_CASE_ID;
                    newTestCase.TEST_CASE_NAME = regTestCase.TEST_CASE_NAME;
                    //newTestCase.IsSelected = (marsEntities.REL_TEST_CASE_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_ID == testSuiteId && x.TEST_CASE_ID == regTestCase.TEST_CASE_ID) != null ? true : false);
                    TestCase.Add(newTestCase);
                }
            }
            return TestCase;
        }

        public static T_TEST_CASE_SUMMARYDTO GetTestCaseInfoByName(string strDBIdx, string testCaseName, ref bool isOk, ref string strError)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var t = marsEntities.T_TEST_CASE_SUMMARY.Where(p => string.Compare(p.TEST_CASE_NAME, testCaseName) == 0).FirstOrDefault();
                isOk = true;
                if (t == null) return null;

                return t.ToDTO();
            }
            catch (Exception e)
            {
                Logger.Error("GetTestCaseInfoByName", strError = string.Format("Exception:[{0}]", e.Message), e);
                isOk = false;
                return null;
            }
        }

        public List<B_TEST_CASE> GetTestCase(string strDBIdx, long testSuiteId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx); ;
            List<B_TEST_CASE> TestCase = new List<B_TEST_CASE>();

            var testCase = (from c in marsEntities.T_TEST_CASE_SUMMARY
                            join d in marsEntities.REL_APP_TESTCASE on c.TEST_CASE_ID equals d.TEST_CASE_ID
                            where (from e in marsEntities.REL_APP_TESTSUITE where e.TEST_SUITE_ID == testSuiteId select e.APPLICATION_ID).Contains(d.APPLICATION_ID)
                            join tstc in marsEntities.REL_TEST_CASE_TEST_SUITE.Where(x => x.TEST_SUITE_ID == testSuiteId) on c.TEST_CASE_ID equals tstc.TEST_CASE_ID into tstc_c
                            from tstv2 in tstc_c.DefaultIfEmpty()
                                //where tstc.TEST_SUITE_ID == testSuiteId
                            orderby c.TEST_CASE_NAME
                            select new
                            {
                                testcaseSummary = c,
                                isSelected = tstv2.TEST_SUITE_ID == null ? false : true
                            });

            B_TEST_CASE newTestCase = null;
            foreach (var tmp in testCase)
            {
                T_TEST_CASE_SUMMARY regTestCase = tmp.testcaseSummary;
                newTestCase = TestCase.Where(p => p.TEST_CASE_ID == regTestCase.TEST_CASE_ID).FirstOrDefault();
                if (newTestCase == null)
                {
                    newTestCase = new B_TEST_CASE();
                    newTestCase.TEST_CASE_ID = regTestCase.TEST_CASE_ID;
                    newTestCase.TEST_CASE_NAME = regTestCase.TEST_CASE_NAME;
                    newTestCase.IsSelected = tmp.isSelected;
                    //newTestCase.IsSelected = (marsEntities.REL_TEST_CASE_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_ID == testSuiteId && x.TEST_CASE_ID == regTestCase.TEST_CASE_ID) != null ? true : false);
                    TestCase.Add(newTestCase);
                }
            }
            return TestCase;
        }

        public static T_TEST_CASE_SUMMARYDTO GetTestCaseInfoByName(long testCaseId)
        {
            Mars_CachedObjects_Base objCached = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_TESTCASE);
            if (objCached == null) return null;
            object o = objCached.GetObjectByChildId(testCaseId);
            if (o == null) return null;
            if (o is T_TEST_CASE_SUMMARYDTO) return (T_TEST_CASE_SUMMARYDTO)o;
            return null;
        }


        public List<B_TEST_CASE> GetMappedTestCase(string strDBIdx, long testSuiteId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx); ;
            List<B_TEST_CASE> TestCase = new List<B_TEST_CASE>();
            var testCase = (from c in marsEntities.T_TEST_CASE_SUMMARY
                            join d in marsEntities.REL_TEST_CASE_TEST_SUITE on c.TEST_CASE_ID equals d.TEST_CASE_ID
                            where d.TEST_SUITE_ID == testSuiteId
                            orderby c.TEST_CASE_NAME
                            select c);

            foreach (T_TEST_CASE_SUMMARY regTestCase in testCase)
            {
                B_TEST_CASE newTestCase = new B_TEST_CASE();
                newTestCase.TEST_CASE_ID = regTestCase.TEST_CASE_ID;
                newTestCase.TEST_CASE_NAME = regTestCase.TEST_CASE_NAME;
                newTestCase.IsSelected = (marsEntities.REL_TEST_CASE_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_ID == testSuiteId && x.TEST_CASE_ID == regTestCase.TEST_CASE_ID) != null ? true : false);
                TestCase.Add(newTestCase);
            }
            return TestCase;
        }

        private static Dictionary<long, List<long?>> TestCaseID_AppIdListCache = new Dictionary<long, List<long?>>();
        public static List<long?> GetAssignedApplications(string strDBIdx, long testCaseId, MarsEntities objDbCntx = null)
        {
            Logger.logBegin("GetAssignedApplications", string.Format("Test caseId [{0}]", testCaseId));
            try
            {
                /// get information from cache first
                /// 
                if (!TestCaseID_AppIdListCache.ContainsKey(testCaseId))
                {
                    MarsEntities marsEntities = objDbCntx ?? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
                    var appList = from tc in marsEntities.REL_APP_TESTCASE
                                  where tc.TEST_CASE_ID == testCaseId
                                  select tc.APPLICATION_ID;
                    List<long?> lstApps = appList.ToList();
                    if (lstApps == null) return new List<long?>();
                    TestCaseID_AppIdListCache.Add(testCaseId, lstApps);
                }
                if (!TestCaseID_AppIdListCache.ContainsKey(testCaseId)) return new List<long?>();
                return TestCaseID_AppIdListCache[testCaseId];
            }catch(Exception e)
            {
                Logger.Error("GetAssignedApplications", e.Message, e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetAssignedApplications");
            }
        }

        public static T_TEST_CASE_SUMMARYDTO CreateNewTestCase(string testCaseName, string testcaseDesc, DbCommand dbCmmd, ref string strError, ref bool isOk)
        {
            string strsql = @"INSERT INTO T_TEST_CASE_SUMMARY (TEST_CASE_ID, TEST_CASE_NAME, TEST_STEP_DESCRIPTION) 
                                VALUES(:TEST_CASE_ID, :TEST_CASE_NAME, :TEST_STEP_DESCRIPTION)";
            try
            {

                dbCmmd.Parameters.Clear();

                T_TEST_CASE_SUMMARYDTO rslt = new T_TEST_CASE_SUMMARYDTO();

                DbParameter paraTEST_CASE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraTEST_CASE_ID.ParameterName = "TEST_CASE_ID";
                paraTEST_CASE_ID.Value = rslt.TEST_CASE_ID = BoHelper.GetBussinessSeq(T_TEST_CASE_SUMMARY_SEQ, dbCmmd, ref strError, ref isOk);
                if (!isOk) return null;

                DbParameter paraTEST_CASE_NAME = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraTEST_CASE_NAME.ParameterName = "TEST_CASE_NAME";
                paraTEST_CASE_NAME.Value = rslt.TEST_CASE_NAME = testCaseName;
                DbParameter paraTEST_STEP_DESCRIPTION = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraTEST_STEP_DESCRIPTION.ParameterName = "TEST_STEP_DESCRIPTION";
                paraTEST_STEP_DESCRIPTION.Value = rslt.TEST_STEP_DESCRIPTION = testcaseDesc;

                dbCmmd.CommandText = strsql;
                dbCmmd.Parameters.Clear();
                dbCmmd.Parameters.Add(paraTEST_CASE_ID);
                dbCmmd.Parameters.Add(paraTEST_CASE_NAME);
                dbCmmd.Parameters.Add(paraTEST_STEP_DESCRIPTION);

                int iCnt = dbCmmd.ExecuteNonQuery();
                Logger.Info("CreateNewTestCase", string.Format("Created test case with number:[{0}]", iCnt));
                isOk = true;
                return rslt;
            }
            catch (Exception e)
            {
                Logger.Error("CreateNewTestCase", strError = string.Format("Exception:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace), e);
                isOk = false;
                return null;
            }
        }

        public B_TEST_CASE GetTestCase(string strDBIdx, string testCaseName, MarsEntities objDbCntx = null)
        {
            MarsEntities marsEntities = objDbCntx ?? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            B_TEST_CASE TestCase = new B_TEST_CASE();
            var testCase = (from c in marsEntities.T_TEST_CASE_SUMMARY
                            join d in marsEntities.REL_APP_TESTCASE on c.TEST_CASE_ID equals d.TEST_CASE_ID
                            where c.TEST_CASE_NAME == testCaseName
                            orderby c.TEST_CASE_NAME
                            select c).FirstOrDefault();

            if (testCase == null) return null;
            TestCase.TEST_CASE_ID = testCase.TEST_CASE_ID;
            TestCase.TEST_CASE_NAME = testCase.TEST_CASE_NAME;
            TestCase.TEST_STEP_DESCRIPTION = testCase.TEST_STEP_DESCRIPTION;
            TestCase.TEST_STEP_CREATE_TIME = testCase.TEST_STEP_CREATE_TIME;

            return TestCase;
        }

        public long MergeTestCaseByNameAndDesc(
            string strDBIdx,
            string testCaseName,
            string testcaseDesc,
            ref string strNewTestName,
            ref string strError,
            MarsEntities objDBCntx,
            ref bool isExistsTestCase,
            bool isOverride = false)
        {
            Logger.logBegin("MergeTestCaseByNameAndDesc");
            try
            {

                var t = from o in objDBCntx.T_TEST_CASE_SUMMARY
                        where o.TEST_CASE_NAME == testCaseName
                        && o.TEST_STEP_DESCRIPTION == testcaseDesc
                        select o;
                T_TEST_CASE_SUMMARY oTargetTest = null;
                isExistsTestCase = (oTargetTest = t.FirstOrDefault()) != null;
                strNewTestName = isOverride ? testCaseName : (isExistsTestCase ? string.Format("{0}_imp_{1}", testCaseName, DateTime.Now.ToString("MMddyyyy")) : testCaseName);
                if ((isExistsTestCase) && (isOverride))
                {
                    // 
                    return oTargetTest.TEST_CASE_ID;
                }
                T_TEST_CASE_SUMMARYDTO objNew = new T_TEST_CASE_SUMMARYDTO();
                objNew.TEST_CASE_ID = getTestCaseId(strDBIdx,objDBCntx);
                objNew.TEST_CASE_NAME = strNewTestName;
                objNew.TEST_STEP_CREATE_TIME = DateTime.Now;
                objNew.TEST_STEP_CREATOR = "IMPORT";
                objNew.TEST_STEP_DESCRIPTION = testcaseDesc;
                objNew.USAGE_STATUS = 1;

                objDBCntx.Set<T_TEST_CASE_SUMMARY>();
                objDBCntx.T_TEST_CASE_SUMMARY.Add(objNew.ToEntity());

                return objNew.TEST_CASE_ID;
            }
            catch (Exception e)
            {
                Logger.Error("MergeTestCaseByNameAndDesc", strError = string.Format("Exception:[{0}] stackTrace:{1}", e.Message, e.StackTrace), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("MergeTestCaseByNameAndDesc");
            }

        }

        public T_TEST_CASE_SUMMARYDTO GetTestCaseById(string strDBIdx, long testcaseId, MarsEntities dbCntxt = null)
        {
            MarsEntities marsEntities = dbCntxt == null ? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : dbCntxt;
            var q = (from t in marsEntities.T_TEST_CASE_SUMMARY
                     where t.TEST_CASE_ID == testcaseId
                     select t).FirstOrDefault();
            if (q == null) return null;
            return T_TEST_CASE_SUMMARYAssembler.ToDTO(q);
        }

        public List<B_TEST_CASE> GetMappedTestCase(string strDBIdx, string testSuiteName)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_TEST_CASE> TestCase = new List<B_TEST_CASE>();
            var testCase = (from c in marsEntities.T_TEST_CASE_SUMMARY
                            join d in marsEntities.REL_TEST_CASE_TEST_SUITE on c.TEST_CASE_ID equals d.TEST_CASE_ID
                            join e in marsEntities.T_TEST_SUITE on d.TEST_SUITE_ID equals e.TEST_SUITE_ID
                            where e.TEST_SUITE_NAME == testSuiteName
                            orderby c.TEST_CASE_NAME
                            select c);

            foreach (T_TEST_CASE_SUMMARY regTestCase in testCase)
            {
                B_TEST_CASE newTestCase = new B_TEST_CASE();
                newTestCase.TEST_CASE_ID = regTestCase.TEST_CASE_ID;
                newTestCase.TEST_CASE_NAME = regTestCase.TEST_CASE_NAME;
                TestCase.Add(newTestCase);
            }
            return TestCase;
        }
        internal const string T_TEST_CASE_SUMMARY_SEQ = "T_TEST_CASE_SUMMARY_SEQ";
        public long getTestCaseId(string strDBIdx, MarsEntities dbCntxt = null)
        {
            MarsEntities marsEntities = dbCntxt == null ? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : dbCntxt;
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL(T_TEST_CASE_SUMMARY_SEQ, outparam);
            return long.Parse(outparam.Value.ToString());
        }



        public bool TestCaseExists(string strDBIdx, string testCaseName, MarsEntities dbCntx = null)
        {
            MarsEntities marsEntities = dbCntx ?? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var testCase = (from c in marsEntities.T_TEST_CASE_SUMMARY
                            where c.TEST_CASE_NAME.ToUpper() == testCaseName.ToUpper()
                            select c);
            if (testCase != null && testCase.Count() > 0)
            {
                return true;
            }
            return false;
        }

        public List<B_TEST_CASE> GetApplicationTestCase(string strDBIdx, long applicationId)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_TEST_CASE> TestCase = new List<B_TEST_CASE>();
            var allTestCase = (from c in marsEntities.T_TEST_CASE_SUMMARY
                               join d in marsEntities.REL_APP_TESTCASE on c.TEST_CASE_ID equals d.TEST_CASE_ID
                               where d.APPLICATION_ID == applicationId
                               group c by new
                               {
                                   TEST_CASE_ID = c.TEST_CASE_ID,
                                   TEST_CASE_NAME = c.TEST_CASE_NAME,
                                   TEST_STEP_DESCRIPTION = c.TEST_STEP_DESCRIPTION,
                                   APPLICATION_ID = d.APPLICATION_ID
                               } into ListTestCase
                               orderby ListTestCase.Key.TEST_CASE_NAME
                               select ListTestCase);

            string strError = "";
            bool isOk = false;
            foreach (var regTestCase in allTestCase)
            {
                B_TEST_CASE newTestCase = new B_TEST_CASE();
                newTestCase.TEST_CASE_ID = regTestCase.Key.TEST_CASE_ID;
                newTestCase.TEST_CASE_NAME = regTestCase.Key.TEST_CASE_NAME;
                newTestCase.TEST_STEP_DESCRIPTION = regTestCase.Key.TEST_STEP_DESCRIPTION;
                B_REGISTERED_APPS currentApp = B_REGISTERED_APPS.GetApplicationByAppIdFromCache(strDBIdx, regTestCase.Key.APPLICATION_ID, ref strError, ref isOk);
                if (currentApp != null)
                {
                    newTestCase.APP_SHORT_NAME = currentApp.APP_SHORT_NAME;// marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).APP_SHORT_NAME;
                    newTestCase.VERSION = currentApp.VERSION;// marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestSuite.APPLICATION_ID).VERSION;
                }
                //newTestCase.APP_SHORT_NAME = marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestCase.Key.APPLICATION_ID).APP_SHORT_NAME;
                //newTestCase.VERSION = marsEntities.T_REGISTERED_APPS.FirstOrDefault(x => x.APPLICATION_ID == regTestCase.Key.APPLICATION_ID).VERSION;
                TestCase.Add(newTestCase);
            }
            return TestCase;
        }


        public static Dictionary<T_TEST_CASE_SUMMARYDTO, List<V_TEST_STEPS_FULLVISIONDTO>> GetTestCaseViaStoryBoardId(
            string strDBIdx,
            long testStoryBoardId, ref string strError, ref bool isRight, bool isNormalization = false)
        {
            Logger.Info("GetTestCaseViaStoryBoardId", string.Format("try to get testcase with steps information by stroyboard Id:[{0}]", testStoryBoardId));

            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = from tStbd in objEntities.T_PROJ_TC_MGR
                        where tStbd.STORYBOARD_ID == testStoryBoardId
                        orderby tStbd.RUN_ORDER
                        group tStbd by tStbd.TEST_CASE_ID into stdb
                        select stdb
                        ;
            if (query == null) return null;
            List<long> lstTCIds = query.Select(p => p.Key ?? -1).ToList();
            var query2 = from vStp in objEntities.V_TEST_STEPS_FULLVISION
                         from vTC in objEntities.T_TEST_CASE_SUMMARY
                         where lstTCIds.Contains(vTC.TEST_CASE_ID)
                         && vStp.TEST_CASE_ID == vTC.TEST_CASE_ID
                         select new
                         {
                             tc = vTC,
                             stp = vStp
                         };
            Dictionary<T_TEST_CASE_SUMMARY, List<V_TEST_STEPS_FULLVISION>> dicResultEntity = query2.GroupBy(p => p.tc, p => p.stp).ToDictionary(p => p.Key, p => p.ToList());
            Dictionary<T_TEST_CASE_SUMMARYDTO, List<V_TEST_STEPS_FULLVISIONDTO>> dicResult = new Dictionary<T_TEST_CASE_SUMMARYDTO, List<V_TEST_STEPS_FULLVISIONDTO>>();
            foreach (var itm in dicResultEntity.Keys)
            {
                if (itm == null)
                    continue;
                if (dicResultEntity[itm] == null) continue;
                List<V_TEST_STEPS_FULLVISIONDTO> lstStepInfo = null;
                dicResult.Add(T_TEST_CASE_SUMMARYAssembler.ToDTO(itm), lstStepInfo = V_TEST_STEPS_FULLVISIONAssembler.ToDTOs(dicResultEntity[itm]));

            }
            return dicResult;
        }


        public bool SaveTestCase(string strDBIdx, 
            long lSrcTestCaseId, string strNewTestCaseName, 
            int iSaveOption, ref string strError)
        {
            Logger.Info("SaveTestCase", string.Format("Save test case from :[{0}] to [{1}], with option:[{2}] ", lSrcTestCaseId, strNewTestCaseName, iSaveOption <= 0 ? "Copy Test case only" : "Copy Data sets too well"));
            if (string.IsNullOrWhiteSpace(strNewTestCaseName))
            {
                strError = "New test case name is null or empty.";
                return false;
            }
            ///Steps:
            /// 1, create test case information            
            /// 2, create new test steps 
            /// 3, if data sets are required, copy datasets and build relationship 
            /// 
            DbTransaction dbDtrans = null;
            try
            {
                string strTCName = strNewTestCaseName.Trim();
                MarsEntities objDBCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                /// Note: before save as testcase, name check is already done. 
                //var q = (from tc in objDBCntx.T_TEST_CASE_SUMMARY
                //         where tc.TEST_CASE_NAME == strTCName
                //         select tc).FirstOrDefault();

                if (objDBCntx.Database.Connection.State != System.Data.ConnectionState.Open)
                    objDBCntx.Database.Connection.Open();
                dbDtrans = objDBCntx.Database.Connection.BeginTransaction();
                //using (TransactionScope trans = new TransactionScope()) {
                /// 1, create test case information
                /// 
                int iUpdateRecordCnt = 0;
                long lNewTestCaseId = BoHelper.GetIdBySeqName(T_TEST_CASE_SUMMARY_SEQ, objDBCntx.Database.Connection);

                string strSqlInsertNewTestCase = string.Format(@"INSERT INTO T_TEST_CASE_SUMMARY(TEST_CASE_ID,TEST_CASE_NAME, TEST_STEP_DESCRIPTION, TEST_STEP_CREATE_TIME, USAGE_STATUS)
                    SELECT :TEST_CASE_ID,:NEW_TESTCASE_NAME,TEST_STEP_DESCRIPTION, TEST_STEP_CREATE_TIME, USAGE_STATUS
                    FROM T_TEST_CASE_SUMMARY 
                    WHERE TEST_CASE_ID={0}", lSrcTestCaseId);
                using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlInsertNewTestCase;
#if Managed_Driver
                    Oracle.ManagedDataAccess.Client.OracleParameter objP = new Oracle.ManagedDataAccess.Client.OracleParameter();
#else
                    Oracle.DataAccess.Client.OracleParameter objP = new Oracle.DataAccess.Client.OracleParameter();
#endif
                    objP.ParameterName = "TEST_CASE_ID";
                    objP.Value = lNewTestCaseId;
#if Managed_Driver
                    Oracle.ManagedDataAccess.Client.OracleParameter objP2 = new Oracle.ManagedDataAccess.Client.OracleParameter();
#else
                    Oracle.DataAccess.Client.OracleParameter objP2 = new Oracle.DataAccess.Client.OracleParameter();
#endif
                    objP2.ParameterName = "NEW_TESTCASE_NAME";
                    objP2.Value = strNewTestCaseName;

                    dbCmmd.Parameters.Add(objP);
                    dbCmmd.Parameters.Add(objP2);
                    iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("SaveTestCase", string.Format("Insert into T_TEST_CASE_SUMMARY, records:[{0}]", iUpdateRecordCnt));
                }

                /// 2, create new test steps 
                /// 
                B_TEST_STEPS objStp = new B_TEST_STEPS();
                bool isOk = objStp.DuplicateStepsFromSourceTestCase(lSrcTestCaseId, lNewTestCaseId, objDBCntx, ref strError);
                if (!isOk)
                {
                    Logger.Error("SaveTestCase", string.Format("Error from DuplicateStepsFromSourceTestCase \r\n\t{0}", strError));
                    return false;
                }

                #region using the same codes in a fucntion
                //string strSqlInsertTestSteps = string.Format(@"INSERT INTO T_TEST_STEPS(STEPS_ID, RUN_ORDER, KEY_WORD_ID, TEST_CASE_ID, OBJECT_ID, COLUMN_ROW_SETTING, VALUE_SETTING, ""COMMENT"", IS_RUNNABLE, OBJECT_NAME_ID)
                //    SELECT T_TEST_STEPS_SEQ.NEXTVAL, RUN_ORDER, KEY_WORD_ID, {0}, OBJECT_ID, COLUMN_ROW_SETTING, VALUE_SETTING, ""COMMENT"", IS_RUNNABLE, OBJECT_NAME_ID
                //    FROM T_TEST_STEPS 
                //    WHERE TEST_CASE_ID={1}", lNewTestCaseId, lSrcTestCaseId);
                //using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                //{
                //    dbCmmd.CommandText = strSqlInsertTestSteps;
                //    iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                //    Logger.Info("SaveTestCase", string.Format("Insert into T_TEST_STEPS, records:[{0}]", iUpdateRecordCnt));

                //}
                /// BUILD TEST CASE WITH APPINFO
                /// 
                #endregion

                string strSqlInsertTestCaseAppRel = string.Format(@"INSERT INTO REL_APP_TESTCASE(RELATIONSHIP_ID, APPLICATION_ID, TEST_CASE_ID) 
                    SELECT REL_APP_TESTCASE_SEQ.NEXTVAL, APPLICATION_ID, {0}
                    FROM REL_APP_TESTCASE
                    WHERE TEST_CASE_ID={1}", lNewTestCaseId, lSrcTestCaseId);
                using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlInsertTestCaseAppRel;
                    iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("SaveTestCase", string.Format("Insert into REL_APP_TESTCASE, records:[{0}]", iUpdateRecordCnt));
                }
                /// BUILD TEST CASE WITH TEST SUITE
                /// 
                string strSqlInsertTestCaseSuiteRel = string.Format(@"INSERT INTO REL_TEST_CASE_TEST_SUITE(RELATIONSHIP_ID, TEST_SUITE_ID, TEST_CASE_ID) 
                    SELECT REL_TEST_CASE_TEST_SUITE_SEQ.NEXTVAL, TEST_SUITE_ID, {0}
                    FROM REL_TEST_CASE_TEST_SUITE
                    WHERE TEST_CASE_ID={1}", lNewTestCaseId, lSrcTestCaseId);
                using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlInsertTestCaseSuiteRel;
                    iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("SaveTestCase", string.Format("Insert into REL_TEST_CASE_TEST_SUITE, records:[{0}]", iUpdateRecordCnt));
                }

                if (iSaveOption > 0)
                {
                    ///save datasets
                    /// steps: 
                    /// 1, create all relation ships with data setting
                    /// 
                    string strSqlInsertTestDataSetting = string.Format(@"INSERT INTO TEST_DATA_SETTING (DATA_SETTING_ID, STEPS_ID, LOOP_ID, DATA_VALUE, VALUE_OR_OBJECT, DESCRIPTION, DATA_SUMMARY_ID, DATA_DIRECTION, VERSION, CREATE_TIME, POOL_ID)
                        SELECT TEST_DATA_SETTING_SEQ.NEXTVAL, T2.STEPS_ID,DS.LOOP_ID,DS.DATA_VALUE, DS.VALUE_OR_OBJECT, DS.DESCRIPTION, DS.DATA_SUMMARY_ID, DS.DATA_DIRECTION, DS.VERSION, SYSDATE, DS.POOL_ID 
                        FROM T_TEST_STEPS T1, T_TEST_STEPS T2, TEST_DATA_SETTING DS
                        WHERE T1.RUN_ORDER=T2.RUN_ORDER  AND DS.STEPS_ID=T1.STEPS_ID AND T1.TEST_CASE_ID={0} AND T2.TEST_CASE_ID={1} ", lSrcTestCaseId, lNewTestCaseId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlInsertTestDataSetting;
                        iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                        Logger.Info("SaveTestCase", string.Format("Insert into TEST_DATA_SETTING, records:[{0}]", iUpdateRecordCnt));
                    }
                    /// 2, assign data summary for new test case
                    /// 
                    string strSqlInsertTCDataRel = string.Format(@"INSERT INTO REL_TC_DATA_SUMMARY(ID, DATA_SUMMARY_ID, TEST_CASE_ID,CREATE_TIME)
                        SELECT T_TEST_STEPS_SEQ.NEXTVAL,DATA_SUMMARY_ID,{1},SYSDATE
                        FROM REL_TC_DATA_SUMMARY
                        WHERE TEST_CASE_ID={0}", lSrcTestCaseId, lNewTestCaseId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlInsertTCDataRel;
                        iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                        Logger.Info("SaveTestCase", string.Format("Insert into REL_TC_DATA_SUMMARY, records:[{0}]", iUpdateRecordCnt));
                    }

                }
                else
                {
                    /// select any one of the Data summary
                    /// 
                    string strSqlFindOneDataSummaryId = string.Format("SELECT DATA_SUMMARY_ID FROM REL_TC_DATA_SUMMARY WHERE TEST_CASE_ID={0} AND ROWNUM=1", lSrcTestCaseId);
                    long lAssDataSummaryId = -1;
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlFindOneDataSummaryId;
                        DbDataReader dbRd = dbCmmd.ExecuteReader();
                        if ((dbRd == null) || (!dbRd.HasRows))
                        {
                            Logger.Error("SaveTestCase", strError = string.Format("Can't find any data summary assigned to testcase :[{0}]", lSrcTestCaseId));
                            dbDtrans.Rollback();
                            return false;
                        }
                        if (!dbRd.Read())
                        {
                            Logger.Error("SaveTestCase", strError = string.Format("Can't read from DBReader for testcase :[{1}]", lSrcTestCaseId));
                            dbDtrans.Rollback();
                            return false;
                        }
                        lAssDataSummaryId = (long)dbRd["DATA_SUMMARY_ID"];
                    }
                    // insert into test_data_setting
                    string strSqlInsertTestDataSetting = string.Format(@"INSERT INTO TEST_DATA_SETTING (DATA_SETTING_ID, STEPS_ID, LOOP_ID, DATA_VALUE, VALUE_OR_OBJECT, DESCRIPTION, DATA_SUMMARY_ID, DATA_DIRECTION, VERSION, CREATE_TIME, POOL_ID)
                            SELECT TEST_DATA_SETTING_SEQ.NEXTVAL, T2.STEPS_ID,DS.LOOP_ID,DS.DATA_VALUE, DS.VALUE_OR_OBJECT, DS.DESCRIPTION, DS.DATA_SUMMARY_ID, DS.DATA_DIRECTION, DS.VERSION, SYSDATE, DS.POOL_ID
                            FROM T_TEST_STEPS T1, T_TEST_STEPS T2, TEST_DATA_SETTING DS
                            WHERE T1.RUN_ORDER = T2.RUN_ORDER  AND DS.STEPS_ID = T1.STEPS_ID AND T1.TEST_CASE_ID ={0} AND DATA_SUMMARY_ID={2}
                            AND T2.TEST_CASE_ID ={1}",
                            lSrcTestCaseId, lNewTestCaseId, lAssDataSummaryId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlInsertTestDataSetting;
                        iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                        Logger.Info("SaveTestCase", string.Format("Insert into TEST_DATA_SETTING for One data set, records:[{0}]", iUpdateRecordCnt));
                    }

                    /// 2, assign data summary for new test case
                    /// 
                    string strSqlInsertTCDataRel = string.Format(@"INSERT INTO REL_TC_DATA_SUMMARY(ID, DATA_SUMMARY_ID, TEST_CASE_ID,CREATE_TIME)
                        VALUES(T_TEST_STEPS_SEQ.NEXTVAL,{0},{1},SYSDATE)", lAssDataSummaryId, lNewTestCaseId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlInsertTCDataRel;
                        iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                        Logger.Info("SaveTestCase", string.Format("Insert into REL_TC_DATA_SUMMARY FOR ONE data set, records:[{0}]", iUpdateRecordCnt));
                    }

                }

                dbDtrans.Commit();

                MarsDBGlobe_Cache.UpdateAppTestCaseCache();
                return true;

            }
            catch (Exception e)
            {
                try
                {
                    if (dbDtrans != null)
                    {
                        dbDtrans.Rollback();
                    }
                }
                catch (Exception) { }

                Logger.Error("SaveTestCase", strError = string.Format("Exception when save as test case:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
            #region old codes 

            //marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            ////marsEntities.Database.Connection.Open();
            //T_TEST_CASE_SUMMARYDTO newTestCase = new T_TEST_CASE_SUMMARYDTO();
            //B_TEST_CASE bTestCase = new B_TEST_CASE();
            //B_TEST_STEPS bTestStep = new B_TEST_STEPS();
            //B_REL_APP_TESTCASE bRelAppTestCase = new B_REL_APP_TESTCASE();
            //if (!bTestCase.TestCaseExists(ContextName))
            //{

            //    var oldTestCase = (from t in marsEntities.T_TEST_CASE_SUMMARY
            //                           //where t.TEST_CASE_NAME == searchName
            //                       where t.TEST_CASE_ID == refObjectId
            //                       select t).FirstOrDefault();

            //    long testCaseId = bTestCase.getTestCaseId();
            //    newTestCase.TEST_CASE_ID = testCaseId;
            //    newTestCase.TEST_CASE_NAME = ContextName;
            //    newTestCase.TEST_STEP_DESCRIPTION = oldTestCase.TEST_STEP_DESCRIPTION;

            //    LastTestCase = newTestCase.TEST_CASE_ID;

            //    // create REL_TC_DATA_SUMMARY
            //    var relDataSetIds = (from r in marsEntities.REL_TC_DATA_SUMMARY
            //                         where r.TEST_CASE_ID == oldTestCase.TEST_CASE_ID
            //                         select r.DATA_SUMMARY_ID);

            //    foreach (var relDataSetId in relDataSetIds)
            //    {
            //        BoHelper.CreateRelTCDataSummary((long)relDataSetId, newTestCase.TEST_CASE_ID);
            //        LastDataSet = (long)relDataSetId;
            //    }

            //    // create REL_TEST_CASE_TEST_SUITE
            //    var relTestCaseTestSuteIds = (from r in marsEntities.REL_TEST_CASE_TEST_SUITE
            //                                  where r.TEST_CASE_ID == oldTestCase.TEST_CASE_ID
            //                                  select r.TEST_SUITE_ID).ToList();

            //    DataTable dtr = DataTableUtil.ToDataTable(relTestCaseTestSuteIds);

            //    foreach (var relTestCaseTestSuteId in relTestCaseTestSuteIds)
            //    {
            //        B_REL_TEST_CASE_TEST_SUITE bRelTestCaseTestSuite = new B_REL_TEST_CASE_TEST_SUITE();
            //        bRelTestCaseTestSuite.TEST_SUITE_ID = relTestCaseTestSuteId;
            //        bRelTestCaseTestSuite.TEST_CASE_ID = newTestCase.TEST_CASE_ID;
            //        bRelTestCaseTestSuite.RELATIONSHIP_ID = bRelTestCaseTestSuite.getRelTestCasteTestSuite();
            //        marsEntities.REL_TEST_CASE_TEST_SUITE.Add(REL_TEST_CASE_TEST_SUITEAssembler.ToEntity(bRelTestCaseTestSuite));
            //    }

            //    // Create REL_APP_TEST_CASE rows
            //    var relAppTestCase = (from a in marsEntities.REL_APP_TESTCASE
            //                          where a.TEST_CASE_ID == oldTestCase.TEST_CASE_ID
            //                          select a);

            //    foreach (var a in relAppTestCase)
            //    {
            //        REL_APP_TESTCASEDTO relAppTestCaseDto = new REL_APP_TESTCASEDTO();
            //        relAppTestCaseDto.TEST_CASE_ID = testCaseId;
            //        relAppTestCaseDto.RELATIONSHIP_ID = bRelAppTestCase.getRelTestCaseAppId();
            //        relAppTestCaseDto.APPLICATION_ID = a.APPLICATION_ID;
            //        marsEntities.REL_APP_TESTCASE.Add(REL_APP_TESTCASEAssembler.ToEntity(relAppTestCaseDto));
            //    }

            //    // Create T_TEST_STEPS rows
            //    var testCaseTestStep = (from r in marsEntities.T_TEST_STEPS
            //                            where r.TEST_CASE_ID == oldTestCase.TEST_CASE_ID
            //                            select r);

            //    foreach (var r in testCaseTestStep)
            //    {
            //        T_TEST_STEPSDTO bTestCaseTestStepsDTo = new T_TEST_STEPSDTO();
            //        bTestCaseTestStepsDTo.STEPS_ID = BoHelper.GetTestStepsId();
            //        bTestCaseTestStepsDTo.TEST_CASE_ID = testCaseId;
            //        bTestCaseTestStepsDTo.KEY_WORD_ID = r.KEY_WORD_ID;
            //        bTestCaseTestStepsDTo.RUN_ORDER = r.RUN_ORDER;
            //        bTestCaseTestStepsDTo.OBJECT_ID = r.OBJECT_ID;
            //        bTestCaseTestStepsDTo.COLUMN_ROW_SETTING = r.COLUMN_ROW_SETTING;
            //        bTestCaseTestStepsDTo.VALUE_SETTING = r.VALUE_SETTING;
            //        bTestCaseTestStepsDTo.COMMENT = r.COMMENT;
            //        bTestCaseTestStepsDTo.IS_RUNNABLE = r.IS_RUNNABLE;
            //        marsEntities.T_TEST_STEPS.Add(T_TEST_STEPSAssembler.ToEntity(bTestCaseTestStepsDTo));

            //        AddTestDataSetting(bTestCaseTestStepsDTo.STEPS_ID, r.STEPS_ID, bTestStep);

            //    }

            //    // Create T_TEST_CASE_SUMMARY
            //    marsEntities.T_TEST_CASE_SUMMARY.Add(T_TEST_CASE_SUMMARYAssembler.ToEntity(newTestCase));
            //    try
            //    {

            //        if (marsEntities.Database.Connection.State != ConnectionState.Open)
            //            marsEntities.Database.Connection.Open();
            //        if (marsEntities.SaveChanges() > 0)
            //        {
            //            MarsTreeView.GetMarsTree();
            //            MessageBox.Show("Test case successfully saved as", "Test case SaveAs", MessageBoxButton.OK, MessageBoxImage.Information);
            //            Clear();
            //            MarsDBGlobe_Cache.UpdateAppTestCaseCache(); //AF added this
            //            return true;
            //        }
            //        else
            //        {
            //            marsEntities = null;
            //            MessageBox.Show("Error saving test case", "Test case SaveAs", MessageBoxButton.OK, MessageBoxImage.Warning);
            //            return false;
            //        }

            //    }
            //    catch (Exception ex)
            //    {
            //        marsEntities = null;
            //        MessageBox.Show(ex.InnerException.ToString(), "Test case saveas", MessageBoxButton.OK, MessageBoxImage.Error);
            //        return false;
            //    }
            //}
            //else
            //{
            //    MessageBox.Show("Test case already exists");
            //    return false;
            //}
            #endregion old codes
        }

        /// <summary>
        /// 依据现有testcaes 添加新的
        /// </summary>
        /// <param name="testCase"></param>
        /// <param name="currentDBContext"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public bool AddNewTestCase(string strDBIdx, T_TEST_CASE_SUMMARYDTO testCase, MarsEntities currentDBContext, ref string strError, ref long lNewTestCaseId)
        {
            Logger.logBegin("AddNewTestCase", string.Format("Test case name:[{0}]", testCase.TEST_CASE_NAME));
            try
            {
                testCase.TEST_CASE_ID = lNewTestCaseId = getTestCaseId(strDBIdx, currentDBContext);
                currentDBContext.Set<T_TEST_CASE_SUMMARY>();
                currentDBContext.T_TEST_CASE_SUMMARY.Add(testCase.ToEntity());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("AddNewTestCase", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("AddNewTestCase");
            }
        }

        public bool DeleteTestCaseById(string strDBIdx, long lTestId, ref string strError)
        {
            Logger.Info("DeleteTestCaseById", string.Format("testcase id:[{0}]", lTestId));
            DbTransaction trans = null;
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if (dbCntx.Database.Connection.State != System.Data.ConnectionState.Open)
                    dbCntx.Database.Connection.Open();
                ///steps:
                /// 1, if delete data is required, then delete runtime data
                /// 2, delete test case with steps
                /// 3, delete test case with data settings. if data setting is only one reference, then delete data summary
                /// 4, delete test case with test suite
                /// 5, delete test case with projects
                /// 6, delete test case with story board
                /// 7, delete test case info
                /// 
                trans = dbCntx.Database.Connection.BeginTransaction();

                long lRecCnt = -1;

                string strSqlDelRuntimeData = string.Format("DELETE T_TEST_REPORT_STEPS WHERE STEPS_ID IN ( SELECT STEPS_ID FROM T_TEST_STEPS WHERE TEST_CASE_ID = {0})", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelRuntimeData;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE T_TEST_REPORT_STEPS, records:[{0}]", lRecCnt));
                }
                strSqlDelRuntimeData = string.Format("DELETE T_TEST_REPORT WHERE TEST_CASE_ID={0}", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelRuntimeData;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE T_TEST_REPORT, records:[{0}]", lRecCnt));
                }
                strSqlDelRuntimeData = string.Format("DELETE T_PROJ_TEST_RESULT WHERE TEST_CASE_ID={0}", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelRuntimeData;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE T_PROJ_TEST_RESULT, records:[{0}]", lRecCnt));
                }

                /// 2, delete test case with data settings. if data setting is only one reference, then delete data summary
                /// 
                string strSqlDelDataSetting = string.Format("DELETE TEST_DATA_SETTING WHERE STEPS_ID IN (SELECT STEPS_ID FROM T_TEST_STEPS WHERE TEST_CASE_ID={0})", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelDataSetting;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE TEST_DATA_SETTING, records:[{0}]", lRecCnt));
                }
                strSqlDelDataSetting = string.Format("DELETE REL_TC_DATA_SUMMARY WHERE TEST_CASE_ID={0}", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelDataSetting;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE REL_TC_DATA_SUMMAY, records:[{0}]", lRecCnt));
                }

                /// 3, delete test case with steps
                /// 
                string strSqlDelSteps = "DELETE T_TEST_STEPS WHERE TEST_CASE_ID=" + lTestId;
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelSteps;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE T_TEST_STEPS, records:[{0}]", lRecCnt));
                }
                /// 4, delete test case with test suite
                /// 
                string strSqlDelTcTs = string.Format("DELETE REL_TEST_CASE_TEST_SUITE WHERE TEST_CASE_ID={0}", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelTcTs;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE REL_TEST_CASE_TEST_SUITE, records:[{0}]", lRecCnt));
                }
                /// 5, delete test case with projects
                /// 6, delete test case with story board   
                /// 
                string strSqlDelStoryboardDetail = string.Format("DELETE T_STORYBOARD_DATASET_SETTING WHERE STORYBOARD_DETAIL_ID IN (SELECT STORYBOARD_DETAIL_ID FROM T_PROJ_TC_MGR WHERE TEST_CASE_ID={0})", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelStoryboardDetail;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE T_STORYBOARD_DATASET_SETTING, records:[{0}]", lRecCnt));
                }
                string strSqlDelTcPro = string.Format("DELETE T_PROJ_TC_MGR WHERE TEST_CASE_ID={0}", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelTcPro;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE T_PROJ_TC_MGR, records:[{0}]", lRecCnt));
                }
                string strSqlDelTCApp = string.Format("DELETE REL_APP_TESTCASE WHERE TEST_CASE_ID={0}", lTestId);
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelTCApp;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE REL_APP_TESTCASE, records:[{0}]", lRecCnt));
                }
                /// 6, delete test case INFO
                /// 
                string strSqlDelStbTC = "DELETE T_TEST_CASE_SUMMARY WHERE TEST_CASE_ID=" + lTestId;
                using (DbCommand dbCmmd = dbCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelStbTC;
                    lRecCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DeleteTestCaseById", string.Format("DELETE T_TEST_CASE_SUMMARY, records:[{0}]", lRecCnt));
                }

                trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                if (trans != null)
                {
                    try
                    {
                        trans.Rollback();
                    }
                    catch (Exception) { }
                }
                Logger.Error("DeleteTestCaseById", strError = string.Format("Exception when delete test case id :[{0}],\r\n{1}\r\nStackTrace:[2]", lTestId, e.Message, e.StackTrace), e);
                return false;
            }

            #region old codes from Logical Layer

            //MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            //var testCase = (from t in marsEntities.T_TEST_CASE_SUMMARY
            //                where t.TEST_CASE_NAME == testCaseName
            //                select t).FirstOrDefault();


            //var testCaseSteps = (from s in marsEntities.T_TEST_STEPS
            //                         where s.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                         select s);

            //// delete report data begin

            //var testCaseStepsIds = (from ts in testCaseSteps select ts.STEPS_ID).ToList();

            //// T_TEST_REPORT_STEPS
            //var testReportSteps = (from trs in marsEntities.T_TEST_REPORT_STEPS
            //                      where (testCaseStepsIds.Contains((long)trs.STEPS_ID))  
            //                      select trs).ToList();

            //foreach (var tsr in testReportSteps)
            //{
            //    marsEntities.T_TEST_REPORT_STEPS.Remove(tsr);
            //}

            //// T_TEST_REPORT
            //var testReports = (from tr in marsEntities.T_TEST_REPORT
            //                       where tr.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                       select tr);

            //foreach (var tr in testReports)
            //{
            //    marsEntities.T_TEST_REPORT.Remove(tr);
            //}

            //// T_PROJ_TEST_RESULT
            //var testReportResults = (from trr in marsEntities.T_PROJ_TEST_RESULT
            //                   where trr.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                   select trr);

            //foreach (var trr in testReportResults)
            //{
            //    marsEntities.T_PROJ_TEST_RESULT.Remove(trr);
            //}

            //// delete report data end

            //// data settings and object pool data

            //SharedDataSetViewModel.Delete(testCaseStepsIds, false);

            //// remove test steps
            //foreach (var s in testCaseSteps)
            //{
            //     marsEntities.T_TEST_STEPS.Remove(s);
            //}

            //var relTestCaseTestSuite = (from r in marsEntities.REL_TEST_CASE_TEST_SUITE
            //                            where r.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                            select r);

            //foreach (var r in relTestCaseTestSuite)
            //{
            //    marsEntities.REL_TEST_CASE_TEST_SUITE.Remove(r);
            //}


            //var allDataSummaryIds = (from ds in marsEntities.REL_TC_DATA_SUMMARY
            //                         where ds.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                         select ds.DATA_SUMMARY_ID).Distinct().ToList();

            //var allDataSummary = (from ds in marsEntities.T_TEST_DATA_SUMMARY
            //                      join id in allDataSummaryIds on ds.DATA_SUMMARY_ID equals id
            //                      select ds).ToList(); ;

            //// Find all rows for REL_TC_DATA_SUMMARY
            //// SEEMS TO BE WRONG -- rewritten 
            ///*
            //var relTcDataSummary = (from rt in marsEntities.REL_TC_DATA_SUMMARY
            //                        join id in allDataSummaryIds on rt.DATA_SUMMARY_ID equals id
            //                        select rt).ToList();
            //*/

            // var relTcDataSummary = (from rt in marsEntities.REL_TC_DATA_SUMMARY 
            //                         where rt.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                         select rt).ToList();



            //// Delete DATA_SUMMARY rows

            //// AF NOT DELETING SHARED_POOL AND TEST_DATA_SUMMARY  preserve data even when TC is deleted
            ///*
            //foreach (var ds in allDataSummary)
            //{
            //    if (ds != null)
            //    {
            //        int dsCount =  (from rt in marsEntities.REL_TC_DATA_SUMMARY
            //                        where rt.DATA_SUMMARY_ID == ds.DATA_SUMMARY_ID
            //                        select rt).Count();

            //        if (dsCount == 1)
            //            marsEntities.T_TEST_DATA_SUMMARY.Remove(ds);
            //    }
            //}
            // */ 

            //// Delete from REL_TC_DATA_SUMMARY
            //foreach (var rt in relTcDataSummary)
            //{
            //    marsEntities.REL_TC_DATA_SUMMARY.Remove(rt);
            //}

            //// Delete from Storyboards

            //var storyboardIds = (from s in marsEntities.T_PROJ_TC_MGR
            //                     where s.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                     select s.STORYBOARD_DETAIL_ID).ToList();

            //foreach (var storyboard in storyboardIds)
            //{
            //    BoHelper.DeleteStoryboard((long)storyboard);
            //}

            //// Delete from REL_APP_TESTCASE
            //var relAppTestCase = (from a in marsEntities.REL_APP_TESTCASE
            //                      where a.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                      select a);

            //foreach (var a in relAppTestCase)
            //{
            //    marsEntities.REL_APP_TESTCASE.Remove(a);
            //}

            //marsEntities.T_TEST_CASE_SUMMARY.Remove(testCase);

            //    try
            //    {
            //        if (marsEntities.SaveChanges() > 0)
            //        {
            //            MarsTreeView.GetMarsTree();
            //            /// AF
            //            if (VMCollCash.cache.ContainsKey(testCaseName))
            //            {
            //                VMCollCash.cache.Remove(testCaseName);
            //            }
            //            ///

            //            System.Windows.MessageBox.Show("Test Case deleted successfully", "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Information);
            //            return true;
            //        }
            //        else
            //        {
            //            marsEntities = null;
            //            System.Windows.MessageBox.Show("Error deleting Test Case", "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            //            return false;
            //        }

            //    }


            //catch (Exception ex)
            //{
            //    marsEntities = null;
            //    System.Windows.MessageBox.Show(ex.InnerException.ToString(), "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return false;
            //}
            #endregion
        }
    }
}
