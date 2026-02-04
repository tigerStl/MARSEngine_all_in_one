using MarsTestFrame.SourceCode.xmlConfig;
using QtpStarter.Info;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Windows.Forms;
using TestFrameMonitor.Server;

#if _VEDIO_TIGER_
using TestFrameMonitor.source.serializeration;
using TestFrameMonitor.source.media;
#endif

namespace TestFrameMonitor
{
    static class Program
    {
        private static MLogger Logger = null;// MLogger.GetLogger(typeof(Program));
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //try
            //{

            //MessageBox.Show("ddd");
            string strAsmbName = typeof(Program).Assembly.Location;
            string strLog4NetName = $"{strAsmbName}.config";
            MLogger.LogFileCofigName = strLog4NetName;

            //MLogger.LogFileCofigName = "TestFrameMonitor.exe";
            Logger = MLogger.GetLogger(typeof(Program));
            Logger.Info("MAIN", string.Join(",", args));
            //string strCommand = "CONDITION:COLUMN=A;ColumnName";
            //TigerMarsUtil.RegularTest(@"CONDITION:\b\w+\b=\w+;\b\w+\b(;NOSUB){0,1}", strCommand);
#if _VEDIO_TIGER_

            //MarsPlayer.PlayOneFullTestCase(@"C:\automationTest\Automation Workbooks\results\Vedio\MARQUIS1 10-22-2015 5.10.39 PM\[SophisDemo]-[SophisDemo]-[LP_0]_T3.wmv");

            //MarsVedioRptMgr.Test();


            //Caption4VideoMgr.RefreshCaptionList();
            //Caption4VideoMgr.RefreshTimeSpan();
            //Thread.Sleep(1);
            //Caption4VideoMgr.AddOneCaptionStep("FILLEDIT", "USER_NAME", "", "hiQauser\n>");
            //Thread.Sleep(2);
            //Caption4VideoMgr.AddOneCaptionStep("FILLEDIT", "USER_PASSWORD", "", "hiQauser");
            //Caption4VideoMgr.ConvertAndAddCaptionsTask(@"C:\automationTest\Automation Workbooks\results\Vedio\[SophisDemo]-[SophisDemo]-[LP_0]_421.xesc", Caption4VideoMgr.gLstCaption);
#endif

            int iPort = -1;
            if (args != null)
            {
                if (args.Length == 2)
                {
                    WCFXmlCfgMgr.CurrentLoginUser = args[0];
                    string strPort = args[1];
                    if (!int.TryParse(strPort, out iPort))
                    {
                        MessageBox.Show("To start monitor, current MARS account and available TCP port should be passed.", "MARS Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            TestStepHintForm objMainForm = TestStepHintForm.GetInstance(WCFXmlCfgMgr.CurrentLoginUser, iPort);
            /** Create service **/
            MonitorService.onNewServiceStarts = objMainForm.OnNewInstanceofServiceCreated;
            MonitorService.OnShutdownServerHandler = objMainForm.ShuddownSystem;
            MonitorService.OnSetBatchModeHandler = objMainForm.SetBatchMode;
            MonitorService.OnSkipCurrentStepHandler = objMainForm.SkipCurrentStep;

            if (MonitorService.StartService(iPort) != 1)
            {
                Logger.Info("Main", "Application.Exit()");
                Application.Exit();
                return;
            }

            Application.Run(objMainForm);

            #region Remove Debugger Events
            //objMainForm.SetOnAddOrRemoveOneStepFromBreakpointsListHandler(objService.onAddOrRemoveOneStepFromBreakpointsListImpl, false);
            #endregion
            /*
            objService.onTestSuiteTestCaseNamesChangeHandler = null ;
            objService.onTestCaseListChangeHandler = null;
            objService.onTestStepCompilerEndHandler = null;
            objService.onCompilerOneTestStepHandler = null;
            objService.beforeRunTestStepHandler = null;
            objService.afterRunTestStepHandler = null;
            objService.onWriteCurrentLogHandler = null;
            objService.OnLogModeChangedHandler = null;
            objService.onCurrentLoopChangeHandler = null;

            objService.onLoopCountChangeHandler = null ;
            objService.onBreakpointReachedHandler = null ;
             * */
            Console.WriteLine("Finished");
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show($"{e.Message}\r\n{e.StackTrace}");

            //}

        }
    }
}
