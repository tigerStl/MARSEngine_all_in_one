using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Business;
using Mars.DataLayer;
using Mars.Delegate;
using Route2NSEx.src.Marquis.systemUtil;
using Mars.Dto;

namespace Mars.ViewModel
{
    public class MarsProjectTreeView : TreeViewModelBase
    {
        string _projectName;
        long _projectId;
        
        ObservableCollection<MarsTestSuiteTreeView> _testSuite = null;

        ObservableCollection<MarsFolderTreeView> _folders = null;

        #region Added for performance
        List<V_PROJ_TS_TC_FULLVISIONDTO> Project_TS_TC_Data_List = null;
            #endregion
        public MarsProjectTreeView(List<V_PROJ_TS_TC_FULLVISIONDTO> lstProjtstcDat=null)
        {
            Project_TS_TC_Data_List = lstProjtstcDat;
        }

        //public MarsProjectTreeView(string projectName)
        //    : this(projectName, null)
        //{
        //}

        //public MarsProjectTreeView(string projectName, ObservableCollection<MarsTestSuiteTreeView> testSuites)
        //{
        //    _projectName = projectName;
        //    _testSuite = testSuites;            
        //}       

        public string ProjectName
        {
            get
            {
                return _projectName;
            }
            set
            {
                _projectName = value;
                RaisePropertyChanged("ProjectName"); 
            }
        }

        public long ProjectId
        {
            get { return _projectId; }
            set { _projectId = value; }
        }

#if v_16AndUp
        public List<long?> AssignedApplicationIdList
        {
            get;set;
        }
#endif

        public string ToolTip
        {
            get { return _projectName + ":" + _projectId; }
            
        }

        public ObservableCollection<MarsFolderTreeView> TEST_FOLDER
        {
            get
            {
                if (_folders == null) _folders = new ObservableCollection<MarsFolderTreeView>();
                return _folders;
            }
            set 
            {
                _folders = value;
                RaisePropertyChanged("TEST_FOLDER");
            }
        }   

        public ObservableCollection<MarsTestSuiteTreeView> TEST_SUITE
        {
            get
            { 
                if (_testSuite == null) _testSuite = new ObservableCollection<MarsTestSuiteTreeView>();
                return _testSuite;
            }
            set 
            { 
                _testSuite = value;
                RaisePropertyChanged("TEST_SUITE");
            }
        }   
    }

    /*
     * public class MarsProjectTreeView : ViewModelBase
    {
        string _projectName;
        ObservableCollection<MarsTestSuiteTreeView> _testSuite = null;
        
        public MarsProjectTreeView()
        {}

        public MarsProjectTreeView(string projectName)
            : this(projectName, null)
        {
        }

        public MarsProjectTreeView(string projectName, ObservableCollection<MarsTestSuiteTreeView> testSuites)
        {
            _projectName = projectName;
            _testSuite = testSuites;            
        }       

        public string ProjectName
        {
            get
            {
                return _projectName;
            }
            set
            {
                _projectName = value;
                RaisePropertyChanged("ProjectName"); 
            }
        }

        public ObservableCollection<MarsTestSuiteTreeView> TEST_SUITE
        {
            get
            {
                if (_testSuite == null) _testSuite = new ObservableCollection<MarsTestSuiteTreeView>();
                return _testSuite;
            }
            set 
            { 
                _testSuite = value;
                RaisePropertyChanged("TEST_SUITE");
            }
        }   
    }
     */

    public class MarsTestSuiteTreeView : TreeViewModelBase
    {
        string _testSuiteName;
        ObservableCollection<MarsTestCaseTreeView> _testCase = null;

        public MarsTestSuiteTreeView()
        {}

        public MarsTestSuiteTreeView(string testSuiteName)
            : this(testSuiteName, null)
        {
        }

        public MarsTestSuiteTreeView(string testSuiteName, ObservableCollection<MarsTestCaseTreeView> testCase)
        {
            _testSuiteName = testSuiteName;
            _testCase = testCase;            
        }       

        public string TestSuiteName
        {
            get
            {
                return _testSuiteName;
            }
            set
            {
                _testSuiteName = value;
                RaisePropertyChanged("TestSuiteName");
            }
        }

        public ObservableCollection<MarsTestCaseTreeView> TEST_CASE
        {
            get
            {
                if (_testCase == null) _testCase = new ObservableCollection<MarsTestCaseTreeView>();
                return _testCase;
            }
            set 
            { 
                _testCase = value;
                RaisePropertyChanged("TEST_CASE");
            }
        }

        public long TestSuiteId { get; set; }

        public long ProjectId { get; set; }
    }

    public class MarsTestCaseTreeView : TreeViewModelBase
    {
        ObservableCollection<MarsDataSheetTreeView> _dataSheet = null;
        
        public MarsTestCaseTreeView()
        {}

        string _testCaseName;
        public string TestCaseName
        {
            get
            {
                return _testCaseName;
            }
            set
            {
                _testCaseName = value;
                RaisePropertyChanged("TestCaseName");
            }
        }

        long _testCaseId;

        public long TestCaseId
        {
            get { return _testCaseId; }
            set { _testCaseId = value; }
        }

        long _testSuiteId;

        public long TestSuiteId
        {
            get { return _testSuiteId; }
            set { _testSuiteId = value; }
        }

        long _projectId;

        public long ProjectId
        {
            get { return _projectId; }
            set { _projectId = value; }
        }

        public ObservableCollection<MarsDataSheetTreeView> DATA_SHEET
        {
            get
            {
                if (_dataSheet == null) _dataSheet = new ObservableCollection<MarsDataSheetTreeView>();
                return _dataSheet;
            }
            set
            {
                _dataSheet = value;
                RaisePropertyChanged("DATA_SHEET");
            }
        }

    }

///
    public class MarsDataSheetTreeView : TreeViewModelBase
    {

        public MarsDataSheetTreeView()
        { }

        string _dataSheetName;

        public string DataSheetName
        {
            get { return _dataSheetName; }
            set 
            { 
                _dataSheetName = value;
                RaisePropertyChanged("DataSheetName");
            }
        }

        string _dataSheetDescription;

        public string DataSheetDescription
        {
            get { return _dataSheetDescription; }
            set
            {
                _dataSheetDescription = value;
                RaisePropertyChanged("DataSheetDescription");
            }
        }

        long _dataSheetId;

        public long DataSheetId
        {
            get { return _dataSheetId; }
            set { _dataSheetId = value; }
        }

        string _testCaseName;
        public string TestCaseName
        {
            get
            {
                return _testCaseName;
            }
            set
            {
                _testCaseName = value;
                RaisePropertyChanged("TestCaseName");
            }
        }

        long _testCaseId;

        public long TestCaseId
        {
            get { return _testCaseId; }
            set { _testCaseId = value; }
        }

        long _testSuiteId;

        public long TestSuiteId
        {
            get { return _testSuiteId; }
            set { _testSuiteId = value; }
        }

        long _projectId;

        public long ProjectId
        {
            get { return _projectId; }
            set { _projectId = value; }
        }

        public string ToolTip
        {
            get {
                return _dataSheetDescription;
                    /*
                "PID:" + _projectId + "\n" +
                "TSID:" + _testSuiteId + "\n" +
                "TCID:" + _testCaseId + "\n" + 
                "DSID:" + _dataSheetId;
                */
            }

        }

    }


//
    public class MarsFolderTreeView : TreeViewModelBase
    {
        string _folderName;
        ObservableCollection<ViewModelBase> _treeItem = null;

        public MarsFolderTreeView()
        { }

        public MarsFolderTreeView(string folderName, long projectId, string projectName)
            : this(folderName, null, projectId, projectName)
        {
        }

        public MarsFolderTreeView(string folderName, ObservableCollection<ViewModelBase> treeItems, long projectId, string projectName)
        {
            _folderName = folderName;
            _treeItem = treeItems;
            _projectId = projectId;
            _projectName = projectName;
        }

        long _projectId;
        private string _projectName;

        public string ProjectName
        {
            get { return _projectName; }
            set { _projectName = value; }
        }

        public long ProjectId
        {
            get { return _projectId; }
            set { _projectId = value; }
        }

        public string FolderName
        {
            get
            {
                return _folderName;
            }
            set
            {
                _folderName = value;
                RaisePropertyChanged("FolderName");
            }
        }

        public ObservableCollection<ViewModelBase> TREE_ITEM
        {
            get
            {
                if (_treeItem == null) _treeItem = new ObservableCollection<ViewModelBase>();
                return _treeItem;
            }
            set
            {
                _treeItem = value;
                RaisePropertyChanged("TREE_ITEM");
            }
        }
    }
    //

    public class MarsStoryboardTreeView : TreeViewModelBase
    {
        string _storyboardName;
        string _projectName;
        long? _storyboardId;
        long? _projectId;


        public OnRibbonTestApplicationsReady onRibbonTestAppliationsReadyImpl = null;
        public MarsStoryboardTreeView()
        { }



        public MarsStoryboardTreeView(string storyboardName, long? storyboardId, string projectName, long? projectId)
        {
            StoryboardName = storyboardName;
            _projectName = projectName;
            _storyboardId = storyboardId;
            _projectId = projectId;

        }

        public string ToolTip
        {
            get 
            {
               
               
                return StoryboardName + ":" + _storyboardId; 
            }
        }

        public string StoryboardName
        {
            get
            {
                return _storyboardName;
            }
            set
            {
                _storyboardName = value;
                RaisePropertyChanged("StoryboardName");
            }
        }

        public string ProjectName
        {
            get { return _projectName; }
            set 
            { 
                _projectName = value;
                RaisePropertyChanged("ProjectName");
            }
        }

        public long? StoryboardId
        {
            get { return _storyboardId; }
            set { _storyboardId = value; }
        }

        public long? ProjectId
        {
            get { return _projectId; }
            set { _projectId = value; }
        }


       
        public static MarsStoryboardTreeView SelectedStoryboardNode { get; set; }
    }

    //
    public class MarsTreeView : ViewModelBase
    {
        static ObservableCollection<MarsProjectTreeView> _marsProjectTreeView = new ObservableCollection<MarsProjectTreeView>();
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsTreeView));
        public ObservableCollection<MarsProjectTreeView> MarsProjectTreeView
        {
            get
            {
                return _marsProjectTreeView;
            }
            set
            {
                _marsProjectTreeView = value;
                RaisePropertyChanged("MarsProjectTreeView");
            }
        }


        public static void BuildDataSheetTree(MarsTestCaseTreeView newTestCase, List<V_PROJ_TS_TC_FULLVISIONDTO> lstProjSource)
        {
            newTestCase.DATA_SHEET.Clear();
            //List<B_LINKED_DATA_SHEET> LinkedDataSheetList = BoHelper.GetLinkedDataSheet(newTestCase.ProjectId, newTestCase.TestSuiteId, newTestCase.TestCaseId);
            List<B_LINKED_DATA_SHEET> LinkedDataSheetList = B_V_PROJ_TS_TC_FULLVISION.FilterByProjTSIdTCIdAndConvert2SimpleDataSheet(newTestCase.ProjectId, 
                newTestCase.TestSuiteId, newTestCase.TestCaseId,lstProjSource);
            foreach (B_LINKED_DATA_SHEET linkedDataSheet in LinkedDataSheetList)
            {
                if (linkedDataSheet.IsSelected)
                {
                    if (linkedDataSheet.Id == -1) continue;
                    MarsDataSheetTreeView newDataSheet = new MarsDataSheetTreeView();
                    
                    newDataSheet.DataSheetId = linkedDataSheet.Id;
                    newDataSheet.DataSheetName = linkedDataSheet.DataItemName;
                    newDataSheet.DataSheetDescription = linkedDataSheet.DataItemDescription;
                    newDataSheet.TestCaseName = newTestCase.TestCaseName;
                    newDataSheet.TestCaseId = newTestCase.TestCaseId;
                    newDataSheet.TestSuiteId = newTestCase.TestSuiteId;
                    newDataSheet.ProjectId = newTestCase.ProjectId;
                    newTestCase.DATA_SHEET.Add(newDataSheet);
#if v_16AndUp
                    newDataSheet.Parent = newTestCase;
#endif
                }
            }
        }

        
        public static void BuildTestCaseTree(MarsTestSuiteTreeView newTestSuite, List<V_PROJ_TS_TC_FULLVISIONDTO> lstProjSource)
        {
            B_TEST_CASE objTestCase = new B_TEST_CASE();
            newTestSuite.TEST_CASE.Clear();

            List<B_TEST_CASE> TestCaseList = new List<B_TEST_CASE>();

            //TestCaseList = objTestCase.GetMappedTestCase(newTestSuite.TestSuiteId);
            TestCaseList = B_V_PROJ_TS_TC_FULLVISION.FilterByProjTSIdAndConvert2SimpleTestCase(newTestSuite.ProjectId,newTestSuite.TestSuiteId, lstProjSource);
            if (TestCaseList != null && TestCaseList.Count > 0)
            {
                foreach (B_TEST_CASE testCase in TestCaseList)
                {
                    MarsTestCaseTreeView newTestCase = new MarsTestCaseTreeView();
                    newTestCase.TestCaseName = testCase.TEST_CASE_NAME;
                    newTestCase.TestCaseId = testCase.TEST_CASE_ID;
                    newTestCase.TestSuiteId = newTestSuite.TestSuiteId;
                    newTestCase.ProjectId = newTestSuite.ProjectId;

                    newTestSuite.TEST_CASE.Add(newTestCase);
#if v_16AndUp
                    newTestCase.Parent = newTestSuite;
#endif
                    BuildDataSheetTree(newTestCase, lstProjSource);
                }
            }
        }

        public static void BuildStoryboardFolderTree(MarsFolderTreeView storyBoardFolder)
        {
            if (storyBoardFolder == null) return;
            if (storyBoardFolder.TREE_ITEM == null) return;
            storyBoardFolder.TREE_ITEM.Clear();
            List<B_STORYBOARD_SUMMARY> storyboardSummaryList = BoHelper.GetAllStoryboardRows(MarsMainWindow.CurrentDatabaseIdx, storyBoardFolder.ProjectId);

            foreach (var storyboard in storyboardSummaryList)
            {
                MarsStoryboardTreeView storyboardTreeView = new MarsStoryboardTreeView(storyboard.STORYBOARD_NAME,
                                                                                       storyboard.STORYBOARD_ID,
                                                                                       storyBoardFolder.ProjectName,
                                                                                       storyBoardFolder.ProjectId
                                                                                       );

                storyBoardFolder.TREE_ITEM.Add(storyboardTreeView);
                storyboardTreeView.Parent = storyBoardFolder;
            }
        }

        /// <summary>
        /// Append a project into tree
        /// </summary>
        /// <param name="lProjId"></param>
        /// <returns></returns>
        public static bool InsertProjectsToTree(long lProjId)
        {
            Logger.logBegin("InsertProjectsToTree",string.Format("Project Id to insert:[{0}]", lProjId));
            string strError = "";
            bool isOk = false;
            try
            {
                List<V_PROJ_TS_TC_FULLVISIONDTO> lstProjtstcDat = B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(
                    MarsMainWindow.CurrentDatabaseIdx,
                    ref strError, ref isOk);
                List<V_PROJ_TS_TC_FULLVISIONDTO> objPrj = B_V_PROJ_TS_TC_FULLVISION.GetProjectById(MarsMainWindow.CurrentDatabaseIdx, lProjId);
                if (objPrj == null) return false;
                B_TEST_PROJECT objProject = (new B_TEST_PROJECT()).GetProjectBOById(MarsMainWindow.CurrentDatabaseIdx, lProjId);
                //T_TEST_PROJECTDTO proj = objProject.GetProject(lProjId);
                if (objProject == null) return false;

                MarsProjectTreeView treeItem = new MarsProjectTreeView();
                treeItem.ProjectName = objProject.PROJECT_NAME;
                treeItem.ProjectId = objProject.PROJECT_ID;
                treeItem.AssignedApplicationIdList = objProject.AssignedAppIds;

                MarsFolderTreeView testSuiteFolder = new MarsFolderTreeView("Test Suites", objProject.PROJECT_ID, objProject.PROJECT_NAME);
                MarsFolderTreeView dashboardFolder = new MarsFolderTreeView("Dashboard", objProject.PROJECT_ID, objProject.PROJECT_NAME);
                MarsFolderTreeView storyBoardFolder = new MarsFolderTreeView("Storyboards", objProject.PROJECT_ID, objProject.PROJECT_NAME);
                treeItem.TEST_FOLDER.Add(dashboardFolder);
                treeItem.TEST_FOLDER.Add(storyBoardFolder);
                treeItem.TEST_FOLDER.Add(testSuiteFolder);
                testSuiteFolder.Parent = treeItem;
                dashboardFolder.Parent = treeItem;
                storyBoardFolder.Parent = treeItem;

                BuildStoryboardFolderTree(storyBoardFolder);
                GetTestSuiteByProjectId(objProject.PROJECT_ID, treeItem.TEST_FOLDER[2].TREE_ITEM, treeItem.TEST_FOLDER[2], lstProjtstcDat);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertProjectsToTree", string.Format("Exception:[{0}]",e.Message),e);
                return false;
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static ObservableCollection<MarsProjectTreeView> GetMarsTree(string strDBIdx)
        {
            //ObservableCollection<MarsProjectTreeView> listToReturn = new ObservableCollection<MarsProjectTreeView>();
            //Get all Projects from DB, then fetch Testsuites for it and testcases to build 
            //tree in foreach loop of projects
            Logger.logBegin("GetMarsTree");            
            _marsProjectTreeView.Clear();

            List<B_TEST_PROJECT> Project = new List<B_TEST_PROJECT>();
            B_TEST_PROJECT objProject = new B_TEST_PROJECT();
            B_TEST_SUITE objTestSuite = new B_TEST_SUITE();
            B_TEST_CASE objTestCase = new B_TEST_CASE();
            Project = objProject.GetProject(strDBIdx);
            //Dictionary<long, Dictionary<B_TEST_PROJECTDTO,>>
            #region added for performance
            /// 效率问题，原模式进行了无数次查询，现将数据先拿出来 到一个链表中
            /// 
            string strError = "";
            bool isOk = false;
            List<V_PROJ_TS_TC_FULLVISIONDTO> lstProjtstcDat = B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(strDBIdx: strDBIdx, 
                ref strError,ref isOk
                );
            if (!isOk)
            {
                Logger.Error("GetMarsTree",string.Format("Error from GetAllTestProjInfo --{0}",strError));
                HintByMessageBox(strError, "ERROR");
                return null;
            }
            #endregion //added for performance

            foreach (B_TEST_PROJECT project in Project)
            {
                MarsProjectTreeView treeItem = new MarsProjectTreeView();
                treeItem.ProjectName = project.PROJECT_NAME;
                treeItem.ProjectId = project.PROJECT_ID;
#if v_16AndUp
                treeItem.AssignedApplicationIdList = project.AssignedAppIds;
                
#endif
                // Add folders

                MarsFolderTreeView testSuiteFolder =  new MarsFolderTreeView("Test Suites", project.PROJECT_ID, project.PROJECT_NAME);
                MarsFolderTreeView dashboardFolder = new MarsFolderTreeView("Dashboard", project.PROJECT_ID, project.PROJECT_NAME);
                MarsFolderTreeView storyBoardFolder = new MarsFolderTreeView("Storyboards", project.PROJECT_ID, project.PROJECT_NAME);
              
                treeItem.TEST_FOLDER.Add(dashboardFolder);
                treeItem.TEST_FOLDER.Add(storyBoardFolder);
                treeItem.TEST_FOLDER.Add(testSuiteFolder);
#if v_16AndUp
                testSuiteFolder.Parent = treeItem;
                dashboardFolder.Parent = treeItem;
                storyBoardFolder.Parent = treeItem;
#endif
                // Add Storyboards
                BuildStoryboardFolderTree(storyBoardFolder);

                //Get All Suites for this Project
                GetTestSuiteByProjectId(project.PROJECT_ID, treeItem.TEST_FOLDER[2].TREE_ITEM, treeItem.TEST_FOLDER[2], lstProjtstcDat);
                #region trash code
                //                List<B_TEST_SUITE> TestSuiteList = new List<B_TEST_SUITE>();
                //                TestSuiteList = objTestSuite.GetMappedTestSuite(project.PROJECT_ID);
                //                if (TestSuiteList != null && TestSuiteList.Count > 0)
                //                {
                //                    foreach (B_TEST_SUITE testSuite in TestSuiteList)
                //                    {
                //                        MarsTestSuiteTreeView newTestSuite = new MarsTestSuiteTreeView();
                //                        newTestSuite.TestSuiteName = testSuite.TEST_SUITE_NAME;
                //                        newTestSuite.TestSuiteId = testSuite.TEST_SUITE_ID;
                //                        newTestSuite.ProjectId = project.PROJECT_ID;
                //                        //for loop for test case
                //                        BuildTestCaseTree(newTestSuite);

                //                        testSuiteFolder.TREE_ITEM.Add(newTestSuite);
                //#if v_16AndUp
                //                        newTestSuite.Parent = testSuiteFolder;
                //#endif

                //                    }
                //                }
                #endregion //trash code
                _marsProjectTreeView.Add(treeItem);
            }
            return _marsProjectTreeView;
        }

        internal static void RefreshNodeName(ViewModel.MarsProjectTreeView projView)
        {
            //throw new NotImplementedException();
        }

        internal static void GetTestSuiteByProjectId(long lProj, ObservableCollection<ViewModelBase> targetTsList, MarsFolderTreeView parentView, List<V_PROJ_TS_TC_FULLVISIONDTO> lstProjSource)
        {
            if (targetTsList == null) return ;
            B_TEST_SUITE objTestSuite = new B_TEST_SUITE();
            List<B_TEST_SUITE> TestSuiteList = new List<B_TEST_SUITE>();
            targetTsList.Clear();

            TestSuiteList = B_V_PROJ_TS_TC_FULLVISION.FilterByProjIdAndConver2SimpleTestSuite(lProj, lstProjSource);
            //TestSuiteList = objTestSuite.GetMappedTestSuite(lProj);
            
            if (TestSuiteList != null && TestSuiteList.Count > 0)
            {
                foreach (B_TEST_SUITE testSuite in TestSuiteList)
                {
                    MarsTestSuiteTreeView newTestSuite = new MarsTestSuiteTreeView();
                    newTestSuite.TestSuiteName = testSuite.TEST_SUITE_NAME;
                    newTestSuite.TestSuiteId = testSuite.TEST_SUITE_ID;
                    newTestSuite.ProjectId = lProj;
                    //for loop for test case
                    BuildTestCaseTree(newTestSuite, lstProjSource);

                    targetTsList.Add(newTestSuite);
#if v_16AndUp
                    newTestSuite.Parent = parentView;
#endif

                }
            }
        }

    }
}
