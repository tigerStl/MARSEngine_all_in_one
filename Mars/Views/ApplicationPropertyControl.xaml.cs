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
    /// Interaction logic for ApplicationPropertyControl.xaml
    /// </summary>
    public partial class ApplicationPropertyControl :
        MarsBaseViewControl
    {
        public ApplicationPropertyControl()
        {
            InitializeComponent();
            this.DataContext = new ApplicationViewModel();
            Title = "Application Property";
        }
    }
}
