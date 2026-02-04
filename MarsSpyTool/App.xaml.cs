using Mars.message.Utility;
using MarsSpyTool.objectFileMonitor;
using MarsSpyTool.Utility;
using NLog;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace MarsSpyTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger logger = NLog.LogManager.GetLogger("MarsSpyLog");
        /// <summary>
        /// parameter 
        /// </summary>
        /// <param name="sender">
        /// var p = Process.Start(strPath, $"-mode spyObject -uuid {strUUID}");
        /// var p = Process.Start(strPath, $"-mode recTestcase -uuid {strUUID}");
        /// </param>
        /// <param name="e"></param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string strP = string.Join("|", e.Args);
            try
            {
                string logDir = System.IO.Path.GetDirectoryName(typeof(App).Assembly.Location);

                logger.Info($"args from outside [{strP}]");
                MarsObjectSpy wnd = new MarsObjectSpy();
                Dictionary<string, string> paras = new Dictionary<string, string>();
                SpyToolParaInfo.currentToolParaInfo.Clear();
                logger.Info($"SpyToolParaInfo.currentToolParaInfo [{strP}]");
                string strError = "";
                if (e.Args.Length >= 1)
                {

                    logger.Info($"SpyToolParaInfo.e.Args.Length [{e.Args.Length}]");

                    /// put para pair to dictionary
                    /// 
                    for (int i = 0; i < (e.Args.Length / 2); i++)
                    {
                        string k = e.Args[i * 2];
                        string v = e.Args[i * 2 + 1];
                        SpyToolParaInfo.currentToolParaInfo.Add(k, v);
                    }
                    string uuId = SpyToolParaInfo.currentToolParaInfo[MarsConstants.CNST_SYPTOOL_PARA_UUID];
                    if (string.IsNullOrEmpty(uuId))
                    {
                        MessageBox.Show(strError = "No UUID is passed.");
                        logger.Error(strError);
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                    MarsGlobalData.currentUUIDFromWeb = uuId;
                    string strMode = SpyToolParaInfo.currentToolParaInfo[MarsConstants.CNST_SPYTOOL_PARA_MODE];
                    if (string.IsNullOrEmpty(strMode))
                    {
                        MessageBox.Show(strError = "No mode parameter is passed.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.Error(strError);
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }

                    bool isRecTC = false;
                    if (
                        !((isRecTC = (string.Compare(strMode, MarsConstants.CNST_SPYTOOL_PARA_MODE_RECTC, true) == 0))
                        || (string.Compare(strMode, MarsConstants.CNST_QUERY_URL_OBJREC, true) == 0)
                        || (string.Compare(strMode, MarsConstants.CNST_QUERY_URL_RECORDREPLAY, true) == 0)
                        )
                    )
                    {
                        MessageBox.Show(strError = $"No mode value|{strMode}|, parameter should be in ({MarsConstants.CNST_QUERY_URL_OBJREC},  {MarsConstants.CNST_SPYTOOL_PARA_MODE_RECTC}, {MarsConstants.CNST_QUERY_URL_RECORDREPLAY} ).", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.Error(strError);
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                    MarsGlobalData.currentMode = strMode;
                    if (!SpyToolParaInfo.currentToolParaInfo.ContainsKey(MarsConstants.CNST_SPYTOOL_PARA_REMOTESERVER))
                    {
                        MessageBox.Show(strError = "No remote server parameter is passed (Contained).", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.Error(strError);
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                    string strServer = SpyToolParaInfo.currentToolParaInfo[MarsConstants.CNST_SPYTOOL_PARA_REMOTESERVER];
                    if (string.IsNullOrEmpty(strServer))
                    {
                        MessageBox.Show(strError = "No remote server parameter is passed.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                        logger.Error(strError);
                        System.Windows.Application.Current.Shutdown();
                        return;
                    }
                    MarsGlobalData.currentRemoteServerWithAddress = strServer;

                    /// write UUID
                    /// 
                    string strDir = typeof(MarsObjectSpy).Assembly.Location;
                    strDir = System.IO.Path.GetDirectoryName(strDir);
                    var currentSystemUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                    strDir = System.IO.Path.Combine(strDir, $"data\\obj\\{currentSystemUser}");
                    if (!System.IO.Directory.Exists(strDir))
                    {
                        System.IO.Directory.CreateDirectory(strDir);
                    }
                    string uuidFileName = System.IO.Path.Combine(strDir, MarsConstants.CNST_SYPTOOL_UUIDFILE_NAME);
                    if (File.Exists(uuidFileName))
                    {
                        try
                        {
                            File.Delete(uuidFileName);
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Application_Startup", $"uuid file {uuidFileName} can't be deleted|{ex.Message}|{ex.StackTrace}");
                        }
                    }
                    System.IO.File.AppendAllText(uuidFileName, $"\r\n{uuId}");

                    logger.Info("Para:" + string.Join(" ", e.Args));

                    MarsObjectMonitor.Instance.InitMonitor(wnd.ObjectFileChangeImpl);
                }
                wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                wnd.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}\r\n{ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Error("Application_Startup", $"Error|{ex.Message}|{ex.StackTrace}");

                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}
