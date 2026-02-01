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
    /// Interaction logic for KeywordLibrary.xaml
    /// </summary>
    public partial class KeywordLibrary :
        MarsBaseViewControl
    {
        public KeywordLibrary()
        {
            InitializeComponent();
            this.DataContext = new KeywordLibraryViewModel(MarsMainWindow.CurrentDatabaseIdx);
            Title = "Keyword Management";
        }
    }
}
