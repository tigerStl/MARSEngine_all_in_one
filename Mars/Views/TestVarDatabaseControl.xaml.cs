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
    /// Interaction logic for TestVarDatabaseControl.xaml
    /// </summary>
    public partial class TestVarDatabaseControl :
        MarsBaseViewControl
    {
        public TestVarDatabaseControl()
        {
            InitializeComponent();
            this.DataContext = new TestVarDatabaseViewModel();
            Title = "System Variables";
        }

        private void txtFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            
            TestVarDatabaseViewModel vm = (TestVarDatabaseViewModel)this.DataContext;
            var source = CollectionViewSource.GetDefaultView(vm.SystemLookup);
            vm.nameSearchString = txtFilter.Text;
            //source.Filter = vm.UserFilterByName;
            source.Filter = vm.UserFilterByNameAndType;
            
            source.Refresh();
            

        }

        //private void typeFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        //{
        //    TestVarDatabaseViewModel vm = (TestVarDatabaseViewModel)this.DataContext;
            
        //    var source = CollectionViewSource.GetDefaultView(vm.SystemLookup);
        //    if (sender is ComboBox)
        //    {
        //        string strCurrentFilter = ((ComboBox)sender).Name;
        //        if (string.Compare(strCurrentFilter, "All", true) == 0)
        //            vm.typeSearchString = "";
        //        else
        //            vm.typeSearchString = strCurrentFilter;
        //    }
        //        //vm.typeSearchString = txtTypeFilter.Text;
        //    //source.Filter = vm.UserFilterByType;
        //    source.Filter = vm.UserFilterByNameAndType;
        //    source.Refresh();
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TestVarDatabaseViewModel vm = (TestVarDatabaseViewModel)this.DataContext;
            vm.InitLookupList();
        }

        private void typeFilter_TextChanged(object sender, SelectionChangedEventArgs e)
        {
            TestVarDatabaseViewModel vm = (TestVarDatabaseViewModel)this.DataContext;

            var source = CollectionViewSource.GetDefaultView(vm.SystemLookup);
            if (sender is ComboBox)
            {
                string strCurrentFilter = ((ComboBoxItem)(((ComboBox)sender).SelectedItem)).Name;
                if (string.Compare(strCurrentFilter, "All", true) == 0)
                    vm.typeSearchString = "";
                else
                    vm.typeSearchString = strCurrentFilter;
            }
            //vm.typeSearchString = txtTypeFilter.Text;
            //source.Filter = vm.UserFilterByType;
            source.Filter = vm.UserFilterByNameAndType;
            source.Refresh();
        }
    }
}
