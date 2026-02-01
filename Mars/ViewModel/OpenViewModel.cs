
using Mars.Business;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel
{
    public class OpenViewModel : ViewModelBase
    {
        private static MLogger log = MLogger.GetLogger(typeof(OpenViewModel));
        private static ObservableCollection<OpenApplication> _application = new ObservableCollection<OpenApplication>();
        private static ObservableCollection<OpenTestSuite> _testSuite = new ObservableCollection<OpenTestSuite>();
        private static ObservableCollection<OpenTestCase> _testCase = new ObservableCollection<OpenTestCase>();
        private static string _type="";
        public OpenViewModel(string Type)
        {
            _type = Type;
            _application.Clear();
            _testSuite.Clear();
            _testCase.Clear();
            GetApplication();
        }

        public ObservableCollection<OpenApplication> Application
        {
            get
            {
                return _application;
            }
            set
            {
                _application = value;
                RaisePropertyChanged("Application");
            }
        }

        public ObservableCollection<OpenTestSuite> TestSuite
        {
            get
            {
                return _testSuite;
            }
            set
            {
                _testSuite = value;
                RaisePropertyChanged("TestSuite");
            }
        }

        public ObservableCollection<OpenTestCase> TestCase
        {
            get
            {
                return _testCase;
            }
            set
            {
                _testCase = value;
                RaisePropertyChanged("TestCase");
            }
        }

        public static ObservableCollection<OpenApplication> GetApplication()
        {
            _application.Clear();
            List<B_REGISTERED_APPS> registredApps = new List<B_REGISTERED_APPS>();
            B_REGISTERED_APPS bregApps = new B_REGISTERED_APPS();
            registredApps = bregApps.GetApplication(MarsMainWindow.CurrentDatabaseIdx);
            foreach (B_REGISTERED_APPS apps in registredApps)
            {
                OpenApplication oApplication = new OpenApplication();
                oApplication.ApplicationId = apps.APPLICATION_ID;
                oApplication.ApplicationName = apps.APP_SHORT_NAME;
                oApplication.ApplicationVersion = apps.VERSION;
                _application.Add(oApplication);
            }
            return _application;
        }

        public static ObservableCollection<OpenTestSuite> GetApplicationTestSuite()
        {
            if (_type != "TestSuite")
                return null;
            _testSuite.Clear();
            B_TEST_SUITE btestSuite = new B_TEST_SUITE();
            List<B_TEST_SUITE> lBTestSuite = new List<B_TEST_SUITE>();
            foreach (OpenApplication regApps in _application)
            {
                if (regApps.IsSelected)
                {
                    lBTestSuite = btestSuite.GetApplicationTestSuite(MarsMainWindow.CurrentDatabaseIdx, regApps.ApplicationId); //loop for all selected application
                    foreach (B_TEST_SUITE tSuite in lBTestSuite)
                    {
                        OpenTestSuite oTestSuite = new OpenTestSuite();
                        oTestSuite.TestSuiteName = ((Mars.Dto.T_TEST_SUITEDTO)(tSuite)).TEST_SUITE_NAME;
                        oTestSuite.ApplicationName = tSuite.APP_SHORT_NAME;
                        oTestSuite.ApplicationVersion = tSuite.VERSION;
                        oTestSuite.TestSuiteId = tSuite.TEST_SUITE_ID;
                        _testSuite.Add(oTestSuite);
                    }
                }

            }
            return _testSuite;
        }

        public static ObservableCollection<OpenTestSuite> GetApplicationTestCase()
        {
            if (_type != "TestCase")
                return null;
            _testCase.Clear();
            B_TEST_CASE btestCase = new B_TEST_CASE();
            List<B_TEST_CASE> lBTestCase = new List<B_TEST_CASE>();
            foreach (OpenApplication regApps in _application)
            {
                if (regApps.IsSelected)
                {
                    lBTestCase = btestCase.GetApplicationTestCase(MarsMainWindow.CurrentDatabaseIdx, regApps.ApplicationId); //loop for all selected application
                    foreach (B_TEST_CASE tCase in lBTestCase)
                    {
                        OpenTestCase oTestCase = new OpenTestCase();
                        oTestCase.TestCaseName = ((Mars.Dto.T_TEST_CASE_SUMMARYDTO)(tCase)).TEST_CASE_NAME;
                        oTestCase.ApplicationName = tCase.APP_SHORT_NAME;
                        oTestCase.ApplicationVersion = tCase.VERSION;
                        _testCase.Add(oTestCase);
                    }
                }

            }
            return _testSuite;
        }
    }

    public class OpenApplication : ViewModelBase
    {
        private bool _isSelected;
        private long _applicationId;
        private string _applicationName;
        private string _applicationVersion;
        private static ObservableCollection<OpenTestSuite> _testSuite = new ObservableCollection<OpenTestSuite>();
        ObservableCollection<OpenTestCase> _testCase = null;
        public OpenApplication()
        {}

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                RaisePropertyChanged("IsSelected");
                OpenViewModel.GetApplicationTestSuite();
                OpenViewModel.GetApplicationTestCase();
            }
        }

        public long ApplicationId
        {
            get
            {
                return _applicationId;
            }
            set
            {
                _applicationId = value;
                RaisePropertyChanged("ApplicationId");
            }
        }

        public string ApplicationName
        {
            get
            {
                return _applicationName;
            }
            set
            {
                _applicationName = value;
                RaisePropertyChanged("ApplicationName");
            }
        }

        public string ApplicationVersion
        {
            get
            {
                return _applicationVersion;
            }
            set
            {
                _applicationVersion = value;
                RaisePropertyChanged("ApplicationVersion");
            }
        }

        public ObservableCollection<OpenTestSuite> TestSuite
        {
            get
            {
                if (_testSuite == null) _testSuite = new ObservableCollection<OpenTestSuite>();
                return _testSuite;
            }
            set
            {
                _testSuite = value;
                RaisePropertyChanged("TestSuite");
            }
        }

        public ObservableCollection<OpenTestCase> TestCase
        {
            get
            {
                if (_testCase == null) _testCase = new ObservableCollection<OpenTestCase>();
                return _testCase;
            }
            set
            {
                _testCase = value;
                RaisePropertyChanged("TestCase");
            }
        }        
    }

    public class OpenTestSuite : ViewModelBase
    {
        private bool _isSelected;
        private string _testSuiteName;
        private string _applicationName;
        private string _applicationVersion;
        private long testsuiteId;

        public OpenTestSuite()
        {}


        public long TestSuiteId
        {
            get { return testsuiteId; }
            set
            {
                testsuiteId = value;
                RaisePropertyChanged("TestSuiteId");
            }
        }
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                RaisePropertyChanged("IsSelected");
            }
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

        public string ApplicationName
        {
            get
            {
                return _applicationName;
            }
            set
            {
                _applicationName = value;
                RaisePropertyChanged("ApplicationName");
            }
        }

        public string ApplicationVersion
        {
            get
            {
                return _applicationVersion;
            }
            set
            {
                _applicationVersion = value;
                RaisePropertyChanged("ApplicationVersion");
            }
        }
    }

    public class OpenTestCase : ViewModelBase
    {
        private bool _isSelected;
        string _testCaseName;
        private string _applicationName;
        private string _applicationVersion;

        public OpenTestCase()
        {}

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }

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
        public string ApplicationName
        {
            get
            {
                return _applicationName;
            }
            set
            {
                _applicationName = value;
                RaisePropertyChanged("ApplicationName");
            }
        }

        public string ApplicationVersion
        {
            get
            {
                return _applicationVersion;
            }
            set
            {
                _applicationVersion = value;
                RaisePropertyChanged("ApplicationVersion");
            }
        }
    }
}
