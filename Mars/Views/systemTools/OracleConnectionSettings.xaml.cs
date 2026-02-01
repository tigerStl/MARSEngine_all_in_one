#if Managed_Driver
using Mars.auto.LoadDataFromDB.auto.db;
using Mars.Utility;
using Mars.ViewModel;
using Oracle.ManagedDataAccess.Client;
#else
using Oracle.DataAccess.Client;
#endif
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mars.Views.systemTools
{
    /// <summary>
    /// Interaction logic for OracleConnectionSettings.xaml
    /// </summary>
    public partial class OracleConnectionSettings : UserControl
    {

        public OracleConnectionSettings()
        {
            InitializeComponent();
        }


        private void ClickTestDBConnection(object sender, RoutedEventArgs e)
        {
            if (connectTypeDropList.SelectedIndex == -1) return;
            switch (connectTypeDropList.SelectedIndex)
            {
                case 0:
                    /// connection from tns name
                    /// 
                    ConnectionDBFromTNSName();
                    break;
                case 1:
                    ConnectionDBFromTNSString();
                    break;
                case 2:
                    break;
                default:
                    return;
            }
        }

        public DbConnection CurrentOracleConnection = null;

        private void ConnectionDBFromTNSString()
        {
            if (connectTypeDropList.SelectedIndex == -1) return;
            string strTnsInfo = edtConnectionDBInfo.Text.Trim();
            strTnsInfo = strTnsInfo.Replace("\r", "");
            strTnsInfo = strTnsInfo.Replace("\n", "");

            CurrentOracleConnection = new OracleConnection();
            CurrentOracleConnection.ConnectionString = string.Format("Data Source={0};User Id={1};Password={2}", strTnsInfo,
                txtDBUserName.Text.Trim(), txtPassword.Text.Trim());
            try
            {
                CurrentOracleConnection.Open();
                txtDBLog.Text = string.Format("{2}\r\n{0:MM/yyyy/dd HH:mm:ss} Connected to Database {1}/{3} ", DateTime.Now, txtDBUserName.Text.Trim(), strTnsInfo, txtPassword.Text.Trim());
                MessageBox.Show("Connection opened");
            }
            catch (Exception e)
            {
                string strTmp = "";
                MessageBox.Show(strTmp = string.Format("Exception:[{0}]\r\n{1}\r\nInnerException:{3}\r\n{2}", e.Message, e.StackTrace,
                    e.InnerException == null ? "N/A" : e.InnerException.StackTrace
                    , e.InnerException == null ? "N/A" : e.InnerException.Message));
                txtDBLog.Text = string.Format("{0}\r\n{1}", txtDBLog.Text, strTmp);
            }
        }

        private void ConnectionDBFromTNSName()
        {
            //Data Source = TORCL; User Id = urUsername; Password = urPassword;
            CurrentOracleConnection = new OracleConnection();
            CurrentOracleConnection.ConnectionString = string.Format("Data Source={0};User Id={1};Password={2}", edtConnectionDBInfo.Text.Trim(),
                txtDBUserName.Text.Trim(), txtPassword.Text.Trim());
            try
            {
                CurrentOracleConnection.Open();
                txtDBLog.Text = string.Format("{2}\r\n{0:MM/yyyy/dd HH:mm:ss} Connected to Database {1}/{3} ", DateTime.Now, txtDBUserName.Text.Trim(), edtConnectionDBInfo.Text.Trim(), txtPassword.Text.Trim());
                MessageBox.Show("Connection opened");
            }
            catch (Exception e)
            {
                string strTmp = "";
                MessageBox.Show(strTmp = string.Format("Exception:[{0}]\r\n{1}\r\nInnerException:{3}\r\n{2}", e.Message, e.StackTrace,
                    e.InnerException == null ? "N/A" : e.InnerException.StackTrace
                    , e.InnerException == null ? "N/A" : e.InnerException.Message));
                txtDBLog.Text = string.Format("{0}\r\n{1}", txtDBLog.Text, strTmp);
            }
        }

        private void ClickSaveDBConnectionType(object sender, RoutedEventArgs e)
        {
            ///将设置 设置到默认的配置文件中
            /// 位置为%MARS%autoLoadDataCfg
            /// 
            DbCnnXmlFor3rd objXmlDBConn = new DbCnnXmlFor3rd();
            objXmlDBConn.DatabaseType = "ORACLE";
            switch(this.connectTypeDropList.SelectedIndex)
            {
                case 0:
                    objXmlDBConn.DatabaseConnectionType = "TNSNAME";
                    break;
                case 1:
                    objXmlDBConn.DatabaseConnectionType = "TNSString";
                    break;
                default:
                    objXmlDBConn.DatabaseConnectionType = "Entity Framework";
                    break;
            }
            //objXmlDBConn.DatabaseConnectionType = this.connectTypeDropList.SelectedIndex==0?"DNS" this.connectTypeDropList.SelectedItem.ToString();
            string strTnsInfo = edtConnectionDBInfo.Text.Trim();
            strTnsInfo = strTnsInfo.Replace("\r", "");
            strTnsInfo = strTnsInfo.Replace("\n", "");
            objXmlDBConn.DataSource = strTnsInfo;
            objXmlDBConn.UserName = this.txtDBUserName.Text.Trim();
            ///加密密码
            /// 
            string strPwd = this.txtPassword.Text.Trim();
            if (string.IsNullOrEmpty(strPwd)||string.IsNullOrEmpty(objXmlDBConn.UserName))
            {
                ViewModelBase.HintByMessageBox("Plese set password and userName for database.", "ERROR");
                return;
            }
            objXmlDBConn.PassWord = Mars.Securities.MarsEncodePwd.EncodeString(strPwd);
            string strFileName = "", strError="";            
            bool isOk = objXmlDBConn.SaveToFile(strFileName=System.IO.Path.Combine(SystemCommonUtil.GetCurrentPathDir(), string.Format("..\\{0}\\Connection.cfg", MarsConstants.CNST_AUTO_LOAD_DATA_DIRECTORY)),ref strError);
            if (!isOk)
            {
                ViewModelBase.HintByMessageBox(string.Format("Can't save file to [{0}] with Error:[{1}]", strFileName,strError),"ERROR");
            }
            else
            {
                ViewModelBase.HintByMessageBox(string.Format("Database Information is saved to [{0}]",strFileName),"Hint") ;
            }
        }
    }
}
