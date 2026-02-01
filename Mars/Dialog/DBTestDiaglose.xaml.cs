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
using System.Windows.Shapes;

namespace Mars.Dialog
{
    /// <summary>
    /// Interaction logic for DBTestDiaglose.xaml
    /// </summary>
    public partial class DBTestDiaglose : Window
    {
        private static DBTestDiaglose TestWindowInstance = null;
        public DBTestDiaglose()
        {
            InitializeComponent();
        }

        internal static DBTestDiaglose GetInstance()
        {
            if (TestWindowInstance == null)
                TestWindowInstance = new DBTestDiaglose();
            TestWindowInstance.ShowActivated = true;
            TestWindowInstance.Show();
            TestWindowInstance.Activate();
            return TestWindowInstance;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            e.Cancel = true;
        }
    }
}
