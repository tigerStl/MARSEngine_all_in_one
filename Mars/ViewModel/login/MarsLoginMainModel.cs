using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;
using Mars.Securities;
using Mars.Utility;
using Mars.ViewModel.BaseData;
using Mars.Views.login;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.SourceCode.xmlConfig;
//using Microsoft.Practices.Prism.Commands;
using Route2NSEx.src.Marquis.systemUtil;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace Mars.ViewModel.login
{
    
    internal class MarsLoginMainModel: ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsLoginMainModel)); 

        public ICommand LoginCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand SetDatabasePasswordCommand { get; private set; }
        public string UserName { get; set; }
        private bool? isUserLogin;
        public bool? IsUserLogin { get { return isUserLogin; }
            set
            {
                isUserLogin = value;
                RaisePropertyChanged("IsUserLogin");
            }
        }

        private ObservableCollection<string> databaseConnectionItems = null;
        public ObservableCollection<string> DatabaseConnectionItems
        {
            get => databaseConnectionItems;
            set
            {
                if (databaseConnectionItems != value)
                {
                    databaseConnectionItems = value;
                    RaisePropertyChanged("DatabaseConnectionItems");
                }
            }
            
        }

        private string currentDatabaseConnectionItems;
        public string CurrentDatabaseConnectionItems
        {
            get => currentDatabaseConnectionItems;
            set
            {
                if (currentDatabaseConnectionItems!=value)
                {
                    currentDatabaseConnectionItems = value;
                    RaisePropertyChanged("CurrentDatabaseConnectionItems");
                    //修改
                    var itm = MarsEntitiesExtends.CachedConnectionStrings.Where(p => (p.Key.Equals(value, StringComparison.OrdinalIgnoreCase))).FirstOrDefault();
                    if (!itm.Equals(default(KeyValuePair<string, MarsDBCnnectionInfo>)))
                    {                       
                        DatabaseServerInfo = itm.Value.connectionStringFromCfg;
                    }
                    MarsMainWindow.CurrentDatabaseIdx = value;
                }
            }
        }

        private string databaseServerInfo;
        public string DatabaseServerInfo
        {
            get => databaseServerInfo;
            set
            {
                if (databaseServerInfo != value)
                {
                    databaseServerInfo = value;
                    RaisePropertyChanged("DatabaseServerInfo");
                }
            }
            //{
                
            //    //Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder oCnn = new Oracle.ManagedDataAccess.Client.OracleConnectionStringBuilder(MarsEntitiesExtends.connectionBuilder.ProviderConnectionString);
            //    //return string.Format("[{0}/{1}]",oCnn.DataSource, oCnn.UserID);
            //}
           
        }
        internal T_TESTER_INFODTO CurrentTesterInfo { get; private set; }
        public MarsLoginMainModel()
        {
            LoginCommand = new DelegateCommand<object>(loginButtonClick);
            CancelCommand = new DelegateCommand<object>(cancelButtonClick);
            SetDatabasePasswordCommand = new DelegateCommand<object>(setDatabasePwdButtonClick);

            InitDatabaseConnections();
        }

        private void InitDatabaseConnections()
        {
            databaseConnectionItems = new ObservableCollection<string>();
            MarsEntitiesExtends.InitDBInfo(AppConfigReader.GetAppsettings(),
                    AppConfigReader.GetConfigurationInstance().ConnectionStrings.ConnectionStrings);
            foreach(var itm in MarsEntitiesExtends.CachedConnectionStrings)
            {
                if (itm.Equals(default(KeyValuePair<string, MarsDBCnnectionInfo>))) continue;
                databaseConnectionItems.Add(itm.Key);
            }
        }
        

        private void cancelButtonClick(object objPwdTxtBox)
        {
            //App.Current.Shutdown();
            if (objPwdTxtBox == null) return;
            if (!(objPwdTxtBox is MarsLoginMain)) return;
            ((MarsLoginMain)objPwdTxtBox).Close();
        }

        private void setDatabasePwdButtonClick(object objFrm)
        {
            if (objFrm == null) return;
            if (!(objFrm is MarsLoginMain)) return;
            PasswordBox objPwd = (objFrm as MarsLoginMain).dbPwd,
                objRetypePwd = (objFrm as MarsLoginMain).dbRetypePwd;
            System.Security.SecureString secstrPwd = objPwd.SecurePassword,
                secstrRePwd = objRetypePwd.SecurePassword;

            string strPwdEncoded = ConvertToUnsecureString(secstrPwd),
                strretyepPwdEncoded = ConvertToUnsecureString(secstrRePwd); 
            if (string.Compare(strPwdEncoded, strretyepPwdEncoded)!=0)
            {
                objPwd.Focus();
                ViewModelBase.HintByMessageBox("Passwords don't match. Please retype.");
                return;
            }

            ///加密，写到配置文件中
            /// 
            
            string strEncoded = Mars.Securities.MarsEncodePwd.EncodeString(strPwdEncoded);
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD]==null)
            {
                config.AppSettings.Settings.Add(MarsConstants.CNST_DATABASE_PASSWORD, "N/A");
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("AppSettings");
            }
                config.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD].Value = strEncoded;            
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("AppSettings");

            HintByMessageBox("Database password changed.");
        }

        private void loginButtonClick(object objPwdTxtBox)
        {
            WCFXmlCfgMgr.CurrentLoginUser = null;

            if (objPwdTxtBox == null) return ;
            if (!(objPwdTxtBox is MarsLoginMain)) return ;

            MarsMainWindow.getDatabasePwd();

            PasswordBox objPwd = (objPwdTxtBox as MarsLoginMain).pwdInfo;
            System.Security.SecureString secureString = objPwd.SecurePassword;
            string pwdEncoded = ConvertToUnsecureString(secureString);

            Logger.Info("loginButtonClick", string.Format("Encoded:[{0}]",pwdEncoded));
            /// check user name and Tripled code exists in database
            /// 
            if (string.IsNullOrEmpty(UserName)||string.IsNullOrEmpty(pwdEncoded))
            {
                HintByMessageBox("Please input User Name and Password.", "Warning");
                return ;
            }

            string pwdAfterDes = MarsEncodePwd.EncodeString(pwdEncoded);
            string strError = "";
            try
            {
                if ((CurrentTesterInfo = BoHelper.VerifyUserLogin(UserName, pwdAfterDes,CurrentDatabaseConnectionItems,  ref strError)) == null)
                {
                    HintByMessageBox("Can't Login, please check User Name and password.\r\n" + strError, "Hint");
                    Logger.Error("loginButtonClick", string.Format("Error from DB layer:[{0}]", strError));
                    return;
                }

                //test
                //bool isOk = false;
                //B_PROJ_TS_TC_FULLVISION.GetTSTCByNamePair(new List<KeyValuePair<string, string>> {
                //    new KeyValuePair<string, string> ("OPICS FOREX", "Opics Launch Trade from QMON"),
                //    new KeyValuePair<string, string> ("OPICS MISC", "Launch Module Of Opics")
                //}, ref isOk, ref strError);
            }
            catch (Exception e)
            {

                Logger.Error("loginButtonClick", strError = string.Format("Exception:[{0}]\r\nStackTrace:[{1}] ",e.Message,e.StackTrace),e);
                if (e.InnerException!=null)
                {
                    Logger.Error("loginButtonClick",strError+=string.Format("\r\nInnerException:[{0}]",e.InnerException.Message));
                }
                HintByMessageBox(strError);
                return;
            }

            ///try to make sure no other mars with same account is running 
            ///
            bool isAnotherMarsWithSameAccountRunning = CheckAnotherMarsWithSameAccountRunning(UserName);
            if (isAnotherMarsWithSameAccountRunning)
            {
                HintByMessageBox(string.Format("Account [{0}] is already login, Account can only login once per time.",UserName));
                return;
            }

            IsUserLogin = true;

#if _NO_C_DRIVER_WRITE
            //获得shellDir的设置
            string strShellDir = AppConfigReader.GetShellDir();
            if (string.IsNullOrEmpty(strShellDir))
            {
                HintByMessageBox("Please set ShellDir in MarsTestFrame.dll.config First and restart.Press Ok then Quit Mars.");
                System.Windows.Application.Current.Shutdown();

            }
            if (!Directory.Exists(strShellDir))
            {
                try
                {
                    Directory.CreateDirectory(strShellDir);
                }
                catch (Exception e)
                {
                    HintByMessageBox(string.Format(e.Message));
                    System.Windows.Application.Current.Shutdown();
                }
            }
            
            Environment.SetEnvironmentVariable("_MARS_SHELL_DIR_", strShellDir,EnvironmentVariableTarget.User);
#endif
            WCFXmlCfgMgr.CurrentLoginUser = UserName;
            //CloseWindow(true);
            (objPwdTxtBox as MarsLoginMain).DialogResult = true;
            (objPwdTxtBox as MarsLoginMain).Close();

            //启动qtp

            return ;
        }

        private bool CheckAnotherMarsWithSameAccountRunning(string strUserName)
        {
            Process p = Process.GetCurrentProcess();
            string strExe = p.MainModule.FileName;
            Logger.Info("CheckAnotherMarsWithSameAccountRunning", string.Format("strUserName-[{0}]", strUserName));
            strExe = System.IO.Path.GetFileNameWithoutExtension(strExe);
            Process[] arrP = Process.GetProcessesByName(strExe);
            if (arrP == null) return false;
            if (arrP.Length <= 0) return false;
            foreach(var itm in arrP)
            {
                if (itm.Id == p.Id) continue;
                Logger.Info("CheckAnotherMarsWithSameAccountRunning", string.Format("MainWindowTitle-[{0}]", itm.MainWindowTitle));
                if (itm.MainWindowTitle.StartsWith(strUserName + ":"))
                {
                    return true;
                }
            }
            return false;
        }

        private string ConvertToUnsecureString(System.Security.SecureString securePassword)
        {
            return MarsUtilities.ConvertToUnsecureString(securePassword);
        }
    }
}
