using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Dto;
using System.Windows.Input;
using Mars.Model;
using System.Collections.ObjectModel;
using System.Security.Principal;
using Mars.Business;
using System.Windows;
using Mars.DataLayer;
namespace Mars.ViewModel
{
    public class TestSuiteViewModel : ViewModelBase, ITestSuiteViewModel
    {
        long testSuiteId;
        string testSuiteName;
        string testSuiteDescription;
        public ObservableCollection<B_REGISTERED_APPS> RegisterdApplication { get; set; }
        private ICommand _saveCommand;
        private ICommand _clearCommand;
        private ICommand _EditCommand;
        MarsEntities marsEntities;
        bool isOpen;

        private Visibility _isShowEditMode = Visibility.Collapsed;
        public Visibility isShowEditMode
        {
            get { return _isShowEditMode; }
            set
            {
                _isShowEditMode = value;
                RaisePropertyChanged("isShowEditMode");
            }
        }

        private bool isEditMode = false;
        public bool IsEditMode
        {
            get { return isEditMode; }
            set { isEditMode = value;
                RaisePropertyChanged("IsEditMode");
            }
        }



        public TestSuiteViewModel()
        {
            isOpen = false;
            isShowEditMode = Visibility.Collapsed;
            IsEditMode = true;
            CommonLoad();
            GetApplication();
        }

        private void CommonLoad()
        {
            marsEntities =BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            _saveCommand = new DelegateCommand(() => { SaveTestSuite(); });
            _clearCommand = new DelegateCommand(() => { ClearTestSuite(); });
            _EditCommand = new DelegateCommand(()=> { OnEditButtonClick(); });
            RegisterdApplication = new ObservableCollection<B_REGISTERED_APPS>();
        }

        public TestSuiteViewModel(string testSuiteName, bool isEditMode, long lTestSuiteId)
        {
            isOpen = true;
            CommonLoad();
            //var testSuite = marsEntities.T_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_NAME == testSuiteName);
            var testSuite = marsEntities.T_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_ID== lTestSuiteId);
            //isShowEditMode = isEditMode ? Visibility.Visible : Visibility.Collapsed;
            isShowEditMode = Visibility.Visible;
            TestSuiteName = testSuite.TEST_SUITE_NAME;
            TestSuiteId = testSuite.TEST_SUITE_ID;  
            TestSuiteDescription = testSuite.TEST_SUITE_DESCRIPTION;
            GetApplication(testSuiteName);
        }

        private string _editButtonCaption = "Edit";
        public string editButtonCaption
        {
            get { return _editButtonCaption; }
            set { _editButtonCaption = value;
                RaisePropertyChanged("editButtonCaption");
            }
        }
        public void OnEditButtonClick()
        {
            if (string.Compare("Edit", _editButtonCaption,true)==0)
            {
                editButtonCaption = "Read Only";
                IsEditMode = true;
            }
            else
            {
                editButtonCaption = "Edit";
                IsEditMode = false;
            }
        }
        public void ClearTestSuite()
        {
            TestSuiteId = 0;
            TestSuiteName = "";
            TestSuiteDescription = "";
           // ResetApplicationSelection();
        }

        public long TestSuiteId
        {
            get
            {
                return testSuiteId;
            }
            set
            {
                testSuiteId = value;
                RaisePropertyChanged("TestSuiteId");
            }
        }

        public string TestSuiteName
        {
            get
            {
                return testSuiteName;
            }
            set
            {
                testSuiteName = value;
                RaisePropertyChanged("TestSuiteName");
            }
        }

        public string TestSuiteDescription
        {
            get
            {
                return testSuiteDescription;
            }
            set
            {
                testSuiteDescription = value;
                RaisePropertyChanged("TestSuiteDescription");
            }
        }

        public List<string> validationErrors = new List<string>();

        static readonly string[] ValidatedProperties =
        {
                "TestSuiteName",
                "TestSuiteDescription"
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
                case "TestSuiteName":
                    error = this.ValidateTestSuiteName();
                    break;

                case "TestSuiteDescription":
                    error = this.ValidateTestSuiteDescription();
                    break;

                default:
                    error = null;
                    throw new Exception("Unexpected property being validated on Service");
            }

            return error;
        }


        string ValidateTestSuiteName()
        {
            if (IsStringMissing(this.TestSuiteName))
            {
                return "TestSuiteName";
            }
            return null;
        }

        string ValidateTestSuiteDescription()
        {
            if (IsStringMissing(this.TestSuiteDescription))
            {
                return "TestSuiteDescription";
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


        private void  ResetApplicationSelection()
        {
            foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
            {
                regApps.IsSelected = false;
               
            }
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

        public void GetApplication(string testSuiteName)
        {
            B_REGISTERED_APPS objBRegApps = new B_REGISTERED_APPS();
            List<B_REGISTERED_APPS> applications = objBRegApps.GetTestSuiteApplication(MarsMainWindow.CurrentDatabaseIdx, testSuiteName);
            foreach (B_REGISTERED_APPS regApps in applications)
            {
                RegisterdApplication.Add(regApps);
            }
        }

        public bool SaveTestSuite()
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
                MessageBox.Show(sbError.ToString(), "Test Suite Add", MessageBoxButton.OK, MessageBoxImage.Error);
                validationErrors.Clear();
                return false;
            }

            if (!ValidateRegisteredApplication())
            {
                MessageBox.Show("Please select application for the Test Suite.", "Test Suite Add", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            T_TEST_SUITEDTO testSuiteDto = new T_TEST_SUITEDTO();
            B_TEST_SUITE bTestSuite = new B_TEST_SUITE();
            B_REL_APP_TESTSUITE bRelAppTestSuite = new B_REL_APP_TESTSUITE();
            WindowsIdentity ident = WindowsIdentity.GetCurrent();

            if (!bTestSuite.TestSuiteExists(MarsMainWindow.CurrentDatabaseIdx, testSuiteName) && isOpen == false)
            {
                testSuiteDto.TEST_SUITE_ID = bTestSuite.getTestSuiteId(MarsMainWindow.CurrentDatabaseIdx);
                testSuiteDto.TEST_SUITE_NAME = testSuiteName;
                testSuiteDto.TEST_SUITE_DESCRIPTION = testSuiteDescription;
                marsEntities.T_TEST_SUITE.Add(T_TEST_SUITEAssembler.ToEntity(testSuiteDto));
                foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
                {
                    if (regApps.IsSelected)
                    {
                        REL_APP_TESTSUITE relAppTestSuite = new REL_APP_TESTSUITE();
                        relAppTestSuite.RELATIONSHIP_ID = bRelAppTestSuite.getRelTestSuiteAppId(MarsMainWindow.CurrentDatabaseIdx);
                        relAppTestSuite.TEST_SUITE_ID = testSuiteDto.TEST_SUITE_ID;
                        relAppTestSuite.APPLICATION_ID = regApps.APPLICATION_ID; //This should be changed in schema to application id
                        marsEntities.REL_APP_TESTSUITE.Add(relAppTestSuite);
                    }
                }

                if (marsEntities.SaveChanges() > 0)
                {
                    MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                    MessageBox.Show("Test suite saved successfully", "Test Suite Add", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearTestSuite();
                    return true;
                }
                else
                {
                    //Give error message pop-up and rollback project add
                    //dbContextTransaction.Rollback();
                    return false;
                }
                
            }
            else if (isOpen == true)
            {
                try
                {
                    var testSuite = marsEntities.T_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_NAME == testSuiteName);
                    testSuite.TEST_SUITE_NAME = TestSuiteName;
                    testSuite.TEST_SUITE_DESCRIPTION = TestSuiteDescription;

                    foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
                    {
                        if (regApps.IsSelected && (marsEntities.REL_APP_TESTSUITE.FirstOrDefault(x => x.TEST_SUITE_ID == testSuite.TEST_SUITE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID) == null))
                        {
                            REL_APP_TESTSUITE relAppTestSuite = new REL_APP_TESTSUITE();
                            relAppTestSuite.RELATIONSHIP_ID = bRelAppTestSuite.getRelTestSuiteAppId(MarsMainWindow.CurrentDatabaseIdx);
                            relAppTestSuite.APPLICATION_ID = regApps.APPLICATION_ID;
                            relAppTestSuite.TEST_SUITE_ID = testSuite.TEST_SUITE_ID;
                            marsEntities.REL_APP_TESTSUITE.Add(relAppTestSuite);
                        }
                        else if (!regApps.IsSelected && (marsEntities.REL_APP_TESTSUITE.FirstOrDefault(x => x.TEST_SUITE_ID == testSuite.TEST_SUITE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID) != null))
                        {
                            var RelAppTestSuite = marsEntities.REL_APP_TESTSUITE.FirstOrDefault(x => x.TEST_SUITE_ID == testSuite.TEST_SUITE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID);
                            if (RelAppTestSuite != null)
                                marsEntities.REL_APP_TESTSUITE.Remove(RelAppTestSuite);
                        }
                    }
                    try
                    {
                        if (marsEntities.SaveChanges() > 0)
                        {
                            MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                            MessageBox.Show("Test Suite amedned successfully", "Test suite Open", MessageBoxButton.OK, MessageBoxImage.Information);
                            ClearTestSuite();
                            return true;
                        }
                        else
                        {
                            marsEntities = null;
                            MessageBox.Show("Error saving test suite", "Test suite Open", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        marsEntities = null;
                        MessageBox.Show(ex.InnerException.ToString(), "Test suite Open", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
                catch(Exception ex)
                {
                    marsEntities = null;
                    MessageBox.Show(ex.InnerException.ToString(), "Test suite Open", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            MessageBox.Show("Test Suite Already Exists");
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
        public ICommand EditCommand
        {
            get { return _EditCommand; }
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

        
    }
}
