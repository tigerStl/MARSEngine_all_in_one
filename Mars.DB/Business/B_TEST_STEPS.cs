using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Mars.message.Business
{
    public class B_TEST_STEPS : T_TEST_STEPSDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_TEST_STEPS));
        internal const string TABLE_ID_SEQ = "T_TEST_STEPS_SEQ";

        #region data for additional
        public string OBJECT_NAME { get; set; }
        public string KEYWORD_NAME { get; set; }
        public string ASSIGNED_DATA { get; set; }
        #region form 3.0
        private B_KEYWORD assignedKeyword;
        public B_KEYWORD AssignedKeyword
        {
            get { return assignedKeyword; }
            set { assignedKeyword = value; }
        }

        private B_OBJECT_NAMEINFO assignedNameInfo;
        public B_OBJECT_NAMEINFO AssignedNameInfo
        {
            get
            {
                return assignedNameInfo;
            }
            set
            {
                assignedNameInfo = value;
            }
        }
        #endregion //for 3.0
        #endregion

        private List<TEST_DATA_SETTING> lstDataSetting = null;
        public List<TEST_DATA_SETTING> ListDataSetting
        {
            get { return lstDataSetting; }
            set
            {
                lstDataSetting = value;
                if (lstDataSetting != null)
                {
                    if (this.TEST_DATA_SETTING_DATA_SETTING_ID == null)
                        this.TEST_DATA_SETTING_DATA_SETTING_ID = new List<long>();
                    this.TEST_DATA_SETTING_DATA_SETTING_ID.Clear();
                    foreach (TEST_DATA_SETTING objDts in lstDataSetting)
                    {
                        if (objDts == null) return;
                        this.TEST_DATA_SETTING_DATA_SETTING_ID.Add(objDts.DATA_SETTING_ID);
                    }
                }
            }
        }
        public static B_TEST_STEPS ToBO(T_TEST_STEPS entity)
        {
            if (entity == null) return null;

            var bo = new B_TEST_STEPS();

            bo.STEPS_ID = entity.STEPS_ID;
            bo.RUN_ORDER = entity.RUN_ORDER;
            bo.KEY_WORD_ID = entity.KEY_WORD_ID;
            bo.TEST_CASE_ID = entity.TEST_CASE_ID;
            bo.OBJECT_ID = entity.OBJECT_ID;
            bo.COLUMN_ROW_SETTING = entity.COLUMN_ROW_SETTING;
            bo.VALUE_SETTING = entity.VALUE_SETTING;
            bo.COMMENT = entity.COMMENT;
            bo.IS_RUNNABLE = entity.IS_RUNNABLE;
#if v_16AndUp
            bo.OBJECT_NAME_ID = entity.OBJECT_NAME_ID;
#endif
            //entity.OnDTO((T_TEST_STEPSDTO)bo);

            return bo;
        }

        public bool OverrideTestStep(
            string strDBIdx,
            long lSourceTestStepId, 
            long iTestCaseId, 
            string keyword, 
            long? objNameId, 
            long runOrder,
            string stepsParameter, 
            string strDesc, 
            long? lObjRegTableId,
            ref string strError,
            MarsEntities objDbCntx)
        {
            Logger.logBegin("OverrideTestStep", string.Format("SourceTestStepId:[{0}] TestCaseId:[{1}]", lSourceTestStepId, iTestCaseId));
            MarsEntities dbCnn = objDbCntx == null ? Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDbCntx;
            try
            {
                var oTargetTestSTep = (from q in dbCnn.T_TEST_STEPS
                                       where q.STEPS_ID == lSourceTestStepId
                                       select q).FirstOrDefault();
                if (oTargetTestSTep == null)
                {
                    Logger.Error("OverrideTestStep", strError = string.Format("No such test step id :[{0}]", lSourceTestStepId));
                    return false;
                }
                B_KEYWORD kObj = new B_KEYWORD();
                bool isOk = false;
                T_KEYWORDDTO objKeyword = kObj.GetKeywordByName(strDBIdx, keyword, objDbCntx, ref isOk, ref strError);
                if (!isOk)
                    return false;
                dbCnn.T_TEST_STEPS.Attach(oTargetTestSTep);
                oTargetTestSTep.COLUMN_ROW_SETTING = stepsParameter;
                oTargetTestSTep.COMMENT = strDesc;
                oTargetTestSTep.IS_RUNNABLE = 1;

                oTargetTestSTep.KEY_WORD_ID = objKeyword.KEY_WORD_ID;

                oTargetTestSTep.OBJECT_NAME_ID = objNameId;
                oTargetTestSTep.RUN_ORDER = runOrder;
                oTargetTestSTep.TEST_CASE_ID = iTestCaseId;
                oTargetTestSTep.VALUE_SETTING = "";
                var et = dbCnn.Entry(oTargetTestSTep);
                et.State = EntityState.Modified;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("OverrideTestStep", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("OverrideTestStep");
            }
        }

        public long InsertNewStep(string strDBIdx,long iTestCaseId, string keyword, long? objNameId, long runOrder,
            string stepsParamenter, string strDesc, long? lObjRegTableId,
            ref bool isOk, ref string strError,
            MarsEntities objDbCntx,
            bool isOverride = false)
        {
            Logger.logBegin("InsertNewStep", string.Format("#[{3}] keyword {0} NameId:[{1}] Parameter:[{2}] ", keyword, objNameId, stepsParamenter, runOrder));
            try
            {
                ///算法：
                /// 1，判断keyword是否存在
                /// 2，构建对象
                /// 

                /// 1，判断keyword是否存在
                /// 

                B_KEYWORD objKeyword = new B_KEYWORD();
                T_KEYWORDDTO objK = objKeyword.GetKeywordByName(strDBIdx, keyword, objDbCntx, ref isOk, ref strError);
                if ((objK == null) || (!isOk))
                {
                    Logger.Error("InsertNewStep", string.Format("Error from GetKeywordByName [{0}]", strError));
                    isOk = false;
                    return -1;
                }
                T_TEST_STEPSDTO objStp = new T_TEST_STEPSDTO();
                objStp.COLUMN_ROW_SETTING = stepsParamenter;
                objStp.COMMENT = strDesc;
                objStp.IS_RUNNABLE = 1;
                objStp.KEY_WORD_ID = objK.KEY_WORD_ID;
                objStp.OBJECT_ID = lObjRegTableId;
                objStp.OBJECT_NAME_ID = objNameId ?? -1;
                objStp.RUN_ORDER = runOrder;
                objStp.TEST_CASE_ID = TEST_CASE_ID;
                objStp.STEPS_ID = BoHelper.GetIdBySeqName(TABLE_ID_SEQ, objDbCntx);
                objStp.VALUE_SETTING = "";

                objDbCntx.Set<T_TEST_STEPS>();
                objDbCntx.T_TEST_STEPS.Add(objStp.ToEntity());
                Logger.Info("InsertNewStep", string.Format("Step is inserted, with new Id:[{0}], object new Id, [{1}] - [{2}-{3}]", objStp.STEPS_ID, lObjRegTableId, keyword, stepsParamenter));

                return objStp.STEPS_ID;
            }
            catch (Exception e)
            {
                Logger.Error("InsertNewStep", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return -1;
            }
            finally
            {
                Logger.logEnd("InsertNewStep");
            }
        }

        public static List<T_TEST_STEPSDTO> GetTestStepViaDetailId(string strDBIdx, 
            long? lstrybrdId)
        {
            Logger.logBegin("GetTestStepViaDetailId", string.Format("detail id:[{0}]", lstrybrdId));
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var strybrd = (from s in dbCntx.T_PROJ_TC_MGR
                               from stp in dbCntx.T_TEST_STEPS
                               where s.STORYBOARD_DETAIL_ID == lstrybrdId
                               && s.TEST_CASE_ID == stp.TEST_CASE_ID
                               select stp).OrderBy(p => p.RUN_ORDER);
                return strybrd.ToDTOs();
            }
            catch (Exception e)
            {
                Logger.Error("GetTestStepViaDetailId", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }

        public bool DuplicateStepsFromSourceTestCase(long lSourceTestCaseId, long lNewTestCaseId, MarsEntities objDBCntx, ref string strError)
        {
            Logger.logBegin("DuplicateStepsFromSourceTestCase", string.Format("From Test Case Id:[{0}] to [{1}]", lSourceTestCaseId, lNewTestCaseId));
            try
            {
                if (objDBCntx.Database.Connection.State != ConnectionState.Open)
                    objDBCntx.Database.Connection.Open();
                string strSqlInsertTestSteps = string.Format(@"INSERT INTO T_TEST_STEPS(STEPS_ID, RUN_ORDER, KEY_WORD_ID, TEST_CASE_ID, OBJECT_ID, COLUMN_ROW_SETTING, VALUE_SETTING, ""COMMENT"", IS_RUNNABLE, OBJECT_NAME_ID)
                    SELECT T_TEST_STEPS_SEQ.NEXTVAL, RUN_ORDER, KEY_WORD_ID, {0}, OBJECT_ID, COLUMN_ROW_SETTING, VALUE_SETTING, ""COMMENT"", IS_RUNNABLE, OBJECT_NAME_ID
                    FROM T_TEST_STEPS 
                    WHERE TEST_CASE_ID={1}", lNewTestCaseId, lSourceTestCaseId);
                using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlInsertTestSteps;
                    int iUpdateRecordCnt = dbCmmd.ExecuteNonQuery();
                    Logger.Info("DuplicateStepsFromSourceTestCase", string.Format("Insert into T_TEST_STEPS, records:[{0}]", iUpdateRecordCnt));
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DuplicateStepsFromSourceTestCase", strError = string.Format("Exception:[{0}], stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("DuplicateStepsFromSourceTestCase");
            }
        }

        public static List<T_TEST_STEPSDTO> GetTestStepsByTestCaseID(string strDBIdx, 
            long iTestId, ref string strError, ref bool isOk)
        {
            Logger.logBegin("GetTestStepsByTestCaseID", string.Format("iTestId:[{0}]", iTestId));
            MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var stps = (from stp in dbCntx.T_TEST_STEPS
                            where stp.TEST_CASE_ID == iTestId
                            orderby stp.RUN_ORDER
                            select stp).ToDTOs();
                isOk = true;
                return stps;
            }
            catch (Exception e)
            {
                Logger.Error("GetTestStepsByTestCaseID", strError = string.Format("Exception:[{0}]", e.Message), e);
                isOk = false;
                return null;
            }
        }

        public static List<T_TEST_STEPSDTO> GetTestStepsByTestCaseName(string strDBIdx, 
            string testCaseName, ref string strError, ref bool isOk)
        {
            Logger.logBegin("GetTestStepsByTestCaseName", string.Format("tc name:[{0}]", testCaseName));
            MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var stps = (from tc in dbCntx.T_TEST_CASE_SUMMARY
                            where tc.TEST_CASE_NAME == testCaseName
                            from stp in dbCntx.T_TEST_STEPS
                            where stp.TEST_CASE_ID == tc.TEST_CASE_ID
                            orderby stp.RUN_ORDER
                            select stp).ToDTOs();
                isOk = true;
                return stps;
            }
            catch (Exception e)
            {
                Logger.Error("GetTestStepsByTestCaseName", strError = string.Format("Exception:[{0}]", e.Message), e);
                isOk = false;
                return null;
            }
        }



        public bool DeleteTestStep(List<long> lstStpsIds, MarsEntities dbCntx, ref string strError)
        {
            Logger.logBegin("DeleteTestSteps");
            try
            {
                //  REMOVE T_TEST_REPORT_STPES
                var rptsStps = from q in dbCntx.T_TEST_REPORT_STEPS
                               where lstStpsIds.Contains(q.STEPS_ID ?? -1)
                               select q;
                if (rptsStps != null)
                {
                    var lstStpsTobeDelted = rptsStps.ToList();
                    dbCntx.Set<T_TEST_REPORT_STEPS>();
                    foreach (var itmStpsRpt in lstStpsTobeDelted)
                    {
                        dbCntx.T_TEST_REPORT_STEPS.Remove(itmStpsRpt);
                    }
                }

                // remove test_data_setting
                var stpDataSetting = from q in dbCntx.TEST_DATA_SETTING
                                     where lstStpsIds.Contains(q.STEPS_ID)
                                     select q;
                if (stpDataSetting == null)
                    return true;
                dbCntx.Set<TEST_DATA_SETTING>();
                foreach (var itm in stpDataSetting)
                {
                    dbCntx.TEST_DATA_SETTING.Remove(itm);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteTestSteps", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("DeleteTestSteps");
            }
        }

        public static bool StepHasTorlarenceInfo(T_TEST_STEPSDTO objStpsInfo, ref string strTorlarenceFunc)
        {
            if (objStpsInfo == null) return false;
            if (string.IsNullOrEmpty(objStpsInfo.COMMENT)) return false;
            if (!objStpsInfo.COMMENT.ToUpper().StartsWith("TOL:")) return false;
            strTorlarenceFunc = objStpsInfo.COMMENT.Substring("TOL:".Length);
            return !string.IsNullOrEmpty(strTorlarenceFunc);
        }

        public static bool UpdateStepWithNewObjectNameId(long testStepId, DbCommand dbCmmd, long lTargetNameId, ref string strError)
        {
            Logger.logBegin("UpdateStepWithNewObjectNameId", string.Format("Test case Id:[{0}], nameId:[{1}]", testStepId, lTargetNameId));
            try
            {
                dbCmmd.Parameters.Clear();
                string sql = @"UPTE T_TEST_STEPS SET OBJECT_NAME_ID=" + lTargetNameId;
                dbCmmd.CommandText = sql;
                dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateStepWithNewObjectNameId", strError = string.Format("Exception:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public static bool UpdateStepWithData(long sTEPS_ID, long runOrder, long kEY_WORD_ID, long tEST_CASE_ID, long objectId,
            string stepsParamenter, string valueForRow, string strComment, bool isRunnable, long lTargetNameId,
            DbCommand dbCmmd, ref string strError)
        {
            Logger.logBegin("UpdateStepWithData", string.Format("stepsId:[{0}] runOrder:[{1}]", sTEPS_ID, runOrder));
            //update all test steps 
            string strSqlUpdateSteps = @"UPDATE T_TEST_STEPS(STEPS_ID, RUN_ORDER, KEY_WORD_ID, TEST_CASE_ID, OBJECT_ID, COLUMN_ROW_SETTING,VALUE_SETTING, COMMENT, IS_RUNNABLE, OBJECT_NAME_ID) 
                                                 VALUES(:STEPS_ID, :RUN_ORDER, :KEY_WORD_ID, :TEST_CASE_ID, :OBJECT_ID, :COLUMN_ROW_SETTING,:VALUE_SETTING,:COMMENT, :IS_RUNNABLE, :OBJECT_NAME_ID) ";
            try
            {
                dbCmmd.CommandText = strSqlUpdateSteps;
                dbCmmd.Parameters.Clear();
                DbParameter paraSTEPS_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraSTEPS_ID.ParameterName = "STEPS_ID";
                paraSTEPS_ID.Value = sTEPS_ID;
                DbParameter paraRUN_ORDER = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraRUN_ORDER.ParameterName = "RUN_ORDER";
                paraRUN_ORDER.Value = runOrder;
                DbParameter paraKEY_WORD_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraKEY_WORD_ID.ParameterName = "KEY_WORD_ID";
                paraKEY_WORD_ID.Value = kEY_WORD_ID;
                DbParameter paraTEST_CASE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraTEST_CASE_ID.ParameterName = "TEST_CASE_ID";
                paraTEST_CASE_ID.Value = tEST_CASE_ID;
                DbParameter paraobjectId = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraobjectId.ParameterName = "OBJECT_ID";
                paraobjectId.Value = objectId;
                DbParameter paraCOLUMN_ROW_SETTING = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraCOLUMN_ROW_SETTING.ParameterName = "COLUMN_ROW_SETTING";
                paraCOLUMN_ROW_SETTING.Value = stepsParamenter;
                DbParameter paraVALUE_SETTING = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraVALUE_SETTING.ParameterName = "VALUE_SETTING";
                paraVALUE_SETTING.Value = valueForRow;
                DbParameter paraCOMMENT = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraCOMMENT.ParameterName = "COMMENT";
                paraCOMMENT.Value = strComment;
                DbParameter paraIS_RUNNABLE = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraIS_RUNNABLE.ParameterName = "IS_RUNNABLE";
                paraIS_RUNNABLE.Value = isRunnable;
                DbParameter paraOBJECT_NAME_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraOBJECT_NAME_ID.ParameterName = "OBJECT_NAME_ID";
                paraOBJECT_NAME_ID.Value = lTargetNameId;

                dbCmmd.Parameters.Add(paraSTEPS_ID);
                dbCmmd.Parameters.Add(paraRUN_ORDER);
                dbCmmd.Parameters.Add(paraKEY_WORD_ID);
                dbCmmd.Parameters.Add(paraTEST_CASE_ID);
                dbCmmd.Parameters.Add(paraCOLUMN_ROW_SETTING);
                dbCmmd.Parameters.Add(paraVALUE_SETTING);
                dbCmmd.Parameters.Add(paraCOMMENT);
                dbCmmd.Parameters.Add(paraIS_RUNNABLE);
                dbCmmd.Parameters.Add(paraOBJECT_NAME_ID);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateStepWithData", strError = string.Format("exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public static T_TEST_STEPSDTO CreateTestStep(long kEY_WORD_ID, long lObjeNameId, string stepsParamenter, long testcaseId, long runOrder, string strCmmt, DbCommand dbCmmd, ref bool isOk, ref string strError)
        {
            try
            {
                T_TEST_STEPSDTO rslt = new T_TEST_STEPSDTO();
                dbCmmd.Parameters.Clear();
                long lTestStepIdNew = BoHelper.GetBussinessSeq(TABLE_ID_SEQ, dbCmmd, ref strError, ref isOk);
                string strSql = @"INSERT INTO T_TEST_STEPS(STEPS_ID, RUN_ORDER, KEY_WORD_ID, TEST_CASE_ID, OBJECT_ID, COLUMN_ROW_SETTING, VALUE_SETTING, IS_RUNNABLE, OBJECT_NAME_ID)
                                   VALUES(:STEPS_ID, :RUN_ORDER, :KEY_WORD_ID, :TEST_CASE_ID, null, :COLUMN_ROW_SETTING, null, 1, :OBJECT_NAME_ID)";
                dbCmmd.CommandText = strSql;
                DbParameter paraSTEPS_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraSTEPS_ID.ParameterName = "STEPS_ID";
                paraSTEPS_ID.Value = rslt.STEPS_ID = lTestStepIdNew;

                DbParameter paraRUN_ORDER = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraRUN_ORDER.ParameterName = "RUN_ORDER";
                paraRUN_ORDER.Value = rslt.RUN_ORDER = runOrder;

                DbParameter paraKEY_WORD_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraKEY_WORD_ID.ParameterName = "KEY_WORD_ID";
                paraKEY_WORD_ID.Value = rslt.KEY_WORD_ID = kEY_WORD_ID;

                DbParameter paraTEST_CASE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraTEST_CASE_ID.ParameterName = "TEST_CASE_ID";
                paraTEST_CASE_ID.Value = rslt.TEST_CASE_ID = testcaseId;

                DbParameter paraCOLUMN_ROW_SETTING = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraCOLUMN_ROW_SETTING.ParameterName = "COLUMN_ROW_SETTING";
                paraCOLUMN_ROW_SETTING.Value = rslt.COLUMN_ROW_SETTING = stepsParamenter;

                //DbParameter paraCOMMENT = new Oracle.ManagedDataAccess.Client.OracleParameter();
                //paraCOMMENT.ParameterName = "COMMENTX";
                //paraCOMMENT.Value = rslt.COMMENT = strCmmt;

                DbParameter paraOBJECT_NAME_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraOBJECT_NAME_ID.ParameterName = "OBJECT_NAME_ID";
                paraOBJECT_NAME_ID.Value = rslt.OBJECT_NAME_ID = lObjeNameId;

                dbCmmd.Parameters.Add(paraSTEPS_ID);
                dbCmmd.Parameters.Add(paraRUN_ORDER);
                dbCmmd.Parameters.Add(paraKEY_WORD_ID);
                dbCmmd.Parameters.Add(paraTEST_CASE_ID);
                dbCmmd.Parameters.Add(paraCOLUMN_ROW_SETTING);
                //dbCmmd.Parameters.Add(paraCOMMENT);
                dbCmmd.Parameters.Add(paraOBJECT_NAME_ID);

                dbCmmd.ExecuteNonQuery();
                isOk = true;
                return rslt;
            }
            catch (Exception e)
            {
                Logger.Error("CreateTestStep", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                isOk = false;
                return null;
            }
        }

        internal static Dictionary<long, List<T_TEST_STEPSDTO>> GetStepsByStoryboardId(string strDBIdx, long storyboarId)
        {
            Logger.logBegin("GetStepsByStoryboardId", string.Format("stroyboardId:[{0}]", storyboarId));
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var s1 = from q in dbCntx.T_PROJ_TC_MGR
                         from stp in dbCntx.T_TEST_STEPS
                         where q.TEST_CASE_ID == stp.TEST_CASE_ID
                         && q.STORYBOARD_ID == storyboarId
                         select new
                         {
                             dtl_id = q.STORYBOARD_DETAIL_ID,
                             steps = stp
                         };
                var s = s1.GroupBy(p => p.dtl_id)
                    .ToDictionary(p => p.Key, g => g.Select(x => x.steps).ToDTOs());
                //group stp by q.STORYBOARD_DETAIL_ID into g                         
                //select new
                //{
                //    key = g.Key,
                //    stps = g.
                //}).ToList();
                return s;
            }
            catch (Exception e)
            {
                Logger.Error("GetStepsByStoryboardId", e.Message, e);
                return new Dictionary<long, List<T_TEST_STEPSDTO>>();
            }
        }

        internal static int UpdateNameIdToSpecByCnn(DbConnection dbConnection, long targetId, List<long> toUpDate, ref bool isOk, ref string strError)
        {
            string strIds = "";
            Logger.logBegin("UpdateNameIdToSpecByCnn", strIds = string.Join(",", toUpDate));
            try
            {
                using (DbCommand dbCmmd = dbConnection.CreateCommand())
                {
                    string strSql = @"UPDATE T_TEST_STEPS SET OBJECT_NAME_ID=" + targetId
                                     + " WHERE OBJECT_NAME_ID IN (" + strIds + ")";
                    Logger.Info("UpdatObjIdToSpecByCnn", strSql);
                    dbCmmd.CommandText = strSql;
                    int iCnt = dbCmmd.ExecuteNonQuery();
                    isOk = true;
                    return iCnt;
                }
            }
            catch (Exception e)
            {
                Logger.Error("UpdateNameIdToSpecByCnn", strError = e.Message, e);
                isOk = false;
                return -1;
            }
        }

        internal static bool UpdatObjIdToSpecByCnn(DbConnection dbCnn, long targetId, List<long> lstToUpdate, ref string strError, ref int iTmpCnt)
        {
            try
            {

                string strIds = string.Join(",", string.Join(",", lstToUpdate));
                Logger.logBegin("UpdatObjIdToSpecByCnn", strIds);
                string strSql = @"UPDATE T_TEST_STEPS SET OBJECT_ID = " + targetId
                                + " WHERE OBJECT_ID IN (" + strIds + ")";
                Logger.Info("UpdatObjIdToSpecByCnn", strSql);
                using (DbCommand dbCmmd = dbCnn.CreateCommand())
                {
                    dbCmmd.CommandText = strSql;
                    iTmpCnt = dbCmmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception e)
            {
                iTmpCnt = -1;
                Logger.Error("UpdatObjIdToSpecByCnn", strError = e.Message, e);
                return false;
            }
        }

        /*
        public List<B_TEST_STEPS> GetTestSteps(long testCaseId)
        {
            MarsEntities marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx)()();
            List<B_TEST_STEPS> testStepList = new List<B_TEST_STEPS>();
            var testSteps = (from c in marsEntities.T_TEST_STEPS
                             where c.TEST_CASE_ID == testCaseId
                                orderby c.RUN_ORDER
                                select c);

            foreach (T_TEST_STEPS tStep in testSteps)
            {
                B_TEST_STEPS newTeststeps = new B_TEST_STEPS();
                newTeststeps.STEPS_ID = tStep.STEPS_ID;
                newTeststeps.KEY_WORD_ID = tStep.KEY_WORD_ID;
                newTeststeps.OBJECT_ID = tStep.OBJECT_ID;
                newTeststeps.COLUMN_ROW_SETTING = tStep.COLUMN_ROW_SETTING;
                newTeststeps.RUN_ORDER = tStep.RUN_ORDER;
                newTeststeps.IS_RUNNABLE = tStep.IS_RUNNABLE;
                newTeststeps.VALUE_SETTING = tStep.VALUE_SETTING;
                newTeststeps.COMMENT = tStep.COMMENT;
                newTeststeps.TEST_DATA_SETTING_DATA_SETTING_ID = LoadTestDataSettingIds(tStep.STEPS_ID);
                testStepList.Add(newTeststeps);
            }
            return testStepList;
        }

        public List<TEST_DATA_SETTING> LoadTestDataSettings(long stepsId)
        {
            MarsEntities marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx)()();
            List<TEST_DATA_SETTING> testStepList = new List<TEST_DATA_SETTING>();
            var testDataSettings = (from c in marsEntities.TEST_DATA_SETTING
                                    where c.STEPS_ID == stepsId
                             orderby c.STEPS_ID select c);
            if (testDataSettings != null && testDataSettings.Count() > 0)
            {
                foreach (TEST_DATA_SETTING tStep in testDataSettings)
                {
                    testStepList.Add(tStep);
                }
            }
            return testStepList;
        }


        private List<long> LoadTestDataSettingIds(long stepsId)
        {
            MarsEntities marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx)()();
            List<long> dataSettingIds = new List<long>();
            var testDataSettings = (from c in marsEntities.TEST_DATA_SETTING
                                    where c.STEPS_ID == stepsId
                                    orderby c.STEPS_ID
                                    select c);

            foreach (TEST_DATA_SETTING tStep in testDataSettings)
            {
                dataSettingIds.Add(tStep.DATA_SETTING_ID);
            }
            return dataSettingIds;
        }

        public long GetTestStepsId()
        {
            MarsEntities marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx)()();
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
            long projectId = (long)marsEntities.GETNEXT_VAL("T_TEST_STEPS_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }

        public long GetDataSettingId()
        {
            MarsEntities marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx)()();
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
            long projectId = (long)marsEntities.GETNEXT_VAL("TEST_DATA_SETTING_SEQ", outparam);
            return long.Parse(outparam.Value.ToString());

        }
         */
    }
}
