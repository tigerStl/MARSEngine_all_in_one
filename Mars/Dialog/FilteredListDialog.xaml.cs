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
    /// Interaction logic for FilteredListDialog.xaml
    /// </summary>
    public partial class FilteredListDialog : Window
    {
        private List<string> testEnums;
        private TextBox _targetTextBox;
        private static FilteredListDialog instance;

        public FilteredListDialog(List<string> testEnums, TextBox targetTextBox)
        {
            InitializeComponent();
            _targetTextBox = targetTextBox;
            string[] wordList = testEnums.ToArray();

            //this.DataContext = testEnums;

            //this.lstBox.DataContext = testEnums;
            this.lstBox.ItemsSource = wordList;
            ICollectionView view =
                CollectionViewSource.GetDefaultView(wordList);

            new TextSearchFilter(view, this.txtSearch);
            this.testEnums = testEnums;
        }

        private void lstBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _targetTextBox.Text = ((ListBox)sender).SelectedItems[0].ToString();

            this.Hide();
        }

        internal static FilteredListDialog GetInstance(List<string> testEnums, TextBox txtEnumType)
        {
            if (instance == null)
                instance = new FilteredListDialog(testEnums, txtEnumType);

            return instance;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true; ;
        }
    }
}
