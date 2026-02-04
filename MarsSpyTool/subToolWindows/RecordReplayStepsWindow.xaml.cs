using MarsSpyTool.subToolWindows.viewModal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MarsSpyTool.subToolWindows
{

    public class MarsIndexConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var dataGrid = values[1] as System.Windows.Controls.DataGrid;
            var item = values[0];

            if (dataGrid != null && item != null)
            {
                var index = dataGrid.Items.IndexOf(item);
                return index + 1; // Convert to 1-based index
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Interaction logic for RecordReplayStepsWindow.xaml
    /// </summary>
    public partial class RecordReplayStepsWindow : Window
    {
        [DllImport("shell32.dll", SetLastError = true)]
        public static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
        public const int default_windows_height_forUpAndDown = 130;
        public const int default_windows_widht_forLeftAndRight = 140;
        public enum AppBarMessages : uint
        {
            New = 0x00000000,
            Remove = 0x00000001,
            QueryPos = 0x00000002,
            SetPos = 0x00000003,
            GetState = 0x00000004,
            GetTaskbarPos = 0x00000005,
            Activate = 0x00000006,
            GetAutoHideBar = 0x00000007,
            SetAutoHideBar = 0x00000008,
            WindowPosChanged = 0x00000009,
            SetState = 0x0000000A
        }

        public enum AppBarEdges : uint
        {
            Left = 0,
            Top = 1,
            Right = 2,
            Bottom = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public AppBarEdges uEdge;
            public RECT rc;
            public int lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left, top, right, bottom;
        }

        private static Rect default_original_workArea = default(Rect);
        public static Rect Default_Original_WorkArea
        {
            get
            {
                if (default_original_workArea.Equals(default(Rect)))
                {
                    default_original_workArea = SystemParameters.WorkArea;
                }
                return default_original_workArea;
            }            
        }

        private const int ABM_NEW = 0x00000000;
        private const int ABM_REMOVE = 0x00000001;
        private const int ABM_QUERYPOS = 0x00000002;
        private const int ABM_SETPOS = 0x00000003;


        private static RecordReplayStepsWindow gInstatnce = null;
        private static Window marsMainWindow = null;

        //private Popup dockMenu;
        private MenuItem bottomMenuItem;

        public static void showRecordStepList(Window prnt)
        {
            marsMainWindow = prnt;
            if (gInstatnce != null)
            {
                gInstatnce.Close();
                gInstatnce = null;
            }
            gInstatnce = new RecordReplayStepsWindow();
            gInstatnce.Visibility = Visibility.Visible;
            //gInstatnce.Show();
        }
        private void AppbarWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RegisterAppBar();
            SetAppBarPosition(AppBarEdges.Bottom);
        }

        private void AppbarWindow_Closed(object sender, EventArgs e)
        {
            UnregisterAppBar();
        }

        private void RegisterAppBar()
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(abd);
            abd.hWnd = new WindowInteropHelper(this).Handle;
            SHAppBarMessage(ABM_NEW, ref abd);
        }
        private void UnregisterAppBar()
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(abd);
            abd.hWnd = new WindowInteropHelper(this).Handle;
            SHAppBarMessage(ABM_REMOVE, ref abd);
        }

        private void SetAppBarPosition(AppBarEdges edge)
        {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = (uint)Marshal.SizeOf(abd);
            abd.hWnd = new WindowInteropHelper(this).Handle;
            abd.uEdge = edge;

            if (edge == AppBarEdges.Left || edge == AppBarEdges.Right)
            {
                Width = default_windows_widht_forLeftAndRight;
                Height = Default_Original_WorkArea.Height;
            }
            else
            {
                Height = default_windows_height_forUpAndDown;
                Width = Default_Original_WorkArea.Width;
            }

            SHAppBarMessage(ABM_QUERYPOS, ref abd);

            if (edge == AppBarEdges.Left || edge == AppBarEdges.Right)
            {
                abd.rc.top = 0;
                abd.rc.bottom = (int)Default_Original_WorkArea.Height;

                if (edge == AppBarEdges.Left)
                {
                    abd.rc.left = 0;
                    abd.rc.right = default_windows_widht_forLeftAndRight;
                }
                else
                {
                    abd.rc.right = (int)Default_Original_WorkArea.Width;
                    abd.rc.left = (int)Default_Original_WorkArea.Width-default_windows_widht_forLeftAndRight;
                }
            }
            else
            {
                abd.rc.left = 0;
                abd.rc.right = (int)Default_Original_WorkArea.Width;

                if (edge == AppBarEdges.Top)
                {
                    abd.rc.top = 0;
                    abd.rc.bottom = default_windows_height_forUpAndDown;
                }
                else
                {
                    abd.rc.bottom = (int)Default_Original_WorkArea.Height;
                    abd.rc.top = (int)Default_Original_WorkArea.Height - (int)default_windows_height_forUpAndDown;
                }
            }

            SHAppBarMessage(ABM_SETPOS, ref abd);

            Left = abd.rc.left;
            Top = abd.rc.top;
            Width = abd.rc.right - abd.rc.left;
            Height = abd.rc.bottom - abd.rc.top;
            Height = Height < default_windows_height_forUpAndDown ? default_windows_height_forUpAndDown : Height;
        }

        protected RecordReplayStepsWindow()
        {
            InitializeComponent();
            Loaded += AppbarWindow_Loaded;
            Closed += AppbarWindow_Closed;

            DataContext = new RecordReplayStepsWinModal();
            ((RecordReplayStepsWinModal)DataContext).SetTestStepExecuteStatusColor = ChangeRowBackgroundByItem;
            ((RecordReplayStepsWinModal)DataContext).TestSteps.CollectionChanged += (s, e) => ScrollLatestToViewPoint();
        }

        private void ScrollLatestToViewPoint()
        {
            var items = ((RecordReplayStepsWinModal)DataContext).TestSteps;
            if (items.Count > 0)
            {
                var lastItem = items[items.Count - 1];  
                this.TestStepDataGrid.ScrollIntoView(lastItem);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to MenuItem to access the Tag property
            MenuItem menuItem = sender as MenuItem;

            if (menuItem != null)
            {
                // Retrieve the Tag value, which is the string you want to pass
                string dockPosition = menuItem.Tag as string;

                // You can call the DockTo method and pass this parameter if needed
                switch (dockPosition)
                {
                    case "Top":
                        SetAppBarPosition(AppBarEdges.Top);
                        break;
                    case "Left":
                        SetAppBarPosition(AppBarEdges.Left);
                        break;
                    case "Right":
                        SetAppBarPosition(AppBarEdges.Right);
                        break;
                    case "Bottom":
                        SetAppBarPosition(AppBarEdges.Bottom);
                        break;
                }
            }
        }

        public void ChangeRowBackgroundByItem(object item, Brush color)
        {
            var row = (DataGridRow)this.TestStepDataGrid.ItemContainerGenerator.ContainerFromItem(item);
            if (row != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    row.Background = color;
                });
            }
        }

        private void DockToPosition(string position)
        {
            // Uncheck all items
            foreach (System.Windows.Controls.MenuItem item in ((Menu)DockMenuPopup.Child).Items)
            {
                item.IsChecked = false;
            }

            // Check the selected item
            MenuItem selectedItem = ((Menu)DockMenuPopup.Child).Items[position == "Top" ? 0 :
                                                             position == "Left" ? 1 :
                                                             position == "Right" ? 2 : 3] as MenuItem;
            selectedItem.IsChecked = true;

            // Dock logic
            switch (position)
            {
                case "Top":
                    SetAppBarPosition(AppBarEdges.Top);
                    break;
                case "Left":
                    SetAppBarPosition(AppBarEdges.Left);
                    break;
                case "Right":
                    SetAppBarPosition(AppBarEdges.Right);
                    break;
                case "Bottom":
                    SetAppBarPosition(AppBarEdges.Bottom);
                    break;
            }
        }

        private void OnStepwindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (marsMainWindow != null)
                marsMainWindow.WindowState = WindowState.Normal;
        }

        private void DockButton_Click(object sender, RoutedEventArgs e)
        {
            DockMenuPopup.IsOpen = true;
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
            gInstatnce=null;
        }
    }
}
