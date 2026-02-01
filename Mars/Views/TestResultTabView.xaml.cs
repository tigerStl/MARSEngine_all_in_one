using Mars.Utility;
using Mars.ViewModel;
using Mars.Views.baseView;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
    /// Interaction logic for TestResultTabView.xaml
    /// </summary>
    public partial class TestResultTabView :
        MarsBaseViewControl
//, INotifyPropertyChanged
    {

        private MLogger Logger = MLogger.GetLogger(typeof(TestResultTabView));

        public static readonly DependencyProperty CurrentStoryBoardDetailIdProperty = DependencyProperty.Register("CurrentStoryBoardDetailId", typeof(long?), typeof(TestResultTabView));
        public static readonly DependencyProperty IsNeedRefreshProperty = DependencyProperty.Register("IsNeedRefresh",typeof(bool),typeof(TestResultTabView));

        //public event PropertyChangedEventHandler PropertyChanged;
        //internal void RaisePropertyChanged(string prop)
        //{
        //    if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        //}

        public long? StoryboardId { get; set; }
        public long? CurrentStoryBoardDetailId
        {
            get { return (long?)GetValue(CurrentStoryBoardDetailIdProperty); }
            set {
                SetValue(CurrentStoryBoardDetailIdProperty,value);
                /// refresh data context
                /// 
                CreateDataContext(value,storyboardId: StoryboardId);
                
            }
        }

        public bool IsNeedRefresh
        {
            get { return (bool)GetValue(IsNeedRefreshProperty); }
            set
            {
                SetValue(IsNeedRefreshProperty,value);
                if (IsNeedRefresh)
                {
                    RaisePropertyChanged("IsNeedRefresh");
                    CreateDataContext(CurrentStoryBoardDetailId);
                }
            }
        }

        public TestResultTabView()
        {
            InitializeComponent();

            /// initialization data context
            /// 
            CreateDataContext();

            Title = "TC Report";
        }

        public void CreateDataContext(long? storyboardDetailId=null,long? storyboardDetailIdCompare=null, long? storyboardId=null)
        {
            try
            {
                Logger.logBegin("CreateDataContext", $"detail:{storyboardDetailId}, compareDetail:{storyboardDetailIdCompare}");
            
                Logger.Info("CreateDataContext",string.Format("storyboardDetailId:[{0}] storyboardDetailIdCompare:[{1}]", storyboardDetailId??-1, storyboardDetailIdCompare??-1));
                this.DataContext = new TestResultTabViewModel(storyboardDetailId, storyboardDetailIdCompare, storyboardId);
                RaisePropertyChanged("DataContext");
                this.tabcntrlResults.SelectedIndex = 0;
                Logger.Info("CreateDataContext", "Performance...trace end");
            }
            finally
            {
                Logger.logEnd("CreateDataContext");
            }
        }

        internal void onStoryBoardDetailIdChangeImpl(long? storyboardDetailId)
        {
            Logger.Info("onStoryBoardDetailIdChangeImpl",string.Format("storyboardDetailId:[{0}]", storyboardDetailId));
            CurrentStoryBoardDetailId = storyboardDetailId;
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveResultToFile();
            }
        }

        private void SaveResultToFile()
        {
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog.Title = "Select Output File Name";
                saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TestResultTabViewModel report = (TestResultTabViewModel)this.DataContext;
                    DataTable table = new DataTable();
                    table.TableName = "Result1";
                    table.Columns.Add("Object", typeof(string));
                    table.Columns.Add("Baseline", typeof(string));
                    table.Columns.Add("Compare", typeof(string));
                    table.Columns.Add("Result", typeof(string));

                    int currentTab = tabcntrlResults.SelectedIndex;

                    foreach (var row in report.ResultTestReport[currentTab].LoopCompareReuslt)
                    {
                        table.Rows.Add(row.ObjectHappyName,
                                        row.BaseLineData,
                                        row.NoneBaseLineData,
                                        row.CompareResult);
                    }

                    DataSet ds = new DataSet();
                    ds.Tables.Add(table);
                    ExcelUtil.ExportDataSetToExcel(ds, saveFileDialog.FileName);
                }
        }

        internal void onHistoryDataRequireRefreshImpl(string strPropertyName)
        {
            IsNeedRefresh = true;
            //RaisePropertyChanged("IsNeedRefresh");            
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
             SaveResultToFile();
        }
    }
}
