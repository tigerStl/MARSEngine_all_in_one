using Mars.Helpers;
using Mars.Utility;
using Mars.ViewModel;
using Mars.Views.baseView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for DashboardControl.xaml
    /// </summary>
    public partial class DashboardControl :
        MarsBaseViewControl
    {
        public DashboardControl(long projectId, string projectName)
        {
            
            InitializeComponent();
            this.DataContext = new DashboardViewModel(projectId, projectName);
            //this.Loaded += SetDetailPanelOnUI;
            Title = string.Format("Dashboard-[proj-{0}]", projectName);
        }

        /*
        private void SetDetailPanelOnUI(object sender, EventArgs e)
        {
            DashboardViewModel view = (DashboardViewModel)this.DataContext;

            //ContentPresenter contentPresenter = this.userControlColumn.HeaderTemplate.
            var rrr = this.userControlColumn.HeaderTemplate;

            //ContentPresenter cp = (ContentPresenter)FindName("dataTemplate", out rrr);

            for (int count = 1; count <= 9; count++)
            {
                Label lbl = TreeViewHelper.FindChild<Label>(this, "dblbl" + count);
                if (lbl != null)
                    lbl.Content = count * view.ScaleUnit;
            }
          
        }

     */

        private void dashboardGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender == null) return;

            DashboardViewModel dv = (DashboardViewModel)this.DataContext;
            
            if (dv.SelectedDashboardData == null) return;

            DashboardData data = dv.SelectedDashboardData;
            long storyboardId = data.StoryboardId;

            /*
            StoryboardEditViewModel sbvm = sbColl.SelectedStoryboardRows[0];
            string testCaseName = sbvm.TestCaseName;
            */

            SignalParent(this, new System.Windows.RoutedEventArgs(), storyboardId);
           
        }

        #region RoutedEvent
        public static readonly RoutedEvent TapEvent =
            EventManager.RegisterRoutedEvent(
                        "ShowStoryboard",
                        RoutingStrategy.Bubble,
                        typeof(RoutedEventHandler),
                        typeof(DashboardControl)
                        );

        // Provide CLR accessors for the event
        public event RoutedEventHandler ShowStoryboard
        {
            add { AddHandler(TapEvent, value); }
            remove { RemoveHandler(TapEvent, value); }
        }

        private void SignalParent(object sender, System.Windows.RoutedEventArgs e, long id)
        {
            //this.RaiseEvent(new RoutedEventArgs(TapEvent, this));
            this.RaiseEvent(new CustomRoutedEventArgs(TapEvent, id));

        }
        #endregion
    }
}
