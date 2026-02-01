using Mars.auto.LoadDataFromDB.auto.db;
using Mars.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel.SystemTools
{
    class SQLServerConnectionSettingModel:ViewModelBase
    {
        public SQLServerConnectionSettingModel():base()
        {
            ///算法 load from config.cfg file
            /// 
            InitBaseData();
        }

        private Mars.auto.LoadDataFromDB.auto.db.ThirdPartDBCnn currentCfgInfo = null;// new DbCnnXmlFor3rd();

        private void InitBaseData()
        {
            //currentCfgInfo = new ThirdPartDBCnn();
            string strFileNameWith = System.IO.Path.Combine(SystemCommonUtil.GetCurrentPathDir(), string.Format("..\\{0}\\Connection.cfg", MarsConstants.CNST_AUTO_LOAD_DATA_DIRECTORY));
            bool isOk = false;
            string strError = "";
            DbCnnXmlFor3rd xmlCnn = DbCnnXmlFor3rd.LoadFromFile(strFileNameWith,ref isOk, ref strError);
            if (!isOk)
            {
                return;
            }
            if (string.Compare("SQL Server", xmlCnn.DatabaseType, true) != 0) return;//none 

            this.DbUserName = xmlCnn.UserName;
            this.DbPassword = Mars.Securities.MarsEncodePwd.DecodeString(xmlCnn.PassWord);
            string strDataSource = xmlCnn.DataSource;
            string[] arrDBServer = strDataSource.Split(new string[] {";","\\" },StringSplitOptions.RemoveEmptyEntries);
            if ((arrDBServer == null) || (arrDBServer.Length != 3)) return;
            this.DbServerName = arrDBServer[0];
            this.DbInstanceName = arrDBServer[1];
            this.DatabaseName = arrDBServer[2];
        }

        private string dbServerName;  
        public string DbServerName
        {
            get {
                return dbServerName; 
            }
            set
            {
                dbServerName = value;
                RaisePropertyChanged("DbServerName");
            }
        }

        private string dbName;
        public string DatabaseName
        {
            get
            {
                return dbName;
            }
            set
            {
                dbName = value;
                RaisePropertyChanged("DatabaseName");
            }
        }

        private string dbUserName;
        public string DbUserName
        {
            get { return dbUserName; }
            set
            {
                dbUserName = value;
                RaisePropertyChanged("DbUserName");
            }
        }

        private string dbPassword;
        public string DbPassword
        {
            get
            {
                return dbPassword;
            }
            set
            {
                dbPassword = value;
                RaisePropertyChanged("DbPassword");
            }
        }

        private string dbInstanceName;
        public string DbInstanceName
        {
            get
            {
                return dbInstanceName;
            }
            set
            {
                dbInstanceName = value;
                RaisePropertyChanged("DbInstanceName");
            }
        }

        private string tracelog;
        public string TraceLog
        {
            get { return tracelog; }
            set { tracelog = value;RaisePropertyChanged("TraceLog"); }
        }
        private bool ConnectionSettingValidated(ref string strError)
        {
            if (string.IsNullOrEmpty(dbServerName)||string.IsNullOrEmpty(dbName)
                ||string.IsNullOrEmpty(dbUserName)||string.IsNullOrEmpty(dbPassword))
            {
                strError = "Please input all Information.";
                return false;
            }
            return true;
        }
        /**
        Server=myServerName\myInstanceName;Database=myDataBase;User Id=myUsername;
    Password=myPassword;
        **/
        public DelegateCommand TestConnectionCommand
        {
            get
            {
                return new DelegateCommand(new Action(()=> {
                    string strError = "";
                    if (!ConnectionSettingValidated(ref strError))
                    {
                        HintByMessageBox(strError, "ERROR");
                        return;
                    }
                    try
                    {
                        System.Data.SqlClient.SqlConnection cnn = new System.Data.SqlClient.SqlConnection(string.Format("Data Source={0}\\{1};Initial Catalog={2};User ID={3};Password={4};TrustServerCertificate=true",
                        this.dbServerName, this.dbInstanceName, this.dbName, this.dbUserName, this.DbPassword
                        ));
                        cnn.Open();
                        HintByMessageBox(TraceLog="Connection sucess.");
                        cnn.Close();
                        cnn = null;
                    }
                    catch (Exception e)
                    {
                        TraceLog += (strError = string.Format("Exception:[{0}]\r\nStackTrace:[{1}]", e.Message,e.StackTrace));
                        //Logger.Error

                    }
                    
                }));
            }
        }

        public DelegateCommand SaveCommand
        {
            get
            {
                return new DelegateCommand(new Action(() =>{
                    //Write to dbconfig.cfg file under 
                    DbCnnXmlFor3rd xmlCnn = new DbCnnXmlFor3rd();
                    xmlCnn.DatabaseType = "SQL Server";
                    xmlCnn.DatabaseConnectionType = "";
                    xmlCnn.DataSource = string.Format("{0}\\{1};{2}",this.dbServerName,this.dbInstanceName,this.dbName);
                    xmlCnn.UserName = this.dbUserName;
                    xmlCnn.PassWord = Mars.Securities.MarsEncodePwd.EncodeString(this.dbPassword);
                    
                    string strFileNameWith = System.IO.Path.Combine(SystemCommonUtil.GetCurrentPathDir(), string.Format("..\\{0}\\Connection.cfg", MarsConstants.CNST_AUTO_LOAD_DATA_DIRECTORY));
                    string strError = "";
                    if (xmlCnn.SaveToFile(strFileNameWith, ref strError))
                    {
                        HintByMessageBox(string.Format("SQL Server configuration saved to file:\r\n[{0}]",strFileNameWith));
                        return;
                    }
                    HintByMessageBox(string.Format("Error when save Sql server to File:[{0}]\r\n{1}", strError));
                })) ;
            }
        }
    }
}
