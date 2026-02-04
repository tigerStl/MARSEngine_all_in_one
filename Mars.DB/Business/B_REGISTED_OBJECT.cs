using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.Objects;
using System.Linq;
using System.Threading;
using System.Transactions;

namespace Mars.message.Business
{
    public class B_REGISTED_OBJECT : T_REGISTED_OBJECTDTO, INotifyPropertyChanged
    {
        public const string SEQ_MARS_OBJECT_ID = "SEQ_MARS_OBJECT_ID";
#if v_16AndUp
        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;


        private string object_happy_name;
        public string OBJECT_HAPPY_NAME
        {
            get { return object_happy_name; }
            set
            {
                object_happy_name = value;
                RaisePropertyChanged("OBJECT_HAPPY_NAME");
            }
        }

        public override Int64 OBJECT_ID { get { return base.OBJECT_ID; } set { base.OBJECT_ID = value; RaisePropertyChanged("OBJECT_ID"); } }

        public override Nullable<Int64> APPLICATION_ID
        {
            get { return base.APPLICATION_ID; }
            set { base.APPLICATION_ID = value; RaisePropertyChanged("APPLICATION_ID"); }
        }
        public override Nullable<Int64> TYPE_ID
        {
            get { return base.TYPE_ID; }
            set { base.TYPE_ID = value; RaisePropertyChanged("TYPE_ID"); }
        }
        public override String QUICK_ACCESS
        {
            get { return base.QUICK_ACCESS; }
            set { base.QUICK_ACCESS = value; RaisePropertyChanged("QUICK_ACCESS"); }
        }
        public override String OBJECT_TYPE
        {
            get { return base.OBJECT_TYPE; }
            set { base.OBJECT_TYPE = value; RaisePropertyChanged("OBJECT_TYPE"); }
        }
        public override String COMMENT
        {
            get { return base.COMMENT; }
            set { base.COMMENT = value; RaisePropertyChanged("COMMENT"); }
        }
        public override String ENUM_TYPE
        {
            get { return base.ENUM_TYPE; }
            set { base.ENUM_TYPE = value; RaisePropertyChanged("ENUM_TYPE"); }
        }

        public bool IsAutoCheckErrorValue
        {
            get
            {
                return this.IS_CHECKERROR_OBJ.HasValue ? (this.IS_CHECKERROR_OBJ.Value == 1 ? true : false) : false;
            }
            set
            {
                if (this.IS_CHECKERROR_OBJ == null)
                {
                    if (value)
                        this.IS_CHECKERROR_OBJ = 1;
                    else
                        this.IS_CHECKERROR_OBJ = 0;
                }
                else
                {
                    bool tmpV = this.IS_CHECKERROR_OBJ.Value == 1 ? true : false;
                    if (tmpV == value) return;
                    this.IS_CHECKERROR_OBJ = (short)(value ? 1 : 0);
                }

                RaisePropertyChanged("IsAutoCheckErrorValue");

            }
        }

        private long newAppId = -1;
        public long NewAppId
        {
            get { return newAppId == -1 ? (this.APPLICATION_ID ?? -1) : newAppId; }
            set { newAppId = value; }
        }
        private long newPegNameId = -1;
        public long NewPegNameId
        {
            get { return newPegNameId; }
            set { newPegNameId = value; }
        }
        private long newObjectNameId = -1;
        public long NewObjectNameId
        {
            get { return newObjectNameId; }
            set { newObjectNameId = value; }
        }

        private long newObjectRegTableId = -1;
        public long NewObjectRegTableId
        {
            get { return newObjectRegTableId; }
            set { newObjectRegTableId = value; }
        }

        private string newHappyName = null;
        public string NewHappyName
        {
            get { return newHappyName; }
            set { newHappyName = value; }
        }

        public B_REGISTED_OBJECT getShallowColone()
        {
            return (B_REGISTED_OBJECT)this.MemberwiseClone();
        }

#endif
        //public Int64 OBJECT_ID { get; set; }
        //public String OBJECT_HAPPY_NAME { get; set; }
        //public Nullable<Int64> APPLICATION_ID { get; set; }
        //public Nullable<Int64> TYPE_ID { get; set; }
        //public String QUICK_ACCESS { get; set; }
        //public String OBJECT_TYPE { get; set; }
        //public String COMMENT { get; set; }
        //public String ENUM_TYPE { get; set; }

        public T_GUI_COMPONENT_TYPE_DIC T_GUI_COMPONENT_TYPE_DIC { get; set; }


        public static bool UpdateMaterializedViews(string strDBIdx, MarsEntities enities, ref string strError)
        {
            MarsEntities dbCntx = enities ?? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                if (dbCntx.Database.Connection.State != ConnectionState.Open)
                    dbCntx.Database.Connection.Open();
                DbCommand cmmd = dbCntx.Database.Connection.CreateCommand();
                UpdateMaterializedViews(cmmd, ref strError);
                cmmd = null;
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception [{0}]", e.Message);
                return false;
            }
        }
        private static bool UpdateMaterializedViews(DbCommand dbCmmd, ref string strError)
        {
            strError = "DbCommand is null";
            if (dbCmmd == null) return true;

            try
            {
                //strSql = "ALTER MATERIALIZED VIEW MV_OBJECT_SNAPSHOT COMPILE";
                //dbCmmd.CommandText = strSql;
                //dbCmmd.ExecuteNonQuery();
                string strSql = "ALTER MATERIALIZED VIEW V_OBJECT_SNAPSHOT COMPILE";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();
                //strSql = "ALTER MATERIALIZED VIEW MV_LAST_TC_INFO COMPILE";
                //dbCmmd.CommandText = strSql;
                //dbCmmd.ExecuteNonQuery();
                strSql = "ALTER MATERIALIZED VIEW MV_STORYBOARD_LATEST COMPILE";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {

                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lSourceAppId"></param>
        /// <param name="lTargetAppId"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public static bool CopyObjectsFromAppliationToApplication(string strDBIdx, long lSourceAppId, long lTargetAppId, ref string strError)
        {
            Logger.logBegin("CopyObjectsFromAppliationToApplication", string.Format("From Applicationid:[{0}]-To [{1}]", lSourceAppId, lTargetAppId));
            System.Data.Common.DbTransaction trans = null;
            try
            {
                MarsEntities dbCntxt = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                System.Data.Common.DbConnection dbCnn = dbCntxt.Database.Connection;
                if (dbCnn.State != System.Data.ConnectionState.Open)
                {
                    dbCnn.Open();
                }
                trans = dbCnn.BeginTransaction();
                System.Data.Common.DbCommand dbCmmd = dbCnn.CreateCommand();
                try
                {
                    // to make sure there is a table 
                    string strTmpTable = @"DROP TABLE TMP_T_OBJ";
                    dbCmmd.CommandText = strTmpTable;
                    dbCmmd.ExecuteNonQuery();
                }
                catch (Exception)
                {
                }
                try
                {
                    // to make sure there is a table 
                    string strTmpTable = @"CREATE TABLE TMP_T_OBJ AS SELECT OBJECT_ID, APPLICATION_ID, 
                                        TYPE_ID, QUICK_ACCESS, OBJECT_TYPE, ""COMMENT"",ENUM_TYPE, OBJECT_NAME_ID，
                                        OBJECT_HAPPY_NAME, OBJ_DATA_SRC, IS_CHECKERROR_OBJ, OBJECT_ID OLD_OBJID FROM T_REGISTED_OBJECT WHERE 1=2";
                    dbCmmd.CommandText = strTmpTable;
                    dbCmmd.ExecuteNonQuery();
                }
                catch (Exception)
                {
                }
                string strSql = "TRUNCATE TABLE TMP_T_OBJ";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();

                strSql = string.Format(@"insert into TMP_T_OBJ(OBJECT_ID, APPLICATION_ID, TYPE_ID, QUICK_ACCESS, OBJECT_TYPE, ""COMMENT"", ENUM_TYPE, OBJECT_NAME_ID, OBJECT_HAPPY_NAME, OBJ_DATA_SRC, IS_CHECKERROR_OBJ,OLD_OBJID)
select {2}.NEXTVAL, {0}, TYPE_ID, QUICK_ACCESS, OBJECT_TYPE, ""COMMENT"", ENUM_TYPE,OBJECT_NAME_ID, OBJECT_HAPPY_NAME, OBJ_DATA_SRC, IS_CHECKERROR_OBJ,OBJECT_ID
FROM T_REGISTED_OBJECT
WHERE
   APPLICATION_ID = {1}", lTargetAppId, lSourceAppId, SEQ_MARS_OBJECT_ID);
                dbCmmd.CommandText = strSql;
                int iCnt = dbCmmd.ExecuteNonQuery();

                ///先复制数据到注册对象表中
                strSql = @"INSERT INTO T_REGISTED_OBJECT(OBJECT_ID, APPLICATION_ID, TYPE_ID, QUICK_ACCESS, OBJECT_TYPE, ""COMMENT"", ENUM_TYPE,OBJECT_NAME_ID, OBJECT_HAPPY_NAME, OBJ_DATA_SRC, IS_CHECKERROR_OBJ) SELECT OBJECT_ID, APPLICATION_ID, TYPE_ID, QUICK_ACCESS, OBJECT_TYPE, ""COMMENT"", ENUM_TYPE, OBJECT_NAME_ID,OBJECT_HAPPY_NAME, OBJ_DATA_SRC, IS_CHECKERROR_OBJ FROM TMP_T_OBJ";
                dbCmmd.CommandText = strSql;
                iCnt = dbCmmd.ExecuteNonQuery();
                strError = string.Format("{0} records are inserted", iCnt);
                ///处理所有的test case,没有必要
                /// 
                //strSql = string.Format(@"INSERT INTO T_TEST_STEPS(STEPS_ID, RUN_ORDER,KEY_WORD_ID,TEST_CASE_ID,OBJECT_ID, COLUMN_ROW_SETTING, VALUE_SETTING,""COMMENT"", IS_RUNNABLE, OBJECT_NAME_ID)
                //           SELECT  T_TEST_STEPS_SEQ.NEXTVAL, STP.RUN_ORDER,STP.KEY_WORD_ID,STP.TEST_CASE_ID,O.OBJECT_ID, STP.COLUMN_ROW_SETTING, STP.VALUE_SETTING,STP.""COMMENT"", STP.IS_RUNNABLE, STP.OBJECT_NAME_ID
                //           FROM T_TEST_STEPS STP, TMP_T_OBJ O 
                //           WHERE O.APPLICATION_ID={0} AND O.OLD_OBJID=STP.OBJECT_ID", lTargetAppId);
                //dbCmmd.CommandText = strSql;
                //iCnt = dbCmmd.ExecuteNonQuery();
                //strError = string.Format("{0} and {1} test steps is created for new applications", strError, iCnt);

                strSql = "TRUNCATE TABLE TMP_T_OBJ";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();
                //strSql = "DROP TABLE TMP_T_OBJ";
                //dbCmmd.CommandText = strSql;
                trans.Commit();


                //strSql = "ALTER MATERIALIZED VIEW MV_OBJECT_SNAPSHOT COMPILE";
                //dbCmmd.CommandText = strSql;
                //dbCmmd.ExecuteNonQuery();
                UpdateMaterializedViews(dbCmmd, ref strError);
                #region replaced by method
                //strSql = "ALTER MATERIALIZED VIEW V_OBJECT_SNAPSHOT COMPILE";
                //dbCmmd.CommandText = strSql;
                //dbCmmd.ExecuteNonQuery();
                ////strSql = "ALTER MATERIALIZED VIEW MV_LAST_TC_INFO COMPILE";
                ////dbCmmd.CommandText = strSql;
                ////dbCmmd.ExecuteNonQuery();
                //strSql = "ALTER MATERIALIZED VIEW MV_STORYBOARD_LATEST COMPILE";
                //dbCmmd.CommandText = strSql;
                //dbCmmd.ExecuteNonQuery();
                #endregion //replaced by methode
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CopyObjectsFromAppliationToApplication", strError = string.Format("Exception:[{0}]", e.Message), e);
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

        public static bool DeleteObject(string strDBIdx, long objId, long? appId, ref string strError)
        {
            Logger.logBegin("DeleteObject", string.Format("objId:[{0}] application:[{1}]", objId, appId));
            DbTransaction dbTrans = null;
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

                var o = (from q in dbCntx.T_REGISTED_OBJECT
                         where q.APPLICATION_ID == appId
                         && q.OBJECT_ID == objId
                         select q
                        ).FirstOrDefault();
                if (o == null)
                {
                    strError = string.Format("No such object exists!");
                    return false;
                }
                dbCntx.T_REGISTED_OBJECT.Remove(o);
                dbCntx.SaveChanges();

                DbConnection cnn = dbCntx.Database.Connection;
                if (cnn.State != ConnectionState.Open)
                {
                    cnn.Open();
                }
                DbCommand dbCmmd = cnn.CreateCommand();


                string strSql = "ALTER MATERIALIZED VIEW V_OBJECT_SNAPSHOT COMPILE";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();
                //strSql = "ALTER MATERIALIZED VIEW MV_LAST_TC_INFO COMPILE";
                //dbCmmd.CommandText = strSql;
                //dbCmmd.ExecuteNonQuery();
                strSql = "ALTER MATERIALIZED VIEW MV_STORYBOARD_LATEST COMPILE";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();

                //refresh cache
                MarsDBGlobe_Cache.UpdateObjectsCache();

                return true;
            }
            catch (Exception e)
            {

                Logger.Error("DeleteObject", strError = string.Format("exception:[{0}]", e.Message), e);
                if (dbTrans != null)
                {
                    try
                    {
                        dbTrans.Rollback();
                    }
                    catch (Exception)
                    {

                    }
                }
                return false;
            }
            finally
            {
                Logger.logEnd("DeleteObject");
            }
        }

        public static bool CopyObjectsFromAppliationToApplication(string strDBIdx, ObservableCollection<B_REGISTED_OBJECT> lstSourceObjs,
            long lSourceAppId, long lTargetAppId, ref string strError)
        {
            Logger.logBegin("CopyObjectsFromAppliationToApplication", string.Format("From Applicationid:[{0}]-To [{1}] with list source count:[{2}]",
                lSourceAppId, lTargetAppId, lstSourceObjs == null ? 0 : lstSourceObjs.Count));

            System.Data.Common.DbTransaction trans = null;
            try
            {
                MarsEntities dbCntxt = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                System.Data.Common.DbConnection dbCnn = dbCntxt.Database.Connection;
                if (dbCnn.State != System.Data.ConnectionState.Open)
                {
                    dbCnn.Open();
                }
                string strSql = null;
                trans = dbCnn.BeginTransaction();
                using (DbCommand dbCmmd = dbCnn.CreateCommand())
                {
                    foreach (var itm in lstSourceObjs)
                    {
                        if (itm == null) continue;
                        strSql = string.Format(@"insert into T_REGISTED_OBJECT(OBJECT_ID, APPLICATION_ID, TYPE_ID, QUICK_ACCESS, OBJECT_TYPE, ""COMMENT"", ENUM_TYPE, OBJECT_NAME_ID, OBJECT_HAPPY_NAME, OBJ_DATA_SRC, IS_CHECKERROR_OBJ)
VALUES({0}.NEXTVAL, :paraAPPLICATION_ID, :paraTYPE_ID, :paraQUICK_ACCESS, :paraOBJECT_TYPE, :paraCOMMENT, :paraENUM_TYPE,:paraOBJECT_NAME_ID, :paraOBJECT_HAPPY_NAME, :paraOBJ_DATA_SRC, :paraIS_CHECKERROR_OBJ)", SEQ_MARS_OBJECT_ID, lTargetAppId);
                        dbCmmd.CommandText = strSql;
                        DbParameter paraAPPLICATION_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraAPPLICATION_ID.ParameterName = "paraAPPLICATION_ID";
                        paraAPPLICATION_ID.Value = lTargetAppId;
                        DbParameter paraTYPE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraTYPE_ID.ParameterName = "paraTYPE_ID";
                        paraTYPE_ID.Value = itm.TYPE_ID;
                        DbParameter paraQUICK_ACCESS = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraQUICK_ACCESS.ParameterName = "paraQUICK_ACCESS";
                        paraQUICK_ACCESS.Value = itm.QUICK_ACCESS;
                        DbParameter paraOBJECT_TYPE = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraOBJECT_TYPE.ParameterName = "paraOBJECT_TYPE";
                        paraOBJECT_TYPE.Value = itm.OBJECT_TYPE;
                        DbParameter paraCOMMENT = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraCOMMENT.ParameterName = "paraCOMMENT";
                        paraCOMMENT.Value = itm.COMMENT;
                        DbParameter paraENUM_TYPE = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraENUM_TYPE.ParameterName = "paraENUM_TYPE";
                        paraENUM_TYPE.Value = itm.ENUM_TYPE;
                        DbParameter paraOBJECT_NAME_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraOBJECT_NAME_ID.ParameterName = "paraOBJECT_NAME_ID";
                        paraOBJECT_NAME_ID.Value = itm.OBJECT_NAME_ID;
                        DbParameter paraOBJECT_HAPPY_NAME = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraOBJECT_HAPPY_NAME.ParameterName = "paraOBJECT_HAPPY_NAME";
                        paraOBJECT_HAPPY_NAME.Value = itm.OBJECT_HAPPY_NAME;
                        DbParameter paraOBJ_DATA_SRC = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraOBJ_DATA_SRC.ParameterName = "paraOBJ_DATA_SRC";
                        paraOBJ_DATA_SRC.Value = itm.OBJ_DATA_SRC;
                        DbParameter paraIS_CHECKERROR_OBJ = new Oracle.ManagedDataAccess.Client.OracleParameter();
                        paraIS_CHECKERROR_OBJ.ParameterName = "paraIS_CHECKERROR_OBJ";
                        paraIS_CHECKERROR_OBJ.Value = itm.IS_CHECKERROR_OBJ;
                        dbCmmd.Parameters.Clear();
                        dbCmmd.Parameters.Add(paraAPPLICATION_ID);
                        dbCmmd.Parameters.Add(paraTYPE_ID); 
                        dbCmmd.Parameters.Add(paraQUICK_ACCESS);
                        dbCmmd.Parameters.Add(paraOBJECT_TYPE);
                        dbCmmd.Parameters.Add(paraCOMMENT);
                        dbCmmd.Parameters.Add(paraENUM_TYPE);
                        dbCmmd.Parameters.Add(paraOBJECT_NAME_ID);
                        dbCmmd.Parameters.Add(paraOBJECT_HAPPY_NAME);
                        dbCmmd.Parameters.Add(paraOBJ_DATA_SRC);
                        dbCmmd.Parameters.Add(paraIS_CHECKERROR_OBJ);

                        int iInsertCnt = dbCmmd.ExecuteNonQuery();

                    }
                    trans.Commit();
                    //strSql = "ALTER MATERIALIZED VIEW MV_OBJECT_SNAPSHOT COMPILE";
                    //dbCmmd.CommandText = strSql;
                    //dbCmmd.ExecuteNonQuery();
                    strSql = "ALTER MATERIALIZED VIEW V_OBJECT_SNAPSHOT COMPILE";
                    dbCmmd.CommandText = strSql;
                    dbCmmd.ExecuteNonQuery();
                    strSql = "ALTER MATERIALIZED VIEW MV_LAST_TC_INFO COMPILE";
                    dbCmmd.CommandText = strSql;
                    dbCmmd.ExecuteNonQuery();
                    strSql = "ALTER MATERIALIZED VIEW MV_STORYBOARD_LATEST COMPILE";
                    dbCmmd.CommandText = strSql;
                    dbCmmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("CopyObjectsFromAppliationToApplication", strError = string.Format("Exception:[{0}]", e.Message), e);
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

        public static bool UpdateObject(long lObjId, string quickAcess, DbCommand dbCmmd, ref string strError)
        {
            Logger.logBegin("UpdateObject", string.Format("objId:[{0}] Identifier:[{1}]", lObjId, quickAcess));
            try
            {
                dbCmmd.Parameters.Clear();
                string strSqlUpdate = @"UPDATE T_REGISTED_OBJECT SET QUICK_ACCESS=:QUICK_ACCESS WHERE OBJECT_ID=:OBJECT_ID";
                DbParameter OBJECT_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                OBJECT_ID.ParameterName = "OBJECT_ID";
                OBJECT_ID.Value = lObjId;
                DbParameter QUICK_ACCESS = new Oracle.ManagedDataAccess.Client.OracleParameter();
                QUICK_ACCESS.ParameterName = "QUICK_ACCESS";
                QUICK_ACCESS.Value = quickAcess;

                dbCmmd.CommandText = strSqlUpdate;
                dbCmmd.Parameters.Add(OBJECT_ID);
                dbCmmd.Parameters.Add(QUICK_ACCESS);

                dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateObject", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public B_REGISTED_OBJECT CreateNewObjectByCommand(string strObjectName, string quickAcess, long oBJECT_NAME_ID, long aPPLICATION_ID,
            string strPegName, long typId, string desc, string dataSrc, int isCheckErrorObj,
            DbCommand dbCmmd, ref bool isOk, ref string strError)
        {
            Logger.logBegin("CreateNewObjectByCommand", string.Format("quick access:[{0}], name id:[{1}] appId:[{2}]", quickAcess, oBJECT_NAME_ID, aPPLICATION_ID));
            long lNewObjId = BoHelper.GetBussinessSeq(SEQ_MARS_OBJECT_ID, dbCmmd, ref strError, ref isOk);
            if (!isOk) return null;
            string strSqlInsert = @"INSERT INTO T_REGISTED_OBJECT(OBJECT_ID, APPLICATION_ID,TYPE_ID,QUICK_ACCESS,OBJECT_TYPE,""COMMENT"",OBJECT_NAME_ID,IS_CHECKERROR_OBJ)
                                                        VALUES(:OBJECT_ID, :APPLICATION_ID,:TYPE_ID,:QUICK_ACCESS,:OBJECT_TYPE,:COMMENTx,:OBJECT_NAME_ID,:IS_CHECKERROR_OBJ)";
            dbCmmd.CommandText = strSqlInsert;
            try
            {
                dbCmmd.Parameters.Clear();

                DbParameter pmOBJECT_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                pmOBJECT_ID.ParameterName = "OBJECT_ID";
                pmOBJECT_ID.Value = lNewObjId;
                DbParameter pmAPPLICATION_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                pmAPPLICATION_ID.ParameterName = "APPLICATION_ID";
                pmAPPLICATION_ID.Value = aPPLICATION_ID;
                DbParameter pmQUICK_ACCESS = new Oracle.ManagedDataAccess.Client.OracleParameter();
                pmQUICK_ACCESS.ParameterName = "QUICK_ACCESS";
                pmQUICK_ACCESS.Value = quickAcess;
                DbParameter pmTYPE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                pmTYPE_ID.ParameterName = "TYPE_ID";
                pmTYPE_ID.Value = typId;
                DbParameter pmOBJECT_TYPE = new Oracle.ManagedDataAccess.Client.OracleParameter();
                pmOBJECT_TYPE.ParameterName = "OBJECT_TYPE";
                pmOBJECT_TYPE.Value = strPegName;
                DbParameter pmCOMMENT = new Oracle.ManagedDataAccess.Client.OracleParameter();
                pmCOMMENT.ParameterName = "COMMENTx";
                pmCOMMENT.Value = desc;
                DbParameter pmOBJECT_NAME_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                pmOBJECT_NAME_ID.ParameterName = "OBJECT_NAME_ID";
                pmOBJECT_NAME_ID.Value = oBJECT_NAME_ID;
                //DbParameter pmOBJ_DATA_SRC = new Oracle.ManagedDataAccess.Client.OracleParameter();
                //pmOBJ_DATA_SRC.ParameterName = "OBJ_DATA_SRC";
                //pmOBJ_DATA_SRC.Value = string.IsNullOrEmpty(dataSrc) ? null : dataSrc.Select(p => (byte)p).ToArray();
                DbParameter pmIS_CHECKERROR_OBJ = new Oracle.ManagedDataAccess.Client.OracleParameter();
                pmIS_CHECKERROR_OBJ.ParameterName = "IS_CHECKERROR_OBJ";
                pmIS_CHECKERROR_OBJ.Value = isCheckErrorObj;

                dbCmmd.Parameters.Add(pmOBJECT_ID);
                dbCmmd.Parameters.Add(pmAPPLICATION_ID);
                dbCmmd.Parameters.Add(pmQUICK_ACCESS);
                dbCmmd.Parameters.Add(pmTYPE_ID);
                dbCmmd.Parameters.Add(pmOBJECT_TYPE);
                dbCmmd.Parameters.Add(pmCOMMENT);
                dbCmmd.Parameters.Add(pmOBJECT_NAME_ID);
                //dbCmmd.Parameters.Add(pmOBJ_DATA_SRC);
                dbCmmd.Parameters.Add(pmIS_CHECKERROR_OBJ);

                int iRslt = dbCmmd.ExecuteNonQuery();
                isOk = true;

                return new B_REGISTED_OBJECT()
                {
                    OBJECT_ID = lNewObjId,
                    QUICK_ACCESS = quickAcess,
                    APPLICATION_ID = aPPLICATION_ID,
                    OBJECT_NAME_ID = oBJECT_NAME_ID,
                    TYPE_ID = typId,
                    OBJECT_TYPE = strPegName,
                    COMMENT = desc,
                    OBJ_DATA_SRC = string.IsNullOrEmpty(dataSrc) ? null : dataSrc.Select(p => (byte)p).ToArray(),
                    IS_CHECKERROR_OBJ = (short)isCheckErrorObj,
                    OBJECT_HAPPY_NAME = strObjectName
                };
            }
            catch (Exception e)
            {
                Logger.Error("CreateNewObjectByCommand", strError = string.Format("Exception:[{0}] stackProcess:[{1}]", e.Message, e.StackTrace), e);
                isOk = false;
                return null;
            }
        }

        #region Old Code
        //public List<T_REGISTED_OBJECTDTO> GetReistedObjects(List<long> typeIdList, List<long> appIds)
        //{
        //    MarsEntities marsEntities =BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
        //    List<T_REGISTED_OBJECTDTO> regObjectList = new List<T_REGISTED_OBJECTDTO>();

        //    var registeredObjects  = (from c in marsEntities.T_REGISTED_OBJECT
        //                     where (typeIdList.Contains((long)c.TYPE_ID ) && appIds.Contains((long)c.APPLICATION_ID))
        //                     orderby c.OBJECT_HAPPY_NAME ascending
        //                     select c);

        //    foreach (T_REGISTED_OBJECT RegObj in registeredObjects)
        //    {
        //        regObjectList.Add(T_REGISTED_OBJECTAssembler.ToDTO(RegObj));
        //    }
        //    return regObjectList;
        //}
        #endregion //Old Code 


        public static List<T_REGISTED_OBJECTDTO> LoadObjectsFromDbByAppliationId(string strDBIdx, 
            long lAppId, ref bool isOk, ref string strError)
        {
            Logger.logBegin("LoadObjectsFromDbByAppliationId");
            try
            {
                MarsEntities dbCntxt = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var o = (from q in dbCntxt.T_REGISTED_OBJECT
                         where q.APPLICATION_ID == lAppId
                         select q).OrderBy(p => new { p.OBJECT_TYPE, p.OBJECT_NAME_ID });
                isOk = true;
                return o.ToDTOs();
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("LoadObjectsFromDbByAppliationId", strError = string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
            finally
            {
                Logger.logEnd("LoadObjectsFromDbByAppliationId");
            }
        }

#if v_16AndUp
        public List<B_REGISTED_OBJECT> GetRegistedObjects(string strDBIdx, List<long> typeIdList, List<long> appIds)
#else
        public List<T_REGISTED_OBJECTDTO> GetRegistedObjects(List<long> typeIdList, List<long> appIds)
#endif
        {
            Logger.Info("GetRegistedObjects", string.Format("typeIdList:[{0}], appIds:[{1}]", typeIdList, appIds));
#if v_16AndUp
            List<B_REGISTED_OBJECT> lstResult = new List<B_REGISTED_OBJECT>();
            IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>> eApp_Obj = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS)
                .GetCachedObjctAs<IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>>>(strDBIdx, appIds);
            //Logger.Info("GetRegistedObjects---Performance", string.Format("typeIdList:[{0}], appIds:[{1}]", typeIdList, appIds));
            if (eApp_Obj == null) return lstResult;

            foreach (KeyValuePair<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> appObjItm in eApp_Obj)
            {
                var tmpLst = appObjItm.Value.Where(p => typeIdList.Contains(p.TYPE_ID ?? -1));
                if (tmpLst == null) continue;
                lstResult.AddRange(tmpLst);
            }

            return lstResult;
#else
            List<T_REGISTED_OBJECTDTO> lstResult = new List<T_REGISTED_OBJECTDTO>();
            IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>>  eApp_Obj = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS)
                .GetCachedObjctAs<IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>>>(appIds);

            Logger.Info("GetRegistedObjects---Performance", string.Format("typeIdList:[{0}], appIds:[{1}]", typeIdList, appIds));
            if (eApp_Obj == null) return lstResult;

            foreach(KeyValuePair<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> appObjItm in eApp_Obj)
            {
                var tmpLst = appObjItm.Value.Where(p => typeIdList.Contains(p.TYPE_ID??-1));
                if (tmpLst == null) continue;
                lstResult.AddRange(tmpLst);
            }
            return lstResult;
#endif
        }

        public bool updateObjectDataSource(string strDBIdx, long objectId, byte[] arrData, ref string strError)
        {
            Logger.logBegin("updateObjectDataSource", string.Format("objectId:[{0}] xmldata Length:[{1}]", objectId, arrData == null ? 0 : arrData.Length));
            MarsEntities objEntitis = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var o = (from q in objEntitis.T_REGISTED_OBJECT
                         where q.OBJECT_ID == objectId
                         select q).FirstOrDefault();
                if (o == null)
                {
                    Logger.Error("updateObjectDataSource", strError = string.Format("No such object Id exists:[{0}]", objectId));
                    return false;
                }
                o.OBJ_DATA_SRC = arrData;

                objEntitis.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateObjectDataSource", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        /// <summary>
        /// 注意：这里返回的数据结构中，key，既是peg。返回的object id应该是无效的。只是注重pegName和objectNameId
        /// </summary>
        /// <param name="lstPegNames"></param>
        /// <param name="applicationIds"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns>peg 和其对象</returns>
        public Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> GetObjectsByPegAndAppIds(string strDBIdx, 
            List<string> lstPegNames, List<long> applicationIds, ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetObjectsByPegAndAppIds", string.Format("Pegs:[{0}] AppIds:[{1}]", lstPegNames == null ? "N/A" : string.Join(",", lstPegNames),
                applicationIds == null ? "N/A" : string.Join(",", applicationIds.ToArray())));
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> dictionaryResult = new Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>>();
            if (applicationIds == null)
            {
                isOk = false;
                Logger.Error("GetObjectsByPegAndAppIds", strError = "No Application Ids");
                return dictionaryResult;
            }
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var qoTmp = from obj in dbCntx.V_OBJECT_SNAPSHOT
                            where applicationIds.Contains(obj.APPLICATION_ID ?? -1)
                            && lstPegNames.Contains(obj.PEG_NAME)
                            select obj;
                List<V_OBJECT_SNAPSHOT> lstObjSnap = qoTmp.ToList();
                foreach (var objItm in lstObjSnap)
                {
                    if ((objItm == null) || (objItm.PEG_ID == null)) continue;
                    B_REGISTED_OBJECT objPeg = dictionaryResult.Keys.Where(p => p.OBJECT_NAME_ID == objItm.PEG_ID).FirstOrDefault();
                    List<B_REGISTED_OBJECT> lstSubObjs;
                    if (objPeg == null)
                    {
                        objPeg = CreateFromVObjSnap(objItm, null);
                        objPeg.OBJECT_HAPPY_NAME = objPeg.OBJECT_TYPE;
                        objPeg.OBJECT_ID = objItm.OBJECT_ID;
                        objPeg.OBJECT_NAME_ID = objItm.PEG_ID;
                        dictionaryResult.Add(objPeg, lstSubObjs = new List<B_REGISTED_OBJECT>());
                    }
                    else
                    {
                        lstSubObjs = dictionaryResult[objPeg];
                    }
                    lstSubObjs.Add(CreateFromVObjSnap(objItm, null));
                }
                isOk = true;
                return dictionaryResult;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetObjectsByPegAndAppIds", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace));
                return null;
            }
        }

        private long GetObjectTypeId(string strDBIdx, string strObjectType, ref bool isOk, ref string strError)
        {
            ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> lstObjType = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeListEx(strDBIdx);
            if (lstObjType == null)
            {
                Logger.Error("CreateObjectMappingForImport", strError = "Object Type List is null. ");
                isOk = false;
                return -1;
            }
            var oPegTypeId = lstObjType.Where(p => string.Compare(strObjectType, p.TYPE_NAME, true) == 0).FirstOrDefault();
            if (oPegTypeId == null)
            {
                Logger.Error("CreateObjectMappingForImport", strError = "No Pegwindow type in table. ");
                isOk = false;
                return -1;
            }
            isOk = true;
            return oPegTypeId.TYPE_ID;
        }



        public void CreateObjectMappingForImport(
            List<KeyValuePair<long, T_REGISTERED_APPSDTO>> lstMappedApp,
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> lstObjectFromImportXml,
            ref bool isOk,
            ref string strError,
            MarsTransactionMgr objTrans,            
            string strDBIdx ,//= MarsEntitiesExtends.cnst_default_dbName,
            bool isOverrideObject = false
            )
        {
            Logger.logBegin("CreateObjectMappingForImport");
            try
            {
                long lPegType = GetObjectTypeId(strDBIdx,B_GUI_COMPONENT_TYPE_DIC.CNST_PEGWINDOW_TYPE_NAME, ref isOk, ref strError);
                if (!isOk)
                {
                    return;
                }

                ///算法：
                /// 1，获得所有的pegwindow和application
                /// 
                if (objTrans == null)
                {
                    Logger.Error("CreateObjectMappingForImport", strError = "No DB Connection. DB connection is null.");
                    isOk = false;
                    return;
                }
                if (lstObjectFromImportXml == null)
                {
                    Logger.Warnning("CreateObjectMappingForImport", "Object source is empty, Imported test case xml isnot with objects?");
                    isOk = true;
                    return;
                }
                List<string> lstPeg = lstObjectFromImportXml.Select(p => p.Key.OBJECT_HAPPY_NAME).Distinct().ToList();
                //List<long?> lstAppIds = lstObjectFromImportXml.Select(p =>p.Key.APPLICATION_ID).Distinct().ToList();
                List<long?> lstAppIds = lstMappedApp.Select(p => p.Key).Cast<long?>().ToList();

                /// 获得objects information
                /// 
                List<B_V_OBJECT_SNAPSHOT> lstObjFromDB = (new B_V_OBJECT_SNAPSHOT()).FetchObjsByAppIds(lstAppIds, objTrans.CurrentDBContext, 
                    ref isOk, ref strError, strDBIdx);
                if (!isOk)
                {
                    Logger.Error("CreateObjectMappingForImport", string.Format("Error when call (new B_V_OBJECT_SNAPSHOT()).FetchObjsByAppIds \r\n{0}", strError));
                    return;
                }

                /// 这里有几种情况：
                /// 1，pegwindow 不存在，那么需要插入pegwindow object 先
                ///  1.1 可能是针对某个application 不存在
                /// 2，pegwindow 存在，但是quick——access不一样，需要插入一个新的pegwindow，将名称改为：原名称+_imported
                /// 3，pegwindow 存在，object不存在，直接插入新的object
                /// 4，pegwindow 存在，object存在，但是quick——access不一样，将名称改为：原名称+_imported
                /// 无论哪种情况，都需要保留新对象和旧对象的id的map
                /// 这里有个问题:
                /// import的目的是发现原系统的问题。因此，不应该改变现有系统的quick——access
                /// 
                var tmpPegs = from peg in lstObjFromDB
                              where peg.PEG_ID == peg.OBJECT_ID
                              select peg;
                if (tmpPegs == null)
                {
                    Logger.Error("CreateObjectMappingForImport", strError = "No Peg information from DB, check V_OBJECT_SNAPSHOT");
                    isOk = false;
                    return;
                }
                List<B_V_OBJECT_SNAPSHOT> lstPegSnpShtFromDB = tmpPegs.ToList();
                /// 1，pegwindow 不存在，那么需要插入pegwindow object 先，                                
                ///  1.1 可能是针对某(几)个application 不存在
                /// 

                Dictionary<long, B_REGISTED_OBJECT> dicPegNameExistsNoApp = new Dictionary<long, B_REGISTED_OBJECT>();
                List<T_OBJECT_NAMEINFODTO> allObjNames = B_OBJECT_NAMEINFO.GetAllObjectNameInfo(ref strError, ref isOk, strDBIdx);
                if (!isOk)
                {
                    return;
                }
                //Dictionary<long, B_REGISTED_OBJECT> dicPegNameNeedToBeUpdate = new Dictionary<long, B_REGISTED_OBJECT>();
                //var pegExistNoApp = from p in lstPegFromDB
                //                    from a in lstMappedApp
                //                    from itm in lstObjectFromImportXml
                //                    where p.PEG_NAME == itm.OBJECT_HAPPY_NAME
                //                    && a.Key == itm.APPLICATION_ID
                //                    && itm.APPLICATION_ID == p.APPLICATION_ID
                foreach (var itmPegFromXml in lstObjectFromImportXml.Keys)
                {
                    if (itmPegFromXml == null) continue;
                    Logger.Info("CreateObjectMappingForImport", string.Format("object:[{0}] type:[{1}] object Id:[{2}]", 
                        itmPegFromXml.OBJECT_HAPPY_NAME, itmPegFromXml.OBJECT_TYPE, itmPegFromXml.OBJECT_ID));
                    var peg = from p in lstPegSnpShtFromDB
                              where p.PEG_NAME == itmPegFromXml.OBJECT_HAPPY_NAME
                              select p;
                    var objNewAppObj = lstMappedApp.Where(p => p.Value.APPLICATION_ID == itmPegFromXml.NewAppId).FirstOrDefault();
                    if (objNewAppObj.Equals(default(KeyValuePair<long, T_REGISTERED_APPSDTO>)))
                    {
                        //something wrong, 
                        continue;
                    }
                    long lNewAppId = objNewAppObj.Key;
                    long lNewObjId = -1;
                    itmPegFromXml.newAppId = lNewAppId;

                    if (peg.FirstOrDefault() == null)
                    {
                        #region new peg object
                        ///说明无该pegwindow，首先想要创建peg的nameInfo，然后创建RegiteredObject
                        /// 
                        ///在19年4月后，objectNameInfo添加了唯一索引，因此，需要先判断是否存在指定object name
                        ///
                        T_OBJECT_NAMEINFODTO oN = allObjNames.FirstOrDefault(p => string.Compare(p.OBJECT_HAPPY_NAME, itmPegFromXml.OBJECT_HAPPY_NAME) == 0);
                        if (oN == null)
                        {
                            isOk = B_OBJECT_NAMEINFO.CreateObject(strDBIdx,itmPegFromXml.OBJECT_HAPPY_NAME, 
                                itmPegFromXml.QUICK_ACCESS, 1, ref itmPegFromXml.newPegNameId, 
                                ref strError, objTrans.CurrentDBContext);
                            if (!isOk)
                            {
                                Logger.Error("CreateObjectMappingForImport", string.Format("Error when call B_OBJECT_NAMEINFO.CreateObject，pegwindow type\r\n[{0}]", strError));
                                return;
                            }
                        }
                        else
                        {
                            itmPegFromXml.newPegNameId = oN.OBJECT_NAME_ID;
                        }
                        ///然后创建RegiteredObject
                        ///                         
                        isOk = this.CreateNewObject(itmPegFromXml.newPegNameId, itmPegFromXml.OBJECT_HAPPY_NAME, itmPegFromXml.OBJECT_HAPPY_NAME,
                            itmPegFromXml.QUICK_ACCESS, lPegType,
                            lNewAppId, itmPegFromXml.COMMENT, ref strError, ref lNewObjId, 
                            objTrans.CurrentDBContext, strDBIdx);
                        if (isOk)
                        {
                            itmPegFromXml.NewObjectNameId = itmPegFromXml.newPegNameId;
                            itmPegFromXml.NewObjectRegTableId = lNewObjId;
                        }
                        else
                        {
                            Logger.Error("CreateObjectMappingForImport", string.Format("Error when CreateNewObject for Peg:\r\n{0}", strError));
                            return;
                        }
                        //添加到lstPegSnpShtFromDB
                        lstPegSnpShtFromDB.Add(GenPegSnapShotForXmlImport(itmPegFromXml));
                        #endregion //new peg object

                    }
                    else //c存在pegwindow，判断是否存在指定的appid
                    {
                        var app = (from a in peg
                                       //where a.APPLICATION_ID == itm.APPLICATION_ID
                                   where a.APPLICATION_ID == itmPegFromXml.newAppId
                                   select a).FirstOrDefault();
                        if (app == null)
                        {
                            #region exist peg object but no assigned application
                            ///创建registed object
                            /// 
                            Logger.Info("CreateObjectMappingForImport", "不存在pegwindow，存在指定的appid");
                            isOk = this.CreateNewObject(itmPegFromXml.newPegNameId, itmPegFromXml.OBJECT_HAPPY_NAME, 
                                itmPegFromXml.OBJECT_HAPPY_NAME, itmPegFromXml.QUICK_ACCESS, lPegType,
                            lNewAppId, itmPegFromXml.COMMENT, ref strError, ref lNewObjId, objTrans.CurrentDBContext,
                            strDBIdx);
                            if (isOk)
                            {
                                itmPegFromXml.NewObjectNameId = itmPegFromXml.newPegNameId;
                                itmPegFromXml.NewObjectRegTableId = lNewObjId;
                            }
                            else
                            {
                                Logger.Error("CreateObjectMappingForImport", string.Format("Error when CreateNewObject for Peg:\r\n{0}", strError));
                                return;
                            }
                            lstPegSnpShtFromDB.Add(GenPegSnapShotForXmlImport(itmPegFromXml));
                            #endregion //exist peg object but no assigned application
                        }
                        else
                        {
                            Logger.Info("CreateObjectMappingForImport", "c存在pegwindow，存在指定的appid");
                            ///都存在,判断是否quick access不一样
                            /// 
                            if (string.Compare(app.QUICK_ACCESS, itmPegFromXml.QUICK_ACCESS, true) != 0)
                            {
                                /// 需要update xtong 
                                /// 
                                if (isOverrideObject)
                                {
                                    isOk = this.updateSpecialObjectQuickAccess(app.OBJECT_ID, itmPegFromXml.QUICK_ACCESS, objTrans.CurrentDBContext,
                                        ref strError);
                                    if (!isOk)
                                    {
                                        Logger.Error("CreateObjectMappingForImport", string.Format("updateSpecialObjectQuickAccess for [{0}] with error:[{1}]", itmPegFromXml.QUICK_ACCESS, strError));
                                        return;
                                    }
                                    //update special object from db
                                    app.QUICK_ACCESS = itmPegFromXml.QUICK_ACCESS;
                                }
                            }
                            itmPegFromXml.newObjectNameId = app.OBJECT_NAME_ID ?? -1;
                            itmPegFromXml.NewObjectRegTableId = app.OBJECT_ID;
                            itmPegFromXml.newPegNameId = app.OBJECT_NAME_ID ?? -1;
                        }
                        //Thread.Sleep(5);
                    }
                    #region 循环处理子对象
                    List<B_REGISTED_OBJECT> lstChild = lstObjectFromImportXml[itmPegFromXml];
                    if (lstChild == null) continue;
                    if (lstChild.Count <= 0) continue;
                    long lSubObjId = -1, lNewChildId = -1;
                    foreach (var itmChild in lstChild)
                    {
                        ///和pegwindow一样，存在几种类似情况
                        /// 
                        if (itmChild == null) continue;
                        itmChild.newAppId = lNewAppId;
                        var qlDb = lstObjFromDB.Where(p => p.OBJECT_HAPPY_NAME == itmChild.OBJECT_HAPPY_NAME);
                        var q = qlDb.FirstOrDefault();
                        if (q == null)
                        {
                            T_OBJECT_NAMEINFODTO oN = allObjNames.FirstOrDefault(p => string.Compare(p.OBJECT_HAPPY_NAME, itmChild.OBJECT_HAPPY_NAME) == 0);
                            if (oN == null)
                            {
                                //新对象
                                isOk = B_OBJECT_NAMEINFO.CreateObject(strDBIdx,itmChild.OBJECT_HAPPY_NAME, itmChild.COMMENT, 0, ref lSubObjId, ref strError, objTrans.CurrentDBContext);
                                if (!isOk)
                                {
                                    Logger.Error("CreateObjectMappingForImport", string.Format("Error:{0}, when create child object for peg. ObjectName:[{1}]", strError, itmChild.OBJECT_HAPPY_NAME));
                                    return;
                                }
                            }
                            else
                            {
                                lSubObjId = oN.OBJECT_NAME_ID;
                            }

                            itmChild.newObjectNameId = lSubObjId;
                            itmChild.newPegNameId = itmPegFromXml.newObjectNameId;
                            itmChild.newObjectNameId = lSubObjId;

                            //CreateNewObject(long iObjNameId,string strPegWindow, string strHappyName, string strQuickAccess, 
                            //long iTypeId,long iAppId,string strComment, ref string strError, ref long lObjectId,MarsEntities objContext)
                            isOk = CreateNewObject(itmChild.newObjectNameId, itmChild.OBJECT_TYPE, itmChild.OBJECT_HAPPY_NAME, itmChild.QUICK_ACCESS,
                                itmChild.TYPE_ID ?? -1, itmChild.NewAppId, itmChild.COMMENT, 
                                ref strError, ref itmChild.newObjectRegTableId, 
                                objTrans.CurrentDBContext,
                                strDBIdx
                                );
                            if (!isOk)
                            {
                                Logger.Error("CreateObjectMappingForImport", string.Format("Error:[{0}] when call B_REGISTED_OBJECT.CreateObject for [{1}]", strError, itmChild.QUICK_ACCESS));
                                return;
                            }

                            //需要添加到数据库列表中
                            B_V_OBJECT_SNAPSHOT objTmpNewObject = null;
                            lstPegSnpShtFromDB.Add(objTmpNewObject = GenObjSnapShotForXmlImport(itmPegFromXml, itmChild));
                            objTmpNewObject.OBJECT_ID = itmChild.newObjectRegTableId;
                            lstObjFromDB.Add(objTmpNewObject);
                        }
                        else
                        {
                            ///存在名称，继续测试是否和application关联
                            /// 
                            var qAppObjFromDB = qlDb.Where(p => p.APPLICATION_ID == lNewAppId);
                            var qDefaultAppobjFromDB = qAppObjFromDB.FirstOrDefault();
                            if (qDefaultAppobjFromDB == null)
                            {
                                ///不存在，创建新记录
                                /// 
                                isOk = this.CreateNewObject(q.OBJECT_NAME_ID ?? -1, itmPegFromXml.OBJECT_HAPPY_NAME,
                                    itmChild.OBJECT_HAPPY_NAME,
                                    itmChild.QUICK_ACCESS,
                                    itmChild.TYPE_ID ?? 0, //可能有问题
                                    lNewAppId,
                                    string.Format("IMPORTED\r\n{0}", itmChild.COMMENT),
                                    ref strError, ref lNewChildId, 
                                    objTrans.CurrentDBContext,
                                    strDBIdx);
                                if (!isOk)
                                {
                                    Logger.Error("CreateObjectMappingForImport", string.Format("Error when call CreateNewObject:[{0}]", strError));
                                    return;
                                }

                                itmChild.NewObjectNameId = q.OBJECT_NAME_ID ?? -1;
                                itmChild.NewObjectRegTableId = lNewChildId;
                                itmChild.newPegNameId = itmPegFromXml.newObjectNameId;
                                //需要添加到数据库列表中
                                B_V_OBJECT_SNAPSHOT objTmpNewObject = null;
                                lstPegSnpShtFromDB.Add(objTmpNewObject = GenObjSnapShotForXmlImport(itmPegFromXml, itmChild));
                                lstObjFromDB.Add(objTmpNewObject);
                            }
                            else
                            {
                                var qoa = qAppObjFromDB.Where(p => p.QUICK_ACCESS == itmChild.QUICK_ACCESS).FirstOrDefault();
                                if (qoa == null)
                                {
                                    if (!isOverrideObject)
                                    {
                                        ///尽管存在同名，同application的对象，但是quickacess不同，因此创建有个带import日期的同名对象
                                        itmChild.NewHappyName = string.Format("{0}_imp_{1}", itmChild.OBJECT_HAPPY_NAME, (DateTime.Now).ToString("MM/dd/yyyy"));
                                        isOk = B_OBJECT_NAMEINFO.CreateObject(strDBIdx, itmChild.newHappyName, itmChild.COMMENT, 0, ref itmChild.newObjectNameId,
                                            ref strError, objTrans.CurrentDBContext);
                                        if (!isOk)
                                        {
                                            Logger.Error("CreateObjectMappingForImport", string.Format("Error when call B_OBJECT_NAMEINFO.CreateObject\r\n{0}", strError));
                                            return;
                                        }
                                        isOk = this.CreateNewObject(itmChild.newObjectNameId, itmPegFromXml.NewHappyName ?? itmPegFromXml.OBJECT_HAPPY_NAME, itmChild.NewHappyName, itmChild.QUICK_ACCESS,
                                            itmChild.TYPE_ID ?? -1,
                                            lNewAppId,
                                            string.Format("IMPORTED\r\n{0}", itmChild.COMMENT),
                                            ref strError, ref itmChild.newObjectRegTableId,
                                            objTrans.CurrentDBContext,
                                            strDBIdx);
                                        if (!isOk)
                                        {
                                            Logger.Error("CreateObjectMappingForImport", string.Format("Error when call CreateNewObject\r\n{0}", strError));
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        /// 更新 覆盖模式，将更新object数据
                                        /// 
                                        isOk = this.updateSpecialObjectQuickAccess(qDefaultAppobjFromDB.OBJECT_ID, itmChild.QUICK_ACCESS, objTrans.CurrentDBContext, ref strError);
                                        if (!isOk)
                                        {
                                            Logger.Error("CreateObjectMappingForImport", string.Format("Update object QuickAccess with error:[{0}]", strError));
                                            return;
                                        }
                                        itmChild.newObjectRegTableId = qDefaultAppobjFromDB.OBJECT_ID;
                                        itmChild.newObjectNameId = qDefaultAppobjFromDB.OBJECT_NAME_ID ?? -2;
                                        itmChild.newPegNameId = itmPegFromXml.newPegNameId;
                                        qDefaultAppobjFromDB.QUICK_ACCESS = itmChild.QUICK_ACCESS;
                                    }
                                    if (!isOverrideObject)
                                        lstPegSnpShtFromDB.Add(GenObjSnapShotForXmlImport(itmPegFromXml, itmChild));
                                }
                                else
                                {
                                    Logger.Info("CreateObjectMappingForImport", string.Format("Reuser object [{2}] from DB:Name_id [{0}]-ObjectId:[{1}]", qoa.OBJECT_NAME_ID, qoa.OBJECT_ID, itmChild.OBJECT_HAPPY_NAME + " " + itmChild.OBJECT_TYPE));
                                    itmChild.newObjectRegTableId = qoa.OBJECT_ID;
                                    itmChild.newObjectNameId = qoa.OBJECT_NAME_ID ?? -2;
                                    itmChild.newPegNameId = itmPegFromXml.newPegNameId;

                                }
                            }
                        }

                        Thread.Sleep(5);

                    }
                    #endregion //循环处理子对象
                }

            }
            catch (Exception e)
            {
                Logger.Error("CreateObjectMappingForImport", strError = string.Format("Exception:[{0}]\r\nStackTrace:{1}", e.Message, e.StackTrace));
                isOk = false;
                return;
            }
            finally
            {
                Logger.logEnd("CreateObjectMappingForImport");
            }
        }

        private B_V_OBJECT_SNAPSHOT GenObjSnapShotForXmlImport(B_REGISTED_OBJECT itmPeg, B_REGISTED_OBJECT itmChild)
        {
            if (itmPeg == null || itmChild == null) return null;
            B_V_OBJECT_SNAPSHOT objRsltSnapObjShot = new B_V_OBJECT_SNAPSHOT();
            objRsltSnapObjShot.APPLICATION_ID = itmPeg.newAppId == -1 ? itmPeg.APPLICATION_ID : itmPeg.newAppId;
            objRsltSnapObjShot.COMMENT = itmChild.COMMENT;
            objRsltSnapObjShot.ENUM_TYPE = null;
            objRsltSnapObjShot.OBJECT_HAPPY_NAME = itmChild.OBJECT_HAPPY_NAME;
            objRsltSnapObjShot.OBJECT_ID = itmChild.newObjectNameId;
            objRsltSnapObjShot.OBJECT_NAME_ID = itmChild.newPegNameId == -1 ? itmChild.OBJECT_NAME_ID : itmChild.newPegNameId;
            objRsltSnapObjShot.OBJECT_TYPE = itmPeg.OBJECT_HAPPY_NAME;
            objRsltSnapObjShot.PEG_ID = objRsltSnapObjShot.OBJECT_ID;
            objRsltSnapObjShot.PEG_NAME = itmPeg.OBJECT_HAPPY_NAME;
            objRsltSnapObjShot.PEG_QUICK_ACCESS = itmPeg.QUICK_ACCESS;
            objRsltSnapObjShot.QUICK_ACCESS = itmChild.QUICK_ACCESS;
            return objRsltSnapObjShot;
        }

        private B_V_OBJECT_SNAPSHOT GenPegSnapShotForXmlImport(B_REGISTED_OBJECT itm)
        {
            if (itm == null) return null;
            B_V_OBJECT_SNAPSHOT objRsltSnapPegShot = new B_V_OBJECT_SNAPSHOT();
            objRsltSnapPegShot.APPLICATION_ID = itm.newAppId == -1 ? itm.APPLICATION_ID : itm.newAppId;
            objRsltSnapPegShot.COMMENT = itm.COMMENT;
            objRsltSnapPegShot.ENUM_TYPE = null;
            objRsltSnapPegShot.OBJECT_HAPPY_NAME = itm.OBJECT_HAPPY_NAME;
            objRsltSnapPegShot.OBJECT_ID = itm.newObjectNameId;
            objRsltSnapPegShot.OBJECT_NAME_ID = itm.newPegNameId == -1 ? itm.OBJECT_NAME_ID : itm.newPegNameId;
            objRsltSnapPegShot.OBJECT_TYPE = itm.OBJECT_HAPPY_NAME;
            objRsltSnapPegShot.PEG_ID = objRsltSnapPegShot.OBJECT_ID;
            objRsltSnapPegShot.PEG_NAME = itm.OBJECT_HAPPY_NAME;
            objRsltSnapPegShot.PEG_QUICK_ACCESS = itm.QUICK_ACCESS;
            objRsltSnapPegShot.QUICK_ACCESS = itm.QUICK_ACCESS;
            return objRsltSnapPegShot;
        }
#if v_16AndUp
        public List<B_REGISTED_OBJECT> GetReistedObjectsByObjectType(string strDBIdx, string objectType, List<long> appIds)
#else
        public List<T_REGISTED_OBJECT> GetReistedObjectsByObjectType(string objectType, List<long> appIds)
#endif
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if v_16AndUp
            List<B_REGISTED_OBJECT> regObjectList = new List<B_REGISTED_OBJECT>();
#else
            List<T_REGISTED_OBJECT> regObjectList = new List<T_REGISTED_OBJECT>();
#endif

            var registeredObjects = (from c in marsEntities.T_REGISTED_OBJECT
#if v_16AndUp
                                     from v_o in marsEntities.V_OBJECT_SNAPSHOT
#endif
                                     where (c.OBJECT_TYPE.Equals(objectType)) && appIds.Contains((long)c.APPLICATION_ID)
#if v_16AndUp
                                     && (c.OBJECT_ID == v_o.OBJECT_ID)
                                     orderby v_o.OBJECT_HAPPY_NAME //APPLICATION_ID change Object Haapy Name
                                     select new
                                     {
                                         objectHappyName = v_o.OBJECT_HAPPY_NAME,
                                         obj = c
                                     });
            foreach (var objItm in registeredObjects)
            {
                B_REGISTED_OBJECT objB = B_REGISTED_OBJECT.ConvertEntityDTOToB(T_REGISTED_OBJECTAssembler.ToDTO(objItm.obj));
                objB.OBJECT_HAPPY_NAME = objItm.objectHappyName;
                regObjectList.Add(objB);
            }

#else
                                     orderby c.OBJECT_HAPPY_NAME
                                     select c);
                                    foreach (T_REGISTED_OBJECT RegObj in registeredObjects)
                                    {
                                        regObjectList.Add(RegObj);
                                    }

#endif

            return regObjectList;
        }
#if v_16AndUp
        private static B_REGISTED_OBJECT ConvertEntityDTOToB(T_REGISTED_OBJECTDTO objSrc)
        {
            B_REGISTED_OBJECT objB = new B_REGISTED_OBJECT();
            objB.APPLICATION_ID = objSrc.APPLICATION_ID;
            objB.COMMENT = objSrc.COMMENT;
            objB.ENUM_TYPE = objSrc.ENUM_TYPE;
            objB.OBJECT_ID = objSrc.OBJECT_ID;
            objB.OBJECT_TYPE = objSrc.OBJECT_TYPE;
            objB.QUICK_ACCESS = objSrc.QUICK_ACCESS;
            objB.TYPE_ID = objSrc.TYPE_ID;
            objB.IS_CHECKERROR_OBJ = objSrc.IS_CHECKERROR_OBJ;
            objB.T_GUI_COMPONENT_TYPE_DIC_TYPE_ID = objSrc.T_GUI_COMPONENT_TYPE_DIC_TYPE_ID;
            objB.T_TEST_STEPS_STEPS_ID = objSrc.T_TEST_STEPS_STEPS_ID;
            return objB;
        }
#endif
        /// <summary>
        /// Get all children objects for special window
        /// </summary>
        /// <param name="objectParentName"></param>
        /// <param name="appIds"></param>
        /// <returns></returns>
#if v_16AndUp
        public List<B_REGISTED_OBJECT> GetRegisterdObjectsByObjectParentFromCache(string strDBIdx, string objectParentName, List<long> appIds)
#else
        public List<T_REGISTED_OBJECTDTO> GetRegisterdObjectsByObjectTypeFromCache(string objectType, List<long> appIds)
#endif
        {
            //Logger.Info("GetRegisterdObjectsByObjectTypeFromCache",string.Format("objectType:[{0}], appIds:[{1}]", objectParentName, appIds));
            if (MarsDBGlobe_Cache.IsCached(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS))
            {
#if v_16AndUp
                Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> dicObj = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS)
                    .GetCachedObjctAs<IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>>>(strDBIdx, appIds).ToDictionary(p => p.Key, p => p.Value);
#else
                Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> dicObj = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS)
                    .GetCachedObjctAs<IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>>>(appIds).ToDictionary(p=>p.Key,p=>p.Value);
#endif
                if (dicObj == null)
                {
                    Logger.Error("ERROR", "no Objects is cached/Getten by System.");
                    return null;
                }

#if v_16AndUp
                List<B_REGISTED_OBJECT> lstResult = new List<B_REGISTED_OBJECT>();
                foreach (T_REGISTERED_APPSDTO objApps in dicObj.Keys)
                {
                    var o_rslt = from o in dicObj[objApps]

                                 where o.OBJECT_TYPE == objectParentName
                                 orderby o.OBJECT_HAPPY_NAME
                                 select o;
                    lstResult.AddRange(o_rslt);
                }
                return lstResult;
#else

                ///find object type
                /// 
                List<T_REGISTED_OBJECTDTO> lstResult = new List<T_REGISTED_OBJECTDTO>();
                foreach(T_REGISTERED_APPSDTO objApps in dicObj.Keys)
                {
                    var o_rslt = from o in dicObj[objApps]
                                 where o.OBJECT_TYPE == objectType
                                 orderby o.OBJECT_HAPPY_NAME
                                 select o;
                    lstResult.AddRange(o_rslt);
                }
                return lstResult;
#endif
            }
            else return null;
        }

#if v_16AndUp
        public List<B_REGISTED_OBJECT> GetRegisterdObjectsByObjectNameFromCache(string strDBIdx, List<string> objectNames, List<long> appIds, string pegWindow)
#else
        public List<T_REGISTED_OBJECTDTO> GetRegisterdObjectsByObjectNameFromCache(List<string> objectNames, List<long> appIds,  string pegWindow)
#endif
        {

            //Logger.Info("GetRegisterdObjectsByObjectTypeFromCache", string.Format("objectType:[{0}], appIds:[{1}]", objectType, appIds));
            if (MarsDBGlobe_Cache.IsCached(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS))
            {
#if v_16AndUp
                Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> dicObj = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS)
                    .GetCachedObjctAs<IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>>>(strDBIdx, appIds)
                    .ToDictionary(p => p.Key, p => p.Value);
#else
                Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> dicObj = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS)
                    .GetCachedObjctAs<IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>>>(appIds).ToDictionary(p => p.Key, p => p.Value);
#endif
                if (dicObj == null)
                {
                    Logger.Error("ERROR", "no Objects is cached/Getten by System.");
                    return null;
                }
                ///find object type
                /// 
#if v_16AndUp
                List<B_REGISTED_OBJECT> lstResult = new List<B_REGISTED_OBJECT>();
#else
                List<T_REGISTED_OBJECTDTO> lstResult = new List<T_REGISTED_OBJECTDTO>();
#endif
                foreach (T_REGISTERED_APPSDTO objApps in dicObj.Keys)
                {
                    /*
                    var o_rslt = from o in dicObj[objApps]
                                 where o.OBJECT_HAPPY_NAME == objectType
                                 orderby o.OBJECT_HAPPY_NAME
                                 select o;
                   */

                    // var o_rslt = dicObj[objApps].Where(item => onjectNames.Contains(item.OBJECT_HAPPY_NAME))
                    //  .Select(a => a.OBJECT_ID).ToList().FirstOrDefault();
#if v_16AndUp
                    foreach (B_REGISTED_OBJECT obj in dicObj[objApps])
#else
                    foreach (T_REGISTED_OBJECTDTO obj in dicObj[objApps])
#endif
                    {

                        if (objectNames.Contains(obj.OBJECT_HAPPY_NAME) && obj.OBJECT_TYPE.Equals(pegWindow))
                        {
                            lstResult.Add(obj);
                        }
                    }

                }
                return lstResult;
            }
            else return null;
        }

        public static List<B_REGISTED_OBJECT> GetObjectsInfoByAppIdsAndNameIds(string strDBIdx, 
            List<long> arrListAppIds, List<long> arrListNameIds)
        {
            Logger.logBegin("GetObjectsInfoByAppIdsAndNameIds");
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var o = from x in marsEntities.T_REGISTED_OBJECT
                        from n in marsEntities.T_OBJECT_NAMEINFO
                        where arrListAppIds.Contains(x.APPLICATION_ID ?? -1)
                        && arrListNameIds.Contains(x.OBJECT_NAME_ID ?? -1)
                        && n.OBJECT_NAME_ID == x.OBJECT_NAME_ID
                        select new
                        {
                            happyName = n.OBJECT_HAPPY_NAME,
                            obj = x
                        };
                List<B_REGISTED_OBJECT> lstRslt = new List<B_REGISTED_OBJECT>();
                foreach (var itm in o)
                {
                    if (itm.obj == null) continue;
                    B_REGISTED_OBJECT ob = B_REGISTED_OBJECT.CreateFromDto(strDBIdx,
                        itm.obj.ToDTO(), itm.happyName);
                    lstRslt.Add(ob);
                }
                return lstRslt;
            }
            catch (Exception e)
            {
                Logger.Error("GetObjectsInfoByAppIdsAndNameIds", string.Format("Exception:[{0}]", e.Message), e);
                return new List<B_REGISTED_OBJECT>();
            }
        }

        /// <summary>
        /// for some reasons, it is neseccarsy to get data from db directly, like get blob data
        /// </summary>
        /// <param name="oBJECT_ID"></param>
        /// <returns></returns>
        public static T_REGISTED_OBJECTDTO GetObjectByIdFromDB(string strDBIdx, long objId)
        {
            Logger.logBegin("GetObjectByIdFromDB", string.Format("ObjectId:[{0}]", objId));
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var objTest = (from o in marsEntities.T_REGISTED_OBJECT
                               where o.OBJECT_ID == objId
                               select o).FirstOrDefault();
                if (objTest == null) return null;
                return objTest.ToDTO();
            }
            catch (Exception e)
            {
                Logger.Error("GetObjectByIdFromDB", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }
#if v_16AndUp
        public List<B_REGISTED_OBJECT> GetReistedObjectsByObjectTypeAndKeyword(string strDBIdx, string objectType, List<long> appIds, List<long> typeIdList)
#else
        public List<T_REGISTED_OBJECT> GetReistedObjectsByObjectTypeAndKeyword(string objectType, List<long> appIds, List<long> typeIdList)
#endif
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if v_16AndUp
            List<B_REGISTED_OBJECT> regObjectList = new List<B_REGISTED_OBJECT>();
            var registeredObjects = (from c in marsEntities.V_OBJECT_SNAPSHOT
                                     where (c.OBJECT_TYPE.Equals(objectType) && typeIdList.Contains((long)c.TYPE_ID)) && appIds.Contains((long)c.APPLICATION_ID)
                                     orderby c.OBJECT_HAPPY_NAME //APPLICATION_ID change Object Haapy Name
                                     select c);
            return CreateFromVObjSnap(registeredObjects == null ? null : registeredObjects.ToList());
#else
            List<T_REGISTED_OBJECT> regObjectList = new List<T_REGISTED_OBJECT>();

            var registeredObjects = (from c in marsEntities.T_REGISTED_OBJECT
                                     where (c.OBJECT_TYPE.Equals(objectType) && typeIdList.Contains((long)c.TYPE_ID)) && appIds.Contains((long)c.APPLICATION_ID)
                                     orderby c.OBJECT_HAPPY_NAME //APPLICATION_ID change Object Haapy Name
                                     select c);

            foreach (T_REGISTED_OBJECT RegObj in registeredObjects)
            {
                regObjectList.Add(RegObj);
            }
            return regObjectList;
#endif

        }

        private static Dictionary<long, ObservableCollection<B_REGISTED_OBJECT>> cachedPegWindowsByAppId = new Dictionary<long, ObservableCollection<B_REGISTED_OBJECT>>();
        public static ObservableCollection<B_REGISTED_OBJECT> GetPegwindowByAppId(string strDBIdx, long? lAppId)
        {
            Logger.Info("GetPegwindowByAppId", string.Format("ApplicationId:[{0}]", lAppId ?? -1));
            if (lAppId == null) return null;
            //if (lAppId < 0) return null;

            if (!cachedPegWindowsByAppId.ContainsKey((long)lAppId))
            {
                /// get data from database
                /// 
                ObservableCollection<B_REGISTED_OBJECT> pegAppList = LoadPegwindowFromDBByAppId(strDBIdx,(long)lAppId);
                cachedPegWindowsByAppId.Add((long)lAppId, pegAppList);
            }
            return cachedPegWindowsByAppId[(long)lAppId];
        }

        internal static ObservableCollection<B_REGISTED_OBJECT> LoadPegwindowFromDBByAppId(string strDBIdx, long lAppId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if v_16AndUp
            var query = from peg in marsEntities.V_OBJECT_SNAPSHOT
                        where peg.APPLICATION_ID == lAppId
                        && peg.OBJECT_HAPPY_NAME == peg.OBJECT_TYPE
                        orderby peg.OBJECT_TYPE
                        select peg;
            ObservableCollection<B_REGISTED_OBJECT> lstResult = new ObservableCollection<B_REGISTED_OBJECT>();
            foreach (V_OBJECT_SNAPSHOT objPeg in query)
            {
                B_REGISTED_OBJECT objPegRslt = CreateFromVObjSnap(objPeg, null);
                lstResult.Add(objPegRslt);
            }
#else
            var query = from peg in marsEntities.T_REGISTED_OBJECT
                        where peg.APPLICATION_ID == lAppId
                        && peg.OBJECT_HAPPY_NAME == peg.OBJECT_TYPE
                        select peg;
            ObservableCollection<B_REGISTED_OBJECT> lstResult = new ObservableCollection<B_REGISTED_OBJECT>();
            foreach(T_REGISTED_OBJECT objPeg in query)
            {
                B_REGISTED_OBJECT objPegRslt = B_REGISTED_OBJECT.CreateFromDto(T_REGISTED_OBJECTAssembler.ToDTO(objPeg));
                lstResult.Add(objPegRslt);
            }

#endif
            return lstResult;
        }

        private static bool InsertObject2ObjectNameInfo(string strDBIdx, 
            string strObjectHappyName, string strDescription, ref string strError, ref long l_objectId, MarsEntities objContext = null)
        {
            Logger.Info("InsertObject2ObjectNameInfo", string.Format("try to create a new Name info:[{0}] description:[{1}]", strObjectHappyName, strDescription));
            try
            {
                if (objContext == null)
                {
                    objContext = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                }

#if db4SQL
                System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
                ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

                objContext.Set<T_OBJECT_NAMEINFO>();

                T_OBJECT_NAMEINFO objNew = new T_OBJECT_NAMEINFO();
                objNew.OBJNAME_DESCRIPTION = strDescription;

                objNew.OBJECT_NAME_ID = objContext.GETNEXT_VAL(SEQ_MARS_OBJECT_ID, outparam);
                objNew.OBJECT_NAME_ID = long.Parse(outparam.Value.ToString());
                objNew.PEGWINDOW_MARK = 0;
                objNew.OBJECT_HAPPY_NAME = strObjectHappyName;

                objContext.T_OBJECT_NAMEINFO.Add(objNew);

                l_objectId = objNew.OBJECT_NAME_ID;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertObject2ObjectNameInfo", strError = string.Format("Excepitons:[{0}] when add new Nameinfo object. StackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public static bool InsertObjectWithTransaction(string strDBIdx, 
            B_REGISTED_OBJECT objToInsert, ref string strError, bool isInsertPeg = false)
        {
            Logger.logBegin("InsertObjectWithTransaction", string.Format("object happy name:[{0}]", objToInsert == null ? "N/A" : objToInsert.OBJECT_HAPPY_NAME));
            try
            {
                if ((objToInsert == null) || (string.IsNullOrEmpty(objToInsert.OBJECT_HAPPY_NAME)))
                {
                    Logger.Error("InsertObjectWithTransaction", strError = "Object is error or happy name is empty.");
                    return false;
                }
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                using (TransactionScope scope = new TransactionScope())
                {
                    ///插入一个新对象，存在2种情况
                    /// 1，happy name不存在
                    /// 2，happy name 存在
                    /// 
                    var o = (from n in marsEntities.T_OBJECT_NAMEINFO
                             where n.OBJECT_HAPPY_NAME == objToInsert.OBJECT_HAPPY_NAME
                             select n).FirstOrDefault();
                    long lObjNameId = -1;
                    if (o == null)
                    {
                        Logger.Info("InsertObjectWithTransaction", string.Format("NO Such object name exists:[{0}]", objToInsert.OBJECT_HAPPY_NAME));
                        /// 1，happy name不存在
                        /// 
                        bool isCreateNameInfoOk = InsertObject2ObjectNameInfo(strDBIdx,objToInsert.OBJECT_HAPPY_NAME, objToInsert.COMMENT, ref strError, ref lObjNameId, marsEntities);
                        if (!isCreateNameInfoOk)
                        {
                            Logger.Error("InsertObjectWithTransaction", strError = string.Format("InsertObject2ObjectNameInfo return with error:[{0}]", strError));
                            return false;
                        }

                    }
                    else
                    {
                        lObjNameId = o.OBJECT_NAME_ID;
                    }
#if db4SQL
                System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
                    ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif
                    if (isInsertPeg)
                        objToInsert.OBJECT_TYPE = objToInsert.OBJECT_HAPPY_NAME;
                    objToInsert.OBJECT_ID = marsEntities.GETNEXT_VAL(SEQ_MARS_OBJECT_ID, outparam);
                    objToInsert.OBJECT_ID = int.Parse(outparam.Value.ToString());
                    objToInsert.OBJECT_NAME_ID = lObjNameId;

                    marsEntities.Set<T_REGISTED_OBJECT>();
                    marsEntities.T_REGISTED_OBJECT.Add(objToInsert.ToEntity());
                    marsEntities.SaveChanges();

                    scope.Complete();
                    
                }

                ///re compile materialized view
                ///
                AlterMVCompile(strDBIdx,"MV_OBJ_WITH_PEG");

                MarsDBGlobe_Cache.UpdateObjectsCache();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertObjectWithTransaction", strError = string.Format("exception:[{0}]\r\n{1}", e.Message, e.StackTrace), e);
                return false;
            }
            finally
            {
                Logger.logEnd("InsertObjectWithTransaction");
            }
        }

        private static void AlterMVCompile(string strDBIdx, string strMVName)
        {
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                DbConnection dbCnn = dbCntx.Database.Connection;
                if (dbCnn.State != ConnectionState.Open)
                {
                    dbCnn.Open();
                }
                using (DbCommand dbCmmd = dbCnn.CreateCommand())
                {
                    dbCmmd.CommandText = string.Format("ALTER MATERIALIZED VIEW {0} COMPILE", strMVName);
                    dbCmmd.ExecuteNonQuery();
                }

                return;
            }
            catch (Exception e)
            {
                Logger.Error("AlterMVCompile", string.Format("Exception:[{0}]", e.Message), e);
                throw;
            }
        }

        public static bool InsertObjectInTrans(string strDBIdx, B_REGISTED_OBJECT objToInsert, ref string strError, bool isInsertPeg = false, MarsEntities objDBCntx = null)
        {
            MarsEntities marsEntities = objDBCntx == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDBCntx;

            long lObjNameId = -1;
            var objNameInfo = (from p in marsEntities.T_OBJECT_NAMEINFO
                               where p.OBJECT_HAPPY_NAME == objToInsert.OBJECT_HAPPY_NAME
                               select p).FirstOrDefault();
            if (objNameInfo == null)
            {

                bool isCreateNameInfoOk = InsertObject2ObjectNameInfo(strDBIdx, objToInsert.OBJECT_HAPPY_NAME, 
                    objToInsert.COMMENT, ref strError, ref lObjNameId, marsEntities);
                if (!isCreateNameInfoOk)
                {
                    Logger.Error("InsertObject", strError);
                    return false;
                }
                // create one 
            }
            else
            {
                lObjNameId = objNameInfo.OBJECT_NAME_ID;
            }

            //判断是否已经存在 regobject 表中
            var objReg = (from reg in marsEntities.T_REGISTED_OBJECT
                          where reg.OBJECT_NAME_ID == lObjNameId
                          && reg.OBJECT_TYPE == objToInsert.OBJECT_HAPPY_NAME
                          && reg.APPLICATION_ID == objToInsert.APPLICATION_ID
                          select reg).FirstOrDefault();
            if (objReg == null)
            {

#if db4SQL
                System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
                ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif
                if (isInsertPeg)
                    objToInsert.OBJECT_TYPE = objToInsert.OBJECT_HAPPY_NAME;
                objToInsert.OBJECT_ID = marsEntities.GETNEXT_VAL(SEQ_MARS_OBJECT_ID, outparam);
                objToInsert.OBJECT_ID = int.Parse(outparam.Value.ToString());
                objToInsert.OBJECT_NAME_ID = lObjNameId;
                marsEntities.Set<T_REGISTED_OBJECT>();
                marsEntities.T_REGISTED_OBJECT.Add(objToInsert.ToEntity());

            }
            else
            {
                //update
#if db4SQL
                System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
                //ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif
                marsEntities.Set<T_REGISTED_OBJECT>();
                if (isInsertPeg)
                    objReg.OBJECT_TYPE = objToInsert.OBJECT_HAPPY_NAME;
                //objToInsert.OBJECT_ID = marsEntities.GETNEXT_VAL(SEQ_MARS_OBJECT_ID, outparam);
                //objToInsert.OBJECT_ID = int.Parse(outparam.Value.ToString());
                objReg.OBJECT_NAME_ID = lObjNameId;
                objReg.QUICK_ACCESS = objToInsert.QUICK_ACCESS;
                marsEntities.T_REGISTED_OBJECT.Attach(objReg);
                marsEntities.Entry(objReg).State = System.Data.EntityState.Modified;
            }
            if (objDBCntx == null)
                marsEntities.SaveChanges();
            return true;
        }

        public static bool InsertObject(string strDBIdx, B_REGISTED_OBJECT objToInsert, ref string strError, bool isInsertPeg = false, MarsEntities objDBCntx = null)
        {
            Logger.Info("InsertObject", string.Format("objToInsert:[{0}]", objToInsert == null ? "" : objToInsert.OBJECT_HAPPY_NAME));
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    bool isOk = InsertObjectInTrans(strDBIdx, objToInsert, ref strError, isInsertPeg, objDBCntx);
                    if (objDBCntx != null)
                        objDBCntx.SaveChanges();
                    if (isOk)
                        scope.Complete();
                    else return false;
                }
                MarsDBGlobe_Cache.UpdateObjectsCache();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertObject", strError = string.Format("Exception:[{0}],stackTrace:[{1}], innerMessage:[{2}]", e.Message, e.StackTrace, e.InnerException == null ? "" : e.InnerException.Message), e);
                return false;
            }
        }

        public static bool UpdateObject(string strDBIdx, B_REGISTED_OBJECT objToInsert, ref string strError)
        {
            Logger.Info("UpdateObject", string.Format("objToInsert:[{0}]", objToInsert == null ? "" : objToInsert.OBJECT_HAPPY_NAME));
            try
            {
                MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                //using (TransactionScope scope = new TransactionScope())
                //{
                    /// steps:
                    /// 1, update T_REGISTED_OBJECT
                    /// 2, update happy_name if changed
                    /// 

                    /// 1, update T_REGISTED_OBJECT
                    var query = from obj in marsEntities.T_REGISTED_OBJECT
                                where obj.OBJECT_ID == objToInsert.OBJECT_ID
                                select obj;
                    var objResult = query.SingleOrDefault();
                    if (objResult == null) return InsertObject(strDBIdx,objToInsert, ref strError, false, marsEntities);
                    marsEntities.Set<T_REGISTED_OBJECT>();

                    objToInsert.CopyDataToEntityWithoutKey(objResult);
                    marsEntities.T_REGISTED_OBJECT.Attach(objResult);
                    marsEntities.Entry(objResult).State = System.Data.EntityState.Modified;
#if v_16AndUp
                    /// 2, update happy_name to t_name_info
                    /// 
                    var queryNameInfo = from n in marsEntities.T_OBJECT_NAMEINFO
                                        where n.OBJECT_NAME_ID == objToInsert.OBJECT_NAME_ID
                                        select n;
                    var objN = queryNameInfo.FirstOrDefault();

                    if (objN == null)
                    {
                        /// create a new object and update
                        /// 
                        long lObjId = -1;

                        bool isOk = B_OBJECT_NAMEINFO.CreateObject(strDBIdx,
                            objToInsert.OBJECT_HAPPY_NAME, B_OBJECT_NAMEINFO.cnst_default_comment, 0, ref lObjId, ref strError, marsEntities);
                        if (!isOk)
                        {
                            Logger.Error("UpdateObject", strError = string.Format("Can't create new NameInfo object:[{0}]", strError));
                            return false;
                        }
                        objResult.OBJECT_NAME_ID = lObjId;
                    }
                    else
                    {
                        if ((string.Compare(objN.OBJECT_HAPPY_NAME, objToInsert.OBJECT_HAPPY_NAME) != 0) && (!string.IsNullOrEmpty(objToInsert.OBJECT_HAPPY_NAME)))
                        {
                            ///不一样的happname，需要判断是不是已经存在新的Happy name,如果存在，需要判断是不是唯一的引用。如果是唯一的引用，就改变，否则插入一个新的
                            /// 
                            var qName = from qn in marsEntities.T_OBJECT_NAMEINFO
                                        where qn.OBJECT_HAPPY_NAME == objToInsert.OBJECT_HAPPY_NAME
                                        select qn;
                            var qnInstn = qName.FirstOrDefault();
                            long lObjId = -1;
                            if (qnInstn == null)
                            {
                                //需要创建新的对象
                                bool isOk = B_OBJECT_NAMEINFO.CreateObject(strDBIdx,
                                    objToInsert.OBJECT_HAPPY_NAME, B_OBJECT_NAMEINFO.cnst_default_comment, 0, ref lObjId, ref strError, marsEntities);
                                if (!isOk)
                                {
                                    Logger.Error("UpdateObject", strError = string.Format("Can't create new NameInfo object:[{0}] when happy name is changed", strError));
                                    return false;
                                }
                                objResult.OBJECT_NAME_ID = lObjId;
                            }
                            else
                            {
                                objResult.OBJECT_NAME_ID = qnInstn.OBJECT_NAME_ID;
                            }
                        }
                        else
                        {
                            //marsEntities.T_OBJECT_NAMEINFO.Attach(objN);
                            // update
                            objResult.OBJECT_NAME_ID = objN.OBJECT_NAME_ID;
                        }
                    }
#endif                    
                    int iCnt = marsEntities.SaveChanges();

                    //scope.Complete();
                    Logger.Info("UpdateObject", string.Format("Updated count:[{0}]", iCnt));
                //}
                MarsDBGlobe_Cache.UpdateObjectsCache();
                return true;
            }
            catch (Exception e)
            {

                Logger.Error("UpdateObject", strError = string.Format("Exception:[{0}] \r\n innerError:[{1}]\r\n[{2}]", e.Message, e.InnerException == null ? "" : e.InnerException.Message, e.StackTrace), e);
                return false;
            }
        }

        public void CopyDataToBobjWithoutKey(B_REGISTED_OBJECT objResult)
        {
            if (objResult == null) return;
            objResult.APPLICATION_ID = this.APPLICATION_ID;
            objResult.COMMENT = this.COMMENT;
            objResult.ENUM_TYPE = this.ENUM_TYPE;
            objResult.OBJECT_NAME_ID = this.OBJECT_NAME_ID;
            objResult.OBJECT_HAPPY_NAME = this.OBJECT_HAPPY_NAME;
            //objResult.OBJECT_ID = this.OBJECT_ID;
            objResult.OBJECT_TYPE = this.OBJECT_TYPE;
            objResult.QUICK_ACCESS = this.QUICK_ACCESS;
            objResult.TYPE_ID = this.TYPE_ID;

            objResult.IS_CHECKERROR_OBJ = this.IS_CHECKERROR_OBJ;
        }

        private void CopyDataToEntityWithoutKey(T_REGISTED_OBJECT objResult)
        {
            if (objResult == null) return;
            objResult.APPLICATION_ID = this.APPLICATION_ID;
            objResult.COMMENT = this.COMMENT;
            objResult.ENUM_TYPE = this.ENUM_TYPE;
#if v_16AndUp
            objResult.OBJECT_NAME_ID = this.OBJECT_NAME_ID;
#else
            objResult.OBJECT_HAPPY_NAME = this.OBJECT_HAPPY_NAME;
#endif
            //objResult.OBJECT_ID = this.OBJECT_ID;
            objResult.OBJECT_TYPE = this.OBJECT_TYPE;
            objResult.QUICK_ACCESS = this.QUICK_ACCESS;
            objResult.TYPE_ID = this.TYPE_ID;
            Logger.Info("CopyDataToEntityWithoutKey", string.Format("IS_CHECKERROR_OBJ:[{0}-{1}]", objResult.IS_CHECKERROR_OBJ, this.IS_CHECKERROR_OBJ));
            objResult.IS_CHECKERROR_OBJ = this.IS_CHECKERROR_OBJ;
        }
#if v_16AndUp
        internal Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> GetObjectByAppNames(string[] cachedAppShortNames, string strDBIdx)
#else
        internal Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> GetObjectByAppNames(string[] cachedAppShortNames)
#endif
        {
            Logger.Info("GetObjectByAppNames", string.Format("cachedAppShortNames:[{0}] from dbidx:[{1}]", cachedAppShortNames,strDBIdx));
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if v_16AndUp
            var query = from app in marsEntities.T_REGISTERED_APPS
                        where cachedAppShortNames.Contains(app.APP_SHORT_NAME)
                        join obj in marsEntities.V_OBJECT_SNAPSHOT on app.APPLICATION_ID equals obj.APPLICATION_ID
                        orderby new { app.APPLICATION_ID, obj.OBJECT_TYPE, obj.OBJECT_HAPPY_NAME }
                        select new
                        {
                            mars_app = app,
                            mars_obj = obj
                        };
            try
            {
                var qd = query.ToList();
                Logger.Info("-----", "begin");
                Dictionary<T_REGISTERED_APPS, List<V_OBJECT_SNAPSHOT>> objResultEntity = qd.GroupBy(p => p.mars_app, p => p.mars_obj).ToDictionary(p => p.Key, p => p.ToList());
                Logger.Info("-----", "end");
                return FormatDataFromQueryByNamesOrIds(objResultEntity);
            }
            catch (Exception e)
            {

                Logger.Error("GetObjectByAppNames", string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return null;
            }

#else
            var query = from app in marsEntities.T_REGISTERED_APPS
                        where cachedAppShortNames.Contains(app.APP_SHORT_NAME)
                        join obj in marsEntities.T_REGISTED_OBJECT on app.APPLICATION_ID equals obj.APPLICATION_ID
                        orderby new { app.APPLICATION_ID,obj.OBJECT_TYPE, obj.OBJECT_HAPPY_NAME}
                        select new
                        {
                            mars_app = app,
                            mars_obj = obj
                        };
            Dictionary<T_REGISTERED_APPS, List<T_REGISTED_OBJECT>>  objResultEntity = query.GroupBy(p => p.mars_app, p => p.mars_obj).ToDictionary(p => p.Key, p => p.ToList());
            return FormatDataFromQueryByNamesOrIds(objResultEntity);
#endif

            //return objResultDTOs;;
        }
#if v_16AndUp
        internal Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> GetObjectByAppIds(string strDBIdx, List<long> lstAppIds)
#else
        internal Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> GetObjectByAppIds(List<long> lstAppIds)
#endif
        {
            Logger.Info("GetObjectByAppIds", string.Format("lstAppIds:[0]", lstAppIds));
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if v_16AndUp
            #region old code
            var qaTmp = from app in marsEntities.T_REGISTERED_APPS
                        where lstAppIds.Contains(app.APPLICATION_ID)
                        select app;
            var qa = qaTmp.ToList();
            Logger.Info("GetObjectByAppIds", string.Format("app---------------------------------\r\n{0}", qaTmp.ToString()));

            var qoTmp = from obj in marsEntities.V_OBJECT_SNAPSHOT
                        where lstAppIds.Contains(obj.APPLICATION_ID ?? -1)
                        select obj;
            var qo = qoTmp.ToList();
            Logger.Info("GetObjectByAppIds", string.Format("obj---------------------------------\r\n{0}", qoTmp.ToString()));
            var query = from app in qa
                        from obj in qo
                        where app.APPLICATION_ID == obj.APPLICATION_ID
                        select new
                        {
                            mars_app = app,
                            mars_obj = obj
                        };
            //var query = from app in marsEntities.T_REGISTERED_APPS
            //            from obj in marsEntities.V_OBJECT_SNAPSHOT                        
            //            where lstAppIds.Contains(app.APPLICATION_ID)
            //            && app.APPLICATION_ID == obj.APPLICATION_ID
            //            && lstAppIds.Contains(obj.APPLICATION_ID??-1)
            //            orderby new { app.APPLICATION_ID, obj.OBJECT_TYPE, obj.OBJECT_HAPPY_NAME }
            //            select new
            //            {
            //                mars_app = app,
            //                mars_obj = obj
            //            };
            //var query = from app in marsEntities.T_REGISTERED_APPS
            //            where lstAppIds.Contains(app.APPLICATION_ID)
            //            join obj in marsEntities.V_OBJECT_SNAPSHOT on app.APPLICATION_ID equals obj.APPLICATION_ID
            //            where lstAppIds.Contains(app.APPLICATION_ID)
            //            orderby new { app.APPLICATION_ID, obj.OBJECT_TYPE, obj.OBJECT_HAPPY_NAME }
            //            select new
            //            {
            //                mars_app = app,
            //                mars_obj = obj
            //            };
            Logger.Info("GetObjectByAppIds", string.Format("before group by, query is :[{0}]", query.ToString()));

            //#region for performance testing
            ////int i = 0;
            ////foreach(var itm in query)
            ////{ 
            ////    i++;
            ////}
            //Logger.Info("GetObjectByAppIds",string.Format("count:[{0}]",query.ToList().Count));
            //#endregion
            Dictionary<T_REGISTERED_APPS, List<V_OBJECT_SNAPSHOT>> objResultEntity = query.GroupBy(p => p.mars_app, p => p.mars_obj).ToDictionary(p => p.Key, p => p.ToList());
            Logger.logEnd("GetObjectByAppIds");
            return FormatDataFromQueryByNamesOrIds(objResultEntity);
            #endregion //old code
            /////看了下代码 好像没有 必要用那个view
            ///// 因为结果是需要那个dictionary，因此，可以如下做
            ///// 
            //var q = from obj in marsEntities.T_REGISTED_OBJECT
            //        where lstAppIds.Contains(obj.APPLICATION_ID ?? -1)
            //        join app in marsEntities.T_REGISTERED_APPS
            //        on obj.APPLICATION_ID equals app.APPLICATION_ID into appX
            //        from a in appX.DefaultIfEmpty()
            //        //from a in marsEntities.T_REGISTERED_APPS
            //        //where lstAppIds.Contains(a.APPLICATION_ID)
            //        //where a.APPLICATION_ID == obj.APPLICATION_ID
            //        select new
            //        {
            //            mars_app = a,
            //            mars_obj = obj
            //        };
            //Logger.Info("GetObjectByAppIds",string.Format("Sql is :[{0}]", q.ToString()));
            ////dynamic lstAppAndObj = q.ToList();
            ////Logger.Info("GetObjectByAppIds",string.Format("Get count:[{0}]", lstAppAndObj.Count));
            //Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> objResultEntity = q.GroupBy(p => p.mars_app, p => p.mars_obj)
            //    .ToDictionary(p => p.Key.ToDTO(), p => p.ToDTOs());
            //Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> objResult = new Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>();
            //Logger.Info("GetObjectByAppIds", "After group");
            //foreach (T_REGISTERED_APPSDTO app in objResultEntity.Keys)
            //{
            //    List<B_REGISTED_OBJECT> lstBObjs = new List<B_REGISTED_OBJECT>();
            //    if (objResultEntity[app] == null) continue;
            //    objResultEntity[app].ForEach(itm => {
            //        B_REGISTED_OBJECT objB = ConvertEntityDTOToB(itm);
            //        lstBObjs.Add(objB);
            //    });
            //    lstBObjs.OrderBy(p => p.APPLICATION_ID).ThenBy(p => p.OBJECT_TYPE).ThenBy(p=>p.OBJECT_HAPPY_NAME);
            //    objResult.Add(app, lstBObjs);
            //}
            //Logger.logEnd("GetObjectByAppIds");
            //return objResult;
#else
            var query = from app in marsEntities.T_REGISTERED_APPS
                        where lstAppIds.Contains(app.APPLICATION_ID)
                        join obj in marsEntities.T_REGISTED_OBJECT on app.APPLICATION_ID equals obj.APPLICATION_ID
                        orderby new { app.APPLICATION_ID, obj.OBJECT_TYPE, obj.OBJECT_HAPPY_NAME }
                        select new
                        {
                            mars_app = app,
                            mars_obj = obj
                        };
            Dictionary<T_REGISTERED_APPS, List<T_REGISTED_OBJECT>> objResultEntity = query.GroupBy(p => p.mars_app, p => p.mars_obj).ToDictionary(p => p.Key, p => p.ToList());
            return FormatDataFromQueryByNamesOrIds(objResultEntity);
#endif
            //return objResultDTOs;
        }
#if v_16AndUp
        public bool UpdateOrCreateObject(string strDBIdx, string strPegWindow, 
            string strHappyName, long iAppId, string strQuickAccess, long iTypeId, string strComment, ref string strError)
        {
            Logger.Info("UpdateOrCreateObject", string.Format("try PegName:[{0}], happyName:[{1}], appId:[{2}], QuickAccess:[{3}], typeId:{4}, Comment:[{5}]",
                strPegWindow, strHappyName, iAppId, strQuickAccess, iTypeId, strComment));
            /// steps:
            /// 1, check whether the object exits
            /// 2, if not exists, create, then end
            /// 3, if application id doesn't exists, create
            /// 4, if create a new
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<V_OBJECT_SNAPSHOTDTO> lstSnap = new List<V_OBJECT_SNAPSHOTDTO>();
            strError = "";
            bool isExists = ObjectExistsByHappyName(strDBIdx,strHappyName, ref strError, out lstSnap, marsEntities);
            if (!isExists && !(string.IsNullOrEmpty(strError)))
            {
                Logger.Error("UpdateOrCreateObject", string.Format("Error:[{0}]", strError));
                return false;
            }
            try
            {
                bool isOk = false;
                long lObjeNameId = -1, lObjectId = -1;
                using (var scope = new TransactionScope())
                {
                    if (!isExists)
                    {
                        /// 2, if not exists, create, then end
                        ///  2.1 create Happy object
                        isOk = B_OBJECT_NAMEINFO.CreateObject(strDBIdx,
                            strHappyName, strComment, 0, ref lObjeNameId, ref strError, marsEntities);
                        if (!isOk)
                        {
                            Logger.Error("UpdateOrCreateObject", strError = string.Format("Can't update or Insert (CreateObject for NameInfo), as [{0}]", strError));
                            return false;
                        }
                        ///  2.2 create object
                        /// 
                        isOk = CreateNewObject(lObjeNameId, strPegWindow, strHappyName, strQuickAccess, iTypeId, 
                            iAppId, strComment, ref strError, ref lObjectId, 
                            marsEntities,
                            strDBIdx);
                        if (!isOk)
                        {
                            Logger.Error("UpdateOrCreateObject", strError = string.Format("Can't update or Insert, as [{0}]", strError));
                            return false;
                        }
                        marsEntities.SaveChanges();
                        scope.Complete();
                        return true;
                    }
                    if (!lstSnap.Exists(p => p.APPLICATION_ID == iAppId))
                    {
                        // new such object for the application 
                        /// it could be two kind of cases, 1, pegwindow is exists, 2, pegwindow is not exists
                        /// if there is not a pegwindow information, the object should be ignored. but current database structure needn't pegwindows's id,  
                        /// therefore, just inserting a new object name info is enough.
                        /// 
                        lObjeNameId = lstSnap[0].OBJECT_NAME_ID ?? -1;
                        isOk = CreateNewObject(lObjeNameId, strPegWindow, strHappyName, strQuickAccess, 
                            iTypeId, iAppId, strComment, 
                            ref strError, ref lObjectId, 
                            marsEntities,
                            strDBIdx);
                        if (!isOk)
                        {
                            Logger.Error("UpdateOrCreateObject", strError = string.Format("Can't update or Insert, as [{0}]", strError));
                            return false;
                        }
                        marsEntities.SaveChanges();
                        scope.Complete();
                        return true;
                    }
                    if (!lstSnap.Exists(p => p.APPLICATION_ID == iAppId && p.OBJECT_TYPE == strPegWindow))
                    {
                        /// create a new object and assign the object with name_info object id
                        /// 
                        lObjeNameId = lstSnap[0].OBJECT_NAME_ID ?? -1;
                        isOk = CreateNewObject(lObjeNameId, strPegWindow, strHappyName, strQuickAccess, 
                            iTypeId, iAppId, strComment, ref strError, ref lObjectId, 
                            marsEntities,
                            strDBIdx);
                        if (!isOk)
                        {
                            Logger.Error("UpdateOrCreateObject", strError = string.Format("Can't update or Insert, as [{0}]", strError));
                            return false;
                        }
                        marsEntities.SaveChanges();
                        scope.Complete();
                        return true;
                    }
                    /// update 
                    /// 
                    isOk = UpdateObjectInfoByPegAppIdAndName(strDBIdx,strPegWindow, strHappyName, iAppId, strQuickAccess, iTypeId, strComment, ref strError, marsEntities);
                    if (!isOk)
                    {
                        Logger.Error("UpdateOrCreateObject", strError = string.Format("Can't update, as [{0}]", strError));
                        return false;
                    }
                    marsEntities.SaveChanges();
                    scope.Complete();
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("UpdateOrCreateObject", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        private bool UpdateObjectInfoByPegAppIdAndName(string strDBIdx, string strPegWindow, string strHappyName, 
            long iAppId, string strQuickAccess, long iTypeId, string strComment, ref string strError, MarsEntities objContext)
        {
            Logger.Info("CreateNewObject", string.Format("Parameters: pegWindow:[{0}] HappyName:[{1}] QuickAccess:[{2}] TypeId:[{3}] Comment:[{4}] ApplicationId:[{5}]",
                strPegWindow, strHappyName, strQuickAccess, iTypeId, strComment, iAppId));
            MarsEntities objOpContext = objContext == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objContext;
            try
            {
                var q = from o in objOpContext.T_REGISTED_OBJECT
                        where o.APPLICATION_ID == iAppId
                        && o.OBJECT_TYPE == strPegWindow
                        //&& o.TYPE_ID == iTypeId
                        select o;
                var ox = q.FirstOrDefault();
                if (ox == null)
                {
                    /// nothing need to be changed
                    /// 
                    Logger.Info("UpdateObjectInfoByPegAppIdAndName", string.Format("Unable to locate the object [{2}] with Pegwindow:[{0}] application:[{1}] ", strPegWindow, iAppId, strHappyName));
                    return true;
                }
                objOpContext.Set<T_REGISTED_OBJECT>();

                objOpContext.T_REGISTED_OBJECT.Attach(ox);
                ox.TYPE_ID = iTypeId;
                ox.QUICK_ACCESS = strQuickAccess;
                ox.COMMENT = strComment;

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateObjectInfoByPegAppIdAndName", strError = string.Format("Exception:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        private bool updateSpecialObjectQuickAccess(long lObjectId, string strnewQuickAccess, MarsEntities objContext, ref string strError)
        {
            Logger.logBegin("updateSpecialObjectQuickAccess", string.Format("Object Id:[{0}]  QuickAccess:[{1}]", lObjectId, strnewQuickAccess));
            try
            {
                var obj = from o in objContext.T_REGISTED_OBJECT
                          where o.OBJECT_ID == lObjectId
                          select o;
                T_REGISTED_OBJECT oTarget = obj.FirstOrDefault();
                if (oTarget != null)
                {
                    oTarget.QUICK_ACCESS = strnewQuickAccess;
                    objContext.T_REGISTED_OBJECT.Attach(oTarget);
                    var et = objContext.Entry(oTarget);
                    et.Property(p => p.QUICK_ACCESS).IsModified = true;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateSpecialObjectQuickAccess", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("updateSpecialObjectQuickAccess");
            }
        }
        private bool CreateNewObject(long iObjNameId, string strPegWindow, string strHappyName, string strQuickAccess, long iTypeId, 
            long iAppId, string strComment, ref string strError, 
            ref long lObjectId, 
            MarsEntities objContext,
            string strDBIdx //= MarsEntitiesExtends.cnst_default_dbName
            )
        {
            Logger.Info("CreateNewObject", string.Format("Parameters: pegWindow:[{0}] HappyName:[{1}] QuickAccess:[{2}] TypeId:[{3}] Comment:[{4}] ApplicationId:[{5}]",
                strPegWindow, strHappyName, strQuickAccess, iTypeId, strComment, iAppId));
            MarsEntities objOpContext = objContext == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx) : objContext;
            try
            {
                T_REGISTED_OBJECTDTO objTargetObj = new T_REGISTED_OBJECTDTO();
                objTargetObj.APPLICATION_ID = iAppId;
                objTargetObj.COMMENT = strComment;
                objTargetObj.ENUM_TYPE = "";
                objTargetObj.OBJECT_ID = lObjectId = BoHelper.GetIdBySeqName(B_REGISTED_OBJECT.SEQ_MARS_OBJECT_ID, objContext,strDBIdx:strDBIdx);
                objTargetObj.OBJECT_TYPE = strPegWindow;
                objTargetObj.QUICK_ACCESS = strQuickAccess;
                objTargetObj.TYPE_ID = iTypeId;
                objTargetObj.OBJECT_NAME_ID = iObjNameId;

                objOpContext.Set<T_REGISTED_OBJECT>();
                objOpContext.T_REGISTED_OBJECT.Add(T_REGISTED_OBJECTAssembler.ToEntity(objTargetObj));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateNewObject", strError = string.Format("Error:[{0}]", e.Message), e);
                return false;
            }
        }

        private bool ObjectExistsByHappyName(string strDBIdx, string strHappyName, ref string strError, out List<V_OBJECT_SNAPSHOTDTO> objSnap, MarsEntities objContext = null)
        {
            Logger.Info("ObjectExistsByHappyName", string.Format("strHappyName:[{0}]", strHappyName));
            try
            {
                MarsEntities marsEntities = null;
                if (objContext == null)
                {
                    marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                }
                else
                {
                    marsEntities = objContext;
                }
                var objQuery = from c in marsEntities.V_OBJECT_SNAPSHOT
                               where
                                c.OBJECT_HAPPY_NAME == strHappyName
                               select c;

                return (objSnap = V_OBJECT_SNAPSHOTAssembler.ToDTOs(objQuery)) == null ? false : objSnap.Count > 0;
            }
            catch (Exception e)
            {
                Logger.Error("ObjectExistsByHappyName", strError = string.Format("Exception:[{0}]", e.Message), e);
                objSnap = null;
                return false;
            }


        }
#endif

#if v_16AndUp
        private Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> FormatDataFromQueryByNamesOrIds(Dictionary<T_REGISTERED_APPS, List<V_OBJECT_SNAPSHOT>> objQueryResult)
        {
            Logger.Info("FormatDataFromQueryByNamesOrIds", "Begin");
            Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>> objResultDTOs = new Dictionary<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>();
#else
        private Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> FormatDataFromQueryByNamesOrIds(Dictionary<T_REGISTERED_APPS, List<T_REGISTED_OBJECT>> objQueryResult)

        {
            Logger.Info("FormatDataFromQueryByNamesOrIds","Begin");
            Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>> objResultDTOs = new Dictionary<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>();
#endif
            foreach (T_REGISTERED_APPS objApp in objQueryResult.Keys.Distinct())
            {
                T_REGISTERED_APPSDTO objAPPDto = T_REGISTERED_APPSAssembler.ToDTO(objApp);
                T_REGISTERED_APPSDTO objDtoTmp = objResultDTOs.Keys.FirstOrDefault(p => p.APPLICATION_ID == objAPPDto.APPLICATION_ID);
                if (objDtoTmp == null)
                {
#if v_16AndUp
                    objResultDTOs.Add(objDtoTmp = objAPPDto, new List<B_REGISTED_OBJECT>());
#else
                    objResultDTOs.Add(objDtoTmp = objAPPDto, new List<T_REGISTED_OBJECTDTO>());
#endif
                }
#if v_16AndUp
                objResultDTOs[objDtoTmp] = CreateFromVObjSnap(objQueryResult[objApp]);
#else
                objResultDTOs[objDtoTmp] = T_REGISTED_OBJECTAssembler.ToDTOs(objQueryResult[objApp]);
#endif

            }

            return objResultDTOs;
        }

        public List<string> GetReistedObjectsParent(string strDBIdx)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<string> regObjectListParent = new List<string>();
            var parent = (from c in marsEntities.T_REGISTED_OBJECT
                          orderby c.OBJECT_ID
                          select new { c.OBJECT_TYPE }).Distinct();

            foreach (var par in parent)
            {
                if (par.OBJECT_TYPE != null)
                    regObjectListParent.Add(par.OBJECT_TYPE.ToString());
            }

            return regObjectListParent;
        }

        public bool ObjectExists(string strDBIdx, string name, string objectType, long appId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if v_16AndUp
            var tObject = (from c in marsEntities.V_OBJECT_SNAPSHOT
                           where c.OBJECT_HAPPY_NAME.ToUpper() == name.ToUpper() &&
                                 c.OBJECT_TYPE.ToUpper() == objectType.ToUpper() &&
                                 c.APPLICATION_ID == appId
                           select c);
#else
            var tObject = (from c in marsEntities.T_REGISTED_OBJECT
                          where c.OBJECT_HAPPY_NAME.ToUpper() == name.ToUpper() && 
                                c.OBJECT_TYPE.ToUpper() == objectType.ToUpper() &&
                                c.APPLICATION_ID == appId
                           select c);
#endif
            if (tObject != null && tObject.Count() > 0)
            {
                return true;
            }
            return false;
        }
#if v_16AndUp
        public B_REGISTED_OBJECT GetObject(string strDBIdx, string name, string objectType, long appId)
#else
        public T_REGISTED_OBJECT GetObject(string name, string objectType, long appId)
#endif
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if v_16AndUp
            var query = from c in marsEntities.T_REGISTED_OBJECT
                        from v in marsEntities.V_OBJECT_SNAPSHOT
                        where v.OBJECT_HAPPY_NAME.ToUpper() == name.ToUpper() &&
                              c.OBJECT_TYPE.ToUpper() == objectType.ToUpper() &&
                              c.APPLICATION_ID == appId &&
                              v.OBJECT_ID == c.OBJECT_ID
                        select c;
            List<T_REGISTED_OBJECT> l = query.ToList();
            if (l.Count != 1)
            {
                Console.WriteLine("Count = " + l.Count);
            }
            T_REGISTED_OBJECT objRsltEntity = query.SingleOrDefault();
            return objRsltEntity == null ? null : ConvertEntityDTOToB(T_REGISTED_OBJECTAssembler.ToDTO(query.SingleOrDefault()));
#else
            var query = from c in marsEntities.T_REGISTED_OBJECT
                           where c.OBJECT_HAPPY_NAME.ToUpper() == name.ToUpper() &&
                                 c.OBJECT_TYPE.ToUpper() == objectType.ToUpper() &&
                                 c.APPLICATION_ID == appId
                        select c;
            List<T_REGISTED_OBJECT> l = query.ToList();
            if (l.Count != 1)
            {
                Console.WriteLine("Count = " + l.Count);
            }

            return query.SingleOrDefault(); 
#endif

        }

        public long GetObjectId(string strDBIdx )
        {

            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
#if db4SQL
            System.Data.Entity.Core.Objects.ObjectParameter outparam = new System.Data.Entity.Core.Objects.ObjectParameter("v_NEXTVAL", typeof(Int32));
#else
            ObjectParameter outparam = new ObjectParameter("v_NEXTVAL", typeof(Int32));
#endif

            long projectId = (long)marsEntities.GETNEXT_VAL(SEQ_MARS_OBJECT_ID, outparam);
            return long.Parse(outparam.Value.ToString());

        }

        public List<string> GetTestEnumValues(string strDBIdx, string enumType)
        {
            List<string> testEnumValues = new List<string>();
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var testEnumValue = (from c in marsEntities.T_TEST_ENUM
                                 where c.ENUM_TYPE == enumType
                                 orderby c.ENUM_VALUE
                                 select c);

            foreach (var testEnumVal in testEnumValue)
            {
                if (testEnumValues.IndexOf(testEnumVal.ENUM_VALUE) == -1)
                    testEnumValues.Add(testEnumVal.ENUM_VALUE);
            }
            return testEnumValues;
        }

#if v_16AndUp

        private static List<B_REGISTED_OBJECT> CreateFromVObjSnap(IEnumerable<V_OBJECT_SNAPSHOT> lstToConvert)
        {
            Logger.Info("CreateFromVObjSnap-list", string.Format("try to convert [{0}] objects", lstToConvert == null ? 0 : lstToConvert.ToList().Count));
            if (lstToConvert == null) return null;
            List<B_REGISTED_OBJECT> lstRslt = new List<B_REGISTED_OBJECT>();
            int iCnt = 0;
            foreach (var itm in lstToConvert)
            {
                B_REGISTED_OBJECT obj = CreateFromVObjSnap(itm, null);
                lstRslt.Add(obj);
                iCnt++;
            }
            return lstRslt;
        }


        private static B_REGISTED_OBJECT CreateFromVObjSnap(V_OBJECT_SNAPSHOT objVObjSnap, T_GUI_COMPONENT_TYPE_DIC objGuiTyp)
        {
            //Logger.Info("CreateFromVObjSnap",string.Format("Create object from source:[{0}]",objVObjSnap.OBJECT_HAPPY_NAME));
            B_REGISTED_OBJECT objResult = new B_REGISTED_OBJECT();
            objResult.APPLICATION_ID = objVObjSnap.APPLICATION_ID;
            objResult.COMMENT = objVObjSnap.COMMENT;
            objResult.ENUM_TYPE = objVObjSnap.ENUM_TYPE;
            objResult.OBJECT_HAPPY_NAME = objVObjSnap.OBJECT_HAPPY_NAME;
            objResult.OBJECT_ID = objVObjSnap.OBJECT_ID;
            objResult.OBJECT_TYPE = objVObjSnap.OBJECT_TYPE;
            objResult.QUICK_ACCESS = objVObjSnap.QUICK_ACCESS;
            objResult.TYPE_ID = objVObjSnap.TYPE_ID;
            objResult.OBJECT_NAME_ID = objVObjSnap.OBJECT_NAME_ID;
            objResult.T_GUI_COMPONENT_TYPE_DIC = objGuiTyp;
            objResult.IS_CHECKERROR_OBJ = objVObjSnap.IS_CHECKERROR_OBJ;
            return objResult;
        }
#endif

        public List<B_REGISTED_OBJECT> GetReistedObjects(string strDBIdx, long appId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_REGISTED_OBJECT> regObjectList = new List<B_REGISTED_OBJECT>();
#if v_16AndUp
            var registeredObjects = (from c in marsEntities.V_OBJECT_SNAPSHOT
                                     from comGui in marsEntities.T_GUI_COMPONENT_TYPE_DIC
                                     where c.APPLICATION_ID == appId
                                     && (comGui.TYPE_ID == c.TYPE_ID)
                                     orderby c.APPLICATION_ID
                                     select new
                                     {
                                         objV = c,
                                         objGui = comGui
                                     }
                                     );
            foreach (var objVObjSnap in registeredObjects.Where(p => p.objV.TYPE_ID != null))
            {
                B_REGISTED_OBJECT bRegObject = CreateFromVObjSnap(objVObjSnap.objV, objVObjSnap.objGui);
                regObjectList.Add(bRegObject);
            }
#else
            var registeredObjects = (from c in marsEntities.T_REGISTED_OBJECT.Include("T_GUI_COMPONENT_TYPE_DIC")                                     
                                     where c.APPLICATION_ID == appId                                     
                                     orderby c.APPLICATION_ID
                                     select c);

            foreach (T_REGISTED_OBJECT RegObj in registeredObjects.Where(P=>P.TYPE_ID!=null))
            {

                B_REGISTED_OBJECT bRegObject = CreateFromDto(T_REGISTED_OBJECTAssembler.ToDTO(RegObj));
                bRegObject.T_GUI_COMPONENT_TYPE_DIC = RegObj.T_GUI_COMPONENT_TYPE_DIC;
                regObjectList.Add(bRegObject);
            }
#endif
            return regObjectList;
        }

        private static MLogger Logger = MLogger.GetLogger(typeof(B_REGISTED_OBJECT));
        private B_REGISTED_OBJECT value;


#if v_16AndUp
        private static B_REGISTED_OBJECT CreateFromDto(string strDBIdx, T_REGISTED_OBJECTDTO objDto, string strHappyName)
        {

            Logger.Info("CreateFromDto", string.Format("DTO:[{0}]", objDto == null ? "" : strHappyName));
#else
        private static B_REGISTED_OBJECT CreateFromDto(T_REGISTED_OBJECTDTO objDto)
        {

            Logger.Info("CreateFromDto",string.Format("DTO:[{0}]",objDto==null?"":objDto.OBJECT_HAPPY_NAME));
#endif
            B_REGISTED_OBJECT objResult = new B_REGISTED_OBJECT();
            objResult.APPLICATION_ID = objDto.APPLICATION_ID;
            objResult.COMMENT = objDto.COMMENT;
            objResult.ENUM_TYPE = objDto.ENUM_TYPE;
#if v_16AndUp
            objResult.OBJECT_HAPPY_NAME = strHappyName;
#else
            objResult.OBJECT_HAPPY_NAME = objDto.OBJECT_HAPPY_NAME;
#endif
            objResult.OBJECT_ID = objDto.OBJECT_ID;
            objResult.OBJECT_TYPE = objDto.OBJECT_TYPE;
            objResult.QUICK_ACCESS = objDto.QUICK_ACCESS;
            objResult.TYPE_ID = objDto.TYPE_ID;
            objResult.OBJ_DATA_SRC = objDto.OBJ_DATA_SRC;
            objResult.T_GUI_COMPONENT_TYPE_DIC_TYPE_ID = objDto.T_GUI_COMPONENT_TYPE_DIC_TYPE_ID;
            objResult.T_TEST_STEPS_STEPS_ID = objDto.T_TEST_STEPS_STEPS_ID;
            objResult.IS_CHECKERROR_OBJ = objDto.IS_CHECKERROR_OBJ;
            objResult.OBJECT_NAME_ID = objDto.OBJECT_NAME_ID;
            return objResult;
        }

        public List<B_REGISTED_OBJECT> FetchObjectsByTypeId(string strDBIdx, long iParentId)
        {
            Logger.Info("FetchObjectsByTypeId", string.Format("ParentId:[{0}]", iParentId));

            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_REGISTED_OBJECT> regObjectList = new List<B_REGISTED_OBJECT>();
#if v_16AndUp
            var query = from oP in marsEntities.V_OBJECT_SNAPSHOT
                        from oDes in marsEntities.V_OBJECT_SNAPSHOT
                        where oDes.OBJECT_TYPE == oP.OBJECT_HAPPY_NAME
                        && oP.OBJECT_ID == iParentId
                        && oDes.APPLICATION_ID == oP.APPLICATION_ID
                        select oDes;
            try
            {
                return CreateFromVObjSnap(query);
            }
            catch (Exception e)
            {
                Logger.Error("FetchObjectsByTypeId", string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return regObjectList;
            }

#else
            var query = from oP in marsEntities.T_REGISTED_OBJECT
                        from oDes in marsEntities.T_REGISTED_OBJECT
                        where oDes.OBJECT_TYPE == oP.OBJECT_HAPPY_NAME
                        && oP.OBJECT_ID==iParentId
                        && oDes.APPLICATION_ID == oP.APPLICATION_ID 
                        select oDes;
            List<T_REGISTED_OBJECTDTO> lstDto = T_REGISTED_OBJECTAssembler.ToDTOs(query);
            foreach (T_REGISTED_OBJECTDTO objDto in lstDto)
                regObjectList.Add(B_REGISTED_OBJECT.CreateFromDto(objDto));
             return regObjectList;
#endif

        }
#if v_16AndUp
        /// <summary>
        /// Get Registerd object from cache based on ParentName, testcaseId,kwId
        /// </summary>
        /// <param name="objParentName"></param>
        /// <param name="testcaseId"></param>
        /// <param name="kwId"></param>
        /// <returns></returns>
        /// 
        public List<B_REGISTED_OBJECT> GetRegObjectsByTCId_KWId_Parent(string strDBIdx, string objParentName, long testcaseId, long kwId)
        {
#else
        public List<T_REGISTED_OBJECTDTO> GetRegObjectsByTCId_KWId_Parent(string objParentName, long testcaseId, long kwId)
        {
#endif
            Logger.Info("GetRegObjectsByTCId_KWId_Parent", string.Format("objParentName:[{0}] testcaseId:[{1}] kwId:[{2}]", objParentName, testcaseId, kwId));
            try
            {


#if v_16AndUp
                List<B_REGISTED_OBJECT> lstResult = new List<B_REGISTED_OBJECT>();
#else
            List<T_REGISTED_OBJECTDTO> lstResult = new List<T_REGISTED_OBJECTDTO>();
#endif
                Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>> dicAppTC = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_TESTCASE)
                    .GetCachedObjctAs<Dictionary<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>>>(strDBIdx);
                //List<KeyValuePair<T_REGISTERED_APPSDTO, List<T_TEST_CASE_SUMMARYDTO>>> lstAppTC = dicAppTC.ToList();

                if (dicAppTC == null) return lstResult;

                IEnumerable<long> tmpDicAppTC = (from c in dicAppTC
                                                 where c.Value.Any(p => p.TEST_CASE_ID == testcaseId)
                                                 select c.Key.APPLICATION_ID).Distinct();
                if (tmpDicAppTC == null) return lstResult;
#if v_16AndUp
                IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>> eApp_Objects = 
                    MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS)
                    .GetCachedObjctAs<IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<B_REGISTED_OBJECT>>>>(strDBIdx, tmpDicAppTC.ToList());
#else
            IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>> eApp_Objects = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS)
                .GetCachedObjctAs<IEnumerable<KeyValuePair<T_REGISTERED_APPSDTO, List<T_REGISTED_OBJECTDTO>>>>(tmpDicAppTC.ToList());
#endif
                /// Get Keywords with its operate types
                IEnumerable<KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>> dicKeysInfo = 
                    MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_KEYWORDS)
                    .GetCachedObjctAs<IEnumerable<KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>>>(strDBIdx, new List<long>() { kwId });
                List<long?> lstType = new List<long?>();
                //Logger.Info("GetRegObjectsByTCId_KWId_Parent","Begin Type Id");
                foreach (KeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> itm in dicKeysInfo)
                {
                    if (itm.Key == null) continue;
                    var typQuery = from x in itm.Value
                                   where x.TYPE_ID != null
                                   select x.TYPE_ID;
                    lstType.AddRange(typQuery);
                }
                //Logger.Info("GetRegObjectsByTCId_KWId_Parent", "End Type Id");
                IEnumerable<long?> eTypLst = lstType.Distinct();

                foreach (var objAppObjItm in eApp_Objects)
                {
                    var tmpList = objAppObjItm.Value.Where(p => string.Compare(objParentName, p.OBJECT_TYPE, true) == 0 && eTypLst.Contains(p.TYPE_ID));
                    if (tmpList == null) continue;
                    if (!tmpList.Any()) continue;
                    lstResult.AddRange(tmpList.ToList());
                }
                Logger.Info("GetRegObjectsByTCId_KWId_Parent", "End obj Compact");
                return lstResult;
            }
            finally
            {
                Logger.logEnd("GetRegObjectsByTCId_KWId_Parent");
            }
        }
#if v_16AndUp
        public static B_REGISTED_OBJECT GetObjectById(long objectId)
#else
        public static T_REGISTED_OBJECTDTO GetObjectById(long objectId)
#endif
        {
            object o = MarsDBGlobe_Cache.GetCacheObjectsByKey(MarsDBGlobe_Cache.CACHED_KEY_APP_OBJECTS).GetObjectByChildId(objectId);
#if v_16AndUp
            if (o is B_REGISTED_OBJECT) return (B_REGISTED_OBJECT)o;
#else
            if (o is T_REGISTED_OBJECTDTO) return (T_REGISTED_OBJECTDTO)o;
#endif
            return null;
        }

        internal static bool DeleteAllDuplicatedObjectsWithoutUsed(DbCommand dbCmmd, ref int iCnt, ref string strError)
        {



            //, N.OBJECT_NAME_ID, N1.CNT , O.OBJECT_ID  
            string strsql = @"DELETE T_OBJECT_NAMEINFO WHERE OBJECT_NAME_ID IN (
                              SELECT N.OBJECT_NAME_ID 
                              FROM	T_OBJECT_NAMEINFO N
                              LEFT JOIN T_REGISTED_OBJECT O ON	N.OBJECT_NAME_ID = O.OBJECT_NAME_ID 
                              , (
                                SELECT	OBJECT_HAPPY_NAME , COUNT(*) CNT FROM T_OBJECT_NAMEINFO	GROUP BY OBJECT_HAPPY_NAME	HAVING	COUNT(*)>=2
                                ) N1
                              WHERE	N.OBJECT_HAPPY_NAME   =N1.OBJECT_HAPPY_NAME	AND N1.CNT >=2	AND O.OBJECT_ID IS NULL)";
            try
            {
                dbCmmd.Parameters.Clear();
                dbCmmd.CommandText = strsql;
                iCnt = dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetAllDuplicatedObjectsWithoutUsed", strError = string.Format("exception when try to delete un-used object name from T_OBJECT_NAMEINFO, \r\n{0}\r\n{1}", e.Message
                    , e.StackTrace), e);
                return false;
            }
        }

        internal static bool UpdateNameIdToSpecByCnn(DbConnection dbConnection, long targetNameId, List<long> toUpDate, ref string strError)
        {
            string strAllIds = "";
            Logger.logBegin("UpdateNameIdToSpecByCnn", string.Format("update Name ids [{1}] to :[{0}]", targetNameId, strAllIds = string.Join(",", toUpDate)));
            try
            {
                using (DbCommand dbCmm = dbConnection.CreateCommand())
                {
                    string strSql = "UPDATE T_REGISTED_OBJECT SET OBJECT_NAME_ID = " + targetNameId
                            + " WHERE OBJECT_NAME_ID IN (" + strAllIds + ")";
                    Logger.Info("UpdateNameIdToSpecByCnn", strSql);

                    dbCmm.CommandText = strSql;

                    int iCnt = dbCmm.ExecuteNonQuery();
                    Logger.Info("UpdateNameIdToSpecByCnn", string.Format("updated [{0}] records ", iCnt));
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("UpdateNameIdToSpecByCnn", strError = e.Message, e);
                return false;

            }
        }

        private static T_REGISTED_OBJECTDTO FromReader(DbDataReader rd)
        {
            T_REGISTED_OBJECTDTO obj = new T_REGISTED_OBJECTDTO();
            obj.APPLICATION_ID = rd["APPLICATION_ID"] as Nullable<Int64>;
            obj.COMMENT = rd["COMMENT"] as String;
            obj.ENUM_TYPE = rd["ENUM_TYPE"] as String;
            obj.IS_CHECKERROR_OBJ = rd["IS_CHECKERROR_OBJ"] as Nullable<Int16>;
            obj.OBJECT_ID = rd["OBJECT_ID"] == null ? -1 : (Int64)rd["OBJECT_ID"];
            obj.OBJECT_NAME_ID = rd["OBJECT_NAME_ID"] == null ? -1 : Convert.ToInt64((Decimal)rd["OBJECT_NAME_ID"]);// as Nullable<Int64>;
            obj.OBJECT_TYPE = rd["OBJECT_TYPE"] as String;
            obj.OBJ_DATA_SRC = rd["OBJ_DATA_SRC"] as Byte[];
            obj.QUICK_ACCESS = rd["QUICK_ACCESS"] as String;
            obj.TYPE_ID = rd["TYPE_ID"] as Nullable<Int64>;

            return obj;
        }

        internal static List<T_REGISTED_OBJECTDTO> GetDuplicatedAppObjects(DbConnection dbConnection, ref bool isOk, ref string strError)
        {
            string strSql = @"SELECT * FROM T_REGISTED_OBJECT
                            WHERE (OBJECT_NAME_ID, OBJECT_TYPE, APPLICATION_ID) IN (
                                SELECT OBJECT_NAME_ID,OBJECT_TYPE, APPLICATION_ID FROM T_REGISTED_OBJECT
                                GROUP BY OBJECT_NAME_ID, OBJECT_TYPE, APPLICATION_ID
                                HAVING COUNT(*)>=2
                            )
                            ORDER BY OBJECT_ID";
            try
            {
                using (DbCommand dbcmmd = dbConnection.CreateCommand())
                {
                    dbcmmd.CommandText = strSql;
                    DbDataReader rd = dbcmmd.ExecuteReader();
                    List<T_REGISTED_OBJECTDTO> lstRslt = new List<T_REGISTED_OBJECTDTO>();
                    while (rd.Read())
                    {
                        T_REGISTED_OBJECTDTO obj = FromReader(rd);
                        lstRslt.Add(obj);

                    }
                    isOk = true;
                    return lstRslt;
                }

            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetDuplicatedAppObjects", strError = e.Message, e);
                return null;
            }

        }

        internal static int DeleteObjectById(DbConnection dbConnection, List<long> lstToUpdate, ref string strError, ref bool isOk)
        {
            try
            {
                string strIds = string.Join(",", lstToUpdate);
                string strSql = @"DELETE T_REGISTED_OBJECT WHERE OBJECT_ID IN (" + strIds + ")";
                Logger.Info("DeleteObjectById", strSql);
                using (DbCommand dbCmmd = dbConnection.CreateCommand())
                {
                    dbCmmd.CommandText = strSql;
                    int iCnt = dbCmmd.ExecuteNonQuery();
                    isOk = true;
                    return iCnt;
                }
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("DeleteObjectById", strError = e.Message, e);
                return -1;
            }
        }

        private static bool RecompileOBJECT_SNAPSHOT(DbCommand dbCmmd, ref string strError)
        {
            try
            {
                string strSql = "ALTER MATERIALIZED VIEW V_OBJECT_SNAPSHOT COMPILE";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("RecompileOBJECT_SNAPSHOT", strError = e.Message, e);
                return false;
            }

        }

        private static bool RecompileMV_LAST_TC_INFO(DbCommand dbCmmd, ref string strError)
        {
            try
            {
                string strSql = "ALTER MATERIALIZED VIEW MV_LAST_TC_INFO COMPILE";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("RecompileMV_LAST_TC_INFO", strError = e.Message, e);
                return false;
            }

        }

        private static bool RecompileMV_STORYBOARD_LATEST(DbCommand dbCmmd, ref string strError)
        {
            try
            {
                string strSql = "ALTER MATERIALIZED VIEW MV_STORYBOARD_LATEST COMPILE";
                dbCmmd.CommandText = strSql;
                dbCmmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("RecompileMV_STORYBOARD_LATEST", strError = e.Message, e);
                return false;
            }

        }

        internal static bool RecompareObjectsMV(DbConnection dbConnection, ref string strError)
        {
            strError = "";
            string strTmpError = "";
            List<string> lstErrorMV = new List<string>();
            using (DbCommand dbCmmd = dbConnection.CreateCommand())
            {
                if (!RecompileOBJECT_SNAPSHOT(dbCmmd, ref strTmpError))
                {
                    lstErrorMV.Add("V_OBJECT_SNAPSHOT");
                }
                if (!RecompileMV_LAST_TC_INFO(dbCmmd, ref strTmpError))
                {
                    lstErrorMV.Add("MV_LAST_TC_INFO");
                }
                if (!RecompileMV_STORYBOARD_LATEST(dbCmmd, ref strTmpError))
                {
                    lstErrorMV.Add("MV_LAST_TC_INFO");
                }
                if (lstErrorMV.Count > 0)
                {
                    strError = string.Format("These materialized views [{0}] can't be compiled, please tell DBA to check.", string.Join(",", lstErrorMV));
                    return false;
                }
                return true;
            }
        }
    }

#if v_16AndUp
    public class B_OBJECT_NAMEINFO : T_OBJECT_NAMEINFODTO
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_OBJECT_NAMEINFO));
        internal const string cnst_default_comment = "Create Automatic";
        /// <summary>
        /// create a object Name info 
        /// </summary>
        /// <param name="strHappyName"></param>
        /// <param name="strComment"></param>
        /// <param name="isPegwindow">0 means common Object, 1 pegiwnodw</param>
        /// <param name="lObjectId"></param>
        /// <param name="strError"></param>
        /// <param name="marsEntities"></param>
        /// <returns></returns>
        internal static bool CreateObject(string strDBIdx, string strHappyName, string strComment, short isPegwindow, ref long lObjectId, ref string strError, MarsEntities objDBContext)
        {
            Logger.Info("CreateObject", string.Format("begins, HappyName:[{0}] comment:[{1}], isPegwindow:[{2}]", strHappyName, strComment, isPegwindow));
            try
            {
                MarsEntities objEntities = objDBContext == null ? BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDBContext;
                T_OBJECT_NAMEINFODTO objNameDto = new T_OBJECT_NAMEINFODTO();
                objNameDto.EXT_ID = -1;
                objNameDto.OBJECT_HAPPY_NAME = strHappyName;
                objNameDto.OBJECT_NAME_ID = lObjectId = BoHelper.GetIdBySeqName(B_REGISTED_OBJECT.SEQ_MARS_OBJECT_ID, objEntities);
                objNameDto.OBJNAME_DESCRIPTION = strComment;
                objNameDto.PEGWINDOW_MARK = isPegwindow;

                objEntities.Set<T_OBJECT_NAMEINFO>();
                objEntities.T_OBJECT_NAMEINFO.Add(T_OBJECT_NAMEINFOAssembler.ToEntity(objNameDto));

                if (CachedObjectNameInfo != null)
                {
                    CachedObjectNameInfo.Add(objNameDto);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateObject", string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("CreateObject");
            }

        }

        internal static B_OBJECT_NAMEINFO ConvertFromDto(T_OBJECT_NAMEINFODTO objDto)
        {
            if (objDto == null) return null;
            return new B_OBJECT_NAMEINFO()
            {
                EXT_ID = objDto.EXT_ID,
                OBJECT_HAPPY_NAME = objDto.OBJECT_HAPPY_NAME,
                OBJECT_NAME_ID = objDto.OBJECT_NAME_ID,
                OBJNAME_DESCRIPTION = objDto.OBJNAME_DESCRIPTION,
                PEGWINDOW_MARK = objDto.PEGWINDOW_MARK
            };
        }


        private static List<T_OBJECT_NAMEINFODTO> CachedObjectNameInfo = null;
        public static List<T_OBJECT_NAMEINFODTO> GetAllObjectNameInfo(ref string strError, ref bool isOk, 
            string strDBIdx //= MarsEntitiesExtends.cnst_default_dbName
            )
        {
            if (CachedObjectNameInfo != null)
            {
                Logger.Info("GetAllObjectNameInfo", string.Format("Get  catched count:[{0}]", CachedObjectNameInfo.Count));
                return CachedObjectNameInfo;
            }

            try
            {
                isOk = true;
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var lstRslt = CachedObjectNameInfo = dbCntx.T_OBJECT_NAMEINFO.OrderBy(p => p.OBJECT_HAPPY_NAME).ToDTOs();
                Logger.Info("GetAllObjectNameInfo", string.Format("Get count:[{0}]", lstRslt.Count));
                return lstRslt;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetAllObjectNameInfo", strError = string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }

        public static void CreateNewNameObjectByDBCnn(long newObjNameId, string strName, string strCommnt, DbCommand dbCmmd, ref string strError, ref bool isOk)
        {
            Logger.logBegin("CreateNewNameObjectByDBCnn", string.Format("Try to create a new object:[{0}] ", strName));
            T_OBJECT_NAMEINFODTO newNameInfo = new T_OBJECT_NAMEINFODTO()
            {
                OBJECT_NAME_ID = newObjNameId,
                OBJECT_HAPPY_NAME = strName,
                OBJNAME_DESCRIPTION = strCommnt,
                PEGWINDOW_MARK = 0,
                EXT_ID = 0
            };

            string strSqlInsert = @"INSERT INTO T_OBJECT_NAMEINFO(OBJECT_NAME_ID,OBJECT_HAPPY_NAME, OBJNAME_DESCRIPTION, PEGWINDOW_MARK,EXT_ID)
                                    VALUES(:OBJECT_NAME_ID,:OBJECT_HAPPY_NAME, :OBJNAME_DESCRIPTION, :PEGWINDOW_MARK,:EXT_ID)";
            try
            {
                dbCmmd.CommandText = strSqlInsert;
                dbCmmd.Parameters.Clear();
                DbParameter OBJECT_NAME_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                OBJECT_NAME_ID.ParameterName = "OBJECT_NAME_ID";
                OBJECT_NAME_ID.Value = newObjNameId;
                DbParameter OBJECT_HAPPY_NAME = new Oracle.ManagedDataAccess.Client.OracleParameter();
                OBJECT_HAPPY_NAME.ParameterName = "OBJECT_HAPPY_NAME";
                OBJECT_HAPPY_NAME.Value = newNameInfo == null ? "" : newNameInfo.OBJECT_HAPPY_NAME;
                DbParameter OBJNAME_DESCRIPTION = new Oracle.ManagedDataAccess.Client.OracleParameter();
                OBJNAME_DESCRIPTION.ParameterName = "OBJNAME_DESCRIPTION";
                OBJNAME_DESCRIPTION.Value = newNameInfo == null ? "" : newNameInfo.OBJNAME_DESCRIPTION;
                DbParameter PEGWINDOW_MARK = new Oracle.ManagedDataAccess.Client.OracleParameter();
                PEGWINDOW_MARK.ParameterName = "PEGWINDOW_MARK";
                PEGWINDOW_MARK.Value = 0;
                DbParameter EXT_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                EXT_ID.ParameterName = "EXT_ID";
                EXT_ID.Value = 0;

                dbCmmd.Parameters.Add(OBJECT_NAME_ID);
                dbCmmd.Parameters.Add(OBJECT_HAPPY_NAME);
                dbCmmd.Parameters.Add(OBJNAME_DESCRIPTION);
                dbCmmd.Parameters.Add(PEGWINDOW_MARK);
                dbCmmd.Parameters.Add(EXT_ID);

                int iCnt = dbCmmd.ExecuteNonQuery();
                isOk = true;
                return;
            }
            catch (Exception e)
            {
                Logger.Error("newObjNameId", strError = string.Format("Exception:[{0}],stackTrace:[{1}]", e.Message, e.StackTrace), e);
                isOk = false;
                return;
            }
        }

        private static T_OBJECT_NAMEINFODTO FromDbReader(DbDataReader rd)
        {
            T_OBJECT_NAMEINFODTO objNewObjName = new T_OBJECT_NAMEINFODTO();
            objNewObjName.EXT_ID = rd["EXT_ID"] as Nullable<Int64>;
            objNewObjName.OBJECT_HAPPY_NAME = rd["OBJECT_HAPPY_NAME"] as string;
            objNewObjName.OBJECT_NAME_ID = rd["OBJECT_NAME_ID"] == null ? -1 : Convert.ToInt64((decimal)rd["OBJECT_NAME_ID"]);
            objNewObjName.OBJNAME_DESCRIPTION = rd["OBJNAME_DESCRIPTION"] as string;
            objNewObjName.PEGWINDOW_MARK = rd["PEGWINDOW_MARK"] as Nullable<Int16>;
            return objNewObjName;
        }
        internal static List<T_OBJECT_NAMEINFODTO> GetDuplicatedObjectNames(DbCommand dbCmmd, ref bool isOk, ref string strError)
        {
            try
            {
                string strSql = @"SELECT * FROM T_OBJECT_NAMEINFO
                                    WHERE OBJECT_HAPPY_NAME IN (
                                      SELECT	OBJECT_HAPPY_NAME FROM T_OBJECT_NAMEINFO	GROUP BY OBJECT_HAPPY_NAME	HAVING	COUNT(*)>=2
                                    )
                                    ORDER BY OBJECT_HAPPY_NAME,OBJECT_NAME_ID ";
                dbCmmd.CommandText = strSql;
                DbDataReader rd = dbCmmd.ExecuteReader();

                List<T_OBJECT_NAMEINFODTO> lstRslt = new List<T_OBJECT_NAMEINFODTO>();
                while (rd.Read())
                {
                    T_OBJECT_NAMEINFODTO objName = FromDbReader(rd);
                    lstRslt.Add(objName);
                }
                isOk = true;
                return lstRslt;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetDuplicatedObjectNames", strError = e.Message, e);
                return null;
            }
        }

        internal static bool DeleteNameObjectsViaList(DbConnection dbConnection, List<long> nameIdsToBeDelete, ref string strError)
        {
            string strIds = string.Join(",", nameIdsToBeDelete);
            Logger.logBegin("DeleteNameObjectsViaList", string.Format("Name Ids to be delete:[{0}]", string.Join(",", nameIdsToBeDelete)));
            try
            {
                using (DbCommand dbcmmd = dbConnection.CreateCommand())
                {
                    string strSql = "DELETE T_OBJECT_NAMEINFO WHERE OBJECT_NAME_ID IN (" + strIds + ")";
                    dbcmmd.CommandText = strSql;
                    int iCnt = dbcmmd.ExecuteNonQuery();
                    Logger.Info("DeleteNameObjectsViaList", string.Format("[{0}] records deleted", iCnt));
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("DeleteNameObjectsViaList", strError = e.Message, e);
                return false;
            }
        }

        internal static bool CreateUniqueIndex(DbConnection dbConnection, ref string strError)
        {
            try
            {
                string strSql = @"CREATE UNIQUE INDEX IDX_U_OBJECT_NAME_ID ON T_OBJECT_NAMEINFO (OBJECT_HAPPY_NAME) ";
                using (DbCommand dbCmdd = dbConnection.CreateCommand())
                {
                    dbCmdd.CommandText = strSql;
                    dbCmdd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                Logger.Error("CreateUniqueIndex", strError, e);
                return false;
            }
        }

    }
#endif
}
