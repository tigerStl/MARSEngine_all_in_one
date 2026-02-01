using Mars.Utility;
using Mars.ViewModel;
using Mars.Views.baseView;
using Route2NSEx.src.Marquis.systemUtil;
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
    /// Interaction logic for TestCaseAddControl.xaml
    /// </summary>
    public partial class StoryboardAddControl :
        MarsBaseViewControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardAddControl));
        public StoryboardAddControl(long projectId)
        {
            InitializeComponent();
            this.DataContext = new StoryboardViewModel(projectId);
            try
            {
                Title = string.Format("Add StoryBoard for Project:[{0}]" ,projectId);
            }
            catch (Exception e)
            {
                Logger.Error("StoryboardAddControl", string.Format("Exception:[{0}]",e.Message),e);
            }
            
        }

        public StoryboardAddControl(string storyboardName, string action)
        {
            InitializeComponent();
            this.DataContext = new StoryboardViewModel(storyboardName);
            if (action.Equals("Open Test Case"))
            {
                this.storyboardName.IsReadOnly = true;
                this.btnCancel.IsEnabled = false;
                this.lblHeader.Content = "Test Case Open";
            }
            else if (action.Equals("Test Case Properties"))
            {
                this.storyboardName.IsEnabled = false;
                this.btnCancel.IsEnabled = false;
                this.lblHeader.Content = "Test Case Properties";
            }
            Title = (string)this.lblHeader.Content;
        }
          
        #region RoutedEvent
        public static readonly RoutedEvent TapEvent =
            EventManager.RegisterRoutedEvent(
                        "RefreshTree",
                        RoutingStrategy.Bubble,
                        typeof(RoutedEventHandler),
                        typeof(StoryboardAddControl)
                        );

        // Provide CLR accessors for the event
        public event RoutedEventHandler RefreshTree
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StoryboardViewModel vm = (StoryboardViewModel)DataContext;
            long storyboardId = vm.CreateStoryboard();
                        
            MessageBox.Show("Test storyboard is created!", "Mars Hint");
            SignalParent(this, new System.Windows.RoutedEventArgs(), storyboardId);
        }

        
    }
}
