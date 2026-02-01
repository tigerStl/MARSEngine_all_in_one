using Mars.Helpers;
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

namespace Mars.Views.storyboard
{
    /// <summary>
    /// Interaction logic for StoryboardResultDetailControl.xaml
    /// </summary>
    public partial class StoryboardResultDetailControl : MarsBaseViewControl
    {
        public StoryboardResultDetailControl()
        {
            InitializeComponent();
        }
        
        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            DataGridRow objPrntRow = (DataGridRow)TreeViewHelper.FindParent<DataGridRow>(this);
            if (objPrntRow == null) return;
            objPrntRow.DetailsVisibility = Visibility.Collapsed;
            
        }
        

        private void SaveResultHisRecordClick(object sender, RoutedEventArgs e)
        {
            if (!(this.DataContext is MarsReportDetail)) return;

            ((MarsReportDetail)this.DataContext).SaveResultCommand.Execute(null);

        }
    }
}
