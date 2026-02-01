using Mars.Dialog;
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
    /// Interaction logic for BaselineDataEditorControl1.xaml
    /// </summary>
    public partial class BaselineDataEditorControl1 :
        MarsBaseViewControl
    {
        public BaselineDataEditorControl1()
        {
            object o = MarsViewModelFactory.GetViewModeDataContextByType(MarsViewModelFactory.CNST_VIEW_NAME_BASELINE); 
            //MarsViewModelFactory.GetViewModeDataContextByType(MarsViewModelFactory.CNST_VIEW_NAME_BASELINE);
            InitializeComponent();
            //DockPanel.SetDock(this._hostGrid, Dock.Top);
            this.DataContext = o;

            Title = "BaseLine Data Editor";
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
           
        }
    }
}
