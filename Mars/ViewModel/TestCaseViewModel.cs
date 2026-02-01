using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Mars.Dto;
using System.Security.Principal;
using Mars.Business;
using Mars.Model;

using Route2NSEx.src.Marquis.systemUtil;
using Mars.DataLayer;
using System.Transactions;

namespace Mars.ViewModel
{
    public class TestCaseViewModel : ViewModelBase, ITestCaseViewModel
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestCaseViewModel));
        long testCaseId;
        string testCaseName;
        string testCaseDescription;
        public ObservableCollection<ITestCaseViewModel> TestCaseView { get; set; }
        ObservableCollection<B_REGISTERED_APPS> _registerdApplication = new ObservableCollection<B_REGISTERED_APPS>();
        B_TEST_CASE _selectedTestCase;
        private ICommand _saveCommand;
        private ICommand _clearCommand;
        //private ICommand _closeCommand;
        private ICommand _editCommand;

        private string searchText;
        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                RaisePropertyChanged("SearchText");
            }
        }
        public ICommand LocateSpecialTestStepClickCmd
        {
            get
            {
                return new DelegateCommand(() => { });
            }
        }

        public ICommand NextSpecialTestStepClickCmd
        {
            get { return new DelegateCommand(() => { });
            }
        }

        public ICommand PreviousTestStepClickCmd
        {
            get
            {
                return new DelegateCommand(() => { });
            }
        }
    

        MarsEntities marsEntities;
        bool isOpen;

        private string _editModeButtonCaption = "Edit";
        public string editModeButtonCaption
        {
            get { return _editModeButtonCaption; }
            set
            {
                _editModeButtonCaption = value;
                RaisePropertyChanged("editModeButtonCaption");
            }
        }

        public TestCaseViewModel()
        {
            isOpen = false;
            CommonLoad();
            GetApplication();

        }

        private void CommonLoad()
        {
            marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            _saveCommand = new DelegateCommand(() => { SaveTestCase(); });
            _clearCommand = new DelegateCommand(() => { ClearTestCase(); });
            _editCommand = new DelegateCommand(() => { EditModeChange(); });
            RegisterdApplication = new ObservableCollection<B_REGISTERED_APPS>();
        }

        public TestCaseViewModel(string _testCaseName)
        {
            isOpen = true;
            B_TEST_CASE testCase = new B_TEST_CASE();
            SelectedTestCase = testCase.GetTestCase(MarsMainWindow.CurrentDatabaseIdx, _testCaseName);
            CommonLoad();
            var selTestCase = marsEntities.T_TEST_CASE_SUMMARY.FirstOrDefault(x => x.TEST_CASE_NAME == _testCaseName);
            TestCaseName = _testCaseName;
            TestCaseId = selTestCase.TEST_CASE_ID;
            TestCaseDescription = selTestCase.TEST_STEP_DESCRIPTION;
            GetApplication(SelectedTestCase);
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

        public B_TEST_CASE SelectedTestCase
        {
            get
            {
                return _selectedTestCase;
            }
            set
            {
                _selectedTestCase = value;
                RaisePropertyChanged("SelectedApplication");
            }
        }

        public long TestCaseId
        {
            get
            {
                return testCaseId;
            }
            set
            {
                testCaseId = value;
                RaisePropertyChanged("TestCaseId");
            }
        }

        public string TestCaseName
        {
            get
            {
                return testCaseName;
            }
            set
            {
                testCaseName = value;
                RaisePropertyChanged("TestCaseName");
            }
        }

        public string TestCaseDescription
        {
            get
            {
                return testCaseDescription;
            }
            set
            {
                testCaseDescription = value;
                RaisePropertyChanged("TestCaseDescription");
            }
        }

        public List<string> validationErrors = new List<string>();

        static readonly string[] ValidatedProperties =
        {
                "TestCaseName",
                "TestCaseDescription"
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
                case "TestCaseName":
                    error = this.ValidateTestCaseName();
                    break;

                case "TestCaseDescription":
                    error = this.ValidateTestCaseDescription();
                    break;

                default:
                    error = null;
                    throw new Exception("Unexpected property being validated on Service");
            }

            return error;
        }


        string ValidateTestCaseName()
        {
            if (IsStringMissing(this.TestCaseName))
            {
                return "TestSuiteName";
            }
            return null;
        }

        string ValidateTestCaseDescription()
        {
            if (IsStringMissing(this.TestCaseDescription))
            {
                return "TestCaseDescription";
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


        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }

            set
            { }

        }

        public ICommand EditCommand
        {
            get
            {
                return _editCommand;
            }

            set
            { }

        }

        public ICommand ClearCommand
        {
            get
            {
                return _clearCommand;
            }

            set
            { }

        }

        //public ICommand CloseCommand
        //{
        //    get
        //    {
        //        return _closeCommand;
        //    }

        //    set
        //    { }

        //}


        public bool SaveTestCase()
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
                MessageBox.Show(sbError.ToString(), "Test Case Add", MessageBoxButton.OK, MessageBoxImage.Error);
                validationErrors.Clear();
                return false;
            }

            if (!ValidateRegisteredApplication())
            {
                MessageBox.Show("Please select application for the Test Case.", "Test Case Add", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            B_TEST_CASE bTestCase = new B_TEST_CASE();
            B_REL_APP_TESTCASE bRelAppTestCase = new B_REL_APP_TESTCASE();
            WindowsIdentity ident = WindowsIdentity.GetCurrent();

            MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);
            string strHint = "",strHint1="",strError="";
            bool isOk = false;
            int iCnt = -1;
            try
            {
                using (var scope = new TransactionScope())
                {
                    if (!bTestCase.TestCaseExists(MarsMainWindow.CurrentDatabaseIdx, testCaseName, objTrans.CurrentDBContext) && isOpen == false)
                    {
                        strHint = "Test Case Add";
                        strHint1 = "Test case saved successfully";

                        bTestCase.TEST_CASE_ID = bTestCase.getTestCaseId(MarsMainWindow.CurrentDatabaseIdx, objTrans.CurrentDBContext);
                        bTestCase.TEST_CASE_NAME = testCaseName;
                        bTestCase.TEST_STEP_DESCRIPTION = testCaseDescription;
                        bTestCase.TEST_STEP_CREATOR = ident.Name.ToString();
                        bTestCase.TEST_STEP_CREATE_TIME = DateTime.Now;

                        objTrans.CurrentDBContext.T_TEST_CASE_SUMMARY.Add(T_TEST_CASE_SUMMARYAssembler.ToEntity(bTestCase));
                        //marsEntities.T_TEST_CASE_SUMMARY.Add(T_TEST_CASE_SUMMARYAssembler.ToEntity(bTestCase));

                        // Add Shared Data Set
                        SharedDataSetViewModel.CreateDataSet(bTestCase.TEST_CASE_ID, testCaseName,objTrans.CurrentDBContext);

                        foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
                        {
                            if (regApps.IsSelected)
                            {
                                REL_APP_TESTCASE relAppTestCase = new REL_APP_TESTCASE();
                                relAppTestCase.RELATIONSHIP_ID = bRelAppTestCase.getRelTestCaseAppId(MarsMainWindow.CurrentDatabaseIdx, 
                                    objTrans.CurrentDBContext);
                                relAppTestCase.APPLICATION_ID = regApps.APPLICATION_ID;
                                relAppTestCase.TEST_CASE_ID = bTestCase.TEST_CASE_ID;

                                objTrans.CurrentDBContext.REL_APP_TESTCASE.Add(relAppTestCase);
                                //marsEntities.REL_APP_TESTCASE.Add(relAppTestCase);
                            }
                        }
                        iCnt = objTrans.CurrentDBContext.SaveChanges();
                        isOk = iCnt > 0;
                        scope.Complete();

                        #region codes can be optimized
                        //if (marsEntities.SaveChanges() > 0)
                        //{
                        //    MarsTreeView.GetMarsTree();
                        //    MessageBox.Show("Test case saved successfully", strHint, MessageBoxButton.OK, MessageBoxImage.Information);
                        //    ClearTestCase();

                        //    /// update cache
                        //    /// 
                        //    MarsDBGlobe_Cache.UpdateAppTestCaseCache();
                        //    return true;
                        //}
                        //else
                        //{
                        //    return false;
                        //}
                        #endregion
                    }
                    else if (isOpen == true)
                    {
                        strHint = "Test case Open";
                        strHint1 = "Test case amedned successfully";
                        try
                        {
                            //var testCase = marsEntities.T_TEST_CASE_SUMMARY.FirstOrDefault(x => x.TEST_CASE_NAME == testCaseName);
                            var testCase = objTrans.CurrentDBContext.T_TEST_CASE_SUMMARY.FirstOrDefault(x => x.TEST_CASE_NAME == testCaseName);
                            testCase.TEST_CASE_NAME = TestCaseName;
                            testCase.TEST_STEP_DESCRIPTION = TestCaseDescription;

                            foreach (B_REGISTERED_APPS regApps in RegisterdApplication)
                            {
                                if (regApps.IsSelected &&
                                    //(marsEntities.REL_APP_TESTCASE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID) == null)
                                    objTrans.CurrentDBContext.REL_APP_TESTCASE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID) == null)                                    
                                {
                                    REL_APP_TESTCASE relAppTestCase = new REL_APP_TESTCASE();
                                    relAppTestCase.RELATIONSHIP_ID = bRelAppTestCase.getRelTestCaseAppId(MarsMainWindow.CurrentDatabaseIdx, 
                                        objTrans.CurrentDBContext);
                                    relAppTestCase.APPLICATION_ID = regApps.APPLICATION_ID;
                                    relAppTestCase.TEST_CASE_ID = testCase.TEST_CASE_ID;

                                    objTrans.CurrentDBContext.REL_APP_TESTCASE.Add(relAppTestCase);
                                    //marsEntities.REL_APP_TESTCASE.Add(relAppTestCase);
                                }
                                else if (!regApps.IsSelected
                                    //&& (marsEntities.REL_APP_TESTCASE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID) != null))
                                    && (objTrans.CurrentDBContext.REL_APP_TESTCASE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID) != null))
                                {
                                    //var RelAppTestCase = marsEntities.REL_APP_TESTCASE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID);
                                    var RelAppTestCase = objTrans.CurrentDBContext.REL_APP_TESTCASE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.APPLICATION_ID == regApps.APPLICATION_ID);
                                    if (RelAppTestCase != null)
                                        //marsEntities.REL_APP_TESTCASE.Remove(RelAppTestCase);
                                        objTrans.CurrentDBContext.REL_APP_TESTCASE.Remove(RelAppTestCase);
                                }
                            }

                            iCnt = objTrans.CurrentDBContext.SaveChanges();
                            isOk = iCnt > 0;
                            scope.Complete();

                            #region codes can be optimized
                            //try
                            //{
                            //    if (marsEntities.SaveChanges() > 0)
                            //    {
                            //        MarsTreeView.GetMarsTree();
                            //        MessageBox.Show("Test case amedned successfully", "Test case Open", MessageBoxButton.OK, MessageBoxImage.Information);
                            //        ClearTestCase();
                            //        MarsDBGlobe_Cache.UpdateAppTestCaseCache();
                            //        return true;
                            //    }
                            //    else
                            //    {
                            //        marsEntities = null;
                            //        MessageBox.Show("Error saving test case", "Test case Open", MessageBoxButton.OK, MessageBoxImage.Warning);
                            //        return false;
                            //    }
                            //}
                            //catch (Exception ex)
                            //{
                            //    marsEntities = null;
                            //    MessageBox.Show(ex.InnerException.ToString(), "Test case Open", MessageBoxButton.OK, MessageBoxImage.Error);
                            //    return false;
                            //}
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            
                            Logger.Error("SaveTestCase", strError = string.Format("Exceptions:[{0}]", ex.Message), ex);
                            HintByMessageBox(strError,"Error");
                            return false; 
                        }
                    }
                    else
                    {
                        //MessageBox.Show("Test Case Already Exists");
                        HintByMessageBox("Test Case Already Exists", "Error");
                        return false;
                    }
                    
                }
                if (isOk)
                {
                    MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                    HintByMessageBox(strHint1, strHint);
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("SaveTestCase", strError=string.Format("Exception :[{0}] stackTrace:\r\n{1}",e.Message,e.StackTrace));
                HintByMessageBox(strError, "Error");
                return false;
            }
            finally
            {
                Logger.logEnd("SaveTestCase");
            }
        }

        public void ClearTestCase()
        {
            TestCaseId = 0;
            TestCaseName = "";
            TestCaseDescription = "";
        }

        private bool _isEditMode = false;
        public bool isEditMode
        {
            get { return _isEditMode; }
            set
            {
                _isEditMode = value;
                RaisePropertyChanged("isEditMode");
            }
        }
        public void EditModeChange()
        {
            isEditMode = !isEditMode;
            if (string.Compare("Edit", editModeButtonCaption, true) == 0)
            {
                editModeButtonCaption = "Read Only";
            }
            else
            {
                editModeButtonCaption = "Edit";
            }
        }

        public void CloseTestCase()
        {

        }

        public bool EditTestCase()
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            var selectedTestCaseEntity = marsEntities.T_TEST_CASE_SUMMARY.First(x => x.TEST_CASE_ID == SelectedTestCase.TEST_CASE_ID);
            selectedTestCaseEntity.TEST_STEP_DESCRIPTION = SelectedTestCase.TEST_STEP_DESCRIPTION;

            if (marsEntities.SaveChanges() > 0)
            {
                MarsDBGlobe_Cache.UpdateAppTestCaseCache();

                MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                MessageBox.Show("Test case edited successfully", "Test Case Add", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            else
            {
                return false;
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

        public void GetApplication(B_TEST_CASE testCase)
        {
            MarsEntities marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            B_REGISTERED_APPS objBRegApps = new B_REGISTERED_APPS();
            List<B_REGISTERED_APPS> applications = objBRegApps.GetApplication(MarsMainWindow.CurrentDatabaseIdx);
            foreach (B_REGISTERED_APPS regApps in applications)
            {
                if (marsEntities.REL_APP_TESTCASE.FirstOrDefault(x => x.APPLICATION_ID == regApps.APPLICATION_ID && x.TEST_CASE_ID == testCase.TEST_CASE_ID) != null ? true : false)
                {
                    regApps.IsSelected = true;
                }

                RegisterdApplication.Add(regApps);
            }
        }


    }
}
