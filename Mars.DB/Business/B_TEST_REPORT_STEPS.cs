using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Objects;
using System.Linq;
#if !_pythonInterface
using Route2NSEx.src.Marquis.systemUtil;
#endif
#if !_noEntities
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Text;
#endif
#if _marsLog
using Logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.Business
{
    [Serializable]
    public class B_TEST_REPORT_STEPS : T_TEST_REPORT_STEPSDTO
    {
#if !_marsLog
        private static MLogger Logger = MLogger.GetLogger(typeof(B_TEST_REPORT_STEPS));
#endif
        public const string CNST_SEQ_IDNAME = "SEQ_TEST_REPORT_STEPS";
        public int CreateIdAndSave(ref string strError, ref string strAdv, 
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("CreateIdAndSave", $"db:[{strDBIdx}]");
            try
            {
#if !_forWebClient
                this.TEST_REPORT_STEP_ID = BoHelper.GetIdBySeqName(CNST_SEQ_IDNAME,strDBIdx:strDBIdx);
                DataLayer.Generic.MarsDataAccessLayer<T_TEST_REPORT_STEPS> objDBAc = new DataLayer.Generic.MarsDataAccessLayer<T_TEST_REPORT_STEPS>(strDBIdx);
                //Logger.Info("\t", "stackInfo Len:" + (string.IsNullOrEmpty(objItem.STACKINFO) ? "0" : objItem.STACKINFO.Length + ""));
                if (this.STACKINFO != null)
                {
                    byte[] b = Encoding.ASCII.GetBytes(this.STACKINFO);
                    Logger.Info("\t", $"byte array len:[{b.Length}]");
                    if (b.Length >= 1024)
                    {
                        Array.Resize(ref b, 1024);
                        //this.STACKINFO = "to long";
                    }
                    this.STACKINFO = Encoding.ASCII.GetString(b);

                }
                return objDBAc.AddSingle(this.ToEntity());
#else
                //this.TEST_REPORT_STEP_ID = BoHelper.GetApplicationIdByName(CNST_SEQ_IDNAME);
                //if (this.T_TEST_REPORT_TEST_REPORT_ID == -1)
                //{
                //    strError = string.Format("can't get new test report id from sequence:[{0}]", CNST_SEQ_IDNAME);
                //    return -1;
                //}
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                bool isOk = false;
                int iCnt = clnt.SaveTEST_REPORT_STEPS(this, ref isOk, ref strError, ref strAdv);
                return iCnt;
#endif
            }
            catch (Exception e)
            {
                Logger.Error("\t", $"{e.GetType()}");
                Logger.Error("CreateIdAndSave", strError = string.Format("Exception:[{0}]", e.Message), e);
                if(e is DbEntityValidationException)
                {
                    DbEntityValidationException ex = (DbEntityValidationException)e;
                    var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                    // Join the list to a single string.
                    var fullErrorMessage = string.Join("; ", errorMessages);
                    Logger.Error("\tfullErrorMessage", fullErrorMessage);
                }
                return -1;
            }
        }

        public int updateRecord(string strDBIdx )//= MarsEntitiesExtends.cnst_default_dbName )
        {
            Logger.logBegin("updateRecord", $"TEST_REPORT_STEP_ID:{this.TEST_REPORT_STEP_ID}, TEST_REPORT_ID:{this.TEST_REPORT_ID}, {this.RUNNING_RESULT}");
            try
            {
#if !_forWebClient
                MarsEntities objEntitis = BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);

                T_TEST_REPORT_STEPS objResult = objEntitis.T_TEST_REPORT_STEPS.Single(p => p.TEST_REPORT_STEP_ID == this.TEST_REPORT_STEP_ID);
                if (objResult == null)
                {
                    Logger.Error("updateRecord", string.Format("Can't get special Id for Test_report_Step_id. Id:[{0}]", this.TEST_REPORT_STEP_ID));
                    return -1;
                }

                objResult.ACTUAL_INPUT_DATA = this.ACTUAL_INPUT_DATA;
                objResult.BEGIN_TIME = this.BEGIN_TIME;
                objResult.DATA_ORDER = this.DATA_ORDER;
                objResult.DATA_SUMMARY_ID = this.DATA_SUMMARY_ID;
                objResult.END_TIME = this.END_TIME;
                objResult.INPUT_VALUE_SETTING = this.INPUT_VALUE_SETTING;
                objResult.RETURN_VALUES = this.RETURN_VALUES;
                objResult.RUNNING_RESULT = this.RUNNING_RESULT;
                objResult.RUNNING_RESULT_INFO = this.RUNNING_RESULT_INFO;
                objResult.STEPS_ID = this.STEPS_ID;
                objResult.TEST_REPORT_ID = this.TEST_REPORT_ID;
                if (this.INFO_PIC!=null)
                    objResult.INFO_PIC = this.INFO_PIC;
                objResult.ADVICE = this.ADVICE;
                objResult.STACKINFO = this.STACKINFO;
                Logger.Info("----test byte[] Len----", string.Format("copied data lenth:[{0}]", objResult.INFO_PIC == null ? -1 : objResult.INFO_PIC.Length));

                objEntitis.SaveChanges();
                return 1;
#else
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                bool isOk = false;
                string strError = "";
                int iUpdateCnt = clnt.updateRecordTestReportStepsRecord(this, ref isOk, ref strError);
                if (!isOk) return -1;
                return iUpdateCnt;
#endif
            }
            catch (Exception e)
            {
                Logger.Error("updateRecord", string.Format("Exception:[{0}]", e.Message), e);
                return -1;
            }
        }

        public bool DeleteRptStepsAfterByStepsId(string stepId, string testReportId, string currentDBIdx,ref int iRsltCnt, ref string strError)
        {
            Logger.logBegin("DeleteRptStepsAfterByStepsId", $"[{stepId}] testReportId:[{testReportId}], DBIdx:[{currentDBIdx}]");
            try
            {
                string strSql = @"delete t_test_report_steps
where test_report_id = :testReportId
and steps_id in (
    select b.steps_id from t_test_steps a,
    t_test_steps b
    where
      a.steps_id = :stepId
    and a.test_case_id = b.test_case_id
    and b.run_order >= a.run_order
)";
                MarsEntities objEntitis = BoHelper.GetMarsEntitiesInstance(strCurrentDB: currentDBIdx);
                DbConnection db = objEntitis.Database.Connection;
                if (db == null)
                {
                    strError = $"can't get connection from {currentDBIdx}";
                    Logger.Error("\t", $"{strError}\r\n{Environment.StackTrace}" );
                    return false;
                }
                if (db.State != ConnectionState.Open)
                {
                    db.Open();
                }
                var trans = db.BeginTransaction();
                try
                {
                    var cmmd = db.CreateCommand();
                    cmmd.CommandText = strSql;
                    DbParameter dbTestReportId = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    dbTestReportId.ParameterName = "testReportId";
                    dbTestReportId.Value = testReportId;

                    DbParameter dbStepId = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    dbStepId.ParameterName = "stepId";
                    dbStepId.Value = stepId;

                    cmmd.Parameters.Add(dbTestReportId);
                    cmmd.Parameters.Add(dbStepId);
                    iRsltCnt = cmmd.ExecuteNonQuery();

                    trans.Commit();

                    return true;
                }
                catch (Exception et)
                {
                    if (trans != null)
                    {
                        try
                        {
                            trans.Rollback();
                        }
                        catch (Exception)
                        {
                        }
                        
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Error("DeleteRptStepsAfterByStepsId", strError = e.Message, e);
                return false;
            }
            finally
            {
                Logger.logEnd("DeleteRptStepsAfterByStepsId");
            }
        }

        public B_TEST_REPORT_STEPS CloneSelf()
        {
            B_TEST_REPORT_STEPS objNew = new B_TEST_REPORT_STEPS();
            objNew.ACTUAL_INPUT_DATA = this.ACTUAL_INPUT_DATA;
            objNew.BEGIN_TIME = this.BEGIN_TIME;
            objNew.DATA_ORDER = this.DATA_ORDER;
            objNew.DATA_SUMMARY_ID = this.DATA_SUMMARY_ID;
            objNew.END_TIME = this.END_TIME;
            objNew.INPUT_VALUE_SETTING = this.INPUT_VALUE_SETTING;
            objNew.RETURN_VALUES = this.RETURN_VALUES;
            objNew.RUNNING_RESULT = this.RUNNING_RESULT;
            objNew.RUNNING_RESULT_INFO = this.RUNNING_RESULT_INFO;
            objNew.STEPS_ID = this.STEPS_ID;
            objNew.TEST_REPORT_ID = this.TEST_REPORT_ID;
            objNew.TEST_REPORT_STEP_ID = this.TEST_REPORT_STEP_ID;
            objNew.ADVICE = this.ADVICE;
            objNew.STACKINFO = this.STACKINFO;
#if db4Oracle
            objNew.T_TEST_STEPS_STEPS_ID = this.T_TEST_STEPS_STEPS_ID;
#endif
            objNew.INFO_PIC = this.INFO_PIC;

            return objNew;
        }

#if !_forWebSvc
        public static bool InsertTestStepResultForKeyCompare(long? lRptId, long? stpId, 
            DateTime? beginTime,DateTime? endTime,
            short iSuccessId, List<KeyValuePair<string, string>> lstObjectNameAndValues,
            long? dATA_SUMMARY_ID, string strObjectNameIdx,
            string strRunningError,
            ref string strError,
            string strDBIdx)
#else
        public bool InsertTestStepResultForKeyCompare(long? lRptId, long? stpId, DateTime? beginTime,
            short iSuccessId, List<KeyValuePair<string, string>> lstObjectNameAndValues,
            long? dATA_SUMMARY_ID, string strObjectNameIdx,
            string strRunningError,
            ref string strError,
            string strDBIdx)
#endif
        {
#if !_forWebClient
            Logger.logBegin("InsertTestStepResultForKeyCompare",
                string.Format("rptId:[{0}] stpId:[{1}] sucesssId:[{2}] data row count:[{3}] DataSummaryId:[{4}] objectName for :[{5}]",
                lRptId, stpId, iSuccessId, lstObjectNameAndValues == null ? 0 : lstObjectNameAndValues.Count, dATA_SUMMARY_ID, strObjectNameIdx));
            // use sql to do 
            DbConnection dbCnn = null;
            DbTransaction dbTrans = null;
            try
            {
                MarsEntities objCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if ((dbCnn = objCntx.Database.Connection).State != ConnectionState.Open)
                {
                    dbCnn.Open();
                }
                dbTrans = dbCnn.BeginTransaction();
                string strSqlStpDta = "INSERT INTO T_TEST_REPORT_STEPS (TEST_REPORT_STEP_ID,TEST_REPORT_ID,STEPS_ID,BEGIN_TIME,END_TIME,RUNNING_RESULT,RETURN_VALUES,RUNNING_RESULT_INFO,DATA_SUMMARY_ID,INPUT_VALUE_SETTING,ACTUAL_INPUT_DATA) "
                    + " VALUES(SEQ_TEST_REPORT_STEPS.NEXTVAL, :TEST_REPORT_ID, :STEPS_ID,:BEGIN_TIME,SYSDATE,:RUNNING_RESULT,:RETURN_VALUES,:RUNNING_RESULT_INFO,:DATA_SUMMARY_ID,:INPUT_VALUE_SETTING,:ACTUAL_INPUT_DATA)";
                DbCommand dbCommand = dbCnn.CreateCommand();
                int iCurrentUpdateRecords = 0, iTotalUpdatedRecord = 0;
                for (int i = 0; i < lstObjectNameAndValues.Count; i++)
                {
                    var itm = lstObjectNameAndValues[i];
                    if (itm.Equals(default(KeyValuePair<string, string>))) continue;
                    dbCommand.Parameters.Clear();

                    dbCommand.CommandText = strSqlStpDta;

                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_TEST_REPORT_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_TEST_REPORT_ID.Value = lRptId;
                    oraPara_TEST_REPORT_ID.ParameterName = "TEST_REPORT_ID";
                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_STEPS_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_STEPS_ID.Value = stpId;
                    oraPara_STEPS_ID.ParameterName = "STEPS_ID";
                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_BEGIN_TIME = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_BEGIN_TIME.Value = beginTime;
                    oraPara_BEGIN_TIME.ParameterName = "BEGIN_TIME";
                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_RUNNING_RESULT = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_RUNNING_RESULT.Value = iSuccessId;
                    oraPara_RUNNING_RESULT.ParameterName = "RUNNING_RESULT";
                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_RETURN_VALUES = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_RETURN_VALUES.Value = itm.Value;
                    //oraPara_RETURN_VALUES.Value = string.Format("[{0}]-[{1}]",itm.Key,itm.Value);
                    oraPara_RETURN_VALUES.ParameterName = "RETURN_VALUES";
                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_RUNNING_RESULT_INFO = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_RUNNING_RESULT_INFO.Value = ((iSuccessId == 1) || (iSuccessId == 3)) ? "SUCCESS" : (iSuccessId == 2 ? strRunningError : "N/A");
                    oraPara_RUNNING_RESULT_INFO.ParameterName = "RUNNING_RESULT_INFO";
                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_DATA_SUMMARY_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_DATA_SUMMARY_ID.Value = dATA_SUMMARY_ID;
                    oraPara_DATA_SUMMARY_ID.ParameterName = "DATA_SUMMARY_ID";
                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_INPUT_VALUE_SETTING = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_INPUT_VALUE_SETTING.Value = itm.Key;
                    oraPara_INPUT_VALUE_SETTING.ParameterName = "INPUT_VALUE_SETTING";
                    Oracle.ManagedDataAccess.Client.OracleParameter oraPara_ACTUAL_INPUT_DATA = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    oraPara_ACTUAL_INPUT_DATA.Value = itm.Key;
                    oraPara_ACTUAL_INPUT_DATA.ParameterName = "ACTUAL_INPUT_DATA";

                    dbCommand.Parameters.Add(oraPara_TEST_REPORT_ID);
                    dbCommand.Parameters.Add(oraPara_STEPS_ID);
                    dbCommand.Parameters.Add(oraPara_BEGIN_TIME);
                    dbCommand.Parameters.Add(oraPara_RUNNING_RESULT);
                    dbCommand.Parameters.Add(oraPara_RETURN_VALUES);
                    dbCommand.Parameters.Add(oraPara_RUNNING_RESULT_INFO);
                    dbCommand.Parameters.Add(oraPara_DATA_SUMMARY_ID);
                    dbCommand.Parameters.Add(oraPara_INPUT_VALUE_SETTING);
                    dbCommand.Parameters.Add(oraPara_ACTUAL_INPUT_DATA);

                    iCurrentUpdateRecords = dbCommand.ExecuteNonQuery();
                    iTotalUpdatedRecord += iCurrentUpdateRecords;
                }
                dbTrans.Commit();
                Logger.Info("InsertTestStepResultForKeyCompare", string.Format("{0} records are inserted", iTotalUpdatedRecord));

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertTestStepResultForKeyCompare", strError = string.Format("Exception:[{0}]", e.Message), e);
                if (dbTrans != null)
                {
                    try
                    {
                        dbTrans.Rollback();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("InsertTestStepResultForKeyCompare", ex.Message, ex);
                    }
                }
                if (dbCnn != null)
                {
                    try
                    {
                        dbCnn.Close();
                    }
                    catch (Exception)
                    {

                    }

                }
                return false;
            }
#else
            try
            {
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                ///short iSuccessId, List<KeyValuePair<string, string>> lstObjectNameAndValues, 
                //long? dATA_SUMMARY_ID, string strObjectNameIdx,
                //string strRunningError,
                //ref string strError)
                bool isOk = clnt.InsertTestStepResultForKeyCompare(lRptId, stpId, 
                    beginTime, endTime,
                    iSuccessId, lstObjectNameAndValues,
                    dATA_SUMMARY_ID, strObjectNameIdx, strRunningError, ref strError);
                if (!isOk)
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = "can't insert Captured data, and no error returns.";
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]\r\n{1}", e.Message, e.StackTrace);
                return false;
            }
#endif
            finally
            {
                Logger.logEnd("InsertTestStepResultForKeyCompare");
            }
        }
#if !_forWebSvc
        public static int UpdateAndInsertList(B_TEST_REPORT_STEPS updateObj,
            List<B_TEST_REPORT_STEPS> lstInsertTestStepReport,
            string strObjName,
            string strDBIdx)
#else
            public int UpdateAndInsertList(B_TEST_REPORT_STEPS updateObj, 
            List<B_TEST_REPORT_STEPS> lstInsertTestStepReport,
            string strObjName,
            string strDBIdx)
#endif
        {
#if !_forWebClient
            Logger.Info("UpdateAndInsertList", string.Format("Update:[{0}], InsertCount:[{1}] objectName:[{2}]",
                updateObj.RETURN_VALUES,
                lstInsertTestStepReport == null ? 0 : lstInsertTestStepReport.Count,
                strObjName));
            //int iSaveCnt = 0;
            try
            {
                MarsEntities objEntitis = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                DbSet<T_TEST_REPORT_STEPS> dbQuery = objEntitis.Set<T_TEST_REPORT_STEPS>();
                T_TEST_REPORT_STEPS objDBObj = dbQuery.Where(p => p.TEST_REPORT_STEP_ID == updateObj.TEST_REPORT_STEP_ID).Single();
                dbQuery.Attach(objDBObj);
                /// copy to objDBObj
                /// 
                if (objDBObj != null)
                {
                    if (updateObj.RETURN_VALUES != null)
                    {
                        if (updateObj.RETURN_VALUES.Length > 256)
                        {
                            Logger.Warnning("UpdateAndInsertList", string.Format("Data to save is longer than 256,which is [{0}], only the first 256 is kept", updateObj.RETURN_VALUES.Length));
                            updateObj.RETURN_VALUES = updateObj.RETURN_VALUES.Substring(0, 255);
                        }
                    }
                    updateObj.CopyObjTo(objDBObj);
                }
                else
                {
                    Logger.Warnning("UpdateAndInsertList", string.Format("Can't find data from databbase with ID:[{0}]", updateObj.TEST_REPORT_STEP_ID));
                }
                // get keys for insert lst
                for (int i = 0; i < lstInsertTestStepReport.Count; i++)
                {
                    B_TEST_REPORT_STEPS objItem = lstInsertTestStepReport[i];

                    if (objItem == null) continue;
                    //if (objItem.RETURN_VALUES!=null)
                    //{
                    //    if (objItem.RETURN_VALUES.Length > 64)
                    //    {
                    //        Logger.Warnning("UpdateAndInsertList-objItem", string.Format("Data to save is longer than 64,which is [{0}]-value to be cut:[{1}], only the first 63 is kept", objItem.RETURN_VALUES.Length, objItem.RETURN_VALUES));
                    //        objItem.RETURN_VALUES = updateObj.RETURN_VALUES.Substring(1, 63);
                    //    }
                    //    else
                    //    {
                    //        objItem.RETURN_VALUES = updateObj.RETURN_VALUES;
                    //    }
                    //}

#if db4SQL
                    System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
                    ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

                    long tmpID = (long)objEntitis.GETNEXT_VAL(CNST_SEQ_IDNAME, outparam);
                    objItem.TEST_REPORT_STEP_ID = long.Parse(outparam.Value.ToString());
                    objItem.INPUT_VALUE_SETTING = string.Format("{0}_{1}", strObjName, i + 1);
                    Logger.Info("\t", "stackInfo Len:"+(string.IsNullOrEmpty(objItem.STACKINFO)?"0": objItem.STACKINFO.Length+""));
                    if (objItem.STACKINFO != null)
                    {
                        byte[] b = Encoding.ASCII.GetBytes(objItem.STACKINFO);
                        Logger.Info("\t", $"byte array len:[{b.Length}]");
                        if (b.Length >= 1024)
                        {
                            Array.Resize(ref b, 1024);
                            objItem.STACKINFO = "to long";
                        }
                        //objItem.STACKINFO = Encoding.ASCII.GetString(b);

                    }
                    dbQuery.Add(objItem.ToEntity());
                }

                objEntitis.SaveChanges();
                return 1;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateAndInsertList", string.Format("Exceptions:[{0}]", e.Message), e);
                if (e is DbEntityValidationException)
                {
                    try
                    {
                        var ex = e as DbEntityValidationException;
                        var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);

                        // Join the list to a single string.
                        var fullErrorMessage = string.Join("; ", errorMessages);

                        // Combine the original exception message with the new one.
                        var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                        Logger.Error("\tUpdateAndInsertList", string.Format("Exceptions:[{0}]", exceptionMessage));
                    }
                    catch (Exception x)
                    {

                    }
                }
                return -1;
            }
            finally
            {
                Logger.logEnd("UpdateAndInsertList");
            }
#else
            Logger.logBegin("UpdateAndInsertList", $"objName|{strObjName}|dbIdx|{strDBIdx}");
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            bool isOk = true;
            string strError = "";
            int iCnt = clnt.TestReportStps_UpdateAndInsertList(updateObj, lstInsertTestStepReport, strObjName, ref isOk, ref strError);
            if (!isOk)
            {
                if (string.IsNullOrEmpty(strError)) strError = "can't Update or insert test rest and no Error returns.";
                return -1;
            }
            return 1;
#endif
        }

        //public Dictionary<long, List<T_TEST_REPORT_STEPSDTO>> GetDataByStoryboardIds(short sTestMode,List<long> lstStoryBoardIds, ref string strError, MarsEntities objCntx=null)
        //{
        //    Logger.logBegin("GetDataByStoryboardIds",string.Format("Data List to Get:[{0}]", lstStoryBoardIds));
        //    try
        //    {
        //        MarsEntities objDataCntx = objCntx==null?BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx): objCntx;

        //        var q = from stp in objDataCntx.T_TEST_REPORT_STEPS
        //                from rpt in objDataCntx.T_TEST_REPORT
        //                where
        //                    stp.TEST_REPORT_ID == rpt.TEST_REPORT_ID
        //                && rpt.TEST_MODE == sTestMode ;

        //    }
        //    catch (Exception e)
        //    {

        //    }
        //    finally {
        //        Logger.logEnd("GetDataByStoryboardIds");
        //    }
        //}

        public void CopyObjTo(T_TEST_REPORT_STEPS objDes)
        {
            if (objDes == null) return;
            objDes.ACTUAL_INPUT_DATA = this.ACTUAL_INPUT_DATA;
            objDes.BEGIN_TIME = this.BEGIN_TIME;
            objDes.DATA_ORDER = this.DATA_ORDER;
            objDes.DATA_SUMMARY_ID = this.DATA_SUMMARY_ID;
            objDes.END_TIME = this.END_TIME;
            objDes.INPUT_VALUE_SETTING = this.INPUT_VALUE_SETTING;
            objDes.RETURN_VALUES = this.RETURN_VALUES;
            objDes.RUNNING_RESULT = this.RUNNING_RESULT;
            objDes.RUNNING_RESULT_INFO = this.RUNNING_RESULT_INFO;
            objDes.STEPS_ID = this.STEPS_ID;
            objDes.TEST_REPORT_ID = this.TEST_REPORT_ID;
            objDes.TEST_REPORT_STEP_ID = this.TEST_REPORT_STEP_ID;
            objDes.INFO_PIC = this.INFO_PIC;

            objDes.ADVICE = this.ADVICE;
            objDes.STACKINFO = this.STACKINFO;
        }
        public void cloneFrom(T_TEST_REPORT_STEPSDTO objDto)
        {
            if (objDto == null) return;
            this.ACTUAL_INPUT_DATA = objDto.ACTUAL_INPUT_DATA;
            this.BEGIN_TIME = objDto.BEGIN_TIME;
            this.DATA_ORDER = objDto.DATA_ORDER;
            this.DATA_SUMMARY_ID = objDto.DATA_SUMMARY_ID;
            this.END_TIME = objDto.END_TIME;
            this.INPUT_VALUE_SETTING = objDto.INPUT_VALUE_SETTING;
            this.RETURN_VALUES = objDto.RETURN_VALUES;
            this.RUNNING_RESULT = objDto.RUNNING_RESULT;
            this.RUNNING_RESULT_INFO = objDto.RUNNING_RESULT_INFO;
            this.STEPS_ID = objDto.STEPS_ID;
            this.TEST_REPORT_ID = objDto.TEST_REPORT_ID;
            this.TEST_REPORT_STEP_ID = objDto.TEST_REPORT_STEP_ID;
            this.INFO_PIC = objDto.INFO_PIC;

            this.ADVICE = objDto.ADVICE;
            this.STACKINFO = objDto.STACKINFO;
        }

        public B_TEST_REPORT_STEPS(T_TEST_REPORT_STEPSDTO objSrc)
        {
            this.ACTUAL_INPUT_DATA = objSrc.ACTUAL_INPUT_DATA;
            this.BEGIN_TIME = objSrc.BEGIN_TIME;
            this.DATA_ORDER = objSrc.DATA_ORDER;
            this.DATA_SUMMARY_ID = objSrc.DATA_SUMMARY_ID;
            this.END_TIME = objSrc.END_TIME;
            this.INPUT_VALUE_SETTING = objSrc.INPUT_VALUE_SETTING;
            this.RETURN_VALUES = objSrc.RETURN_VALUES;
            this.RUNNING_RESULT = objSrc.RUNNING_RESULT;
            this.RUNNING_RESULT_INFO = objSrc.RUNNING_RESULT_INFO;
            this.STEPS_ID = objSrc.STEPS_ID;
            this.TEST_REPORT_ID = objSrc.TEST_REPORT_ID;
            this.TEST_REPORT_STEP_ID = objSrc.TEST_REPORT_STEP_ID;
            this.INFO_PIC = objSrc.INFO_PIC;
            this.ADVICE = objSrc.ADVICE;
            this.STACKINFO = objSrc.STACKINFO;
        }

        public B_TEST_REPORT_STEPS() : base()
        {

        }

    }
}
