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
    public class B_SHARED_OBJECT_POOL : T_SHARED_OBJECT_POOLDTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_SHARED_OBJECT_POOL));
        private const string CNST_SEQ_NAME = "T_TEST_STEPS_SEQ";
        public static B_SHARED_OBJECT_POOL ToBo(T_SHARED_OBJECT_POOL entity)
        {
            if (entity == null) return null;

            var bo = new B_SHARED_OBJECT_POOL();

            bo.OBJECT_POOL_ID = entity.OBJECT_POOL_ID;
            bo.DATA_SUMMARY_ID = entity.DATA_SUMMARY_ID;
            bo.OBJECT_NAME = entity.OBJECT_NAME;
            bo.OBJECT_ORDER = entity.OBJECT_ORDER;
            bo.LOOP_ID = entity.LOOP_ID;
            bo.DATA_VALUE = entity.DATA_VALUE;
            bo.CREATE_TIME = entity.CREATE_TIME;
            bo.VERSION = entity.VERSION;

            return bo;
        }

        public long CreateNewRecorder(long lDataSetId,
            string strObjectName,
            int iObjectOrder,
            int iLoop,
            string dataValue,
            MarsEntities objDbCntx,
            ref bool isOk,
            ref string strError)
        {
            Logger.logBegin("CreateNewRecorder", string.Format("dataSetId:{0} objectName:[{1}] ObjectOrder:[{2}] iLoop:[{3}] dataValue:[{4}] ", lDataSetId, strObjectName, iObjectOrder, iLoop, dataValue));
            try
            {
                T_SHARED_OBJECT_POOLDTO objDto = new T_SHARED_OBJECT_POOLDTO();
                objDto.CREATE_TIME = DateTime.Now;
                objDto.DATA_SUMMARY_ID = lDataSetId;
                objDto.DATA_VALUE = dataValue;
                objDto.LOOP_ID = iLoop;
                objDto.OBJECT_NAME = strObjectName;
                objDto.OBJECT_ORDER = iObjectOrder;
                objDto.OBJECT_POOL_ID = BoHelper.GetIdBySeqName(CNST_SEQ_NAME, objDbCntx);

                objDbCntx.Set<T_SHARED_OBJECT_POOL>();
                objDbCntx.T_SHARED_OBJECT_POOL.Add(objDto.ToEntity());

                isOk = true;
                return objDto.OBJECT_POOL_ID;
            }
            catch (Exception e)
            {
                Logger.Error("CreateNewRecorder", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}InnerException:[{2}] \r\nInner's Inner:[{3}]", e.Message, e.StackTrace,
                    e.InnerException == null ? "" : e.InnerException.Message,
                    e.InnerException == null ? "" : e.InnerException.InnerException == null ? "" : e.InnerException.InnerException.Message), e);
                isOk = false;
                return -1;
            }
            finally
            {
                Logger.logEnd("CreateNewRecorder");
            }
        }

        public static Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>> GetSharePoolInfoByDataSetId(long lDataSetId, DbCommand dbCntx, ref bool isOk, ref string strError)
        {
            try
            {
                dbCntx.Parameters.Clear();
                string strSql = "SELECT * FROM T_SHARED_OBJECT_POOL WHERE DATA_SUMMARY_ID=" + lDataSetId;
                dbCntx.CommandText = strSql;
                var dataReader = dbCntx.ExecuteReader();
                T_SHARED_OBJECT_POOLDTO dtoShare = null;
                List<T_SHARED_OBJECT_POOLDTO> lstResult = new List<T_SHARED_OBJECT_POOLDTO>();
                while (dataReader.Read())
                {
                    dtoShare = new T_SHARED_OBJECT_POOLDTO();
                    dtoShare.OBJECT_POOL_ID = (long)dataReader["OBJECT_POOL_ID"];
                    dtoShare.DATA_SUMMARY_ID = dataReader["DATA_SUMMARY_ID"] as Nullable<Int64>;
                    dtoShare.OBJECT_NAME = dataReader["OBJECT_NAME"] as string;
                    dtoShare.OBJECT_ORDER = dataReader["OBJECT_ORDER"] as Nullable<Int64>;
                    dtoShare.LOOP_ID = dataReader["LOOP_ID"] as Nullable<Int64>;
                    dtoShare.DATA_VALUE = dataReader["DATA_VALUE"] as string;
                    dtoShare.VERSION = dataReader["VERSION"] as Nullable<Int64>;

                    lstResult.Add(dtoShare);
                }
                dataReader.Close();
                isOk = true;
                return lstResult.GroupBy(p => p.OBJECT_NAME).ToDictionary(p => p.Key, p => p.OrderBy(z => z.OBJECT_ORDER).ToList());
            }
            catch (Exception e)
            {
                Logger.Error("GetSharePoolInfoByDataSetId", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                isOk = false;
                return null;
            }
        }

        public static Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>> GetSharePoolInfoByDataSetId(long lDataSetId, MarsEntities dbCntx, ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetSharePoolInfoByDataSetId");
            try
            {
                Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>> dicResult = new Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>>();
                var dp = from p in dbCntx.T_SHARED_OBJECT_POOL
                         where p.DATA_SUMMARY_ID == lDataSetId
                         select p;

                var groupedDp = dp.GroupBy(p => p.OBJECT_NAME).ToDictionary(p => p.Key, p => p.OrderBy(z => z.OBJECT_ORDER).ToDTOs());
                isOk = true;
                return groupedDp;
            }
            catch (Exception e)
            {
                Logger.Error("GetSharePoolInfoByDataSetId", strError = string.Format("Exception:[{0}] StackTrace:[{1}]", e.Message, e.StackTrace), e);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("GetSharePoolInfoByDataSetId");
            }
        }

        public bool updateRecordwithNewData(T_SHARED_OBJECT_POOLDTO objTarget, string strNewData, MarsEntities dbCntx, ref string strError)
        {
            Logger.logBegin("updateRecordwithNewData", string.Format("target id:[{1}--{0}] new Data:[{2}]",
                objTarget.OBJECT_POOL_ID,
                objTarget.OBJECT_NAME, strNewData));
            try
            {
                dbCntx.Set<T_SHARED_OBJECT_POOL>();
                T_SHARED_OBJECT_POOL objTargetPool = null;
                objTargetPool = dbCntx.T_SHARED_OBJECT_POOL.Where(p => p.OBJECT_POOL_ID == objTarget.OBJECT_POOL_ID).FirstOrDefault();
                if (objTargetPool == null)
                {
                    strError = string.Format("cant find object from database T_SHARED_OBJECT_POOL, Pool_id:[{0}]", objTarget.OBJECT_POOL_ID);
                    return false;
                }
                dbCntx.T_SHARED_OBJECT_POOL.Attach(objTargetPool);
                objTargetPool.DATA_VALUE = strNewData;

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateRecordwithNewData", strError = string.Format("Exception :[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("updateRecordwithNewData");
            }

        }

        public long CreateNewRecorder(long dATA_SUMMARY_ID, string strDataObjectName, int iOrd, int iLoop, string dataValue, DbCommand dbCmmd, ref bool isOk, ref string strError)
        {
            string strSql = @"INSERT INTO T_SHARED_DATA_POOL(OBJECT_POOL_ID, DATA_SUMMARY_ID,OBJECT_NAME,OBJECT_ORDER, LOOP_ID,DATA_VALUE ) 
                             VALUES(:OBJECT_POOL_ID, :DATA_SUMMARY_ID,:OBJECT_NAME,:OBJECT_ORDER, 1,:DATA_VALUE)";
            try
            {
                dbCmmd.Parameters.Clear();
                dbCmmd.CommandText = strSql;

                long poolId = BoHelper.GetBussinessSeq(CNST_SEQ_NAME, dbCmmd, ref strError, ref isOk);
                if (!isOk) return -1;

                DbParameter paraOBJECT_POOL_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraOBJECT_POOL_ID.ParameterName = "OBJECT_POOL_ID";
                paraOBJECT_POOL_ID.Value = poolId;
                DbParameter paraDATA_SUMMARY_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraOBJECT_POOL_ID.ParameterName = "DATA_SUMMARY_ID";
                paraOBJECT_POOL_ID.Value = dATA_SUMMARY_ID;
                DbParameter paraOBJECT_NAMED = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraOBJECT_NAMED.ParameterName = "OBJECT_NAME";
                paraOBJECT_NAMED.Value = strDataObjectName;
                DbParameter paraOBJECT_ORDER = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraOBJECT_ORDER.ParameterName = "OBJECT_ORDER";
                paraOBJECT_ORDER.Value = iOrd;
                DbParameter paraDATA_VALUE = new Oracle.ManagedDataAccess.Client.OracleParameter();
                paraDATA_VALUE.ParameterName = "DATA_VALUE";
                paraDATA_VALUE.Value = dataValue;

                dbCmmd.Parameters.Add(paraOBJECT_POOL_ID);
                dbCmmd.Parameters.Add(paraDATA_SUMMARY_ID);
                dbCmmd.Parameters.Add(paraOBJECT_NAMED);
                dbCmmd.Parameters.Add(paraOBJECT_ORDER);
                dbCmmd.Parameters.Add(paraDATA_VALUE);

                dbCmmd.ExecuteNonQuery();
                isOk = true;
                return poolId;
            }
            catch (Exception e)
            {
                Logger.Error("CreateNewRecorder", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                isOk = false;
                return -1;
            }
        }
    }
}
