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
using System.Windows.Controls.Ribbon;
using Mars.Views;
using Mars.ViewModel;
using Mars.Business;
using System.Collections.ObjectModel;
using Mars.DataLayer;
using System.ComponentModel;
using System.Threading;
using Mars.Delegate;
using Mars.Properties;
using Mars.Utility;

using System.Windows.Threading;

using System.Reflection;
using System.Diagnostics;
using Mars.Helpers;

using Mars.Dialog;
using System.Data;
using Mars.DataLoader;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System.IO;
using Mars.Converter;

using System.Configuration;

using Mars.Report;
using Mars.Dto;
using com.Mars.Constants;
using MarsTestFrame.systemUtil;
using com.Mars.MarsTestingFrame;
using MarsTestFrame.SourceCode.systemUtil;
using com.Mars.TestFrame.Application;
using Mars.Views.user.management;
using Mars.xml.importExport;
using Mars.Views.xml;
using Mars.Views.systemTools;
using Xceed.Wpf.AvalonDock.Layout;
using Mars.Views.baseView;
using Mars.Utility.visualObjects;
using Mars.xml.importExport.xmlnodes;
using XmlCompareLib;
using MARS.CompareGUI;
using com.Mars.Config;
using Mars.autoTest.report.Word;
using System.Windows.Interop;
#if _TestStepUnit
using MarsTestFrame.SourceCode.com.Mars.QTP;
using Mars.Views.objectManagement;
using System.Windows.Interop;
using MarsTestFrame.SourceCode.xmlConfig;
using System.Security.Principal;
#if _NOQTP
using Mars.InjectorAgent;
using Mars.Utility.visualObjects.objectSpyer;

#endif
using Mars.Views.storyboard.batch;
using MARS.OpicsObjects.Extension.fileSelection;
using Mars.thirdPartAppSupport.opics;
using Mars.windowsWrapper.SystemUtil;
#endif

#if _pdfreport
using Mars.autoTest.report.pdf;
#endif

namespace Mars
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MarsMainWindow : RibbonWindow, INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsMainWindow));
        TestCaseTabControl testCaseTabControl;
        TestCaseDataLoader loader;

        private static Configuration currentExeCfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        public static string DbPasswordDecoded
        {
            get
            {
                if (currentExeCfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD] == null)
                {
                    currentExeCfg.AppSettings.Settings.Add(MarsConstants.CNST_DATABASE_PASSWORD, "");
                    currentExeCfg.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("AppSetting");
                    return "";
                }
                string strEncoded = currentExeCfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD].Value.ToString();
                string strDecoded = Mars.Securities.MarsEncodePwd.DecodeString(strEncoded);
                return strDecoded;
            }
        }

        public static string MarsEntiesConnString
        {
            get
            {
                if (currentExeCfg.ConnectionStrings.ConnectionStrings["MarsEntities"] == null) return null;
                return currentExeCfg.ConnectionStrings.ConnectionStrings["MarsEntities"].ToString();
            }
        }

        public static void getDatabasePwd()
        {
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            try
            {
                if (cfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD]==null)
                {
                    cfg.AppSettings.Settings.Add(MarsConstants.CNST_DATABASE_PASSWORD, "");
                    cfg.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("AppSetting");
                }
                string strEncoded = cfg.AppSettings.Settings[MarsConstants.CNST_DATABASE_PASSWORD].Value.ToString();
                string strDecoded = Mars.Securities.MarsEncodePwd.DecodeString(strEncoded);
                MarsEntities.Database_Password = strDecoded;
                string strCnn = cfg.ConnectionStrings.ConnectionStrings["MarsEntities"].ToString();
                MarsEntities.Database_ConnectionString = strCnn;
                //Logger.Info("getDatabasePwd", string.Format("Cnn str:[{0}] EncodedPwd:[{1}]", strCnn, strEncoded));
            }
            catch (Exception e)
            {
                Logger.Error("getDatabasePwd",string.Format("Can't get database password setting from config file. \r\nException:[{0}]",e.Message));
                return;
            }
        }


        #region GUI constant
        public const string CNST_APPSETTING_ISCONTINUETOTEST = "IsContinueLatestTest";
        #endregion

        internal static bool Config_GetValueAsBool(string strItem, bool isDefault = false)
        {
            string strItemAsStr = ConfigurationManager.AppSettings[strItem];
            bool isResult = isDefault;
            bool.TryParse(strItemAsStr, out isResult);
            return isResult;
        }
        internal static void Config_SetValueAsBool(string strItm, bool isTargetValue)
        {
            var objConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (objConfig.AppSettings.Settings[strItm] == null)
            {
                objConfig.AppSettings.Settings.Add(strItm, isTargetValue + "");

            }
            else
            {
                objConfig.AppSettings.Settings[strItm].Value = isTargetValue ? "true" : "false";
            }
            objConfig.Save(ConfigurationSaveMode.Modified);
        }

        public virtual ContextMenu NodeContextMenu
        {
            get { return (ContextMenu)GetValue(NodeContextMenuProperty); }
            set { SetValue(NodeContextMenuProperty, value); }
        }

        public static readonly DependencyProperty NodeContextMenuProperty;

        private ICommand _runCurrentTestStoryBoard;
        public ICommand RunCurrentTestStoryBoard
        {
            get
            {
                return _runCurrentTestStoryBoard;
            }
            set
            {
                _runCurrentTestStoryBoard = value;
            }
        }

        private ICommand _restartFrameWork;
        public ICommand RestartFrameWork
        {
            get { return _restartFrameWork; }
            set { _restartFrameWork = value; }
        }

        private static string currentDatabaseIdx;
        public static string CurrentDatabaseIdx {
            get => currentDatabaseIdx;
            set
            {
                if (value!= currentDatabaseIdx)
                {
                    currentDatabaseIdx = value;
                    //给其他几个模块赋值
                }
            }
        }

        public MarsMainWindow()
        {            
            try
            {
                MarsDBGlobe_Cache.InitCache(currentDatabaseIdx);

                Logger.logBegin("MarsMainWindow");                

                //Style = (Style)FindResource(typeof(TreeViewItem));
                this.isBaseLineTest = GetBaseLineTestFromConfigFile();
                InitializeComponent();

#if NO_DataCompare
                this.ObjectDatabaseGroup.Visibility = Visibility.Collapsed ;
                this.buttonDataCompson.Visibility = Visibility.Collapsed ;
#endif
                string marsDBInstance = ConfigurationManager.AppSettings["MARSDBInstance"];

                _runCurrentTestStoryBoard = new DelegateCommand(() => { OnRunCurrentStoryBoard(null, null); });
                _restartFrameWork = new DelegateCommand(() => { OnRestartFramework(); });

                this.InitTestingFrame();

                Title = WCFXmlCfgMgr.CurrentLoginUser+ ":MARS - Marquis Automation Reusable System v2.6 Build Time: " + Assembly.GetExecutingAssembly().GetLinkerTime() + " DB Instance: " + marsDBInstance+" Session:"+Process.GetCurrentProcess().SessionId;
                PopulateDataSheetList();

                BindTree();
                DataContext = this;
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow", string.Format("Exceptions:[{0}]", ex.Message), ex);
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.ToString(), "MainWindow Error", MessageBoxButton.OK);
            }
        }

        private bool GetBaseLineTestFromConfigFile()
        {
            Logger.logBegin("GetBaseLineTestFromConfigFile");
            return MarsTestFrameMain.GetBaseLineValue();
        }



        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //     this.Hide();
            //     e.Cancel = true; ;
            App.Current.Shutdown(0);
        }

        private void LoadInitialPage()
        {
            if (RunStoryboardWithRunTimeParams())
                return;
            
            try
            {
                string settings = Properties.Settings.Default.CurrentPage;
                if (settings != null)
                {
                    string[] strings = settings.Split(',');

                    if (strings[0].Equals("TC"))
                    {
                        long projectId = Convert.ToInt64(strings[1]);
                        long testSuiteId = Convert.ToInt64(strings[2]);
                        long testCaseId = Convert.ToInt64(strings[3]);
                        long dataSetId = Convert.ToInt64(strings[4]);
                        //LoadInitialTestCase(projectId, testSuiteId, testCaseId, dataSetId);
                        LoadInitialTestCase(tvMars, projectId, testSuiteId, testCaseId, dataSetId,true);
                    }

                    else
                    if (strings[0].Equals("SB"))
                    {
                        long projectId = Convert.ToInt64(strings[1]);
                        long storyboardId = Convert.ToInt64(strings[2]);
                        //LoadInitialStoryboard(projectId, storyboardId);
                        LoadInitialStoryboard(tvMars, projectId, storyboardId, true);
                    }
                    if (strings[0].Equals("DB"))
                    {
                        long projectId = Convert.ToInt64(strings[1]);
                        LoadInitialDashboard(projectId);
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }

        private TreeViewItem LoadInitialTestCase(TreeView tv, long lPorjId, long lTSID, long lTCId, long lDSId,bool isSelect = false)
        {
            TreeViewItem projNode = SelectItemFromTree(tv, lPorjId, false);
            if (projNode == null) return null;
            TreeViewItem tsNode = SelecteTSFromNode(projNode, lTSID, false);
            if (tsNode == null) return null;
            TreeViewItem tcNode = SelectTCFromNode(tsNode, lTCId, false);
            if (tcNode == null) return null;
            TreeViewItem dsNode = SelectDSFromNode(tcNode, lDSId, isSelect);
            return dsNode;
        }

        private TreeViewItem LoadInitialStoryboard(TreeView tv, long lPorjId,long lSBId,bool isSelect=false)
        {
            TreeViewItem projNode = SelectItemFromTree(tv, lPorjId, false);
            if (projNode == null) return null;
            return SelecteStoryboardFromNode(projNode, lSBId, isSelect);
        }

        private TreeViewItem SelecteStoryboardFromNode(TreeViewItem currentProjNode, long sbId, bool isSelect= false)
        {
            if (currentProjNode == null) return null;
            TreeViewItem sbRoot = currentProjNode.ItemContainerGenerator.ContainerFromIndex(1) as TreeViewItem;
            if (!sbRoot.IsExpanded)
            {
                sbRoot.IsExpanded = true;
                sbRoot.UpdateLayout();
            }
            TreeViewItem currentSB = null;
            for (int i = 0; i < sbRoot.Items.Count; i++)
            {
                var tsNodeMOdel = sbRoot.Items[i] as Mars.ViewModel.MarsStoryboardTreeView;
                if (tsNodeMOdel == null) continue;
                var itTS = sbRoot.ItemContainerGenerator.ContainerFromItem(tsNodeMOdel) as System.Windows.Controls.TreeViewItem;//  itTS.DataContext as MarsTestSuiteTreeView;
                if (itTS == null) continue;
                if (tsNodeMOdel.StoryboardId != sbId) continue;
                currentSB = itTS;
                break;
            }
            if (isSelect)
                currentSB.IsSelected = true;
            return currentSB;
        }

        private TreeViewItem SelectDSFromNode(TreeViewItem currentTestCaseNode, long lDSId, bool isSelect = false)
        {
            if (currentTestCaseNode == null) return null;
            TreeViewItem currentDataSetNode = null;
            for (int i = 0; i < currentTestCaseNode.Items.Count; i++)
            {
                var tcNodeMOdel = currentTestCaseNode.Items[i] as Mars.ViewModel.MarsDataSheetTreeView;
                if (tcNodeMOdel == null) continue;
                if (tcNodeMOdel.DataSheetId != lDSId) continue;
                TreeViewItem itDs = currentTestCaseNode.ItemContainerGenerator.ContainerFromItem(tcNodeMOdel) as System.Windows.Controls.TreeViewItem;
                if (itDs == null) continue;
                currentDataSetNode = itDs;
                break;
            }
            if (isSelect)
                currentDataSetNode.IsSelected = true;
            return currentDataSetNode;
        }

        private TreeViewItem SelectTCFromNode(TreeViewItem currentTestSuiteNode, long lTCId, bool isSelect =false)
        {
            if (currentTestSuiteNode == null) return null;
            TreeViewItem currentTestCaseNode = null;
            for (int i = 0; i < currentTestSuiteNode.Items.Count; i++)
            {
                var tcNodeMOdel = currentTestSuiteNode.Items[i] as Mars.ViewModel.MarsTestCaseTreeView;
                if (tcNodeMOdel == null) continue;
                if (tcNodeMOdel.TestCaseId != lTCId) continue;

                TreeViewItem itTC = currentTestSuiteNode.ItemContainerGenerator.ContainerFromItem(tcNodeMOdel) as System.Windows.Controls.TreeViewItem;
                if (itTC == null) continue;

                currentTestCaseNode = itTC;
                break;
            }
            if (currentTestCaseNode == null) return null;
            if (!currentTestCaseNode.IsExpanded)
            {
                currentTestCaseNode.IsExpanded = true;
                currentTestCaseNode.UpdateLayout();
            }
            if (isSelect)
                currentTestCaseNode.IsSelected = true;
            return currentTestCaseNode;
            
        }
        private TreeViewItem SelecteTSFromNode(TreeViewItem currentProjNode, long lTSId, bool isSelect = false)
        {
            if (currentProjNode == null) return null;
            TreeViewItem testsuiteRoot = currentProjNode.ItemContainerGenerator.ContainerFromIndex(2) as TreeViewItem;
            if (!testsuiteRoot.IsExpanded)
            {
                testsuiteRoot.IsExpanded = true;
                testsuiteRoot.UpdateLayout();
            }
            TreeViewItem currentTestSuiteNode = null;
            for (int i = 0; i < testsuiteRoot.Items.Count; i++)
            {
                var tsNodeMOdel = testsuiteRoot.Items[i] as Mars.ViewModel.MarsTestSuiteTreeView;
                if (tsNodeMOdel == null) continue;
                var itTS = testsuiteRoot.ItemContainerGenerator.ContainerFromItem(tsNodeMOdel) as System.Windows.Controls.TreeViewItem;//  itTS.DataContext as MarsTestSuiteTreeView;
                if (itTS == null) continue;
                if (tsNodeMOdel.TestSuiteId != lTSId) continue;
                currentTestSuiteNode = itTS;
                break;
            }
            if (currentTestSuiteNode == null) return null;
            if (!currentTestSuiteNode.IsExpanded)
            {
                currentTestSuiteNode.IsExpanded = true;
                currentTestSuiteNode.UpdateLayout();
            }
            if (isSelect)
                currentTestSuiteNode.IsSelected = true;
            return currentTestSuiteNode;
            
        }

        private TreeViewItem SelectItemFromTree(TreeView tv, long lprojId,bool isSelect=false)
        {
            if (!(tvMars.ItemsSource is ObservableCollection<MarsProjectTreeView>)) return null;
            ObservableCollection<MarsProjectTreeView> lstTestProjs = tvMars.ItemsSource as ObservableCollection<MarsProjectTreeView>;
            var tmpProj = lstTestProjs.Where(p => p.ProjectId == lprojId).FirstOrDefault();
            if (tmpProj == null)
            {
                //ViewModelBase.HintByMessageBox("Can't get the project from left Tree view");
                return null;
            }
            //tvMars.node
            TreeViewItem currentProjNode = null;
            for (int i = 0; i < tvMars.Items.Count; i++)
            {
                var projectItm = tvMars.ItemContainerGenerator.ContainerFromIndex(i) as System.Windows.Controls.TreeViewItem;
                if (projectItm == null) continue;
                MarsProjectTreeView proj = projectItm.DataContext as MarsProjectTreeView;
                if (proj == null) continue;
                if (proj.ProjectId != lprojId) continue;
                currentProjNode = projectItm;
                break;
            }
            if (currentProjNode == null) return null;
            if (!currentProjNode.IsExpanded)
            {
                currentProjNode.IsExpanded = true;
                currentProjNode.UpdateLayout();
            }
            if (isSelect)
                currentProjNode.IsSelected = true;
            return currentProjNode;
        }

        private bool RunStoryboardWithRunTimeParams()
        {
            bool rc = false;
            var runTimeParameters = Environment.GetCommandLineArgs();

            if (runTimeParameters.Length >= 7)
            {
                CommdLineOptions options = new CommdLineOptions();
                options.init(runTimeParameters, 1);

                string projectName = options.GetOptionStringValue("-P");
                string storyboardName = options.GetOptionStringValue("-S");
                string applicationName = options.GetOptionStringValue("-A");

                isBaseLineTest = options.GetOptionBooleanValue("-B");
                IsContinueLatestTest = options.GetOptionBooleanValue("-C");
                IsIgnoreTestCaseError = options.GetOptionBooleanValue("-E");

                if (projectName != null && storyboardName != null && applicationName != null)
                {
                    IsRunningInBatchMode = true;
                    RunStoryboard(projectName, storyboardName, applicationName);
                    rc = true;
                }
            }

            if (runTimeParameters.Length>=2)
            {
                if (runTimeParameters.Where(p=>string.Compare( p,"-NoCache",true)==0).FirstOrDefault()!=null)
                    return true;
            }
            return rc;
        }

        private void RunStoryboard(string projectName, string storyboardName, string applicationName)
        {
            long projectId = BoHelper.GetProjectIdByName(projectName,currentDatabaseIdx);
            long storyboardId = BoHelper.GetStoryboardByName(storyboardName, projectId,currentDatabaseIdx);
            long applicationId = BoHelper.GetApplicationIdByName(currentDatabaseIdx, applicationName);
            LoadInitialStoryboard(projectId, storyboardId);
            RunCurrentStoryBoard(applicationName, applicationId);
        }

        private void LoadInitialStoryboard(long projectId, long storyboardId)
        {
            bool sbFound = false;

            // Open project
            TreeViewItem tv = TreeViewHelper.FindProjectViewItem(tvMars, projectId);
            TreeViewItem prntItm = null;
            if (tv != null)
            {
                //tv.IsSelected = true;
                tv.IsExpanded = true;

                // Open storyboard
                //tv = TreeViewHelper.FindStoryboardTreeViewItem(tv, storyboardId);
                tv = TreeViewHelper.FindStoryboardTreeViewItem(tvMars, storyboardId);
                if (tv != null)
                {
                    tv.IsSelected = true;
                    tv.IsExpanded = true;
                    sbFound = true; ;
                }

                ItemsControl parent = TreeViewHelper.GetSelectedTreeViewItemParent(tv);

                if (parent == null)
                    return;

                tv = parent as TreeViewItem;
                tv.IsExpanded = true;

                if (sbFound == false)
                {
                    tv = TreeViewHelper.FindStoryboardTreeViewItem(tvMars, storyboardId);
                    if (tv != null)
                    {
                        tv.IsSelected = true;
                        tv.IsExpanded = true;
                        sbFound = true; ;
                    }
                }
            }
        }

        private void LoadInitialDashboard(long projectId)
        {
            // Open project
            TreeViewItem tv = TreeViewHelper.FindDashboardTreeViewItem(tvMars, projectId);
            if (tv != null)
            {
                tv.IsSelected = true;
                tv.IsExpanded = true;

                ItemsControl parent = TreeViewHelper.GetSelectedTreeViewItemParent(tv);

                tv = parent as TreeViewItem;
                tv.IsExpanded = true;
            }
        }


        private void LoadInitialTestCase(long projectId, long testSuiteId, long testCaseId, long dataSetId)
        {
            Logger.logBegin("LoadInitialTestCase",string.Format("Projid:[{0}] TSId:[{1}], TCId:[{2}] DSId:[{3}]",  projectId, testSuiteId,testCaseId,dataSetId));

            //navigate treeitems cost too many times
            if (tvMars.ItemsSource == null) return;
            if (!(tvMars.ItemsSource is ObservableCollection<MarsProjectTreeView>)) return;
            ObservableCollection<MarsProjectTreeView> lstTestProjs = tvMars.ItemsSource as ObservableCollection<MarsProjectTreeView>;
            var tmpProj = lstTestProjs.Where(p => p.ProjectId == projectId).FirstOrDefault();
            if (tmpProj==null)
            {
                //ViewModelBase.HintByMessageBox("Can't get the project from left Tree view");
                return;
            }
            //tvMars.node
            TreeViewItem currentProjNode = null;
            for (int i=0;i<tvMars.Items.Count;i++)
            {
                var projectItm = tvMars.ItemContainerGenerator.ContainerFromIndex(i) as System.Windows.Controls.TreeViewItem;
                if (projectItm == null) return;
                MarsProjectTreeView proj = projectItm.DataContext as MarsProjectTreeView;
                if (proj == null) return;
                if (proj.ProjectId != projectId) continue;
                currentProjNode = projectItm;
                break;
            }
            if (currentProjNode == null) return;
            if (!currentProjNode.IsExpanded)
            {
                currentProjNode.IsExpanded = true; 
                currentProjNode.UpdateLayout();
            }

            TreeViewItem testsuiteRoot = currentProjNode.ItemContainerGenerator.ContainerFromIndex(2) as TreeViewItem;
            if (!testsuiteRoot.IsExpanded)
            {
                testsuiteRoot.IsExpanded = true;
                testsuiteRoot.UpdateLayout();
            }
            TreeViewItem currentTestSuiteNode = null;
            for (int i=0;i< testsuiteRoot.Items.Count;i++)
            {
                var tsNodeMOdel = testsuiteRoot.Items[i] as Mars.ViewModel.MarsTestSuiteTreeView;
                if (tsNodeMOdel == null) continue;
                var itTS = testsuiteRoot.ItemContainerGenerator.ContainerFromItem(tsNodeMOdel) as System.Windows.Controls.TreeViewItem;//  itTS.DataContext as MarsTestSuiteTreeView;
                if (itTS == null) continue;
                if (tsNodeMOdel.TestSuiteId != testSuiteId) continue;
                currentTestSuiteNode = itTS;
                break;
            }
            if (currentTestSuiteNode == null) return;
            if (!currentTestSuiteNode.IsExpanded)
            {
                currentTestSuiteNode.IsExpanded = true;
                currentTestSuiteNode.UpdateLayout();
            }
            TreeViewItem currentTestCaseNode = null;
            for (int i=0; i<currentTestSuiteNode.Items.Count;i++)
            {
                var tcNodeMOdel = currentTestSuiteNode.Items[i] as Mars.ViewModel.MarsTestCaseTreeView;
                if (tcNodeMOdel == null) continue;
                if (tcNodeMOdel.TestCaseId != testCaseId) continue;

                TreeViewItem itTC = currentTestSuiteNode.ItemContainerGenerator.ContainerFromItem(tcNodeMOdel) as System.Windows.Controls.TreeViewItem;
                if (itTC == null) continue;
                
                currentTestCaseNode = itTC;
                break;
            }
            if (currentTestCaseNode == null) return;
            if (!currentTestCaseNode.IsExpanded)
            {
                currentTestCaseNode.IsExpanded = true;
                currentTestCaseNode.UpdateLayout();
            }
            TreeViewItem currentDataSetNode = null;
            for (int i = 0; i < currentTestCaseNode.Items.Count; i++)
            {
                var tcNodeMOdel= currentTestCaseNode.Items[i] as Mars.ViewModel.MarsDataSheetTreeView;
                if (tcNodeMOdel == null) continue;
                if (tcNodeMOdel.DataSheetId != dataSetId) continue;
                TreeViewItem itDs = currentTestCaseNode.ItemContainerGenerator.ContainerFromItem(tcNodeMOdel) as System.Windows.Controls.TreeViewItem;
                if (itDs == null) continue;                
                currentDataSetNode = itDs;
                break;  
            }

            currentDataSetNode.BringIntoView();
            if (currentDataSetNode.IsSelected)
                currentDataSetNode.IsSelected = false;
            currentDataSetNode.IsSelected = true;

            /*
            //var tmpts = tmpProj.TEST_SUITE.Where

            TreeViewItem tv = TreeViewHelper.FindDataSheetTreeViewItem(tvMars, projectId, testSuiteId, testCaseId, dataSetId);
            if (tv != null)
            {
                tv.IsSelected = true;

                ItemsControl parent = TreeViewHelper.GetSelectedTreeViewItemParent(tv);
                tv = parent as TreeViewItem;
                tv.IsExpanded = true;
                tv.UpdateLayout();

                parent = TreeViewHelper.GetSelectedTreeViewItemParent(tv);
                tv = parent as TreeViewItem;
                tv.IsExpanded = true;
                tv.UpdateLayout();

                parent = TreeViewHelper.GetSelectedTreeViewItemParent(tv);
                tv = parent as TreeViewItem;
                tv.IsExpanded = true;
                tv.UpdateLayout();

                parent = TreeViewHelper.GetSelectedTreeViewItemParent(tv);
                tv = parent as TreeViewItem;
                tv.IsExpanded = true;
                tv.UpdateLayout();

            }
            */
            Logger.logEnd("LoadInitialTestCase");
        }

        private void LoadInitialTestCase2(long projectId, long testSuiteId, long testCaseId, long dataSetId)
        {
            // Open project
            TreeViewItem tv = TreeViewHelper.FindProjectViewItem(tvMars, projectId);
            if (tv == null)
                return;
            else
            {
                tv.IsSelected = true;
                tv.IsExpanded = true;
            }

            // Open TestSuite

            tv = TreeViewHelper.FindTestSuiteTreeViewItem(tv, testSuiteId);
            if (tv == null)
                return;
            else
            {
                tv.IsExpanded = true;
            }

            ItemsControl parent = TreeViewHelper.GetSelectedTreeViewItemParent(tv);

            TreeViewItem tvParent = parent as TreeViewItem;
            tvParent.IsExpanded = true;

            // Open TestCase

            tv = TreeViewHelper.FindTestCaseTreeViewItem(tv, testCaseId);
            if (tv == null)
                return;
            else
            {
                tv.IsExpanded = true;
            }


            // Open DataSet

            tv = TreeViewHelper.FindSetTreeViewItem(tv, dataSetId);
            if (tv == null)
                return;
            else
            {
                tv.IsExpanded = true;
                tv.IsSelected = true;
            }


        }


        private void BindTree()
        {
            try
            {
                Logger.logBegin("BindTree");
                tvMars.ItemsSource = MarsTreeView.GetMarsTree(currentDatabaseIdx);
                tvMars.Items.Refresh();
                tvMars.UpdateLayout();
            }
            catch (Exception ex)
            {
                Logger.Error("BindTree", string.Format("Exception:[{0}]", ex.Message), ex);
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.ToString(), "BindTree Error", MessageBoxButton.OK);
            }
        }

        private void RibbonButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //LoadControl((RibbonButton) sender);
        }
        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadControl((RibbonButton)sender);
            }
            catch (Exception ex)
            {
                Logger.Error("RibbonButton_Click",string.Format("{0}",ex.Message),ex);
                System.Windows.MessageBox.Show("Exception: \n" + ex.ToString());
            }

        }

        private void LoadTestNetWorkDBTest()
        {
            clearFormPanel();
            AddOneModelToDockPanel(new MarsNetWorkTool(),"MARS Net working Testing");
            //FormPanel.Children.Add(new MarsNetWorkTool());
        }



#if tiger_dock

        private void LoadBatchManagement()
        {
            var batchMgr = new StoryboardBatch();
            AddOneModelToDockPanel(batchMgr, "Mars Test Batch Management");
        }

        private List<DockPanel> GetSpecialTabsByType<T>()
        {
            var q = HostOfUserControl.Children.Where(p => (((DockPanel)p.Content).Children.Count>0) && (((DockPanel)p.Content).Children[0] is T));
            return q.Select(p => p.Content).Cast<DockPanel>().ToList<DockPanel>();
        }

        private List<DockPanel> GetSpecialTabsByType(Type targetType)
        {
            var q = HostOfUserControl.Children.Where(p => (((DockPanel)p.Content).Children.Count > 0) && (((DockPanel)p.Content).Children[0].GetType()==targetType));
            return q.Select(p => p.Content).Cast<DockPanel>().ToList<DockPanel>();
        }

        private bool FindAndActiveControl(UserControl itm, string strTitle)
        {
            if (!(itm is MarsBaseViewControl)) return false;

            List<DockPanel> lstDockHost = GetSpecialTabsByType(itm.GetType());
            if (lstDockHost == null) return false;
            switch (itm.GetType().ToString())
            {
                case "Mars.Views.TestApplicationRegistration":
                case "Mars.Views.systemTools.MarsNetWorkTool":
                case "Mars.Views.user.management.UserAndCompanyManagement":
                case "Mars.Views.objectManagement.DataPopup4ObjDefinition":
                case "Mars.Views.KeywordLibrary":
                case "Mars.Views.TestVarDatabaseControl":
                case "Mars.Views.BaselineDataEditorControl1":
                case "Mars.Views.ObjectDatabaseOpenControl":
                    if (lstDockHost.Count>0)
                    {
                        dockManager.ActiveContent = lstDockHost[0];
                        return true;
                    }
                    return false;
                case "Mars.Views.StoryboardCombinedControl":
                    foreach (var itmTab in lstDockHost)
                    {
                        var itmChld = itmTab.Children[0] as Mars.Views.StoryboardCombinedControl;
                        if (itmChld == null) continue;
                        if (itmChld.StoryboardId == ((Mars.Views.StoryboardCombinedControl)itm).StoryboardId)
                        {
                            dockManager.ActiveContent = itmTab;
                            return true;
                        }
                    }
                    return false;
                case "Mars.Views.ProjectEditControl":
                    foreach (var itmTab in lstDockHost)
                    {
                        var itmChld = itmTab.Children[0] as Mars.Views.ProjectEditControl;
                        if (itmChld == null) continue;
                        if (itmChld.ProjectId == ((Mars.Views.ProjectEditControl)itm).ProjectId)
                        {
                            dockManager.ActiveContent = itmTab;
                            return true;
                        }
                    }
                    return false;
                case "Mars.Views.baseView.MarsProjectBaseControlView":
                    foreach(var itmTab  in lstDockHost)
                    {
                        var itmChld = itmTab.Children[0] as Mars.Views.baseView.MarsProjectBaseControlView;
                        if (itmChld == null) continue;
                        if (itmChld.ProjectId==((Mars.Views.baseView.MarsProjectBaseControlView)itm).ProjectId)
                        {
                            dockManager.ActiveContent = itmTab;
                            return true;
                        }
                    }
                    return false;
                case "Mars.Views.TestCaseEdit":
                    foreach (var itmTab in lstDockHost)
                    {
                        var itmChld = itmTab.Children[0] as Mars.Views.TestCaseEdit;
                        if (itmChld == null) continue;
                        if ((itmChld.TestCaseId == ((Mars.Views.TestCaseEdit)itm).TestCaseId)
                            &&(itmChld.DataSheetId==((Mars.Views.TestCaseEdit)itm).DataSheetId))
                        {
                            dockManager.ActiveContent = itmTab;
                            return true;
                        }
                    }
                    return false;
                case "Mars.Views.TestSuiteEditControl":
                    foreach (var itmTab in lstDockHost)
                    {
                        var itmChld = itmTab.Children[0] as Mars.Views.TestSuiteEditControl;
                        if (itmChld == null) continue;
                        if (itmChld.TestSuiteId == ((Mars.Views.TestSuiteEditControl)itm).TestSuiteId)
                        {
                            dockManager.ActiveContent = itmTab;
                            return true;
                        }
                    }
                    return false;
            }
            return false;
        }

        /// <summary>
        /// 创建新的模块到dock的host中
        /// </summary>
        /// <param name="itm">待添加的模块</param>
        /// <param name="strTitle">需要显示的title</param>
        private void AddOneModelToDockPanel(UserControl itm,string strTitle="New MARS Model")
        {
            //firstly ,check whether same object is creaed on that tab host manager
            bool isFindAndActived = FindAndActiveControl(itm, strTitle);
            if (isFindAndActived) return;

            LayoutDocument objCurrentUserCntrlPrnt = new LayoutDocument();
            HostOfUserControl.Children.Add(objCurrentUserCntrlPrnt);
            objCurrentUserCntrlPrnt.Parent = HostOfUserControl;

            if (itm is MarsBaseViewControl)
                objCurrentUserCntrlPrnt.Title = ((MarsBaseViewControl)itm).Title;
            else
                objCurrentUserCntrlPrnt.Title = strTitle;

            DockPanel hostDockPanel = new DockPanel();
            hostDockPanel.LastChildFill = true;
            hostDockPanel.Children.Add(itm);
            
            objCurrentUserCntrlPrnt.Content = hostDockPanel;

            dockManager.ActiveContent = hostDockPanel;// objCurrentUserCntrlPrnt;

            //objCurrentUserCntrlPrnt. =(new ImageSource()).load (Image)Properties.Resources.ResourceManager.GetObject("ico1.png");

            //dockPanelHost.Items.Add(itm);
            //dockPanelHost.SelectedItem = itm;
        }
#endif

        private void LoadProjExpImp(string cmmdType)
        {
            Logger.logBegin("LoadProjExpImp",string.Format("Command Type:[{0}]",cmmdType));
            try
            {
                string strError = "";
                if (string.IsNullOrEmpty(cmmdType))
                {
                    ViewModelBase.HintByMessageBox("No Command passes to Method.", "Error");
                    return;
                }
                if (cmmdType.ToUpper().StartsWith("IMPORT"))
                {
                    AddOneModelToDockPanel(new XmlProjImportControlxaml(), "Project Import");
                    return;
                }

                if (cmmdType.ToUpper().StartsWith("EXPORT"))
                {
                    MarsProjectBaseControlView objProjControl = null;
                    if (!IsActiveContentProjectEdit(ref strError,ref objProjControl))
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Please Active a project First.\r\nError:[{0}]",strError), "Error");
                        return;
                    }

                    long lProjId = objProjControl.ProjectId;
                    string strProjName = objProjControl.ProjectName;
                    string strTargetDir = "";
                    if (ExportProjectToXml(lProjId, ref strTargetDir,ref strError))
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Project [{0}] exported Sucessfully to\r\n[{1}].", objProjControl.ProjectName, strTargetDir));
                    }
                    else
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Can't Export Project [{0}], with Error:\r\n{1} ", strProjName, strError));
                    }
                    return;
                }
            }
            finally
            {
                Logger.logEnd("LoadProjExpImp");
            }
        }

        private void LoadServicesSetting()
        {
            AddOneModelToDockPanel(new MultipleUserServiceSettingView(), "Users Services Setting");
        }

        private void LoadObjectDataSourceSettings()
        {
            RefreshRibbonControls("Object DataSource Settings");
            clearFormPanel();
            AddOneModelToDockPanel(new DataPopup4ObjDefinition(),"Object Datasource Settings");
        }

        private void LoadControl(string controlName)
        {
            string selectedProject = "";
            long lProject = -1;
            string selectedTestSuite = "";
            RefreshRibbonControls(controlName);
#if !tiger_dock
            FormPanel.LastChildFill = true;
#endif

            string strError = "";
            bool isOk = false;
            try
            {
                switch (controlName)
                {
                    case "Load Batch Management":
                        LoadBatchManagement();
                        break;
                    case "NETWORK/DB Test":
                        LoadTestNetWorkDBTest();
                        break;
                    case "New Project":
                        ProjectAddControl projectAddControl = new ProjectAddControl();
#if tiger_dock
                        AddOneModelToDockPanel(projectAddControl, controlName);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(projectAddControl);
#endif
                        break;
                    case "Open Project":
                        try
                        {
                            LoadAmendDeleteProject("Open", MarsInvokeFrom.e_FromMenu_Rebbon);
                            
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                        }

                        break;
                    case "Amend Project":
                        LoadAmendDeleteProject("Amend",MarsInvokeFrom.e_FromNotTreeView);
                        break;
                    case "Delete Project":
                        LoadAmendDeleteProject("Delete");
                        break;
                    case "SaveAs Project":
                        long lProjectIdForSaveAs = -1;
                        try
                        {
                            MarsProjectBaseControlView objProjCntrl = null;
                            if (!IsActiveContentProjectEdit(ref strError,ref objProjCntrl))
                            {
                                ViewModelBase.HintByMessageBox("Please Active a project first.", "Error");
                                return;
                            }
                            selectedProject = objProjCntrl.ProjectName;
                            lProjectIdForSaveAs = objProjCntrl.ProjectId;
                            //if (tvMars.SelectedItem != null)
                            //    selectedProject = ((MarsProjectTreeView)(tvMars.SelectedItem)).ProjectName;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                        }
                        if (selectedProject == "")
                        {
                            //MessageBox.Show("Please select project to Saveas another project");
                            Status.Text = "Please select project to Saveas another project";
                            return;
                        }
                        LoadSaveAs("Project", selectedProject, lProjectIdForSaveAs);
                        break;
                    case "Project Properties":
                        lProject = -1;
                        try
                        {
                            LoadAmendDeleteProject("Project Properties", MarsInvokeFrom.e_FromMenu_Rebbon);
                            //if (tvMars.SelectedItem != null)
                            //{
                            //    selectedProject = ((MarsProjectTreeView)(tvMars.SelectedItem)).ProjectName;
                            //    lProject = ((MarsProjectTreeView)(tvMars.SelectedItem)).ProjectId;
                            //}
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                        }
//                        if (selectedProject == "" || lProject == -1)
//                        {
//                            //MessageBox.Show("Please select project to Open Project");
//                            Status.Text = "Please select project to Open Project";
//                            ViewModelBase.HintByMessageBox(Status.Text, "Hint");
//                            return;
//                        }
//                        ProjectAddControl projectPropControl = new ProjectAddControl("Project Properties", lProject);
//#if tiger_dock
//                        AddOneModelToDockPanel(projectPropControl);
//#else
//                        clearFormPanel();
//                        FormPanel.Children.Add(projectPropControl);
//#endif
                        break;
                    case "New Test Suite":
                        TestSuiteAddControl testSuiteAddControl = new TestSuiteAddControl();
#if tiger_dock
                        AddOneModelToDockPanel(testSuiteAddControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(testSuiteAddControl);
#endif
                        break;
                    case "Open Test Suite":
                        long lOpenTSId = -1;
                        try
                        {
                            if (!(tvMars.SelectedItem is MarsTestSuiteTreeView))
                            {
                                ViewModelBase.HintByMessageBox("Please activate a Test suite first.", "Hint");
                                return;
                            }
                            if (tvMars.SelectedItem != null)
                            {
                                selectedTestSuite = ((MarsTestSuiteTreeView)(tvMars.SelectedItem)).TestSuiteName;
                                lOpenTSId = ((MarsTestSuiteTreeView)(tvMars.SelectedItem)).TestSuiteId;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                        }
                        if (selectedTestSuite == "")
                        {
                            //popup.DataContext = new ProjectEditViewModel();
                            string type = "TestSuite";
                            popupTestSuite.DataContext = new OpenViewModel(type);
                            popupTestSuite.IsOpen = true;
                            //MessageBox.Show("Please select Test Suite to Open Test Suite");
                            //return;
                        }
                        else
                        {
                            TestSuiteAddControl testSuiteControl = new TestSuiteAddControl(selectedTestSuite, "Open Test Suite", lOpenTSId);
#if tiger_dock
                            AddOneModelToDockPanel(testSuiteControl);
#else
                            clearFormPanel();
                            FormPanel.Children.Add(testSuiteControl);
#endif
                        }
                        break;
                    case "Amend Test Suite":
                        LoadAmendDeleteTestSuite("Amend");
                        break;
                    case "Delete Test Suite":
                        LoadAmendDeleteTestSuite("Delete");
                        break;
                    case "SaveAs Test Suite":
                        try
                        {
                            if ((!(tvMars.SelectedItem is MarsTestSuiteTreeView)) || (tvMars.SelectedItem == null))
                            {
                                ViewModelBase.HintByMessageBox("Please activate a Test suite first.", "Hint");
                                return;
                            }
                            //if (tvMars.SelectedItem != null)
                            //{
                            selectedTestSuite = ((MarsTestSuiteTreeView)(tvMars.SelectedItem)).TestSuiteName;
                            lOpenTSId = ((MarsTestSuiteTreeView)(tvMars.SelectedItem)).TestSuiteId;
                            //}
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                            return;
                        }
                        if (selectedTestSuite == "")
                        {
                            //MessageBox.Show("Please select Test Suite to Saveas.");
                            Status.Text = "Please select Test Suite to Saveas.";
                            return;
                        }
                        LoadSaveAs("Test suite", selectedTestSuite, lOpenTSId);
                        break;
                    case "Test Suite Properties":
                        long lTSId = -1;
                        try
                        {
                            if (tvMars.SelectedItem != null)
                            {
                                selectedTestSuite = ((MarsTestSuiteTreeView)(tvMars.SelectedItem)).TestSuiteName;
                                lTSId = ((MarsTestSuiteTreeView)(tvMars.SelectedItem)).TestSuiteId;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                        }
                        if (selectedTestSuite == "")
                        {
                            //MessageBox.Show("Please select Test Suite to Open.");
                            Status.Text = "Please select Test Suite to Open.";
                            return;
                        }
                        TestSuiteAddControl testSuitePropControl = new TestSuiteAddControl(selectedTestSuite, "Test Suite Properties", lTSId);
#if tiger_dock
                        AddOneModelToDockPanel(testSuitePropControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(testSuitePropControl);
#endif
                        break;
                    case "New Test Case":
                        /*
                         * AF commented out for now
                         */
                        TestCaseAddControl testCaseAddControl = new TestCaseAddControl();
                        AddOneModelToDockPanel(testCaseAddControl, "New Test Case");
                        //dockPanelHost.Items.Add(testCaseAddControl);
                        //testCaseAddControl.Title = "New Test Case";
                        //testCaseAddControl.Show() ;
                        //clearFormPanel();
                        //FormPanel.Children.Add(testCaseAddControl);

                        break;
                    case "Open Test Case":
                        string selectedTestCase = "";
                        try
                        {
                            if (!(tvMars.SelectedItem is MarsTestCaseTreeView))
                            {
                                ViewModelBase.HintByMessageBox("Please Select a test case Node from Left Tree", "Hint");
                                return;
                            }
                            if ((tvMars.SelectedItem != null))
                                selectedTestCase = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseName;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                        }
                        if (selectedTestSuite == "")
                        {
                            //popup.DataContext = new ProjectEditViewModel();
                            string type = "TestCase";
                            popupTestCase.DataContext = new OpenViewModel(type);
                            popupTestCase.IsOpen = true;
                            //MessageBox.Show("Please select Test Suite to Open Test Suite");
                            //return;
                        }
                        else
                        {
                            TestCaseAddControl testCaseControl = new TestCaseAddControl(selectedTestCase, "Open Test Case");
#if tiger_dock
                            
                            AddOneModelToDockPanel(testCaseControl);
#else
                            clearFormPanel();
                            FormPanel.Children.Add(testCaseControl);
#endif
                        }
                        break;
                    case "SaveAs Test Case":
                        selectedTestCase = "";
                        long lTestId = -1;
                        try
                        {
                            if (!((tvMars.SelectedItem is MarsTestCaseTreeView)||(tvMars.SelectedItem is Mars.ViewModel.MarsDataSheetTreeView)))
                            {
                                ShowMessage("Plese Active a test case first!");
                                return;
                            }
                            if (tvMars.SelectedItem != null)
                            {
                                if (tvMars.SelectedItem is MarsTestCaseTreeView)
                                {
                                    selectedTestCase = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseName;
                                    lTestId = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseId;
                                }
                                if (tvMars.SelectedItem is Mars.ViewModel.MarsDataSheetTreeView)
                                {
                                    lTestId = ((Mars.ViewModel.MarsDataSheetTreeView)(tvMars.SelectedItem)).TestCaseId;
                                    selectedTestCase = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseName;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                            
                        }
                        if (selectedTestCase == "")
                        {
                            //MessageBox.Show("Please select Test Case to Saveas.");
                            Status.Text = "Please select Test Case to Saveas.";
                            return;
                        }
                        LoadSaveTestCase("Test case", selectedTestCase, new List<string> {"Only Test Case will be copied","Copy Dataset(s) together" }, lTestId);
                        clearFormPanel();
                        //reload the treeNodes
                        MarsTreeView.BuildTestCaseTree((MarsTestSuiteTreeView)((MarsTestCaseTreeView)tvMars.SelectedItem).Parent, 
                            B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(CurrentDatabaseIdx, ref strError,ref isOk));
                        break;
                    case "Export Test Cases To XML": 
                        selectedTestCase = null;
                        try
                        {
                            lTestId = -1;
                            if (!((tvMars.SelectedItem is MarsTestCaseTreeView)||(tvMars.SelectedItem is MarsDataSheetTreeView)))
                            {
                                ShowMessage("Plese Active a test case first!");
                                return;
                            }
                            if (tvMars.SelectedItem != null)
                            {
                                if (tvMars.SelectedItem is MarsTestCaseTreeView)
                                {
                                    selectedTestCase = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseName;
                                    lTestId = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseId;
                                }
                                else
                                {
                                    selectedTestCase = ((MarsDataSheetTreeView)(tvMars.SelectedItem)).TestCaseName;
                                    lTestId = ((MarsDataSheetTreeView)(tvMars.SelectedItem)).TestCaseId;
                                }
                            }

                            if (!ViewModelBase.QuestionByMessageBox(string.Format("MARS Will {0} [{1}].\r\n\r\nDo you want to Continue?", controlName, selectedTestCase),"Hint")) return;
                            string strTargetFile = "";
                            
                            if (controlName== "Export Test Cases To XML")
                            {
                                ExportTestCase(lTestId, ref strTargetFile, ref strError, selectedTestCase);
                             
                            }
                            return;

                        }
                        catch (Exception ex)
                        {
                            Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", ex.Message, controlName), ex);
                        }
                        break;
                    case "Import Test Cases From XML":
                        /// Select File
                        /// 
                        string strTargetFileImp = "";
                        ImportTestCase(-1, ref strTargetFileImp, ref strError);                        
                        return;
                    case "Amend Test Case":
                        LoadAmendDeleteTestCase("Amend");
                        break;
                    case "Delete Test Case":
                        LoadAmendDeleteTestCase("Delete");
                        break;
                    case "Test Case Properties":
                        LoadPropertiesTestCase();
                        break;
                    case "Copy to the Empty Row(s)":
                        CopyToEmptyRow();
                        break;
                    case "Run Selected Rows":
                        RunSelectedTestSteps();
                        break;
                    case "Amend Application":
                        TestApplicationRegistration newApplicationControl = new TestApplicationRegistration();
#if tiger_dock
                        AddOneModelToDockPanel(newApplicationControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(newApplicationControl);
#endif
                        break;
                    //ApplicationAmendControl applicationAmendControl = new ApplicationAmendControl(controlName);
                    //clearFormPanel();
                    //FormPanel.Children.Add(applicationAmendControl);
                    //break;
                    case "New Application":
                        ApplicationAmendControl applicationAmend = new ApplicationAmendControl();
#if tiger_dock
                        AddOneModelToDockPanel(applicationAmend);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(applicationAmend);
#endif
                        break;
                    case "Application Properties":
                        ApplicationPropertyControl applicationPropertyControl = new ApplicationPropertyControl();
#if tiger_dock
                        AddOneModelToDockPanel(applicationPropertyControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(applicationPropertyControl);
#endif
                        break;
                    case "Keyword Library":
                        KeywordLibrary keywordLibraryControl = new KeywordLibrary();
#if tiger_dock
                        AddOneModelToDockPanel(keywordLibraryControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(keywordLibraryControl);
#endif
                        break;
                    case "Dashboard":
                        long projectId = (long)((MarsFolderTreeView)(tvMars.SelectedItem)).ProjectId;
                        string projectName = (string)((MarsFolderTreeView)(tvMars.SelectedItem)).ProjectName;
                        DashboardControl dashboardControl = new DashboardControl(projectId, projectName);
#if tiger_dock
                        AddOneModelToDockPanel(dashboardControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(dashboardControl);
#endif

                        Properties.Settings.Default.CurrentPage = "DB," + projectId;
                        Properties.Settings.Default.Save();
                        break;
                    case "Storyboard":
                        if (tvMars.SelectedItem != null && tvMars.SelectedItem.GetType().ToString().Contains("MarsStoryboardTreeView"))
                        {
                            selectedProject = ((MarsStoryboardTreeView)(tvMars.SelectedItem)).ProjectName;
                            string storyboardName = ((MarsStoryboardTreeView)(tvMars.SelectedItem)).StoryboardName;
                            long storyboardId = (long)((MarsStoryboardTreeView)(tvMars.SelectedItem)).StoryboardId;
                            projectId = (long)((MarsStoryboardTreeView)(tvMars.SelectedItem)).ProjectId;
#if PERFORMANCE_TRACKING
                            Logger.Info("PERFORMANCE_TRACKING...", "LoadStoryboardControl begin");
#endif
                            LoadStoryboardControl(selectedProject, storyboardName, projectId, storyboardId, this.CurrentStoryBoardDetailID, ((MarsStoryboardTreeView)(tvMars.SelectedItem)));
#if PERFORMANCE_TRACKING
                            Logger.Info("PERFORMANCE_TRACKING...", "LoadStoryboardControl end");
#endif
#if PERFORMANCE_TRACKING
                            Logger.Info("PERFORMANCE_TRACKING...", "UpdateTestInfoByStoryBoardProjectId begin");
#endif
                            UpdateTestInfoByStoryBoardProjectId(projectId, storyboardId);
#if PERFORMANCE_TRACKING
                            Logger.Info("PERFORMANCE_TRACKING...", "UpdateTestInfoByStoryBoardProjectId end");
#endif
                            Properties.Settings.Default.CurrentPage = "SB," + projectId + "," + storyboardId;
                            Properties.Settings.Default.Save();
                        }
                        break;

                    case "TestCase Import":
                        LoadTestCaseImportControl(currentDatabaseIdx);
                        break;


                    case "Storyboard Compare":
                        if (tvMars.SelectedItem != null && tvMars.SelectedItem.GetType().ToString().Contains("MarsStoryboardTreeView"))
                        {
                            selectedProject = ((MarsStoryboardTreeView)(tvMars.SelectedItem)).ProjectName;
                            string storyboardName = ((MarsStoryboardTreeView)(tvMars.SelectedItem)).StoryboardName;
                            long storyboardId = (long)((MarsStoryboardTreeView)(tvMars.SelectedItem)).StoryboardId;
                            projectId = (long)((MarsStoryboardTreeView)(tvMars.SelectedItem)).ProjectId;
                            LoadStoryboardCompareControl(selectedProject, storyboardName, projectId, storyboardId, this.CurrentStoryBoardDetailID);
                            UpdateTestInfoByStoryBoardProjectId(projectId, storyboardId);
                            Properties.Settings.Default.CurrentPage = "SB," + projectId + "," + storyboardId;
                            Properties.Settings.Default.Save();
                        }


                        break;
                    case "StoryboardCombinedControl":
                        //LoadStoryboardControl(null);
                        /*
                        DashboardCombinedControl dashboardCombinedControl = new DashboardCombinedControl();
                        clearFormPanel();
                        FormPanel.Children.Add(dashboardCombinedControl);
                        DashboardEditControl dashboardEditControl2 = new DashboardEditControl();
                        clearDashboardPanel(dashboardCombinedControl.projectTabControl.dashboardDocPanel);
                        dashboardCombinedControl.projectTabControl.dashboardDocPanel.Children.Add(dashboardEditControl2);
                        
                        TestCaseEdit testCaseEdit = new TestCaseEdit("tt55");
                        dashboardCombinedControl.ProjectGridPanel.Children.Add(testCaseEdit);
                    // dashboardCombinedControl.ProjectGridPanel.Children.Add(dashboardEditControl2);
                         */
                        break;
                    case "Object New":
                        ObjectDatabaseAddControl objectControl = new ObjectDatabaseAddControl();
#if tiger_dock
                        AddOneModelToDockPanel(objectControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(objectControl);
#endif
                        break;
                    case "Object Open":
                        ObjectDatabaseOpenControl objectOpenControl = new ObjectDatabaseOpenControl();
#if tiger_dock
                        AddOneModelToDockPanel(objectOpenControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(objectOpenControl);
#endif
                        break;
                    case "Edit Variables":
                        TestVarDatabaseControl editVarControl = new TestVarDatabaseControl();
#if tiger_dock
                        AddOneModelToDockPanel(editVarControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(editVarControl);
#endif
                        break;
                    case "Object Properties":
                        ObjectDatabaseAmendControl objectPropControl = new ObjectDatabaseAmendControl();
#if tiger_dock
                        AddOneModelToDockPanel(objectPropControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(objectPropControl);
#endif
                        break;

                    case "Load Object Data from Excel":
                        ObjectLoadFromExcel objectLoadFromExcel = new ObjectLoadFromExcel();
#if tiger_dock
                        AddOneModelToDockPanel(objectLoadFromExcel);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(objectLoadFromExcel);
#endif
                        break;
                    case "Save Data":
                        DealWithSaveTestCaseData();                        
                        break;

                    case "SaveAs Shared Data":

                        SaveAsWithDescriptionDialog inputDialog = new SaveAsWithDescriptionDialog("Enter Data Set name:", "Enter Data Set Description:", "", "Save as Data Set");
                        string name = "";
                        string description = "";

                        if (inputDialog.ShowDialog() == true)
                        {
                            name = inputDialog.Answer1;
                            description = inputDialog.Answer2;
                            long lDatasheetIdTobeRefreshed = -1,lOwnerProjId=-1, lOwernTSId=-1,lTcId =-1;
                            ///deal with Test case Save Shared Data Sheet As
                            /// 
                            isOk = DealWithTestCaseSaveSharedDataSheetAs(name, description, ref strError,ref lDatasheetIdTobeRefreshed,ref lOwnerProjId,ref lOwernTSId,ref lTcId);
                            //if (!isOk)
                            //{
                            //    ViewModelBase.HintByMessageBox(string.Format("Can't save data sheet as [{0}], with Error:\r\n{1}", name, strError), "Error");
                            //    return;
                            //}
                            //else
                            //{
                            //    //VMCollCash.currentVMColl.SaveSharedDataSheetAs(name, description);
                            //    ViewModelBase.HintByMessageBox(string.Format("Save data sheet as [{0}] successfully", name),"Hint");
                            //}

                            //RefreshTreeForTestCase(VMCollCash.currentVMColl.DataSheetId);
                            RefreshTreeForTestCase(lDatasheetIdTobeRefreshed, lOwnerProjId, lOwernTSId, lTcId);
                        }
                        break;

                    case "Link Data Sheet":
                        LoadAmendDeleteLinkedDataSheet("Amend");
                        break;

                    case "Setting Applications":
                        LoadApplicationSetting();
                        break;
                    case "Base Line Data Setting":
                        LoadBaseLineDataSetting();
                        break;
                    case "Tester Account Setting":
                        LoadTesterAccountManagementControl();
                        break;
                    case "Dataset copy":
                        CopyDatasetForTestCase();
                        break;
                    case "SaveAs DataSet":
                        DoSaveAsDataset();
                        break;
                    default:
                        break;
                }
                //FormPanel.Children.Add(new TextBox() { Width = 100 });
#if !tiger_dock
                FormPanel.LastChildFill = true;
#endif
            }
            catch (Exception e)
            {
                Logger.Error("LoadControl", string.Format("Exception:[{0}],LoadControl,parameters:controlName-[{1}]", e.Message, controlName), e);
                if (e.InnerException != null)
                    MessageBox.Show(e.InnerException.ToString(), "BindTree Error", MessageBoxButton.OK);
            }
        }

        private bool ImportTestCase(long lTestId, ref string strTargetFile, ref string strError)
        {
#if tiger_dock
            AddOneModelToDockPanel(new XmlImportPreviewControl()); 
#else
            clearFormPanel();
            FormPanel.Children.Add(new XmlImportPreviewControl());
#endif
            return true; 
        }


        private bool DealWithSaveTestCaseData()
        {
            Logger.logBegin("DealWithSaveTestCaseData");
            try
            {
                string strError = "";
                TestCaseEdit tcEdtCntrol = null;
                bool isOk = IsActiveContentTestcase(ref strError, ref tcEdtCntrol);
                if (!isOk) return false;
                if (!(tcEdtCntrol.DataContext is VMColl))
                {
                    strError = string.Format("DataContext of Text Case requires VMColl, but it is [{0}]", tcEdtCntrol == null ? "null" : tcEdtCntrol.GetType().ToString());
                    return false;
                }
                //VMCollCash.currentVMColl.SaveDataSheet();
                ((VMColl)tcEdtCntrol.DataContext).SaveDataSheet();
                return true;
            }
            finally
            {
                Logger.logEnd("DealWithSaveTestCaseData");
            }
        }
        private bool DealWithTestCaseSaveSharedDataSheetAs(string strNewName,string strDesc, ref string strError,ref long datasheetIdToBeRefresh,ref long lAssignedProjId, ref long lAssignedTSId,ref long lTCId)
        {
            Logger.logBegin("DealWithTestCaseSaveSharedDataSheetAs",string.Format("Datasheet new name:[{0}], desc:[{1}]",strNewName,strDesc));
            ///算法 
            /// 1，判断是或否testcase active
            /// 2，获得testcase 的context
            /// 3，调用contxt的函数
            /// 
            TestCaseEdit tcEdtCntrol = null;
            bool isOk = IsActiveContentTestcase(ref strError,ref tcEdtCntrol);
            if (!isOk) return false;
            if (!(tcEdtCntrol.DataContext is VMColl))
            {
                strError = string.Format("DataContext of Text Case requires VMColl, but it is [{0}]", tcEdtCntrol == null ? "null" : tcEdtCntrol.GetType().ToString());
                return false;
            }
            ((VMColl)tcEdtCntrol.DataContext).SaveSharedDataSheetAs(strNewName, strDesc);
            datasheetIdToBeRefresh = ((VMColl)tcEdtCntrol.DataContext).DataSheetId;
            lAssignedProjId = ((VMColl)tcEdtCntrol.DataContext).CurrentOwner_ProjectId;
            lAssignedTSId = ((VMColl)tcEdtCntrol.DataContext).CurrentOwner_TestSuiteId;
            lTCId = ((VMColl)tcEdtCntrol.DataContext).TestCaseId;
            return true;
        }

        /// <summary>
        /// 将整个project导出到xml文件树中
        /// 默认目录是auto>....\export\projectsExport\[projectName]\[SB]\SB_Files
        ///                                                       \[TS]\TS_RelationFiles
        ///                                                       \[TC]\TC_Files
        /// </summary>
        /// <param name="lProjId"></param>
        /// <param name="strErrorOrHint"></param>
        /// <returns></returns>
        private bool ExportProjectToXml(long lProjId, ref string strTargetDir, ref string strErrorOrHint)
        {
            Logger.logBegin("ExportProjectToXml",string.Format("Project to Exports:[{0}]", lProjId));
            ///steps:
            /// 1, Export Project Information
            /// 
            TestProjectsExp objProjExp = new TestProjectsExp();
            string strTargetFile = "";            
            strTargetDir = ConfigurationManager.AppSettings[AppConfigReader.CNST_APPSETTING_TARGET_XML_PROJ_EXP_DIR] ?? null; 
            bool isOk = objProjExp.ExportProjectInfo(lProjId, strTargetDir, ref strErrorOrHint, ref strTargetFile);

            return isOk;
        }

        private bool ExportTestCase(long lTestId, ref string strTargetFile, ref string strError,string strTestCaseName)
        {
            Logger.Info("ExportTestCase",string.Format("TestId:[{0}] save to :[{1}]", lTestId, strTargetFile));
            string strTargetDir = AppConfigReader.GetXmlExportDir();
            if (string.IsNullOrEmpty(strTargetDir))
            {
                ViewModelBase.HintByMessageBox(strError="No Directory setting is found.","Error");
                return false;
            }
            if (!Directory.Exists(strTargetDir))
            {
                ViewModelBase.HintByMessageBox(strError = string.Format("No such Directory :\r\n{0}.",strTargetDir), "Error");
                return false;
            }

            TestCaseWithObjectsExp objExp = new TestCaseWithObjectsExp();
            objExp.TargetDirectory = strTargetDir;

            //remove all system unacceptable char's
            strTestCaseName = strTestCaseName ?? "";
            string strToReplace = @"/\*!@~`#$%^&()<>?";
            for (int i=0;i< strToReplace.Length;i++)
                strTestCaseName = strTestCaseName.Replace(strToReplace[i], '_');
            
            if (!objExp.ExportTestCaseWithObjectByTestCaseId(lTestId, ref strError,strTestCaseName))
            {
                ViewModelBase.HintByMessageBox(string.Format("Can't export xml with Error:\r\n\r\n{0}", strError), "Hint");
                return false;
            }
            strTargetFile = objExp.TargetFileName;
            ViewModelBase.HintByMessageBox(string.Format("Test case is successfully exported to [{0}]", strTargetFile), "Hint");
            return true;
        }

        private void LoadApplicationSetting()
        {
            Logger.logBegin("LoadApplicationSetting");
#if tiger_dock
            AddOneModelToDockPanel(new TestApplicationRegistration());
#else
            clearFormPanel();
            FormPanel.Children.Add(new TestApplicationRegistration());
#endif
        }

        private void LoadTesterAccountManagementControl()
        {
            Logger.logBegin("LoadTesterAccountManagementControl");
#if tiger_dock
            AddOneModelToDockPanel(new UserAndCompanyManagement());
#else
            clearFormPanel();
            FormPanel.Children.Add(new UserAndCompanyManagement());
#endif
        }

        private void CopyDatasetForTestCase()
        {
            Logger.logBegin("CopyDatasetForTestCase");
            bool isHintTestCase = false;
            string strError = "";
#if !tiger_dock
            isHintTestCase = (this.FormPanel.Children.Count <= 0) || (!(this.FormPanel.Children[0] is TestCaseEdit));
            if (isHintTestCase)
            {
                ViewModelBase.HintByMessageBox("The functionality is used for Test case, Please active a test case and select rows what you want to copy.", "Hint");
                return;
            }
            TestCaseEdit objTCEdt = (TestCaseEdit)(this.FormPanel.Children[0]);
#else
            TestCaseEdit objTCEdt = null;
            
            bool isOk = IsActiveContentTestcase(ref strError, ref objTCEdt);

#endif
            if (!(objTCEdt.DataContext is VMColl))
            {
                ViewModelBase.HintByMessageBox(string.Format("Test case editor's Datacontext is not VMColl,\r\nbut a [{0}]. \r\nNo addvanced process will be taken.", objTCEdt.DataContext.GetType()), "Hint");
                return;
            }
            
            bool isCopiedDataSett = ((VMColl)objTCEdt.DataContext).CopyDataSettings(ref strError);
            if (!isCopiedDataSett)
            {
                ViewModelBase.HintByMessageBox(strError, "Error");
                return;
            }
            else
            {
                ViewModelBase.HintByMessageBox("Dataset is copied to others with message:\r\n" + strError, "Hint");
                return;
            }

        }

        private void LoadBaseLineDataSetting()
        {
            Logger.logBegin("LoadBaseLineDataSetting");
#if tiger_dock
            AddChild2MainWorkArea(new BaselineDataEditorControl1());
#else
            AddChild2MainWorkArea(new BaselineDataEditorControl1());
#endif
        }

#if tiger_dock
        private void AddChild2MainWorkArea(UserControl objUserControlInstance)
#else
        private void AddChild2MainWorkArea(UserControl objUserControlInstance)
#endif
        {
#if tiger_dock
            AddOneModelToDockPanel(objUserControlInstance);
#else
            clearFormPanel();

            FormPanel.Children.Add(objUserControlInstance);
#endif
        }

        private void LoadTestCaseImportControl(string strDBIdx)
        {
            TestCaseImportControl testCaseImportControl = new TestCaseImportControl();
#if tiger_dock
            AddOneModelToDockPanel(testCaseImportControl);
#else
            clearFormPanel();
            FormPanel.Children.Add(testCaseImportControl);
#endif
            string testCaseName = "EmptyTC";
            long testcaseId = 366;

            //
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var testCase = (from t in marsEntities.T_TEST_CASE_SUMMARY
                            where t.TEST_CASE_NAME == testCaseName
                            select t).FirstOrDefault();

            testcaseId = testCase.TEST_CASE_ID;
            //

            long dataSheetId = 1;

            TestCaseTabControl testCaseTabControlImp = new TestCaseTabControl();

            testCaseImportControl.ProjectGridPanel.Children.Add(testCaseTabControlImp);

            foreach (DataTable dt in loader.TcImportDataSet.Tables)
            {

                if (loader.TcImportDictionary[dt.TableName] == true)
                {
                    TestCaseEdit testCaseEdit = new TestCaseEdit(currentDatabaseIdx, testcaseId, dataSheetId, false, MarsTestingFrame==null? null : MarsTestingFrame.onAddTeststepUnitObjHandler,  false);
                    testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
                    testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

                    testCaseEdit.SetApplicationId(loader.AppId);
                    testCaseEdit.AddTestCaseData(dt);
                    testCaseTabControlImp.addTestCaseEdit(testCaseEdit, dt.TableName);
                }
            }

        }

        private void ReAttachStoryboard2FrameWork(StoryboardCombinedControl storyboardCombinedControl)
        {
            this.onStoryBoardDetailIDChangeHandler += storyboardCombinedControl.ResultViewTabControl.onStoryBoardDetailIdChangeImpl;
            this.onHistoryDataDeleteRequireRefreshHandler += storyboardCombinedControl.ResultViewTabControl.onHistoryDataRequireRefreshImpl;
            //storyboardCombinedControl.DataContext = this;

            StoryboardEditControl storyboardEditControl2 = (StoryboardEditControl)storyboardCombinedControl.storyboardDocPanel.Children[0];            

            Binding objAvailableApps = new Binding
            {
                Path = new PropertyPath("AvailableApplications"),
                Source = storyboardEditControl2
            };
            Binding objUnAvailableApps = new Binding
            {
                Path = new PropertyPath("UnInstalledApplications"),
                Source = storyboardEditControl2,
                Mode = BindingMode.TwoWay
            };
            this.BindData2TestAvailableApps(objAvailableApps);
            this.BindData2UnAvailableApps(objUnAvailableApps);

            Binding objBind = new Binding()
            {
                Path = new PropertyPath("CurrentStoryBoardID"),
                ElementName = "_StoryBoardEditView"
                //Source = storyboardEditControl2
            };
            SetBinding(CurrentStoryBoardIDProperty, objBind);
            storyboardEditControl2.PerformPropertyEvent("CurrentStoryBoardID");
            storyboardEditControl2.PerformPropertyEvent("AvailableApplications");
            storyboardEditControl2.PerformPropertyEvent("UnInstalledApplications");

            Binding objTargetApplicationChange = new Binding
            {
                Path = new PropertyPath("SelectecTestTargetApplication"),
                Source = this
            };
            storyboardEditControl2.SetBinding(StoryboardEditControl.OnTestApplicationIdChange, objTargetApplicationChange);
        }

        private void LoadStoryboardControl(string projectName, string storyboardName, long projectId, long storyboardId, long? storyBoardDetailId, MarsStoryboardTreeView assignedTreeItm = null)
        {
            try
            {
                Logger.logBegin("LoadStoryboardControl");
            
            StoryboardCombinedControl storyboardCombinedControl = new StoryboardCombinedControl(storyboardName, storyboardId);
#if tiger_dock
            AddOneModelToDockPanel(storyboardCombinedControl);
#else
            clearFormPanel();
            FormPanel.Children.Add(storyboardCombinedControl);
            FormPanel.LastChildFill = true;
#endif
            this.onStoryBoardDetailIDChangeHandler += storyboardCombinedControl.ResultViewTabControl.onStoryBoardDetailIdChangeImpl;
            this.onHistoryDataDeleteRequireRefreshHandler += storyboardCombinedControl.ResultViewTabControl.onHistoryDataRequireRefreshImpl;
            // Storyboard
            storyboardCombinedControl.DataContext = this;
            
            StoryboardEditControl storyboardEditControl2 = new StoryboardEditControl(currentDatabaseIdx, projectName, storyboardName, projectId, storyboardId, assignedTreeItm);
            storyboardEditControl2.RibbonTestApplicationReadyHandler = this.OnRibbonTestApplicationsReadyImpl;
            storyboardCombinedControl.storyboardDocPanel.Children.Add(storyboardEditControl2);
            //clearDocPanel(storyboardCombinedControl.projectTabControl.storyboardDocPanel);
            //storyboardCombinedControl.projectTabControl.storyboardDocPanel.Children.Add(storyboardEditControl2);
            Binding objBind = new Binding()
            {
                Path = new PropertyPath("CurrentStoryBoardID"),
                ElementName = "_StoryBoardEditView"
                //Source = storyboardEditControl2
            };

            Binding objBindDetailStoryBoardId = new Binding()
            {
                Path = new PropertyPath("CurrentStoryBoardDetailID"),
                Source = this
            };

            storyboardCombinedControl.SetBinding(StoryboardCombinedControl.CurrentStoryBoardDetailIDProperty, objBindDetailStoryBoardId);

            Binding objBindRefreshEvent = new Binding()
            {
                Path = new PropertyPath("IsRefreshDataReportTimeNow"),
                Source = this,
                Mode = BindingMode.TwoWay
            };
            objBindRefreshEvent.NotifyOnSourceUpdated = true;
            BindingOperations.SetBinding(storyboardCombinedControl, StoryboardCombinedControl.IsNeedRefreshResultNowProperty, objBindRefreshEvent);
            //storyboardCombinedControl.SetBinding(StoryboardCombinedControl.IsNeedRefreshResultNowProperty, objBindRefreshEvent);

            Binding objAvailableApps = new Binding
            {
                Path = new PropertyPath("AvailableApplications"),
                Source = storyboardEditControl2
            };
            Binding objUnAvailableApps = new Binding
            {
                Path = new PropertyPath("UnInstalledApplications"),
                Source = storyboardEditControl2,
                Mode = BindingMode.TwoWay
            };
            this.BindData2TestAvailableApps(objAvailableApps);
            this.BindData2UnAvailableApps(objUnAvailableApps);
            //objBind.ElementName = "_StoryBoardEditView";
            //objBind.sour
            SetBinding(CurrentStoryBoardIDProperty, objBind);

            storyboardEditControl2.PerformPropertyEvent("CurrentStoryBoardID");
            storyboardEditControl2.PerformPropertyEvent("AvailableApplications");
            storyboardEditControl2.PerformPropertyEvent("UnInstalledApplications");



            ///Add binding for TargetApplication selection change
            /// 
            Binding objTargetApplicationChange = new Binding
            {
                Path = new PropertyPath("SelectecTestTargetApplication"),
                Source = this
            };
            storyboardEditControl2.SetBinding(StoryboardEditControl.OnTestApplicationIdChange, objTargetApplicationChange);

            testCaseTabControl = new TestCaseTabControl();
            clearDocPanel(storyboardCombinedControl.ProjectGridPanel);
            storyboardCombinedControl.ProjectGridPanel.Children.Add(testCaseTabControl);
            /// report info
            /// 
            Logger.Info("LoadStoryboardControl", "call RaisePropertyChanged-CurrentStoryBoardDetailID");
            storyboardCombinedControl.ResultViewTabControl.SetBinding(TestResultTabView.CurrentStoryBoardDetailIdProperty, objBindDetailStoryBoardId);
            RaisePropertyChanged("CurrentStoryBoardDetailID");

            Logger.Info("LoadStoryboardControl", "call RaisePropertyChanged-CurrentStoryBoardDetailID,end");
                //this.CurrentStoryBoardDetailID = storyBoardDetailId ;
                /// Deal with Result Panel
                /// 
                //storyboardCombinedControl.AttachResultPanel();

            }
            finally
            {
                Logger.logEnd("LoadStoryboardControl");
            }

        }


        private void LoadStoryboardCompareControl(string projectName, string storyboardName, long projectId, long storyboardId, long? storyBoardDetailId)
        {
            StoryboardCompareControl storyboardCompareControl = new StoryboardCompareControl();
#if tiger_dock
            AddOneModelToDockPanel(storyboardCompareControl);
#else
            clearFormPanel();
            FormPanel.Children.Add(storyboardCompareControl);
#endif

            this.onStoryBoardDetailIDChangeHandler += storyboardCompareControl.ResultViewTabControl.onStoryBoardDetailIdChangeImpl;
            this.onHistoryDataDeleteRequireRefreshHandler += storyboardCompareControl.ResultViewTabControl.onHistoryDataRequireRefreshImpl;
            // Storyboard
            storyboardCompareControl.DataContext = this;

            StoryboardEditControl storyboardEditControl = new StoryboardEditControl(currentDatabaseIdx,projectName, storyboardName, projectId, storyboardId);

            StoryboardEditControl storyboardEditControl2 = new StoryboardEditControl(currentDatabaseIdx,
                MarsStoryboardTreeView.SelectedStoryboardNode.ProjectName,
                MarsStoryboardTreeView.SelectedStoryboardNode.StoryboardName,
                (long)MarsStoryboardTreeView.SelectedStoryboardNode.ProjectId,
                (long)MarsStoryboardTreeView.SelectedStoryboardNode.StoryboardId);

            storyboardEditControl.HideControlPanel();
            storyboardEditControl2.HideControlPanel();

            storyboardEditControl.RibbonTestApplicationReadyHandler = this.OnRibbonTestApplicationsReadyImpl;

            storyboardCompareControl.storyboardDocPanel.Children.Add(storyboardEditControl);  // left upper pannel
            storyboardCompareControl.storyboardDocPanel2.Children.Add(storyboardEditControl2); // right upper pannel

            //StoryboardColl m1 = (StoryboardColl)storyboardEditControl.DataContext;
            //StoryboardColl m2 = (StoryboardColl)storyboardEditControl2.DataContext;

            Binding objBind = new Binding()
            {
                Path = new PropertyPath("CurrentStoryBoardID"),
                ElementName = "_StoryBoardEditView"

            };

            Binding objBindDetailStoryBoardId = new Binding()
            {
                Path = new PropertyPath("CurrentStoryBoardDetailID"),
                Source = this
            };

            storyboardCompareControl.SetBinding(StoryboardCombinedControl.CurrentStoryBoardDetailIDProperty, objBindDetailStoryBoardId);

            Binding objBindRefreshEvent = new Binding()
            {
                Path = new PropertyPath("IsRefreshDataReportTimeNow"),
                Source = this,
                Mode = BindingMode.TwoWay
            };
            objBindRefreshEvent.NotifyOnSourceUpdated = true;
            BindingOperations.SetBinding(storyboardCompareControl, StoryboardCombinedControl.IsNeedRefreshResultNowProperty, objBindRefreshEvent);

            Binding objAvailableApps = new Binding
            {
                Path = new PropertyPath("AvailableApplications"),
                Source = storyboardEditControl
            };
            Binding objUnAvailableApps = new Binding
            {
                Path = new PropertyPath("UnInstalledApplications"),
                Source = storyboardEditControl,
                Mode = BindingMode.TwoWay
            };
            this.BindData2TestAvailableApps(objAvailableApps);
            this.BindData2UnAvailableApps(objUnAvailableApps);

            SetBinding(CurrentStoryBoardIDProperty, objBind);

            storyboardEditControl.PerformPropertyEvent("CurrentStoryBoardID");
            storyboardEditControl.PerformPropertyEvent("AvailableApplications");
            storyboardEditControl.PerformPropertyEvent("UnInstalledApplications");

            ///Add binding for TargetApplication selection change
            /// 
            Binding objTargetApplicationChange = new Binding
            {
                Path = new PropertyPath("SelectecTestTargetApplication"),
                Source = this
            };
            storyboardEditControl.SetBinding(StoryboardEditControl.OnTestApplicationIdChange, objTargetApplicationChange);

            testCaseTabControl = new TestCaseTabControl();
            clearDocPanel(storyboardCompareControl.ProjectGridPanel);
            storyboardCompareControl.ProjectGridPanel.Children.Add(testCaseTabControl);
            /// report info
            /// 
            storyboardCompareControl.ResultViewTabControl.SetBinding(TestResultTabView.CurrentStoryBoardDetailIdProperty, objBindDetailStoryBoardId);
            RaisePropertyChanged("CurrentStoryBoardDetailID");
            //this.CurrentStoryBoardDetailID = storyBoardDetailId ;
            /// Deal with Result Panel
            /// 
            //storyboardCombinedControl.AttachResultPanel();

            
            PopulateTesCaseForStoryboard();

        }


        private void LoadStoryboardControlTESTING(string projectName, string storyboardName, long projectId, long storyboardId)
        {
            // StoryboardCombinedControl storyboardCombinedControl = new StoryboardCombinedControl();
            
            // FormPanel.Children.Add(storyboardCombinedControl);

            // Storyboard

            StoryboardEditControl storyboardEditControl2 = new StoryboardEditControl(currentDatabaseIdx,projectName, storyboardName, projectId, storyboardId);
#if tiger_dock
            AddOneModelToDockPanel(storyboardEditControl2);
#else
            clearFormPanel();
            FormPanel.Children.Add(storyboardEditControl2);
#endif
            // clearDocPanel(storyboardCombinedControl.projectTabControl.storyboardDocPanel);
            ///storyboardCombinedControl.projectTabControl.storyboardDocPanel.Children.Add(storyboardEditControl2);
            /*
             // Project
             ProjectEditControl projectEditControl;
             if (projectName != null)
                 projectEditControl = new ProjectEditControl(projectName);
             else
                 projectEditControl = new ProjectEditControl();
             clearDocPanel(storyboardCombinedControl.projectTabControl.procDocPanel);
             storyboardCombinedControl.projectTabControl.procDocPanel.Children.Add(projectEditControl);
            */
            // TestCase TabControl

            // testCaseTabControl = new TestCaseTabControl();
            // clearDocPanel(storyboardCombinedControl.ProjectGridPanel);
            // storyboardCombinedControl.ProjectGridPanel.Children.Add(testCaseTabControl);
        }


        private void LoadSaveTestCase(string action, string strName, List<string> lstOptions, long lObjectRefId = -1)
        {
            try
            {
                Window window = new Window
                {
                    Title = "SaveAs " + action,
                    Content = new SaveAsControl(action, strName, true, lstOptions,lObjectRefId, "Save As Option:"),
                    Height = 200,                    
                    Width = 360,
                    WindowStyle = WindowStyle.ToolWindow,

                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Owner = this,
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("LoadSaveAs", string.Format("Exception:[{0}]", ex.Message), ex);
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.ToString(), "LoadSaveAs Error", MessageBoxButton.OK);
            }
        }

        private void LoadSaveAs(string action, string strName, long lObjectRefId = -1)
        {
            try
            {
                Window window = new Window
                {
                    Title = "SaveAs " + action,
                    Content = new SaveAsControl(action, strName, lObjectRefId),
                    Height = 160,

                    Width = 350,
                    WindowStyle = WindowStyle.ToolWindow,

                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Owner = this,
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("LoadSaveAs", string.Format("Exception:[{0}]", ex.Message), ex);
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.ToString(), "LoadSaveAs Error", MessageBoxButton.OK);
            }
        }

        private void LoadAmendDeleteProject(string action, MarsInvokeFrom eInvokeFrom = MarsInvokeFrom.e_FromTreeView)
        {
            string selectedProject = "";

            long iProjectId = -1;
            MarsProjectBaseControlView objProj = null;
            string strError = "";
            ///如果InvokeFrom不是from treeview,需要做active content和treeView的判断
            /// 
            MarsInvokeFrom eTmpFrom = eInvokeFrom== MarsInvokeFrom.e_FromTreeView? MarsInvokeFrom.e_FromTreeView:MarsInvokeFrom.e_FromUnknow;
            if (eTmpFrom != MarsInvokeFrom.e_FromTreeView)
            {
                if (tvMars.SelectedItem is MarsProjectTreeView) eTmpFrom = MarsInvokeFrom.e_FromTreeView;
                if (IsActiveContentProjectEdit(ref strError, ref objProj)) eTmpFrom = MarsInvokeFrom.e_FromDockTab;
                if (eTmpFrom== MarsInvokeFrom.e_FromUnknow)
                {
                    MessageBox.Show(string.Format("Please Active a project to {0}.", action), "Project Error", MessageBoxButton.OK,MessageBoxImage.Error);
                    return;
                }
                if (eTmpFrom == MarsInvokeFrom.e_FromDockTab)
                {
                    if (objProj.DataContext == null)
                    {
                        Logger.Error("LoadAmendDeleteProject", "DataContext ==null, system Error.");
                        ViewModelBase.HintByMessageBox("Can't Get Model object.", "Error");
                        return;
                    }
                    
                    selectedProject = ((MarsProjectBaseControlView)objProj).ProjectName;
                    iProjectId = ((MarsProjectBaseControlView)objProj).ProjectId;
                }
                if (eTmpFrom == MarsInvokeFrom.e_FromTreeView)
                {
                    try
                    {
                        if (tvMars.SelectedItem != null)
                        {
                            selectedProject = ((MarsProjectTreeView)(tvMars.SelectedItem)).ProjectName;
                            iProjectId = ((MarsProjectTreeView)(tvMars.SelectedItem)).ProjectId;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("LoadAmendDeleteProject", string.Format("Exception:[{0}]", ex.Message), ex);
                        if (ex.InnerException != null)
                            MessageBox.Show(ex.InnerException.ToString(), "LoadAmendDeleteProject Error", MessageBoxButton.OK);
                        return;
                    }
                }
            }
            else
            {                
                if ((tvMars.SelectedItem is MarsProjectTreeView) == false)
                {
                    MessageBox.Show("Please select a project to be deleted", "Delete Project Error", MessageBoxButton.OK);
                    return;
                }
                try
                {
                    if (tvMars.SelectedItem != null)
                    {
                        selectedProject = ((MarsProjectTreeView)(tvMars.SelectedItem)).ProjectName;
                        iProjectId = ((MarsProjectTreeView)(tvMars.SelectedItem)).ProjectId;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("LoadAmendDeleteProject", string.Format("Exception:[{0}]", ex.Message), ex);
                    if (ex.InnerException != null)
                        MessageBox.Show(ex.InnerException.ToString(), "LoadAmendDeleteProject Error", MessageBoxButton.OK);
                }
            }
            if ((selectedProject == "") || (iProjectId == -1))
            {
                //MessageBox.Show("Please select project to "+ action.ToLower().ToString());
                Status.Text = "Please select project to " + action.ToLower().ToString();
                ViewModelBase.HintByMessageBox(Status.Text, "Hint");
            }
            else
            {
                if (action.Equals("Amend"))
                {

                    ProjectEditControl projectEditControl = new ProjectEditControl(iProjectId, selectedProject, ((MarsProjectTreeView)(tvMars.SelectedItem)));
#if tiger_dock
                    AddOneModelToDockPanel(projectEditControl);
#else
                    clearFormPanel();
                    FormPanel.Children.Add(projectEditControl);
                    FormPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
#endif
                    //LoadStoryboardControl(selectedProject);
                }
                else
                {
                    if (string.Compare("delete", action, true) == 0)
                    {
                        MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                        if (messageBoxResult == MessageBoxResult.Yes)
                        {
                            ProjectEditViewModel pEditVM = new ProjectEditViewModel(iProjectId, selectedProject);
                            pEditVM.DeleteProject(iProjectId);
                        }
                        return;
                    }

                    if (string.Compare("Open", action,true)==0)
                    {
                        ProjectAddControl projectControl = new ProjectAddControl("Open Project", iProjectId);
#if tiger_dock
                        AddOneModelToDockPanel(projectControl);
#else
                        clearFormPanel();
                        FormPanel.Children.Add(projectControl);
#endif
                        return;
                    }
#if tiger_dock
                    if (string.Compare("Project Properties",action,true)==0)
                    {
                        ProjectAddControl projectControl = new ProjectAddControl("Project Properties", iProjectId);
                        AddOneModelToDockPanel(projectControl);
                        return;
                    }
#endif
                }
            }
        }



        private void LoadAmendDeleteTestSuite(string action, bool isFromTreeViewClick = false)
        {
            string selectedTestSuite = "", strError = "";
            long lSelectedTSId = -1;
            bool isOk = false;
            TestSuiteEditControl objTS = null;

            if (!isFromTreeViewClick)
            {
                if (!IsActiveContentTestSuite(ref strError, ref objTS))
                {
                    ViewModelBase.HintByMessageBox("Please select/Active a test suite first.", "Hint");
                    return;
                }
                selectedTestSuite = ((MarsTestSuiteTreeView)objTS.DataContext).TestSuiteName;
                lSelectedTSId = ((MarsTestSuiteTreeView)objTS.DataContext).TestSuiteId;
            }
            else
            {
                try
                {

                    if (!(tvMars.SelectedItem is MarsTestSuiteTreeView))
                    {
                        MessageBox.Show("Please select/Active a test suite first.", "Hint");
                        return;
                    }
                    // everything should be ID
                    if (tvMars.SelectedItem != null)
                    {
                        selectedTestSuite = ((MarsTestSuiteTreeView)(tvMars.SelectedItem)).TestSuiteName;
                        lSelectedTSId = ((MarsTestSuiteTreeView)(tvMars.SelectedItem)).TestSuiteId;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("LoadAmendDeleteTestSuite", string.Format("Exception:[{0}]", ex.Message), ex);
                    if (ex.InnerException != null)
                        MessageBox.Show(ex.InnerException.ToString(), "LoadAmendDeleteTestSuite Error", MessageBoxButton.OK);
                    return;
                }
            }
            if (selectedTestSuite == "")
            {
                //MessageBox.Show("Please select test suite to " + action.ToLower().ToString());
                Status.Text = "Please select test suite to " + action.ToLower().ToString();
            }
            else
            {
                if (action.Equals("Amend"))
                {
                    //TestSuiteEditControl testSuiteEditControl = new TestSuiteEditControl(selectedTestSuite);
                    TestSuiteEditControl testSuiteEditControl = new TestSuiteEditControl(lSelectedTSId, selectedTestSuite, (MarsTestSuiteTreeView)(tvMars.SelectedItem));
                    //int iLevel = 0;
                    //MarsProjectTreeView objProjPrnt = FindProjTreeViewMode((MarsTestSuiteTreeView)(tvMars.SelectedItem), iLevel);
#if tiger_dock
                    AddOneModelToDockPanel(testSuiteEditControl);
#else
                    clearFormPanel();
                    FormPanel.Children.Add(testSuiteEditControl);
#endif
                }
                else
                {
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        TestSuiteEditViewModel tEditVM = new TestSuiteEditViewModel(lSelectedTSId, selectedTestSuite);

                        if (!tEditVM.DeleteTestSuite(selectedTestSuite, lSelectedTSId))
                            return;
                        int iLevel = 0;
                        MarsProjectTreeView objProjPrnt = FindProjTreeViewMode((MarsTestSuiteTreeView)(tvMars.SelectedItem), iLevel);
                        if (objProjPrnt == null) return;
                        MarsTreeView.GetTestSuiteByProjectId(objProjPrnt.ProjectId, objProjPrnt.TEST_FOLDER[2].TREE_ITEM, objProjPrnt.TEST_FOLDER[2],
                            B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(CurrentDatabaseIdx, ref strError, ref isOk));

                    }
                }
            }
        }

        private static MarsProjectTreeView FindProjTreeViewMode(TreeViewModelBase objTreeNode, int iLevel)
        {
            if (objTreeNode == null) return null;
            if (objTreeNode.Parent == null) return null;
            if (objTreeNode.Parent is MarsProjectTreeView) return (MarsProjectTreeView)objTreeNode.Parent;
            if (!(objTreeNode.Parent is TreeViewModelBase))
                return null;
            if (iLevel >= 10) return null;// Noted by tiger, to avoid gabarge codes causing error
            return FindProjTreeViewMode((TreeViewModelBase)objTreeNode.Parent, iLevel + 1);
        }

        private void LoadAmendDeleteLinkedDataSheet(string action)
        {
            long projectId = -1;
            long testSuiteId = -1;
            long testCaseId = -1;

            try
            {
                if (tvMars.SelectedItem != null &&
                    tvMars.SelectedItem is MarsTestCaseTreeView)
                {
                    MarsTestCaseTreeView testCaseTreeView = (MarsTestCaseTreeView)tvMars.SelectedItem;
                    projectId = testCaseTreeView.ProjectId;
                    testSuiteId = testCaseTreeView.TestSuiteId;
                    testCaseId = testCaseTreeView.TestCaseId;

                }
            }
            catch (Exception ex)
            {
                Logger.Error("LoadAmendDeleteLinkedDataSheet", string.Format("Exception:[{0}]", ex.Message), ex);
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.ToString(), "LoadAmendDeleteLinkedDataSheet Error", MessageBoxButton.OK);
            }
            if (projectId == -1)
            {
                //MessageBox.Show("Please select test suite to " + action.ToLower().ToString());
                Status.Text = "Please select test case to " + action.ToLower().ToString();
            }
            else
            {
                if (action.Equals("Amend"))
                {
                    DataSheetLink dataSheetLink = new DataSheetLink(projectId, testSuiteId, testCaseId);
#if tiger_dock
                    AddOneModelToDockPanel(dataSheetLink);
#else
                    clearFormPanel();
                    FormPanel.Children.Add(dataSheetLink);
#endif
                }
                /*
            else
            {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    TestSuiteEditViewModel tEditVM = new TestSuiteEditViewModel(selectedTestSuite);
                    tEditVM.DeleteTestSuite(selectedTestSuite);
                }
            }
                 */
            }
        }

        public void PopulateDataSheetList()
        {
            DataSheetColl = new ObservableCollection<DataItem>();
            /*
            DataSheetColl.Add(new DataItem(1, "First"));
            DataSheetColl.Add(new DataItem(2, "Seconf"));
            DataSheetColl.Add(new DataItem(3, "Third"));
            DataSheetColl.Add(new DataItem(4, "Fourth"));
             * */
        }

        public void PopulateDataSheetList(MarsTestCaseTreeView testCaseTreeView)
        {
            DataSheetColl.Clear();
            IList<KeyValuePair<Int64, string>> dataSheets = BoHelper.GetDataSheetNames(currentDatabaseIdx,
                testCaseTreeView.ProjectId, testCaseTreeView.TestSuiteId, testCaseTreeView.TestCaseId);
            foreach (var dataSheet in dataSheets)
            {
                DataSheetColl.Add(new DataItem(dataSheet.Key, dataSheet.Value));
            }

            // SelectedItem = DataSheetColl.FirstOrDefault().DataItemName;
            _selectedDataSheet = DataSheetColl.FirstOrDefault();
        }

        public void PopulateDataSheetList(MarsDataSheetTreeView dataSheetTreeView)
        {
            DataSheetColl.Clear();
            IList<KeyValuePair<Int64, string>> dataSheets = BoHelper.GetDataSheetNames(currentDatabaseIdx,
                dataSheetTreeView.ProjectId, dataSheetTreeView.TestSuiteId, dataSheetTreeView.TestCaseId);
            foreach (var dataSheet in dataSheets)
            {
                DataSheetColl.Add(new DataItem(dataSheet.Key, dataSheet.Value));
            }

            // SelectedItem = DataSheetColl.FirstOrDefault().DataItemName;
            _selectedDataSheet = DataSheetColl.FirstOrDefault(x => x.Id == dataSheetTreeView.DataSheetId);
            RaisePropertyChanged("SelectedDataSheet");
        }

        private ObservableCollection<string> _control1 = new ObservableCollection<string>();

        public ObservableCollection<string> Control1
        {
            get
            {
                return _control1;
            }
            set
            {
                _control1 = value;

            }
        }

        String _selectedItem;

        public String SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;


        IList<KeyValuePair<Int64, string>> _dataSheetList;

        public IList<KeyValuePair<Int64, string>> DataSheetList
        {
            get { return _dataSheetList; }
            set { _dataSheetList = value; }
        }

        public class DataItem : Notify
        {
            public DataItem()
            {

            }
            public DataItem(long id, string name)
            {
                _id = id;
                _dataItemName = name;
            }

            public long _id;
            public long Id
            {
                get { return _id; }
                set
                {
                    _id = value;
                    OnPropertyChanged("Id");
                }
            }
            private string _dataItemName = "";
            public string DataItemName
            {
                get { return _dataItemName; }
                set
                {
                    _dataItemName = value;
                    OnPropertyChanged("DataItemName");
                }
            }

        }

        private ObservableCollection<DataItem> _dataSheetColl;

        public ObservableCollection<DataItem> DataSheetColl
        {
            get
            {
                return _dataSheetColl;
            }
            set { _dataSheetColl = value; }
        }

        private DataItem _selectedDataSheet;

        public DataItem SelectedDataSheet
        {
            get { return _selectedDataSheet; }
            set
            {
                if (value == null) return;
                if (_selectedDataSheet != null &&
//                     value != null &&
                    _selectedDataSheet.Id == value.Id)
                    return;
                _selectedDataSheet = value;


                if (_selectedDataSheet != null
                    && value != null
                    //&& _selectedDataSheet.Id != value.Id
                    )
                {
                    // Load test case without shared data
                    //LoadTestCase();
                    // TESTING !!!
                    clearFormPanel();
                    LoadTestCaseWithSharedData();
                }
                RaisePropertyChanged("SelectedDataSheet");
            }
        }

        private void LoadTestCaseWithSharedData()
        {
            if (tvMars.SelectedItem == null) return;
            string selectionType = tvMars.SelectedItem.GetType().ToString();
            Console.WriteLine("selectionType=" + selectionType);
            if (selectionType.Equals("Mars.ViewModel.MarsDataSheetTreeView"))
            {
                MarsDataSheetTreeView dataSheetTreeView = null;
                dataSheetTreeView = (MarsDataSheetTreeView)tvMars.SelectedItem;
                string selectedTestCase = dataSheetTreeView.TestCaseName;
                long testCaseId = dataSheetTreeView.TestCaseId;

                long dataSheetId = BoHelper.GetDataSheetId(
                    currentDatabaseIdx,
                    dataSheetTreeView.ProjectId,
                    dataSheetTreeView.TestSuiteId,
                    dataSheetTreeView.TestCaseId);

                if (SelectedDataSheet != null)
                    dataSheetId = SelectedDataSheet.Id;

                TestCaseEdit testCaseEdit = new TestCaseEdit(currentDatabaseIdx, testCaseId, dataSheetId, false, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);
                testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
                testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

                testCaseEdit.PopulateIds(dataSheetTreeView.ProjectId, dataSheetTreeView.TestSuiteId, dataSheetTreeView.TestCaseId);
                //testCaseEdit
                clearFormPanel();
                //
                testCaseEdit.dataSetIsReadImpl += DataSetListIsReadyHandler;
                testCaseEdit.PopulateItemList();
                //

                // TESTING!
                AddComponentToFormPanel(testCaseEdit);
                Properties.Settings.Default.CurrentPage = "TC," +
                                                               dataSheetTreeView.ProjectId + "," +
                                                               dataSheetTreeView.TestSuiteId + "," +
                                                               dataSheetTreeView.TestCaseId + "," +
                                                               dataSheetId;

                Properties.Settings.Default.Save();
            }
            else
            {
                MarsTestCaseTreeView testCaseTreeView = null;
                testCaseTreeView = (MarsTestCaseTreeView)tvMars.SelectedItem;
                string selectedTestCase = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseName;
                long selectedTestId = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseId;
                long dataSheetId = BoHelper.GetDataSheetId(
                    currentDatabaseIdx,
                    testCaseTreeView.ProjectId,
                    testCaseTreeView.TestSuiteId,
                    testCaseTreeView.TestCaseId);
                if (SelectedDataSheet != null)
                    dataSheetId = SelectedDataSheet.Id;

                bool shared = false;

                try
                {
                    if (SelectedDataSheet != null && (SelectedDataSheet.Id >= 0) && SelectedDataSheet.DataItemName.StartsWith("SH"))
                        shared = true;
                }
                catch (Exception)
                {
                    shared = false;
                }

                TestCaseEdit testCaseEdit = new TestCaseEdit(currentDatabaseIdx, selectedTestId, dataSheetId, shared, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);
                testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
                testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

                testCaseEdit.PopulateIds(testCaseTreeView.ProjectId, testCaseTreeView.TestSuiteId, testCaseTreeView.TestCaseId);
                //testCaseEdit
                clearFormPanel();
                //
                testCaseEdit.dataSetIsReadImpl += DataSetListIsReadyHandler;
                testCaseEdit.PopulateItemList();
                //

                // TESTING !!!
                AddComponentToFormPanel(testCaseEdit);
            }

        }

        private void AddComponentToFormPanel(TestCaseEdit testCaseEdit)
        {
#if tiger_dock
            AddOneModelToDockPanel(testCaseEdit);
#else
            clearFormPanel();
            FormPanel.Children.Add(testCaseEdit);
#endif
        }


        private void DataSetListIsReadyHandler(IList<KeyValuePair<Int64, string>> objList)
        {
            //fillup combobox
            // Controls = objList;

            //Control2.Clear();
            // Control2.Add(new DataItem(5, "Alex"));
            return;
        }

        private void LoadAmendDeleteTestCase(string action)
        {
            Logger.logBegin("LoadAmendDeleteTestCase",action);
            string selectedTestCase = "", strError="";
            long selectedTestCaseId_tmp = -1;
            bool isOk = false;
            MarsTestCaseTreeView testCaseTreeView = null;
            try
            {
                if (!(tvMars.SelectedItem is MarsTestCaseTreeView))
                {
                    MessageBox.Show("Please Select/Active a test case first.", "Hint");
                    return;
                }
                if (tvMars.SelectedItem != null)
                {
                    selectedTestCase = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseName;
                    selectedTestCaseId_tmp = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseId;
                    testCaseTreeView = (MarsTestCaseTreeView)tvMars.SelectedItem;

                }

            }
            catch (Exception ex)
            {
                Logger.Error("LoadAmendDeleteTestCase", string.Format("Exception:[{0}]", ex.Message), ex);
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.ToString(), "LoadAmendDeleteTestCase Error", MessageBoxButton.OK);
            }
            if (selectedTestCase == "")
            {
                //MessageBox.Show("Please select test case to " + action.ToLower().ToString());
                Status.Text = "Please select test case to " + action.ToLower().ToString();
            }
            else
            {
                if (action.Equals("Amend"))
                {
                    KeyValuePair<long, string> dataSetInfo = BoHelper.GetDataSheetInfo(currentDatabaseIdx, testCaseTreeView.ProjectId, testCaseTreeView.TestSuiteId, testCaseTreeView.TestCaseId);
                    long dataSheetId = -1;
                    if (dataSetInfo.Equals(default(KeyValuePair<long, string>)))
                    {
                        Logger.Info("LoadAmendDeleteTestCase", strError = string.Format("Can't find any dataset for Test case."));

                    }
                    else
                    {
                        dataSheetId = dataSetInfo.Key;
                        this._selectedDataSheet = new DataItem(dataSheetId, dataSetInfo.Value);
                    }
                    //long dataSheetId =  BoHelper.GetDataSheetId(testCaseTreeView.ProjectId, testCaseTreeView.TestSuiteId, testCaseTreeView.TestCaseId);


                    TestCaseEdit testCaseEdit = new TestCaseEdit(currentDatabaseIdx, selectedTestCaseId_tmp, dataSheetId, false, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);
                    testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
                    testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

                    testCaseEdit.PopulateIds(testCaseTreeView.ProjectId, testCaseTreeView.TestSuiteId, testCaseTreeView.TestCaseId);
                    //testCaseEdit
                    clearFormPanel();
                    //
                    //testCaseEdit.dataSetIsReadImpl += DataSetListIsReadyHandler;
                    testCaseEdit.PopulateItemList();
                    //
                    PopulateDataSheetList(testCaseTreeView);
                    AddComponentToFormPanel(testCaseEdit);

                    Properties.Settings.Default.CurrentPage = "TC," +
                                                               testCaseTreeView.ProjectId + "," +
                                                               testCaseTreeView.TestSuiteId + "," +
                                                               testCaseTreeView.TestCaseId + "," +
                                                               dataSheetId;

                    Properties.Settings.Default.Save();

                    
                }
                else
                {
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        TestStepViewModel tEditVM = new TestStepViewModel(selectedTestCase);
                        try
                        {
                            if (tEditVM.DeleteTestCase(selectedTestCase, selectedTestCaseId_tmp))
                            {
                                clearFormPanel();
                                //reload the treeNodes
                                MarsTreeView.BuildTestCaseTree((MarsTestSuiteTreeView)testCaseTreeView.Parent, 
                                    B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(CurrentDatabaseIdx, ref strError, ref isOk));
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Test Case Delete Command failed. \n Please unlink Data Sets before deleting test case.", "Hint");
                        }
                    }
                }
            }
        }

        private void LoadAmendDeleteStoryboard(string action)
        {
            try
            {
                LoadControl("Storyboard");
            }

            catch (Exception e)
            {
                MessageBox.Show("Exception in LoadAmendDeleteStoryboard. \n " + e, "Exception");
            }

        }

        private void LoadAmendDeleteTestCaseAndDataSheet(string action)
        {
            string selectedTestCase = "",strError ="";
            long selectedTestCaseId = -1;
            bool isOk = false;
            MarsDataSheetTreeView testCaseTreeViewDs = null;
            try
            {
                if (tvMars.SelectedItem != null)
                {
                    selectedTestCase = ((MarsDataSheetTreeView)(tvMars.SelectedItem)).TestCaseName;
                    selectedTestCaseId = ((MarsDataSheetTreeView)(tvMars.SelectedItem)).TestCaseId;
                    testCaseTreeViewDs = (MarsDataSheetTreeView)tvMars.SelectedItem;
                }
#if v_16AndUp
                MarsProjectTreeView objProj = testCaseTreeViewDs.TraceParentNodeToSpecialType<MarsProjectTreeView, TreeViewModelBase>();
                List<long?> lstAssignedAppIds = objProj == null ? null : objProj.AssignedApplicationIdList;
#endif
                if (selectedTestCase == "" || selectedTestCaseId == -1)
                {
                    //MessageBox.Show("Please select test case to " + action.ToLower().ToString());
                    Status.Text = "Please select test case to " + action.ToLower().ToString();
                }
                else
                {
                    if (action.Equals("Amend"))
                    {

                        // long dataSheetId = BoHelper.GetDataSheetId(testCaseTreeViewDs.ProjectId, testCaseTreeViewDs.TestSuiteId, testCaseTreeView.TestCaseId);
                        long dataSheetId = testCaseTreeViewDs.DataSheetId;
                        TestCaseEdit testCaseEdit;
                        if (testCaseTreeViewDs.DataSheetName != null &&
                            testCaseTreeViewDs.DataSheetName.StartsWith("SH"))
                            testCaseEdit = new TestCaseEdit(currentDatabaseIdx, selectedTestCaseId, dataSheetId, true, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);
                        else
                            testCaseEdit = new TestCaseEdit(currentDatabaseIdx, selectedTestCaseId, dataSheetId, false, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);

                        testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
                        testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

                        testCaseEdit.PopulateIds(testCaseTreeViewDs.ProjectId, testCaseTreeViewDs.TestSuiteId, testCaseTreeViewDs.TestCaseId);
                        //testCaseEdit
                        clearFormPanel();
                        //
                        testCaseEdit.dataSetIsReadImpl += DataSetListIsReadyHandler;
                        testCaseEdit.PopulateItemList();
                        //
                        PopulateDataSheetList(testCaseTreeViewDs);
                        AddComponentToFormPanel(testCaseEdit);
                    }
                    else
                    {
                        MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                        if (messageBoxResult == MessageBoxResult.Yes)
                        {
                            TestStepViewModel tEditVM = new TestStepViewModel(selectedTestCase);
                            if (tEditVM.DeleteTestCase(selectedTestCase, selectedTestCaseId))
                            {
                                clearFormPanel();
                                MarsTreeView.BuildTestCaseTree((MarsTestSuiteTreeView)(((MarsTestCaseTreeView)(tvMars.SelectedItem)).Parent), 
                                    B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(CurrentDatabaseIdx, ref strError, ref isOk));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LoadAmendDeleteTestCaseAndDataSheet", string.Format("Exception:[{0}]", ex.Message), ex);
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.ToString(), "LoadAmendDeleteTestCase Error", MessageBoxButton.OK);
            }
        }

        private void LoadTestCase()
        {
            string selectionType = tvMars.SelectedItem.GetType().ToString();
            //Console.WriteLine("selectionType=" + selectionType);
            if (selectionType.Equals("Mars.ViewModel.MarsDataSheetTreeView"))
            {
                MarsDataSheetTreeView dataSheetTreeView = null;
                dataSheetTreeView = (MarsDataSheetTreeView)tvMars.SelectedItem;
                string selectedTestCase = dataSheetTreeView.TestCaseName;
                long selectedTestcaseId = dataSheetTreeView.TestCaseId;

                long dataSheetId = BoHelper.GetDataSheetId(
                    currentDatabaseIdx,
                    dataSheetTreeView.ProjectId,
                    dataSheetTreeView.TestSuiteId,
                    dataSheetTreeView.TestCaseId);

                if (SelectedDataSheet != null)
                    dataSheetId = SelectedDataSheet.Id;

                TestCaseEdit testCaseEdit = new TestCaseEdit(currentDatabaseIdx, selectedTestcaseId, dataSheetId, false, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);
                testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
                testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;
                testCaseEdit.PopulateIds(dataSheetTreeView.ProjectId, dataSheetTreeView.TestSuiteId, dataSheetTreeView.TestCaseId);
                //testCaseEdit
                clearFormPanel();
                //
                testCaseEdit.dataSetIsReadImpl += DataSetListIsReadyHandler;
                testCaseEdit.PopulateItemList();
                //

                AddComponentToFormPanel(testCaseEdit);
            }
            else
            {
                MarsTestCaseTreeView testCaseTreeView = null;
                testCaseTreeView = (MarsTestCaseTreeView)tvMars.SelectedItem;
                string selectedTestCase = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseName;
                long selectedTestcaseId = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseId;
                long dataSheetId = BoHelper.GetDataSheetId(
                    currentDatabaseIdx,
                    testCaseTreeView.ProjectId,
                    testCaseTreeView.TestSuiteId,
                    testCaseTreeView.TestCaseId);
                if (SelectedDataSheet != null)
                    dataSheetId = SelectedDataSheet.Id;

                TestCaseEdit testCaseEdit = new TestCaseEdit(currentDatabaseIdx, selectedTestcaseId, dataSheetId, false, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);
                testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
                testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

                testCaseEdit.PopulateIds(testCaseTreeView.ProjectId, testCaseTreeView.TestSuiteId, testCaseTreeView.TestCaseId);
                //testCaseEdit
                clearFormPanel();
                //
                testCaseEdit.dataSetIsReadImpl += DataSetListIsReadyHandler;
                testCaseEdit.PopulateItemList();
                //

                AddComponentToFormPanel(testCaseEdit);
            }
        }

        private void CopyToEmptyRow()
        {
            string strError = "";
            TestCaseEdit objCurrentTestCase = null;
            if (!IsActiveContentTestcase(ref strError, ref objCurrentTestCase))
            {
                ViewModelBase.HintByMessageBox(strError);
                return;
            }
            if (objCurrentTestCase == null)
            {
                ViewModelBase.HintByMessageBox("No testcase is found.", "ERROR");
                return;
            }
            if (!(objCurrentTestCase.DataContext is VMColl))
            {
                ViewModelBase.HintByMessageBox(string.Format("Testcase is found with unsupported Data context type.[{0}]", objCurrentTestCase.DataContext.GetType()), "ERROR");
                return;
            }

            ((VMColl)objCurrentTestCase.DataContext).OnCopyToEmptyTestStep();
        }

        private void RunSelectedTestSteps()
        {
            Logger.logBegin("RunSelectedTestSteps");
            try
            {
                /// 首先判断是否是test caseedit enabled
                /// 然后判断调用其context的函数
                /// 
                string strError = "";
                TestCaseEdit objCurrentTestCase = null;
                if (!IsActiveContentTestcase(ref strError,ref objCurrentTestCase))
                {
                    ViewModelBase.HintByMessageBox(strError);
                    return;
                }

                if (objCurrentTestCase==null)
                {
                    ViewModelBase.HintByMessageBox("No testcase is found.", "ERROR");
                    return;
                }
                if (!(objCurrentTestCase.DataContext is VMColl))
                {
                    ViewModelBase.HintByMessageBox(string.Format("Testcase is found with unsupported Data context type.[{0}]", objCurrentTestCase.DataContext.GetType()), "ERROR");
                    return;
                }
                ((VMColl)objCurrentTestCase.DataContext).OnRunSeletectedTestSteps();
            }
            finally
            {
                Logger.logEnd("RunSelectedTestSteps");
            }
        }

        private void LoadPropertiesTestCase()
        {
            string selectedTestCase = "";
            try
            {
                if (tvMars.SelectedItem != null)
                    selectedTestCase = ((MarsTestCaseTreeView)(tvMars.SelectedItem)).TestCaseName;
            }
            catch (Exception)
            {

            }
            if (selectedTestCase == "")
            {
                //MessageBox.Show("Please select test case");
                Status.Text = "Please select test case";
            }
            else
            {
                TestCasePropertiesControl testCasePropertiesControl = new TestCasePropertiesControl(selectedTestCase);
#if tiger_dock
                AddOneModelToDockPanel(testCasePropertiesControl);
#else
                clearFormPanel();
                FormPanel.Children.Add(testCasePropertiesControl);
#endif
            }
        }

        private bool KillProcess(string strPrcName,ref string strError)
        {
            Process[] arrp = Process.GetProcessesByName(strPrcName);
            strError = string.Format("No application [{0}] is be killed",strPrcName);
            int iCount = arrp.Length;
            if (arrp == null) return true;
            if (arrp.Length == 0) return true;
            try
            {
                foreach(var itm in arrp)
                {
                    itm.Kill();
                }
                strError = string.Format("Totle [{0}] processes [{1}] is/are killed",iCount,strPrcName );
                return true;
            }catch(Exception e)
            {
                strError = string.Format("can't kill process:[{0}] with exception:\r\n[{1}]]",strPrcName, e.Message);
                return false;
            }
            finally
            {

            }
        }

        private void StartOpicsObjectConvert()
        {
            MarsOpicsXmlObjectManagementForm frmObjct = MarsOpicsXmlObjectManagementForm.GetInstance(currentDatabaseIdx);
            frmObjct.OnGenObject = OpicsObjectConvertor.GenerateObjectHandle;
            frmObjct.OnGenPegwinObj = OpicsObjectConvertor.GeneratePegwinObjHandle;
            frmObjct.OnObjectGenBegin = OpicsObjectConvertor.onObjectGenBeginHandle;
            frmObjct.OnObjectGenEnd = OpicsObjectConvertor.ObjectGenEndHandle;
            frmObjct.OnGetDefaultApplicationConvertorFor = OpicsObjectConvertor.OnGetDefaultApplicationConvertorForHandle;
            frmObjct.OnAfterTransactionIsDone = OpicsObjectConvertor.OnAfterTransactionIsDoneHandle;

            MARS.OpicsObjects.Extension.MarsOpicsObjectsMainEntry.startGUI(currentDatabaseIdx);
        }

        //Check which RibbonButton is clicked and load that control
        private void LoadControl(object sender)
        {
            string strLable = (sender as RibbonButton).Label;
            string strError = "";
            bool isOk = false;

            switch (strLable)
            {
                case "Opics Converter":

                    StartOpicsObjectConvert();
                    return;

                case "Dataset copy":
                    LoadControl(strLable);
                    return;
                case "Clean Duplicated Datasets":
                    int iCleaned = 0;
                    if (B_STORYBOARD_DATASET_SETTING.CleanDuplicatedDatasettings(currentDatabaseIdx, ref iCleaned,ref strError))
                        ViewModelBase.HintByMessageBox(string.Format("Clean duplicated {0} datasettings!", iCleaned));
                    else
                        ViewModelBase.HintByMessageBox(string.Format("Failed to Clean with Error:\r\n{0}", strError));
                    return;
                default:
                    break;
            }


            string toolTipTitle = (sender as RibbonButton).ToolTipTitle;
            switch (toolTipTitle)
            {
                case "Load Batch Management":
                    LoadControl("Load Batch Management");
                    return;
                case "Clean Duplicated Object":
                    int iCnt = -1;
                    if (BoHelper.CleanDuplicatedObjects(currentDatabaseIdx, ref iCnt, ref strError))
                    {                        
                        ViewModelBase.HintByMessageBox(string.Format("Cleaned [{0}] Objects!",iCnt));
                        return;
                    }
                    else
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Can't Clean objects with Error:\r\n{0}", strError));
                        return;
                    }
                    
                case "Kill UFT":
                    string strKillHint = "";
                    KillProcess("UFT", ref strKillHint);
                    ViewModelBase.HintByMessageBox(strKillHint);
                    return;

                case "MARS Services setting":
                    LoadServicesSetting();
                    return;
                case "Object DataSource Setting":
                    LoadObjectDataSourceSettings();
                    return;
                case "NETWORK/DB Test":
                    LoadControl("NETWORK/DB Test");
                    return;
                case "Close MARS":
                    Close();
                    return;
                case "New Project":
                    LoadControl("New Project");
                    break;
                case "Open Project":
                    LoadControl("Open Project");
                    break;
                case "Import Project":
                case "Export Project":
                    LoadProjExpImp(toolTipTitle);
                    break;
                case "Amend Project":
                    LoadControl("Amend Project");
                    break;
                case "Delete Project":
                    LoadControl("Delete Project");
                    break;
                case "SaveAs Project":
                    LoadControl("SaveAs Project");
                    break;
                case "Project Properties":
                    LoadControl("Project Properties");
                    break;
                case "New Test Suite":
                    LoadControl("New Test Suite");
                    break;
                case "Open Test Suite":
                    LoadControl("Open Test Suite");
                    break;
                case "Amend Test Suite":
                    LoadControl("Amend Test Suite");
                    break;
                case "Delete Test Suite":
                    LoadControl("Delete Test Suite");
                    break;
                case "SaveAs Test Suite":
                    LoadControl("SaveAs Test Suite");
                    break;
                case "Test Suite Properties":
                    LoadControl("Test Suite Properties");
                    break;
                case "New Test Case":
                    LoadControl("New Test Case");
                    break;
                case "Open Test Case":
                    LoadControl("Open Test Case");
                    break;
                case "SaveAs Test Case":
                    LoadControl("SaveAs Test Case");
                    break;
                case "Test Case Properties":
                    LoadControl("Test Case Properties");
                    break;
                case "Run Selected Rows":
                    LoadControl("Run Selected Rows");
                    break;
                case "Amend Test Case":
                    LoadControl("Amend Test Case");
                    break;
                case "Delete Test Case":
                    LoadAmendDeleteTestCase("Delete");
                    break;
                case "Amend Application":
                    LoadControl("Amend Application");
                    break;
                case "Application Properties":
                    LoadControl("Application Properties");
                    break;
                case "Keyword Library":
                    LoadControl("Keyword Library");
                    break;
                case "Object New":
                    LoadControl("Object New");
                    break;
                case "Object Open":
                    LoadControl("Object Open");
                    break;
                case "Edit Variables":
                    LoadControl("Edit Variables");
                    break;
                case "Object Properties":
                    LoadControl("Object Properties");
                    break;
                case "SaveAs DataSet":
                case "SaveAs New DataSet":
                case "SaveAs Data":
                    LoadControl("SaveAs DataSet");
                    break;
                case "SaveAs Shared Data":
                    LoadControl("SaveAs Shared Data");
                    break;
                case "Save Data":
                    LoadControl("Save Data");
                    break;
                case "Link Data Sheet":
                    LoadControl("Link Data Sheet");
                    break;

                case "Load Object Data from Excel":
                    LoadControl("Load Object Data from Excel");
                    break;

                case "Import Test Case Data from Excel":
                    LoadWindow("Import Test Case Data from Excel");
                    break;

                case "Start Data Comparison":
                    StartDataComparison();
                    break;

                case "Refresh Tree":
                    BindTree();
                    break;
                case "Setting Applications":
                case "Base Line Data Setting":
                case "New Application":
                case "Tester Account Setting":
                case "Export Test Cases To XML":
                case "Import Test Cases From XML":
                case "Dataset copy":
                    LoadControl(toolTipTitle);
                    break;
                case "Copy to the Empty Row(s)":
                    CopyToEmptyRow();
                    break;
                default:
                    break;
            }
        }

        private void LoadWindow(string action)
        {
            bool isWindowOpen = false;
            foreach (Window w in Application.Current.Windows)
            {
                if (w is ExcelToTCDialog)
                {
                    isWindowOpen = true;
                    w.Activate();
                }
            }


            if (isWindowOpen == false)
            {
                ExcelToTCDialog dlg = ExcelToTCDialog.GetInstance();


                // dlg.onLoadTC += LoadTCFromFile;
                dlg.LoadEventHandler += LoadTCFromFile;
                dlg.Show();
            }

        }

#region Data compare management part
        //private string dataComparison = null;
        private DataCompareForm mc = null;

        private void StartDataComparison()
        {

            mc = DataCompareForm.GetInstance();
            mc.Show();
            return;

            /*
            bool isWindowOpen = false;
            foreach (Window w in Application.Current.Windows)
            {
                if (w is DataCompareForm)
                {
                    isWindowOpen = true;
                    w.Activate();
                }

                if (isWindowOpen == false)
                {
                    DataCompareForm dlg = new DataCompareForm();
                    dlg.Show();
                }
            }
            */

        }
#endregion

        private void LoadTCFromFile(DataSet ds, Dictionary<string, bool> dict, long appId)
        {
            loader = new TestCaseDataLoader("Import", ds, dict, appId);
            LoadControl("TestCase Import");
            loader.LoadAll();
        }

        private void clearFormPanel()
        {
#if !tiger_dock
            if (FormPanel != null && FormPanel.Children.Count > 0)
            {

                while (FormPanel.Children.Count > 0)
                {
                    var x = FormPanel.Children[0];
                    FormPanel.Children.Remove(x);
                }
            }
#endif
        }

        private void clearDocPanel(DockPanel dockPanel)
        {
            if (dockPanel != null && dockPanel.Children.Count > 0)
            {
                foreach (Control x in dockPanel.Children)
                {
                    dockPanel.Children.Remove(x);
                    if (dockPanel.Children.Count == 0)
                        break;
                }
            }
        }

        private void MarsRibbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadControl((Ribbon)sender);
        }

        //Check which RibbonTab is clicked and load that control
        private void LoadControl(Ribbon sender)
        {
            //Intial loading and hence not action required on Tab selection change. Return
#if !tiger_dock
            if (FormPanel == null)
                return;
#endif
            var item = sender as Ribbon;
            var selected = item.SelectedItem as RibbonTab;
            //Check which RibbonButton is clicked and load that control
            string strHeader = selected.Header.ToString();
            switch (strHeader)
            {
                case "Project":
                    // LoadControl("New Project");
                    break;
                case "Test Suite":
                    //LoadControl("New Test Suite");
                    break;
                case "Test Case":
                    //LoadControl("New Test Case");
                    break;
                case "Application":
                    LoadControl("New Application");
                    break;
                case "Keyword Library":
                    LoadControl("Keyword Library");
                    break;
                case "Storyboard":
                    //LoadControl("Storyboard");
                    break;
                case "Dashboard":
                    // LoadControl("Dashboard");
                    break;
                case "Storyboard2":
                    //LoadControl("StoryboardCombinedControl");
                    break;
                case "Object Database":
                    LoadControl("Object New");
                    break;
                default:
                    break;
            }
        }


        private void tvMars_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //MarsStoryboardTreeView treeNode = MarsStoryboardTreeView.SelectedStoryboardNode;
            TreeViewItem item = GetTreeViewItemClicked(e.OriginalSource);

            if (item != null)
            {
                item.IsSelected = false;
                item.IsSelected = true;
                if (item.Header is MarsStoryboardTreeView)
                {

                    //tvMars.RaiseEvent(new RoutedPropertyChangedEventArgs<object>(tvMars.SelectedItem, item.Header));
                    //RaiseEvent();
                    //MarsStoryboardTreeView.SelectedStoryboardNode = (MarsStoryboardTreeView)item.Header;
                }
                else
                {
                    e.Handled = true;
                }
            }

        }


        private TreeViewItem GetTreeViewItemClicked(object targetObject)
        {

            DependencyObject obj = targetObject as DependencyObject;
            while (obj != null && !(obj is TreeViewItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
                if (obj == null) continue;
                string t = obj.GetType().ToString();
            }

            return obj as TreeViewItem;
        }

        private void tvMars_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            object current;
            current = tvMars.SelectedItem;
            if (current == null)
                return;
            Logger.logBegin("tvMars_SelectedItemChanged",string.Format("Action tYPE:[{0}]",e.NewValue==null?"N/A":e.NewValue.GetType().ToString()));

            switch (current.GetType().Name.ToLower())
            {
                case "marsprojecttreeview":
                    //var selectedProject= ((MarsProjectTreeView)(current)).ProjectName;
                    //MarsRibbon.SelectedIndex = 0;
                    LoadAmendDeleteProject("Amend",MarsInvokeFrom.e_FromTreeView);
                    break;
                case "marstestsuitetreeview":
                    //var selectedTestSuite = ((MarsTestSuiteTreeView)(current)).TestSuiteName;
                    //MarsRibbon.SelectedIndex = 1;
                    LoadAmendDeleteTestSuite("Amend",true);
                    //TODO:Akhilesh by commented as it was replacing testecase edit screen
                    break;
                case "marstestcasetreeview":
                    //var selectedTestCase = ((MarsTestCaseTreeView)(current)).TestCaseName;
                    //MarsRibbon.SelectedIndex = 2;
                    LoadAmendDeleteTestCase("Amend");
                    break;
                case "marsdatasheettreeview":
                    //var selectedTestCase = ((MarsTestCaseTreeView)(current)).TestCaseName;
                    // MarsRibbon.SelectedIndex = 2;
                    LoadAmendDeleteTestCaseAndDataSheet("Amend");
                    break;
                case "marsstoryboardtreeview":
                    LoadAmendDeleteStoryboard("Amend");
                    break;
                case "marsfoldertreeview":
                    LoadAmendDeleteFolder("Amend");
                    break;
                default:
                    break;
            }
            //Logger.logEnd("tvMars_SelectedItemChanged"); 
        }

        private void LoadAmendDeleteFolder(string p)
        {
            string tag = "";

            if (tvMars.SelectedItem != null)
                tag = ((MarsFolderTreeView)(tvMars.SelectedItem)).FolderName;
            if (tag.Equals("Dashboard"))
                LoadControl("Dashboard");
        }



        private void tvMars_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            BindTree();
        }

        private void PopUp_OK_Click(object sender, RoutedEventArgs e)
        {
            popupTestSuite.IsOpen = false;
            string selectedTestSuite = "";
            long lTSId = -1;
            foreach (OpenTestSuite item in listTestSuite.Items)
            {
                if (item.IsSelected)
                {
                    selectedTestSuite = ((Mars.ViewModel.OpenTestSuite)(item)).TestSuiteName;
                    lTSId = ((Mars.ViewModel.OpenTestSuite)(item)).TestSuiteId;
                    TestSuiteAddControl testSuiteControl = new TestSuiteAddControl(selectedTestSuite, "Open Test Suite", lTSId);
#if tiger_dock
                    //testSuiteControl.Title = "Open Test Suite";
                    AddOneModelToDockPanel(testSuiteControl);
#else
                    clearFormPanel();
                    FormPanel.Children.Add(testSuiteControl);
#endif
                    break;
                }
            }

        }

        private void PopUp_Close_Click(object sender, RoutedEventArgs e)
        {
            popupTestSuite.IsOpen = false;
        }

        private void PopUpTestCase_OK_Click(object sender, RoutedEventArgs e)
        {
            popupTestCase.IsOpen = false;
            string selectedTestCase = "";
            foreach (OpenTestCase item in listTestCase.Items)
            {
                if (item.IsSelected)
                {
                    selectedTestCase = ((Mars.ViewModel.OpenTestCase)(item)).TestCaseName;
                    TestCaseAddControl testCaseControl = new TestCaseAddControl(selectedTestCase, "Open Test Case");
#if tiger_dock
                    testCaseControl.Title = "Open Test Case";
                    AddOneModelToDockPanel(testCaseControl);
#else
                    clearFormPanel();
                    FormPanel.Children.Add(testCaseControl);
#endif
                    break;
                }
            }

        }

        private void PopUpTestCase_Close_Click(object sender, RoutedEventArgs e)
        {
            popupTestCase.IsOpen = false;
        }

        private void RibbonTab_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void RibbonWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Console.WriteLine("CTRL");
            }
        }

        private void DataSheetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            string text = (sender as ComboBox).SelectedItem as string;
            Console.WriteLine("DataSheetComboBox_SelectionChanged -- " + text);
        }


#region RoutedEvent
        private void HandleChildSignal(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.logBegin("HandleChildSignal",string.Format("Sender:[{0}]", sender));

            if (testCaseTabControl == null)
            {
                Logger.Warnning("HandleChildSignal", "testCaseTabControl == null");
                return;
            }

            StoryboardColl sbColl = StoryboardCache.currentSBColl;
            if (sbColl.SelectedStoryboardRows == null) return;
            if (sbColl.SelectedStoryboardRows.Count() == 0)
            {
                Logger.Warnning("HandleChildSignal", "sbColl.SelectedStoryboardRows.Count() == 0");
                return;
            }

            StoryboardEditViewModel sbvm = sbColl.SelectedStoryboardRows[0];
            if (sbvm.SelectedTestCase == null)
            {
                Logger.Warnning("HandleChildSignal", "sbvm.SelectedTestCase == null");
                return;
            }
            string testCaseName = sbvm.SelectedTestCase.DataItemName;
            long testCaseId = sbvm.SelectedTestCase.Id;
            this.CurrentStoryBoardDetailID = sbvm.StoryboardDetailId;
            long dataSheetId = (long)sbvm.SelectedDataSetName.Id;


            // TestCaseEdit testCaseEdit = new TestCaseEdit(testCaseName, dataSheetId);
            // testCaseTabControl.addTestCaseEdit(testCaseEdit, testCaseName);

            string dataSheetName = sbvm.SelectedDataSetName.DataItemName;
            bool shared = false;
            if (dataSheetName != null && dataSheetName.StartsWith("SH"))
                shared = true;

            TestCaseEdit testCaseEdit = new TestCaseEdit(currentDatabaseIdx, testCaseId, dataSheetId, shared, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);
            testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
            testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

            testCaseEdit.HideControlPanel();
            testCaseTabControl.addTestCaseEdit(testCaseEdit, testCaseName);

        }

        private void HandleChildEditSignal(object sender, System.Windows.RoutedEventArgs e)
        {
            //Console.WriteLine("RoutedEvent!!!");
            Logger.logBegin("HandleChildEditSignal", string.Format("sender:[{0}]",sender));

            StoryboardColl sbColl = StoryboardCache.currentSBColl;

            if ((sbColl.SelectedStoryboardRows == null) || (sbColl.SelectedStoryboardRows.Count == 0))
            {
                Logger.Warnning("HandleChildEditSignal", "sbColl.SelectedStoryboardRows == null");
                return;
            }
            if (sbColl.SelectedStoryboardRows.Count > 1)
            {
                
            }
            StoryboardEditViewModel sbvm = sbColl.SelectedStoryboardRows[0];
            if (sbvm.SelectedTestCase == null)
            {
                Logger.Warnning("HandleChildEditSignal", "sbvm.SelectedTestCase == null");
                return;
            }
            string testCaseName = sbvm.SelectedTestCase.DataItemName;
            long testCaseId = sbvm.SelectedTestCase.Id;
            this.CurrentStoryBoardDetailID = sbvm.StoryboardDetailId;
            long dataSheetId = (long)sbvm.SelectedDataSetName.Id;
            try {
                LoadInitialTestCase(sbvm.ProjectId, sbvm.SelectedTestSuite == null ? -1 : sbvm.SelectedTestSuite.Id,
                    sbvm.SelectedTestCase == null ? -1 : sbvm.SelectedTestCase.Id,
                    sbvm.SelectedDataSetName.Id);
                //LoadInitialTestCase(sbvm.ProjectId,
                //                    (long)sbvm.TestSuiteId,
                //                    sbvm.TestCaseId,
                //                    (long)sbvm.DataSummaryId);

            }
            catch(Exception ex)
            {
                ViewModelBase.HintByMessageBox(string.Format("Please try to Left click first. \r\n\r\nExceptions:\r\n{0}\r\nStackTrace:{1}", ex.Message,ex.StackTrace));
            }

        }

        private void ShowStoryboard(object sender, System.Windows.RoutedEventArgs e)
        {
            Console.WriteLine("ShowStoryboard !!");
            CustomRoutedEventArgs args = (CustomRoutedEventArgs)e;
            long storyboardId = args.Id;

            TreeViewItem tv = TreeViewHelper.FindStoryboardTreeViewItem(tvMars, storyboardId);
            if (tv != null)
                tv.IsSelected = true;
        }

        private void RefreshTree(object sender, System.Windows.RoutedEventArgs e)
        {
            MarsFolderTreeView folderView = (MarsFolderTreeView)tvMars.SelectedItem;
            MarsTreeView.BuildStoryboardFolderTree(folderView);

            CustomRoutedEventArgs args = (CustomRoutedEventArgs)e;
            long storyboardId = args.Id;
            Console.WriteLine("storyboardId = " + storyboardId);

            List<MarsStoryboardTreeView> storyboardList = new List<MarsStoryboardTreeView>();
            foreach (var item in folderView.TREE_ITEM)
            {
                storyboardList.Add((MarsStoryboardTreeView)item);
            }

            MarsStoryboardTreeView v = storyboardList.Where(x => x.StoryboardId == storyboardId).FirstOrDefault();

            TreeViewItem tv = TreeViewHelper.FindStoryboardTreeViewItem(tvMars, storyboardId);
            if (tv != null)
                tv.IsSelected = true;
        }
        
        private void RefreshTreeForTestCase(long dataSheetId,long lOwnerProjId, long lOwnerTSId,long lTCId)
        {
            Logger.logBegin("RefreshTreeForTestCase", string.Format("dsId:[{0}] proj_id:[{1}] TS_id:[{2}] TC_Id:[{3}]", dataSheetId, lOwnerProjId, lOwnerTSId, lTCId));
            string strError = "";
            bool isOk = false; ;
            TreeViewItem objTCItem = TreeViewHelper.FindTestcaseNode(tvMars, lOwnerProjId, lOwnerTSId, lTCId);
            if  (objTCItem!=null)
            {
                MarsTreeView.BuildDataSheetTree((MarsTestCaseTreeView)objTCItem.DataContext,
                    B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(CurrentDatabaseIdx,ref strError, ref isOk));
                return;
            }
            return;
#region oldCode
            //var selectedItem = tvMars.SelectedItem;
            //long projectId = 0;
            //long testSuiteId = 0;
            //long testCaseId = 0;
            //MarsTestCaseTreeView tc = null;
            
            //if (selectedItem is MarsTestCaseTreeView)
            //{
            //    tc = (MarsTestCaseTreeView)selectedItem;
            //    MarsTreeView.BuildDataSheetTree((MarsTestCaseTreeView)selectedItem, B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(ref strError, ref isOk));
            //}

            //else if (selectedItem is MarsDataSheetTreeView)
            //{
            //    var dataSheet = (MarsDataSheetTreeView)selectedItem;
            //    TreeViewItem tv = TreeViewHelper.FindTestCaseTreeViewItem(tvMars, (long)dataSheet.TestCaseId);
            //    tc = (MarsTestCaseTreeView)tv.DataContext;
            //    MarsTreeView.BuildDataSheetTree((MarsTestCaseTreeView)tc, B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(ref strError, ref isOk));
            //}

            //if (tc != null)
            //{
            //    projectId = tc.ProjectId;
            //    testSuiteId = tc.TestSuiteId;
            //    testCaseId = tc.TestCaseId;
            //    TreeViewItem tvi = TreeViewHelper.FindDataSheetTreeViewItem((TreeView)tvMars, projectId, testSuiteId, testCaseId, dataSheetId);
            //    if (tvi != null)
            //    {
            //        tvi.IsSelected = true;
            //    }

            //}
#endregion
        }

        private void RefreshTreeForTestCaseId()
        {
            var selectedItem = tvMars.SelectedItem;
            string strError = "";
            bool isOk = false;

            MarsTestCaseTreeView tc = null;

            if (selectedItem is MarsTestCaseTreeView)
            {
                tc = (MarsTestCaseTreeView)selectedItem;
                MarsTreeView.BuildDataSheetTree((MarsTestCaseTreeView)selectedItem, B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(CurrentDatabaseIdx,ref strError, ref isOk));
                long testCaseId = tc.TestCaseId;
                TreeViewItem tv = TreeViewHelper.FindTestCaseTreeViewItem(tvMars, testCaseId);
                if (tv != null)
                    tv.IsExpanded = true;
            }

            else if (selectedItem is MarsDataSheetTreeView)
            {
                var dataSheet = (MarsDataSheetTreeView)selectedItem;
                TreeViewItem tv = TreeViewHelper.FindTestCaseTreeViewItem(tvMars, (long)dataSheet.TestCaseId);
                tc = (MarsTestCaseTreeView)tv.DataContext;
                MarsTreeView.BuildDataSheetTree((MarsTestCaseTreeView)tc, B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(CurrentDatabaseIdx, ref strError, ref isOk));
            }
            /*
           if (tc != null)
            {
                projectId = tc.ProjectId;
                testSuiteId = tc.TestSuiteId;
                testCaseId = tc.TestCaseId;
                TreeViewItem tvi = TreeViewHelper.FindDataSheetTreeViewItem((TreeView)tvMars, projectId, testSuiteId, testCaseId, dataSheetId);
                if (tvi != null)
                {
                    tvi.IsSelected = true;
                }

            }
            */
        }


#endregion

        private void PopulateTesCaseForStoryboard()
        {
            Console.WriteLine("PopulateTesCaseForStoryboard!!!");

            if (testCaseTabControl == null)
                return;

            StoryboardColl sbColl = StoryboardCache.currentSBColl;

            if (sbColl.StoryboardRows.Count == 0)
                return;

            StoryboardEditViewModel sbvm = sbColl.StoryboardRows[0];
            if (sbvm == null)
                return;

            if (sbvm.SelectedTestCase == null)
                return;
            string testCaseName = sbvm.SelectedTestCase.DataItemName;
            long testcaseId = sbvm.SelectedTestCase.Id;
            long dataSheetId = (long)sbvm.SelectedDataSetName.Id;

            string dataSheetName = sbvm.SelectedDataSetName.DataItemName;
            bool shared = false;
            if (dataSheetName != null &&
                dataSheetName.StartsWith("SH"))
                shared = true;

            TestCaseEdit testCaseEdit = new TestCaseEdit(currentDatabaseIdx, testcaseId, dataSheetId, shared, MarsTestingFrame == null ? null : MarsTestingFrame.onAddTeststepUnitObjHandler);
            testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
            testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

            testCaseEdit.HideControlPanel();
            testCaseTabControl.addTestCaseEdit(testCaseEdit, testCaseName);
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.logBegin("RibbonWindow_Loaded");
#if !_TIGER_TEST
            //this.OnMainRibbon_ProjectTestChangeImpl((new HashSet<E_DISPLAY_TESTRIBBON>() { E_DISPLAY_TESTRIBBON.E_HIDDEN_ALL}));

#else
            TestApplications();
#endif
            LoadInitialPage();

            App.Current.MainWindow = this;
            ((App)(App.Current)).CloseSplashWindow();

#if _TestStepUnit
            ///在teststep模式下启动test service
            /// 
            ///StartTestServiceUnderTestStepMode();
            ///在framework启动后再启动qtp和客户端
            /// 
            //MarsUtilities.StartQTPStarer("/StartQTPBackGround");
#endif

            AddWndProc();

            Logger.logEnd("RibbonWindow_Loaded");
        }

        private void AddWndProc()
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(MarsWndProc));            
        }

        private IntPtr MarsWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == SystemConstant.WM_MARS_TESTIS_DONE)
            {
                Logger.Info("MarsWndProc", "refresh storyboard is required");
                //update storyboard
                UpdateStoryBoard((int)wParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private  void UpdateStoryBoard(int storyBoardId)
        {
            try
            {
                string strError = "";

                if (!(dockManager.ActiveContent is DockPanel))
                {
                    strError = string.Format("No storyBoard is currently actived, ActiveContent is [{0}] which should be DockPanel.",
                        dockManager.ActiveContent.GetType().ToString());
                    return ;
                }
                List<StoryboardCombinedControl> lstStoryBoard = MarsObjectsAndChildrenHelper.GetChildList<StoryboardCombinedControl>(dockManager);

                if ((lstStoryBoard == null) || (lstStoryBoard.Count == 0)) return;

                foreach(var itm in lstStoryBoard)
                {
                    if (itm == null) continue;
                    if (itm.StoryboardId != storyBoardId) continue;
                    /// fire the refresh button event
                    /// 
                    if (itm.storyboardDocPanel.Children.Count <= 0) continue;
                    StoryboardEditControl sbEditr = itm.storyboardDocPanel.Children[0] as StoryboardEditControl;

                    if (sbEditr == null) continue;
                    if (sbEditr.refreshStoryBoard_Btn.Command!=null)
                    {
                        sbEditr.refreshStoryBoard_Btn.Command.Execute(null);
                        break;
                    }
                    //sbEditr.refreshStoryBoard_Btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            }
            catch (Exception e)
            {
                
            }
        }

        private void DoAssignApplicationToTC()
        {
            long projId = -1;
            if (!tvMars.SelectedItem.GetType().Name.Equals("MarsProjectTreeView"))
                return;
            projId = ((MarsProjectTreeView)tvMars.SelectedItem).ProjectId;
            string strProjName = ((MarsProjectTreeView)tvMars.SelectedItem).ProjectName;

            AssignApplicationToTestCases testCaseAssignMgr = new AssignApplicationToTestCases(projId, strProjName);
            testCaseAssignMgr.ShowDialog();
        }

        private void TreeContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = e.OriginalSource as MenuItem;
            string action = item.Header.ToString();
            string storyboadName = null;
            long? storyboardId = 0;
            string folderName;

            MarsStoryboardTreeView sbView = null;
            MarsProjectTreeView projView = null;
            MarsTestSuiteTreeView tsView = null;
            MarsTestCaseTreeView tcView = null;
            MarsDataSheetTreeView dsView = null;

            MarsFolderTreeView fView = null;
            SaveAsDialog inputDialog;
            string objectName = "",strError="";
            bool isOk = false;

            if (tvMars.SelectedItem != null)
            {

                if (tvMars.SelectedItem.GetType().Name.Equals("MarsTestCaseTreeView"))
                {
                    RefreshTreeForTestCaseId();
                }

                else if (tvMars.SelectedItem.GetType().Name.Equals("MarsStoryboardTreeView"))
                {
                    storyboadName = ((MarsStoryboardTreeView)(tvMars.SelectedItem)).StoryboardName;
                    storyboardId = ((MarsStoryboardTreeView)(tvMars.SelectedItem)).StoryboardId;
                    //Console.WriteLine("action = " + action + "storyboardName = " + storyboadName);
                }

                else if (tvMars.SelectedItem.GetType().Name.Equals("MarsFolderTreeView"))
                {
                    folderName = ((MarsFolderTreeView)(tvMars.SelectedItem)).FolderName;
                    Console.WriteLine("action = " + action + "folderName = " + folderName);
                    long projectId = ((MarsFolderTreeView)(tvMars.SelectedItem)).ProjectId;

                    StoryboardAddControl storyboardAddControl = new StoryboardAddControl(projectId);
#if tiger_dock
                    AddOneModelToDockPanel(storyboardAddControl);
#else
                    clearFormPanel();
                    FormPanel.Children.Add(storyboardAddControl);
#endif
                } 


                switch (tvMars.SelectedItem.GetType().Name)
                {
                    case "MarsStoryboardTreeView":
                        sbView = (MarsStoryboardTreeView)tvMars.SelectedItem;
                        fView = (MarsFolderTreeView)sbView.Parent;
                        break;

                    case "MarsProjectTreeView":
                        projView = (MarsProjectTreeView)tvMars.SelectedItem;
                        break;

                    case "MarsTestSuiteTreeView":
                        tsView = (MarsTestSuiteTreeView)tvMars.SelectedItem;
                        break;

                    case "MarsTestCaseTreeView":
                        tcView = (MarsTestCaseTreeView)tvMars.SelectedItem;
                        tsView = (MarsTestSuiteTreeView)tcView.Parent;
                        break;

                    case "MarsDataSheetTreeView":
                        dsView = (MarsDataSheetTreeView)tvMars.SelectedItem;
                        break;

                    default:
                        break;
                }


                switch (action)
                {
                    case "Assgin Applications To Test Cases...":
                        //添加其他的application到该project下的
                        DoAssignApplicationToTC();
                        return;
                    case "SaveAs Storyboard":
                        inputDialog = new SaveAsDialog("Please Enter Storyboard name:", sbView.StoryboardName);
                        objectName = "";
                        if (inputDialog.ShowDialog() == true)
                        {
                            objectName = inputDialog.Answer;
                            if (BoHelper.isStoryboardNameExist(currentDatabaseIdx, objectName, (long)sbView.ProjectId))
                            {
                                MessageBox.Show("Error: Storyboard " + objectName + " already exists in project  " + sbView.ProjectName, "SaveAs Error", MessageBoxButton.OK);
                            }
                            else
                            {
                                long id = StoryboardCache.currentSBColl.SaveAs(objectName);
                                MarsTreeView.BuildStoryboardFolderTree(fView);
                                LoadInitialStoryboard((long)sbView.ProjectId, id);
                            }
                        }


                        break;

                    case "Rename Storyboard":
                        if (sbView == null)
                        {
                            MessageBox.Show("Error: Storyboard " + " Please select storyboard first  " + sbView.ProjectName, "Rename Error", MessageBoxButton.OK);
                            break;
                        }
                        inputDialog = new SaveAsDialog("Please Enter Storyboard name:", sbView.StoryboardName);
                        objectName = "";
                        if (inputDialog.ShowDialog() == true)
                        {
                            objectName = inputDialog.Answer;
                            if (BoHelper.isStoryboardNameExist(currentDatabaseIdx, objectName, (long)sbView.ProjectId))
                            {
                                MessageBox.Show("Error: Storyboard " + objectName + " already exists in project  " + sbView.ProjectName, "Rename Error", MessageBoxButton.OK);
                            }
                            else
                            {
                                long id = (long)sbView.StoryboardId;
                                BoHelper.UpdateStoryboardName(currentDatabaseIdx, (long)sbView.StoryboardId, objectName);
                                StoryboardCache.currentSBColl.Title = "Storyboard: " + objectName;
                                LoadInitialStoryboard((long)sbView.ProjectId, id);
                            }
                        }

                        MarsTreeView.BuildStoryboardFolderTree(fView);
                        break;
                    case "Delete Storyboard":
                        MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                        if (messageBoxResult == MessageBoxResult.Yes)
                        {
                            StoryboardCache.currentSBColl.DeleteStoryboard(storyboardId);
                            MarsTreeView.BuildStoryboardFolderTree(fView);
                            clearFormPanel();
                        }

                        break;

                    case "Rename Project":
                        inputDialog = new SaveAsDialog("Please Enter Project name:", projView.ProjectName);
                        objectName = "";
                        if (inputDialog.ShowDialog() == true)
                        {
                            objectName = inputDialog.Answer;
                            if (BoHelper.isProjectNameExist(currentDatabaseIdx, objectName))
                            {
                                MessageBox.Show("Error: Project " + objectName + " already exists.", "SaveAs Error", MessageBoxButton.OK);
                            }
                            else
                            {
                                long id = (long)projView.ProjectId;
                                BoHelper.UpdateProjectName(currentDatabaseIdx, id, objectName);
                                projView.ProjectName = objectName;
                            }
                        }
                        break;
                    case "Export Project...":
                        string strTargetDir = "";
                        if (ExportProjectToXml(projView.ProjectId,ref strTargetDir,ref strError))
                        {
                            ViewModelBase.HintByMessageBox(strError);
                        }else
                        {
                            ViewModelBase.HintByMessageBox(string.Format("Can't Export Project [{0}], with Error:\r\n{1} ", projView.ProjectName,strError));
                        }
                        break;
                    case "Delete Project":
                        LoadAmendDeleteProject("Delete", MarsInvokeFrom.e_FromTreeView);
                        break;

                    case "SaveAs Project":
                        LoadControl("SaveAs Project");

                        break;

                    case "SaveAs Test Case":
                        LoadControl("SaveAs Test Case");
                        if (SaveAsViewModel.LastTestCase != -1)
                        {
                            LoadInitialTestCase(tcView.ProjectId, tcView.TestSuiteId, SaveAsViewModel.LastTestCase, SaveAsViewModel.LastDataSet);
                            SaveAsViewModel.LastTestCase = -1;
                        }
                        break;

                    case "Delete Test Case":
                        LoadControl("Delete Test Case");
                        if (tcView==null)
                        {
                            return;
                        }
                        if (SaveAsViewModel.LastTestCase != -1)
                        {
                            LoadInitialTestCase(tcView.ProjectId, tcView.TestSuiteId, SaveAsViewModel.LastTestCase, SaveAsViewModel.LastDataSet);
                            SaveAsViewModel.LastTestCase = -1;
                        }
                        break;

                    case "SaveAs Test Suite":
                        LoadControl("SaveAs Test Suite");

                        break;

                    case "Delete Test Suite":
                        LoadControl("Delete Test Suite");

                        break;

                    case "Rename Test Suite":
                        if (tsView == null)
                        {
                            MessageBox.Show("Please select test suite first.Error: Test Suite " + objectName + " already exists.", "SaveAs Error", MessageBoxButton.OK);
                            break;
                        }
                        inputDialog = new SaveAsDialog("Please Enter Test Suite name:", tsView.TestSuiteName);
                        objectName = "";
                        if (inputDialog.ShowDialog() == true)
                        {
                            objectName = inputDialog.Answer;
                            if (BoHelper.isTestSuiteNameExist(currentDatabaseIdx, objectName))
                            {
                                MessageBox.Show("Error: Test Suite " + objectName + " already exists.", "SaveAs Error", MessageBoxButton.OK);
                            }
                            else
                            {
                                long id = (long)tsView.TestSuiteId;
                                BoHelper.UpdateTestSuiteName(currentDatabaseIdx, id, objectName);
                                tsView.TestSuiteName = objectName;
                            }
                        }
                        break;

                    case "Rename Test Case":
                        inputDialog = new SaveAsDialog("Please Enter Test Case name:", tcView.TestCaseName);
                        objectName = "";
                        if (inputDialog.ShowDialog() == true)
                        {
                            objectName = inputDialog.Answer;
                            if (BoHelper.isTestCaseNameExist(currentDatabaseIdx, objectName))
                            {
                                MessageBox.Show("Error: Test Case " + objectName + " already exists.", "SaveAs Error", MessageBoxButton.OK);
                            }
                            else
                            {
                                long id = (long)tcView.TestCaseId;
                                BoHelper.UpdateTestCaseName(currentDatabaseIdx, id, objectName);
                                tcView.TestCaseName = objectName;
                            }
                        }
                        break;

                    case "Rename DataSet":
                        SaveAsWithDescriptionDialog inputDialog2 = new SaveAsWithDescriptionDialog("Enter DataSet name:", "Enter DataSet Description", dsView.DataSheetName, dsView.DataSheetDescription);
                        string name = "";
                        string description = "";


                        if (inputDialog2.ShowDialog() == true)
                        {
                            name = inputDialog2.Answer1;
                            description = inputDialog2.Answer2;
                            long id = (long)dsView.DataSheetId;

                            if (BoHelper.isDataSetNameExist(MarsMainWindow.CurrentDatabaseIdx, objectName, id))
                            {
                                MessageBox.Show("Error: DataSet " + objectName + " already exists.", "SaveAs Error", MessageBoxButton.OK);
                            }
                            else
                            {

                                if (!BoHelper.UpdateDataSetName(currentDatabaseIdx, id, name, description,ref strError))
                                {
                                    MessageBox.Show(strError, "SaveAs Error", MessageBoxButton.OK);
                                    return;
                                }
                                dsView.DataSheetName = name;
                            }
                        }
                        break;

                    case "Compare Storyboard":

                        Console.WriteLine("Compare Storyboard");
                        LoadControl("Storyboard Compare");
                        break;
#region base line data
                    case "Base Line Data Setting":
                        Logger.Info("TreeContextMenuItem_Click", string.Format("action:[{0}]", action));
                        LoadControl(action);
                        break;
#endregion base line data
                    case "SaveAs DataSet":
                        DoSaveAsDataset();
                        break;
                    case "Refresh Data":
                        if (tvMars.SelectedItem.GetType().Name.Equals("MarsTestCaseTreeView"))
                        {
                            MarsTestCaseTreeView view = (MarsTestCaseTreeView)tvMars.SelectedItem;
                            BoHelper.GetMarsEntitiesInstance(true,currentDatabaseIdx);
                            MarsTreeView.BuildDataSheetTree(view, B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(currentDatabaseIdx, ref strError, ref isOk));
                        }
                        break;
                    case "Delete DataSet":
                        DoDeleteDataSet();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                MessageBox.Show("Please active Storyboard/Test Case or other Modulars. ", "Hint");
            }

        }
        private void DoSaveAsDataset()
        {
            bool isOk = false;
            if ((tvMars.SelectedItem==null)||(!(tvMars.SelectedItem is MarsDataSheetTreeView)))
            {
                ViewModelBase.HintByMessageBox("Please select a data set from the left tree view firstly.", "true");
                return;
            }
            try
            {

                /// steps:
                /// 1, popup a dialog, 
                //Logger.Info("DoSaveAsDataset",string.Format());
                SaveAsDialog inputDialog = new SaveAsDialog("Save Data Set As:", "", "Save Data Set As", "No DataSet Name input. do you want to Cancel Dataset SaveAs?");
                if (!(inputDialog.ShowDialog() ?? false)) return;
                string strNewDataSetName = inputDialog.txtAnswer.Text;
                /// 2, save to database
                /// 
                MarsDataSheetTreeView objSrcDataSet = (MarsDataSheetTreeView)tvMars.SelectedItem;
                B_T_TEST_DATA_SUMMARYDTO objBo = new B_T_TEST_DATA_SUMMARYDTO();
                string strError = "";
                if (!objBo.SaveDataSetAs(
                    currentDatabaseIdx,
                    objSrcDataSet.DataSheetId, objSrcDataSet.TestCaseId, objSrcDataSet.DataSheetName, strNewDataSetName, ref strError))
                {
                    ViewModelBase.HintByMessageBox(string.Format("Can't save [{0}] to [{1}], with Error:\r\n[{2}]", objSrcDataSet.DataSheetName, strNewDataSetName, strError), "Hint");
                    return;
                }
                /// refresh the tree
                /// 
                MarsTreeView.BuildDataSheetTree((MarsTestCaseTreeView)objSrcDataSet.Parent, 
                    B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(currentDatabaseIdx, ref strError, ref isOk));
                ViewModelBase.HintByMessageBox(string.Format("Dataset [{0}] is created.", strNewDataSetName), "Hint") ;
            }
            catch (Exception e)
            {
                Logger.Error("DoSaveAsDataset", string.Format("Exception:[{0}] StackTrace:[{1}]",e.Message,e.StackTrace));
            }


        }

        private void DoDeleteDataSet()
        {
            if (!(tvMars.SelectedItem is MarsDataSheetTreeView))
            {
                ViewModelBase.HintByMessageBox("Please select a data set node first. ", "Hint");
                return;
            }
            string strDataSetName = "";
            MarsDataSheetTreeView objCurrentDataNode = (MarsDataSheetTreeView)tvMars.SelectedItem;
            if (objCurrentDataNode == null) return;
            Logger.Info("DoDeleteDataSet",string.Format("Try to delete Data node [{0}], Name:[{1}]", objCurrentDataNode.DataSheetId, objCurrentDataNode.DataSheetName));

            if (!ViewModelBase.QuestionByMessageBox(string.Format("Are you sure to delete this Data Set? \r\nData Set Name:[{0}]\r\n\r\nNote:\r\nOnly Data set without runtime testing data will be deleted. ", objCurrentDataNode.DataSheetName),"Warning"))
            {
                return;
            }
            B_T_TEST_DATA_SUMMARYDTO boDataSummary = new B_T_TEST_DATA_SUMMARYDTO();
            string strError = "";bool isOk = false;
            if (!boDataSummary.DeleteDataSetById(
                currentDatabaseIdx,
                objCurrentDataNode.DataSheetId, strDataSetName=objCurrentDataNode.DataSheetName, objCurrentDataNode.TestCaseId, ref strError))
            {
                ViewModelBase.HintByMessageBox(string.Format("Can't Delete [{0}] to [{1}], with Error:\r\n[{2}]", objCurrentDataNode.DataSheetName, objCurrentDataNode, strError), "Hint");
                return;
            }
            MarsTreeView.BuildDataSheetTree((MarsTestCaseTreeView)objCurrentDataNode.Parent, B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(
                currentDatabaseIdx,
                ref strError, ref isOk));
            SelectATreeNode(ref tvMars, (MarsTestCaseTreeView)objCurrentDataNode.Parent);
            ViewModelBase.HintByMessageBox(string.Format("Dataset [{0}] is deleted.", strDataSetName) );
        }

        private static void SelectATreeNode(ref TreeView objTree, object objNode)
        {
            try
            {
                DependencyObject objD = objTree.ItemContainerGenerator.ContainerFromItem(objNode);
                MethodInfo selectMethod = typeof(TreeViewItem).GetMethod("Select", BindingFlags.NonPublic | BindingFlags.Instance);
                selectMethod.Invoke(objD, new object[] { true });
            }
            catch (Exception e)
            {
                Logger.Error("SelectATreeNode", e.Message,e );
            }
            
        }

        // AF
        private void ComboxExecutionMode_SelectionChanged(object sender,
                              RoutedPropertyChangedEventArgs<object> e)
        {
            RibbonGallery source = e.OriginalSource as RibbonGallery;
            if (source.SelectedValue.Equals("Baseline"))
                isBaseLineTest = true;
            else
                isBaseLineTest = false;
            Console.Out.WriteLine("ComboxExecutionMode_SelectionChanged");
        }

        private bool IsExecutionModeSelected()
        {
            bool rc = false;
            if (ComboxExecutionMode.Text != null && 
                ComboxExecutionMode.Text.Equals("Baseline") || 
                ComboxExecutionMode.Text.Equals("Compare"))
                rc = true;

            return rc;
        }
      // AF END


        private MarsKeyValues<string, string> _selectecTestTargetApplication;
        public MarsKeyValues<string, string> SelectecTestTargetApplication
        {
            get { return this._selectecTestTargetApplication; }
            set
            {
                _selectecTestTargetApplication = value;
                if (value != null)
                    AppConfigReader.WriteCurrentApplicationShortName(value.MValue);
                RaisePropertyChanged("SelectecTestTargetApplication");
            }
        }

        private ObservableCollection<MarsKeyValues<int, string>> _listTestApplications;
        public ObservableCollection<MarsKeyValues<int, string>> ListTestApplications
        {
            get { return this._listTestApplications; }
            set { _listTestApplications = value; RaisePropertyChanged("ListTestApplications"); }
        }

        public void OnRibbonTestApplicationsReadyImpl(MarsKeyValues<int, string>[] arrListApplicationsInfo)
        {
            if (!this.ComboxTargetApplication.IsVisible) return;
            ObservableCollection<MarsKeyValues<int, string>> lstApps = new ObservableCollection<MarsKeyValues<int, string>>();

            //this.SelectecTestTargetApplication = lstApps[0];
            this.ComboxTargetApplication.DisplayMemberPath = "MValue";
            //this.ComboxTargetApplication.DataContext = lstApps;
            //this.ComboxTargetApplication.SetBinding(RibbonComboBox.ItemsSourceProperty, new Binding());
            //this.ComboxTargetApplication.SetBinding(RibbonComboBox.item, new Binding("SelectecTestTargetApplication"));
            //this.ComboxTargetApplication.ItemsSource = lstApps;

            foreach (MarsKeyValues<int, string> objKeyApps in arrListApplicationsInfo)
            {
                lstApps.Add(objKeyApps);
            }
            ListTestApplications = lstApps;
        }
#if _TIGER_TEST
        private void TestApplications()
        {
            this.OnRibbonTestApplicationsReadyImpl(new MarsKeyValues<int, string>[] {
                new MarsKeyValues<int, string>(1,"
                
                60"),
                new MarsKeyValues<int, string>(2,"Summit57"),
            });
        }
#endif

        private void RefreshRibbonControls(string controlName)
        {
            //Logger.Info("RefreshRi", string.Format("ControlName:{0}", controlName));
            HashSet<E_DISPLAY_TESTRIBBON> hsHiddenTest = new HashSet<E_DISPLAY_TESTRIBBON>();
            hsHiddenTest.Add(E_DISPLAY_TESTRIBBON.E_HIDDEN_ALL);
            OnMainRibbon_ProjectTestChangeImpl(hsHiddenTest);
        }

        private void BindData2TestAvailableApps(BindingBase objBind)
        {
            //this.ComboxTargetApplication.SetBinding(RibbonComboBox.ItemsSourceProperty, objBind);
            this.SetBinding(TestTargetApplicationsProperty, objBind);
        }
        private void BindData2UnAvailableApps(BindingBase objBind)
        {
            this.SetBinding(UninstalledApplicationProperty, objBind);
        }
        public static readonly DependencyProperty TestTargetApplicationsProperty =
            DependencyProperty.Register("TestTargetApplications", typeof(ObservableCollection<MarsKeyValues<string, string>>),
                typeof(MarsMainWindow), null);
        public ObservableCollection<MarsKeyValues<string, string>> TestTargetApplications
        {
            get { return (ObservableCollection<MarsKeyValues<string, string>>)GetValue(TestTargetApplicationsProperty); }
            set { SetValue(TestTargetApplicationsProperty, value); }
        }
        public static readonly DependencyProperty UninstalledApplicationProperty =
            DependencyProperty.Register("UninstalledApplication", typeof(ObservableCollection<MarsKeyValues<string, string>>),
                typeof(MarsMainWindow), null);
        public ObservableCollection<MarsKeyValues<string, string>> UninstalledApplication
        {
            get { return (ObservableCollection<MarsKeyValues<string, string>>)GetValue(UninstalledApplicationProperty); }
            set
            {
                SetValue(UninstalledApplicationProperty, value);
                RaisePropertyChanged("UninstalledApplication");
                HintDataNoApplicationsInstalled(value);
            }
        }

        private void HintDataNoApplicationsInstalled(ObservableCollection<MarsKeyValues<string, string>> lstApps)
        {
            if (lstApps == null) return;
            if (lstApps.Count == 0) return;
            string strApps = "";
            try
            {
                foreach (MarsKeyValues<string, string> oneApp in lstApps)
                {
                    if (string.IsNullOrEmpty(strApps))
                        strApps = oneApp.MValue;
                    else
                        strApps = string.Format("{0}\r\n{1}", strApps, oneApp.MValue);
                }
                if (string.IsNullOrEmpty(strApps)) return;
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new Action(delegate ()
                    {
                        MessageBox.Show(string.Format("No such applications installed on this machine, \r\nTest on those appliations will be ignored.\r\nApps:{0}", strApps), "Hint");
                    }
                    )
                    );
            }
            catch (Exception)
            {

                throw;
            }
        }

        private StoryboardEditControl objTargetStoryBoard = null;

        private bool _isBaseLineTest;
        public bool isBaseLineTest
        {
            get
            {
                return _isBaseLineTest;
            }
            set
            {
                _isBaseLineTest = value;
                RaisePropertyChanged("isBaseLineTest");

                MarsTestFrameMain.ChangeBaseLineValue(_isBaseLineTest);
            }
        }

        private bool _isContinueLatestTest = Config_GetValueAsBool(CNST_APPSETTING_ISCONTINUETOTEST, false);

        public bool IsContinueLatestTest
        {
            get
            {
                return _isContinueLatestTest;
            }

            set
            {
                _isContinueLatestTest = value;
                Config_SetValueAsBool(CNST_APPSETTING_ISCONTINUETOTEST, _isContinueLatestTest);
                RaisePropertyChanged("IsContinueLatestTest");
            }
        }

        private bool _IsAutoFixDepends;
        public bool IsAutoFixDepends
        {
            get
            {
                return _IsAutoFixDepends;
            }
            set
            {
                string strError = "";
                if (value)
                {
#if tiger_dock
                   
                    if (!IsActiveContentAStoryBoard(ref strError))
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Can't check storyboard with error:\r\n{0}", strError), "Hint");
                        return;
                    }
                    if (!(dockManager.ActiveContent is DockPanel))
                    {
                        strError = string.Format("No storyBoard is currently actived, ActiveContent is [{0}] which should be DockPanel.",
                            dockManager.ActiveContent.GetType().ToString());
                        return;
                    }

                    StoryboardCombinedControl objStoryBoard = MarsObjectsAndChildrenHelper.GetChildOfType<StoryboardCombinedControl>((UIElement)(dockManager.ActiveContent));

                    if (objStoryBoard == null)
                    {
                        strError = string.Format("No storyBoard is currently actived, ActiveContent is [{0}].",
                            dockManager.ActiveContent.GetType().ToString());
                        return;
                    }
                    StoryboardEditControl storyboardControl = objStoryBoard.storyboardDocPanel.Children[0] as StoryboardEditControl;
                    if (storyboardControl == null) return;
                    StoryboardColl storyboardData = storyboardControl.DataContext as StoryboardColl;
                    if (storyboardData == null) return;

                    storyboardData.ChangeActionsByDepends();
#endif
                }
                _IsAutoFixDepends = value;
                RaisePropertyChanged("IsAutoFixDepends");
                
            }
        }


        private bool _isIgnoreTestCaseError;
        public bool IsIgnoreTestCaseError
        {
            get
            {
                return _isIgnoreTestCaseError;
            }

            set
            {
                _isIgnoreTestCaseError = value;
                RaisePropertyChanged("IsIgnoreTestCaseError");
            }
        }


#if tiger_dock

        private bool IsActiveContent_Batch(ref string strError, ref StoryboardBatch targetObj)
        {
            Logger.logBegin("IsActiveContent_T");
            if (dockManager.ActiveContent == null)
            {
                Logger.Warnning("IsActiveContent_T", strError = "No Model is loaded.");
                return false;
            }
            if (!(dockManager.ActiveContent is DockPanel))
            {
                strError = string.Format("No Test Case is currently actived, ActiveContent is [{0}] which should be DockPanel.",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }
            targetObj = MarsObjectsAndChildrenHelper.GetChildOfType< StoryboardBatch > ((UIElement)(dockManager.ActiveContent));
            if (targetObj==null)
            {
                strError = string.Format("No StoryboardBatch is currently actived, ActiveContent is [{0}].",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }
            return true;
        }

        private bool IsActiveContentProjectEdit(ref string strError, ref MarsProjectBaseControlView objProjEdit)
        {
            Logger.logBegin("IsActiveContentProjectEdit");
            if(dockManager.ActiveContent==null)
            {
                Logger.Warnning("IsActiveContentProjectEdit", strError="No Model is loaded.");
                return false;
            }
            if (!(dockManager.ActiveContent is DockPanel))
            {
                strError = string.Format("No Test Case is currently actived, ActiveContent is [{0}] which should be DockPanel.",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }
            objProjEdit = MarsObjectsAndChildrenHelper.GetChildOfType<MarsProjectBaseControlView>((UIElement)(dockManager.ActiveContent));
            if (objProjEdit == null)
            {
                strError = string.Format("No Project is currently actived, ActiveContent is [{0}].",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }
            return true;
        }

        private bool IsActiveContentTestcase(ref string strError,  ref TestCaseEdit tcEdtCntrol)
        {
            if (!(dockManager.ActiveContent is DockPanel))
            {
                strError = string.Format("No Test Case is currently actived, ActiveContent is [{0}] which should be DockPanel.",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }

            tcEdtCntrol = MarsObjectsAndChildrenHelper.GetChildOfType<TestCaseEdit>((UIElement)(dockManager.ActiveContent));
            if(tcEdtCntrol == null)
            {
                strError = string.Format("No TestCase is currently actived, ActiveContent is [{0}].",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }            
            return true;
        }

        private bool IsActiveContentTestSuite(ref string strError, ref TestSuiteEditControl tsCntrl)
        {
            if (!(dockManager.ActiveContent is DockPanel))
            {
                strError = string.Format("No Test Case is currently actived, ActiveContent is [{0}] which should be DockPanel.",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }

            tsCntrl = MarsObjectsAndChildrenHelper.GetChildOfType<TestSuiteEditControl>((UIElement)(dockManager.ActiveContent));
            if (tsCntrl == null)
            {
                strError = string.Format("No Testsuite is currently actived, ActiveContent is [{0}].",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }
            return true;
        }

        private bool IsActiveContentAStoryBoard(ref string strError,bool isToReattach=true)
        {
            //if (dockPanelHost.Items.Count == 1)
            //    dockingManager.ActiveContent = (DockableContent)dockPanelHost.Items[0];
            if (dockManager.ActiveContent == null)
            {
                strError = "No storyBoard is currently actived, ActiveContent is null.";
                return false;
            }

            if (!(dockManager.ActiveContent is DockPanel))
            {
                strError = string.Format("No storyBoard is currently actived, ActiveContent is [{0}] which should be DockPanel.",
                    dockManager.ActiveContent.GetType().ToString());
                return false ;
            }
            
            StoryboardCombinedControl objStoryBoard = MarsObjectsAndChildrenHelper.GetChildOfType<StoryboardCombinedControl>((UIElement)(dockManager.ActiveContent));

            if (objStoryBoard==null)
            {
                strError = string.Format("No storyBoard is currently actived, ActiveContent is [{0}].",
                    dockManager.ActiveContent.GetType().ToString());
                return false;
            }
            if(isToReattach)
                ReAttachStoryboard2FrameWork(objStoryBoard);
            return true;
        }
#endif
        /// <summary>
        /// 其实是启动客户端部分，重新启动qtp在查询模式
        /// </summary>
        private void OnRestartFramework()
        {
            Logger.logBegin("OnRestartFramework");
            MarsUtilities.StartQTPStarer("-RestartQTPClient");
            Logger.logEnd("OnRestartFramework");
        }
        private void OnRunCurrentStoryBoard(object sender, RoutedEventArgs e)
        {
#if tiger_dock
            string strError = "";
            if (!IsActiveContentAStoryBoard(ref strError))
            {
                ViewModelBase.HintByMessageBox(string.Format("Can't run storyboard with error:\r\n{0}", strError), "Hint");
                return;
            }
#endif
#if _NOQTP
            RunCurrentStoryBoardWithMarsEngine();
#else
            RunCurrentStoryBoard();
#endif
        }

        private bool IsApplication64bit(string appKey)
        {
            int iAppId;
            if (!int.TryParse(appKey,out iAppId))
            {
                Logger.Error("IsApplication64bit", string.Format("application key is not a int:{0}", appKey));
                return true;
            }
            List<B_REGISTERED_APPS> lstApp = (new B_REGISTERED_APPS()).GetApplication(currentDatabaseIdx);
            var a = (from q in lstApp
                    where q.APPLICATION_ID == iAppId
                    select q).FirstOrDefault();
            if (a == null)
            {
                Logger.Error("IsApplication64bit", string.Format("no such application id from cache:[{0}]", appKey));
                return true;
            }
            string strExtra = a.EXTRAREQUIREMENT;
            strExtra = strExtra == null ? "" : strExtra.ToUpper();
            if (strExtra.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WINX86) >= 0)
            {
                Logger.Info("IsApplication64bit", string.Format("app [{0}] is x86 app", appKey));
                return false;
            }

            Process[] pApps = Process.GetProcessesByName(a.PROCESS_IDENTIFIER);
            if (pApps == null || pApps.Length <= 0) return true;
            bool Wow64Process = false;
            if (Wow64Process = MarsWindowsAPIsExtend.IsProcess32(pApps[0].Handle))
            {
                return false;
            }

            return true;
        }

        private IntPtr currentMainWindowHandle;
        private static bool IsMessageWarrningShew= false ;

        private void RunCurrentStoryBoardWithMarsEngine(string applicationName = null, long applicationId = 0)
        {
            bool isTargetApplicationReady = CheckWhetherApplicationIsSelected();
            if (applicationName != null)
                isTargetApplicationReady = true;
            if (!isTargetApplicationReady)
            {
                MarsSystemUtilty.ShowSpecialMessage(this, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_NO_APPLICATION_IS_SELECTED));
                return;
            }
            bool isStoryboardActived = CheckStoryBoardActiveStatus(ref objTargetStoryBoard);
            if ((!isStoryboardActived) || (objTargetStoryBoard == null))
            {
                MarsSystemUtilty.ShowSpecialMessage(this, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_NO_STORYBOARD_MODULA_ACTIVE));
                return;
            }
            bool isShowLicenseWarnning = false;
            string strMessageLic = "";
            if (!checkLicense(ref isShowLicenseWarnning, ref strMessageLic))
            {
                MessageBox.Show("Mars license has expired.\r\nPlease contact Marquis Business and Technology Solutions", "Message",MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isX64 = IsApplication64bit(SelectecTestTargetApplication.MKey);
            Logger.Info("\t", string.Format("target application [{0}] is [{1}]", SelectecTestTargetApplication.MKey,
                isX64?"64bit":"32bit"));
            if (isShowLicenseWarnning&&(!IsMessageWarrningShew))
            {
                MessageBox.Show(strMessageLic, "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsMessageWarrningShew = true;
            }
            
            this.WindowState = WindowState.Minimized;
            string strError = "";
            //直接调用console application模式
            new Thread(new ThreadStart(new Action(() => {
                //admin -S “TigerTest” 138394 -App 2 -Mode NonBase -IsContinue true -IsIgnoreError true
                if (!MarsUtilities.StartMarsEngine(
                    WCFXmlCfgMgr.CurrentLoginUser,
                    SelectecTestTargetApplication.MKey,
                    objTargetStoryBoard.CurrentStoryBoardID,
                    objTargetStoryBoard._CurrentStoryboardName,
                    isBaseLineTest,
                    IsContinueLatestTest,
                    IsIgnoreTestCaseError,
                    isX64, 
                    ref strError
                    ))
                {
                    Logger.Error("\t", strError);
                    Dispatcher.Invoke(()=> {
                    });
                }
            }))).Start();
            
        }

        /// <summary>
        /// Run current story board on selected application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunCurrentStoryBoard(string applicationName = null, long applicationId = 0, bool isBaseLineFlag = false, bool continueToTestFlag = false, bool ignoreRuntimeErrorFlag = false)
        {
            /// for test with constant data
            /// 
#if _TIGER_TEST
            StartTest("Summit6.0", "1", "DASH_Marquis Project", "45");
#else
            //Logger.Info("OnRunCurrentStoryBoard", "Storyboard to test");
            /// steps:
            /// 1, check whether application is selected
            /// 2, stop and start the services, 
            /// 
            // 1, check whether application is selected
            bool isTargetApplicationReady = CheckWhetherApplicationIsSelected();
            bool isExecModeSelected = IsExecutionModeSelected();

            if (isExecModeSelected == false)
            {
                MarsSystemUtilty.ShowSpecialMessage(this, "Execution mode is not selected! ");
                return;
            }


            if (applicationName != null)
                isTargetApplicationReady = true;
            if (!isTargetApplicationReady)
            {
                MarsSystemUtilty.ShowSpecialMessage(this, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_NO_APPLICATION_IS_SELECTED));
                return;
            }
            /// Make sure the current main Working Section is Storyboard, and actived
            /// 

            bool isStoryboardActived = CheckStoryBoardActiveStatus(ref objTargetStoryBoard);
            if ((!isStoryboardActived) || (objTargetStoryBoard == null))
            {
                MarsSystemUtilty.ShowSpecialMessage(this, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_NO_STORYBOARD_MODULA_ACTIVE));
                return;
            }

            // 2, stop and start the services, 
            /// check license
            ///             
            bool isMessageLicenseWarnning = false;
            string strLicenseWarning = "";

            if (!checkLicense(ref isMessageLicenseWarnning,ref strLicenseWarning))
            {
                MessageBox.Show("Mars license has expired.\r\nPlease contact Marquis Business and Technology Solutions.", "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (isMessageLicenseWarnning && (!IsMessageWarrningShew))
            {
                MessageBox.Show(strLicenseWarning, "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsMessageWarrningShew = true;
            }


            this.WindowState = WindowState.Minimized;
            try
            {
                //this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate () { objTargetStoryBoard.TestCurrentStoryBoard(); }));
                _CurrentProjectName = objTargetStoryBoard._CurrentProjectName;
                _CurrentStoryboardName = objTargetStoryBoard._CurrentStoryboardName;
                _CurrentProjectId = objTargetStoryBoard._CurrentProjectId;
                _CurrentStoryboardID = objTargetStoryBoard.CurrentStoryBoardID ?? -1;


                string appShortName;
                string appId;

                if (applicationName == null)
                {
                    appShortName = objTargetStoryBoard.CurrentTestApplicationId.MValue;
                    appId = objTargetStoryBoard.CurrentTestApplicationId.MKey;
                }
                else
                {
                    appShortName = applicationName;
                    appId = "" + applicationId;
                }

                bool isCntn = this.continueLatestTest.IsChecked ?? false;
                bool isIgnoreError = this.chckbxIgnoreTestCaseError.IsChecked ?? false;

                currentMainWindowHandle = (new WindowInteropHelper(App.Current.MainWindow)).Handle;

                Thread testThrd;
                testThrd = new Thread(new ThreadStart(
                    delegate
                    {
                        StartTest(isIgnoreError, isCntn, appShortName, appId, _CurrentStoryboardName, _CurrentStoryboardID + "");
                    }));
                testThrd.Priority = ThreadPriority.AboveNormal;
                testThrd.Start();
            }
            catch (TargetInvocationException ex)
            {
                Logger.Error("OnRunCurrentStoryBoard", string.Format("Exception:[{0}]", ex.Message), ex);
            }
            catch (Exception e1)
            {
                Logger.Error("OnRunCurrentStoryBoard", string.Format("Exception:[{0}]", e1.Message), e1);
            }

#endif
        }

        private bool isRefreshDataReportTimeNow;
        public bool IsRefreshDataReportTimeNow
        {
            get { return isRefreshDataReportTimeNow; }
            set
            {

                isRefreshDataReportTimeNow = value;
                if (isRefreshDataReportTimeNow)
                {
                    RaisePropertyChanged("IsRefreshDataReportTimeNow");
                    DealWithHistoryDataRefreshChange();
                }

            }
        }
        private void DeleteHisTestData(object sender, RoutedEventArgs e)
        {
            StoryboardEditControl tmpStoryboard = null;
            bool isStoryboardActived = CheckStoryBoardActiveStatus(ref tmpStoryboard);
            if ((!isStoryboardActived) || (tmpStoryboard == null))
            {
                MarsSystemUtilty.ShowSpecialMessage(this, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_NO_STORYBOARD_MODULA_ACTIVE));
                return;
            }
            string strError = "";
            if (tmpStoryboard.DelTestHisData(this.checkDelBaseline.IsChecked, this.checkDelNonBaseLine.IsChecked, ref strError) < 0)
            {
                MessageBox.Show(string.Format(@"Error, Can't delete data.ErrorMessage:\r\n{0}", strError), "Error");
            }
            else
            {
                IsRefreshDataReportTimeNow = true;
                MessageBox.Show(@"Deleted all Data records!", "Hint");
                ///refresh the storyboard
                /// 
                OnTestCaseIsDoneImpl(-1);
            }


        }

#if _TestStepUnit
        internal bool OnRequestStartWCFSvcImpl(int iMode, ref string strError)
        {
            Logger.logBegin("OnRequestStartWCFSvcImpl", string.Format("Try to start WCF server with mode:[{0}]", iMode));
            try
            {

                if (MarsTestingFrame != null)
                {
                    MarsTestingFrame.StopService(iMode);
                }
                if (iMode == 2)
                {
                    MarsTestingFrame.StartServiceWithMode(FrameWorkStartMode.FWSM_STEPMODE,null, ref strError);
                    StartTestServiceUnderTestStepMode();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("OnRequestStartWCFSvcImpl",strError = string.Format("Exception:[{0}],stackTrace:[{1}]",e.Message,e.StackTrace));
                return false;
            }
        }
        internal bool onRequestWCFServiceStatusImpl()
        {
            if (MarsTestingFrame == null) return false;
            if (MarsTestingFrame.CurrentSvcStatus == null) return false;
            if (MarsTestingFrame.CurrentSvcStatus == System.ServiceModel.CommunicationState.Opened) return true;
            return false;
        }

        internal void StartTestServiceUnderTestStepMode()
        {
            ///算法：
            /// 1，copy当前应用相关的dll到current目录
            /// 2，启动qtp设置为等待命令模式
            /// 

            /// 1，copy当前应用相关的dll到current目录
            /// 
            string strError="";
            bool isContinue = SwitchTheCurrentAddins(ref strError);
            if (!isContinue)
            {
                Logger.Error("StartTestServiceUnderTestStepMode", strError);
                ///有可能待测试系统已经启动，故而直接启动qtp等
                //return;
            }
            /// 启动framework wcf 服务
            /// 
            if (MarsTestingFrame != null)
            {
                if (!MarsTestingFrame.IsFrameworkRunning)
                    MarsTestingFrame.StartServiceWithMode(FrameWorkStartMode.FWSM_STEPMODE, null, ref strError);
            }
            /// 2，启动qtp设置为等待命令模式
            isContinue = StartQTPWithCmmdMode(ref strError);
        }

        private bool SwitchTheCurrentAddins(ref string strError)
        {
            ConfigTestApplication objcurrentApp = AppConfigReader.GetDefaultApplicationInfo();
            if (objcurrentApp==null)
            {
                strError = "No default application data setting in configuration file";
                return false; 
            }
            string strAppName = objcurrentApp.AppName;
            int iError = TargetApplicationsManagement.SwitchAddinsFilesByShortName(strAppName, ref strError);
            return iError == 1;
        }

        private bool StartQTPWithCmmdMode(ref string strError)
        {

            MarsUtilities.StartQTPStarer("-TestStep");
            //(new Thread(new ThreadStart(delegate ()
            //{
            //    ProcessStartInfo StartInfo = new ProcessStartInfo();
            //    StartInfo.FileName = @".\QtpStarter.exe";
            //    StartInfo.Arguments = "-TestStep";
            //    StartInfo.Domain = "NewMarsDomain";
            //    Process objNewProce = new Process();                
            //    objNewProce.StartInfo = StartInfo;                
            //    objNewProce.Start();                
            //}))).Start();

            return true;
        }

#endif

        private string _CurrentProjectName;
        private string _CurrentStoryboardName;
        private long _CurrentProjectId;
        private long _CurrentStoryboardID;
        private static MarsTestFrameMain MarsTestingFrame = null;// new MarsTestFrameMain();
        //private string _storyBoardName;

        private void InitTestingFrame()
        {
            MarsTestingFrame = new MarsTestFrameMain(currentDatabaseIdx);
            MarsTestingFrame.AutoGen_CurrentPegInfoHandler = this.AutoGen_CurrentPegInfoImpl;
            MarsTestingFrame.AutoGen_GenStepHandler = this.AutoGen_GenStepImpl;

            MarsTestingFrame.onTestResultMessageArrivedHandler = this.MessageFromClientArriveImpl;
        }

        private void MessageFromClientArriveImpl(string strInformation, string strHint)
        {
            ViewModelBase.HintByMessageBox(strInformation, strHint);
        }

        internal void StartTest(bool bIgnoreError, bool bContinueToTest, string strApplicationShortName, string strApplicationId, string strCurrentStoryBoardName, string strStoryBoardId, bool isAutoGen = false)
        {
            string strErrorInfo = "";

            if (strApplicationShortName != null)
            {
                int iError = TargetApplicationsManagement.SwitchAddinsFilesByShortName(strApplicationShortName, ref strErrorInfo);
                if (iError != 1)
                {
                    //this.Dispatcher.InvokeAsync(new Action(delegate ()
                    //{
                    //    MessageBox.Show(string.Format(@"Can't switch to sepecial test target application :[{0}], with ErrorInfo:[{1}]", strApplicationShortName, strErrorInfo));
                    //}), DispatcherPriority.Background);

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                        new Action(delegate ()
                        {
                            MessageBox.Show(string.Format(@"Can't switch to sepecial test target application :[{0}], with ErrorInfo:[{1}]", strApplicationShortName, strErrorInfo));
                        }));
                    //return;
                }
            }

            //Logger.logBegin("StartTest");
            if (MarsTestingFrame == null)
            {
                MarsTestingFrame = new MarsTestFrameMain(currentDatabaseIdx);
                MarsTestingFrame.AutoGen_CurrentPegInfoHandler = this.AutoGen_CurrentPegInfoImpl;
                MarsTestingFrame.AutoGen_GenStepHandler = this.AutoGen_GenStepImpl;
            }


#if _Datafrom_Database
            MarsTestingFrame.isContinueToTestMode = bContinueToTest;
            MarsTestingFrame.IsIgnoreError = bIgnoreError;
#endif
            MarsTestingFrame.StopService();
            MarsTestingFrame.CurrentTestProjectName = strCurrentStoryBoardName;
            MarsTestingFrame.CurrentTestApplicationShortName = strApplicationShortName;
            MarsTestingFrame.Gui_TestCaseFinishedCallBackHandler = this.OnTestCaseIsDoneImpl;

            MarsTestingFrame.SetBatchMode(IsRunningInBatchMode);

            if (!isAutoGen)
                MarsTestingFrame.RunTestBatchFileByThread(strCurrentStoryBoardName, FrameWorkStartMode.FWSM_Normal, strStoryBoardId, strApplicationId, _isBaseLineTest);
            else
                MarsTestingFrame.RunTestBatchFileByThread(strCurrentStoryBoardName, FrameWorkStartMode.FWSM_AUTOGEN_SCRIPTS, strStoryBoardId, strApplicationId, _isBaseLineTest);

#if _TestStepUnit
            MarsTestingFrame.current_FrameworkSvsMode = MarsTestFrame.CommuniteServer.MarsFrameWorkServicesMode._svcmode_Default;
            /// 在增加一个命令前 需要停止qtp，以免qtp线程从取走数据StopQTPFrameWorkThread
            /// 
            string strError = "";
            
            if (!QTPManagement.StopQTPFrameWorkThread(ref strError,WCFXmlCfgMgr.CurrentLoginUser))
            {
                TigerMarsUtil.ShowWindow(currentMainWindowHandle, TigerMarsUtil.SW_RESTORE);
                ViewModelBase.HintByMessageBox(strError);

                return;
            }

            //增加一个命令
            if (!isAutoGen)
                MarsTestingFrame.CreateTestStoryboardCmd();
            else
                MarsTestingFrame.CreateAutoGenTestStepsCmd();
#endif

            MarsTestingFrame.CommServerShutdownHandler = ShuddownSystem;
            
            /// then start QTPStarter.exe
            /// 在新查询模式下 需要重新启动qtp和测试用例
            /// 
#if _TestStepUnit
            MarsUtilities.StartQTPStarer(string.Format("-RestartQTPClient -UserName {0}",WCFXmlCfgMgr.CurrentLoginUser));
#endif

            //Process objNewProce = new Process { StartInfo = new ProcessStartInfo { FileName = @".\QtpStarter.exe" } };
            /// 如果从dll中启动qtp，将造成exception，所以将该段代码移到这里
            /// except that, the Mars compiler should change to "any cpu"
#if _START_QTP_FROM_APP && !_TestStepUnit
            ///注：如果采用TestStepUnit模式，客户端将从服务器端获取命令
            ///并且qtp已经启动
            (new Thread(new ThreadStart(delegate ()
            {
                ProcessStartInfo StartInfo = new ProcessStartInfo();
                StartInfo.FileName = @".\QtpStarter.exe";
                StartInfo.Arguments = isAutoGen ? "/Gen" : null;
                StartInfo.Domain = "NewMarsDomain";
                Process objNewProce = new Process();
                //Logger.Info("StartTest", string.Format("anonymous Thread start begin"));
                objNewProce.StartInfo = StartInfo;
                objNewProce.Start();
                //Logger.Info("StartTest", string.Format("anonymous Thread started, with process Id:[{0}]", objNewProce.Id));
            }))).Start();
#endif
        }



        private bool checkLicense(ref bool isMessageRequired, ref string strMessageToDisplay)
        {
            bool isShowLicenseInfo = false;
            if (((App)Application.Current).gLicenseMgr == null)
            {
                isShowLicenseInfo = false;
            }
            else
            {
                var lic = ((App)Application.Current).gLicenseMgr;
                isShowLicenseInfo = lic.isAvailable();

                if (isShowLicenseInfo)
                {
                    isMessageRequired = false;
                    if (lic is TestFrameLicense)
                    {
                        TestFrameLicense testFrameLicense = lic as TestFrameLicense;
                        if (testFrameLicense == null) return true;

                        double dt = testFrameLicense.GetDistance();
                        if ((dt<=6)&&(dt>0))
                        {
                            isMessageRequired = true;
                            strMessageToDisplay = dt > 1 ? string.Format("License will expired in [{0}] days!", dt) :
                                "License expires today!";
                        }
                    }
                }
            }
            return isShowLicenseInfo;
        }

        private bool CheckStoryBoardActiveStatus(ref StoryboardEditControl targetStoryBoard)
        {
#if tiger_dock
            if (dockManager.ActiveContent == null) return false;
            StoryboardCombinedControl objStoryBoard = MarsObjectsAndChildrenHelper.GetChildOfType<StoryboardCombinedControl>((UIElement)(dockManager.ActiveContent));
            if (objStoryBoard == null) return false;

            //if (!(dockManager.ActiveContent is StoryboardCombinedControl)) return false;
            //StoryboardCombinedControl objStoryBoard = ((StoryboardCombinedControl)dockManager.ActiveContent);
            objStoryBoard.DataContext = this;
            targetStoryBoard = (StoryboardEditControl)objStoryBoard.storyboardDocPanel.Children[0];
#else
            if (this.FormPanel.Children == null) return false;
            if (this.FormPanel.Children.Count <= 0) return false;
            if (!(this.FormPanel.Children[0] is StoryboardCombinedControl)) return false;
            /// check whether the current Active tab is Storyboard
            /// 
            StoryboardCombinedControl objStoryBoard = ((StoryboardCombinedControl)this.FormPanel.Children[0]);
            objStoryBoard.DataContext = this;
            targetStoryBoard = (StoryboardEditControl)objStoryBoard.storyboardDocPanel.Children[0];
            //if (objStoryBoard.projectTabControl.storyboardDocPanel == null) return false;
            //if (objStoryBoard.projectTabControl.storyboardDocPanel.Children == null) return false;
            //if (objStoryBoard.projectTabControl.storyboardDocPanel.Children.Count <= 0) return false;
            //if (!(objStoryBoard.projectTabControl.storyboardDocPanel.Children[0] is StoryboardEditControl)) return false;

            //if (objStoryBoard.projectTabControl.tabHost.SelectedIndex == 0) return false;
            //targetStoryBoard = ((StoryboardEditControl)objStoryBoard.projectTabControl.storyboardDocPanel.Children[0]);
            //if (targetStoryBoard.CurrentTestApplicationId == null) return false;
#endif
            return true;
        }

        private bool CheckWhetherApplicationIsSelected()
        {

            return this.SelectecTestTargetApplication != null;
        }

#region codes from other partial class
        private long? currentStoryBoardDetailID = null;
        public long? CurrentStoryBoardDetailID
        {
            get
            {
                return currentStoryBoardDetailID;
            }
            set
            {
                currentStoryBoardDetailID = value;
                RaisePropertyChanged("CurrentStoryBoardDetailID");
                DealwithStoryBoardDetailIdchange();
            }
        }

        private OnStoryBoardDetailIdChangeEvent onStoryBoardDetailIDChangeHandler = null;
        private void DealwithStoryBoardDetailIdchange()
        {
#if tiger_dock
            if (dockManager.ActiveContent == null) return ;
            StoryboardCombinedControl objStoryBoard = MarsObjectsAndChildrenHelper.GetChildOfType<StoryboardCombinedControl>((UIElement)(dockManager.ActiveContent));
            if (objStoryBoard == null) return;

            string strStoryBoardDtlId = objStoryBoard.CurrentStoryBoardDetailID;
            if (string.IsNullOrEmpty(strStoryBoardDtlId)) return;
            long lTmpStoryId =-1;
            if (long.TryParse(strStoryBoardDtlId, out lTmpStoryId))
                this.currentStoryBoardDetailID = lTmpStoryId;
            else
                return;
#else

            /// check whether the current work area is storyboard
            /// 
            if (FormPanel.Children.Count == 0) return;
            if (!(FormPanel.Children[0] is StoryboardCombinedControl)) return;
#endif
            if (onStoryBoardDetailIDChangeHandler == null) return;
            onStoryBoardDetailIDChangeHandler(this.currentStoryBoardDetailID);
        }
        private OnRefreshRequired onHistoryDataDeleteRequireRefreshHandler = null;
        public void DealWithHistoryDataRefreshChange()
        {

#if tiger_dock
            if (dockManager.ActiveContent == null) return;
            StoryboardCombinedControl objStoryBoard = MarsObjectsAndChildrenHelper.GetChildOfType<StoryboardCombinedControl>((UIElement)(dockManager.ActiveContent));
            if (objStoryBoard==null) return;
#else
            if (FormPanel.Children.Count == 0) return;
            if (!(FormPanel.Children[0] is StoryboardCombinedControl)) return;
#endif
            if (onHistoryDataDeleteRequireRefreshHandler == null) return;
            onHistoryDataDeleteRequireRefreshHandler();
        }


        //private static MLogger Logger = MLogger.GetLogger(typeof(MainWindow));
        public static readonly DependencyProperty CurrentStoryBoardIDProperty =
            DependencyProperty.Register("CurrentActiveStoryBoardID", typeof(long?),
                typeof(MarsMainWindow), null);
        public long? CurrentActiveStoryBoardID
        {
            get
            {
                return (long?)GetValue(CurrentStoryBoardIDProperty); //currentActiveStoryBoardID;
            }
            set
            {
                SetValue(CurrentStoryBoardIDProperty, value);
                //if (value!=currentActiveStoryBoardID)
                //{
                //    currentActiveStoryBoardID = value;
                //    RaisePropertyChanged("CurrentActiveStoryBoardID");
                //}

            }
        }



        private void UpdateTestInfoByStoryBoardProjectId(long projectId, long storyboardId)
        {
            //Logger.logBegin("UpdateTestInfoByStoryBoardProjectId");
            /// enable ribbon 
            /// 
            EnableTestRibbon();
            //CurrentActiveStoryBoardID = storyboardId;
            /// get data from DB
            /// 

        }

        private void EnableTestRibbon()
        {
            HashSet<E_DISPLAY_TESTRIBBON> hsHiddenTest = new HashSet<E_DISPLAY_TESTRIBBON>();
            hsHiddenTest.Add(E_DISPLAY_TESTRIBBON.E_ENABLE_STORYBOARD_TEST);
            hsHiddenTest.Add(E_DISPLAY_TESTRIBBON.E_ENABLE_TESTCASE_TEST);
            OnMainRibbon_ProjectTestChangeImpl(hsHiddenTest);
        }

        public void OnMainRibbon_ProjectTestChangeImpl(HashSet<E_DISPLAY_TESTRIBBON> displayId)
        {
            //if (displayId == null) return;
            //if (displayId.Contains(E_DISPLAY_TESTRIBBON.E_HIDDEN_ALL))
            //{
            //    /// hidden all test group
            //    /// 
            //    this.ProjectTestGroup.Visibility = Visibility.Hidden;
            //    return;
            //}
            //if (this.ProjectTestGroup.Visibility != Visibility.Visible)
            //{
            //    this.ProjectTestGroup.Visibility = Visibility.Visible;
            //}
            //if ((displayId.Contains(E_DISPLAY_TESTRIBBON.E_ENABLE_STORYBOARD_TEST)))
            //{
            //    this.ComboxTargetApplication.Visibility = Visibility.Visible;
            //    this.ribbonBtnTestCurrentStoryboard.Visibility = Visibility.Visible;
            //    this.ribbonBtnTestCurrentTC.Visibility = Visibility.Hidden;
            //}
            //if ((displayId.Contains(E_DISPLAY_TESTRIBBON.E_ENABLE_TESTCASE_TEST)))
            //{
            //    this.ComboxTargetApplication.Visibility = Visibility.Visible;
            //    this.ribbonBtnTestCurrentStoryboard.Visibility = Visibility.Visible;
            //    this.ribbonBtnTestCurrentTC.Visibility = Visibility.Visible;
            //}

        }

        public int OnRequireRibbonCurrentTestApplicationImpl(ref string errorMessage, ref E_ERROR_CODE_TEST_FRAMEWORK errorCode)
        {
            if (!this.ComboxTargetApplication.IsVisible)
            {
                errorCode = E_ERROR_CODE_TEST_FRAMEWORK.E_ERROR_NOT_ENABLED_APPLICATION_SELECTLIST;
                errorMessage = Mars.Properties.Resources.E_ERROR_NOT_ENABLED_APPLICATION_SELECTLIST;
                return int.MinValue;
            }
            if ((this.ComboxTargetApplication.Items == null) || (this.ComboxTargetApplication.Items.Count == 0))
            {
                errorCode = E_ERROR_CODE_TEST_FRAMEWORK.E_ERROR_NO_APPLICATIONS;
                errorMessage = Mars.Properties.Resources.E_ERROR_NO_APPLICATIONS;
                return int.MinValue;
            }

            if (this.ComboxTargetApplication.SelectionBoxItem == null)
            {
                errorCode = E_ERROR_CODE_TEST_FRAMEWORK.E_ERROR_NO_APPLICATION_IS_SELECTED;
                errorMessage = Mars.Properties.Resources.E_ERROR_NO_APPLICATION_IS_SELECTED;
                return int.MinValue;
            }
            if (!(this.ComboxTargetApplication.SelectionBoxItem is MarsKeyValues<int, string>))
            {
                errorCode = E_ERROR_CODE_TEST_FRAMEWORK.E_ERROR_WRONG_SELECTED_ITEM_TYPE_PARA_2;
                errorMessage = string.Format(Mars.Properties.Resources.E_ERROR_WRONG_SELECTED_ITEM_TYPE_PARA_2, "MarsKeyValues<int, string>", this.ComboxTargetApplication.SelectionBoxItem.GetType().ToString());
                return int.MinValue;
            }
            return ((MarsKeyValues<int, string>)(this.ComboxTargetApplication.SelectionBoxItem)).MKey;

        }


#endregion

        private bool AutoGen_GenStepImpl(string strSwfName, string strType, string strTxt, ref string strErrorInfo)
        {
            Logger.Info("AutoGen_GenStepImpl", string.Format("{0} \t{1} \tBegins", "AutoGen_GenStepImpl", DateTime.Now));

            try
            {
                bool isOk = false;
                string strTmp = "";
                Dispatcher.Invoke(new Action(delegate ()
                {
#if tiger_dock
                    if (dockManager.ActiveContent == null) return;
                    TestCaseEdit objTCEdit = MarsObjectsAndChildrenHelper.GetChildOfType<TestCaseEdit>((UIElement)(dockManager.ActiveContent));
                    if (objTCEdit==null) return;
#else
                    TestCaseEdit objTCEdit = (TestCaseEdit)FormPanel.Children[0];
#endif
                    isOk = objTCEdit.AutoGen_GenStep(strSwfName, strType, strTxt, ref strTmp);
                }));
                if (!isOk)
                {
                    strErrorInfo = strTmp;
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("AutoGen_GenStepImpl", string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }


        private bool AutoGen_CurrentPegInfoImpl(ref string strPegWindowInfo, ref string strErrorInfo)
        {
            Logger.Info("AutoGen_CurrentPegInfoImpl", string.Format("{0} \t{1} \tBegins", "AutoGen_CurrentPegInfoImpl", DateTime.Now));

            /// check current Model 
            /// 
            try
            {
                bool isOk = false;
                string strTmp = "", strTmpPeg = "";
                Dispatcher.Invoke(new Action(delegate ()
                {
                    isOk = IsCurrentModelUnderGen(ref strTmp);
                    if (!isOk) return;
#if tiger_dock
                    if (dockManager.ActiveContent== null)
                    {
                        strTmp = "No TestCase Edit model is actived";
                        Logger.Error("AutoGen_CurrentPegInfoImpl", strTmp);
                        isOk = false;
                        return;
                    }
                    TestCaseEdit objTCEdit = MarsObjectsAndChildrenHelper.GetChildOfType<TestCaseEdit>((UIElement)(dockManager.ActiveContent));
                    strTmpPeg = objTCEdit.LatestPegwindowInfo;
#else
                    strTmpPeg = ((TestCaseEdit)FormPanel.Children[0]).LatestPegwindowInfo;
#endif
                    if (string.IsNullOrEmpty(strTmpPeg))
                    {
                        strTmp = "No Pegwindow Quick Info returns, please check whether the object infomation.";
                        isOk = false;
                    }
                    else
                        isOk = true;
                }));
                if (!isOk)
                {
                    strErrorInfo = strTmp;
                    return false;
                }
                else
                    strPegWindowInfo = strTmpPeg;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("AutoGen_CurrentPegInfoImpl", string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }


        }

        private bool IsCurrentModelUnderGen(ref string strErrorInfo)
        {
            Logger.Info("IsCurrentModelUnderGen", string.Format("{0} \t{1} \tBegins", "IsCurrentModelUnderGen", DateTime.Now));

            try
            {
#if tiger_dock
                TestCaseEdit objCurrentTestCase = MarsObjectsAndChildrenHelper.GetChildOfType<TestCaseEdit>((UIElement)(dockManager.ActiveContent));
                if (objCurrentTestCase==null)
                {
                    strErrorInfo = "Please Make sure a Test case is loaded and actived.";
                    return false;
                }
#else
                if (!(FormPanel.Children[0] is TestCaseEdit))
                {
                    strErrorInfo = "Please Make sure a Test case is loaded and actived.";
                    return false;
                }

                TestCaseEdit objCurrentTestCase = (TestCaseEdit)FormPanel.Children[0];
#endif
                ///Check where the the first row or can find last row with peg window
                /// 
                if (!objCurrentTestCase.IsUnderAutoGen())
                {
                    strErrorInfo = "Please Make sure a Pegwindow exists of the current Test case.\r\nAll Generated Scripts are based on that Window";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("IsCurrentModelUnderGen", strErrorInfo = string.Format("Exception:[{0}], stackTrace:[{1}]", ex.Message, ex.StackTrace), ex);
                return false;

            }
        }

        private void RibbonButton_ReplayRecordClick(object sender, RoutedEventArgs e)
        {
            ///check whether the current work area is about test case
            /// 
            string strErrorInfo = "";
            bool isUnderGen = IsCurrentModelUnderGen(ref strErrorInfo);
            if (!isUnderGen)
            {
                MessageBox.Show(strErrorInfo, "Hint");
                return;
            }

            ///set it self as half of the screen
            ///adjust target application size and position
            /// for currently, only Summit is applied
            /// 
            AdjustAutoGenWindowAndTargetWindow();

            ///Start WCF server
            /// 
            try
            {
                bool isCntn = this.continueLatestTest.IsChecked ?? false;
                bool isIgnoreError = this.chckbxIgnoreTestCaseError.IsChecked ?? false;
                Thread testThrd;
                (testThrd = new Thread(new ThreadStart(new Action(
                    delegate
                    {
                        StartTest(isIgnoreError, isCntn, null, null, _CurrentStoryboardName, _CurrentStoryboardID + "", true);
                    })
                        )
                    )
                ).Start();
            }
            catch (Exception ex)
            {
                Logger.Error("RibbonButton_ReplayRecordClick", string.Format("Exception:[{0}]", ex.Message), ex);
            }


            /// Start QTP with Gen Scripts
            /// 

        }

        private void AdjustAutoGenWindowAndTargetWindow()
        {
            System.Drawing.Rectangle rectCurrentSc = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle).WorkingArea;
            string strAppToGen = AppConfigReader.GetAppForGen() ?? "SummitFT.exe";
            bool isTargetAppAvailable = AdjustTargetWindows(strAppToGen, rectCurrentSc);
            if (!isTargetAppAvailable)
            {
                MessageBox.Show(string.Format("Make sure the application is Available:[{0}]", strAppToGen), "Warnning");
                return;
            }
            AdjustSelf(rectCurrentSc);

        }

        private void AdjustSelf(System.Drawing.Rectangle rectCurrentSc)
        {

            this.WindowState = WindowState.Normal;
            this.Left = rectCurrentSc.Left;
            this.Top = rectCurrentSc.Top;
            this.Width = rectCurrentSc.Width / 2;
            this.Height = rectCurrentSc.Height;
        }

        private bool AdjustTargetWindows(string strTargetApplicationLabel, System.Drawing.Rectangle rectCurrentSc)
        {
            Logger.Info("AdjustTargetWindows", string.Format("[{0}] begins \t[{1}], ApplicationLabel:[{2}]", "AdjustTargetWindows", DateTime.Now, strTargetApplicationLabel));

            try
            {
                Process[] arrP = Process.GetProcesses();
                Process pTarget = null;
                pTarget = arrP.Where(delegate (Process p)
                {
                    try
                    {
                        return p.MainModule == null ? false : p.MainModule.FileName.ToUpper().IndexOf(strTargetApplicationLabel.ToUpper()) >= 0;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("AdjustTargetWindows", string.Format("Exception:[{0}]", e.Message), e);
                        return false;
                    }
                }).SingleOrDefault();

                if (pTarget == null)
                {
                    return false;
                }
                int x = rectCurrentSc.X + rectCurrentSc.Width / 2;
                int y = 0;
                int iW = rectCurrentSc.Width / 2;
                int iH = rectCurrentSc.Height;
                TigerMarsUtil.SetForegroundWindow(pTarget.MainWindowHandle);
                TigerMarsUtil.ShowWindow(pTarget.MainWindowHandle, TigerMarsUtil.SW_RESTORE);
                TigerMarsUtil.MoveWindow(pTarget.MainWindowHandle, x, y, iW, iH, true);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("AdjustTargetWindows", string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        private void MarsMainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Logger.logBegin("MarsMainWindow_SizeChanged");
            if (this.Width < 1920 / 2 + 100)
                rectLogo.Visibility = Visibility.Hidden;
            else
                rectLogo.Visibility = Visibility.Visible;
            Logger.logEnd("MarsMainWindow_SizeChanged");
        }

        object testcaseIsDoneLck = "Lock_for_Test_case_is_done";
        private void OnTestCaseIsDoneImpl(long lStoryboardDetailId)
        {

            /// steps:
            /// 1, check whether the current model is storyboard
            /// 2, call storyboard refresh
            /// 
            //new Thread(
            //    new ThreadStart(new Action(delegate () {
            //    Dispatcher.BeginInvoke(DispatcherPriority.Background ,
            //        new Action(
            //            delegate () {
            //                try
            //                {
            //                    Monitor.Enter(testcaseIsDoneLck);
            //                    StoryboardEditControl tmpStoryboard = null;
            //                    bool isStoryboardActived = CheckStoryBoardActiveStatus(ref tmpStoryboard);
            //                    if ((!isStoryboardActived) || (tmpStoryboard == null))
            //                    {
            //                        return;
            //                    }
            //                    if (null != tmpStoryboard.refreshStoryBoard_Btn.Command)
            //                        tmpStoryboard.refreshStoryBoard_Btn.Command.Execute(null);
            //                }
            //                catch (Exception e)
            //                {
            //                    Logger.Error("OnTestCaseIsDoneImpl-Thread",string.Format("Exception:[{0}] trace:[{1}]",e.Message,e.TargetSite),e);
            //                }
            //                finally
            //                {
            //                    Monitor.Exit(testcaseIsDoneLck);
            //                }

            //            }
            //            ));
            //}))).Start();
            try
            {
                Monitor.Enter(testcaseIsDoneLck);
                Dispatcher.Invoke(
                    new Action(delegate ()
                    {
                        StoryboardEditControl tmpStoryboard = null;
                        bool isStoryboardActived = CheckStoryBoardActiveStatus(ref tmpStoryboard);
                        if ((!isStoryboardActived) || (tmpStoryboard == null))
                        {
                            return;
                        }
                        if (null != tmpStoryboard.refreshStoryBoard_Btn.Command)
                            tmpStoryboard.refreshStoryBoard_Btn.Command.Execute(null);
                    }),
                    DispatcherPriority.Background);

            }
            catch (Exception e)
            {
                Logger.Error("OnTestCaseIsDoneImpl-Thread", string.Format("Exception:[{0}] trace:[{1}]", e.Message, e.TargetSite), e);
            }
            finally
            {
                Monitor.Exit(testcaseIsDoneLck);
            }
        }

        private void compareData_Click(object sender, RoutedEventArgs e)
        {
            ///steps: 
            /// 1, check wether it is in the comparison GUI
            /// 2, get Leftside and rightside Selected story Board DetailId
            /// 3, call update
            /// 
            if (!IsCurrentModeActived(typeof(StoryboardCompareControl)))
            {
                ShowMessage("Please active Storyboard Comparasion mode first", "Hint");
                return;
            }

#if tiger_dock
            StoryboardCompareControl objCmpCntrl = MarsObjectsAndChildrenHelper.GetChildOfType<StoryboardCompareControl>((UIElement)(dockManager.ActiveContent));
            
#else
            StoryboardCompareControl objCmpCntrl = (StoryboardCompareControl)this.FormPanel.Children[0];
#endif
            if ((objCmpCntrl.storyboardDocPanel.Children.Count == 0) || (objCmpCntrl.storyboardDocPanel2.Children.Count == 0))
            {
                ShowMessage("No Data in Storyboards", "Warning");
                return;
            }
            StoryboardEditControl stbEdtCntrl1 = (StoryboardEditControl)objCmpCntrl.storyboardDocPanel.Children[0];
            StoryboardEditControl stbEdtCntrl2 = (StoryboardEditControl)objCmpCntrl.storyboardDocPanel2.Children[0];
            if (stbEdtCntrl1 == null || stbEdtCntrl2 == null)
            {
                ShowMessage("At least one storyboard is NULL!", "Error");
                return;
            }
            StoryboardColl objStbDTCntxBaseLine = (StoryboardColl)stbEdtCntrl1.DataContext;
            StoryboardColl objStbDTCntxCmp = (StoryboardColl)stbEdtCntrl2.DataContext;
            if (objStbDTCntxCmp == null || objStbDTCntxBaseLine == null)
            {
                ShowMessage("No Data in Storyboards");
                return;
            }

            if (objStbDTCntxBaseLine.SelectedStoryboardRows == null || objStbDTCntxBaseLine.SelectedStoryboardRows.Count <= 0
                || objStbDTCntxCmp.SelectedStoryboardRows == null || objStbDTCntxCmp.SelectedStoryboardRows.Count <= 0)
            {
                ShowMessage("Please select one storyboard each side!");
                return;
            }

            if (objStbDTCntxBaseLine.SelectedStoryboardRows[0].DataSummaryId != objStbDTCntxCmp.SelectedStoryboardRows[0].DataSummaryId)
            {
                ShowMessage("Please Select two storyboards with the same Dataset!", "Error");
                return;
            }
            long? baseLineDetailId = objStbDTCntxBaseLine.SelectedStoryboardRows[0].StoryboardDetailId;
            long? cmpDetailId = objStbDTCntxCmp.SelectedStoryboardRows[0].StoryboardDetailId;
            
            objCmpCntrl.ResultViewTabControl.CreateDataContext(baseLineDetailId, cmpDetailId);
        }

        private void ShowMessage(string strMsg, string strTitle = "Hint")
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new Action(delegate ()
                    {
                        MessageBox.Show(strMsg, strTitle);
                    }
                    )
                    );
        }

        private bool IsCurrentModeActived(Type objType)
        {
#if tiger_dock
            
            if (this.dockManager.ActiveContent == null) return false;
            try
            {
                LayoutDocument objLayDoc = (LayoutDocument)dockManager.ActiveContent;
                if (!(objLayDoc.Content is DockPanel)) return false;
                if (((DockPanel)objLayDoc.Content).Children[0].GetType()== objType) return true;
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("IsCurrentModeActived", e.Message,e);
                return false;
            }
            //return dockManager.ActiveContent.GetType() == objType; 
#else
            if (this.FormPanel.Children == null) return false;
            if (this.FormPanel.Children.Count <= 0) return false;

            return this.FormPanel.Children[0].GetType() == objType;
#endif
        }


        bool _isRunningInBatchMode = false;
        public bool IsRunningInBatchMode
        {
            get
            {
                return _isRunningInBatchMode;
            }

            set
            {
                _isRunningInBatchMode = value;
            }
        }


        void ShuddownSystem()
        {
            this.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }



#region generate pdf report

        public static bool USE_REPORT_DRIVER = true;

        public void GenPdfMgmtRpt(object sender, RoutedEventArgs e)
        {
            if (USE_REPORT_DRIVER)
            { 
                ReportDriver driver = new ReportDriver(currentDatabaseIdx);
                driver.GenPdfMgmtRpt();
                return;
            }
            KillWordProc("MARS_SUMMARY_TEMPLATE");
            try
            {
                string mgmtReportConfigPath = ConfigurationManager.AppSettings["REPORT_TEMPLATE_PATH"] + "\\MarsSummaryReportConfig.txt";
                if (File.Exists(mgmtReportConfigPath) == false)
                {
                    ShowMessage("WARNING: Report config file " + mgmtReportConfigPath + " is not found");
                    return;
                }

                WordSummaryReportConfig config = new WordSummaryReportConfig();
                WordReportFilter rf = new WordReportFilter(mgmtReportConfigPath);

                // proj result used to get true negin and end time
                B_T_PROJ_TEST_RESULT ptr = new B_T_PROJ_TEST_RESULT();
                var projTestResultList = ptr.GetProjTestResult(currentDatabaseIdx);

                // Get project data
                List<B_TEST_PROJECT> ProjectList = new List<B_TEST_PROJECT>();
                B_TEST_PROJECT objProject = new B_TEST_PROJECT();
                ProjectList = objProject.GetProject(CurrentDatabaseIdx);

                // Process projects while applying filtering
                foreach (var project in ProjectList)
                {
                    //if (ProjectIsRequiredForReport(project.PROJECT_NAME))
                    if (rf.ProjectIsRequired(project.PROJECT_NAME))
                    {
                        config.AddRowToProjectSummaryData(project.PROJECT_NAME, project.PROJECT_DESCRIPTION);
                        config.MarsProjectCount++;
                        var projectConfig = config.ConfigureProjectData(project.PROJECT_NAME, project.PROJECT_DESCRIPTION, project.PROJECT_ID);

                        List<B_STORYBOARD_SUMMARY> storyboardList = BoHelper.GetAllStoryboardRows(currentDatabaseIdx, project.PROJECT_ID);
                        foreach (var sb in storyboardList)
                        {
                            //if (StoryboardIsRequiredForReport(sb.STORYBOARD_NAME))
                            if (rf.StoryboardIsRequired(project.PROJECT_NAME, sb.STORYBOARD_NAME))
                            {
                                projectConfig.AddRowToProjectStoryboardData(sb.STORYBOARD_NAME, sb.DESCRIPTION);
                                config.MarsStoryboardCount++;
                                // get counts and stats from db
                                int iUnprocecced = 0;
                                V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo = GetStoryBoardSummaryInfoByStoryBoardId(sb.STORYBOARD_ID, ref iUnprocecced);

                                if (objStoryBrdSumInfo == null)
                                    continue;

                                // stats            
                                int marsTSCount = (int)(objStoryBrdSumInfo.TSCNT ?? 0);
                                int marsTCCount = (int)(objStoryBrdSumInfo.TCCNT ?? 0);
                                int marsTestStepCount = (int)(objStoryBrdSumInfo.STEP_CNT ?? 0);

                                // success stats
                                int marsBSucc = (int)(objStoryBrdSumInfo.BASE_RIGHT_CNT ?? 0);
                                int marsCSucc = (int)(objStoryBrdSumInfo.CMP_RIGHT_CNT ?? 0);
                                int marsBFail = (int)(objStoryBrdSumInfo.BASE_FAIL_CNT ?? 0);
                                int marsCFail = (int)(objStoryBrdSumInfo.CMP_FAIL_CNT ?? 0);
                                int marsBUnpr = iUnprocecced;
                                int marsCUnpr = iUnprocecced;
                                int marsBPartial = (int)(objStoryBrdSumInfo.BASE_PARTIAL_CNT ?? 0);
                                int marsCPartial = (int)(objStoryBrdSumInfo.CMP_PARTIAL_CNT ?? 0);

                                // TODO replace numbers with real counts
                                var sbConfig = projectConfig.ConfigureStoryboard(sb.STORYBOARD_NAME, sb.DESCRIPTION, sb.STORYBOARD_ID, marsCSucc, marsCFail, marsCUnpr, marsCPartial);

                                // storyboard detail data 
                                List<V_STORYBOARD_TEST_FULLVISIONDTO> currentStoryBoardInfo = B_V_STORYBOARD_TEST_FULLVISION.GetStoryBoards(sb.STORYBOARD_ID,currentDatabaseIdx);
                                /// sorted
                                currentStoryBoardInfo = new List<V_STORYBOARD_TEST_FULLVISIONDTO>(currentStoryBoardInfo.OrderBy(p => p.RUN_ORDER));
                                foreach (var row in currentStoryBoardInfo)
                                {
                                    var baselineProjTestResult = GetProjTestResult(projTestResultList, 1, row.STORYBOARD_DETAIL_ID);
                                    var compareProjTestResult = GetProjTestResult(projTestResultList, 0, row.STORYBOARD_DETAIL_ID);

                                    string bStart = "";
                                    string bDuration = "";
                                    string cStart = "";
                                    string cDuration = "";

                                    if (baselineProjTestResult != null)
                                    {
                                        bStart = baselineProjTestResult.TEST_BEGIN_TIME.ToString();
                                        bDuration = (baselineProjTestResult.TEST_END_TIME - baselineProjTestResult.TEST_BEGIN_TIME).ToString();
                                    }

                                    if (compareProjTestResult != null)
                                    {
                                        cStart = compareProjTestResult.TEST_BEGIN_TIME.ToString();
                                        cDuration = (compareProjTestResult.TEST_END_TIME - compareProjTestResult.TEST_BEGIN_TIME).ToString();
                                    }

                                    sbConfig.AddRowToStoryBoardData("" + row.RUN_ORDER,
                                                                    row.TEST_CASE_NAME,
                                                                    row.TEST_SUITE_NAME,
                                                                    row.DATA_SET_ALIAS_NAME,
                                                                    row.DATASET_DESCRIPTION,
                                                                    row.HIST_TEST_RESULT_IN_TEXT,
                                                                    bStart,
                                                                    bDuration,
                                                                    cStart,
                                                                    cDuration);

                                    //row.TEST_CASE_BEGIN_TIME.ToString(),
                                    //row.TEST_CASE_END_TIME.ToString());
                                }

                                // storyboard results

                                StoryboardStats sbs = ComputeStoryboardStats((long)sb.STORYBOARD_ID);
                                /*
                                sbConfig.AddRowToStoryBoardReportData("Passed", marsBSucc, marsCSucc);
                                sbConfig.AddRowToStoryBoardReportData("Failed", marsBFail, marsCFail);
                                sbConfig.AddRowToStoryBoardReportData("Unprocessed", marsBUnpr, marsCUnpr);
                                */

                                sbConfig.AddRowToStoryBoardReportData("Passed", sbs.MarsBSucc, sbs.MarsCSucc);
                                sbConfig.AddRowToStoryBoardReportData("Failed", sbs.MarsBFail, sbs.MarsCFail);
                                sbConfig.AddRowToStoryBoardReportData("Partial", 0, sbs.MarsCPartial);
                                sbConfig.AddRowToStoryBoardReportData("Unprocessed", sbs.MarsBUnpr, sbs.MarsCUnpr);

                                // storyboard stats
                                sbConfig.AddRowToStoryBoardTestingData("Number of Test Suites", marsTSCount);
                                sbConfig.AddRowToStoryBoardTestingData("Number of Test Cases", marsTCCount);
                                sbConfig.AddRowToStoryBoardTestingData("Number of Test Steps", marsTestStepCount);

                                // update top summary results

                                config.MarsTestCaseCount += marsTCCount;
                                config.MarsTestStepCount += marsTestStepCount;

                                config.MarsCSucc += sbs.MarsCSucc;
                                config.MarsCFail += sbs.MarsCFail;
                                config.MarsCPartial += sbs.MarsCPartial;
                                config.MarsCUnpr += sbs.MarsCUnpr;
                                config.MarsBSucc += sbs.MarsBSucc;
                                config.MarsBFail += sbs.MarsBFail;
                                config.MarsBPartial += sbs.MarsBPartial;
                                config.MarsBUnpr += sbs.MarsBUnpr;
                            }
                        }
                    }


                    // Fill file config data
                    string strPath = ConfigurationManager.AppSettings[MarsConstants.CNST_TEST_REPORT_PATH];
                    string reportTemplatePath = ConfigurationManager.AppSettings["REPORT_TEMPLATE_PATH"];

                    config.TemplateFilePath = reportTemplatePath + "\\" + "MARS_SUMMARY_TEMPLATE.docx";
                    if (File.Exists(config.TemplateFilePath) == false)
                    {
                        ShowMessage("WARNING: Report template file " + config.TemplateFilePath + " is not found");
                        return;
                    }

                    config.OutputFilePath = strPath + "\\" + "MarsTestSummaryReport_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".docx";

                    // configure report date
                    string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
                    config.ReportGenDate = currentDateTime;
                }


                // generate report
                WordSummaryReportGen gen = new WordSummaryReportGen(config);
                gen.OpenDocument();
                gen.GenerateDocument();
                gen.SaveDocument();
                ShowMessage("Report is created in " + config.OutputFilePath);
            }
            catch (Exception ex)
            {
                ShowMessage("Error: " + ex);
            }

            KillWordProc("MARS_SUMMARY_TEMPLATE");
        }

        private bool StoryboardIsRequiredForReport(string storyboardName)
        {
            if (storyboardName.Equals("WAIT Testing") == false)
                return true;
            else
                return false;
        }

        private bool ProjectIsRequiredForReport(string projectName)
        {
            if (projectName.Equals("FHLBC Repo") || projectName.Equals("FHLBC Treasury Products"))
                return true;
            else
                return false;
        }

        bool USE_WORD = true;

        public void GenPdfForCurrentTestStoryBoard(object sender, RoutedEventArgs e)
        {

            if (USE_WORD)
            {
                GenWordReportForCurrentTestStoryBoard();
            }

            else
            {
                GenPdfForCurrentTestStoryBoardReal(sender, e);
            }

        }

        public void GenWordReportForCurrentTestStoryBoard()
        {
            KillWordProc("MARS_TEMPLATE");

            int iUnprocecced = 0;
            StoryboardEditControl targetStoryBoard = null;
            string strError = "";
            //
            bool isCurrentStatusRight = CheckStoryBoardActiveStatus(ref targetStoryBoard);
            if (!isCurrentStatusRight)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = "No storyboard is actived.");
                ShowMessage(strError, "Error");
                return;
            }
            //
            V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo = GetStoryBoardSummaryInfoByStoryBoardId(targetStoryBoard.CurrentStoryBoardID ?? -1, ref iUnprocecced);
           
            //config.StoryBoardConfig.StoryBoardDescr = objStoryBrdSumInfo.
            if (objStoryBrdSumInfo == null)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Can't get Storyboard summary information:Name:[{0}] ID:[{1}]", objTargetStoryBoard.CurrentStoryBoardID ?? -1, targetStoryBoard._CurrentStoryboardName));
                ShowMessage(strError, "Error");
                return;
            }

            ReportDriver driver = new ReportDriver(CurrentDatabaseIdx);
            driver.GenWordReportForStoryBoardId(objStoryBrdSumInfo,"EXCEL");

        }

        private void KillWordProc(string templateName)
        {
            Logger.logBegin("KillWordProc");
            try
            {

                System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("WINWORD");
                foreach (System.Diagnostics.Process CurrentProcess in processes)
                {
                    if (CurrentProcess.MainWindowTitle.Contains(templateName))
                    {
                        CurrentProcess.Kill();
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error("KillWordProc", string.Format("{0}", ex.Message), ex);
        }
            Logger.logEnd("KillWordProc");
        }

        public void GenWordReportForStoryBoardId(V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo)
        {
            // Word Doc Generation
            Logger.logBegin("GenWordReportForStoryBoardId");
            try
            {

            
            WordReportConfig config = new WordReportConfig();

            // Configure file locations
            //string templateWordDoc = "";
           // string outputWordDoc = "";

            config.TemplateFilePath = "";
            config.OutputFilePath = "";

            // Misc info
                     
            string currentDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
            config.ReportGenDate = currentDateTime;

           
            // Configure Storyboard report data

            String storyboardName = "MyStoryboard";
            String storyboardDescr = "MyStoryboard Description";

            DataTable sbdt = new DataTable();
            sbdt.Columns.Add("#");
            //sbdt.Columns.Add("Step_Name");
            sbdt.Columns.Add("TC_Name");
            sbdt.Columns.Add("TS_Name");
            sbdt.Columns.Add("Data_Set");
           
            sbdt.Columns.Add("BL Start");
            sbdt.Columns.Add("BL Duration");
            sbdt.Columns.Add("CP Start");
            sbdt.Columns.Add("CP Duration");
            sbdt.Columns.Add("BL Result");
            sbdt.Columns.Add("CP Result");

            config.ConfigureStoryBoard(storyboardName, storyboardDescr, sbdt);
            config.ProjectDescription = "My Project Description";
            
            string strError = "";

            config.StoryBoardConfig.StoryBoardName = objStoryBrdSumInfo.STORYBOARD_NAME;

            // Configure Report template data
            string strPath = ConfigurationManager.AppSettings[MarsConstants.CNST_TEST_REPORT_PATH];        
            string reportTemplatePath = ConfigurationManager.AppSettings["REPORT_TEMPLATE_PATH"];

           
            config.TemplateFilePath = reportTemplatePath + "\\" + "MARS_TEMPLATE.docx";

            Logger.Info("GenWordReportForStoryBoardId", "config.TemplateFilePath :" + config.TemplateFilePath);

            if (File.Exists(config.TemplateFilePath) == false)
            {
                ShowMessage("WARNING: Report template file " + config.TemplateFilePath + " is not found");
                return;
            }
            
            config.OutputFilePath = strPath + "\\" + "MarsTestReport_" + config.StoryBoardConfig.StoryBoardName + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + ".docx";

            Logger.Info("GenWordReportForStoryBoardId", "config.OutputFilePath :" + config.OutputFilePath);
            
                // Fill stats config data

            if (Directory.Exists(strPath) == false)
            {
                ShowMessage("WARNING: Report folder " + strPath + " is not found");
                return;
            }

            // Configure data for "Testing Summary"
            config.MarsTSCount = (int)(objStoryBrdSumInfo.TSCNT ?? 0);
            config.MarsTCCount = (int)(objStoryBrdSumInfo.TCCNT ?? 0);
            config.MarsTestStepCount = (int)(objStoryBrdSumInfo.STEP_CNT ?? 0);

            // Configure data for "Result Summary" and the Pie Chart
            StoryboardStats sbs = ComputeStoryboardStats((long)objStoryBrdSumInfo.STORYBOARD_ID);
            config.MarsBSucc = sbs.MarsBSucc;
            config.MarsCSucc = sbs.MarsCSucc;
            config.MarsBFail = sbs.MarsBFail;
            config.MarsCFail = sbs.MarsCFail;
            config.MarsBPartial = sbs.MarsBPartial;
            config.MarsCPartial = sbs.MarsCPartial;
            config.MarsBUnpr = config.MarsTCCount - (sbs.MarsBSucc + sbs.MarsBFail);
            config.MarsCUnpr = config.MarsTCCount - (sbs.MarsCSucc + sbs.MarsCFail + sbs.MarsCPartial);

            //Configure data for the Storyboard         
            List<V_STORYBOARD_TEST_FULLVISIONDTO> currentStoryBoardInfo = B_V_STORYBOARD_TEST_FULLVISION.GetStoryBoards(objStoryBrdSumInfo.STORYBOARD_ID,currentDatabaseIdx);
            currentStoryBoardInfo = new List<V_STORYBOARD_TEST_FULLVISIONDTO>(currentStoryBoardInfo.OrderBy(p => p.RUN_ORDER));

            B_T_PROJ_TEST_RESULT ptr = new B_T_PROJ_TEST_RESULT();

            var projTestResultList = ptr.GetProjTestResult(currentDatabaseIdx);

            config.ProjectDescription = (from o in currentStoryBoardInfo select o).FirstOrDefault().PROJECT_DESCRIPTION;
            config.ProjectName = (from o in currentStoryBoardInfo select o).FirstOrDefault().PROJECT_NAME;

            var sbId = (from o in currentStoryBoardInfo select o).FirstOrDefault().STORYBOARD_ID;

            string sbDescr = B_STORYBOARD_SUMMARY.GetStoryBoardInfoById(currentDatabaseIdx, sbId).DESCRIPTION;
            config.StoryBoardConfig.StoryBoardDescr = sbDescr;

            foreach (var row in currentStoryBoardInfo)
            {
                var baselineProjTestResult = GetProjTestResult(projTestResultList, 1, row.STORYBOARD_DETAIL_ID);
                var compareProjTestResult = GetProjTestResult(projTestResultList, 0, row.STORYBOARD_DETAIL_ID);
                
                DataRow newDTRow = sbdt.NewRow();
                sbdt.Rows.Add(newDTRow);
                newDTRow["#"] = row.RUN_ORDER;
              
                newDTRow["TC_Name"] = row.TEST_CASE_NAME;
                newDTRow["TS_Name"] = row.TEST_SUITE_NAME;
                newDTRow["Data_Set"] = row.DATA_SET_ALIAS_NAME;

                string status = row.HIST_TEST_RESULT_IN_TEXT;
                if ((!string.IsNullOrEmpty(status))&&(status.StartsWith("SUCCESS")))
                    status = "PASS";

                if (status == null || status.StartsWith("Begin"))
                        status = "UNPR";
                else if (status.StartsWith("FAILED"))
                        status = "FAILED";
                
                newDTRow["BL Result"] = GenTestCaseStatus(baselineProjTestResult, row, "BL", sbs);
                newDTRow["CP Result"] = GenTestCaseStatus(compareProjTestResult, row, "CP", sbs);

                newDTRow["BL Start"] = baselineProjTestResult==null?null:baselineProjTestResult.TEST_BEGIN_TIME;
                newDTRow["BL Duration"] = baselineProjTestResult == null ? null :( baselineProjTestResult.TEST_END_TIME - baselineProjTestResult.TEST_BEGIN_TIME);

                newDTRow["CP Start"] = compareProjTestResult==null? null : compareProjTestResult.TEST_BEGIN_TIME;
                newDTRow["CP Duration"] = compareProjTestResult == null ? null : (compareProjTestResult.TEST_END_TIME - compareProjTestResult.TEST_BEGIN_TIME);
            }

            // Recalculate stats
            config.MarsCFail = (from DataRow row in sbdt.Rows where (string)row["CP Result"] == "FAIL" select row).Count();
            config.MarsCPartial = (from DataRow row in sbdt.Rows where (string)row["CP Result"] == "PARTIAL" select row).Count();
            config.MarsCSucc = (from DataRow row in sbdt.Rows where (string)row["CP Result"] == "PASS" select row).Count();
            config.MarsCUnpr = (from DataRow row in sbdt.Rows where (string)row["CP Result"] == "UNPR" select row).Count();

            config.MarsBFail = (from DataRow row in sbdt.Rows where (string)row["BL Result"] == "FAIL" select row).Count();
            config.MarsBPartial = (from DataRow row in sbdt.Rows where (string)row["BL Result"] == "PARTIAL" select row).Count();
            config.MarsBSucc = (from DataRow row in sbdt.Rows where (string)row["BL Result"] == "PASS" select row).Count();
            config.MarsBUnpr = (from DataRow row in sbdt.Rows where (string)row["BL Result"] == "UNPR" select row).Count();

            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptBaseline =
                GetTestStepReportViaStoryBoardId((long)objStoryBrdSumInfo.STORYBOARD_ID, 1, true);

            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptCompare = 
                GetTestStepReportViaStoryBoardId((long)objStoryBrdSumInfo.STORYBOARD_ID, 0, true);

            if (

                // lstStepsRptBaseline == null || lstStepsRptBaseline.Count == 0 || 
                lstStepsRptCompare == null || lstStepsRptCompare.Count == 0)
            {
                ShowMessage("WARNING: Compare result data is not available. \n Report can not be generated without result data.");
                return;
            }
                   

                bool isRight = false;
            Dictionary<T_TEST_CASE_SUMMARYDTO, List<V_TEST_STEPS_FULLVISIONDTO>> dicTestCaseInfo = B_TEST_CASE.GetTestCaseViaStoryBoardId(
                currentDatabaseIdx,
                (long)objStoryBrdSumInfo.STORYBOARD_ID, ref strError, ref isRight);
            //string strError = "";
            for (int i = 0; i < lstStepsRptCompare.Count; i++)
            {
                    bool baselineExists = true;
                    KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlCompare = default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>);
                    KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlBaseline = default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>);

                    try
                    {

                        // if (default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>).Equals(lstStepsRptCompare[i]))
                        // {

                        // }
                        //  KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlCompare = lstStepsRptCompare[i];
                        //  KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlBaseline = lstStepsRptBaseline[i];

                        stryBordDtlCompare = lstStepsRptCompare[i];
                        if (lstStepsRptBaseline.Count > i)
                            stryBordDtlBaseline = lstStepsRptBaseline[i];
                        else
                            baselineExists = false;


                    }
                    catch (Exception e)
                    {

                        Logger.Error("GenWordReportForCurrentTestStoryBoard", string.Format("{0}", e.Message), e);
                    }

                    TestStepsReportGridData objStepRptData = new TestStepsReportGridData();
                    objStepRptData.GridData = stryBordDtlCompare;

                    /// write head 
                    /// 
                    string strTestcaseSectionInfo = string.Format("3.{0}. Test Case", i + 1);
                    int iTestCaseId = objStepRptData.GetTestCaseId(ref strError);
                    if (iTestCaseId < 0)
                    {
                        Logger.Error("GenStoryBoardTestCaseDetail", strError);
                        /// write error info to report
                        /// 
                        strTestcaseSectionInfo = string.Format("{0}\r\n    --------Error------\r\n        {1}", strTestcaseSectionInfo, strError);

                        continue;
                    }
                    var queryTC = from tc in dicTestCaseInfo.Keys
                                  where tc.TEST_CASE_ID == iTestCaseId
                                  select tc;
                    T_TEST_CASE_SUMMARYDTO objTCSum = queryTC.FirstOrDefault();
                    if (objTCSum == null)
                    {
                        strTestcaseSectionInfo = string.Format("{0}\r\n    --------Error------\r\n        Can't find Test case summary info from database.", strTestcaseSectionInfo);
                        continue;
                    }
                    /// get test case info               
                    strTestcaseSectionInfo = string.Format("{0} Name:{1}\r\n3.{3}.1. Test Case Description:\r\n {2}\r\n3.{3}.2. Test Case Summary", strTestcaseSectionInfo, objTCSum.TEST_CASE_NAME, objTCSum.TEST_STEP_DESCRIPTION ?? "(N/A)", i + 1);

                    string testCaseName = objTCSum.TEST_CASE_NAME;
                    var sbDetailId = stryBordDtlCompare.Value.FirstOrDefault().Value.FirstOrDefault().STORYBOARD_DETAIL_ID;
                    string dataSetName = (from m in currentStoryBoardInfo where m.STORYBOARD_DETAIL_ID == sbDetailId select m.DATA_SET_ALIAS_NAME).FirstOrDefault();
                    string dataSetDescr = (from m in currentStoryBoardInfo where m.STORYBOARD_DETAIL_ID == sbDetailId select m.DATASET_DESCRIPTION).FirstOrDefault();
                    //string dataSetName = objTCSum.;

                    DataTable tcdt = new DataTable();

                    //tcdt.Columns.Add("REF");
                    tcdt.Columns.Add("#");
                    tcdt.Columns.Add("Keyword");
                    tcdt.Columns.Add("Object Name");
                    tcdt.Columns.Add("Parameters");
                    tcdt.Columns.Add("Input");
                    tcdt.Columns.Add("Outp Baseline");
                    tcdt.Columns.Add("Outp Compare");
                    tcdt.Columns.Add("Status");
                    tcdt.Columns.Add("Img");
                    config.ConfigureTestCase(testCaseName, dataSetName, dataSetDescr, tcdt);

                    var stpCmp = stryBordDtlCompare.Value.FirstOrDefault();
                    if ((stpCmp.Equals(default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>))) || (stpCmp.Value == null)) continue;

                    foreach (var testStepCompare in stryBordDtlCompare.Value.FirstOrDefault().Value)
                    {
                        if (testStepCompare == null) continue;

                        DataRow newDTRow = tcdt.NewRow();
                        tcdt.Rows.Add(newDTRow);

                        newDTRow["#"] = testStepCompare.RUN_ORDER;
                        newDTRow["Keyword"] = testStepCompare.KEY_WORD_NAME;
                        newDTRow["Object Name"] = testStepCompare.OBJECT_HAPPY_NAME;
                        newDTRow["Parameters"] = testStepCompare.COLUMN_ROW_SETTING;
                        newDTRow["Input"] = testStepCompare.INPUT_VALUE_SETTING;

                        // Fill bValue with NA if data is not available
                        string bValue = "NA";
                        if (baselineExists)
                        {
                            // This should not be done if baseline is missing
                            var testStepBaseline =
                                (from o in stryBordDtlBaseline.Value.FirstOrDefault().Value
                                 where o.RUN_ORDER == testStepCompare.RUN_ORDER
                                 select o).FirstOrDefault();
                            if (testStepBaseline != null && testStepBaseline.RETURN_VALUES != null)
                                bValue = CleanupValues(testStepBaseline.RETURN_VALUES);
                            else
                                bValue = "";
                        }

                        string cValue = CleanupValues(testStepCompare.RETURN_VALUES);
                        newDTRow["Outp Baseline"] = bValue;
                        newDTRow["Outp Compare"] = cValue;

                    string status = testStepCompare.RUNNING_RESULT_INFO;
                    if (status != null && status.StartsWith("SUCCESS"))
                        status = "PASS";

                        // Exclued comaring trade id's
                        if (testStepCompare.OBJECT_HAPPY_NAME != null && testStepCompare.OBJECT_HAPPY_NAME.EndsWith("TRADE_ID") == false && bValue.Equals(cValue) == false)
                        {
                            status = "PARTIAL";
                        }

                        newDTRow["Status"] = status;

                        if (testStepCompare.INFO_PIC != null)
                        {
                            string fileName = GeneratePictureFile(testStepCompare.INFO_PIC, testCaseName, dataSetName, testStepCompare.RUN_ORDER);
                            newDTRow["Img"] = fileName;
                        }
                    }
                }

                // generate report
                WordReportGen gen = new WordReportGen(config);
                gen.OpenDocument();
                gen.GenerateDocument();
                gen.SaveDocument();
                ShowMessage("Report is created in " + config.OutputFilePath);
            }
            catch (Exception e)
            {
                Logger.Error("GenWordReportForCurrentTestStoryBoard",string.Format("{0}-\r\n{1}",e.Message,e.StackTrace),e);
            }
            Logger.logEnd("GenWordReportForStoryBoardId");
        }

        private object GenTestCaseStatus(T_PROJ_TEST_RESULTDTO projTestResult, V_STORYBOARD_TEST_FULLVISIONDTO combined, string modev, StoryboardStats sbs)
        {
            string status = "UNPR";
            if (projTestResult != null)
            {
                status = projTestResult.TEST_RESULT_IN_TEXT;
            }

            if (modev.Equals("CP") && 
                projTestResult != null && 
                combined != null )
            { 
                if (sbs.sbStatusDict.Keys.Contains(combined.STORYBOARD_DETAIL_ID) &&
                    sbs.sbStatusDict[combined.STORYBOARD_DETAIL_ID] == (int)TestCaseStatus.PARTIAL)
                status = "PARTIAL";
            }

            if (status != null && status.StartsWith("SUCCESS"))
                status = "PASS";

            if (status != null &&
                (status.StartsWith("FAIL") || 
                 status.StartsWith("Begin") || 
                 status.StartsWith("Exception")) )

                status = "FAIL";

            return status;
        }

        public enum TestCaseStatus
        {
            UNPR = 1,
            FAIL = 2,
            PARTIAL = 3,
            PASS = 4
        }

        StoryboardStats ComputeStoryboardStats(long sbId)
        {
            StoryboardStats sbs = new StoryboardStats();
            sbs.MarsTSCount = 0;
            sbs.MarsTCCount = 0;

            sbs.MarsTestStepCount = 0;

            sbs.MarsBSucc = 0;
            sbs.MarsCSucc = 0;
            sbs.MarsBFail = 0;
            sbs.MarsCFail = 0;
            sbs.MarsBUnpr = 0;
            sbs.MarsCUnpr = 0;
            sbs.MarsBPartial = 0;
            sbs.MarsCPartial = 0;

            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptBaseline =
              GetTestStepReportViaStoryBoardId(sbId, 1, true);

            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRptCompare =
               GetTestStepReportViaStoryBoardId(sbId, 0, true);


            // Go through all storyboard lines
            for (int i = 0; i < lstStepsRptCompare.Count; i++)
            {
                TestCaseStatus bStatus = TestCaseStatus.PASS;
                TestCaseStatus cStatus = TestCaseStatus.PASS;

                KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlCompare = lstStepsRptCompare[i];

                Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> stryBordDtlCompareDict = lstStepsRptCompare[i].Value;

                long sbCompareDetId = (from o in stryBordDtlCompareDict
                                       select o.Value.FirstOrDefault().STORYBOARD_DETAIL_ID).FirstOrDefault().Value;

                // not sure if this is correct
                var stryBordDtlBaselineDict =
                    (from o in lstStepsRptBaseline
                     where o.Value.FirstOrDefault().Value.FirstOrDefault().STORYBOARD_DETAIL_ID == sbCompareDetId
                     select o);


                // There is a potential carash here !!! If baseline was not run completely, FIX it!!!
             //   KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtlBaseline = lstStepsRptBaseline[i];

                if (stryBordDtlCompare.Value == null)
                {
                    cStatus = TestCaseStatus.UNPR;
                }

                if (stryBordDtlBaselineDict == null)
                {
                    bStatus = TestCaseStatus.UNPR;
                }

                // go through evey testStep to find if there are any differences
                foreach (var testStepCompare in stryBordDtlCompare.Value.FirstOrDefault().Value)
                {
                    sbs.MarsTestStepCount++;

                    V_TEST_DATA_REPORT_SUMMARYDTO testStepBaseline = null;

                    if (stryBordDtlBaselineDict.FirstOrDefault().Value != null)
                    {

                        try
                        {
                            Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> baseListDict = stryBordDtlBaselineDict.FirstOrDefault().Value ;
                            IEnumerable<V_TEST_DATA_REPORT_SUMMARYDTO> arrDtos = baseListDict.SelectMany(p => p.Value);
                            if (arrDtos==null)
                            {
                                testStepBaseline = null;
                            }else
                            {
                                V_TEST_DATA_REPORT_SUMMARYDTO ob = arrDtos.Where(p => p.RUN_ORDER == testStepCompare.RUN_ORDER).FirstOrDefault();
                                if (ob==null)
                                {
                                    testStepBaseline = null;
                                }
                                else
                                {
                                    testStepBaseline = ob;
                                }
                            }
                           
                        }
                        catch (Exception e)
                        {
                            Logger.Error("ComputeStoryboardStats", string.Format("Exception:[{0}]",e.Message),e);
                            testStepBaseline = null;
                        };
                    }

                    if (testStepCompare != null 
                        && testStepCompare.RUNNING_RESULT_INFO != null
                        && testStepCompare.RUNNING_RESULT_INFO.Contains("Exception"))
                    { 
                        Logger.Info("ComputeStoryboardStats", "Exception detected");
                    }


                    if (testStepCompare == null || testStepCompare.RUNNING_RESULT_INFO == null)
                    {
                        cStatus = UpdateStatusValue(cStatus, TestCaseStatus.UNPR);
                    }

                    else if (testStepCompare.RUNNING_RESULT_INFO.Equals("FAIL") || testStepCompare.RUNNING_RESULT_INFO.Contains("Exception"))
                    {
                        cStatus = UpdateStatusValue(cStatus, TestCaseStatus.FAIL);
                    }

                    if (testStepBaseline == null || testStepBaseline.RUNNING_RESULT_INFO == null)
                    {
                        bStatus = TestCaseStatus.UNPR;
                    }

                    else if (testStepBaseline.RUNNING_RESULT_INFO.Equals("FAIL") || testStepBaseline.RUNNING_RESULT_INFO.Contains("Exception"))
                    {
                        bStatus = UpdateStatusValue(bStatus, TestCaseStatus.FAIL);
                    }

                    string bValue = "";

                    if (testStepBaseline != null)
                        bValue = CleanupValues(testStepBaseline.RETURN_VALUES);

                    string cValue = CleanupValues(testStepCompare.RETURN_VALUES);


                    string status = testStepCompare.RUNNING_RESULT_INFO;
                    if ((status!=null)&&(status.StartsWith("SUCCESS")))
                    {
                        status = "PASS";
                    }

                    // Excluded comaring trade id's
                    if (testStepCompare.OBJECT_HAPPY_NAME != null && testStepCompare.OBJECT_HAPPY_NAME.EndsWith("TRADE_ID") == false && bValue.Equals(cValue) == false)
                    {
                        status = "PARTIAL";
                        cStatus = UpdateStatusValue(cStatus, TestCaseStatus.PARTIAL);
                    }
                }

                switch (bStatus)
                {
                    case TestCaseStatus.PASS:
                        sbs.MarsBSucc++;
                        break;
                    case TestCaseStatus.FAIL:
                        sbs.MarsBFail++;
                        break;
                    case TestCaseStatus.UNPR:
                        sbs.MarsBUnpr++;
                        break;
                    case TestCaseStatus.PARTIAL:
                        sbs.MarsBPartial++;
                        break;
                    default:
                        break;

                }

                switch (cStatus)
                {
                    case TestCaseStatus.PASS:
                        sbs.MarsCSucc++;
                        break;
                    case TestCaseStatus.FAIL:
                        sbs.MarsCFail++;
                        break;
                    case TestCaseStatus.UNPR:
                        sbs.MarsCUnpr++;
                        break;
                    case TestCaseStatus.PARTIAL:
                        sbs.MarsCPartial++;
                        break;
                    default:
                        break;

                }
                sbs.sbStatusDict.Add(sbCompareDetId, (int)cStatus);
            }

            return sbs;
        }

        // while iterating through all testcase steps we should be careful not to overwrite "worth" status with a "better" one
        private TestCaseStatus UpdateStatusValue(TestCaseStatus oldStatus, TestCaseStatus newStatus)
        {
            TestCaseStatus updatedStatus = oldStatus;

            if (newStatus < oldStatus)
                updatedStatus = newStatus;

            return updatedStatus;
        }

        private T_PROJ_TEST_RESULTDTO GetProjTestResult(List<T_PROJ_TEST_RESULTDTO> projTestResultList, int testMode, long sbDetId)
        {
            T_PROJ_TEST_RESULTDTO result = null;

            var maxValue = (from o in projTestResultList
                            where o.STORYBOARD_DETAIL_ID == sbDetId && o.TEST_MODE == testMode
                            select o).Max(p => p.LATEST_TEST_MARK_ID);

            result = (from o in projTestResultList
                     where o.STORYBOARD_DETAIL_ID == sbDetId && o.TEST_MODE == testMode &&  o.LATEST_TEST_MARK_ID == maxValue
                     select o).FirstOrDefault();

            return result;
        }

        private string CleanupValues(string inpValues)
        {
            string outpValues = "";

            if (inpValues != null)
            {
                char[] delimiters = new char[] { '\r', '\n' };
                string[] parts = inpValues.Split(delimiters,
                             StringSplitOptions.RemoveEmptyEntries);

                int partsCount = parts.Count();
                int lineNum = 1;

                foreach (string word in parts)
                {
                    string line = "";

                    Double n;

                    bool isNumeric = Double.TryParse(word, out  n);

                    if (word.Contains("+308")  || (isNumeric && Math.Abs(n) > 100000000000000))
                    {
                        line = "0";
                    }
                    else if (word.Equals("1/1/0001"))
                    {
                        line = " ";
                    }
                    else
                        line = word;


                    if (partsCount > 1)
                        line = "" + lineNum++ + ":   " + line;

                    outpValues += line + "\r";
                }
            }
            outpValues = outpValues.TrimEnd('\r');
            return outpValues;
        }

        public string GeneratePictureFile(System.Byte[] pictureBytes, string testCaseName, string dataSetName, decimal? testStepNumber)
        {
            // string strTmpFileName = Guid.NewGuid().ToString() + ".png";

            string strTmpFileName = "__" + testCaseName + "__" + dataSetName  + "__" + testStepNumber + ".png";
            // strTmpFileName = System.IO.Path.Combine(MarsReportGen.TempPicturePath, strTmpFileName);
            strTmpFileName = System.IO.Path.Combine(@"c:\temp", strTmpFileName);

            if (File.Exists(strTmpFileName))
            {
                File.Delete(strTmpFileName);
            }

            FileStream objImgStream = File.Open(strTmpFileName, FileMode.CreateNew);
            objImgStream.Seek(0, SeekOrigin.Begin);
            objImgStream.Write(pictureBytes, 0, pictureBytes.Length);
            objImgStream.Close();

            return strTmpFileName;
        }

        public void GenPdfForCurrentTestStoryBoardReal(object sender, RoutedEventArgs e)
        {

            Logger.logBegin("GenPdfForCurrentTestStoryBoard");
#if _pdfreport
            MarsReportGen objPdfGen = new MarsReportGen();
            string strPath = ConfigurationManager.AppSettings[MarsConstants.CNST_TEST_REPORT_PATH];// @"C:\automationTest\Automation Workbooks\Report";            
            string strTmpPicturePath = ConfigurationManager.AppSettings[MarsConstants.CNST_TEMP_PICTUREPATH];
            if (!Directory.Exists(strTmpPicturePath))
            {
                try
                {
                    Directory.CreateDirectory(strTmpPicturePath);
                }
                catch (Exception ex)
                {
                    Logger.Error("GenPdfForCurrentTestStoryBoard", string.Format("Exception:[{0}] statck:[{1}]", ex.Message, ex.StackTrace));
                }
            }
            MarsReportGen.TempPicturePath = strTmpPicturePath;
            objPdfGen.ReportFirstPageHeader = ConfigurationManager.AppSettings[MarsConstants.CNST_REPORTFIRSTPAGEHEADER];
            objPdfGen.ReportPageEyebrow = ConfigurationManager.AppSettings[MarsConstants.CNST_REPORTPAGEEYEBROW];
            string strError = "";
            if (!objPdfGen.BeginToGen(strPath, ref strError))
            {
                ShowMessage(string.Format("Can't access the path:[{0}] with error info:\r\n[{1}]", strPath, strError));
                return;
            }
            objPdfGen.ClientName = AppConfigReader.GetCurrentClientName();// "Northern Trust Ltd.";
            objPdfGen.FileName = string.Format("MarsRpt_{0}_{1}.pdf", this._CurrentStoryboardName, DateTime.Now.ToString("yyyy-MM-dd"));
            objPdfGen.TargetApplicationName = ConfigurationManager.AppSettings[MarsConstants.CNST_TARGET_APPLICATION];
            objPdfGen.ClientLogoPath = AppConfigReader.GetClientLogoPos();

            /// generate First pre Page
            /// 
            bool isGenerated = objPdfGen.GenLogoPage(ref strError);
            if (!isGenerated)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Error when Call GenLogoPage:[{0}]", strError));
                ShowMessage(strError, "Error");
                return;
            }

            /// get TestStoryBorder Summary information
            /// 
            StoryboardEditControl targetStoryBoard = null;
            bool isCurrentStatusRight = CheckStoryBoardActiveStatus(ref targetStoryBoard);
            if (!isCurrentStatusRight)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = "No storyboard is actived.");
                ShowMessage(strError, "Error");
                return;
            }
            int iUnprocecced = 0;
            V_TESTSTORYBOARD_SUMMARYDTO objStoryBrdSumInfo = GetStoryBoardSummaryInfoByStoryBoardId(targetStoryBoard.CurrentStoryBoardID ?? -1, ref iUnprocecced);
            if (objStoryBrdSumInfo == null)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Can't get Storyboard summary information:Name:[{0}] ID:[{1}]", objTargetStoryBoard.CurrentStoryBoardID ?? -1, targetStoryBoard._CurrentStoryboardName));
                ShowMessage(strError, "Error");
                return;
            }

            //List<B_STORYBOARD_TEST_FULLVISION> bStoryboardRows = BoHelper.GetStoryboardRows(targetStoryBoard.CurrentStoryBoardID ?? -1);
            //if (bStoryboardRows==null)
            //{
            //    Logger.Error("GenPdfForCurrentTestStoryBoard",strError =string.Format("Can't get storyboard summary information from Storyboard test full vision table for storyboardId:[{0}]", targetStoryBoard.CurrentStoryBoardID ?? -1));
            //    ShowMessage(strError, "Error");
            //    return;
            //}
            //int iPartialCnt = bStoryboardRows.Count(p => p.HIST_RESULT == 3);
            //objStoryBrdSumInfo.CMP_PARTIAL_CNT = iPartialCnt;

            int iNextPage = -1;
            double dCurrentPos_y = 0.0;
            int iCurrentPage = 2;
            TestStoryboardPieData storybrdPieData = new TestStoryboardPieData();
            storybrdPieData.FailedTCCnt = (int)(objStoryBrdSumInfo.CMP_FAIL_CNT ?? 0);
            storybrdPieData.PartialCount = (int)(objStoryBrdSumInfo.CMP_PARTIAL_CNT ?? 0);
            storybrdPieData.SuccessTCCnt = (int)(objStoryBrdSumInfo.CMP_RIGHT_CNT ?? 0);
            storybrdPieData.UnprocessedTCCnt = iUnprocecced;
            isGenerated = objPdfGen.GenSummaryPage(objStoryBrdSumInfo.BASE_FAIL_CNT, objStoryBrdSumInfo.BASE_PARTIAL_CNT, objStoryBrdSumInfo.BASE_RIGHT_CNT,
                objStoryBrdSumInfo.CMP_FAIL_CNT, objStoryBrdSumInfo.CMP_PARTIAL_CNT, objStoryBrdSumInfo.CMP_RIGHT_CNT, objStoryBrdSumInfo.TCCNT,
                objStoryBrdSumInfo.TSCNT, objStoryBrdSumInfo.STEP_CNT,
                objStoryBrdSumInfo.STORYBOARD_NAME, objStoryBrdSumInfo.STORYBOARD_ID, iUnprocecced, storybrdPieData,
                ref strError, ref iNextPage, ref dCurrentPos_y, ref iCurrentPage);
            if (!isGenerated)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Error when Call GenSummaryPage:[{0}]", strError));
                ShowMessage(strError, "Error");
                return;
            }

            /// Write System Testing Environment
            /// 
            isGenerated = objPdfGen.GenEnvironment(ref iCurrentPage, ref dCurrentPos_y, ref strError);
            if (!isGenerated)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Error when Call GenSummaryPage:[{0}]", strError));
                ShowMessage(strError, "Error");
                return;
            }

            /// Generate storyboard Detail
            /// 
            TestStoryBoardSummayGridData objStoryBoardData = new TestStoryBoardSummayGridData(targetStoryBoard.CurrentStoryBoardID ?? -1,currentDatabaseIdx);
            bool isDataRead = objStoryBoardData.BeginFetchRows();
            if (!isDataRead)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Can't get storyboard data from database, storyboard Name/Id:[{0}/{1}]", targetStoryBoard._CurrentStoryboardName, targetStoryBoard.CurrentStoryBoardID ?? -1));
                ShowMessage(strError, "Error");
                return;
            }
            List<KeyValuePair<string, int>> dicGridHeader = objStoryBoardData.GetGridColumnInfo();
            if ((dicGridHeader == null) || (dicGridHeader.Count <= 0))
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Can't Grid Header, for storyBoard Name/Id::[{0}/{1}]", targetStoryBoard._CurrentStoryboardName, targetStoryBoard.CurrentStoryBoardID ?? -1));
                ShowMessage(strError, "Error");
                return;
            }
            isGenerated = objPdfGen.GenTestStoryBoardSummary(objStoryBoardData, targetStoryBoard.StoryboardDescription, ref iCurrentPage, ref dCurrentPos_y, ref strError);
            if (!isDataRead)
            {
                Logger.Error("GenTestStoryBoardSummary", strError = string.Format("Error when Call GenSummaryPage:[{0}]", strError));
                ShowMessage(strError, "Error");
                return;
            }

            /// gen details of test case and its values
            isGenerated = GenStoryBoardTestCaseDetail(targetStoryBoard.CurrentStoryBoardID ?? -1, objPdfGen, ref iCurrentPage, ref dCurrentPos_y, ref strError);
            if (!isDataRead)
            {
                Logger.Error("GenTestStoryBoardSummary", strError = string.Format("Error when Call GenStoryBoardTestCaseDetail:[{0}]", strError));
                ShowMessage(strError, "Error");
                return;
            }

            /// Gen Signature pages
            /// 
            string strSofteWareProvider = ConfigurationManager.AppSettings[MarsConstants.CNST_TEST_SOFTWARE_PROVIDER];
            string strClientFullName = ConfigurationManager.AppSettings[MarsConstants.CNST_TEST_CLIENTFULLNAME];
            isGenerated = objPdfGen.GensignaturePage(strSofteWareProvider, ref iCurrentPage, ref dCurrentPos_y, ref strError, strClientFullName);

            bool isOk = objPdfGen.saveToFile(ref strError);
            if (!isOk)
            {
                Logger.Error("GenPdfForCurrentTestStoryBoard", strError = string.Format("Error when Call saveToFile:[{0}]", strError));
                ShowMessage(strError, "Error");
                return;
            }
            ShowMessage("Report is created");
#endif
        }

        private bool GenStoryBoardTestCaseDetail(long testStoryBoardId, MarsReportGen objPdfGen, ref int iCurrentPage, ref double dCurrentPos_y, ref string strError)
        {
            Logger.Info("GenStoryBoardTestCaseDetail", string.Format("from Y place:[{0}]", dCurrentPos_y));

            /// Steps:
            /// 1, Write Section Head
            /// 2, use loop to write all test cases
            /// 
            if (objPdfGen == null)
            {
                Logger.Error("GenStoryBoardTestCaseDetail", strError = "PdfGen Object is Null.");
                return false;
            }
            // 1, Write Section Head
            bool isGenerated = objPdfGen.GenTestStoryTestCaseSection("3. Test cases Detail Information", ref iCurrentPage, ref dCurrentPos_y, ref strError);

            /// 2, use loop to write all test cases 
            ///   2.1 get all Test steps reports from Database 
            ///   2.2 loop to write Data grid
            /// 
            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstStepsRpt = GetTestStepReportViaStoryBoardId(testStoryBoardId, 1, true);
            /// get all test cases with 
            /// 
            bool isRight = false;
            Dictionary<T_TEST_CASE_SUMMARYDTO, List<V_TEST_STEPS_FULLVISIONDTO>> dicTestCaseInfo = 
                B_TEST_CASE.GetTestCaseViaStoryBoardId(currentDatabaseIdx, testStoryBoardId, ref strError, ref isRight);
            //string strError = "";
            for (int i = 0; i < lstStepsRpt.Count; i++)
            {
                KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> stryBordDtl = lstStepsRpt[i];
                TestStepsReportGridData objStepRptData = new TestStepsReportGridData();
                objStepRptData.GridData = stryBordDtl;

                /// write head 
                /// 
                string strTestcaseSectionInfo = string.Format("3.{0}. Test Case", i + 1);
                int iTestCaseId = objStepRptData.GetTestCaseId(ref strError);
                if (iTestCaseId < 0)
                {
                    Logger.Error("GenStoryBoardTestCaseDetail", strError);
                    /// write error info to report
                    /// 
                    strTestcaseSectionInfo = string.Format("{0}\r\n    --------Error------\r\n        {1}", strTestcaseSectionInfo, strError);
                    objPdfGen.GenTestCaseDetailInfo(strTestcaseSectionInfo, ref iCurrentPage, ref dCurrentPos_y, ref strError);
                    continue;
                }
                var queryTC = from tc in dicTestCaseInfo.Keys
                              where tc.TEST_CASE_ID == iTestCaseId
                              select tc;
                T_TEST_CASE_SUMMARYDTO objTCSum = queryTC.FirstOrDefault();
                if (objTCSum == null)
                {
                    strTestcaseSectionInfo = string.Format("{0}\r\n    --------Error------\r\n        Can't find Test case summary info from database.", strTestcaseSectionInfo);
                    continue;
                }
                /// get test case info               
                strTestcaseSectionInfo = string.Format("{0} Name:{1}\r\n3.{3}.1. Test Case Descrition:\r\n {2}\r\n3.{3}.2. Test Case Summary", strTestcaseSectionInfo, objTCSum.TEST_CASE_NAME, objTCSum.TEST_STEP_DESCRIPTION ?? "(N/A)", i + 1);
                objPdfGen.GenTestCaseDetailInfo(strTestcaseSectionInfo, ref iCurrentPage, ref dCurrentPos_y, ref strError);
                objPdfGen.GenTestCaseGraphPieInfo(objStepRptData, ref iCurrentPage, ref dCurrentPos_y, ref strError);
                //return true;               
            }
            return true;
        }

        private List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> GetTestStepReportViaStoryBoardId(long testStoryBoardId,  int testMode, bool isNormalizationReq = false)
        {
            Logger.Info("GetTestStepReportViaStoryBoardId", string.Format("try to get teststeps report info via storyboardid:[{0}]", testStoryBoardId));
            B_V_TEST_DATA_REPORT_SUMMARY objTestDtRpt = new B_V_TEST_DATA_REPORT_SUMMARY();
            string strError = "";
            List<KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>> lstTestStepRpt = 
                objTestDtRpt.getTestStpReportDataByTestStoryBoardId(currentDatabaseIdx, testStoryBoardId, ref strError, testMode, isNormalizationReq);
            return lstTestStepRpt;
        }

        private V_TESTSTORYBOARD_SUMMARYDTO GetStoryBoardSummaryInfoByStoryBoardId(long lStoryBoardId, ref int iUnprocecced)
        {
            Logger.Info("GetStoryBoardSummaryInfoByStoryBoardId", string.Format("Try to get Storyboard Summary info by ID:[{0}]", lStoryBoardId));
            B_V_TESTSTORYBOARD_SUMMARY objSum = new B_V_TESTSTORYBOARD_SUMMARY();
            V_TESTSTORYBOARD_SUMMARYDTO objResult = objSum.getSummaryInfoByStoryBoardId(
                currentDatabaseIdx,
                lStoryBoardId);
            if (objResult == null) return null;
            // get partial information and change result 
            List<B_STORYBOARD_TEST_FULLVISION> lstStryBrdInfo = BoHelper.GetStoryboardRows(currentDatabaseIdx,lStoryBoardId);
            int iPartialCnt = lstStryBrdInfo == null ? (int)(objResult.CMP_PARTIAL_CNT ?? 0) : lstStryBrdInfo.Count(p => p.HIST_RESULT == 3);
            int iRightCnt = lstStryBrdInfo == null ? (int)(objResult.CMP_RIGHT_CNT ?? 0) : lstStryBrdInfo.Count(p => p.HIST_RESULT == 1);
            iUnprocecced = lstStryBrdInfo.Count - (int)(objResult.TCCNT ?? 0);
            int iFailCnt = lstStryBrdInfo.Count - iPartialCnt - iRightCnt - iUnprocecced;
            iFailCnt = iFailCnt < 0 ? 0 : iFailCnt;
            objResult.CMP_PARTIAL_CNT = iPartialCnt;
            objResult.CMP_FAIL_CNT = iFailCnt;
            objResult.CMP_RIGHT_CNT = iRightCnt;

            return objResult;
        }



#endregion

        private void marsMainWindow_Closed(object sender, EventArgs e)
        {
            //Application.Current.Shutdown();

        }

        private void dockingManager_ActiveContentChanged(object sender, EventArgs e)
        {

            string strError = "";
            IsActiveContentAStoryBoard(ref strError, false);
        }

        private void HostOfUserControl_ChildrenCollectionChanged(object sender, EventArgs e)
        {
            Logger.Info("HostOfUserControl_ChildrenCollectionChanged","Changed");
        }
#region object spy part
        private bool gIsMouse_down = false;
        
        private void RibbonButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Logger.logBegin("RibbonButton_MouseUp");
            if (!gIsMouse_down) return;

            System.Drawing.Point pt = System.Windows.Forms.Cursor.Position;
#if  _EngineDriver
            IntPtr hwnd = windowsWrapper.SystemUtil.MarsWindowsAPIs.WindowFromPoint(pt);
#else
            IntPtr hwnd = windowsWrapper.SystemUtil.MarsWindowsAPIs.WindowFromPoint(pt);
#endif 
            if (hwnd == IntPtr.Zero) return;

            int processId;
            //get the processid
            if (windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowThreadProcessId(hwnd, out processId) == 0)
            {
                return;
            }

            Process p = Process.GetProcessById(processId);
            ///判断是否已经被inject
            ///
            string strError = "";
            bool isError = false;
#if _NOQTP
            MarsGuiInjectorAgent objInjectorAgnt;
            if (!(objInjectorAgnt = new MarsGuiInjectorAgent()).IsInjected(processId, p.ProcessName, ref strError, ref isError))
            {
                //objInjectorAgnt.InjectToTargetProcess(processId,ref strError, ref isError); 
            }
#endif

        }
        

        private void RibbonButton_MouseMove(object sender, MouseEventArgs e)
        {
            Logger.logBegin("RibbonButton_MouseMove");
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                ReleaseMouseCapture();
                gIsMouse_down = false;
            }
            if (!gIsMouse_down) return;
        }

        private void RibbonButton_DragLeave(object sender, DragEventArgs e)
        {
            
                gIsMouse_down = true;
        }
        private Cursor FinderCursor = null;
        private Cursor CurrentOldCursor = null;
        private void RibbonButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Logger.logBegin("RibbonButton_PreviewMouseDown");
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                Logger.Info("RibbonButton_PreviewMouseDown", "not left button");
                return;
            }
            CaptureMouse();
            if (FinderCursor == null)
                InitFinderCursor();
            Mouse.SetCursor(FinderCursor);
            gIsMouse_down = true; 
            e.Handled = true;
        }
        private void InitFinderCursor()
        {
            Logger.logBegin("InitFinderCursor");
            FinderCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Mars.Resources.CrosshairsCursor.cur"));
        }
#endregion

        private void RibbonButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Logger.logBegin("RibbonButton_MouseLeftButtonUp");
            ReleaseMouseCapture();
            gIsMouse_down = false;
            //e.Handled = true;
        }

        private void RibbonButton_SpyClick_1(object sender, RoutedEventArgs e)
        {
#if _NOQTP
            MarsObjectSpy spyWindow = MarsObjectSpy.GetSpyInstance();
            spyWindow.Show();
            spyWindow.Activate();
            this.WindowState = WindowState.Minimized;
#endif
        }

        private void tvMars_DragEnter(object sender, DragEventArgs e)
        {
            Logger.logBegin("tvMars_DragEnter");
        }

        private bool IsUnderDrag = false;
        private void tvMars_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string strError = "";
            if (!(sender is TreeView)) return;
            Mars.windowsWrapper.SystemUtil.POINT pt = new windowsWrapper.SystemUtil.POINT();
            Mars.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetCursorPos(ref pt);
            TreeView tv = sender as TreeView;
            StoryboardBatch objBatch = new StoryboardBatch();
            if (!IsActiveContent_Batch(ref strError,ref objBatch))
            {
                return;
            }
            //get the click item         

            HitTestResult hit = VisualTreeHelper.HitTest(tv, tv.PointFromScreen(new Point(pt.X, pt.Y)));
            if (!(hit.VisualHit is System.Windows.FrameworkElement)) return;
            System.Windows.FrameworkElement HitSrcControl = hit.VisualHit as System.Windows.FrameworkElement;
            TreeViewItem treeItm = GridViewSort.GetAncestor<TreeViewItem>(hit.VisualHit);
            if (treeItm == null) return;
            //
            
            if ((treeItm.DataContext is MarsStoryboardTreeView))
            {
                DataObject data = new DataObject();
                MarsStoryboardTreeView selectedStoryboard = (MarsStoryboardTreeView)treeItm.DataContext;
                data.SetData("int", selectedStoryboard.StoryboardId);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);

                Mouse.SetCursor(Cursors.ArrowCD);
                IsUnderDrag = true;
                e.Handled = true;

                return;
            }            
        }

        private void tvMars_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            IsUnderDrag = false;
        }
    }

}
