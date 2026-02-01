using com.Mars.Config;
using Mars.Business;
using Mars.DataLayer;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.systemUtil;
//using Microsoft.Practices.Prism.Commands;
using Microsoft.Win32;
using Prism.Commands;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Input;
using System.Windows.Threading;

namespace Mars.ViewModel
{
    internal class TestApplicationRegistrationViewModel : ViewModelBase
    {
        #region Constructor And static property
        private static MLogger Logger = MLogger.GetLogger(typeof(TestApplicationRegistrationViewModel));
        public const string CNST_CMD_SAVE = "Save";
        public const string CNST_CMD_DELETE = "Delete Local Setting";
        public const string CNST_CMD_ADDTOLOCAL = "Add To Local";
        public const string CNST_CMD_ADDTOFRAMEWORK = "Add To Framework";
        public const string CNST_CMD_CLEAR_LOCAL = "Clear Local Setting";
        public const string CNST_CMD_DELETE_SERVER = "Delete From System";

        public TestApplicationRegistrationViewModel()
        {
            Logger.Info("TestApplicationRegistrationViewModel","begin");
            cmdSelectTargetApplication = new DelegateCommand(() => { SelectTargetApplicationImpl(); });
            modelButtonCmd = new DelegateCommand<object>(this.clickButtons);
            MergeApplicationsInfo();
        }
        #endregion //Constructor And static property
        
        #region properties
        ObservableCollection<B_REGISTERED_APPS> RegAppsFromDB = null;
        ConfigTestApplicationCollection LocalConfiguredApps = null;
        private ObservableCollection<RegistedTestApplication> registeredApplications = null;
        public ObservableCollection<RegistedTestApplication> RegisteredApplications
        {
            get { return registeredApplications; }
            set
            {
                registeredApplications = value;
                RaisePropertyChanged("RegisteredApplications");
            }
        }
        private RegistedTestApplication selectedApplication;
        public RegistedTestApplication SelectedApplication
        {
            get { return selectedApplication; }
            set
            {
                selectedApplication = value;
                RaisePropertyChanged("SelectedApplication");
            }
        }

        private ICommand cmdSelectTargetApplication;
        public ICommand CmdSelectTargetApplication
        {
            get { return cmdSelectTargetApplication; }
        }

        private DelegateCommand<object> modelButtonCmd;
        public DelegateCommand<object> ModelButtonCmd
        {
            get { return modelButtonCmd; }
        }
        #endregion

        #region Methods

        private void MergeApplicationsInfo()
        {
            ///步骤,
            /// 1，获得Application from数据库
            /// 2，获得application from配置文件
            /// 3，设置是否配置的状态
            /// 
            GetApplicationsFromDB();
            GetApplicationsFromConfiguration();
            SetApplicationsStatus();
        }

        private void clickButtons(object bttn)
        {
            Logger.Info("clickButtons",string.Format("button:[{0}]",bttn));

            string strError = "";
            if (!(bttn is System.Windows.Controls.Button))
            {
                Logger.Error("clickButtons",strError=string.Format("Object is not a button:[{0}]", bttn==null?"null":bttn.GetType().ToString()));
                Dispatcher.CurrentDispatcher.Invoke(() => {
                    System.Windows.Forms.MessageBox.Show(strError, "Error");
                });
                return;
            }
            System.Windows.Controls.Button objBtn = (System.Windows.Controls.Button)bttn;
            bool isOk = false;
            switch(objBtn.Content.ToString())
            {
                case CNST_CMD_SAVE:
                    isOk = SaveCurrentAppRegSettingImpl( ref strError);
                    break;
                case CNST_CMD_DELETE:
                    isOk = DeleteCurrentAppRegSettingImpl(ref strError);
                    break;
                case CNST_CMD_ADDTOLOCAL:
                    isOk = AddCurrentAppregToLocalImp(ref strError);
                    break;
                case CNST_CMD_CLEAR_LOCAL:
                    isOk = CleanAllItemsFromLocal(ref strError);
                    break;
                case CNST_CMD_DELETE_SERVER:
                    isOk = DeleteApplicationFromSystem(ref strError);
                    return;
                default:
                    return;
            }
            RaisePropertyChanged("RegisteredApplications");
            this.selectedApplication.RaisePropertyChanged("StarterPath");
        }

        private bool CleanAllItemsFromLocal(ref string strError)
        {
            Logger.Info("CleanAllItemsFromLocal", "Begin");
            if (this.selectedApplication == null)
            {
                Logger.Error("CleanAllItemsFromLocal", strError = "No application is selected.");
                HintByMessageBox(strError, "Error");
                return false;
            }
            bool isOk = AppConfigReader.ClearAllAppSettings(ref strError);
            if (isOk)
            {
                HintByMessageBox("Cleaned all Items from Configuration file", "Hint");
            }else
                HintByMessageBox(string.Format("Error when clean items from DB:[{0}]", strError), "Error");
            return false;
        }

        private bool DeleteApplicationFromSystem(ref string strError)
        {
            Logger.Info("DeleteApplicationFromSystem", "Try to Delete");
            /// 1, Check whether an application is selected
            if (selectedApplication==null)
            {
                HintByMessageBox("Please select an application First.", "Mars Hint");
                return false;
            }

            /// 2, Warnning user about deleting application

            if (!QuestionByMessageBox(string.Format("Application infomation is important.\r\nAre you sure deleting this application?\r\nApplication:{0}", selectedApplication.ApplicationShortName), "MARS Warning"))
                return true;
            /// 
            /// 3, delete object information
            /// 
            if (selectedApplication.AssignedDBApplication == null)
            {
                Logger.Warnning("DeleteApplicationFromSystem", strError = string.Format("No Database information for Selected Application:[{0}]", selectedApplication.ApplicationShortName));
                HintByMessageBox(strError, "Warning");
            }
            if (!Mars.DataLayer.BoHelper.DeleteApplicationById(MarsMainWindow.CurrentDatabaseIdx, selectedApplication.AssignedDBApplication.APPLICATION_ID, ref strError))
            {
                string strHint = "";
                Logger.Error("DeleteApplicationFromSystem", strHint= string.Format("Can't Delete Application:[{0}], with Error Info:\r\n{1}",selectedApplication.ApplicationShortName, strError));
                HintByMessageBox(strHint,"Hint");
                return false;
            }
            else
            {
                //update cache
                HintByMessageBox(string.Format("Application:[{0}] is deleted sucessfully.", selectedApplication.ApplicationShortName), "Hint");
                if (!MarsDBGlobe_Cache.updateApplicationCache(MarsMainWindow.CurrentDatabaseIdx, ref strError))
                {
                    HintByMessageBox(string.Format("Can't update System cache, MARS is required to restart.\r\nerror:[{0}]", strError), "WARNING");
                    return false;
                }
                registeredApplications.Remove(selectedApplication);
                RaisePropertyChanged("RegisteredApplications");
                return true;
            }
            
        }

        private bool SaveCurrentAppRegSettingImpl(ref string strError)
        {
            Logger.Info("SaveCurrentAppRegSettingImpl","begin");
            /// Steps: 
            /// 1 save to database 
            /// 2 if 1 is ok then save to config file
            /// 
            bool isOk = false;
            if (this.selectedApplication==null)
            {
                Logger.Error("SaveCurrentAppRegSettingImpl",strError = "No application is selected.");
                HintByMessageBox(strError, "Error");
                return false;
            }
            /// save to database
            /// 
            if (this.selectedApplication.AssignedDBApplication==null ||
                this.selectedApplication.AssignedDBApplication.APPLICATION_ID <= 0)
            {
                Logger.Error("SaveCurrentAppRegSettingImpl", strError ="Can't save, use add to Framework first.");
                HintByMessageBox(strError, "Error");
                return false;
            }
            //this.selectedApplication.AssignedDBApplication.
            isOk=this.selectedApplication.AssignedDBApplication.Update(ref strError, MarsMainWindow.CurrentDatabaseIdx);
            if(!isOk)
            {
                Logger.Error("SaveCurrentAppRegSettingImpl",strError = string.Format("Error from DB model:[{0}]",strError));
                HintByMessageBox(strError, "Error");
            }
            else
            {
                HintByMessageBox("Saved Data to DB.","Hint");
            }
            return isOk;
        }
        private bool DeleteCurrentAppRegSettingImpl(ref string strError)
        {
            Logger.logBegin("DeleteCurrentAppRegSettingImpl");
            bool isOk = true;
            if (this.selectedApplication == null)
            {
                Logger.Error("DeleteCurrentAppRegSettingImpl", strError = "No application is selected.");
                HintByMessageBox(strError, "Error");
                return false;
            }

            if (isOk)
            {
                /// save to configuration file
                /// 
                //this.selectedApplication.AssignedConfigurationApplication
                isOk = AppConfigReader.SaveConfiguration(this.selectedApplication.AssignedConfigurationApplication, ref strError,true);
            }
            if (!isOk)
            {
                Logger.Error("AddCurrentAppregToLocalImp", strError = string.Format("Error from DB model:[{0}]", strError));
                HintByMessageBox(strError, "Error");
            }
            else
            {
                HintByMessageBox("Saved to Configuration File.", "Hint");
            }
            return isOk;
        }
        private bool AddCurrentAppregToLocalImp(ref string strError)
        {
            bool isOk = true;
            if (this.selectedApplication == null)
            {
                Logger.Error("AddCurrentAppregToLocalImp", strError = "No application is selected.");
                HintByMessageBox(strError, "Error");
                return false;
            }
            if (isOk)
            {
                /// save to configuration file
                /// 
                //this.selectedApplication.AssignedConfigurationApplication
                isOk = AppConfigReader.SaveConfiguration(this.selectedApplication.AssignedConfigurationApplication, ref strError);
#if _NOQTP
                //isOk = AppConfigReader.SaveConfigurationNoQTP(this.selectedApplication.AssignedConfigurationApplication, ref strError);
#endif
            }
            if (!isOk)
            {
                Logger.Error("AddCurrentAppregToLocalImp", strError = string.Format("Error from DB model:[{0}]", strError));
                HintByMessageBox(strError, "Error");
            }
            else {
                HintByMessageBox("Saved to Configuration File.", "Hint");
            }
            return isOk;
        }
        public bool SelectTargetApplicationImpl()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Executable Files (*.exe)|*.exe|*.Application|*.*";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.Multiselect = false;
            
            if (openFileDialog1.ShowDialog() == true)
            {
                this.selectedApplication.StarterPath = openFileDialog1.FileName;
            }
            return true;
        }
        
        private void GetApplicationsFromDB()
        {
            RegAppsFromDB = B_REGISTERED_APPS.GetCacheApps(MarsMainWindow.CurrentDatabaseIdx);
        }
        private void GetApplicationsFromConfiguration()
        {
            LocalConfiguredApps = AppConfigReader.GetRegApplications();
        }

        private void SetApplicationsStatus()
        {
            List<RegistedTestApplication> lstTestApplicationInfo = new List<RegistedTestApplication>();
            if (LocalConfiguredApps == null)
                LocalConfiguredApps = new ConfigTestApplicationCollection();
            foreach (B_REGISTERED_APPS objAppsFromDB  in RegAppsFromDB)
            {
                RegistedTestApplication objRsltItm = new RegistedTestApplication();
                if (objRsltItm == null) continue;
                objRsltItm.AssignedDBApplication = objAppsFromDB;

                ConfigTestApplication tmpObj = LocalConfiguredApps.GetSingle(objAppsFromDB.APP_SHORT_NAME);

                if (tmpObj == null)
                {
                    /// create a default one 
                    /// 
                    tmpObj = new ConfigTestApplication();
                    tmpObj.AppName = objAppsFromDB.APP_SHORT_NAME;
                    objRsltItm.IsConfiguredOnLocal = false;
                }
                else
                    objRsltItm.IsConfiguredOnLocal = true;
                objRsltItm.AssignedConfigurationApplication = tmpObj;
                objRsltItm.CheckApplicationStatus();

                CloneApplicationInfoFromDB2LocalObj(objAppsFromDB, tmpObj);

                lstTestApplicationInfo.Add(objRsltItm);
            }
            registeredApplications = new ObservableCollection<RegistedTestApplication>( lstTestApplicationInfo.OrderBy(p => p.AssignedDBApplication.APP_SHORT_NAME));
        }

        private void CloneApplicationInfoFromDB2LocalObj(B_REGISTERED_APPS objSrc, ConfigTestApplication objDes)
        {
            if (objSrc == null || objDes == null) return;
            bool bOk = false;
            string strError = "";
            objDes.AppliationType = objSrc.GetRegApplicationType(objSrc.APPLICATION_TYPE_ID??-1, ref bOk, ref strError);
            if (!bOk)
            {
                Logger.Error("CloneApplicationInfoFromDB2LocalObj",string.Format("Can't find object:[{0}],Error:[{1}]",objSrc.APPLICATION_TYPE_ID,strError));
                objDes.AppliationType = "Window";
            }
            objDes.path =string.IsNullOrEmpty(objDes.path)? objSrc.STARTER_PATH: objDes.path;
            try
            {
                objDes.identifier = Path.GetFileName(objDes.path);
            }
            catch (Exception e)
            {
                objDes.identifier = "";
                Logger.Error("CloneApplicationInfoFromDB2LocalObj",string.Format("Exception:[{0}] when set identifier",e.Message),e);
            }
            objDes.ObjectPath = "";
            List<string> lstRequirement = new List<string>();
            if (!objSrc.RecorrectRequirement(MarsMainWindow.CurrentDatabaseIdx, lstRequirement,ref strError))
            {
                objDes.ExtraRequirement = string.IsNullOrEmpty(objDes.ExtraRequirement)?"": objDes.ExtraRequirement;
            }
            else
            {
                objDes.ExtraRequirement = lstRequirement == null ? objDes.ExtraRequirement : lstRequirement.Count==0? objDes.ExtraRequirement : string.Join(";",lstRequirement.ToArray());
            }
            
        }

        #endregion //Methods

    }

    public class RegistedTestApplication: INotifyPropertyChanged
    {
        public const string CNST_NOT_CONFIGURED = "N/A For Local";
        public const string CNST_RIGHT = "OK";
        public const string CNST_PATH_NOT_CHECKABLE = "Path N/A";
        public const string CNST_PATH_WRONG = "Path Wrong";
        public B_REGISTERED_APPS AssignedDBApplication;
        public ConfigTestApplication AssignedConfigurationApplication;

        public bool IsConfiguredOnLocal;
        private int ApplicationLocalStatusId;
        

        public string ApplicationIdentifier
        {
            get
            {
                if (AssignedConfigurationApplication == null) return "";
                return AssignedConfigurationApplication.identifier;
            }
            set
            {
                if (AssignedConfigurationApplication == null)
                {
                    AssignedConfigurationApplication = new ConfigTestApplication();
                }
                if (AssignedConfigurationApplication.identifier != value)
                {
                    //need save to configuration file
                    AssignedConfigurationApplication.identifier = value;
                    RaisePropertyChanged("ApplicationIdentifier");
                    //save to file
                }
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        #endregion //INotifyPropertyChanged

        ///0 not configured, 1, configured with available path, 2, path can't check --for http or startwith \\ 3, wrong path
        public string Status
        {
            get
            {
                switch(ApplicationLocalStatusId)
                {
                    case 0:return CNST_NOT_CONFIGURED;
                    case 1:return CNST_RIGHT;
                    case 2:return CNST_PATH_NOT_CHECKABLE;
                    case 3:return CNST_PATH_WRONG;
                    default: return CNST_NOT_CONFIGURED;
                }
            } 
        }

        public string ApplicationShortName
        {
            get {
                return AssignedConfigurationApplication == null ? "" : AssignedConfigurationApplication.AppName;
            }
            set {
                if (AssignedConfigurationApplication != null)
                {
                    AssignedConfigurationApplication.AppName = value;
                }
                if (AssignedDBApplication!=null)
                {
                    AssignedDBApplication.APP_SHORT_NAME = value;                    
                }
                RaisePropertyChanged("ApplicationShortName");
            }
        }

        public string StarterPath
        {
            get {
                return AssignedConfigurationApplication == null ? "" : AssignedConfigurationApplication.path;
            }
            set {
                if (AssignedConfigurationApplication != null)
                {
                    AssignedConfigurationApplication.path = value;
                    ///check path then save
                    /// 
                    CheckApplicationStatus();
                    if ((ApplicationLocalStatusId == 2) || (ApplicationLocalStatusId == 1))
                    {
                        //Write to xml file 
                    }
                    RaisePropertyChanged("StarterPath");
                    RaisePropertyChanged("Status");
                    if (string.IsNullOrEmpty(this.ApplicationIdentifier)&&(!string.IsNullOrEmpty(value)))
                    {
                        this.ApplicationIdentifier = Path.GetFileName(value);
                    }
                }                
            }
        }
        /***
        private object infragisticsSupportObj;
        public object InfragisticSupportObj
        {
            get { return infragisticsSupportObj; }
            set { 
                    infragisticsSupportObj = value;
                RaisePropertyChanged("InfragisticSupportObj");
            }
        }
        ***/

        public KeyValuePair<string,string> DevExpressSupport
        {
            get {
                string strTmpAddins = AssignedConfigurationApplication == null ? "" : AssignedConfigurationApplication.ExtraRequirement;
                string[] arrAddins = strTmpAddins.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (arrAddins.Length==0)
                {
                    return B_REGISTERED_APPS.APPLICATION_ADDINS_DEVEXP.ElementAt(0);
                }
                foreach (string strItm in arrAddins)
                {
                    if (strItm == null) continue;
                    if (TigerMarsUtil.RegularTest("^DEVEXPRESS", strItm))
                    {
                        return B_REGISTERED_APPS.APPLICATION_ADDINS_DEVEXP.FirstOrDefault(p => string.Compare(p.Key, strItm, true) == 0);
                        //return strItm;
                    }
                }
                return default(KeyValuePair<string, string>);
            }
            set {
                if (EqualityComparer<KeyValuePair<string, string>>.Default.Equals(value, default(KeyValuePair<string, string>))) return;
                KeyValuePair<string, string> objTmp = (KeyValuePair<string, string>)value;
                bool isRemove = TigerMarsUtil.RegularTest("None", objTmp.Value);
                
                string strTmpAddins = AssignedConfigurationApplication == null ? "" : AssignedConfigurationApplication.ExtraRequirement;
                string[] arrAddins = strTmpAddins.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                List<string> lstTmpAddins = new List<string>(arrAddins);
                foreach (string strItm in arrAddins)
                {
                    if (strItm == null) continue;
                    if (TigerMarsUtil.RegularTest("^DEVEXPRESS", strItm))
                    {
                        lstTmpAddins.Remove(strItm);
                        //AssignedConfigurationApplication.ExtraRequirement.Replace(strItm, "");
                    }
                }
                if (!isRemove) 
                    lstTmpAddins.Add(objTmp.Key);
                AssignedConfigurationApplication.ExtraRequirement = string.Join(";", lstTmpAddins.ToArray());
                RequirementsForAddins = AssignedConfigurationApplication.ExtraRequirement;
                //AppConfigReader.SaveConfiguration();
                if (AssignedDBApplication != null)
                    AssignedDBApplication.EXTRAREQUIREMENT = AssignedConfigurationApplication.ExtraRequirement;
                RaisePropertyChanged("DevExpressSupport");
            }
        }
        public KeyValuePair<string, string> InfragisticSupport
        {
            get
            {
                string strTmpAddins = AssignedConfigurationApplication == null ? "" : AssignedConfigurationApplication.ExtraRequirement;
                string[] arrAddins = strTmpAddins.Split(new string[] { ";"},StringSplitOptions.RemoveEmptyEntries);
                if (arrAddins.Length == 0)
                {
                    return B_REGISTERED_APPS.APPLICATION_ADDINS_INFRAGISTICS.ElementAt(0);
                }
                foreach (string strItm in arrAddins)
                {
                    if (strItm == null) continue;
                    if (TigerMarsUtil.RegularTest("^INFRAGISTICS", strItm)) {
                        return B_REGISTERED_APPS.APPLICATION_ADDINS_INFRAGISTICS.FirstOrDefault(p=>string.Compare(p.Key,strItm,true)==0);
                        //return strItm;
                    }
                }
                return default(KeyValuePair<string, string>);
            }
            set
            {
                if (EqualityComparer<KeyValuePair<string, string>>.Default.Equals(value, default(KeyValuePair<string, string>))) return;
                KeyValuePair<string, string> objTmp = (KeyValuePair<string, string>)value;
                bool isRemove = TigerMarsUtil.RegularTest("None", objTmp.Value);

                string strTmpAddins = AssignedConfigurationApplication == null ? "" : AssignedConfigurationApplication.ExtraRequirement;
                string[] arrAddins = strTmpAddins.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                List<string> lstTmpAddins = new List<string>(arrAddins);
                foreach (string strItm in arrAddins)
                {
                    if (strItm == null) continue;
                    if (TigerMarsUtil.RegularTest("^INFRAGISTICS", strItm))
                    {
                        lstTmpAddins.Remove(strItm);
                        //AssignedConfigurationApplication.ExtraRequirement.Replace(strItm, "");
                    }
                }
                if (!isRemove)
                    lstTmpAddins.Add(objTmp.Key);
                AssignedConfigurationApplication.ExtraRequirement = string.Join(";", lstTmpAddins.ToArray());
                RequirementsForAddins = AssignedConfigurationApplication.ExtraRequirement;
                //AppConfigReader.SaveConfiguration();
                if (AssignedDBApplication != null)
                    AssignedDBApplication.EXTRAREQUIREMENT = AssignedConfigurationApplication.ExtraRequirement;
                RaisePropertyChanged("InfragisticSupport");
            }
        }

        public KeyValuePair<int,string> ApplicationType
        {
            get {
                //return AssignedConfigurationApplication == null ? "" : AssignedConfigurationApplication.AppliationType;
                if (AssignedDBApplication == null) return default(KeyValuePair<int, string>);
                return B_REGISTERED_APPS.GetApplicationTypeById(AssignedDBApplication.APPLICATION_TYPE_ID??-1);
            }
            set {

                if (AssignedConfigurationApplication != null)
                {
                    AssignedConfigurationApplication.AppliationType = value.Value;
                    ///Write back to xml file
                    /// 
                    AssignedConfigurationApplication.Update();
                }
                if (AssignedDBApplication!=null)
                {
                    AssignedDBApplication.APPLICATION_TYPE_ID = (short)value.Key;
                }

                RaisePropertyChanged("ApplicationType");
            }
        }

        public string RequirementsForAddins
        {
            get { return AssignedConfigurationApplication == null ? "" : AssignedConfigurationApplication.ExtraRequirement; }
            set {
                AssignedConfigurationApplication.ExtraRequirement = value;
                AssignedConfigurationApplication.Update();
                RaisePropertyChanged("RequirementsForAddins");
            }
        }

        internal void CheckApplicationStatus()
        {
            if (AssignedConfigurationApplication==null)
            {
                ApplicationLocalStatusId = 0;
                return;
            }
            if (!this.IsConfiguredOnLocal)
            {
                ApplicationLocalStatusId = 0;
                return;
            }
            if (string.IsNullOrEmpty(AssignedConfigurationApplication.path))
            {
                ApplicationLocalStatusId = 3;
                return;
            }
            if (AssignedConfigurationApplication.path.Trim().StartsWith(@"\\")||(AssignedConfigurationApplication.path.Trim().ToUpper().StartsWith(@"HTTP:\\")))
            {
                ApplicationLocalStatusId = 2;
                return;
            }
            if(File.Exists(AssignedConfigurationApplication.path))
            {
                ApplicationLocalStatusId = 1;
                return;
            }
        }
    }
}
