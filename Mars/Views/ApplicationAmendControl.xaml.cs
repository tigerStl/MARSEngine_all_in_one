using Mars.ViewModel;
using Mars.Views.baseView;
using System;
using System.Collections;
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
    /// Interaction logic for ApplicationAddControl.xaml
    /// </summary>
    public partial class ApplicationAmendControl :
        MarsBaseViewControl
    {
        public ApplicationAmendControl(string strMode= "Amend Application")
        {
            InitializeComponent();
            this.DataContext = new ApplicationViewModel();

            Title = strMode;
        }

        private void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void lstBoxExtraReq_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedItemsString = "";
            ListBox lb = (ListBox)sender;
           IList items = lb.SelectedItems;
           foreach (var item in items)
           {
               selectedItemsString += item.ToString() + ";" ;
           }

           ((ApplicationViewModel)DataContext).ExtraRequirement = selectedItemsString;
            
        }

        private void txtExtraPopupMenu_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
