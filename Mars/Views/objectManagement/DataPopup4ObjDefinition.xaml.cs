using Mars.Views.baseView;
using Mars.Views.systemTools;
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
    /// Interaction logic for DataPopup4ObjDefinition.xaml
    /// </summary>
    public partial class DataPopup4ObjDefinition : MarsBaseViewControl
    {
        public DataPopup4ObjDefinition()
        {
            InitializeComponent();
            this.DataContext = new DataPopup4ObjDefinitionDataModal();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DatabaseConnectionContainer == null) return;
            switch(((RadioButton)sender).Tag.ToString().ToUpper())
            {
                case "O":
                case "S":
                case "B":
                    this.DatabaseConnectionContainer.Children.Clear();
                    this.DatabaseConnectionContainer.Children.Add(new OracleConnectionSettings());
                    return;
            }
        }

        private void MarsBaseViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DatabaseConnectionContainer == null) return;
            rdbtnOrale.IsChecked = true;
        }
    }
}
