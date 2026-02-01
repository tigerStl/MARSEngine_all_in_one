using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Model;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Security.Principal;
using Mars.Business;
using Mars.Dto;
using Route2NSEx.src.Marquis.systemUtil;
using Mars.DataLayer;
using Mars.DataLayer.Generic;

namespace Mars.ViewModel
{
    public class ProjectViewModel : ViewModelBase, IProjectViewModel
    {
        private static MLogger log = MLogger.GetLogger(typeof(ProjectViewModel));
        long projectId;
        string projectName;
        string projectDescription;
        bool isOpen;       
        ObservableCollection<B_REGISTERED_APPS> _registerdApplication = new ObservableCollection<B_REGISTERED_APPS>();
        private ICommand _saveCommand;
        private ICommand _clearCommand;
        private ICommand _enableEditCommand;
        MarsEntities marsEntities;
        ObservableCollection<B_TEST_PROJECT> _projectList = new ObservableCollection<B_TEST_PROJECT>();
        long _selectedProject;
        string projectControlStatus;


        private bool _isEnableEdit = true;
        public bool isEnableEdit
        {
            get {
                return _isEnableEdit;
            }
            set {
                _isEnableEdit = value;
                RaisePropertyChanged("isEnableEdit");
            }
        }

        private string _editButtonCaption="Edit";
        public string editButtonCaption
        {
            get { return _editButtonCaption; }
            set { _editButtonCaption = value;
                RaisePropertyChanged("editButtonCaption");
            }
        }

        public ProjectViewModel()
        {
            CommonLoad();
            isOpen = false;
            GetApplication();
#if tiger_dock
            Title = "Project Add";
#endif
        }

        #region old code
        //public ProjectViewModel(string projectName,long lProjectId = -1)
        #endregion //old code
        public ProjectViewModel(long lProjectId)
        {
            CommonLoad();
            marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            isOpen = true;
            
            #region old code 
            //var project = marsEntities.T_TEST_PROJECT.FirstOrDefault(x => x.PROJECT_NAME == projectName);
            //ProjectId = project.PROJECT_ID;
            #endregion //old code
            var project = marsEntities.T_TEST_PROJECT.FirstOrDefault(x => x.PROJECT_ID==lProjectId);

            if (project==null)
            {
                HintByMessageBox(string.Format("Can't find project info by id:[{0}]", lProjectId), "Error");
                return ;
            }

            ProjectId = lProjectId;
            ProjectName = project.PROJECT_NAME;
            ProjectStatus = (int)project.STATUS;
            ProjectDescription = project.PROJECT_DESCRIPTION;
            GetProject();
            GetApplication(lProjectId);
            SelectedProject = project.PROJECT_ID;
#if tiger_dock
            Title = string.Format("Project:[{0}]", ProjectName);
#endif     
        }
        private string title;
        public string Title { get { return title; }
            set {
                title = value;
                RaisePropertyChanged("Title");
            }
        }

        private void CommonLoad()
        {
            _saveCommand = new DelegateCommand(() => { SaveProject(); });
            _clearCommand = new DelegateCommand(() => { ClearProject(); });
            _enableEditCommand = new DelegateCommand(()=> { EnableEditModeCommand(); });
            RegisterdApplication = new ObservableCollection<B_REGISTERED_APPS>();
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

        public ObservableCollection<B_TEST_PROJECT> ProjectList
        {
            get
            {
                return _projectList;
            }
            set
            {
                _projectList = value;
                RaisePropertyChanged("ProjectList");
            }
        }

        public long SelectedProject
        {
            get
            {
                return _selectedProject;
            }
            set
            {
                _selectedProject = value;
                RaisePropertyChanged("SelectedProject");
            }
        }   

        public long ProjectId 
        { 
            get
            {
                return projectId;
            }
            set
            {
                    projectId = value;
                    RaisePropertyChanged("ProjectId");                
            } 
        }

        public string ProjectName 
        {
            get
            {
                return projectName;
            }
            set
            {
                projectName = value;
                RaisePropertyChanged("ProjectName");
            } 
        }

        public string ProjectDescription 
        {
            get
            {
                return projectDescription;
            }
            set
            {
                projectDescription = value;
                RaisePropertyChanged("ProjectDescription");
            }  
        }

        public int ProjectStatus
        {
            get
            {
                return _projectStatus;
            }
            set
            {
                _projectStatus = value;
                RaisePropertyChanged("ProjectStatus");
            }
        }

        public string ProjectControlStatus
        {
            get
            {
                return projectControlStatus;
            }
            set
            {
                projectControlStatus = value;
                RaisePropertyChanged("ProjectControlStatus");
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }

            set
            {}

        }

        public ICommand EnableEditCommand
        {
            get { return _enableEditCommand; } 
        }

        public ICommand ClearCommand
        {
            get
            {
                return _clearCommand;
            }

            set
            {}

        }

        Dictionary<int, string> _statusSource = new Dictionary<int, string>();
        int _projectStatus = 0;
        private Dictionary<int, string> GetSource()
        {
            _statusSource.Add(1, "Select Status");
            _statusSource.Add(2, "Edit");
            _statusSource.Add(3, "Ready to Test");

            return _statusSource;
        }

        public Dictionary<int, string> StatusSource
        {
            get
            {
                return GetSource();
            }
        }


        public List<string> validationErrors = new List<string>();

        static readonly string[] ValidatedProperties =
        {
                "ProjectName",
                "ProjectDescription",
                "ProjectStatus",
        };

        public bool IsValid
        {
            get
            {
                foreach (string property in ValidatedProperties)
                {
                    if (GetValidationError(property) != null)
                        validationErrors.Add(GetValidationError(property));
                }
                if (validationErrors.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }

        private string GetValidationError(string propertyName)
        {
            string error = null;

            switch (propertyName)
            {
                case "ProjectName":
                    error = this.ValidateProjectName();
                    break;

                case "ProjectDescription":
                    error = this.ValidateProjectDescription();
                    break;

                case "ProjectStatus":
                    error = this.ValidateProjectStatus();
                    break;
                default:
                    error = null;
                    throw new Exception("Unexpected property being validated on Service");
            }

            return error;
        }

        string ValidateProjectStatus()
        {
            if (this.ProjectStatus < 1)
            {
                return "Project Status";
            }
            return null;
        }

        string ValidateProjectDescription()
        {
            if (IsStringMissing(this.ProjectDescription))
            {
                return "Project Description";
            }
            return null;
        }

        string ValidateProjectName()
        {
            if (IsStringMissing(this.ProjectName))
            {
                return "Project Name";
            }
            return null;
        }

        static bool IsStringMissing(string value)
        {
            return
                String.IsNullOrEmpty(value) ||
                value.Trim() == String.Empty;
        }

        private bool ValidateRegisteredApplication()
        {
            foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
            {
                if (regApps.IsSelected)
                {
                    return true;
                }
            }
            return false;
        }


        public void EnableEditModeCommand()
        {
            this.isEnableEdit = true;
            //if (string.Compare(this._editButtonCaption,"Edit", true)==0)
            //{
            //    this.editButtonCaption = "Read Only";
            //    this.isEnableEdit = true;
            //}
            //else
            //{
            //    this.editButtonCaption = "Edit";
            //    this.isEnableEdit = false;
            //}
        }

        public bool SaveProject()
        {
            if (!IsValid)
            {
                StringBuilder sbError = new StringBuilder();
                sbError.Append("Please enter valid :");

                foreach (string error in validationErrors)
                {
                    sbError.Append(error);
                    sbError.Append(" : ");
                }
                //MessageBox.Show(sbError.ToString(), "Project Add", MessageBoxButton.OK, MessageBoxImage.Error);
                ProjectControlStatus = sbError.ToString();
                validationErrors.Clear();
                return false;
            }

            if (!ValidateRegisteredApplication())
            {
                //MessageBox.Show("Please select application for the project.", "Project Add", MessageBoxButton.OK, MessageBoxImage.Error);
                ProjectControlStatus = "Please select application for the project.";
                return false;
            }
#region old code
            //marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            //T_TEST_PROJECTDTO bProject = new T_TEST_PROJECTDTO();
#endregion //old code
            B_TEST_PROJECT bTestProject = new B_TEST_PROJECT();
            B_REL_APP_PROJ bRelAppProj = new B_REL_APP_PROJ();
            WindowsIdentity ident = WindowsIdentity.GetCurrent();

            //if (!bTestProject.ProjectExists(projectName) && isOpen == false)
            if (isOpen == false)
            {
                // Create a new project
                string strError = "";
                bool isTranscationReady = MarsTransactionMgr.BeginTransaction(MarsMainWindow.CurrentDatabaseIdx, ref strError);
#region old Code
                //bProject.PROJECT_ID = bTestProject.getProjectId();
                //bProject.PROJECT_NAME = projectName;
                //bProject.PROJECT_DESCRIPTION = projectDescription;
                //bProject.CREATOR = ident.Name.ToString();
                //bProject.CREATE_DATE = DateTime.Now;
                //bProject.STATUS = Convert.ToInt16(ProjectStatus);
                //marsEntities.T_TEST_PROJECT.Add(T_TEST_PROJECTAssembler.ToEntity(bProject));
#endregion //old Code

                bTestProject.PROJECT_ID = -1;
                bTestProject.PROJECT_NAME = projectName;
                bTestProject.PROJECT_DESCRIPTION = projectDescription;
                bTestProject.CREATOR = ident.Name.ToString();
                bTestProject.CREATE_DATE = DateTime.Now;
                bTestProject.STATUS = Convert.ToInt16(ProjectStatus);
                List<IMarsTigerTranscation> lstProj = new List<IMarsTigerTranscation>();
                lstProj.Add(bTestProject);

                isTranscationReady = MarsTransactionMgr.AddList(MarsMainWindow.CurrentDatabaseIdx, lstProj, ref strError);
                if (!isTranscationReady)
                {
                    HintByMessageBox(string.Format("Can't Add currently project:\r\n[{0}]", strError), "Error");
                    return false;
                }
                List<IMarsTigerTranscation> lstCheckedRelation = new List<IMarsTigerTranscation>();
                foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
                {
                    if (regApps.IsSelected)
                    {
#region old code
                        //REL_APP_PROJ relAppProj = new REL_APP_PROJ();
                        //relAppProj.RELATIONSHIP_ID = bRelAppProj.getRelAppProjId();
                        //relAppProj.APPLICATION_ID = regApps.APPLICATION_ID;
                        //relAppProj.PROJECT_ID = bProject.PROJECT_ID;
                        //marsEntities.REL_APP_PROJ.Add(relAppProj);
#endregion //old code

                        B_REL_APP_PROJ relAppProj = new B_REL_APP_PROJ();
                        relAppProj.RELATIONSHIP_ID = -1;
                        //relAppProj.RELATIONSHIP_ID = bRelAppProj.getRelAppProjId();
                        relAppProj.APPLICATION_ID = regApps.APPLICATION_ID;
                        relAppProj.PROJECT_ID = bTestProject.PROJECT_ID;

                        lstCheckedRelation.Add(relAppProj);
                    }

                }
                if (lstCheckedRelation.Count>0)
                    isTranscationReady = MarsTransactionMgr.AddList(MarsMainWindow.CurrentDatabaseIdx, lstCheckedRelation, ref strError);
                if (!isTranscationReady)
                {
                    HintByMessageBox(string.Format("Can't Add Relationed Applications:\r\n[{0}]", strError), "Error");
                    return false;
                }
                try
                {
#region old code
                    //if (marsEntities.SaveChanges() > 0)
                    //{
                    //    MarsTreeView.GetMarsTree();
                    //    //MessageBox.Show("Project saved successfully", "Project Add", MessageBoxButton.OK, MessageBoxImage.Information);
                    //    ProjectControlStatus = "Project saved successfully";
                    //    ClearProject();
                    //    return true;
                    //}
                    //else
                    //{
                    //    marsEntities = null;
                    //    //MessageBox.Show("Error saving project", "Project Add", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //    ProjectControlStatus = "Error saving project";
                    //    return false;
                    //}
#endregion //old code

                    if (MarsTransactionMgr.SaveChangesToTranscation(ref strError))
                    {
                        MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                        //MessageBox.Show("Project amedned successfully", "Project Open", MessageBoxButton.OK, MessageBoxImage.Information);
                        ProjectControlStatus = "Project saved successfully";
                        ViewModelBase.HintByMessageBox(ProjectControlStatus, "Hint");
                        //ClearProject();
                        return true;
                    }
                    else
                    {
                        ProjectControlStatus = "Error saving project";
                        ViewModelBase.HintByMessageBox(string.Format("Can't save changes with error:\r\n[{0}]", strError), "Error");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    marsEntities = null;
                    log.Error("SaveProject" , ex.StackTrace.ToString(),ex);
                    MessageBox.Show(ex.InnerException.ToString(), "Project Add", MessageBoxButton.OK, MessageBoxImage.Error);
                    //ProjectControlStatus = ex.InnerException.ToString();
                    return false;
                }               
            }
            else if (isOpen == true)
            {
                try
                {
                    string strError = "";
                    bool isTranscationReady = MarsTransactionMgr.BeginTransaction(MarsMainWindow.CurrentDatabaseIdx, ref strError);
                    if (!isTranscationReady)
                    {
                        HintByMessageBox(string.Format("Can't modify currently:\r\n[{0}]",strError), "Error");
                        return false;
                    }
                    /****
                    * var project = marsEntities.T_TEST_PROJECT.FirstOrDefault(x => x.PROJECT_NAME == projectName);
                    * var project = marsEntities.T_TEST_PROJECT.FirstOrDefault(x => x.PROJECT_ID==this.projectId);
                    ***/
                    B_TEST_PROJECT project = B_TEST_PROJECT.GetProjectById(MarsMainWindow.CurrentDatabaseIdx, this.projectId);
                    if (project==null)
                    {
                        return false;
                    }
                    project.PROJECT_DESCRIPTION = ProjectDescription;
                    project.CREATOR = ident.Name.ToString();
                    project.CREATE_DATE = DateTime.Now;
                    project.STATUS = Convert.ToInt16(ProjectStatus);
                    project.PROJECT_NAME = this.projectName;

                    isTranscationReady = MarsTransactionMgr.AddModification(project,ref strError);
                    if (!isTranscationReady)
                    {
                        HintByMessageBox(string.Format("Can't modify currently:\r\n[{0}]", strError), "Error");
                        return false;
                    }
                    /// 修改如下：
                    /// 1，获得所有的 checked对象
                    /// 2，获得所有的 an-checked的对象
                    /// 
                    List<IMarsTigerTranscation> lstUncheckedRelation = new List<IMarsTigerTranscation>(),
                        lstCheckedRelation = new List<IMarsTigerTranscation>();
                    foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
                    {
                        if (regApps.IsSelected)
                        {
                            B_REL_APP_PROJ objToAdd = new B_REL_APP_PROJ();
                            objToAdd.APPLICATION_ID = regApps.APPLICATION_ID;
                            objToAdd.PROJECT_ID = this.projectId;
                            objToAdd.RELATIONSHIP_ID = -1;
                            lstCheckedRelation.Add(objToAdd);
                            //marsEntities.REL_APP_PROJ.Add(relAppProj);
                        }
                        else 
                        {
                            B_REL_APP_PROJ objToDel = new B_REL_APP_PROJ();
                            objToDel.APPLICATION_ID = regApps.APPLICATION_ID;
                            objToDel.PROJECT_ID = this.projectId;
                            objToDel.RELATIONSHIP_ID = -1;
                            lstUncheckedRelation.Add(objToDel);                            
                        }
                    }
                    isTranscationReady = MarsTransactionMgr.RemoveList(lstUncheckedRelation,ref strError);
                    isTranscationReady = MarsTransactionMgr.AddList(MarsMainWindow.CurrentDatabaseIdx, lstCheckedRelation,ref strError );

#region old Code
                    //foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
                    //{
                    //    if (regApps.IsSelected && (marsEntities.REL_APP_PROJ.FirstOrDefault(x => x.PROJECT_ID == project.PROJECT_ID && x.APPLICATION_ID == regApps.APPLICATION_ID) == null))
                    //    {
                    //        B_REL_APP_PROJ objRelAppPrj = B_REL_APP_PROJ.CreateNewObject(regApps.APPLICATION_ID, project.PROJECT_ID);
                    //        REL_APP_PROJ relAppProj = new REL_APP_PROJ();
                    //        relAppProj.RELATIONSHIP_ID = bRelAppProj.getRelAppProjId();
                    //        relAppProj.APPLICATION_ID = regApps.APPLICATION_ID;
                    //        relAppProj.PROJECT_ID = project.PROJECT_ID;
                    //        marsEntities.REL_APP_PROJ.Add(relAppProj);
                    //    }
                    //    else if (!regApps.IsSelected && (marsEntities.REL_APP_PROJ.FirstOrDefault(x => x.PROJECT_ID == project.PROJECT_ID && x.APPLICATION_ID == regApps.APPLICATION_ID) != null))
                    //    {
                    //        var RelAppProj = marsEntities.REL_APP_PROJ.FirstOrDefault(x => x.PROJECT_ID == project.PROJECT_ID && x.APPLICATION_ID == regApps.APPLICATION_ID);
                    //        if (RelAppProj != null)
                    //            marsEntities.REL_APP_PROJ.Remove(RelAppProj);
                    //    }
                    //}
#endregion //old code
                    try
                    {
                        if (MarsTransactionMgr.SaveChangesToTranscation(ref strError))
                        {
                            MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                            //MessageBox.Show("Project amedned successfully", "Project Open", MessageBoxButton.OK, MessageBoxImage.Information);
                            ProjectControlStatus = "Project amedned successfully";
                            ViewModelBase.HintByMessageBox(ProjectControlStatus, "Hint");
                            //ClearProject();
                            return true;
                        }
                        else
                        {
                            ProjectControlStatus = "Error saving project";
                            ViewModelBase.HintByMessageBox(string.Format("Can't save changes with error:\r\n[{0}]",strError), "Error");
                            return false;
                        }

#region old Code
                        //if (marsEntities.SaveChanges() > 0)
                        //{
                        //    MarsTreeView.GetMarsTree();
                        //    //MessageBox.Show("Project amedned successfully", "Project Open", MessageBoxButton.OK, MessageBoxImage.Information);
                        //    ProjectControlStatus = "Project amedned successfully";
                        //    ClearProject();
                        //    return true;
                        //}
                        //else
                        //{
                        //    marsEntities = null;
                        //    //MessageBox.Show("Error saving project", "Project Open", MessageBoxButton.OK, MessageBoxImage.Warning);
                        //    ProjectControlStatus = "Error saving project";
                        //    return false;
                        //}
#endregion //old code
                    }
                    catch (Exception ex)
                    {
                        //marsEntities = null;
                        MessageBox.Show(ex.InnerException.ToString(), "Project Open", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                }
                catch(Exception ex)
                {
                    //marsEntities = null;
                    MessageBox.Show(ex.InnerException.ToString(), "Project Open", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            //MessageBox.Show("Project Already Exists");
            ProjectControlStatus = "Project Already Exists";
            return false;            
        }
               
        public void ClearProject()
        {
            ProjectId = 0;
            ProjectName = "";
            ProjectDescription = "";
            ProjectStatus = 0;
        }

        public void GetApplication()
        {
            B_REGISTERED_APPS objBRegApps = new B_REGISTERED_APPS();
            List<B_REGISTERED_APPS> applications = objBRegApps.GetApplication(MarsMainWindow.CurrentDatabaseIdx);
            foreach (B_REGISTERED_APPS regApps in applications)
            {
                RegisterdApplication.Add(regApps);
            }                   
        }

        public void GetApplication(long lprojectId)
        {
            B_REGISTERED_APPS objBRegApps = new B_REGISTERED_APPS();
            List<B_REGISTERED_APPS> applications = objBRegApps.GetProjectApplication(MarsMainWindow.CurrentDatabaseIdx, lprojectId);
            foreach (B_REGISTERED_APPS regApps in applications)
            {
                RegisterdApplication.Add(regApps);
            }
        }

        public void GetProject()
        {
            B_TEST_PROJECT objProject = new B_TEST_PROJECT();
            List<B_TEST_PROJECT> projectList = objProject.GetProject(MarsMainWindow.CurrentDatabaseIdx);
            foreach (B_TEST_PROJECT project in projectList)
            {
                ProjectList.Add(project);
            }
        }
        
        private B_TEST_PROJECT ConvertToBTestProject(T_TEST_PROJECT project)
        {
            B_TEST_PROJECT bProject = new B_TEST_PROJECT();
            bProject.PROJECT_ID = project.PROJECT_ID;
            bProject.PROJECT_NAME = project.PROJECT_NAME;
            bProject.PROJECT_DESCRIPTION = project.PROJECT_DESCRIPTION;
            return bProject;
        }
    }
}
