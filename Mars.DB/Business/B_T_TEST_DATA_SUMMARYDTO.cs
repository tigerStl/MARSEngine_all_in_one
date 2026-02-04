using Mars.message.DataLayer;
using Mars.message.DataLayer.Generic;
using Mars.message.Dto;
using Mars.Model;

using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Transactions;

namespace Mars.message.Business
{
    /// <summary>
    /// 修改记录：
    /// 时间：2017-12-19
    /// 作者：tiger
    /// 内容：DATA_DIRECTION的用途新增：0-表示数据从数据库获得，1-表示从excel获得，第三位，为1表示该行在执行中将被skip
    /// </summary>
    public class B_T_TEST_DATA_SUMMARYDTO : T_TEST_DATA_SUMMARYDTO
    {

        private const string CNST_ASSIGNED_ID_SEQ = "SEQ_BASELINE_DATA_ID";
        internal const string CNST_DATASET_ID_SEQ = "T_TEST_STEPS_SEQ";
        private static MLogger Logger = MLogger.GetLogger(typeof(B_T_TEST_DATA_SUMMARYDTO));
        public static T_TEST_DATA_SUMMARYDTO GetDataSummaryByStoryBoardIdTestCaseIDRunorder(Int64 iStoryBoardId, Int64 iTestCaseId, Int64 iRunOrder,
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.Info("GetDataSummaryByStoryBoardIdTestCaseIDRunorder", string.Format("iStoryBoardId:[{0}] iTestCaseId:[1],iRunOrder:[2]", iStoryBoardId, iTestCaseId, iRunOrder));
            //Logger.Info();
            MarsDataAccessLayer<T_STORYBOARD_DATASET_SETTING> objLayer = new MarsDataAccessLayer<T_STORYBOARD_DATASET_SETTING>(strDBIdx);
            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = (from storyDataSet in objEntities.T_STORYBOARD_DATASET_SETTING
                         from storyDetail in objEntities.T_PROJ_TC_MGR
                         where storyDetail.STORYBOARD_DETAIL_ID == iStoryBoardId
                            && storyDataSet.STORYBOARD_DETAIL_ID == storyDetail.STORYBOARD_DETAIL_ID
                            && storyDetail.TEST_CASE_ID == iTestCaseId
                         //&& storyDataSet.RUN_ORDER == iRunOrder                            
                         select storyDataSet.T_TEST_DATA_SUMMARY).Distinct();
            Logger.Info("GetDataSummaryByStoryBoardIdTestCaseIDRunorder", query.ToString());
            foreach (T_TEST_DATA_SUMMARY objEntity in query)
            {
                T_TEST_DATA_SUMMARYDTO objDto = T_TEST_DATA_SUMMARYAssembler.ToDTO(objEntity);
                return objDto;
            }
            Logger.Warnning("GetDataSummaryByStoryBoardIdTestCaseIDRunorder", string.Format("NO such T_TEST_DATA_SUMMARY found by iStoryBoardId:[{0}], iTestCaseId:[{1}], iRunOrder:[{2}]", iStoryBoardId, iTestCaseId, iRunOrder));
            return null;
        }



        public List<T_TEST_DATA_SUMMARYDTO> getBaselineDataSetsBy(string strDBIdx, string searchKey)
        {
            Logger.Info("getBaselineDataSetsBy", string.Format("try to get list by:[{0}]", searchKey));
            MarsEntities objEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var p = (from q in objEntities.T_TEST_DATA_SUMMARY
                         where q.ALIAS_NAME.IndexOf(searchKey) >= 0 || q.DESCRIPTION_INFO.IndexOf(searchKey) >= 0
                         select q).ToList();
                return T_TEST_DATA_SUMMARYAssembler.ToDTOs(p);
            }
            catch (Exception e)
            {
                Logger.Error("getBaselineDataSetsBy", string.Format("exception :[{0}]", e.Message), e);
                return null;
            }

            //MarsDataAccessLayer<T_TEST_DATA_SUMMARY> objLayer = new MarsDataAccessLayer<T_TEST_DATA_SUMMARY>();
            //IList<T_TEST_DATA_SUMMARY> lstEntity = objLayer.GetList(p => p.ALIAS_NAME.IndexOf(searchKey) >= 0 || p.DESCRIPTION_INFO.IndexOf(searchKey) >= 0);

            //List<T_TEST_DATA_SUMMARYDTO> lstDto = T_TEST_DATA_SUMMARYAssembler.ToDTOs(lstEntity.OrderBy(p => p.ALIAS_NAME));
            //return lstDto;
        }

        #region Transaction update database
        /// <summary>
        /// create a transaction variable
        /// Try catch is required
        /// </summary>
        /// 
        MarsEntities m_EntitiesInstance = null;
        DbTransaction m_objTrans = null;

        public long GetNewIdForBaseLineData()
        {
            Logger.logBegin("GetNewIdForBaseLineData");
            long lId = BoHelper.GetIdBySeqName(CNST_ASSIGNED_ID_SEQ, m_objTrans.Connection);
            if (lId <= 0)
            {
                Logger.Error("GetNewIdForBaseLineData", string.Format("Can't get ID from Sequenes:[{0}]", CNST_ASSIGNED_ID_SEQ));
            }
            return lId;
        }

        public DbTransaction InitTransaction(string strDBIdx)
        {
            Logger.logBegin("InitTransaction");
            m_EntitiesInstance = BoHelper.GetMarsEntitiesInstance(true,strDBIdx);

            if (m_EntitiesInstance.Database.Connection.State != ConnectionState.Open)
            {
                m_EntitiesInstance.Database.Connection.Open();
            }
            //m_objTrans = new TransactionScope();
            m_objTrans = m_EntitiesInstance.Database.Connection.BeginTransaction();
            return m_objTrans;
        }

        public bool DeleteObject_BASELINE_DATA_DETAIL(IList<T_BASELINE_DATA_SUMMARYDTO> lstSum, ref string strError)
        {
            Logger.Info("DeleteObject_BASELINE_DATA_DETAIL", string.Format("Try to delete details by list, count:[{0}]", lstSum == null ? -1 : lstSum.Count));
            if (m_objTrans == null)
            {
                Logger.Error("DeleteObject_BASELINE_DATA_DETAIL", strError = "No Transaction object is Exists");
                return false;
            }
            if ((lstSum == null ? -1 : lstSum.Count) <= 0)
                return true;
            string strDelSqlFormat = "delete from T_BASELINE_DATA_DETAIL where DATA_BASE_OBJ_ID={0}";
            try
            {
                using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
                {
                    foreach (T_BASELINE_DATA_SUMMARYDTO objItm in lstSum)
                    {
                        string strSqlToRun = string.Format(strDelSqlFormat, objItm.DATA_BASE_OBJ_ID);
                        dbCmd.CommandText = strSqlToRun;
                        //int iCnt = this.m_EntitiesInstance.Database.ExecuteSqlCommand(strSqlToRun);
                        int iCnt = dbCmd.ExecuteNonQuery();
                        Logger.Info("DeleteObject_BASELINE_DATA_DETAIL", string.Format("delete from T_BASELINE_DATA_DETAIL, count:{0}", iCnt));
                    }

                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteObject_BASELINE_DATA_DETAIL", strError = string.Format("Exception:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public bool DeleteObject_BASELINE_DATA_DETAIL(T_BASELINE_DATA_SUMMARYDTO objSum, ref string strError)
        {
            Logger.Info("DeleteObject_BASELINE_DATA_DETAIL", string.Format("Try to delete details with data_base_obj_id:[{0}]", objSum == null ? -1 : objSum.DATA_BASE_OBJ_ID));
            if (m_objTrans == null)
            {
                Logger.Error("DeleteObject_BASELINE_DATA_DETAIL", strError = "No Transaction object is Exists");
                return false;
            }

            if (objSum.DATA_BASE_OBJ_ID <= 0)
            {
                Logger.Warnning("DeleteObject_BASELINE_DATA_DETAIL", string.Format("DATA_BASE_OBJ_ID :[{0}] is less than 0 ", objSum.DATA_BASE_OBJ_ID));
                return true;
            }
            string strDel = @"delete from T_BASELINE_DATA_DETAIL
                              where DATA_BASE_OBJ_ID=" + objSum.DATA_BASE_OBJ_ID;
            try
            {
                using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
                {
                    dbCmd.CommandText = strDel;
                    Logger.Info("SQL OUT PUT", strDel);
                    int iCnt = dbCmd.ExecuteNonQuery();
                    //int iCnt = this.m_EntitiesInstance.Database.ExecuteSqlCommand(strDel);

                    Logger.Info("DeleteObject_BASELINE_DATA_DETAIL", string.Format("delete from T_BASELINE_DATA_DETAIL, count:{0}", iCnt));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteObject_BASELINE_DATA_DETAIL", strError = string.Format("Exceptions:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }

        }

        public static List<T_TEST_DATA_SUMMARYDTO> GetAllDataSetSummary(string strDBIdx)
        {
            MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var ds = (from qp in dbCntx.T_TEST_DATA_SUMMARY
                      select qp).OrderBy(p => p.ALIAS_NAME);
            return ds.ToDTOs();
        }

        public long AssignDataSetToTestCase(long lTCId, long lDataSetId, MarsEntities dbCntx, ref bool isOk, ref string strError, bool isCreateNewAlways = true)
        {
            Logger.logBegin("AssignDataSetToTestCase", string.Format("Test case Id:[{0}] data id:[{1}]", lTCId, lDataSetId));
            try
            {

                if (!isCreateNewAlways)
                {
                    var tc_dt = (from d in dbCntx.REL_TC_DATA_SUMMARY
                                 where d.TEST_CASE_ID == lTCId
                                 && d.DATA_SUMMARY_ID == lDataSetId
                                 select d).FirstOrDefault();
                    if (tc_dt != null)
                    {
                        isOk = true;
                        return tc_dt.ID;
                    }
                }
                REL_TC_DATA_SUMMARYDTO objRel = new REL_TC_DATA_SUMMARYDTO();
                objRel.ID = BoHelper.GetIdBySeqName(CNST_DATASET_ID_SEQ, dbCntx);
                objRel.CREATE_TIME = DateTime.Now;
                objRel.DATA_SUMMARY_ID = lDataSetId;
                objRel.TEST_CASE_ID = lTCId;

                dbCntx.Set<REL_TC_DATA_SUMMARY>();
                dbCntx.REL_TC_DATA_SUMMARY.Add(objRel.ToEntity());

                isOk = true;
                return objRel.ID;
            }
            catch (Exception e)
            {
                Logger.Error("AssignDataSetToTestCase", string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace));
                isOk = false;
                return -1;
            }
            finally
            {
                Logger.logEnd("AssignDataSetToTestCase");
            }
        }

        public long CreateDataSet(string dataSetName,
            string description,
            ref bool isOk,
            ref string strError,
            MarsEntities objDBCntx)
        {
            Logger.logBegin("CreateDataSet", string.Format("Data set Name:[{0}]", dataSetName));
            if (objDBCntx == null)
            {
                Logger.Error("CreateDataSet", strError = "Database context is null");
                isOk = false;
                return -1;
            }
            try
            {
                T_TEST_DATA_SUMMARYDTO objNew = new T_TEST_DATA_SUMMARYDTO();
                objNew.ALIAS_NAME = dataSetName;
                objNew.AVAILABLE_MARK = 1;
                objNew.CREATE_TIME = DateTime.Now;
                objNew.DATA_SET_TYPE = 0; //for test case
                objNew.DATA_SUMMARY_ID = BoHelper.GetIdBySeqName(CNST_DATASET_ID_SEQ, objDBCntx);
                objNew.DESCRIPTION_INFO = description;

                objDBCntx.Set<T_TEST_DATA_SUMMARY>();
                objDBCntx.T_TEST_DATA_SUMMARY.Add(objNew.ToEntity());
                isOk = true;
                return objNew.DATA_SUMMARY_ID;
            }
            catch (Exception e)
            {
                Logger.Error("CreateDataSet", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace), e);
                isOk = false;
                return -1;
            }
        }

        public static T_TEST_DATA_SUMMARYDTO GetDataSetFromName(string dataSetName, string description, ref bool isOk, ref string strError, MarsEntities dbCntx)
        {
            Logger.logBegin("GetDataSetFromName", string.Format("DataSetName:[{0}] desc:[{1}]", dataSetName, description));
            try
            {
                var dataSet = (from d in dbCntx.T_TEST_DATA_SUMMARY
                               where string.Compare(dataSetName, d.ALIAS_NAME) == 0
                               select d).FirstOrDefault();
                if (dataSet == null)
                {
                    isOk = true;
                    strError = string.Format("Cant find dataset by name:[{0}]", dataSetName);
                    return null;
                }
                isOk = true;
                return dataSet.ToDTO();
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetDataSetFromName", strError = string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetDataSetFromName");
            }
        }

        public static bool AddNewDataSetAndTestCaseRelByCmd(string dataSetName, string desc, long tEST_CASE_ID, DbCommand dbCmmd, ref string strError,
            out T_TEST_DATA_SUMMARYDTO createdNew)
        {
            Logger.logBegin("AddNewDataSetByCmd", string.Format("Data setName:[{0}] test case_id", dataSetName, tEST_CASE_ID));
            try
            {
                dbCmmd.Parameters.Clear();
                bool isOk = false;
                long datasetId = BoHelper.GetBussinessSeq(CNST_DATASET_ID_SEQ, dbCmmd, ref strError, ref isOk);
                if (!isOk)
                {
                    createdNew = null;
                    return false;
                }

                isOk = AddnewDataSet(datasetId, dataSetName, desc, dbCmmd, ref strError);
                if (!isOk)
                {
                    createdNew = null;
                    return false;
                }

                long relId = BoHelper.GetBussinessSeq(CNST_DATASET_ID_SEQ, dbCmmd, ref strError, ref isOk);
                if (!isOk)
                {
                    createdNew = null;
                    return false;
                }
                isOk = BoHelper.CreateTCDataSetRel(relId, datasetId, dbCmmd, tEST_CASE_ID, ref strError);
                if (!isOk)
                {
                    createdNew = null;
                    return false;
                }
                createdNew = new T_TEST_DATA_SUMMARYDTO()
                {
                    DATA_SUMMARY_ID = datasetId,
                    ALIAS_NAME = dataSetName
                };
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("AddNewDataSetAndTestCaseRelByCmd", strError = string.Format("Exception:[{0}], stacktrace:[{1}]", e.Message, e.StackTrace), e);
                createdNew = null;
                return false;
            }
            finally
            {
                Logger.logEnd("AddNewDataSetAndTestCaseRelByCmd");
            }
        }

        private static bool AddnewDataSet(long datasetId, string dataSetName, object dssc, DbCommand dbCmmd, ref string strError)
        {
            string strSql = @"INSERT INTO T_TEST_DATA_SUMMARY(DATA_SUMMARY_ID, ALIAS_NAME, DESCRIPTION_INFO) VALUES(:DATA_SUMMARY_ID, :ALIAS_NAME, :DESCRIPTION_INFO)";
            dbCmmd.Parameters.Clear();
            try
            {
                dbCmmd.CommandText = strSql;

                DbParameter paraDATA_SUMMARY_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDATA_SUMMARY_ID.ParameterName = "DATA_SUMMARY_ID";
                paraDATA_SUMMARY_ID.Value = datasetId;
                DbParameter paraALIAS_NAME = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraALIAS_NAME.ParameterName = "ALIAS_NAME";
                paraALIAS_NAME.Value = dataSetName;
                DbParameter paraDESCRIPTION_INFO = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDESCRIPTION_INFO.ParameterName = "DESCRIPTION_INFO";
                paraDESCRIPTION_INFO.Value = dssc;

                dbCmmd.Parameters.Clear();
                dbCmmd.Parameters.Add(paraDATA_SUMMARY_ID);
                dbCmmd.Parameters.Add(paraALIAS_NAME);
                dbCmmd.Parameters.Add(paraDESCRIPTION_INFO);

                dbCmmd.ExecuteNonQuery();

                return true;

            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]", e.Message);
                Logger.Error("AddnewDataSet", strError, e);
                return false;
            }
        }

        public bool DeleteObject_BASELINE_DATA_DETAIL_byBaseObject(long lBaseObjectId, ref string strError)
        {
            Logger.Info("DeleteObject_BASELINE_DATA_DETAIL_byBaseObject", string.Format("try to delete object by ID:[{0}]", lBaseObjectId));
            string strSqlDel = @"delete from T_BASELINE_DATA_DETAIL 
                                 where DATA_BASE_OBJ_ID in (SELECT DISTINCT DATA_BASE_OBJ_ID FROM  T_BASELINE_DATA_SUMMARY WHERE 
                                    DATA_BASE_OBJ_PARENT_ID={0})";
            if (m_objTrans == null)
            {
                Logger.Error("DeleteObject_BASELINE_DATA_DETAIL_byBaseObject", strError = "No Transaction object is Exists");
                return false;
            }

            try
            {
                using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
                {
                    dbCmd.CommandText = strSqlDel = string.Format(strSqlDel, lBaseObjectId);
                    Logger.Info("SQL OUT PUT", strSqlDel);
                    int iCnt = dbCmd.ExecuteNonQuery();
                    //int iCnt = this.m_EntitiesInstance.Database.ExecuteSqlCommand(strDel);

                    Logger.Info("DeleteObject_BASELINE_DATA_DETAIL_byBaseObject", string.Format("delete from T_BASELINE_DATA_DETAIL, count:{0}", iCnt));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteObject_BASELINE_DATA_DETAIL_byBaseObject", strError = string.Format("Exceptions:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public bool DeleteObject_BASELINE_DATA_DETAIL(T_BASELINE_DATA_DETAILDTO objDto, ref string strError)
        {
            Logger.Info("DeleteObject_BASELINE_DATA_DETAIL", string.Format("Id to be delted:[{0}]", objDto == null ? -2 : objDto.DETAIL_ID));
            if (m_objTrans == null)
            {
                Logger.Error("DeleteObject_BASELINE_DATA_DETAIL", strError = "No Transaction object is Exists");
                return false;
            }

            if (objDto.DETAIL_ID <= 0)
            {
                Logger.Warnning("DeleteObject_BASELINE_DATA_DETAIL", string.Format("DETAIL_ID :[{0}] is less than 0 ", objDto.DETAIL_ID));
                /// that means data dealing logic is not right,just return true
                return true;
            }
            string strDelSql = @"delete from T_BASELINE_DATA_DETAIL 
                                 where DETAIL_ID=" + objDto.DETAIL_ID;
            //int iCnt = m_EntitiesInstance.Database.ExecuteSqlCommand(strDelSql);
            using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
            {
                dbCmd.CommandText = strDelSql;
                Logger.Info("SQL OUT PUT", strDelSql);
                int iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("DeleteObject_BASELINE_DATA_DETAIL", string.Format("delete from T_BASELINE_DATA_DETAIL, count:{0}", iCnt));
            }
            return true;
        }

        public bool Deleteobject_BASELINE_DATA_SUMMARY_AUTOGEN(T_BASELINE_DATA_SUMMARYDTO objMajor, ref string strError)
        {
            Logger.Info("Deleteobject_BASELINE_DATA_SUMMARY_AUTOGEN", string.Format("Try to delete auto generated objects by parent id:[{0}],object name:[{1}]", objMajor == null ? -1 : objMajor.DATA_BASE_OBJ_ID, objMajor == null ? "" : objMajor.OBJECT_HAPPY_NAME));
            if (m_objTrans == null)
            {
                Logger.Error("Deleteobject_BASELINE_DATA_SUMMARY_AUTOGEN", strError = "No Transaction object is Exists");
                return false;
            }
            if (objMajor == null)
            {
                Logger.Warnning("Deleteobject_BASELINE_DATA_SUMMARY_AUTOGEN", "major  object is null, just ignored.");
                return true;
            }
            string strSqlDel = @"DELETE FROM T_BASELINE_DATA_SUMMARY WHERE DATA_BASE_OBJ_PARENT_ID=" + objMajor.DATA_BASE_OBJ_ID;
            using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
            {
                dbCmd.CommandText = strSqlDel;
                Logger.Info("SQL OUT PUT", strSqlDel);
                int iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("Deleteobject_BASELINE_DATA_SUMMARY_AUTOGEN", string.Format("delete from T_BASELINE_DATA_SUMMARY, count:{0}", iCnt));
            }
            return true;
        }

        public bool Deleteobject_BASELINE_DATA_SUMMARY(T_BASELINE_DATA_SUMMARYDTO objMajor, ref string strError)
        {
            Logger.Info("Deleteobject_BASELINE_DATA_SUMMARY", string.Format("Try to delete marjor object by ID:[{0}]", objMajor == null ? -1 : objMajor.DATA_BASE_OBJ_ID));
            if (m_objTrans == null)
            {
                Logger.Error("Deleteobject_BASELINE_DATA_SUMMARY", strError = "No Transaction object is Exists");
                return false;
            }
            if (objMajor == null)
            {
                Logger.Warnning("Deleteobject_BASELINE_DATA_SUMMARY", "major  object is null, just ignored.");
                return true;
            }
            string strSqlDel = @"DELETE FROM T_BASELINE_DATA_SUMMARY WHERE DATA_BASE_OBJ_ID=" + objMajor.DATA_BASE_OBJ_ID;
            using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
            {
                dbCmd.CommandText = strSqlDel;
                Logger.Info("SQL OUT PUT", strSqlDel);
                int iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("Deleteobject_BASELINE_DATA_SUMMARY_AUTOGEN", string.Format("delete from T_BASELINE_DATA_SUMMARY, count:{0}", iCnt));
            }
            return true;
        }



        public bool RollbackTransactoin(ref string strError)
        {
            Logger.logBegin("RollbackTransactoin");
            try
            {

                //m_objTrans.
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("RollbackTransactoin", strError = string.Format("Exception when rollback:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public bool CommitTransction(ref string strError)
        {
            Logger.logBegin("CommitTransction");
            try
            {
                //m_objTrans.Complete();
                m_objTrans.Commit();
                return true;
            }
            catch (Exception e)
            {
                try
                {
                    m_objTrans.Rollback();
                    strError += string.Format("Exceptoin when call commit:[{0}],\r\nstackTrace[{1}]", e.Message, e.StackTrace);
                }
                catch (Exception ex)
                {
                    strError += string.Format("CommitTransction-rollback Exception:[{0}],\r\nstackTrace:[{1}]", ex.Message, ex.StackTrace);
                }
                return false;
            }
        }

        private string CreateNewBaseLineDataSummarySql(T_BASELINE_DATA_SUMMARYDTO objBaseSummary)
        {
            string strSqlInsert = @"INSERT INTO T_BASELINE_DATA_SUMMARY VALUES(
                            {0}, 
                            {1},
                            {2},    
                            '{3}',
                            {4}
                            )";
            return string.Format(strSqlInsert, objBaseSummary.DATA_BASE_OBJ_ID,
                objBaseSummary.DATA_SUMMARY_ID == null ? "null" : objBaseSummary.DATA_SUMMARY_ID.ToString(),
                objBaseSummary.DATA_BASE_OBJ_PARENT_ID == null ? "null" : objBaseSummary.DATA_BASE_OBJ_PARENT_ID.ToString(),
                objBaseSummary.OBJECT_HAPPY_NAME ?? "",
                objBaseSummary.ORDER_ID == null ? "null" : objBaseSummary.ORDER_ID.ToString());
        }
        private string UpdateNewBaseLineDataSummarySql(T_BASELINE_DATA_SUMMARYDTO objBaseSummary)
        {
            string strSqlUpdate = @"UPDATE T_BASELINE_DATA_SUMMARY set                         
                        DATA_SUMMARY_ID ={1},
                        DATA_BASE_OBJ_PARENT_ID={2},
                        OBJECT_HAPPY_NAME ='{3}',
                        ORDER_ID={4}
                        Where DATA_BASE_OBJ_ID ={0}";
            return string.Format(strSqlUpdate, objBaseSummary.DATA_BASE_OBJ_ID,
                objBaseSummary.DATA_SUMMARY_ID == null ? "null" : objBaseSummary.DATA_SUMMARY_ID.ToString(),
                objBaseSummary.DATA_BASE_OBJ_PARENT_ID == null ? "null" : objBaseSummary.DATA_BASE_OBJ_PARENT_ID.ToString(),
                objBaseSummary.OBJECT_HAPPY_NAME ?? "",
                objBaseSummary.ORDER_ID == null ? "null" : objBaseSummary.ORDER_ID.ToString());
        }

        public bool updateOrCreateBaseLineSummaryObj(bool isCreateANew, T_BASELINE_DATA_SUMMARYDTO objBaseSummary, ref string strError)
        {
            Logger.Info("updateOrCreateBaseLineSummaryObj", string.Format("Try [{0}] with DATA_BASE_OBJ_ID :[{1}]", isCreateANew ? "Create" : "update", objBaseSummary == null ? -1 : objBaseSummary.DATA_BASE_OBJ_ID));
            if (objBaseSummary == null) return true;
            try
            {
                string strSqlToBeRun = "";
                if (isCreateANew)
                {
                    strSqlToBeRun = CreateNewBaseLineDataSummarySql(objBaseSummary);
                }
                else
                {
                    strSqlToBeRun = UpdateNewBaseLineDataSummarySql(objBaseSummary);
                }

                using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
                {
                    dbCmd.CommandText = strSqlToBeRun;
                    Logger.Info("SQL OUT PUT", strSqlToBeRun);
                    int iCnt = dbCmd.ExecuteNonQuery();
                    Logger.Info("updateOrCreateBaseLineSummaryObj", string.Format("updateOrCreateBaseLineSummaryObj from T_BASELINE_DATA_DETAIL, count:{0}", iCnt));
                }

                #region using EntityFrame
                //if (isCreateANew)
                //{
                //    m_EntitiesInstance.T_BASELINE_DATA_SUMMARY.Add(T_BASELINE_DATA_SUMMARYAssembler.ToEntity(objBaseSummary));

                //}
                //else
                //{
                //    DbSet<T_BASELINE_DATA_SUMMARY> objQuery = m_EntitiesInstance.Set<T_BASELINE_DATA_SUMMARY>();
                //    T_BASELINE_DATA_SUMMARY objData = objQuery.Where(p => p.DATA_BASE_OBJ_ID == objBaseSummary.DATA_BASE_OBJ_ID).FirstOrDefault();
                //    /// copy object to objData
                //    /// 
                //    if (objData == null)
                //    {
                //        m_EntitiesInstance.T_BASELINE_DATA_SUMMARY.Add(T_BASELINE_DATA_SUMMARYAssembler.ToEntity(objBaseSummary));
                //        Logger.Warnning("updateOrCreateBaseLineSummaryObj", string.Format("Can't find T_BASELINE_DATA_SUMMAR object by ID [{0}], creating a new object is applied, not updating"));
                //    }
                //    else
                //    {
                //        objData.DATA_BASE_OBJ_PARENT_ID = objBaseSummary.DATA_BASE_OBJ_PARENT_ID;
                //        objData.DATA_SUMMARY_ID = objBaseSummary.DATA_SUMMARY_ID;
                //        objData.OBJECT_HAPPY_NAME = objBaseSummary.OBJECT_HAPPY_NAME;
                //        objData.ORDER_ID = objBaseSummary.ORDER_ID;

                //        objQuery.Attach(objData);
                //    }

                //}
                #endregion

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateOrCreateBaseLineSummaryObj", strError = string.Format("Exception when attache object:[{0}]\r\nStackTrace:[{1}]", e.Message, e.StackTrace));
                return false;
            }
        }

        public bool updateOrCreateExtendBaseLineSummaryObj(bool isCreateANew, T_BASELINE_DATA_SUMMARYDTO objExtendBaseObj, ref string strError)
        {
            Logger.logBegin("updateOrCreateExtendBaseLineSummaryObj");
            return updateOrCreateBaseLineSummaryObj(isCreateANew, objExtendBaseObj, ref strError);
        }


        private string InsertSqlForDetail(T_BASELINE_DATA_DETAILDTO objDtl)
        {
            string strResult = @"INSERT INTO T_BASELINE_DATA_DETAIL VALUES(
                {0},
                {1},
                {2},                
                '{3}'
            )";
            return string.Format(strResult,
                objDtl.DETAIL_ID,
                objDtl.DATA_BASE_OBJ_ID == null ? "null" : objDtl.DATA_BASE_OBJ_ID.ToString(),
                objDtl.LOOP_ID == null ? "null" : objDtl.LOOP_ID.ToString(),
                objDtl.DATA_VALUE ?? "");
        }
        private string UpdateSqlForDetail(T_BASELINE_DATA_DETAILDTO objDtl)
        {
            string strResult = @"UPDATE T_BASELINE_DATA_DETAIL SET
                DATA_BASE_OBJ_ID ={1},
                LOOP_ID ={2},
                DATA_VALUE='{3}'
                WHERE DETAIL_ID={0}";
            return string.Format(strResult,
                objDtl.DETAIL_ID,
                objDtl.DATA_BASE_OBJ_ID == null ? "null" : objDtl.DATA_BASE_OBJ_ID.ToString(),
                objDtl.LOOP_ID == null ? "null" : objDtl.LOOP_ID.ToString(),
                objDtl.DATA_VALUE ?? "");
        }


        public bool updateOrCreateDetailObject(bool isCreateANew, T_BASELINE_DATA_DETAILDTO objDetailInfo, ref string strError)
        {
            Logger.Info("updateOrCreateDetailObject", string.Format("try to create a new Detail object"));

            string strSqlToRun = "";

            if (isCreateANew)
            {

                strSqlToRun = InsertSqlForDetail(objDetailInfo);
            }
            else
            {
                strSqlToRun = UpdateSqlForDetail(objDetailInfo);
            }
            using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
            {
                dbCmd.CommandText = strSqlToRun;
                Logger.Info("SQL OUT PUT", strSqlToRun);
                int iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("updateOrCreateDetailObject", string.Format("update Or insert into T_BASELINE_DATA_DETAIL, count:{0}", iCnt));
            }

            #region entityframeworkd
            //if (isCreateANew)
            //{
            //    m_EntitiesInstance.T_BASELINE_DATA_DETAIL.Add(T_BASELINE_DATA_DETAILAssembler.ToEntity(objDtl));
            //}
            //else
            //{
            //    DbSet<T_BASELINE_DATA_DETAIL> objQuery = m_EntitiesInstance.Set<T_BASELINE_DATA_DETAIL>();
            //    T_BASELINE_DATA_DETAIL objDtlEntity = objQuery.Where(p => p.DETAIL_ID == objDtl.DETAIL_ID).FirstOrDefault();
            //    if (objDtlEntity==null)
            //    {
            //        Logger.Warnning("updateOrCreateDetailObject",string.Format("Can't find detail object by id:[{0}],creating a new object is applied, not updating", objDtl.DETAIL_ID));
            //        m_EntitiesInstance.T_BASELINE_DATA_DETAIL.Add(T_BASELINE_DATA_DETAILAssembler.ToEntity(objDtl));
            //    }
            //    else
            //    {
            //        objDtlEntity.DATA_BASE_OBJ_ID = objDtl.DATA_BASE_OBJ_ID;
            //        objDtlEntity.DATA_VALUE = objDtl.DATA_VALUE;
            //        objDtlEntity.LOOP_ID = sLoop;

            //        objQuery.Attach(objDtlEntity);
            //    }
            //}
            #endregion
            return true;
        }

        public bool DeleteBaseDetailByDataSetId(long dATA_SUMMARY_ID, ref string strError)
        {
            Logger.Info("DeleteBaseDetailByDataSetId", string.Format("Data Summary Id:[{0}]", dATA_SUMMARY_ID));
            if (m_objTrans == null)
            {
                Logger.Error("DeleteBaseDetailByDataSetId", strError = "No Transaction object is Exists");
                return false;
            }
            string strSqlDel = @"DELETE FROM T_BASELINE_DATA_DETAIL 
                                WHERE DATA_BASE_OBJ_ID IN (
                                        SELECT DISTINCT DATA_BASE_OBJ_ID 
                                        FROM T_BASELINE_DATA_SUMMARY WHERE DATA_SUMMARY_ID=" + dATA_SUMMARY_ID + ")";
            using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
            {
                dbCmd.CommandText = strSqlDel;
                Logger.Info("SQL OUT PUT", strSqlDel);
                int iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("DeleteBaseDetailByDataSetId", string.Format("delete from T_BASELINE_DATA_SUMMARY, count:{0}", iCnt));
            }
            return true;
        }

        public bool DeleteBaseSummaryByDataSetId(long dATA_SUMMARY_ID, ref string strError)
        {
            Logger.Info("DeleteBaseSummaryByDataSetId", string.Format("Data Summary Id:[{0}]", dATA_SUMMARY_ID));
            if (m_objTrans == null)
            {
                Logger.Error("DeleteBaseSummaryByDataSetId", strError = "No Transaction object is Exists");
                return false;
            }
            string strSqlDel = @"DELETE FROM T_BASELINE_DATA_SUMMARY WHERE DATA_SUMMARY_ID=" + dATA_SUMMARY_ID;
            using (DbCommand dbCmd = m_objTrans.Connection.CreateCommand())
            {
                dbCmd.CommandText = strSqlDel;
                Logger.Info("SQL OUT PUT", strSqlDel);
                int iCnt = dbCmd.ExecuteNonQuery();
                Logger.Info("DeleteBaseSummaryByDataSetId", string.Format("delete from T_BASELINE_DATA_SUMMARY, count:{0}", iCnt));
            }
            return true;
        }

        public bool SaveDataSetAs(string strDBIdx, 
            long isrcDataId, long lTestCaseId, string strOldDSName, string strNewDataSetName, ref string strError)
        {
            Logger.Info("SaveDataSetAs", string.Format("SrcDataId:[{0}] oldDataSetName:[{3}] newDataSetName:[{1}], testcaseId:[{2}]",
                isrcDataId, strNewDataSetName, lTestCaseId, strOldDSName));
            try
            {
                MarsEntities objDBCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var oldTestDataInfo = (from d in objDBCntx.T_TEST_DATA_SUMMARY
                                       where d.DATA_SUMMARY_ID == isrcDataId
                                       select d).FirstOrDefault();
                //T_TEST_DATA_SUMMARYDTO objDtSumDto = new T_TEST_DATA_SUMMARYDTO();
                //objDtSumDto.DATA_SUMMARY_ID = BoHelper.GetTestStepsId(objDBCntx);
                //objDtSumDto.ALIAS_NAME = strNewDataSetName;
                //objDtSumDto.AVAILABLE_MARK = oldTestDataInfo.AVAILABLE_MARK;
                //objDtSumDto.CREATE_TIME = DateTime.Now;
                //objDtSumDto.DATA_SET_TYPE = oldTestDataInfo.DATA_SET_TYPE;
                //objDtSumDto.DESCRIPTION_INFO = oldTestDataInfo.DESCRIPTION_INFO;
                //objDtSumDto.SHARE_MARK = oldTestDataInfo.SHARE_MARK;
                //objDtSumDto.STATUS = oldTestDataInfo.STATUS;
                //objDtSumDto.VERSION = oldTestDataInfo.VERSION;
                //objDBCntx.T_TEST_DATA_SUMMARY.Add(objDtSumDto.ToEntity());
                if (oldTestDataInfo == null)
                {
                    // strError = string.Format("No such Data set [{0}] with Id [{1}] in data base. Could it be deleted?",isrcDataId, );
                    return false;
                }
                using (TransactionScope scope = new TransactionScope())
                {
                    ///steps:
                    /// 1, create a new Summary with 
                    /// 2, create a shared data pool 
                    /// 3, create Test_data_setting
                    /// 
                    /// 1, create a new Summary with 
                    /// 
                    if (objDBCntx.Database.Connection.State != ConnectionState.Open)
                        objDBCntx.Database.Connection.Open();
                    long lNewId = BoHelper.GetIdBySeqName("T_TEST_STEPS_SEQ", objDBCntx.Database.Connection);
                    /// 2, create a shared data pool 
                    string strSqlInsertNewDataSummary = string.Format(@"INSERT INTO T_TEST_DATA_SUMMARY (DATA_SUMMARY_ID, ALIAS_NAME, DESCRIPTION_INFO,AVAILABLE_MARK,VERSION,SHARE_MARK,CREATE_TIME,STATUS,DATA_SET_TYPE)
                    SELECT {1}, :ALIAS_NAME,DESCRIPTION_INFO,AVAILABLE_MARK,VERSION,SHARE_MARK,SYSDATE,STATUS,DATA_SET_TYPE
                    FROM T_TEST_DATA_SUMMARY
                    WHERE DATA_SUMMARY_ID={0}", isrcDataId, lNewId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlInsertNewDataSummary;
#if Managed_Driver
                        DbParameter aliasPara = new Oracle.ManagedDataAccess.Client.OracleParameter();
#else
                        DbParameter aliasPara = new Oracle.DataAccess.Client.OracleParameter();
#endif
                        aliasPara.ParameterName = "ALIAS_NAME";
                        aliasPara.Value = strNewDataSetName;
                        dbCmmd.Parameters.Add(aliasPara);
                        dbCmmd.ExecuteNonQuery();
                    }

                    string strSqlInsertDataPool = string.Format(@"INSERT INTO T_SHARED_OBJECT_POOL (OBJECT_POOL_ID, DATA_SUMMARY_ID, OBJECT_NAME, OBJECT_ORDER, LOOP_ID, DATA_VALUE, CREATE_TIME, VERSION) 
                    SELECT T_TEST_STEPS_SEQ.NEXTVAL, {0}, OBJECT_NAME, OBJECT_ORDER, LOOP_ID, DATA_VALUE, SYSDATE, VERSION
                    FROM T_SHARED_OBJECT_POOL
                    WHERE DATA_SUMMARY_ID={1}", lNewId, isrcDataId);

                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlInsertDataPool;
                        dbCmmd.ExecuteNonQuery();
                    }

                    /// 3, create Test_data_setting  --seq TEST_DATA_SETTING_SEQ
                    /// ONLY LOOP==1 IS APPLIED
                    string strSqlInsertTestDataSetting = string.Format(@"INSERT INTO TEST_DATA_SETTING(DATA_SETTING_ID, STEPS_ID, LOOP_ID, DATA_VALUE, VALUE_OR_OBJECT, DESCRIPTION, DATA_SUMMARY_ID, DATA_DIRECTION, VERSION, CREATE_TIME, POOL_ID) 
                    SELECT TEST_DATA_SETTING_SEQ.NEXTVAL, STP.STEPS_ID, DT.LOOP_ID, DT.DATA_VALUE, DT.VALUE_OR_OBJECT, DT.DESCRIPTION, {2}, DT.DATA_DIRECTION, 1,SYSDATE , NULL
                    FROM TEST_DATA_SETTING DT, T_TEST_STEPS STP
                    WHERE DT.STEPS_ID=STP.STEPS_ID
                    AND DT.DATA_SUMMARY_ID={0} AND STP.TEST_CASE_ID={1} 
                    AND LOOP_ID=1", isrcDataId, lTestCaseId, lNewId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlInsertTestDataSetting;
                        dbCmmd.ExecuteNonQuery();
                    }

                    ///4, update pool_id 
                    /// not implemented, not neccessary
                    /// 
                    /// 5 update TC_Data summary relation table
                    /// 1
                    string strSqlInsertTCDataRel = string.Format("INSERT INTO REL_TC_DATA_SUMMARY ( ID,DATA_SUMMARY_ID,TEST_CASE_ID, CREATE_TIME) VALUES( T_TEST_STEPS_SEQ.NEXTVAL, {0}, {1}, SYSDATE)", lNewId, lTestCaseId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlInsertTCDataRel;
                        dbCmmd.ExecuteNonQuery();
                    }
                    scope.Complete();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("SaveDataSetAs", strError = string.Format("Exception when SaveData:[{0}] \r\n:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public bool DeleteDataSetById(string strDBIdx, long dataSheetId,
            string dataSheetName, long testCaseId, ref string strError)
        {
            Logger.Info("DeleteDataSetById", string.Format("Try to delete data set, id:[{0}] dataset Name:[{1}] test case Id:[{2}]", dataSheetId, dataSheetName, testCaseId));
            try
            {
                MarsEntities objDBCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if (objDBCntx.Database.Connection.State != ConnectionState.Open)
                    objDBCntx.Database.Connection.Open();
                using (TransactionScope trans = new TransactionScope())
                {
                    string strSqlDeleTestCaseData = string.Format(@"DELETE TEST_DATA_SETTING WHERE STEPS_ID IN (
                    SELECT STEPS_ID FROM T_TEST_STEPS WHERE TEST_CASE_ID={0}) AND DATA_SUMMARY_ID={1}", testCaseId, dataSheetId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlDeleTestCaseData;
                        dbCmmd.ExecuteNonQuery();
                    }

                    string strSqlDeleTC_DS_Rel = string.Format("DELETE REL_TC_DATA_SUMMARY WHERE DATA_SUMMARY_ID={0}", dataSheetId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlDeleTC_DS_Rel;
                        dbCmmd.ExecuteNonQuery();
                    }

                    string strSqlDeleSharedPool = string.Format("DELETE T_SHARED_OBJECT_POOL WHERE DATA_SUMMARY_ID={0}", dataSheetId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlDeleSharedPool;
                        dbCmmd.ExecuteNonQuery();
                    }

                    string strSqlDeleDataSummary = string.Format("DELETE T_TEST_DATA_SUMMARY WHERE DATA_SUMMARY_ID={0}", dataSheetId);
                    using (DbCommand dbCmmd = objDBCntx.Database.Connection.CreateCommand())
                    {
                        dbCmmd.CommandText = strSqlDeleDataSummary;
                        dbCmmd.ExecuteNonQuery();
                    }

                    trans.Complete();

                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteDataSetById", strError = string.Format("Exception when delete dataset:[{0}]\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
        }

        #endregion //Transaction update database

    }
}
