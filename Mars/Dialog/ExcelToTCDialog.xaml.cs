using Mars.Utility;
using Mars.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace Mars.Dialog
{
    /// <summary>
    /// Interaction logic for ExcelToTCDialog.xaml
    /// </summary>
    public partial class ExcelToTCDialog : Window
    {
        private static ExcelToTCDialog instance;

        public delegate void LoadTC(DataSet ds , Dictionary<string, bool> dict, long appId);
        private  event LoadTC onLoadTC;


        public event LoadTC LoadEventHandler
        {
            add
            {
                // First try to remove the handler, then re-add it
                onLoadTC -= value;
                onLoadTC += value;
            }
            remove
            {
                onLoadTC -= value;
            }
        }


        public List<string> COL_NAMES = new List<string>() { "keyword", "object", "row_column", "value", "Comment" };

        //private ObservableCollection<string> _errorList;
        private bool _tcIsLoaded = false;
        private bool _dataIsLoaded = false;
        public ExcelToTCDialog()
        {
            InitializeComponent();
            this.DataContext = new ExcelToTCViewModel();
        }

        internal static ExcelToTCDialog GetInstance()
        {
        //    if (instance == null)
                instance = new ExcelToTCDialog();

            return instance;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Title = "Select Test Case Excel File";
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xls";
            dlg.Filter = "Excel Files|*.xls;*.xlsx";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                this.ExcelFileName.Text = filename;
            }
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        public void LoadFileWorker(string fileName, string mode)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                try
                {
                    this.IsStartedToLoad = true;
                    DataSet testCaseDataSet = ExcelUtil.WorkbookToDataSet(fileName, mode);
                    ExcelToTCViewModel model = (ExcelToTCViewModel)this.DataContext;

                    if (mode.Equals("TC"))
                    {
                        

                        foreach (DataTable dt in testCaseDataSet.Tables)
                        {
                            string[] columnNames = dt.Columns.Cast<DataColumn>()
                                    .Select(x => x.ColumnName)
                                    .ToArray();

                            foreach (string colName in columnNames)
                            {
                                if (COL_NAMES.Contains(colName) == false)
                                    dt.Columns.Remove(colName);
                            }

                            dt.Columns.Add("Status", typeof(System.String));
                        }
                        model.InitTC(testCaseDataSet);
                        this.listView1.Items.Refresh();
                    }

                    else if (mode.Equals("DATA"))
                    {
                        if (_dataIsLoaded == true)
                        {
                            System.Windows.MessageBox.Show("Loading of multiple data files is not supported.\n If you need to use a different Data file, please close and reopen the Test Case Import dialog ", "Load File", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        else
                        {
                            model.InitData(testCaseDataSet);
                            this.listView1.Items.Refresh();
                            _dataIsLoaded = true;
                        }
                       
                    }
                }
                finally
                {
                    this.IsStartedToLoad = false;
                }
            }
            ));
        }

        public bool IsStartedToLoad { get; set; }

        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = ExcelFileName.Text.ToString();
            if (File.Exists(filePath) == false)
            {
                System.Windows.MessageBox.Show("File " + filePath + " was not found.", "Load File", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
              
            new Thread(new ThreadStart(delegate() { LoadFileWorker(filePath, "TC"); })).Start();
            this._tcIsLoaded = true;
        }

        private void allButton_Click(object sender, RoutedEventArgs e)
        {
            ExcelToTCViewModel model = (ExcelToTCViewModel)this.DataContext;
            model.SetAllImportRequired(true);
            listView1.Items.Refresh();
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            ExcelToTCViewModel model = (ExcelToTCViewModel)this.DataContext;
            model.SetAllImportRequired(false);
            listView1.Items.Refresh();
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string tableName = this.listView1.SelectedItems
            ExcelToTCViewModel model = (ExcelToTCViewModel)this.DataContext;
            //model.SetCurrentDataTable();
            tcDataGrid.ItemsSource = model.CurrentDataTable.DefaultView;
            tcDataGrid.Items.Refresh();
        }

        private void btnGenerateTC_Click(object sender, RoutedEventArgs e)
        {
            if (this._tcIsLoaded == false)
            {
                System.Windows.MessageBox.Show("Please load Test Case first. ", "Load File", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try
            {
                ExcelToTCViewModel model = (ExcelToTCViewModel)this.DataContext;
                onLoadTC(model.ExcelData, model.GetTestCaseDict(), model.SelectedAppValue);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Load File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
           
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
       //     this.Hide();
       //     e.Cancel = true; ;
        }

        private void btnOpenDataFile_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Title = "Select Test Case Data File";
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xls";
            dlg.Filter = "Excel Files|*.xls;*.xlsx";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                dataFileName.Text = filename;
            }
        }

        private void btnLoadDataFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = dataFileName.Text.ToString();

            if (File.Exists(filePath) == false)
            {
                System.Windows.MessageBox.Show("File " + filePath + " was not found.", "Load File", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            new Thread(new ThreadStart(delegate() { LoadFileWorker(filePath, "DATA"); })).Start();
        }

        
    }
}

