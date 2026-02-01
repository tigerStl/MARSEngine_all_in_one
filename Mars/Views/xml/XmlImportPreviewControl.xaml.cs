using Mars.ViewModel.xml;
using Mars.Views.baseView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Mars.Views.xml
{
    /// <summary>
    /// Interaction logic for XmlImportPreviewControl.xaml
    /// </summary>
    public partial class XmlImportPreviewControl :
        MarsBaseViewControl
    {
        public XmlImportPreviewControl()
        {
            InitializeComponent();
            this.DataContext = new XmlImportPreviewModel();
            Title = "Xml Imp/Exp";
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
             
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //MainTab.TabIndex = 2;
            //MainTab.SelectedIndex = 2;
            Thread.Sleep(200);
            
        }
    }
}
