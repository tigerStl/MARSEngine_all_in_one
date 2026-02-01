using Mars.ViewModel.user.management;
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

namespace Mars.Views.user.management
{
    /// <summary>
    /// Interaction logic for UserAndCompanyManagement.xaml
    /// </summary>
    public partial class UserAndCompanyManagement :
        MarsBaseViewControl
    {
        public UserAndCompanyManagement()
        {
            InitializeComponent();
            this.DataContext = new UserAndCompanyManagementModel();
            Title = "Account Management";
        }
    }
}
