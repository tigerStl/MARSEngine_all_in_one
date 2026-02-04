using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace MarsTestFrame.SourceCode.xmlConfig
{
    [Serializable]
    [XmlRoot(ElementName = "WcfService")]
    public class WCFXmlCfgMgr: INotifyPropertyChanged
    {
        
        [XmlIgnore]
        public static string CurrentLoginUser = null;

        private const string cnst_defaultFileName = "MarsService.xml";
        private const string cnst_wcf_service_node_name = "ServiceNode";
        private static WCFXmlCfgMgr wcfInst = null;
        public static WCFXmlCfgMgr WCFInst
        {
            get
            {
                return wcfInst;
            }
            set
            {
                wcfInst = value;
            }
        }
        public static WCFXmlCfgMgr LoadFromFile(ref bool isOk,
            ref string strError , 
            string strFileName = cnst_defaultFileName)
        {
            try
            {
#if _NO_C_DRIVER_WRITE
                string strPath = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
                strPath = Path.Combine(Path.GetDirectoryName(strPath), strFileName);
                //MessageBox.Show(strPath);
#else
                string strPath = typeof(WCFXmlCfgMgr).Assembly.Location;
                strPath = Path.Combine(Path.GetDirectoryName(strPath), strFileName);
#endif
                if (!File.Exists(strPath))
                {
#if _NO_C_DRIVER_WRITE
                    //首先 copy原始文件 
                    string strOldPath = typeof(WCFXmlCfgMgr).Assembly.Location;
                    strOldPath = Path.Combine(Path.GetDirectoryName(strOldPath), strFileName);
                    if (!File.Exists(strOldPath))
                    {
                        isOk = false;
                        strError = string.Format("Can't find source [{0}] from [{1}]", cnst_defaultFileName, strOldPath);
                        MessageBox.Show(strError);
                        return null;
                    }
                    File.Copy(strOldPath, strPath);
#else
                    SaveToXmlFile(new WCFXmlCfgMgr());
#endif
                }
                using (FileStream fs = new FileStream(strPath, FileMode.Open,FileAccess.Read, FileShare.Read))
                {
                    XmlSerializer xWriter = new XmlSerializer(typeof(WCFXmlCfgMgr));
                    
                    object oInst = xWriter.Deserialize(fs);
                    wcfInst = oInst as WCFXmlCfgMgr;
                }
                isOk = true;

                ///如果为空 就创建有个默认的账户
                if ((wcfInst.wcfNodes==null)||(wcfInst.wcfNodes.Count==0))
                {
                    //create default one
                    wcfInst.WCFNodes.Add(new WCFServiceNode()
                    {
                        //AccountName = Environment.UserName ,
                        AccountName = CurrentLoginUser,
                        ServerPort = 9888,
                        MonitorPort = 9889
                    });
                }
                SaveToXmlFile(wcfInst);
                return wcfInst;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception:[{0}]\r\nStackTrace:{1}", e.Message, e.StackTrace);
                return wcfInst = null;
            }
        }

        public static void SaveToXmlFile(WCFXmlCfgMgr objInst,string strFileName = cnst_defaultFileName)
        {

            XmlSerializer xWriter = new XmlSerializer(typeof(WCFXmlCfgMgr));
#if _NO_C_DRIVER_WRITE
            string strPath = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
            strPath = Path.Combine(strPath, strFileName);
#else
            string strPath = typeof(WCFXmlCfgMgr).Assembly.Location;
            strPath = Path.Combine(Path.GetDirectoryName(strPath), strFileName);
#endif
            FileStream fs = null;
            using (fs = new FileStream(strPath, FileMode.OpenOrCreate))
            {
                
                xWriter.Serialize(fs, objInst);
                fs.Flush();
                fs.Close();
            }
        }
        private ObservableCollection<WCFServiceNode> wcfNodes;
        [XmlArrayItem(ElementName = cnst_wcf_service_node_name, Type =typeof(WCFServiceNode))]
        public ObservableCollection<WCFServiceNode> WCFNodes { get { return wcfNodes; } set { wcfNodes = value; OnPropertyChanged("WCFNodes"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        internal void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }

        internal static WCFServiceNode GetCurrerntWcfNodeInfo(ref bool isOk, ref string strError)
        {
            ///use inner account name
            ///

            //string currentAccountName = Environment.UserName;
            string currentAccountName = CurrentLoginUser;

            isOk = true;
            if (wcfInst == null)
                LoadFromFile(ref isOk, ref strError);
            if ((!isOk)||(wcfInst==null))
            {
                return null;
            }
            WCFServiceNode oNode = wcfInst.WCFNodes.Where(p => string.Compare(currentAccountName, p.AccountName, true) == 0).FirstOrDefault();
            if (oNode==null)
            {
                strError = string.Format("No wcf information for user [{0}] is configured", currentAccountName);
                isOk = false;
                return null;
            }
            return oNode;
        }
    }

    public class WCFServiceNode: INotifyPropertyChanged
    {
        private const string cnst_account = "AccountName";

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(AccountName) || (MonitorPort <= 1000) || (ServerPort <= 1000);
        }

        private const string cnst_framePort = "ServicePort";
        private const string cnst_monitorPort = "MonitorPort";

        private string accountName;
        [XmlAttribute(cnst_account)]
        public string AccountName { get { return accountName; } set { accountName = value;OnPropertyChanged("AccountName"); } }
        private int serverPort;
        [XmlAttribute(cnst_framePort)]
        public int ServerPort { get
            {
                return serverPort;
            } set
            {
                serverPort = value;
                OnPropertyChanged("ServerPort");
            }
        }
        private int monitorPort;
        [XmlAttribute(cnst_monitorPort)]
        public int MonitorPort { get
            {
                return monitorPort;
            }
            set
            {
                monitorPort = value;
                OnPropertyChanged("MonitorPort");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        internal void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
    }
}
