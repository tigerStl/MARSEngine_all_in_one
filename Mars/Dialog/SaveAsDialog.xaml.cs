using Mars.ViewModel;
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
    public partial class SaveAsDialog : Window
    {
        internal string EmptyHint = "";
        public SaveAsDialog(string question, string defaultAnswer = "",string strTitle="", string strQuestionWhenEmpty="")
        {
            InitializeComponent();
            lblQuestion.Content = question;
            txtAnswer.Text = defaultAnswer;

            if (!string.IsNullOrEmpty(strTitle))
                this.Title = strTitle;
            EmptyHint = strQuestionWhenEmpty;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtAnswer.Text.Trim()))
            {
                if (ViewModelBase.QuestionByMessageBox(string.IsNullOrEmpty(EmptyHint)?"NO Data input, do you want to Cancel SaveAs?":EmptyHint,"Hint"))
                {
                    this.DialogResult = false;
                    return;
                }
                return;
            }
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtAnswer.SelectAll();
            txtAnswer.Focus();
        }

        public string Answer
        {
            get { return txtAnswer.Text; }
        }
    }
}
