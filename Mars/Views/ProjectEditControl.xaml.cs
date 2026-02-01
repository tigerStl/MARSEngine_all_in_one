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
using System.Collections.ObjectModel;
using Mars.Views.baseView;


namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for ProjectEditControl.xaml
    /// </summary>
    public partial class ProjectEditControl :
        MarsProjectBaseControlView
    {
        
        public ProjectEditControl()
        {
            InitializeComponent();
            Title = "Project Editor";        
        }        

        public ProjectEditControl(long lProjectId, string projectName, MarsProjectTreeView objTargetProjTreeView =null)
        {
            InitializeComponent();
            _projectName = projectName;
            projectId = lProjectId;
            this.DataContext = new ProjectEditViewModel(lProjectId,projectName, objTargetProjTreeView);
            Title = string.Format("Project [{0}] Editor",string.IsNullOrEmpty(projectName)?"N/A":(projectName.Length>10?(projectName.Substring(0,7)+"..."):projectName));
        }

        
    }
}
