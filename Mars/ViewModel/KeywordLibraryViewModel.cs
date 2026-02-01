using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Model;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;
using Microsoft.Practices.EnterpriseLibrary.Validation;
using System.Security.Principal;
using Mars.Business;
using Mars.Dto;
using System.Windows.Forms;
using System.IO;
//using Microsoft.Practices.Prism.Commands;
using Mars.DataLayer;
using Route2NSEx.src.Marquis.systemUtil;
using Prism.Commands;

namespace Mars.ViewModel
{
    public class KeywordLibraryViewModel : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(KeywordLibraryViewModel));

        #region Private variables 
        long _sNo;
        string _keyword;
        string _controlType;
        string _entryInDataFile;

        private ICommand _saveCommand;
        private ICommand _addCommand;
        private ICommand _editCommand;
        private ICommand _deleteCommand;
        private ICommand _closePopupCommand;
        MarsEntities marsEntities;
        ObservableCollection<KeywordLibraryData> _keywordLibrarydata = new ObservableCollection<KeywordLibraryData>();
        private DelegateCommand<object> _exportToExcelCommand;
        #endregion

        #region Constructor 
        public KeywordLibraryViewModel(string strDBIdx)
        {
            marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            SaveCommand = new DelegateCommand(() => { SaveKeyword(); });
            _exportToExcelCommand = new DelegateCommand<object>(this.ExportToExcel);            
            AddCommand = new DelegateCommand(() => { AddKeyword(); });
            EditCommand = new DelegateCommand(() => { EditKeyword(); });
            DeleteCommand = new DelegateCommand(() => { DeleteKeyword(); });

            ClosePopupCommand = new DelegateCommand(() => { ClosePopUp(); });

            KeywordList = new ObservableCollection<KeywordLibraryData>();
            GetKeywordList(strDBIdx);
            GetControlTypeList(strDBIdx);
            SelectedItems = new Dictionary<string, string>();
        }

        public DelegateCommand<object> ExportToExcelCommand
        {
            get
            {
                return _exportToExcelCommand;
            }

            set
            {
                _exportToExcelCommand = value;
            }
        }
        #endregion
       
        #region Properties
        public ObservableCollection<KeywordLibraryData> KeywordList
        {
            get
            {
                return _keywordLibrarydata;
            }
            set
            {
                _keywordLibrarydata = value;
                RaisePropertyChanged("KeywordList");
            }
        }

        KeywordLibraryData _selectedRow;
        public KeywordLibraryData SelectedRow
        {
            get
            {
                return _selectedRow;
            }
            set
            {
                _selectedRow = value;
                RaisePropertyChanged("SelectedRow");
            }
        }

        public long SNo
        {
            get
            {
                return _sNo;
            }
            set
            {
                _sNo = value;
                RaisePropertyChanged("SNo");
            }
        }

        public string Keyword
        {
            get
            {
                return _keyword;
            }
            set
            {
                _keyword = value;
                RaisePropertyChanged("Keyword");
            }
        }

        private ObservableCollection<string> controlTypes;
        public ObservableCollection<string> ControlTypes
        {
            get
            {
                return controlTypes;
            }
            set
            {
                controlTypes = value;
                RaisePropertyChanged("ControlTypes");
            }
        }

        public string ControlType
        {
            get
            {
                return _controlType;
            }
            set
            {
                _controlType = value;
                RaisePropertyChanged("ControlType");
            }
        }

        public string EntryInDataFile
        {
            get
            {
                return _entryInDataFile;
            }
            set
            {
                _entryInDataFile = value;
                RaisePropertyChanged("EntryInDataFile");
            }
        }

        string _keywordInput;
        public string KeywordInput
        {
            get
            {
                return _keywordInput;
            }
            set
            {
                _keywordInput = value;
                RaisePropertyChanged("KeywordInput");
            }
        }

        string _entryInDataFileInput;
        public string EntryInDataFileInput
        {
            get
            {
                return _entryInDataFileInput;
            }
            set
            {
                _entryInDataFileInput = value;
                RaisePropertyChanged("EntryInDataFileInput");
            }
        }

        private bool _isOpen;
        public bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                if (_isOpen == value) return;
                _isOpen = value;
                RaisePropertyChanged("IsOpen");
            }
        }

        private Dictionary<string, string> _items;
        private Dictionary<string, string> _selectedItems;

        public Dictionary<string, string> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                RaisePropertyChanged("Items");
            }
        }

        public Dictionary<string, string> SelectedItems
        {
            get
            {
                return _selectedItems;
            }
            set
            {
                _selectedItems = value;
                RaisePropertyChanged("SelectedItems");
            }
        }
        #endregion

        #region Methods
        private void ExportToExcel(object dataGrid)
        {
            try
            {
                if (dataGrid != null)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "CSV|*.CSV|Excel|*.XLS";
                    saveFileDialog.Title = "Save an excel File";
                    saveFileDialog.ShowDialog();
                    string strFilePath = "";
                    if (saveFileDialog.FileName != "")
                    {
                        strFilePath = saveFileDialog.FileName;
                        StreamWriter sw = new StreamWriter(strFilePath, false);
                        sw.Write("No,Keyword,ControlType,Entry in data file");
                        sw.Write(sw.NewLine);
                        foreach (KeywordLibraryData objRegObj in (System.Collections.ObjectModel.ObservableCollection<Mars.ViewModel.KeywordLibraryData>)(dataGrid))
                        {
                            sw.Write(objRegObj.SNo + "," + objRegObj.Keyword + "," + objRegObj.ControlType.Replace("\r\n", " | ").ToString() + "," + objRegObj.EntryInDataFile);
                            sw.Write(sw.NewLine);
                        }
                        sw.Close();
                        System.Windows.MessageBox.Show("Export completed. Please open your file from " + strFilePath.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExportToExcel",string.Format("Exception:[{0}]",ex.Message),ex);
            }

        }
        private void ClosePopUp()
        {
            IsOpen = false;
        }

        private void ClearTextInPopUp()
        {
            keywordId = 0;
            KeywordInput = string.Empty;
            EntryInDataFileInput = string.Empty;
            SelectedItems.Clear();
            SelectedItems = new Dictionary<string, string>();
        }

        long keywordId;
        private void EditKeyword()
        {
            IsOpen = true;
            try
            {
                if (SelectedRow != null)
                {
                    keywordId = SelectedRow.KeywordId;
                    KeywordInput = SelectedRow.Keyword;
                    EntryInDataFileInput = SelectedRow.EntryInDataFile;

                    string conrolTypeInput = SelectedRow.ControlType;
                    var splitResult = conrolTypeInput.Split(new string[] { System.Environment.NewLine },
                                        System.StringSplitOptions.RemoveEmptyEntries);

                    SelectedItems.Clear();
                    SelectedItems = new Dictionary<string, string>();
                    Dictionary<string, string> lKV = new Dictionary<string, string>();
                    foreach (string type in splitResult)
                    {
                        string key = string.Empty;
                        Items.TryGetValue(type, out key);
                        lKV.Add(type, key);
                    }
                    SelectedItems = lKV;
                }
            }
            catch(Exception ex)
            {
                Logger.Error("EditKeyword",string.Format("Exceptions:[{0}]",ex.Message),ex);
            }
        }

        public void AddKeyword()
        {
            IsOpen = true;
            ClearTextInPopUp();
        }

        public bool SaveNewKeyword(string strDBIdx,KeywordLibraryData newKeyword)
        {
            if (!IsValid)
            {
                StringBuilder sbError = new StringBuilder();
                sbError.Append("Please enter valid :");

                foreach (string error in validationErrors)
                {
                    sbError.Append(error);
                    sbError.Append(" : ");
                }
                System.Windows.MessageBox.Show(sbError.ToString(), "Add Keyword", MessageBoxButton.OK, MessageBoxImage.Error);
                validationErrors.Clear();
                return false;
            }

            marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            T_KEYWORDDTO tKeywordDto = new T_KEYWORDDTO();

            tKeywordDto.KEY_WORD_ID = newKeyword.KeywordId;
            tKeywordDto.KEY_WORD_NAME = newKeyword.Keyword;
            tKeywordDto.ENTRY_IN_DATA_FILE = newKeyword.EntryInDataFile;

            marsEntities.T_KEYWORD.Add(T_KEYWORDAssembler.ToEntity(tKeywordDto));

            B_DIC_RELATION_KEYWORD objDicRelKeyword = new B_DIC_RELATION_KEYWORD();
            T_DIC_RELATION_KEYWORDDTO objDicRelKeywordDto;
            foreach (var selItem in SelectedItems)
            {
                objDicRelKeywordDto = new T_DIC_RELATION_KEYWORDDTO();
                objDicRelKeywordDto.TYPE_ID = Convert.ToInt64(selItem.Value);
                objDicRelKeywordDto.KEY_WORD_ID = newKeyword.KeywordId;
                objDicRelKeywordDto.RELATION_ID = objDicRelKeyword.GetKeywordRelationId(strDBIdx);

                marsEntities.T_DIC_RELATION_KEYWORD.Add(T_DIC_RELATION_KEYWORDAssembler.ToEntity(objDicRelKeywordDto));
            }

            try
            {
                if (marsEntities.SaveChanges() > 0)
                {
                    GetKeywordList(strDBIdx);
                    IsOpen = false;
                    System.Windows.MessageBox.Show("Keyword relation Added successfully", "Add Keyword Library", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    marsEntities = null;
                    System.Windows.MessageBox.Show("Error Adding Keyword relation", "Add Keyword Library", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                marsEntities = null;
                System.Windows.MessageBox.Show(ex.InnerException.ToString(), "Add Keyword Library", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool SaveUpdatedKeyword(string strDBIdx, KeywordLibraryData updatedKeyword)
        {
            if (!IsValid)
            {
                StringBuilder sbError = new StringBuilder();
                sbError.Append("Please enter valid :");

                foreach (string error in validationErrors)
                {
                    sbError.Append(error);
                    sbError.Append(" : ");
                }
                System.Windows.MessageBox.Show(sbError.ToString(), "Update Keyword", MessageBoxButton.OK, MessageBoxImage.Error);
                validationErrors.Clear();
                return false;
            }

            marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

            var selectedKeyword = marsEntities.T_KEYWORD.First(x => x.KEY_WORD_ID == updatedKeyword.KeywordId);

            selectedKeyword.KEY_WORD_NAME = updatedKeyword.Keyword;
            selectedKeyword.ENTRY_IN_DATA_FILE = updatedKeyword.EntryInDataFile;

            B_DIC_RELATION_KEYWORD objDicRelKeyword = new B_DIC_RELATION_KEYWORD();
            T_DIC_RELATION_KEYWORDDTO objDicRelKeywordDto;

            //remove keyword relation before adding new for the keyword 
            var relDecKeyword = (from a in marsEntities.T_DIC_RELATION_KEYWORD
                                 where a.KEY_WORD_ID == updatedKeyword.KeywordId
                                 select a);
            foreach (var keywordRel in relDecKeyword)
            {
                marsEntities.T_DIC_RELATION_KEYWORD.Remove(keywordRel);
            }

            foreach (var selItem in SelectedItems)
            {
                objDicRelKeywordDto = new T_DIC_RELATION_KEYWORDDTO();
                objDicRelKeywordDto.TYPE_ID = Convert.ToInt64(selItem.Value);
                objDicRelKeywordDto.KEY_WORD_ID = updatedKeyword.KeywordId;
                objDicRelKeywordDto.RELATION_ID = objDicRelKeyword.GetKeywordRelationId(strDBIdx);
                marsEntities.T_DIC_RELATION_KEYWORD.Add(T_DIC_RELATION_KEYWORDAssembler.ToEntity(objDicRelKeywordDto));
            }

            try
            {
                if (marsEntities.SaveChanges() > 0)
                {
                    GetKeywordList(strDBIdx);
                    IsOpen = false;
                    System.Windows.MessageBox.Show("Keyword relation Updated successfully", "Edit Keyword Library", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    marsEntities = null;
                    System.Windows.MessageBox.Show("Error updating Keyword relation", "Edit Keyword Library", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                marsEntities = null;
                System.Windows.MessageBox.Show(ex.InnerException.ToString(), "Edit Keyword Library", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SaveKeyword()
        {
            if (keywordId > 0)
            {
                KeywordLibraryData keywordDataFiltered = new KeywordLibraryData();
                keywordDataFiltered.KeywordId = keywordId;
                keywordDataFiltered.Keyword = KeywordInput;
                keywordDataFiltered.EntryInDataFile = EntryInDataFileInput;

                StringBuilder sbSelControls = new StringBuilder();
                foreach (var selItem in SelectedItems)
                {
                    sbSelControls.Append(selItem.Key);
                    sbSelControls.Append(Environment.NewLine);
                }
                keywordDataFiltered.ControlType = sbSelControls.ToString();
                keywordDataFiltered.ControlTypes = new ObservableCollection<string>(SelectedItems.Select(p=>p.Key).OrderBy(p=>p));
                SaveUpdatedKeyword(MarsMainWindow.CurrentDatabaseIdx,keywordDataFiltered);
            }
            else
            {
                KeywordLibraryData keywordData = new KeywordLibraryData();
                B_KEYWORD bKeyword = new B_KEYWORD();
                keywordData.KeywordId = bKeyword.GetKeywordId(MarsMainWindow.CurrentDatabaseIdx); ;
                keywordData.Keyword = KeywordInput;
                keywordData.EntryInDataFile = EntryInDataFileInput;

                StringBuilder sbSelControls = new StringBuilder();
                foreach (var selItem in SelectedItems)
                {
                    sbSelControls.Append(selItem.Key);
                    sbSelControls.Append(Environment.NewLine);
                }
                keywordData.ControlType = sbSelControls.ToString();
                keywordData.ControlTypes = new ObservableCollection<string>(SelectedItems.Select(p => p.Key).OrderBy(p => p));
                SaveNewKeyword(MarsMainWindow.CurrentDatabaseIdx, keywordData);
            }
        }

        public void GetControlTypeList(string strDBIdx)
        {
            B_GUI_COMPONENT_TYPE_DIC objControlType = new B_GUI_COMPONENT_TYPE_DIC();
            Items = objControlType.GetControlTypeList(strDBIdx);
        }
        public void GetKeywordList(string strDBIdx)
        {
            KeywordLibraryData keywordLibData;
            B_KEYWORD objKeyword = new B_KEYWORD();
            B_DIC_RELATION_KEYWORD objKeywordRel = new B_DIC_RELATION_KEYWORD();
            B_GUI_COMPONENT_TYPE_DIC objControlType = new B_GUI_COMPONENT_TYPE_DIC();

            List<B_KEYWORD> keywordData = objKeyword.GetKeywords(strDBIdx);
            ObservableCollection<KeywordLibraryData> refreshedKeyword = new ObservableCollection<KeywordLibraryData>();
            int serialNo = 1;
            foreach (B_KEYWORD keyword in keywordData)
            {
                keywordLibData = new KeywordLibraryData();
                keywordLibData.SNo = serialNo;
                keywordLibData.KeywordId = keyword.KEY_WORD_ID;
                keywordLibData.Keyword = keyword.KEY_WORD_NAME;
                keywordLibData.EntryInDataFile = keyword.ENTRY_IN_DATA_FILE;

                StringBuilder sbControlTypes = new StringBuilder();
                List<string> tmpList = new List<string>();
                //keywordLibData.ControlTypes = new ObservableCollection<string>();
                foreach (long typeId in keyword.T_DIC_RELATION_KEYWORD_RELATION_ID)
                {

                    string controlType = objControlType.GetControlTypeNames(strDBIdx,typeId);
                    tmpList.Add(controlType);
                    sbControlTypes.Append(controlType);
                }
                tmpList.Sort();
                keywordLibData.ControlType = string.Join(Environment.NewLine, tmpList);//sbControlTypes.ToString();
                keywordLibData.ControlTypes = new ObservableCollection<string>(tmpList);


                refreshedKeyword.Add(keywordLibData);
                serialNo++;
            }
            KeywordList.Clear();
            KeywordList = refreshedKeyword;
        }

        public void DeleteKeyword()
        {
            if (SelectedRow != null)
            {
                keywordId = SelectedRow.KeywordId;
                string msg = "Do you want to delete " + SelectedRow.Keyword + " Keyword from the Library?";
                MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Delete Keyword Library", MessageBoxButton.YesNo, MessageBoxImage.Question);
             
                if (MessageBoxResult.Yes == result)
                {
                    marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:MarsMainWindow.CurrentDatabaseIdx);

                    var selectedKeyword = marsEntities.T_KEYWORD.First(x => x.KEY_WORD_ID == keywordId);
                    marsEntities.T_KEYWORD.Remove(selectedKeyword);

                    //remove keyword relation
                    var relDecKeyword = (from a in marsEntities.T_DIC_RELATION_KEYWORD
                                         where a.KEY_WORD_ID == keywordId
                                         select a);
                    foreach (var keywordRel in relDecKeyword)
                    {
                        marsEntities.T_DIC_RELATION_KEYWORD.Remove(keywordRel);
                    }

                    try
                    {
                        if (marsEntities.SaveChanges() > 0)
                        {
                            GetKeywordList(MarsMainWindow.CurrentDatabaseIdx);
                            System.Windows.MessageBox.Show("Keyword relation Deleted successfully", "Delete Keyword Library", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            marsEntities = null;
                            System.Windows.MessageBox.Show("Error deleting Keyword relation", "Delete Keyword Library", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        marsEntities = null;
                        System.Windows.MessageBox.Show(ex.InnerException.ToString(), "Edit Keyword Library", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion

        #region Commands
        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }

            set
            {
                _saveCommand = value;
            }
        }

        
        public ICommand AddCommand
        {
            get { return _addCommand; }
            set { _addCommand = value; }
        }

        public ICommand EditCommand
        {
            get { return _editCommand; }
            set { _editCommand = value; }
        }

        public ICommand DeleteCommand
        {
            get { return _deleteCommand; }
            set { _deleteCommand = value; }
        }

        public ICommand ClosePopupCommand
        {
            get { return _closePopupCommand; }
            set { _closePopupCommand = value; }
        }
        #endregion

        public List<string> validationErrors = new List<string>();

        static readonly string[] ValidatedProperties =
        {
                "KeywordInput",
                "EntryInDataFileInput",
        };

        public bool IsValid
        {
            get
            {
                foreach (string property in ValidatedProperties)
                {
                    if (GetValidationError(property) != null)
                        validationErrors.Add(GetValidationError(property));
                }
                if (validationErrors.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }

        private string GetValidationError(string propertyName)
        {
            string error = null;

            switch (propertyName)
            {
                case "KeywordInput":
                    error = this.ValidateKeyword();
                    break;

                case "EntryInDataFileInput":
                    error = this.ValidateEntryinDataFile();
                    break;
                default:
                    error = null;
                    throw new Exception("Unexpected property being validated on Service");
            }

            return error;
        }

        string ValidateKeyword()
        {
            if (IsStringMissing(this.KeywordInput))
            {
                return "Keyword";
            }
            return null;
        }

        string ValidateEntryinDataFile()
        {
            if (IsStringMissing(this.EntryInDataFileInput))
            {
                return "EntryInDataFileInput";
            }
            return null;
        }

        static bool IsStringMissing(string value)
        {
            return
                String.IsNullOrEmpty(value) ||
                value.Trim() == String.Empty;
        }
    }

    public class KeywordLibraryData
    {
        public long SNo { get; set; }
        public long KeywordId { get; set; }
        public string Keyword { get; set; }
        public string EntryInDataFile { get; set; }
        public string ControlType { get; set; }

        private ObservableCollection<string> controlTypes;
        public ObservableCollection<string> ControlTypes
        {
            get
            {
                return controlTypes;
            }
            set
            {
                controlTypes = value;
            }
        }
    }
}
