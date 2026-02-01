using Mars.DataLayer;
using Mars.Model;
using Mars.ViewModel;
using MarsTestFrame.CommuniteServer;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.EntityClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using System.Windows.Threading;

namespace Mars
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(App));

        public ILicenseMgr gLicenseMgr { get; set; }
        public bool FlashIsLoaded { get; internal set; }

        private Window mobjSplashScreen = null;
        private MarsMainWindow mobjMain = null;
        public void CloseSplashWindow()
        {
#if Login_defined
            mobjSplashScreen.Close();
#endif
        }

        public void CreateMarsMain()
        {
            mobjMain = new MarsMainWindow();
            //mobjSplashScreen.Close();
            try
            {
                mobjMain.Show();
                App.Current.MainWindow = mobjMain;
            }
            catch (Exception e)
            {

                Console.WriteLine(string.Format("exception:[{0}] stackTrace:[{1}]", e.Message,e.StackTrace));
            }
            
        }

        protected void InstanceCheck()
        {
            string strProcName = System.AppDomain.CurrentDomain.FriendlyName;
            strProcName = Path.GetFileNameWithoutExtension(strProcName);
            Process[] arrPro = Process.GetProcessesByName(strProcName);
            if (arrPro == null) return;
            if (arrPro.Length <= 1) return;
            Process pcur = Process.GetCurrentProcess();
            foreach (var p in arrPro)
            {
                if (p.Id == pcur.Id) continue;
                try
                {
                    TigerMarsUtil.ShowWindow(p.MainWindowHandle, TigerMarsUtil.SW_RESTORE);
                    Application.Current.Shutdown();
                }
                catch (Exception e)
                {
                    Logger.Error("InstanceCheck", string.Format("Exception:[{0}]",e.Message),e);
                    ViewModelBase.HintByMessageBox("There are more than one MARS running, Only one instance is allowd.");
                }
            }
        }

       
        private bool InitSchemaChangingAndDBConnection()
        {
            //EntityConnectionStringBuilder dd = new EntityConnectionStringBuilder("metadata=res://*/Model.MarsModel.csdl|res://*/Model.MarsModel.ssdl|res://*/Model.MarsModel.msl;provider=Oracle.ManagedDataAccess.Client;provider connection string=';DATA SOURCE=TESTIDELOCAL;PASSWORD=TESTMARS;USER ID=TESTMARS';");
            //EntityConnection objCnn = MarsEntitiesExtends.createConnection("TESTMARS", dd, "Model.MarsModel");
            //MarsEntities objDBCntx = BoHelper.GetMarsEntitiesInstance(objCnn);
            //try
            //{
            //    var user = (from q in objDBCntx.V_STORYBOARD_TEST_FULLVISION
            //                where q.PROJECT_NAME == "FHLBC Debt Bonds"
            //                select q).FirstOrDefault();
            //}
            //catch (Exception ex)
            //{
            //    Logger.Error("OnStartUp",string.Format("Exceptions:[{0}]",ex.Message),ex);
            //}
            try
            {
                Logger.logBegin("InitSchemaChangingAndDBConnection");
                MarsEntitiesExtends.InitDBInfo(ConfigurationManager.AppSettings,
                    ConfigurationManager.ConnectionStrings);

                MarsEntitiesExtends.NewSchemaName = AppConfigReader.GetDefaultSchemaForOracle();
                Logger.Info("InitSchemaChangingAndDBConnection", string.Format("Get schema Name:[{0}]", MarsEntitiesExtends.NewSchemaName));
                ///get Connection string from configuration file
                /// 
                string strPassword = MarsMainWindow.DbPasswordDecoded;
                string strConnString = MarsMainWindow.MarsEntiesConnString;
                if (string.IsNullOrEmpty(strConnString)) return false;

                strConnString = string.Format(strConnString, strPassword);
                MarsEntitiesExtends.connectionBuilder = new EntityConnectionStringBuilder(strConnString);
                Logger.Info("InitSchemaChangingAndDBConnection",  string.Format("Connection string is :[{0}]", strConnString));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InitSchemaChangingAndDBConnection",string.Format("exception:[{0}]", e.Message));
                return false;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            #region //test code here

            //MarsTestFrameCommuniteServer objServer = new MarsTestFrameCommuniteServer();
            //var data = objServer.GetOpicsMessageAndTypeList();
            #endregion



#if remoteDebugger
            MessageBox.Show("wait for debugger connection");
#endif

            //InstanceCheck();
            try
            {

            
            InitSchemaChangingAndDBConnection();

#if Login_defined
            Mars.Views.login.MarsLoginMain logWin = new Mars.Views.login.MarsLoginMain();
            this.MainWindow = logWin;
            if (!(logWin.ShowDialog() ?? false))
            {
                Application.Current.Shutdown();
                return;
            }


            //testcode();
            if (mobjSplashScreen == null)
            {
                mobjSplashScreen = new Mars.Views.login.MARSFlash();
            }
            
            //mobjSplashScreen.Show();
            //Thread thrd = new Thread(delegate () { mobjSplashScreen.Show(false, true); });
            FlashIsLoaded = false;
            Thread thrd = new Thread(delegate () {
                MarsDBGlobe_Cache.default_cache_app = AppConfigReader.GetDefaultCacheAppsObjs();
                //MarsDBGlobe_Cache.InitCache();
            });

            mobjSplashScreen.Activate();
            mobjSplashScreen.WindowState = WindowState.Normal;
            mobjSplashScreen.Show();
            mobjSplashScreen.UpdateLayout();

            Dispatcher.CurrentDispatcher.Invoke(delegate () {
                try {
                    gLicenseMgr = TestFrameLicense.LoadLicense();
                    
                   
                    //Thread thrd = new Thread(delegate () { (new MainWindow()).Show(); }); 
                    
                    thrd.Start();
                    //thrd.Join();
                    
                    mobjSplashScreen.InvalidateVisual();
                    ((App)(App.Current)).CreateMarsMain();
                }
                catch(Exception ex)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Exception:[{0}]\r\nInner:[{1}]", ex.Message, ex.InnerException==null?"N/A": ex.InnerException.Message));
                    return;
                }
            },DispatcherPriority.Background);
            //Current.MainWindow = mobjSplashScreen;
            while (!FlashIsLoaded)
                Thread.Sleep(150);
#else

#endif
            Logger.Info("APP", "call base wpf startup");
            base.OnStartup(e);
            Logger.Info("APP", "call base wpf startup end");
            try
            {
                if (e.Args.Length > 0)
                {
                    if (String.Compare(e.Args[0], "attach", true) == 0)
                    {
                        Thread.Sleep(6000);
                    }
                }
                

                return;
            }
            catch (Exception ex)
            {
                string strError = "";
                //Console.WriteLine("[{0}] [{1}]", ex.Message, ex.StackTrace);
                MessageBox.Show(strError = string.Format("Exception:[{0}]\r\n stackTrace:[{1}]\r\n InnerException:[{2}]\r\nInnerStackTrace:[{3}]",
                    ex.Message,
                    ex.StackTrace,
                    ex.InnerException == null ? "N/A" : ex.InnerException.Message,
                    ex.InnerException == null ? "N/A" : ex.InnerException.StackTrace
                    ));
                Logger.Error("APP",strError);
                //Application.Current.Shutdown();
            }
            finally
            {
#if !Login_defined
                CreateMarsMain();
#endif
            }
            }
            catch (Exception ee)
            {
                Logger.Error("\t", ee.Message, ee);
            }

        }

        private void testcode()
        {
            string strExp = "Customer   code".Replace(" ", @"\s");
            if (TigerMarsUtil.RegularTest(strExp, "Customer   code"))
            {
                MessageBox.Show("OK");
            }
        }

        public Window GetMainWindow()
        {
            return this.MainWindow;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Console.WriteLine("Tiger Mars starts");
            
            //MessageBox.Show(string.Format("{0}",e.Args.Length));
            
        }

        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {

        }
    }
}
