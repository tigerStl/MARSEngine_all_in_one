using Mars.network;
using Mars.Views.baseView;

#if Managed_Driver
using Oracle.ManagedDataAccess.Client;
using Route2NSEx.src.Marquis.systemUtil;
#else
using Oracle.DataAccess.Client;
#endif
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for MarsNetWorkTool.xaml
    /// </summary>
    public partial class MarsNetWorkTool :
        MarsBaseViewControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsNetWorkTool));
        public MarsNetWorkTool()
        {
            InitializeComponent();
            DataContext = this;
        }

        private string currentSqlOrTableSummary;
        public string CurrentSqlOrTableSummary
        {
            get
            {
                return currentSqlOrTableSummary;
            }
            set
            {
                currentSqlOrTableSummary = value;
                RaisePropertyChanged("CurrentSqlOrTableSummary");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private static MarsTelnetConnection currentConnection = null;
        private void TestButtonClick(object sender, RoutedEventArgs ev)
        {
            string strHostOrIp = edtMachineName.Text.Trim(),
                strPort = edtPort.Text.Trim();
            int iPort;
            if (!int.TryParse(strPort,out iPort)||string.IsNullOrEmpty(strHostOrIp))
            {
                MessageBox.Show("Please input validate HostName and Port.\r\nPort should be Number.", "Hint");
                return;
            }
            try
            {
                currentConnection = new MarsTelnetConnection(strHostOrIp, iPort);
                if (currentConnection.IsConnected)
                    txtLog.Text = string.Format("{2}\r\n{0:MM/yyyy/dd HH:mm:ss} Connected to {1}:{3} ", DateTime.Now, strHostOrIp,txtLog.Text,iPort);
                string strDataFromTxt = currentConnection.Read();
                txtLog.Text = string.Format("{0}\r\nGet Txt From server{1}:{2}", txtLog.Text, strHostOrIp, strDataFromTxt);
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Exception when call Telnet\r\n{0}\r\n{1}",e.Message,e.StackTrace));
                return;
            }
        }

        private void SendCommand(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!currentConnection.IsConnected)
                {
                    MessageBox.Show("No TCP connections.");
                    return;
                }
                currentConnection.WriteLine(edtCmmd.Text.Trim());
                Thread.Sleep(2000);
                string strDataFromTxt = currentConnection.Read();
                txtLog.Text = string.Format("{0}\r\nGet Txt From server{1}:{2}", txtLog.Text, edtMachineName.Text.Trim(), strDataFromTxt);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection might be closed.");
            }
            
        }

        private void ClearLog(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        private void ClickSaveDBConnectionType(object sender, RoutedEventArgs e)
        {
            
        }

        private void RunACommand(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(edtCmmd.Text)) return;
            try
            {
                Process p = Process.Start(new ProcessStartInfo(edtCmmd.Text.Trim()));
                txtLog.AppendText(string.Format("process:[{0}]", p.Id));
            }catch(Exception ex)
            {
                Logger.Error("RunACommand",string.Format("{0}\r\n\t{1}",ex.Message,ex.StackTrace),ex);
            }
        }

       
        private void DoButton_Click_1(object sender, RoutedEventArgs e)
        {
            
            if (OracleConnectionGui.CurrentOracleConnection == null)
            {
                MessageBox.Show("Connect to database first.");
                return;
            }
            if (OracleConnectionGui.CurrentOracleConnection.State!= ConnectionState.Open)
            {
                try
                {
                    OracleConnectionGui.CurrentOracleConnection.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Exceptions when open conection:[{0}]",ex.Message));
                    Logger.Error("DoButton_Click_1", ex.Message,ex);
                    return;
                }
            }

            if (string.IsNullOrEmpty(this.TestQueryInfo.Text))
            {
                MessageBox.Show("Please input query or Table Name.");
                this.TestQueryInfo.Focus();
                return;
            }

            string strCmmd = "";
            if (this.TestQueryInfo.Text.Trim().ToUpper().StartsWith("SELECT"))
            {
                strCmmd = this.TestQueryInfo.Text;
            }
            else
            {
                strCmmd = string.Format("SELECT * FROM {0}", this.TestQueryInfo.Text);
            }
            try
            {
                string strFileds = "";
                StringBuilder sb = new StringBuilder();
                using (DbCommand dbCmd = this.OracleConnectionGui.CurrentOracleConnection.CreateCommand())
                {
                    dbCmd.CommandText = strCmmd;
                    DbDataReader dbRd =dbCmd.ExecuteReader();
                    int iRowCnt = 0;
                    
                    bool headerInitialized = false;
                    while((dbRd.Read())&&((iRowCnt++)<100))
                    {
                        StringBuilder strCurrentRow = new StringBuilder();
                        for (int i=0;i<dbRd.FieldCount;i++)
                        {
                            if (!headerInitialized)
                            {
                                strFileds += string.Format("{0,20}", dbRd.GetName(i));
                            }
                            strCurrentRow.Append(string.Format("{0,20}", dbRd[i] == null ? " " : dbRd[i].ToString()));
                        }
                        strCurrentRow.Append("\r\n");
                        headerInitialized = true;
                        sb.Append(strCurrentRow);
                    }
                }
                this.QueryResultText.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                string strError = "";
                Logger.Error("strCurrentRow", strError=string.Format("Exception:[{0}]",ex.Message), ex);
                MessageBox.Show(strError);
            }
            

        }
    }
}
