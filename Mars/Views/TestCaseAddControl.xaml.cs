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
    /// Interaction logic for TestCaseAddControl.xaml
    /// </summary>
    /// 
    public partial class TestCaseAddControl : MarsBaseViewControl
    {
        public TestCaseAddControl()
        {
            InitializeComponent();
            Title = "Add Test Case";
            this.DataContext = new TestCaseViewModel();
        }

        public TestCaseAddControl(string testCaseName, string action)
        {
            InitializeComponent();
            this.DataContext = new TestCaseViewModel(testCaseName);
            if (action.Equals("Open Test Case"))
            {
                this.txtTestCaseName.IsReadOnly = true;
                this.btnCancel.IsEnabled = false;
                this.lblHeader.Content = "Test Case Open";
            }
            else if (action.Equals("Test Case Properties"))
            {
                this.txtTestCaseName.IsEnabled = false;
                this.listViewClients.IsEnabled = false;
                this.btnCancel.IsEnabled = false;
                this.lblHeader.Content = "Test Case Properties";
            }
        }
    }
}
