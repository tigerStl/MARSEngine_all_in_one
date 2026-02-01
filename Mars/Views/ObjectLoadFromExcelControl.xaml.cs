using Mars.ViewModel;
using Mars.Views.baseView;
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

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for ObjectLoadFromExcel.xaml
    /// </summary>
    public partial class ObjectLoadFromExcel :
        MarsBaseViewControl
    {
        public ObjectLoadFromExcel()
        {
            InitializeComponent();
            this.DataContext = new ObjectDatabaseViewModel(MarsMainWindow.CurrentDatabaseIdx);
            Title = "Load object From Excel";
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();


            dlg.Title = "Select Dictionary Excel File";
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xls";
            dlg.Filter = "Excel Files (*.xls)|*.xls";


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
    }
}
