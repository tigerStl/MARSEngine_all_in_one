using com.Mars.Constants;
using com.Mars.MarsTestingFrame;
using Mars.Delegate;
using Mars.Utility;
using Mars.VideoViewer;
using Mars.ViewModel;
using MarsTestFrame.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using Route2NSEx.src.Marquis.systemUtil;
using Mars.Views.baseView;
using Mars.Helpers;
using System.Collections;
using System.Data;
using System.Xml;

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for DashboardEditControl.xaml
    /// </summary>
    public partial class StoryboardEditControl :
        MarsBaseViewControl
        , INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardEditControl));

        MarsVideoViewer viewer;
        
        public StoryboardEditControl(string strDBIdx, string projectName, string storyboardName, long projectId, long storyboardId, MarsStoryboardTreeView assignedTreeNode = null)
        {
            InitializeComponent();
            KeepKeyValue(projectName, storyboardName, projectId, storyboardId);
            this.DataContext = StoryboardCache.getSBCall(strDBIdx, projectName, storyboardName, projectId, storyboardId, assignedTreeNode);
            InitialPopupProperties();

            Title = string.Format("SB:[{0}]", storyboardName); ;
        }

        public string StoryboardDescription
        {
            get {
                if (DataContext is StoryboardColl)
                {
                    return ((StoryboardColl)DataContext).StoryBoardDescription;
                }
                return "";
            }
        }

        public void HideControlPanel()
        {
            this.controlPanel.Visibility = Visibility.Collapsed;
           
            this.contolPanelColumn.Width = GridLength.Auto;
            this.gridPanel.Margin = new Thickness(0, 0, 5, 0); 
        }
        
        private void KeepKeyValue(string projectName, string storyboardName, long projectId, long storyboardId)
        {
            _CurrentProjectName = projectName;
            _CurrentStoryboardName = storyboardName;
            _CurrentProjectId = projectId;
            _CurrentStoryboardID = storyboardId;
        }

        private void cbTestSuite_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void InitialPopupProperties()
        {
            this.AvailableApplications = ((StoryboardColl)DataContext).AssignedApplication;
        }

        private void cbDataSetName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void storyboardGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender == null) return;
            StoryboardDataGrid dg = (StoryboardDataGrid)sender;

            if (dg.Items.Count > 0) {
                //dg.SelectedIndex = 0;      
                if ((dg.SelectedItem == null))
                {
                    dg.SelectedItem = dg.Items[0];
                    SignalParent(this, new System.Windows.RoutedEventArgs(), "SHOW");
                }
            }
            //SignalParent(this, new System.Windows.RoutedEventArgs());
        }

        private void storyboardGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender == null) return;
            StoryboardDataGrid dg = (StoryboardDataGrid)sender;

            StoryboardColl sbColl = StoryboardCache.currentSBColl;
            if (sbColl.SelectedStoryboardRows == null) return;
            if (sbColl.SelectedStoryboardRows.Count == 0) return;
            StoryboardEditViewModel sbvm = sbColl.SelectedStoryboardRows[0];
            string testCaseName =  sbvm.TestCaseName;


            SignalParent(this, new System.Windows.RoutedEventArgs(), "SHOW");

            //StoryboardEditViewModel model = dg.Items.CurrentItem.
            //Consol.WriteLine("storyboardGrid_MouseDoubleClick");
        }

#region RoutedEvent
        public static readonly RoutedEvent TapEvent =
            EventManager.RegisterRoutedEvent(
                        "Tap",
                        RoutingStrategy.Bubble,
                        typeof(RoutedEventHandler),
                        typeof(StoryboardEditControl)
                        );

        public static readonly RoutedEvent TapEditEvent =
           EventManager.RegisterRoutedEvent(
                       "TapEdit",
                       RoutingStrategy.Bubble,
                       typeof(RoutedEventHandler),
                       typeof(StoryboardEditControl)
                       );

        // Provide CLR accessors for the event
        public event RoutedEventHandler Tap
        {
            add { AddHandler(TapEvent, value); }
            remove { RemoveHandler(TapEvent, value); }
        }

        public event RoutedEventHandler TapEdit
        {
            add { AddHandler(TapEditEvent, value); }
            remove { RemoveHandler(TapEditEvent, value); }
        }

        private void SignalParent(object sender, System.Windows.RoutedEventArgs e, string action)
        {
            if (action.Equals("SHOW"))
                this.RaiseEvent(new RoutedEventArgs(TapEvent, this));

            else if (action.Equals("EDIT"))
                this.RaiseEvent(new RoutedEventArgs(TapEditEvent, this));
        }
#endregion


#region Data provoided for outside
        public ObservableCollection<MarsKeyValues<string, string>> AvailableApplications
        {
            get { return _availableApplciations; }
            set {
                _availableApplciations = value;
                OnPropertyChangedEx("AvailableApplication");
            }
        }
        private ObservableCollection<MarsKeyValues<string, string>> _availableApplciations;

        private ObservableCollection<MarsKeyValues<string, string>> _unInstalledApplications;
        public ObservableCollection<MarsKeyValues<string, string>> UnInstalledApplications
        {
            get { return this._unInstalledApplications; } 
            set
            {
                this._unInstalledApplications = value;
                
                OnPropertyChangedEx("UnInstalledApplications");
            } 
        }

        internal void PerformPropertyEvent(string strPropertyName)
        {
            switch(strPropertyName)
            {
                case "UnInstalledApplications":
                    this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(delegate () {
                    ObservableCollection<MarsKeyValues<string, string>> lstApp = new ObservableCollection<MarsKeyValues<string, string>> {
                        new MarsKeyValues<string,string> ("x","def" ),
                        new MarsKeyValues<string,string> ("abc1x","def1" )
                    };
                    this.UnInstalledApplications = lstApp;
                    OnPropertyChangedEx(strPropertyName); })
                ); 
                    break;
                
            }
            

        }
#endregion

        //private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardEditControl));

        public OnRibbonTestApplicationsReady RibbonTestApplicationReadyHandler;

        public static readonly RoutedEvent OnStoryBoardIdChangeEvent =
            EventManager.RegisterRoutedEvent(
            "OnStoryBoardIdChange",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(StoryboardEditControl)
            );
        public event RoutedEventHandler OnStoryBoardIdChange
        {
            add { AddHandler(OnStoryBoardIdChangeEvent, value); }
            remove { RemoveHandler(OnStoryBoardIdChangeEvent, value); }
        }

        private void RaiseOnStoryBoardIdChangeEvent(object sender, System.Windows.RoutedEventArgs e)
        {
            //Logger.logBegin("RaiseOnStoryBoardIdChangeEvent");
            this.RaiseEvent(new RoutedEventArgs(OnStoryBoardIdChangeEvent, sender));
            //Logger.logEnd("RaiseOnStoryBoardIdChangeEvent");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChangedEx(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private long? _CurrentStoryboardID;
        public long? CurrentStoryBoardID
        {
            get { return this._CurrentStoryboardID; }
            set
            {
                if (_CurrentStoryboardID != value)
                {
                    _CurrentStoryboardID = value;
                    RaiseOnStoryBoardIdChangeEvent(_CurrentStoryboardID, new RoutedEventArgs());
                    OnPropertyChangedEx("CurrentStoryBoardID");
                }
            }
        }

        #region Data for communit with main window
        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty OnTestApplicationIdChange = DependencyProperty.Register("CurrentTestApplicationId", typeof(MarsKeyValues<string, string>), typeof(StoryboardEditControl));
        public MarsKeyValues<string, string> CurrentTestApplicationId
        {
            get { return (MarsKeyValues<string, string>)GetValue(OnTestApplicationIdChange); }
            set
            {
                SetValue(OnTestApplicationIdChange, value);
            }
        }

        #endregion //Data for communit with main window

        //private StoryboardColl objDataProvider = null;
        internal void TestCurrentStoryBoard()
        {
            //Logger.logBegin("TestCurrentStoryBoard");
            string strError = "";
            if (!(this.DataContext is StoryboardColl))
            {
                //Logger.Error("TestCurrentStoryBoard", strError=ERROR_INFO.GET_ERROR_STR(ERROR_CODE._STORYBOARD_ERROR_DATACONTEXT_IS_NOT_STORYBOARDCOLL));
                MarsSystemUtilty.ShowSpecialMessage(this, strError);
                return;
            }
            StartTest(CurrentTestApplicationId.MValue, CurrentTestApplicationId.MKey);
            //objDataProvider = (StoryboardColl)this.DataContext;
            ///// minimize main window
            ///// 
            ////this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(delegate {
            //    //Application.Current.MainWindow.WindowState = WindowState.Minimized;
            //    //objDataProvider.StopTest();
            //    objDataProvider.StartTest(CurrentTestApplicationId.MValue, CurrentTestApplicationId.MKey);
            ////}));


        }

        public string _CurrentProjectName;

        public string _CurrentStoryboardName;
        public long _CurrentProjectId;
        //private static MarsTestFrameMain MarsTestingFrame = new MarsTestFrameMain();
        //private string _storyBoardName;

        internal void StartTest(string strApplicationShortName, string strApplicationId)
        {
            //Logger.logBegin("StartTest");

            //MarsTestingFrame.StopService();
            //MarsTestingFrame.CurrentTestProjectName = _CurrentStoryboardName;
            //MarsTestingFrame.CurrentTestApplicationShortName = strApplicationShortName;
            //MarsTestingFrame.RunTestBatchFileByThread(_CurrentStoryboardName, FrameWorkStartMode.FWSM_Normal, _CurrentStoryboardID + "", strApplicationId);
        }

        private void btnVideo_Click(object sender, RoutedEventArgs e)
        {

            StoryboardColl sbColl = StoryboardCache.currentSBColl;
            if (sbColl.SelectedStoryboardRows.Count == 0)
            {
                MarsSystemUtilty.ShowSpecialMessage(this, "Please select a TestCase in the Storyboard.");
                return;
            }

            string indexFileName = GetIdxFileName();
            Logger.Info("btnVideo_Click",string.Format("Path of the idx file:[{0}]", indexFileName));
            string videoFileName = GetVideoFileName();
            Console.WriteLine(indexFileName);
            Console.WriteLine(videoFileName);

            if (File.Exists(indexFileName) == false)
            {
                MarsSystemUtilty.ShowSpecialMessage(this, "Index file for this Test Case does not exist.");
                return;
            }

            if (File.Exists(indexFileName) == false)
            {
                MarsSystemUtilty.ShowSpecialMessage(this, "Video file for this Test Case does not exist.");
                 return;
            }
#if Alex_debug
            viewer = new MarsVideoViewer(@"C:\Users\Alex\Documents\visual studio 2013\Projects\MarsVideoViewer\MarsVideoViewer2\Data\test.wmv",
                                        @"C:\Users\Alex\Documents\visual studio 2013\Projects\MarsVideoViewer\MarsVideoViewer2\Data\test.mti");
#else
            viewer = MarsVideoViewer.GetInstance(videoFileName, indexFileName);
#endif
            viewer.Show();
        }

        private string GetVideoFileName()
        {
            StoryboardColl sbColl = StoryboardCache.currentSBColl;
            if (sbColl.SelectedStoryboardRows.Count == 0) return null;
            StoryboardEditViewModel sbvm = sbColl.SelectedStoryboardRows[0];

            string strPath = GetVideoPath();

            return string.Format("{0}\\[{1}]_[{2}]_[{3}]_LP[{4}].wmv",
                                strPath,
                                sbvm.StoryboardDetailId, //sbvm.TestSuiteId,
                                sbvm.TestSuiteName.Replace(' ', '_'),
                                sbvm.TestCaseName.Replace(' ', '_'),
                                0
                                 );
        }

        ///////////////////////////////////
        // File and path utils
        internal string GetIdxFileName()
        {
            StoryboardColl sbColl = StoryboardCache.currentSBColl;
            if (sbColl.SelectedStoryboardRows.Count == 0) return null;
            StoryboardEditViewModel sbvm = sbColl.SelectedStoryboardRows[0];
            
            string strPath = GetIdxPath();

            return string.Format("{0}\\[{4}]_[{1}]_[{2}]_LP[{3}].mti", strPath,
                sbvm.TestSuiteName.Replace(' ', '_'),
                sbvm.TestCaseName.Replace(' ','_'), 
                0,  // loop
                //sbvm.TestSuiteId
                sbvm.StoryboardDetailId
                ); // relyId
        }

        internal static string GetIdxPath()
        {
#if _NO_C_DRIVER_WRITE
            string strLocation = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");            
            string strPath = System.IO.Path.Combine(strLocation, ".\\Results\\Index");
#else
            string strLocation = typeof(StoryboardEditControl).Assembly.Location;
            strLocation = TigerMarsUtil.GetPathWithoutFileName(strLocation);
            string strPath = System.IO.Path.Combine(strLocation, "..\\Results\\Index");
#endif
            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }
            return strPath;
        }

        internal static string GetVideoPath()
        {
#if _NO_C_DRIVER_WRITE
            string strLocation = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
            string strPath = System.IO.Path.Combine(strLocation, ".\\Results\\Video\\wmv");
#else
            string strLocation = typeof(StoryboardEditControl).Assembly.Location;
            strLocation = TigerMarsUtil.GetPathWithoutFileName(strLocation);
            string strPath = System.IO.Path.Combine(strLocation, "..\\Results\\Video\\wmv");
#endif
            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }
            return strPath;
        }

        internal int DelTestHisData(bool? isDelBaseline, bool? isDelNoneBaseline,ref string strError)
        {
            Logger.Info("DelTestHisData",string.Format("isDelBaseLine:[{0}] ,isDelNoneBaseline:[{1}]", isDelBaseline??false,isDelNoneBaseline??false));

            if (!(this.DataContext is StoryboardColl))
            {
                strError = string.Format("DataContext should be StoryboardColl, but it is :[{0}]", this.DataContext==null?"null":this.DataContext.GetType().ToString());
                return -1;
            }
            return  ((StoryboardColl)(this.DataContext)).DelTestHisData(isDelBaseline,isDelNoneBaseline,ref strError);
            //return (int)ERROR_CODE._NO_ERROR;
        }

        private void storyboardGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender == null) return;
            StoryboardDataGrid dg = (StoryboardDataGrid)sender;

            StoryboardColl sbColl = StoryboardCache.currentSBColl;
            if (sbColl.SelectedStoryboardRows.Count == 0) return;
            StoryboardEditViewModel sbvm = sbColl.SelectedStoryboardRows[0];

            SignalParent(this, new System.Windows.RoutedEventArgs(), "EDIT");
        }

        private void MenuItem_PreviewKeyUp(object sender, RoutedEventArgs e)
        {
            SignalParent(this, new System.Windows.RoutedEventArgs(), "EDIT");
        }

        /// <summary>
        /// 显示选中行的Test Result list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowTestResultAsDetail(object sender, RoutedEventArgs e)
        {
            Logger.logBegin("ShowTestResultAsDetail");
            try
            {
                DataGrid objDG = (DataGrid)((ContextMenu)(((MenuItem)sender).Parent)).PlacementTarget;
                IList<DataGridCellInfo> lstCells = objDG.SelectedCells;
                if (lstCells == null)
                {
                    Logger.Warnning("ShowTestResultAsDetail", "No Row is selected");
                    return;
                }
                var oRow = lstCells[0].Item;
                DataGridCell oC = (DataGridCell)lstCells[0].Column.GetCellContent(lstCells[0].Item).Parent;
                DataGridRow  oR = (DataGridRow)TreeViewHelper.FindParent<DataGridRow>(oC);
                if (oR==null)
                {
                    Logger.Warnning("ShowTestResultAsDetail", "No Row is selected, oR==null");
                    return;
                }
                ///算法：
                /// 1，获得test story board detail id，
                /// 2，通过Detail Id 获得 所有的 dataset的History信息
                /// 3，
                /// 
                StoryboardEditViewModel dtModel = (StoryboardEditViewModel)lstCells[0].Item;
                bool isOk = dtModel.LoadDetailInfo();
                if (isOk)
                {
                    oR.DetailsVisibility = Visibility.Visible;
                }
                Logger.Info("ShowTestResultAsDetail", oC.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error("ShowTestResultAsDetail", string.Format("Exception:[{0}]",ex.Message),ex);
            }
        }



        private void cbAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public IEnumerable<DataGridRow> GetDataGridRows(DataGrid grid)
        {
            var itemsSource = grid.ItemsSource as IEnumerable;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (null != row) yield return row;
                
            }
        }

        private void storyboardGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Logger.logBegin("storyboardGrid_PreviewKeyDown");            

            if ( e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            { 
                if (this.storyboardGrid.IsEditing())
                {
                    if ((this.DataContext == null) || (!(this.DataContext is StoryboardColl)))
                    {
                        return;
                    }
                    ///判断是不是new row
                    /// 
                    //if (((StoryboardColl)this.DataContext).IsSelecteRowEmptyRow())
                    {
                        e.Handled = true;
                        ((StoryboardColl)this.DataContext).PasteSelectedRowsCommand.Execute(null);
                    }
                }
                return;
            }

            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                
                //if (this.storyboardGrid.GetEditingRow() != null) return;

                //var rows = GetDataGridRows(this.storyboardGrid).ToList();
                //DataTable dt = new DataTable();
                //dt.Columns.Clear();
                //foreach (var c in this.storyboardGrid.Columns)
                //{
                //    dt.Columns.Add(c.Header.ToString());
                //}
                //foreach(var itm in rows)
                //{
                //    if (itm == null) continue;
                //    if (!itm.IsSelected) continue;            
                //    var cellsOfCurrentRow = MarsBaseGridViewControl.GetVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>((Visual)itm);
                //    var oneRow = dt.NewRow();
                //    for (int i = 0; i < dt.Columns.Count; i++)
                //    {
                //        DataGridCell cell = (DataGridCell)cellsOfCurrentRow.ItemContainerGenerator.ContainerFromIndex(i);

                //        if (cell == null)
                //            oneRow[i] = null;
                //        else {
                //            TextBlock tx = MarsBaseGridViewControl.GetVisualChild<TextBlock>(cell);
                //            if (tx!=null)
                //            oneRow[i] = tx.Text;
                //            else
                //                oneRow[i] = null;
                //        }
                //        //oneRow[i] = cellsOfCurrentRow.Items[i];
                //    }
                //    dt.Rows.Add(oneRow);
                //}
                //DataSet ds = new DataSet();
                //ds.Tables.Add(dt);

                ////XmlDocument xmld = new XmlDocument();
                ////xmld.LoadXml(ds.GetXml());


                //Clipboard.SetData(DataFormats.StringFormat, ds.GetXml());

               // var d = LogicalTreeHelper.FindLogicalNode(this, "storyboardGrid");
               //this.storyboardGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
               // DataGrid objDG = (DataGrid)sender;
               // if (objDG.SelectedItems == null) return;
               // ///需要将选中的数据封装成 tab 格式，以便excel 能够导入，问题是
               // /// 
               // if ((this.DataContext == null)||(!(this.DataContext is StoryboardColl)))
               // {
               //     Logger.Error("storyboardGrid_PreviewKeyDown","Error for assert (this.DataContext == null)||(!(this.DataContext is StoryboardColl))");
               //     ViewModelBase.HintByMessageBox("Data type Error for story board.", "Error");
               //     return;
               // }
               // string strError = "";
               // bool isOk = ((StoryboardColl)DataContext).CopySelectedRows2WindowsClipboard(ref strError); 
               // if (!isOk)
               // {
               //     ViewModelBase.HintByMessageBox(string.Format("Error when copy values to Clipboard:\r\n{0}", strError),"Hint");
               // }
               // else
               // {
               //     ViewModelBase.HintByMessageBox(string.Format("Selected storyboard rows are copied.", strError), "Hint");
               // }
                return;
            }
        }

        private void CanCopyExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        

        private void CanPasteExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void PasteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            Logger.logBegin("PasteCommand");
            try
            {
                string strFromClip = (string)Clipboard.GetData(DataFormats.Text);
                if (string.IsNullOrEmpty(strFromClip))
                {
                    e.Handled = true;
                    return;
                }
                if (Clipboard.GetData(DataFormats.CommaSeparatedValue)!=null)
                {
                    strFromClip = Clipboard.GetData(DataFormats.CommaSeparatedValue) as string;
                    if (strFromClip.ToUpper().StartsWith(StoryboardColl.GetStandardCVSHeader().ToUpper()))
                    {

                        StoryboardColl storyModel = this.DataContext as StoryboardColl;
                        string strError = "";
                        if (!storyModel.DealWithCSVFormatPaste(strFromClip, ref strError))
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
                    if (ds.Tables.Count<=0)
                    {
                        ViewModelBase.HintByMessageBox(string.Format("no table information from xml:\r\n{0}\r\n......", strFromClip.Substring(1, 100))) ;
                        return;
                    }
                    StoryboardColl storyModel = this.DataContext as StoryboardColl;
                    if (storyModel == null) return;
                    string strError = "";
                    if (!storyModel.DealPasteByDataTable(ds.Tables[0], ref strError))
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Can't paste from MARS Inner copy with error:\r\n[{0}]",strError));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Clipboard is not formatted by Dataset or CVS for Storyboard, \r\n if text copied from Excel as CVS, standard Mars header must be included. \r\nwith exception:[{0}]",ex.Message));
                    return;
                }
        }
            finally
            {
                Logger.logEnd("PasteComand");
            }
            
        }

        public static string DataTableToCSV(DataTable datatable, char seperator)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < datatable.Columns.Count; i++)
            {
                sb.Append(datatable.Columns[i]);
                if (i < datatable.Columns.Count - 1)
                    sb.Append(seperator);
            }
            sb.AppendLine();
            foreach (DataRow dr in datatable.Rows)
            {
                for (int i = 0; i < datatable.Columns.Count; i++)
                {
                    sb.Append(dr[i].ToString());

                    if (i < datatable.Columns.Count - 1)
                        sb.Append(seperator);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void CopyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.storyboardGrid.GetEditingRow() != null)
            {
                ApplicationCommands.Copy.Execute(null, null);
                e.Handled = true;
                return;
            }
            DataTable dt = new DataTable();
            dt.Columns.Clear();
            foreach (var c in this.storyboardGrid.Columns)
            {
                dt.Columns.Add(c.Header.ToString());
            }

            
            string strError = "";
            foreach (var itm in storyboardGrid.SelectedItems)
            {
                if (itm == null) continue;
                StoryboardEditViewModel oneRow = itm as StoryboardEditViewModel;
                if (oneRow == null) continue;
                var oneDataRow = dt.NewRow();
                if (!oneRow.ToStandardDataRow(oneDataRow,ref strError))
                {
                    ViewModelBase.HintByMessageBox(string.Format("Can't convert to standard Table with Error \r\n{0}",strError));
                    return ;
                }
                dt.Rows.Add(oneDataRow);
            }
            //有可能有些row因为效率问题，wpf没有产生‘故而不用该块代码 
            //var rows = GetDataGridRows(this.storyboardGrid).ToList();
            //foreach (var itm in rows)
            //{
            //    if (itm == null) continue;
            //    if (!itm.IsSelected) continue;
            //    var cellsOfCurrentRow = MarsBaseGridViewControl.GetVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>((Visual)itm);
            //    var oneRow = dt.NewRow();
            //    for (int i = 0; i < dt.Columns.Count; i++)
            //    {
            //        DataGridCell cell = (DataGridCell)cellsOfCurrentRow.ItemContainerGenerator.ContainerFromIndex(i);

            //        if (cell == null)
            //            oneRow[i] = null;
            //        else
            //        {
            //            TextBlock tx = MarsBaseGridViewControl.GetVisualChild<TextBlock>(cell);
            //            if (tx != null)
            //                oneRow[i] = tx.Text;
            //            else
            //                oneRow[i] = null;
            //        }
            //        //oneRow[i] = cellsOfCurrentRow.Items[i];
            //    }
            //    dt.Rows.Add(oneRow);
            //}

            if (copyMode.IsChecked.HasValue ? copyMode.IsChecked.Value : false)
            {
                //cvs mode 
                string strData = DataTableToCSV(dt, ',');
                string strTab = DataTableToCSV(dt, '\t');

                var dataObject = new System.Windows.DataObject();

                // Add tab-delimited text to the container object as is.
                dataObject.SetText(strTab);

                // Convert the CSV text to a UTF-8 byte stream before adding it to the container object.
                var bytes = System.Text.Encoding.UTF8.GetBytes(strData);
                var stream = new System.IO.MemoryStream(bytes);
                dataObject.SetData(System.Windows.DataFormats.CommaSeparatedValue, stream);

                // Copy the container object to the clipboard.
                System.Windows.Clipboard.SetDataObject(dataObject, true);

                //Clipboard.SetData(DataFormats.Text, strData);
            }
            else
            {
                DataSet ds = new DataSet();
                ds.Tables.Add(dt);

                //XmlDocument xmld = new XmlDocument();
                //xmld.LoadXml(ds.GetXml());
                //csv mode
                //Clipboard.SetData(DataFormats.Text, MarsClipBoard.DataTable2Cvs(dt));
                Clipboard.SetData(DataFormats.Text, ds.GetXml());
            }
            e.Handled = true;
            //ApplicationCommands.Copy.Execute(null, storyboardGrid);

        }
        private void storyboardGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            //Logger.logBegin("storyboardGrid_GotFocus");
            //DataGrid dbG = sender as DataGrid;
            //if (dbG == null) return;
            //if (dbG.CurrentCell == null) return;

            //int iSelectedIdx = dbG.SelectedIndex;
            //if (iSelectedIdx < 0) return;
            //var oneRow = dbG.ItemContainerGenerator.ContainerFromIndex(iSelectedIdx);

            //DataGridRow aRow = oneRow as DataGridRow;
            //if (aRow == null) return ;
            //if (aRow.IsEditing) return;

            //System.Windows.Controls.Primitives.DataGridCellsPresenter cellsRepresenter = MarsBaseGridViewControl.GetVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(aRow);
            //for (int i=0;i< (cellsRepresenter.ItemContainerGenerator.Items==null?-1: cellsRepresenter.ItemContainerGenerator.Items.Count);i++)
            //{
            //    var itm = cellsRepresenter.ItemContainerGenerator.Items[i];
            //    if (itm == null) continue;
            //    var oneCellV = cellsRepresenter.ItemContainerGenerator.ContainerFromIndex(i) ;
            //    DataGridCell oneCell = oneCellV as DataGridCell;
            //    if (oneCell == null) continue;
            //    if (oneCell.IsEditing) return;
            //}
            //dbG.BeginEdit();
            //if (aRow.col)
        }

        private void storyboardGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

        }

        private void storyboardGrid_KeyUp(object sender, KeyEventArgs e)
        {
            DataGrid dbStoryboard = sender as DataGrid;
            if (dbStoryboard == null) return;
            if (!(e.Key == Key.Tab) || (e.Key == Key.Up) || (e.Key == Key.Down)||(e.Key==Key.Up)||(e.Key==Key.Down)) return;
            Key currentKey = e.Key;
            ///对于上述任意按键，均需要使grid进入edit状态
            ///             
            if ((dbStoryboard.Items == null)|| (dbStoryboard.Items.Count == 0)) return;
            if ((dbStoryboard.SelectedItem == null))
            {
                dbStoryboard.SelectedItem = dbStoryboard.Items[0];
            }
            try
            {
                DataGridRow o = (DataGridRow)dbStoryboard.ItemContainerGenerator.ContainerFromIndex(dbStoryboard.SelectedIndex);
                if (o.IsEditing) return;
                System.Windows.Controls.Primitives.DataGridCellsPresenter grdCellPrnt =
                    MarsBaseGridViewControl.GetVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>((Visual)o);
                int iCnt = grdCellPrnt.Items == null ? -1 : grdCellPrnt.Items.Count;
                DataGridCell cell2nd = null;
                for (int i=0;i< iCnt;i++)
                {
                    var oneCellV = grdCellPrnt.ItemContainerGenerator.ContainerFromIndex(i);
                    DataGridCell oneCell = oneCellV as DataGridCell;
                    if (oneCell == null) continue;
                    if (i == 1)
                        cell2nd = oneCell;
                    if (oneCell.IsEditing) return;
                }
                cell2nd.IsEditing = true;
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Logger.Error("storyboardGrid_KeyUp",string.Format("Exception:[{0}]",ex.Message),ex);
            }           
            
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Logger.logBegin("CheckBox_Checked",string.Format("value:[{0}]",e));
            CheckBox objChck = sender as CheckBox;
            if (objChck == null) return;

            if (objChck.IsChecked.HasValue? objChck.IsChecked.Value:false)
            {
                storyboardGrid.SelectAll();
            }else
            {
                storyboardGrid.UnselectAll();
            }
        }

        private void popupImportExportMenu(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsEnabled = true;
            (sender as Button).ContextMenu.PlacementTarget = (sender as Button);
            (sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            (sender as Button).ContextMenu.IsOpen = true;
        }

        private void storyboardGrid_MouseRightButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            
        }

        //private void storyboardGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        //{
        //    Logger.logBegin("storyboardGrid_CopyingRowClipboardContent",e==null?"N/A":e.ToString());
        //    DataGrid objDG = (DataGrid)sender;
        //    List<DataGridClipboardCellContent> lstCnt = e.ClipboardRowContent;

        //}
    }
}
