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

namespace Mars.Views.MarsControl
{
    /// <summary>
    /// Interaction logic for MarsPasswordForCellControl.xaml
    /// </summary>
    public partial class MarsPasswordForCellControl : MarsBaseViewControl
    {
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register("Password", typeof(string), typeof(MarsPasswordForCellControl));
        
        /// <summary>
        /// stored real password
        /// </summary>
        public string Password
        {
            get
            {
                
                return (string)GetValue(PasswordProperty);                
            }

            set
            {
                SetValue(PasswordProperty,value);
                EncodePasswordEdit.Password = value;
                RaisePropertyChanged("Password");
            }
        }

        public MarsPasswordForCellControl()
        {
            InitializeComponent();
            ///this.DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if ((e.LeftButton== MouseButtonState.Pressed))
            //{
            //    PasswordTextBox.Text = Password;                
            //}
            //else
            //{
            //    if (string.IsNullOrEmpty(Password))
            //        PasswordTextBox.Text = "";
            //    else
            //        PasswordTextBox.Text = new string('*', Password.Length);
            //}
            
            ((ToolTip)(HostButton.ToolTip)).IsOpen = true;

        }

        private void ViewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (((ToolTip)(HostButton.ToolTip)).IsOpen)
                ((ToolTip)(HostButton.ToolTip)).IsOpen = false;
        }

        private void ViewImage_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            if (((ToolTip)(HostButton.ToolTip)).IsOpen)
                ((ToolTip)(HostButton.ToolTip)).IsOpen = false;
        }

        private void EncodePasswordEdit_PasswordChanged(object sender, RoutedEventArgs e)
        {
            //Password = EncodePasswordEdit.Password;
            //EncodePasswordEdit.SetValue(
        }

        private void HostButton_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
           
        }
    }
}
