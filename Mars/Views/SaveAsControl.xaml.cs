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
using Microsoft.Practices.EnterpriseLibrary.Validation;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Mars.Views.baseView;

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for SaveAs.xaml
    /// </summary>
    public partial class SaveAsControl : MarsBaseViewControl
    {
        public SaveAsControl()
        {
            InitializeComponent();
        }

        public SaveAsControl(string caller, string strName, bool isOptionsRequired , List<string > lstOptions,long lObjectId=-1,string strOptionHint=""):this(caller, strName, lObjectId)
        {
            ((SaveAsViewModel)this.DataContext).IsExtendsOptionRequired = isOptionsRequired ? Visibility.Visible : Visibility.Collapsed;
            ((SaveAsViewModel)this.DataContext).AvailableOptions = lstOptions;
            ((SaveAsViewModel)this.DataContext).OptionHint = strOptionHint;
        }

        public SaveAsControl(string caller, string strName, long lObjectId=-1)
        {
            InitializeComponent();
            this.DataContext = new SaveAsViewModel(caller, strName, lObjectId);
            lblSaveAsHeader.Content = caller + " save as";
            lblSaveAs.Content = "Name of " + caller;
            this.contextName.Text = strName;

            Title = "Save As " + strName;
        }

        private void ValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                ((Control)sender).ToolTip = e.Error.ErrorContent.ToString();
            }
            else
            {
                ((Control)sender).ToolTip = "";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }
    }
}
