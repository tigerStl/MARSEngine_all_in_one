
using Mars.network;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.SourceCode.xmlConfig;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mars.ViewModel.SystemTools
{
    internal class MultipleUserServiceSettingModel: ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MultipleUserServiceSettingModel));

        private ObservableCollection<WCFServiceNode> currentServicesInfo=null;
        public ObservableCollection<WCFServiceNode> CurrentServicesInfo
        {
            get
            {
                if (currentServicesInfo==null)
                {
                    InitWcfServicesInfo();
                }
                return currentServicesInfo;
            }
            set
            {
                currentServicesInfo = value;
                OnPropertyChanged("CurrentServicesInfo");
            }
        }

        private WCFServiceNode currentAccountInfo;
        public WCFServiceNode SelectedServiceInfo
        {
            get
            {
                if (currentAccountInfo == null)
                    currentAccountInfo = new WCFServiceNode();
                return currentAccountInfo;
            }
            set
            {
                currentAccountInfo = value;
                OnPropertyChanged("SelectedServiceInfo"); 
            }
        }
        private void InitWcfServicesInfo()
        {
            ///read data from configuration file
            /// 
            bool isOk = false;
            string strError = "";
            WCFXmlCfgMgr objXmlWCF = WCFXmlCfgMgr.LoadFromFile(ref isOk, ref strError);
            if ((!isOk)||(objXmlWCF==null))
            {
                Logger.Error("InitWcfServicesInfo", strError+" Or no Object returns");
                return;
            }
            CurrentServicesInfo = objXmlWCF.WCFNodes;
        }

        public ICommand DeleteCurrentNodeInfo
        {
            get
            {
                return new DelegateCommand(()=> {
                    if (currentAccountInfo == null)
                    {
                        HintByMessageBox("Please Select One Item before click Delete Button.", "Hint");
                        return;
                    }
                    string strHint = string.Format("account:[{0}] with port:[{1}]\r\n is Deleted Sucessfully.", currentAccountInfo.AccountName, currentAccountInfo.ServerPort);
                    currentServicesInfo.Remove(currentAccountInfo);                    
                    HintByMessageBox(strHint);
                    SelectedServiceInfo = null;
                    return;                                       
                });
            }
        }

        public ICommand CreateNodeInfo
        {
            get
            {
                return new DelegateCommand(()=> {
                    //bool isEmptyObjExists = false;
                    if (CurrentServicesInfo == null)
                    {
                        currentServicesInfo = new ObservableCollection<WCFServiceNode>();
                    }
                    for (int i=0;i< CurrentServicesInfo.Count;i++)
                    {
                        if (CurrentServicesInfo[i].IsEmpty())
                        {
                            SelectedServiceInfo = CurrentServicesInfo[i];
                            return;
                        }
                    }
                    
                    CurrentServicesInfo.Add(new WCFServiceNode() {
                        AccountName = SelectedServiceInfo.AccountName, 
                        ServerPort = SelectedServiceInfo.ServerPort,
                        MonitorPort = SelectedServiceInfo.MonitorPort
                    });
                    //SelectedServiceInfo = currentServicesInfo[currentServicesInfo.Count-1];
                });
            }
        }

        public ICommand SaveNodesInfo
        {
            get
            {
                return new DelegateCommand(()=> {
                    
                    for (int i = 0; i < CurrentServicesInfo.Count; i++)
                    {
                        if (CurrentServicesInfo[i].IsEmpty())
                        {
                            SelectedServiceInfo = CurrentServicesInfo[i];
                            HintByMessageBox("An empty record exists, please fill or delete it first","Warrning");
                            return;
                        }
                    }
                    WCFXmlCfgMgr.WCFInst.WCFNodes = currentServicesInfo;
                    WCFXmlCfgMgr.SaveToXmlFile(WCFXmlCfgMgr.WCFInst);
                    //AppConfigReader.SaveWcfNodeInfo(CurrentServicesInfo);
                    HintByMessageBox(string.Format("[{0}] Records Saved to Configuration files. ", CurrentServicesInfo.Count));
                });
            }
        }
    }

    
}
