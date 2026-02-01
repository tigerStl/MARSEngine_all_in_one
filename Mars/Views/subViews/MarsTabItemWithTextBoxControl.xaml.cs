using Route2NSEx.src.Marquis.systemUtil;
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

namespace Mars.Views.subViews
{

    
    /// <summary>
    /// Interaction logic for MarsTabItemWithTextBoxControl.xaml
    /// </summary>
    public partial class MarsTabItemWithTextBoxControl : UserControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsTabItemWithTextBoxControl));
        public MarsTabItemWithTextBoxControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("UserControl_Loaded",string.Format("Parent's width:[{0}]", this.Parent==null?10:((Control)this.Parent).Width));
        }

        private void _hostGrid_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("_hostGrid_Loaded", string.Format("Parent's width:[{0}]", this.Parent == null ? 10 : ((Control)this.Parent).Width));

        }

        private void UserControl_LayoutUpdated(object sender, EventArgs e)
        {
            Logger.Info("UserControl_LayoutUpdated", string.Format("Parent's width:[{0}]", this.Parent == null ? 10 : ((Control)this.Parent).Width));
        }
    }
}
