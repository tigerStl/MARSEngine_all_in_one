using com.Mars.ClipboardMgr;
using com.Mars.Constants;
using Mars.basicDataStructure;
using Mars.Business;
using Mars.Converter;
using Mars.DataLayer;
using Mars.Dialog;
using Mars.Dto;
using Mars.Helpers;
using Mars.MarsDataStructure.TestResult;
using Mars.Model;
using Mars.Utility;
using Mars.Utility.clipboardManagement;
using Mars.Views.gridBase;
using Mars.xml.importExport;
using MarsTestFrame.SourceCode.com.Mars.BusinessLogic;
using MarsTestFrame.systemUtil;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mars.ViewModel
{
    public class StoryboardDataGrid : MarsTigerGridBase
    {
        private static MLogger log = MLogger.GetLogger(typeof(StoryboardDataGrid));

        public StoryboardDataGrid()
        {
            this.SelectionChanged += StoryboardDataGrid_SelectionChanged;
        }



        void StoryboardDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.SelectedItemsList = this.SelectedItems.Cast<StoryboardEditViewModel>()
                                .ToList();
            }
            catch (Exception ex)
            {
                log.Error("StoryboardDataGrid_SelectionChanged",ex.Message,ex);
            }
        }
        #region SelectedItemsList

        

        public IList<StoryboardEditViewModel> SelectedItemsList
        {
            get {
                return (IList<StoryboardEditViewModel>)GetValue(SelectedItemsListProperty);
            }
            set {
                SetValue(SelectedItemsListProperty, value);
            }
        }

        //public static TContainer GetContainerFromIndex<TContainer>(this ItemsControl itemsControl, int index) where TContainer : DependencyObject
        //{
        //    return (TContainer)itemsControl.ItemContainerGenerator.ContainerFromIndex(index);
        //}

        

        public static readonly DependencyProperty SelectedItemsListProperty =
                //DependencyProperty.Register("SelectedItemsList", typeof(IList<StoryboardEditViewModel>), typeof(StoryboardDataGrid), new PropertyMetadata(null));
                DependencyProperty.Register("SelectedItemsList", typeof(IList<StoryboardEditViewModel>), typeof(StoryboardDataGrid), new PropertyMetadata(null));

        #endregion
    }

    public class StoryboardColl : Notify
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardColl));

        private List<long> deletedStoryboards;
        internal MarsStoryboardTreeView _AssignedTreeNode = null;

        #region cell变化的事件管理
        private void StoryboardDetailAliasChangeImpl(Notify objSource)
        {
            if (objSource == null) return;
            if (!(objSource is StoryboardEditViewModel)) return;
            StoryboardEditViewModel sourceStoryboardRow = objSource as StoryboardEditViewModel;
            if (StoryboardRows == null) return;
            for (int i=1;i< StoryboardRows.Count;i++)
            {
                StoryboardRows[i].DependOnSteps = StoryboardRows[i].PopulateDependOnSteps(false);
            }
        }

        

        #endregion

        #region Commands

        private ICommand _refreshCommand;

        public ICommand RefreshCommand
        {
            get { return _refreshCommand; }
            set { _refreshCommand = value; }
        }


        private ICommand _addNewRowCommand;
        public ICommand AddNewRowCommand
        {
            get { return _addNewRowCommand; }
            set { _addNewRowCommand = value; }
        }

        private ICommand _deleteSelectedRowsCommand;
        public ICommand DeleteSelectedRowsCommand
        {
            get { return _deleteSelectedRowsCommand; }
            set { _deleteSelectedRowsCommand = value; }
        }

        private ICommand _moveUpSelectedRowsCommand;
        public ICommand MoveUpSelectedRowsCommand
        {
            get { return _moveUpSelectedRowsCommand; }
            set { _moveUpSelectedRowsCommand = value; }
        }

        private ICommand _moveDownSelectedRowsCommand;
        public ICommand MoveDownSelectedRowsCommand
        {
            get { return _moveDownSelectedRowsCommand; }
            set { _moveDownSelectedRowsCommand = value; }
        }

        private ICommand _copySelectedRowsCommand;
        public ICommand CopySelectedRowsCommand
        {
            get { return _copySelectedRowsCommand; }
            set { _copySelectedRowsCommand = value; }
        }

        private ICommand _pasteSelectedRowsCommand;
        public ICommand PasteSelectedRowsCommand
        {
            get { return _pasteSelectedRowsCommand; }
            set { _pasteSelectedRowsCommand = value; }
        }

        private ICommand _importBaseLineDataCommand;
        public ICommand ImportBaseLineDataCommand
        {
            get
            {
                return _importBaseLineDataCommand;
            }
            set
            {
                _importBaseLineDataCommand = value;
            }
        }

        private ICommand _exportBaseLineDataCommand;
        public ICommand ExportBaseLineDataCommand
        {
            get
            {
                return _exportBaseLineDataCommand;
            }
            set
            {
                _exportBaseLineDataCommand = value;
            }
        }

        public void CopyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Clipboard operation occured!");
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get { return _saveCommand; }
            set { _saveCommand = value; }
        }

        private ICommand _saveAsCommand;
        public ICommand SaveAsCommand
        {
            get { return _saveAsCommand; }
            set { _saveAsCommand = value; }
        }

        private ICommand _actionChangedEnventHandle;
        public ICommand ActionChangedEnventHandle
        {
            get { return _actionChangedEnventHandle; }
            private set { _actionChangedEnventHandle = value; }
        }

        #endregion

        public List<B_PROJ_TS_TC_FULLVISION> projectData;

        

        List<string> actionNames = new List<string>();

        public List<string> ActionNames
        {
            get
            {
                return actionNames;
            }
            set { actionNames = value; }
        }

        string testcaseIsDoneLck = "_Monitor_RefreshStoryboard";
        public void RefreshStoryboard()
        {
            // LoadProjectData(ProjectName);
            try
            {
                Monitor.Enter(testcaseIsDoneLck);
                LoadStoryboardRows(_storyboardId);
                deletedStoryboards = new List<long>();
            }
            catch (Exception e)
            {
                Logger.Error("RefreshStoryboard",string.Format("exception:[{0}],trace:[{1}]",e.Message,e.StackTrace),e);
            }finally
            {
                Monitor.Exit(testcaseIsDoneLck);
            }
            
        }

        public void addNewRow(string storyboardName)
        {
            StoryboardEditViewModel newStoryBoardVM;
            if (StoryboardRows.Count == 0)
            {
                newStoryBoardVM = new StoryboardEditViewModel(new StoryboardEditViewModel(), 1, storyboardName, ref _storyboardRows, this);
                newStoryBoardVM.StorboardStepNameChangeHandler = StoryboardDetailAliasChangeImpl;
                StoryboardRows.Add(newStoryBoardVM);
                
                return;
            }
            var lastSelectedRow = SelectedStoryboardRows == null ? null : SelectedStoryboardRows.LastOrDefault();
            if (lastSelectedRow==null)
            {
                newStoryBoardVM = new StoryboardEditViewModel(new StoryboardEditViewModel(), StoryboardRows.Count+1, storyboardName, ref _storyboardRows, this);
                newStoryBoardVM.StorboardStepNameChangeHandler = StoryboardDetailAliasChangeImpl;
                StoryboardRows.Add(newStoryBoardVM);
                return;
            }
            long iRunOrd = lastSelectedRow.RunOrder + 1;
            foreach(var itm in StoryboardRows.Where(p=>p.RunOrder>= iRunOrd))
            {
                if (itm == null) continue;
                itm.RunOrder += 1;
            }
            int iPos = StoryboardRows.IndexOf(lastSelectedRow);
            SelectedStoryboardRows.Clear();
            StoryboardRows.Insert(iPos + 1, newStoryBoardVM =new StoryboardEditViewModel(new StoryboardEditViewModel(), iRunOrd, storyboardName, ref _storyboardRows, this));
            newStoryBoardVM.StorboardStepNameChangeHandler = StoryboardDetailAliasChangeImpl;
            SelectedStoryboardRows.Add(newStoryBoardVM);
            //else
            //{
            //    var lastRowViewmodel = StoryboardRows.FirstOrDefault(x => x.RunOrder == StoryboardRows.Count);
            //    if (lastRowViewmodel == null) return;
            //    long runOrder = StoryboardRows.Count + 1;
            //    newStoryBoardVM = new StoryboardEditViewModel(lastRowViewmodel, runOrder, storyboardName, ref _storyboardRows, this);
            //}
            //StoryboardRows.Add(newStoryBoardVM);
            //newStoryBoardVM.DependOnSteps = newStoryBoardVM.PopulateDependOnSteps();

            //if (SelectedStoryboardRows != null && SelectedStoryboardRows.Count > 0)
            //{

            //    long selectedRow = SelectedStoryboardRows[0].RunOrder;
            //    long delta = StoryboardRows.Count - selectedRow - 1;
            //    SelectedStoryboardRows.Clear();
            //    SelectedStoryboardRows.Add(newStoryBoardVM);

            //    for (long i = 0; i < delta; i++)
            //        moveSelectedRows("up");
            //}
        }

        private void moveSelectedRows(string direction)
        {
            if (SelectedStoryboardRows.Count == 1)
            {
                int indexOfSelectedRow = StoryboardRows.IndexOf(SelectedStoryboardRows[0]);
                foreach (StoryboardEditViewModel selectedRow in SelectedStoryboardRows)
                {
                    if (direction == "up")
                    {
                        if (indexOfSelectedRow > 0)
                        {
                            StoryboardRows.Move(indexOfSelectedRow, indexOfSelectedRow - 1);
                            StoryboardRows[indexOfSelectedRow].RunOrder = indexOfSelectedRow + 1;
                            StoryboardRows[indexOfSelectedRow - 1].RunOrder = indexOfSelectedRow;
                        }
                    }
                    else
                    {
                        if (indexOfSelectedRow < StoryboardRows.Count - 1)
                        {
                            StoryboardRows.Move(indexOfSelectedRow, indexOfSelectedRow + 1);
                            StoryboardRows[indexOfSelectedRow].RunOrder = indexOfSelectedRow + 1;
                            StoryboardRows[indexOfSelectedRow + 1].RunOrder = indexOfSelectedRow + 2;
                        }
                    }
                }
  //             RebuldDependOnSteps();
            }
            else
            {
                System.Windows.MessageBox.Show("Select a single row to move", "Test Steps", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private string _RowsSelectCheckBoxContext="Select All Rows";
        public string RowsSelectCheckBoxContext
        {
            get {
                return _RowsSelectCheckBoxContext;
            }
            set
            {
                _RowsSelectCheckBoxContext = value;
                OnPropertyChanged("RowsSelectCheckBoxContext");
            }
        }

        private bool? _IsSelectAllRows;
        public bool? IsSelectAllRows
        {
            get
            {
                return _IsSelectAllRows;
            }
            set
            {
                if (_IsSelectAllRows != value)
                {
                    _IsSelectAllRows = value;
                    OnPropertyChanged("IsSelectAllRows");

                    if (_IsSelectAllRows.HasValue ? _IsSelectAllRows.Value : false)
                    {
                        RowsSelectCheckBoxContext = "Unselect All Rows";
                    }
                    else
                    {
                        RowsSelectCheckBoxContext = "Select All Rows";
                    }
                }
            }
        }

        //string action;


        public class RunAction : Notify
        {
            public RunAction()
            {

            }
        }


        private void AddActionNames()
        {
            actionNames.Add("EXECUTE");
            actionNames.Add("RUN");
            actionNames.Add("SKIP");
            actionNames.Add("DONE");
            actionNames.Add("FAILUE");
        }
        private string currentDBIdx = null; 
        public StoryboardColl(string strDBIdx, string projectName, string storyboardName, long projectId, long storyboardId)
        {
            currentDBIdx = strDBIdx;

            AddActionNames();
            _storyboardId = storyboardId;
            _pojectId = projectId;

            //_storyBoardName = storyboardName;
            _projectName = projectName;
            _storyboardName = storyboardName;

            long lTm;
            //Logger.Info("StoryboardColl", string.Format("----PerformanceTesting:{0}", lTm = DateTime.Now.Ticks));
            //LoadProjectData( projectId);
            //Logger.Info("StoryboardColl", string.Format("----PerformanceTesting cost:{0}", lTm = DateTime.Now.Ticks - lTm));
            lTm = DateTime.Now.Ticks;
            LoadStoryboardRows(storyboardId);
            Logger.Info("StoryboardColl", string.Format("----PerformanceTesting cost:{0}", lTm = DateTime.Now.Ticks - lTm));

            deletedStoryboards = new List<long>();

            // Commands

            _addNewRowCommand = new DelegateCommand(() => { addNewRow(projectName); });
            _deleteSelectedRowsCommand = new DelegateCommand(() => { deleteSelectedRows(); });

            _moveUpSelectedRowsCommand = new DelegateCommand(() => { moveSelectedRows("up"); });
            _moveDownSelectedRowsCommand = new DelegateCommand(() => { moveSelectedRows("down"); });

            _copySelectedRowsCommand = new DelegateCommand(() => { copySelectedRows(); });
            _pasteSelectedRowsCommand = new DelegateCommand(() => { pasteSelectedRows(); });

            _exportBaseLineDataCommand = new DelegateCommand(() => { ExportBaseLineDataImpl(); });
            _importBaseLineDataCommand = new DelegateCommand(() => { ImportBaseLineDataImpl(); });

            _saveAsCommand = new DelegateCommand(() => { SaveStoryboardAs(); });
            //_saveCommand = new DelegateCommand(() => { SaveStoryboard(null); });
            _saveCommand = new DelegateCommand(() => { SaveStoryboardNew(); }); 

            _actionChangedEnventHandle = new DelegateCommand(() => { ActionChangeEnventImpl(); });

            #region Application information
            Logger.Info("StoryboardColl", "Begin to get LoadAssignedApplicationByProjectAndStoryboard");
            LoadAssignedApplicationByProjectAndStoryboard(_pojectId, _storyboardId);
            Logger.Info("StoryboardColl", "end to get LoadAssignedApplicationByProjectAndStoryboard");
            #endregion //Application information

            _refreshCommand = new DelegateCommand(() => { RefreshStoryboard(); });

            Title = "Storyboard: " + _storyboardName;
            
        }

        

        public void RebuldDependOnSteps()
        {
            foreach (var row in StoryboardRows)
            {
                row.DependOnSteps = row.PopulateDependOnSteps();
                row.SelectedDependOnStep = row.DependOnSteps.Where(a => a.Id == row.RelyOn).FirstOrDefault();
            }
        }
        private void ActionChangeEnventImpl()
        {
            Logger.Info("ActionChangeEnventImpl", "test");
        }

        public bool SaveStoryboardAs()
        {
            List<B_PROJ_TC_MGR> storyBoardRowList = new List<B_PROJ_TC_MGR>();
            List<B_STORYBOARD_DATASET_SETTING> storyBoardRowDataSettingList = new List<B_STORYBOARD_DATASET_SETTING>();
            string newStoryboardName = "";
            
            SaveAsDialog inputDialog = new SaveAsDialog("Please Enter Storyboard name:", _storyboardName);
            newStoryboardName = "";
            if (inputDialog.ShowDialog() == true)
            {
                newStoryboardName = inputDialog.Answer;
                if (string.IsNullOrEmpty(newStoryboardName))
                {
                    MessageBox.Show("Please input a validate storyboard name.", "Hint");
                    return false;
                }
                if (BoHelper.isStoryboardNameExist(MarsMainWindow.CurrentDatabaseIdx, newStoryboardName, _pojectId))
                {
                    MessageBox.Show("Error: Storyboard " + newStoryboardName + " already exists in project  " + _projectName, "SaveAs Error", MessageBoxButton.OK);
                    return false;
                }
            }
            else
            {
                return true;
            }
            
            MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);
            long lNewStoryboardId = -1;
            try
            {
                using (var scope = new TransactionScope())
                {

                    // Save T_STORYBOARD_SUMMARY
                    B_STORYBOARD_SUMMARY summary = new B_STORYBOARD_SUMMARY();
                    summary.STORYBOARD_ID = lNewStoryboardId= BoHelper.GetTestStepsId(objTrans.CurrentDBContext);
                    summary.STORYBOARD_NAME = newStoryboardName;
                    summary.ASSIGNED_PROJECT_ID = StoryboardRows[0].ProjectId;
                    summary.DESCRIPTION = "Storyboard " + summary.STORYBOARD_ID;
                    //BoHelper.SaveStoryboardSummary(summary);

                    /// because of transcation problems, no data layer object should be here
                    /// and transcation should be finished in one method
                    /// those rows need delete and replaced
                    /// 
                    // Save T_PROJ_TC_MGR
                    foreach (StoryboardEditViewModel model in StoryboardRows)
                    {
                        B_PROJ_TC_MGR storyBoardRow = new B_PROJ_TC_MGR();
                        storyBoardRow.STORYBOARD_ID = summary.STORYBOARD_ID;
                        storyBoardRow.STORYBOARD_DETAIL_ID = BoHelper.GetTestStepsId(objTrans.CurrentDBContext);
                        storyBoardRow.PROJECT_ID = model.ProjectId;
                        storyBoardRow.TEST_CASE_ID = model.TestCaseId;
                        storyBoardRow.TEST_SUITE_ID = model.TestSuiteId;

                        storyBoardRow.RUN_ORDER = model.RunOrder;

                        storyBoardRow.ALIAS_NAME = model.AliasName;
                        storyBoardRow.RUN_TYPE = BoHelper.GetSystemLookupValue(MarsMainWindow.CurrentDatabaseIdx, "T_PROJ_TC_MGR", "RUN_TYPE", model.DisplayName);
                        storyBoardRowList.Add(storyBoardRow);

                        B_STORYBOARD_DATASET_SETTING storyBoardRowDataSetting = new B_STORYBOARD_DATASET_SETTING();
                        storyBoardRowDataSetting.SETTING_ID = BoHelper.GetTestStepsId(objTrans.CurrentDBContext);
                        storyBoardRowDataSetting.DATA_SUMMARY_ID = model.DataSummaryId;
                        storyBoardRowDataSetting.STORYBOARD_DETAIL_ID = storyBoardRow.STORYBOARD_DETAIL_ID;
                        storyBoardRowDataSettingList.Add(storyBoardRowDataSetting);

                    }
                    //BoHelper.SaveStoryboardRows(storyBoardRowList);
                    //BoHelper.SaveStoryboardRowDataSettings(storyBoardRowDataSettingList);

                    BoHelper objBoHelp = new BoHelper();
                    objBoHelp.SaveStoryboardSummaryByInst(MarsMainWindow.CurrentDatabaseIdx, summary, objTrans.CurrentDBContext);
                    objBoHelp.SaveStoryboardRowsByInst(MarsMainWindow.CurrentDatabaseIdx, storyBoardRowList, objTrans.CurrentDBContext);
                    objBoHelp.SaveStoryboardRowDataSettingsByInst(MarsMainWindow.CurrentDatabaseIdx, storyBoardRowDataSettingList, objTrans.CurrentDBContext);
                    //objBoHelp.SaveChangesByInst();

                    int iUpdatedCnt = objTrans.CurrentDBContext.SaveChanges();
                    scope.Complete();

                    Logger.Info("SaveStoryboardAs",string.Format("Total records [{0}] are updated/inserted sucessfully ", iUpdatedCnt));
                }
                ViewModelBase.HintByMessageBox("Storyboard has been saved successfully." );

                if (_AssignedTreeNode!=null )
                {
                    MarsProjectTreeView projectTreeNode = null;
                    TreeViewModelBase nodeParent = _AssignedTreeNode;
                    while ((nodeParent != null)&&(!(nodeParent is MarsFolderTreeView)))
                    {
                        nodeParent = (TreeViewModelBase)nodeParent.Parent ;
                        if (!(nodeParent is TreeViewModelBase))
                        {
                            nodeParent = null;
                            break;
                        }
                    }
                    if (nodeParent == null) return true;
                    //projectTreeNode = nodeParent as MarsProjectTreeView;
                    MarsFolderTreeView foldOfProjNode = (_AssignedTreeNode.Parent as MarsFolderTreeView);
                    if (foldOfProjNode == null) return true;
                    if ((foldOfProjNode.TREE_ITEM == null)) return true;
                    /**
                     * MarsStoryboardTreeView storyboardTreeView = new MarsStoryboardTreeView(storyboard.STORYBOARD_NAME,
                                                                                       storyboard.STORYBOARD_ID,
                                                                                       storyBoardFolder.ProjectName,
                                                                                       storyBoardFolder.ProjectId
                                                                                       );
                     * 
                     * **/
                    foldOfProjNode.TREE_ITEM.Add(new MarsStoryboardTreeView(newStoryboardName, lNewStoryboardId, _projectName, _pojectId));
                    //refresh

                }
                return true;
            }
            catch (Exception e)
            {
                string strError = "";
                Logger.Error("SaveStoryboardAs", strError=string.Format("Exception:[{0}] stackTrace:\r\n{1}",e.Message,e.StackTrace),e);
                ViewModelBase.HintByMessageBox(strError, "Error");
                return false;
            }
            finally
            {
                Logger.logEnd("SaveStoryboardAs"); 
            }
            //BoHelper.SaveChanges();

        }

        private bool InitStorybarodBeforeSave()
        {
            if (StoryboardRows.Any(p => (p == null) || (p.TestCaseId == -1) || (p.TestSuiteId == null) || (p.DataSummaryId == null)))
            {
                ViewModelBase.HintByMessageBox("Please set all Test Case/Suite/Data Set Information, before save.");
                return false;
            }
            foreach (var itm in StoryboardRows)
            {
                if (itm == null) continue;
                itm.HasDealedBySaveMark = false;
            }
            return true;
        }
        public bool SaveStoryboardNew()
        {
            Logger.logBegin("SaveStoryboardNew");
            string strError = "";
            DbConnection cnn = null;


            if (StoryboardRows.Any(p=>(string.IsNullOrEmpty(p.DisplayName)) || (p.SelectedTestCase==null)||(p.SelectedTestSuite==null)||p.SelectedDataSetName==null))
            {
                ViewModelBase.HintByMessageBox("please make sure Test case and data set are selected!");
                return false;
            }

            try
            {
                //if (!InitStorybarodBeforeSave()) return false;

                //TO MAKE SURE THAT ALL 
                BoHelper.GetSystemLookupValue(MarsMainWindow.CurrentDatabaseIdx, "T_PROJ_MGR", "RUN_TYPE", "SKIP");

                //Get default storyboard for deleted items;
                //B_STORYBOARD_SUMMARY objDefaultStoryboard = B_STORYBOARD_SUMMARY.GetStoryBoardInfoById(-1);

                //List<StoryboardEditViewModel> storyboardToSave = StoryboardRows.ToList();
                List<StoryboardEditViewModel> haveNotDoneStoryDtal = new List<StoryboardEditViewModel>();
                //首先获得现在数据库被版本
                //然后和当前版本对比修改或者添加
                List<V_STORYBOARD_TEST_FULLVISIONDTO> lstCurrentStoryboard = B_V_STORYBOARD_TEST_FULLVISION.GetStoryBoards( this.StoryboardId, MarsMainWindow.CurrentDatabaseIdx);
                //标记为未处理
                lstCurrentStoryboard.ForEach(p => p.PROJECT_ID = -1);                

                MarsEntities objDBCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
                //确保runtype已经加载
                BoHelper.GetSystemLookupValue(MarsMainWindow.CurrentDatabaseIdx, "T_PROJ_TC_MGR", "RUN_TYPE", "SKIP", objDBCntx);

                if ((cnn=objDBCntx.Database.Connection).State != ConnectionState.Open)
                {
                    cnn.Open();
                }

                //B_PROJ_TC_MGR objStoryboardDtlOp = new B_PROJ_TC_MGR();
                DbTransaction objTrans = objDBCntx.Database.Connection.BeginTransaction();
                
                for(int i=0;i<this.StoryboardRows.Count;i++)
                {
                    var storyboardDtl = StoryboardRows[i];
                    if (storyboardDtl == null) continue;
                    if (storyboardDtl.RunOrder != i + 1)
                        storyboardDtl.RunOrder = i + 1;
                    //if (storyboardDtl.StoryboardDetailId<0)
                    //{
                    // 创建一个新的storyboard detail item
                    B_PROJ_TC_MGR objNewItm = new B_PROJ_TC_MGR()
                    {
                        STORYBOARD_DETAIL_ID = storyboardDtl.StoryboardDetailId,
                        PROJECT_ID = PojectId,
                        TEST_CASE_ID = storyboardDtl.SelectedTestCase.Id,
                        STORYBOARD_ID = this.StoryboardId,
                        RUN_TYPE = BoHelper.GetSystemLookupValue(MarsMainWindow.CurrentDatabaseIdx, "T_PROJ_TC_MGR", "RUN_TYPE", storyboardDtl.DisplayName, null, false),
                        //DependsOnString = storyboardDtl.SelectedDependOnStep==null?null: storyboardDtl.SelectedDependOnStep.DataItemName,
                        RUN_ORDER = i + 1,
                        LATEST_TEST_MARK_ID = storyboardDtl.LatestTestMarkId,
                        ALIAS_NAME = storyboardDtl.AliasName,
                        TEST_SUITE_ID = storyboardDtl.SelectedTestSuite.Id,
                    };
                    //处理depends on。因为denpends on 必须是前面的数据，因此，前面的数据一定有detail id
                    if (storyboardDtl.SelectedDependOnStep != null)
                    {
                        var parntDtl = StoryboardRows.FirstOrDefault(p => (!string.IsNullOrEmpty(p.AliasName)) && (string.Compare(storyboardDtl.SelectedDependOnStep.DataItemName, p.AliasName, true) == 0));
                        if (parntDtl != null)
                        {
                            if (parntDtl.StoryboardDetailId < 0)
                            {
                                objTrans.Rollback();
                                objDBCntx.Database.Connection.Close();
                                Logger.Error("SaveStoryboardNew", string.Format("${0}'s depends on itm's storyboard detail id is less than 0-[{1}]", i + 1, storyboardDtl.SelectedDependOnStep.DataItemName));
                                ViewModelBase.HintByMessageBox("Wrong depends on Storyboard Detail Id, -1");
                                return false;
                            }
                            objNewItm.DEPENDS_ON = parntDtl.StoryboardDetailId;
                        }
                    }
                    //判断是否新的
                    if (objNewItm.STORYBOARD_DETAIL_ID != 0)
                    {
                        //判断是否存在
                        var oldStoryItm = lstCurrentStoryboard.FirstOrDefault(p => p.STORYBOARD_DETAIL_ID == objNewItm.STORYBOARD_DETAIL_ID);
                        if (oldStoryItm != null)
                        {
                            //将现有数据runorder修改为-1
                            oldStoryItm.RUN_ORDER = -1;
                            oldStoryItm.PROJECT_ID = objNewItm.PROJECT_ID ?? -2; //将该行标记为处理
                                                                                 //通过connect 修改数据库记录
                            if (!objNewItm.UpdateByConnection(cnn, ref strError))
                            {
                                objTrans.Rollback();
                                ViewModelBase.HintByMessageBox(strError);
                                return false;
                            }
                            ////创建dataset的关系
                            /// 先删除，在添加，使用merge
                            /// 
                            if (!B_STORYBOARD_DATASET_SETTING.MergStoryboardDtlIdAndDataSetId(cnn, objNewItm.STORYBOARD_DETAIL_ID, storyboardDtl.SelectedDataSetName.Id, ref strError))
                            {
                                objTrans.Rollback();
                                ViewModelBase.HintByMessageBox(strError);
                                return false;
                            }
                        }
                    }
                    else//新记录
                    {
                        long lNewDtlId = -1;
                        //新记录
                        if (!objNewItm.CreateNewByConnection(cnn,ref lNewDtlId, ref strError))
                        {
                            objTrans.Rollback();
                            ViewModelBase.HintByMessageBox(strError);
                            return false;
                        }
                        objNewItm.STORYBOARD_DETAIL_ID = lNewDtlId;

                        //创建dataset的关系
                        if (!B_STORYBOARD_DATASET_SETTING.MergStoryboardDtlIdAndDataSetId(cnn, objNewItm.STORYBOARD_DETAIL_ID, storyboardDtl.SelectedDataSetName.Id, ref strError))
                        {
                            objTrans.Rollback();
                            ViewModelBase.HintByMessageBox(strError);
                            return false;
                        }
                    }
                    
                }
                //}

                //然后将所有的没有处理的放到一个专门用来保留删除数据的Storyboard中
                //该storyboard为空
                //判断是否存在一个空的
                if (!B_STORYBOARD_SUMMARY.CheckOrCreateDefaultStoryboardByCnn(cnn, ref strError))
                {
                    objTrans.Rollback();
                    ViewModelBase.HintByMessageBox(strError);

                    return false;
                }

                //然后将所有的没有处理的放到一个专门用来保留删除数据的Storyboard中，-1
                var lstUndealed = lstCurrentStoryboard.Where(p => p.PROJECT_ID == -1).ToList();
                foreach(var itmUndealed in lstUndealed)
                {
                    if (itmUndealed == null) continue;
                    if (!B_PROJ_TC_MGR.DeleteStoryboardDetailToContainerByCnn(cnn, itmUndealed.STORYBOARD_DETAIL_ID,ref strError))
                    {
                        objTrans.Rollback();
                        ViewModelBase.HintByMessageBox(strError);
                        return false;
                    }
                }
                objTrans.Commit();
                ViewModelBase.HintByMessageBox(string.Format("Storyboard [{0}] have been saved!",this._storyboardName));
                return true;
                
            }catch(Exception e)
            {
                ViewModelBase.HintByMessageBox(string.Format("Exception when save:\r\n{0}", e.Message));
                Logger.Error("SaveStoryboardNew", e.Message, e);
                return false;
            }
            finally
            {
                try
                {
                    if (cnn!=null)
                    {
                        cnn.Close();
                    }
                }
                catch (Exception)
                {
                    
                }
                Logger.logEnd("SaveStoryboardNew");
            }
        }

        internal bool DealWithCSVFormatPaste(string strFromClip, ref string strError)
        {
            string[] arrRows = strFromClip.Split(new string[] { "\r\n","\r", "\n"  }, StringSplitOptions.None);
            string[] arrHeader = arrRows[0].Split(new char[] { ',' }, StringSplitOptions.None);
            //header 已经经过验证
            DataTable dt = new DataTable();
            dt.Columns.Clear();
            for (int i=0;i< cnst_listStandardHeaders.Count; i++)
            {
                dt.Columns.Add(cnst_listStandardHeaders[i]);
            }

            DataTable dtSource = new DataTable();
            dtSource.Columns.Clear();
            for (int i=0;i<arrHeader.Length;i++)
            {
                dtSource.Columns.Add(arrHeader[i]);
            }

            for (int i=1;i<arrRows.Length;i++)
            {
                if (string.IsNullOrEmpty(arrRows[i])) continue;
                string[] arrOneRow = arrRows[i].Split(new char[] { ',' }, StringSplitOptions.None);
                try
                {
                    dtSource.Rows.Add(arrOneRow);
                    DataRow oneRow = dt.NewRow();
                    for  (int j=0;j<dt.Columns.Count;j++)
                    {                        
                        oneRow[j] = dtSource.Rows[i - 1][dt.Columns[j].Caption];
                    }
                    dt.Rows.Add(oneRow);
                }
                catch (Exception e)
                {
                    strError = string.Format("Row data [{0}] has problem when create to datatable with error:\r\n{1}", arrRows[i], e.Message);
                    return false;
                }
                
            }


            return DealPasteByDataTable(dt, ref strError);

        }

        private static List<string> cnst_listStandardHeaders = new List<string>{ "Run Order", "Action", "Step Name",
        "Test Suite Name", "Test Case Name","Data Set Name", "Dependency"};
        public static string GetStandardCVSHeader()
        {
            return string.Join(",",cnst_listStandardHeaders).Substring(0,30);
        }

        internal bool DealPasteByDataTable(DataTable dt, ref string strError)
        {
            if (dt == null)
            {
                strError = "Datatable is null";
                return false;
            }
            /**
             * 算法：
             * 1，判断是否是有效的header
             * 2，创建临时变量list
             * 3，排序
             * 4，插入
             * */
            List<string> lstNotInclude = new List<string>();
            cnst_listStandardHeaders.ForEach((p) => {
                bool isIncluded = false;
                foreach (var hd in dt.Columns)
                {
                    if (hd == null) continue;

                    if (string.Compare(p, ((DataColumn)hd).Caption, true)==0)
                    {
                        isIncluded = true;
                    }
                }
                if (!isIncluded)
                    lstNotInclude.Add(p);
            });
            if (lstNotInclude.Count>0)
            {
                strError = string.Format("columns [{0}] are not find from Data table", string.Join(",", lstNotInclude ));
                return false;
            }
            //2，创建临时变量list
            ///2.1 获得所有的ts和tc的配套的名称
            ///
            List<KeyValuePair<string, string>> lstTSTCPairFromDT = new List<KeyValuePair<string, string>>();
            bool isOk = false;
            var tsColumn = dt.Columns.Cast<DataColumn>().FirstOrDefault(p=> string.Compare(p.ColumnName, "Test Suite Name",true)==0);
            var tcColumn = dt.Columns.Cast<DataColumn>().FirstOrDefault(p => string.Compare(p.ColumnName, "Test Case Name",true)==0);
            var dsColumn = dt.Columns.Cast<DataColumn>().FirstOrDefault(p => string.Compare(p.ColumnName, "Data Set Name", true)==0);
            List<string> lstDTSetId = new List<string>();
            StringBuilder sbNotAvailable = new StringBuilder();
            for (int i=0;i<dt.Rows.Count;i++)
            {
                DataRow rowCurrent = dt.Rows[i];
                string strTC = rowCurrent.Field<string>(tsColumn),
                     strTS = rowCurrent.Field<string>(tsColumn),
                     strDS = rowCurrent.Field<string>(dsColumn);
                if (string.IsNullOrEmpty(strTS))
                {
                    sbNotAvailable.Append(strTS);
                    continue;
                }
                if (string.IsNullOrEmpty(strTC))
                {
                    sbNotAvailable.Append(string.Join(",",strTS, strTC ));
                    continue;
                }
                lstTSTCPairFromDT.Add(new KeyValuePair<string, string>(dt.Rows[i].Field<string>(tsColumn), dt.Rows[i].Field<string>(tcColumn)));
                lstDTSetId.Add(dt.Rows[i].Field<string>(dsColumn));
            }
            if (sbNotAvailable.Length > 1)
            {
                strError = string.Format("No such Test Suite , test cases and data set exist in current test project. \r\n{0} ", sbNotAvailable.ToString());
                return false;
            }
            
            List<V_PROJ_TS_TC_FULLVISIONDTO> lstTSTC = B_PROJ_TS_TC_FULLVISION.GetTSTCByNamePair(
                MarsMainWindow.CurrentDatabaseIdx,
                lstTSTCPairFromDT, ref isOk, ref strError);
            if (lstTSTC == null) return false;
            lstTSTC =lstTSTC.Where(p => (p.PROJECT_ID == _pojectId)&&(lstDTSetId.Contains(p.DATA_ALIAS))).ToList();
            
            List<StoryboardEditViewModel> lstTmpStoryboard = new List<StoryboardEditViewModel>();
            
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                StoryboardEditViewModel tmpViewMode = new StoryboardEditViewModel();
                tmpViewMode.storyboardColl = this;

                string strDataSetNameFromClipboard = lstDTSetId[i];
                KeyValuePair<string, string> tstcFromClipboard = lstTSTCPairFromDT[i];

                //找到testsuite 
                tmpViewMode.TestSuites = StoryboardEditViewModel.PopulateTestSuites(projectData);
                tmpViewMode.SelectedTestSuite = tmpViewMode.TestSuites.Where(p => p.DataItemName == tstcFromClipboard.Key).FirstOrDefault();
                if (tmpViewMode.SelectedTestSuite == null)
                {
                    strError = string.Format("no such Test suite in current test suites:[{0}]", tstcFromClipboard.Key + ";" + tstcFromClipboard.Value);
                    return false;
                }
                //找到testcase
                tmpViewMode.SelectedTestCase = tmpViewMode.TestCases.FirstOrDefault(p=>p.DataItemName==tstcFromClipboard.Value);
                if (tmpViewMode.SelectedTestCase==null)
                {
                    strError = string.Format("no such Test case in current test suites:[{0}]", tstcFromClipboard.Key + ";" + tstcFromClipboard.Value);
                    return false;
                }
                //找到选中的dataset
                tmpViewMode.SelectedDataSetName = tmpViewMode.DataSetNames.FirstOrDefault(p=>p.DataItemName==strDataSetNameFromClipboard);
                if (tmpViewMode.SelectedDataSetName==null)
                {
                    strError = string.Format("no such Dataset in current test case:[{0}]-[{1}]", tstcFromClipboard.Value, strDataSetNameFromClipboard);
                    return false;
                }
                //处理action， depends 以及别名
                tmpViewMode.AliasName = dt.Rows[i].Field<string>("Step Name");
                tmpViewMode.RunOrder = -1;//tmp
                //tmpViewMode.ParentAliasName 依赖需要后续处理
                string strTmpAction;
                if (actionNames.Contains(strTmpAction = dt.Rows[i].Field<string>("Action")))
                {
                    tmpViewMode.changeSelectedDisplay = false;
                    tmpViewMode.DisplayName = strTmpAction;
                    tmpViewMode.changeSelectedDisplay = true;
                }
                else
                {
                    strError = string.Format("no such Dataset in current test case:[{0}]-[{1}]", tstcFromClipboard.Value, strDataSetNameFromClipboard);
                    return false;
                }

                lstTmpStoryboard.Add(tmpViewMode);
            }
            //确保所有的runorder是连续的
            for (int i = 0; i < StoryboardRows.Count; i++)
                StoryboardRows[i].RunOrder = i + 1;

            if (this.SelectedStoryboardRows == null)
            {
                //appeend to the end
                lstTmpStoryboard.ForEach(p =>
                {                    
                    StoryboardRows.Add(p);
                }
                );
            }
            else
            {
                //override from last selected row
                var lastsortedSelectedRow = this.SelectedStoryboardRows.OrderBy(p => p.RunOrder).ToList().LastOrDefault();
                //SelectedStoryboardRows.Clear();
                for (int i= 0; i<lstTmpStoryboard.Count;i++)
                {
                    
                    var currentItm = lstTmpStoryboard[i];
                    StoryboardRows.Insert((int)lastsortedSelectedRow.RunOrder + i, currentItm);
                    //if ((lastsortedSelectedRow.RunOrder + i) <= StoryboardRows.Count)
                    //{
                    //    StoryboardRows[(int)lastsortedSelectedRow.RunOrder + i-1] = currentItm;
                    //}
                    //else
                    //{
                    //    StoryboardRows.Add(currentItm);
                    //}
                }
            }

            //确保所有的runorder是连续的
            for (int i = 0; i < StoryboardRows.Count; i++)
                StoryboardRows[i].RunOrder = i + 1;

            return true;
        }

        public bool SaveStoryboard(string newStoryboardName=null)
        {
            Logger.logBegin("SaveStoryboard");
            string strError = "";
            MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);
            try
            {                
                List<B_PROJ_TC_MGR> storyBoardRowList = new List<B_PROJ_TC_MGR>();
                List<B_STORYBOARD_DATASET_SETTING> storyBoardRowDataSettingList = new List<B_STORYBOARD_DATASET_SETTING>();
                using (var scope = new TransactionScope())
                {
                    
                    try
                    {
                        // if it is to createa a new storyboard
                        if (!string.IsNullOrEmpty(newStoryboardName))
                        {
                            long id = CreateStoryboard(newStoryboardName,objTrans.CurrentDBContext);

                            this.StoryboardId = id;
                            foreach (var row in this.StoryboardRows)
                            {
                                row.StoryboardDetailId = -1;
                                row.StoryboardId = id;
                            }
                        }
                        // Save deleted steps to database
                        ///这里可以用sql，速度快
                        foreach (var storyboard in deletedStoryboards)
                        {
                            if (storyboard != 0)
                                BoHelper.DeleteStoryboard(storyboard,objTrans.CurrentDBContext);
                        }
                        deletedStoryboards.Clear();
                    }
                    catch (Exception ex)
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Error while saving deleted records.", ex.ToString()),"Error");                        
                        return false;
                    }

                    // Save T_PROJ_TC_MGR
                    int iOrd = 1;
                    foreach (StoryboardEditViewModel model in StoryboardRows)
                    {
                        try
                        {
                            if (model.SelectedTestSuite == null ||
                                model.SelectedTestCase == null ||
                                model.SelectedDataSetName == null
                                )
                            {
                                ViewModelBase.HintByMessageBox("Please fill Project Name, Test Suite Name, Test Case Name and Data Set Name for each row in the storyboard", "Error");
                                return false;
                            }
                            if (model.RunOrder != iOrd)
                                model.RunOrder = iOrd; // make sure the order is right 

                            if (model.StoryboardDetailId == -1 || BoHelper.GetStoryboardByID(MarsMainWindow.CurrentDatabaseIdx, 
                                model.StoryboardDetailId, objTrans.CurrentDBContext) == null)
                            {
                                //delete by run_order if the run_order exists
                                B_PROJ_TC_MGR.DeleteIfStoryboardRunOrdExists(model.StoryboardId, model.RunOrder, objTrans.CurrentDBContext, ref strError);

                                B_PROJ_TC_MGR storyBoardRow = new B_PROJ_TC_MGR();
                                storyBoardRow.STORYBOARD_ID = model.StoryboardId;
                                storyBoardRow.STORYBOARD_DETAIL_ID = BoHelper.GetTestStepsId(objTrans.CurrentDBContext);
                                storyBoardRow.PROJECT_ID = model.ProjectId;
                                storyBoardRow.TEST_CASE_ID = model.SelectedTestCase.Id;
                                storyBoardRow.TEST_SUITE_ID = model.SelectedTestSuite.Id;
                                storyBoardRow.RUN_ORDER = model.RunOrder;
                                storyBoardRow.ALIAS_NAME = model.AliasName;
                                storyBoardRow.RUN_TYPE = BoHelper.GetSystemLookupValue(MarsMainWindow.CurrentDatabaseIdx, 
                                    "T_PROJ_TC_MGR", "RUN_TYPE", model.DisplayName, objTrans.CurrentDBContext);
                                if (model.SelectedDependOnStep != null && model.SelectedDependOnStep.Id != -1)
                                    storyBoardRow.DEPENDS_ON = model.SelectedDependOnStep.Id;
                                storyBoardRowList.Add(storyBoardRow);

                                // AF save the new STORYBOARD_DETAIL_ID in the model
                                model.StoryboardDetailId = storyBoardRow.STORYBOARD_DETAIL_ID;

                                B_STORYBOARD_DATASET_SETTING storyBoardRowDataSetting = new B_STORYBOARD_DATASET_SETTING();
                                storyBoardRowDataSetting.SETTING_ID = BoHelper.GetTestStepsId(objTrans.CurrentDBContext);
                                storyBoardRowDataSetting.DATA_SUMMARY_ID = model.SelectedDataSetName.Id;
                                storyBoardRowDataSetting.STORYBOARD_DETAIL_ID = storyBoardRow.STORYBOARD_DETAIL_ID;
                                storyBoardRowDataSettingList.Add(storyBoardRowDataSetting);
                            }
                            else
                            {
                                // Updating an existing test step
                                B_PROJ_TC_MGR storyBoardRow = BoHelper.GetStoryboardByID(MarsMainWindow.CurrentDatabaseIdx, 
                                    model.StoryboardDetailId, objTrans.CurrentDBContext);
                                //storyBoardRow.STORYBOARD_ID = model.StoryboardId;
                                //storyBoardRow.STORYBOARD_DETAIL_ID = BoHelper.GetTestStepsId();
                                storyBoardRow.PROJECT_ID = model.ProjectId;
                                storyBoardRow.TEST_CASE_ID = model.SelectedTestCase.Id;
                                storyBoardRow.TEST_SUITE_ID = model.SelectedTestSuite.Id;
                                storyBoardRow.RUN_ORDER = model.RunOrder;
                                storyBoardRow.ALIAS_NAME = model.AliasName;
                                storyBoardRow.RUN_TYPE = BoHelper.GetSystemLookupValue(MarsMainWindow.CurrentDatabaseIdx, 
                                    "T_PROJ_TC_MGR", "RUN_TYPE", model.DisplayName, objTrans.CurrentDBContext);
                                if (model.SelectedDependOnStep != null && model.SelectedDependOnStep.Id != -1)
                                    storyBoardRow.DEPENDS_ON = model.SelectedDependOnStep.Id;
                                else
                                    storyBoardRow.DEPENDS_ON = null;

                                storyBoardRowList.Add(storyBoardRow);

                                B_STORYBOARD_DATASET_SETTING storyBoardRowDataSetting = new B_STORYBOARD_DATASET_SETTING();
                                //storyBoardRowDataSetting.SETTING_ID = BoHelper.GetTestStepsId();
                                storyBoardRowDataSetting.SETTING_ID = -1;
                                storyBoardRowDataSetting.DATA_SUMMARY_ID = model.SelectedDataSetName.Id;
                                storyBoardRowDataSetting.STORYBOARD_DETAIL_ID = storyBoardRow.STORYBOARD_DETAIL_ID;
                                storyBoardRowDataSettingList.Add(storyBoardRowDataSetting);
                            }
                        }
                        finally
                        {
                            iOrd++;
                        }
                    }
                    BoHelper.SaveStoryboardRows(MarsMainWindow.CurrentDatabaseIdx, storyBoardRowList, objTrans.CurrentDBContext);
                    BoHelper.SaveStoryboardRowDataSettings(MarsMainWindow.CurrentDatabaseIdx, storyBoardRowDataSettingList, objTrans.CurrentDBContext);
                    ///BoHelper.SaveChanges();
                    /// 
                    int iCnt = objTrans.CurrentDBContext.SaveChanges();
                    scope.Complete();
                    
                    ViewModelBase.HintByMessageBox(strError=string.Format("Storybord:[{0}] is saved successfully.", this._storyboardName), "Hint");
                    Logger.Info("SaveStoryboard",string.Format("{0} total [{1}] records are updated/inserted", strError, iCnt));
                    return true;
                }
            }
            catch (Exception e)
            {
                ViewModelBase.HintByMessageBox(strError=string.Format("SaveStoryboard generates Exception: [{0}], stackTrace:[{1}]",e.Message,e.StackTrace));
                Logger.Error("SaveStoryboard", strError,e);
                //System.Windows.MessageBox.Show("Exception: \n" + e.ToString());
                return false;
            }
            finally
            {
                Logger.logEnd("SaveStoryboard");
            }
        }
        /// <summary>
        /// 判断选中的行是不是新行
        /// </summary>
        /// <returns></returns>
        internal bool IsSelecteRowEmptyRow()
        {
            if (this.SelectedStoryboardRows == null) return true;
            foreach(var itmRow in this.SelectedStoryboardRows)
            {
                if ((itmRow.TestSuiteId == null) || (itmRow.TestSuiteId <= 0)) return true;
                if ((itmRow.TestCaseId <= 0)) return true;
            }
            return false;
        }

        internal bool CopySelectedRows2WindowsClipboard(ref string strError)
        {
            Logger.logBegin("CopySelectedRows2WindowsClipboard");
            if (this.SelectedStoryboardRows == null)
            {
                Clipboard.SetDataObject("");
                return true;
            }
            StringBuilder objCnt = new StringBuilder();
            try
            {
                //Append Header
                objCnt.Append(ClipboardMgrForStoryboard.FormatStoryboardInfo(new string[] {"RUN_ORDER", "Action", "Step Name", "Test Suite Name", "Test Case Name",
                    "Data Set Name", "Result", "Error Cause" , "Script Start", "Script End", "Dependency", "Description"}));
                foreach (var itm in this.SelectedStoryboardRows)
                {
                    if (itm == null) continue;
                    string[] arrItmsForStoryBoard = itm.ToStringArray();
                    objCnt.Append(ClipboardMgrForStoryboard.FormatStoryboardInfo(arrItmsForStoryBoard));
                }
                Clipboard.SetDataObject(objCnt.ToString());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CopySelectedRows2WindowsClipboard",strError=string.Format("Exception:[{0}],stackTrace:\r\n{1}",e.Message,e.StackTrace));
                return false;
            }
            finally
            {
                Logger.logEnd("CopySelectedRows2WindowsClipboard");
            }
        }

        internal int DelTestHisData(bool? isDelBaseline, bool? isDelNoneBaseline, ref string strError)
        {
            /// check wheether a test case is actived
            /// 
            if (this.SelectedStoryboardRows == null)
            {
                strError = string.Format("None test case is selected.");
                return -1;
            }

            int iErrorId = 0;
            foreach (var sBoard in this.SelectedStoryboardRows)
            {
                iErrorId = BoHelper.DeleteHistDataByStoryBoardDetailId(MarsMainWindow.CurrentDatabaseIdx, 
                    sBoard.StoryboardDetailId, isDelBaseline, isDelNoneBaseline, ref strError);
            }

            // int iErrorId = BoHelper.DeleteHistDataByStoryBoardDetailId(this.SelectedStoryboardRows[0].StoryboardDetailId,isDelBaseline,isDelNoneBaseline, ref strError);

            //this.SelectedStoryboardRows[0].TestCaseId;
            return iErrorId;
        }


        private void deleteSelectedRows()
        {
            foreach (StoryboardEditViewModel selectedRow in SelectedStoryboardRows)
            {
                deletedStoryboards.Add(selectedRow.StoryboardDetailId);
                StoryboardRows.Remove(selectedRow);
            }

            for (int i = 0; i < StoryboardRows.Count; i++)
            {
                StoryboardRows[i].RunOrder = i + 1;
            }

            RebuldDependOnSteps();
        }

       
        private void copySelectedRows()
        {
            
            if (isSelectionInSequence())
            {
                //ApplicationCommands.Copy.Execute(null, );
                StoryboardClipBoard.storyboardClipBoard = new List<StoryboardEditViewModel>();
                foreach (StoryboardEditViewModel selectedRow in SelectedStoryboardRows)
                {

                    StoryboardEditViewModel storyboard = selectedRow.CloneObj();
                    StoryboardClipBoard.storyboardClipBoard.Add(storyboard);
                }
            }
        }

        private TigerClipBoardMgr4StoryBoard StoryclipBoardMgrExcel = new TigerClipBoardMgr4StoryBoard(MarsMainWindow.CurrentDatabaseIdx);
        private bool IsClipBoardFormatRight()
        {
            Logger.logBegin("IsClipBoardFormatRight");

            DataTable objTbl = StoryclipBoardMgrExcel.clipboardExcelToDataTable(1,true);

            if (objTbl == null) return false;    
             
            return true;
        }

        private bool PasteDataFromExcel(ref string strError)
        {
            Logger.logBegin("PasteDataFromExcel");
            try
            {
                if (SelectedStoryboardRows == null)
                {
                    strError = "No Empty Row is";
                    return false;
                }
                ///算法：
                /// 1，从数据库中查询是否存在testcase testsuite data set 的名称
                /// 2，如果存在
                /// 
                bool isOk = false;
                List<V_STORYBOARD_TEST_FULLVISIONDTO> lstStorySummaryFromExcel = StoryclipBoardMgrExcel.ConvertDataTable2Dto(ref isOk, ref strError);
                MarsEntities db = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
                var q = from tc in db.T_TEST_CASE_SUMMARY
                        from tstc in db.REL_TEST_CASE_TEST_SUITE
                        from ts in db.T_TEST_SUITE
                        from tcdat in db.REL_TC_DATA_SUMMARY
                        from dat in db.T_TEST_DATA_SUMMARY
                        from proj in db.T_TEST_PROJECT
                        from projts in db.REL_TEST_SUIT_PROJECT
                        where
                            tstc.TEST_SUITE_ID == ts.TEST_SUITE_ID
                        && tstc.TEST_CASE_ID == tc.TEST_CASE_ID
                        && tcdat.TEST_CASE_ID == tc.TEST_CASE_ID
                        && tcdat.DATA_SUMMARY_ID == dat.DATA_SUMMARY_ID
                        && projts.PROJECT_ID == proj.PROJECT_ID
                        && projts.TEST_SUITE_ID == ts.TEST_SUITE_ID
                        && proj.PROJECT_ID == this._pojectId
                        select new
                        {
                            tc_name = tc.TEST_CASE_NAME,
                            ts_name = ts.TEST_SUITE_NAME,
                            tc_id = tc.TEST_CASE_ID,
                            ts_id = ts.TEST_SUITE_ID,
                            dat_name = dat.ALIAS_NAME,
                            dat_id = dat.DATA_SUMMARY_ID,
                            tc_desc = tc.TEST_STEP_DESCRIPTION,
                            ts_desc = ts.TEST_SUITE_DESCRIPTION,
                            dt_desc = dat.DESCRIPTION_INFO
                        };
                Logger.Info("PasteDataFromExcel",string.Format("sQL:[{0}]", q.ToString()));
                dynamic dList = q.ToList();
                lstStorySummaryFromExcel.ForEach(oneInExcel => {
                    Logger.logBegin("lstStorySummaryFromExcel.ForEach", string.Format("Test case:[{0}] Test suite:[{1}] Target Count:[{2}]\r\nXml:[{3}]", 
                        oneInExcel.TEST_CASE_NAME, oneInExcel.TEST_SUITE_NAME, dList.Count, dList.ToString()));
                    
                    var q1 = q.FirstOrDefault(z =>
                        z.dat_name == oneInExcel.DATA_SET_ALIAS_NAME
                        && z.tc_name == oneInExcel.TEST_CASE_NAME
                        && z.ts_name == oneInExcel.TEST_SUITE_NAME);
                    if (q1 != null)
                    {
                        oneInExcel.TEST_CASE_ID = q1.tc_id;
                        oneInExcel.TEST_SUITE_ID = q1.ts_id;
                        oneInExcel.DATA_SETTING_ID = q1.dat_id;
                        oneInExcel.TEST_STEP_DESCRIPTION = q1.tc_desc;
                        oneInExcel.TEST_SUITE_DESCRIPTION = q1.ts_desc;
                        oneInExcel.DATASET_DESCRIPTION = q1.dt_desc;
                    }
                });

                List<V_STORYBOARD_TEST_FULLVISIONDTO> lstErrorItm = lstStorySummaryFromExcel.Where(p => p.TEST_CASE_ID == -2 || p.TEST_SUITE_ID == -2).ToList();
                string strErrorTmp = "No such Test case or Test suite Names:";
                if (lstErrorItm!=null&&lstErrorItm.Count>0)
                {
                    lstErrorItm.ForEach(oneItm => {
                        strErrorTmp = string.Format("{0}\r\nTS Name:[{1}]-TC Name:[{2}]", strErrorTmp,oneItm.TEST_SUITE_NAME, oneItm.TEST_CASE_NAME);
                    });
                    strError = strErrorTmp;
                    return false;
                }

                ///添加数据到StoryboardEditViewModel， 目标对象 StoryboardRows _storyboardRows
                /// 
                List<StoryboardEditViewModel> lstTmpNewStoryBoardModel = GenerateStoryBoardRowFromSTORYBOARD_TEST_FULLVISIONDTO(lstStorySummaryFromExcel, ref isOk, ref strError);
                ///重新编排run——order
                /// 
                int iStart = (int)SelectedStoryboardRows.LastOrDefault().RunOrder,iCnt=0;                
                lstTmpNewStoryBoardModel.ForEach(itm=> {
                    itm.RunOrder = iStart+iCnt++;
                });
                
                var last = _storyboardRows.Where(p => p.RunOrder >= iStart).ToList();
                if (last!=null)
                {
                    last.ForEach(l =>
                    {
                        l.RunOrder = iStart + iCnt++;
                    });
                }
                iCnt = 0;
                lstTmpNewStoryBoardModel.ForEach(itm => { _storyboardRows.Insert(((iStart-1)<0?0:(iStart-1))+ iCnt++, itm); });
                OnPropertyChanged("StoryboardRows");

                return true;
            }catch(Exception e)
            {
                Logger.Error("PasteDataFromExcel", strError=string.Format("Exception:[{0}] StackTrace:\r\n{1}",e.Message,e.StackTrace));
                return false;
            }
            finally
            {
                Logger.logEnd("PasteDataFromExcel");
            }
        }

        private List<StoryboardEditViewModel> GenerateStoryBoardRowFromSTORYBOARD_TEST_FULLVISIONDTO(List<V_STORYBOARD_TEST_FULLVISIONDTO> lstTmpStoryBoardItm, ref bool isOk, ref string strError)
        {
            Logger.logBegin("GenerateStoryBoardRowFromSTORYBOARD_TEST_FULLVISIONDTO",string.Format("Source item count:[{0}]", lstTmpStoryBoardItm==null?0: lstTmpStoryBoardItm.Count));
            try
            {
                List<StoryboardEditViewModel> lstRslt = new List<StoryboardEditViewModel>();
                if (lstTmpStoryBoardItm == null)
                {
                    isOk = true;
                    return null;
                }
                lstTmpStoryBoardItm.ForEach(oneStoryBoardItm =>
                {
                    StoryboardEditViewModel objTmpViewModel = new StoryboardEditViewModel(this,
                        this.StoryboardId,//oneStoryBoardItm.STORYBOARD_ID,
                        -1,//oneStoryBoardItm.STORYBOARD_DETAIL_ID,
                        this.PojectId,
                        this._projectName,
                        "",//oneStoryBoardItm.PROJECT_DESCRIPTION,
                        oneStoryBoardItm.TEST_CASE_NAME,
                        oneStoryBoardItm.TEST_CASE_ID,
                        oneStoryBoardItm.TEST_STEP_DESCRIPTION,
                        oneStoryBoardItm.TEST_SUITE_ID,
                        oneStoryBoardItm.TEST_SUITE_NAME,
                        oneStoryBoardItm.TEST_SUITE_DESCRIPTION,
                        -1,
                        null,
                        oneStoryBoardItm.ALIAS_NAME,
                        oneStoryBoardItm.DISPLAY_NAME,
                        null,//oneStoryBoardItm.PARENT_ALIAS_NAME,
                        null,//oneStoryBoardItm.TEST_RUN_VALUE,
                        null,//oneStoryBoardItm.LATEST_TEST_MARK_ID,
                        null,//oneStoryBoardItm.HIST_LATEST_TEST_MARK_ID,
                        null,//oneStoryBoardItm.HIST_ID,
                        null,//oneStoryBoardItm.HIST_TEST_ID,
                        null,//oneStoryBoardItm.TEST_CASE_BEGIN_TIME,
                        null,//oneStoryBoardItm.TEST_CASE_END_TIME,
                        null,//oneStoryBoardItm.HIST_TEST_RESULT_IN_TEXT,
                        null,//oneStoryBoardItm.HIST_TEST_MODE,
                        null,//oneStoryBoardItm.HIST_RESULT,
                        oneStoryBoardItm.DATA_SUMMARY_ID,
                        oneStoryBoardItm.DATA_SET_ALIAS_NAME,
                        oneStoryBoardItm.DATASET_DESCRIPTION
                        );
                    objTmpViewModel.StorboardStepNameChangeHandler = StoryboardDetailAliasChangeImpl;
                    lstRslt.Add(objTmpViewModel);
                });
                return lstRslt;
            }
            finally
            {
                Logger.logEnd("GenerateStoryBoardRowFromSTORYBOARD_TEST_FULLVISIONDTO");
            }
            

        }

        private void ImportBaseLineDataImpl()
        {
            Logger.logBegin("ImportBaseLineDataImpl");
            try
            {
                ///算法：
                /// 1， 选择文件
                /// 2， loaddata，
                /// 
                OpenFileDialog dlgOpenFile = new OpenFileDialog()
                {
                    Filter = "*.xml|*.cfg",
                    CheckFileExists = false,
                    InitialDirectory = SystemCommonUtil.GetCurrentPathDir(),
                };

                bool? isFiled = dlgOpenFile.ShowDialog();
                if ((!isFiled.HasValue) || (!isFiled.Value))
                {
                    return;
                }
                string strFileName = dlgOpenFile.FileName,strError="";

                List<long> lstDtlIds = StoryboardRows.Select(p => p.StoryboardDetailId).ToList();

                XmlBaseLineExportImportMgr objBaseLineImp = new XmlBaseLineExportImportMgr();
                bool isOk = objBaseLineImp.ImportBaselineDataFromFile(this.StoryboardId, lstDtlIds,strFileName, ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Error:[{0}]", strError));
                    return;
                }
                ViewModelBase.HintByMessageBox(string.Format("File:[{0}] is imported.", strFileName));
            }
            catch (Exception e)
            {
                Logger.Error("ImportBaseLineDataImpl", string.Format("Exception:[{0}]", e.Message), e);
            }
            finally
            {

            }
        }

        private void ExportBaseLineDataImpl()
        {
            Logger.logBegin("ExportBaseLineDataImpl");
            try
            {
                if ((this.SelectedStoryboardRows==null)||(this.SelectedStoryboardRows.Count<=0))
                {
                    ViewModelBase.HintByMessageBox("Please select at least one Item to export.");
                    return;
                }
                List<long> lstStryBrdDtlToExport = this._selectedStoryboardRows.Select(p => p.StoryboardDetailId).ToList();

                OpenFileDialog dlgOpenFile = new OpenFileDialog()
                {
                    Filter = "*.xml|*.cfg",
                    CheckFileExists= false,
                    InitialDirectory = SystemCommonUtil.GetCurrentPathDir(),
                };

                bool? isFiled =dlgOpenFile.ShowDialog();
                if ((!isFiled.HasValue)||(!isFiled.Value))
                {
                    return;
                }

                string strError = "", strDesFileName= dlgOpenFile.FileName;
                XmlBaseLineExportImportMgr objBaseLineExpMgr = new XmlBaseLineExportImportMgr();
                objBaseLineExpMgr.CurrentStoryboardName = this._storyboardName;
                objBaseLineExpMgr.TestMode = "BASELINE";
                objBaseLineExpMgr.CurrentStoryboardId = this.StoryboardId;
                bool isOk = objBaseLineExpMgr.InitDataFromDataBase(lstStryBrdDtlToExport,ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Error When Fetch Data From DB:[{0}]", strDesFileName));
                }
                
                isOk = objBaseLineExpMgr.ExportBaselineDataByStoryBoardIds(lstStryBrdDtlToExport, strDesFileName, ref strError);
                if (isOk)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Baseline data has already exported to :[{0}]", strDesFileName));
                    return;
                }
                ViewModelBase.HintByMessageBox(string.Format("Error when export base line data:[{0}]", strError), "ERROR");                
            }
            finally
            {
                Logger.logEnd("ExportBaseLineDataImpl");
            }
        }

        private void pasteSelectedRows()
        {
            //string reason;
            #region copy from system clipboard
            if (IsClipBoardFormatRight())
            {
                ///处理数据 
                /// 
                string strError = "";
                bool isOk = PasteDataFromExcel(ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox("Can't copy Storyboard Items from Clipboard, with Error:\r\n{0}",strError);
                }
                return;
            }
            #endregion

            if (isSelectionInSequence())
            {
                int indexOfSelectedRow = StoryboardRows.IndexOf(SelectedStoryboardRows[0]);
                
                if (StoryboardClipBoard.storyboardClipBoard == null) return;
                foreach (StoryboardEditViewModel selectedRow in StoryboardClipBoard.storyboardClipBoard)
                {
                    StoryboardEditViewModel storyboard = selectedRow.CloneObj();
                    storyboard.StoryboardDetailId = -1;
                    storyboard.StoryboardId = this.StoryboardId;
                    //storyboard.StoryboardId = 
                    if (indexOfSelectedRow < StoryboardRows.Count)
                    {
                        StoryboardRows[indexOfSelectedRow] = storyboard;
                    }
                    else
                    {
                        StoryboardRows.Add(storyboard);
                    }
                    storyboard.AliasName = null;
                    indexOfSelectedRow++;
                }

                int runOrder = 1;
                foreach (StoryboardEditViewModel sbRow in StoryboardRows)
                 {
                     sbRow.RunOrder = runOrder++;
                 }
                RebuldDependOnSteps();
            }
        }

        private bool isSelectionInSequence()
        {
            if (SelectedStoryboardRows.Count == 0)
            {
                System.Windows.MessageBox.Show("Select a row to perform this action", "Storyboards", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            int indexOfSelectedTestCase = StoryboardRows.IndexOf(SelectedStoryboardRows[0]);
            for (int i = 0; i < SelectedStoryboardRows.Count; i++)
            {
                if (StoryboardRows.ElementAt(indexOfSelectedTestCase) != SelectedStoryboardRows[i])
                {
                    System.Windows.MessageBox.Show("The Selection is not sequence", "Storyboards", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                indexOfSelectedTestCase++;

            }
            return true;
        }

        private void LoadProjectData(long  projectId)
        {
            projectData = BoHelper.GetProjectData(currentDBIdx, projectId);
        }

        private ObservableCollection<StoryboardEditViewModel> _storyboardRows;
        public ObservableCollection<StoryboardEditViewModel> StoryboardRows
        {
            get
            {
                return _storyboardRows;
            }
            set
            {
                _storyboardRows = value;
                OnPropertyChanged("StoryboardRows");
            }
        }

        private long _storyboardId;

        public long StoryboardId
        {
            get { return _storyboardId; }
            set { _storyboardId = value; }
        }

        private string _storyBoardDescription;
        public string StoryBoardDescription
        {
            get { return _storyBoardDescription; }
            set { _storyBoardDescription = value; }
        }

        private long _pojectId;

        public long PojectId
        {
            get { return _pojectId; }
            set { _pojectId = value; }
        }


        public IList<StoryboardEditViewModel> _selectedStoryboardRows;
        private string _projectName;
        private string _storyboardName;
       
       
        public IList<StoryboardEditViewModel> SelectedStoryboardRows
        {
            get
            {
                return _selectedStoryboardRows;
            }
            set
            {
                _selectedStoryboardRows = value;
                OnPropertyChanged("SelectedStoryboardRows");
            }
        }

        internal class MarsDealThread
        {
            internal Thread tmpThread = null;
            internal List<B_STORYBOARD_TEST_FULLVISION> LstToUpdate = new List<B_STORYBOARD_TEST_FULLVISION>();
            internal string _storyboardName;
            internal void beginThread()
            {
                tmpThread = new Thread(DealwithStoryboard);
                tmpThread.Start();
            }
            internal List<StoryboardEditViewModel> resultToReturn = new List<StoryboardEditViewModel>();
            internal ObservableCollection<StoryboardEditViewModel> _storyboardRows = null;
            //internal StoryboardEditViewModel parentModel;
            internal StoryboardColl parentCol;
            internal OnStoryboardStepNameChangeEvent StoryboardDetailAliasChangeImpl;
            internal void join()
            {
                tmpThread.Join();
            }
            void DealwithStoryboard()
            {
                
                foreach (B_STORYBOARD_TEST_FULLVISION storyboardRow in LstToUpdate)
                {
                    StoryboardEditViewModel devm;
                    if (storyboardRow == null) continue;
                    try
                    {
                        if (storyboardRow.RUN_ORDER == -1)
                        {
                            devm = new StoryboardEditViewModel(new StoryboardEditViewModel(), 1, _storyboardName, ref _storyboardRows, parentCol);
                            devm.StorboardStepNameChangeHandler = StoryboardDetailAliasChangeImpl;
                        }
                        else
                        {
                            devm = new StoryboardEditViewModel(parentCol,
                                                                storyboardRow.STORYBOARD_ID,
                                                                storyboardRow.STORYBOARD_DETAIL_ID,
                                                                storyboardRow.PROJECT_ID,
                                                                storyboardRow.PROJECT_NAME,
                                                                storyboardRow.PROJECT_DESCRIPTION,
                                                                storyboardRow.TEST_CASE_NAME,
                                                                storyboardRow.TEST_CASE_ID,
                                                                storyboardRow.TEST_STEP_DESCRIPTION,
                                                                storyboardRow.TEST_SUITE_ID,
                                                                storyboardRow.TEST_SUITE_NAME,
                                                                storyboardRow.TEST_SUITE_DESCRIPTION,
                                                                storyboardRow.RUN_ORDER,
                                                                storyboardRow.DEPENDS_ON,
                                                                storyboardRow.ALIAS_NAME,
                                                                storyboardRow.DISPLAY_NAME,
                                                                storyboardRow.PARENT_ALIAS_NAME,
                                                                storyboardRow.TEST_RUN_VALUE,
                                                                storyboardRow.LATEST_TEST_MARK_ID,
                                                                storyboardRow.HIST_LATEST_TEST_MARK_ID,
                                                                storyboardRow.HIST_ID,
                                                                storyboardRow.HIST_TEST_ID,
                                                                storyboardRow.TEST_CASE_BEGIN_TIME,
                                                                storyboardRow.TEST_CASE_END_TIME,
                                                                storyboardRow.HIST_TEST_RESULT_IN_TEXT,
                                                                storyboardRow.HIST_TEST_MODE,
                                                                storyboardRow.HIST_RESULT,
                                                                storyboardRow.DATA_SUMMARY_ID,
                                                                storyboardRow.DATA_SET_ALIAS_NAME,
                                                                storyboardRow.DATASET_DESCRIPTION);


                            resultToReturn.Add(devm);
                            devm.StorboardStepNameChangeHandler = StoryboardDetailAliasChangeImpl;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("DealwithStoryboard", string.Format("Exception:[{0}] stackTrace:[{1}]", ex.Message, ex.StackTrace));
                    }
                }
            }
        }

        private  void LoadStoryboardRows(long storyboardId)
        {
            Logger.logBegin("LoadStoryboardRows");
            StoryboardRows = new ObservableCollection<StoryboardEditViewModel>();
            /// tiger 增加
            /// 
            Logger.Info("\t", "time mark LoadProjectData");
            LoadProjectData(_pojectId);
            Logger.Info("\t", "time mark LoadProjectData end");
            /// end

            // B_STORYBOARD_TEST_FULLVISION bStoryboardRow = new B_STORYBOARD_TEST_FULLVISION();

            //Logger.Info("LoadStoryboardRows", string.Format("----PerformanceTest:{0}", lTm = DateTime.Now.Ticks));
            List<B_STORYBOARD_TEST_FULLVISION> bStoryboardRows = BoHelper.GetStoryboardRows(currentDBIdx, storyboardId);
            B_STORYBOARD_SUMMARY objStbInfo = B_STORYBOARD_SUMMARY.GetStoryBoardInfoById(currentDBIdx, storyboardId);
            if (objStbInfo == null)
                _storyBoardDescription = "N/A";
            else
                _storyBoardDescription = objStbInfo.DESCRIPTION;

            #region trash,try to increase performance
            //Logger.Info("LoadStoryboardRows", string.Format("----PerformanceTest cost:{0}", DateTime.Now.Ticks-lTm));
            //Logger.Info("LoadStoryboardRows", string.Format("----PerformanceTest:{0}", lTm = DateTime.Now.Ticks));
            /////增加线程处理
            /////注意：
            /////1，每个线程处理10个
            /////2，每次最后需要StoryboardDetailAliasChangeImpl 重新赋值
            /////3，
            /////
            //Logger.Info("\t", "Thread deal begin");
            //List<MarsDealThread> lstThrd = new List<MarsDealThread>();
            //List<B_STORYBOARD_TEST_FULLVISION> lstVision = bStoryboardRows.OrderBy(x => x.RUN_ORDER).ToList();
            /////计算需要多少个线程
            /////
            //int iThrdCount = lstVision.Count / 10+lstVision.Count%10>0?1:0;
            //MarsDealThread currrentThrd = null;

            //for (int i = 0; i < lstVision.Count; i++)
            //{
            //    if (i % 10 == 0)
            //    {
            //        currrentThrd = new MarsDealThread();
            //        currrentThrd.parentCol = this;
            //        currrentThrd.LstToUpdate = new List<B_STORYBOARD_TEST_FULLVISION>();

            //        lstThrd.Add(currrentThrd);
            //    }
            //    currrentThrd.LstToUpdate.Add(lstVision[i]);
            //}
            //Logger.Info("\t", string.Format("total thread count:[{0}]", lstThrd.Count));
            //for (int i = 0; i < lstThrd.Count; i++)
            //{
            //    lstThrd[i].beginThread();
            //}
            //for (int i = 0; i < lstThrd.Count; i++)
            //{
            //    lstThrd[i].join();
            //}
            ////最后将数据拼到
            //List<StoryboardEditViewModel> lstResult = new List<StoryboardEditViewModel>();
            //for (int i = 0; i < lstThrd.Count; i++)
            //{
            //    lstResult.AddRange(lstThrd[i].resultToReturn);
            //}
            //StoryboardRows.AddRange(lstResult.OrderBy(x => x.RunOrder));
            //Logger.Info("\t", "Thread deal end");
            #endregion trash,try to increase performance 
            #region original code

            foreach (B_STORYBOARD_TEST_FULLVISION storyboardRow in bStoryboardRows.OrderBy(x=> x.RUN_ORDER))
            {
                StoryboardEditViewModel devm;
                try
                {
                    if (storyboardRow.RUN_ORDER == -1)
                    {
                        devm = new StoryboardEditViewModel(new StoryboardEditViewModel(), 1, _storyboardName, ref _storyboardRows, this);
                        devm.StorboardStepNameChangeHandler = StoryboardDetailAliasChangeImpl;
                    }
                    else
                    {
                        devm = new StoryboardEditViewModel(this,
                                                        storyboardRow.STORYBOARD_ID,
                                                        storyboardRow.STORYBOARD_DETAIL_ID,
                                                        storyboardRow.PROJECT_ID,
                                                        storyboardRow.PROJECT_NAME,
                                                        storyboardRow.PROJECT_DESCRIPTION,
                                                        storyboardRow.TEST_CASE_NAME,
                                                        storyboardRow.TEST_CASE_ID,
                                                        storyboardRow.TEST_STEP_DESCRIPTION,
                                                        storyboardRow.TEST_SUITE_ID,
                                                        storyboardRow.TEST_SUITE_NAME,
                                                        storyboardRow.TEST_SUITE_DESCRIPTION,
                                                        storyboardRow.RUN_ORDER,
                                                        storyboardRow.DEPENDS_ON,
                                                        storyboardRow.ALIAS_NAME,
                                                        storyboardRow.DISPLAY_NAME,
                                                        storyboardRow.PARENT_ALIAS_NAME,
                                                        storyboardRow.TEST_RUN_VALUE,
                                                        storyboardRow.LATEST_TEST_MARK_ID,
                                                        storyboardRow.HIST_LATEST_TEST_MARK_ID,
                                                        storyboardRow.HIST_ID,
                                                        storyboardRow.HIST_TEST_ID,
                                                        storyboardRow.TEST_CASE_BEGIN_TIME,
                                                        storyboardRow.TEST_CASE_END_TIME,
                                                        storyboardRow.HIST_TEST_RESULT_IN_TEXT,
                                                        storyboardRow.HIST_TEST_MODE,
                                                        storyboardRow.HIST_RESULT,
                                                        storyboardRow.DATA_SUMMARY_ID,
                                                        storyboardRow.DATA_SET_ALIAS_NAME,
                                                        storyboardRow.DATASET_DESCRIPTION);

                        //Console.WriteLine("dashboardRow.RUN_ORDER = " + storyboardRow.RUN_ORDER);
                        //Console.WriteLine("dashboardRow.DISPLAY_NAME = " + storyboardRow.DISPLAY_NAME);
                        //Console.WriteLine("dashboardRow.DATA_SUMMARY_ID = " + storyboardRow.DATA_SUMMARY_ID);
                        //Console.WriteLine("=======");
                        // For Testing only
                        //devm.HistResult = 1000;
                        //devm.TestCaseBeginTime = DateTime.Now;
                        //devm.TestCaseEndTime = DateTime.Now;
                        StoryboardRows.Add(devm);
                        devm.StorboardStepNameChangeHandler = StoryboardDetailAliasChangeImpl;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("LoadStoryboardRows",string.Format("Exception:[{0}] stackTrace:[{1}]",ex.Message,ex.StackTrace));
                }
                //Logger.Info("LoadStoryboardRows", string.Format("----PerformanceTest Load cost:{0}", DateTime.Now.Ticks - lTm));
            }
            
            #endregion
            Logger.logEnd("LoadStoryboardRows");
            
        }
        public void DeleteStoryboard(long? storyboardId)
        {
            // Remove from DB
            string strError = "";
            //MarsEntities marsEntities = null;
            int iCnt = BoHelper.DeleteStoryboardAndDependents(MarsMainWindow.CurrentDatabaseIdx, storyboardId,ref strError);
            if (iCnt > 0)
            {
                StoryboardCache.removeStoryboard((long)storyboardId);
                ViewModelBase.HintByMessageBox(string.Format("delete storyboard id [{0}] finished, \r\ntotal [{1}] records are affected.",storyboardId, iCnt));
            }
            else
            {
                ViewModelBase.HintByMessageBox(string.Format("delete storyboard id [{0}] failed, with error:\r\n{1}", storyboardId, strError));
            }
            // Remove from Cache

            // Update tree 
        }

#region Tiger added part
        private ObservableCollection<MarsKeyValues<string, string>> assignedApplication=new ObservableCollection<MarsKeyValues<string, string>>();
        public ObservableCollection<MarsKeyValues<string, string>> AssignedApplication
        {
            get { return assignedApplication; }
            set { assignedApplication = value; OnPropertyChanged("AssignedApplication"); }
        }
        private ObservableCollection<MarsKeyValues<string, string>> uninstalledApplications=new ObservableCollection<MarsKeyValues<string, string>>();
        public ObservableCollection<MarsKeyValues<string, string>> UninstalledApplciations
        {
            get { return uninstalledApplications; }
            set { uninstalledApplications = value;OnPropertyChanged("UninstalledApplciation"); }
        }

        internal void LoadAssignedApplicationByProjectAndStoryboard(long projectId, long storyboardId)
        {
            List<MarsKeyValues<string, string>> lstUnavailableApps = new List<MarsKeyValues<string, string>>();
            ERROR_CODE eCde = ERROR_CODE._NO_ERROR;
            IList<MarsKeyValues<string, string>> objAvailableLst = MarsTestFrameBusinessLogic.GetAvailableAppsByProjectIdAndStoryBoardID(MarsMainWindow.CurrentDatabaseIdx, projectId, storyboardId, ref eCde,ref lstUnavailableApps);
            if (eCde!=ERROR_CODE._NO_ERROR)
            {
                assignedApplication.Clear();               
            }
            else
            {
                assignedApplication.Clear();
                CopyIListToObserverCollection<MarsKeyValues<string, string>>(objAvailableLst, assignedApplication);
                OnPropertyChanged("AssignedApplication");
            }
            //TargetApplicationsManagement.CheckInstalledApps(((MarsKeyValues<string, string>)AvailableProjects.SelectedItem).Children, ref lstUnInstalled);
        }

        private void CopyIListToObserverCollection<T>(IList<T> lstSource, ObservableCollection<T> lstTarget)
        {
            if (lstTarget == null) return;
            if (lstSource == null) return;
            foreach(T oItem in lstSource)
            {
                lstTarget.Add(oItem);
            }
        }

        //private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardDataGrid));
        /// <summary>
        ///  for Testing 
        /// </summary>
        /// 
        //private static MarsTestFrameMain MarsTestingFrame = new MarsTestFrameMain();
        //private string _storyBoardName;

        //internal void StartTest(string strApplicationShortName, string strApplicationId)
        //{
        //    Logger.logBegin("StartTest");
        //    MarsTestingFrame.StopService();
        //    MarsTestingFrame.CurrentTestProjectName = _storyBoardName;
        //    MarsTestingFrame.CurrentTestApplicationShortName = strApplicationShortName;
        //    MarsTestingFrame.RunTestBatchFileByThread(_storyBoardName, FrameWorkStartMode.FWSM_Normal, this.StoryboardId+"", strApplicationId);
        //    /// prepare items before Test services begin
        //    /// 
        //    //string strStoryBoardId = 

        //}
#endregion //Tiger added part


        public string Title { get; set; }

        internal long SaveAs(string newStoryboardName)
        {
            Logger.logBegin("SaveAs",string.Format("from storyboard id [{1}] to New storyboard Name:[{0}]",newStoryboardName, StoryboardId));

            #region code should move to transaction -- SaveStoryboard

            //long id = CreateStoryboard(newStoryboardName);

            //this.StoryboardId = id;
            //foreach (var row in this.StoryboardRows)
            //{
            //    row.StoryboardDetailId = -1;
            //    row.StoryboardId = id;
            //}
            #endregion

            this.SaveStoryboard(newStoryboardName);
            //return id;
            return StoryboardId;
        }

        private long CreateStoryboard(string StoryboardName,MarsEntities dbCntx)
        {
            Console.WriteLine("storyboard " + StoryboardName + " description " + StoryboardName);
            B_STORYBOARD_SUMMARY summary = new B_STORYBOARD_SUMMARY();
            summary.STORYBOARD_ID = BoHelper.GetTestStepsId(dbCntx);
            summary.STORYBOARD_NAME = StoryboardName;
            summary.ASSIGNED_PROJECT_ID = this.PojectId;
            summary.DESCRIPTION = StoryboardName;
            BoHelper.SaveStoryboardSummary(MarsMainWindow.CurrentDatabaseIdx, summary);
            BoHelper.SaveChanges(MarsMainWindow.CurrentDatabaseIdx);
            return summary.STORYBOARD_ID;
        }

        internal void ChangeActionsByDepends()
        {
            var allRunable = _storyboardRows
                .Where(p => (string.Compare("RUN", p.DisplayName, true) == 0 || (string.Compare("EXECUTE", p.DisplayName, true) == 0)))
                .OrderBy(p=>p.RunOrder)
                .ToList();
            if (allRunable == null) return;
            bool isChange = false;
            foreach (var itm in allRunable)
            {
                if (itm == null) continue;
                if (itm.DependOnSteps == null) continue;
                if (itm.SelectedDependOnStep == null) continue;
                if (string.Compare("NONE", itm.SelectedDependOnStep.DataItemName, true) == 0) continue;
                if (!((string.Compare("EXECUTE", itm.DisplayName, true) == 0) || (string.Compare("RUN", itm.DisplayName, true) == 0))) continue;
                isChange = ChangeActionsByDepends(_storyboardRows, itm, (int)itm.RunOrder,false);
            }
            if (isChange)
            {
                SaveCommand.Execute(null);
            }
        }

        private bool ChangeActionsByDepends(ObservableCollection<StoryboardEditViewModel> lstStoryboardRows, StoryboardEditViewModel srcItm, int iLevel, bool isChanged)
        {
            if (srcItm == null) return false;
            var itm = lstStoryboardRows.Where(p => p.StoryboardDetailId == srcItm.SelectedDependOnStep.Id).FirstOrDefault();
            if (itm == null)
            {
                srcItm.SelectedDependOnStep = null;
                return false;
            }
            //if (itm.SelectedDependOnStep.Id == -1) return false;
            if ((iLevel + 1) > lstStoryboardRows.Count) return false;
            if (string.Compare(itm.DisplayName, "EXECUTE", true) != 0)
            {
                itm.DisplayName = srcItm.DisplayName;
                isChanged = true;
            }
                
            return ChangeActionsByDepends(lstStoryboardRows, itm, iLevel + 1, isChanged) ||isChanged;
        }
    }

    public delegate void OnStoryboardStepNameChangeEvent(Notify sourceStoryBoardRowMode);

    [Serializable]
    public class StoryboardEditViewModel : Notify
    {
        public static ObservableCollection<StoryboardEditViewModel> dashBoardColl;

        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardEditViewModel));

        public StoryboardEditViewModel()
        {
            DisplayName = "RUN";
            
        }

        private bool hasDealedBySaveMark = false;
        public bool HasDealedBySaveMark
        {
            get
            {
                return hasDealedBySaveMark;
            }
            set
            {
                hasDealedBySaveMark = value;
            }
        }

        string _selectedAction;

        public string SelectedAction
        {
            get { return _selectedAction; }
            set
            {
                _selectedAction = value;

            }
        }


        public StoryboardEditViewModel CloneObj()
        {
            StoryboardEditViewModel newVm = new StoryboardEditViewModel
            (
                storyboardColl,
                StoryboardId,
                StoryboardDetailId,
                ProjectId,
                ProjectName,
                PojectDescription,
                TestCaseName,
                TestCaseId,
                TestCaseDescription,
                TestSuiteId,
                TestSuiteName,
                TestSuiteDescription,
                RunOrder,
                RelyOn,
                AliasName,
                DisplayName,
                ParentAliasName,
                TestRunValue,
                LatestTestMarkId,
                HistLatestTestMarkId,
                HistId,
                HistTestId,
                TestCaseBeginTime,
                TestCaseEndTime,
                HistTestResultInText,
                HistTestMode,
                HistResult,
                DataSummaryId,
                DataSetAiliasName,
                DataSetDescription
                );

            //newVm.StoryboardId = BoHelper.GetDataSettingId();
            newVm.StoryboardId = storyboardColl.StoryboardId;
            newVm.TestSuites = TestSuites;
            newVm.TestCases = TestCases;
            newVm.DataSetNames = DataSetNames;

            newVm.SelectedAction = SelectedAction;
            //newVm.SelectedTestSuite = new DataItem(SelectedTestSuite.Id, SelectedTestSuite.DataItemName);
            newVm.SelectedTestSuite = newVm.TestSuites.Where(a => a.Id == SelectedTestSuite.Id).FirstOrDefault();
            newVm.SelectedTestCase = newVm.TestCases.Where(a => a.Id == SelectedTestCase.Id).FirstOrDefault();
            newVm.SelectedDataSetName = newVm.DataSetNames.Where(a => a.Id == SelectedDataSetName.Id).FirstOrDefault();
           // newVm.SelectedTestCase = new DataItem(SelectedTestCase.Id, SelectedTestCase.DataItemName);
            //newVm.SelectedDataSetName = new DataItem(SelectedDataSetName.Id, SelectedDataSetName.DataItemName);
            return newVm;
        }

        public StoryboardEditViewModel(StoryboardColl storyboardColl,
                                        Int64 storyboardId,
                                        Int64 storyboardDetailId,
                                        Int64 projectId,
                                        String projectName,
                                        String pojectDescription,
                                        String testCaseName,
                                        Int64 testCaseId,
                                        String testCaseDescription,
                                        Nullable<Int64> testSuiteId,
                                        String testSuiteName,
                                        String testSuiteDescription,
                                        Int64 runOrder,
                                        Nullable<Int64> relyOn,
                                        String aliasName,
                                        String displayName,
                                        String parentAliasName,
                                        Nullable<Int16> testRunValue,
                                        Nullable<Int64> latestTestMarkId,
                                        Nullable<Int64> histLatestTestMarkId,
                                        Nullable<Int64> histId,
                                        Nullable<Int64> histTestId,
                                        Nullable<DateTime> testCaseBeginTime,
                                        Nullable<DateTime> testCaseEndTime,
                                        String histTestResultInText,
                                        Nullable<Int16> histTestMode,
                                        Nullable<Int16> histResult,
                                        Nullable<Int64> dataSummaryId,
                                        String dataSetAiliasName,
                                        String dataSetDescription

            )
        {
            this.storyboardColl = storyboardColl;
            //StoryboardId = storyboardId;
            _storyboardId = storyboardId;
            _storyboardDetailId = storyboardDetailId;
            _projectId = projectId;
            _projectName = projectName;
            _pojectDescription = pojectDescription;
            _testCaseName = testCaseName;
            _testCaseId = testCaseId;
            _testCaseDescription = testCaseDescription;
            _testSuiteId = testSuiteId;
            _testSuiteName = testSuiteName;
            _testSuiteDescription = testSuiteDescription;
            _runOrder = runOrder;
            _relyOn = relyOn;
            _aliasName = aliasName;
            _displayName = displayName;
            _parentAliasName = parentAliasName;
            _testRunValue = testRunValue;
            _latestTestMarkId = latestTestMarkId;
            _histLatestTestMarkId = histLatestTestMarkId;
            _histId = histId;
            _histTestId = histTestId;
            _testCaseBeginTime = testCaseBeginTime;
            _testCaseEndTime = testCaseEndTime;
            _histTestResultInText = histTestResultInText;
            _histTestMode = histTestMode;
            _histResult = histResult;
            _dataSummaryId = dataSummaryId;
            _dataSetAiliasName = dataSetAiliasName;
            _dataSetDescription = dataSetDescription;

            // Init lists to support combo boxes
            _testSuites = PopulateTestSuites();
            _testCases = PopulateTestCases(_testSuiteId??-1);
            //PopulateTestCases(TestSuiteName);
            _dataSetNames = PopulateDataSetNames(_testCaseId);
            //PopulateDataSetNames(TestCaseName);

            DependOnSteps = PopulateDependOnSteps();  //_dependOnSteps = PopulateDependOnSteps();


            //Console.WriteLine("Done");

            SelectedTestSuite = TestSuites.Where(a => a.Id == TestSuiteId).FirstOrDefault();

            SelectedTestCase = TestCases.Where(a => a.Id == TestCaseId).FirstOrDefault();

            SelectedDataSetName = DataSetNames.Where(a => a.Id == this.DataSummaryId).FirstOrDefault();

            SelectedDependOnStep = DependOnSteps.Where(a => a.Id == this.RelyOn).FirstOrDefault();


            //VisualTreeHelper.GetParent()
        }

        public ObservableCollection<DataItem> PopulateDependOnSteps(bool isUpdate=true)
        {
            ObservableCollection<DataItem> steps = new ObservableCollection<DataItem>();

            DataItem dataItem = new DataItem(-1, "NONE");
            steps.Add(dataItem);

            foreach (var step in storyboardColl.StoryboardRows)
            {
                if (step == this)
                    break;
                if (string.IsNullOrEmpty(step.AliasName)) continue;
                // AF dataItem = new DataItem(step.StoryboardId, step.AliasName);
                dataItem = new DataItem(step.StoryboardDetailId, step.AliasName);
                dataItem.AssignedObj = step;
                steps.Add(dataItem);
            }
            if (isUpdate)
                SelectedDependOnStep = steps.Where(x => x.DataItemName.Equals("NONE")).FirstOrDefault();
            else {
                if (_selectedDependOnStep == null) return steps; 

                if (_selectedDependOnStep.AssignedObj ==null)
                {
                    SelectedDependOnStep = steps.Where(x => x.DataItemName.Equals("NONE")).FirstOrDefault();
                    return steps;
                }
                var itm = steps.Where(p => p.Id == _selectedDependOnStep.Id).FirstOrDefault();
                if (itm == null) return steps;
                SelectedDependOnStep = itm;
            }

           return steps;
        }

        public StoryboardEditViewModel(StoryboardEditViewModel lastRowViewmodel, long runOrder, string storyboardName, ref ObservableCollection<StoryboardEditViewModel> _storyboardRows, StoryboardColl storyboardColl)
        {
            // TODO: Complete member initialization
            this.lastRowViewmodel = lastRowViewmodel;
            this.RunOrder = runOrder;
            this.storyboardName = storyboardName;
            this._storyboardRows = _storyboardRows;
            this.storyboardColl = storyboardColl;
            this.StoryboardId = storyboardColl.StoryboardId;
            this.ProjectId = storyboardColl.PojectId;
            this.DisplayName = "RUN";
            _testSuites = PopulateTestSuites();
            SelectedTestSuite = TestSuites.Where(a => a.Id == TestSuiteId).FirstOrDefault();
        }


#region Data Members
        private Int64 _storyboardId;

        public Int64 StoryboardId
        {
            get { return _storyboardId; }
            set
            {
                _storyboardId = value;
                OnPropertyChanged("StoryboardId");
            }
        }

        private Int64 _storyboardDetailId;


        public Int64 StoryboardDetailId
        {
            get { return _storyboardDetailId; }
            set 
            { 
                _storyboardDetailId = value;
                OnPropertyChanged("StoryboardDetailId");
            }
        }

        private Int64 _projectId;

        public Int64 ProjectId
        {
            get { return _projectId; }
            set
            {
                _projectId = value;
                OnPropertyChanged("ProjectId");
            }
        }

        private String _projectName;

        public String ProjectName
        {
            get { return _projectName; }
            set
            {
                _projectName = value;
                OnPropertyChanged("ProjectName");
            }
        }

        private String _pojectDescription;

        public String PojectDescription
        {
            get { return _pojectDescription; }
            set
            {
                _pojectDescription = value;
                OnPropertyChanged("PojectDescription");
            }
        }

        public String ToolTipString
        {
            get { return
                "Descr: " + _testCaseDescription + 
                "\nSB Id: " + _storyboardId +
                "\nSB Det Id: " + _storyboardDetailId +
                "\nTC Id: " + _testCaseId + 
                "\nProject Id: " + _projectId; 
            }
            
        }

        private String _testCaseName;

        public String TestCaseName
        {
            get { return _testCaseName; }
            set
            {
                _testCaseName = value;
                OnPropertyChanged("TestCaseName");
            }
        }

        private Int64 _testCaseId;

        public Int64 TestCaseId
        {
            get { return _testCaseId; }
            set
            {
                _testCaseId = value;
                OnPropertyChanged("TestCaseId");
            }
        }
        private String _testCaseDescription;

        public String TestCaseDescription
        {
            get { return _testCaseDescription; }
            set
            {
                _testCaseDescription = value;
                OnPropertyChanged("TestCaseDescription");
            }
        }
        private Nullable<Int64> _testSuiteId;

        public Nullable<Int64> TestSuiteId
        {
            get { return _testSuiteId; }
            set
            {
                _testSuiteId = value;
                OnPropertyChanged("TestSuiteId");
            }
        }

        private String _testSuiteDescription;

        public String TestSuiteDescription
        {
            get { return _testSuiteDescription; }
            set
            {
                _testSuiteDescription = value;
                OnPropertyChanged("TestSuiteDescription");
            }
        }

        private String _testSuiteName;

        public String TestSuiteName
        {
            get { return _testSuiteName; }
            set
            {
                _testSuiteName = value;
                OnPropertyChanged("TestSuiteName");
            }
        }

        private Int64 _runOrder;

        public Int64 RunOrder
        {
            get { return _runOrder; }
            set
            {
                _runOrder = value;
                OnPropertyChanged("RunOrder");
            }
        }

        private Nullable<Int64> _relyOn;

        public Nullable<Int64> RelyOn
        {
            get { return _relyOn; }
            set
            {
                _relyOn = value;
                OnPropertyChanged("RelyOn");
            }
        }

        public OnStoryboardStepNameChangeEvent StorboardStepNameChangeHandler = null;

        private String _aliasName;
        
        public String AliasName
        {
            get { return _aliasName; }
            set
            {
                if (string.Compare(_aliasName, value) == 0) return;
                
                _aliasName = value;
                //Console.WriteLine("AliasName=" + _aliasName);
                OnPropertyChanged("AliasName");

                //update all referenced other steps
                if (StorboardStepNameChangeHandler != null)
                    StorboardStepNameChangeHandler(this);
            }
        }
        private String _displayName;
        public bool changeSelectedDisplay = true;
        public String DisplayName
        {
            get { return _displayName; }
            set
            {
                _displayName = value;
                if (!changeSelectedDisplay) return;

                if (storyboardColl!=null)
                {
                    ///增加自动改变所有的选中的行的数据
                    if (storyboardColl.SelectedStoryboardRows!=null)
                    {
                        foreach(var itm in storyboardColl.SelectedStoryboardRows)
                        {
                            if (itm == null) continue;
                            if (itm.RunOrder == this._runOrder) continue;
                            //if (string.IsNullOrEmpty(itm._displayName))
                            if (string.Compare(itm._displayName, value) == 0) continue;
                                itm._displayName = _displayName;
                            itm.OnPropertyChanged("DisplayName");
                        }
                    }
                }
                OnPropertyChanged("DisplayName");
            }
        }

        private String _parentAliasName;

        public String ParentAliasName
        {
            get { return _parentAliasName; }
            set
            {
                _parentAliasName = value;
                OnPropertyChanged("ParentAliasName");
            }
        }

        private Nullable<Int16> _testRunValue;

        public Nullable<Int16> TestRunValue
        {
            get { return _testRunValue; }
            set
            {
                _testRunValue = value;
                OnPropertyChanged("TestRunValue");
            }
        }
        private Nullable<Int64> _latestTestMarkId;

        public Nullable<Int64> LatestTestMarkId
        {
            get { return _latestTestMarkId; }
            set
            {
                _latestTestMarkId = value;
                OnPropertyChanged("LatestTestMarkId");
            }
        }
        private Nullable<Int64> _histLatestTestMarkId;

        public Nullable<Int64> HistLatestTestMarkId
        {
            get { return _histLatestTestMarkId; }
            set
            {
                _histLatestTestMarkId = value;
                OnPropertyChanged("HistLatestTestMarkId");
            }
        }
        private Nullable<Int64> _histId;

        public Nullable<Int64> HistId
        {
            get { return _histId; }
            set
            {
                _histId = value;
                OnPropertyChanged("HistId");
            }
        }
        private Nullable<Int64> _histTestId;

        public Nullable<Int64> HistTestId
        {
            get { return _histTestId; }
            set
            {
                _histTestId = value;
                OnPropertyChanged("HistTestId");
            }
        }
        private Nullable<DateTime> _testCaseBeginTime;

        public Nullable<DateTime> TestCaseBeginTime
        {
            get { return _testCaseBeginTime; }
            set
            {
                _testCaseBeginTime = value;
                OnPropertyChanged("TestCaseBeginTime");
            }
        }
        private Nullable<DateTime> _testCaseEndTime;

        public Nullable<DateTime> TestCaseEndTime
        {
            get { return _testCaseEndTime; }
            set
            {
                _testCaseEndTime = value;
                OnPropertyChanged("TestCaseEndTime");
            }
        }
        private String _histTestResultInText;

        public String HistTestResultInText
        {
            get { return _histTestResultInText; }
            set
            {
                _histTestResultInText = value;
                OnPropertyChanged("HistTestResultInText");
                OnPropertyChanged("HistTestResultInTextToolTip");
            }
        }

        public String HistTestResultInTextToolTip
        {
            get 
            {
                string formatted = "";
                if (_histTestResultInText != null)
                {
                    formatted.Replace("Exception", "\n Exception");
                }

                return formatted; 
            }
            
        }

        private Nullable<Int16> _histTestMode;

        public Nullable<Int16> HistTestMode
        {
            get { return _histTestMode; }
            set
            {
                _histTestMode = value;
                OnPropertyChanged("HistTestMode");
            }
        }

        // WARNING : these members are auto created -- should be removed!!!
        private Nullable<Int16> _histResult;
        //private StoryboardEditViewModel storyboardEditViewModel;
        //private int p;
        private string storyboardName;
        private ObservableCollection<StoryboardEditViewModel> _storyboardRows;
        private StoryboardEditViewModel lastRowViewmodel;

        public Nullable<Int16> HistResult
        {
            get { return _histResult; }
            set
            {
                _histResult = value;
                OnPropertyChanged("HistResult");
            }
        }


        Nullable<Int64> _dataSummaryId;

        public Nullable<Int64> DataSummaryId
        {
            get { return _dataSummaryId; }
            set
            {
                _dataSummaryId = value;
                OnPropertyChanged("DataSummaryId");
            }
        }

        String _dataSetAiliasName;

        public String DataSetAiliasName
        {
            get { return _dataSetAiliasName; }
            set
            {
                _dataSetAiliasName = value;
                OnPropertyChanged("DataSetAiliasName");
            }
        }

        String _dataSetDescription;

        public String DataSetDescription
        {
            get { return _dataSetDescription; }
            set
            {
                _dataSetDescription = value;
                OnPropertyChanged("DataSetDescription");
            }
        }


#endregion

#region Data in support of ComboBoxes


        public class DataItem : Notify
        {
            public object AssignedObj;
            public DataItem()
            {

            }
            public DataItem(long id, string name)
            {
                _id = id;
                _dataItemName = name;
            }

            public DataItem(long id, string name, string description)
            {
                _id = id;
                _dataItemName = name;
                _dataItemDescription = description;
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

            private string _dataItemName;
            public string DataItemName
            {
                get { return _dataItemName; }
                set
                {
                    _dataItemName = value;
                    OnPropertyChanged("DataItemName");
                }
            }

            private string _dataItemDescription;
            public string DataItemDescription
            {
                get { return _dataItemDescription; }
                set
                {
                    _dataItemDescription = value;
                    OnPropertyChanged("DataItemDescription");
                }
            }

        }
        internal StoryboardColl storyboardColl {
            get;
            set;
        }


        // Test Suites
        DataItem _selectedTestSuite;

        public DataItem SelectedTestSuite
        {
            get { return _selectedTestSuite; }
            set
            {
                if (value != null)
                {
                    _selectedTestSuite = value;

                    TestCases = PopulateTestCases(_selectedTestSuite.DataItemName);
                    //SelectedTestCase = TestCases.Where(a => a.Id == TestCaseId).FirstOrDefault();
                    SelectedTestCase = TestCases.FirstOrDefault();
                    OnPropertyChanged("SelectedTestSuite");
                    OnPropertyChanged("SelectedTestCase");
                }
            }
        }


        private ObservableCollection<DataItem> _testSuites;
        public ObservableCollection<DataItem> TestSuites
        {
            get { return _testSuites; }
            set 
            { 
               // _testSuites = value; 
                if (value != null)
                    _testSuites = new ObservableCollection<DataItem>(value.OrderBy(x => x.DataItemName));
                else
                    _testSuites = value; 
            }
        }

        // Test Cases
        DataItem _selectedTestCase;

        public DataItem SelectedTestCase
        {
            get { return _selectedTestCase; }
            set
            {
                if (value != null)
                {
                    _selectedTestCase = value;
                    DataSetNames = PopulateDataSetNames(_selectedTestCase.DataItemName);
                    SelectedDataSetName = DataSetNames.FirstOrDefault();

                    OnPropertyChanged("SelectedTestCase");
                    OnPropertyChanged("SelectedDataSetName");
                }
            }
        }


        private ObservableCollection<DataItem> _testCases;
        public ObservableCollection<DataItem> TestCases
        {
            get { return _testCases; }
            set
            {
               // _testCases = value;
                if (value != null)
                    _testCases = new ObservableCollection<DataItem>(value.OrderBy(x => x.DataItemName));
                else
                    _testCases = value;

                OnPropertyChanged("TestCases");
            }
        }

        // Data Set Name
        DataItem _selectedDataSetName;

        public DataItem SelectedDataSetName
        {
            get { return _selectedDataSetName; }
            set
            {
                if (value != null)
                {
                    _selectedDataSetName = value;
                    OnPropertyChanged("SelectedDataSetName");
                }
            }
        }


        private ObservableCollection<DataItem> _dataSetNames;
        public ObservableCollection<DataItem> DataSetNames
        {
            get { return _dataSetNames; }
            set
            {
                //_dataSetNames = value;

                if (value != null)
                    _dataSetNames = new ObservableCollection<DataItem>(value.OrderBy(x => x.DataItemName));
                else
                    _dataSetNames = value;

                OnPropertyChanged("DataSetNames");
            }
        }


        DataItem _selectedDependOnStep;

        public DataItem SelectedDependOnStep
        {
            get { return _selectedDependOnStep; }
            set
            {
                if (value != null)
                {
                  
                    if (value == null)
                        RelyOn = null;
                    else if (value._id != -1)
                        RelyOn = value._id;
                   
                    _selectedDependOnStep = value;
                    OnPropertyChanged("SelectedDependOnStep");
                }
            }
        }


        private ObservableCollection<DataItem> _dependOnSteps;
        public ObservableCollection<DataItem> DependOnSteps
        {
            get {
                return _dependOnSteps;
            }
            set
            {
                
                _dependOnSteps = value;
                OnPropertyChanged("DependOnSteps");
            }
        }


#endregion

        public static ObservableCollection<DataItem> PopulateTestSuites(IList<B_PROJ_TS_TC_FULLVISION> lstProjectData)
        {
            ObservableCollection<DataItem> lstSuites = new ObservableCollection<DataItem>();
            var ts = (from item in lstProjectData
                      select new
                      {
                          TEST_SUITE_ID = item.TEST_SUITE_ID,
                          TEST_SUITE_NAME = item.TEST_SUITE_NAME
                      })
              .ToList()
              .OrderBy(x => x.TEST_SUITE_NAME)
              .Distinct();

            foreach (var item in ts)
            {
                DataItem di = new DataItem(item.TEST_SUITE_ID, item.TEST_SUITE_NAME);
                lstSuites.Add(di);
            }

            return lstSuites;
        }

        public ObservableCollection<DataItem> PopulateTestSuites()
        {
            return PopulateTestSuites(storyboardColl.projectData);
            //var _tSuites = new ObservableCollection<DataItem>();

            //var result = (from item in storyboardColl.projectData
            //              select new
            //              {
            //                  TEST_SUITE_ID = item.TEST_SUITE_ID,
            //                  TEST_SUITE_NAME = item.TEST_SUITE_NAME
            //              })
            //  .ToList()
            //  .OrderBy(x => x.TEST_SUITE_NAME)
            //  .Distinct();

            //foreach (var item in result)
            //{
            //    DataItem di = new DataItem(item.TEST_SUITE_ID, item.TEST_SUITE_NAME);
            //    _tSuites.Add(di);
            //}

            //return _tSuites;
        }


        public ObservableCollection<DataItem> PopulateTestCases(long tsId)
        {
            var _tCases = new ObservableCollection<DataItem>();

            //foreach (var dd in storyboardColl.projectData)
            //    Console.WriteLine(dd.TEST_CASE_NAME);

            var result = (from item in storyboardColl.projectData
                          where item.TEST_SUITE_ID== tsId
                          select new
                          {
                              TEST_CASE_ID = item.TEST_CASE_ID,
                              TEST_CASE_NAME = item.TEST_CASE_NAME
                          })
              .ToList()
              .Distinct();

            foreach (var item in result)
            {
                DataItem di = new DataItem(item.TEST_CASE_ID, item.TEST_CASE_NAME);
                _tCases.Add(di);
            }

            return _tCases;
        }

        public ObservableCollection<DataItem> PopulateTestCases(string testSuite)
        {
            var _tCases = new ObservableCollection<DataItem>();

            //foreach (var dd in storyboardColl.projectData)
            //    Console.WriteLine(dd.TEST_CASE_NAME);

            var result = (from item in storyboardColl.projectData
                          where item.TEST_SUITE_NAME == testSuite
                          select new
                          {
                              TEST_CASE_ID = item.TEST_CASE_ID,
                              TEST_CASE_NAME = item.TEST_CASE_NAME
                          })
              .ToList()
              .Distinct();

            foreach (var item in result)
            {
                DataItem di = new DataItem(item.TEST_CASE_ID, item.TEST_CASE_NAME);
                _tCases.Add(di);
            }

            return _tCases;
        }

        public ObservableCollection<DataItem> PopulateDataSetNames(long tcId)
        {
            var _tDataSetNames = new ObservableCollection<DataItem>();

            var result = (from item in storyboardColl.projectData
                          where item.TEST_CASE_ID==tcId
                          select new
                          {
                              DATA_SUMMARY_ID = item.DATA_SUMMARY_ID,
                              DATA_ALIAS = item.DATA_ALIAS,
                              DATA_DESCRIPTION = item.DATASET_DESCRIPTION
                          })
              .ToList()
              .Distinct();

            foreach (var item in result)
            {
                DataItem di = new DataItem(item.DATA_SUMMARY_ID, item.DATA_ALIAS, item.DATA_DESCRIPTION);
                _tDataSetNames.Add(di);
            }

            return _tDataSetNames;
        }


        public ObservableCollection<DataItem> PopulateDataSetNames(string testCase)
        {
            var _tDataSetNames = new ObservableCollection<DataItem>();

            var result = (from item in storyboardColl.projectData
                          where item.TEST_CASE_NAME == testCase
                          select new
                          {
                              DATA_SUMMARY_ID = item.DATA_SUMMARY_ID,
                              DATA_ALIAS = item.DATA_ALIAS,
                              DATA_DESCRIPTION = item.DATASET_DESCRIPTION
                          })
              .ToList()
              .Distinct();

            foreach (var item in result)
            {
                DataItem di = new DataItem(item.DATA_SUMMARY_ID, item.DATA_ALIAS, item.DATA_DESCRIPTION);
                _tDataSetNames.Add(di);
            }

            return _tDataSetNames;
        }

        internal string[] ToStringArray()
        {
            if ((SelectedTestSuite == null)||(SelectedTestCase==null)) return null;
            MarsTestReportResultConvert objConvrtRslt = new MarsTestReportResultConvert();
            object strRslt = objConvrtRslt.Convert(HistResult, null, null, null);
            return new string[] { RunOrder+"",
                _selectedAction,
                AliasName,
                SelectedTestSuite.DataItemName,
                SelectedTestCase.DataItemName,
                SelectedDataSetName==null?"":SelectedDataSetName.DataItemName,
                strRslt==null?"UNKNOW":(string)strRslt,
                HistTestResultInText,
                TestCaseBeginTime==null?"":((DateTime)TestCaseBeginTime).ToString("MM/dd/YYYY HH:mm:ss"),
                TestCaseEndTime==null?"":((DateTime)TestCaseEndTime).ToString("MM/dd/YYYY HH:mm:ss"),
                SelectedDependOnStep==null?"":SelectedDependOnStep.DataItemName,
                TestCaseDescription
            };
        }


        ///storyboard的测试结果区间
        /// 
        #region storyboard Test Result


        
        private MarsReportDetail _reportDetail = new MarsReportDetail();
        public MarsReportDetail ReportDetail
        {
            get { return _reportDetail; }
            set {
                _reportDetail = value;
                OnPropertyChanged("ReportDetail");
            }
        }

        

        /// <summary>
        /// 依据detail ID 获得test result detail的信息
        /// </summary>
        /// <returns></returns>
        internal bool LoadDetailInfo()
        {
            Logger.logBegin("LoadDetailInfo");
            B_PROJ_TEST_RESULT objRslt = new B_PROJ_TEST_RESULT();
            bool isOk = false;
            string strError = "";
            ReportDetail.IsBeforeDataLoading = Visibility.Visible;

            List<StoryboardHistSummaryInfo> lstStoryHistInfo = objRslt.getResultHistInfoByDetailId(
                MarsMainWindow.CurrentDatabaseIdx,
                this._storyboardDetailId,ref isOk, ref strError);
            if (!isOk)
            {
                ReportDetail.IsDataVisible = Visibility.Collapsed;
                //ReportDetail.IsBeforeDataLoading = Visibility.Hidden;
                ReportDetail.ErrorMessage = strError;
                return false;
            }
            else
            {
                ReportDetail.IsDataVisible = Visibility.Visible;
                ReportDetail.DetailResultData = StoryboardRowDetailDataForMadel.ConverFrom(lstStoryHistInfo);
            }
            return true;
            //List<StoryboardDetailDataForMadel> lstHisDataForModel = StoryboardDetailDataForMadel.ConverFrom(lstStoryHistInfo);
        }

        internal bool ToStandardDataRow(DataRow oneDataRow, ref string strError)
        {
            if (oneDataRow==null)
            {
                strError = "Target Data Row is null";
                return false;
            }

            foreach(DataColumn col in oneDataRow.Table.Columns)
            {
                switch(col.Caption)
                {
                    case "Run Order":
                        oneDataRow[col] = RunOrder;
                        break;
                    case "Action":
                        if (string.IsNullOrEmpty(DisplayName))
                            oneDataRow[col] = string.Empty;
                        else oneDataRow[col] = DisplayName;
                        break;
                    case "Step Name":
                        if (string.IsNullOrEmpty(AliasName))
                            oneDataRow[col] = string.Empty;
                        else
                            oneDataRow[col] = AliasName;
                        break;
                    case "Test Suite Name":
                        oneDataRow[col] = SelectedTestSuite==null?string.Empty:SelectedTestSuite.DataItemName;
                        break;
                    case "Test Case Name":
                        oneDataRow[col] = SelectedTestCase == null ? string.Empty : SelectedTestCase.DataItemName;
                        break;
                    case "Data Set Name":
                        oneDataRow[col] = SelectedDataSetName == null ? string.Empty : SelectedDataSetName.DataItemName;
                        break;
                    case "Result":
                        oneDataRow[col] = HistResult==null? string.Empty: HistResult+"";
                        break;
                    case "Error Cause":
                        oneDataRow[col] = string.IsNullOrEmpty(HistTestResultInText)?string.Empty: HistTestResultInText;
                        break;
                    case "Script Start":
                        oneDataRow[col] = TestCaseBeginTime;
                        break;
                    case "Script End":
                        oneDataRow[col] = TestCaseEndTime;
                        break;
                    case "Dependency":
                        oneDataRow[col] = SelectedDependOnStep==null?string.Empty: SelectedDependOnStep.DataItemName;
                        break;
                    case "Description":
                        oneDataRow[col] = TestCaseDescription;
                        break;
                }
               
            }
            return true;
        }
        #endregion //storyboard Test Result
    }

    public class MarsReportDetail : Notify
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsReportDetail));
        public MarsReportDetail()
        {
            saveResultCommand = new DelegateCommand(() => { saveResultImpl(); });
        }

        private ObservableCollection<StoryboardRowDetailDataForMadel> detailResultData = null;
        public ObservableCollection<StoryboardRowDetailDataForMadel> DetailResultData
        {
            get { return detailResultData; }
            set { detailResultData = value; OnPropertyChanged("DetailResultData"); }
        }

        public ObservableCollection<StoryboardRowDetailDataForMadel> DetailResultDataNonBase
        {
            get { return new ObservableCollection<StoryboardRowDetailDataForMadel>(detailResultData.Where(p => p.Test_ModeMark == 0)); }
        }
        public ObservableCollection<StoryboardRowDetailDataForMadel> DetailResultDataBaseLine
        {
            get { return new ObservableCollection<StoryboardRowDetailDataForMadel>(detailResultData.Where(p => p.Test_ModeMark == 1)); }
        }

        private Visibility isDataLoaded;
        public Visibility IsDataVisible
        {
            get { return isDataLoaded; }
            set
            {
                isDataLoaded = value; OnPropertyChanged("IsDataLoaded");
                if (isDataLoaded == Visibility.Visible)
                {
                    IsErrorVisible = Visibility.Collapsed;
                }
                IsBeforeDataLoading = Visibility.Hidden;
            }
        }

        private Visibility isBeforeDataLoading = Visibility.Visible;
        public Visibility IsBeforeDataLoading
        {
            get { return isBeforeDataLoading; }
            set
            {
                isBeforeDataLoading = value;
                OnPropertyChanged("IsBeforeDataLoading");
            }
        }

        private Visibility isErrorVisible = Visibility.Hidden;
        public Visibility IsErrorVisible
        {
            get { return isErrorVisible; }
            set { isErrorVisible = value; OnPropertyChanged("IsErrorVisible"); }
        }

        private string errorMessage = "";
        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; OnPropertyChanged("ErrorMessage"); }
        }

        #region Command
        private ICommand saveResultCommand;
        private void saveResultImpl()
        {
            ///算法：
            /// 1，将数据转换为数据库端可以接受的数据
            /// 2，update。因为这是对历史数据的回顾，因此，不可能存在对新历史数据的增加
            /// 
            Logger.logBegin("SaveResultCommand");
            List<StoryboardHistSummaryInfo> lstChangedData = new List<StoryboardHistSummaryInfo>();
            if (DetailResultData != null)
            {
                DetailResultData.ToList().ForEach(itm =>
                {
                    lstChangedData.Add(itm);
                });
                B_PROJ_TEST_RESULT objProjRslt = new B_PROJ_TEST_RESULT();
                bool isOk = false;
                string strError = "";
                isOk = objProjRslt.SaveDetailInof(MarsMainWindow.CurrentDatabaseIdx, lstChangedData, ref strError);
                if (!isOk)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Save Report Result Failed. Error:\r\n", strError), "Error");
                    return;
                }
                else
                {
                    OnPropertyChanged("DetailResultDataNonBase");
                    OnPropertyChanged("DetailResultDataBaseLine");
                    OnPropertyChanged("ReportDetail") ;
                    ViewModelBase.HintByMessageBox("Saving Test Result successfully.", "Hint");
                    return;
                }
            }
            else
            {
                ViewModelBase.HintByMessageBox("No Data needs to be updated!", "Warnning");
                return;
            }
        }
        public ICommand SaveResultCommand
        {
            get
            {
                return saveResultCommand;
            }
        }
        #endregion
    };

    public class StoryboardCache
    {
        public static Dictionary<long, StoryboardColl> cache = new Dictionary<long, StoryboardColl>();
        public static StoryboardColl currentSBColl;

        
        public static StoryboardColl getSBCall(string strDBIdx, string projectName, string storyboardName, long projectId, long storyboardId, MarsStoryboardTreeView assignedTreeNode = null)
        {
            StoryboardColl sbCall = null;
            
            //string key = projectName + "_" + storyboardName;
            if (cache.ContainsKey(storyboardId))
            {
                sbCall = cache[storyboardId];
            }
            else
            {
                sbCall = new StoryboardColl(strDBIdx,projectName, storyboardName, projectId,storyboardId);
                sbCall._AssignedTreeNode = assignedTreeNode;
                cache.Add(storyboardId, sbCall);
            }
            currentSBColl = sbCall;
            return sbCall;
        }

        internal static void removeStoryboard(long storyboardId)
        {
            if (cache.ContainsKey(storyboardId))
                cache.Remove(storyboardId);
        }
    }

    public class StoryboardClipBoard
    {
        public static IList<StoryboardEditViewModel> storyboardClipBoard;
        public static StringBuilder StoryInfoForClipBoard
        {
            get { return CreateClipBoardFromList(); }
        }
        private static StringBuilder CreateClipBoardFromList()
        {
            StringBuilder strRslt = new StringBuilder();
            if (storyboardClipBoard == null) return strRslt;

            return strRslt;
        }
    }
}
