using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Data;
using System.Data.Common;

namespace Mars.Business
{
    public class B_STORYBOARD_DATASET_SETTING : T_STORYBOARD_DATASET_SETTINGDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_STORYBOARD_DATASET_SETTING));
        public static bool MergStoryboardDtlIdAndDataSetId(DbConnection cnn, long dtlId, long? dataSetId, ref string strError)
        {
            Logger.logBegin("MergStoryboardDtlIdAndDataSetId", string.Format("dtlId:[{0}]-dataId:[{1}]", dtlId, dataSetId));
            //string strSql = @"MERGE INTO T_STORYBOARD_DATASET_SETTING A USING ( SELECT " + dtlId + " AS STORYBOARD_DETAIL_ID, " + dataSetId + " AS DATA_SUMMARY_ID FROM DUAL) B "
            //              + " ON (A.STORYBOARD_DETAIL_ID=B.STORYBOARD_DETAIL_ID AND A.DATA_SUMMARY_ID=B.DATA_SUMMARY_ID AND ROWNUM = 1) "
            //              + " WHEN MATCHED THEN UPDATE SET A.TESTER_ID=A.TESTER_ID "
            //              + " WHEN NOT MATCHED THEN INSERT (SETTING_ID,STORYBOARD_DETAIL_ID,DATA_SUMMARY_ID,CREATETIME,VERSION, TESTER_ID, RUN_ORDER)"
            //              + " VALUES(T_TEST_STEPS_SEQ.NEXTVAL, :STORYBOARD_DETAIL_ID, :DATA_SUMMARY_ID, SYSDATE, 1, NULL, NULL)";
            string strSql = @"MERGE INTO T_STORYBOARD_DATASET_SETTING A USING ( SELECT " + dtlId + " AS STORYBOARD_DETAIL_ID FROM DUAL) B "
                          + " ON (A.STORYBOARD_DETAIL_ID=B.STORYBOARD_DETAIL_ID AND ROWNUM = 1) "
                          + " WHEN MATCHED THEN UPDATE SET A.DATA_SUMMARY_ID=" + dataSetId
                          + " WHEN NOT MATCHED THEN INSERT (SETTING_ID,STORYBOARD_DETAIL_ID,DATA_SUMMARY_ID,CREATETIME,VERSION, TESTER_ID, RUN_ORDER)"
                          + " VALUES(T_TEST_STEPS_SEQ.NEXTVAL, " + dtlId + " ," + dataSetId + ", SYSDATE, 1, NULL, NULL)";
            try
            {
                using (DbCommand cmmd = cnn.CreateCommand())
                {
                    cmmd.CommandText = strSql;
                    //DbParameter para_STORYBOARD_DETAIL_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_STORYBOARD_DETAIL_ID.ParameterName = "STORYBOARD_DETAIL_ID";
                    //para_STORYBOARD_DETAIL_ID.Value = dtlId;
                    //cmmd.Parameters.Add(para_STORYBOARD_DETAIL_ID);
                    //DbParameter para_DATA_SUMMARY_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_DATA_SUMMARY_ID.ParameterName = "DATA_SUMMARY_ID";
                    //para_DATA_SUMMARY_ID.Value = dataSetId;
                    //cmmd.Parameters.Add(para_DATA_SUMMARY_ID);

                    //DbParameter para_DATA_SUMMARY_IDX = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_DATA_SUMMARY_IDX.ParameterName = "DATA_SUMMARY_IDX";
                    //para_DATA_SUMMARY_IDX.Value = dataSetId;
                    //cmmd.Parameters.Add(para_DATA_SUMMARY_IDX);



                    int iCnt = cmmd.ExecuteNonQuery();
                    Logger.Info("MergStoryboardDtlIdAndDataSetId", string.Format("merged {0} records", iCnt));

                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("MergStoryboardDtlIdAndDataSetId", strError = e.Message, e);
                return false;
            }
        }

        public static bool CleanDuplicatedDatasettings(string strDBIdx, ref int iCleaned, ref string strError)
        {
            DbTransaction dbtrans = null;
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                DbConnection dbCnn = null;
                if ((dbCnn = dbCntx.Database.Connection).State != ConnectionState.Open)
                    dbCnn.Open();
                dbtrans = dbCnn.BeginTransaction();
                string strSql = @"DELETE T_STORYBOARD_DATASET_SETTING
                        WHERE SETTING_ID IN (
                        SELECT A.SETTING_ID
                        FROM T_STORYBOARD_DATASET_SETTING A, 
                            (SELECT STORYBOARD_DETAIL_ID, MAX(SETTING_ID) MX_SETTING_ID, COUNT(*) 
                             FROM T_STORYBOARD_DATASET_SETTING
                             GROUP BY STORYBOARD_DETAIL_ID
                             HAVING COUNT(*)>1 
                            ) B
                        WHERE A.STORYBOARD_DETAIL_ID=B.STORYBOARD_DETAIL_ID
                        AND A.SETTING_ID <> B.MX_SETTING_ID
                        )";
                using (DbCommand dbCmd = dbCnn.CreateCommand())
                {
                    dbCmd.CommandText = strSql;
                    iCleaned = dbCmd.ExecuteNonQuery();
                }
                dbtrans.Commit();
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                Logger.Error("CleanDuplicatedDatasettings", strError, e);
                try
                {
                    if (dbtrans != null)
                        dbtrans.Rollback();
                }
                catch (Exception)
                {


                }
                return false;
            }
        }
    }
}
