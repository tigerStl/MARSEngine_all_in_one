using Mars.Business;
using Mars.DataLayer;
using Mars.Dialog;
using Mars.Dto;
using Mars.ViewModel;
using Mars.Views.baseView;
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
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

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for TestCaseEdit.xaml
    /// </summary>
    public delegate void OnDataSetListIsReady(IList<KeyValuePair<Int64,string>> lstDataSetInfo) ;
    public partial class TestCaseEdit :
        MarsBaseGridViewControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestCaseEdit));

        public OnDataSetListIsReady dataSetIsReadImpl = null;

        string _testCaseName;
        string _currentDataSheetName;

        long currentTestCaseId;
        public long TestCaseId
        {
            get { return currentTestCaseId; }
        }
        private long datasheetId;
        public long DataSheetId
        {
            get { return datasheetId; }
        }

        private string currentDBIdx = null;
        //public TestCaseEdit(string testCaseName, long dataSheetId, bool isSharedData)
#if v_16AndUp
        protected List<long?> TestcaseAppliedAppIds = null;
        public TestCaseEdit(string strDBIdx, long testCaseId, long dataSheetId, bool isSharedData, 
            OnAddTestStepUnitObjEvent addTestStepUnitImpl,bool useCache = true, List<long?> lstAssignedAppIds=null)
        {
            TestcaseAppliedAppIds = lstAssignedAppIds;
            currentDBIdx = strDBIdx;
#else
        public TestCaseEdit(long testCaseId, long dataSheetId, bool isSharedData, bool useCache = true)
        {
#endif

            InitializeComponent();
            currentTestCaseId = testCaseId;
            this.datasheetId = dataSheetId;
            //_testCaseName = GetTestCaseNameById(testCaseId); /// delete reason: re get it in COL data context
            // AF     this.DataContext = new VMColl(testCaseName);
            //long lTm;
            //Logger.Info("TestCaseEdit", string.Format("----PerformanceTest:{0},testCaseId:[{1}],dataSheetId:[{2}]", lTm = DateTime.Now.Ticks, currentTestCaseId, dataSheetId));
#if v_16AndUp
            //if (useCache)
            //    this.DataContext = VMCollCash.getVMCall(currentTestCaseId, dataSheetId, isSharedData);
            //else
            var tmpObj = new VMColl(strDBIdx, currentTestCaseId, dataSheetId, isSharedData, null, addTestStepUnitImpl);
            
            _testCaseName = tmpObj.TestCaseName;
            _currentDataSheetName = tmpObj.CurrentDataSheetName;
#else
            if (useCache)
                this.DataContext = VMCollCash.getVMCall(currentTestCaseId, dataSheetId, isSharedData);
            else
                this.DataContext = new VMColl(currentTestCaseId, dataSheetId, isSharedData);
#endif
            Title = string.Format("TC:[{0}] DS:[{1}]",string.IsNullOrEmpty(_testCaseName)?"N/A":(_testCaseName.Length>10?(_testCaseName.Substring(0,7)+"..."):_testCaseName),_currentDataSheetName);
            this.DataContext = tmpObj;
            tmpObj.AssignedGuiObj = this;

            //double dx;
            //Logger.Info("TestCaseEdit", string.Format("----PerformanceTest cost:{0},seconds:[{1}]", dx=DateTime.Now.Ticks-lTm,dx/10000000.0));
        }

        private string GetTestCaseNameById(long testCaseId)
        {
            T_TEST_CASE_SUMMARYDTO objTC = B_TEST_CASE.GetTestCaseInfoByName(testCaseId);
            if (objTC == null) return "";
            return objTC.TEST_CASE_NAME;
        }
        

        public void PopulateItemList()
        {
            if (dataSetIsReadImpl != null)
            {
                /// get data list
                /// 
                IList<KeyValuePair<Int64, string>> dataList = new List<KeyValuePair<Int64, string>>();

                KeyValuePair<Int64, string> kvp = new KeyValuePair<long, string>(4, "Alex");
                dataList.Add(kvp);
                this.dataSetIsReadImpl(dataList);
            }
        }

        private void keywordGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            //DependencyObject dep = (DependencyObject)e.OriginalSource;
            //while ((dep != null) && !(dep is DataGridCell))
            //{
            //    dep = VisualTreeHelper.GetParent(dep);
            //}

            //if (dep == null)
            //    return;

            //if (dep is DataGridCell)
            //{
            //    DataGridCell cell = dep as DataGridCell;
            //    if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            //    {
            //        if (!cell.IsFocused)
            //        {
            //            cell.Focus();
            //            keywordGrid.BeginEdit();
            //        }                    
            //    }
            //}

        }

        private void cbObjectName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.ComboBox cb = (System.Windows.Controls.ComboBox)sender;
            if (cb.SelectedItem == null) return;
            ObjectName o = (ObjectName)cb.SelectedItem;
            
            if (cb.IsDropDownOpen)
            {
                VMColl v = (VMColl)this.DataContext;
                if (v.SelectedTestCases == null) return;
                if (v.SelectedTestCases.Count == 0)
                {
                    return;
                }
                if (v.IsEmptySettingRow())
                {
                    v.DeleteRelatedData();
                    v.PopulateDataFromObjectPool();
                }
            }
        }

        private void cbKeyword_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Console.WriteLine(sender.GetType());
            //Console.WriteLine("cbKeyword_SelectionChanged-" + e.OriginalSource.ToString() ) ;
            System.Windows.Controls.ComboBox cb = (System.Windows.Controls.ComboBox)sender;
            //if (cb.HighlightedElement == null)
            //Console.WriteLine(cb.IsDropDownOpen);
            if (cb.SelectedItem == null) return;
            if (cb.IsDropDownOpen)
            {
                Keyword kw = (Keyword)cb.SelectedItem;
                string keywordName = kw.KeywordName;
                VMColl v = (VMColl)this.DataContext;
                if (v.SelectedTestCases == null) return;
                if (v.SelectedTestCases.Count == 0)
                {
                    return;
                }

                v.DeleteRelatedData();

                v.PopulateDataFromObjectPool();

                if (keywordName.Equals("PegWindow"))
                {
                    v.DeleteRowsAfterPegWindow();
                }
            }

            //Need to change object selection value based on keyword selected
            //cbObjectName
            //keywordGrid
            ////DataTable dt = new DataTable();
            ////dt.Columns.Add("RunOrder", typeof(int));
            ////dt.Columns.Add("KeyWordId", typeof(int));
            ////dt.Columns.Add("KeyWord", typeof(string));
            ////dt.Columns.Add("ObjectId", typeof(int));
            ////dt.Columns.Add("ObjectName", typeof(string));
            ////int iRowCount=0;
            ////int currentRow;
            ////string lastPegWindowKeyWord = "";
            ////long lastMasterObjectId = 0;
            ////string lastMasterObjectName = "";
            ////currentRow = keywordGrid.SelectedIndex;
            ////var myDV = keywordGrid.ItemsSource;
            ////foreach(TestCaseEditViewModel tVm in myDV)
            ////{
            ////    iRowCount++;
            ////    if (currentRow == iRowCount)
            ////        break;
            ////    if (tVm.SelectedKeyword._keywordName == "PegWindow" && tVm.SelectedObjectName != null)
            ////    {
            ////        DataRow dr = dt.NewRow();
            ////        dr["RunOrder"] = tVm.RunOrder;
            ////        dr["KeyWordId"] = tVm.SelectedKeyword._id;
            ////        dr["KeyWord"] = tVm.SelectedKeyword._keywordName;
            ////        lastPegWindowKeyWord = tVm.SelectedKeyword._keywordName;
            ////        dr["ObjectId"] = tVm.SelectedObjectName._id;
            ////        lastMasterObjectId = tVm.SelectedObjectName._id;
            ////        dr["ObjectName"] = tVm.SelectedObjectName.ObjName;
            ////        lastMasterObjectName = tVm.SelectedObjectName.ObjName;
            ////        dt.Rows.Add(dr);
            ////    }
            ////}            
        }

        private void keywordGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Console.WriteLine("keywordGrid_CellEditEnding");
            
            //e.Cancel = true;
        }

        void ClickEventOnRepeatButton(object sender, RoutedEventArgs e)
        {
            Logger.logBegin("ClickEventOnRepeatButton");
        }
        private void TcGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Logger.logBegin("TcGrid_ScrollChanged");
            if (e.HorizontalChange != 0)
            {
                // Do stuff..
            }
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Logger.Info("UserControl_PreviewKeyDown", string.Format("key :[{0}] is down",e.Key.ToString()));
            VMColl v = (VMColl)this.DataContext;
            //if (e.Key == Key.Tab)
            //{
            //    e.Handled = true;
            //    KeyEventArgs args = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, Key.Return);
            //    args.RoutedEvent = Keyboard.KeyDownEvent;
            //    InputManager.Current.ProcessInput(args);
            //    return;
            //}
            if (e.Key==Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (wndFinder.Visibility==Visibility.Collapsed)
                {
                    wndFinder.Visibility = Visibility.Visible;
                }
                else
                {
                    wndFinder.Visibility = Visibility.Collapsed;
                }
            }

            if ((e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control))
            {
                if (this.keywordGrid.IsEditing()) return;
                e.Handled = true;
                v.CopySelectedRowsCommand.Execute(_testCaseName);
            }

            else  if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (this.keywordGrid.IsEditing()) return;
                e.Handled = true;
                v.PasteSelectedRowsCommand.Execute(_testCaseName);
            }

            else if (e.Key == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
            {
                v.PasteRowsSpecialCommand.Execute(_testCaseName);
            }

            else if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
            {
                v.AddNewRowCommand.Execute(_testCaseName);
            }

            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                 v.SaveCommand.Execute(_testCaseName);
            }

            else if (e.Key == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
            {
                v.MoveUpSelectedRowsCommand.Execute(_testCaseName);
            }

            else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                v.MoveDownSelectedRowsCommand.Execute(_testCaseName);
            }

            else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ProcessWizardRequest(v);
                e.Handled = true;
            }

            else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Control)
            {
                v.DeleteSelectedRowsCommand.Execute(_testCaseName);
            }
// just use this for testing
            else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
            {

                if (MessageBox.Show("Are you sure?", "?", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                {
                    // If yes
                }
                else
                {
                    // If no
                }
            }
        }

        private void ProcessWizardRequest(VMColl v)
        {
            string rowColValue = v.SelectedTestCases[0].RowColumn;
            string keywordName = v.SelectedTestCases[0].SelectedKeyword.KeywordName;
            long objectId = v.SelectedTestCases[0].SelectedObjectName.Id;
            string objectName = v.SelectedTestCases[0].SelectedObjectName.ObjName;

            switch (keywordName)
            {
                case "FillTable":
                    /*
                    FillTableWizardDialog inputDialog = new FillTableWizardDialog("", rowCol);
                    string result = "";
                    if(inputDialog.ShowDialog() == true)
                    {
                        result = inputDialog.Answer;
                        v.SelectedTestCases[0].RowColumn = result;
                    }
                    */

                    List<T_OBJECT_CHILDDTO> objectChildDtoList = BoHelper.GetObjectChildList(MarsMainWindow.CurrentDatabaseIdx, objectId);

                   
                    FillEditWizardDialog1 inputDialog = new FillEditWizardDialog1(keywordName,
                                                                      objectName,
                                                                      rowColValue,
                                                                      objectChildDtoList);
                    string result = "";
                    if (inputDialog.ShowDialog() == true)
                    {
                        result = inputDialog.RowColValue;
                        v.SelectedTestCases[0].RowColumn = result;
                    }

                    break;

            }
        }

        private void keywordGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            //Console.WriteLine(e.Column.Header.ToString());
        }

        
        internal void PopulateIds(long projectId, long testSuiteId, long testCaseId)
        {
            VMColl v = (VMColl)this.DataContext;
            v.PopulateIds(projectId, testSuiteId, testCaseId);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Console.WriteLine();
            TextBox tb = (TextBox)sender;
            //tb.EndChange();
           // this.keywordGrid.CommitEdit();
            
        }

        private string latestPegwindowInfo;
        public string LatestPegwindowInfo
        {
            get { return latestPegwindowInfo; }
        }
        internal bool IsUnderAutoGen()
        {
            Logger.logBegin("IsUnderAutoGen");
            if (!(this.DataContext is VMColl))
            {
                Logger.Warnning("IsUnderAutoGen", string.Format("DataContext requires VMColl,but [{0}] returns", this.DataContext == null ? "NULL" : this.DataContext.GetType().ToString()));
                return false;
            }

            VMColl vmDt = (VMColl)this.DataContext;
            
            bool isUnderGen = vmDt.IsUnderAutoGen();
            latestPegwindowInfo = vmDt.CurrentLatestPegAccessQuickInfo;
            return isUnderGen;
        }

        internal bool AutoGen_GenStep(string strSwfName, string strType, string strTxt, ref string strTmp)
        {
            Logger.logBegin("AutoGen_GenStep");
            if (!(this.DataContext is VMColl))
            {
                Logger.Warnning("AutoGen_GenStep", string.Format("DataContext requires VMColl,but [{0}] returns", this.DataContext == null ? "NULL" : this.DataContext.GetType().ToString()));
                return false;
            }
            VMColl vmDt = (VMColl)this.DataContext;
            
            bool isGenreated = vmDt.AutGen_GenStep(strSwfName, strType, strTxt, ref strTmp); 
            if (isGenreated)
            {
                /// set border
                /// 
                FlashLatestRow();
            }
            return isGenreated;
        }

        private void FlashLatestRow()
        {
            //keywordGrid.Items[keywordGrid.Items.Count]
        }

        private void keywordGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            if (((DataGrid)this.keywordGrid).VerticalScrollBarVisibility==ScrollBarVisibility.Visible)
            {
                var border = VisualTreeHelper.GetChild(keywordGrid, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null) scroll.ScrollToEnd();
                }
            }
        }

        internal void HideControlPanel()
        {
            this.controlPanel.Visibility = Visibility.Collapsed;

            this.contolPanelColumn.Width = GridLength.Auto;
            this.keywordGrid.Margin = new Thickness(0, 0, 5, 0); 
        }

        internal void AddTestCaseData(DataTable dt)
        {
            PasteDataTable = dt;
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            var kwrdSource = this.Resources["keyword_source"] as CollectionViewSource;
            if (kwrdSource == null) return;
            kwrdSource.Filter += KeywordMarsFilter;

            if (PasteDataTable  != null)
            {
                VMColl v = (VMColl)this.DataContext;
                v.pasteRowsFromDataTable(MarsMainWindow.CurrentDatabaseIdx, PasteDataTable);
                v.TestCaseName = PasteDataTable.TableName;
                v.RefershTitle();
                v.isTestCaseCreated = false;
                PasteDataTable = null;
            }
        }

        private void KeywordMarsFilter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            Logger.Info("KeywordMarsFilter",string.Format("sender type:[{0}]",sender.GetType().ToString()));
        }

        public DataTable PasteDataTable { get; set; }

        internal void SetApplicationId(long appId)
        {
            VMColl v = (VMColl)this.DataContext;
            v.SetApplicationId(appId);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void cbKeyword_KeyUp(object sender, KeyEventArgs e)
        {
            if (!(sender is ComboBox)) return;
            ComboBox cb = (ComboBox)sender;
            CollectionView clctVw = (CollectionView)CollectionViewSource.GetDefaultView(cb.ItemsSource);

            clctVw.Filter=((o) => {
                
                if (o == null) return true;
                if (!(o is Keyword)) return true;
                if (string.IsNullOrEmpty(cb.Text)) return true;
                else
                {
                    if (((Keyword)o).KeywordName.Contains(cb.Text)) return true;
                    else return false;
                }
            });
            clctVw.Refresh();
        }

        private void cbKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is ComboBox)) return;
            ComboBox cb = (ComboBox)sender;
            //if (cb.)
        }

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = sender as DataGridCell;
            
            if (cell != null && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (!cell.IsFocused)
                {
                    cell.Focus();
                }
                DataGrid dataGrid = FindVisualParent<DataGrid>(cell);
                //ComboBox objEmbeded = FindVisualChild<ComboBox>(cell);
                if (dataGrid != null)
                {
                    //dataGrid.CancelEdit();
                    if (dataGrid.SelectionUnit != DataGridSelectionUnit.FullRow)
                    {
                        if (!cell.IsSelected)
                            cell.IsSelected = true;
                    }
                    else
                    {
                        DataGridRow row = FindVisualParent<DataGridRow>(cell);
                        if (row != null && !row.IsSelected)
                        {
                            if (((Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) == KeyStates.Down) ||
                                ((Keyboard.GetKeyStates(Key.RightShift) & KeyStates.Down) == KeyStates.Down) ||
                                ((Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) == KeyStates.Down) ||
                                ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == KeyStates.Down))
                                row.IsSelected = true;
                            else
                            {
                                if (dataGrid.SelectedItems != null)
                                {
                                    for (int i = dataGrid.SelectedItems.Count - 1; i >= 0; i--)
                                    {
                                        if (dataGrid.SelectedItems[i] != row)
                                            dataGrid.SelectedItems.RemoveAt(i);
                                    }

                                }
                                row.IsSelected = true;
                            }
                            //row.IsSelected = true;
                        }
                    }
                    cell.IsEditing = true;
                }
            }
        }

        static T FindVisualChild<T>(UIElement element) where T : UIElement
        {
            UIElement child = element;
            while(child!=null)
            {
                T correctlyTyped = child as T;
                if (correctlyTyped!=null)
                {
                    return correctlyTyped;
                }
                T objTmp;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(child); i++)
                {
                    objTmp = FindVisualChild<T>(VisualTreeHelper.GetChild(child, i) as UIElement);
                    if (objTmp != null)
                        return (T)objTmp;
                }
            }
            return null;
        }
        static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                T correctlyTyped = parent as T;
                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }
            return null;
        }

        private void txtBoxForKeyword_TextChanged(object sender, TextChangedEventArgs e)
        {
            Logger.logBegin("txtBoxForKeyword_TextChanged");
        }

        private void TCGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            
            
        }

        private void keywordGrid_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void keywordGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Logger.Info("keywordGrid_MouseLeftButtonDown","Mouse donw");
        }

        private void keywordGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Logger.logBegin("keywordGrid_PreviewMouseDown");
        }

        private void keywordGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            
        }

        private void keywordGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var dg = (DataGrid)sender;
            //if (dg.selectrow)
            Logger.Info("keywordGrid_SelectedCellsChanged",string.Format("count :[{0}]", dg.SelectedItems==null?0:dg.SelectedItems.Count));
        }
#if _TestStepUnit
        public OnRequestStartWCFSvcEvent onRequestStartWCFSvcAgent
        {
            get {
                if (this.DataContext == null) return null;
                if (!(this.DataContext is VMColl)) return null;
                return ((VMColl)this.DataContext).onRequestStartWCFSvcHandler;
            }

            set {
                if (this.DataContext == null) return ;
                if (!(this.DataContext is VMColl)) return ;
                ((VMColl)this.DataContext).onRequestStartWCFSvcHandler = value;
            }
        }

        public OnRequestWCFSvcStatusEvent onRequestWCFSvcStatusAgent
        {
            get
            {
                if (this.DataContext == null) return null;
                if (!(this.DataContext is VMColl)) return null;
                return ((VMColl)this.DataContext).onRequestWCFSvcStatusHandler;
            }
            set
            {
                if (this.DataContext == null) return;
                if (!(this.DataContext is VMColl)) return;
                ((VMColl)this.DataContext).onRequestWCFSvcStatusHandler = value;
            }
        }

        //private void keywordGrid_PreviewMouseDown_1(object sender, MouseButtonEventArgs e)
        //{

        //}

        //private void keywordGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    Logger.logBegin("keywordGrid_PreviewKeyDown", string.Format("sender type:[{0}], key:[{1}]", sender==null?"N/A":sender.GetType().ToString(), e.Key));
            
        //}

        private void keywordGrid_KeyDown(object sender, KeyEventArgs e)
        {
            Logger.logBegin("keywordGrid_KeyDown", string.Format("sender type:[{0}], key:[{1}]", sender == null ? "N/A" : sender.GetType().ToString(), e.Key));
            if (!(sender is DataGrid)) return;
            try
            {
                if (!((e.Key == Key.Tab)
                    //||(e.Key== Key.Left)||(e.Key == Key.Right)
                    || (e.Key == Key.Up) || (e.Key == Key.Down))) return;

                Key currentKey = e.Key;
                //if (e.Key==Key.Right)
                //{
                //    currentKey = Key.Tab;
                //}
                DataGrid dbGrid = sender as DataGrid;
                if (dbGrid==null)
                {
                    return;
                }
                int iCurrentRowIdx = dbGrid.Items.IndexOf(dbGrid.CurrentItem) - 1;
                ///获得当前active的cell
                /// 
                switch (currentKey)
                {
                    case Key.V:
                        if ((Keyboard.IsKeyDown(Key.LeftCtrl)) || (Keyboard.IsKeyDown(Key.RightCtrl)))
                        {
                            VMColl dataCntx = (this.DataContext as VMColl);
                            if (dataCntx == null) return;
                            dataCntx.PasteSelectedRowsCommand.Execute(_testCaseName);
                            e.Handled = true;
                            return;
                        }
                        else
                        {
                            return;
                        }

                    case Key.Tab:
                        if (dbGrid.CurrentCell == null)
                        {
                            ///active the first Keyword cell
                            /// 
                            SetCurrentActiveCellTo(dbGrid, 0, 2);
                        }
                        else
                        {

                            if (dbGrid.CurrentCell.Column.DisplayIndex < 6)
                            {
                                //SetCurrentActiveCellTo(dbGrid, iCurrentRowIdx, dbGrid.CurrentCell.Column.DisplayIndex + 1);
                                dbGrid.BeginEdit();
                                e.Handled = true;
                            }
                            else
                            {
                                if (iCurrentRowIdx < (dbGrid.Items == null ? int.MinValue : dbGrid.Items.Count - 1))
                                {
                                    SetCurrentActiveCellTo(dbGrid, iCurrentRowIdx + 2, 2);
                                    e.Handled = true;
                                }
                            }
                        }
                        break;
                    case Key.Up:
                        if ((iCurrentRowIdx + 1) >= (dbGrid.Items.Count))
                            iCurrentRowIdx = dbGrid.Items.Count - 1;
                        else
                        {
                            if (iCurrentRowIdx >= 0)
                                iCurrentRowIdx -= 1;
                            else
                            {
                                e.Handled = true;
                                return;
                            }
                            SetCurrentActiveCellTo(dbGrid, iCurrentRowIdx + 1, dbGrid.CurrentCell.Column.DisplayIndex);
                        }
                        e.Handled = true;
                        break;
                    case Key.Down:
                        if ((iCurrentRowIdx + 2) >= (dbGrid.Items.Count))
                        {
                            return;
                        }
                        iCurrentRowIdx += 1;
                        SetCurrentActiveCellTo(dbGrid, iCurrentRowIdx + 1, dbGrid.CurrentCell.Column.DisplayIndex);
                        e.Handled = true;
                        break;
                }

            }catch(Exception exc)
            {
                Logger.Error("keywordGrid_KeyDown", exc.Message, exc);
                e.Handled = true; 
            }


        }

        private void SetCurrentActiveCellTo(DataGrid dbGrid, int iRow, int iCol)
        {
            Logger.logBegin("SetCurrentActiveCellTo", string.Format("Row/Col:[{0}/{1}]", iRow, iCol));
            
            int iRowCount = dbGrid.ItemContainerGenerator==null?-1:(dbGrid.ItemContainerGenerator.Items==null?-1:dbGrid.ItemContainerGenerator.Items.Count);
            if (iRowCount <0||iRowCount<iRow||iRow<0) return;

            DataGridRow aRow = (DataGridRow)dbGrid.ItemContainerGenerator.ContainerFromIndex(iRow);
            if (aRow == null) return;

            dbGrid.UpdateLayout();
            if (iRow+1< dbGrid.Items.Count)
                dbGrid.ScrollIntoView(dbGrid.Items[iRow+1]);
            bool isOk = false;
            string strError = "";
            DataGridCell oneCell = this.GetCellFromRowAndCellOrd(aRow, iCol,ref isOk, ref strError);
            if (oneCell==null||(!isOk))
            {
                Logger.Error("SetCurrentActiveCellTo",string.Format("Nosuch column with index:[{0}].Error from {1}", iCol, strError));
                return;
            }
            //dbGrid.CurrentCell= new DataGridCellInfo(oneCell);
            oneCell.Focus();
            dbGrid.BeginEdit();
            
            //if (!oneCell.IsSelected)
            //    oneCell.IsSelected = true;
            //oneCell.IsEditing = true;
            
           
        }

        private void keywordGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
        
        }

        private void cbKeyword_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void keywordGrid_BeginningEdit_1(object sender, DataGridBeginningEditEventArgs e)
        {
            
        }

        private void keywordGrid_StylusMove(object sender, StylusEventArgs e)
        {
            
        }

        internal void ActiveRowByIndex(TestStepViewModel testStepViewModel)
        {
            DataGridRow row =(DataGridRow)this.keywordGrid.ItemContainerGenerator.ContainerFromItem(testStepViewModel);
            row.IsSelected = true;
            //this.keywordGrid.SelectedItem = row
            this.keywordGrid.ScrollIntoView(row);
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this.keywordGrid.IsEditing()) e.Handled = true;
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }


#endif
    }
}
