using Mars.ViewModel;
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

namespace Mars.Views.objectManagement
{
    /// <summary>
    /// Interaction logic for ObjectDataSourceDefinition.xaml
    /// </summary>
    public partial class ObjectDataSourceDefinition : UserControl
    {
        public ObjectDataSourceDefinition()
        {
            InitializeComponent();
        }

        public const string CNST_HEADER_FRIENDLYNAME = "Parameter Friendly Name";

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column == null || e.Column.Header == null) return;
            string strHeader = e.Column.Header.ToString();
            
            if ((string.Compare(strHeader, CNST_HEADER_FRIENDLYNAME, true)==0) && (e.EditingElement is TextBox))
            {
                if (string.IsNullOrEmpty(((TextBox)e.EditingElement).Text))
                {
                    ViewModelBase.HintByMessageBox(string.Format("[{0}] is used to hint when fetching data from DB.\r\nIt can't be empty.", CNST_HEADER_FRIENDLYNAME));
                    e.Cancel = true;
                    return;
                }
            }
        }
    }
}
