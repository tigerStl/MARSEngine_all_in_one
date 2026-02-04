using Mars.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using Mars.DataLayer;
using System.Data.Common;
using System.ComponentModel;

namespace Mars.Business
{

    public enum ENUM_TEST_SUITE_RUNTYPE
    {
        _EXECUTE=0x1,
        _RUN=0x2,
        _SKIP=0X4,
        _DONE=0X8,
        _FAILUE=0x10    
    }


    public class B_PROJ_TC_MGR : T_PROJ_TC_MGRDTO, INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_PROJ_TC_MGR));

        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }

        }


        public override Int64 STORYBOARD_DETAIL_ID
        {
            get
            {
                return base.STORYBOARD_DETAIL_ID;
            }
            set
            {
                base.STORYBOARD_DETAIL_ID = value;
                RaisePropertyChanged("STORYBOARD_DETAIL_ID");
            }
        }


        public override Nullable<Int64> PROJECT_ID { get { return base.PROJECT_ID; } set { base.PROJECT_ID = value; RaisePropertyChanged("PROJECT_ID"); } }


        public override Nullable<Int64> TEST_CASE_ID { get { return base.TEST_CASE_ID; } set { base.TEST_CASE_ID = value; RaisePropertyChanged("TEST_CASE_ID"); } }


        public override Nullable<Int64> STORYBOARD_ID { get { return base.STORYBOARD_ID; } set { base.STORYBOARD_ID = value; RaisePropertyChanged("STORYBOARD_ID"); } }


        public override Nullable<Int16> RUN_TYPE { get { return base.RUN_TYPE; } set { base.RUN_TYPE = value; RaisePropertyChanged("RUN_TYPE"); } }


        public override Nullable<Int64> DEPENDS_ON { get { return base.DEPENDS_ON; } set { base.DEPENDS_ON = value; RaisePropertyChanged("DEPENDS_ON"); } }


        public override Nullable<Int64> RUN_ORDER { get { return base.RUN_ORDER; } set { base.RUN_ORDER = value; RaisePropertyChanged("RUN_ORDER"); } }


        public override Nullable<Int64> LATEST_TEST_MARK_ID { get { return base.LATEST_TEST_MARK_ID; } set { base.LATEST_TEST_MARK_ID = value; RaisePropertyChanged("LATEST_TEST_MARK_ID"); } }


        public override Nullable<Decimal> RECORD_VERSION { get { return base.RECORD_VERSION; } set { base.RECORD_VERSION = value; RaisePropertyChanged("RECORD_VERSION"); } }


        public override String ALIAS_NAME { get { return base.ALIAS_NAME; } set { base.ALIAS_NAME = value; RaisePropertyChanged("ALIAS_NAME"); } }


        public override Nullable<Int64> TEST_SUITE_ID { get { return base.TEST_SUITE_ID; } set { base.TEST_SUITE_ID = value; RaisePropertyChanged("TEST_SUITE_ID"); } }

        //public string DependsOnString { get; set; }

        public static short Action2RunType(string strAction)
        {
            if (string.IsNullOrEmpty(strAction)) return (short)ENUM_TEST_SUITE_RUNTYPE._EXECUTE;
            if (string.Compare("DONE", strAction, true) == 0) return (short)ENUM_TEST_SUITE_RUNTYPE._DONE;
            if (string.Compare("FAILUE", strAction, true) == 0) return (short)ENUM_TEST_SUITE_RUNTYPE._FAILUE;
            if (string.Compare("EXECUTE", strAction, true) == 0) return (short)ENUM_TEST_SUITE_RUNTYPE._EXECUTE;
            if (string.Compare("SKIP", strAction, true) == 0) return (short)ENUM_TEST_SUITE_RUNTYPE._SKIP;
            return (short)ENUM_TEST_SUITE_RUNTYPE._RUN;
        }
        internal static B_PROJ_TC_MGR ToBO(Model.T_PROJ_TC_MGR entity)
        {
            if (entity == null) return null;

            var bo = new B_PROJ_TC_MGR();

            bo.STORYBOARD_DETAIL_ID = entity.STORYBOARD_DETAIL_ID;
            bo.PROJECT_ID = entity.PROJECT_ID;
            bo.TEST_CASE_ID = entity.TEST_CASE_ID;
            bo.STORYBOARD_ID = entity.STORYBOARD_ID;
            bo.RUN_TYPE = entity.RUN_TYPE;
            bo.DEPENDS_ON = entity.DEPENDS_ON;
            bo.RUN_ORDER = entity.RUN_ORDER;
            bo.LATEST_TEST_MARK_ID = entity.LATEST_TEST_MARK_ID;
            bo.RECORD_VERSION = entity.RECORD_VERSION;
            bo.ALIAS_NAME = entity.ALIAS_NAME;
            bo.TEST_SUITE_ID = entity.TEST_SUITE_ID;

            return bo;
        }

        public bool Insert2DB(MarsEntities dbcntx, ref string strError)
        {
            Logger.logBegin("Insert2DB");
            try
            {
                dbcntx.Set<T_PROJ_TC_MGR>();
                this.STORYBOARD_DETAIL_ID = BoHelper.GetTestStepsId(dbcntx);
                dbcntx.T_PROJ_TC_MGR.Add(this.ToEntity());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Insert2DB", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("Insert2DB");
            }
        }

        internal static bool updateRunTypeByIdsViaConnection(List<long> lstStoryboardDetailId, string strTargetRunType, DbConnection dbCnn, ref string strError)
        {
            Logger.logBegin("updateRunTypeByIdsViaConnection");
            try
            {
                short iRunType = Action2RunType(strTargetRunType);
                DbCommand dbCmmd = dbCnn.CreateCommand();
                string strSql = "";
                if (lstStoryboardDetailId.Count == 1)
                    strSql = string.Format("UPDATE T_PROJ_TC_MGR SET RUN_TYPE={0} WHERE STORYBOARD_DETAIL_ID={1}", iRunType, string.Join(",", lstStoryboardDetailId));
                else
                    strSql = string.Format("UPDATE T_PROJ_TC_MGR SET RUN_TYPE={0} WHERE STORYBOARD_DETAIL_ID IN ({1})", iRunType, string.Join(",", lstStoryboardDetailId));

                Logger.Info("updateRunTypeByIdsViaConnection", strSql);
                dbCmmd.CommandText = strSql;

                int iRsult = dbCmmd.ExecuteNonQuery();
                Logger.Info("updateRunTypeByIdsViaConnection", string.Format("updated [{0}] records, and [{1}] records are required.", iRsult, lstStoryboardDetailId.Count));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateRunTypeByIdsViaConnection", strError = e.Message, e);
                return false;
            }
            finally
            {
                Logger.logEnd("updateRunTypeByIdsViaConnection");
            }
        }

        public static bool DeleteIfStoryboardRunOrdExists(long storyboardId, long runOrder, MarsEntities dbCntx, ref string strError)
        {
            if (dbCntx == null)
            {
                strError = "DbContex is null";
                return false;
            }

            try
            {
                var d = (from p in dbCntx.T_PROJ_TC_MGR
                         where p.STORYBOARD_ID == storyboardId
                         && p.RUN_ORDER == runOrder
                         select p).ToList();
                if ((d == null) || (d.Count == 0))
                {
                    return true;
                }
                foreach (var itm in d)
                {
                    if (itm == null) continue;
                    var dt = (from p in dbCntx.T_STORYBOARD_DATASET_SETTING
                              where p.STORYBOARD_DETAIL_ID == itm.STORYBOARD_DETAIL_ID
                              select p)
                             .ToList();
                    foreach (var x in dt)
                    {
                        if (x == null) continue;
                        dbCntx.T_STORYBOARD_DATASET_SETTING.Remove(x);
                    }
                    dbCntx.T_PROJ_TC_MGR.Remove(itm);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteIfStoryboardRunOrdExists", strError = e.Message, e);
                return false;
            }
        }

        internal static B_PROJ_TC_MGR FromDTO(T_PROJ_TC_MGRDTO objSrc)
        {
            if (objSrc == null) return null;
            B_PROJ_TC_MGR objResult = new B_PROJ_TC_MGR();
            objResult.ALIAS_NAME = objSrc.ALIAS_NAME;
            objResult.DEPENDS_ON = objSrc.DEPENDS_ON;
            objResult.LATEST_TEST_MARK_ID = objSrc.LATEST_TEST_MARK_ID;
            objResult.PROJECT_ID = objSrc.PROJECT_ID;
            objResult.RECORD_VERSION = objSrc.RECORD_VERSION;
            objResult.RUN_ORDER = objSrc.RUN_ORDER;
            objResult.RUN_TYPE = objSrc.RUN_TYPE;
            objResult.STORYBOARD_DETAIL_ID = objSrc.STORYBOARD_DETAIL_ID;
            objResult.STORYBOARD_ID = objSrc.STORYBOARD_ID;
            objResult.TEST_CASE_ID = objSrc.TEST_CASE_ID;
            objResult.TEST_SUITE_ID = objSrc.TEST_SUITE_ID;

            return objResult;
        }

        private void AddStandardParas(DbParameterCollection targetDbParameter)
        {
            DbParameter para_PROJECT_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_PROJECT_ID.ParameterName = "PROJECT_ID";
            para_PROJECT_ID.Value = PROJECT_ID;
            targetDbParameter.Add(para_PROJECT_ID);
            DbParameter para_TEST_CASE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_TEST_CASE_ID.ParameterName = "TEST_CASE_ID";
            para_TEST_CASE_ID.Value = TEST_CASE_ID;
            targetDbParameter.Add(para_TEST_CASE_ID);
            DbParameter para_STORYBOARD_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_STORYBOARD_ID.ParameterName = "STORYBOARD_ID";
            para_STORYBOARD_ID.Value = STORYBOARD_ID;
            targetDbParameter.Add(para_STORYBOARD_ID);
            DbParameter para_RUN_TYPE = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_RUN_TYPE.ParameterName = "RUN_TYPE";
            para_RUN_TYPE.Value = RUN_TYPE;
            targetDbParameter.Add(para_RUN_TYPE);
            DbParameter para_DEPENDS_ON = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_DEPENDS_ON.ParameterName = "DEPENDS_ON";
            para_DEPENDS_ON.Value = DEPENDS_ON;
            targetDbParameter.Add(para_DEPENDS_ON);
            DbParameter para_RUN_ORDER = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_RUN_ORDER.ParameterName = "RUN_ORDER";
            para_RUN_ORDER.Value = RUN_ORDER;
            targetDbParameter.Add(para_RUN_ORDER);
            DbParameter para_LATEST_TEST_MARK_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_LATEST_TEST_MARK_ID.ParameterName = "LATEST_TEST_MARK_ID";
            para_LATEST_TEST_MARK_ID.Value = LATEST_TEST_MARK_ID;
            targetDbParameter.Add(para_LATEST_TEST_MARK_ID);
            DbParameter para_RECORD_VERSION = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_RECORD_VERSION.ParameterName = "RECORD_VERSION";
            para_RECORD_VERSION.Value = RECORD_VERSION;
            targetDbParameter.Add(para_RECORD_VERSION);
            DbParameter para_ALIAS_NAME = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_ALIAS_NAME.ParameterName = "ALIAS_NAME";
            para_ALIAS_NAME.Value = ALIAS_NAME;
            targetDbParameter.Add(para_ALIAS_NAME);
            DbParameter para_TEST_SUITE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
            para_TEST_SUITE_ID.ParameterName = "TEST_SUITE_ID";
            para_TEST_SUITE_ID.Value = TEST_SUITE_ID;
            targetDbParameter.Add(para_TEST_SUITE_ID);
        }


        public static bool UpdateStatusByConnection(DbConnection cnn, long storyboardDtlId, ref string strError)
        {
            string strSql = @" UPDATE T_PROJ_TC_MGR SET RUN_TYPE= "+BoHelper.CNST_RUN_TYPE_DON
                           +" WHERE STORYBOARD_DETAIL_ID="+ storyboardDtlId
                           + " AND RUN_TYPE=" + BoHelper.CNST_RUN_TYPE_RUN;
            try
            {
                using(DbCommand dbCmmd = cnn.CreateCommand())
                {
                    dbCmmd.CommandText = strSql;
                    int iRsult = dbCmmd.ExecuteNonQuery();
                    Logger.Info("UpdateStatusByConnection", string.Format("updated [{0}] recorders", iRsult)) ;
                }
                return true;
            }
            catch(Exception e)
            {
                Logger.Error("UpdateStatusByConnection",strError = string.Format("can't update T_PROJ_TC_MGR table with exception:[{0}]", e.Message),e);
                return false;
            }
        }
        public bool UpdateByConnection(DbConnection cnn, ref string strError)
        {
            string strSql = @"UPDATE T_PROJ_TC_MGR SET PROJECT_ID=:PROJECT_ID, TEST_CASE_ID=:TEST_CASE_ID, STORYBOARD_ID=:STORYBOARD_ID, 
                                RUN_TYPE=:RUN_TYPE, DEPENDS_ON=:DEPENDS_ON, RUN_ORDER=:RUN_ORDER, LATEST_TEST_MARK_ID=:LATEST_TEST_MARK_ID, RECORD_VERSION=:RECORD_VERSION,
                                ALIAS_NAME=:ALIAS_NAME, TEST_SUITE_ID=:TEST_SUITE_ID
                             WHERE STORYBOARD_DETAIL_ID=" + this.STORYBOARD_DETAIL_ID;
            try
            {
                using (DbCommand cmmd = cnn.CreateCommand())
                {
                    cmmd.CommandText = strSql;

                    AddStandardParas(cmmd.Parameters);
                    #region replaced by methods
                    //DbParameter para_PROJECT_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_PROJECT_ID.ParameterName = "PROJECT_ID";
                    //para_PROJECT_ID.Value = PROJECT_ID;
                    //cmmd.Parameters.Add(para_PROJECT_ID);
                    //DbParameter para_TEST_CASE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_TEST_CASE_ID.ParameterName = "TEST_CASE_ID";
                    //para_TEST_CASE_ID.Value = TEST_CASE_ID;
                    //cmmd.Parameters.Add(para_TEST_CASE_ID);
                    //DbParameter para_STORYBOARD_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_STORYBOARD_ID.ParameterName = "STORYBOARD_ID";
                    //para_STORYBOARD_ID.Value = STORYBOARD_ID;
                    //cmmd.Parameters.Add(para_STORYBOARD_ID);
                    //DbParameter para_RUN_TYPE = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_RUN_TYPE.ParameterName = "RUN_TYPE";
                    //para_RUN_TYPE.Value = RUN_TYPE;
                    //cmmd.Parameters.Add(para_RUN_TYPE);
                    //DbParameter para_DEPENDS_ON = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_DEPENDS_ON.ParameterName = "DEPENDS_ON";
                    //para_DEPENDS_ON.Value = DEPENDS_ON;
                    //cmmd.Parameters.Add(para_DEPENDS_ON);
                    //DbParameter para_RUN_ORDER = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_RUN_ORDER.ParameterName = "RUN_ORDER";
                    //para_RUN_ORDER.Value = RUN_ORDER;
                    //cmmd.Parameters.Add(para_PROJECT_ID);
                    //DbParameter para_LATEST_TEST_MARK_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_LATEST_TEST_MARK_ID.ParameterName = "LATEST_TEST_MARK_ID";
                    //para_LATEST_TEST_MARK_ID.Value = LATEST_TEST_MARK_ID;
                    //cmmd.Parameters.Add(para_LATEST_TEST_MARK_ID);
                    //DbParameter para_RECORD_VERSION = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_RECORD_VERSION.ParameterName = "RECORD_VERSION";
                    //para_RECORD_VERSION.Value = RECORD_VERSION;
                    //cmmd.Parameters.Add(para_RECORD_VERSION);
                    //DbParameter para_ALIAS_NAME = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_ALIAS_NAME.ParameterName = "ALIAS_NAME";
                    //para_ALIAS_NAME.Value = ALIAS_NAME;
                    //cmmd.Parameters.Add(para_PROJECT_ID);
                    //DbParameter para_TEST_SUITE_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    //para_TEST_SUITE_ID.ParameterName = "TEST_SUITE_ID";
                    //para_TEST_SUITE_ID.Value = TEST_SUITE_ID;
                    //cmmd.Parameters.Add(para_TEST_SUITE_ID);
                    #endregion

                    int iUpdateCnt = cmmd.ExecuteNonQuery();
                    Logger.Info("UpdateByConnection", string.Format("{0} rec updated", iUpdateCnt));
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("UpdateByConnection", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        public bool CreateNewByConnection(DbConnection cnn, ref long newDtlId, ref string strError)
        {
            
            string strSql = @"INSERT INTO T_PROJ_TC_MGR (STORYBOARD_DETAIL_ID,PROJECT_ID,TEST_CASE_ID,STORYBOARD_ID,RUN_TYPE,DEPENDS_ON,
                                RUN_ORDER,LATEST_TEST_MARK_ID,RECORD_VERSION,ALIAS_NAME,TEST_SUITE_ID)
                              VALUES(:STORYBOARD_DETAIL_ID, :PROJECT_ID, :TEST_CASE_ID, :STORYBOARD_ID, :RUN_TYPE, :DEPENDS_ON, 
                                     :RUN_ORDER, :LATEST_TEST_MARK_ID, :RECORD_VERSION,:ALIAS_NAME, :TEST_SUITE_ID)";
            DbCommand cmmd = null;
            try
            {
                using (cmmd = cnn.CreateCommand())
                {
                    bool isOk = false;
                    newDtlId=BoHelper.GetBussinessSeq("T_TEST_STEPS_SEQ", cmmd, ref strError, ref isOk);
                    if (!isOk) return false;
                    cmmd.Parameters.Clear();

                    cmmd.CommandText = strSql;

                    DbParameter para_STORYBOARD_DETAIL_ID = new Oracle.ManagedDataAccess.Client.OracleParameter();
                    para_STORYBOARD_DETAIL_ID.ParameterName = "STORYBOARD_DETAIL_ID";
                    para_STORYBOARD_DETAIL_ID.Value = newDtlId;
                    cmmd.Parameters.Add(para_STORYBOARD_DETAIL_ID);

                    AddStandardParas(cmmd.Parameters);
                    int iUpdateCnt = cmmd.ExecuteNonQuery();
                    Logger.Info("UpdateByConnection", string.Format("{0} rec updated", iUpdateCnt));
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("UpdateByConnection", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }            
        }

        public static bool DeleteStoryboardDetailToContainerByCnn(DbConnection cnn, long lDtlId, ref string strError)
        {
            string strSql = "UPDATE T_PROJ_TC_MGR SET STORYBOARD_ID=-1 WHERE STORYBOARD_DETAIL_ID="+ lDtlId;
            try
            {
                using (DbCommand cmmd = cnn.CreateCommand())
                {
                    cmmd.CommandText = strSql;
                    int iCnt = cmmd.ExecuteNonQuery();
                    Logger.Info("DeleteStoryboardDetailToContainerByCnn", string.Format("remove [{0}] to -1 with return :[{1}]", lDtlId, iCnt));
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("DeleteStoryboardDetailToContainerByCnn", strError = e.Message, e);
                return false;
            }
        }
    }
}
