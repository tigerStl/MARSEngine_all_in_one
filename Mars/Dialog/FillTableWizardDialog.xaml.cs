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

namespace Mars.Dialog
{
    /// <summary>
    /// Interaction logic for SaveAsDialog.xaml
    /// </summary>
    public partial class FillTableWizardDialog : Window, INotifyPropertyChanged
    {
        string _selectedRow;

        public string SelectedRow
        {
            get { return _selectedRow; }
            set 
            { 
                _selectedRow = value;
                UpdateResultValue();
            }
        }

        string _selectedColumn;

        public string SelectedColumn
        {
            get { return _selectedColumn; }
            set
            {
                _selectedColumn = value;
                UpdateResultValue();
            }
        }

        string _selectedType;

        public string SelectedType
        {
            get { return _selectedType; }
            set
            {
                _selectedType = value;
                UpdateResultValue();
            }
        }

        string _resultValue;

        public string ResultValue
        {
            get { return _resultValue; }
            set { _resultValue = value; }
        }

        public List<string> RowList { get; set; }
        public List<string> ColumnList { get; set; }
        public List<string> TypeList { get; set; }

       
        public FillTableWizardDialog(string question, string defaultAnswer = "")
        {
            init(defaultAnswer);
            DataContext = this;
            InitializeComponent();
            lblQuestion.Content = question;
            //txtAnswer.Text = defaultAnswer;
           
        }

    public void init(string initValue)
    {
        ResultValue = initValue;
        string[] words = initValue.Split(';');
        if (words.Length > 0 && words[0] != null)
            SelectedRow = words[0] ;

        if (words.Length > 1 && words[1] != null)
             SelectedColumn = words[1] ;

        if (words.Length > 2 && words[2] != null)
             SelectedType = DecodeType(words[2]) ;

            RowList = new List<string>
            {
               
               "DYNAMICROW",
                "1",
                "2",
                "3",
                "4",
                "5"
            };

            ColumnList = new List<string>
            {
                "FirstCol",
                "SecondCol",
                "ThitdCol",
                "_ccyCol"
            };

            TypeList = new List<string>
            {
                "Edit",
                "List",
                "Combo"
            };
        }


    private void UpdateResultValue()
    {
        if (_selectedRow != null && _selectedColumn != null && _selectedType != null)
        {
            ResultValue = _selectedRow + ";" + _selectedColumn + ";" + EncodeType(_selectedType);
            RaisePropertyChanged("ResultValue");
        }
            
       
    }
    private string DecodeType(string controlType)
    {
        string rc = "";

        switch (controlType)
        {
            case "1":
                rc = "Edit";
                break;

            case "2":
                rc = "List";
                break;

            case "3":
                rc = "Combo";
                break;


        }
        return rc;
    }

    private string EncodeType(string controlTypeCode)
    {
        string rc = "";

        switch (controlTypeCode)
        {
            case "Edit":
                rc = "1";
                break;

            case "List":
                rc = "2";
                break;

            case "Combo":
                rc = "3";
                break;
        }
        return rc;
    }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
                this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
                txtAnswer.SelectAll();
                txtAnswer.Focus();
        }

        public string Answer
        {
                get { return txtAnswer.Text; }
        }

        void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;

    }
}
