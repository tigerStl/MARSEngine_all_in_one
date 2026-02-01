using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Dto;
using Mars.Model;
using System.Windows.Input;
using System.Security.Principal;
using System.Windows;
using Mars.Business;
using System.Collections.ObjectModel;

namespace Mars.ViewModel
{
    public class TestSuiteEditViewModel : ViewModelBase, ITestSuiteViewModel
    {
        long testSuiteId;
        string testSuiteName;
        string testSuiteDescription;
        //bool isUseId = false;
        private ICommand _saveCommand;
        private ICommand _clearCommand;
        public ICommand FilterChangeImpl
        {
            get
            {
                return new DelegateCommand(()=> {
                    if (allTestCases == null) return;
                    if (FilterName == null) return;
                    TestCase = new ObservableCollection<B_TEST_CASE>(allTestCases.Where(p => (p.TEST_CASE_NAME ?? "").ToUpper().Contains((FilterName ?? "").ToUpper())).ToList());
                });
            }
        }
        MarsEntities marsEntities;
        private ObservableCollection<B_TEST_CASE> curTestCases = null;
        private ObservableCollection<B_TEST_CASE> allTestCases = null;
        public ObservableCollection<B_TEST_CASE> TestCase { get { return curTestCases; }
            set {
                curTestCases = value;
                OnPropertyChanged("TestCase");
            }
        }
        List<B_TEST_CASE> mappedTestCase = new List<B_TEST_CASE>();


        private string filterName;
        public string FilterName
        {
            get
            {
                return filterName;
            }
            set
            {
                
                if (string.Compare(filterName, value)!=0)
                {
                    filterName = value;
                    RaisePropertyChanged("FilterName");

                    if (allTestCases == null) return;
                    if (filterName == null) return;
                    TestCase = new ObservableCollection<B_TEST_CASE>(allTestCases.Where(p => (p.TEST_CASE_NAME ?? "").ToUpper().Contains((FilterName ?? "").ToUpper()))
                        .OrderBy(p=>p.TEST_CASE_NAME)
                        .ToList());
                }
                
            }
        }

        
        

        public TestSuiteEditViewModel(string _testSuiteName)
        {
            TestSuiteName = _testSuiteName;            
            _saveCommand = new DelegateCommand(() => { SaveTestSuite(); });
            _clearCommand = new DelegateCommand(() => { ClearTestSuite(); });
            
            TestCase = new ObservableCollection<B_TEST_CASE>();
            GetTestCase(_testSuiteName);            
        }

        private MarsTestSuiteTreeView TestCasesToBeAmend = null;

        public TestSuiteEditViewModel(long lTestSuiteId, string _testSuiteName, MarsTestSuiteTreeView currentTestSuite = null)
        {
            //isUseId = true;
            testSuiteId = lTestSuiteId;
            TestSuiteName = _testSuiteName;  
            _saveCommand = new DelegateCommand(() => { SaveTestSuite(); });
            _clearCommand = new DelegateCommand(() => { ClearTestSuite(); });
            
            TestCase = new ObservableCollection<B_TEST_CASE>();
            GetTestCase(testSuiteId);
            applicationInfo = GetApplicationInfoForDisplay(testSuiteId);

            TestCasesToBeAmend = currentTestSuite;
        }


        private string applicationInfo = null;
        public string ApplicationInfo
        {
            get
            {
                return applicationInfo;
            }
            set
            {
                applicationInfo = value;
                RaisePropertyChanged("ApplicationInfo");
            }
        }

        private string GetApplicationInfoForDisplay(long tsId)
        {
            List<T_REGISTERED_APPSDTO> appInfo = new List<T_REGISTERED_APPSDTO>();
            appInfo = B_REGISTERED_APPS.GetAppInfoByTestSuiteId(MarsMainWindow.CurrentDatabaseIdx, tsId);
            if (appInfo == null) return "NO Appliation is assigend to this test suite";
            var sAppName = appInfo.Select(p => p.APP_SHORT_NAME).ToList();
                       
            return string.Join("\r\n", sAppName);
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
        
        public void GetTestCase(string testSuiteName)
        {
            MarsEntities marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            B_TEST_CASE objBTestCase = new B_TEST_CASE();
            List<B_TEST_CASE> testCase = objBTestCase.GetTestCase(
                MarsMainWindow.CurrentDatabaseIdx,
                marsEntities.T_TEST_SUITE.FirstOrDefault(x => x.TEST_SUITE_NAME == testSuiteName).TEST_SUITE_ID);
            allTestCases = null;
            if (testCase != null && testCase.Count > 0)
            {
                //mappedTestCase = objBTestCase.GetMappedTestCase(testSuiteName);
                foreach (B_TEST_CASE bTestCase in testCase)
                {
                    ////set selected if match found
                    //if (mappedTestCase.FirstOrDefault(x => x.TEST_CASE_ID == bTestCase.TEST_CASE_ID) != null)
                    //{
                    //    bTestCase.IsSelected = true;
                    //}
                    TestCase.Add(bTestCase);
                }

                allTestCases = new ObservableCollection<B_TEST_CASE>(testCase);
            }
        }

        public void GetTestCase(long lTestSuiteId)
        {
            B_TEST_CASE objBTestCase = new B_TEST_CASE();
            List<B_TEST_CASE> testCase = objBTestCase.GetTestCase(MarsMainWindow.CurrentDatabaseIdx, lTestSuiteId);
            allTestCases = null;
            if (testCase != null && testCase.Count > 0)
            {
                //mappedTestCase = objBTestCase.GetMappedTestCase(testSuiteName);
                foreach (B_TEST_CASE bTestCase in testCase)
                {
                    ////set selected if match found
                    //if (mappedTestCase.FirstOrDefault(x => x.TEST_CASE_ID == bTestCase.TEST_CASE_ID) != null)
                    //{
                    //    bTestCase.IsSelected = true;
                    //}
                    TestCase.Add(bTestCase);
                }
                allTestCases = new ObservableCollection<B_TEST_CASE>(testCase);
            }
        }

        public bool SaveTestSuite()
        {
            marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            B_TEST_SUITE bTestSuite = new B_TEST_SUITE();
            //long TestSuiteId = bTestSuite.getTestSuiteId(TestSuiteName);
            B_REL_TEST_CASE_TEST_SUITE boRelTcTS = new B_REL_TEST_CASE_TEST_SUITE();
            string strError = null;
            if (!boRelTcTS.UpdateTSTCRelations(MarsMainWindow.CurrentDatabaseIdx, this.testSuiteId,TestCase,ref strError))
            {
                HintByMessageBox(string.Format("Saving Test suite [{1}] Failed with Error:\r\n[{0}]",strError,TestSuiteName),"Hint");
                return false;
            }
            HintByMessageBox(string.Format("Saving Test suite [{0}] successfully",TestSuiteName),"Hint");
            bool isOk = false;
            if (TestCasesToBeAmend!=null)
            {
                MarsTreeView.BuildTestCaseTree(TestCasesToBeAmend, B_V_PROJ_TS_TC_FULLVISION.GetAllTestProjInfo(MarsMainWindow.CurrentDatabaseIdx, ref strError, ref isOk));
            }
            return true;
            #region old codes section, Should remove codes to database layer
            //foreach (B_TEST_CASE testCase in TestCase)
            //{
            //    if (testCase.IsSelected && (marsEntities.REL_TEST_CASE_TEST_SUITE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.TEST_SUITE_ID == TestSuiteId) == null))
            //    {
            //        B_REL_TEST_CASE_TEST_SUITE bRelTestCaseTestSuite = new B_REL_TEST_CASE_TEST_SUITE();
            //        bRelTestCaseTestSuite.TEST_SUITE_ID = TestSuiteId;
            //        bRelTestCaseTestSuite.TEST_CASE_ID = testCase.TEST_CASE_ID;
            //        bRelTestCaseTestSuite.RELATIONSHIP_ID = bRelTestCaseTestSuite.getRelTestCasteTestSuite();
            //        marsEntities.REL_TEST_CASE_TEST_SUITE.Add(REL_TEST_CASE_TEST_SUITEAssembler.ToEntity(bRelTestCaseTestSuite));
            //    }
            //    else if (!testCase.IsSelected && (marsEntities.REL_TEST_CASE_TEST_SUITE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.TEST_SUITE_ID == TestSuiteId) != null))
            //    {
            //        //Not selected now but was selected earlier then remove
            //        var bRelTestSuiteProject = marsEntities.REL_TEST_CASE_TEST_SUITE.FirstOrDefault(x => x.TEST_CASE_ID == testCase.TEST_CASE_ID && x.TEST_SUITE_ID == TestSuiteId);
            //        if (bRelTestSuiteProject != null)
            //            marsEntities.REL_TEST_CASE_TEST_SUITE.Remove(bRelTestSuiteProject);
            //    }
            //}
            //try
            //{
            //    if (marsEntities.SaveChanges() > 0)
            //    {
            //        MarsTreeView.GetMarsTree();
            //        MessageBox.Show("Test suite, test case relation saved successfully", "Test Suite Amend", MessageBoxButton.OK, MessageBoxImage.Information);
            //        ClearTestSuite();
            //        return true;
            //    }
            //    else
            //    {
            //        marsEntities = null;
            //        MessageBox.Show("Error saving test suite test case relation", "Test Suite Amend", MessageBoxButton.OK, MessageBoxImage.Warning);
            //        return false;
            //    }
            //}
            //catch(Exception ex)
            //{
            //    marsEntities = null;
            //    MessageBox.Show(ex.InnerException.ToString(), "Test Suite Amend", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return false;
            //}
            #endregion //old codes section
        }

        public bool DeleteTestSuite(string testSuiteName, long lTestSuiteId)
        {
            
           //MarsEntities marsEntities =Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            B_TEST_SUITE bo = new B_TEST_SUITE();
            string strError = "";
            if (bo.DeleteTestSuiteById(MarsMainWindow.CurrentDatabaseIdx, lTestSuiteId, ref strError))
            {
                MessageBox.Show("Test suite deleted successfully", "Test Suite Delete", MessageBoxButton.OK, MessageBoxImage.Information);
                //refresh true
                return true;
            }
            if (!string.IsNullOrEmpty(strError))
                HintByMessageBox(strError,"Error");
            else
            {
                HintByMessageBox("Deleting fails.","Warning");
            }
            return false;
            #region moved codes to database layer
            //var testSuite = (from t in marsEntities.T_TEST_SUITE
            //               where t.TEST_SUITE_NAME == testSuiteName
            //               select t).FirstOrDefault();

            //var relAppTestSuite = (from a in marsEntities.REL_APP_TESTSUITE
            //                       where a.TEST_SUITE_ID == testSuite.TEST_SUITE_ID
            //                     select a);
            //foreach (var a in relAppTestSuite)
            //{
            //    marsEntities.REL_APP_TESTSUITE.Remove(a);
            //}

            //var relProjTestSuite = (from r in marsEntities.REL_TEST_SUIT_PROJECT
            //                            where r.TEST_SUITE_ID == testSuite.TEST_SUITE_ID
            //                            select r);
            //foreach (var r in relProjTestSuite)
            //{
            //    marsEntities.REL_TEST_SUIT_PROJECT.Remove(r);
            //}

            //var relTestCaseTestSuite = (from r in marsEntities.REL_TEST_CASE_TEST_SUITE
            //                            where r.TEST_SUITE_ID == testSuite.TEST_SUITE_ID
            //                            select r);
            //foreach (var r in relTestCaseTestSuite)
            //{
            //    marsEntities.REL_TEST_CASE_TEST_SUITE.Remove(r);
            //}

            //marsEntities.T_TEST_SUITE.Remove(testSuite);
            #endregion //moved codes to database layer
            #region un-used
            //try
            //{
            //    if (marsEntities.SaveChanges() > 0)
            //    {
            //        MarsTreeView.GetMarsTree();
            //        MessageBox.Show("Test suite deleted successfully", "Test Suite Delete", MessageBoxButton.OK, MessageBoxImage.Information);
            //        ClearTestSuite();
            //        return true;
            //    }
            //    else
            //    {
            //        marsEntities = null;
            //        MessageBox.Show("Error deleting test suite", "Test Suite Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            //        return false;
            //    }

            //}
            //catch (Exception ex)
            //{
            //    marsEntities = null;
            //    MessageBox.Show(ex.InnerException.ToString(), "Test Suite Delete", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return false;
            //}
            #endregion //un-used
        }

        public void ClearTestSuite()
        {

        }
    }
}
