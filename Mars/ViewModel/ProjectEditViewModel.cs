using Mars.Business;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Dto;
using Mars.Model;
using System.Windows.Input;
using System.Security.Principal;
using System.Windows;
using Mars.DataLayer;
using Mars.DataLayer.Generic;
using Route2NSEx.src.Marquis.systemUtil;
using System.Transactions;

namespace Mars.ViewModel
{
    public class ProjectEditViewModel : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ProjectEditViewModel));

        string _projectName;
        long project_id=-1;

        public long ProjectId
        {
            get { return project_id; }
            set { project_id = value;RaisePropertyChanged("ProjectId"); }
        }
        public ObservableCollection<B_TEST_SUITE> TestSuite { get; set; }
        List<B_TEST_SUITE> mappedTestSuite = new List<B_TEST_SUITE>();
//        MarsEntities marsEntities;
        private ICommand _saveCommand;
        private ICommand _clearCommand;
        private ICommand _enableEditCommand;
        ObservableCollection<B_REGISTERED_APPS> _registerdApplication = new ObservableCollection<B_REGISTERED_APPS>();

        private ICommand _rePopulateTestSuite;
        B_REGISTERED_APPS _selectedApplication;
        private bool _isSelected;
        string projectEditControlStatus;
        #region old code
        //public ProjectEditViewModel(string strProjectName)
        #endregion //oldCode

        #region syncronization section
        MarsProjectTreeView _marsProjectTreeView = null;
        #endregion //syncronization section

#if tiger_dock
        private string title;
        public string Title
        {
            get { return title; }
            set { Title = value;RaisePropertyChanged("Title"); }
        }
#endif

        public ProjectEditViewModel(long lProjectId,string strProjectName, MarsProjectTreeView marsProjectTreeView = null)
        {
            ProjectName = strProjectName;
            project_id = lProjectId;
            //marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            _saveCommand = new DelegateCommand(() => { SaveProject(); });
            _clearCommand = new DelegateCommand(() => { ClearProject(); });
            _enableEditCommand = new DelegateCommand(()=> { EnableEditCommand(); });
            _rePopulateTestSuite = new DelegateCommand(() => { PopulateTestSuite(); });
            TestSuite = new ObservableCollection<B_TEST_SUITE>();
            GetApplication();
            GetTestSuite(project_id);

            #region syncronization section
            _marsProjectTreeView = marsProjectTreeView;
            #endregion //syncronization section

#if tiger_dock
            title = string.Format("Project Edit: [{0}]", strProjectName);
#endif
        }

        public ProjectEditViewModel()
        {
            //marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            _saveCommand = new DelegateCommand(() => { SaveProject(); });
            _clearCommand = new DelegateCommand(() => { ClearProject(); });
            _rePopulateTestSuite = new DelegateCommand(() => { PopulateTestSuite(); });
            TestSuite = new ObservableCollection<B_TEST_SUITE>();
            GetApplication();
            //GetTestSuite();

        }

        private Visibility _isShowEditMode = Visibility.Collapsed;
        public Visibility isShowEditMode
        {
            get { return _isShowEditMode; }
            set {
                _isShowEditMode = value;
                RaisePropertyChanged("isShowEditMode");
            }
        }

        public string ProjectEditControlStatus
        {
            get
            {
                return projectEditControlStatus;
            }
            set
            {
                projectEditControlStatus = value;
                RaisePropertyChanged("ProjectEditControlStatus");
            }
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

        public long APPLICATION_ID { get; set; }
        public string APP_SHORT_NAME { get; set; }
        public Nullable<short> VERSION { get; set; }

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }

            set
            {

            }

        }

        public ICommand ClearCommand
        {
            get
            {
                return _clearCommand;
            }

            set
            {

            }

        }

        public ObservableCollection<B_REGISTERED_APPS> RegisterdApplication
        {
            get
            {
                return _registerdApplication;
            }
            set
            {
                _registerdApplication = value;
                RaisePropertyChanged("RegisterdApplication");
            }
        }
        private string _editButtonCaption = "Edit";
        public string editButtonCaption
        {
            get { return _editButtonCaption; }
            set {
                _editButtonCaption = value;
                RaisePropertyChanged("editButtonCaption");
            }
        }
        public void EnableEditCommand()
        {
            if (string.Compare("Edit", _editButtonCaption,true)==0)
            {
                _editButtonCaption = "Read Only";
                isShowEditMode = Visibility.Visible;
            }
            else
            {
                isShowEditMode = Visibility.Collapsed;
                _editButtonCaption = "Edit";
            }
        }

        public bool SaveProject()
        {
            
            B_TEST_PROJECT bTestProject = new B_TEST_PROJECT();
            List<IMarsTigerTranscation> lstChecked = new List<IMarsTigerTranscation>(),
                lstUnChecked = new List<IMarsTigerTranscation>();
            string strError = "";
            bool bOk = MarsTransactionMgr.BeginTransaction(MarsMainWindow.CurrentDatabaseIdx,ref strError);
            if (!bOk)
            {
                HintByMessageBox(string.Format("Can't start DB Transcation with error:[{0}]", strError), "Hint");
                return bOk;
            }

            //long ProjectId = bTestProject.getProjectId(ProjectName);
            foreach (B_TEST_SUITE testSuite in TestSuite)
            {
                #region old code
                //if (testSuite.IsSelected && (marsEntities.REL_TEST_SUIT_PROJECT.FirstOrDefault(x => x.TEST_SUITE_ID == testSuite.TEST_SUITE_ID && x.PROJECT_ID == this.project_id) == null))
                #endregion //old code
                if (testSuite.IsSelected)
                {
                    B_REL_TEST_SUIT_PROJECT bRelTestSuiteProject = new B_REL_TEST_SUIT_PROJECT();
                    bRelTestSuiteProject.PROJECT_ID = project_id;
                    bRelTestSuiteProject.TEST_SUITE_ID = testSuite.TEST_SUITE_ID;
                    bRelTestSuiteProject.RELATIONSHIP_ID = -1;// bRelTestSuiteProject.getRelTestSuiteProject();
                    #region old code
                    //marsEntities.REL_TEST_SUIT_PROJECT.Add(REL_TEST_SUIT_PROJECTAssembler.ToEntity(bRelTestSuiteProject));
                    #endregion //old code

                    lstChecked.Add(bRelTestSuiteProject);
                }
                #region old code
                //else if (!testSuite.IsSelected && (marsEntities.REL_TEST_SUIT_PROJECT.FirstOrDefault(x => x.TEST_SUITE_ID == testSuite.TEST_SUITE_ID && x.PROJECT_ID == this.project_id) != null))
                #endregion //old code
                else if (!testSuite.IsSelected)
                {
                    #region old code
                    //Not selected now but was selected earlier then remove
                    //var bRelTestSuiteProject = marsEntities.REL_TEST_SUIT_PROJECT.FirstOrDefault(x => x.TEST_SUITE_ID == testSuite.TEST_SUITE_ID && x.PROJECT_ID == this.project_id);
                    //if (bRelTestSuiteProject!= null)
                    //    marsEntities.REL_TEST_SUIT_PROJECT.Remove(bRelTestSuiteProject);
                    #endregion //old code
                    B_REL_TEST_SUIT_PROJECT bRelTestSuiteProject = new B_REL_TEST_SUIT_PROJECT();
                    bRelTestSuiteProject.PROJECT_ID = project_id;
                    bRelTestSuiteProject.TEST_SUITE_ID = testSuite.TEST_SUITE_ID;

                    lstUnChecked.Add(bRelTestSuiteProject);
                }
            }
            bOk = MarsTransactionMgr.AddList(MarsMainWindow.CurrentDatabaseIdx, lstChecked,ref strError);
            
            if (!bOk)
            {
                HintByMessageBox(string.Format("Can't add new record(s) to database with error:[{0}]",strError),"Error");
                return false;
            }
            bOk = bOk && MarsTransactionMgr.RemoveList(lstUnChecked, ref strError);
            if (!bOk)
            {
                HintByMessageBox(string.Format("Can't delete record(s) from database with error:[{0}]", strError), "Error");
                return false;
            }

            #region old code
            //if (marsEntities.SaveChanges() > 0)
            //{
            //    MarsTreeView.GetMarsTree();
            //    //MessageBox.Show("Project, test suite relation saved successfully");
            //    ProjectEditControlStatus = "Project, test suite relation saved successfully";
            //    ClearProject();
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
            #endregion //old code

            try
            {
                bOk = MarsTransactionMgr.SaveChangesToTranscation(ref strError);
                if (!bOk)
                {
                    Logger.Error("SaveProject", string.Format("Error when call SaveChangesToTranscation:[{0}]", strError));
                    HintByMessageBox(string.Format("Can't save projects with Error:\r\n[{0}]", strError), "Error");
                    return bOk;
                }

                HintByMessageBox(string.Format("Project [{0}] setting saved sucessfully!", this.ProjectName), "Hint");
                MarsTreeView.GetTestSuiteByProjectId(this.project_id, 
                    this._marsProjectTreeView.TEST_FOLDER[2].TREE_ITEM, 
                    this._marsProjectTreeView.TEST_FOLDER[2],
                    B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(MarsMainWindow.CurrentDatabaseIdx, ref strError, ref bOk));
                return bOk;
            }
            catch (Exception e)
            {
                Logger.Error("SaveProject", string.Format("Exception when call SaveChangesToTranscation:[{0}]",e.Message),e);
                HintByMessageBox(string.Format("Exception when save project setting. Error:\r\n[{0}]\r\nStackTrace:\r\n[{1}]",e.Message,e.StackTrace), "Error");
                return false;
            }
        }

        public bool DeleteProject(long lProjId) //string projectName)
        {
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:MarsMainWindow.CurrentDatabaseIdx);
            try
            {
                using (TransactionScope scope = new TransactionScope()) {             
                    var project = (from p in marsEntities.T_TEST_PROJECT
                                       //where p.PROJECT_NAME == projectName
                                   where p.PROJECT_ID == lProjId
                                   select p).FirstOrDefault();
                    if (project == null) return false;

                    var relAppProject = (from a in marsEntities.REL_APP_PROJ
                                         where a.PROJECT_ID == project.PROJECT_ID
                                         select a);
                    foreach (var a in relAppProject)
                    {
                        marsEntities.REL_APP_PROJ.Remove(a);
                    }

                    var relTestSuiteProject = (from r in marsEntities.REL_TEST_SUIT_PROJECT
                                               where r.PROJECT_ID == project.PROJECT_ID
                                               select r);
                    foreach (var r in relTestSuiteProject)
                    {
                        marsEntities.REL_TEST_SUIT_PROJECT.Remove(r);

                        //var relTestCaseTestSuite = (from s in marsEntities.REL_TEST_CASE_TEST_SUITE
                        //                            where s.T_TEST_SUITE == r.T_TEST_SUITE
                        //                            select s);
                        //foreach (var s in relTestCaseTestSuite)
                        //{
                        //    marsEntities.REL_TEST_CASE_TEST_SUITE.Remove(s);
                        //    var relTestStepTestCase = (from c in marsEntities.T_TEST_STEPS
                        //                               where c.TEST_CASE_ID == s.TEST_CASE_ID
                        //                               select c);
                        //    //Remove test steps
                        //}
                    }
                    var dp = from p in marsEntities.T_STORYBOARD_DATASET_SETTING
                             from s in marsEntities.T_PROJ_TC_MGR
                             where p.STORYBOARD_DETAIL_ID == s.STORYBOARD_DETAIL_ID
                             && s.PROJECT_ID == lProjId
                             select p;
                    foreach(var itm in dp)
                    {
                        if (itm == null) continue;
                        marsEntities.T_STORYBOARD_DATASET_SETTING.Remove(itm);
                    }
                    var q = from p in marsEntities.T_PROJ_TC_MGR
                            where p.PROJECT_ID == lProjId
                            select p;
                    foreach (var itm in q)
                    {
                        if (itm == null) continue;
                        marsEntities.T_PROJ_TC_MGR.Remove(itm);
                    }

                    var dpr = from p in marsEntities.T_PROJECT_DATA_SOURCE
                             where p.PROJECT_ID == lProjId
                             select p;
                    foreach (var itm in dpr)
                    {

                        if (itm == null) continue;
                        marsEntities.T_PROJECT_DATA_SOURCE.Remove(itm);
                    }

                    marsEntities.T_TEST_PROJECT.Remove(project);
                    marsEntities.SaveChanges();
                    scope.Complete();
                } // end of sope
                MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                //MessageBox.Show("Project deleted successfully", "Project Delete", MessageBoxButton.OK, MessageBoxImage.Information);
                ProjectEditControlStatus = "Project deleted successfully";
                ClearProject();
                return true;                   
                
            }                
            catch (Exception ex)
            {

                marsEntities = null;
                    MessageBox.Show(ex.InnerException.ToString(), "Project Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
            }
        }

        public void ClearProject()
        {
            
        }
        #region old code
        //public void GetTestSuite(string strprojectName)
        #endregion //old code
        public void GetTestSuite(long lProject)
        {
            B_TEST_SUITE objBTestSuite = new B_TEST_SUITE();
            List<B_TEST_SUITE> testSuite = objBTestSuite.GetTestSuite(MarsMainWindow.CurrentDatabaseIdx, lProject);           
           foreach (B_TEST_SUITE bTestSuite in testSuite)
            {
                TestSuite.Add(bTestSuite);
            }
        }

        public void GetTestSuite()
        {
            B_TEST_SUITE objBTestSuite = new B_TEST_SUITE();
            List<B_TEST_SUITE> testSuite = objBTestSuite.GetTestSuite(MarsMainWindow.CurrentDatabaseIdx);
            foreach (B_TEST_SUITE bTestSuite in testSuite)
            {
                TestSuite.Add(bTestSuite);
            }
        }

        public void GetApplication()
        {
            B_REGISTERED_APPS objBRegApps = new B_REGISTERED_APPS();
            List<B_REGISTERED_APPS> applications = objBRegApps.GetApplication(MarsMainWindow.CurrentDatabaseIdx);
            foreach (B_REGISTERED_APPS regApps in applications)
            {               
                regApps.TestSuite = objBRegApps.GetApplicationTestSuite(MarsMainWindow.CurrentDatabaseIdx, regApps.APPLICATION_ID);
                RegisterdApplication.Add(regApps);
            }
        }

        public ICommand RePopulateTestSuite
        {
            get
            {
                return _rePopulateTestSuite;
            }

            set
            { 
            }

        }

        public bool IsSelected
        {
            get {
             return   _isSelected;
            }
            set
            {
                _isSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }

        public B_REGISTERED_APPS SelectedApplication
        {
            get
            {
                return _selectedApplication;
            }
            set
            {
                _selectedApplication = value;
                if (!value.IsSelected)
                    _selectedApplication.IsSelected = true;
                else
                    _selectedApplication.IsSelected = false;
                RaisePropertyChanged("IsSelected");
                //PopulateTestSuite();
                RaisePropertyChanged("SelectedApplication");
            }
        }

        private B_REGISTERED_APPS PopulateTestSuite()
        {
            B_TEST_SUITE objBTestSuite = new B_TEST_SUITE();
            List<B_TEST_SUITE> testSuite = new List<B_TEST_SUITE>();
            TestSuite.Clear();
            foreach (var selApps in RegisterdApplication)
            {
                if (selApps.IsSelected)
                {
                    testSuite = objBTestSuite.GetApplicationTestSuite(MarsMainWindow.CurrentDatabaseIdx, selApps.APPLICATION_ID);
                    foreach (B_TEST_SUITE bTestSuite in testSuite)
                    {
                        TestSuite.Add(bTestSuite);
                    }
                }
            }
            return null;
        }
    }
}
