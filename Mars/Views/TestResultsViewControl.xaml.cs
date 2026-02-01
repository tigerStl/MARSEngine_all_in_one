using Mars.Views.baseView;
using Route2NSEx.src.Marquis.systemUtil;
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
    /// Interaction logic for TestResultsViewControl.xaml
    /// </summary>
    public partial class TestResultsViewControl : MarsBaseViewControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestResultsViewControl));

        private long? _currentStoryBoardDetailID=null;
        public long? currentStoryBoardDetailID
        {
            get { return _currentStoryBoardDetailID; }
            set {
                _currentStoryBoardDetailID = value;
                UpdateDataContext();
            }
        }

        private void UpdateDataContext()
        {
            Logger.logBegin("UpdateDataContext");


        }

        public TestResultsViewControl()
        {
            InitializeComponent();
            Title = "TC Result Comparason";
        }

  
        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine("UserControl_PreviewKeyDown");
        }
    }
}
