using Mars.ViewModel.BaseData;
using Mars.ViewModel.login;
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
using System.Security;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Mars.Views.login
{
    

    /// <summary>
    /// Interaction logic for MarsLoginMain.xaml
    /// </summary>
    public partial class MarsLoginMain : Window, IUserNamePassword
    {
        private const int cnst_defaultHeight = 370;
        private const int cnst_OptionHeight = 600;
        public MarsLoginMain()
        {
            InitializeComponent();
            Height = cnst_defaultHeight;
            this.DataContext = new MarsLoginMainModel();
        }

        public SecureString GetPassword() 
        {
            return pwdInfo.SecurePassword;            
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (dockPanelOption.Visibility!= Visibility.Visible)
            {
                Height = cnst_OptionHeight;
                dockPanelOption.Visibility = Visibility.Visible;
                //ofr
            }
            else
            {
                Height = cnst_defaultHeight;
                dockPanelOption.Visibility = Visibility.Collapsed;
            }
            
        }

        private void HideDatabasePwdClick(object sender, RoutedEventArgs e)
        {
            //Height = cnst_defaultHeight;
            //dockPanelOption.Visibility = Visibility.Hidden;
        }

        private void LoginWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) {
                ButtonAutomationPeer autoButton = new ButtonAutomationPeer(this.loginButton);
                IInvokeProvider cmd = autoButton.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                cmd.Invoke();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Mars.Dialog.DBTestDiaglose.GetInstance();
        }
    }
}
