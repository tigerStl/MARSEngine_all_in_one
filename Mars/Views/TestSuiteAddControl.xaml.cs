using Mars.ViewModel;
using Mars.Views.baseView;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for TestSuiteAddControl.xaml
    /// </summary>
    public partial class TestSuiteAddControl :
        MarsBaseViewControl
    {
        public TestSuiteAddControl()
        {
            //Testing resync
            InitializeComponent();
            this.DataContext = new TestSuiteViewModel();
            //
            Title = "TS Add";
        }


        /// <summary>
        /// No test suite name should be applied, only test suite ID should be only used
        /// </summary>
        /// <param name="testSuiteName"></param>
        /// <param name="action"></param>
        /// <param name="lTestSuiteId"></param>
        public TestSuiteAddControl(string testSuiteName, string action, long lTestSuiteId)
        {
            //Testing resync
            InitializeComponent();
            this.DataContext = new TestSuiteViewModel(testSuiteName, 
                //action.Equals("Test Suite Properties"), 
                true,
                lTestSuiteId);
            if (action.Equals("Open Test Suite"))
            {
                //this.txtTestSuiteName.IsEnabled = false;
                //this.txtTestSuiteName.IsReadOnly = true;
                //this.btnCancel.IsEnabled = false;
                this.lblHeader.Content = "Test Suite Open";
            }
            else if(action.Equals("Test Suite Properties"))
            {
                //this.txtTestSuiteName.IsEnabled = false;
                //this.listViewApplication.IsEnabled = false;
                //this.btnCancel.IsEnabled = false;
                this.lblHeader.Content = "Test Suite Properties";
            }
#if tiger_dock
            Title = string.Format("TS Mend:[{0}]", testSuiteName) ;
#endif
        }
    }
}
