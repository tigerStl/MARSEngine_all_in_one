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
    /// Interaction logic for TestApplicationRegistration.xaml
    /// </summary>
    public partial class TestApplicationRegistration :
        MarsBaseViewControl
    {
        public TestApplicationRegistration()
        {
            InitializeComponent();

            this.DataContext = new TestApplicationRegistrationViewModel();

            Title = "Application Register";

        }

        private void InfragisticsSupport(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
