using Mars.ViewModel.xml;
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

namespace Mars.Views.xml
{
    /// <summary>
    /// Interaction logic for XmlProjImportControlxaml.xaml
    /// </summary>
    public partial class XmlProjImportControlxaml : MarsBaseViewControl
    {
        
        public XmlProjImportControlxaml()
        {
            InitializeComponent();
            this.DataContext = new XmlProjImportDataModel(this.LoadInformationLogList);
            Title = "Import Project";
        }
    }
}
