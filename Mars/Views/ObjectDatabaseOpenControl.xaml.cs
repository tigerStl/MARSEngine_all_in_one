using Mars.ViewModel;
using Mars.Views.baseView;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for ObjectDatabaseOpenControl.xaml
    /// </summary>
    public partial class ObjectDatabaseOpenControl :
        MarsBaseViewControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ObjectDatabaseOpenControl));
        public ObjectDatabaseOpenControl()
        {
            InitializeComponent();
            this.DataContext = new ObjectDatabaseViewModel(MarsMainWindow.CurrentDatabaseIdx);
            Title = "Object Management";
        }

        private void txtFilter_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ObjectDatabaseViewModel vm = (ObjectDatabaseViewModel)this.DataContext;
            var source = CollectionViewSource.GetDefaultView(vm.RegisterdObject);
            vm.searchString = txtFilter.Text;
            source.Filter = vm.UserFilter;
            source.Refresh();
        }

        private void MarsBaseViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            objectEdit.ObjectListIsChangedHandle = ((ObjectDatabaseViewModel)this.DataContext).objectListChangeImplement;
        }
        #region object spy part
        private Cursor FinderCursor = null;
        private Cursor CurrentOldCursor = null;
        private bool gIsMouse_down = false;
        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Logger.logBegin("RibbonButton_PreviewMouseDown");
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                Logger.Info("RibbonButton_PreviewMouseDown", "not left button");
                return;
            }
            CaptureMouse();
            if (FinderCursor == null)
                InitFinderCursor();
            //Mouse.SetCursor(FinderCursor);
            Cursor = FinderCursor;
            gIsMouse_down = true;
            e.Handled = true;
        }
        private void InitFinderCursor()
        {
            Logger.logBegin("InitFinderCursor");
            FinderCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Mars.Resources.CrosshairsCursor.cur"));
        }
        #endregion

        private void listViewObjects_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (DataContext as ObjectDatabaseViewModel).AddToBatchConvert.Execute(null);
        }

        private void ObjectsForBatchConvert_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (DataContext as ObjectDatabaseViewModel).RemoveFromBatchConvertListCommand.Execute(null); 
        }
    }


}
