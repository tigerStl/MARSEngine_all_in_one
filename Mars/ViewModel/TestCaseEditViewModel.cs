
using Mars.Business;
using Mars.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using System.IO;
//using Microsoft.Practices.Prism.Commands;
using Prism.Commands;
using Mars.Dto;
using System.Data.Common;
using Mars.DataLayer;
using Route2NSEx.src.Marquis.systemUtil;
using System.Windows.Media;
using Mars.Helpers;

using Mars.Utility;
using System.Data;
using System.Security.Principal;
using System.Text;
using System.Transactions;
using Mars.Views.gridBase;
using com.Mars.ClipboardMgr;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.CommuniteServer;
using MarsTestFrame.com.Mars.TestConfigObjects;
using System.Windows.Threading;
using Mars.auto.loaddata;
using System.Threading;
using com.BasicData;
using MarsTestFrame.systemUtil;
using Mars.auto.LoadDataFromDB.auto.basedatastructure.MarsCfg;
using MarsTestFrame.SourceCode.com.Mars.QTP;
using System.Diagnostics;
#if _NOQTP
using Mars.InjectorAgent;
using Mars.AutoTestingDriver.injector;
using Mars.AutoTestingDriver.message;
#endif
using Mars.Views;
using Excel = Microsoft.Office.Interop.Excel;

namespace Mars.ViewModel
{
    public static class MarsSkipMgr
    {
        private static ObservableCollection<MarsKeyValues<int, string>> _TestStepSkipSettings = new ObservableCollection<MarsKeyValues<int, string>> {
                    new MarsKeyValues<int, string>(0, "None" ),
                    new MarsKeyValues<int, string>(4, "Skip" ),
                    new MarsKeyValues<int, string>(5, "Skip When Error" )
                };
        public static ObservableCollection<MarsKeyValues<int, string>> GetMarsSkipSettins()
        {          
                return _TestStepSkipSettings;
           
        }
    }

    public class TestStepsDataGrid : MarsTigerGridBase
    {
        private static MLogger Logger = MLogger.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TestStepsDataGrid()
        {
            this.SelectionChanged += TestStepsDataGrid_SelectionChanged;
        }

        void TestStepsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.SelectedItemsList = new ObservableCollection<TestStepViewModel>(this.SelectedItems.Cast<TestStepViewModel>()
                                .ToList());
            }
            catch (Exception ex)
            {
                Logger.Error("TestStepsDataGrid_SelectionChanged", ex.StackTrace.ToString(), ex);
            }
        }
#region SelectedItemsList

        public ObservableCollection<TestStepViewModel> SelectedItemsList
        {
            get { return (ObservableCollection<TestStepViewModel>)GetValue(SelectedItemsListProperty); }
            set
            {
                object oldValue = GetValue(SelectedItemsListProperty);
                SetValue(SelectedItemsListProperty, value);
                OnPropertyChanged(new DependencyPropertyChangedEventArgs(SelectedItemsListProperty, oldValue, value));
            }
        }


        public static readonly DependencyProperty SelectedItemsListProperty =
                DependencyProperty.Register("SelectedItemsList", typeof(ObservableCollection<TestStepViewModel>), typeof(TestStepsDataGrid), new PropertyMetadata(null));


#endregion
    }

    public class VMColl : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(VMColl));
        private ICommand _addNewRowCommand;
        private ICommand _deleteSelectedRowsCommand;
        private ICommand _moveUpSelectedRowsCommand;
        private ICommand _moveDownSelectedRowsCommand;
        private ICommand _copySelectedRowsCommand;
        private ICommand _pasteSelectedRowsCommand;
        //private static MarsClipBoard marsClipboard;
        private ICommand _saveCommand;
        private ICommand _loadDataFromDatabaseCommand;
        private ICommand _saveAsCommand;
        private ICommand _refreshCommand;
        private ICommand trialActionStepsButtonClick;
        MarsEntities marsEntities;
        private long _testCaseId;
        public long TestCaseId {
            get { return _testCaseId; }
            set { _testCaseId = value; }
        }
        private string _testCaseName;
        private string _dataSheetName;
        
        public string CurrentDataSheetName
        {
            get { return _dataSheetName; }
        }

        private List<long> deletedTestSteps;
        private List<long> deletedDataSettigs;
        private DelegateCommand<object> _exportToExcelCommand;

        

#if v_16AndUp
        internal List<long?> TestcaseAppliedAppIds = null;
#endif

        public string TestCaseName
        {
            get { return _testCaseName; }
            set { _testCaseName = value; }
        }

        //public Dictionary<int, string> MarsSkipSettings
        //{
        //    get
        //    {
        //        return 
        //    }
        //}

        //Added by Girish for contains
        //internal readonly static List<string> staticKeywordNames = new List<string>()
        //{
        //    "KillApplication",
        //    "SummitLaunch",
        //    "Loop",
        //    "WaitForSeconds",
        //    "StartApplication"
        //};        
        // End of adding by Girish 7th Sep 2015

#region keywords require no objects, tiger added
        internal readonly static List<string> keywordNotRequireObject = B_KEYWORD.KeywordNotRequireObject;
        //internal readonly static List<string> keywordNotRequireObject = new List<string> {
        //    "ClickMenuIcon",
        //    "LaunchApplication",
        //    "SelectMenuItem",
        //    "Dismiss",
        //    "DBCompare",
        //    "WaitForSeconds",
        //    "CheckError",
        //    "RemovePage",
        //    "SetDataFile",
        //    "SetDefaultDataFile",
        //    "ResumeNext",
        //    "CopyExcelRangeToClipboard",
        //    "ExecuteCommand"
        //};

#endregion //keywords require no objects

        //public VMColl(string testCaseName, long dataSheetId, bool isSharedData)
#if v_16AndUp
        /// <summary>
        /// about .5ms
        /// </summary>
        /// <param name="testCaseId"></param>
        /// <param name="dataSheetId"></param>
        /// <param name="isSharedData"></param>
        /// <param name="appliedAppIds"></param>
        public VMColl(string strDBIdx, long testCaseId, long dataSheetId, bool isSharedData, List<long?> appliedAppIds = null, 
            OnAddTestStepUnitObjEvent funcAddNewObj =null )
        {

            this.TestcaseAppliedAppIds = appliedAppIds == null ? B_TEST_CASE.GetAssignedApplications(strDBIdx,testCaseId) : appliedAppIds;
#else
        public VMColl(long testCaseId, long dataSheetId, bool isSharedData)
        {
#endif
            _isSharedData = isSharedData;
            marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            //_testCaseId = GetTestCaseId(testCaseName);
            _testCaseId = testCaseId;
            _testCaseName = GetTestCaseNameById(_testCaseId);

            LoadTestSteps(_testCaseId, dataSheetId);

            deletedTestSteps = new List<long>();
            deletedDataSettigs = new List<long>();

            _addNewRowCommand = new DelegateCommand(() => { addNewRow(_testCaseId); });
            _deleteSelectedRowsCommand = new DelegateCommand(() => { deleteSelectedRows(); });
            _moveUpSelectedRowsCommand = new DelegateCommand(() => { moveSelectedRows("up"); });
            _moveDownSelectedRowsCommand = new DelegateCommand(() => { moveSelectedRows("down"); });
            _copySelectedRowsCommand = new DelegateCommand(() => {
                copySelectedRowsNew();
                //copySelectedRows();
            });
            _pasteSelectedRowsCommand = new DelegateCommand(() => {
                //pasteSelectedRows();
                PasteNew();
            });
            _pasteRowsSpecialCommand = new DelegateCommand(() => {
                PasteNew();
                //pasteRowsSpecial();
            });
            _saveCommand = new DelegateCommand(() => { SaveTestSteps(); });
            _loadDataFromDatabaseCommand = new DelegateCommand(()=> { LoadDataFromDatabaseCommandImpl(); });
            _saveAsCommand = new DelegateCommand(() => { SaveTestStepsAs(); });

            trialActionStepsButtonClick = new DelegateCommand<object>(TrialActionStepsButtonHandler);
            _exportToExcelCommand = new DelegateCommand<object>(this.ExportToExcel);
            _refreshCommand = new DelegateCommand(() => { Refresh(); });
            rowDataCopy = new DelegateCommand<object>(CopyData2OtherDataSet);

            _dataSheetId = dataSheetId;

            _dataSheetName = GetDataSheetNameNameById( _dataSheetId);


             GetAppNames(strDBIdx);

            // AF
            TestcaseAppliedAppIds = GetAppIDs(strDBIdx).ConvertAll(i => (long?)i);

            RefershTitle();

            _isModifiedTestCase = false;
            isTestCaseCreated = true;

#if _TestStepUnit
            if (funcAddNewObj!=null)
            {
                this.onAddTestStepUnitObjHandler += funcAddNewObj;
            }
#endif

        }

        internal bool IsEmptySettingRow()
        {
            var row = SelectedTestCases == null ? null : (SelectedTestCases.Count > 0 ? SelectedTestCases[0] : null);
            if (row == null) return true;
            return string.IsNullOrEmpty(row.DataSet1 == null ? null : row.DataSet1.ToString())
                && string.IsNullOrEmpty(row.RowColumn)
                && string.IsNullOrEmpty(row.Comment);
        }

        private string GetDataSheetNameNameById(long _dataSheetId)
        {
            string name = BoHelper.GetDataSheetNameById(MarsMainWindow.CurrentDatabaseIdx, _dataSheetId);
            if (name == null)
                name = "";
            return name;
        }

        public void RefershTitle()
        {
            Title = "TestCase: " + _testCaseName +
                    "      DataSet: " + _dataSheetName +
                    "      Applied to Apps: " 
                    //+ _appNames
                    ;
        }

        private void GetAppNames(string strDBIdx)
        {
            List<T_REGISTERED_APPSDTO> lstTCApp= BoHelper.GetAppNames(strDBIdx, _testCaseId);
            if (lstTCApp!=null)
            {
                Testcase_applications = new ObservableCollection<T_REGISTERED_APPSDTO>(lstTCApp);
            }else
            {
                Testcase_applications = null;
            }
        }

        // GetAppIDs
        private List<long> GetAppIDs(string strDBIdx)
        {
            return BoHelper.GetAppIds(strDBIdx, _testCaseId);
        }

        private ICommand rowDataCopy = null;
        public ICommand RowDataCopy
        {
            get { return rowDataCopy; }

        }

        private void CopyData2OtherDataSet(object objSelectedTestStep)
        {
            ViewModelBase.HintByMessageBox("test", "hint");
            if (objSelectedTestStep == null) return;
            if (!(objSelectedTestStep is IList<TestStepViewModel>)) return;
            IList<TestStepViewModel> lstTmp = (IList<TestStepViewModel>)objSelectedTestStep;
            if (lstTmp.Count != 1)
            {
                ViewModelBase.HintByMessageBox("Please select Only One Row.", "Hint");
                return;
            }
            if (lstTmp[0].StepNo <= 0)
            {
                ViewModelBase.HintByMessageBox("Please Save Test case first.", "Hint");
                return;
            }
        }
        private void Refresh()
        {

            _isModifiedTestCase = false;
            BoHelper.GetMarsEntitiesInstance(true,MarsMainWindow.CurrentDatabaseIdx);
            LoadTestSteps(_testCaseId, _dataSheetId);
            this.deletedTestSteps = new List<long>();
        }

        public void SaveDataSheet()
        {
            if (_isModifiedTestCase == true)
            {
                System.Windows.MessageBox.Show("Test case data was not saved. \nPlease save test case before savind data.", "Save Test Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SharedDataSetViewModel.SaveAndDelete( 
                MarsMainWindow.CurrentDatabaseIdx,
                _testCaseId, _dataSheetId, TestCases, deletedTestSteps);

            //if (1 == 1)
            return;
            /*
            // save test data summary
            //long summaryId = BoHelper.CreateTestDataSummary();
            long summaryId = this._dataSheetId;

            // save rel_test_case_test_data_summary
            //BoHelper.CreateRelTCDataSummary(summaryId, _testCaseId);
            /// create default Data summary when necessary
            /// 
            if (summaryId == -1)
            {
                summaryId = BoHelper.CreateTestDataSummary(this._testCaseName.Length>18?this._testCaseName.Substring(0, 18): this._testCaseName);
                /// create relation ship
                /// 
                BoHelper.CreateRelTCDataSummary(summaryId, this._testCaseId);
                this._dataSheetId = summaryId;
            }

            // save test data
            if (this._isSharedData)
                SaveSharedDataSettings(summaryId);
            else
                SaveDataSettings(summaryId);
                */
        }

        public bool SaveDataSheetAs(string dataSheetName,ref string strError)
        {
            Logger.logBegin("SaveDataSheetAs",string.Format("Data SheetName:[{0}]", dataSheetName));
            try
            {
                MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);
                bool isOk = false;
                using (var scope = new TransactionScope())
                {
                    long summaryId = BoHelper.CreateTestDataSummary(dataSheetName, objTrans.CurrentDBContext,ref isOk, ref strError);
                    if (!isOk)
                    {
                        ViewModelBase.HintByMessageBox(strError,"ERROR");
                        return false;
                    }
                    // save rel_test_case_test_data_summary
                    BoHelper.CreateRelTCDataSummary(MarsMainWindow.CurrentDatabaseIdx, 
                        summaryId, _testCaseId, objTrans.CurrentDBContext);

                    // save test data
                    isOk = SaveDataSettings(summaryId,objTrans.CurrentDBContext,ref strError);
                    if (!isOk)
                    {
                        ViewModelBase.HintByMessageBox(strError, "ERROR");
                        return false;
                    }
                    _dataSheetId = summaryId;
                    _dataSheetName = GetDataSheetNameNameById(_dataSheetId);
                    this.RefershTitle();

#region transaction part
                    int iCnt = objTrans.CurrentDBContext.SaveChanges();
                    scope.Complete();
#endregion //transaction part
                    Logger.Info("SaveDataSheetAs", string.Format("updated/inserted {0} records", iCnt));
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("SaveDataSheetAs",strError=string.Format("Exception:[{0}], stackTrace:\r\n{1}\r\nInner:{2}",
                    e.Message,
                    e.StackTrace,
                    e.InnerException==null?"":(e.InnerException.InnerException==null?e.InnerException.Message:e.InnerException.InnerException.Message)
                    ));
                return false;
            }
            // save test data summary
            
        }
#region Test steps 的基本数据
        public OnAddTestStepUnitObjEvent onAddTestStepUnitObjHandler = null;
        public OnRequestStartWCFSvcEvent onRequestStartWCFSvcHandler = null;
        public OnRequestWCFSvcStatusEvent onRequestWCFSvcStatusHandler = null;
#endregion
        private string GetPegUrlBasedOnRunOrder(int idx,ref string strError, ref bool isRight )
        {
            int iPegObj = -1;
            for (int i = idx - 1; i >= 0; i--)
            {
                TestStepViewModel objTestStp = _testCases[i];
                if (objTestStp == null) continue;
                if (objTestStp.SelectedKeyword == null) continue;
                if (string.Compare("PEGWINDOW", objTestStp.SelectedKeyword.KeywordName, true) == 0)
                {
                    iPegObj = i;
                    break;
                }
            }
            
            string strPegUrl = "";
            if (iPegObj != -1)
            {
                ///说明是前几个teststeps，如closeapplication, startapplication, waitforseconds, 
                /// 获得pegwindow的URL
                strPegUrl = GetPegwindowURL(_testCases[iPegObj], ref isRight, ref strError);
                isRight = true;
                return strPegUrl;
            }
            else
            {
                isRight = false;
                return null;
            }

        }

        private string LOCKOFTESTSTEP = "NO_REENTRY";
        private void TrialActionStepsButtonHandler(object iRunOrder)
        {
#if _NOQTP
            Monitor.Enter(LOCKOFTESTSTEP);
            try
            {
                RunTestStepFromMarsEng(iRunOrder);
            }
            catch (Exception)
            {
                
            }finally
            {
                Monitor.Exit(LOCKOFTESTSTEP);
            }
            
#else
#if _TestStepUnit
            int iRO = 0;
            if (iRunOrder==null)
            {
                Logger.Warnning("TrialActionStepsButtonHandler","Object run orde is null");
                return;
            }

            if (onAddTestStepUnitObjHandler == null)
            {
                return;
            }
            if (this._testCases == null)
            {
                Logger.Warnning("TrialActionStepsButtonHandler", "_testCases is null");
                return;
            }

            bool isRight = true;
            string strError = "";
            string strPegUrl = "";

            IList<TestStepViewModel> batchTestSteps = null;
            if (iRunOrder is IList<TestStepViewModel>)
            {                
                batchTestSteps = (IList<TestStepViewModel>)iRunOrder;

                List<TestStep4Services> lstStpInfoForWcfSvc = new List<TestStep4Services>();
                foreach (var itmTCViewMode in batchTestSteps)
                {
                    strPegUrl = GetPegUrlBasedOnRunOrder((int)itmTCViewMode.RunOrder, ref strError, ref isRight);
                    TestStep4Services objTestStep = new TestStep4Services();
                    bool isOk = itmTCViewMode.convert2TestStepsvc(objTestStep, strPegUrl, ref strError);
                    if (!isOk)
                    {
                        ViewModelBase.HintByMessageBox(strError, "Error");
                        return;
                    }
                    lstStpInfoForWcfSvc.Add(objTestStep);
                }
                if (onAddTestStepUnitObjHandler != null)
                    onAddTestStepUnitObjHandler(lstStpInfoForWcfSvc);
            }
            else
            {
                isRight = int.TryParse(iRunOrder.ToString(), out iRO);
                if (!isRight)
                {
                    Logger.Warnning("TrialActionStepsButtonHandler", "Object run orde is not an int");
                    return;
                }
                batchTestSteps = new List<TestStepViewModel>() { };
                int idx = iRO - 1;

                if ((idx < 0) || (idx > this._testCases.Count))
                {
                    Logger.Error("TrialActionStepsButtonHandler", "runorder is out of range");
                    return;
                }

                ///获得最近的pegwindow信息
                ///      

                strPegUrl = GetPegUrlBasedOnRunOrder(idx, ref strError, ref isRight);
                TestStepViewModel objCurrentTestStep = _testCases[idx];
                TestStep4Services objTestStep = new TestStep4Services();
                bool isOk = objCurrentTestStep.convert2TestStepsvc(objTestStep, strPegUrl, ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox(strError, "Error");
                    return;
                }
                //添加到堆栈
                if (onAddTestStepUnitObjHandler != null)
                    this.onAddTestStepUnitObjHandler(new List<TestStep4Services>() { objTestStep });
            }
            /// 算法：
            /// 1，判断是否存在已经启动的service，如果不存在，在TestStep模式下启动framework service 
            /// 2，判断是否有客户端连接上，如果没有提示是否启动，如果不启动，退出
            /// 3，设置需要处理的keyword，对象和数据等，通过临界区
            /// 4，等待客户端返回数据
            /// 
            /// 把数据送到堆栈中
            /// 
            
            

            //int iPegObj = -1;       
            //for(int i=idx-1;i>=0;i--)
            //{
            //    TestCaseEditViewModel objTestStp = _testCases[i];
            //    if (objTestStp == null) continue;
            //    if (objTestStp.SelectedKeyword == null) continue;
            //    if (string.Compare("PEGWINDOW", objTestStp.SelectedKeyword.KeywordName,true)==0)
            //    {
            //        iPegObj = i;
            //        break;
            //    }
            //}
            
            //if (iPegObj!=-1)
            //{
            //    ///说明是前几个teststeps，如closeapplication, startapplication, waitforseconds, 
            //    /// 获得pegwindow的URL
            //    strPegUrl = GetPegwindowURL(_testCases[iPegObj],ref isRight, ref strError);
            //}

            

            ///判断是否启动了服务
            /// 
            bool? isServicesStart = onRequestWCFSvcStatusHandler == null ? null:(bool?)onRequestWCFSvcStatusHandler();
            if (isServicesStart==null)
            {
                Logger.Error("TrialActionStepsButtonHandler", strError = "No service methods is assigned");
                ViewModelBase.HintByMessageBox(strError);
                return;
            }
            if (isServicesStart==null? false: !(isServicesStart??false))
            {
                if (this.onRequestStartWCFSvcHandler!=null)
                {
                    isServicesStart = this.onRequestStartWCFSvcHandler(2, ref strError);
                    if (!(isServicesStart??true))
                    {
                        Logger.Error("TrialActionStepsButtonHandler", strError = string.Format("Can't start Automation Test service with error:\r\n[{0}]",strError));
                        ViewModelBase.HintByMessageBox(strError);
                        return;
                    }
                }
            }

            ///需要判断 是否已经启动了qtp和test，如果没有需要重新启动
            /// 
            /// exceptions, when start qtp here if qtp doesn't start. 
            /// using qtp starter with check parameter to do that
            Process objNewProce = new Process { StartInfo = new ProcessStartInfo { FileName = @".\QtpStarter.exe", Arguments = "-Check" } };
            objNewProce.Start();
            //if (!QTPManagement.QTPAppStarts())
            //{
            //    if (QTPManagement.StopQTPFrameWorkThread(ref strError))
            //        MarsUtilities.StartQTPStarer("-TestStep");
            //}
#endif
#endif
            return;
        }
        #if _NOQTP
        private void RunTestStepFromMarsEng(object oRunOrder)
        {
            //get running application
            if (_currentTCApplication == null)
            {
                ViewModelBase.HintByMessageBox("No Application is selected.");
                return;
            }
            Process pcur = Process.GetCurrentProcess();
            Process[] arrPCurrent = Process.GetProcessesByName(_currentTCApplication.PROCESS_IDENTIFIER);
            if (arrPCurrent != null)
                arrPCurrent = arrPCurrent.Where(p => p.SessionId == pcur.SessionId).ToArray();
            if ((arrPCurrent!=null)&&(arrPCurrent.Length>1))
            {
                ViewModelBase.HintByMessageBox(string.Format("There are more than one [{0}] are running, please keep only one runs", _currentTCApplication.APP_SHORT_NAME));
                return;
            }
            int iRunOrder = -1;

            List<TestStepViewModel> lstToRun = new List<TestStepViewModel>();
            if (oRunOrder is TestStepViewModel)
            {
                lstToRun.Add(oRunOrder as TestStepViewModel);
            }
            else
            {
                if (oRunOrder is List<TestStepViewModel>)
                {
                    lstToRun = oRunOrder as List<TestStepViewModel>;
                }
                else
                {
                    Logger.Error("RunTestStepFromMarsEng", string.Format("type TestStepViewModel required, but [{0}] here",oRunOrder==null?"":oRunOrder.GetType().FullName));
                    return;
                }
            }

            string strResult = "", strError = "";
            InjectorMessageAgent.cleanQueuebyName(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME, ref strError);

            for (int i = 0; i < (lstToRun == null ? -1 : lstToRun.Count); i++)
            {
                var oneStep = lstToRun[i];
                if (oneStep == null) continue;

                //if (!int.TryParse(oRunOrder == null ? "-1" : oRunOrder.ToString(), out iRunOrder))
                //{
                //    ViewModelBase.HintByMessageBox(string.Format("Not such runorder:[{0}]", oRunOrder));
                //    return;
                //}
                iRunOrder = (int)oneStep.RunOrder;

                var latestPeg = TestCases.Where(p => (string.Compare(p.SelectedKeyword == null ? "NULL" : p.SelectedKeyword.KeywordName, "Pegwindow", true) == 0) && (p.RunOrder <= iRunOrder))
                    .LastOrDefault();
                if (latestPeg == null)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Can't find pegwindow info for step No:[{0}]", iRunOrder));
                    return;
                }
                iRunOrder = iRunOrder <= 0 ? 0 : iRunOrder - 1;

                var curStep = TestCases[iRunOrder];
                if ((curStep == null) || (curStep.SelectedKeyword == null))
                {
                    ViewModelBase.HintByMessageBox("no Test step is selected?");
                    return;
                }
                

                //firstly , run highlight
                //bool isOk = MarsGuiInjectorAgent.RunOneStepByAgent(
                //    arrPCurrent[0].Id,
                //    _currentTCApplication.PROCESS_IDENTIFIER,
                //    "HIGHLIGHT",
                //    curStep.RowColumn,
                //    latestPeg.SelectedObjectName.QuickAccess,
                //    curStep.SelectedObjectName == null ? "" : curStep.SelectedObjectName.QuickAccess,
                //    curStep.SelectedObjectName.ObjectSwfType,
                //    curStep.DataSet1 == null ? "" : curStep.DataSet1.ToString(), ref strResult, ref strError);
                //if (!isOk)
                //{
                //    ViewModelBase.HintByMessageBox(string.Format("Can't run current step with Error:\r\n{0}",strError));
                //    return;
                //}
                bool isOk = MarsGuiInjectorAgent.RunOneStepByAgent(
                    arrPCurrent[0].Id,
                    _currentTCApplication.PROCESS_IDENTIFIER,
                    curStep.SelectedKeyword.KeywordName,
                    curStep.RowColumn,
                    latestPeg.SelectedObjectName.QuickAccess,
                    curStep.SelectedObjectName == null ? "" : curStep.SelectedObjectName.QuickAccess,
                    curStep.SelectedObjectName.ObjectSwfType,
                    curStep.DataSet1 == null ? "" : curStep.DataSet1.ToString(),
                    ref strResult, ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Can't run current step with Error:\r\n{0}", strError));
                    return;
                }
            }
        }
#endif
        public bool SaveTestStepsAs()
        {
            // For testing only
            string newTestCaseName = "TestCase101";
            string ContextName = newTestCaseName;
            //string searchName = " ";

            MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);
            bool isOk = false;
            string strError = "";
            try
            {
                
#region transaction 
                using (var scope = new TransactionScope())
                {

                    //MarsEntities marsEntitiesNew = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                    T_TEST_CASE_SUMMARYDTO testCase = new T_TEST_CASE_SUMMARYDTO();
                    B_TEST_CASE bTestCase = new B_TEST_CASE(), objTcFromDb;
                    B_TEST_STEPS bTestStep = new B_TEST_STEPS();
                    B_REL_APP_TESTCASE bRelAppTestCase = new B_REL_APP_TESTCASE();

                    bool isTestExist = (objTcFromDb=bTestCase.GetTestCase(MarsMainWindow.CurrentDatabaseIdx, 
                        ContextName, objTrans.CurrentDBContext))!=null;

                    //if (!bTestCase.TestCaseExists(ContextName, objTrans.CurrentDBContext))
                    if (!isTestExist)
                    {
#region old codes to be removed

                        //var testcase = (from t in marsEntities.T_TEST_CASE_SUMMARY
                        //                where t.TEST_CASE_NAME == _testCaseName // Change this !!!!
                        //                select t).FirstOrDefault();
#endregion
                        var testcase = objTcFromDb;

#region old garbage codes
                        long testCaseId = -1;//all database operation should be in business layer//bTestCase.getTestCaseId(objTrans.CurrentDBContext);
                        testCase.TEST_CASE_ID = testCaseId;
                        testCase.TEST_CASE_NAME = ContextName;
                        testCase.TEST_STEP_DESCRIPTION = testcase.TEST_STEP_DESCRIPTION;

                        /// Garbage codes!!!! here!
                        /// Stupid!!! the test case id is new from sequence. No test case - app is created!!!
                        //var relAppTestCase = (from a in marsEntitiesNew.REL_APP_TESTCASE
                        //                      where a.TEST_CASE_ID == testcase.TEST_CASE_ID
                        //                      select a);
                        //foreach (var a in relAppTestCase)
                        //{
                        //    REL_APP_TESTCASEDTO relAppTestCaseDto = new REL_APP_TESTCASEDTO();
                        //    relAppTestCaseDto.TEST_CASE_ID = testCaseId;
                        //    relAppTestCaseDto.RELATIONSHIP_ID = bRelAppTestCase.getRelTestCaseAppId();
                        //    relAppTestCaseDto.APPLICATION_ID = a.APPLICATION_ID;
                        //    marsEntitiesNew.REL_APP_TESTCASE.Add(REL_APP_TESTCASEAssembler.ToEntity(relAppTestCaseDto));
                        //}
#endregion

#region old garbage codes 2
                        //var testCaseTestStep = (from r in marsEntitiesNew.T_TEST_STEPS
                        //                        where r.TEST_CASE_ID == testcase.TEST_CASE_ID
                        //                        select r);
#endregion
                        /// create test case summary first
                        /// 
                        isOk = bTestCase.AddNewTestCase(MarsMainWindow.CurrentDatabaseIdx, 
                            testCase,objTrans.CurrentDBContext,ref strError,ref testCaseId);
                        if (!isOk)
                        {
                            Logger.Error("SaveTestStepsAs", strError=string.Format("Error from AddNewTestCase:{0}",strError));
                            ViewModelBase.HintByMessageBox(strError,"Error");
                            return false;
                        }

#region old garbage codes 3
                        //var testCaseTestStep = (from r in objTrans.CurrentDBContext.T_TEST_STEPS
                        //                        where r.TEST_CASE_ID == _testCaseId
                        //                        select r);
#endregion
                        /// copy all test steps
                        /// 
                        B_TEST_STEPS objStp = new B_TEST_STEPS();
                        isOk = objStp.DuplicateStepsFromSourceTestCase(_testCaseId,testCaseId, objTrans.CurrentDBContext,ref strError);
                        if (!isOk)
                        {
                            Logger.Error("SaveTestStepsAs",string.Format("Error when call DuplicateStepsFromSourceTestCase \r\n{0}",strError));
                            return false;
                        }
                        int iUpdatedCnt = objTrans.CurrentDBContext.SaveChanges();
                        scope.Complete();
                        Logger.Info("SaveTestStepsAs", strError = string.Format("total [{0}] records are inserted/updated", iUpdatedCnt));
                        ViewModelBase.HintByMessageBox(string.Format("Test case [{0}] has been saved. ",TestCaseName),"Hint");
                        return true;
#region trash codes
                        //foreach (var r in testCaseTestStep)
                        //{
                        //    T_TEST_STEPSDTO bTestCaseTestStepsDTo = new T_TEST_STEPSDTO();
                        //    bTestCaseTestStepsDTo.STEPS_ID = BoHelper.GetTestStepsId(objTrans.CurrentDBContext);
                        //    bTestCaseTestStepsDTo.TEST_CASE_ID = testCaseId;
                        //    bTestCaseTestStepsDTo.KEY_WORD_ID = r.KEY_WORD_ID;
                        //    bTestCaseTestStepsDTo.RUN_ORDER = r.RUN_ORDER;
                        //    bTestCaseTestStepsDTo.OBJECT_ID = r.OBJECT_ID;
                        //    bTestCaseTestStepsDTo.COLUMN_ROW_SETTING = r.COLUMN_ROW_SETTING;
                        //    bTestCaseTestStepsDTo.VALUE_SETTING = r.VALUE_SETTING;
                        //    bTestCaseTestStepsDTo.COMMENT = r.COMMENT;
                        //    //marsEntitiesNew.T_TEST_STEPS.Add(T_TEST_STEPSAssembler.ToEntity(bTestCaseTestStepsDTo));
                        //    objTrans.CurrentDBContext.T_TEST_STEPS.Add(T_TEST_STEPSAssembler.ToEntity(bTestCaseTestStepsDTo));
                        //}

                        ///应该先创建test case
                        ///marsEntitiesNew.T_TEST_CASE_SUMMARY.Add(T_TEST_CASE_SUMMARYAssembler.ToEntity(testCase));
                        //try
                        //{
                        //    if (marsEntitiesNew.SaveChanges() > 0)
                        //    {
                        //        MarsTreeView.GetMarsTree();
                        //        System.Windows.MessageBox.Show("Test case successfully saved as", "Test case SaveAs", MessageBoxButton.OK, MessageBoxImage.Information);

                        //        return true;
                        //    }
                        //    else
                        //    {
                        //        marsEntitiesNew = null;
                        //        System.Windows.MessageBox.Show("Error saving test case", "Test case SaveAs", MessageBoxButton.OK, MessageBoxImage.Warning);
                        //        return false;
                        //    }

                        //}
                        //catch (Exception ex)
                        //{
                        //    marsEntitiesNew = null;
                        //    System.Windows.MessageBox.Show(ex.InnerException.ToString(), "Test case saveas", MessageBoxButton.OK, MessageBoxImage.Error);
                        //    return false;
                        //}
#endregion trash codes
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Test case already exists");
                        return false;
                    }                    
                }
#endregion //transaction 
            }
            catch (Exception e)
            {
                Logger.Error("SaveTestStepsAs", strError = string.Format("Exception:[{0}] StackTrace:\r\n{1}",e.Message,e.StackTrace),e);
                ViewModelBase.HintByMessageBox(strError, "Error");
                return false;
            }
        }



        internal bool AutGen_GenStep(string strSwfName, string strType, string strTxt, ref string strError)
        {
            /// steps:
            /// 1, check current pegwindow contains the object with swfName combined with strtype, if not then return strError
            /// 2, create a new row by calling addnew()
            /// 3, set the new row with red boarder
            /// 4, set keyword, object and data
            /// 5, change new row's border back to normal
            /// 

            /**1, check current pegwindow contains the object with swfName combined with strtype, if not then return strError**/

            ObjectName objDesObj = LatestTestStep.SubObjects.Where(p => p.QuickAccess == null ? false : p.QuickAccess.IndexOf(strSwfName, StringComparison.CurrentCultureIgnoreCase) >= 0).FirstOrDefault();
            if (objDesObj == null)
            {
                /// no such 
                /// 
                strError = string.Format("Please check whether object [{0}] belong to current Parent window:[{1}]", strSwfName, LatestTestStep.SelectedObjectName.ObjName);
                Logger.Error("AutGen_GenStep", string.Format("{0} \t{1} \tError:{2}", "AutGen_GenStep", DateTime.Now, strError));
                return false;
            }

            /// disable anomation
            /// 
            //var lstAnm = TestCases.Where(p => p.AutoGenStatus == true);
            //foreach (TestCaseEditViewModel objItm in lstAnm)
            //{
            //    objItm.AutoGenStatus = false;
            //    objItm.
            //}

            //TestCaseEditViewModel objNewItem = this.addNewRow(LatestTestStep.TestCaseName);
            TestStepViewModel objNewItem = this.addNewRow(LatestTestStep.TestCaseId);


            objNewItem.AutoGenStatus = true;
            /// As the row data is bindinged by test cases. Changing color of the new row will be implemented outside
            /// 
            long iKeyWordId = -1;
            bool isFindObj = AutoGen_DealWithKeyWord(objNewItem, strType, ref strError, ref iKeyWordId);
            if (!isFindObj)
            {
                return false;
            }
            objNewItem.SelectedKeyword = objNewItem.Keywords.Where(p => p.Id == iKeyWordId).SingleOrDefault();
            /// Select Objects
            objNewItem.SelectedObjectName = objNewItem.Objects.Where(p => p.Id == objDesObj.Id).SingleOrDefault();

            /// Set Dataset 1
            /// 
            if (!string.IsNullOrEmpty(strTxt))
            {
                objNewItem.DataSet1 = strTxt;
            }
            //TestCases.Add(objNewItem);
            return true;// objNewItem   
        }

        private bool AutoGen_DealWithKeyWord(TestStepViewModel objNewItem, string strType, ref string strError, ref long iKeywordId)
        {
            if (objNewItem == null)
            {
                strError = "No new Empty Row Data";
                Logger.Error("AutoGen_DealWithKeyWord", string.Format("{0} \t{1} \tError:{2}", "AutGen_GenStep", DateTime.Now, strError));
                return false;
            }
            ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> lstType = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeListEx(MarsMainWindow.CurrentDatabaseIdx);
            B_GUI_COMPONENT_TYPE_DIC objTypeDic = lstType.Where(p => string.Compare(p.TYPE_NAME, strType, true) == 0).SingleOrDefault();
            if (objTypeDic == null)
            {
                strError = string.Format("No such type exists :[{0}]", strType);
                Logger.Error("AutoGen_DealWithKeyWord", string.Format("{0} \t{1} \tError:{2}", "AutGen_GenStep", DateTime.Now, strError));
                return false;
            }
            string strKeyWord = "";
            switch (strType.ToUpper())
            {
                case "SWFEDIT":
                    strKeyWord = "FillEdit";
                    break;
                case "SWFCOMBOBOX":
                    strKeyWord = "SelectDropDown";
                    break;
                default:
                    strError = string.Format("Currently, Auto Generating Test Scripts doesn't support this Type:[{0}]", strType);
                    Logger.Error("AutoGen_DealWithKeyWord", string.Format("{0} \t{1} \tError:{2}", "AutGen_GenStep", DateTime.Now, strError));
                    return false;
            }
            Keyword objCurrentKey = objNewItem.Keywords == null ? null : objNewItem.Keywords.Where(p => string.Compare(p.KeywordName, strKeyWord, true) == 0).SingleOrDefault();
            if (objCurrentKey == null)
            {
                strError = string.Format("Can't find such keyword from List :[{0}]", strKeyWord);
                Logger.Error("AutoGen_DealWithKeyWord", string.Format("{0} \t{1} \tError:{2}", "AutGen_GenStep", DateTime.Now, strError));
                return false;
            }
            iKeywordId = objCurrentKey.Id;
            return true;
        }




#region Script Auto Generation
        private TestStepViewModel LatestTestStep = null;
        public String CurrentLatestPegAccessQuickInfo
        {
            get
            {
                /// 这里 返回完整的pegwindow的定义，故而，客户端无须再次拼组
                /// 

                if (LatestTestStep == null) return null;
                if (LatestTestStep.SelectedObjectName == null) return null;
                bool isOk = false;string strError = "";
                string strQuickAceessFullPath = LatestTestStep.GetCurrentObjectQuickURL(ref isOk, ref strError);
                if (isOk) return strQuickAceessFullPath;
                return null;
                //return LatestTestStep.SelectedObjectName.QuickAccess;
            }
        }
        internal bool IsUnderAutoGen()
        {
            Logger.Info("IsUnderAutoGen", string.Format("IsUnderAutoGen\tBegins:[{0}]", DateTime.Now));

            // the check whether a peg wind
            LatestTestStep = TestCases.Where(p => string.Compare(p.SelectedKeyword == null ? "" : p.SelectedKeyword.KeywordName.ToUpper(), "PEGWINDOW") == 0).LastOrDefault();
            if (LatestTestStep != null)
            {
                LatestTestStep.TestCaseId = this._testCaseId;
                //LatestTestStep.TestCaseName = this._testCaseName;
            }
            return LatestTestStep != null;
        }
#endregion //Script Auto Generation

        private MarsQueueManagement<KeyValuePair<int, DateTime>> ChangeedDataCellQueues = null;

        public bool LoadDataFromDatabaseCommandImpl()
        {
            this.CleanORFill = true;
            return LoadDataFromDatabaseCommandTmp();
        }

        public bool LoadDataFromDatabaseCommandTmp()
        {
            Logger.logBegin("LoadDataFromDatabaseCommandImpl");
            try
            {
                /// 算法：
                /// 1，循环处理teststeps
                /// 2，获得处理的对象
                /// 3，获得相关的配置文件--文件是peg.object.mars.cfg位置
                /// 4，分析配置文件
                /// 5，是否存在需要处理的参数，如果存在，弹出窗口，让用户输入，目前采用xml文件作为实际参数文件
                /// 6，逐步运行sql，其他dll
                /// 备注：
                /// 这里应该让用户选择配置文件系列的位置。这样可以针对不同配置信息选取多个test case等
                LoadDataFromDataBaseMgr objLoadDataFromDBMgr = new LoadDataFromDataBaseMgr();
                string strCurrentAutoLoadDBCfgPath = System.Configuration.ConfigurationManager.AppSettings[MarsConstants.CNST_AUTO_LOAD_DATAFROMDB_PATH];
                if (string.IsNullOrEmpty(strCurrentAutoLoadDBCfgPath))
                {
                    strCurrentAutoLoadDBCfgPath = string.Format("..\\{0}\\", MarsConstants.CNST_AUTO_LOAD_DATA_DIRECTORY);                    
                }
                /// 3，获得相关的配置文件--文件是peg.object.mars.cfg位置
                string strCurrentPath = SystemCommonUtil.CombinePath(SystemCommonUtil.GetCurrentPathDir(), strCurrentAutoLoadDBCfgPath);

                string strError = "";
                List<LoadDataFromDBBasicStpConfigInfo> lstObjConfig = new List<LoadDataFromDBBasicStpConfigInfo>();
                if (_testCases == null)
                {
                    ViewModelBase.HintByMessageBox(strError = "No Test steps in current test case.", "WARNNING");
                    Logger.Warnning("LoadDataFromDatabaseCommandImpl", strError);
                    return true;
                }

                /// 2，获得处理的对象
                /// 
                string strCurrentPeg = "";
                ///lstObjectRefCnt 标识了所有对象和对象的层次信息,对象的名称由对象名称+字段名称组成
                List<KeyValuePair<string, int>> lstObjectRefCnt = new List<KeyValuePair<string, int>>();
                bool isOk = false;
#region check loop 

#region Get data source setting from database
                List<long> arrListAppIds = _testCases.SelectMany(p => p.GetAssignedAppId()).Distinct().ToList();
                arrListAppIds.RemoveAll(p => p == -1);
                //获得所有的对象, Name_id
                List<long> arrListNameIds = _testCases.Select(p => p.SelectedObjectName == null ? -1 : p.SelectedObjectName.NameId).Distinct().ToList();
                List<B_REGISTED_OBJECT> lstObj = B_REGISTED_OBJECT.GetObjectsInfoByAppIdsAndNameIds(MarsMainWindow.CurrentDatabaseIdx, 
                    arrListAppIds,arrListNameIds);
                //过滤datasource为空的
                lstObj.RemoveAll(p => (p == null) || (p.OBJ_DATA_SRC == null));                
                ///获得所有的
                if (!this.CleanORFill)
                {
                    var stpTmp = from s in _testCases
                                 from o in lstObj
                                 where s.ObjectNameId == o.OBJECT_NAME_ID
                                 select s;
                    stpTmp.ToList().ForEach(p=>p.DataSet1="");
                    return true ;
                }
                string strPath = System.IO.Path.Combine(SystemCommonUtil.GetCurrentPathDir(), string.Format("..\\{0}\\", MarsConstants.CNST_AUTO_LOAD_DATA_DIRECTORY));

                lstObj.ForEach(p => {
                    if (p == null) return;
                    bool isOktmp = true;
                    string strTmp = "";

                    MarsObjectDataSourceMapping objOM = MarsObjectDataSourceMapping.LoadFromBytes(p.OBJ_DATA_SRC, ref isOktmp, ref strTmp);
                    if (isOktmp)
                    {
                        objOM.Write2File(strPath, p.OBJECT_TYPE, p.OBJECT_HAPPY_NAME, ref strTmp, ref isOktmp);
                    }
                });

#endregion

                foreach (var stp in _testCases )
                {
                    if (stp == null) continue;
                    if ((stp.SelectedObjectName == null)||(string.IsNullOrEmpty(stp.SelectedObjectName.ObjName))) continue;
                    if (stp.SelectedKeyword == null) continue;
                    if (string.Compare("Pegwindow", stp.SelectedKeyword.KeywordName,true)==0)
                    {
                        strCurrentPeg = stp.SelectedObjectName.ObjName;
                        continue;
                    }
                    string strObjectName = stp.SelectedObjectName.ObjName, strObjAttached="";
                    int iMode = -1,iGroup=0, iTypeMode=-1;
                    if (string.Compare("FillTable", stp.SelectedKeyword.KeywordName, true) == 0)
                    {
                        ///因为对于filltable而言，需要考虑几个要素
                        /// 1，block，通常对于grid数据而言，会一次操作几个cell，在换行之前，这些cell应该属于同一行
                        /// 2，字段信息。字段格式比较复杂。对于存在group的，和非group的不一样
                        /// 
                        
                        if (((iTypeMode=TigerMarsUtil.RegularTest("DYNAMICROWS.*", stp.RowColumn)?1:0)==1 )||
                            ((iTypeMode=TigerMarsUtil.RegularTest(@"\S+;\S+:\S+.*-\S+.*", stp.RowColumn)?2:0)==2))
                        {
                            if (iTypeMode == 1)
                            {
                                strObjAttached = SystemCommonUtil.ExtractFieldNameFromParameterForKeyword(stp.RowColumn, ref isOk, ref strError);
                                if (!isOk)
                                {
                                    Logger.Error("LoadDataFromDatabaseCommandImpl", strError = string.Format("parameter is not right with error when extracting field:\r\n[{0}]", strError));
                                    ViewModelBase.HintByMessageBox(strError, "ERROR");
                                    return false;
                                }
                            }
                            else
                            {
                                if (iTypeMode==2)
                                {
                                    strObjAttached = SystemCommonUtil.ExtractFieldNameFromParameterGroupModeForKeyword(stp.RowColumn, ref isOk,ref strError);
                                    if (!isOk)
                                    {
                                        Logger.Error("LoadDataFromDatabaseCommandImpl", strError = string.Format("parameter is not right with error when extracting field:\r\n[{0}]", strError));
                                        ViewModelBase.HintByMessageBox(strError, "ERROR");
                                        return false;
                                    }
                                }
                                else
                                {
                                    Logger.Error("LoadDataFromDatabaseCommandImpl", strError = string.Format("parameter is not right with error when extracting field:\r\n[{0}]", strError));
                                    ViewModelBase.HintByMessageBox(strError, "ERROR");
                                    return false;
                                }
                            }
                            string strTmp = string.Format("{0}.{1}", strObjectName, strObjAttached);
                            iMode = strTmp.Split('.').Length;
                            var objRef = lstObjectRefCnt.Where(p=>string.Compare(p.Key, strObjectName,true)==0).FirstOrDefault();
                            if (default(KeyValuePair<string, int>).Equals(objRef))
                            {
                                lstObjectRefCnt.Add(new KeyValuePair<string, int>(strObjectName, iGroup = 0));
                            }
                            else {
                                lstObjectRefCnt.Remove(objRef);
                                lstObjectRefCnt.Add(new KeyValuePair<string, int>(objRef.Key,iGroup=(objRef.Value+1)));
                            }
                        }
                    }
                    else
                    {
                        iMode = -1;
                        iGroup = -1;
                    }

                    lstObjConfig.Add(new LoadDataFromDBBasicStpConfigInfo()
                    {
                        RunOrder = stp.RunOrder,
                        PegName = strCurrentPeg,
                        ObjectName = strObjectName,
                        ObjectNameMode = iMode,
                        ObjectGroupId = iGroup,
                        ObjAttachedInfo = strObjAttached,
                        FilePath = strCurrentPath
                    });
                }
#endregion //check loop 
                List<MarsObjDataSrcMappingParameter> lstParaParamters=null;
                isOk = objLoadDataFromDBMgr.AlystCfgFiles(lstObjConfig,out lstParaParamters,ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Configuration files are with error:\r\n[{0}]",strError));
                    return false;
                }
                //是否从文件中获得参数数据
                bool isParametersFromFile = false;
                string strDataSetNamePattern = "";
                ///存在参数需求 才需要弹出窗口
                if ((lstParaParamters != null) && (lstParaParamters.Count > 0)&& (!isParametersFromFile))
                {

                    /// 显示参数设置窗口
                    /// 
                    Mars.Dialog.autoLoadData.AutoLoadDataParameterSettingModal objAutoParaSettingModal = new Dialog.autoLoadData.AutoLoadDataParameterSettingModal();
                    /// 将参数数据抽取给 datacontext
                    /// 
                    objAutoParaSettingModal.ParametersNeedSet = new ObservableCollection<MarsObjDataSrcMappingParameter>(lstParaParamters);

                    Mars.Dialog.autoLoadData.AutoLoadDataParameterSetting wndParaSetting = new Mars.Dialog.autoLoadData.AutoLoadDataParameterSetting();
                    wndParaSetting.DataContext = objAutoParaSettingModal;
                    bool? resultDialog = wndParaSetting.ShowDialog();
                    if (!(resultDialog ?? false))
                    {
                        Logger.Error("LoadDataFromDatabaseCommandImpl", strError = "Parameter setting is cancelled.");
                        return false;
                    }
                    strDataSetNamePattern = objAutoParaSettingModal.DatasetNamePattern;
                }


                /// 获取数据
                /// 首先获得参数
                /// 然后获得数据
                /// 此处是，参数文件通过配置文件给予
                ///
                
                int iParameterCnt = -1;
                if (isParametersFromFile)
                {
                    isOk = objLoadDataFromDBMgr.LoadParaMetersFromConfigFile(lstObjConfig, strCurrentPath, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("LoadDataFromDatabaseCommandImpl", strError);
                        ViewModelBase.HintByMessageBox("Error when analysis paramter file.\r\n" + strError, "ERROR");
                        return false;
                    }
                    lstParaParamters = null;
                    iParameterCnt = objLoadDataFromDBMgr.GetParameterCountAfterAnalysis();
                }
                else {
                    ///参数通过动态设定
                    /// 
                    iParameterCnt = lstParaParamters == null ? -1 : lstParaParamters.Count;
                 }
                /// 读取Xml的connection文件
                /// 
                isOk = objLoadDataFromDBMgr.LoadConnectionInformation(strCurrentPath,ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Error when load connection information:\r\n[{0}]", strError));
                    return false;
                }
                /// 循环读取数据
                /// 
                if (_testCases == null) return true;
                TestStepViewModel currentTestStepTmp = null;
                //if (ChangeedDataCellQueues == null)
                    ChangeedDataCellQueues = MarsQueueManagement<KeyValuePair<int, DateTime>>.GetInstance();
                //ChangeedDataCellQueues.CleanQueue();

                int iMaxParameterLoopCnt = objLoadDataFromDBMgr.getMaxParameterRowCnt(isParametersFromFile, lstParaParamters);

                for (int i = 0; i < iParameterCnt; i++)
                {
                    objLoadDataFromDBMgr.initGroupedCachedData();

                    //目前只支持一个参数
                    foreach (var itm in lstObjConfig)
                    {
                        if (itm == null) continue;
                        if (itm.ObjectDataSourceMapping == null) continue;
                        if ((currentTestStepTmp = _testCases.Where(p => p.RunOrder == itm.RunOrder).FirstOrDefault()) == null) continue;
                        
                        string strDataForTestStep = objLoadDataFromDBMgr.FetchDataForTestStepByCfg(i, itm, ref isOk, ref strError, lstParaParamters);
                        if ((!isOk))
                        {
                            ViewModelBase.HintByMessageBox(strError, "ERROR");
                            //return false;
                            continue;
                        }
                        
                        currentTestStepTmp.DataSet1 = strDataForTestStep;
                        currentTestStepTmp.currentDataCellColor = Brushes.DeepSkyBlue;
                        ChangeedDataCellQueues.Add(new KeyValuePair<int, DateTime>((int)itm.RunOrder, DateTime.Now));
                        
                        //Thread.Sleep(200);
                        //currentTestStepTmp.currentDataCellColor = null;
                    }
                }
                
                //new Thread(new ThreadStart(new Action(()=>{
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(delegate () {
                    int iCnt = ChangeedDataCellQueues.GetCount();
                    int iSleep = 200;
                    for (int i=0;i<iCnt;i++)
                    {
                        KeyValuePair<int, DateTime> currentItmToChangeColor =  ChangeedDataCellQueues.Peek();
                        if (currentItmToChangeColor.Equals(default(KeyValuePair<int, DateTime>))) continue;
                        var objStp = _testCases.Where(p => p.RunOrder == currentItmToChangeColor.Key).FirstOrDefault();
                        if (objStp == null) continue;
                        TimeSpan t = DateTime.Now - currentItmToChangeColor.Value;

                        if ((iSleep = (int)(t.TotalMilliseconds-50))>0)
                        {
                            Thread.Sleep(50);
                        }
                        objStp.currentDataCellColor = null;
                        
                    }
                }), DispatcherPriority.Background, null);

                return true;
            }
            finally
            {
                Logger.logEnd("LoadDataFromDatabaseCommandImpl");
            }
            
        }

        private bool AllStepsAreValidate(ref string strError)
        {
            if ((TestCases == null) || (TestCases.Count <= 0))
            {
                strError = "No Test steps.";
                return false;
            }
            List<TestStepViewModel> errorStps = new List<TestStepViewModel>();
            string strTmpError = "";
            //foreach(var stpItm in TestCases)
            for (int i= 0 ;i < TestCases.Count;i++)
            {
                var stpItm = TestCases[i];
                stpItm.RunOrder = i + 1;
                if (stpItm == null) continue;
                if (stpItm.SelectedKeyword == null)
                {
                    strTmpError = string.Format("No. {0} step has not Selected Keyword ", stpItm.RunOrder);
                    if (string.IsNullOrEmpty(strError))
                        strError = strTmpError;
                    else strError = string.Format("{0}/r/n{1}", strError, strTmpError);
                    stpItm.CurrentRowColor = Brushes.Red;
                    errorStps.Add(stpItm);
                    continue;
                }
                if (stpItm.SelectedObjectName == null)
                {
                    var objNotRequired = keywordNotRequireObject.Where(p => string.Compare(stpItm.SelectedKeyword.KeywordName, p, true) == 0)
                        .FirstOrDefault();
                    if (objNotRequired == null)
                    {
                        //说明不存在于不需要对象的
                        strTmpError = string.Format("No. {1} Keyword:[{0}] requires objects, but it hasn't", stpItm.RunOrder, stpItm.SelectedKeyword.KeywordName);
                        if (string.IsNullOrEmpty(strError))
                            strError = strTmpError;
                        else strError = string.Format("{0}/r/n{1}", strError, strTmpError);
                        stpItm.CurrentRowColor = Brushes.Red;
                        errorStps.Add(stpItm);                        
                    }
                    continue;
                }

            }
            if (errorStps.Count > 0)
            {
                //将背景色改变
                
                return false;
            }
            return true;
        }

        // Todo add validation's while saving the Test Steps
        public bool SaveTestSteps()
        {
            try
            {
                bool isOk = false;

                ///before save, check whether all stest test stpes are validate
                ///
                string strError = "";


                if (TestCases != null)
                {
                    TestStepViewModel itmStp = TestCases.Where(p => p.SelectedKeyword == null).FirstOrDefault();
                    if ((itmStp != null) && ((itmStp.SelectedKeyword == null) || (string.IsNullOrEmpty(itmStp.SelectedKeyword.KeywordName))))
                    {
                        ViewModelBase.HintByMessageBox("Empty Test step exists. Please remove it first.");
                        return false;
                    }
                }
                else
                {
                    HintByMessageBox("No Test steps!");
                    return false;
                }
                isOk = AllStepsAreValidate(ref strError);
                if (!isOk)
                {
                    HintByMessageBox(strError, "Error");
                    return false;
                }

                // this code will be used for TC import
                MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TimeSpan(0,20,0)))
                {
                    
                    if (isTestCaseCreated == false)
                    { 
                        if (CreateTestCase(objTrans.CurrentDBContext) == false)
                            return false;
                        else
                        {
                            this._dataSheetId = SharedDataSetViewModel.CreateDataSet(_testCaseId, _testCaseName, objTrans.CurrentDBContext);
                            isTestCaseCreated = true;
                        }
                    }
                    else
                    {
                        if (this._dataSheetId <= -1)
                        {
                            this._dataSheetId = SharedDataSetViewModel.CreateDataSet(_testCaseId, _testCaseName, objTrans.CurrentDBContext);
                        }
                    }


                    List<B_TEST_STEPS> bTestStepsList = new List<B_TEST_STEPS>();
                    List<B_TEST_DATA_SETTING> bTestDataSettingList = new List<B_TEST_DATA_SETTING>();

                    //BoHelper.SaveChanges();

                    SharedDataSetViewModel.Delete(MarsMainWindow.CurrentDatabaseIdx, deletedDataSettigs,objTrans.CurrentDBContext,false);
                    deletedDataSettigs.Clear();

                    // Save deleeted rows to database
                    try
                    {
                        BoHelper.DeleteTestSteps(MarsMainWindow.CurrentDatabaseIdx, 
                            deletedTestSteps,objTrans.CurrentDBContext);
                        deletedTestSteps.Clear();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("SaveTestSteps",string.Format("Exception:[{0}] StackTrace:\r\n{1}",ex.Message,ex.StackTrace),ex); 
                        ViewModelBase.HintByMessageBox(string.Format("Error while saving deleted records.\r\n{0}", ex.ToString()));
                        return false;
                    }

                    // Save TEST_STEPS, test data settings

                    int runOrderDelta = 0;
                    foreach (var teststeps in TestCases)
                    {
                        // If this is a new test step 
                        if (BoHelper.GetTestStepByID(MarsMainWindow.CurrentDatabaseIdx, teststeps.StepNo,objTrans.CurrentDBContext) == null)
                        {
                            // AF added the next  lines to exclude entries that have no keyword at all
                            if (teststeps.SelectedKeyword == null || !TestStepViewModel.staticKeywordNames.Contains(teststeps.SelectedKeyword._keywordName) &&
                                (teststeps.SelectedKeyword == null || teststeps.SelectedObjectName == null))
                            {
                                if (teststeps.SelectedKeyword == null)
                                {
                                    runOrderDelta++;
                                    continue;
                                }
                                if (keywordNotRequireObject.Where(s => string.Compare(s, teststeps.SelectedKeyword._keywordName, true) == 0).Count() == 0)
                                {
                                    runOrderDelta++;
                                    continue;
                                }
                            }

                            B_TEST_STEPS bTestSteps = new B_TEST_STEPS();
                            bTestSteps.TEST_CASE_ID = _testCaseId;
                            bTestSteps.STEPS_ID = teststeps.StepNo<=0? BoHelper.GetTestStepsId(objTrans.CurrentDBContext):teststeps.StepNo; //assign new stepsId
                            teststeps.StepNo = bTestSteps.STEPS_ID;
                            bTestSteps.KEY_WORD_ID = teststeps.SelectedKeyword.Id;
                            /// Name_id will replace it
                            //if (teststeps.SelectedObjectName != null)
                            //  bTestSteps.OBJECT_ID = teststeps.SelectedObjectName.Id;
                            bTestSteps.COLUMN_ROW_SETTING = teststeps.RowColumn;
                            bTestSteps.VALUE_SETTING = teststeps.RowColumnValue;
#if v_16AndUp
                            bTestSteps.OBJECT_NAME_ID = teststeps.SelectedObjectName == null ? -1 : teststeps.SelectedObjectName.NameId;
#endif

                            if (teststeps.IsRunChecked)
                            {
                                bTestSteps.IS_RUNNABLE = 1;
                                
                            }
                            else
                            {
                                bTestSteps.IS_RUNNABLE = 0;
                            }

                            bTestSteps.RUN_ORDER = teststeps.RunOrder - runOrderDelta;

                            bTestSteps.COMMENT = teststeps.Comment;
#region trash codes
                            // The following block was replced by call to SharedDataSetViewModel Save()
                            /*
                            List<B_TEST_DATA_SETTING> testStepList = new List<B_TEST_DATA_SETTING>();
                            for (int count = 1; count <= MarsConstants.NumberOfDataSetColumns; count++)
                            {
                                var testDataSetting = AddTestDataSettings(teststeps, count, bTestSteps);
                                if (testDataSetting != null)
                                    //testStepList.Add(testDataSetting);
                                    bTestDataSettingList.Add(testDataSetting);
                            }

                             */
#endregion //trash codes
                            bTestStepsList.Add(bTestSteps);
                        }
                        else
                        {
                            // Updating an existing test step
                            //var bSelTestStep = BoHelper.GetTestStepByID(teststeps.StepNo);
                            var selTestStep = BoHelper.GetTestStepEntByID(teststeps.StepNo,objTrans.CurrentDBContext);

                            if (teststeps.SelectedKeyword != null)
                                selTestStep.KEY_WORD_ID = teststeps.SelectedKeyword.Id;
                            if (teststeps.SelectedObjectName != null)
                                selTestStep.OBJECT_ID = teststeps.SelectedObjectName.Id;
                            selTestStep.COLUMN_ROW_SETTING = teststeps.RowColumn;
                            selTestStep.VALUE_SETTING = teststeps.RowColumnValue;

                            if (teststeps.IsRunChecked)
                                selTestStep.IS_RUNNABLE = 1;
                            else
                                selTestStep.IS_RUNNABLE = 0;

                            selTestStep.RUN_ORDER = teststeps.RunOrder;
                            // Console.WriteLine("Setting selTestStep.RUN_ORDER = " + selTestStep.RUN_ORDER);
                            short sDirection = (short)(teststeps.IsSkipForDataset ? 4 : 0);
                            
                            selTestStep.COMMENT = teststeps.Comment;
                            selTestStep.COLUMN_ROW_SETTING = teststeps.RowColumn ?? "";
#if v_16AndUp
                            selTestStep.OBJECT_NAME_ID = teststeps.SelectedObjectName == null ? -1 : teststeps.SelectedObjectName.NameId; ;
#endif
#region trash codes
                            // The following block was replced by call to SharedDataSetViewModel Save()
                            /*
                            List<B_TEST_DATA_SETTING> bTestDataSettingLocalList = BoHelper.LoadBOTestDataSettings(teststeps.StepNo);
                            for (int count = 1; count <= MarsConstants.NumberOfDataSetColumns; count++)
                            {
                                var testDataSetting = bTestDataSettingLocalList.FirstOrDefault(a => a.LOOP_ID == count);
                                if (testDataSetting == null)
                                {
                                    testDataSetting = AddTestDataSettings(teststeps, count, bSelTestStep);
                                    if (testDataSetting != null)
                                        //selTestStep.TEST_DATA_SETTING.Add(testDataSetting);
                                        bTestDataSettingList.Add(testDataSetting);
                                }
                                else
                                {
                                    UpdateTestDataSettings(teststeps, count, ref testDataSetting);
                                }
                            }
                            */
#endregion //trash codes

                        }

                    } //forloop close


                    //if (BoHelper.SaveTestStepsAndData(bTestStepsList, bTestDataSettingList) > 0)
                    BoHelper.SetToSaveTestSteps(bTestStepsList, objTrans.CurrentDBContext);
#region stupid code --noted by tiger
                    //if (BoHelper.SetToSaveTestSteps(bTestStepsList,objTrans.CurrentDBContext) > 0)
                    //{

                    //    // MarsTreeView.GetMarsTree();
                    //    //LoadTestSteps(_testCaseId);
                    //    System.Windows.MessageBox.Show("Test Steps saved successfully");
                    //    _isModifiedTestCase = false;
                    //    //return true;
                    //}
                    //else
                    //{
                    //    System.Windows.MessageBox.Show("Failed to save Test Steps");
                    //    //return false;
                    //}
#endregion

                    // Save data after saving steps!                    
                    //bool isOk = SharedDataSetViewModel.Save_Ex(_testCaseId, _dataSheetId, TestCases, ref strError);
                    isOk = SharedDataSetViewModel.Save(MarsMainWindow.CurrentDatabaseIdx,_testCaseId, _dataSheetId, TestCases,objTrans.CurrentDBContext,ref strError);
                    if (isOk)
                    {
                        int iCnt = objTrans.CurrentDBContext.SaveChanges();
                        scope.Complete();
                        Logger.Info("SaveTestSteps", string.Format("Changed [{0}] records", iCnt));
                        
                    }
                    else
                    {
                        ViewModelBase.HintByMessageBox("Can't save Test steps\r\n"+strError);
                    }
                }
                if (isOk)
                {
                    MarsDBGlobe_Cache.UpdateAppTestCaseCache();
                    ViewModelBase.HintByMessageBox("Success saved.");
                }
            }
            catch (Exception e)
            {
                Logger.Error("SaveTestSteps", string.Format("Exception:[{0}]", e.Message), e);
                System.Windows.MessageBox.Show("Exception: \n" + e.ToString());
            }
            return true;
        }

        private bool CreateTestCase(MarsEntities objDbCntx)
        {
            bool success = false;
            B_TEST_CASE bTestCase = new B_TEST_CASE();
            B_REL_APP_TESTCASE bRelAppTestCase = new B_REL_APP_TESTCASE();
            WindowsIdentity ident = WindowsIdentity.GetCurrent();
            if (!bTestCase.TestCaseExists(MarsMainWindow.CurrentDatabaseIdx, _testCaseName))
            {
                bTestCase.TEST_CASE_ID = bTestCase.getTestCaseId(MarsMainWindow.CurrentDatabaseIdx, objDbCntx);
                bTestCase.TEST_CASE_NAME = _testCaseName;
                bTestCase.TEST_STEP_DESCRIPTION = _testCaseName;
                bTestCase.TEST_STEP_CREATOR = ident.Name.ToString();
                bTestCase.TEST_STEP_CREATE_TIME = DateTime.Now;
                ///marsEntities.T_TEST_CASE_SUMMARY.Add(T_TEST_CASE_SUMMARYAssembler.ToEntity(bTestCase));
                /// 
                objDbCntx.Set<T_TEST_CASE_SUMMARY>();
                objDbCntx.T_TEST_CASE_SUMMARY.Add(T_TEST_CASE_SUMMARYAssembler.ToEntity(bTestCase));

                REL_APP_TESTCASE relAppTestCase = new REL_APP_TESTCASE();
                relAppTestCase.RELATIONSHIP_ID = bRelAppTestCase.getRelTestCaseAppId(MarsMainWindow.CurrentDatabaseIdx, objDbCntx);
                relAppTestCase.APPLICATION_ID = ApplicationId;
                relAppTestCase.TEST_CASE_ID = bTestCase.TEST_CASE_ID;
                ///marsEntities.REL_APP_TESTCASE.Add(relAppTestCase);
                /// 
                objDbCntx.Set<REL_APP_TESTCASE>();
                objDbCntx.REL_APP_TESTCASE.Add(relAppTestCase);

                _testCaseId = bTestCase.TEST_CASE_ID;
#region remove for transaction 
                //if (marsEntities.SaveChanges() > 0)
                //{
                //    success = true;
                //    MarsDBGlobe_Cache.UpdateAppTestCaseCache();
                //}
#endregion //remove for transaction 

            }
            return success;
        }

        public bool SaveDataSettings(long summaryId,MarsEntities objDbCntx,ref string strError)
        {
            //         List<B_TEST_STEPS> bTestStepsList = new List<B_TEST_STEPS>();
            List<B_TEST_DATA_SETTING> bTestDataSettingList = new List<B_TEST_DATA_SETTING>();

            // save delete datasettings -- those that are deleted due to change of keyword or object

            foreach (int stepId in deletedDataSettigs)
            {
                BoHelper.DeleteDataSettings(MarsMainWindow.CurrentDatabaseIdx, stepId, objDbCntx);
            }
            // We need to save changes at this point (old DataSettings) so that we don't accidentally delete the new dataSettings

            //BoHelper.SaveChanges();
            deletedDataSettigs.Clear();

            // Save deleeted rows to database
            try
            {
                BoHelper.DeleteTestSteps(MarsMainWindow.CurrentDatabaseIdx, deletedTestSteps, objDbCntx);
                //foreach (var stepId in deletedTestSteps)
                //{
                //    BoHelper.DeleteTestStep(stepId);
                //}
                deletedTestSteps.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error("SaveDataSettings",strError=string.Format("Savign Delete records Exception [{0}] stackTrace:\r\n{1}",ex.Message, ex.StackTrace),ex);
                //System.Windows.MessageBox.Show("Error while saving deleted records.", ex.ToString());
                return false;
            }

            // Save TEST_STEPS, test data settings

            int runOrderDelta = 0;
            foreach (var teststeps in TestCases)
            {
                teststeps.InitDataSets();
                // If this is a new test step 
                if (BoHelper.GetTestStepByID(MarsMainWindow.CurrentDatabaseIdx, teststeps.StepNo, objDbCntx) == null)
                {
                    // AF added the next  lines to exclude entries that have no keyword at all
                    if (teststeps.SelectedKeyword == null ||
                        !TestStepViewModel.staticKeywordNames.Contains(teststeps.SelectedKeyword._keywordName) && (teststeps.SelectedKeyword == null || teststeps.SelectedObjectName == null))
                    {
                        runOrderDelta++;
                        continue;
                    }

                    B_TEST_STEPS bTestSteps = new B_TEST_STEPS();
                    bTestSteps.TEST_CASE_ID = _testCaseId;
                    bTestSteps.STEPS_ID = BoHelper.GetTestStepsId(objDbCntx); //assign new stepsId

                    bTestSteps.KEY_WORD_ID = teststeps.SelectedKeyword.Id;
                    if (teststeps.SelectedObjectName != null)
                        bTestSteps.OBJECT_ID = teststeps.SelectedObjectName.Id;
                    bTestSteps.COLUMN_ROW_SETTING = teststeps.RowColumn;
                    bTestSteps.VALUE_SETTING = teststeps.RowColumnValue;

                    if (teststeps.IsRunChecked)
                        bTestSteps.IS_RUNNABLE = 1;
                    else
                        bTestSteps.IS_RUNNABLE = 0;

                    bTestSteps.RUN_ORDER = teststeps.RunOrder - runOrderDelta;

                    bTestSteps.COMMENT = teststeps.Comment;

                    List<B_TEST_DATA_SETTING> testStepList = new List<B_TEST_DATA_SETTING>();
                    for (int count = 1; count <= MarsConstants.NumberOfDataSetColumns; count++)
                    {
                        var testDataSetting = AddTestDataSettings(teststeps, count, bTestSteps, summaryId,objDbCntx);
                        if (testDataSetting != null)
                            //testStepList.Add(testDataSetting);
                            bTestDataSettingList.Add(testDataSetting);
                    }

                    // bTestStepsList.Add(bTestSteps);
                }
                else
                {
                    // Updating an existing test step
                    var selTestStep = BoHelper.GetTestStepByID(MarsMainWindow.CurrentDatabaseIdx, teststeps.StepNo, objDbCntx);

                    //Logger.Info("SaveDataSettings", "Processing step #" + selTestStep.STEPS_ID);

                    if (teststeps.SelectedKeyword != null)
                        selTestStep.KEY_WORD_ID = teststeps.SelectedKeyword.Id;
                    if (teststeps.SelectedObjectName != null)
                        selTestStep.OBJECT_ID = teststeps.SelectedObjectName.Id;
                    selTestStep.COLUMN_ROW_SETTING = teststeps.RowColumn;
                    selTestStep.VALUE_SETTING = teststeps.RowColumnValue;

                    if (teststeps.IsRunChecked)
                        selTestStep.IS_RUNNABLE = 1;
                    else
                        selTestStep.IS_RUNNABLE = 0;

                    selTestStep.RUN_ORDER = teststeps.RunOrder;
                    selTestStep.COMMENT = teststeps.Comment;

                    List<B_TEST_DATA_SETTING> bTestDataSettingLocalList = BoHelper.LoadBOTestDataSettings(MarsMainWindow.CurrentDatabaseIdx, 
                        teststeps.StepNo, summaryId, objDbCntx);
                    for (int count = 1; count <= MarsConstants.NumberOfDataSetColumns; count++)
                    {
                        var testDataSetting = bTestDataSettingLocalList.FirstOrDefault(a => a.LOOP_ID == count);
                        if (testDataSetting == null)
                        {

                            testDataSetting = AddTestDataSettings(teststeps, count, selTestStep, summaryId,objDbCntx);
                            if (testDataSetting != null)
                            {
                                //testDataSetting.DATA_SETTING_ID = BoHelper.GetDataSettingId();
                                bTestDataSettingList.Add(testDataSetting);
                            }

                        }
                        else
                        {
                            UpdateTestDataSettings(teststeps, count, ref testDataSetting);
                            bTestDataSettingList.Add(testDataSetting);
                        }
                    }
                }

            } //forloop close

            // if (marsEntities.SaveChanges() > 0)

            if (BoHelper.SaveDataSettings(MarsMainWindow.CurrentDatabaseIdx, bTestDataSettingList,objDbCntx) > 0)
            {
                //MarsTreeView.GetMarsTree();
                //LoadTestSteps(_testCaseId, _testCaseName);
                System.Windows.MessageBox.Show("Test Data saved successfully");
                return true;
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to save Test Data");
                return false;
            }
        }

        internal bool CopyDataSettings(ref string strError)
        {
            Logger.Info("CopyDataSettings", "Begins");
            ///steps:
            /// 1, check whether all selected rows are saved
            /// 2, find all assigned datasettings 
            /// 3, fill
            /// 
            if (_selectedTestCases == null)
            {
                Logger.Error("CopyDataSettings", strError = "No Row is selected. No data will be set.");
                return false;
            }

            if (_selectedTestCases.Count<=0)
            {
                strError="Select at least one test step before copy data to other datasets. ";
                return false;
            }

            foreach (var itm in _selectedTestCases)
            {
                if (itm.StepNo > 0) continue;
                Logger.Warnning("CopyDataSettings", strError = "Test steps should be saved first. ");
                return false;
            }
            try
            {
                //check 
                //this.DataSheetId
                //this._testCaseId
                List<B_TEST_STEPS> lstStpsToDeal = new List<B_TEST_STEPS>();
                if (!ViewModelBase.QuestionByMessageBox(string.Format("Other dataset(s) information assigned to current TestCase :[{0}] will be updated or inserted. \r\nAre you sure to Continue?", this.TestCaseName), "Question"))
                {
                    strError = "Cancelled by User action.";
                    return false;
                }
                foreach (var itm in _selectedTestCases)
                {
                    if (itm.SelectedKeyword == null) continue;
                    B_TEST_STEPS objStp = new B_TEST_STEPS();
                    objStp.COLUMN_ROW_SETTING = itm.RowColumn;
                    objStp.COMMENT = itm.Comment;
                    objStp.IS_RUNNABLE = itm.IsRunChecked ? 0 : 1;
                    objStp.KEY_WORD_ID = itm.SelectedKeyword.Id;
                    objStp.OBJECT_ID = itm.SelectedObjectName == null ? null : (long?)itm.SelectedObjectName.Id;
                    objStp.OBJECT_NAME_ID = itm.ObjectNameId;
                    objStp.RUN_ORDER = itm.RunOrder;
                    objStp.STEPS_ID = itm.StepNo;
                    objStp.TEST_CASE_ID = itm.TestCaseId;
                    objStp.VALUE_SETTING = itm.DataSet1 == null ? "" : itm.DataSet1.ToString();

                    objStp.OBJECT_NAME = itm.SelectedObjectName == null ? "" : itm.SelectedObjectName.ObjName;
                    objStp.KEYWORD_NAME = itm.SelectedKeyword == null ? "" : itm.SelectedKeyword.KeywordName;
                    objStp.ASSIGNED_DATA = itm.DataSet1 == null ? "" : itm.DataSet1.ToString();

                    lstStpsToDeal.Add(objStp);
                }
                var q = (from itm in _selectedTestCases
                         select new
                         {
                             data = itm.DataSet1 == null ? "" : itm.DataSet1.ToString(),
                             run_order = itm.RunOrder
                         }).ToList();
                List<KeyValuePair<long, string>> lstKeyData = new List<KeyValuePair<long, string>>();
                foreach (var itm in q)
                {
                    lstKeyData.Add(new KeyValuePair<long, string>(itm.run_order, itm.data));
                }
                bool isRslt = B_TEST_DATA_SETTING.CopyDataSettings(MarsMainWindow.CurrentDatabaseIdx, this._dataSheetId, lstStpsToDeal, ref strError);
                if (!isRslt)
                {
                    Logger.Error("CopyDataSettings", string.Format("Error from CopyDataSettings [{0}]", strError));
                    return false;
                }
                Logger.Info("CopyDataSettings", "Copied datasets to data.");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CopyDataSettings", strError = string.Format("Exception when Copy DataSettings:[{0}] \r\nStackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }

        }


        /*
       /// Old way using entities directly
        public bool SaveTestStepsUsingEntities()
        {
            // Save deleeted rows to database
            try
            {
                foreach (var stepId in deletedTestSteps)
                {
                    BoHelper.DeleteTestStep(stepId);
                }
                deletedTestSteps.Clear();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error while saving deleted records.", ex.ToString());
            }

            // Save TEST_STEPS, test data setting
            B_TEST_STEPS bTestSteps = new B_TEST_STEPS();

            int runOrderDelta = 0;
            foreach (var teststeps in TestCases)
            {
                if (marsEntities.T_TEST_STEPS.FirstOrDefault(x => x.STEPS_ID == teststeps.StepNo) == null)
                {
                    // AF added the next  lines to exclude entries that have no keyword at all
                    if (teststeps.SelectedKeyword == null ||
                        !staticKeywordNames.Contains(teststeps.SelectedKeyword._keywordName) && (teststeps.SelectedKeyword == null || teststeps.SelectedObjectName == null))
                    {
                        runOrderDelta++;
                        continue;
                    }


                    //if (!staticKeywordNames.Contains(teststeps.SelectedKeyword._keywordName) && (teststeps.SelectedKeyword == null || teststeps.SelectedObjectName == null))
                    //    continue;

                    B_TEST_STEPS tTestSteps = new B_TEST_STEPS();
                    tTestSteps.TEST_CASE_ID = _testCaseId;
                    tTestSteps.STEPS_ID = BoHelper.GetTestStepsId(); //assign new stepsId
                    tTestSteps.KEY_WORD_ID = teststeps.SelectedKeyword.Id;
                    if (teststeps.SelectedObjectName != null)
                        tTestSteps.OBJECT_ID = teststeps.SelectedObjectName.Id;
                    tTestSteps.COLUMN_ROW_SETTING = teststeps.RowColumn;
                    tTestSteps.VALUE_SETTING = teststeps.RowColumnValue;

                    if (teststeps.IsRunChecked)
                        tTestSteps.IS_RUNNABLE = 1;
                    else
                        tTestSteps.IS_RUNNABLE = 0;

                    tTestSteps.RUN_ORDER = teststeps.RunOrder - runOrderDelta;

                    tTestSteps.COMMENT = teststeps.Comment;

                    List<B_TEST_DATA_SETTING> testStepList = new List<B_TEST_DATA_SETTING>();
                    for (int count = 1; count <= MarsConstants.NumberOfDataSetColumns; count++)
                    {
                        var testDataSetting = AddTestDataSettings(teststeps, count, tTestSteps);
                        if (testDataSetting != null)
                            testStepList.Add(testDataSetting);
                    }
                    tTestSteps.TEST_DATA_SETTING = testStepList;
                    marsEntities.T_TEST_STEPS.Add(tTestSteps);
                }
                else
                {
                    var selTestStep = marsEntities.T_TEST_STEPS.FirstOrDefault(x => x.STEPS_ID == teststeps.StepNo);

                    if (teststeps.SelectedKeyword != null)
                        selTestStep.KEY_WORD_ID = teststeps.SelectedKeyword.Id;
                    if (teststeps.SelectedObjectName != null)
                        selTestStep.OBJECT_ID = teststeps.SelectedObjectName.Id;
                    selTestStep.COLUMN_ROW_SETTING = teststeps.RowColumn;
                    selTestStep.VALUE_SETTING = teststeps.RowColumnValue;

                    if (teststeps.IsRunChecked)
                        selTestStep.IS_RUNNABLE = 1;
                    else
                        selTestStep.IS_RUNNABLE = 0;

                    selTestStep.RUN_ORDER = teststeps.RunOrder;
                    selTestStep.COMMENT = teststeps.Comment;

                    for (int count = 1; count <= MarsConstants.NumberOfDataSetColumns; count++)
                    {
                        var testDataSetting = selTestStep.TEST_DATA_SETTING.FirstOrDefault(a => a.LOOP_ID == count);
                        if (testDataSetting == null)
                        {
                            testDataSetting = AddTestDataSettings(teststeps, count, selTestStep);
                            if (testDataSetting != null)
                                selTestStep.TEST_DATA_SETTING.Add(testDataSetting);
                        }
                        else
                        {
                            UpdateTestDataSettings(teststeps, count, ref testDataSetting);
                        }
                    }
                }

            } //forloop close

            if (marsEntities.SaveChanges() > 0)
            {
                MarsTreeView.GetMarsTree();
                LoadTestSteps(_testCaseId, _testCaseName);
                System.Windows.MessageBox.Show("Test Steps saved successfully");
                return true;
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to save Test Steps");
                return false;
            }
        }
        */
        ///

        private static B_TEST_DATA_SETTING AddTestDataSettings(TestStepViewModel teststeps, int count, B_TEST_STEPS bTestSteps, 
            long summaryId, MarsEntities objDbCntx=null)
        {
            B_TEST_DATA_SETTING testDataSetting = null;
            string[] format = new string[] {"M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                         "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                         "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                         "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                         "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};
            DateTime datetime;

            if (teststeps.DataSets[count] != null)
            {
                testDataSetting = SetTestDataSettings(count, bTestSteps, summaryId, objDbCntx);
                if (DateTime.TryParseExact(teststeps.DataSets[count].ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                    testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                else
                    testDataSetting.DATA_VALUE = teststeps.DataSets[count].ToString();
            }

            return testDataSetting;
        }

        private static B_TEST_DATA_SETTING AddTestDataSettings(TestStepViewModel teststeps, int count, B_TEST_STEPS bTestSteps)
        {
            B_TEST_DATA_SETTING testDataSetting = null;
            string[] format = new string[] {"M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                         "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                         "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                         "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                         "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};
            DateTime datetime;
            switch (count)
            {
                case 1:
                    if (teststeps.DataSet1 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet1.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet1.ToString();
                    }
                    break;
                case 2:
                    if (teststeps.DataSet2 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet2.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet2.ToString();
                    }
                    break;
                case 3:
                    if (teststeps.DataSet3 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet3.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet3.ToString();
                    }
                    break;
                case 4:
                    if (teststeps.DataSet4 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet4.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet4.ToString();
                    }
                    break;
                case 5:
                    if (teststeps.DataSet5 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet5.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet5.ToString();
                    }
                    break;
                case 6:
                    if (teststeps.DataSet6 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet6.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet6.ToString();
                    }
                    break;
                case 7:
                    if (teststeps.DataSet7 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet7.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet7.ToString();
                    }
                    break;
                case 8:
                    if (teststeps.DataSet8 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet8.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet8.ToString();
                    }
                    break;
                case 9:
                    if (teststeps.DataSet9 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet9.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet9.ToString();
                    }
                    break;
                case 10:
                    if (teststeps.DataSet10 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet10.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet10.ToString();
                    }
                    break;

                case 11:
                    if (teststeps.DataSet11 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet11.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet11.ToString();
                    }
                    break;
                case 12:
                    if (teststeps.DataSet12 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet12.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet12.ToString();
                    }
                    break;
                case 13:
                    if (teststeps.DataSet13 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet13.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet13.ToString();
                    }
                    break;
                case 14:
                    if (teststeps.DataSet14 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet14.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet14.ToString();
                    }
                    break;
                case 15:
                    if (teststeps.DataSet15 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet15.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet15.ToString();
                    }
                    break;
                case 16:
                    if (teststeps.DataSet16 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet16.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet16.ToString();
                    }
                    break;
                case 17:
                    if (teststeps.DataSet17 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet17.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet17.ToString();
                    }
                    break;
                case 18:
                    if (teststeps.DataSet18 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet18.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet18.ToString();
                    }
                    break;
                case 19:
                    if (teststeps.DataSet19 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet19.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet19.ToString();
                    }
                    break;
                case 20:
                    if (teststeps.DataSet20 != null)
                    {
                        testDataSetting = SetTestDataSettings(count, bTestSteps);
                        if (DateTime.TryParseExact(teststeps.DataSet20.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet20.ToString();
                    }
                    break;
            }
            return testDataSetting;
        }

        private static void UpdateTestDataSettings(TestStepViewModel teststeps, int count, ref B_TEST_DATA_SETTING testDataSetting)
        {
            string[] format = new string[] {"M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                         "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                         "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                         "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                         "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};
            DateTime datetime;
            switch (count)
            {
                case 1:
                    if (teststeps.DataSet1 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet1.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet1.ToString();
                    }
                    break;
                case 2:
                    if (teststeps.DataSet2 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet2.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet2.ToString();
                    }
                    break;
                case 3:
                    if (teststeps.DataSet3 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet3.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet3.ToString();
                    }
                    break;
                case 4:
                    if (teststeps.DataSet4 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet4.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet4.ToString();
                    }
                    break;
                case 5:
                    if (teststeps.DataSet5 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet5.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet5.ToString();
                    }
                    break;
                case 6:
                    if (teststeps.DataSet6 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet6.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet6.ToString();
                    }
                    break;
                case 7:
                    if (teststeps.DataSet7 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet7.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet7.ToString();
                    }
                    break;
                case 8:
                    if (teststeps.DataSet8 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet8.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet8.ToString();
                    }
                    break;
                case 9:
                    if (teststeps.DataSet9 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet9.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet9.ToString();
                    }
                    break;
                case 10:
                    if (teststeps.DataSet10 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet10.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet10.ToString();
                    }
                    break;

                case 11:
                    if (teststeps.DataSet11 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet11.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet11.ToString();
                    }
                    break;
                case 12:
                    if (teststeps.DataSet12 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet12.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet12.ToString();
                    }
                    break;
                case 13:
                    if (teststeps.DataSet13 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet13.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet13.ToString();
                    }
                    break;
                case 14:
                    if (teststeps.DataSet14 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet14.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet14.ToString();
                    }
                    break;
                case 15:
                    if (teststeps.DataSet15 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet15.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet15.ToString();
                    }
                    break;
                case 16:
                    if (teststeps.DataSet16 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet16.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet16.ToString();
                    }
                    break;
                case 17:
                    if (teststeps.DataSet17 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet17.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet17.ToString();
                    }
                    break;
                case 18:
                    if (teststeps.DataSet18 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet18.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet18.ToString();
                    }
                    break;
                case 19:
                    if (teststeps.DataSet19 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet19.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet19.ToString();
                    }
                    break;
                case 20:
                    if (teststeps.DataSet20 != null)
                    {
                        if (DateTime.TryParseExact(teststeps.DataSet20.ToString(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                            testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                        else
                            testDataSetting.DATA_VALUE = teststeps.DataSet20.ToString();
                    }
                    break;
            }
        }
        private static B_TEST_DATA_SETTING SetTestDataSettings(int count, B_TEST_STEPS bTestSteps)
        {
            B_TEST_DATA_SETTING testDataSetting = new B_TEST_DATA_SETTING();
            testDataSetting.DATA_SETTING_ID = BoHelper.GetDataSettingId(MarsMainWindow.CurrentDatabaseIdx); //assign new test data setting Id
            testDataSetting.STEPS_ID = bTestSteps.STEPS_ID;
            testDataSetting.LOOP_ID = count;
            return testDataSetting;
        }

        private static B_TEST_DATA_SETTING SetTestDataSettings(int count, B_TEST_STEPS bTestSteps, long summaryId, MarsEntities objDbCntx)
        {
            B_TEST_DATA_SETTING testDataSetting = new B_TEST_DATA_SETTING();
            testDataSetting.DATA_SETTING_ID = BoHelper.GetDataSettingId(MarsMainWindow.CurrentDatabaseIdx, objDbCntx); //assign new test data setting Id
            testDataSetting.STEPS_ID = bTestSteps.STEPS_ID;
            testDataSetting.LOOP_ID = count;
            testDataSetting.DATA_SUMMARY_ID = summaryId;
            return testDataSetting;
        }


        //public void addNewRows(string testCaseName, int count)
        public void addNewRows(long testcaseId, int count)
        {
            for (int i = 0; i < count; i++)
                addNewRow(testcaseId);
        }


        public TestStepViewModel addNewRow(long testCaseId,int runOrder=-1)
        {
#if PERFORMANCE_TRACKING
            long tm = DateTime.Now.Ticks;
            Logger.Info("----性能记录---", string.Format("current :{0}", tm));
#endif
            TestStepViewModel newTestStepRowVM = null;
            long iRunOrder = 1;
            if (TestCases.Count == 0)
            {
                newTestStepRowVM = new TestStepViewModel(new TestStepViewModel(), 1, testCaseId, ref _testCases);
            }
            else
            {
#region Tiger edit
                /// Firstly, check whether is an insert mode or append mode
                /// 
                if (runOrder == -1)
                {
                    if ((SelectedTestCases != null) && (SelectedTestCases.Count > 0))
                    {
                        iRunOrder = SelectedTestCases[SelectedTestCases.Count - 1].RunOrder + 1;
                    }
                    else
                    {
                        iRunOrder = TestCases.Count + 1;
                    }
                }
                else
                {
                    iRunOrder = runOrder;
                }

                // for a new row, the objects should be get from last pegwindow
                //var lastRowViewmodel = TestCases.FirstOrDefault(x => x.RunOrder == iRunOrder - 1);
                var lastRowViewmodel = TestCases.LastOrDefault(x => 
                    (x.RunOrder <= iRunOrder - 1)
                    &&(x.SelectedKeyword!=null)
                    &&(string.Compare(x.SelectedKeyword.KeywordName, "Pegwindow",true)==0));
                //if (lastRowViewmodel == null)
                //    lastRowViewmodel = TestCases[TestCases.Count - 1];
                //lastRowViewmodel.TestCaseName = this._testCaseName;
                if (lastRowViewmodel==null)
                {
                    lastRowViewmodel = TestCases[TestCases.Count - 1];
                    lastRowViewmodel.TestCaseName = this._testCaseName;
                }
                
                newTestStepRowVM = new TestStepViewModel( lastRowViewmodel, iRunOrder, testCaseId, ref _testCases);
#endregion //Tiger edit
            }
            if (iRunOrder - 1 >= TestCases.Count)
                TestCases.Add(newTestStepRowVM);
            else
                TestCases.Insert((int)((iRunOrder>0)?(iRunOrder - 1):0), newTestStepRowVM);

            ///这里可能存在问题
            /// 由于某些原因，run order并非连续 因此这里最好是将所有的所有的runorder重新处理
            for (int i=0;i< _testCases.Count; i++)
            {
                int iCorrectedRunOrder = i + 1;
                if (_testCases[i] == null) continue;
                if (_testCases[i].RunOrder == iCorrectedRunOrder) continue;
                _testCases[i].RunOrder = iCorrectedRunOrder;
            }
#region trash code
            /// correct all Runorder after iRunOrder
            /// 
            //IEnumerable<TestStepViewModel> arrTestStepToBeCorrected = from oneItem in TestCases
            //                                                          where oneItem.RunOrder >= iRunOrder
            //                                                          select oneItem;

            //for (int i = 0; i < arrTestStepToBeCorrected.Count(); i++)
            //{
            //    arrTestStepToBeCorrected.ElementAt(i).RunOrder = iRunOrder + i + 1;
            //}
#endregion
#if PERFORMANCE_TRACKING
            tm = DateTime.Now.Ticks - tm;
            Logger.Info("----性能记录---", string.Format("cost :{0}", tm));
#endif
            return newTestStepRowVM;
        }



        private string getCurrentPegWindow()
        {
            string pegWindow = "";
            if ((SelectedTestCases==null)||(SelectedTestCases.Count==0))
            {
                return null;
            }

            long selectedRow = SelectedTestCases[0].RunOrder;
            TestStepViewModel[] ar = TestCases.ToArray();

            if (selectedRow >= ar.Length)
                selectedRow = ar.Length - 1;

            for (long i = selectedRow; i >= 0; i--)
            {


                TestStepViewModel tc = ar[i];

                if (tc.SelectedKeyword != null)
                    Console.WriteLine(tc.RunOrder + " -- " + tc.StepNo + " - " + tc.SelectedKeyword._keywordName);

                if (tc.SelectedKeyword != null &&
                    tc.SelectedKeyword.KeywordName.Equals("PegWindow") &&
                    tc.SelectedObjectName != null)
                {
                    pegWindow = tc.SelectedObjectName.ObjName;
                    Console.WriteLine(tc.RunOrder + " -- " + tc.StepNo + " - " + tc.SelectedKeyword._keywordName + " - " + tc.SelectedObjectName.ObjName);
                    break;
                }
                //Console.WriteLine(tc.RunOrder + " -- " + tc.StepNo + " - " + tc.SelectedKeyword._keywordName + " - " + tc.SelectedObjectName.ObjName);
            }

            return pegWindow;

        }


        private TestStepViewModel getCurrentPegWindowViewModel()
        {
            TestStepViewModel pegWindow = null;

            long selectedRow = SelectedTestCases[0].RunOrder;


            TestStepViewModel[] ar = TestCases.ToArray();

            if (selectedRow >= ar.Length)
                selectedRow = ar.Length - 1;

            for (long i = selectedRow; i >= 0; i--)
            {

                TestStepViewModel tc = ar[i];
                if (tc.SelectedKeyword != null && tc.SelectedKeyword._keywordName.Equals("PegWindow"))
                {
                    pegWindow = tc;
                    //Console.WriteLine(tc.RunOrder + " -- " + tc.StepNo + " - " + tc.SelectedKeyword._keywordName + " - " + tc.SelectedObjectName.ObjName);
                    break;
                }
                //Console.WriteLine(tc.RunOrder + " -- " + tc.StepNo + " - " + tc.SelectedKeyword._keywordName + " - " + tc.SelectedObjectName.ObjName);
            }

            return pegWindow;

        }


        private void deleteSelectedRows()
        {
            _isModifiedTestCase = true;
            foreach (TestStepViewModel selectedRow in SelectedTestCases)
            {
                deletedTestSteps.Add(selectedRow.StepNo);
                // AF added next line to delete data related to tis row
                deletedDataSettigs.Add(selectedRow.StepNo);

                TestCases.Remove(selectedRow);
            }

            for (int i = 0; i < TestCases.Count; i++)
            {
                TestCases[i].RunOrder = i + 1;
            }
        }

        private void moveSelectedRows(string direction)
        {
            if (SelectedTestCases.Count == 1)
            {
                int indexOfSelectedRow = TestCases.IndexOf(SelectedTestCases[0]);
                foreach (TestStepViewModel selectedRow in SelectedTestCases)
                {
                    if (direction == "up")
                    {
                        if (indexOfSelectedRow > 0)
                        {
                            TestCases.Move(indexOfSelectedRow, indexOfSelectedRow - 1);
                            TestCases[indexOfSelectedRow].RunOrder = indexOfSelectedRow + 1;
                            TestCases[indexOfSelectedRow - 1].RunOrder = indexOfSelectedRow;
                        }
                    }
                    else
                    {
                        if (indexOfSelectedRow < TestCases.Count - 1)
                        {
                            TestCases.Move(indexOfSelectedRow, indexOfSelectedRow + 1);
                            TestCases[indexOfSelectedRow].RunOrder = indexOfSelectedRow + 1;
                            TestCases[indexOfSelectedRow + 1].RunOrder = indexOfSelectedRow + 2;
                        }
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Select a single row to move", "Test Steps", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool _isCopyAsCVS;
        public bool isCopyAsCVS
        {
            get
            {
                return _isCopyAsCVS;
            }
            set
            {
                if (_isCopyAsCVS!=value)
                {
                    _isCopyAsCVS = value;
                    OnPropertyChanged("isCopyAsCVS"); 
                }
            }
        }


        private bool ImportDataTableIntoTestCaseEditor(DataTable dt, ref string strError)
        {
            Logger.logBegin("ImportDataTableIntoTestCaseEditor");
            try
            {
                B_REGISTED_OBJECT regObj = new B_REGISTED_OBJECT();
                List<long> appIds = TestcaseAppliedAppIds.Where(p => p != null).Cast<long>().ToList();
                int currentRunOrder = (int)(this.SelectedTestCases == null ? (this.TestCases == null ? 1 : this.TestCases.Count) : (this.SelectedTestCases.LastOrDefault().RunOrder + 1));
                ObservableCollection<Keyword> defaultPegKeyowrds = TestStepViewModel.PopulateKeywordsById(TestCaseId);
                TestStepViewModel previousPegRow = null;
                List<long> objectTypeIds = new List<long>();
                
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string strObjectName = dt.Rows[i][cnst_arr_Standard_headerForCopy[1]] as string;
                    string strKeyword = dt.Rows[i][cnst_arr_Standard_headerForCopy[0]] as string;
                    string strPara = dt.Rows[i][cnst_arr_Standard_headerForCopy[2]] as string;
                    string strComment = dt.Rows[i][cnst_arr_Standard_headerForCopy[3]] as string;
                    string strTmpBool = dt.Rows[i][cnst_arr_Standard_headerForCopy[4]] as string;

                    bool isSkipMark;
                    if (string.IsNullOrEmpty(strTmpBool))
                        isSkipMark = false;
                    else
                    {
                        if (!bool.TryParse(strTmpBool, out isSkipMark))
                        {
                            isSkipMark = false;
                        }
                    }
                    
                    string strData = dt.Rows[i][cnst_arr_Standard_headerForCopy[5]] as string; ;

                    TestStepViewModel newTestStep = new TestStepViewModel();
                    newTestStep.TestCaseId = this.TestCaseId;
                    newTestStep.RunOrder = currentRunOrder + i;


                    if ((i == 0)||(string.Compare(strKeyword, "Pegwindow", true) == 0))
                    {
                        //the first row should be Pegwindow
                        if (string.Compare(strKeyword, "Pegwindow", true) != 0)
                        {
                            strError = string.Format("First Row should be Pegwindow. but it is [{0}]", strKeyword);
                            return false;
                        }
                        if (string.IsNullOrEmpty(strObjectName))
                        {
                            strError = "Object for pegwindow should not be empty";
                            return false;
                        }

                        //get pegiwndow information from cache
                        List<B_REGISTED_OBJECT> regObjectList = regObj.GetRegisterdObjectsByObjectParentFromCache(MarsMainWindow.CurrentDatabaseIdx, 
                            strObjectName, appIds);
                        if (regObjectList == null)
                        {
                            strError = string.Format("Can't find pegobjects [{0}] in applications ids:[{1}]", strObjectName, string.Join(",", appIds));
                            return false;
                        }
                        newTestStep.Keywords = defaultPegKeyowrds;
                        newTestStep.SelectedKeyword = defaultPegKeyowrds.FirstOrDefault(p => string.Compare("Pegwindow", p._keywordName, true) == 0);
                        if (newTestStep.SelectedKeyword == null)
                        {
                            strError = string.Format("Can't find Pegwindow from Keyword List:[{0}]", string.Join(",", defaultPegKeyowrds.Select(p => p._keywordName)));
                            return false;
                        }
                        //当newTestStep.SelectedKeyword设置时候，objects 已经设置 
                        //newTestStep.Objects = 
                        if ((newTestStep.Objects == null) && (!string.IsNullOrEmpty(strObjectName)))
                        {
                            strError = string.Format("Object list for pegwindow [{0}] is null.", strObjectName);
                            return false;
                        }
                        objectTypeIds.Clear();


                        newTestStep.SelectedObjectName = newTestStep.Objects.FirstOrDefault(p => string.Compare(p.ObjName, strObjectName, true) == 0);
                        if (newTestStep.SelectedObjectName == null)
                        {
                            strError = string.Format("Can't find object [{0}] from Objects.", strObjectName);
                            return false;
                        }
                        previousPegRow = newTestStep;
                    }
                    else
                    {
                        if (previousPegRow == null)
                        {
                            strError = "previous Pegwindow is null";
                            return false;
                        }
                        newTestStep.addObjectInfo(previousPegRow, TestCaseId);
                        List<ObjectName> lstTmpObjs = newTestStep.PopulateObjectsByObjectParent(previousPegRow.SelectedObjectName.ObjName, TestCaseId);
                        ObservableCollection<ObjectName> tmpObjects = new ObservableCollection<ObjectName>(lstTmpObjs);
                        //objectTypeIds = lstTmpObjs==null?new List<long>():lstTmpObjs.Select(p=>p.ObjectType)
                        B_KEYWORD objKeyword = new B_KEYWORD();
                        Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> tmpKeywordNames = objKeyword.GetKeyWordNamesByObjectTypeId(newTestStep._objectTypeIds);
                        var filteredKeywords = new ObservableCollection<Keyword>();

                        foreach (var keyword in tmpKeywordNames.Keys)
                        {
                            filteredKeywords.Add(new Keyword(keyword, tmpKeywordNames[keyword], "", _testCaseId));
                        }

                        newTestStep.Keywords = filteredKeywords;
                        newTestStep.Objects = tmpObjects;

                        newTestStep.SelectedKeyword = filteredKeywords.FirstOrDefault(p => string.Compare(p.KeywordName, strKeyword, true) == 0);
                        if (newTestStep.SelectedKeyword == null)
                        {
                            strError = string.Format("No such Keyword Name [{0}] exists from Keywords", strKeyword);
                            return false;
                        }
                        if (!string.IsNullOrEmpty(strObjectName))
                        {
                            newTestStep.SelectedObjectName = tmpObjects.FirstOrDefault(p => string.Compare(p.ObjName, strObjectName, true) == 0);
                            if (newTestStep.SelectedObjectName == null)
                            {
                                strError = string.Format("No such object [{0}] is found for peg :[{1}]", strObjectName, previousPegRow.SelectedObjectName.ObjName);
                                return false;
                            }
                        }
                        else
                        {
                            newTestStep.ForceSetSelectObjectNameNull();
                        }

                    }

                    newTestStep.RowColumn = strPara;
                    newTestStep.DataSet1 = strData;
                    newTestStep.IsSkipForDataset = isSkipMark ;
                    TestCases.Insert((int)newTestStep.RunOrder - 1, newTestStep);
                }

                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                Logger.Error("ImportDataTableIntoTestCaseEditor", strError, e) ;
                return false;
            }
            finally
            {
                Logger.logEnd("ImportDataTableIntoTestCaseEditor");
            }
        }

        private bool DealWithCSVFormatPaste(string strFromClip,ref string strError)
        {
            //首先存到临时的csv文件中
            string strShellDir = AppConfigReader.GetShellDir();
            if (string.IsNullOrEmpty(strShellDir))
            {
                strError = "No Shell Dir is configed.";
                return false;
            }
            Excel.Application exlapp = null;
            try
            {
                string strFileName = Path.Combine(strShellDir, ".\\tmpcsvFile\\");
                if (!Directory.Exists(strFileName))
                {
                    Directory.CreateDirectory(strFileName);
                }
                strFileName = Path.Combine(strShellDir, ".\\tmpcsvFile\\", "tmpcsv.csv");

                if (File.Exists(strFileName))
                {
                    try
                    {
                        File.Delete(strFileName);
                    }
                    catch (Exception)
                    {
                        strError = string.Format("Can't delete file:[{0}], try to close other MARS instance first", strFileName);
                        return false;
                    }
                }
                System.IO.File.WriteAllLines(strFileName, new string[] { strFromClip });
                exlapp= new Excel.Application();
                exlapp.Visible = true;
                exlapp.Workbooks.Open(strFileName);

                var actWb = exlapp.ActiveWorkbook;
                Excel.Worksheet sht = actWb.Sheets["tmpcsv"];
                DataTable dt = new DataTable();
                for (int i=0; i<sht.UsedRange.Rows.Count;i++)
                {
                    if (i==0)
                    {
                        for (int ic =0;ic<sht.UsedRange.Columns.Count;ic++)
                        {
                            dt.Columns.Add(sht.UsedRange.Cells[i+1, ic+1].Text);
                        }
                    }
                    else
                    {
                        DataRow oneRow = dt.NewRow();
                        for (int ic = 0; ic < dt.Columns.Count; ic++) {
                            oneRow[dt.Columns[ic]] = sht.UsedRange.Cells[i+1, ic+1].Text;
                            string strData = oneRow[dt.Columns[ic]] == null ? "" : oneRow[dt.Columns[ic]].ToString();
                            if (strData.StartsWith("#"))
                            {
                                object ov = sht.UsedRange.Cells[i + 1, ic + 1].Value2;
                                if (ov!=null)
                                {
                                    oneRow[dt.Columns[ic]] = ov.ToString();
                                }
                            }
                        }

                        dt.Rows.Add(oneRow);
                    }
                }
                exlapp.Quit();
                exlapp = null;
                return ImportDataTableIntoTestCaseEditor(dt, ref strError);
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]", e.Message);
                ViewModelBase.HintByMessageBox(strError);
                return false;
            }
            finally
            {
                if (exlapp!=null)
                {
                    exlapp.Quit();
                }
            }
            
        }

        private void PasteNew()
        {
            Logger.logBegin("PasteNew");
            string strError = "";
            try
            {
                string strFromClip = System.Windows.Clipboard.GetData(System.Windows.DataFormats.Text) as string;
                if (string.IsNullOrEmpty(strFromClip))
                {
                    ViewModelBase.HintByMessageBox("no Data get from Clipboard!");
                    return;
                }
                if (System.Windows.Clipboard.GetData(System.Windows.DataFormats.CommaSeparatedValue) != null)
                {
                    strFromClip = System.Windows.Clipboard.GetData(System.Windows.DataFormats.CommaSeparatedValue) as string;
                    if (strFromClip.ToUpper().StartsWith(GetStandardCVSHeader().ToUpper()))
                    {   
                        if (!DealWithCSVFormatPaste(strFromClip, ref strError))
                        {
                            ViewModelBase.HintByMessageBox(string.Format("Data pasting has Error:[{0}]", strError));
                            return;
                        }
                        return;
                    }
                    else
                    {
                        ViewModelBase.HintByMessageBox(string.Format("CSV format content should include header and like:\r\n{0}", StoryboardColl.GetStandardCVSHeader()));
                        return;
                    }
                }


                // it should at least two rows, the first row
                DataSet ds = new DataSet();
                
                try
                {
                    //XmlDocument xmlDoc = new XmlDocument();
                    //xmlDoc.LoadXml(strFromClip);
                    StringReader txtReader = new StringReader(strFromClip);
                    ds.ReadXml(txtReader);
                    if (ds.Tables.Count <= 0)
                    {
                        ViewModelBase.HintByMessageBox(string.Format("no table information from xml:\r\n{0}\r\n......", strFromClip.Substring(1, 100)));
                        return;
                    }
                    
                    if (!ImportDataTableIntoTestCaseEditor(ds.Tables[0], ref strError))
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Can't paste from MARS Inner datatable copy with error:\r\n[{0}]", strError));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Clipboard is not formatted by Dataset or CVS for Storyboard, \r\n if text copied from Excel as CVS, standard Mars header must be included. \r\nwith exception:[{0}]", ex.Message));
                    return;
                }
            }
            catch (Exception e)
            {
                ViewModelBase.HintByMessageBox(string.Format("PasteNew Exception:[{0}]", e.Message));
                Logger.Error("PasteNew",e.Message, e);
                return;
            }
        }

        private static string[] cnst_arr_Standard_headerForCopy = new string[] {"Keyword", "Object", "Parameters", "Comment", "Is Skip", "Data" };
        private static string GetStandardCVSHeader()
        {
            return string.Join(",", cnst_arr_Standard_headerForCopy);
        }
        private void copySelectedRowsNew()
        {
            //created by tigre for copy and paste
            DataTable dt = new DataTable();
            dt.Columns.Clear();
            for (int i=0;i<cnst_arr_Standard_headerForCopy.Length;i++)
            {
                dt.Columns.Add(cnst_arr_Standard_headerForCopy[i]);
            }
            if ((SelectedTestCases==null)||(SelectedTestCases.Count<=0))
            {
                HintByMessageBox("Please select rows to be copied");
                return;
            }
            for (int i=0;i<SelectedTestCases.Count;i++)
            {
                var itm = SelectedTestCases[i];
                if (itm == null) continue;
                DataRow oneRow = dt.NewRow();
                
                if ((itm.SelectedKeyword == null))
                {
                    continue;
                }
                for (int j= 0; j < cnst_arr_Standard_headerForCopy.Length ;j++)
                {
                    switch(cnst_arr_Standard_headerForCopy[j].ToUpper())
                    {
                        case "KEYWORD":
                            oneRow[cnst_arr_Standard_headerForCopy[j]] = itm.SelectedKeyword.KeywordName;
                            if (i==0)
                            {
                                if (string.Compare("Pegwindow", itm.SelectedKeyword.KeywordName, true)!=0)
                                {
                                    HintByMessageBox("The first row should be Pegwindow.");
                                    return;
                                }
                            }

                            break;
                        case "OBJECT":
                            oneRow[cnst_arr_Standard_headerForCopy[j]] = itm.SelectedObjectName == null ? "" : itm.SelectedObjectName.ObjName;
                            break;
                        case "PARAMETERS":
                            oneRow[cnst_arr_Standard_headerForCopy[j]] = itm.RowColumn == null ? "" : itm.RowColumn;
                            break;
                        case "COMMENT":
                            oneRow[cnst_arr_Standard_headerForCopy[j]] = itm.Comment == null ? "" : itm.Comment;
                            break;
                        case "IS SKIP":
                            oneRow[cnst_arr_Standard_headerForCopy[j]] = itm.IsSkipForDataset;
                            break;
                        case "DATA":
                            oneRow[cnst_arr_Standard_headerForCopy[j]] = itm.DataSet1;
                            break;
                    }
                    
                }
                dt.Rows.Add(oneRow);
                
            }
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            if (!isCopyAsCVS)
            {
                System.Windows.Clipboard.SetData(System.Windows.DataFormats.Text, ds.GetXml());
            }
            else
            {
                //manage data to cvs formatters
                string strData = StoryboardEditControl.DataTableToCSV(dt, ',');
                string strTab = StoryboardEditControl.DataTableToCSV(dt, '\t');
                var dataObject = new System.Windows.DataObject();

                // Add tab-delimited text to the container object as is.
                dataObject.SetText(strTab);

                // Convert the CSV text to a UTF-8 byte stream before adding it to the container object.
                var bytes = System.Text.Encoding.UTF8.GetBytes(strData);
                var stream = new System.IO.MemoryStream(bytes);
                dataObject.SetData(System.Windows.DataFormats.CommaSeparatedValue, stream);

                // Copy the container object to the clipboard.
                System.Windows.Clipboard.SetDataObject(dataObject, true);

            }
        }

        private void copySelectedRows()
        {
            MarsClipBoard.pegWindow = getCurrentPegWindow();
            if (string.IsNullOrEmpty(MarsClipBoard.pegWindow)) return;

            MarsClipBoard.pegWindowVM = getCurrentPegWindowViewModel();
            MarsClipBoard.appNames = string.Join(",",_appNames.Select(p=>p.APP_SHORT_NAME));
            if (isSelectionInSequence())
            {
                CopyToWindowsClipboard();

                MarsClipBoard.testCasesClipBoard = new List<TestStepViewModel>();
                foreach (TestStepViewModel selectedRow in SelectedTestCases)
                {
                    List<TEST_DATA_SETTING> listTestDataSettings = new List<TEST_DATA_SETTING>();
                    TestStepViewModel testStep = new TestStepViewModel(
                       selectedRow.IsRunChecked,
                       selectedRow.StepNo,
                       selectedRow.RunOrder,
                       selectedRow.RowColumn,
                       selectedRow.RowColumnValue,
                       selectedRow.Comment,
                       selectedRow.SelectedKeyword != null ? selectedRow.SelectedKeyword.Id : 0,
                       selectedRow.SelectedObjectName != null ? selectedRow.SelectedObjectName.Id : 0,
                       listTestDataSettings,
                         _testCaseId,
                         selectedRow.ObjectNameId,
                         selectedRow, ref _testCases
                       );

                    testStep.DataSet1 = selectedRow.DataSet1 != null ? selectedRow.DataSet1.ToString() : null;
                    testStep.DataSet2 = selectedRow.DataSet2 != null ? selectedRow.DataSet2.ToString() : null;
                    testStep.DataSet3 = selectedRow.DataSet3 != null ? selectedRow.DataSet3.ToString() : null;
                    testStep.DataSet4 = selectedRow.DataSet4 != null ? selectedRow.DataSet4.ToString() : null;
                    testStep.DataSet5 = selectedRow.DataSet5 != null ? selectedRow.DataSet5.ToString() : null;
                    testStep.DataSet6 = selectedRow.DataSet6 != null ? selectedRow.DataSet6.ToString() : null;
                    testStep.DataSet7 = selectedRow.DataSet7 != null ? selectedRow.DataSet7.ToString() : null;
                    testStep.DataSet8 = selectedRow.DataSet8 != null ? selectedRow.DataSet8.ToString() : null;
                    testStep.DataSet9 = selectedRow.DataSet9 != null ? selectedRow.DataSet9.ToString() : null;
                    testStep.DataSet10 = selectedRow.DataSet10 != null ? selectedRow.DataSet10.ToString() : null;
                    testStep.DataSet11 = selectedRow.DataSet11 != null ? selectedRow.DataSet11.ToString() : null;
                    testStep.DataSet12 = selectedRow.DataSet12 != null ? selectedRow.DataSet12.ToString() : null;
                    testStep.DataSet13 = selectedRow.DataSet13 != null ? selectedRow.DataSet13.ToString() : null;
                    testStep.DataSet14 = selectedRow.DataSet14 != null ? selectedRow.DataSet14.ToString() : null;
                    testStep.DataSet15 = selectedRow.DataSet15 != null ? selectedRow.DataSet15.ToString() : null;
                    testStep.DataSet16 = selectedRow.DataSet16 != null ? selectedRow.DataSet16.ToString() : null;
                    testStep.DataSet17 = selectedRow.DataSet17 != null ? selectedRow.DataSet17.ToString() : null;
                    testStep.DataSet18 = selectedRow.DataSet18 != null ? selectedRow.DataSet18.ToString() : null;
                    testStep.DataSet19 = selectedRow.DataSet19 != null ? selectedRow.DataSet19.ToString() : null;
                    testStep.DataSet20 = selectedRow.DataSet20 != null ? selectedRow.DataSet20.ToString() : null;

                    MarsClipBoard.testCasesClipBoard.Add(testStep);

                }
            }
        }

        internal void OnCopyToEmptyTestStep()
        {
        
            TestStepViewModel firstSelected = _selectedTestCases.FirstOrDefault();
            if ((this._selectedTestCases==null)||(firstSelected==null))
            {
                ViewModelBase.HintByMessageBox("No Selected Test steps for coping to.");
                return;
            }

            var sortedSelected = _selectedTestCases.OrderBy(p => p.RunOrder)
                .ToList();
            //get the empty rows
            var emptyRows = _testCases.Where(p=>p.SelectedKeyword==null)
                .OrderBy(p=>p.RunOrder)
                .ToList();
            if ((emptyRows==null))
            {
                ViewModelBase.HintByMessageBox("No Empty Rows exist!");
                return;
            }

            if (string.Compare(sortedSelected[0].SelectedKeyword.KeywordName, "Pegwindow",true)!=0)
            {
                ViewModelBase.HintByMessageBox("The first selected Test step should be Pegwindow");
                return;
            }

            ///判断是否run order连续
            ///
            for (int i=0;i<emptyRows.Count-1;i++)
            {
                var c = emptyRows[i];
                var n = emptyRows[i + 1];
                if (n.RunOrder-c.RunOrder!=1)
                {
                    ViewModelBase.HintByMessageBox("The empty rows arenot in sequential!");
                    return;
                }
            }

            // get last peg window
            var lstPeg = emptyRows.LastOrDefault();
            var lastEmpty = lstPeg;
            var FirstRowAfterEmpties = _testCases.FirstOrDefault(p=>p.RunOrder>lstPeg.RunOrder);
            if (FirstRowAfterEmpties == null)
            {
                //empty steps后面没有了
                lstPeg = null;
            }
            else
            {
                if (string.Compare("Pegwindow", FirstRowAfterEmpties.SelectedKeyword.KeywordName, true) == 0)
                {
                    lstPeg = null;
                }
                else
                {
                    if (lstPeg.RunOrder >= FirstRowAfterEmpties.RunOrder)
                    {
                        lstPeg = null;
                    }
                    else
                    {
                        lstPeg = _testCases.Where(p => (string.Compare(p.SelectedKeyword.KeywordName, "Pegwindow", true) == 0) && (p.RunOrder <= lstPeg.RunOrder)).FirstOrDefault();
                    }
                }
            }
            
            int iEndNum = sortedSelected.Count;
            if (emptyRows.Count< sortedSelected.Count)
            {
                System.Windows.Forms.DialogResult way = ViewModelBase.QuestionsByMessageBox(string.Format("Selected [{0}] steps, and [{1}] empty rows exists. Do you want to continue?\r\nYes->Just Copy [{1}] Rows,\r\nNo->Auto Expand rows,\r\nCancel->do Nothing."
                    , _selectedTestCases.Count, emptyRows.Count),"Message",
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel);

                if (way == DialogResult.Cancel) return;

                //insert 最近一个pegwindow
                if (lstPeg != null)
                {
                    var newPegTmp = addNewRow(_testCaseId, (int)FirstRowAfterEmpties.RunOrder);

                    newPegTmp.SelectedKeyword = lstPeg.SelectedKeyword;
                    newPegTmp.Comment = lstPeg.Comment;
                    newPegTmp.ContentValue = lstPeg.ContentValue;
                    newPegTmp.DataSet1 = lstPeg.DataSet1;
                    newPegTmp.DataSetDataType = lstPeg.DataSetDataType;
                    newPegTmp.EnumTypeDataSource = lstPeg.EnumTypeDataSource;
                    newPegTmp.IsRunChecked = lstPeg.IsRunChecked;
                    newPegTmp.IsSkipForDataset = lstPeg.IsSkipForDataset;
                    newPegTmp.Keywords = lstPeg.Keywords;
                    newPegTmp.ObjectNameId = lstPeg.ObjectNameId;
                    newPegTmp.RowColumn = lstPeg.RowColumn;
                    newPegTmp.SelectedKeyword = lstPeg.SelectedKeyword;
                    newPegTmp.SelectedObjectName = lstPeg.SelectedObjectName;
                    newPegTmp._keywords = lstPeg.Keywords;
                    OnPropertyChanged("");
                }

                if (way== DialogResult.No)
                {
                    int iLastEmptyRowNumber = (int)emptyRows.LastOrDefault().RunOrder;
                    for (int i=0;i< sortedSelected.Count-emptyRows.Count;i++)
                    {
                        var newEmptyRow = addNewRow(_testCaseId, iLastEmptyRowNumber);
                        if (newEmptyRow!=null)
                        {
                            iLastEmptyRowNumber = (int)newEmptyRow.RunOrder;
                        }
                    }
                    //AddNewRowCommand.Execute(_testCaseId);
                    //return;
                }               

            }

            //if (firstSelected.RunOrder<=0)
            //{
            //    ViewModelBase.HintByMessageBox("Can't copy from a row before first row.");
            //    return;
            //}

            ////先增加pegwindow， 以确保copy后没有问题            
            for (int i=0;i< sortedSelected.Count;i++)
            {
                var copyFrom = sortedSelected[i];
                emptyRows[i].SelectedKeyword = copyFrom.SelectedKeyword;
                emptyRows[i].Comment = copyFrom.Comment;
                emptyRows[i].ContentValue = copyFrom.ContentValue;
                emptyRows[i].DataSet1 = copyFrom.DataSet1;
                emptyRows[i].DataSetDataType = copyFrom.DataSetDataType;
                emptyRows[i].EnumTypeDataSource = copyFrom.EnumTypeDataSource;
                emptyRows[i].IsRunChecked = copyFrom.IsRunChecked;
                emptyRows[i].IsSkipForDataset = copyFrom.IsSkipForDataset;
                emptyRows[i].Keywords = copyFrom.Keywords;
                emptyRows[i].ObjectNameId = copyFrom.ObjectNameId;
                emptyRows[i].RowColumn = copyFrom.RowColumn;
                emptyRows[i].SelectedKeyword = copyFrom.SelectedKeyword;
                emptyRows[i].SelectedObjectName = copyFrom.SelectedObjectName;
                emptyRows[i]._keywords = copyFrom.Keywords;
            }            

            
            //var copyFrom = _testCases.LastOrDefault(p => p.RunOrder < firstSelected.RunOrder);
            //if (copyFrom==null)
            //{
            //    ViewModelBase.HintByMessageBox("Can't copy from null row.");
            //    return;
            //}

            //firstSelected.SelectedKeyword = copyFrom.SelectedKeyword;
            //firstSelected.Comment = copyFrom.Comment;
            //firstSelected.ContentValue = copyFrom.ContentValue;
            //firstSelected.DataSet1 = copyFrom.DataSet1;
            //firstSelected.DataSetDataType = copyFrom.DataSetDataType;
            //firstSelected.EnumTypeDataSource = copyFrom.EnumTypeDataSource;
            //firstSelected.IsRunChecked = copyFrom.IsRunChecked;
            //firstSelected.IsSkipForDataset = copyFrom.IsSkipForDataset;
            //firstSelected.Keywords = copyFrom.Keywords;
            //firstSelected.ObjectNameId = copyFrom.ObjectNameId;
            //firstSelected.RowColumn = copyFrom.RowColumn;
            //firstSelected.SelectedKeyword = copyFrom.SelectedKeyword;
            //firstSelected.SelectedObjectName = copyFrom.SelectedObjectName;
            //firstSelected._keywords = copyFrom.Keywords;
        }

        internal void OnRunSeletectedTestSteps()
        {
            Logger.logBegin("OnRunSeletectedTestSteps");
            try
            {
                if (this._selectedTestCases==null)
                {
                    ViewModelBase.HintByMessageBox("No Selected Test steps.");
                    return;
                }
                List<TestStepViewModel> lstToRunBySteps = this._selectedTestCases.OrderBy(p => p.RunOrder).ToList();
                TrialActionStepsButtonHandler(lstToRunBySteps);
            }
            finally
            {
                Logger.logEnd("OnRunSeletectedTestSteps");
            }
        }

        private void pasteRowsSpecial(string strDBIdx)
        {
            try
            {
                MarsSpecialClipBoard clipBoard = new MarsSpecialClipBoard();
                LoadClipboardData(strDBIdx,clipBoard);
            }

            catch (Exception ex)
            {
                Logger.Error("pasteRowsSpecial", string.Format("Exception:[{0}]", ex.Message), ex);

            }

        }

        public void pasteRowsFromDataTable(string strDBIdx,DataTable dt)
        {
            try
            {
                MarsSpecialClipBoard clipBoard = new MarsSpecialClipBoard(dt);
                LoadClipboardData(strDBIdx, clipBoard);
            }

            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Exception: \n" + ex.ToString());
            }

        }

        public void LoadClipboardData(string strDBIdx, MarsSpecialClipBoard clipBoard)
        {
            Logger.logBegin("LoadClipboardData");
            DataTable data = clipBoard.SpecialClipBoardData;

            bool clipboardDataValid = true;

            string pegWindow = "";
            if (clipboardDataValid)

            {
                int indexOfSelectedRow = 0;
                if (SelectedTestCases == null)
                {
                    SelectedTestCases = new ObservableCollection<TestStepViewModel>();
                    SelectedTestCases.Add(TestCases.ElementAt(0));
                }
                indexOfSelectedRow = TestCases.IndexOf(SelectedTestCases[0]);

                // case when current TC is empty
                if (indexOfSelectedRow == 0)
                {
                    if (data.Rows[0]["keyword"].ToString().Equals("PegWindow"))
                    {
                        pegWindow = data.Rows[0]["object"].ToString();
                    }
                }
                else
                    pegWindow = getCurrentPegWindow();

                // Extra rows 
                int extraRowRequirement = indexOfSelectedRow +
                                            data.Rows.Count -
                                            TestCases.Count + 1;
                if (extraRowRequirement > 0)
                {
                    //addNewRows(_testCaseName, extraRowRequirement);
                    addNewRows(_testCaseId, extraRowRequirement);
                }

                //   if (pegWindow.Equals(MarsClipBoard.pegWindow) == false &&
                //       MarsClipBoard.testCasesClipBoard[0].SelectedKeyword.KeywordName.Equals("PegWindow") == false)
                var pw = getCurrentPegWindow();
                if (pw.Length == 0)
                    pw = pegWindow;

                if (clipboardDataValid)
                {

                    // Get keyword ids from cache
                    clipBoard.InitKeywodDict();

                    Dictionary<string, long> kwDict = clipBoard.KwDict;

                    // Get object ids from cache
                    clipBoard.InitObjectDict(strDBIdx, _testCaseId, pw);
                    Dictionary<string, long> objDict = clipBoard.ObjDict;

                    var lastTC = _testCases[0];
                    if (lastTC.SelectedKeyword == null)
                        lastTC = null;

                    for (int i = 0; i < data.Rows.Count; i++)
                    {
                        DataRow tcRow = data.Rows[i];
                        List<TEST_DATA_SETTING> listTestDataSettings = new List<TEST_DATA_SETTING>();
                        TestStepViewModel testStep = null;

                        string kw = tcRow["keyword"].ToString();
                        if (kw.ToLower().Equals("pegwindow"))
                        {
                            pw = tcRow["object"].ToString();
                            clipBoard.InitObjectDict(strDBIdx, _testCaseId, pw);
                            objDict = clipBoard.ObjDict;
                        }

                        if (kw.Equals("Comment"))
                        {
                            continue;
                        }

                        long keywordId = kwDict[kw];

                        long objectId;
                        string tdData = tcRow["object"].ToString();
                        if (tdData.Trim().Length == 0)
                            objectId = 0;
                        else
                            objectId = objDict[tdData];
#if v_16AndUp
                        //string strObjNameId = tcRow["object_name_id"].ToString();
                        //long lNameId = -1;
                        //long.TryParse(strObjNameId, out lNameId);

                        // AF ***
                        long appId = (long)TestcaseAppliedAppIds[0];
                        string strObjName = tcRow["object"].ToString();
                        long lNameId = BoHelper.GetObjectNameIdByNameAndAppId(MarsMainWindow.CurrentDatabaseIdx, strObjName, appId);


#endif
                        string rowCol = tcRow["row_column"].ToString();
                        //string value = tcRow["value"].ToString();
                        string value = null;
                        string strComment = tcRow["Comment"].ToString();


                        testStep = new TestStepViewModel(
                                true,
                                indexOfSelectedRow < TestCases.Count ? TestCases[indexOfSelectedRow].StepNo : indexOfSelectedRow + 1,
                                indexOfSelectedRow < TestCases.Count ? TestCases[indexOfSelectedRow].RunOrder : indexOfSelectedRow + 1,
                                rowCol,
                                value,
                                strComment,
                                keywordId,
                                objectId,
                                listTestDataSettings,
                                _testCaseId,
#if v_16AndUp
                                lNameId,
#endif
                                lastTC, ref _testCases
                                );

                        testStep.DataSet1 = value != null ? value : null;
                        foreach (DataColumn col in data.Columns)
                        {
                            if (col.ToString().Contains("Data") &&
                                tcRow[col] != null &&
                                tcRow[col].ToString().Length > 0)
                            {
                                switch (col.ToString())
                                {
                                    case "Data Set 1": testStep.DataSet1 = tcRow[col].ToString(); break;
                                    case "Data Set 2": testStep.DataSet2 = tcRow[col].ToString(); break;
                                    case "Data Set 3": testStep.DataSet3 = tcRow[col].ToString(); break;
                                    case "Data Set 4": testStep.DataSet4 = tcRow[col].ToString(); break;
                                    case "Data Set 5": testStep.DataSet5 = tcRow[col].ToString(); break;
                                    case "Data Set 6": testStep.DataSet6 = tcRow[col].ToString(); break;
                                    case "Data Set 7": testStep.DataSet7 = tcRow[col].ToString(); break;
                                    case "Data Set 8": testStep.DataSet8 = tcRow[col].ToString(); break;
                                    case "Data Set 9": testStep.DataSet9 = tcRow[col].ToString(); break;
                                    case "Data Set 10": testStep.DataSet10 = tcRow[col].ToString(); break;
                                    case "Data Set 11": testStep.DataSet11 = tcRow[col].ToString(); break;
                                    case "Data Set 12": testStep.DataSet12 = tcRow[col].ToString(); break;
                                    case "Data Set 13": testStep.DataSet13 = tcRow[col].ToString(); break;
                                    case "Data Set 14": testStep.DataSet14 = tcRow[col].ToString(); break;
                                    case "Data Set 15": testStep.DataSet15 = tcRow[col].ToString(); break;
                                    case "Data Set 16": testStep.DataSet16 = tcRow[col].ToString(); break;
                                    case "Data Set 17": testStep.DataSet17 = tcRow[col].ToString(); break;
                                    case "Data Set 18": testStep.DataSet18 = tcRow[col].ToString(); break;
                                    case "Data Set 19": testStep.DataSet19 = tcRow[col].ToString(); break;
                                    case "Data Set 20": testStep.DataSet10 = tcRow[col].ToString(); break;

                                    default:
                                        break;
                                }
                            }
                        }

                        lastTC = testStep;
                        DataTable dtt = DataTableUtil.ToDataTable(lastTC.Keywords);

                        if (indexOfSelectedRow < TestCases.Count)
                        {
                            TestCases[indexOfSelectedRow] = testStep;
                        }

                        else
                        {
                            TestCases.Add(testStep);
                        }
                        indexOfSelectedRow++;
                    }
                }
            }
        }
        private TigerClipBoardMgr4Testcase TestCaseclipBoardMgrExcel = new TigerClipBoardMgr4Testcase(MarsMainWindow.CurrentDatabaseIdx);
        
        private bool IsClipboardFormatRight()
        {
            Logger.logBegin("IsClipboardFormatright");
            DataTable objTbl = TestCaseclipBoardMgrExcel.clipboardExcelToDataTable(0,true);
            if (objTbl ==null)
            {
                Logger.Warnning("IsClipboardFormatRight", "Format of clipboard could be wrong.");
                return false;
            }
            ///判断header 是否是标准
            /// 
            if (objTbl.Columns.Count != TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader.Length)
            {
                Logger.Error("IsClipboardFormatRight", string.Format("Columns lenght [{0}] dosn't match data from Excel [{1}].", TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader.Length, objTbl.Columns.Count));
                return false;
            }
            for (int i=0;i<objTbl.Columns.Count;i++)
            {
                if (objTbl.Columns[i]==null)
                {
                    Logger.Error("IsClipboardFormatRight", string.Format("Column is null, it should be :[{0}]", TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader[i]));
                }
                if (string.Compare(objTbl.Columns[i].ToString().Trim(), TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader[i], true) != 0)
                    return false;
            }
            return true;
        }


        private bool PasteDataFromExcel(ref string strError)
        {
            Logger.logBegin("PasteDataFromExcel");
            bool isOk = false;
            Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> dicPegWithItsSubs = new Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>>();
            List<V_TEST_STEPS_FULLVISIONDTO> lstTmpStpsInfo = TestCaseclipBoardMgrExcel.ConvertDataTable2Dto(ref isOk, ref strError,ref dicPegWithItsSubs);
            if (!isOk)
            {
                return false;
            }
            ///依据 List<V_TEST_STEPS_FULLVISIONDTO> lstTmpStpsInfo 添加到 testcase 变量中
            if ((lstTmpStpsInfo==null)||(lstTmpStpsInfo.Count==0))
            {
                strError = "No Test step returns. \r\nMake sure clipboard is with test steps from Excel.";
                return false;
            }
            //testcase
            //找到
            long iStartRowNum = 0;
            TestStepViewModel prevRowViewmodel = null;
            if (TestCases == null) 
                prevRowViewmodel = null;
            else
            {
                if (SelectedTestCases == null)
                    iStartRowNum = TestCases.Count;
                else
                {
                    if (SelectedTestCases.LastOrDefault() == null) iStartRowNum = -1;
                    else
                        iStartRowNum = SelectedTestCases.LastOrDefault().RunOrder;
                }
                prevRowViewmodel = iStartRowNum <= 0 ? null : TestCases[(int)iStartRowNum - 1];
            }
            int iPreviousStp = -1;
            foreach (var itm in lstTmpStpsInfo)
            {
                if (itm == null) continue;
                if (iPreviousStp > (iStartRowNum + itm.RUN_ORDER)) break;
                
                TestStepViewModel tmpStepModel = this.addNewRow(_testCaseId, (int)(iStartRowNum + itm.RUN_ORDER));
                tmpStepModel.BuildKeywordsListBySelectedKeyword(itm.KEY_WORD_NAME, dicPegWithItsSubs,itm.OBJECT_TYPE);
                tmpStepModel.BuildObjectsListBySelectedObjName(itm.OBJECT_HAPPY_NAME, itm.OBJECT_TYPE,dicPegWithItsSubs);
                tmpStepModel.RowColumn = itm.COLUMN_ROW_SETTING;
                tmpStepModel.DataSet1 = itm.VALUE_SETTING;
                tmpStepModel.Comment = itm.COMMENTINFO;
                //new TestCaseEditViewModel(
                //true,
                //iStartRowNum+itm.RUN_ORDER,
                //-1,
                //itm.COLUMN_ROW_SETTING,
                //itm.VALUE_SETTING,
                //itm.COMMENTINFO,
                //itm.KEY_WORD_ID,
                //itm.OBJECT_ID,
                //null,
                //_testCaseId,
                //itm.OBJECT_NAME_ID,
                //prevRowViewmodel,
                //ref _testCases
                //);
                //tmpStepModel.SelectedObjectName
                prevRowViewmodel = tmpStepModel;
                iPreviousStp = (int)(iStartRowNum + itm.RUN_ORDER);

                /*break*/;//for testing
                //_testCases.Add(tmpStepModel);
                OnPropertyChanged("TestCases");
            }
            OnPropertyChanged("TestCases");
            return true;
        }


        

        private void pasteSelectedRows()
        {
#region Copy from Excel
            if (IsClipboardFormatRight())
            {
                string strError = "";
                TestCaseclipBoardMgrExcel.ApplicationIds = this.TestcaseAppliedAppIds.Where(x => x.HasValue).Select(z => z.Value).ToList();
                bool isOk = PasteDataFromExcel(ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Can't copy Storyboard Items from Clipboard, with Error:\r\n{0}", strError),"ERROR");
                }
                return;
            }
#endregion

            string reason;
            int iMode;
            if (MarsClipBoard.Validate(out reason, out iMode) == false)
            {
                if (iMode==0)
                {
                    ///判断window clipboard是否存在数据
                    /// 
                    if (IsClipboardFormatRight())
                    {
                        string strError = "";
                        TestCaseclipBoardMgrExcel.ApplicationIds = this.TestcaseAppliedAppIds.Where(x=>x.HasValue).Select(z=> z.Value).ToList();
                        bool isOk = PasteDataFromExcel(ref strError);
                        if (!isOk)
                        {
                            ViewModelBase.HintByMessageBox("Can't copy Storyboard Items from Clipboard, with Error:\r\n{0}", strError);
                        }
                        return;
                    }
                }
                System.Windows.MessageBox.Show("Error while executing paste command: " + reason);
                return;
            }

            if (!MarsClipBoard.appNames.Equals(_appNames==null?"":string.Join(";",_appNames.Select(p=>p.APP_SHORT_NAME))))
            {
                System.Windows.MessageBox.Show("Error while executing paste command: Copy/Paste operation is not allowed when source and target Test Cases belong to different Applications\n" +
                    "Source TestCase belongs to Application(s) \t" + MarsClipBoard.appNames + "\n" +
                    "Destination TestCase belongs to Application(s) \t" + _appNames);
                return;
            }

            //if (_appNames.Split(',').Length > 1 || MarsClipBoard.appNames.Split(',').Length > 1)
            //{
            //    System.Windows.MessageBox.Show("Error while executing paste command: Copy/Paste operation is not allowed when source or target Test Case belongs to more than one Application\n" +
            //        "Source TestCase belongs to Application(s) \t" + MarsClipBoard.appNames + "\n" +
            //        "Destination TestCase belongs to Application(s) \t" + _appNames);
            //    return;
            //}

            if (isSelectionInSequence())
            {
                int indexOfSelectedRow = TestCases.IndexOf(SelectedTestCases[0]);
                string pegWindow = getCurrentPegWindow();

                // Extra rows 
                int extraRowRequirement = indexOfSelectedRow +
                                            MarsClipBoard.testCasesClipBoard.Count -
                                            TestCases.Count; // +1;
                if (extraRowRequirement > 0)
                {
                    //addNewRows(_testCaseName, extraRowRequirement);
                    addNewRows(_testCaseId, extraRowRequirement);
                }

                if (pegWindow.Equals(MarsClipBoard.pegWindow) == false &&
                    MarsClipBoard.testCasesClipBoard[0].SelectedKeyword.KeywordName.Equals("PegWindow") == false)
                {


                    List<TEST_DATA_SETTING> listTestDataSettings = new List<TEST_DATA_SETTING>();

                    TestStepViewModel testStep = new TestStepViewModel(
                           MarsClipBoard.pegWindowVM.IsRunChecked,
                           indexOfSelectedRow < TestCases.Count ? TestCases[indexOfSelectedRow].StepNo : indexOfSelectedRow + 1,
                           indexOfSelectedRow < TestCases.Count ? TestCases[indexOfSelectedRow].RunOrder : indexOfSelectedRow + 1,
                           MarsClipBoard.pegWindowVM.RowColumn,
                           MarsClipBoard.pegWindowVM.RowColumnValue,
                           MarsClipBoard.pegWindowVM.Comment,
                           MarsClipBoard.pegWindowVM.SelectedKeyword != null ? MarsClipBoard.pegWindowVM.SelectedKeyword.Id : 0,
                           MarsClipBoard.pegWindowVM.SelectedObjectName != null ? MarsClipBoard.pegWindowVM.SelectedObjectName.Id : 0,
                           //MarsClipBoard.pegWindowVM.SelectedObjectName.Id ,
                           listTestDataSettings,
                           _testCaseId,
                           MarsClipBoard.testCasesClipBoard[0].ObjectNameId,
                           MarsClipBoard.pegWindowVM, ref _testCases
                           );

                    // AF BEGIN
                    if (pegWindow.Equals(""))
                    {
                        testStep.addObjectInfo(MarsClipBoard.pegWindowVM, _testCaseId);
                    }

                    // AF END

                    if (indexOfSelectedRow < TestCases.Count)
                    {
                        TestCases[indexOfSelectedRow] = testStep;
                    }

                    else
                    {
                        TestCases.Add(testStep);
                    }
                    indexOfSelectedRow++;
                }

                foreach (TestStepViewModel selectedRow in MarsClipBoard.testCasesClipBoard)
                {

                    List<TEST_DATA_SETTING> listTestDataSettings = new List<TEST_DATA_SETTING>();

                    TestStepViewModel testStep = new TestStepViewModel(
                           selectedRow.IsRunChecked,
                           indexOfSelectedRow < TestCases.Count ? TestCases[indexOfSelectedRow].StepNo : indexOfSelectedRow + 1,
                           indexOfSelectedRow < TestCases.Count ? TestCases[indexOfSelectedRow].RunOrder : indexOfSelectedRow + 1,
                           selectedRow.RowColumn,
                           selectedRow.RowColumnValue,
                           selectedRow.Comment,
                           selectedRow.SelectedKeyword != null ? selectedRow.SelectedKeyword.Id : 0,
                           selectedRow.SelectedObjectName != null ? selectedRow.SelectedObjectName.Id : 0,
                           listTestDataSettings,
                           _testCaseId,
                           selectedRow.ObjectNameId,
                           selectedRow, ref _testCases
                           );

                    testStep.DataSet1 = selectedRow.DataSet1 != null ? selectedRow.DataSet1.ToString() : null;
                    testStep.DataSet2 = selectedRow.DataSet2 != null ? selectedRow.DataSet2.ToString() : null;
                    testStep.DataSet3 = selectedRow.DataSet3 != null ? selectedRow.DataSet3.ToString() : null;
                    testStep.DataSet4 = selectedRow.DataSet4 != null ? selectedRow.DataSet4.ToString() : null;
                    testStep.DataSet5 = selectedRow.DataSet5 != null ? selectedRow.DataSet5.ToString() : null;
                    testStep.DataSet6 = selectedRow.DataSet6 != null ? selectedRow.DataSet6.ToString() : null;
                    testStep.DataSet7 = selectedRow.DataSet7 != null ? selectedRow.DataSet7.ToString() : null;
                    testStep.DataSet8 = selectedRow.DataSet8 != null ? selectedRow.DataSet8.ToString() : null;
                    testStep.DataSet9 = selectedRow.DataSet9 != null ? selectedRow.DataSet9.ToString() : null;
                    testStep.DataSet10 = selectedRow.DataSet10 != null ? selectedRow.DataSet10.ToString() : null;
                    testStep.DataSet11 = selectedRow.DataSet11 != null ? selectedRow.DataSet11.ToString() : null;
                    testStep.DataSet12 = selectedRow.DataSet12 != null ? selectedRow.DataSet12.ToString() : null;
                    testStep.DataSet13 = selectedRow.DataSet13 != null ? selectedRow.DataSet13.ToString() : null;
                    testStep.DataSet14 = selectedRow.DataSet14 != null ? selectedRow.DataSet14.ToString() : null;
                    testStep.DataSet15 = selectedRow.DataSet15 != null ? selectedRow.DataSet15.ToString() : null;
                    testStep.DataSet16 = selectedRow.DataSet16 != null ? selectedRow.DataSet16.ToString() : null;
                    testStep.DataSet17 = selectedRow.DataSet17 != null ? selectedRow.DataSet17.ToString() : null;
                    testStep.DataSet18 = selectedRow.DataSet18 != null ? selectedRow.DataSet18.ToString() : null;
                    testStep.DataSet19 = selectedRow.DataSet19 != null ? selectedRow.DataSet19.ToString() : null;
                    testStep.DataSet20 = selectedRow.DataSet20 != null ? selectedRow.DataSet20.ToString() : null;

                    if (indexOfSelectedRow < TestCases.Count)
                    {
                        TestCases[indexOfSelectedRow] = testStep;
                    }

                    else
                    {
                        TestCases.Add(testStep);
                    }
                    indexOfSelectedRow++;
                }

                ///clear  MarsClipBoard.testCasesClipBoard
                /// 
                if (MarsClipBoard.testCasesClipBoard!=null)
                    MarsClipBoard.testCasesClipBoard.Clear();
            }
        }

        private bool isSelectionInSequence()
        {
            if (SelectedTestCases.Count == 0)
            {
                System.Windows.MessageBox.Show("Select a row to perform this action", "Test Steps", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            int indexOfSelectedTestCase = TestCases.IndexOf(SelectedTestCases[0]);
            for (int i = 0; i < SelectedTestCases.Count; i++)
            {
                if (TestCases.ElementAt(indexOfSelectedTestCase) != SelectedTestCases[i])
                {
                    System.Windows.MessageBox.Show("The Selection is not sequence", "Test Steps", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                indexOfSelectedTestCase++;

            }
            return true;
        }

        private void CopyToWindowsClipboard()
        {
            try
            {
                StringBuilder SBOutput = new StringBuilder();
#if !v_16AndUp
                    SBOutput.Append("keyword\t object\t row_column\t value\t Comment\t Data Set 1\t Data Set 2\t Data Set 3\t Data Set 4\t Data Set 5\t Data Set 6\t Data Set 7\t Data Set 8\t Data Set 9\t Data Set 10\t Data Set 11\t Data Set 12\t Data Set 13\t Data Set 14\t Data Set 15\t Data Set 16\t Data Set 17\t Data Set 18\t Data Set 19\t Data Set 20");
#else
                //SBOutput.Append("keyword\t object\t row_column\t object_name_id\t value\t Comment\t Data Set 1\t Data Set 2\t Data Set 3\t Data Set 4\t Data Set 5\t Data Set 6\t Data Set 7\t Data Set 8\t Data Set 9\t Data Set 10\t Data Set 11\t Data Set 12\t Data Set 13\t Data Set 14\t Data Set 15\t Data Set 16\t Data Set 17\t Data Set 18\t Data Set 19\t Data Set 20");
                //SBOutput.Append("keyword\t object\t row_column\t value\t Comment\t Data Set 1\t Data Set 2\t Data Set 3\t Data Set 4\t Data Set 5\t Data Set 6\t Data Set 7\t Data Set 8\t Data Set 9\t Data Set 10\t Data Set 11\t Data Set 12\t Data Set 13\t Data Set 14\t Data Set 15\t Data Set 16\t Data Set 17\t Data Set 18\t Data Set 19\t Data Set 20");
                //{ "Keyword", "Object", "Parameters", "Comment", "Data" };
                SBOutput.Append(string.Join("\t ", TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader));
#endif
                SBOutput.Append(Environment.NewLine);
                foreach (TestStepViewModel testCase in SelectedTestCases)
                {

                    SBOutput.Append(
                        (testCase.SelectedKeyword != null ? testCase.SelectedKeyword.KeywordName : "") + "\t" +
                        (testCase.SelectedObjectName != null ? testCase.SelectedObjectName.ObjName : "") + "\t" +
                        testCase.RowColumn + "\t" +
#if v_16AndUp
                                // AF                         testCase.ObjectNameId??"-1" + "\t" +
#endif
                                //"\t" +
                        testCase.Comment + "\t" +
                        (testCase.DataSet1 != null ? testCase.DataSet1.ToString() : "") + "\t" 
                        /***+
                        (testCase.DataSet2 != null ? testCase.DataSet2.ToString() : "") + "\t" +
                        (testCase.DataSet3 != null ? testCase.DataSet3.ToString() : "") + "\t" +
                        (testCase.DataSet4 != null ? testCase.DataSet4.ToString() : "") + "\t" +
                        (testCase.DataSet5 != null ? testCase.DataSet5.ToString() : "") + "\t" +
                        (testCase.DataSet6 != null ? testCase.DataSet6.ToString() : "") + "\t" +
                        (testCase.DataSet7 != null ? testCase.DataSet7.ToString() : "") + "\t" +
                        (testCase.DataSet8 != null ? testCase.DataSet8.ToString() : "") + "\t" +
                        (testCase.DataSet9 != null ? testCase.DataSet9.ToString() : "") + "\t" +
                        (testCase.DataSet10 != null ? testCase.DataSet10.ToString() : "") + "\t" +
                        (testCase.DataSet11 != null ? testCase.DataSet11.ToString() : "") + "\t" +
                        (testCase.DataSet12 != null ? testCase.DataSet12.ToString() : "") + "\t" +
                        (testCase.DataSet13 != null ? testCase.DataSet13.ToString() : "") + "\t" +
                        (testCase.DataSet14 != null ? testCase.DataSet14.ToString() : "") + "\t" +
                        (testCase.DataSet15 != null ? testCase.DataSet15.ToString() : "") + "\t" +
                        (testCase.DataSet16 != null ? testCase.DataSet16.ToString() : "") + "\t" +
                        (testCase.DataSet17 != null ? testCase.DataSet17.ToString() : "") + "\t" +
                        (testCase.DataSet18 != null ? testCase.DataSet18.ToString() : "") + "\t" +
                        (testCase.DataSet19 != null ? testCase.DataSet19.ToString() : "") + "\t" +
                        (testCase.DataSet20 != null ? testCase.DataSet20.ToString() : "")
                        ****/
                       );
                    SBOutput.Append(Environment.NewLine);
                }
                System.Windows.Clipboard.SetDataObject(SBOutput.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error("ExportToExcel", string.Format("Exception:[{0}]", ex.Message), ex);
            }

        }

        private void ExportToExcel(object dataGrid)
        {
            try
            {
                if (dataGrid != null)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "CSV|*.CSV|Excel|*.XLS";
                    saveFileDialog.Title = "Save an excel File";
                    saveFileDialog.ShowDialog();
                    string strFilePath = "";
                    if (saveFileDialog.FileName != "")
                    {
                        strFilePath = saveFileDialog.FileName;
                        StreamWriter sw = new StreamWriter(strFilePath, false);
                        sw.Write("keyword, object, row_column, value, Comment, Data Set 1, Data Set 2, Data Set 3, Data Set 4, Data Set 5, Data Set 6, Data Set 7, Data Set 8, Data Set 9, Data Set 10, Data Set 11, Data Set 12, Data Set 13, Data Set 14, Data Set 15, Data Set 16, Data Set 17, Data Set 18, Data Set 19, Data Set 20");
                        sw.Write(sw.NewLine);
                        foreach (TestStepViewModel testCase in (System.Collections.ObjectModel.ObservableCollection<Mars.ViewModel.TestStepViewModel>)(dataGrid))
                        {

                            sw.Write(
                                (testCase.SelectedKeyword != null ? testCase.SelectedKeyword.KeywordName : "") + "," +
                                (testCase.SelectedObjectName != null ? testCase.SelectedObjectName.ObjName : "") + "," +
                                testCase.RowColumn + "," +
                                "," +
                                testCase.Comment + "," +
                                (testCase.DataSet1 != null ? testCase.DataSet1.ToString() : "") + "," +
                                (testCase.DataSet2 != null ? testCase.DataSet2.ToString() : "") + "," +
                                (testCase.DataSet3 != null ? testCase.DataSet3.ToString() : "") + "," +
                                (testCase.DataSet4 != null ? testCase.DataSet4.ToString() : "") + "," +
                                (testCase.DataSet5 != null ? testCase.DataSet5.ToString() : "") + "," +
                                (testCase.DataSet6 != null ? testCase.DataSet6.ToString() : "") + "," +
                                (testCase.DataSet7 != null ? testCase.DataSet7.ToString() : "") + "," +
                                (testCase.DataSet8 != null ? testCase.DataSet8.ToString() : "") + "," +
                                (testCase.DataSet9 != null ? testCase.DataSet9.ToString() : "") + "," +
                                (testCase.DataSet10 != null ? testCase.DataSet10.ToString() : "") + "," +
                                (testCase.DataSet11 != null ? testCase.DataSet11.ToString() : "") + "," +
                                (testCase.DataSet12 != null ? testCase.DataSet12.ToString() : "") + "," +
                                (testCase.DataSet13 != null ? testCase.DataSet13.ToString() : "") + "," +
                                (testCase.DataSet14 != null ? testCase.DataSet14.ToString() : "") + "," +
                                (testCase.DataSet15 != null ? testCase.DataSet15.ToString() : "") + "," +
                                (testCase.DataSet16 != null ? testCase.DataSet16.ToString() : "") + "," +
                                (testCase.DataSet17 != null ? testCase.DataSet17.ToString() : "") + "," +
                                (testCase.DataSet18 != null ? testCase.DataSet18.ToString() : "") + "," +
                                (testCase.DataSet19 != null ? testCase.DataSet19.ToString() : "") + "," +
                                (testCase.DataSet20 != null ? testCase.DataSet20.ToString() : "")
                               );
                            sw.Write(sw.NewLine);
                        }
                        sw.Close();
                        System.Windows.MessageBox.Show("Export completed. Please open your file from " + strFilePath.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExportToExcel ", ex.Message, ex);
            }

        }

        private void ExportToExcelOld(object dataGrid)
        {
            try
            {
                if (dataGrid != null)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "CSV|*.CSV|Excel|*.XLS";
                    saveFileDialog.Title = "Save an excel File";
                    saveFileDialog.ShowDialog();
                    string strFilePath = "";
                    if (saveFileDialog.FileName != "")
                    {
                        strFilePath = saveFileDialog.FileName;
                        StreamWriter sw = new StreamWriter(strFilePath, false);
                        sw.Write("No, isRunChecked, SelectedKeyword, SelectedObjectName, RowColumn, Comment, Data Set1, Dataset2, Dataset3, Dataset4, Dataset5, Dataset6, Dataset7, Dataset8, Dataset9, Dataset10, Dataset11, Dataset12, Dataset13, Dataset14, Dataset15, Dataset16, Dataset17, Dataset18, Dataset19, Dataset20");
                        sw.Write(sw.NewLine);
                        foreach (TestStepViewModel testCase in (System.Collections.ObjectModel.ObservableCollection<Mars.ViewModel.TestStepViewModel>)(dataGrid))
                        {
                            sw.Write(testCase.RunOrder + "," +
                                testCase.IsRunChecked + "," +
                                (testCase.SelectedKeyword != null ? testCase.SelectedKeyword.KeywordName : "") + "," +
                                (testCase.SelectedObjectName != null ? testCase.SelectedObjectName.ObjName : "") + "," +
                                testCase.RowColumnValue + "," +
                                testCase.Comment + "," +
                                (testCase.DataSet1 != null ? testCase.DataSet1.ToString() : "") + "," +
                                (testCase.DataSet2 != null ? testCase.DataSet2.ToString() : "") + "," +
                                (testCase.DataSet3 != null ? testCase.DataSet3.ToString() : "") + "," +
                                (testCase.DataSet4 != null ? testCase.DataSet4.ToString() : "") + "," +
                                (testCase.DataSet5 != null ? testCase.DataSet5.ToString() : "") + "," +
                                (testCase.DataSet6 != null ? testCase.DataSet6.ToString() : "") + "," +
                                (testCase.DataSet7 != null ? testCase.DataSet7.ToString() : "") + "," +
                                (testCase.DataSet8 != null ? testCase.DataSet8.ToString() : "") + "," +
                                (testCase.DataSet9 != null ? testCase.DataSet9.ToString() : "") + "," +
                                (testCase.DataSet10 != null ? testCase.DataSet10.ToString() : "") + "," +
                                (testCase.DataSet11 != null ? testCase.DataSet11.ToString() : "") + "," +
                                (testCase.DataSet12 != null ? testCase.DataSet12.ToString() : "") + "," +
                                (testCase.DataSet13 != null ? testCase.DataSet13.ToString() : "") + "," +
                                (testCase.DataSet14 != null ? testCase.DataSet14.ToString() : "") + "," +
                                (testCase.DataSet15 != null ? testCase.DataSet15.ToString() : "") + "," +
                                (testCase.DataSet16 != null ? testCase.DataSet16.ToString() : "") + "," +
                                (testCase.DataSet17 != null ? testCase.DataSet17.ToString() : "") + "," +
                                (testCase.DataSet18 != null ? testCase.DataSet18.ToString() : "") + "," +
                                (testCase.DataSet19 != null ? testCase.DataSet19.ToString() : "") + "," +
                                (testCase.DataSet20 != null ? testCase.DataSet20.ToString() : "")
                               );
                            sw.Write(sw.NewLine);
                        }
                        sw.Close();
                        System.Windows.MessageBox.Show("Export completed. Please open your file from " + strFilePath.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExportToExcel", ex.Message, ex);
            }

        }


#region command
        public ICommand AddNewRowCommand
        {
            get
            {
                return _addNewRowCommand;
            }
            set
            { }
        }

        public ICommand DeleteSelectedRowsCommand
        {
            get
            {
                return _deleteSelectedRowsCommand;
            }
            set
            { }

        }

        public ICommand MoveUpSelectedRowsCommand
        {
            get
            {
                return _moveUpSelectedRowsCommand;
            }
            set
            { }
        }

        public ICommand MoveDownSelectedRowsCommand
        {
            get
            {
                return _moveDownSelectedRowsCommand;
            }
            set
            { }
        }

        public ICommand CopySelectedRowsCommand
        {
            get
            {
                return _copySelectedRowsCommand;
            }
            set
            { }
        }

        public ICommand PasteSelectedRowsCommand
        {
            get
            {
                return _pasteSelectedRowsCommand;
            }
            set
            { }
        }

        private ICommand _pasteRowsSpecialCommand;
        public ICommand PasteRowsSpecialCommand
        {
            get { return _pasteRowsSpecialCommand; }

        }

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }

            set
            {
                _saveCommand = value;
            }
        }

        public ICommand SaveAsCommand
        {
            get
            {
                return _saveAsCommand;
            }

            set
            {
                _saveAsCommand = value;
            }
        }

        public ICommand LoadDataFromDatabaseCommand
        {
            get
            {
                return _loadDataFromDatabaseCommand;
            }
            set
            {
                _loadDataFromDatabaseCommand = value;
            }
        }

        private bool CleanORFill = true;
        public ICommand DeleteForLoadDatabaseCommand
        {
            get
            {
                return new DelegateCommand(new Action(()=>{
                    CleanORFill = false;
                    LoadDataFromDatabaseCommandTmp();
                }));
            }
        }

        public ICommand TrialActionStepsButtonClick
        {
            get
            {
                return trialActionStepsButtonClick;
            }
            set
            {
                trialActionStepsButtonClick = value;
            }
        }

        public ICommand RefreshCommand
        {
            get
            {
                return _refreshCommand;
            }

            set
            {
                _refreshCommand = value;
            }
        }

        public DelegateCommand<object> ExportToExcelCommand
        {
            get
            {
                return _exportToExcelCommand;
            }

            set
            {
                _exportToExcelCommand = value;
            }
        }

        private int _currentHitIndex = -1;
        List<TestStepViewModel> searchHittedSteps = null;
        public DelegateCommand LocateSpecialTestStepClickCmd
        {
            get
            {
                return new DelegateCommand(() => {
                    _currentHitIndex = -1;
                    searchHittedSteps=TestCases.Where(p => p.CurrentRowColor == TestStepViewModel.RowColorMgr.SearchHitBrush)
                    .OrderBy(o => o.RunOrder)
                    .ToList();
                    if ((searchHittedSteps==null) ||(searchHittedSteps.Count == 0)) return;
                    _currentHitIndex = 0;
                    if (SelectedTestCases == null)
                        SelectedTestCases = new ObservableCollection<TestStepViewModel>();
                    if ((SelectedTestCases != null) && (SelectedTestCases.Count > 0))
                        SelectedTestCases.Clear();
                    SelectedTestCases.Add(searchHittedSteps[0]);
                    //OnPropertyChanged("SelectedTestCases");
                });
            }
        }

        public DelegateCommand PreviousSpecialTestStepClickCmd
        {
            get {
                return new DelegateCommand(() => {
                    try
                    {
                        if (SelectedTestCases == null)
                            SelectedTestCases = new ObservableCollection<TestStepViewModel>();
                        if (searchHittedSteps == null)
                            searchHittedSteps = TestCases.Where(p => p.CurrentRowColor == TestStepViewModel.RowColorMgr.SearchHitBrush)
                                .OrderBy(o => o.RunOrder)
                                .ToList();
                        if (searchHittedSteps.Count <= 0) return;
                        _currentHitIndex--;
                        if (_currentHitIndex < 0)
                        {
                            _currentHitIndex = 0;
                        }
                        SelectedTestCases.Clear();
                        try
                        {
                            SelectedTestCases.Add(searchHittedSteps[_currentHitIndex]);
                        }
                        catch (Exception)
                        {

                        }
                    }catch(Exception e)
                    {

                    }

                });
            }
        }

        public DelegateCommand NextSpecialTestStepClickCmd
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    try
                    {
                        if (SelectedTestCases == null)
                            SelectedTestCases = new ObservableCollection<TestStepViewModel>();
                        if (searchHittedSteps == null)
                            searchHittedSteps = TestCases.Where(p => p.CurrentRowColor == TestStepViewModel.RowColorMgr.SearchHitBrush)
                                .OrderBy(o => o.RunOrder)
                                .ToList();
                        if (searchHittedSteps.Count <= 0) return;

                        _currentHitIndex++;
                        if ((AssignedGuiObj != null) && ((AssignedGuiObj as TestCaseEdit) != null))
                        {
                            (AssignedGuiObj as TestCaseEdit).ActiveRowByIndex(searchHittedSteps[_currentHitIndex]);
                        }

                        //if (_currentHitIndex >= searchHittedSteps.Count)
                        //{
                        //    _currentHitIndex = searchHittedSteps.Count-1;
                        //}
                        //SelectedTestCases.Clear();
                        //try
                        //{
                        //    SelectedTestCases.Add(searchHittedSteps[_currentHitIndex]);
                        //}
                        //catch (Exception)
                        //{

                        //}
                    }catch(Exception e)
                    {

                    }

                });
            }
        }
#endregion //command

        public ObservableCollection<TestStepViewModel> _selectedTestCases;
        public ObservableCollection<TestStepViewModel> SelectedTestCases
        {
            get
            {
                return _selectedTestCases;
            }
            set
            {
                _selectedTestCases = value;
                OnPropertyChanged("SelectedTestCases");
            }
        }
        public ObservableCollection<TestStepViewModel> _testCases;
        public ObservableCollection<TestStepViewModel> TestCases
        {
            get
            {
                return _testCases;
            }
            set
            {
                _testCases = value;
                OnPropertyChanged("TestCases");
            }
        }

        private string _SearchText;
        public string SearchText
        {
            get
            {
                return _SearchText;
            }
            set
            {
                _SearchText = value;
                OnPropertyChanged("SearchText");
                if (string.IsNullOrEmpty(_SearchText) || _SearchText.Length < 4)
                {
                    foreach (var itm in _testCases)
                    {
                        itm.SearchText("");
                    }
                    return;
                }
                foreach (var itm in _testCases)
                {
                    itm.SearchText(_SearchText);
                }
            }
        }

        private long _projectId;
        public long CurrentOwner_ProjectId
        {
            get { return _projectId; }            
            set { _projectId = value; }
        }
        
        private long _testSuiteId;
        public long CurrentOwner_TestSuiteId
        {
            get { return _testSuiteId; }
            set { _testSuiteId = value; }
        }

        private long _dataSheetId;
        public long DataSheetId
        {
            get { return _dataSheetId; }
            set {
                _dataSheetId = value;
            }
        }

        public ObservableCollection<Keyword> _staticKeywords;
        private bool _isSharedData;
        private ObservableCollection<T_REGISTERED_APPSDTO> _appNames=null;
        public ObservableCollection<T_REGISTERED_APPSDTO> Testcase_applications
        {
            get { return _appNames; }
            set
            {
                _appNames = value;
                if ((_appNames != null)&&(_appNames.Count>=1))
                {
                    CurrentTCApplication = _appNames[0];
                }
                OnPropertyChanged("Testcase_applications");
            }
        }
        private T_REGISTERED_APPSDTO _currentTCApplication;
        public T_REGISTERED_APPSDTO CurrentTCApplication
        {
            get { return _currentTCApplication; }
            set
            {
                _currentTCApplication = value;
                OnPropertyChanged("CurrentTCApplication");
            }
        }

        private bool _isModifiedTestCase;

        public ObservableCollection<Keyword> StaticKeywords
        {
            get
            {
                return _staticKeywords;
            }
            set
            {
                _staticKeywords = value;
                OnPropertyChanged("StaticKeywords");
            }
        }


        public class ObjectOccuranceRepository
        {
            private Dictionary<string, int>[] objectOccuranceDictArray = new Dictionary<string, int>[21];

            public void UpdateObjectOrder(string objectName, int loopId)
            {
                // Console.WriteLine("UpdateObjectOrder objectName = " + objectName + " loopId = " + loopId);
                Dictionary<string, int> objectOccuranceDict;

                if (objectOccuranceDictArray[loopId] == null)
                    objectOccuranceDictArray[loopId] = new Dictionary<string, int>();

                objectOccuranceDict = objectOccuranceDictArray[loopId];

                if (objectOccuranceDict.ContainsKey(objectName))
                    objectOccuranceDict[objectName]++;
                else
                    objectOccuranceDict.Add(objectName, 1);

            }

            public int GetObjectOrder(string objectName, int loopId)
            {
                //Console.WriteLine("GetObjectOrder objectName = " + objectName + " loopId = " + loopId);
                int objectOrder = 0;
                Dictionary<string, int> objectOccuranceDict;
                if (objectOccuranceDictArray[loopId] != null)
                {
                    objectOccuranceDict = objectOccuranceDictArray[loopId];
                    if (objectOccuranceDict.ContainsKey(objectName))
                        objectOrder = objectOccuranceDict[objectName];
                }
                return objectOrder;
            }

        }

        private void LoadTestSteps(long testCaseId, long dataSheetId)
        {
            List<TestStepViewModel> lstTestCaseTmp = new List<TestStepViewModel>();
            // Af change to fix refresh
            //_testCases = new ObservableCollection<TestCaseEditViewModel>();
            //TestCases = new ObservableCollection<TestStepViewModel>();
            _testCases = new ObservableCollection<TestStepViewModel>();

            //chnaged by tiger
            bool isOk = false;
            string strError = "";
            List<KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>> lstStepAndData = BoHelper.LoadTestStepAndItsDataByTCIdAndDtaId( 
                MarsMainWindow.CurrentDatabaseIdx,
                testCaseId, 
                dataSheetId, ref isOk, ref strError);
            if (!isOk)
            {
                ViewModelBase.HintByMessageBox(string.Format("Can't get test case from DB with error:\r\n{0}", strError));
                return;
            }
            ObservableCollection<Keyword> staticKeyWordsFromOuter = TestStepViewModel.PopulateKeywordsById(testCaseId);
            //install objectNameInfo
            TestStepViewModel tmpStpViewModal = null,preStpViewModal=null;
            int iPegIdx = 0;
            for (int i=0;i<lstStepAndData.Count;i++)
            {
                var itm = lstStepAndData[i];
                if (itm.Equals(default(KeyValuePair<T_TEST_STEPSDTO, TEST_DATA_SETTINGDTO>))) continue;
                if (itm.Key == null) continue;
                lstTestCaseTmp.Add(tmpStpViewModal = new TestStepViewModel(
                    itm.Key.IS_RUNNABLE == null ? false : itm.Key.IS_RUNNABLE == -1 ? true : false,
                    itm.Key.STEPS_ID,
                    (long)(itm.Key.RUN_ORDER ?? i + 1),
                    itm.Key.COLUMN_ROW_SETTING,
                    itm.Key.VALUE_SETTING,
                    itm.Key.COMMENT,
                    itm.Key.KEY_WORD_ID ?? -1,
                    itm.Key.OBJECT_ID ?? -1,
                    new List<TEST_DATA_SETTING>() { itm.Value==null?null:itm.Value.ToEntity() }, //listTestDataSettings,  //以前支持多个数据集合，因此传入一个list，现在只有一个
                    testCaseId,
                    itm.Key.OBJECT_NAME_ID,
                    preStpViewModal,
                     ref _testCases,
                     staticKeyWordsFromOuter
                     )
                    );
                var tmpPre = lstTestCaseTmp
                    .LastOrDefault(p => ((p.SelectedKeyword == null ? false : string.Compare(p.SelectedKeyword.KeywordName, "Pegwindow", true) == 0)&&(p.RunOrder<=itm.Key.RUN_ORDER)));
                if (string.Compare("Pegwindow", tmpStpViewModal.SelectedKeyword==null?"": tmpStpViewModal.SelectedKeyword.KeywordName,true)==0)
                {
                    iPegIdx++;
                }
                tmpStpViewModal.SetPegwindowIdx(iPegIdx);

                if (tmpPre == null)
                    preStpViewModal = tmpStpViewModal;
                else
                    preStpViewModal = tmpPre;
            }
            TestCases = new ObservableCollection<TestStepViewModel>(lstTestCaseTmp);
            OnPropertyChanged("TestCases");
            return;
            /*
            //gabage codes
            B_TEST_STEPS btestStep = new B_TEST_STEPS();

            List<B_TEST_STEPS> bTestStepsList = BoHelper.GetTestSteps(testCaseId);
            

            ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();
            //List<TestCaseEditViewModel> lstTmpTestStep = new List<TestCaseEditViewModel>(); static keywords information

            

            foreach (B_TEST_STEPS testStep in bTestStepsList)
            {
                //Console.WriteLine("Loading testCaseRow with RunOrder = " + testStep.RUN_ORDER + " keyword id = " + testStep.KEY_WORD_ID);
                try
                {
                    long keywordId = (long)testStep.KEY_WORD_ID;
                    long objectId = 0;
                    if (testStep.OBJECT_ID != null) //Girish 9/7/2015
                        objectId = (long)testStep.OBJECT_ID;
#if v_16AndUp
                    long lObjNameId = testStep.OBJECT_NAME_ID ?? -1;
#endif
                    long runOrder = (long)testStep.RUN_ORDER;
                    bool isRunnable = testStep.IS_RUNNABLE == 1 ? true : false;

                    List<TEST_DATA_SETTING> listTestDataSettings = new List<TEST_DATA_SETTING>();
                    List<T_SHARED_OBJECT_POOL> sharedObjectPoolList = new List<T_SHARED_OBJECT_POOL>();

                    T_SHARED_OBJECT_POOL sharedObjectPool;

                    int objectOrder;

                    if (_isSharedData)
                    {
                        string objectName;

                        for (int loopId = 0; loopId < MarsConstants.NumberOfDataSetColumns; loopId++)
                        {
#if !v_useNameId
                            if (objectId != 0)
                            {
                                objectName = BoHelper.GetObjectNameById(objectId);
#else
                            if (lObjNameId > 0)
                            {
                                objectName = BoHelper.GetObjectNameByNameId(lObjNameId);
#endif


                                objectOrder = objectOccuranceRepository.GetObjectOrder(objectName, loopId);
                                sharedObjectPool = BoHelper.LoadSharedObjectPool(testStep.STEPS_ID, dataSheetId, "obj_" + objectName, loopId, objectOrder);

                            }
                            else
                            {

                                objectName = BoHelper.GetKeywordNameById(keywordId);
                                objectOrder = objectOccuranceRepository.GetObjectOrder(objectName, loopId);
                                sharedObjectPool = BoHelper.LoadSharedObjectPool(testStep.STEPS_ID, dataSheetId, "kw_" + objectName, loopId, objectOrder);

                            }

                            if (sharedObjectPool != null)
                            {
                                sharedObjectPoolList.Add(sharedObjectPool);
                                //Console.WriteLine("objectName = " + objectName);
                                objectOccuranceRepository.UpdateObjectOrder(objectName, loopId);
                            }

                            listTestDataSettings = ConvertToDataSettingsList(sharedObjectPoolList);
                        }
                    }
                    else
                    {


                        // AF fixing problem of correctly selecting data sheet data
                        // listTestDataSettings = testStep.ListDataSetting;
                        if (testStep.ListDataSetting != null &&
                            testStep.ListDataSetting[0] != null &&
                            dataSheetId != -1)
                            listTestDataSettings = (from d in testStep.ListDataSetting
                                                    where d.DATA_SUMMARY_ID == dataSheetId
                                                    select d).ToList();
                        else
                            listTestDataSettings = testStep.ListDataSetting;
                        //listTestDataSettings = BoHelper.LoadTestDataSettings(testStep.STEPS_ID, dataSheetId);
                    }

                    var prevRowViewmodel = TestCases.Count == 0 ? null : TestCases.Last();
                    TestStepViewModel testCaseVM = new TestStepViewModel(isRunnable, testStep.STEPS_ID, runOrder, testStep.COLUMN_ROW_SETTING,
                        testStep.VALUE_SETTING, testStep.COMMENT, keywordId, objectId,
                        listTestDataSettings, testCaseId,
                        testStep.OBJECT_NAME_ID,
                        prevRowViewmodel, ref _testCases, staticKeyWordsFromOuter);
                    //lstTmpTestStep.Add(testCaseVM);
                    TestCases.Add(testCaseVM);

                }
                catch (Exception ex)
                {
                    Logger.Error("LoadTestSteps", ex.Message, ex);
                }
            }
            //TestCases = new ObservableCollection<TestCaseEditViewModel>(lstTestCaseTmp);
            //_testCases = new ObservableCollection<TestCaseEditViewModel>(lstTmpTestStep);
            if (_testCases.Count == 0)
            {
                //addNewRow(testCaseName);
                addNewRow(testCaseId);
            }
            OnPropertyChanged("TestCases");
            */

        }

        private List<TEST_DATA_SETTING> ConvertToDataSettingsList(List<T_SHARED_OBJECT_POOL> sharedObjectPoolList)
        {
            List<TEST_DATA_SETTING> newList = new List<TEST_DATA_SETTING>();
            foreach (T_SHARED_OBJECT_POOL pool in sharedObjectPoolList)
            {
                TEST_DATA_SETTING dataSetting = new TEST_DATA_SETTING();
                dataSetting.LOOP_ID = pool.LOOP_ID;
                dataSetting.DATA_VALUE = pool.DATA_VALUE;

                newList.Add(dataSetting);
            }

            return newList;
        }

        private void LoadTestSteps(string strDBIdx, long testCaseId)
        {
            TestCases = new ObservableCollection<TestStepViewModel>();
            B_TEST_STEPS btestStep = new B_TEST_STEPS();
            List<B_TEST_STEPS> bTestStepsList = BoHelper.GetTestSteps(
                strDBIdx,
                testCaseId);

            foreach (B_TEST_STEPS testStep in bTestStepsList)
            {
                try
                {
                    long keywordId = (long)testStep.KEY_WORD_ID;
                    long objectId = 0;
                    if (testStep.OBJECT_ID != null) //Girish 9/7/2015
                        objectId = (long)testStep.OBJECT_ID;
                    long runOrder = (long)testStep.RUN_ORDER;
                    bool isRunnable = testStep.IS_RUNNABLE == 1 ? true : false;
#if v_16AndUp
                    long? lNameId = testStep.OBJECT_NAME_ID;
#endif
                    List<TEST_DATA_SETTING> listTestDataSettings = BoHelper.LoadTestDataSettings(strDBIdx,
                        testStep.STEPS_ID);
                    var prevRowViewmodel = TestCases.Count == 0 ? null : TestCases.Last();
                    TestStepViewModel testCaseVM = new TestStepViewModel(isRunnable, testStep.STEPS_ID, runOrder,
                        testStep.COLUMN_ROW_SETTING, testStep.VALUE_SETTING,
                        testStep.COMMENT, keywordId, objectId, listTestDataSettings,
                        testCaseId,
#if v_16AndUp
                        lNameId ?? -1,
#endif
                        prevRowViewmodel, ref _testCases);
                    TestCases.Add(testCaseVM);
                }
                catch (Exception ex)
                {
                    Logger.Error("LoadTestSteps", ex.Message, ex);
                }
            }

            if (TestCases.Count == 0)
            {
                //addNewRow(testCaseName);
                addNewRow(testCaseId);
            }
        }

        public long GetTestCaseId(string testCaseName)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            B_TEST_CASE objBTestCase = new B_TEST_CASE();
            B_TEST_CASE testCase = objBTestCase.GetTestCase(MarsMainWindow.CurrentDatabaseIdx, testCaseName);

            long testCaseId = 0;

            if (testCase != null)
            {
                testCaseId = testCase.TEST_CASE_ID;
            }
            return testCaseId;
        }


        internal void DeleteRowsAfterPegWindow()
        {
            if (SelectedTestCases.Count == 0)
                return;

            long selectedRow = SelectedTestCases[0].RunOrder;
            if (TestCases.Count > 1 &&
                TestCases.Count != selectedRow &&
                System.Windows.MessageBox.Show("Inserting a new PegWindow will cause deletion of steps between this row and the next PegWindow. Proceed?", "Warning", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
            {
                var row = SelectedTestCases[0];

                // Delete rows following this row
                SelectedTestCases.Clear();

                long lastRowNum = getNextPegWinRowNum(selectedRow);
                foreach (var tc in TestCases)
                {
                    if (tc.RunOrder > selectedRow && tc.RunOrder <= lastRowNum)
                        SelectedTestCases.Add(tc);
                }
                deleteSelectedRows();
                // Delete data in this row
                row.DeleteData();

            }
        }

        private long getNextPegWinRowNum(long selectedRow)
        {
            //long rowNum = 99999;

            // var tc1 =  TestCases.

            // long  maxRow = TestCases.Max(x => x.RunOrder && x.SelectedKeyword.KeywordName.Equals("PegWindow"));
            long lMaxRow = -1;
            for (int i = (int)selectedRow; i < TestCases.Count; i++)
            {
                if (TestCases[i].SelectedKeyword == null)
                {
                    lMaxRow = TestCases[i].RunOrder;
                    continue;
                }
                if (TestCases[i].SelectedKeyword.KeywordName == null)
                {
                    lMaxRow = TestCases[i].RunOrder;
                    continue;
                }
                if (string.Compare(TestCases[i].SelectedKeyword.KeywordName, "PegWindow", true) == 0)
                {
                    //lMaxRow = TestCases[i].RunOrder;
                    break;
                }
            }
            //long? maxRow = TestCases.Where(x => x.SelectedKeyword.KeywordName.Equals("PegWindow")
            //                        && x.RunOrder > selectedRow)
            //                         .Max(y => (long?)y.RunOrder);

            //if (maxRow != null)
            //    rowNum = maxRow.Value;
            //return rowNum;
            return lMaxRow;
        }

        internal void DeleteRelatedData()
        {
            if (SelectedTestCases.Count == 0)
                return;

            var row = SelectedTestCases[0];
            row.DeleteRelatedDataSettings();
            deletedDataSettigs.Add(row.StepNo);
            OnPropertyChanged("TestCases");
        }

        internal void LoadSelectedDataSheet(long dataSheetId)
        {
            Console.WriteLine("LoadSelectedDataSheet");
        }

        internal void PopulateIds(long projectId, long testSuiteId, long testCaseId)
        {
            _projectId = projectId;
            _testSuiteId = testSuiteId;
        }

        internal void SaveSharedDataSheetAs(string dataSetName, string dataSetDescription)
        {
            // DataTable dt = DataTableUtil.ToDataTable(TestCases);

            long dsId = 0;
            SharedDataSetViewModel.SaveAs(dataSetName, dataSetDescription, _testCaseId, TestCases, ref dsId);
            return;
#region removed code

            /* removed, trash codes
            Console.WriteLine("ContextName = " + dataSetName);

            // save test data summary
            long summaryId = BoHelper.CreateTestDataSummary(dataSetName);

            // save rel_test_case_test_data_summary
            BoHelper.CreateRelTCDataSummary(summaryId, _testCaseId);

            // save test data
            SaveSharedDataSettings(summaryId);
             */
#endregion
        }
        /* removed, no referenced code, tiger, 6-15-2017
        private bool SaveSharedDataSettings(long summaryId)
        {
            Console.WriteLine("SaveSharedDataSettings summaryId = " + summaryId);
            Dictionary<string, int>[] objectOccuranceDictArray = new Dictionary<string, int>[21];

            List<B_TEST_DATA_SETTING> bTestDataSettingList = new List<B_TEST_DATA_SETTING>();
            List<B_SHARED_OBJECT_POOL> bSharedObjectPoolList = new List<B_SHARED_OBJECT_POOL>();

            // save delete datasettings -- those that are deleted due to change of keyword or object

            foreach (int stepId in deletedDataSettigs)
            {
                BoHelper.DeleteDataSettings(stepId);
            }
            // We need to save changes at this point (old DataSettings) so that we don't accidentally delete the new dataSettings
            BoHelper.SaveChanges();
            deletedDataSettigs.Clear();

            // Save deleeted rows to database
            try
            {
                BoHelper.DeleteTestSteps(deletedTestSteps);
                //foreach (var stepId in deletedTestSteps)
                //{
                //    BoHelper.DeleteTestStep(stepId);
                //}
                deletedTestSteps.Clear();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error while saving deleted records.", ex.ToString());
            }

            // Save TEST_STEPS, test data settings

            int runOrderDelta = 0;
            foreach (var teststeps in TestCases)
            {
                teststeps.InitDataSets();
                // If this is a new test step 
                if (BoHelper.GetTestStepByID(teststeps.StepNo) == null)
                {
                    // AF added the next  lines to exclude entries that have no keyword at all
                    if (teststeps.SelectedKeyword == null ||
                        !TestCaseEditViewModel.staticKeywordNames.Contains(teststeps.SelectedKeyword._keywordName) && (teststeps.SelectedKeyword == null || teststeps.SelectedObjectName == null))
                    {
                        runOrderDelta++;
                        continue;
                    }

                    B_TEST_STEPS bTestSteps = new B_TEST_STEPS();
                    bTestSteps.TEST_CASE_ID = _testCaseId;
                    bTestSteps.STEPS_ID = BoHelper.GetTestStepsId(); //assign new stepsId
                    bTestSteps.KEY_WORD_ID = teststeps.SelectedKeyword.Id;
                    if (teststeps.SelectedObjectName != null)
                        bTestSteps.OBJECT_ID = teststeps.SelectedObjectName.Id;
                    bTestSteps.COLUMN_ROW_SETTING = teststeps.RowColumn;
                    bTestSteps.VALUE_SETTING = teststeps.RowColumnValue;

                    if (teststeps.IsRunChecked)
                        bTestSteps.IS_RUNNABLE = 1;
                    else
                        bTestSteps.IS_RUNNABLE = 0;

                    bTestSteps.RUN_ORDER = teststeps.RunOrder - runOrderDelta;

                    bTestSteps.COMMENT = teststeps.Comment;

                    // CREATE POOL RECORDS !!!
                    // List<B_SHARED_OBJECT_POOL> testStepList = new List<B_SHARED_OBJECT_POOL>();
                    for (int count = 1; count <= MarsConstants.NumberOfDataSetColumns; count++)
                    {
                        var testDataSetting = AddTestDataSettings(teststeps, count, bTestSteps, summaryId);
                        if (testDataSetting != null)
                        {
                            //testStepList.Add(testDataSetting);
                            //bTestDataSettingList.Add(testDataSetting);

                            B_SHARED_OBJECT_POOL pool = PopulateSharedObjectPool(testDataSetting, teststeps, objectOccuranceDictArray);

                            bSharedObjectPoolList.Add(pool);
                        }

                    }

                    // bTestStepsList.Add(bTestSteps);
                }
                else
                {
                    // Updating an existing test step
                    var selTestStep = BoHelper.GetTestStepByID(teststeps.StepNo);

                    Console.WriteLine("Processing step #" + selTestStep.STEPS_ID);

                    if (teststeps.SelectedKeyword != null)
                        selTestStep.KEY_WORD_ID = teststeps.SelectedKeyword.Id;
                    if (teststeps.SelectedObjectName != null)
                        selTestStep.OBJECT_ID = teststeps.SelectedObjectName.Id;
                    selTestStep.COLUMN_ROW_SETTING = teststeps.RowColumn;
                    selTestStep.VALUE_SETTING = teststeps.RowColumnValue;

                    if (teststeps.IsRunChecked)
                        selTestStep.IS_RUNNABLE = 1;
                    else
                        selTestStep.IS_RUNNABLE = 0;

                    selTestStep.RUN_ORDER = teststeps.RunOrder;
                    selTestStep.COMMENT = teststeps.Comment;

                    // CREATE POOL RECORDS !!!
                    List<B_TEST_DATA_SETTING> bTestDataSettingLocalList = BoHelper.LoadBOTestDataSettings(teststeps.StepNo, summaryId, objDbCntx);
                    for (int count = 1; count <= MarsConstants.NumberOfDataSetColumns; count++)
                    {
                        var testDataSetting = bTestDataSettingLocalList.FirstOrDefault(a => a.LOOP_ID == count);
                        if (testDataSetting == null)
                        {

                            testDataSetting = AddTestDataSettings(teststeps, count, selTestStep, summaryId);
                            if (testDataSetting != null)
                            {
                                B_SHARED_OBJECT_POOL pool = PopulateSharedObjectPool(testDataSetting, teststeps, objectOccuranceDictArray);

                                bSharedObjectPoolList.Add(pool);

                            }

                        }
                        else
                        {
                            UpdateTestDataSettings(teststeps, count, ref testDataSetting);
                            bTestDataSettingList.Add(testDataSetting);
                        }
                    }
                }

            } //forloop close

            // if (marsEntities.SaveChanges() > 0)

            if (BoHelper.SaveObjectPool(bSharedObjectPoolList) > 0)
            {
                //MarsTreeView.GetMarsTree();
                //LoadTestSteps(_testCaseId, _testCaseName);
                System.Windows.MessageBox.Show("Shared Data Settings saved successfully");
                return true;
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to save Shared Data Settings");
                return false;
            }

        }
        

        private B_SHARED_OBJECT_POOL PopulateSharedObjectPool(B_TEST_DATA_SETTING testDataSetting,
                                                              TestCaseEditViewModel teststeps,
                                                              Dictionary<string, int>[] objectOccuranceDictArray)
        {
            B_SHARED_OBJECT_POOL pool = new B_SHARED_OBJECT_POOL();
            pool.OBJECT_POOL_ID = BoHelper.GetTestStepsId();
            pool.LOOP_ID = (long?)testDataSetting.LOOP_ID;

            pool.DATA_VALUE = testDataSetting.DATA_VALUE;
            //pool.T_TEST_DATA_SUMMARY_DATA_SUMMARY_ID = (long)testDataSetting.DATA_SUMMARY_ID;
            pool.DATA_SUMMARY_ID = testDataSetting.DATA_SUMMARY_ID;
            if (teststeps.SelectedObjectName != null)
                pool.OBJECT_NAME = "obj_" + teststeps.SelectedObjectName.ObjName;
            else
                pool.OBJECT_NAME = "kw_" + teststeps.SelectedKeyword.KeywordName;

            int loopId = (int)pool.LOOP_ID;
            if (objectOccuranceDictArray[loopId] == null)
                objectOccuranceDictArray[loopId] = new Dictionary<string, int>();

            Dictionary<string, int> objectOccuranceDict = objectOccuranceDictArray[loopId];

            if (objectOccuranceDict.ContainsKey(pool.OBJECT_NAME))
                objectOccuranceDict[pool.OBJECT_NAME]++;
            else
                objectOccuranceDict.Add(pool.OBJECT_NAME, 0);

            pool.OBJECT_ORDER = objectOccuranceDict[pool.OBJECT_NAME];

            return pool;
        }
        */

        private string _title;

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }


        internal static string GetTestCaseNameById(long lTestCaseId)
        {
            T_TEST_CASE_SUMMARYDTO objTC = B_TEST_CASE.GetTestCaseInfoByName(lTestCaseId);
            if (objTC == null) return "";
            return objTC.TEST_CASE_NAME;
        }

        public bool isTestCaseCreated { get; set; }

        internal void SetApplicationId(long appId)
        {
            this.ApplicationId = appId;
        }

        public long ApplicationId { get; set; }

        internal void LinkDataSet(string strDBIdx, long dataSetId)
        {
            SharedDataSetViewModel.Link(strDBIdx, _testCaseId, dataSetId, TestCases);
        }

        internal void UnLinkDataSet(string strDBIdx, long dataSetId)
        {
            SharedDataSetViewModel.UnLink(strDBIdx, _testCaseId, dataSetId, TestCases);
        }

        internal void PopulateDataFromObjectPool()
        {
            if (SelectedTestCases == null) return;
            if (SelectedTestCases.Count <= 0) return;
            if (SelectedTestCases[0] != null &&
                //SelectedTestCases[0].RunOrder != null && 
                SelectedTestCases[0].RunOrder != 0)
            {

                List<B_SHARED_OBJECT_POOL> sharedObjectPoolList = SharedDataSetViewModel.GetPoolDataForTestStep(
                    MarsMainWindow.CurrentDatabaseIdx,
                    _dataSheetId,
                                                                            TestCases,
                                                                            this.SelectedTestCases[0].RunOrder);
                SelectedTestCases[0].SetDataSetColumns(sharedObjectPoolList);

            }
        }

#if _TestStepUnit
        private string GetPegwindowURL(TestStepViewModel objStep,ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetPegwindowURL");
            if (objStep==null)
            {
                Logger.Error("GetPegwindowURL",strError = "Source object is null");
                isOk = false;
                return "";
            }

            if (objStep.SelectedObjectName==null)
            {
                Logger.Error("GetPegwindowURL",strError = "objStep.SelectedObjectName is null");
                isOk = false;
                return "";
            }

            string strDefaultWindowPrefix = "Window";
            if (objStep.SelectedObjectName.AssignedDto != null)
            {
                /// get window type for vbs by type id
                /// 
                isOk = true;
                strDefaultWindowPrefix = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeById(MarsMainWindow.CurrentDatabaseIdx, 
                    objStep.SelectedObjectName.AssignedDto.TYPE_ID, ref isOk, ref strError);
                if (!isOk)
                {
                    strDefaultWindowPrefix = "Window";
                }
                else
                {
                    if (string.Compare("Pegwindow", strDefaultWindowPrefix,true)==0)
                    {
                        strDefaultWindowPrefix = "Window";
                    }
                }
            }

            /// 算法：
            /// 1， 是否存在系统
            /// 2， 获得当前application类型，创建不同的pegwindow的前缀
            /// 3， 是否是具有前缀的quick——access信息，如果是，增加迭代处理
            /// 4， 返回数据
            /// 
            string strPegWinPrefix= ConfigObjectBase.GetCurrentApplicationPrefixForQuickAccess(strDefaultWindowPrefix);
            string strOrgQuickAccess = objStep.SelectedObjectName.AssignedDto.QUICK_ACCESS;
            string strAttchPeg = "";
            string strTmp = ConfigObjectBase.DealWithObjectURLWithAttach(strOrgQuickAccess, strPegWinPrefix, ref strAttchPeg);           

            string strURL = MarsTestFrame.systemUtil.TigerMarsUtil.ConvertQuickAccess2CommaMode(strOrgQuickAccess);
            return strTmp;
            //return string.Format("{0}({1})", strPegWinPrefix, strURL);
        }


#endif
    }

    public class TestStepViewModel : Notify
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestStepViewModel));

        /// <summary>
        /// this list is used only for pegwindow, therefore, a pegwindow object will replace it
        /// </summary>
        public ObservableCollection<TestStepViewModel> testCaseColl;
        public TestStepViewModel OwnerPegwindow;

        public readonly static List<string> staticKeywordNames = new List<string>(){
            "PegWindow",
            "KillApplication",
            "LoadVariables",
            "Dismiss",
            "Loop",
            "EndLoop",
            "WaitForSeconds",
            "LoadVariables",
            "WaitUntil",
            "ResumeNext",
            "SummitLaunch",
            "IF",
            "ELSE",
            "IFEND",
            "StartApplication",
            "CopyExcelRangeToClipboard",
            "ExecuteCommand",
            "DBCompare"
        };

        ObservableCollection<Keyword> tmpStaticKeyword;
        public ObservableCollection<Keyword> TmpStaticKeyword
        {
            get
            {
                if (tmpStaticKeyword == null)
                    InitTmpKeywordForTest();
                return tmpStaticKeyword;
            }
            set {
                tmpStaticKeyword = value;
                OnPropertyChanged("TmpStaticKeyword");
            }

        }
        private void InitTmpKeywordForTest()
        {
            tmpStaticKeyword = new ObservableCollection<Keyword>();

            staticKeywordNames.ForEach(p => {
                Keyword k = new Keyword()  ;
                k.KeywordName = p;
                tmpStaticKeyword.Add(k);
            });
        }

        ObservableCollection<string> tmpKeywordStrings=new ObservableCollection<string> { "test1","test2"};
        public ObservableCollection<string> TMPKEYWORDSTRINGS
        {
            get
            {
                return tmpKeywordStrings;
            }
            set
            {
                tmpKeywordStrings = value;
                OnPropertyChanged("TMPKEYWORDSTRINGS");
            }
        }

         
        


        public Object[] DataSets = new Object[21];
        public void InitDataSets()
        {
            DataSets[1] = this.DataSet1;
            DataSets[2] = this.DataSet2;
            DataSets[3] = this.DataSet3;
            DataSets[4] = this.DataSet4;
            DataSets[5] = this.DataSet5;
            DataSets[6] = this.DataSet6;
            DataSets[7] = this.DataSet7;
            DataSets[8] = this.DataSet8;
            DataSets[9] = this.DataSet9;
            DataSets[10] = this.DataSet10;
            DataSets[11] = this.DataSet11;
            DataSets[12] = this.DataSet12;
            DataSets[13] = this.DataSet13;
            DataSets[14] = this.DataSet14;
            DataSets[15] = this.DataSet15;
            DataSets[16] = this.DataSet16;
            DataSets[17] = this.DataSet17;
            DataSets[18] = this.DataSet18;
            DataSets[19] = this.DataSet19;
            DataSets[20] = this.DataSet20;

        }

        private Brush _currentDataCellColor;
        public Brush currentDataCellColor
        {
            get { return _currentDataCellColor; }
            set {
                
                _currentDataCellColor = value;                
                OnPropertyChanged("currentDataCellColor");


            }
        }

        private bool isApplyAnamation = false;
        public bool IsApplyAnamation
        {
            get
            {
                return isApplyAnamation;
            }
            set
            {
                isApplyAnamation = value;
                //if (IsApplyAnamation)
                //{

                //    System.Windows.Media.Animation.ColorAnimation objColorAnimation = new System.Windows.Media.Animation.ColorAnimation();
                //    objColorAnimation.From = Brushes.LightBlue.Color;
                //    objColorAnimation.To = null;

                //    System.Windows.Media.Animation.Storyboard.ta
                //}
                OnPropertyChanged("IsApplyAnamation");
            }
        }


        private bool _AutoGenStatus = false;
        public bool AutoGenStatus
        {
            get { return _AutoGenStatus; }
            set
            {
                _AutoGenStatus = value;
                OnPropertyChanged("AutoGenStatus");
            }
        }

        public static ObservableCollection<Keyword> PopulateKeywordsById(long testCaseId)
        {
            var _staticKeywords = new List<Keyword>();
            B_KEYWORD objKeyword = new B_KEYWORD();

            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> _keywordNames = objKeyword.GetKeyWordNames(staticKeywordNames);
            if (_keywordNames == null) return null;
            foreach (var keyword in _keywordNames.Keys)
            {
                _staticKeywords.Add(new Keyword(keyword, _keywordNames[keyword], null, testCaseId));
            }
            //_staticKeywords.Sort();
            return new ObservableCollection<Keyword>(_staticKeywords);
        }


        /// <summary>
        /// statickeyword information. It should be cached for performance. ideally, it should be second level for caching in bussiness object, but 
        /// it is diffcult to change to currently result from ugly coding and system arch.
        /// tiger, 2016-4-1
        /// </summary>
        internal static ObservableCollection<Keyword> cachedStaticKeyword = null;

        public ObservableCollection<Keyword> _keywords;
        public ObservableCollection<Keyword> Keywords
        {
            get {
                return _keywords;
            }
            set
            {
                //_keywords = value;
                if (value != null)
                    _keywords = new ObservableCollection<Keyword>(value.OrderBy(x => x.KeywordName));
                else
                    _keywords = value;
                OnPropertyChanged("Keywords");
            }
        }

        private ObservableCollection<MarsKeyValues<int, string>> _TestStepSkipSettings= MarsSkipMgr.GetMarsSkipSettins();
        public ObservableCollection<MarsKeyValues<int, string>> TestStepSkipSettings
        {
            get
            {
                return _TestStepSkipSettings;
            }
            set
            {
                _TestStepSkipSettings = value;
                OnPropertyChanged("TestStepSkipSettings");
            }
        }

        private MarsKeyValues<int, string> _MarsSkipSetting ;
        public MarsKeyValues<int, string>  MarsSkipSetting
        {
            get
            {
                return _MarsSkipSetting;
            }
            set
            {
                _MarsSkipSetting = value;
                OnPropertyChanged("MarsSkipSetting");
            }
        }

        internal class RowColorMgr
        {
            static Brush[] SolidBrushForDataGrid = new Brush[2]
            {
                Brushes.Transparent,
                Brushes.WhiteSmoke
            };
            internal static Brush GetRowBrushByIdx(int iIdx)
            {
                return SolidBrushForDataGrid[iIdx % 2];
            }

            internal static Brush SearchHitBrush = Brushes.LightGreen;
        }

        public void SetPegwindowIdx(int iPegIdx)
        {
            CurrentRowColor = RowColorMgr.GetRowBrushByIdx(iPegIdx);
            _CurrentRowDefaultColor = CurrentRowColor;
        }

        
        private Brush _CurrentRowColor = Brushes.LightBlue;
        public Brush CurrentRowColor
        {
            get { return _CurrentRowColor; }
            set { _CurrentRowColor = value;
                OnPropertyChanged("CurrentRowColor");
            }
        }

        private Brush _CurrentRowDefaultColor=null;


        /// <summary>
        /// _subObjects, children objects belong to the current peg window
        /// Only exists when the ojbect is pegwindow
        /// </summary>
        private ObservableCollection<ObjectName> _subObjects;
        public ObservableCollection<ObjectName> SubObjects
        {
            get
            {
                if (_subObjects == null)
                {
                    /// try to get data from DB based on peg object id
                    /// 
                    try
                    {
                        _subObjects = FetchSubChildrenWhenObjectIsPeg();
                    }
                    catch (Exception e)
                    {
                        Logger.Error("SubObjects-get", string.Format("Exception:[{0}]", e.Message), e);
                        _subObjects = null;
                    }
                }
                return _subObjects;
            }

        }

        internal ObservableCollection<ObjectName> _objects;
        public ObservableCollection<ObjectName> Objects
        {
            get { return _objects; }
            set
            {
                //_objects = value;
                _objects = new ObservableCollection<ObjectName>(value.OrderBy(x => x.ObjName));
                OnPropertyChanged("Objects");
            }
        }

        ///// <summary>
        ///// object from pegwindow sub bojects.
        ///// 只有在pegwindow有值
        ///// </summary>
        //public ObservableCollection<ObjectName> _pegSubobjects;
        //public ObservableCollection<ObjectName> PegSubobjects
        //{
        //    get { return _pegSubobjects; }
        //    set
        //    {
        //        _pegSubobjects = new ObservableCollection<ObjectName>(value.OrderBy(x => x.ObjName));
        //        OnPropertyChanged("PegSubobjects");
        //    }
        //}

        List<long> _applicationIds;
        public List<long> GetAssignedAppId()
        {
            return _applicationIds==null?new List<long>() { -1}:_applicationIds;
        }
        internal List<long> _objectTypeIds = new List<long>();

        public List<ObjectName> PopulateObjectsByObjectParent(string ObjectParentName, long testcaseId)
        {

            Logger.Info("PopulateObjectsByObjectType", string.Format("ObjectType:[{0}] testcaseId:[{1}]", ObjectParentName, testcaseId));
            B_REGISTED_OBJECT regObj = new B_REGISTED_OBJECT();

            var appIds = GetRelAppIdsByTCId(MarsMainWindow.CurrentDatabaseIdx, testcaseId);
#if v_16AndUp
            List<B_REGISTED_OBJECT> regObjectList = regObj.GetRegisterdObjectsByObjectParentFromCache(MarsMainWindow.CurrentDatabaseIdx, ObjectParentName, appIds);
#else
            List<T_REGISTED_OBJECTDTO> regObjectList = regObj.GetRegisterdObjectsByObjectTypeFromCache(ObjectParentName, appIds);
#endif



#if PERFORMANCE_TRACKING
            Logger.Info("PopulateObjectsByObjectType", "after GetRegisterdObjectsByObjectTypeFromCache");
#endif
            List<ObjectName> objects = new List<ObjectName>();
            _objectTypeIds.Clear();
#if v_16AndUp
            foreach (B_REGISTED_OBJECT objRegInfo in regObjectList)
#else
            foreach (T_REGISTED_OBJECTDTO objRegInfo in regObjectList)
#endif
            {
                objects.Add(new ObjectName(objRegInfo));
                long objectTypeId = (long)objRegInfo.TYPE_ID;
                //if (!_objectTypeIds.Contains(objectTypeId))
                _objectTypeIds.Add(objectTypeId);
            }

#if v_16AndUp && v_useNameId
            objects = ObjectName.CompactList(objects).ToList();
#endif
            /// As object name can be reused by applications, 
            /// 
            //_objectTypeIds = _objectTypeIds.Distinct().ToList();
            Logger.logEnd("PopulateObjectsByObjectType");
            return objects;
        }

#region Methods need change
        public ObservableCollection<ObjectName> PopulateObjectsByObjectType(string ObjectType, string testCaseName)
        {
            var objects = new ObservableCollection<ObjectName>();

            B_REGISTED_OBJECT regObj = new B_REGISTED_OBJECT();
            // B_KEYWORD objKeyword = new B_KEYWORD();

            var appIds = GetRelAppIds(MarsMainWindow.CurrentDatabaseIdx, testCaseName);
            var regObjectList = regObj.GetReistedObjectsByObjectType(MarsMainWindow.CurrentDatabaseIdx, ObjectType, appIds);

            foreach (var regObjName in regObjectList)
            {
#if v_16AndUp
                objects.Add(new ObjectName(regObjName.OBJECT_ID, regObjName.OBJECT_HAPPY_NAME, regObjName.ENUM_TYPE, regObjName.OBJECT_TYPE, regObjName.QUICK_ACCESS, regObjName.APPLICATION_ID ?? -1));
#else
                objects.Add(new ObjectName(regObjName.OBJECT_ID, regObjName.OBJECT_HAPPY_NAME, regObjName.ENUM_TYPE, regObjName.OBJECT_TYPE, regObjName.QUICK_ACCESS));
#endif
                var objectTypeId = (long)regObjName.TYPE_ID;
                if (!_objectTypeIds.Contains(objectTypeId))
                    _objectTypeIds.Add(objectTypeId);
            }
#if v_16AndUp && v_useNameId
            objects = ObjectName.CompactList(objects.ToList());
#endif
            return objects;
        }
#endregion ///Methods need change

        private ObservableCollection<ObjectName> FetchSubChildrenWhenObjectIsPeg()
        {
            Logger.logBegin("FetchSubChildrenWhenObjectIsPeg");
            B_REGISTED_OBJECT regObj = new B_REGISTED_OBJECT();
            List<B_REGISTED_OBJECT> lst = regObj.FetchObjectsByTypeId(MarsMainWindow.CurrentDatabaseIdx, 
                this.SelectedObjectName.Id);
            ObservableCollection<ObjectName> objResult = new ObservableCollection<ObjectName>();
            foreach (B_REGISTED_OBJECT regObjName in lst)
#if v_16AndUp
                objResult.Add(new ObjectName(regObjName.OBJECT_ID, regObjName.OBJECT_HAPPY_NAME, regObjName.ENUM_TYPE, regObjName.OBJECT_TYPE, regObjName.QUICK_ACCESS, regObjName.APPLICATION_ID ?? -1));
#else
                objResult.Add(new ObjectName(regObjName.OBJECT_ID, regObjName.OBJECT_HAPPY_NAME, regObjName.ENUM_TYPE, regObjName.OBJECT_TYPE, regObjName.QUICK_ACCESS));
#endif
#if v_16AndUp && v_useNameId
            objResult = ObjectName.CompactList(objResult.ToList());
#endif
            return objResult;
        }
        public ObservableCollection<ObjectName> PopulateObjectsByObjectTypeAndKeyword(string ObjectType, long testcaseId, Keyword selKeyword)
        {
            Logger.Info("PopulateObjectsByObjectTypeAndKeyword", string.Format("ObjectType:[{0}] testcaseId:[{1}] selKeyword:[{2}]", ObjectType, testcaseId, selKeyword));
            B_REGISTED_OBJECT regObj = new B_REGISTED_OBJECT();
            B_KEYWORD objKeyword = new B_KEYWORD();

            //List<long> typeidList = objKeyword.GetTypeId(selKeyword.Id); //Get Type ids for selected keyword 
            List<long> typeidList = selKeyword.GetTypeIdList();
#if v_16AndUp
            List<B_REGISTED_OBJECT> regObjectList = regObj.GetRegObjectsByTCId_KWId_Parent(MarsMainWindow.CurrentDatabaseIdx, 
                ObjectType, testcaseId, selKeyword.Id);
#else
            List<T_REGISTED_OBJECTDTO> regObjectList = regObj.GetRegObjectsByTCId_KWId_Parent(ObjectType, testcaseId,selKeyword.Id);
#endif
            var lstObjects = new ObservableCollection<ObjectName>();
            if (regObjectList == null) return lstObjects;
            foreach (T_REGISTED_OBJECTDTO objDtoTmp in regObjectList)
            {
#if v_16AndUp

                lstObjects.Add(new ObjectName((B_REGISTED_OBJECT)objDtoTmp));
#else
                lstObjects.Add(new ObjectName(objDtoTmp));
#endif
            }
#if v_16AndUp && v_useNameId
            lstObjects = ObjectName.CompactList(lstObjects.ToList());
#endif
            Logger.logEnd("PopulateObjectsByObjectTypeAndKeyword");
            return lstObjects;
        }

#region Method need change
        private List<long> GetRelAppIds(string strDBIdx, string testCaseName)
        {
            if (_applicationIds == null || _applicationIds.Count == 0)
            {
                B_REL_APP_TESTCASE bRelAppTestCaseObject = new B_REL_APP_TESTCASE();
                _applicationIds = bRelAppTestCaseObject.GetApplicationsIds(strDBIdx,testCaseName);
            }
            return _applicationIds;
        }
#endregion ///Method need change

        private List<long> GetRelAppIdsByTCId(string strDBIdx, long tcId)
        {
            if (_applicationIds == null || _applicationIds.Count == 0)
            {
                B_REL_APP_TESTCASE bRelAppTestCaseObject = new B_REL_APP_TESTCASE();
                _applicationIds = bRelAppTestCaseObject.GetApplicationsIdsByTCId(strDBIdx, tcId);
            }
            return _applicationIds;
        }

        //public ObservableCollection<ObjectName> PopulateObjectsByKeyword(long keywordId, string testCaseName)
        public ObservableCollection<ObjectName> PopulateObjectsByKeyword(long keywordId, long testCaseId)
        {
            Logger.logBegin("PopulateObjectsByKeyword");
            var objects = new List<ObjectName>();


            B_REGISTED_OBJECT regObj = new B_REGISTED_OBJECT();
            B_KEYWORD objKeyword = new B_KEYWORD();

            List<long> typeidList = objKeyword.GetTypeId(keywordId, MarsMainWindow.CurrentDatabaseIdx); //Get Type ids for selected keyword 
            var appIds = GetRelAppIdsByTCId(MarsMainWindow.CurrentDatabaseIdx, testCaseId);
            var regObjectList = regObj.GetRegistedObjects(MarsMainWindow.CurrentDatabaseIdx, typeidList, appIds);

            foreach (var regObjName in regObjectList.OrderBy(p => p.OBJECT_HAPPY_NAME))
            {
                objects.Add(new ObjectName(regObjName));
            }

#if v_16AndUp && v_useNameId
            Objects = new ObservableCollection<ObjectName>(ObjectName.CompactList(objects));
#else
            Objects = new ObservableCollection<ObjectName>(objects);
#endif
            Logger.logEnd("PopulateObjectsByKeyword");
            return Objects;
        }



        public TestStepViewModel()
        {
            InitDataSets();
        }

        public TestStepViewModel(string testCaseName)
        {
            InitDataSets();
        }

        public TestStepViewModel(TestStepViewModel latsRowViewmodel, long runOrder, long testCaseId, ref ObservableCollection<TestStepViewModel> TestCaseColl)
        {
#if PERFORMANCE_TRACKING
            long tm = DateTime.Now.Ticks;
            Logger.Info("----性能记录---",string.Format("current :{0}",tm));
#endif
            InitDataSets();
            //TestCaseName = testCaseName;
            _testCaseId = testCaseId;
            testCaseColl = TestCaseColl;

            if ((runOrder == 1)||(latsRowViewmodel==null))
            {
                Keywords = PopulateKeywordsById(_testCaseId);
            }
            else
            {
                if (latsRowViewmodel.SelectedKeyword != null)
                {
                    if (latsRowViewmodel.SelectedKeyword.KeywordName == "PegWindow")
                    {
                        var _filteredKeywords = new ObservableCollection<Keyword>();
                        var _objects = new ObservableCollection<ObjectName>();

                        //Populate Objects based on previous object type + application id 
                        if (latsRowViewmodel.SelectedObjectName != null)
                        {
                            List<ObjectName> lstTmpObj = PopulateObjectsByObjectParent(latsRowViewmodel.SelectedObjectName.ObjectType, _testCaseId);
                            _objects = new ObservableCollection<ObjectName>(lstTmpObj);

                            //Populate Keyword based on loaded object typeIds
                            B_KEYWORD objKeyword = new B_KEYWORD();
                            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> _keywordNames = objKeyword.GetKeyWordNamesByObjectTypeId(_objectTypeIds);

                            foreach (var keyword in _keywordNames.Keys)
                            {
                                _filteredKeywords.Add(new Keyword(keyword, _keywordNames[keyword], "", _testCaseId));
                            }

                            Keywords = _filteredKeywords;
                            Objects = _objects;
                        }
                    }
                    else
                    {
                        Keywords = latsRowViewmodel.Keywords;
                        Objects = latsRowViewmodel.Objects;
                    }
                }

            }
            RunOrder = runOrder;
            IsRunChecked = true;
            StepNo = 0;
            RowColumn = "";
            RowColumnValue = "";
            Comment = "";
            SetDataSetColumns(new List<TEST_DATA_SETTING>());
#if PERFORMANCE_TRACKING
            tm = DateTime.Now.Ticks-tm;
            Logger.Info("----性能记录---", string.Format("cost :{0}", tm));
#endif
        }


        public TestStepViewModel(bool isRunnable, long stepNo, long runOrder,
            string rowColumn, string rowColumnValue, string comment,
            long keywordId, long objectId,
            List<TEST_DATA_SETTING> testDataSettings, long testcaseId,
#if v_16AndUp
            long? objNameId,
#endif
            TestStepViewModel prevRowViewmodel, ref ObservableCollection<TestStepViewModel> TestCaseColl,
            ObservableCollection<Keyword> staticKeyWordsFromOuter = null)
        {
            Logger.logBegin("TestCaseEditViewModel");
#if PERFORMANCE_TRACKING
            long tm = DateTime.Now.Ticks;
            Logger.Info("----性能记录---", string.Format("current :{0}", tm));
#endif
            InitDataSets();
            testCaseColl = TestCaseColl;
            RunOrder = runOrder;
            IsRunChecked = isRunnable;

            IsSkipForDataset = SetSkipForInfo(testDataSettings);
            isSkipForDataSetInitValue = isSkipForDataset;

            StepNo = stepNo;
            RowColumn = rowColumn;
            RowColumnValue = rowColumnValue;
            Comment = comment;

            this.TestCaseId = testcaseId;
#if v_16AndUp
            //该信息没用了 因为objectId为null或者0，采用objectNameId了
            //B_REGISTED_OBJECT objB = B_REGISTED_OBJECT.GetObjectById(objectId);
            ///Steps:
            /// 1, get object happy name

            this.ObjectNameId = objNameId;
#else
            T_REGISTED_OBJECTDTO objB = B_REGISTED_OBJECT.GetObjectById(objectId);
#endif
            string strObjNameTmp = "";
            //if (objB != null)
            //    strObjNameTmp = objB.OBJECT_HAPPY_NAME;

            ObservableCollection<Keyword> _staticKeywords = staticKeyWordsFromOuter == null ? PopulateKeywordsById(testcaseId) : new ObservableCollection<Keyword>(staticKeyWordsFromOuter);
#if PERFORMANCE_TRACKING
            long tmpTmp = DateTime.Now.Ticks;
            Logger.Info("----性能记录---", string.Format("current cost PopulateKeywordsById:{0} ms", (tmpTmp - tm) / 10000.0));
#endif
            // AF added doing this only for case where prevRowViewmodel == null
            Keyword currentTmpKeyword = null;
            if ((currentTmpKeyword=_staticKeywords.Where(a => a.Id == keywordId).FirstOrDefault()) != null && prevRowViewmodel == null)
            {
                Keywords = _staticKeywords;
            }
            else
            {
                if (prevRowViewmodel != null && prevRowViewmodel.SelectedKeyword != null)
                {
                    if (prevRowViewmodel.SelectedKeyword.KeywordName == "PegWindow")
                    {
                        var _filteredKeywords = new ObservableCollection<Keyword>();
                        var _objects = new ObservableCollection<ObjectName>();

                        //Populate Objects based on previous object type + application id 
                        if (prevRowViewmodel.SelectedObjectName != null)
                        {
#if PERFORMANCE_TRACKING
                            long tmpTmp1 = DateTime.Now.Ticks;
                            Logger.Info("----性能记录---", string.Format("current before PopulateObjectsByObjectType:{0} ms", (tmpTmp - tmpTmp1) / 10000.0));
#endif
                            List<ObjectName> lstTmpObj = PopulateObjectsByObjectParent(prevRowViewmodel.SelectedObjectName.ObjectType, testcaseId);
                            _objects = new ObservableCollection<ObjectName>(lstTmpObj);

                            //Populate Keyword based on loaded object typeIds
                            B_KEYWORD objKeyword = new B_KEYWORD();
                            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> _keywordNames = objKeyword.GetKeyWordNamesByObjectTypeId(_objectTypeIds);
                            //_keywordNames.Keys.OrderBy(p => p.KEY_WORD_NAME);
                            foreach (var keyword in _keywordNames.Keys.OrderBy(p => p.KEY_WORD_NAME))
                            {
                                _filteredKeywords.Add(new Keyword(keyword, _keywordNames[keyword], "", testcaseId));
                            }

                            Keywords = _filteredKeywords;
                            Objects = _objects;

                            /// get all the sub objects for the pegwindow
#if PERFORMANCE_TRACKING
                            long tmpTmp2 = DateTime.Now.Ticks;
                            Logger.Info("----性能记录---", string.Format("current after PopulateObjectsByObjectType:{0} ms", (tmpTmp2 - tmpTmp1) / 10000.0));
#endif
                        }
                    }
                    else
                    {
                        ///如果当前的keyword是pegwind，需要获得所有的pegwindow对象
                        /// 因为在SelectedKeyword变化时候，会选择对象
                        /// 
                        //if (string.Compare("Pegwindow", currentTmpKeyword == null ? "" : currentTmpKeyword.KeywordName, true) == 0)
                        //{
                        //    Keywords = prevRowViewmodel.Keywords;
                        //    ///获得所有的pegwindowobject
                        //    /// 
                        //    Objects = GetPegwindowObjectsFromPegObjId(objB==null?-1:(objB.APPLICATION_ID??-1));                            
                        //}
                        //else
                        //{
                            Keywords = prevRowViewmodel.Keywords;
                            Objects = prevRowViewmodel.Objects;
                        //}
                    }
                }
                else
                {
                    //很难理解的逻辑。keyword和当前step的keywordid 有关，如果是为了要保留keywords的列表，完全可以通过 lastest pegwindow 获得，没有必要一层层传递
                    var latestPeg = TestCaseColl
                        .LastOrDefault(p => (string.Compare(p.SelectedKeyword==null?"":p.SelectedKeyword.KeywordName, "Pegwindow", true) == 0)
                        &&(p.RunOrder<=runOrder));
                    if (latestPeg == null)
                    {
                        Keywords = staticKeyWordsFromOuter;
                    }
                    else
                    {
                        Keywords = latestPeg.Keywords;
                        Objects = latestPeg.Objects;
                    }
                }
            }

            if (null != Keywords)
                SelectedKeyword = Keywords.Where(a => a.Id == keywordId).FirstOrDefault();

            if (null != SelectedKeyword)

#if !v_useNameId
                SelectedObjectName = Objects.Where(a => a.Id == objectId||string.Compare(a.ObjName, strObjNameTmp, true)==0).FirstOrDefault();
#else
                SelectedObjectName = Objects.Where(a => a.NameId == objNameId || string.Compare(a.ObjName, strObjNameTmp, true) == 0).FirstOrDefault();
#endif
            
            _dataSetDataType = "STRING";
#if v_16AndUp
            this._ObjectNameId = _selectedObjectName == null ? -1 : _selectedObjectName.NameId;
#endif
            if (SelectedObjectName != null && SelectedObjectName.EnumType != null)
            {
                B_REGISTED_OBJECT regObj = new B_REGISTED_OBJECT();
                List<string> enumTypeValue = regObj.GetTestEnumValues(MarsMainWindow.CurrentDatabaseIdx, SelectedObjectName.EnumType);

                if (enumTypeValue.Count > 1)
                {
                    _dataSetDataType = "ComboBox";
                    EnumTypeDataSource = enumTypeValue;
                }
                else
                {
                    if (SelectedObjectName.EnumType.CompareTo("Date") == 0)
                        _dataSetDataType = "DateTime";
                }
            }
            if (testDataSettings!=null)
                SetDataSetColumns(testDataSettings);
#if PERFORMANCE_TRACKING
            tm = DateTime.Now.Ticks-tm;
            Logger.Info("----性能记录---", string.Format("current cost:{0} ms", tm/ 10000.0));
#endif
            Logger.logEnd("TestCaseEditViewModel");
        }

        private bool SetSkipForInfo(List<TEST_DATA_SETTING> testDataSettings)
        {
            if (testDataSettings == null) return false;
            if (testDataSettings.Count <= 0) return false;
            if (testDataSettings[0] == null) return false;
            short s = testDataSettings[0].DATA_DIRECTION ?? 0;
            return (s&4)==4; //第三位是1
            //return testDataSettings[0].DATA_DIRECTION == null ? false: (testDataSettings[0].DATA_DIRECTION??0)&0b100)==1);
        }

        /// <summary>
        /// 通过一个对象的ID，获得所有的同属于该应用程序的所有的pegwindow对象
        /// </summary>
        /// <param name="lPegId"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private ObservableCollection<ObjectName> GetPegwindowObjectsFromPegObjId(long lAppId)
        {
            Logger.logBegin("GetPegwindowObjectsFromPegObjId",string.Format("Peg id:[{0}]", lAppId));
            try
            {
                ObservableCollection<B_REGISTED_OBJECT>  lstObj = B_REGISTED_OBJECT.GetPegwindowByAppId(MarsMainWindow.CurrentDatabaseIdx, lAppId);
                List<ObjectName> lstRslt = new List<ObjectName>();
                if (lstObj!=null)
                {
                    foreach (var o in lstObj)
                    {
                        if (o == null) continue;
                        ObjectName oN = new ObjectName(o);
                        lstRslt.Add(oN);
                    }
                }
                return new ObservableCollection<ObjectName>(lstRslt.OrderBy(p=>p.ObjectDisplayName));
            }catch(Exception e)
            {
                Logger.Error("GetPegwindowObjectsFromPegObjId",string.Format("Exception:[{0}]",e.Message),e);
                return new ObservableCollection<ObjectName>();
            }
            finally
            {
                Logger.logEnd("GetPegwindowObjectsFromPegObjId");
            }
        }

        private void SetDataSetColumns(List<TEST_DATA_SETTING> testDataSettings)
        {
            foreach (var testDataSetting in testDataSettings)
            {
                if (testDataSetting == null) continue;
                switch (testDataSetting.LOOP_ID.ToString())
                {
                    case "1":
                        _dataSet1 = testDataSetting.DATA_VALUE;
                        break;
                    case "2":
                        _dataSet2 = testDataSetting.DATA_VALUE;
                        break;
                    case "3":
                        _dataSet3 = testDataSetting.DATA_VALUE;
                        break;
                    case "4":
                        _dataSet4 = testDataSetting.DATA_VALUE;
                        break;
                    case "5":
                        _dataSet5 = testDataSetting.DATA_VALUE;
                        break;
                    case "6":
                        _dataSet6 = testDataSetting.DATA_VALUE;
                        break;
                    case "7":
                        _dataSet7 = testDataSetting.DATA_VALUE;
                        break;
                    case "8":
                        _dataSet8 = testDataSetting.DATA_VALUE;
                        break;
                    case "9":
                        _dataSet9 = testDataSetting.DATA_VALUE;
                        break;
                    case "10":
                        _dataSet10 = testDataSetting.DATA_VALUE;
                        break;
                    case "11":
                        _dataSet11 = testDataSetting.DATA_VALUE;
                        break;
                    case "12":
                        _dataSet12 = testDataSetting.DATA_VALUE;
                        break;
                    case "13":
                        _dataSet13 = testDataSetting.DATA_VALUE;
                        break;
                    case "14":
                        _dataSet14 = testDataSetting.DATA_VALUE;
                        break;
                    case "15":
                        _dataSet15 = testDataSetting.DATA_VALUE;
                        break;
                    case "16":
                        _dataSet16 = testDataSetting.DATA_VALUE;
                        break;
                    case "17":
                        _dataSet17 = testDataSetting.DATA_VALUE;
                        break;
                    case "18":
                        _dataSet18 = testDataSetting.DATA_VALUE;
                        break;
                    case "19":
                        _dataSet19 = testDataSetting.DATA_VALUE;
                        break;
                    case "20":
                        _dataSet20 = testDataSetting.DATA_VALUE;
                        break;
                }
            }
        }

        public void SetDataSetColumns(List<B_SHARED_OBJECT_POOL> sharedObjectPoolList)
        {
            foreach (var sharedObjectPool in sharedObjectPoolList)
            {
                if (sharedObjectPool == null) continue;
                switch (sharedObjectPool.LOOP_ID.ToString())
                {
                    case "1":
                        DataSet1 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "2":
                        DataSet2 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "3":
                        DataSet3 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "4":
                        DataSet4 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "5":
                        DataSet5 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "6":
                        DataSet6 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "7":
                        DataSet7 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "8":
                        DataSet8 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "9":
                        DataSet9 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "10":
                        DataSet10 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "11":
                        DataSet11 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "12":
                        DataSet12 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "13":
                        DataSet13 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "14":
                        DataSet14 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "15":
                        DataSet15 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "16":
                        DataSet16 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "17":
                        DataSet17 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "18":
                        DataSet18 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "19":
                        DataSet19 = sharedObjectPool.DATA_VALUE;
                        break;
                    case "20":
                        DataSet20 = sharedObjectPool.DATA_VALUE;
                        break;

                }
            }
        }

        private int _StepDataDirection = 0;
        public int StepDataDirection
        {
            get
            {
                return _StepDataDirection;
            }
            set
            {
                _StepDataDirection = value;
                OnPropertyChanged("StepDataDirection");
            }
        }

       

        private bool isSkipForDataSetInitValue;
        private bool isSkipForDataset;

        public bool IsSkipForDataset
        {
            get
            {
                return isSkipForDataset;
            }
            set
            {
                isSkipForDataset = value;
                OnPropertyChanged("IsSkipForDataset");
            }
        }
        public bool IsSkipValueChanged()
        {
            return !(isSkipForDataset==isSkipForDataSetInitValue) ;
        }

        string _dataSetDataType;
        public string DataSetDataType
        {
            get { return _dataSetDataType; }
            set
            {
                _dataSetDataType = value;
                OnPropertyChanged("DataSetDataType");
            }
        }

        bool _isRunChecked = false;
        public bool IsRunChecked
        {
            get { return _isRunChecked; }
            set
            {
                _isRunChecked = value;
                OnPropertyChanged("IsRunChecked");
            }
        }
        private long _stepNo;
        public long StepNo
        {
            get { return _stepNo; }
            set
            {
                _stepNo = value;
                OnPropertyChanged("StepNo");
            }
        }

        private long _runOrder;
        public long RunOrder
        {
            get { return _runOrder; }
            set
            {
                _runOrder = value;
                OnPropertyChanged("RunOrder");
            }
        }

        public String ToolTipString
        {
            get
            {
                if (SelectedObjectName != null)
                    return
                        "Object ID:" + SelectedObjectName.Id;
                else
                    return " ";
            }

        }

        private string _rowColumn;
        public string RowColumn
        {
            get { return _rowColumn; }
            set
            {
                _rowColumn = value;
                OnPropertyChanged("RowColumn");
            }
        }

        private string _rowColumnValue;
        public string RowColumnValue
        {
            get { return _rowColumnValue; }
            set
            {
                _rowColumnValue = value;
                OnPropertyChanged("RowColumnValue");
            }
        }

        private string _comment;
        public string Comment
        {
            get { return _comment; }
            set
            {
                _comment = value;
                OnPropertyChanged("Comment");
            }
        }

        private string _testCaseName;
        public string TestCaseName
        {
            get { return _testCaseName; }
            set
            {
                _testCaseName = value;
            }
        }

        private long _testCaseId;
        public long TestCaseId
        {
            get { return _testCaseId; }
            set
            {
                _testCaseId = value;
            }
        }
#if v_16AndUp
        private long? _ObjectNameId;
        public long? ObjectNameId
        {
            get { return _ObjectNameId; }
            set { _ObjectNameId = value; }
        }
#endif

        private List<string> _enumTypeDataSource;
        public List<string> EnumTypeDataSource
        {
            get { return _enumTypeDataSource; }
            set
            {
                _enumTypeDataSource = value;
                OnPropertyChanged("EnumTypeDataSource");
            }
        }

        private object contentValue;
        public object ContentValue
        {
            get { return contentValue; }
            set
            {
                contentValue = value;
                OnPropertyChanged("ContentValue");
            }
        }

       
        private object _dataSet1;
        public object DataSet1
        {
            get
            {
                return _dataSet1;
            }
            set
            {

                _dataSet1 = value;
                //Console.WriteLine("set Ds1=" + _dataSet1);
                OnPropertyChanged("DataSet1");
            }
        }
#region unused datasets
        private object _dataSet2;
        public object DataSet2
        {
            get { return _dataSet2; }
            set
            {
                _dataSet2 = value;
                OnPropertyChanged("DataSet2");
            }
        }

        private object _dataSet3;
        public object DataSet3
        {
            get { return _dataSet3; }
            set
            {
                _dataSet3 = value;
                OnPropertyChanged("DataSet3");
            }
        }

        private object _dataSet4;
        public object DataSet4
        {
            get { return _dataSet4; }
            set
            {
                _dataSet4 = value;
                OnPropertyChanged("DataSet4");
            }
        }


        private object _dataSet5;
        public object DataSet5
        {
            get { return _dataSet5; }
            set
            {
                _dataSet5 = value;
                OnPropertyChanged("DataSet5");
            }
        }
        private object _dataSet6;
        public object DataSet6
        {
            get { return _dataSet6; }
            set
            {
                _dataSet6 = value;
                OnPropertyChanged("DataSet6");
            }
        }

        private object _dataSet7;
        public object DataSet7
        {
            get { return _dataSet7; }
            set
            {
                _dataSet7 = value;
                OnPropertyChanged("DataSet7");
            }
        }

        private object _dataSet8;
        public object DataSet8
        {
            get { return _dataSet8; }
            set
            {
                _dataSet8 = value;
                OnPropertyChanged("DataSet8");
            }
        }

        private object _dataSet9;
        public object DataSet9
        {
            get { return _dataSet9; }
            set
            {
                _dataSet9 = value;
                OnPropertyChanged("DataSet9");
            }
        }

        private object _dataSet10;
        public object DataSet10
        {
            get { return _dataSet10; }
            set
            {
                _dataSet10 = value;
                OnPropertyChanged("DataSet10");
            }
        }

        private object _dataSet11;
        public object DataSet11
        {
            get { return _dataSet11; }
            set
            {
                _dataSet11 = value;
                OnPropertyChanged("DataSet11");
            }
        }

        private object _dataSet12;
        public object DataSet12
        {
            get { return _dataSet12; }
            set
            {
                _dataSet12 = value;
                OnPropertyChanged("DataSet12");
            }
        }

        private object _dataSet13;
        public object DataSet13
        {
            get { return _dataSet13; }
            set
            {
                _dataSet13 = value;
                OnPropertyChanged("DataSet13");
            }
        }

        private object _dataSet14;
        public object DataSet14
        {
            get { return _dataSet14; }
            set
            {
                _dataSet14 = value;
                OnPropertyChanged("DataSet14");
            }
        }

        private object _dataSet15;
        public object DataSet15
        {
            get { return _dataSet15; }
            set
            {
                _dataSet15 = value;
                OnPropertyChanged("DataSet15");
            }
        }

        private object _dataSet16;
        public object DataSet16
        {
            get { return _dataSet16; }
            set
            {
                _dataSet16 = value;
                OnPropertyChanged("DataSet16");
            }
        }

        private object _dataSet17;
        public object DataSet17
        {
            get { return _dataSet17; }
            set
            {
                _dataSet17 = value;
                OnPropertyChanged("DataSet17");
            }
        }

        private object _dataSet18;
        public object DataSet18
        {
            get { return _dataSet18; }
            set
            {
                _dataSet18 = value;
                OnPropertyChanged("DataSet18");
            }
        }

        private object _dataSet19;
        public object DataSet19
        {
            get { return _dataSet19; }
            set
            {
                _dataSet19 = value;
                OnPropertyChanged("DataSet19");
            }
        }

        private object _dataSet20;
        public object DataSet20
        {
            get { return _dataSet20; }
            set
            {
                _dataSet20 = value;
                OnPropertyChanged("DataSet20");
            }
        }
#endregion//unused datasets

        private Keyword _selectedKeyword;
        public Keyword SelectedKeyword
        {
            get { return _selectedKeyword; }
            set
            {
                //Logger.logBegin("SelectedKeyword", string.Format("value:[{0}]",value));
                if (value == null) return;
                _selectedKeyword = value;
                OnPropertyChanged("SelectedKeyword");
                if (_selectedKeyword != null && staticKeywordNames.Contains(_selectedKeyword.KeywordName))
                {

                    PopulateObjectsByKeyword(_selectedKeyword.Id, _selectedKeyword.TestCaseId);
                }
                else
                {
                    var testCases = testCaseColl;
                    if (testCases != null)
                    {
                        //  AF IEnumerable<TestCaseEditViewModel> filterVm = testCases.Where(a => a.SelectedKeyword.KeywordName == "PegWindow" && a.RunOrder < this.RunOrder);
                        IEnumerable<TestStepViewModel> filterVm = testCases.Where(a =>
                            a != null &&
                            a.SelectedKeyword != null &&
                            a.SelectedKeyword.KeywordName != null &&
                            a.SelectedKeyword.KeywordName == "PegWindow" &&
                            a.RunOrder > 0 &&
                            this.RunOrder > 0 &&
                            a.RunOrder < this.RunOrder);
                        if (filterVm.ToList().Count != 0)
                        {
                            var vm = filterVm.LastOrDefault().SelectedObjectName;
                            if (vm != null)
                            {
                                string objectType = filterVm.LastOrDefault().SelectedObjectName.ObjectType;
                                // AF added next line
                                if (_selectedKeyword != null)
                                    Objects = PopulateObjectsByObjectTypeAndKeyword(objectType, _selectedKeyword.TestCaseId, _selectedKeyword);
                            }
                        }
                    }
                }
                //Logger.logEnd("SelectedKeyword");
            }
        }

        private ObjectName _selectedObjectName;
        public ObjectName SelectedObjectName
        {
            get { return _selectedObjectName; }
            set
            {
                if (value != null)
                {
                    _selectedObjectName = value;
                    OnPropertyChanged("SelectedObjectName");
                    RePopulateRowOnObjectChange(_selectedObjectName);
                }

                if (SelectedKeyword!=null)
                {
                    if (string.Compare("Pegwindow", SelectedKeyword.KeywordName,true)==0)
                    {
                        //get all  objects belong to this pegwindow
                        //SubObjects 
                    }
                }
            }
        }

        public void ForceSetSelectObjectNameNull()
        {
            _selectedObjectName = null;
            OnPropertyChanged("SelectedObjectName");
            RePopulateRowOnObjectChange(_selectedObjectName);
        }

        private void RePopulateRowOnObjectChange(ObjectName selectedObjectName)
        {
            _dataSetDataType = "STRING";
            if (selectedObjectName != null && selectedObjectName.EnumType != null)
            {
                B_REGISTED_OBJECT regObj = new B_REGISTED_OBJECT();
                List<string> enumTypeValue = regObj.GetTestEnumValues(MarsMainWindow.CurrentDatabaseIdx, selectedObjectName.EnumType);

                if (enumTypeValue.Count > 1)
                {
                    _dataSetDataType = "ComboBox";
                    EnumTypeDataSource = enumTypeValue;
                }
                else
                {
                    if (selectedObjectName.EnumType.CompareTo("Date") == 0)
                        _dataSetDataType = "DateTime";
                }
            }
        }

        public bool DeleteTestCase(string strTestCaseToBeDeleted, long lTestId = -1, bool isDeleteTestRuntimeData = false)
        {
            B_TEST_CASE boTestCase = new B_TEST_CASE();
            string strError = "";
            if (!boTestCase.DeleteTestCaseById(MarsMainWindow.CurrentDatabaseIdx, lTestId, ref strError))
            {
                Logger.Error("DeleteTestCase", strError = string.Format("Error when Delete Testcase:[{1}]\r\nError:[{0}]", strError, strTestCaseToBeDeleted));
                ViewModelBase.HintByMessageBox(strError, "Hint");
                return false;
            }
            ViewModelBase.HintByMessageBox(string.Format("Test case:[{0}] is deleted.", strTestCaseToBeDeleted), "Hint");
            return true;
#region old codes, need to be moved to data layer
            //MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            //var testCase = (from t in marsEntities.T_TEST_CASE_SUMMARY
            //                where t.TEST_CASE_NAME == testCaseName
            //                select t).FirstOrDefault();


            //var testCaseSteps = (from s in marsEntities.T_TEST_STEPS
            //                         where s.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                         select s);

            //// delete report data begin

            //var testCaseStepsIds = (from ts in testCaseSteps select ts.STEPS_ID).ToList();

            //// T_TEST_REPORT_STEPS
            //var testReportSteps = (from trs in marsEntities.T_TEST_REPORT_STEPS
            //                      where (testCaseStepsIds.Contains((long)trs.STEPS_ID))  
            //                      select trs).ToList();

            //foreach (var tsr in testReportSteps)
            //{
            //    marsEntities.T_TEST_REPORT_STEPS.Remove(tsr);
            //}

            //// T_TEST_REPORT
            //var testReports = (from tr in marsEntities.T_TEST_REPORT
            //                       where tr.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                       select tr);

            //foreach (var tr in testReports)
            //{
            //    marsEntities.T_TEST_REPORT.Remove(tr);
            //}

            //// T_PROJ_TEST_RESULT
            //var testReportResults = (from trr in marsEntities.T_PROJ_TEST_RESULT
            //                   where trr.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                   select trr);

            //foreach (var trr in testReportResults)
            //{
            //    marsEntities.T_PROJ_TEST_RESULT.Remove(trr);
            //}

            //// delete report data end

            //// data settings and object pool data

            //SharedDataSetViewModel.Delete(testCaseStepsIds, false);

            //// remove test steps
            //foreach (var s in testCaseSteps)
            //{
            //     marsEntities.T_TEST_STEPS.Remove(s);
            //}

            //var relTestCaseTestSuite = (from r in marsEntities.REL_TEST_CASE_TEST_SUITE
            //                            where r.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                            select r);

            //foreach (var r in relTestCaseTestSuite)
            //{
            //    marsEntities.REL_TEST_CASE_TEST_SUITE.Remove(r);
            //}


            //var allDataSummaryIds = (from ds in marsEntities.REL_TC_DATA_SUMMARY
            //                         where ds.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                         select ds.DATA_SUMMARY_ID).Distinct().ToList();

            //var allDataSummary = (from ds in marsEntities.T_TEST_DATA_SUMMARY
            //                      join id in allDataSummaryIds on ds.DATA_SUMMARY_ID equals id
            //                      select ds).ToList(); ;

            //// Find all rows for REL_TC_DATA_SUMMARY
            //// SEEMS TO BE WRONG -- rewritten 
            ///*
            //var relTcDataSummary = (from rt in marsEntities.REL_TC_DATA_SUMMARY
            //                        join id in allDataSummaryIds on rt.DATA_SUMMARY_ID equals id
            //                        select rt).ToList();
            //*/

            // var relTcDataSummary = (from rt in marsEntities.REL_TC_DATA_SUMMARY 
            //                         where rt.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                         select rt).ToList();



            //// Delete DATA_SUMMARY rows

            //// AF NOT DELETING SHARED_POOL AND TEST_DATA_SUMMARY  preserve data even when TC is deleted
            ///*
            //foreach (var ds in allDataSummary)
            //{
            //    if (ds != null)
            //    {
            //        int dsCount =  (from rt in marsEntities.REL_TC_DATA_SUMMARY
            //                        where rt.DATA_SUMMARY_ID == ds.DATA_SUMMARY_ID
            //                        select rt).Count();

            //        if (dsCount == 1)
            //            marsEntities.T_TEST_DATA_SUMMARY.Remove(ds);
            //    }
            //}
            // */ 

            //// Delete from REL_TC_DATA_SUMMARY
            //foreach (var rt in relTcDataSummary)
            //{
            //    marsEntities.REL_TC_DATA_SUMMARY.Remove(rt);
            //}

            //// Delete from Storyboards

            //var storyboardIds = (from s in marsEntities.T_PROJ_TC_MGR
            //                     where s.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                     select s.STORYBOARD_DETAIL_ID).ToList();

            //foreach (var storyboard in storyboardIds)
            //{
            //    BoHelper.DeleteStoryboard((long)storyboard);
            //}

            //// Delete from REL_APP_TESTCASE
            //var relAppTestCase = (from a in marsEntities.REL_APP_TESTCASE
            //                      where a.TEST_CASE_ID == testCase.TEST_CASE_ID
            //                      select a);

            //foreach (var a in relAppTestCase)
            //{
            //    marsEntities.REL_APP_TESTCASE.Remove(a);
            //}

            //marsEntities.T_TEST_CASE_SUMMARY.Remove(testCase);

            //    try
            //    {
            //        if (marsEntities.SaveChanges() > 0)
            //        {
            //            MarsTreeView.GetMarsTree();
            //            /// AF
            //            if (VMCollCash.cache.ContainsKey(testCaseName))
            //            {
            //                VMCollCash.cache.Remove(testCaseName);
            //            }
            //            ///

            //            System.Windows.MessageBox.Show("Test Case deleted successfully", "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Information);
            //            return true;
            //        }
            //        else
            //        {
            //            marsEntities = null;
            //            System.Windows.MessageBox.Show("Error deleting Test Case", "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            //            return false;
            //        }

            //    }


            //catch (Exception ex)
            //{
            //    marsEntities = null;
            //    System.Windows.MessageBox.Show(ex.InnerException.ToString(), "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return false;
            //}
#endregion // old codes, need to be moved to data layer
        }

        public bool DeleteTestCaseSql(string testCaseName)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            DeleteTestCaseHelper helper = new DeleteTestCaseHelper(marsEntities);

            var testCase = (from t in marsEntities.T_TEST_CASE_SUMMARY
                            where t.TEST_CASE_NAME == testCaseName
                            select t).FirstOrDefault();
            /*
            DeleteResultData(testCase.TEST_CASE_ID);

            // this needs to be done because the previous method accesses DB directly and invalidates marsEntities
            marsEntities = BoHelper.GetMarsEntitiesInstance(true);
            testCase = (from t in marsEntities.T_TEST_CASE_SUMMARY
                        where t.TEST_CASE_NAME == testCaseName
                        select t).FirstOrDefault();
            */


            var relAppTestCase = (from a in marsEntities.REL_APP_TESTCASE
                                  where a.TEST_CASE_ID == testCase.TEST_CASE_ID
                                  select a);

            foreach (var a in relAppTestCase)
            {
                //marsEntities.REL_APP_TESTCASE.Remove(a);
                helper.RelAppTestCaseList.Add(a.RELATIONSHIP_ID);
            }

            var relTestCaseTestSuite = (from r in marsEntities.REL_TEST_CASE_TEST_SUITE
                                        where r.TEST_CASE_ID == testCase.TEST_CASE_ID
                                        select r);

            foreach (var r in relTestCaseTestSuite)
            {
                // marsEntities.REL_TEST_CASE_TEST_SUITE.Remove(r);
                helper.RelTestCaseTestSuiteList.Add(r.RELATIONSHIP_ID);
            }

            var testStepsTestCase = (from s in marsEntities.T_TEST_STEPS
                                     where s.TEST_CASE_ID == testCase.TEST_CASE_ID
                                     select s);

            // create list of T_TEST_DATA_SUMMARY related to data in this test case
            // first find all TEST_DATA_SETTINGS
            var allDataSummaryIds = (from ds in marsEntities.TEST_DATA_SETTING
                                     join ts in testStepsTestCase on ds.STEPS_ID equals ts.STEPS_ID
                                     select ds.DATA_SUMMARY_ID).Distinct().ToList();

            var allDataSummary = (from ds in marsEntities.T_TEST_DATA_SUMMARY
                                  join id in allDataSummaryIds on ds.DATA_SUMMARY_ID equals id
                                  select ds).ToList(); ;

            // Delete DATA_SUMMARY rows
            foreach (var ds in allDataSummary)
            {
                if (ds != null)
                {
                    //marsEntities.T_TEST_DATA_SUMMARY.Remove(ds);
                    helper.TestDataSummary.Add(ds.DATA_SUMMARY_ID);
                }
            }

            foreach (var s in testStepsTestCase)
            {
                //RemoveDataSettngs(s.STEPS_ID, marsEntities);
                helper.TestDataSettingsList.Add(s.STEPS_ID);
                //marsEntities.T_TEST_STEPS.Remove(s);
                helper.TestCaseStepsList.Add(s.STEPS_ID);

            }

            // Delete from REL_TC_DATA_SUMMARY
            var relTcDataSummary = (from rt in marsEntities.REL_TC_DATA_SUMMARY
                                    join id in allDataSummaryIds on rt.DATA_SUMMARY_ID equals id
                                    select rt).ToList();

            foreach (var rt in relTcDataSummary)
            {
                //marsEntities.REL_TC_DATA_SUMMARY.Remove(rt);
                helper.RelTcDataSummaryList.Add((long)rt.DATA_SUMMARY_ID);
            }

            // Delete from Storyboards

            var storyboardIds = (from s in marsEntities.T_PROJ_TC_MGR
                                 where s.TEST_CASE_ID == testCase.TEST_CASE_ID
                                 select s.STORYBOARD_DETAIL_ID).ToList();

            foreach (var storyboard in storyboardIds)
            {
                //BoHelper.DeleteStoryboard((long)storyboard);
                helper.StoryboardList.Add(storyboard);
            }

            //marsEntities.T_TEST_CASE_SUMMARY.Remove(testCase);
            helper.TestCaseList.Add(testCase.TEST_CASE_ID);

            try
            {
                DeleteResultData(testCase.TEST_CASE_ID);

                try
                {
                    if (helper.ApplyDeletions() > 0)
                    {
                        MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                        /// AF
                        if (VMCollCash.cache.ContainsKey(testCaseName))
                        {
                            VMCollCash.cache.Remove(testCaseName);
                        }
                        ///

                        System.Windows.MessageBox.Show("Test Case deleted successfully", "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Information);
                        return true;
                    }
                    else
                    {
                        marsEntities = null;
                        System.Windows.MessageBox.Show("Error deleting Test Case", "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                }
                catch (Exception)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                marsEntities = null;
                System.Windows.MessageBox.Show(ex.InnerException.ToString(), "Test Case Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        private void DeleteResultData(long testCaseId)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);

            var storyboardIds = (from s in marsEntities.T_PROJ_TC_MGR
                                 where s.TEST_CASE_ID == testCaseId
                                 select s.STORYBOARD_DETAIL_ID).ToList();

            // delete result data related to this storyboard
            string refError = "";
            bool? isDelBaseline = true;
            bool? isDelNoneBaseline = true;
            foreach (var storyboard in storyboardIds)
            {
                BoHelper.DeleteHistDataByStoryBoardDetailId(MarsMainWindow.CurrentDatabaseIdx, (long)storyboard, isDelBaseline, isDelNoneBaseline, ref refError);
            }

        }


        private void RemoveDataSettngs(long stepID)
        {
            BoHelper.DeleteDataSettings(MarsMainWindow.CurrentDatabaseIdx, stepID);
        }


        internal void DeleteData()
        {
            Comment = null;
            RowColumn = null;
            DataSet1 = null;
            DataSet2 = null;
            DataSet3 = null;
            DataSet4 = null;
            DataSet5 = null;
            DataSet6 = null;
            DataSet7 = null;
            DataSet8 = null;
            DataSet9 = null;
            DataSet10 = null;
            DataSet11 = null;
            DataSet12 = null;
            DataSet13 = null;
            DataSet14 = null;
            DataSet15 = null;
            DataSet16 = null;
            DataSet17 = null;
            DataSet18 = null;
            DataSet19 = null;
            DataSet20 = null;
        }

        internal void addObjectInfo(TestStepViewModel prevRowViewmodel, long testcaseId)
        {
            //_testCaseName = testCaseName;
            _testCaseId = testcaseId;
            if (prevRowViewmodel.SelectedKeyword.KeywordName == "PegWindow")
            {
                Keywords = prevRowViewmodel.Keywords;
                // Objects = prevRowViewmodel.Objects;
            }
            //SelectedKeyword = Keywords.Where(a => a.Id == prevRowViewmodel.SelectedKeyword.Id).FirstOrDefault();
            //SelectedObjectName = Objects.Where(a => a.Id == prevRowViewmodel.SelectedObjectName.Id).FirstOrDefault();

            SelectedKeyword = prevRowViewmodel.SelectedKeyword;
            PopulateObjectsByKeyword(SelectedKeyword.Id, _testCaseId);
            SelectedObjectName = Objects.Where(a => a.Id == prevRowViewmodel.SelectedObjectName.Id).FirstOrDefault();
            //SelectedObjectName = prevRowViewmodel.SelectedObjectName;      
        }

        internal void DeleteRelatedDataSettings()
        {
            DeleteData();

        }

        private static List<Keyword> defaultStaticKeywords = null;
        private static List<Keyword> defaultOtherKeywords = null;
        private static bool PopulateDefaultStaticKeywords(ref string strError)
        {
            Logger.logBegin("PopulateDefaultStaticKeywords");
            defaultStaticKeywords = new List<Keyword>();
            try
            {
                B_KEYWORD objKwrd = new B_KEYWORD();
                Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>  dictKeyword= objKwrd.GetKeyWordNames(staticKeywordNames);
                foreach(T_KEYWORDDTO k in dictKeyword.Keys)
                {
                    Keyword oneK = new Keyword(k, dictKeyword[k], null, -1);
                    defaultStaticKeywords.Add(oneK);
                }
                defaultStaticKeywords = defaultStaticKeywords.OrderBy(p => p.KeywordName).ToList();
                return true;
            }
            catch(Exception e)
            {
                Logger.Error("PopulateDefaultStaticKeywords", strError=string.Format("Exception:[{0}] Stacktrace:\r\n{1}",e.Message));
                return false;
            }
        }
        private static bool PopulateAllOtherKeywords(ref string strError)
        {
            Logger.logBegin("PopulateAllOtherKeywords");
            try
            {
                B_KEYWORD objKwrd = new B_KEYWORD();
                Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> dictKeyword = objKwrd.GetAllKeywordsFromCache();
                defaultOtherKeywords = new List<Keyword>();
                foreach (T_KEYWORDDTO k in dictKeyword.Keys)
                {
                    Keyword oneK = new Keyword(k, dictKeyword[k], null, -1);
                    defaultOtherKeywords.Add(oneK);
                }
                List<long> lstStatic = defaultStaticKeywords == null ? (new List<long>()) : defaultStaticKeywords.Select(p=>p.Id).ToList();

                defaultOtherKeywords = defaultOtherKeywords.Except(defaultOtherKeywords.Where(p => lstStatic.Contains(p.Id))).ToList();
                defaultOtherKeywords.Sort((a,b)=> {
                    if ((a == null) && (b != null)) return -1;
                    if ((a != null) && (b == null)) return 1;
                    if ((a == null) && (b == null)) return 0;
                    return a.KeywordName.CompareTo(b.KeywordName);
                });
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("PopulateAllOtherKeywords",string.Format("Excepton:[{0}]",e.Message),e);
                defaultOtherKeywords = null;
                return false;
            }
        }
        /// <summary>
        /// 依据 keyword（可以为空）获得keywords的列表。如果strKeywordName!=null那么设置selectedKeyword
        /// </summary>
        /// <param name="strKeyWordName"></param>
        /// <param name="dicPegWithItsSubs"></param>
        /// <param name="strPegHappyName"></param>
        internal void BuildKeywordsListBySelectedKeyword(string strKeyWordName, Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> dicPegWithItsSubs, string strPegHappyName)
        {
            string strError = "";
            bool isOk = true;
            if (defaultStaticKeywords == null)
                isOk = PopulateDefaultStaticKeywords(ref strError);
            if (!isOk) return;
            if (defaultOtherKeywords == null)
                isOk = PopulateAllOtherKeywords(ref strError);
            if (!isOk) return;
            List<Keyword> lstTmp = new List<Keyword>();
                defaultStaticKeywords.ForEach(k=> {
                    Keyword objK = new Keyword(k.Id, k.KeywordName, this._testCaseName);
                    lstTmp.Add(objK);
                });
            ///除去第一行外，所有的keyword中应该包括所有的keywor信息
            if (RunOrder>1)
                defaultOtherKeywords.ForEach(k =>
                {
                    Keyword objK = new Keyword(k.Id, k.KeywordName, this._testCaseName);
                    lstTmp.Add(objK);
                });
            ///依据相关对象过滤
            /// 
            List<long?> lstType=null,lstTypeFilter=new List<long?>();            
            if (string.IsNullOrEmpty(strPegHappyName)&& (dicPegWithItsSubs!= null))
            {
                ///获得type
                /// 
                List<B_REGISTED_OBJECT> lstPeg;
                
                if (_applicationIds != null)
                    lstPeg = dicPegWithItsSubs.Keys.Where(p => (string.Compare(p.OBJECT_HAPPY_NAME, strPegHappyName, true) == 0)
                    && _applicationIds.Contains(p.APPLICATION_ID ?? -1)).ToList();
                else
                    lstPeg = dicPegWithItsSubs.Keys.Where(p => (string.Compare(p.OBJECT_HAPPY_NAME, strPegHappyName, true) == 0)).ToList();
                if (lstPeg!=null)
                {
                    lstPeg.ForEach(p=> {
                        if (dicPegWithItsSubs[p]!= null)
                        {
                            lstType = dicPegWithItsSubs[p].Select(z=>z.TYPE_ID).Distinct().ToList();
                            if ((lstType != null) && (lstType.Count > 0))
                                lstTypeFilter.AddRange(lstType);
                        }
                    });
                    ///过滤
                    /// 
                    lstTmp = lstTmp.Where(p => (
                        ((p.AssignedKeywordDicList != null)
                        && (p.AssignedKeywordDicList.Any(a => lstTypeFilter.Contains(a.TYPE_ID)))))
                        || p.AssignedKeywordDicList == null
                        )
                        .ToList();
                }
            }

            Keywords = new ObservableCollection<Keyword>(lstTmp);
            if (string.IsNullOrEmpty(strKeyWordName))
            {
                ///因为没有预设需要的keyword，因此，返回所有的，包括static的
                ///                 
                SelectedKeyword = null;
                return;
            }
            if (_keywords != null)
            {
                SelectedKeyword = _keywords.Where(p => string.Compare(p._keywordName, strKeyWordName, true) == 0).FirstOrDefault();
            }

        }

        internal void BuildObjectsListBySelectedObjName(string strTargetObjectName, string strPegName, Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> dictionaryObjectList)
        {
            if ((string.IsNullOrEmpty(strPegName)) || (dictionaryObjectList == null))
            {
                Logger.Error("BuildObjectsListBySelectedObjName","No Pegwindow infomation or object list is null");
                return;
            }
            List<ObjectName> lstTmp = new List<ObjectName>();
            List<B_REGISTED_OBJECT> lstPegs = dictionaryObjectList.Keys.Where(p => string.Compare(strPegName, p.OBJECT_HAPPY_NAME, true) == 0).ToList();
            lstPegs.ForEach(itm => {
                if (dictionaryObjectList[itm]!= null)
                {
                    dictionaryObjectList[itm].ForEach(itmSub => {
                        if(itmSub!=null)
                        {
                            if (!lstTmp.Any(p => string.Compare(p.ObjName, itmSub.OBJECT_HAPPY_NAME, true) == 0))
                            {
                                lstTmp.Add(new ObjectName(itmSub));
                            }
                        }
                    });
                }             

            });
            Objects = new ObservableCollection<ObjectName>(lstTmp);
            if (string.IsNullOrEmpty(strTargetObjectName)) return;
            SelectedObjectName = _objects.Where(p => string.Compare(p.ObjName, strTargetObjectName, true) == 0).FirstOrDefault();
        }

        internal bool convert2TestStepsvc(TestStep4Services objTestStep,string strPegUrl, ref string strError)
        {
            Logger.logBegin("convert2TestStepsvc");
            if (this._selectedKeyword==null)
            {
                Logger.Error("convert2TestStepsvc",strError = "No selected Keyword, please select keyword first.");
                return false;
            }
            if (objTestStep == null)
            {
                Logger.Error("convert2TestStepsvc", strError = "Source object is null");
                return false;
            }
            objTestStep.AssignedTestStepId = this._stepNo;
            objTestStep.Comment = this._comment;
            objTestStep.Keyword = this._selectedKeyword.KeywordName;
            objTestStep.Loop = 1;
            objTestStep.ObjectName = this._selectedObjectName == null ? "" : this._selectedObjectName.ObjName;
            objTestStep.ParentAttachInfo = strPegUrl;
            /// 获得对象的type name如swfEdit，javaedit等
            /// 
            bool isOk = false;
            objTestStep.QuickAccess = GetCurrentObjectQuickURL(ref isOk, ref strError);
            objTestStep.Row_Column = this._rowColumn;
            objTestStep.Value = this.DataSet1 == null ? "" : this.DataSet1.ToString();
            return true;
        }
        public string GetCurrentObjectQuickURL(ref bool isOk, ref string strError)
        {
            ///算法
            /// 1, 从cache中获得type name
            /// 2, 组织quickAccess URL
            /// 3，装配
            /// 
            if (this._selectedObjectName==null)
            {
                Logger.Info("GetCurrentObjectQuickURL", "Selected object is NULL.");
                isOk = true;
                return "";
            }
            string strURL = MarsTestFrame.systemUtil.TigerMarsUtil.ConvertQuickAccess2CommaMode(this._selectedObjectName.QuickAccess) ;
            string strTypeName = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeById(MarsMainWindow.CurrentDatabaseIdx, 
                this._selectedObjectName.AssignedDto.TYPE_ID,ref isOk, ref strError);
            if (!isOk) return "";
            
            if ((!string.IsNullOrEmpty(strTypeName))&&(string.Compare("Pegwindow", strTypeName,true)==0))
            {
                strTypeName = "Window";
            }
            return string.Format("{0}({1})",strTypeName, strURL);
        }

        internal void SearchText(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                //clearn row color
                CurrentRowColor = _CurrentRowDefaultColor;
                return;
            }
           
            if (SelectedKeyword!=null)
            {
                if (TigerMarsUtil.RegularTest(searchText,SelectedKeyword.KeywordName))
                {
                    CurrentRowColor = RowColorMgr.SearchHitBrush;
                    return;
                }
            }
            if ((SelectedObjectName!=null)&&(TigerMarsUtil.RegularTest(searchText, SelectedObjectName.ObjName)))
            {
                CurrentRowColor = RowColorMgr.SearchHitBrush;
                return;
            }
            if ((!string.IsNullOrEmpty(RowColumn)) && (TigerMarsUtil.RegularTest(searchText, RowColumn)))
            {
                CurrentRowColor = RowColorMgr.SearchHitBrush;
                return;
            }
            if ((!string.IsNullOrEmpty(DataSet1==null?null:DataSet1.ToString())) && (TigerMarsUtil.RegularTest(searchText, DataSet1 == null ? "" : DataSet1.ToString())))
            {
                CurrentRowColor = RowColorMgr.SearchHitBrush;
                return;
            }
            if ((!string.IsNullOrEmpty(Comment)) && (TigerMarsUtil.RegularTest(searchText, Comment)))
            {
                CurrentRowColor = RowColorMgr.SearchHitBrush;
                return;
            }

            if (CurrentRowColor != _CurrentRowDefaultColor)
                CurrentRowColor = _CurrentRowDefaultColor;
        }
    }

    public class Keyword : ViewModelBase
    {
        public Keyword()
        {
        }

        public Keyword(T_KEYWORDDTO objKeyWordInfo, List<T_DIC_RELATION_KEYWORDDTO> lstKeyTypDicInfo, string strTestcaseName, long lTestcaseId)
        {
            this.AssignedKeywordDto = objKeyWordInfo;
            this.AssignedKeywordDicList = lstKeyTypDicInfo;

            _id = objKeyWordInfo.KEY_WORD_ID;
            _keywordName = objKeyWordInfo.KEY_WORD_NAME;

            TestCaseName = strTestcaseName;
            this.testCaseId = lTestcaseId;
        }

        public Keyword(long keywordId, string keywordName, string testCaseName, long testcaseId = -1)
        {
            _id = keywordId;
            _keywordName = keywordName;
            TestCaseName = testCaseName;
            this.testCaseId = testcaseId;
        }

        public long _id;
        public long Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                OnPropertyChanged("Id");
            }
        }

        public string _testCaseName;
        public string TestCaseName
        {
            get
            {
                return _testCaseName;
            }
            set
            {
                _testCaseName = value;
            }
        }

        private long testCaseId;
        public long TestCaseId
        {
            get { return testCaseId; }
            set
            {
                testCaseId = value;
                OnPropertyChanged("TestCaseId");
            }
        }

        public string _keywordName;
        public string KeywordName
        {
            get {
                return _keywordName;
            }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                _keywordName = value;
                    OnPropertyChanged("KeywordName");
            }
        }

        public T_KEYWORDDTO AssignedKeywordDto { get; set; }
        public List<T_DIC_RELATION_KEYWORDDTO> AssignedKeywordDicList { get; set; }

        public List<long> GetTypeIdList()
        {
            List<long> lstResult = new List<long>();
            if (AssignedKeywordDicList == null) return lstResult;
            foreach (var KeywordDic in AssignedKeywordDicList)
            {
                if (KeywordDic.TYPE_ID == null) continue;
                if (lstResult.Any(p => p == KeywordDic.TYPE_ID))
                    continue;
                lstResult.Add(KeywordDic.TYPE_ID ?? -1);
            }
            return lstResult;
        }
    }



    public class ObjectName : Notify
    {
        //public ObjectName()
        //{

        //}
#if v_16AndUp
        public ObjectName(B_REGISTED_OBJECT objDto)
#else
        public ObjectName(T_REGISTED_OBJECTDTO objDto)
#endif
        {
            _id = objDto.OBJECT_ID;
            _objName = objDto.OBJECT_HAPPY_NAME;
            _enumType = objDto.ENUM_TYPE;
            _objectType = objDto.OBJECT_TYPE;
            _quickAccess = objDto.QUICK_ACCESS;
            type_id = objDto.TYPE_ID;
#if v_16AndUp
            _NameId = objDto.OBJECT_NAME_ID ?? -1;
            this.assignedApplicationId = objDto.APPLICATION_ID ?? -1;
#endif

            this.AssignedDto = objDto;
        }

#if v_16AndUp
        private long _NameId;
        public long NameId
        {
            get { return _NameId; }
            set
            {
                _NameId = value;
                OnPropertyChanged("NameId");
            }
        }

        private long assignedApplicationId;
        public long AssingedApplicationId
        {
            get { return assignedApplicationId; }
            set
            {
                assignedApplicationId = value;
                OnPropertyChanged("AssingedApplicationId");
            }
        }

#endif
#if v_16AndUp

        public ObjectName(long id, string name, string enumType, string objectType, string strQuickAccess, long appId)
        {
            assignedApplicationId = appId;

#else
        public ObjectName(long id, string name, string enumType, string objectType,string strQuickAccess)
        {
#endif
            _id = id;
            _objName = name;
            _enumType = enumType;
            _objectType = objectType;
            _quickAccess = strQuickAccess;
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
        private string _objName;
        public string ObjName
        {
            get { return _objName; }
            set
            {
                _objName = value;
                OnPropertyChanged("ObjName");
            }
        }

        private string _enumType;
        public string EnumType
        {
            get { return _enumType; }
            set
            {
                _enumType = value;
                OnPropertyChanged("EnumType");
            }
        }
        private string _objectType;
        public string ObjectType
        {
            get { return _objectType; }
            set
            {
                _objectType = value;
                OnPropertyChanged("ObjectType");
            }
        }

        private string _quickAccess;
        public string QuickAccess
        {
            get { return _quickAccess; }
            set
            {
                _quickAccess = value;
                OnPropertyChanged("QuickAccess");
            }
        }

        private Nullable<Int64> type_id;
        public string ObjectSwfType
        {
            get
            {
                if (type_id == -1) return "";
                string strError = "";
                bool isOk = false;
                string strResult = "";
                strResult = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeById(MarsMainWindow.CurrentDatabaseIdx, type_id,ref isOk, ref strError);
                if (isOk) return strResult;
                return null;
            }
        }

#if v_useNameId
#region new dictionary for quickAccess
        private string objectDisplayName;
        public string ObjectDisplayName
        {
            get { return objectDisplayName; }
            set
            {
                objectDisplayName = value;
                OnPropertyChanged("ObjectDisplayName");
            }
        }

        /// <summary>
        /// as object Name is used for applications
        /// then for different application with different quickAccess info and EnumType
        /// </summary>
        /// 
        private Dictionary<long, List<ObjectName>> appDictionWithSubObjInfo = new Dictionary<long, List<ObjectName>>();
        public Dictionary<long, List<ObjectName>> AppDictionWithSubObjInfo
        {
            get { return appDictionWithSubObjInfo; }
            set
            {
                appDictionWithSubObjInfo = value;
                OnPropertyChanged("AppDictionWithSubObjInfo");
            }
        }
        private static MLogger Logger = MLogger.GetLogger(typeof(ObjectName));
        public static ObservableCollection<ObjectName> CompactList(List<ObjectName> lstSrc)
        {
            //Logger.Info("CompactList", string.Format("try to compact list:[{0}]", lstSrc == null ? -1 : lstSrc.Count));
            if (lstSrc == null) return null;
            List<ObjectName> lstResult = new List<ObjectName>();

            foreach (ObjectName objItm in lstSrc)
            {
                if (objItm == null) continue;
                // use its parent window and its name to locate an object
                ObjectName objIdx = lstResult.Find(p => (string.Compare(p.ObjName, objItm.ObjName) == 0) && (string.Compare(p.ObjectType, objItm.ObjectType) == 0));
                List<ObjectName> lstAppSubObjInfo = null;
                if (objIdx == null)
                {
                    lstResult.Add(objItm);
                    objItm.appDictionWithSubObjInfo.Add(objItm.assignedApplicationId, lstAppSubObjInfo = new List<ObjectName>());
                    lstAppSubObjInfo.Add(objItm);
                    continue;
                }

                if (objIdx.appDictionWithSubObjInfo.ContainsKey(objItm.assignedApplicationId))
                    lstAppSubObjInfo = objIdx.appDictionWithSubObjInfo[objItm.assignedApplicationId];
                else
                    objIdx.appDictionWithSubObjInfo.Add(objItm.assignedApplicationId, lstAppSubObjInfo = new List<ObjectName>());
                lstAppSubObjInfo.Add(objItm);
            }
            /***
            /// it should tell application that which object is applied for current test cases
            /// but now, it is unimplement
            List<long> lstAllAppIds = lstResult.Select(p => p.assignedApplicationId).Distinct().ToList();
            List<B_REGISTERED_APPS> lstAppInfo = BoHelper.GetAllApps(lstAllAppIds);   
            **/

            ObservableCollection<ObjectName> lstRsltObjservable = new ObservableCollection<ObjectName>(lstResult);
            //Logger.Info("CompactList", string.Format("Item Count Leaves after compact:[{0}]", lstResult.Count));

            return lstRsltObjservable;
        }
#endregion

#endif
        public T_REGISTED_OBJECTDTO AssignedDto { get; set; }
    }



    public class Notify : INotifyPropertyChanged
    {
        // Declare the event 
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
#if v_useNameId
    //public class MarsObjSubInfoForApplication
    //{
    //    public long AssignedApplicationId { get; set; }
    //    private ObjectName assignedObjectInfo;
    //    public ObjectName AssignedObjectInfo {
    //        get { return assignedObjectInfo; }
    //        set {
    //            assignedObjectInfo = value;

    //        } }

    //    private string marsObjectEnumType;
    //    public string MarsObjectEnumType
    //    {
    //        get { return marsObjectEnumType; }
    //        set { marsObjectEnumType = value; }
    //    }

    //    private string marsQuickAccess;
    //    public string MarsQuickAccess
    //    {
    //        get { return marsQuickAccess; }
    //        set { marsQuickAccess = value; }
    //    }
    //}
#endif
    public class MarsClipBoard
    {
        public static IList<TestStepViewModel> testCasesClipBoard;
        public static String pegWindow { get; set; }

        public static TestStepViewModel pegWindowVM { get; set; }

        public static string appNames;

        public static string DataTable2Cvs(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }
            return sb.ToString();
        }

        public static bool Validate(out string reason,out int iMode)
        {
            bool rc = true;
            reason = null;
            if (testCasesClipBoard == null)
            {
                iMode = 1;
                reason = "testCasesClipBoard is not initialized";
            }
            else if (pegWindow == null)
            {
                iMode = 2;
                reason = "pegWindow is not initialized";
            }
            else if (pegWindowVM == null)
            {
                iMode = 3;
                reason = "pegWindowVM is not initialized";
            }
            
            if (reason != null)
                rc = false;
            iMode = 0;
            return rc;
        }
    }





    public class VMCollCash
    {
        public static Dictionary<string, VMColl> cache = new Dictionary<string, VMColl>();
        public static VMColl currentVMColl;



        // private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static MLogger Logger = MLogger.GetLogger(typeof(VMCollCash));

#if !v_16AndUp
        public static VMColl getVMCall(long testCaseId, long dataSheetId, bool isSharedData)
        {
#else

        public static VMColl getVMCall(string strDBIdx, long testCaseId, long dataSheetId, bool isSharedData, List<long?> testcaseAppliedAppIds = null)
        {

#endif

            Logger.Info("getVMCall", "testCaseId:" + testCaseId + " dataSheetId:" + dataSheetId);

            VMColl vmCall = null;

            if (cache.ContainsKey(testCaseId + ""))
            {
                vmCall = cache[testCaseId + ""];
                if (vmCall.DataSheetId != dataSheetId)
                {
                    cache.Remove(testCaseId + "");
                    vmCall = new VMColl(strDBIdx,testCaseId, dataSheetId, isSharedData,null);
#if v_16AndUp
                    vmCall.TestcaseAppliedAppIds = testcaseAppliedAppIds;
#endif
                    cache.Add(testCaseId + "", vmCall);
                }
            }
            else
            {
                vmCall = new VMColl(strDBIdx,testCaseId, dataSheetId, isSharedData);
                cache.Add(testCaseId + "", vmCall);
            }
            currentVMColl = vmCall;
            return vmCall;
        }

    }

    public class MarsSpecialClipBoard
    {
        DataTable specialClipBoardData;

        public DataTable SpecialClipBoardData
        {
            get { return specialClipBoardData; }
            set { specialClipBoardData = value; }
        }

        Dictionary<string, long> kwDict;

        public Dictionary<string, long> KwDict
        {
            get { return kwDict; }
            set { kwDict = value; }
        }

        Dictionary<string, long> objDict;

        public Dictionary<string, long> ObjDict
        {
            get { return objDict; }
            set { objDict = value; }
        }

        public MarsSpecialClipBoard()
        {
            List<string[]> valueArray = ClipboardHelper.ParseClipboardData();
            specialClipBoardData = ClipboardHelper.ParseClipboardToDataTable(valueArray);
            ApplyMapping();
        }

        public MarsSpecialClipBoard(DataTable dt)
        {
            specialClipBoardData = dt;
            ApplyMapping();
        }

        private void ApplyMapping()
        {
            foreach (DataRow row in specialClipBoardData.Rows)
            {
                if (row["keyword"].ToString().Equals("u_checkLoading"))
                {
                    row["keyword"] = "WindowIsLoaded";
                }
            }

            specialClipBoardData = specialClipBoardData.Rows.Cast<DataRow>()
                .Where(row => !row.ItemArray.All(field => field is System.DBNull || string.Compare((field as string)
                    .Trim(), string.Empty) == 0)).CopyToDataTable();

        }

        internal void InitKeywodDict()
        {
            List<String> pastedKeywords = new List<String>();
            pastedKeywords = specialClipBoardData.AsEnumerable().Select(dr => dr.Field<string>("keyword")).Distinct().ToList();

            B_KEYWORD objKeyword = new B_KEYWORD();

            Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> _keywordNames = objKeyword.GetKeyWordNames(pastedKeywords);
            kwDict = new Dictionary<string, long>(StringComparer.CurrentCultureIgnoreCase);

            foreach (var keyDto in _keywordNames.Keys)
            {
                if (kwDict.ContainsKey(keyDto.KEY_WORD_NAME) == false)
                    kwDict.Add(keyDto.KEY_WORD_NAME, keyDto.KEY_WORD_ID);
            }
        }

        internal void InitObjectDict(string strDBIdx, long _testCaseId, string pw)
        {
            List<long> appIds = BoHelper.GetAppIds(strDBIdx,  _testCaseId);
            List<String> pastedObjects = new List<String>();
            pastedObjects = specialClipBoardData.AsEnumerable().Select(dr => dr.Field<string>("object")).Distinct().ToList();
            B_REGISTED_OBJECT bobjDto = new B_REGISTED_OBJECT();
#if v_16AndUp
            List<B_REGISTED_OBJECT> objList = bobjDto.GetRegisterdObjectsByObjectNameFromCache(MarsMainWindow.CurrentDatabaseIdx,
                pastedObjects, appIds, pw);
#else
            List<T_REGISTED_OBJECTDTO> objList = bobjDto.GetRegisterdObjectsByObjectNameFromCache(pastedObjects, appIds, pw);
#endif
            objDict = new Dictionary<string, long>();

            foreach (var objDto in objList)
            {
                // AF objDict.Add(objDto.OBJECT_HAPPY_NAME, objDto.OBJECT_ID);
                objDict[objDto.OBJECT_HAPPY_NAME] = objDto.OBJECT_ID;
            }
        }
    }


}
