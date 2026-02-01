using Mars.ViewModel;
using Mars.Views.baseView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for TestSuiteEditControl.xaml
    /// </summary>
    public partial class TestSuiteEditControl :
        MarsBaseViewControl
    {
        string _testSuiteName;

        //bool isUseId = false;
        long _testSuiteId;

        public long TestSuiteId
        {
            get
            {
                return _testSuiteId;
            }
        }

        public TestSuiteEditControl()
        {
            InitializeComponent();
        }

//        public TestSuiteEditControl(string testSuiteName)
//        {
//            InitializeComponent();
//            _testSuiteName = testSuiteName;
//            this.DataContext = new TestSuiteEditViewModel(testSuiteName);
//#if tiger_dock
//            Title = string.Format("TS:{0}", testSuiteName);
//#endif
//        }


        public TestSuiteEditControl(long lTestSuiteId, string testSuiteName, MarsTestSuiteTreeView currentTestSuiteView =null)
        {
            InitializeComponent();
            //isUseId = true;
            _testSuiteId = lTestSuiteId;
            _testSuiteName = testSuiteName;
            this.DataContext = new TestSuiteEditViewModel(_testSuiteId, testSuiteName,currentTestSuiteView);

            Title = string.Format("TS Editor: [{0}]", testSuiteName);
        }

    }
}
