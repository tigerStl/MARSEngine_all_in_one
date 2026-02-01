using Mars.ViewModel.SystemTools;
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

namespace Mars.Views.systemTools
{
    /// <summary>
    /// Interaction logic for MultipleUserServiceSettingView.xaml
    /// </summary>
    public partial class MultipleUserServiceSettingView : MarsBaseViewControl
    {
        public MultipleUserServiceSettingView()
        {
            this.Title = "MARS Services Setting";
            InitializeComponent();

            this.DataContext = new MultipleUserServiceSettingModel();
        }
    }
}
