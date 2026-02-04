using MarsSpyTool.subToolWindows.viewModal;
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

namespace MarsSpyTool.subToolWindows.hintWindows
{
    /// <summary>
    /// Interaction logic for MarsHintWindowSelectApplication.xaml
    /// </summary>
    public partial class MarsHintWindowSelectApplication : Window
    {
        
        public MarsHintWindowSelectApplication(MarsHintWindowsModal dataCntx) : base()
        {
            InitializeComponent();
            DataContext = dataCntx;
        }

        public MarsHintWindowSelectApplication()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
