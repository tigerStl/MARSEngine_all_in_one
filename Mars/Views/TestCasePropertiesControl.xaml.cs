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
using Mars.ViewModel;
using Mars.Views.baseView;

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for TestCasePropertiesControl.xaml
    /// </summary>
    public partial class TestCasePropertiesControl :
        MarsBaseViewControl
    {
        string _testCaseName;
        public TestCasePropertiesControl()
        {
            InitializeComponent();
        }

        public TestCasePropertiesControl(string testCaseName)
        {
            InitializeComponent();
            _testCaseName = testCaseName;
            this.DataContext = new TestCaseViewModel(testCaseName);

            Title = string.Format("TC Prop:[{0}]", _testCaseName);
        }
    }
}
