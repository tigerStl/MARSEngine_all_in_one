using com.Mars.Constants;
using MarsShellLib;
using MarsTestFrame.CommuniteServer;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using TestFrameMonitor;
using TestFrameMonitor.Form.Info;
using TestFrameMonitor.Server;
using TestFrameMonitor.Server.ServiceContracts;
#if _VEDIO_TIGER_
using Microsoft.Expression.Encoder.Profiles;
#endif

#if _VEDIO_TIGER_
using Microsoft.Expression.Encoder;
using Microsoft.Expression.Encoder.ScreenCapture;
using TestFrameMonitor.source.serializeration;
#endif

namespace QtpStarter.Info
{
    public delegate void FormIsRead();
    public delegate void MonitorFormCloseEvent();

    #region Breakpoints events
    public delegate void OnSetDebuggerModeEvent(SystemDebuggerMode iMode);
    #endregion //Breakpoints events

    #region Delegate for updating Form
    public delegate void TestSuiteTestCaseReadyEvent(string strTestSuiteName, string strTestCase);
    public delegate int OnTestSuiteTestCaseNamesChangeEvent(string strTestSuite, string strTestCase);
    public delegate int OnTestCaseListChangeEvent(List<TestStep4Services> lstTestSteps);
    public delegate int OnTestCompilerEndEvent(bool isError, string strError, List<TestStep4Services> objErrorInfo);
    public delegate int OnWriteCurrentLogEvent(string strMessage, int iErrorOrNormal);
    public delegate int OnCompilerOneTestStepEvent(TestStep4Services objCurrentCompilerTestStep);
    public delegate int BeforeRunTestStepEvent(TestStep4Services objTestStepRunning);
    public delegate int AfterRunTestStepEvent(string strWriteBackData, int iResult, string strMessage);
    public delegate void OnRefreshStepGridEvent();
    public delegate void OnLogModeChangedEvent(int iCurrentLogMode);
    public delegate void OnCurrentLoopChangeEvent(int iCurrentLoopId);
    public delegate void OnClientLoopCountChangeEvent(int iLoopCount);
    public delegate bool OnAddOrRemoveOneStepFromBreakpointsListEvent(TestStep4Services objStepToAddOrRemove, bool isAddNew, int debuggerMode);


    public delegate void OnOneLoopIsDoneEvent();

#if v_16AndUp
    public delegate void OnTestStoryBoardNameEvent(string strStoryBoardName);
    public delegate void OnTestDataSetNameEvent(string strDataSetName);
    public delegate void OnTestSToryBoardTotalStepsChangeEvent(int iStpsCount);
#endif

    #endregion

    public enum LogMode
    {
        LM_ALONE = 0,
        LM_ADD
    }


#if !_NORMAL_FORM_
    public partial class TestStepHintForm : MarsAppDeskTopToolBar
#else
    public partial class TestStepHintForm : Form
#endif
    {

        //private TestStepHintForm gMonitorForm = null;
        private static bool isLoad = false;
        private static MLogger Logger = MLogger.GetLogger(typeof(TestStepHintForm));
        private FormIsRead mFormReadyEventHandler = null;
        private MonitorFormCloseEvent mMonitorFormCloseEventHandler = null;
        private static TestStepHintForm gMonitorForm = null;
        private TestFlowDebugInfo CurrentDebugInfo = new TestFlowDebugInfo();
        public LogMode CurrentLogMode { get; set; }
        public bool IsBatchMode { get; private set; }

        #region monitor objects
        private object StatusGridMonitor = new object();
        private TestStep4Services LatestStep = null;
        #endregion //monitor objects

#if _VEDIO_TIGER_
        #region Screen Snap       
        private bool m_bRecordScreen = false;
        private Rectangle m_rectRecord = Rectangle.Empty;        
        #endregion
#endif
        public TestStepHintForm()
        {
            InitializeComponent();
            this.Icon = TestFrameMonitor.Properties.img.favicon;

#if _VEDIO_TIGER_
            m_bRecordScreen = AppConfigReader.GetVedioSetting();            
#else
#endif
            this.Text = MonitorTitle;
        }

        public string MonitorTitle
        {
            get
            {
#if _VEDIO_TIGER_
            return string.Format("Test Step Viewer-With Vedio");
            
#else
                return string.Format("Test Step Viewer");
#endif
            }
        }


        internal static TestStepHintForm GetInstance(string strUserName, int iPort = -1)
        {
            if (!isLoad) //return new TestStepHintForm();
            {
                if (gMonitorForm == null)
                {
                    gMonitorForm = new TestStepHintForm();
                }
                isLoad = true;
            }
            gMonitorForm.Text = string.Format("{0}:{1}", strUserName, gMonitorForm.MonitorTitle);

            gMonitorForm.Show();
            Thread.Sleep(50);
            gMonitorForm.Update();
            gMonitorForm.InitAllColors();
            Thread.Sleep(50);
            Logger.Info("---CreateAppBar", "End");
            return gMonitorForm;

        }

        private static Color PASS_COLOR = Color.LightGreen;
        private static Color FAILURE_COLOR = Color.Red;
        private static Color SKIP_COLOR = Color.LightYellow;
        private static Color CURRENT_COLOR = Color.LightBlue;

        private static Color GetColorByStringKey(string strKey, Color defaultColor)
        {

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string strColor = "";
            try
            {
                if (config.AppSettings.Settings[strKey] == null)
                {
                    if (!string.IsNullOrEmpty(defaultColor.Name))
                        config.AppSettings.Settings.Add(strKey, defaultColor.Name);
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                }
                strColor = config.AppSettings.Settings[strKey].Value;
                return Color.FromName(strColor);
            }
            catch (Exception e)
            {
                Logger.Error("GetColorByStringKey", string.Format("No such color Name:[{0}]\r\n[{1}]", strColor, e.Message), e);
                return defaultColor;
            }

        }

        internal void InitAllColors()
        {
            Logger.logBegin("InitAllColors");
            PASS_COLOR = GetColorByStringKey("PASS_COLOR", PASS_COLOR);
            FAILURE_COLOR = GetColorByStringKey("FAILURE_COLOR", FAILURE_COLOR);
            SKIP_COLOR = GetColorByStringKey("SKIP_COLOR", SKIP_COLOR);
            CURRENT_COLOR = GetColorByStringKey("CURRENT_COLOR", CURRENT_COLOR);

            this.StatusGrid.DefaultCellStyle.SelectionBackColor = CURRENT_COLOR;
            Logger.logEnd("InitAllColors");
        }
        #region delegate initialization

        /** 
         * 
         * 
         * **/
        public void OnNewInstanceofServiceCreated(MonitorService objService)
        {
            if (objService == null) return;
            objService.onTestSuiteTestCaseNamesChangeHandler = this.OnTestSuiteTestCaseNamesChangeImpl;
            objService.onTestCaseListChangeHandler = this.onTestCaseListChangeImpl;
            objService.onTestStepCompilerEndHandler = this.onTestStepCompilerEndImpl;
            objService.onCompilerOneTestStepHandler = this.onCompilerOneTestStepImpl;
            objService.beforeRunTestStepHandler = this.beforRunTestStepImpl;
            objService.afterRunTestStepHandler = this.afterRunTestStepImpl;
            objService.onWriteCurrentLogHandler = this.OnWriteCurrentLogHandler;
            objService.ReFreshGridStyleHandler = this.OnRefreshStepGridImpl;
            objService.OnLogModeChangedHandler = this.OnLogModeChangedImpl;
            objService.onCurrentLoopChangeHandler = this.onCurrentLoopChangeImpl;
            objService.onLoopCountChangeHandler = this.onLoopCountChangeImpl;
            objService.onBreakpointReachedHandler = this.OnBreakpointReachedImpl;
            objService.onGetCurrentModeHandler = this.OnGetCurrentModeImpl;
            objService.onTestSuiteId4ProjectChangedHandler = this.onTestSuiteId4ProjectImpl;
            #region SetNofity Debugger Events
            this.SetOnDebuggerModeEventHandler(objService.onSetDebuggerModeImpl);
            this.SetOnAddOrRemoveOneStepFromBreakpointsListHandler(objService.onAddOrRemoveOneStepFromBreakpointsListImpl, true);
            #endregion

            objService.onOneLoopIsDoneHandler = onOneLoopIsDoneImpl;

#if v_16AndUp
            objService.onTestStoryboardHandler = onTestStoryboardImpl;
            objService.onDataSetNameHandler = onDataSetNameImpl;
            objService.onTestStoryboardTotalStepsHandler = onTestStoryBoardTotalStepsImpl;
            objService.onTestStoryboardCurrentStepsNoHandler = onTestStoryBoardCurrentStepsNoImpl;
#endif
        }

        private void onTestSuiteId4ProjectImpl(string strTestSuiteId4Project)
        {
            this.CurrentDebugInfo.CurrentRelyId = strTestSuiteId4Project;
        }

        #endregion

        private void StartMonitorAndUpdate()
        {
            if (gMonitorForm == null) return;
            gMonitorForm.Show();
            Thread.Sleep(100);

        }
        private void WriteCurrentLoopInfoToStatusGrid()
        {
            if (this.TSTCGrid.Rows.Count < 3)
            {
                return;
            }
#if !Write_DataSet
            (this.TSTCGrid.Rows[2].Cells[1] as DataGridViewComboBoxCell).Value = this.CurrentDebugInfo.TestCurrentLoopId+1;
#endif
        }

        #region eventhandler out

        public void OnLogModeChangedImpl(int iMode)
        {
            CurrentLogMode = (LogMode)iMode;
        }

        public void onCurrentLoopChangeImpl(int iLoopId)
        {
            if (this.CurrentDebugInfo.TestCurrentLoopId != iLoopId)
            {
                /** change color to normal **/
                OnRefreshStepGridImpl();
                /** recovery all buttons to normal **/
                RemoveOtherCheckedButtonsStatus(this.resumeButton, this.toolStrip1);
                /** clear all breakpoints or other checked checkbox in Grid**/
                ClearAllChecks();
            }
            this.CurrentDebugInfo.TestCurrentLoopId = iLoopId;
            /** write to Status Grid **/
            WriteCurrentLoopInfoToStatusGrid();
#if _VEDIO_TIGER_
            /** begin to record Vedio **/
            BeginToRecordScreen();
#endif
        }

        public void onLoopCountChangeImpl(int iMx)
        {
#if !v_16AndUp
            this.SetTestCaseGridLoopMaxInfo(iMx);
#endif
        }

        public int OnWriteCurrentLogHandler(string strMessage, int iErrorOrNormal)
        {
            //this.Invoke(this.UpdateCurrentLogSection, {strMessage,null}) ;            
            UpdateCurrentLogSection(strMessage, iErrorOrNormal < 0 ? Color.Red : Color.Blue);
            return (int)ERROR_CODE._NO_ERROR;
        }

        public void OnRefreshStepGridImpl()
        {
            for (int i = 0; i < this.StatusGrid.Rows.Count; i++)
            {
                //this.StatusGrid.Rows[i].DefaultCellStyle.BackColor = i % 2 == 0 ? Color.LightBlue : Color.LightGray;
                //this.StatusGrid.Rows[i].DefaultCellStyle.BackColor = i % 2 == 0 ? Color.LightBlue : Color.White;
                this.StatusGrid.Rows[i].DefaultCellStyle.BackColor = i % 2 == 0 ? Color.LightGray : Color.White;
            }
            this.StatusGrid.Update();
        }

        public void AddFormCloseEventCloseHander(MonitorFormCloseEvent funcCloseEventHandler)
        {
            mMonitorFormCloseEventHandler += funcCloseEventHandler;
        }

        public void RefillTestStepsGrid(List<TestStep4Services> lstSteps)
        {
            if (lstSteps == null) return;
            Logger.Info("RefillTestStepsGrid", string.Format("total [{0}] Items to Insert", lstSteps.Count));

            this.StatusGrid.Rows.Clear();
            string strFormatStr = "{0,-" + SystemConstant.CNST_CLIENT_GRID_HINT_MXLENGHT + "}";
            string strRuningInfo = "";
            for (int i = 0; i < lstSteps.Count; i++)
            {
                strRuningInfo = string.Format("#:{2}\n" + strFormatStr + "{1}\n", "",//SystemConstant.CNST_CLIENT_GRID_KEYWORD, 
                    lstSteps[i].Keyword, lstSteps[i].RunID);
                strRuningInfo = string.Format("{2}" + strFormatStr + "{1,-12}\n", "",//SystemConstant.CNST_CLIENT_GRID_OBJECT, 
                    string.IsNullOrEmpty(lstSteps[i].ObjectName) ? "<NOT Set>" : lstSteps[i].ObjectName, strRuningInfo);
                strRuningInfo = string.Format("{2}" + strFormatStr + "{1,-12}", "",//SystemConstant.CNST_CLIENT_GRID_RC, 
                    string.IsNullOrEmpty(lstSteps[i].Row_Column) ? "<NOT Set>" : lstSteps[i].Row_Column, strRuningInfo);

                /** add one to Grid */
                this.StatusGrid.Rows.Add();
                //this.StatusGrid.Rows[i].DefaultCellStyle.BackColor = i % 2 == 0 ? Color.LightBlue : Color.LightGray;
                this.StatusGrid.Rows[i].DefaultCellStyle.BackColor = i % 2 == 0 ? Color.LightGray : Color.White;

                ((StepDetailCell)this.StatusGrid.Rows[i].Cells[1]).DisplayDetail = strRuningInfo;
                //this.StatusGrid.Rows[i].Cells[1].Value = strRuningInfo;
                //this.StatusGrid.Rows[i].Cells[1].DataGridView 
                //this.StatusGrid.Rows[this.StatusGrid.Rows.Count - 1].Height = 80;

                this.StatusGrid.Rows[i].Height = 72;
#if _VEDIO_TIGER_
                //this.StatusGrid.Rows[i].Cells[2].Value = TestFrameMonitor.Properties.img.eye;
#else
                //this.StatusGrid.Rows[i].Cells[2].Value = "-";
#endif
                if (this.StatusGrid.Rows[i].Cells[0] is DataGridViewCheckBoxCell)
                {
                    DataGridViewCheckBoxCell objCheckBoxCell = (DataGridViewCheckBoxCell)this.StatusGrid.Rows[i].Cells[0];
                    //objCheckBoxCell.Value = objCheckBoxCell.TrueValue;
                    this.StatusGrid.Rows[i].Cells[0].Tag = lstSteps[i];
#if _tigerDebug
                    this.StatusGrid.Update();
#endif
                }
#if _tigerDebug
                Thread.Sleep(200);
#endif
            }
            Logger.Info("RefillTestStepsGrid", "before update");
            this.StatusGrid.Update();
            Logger.Info("RefillTestStepsGrid", "after update");
            Thread.Sleep(100);
            Logger.logEnd("RefillTestStepsGrid");
        }
        #endregion

        #region Debugger Mode management
        private OnSetDebuggerModeEvent onSetDebuggerModeHandler = null;
        public void SetOnDebuggerModeEventHandler(OnSetDebuggerModeEvent pFunc)
        {
            if (onSetDebuggerModeHandler == null) onSetDebuggerModeHandler = pFunc;
        }

        private OnAddOrRemoveOneStepFromBreakpointsListEvent onAddOrRemoveAStepFromBreakPointsHandler = null;
        public void SetOnAddOrRemoveOneStepFromBreakpointsListHandler(OnAddOrRemoveOneStepFromBreakpointsListEvent pFunc, bool isAddFunc)
        {
            if (isAddFunc)
                this.onAddOrRemoveAStepFromBreakPointsHandler = pFunc;
            else
                if (this.onAddOrRemoveAStepFromBreakPointsHandler != null)
                this.onAddOrRemoveAStepFromBreakPointsHandler -= pFunc;
        }

        public void OnBreakpointReachedImpl(TestStep4Services objTestInfo, SystemDebuggerMode breakMode)
        {
            Logger.logBegin("OnBreakpointReachedEvent");
            if (objTestInfo == null) return;

            if (breakMode == SystemDebuggerMode.SDM_BREAKAT)
            {
                HotOneRowByObject(objTestInfo, Color.Orange);
                /** show hint window **/
                AnimatedHint.CreateHintForm(this.Width + 2, this.Height - 95);
#if (_VEDIO_TIGER_)
                PauseVedioRecorder() ;                
#endif
            }

            Logger.logEnd("OnBreakpointReachedEvent");
        }

        private TestFlowDebugInfo OnGetCurrentModeImpl()
        {
            return this.CurrentDebugInfo;
        }
        #endregion

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public void TestSuiteInfoEventHandler(string strTestSuite, string strTestCase)
        {
            if (this.TSTCGrid.RowCount < 2)
                LoadDefaultTSTCGridInfo();
            this.TSTCGrid.Rows[0].Cells[1].Value = strTestSuite;
            this.TSTCGrid.Rows[1].Cells[1].Value = strTestCase;
            //this.TSTCGrid.Update();

            this.CurrentDebugInfo.CurrentTestCaseName = strTestCase;
            this.CurrentDebugInfo.CurrentTestSuiteName = strTestSuite;
        }

        public void SetFormReadEventHandler(FormIsRead funcReady)
        {
            this.mFormReadyEventHandler += funcReady;
        }

        private void TestStepHintForm_Load(object sender, EventArgs e)
        {
#if !_NORMAL_FORM_
            this.Edge = AppBarEdges.Left;
#else
            SetAppPosition();
#endif

#if _VEDIO_TIGER_
            m_bRecordScreen = AppConfigReader.GetVedioSetting();
            if (m_bRecordScreen)
            {
                InitialiazationRecordRect();
            }
#endif

            LoadDefaultTSTCGridInfo();
        }

#if _NORMAL_FORM_
        public void SetAppPosition()
        {
            this.Left = 0;
            this.Top = 0;
            this.Height = Screen.FromControl(this).WorkingArea.Height;
        }
#endif

        private void LoadDefaultTSTCGridInfo()
        {
            while (this.TSTCGrid.Rows.Count < 3)
            {
                this.TSTCGrid.Rows.Add();
            }
            this.TSTCGrid.Rows[0].Cells[0].Value = "TS";
            //this.TSTCGrid.Rows[0].DefaultCellStyle.BackColor = Color.LightBlue;
            this.TSTCGrid.Rows[1].Cells[0].Value = "TC";
            //this.TSTCGrid.Rows[1].DefaultCellStyle.BackColor = Color.LightBlue;
#if !v_16AndUp
            this.InitializeLoopRow(this.TSTCGrid, 2);            
#else
            this.InitializeThirdRow(this.TSTCGrid);
#endif
        }

#if v_16AndUp
        private void InitializeThirdRow(DataGridView objTarget, int iRowId = 2)
        {
            if (objTarget == null) return;
            if (iRowId >= objTarget.Rows.Count) return;
            objTarget.Rows[iRowId].Cells[0].Value = "DataSet";
            //objTarget.Rows[iRowId].DefaultCellStyle.BackColor = Color.LightBlue;
            //objTarget.Rows[iRowId].Cells[1] = new DataGridViewTextBoxCell();
            //DataGridViewComboBoxCell objLoopCell = objTarget.Rows[iRowId].Cells[1] as DataGridViewComboBoxCell;
        }
#else
        private void InitializeLoopRow(DataGridView objTarget, int iRowId)
        {
            if (objTarget == null) return;
            if (iRowId >= objTarget.Rows.Count) return;
            objTarget.Rows[iRowId].Cells[0].Value = "LOOP";
            //objTarget.Rows[iRowId].DefaultCellStyle.BackColor = Color.LightBlue;
            objTarget.Rows[iRowId].Cells[1] = new DataGridViewComboBoxCell();
            DataGridViewComboBoxCell objLoopCell = objTarget.Rows[iRowId].Cells[1] as DataGridViewComboBoxCell;
            
        }
#endif


#if v_16AndUp
        private string GetCurrrentLoopRowInfo(int iRow)
        {
            return string.Format("{0} {2}/{1} {3}", this.CurrentTestStoryBoardName, this.totalStoryboardSteps, this.currentStoryboardStepNo, iRow);
        }
#endif
        private void SetTestCaseGridLoopMaxInfo(int iMx)
        {
            if (this.TSTCGrid.Rows.Count < 2) return;
            DataGridViewComboBoxCell objCombobox = this.TSTCGrid.Rows[2].Cells[1] as DataGridViewComboBoxCell;
            objCombobox.Items.Clear();
            for (int i = 1; i <= iMx; i++)
            {
#if !v_16AndUp
                objCombobox.Items.Add(i);
#else
                objCombobox.Items.Add(GetCurrrentLoopRowInfo(i));
#endif
            }
            objCombobox.Value = objCombobox.Items.Count > 0 ? objCombobox.Items[0] : null;
            this.TSTCGrid.Update();
        }

        private void TSTCGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void TestStepHintForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //this.RegisterAppBar(true);

            if (this.mMonitorFormCloseEventHandler != null)
                this.mMonitorFormCloseEventHandler();
            //gMonitorForm = null;

        }

        private void RestoreSelectedRowsStyle()
        {
            for (int i = 0; i < this.StatusGrid.SelectedRows.Count; i++)
            {
                this.StatusGrid.Rows[i].DefaultCellStyle.BackColor = Color.White;
            }
            this.StatusGrid.Update();
            Thread.Sleep(10);
        }


        private DataGridViewRow HotOneRowByObject(TestStep4Services objCurrentObj, Color clrDesBackColor)
        {
            for (int i = 0; i < this.StatusGrid.RowCount; i++)
            {
                if (this.StatusGrid.Rows[i].Cells == null) continue;
                if (this.StatusGrid.Rows[i].Cells.Count <= 0) continue;
                if (this.StatusGrid.Rows[i].Cells[0].Tag is TestStep4Services)
                {
                    TestStep4Services objTmp = (TestStep4Services)this.StatusGrid.Rows[i].Cells[0].Tag;
                    if (objTmp.RunID == objCurrentObj.RunID)
                    {
                        this.StatusGrid.Rows[i].Selected = true;
                        this.StatusGrid.Rows[i].DefaultCellStyle.BackColor = clrDesBackColor;
                        this.StatusGrid.Update();
                        Thread.Sleep(10);
                        AutoScollGridToSelectedRowVisible();
                        this.StatusGrid.Update();
                        return this.StatusGrid.Rows[i];
                    }
                }
            }
            Logger.Warnning("HotOneRowByObject", string.Format("Can't find the currentObject,stepId:[{0}] Keyword:[{1}] ObjectName:[{2}]",
                objCurrentObj.AssignedTestStepId, objCurrentObj.Keyword, objCurrentObj.ObjectName));
            return null;
        }

        private void UpdateCurrentLogSection(string strTextMessage, Color clrFontColor, int iMode = 0)
        {
            if (this.CurrentLogMode == LogMode.LM_ALONE)
                this.listView1.Items.Clear();
            ListViewItem objNewItem = this.listView1.Items.Add(strTextMessage);

            if (clrFontColor != Color.Empty)
                objNewItem.ForeColor = clrFontColor;
            objNewItem.EnsureVisible();
            this.listView1.Refresh();
        }

        private void AutoScollGridToSelectedRowVisible()
        {
            if (this.StatusGrid.SelectedRows == null) return;
            if (this.StatusGrid.SelectedRows.Count <= 0) return;

            int iIdx = this.StatusGrid.SelectedRows[0].Index;
            this.StatusGrid.FirstDisplayedScrollingRowIndex = (iIdx - 4) > 0 ? (iIdx - 4) : iIdx;
        }

        #region Event implement
        internal int OnTestSuiteTestCaseNamesChangeImpl(string strTestSuite, string strTestCase)
        {
            TestSuiteInfoEventHandler(strTestSuite, strTestCase);
            return 1;
        }

        internal int onTestCaseListChangeImpl(List<TestStep4Services> lstTestSteps)
        {
            RefillTestStepsGrid(lstTestSteps);
            return 1;
        }

        internal int onTestStepCompilerEndImpl(bool isError, string strError, List<TestStep4Services> objErrorInfo)
        {
            /** un selected all rows **/
            RestoreSelectedRowsStyle();
            this.StatusGrid.ClearSelection();
            this.StatusGrid.Update();
            if (isError)
            {
                bool bFindItem = false;
                this.listView1.Items.Clear();
                for (int i = 0; i < objErrorInfo.Count; i++)
                {
                    TestStep4Services objCurrentCmpiled = objErrorInfo[i];
                    /** get the tag info of the grid and compile **/
                    for (int j = 0; j < this.StatusGrid.RowCount; j++)
                    {
                        if (this.StatusGrid.Rows[j].Cells == null) continue;
                        if (this.StatusGrid.Rows[j].Cells.Count <= 0) continue;
                        if (this.StatusGrid.Rows[j].Cells[0].Tag is TestStep4Services)
                        {
                            TestStep4Services objTmp = (TestStep4Services)this.StatusGrid.Rows[j].Cells[0].Tag;
                            if (objTmp.RunID == objCurrentCmpiled.RunID)
                            {
                                this.StatusGrid.Rows[j].Selected = false;
                                this.StatusGrid.Rows[j].DefaultCellStyle.BackColor = FAILURE_COLOR;
                                //this.StatusGrid.Rows[j].Cells[0].Style.BackColor = Color.Red;
                                this.StatusGrid.FirstDisplayedScrollingRowIndex = (j - 4 > 0) ? (j - 4) : 0;
                                this.StatusGrid.Update();

                                bFindItem = true;
                                Thread.Sleep(10);
                            }
                        }
                    }
                }
                if ((!bFindItem) && (objErrorInfo.Count > 0))
                    Logger.Info("WARNNING", string.Format("Can't ErrorStep info,RUNID:[{0}], \r\n\tKeyword:[{1}]", objErrorInfo[0].RunID, objErrorInfo[0].Keyword));
                this.UpdateCurrentLogSection(strError, Color.Red);
                return 1;
            }
            else
            {
                return 1;
            }
        }

        public int onCompilerOneTestStepImpl(TestStep4Services objCurrentCompilerTestStep)
        {
            /** un selected all rows **/
            RestoreSelectedRowsStyle();
            this.StatusGrid.ClearSelection();
            this.StatusGrid.Update();

            if (objCurrentCompilerTestStep == null) return 1;
            this.HotOneRowByObject(objCurrentCompilerTestStep, Color.LightBlue);

            listView1.Items.Clear();
            UpdateCurrentLogSection("Compiling one Test step....", Color.Blue);
            return 1;

        }

        public int beforRunTestStepImpl(object objFromVBS)
        {
            Logger.logBegin("beforRunTestStepImpl");
            Logger.Info("beforRunTestStepImpl", string.Format("object objFromVBS:[{0}]", objFromVBS == null ? "NULL" : objFromVBS.ToString()));
            TestStep4Services objTestStepRunning = (TestStep4Services)objFromVBS;

            listView1.Items.Clear();
            if (objTestStepRunning == null) return 1;

            this.CurrentLogMode = LogMode.LM_ADD;
            this.UpdateCurrentLogSection(objTestStepRunning.Keyword, CURRENT_COLOR);
            if (!string.IsNullOrEmpty(objTestStepRunning.ObjectName))
                this.UpdateCurrentLogSection(string.Format("object:[{0}]", objTestStepRunning.ObjectName), Color.Blue);
            if (!string.IsNullOrEmpty(objTestStepRunning.Row_Column))
                this.UpdateCurrentLogSection(string.Format("R_C:[{0}]", objTestStepRunning.Row_Column), Color.Blue);
            if (!string.IsNullOrEmpty(objTestStepRunning.Value))
                this.UpdateCurrentLogSection(string.Format("Value:[{0}]", objTestStepRunning.Value), Color.Blue);
            this.CurrentLogMode = LogMode.LM_ALONE;

            /** hot grid **/
            //this.HotOneRowByObject(objTestStepRunning, Color.LightGreen);
            DataGridViewRow objRow = this.HotOneRowByObject(objTestStepRunning, Color.LightBlue);
            /** set latest Object **/
            LatestStep = objTestStepRunning;
#if _VEDIO_TIGER_
            Caption4VideoMgr.AddOneCaptionStep(objTestStepRunning.Keyword, objTestStepRunning.ObjectName, objTestStepRunning.Row_Column, objTestStepRunning.Value);
            MarsVedioRptMgr.AddOneStepInfo(objTestStepRunning.RunID + "", objTestStepRunning.Keyword, objTestStepRunning.QuickAccess, objTestStepRunning.ObjectName, objTestStepRunning.Row_Column,
                objTestStepRunning.Value);
            if (objRow.Cells.Count==3)
            {
                objRow.Cells[2].Tag = new object[] { MarsVedioRptMgr.GetVedioIndex(), MarsVedioRptMgr.GetLastStepInfo() };
            }
#endif
            return 1;
        }
        #endregion //Event Implement


        internal int afterRunTestStepImpl(string strWriteBackData, int iResult, string strMessage)
        {
            Logger.Info("afterRunTestStepImpl", string.Format("strWriteBackData:[{0}] iResult:[{1}] Message:[{2}]", strWriteBackData, iResult, strMessage));
            listView1.Items.Clear();
            try
            {
                //if (objTestStepRunning == null) return 1;
                this.UpdateCurrentLogSection("after one Step is done", Color.Blue);
                if (LatestStep != null)
                {
                    this.UpdateCurrentLogSection(LatestStep.Keyword, Color.Blue);
                    this.UpdateCurrentLogSection(LatestStep.ObjectName, Color.Blue);
                    this.UpdateCurrentLogSection(LatestStep.Row_Column, Color.Blue);
                    this.UpdateCurrentLogSection(LatestStep.Value, Color.Blue);
                    this.UpdateCurrentLogSection(strWriteBackData, Color.Blue);
                    if (iResult == 1)
                    {
                        this.HotOneRowByObject(LatestStep, CURRENT_COLOR/* Color.FromArgb(200,255,200)*/);
                    }
                    else
                    {
                        if (iResult == 2)
                        {
                            this.HotOneRowByObject(LatestStep, SKIP_COLOR /*Color.FromArgb(200, 255, 200)*/);
                        }
                        else
                        {
                            this.HotOneRowByObject(LatestStep, FAILURE_COLOR /*Color.FromArgb(200, 255, 200)*/);
                        }
                    }
                }
                else
                {
                    Logger.Warnning("afterRunTestStepImpl", "LatestStep is Null!!!!");
                }

#if _VEDIO_TIGER_
                MarsVedioRptMgr.AttachDataToLast(strWriteBackData, iResult == 1, strMessage);
#endif
                return 1;
            }
            catch (Exception e)
            {
                Logger.Error("afterRunTestStepImpl", string.Format("Exception:[{0}]", e.Message), e);
                return 0;
            }

        }

        private void listView1_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void StatusGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

        }


        private void UpdateCurrentModeLabelByDebuggerMode(SystemDebuggerMode systemDebuggerMode)
        {
            string strLabel = "";
            switch (systemDebuggerMode)
            {
                case SystemDebuggerMode.SDM_BREAKAT:
                    strLabel = "BreakPoints";
                    break;
                case SystemDebuggerMode.SDM_REPLAY_THESAME_TEST:
                    strLabel = "Replay";
                    break;
                case SystemDebuggerMode.SDM_REUSME:
                    strLabel = "Resume";
                    break;
                case SystemDebuggerMode.SDM_RUNFROM:
                    strLabel = "Run From...";
                    break;
                case SystemDebuggerMode.SDM_SKIP:
                    strLabel = "skip";
                    break;
                default: return;
            }
            this.troopModeLabel.Text = string.Format("Current Mode:[{0}]", strLabel);
        }

        private void DoSetSkipSteps()
        {

            CurrentDebugInfo.TestDebugMode = (int)SystemDebuggerMode.SDM_SKIP;
            NotifyClientBeginBreakMode(SystemDebuggerMode.SDM_SKIP);
        }

        private void DoSetBreakPoints(SystemDebuggerMode isBreak)
        {
            Logger.logBegin("DoSetBreakPoints");
            /**
             * Descriptions:
             *   Setting break points means system is enable test flow stopping at sepecial lines.
             * 1, clear all checkbox
             * 2, once checkbox is clicked, send information to client via server
             * 
             */
            /** set current Debug information **/
            if (isBreak != SystemDebuggerMode.SDM_REUSME)
                CurrentDebugInfo.TestDebugMode = (int)isBreak;

            NotifyClientBeginBreakMode(isBreak);

            ClearBreakPointsCatch();

            Logger.logEnd("DoSetBreakPoints");
        }

        private void NotifyClientBeginBreakMode(SystemDebuggerMode iBreakOrResume)
        {
            Logger.logBegin("NotifyClientBeginBreakMode");
            if (this.onSetDebuggerModeHandler == null) return;
            this.onSetDebuggerModeHandler(iBreakOrResume);
            Logger.logEnd("NotifyClientBeginBreakMode");
        }

        private void ClearBreakPointsCatch()
        {

        }

        private void ClearAllChecks()
        {
            bool lockWasTaken = false;
            try
            {
                System.Threading.Monitor.Enter(this.StatusGridMonitor, ref lockWasTaken);

                for (int i = 0; i < this.StatusGrid.Rows.Count; i++)
                {
                    if (this.StatusGrid.Rows[i].Selected) this.StatusGrid.Rows[i].Selected = false;
                    if (this.StatusGrid.Rows[i].Cells[0] is DataGridViewCheckBoxCell)
                    {
                        DataGridViewCheckBoxCell objChckBoxCell = (DataGridViewCheckBoxCell)this.StatusGrid.Rows[i].Cells[0];
                        if ((objChckBoxCell.Value == objChckBoxCell.TrueValue))
                            objChckBoxCell.Value = objChckBoxCell.FalseValue;
                    }
                }
            }
            finally
            {
                if (lockWasTaken)
                    System.Threading.Monitor.Exit(StatusGridMonitor);
            }
        }

        private void DoRunFrom()
        {
            CurrentDebugInfo.TestDebugMode = (int)SystemDebuggerMode.SDM_RUNFROM;
        }

        private void RemoveOtherCheckedButtonsStatus(ToolStripButton sender, ToolStrip objParent)
        {
            for (int i = 0; i < objParent.Items.Count; i++)
            {
                object objCurrentSub = objParent.Items[i];
                if (!(objCurrentSub is ToolStripButton)) continue;
                if ((sender == objCurrentSub)) continue;
                else
                {
                    if (sender != resumeButton)
                    {
                        if (objCurrentSub == resumeButton) continue;
                        (objCurrentSub as ToolStripButton).Enabled = false;
                        ((ToolStripButton)objCurrentSub).Checked = false;
                    }
                    else
                    {
                        (objCurrentSub as ToolStripButton).Enabled = true;
                        ((ToolStripButton)objCurrentSub).Checked = false;
                    }

                }

            }
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            Logger.logBegin("toolStrip1_ItemClicked");

        }

        private void StatusGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void BreakPointToolButton_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void StatusGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int iColIdx = e.ColumnIndex;
            int iRowIdx = e.RowIndex;

            object objCell = null;
            if (iRowIdx < 0) return;
            if (sender.Equals(this.StatusGrid))
            {
                if (iColIdx == 0)
                {
                    #region //column 1
                    /*** column Checkbox ***/
                    if (!(this.StatusGrid.Rows[iRowIdx].Cells[0] is DataGridViewCheckBoxCell)) return;
                    DataGridViewCheckBoxCell objCheckCell = (this.StatusGrid.Rows[iRowIdx].Cells[0] as DataGridViewCheckBoxCell);
                    /** Notify client that a new breakpoint is set     **/
                    if (onAddOrRemoveAStepFromBreakPointsHandler == null)
                    {
                        Logger.Error("StatusGrid_CellValueChanged", string.Format("no Event function is assigned, can't pass breakpoints row:[{0}] to client", iRowIdx));
                        return;
                    }
                    bool isChecked = (bool)objCheckCell.EditedFormattedValue;
                    objCheckCell.Value = (!isChecked) ? objCheckCell.TrueValue : objCheckCell.FalseValue;
                    StatusGrid.EndEdit();
                    //StatusGrid.NotifyCurrentCellDirty(true);
                    if (objCheckCell.Value == objCheckCell.TrueValue)
                    {
                        /** get the object step and pass to client  **/
                        if ((objCell = this.StatusGrid.Rows[iRowIdx].Cells[0].Tag) is TestStep4Services)
                        {
                            this.onAddOrRemoveAStepFromBreakPointsHandler((TestStep4Services)objCell, true, this.CurrentDebugInfo.TestDebugMode);
                            /** change the row to breakpoint color **/
                            if (this.CurrentDebugInfo.TestDebugMode == (int)SystemDebuggerMode.SDM_BREAKAT)
                                //this.StatusGrid.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.IndianRed;
                                this.StatusGrid.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                            else
                                this.StatusGrid.Rows[iRowIdx].DefaultCellStyle.BackColor = Color.Yellow;
                            this.StatusGrid.Rows[iRowIdx].Selected = false;
                        }
                    }
                    else
                    {
                        /** Notify client that a new breakpoint is removed **/
                        if ((objCell = this.StatusGrid.Rows[iRowIdx].Cells[0].Tag) is TestStep4Services)
                        {
                            this.onAddOrRemoveAStepFromBreakPointsHandler((TestStep4Services)objCell, false, this.CurrentDebugInfo.TestDebugMode);
                            //this.StatusGrid.Rows[iRowIdx].DefaultCellStyle.BackColor = iRowIdx % 2 == 0 ? Color.LightBlue : Color.LightGray;
                            this.StatusGrid.Rows[iRowIdx].DefaultCellStyle.BackColor = iRowIdx % 2 == 0 ? Color.LightGray : Color.White;
                            this.StatusGrid.Rows[iRowIdx].Selected = false;
                        }
                    }
                    #endregion //column 1
                }
                else
                {
#if (_VEDIO_TIGER_)
                    if (iColIdx == 2)
                    {
                    #region Vedio cell is clicked
                        if (this.StatusGrid.Rows[iRowIdx].Cells.Count >= 3)
                        {
                            DataGridViewCell objSelectedCell = this.StatusGrid.Rows[iRowIdx].Cells[2];
                            if (objSelectedCell == null) return;
                            if (!(objSelectedCell.Tag is object[])) return;

                            MarsTigerXmlReportItem objIdx = (MarsTigerXmlReportItem)((object[])objSelectedCell.Tag)[1];
                            MarsXmlVedioIndex objVedioIdx = (MarsXmlVedioIndex)((object[])objSelectedCell.Tag)[0];
                            ReplayVedioFromSpecialItem(objVedioIdx, objIdx);
                        }
                    #endregion
                    }
#endif
                    return;
                }
            }
        }

        private void BreakPointToolButton_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripButton)
            {
                //if (((ToolStripButton)sender).Checked)
                //if ("RESUME".CompareTo(((ToolStripButton)sender).Tag.ToString().ToUpper()) != 0)
                RemoveOtherCheckedButtonsStatus((ToolStripButton)sender, this.toolStrip1);
            }
            else return;

            /** call different method **/
            string strTag = ((ToolStripButton)sender).Tag.ToString();
            if (SystemConstant.CNST_MONITOR_RUNFROM.CompareTo(strTag) == 0)
            {
                /** run from **/
                DoRunFrom();
                UpdateCurrentModeLabelByDebuggerMode(SystemDebuggerMode.SDM_RUNFROM);
                return;
            }
            if (SystemConstant.CNST_MONITOR_BREAKS.CompareTo(strTag) == 0)
            {
                /** Set Break Points **/
                ClearAllChecks();
                UpdateCurrentModeLabelByDebuggerMode(SystemDebuggerMode.SDM_BREAKAT);
                this.CurrentDebugInfo.TestDebugMode = (int)SystemDebuggerMode.SDM_BREAKAT;
                DoSetBreakPoints(SystemDebuggerMode.SDM_BREAKAT);

                return;
            }
            if ("RESUME".CompareTo(strTag.ToUpper()) == 0)
            {
                /** Continue to run for all command buttons **/
                DoSetBreakPoints((SystemDebuggerMode)(((int)SystemDebuggerMode.SDM_REUSME) | this.CurrentDebugInfo.TestDebugMode));
                UpdateCurrentModeLabelByDebuggerMode(SystemDebuggerMode.SDM_REUSME);

#if (_VEDIO_TIGER_)
                this.ResumeVedioRecord();
#endif
                return;
            }

            if ("Play-Back".CompareTo(strTag) == 0)
            {
                ClearAllChecks();
                UpdateCurrentModeLabelByDebuggerMode(SystemDebuggerMode.SDM_REPLAY_THESAME_TEST);
                /** tell client stop and wait for advanced command **/
                DoSetBreakPoints(SystemDebuggerMode.SDM_REPLAY_THESAME_TEST);
                /** call qtp starter to restart qtp and run **/
                RestartTest();
            }

            if (SystemConstant.CNST_MONITOR_SKIP.CompareTo(strTag) == 0)
            {
                /** Set Skip steps **/
                DoSetSkipSteps();
                UpdateCurrentModeLabelByDebuggerMode(SystemDebuggerMode.SDM_SKIP);
                return;
            }
        }

        private void RestartTest()
        {
            Process objNewProce = new Process { StartInfo = new ProcessStartInfo { FileName = @".\QtpStarter.exe" } };
            objNewProce.Start();
        }

        private void StatusGrid_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void TSTCGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int iRow = e.RowIndex;
            if (iRow == 2)
            {
#if !v_16AndUp
                // loop value
                if (!(sender is DataGridViewComboBoxCell ))
                {
                    return;
                }
                DataGridViewComboBoxCell objCell = (DataGridViewComboBoxCell)sender;
                if (objCell.Value is int)
                {
                    this.CurrentDebugInfo.TestCurrentLoopId = (int)objCell.Value - 1;
                    Logger.Info("TSTCGrid_CellValueChanged", string.Format("Current Test Debugger Info. TestCurrentLoopId is [{0}]", this.CurrentDebugInfo.TestCurrentLoopId));
                }
                else
                {
                    this.CurrentDebugInfo.TestCurrentLoopId = 0;
                    Logger.Info("TSTCGrid_CellValueChanged", string.Format("Loop cell is not a int, default value [{0}] is applied", this.CurrentDebugInfo.TestCurrentLoopId));
                }
#else
                /// for current version, no loop information is available
                // if (!(sender is DataGridViewTextBoxCell))
                //{
                //    return;
                //}
#endif
            }
        }

        private void TestStepHintForm_Resize(object sender, EventArgs e)
        {


        }

        bool isTaskBar = true;
        object TmpLock = new object();

        private void TestStepHintForm_SizeChanged(object sender, EventArgs e)
        {

        }


        private void TestStepHintForm_ResizeEnd(object sender, EventArgs e)
        {

        }

        private void StatusGrid_MouseClick(object sender, MouseEventArgs e)
        {
            //this.contextMenuStrip1.Show(-e.X, e.Y);
        }

#if _NORMAL_FORM_
        protected override void WndProc(ref Message message)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MOVE = 0xF010;

            switch (message.Msg)
            {
                case WM_SYSCOMMAND:
                    int command = message.WParam.ToInt32() & 0xfff0;
                    if (command == SC_MOVE)
                        return;
                    break;
            }

            base.WndProc(ref message);
        }
#endif

#if _VEDIO_TIGER_
        private string m_strVedioRoot = null;
        
        private ScreenCaptureJob m_objScreenJob = null;
        private MarsXmlVedioIndex m_objCaptionInfo = null;
        private void InitialiazationRecordRect()
        {
            //Rectangle rect = Screen.FromControl(this).WorkingArea;
            this.m_rectRecord = new Rectangle(this.Width,0, Screen.FromControl(this).WorkingArea.Width-2, Screen.FromControl(this).WorkingArea.Height-2);
            if (this.m_rectRecord.Width % 4 != 0)
            {
                this.m_rectRecord.Width = ((int)this.m_rectRecord.Width / 4) * 4;
            }
            if (this.m_rectRecord.Height % 4 != 0)
            {
                this.m_rectRecord.Height = ((int)this.m_rectRecord.Height / 4) * 4;
            }

        }

        private void BeginToRecordScreen()
        {   /** **/
            if (m_objScreenJob==null)
            {
                m_objScreenJob = new ScreenCaptureJob();
            }
            else
            {
                if ((m_objScreenJob.Status!= RecordStatus.NotStarted)||(m_objScreenJob.Status!=RecordStatus.Stopped))
                    m_objScreenJob.Stop();
            }
            /** 创建文件名称 如果存在则删除 **/
            string strOutputDir = "", strOutputFileName = "";
            string strVedioFileName = GetFileRecodVedioName(ref strOutputDir, ref strOutputFileName);
            if (System.IO.File.Exists(strVedioFileName))
            {
                System.IO.File.Delete(strVedioFileName);
            }
            if (this.m_rectRecord.Width==0||this.m_rectRecord.Height==0)
            {
                InitialiazationRecordRect();
            }
            m_objScreenJob.CaptureRectangle = this.m_rectRecord;
            //m_objScreenJob.OutputPath = strOutputDir;
            m_objScreenJob.ShowFlashingBoundary = false;
            m_objScreenJob.OutputScreenCaptureFileName = strVedioFileName;
            
            m_objScreenJob.Start();
            Caption4VideoMgr.RefreshTimeSpan();
            Caption4VideoMgr.RefreshCaptionList();

            MarsVedioRptMgr.Initialization();
        }

        private string GetFileRecodVedioName(ref string strDesPath, ref string strDesFileName)
        {
            if (m_strVedioRoot==null)
            {
                m_strVedioRoot = MarsXmlVedioIndex.GetVedioPath(); ;
            }
            string strDt = DateTime.Now.ToString("YYYYMMDD");
            string strVedioName = Path.Combine(m_strVedioRoot, strDt);
            //this.CurrentDebugInfo.CurrentTestCaseName
            string strTstSuite = MarsXmlVedioIndex.GetNormalizedFileName(this.CurrentDebugInfo.CurrentTestSuiteName);
            string strTstCs = MarsXmlVedioIndex.GetNormalizedFileName(this.CurrentDebugInfo.CurrentTestCaseName);            
            strVedioName = MarsXmlVedioIndex.GetVedioOriginalFileName(strTstSuite, strTstCs, this.CurrentDebugInfo.TestCurrentLoopId, this.CurrentDebugInfo.CurrentRelyId);// m_objRandomGen.Next(1000));
            strDesPath = m_strVedioRoot;
            strDesFileName = strVedioName;
            
            //strVedioName = string.Format("{0}\\{1}", m_strVedioRoot, strVedioName);
            return strVedioName;
        }

        private void PauseVedioRecorder()
        {
            if (m_objScreenJob == null) return;
            if (m_objScreenJob.Status == RecordStatus.Running)
                m_objScreenJob.Pause();
        }

        private void ResumeVedioRecord()
        {
            if (m_objScreenJob == null) return;
            if (m_objScreenJob.Status == RecordStatus.Paused)
                m_objScreenJob.Resume();
        }
#endif
        private void onOneLoopIsDoneImpl()
        {
            Logger.logBegin("onOneLoopIsDoneImpl");
#if _VEDIO_TIGER_
            if (m_objScreenJob == null) return;
            m_objScreenJob.Stop();
            //string strError = "";
            //if (!MarsVedioRptMgr.ExportsAll(ref strError))
            //{
            //    Logger.Error("onOneLoopIsDoneImpl", strError);
            //}
            /***
            * create a task to convert
            */
            string strFileName = m_objScreenJob.ScreenCaptureFileName;
            //System.Threading.Tasks.Task objTask = new System.Threading.Tasks.Task(delegate { ConvertToMp4(strFileName); });
            //objTask.Start();
            ConvertToMp4(strFileName);

            Logger.logEnd("onOneLoopIsDoneImpl");
            m_objScreenJob = null;
#endif
        }

#if v_16AndUp
        private string CurrentTestStoryBoardName = "";
        private int totalStoryboardSteps = -1;
        private int currentStoryboardStepNo = -1;
        private string currentDataSetName = "";
        internal void onDataSetNameImpl(string strDataSetName)
        {
            Logger.Info("onDataSetNameImpl", string.Format("current data set Name is :[{0}]", strDataSetName));
            currentDataSetName = strDataSetName;

            if (this.TSTCGrid.Rows.Count < 3) return;
            //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(delegate ()
            {
                try
                {
                    this.TSTCGrid.Rows[2].Cells[1].Value = strDataSetName;
                    //(this.TSTCGrid.Rows[2].Cells[1] as DataGridViewTextBoxCell).Value = strDataSetName;
                    //this.TSTCGrid.Update();
                }
                catch (Exception e)
                {
                    Logger.Error("onDataSetNameImpl", string.Format("Exception:[{0}]", e.Message), e);
                }

            };
            //(this.TSTCGrid.Rows[2].Cells[1] as DataGridViewTextBoxCell).Value = strDataSetName;
            //this.TSTCGrid.Update();
        }
        internal void onTestStoryboardImpl(string strStoryBoardName)
        {
            Logger.Info("onTestStoryboardImpl", string.Format("Current storyBoardName:[{0}]", strStoryBoardName));
            CurrentTestStoryBoardName = strStoryBoardName;
        }
        internal void onTestStoryBoardTotalStepsImpl(int iCnt)
        {
            this.totalStoryboardSteps = iCnt;
        }

        internal void onTestStoryBoardCurrentStepsNoImpl(int iCurrentStpId)
        {
            this.currentStoryboardStepNo = iCurrentStpId;
        }
#endif
#if _VEDIO_TIGER_
       
        class MarsConvertMp4Thread 
        {
            private static int monitorVarForConvertMp4 = 1;
            private Thread convertThrad=null;
            string FileName = "";
            internal MarsConvertMp4Thread(string strFileName)
            {
                FileName = strFileName;
                try
                {
                    Monitor.Enter(monitorVarForConvertMp4);
                    convertThrad = new Thread(new ThreadStart(new Action(delegate ()
                    {
                        try
                        {
                            Logger.Info("----Test-----", "Enter Thread.....");
                            string strErrorx = "";
                            MarsVedioRptMgr.ConvertVedioWith(strFileName, ref strErrorx);
                            Logger.Info("----Test-----", "Exit Thread.....");
                        }
                        catch (Exception x)
                        {
                            Logger.Error("MarsConvertMp4Thread.convertThrad.run", $"exception:{x.Message})", x);
                        }
                        finally
                        {
                            Monitor.Exit(monitorVarForConvertMp4);
                        }

                    })));
                }
                catch (Exception)
                {
                    Monitor.Exit(monitorVarForConvertMp4);
                }
                finally{

                }
            }
            internal void startThrd()
            {
                if (convertThrad == null) return;
                convertThrad.Start();
            }
        }
        private void ConvertToMp4(string strFileName)
        {
            //Caption4VideoMgr.RefreshCaptionList();
            //Caption4VideoMgr.RefreshTimeSpan();
            //Thread.Sleep(1);
            //Caption4VideoMgr.AddOneCaptionStep("FILLEDIT", "USER_NAME", "", "hiQauser\n>");
            //Thread.Sleep(2);
            //Caption4VideoMgr.AddOneCaptionStep("FILLEDIT", "USER_PASSWORD", "", "hiQauser");
            string strError = "";
            MarsVedioRptMgr.UpdateTestProject(this.CurrentDebugInfo.CurrentTestSuiteName);
            MarsVedioRptMgr.UpdateTestSuite(this.CurrentDebugInfo.CurrentTestCaseName);
            MarsVedioRptMgr.UpdateTestCurrentRelyId(this.CurrentDebugInfo.CurrentRelyId);
            MarsVedioRptMgr.UpdateCurrentLoopId(this.CurrentDebugInfo.TestCurrentLoopId);
            bool isRight = MarsVedioRptMgr.ExportsAll(ref strError);
            Logger.Info("ConvertToMp4",string.Format("FileName:[{0}]",strFileName));
            //Dispatcher.CurrentDispatcher.BeginInvoke( )

            if (IsBatchMode == true)
            {
                Logger.Info("ConvertToMp4", "Without a thread.....");
                string strErrorx = "";
                MarsVedioRptMgr.ConvertVedioWith(strFileName, ref strErrorx);
                Logger.Info("ConvertToMp4", "Without a thread.....");
            }
            else
            {
                MarsConvertMp4Thread tmpThrd = new MarsConvertMp4Thread(strFileName);
                tmpThrd.startThrd();

            //new Thread(new ThreadStart(new Action(delegate ()
            //{
            //    Logger.Info("----Test-----", "Enter Thread.....");
            //    string strErrorx = "";
            //    MarsVedioRptMgr.ConvertVedioWith(strFileName, ref strErrorx);
            //    Logger.Info("----Test-----", "Exit Thread.....");
            //}))).Start();
            }
            //System.Threading.Tasks.Task objTask = new System.Threading.Tasks.Task(delegate { MarsVedioRptMgr.ConvertVedioWith(strFileName, ref strError); });
            //objTask.Start();
            Logger.Info("ConvertToMp4", "Task started...");
            //Caption4VideoMgr.ConvertAndAddCaptionsTask(strFileName, Caption4VideoMgr.gLstCaption);           

            //Caption4VideoMgr.ConvertAndAddCaptionsTask(@"C:\automationTest\Automation Workbooks\results\Vedio\[SophisDemo]-[SophisDemo]-[LP_0]_421.xesc", Caption4VideoMgr.gLstCaption);
        }

        private void vedioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //
        }

        private void StatusGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {

        }
        MarsTDetailMgrForm m_MgrForm = null;
        
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            ShowMgrForm();
        }

        private void ShowMgrForm()
        {
            if (m_MgrForm == null)
            {
                m_MgrForm = new MarsTDetailMgrForm();
                m_MgrForm.subMonitorFormHandler = this.SubMonitorsClosedImpl;
            }
            if (!m_MgrForm.Visible)
            {
                m_MgrForm.Show();
                m_MgrForm.WindowState = FormWindowState.Maximized;
            }
        }

        private void ReplayVedioFromSpecialItem(MarsXmlVedioIndex objTargetIdx, MarsTigerXmlReportItem objTargetPlayInfo)
        {
            if (objTargetPlayInfo == null) return;
            ShowMgrForm();
            if (m_MgrForm == null) return;
            m_MgrForm.ReplayItem(objTargetIdx, objTargetPlayInfo);
        }

        private void SubMonitorsClosedImpl()
        {
            m_MgrForm = null;
        }
#else
        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }
#endif
        private void StatusGrid_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

        }



        public void SkipCurrentStep()
        {
            Logger.Info("SkipCurrentStep", string.Format("come to SkipCurrentStep, with null?[{0}] ", this.StatusGrid.SelectedRows[0] == null));
            var currentRow = this.StatusGrid.SelectedRows[0];
            if (currentRow != null)
            {
                currentRow.Cells[1].Style.BackColor = SKIP_COLOR;
            }
        }

        public void ShuddownSystem()
        {
            Logger.Info("ShuddownSystem", "System.Windows.Forms.Application.Exit()");
            System.Windows.Forms.Application.Exit();
        }
        public void SetBatchMode()
        {
            IsBatchMode = true;
        }

        private void TestStepHintForm_Shown(object sender, EventArgs e)
        {
            System.Windows.Forms.Cursor.Position = new Point(Screen.PrimaryScreen.Bounds.X + Screen.PrimaryScreen.Bounds.Width / 2, Screen.PrimaryScreen.Bounds.Y + Screen.PrimaryScreen.Bounds.Height / 2);
            //for (int i=0;i<10;i++)
            //{
            //    Point pt = System.Windows.Forms.Cursor.Position;
            //    System.Windows.Forms.Cursor.Position = new Point(pt.X-10, pt.Y+10);
            //    Thread.Sleep(20);
            //}
            //TigerMarsUtil.LeftMouseClick(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
        }
    }

#if _VEDIO_TIGER_
#if DEBUG
    public class Caption4VideoMgr
#else
    internal class Caption4VideoMgr
#endif
    {
#if DEBUG
        public static List<Caption4Video> gLstCaption = null;
#else
        private static List<Caption4Video> gLstCaption = null;
#endif
        private static DateTime StartTime;
        public static void RefreshTimeSpan()
        {
            StartTime = DateTime.Now;
        }
        public static void RefreshCaptionList()
        {
            if (gLstCaption == null)
                gLstCaption = new List<Caption4Video>();
            gLstCaption.Clear();
            StartTime = DateTime.Now;
        }
        public static void AddOneCaptionStep(string strKeyWord,string strObject, string strRC, string strData)
        {
            if (gLstCaption == null) return;                
            Caption4Video objCaption = new Caption4Video();
            objCaption.Command = string.Format("Keyword:[{0}]    Object  :[{3}]    Rc     :[{1}]    Data   :[{2}]", strKeyWord, strRC,strData,strObject);            
            TimeSpan timeInternal = DateTime.Now - StartTime;
            objCaption.StartTime = string.Format("{0}:{1}:{2}.{3}", timeInternal.Hours.ToString("D2"), timeInternal.Minutes.ToString("D2"), 
                timeInternal.Seconds.ToString("D2"), timeInternal.Milliseconds.ToString("D7"));
            objCaption.TimeOfCaption = timeInternal;
            gLstCaption.Add(objCaption);
        }

        public static void ConvertAndAddCaptionsTask(string strFileName, List<Caption4Video> lstCaption)
        {
            Task.Factory.StartNew(delegate { ConvertVedioTask(strFileName, lstCaption); });
        }

        private static void ConvertVedioTask(string strFileName, List<Caption4Video> lstCaption)
        {
            MediaItem objItm = new MediaItem(strFileName);

            foreach (Caption4Video objCaption in lstCaption)
            {
                ScriptCommand oneCaption = new ScriptCommand();

                oneCaption.Type = objCaption.Type;
                oneCaption.Time = objCaption.TimeOfCaption;
                oneCaption.Command = objCaption.Command;
                objItm.ScriptCommands.Add(oneCaption);
            }
            using (Job objJob = new Job())
            {
                objItm.OutputFormat = new WindowsMediaOutputFormat();

                ((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile = new Microsoft.Expression.Encoder.Profiles.AdvancedVC1VideoProfile();
                ((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile.Size = new System.Drawing.Size(1024, 768);
                ((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile.AspectRatio = new System.Windows.Size(16, 9);
                ((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile.Bitrate = new ConstantBitrate(1000);

                ////((WindowsMediaOutputFormat)objItm.OutputFormat).VideoProfile.NumberOfEncoderThreads = 4;
                objItm.OutputFileName = "{OriginalFilename}.{DefaultExtension}";//                    string.Format("{0}.wmv", strFileName);

                objJob.MediaItems.Add(objItm);
                objJob.OutputDirectory = Path.GetDirectoryName(strFileName);
                objJob.Encode();
            }
        }
    }

    /***
    *
    *
    * to Generate Caption Xml file
    * One Node of Xml is :
    * <ScriptCommand
        Time="00:00:59.3500000"
        Type="caption"
        Command="Start Sophis\ndata&#xA;abdsjfldjl" />
    */
#if DEBUG
    public class Caption4Video
#else
    internal class Caption4Video
#endif
    {
    #region property
        public string StartTime { get; set; }
        public TimeSpan TimeOfCaption { get; set; }
        public string Command
        {
            get { return command; }
            set
            {
                command = value.Replace("&", "&amp;").Replace("\'", "&apos;").Replace("\"", "&quot;").Replace(">", "&gt;").Replace("<","&lt;").Replace("\n", "&#xA;");
                //if (value.Contains("\n") ||value.Contains("\r"))
                //{
                //    command = value.Replace("\n", "&#xA;");
                //    command = value.Replace("\r", "&#xA;");
                //}
                //else
                //{
                //    command = value;
                //}
            }
        }
        public string Type {
            get { return strType; }            
        }
    #endregion //Property

        private string command;
        private string strType = "caption";
        
        public string ToScriptCommand()
        {
            const string cnst_node_name = "ScriptCommand";
            return string.Format("<{0} Time=\"{1}\" Type=\"{2}\" Command=\"{3}\" />", cnst_node_name, StartTime, strType, command);
        }

        public static string ToScriptCommands(List<Caption4Video> lstCaptions)
        {
            StringBuilder strBuild = new StringBuilder();
            strBuild.Append("<?xml version=\"1.0\" encoding=\"utf-16\"?>");
            strBuild.Append("<ScriptCommands>");
            foreach(Caption4Video oneItem in lstCaptions)
            {
                strBuild.Append(oneItem.ToScriptCommand());
            }
            strBuild.Append("</ScriptCommands>");

            //Encoding unicode = Encoding.Unicode;
            //Encoding utf16 = Encoding.Unicode;
            //byte[] unicodeBytes = unicode.GetBytes(strBuild.ToString());

            //byte[] utf16Bytes = Encoding.Convert(unicode,
            //                                     utf16,
            //                                     unicodeBytes);

            return strBuild.ToString();
        }
    }
#endif


}
