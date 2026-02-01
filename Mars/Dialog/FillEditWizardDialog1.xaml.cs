using Mars.Dto;
using Mars.Utility;
using System;
using System.Collections.Generic;
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

namespace Mars.Dialog
{
    /// <summary>
    /// Interaction logic for FillEditWizard1.xaml
    /// </summary>
    public partial class FillEditWizardDialog1 : Window, INotifyPropertyChanged
    {
        #region Notifypropery
        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        #endregion

        private Dictionary<string, TreeViewItem> dict = new Dictionary<string, TreeViewItem>(); 
        private string _selectedKeyword;

        public string SelectedKeyword
        {
            get { return _selectedKeyword; }
            set { _selectedKeyword = value; }
        }
        private string _selectedObject;

        public string SelectedObject
        {
            get { return _selectedObject; }
            set { _selectedObject = value; }
        }

        private string _rowColValue;

        public string RowColValue
        {
            get { return _rowColValue; }
            set { _rowColValue = value; }
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

        public List<string> TypeList { get; set; }

        private List<T_OBJECT_CHILDDTO> objectChildDtoList;
        public FillEditWizardDialog1(List<T_OBJECT_CHILDDTO> objectChildDtoList)
        {
            this.objectChildDtoList = objectChildDtoList;
            DataTable dt = DataTableUtil.ToDataTable(objectChildDtoList);
            InitializeComponent();
        }

        public FillEditWizardDialog1(string keywordName, string objectName, string rowColValue, List<T_OBJECT_CHILDDTO> objectChildDtoList)
        {
            this.SelectedKeyword = keywordName;
            this.SelectedObject = objectName;
            this.RowColValue = rowColValue;
            this.objectChildDtoList = objectChildDtoList;
            DataTable dt = DataTableUtil.ToDataTable(objectChildDtoList);
            this.DataContext = this;
            InitBefore();
            InitializeComponent();
            InitAfter();
            
        }



        public void InitAfter()
        {
            if (SelectedRow.Equals("DYNAMICROW"))
            {
                this.spinButton.IsEnabled = false;
                rbLastRow.IsChecked = true;
            }
 
            else
            {
                this.spinButton.IsEnabled = true;
                rbRowNumber.IsChecked = true;
                RowValue = Convert.ToDouble(SelectedRow);
            }
        }
        public void InitBefore()
        {

            string[] words = RowColValue.Split(';');
            if (words.Length > 0 && words[0] != null)
                SelectedRow = words[0];

            if (words.Length > 1 && words[1] != null)
                SelectedColumn = words[1];

            if (words.Length > 2 && words[2] != null)
                SelectedType = DecodeType(words[2]);

            
            TypeList = new List<string>
            {
                "Edit",
                "List",
                "Combo"
            };
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


        private void UpdateResultValue()
        {

            if (_selectedRow != null && _selectedColumn != null && _selectedType != null)
            {
                RowColValue = SelectedRow + ";" + _selectedColumn + ";" + EncodeType(_selectedType);
                RaisePropertyChanged("RowColValue");
            }

        }
 
        private void TreeView_Loaded(object sender, RoutedEventArgs e)
	    {

            List<T_OBJECT_CHILDDTO> parentList = GetChildren(-1);
            var tree = sender as TreeView;

            foreach (T_OBJECT_CHILDDTO obj in parentList)
            {
                TreeViewItem item = CreateNewItem(obj, "");
                tree.Items.Add(item);
                AddChildren(item, obj.CHILD_ID);
            }
	    }

        private void AddChildren(TreeViewItem parentItem, long id)
        {
            List<T_OBJECT_CHILDDTO> parentList = GetChildren(id);

            TreeViewItemContext parentDataContext = (TreeViewItemContext)parentItem.DataContext;
            foreach (T_OBJECT_CHILDDTO obj in parentList)
            {
                
                TreeViewItem item = CreateNewItem(obj, parentDataContext.path);
                parentItem.Items.Add(item);
                if (SelectedColumn.Equals(obj.INTERNAL_EXPRESSION))
                {
                    parentItem.IsExpanded = true;
                }
                AddChildren(item, obj.CHILD_ID);
            }
        }


        
        private TreeViewItem CreateNewItem(T_OBJECT_CHILDDTO obj, string parentPath)
        {
            TreeViewItem item = new TreeViewItem();
            item.Header = obj.OBJ_CHLD_HAPPYNAME;
            //item.DataContext = obj;

            string separator = ";";
            if (parentPath.Length == 0)
                separator = "";

            string itemPath;
          
            if (obj.INTERNAL_EXPRESSION != null)
            {
                itemPath = parentPath + separator + obj.INTERNAL_EXPRESSION;
                item.ToolTip = obj.INTERNAL_EXPRESSION + "\n" + itemPath;
            }

            else
            {
                itemPath = parentPath + separator + obj.OBJ_CHLD_HAPPYNAME;
                item.ToolTip = obj.OBJ_CHLD_HAPPYNAME + "\n" + itemPath;
            }

            item.DataContext = new TreeViewItemContext(itemPath, obj);
            
            if (SelectedColumn.Equals(obj.INTERNAL_EXPRESSION))
            {
                item.IsSelected = true;
                item.IsExpanded = true;
            }
            return item;
        }


        List<T_OBJECT_CHILDDTO> GetChildren(long id)
        {
            List<T_OBJECT_CHILDDTO> childList;

            childList = (from o in objectChildDtoList
                         where o.PARENT_CHILD_ID == id
                         select o).ToList();

            return childList;
        }

        

	    private void TreeView_SelectedItemChanged(object sender,
	        RoutedPropertyChangedEventArgs<object> e)
	    {
	        var tree = sender as TreeView;

	        // ... Determine type of SelectedItem.
	        if (tree.SelectedItem is TreeViewItem)
	        {
		        // ... Handle a TreeViewItem.
		        var item = tree.SelectedItem as TreeViewItem;
		        this.Title = "Selected header: " + item.Header.ToString();
                //var childObject =  ((TreeViewItemContext) item.DataContext).data;
                 SelectedColumn =  ((TreeViewItemContext) item.DataContext).path;

                /*
                if (childObject.INTERNAL_EXPRESSION != null)
                    SelectedColumn = childObject.INTERNAL_EXPRESSION;
                    
                else
                    SelectedColumn = childObject.OBJ_CHLD_HAPPYNAME;
                 * */
	        }
	        else if (tree.SelectedItem is string)
	        {
		    // ... Handle a string.
		    this.Title = "Selected: " + tree.SelectedItem.ToString();
	        }
	    }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        //private double _rowValue=5.0;

        private double _rowValue = 0;
        public double RowValue
        {
            get { return _rowValue; }
            set 
            { 
                _rowValue = value; 
                RaisePropertyChanged("RowValue");
                this.SelectedRow = "" + RowValue;
                UpdateResultValue();
            }
        }

        public string Answer { get; set; }

        public string _selectedRow { get; set; }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)e.Source;
            string text = tb.Text;
            Console.WriteLine(text);
        }

        private void rbRowNumber_Click(object sender, RoutedEventArgs e)
        {
            this.RowMode = "RowNumber";
            this.SelectedRow = "" + RowValue;
            UpdateResultValue();
            this.spinButton.IsEnabled = true;
        }

        private void rbLastRow_Click(object sender, RoutedEventArgs e)
        {
            this.RowMode = "LastRow";
            this.SelectedRow = "DYNAMICROW";
            UpdateResultValue();
            this.spinButton.IsEnabled = false;
        }

        public string RowMode { get; set; }
    }

    public class TreeViewItemContext
    {

        public TreeViewItemContext(string path, T_OBJECT_CHILDDTO data)
        {
            this.path = path;
            this.data = data;
        }
        public string path;
        public T_OBJECT_CHILDDTO data;

    }
}
