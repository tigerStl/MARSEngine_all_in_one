using Mars.ViewModel;
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
    public partial class StoryboardCombinedControl :
        MarsBaseViewControl
, INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardCombinedControl));
        public readonly static DependencyProperty CurrentStoryBoardDetailIDProperty = DependencyProperty.Register("CurrentStoryBoardDetailID",
            typeof(string), typeof(StoryboardCombinedControl), null);
        public readonly static DependencyProperty IsNeedRefreshResultNowProperty = DependencyProperty.Register("IsNeedRefreshResultNow",
            typeof(bool), typeof(StoryboardCombinedControl), null);
        public string CurrentStoryBoardDetailID
        {
            get { return (string)GetValue(CurrentStoryBoardDetailIDProperty); }
            set {
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
        
        public long StoryboardId
        {
            get;set;
        }

        private MarsStoryboardTreeView _AssignedTreeItem = null;

        public StoryboardCombinedControl(string strStoryBoardName, long storyboardId, MarsStoryboardTreeView assignedTreeItm= null)
        {
            StoryboardId = storyboardId;
            InitializeComponent();
            _AssignedTreeItem = assignedTreeItm;
            
            Title = string.Format("SB:[{0}]",strStoryBoardName);
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
            

            if (resultViewControl==null)
            {
                resultViewControl = new TestResultTabView();
            }
            resultViewControl.SetBinding(TestResultTabView.CurrentStoryBoardDetailIdProperty, bindng);

            this.HistoryResultViewPanel.Children.Add(resultViewControl);


        }

        private void HandleChildSignal(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.logBegin("HandleChildSignal", string.Format("Sender:[{0}]", sender));

            if (ProjectGridPanel.Children.Count <= 0) return;
            var tcTabControl = ProjectGridPanel.Children[0] as Mars.Views.TestCaseTabControl;
            if (tcTabControl == null) return;
            if (storyboardDocPanel.Children.Count <= 0) return;
            var storyboard = storyboardDocPanel.Children[0] as StoryboardEditControl;
            if (storyboard == null) return;
            var storybardModel = storyboard.DataContext as Mars.ViewModel.StoryboardColl;
            if (storybardModel == null) return;
            if (storybardModel.SelectedStoryboardRows.Count <= 0) return;
            StoryboardEditViewModel sbvm = storybardModel.SelectedStoryboardRows[0] as StoryboardEditViewModel;

                //if (testCaseTabControl == null)
                //{
                //    Logger.Warnning("HandleChildSignal", "testCaseTabControl == null");
                //    return;
                //}

            //StoryboardColl sbColl = StoryboardCache.currentSBColl;
            //if (sbColl.SelectedStoryboardRows == null) return;
            //if (sbColl.SelectedStoryboardRows.Count() == 0)
            //{
            //    Logger.Warnning("HandleChildSignal", "sbColl.SelectedStoryboardRows.Count() == 0");
            //    return;
            //}

            //StoryboardEditViewModel sbvm = sbColl.SelectedStoryboardRows[0];
            if (sbvm.SelectedTestCase == null)
            {
                Logger.Warnning("HandleChildSignal", "sbvm.SelectedTestCase == null");
                return;
            }
            string testCaseName = sbvm.SelectedTestCase.DataItemName;
            long testCaseId = sbvm.SelectedTestCase.Id;
            this.CurrentStoryBoardDetailID = sbvm.StoryboardDetailId+"";
            long dataSheetId = (long)sbvm.SelectedDataSetName.Id;


            // TestCaseEdit testCaseEdit = new TestCaseEdit(testCaseName, dataSheetId);
            // testCaseTabControl.addTestCaseEdit(testCaseEdit, testCaseName);

            string dataSheetName = sbvm.SelectedDataSetName.DataItemName;
            bool shared = false;
            if (dataSheetName != null && dataSheetName.StartsWith("SH"))
                shared = true;

            TestCaseEdit testCaseEdit = new TestCaseEdit(MarsMainWindow.CurrentDatabaseIdx, testCaseId, dataSheetId, shared, null);
            //testCaseEdit.onRequestWCFSvcStatusAgent = onRequestWCFServiceStatusImpl;
            //testCaseEdit.onRequestStartWCFSvcAgent = OnRequestStartWCFSvcImpl;

            testCaseEdit.HideControlPanel();
            tcTabControl.addTestCaseEdit(testCaseEdit, testCaseName);

            //testCaseTabControl.addTestCaseEdit(testCaseEdit, testCaseName);
            Binding objBindDetailStoryBoardId = new Binding()
            {
                Path = new PropertyPath("CurrentStoryBoardDetailID"),
                Source = this
            };
            ResultViewTabControl.StoryboardId = sbvm.StoryboardId;
            ResultViewTabControl.CurrentStoryBoardDetailId = sbvm.StoryboardDetailId;
            //CurrentStoryBoardDetailID = 
            //ResultViewTabControl.SetBinding(TestResultTabView.CurrentStoryBoardDetailIdProperty, objBindDetailStoryBoardId);
            //RaisePropertyChanged("CurrentStoryBoardDetailID");

        }

    }

    
}
