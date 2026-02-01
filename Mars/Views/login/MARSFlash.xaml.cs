using Mars.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Cache;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Mars.Views.login
{
    /// <summary>
    /// Interaction logic for MARSFlash.xaml
    /// </summary>
    public partial class MARSFlash : Window
    {
        public MARSFlash()
        {
            ///Get database password and decoded
            /// 
            
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //imgMain.BeginInit();
            //BitmapImage _image = new BitmapImage();
            //_image.BeginInit();
            //_image.CacheOption = BitmapCacheOption.None;
            //_image.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            //_image.CacheOption = BitmapCacheOption.OnLoad;
            //_image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            //_image.UriSource = new Uri(@"pack://application:,,,/Images/Mars.png", UriKind.RelativeOrAbsolute);
            //_image.EndInit();
            //imgMain.Source = _image;

            //imgMain.Focus();
            //imgMain.Uid = "/Mars;component/Images/Mars.png";
            //imgMain.EndInit();
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            ((App)(App.Current)).FlashIsLoaded = true;
            //MarsUtilities.StartQTPStarer("/StartQTPBackGround");
#if !_TestStepUnit
            MarsUtilities.StartQTPStarer("/StartQTPBackGround");
            //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            //{
            //    (new Thread(new ThreadStart(delegate ()
            //    {
            //        ProcessStartInfo StartInfo = new ProcessStartInfo();
            //        StartInfo.FileName = @".\QtpStarter.exe";
            //        StartInfo.Arguments = "/StartQTPBackGround";
            //        StartInfo.Domain = "NewMarsDomain";
            //        Process objNewProce = new Process();
            //        //Logger.Info("StartTest", string.Format("anonymous Thread start begin"));
            //        objNewProce.StartInfo = StartInfo;
            //        objNewProce.Start();
            //        //Logger.Info("StartTest", string.Format("anonymous Thread started, with process Id:[{0}]", objNewProce.Id));
            //    }))).Start();

            

            //}));
#endif
            
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
