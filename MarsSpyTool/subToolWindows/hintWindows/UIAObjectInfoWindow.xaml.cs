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

namespace MarsSpyTool.subToolWindows.hintWindows
{
    /// <summary>
    /// Interaction logic for UIAObjectInfoWindow.xaml
    /// </summary>
    public partial class UIAObjectInfoWindow : Window
    {
        private bool isActivated = false;

        public UIAObjectInfoWindow()
        {
            InitializeComponent();
        }

        public void SetObjectInfo(string info)
        {
            objectInfoTextBlock.Text = info;
        }

        public void ActivateWindow()
        {
            //isActivated = true;
            //Topmost = true;
            mainBorder.Opacity = 1.0;
            mainBorder.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }

        public bool IsActivated
        {
            get { return isActivated; }
        }

        public string GetObjectInfo()
        {
            return objectInfoTextBlock.Text;
        }
    }
}

