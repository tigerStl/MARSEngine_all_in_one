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

namespace Mars.Dialog
{
    /// <summary>
    /// Interaction logic for SaveAsDialog.xaml
    /// </summary>
    public partial class SaveAsWithDescriptionDialog : Window
    {
        public SaveAsWithDescriptionDialog(string question1, string question2, string defaultAnswer1 = "", string defaultAnswer2 = "", string strTitle = "")
        {
            InitializeComponent();
            lblQuestion1.Content = question1;
            txtAnswer1.Text = defaultAnswer1;

            lblQuestion2.Content = question2;
            txtAnswer2.Text = defaultAnswer2;

            if (!string.IsNullOrEmpty(strTitle))
            {
                this.Title = strTitle;
            }
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtAnswer1.SelectAll();
            txtAnswer1.Focus();
        }

        public string Answer1
        {
            get { return txtAnswer1.Text; }
        }

        public string Answer2
        {
            get { return txtAnswer2.Text; }
        }
    }
}
