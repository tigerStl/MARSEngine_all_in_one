using System;
using System.Collections.Generic;
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
using System.Configuration;

using System.Reflection;

using MarsTestFrame.SourceCode.systemUtil;

using System.Collections;

using System.Text.RegularExpressions;
using com.Mars.MarsTestingFrame;
using MarsTestFrame.SourceCode.com.Mars.KeyWords;
using MarsTestFrame.SourceCode.com.Mars.QTP;
using System.Data.OleDb;
using System.Data;
using System.IO;
using MarsFrameWork.TestProjects;
using System.Windows.Threading;
using com.Mars.Config;
using com.Mars.TestFrame.Application;
using MarsTestFrame.systemUtil;
using MarsTestFrame.SourceCode.com.Mars.Excels;

namespace MarsFrameWork
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public MainWindow()
        {
            InitializeComponent();
            
            //this.Icon = TestFrameStarter.Properties.Resources.favicon.;
        }
        public MarsTestFrameMain objMarsTestingFrame = new MarsTestFrameMain();

        private void Button_Click(object sender, RoutedEventArgs e)
        {

#region license info
            bool isShowLicenseInfo = false;
            if (((App)Application.Current).gLicenseMgr==null)
            {
                isShowLicenseInfo = true;
            }
            else
            {
                isShowLicenseInfo = !((App)Application.Current).gLicenseMgr.isAvailable() ;
            }
            if (isShowLicenseInfo)
            {
                MessageBox.Show("License for Testing Framework has expired.\r\nPlease contact Marquis Business and Technology Solutions for License renewal. \r\nTel:212.899.5549\r\nEmail:mars@mbtsllc.com","WARNING") ;
                return ;
            }
#endregion //license Info

            /** kill all QTP instances **/
            string strQptRoot = AppConfigReader.GetQtpRoot();
            /*strQptRoot = strQptRoot == null ? "" : strQptRoot.Replace("\\", "\\\\");
            strQptRoot = strQptRoot == null ? "" : strQptRoot.Replace("(", "\\(");
            strQptRoot = strQptRoot == null ? "" : strQptRoot.Replace(")", "\\)");*/
            TigerMarsUtil.KillProcessBelong2TargetFold(strQptRoot);

            /** switch QTP Testadvantage replacement information **/
            /** for administrative priviledge **/
            /* 
            if (SwitchSwfConfig() != 1)
            {
                return;
            } */
            /** for non-administrative priviledge **/
            if (SwitchAddinsVersion() != 1)
            {
            }

            //
            //objMarsTestingFrame = null;
            if (this.AvailableProjects.SelectedIndex < 0 )
            {
                this.Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(delegate() 
                        { 
                        MessageBox.Show("Please select one Test Project to Run!", "Hint"); 
                        }
                        )
                    );
                return;
            }
            objMarsTestingFrame.CurrentTestProjectName = this.AvailableProjects.SelectedItem.ToString();

            /// One test project assigns to applications. 
            /// check whether the test project can run on that application
            /// threee cases 1, run from configuration files 2, run from dtabase 3, run from IDE
            objMarsTestingFrame.CurrentTestApplicationShortName = this.AvailableApps.SelectedItem.ToString();
#if _Datafrom_Database
            if (!DashBoardFactory.IsDashBoardFromDB())
            { 
                string strCurrentTestTestProjectsFileName = TPManagement.GetCurrentTestProjectFileName();
                objMarsTestingFrame.RunTestBatchFileByThread(strCurrentTestTestProjectsFileName, ((App)Application.Current).currentStartMode);
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate () { this.WindowState = WindowState.Minimized; }));
            }
            else
            {
                /***  use project ID to start Test ***/
                if (!(this.AvailableProjects.SelectedItem is MarsKeyValues<string, string>))
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new Action(delegate () {
                            MessageBox.Show(string.Format("Can't find right object. Object require is :[MarsKeyValues],\r\nbut [{0}] returns", 
                                    this.AvailableProjects.SelectedItem.GetType()), "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);}));
                    return;// should log, 
                }
                /** call run test project by dashboard ID **/
                string strProjectID = ((MarsKeyValues<string, string>)this.AvailableProjects.SelectedItem).MKey;
                string strProjectName = ((MarsKeyValues<string, string>)this.AvailableProjects.SelectedItem).MValue;
                string strApplicationId = ((MarsKeyValues<string, string>)(((ConfigTestApplication)this.AvailableApps.SelectedItem)).Tag).MKey ;
                objMarsTestingFrame.CurrentTestProjectName = strProjectName;
                
                objMarsTestingFrame.CurrentTestApplicationShortName = ((MarsKeyValues<string, string>)(((ConfigTestApplication)this.AvailableApps.SelectedItem)).Tag).MValue;
                objMarsTestingFrame.RunTestBatchFileByThread(strProjectName, ((App)Application.Current).currentStartMode, strProjectID,strApplicationId);
                

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate () { this.WindowState = WindowState.Minimized; }));

            }
#else
            string strCurrentTestTestProjectsFileName = TPManagement.GetCurrentTestProjectFileName();
            objMarsTestingFrame.RunTestBatchFileByThread(strCurrentTestTestProjectsFileName, ((App)Application.Current).currentStartMode);
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate () { this.WindowState = WindowState.Minimized; }));
            
#endif
        }





        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"\d{1}");
            Match match = regex.Match("[StorageMode:Comparing;ColIndx:2;ConvertMethod:NONE];TRADE_PAY_INDEX");
            if (match.Success)
            {
                Console.WriteLine(match.Value);
            }
            string strReg = "";// TxtReg.Text;
            regex = new Regex(strReg);//new Regex(@"(>|<|=|>=|<=){1}(\d+)\?return\=(true|false):(\S+)\[(\S+)\]");
            match = regex.Match("RowCount>=0?return=false:clickButton[dd,d]");
            string[] arrSt = regex.Split("RowCount>=0?return=false:clickButton[dd,d]");
            if (match.Success)
            {
                Console.WriteLine(match.Value);
            }
        }

/*
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            string strFullPath = Assembly.GetExecutingAssembly().Location;
            string strPrntDir = Directory.GetParent(Directory.GetParent(strFullPath).FullName).FullName;
            MessageBox.Show(strPrntDir);
        }
        */
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /** Load List Table to Combox **/
#if _Datafrom_Database
            List<MarsKeyValues<string, string>> lstTestProjects = null;
#else
            List<string> lstTestProjects = null;
#endif
            try
            {
                lstTestProjects = TPManagement.GetTestProjects();
            }
            catch (Exception err)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate()
                {
                    MessageBox.Show(string.Format("Can't load Test project Files, Exceptions:[{0}]", err.Message));
                }));

                return;
            }

            AvailableProjects.Items.Clear();
            //AvailableProjects.text
            //AvailableProjects.ItemsSource = lstTestProjects ;
#if _Datafrom_Database
            AvailableProjects.ItemsSource = lstTestProjects;
            //AvailableProjects.SelectedItem = 
            //if (AvailableProjects.ItemsSource!=null)
            int iIdx = AvailableProjects.ItemsSource != null? lstTestProjects.IndexOf(new MarsKeyValues<string, string>(TPManagement.CNST_DEFAULT_TESTPROJECTNAME, TPManagement.CNST_DEFAULT_TESTPROJECTNAME)):-1;            
#else
            foreach (string strItem in lstTestProjects)
            {
                AvailableProjects.Items.Add(new ComboBoxItem());
                
            }
            int iIdx = AvailableProjects.Items.IndexOf(TPManagement.CNST_DEFAULT_TESTPROJECTNAME);
#endif

            AvailableProjects.SelectedIndex = iIdx >= 0 ? iIdx : 0;

            /** Load applications **/
            LoadRegisterdApps();

            /** Load CheckBox **/
            LoadCheckBoxInfo();
        }

        private void LoadCheckBoxInfo()
        {
            string strBase = AppConfigReader.GetBaseLineMode();
            if (string.IsNullOrEmpty(strBase))
            {
                this.BaseLineCheckBox.IsChecked = true;
                return;
            }

            this.BaseLineCheckBox.IsChecked = AppConfigReader.IsBaseLineMode();
        }

        private void LoadRegisterdApps()
        {
#if _Datafrom_Database
            if (DashBoardFactory.IsDashBoardFromDB()) { return; }
#endif
            //this.AvailableApps.Items.Clear();
            AvailableApps.Tag = null;

            ConfigTestApplicationCollection lstApps = AppConfigReader.GetRegApplications();
            AvailableApps.ItemsSource = lstApps;
            this.AvailableApps.DisplayMemberPath = "AppName";            
            string strDefaultName = AppConfigReader.GetDefaultApplicationNameEx();
            int iIdx = AvailableApps.Items.IndexOf(strDefaultName);
            AvailableApps.Tag = lstApps;
            AvailableApps.SelectedIndex = iIdx >= 0 ? iIdx : (AvailableApps.Items.Count>=0?0:-1);

        }

        private int SwitchAddinsVersion()
        {
            if (AvailableApps.Tag == null) return 1;
            /** **/
            int iIdx = AvailableApps.SelectedIndex;
            if (iIdx < 0) return 1;
            ICollection<ConfigTestApplication> lstApps = (ICollection<ConfigTestApplication>)AvailableApps.Tag;
            if (iIdx >= lstApps.Count) return 1;
            ConfigTestApplication objTestApp = lstApps.ElementAt(iIdx);
            string strErrorInfo = "";
            int iError = TargetApplicationsManagement.SwitchAddinsFiles(objTestApp.ExtraRequirement,ref strErrorInfo);
            if (iError != 1)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate()
                    {
                        MessageBox.Show(string.Format(@"Can't switch to sepecial Swfconfig file :[{0}], with ErrorInfo:[{1}]", objTestApp.ExtraRequirement, strErrorInfo));
                    }));
            }
            return iError;            
        }

        private int SwitchSwfConfig()
        {
            if (AvailableApps.Tag == null) return 1;

            int iIdx = AvailableApps.SelectedIndex;
            if (iIdx < 0) return 1;
            ConfigTestApplicationCollection lstApps = (ConfigTestApplicationCollection)AvailableApps.Tag;
            if (iIdx >= lstApps.Count) return 1;
            ConfigTestApplication objTestApp = lstApps[iIdx];
            int iError = TargetApplicationsManagement.SwitchSwfConfigFile(objTestApp.ExtraRequirement);
            if (iError!=1)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate()
                    {
                        MessageBox.Show(string.Format("Can't switch to sepecial Swfconfig file:[{0}]", objTestApp.ExtraRequirement));
                    }));
            }
            return 1;
        }

        private void StopServiceButton_Click(object sender, RoutedEventArgs e)
        {
            /** Kill Monitor **/
            string[] cnst_arrApps = { "QtpStarter", "TestFrameMonitor" };
            foreach (string strAppName in cnst_arrApps)
            {
                TigerMarsUtil.KillProcessByName(strAppName);
            }
            /** Stop services**/
            if (objMarsTestingFrame == null) return;
            objMarsTestingFrame.StopService();
        }

        private void BaseLineCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            /** write baseLine Information into configuration file **/
            if (objMarsTestingFrame == null) return;
            MarsTestFrameMain.ChangeBaseLineValue(this.BaseLineCheckBox.IsChecked.Value);
            //objMarsTestingFrame.ChangeBaseLineValue(this.BaseLineCheckBox.IsChecked.Value);
            
        }

        private void AvailableApps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (objMarsTestingFrame == null) return;
            if (this.AvailableApps.SelectedItem == null) return;
            objMarsTestingFrame.ChangeDefaultApplication(this.AvailableApps.SelectedItem.ToString());
        }

        private void AvailableProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
#if _Datafrom_Database
            if (!DashBoardFactory.IsDashBoardFromDB()) return;

            if (AvailableProjects.SelectedItem==null)
            {
                /// CLEAN application data 
                /// 
                this.AvailableApps.ItemsSource = null;
            }
            if (AvailableProjects.SelectedItem is MarsKeyValues<string, string>)
            {


                /// ------------------------------------------------------------------------------------------------------
                /// Important!!!!
                ///   Tiger, 11/20/2015
                ///   Applications with their paths could not be the same as database. For example, The test user can't or 
                /// don't install the target applications for some reasons. System should filter unavailable applications. 
                /// And popup a dialog telling user, no such those applications installed!!!!!
                ///   A module should exists helping user to set up target application path in IDE. And save to configuration file.
                /// -----End of Important notice--------------------------------------------------------------------------
                List<MarsKeyValues<string, string>> lstUnInstalled = new List<MarsKeyValues<string, string>>();
                List<ConfigTestApplication> lstAssignedValidtedApps = TargetApplicationsManagement.CheckInstalledApps(((MarsKeyValues<string, string>)AvailableProjects.SelectedItem).Children, ref lstUnInstalled);
                this.AvailableApps.ItemsSource = lstAssignedValidtedApps;// ((MarsKeyValues<string, string>)AvailableProjects.SelectedItem).Children;
                this.AvailableApps.DisplayMemberPath = "AppName";
                this.AvailableApps.Tag = lstAssignedValidtedApps;
                /// set the selected index
                if (((MarsKeyValues<string, string>)AvailableProjects.SelectedItem).Children == null) return;
                if (((MarsKeyValues<string, string>)AvailableProjects.SelectedItem).Children.Count <= 0) return;
                this.AvailableApps.SelectedIndex = 0;

                if (lstUnInstalled.Count > 0)
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(delegate ()
                        {
                            string appnames_mis = "";
                            foreach (MarsKeyValues<string, string> objMissedApp in lstUnInstalled)
                            {
                                if (string.IsNullOrEmpty(appnames_mis)) appnames_mis = objMissedApp.MValue;
                                else
                                    appnames_mis = string.Format("{0}\r\n,{1}", appnames_mis, objMissedApp.MValue);
                            }
                            MessageBox.Show(string.Format("No such applications installed on this machine, \r\nTest on those appliations will be ignored.\r\nApps:{0}", appnames_mis), "Hint");
                        }
                        )
                    );
                    return;
                }
            }
#endif
        }

    }
}

