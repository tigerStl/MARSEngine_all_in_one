using Mars.Views.baseView;
using Route2NSEx.src.Marquis.systemUtil;
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

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for ProjectTabControl.xaml
    /// </summary>    
    public partial class ProjectTabControl : MarsBaseViewControl, INotifyPropertyChanged
    {
        public ProjectTabControl()
        {
            InitializeComponent();
        }
        #region tiger added
        private static MLogger Logger = MLogger.GetLogger(typeof(ProjectTabControl));
        private void OnSelectionTabChanged(object sender, SelectionChangedEventArgs e)
        {
            
            Logger.logBegin("OnSelectionTabChanged");
            TabControl objDes = (TabControl)sender;
            if (objDes.SelectedItem==null)
            {
                return;
            }
            
            if (((TabControl)sender).SelectedIndex!=0)
            {
                /// deal with Storyboard information
                /// 1, get Actived storyboard ID
                /// 
                
            }
            Logger.logEnd("OnSelectionTabChanged");
        }

        private long currentStoryBoarId = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        public long CurrentStoryboardId
        {
            get { return currentStoryBoarId; }
            set { if (currentStoryBoarId == value) return;
                currentStoryBoarId = value;
                
            }
        }
        

        #endregion  //tiger added
    }
}
