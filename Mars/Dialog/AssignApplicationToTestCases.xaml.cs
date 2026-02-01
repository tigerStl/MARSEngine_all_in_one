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
    /// Interaction logic for AssignApplicationToTestCases.xaml
    /// </summary>
    public partial class AssignApplicationToTestCases : Window
    {
        public AssignApplicationToTestCases()
        {
            InitializeComponent();
            DataContext = new AssignApplicationToTestCaseModel();
        }
        public AssignApplicationToTestCases(long projId,string strProjName)
        {
            InitializeComponent();
            DataContext = new AssignApplicationToTestCaseModel(projId, strProjName, this);
        }
    }
}
