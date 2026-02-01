using Mars.auto.LoadDataFromDB.auto.basedatastructure.MarsCfg;
using Mars.auto.LoadDataFromDB.auto.db;
using Mars.Business;
using Mars.Dialog.autoLoadData;
using Mars.performance.systemInfo;
using Mars.Utility;
using Oracle.ManagedDataAccess.Client;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mars.ViewModel.objectManagement
{
    public class ObjectDataSourceViewModel:ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ObjectDataSourceViewModel));

        private string currentObjectName;
        public string CurrentObjectName
        {
            get { return currentObjectName; }
            set
            {
                currentObjectName = value;
                RaisePropertyChanged("CurrentObjectName");
            }
        }

        private long objectId;
        public long ObjectId
        {
            get
            {
                return objectId;
            }
            set
            {
                objectId = value;
            }
        }

        private MarsObjectDataSourceMapping currentObjectMappingInfo=null;
        
        public ObjectDataSourceViewModel():this(null,"N/A",-1)
        {
            
        }

        public ObjectDataSourceViewModel(byte[] arrDataByte, string strObjectName,long iObjId)
        {
            Logger.logBegin("ObjectDataSourceViewModel",string.Format("length:[{0}]", arrDataByte==null?0:arrDataByte.Length));
            ConvertBytesToMappingFile(arrDataByte);
            CurrentObjectName = strObjectName;
            objectId = iObjId;
            Logger.logEnd("ObjectDataSourceViewModel");
        }

        private void ConvertBytesToMappingFile(byte[] arrDataByte)
        {
            string strError = "",strPath = Path.GetTempPath(); 
            bool isOk = false;

            this.currentObjectMappingInfo = MarsObjectDataSourceMapping.LoadFromBytes(arrDataByte, ref isOk, ref strError);
            if (!isOk)
            {
                Logger.Warnning("ConvertBytesToMappingFile", strError);
            }
            else
            {
                RaisePropertyChanged("FieldMappingFrom");
            }            
        }

        private void CheckAndInitSourceMappingObj()
        {
            if (currentObjectMappingInfo == null)
                currentObjectMappingInfo = new MarsObjectDataSourceMapping();
        }

        public string FieldMappingFrom
        {
            get
            {
                if (currentObjectMappingInfo == null) return null;
                return currentObjectMappingInfo.DataSourceMapFrom;
            }
            set
            {
                CheckAndInitSourceMappingObj();
                currentObjectMappingInfo.DataSourceMapFrom = value;
                RaisePropertyChanged("FieldMappingFrom");
            }
        }

        public ICommand OnAddOneParameterClick
        {
            get
            {
                return new DelegateCommand(() => {
                    CheckAndInitSourceMappingObj();
                    if (currentObjectMappingInfo.DataSourceParameters == null)
                    {
                        currentObjectMappingInfo.DataSourceParameters = new ObservableCollection<MarsObjDataSrcMappingParameter>();
                    }
                    foreach(var itm in currentObjectMappingInfo.DataSourceParameters)
                    {
                        if (itm.IsEmptyObject())
                        {
                            HintByMessageBox("An empty row exists, please fill it or delete it before you click ok button.", "WARNNING");
                            return;
                        }
                    }
                    currentObjectMappingInfo.DataSourceParameters.Add(new MarsObjDataSrcMappingParameter());
                    
                });
            }
        }

        public ICommand OnDelParameterClick
        {
            get
            {
                return new DelegateCommand(()=> {
                    if (currentSelectedParameter==null || CurrentParamenters==null || CurrentParamenters.Count<=0)
                    {
                        HintByMessageBox("Please Select One item and Click delete button", "Hint");
                        return;
                    }
                    CurrentParamenters.Remove(currentSelectedParameter);
                });
            }
        }

       

        public ICommand OnClearParameterClick
        {
            get
            {
                return new DelegateCommand(() => {
                    if (CurrentParamenters == null || CurrentParamenters.Count <= 0)
                    {
                        HintByMessageBox("Nothing needs to be removed.");
                        return;
                    }
                    if (QuestionByMessageBox("Do you want to remove all records?","Hint"))
                    {
                        CurrentParamenters.Clear();
                        return;
                    }
                    
                });
            }
        }

        public ICommand OnSaveBtnClick
        {
            get
            {
                return new DelegateCommand(()=> {
                    ///将xml文件对象转换成byte[]
                    /// 然后存入数据库
                    /// 
                    string strError = "";
                    bool isOk = false;
                    currentObjectMappingInfo.SQL = this.CurrentSql;
                    byte[] arrData = MarsObjectDataSourceMapping.CreateMappingToBytes(this.currentObjectMappingInfo, ref strError, ref isOk);
                    if (!isOk)
                    {
                        
                        HintByMessageBox(string.Format("Excetpion when call CreateMappingToBytes:\r\n{0}", strError));
                        return;
                    }

                    //MarsObjectDataSourceMapping.LoadFromBytes(arrData, ref isOk, ref strError);
                    /// 存入数据库
                    /// 
                    B_REGISTED_OBJECT o = new B_REGISTED_OBJECT();
                    isOk = o.updateObjectDataSource(MarsMainWindow.CurrentDatabaseIdx, this.objectId, arrData,ref strError);
                    if (!isOk)
                    {
                        HintByMessageBox(strError, "ERROR");
                        return;
                    }
                    HintByMessageBox(string.Format("Updated object [{0}],sucessful.",this.currentObjectName),"Hint");
                });
            }
        }

        public ICommand OnTestSqlBtnClick
        {
            get{
                return new DelegateCommand(()=> {
                    if (string.IsNullOrEmpty(CurrentSql))
                    {
                        HintByMessageBox("SQL part is empty, please input a sql query.","WARNING");
                        return;
                    }
                    ///使用设置的链接进行sql测试
                    /// 
                    string strSql = this.CurrentSql;
                    ///获得dbconnection infor
                    /// 
                    string strError = "";
                    bool isOk = false;
                    string strFileNameWith = System.IO.Path.Combine(SystemCommonUtil.GetCurrentPathDir(), string.Format("..\\{0}\\Connection.cfg", MarsConstants.CNST_AUTO_LOAD_DATA_DIRECTORY));
                    DbCnnXmlFor3rd dbCnnCfg = DbCnnXmlFor3rd.LoadFromFile(strFileNameWith, ref isOk, ref strError);
                    if ((!isOk)||(dbCnnCfg==null))
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Can't load Database connection Configuration, \r\nwith Information :[{0}]",strError));
                        return;
                    }

                    if (this.CurrentParamenters.Count>0)
                    {
                        ///提供参数dialog
                        /// 
                        AutoLoadDataParameterSettingModal objParaSettingModel = new AutoLoadDataParameterSettingModal();                        
                        objParaSettingModel.ParametersNeedSet = this.CurrentParamenters;
                        //objParaSettingModel
                        AutoLoadDataParameterSetting objParaSettingDialog = new AutoLoadDataParameterSetting();

                        objParaSettingDialog.DataContext = objParaSettingModel;
                        bool? resultDialog = objParaSettingDialog.ShowDialog();
                        if (!(resultDialog??false))
                        {
                            return;
                        }
                        
                    }

                    try
                    {
                        DbConnection objCnn = null;
                        Type paraType = null;
                        if (string.Compare("SQL Server",dbCnnCfg.DatabaseType,true)==0)
                        {
                            objCnn = new System.Data.SqlClient.SqlConnection();
                            paraType = typeof(System.Data.SqlClient.SqlParameter);
                            //return;
                        }
                        else
                        {
                            if (string.Compare("ORACLE", dbCnnCfg.DatabaseConnectionType, true) == 0)
                            {
                                objCnn = new OracleConnection();
                                paraType = typeof(OracleParameter);
                            }
                            else
                            {
                                HintByMessageBox(string.Format("Unsupported Database Type [{0}], Only Oracle/SQL Server is Supported", dbCnnCfg.DatabaseType));
                                return;
                            }
                        }
                        
                        string strCnnstring = dbCnnCfg.getConnectionString();
                        objCnn.ConnectionString = strCnnstring;
                        using (objCnn)
                        {

                        //}

                        //using (OracleConnection objCnn = new OracleConnection())
                        //{
                            //dbCnnCfg.PassWordDecoded = Mars.Securities.MarsEncodePwd.DecodeString(dbCnnCfg.PassWordDecoded);
                            //objCnn.ConnectionString = string.Format("Data Source={0};User Id={1};Password={2}", dbCnnCfg.DataSource,
                                //dbCnnCfg.UserName, dbCnnCfg.PassWordDecoded);
                            objCnn.Open();

                            DbCommand dbCmmd = objCnn.CreateCommand();
                            dbCmmd.CommandText = this.CurrentSql;
                            bool isParameterError = false;

                            if (this.CurrentParamenters.Count > 0)
                            {
                                foreach (var itm in this.CurrentParamenters)
                                {
                                    //OracleParameter op = new OracleParameter();
                                    DbParameter op = Activator.CreateInstance(paraType) as DbParameter;
                                    op.ParameterName = itm.ParameterName;
                                    switch (itm.SourceType == null ? "STRING" : itm.SourceType.ToUpper())
                                    {
                                        case "FLOAT":
                                            float f;
                                            if (!float.TryParse(itm.ParameterValue, out f))
                                            {
                                                isParameterError = true;
                                                strError = string.Format("Data is not a float.[{0}]", itm.ParameterValue);
                                            }
                                            break;
                                        case "INT":
                                            int iV;
                                            if (!int.TryParse(itm.ParameterValue, out iV))
                                            {
                                                isParameterError = true;
                                                strError = string.Format("Data is not a int. [{0}]", itm.ParameterValue);
                                            }
                                            break;
                                        case "STRING":
                                        default:
                                            op.Value = itm.ParameterValue;
                                            break;
                                    }
                                    if (isParameterError)
                                        break;
                                    dbCmmd.Parameters.Add(op);
                                }
                                if (isParameterError)
                                {
                                    HintByMessageBox(strError, "ERROR");
                                    return;
                                }
                            }

                            DbDataReader dbRd = dbCmmd.ExecuteReader();
                            string strColName = "";
                            bool isFind = false;
                            Type fieldType = null;
                            int iFieldIdx = -1;
                            if ((dbRd != null) && (dbRd.HasRows) && (dbRd.Read()))
                            {
                                for (int i = 0; i < dbRd.FieldCount; i++)
                                {
                                    strColName = dbRd.GetName(i);
                                    fieldType = dbRd.GetFieldType(i);
                                    if (string.Compare(strColName, this.FieldMappingFrom, true) == 0)
                                    {
                                        isFind = true;
                                        iFieldIdx = i;
                                        break;
                                    }
                                }
                                if (!isFind)
                                {
                                    HintByMessageBox(string.Format("no such field [{0}] returns when test sql", FieldMappingFrom));
                                    return;
                                }
                                object od = dbRd[iFieldIdx];
                                string dt = od == null ? "NULL" : od.ToString();// dbRd.GetFieldValue<string>(iFieldIdx);
                                HintByMessageBox(string.Format("Find data [{0}] from db column:[{1}]", dt, strColName));
                            }
                            else
                            {
                                HintByMessageBox("No result returns.", "WARNING");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("OnTestSqlBtnClick", strError = string.Format("Exception when Open connections or execute sql\r\n{0}", e.Message), e);
                        ViewModelBase.HintByMessageBox(strError, "ERROR");
                        return;
                    }
                });
            }
        }

        //private string currentSql;
        public string CurrentSql
        {
            get {
                if (this.currentObjectMappingInfo == null)
                    this.currentObjectMappingInfo = new MarsObjectDataSourceMapping();
                return currentObjectMappingInfo.SQL;
            }
            set
            {
                if (this.currentObjectMappingInfo == null)
                    this.currentObjectMappingInfo = new MarsObjectDataSourceMapping();
                currentObjectMappingInfo.SQL = value;
                RaisePropertyChanged("CurrentSql");
            }
        }


        public ObservableCollection<MarsObjDataSrcMappingParameter> CurrentParamenters
        {
            get
            {
                CheckAndInitSourceMappingObj();
                if (currentObjectMappingInfo.DataSourceParameters == null)
                {
                    currentObjectMappingInfo.DataSourceParameters = new ObservableCollection<MarsObjDataSrcMappingParameter>();
                };
                return currentObjectMappingInfo.DataSourceParameters;
            }
            set
            {
                CheckAndInitSourceMappingObj();
                currentObjectMappingInfo.DataSourceParameters = value;
                RaisePropertyChanged("CurrentParamenters");
            }
        }

        private MarsObjDataSrcMappingParameter currentSelectedParameter = null;
        public MarsObjDataSrcMappingParameter CurrentSelectedParameter
        {
            get
            {
                return currentSelectedParameter;
            }

            set
            {
                currentSelectedParameter = value;
                RaisePropertyChanged("CurrentSelectedParameter");
            }
        }

    }
}
