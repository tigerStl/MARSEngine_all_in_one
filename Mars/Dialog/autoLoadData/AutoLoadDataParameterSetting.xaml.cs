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
using System.Windows.Shapes;

namespace Mars.Dialog.autoLoadData
{
    /// <summary>
    /// Interaction logic for AutoLoadDataParameterSetting.xaml
    /// </summary>
    public partial class AutoLoadDataParameterSetting : Window
    {
        public AutoLoadDataParameterSetting()
        {
            InitializeComponent();
        }

        
        private void Button_OkClick(object sender, RoutedEventArgs e)
        {
            if ((this.DataContext==null)||(!(this.DataContext is AutoLoadDataParameterSettingModal)))
            {
                this.DialogResult = false;
                this.Close();
                return;
            }
            string strError = "";
            bool isOk = ((AutoLoadDataParameterSettingModal)this.DataContext).OkBtnClickCommand(ref strError);
            if (!isOk)
            {
                ViewModelBase.HintByMessageBox(string.Format("Error when check parameters:\r\n[{0}]",strError),"ERROR");
                this.DialogResult = false;
            }
            
            this.DialogResult = true;
            Close();
        }
    }
}
