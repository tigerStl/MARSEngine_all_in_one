using Mars.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Mars.Dto;
using Mars.Model;
using System.Collections.ObjectModel;
using System.Security.Principal;
using Mars.Business;
using Mars.DataLayer;

namespace Mars.ViewModel
{
    public class ApplicationViewModel : ViewModelBase,IApplicationViewModel
    {
        long applicationId;
        string applicationName;
        string version;
        string extraRequirement;
        string extraPopupMenu;
        string applicationDescription;
        private ICommand _saveCommand;
        private ICommand _clearCommand;
        private ICommand _deleteCommand;
        private ICommand _editCommand;
        ObservableCollection<B_REGISTERED_APPS> _registerdApplication = new ObservableCollection<B_REGISTERED_APPS>();
        B_REGISTERED_APPS _selectedApplication;       
        MarsEntities marsEntities;
        string applicationControlStatus;

        //B_SYSTEM_LOOKUP _SystemLookup;

        List<string> _SystemLookupList;

        //public List<B_SYSTEM_LOOKUP> GetSystemLookup()

        private int iMode = 0; // 0- means amend, others means Create New
        private string _CaptionForActionButton;
        public string CaptionForActionButton
        {
            get {
                return _CaptionForActionButton;
            }
            set {
                _CaptionForActionButton = value;
                RaisePropertyChanged("CaptionForActionButton");
            }
        }
        public ApplicationViewModel(string strCmdWay= "Amend Application")
        {
            iMode = string.Compare(strCmdWay, "Amend Application",true)==0?0:1;

            marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            //marsEntities.Configuration.ProxyCreationEnabled = false;
            _saveCommand = new DelegateCommand(() => { SaveApplication(); });
            _clearCommand = new DelegateCommand(() => { ClearApplication(); });
            _deleteCommand = new DelegateCommand(() => { DeleteApplication(); });
            _editCommand = new DelegateCommand(() => { EditApplication(); });
            GetApplication();
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

        public B_REGISTERED_APPS SelectedApplication
        {
            get
            {
                return _selectedApplication;
            }
            set
            {
                _selectedApplication = value;
                RaisePropertyChanged("SelectedApplication");
            }
        }

        
        public long ApplicationId
        {
            get
            {
                return applicationId;
            }
            set
            {
                applicationId = value;
                RaisePropertyChanged("ApplicationId");
            }
        }

        public string ApplicationName
        {
            get
            {
                return applicationName;
            }
            set
            {
                applicationName = value;
                RaisePropertyChanged("ApplicationName");
            }
        }

        public string ApplicationDescription
        {
            get
            {
                return applicationDescription;
            }
            set
            {
                applicationDescription = value;
                RaisePropertyChanged("ApplicationDescription");
            }
        }

        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                version = value;
                RaisePropertyChanged("Version");
            }
        }

        public string ExtraRequirement
        {
            get
            {
                return extraRequirement;
            }
            set
            {
                extraRequirement = value;
                RaisePropertyChanged("ExtraRequirement");
            }
        }


        public string ExtraPopupMenu
        {
            get
            {
                return extraPopupMenu;
            }
            set
            {
                extraPopupMenu = value;
                RaisePropertyChanged("ExtraPopupMenu");
            }
        }

        /// <summary>

        public List<String> SystemLookupList
        {
            get
            {
                if (_SystemLookupList == null)
                    InitSystemLookupList();
                return _SystemLookupList;
            }
            
        }
        private const string CNST_EXTRA_REQ_SYSTEM_LOOKUP_TABLENAME = "T_REGISTERED_APPS";
        private const string CNST_EXTRA_REQ_SYSTEM_LOOKUP_FILEDNAME = "EXTAREQUIREMENT";
        private void InitSystemLookupList()
        {
            _SystemLookupList = new List<string>();
            B_SYSTEM_LOOKUP bs = new B_SYSTEM_LOOKUP();
            List<B_SYSTEM_LOOKUP> bsList = bs.GetSystemLookup(CNST_EXTRA_REQ_SYSTEM_LOOKUP_TABLENAME, CNST_EXTRA_REQ_SYSTEM_LOOKUP_FILEDNAME);
            if (bsList == null) return;
            foreach (B_SYSTEM_LOOKUP b in bsList)
            {
                string extra = b.DISPLAY_NAME;
                _SystemLookupList.Add(extra);
            }
        }

        /// </summary>

        public string ApplicationControlStatus
        {
            get
            {
                return applicationControlStatus;
            }
            set
            {
                applicationControlStatus = value;
                RaisePropertyChanged("ApplicationControlStatus");
            }
        }
               

        private long getApplicationId()
        {
            try
            {
                long applicationId = (long)marsEntities.T_REGISTERED_APPS.OrderByDescending(x => x.APPLICATION_ID).First().APPLICATION_ID;
                if (applicationId > 0)
                    return (applicationId + 1);
                else
                    return 1;
            }
            catch(Exception )
            {
                //log.Info(string.Format("getApplicationId,Exception:[{0}]", ex.Message), ex);
                return 1;
            }

        }


        public List<string> validationErrors = new List<string>();

        static readonly string[] ValidatedProperties =
        {
                "ApplicationName",
                "ApplicationDescription",
                "Version",
                //"ExtraRequirement",
                //"ExtraPopupMenu",
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
                case "ApplicationName":
                    error = this.ValidateApplicationName();
                    break;

                case "ApplicationDescription":
                    error = this.ValidateApplicationDescription();
                    break;

                case "Version":
                    error = this.ValidateApplicationVersion();
                    break;

                case "ExtraRequirement":
                    error = this.ValidateApplicationExtraRequirement();
                    break;

                //case "ExtraPopupMenu":
                //    error = this.ValidateApplicationExtraPopupMenu();
                //    break;

                default:
                    error = null;
                    //throw new Exception("Unexpected property being validated on Service");
                    break;
            }

            return error;
        }

        string ValidateApplicationVersion()
        {
            if (IsStringMissing(this.Version))
            {
                return "Application Version";
            }
            return null;
        }

        string ValidateApplicationExtraRequirement()
        {
            if (IsStringMissing(this.ExtraRequirement))
            {
                return "Extra Requirement";
            }
            return null;
        }

        string ValidateApplicationExtraPopupMenu()
        {
            if (IsStringMissing(this.ExtraPopupMenu))
            {
                return "Extra Popup Menu";
            }
            return null;
        }


        string ValidateApplicationDescription()
        {
            if (IsStringMissing(this.ApplicationDescription))
            {
                return "Application Description";
            }
            return null;
        }

        string ValidateApplicationName()
        {
            if (IsStringMissing(this.ApplicationName))
            {
                return "Application Name";
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



        public void GetApplication()
        {
            try
            {
                RegisterdApplication = new ObservableCollection<B_REGISTERED_APPS>();
                var applications = (from c in marsEntities.T_REGISTERED_APPS
                                    orderby c.APP_SHORT_NAME
                                    select c);

                foreach (T_REGISTERED_APPS regApps in applications)
                {
                    B_REGISTERED_APPS newRegApps = new B_REGISTERED_APPS();
                    newRegApps.APPLICATION_ID = regApps.APPLICATION_ID;
                    newRegApps.APP_SHORT_NAME = regApps.APP_SHORT_NAME;
                    newRegApps.PROCESS_IDENTIFIER = regApps.PROCESS_IDENTIFIER;
                    newRegApps.RECORD_CREATE_DATE = regApps.RECORD_CREATE_DATE;
                    newRegApps.VERSION = regApps.VERSION;
                    newRegApps.EXTRAREQUIREMENT = regApps.EXTRAREQUIREMENT;
                    newRegApps.EXTRAPOPUPMENU = regApps.EXTRAPOPUPMENU;

                    RegisterdApplication.Add(newRegApps);
                }

            }
            catch(Exception ex) {
                Console.WriteLine(ex.InnerException);
            }
       }

        public bool DeleteApplication()
        {
            if (!ValidateRegisteredApplication())
            {
                //MessageBox.Show("Please select the application ", "Application Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                ApplicationControlStatus = "Please select the application";
                return false;
            }

            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
            if (messageBoxResult != MessageBoxResult.Yes)
            {
                return false;
            }

            foreach (B_REGISTERED_APPS regApp in RegisterdApplication)
            {
                if (regApp.IsSelected)
                {
                    //remove the selected app 
                    var application = (from a in marsEntities.T_REGISTERED_APPS
                                   where a.APPLICATION_ID == regApp.APPLICATION_ID
                                   select a).FirstOrDefault();

                    marsEntities.T_REGISTERED_APPS.Remove(application);

                    // remove relationship between app and project
                    var relAppProject = (from a in marsEntities.REL_APP_PROJ
                                         where a.APPLICATION_ID == regApp.APPLICATION_ID
                                         select a);
                    foreach (var a in relAppProject)
                    {
                        marsEntities.REL_APP_PROJ.Remove(a);
                    }

                    // remove relationship between app and test case
                    var relAppTestCase = (from a in marsEntities.REL_APP_TESTCASE
                                         where a.APPLICATION_ID == regApp.APPLICATION_ID
                                         select a);

                    foreach (var a in relAppTestCase)
                    {
                        marsEntities.REL_APP_TESTCASE.Remove(a);
                    }

                    // remove relationship between app and test suite
                    var relAppTestSuite = (from a in marsEntities.REL_APP_TESTSUITE
                                         where a.APPLICATION_ID == regApp.APPLICATION_ID
                                         select a);
                    foreach (var a in relAppTestSuite)
                    {
                        marsEntities.REL_APP_TESTSUITE.Remove(a);
                    }
                }
            }

            if (marsEntities.SaveChanges() > 0)
            {
                GetApplication();
                RaisePropertyChanged("RegisterdApplication");
                //MessageBox.Show("Application is deleleted successfully");
                ApplicationControlStatus = "Application is deleleted successfully";
                return true;
            }
            else
            {
                //MessageBox.Show("Failed to delete the Application");
                ApplicationControlStatus = "Failed to delete the Application";
                return false;
            }
        }

        public bool EditApplication()
        {
            //SelectedApplication
            if(SelectedApplication==null)
            {
                ApplicationControlStatus = "Please select application to edit";
                return false;
            }

            //save the edited selectedApplication       
            var selectedApplicationEntity = marsEntities.T_REGISTERED_APPS.First(x => x.APPLICATION_ID == SelectedApplication.APPLICATION_ID);
            selectedApplicationEntity.PROCESS_IDENTIFIER = SelectedApplication.PROCESS_IDENTIFIER;
            // marsEnties saveChanges return codes ? for success and failures
            if (marsEntities.SaveChanges() > 0)
            {
                GetApplication();
                RaisePropertyChanged("SelectedApplication");
                //MessageBox.Show("Application description saved successfully");
                ApplicationControlStatus = "Application description saved successfully";
                return true;
            }
            else
            {
                //MessageBox.Show("Failed to save Application description");
                ApplicationControlStatus = "Failed to save Application description";
                return false;
            }
        }

        public void ClearApplication()
        {
            ApplicationId = 0;
            ApplicationName = "";
            ApplicationDescription = "";
            Version = "";
            ExtraRequirement = "";
            ExtraPopupMenu = "";
        }

        public bool SaveApplication()
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
                //MessageBox.Show(sbError.ToString(), "Application Add", MessageBoxButton.OK, MessageBoxImage.Error);
                ApplicationControlStatus = sbError.ToString();
                validationErrors.Clear();
                return false;
            }

            T_REGISTERED_APPSDTO appsDto = new T_REGISTERED_APPSDTO();
            B_REGISTERED_APPS bRegApps = new B_REGISTERED_APPS();
            if (!bRegApps.applicationExists(MarsMainWindow.CurrentDatabaseIdx, applicationName.ToUpper(), version))
            {
                WindowsIdentity ident = WindowsIdentity.GetCurrent();
                appsDto.APPLICATION_ID = bRegApps.getApplicationId(MarsMainWindow.CurrentDatabaseIdx);
                appsDto.APP_SHORT_NAME = applicationName;
                appsDto.PROCESS_IDENTIFIER = applicationDescription;
                appsDto.RECORD_CREATE_PERSON = ident.Name.ToString();
                appsDto.RECORD_CREATE_DATE = DateTime.Now;
                appsDto.VERSION = version;

                appsDto.EXTRAREQUIREMENT = extraRequirement;
                appsDto.EXTRAPOPUPMENU = extraPopupMenu;
                marsEntities.T_REGISTERED_APPS.Add(T_REGISTERED_APPSAssembler.ToEntity(appsDto));

                try
                {
                    if (marsEntities.SaveChanges() > 0)
                    {
                        GetApplication();
                        RaisePropertyChanged("RegisterdApplication");

                        string strError4UpdateCache = "";
                        if (!MarsDBGlobe_Cache.updateApplicationCache(MarsMainWindow.CurrentDatabaseIdx, ref strError4UpdateCache))
                            ApplicationControlStatus = strError4UpdateCache;
                        else
                        //MessageBox.Show("Application saved successfully");
                        ApplicationControlStatus = "Application saved successfully";
                        ClearApplication();
                        return true;
                    }
                    else
                    {
                        //MessageBox.Show("Failed to save the Application");
                        ApplicationControlStatus = "Failed to save the Application";
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to save the Application" + ex.StackTrace.ToString());
                    return false;
                }
            }
           
          //MessageBox.Show("Application Already Exists");
            ApplicationControlStatus = "Application Already Exists";
          return false;      
        }

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

        public ICommand DeleteCommand
        {
            get
            {
                return _deleteCommand;
            }

            set
            {

            }

        }

        public ICommand EditCommand
        {
            get
            {
                return _editCommand;
            }

            set
            {

            }

        }

    
    }
}
