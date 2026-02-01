using Mars.Views.baseView;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for DashboardCombinedControl.xaml
    /// </summary>
    public partial class StoryboardCompareControl :
        MarsBaseViewControl
        , INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardCompareControl));

        public readonly static DependencyProperty CurrentStoryBoardDetailIDProperty = DependencyProperty.Register("CurrentStoryBoardDetailID",
            typeof(string), typeof(StoryboardCompareControl), null);
        public readonly static DependencyProperty IsNeedRefreshResultNowProperty = DependencyProperty.Register("IsNeedRefreshResultNow",
            typeof(bool), typeof(StoryboardCompareControl), null);
        public string CurrentStoryBoardDetailID
        {
            get { return (string)GetValue(CurrentStoryBoardDetailIDProperty); }
            set
            {
                SetValue(CurrentStoryBoardDetailIDProperty, value);
                OnPropertyChanged("CurrentStoryBoardDetailID");
            }
        }
        public bool IsNeedRefreshResultNow
        {
            get { return (bool)GetValue(IsNeedRefreshResultNowProperty); }
            set
            {
                SetValue(IsNeedRefreshResultNowProperty, value);
                if (value)
                {
                    OnPropertyChanged("IsNeedRefreshResultNow");
                }
            }
        }


        public StoryboardCompareControl()
        {
            InitializeComponent();
            Title = "SB Comparasion";
        }

        private TestResultTabView resultViewControl = null;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var changedEvent = PropertyChanged;
            if (changedEvent != null)
            {
                changedEvent(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        internal void AttachResultPanel()
        {
            Logger.logBegin("AttachResultPanel");
            this.HistoryResultViewPanel.Children.Clear();

            Binding bindng = new Binding()
            {
                Path = new PropertyPath("CurrentStoryBoardDetailID"),

            };


            if (resultViewControl == null)
            {
                resultViewControl = new TestResultTabView();
            }
            resultViewControl.SetBinding(TestResultTabView.CurrentStoryBoardDetailIdProperty, bindng);

            this.HistoryResultViewPanel.Children.Add(resultViewControl);


        }

        private void MarsBaseViewControl_Tap(object sender, RoutedEventArgs e)
        {
            Logger.logBegin("MarsBaseViewControl_Tap");
        }
    }
}
