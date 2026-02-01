using Mars.DataLayer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel
{
    class ExcelToTCViewModel : ViewModelBase
    {
        private DataSet _excelData;
        private DataTable _currentDataTable;

        Dictionary<string, long> _appDict;

        public Dictionary<string, long> AppDict
        {
            get { return _appDict; }
            set { _appDict = value; }
        }

        long _selectedAppValue;

        public long SelectedAppValue
        {
            get { return _selectedAppValue; }
            set 
            { 
                _selectedAppValue = value; 
            }
        }

        private TCHeader _selectedTCItem;

        public TCHeader SelectedTCItem
        {
            get { return _selectedTCItem; }
            set
            {
                _selectedTCItem = value;
                CurrentDataTable = ExcelData.Tables[_selectedTCItem.TCName];
                RaisePropertyChanged("SelectedTCItem");
            }
        }

        public DataTable CurrentDataTable
        {
            get { return _currentDataTable; }
            set
            {
                if (value != null)
                    _currentDataTable = value;
            }
        }

        private ObservableCollection<TCHeader> _tCHeaderRows;

        public ExcelToTCViewModel()
        {
            TCHeaderRows = new ObservableCollection<TCHeader>();
             LoadAppDict();
        
        }

        private void LoadAppDict()
        {
            AppDict = BoHelper.GetAllApps(MarsMainWindow.CurrentDatabaseIdx);
            SelectedAppValue = 1;
        }

        public ObservableCollection<TCHeader> TCHeaderRows
        {
            get
            {
                return _tCHeaderRows;
            }
            set
            {
                _tCHeaderRows = value;
            }
        }

        public DataSet ExcelData
        {
            get { return _excelData; }
            set { _excelData = value; }
        }

        public void InitTC(DataSet data)
        {
            ExcelData = data;
            LoadListData();
        }

        private void LoadListData()
        {
            var names = ExcelData.Tables.OfType<DataTable>().Select(dt => dt.TableName);
            foreach (string tableName in names)
            {
                TCHeader tcHeader = new TCHeader(tableName, true);
                TCHeaderRows.Add(tcHeader);
            }
        }

        internal void SetAllImportRequired(bool flag)
        {
            TCHeaderRows.ToList().ForEach(c => c.ImportRequired = flag);
        }

        internal void SetCurrentDataTable(string tableName)
        {
            CurrentDataTable = this.ExcelData.Tables[tableName];
        }

        public Dictionary<string, bool > GetTestCaseDict()
        {
            Dictionary<string, bool> dict = new Dictionary<string, bool>();
 
            foreach (var tc in TCHeaderRows)
            {
                dict.Add(tc.TCName, tc.ImportRequired);

            }

            return dict;
        }

        internal void InitData(DataSet testCaseDataSet)
        {
            UpdateTCData(testCaseDataSet);
        }

        private void UpdateTCData(DataSet testCaseDataSet)
        {
            DataTable tcDataTable = testCaseDataSet.Tables[0];

            Dictionary<string, DataRow> dict = new Dictionary<string, DataRow>();
            foreach (DataRow row in tcDataTable.Rows)
            {
                string data = row[0].ToString();
                if (data.Trim().Length > 0)
                    if (dict.Keys.Contains(data) == false)
                        dict.Add(data, row);
            }

            foreach (DataTable dt in ExcelData.Tables)
            {
                foreach (var column in tcDataTable.Columns)
                {
                    dt.Columns.Add(column.ToString());
                }

                foreach (DataRow row in dt.Rows)
                {
                    string value = row["value"].ToString();
 
                    foreach (DataColumn col in dt.Columns)
                    {
                        string colName = col.ToString();
                        if (colName.StartsWith("Data"))
                        {
                            if (value.Trim().Length > 0)
                                row[colName] = value;

                            string objName = row["object"].ToString();
                            if (dict.Keys.Contains(objName))
                            {
                                string data = dict[objName][colName].ToString();
                                if (data.Trim().Length > 0)
                                    row[colName] = data;
                            }
                        }
                    }
                }
            }
        }
    }

    class TCHeader : Notify
    {
        public TCHeader(string tcName, bool importRequired)
        {
            _tCName = tcName;
            _importRequired = importRequired;
        }

        string _tCName;

        public string TCName
        {
            get { return _tCName; }
            set { _tCName = value; }
        }

        bool _importRequired;

        public bool ImportRequired
        {
            get { return _importRequired; }
            set
            {
                _importRequired = value;
            }
        }
    }
}
