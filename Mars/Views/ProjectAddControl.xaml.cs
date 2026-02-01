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
using Microsoft.Practices.EnterpriseLibrary.Validation;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Mars.Views.baseView;

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for ProjectAddControl.xaml
    /// </summary>
    public partial class ProjectAddControl :
        MarsProjectBaseControlView
    {

        public ProjectAddControl()
        {
            InitializeComponent();
            //this.txtProjectName.Visibility = Visibility.Visible;
            //this.cbxProject.Visibility = Visibility.Hidden;
            this.DataContext = new ProjectViewModel();
            Title = "Project Add";
            //RaisePropertyChanged("Title");
        }
        #region old code
        //public ProjectAddControl(string projectName, string action,long lProjectId=-1)
        #endregion //old code
        public ProjectAddControl(string action, long lProjectId )
        {
            InitializeComponent();            
            if (action.Equals("Open Project"))
            {
                this.txtProjectName.IsEnabled = true;
                this.txtProjectName.IsReadOnly = false;
                this.btnCancel.IsEnabled = false;
                this.lblHeader.Content = "Project Open";
            }
            else if (action.Equals("Project Properties"))
            {
                this.txtProjectName.IsEnabled = false;
                //this.txtProjectName.IsReadOnly = true;
                //this.listViewClients.IsEnabled = false;
                //this.cbxProjectStatus.IsEnabled = false;
                this.btnCancel.IsEnabled = false;
                this.lblHeader.Content = "Project Properties";
            }
            this.DataContext = new ProjectViewModel(lProjectId);
            Title = ((ProjectViewModel)this.DataContext).Title;

            this.projectId = ((ProjectViewModel)this.DataContext).ProjectId;
            this.ProjectName = ((ProjectViewModel)this.DataContext).ProjectName;
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
    }
}
