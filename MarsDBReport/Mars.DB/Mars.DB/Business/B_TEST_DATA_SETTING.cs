using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Mars.Business
{
    public class B_TEST_DATA_SETTING : TEST_DATA_SETTINGDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_TEST_DATA_SETTING));

        internal const string TABLE_NAME = "TEST_DATA_SETTING";
        internal const string ID_SEQ = "TEST_DATA_SETTING_SEQ";

        public const string CNST_ENHANCE_PEG_RUNTIME_PREFIX = "MultiplePane:Runtime:";
        public static long GetNewId(MarsEntities objDBCntx = null)
        {
            return BoHelper.GetIdBySeqName(ID_SEQ, objDBCntx);
        }
        public static B_TEST_DATA_SETTING ToBO(TEST_DATA_SETTING entity)
        {
            if (entity == null) return null;

            var bo = new B_TEST_DATA_SETTING();

            bo.DATA_SETTING_ID = entity.DATA_SETTING_ID;
            bo.STEPS_ID = entity.STEPS_ID;
            bo.LOOP_ID = entity.LOOP_ID;
            bo.DATA_VALUE = entity.DATA_VALUE;
            bo.VALUE_OR_OBJECT = entity.VALUE_OR_OBJECT;
            bo.DESCRIPTION = entity.DESCRIPTION;
            bo.DATA_SUMMARY_ID = entity.DATA_SUMMARY_ID;
            bo.DATA_DIRECTION = entity.DATA_DIRECTION;
            bo.VERSION = entity.VERSION;
            bo.CREATE_TIME = entity.CREATE_TIME;
            bo.POOL_ID = entity.POOL_ID;

            //entity.OnDTO(bo);

            return bo;
        }
#if !_forWebSvc
        public static IList<KeyValuePair<long?, TEST_DATA_SETTINGDTO>> GetTestDataByTestCaseIDAndDataSetId(long lTSId, long lDsetId,
            string strDBIdx)// = MarsEntitiesExtends.cnst_default_dbName)
#else
        public IList<KeyValuePair<long?, TEST_DATA_SETTINGDTO>> GetTestDataByTestCaseIDAndDataSetId(long lTSId, long lDsetId,
            string strDBIdx)// = MarsEntitiesExtends.cnst_default_dbName)
#endif
        {
            Logger.Info("GetTestDataByTestCaseIDAndDataSetId", string.Format("Test Case ID:[{0}], Data Summary Id:[{1}]", lTSId, lDsetId));
            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(true, strDBIdx);
#if v_16AndUp
            var query = (from stp in objEntities.T_TEST_STEPS
                         from dataSetting in objEntities.TEST_DATA_SETTING
                         where dataSetting.STEPS_ID == stp.STEPS_ID
                         && stp.TEST_CASE_ID == lTSId
                         && dataSetting.DATA_SUMMARY_ID == lDsetId
                         //&& testObj.OBJECT_ID == testCaseStep.OBJECT_ID
                         select new
                         {
                             stpId = stp.STEPS_ID,
                             data = dataSetting
                         }).AsEnumerable()
                         .Select(o => new KeyValuePair<long?, TEST_DATA_SETTINGDTO>(o.stpId, o.data.ToDTO()))
                         .ToList();
            return query;
#endif
        }

        internal static IList<KeyValuePair<TEST_DATA_SETTINGDTO, string>> GetAssignedTestDataByTestCaseID(long tEST_CASE_ID, long dATA_SUMMARY_ID, 
            string strDBIdx ) //= MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.Info("GetAssignedTestDataByTestCaseID", string.Format("Test Case ID:[{0}], Data Summary Id:[{1}]", tEST_CASE_ID, dATA_SUMMARY_ID));
            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(true,strDBIdx);
#if v_16AndUp
#if !v_useNameId
            var query = (
                         from testCaseStep in objEntities.T_TEST_STEPS
                         join testObj in objEntities.V_OBJECT_SNAPSHOT on 
                            testCaseStep.OBJECT_ID equals  testObj.OBJECT_ID                            
                         into TCSTO
                         from tcsTO in TCSTO.DefaultIfEmpty()
                         from dataSetting in objEntities.TEST_DATA_SETTING
                         where dataSetting.STEPS_ID == testCaseStep.STEPS_ID
                         && testCaseStep.TEST_CASE_ID == tEST_CASE_ID
                         && dataSetting.DATA_SUMMARY_ID == dATA_SUMMARY_ID
                         //&& testObj.OBJECT_ID == testCaseStep.OBJECT_ID
                         select new
                         {
                             Data = dataSetting,
                             ObjectReg = tcsTO
                         }
                         ).OrderBy(p => p.Data.LOOP_ID);
            Logger.Info("--SQL--",query.ToString());
#else
            var query = (
                         from testCaseStep in objEntities.T_TEST_STEPS
                         join testObj in objEntities.T_OBJECT_NAMEINFO on
                            testCaseStep.OBJECT_NAME_ID equals testObj.OBJECT_NAME_ID
                         into TCSTO
                         from tcsTO in TCSTO.DefaultIfEmpty()
                         from dataSetting in objEntities.TEST_DATA_SETTING
                         where dataSetting.STEPS_ID == testCaseStep.STEPS_ID
                         && testCaseStep.TEST_CASE_ID == tEST_CASE_ID
                         && dataSetting.DATA_SUMMARY_ID == dATA_SUMMARY_ID
                         //&& testObj.OBJECT_ID == testCaseStep.OBJECT_ID
                         select new
                         {
                             Data = dataSetting,
                             ObjectReg = tcsTO
                         }
                         ).OrderBy(p => p.Data.LOOP_ID);
            Logger.Info("--SQL--", query.ToString());
#endif
#else
            var query = (from dataSetting in objEntities.TEST_DATA_SETTING
                         from testCaseStep in objEntities.T_TEST_STEPS
                         join testObj in objEntities.T_REGISTED_OBJECT on testCaseStep.OBJECT_ID equals testObj.OBJECT_ID                          
                         into TCSTO
                         from tcsTO in TCSTO.DefaultIfEmpty()
                         where dataSetting.STEPS_ID == testCaseStep.STEPS_ID
                         && testCaseStep.TEST_CASE_ID == tEST_CASE_ID
                         && dataSetting.DATA_SUMMARY_ID == dATA_SUMMARY_ID
                         //&& testObj.OBJECT_ID == testCaseStep.OBJECT_ID
                         select new {
                             Data = dataSetting,
                             ObjectReg = tcsTO  
                         }
                         ).OrderBy(p=>p.Data.LOOP_ID);
#endif
            List<KeyValuePair<TEST_DATA_SETTINGDTO, string>> lstDataDto = new List<KeyValuePair<TEST_DATA_SETTINGDTO, string>>();

            foreach (var objQueryResult in query)
            {
#if v_useNameId
                lstDataDto.Add(new KeyValuePair<TEST_DATA_SETTINGDTO, string>(TEST_DATA_SETTINGAssembler.ToDTO(objQueryResult.Data),
                    ((objQueryResult.ObjectReg == null) || (objQueryResult.ObjectReg.OBJECT_NAME_ID < 0)) ? string.Format("{0}_{1}", objQueryResult.Data.STEPS_ID, objQueryResult.Data.LOOP_ID) : objQueryResult.ObjectReg.OBJECT_HAPPY_NAME));
#else
                /// some keywords no requiring parameters results in ObjectReg of objQueryResult is null. 
                /// To make it is searchable by services, combined test step id and loop id is taken as new object name
                lstDataDto.Add(new KeyValuePair<TEST_DATA_SETTINGDTO, string>(TEST_DATA_SETTINGAssembler.ToDTO(objQueryResult.Data), objQueryResult.ObjectReg == null? string.Format("{0}_{1}", objQueryResult.Data.STEPS_ID,objQueryResult.Data.LOOP_ID) :objQueryResult.ObjectReg.OBJECT_HAPPY_NAME));
#endif
            }

            Logger.Info("GetAssignedTestDataByTestCaseID", string.Format("returns [{0}] records", lstDataDto == null ? 0 : lstDataDto.Count));
            return lstDataDto;

        }

        public bool Insert(string strDBIdx,ref string strError, MarsTransactionMgr objTrans)
        {
            if (objTrans == null)
                return Insert(strDBIdx,ref strError);
            return Insert(strDBIdx,ref strError, objTrans.CurrentDBContext);
        }
        public bool Insert(string strDBIdx,ref string strError, MarsEntities objDBCntx = null)
        {
            Logger.Info("Insert", string.Format("stepid:[{0}],value:[{1}]", STEPS_ID, DATA_VALUE));
            try
            {
                MarsEntities dbCntx = objDBCntx ?? BoHelper.GetMarsEntitiesInstance(true, strDBIdx);
                dbCntx.Set<TEST_DATA_SETTING>();
                dbCntx.TEST_DATA_SETTING.Add(this.ToEntity());

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Insert", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        /// <summary>
        /// 获得test case中尚未存在存入的 test case Id
        /// </summary>
        /// <param name="lstStepIds"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public List<long> getNewRecordsFrom(string strDBIdx, List<long> lstStepIds, long dataSummaryId, ref string strError, ref bool isOk)
        {
            Logger.Info("getNewRecordsFrom", string.Format("List Test stepsids:[{0}] data summary id:[{1}]", lstStepIds, dataSummaryId));

            try
            {
                MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(true,strDBIdx);

                var q = objEntities.TEST_DATA_SETTING.Where(p => p.DATA_SUMMARY_ID == dataSummaryId);
                List<long> qStpIds = q.Select(p => p.STEPS_ID).ToList();
                List<long> lst = lstStepIds.Where(p => !qStpIds.Contains(p)).ToList();

                isOk = true;
                return lst;
            }
            catch (Exception e)
            {
                Logger.Error("getNewRecordsFrom", strError = string.Format("Exception:[{0}]\r\nstackTrace:[{1}]", e.Message, e.StackTrace));
                isOk = false;
                return null;
            }
        }

        public static bool deleteReordsByTestCaseId(long tEST_CASE_ID, DbCommand dbCmmd, ref string strError)
        {
            string strSql = string.Format("DELETE TEST_DATA_SETTING A WHERE A.STEPS_ID IN (SELECT STEPS_ID FROM T_TEST_STEPS WHERE TEST_CASE_ID={0})", tEST_CASE_ID);
            try
            {
                dbCmmd.CommandText = strSql;
                dbCmmd.Parameters.Clear();
                dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("deleteReordsByTestCaseId", strError = string.Format("Exception:[{0}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public static bool deleteReordsByTestCaseId(MarsEntities dbCntx, long testcaseId, long datasummaryId, ref string strError)
        {
            try
            {
                var lstDataSettings = from q in dbCntx.T_TEST_STEPS
                                      from d in dbCntx.TEST_DATA_SETTING
                                      where q.TEST_CASE_ID == testcaseId
                                      && q.STEPS_ID == d.STEPS_ID
                                      && d.DATA_SUMMARY_ID == datasummaryId
                                      select d;
                foreach (var itm in lstDataSettings)
                {
                    if (itm != null)
                        dbCntx.TEST_DATA_SETTING.Remove(itm);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("deleteReordsByTestCaseId", strError = string.Format("Exception:[{0}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public Dictionary<T_TEST_DATA_SUMMARYDTO, List<TEST_DATA_SETTINGDTO>> FetchDataForTestCase(string strDBIdx, IEnumerable<long> lstTestStepIds, ref string strError, ref bool isOk)
        {
            try
            {
                MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(true, strDBIdx);
                var q = from stp in objEntities.TEST_DATA_SETTING
                        where lstTestStepIds.Contains(stp.STEPS_ID)
                        from d in objEntities.T_TEST_DATA_SUMMARY
                        where stp.DATA_SUMMARY_ID == d.DATA_SUMMARY_ID
                        select new
                        {
                            dtSet = d,
                            stpDt = stp
                        };
                Dictionary<T_TEST_DATA_SUMMARY, List<TEST_DATA_SETTING>> dicEntities = q.GroupBy(x => x.dtSet, p => p.stpDt).ToDictionary(x => x.Key, c => c.ToList());
                if (dicEntities == null)
                {
                    isOk = true;
                    return null;
                }
                Dictionary<T_TEST_DATA_SUMMARYDTO, List<TEST_DATA_SETTINGDTO>> dicRslt = new Dictionary<T_TEST_DATA_SUMMARYDTO, List<TEST_DATA_SETTINGDTO>>();
                foreach (T_TEST_DATA_SUMMARY itmEntity in dicEntities.Keys)
                {
                    dicRslt.Add(T_TEST_DATA_SUMMARYAssembler.ToDTO(itmEntity), TEST_DATA_SETTINGAssembler.ToDTOs(dicEntities[itmEntity]));
                }
                isOk = true;
                return dicRslt;
            }
            catch (Exception e)
            {
                Logger.Error("FetchDataForTestCase", strError = string.Format("Exception:[{0}]\r\nstackTrace:[{1}]", e.Message, e.StackTrace));
                isOk = false;
                return null;
            }
        }
        /// <summary>
        /// 首先检查是否有数据需要update，然后就需要更新的进行更新
        /// </summary>
        /// <param name="lstTobeUpdatedBTest"></param>
        /// <param name="dataSetId"></param>
        /// <param name="objTransMars">数据库连续信息，用来进行事务管理</param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public bool CheckAndUpdate(List<B_TEST_DATA_SETTING> lstTobeUpdatedBTest, long dataSetId, MarsTransactionMgr objTransMars, ref string strError)
        {
            Logger.Info("CheckAndUpdate", string.Format("Check to be updated records,count:[{0}], dataSetId:[{1}]", lstTobeUpdatedBTest == null ? 0 : lstTobeUpdatedBTest.Count, dataSetId));
            if (objTransMars == null)
            {
                Logger.Error("CheckAndUpdate", strError = "objTransMars==null, No DB information.");
                return false;
            }
            try
            {
                MarsEntities objDbCntx = objTransMars.CurrentDBContext;
                List<TEST_DATA_SETTING> lstDtSett = new List<TEST_DATA_SETTING>();
                foreach (var b in lstTobeUpdatedBTest)
                {
                    lstDtSett.Add(TEST_DATA_SETTINGAssembler.ToEntity(b));
                }
                var chckedLst = objDbCntx.TEST_DATA_SETTING.Where(p => lstDtSett.Any(p1 => p1.DATA_SUMMARY_ID == p.DATA_SUMMARY_ID && p1.DATA_VALUE != p.DATA_VALUE && p1.STEPS_ID == p.STEPS_ID && p.LOOP_ID == 1));
                if (chckedLst == null)
                {
                    Logger.Info("CheckAndUpdate", "no records needs to be updated");
                    return true;
                }
                foreach (var itm2BUpdated in chckedLst.ToList())
                {
                    if (itm2BUpdated == null) continue;
                    var objNew = lstTobeUpdatedBTest.Where(p => p.STEPS_ID == itm2BUpdated.STEPS_ID).FirstOrDefault();
                    if (objNew == null) continue;
                    itm2BUpdated.DATA_VALUE = objNew.DATA_VALUE;
                    objDbCntx.TEST_DATA_SETTING.Attach(itm2BUpdated);
                    var entry = objDbCntx.Entry(itm2BUpdated);
                    entry.Property(e => e.DATA_VALUE).IsModified = true;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CheckAndUpdate", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public long InsertDataRec(long lStepId, long loop_id, string dataValue, int iValueOrObjMark, string description, long lDataSetId, int iDataDirection, long iPoolId,
            MarsEntities dbCntx,
            ref bool isOk, ref string strError)
        {
            Logger.logBegin("InsertDataRec", string.Format("lStepId:[{0}] loopId:[{1}] datavalue:[{2}] iValueOrObj:[{3}] Desc:[{4}] DatasetId:[{5}] DataDirection:[{6}] PoolId:[{7}]",
                lStepId, loop_id, dataValue, iValueOrObjMark, description, lDataSetId, iDataDirection, iPoolId));
            try
            {
                TEST_DATA_SETTINGDTO objDataRecDto = new TEST_DATA_SETTINGDTO();
                objDataRecDto.CREATE_TIME = DateTime.Now;
                objDataRecDto.DATA_DIRECTION = (short)iDataDirection;
                objDataRecDto.DATA_SETTING_ID = BoHelper.GetIdBySeqName(ID_SEQ, dbCntx);
                objDataRecDto.DATA_SUMMARY_ID = lDataSetId;
                objDataRecDto.DATA_VALUE = dataValue;
                objDataRecDto.DESCRIPTION = description;
                objDataRecDto.LOOP_ID = loop_id;
                objDataRecDto.POOL_ID = iPoolId;
                objDataRecDto.STEPS_ID = lStepId;
                objDataRecDto.VALUE_OR_OBJECT = (short)iValueOrObjMark;
                objDataRecDto.VERSION = 1;

                dbCntx.Set<TEST_DATA_SETTING>();
                dbCntx.TEST_DATA_SETTING.Add(objDataRecDto.ToEntity());

                isOk = true;
                return objDataRecDto.DATA_SETTING_ID;
            }
            catch (Exception e)
            {
                Logger.Error("InsertDataRec", strError = string.Format("Exception:[{0}] stackTrace\r\n{1} \r\nInnerException:[{2}] \r\nInner's Inner:[{3}]", e.Message, e.StackTrace,
                    e.InnerException == null ? "" : e.InnerException.Message,
                    e.InnerException == null ? "" : e.InnerException.InnerException == null ? "" : e.InnerException.InnerException.Message));
                isOk = false;
                return -1;
            }
            finally
            {
                Logger.logEnd("InsertDataRec");
            }
        }

        public bool UpdateValueAndDirection(long dATA_SETTING_ID, string strValue, short dataDirection, MarsEntities dbCntx, ref string strError)
        {
            try
            {
                dbCntx.Set<TEST_DATA_SETTING>();
                var testdatasetting = dbCntx.TEST_DATA_SETTING.Where(p => p.DATA_SETTING_ID == dATA_SETTING_ID).FirstOrDefault();
                //(from q in dbCntx.TEST_DATA_SETTING
                //                   where q.DATA_SETTING_ID == dATA_SETTING_ID
                //                   select q).FirstOrDefault();
                if (testdatasetting == null) //it could be deleted by others
                {
                    return true;
                }

                dbCntx.TEST_DATA_SETTING.Attach(testdatasetting);
                testdatasetting.DATA_DIRECTION = dataDirection;
                testdatasetting.DATA_VALUE = strValue;
                dbCntx.Entry(testdatasetting).State = System.Data.EntityState.Modified;
                //dbCntx.TEST_DATA_SETTING.Attach(testdatasetting);
                //var entry = dbCntx.Entry(testdatasetting);
                //entry.Property(e => e.DATA_VALUE).IsModified = true;
                //entry.Property(e => e.DATA_DIRECTION).IsModified = true;

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateValueAndDirection", strError = string.Format("Excecption:[{0}]", e.Message, e), e);
                return false;
            }
        }

        public bool deleteRecords(List<B_TEST_DATA_SETTING> lstUnUsedData, MarsEntities dbCntx, ref string strError)
        {
            try
            {
                dbCntx.Set<TEST_DATA_SETTING>();
                foreach (var itm in lstUnUsedData)
                {
                    if (itm == null) continue;
                    var itmDb = (from v in dbCntx.TEST_DATA_SETTING
                                 where v.DATA_SETTING_ID == itm.DATA_SETTING_ID
                                 select v).FirstOrDefault();
                    if (itmDb == null) continue;

                    dbCntx.TEST_DATA_SETTING.Remove(itmDb);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("deleteRecords", strError = string.Format("Exception [{0}]", e.Message), e);
                return false;
            }
        }

        public static bool IsRuntimeObj(TEST_DATA_SETTINGDTO stepData)
        {
            if (stepData == null) return false;
            if (string.Compare(CNST_ENHANCE_PEG_RUNTIME_PREFIX, stepData.DATA_VALUE, true) == 0) return true;
            return false;
        }

        public B_TEST_DATA_SETTING createDataRec(long stepId, int loopId, string dataSet1, int valueOrobj,
            string desc, long dataSummaryId, long dataDirection, int poolId,
            MarsEntities dbCntx,
            ref bool isOk, ref string strError)
        {
            B_TEST_DATA_SETTING rslt = new B_TEST_DATA_SETTING()
            {
                STEPS_ID = stepId,
                LOOP_ID = loopId,
                DATA_VALUE = dataSet1,
                VALUE_OR_OBJECT = (short)valueOrobj,
                DESCRIPTION = desc,
                DATA_SUMMARY_ID = dataSummaryId,
                DATA_DIRECTION = (short)dataDirection,
                POOL_ID = poolId
            };
            try
            {
                rslt.DATA_SETTING_ID = GetNewId(dbCntx);

                dbCntx.Set<TEST_DATA_SETTING>();
                dbCntx.TEST_DATA_SETTING.Add(rslt.ToEntity());

                isOk = true;
                return rslt;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("B_TEST_DATA_SETTING.createDataRec exception:[{0}]", e.Message);
                return null;
            }

        }

        /// <summary>
        /// 场景：
        /// 假定一个test case 有N个dataset，那么如果对该test case就行step的增减，必然造成其他的test dataset 在运行过程中出现数据错误
        /// 那么，用户可以选择某个指定的dataset 中的特定行 然后copy到其他的dataset中的对应行
        /// </summary>
        /// <param name="currentDataSheetId"></param>
        /// <param name="lstStepsData"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public static bool CopyDataSettings(string strDBIdx, 
            long currentDataSheetId, List<B_TEST_STEPS> lstStepsData, ref string strError)
        {
            Logger.Info("CopyDataSettings", string.Format("Try to copy datasettings by datasheetId:[{0}] dataLenght:[{1}]",
                 currentDataSheetId, lstStepsData == null ? 0 : lstStepsData.Count));
            DbTransaction trans = null;
            try
            {
                if (lstStepsData == null)
                {
                    strError = "No data available. ";
                    Logger.Error("CopyDataSettings", strError);
                    return false;
                }
                long lTestCaseId = lstStepsData[0].TEST_CASE_ID ?? -1;
                if (lTestCaseId <= 0)
                {
                    Logger.Error("CopyDataSettings", strError = "No available test case Id or test case id is null/-1.");
                    return false;
                }
                MarsEntities dbContext = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if (dbContext.Database.Connection.State != System.Data.ConnectionState.Open)
                    dbContext.Database.Connection.Open();
                trans = dbContext.Database.Connection.BeginTransaction();


                ///steps:
                /// 1, delete old summary data ,but the current data set information
                /// 2, insert new data records 
                /// 
                /// 1, delete old summary data ,but the current data set information
                long[] lstStpsId = lstStepsData.Select(p => p.STEPS_ID).ToArray();
                long lUpdatedRec = -1;
                /// 1, delete old summary data ,but the current data set information
                /// 
                string strSqlDelOldValues = string.Format(@"DELETE TEST_DATA_SETTING WHERE STEPS_ID IN ({0}) AND DATA_SUMMARY_ID<>{1}", string.Join(",", lstStpsId), currentDataSheetId);
                using (DbCommand dbCmmd = dbContext.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlDelOldValues;
                    lUpdatedRec = dbCmmd.ExecuteNonQuery();
                    Logger.Info("CopyDataSettings", string.Format("Deleted records:{0}", lUpdatedRec));
                }

                string strSqlInsertDataSet = string.Format(@"INSERT INTO TEST_DATA_SETTING(DATA_SETTING_ID,STEPS_ID, LOOP_ID, DATA_VALUE,VALUE_OR_OBJECT, DESCRIPTION, DATA_SUMMARY_ID,DATA_DIRECTION, VERSION, CREATE_TIME,POOL_ID)
                SELECT TEST_DATA_SETTING_SEQ.NEXTVAL,DS.STEPS_ID, DS.LOOP_ID, DS.DATA_VALUE, DS.VALUE_OR_OBJECT, DS.DESCRIPTION, TARGET_DS.DATA_SUMMARY_ID,DS.DATA_DIRECTION, DS.VERSION, SYSDATE,DS.POOL_ID
                FROM TEST_DATA_SETTING DS LEFT JOIN (
                    SELECT DISTINCT DATA_SUMMARY_ID,STP.STEPS_ID
                    FROM REL_TC_DATA_SUMMARY TCDS, T_TEST_STEPS STP
                    WHERE STP.STEPS_ID IN ({0}) AND ((TCDS.TEST_CASE_ID=STP.TEST_CASE_ID) AND TCDS.DATA_SUMMARY_ID<>{1})
                ) TARGET_DS
                ON DS.STEPS_ID = TARGET_DS.STEPS_ID
                WHERE  DS.STEPS_ID IN ({0}) AND DS.DATA_SUMMARY_ID={1} ", string.Join(",", lstStpsId), currentDataSheetId);
                using (DbCommand dbCmmd = dbContext.Database.Connection.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlInsertDataSet;
                    lUpdatedRec = dbCmmd.ExecuteNonQuery();
                    Logger.Info("CopyDataSettings", string.Format("Inserted records:{0}", lUpdatedRec));
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

                Logger.Error("CopyDataSettings", strError = string.Format("Exception when updata datasettings:[{0}],\r\nError stack trace:[{1}]",
                    e.Message, e.StackTrace), e);
                return false;
            }
        }

        public bool CheckAndUpdateOneRecord(long lstpId, long lDSId, long lPool, string currentData, MarsEntities dbcntx, ref string strError)
        {
            Logger.logBegin("CheckAndUpdateOneRecord", string.Format("StepId:[{0}] Data summary Id:[{1}], Data PoolId[{2}]", lstpId, lDSId, lPool));
            try
            {
                var dataRec = (from q in dbcntx.TEST_DATA_SETTING
                               where q.STEPS_ID == lstpId
                               && q.LOOP_ID == 1
                               && q.DATA_SUMMARY_ID == lDSId
                               select q).FirstOrDefault();
                bool isOk = false;
                if (dataRec == null)
                {
                    InsertDataRec(lstpId, 1, currentData, 2, "AUTO CREATE", lDSId, 1, lPool, dbcntx, ref isOk, ref strError);
                    if (!isOk) return false;
                    return true;
                }
                else
                {

                    if (string.Compare(currentData ?? "", dataRec.DATA_VALUE ?? "") == 0)
                        return true;
                    dbcntx.Set<TEST_DATA_SETTING>();

                    dbcntx.TEST_DATA_SETTING.Attach(dataRec);
                    var tr = dbcntx.Entry(dataRec);
                    dataRec.DATA_VALUE = currentData;

                    tr.Property(p => p.DATA_VALUE).IsModified = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("CheckAndUpdateOneRecord", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("CheckAndUpdateOneRecord");
            }
        }

        public static TEST_DATA_SETTINGDTO CreateNewRecord(long sTEPS_ID, int loopId, string dataValue, int valueOrObj, string desc, long dATA_SUMMARY_ID, int data_direction, long poolid, DbCommand dbCmmd, ref bool isOk, ref string strError)
        {
            TEST_DATA_SETTINGDTO rslt = new TEST_DATA_SETTINGDTO();
            try
            {
                //Logger.Info("CreateNewRecord", string.Format(@"STEP ID:[{0}] LOOPID:[{1}] dataValue:[{2}] dATA_SUMMARY_ID [{3}]", sTEPS_ID, loopId, dataValue, dATA_SUMMARY_ID));
                string strsqlMerge = "MERGE INTO TEST_DATA_SETTING A USING (SELECT " + sTEPS_ID + " AS STEPS_ID, " + dATA_SUMMARY_ID + " AS DATA_SUMMARY_ID, " + loopId + " AS LOOP_ID FROM DUAL) T "
                                     + " ON (A.STEPS_ID=T.STEPS_ID AND A.DATA_SUMMARY_ID=T.DATA_SUMMARY_ID AND A.LOOP_ID=T.LOOP_ID)"
                                     + " WHEN MATCHED THEN "
                                     + "   UPDATE SET A.DATA_DIRECTION=:DATA_DIRECT_UD, A.DATA_VALUE=:DATA_VALUE_UD, A.DESCRIPTION=:DESC_UD"
                                     + " WHEN NOT MATCHED THEN "
                                     + " INSERT (DATA_SETTING_ID,STEPS_ID,LOOP_ID,DATA_VALUE, VALUE_OR_OBJECT, DESCRIPTION,DATA_SUMMARY_ID,DATA_DIRECTION,POOL_ID ) "
                                     + "  VALUES(:DATA_SETTING_ID,:STEPS_ID,1,:DATA_VALUE, :VALUE_OR_OBJECT, :DESCRIPTION,:DATA_SUMMARY_ID,:DATA_DIRECTION,:POOL_ID)";
                string strSql = @"INSERT INTO TEST_DATA_SETTING(DATA_SETTING_ID,STEPS_ID,LOOP_ID,DATA_VALUE, VALUE_OR_OBJECT, DESCRIPTION,DATA_SUMMARY_ID,DATA_DIRECTION,POOL_ID )
                                    VALUES(:DATA_SETTING_ID,:STEPS_ID,1,:DATA_VALUE, :VALUE_OR_OBJECT, :DESCRIPTION,:DATA_SUMMARY_ID,:DATA_DIRECTION,:POOL_ID)";
                long lDataSettingId = BoHelper.GetBussinessSeq(ID_SEQ, dbCmmd, ref strError, ref isOk);
                if (!isOk) return null;

                //Logger.Info("CreateNewRecord",string.Format("sql:[{0}]",strsqlMerge));
                dbCmmd.Parameters.Clear();
                //dbCmmd.CommandText = strSql;
                dbCmmd.CommandText = strsqlMerge;

                DbParameter paraDATA_SETTING_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDATA_SETTING_ID.ParameterName = "DATA_SETTING_ID";
                paraDATA_SETTING_ID.Value = rslt.DATA_SETTING_ID = lDataSettingId;
                DbParameter paraSTEPS_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraSTEPS_ID.ParameterName = "STEPS_ID";
                paraSTEPS_ID.Value = rslt.DATA_SETTING_ID = sTEPS_ID;
                DbParameter paraDATA_VALUE = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDATA_VALUE.ParameterName = "DATA_VALUE";
                paraDATA_VALUE.Value = rslt.DATA_VALUE = dataValue;
                DbParameter paraVALUE_OR_OBJECT = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraVALUE_OR_OBJECT.ParameterName = "VALUE_OR_OBJECT";
                paraVALUE_OR_OBJECT.Value = rslt.VALUE_OR_OBJECT = (short)valueOrObj;
                DbParameter paraDESCRIPTION = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDESCRIPTION.ParameterName = "DESCRIPTION";
                paraDESCRIPTION.Value = rslt.DESCRIPTION = desc;
                DbParameter paraDATA_SUMMARY_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDATA_SUMMARY_ID.ParameterName = "DATA_SUMMARY_ID";
                paraDATA_SUMMARY_ID.Value = rslt.DATA_SUMMARY_ID = dATA_SUMMARY_ID;
                DbParameter paraDATA_DIRECTION = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDATA_DIRECTION.ParameterName = "DATA_DIRECTION";
                paraDATA_DIRECTION.Value = rslt.DATA_DIRECTION = (short)data_direction;
                DbParameter paraPOOL_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraPOOL_ID.ParameterName = "POOL_ID";
                paraPOOL_ID.Value = rslt.POOL_ID = poolid;

                dbCmmd.Parameters.Add(paraDATA_SETTING_ID);
                dbCmmd.Parameters.Add(paraSTEPS_ID);
                dbCmmd.Parameters.Add(paraDATA_VALUE);
                dbCmmd.Parameters.Add(paraVALUE_OR_OBJECT);
                dbCmmd.Parameters.Add(paraDESCRIPTION);
                dbCmmd.Parameters.Add(paraDATA_SUMMARY_ID);
                dbCmmd.Parameters.Add(paraDATA_DIRECTION);
                dbCmmd.Parameters.Add(paraPOOL_ID);

                //FOR UPDATE PART
                DbParameter paraDATA_VALUE_UD = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDATA_VALUE_UD.ParameterName = "DATA_VALUE_UD";
                paraDATA_VALUE_UD.Value = rslt.DATA_VALUE = dataValue;
                DbParameter paraDESC_UD = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDESC_UD.ParameterName = "DESC_UD";
                paraDESC_UD.Value = rslt.DESCRIPTION = desc;
                DbParameter paraDATA_DIRECT_UD = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDATA_DIRECT_UD.ParameterName = "DATA_DIRECT_UD";
                paraDATA_DIRECT_UD.Value = rslt.DATA_DIRECTION = (short)data_direction;
                dbCmmd.Parameters.Add(paraDATA_VALUE_UD);
                dbCmmd.Parameters.Add(paraDESC_UD);
                dbCmmd.Parameters.Add(paraDATA_DIRECT_UD);

                dbCmmd.ExecuteNonQuery();
                isOk = true;
                return rslt;
            }
            catch (Exception e)
            {
                Logger.Error("CreateNewRecord", strError = string.Format("Exception:[{0}] stacktrace:[{1}]", e.Message, e.StackTrace), e);
                isOk = false;
                return null;
            }
        }
    }
}
