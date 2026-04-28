using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.message.Dto;
using Mars.Model;
#if !_pythonInterface
using Route2NSEx.src.Marquis.systemUtil;
#endif
using Mars.message.DataLayer;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;

#if _marsLog
using Logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.Business
{
    /// <summary>
    /// 如果是状态variable，status表示该对象的状态。在loop中，1，表示继续使用，0表示为完成， 可以使用多行或者在一行中用回车分开。如果在一行中表示，为
    /// 00001:0\r\n0002:1\r\n
    /// 调用removeVariable时候，将改改状态值
    /// </summary>
    public class B_SYSTEM_LOOKUP : SYSTEM_LOOKUPDTO, INotifyPropertyChanged
    {
#if !_marsLog
        private static MLogger Logger = MLogger.GetLogger(typeof(B_SYSTEM_LOOKUP));
#endif
        private const string SYSTEM_LOOKUP_SEQ = "SYSTEM_LOOKUP_SEQ";

        public const string CNST_TABLENAME_T_REGISTERED_APPS = "T_REGISTERED_APPS";
        public const string CNST_FIELDNAME_APPLICATION_TYPE = "APPLICATION_TYPE";
        public const string CNST_FIELDNAME_EXTRA_REQUIRE_ADDINS = "EXTAREQUIREMENT";

        public const string CNST_RESERVED_VARIABLE_LOCAL  = "LOCAL_VAR" ;
        public const string CNST_RESERVED_VARIABLE_GLOBAL = "GLOBAL_VAR";
        public const string CNST_RESERVED_VARIABLE_LOOP   = "LOOP_VAR"  ;
        public const string CNST_RESERVED_VARIABLE_MODAL  = "MODAL_VAR" ;
        public const string CNST_RESERVED_VARIABLE_IF     = "IF_VAR"    ;
        public const string CNST_RESERVED_VARIABLE_MEMO   = "MEMO_VAR"  ;
        public Boolean Enabled { get; set; }

        internal void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public override Int64 LOOKUP_ID { get
            {
                return base.LOOKUP_ID;
            }

            set {
                base.LOOKUP_ID = value;
                OnPropertyChanged("LOOKUP_ID");
            } }

        public override String TABLE_NAME { get
            {
                return base.TABLE_NAME;
            }

            set
            {
                base.TABLE_NAME = value;
                OnPropertyChanged("TABLE_NAME");
            }
        }

        public new String FIELD_NAME {
            get
            { return base.FIELD_NAME; }
            set
            {
                base.FIELD_NAME = value;
                OnPropertyChanged("FIELD_NAME");
            } }

        public override Nullable<Int16> VALUE { get {
                return base.VALUE;
            } set {
                base.VALUE = value;
                OnPropertyChanged("VALUE");
            } }

        public new String DISPLAY_NAME {
            get {
                return base.DISPLAY_NAME;
            }
            set {
                base.DISPLAY_NAME = value;
                OnPropertyChanged("DISPLAY_NAME");
            }
        }

        public new Nullable<Int16> STATUS
        {
            get
            {
                return base.STATUS;
            }
            set
            {
                base.STATUS = value;
                OnPropertyChanged("STATUS");
            }
        }
        private ObservableCollection<KeyValuePair<string, int>> _BaseOrCompareDic = new ObservableCollection<KeyValuePair<string, int>>()
        {
            new KeyValuePair<string, int>("BASELINE", 1) ,
            new KeyValuePair<string, int>("COMPARE", 2)
        };
     public ObservableCollection<KeyValuePair<string, int>> BaseOrCompareDic
        {
            get {
                return _BaseOrCompareDic;
            }
            set
            {
                _BaseOrCompareDic = value;
                OnPropertyChanged("BaseOrCompareDic");
            }
        }

        public int CurrentStatus
        {
            get
            {
                if (string.Compare(TABLE_NAME, CNST_RESERVED_VARIABLE_MODAL) != 0)
                    STATUS = (Nullable < Int16 >)0;
                return STATUS??1;
            }
            set
            {
                if (string.Compare(TABLE_NAME, CNST_RESERVED_VARIABLE_MODAL) == 0)
                    STATUS = (short)value;
                OnPropertyChanged("CurrentStatus");
            }
        }
#if !_noEntities
        public List<B_SYSTEM_LOOKUP> GetSystemLookup(string strDBIdx )// = MarsEntitiesExtends.cnst_default_dbName)
        {
            MarsEntities marsEntities =Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            List<B_SYSTEM_LOOKUP> SystemLookup = new List<B_SYSTEM_LOOKUP>();
            var systemLookupRows = (from c in marsEntities.SYSTEM_LOOKUP
                                    orderby c.DISPLAY_NAME
                                    select c).ToList();

            foreach (SYSTEM_LOOKUP systemLookup in systemLookupRows)
            {
                B_SYSTEM_LOOKUP newSystemLookup = new B_SYSTEM_LOOKUP();
                newSystemLookup.LOOKUP_ID = systemLookup.LOOKUP_ID;
                newSystemLookup.TABLE_NAME = systemLookup.TABLE_NAME;
                newSystemLookup.FIELD_NAME = systemLookup.FIELD_NAME;
                newSystemLookup.VALUE = systemLookup.VALUE;
                newSystemLookup.DISPLAY_NAME = systemLookup.DISPLAY_NAME;
                newSystemLookup.STATUS = systemLookup.STATUS;
                SystemLookup.Add(newSystemLookup);
            }
            return SystemLookup;
        }
#endif

        public bool updateStatusData(string strDBIdx, int assignedKey, string varName, string strData2DB, ref string strError)
        {
#if !_forWebClient
            Logger.logBegin("updateStatusData", $"db:[{strDBIdx}]-[{assignedKey}]-[{varName}]-[{strData2DB}]");
            DbTransaction trans = null;
            try
            {
                MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
                string strSql = @"update SYSTEM_LOOKUP set
                    TABLE_NAME='STATUSVAR',
                    FIELD_NAME=:fieldName,
                    DISPLAY_NAME=:displayName
                where LOOKUP_ID=:k";
                DbConnection cnn = null;
                if ((cnn = marsEntities.Database.Connection).State != System.Data.ConnectionState.Open)
                {
                    marsEntities.Database.Connection.Open();
                }
                trans = cnn.BeginTransaction();
                
                var cmd = cnn.CreateCommand();
                cmd.CommandText = strSql;
                DbParameter obj_fieldName = new Oracle.ManagedDataAccess.Client.OracleParameter("fieldName", varName);
                cmd.Parameters.Add(obj_fieldName);
                DbParameter obj_displayName = new Oracle.ManagedDataAccess.Client.OracleParameter("displayName", strData2DB);
                cmd.Parameters.Add(obj_displayName);
                DbParameter obj_k = new Oracle.ManagedDataAccess.Client.OracleParameter("k", assignedKey);
                cmd.Parameters.Add(obj_k);
                int icnt = cmd.ExecuteNonQuery();
                Logger.Info("updateStatusData", $"updated [{icnt}] record");

                trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateStatusData", $"{strError=e.Message}\r\n{e.StackTrace}");
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
            finally
            {
                Logger.logEnd("updateStatusData");
            }
            
#else
            try
            {
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                bool isOk = false;

                isOk = clnt.UpdateStatusVar(assignedKey, varName, strData2DB, ref strError);
                if (!isOk)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = string.Format("can't update status var:[{0}]-[{1}]", varName, strData2DB);
                        Logger.Error("updateStatusData",strError);
                    }
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateStatusData", strError = e.Message, e.StackTrace);
                return false;
            }
#endif

        }

        public List<B_SYSTEM_LOOKUP> GetSystemLookup(string strTableName, string strFieldName, int iRefStatus,ref string strError, 
            string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {
#if !_forWebClient
            Logger.Info("GetSystemLookup",string.Format("TableName:[{0}] FieldName:[{1}] RefValue:[{2}] dbIdx:[{3}]", strTableName, strFieldName, iRefStatus,
                strDBIdx));
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = from l in marsEntities.SYSTEM_LOOKUP
                        where
                            l.TABLE_NAME == strTableName
                        && l.FIELD_NAME == strFieldName
                        && l.STATUS == iRefStatus 
                        select l;
            try
            {
                List<B_SYSTEM_LOOKUP> lstResult = new List<B_SYSTEM_LOOKUP>();
                foreach(var objItm in query)
                {
                    B_SYSTEM_LOOKUP objTItm = FromEntities(objItm);
                    if (objTItm == null) continue;
                    lstResult.Add(objTItm);
                }
                return lstResult;
            }
            catch (Exception e)
            {
                Logger.Error("GetSystemLookup",strError = string.Format("Exception:[{0}]", e.Message),e);
                return null;
            }
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            bool isOk = false;
            List<B_SYSTEM_LOOKUP> lstRslt = clnt.GetSystemLookups(strTableName, new List<string>() { strFieldName}, ref isOk, ref strError);
            if (!isOk) return null;
            if (lstRslt ==null)
            {
                strError = string.Format("no System_lookup data find for [{0}]-[{1}]", strTableName, strFieldName);
                return null;
            }
            return lstRslt.Where(p => p.STATUS == iRefStatus).ToList();
#endif
        }

#if !_noEntities
        public static List<B_SYSTEM_LOOKUP> ApplicationTypes(string strDBIdx)// = MarsEntitiesExtends.cnst_default_dbName)
        {
            B_SYSTEM_LOOKUP boLookup = new B_SYSTEM_LOOKUP();
            return boLookup.GetSystemLookup(CNST_TABLENAME_T_REGISTERED_APPS,CNST_FIELDNAME_APPLICATION_TYPE, strDBIdx: strDBIdx);
        }
#endif
        public List<B_SYSTEM_LOOKUP> GetSystemLookup(string strTableName, List<string> lstFieldName, string strDBIdx)// = MarsEntitiesExtends.cnst_default_dbName)
        {
#if !_forWebClient
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = from l in marsEntities.SYSTEM_LOOKUP
                        where
                            l.TABLE_NAME == strTableName
                        && lstFieldName.Contains(l.FIELD_NAME)
                        select l;
            try
            {
                List<SYSTEM_LOOKUP> lstEntities = query.ToList();
                List<B_SYSTEM_LOOKUP> lstB = new List<B_SYSTEM_LOOKUP>();
                foreach (SYSTEM_LOOKUP objItm in lstEntities)
                {
                    B_SYSTEM_LOOKUP objTItm = FromEntities(objItm);
                    if (objTItm == null) continue;
                    lstB.Add(objTItm);
                }
                return lstB.OrderBy(p => p.DISPLAY_NAME).ToList();
            }
            catch (Exception e)
            {
                Logger.Error("GetSystemLookUp", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            bool isOk = false;
            string strError = "";
            List<B_SYSTEM_LOOKUP> lstDtos = clnt.GetSystemLookups(strTableName, lstFieldName,ref isOk, ref strError);
            if ((!isOk) || (lstDtos == null))
            {
                if (string.IsNullOrEmpty(strError))
                    strError = string.Format("Can't get variable Info for [{0}].[{1}]",strTableName, string.Join(",", lstFieldName));
                return null;
            }

            return lstDtos.OrderBy(p => p.DISPLAY_NAME).ToList(); 
#endif
        }

        public bool CreateOrUpdateLoopVar(string strObjectIdx, string strDataToStore, ref string strError, string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {            
            Logger.logBegin("CreateOrUpdateLoopVar",string.Format("objIdx:[{0}] -vaulue:[{1}]", strObjectIdx, strDataToStore));
#if !_forWebClient
            DbCommand     cmd  = null;
            DbTransaction trns = null;
            DbConnection  cnn  = null;
            try
            {
                MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if ((cnn = marsEntities.Database.Connection).State!= System.Data.ConnectionState.Open)
                {
                    marsEntities.Database.Connection.Open();
                }
                trns = marsEntities.Database.Connection.BeginTransaction();

                string strsql = "DELETE SYSTEM_LOOKUP  WHERE TABLE_NAME = 'LOOP_VAR' AND FIELD_NAME=:OBJ_IDX";
                cmd = marsEntities.Database.Connection.CreateCommand();
                cmd.CommandText = strsql;
                DbParameter obj_idx = new Oracle.ManagedDataAccess.Client.OracleParameter("OBJ_IDX", strObjectIdx);
                cmd.Parameters.Add(obj_idx);
                int iCnt = cmd.ExecuteNonQuery();
                Logger.Info("\t", string.Format("delete [{0}]-[{1}] [{2}] row(s)", strObjectIdx, strDataToStore, iCnt));
                strsql = string.Format("INSERT INTO SYSTEM_LOOKUP (LOOKUP_ID, TABLE_NAME, FIELD_NAME, VALUE, DISPLAY_NAME, STATUS) VALUES ({0}.NEXTVAL, 'LOOP_VAR', :objIdx, 0, :displayName, 1)", SYSTEM_LOOKUP_SEQ);
                cmd.Parameters.Clear();
                cmd.CommandText = strsql;
                DbParameter objIdx      = new Oracle.ManagedDataAccess.Client.OracleParameter("objIdx"     , strObjectIdx  );
                DbParameter displayName = new Oracle.ManagedDataAccess.Client.OracleParameter("displayName", strDataToStore);
                cmd.Parameters.Add(objIdx);
                cmd.Parameters.Add(displayName);
                iCnt = cmd.ExecuteNonQuery();
                Logger.Info("\t", string.Format("delete [{0}]-[{1}] [{2}] row(s)", strObjectIdx, strDataToStore, iCnt));

                trns.Commit();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("\t", strError = e.Message, e);
                if (trns != null)
                {
                    try
                    {
                        trns.Rollback();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("\t", ex.Message, ex);
                    }
                }
                return false;
            }
            finally
            {
                if (cnn != null)
                {
                    try
                    {
                        cnn.Close();
                    }
                    catch (Exception e)
                    {
                        Logger.Error("\t", e.Message, e);
                    }
                }
                Logger.logEnd("CreateOrUpdateLoopVar");
            }
#else
            try
            {
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                bool isOk = false;
                
                isOk = clnt.CreateOrUpdateLoopVar(strObjectIdx, strDataToStore, ref strError);
                if (!isOk)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = string.Format("can't create or update the loop var:[{0}]-[{1}]", strObjectIdx, strDataToStore);
                    }
                    return false;
                }
                return true;
            }
            catch (Exception)
            {

                throw;
            }
#endif
        }
#if !_noEntities
        public List<B_SYSTEM_LOOKUP> GetSystemLookup(string strTableName, string strFieldName, string strDBIdx= MarsEntitiesExtends.cnst_default_dbName)
        {            
            try
            {
#if !_forWebClient
                MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var query = from l in marsEntities.SYSTEM_LOOKUP
                            where
                                l.TABLE_NAME == strTableName
                            && l.FIELD_NAME == strFieldName
                            select l;
                List<SYSTEM_LOOKUP> lstEntities = query.ToList();
                List<B_SYSTEM_LOOKUP> lstB = new List<B_SYSTEM_LOOKUP>();
                foreach(SYSTEM_LOOKUP objItm in lstEntities)
                {
                    B_SYSTEM_LOOKUP objTItm = FromEntities(objItm);
                    if (objTItm == null) continue;
                    lstB.Add(objTItm);
                }
                return lstB.OrderBy(p=>p.DISPLAY_NAME).ToList();
#else

                bool isOk = false;
                string strError = "";
                List<B_SYSTEM_LOOKUP> lstB = (new MarsRESTfulApiClient(strDBIdx)).GetSystemLookup(strTableName, strFieldName, ref isOk, ref strError);
                Logger.Info("\t",$"GetSystemLookup return :{isOk}, with data "+lstB==null?"null":$"count : {lstB.Count}");
                if (!isOk) return null;
                if (lstB == null) return new List<B_SYSTEM_LOOKUP>();
                return lstB;
#endif
            }
            catch (Exception e)
            {
                Logger.Error("GetSystemLookUp", string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
        }

        private static B_SYSTEM_LOOKUP FromEntities(SYSTEM_LOOKUP objEntities)
        {
            if (objEntities == null) return null;
            B_SYSTEM_LOOKUP objLook = new B_SYSTEM_LOOKUP();
            objLook.DISPLAY_NAME = objEntities.DISPLAY_NAME;
            objLook.FIELD_NAME = objEntities.FIELD_NAME;
            objLook.LOOKUP_ID = objEntities.LOOKUP_ID;
            objLook.STATUS = objEntities.STATUS;
            objLook.TABLE_NAME = objEntities.TABLE_NAME;
            objLook.VALUE = objEntities.VALUE;
            return objLook;
        }
#endif

#if !_forWebClient

        public bool InsertSelfWithStatus(ref string strError, MarsEntities objDBCntx = null,string strDBIdx ="MarsEntities")
        {
            Logger.Info("InsertSelfWithStatus",string.Format("Table:[{0}] Filed:[{1}] value:[{2}] status:[{3}] display:[{4}]",
                this.TABLE_NAME,this.FIELD_NAME,this.VALUE,this.STATUS,this.DISPLAY_NAME
                ));
            MarsEntities marsEntities = objDBCntx == null ? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx) : objDBCntx;
            try
            {

                var query = from o in marsEntities.SYSTEM_LOOKUP
                            where
                                o.TABLE_NAME == this.TABLE_NAME
                            &&  o.FIELD_NAME == this.FIELD_NAME
                            &&  o.STATUS == this.STATUS
                            select o;
                SYSTEM_LOOKUP oE = query.FirstOrDefault();
                if (oE != null)
                {
                    strError = string.Format("Table:[{0}] Filed:[{1}] value:[{2}] status:[{3}] display:[{4}]",
                                this.TABLE_NAME, this.FIELD_NAME, this.VALUE, this.STATUS, this.DISPLAY_NAME
                                );
                    Logger.Error("InsertSelfWithStatus", strError = string.Format("Unable to locate the object from system_lookup with info:[{0}]", strError));
                    return false;
                }

                marsEntities.Set<SYSTEM_LOOKUP>();
                this.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                marsEntities.SYSTEM_LOOKUP.Add(SYSTEM_LOOKUPAssembler.ToEntity(this));
                if (objDBCntx == null)
                    marsEntities.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertSelf", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }

        }
#else
        public bool InsertSelfWithStatus(ref string strError, string strDBIdx)
        {
            try
            {
                MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
                bool isOk = clnt.InsertSystemLookupWithStatus(this, ref strError);
                if (!isOk) {
                    if (string.IsNullOrEmpty(strError)) strError = "can't insert ";
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]", e.Message);
                return false;
            }
        }
#endif


#if !_noEntities
        public bool IsNotItemExists(string strDBIdx ,string strTable,string strField, MarsEntities objDBCntx=null, short sStatus=-1) // = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.Info("IsNotItemExists", string.Format("strTable:[{0}] strField:[{1} sStatus:[{2}]", strTable, strField, sStatus));
            MarsEntities marsEntities = objDBCntx == null ? Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDBCntx;
            try
            {
                if (sStatus == -1)
                {
                    var query = from c in marsEntities.SYSTEM_LOOKUP
                                where c.TABLE_NAME == strTable
                                && c.FIELD_NAME == strField
                                select c;
                    return query.FirstOrDefault() == null;
                }
                else
                {
                    var query = from c in marsEntities.SYSTEM_LOOKUP
                                where c.TABLE_NAME == strTable
                                && c.FIELD_NAME == strField
                                && c.STATUS == sStatus
                                select c;
                    return query.FirstOrDefault() == null;
                }

            }
            catch (Exception e)
            {
                Logger.Error("IsNotItemExists", string.Format("Exception:[{0}],stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public bool createGlobalVar(string strDBIdx, List<string> lstName, ref string strError,  MarsEntities objDBCntx = null) // = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.Info("createGlobalVar", string.Format("varibals:[{0}]", lstName == null ? "" : string.Join(",", lstName)));
            MarsEntities marsEntities = objDBCntx == null ? Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDBCntx;
            try
            {
                marsEntities.Set<SYSTEM_LOOKUP>();
                foreach (var itm in lstName)
                {
                    if (string.IsNullOrEmpty(itm)) continue;
                    SYSTEM_LOOKUPDTO objNew = new SYSTEM_LOOKUPDTO();
                    objNew.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                    objNew.TABLE_NAME = CNST_RESERVED_VARIABLE_GLOBAL;
                    objNew.FIELD_NAME = itm;
                    objNew.VALUE = -1;
                    objNew.DISPLAY_NAME = "";
                    objNew.STATUS = 0;
                    marsEntities.SYSTEM_LOOKUP.Add(objNew.ToEntity());
                }
                marsEntities.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("createGlobalVar",strError = string.Format("Exception :[{0}], stacktrace:[{1}]",e.Message,e.StackTrace), e);
                return false;
            }
        }
#endif

#if !_forWebClient
        public bool createModualvar(List<string> lstName, ref string strError, string strDBIdx, MarsEntities objDBCntx = null, short? sStatus = null)
        {
            Logger.Info("createModualvar", string.Format("varibals:[{0}] status:[{1}]", lstName==null?"":string.Join(",", lstName), sStatus));           
            try
            {

                MarsEntities marsEntities = objDBCntx == null ? Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDBCntx;
                marsEntities.Set<SYSTEM_LOOKUP>();

                foreach (var itm in lstName)
                {
                    if (string.IsNullOrEmpty(itm)) continue;

                    var m = (from p in marsEntities.SYSTEM_LOOKUP
                            where p.TABLE_NAME == "MODAL_VAR"
                            && p.FIELD_NAME==itm
                            select p)
                            .ToList();
                    DbConnection dbcnn = null;
                    if (m.Count == 0)
                    {
                        //说明没有
                        //创建两个
                        SYSTEM_LOOKUPDTO objNew = new SYSTEM_LOOKUPDTO();
                        objNew.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                        objNew.TABLE_NAME = CNST_RESERVED_VARIABLE_MODAL;
                        objNew.FIELD_NAME = itm;
                        objNew.VALUE = -1;
                        objNew.DISPLAY_NAME = itm;
                        objNew.STATUS = 1;

                        marsEntities.SYSTEM_LOOKUP.Add(objNew.ToEntity());

                        objNew = new B_SYSTEM_LOOKUP();
                        objNew.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                        objNew.TABLE_NAME = CNST_RESERVED_VARIABLE_MODAL;
                        objNew.FIELD_NAME = itm;
                        objNew.VALUE = -1;
                        objNew.DISPLAY_NAME = itm;
                        objNew.STATUS = 2;
                        marsEntities.SYSTEM_LOOKUP.Add(objNew.ToEntity());

                    }
                    if (sStatus == null)
                    {
                        SYSTEM_LOOKUPDTO objNew = new SYSTEM_LOOKUPDTO();
                        objNew.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                        objNew.TABLE_NAME = CNST_RESERVED_VARIABLE_MODAL;
                        objNew.FIELD_NAME = itm;
                        objNew.VALUE = -1;
                        objNew.DISPLAY_NAME = itm;
                        objNew.STATUS = 1;

                        marsEntities.SYSTEM_LOOKUP.Add(objNew.ToEntity());

                        objNew = new B_SYSTEM_LOOKUP();
                        objNew.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                        objNew.TABLE_NAME = CNST_RESERVED_VARIABLE_MODAL;
                        objNew.FIELD_NAME = itm;
                        objNew.VALUE = -1;
                        objNew.DISPLAY_NAME = itm;
                        objNew.STATUS = 2;
                        marsEntities.SYSTEM_LOOKUP.Add(objNew.ToEntity());
                    }
                    else
                    {
                        SYSTEM_LOOKUPDTO objNew = new SYSTEM_LOOKUPDTO();
                        objNew.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                        objNew.TABLE_NAME = CNST_RESERVED_VARIABLE_MODAL;
                        objNew.FIELD_NAME = itm;
                        objNew.VALUE = -1;
                        objNew.DISPLAY_NAME = itm;
                        objNew.STATUS =(short) (sStatus.HasValue?sStatus.Value:1);

                        marsEntities.SYSTEM_LOOKUP.Add(objNew.ToEntity());
                    }
                }
                marsEntities.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("createModualvar",strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            finally
            {
                Logger.logEnd("createModualvar");
            }
            
        }
#else
        public bool createModualvar(string strDBIdx, List<string> lstName, ref string strError,short? sStatus = null)
        {
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            bool isOk = false;
            strError = "";
            int iCnt = clnt.CreateModualVar(lstName, ref isOk, ref strError, sStatus);
            if (!isOk) return false;
            if (iCnt <= 0)
            {
                if (string.IsNullOrEmpty(strError))
                {
                    strError = string.Format("can't create Modualvars for [{0}]", string.Join(",", lstName??new List<string>()));
                    return false;
                }
            }
            return true;
        }
#endif

#if !_noEntities
        public bool CreateEmptyItem(string strDBIdx , //= MarsEntitiesExtends.cnst_default_dbName,
            string strTable, string strField, ref string strError, MarsEntities objDBCntx = null,short sStatus = 0,short iValue=-10
            )
        {
            Logger.Info("CreateEmptyItem", string.Format("strTable:[{0}] strField:[{1}]", strTable, strField));
            MarsEntities marsEntities = objDBCntx == null ? Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx) : objDBCntx;
            try
            {
                marsEntities.Set<MarsEntities>();
                B_SYSTEM_LOOKUP objNew = new B_SYSTEM_LOOKUP();

                objNew.TABLE_NAME = strTable;
                objNew.FIELD_NAME = strField;
                objNew.VALUE = iValue;
                objNew.DISPLAY_NAME = null;
                objNew.STATUS = sStatus;
                bool isOk = objNew.InsertSelf(strDBIdx, ref strError, marsEntities);

                if (isOk)
                {
                    if (objDBCntx == null)
                        marsEntities.SaveChanges();
                }

                return isOk;
            }
            catch (Exception e)
            {
                Logger.Error("CreateEmptyItem",strError= string.Format("Exceptions:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;                
            }
        }

        public bool InsertSelf(string strDBIdx ,ref string strError, MarsEntities objDBCntx = null)//= MarsEntitiesExtends.cnst_default_dbName )
        {
#if !_forWebClient
            Logger.logBegin("InsertSelf", $"[{this.TABLE_NAME}]-[{this.FIELD_NAME}]-[{this.DISPLAY_NAME}]");
            MarsEntities marsEntities = objDBCntx == null?Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx):objDBCntx;
            try
            {
                var query = from o in marsEntities.SYSTEM_LOOKUP
                            where
                                o.TABLE_NAME == this.TABLE_NAME
                             && o.FIELD_NAME == this.FIELD_NAME
                            select o;
                if (string.Compare(CNST_RESERVED_VARIABLE_MODAL, this.TABLE_NAME, true) == 0)
                {
                    List<SYSTEM_LOOKUPDTO> lstValues = null;
                    if ((lstValues = query.ToDTOs()).Count>=2)
                    {
                        strError = string.Format("There Already exists two values for modal variable -{0}", this.FIELD_NAME);
                        return false;
                    }
                    var lst = lstValues.Select(p=>p.STATUS).Distinct().ToList();
                    if (lst.Count>2)
                    {
                        strError = string.Format("There are more than 2 modal varial for modal variable:[{0}]", this.FIELD_NAME);
                        return false;
                    }
                    if ((lst.Count==1))
                    {
                        //存在一个，添加另外一个
                        if (lst[0]==1)
                        {
                            this.STATUS = 2;
                            this.VALUE = -1;
                        }
                        else
                        {
                            this.STATUS = 1;
                            this.VALUE = 1;
                        }
                        marsEntities.Set<SYSTEM_LOOKUP>();
                        this.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                        marsEntities.SYSTEM_LOOKUP.Add(SYSTEM_LOOKUPAssembler.ToEntity(this));
                        if (objDBCntx == null)
                            marsEntities.SaveChanges();
                        return true;
                        //strError = string.Format("There Already exists the same modal variable for modal variable -{0}", this.FIELD_NAME);
                        //return false;
                    }
                    else
                    {
                        //添加两个
                        this.STATUS = 1;
                        //this.VALUE = 1;
                        marsEntities.Set<SYSTEM_LOOKUP>();
                        this.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                        marsEntities.SYSTEM_LOOKUP.Add(SYSTEM_LOOKUPAssembler.ToEntity(this));

                        SYSTEM_LOOKUP theOtherModal = this.ToEntity();
                        theOtherModal.STATUS = 2;
                        if (this.VALUE != -1)
                            theOtherModal.VALUE = -1;
                        else
                            theOtherModal.VALUE = 1;
                        theOtherModal.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                        marsEntities.SYSTEM_LOOKUP.Add(theOtherModal);

                        if (objDBCntx == null)
                            marsEntities.SaveChanges();

                        return true;
                    }
                }
                else
                {
                    SYSTEM_LOOKUP oE = query.FirstOrDefault();
                    if (oE != null)
                    {
                        strError = "Variable " + oE.FIELD_NAME + " already exists. Insert action failed";
                        //Logger.Error("updateSelf", strError = string.Format("Unable to locate the object from system_lookup with id:[{0}]", this.LOOKUP_ID));
                        return false;
                    }
                }

                marsEntities.Set<SYSTEM_LOOKUP>();
                this.LOOKUP_ID = BoHelper.GetIdBySeqName(SYSTEM_LOOKUP_SEQ, marsEntities);
                marsEntities.SYSTEM_LOOKUP.Add(SYSTEM_LOOKUPAssembler.ToEntity(this));
                if (objDBCntx==null)
                    marsEntities.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertSelf", strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);

            bool isOK = clnt.InsertSystemLookup(this, ref strError);
            if (!isOK) return false;
            return true;
#endif
        }
#endif

        public bool updateSelf(string strData2Store, ref string strError, string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {
#if !_forWebClient
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var query = from o in marsEntities.SYSTEM_LOOKUP
                            where
                                o.LOOKUP_ID == this.LOOKUP_ID
                            select o;
                SYSTEM_LOOKUP oE = query.FirstOrDefault();
                if (oE==null)
                {
                    strError = "";
                    Logger.Error("updateSelf", strError = string.Format("Unable to locate the object from system_lookup with id:[{0}]",this.LOOKUP_ID));
                    return false;
                }
                marsEntities.Set<SYSTEM_LOOKUP>();
                marsEntities.SYSTEM_LOOKUP.Attach(oE);
                oE.DISPLAY_NAME = strData2Store;
                marsEntities.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateself", strError = string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
#else
            MarsRESTfulApiClient clnt = new MarsRESTfulApiClient(strDBIdx);
            B_SYSTEM_LOOKUP tmp = new B_SYSTEM_LOOKUP();
            tmp.LOOKUP_ID = this.LOOKUP_ID;
            tmp.DISPLAY_NAME = strData2Store;
            bool isOK = clnt.UpdateSystemLookup(tmp, ref strError);
            if (!isOK) return false;
            return true;
#endif
        }


#if !_noEntities
        public bool updateSelf(B_SYSTEM_LOOKUP sysLookup, ref string strError, string strDBIdx) // = MarsEntitiesExtends.cnst_default_dbName)
        {
            MarsEntities marsEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: strDBIdx);
            try
            {
                var query = from o in marsEntities.SYSTEM_LOOKUP
                            where
                                o.LOOKUP_ID == this.LOOKUP_ID
                            select o;
                SYSTEM_LOOKUP oE = query.FirstOrDefault();
                if (oE == null)
                {
                    strError = "";
                    Logger.Error("updateSelf", strError = string.Format("Unable to locate the object from system_lookup with id:[{0}]", this.LOOKUP_ID));
                    return false;
                }
                marsEntities.Set<SYSTEM_LOOKUP>();
                marsEntities.SYSTEM_LOOKUP.Attach(oE);
                oE.DISPLAY_NAME = sysLookup.DISPLAY_NAME;
                oE.TABLE_NAME = sysLookup.TABLE_NAME;
                oE.VALUE = sysLookup.VALUE;
                oE.STATUS = sysLookup.STATUS;
                marsEntities.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateself", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        public bool deleteSelf(ref string strError, string strDBIdx )
        {
            MarsEntities marsEntities = Mars.message.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var query = from o in marsEntities.SYSTEM_LOOKUP
                            where
                                o.LOOKUP_ID == this.LOOKUP_ID
                            select o;
                SYSTEM_LOOKUP oE = query.FirstOrDefault();
                if (oE == null)
                {
                    strError = "";
                    Logger.Error("updateSelf", strError = string.Format("Unable to locate the object from system_lookup with id:[{0}]", this.LOOKUP_ID));
                    return false;
                }
                
                marsEntities.SYSTEM_LOOKUP.Remove(oE);
                marsEntities.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("updateself", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }
#endif
        public static B_SYSTEM_LOOKUP createDefault()
        {
            B_SYSTEM_LOOKUP rslt = new B_SYSTEM_LOOKUP();
            rslt.DISPLAY_NAME = "DEFAULT";
            rslt.FIELD_NAME = "CREATE A VAR FIRST";
            rslt.TABLE_NAME = "GLOBAL_VAR";
            rslt.VALUE = 1;
            rslt.STATUS = 1;
            return rslt;
        }
    }

    
}
