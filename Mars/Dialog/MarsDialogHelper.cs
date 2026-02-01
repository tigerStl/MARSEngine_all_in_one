using Mars.Dialog;
using Mars.ViewModel;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Mars.Dialog
{

    public delegate ObservableCollection<SearchableCommonResult4ListView<T>> ConvertData2Display<T>(List<T> lstResult);
    public delegate bool AfterOkButtonClickEvent<T>(SearchableCommonResult4ListView<T> objectSelectedItem);

    public class MarsSearchDialogDataContext<T>: INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsSearchDialogDataContext<T>));

        public ConvertData2Display<T> convertData2Disp=null;
        public AfterOkButtonClickEvent<T> afterOkButtonClickHandler = null;

        private Nullable<bool> _dialogResult;
        public Nullable<bool> dialogResult
        {
            get
            {
                return _dialogResult;
            }
            set
            {
                if (_dialogResult!=value)
                {
                    _dialogResult = value;
                    RaisePropertyChanged("dialogResult");
                }
            }
        }

        protected string strHint;
        public string Hint
        {
            get { return strHint; } 
            set {
                if (string.Compare(strHint ?? "", value) != 0)
                {
                    strHint = value;
                    RaisePropertyChanged("Hint");
                }
             }
        }

        protected string strSearchKey;
        public string SearchKey
        {
            get { return strSearchKey; }
            set {
                if (strSearchKey==value) return;
                strSearchKey = value;
                RaisePropertyChanged("SearchKey");
            }
        }

        public MarsSearchDialogDataContext()
        {
            Logger.logBegin("MarsSearchDialogDataContext");
        }

        private List<T> _listResult;
        public List<T> listResult
        {
            get
            {
                return _listResult;
            }
            set
            {
                if (_listResult!=value)
                {
                    /// change listDisplayResult
                    /// 
                    _listResult = value;
                    if (convertData2Disp!=null)
                    {
                        _listDisplayResult = convertData2Disp(_listResult);
                        RaisePropertyChanged("listDisplayResult");
                    }
                    RaisePropertyChanged("listResult");
                }
            }
        }

        
        private ObservableCollection<SearchableCommonResult4ListView<T>> _listDisplayResult;
        public ObservableCollection<SearchableCommonResult4ListView<T>> listDisplayResult
        {
            get { return _listDisplayResult; }
            set {
                if (_listDisplayResult!=value )
                {
                    _listDisplayResult = value;
                    RaisePropertyChanged("listDisplayResult");
                }
            }
        }

        private SearchableCommonResult4ListView<T> _selectedResultItem;
        public SearchableCommonResult4ListView<T> selectedResultItem
        {
            get { return _selectedResultItem; }
            set {
                if (_selectedResultItem != value)
                {
                    _selectedResultItem = value;
                    RaisePropertyChanged("selectedResultItem");

                    if (_selectedResultItem!=null)
                    {
                        SearchKey = _selectedResultItem.Name;
                    }
                }
            }
        }

        private ICommand _onSearchButtonClick;
        public ICommand onSearchButtonClick
        {
            get
            {
                return _onSearchButtonClick;
            }
            set
            {
                if (_onSearchButtonClick!=value)
                {
                    _onSearchButtonClick = value;
                    RaisePropertyChanged("onSearchButtonClick");
                }
            }
        }

        
        public ICommand onOkButtonClick
        {
            get
            {
                return new DelegateCommand(()=> { ClickOkButtonEventImpl(); });
            }

            
        }

        public bool ClickOkButtonEventImpl()
        {
            Logger.logBegin("ClickOkButtonEventImpl");
            if (_selectedResultItem==null)
            {
                Dispatcher.CurrentDispatcher.Invoke(new Action(delegate () {
                    MessageBox.Show("Please select one object. ", "Hint");
                }));
                return false;
            }
            else
            {
                if (afterOkButtonClickHandler!=null)
                    if (!afterOkButtonClickHandler(this._selectedResultItem))
                    {
                        return false;
                    }
            }
            dialogResult = true;
            return true;
        }


        
        #region Notifypropery 
        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        #endregion
    }

    public class MarsDialogHelper
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsDialogHelper));
        #region MarsCommonFinder
        private static MarsCommonFinder gMarsCommonFinder = null;
        public static bool GetCommonFinderForModal<T>(MarsSearchDialogDataContext<T> objDialogDataContext)
        {
            if (gMarsCommonFinder==null)
            {
                gMarsCommonFinder = new MarsCommonFinder();
                
            }
            gMarsCommonFinder.DataContext = objDialogDataContext;
            bool isOk = gMarsCommonFinder.ShowDialog() ?? false;
            Logger.Info("GetCommonFinderForModal",string.Format("return :[{0}]", isOk));
            //gMarsCommonFinder.DialogResult = false;
            gMarsCommonFinder = null;
            objDialogDataContext.dialogResult = null;
            return isOk;
        }
        #endregion //MarsCommonFinder
    }

    public class SearchableCommonResult4ListView<T>: INotifyPropertyChanged
    {
        private string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set {
                if (_Name!=value)
                {
                    _Name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }
        private string _Description;
        public string Description
        {
            get {
                return _Description;
            }
            set
            {
                if(_Description!=value)
                {
                    _Description = value;
                    RaisePropertyChanged("Description");
                }
            }
        }

        public T objectAttached;


        #region Notifypropery 
        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        #endregion
    }
}
