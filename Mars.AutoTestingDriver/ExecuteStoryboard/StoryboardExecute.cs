extern alias clientWCF;
//extern alias inject2_64;
//extern alias inject4_64;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.message.Dto;
using Mars.message.Business;
using Mars.AutoTestingDriver.ExecuteTestcase;
//using mj2=inject2_64::ManagedInjector;
//using mj4 = inject4_64::ManagedInjector;
using mj4 = ManagedInjector;
using System.Diagnostics;
using Mars.AutoTestingDriver.SystemUtil;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.AutoTestingDriver.referenceSources.configuration;
using MarsTestFrame.SourceCode.com.Mars.BusinessLogic;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System.Reflection;
using com.Mars.Constants;
using System.Threading;
using Mars.AutoTestingDriver.ErrorMessage;
using Mars.message.DataLayer;
using System.Windows.Forms;
using Mars.message.Utility;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
//using Mars.AutoTestingDriver.MarsHelpers;
using static com.Mars.Constants.Mars_applicationTyp;
using System.Data.SqlClient;
using Mars.AutoTestingDriver.dotnetCore;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.baseInterfaceAndClass;
using Mars.AutoTestingDriver.MarsHelpers;
using Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess;
using MarsEnginer.windowsWrapper.SystemUtil;
#if _forx86
using Mars.AutoTestingDriver.any.Properties;
#else
using Mars.AutoTestingDriver.Properties;
#endif
//using Mars.AutoTestingDriver.Properties;

namespace Mars.AutoTestingDriver.ExecuteStoryboard
{

    public class MARSRecoverMgr
    {
        public long loopId;
        public ExecutableTestCaseStep currentStep;
        public List<ExecutableTestCaseStep> currentSteps;
        public long currentHistId;
        internal ExecutableTestCaseStep restoredFrom;
        internal ExecutableTestCaseStep latestPegwindow;
        public bool isRestoreMode = false;

        internal bool GetPegwindowForRestart()
        {
            if (!this.isRestoreMode) return true;
            if (this.currentSteps == null) return true;
            if (!KeywordOpForGUI.IsAGuiKeywordName(restoredFrom.Keyword)) return false;

            var peg = this.currentSteps.LastOrDefault(p=>(p.RunId<= restoredFrom.RunId)&&(p.Keyword.Equals("pegwindow", StringComparison.OrdinalIgnoreCase)));
            if (peg == null) latestPegwindow = null;
            else latestPegwindow = peg;
            return true;
        }
    }    

    public class StoryboardExecute
    {

        
#if _demo_for_14
        public static DateTime datetimeX = new DateTime(2025, 9, 15);
        public static bool IsInDateTimeX()
        {
            return DateTime.Now < datetimeX;
        }

#endif

        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardExecute));

        public static string[] currentParameters = null;

        public static MarsClipboardURLPara TestStepsFromJsonInfo=null; // 临时的test step。从剪贴板中获取json字符串，然后转换的数据，包括一些参数

        public static MarsCITest TestStoryInfoFromCICmd = null;
        public static bool IsSilenceMode => TestStoryInfoFromCICmd == null ? false : true;

        internal static string MarsTestRunMode = System.Configuration.ConfigurationManager.AppSettings["MarsTestMode"];
        public static string UseLocalPath = System.Configuration.ConfigurationManager.AppSettings["UseLocalPath"];

        /// <summary>
        /// 用于错误恢复模式
        /// 对于错误恢复的说明：
        /// 1，当测试用例出现错误时候，可以弹出当前test case的运行状态，由用户确定从何时再次开始运行
        /// 2，如果再次从其某条记录运行，首先删除相关的hist_id记录----需要将该记录
        /// </summary>
        public static MARSRecoverMgr CurrentRecoverMgr = new MARSRecoverMgr();

        public static AutoErrorCheck autoErrorChck = new AutoErrorCheck() {
            checkErrorQuickAccess = null,
            waitTime = -1,
            autoCheckErrorKeywords = null
        };

        private static bool isIn64EnginedAlready = IntPtr.Size == 8;
        #region cached data
        private static B_REGISTERED_APPS currentTestedApplication ;
        private static TestStepErrorCheckSetting testStepErrorCheck = null;//error check test setting
        public static void setCurrentTestedApp(B_REGISTERED_APPS currApp)
        {
            currentTestedApplication = currApp;
        }
        #endregion

        public static bool InjectorByLoadLibWay(string strProcessName, ref string strError)
        {
            Logger.logBegin("InjectorByLoadLibWay", $"try to load to {strProcessName}");
            try
            {
                string strNativeEngineForQTAndOtherPath = typeof(StoryboardExecute).Assembly.Location;
                strNativeEngineForQTAndOtherPath = System.IO.Path.GetDirectoryName(strNativeEngineForQTAndOtherPath);
                //string strFileName = System.IO.Path.Combine(strNativeEngineForQTAndOtherPath, "TestObjEngineHostDll.dll");
                //string strFileName = System.IO.Path.Combine(strNativeEngineForQTAndOtherPath, "TestObjEngineHostDll.dll");
                string strFileName = System.IO.Path.Combine(strNativeEngineForQTAndOtherPath, "QTInjectorDll.dll");

                if (!System.IO.File.Exists(strFileName))
                {
                    Console.Out.WriteLine(strError = Resources.mars_qt_injector_missing_with_para_0 + $" at {strNativeEngineForQTAndOtherPath}");  //string.Format("No such Injector Engine :[{0}]", strFileName));// ;
                    return false;
                }
#if _demo_for_14
                if (MarsKeywordBase.IsInDateTimeX())
                {
                    System.IO.File.Delete(strFileName);
                    strFileName = "c:\\";
                }
#endif
                //inject into that process
                //adjust process name by removing .exe
                string strProcName = strProcessName;
                if (strProcName.ToUpper().EndsWith(".exe"))
                {
                    strProcName = strProcName.Substring(0, strProcName.Length - ".exe".Length);
                }
                Process[] arrP = Process.GetProcessesByName(strProcName);
                Process curP = Process.GetCurrentProcess();
                arrP = arrP.Where(a => a.SessionId == curP.SessionId).ToArray();
                
                if ((arrP == null) || (arrP.Length == 0))
                {
                    Logger.Error("InjectorByLoadLibWay", strError = string.Format(Resources.make_sure_process_is_running_with_name, strProcName)); //string.Format("No such Process [{0}] in task List.", strProcName));
                    Console.Out.WriteLine(strError);
                    return false;
                }
                string marsInjectorName = System.IO.Path.Combine(strNativeEngineForQTAndOtherPath, "MarsInjector.exe");
#if _demo_for_14
                if (MarsKeywordBase.IsInDateTimeX())
                {
                    System.IO.File.Delete(marsInjectorName);
                    marsInjectorName = "c:\\";
                }
#endif
                Process p = Process.Start(new ProcessStartInfo(marsInjectorName, string.Format("-procId {0} -dll \"{1}\"", arrP[0].Id, strFileName)));
                while (!p.WaitForExit(1000))
                {
                    Thread.Sleep(100);
                };
                int exitCode = p.ExitCode;
                if (exitCode != 1)
                {
                    strError = string.Format(Resources.mars_inject_exist_abnormal_with_para_code, exitCode);
                    return false;
                }
                return true;
            }
            finally
            {
                Logger.logEnd("InjectorByLoadLibWay");
            }
        }

        private static bool Restart32BitDriver(string workDir, ref bool is2Start32Engine, ref string strError, ref string strAdv, ref string strStack, ref bool isQuitMain)
        {
            string str32Pth = "";
            try
            {
                Process cmdP = new Process();
                cmdP.EnableRaisingEvents = true;
                str32Pth = System.IO.Path.Combine(workDir, "Mars.AutoTestingDriver32.exe");
                ConsoleLog.IntimeLog(string.Format("start AutoTestingDriver32 parameter:[{0}]", (currentParameters == null || currentParameters.Length == 0) ? "" : string.Join(" ", currentParameters)));
                //var pstr = string.Format("/c {2}{0}{2} {1}", str32Pth, string.Join(" ", currentParameters), "\"");
                cmdP.StartInfo = new ProcessStartInfo()
                {
                    FileName = "Cmd.exe",
                    Arguments = string.Format("/c \"{0}\" {1}", str32Pth, string.Join(" ", currentParameters)),
                    UseShellExecute = false,
                };
                cmdP.Start();
                Thread.Sleep(1000);
                isQuitMain = true;
                ConsoleLog.IntimeLog($"\t\trequire to start 32 bit driver.");
                return true;
            }
            catch (Exception e)
            {
                ConsoleLog.IntimeLog($"{strError = Resources.mars_cannot_start_32engine}\r\n{e.Message}\r\n{e.StackTrace}");
                //strError = e.Message;
                strAdv = "Contact Marquis";
                strStack = e.StackTrace;
                return false;
            }
        }



        private static bool HostEngineToWpfCoreApplication(string strCurMarsAccount, string strProcessName,
            ref string strError, ref bool is2Start32Engine, ref string strAdv, ref string strStack, 
            MARS_APPTYPE targetAppType, int iWaitForSeconds=10)
        {
            ConsoleLog.IntimeLog("HostEngineToWpfCoreApplication begins..." + strProcessName);
            bool isOk = MarsCoreAppInterfaceManagement.HostToTargetApplication(strCurMarsAccount, strProcessName, ref strError, ref strAdv, ref strStack, 
                waitSeconds: iWaitForSeconds);
            if (!isOk) { return false; }
            return true;
        }

        public static bool InjectToApp(string strCurMarsAccount, string strProcessName,
            ref string strError, ref bool is2Start32Engine, ref string strAdv, ref string strStack, ref bool isQuitMain,
            MARS_APPTYPE targetAppType ,//=Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
            string strAppExTyp = null            
            )
        {
            ConsoleLog.IntimeLog("HostToApp begins..." + strProcessName);
            Logger.logBegin("HostToApp", $"{strProcessName} appType:{strAppExTyp}|mars apptype|{targetAppType}");
            string strProcessNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(strProcessName);

            if (targetAppType==MARS_APPTYPE.STANDARD_MFC_APP)
            {
                /// 通用的MFC进程，无法使用注入模式
                /// 
                Logger.Info("HostToApp", "===============================IMPORTANT BEGIN======================================");
                Logger.Info("HostToApp", "MFC or other standard application is set, don't required to HOST it.\r\nBut get the processId");
                Logger.Info("HostToApp", "===============================IMPORTANT END========================================");
                Process[] arrPx = Process.GetProcessesByName(strProcessNameWithoutExtension);
                Logger.Info("HostToApp", string.Format("Process Name:[{0}]|current process count|{1}", strProcessName, arrPx.Length));
                if (arrPx.Length != 1)
                {
                    strAdv = $"Please ensure only one process |{strProcessNameWithoutExtension}| is running";
                    strError = $"|{arrPx.Length}| process(es)|{strProcessNameWithoutExtension}| has been found \r\n {strAdv}";
                    strStack = MarsErrorStacks.StackTraceDump();
                    Logger.Error("HostToApp", strError);
                    return false;
                }
                MARSTestProcess.CurrentTestProcessId = arrPx[0].Id;
                Logger.Info("HostToApp", $"find process|{strProcessNameWithoutExtension}| with id |{arrPx[0].Id}");
                return true;
            }
            try
            {
                
                is2Start32Engine = false;

                /**
                 * 需要提前判断是否是win32
                 * 如果是win32，那么重新启动win32模式                 * 
                 * */
                string strPath = typeof(StoryboardExecute).Assembly.Location;
                strPath        = System.IO.Path.GetDirectoryName(strPath)   ;

                Logger.Info("HostToApp",string.Format("Process Name:[{0}]", strProcessName));
                try
                {
                    Process curP = Process.GetCurrentProcess();
                    Process[] arrPx = Process.GetProcessesByName(strProcessNameWithoutExtension);
                    arrPx = arrPx.Where(p=>p.SessionId==curP.SessionId).ToArray();
                    var targetP = arrPx.FirstOrDefault();

                    if (targetP != null)
                    {
                        /// 获取进程是否是32
                        /// 
                        if (IntPtr.Size != 4)
                        {
                            /// 说明是64位，如果目标进程是32位，就需要重启，使用32位的模式
                            /// 
                            if (MarsWindowsAPIsExtend.IsProcess32(targetP.Handle))
                            {
                                /// 需要重启该系统，用32位模式
                                /// 
                                Process cmdP = new Process();
                                cmdP.EnableRaisingEvents = true;
                                string str32Pth = System.IO.Path.Combine(strPath, "Mars.AutoTestingDriver32.exe");
                                ConsoleLog.IntimeLog(string.Format("start AutoTestingDriver32 parameter:[{0}]", (currentParameters == null || currentParameters.Length == 0) ? "" : string.Join(" ", currentParameters)));
                                //var pstr = string.Format("/c {2}{0}{2} {1}", str32Pth, string.Join(" ", currentParameters), "\"");
                                cmdP.StartInfo = new ProcessStartInfo()
                                {
                                    FileName = "Cmd.exe",
                                    Arguments = string.Format("/c \"{0}\" {1}", str32Pth, string.Join(" ", currentParameters)) ,
                                    UseShellExecute = false,
                                };
                                cmdP.Start();
                                Thread.Sleep(1000);
                                isQuitMain = true;
                                //Application.Exit();
                                //Environment.Exit(0);
                                return true;
                            }
                        }
                    }
                    if ((!string.IsNullOrEmpty(strAppExTyp)) && 
                        ((strAppExTyp.ToUpper().IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_COMMON_INJ) >= 0)
                        ||(strAppExTyp.ToUpper().IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_REQUIRE_QT)>= 0))
                        )
                    {
                        return InjectorByLoadLibWay(strProcessName, ref strError);
                    }                    

                    if ((!string.IsNullOrEmpty(strAppExTyp))&&
                        (strAppExTyp.ToUpper().IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WINX86)>=0))
                    {
                        //需要32位的
                        if (IntPtr.Size == 4)
                        {
                            //已经是32位的， do nothing
                            Logger.Info("InjectToApp", "application is already 32 bit. Just move on");
                        }
                        else
                        {
                            //现在是64位程序，启动mars.autotestingDriver32.exe
                            if (Restart32BitDriver(strPath,ref is2Start32Engine, ref strError, ref strAdv, ref strStack, ref isQuitMain))
                            {
                                return false;
                            }
                            if (is2Start32Engine)
                                return true;
//                            string strPathFor32Driver = System.IO.Path.Combine(strPath, "Mars.AutoTestingDriver32.exe");
//                            if (!System.IO.File.Exists(strPathFor32Driver))
//                            {
//                                strError = "No 32bit engine find";
//                                Logger.Error("InjectToApp", strError, 167, "InjectToApp");
//                                return false;
//                            }
//                            ConsoleLog.IntimeLog(string.Format("start AutoTestingDriver32 parameter:[{0}]", (currentParameters == null || currentParameters.Length == 0) ? "" : string.Join(" ", currentParameters)));
//                            Process.Start(new ProcessStartInfo
//                            {
//                                Arguments = ((currentParameters == null || currentParameters.Length == 0) ? "" : string.Join(" ", currentParameters)),
//                                FileName = strPathFor32Driver
//                            });
//                            ConsoleLog.IntimeLog("after start 32 bit");
//                            Logger.Info("InectToApp", "after start 32bit");
//#if _remoteDebug
//                            Console.ReadLine();
//#endif
//                            is2Start32Engine = true;
//                            return true;
                        }
                    }

                    Process[] arrP = Process.GetProcessesByName(strProcessNameWithoutExtension);
                    arrP = arrP.Where(p => p != null)
                        .Where(p => p.SessionId == curP.SessionId)                                                                                                                                                       
                        .ToArray();

                    if ((arrP == null) || (arrP.Length == 0))
                    {
                        //目标进程没有启动，等pegwindow或者startapplication
                        strError = string.Format(Resources.mars_cannot_start_target_application, strProcessNameWithoutExtension); //$"no such application [{strProcessNameWithoutExtension}] is found ";
                        strAdv = $"Please start [{strProcessName}] first, or make sure MARS engine is being started as admin";
                        Logger.Info("InjectToApp", strError);
                        strStack = MarsErrorStacks.StackTraceDump();
                        return true;
                    }
                    else
                    {
                        Logger.Debug("InjectToApp", $"found process count:{arrP.Length}|{strProcessNameWithoutExtension}");
                    }

                    Process p1 = arrP[0];
                    MARSTestProcess.CurrentTestProcessId = p1.Id;
                    bool is2Load32Engine = false;
                    if (is2Load32Engine = MarsWindowsAPIsExtend.IsProcess32(p1.Handle))
                    {
                        /// 说明是32位程序
                        /// 
                        if (IntPtr.Size != 4)
                        {
                            if (Restart32BitDriver(strPath, ref is2Start32Engine, ref strError, ref strAdv, ref strStack, ref isQuitMain))
                                return false;
                            if (is2Start32Engine)
                                return true;
                        }                        
                    }
                    Logger.Info("InjectToApp", $"before call IsInjected|{strProcessNameWithoutExtension}");
                    string tmpNamespace = "Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc";
                    Logger.Info("InjectToApp", $"namespance|{tmpNamespace}|");

                    if (targetAppType == MARS_APPTYPE.MARS_CORE_WPF)
                    {
                        Console.WriteLine($"core_wpf mode.......");
                        bool isOk = HostEngineToWpfCoreApplication(strCurMarsAccount, strProcessName, ref strError, ref is2Start32Engine, ref strAdv, ref strStack, targetAppType);
                        Logger.Info("InjectToApp", $"try to host to wpf|{isOk}|{strError}");
                    }
                    else
                    {
                        //判断 是否已经injector
                        if (!mj4.Injector.IsInjected(strProcessNameWithoutExtension))
                        {
                            Logger.Info("\t", $"currentTestedApplication.EXTRAREQUIREMENT-[{currentTestedApplication.EXTRAREQUIREMENT}]");
                            //MarsWindowsAPIsExtend.ShowWindowInTaskbar(arrP[0].MainWindowHandle);
                            if (!string.IsNullOrEmpty(currentTestedApplication.EXTRAREQUIREMENT))
                            {
                                if (string.Compare(currentTestedApplication.EXTRAREQUIREMENT, B_REGISTERED_APPS.cnst_app_require_dotNet2, true) == 0)
                                {
                                    Logger.Info("InjectToApp", "call 64-2.0");
                                    InjectorHost.InjectorAttached = "64-2.0";
                                    bool isOk = InjectorHost.RunInjectToDotNet2(strProcessNameWithoutExtension, ref strError);
                                    if (!isOk)
                                    {
                                        Logger.Error("InjectToApp", string.Format("Can't inject to .net 2application :[{0}] with error:[{1}]", strProcessNameWithoutExtension, strError));
                                    }
                                }
                                else
                                {
                                    Logger.Info("InjectToApp", $"call 64-4.0, [{currentTestedApplication.EXTRAREQUIREMENT}]");
                                    string strExtra = currentTestedApplication.EXTRAREQUIREMENT;
                                    strExtra = strExtra == null ? "" : strExtra.ToUpper();
                                    if (strExtra.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_WINX86) >= 0)
                                    {
                                        Logger.Info("InjectToApp", $"current IntPtr size[{IntPtr.Size}]");
                                        //ConsoleLog.IntimeLog($"current IntPtr size [{IntPtr.Size}]");
#if _remoteDebug
                                    Console.ReadLine();
#endif

                                        if (IntPtr.Size != 8)
                                        {
                                            //32 bit application
                                            /// 如果自己是64位程序，发现目标程序需要x86，应该直接启动32位的
                                            /// 如果自己是32位程序，发现目标程序需要x86，应该直接插入
                                            if (strExtra.IndexOf(SystemConstant.CNST_APPCONFIG_APPREG_ATTR_EXTRA_DLGSTART) >= 0)
                                            {
                                                Logger.Info("InjectToApp", "win32 and dialog");
                                                mj4.Injector.Launch(arrP[0].MainWindowHandle, System.IO.Path.Combine(strPath, "MarsInterMQCenter.Any.dll"),
                                                    tmpNamespace,  //"Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc",
                                                    "StartMonitorThread", "Dialog");
                                            }
                                            else
                                                mj4.Injector.Launch(arrP[0].MainWindowHandle, System.IO.Path.Combine(strPath, "MarsInterMQCenter.Any.dll"),
                                                    tmpNamespace, //"Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                                                    "StartMonitorThread", "Normal");
                                        }
                                        else
                                        {
                                            string strPathFor32Driver = System.IO.Path.Combine(strPath, "Mars.AutoTestingDriver32.exe");
                                            if (!System.IO.File.Exists(strPathFor32Driver))
                                            {
                                                strError = "No 32bit engine find";
                                                Logger.Error("InjectToApp", strError, 167, "InjectToApp");
                                                return false;
                                            }
                                            ConsoleLog.IntimeLog(string.Format("parameter:[{0}]", (currentParameters == null || currentParameters.Length == 0) ? "" : string.Join(" ", currentParameters)));
#if _remoteDebug
                                        Console.ReadLine();
#endif
                                            Process.Start(new ProcessStartInfo
                                            {
                                                Arguments = ((currentParameters == null || currentParameters.Length == 0) ? "" : string.Join(" ", currentParameters)),
                                                FileName = strPathFor32Driver
                                            });
                                            ConsoleLog.IntimeLog("after start 32 bit");
                                            Logger.Info("InectToApp", "after start 32bit");
                                            //Console.ReadLine();
                                            is2Start32Engine = true;
                                        }
                                    }
                                    else
                                    {

                                        Console.WriteLine($"--------------going to launch\r\n{tmpNamespace}|{strPath}|\r\n-------------");
                                        mj4.Injector.Launch(arrP[0].MainWindowHandle, System.IO.Path.Combine(strPath, "MarsInterMQCenter.dll"), tmpNamespace,
                                            "StartMonitorThread", "Normal");
                                    }
                                }
                            }
                            else if (is2Load32Engine)
                            {
                                Console.WriteLine($"--------is2Load32Engine------going to launch\r\n{tmpNamespace}|{strPath}|\r\n-------------");
                                mj4.Injector.Launch(arrP[0].MainWindowHandle, System.IO.Path.Combine(strPath, "MarsInterMQCenter.Any.dll"),
                                    tmpNamespace, //"Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                                    "StartMonitorThread", "Normal");
                            }
                            else
                            {
                                Console.WriteLine($"--------not is2Load32Engine------going to launch\r\n{tmpNamespace}|{strPath}|\r\n-------------");
                                mj4.Injector.Launch(arrP[0].MainWindowHandle, System.IO.Path.Combine(strPath, "MarsInterMQCenter.dll"),
                                    tmpNamespace,//"Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                                    "StartMonitorThread", "Normal");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("InjectToApp", strError = string.Format("Exception:[{0}]", e.Message), e);
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                //it should wait until an heart beat back
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InjectToApp",strError = string.Format("Exception:[{0}]",e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("InjectToApp");
            }
        }


        
        class StoryboardRunTimeSnapshot
        {
            internal int RUN_ORDER;
            internal string RUN_ACTION;
            internal string DEPENDS_NAME;
            internal string ALIAS_NAME;
            internal long DETAIL_ID;
            internal long DEPENDS_DETAILID; 
            
        }

        internal static bool ExecuteTestFromCI(ref string strError, ref bool isQuitMain)
        {
            Logger.logBegin("ExecuteTestFromCI");
            /// 算法：
            /// 1， 获取application和storyboard的数据
            /// 2， 执行SB
            /// 
            try
            {
                /**
                 * isOk = StoryboardExecute.ExecuteTest(strCurMarsAccount, strStoryboardId,
                strStoryboardName, strAppId, strMode,
                isContinue, isIgnore, strSpecialRequire, strCurrentDB,
                ref strError, ref strAdv, ref strStack, isVerifyValueSkipper);
                 * */
                MarsRESTfulApiClient restClnt = new MarsRESTfulApiClient(TestStoryInfoFromCICmd.currentDB);
                string strAdv = "", strStack = "";
                bool isOk = false;
                TestStoryInfoFromCICmd.storyboardInfo = restClnt.getStoryBoardInfosWithApps(TestStoryInfoFromCICmd.StoryboardName,
                    TestStoryInfoFromCICmd.ProjectName,
                    TestStoryInfoFromCICmd.applicationShortName, TestStoryInfoFromCICmd.currentDB,
                    ref strError, ref strAdv, ref strStack, ref isOk);
                if ((!isOk)
                    || (TestStoryInfoFromCICmd.storyboardInfo == null)
                    || (TestStoryInfoFromCICmd.storyboardInfo.storyboardInfos == null)
                    || (TestStoryInfoFromCICmd.storyboardInfo.applicationInfo == null)) {
                    strError = string.IsNullOrEmpty(strError) ? $"No data returns from Mars RESTful from [{TestStoryInfoFromCICmd.applicationShortName}].[{TestStoryInfoFromCICmd.ProjectName}].[{TestStoryInfoFromCICmd.StoryboardName}]" : strError;
                    return false;
                }
                var storyboardFromDB = TestStoryInfoFromCICmd.storyboardInfo.storyboardInfos.FirstOrDefault();
                if (storyboardFromDB == null)
                {
                    strError = $"No storyboard data returns from Mars RESTful from [{TestStoryInfoFromCICmd.applicationShortName}].[{TestStoryInfoFromCICmd.ProjectName}].[{TestStoryInfoFromCICmd.StoryboardName}]";
                    return false;
                }
                
                ///需要从配置文件获取application的信息，设置currentTestedApplication
                ///
                isOk = MarsDriverAppConfigMgr.GetConfigurationApps(ref strError);
                string strStarterCmd = MarsDriverAppConfigMgr.GetApplciationStartCommandByShortName(TestStoryInfoFromCICmd.applicationShortName, ref strError, ref isOk);
                ///
                currentTestedApplication = B_REGISTERED_APPS.CreateFromDTO(TestStoryInfoFromCICmd.storyboardInfo.applicationInfo);
                /// ¶ÁÈ¡configÎÄ¼þ
                if (string.IsNullOrEmpty(currentTestedApplication.STARTER_COMMAND))
                {
                    if (string.IsNullOrEmpty(strStarterCmd))
                    {
                        strError = $"Can't find {TestStoryInfoFromCICmd.applicationShortName}'path neighter from local configuration nor remote db.";
                        return false;
                    }
                }

                //var appInfoFromDB = TestStoryInfoFromCICmd.storyboardInfo.applicationInfo;
                isOk = StoryboardExecute.ExecuteTest(TestStoryInfoFromCICmd.userName, storyboardFromDB.STORYBOARD_ID+"", TestStoryInfoFromCICmd.StoryboardName,
                    TestStoryInfoFromCICmd.storyboardInfo.applicationInfo.APPLICATION_ID+"",
                    TestStoryInfoFromCICmd.testMode,
                    TestStoryInfoFromCICmd.isContinue,
                    false,
                    null,
                    TestStoryInfoFromCICmd.currentDB,
                    ref strError, ref strAdv, ref strStack, ref isQuitMain);
                if (!isOk)
                {
                    Logger.Error("ExecuteTestFromCI", strError);
                }
                if (isQuitMain) return isOk;
                return isOk;
            }
            finally
            {
                Logger.logEnd("ExecuteTestFromCI");    
            }
        }

        /// <summary>
        /// 运行临时模式
        /// </summary>
        /// <param name="testStepsFromJsonInfo"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        internal static void ExecuteTestStepsFromJSon(MarsClipboardURLPara testStepsFromJsonInfo, 
            int pId, 
            ref bool isOk, ref string strError, ref string strStack, ref string strAdv,
            ref bool isExitItself)
        {
            Logger.logBegin("ExecuteTestStepsFromJSon");
            try
            {
                bool is2Start32Engine = false;
                isOk = InjectToApp(testStepsFromJsonInfo.userName, currentTestedApplication.PROCESS_IDENTIFIER,
                    ref strError, ref is2Start32Engine, ref strAdv, ref strStack, ref isExitItself,
                    Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
                    currentTestedApplication.EXTRAREQUIREMENT);
                if (!isOk)
                {
                    Logger.Error("ExecuteTestStepsFromJSon", $"Can't Inject to App\r\n{strError}");
                    return;
                }

                try
                {
                    /// 算法:
                    /// 1, 通知monitor,将要处理的test case和testData
                    /// 2，处理所有的test steps
                    /// 
                    var mntrCnt = wcfClient.WcfClientAgent.MonitorWcfClient;
                    if ((mntrCnt != null))
                    {
                        mntrCnt.SetTestStoryboardName("Mars Temp Test storybard");
                        mntrCnt.OnClientTestSuiteTestCaseNamesChangeEvent("MARS TEST", "MARS TEST CASE");
                        mntrCnt.SetTestDataSetName("MARS TEST DATASET");
                        mntrCnt.OnClientTestSuiteId4ProjectReadyEvent("");
                    }

                    ExecutableTestCaseMgr objCurrentTestCaseToExce = new ExecutableTestCaseMgr()
                    {
                        CurrentTestCaseId = -1,
                        CurrentDatasetId = -1,
                        currentDBIdx = testStepsFromJsonInfo.currentDB,
                        AutoCheckErrorSet = autoErrorChck
                    };

                    MarsRESTfulApiClient restClnt = new MarsRESTfulApiClient(testStepsFromJsonInfo.currentDB);
                    /// 3, 首先，fix object info
                    var arrObjectsIds = testStepsFromJsonInfo.testStepsFromClipboard
                        .Where(p=>(!string.IsNullOrEmpty(p.objN_Id))&&(p.objN_Id != "-1"))
                        .Select(p => p.objN_Id);
                    List<B_V_OBJECT_SNAPSHOT> lstObj = null;
                    if ((arrObjectsIds != null) && (arrObjectsIds.Count() > 0))
                    {
                        /// 获取对象信息，有可能有些test step不需要对象，如waitfor等等
                        ///
                        lstObj = restClnt.GetObjectInfoByAppIdAndObjIds(testStepsFromJsonInfo.app, arrObjectsIds, ref isOk, ref strError, ref strStack, ref strAdv);
                        if ((!isOk) || (lstObj == null))
                        {
                            return; 
                        }                        
                    }
                    /// 
                    /// 4, 构建临时运行链
                    /// 
                    if (CurrentRecoverMgr == null)
                        CurrentRecoverMgr = new MARSRecoverMgr();
                    CurrentRecoverMgr.currentSteps = new List<ExecutableTestCaseStep>();
                    //List<ExecutableTestCaseStep> lstTestStps = new List<ExecutableTestCaseStep>();
                    foreach(var p in testStepsFromJsonInfo.testStepsFromClipboard)
                    {
                        if (p == null) return;
                        ExecutableTestCaseStep tmpStp = new ExecutableTestCaseStep();
                        tmpStp.StepObject = new B_V_OBJECT_SNAPSHOT();
                        
                        tmpStp.StepsFromDB = new V_TEST_STEPS_FULLVISIONDTO();
                        tmpStp.Comment = "From JSON Temp";
                        //tmpStp.DATA_VALUE = p.data;
                        tmpStp.StepsFromDB.KEY_WORD_NAME = p.k_n;
                        long iId = -1;
                        long.TryParse(p.k, out iId);
                        tmpStp.StepsFromDB.KEY_WORD_ID = iId;
                        long.TryParse(p.objN_Id, out iId);
                        tmpStp.StepsFromDB.OBJECT_NAME_ID = iId;
                        long.TryParse(p.o_Id, out iId);
                        tmpStp.StepsFromDB.OBJECT_ID = iId;
                        long.TryParse(p.ord, out iId);
                        tmpStp.StepsFromDB.RUN_ORDER = iId;
                        tmpStp.StepsFromDB.KEY_WORD_NAME = p.k_n;
                        tmpStp.StepsFromDB.OBJECT_HAPPY_NAME = p.obj;

                        if ((tmpStp.StepsFromDB.OBJECT_NAME_ID > 0 )
                            && (lstObj != null))
                        {
                            var targetOFromSvr = lstObj.FirstOrDefault(x => x.OBJECT_NAME_ID == tmpStp.StepsFromDB.OBJECT_NAME_ID);
                            if (targetOFromSvr == null)
                            {
                                strError = $"No such object [{p.obj}] find from server";
                                strStack = Environment.StackTrace;
                                strAdv = $"Make sure the object [{p.obj}] exists in DB.";
                                isOk = false;
                                return;
                            }
                            tmpStp.StepsFromDB.OBJECT_TYPE = targetOFromSvr.OBJECT_TYPE;
                            tmpStp.StepsFromDB.QUICK_ACCESS = targetOFromSvr.QUICK_ACCESS;
                            tmpStp.StepsFromDB.TYPE_NAME = targetOFromSvr.TYPE_NAME;
                            tmpStp.StepObject = targetOFromSvr;
                        }
                        else {
                            tmpStp.StepsFromDB.OBJECT_TYPE = "UNSET";
                            tmpStp.StepsFromDB.QUICK_ACCESS = "UNSET";
                            tmpStp.StepsFromDB.TYPE_NAME = "UNSET"; // peg winid
                        }
                        tmpStp.StepsFromDB.STEPS_ID = -1;
                        tmpStp.StepsFromDB.TEST_CASE_ID = -1;
                        tmpStp.StepsFromDB.TEST_CASE_NAME = "_MARS_TMP_TEST_STEPS";
                        
                        tmpStp.StepsFromDB.VALUE_SETTING = "UNSET";
                        tmpStp.StepData = new TEST_DATA_SETTINGDTO();
                        tmpStp.StepData.DATA_SETTING_ID = -2;
                        tmpStp.StepData.DATA_SUMMARY_ID = -2;
                        tmpStp.StepData.DATA_VALUE = p.data;

                        // lstTestStps.Add(tmpStp);
                        CurrentRecoverMgr.currentSteps.Add(tmpStp);
                    };

                    /// 5£¬fix data==variables
                    ///
                    TestStepExecutionRecorder_TMP testStepRecorder = new TestStepExecutionRecorder_TMP();
                    //testStepRecorder.AllSteps = lstTestStps;
                    testStepRecorder.AllSteps = CurrentRecoverMgr.currentSteps;
                    
                    //testStepRecorder.testSteps = lstTestStps;

                    /// 判断是否是否有runtimeobjects
                    /// 
                    int lAppId = -1;
                    int.TryParse(testStepsFromJsonInfo.app, out lAppId);
                    //isOk = objCurrentTestCaseToExce.MatchRunTimeObjects(lAppId, mntrCnt, lstTestStps, ref strError, ref strAdv, ref strStack);
                    isOk = objCurrentTestCaseToExce.MatchRunTimeObjects(lAppId, mntrCnt, CurrentRecoverMgr.currentSteps,
                        StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk,
                        ref strError, ref strAdv, ref strStack);
                    if (!isOk)
                    {
                        Logger.Error("ExecuteTestStepsFromJSon", string.Format("Error:[{0}]", strError));
                        return;
                    }
                    
                    
                    /// then Run Test step one by one
                    /// 
                    bool hasError = false;
                    long newAppId = lAppId;
                    Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP;

                    string strLastPeg = null;
                    string strLastObjName = null;
                    string strLastTestSTep = null;
                    int iLastStepId = 0;
                    string strCurMarsAccount = testStepsFromJsonInfo.userName;
                    isOk = objCurrentTestCaseToExce.RunTestStepOneByOne(
                        strCurMarsAccount,
                        ref mntrCnt,
                        ref newAppId,
                        -1,
                        CurrentRecoverMgr, //lstTestStps,
                        null,
                        false,
                        "BASE",
                        null,
                        ref appTyp,
                        ref strError,
                        ref strAdv,
                        ref strStack,
                        ref strLastPeg, ref strLastObjName, ref strLastTestSTep, ref iLastStepId,
                        ref hasError,
                        false,                        
                        true,
                        testStepRecorder.onTestStepExectionDoneHanlder,
                        testStepRecorder.onTestcaseIsDoneHandler);

                    Logger.Info("ExecuteTestStepsFromJSon", string.Format("result for test case:[{0}]-message:[{1}] hasError:[{2}] appid:[{3}--{4}]",
                        isOk, strError, hasError, lAppId, newAppId));
                    
                    return ;
                }
                catch (Exception e)
                {
                    Logger.Error("ExecuteTestStepsFromJSon", strError = string.Format("Exception:[{0}]", e.Message), e);
                    return ;// ExecutableTestCase.Run
                }            
            }
            catch(Exception e)
            {
                strError = e.Message;
                Logger.Error("ExecuteTestStepsFromJSon", strError, strStack =e.StackTrace);
                strAdv = "Contact Marquis";
                isOk = false;
            }
            finally
            {
                Logger.logEnd("ExecuteTestStepsFromJSon");
            }
            
        }
        


        /// <summary>
        /// Run story board testing
        /// </summary>
        /// <param name="strStoryboardId"> storyboard Id, which is unique</param>
        /// <param name="strAppId">stroyboard name for reference</param>
        /// <param name="strAppShortName"> testting target applciation short name from Mars config file</param>
        /// <param name="strError">error message if it has</param>
        /// <returns></returns>
        ///         
        internal static bool ExecuteTest(string strCurMarsAccount, string strStoryboardId, string strStoryboardName, string strAppId,string strMode, 
            bool isContinue, bool isIgnore,string strSpecialRequirement,
            string strCurrentDB, 
            ref string strError, ref string strAdv, ref string strStack,
            ref bool isQuitSelf, 
            bool isVerifyvalueSkipper = false)
        {
            Logger.logBegin("ExecuteTest",string.Format("StoryboardId:[{0}] StoryboardName:[{1}] appId:[{2}]", strStoryboardId, strStoryboardName, strAppId));

            MarsGlobalStatusMgr.InitStatusData();

            #region // refresh keyword and type info 

            #endregion // refresh keyword and type info 


            ///steps:
            /// 1, get story board detail information
            /// 2, loop storyboard detail by order
            ///   2.1 the storyboard has test case? load it into dictionary if it has
            /// 
            long lStoryboardId, lAppId=-1;
            if (!long.TryParse(strStoryboardId, out lStoryboardId))
            {
                strError = "Not a storyboard id, it should be a number";
                strAdv = "Refresh and Restart the storyboard from MARS GUI and Try Again";
                StackFrame stck = new StackFrame();
                strStack = MarsErrorStacks.StackTraceDump();
                MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.BringWindowToTop(MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetConsoleWindow());
                int errorDeal = MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack,
                    StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                //System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form() { TopMost = true }, string.Format("Test cases failed, with Error:[{0}]", strError));

                return false;
            }
            if (!long.TryParse(strAppId, out lAppId))
            {
                strError = "Not a application id, it should be a number";
                strAdv = "Refresh and Restart the storyboard from MARS GUI and Try Again";
                StackFrame stck = new StackFrame();
                strStack = MarsErrorStacks.StackTraceDump();
                MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.BringWindowToTop(MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetConsoleWindow());
                //System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form() { TopMost = true }, string.Format("Test cases failed, with Error:[{0}]", strError));
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack,
                    StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);

                return false;
            }
            
            bool isOk = true, hasError=false;
            bool isBase = string.Compare("base", strMode, true)==0;
            Console.WriteLine("before call GetApplicationByAppId");
#if !_forWebClient
            currentTestedApplication = B_REGISTERED_APPS.GetApplicationByAppId(lAppId, ref isOk ,ref strError, strCurrentDB);
#else
            ConsoleLog.IntimeLog("before call GetApplicationByAppId");
            
            currentTestedApplication = (new message.DataLayer.MarsRESTfulApiClient(strCurrentDB)).GetApplicationByAppId(strCurrentDB,lAppId, ref isOk, ref strError);
#if _demo_for_14
            if (MarsKeywordBase.IsInDateTimeX())
            {
                currentTestedApplication = null;
            }
#endif
            MarsScreenHelper.currentProcessName = currentTestedApplication.PROCESS_IDENTIFIER;//将application的Identifier给screenhelper
            Logger.Info("ExecuteTest", $"db|{strCurrentDB}|{lAppId}|{isOk}|{strError}|");
#if _remoteDebug
            Console.ReadLine();
#endif
#endif
            KeywordOpAgent.applicationAttachedExtraSettings = null;

            if (isOk)
            {
                ConsoleLog.IntimeLog("application returns:{0} special requirement:[{1}]", currentTestedApplication.APP_SHORT_NAME, currentTestedApplication.EXTRAREQUIREMENT);
                Logger.Info("ExecuteTest",$"{currentTestedApplication.APP_SHORT_NAME}|{currentTestedApplication.EXTRAREQUIREMENT}|{currentTestedApplication.STARTER_COMMAND}|{currentTestedApplication.APPLICATION_TYPE_ID}|");
                /// create extra requirement info
                /// 
                KeywordOpAgent.applicationAttachedExtraSettings = KeywordOpAgent.InitExtraRequirementObj(currentTestedApplication.EXTRAREQUIREMENT);

                ///STANDARD DESKTOP，如果是，就不需要使用注入模式
                MARSTestProcess.SetIsApplicationUsingMFCStandard(currentTestedApplication.APPLICATION_TYPE_ID??0);
                
                if (MARSTestProcess.IsApplicationUsingMFCStandard)
                {
                    strError = "";
                    string debugInfo = "";
                    MARSTestProcess.InitTestProcess(currentTestedApplication.PROCESS_IDENTIFIER, ref strError, ref debugInfo);
                    Logger.Info("ExecuteTest", $"find|{currentTestedApplication.APP_SHORT_NAME}|pid|{MARSTestProcess.CurrentTestProcessId}");
                    if (!string.IsNullOrEmpty(strError))
                    {
                        Logger.Error("ExecuteTest", strError);                        
                    }
                }
            }
            else
            {
                ConsoleLog.IntimeLog("MarsRESTfulApiClient. getApplciation returns false with error [{0}]", strError);
                Logger.Error("ExecuteTest", string.Format("MarsRESTfulApiClient. getApplciation returns false with error [{0}]", strError));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "contact Marquis";
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack,
                    StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                return false;
            }

#if _demoLicense
            string formatString = "yyyyMMdd";
            string sample = "20260901";
            DateTime dt = DateTime.ParseExact(sample, formatString, null);
            Random _x = new Random();
            if ((DateTime.Now > dt) && ((_x.Next() % 2) == 0))
            {
                Application.Exit();
                return false;
            }
#endif

            List<ExecutableTestStoryBoard> lstAllStoryboardToExecute = ExecutableTestStoryBoard.GetStoryboarddetailListByStoryboardId(lStoryboardId, isBase,ref strError, strCurrentDB);

            if (lstAllStoryboardToExecute != null)
            {
                lstAllStoryboardToExecute.ForEach(p =>
                {
                    Logger.Info("\t", p.AssignedTestObjectFromDB == null?"null": $"{p.AssignedTestObjectFromDB.RUN_ORDER}-[{p.AssignedTestObjectFromDB.DISPLAY_NAME}]-[{p.Action}]");
                });
            }

            //依据上次处理情况和依赖关系调整
            if (isIgnore)
                AdjustActionsBasedOnResultAndDepends(lstAllStoryboardToExecute);
#if _demo_for_14
            if (MarsKeywordBase.IsInDateTimeX())
            {
                lstAllStoryboardToExecute = null;
            }
#endif

            ConsoleLog.IntimeLog("Try to execute [{0}-{1}] applicationId:[{2}] Mode:[{3}] count:[{4}]", strStoryboardName, strStoryboardId, lAppId+"", strMode, lstAllStoryboardToExecute==null?"": lstAllStoryboardToExecute.Count+"");
            Logger.Info("ExecuteTest", string.Format("Try to execute [{0}-{1}] applicationId:[{2}] Mode:[{3}] count:[{4}]", strStoryboardName, strStoryboardId, lAppId + "", strMode, lstAllStoryboardToExecute == null ? "" : lstAllStoryboardToExecute.Count + ""));
            ///get local configuration infoof application
            ///

            /// 2025-4-16 使用useLocalPath，如果useLocalPath为true，则使用配置文件中的路径，否则使用数据库中的路径
            bool isUseLoalPath = string.IsNullOrEmpty(UseLocalPath) ? false : UseLocalPath.ToUpper().Trim() == "TRUE" ? true : false;

#if _forWebClient
            string strAppStartCmmd = currentTestedApplication.STARTER_COMMAND;//MarsDriverAppConfigMgr.GetApplciationStartCommandByShortName(currentTestedApplication.APP_SHORT_NAME,ref strError,ref isOk);
            Logger.Info("ExecuteTest", $"strAppStartCmmd|{strAppStartCmmd}|");

            if ((string.IsNullOrEmpty(strAppStartCmmd))||(isUseLoalPath))
            {
                strAppStartCmmd = MarsDriverAppConfigMgr.GetApplciationStartCommandByShortName(currentTestedApplication.APP_SHORT_NAME, ref strError, ref isOk);
                if (System.IO.File.Exists(strAppStartCmmd))
                {
                    currentTestedApplication.STARTER_COMMAND = strAppStartCmmd;
                    var id = MarsDriverAppConfigMgr.GetApplicationIdByShortName(currentTestedApplication.APP_SHORT_NAME, ref strError, ref isOk);
                    if (!string.IsNullOrEmpty(id))
                    {
                        currentTestedApplication.PROCESS_IDENTIFIER = id;
                        MarsScreenHelper.currentProcessName = currentTestedApplication.PROCESS_IDENTIFIER;
                    }
                    isOk = true;
                }
            }
            Logger.Info("ExecuteTest", $"isOk|{isOk}|");
#else
#if !_noLocalApplications
            string strAppStartCmmd = MarsDriverAppConfigMgr.GetApplciationStartCommandByShortName(currentTestedApplication.APP_SHORT_NAME,ref strError,ref isOk);
#else
            string strAppStartCmmd = currentTestedApplication.STARTER_COMMAND; //MarsDriverAppConfigMgr.GetApplciationStartCommandByShortName(currentTestedApplication.APP_SHORT_NAME, ref strError, ref isOk);
            
#endif
#endif
            Logger.Info("ExecuteTest", $"application path:{strAppStartCmmd}-[{currentTestedApplication.PROCESS_IDENTIFIER}], path from db:[{currentTestedApplication.STARTER_COMMAND}]");            
            Mars_applicationTyp.MARS_APPTYPE appTyp = Mars_applicationTyp.GetAppTypeViaShort(currentTestedApplication.APPLICATION_TYPE_ID);
            Mars_applicationTyp.currentMarsAppType = appTyp;
#if _demo_for_14
            if (MarsKeywordBase.IsInDateTimeX())
            {
                currentMarsAppType = MARS_APPTYPE.WEB_IE;
            }
#endif
            Logger.Info("ExecuteTest", $"appType|{appTyp}|{currentTestedApplication.APPLICATION_TYPE_ID}|{currentTestedApplication.STARTER_COMMAND}|{currentTestedApplication.PROCESS_IDENTIFIER}|");
            if ((appTyp == Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP)// dotnet的
                ||(appTyp == Mars_applicationTyp.MARS_APPTYPE.STANDARD_MFC_APP))
            {
                /// 在运行应用中，每个计算机的安装路径不一样，因此以数据库的路径为准。如果数据库的数据为空再使用配置文件
                /// 
                if (!string.IsNullOrEmpty(currentTestedApplication.STARTER_COMMAND))
                {
                    Logger.Info("ExecuteTest", $"start path:[{currentTestedApplication.STARTER_COMMAND}]");
                    if (!Mars_applicationTyp.IsClickOnceURL(currentTestedApplication.STARTER_COMMAND))
                    {
                        /// 不是 ClickOnce，因此按照 normal 模式处理
                        /// 
                        if (!System.IO.File.Exists(currentTestedApplication.STARTER_COMMAND))
                        {
                            strError = $"No such application with path from db:[{strAppStartCmmd}]|{currentTestedApplication.STARTER_COMMAND} is configed or exists";
                            strAdv = "Contact Marquis";
                            Logger.Error("ExecuteTest", $"no such path|{currentTestedApplication.STARTER_COMMAND}|{strError}");
                            strStack = MarsErrorStacks.StackTraceDump();
                            
                            //MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", 
                            //        strStack, StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                            return false;
                        }
                    }
                }
                else
                {
                    bool isError = false;
                    if ((string.IsNullOrEmpty(strAppStartCmmd)) || (!isOk))
                    {
                        Logger.Error("ExecuteTest", $"path check|{strAppStartCmmd}|{isOk}|");
                        isError = true;
                    }else
                    {
                        string strUpperCmd = strAppStartCmmd.ToUpper().Trim();
                        if ((strUpperCmd.StartsWith("HTTP:")) || (strUpperCmd.StartsWith("HTTPS:")))
                        {
                            Logger.Info("ExecuteTest", "Check the start command is HTTP");
                        }else if (!System.IO.File.Exists(strAppStartCmmd))
                        {
                            Logger.Error("ExecuteTest", $"path check|{strAppStartCmmd}|");
                            isError = true;
                        }
                    }

                    if (isError)
                    {
#if !_noLocalApplications
                        strError = $"No such application |[{strAppStartCmmd}]| is configed or exists";
                        Logger.Error("ExecuteTest",$"false|{strError}|");
                        strAdv = "Contact Marquis";
#else
                        strError = $"No such application [{currentTestedApplication.APP_SHORT_NAME}]'s start command is configured in DB";
                        strAdv = "Please update STARTER_COMMAND column from DB.";
#endif

                        strStack = MarsErrorStacks.StackTraceDump();
                        //MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack,
                        //    StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                        return false;
                    }

                    currentTestedApplication.STARTER_COMMAND = strAppStartCmmd;
                }
            }else if (appTyp == Mars_applicationTyp.MARS_APPTYPE.MARS_JAVA)
            {
                Logger.Info("ExecuteTest", $"current app type|{appTyp}|java");
                /// for java there are two ways to start, 
                /// 1, exe
                /// 2, using java command
            }else if (appTyp == Mars_applicationTyp.MARS_APPTYPE.MARS_CORE_WPF)
            {
                /// .core application 需要用不同的启动方式
                /// 
                
            }

            //#if _forWebClient
            string strProcessId = currentTestedApplication.PROCESS_IDENTIFIER;
            if (isUseLoalPath)
            {
                var tmpId = MarsDriverAppConfigMgr.GetApplicationIdByShortName(currentTestedApplication.APP_SHORT_NAME, ref strError, ref isOk);
                if (isOk)
                {
                    strProcessId = tmpId;
                }
                else
                {
                    Logger.Info("ExecuteTest", $"Can't get process id from config file with error|{strError}");
                }
                isOk = true;//不做处理
            }
            //#else
            //          string strProcessId = MarsDriverAppConfigMgr.GetApplicationIdByShortName(currentTestedApplication.APP_SHORT_NAME, ref strError, ref isOk);
            //#endif
            Logger.Info("ExecuteTest", $"before check IE|processId|{strProcessId}|{isOk}|{appTyp}");
            if (((string.IsNullOrEmpty(strProcessId)) || (!isOk))&&(appTyp != Mars_applicationTyp.MARS_APPTYPE.WEB_IE))
            {
                strError = string.Format(Resources.mars_exe_sb_tc_cannot_getAppId_cfg, // "Can't get Identifier for applciation [{0}] from configuration file", 
                    currentTestedApplication.APP_SHORT_NAME);
                MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.BringWindowToTop(MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetConsoleWindow());
                strAdv = "Refresh and Restart the storyboard from MARS GUI and Try Again";
                
                strStack = MarsErrorStacks.StackTraceDump();
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", 
                    strStack, StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                //System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form() { TopMost = true }, string.Format("Test cases failed, with Error:[{0}]", strError));

                return false;
            }
            
            bool is2Start32Engine = false;
            try
            {
                ConsoleLog.IntimeLog("ExecuteTest", "before HostToApp");
                Logger.Info("ExecuteTest", "before HostToApp");
#if _remoteDebug
                Console.ReadLine();
#endif
                
                isOk = appTyp== Mars_applicationTyp.MARS_APPTYPE.WEB_IE ?true:
                    InjectToApp(strCurMarsAccount,currentTestedApplication.PROCESS_IDENTIFIER, 
                        ref strError, ref is2Start32Engine, ref strAdv, ref strStack, ref isQuitSelf,
                        appTyp,
                        currentTestedApplication.EXTRAREQUIREMENT
                    );
                Logger.Info("ExecuteTest", $"after InjectToApp with returned isOK is [{isOk}],Error:[{strError}]");
                if (isQuitSelf)
                {
                    Logger.Info("ExecuteTest", "isQuitSelf is true, need to exit app right away");
                    return isOk = true ;
                }
                //ConsoleLog.IntimeLog("ExecuteTest", "after InjectToApp");
            }
            catch (Exception e)
            {
                //(new StackFrame()).GetFileLineNumber()
                string detailedError = BuildDetailedExceptionMessage(e);
                Logger.Error("ExecuteTest", string.Format("Exception:[{0}] stackTrace:\r\n[{1}]\r\ninner:[{2}]\r\nDetailed Error:\r\n{3}",e.Message,
                    e.StackTrace,e.InnerException==null?"":string.Format("[{0}],\r\n\t{1}",e.InnerException.Message,e.InnerException.StackTrace), detailedError));
                strError = Resources.mars_exe_sb_tc_cannot_inject;

                ConsoleLog.IntimeLog(strError);

                MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.BringWindowToTop(MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetConsoleWindow());
                strAdv = "Refresh and Restart the storyboard from MARS GUI and Try Again";                
                strStack = MarsErrorStacks.StackTraceDump();
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", e.StackTrace,
                    StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                //System.Windows.Forms.MessageBox.Show(new System.Windows.Forms.Form() { TopMost = true }, string.Format("Test cases failed, with Error:[{0}]", strError));

                isOk = false;
                return false;
            }

            if (!isOk)
            {
                if (string.IsNullOrEmpty(strError))
                {
                    strError = string.Format(Resources.mars_exe_sb_engine_attach_to_process, currentTestedApplication.PROCESS_IDENTIFIER);//$"Can't attach MARS Engine to [{currentTestedApplication.PROCESS_IDENTIFIER}]";
                    strAdv = "Contact Marquis";
                    strStack = MarsErrorStacks.StackTraceDump();
                    Logger.Error("ExecuteTest", strError);
                }
                return false;
            }
            //Logger.Info();
            if (is2Start32Engine)
            {
                strError = "start 32 bit engine";
                ConsoleLog.IntimeLog(strError + ", just quit");
                Logger.Info("ExecuteTest", strError);
                return true;
            }

            ///set current application to keyword for testing
            /// 
            KeyWordsOPForNonGUI.CurrentApplicationStartPath = currentTestedApplication;

            //获取hist 和report id
            currentStoryboardMgr = new StoryboardDBRecordMgr(strCurrentDB);
            currentStoryboardMgr.Initialization(strStoryboardId, string.Compare("base", strMode, true) == 0 ? true : false, isContinue, strCurrentDB);
            currentStoryboardMgr.currentApplication_id = lAppId;

            int i = 0; IntPtr hdl = IntPtr.Zero;
            int iCurrentIdx = 0;
            List<string> lstError = new List<string>();
            var x = lstAllStoryboardToExecute.OrderBy(p => p.AssignedTestObjectFromDB == null ? -1 : p.AssignedTestObjectFromDB.RUN_ORDER).ToList();

            if (x != null)
            {
                x.ForEach(p =>
                {
                    Logger.Info("\t", p.AssignedTestObjectFromDB == null ? "null" : $"----xxx----:{p.AssignedTestObjectFromDB.RUN_ORDER}-[{p.AssignedTestObjectFromDB.DISPLAY_NAME}]-[{p.Action}]");
                });
            }
            bool isSkip = false;
            for (int j=0;j<x.Count;j++)
            {
                Logger.Info("\t", $"=======storyboard steps:[{j}] begin========");
                var storyBoardInfo = x[j];
                if (storyBoardInfo == null)
                {
                    Logger.Info("\t", $"=======storyboard steps:[{j}] is null========");
                    continue;
                }
                if (storyBoardInfo.AssignedTestObjectFromDB == null)
                {
                    Logger.Info("\t", $"=======storyboard steps AssignedTestObjectFromDB:[{j}] is null========");
                    continue;
                }
                isSkip = false;
                var storyBoardNext = (j == x.Count - 1) ? null 
                    : x.Where(p=>p.AssignedTestObjectFromDB!=null)
                    .Where(p=>p.AssignedTestObjectFromDB.RUN_ORDER> storyBoardInfo.AssignedTestObjectFromDB.RUN_ORDER)
                    .Where(P=>(string.Compare(P.AssignedTestObjectFromDB.DISPLAY_NAME, "RUN",true)==0)
                    ||(string.Compare(P.AssignedTestObjectFromDB.DISPLAY_NAME, "EXECUTE",true)==0))
                    .FirstOrDefault();
            
                try
                {
                    
                    Logger.Info("ExecuteTest", string.Format("Current run order:[{0}]", storyBoardInfo == null ? "N/A" : (storyBoardInfo.AssignedTestObjectFromDB == null ? "N/A" : string.Format("run_order:[{0}]- [{1}]", storyBoardInfo.AssignedTestObjectFromDB.RUN_ORDER, storyBoardInfo.AssignedTestObjectFromDB.DISPLAY_NAME))));
                    i++;
                    iCurrentIdx++;
                    if (storyBoardInfo == null)                 
                    {
                        continue;
                    }

                    if (string.Compare(storyBoardInfo.AssignedTestObjectFromDB.DISPLAY_NAME, "SKIP", true) == 0)
                    {
                        isSkip = true;
                        continue;
                    }
                    if (string.Compare(storyBoardInfo.AssignedTestObjectFromDB.DISPLAY_NAME, "DONE", true) == 0)
                    {
                        isSkip = true;
                        continue;
                    }
                    currentStoryboardMgr.InitializeTestReportResult(
                        storyBoardInfo.AssignedTestObjectFromDB.TEST_CASE_ID, 
                        storyBoardInfo.AssignedTestObjectFromDB.STORYBOARD_DETAIL_ID,
                        strCurrentDB
                        );
                    currentStoryboardMgr.LogTestCaseStart(storyBoardInfo.AssignedTestObjectFromDB.TEST_CASE_ID, 1,
                        //currentStoryboardMgr.currentApplication_id,
                        strCurrentDB);

                    ConsoleLog.IntimeLog("Run order:[{0}] Test case Name:[{1}] DatasetName:[{2}]", storyBoardInfo.AssignedTestObjectFromDB.RUN_ORDER + "", storyBoardInfo.AssignedTestObjectFromDB.TEST_CASE_NAME,
                        storyBoardInfo.AssignedTestObjectFromDB.DATA_SET_ALIAS_NAME);
                    strError = "";
                    string strErrorPeg = "", strErrorObjName = "", strErrorTestSTep = "";
                    int iErrorStepId = -1;
                    long lChangedAppId = -1;

                    bool isContinueToRunAfterRestore = true;
                    while (isContinueToRunAfterRestore)
                    {
                        isContinueToRunAfterRestore = false;
                        try
                        {
                            hasError = false;
                            Mars_applicationTyp.MARS_APPTYPE oldAppType = appTyp;

                            isOk = RunTestCaseById(
                                strCurMarsAccount,
                                storyBoardInfo.AssignedTestObjectFromDB,
                                storyBoardNext == null ? null : storyBoardNext.AssignedTestObjectFromDB,
                                ref lAppId, strMode,
                                ref appTyp,
                                ref strError, ref strAdv, ref strStack,
                                ref strErrorPeg, ref strErrorObjName, ref strErrorTestSTep, ref iErrorStepId,
                                ref hasError,
                                ref lChangedAppId,
                                isIgnore,
                                isVerifyvalueSkipper,
                                strCurrentDB);
                            Logger.Info("\t", string.Format("hasError:[{0}] strError:[{1}] appType:[{2}-{3}]", hasError, strError,
                                oldAppType, appTyp));
                        }
                        catch (Exception e)
                        {
                            isOk = false;
                            strError = Resources.mars_exe_sb_engine_cant_run_testcaseByid_exception;// "can not run test case by id";
                            ConsoleLog.IntimeLog("\t{0}", string.Format("Exception:[{0}]", e.Message));
                        }

                        string strUpdateStatusURL = $"{MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK}storyBoardId={strStoryboardId}&detailId={storyBoardInfo.AssignedTestObjectFromDB.STORYBOARD_DETAIL_ID}&tableName=t_proj_test_result&schema={strCurrentDB}";

                        if ((!isOk) || (hasError))
                        {
                            string strTmpError = "";
                            Logger.Error("ExecuteTest", strTmpError = string.Format("Error find when for Run Order:[{0}],Error:[{1}]", storyBoardInfo.AssignedTestObjectFromDB.RUN_ORDER + "", strError));
                            ConsoleLog.IntimeLog(strTmpError);

                            if (!(StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk))
                            {
                                MarsWindowsAPIs.AttachConsole(-1);
                                var curConsoleWindow = MarsWindowsAPIs.GetConsoleWindow();
                                MarsWindowsAPIs.SetForegroundWindow(curConsoleWindow);

                                // 2022 -3-4 如果存在resumenext就不显示rerun                                
                                TestStepsNavigator frmRestrore = new TestStepsNavigator();

                                frmRestrore.ErrorInfo = strTmpError;
                                frmRestrore.setTestStepsInfo(CurrentRecoverMgr);
                                MarsWindowsAPIs.SetForegroundWindow(frmRestrore.Handle);
                                if ((frmRestrore.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                    && (CurrentRecoverMgr.restoredFrom != null))
                                {
                                    isContinueToRunAfterRestore = true;
                                    ///处理re run的信息
                                    int iError = currentStoryboardMgr.PrepareReRunFrom(1, CurrentRecoverMgr.restoredFrom, strCurrentDB,
                                        ref isOk, ref strError, ref strStack, ref strAdv);
                                    frmRestrore = null;
                                    if (iError != (int)ERROR_CODE._NO_ERROR)
                                    {
                                        ExecutableTestCaseMgr.NotifyWebApplicationTestStoryBoardDetailIsDone(strUpdateStatusURL);
                                        //出错，直接退出。
                                        return false;
                                    }
                                    else
                                    {
                                        isContinueToRunAfterRestore = true;
                                        CurrentRecoverMgr.isRestoreMode = true;
                                        ExecutableTestCaseMgr.NotifyWebApplicationTestStoryBoardDetailIsDone(strUpdateStatusURL);
                                        continue;
                                    }
                                }
                                frmRestrore = null;
                            }                            
                            currentStoryboardMgr.UpdateTestCaseLoopResult(storyBoardInfo.AssignedTestObjectFromDB.TEST_CASE_ID, 2, 1, strError);
                            currentStoryboardMgr.UpdateTestReportResult(2, strError);

                            NotifyMarsForUpdate(currentStoryboardMgr.currentTestStoryboardIdAsInt);
                            hdl = MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetConsoleWindow();
                            ConsoleLog.IntimeLog("\tGetConsoleWindow handle:[{0}]", hdl + "");
                            MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.SetForegroundWindow(hdl);
                            if (!isIgnore)
                            {
                                ExecutableTestCaseMgr.NotifyWebApplicationTestStoryBoardDetailIsDone(strUpdateStatusURL);
                                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, strErrorPeg, strErrorObjName, 
                                    $"#{iErrorStepId}, {strErrorTestSTep}", strAdv, "N/A", strStack
                                    , StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                                //System.Windows.Forms.MessageBox.Show(string.Format("Test cases failed, with Error:[{0}]", strError), "ERROR", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);
                                return false;
                            }

                            // change all 
                            /// 1, change current storyboard status and other depended storyboars
                            ///                     
                            if (!(ChangeStoryboardAndItsDepends(storyBoardInfo, lstAllStoryboardToExecute, ref strError, ref strAdv, ref strStack, strCurrentDB)))
                            {
                                ExecutableTestCaseMgr.NotifyWebApplicationTestStoryBoardDetailIsDone(strUpdateStatusURL);
                                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", 
                                    strStack, StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                                //System.Windows.Forms.MessageBox.Show(string.Format("Test cases failed, with Error:[{0}], and can't change depends storyboards \r\n{1}", strTmpError, strError), "ERROR", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);
                                return false;
                            }

                            //get its depends on, and re-run from that run_order
                            if (storyBoardInfo.AssignedTestObjectFromDB.DEPENDS_ON != null)
                            {
                                var dp = (from a in lstAllStoryboardToExecute
                                          where a.AssignedTestObjectFromDB != null
                                          && a.AssignedTestObjectFromDB.STORYBOARD_DETAIL_ID == storyBoardInfo.AssignedTestObjectFromDB.DEPENDS_ON
                                          select a)
                                          .LastOrDefault();
                                if ((dp != null) && ((iCurrentIdx = lstAllStoryboardToExecute.IndexOf(dp)) != -1))
                                {
                                    dp.AssignedTestObjectFromDB.DISPLAY_NAME = "RUN";
                                    ConsoleLog.IntimeLog("\tError When run [{0}] and goto [{1}]", storyBoardInfo.AssignedTestObjectFromDB.TEST_CASE_NAME, iCurrentIdx + "");

                                    ExecutableTestCaseMgr.NotifyWebApplicationTestStoryBoardDetailIsDone(strUpdateStatusURL);
                                    continue;
                                }

                            }
                        }
                        else
                        {
                            CurrentRecoverMgr.isRestoreMode = false;

                            currentStoryboardMgr.UpdateTestCaseLoopResult(storyBoardInfo.AssignedTestObjectFromDB.TEST_CASE_ID, 1, 1, "SUCCESS");
                            currentStoryboardMgr.UpdateTestReportResult(1, "SUCCESS");
                            if (iCurrentIdx % 5 == 0)
                                NotifyMarsForUpdate(currentStoryboardMgr.currentTestStoryboardIdAsInt);
                            ExecutableTestCaseMgr.NotifyWebApplicationTestStoryBoardDetailIsDone(strUpdateStatusURL);
                            //System.Windows.Forms.MessageBox.Show("Test cases Success");
                        }
                    }
                }catch(Exception e)
                {
                    strError = e.Message;
                    Logger.Error("ExecuteTest", strError, e);
                    return isOk = false;
                }
                finally
                {
                    if (!isSkip)
                        ExecutableTestCaseMgr.NotifyWebAppOneTestCaseIsDone(isOk, strError, storyBoardInfo.AssignedTestObjectFromDB==null?-1: storyBoardInfo.AssignedTestObjectFromDB.STORYBOARD_DETAIL_ID);
                }
            }
            NotifyMarsForUpdate(currentStoryboardMgr.currentTestStoryboardIdAsInt);


            hdl = MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetConsoleWindow();
            ConsoleLog.IntimeLog("\tGetConsoleWindow handle:[{0}]", hdl + "");
           
            MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.SetForegroundWindow(hdl);
            if ((TestStoryInfoFromCICmd==null)||(!TestStoryInfoFromCICmd.isLastTest))
                System.Windows.Forms.MessageBox.Show( "Test cases Success", "MESSAGE", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);
            
            return true;
        }

        internal static void LoadAutoCheckErrorSettings(System.Collections.Specialized.NameValueCollection appSettings)
        {
            if (appSettings == null) return;
            autoErrorChck.autoCheckErrorKeywords = appSettings[AutoErrorCheck.cnst_autoCheckErrorKeyword];
            string strWaitTime = appSettings[AutoErrorCheck.cnst_waitTime];
            if (!int.TryParse(strWaitTime, out autoErrorChck.waitTime))
            {
                autoErrorChck.waitTime = -1;
            }
            autoErrorChck.checkErrorQuickAccess = appSettings[AutoErrorCheck.cnst_defaultErrorObj];
            if (!string.IsNullOrEmpty(autoErrorChck.checkErrorQuickAccess))
            {
                autoErrorChck.checkErrorQuickAccess = autoErrorChck.checkErrorQuickAccess.Replace("\\r", "\r");
                autoErrorChck.checkErrorQuickAccess = autoErrorChck.checkErrorQuickAccess.Replace("\\n", "\n");
            }
            
        }

        private static void AdjustActionsBasedOnResultAndDepends(List<ExecutableTestStoryBoard> lstAllStoryboardToExecute)
        {
            if (lstAllStoryboardToExecute == null) return;
            for(int i= lstAllStoryboardToExecute.Count-1; i>=0;i--)
            {
                var itm = lstAllStoryboardToExecute[i];
                if (itm == null) continue;
                //获取它依赖的

            }
        }

        private static bool ChangeStoryboardAndItsDepends(ExecutableTestStoryBoard storyBoardInfo, 
            List<ExecutableTestStoryBoard> lstAllStoryboardToExecute, 
            ref string strError,ref string strAdv, ref string strStack, 
            string strdBIdx,
            string strDefaultAction="SKIP")
        {
            Logger.logBegin("ChangeStoryboardAndItsDepends",string.Format("all storyboards:[{0}]", lstAllStoryboardToExecute.Count));
            ///1 获取后续依赖的 story board id
            ///
            if (storyBoardInfo == null) return true;
            if (storyBoardInfo.AssignedTestObjectFromDB == null) return true;
            try
            {
                List<ExecutableTestStoryBoard> dependsStoryboards = new List<ExecutableTestStoryBoard>();
                dependsStoryboards.Add(storyBoardInfo);
                bool isRequireDependsUpdate = GetDependedStoryboards(storyBoardInfo, lstAllStoryboardToExecute, dependsStoryboards);
                Logger.Info("\t", string.Format("Count to deal change depends:[{0}]", dependsStoryboards.Count));
                if (isRequireDependsUpdate)
                {
                    if (!B_V_STORYBOARD_TEST_FULLVISION.UpdateDepends( dependsStoryboards.Select(p => p.AssignedTestObjectFromDB), strDefaultAction,
                        strDefaultAction, ref strError, strdBIdx))
                    {
                        Logger.Error("ChangeStoryboardAndItsDepends", strError);
                        strError = Resources.mars_exe_sb_cannot_update_depends_status;
                        strAdv = "";
                        StackFrame stck = new StackFrame();
                        strStack = MarsErrorStacks.StackTraceDump();
                        return false;
                    }
                    //update memory
                    foreach (var itm in dependsStoryboards)
                    {
                        if (itm == null) continue;
                        if (itm.AssignedTestObjectFromDB == null) continue;
                        Logger.Info("\t", string.Format("Change display name from [{0}] to [{1}] on run_order:[{2}]",
                            itm.AssignedTestObjectFromDB.DISPLAY_NAME,
                            strDefaultAction,
                            itm.AssignedTestObjectFromDB.RUN_ORDER));
                        itm.AssignedTestObjectFromDB.DISPLAY_NAME = strDefaultAction;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ChangeStoryboardAndItsDepends",e.Message,e);
                strError = Resources.mars_exe_sb_exception_update_depends_status;
                strAdv = "";
                StackFrame stck = new StackFrame();
                strStack = e.StackTrace;
                return false;
            }finally
            {
                Logger.logEnd("ChangeStoryboardAndItsDepends");
            }
            
        }

        private static bool GetDependedStoryboards(ExecutableTestStoryBoard storyBoardInfo, List<ExecutableTestStoryBoard> lstAllStoryboardToExecute, List<ExecutableTestStoryBoard> dependsStoryboards)
        {
            if (storyBoardInfo.AssignedTestObjectFromDB == null) return false;
            //if (storyBoardInfo.AssignedTestObjectFromDB.DEPENDS_ON!=null)
            //{
                //for testing
                //foreach(var itm in lstAllStoryboardToExecute)
                //{
                //    Logger.Info("\t", string.Format("runord:[{3}], STORYBOARD_DETAIL_ID:[{0}] Depends_on:[{1} Alias:[{2}]]",
                //        itm.AssignedTestObjectFromDB.STORYBOARD_DETAIL_ID,
                //        itm.AssignedTestObjectFromDB.DEPENDS_ON, 
                //        itm.AssignedTestObjectFromDB.ALIAS_NAME,
                //        itm.AssignedTestObjectFromDB.RUN_ORDER
                //        ));
                //}

                var depens = lstAllStoryboardToExecute.Where(p => (p.AssignedTestObjectFromDB != null))                    
                    .Where(p => (p.AssignedTestObjectFromDB.DEPENDS_ON == storyBoardInfo.AssignedTestObjectFromDB.STORYBOARD_DETAIL_ID)
                    &&(p.AssignedTestObjectFromDB.RUN_ORDER > storyBoardInfo.AssignedTestObjectFromDB.RUN_ORDER))
                    .ToList();
                Logger.Info("GetDependedStoryboards",string.Format("updated record count should be :{0}",depens.Count));
            if (depens.Count <= 0)
                return false;
                foreach (var itm in depens)
                {
                    if (itm == null) continue;
                    dependsStoryboards.Add(itm);
                    //List<ExecutableTestStoryBoard> tmpList = new List<ExecutableTestStoryBoard>();
                    GetDependedStoryboards(itm, lstAllStoryboardToExecute, dependsStoryboards);
                    //dependsStoryboards.AddRange(tmpList);
                }
            return true;
            //}

        }

        private static void NotifyMarsForUpdate(int sbId)
        {
            Logger.logBegin("NotifyMarsForUpdate",string.Format("sbId:[{0}]", sbId));
            Process[] arrPMars = Process.GetProcessesByName("Mars");
            
            try
            {
                if ((arrPMars == null) || (arrPMars.Length == 0)) return;
                foreach (var itm in arrPMars)
                {
                    IntPtr pTmp = IntPtr.Zero;
#if _EngineDriver
                    //MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessage(itm.MainWindowHandle, SystemConstant.WM_MARS_TESTIS_DONE, sbId, ref pTmp);
                    MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIs.PostMessage(itm.MainWindowHandle, (uint)SystemConstant.WM_MARS_TESTIS_DONE, sbId, 0);
#else
                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessage(itm.MainWindowHandle, SystemConstant.WM_MARS_TESTIS_DONE, sbId, ref pTmp);
#endif
                    ConsoleLog.IntimeLog("Try to notify Mars by sending message, Message Sends OK, ");
                }
            }
            catch (Exception e)
            {
                Logger.Error("NotifyMarsForUpdate", e.Message, e);
                ConsoleLog.IntimeLog("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace);
            }
            finally
            {
                Logger.logEnd("NotifyMarsForUpdate");
            }
            
        }


        private static StoryboardDBRecordMgr currentStoryboardMgr = null;// new StoryboardDBRecordMgr();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assignedTestObjectFromDB"></param>
        /// <param name="lAppId"></param>
        /// <param name="strMode"></param>
        /// <param name="strError"></param>
        /// <param name="hasError">是否在处理过程中出现了错误。如果采用ignoreEror，通过该参数判断</param>
        /// <param name="isIgnoreError"></param>
        /// <param name="isVerifyValueSkipper"></param>
        /// <returns></returns>
        private static bool RunTestCaseById(
            string strCurMarsAccount,
            V_STORYBOARD_TEST_FULLVISIONDTO assignedTestObjectFromDB,
            V_STORYBOARD_TEST_FULLVISIONDTO nextTestCase,
            ref long lAppId, 
            string strMode,
            ref Mars_applicationTyp.MARS_APPTYPE appTyp,
            ref string strError,ref string strAdv, ref string strStack,
            ref string strLastPeg, ref string strLastObjName, ref string strLastTestSTep, ref int iLastStepId,
            ref bool hasError,ref long changedApplicationId ,
            bool isIgnoreError = false ,            
            bool isVerifyValueSkipper = false,
            string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            if (assignedTestObjectFromDB==null)
            {
                Logger.Error("RunTestCaseById","assignedTestObjectFromDB==null");
                strError = Resources.mars_exe_sb_no_object_info_fromDB;// "MARS engine doesn't get Object Information from DB.";
                return false;
            }
            Logger.logBegin("RunTestCaseById",string.Format("Test case Name:[{0}-[{1}]] Run order:[{2}] DatasteName:[{3}-[{4}]], database source:[{5}]",
                 assignedTestObjectFromDB.TEST_CASE_NAME, assignedTestObjectFromDB.TEST_CASE_ID ,
                 assignedTestObjectFromDB.RUN_ORDER                                             ,
                 assignedTestObjectFromDB.DATA_SET_ALIAS_NAME                                   ,
                 assignedTestObjectFromDB.DATA_SETTING_ID                                       ,
                 strDBIdx
            ));

            strLastPeg      = "";
            strLastObjName  = "";
            strLastTestSTep = "";
            iLastStepId     = -1;
            strAdv          = "";
            strStack        = "";

            try
            {
               
                /// 算法:
                /// 1, 通知monitor,将要处理的test case和testData
                /// 2，处理所有的test steps
                /// 
                if (wcfClient.WcfClientAgent.IsWcfOffLine())
                {
                    wcfClient.WcfClientAgent.ReconnectTo();
                }
                var mntrCnt = wcfClient.WcfClientAgent.MonitorWcfClient;
                if ((mntrCnt != null))
                {
                    mntrCnt.SetTestStoryboardName(assignedTestObjectFromDB.STORYBOARD_NAME);                    
                    mntrCnt.OnClientTestSuiteTestCaseNamesChangeEvent(assignedTestObjectFromDB.TEST_SUITE_NAME, assignedTestObjectFromDB.TEST_CASE_NAME);
                    mntrCnt.SetTestDataSetName(assignedTestObjectFromDB.DATA_SET_ALIAS_NAME);
                    mntrCnt.OnClientTestSuiteId4ProjectReadyEvent(assignedTestObjectFromDB.STORYBOARD_DETAIL_ID.ToString() ?? "");
                }
                
                if ((assignedTestObjectFromDB.TEST_CASE_ID == -1) || (assignedTestObjectFromDB.DATA_SETTING_ID == null))
                {
                    Logger.Error("RunTestCaseById",strError = string.Format(Resources.mars_exe_sb_tc_id_ds_id_was_not_validate, assignedTestObjectFromDB.TEST_CASE_ID, 
                        assignedTestObjectFromDB.DATA_SETTING_ID));
                    return false;
                }

                ExecutableTestCaseMgr objCurrentTestCaseToExce = new ExecutableTestCaseMgr()
                {
                    CurrentTestCaseId = assignedTestObjectFromDB.TEST_CASE_ID,
                    CurrentDatasetId  = assignedTestObjectFromDB.DATA_SETTING_ID??-1,
                    currentDBIdx      = strDBIdx  ,
                    AutoCheckErrorSet = autoErrorChck
                };
                if ((!CurrentRecoverMgr.isRestoreMode)||(CurrentRecoverMgr.currentSteps==null))
                {
                    CurrentRecoverMgr.currentSteps = new List<ExecutableTestCaseStep>();
                    CurrentRecoverMgr.currentStep = null;
                }
                CurrentRecoverMgr.loopId       = -1;
                bool isOk = true;
                if (!CurrentRecoverMgr.isRestoreMode)
                {
                    //bool isOk = objCurrentTestCaseToExce.LoadTestCase(mntrCnt, lAppId,lstTestStps, ref strError, ref strStack, ref strAdv, strDBIdx);
                    isOk = objCurrentTestCaseToExce.LoadTestCase(mntrCnt, lAppId, CurrentRecoverMgr.currentSteps, ref strError, ref strStack, ref strAdv, strDBIdx);
                    if (!isOk) return false;
                }
                else
                {
                    /// 重运行模式，无需处理
                }
                List<ExecutableTestCaseStep> lstNextStps = new List<ExecutableTestCaseStep>();
                ExecutableTestCaseMgr objNextTestCaseToExce = nextTestCase==null?null:new ExecutableTestCaseMgr()
                {
                    CurrentTestCaseId   = nextTestCase.TEST_CASE_ID,
                    CurrentDatasetId    = nextTestCase.DATA_SETTING_ID ?? -1,
                    currentDBIdx        = strDBIdx
                };

                if (!CurrentRecoverMgr.isRestoreMode)
                {
                    if (changedApplicationId == -1)
                        isOk = objNextTestCaseToExce == null ? true : objNextTestCaseToExce.LoadTestCase(null, lAppId, lstNextStps, ref strError,
                            ref strStack, ref strAdv, strDBIdx);
                    else
                    {
                        //需要重新load test 
                        isOk = objNextTestCaseToExce == null ? true : objNextTestCaseToExce.LoadTestCase(null, changedApplicationId, lstNextStps, ref strError,
                            ref strStack, ref strAdv, strDBIdx);
                    }
                    if (!isOk) return false;
                    /// then get data from data base
                    /// 
                    
                    isOk = objCurrentTestCaseToExce.InstallDataSetToTestStep(mntrCnt, assignedTestObjectFromDB.DATA_SUMMARY_ID,
                        assignedTestObjectFromDB.TEST_CASE_ID,
                        CurrentRecoverMgr.currentSteps, strMode, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("RunTestCaseById", string.Format("InstallDataSetToTestStep Error:[{0}]", strError));
                        StackFrame stck = new StackFrame();
                        strStack = MarsErrorStacks.StackTraceDump();
                        return false;
                    }

                    /// 判断是否是否有runtimeobjects
                    /// 
                    //isOk = objCurrentTestCaseToExce.MatchRunTimeObjects(lAppId,mntrCnt, lstTestStps, ref strError,ref strAdv, ref strStack);
                    isOk = objCurrentTestCaseToExce.MatchRunTimeObjects(lAppId, mntrCnt, CurrentRecoverMgr.currentSteps,
                        StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk,
                        ref strError, ref strAdv, ref strStack);
                    if (!isOk)
                    {
                        Logger.Error("RunTestCaseById", string.Format("Error:[{0}]", strError));
                        return false;
                    }
                    if (objNextTestCaseToExce != null)
                    {
                        isOk = objNextTestCaseToExce.MatchRunTimeObjects(lAppId, mntrCnt, lstNextStps, 
                            StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk, ref strError, ref strAdv, ref strStack);
                        if (!isOk)
                        {
                            Logger.Error("RunTestCaseById", string.Format("next steps Load Error:[{0}]", strError));
                            return false;
                        }
                    }

                    /// then get object quick access and run step by step
                    /// 
                    //isOk = objCurrentTestCaseToExce.MatchObjectQuickAccessInfo(mntrCnt, lAppId, lstTestStps, 
                    //    ref strError,ref strAdv, ref strStack);
                    isOk = objCurrentTestCaseToExce.MatchObjectQuickAccessInfo( mntrCnt, lAppId, CurrentRecoverMgr.currentSteps,
                        StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk,
                        ref strError, ref strAdv, ref strStack);

                    if (!isOk)
                    {
                        Logger.Error("RunTestCaseById", string.Format("Error:[{0}]", strError));
                        return false;
                    }
                    if (objNextTestCaseToExce != null)
                    {
                        isOk = objNextTestCaseToExce.MatchObjectQuickAccessInfo(null, lAppId, lstNextStps,
                            StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk, 
                            ref strError, ref strAdv, ref strStack);
                        if (!isOk)
                        {
                            Logger.Error("RunTestCaseById", string.Format("next steps object load Error:[{0}]", strError));
                            return false;
                        }
                    }
                }
                /// then Run Test step one by one
                /// 
                hasError =false;
                long newAppId = lAppId;
                isOk = objCurrentTestCaseToExce.RunTestStepOneByOne(
                    strCurMarsAccount,
                    ref mntrCnt, 
                    ref newAppId,
                    assignedTestObjectFromDB.TEST_CASE_ID,
                    CurrentRecoverMgr, //.currentSteps,//lstTestStps,                    
                    lstNextStps,
                    isIgnoreError, 
                    strMode,
                    currentStoryboardMgr,
                    ref appTyp,
                    ref strError,
                    ref strAdv, 
                    ref strStack,
                    ref strLastPeg, ref strLastObjName, ref strLastTestSTep, ref iLastStepId,
                    ref hasError,
                    isVerifyValueSkipper);

                Logger.Info("RunTestCaseById", string.Format("result for test case:[{0}]-message:[{1}] hasError:[{2}] appid:[{3}--{4}]", 
                    isOk, strError, hasError, lAppId, newAppId));               

                if (newAppId != lAppId)
                    lAppId = newAppId;                
                return isOk;
            }
            catch(Exception e)
            {
                Logger.Error("RunTestCaseById", strError = Resources.mars_exe_sb_tc_cannot_execute); //"can't execute test case");// string.Format("Exception:[{0}]", e.Message),e );
                return false;// ExecutableTestCase.Run
            }           
        }

        /// <summary>
        /// Builds detailed exception message including FusionLog for assembly loading failures
        /// </summary>
        private static string BuildDetailedExceptionMessage(Exception e)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            // Check if it's a file or assembly loading exception
            if (e is System.IO.FileNotFoundException fileEx)
            {
                sb.AppendLine($"FileNotFoundException Details:");
                sb.AppendLine($"  FileName: {fileEx.FileName}");
                sb.AppendLine($"  FusionLog: {GetFusionLog(fileEx)}");
                
                // Check if it's related to ManagedInjector64-4.0.dll
                if (fileEx.FileName != null && 
                    (fileEx.FileName.IndexOf("ManagedInjector64-4.0", StringComparison.OrdinalIgnoreCase)>=0 ||
                     e.Message.IndexOf("ManagedInjector64-4.0", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    sb.AppendLine($"  This error is related to ManagedInjector64-4.0.dll");
                    sb.AppendLine($"  Missing dependency file: {fileEx.FileName}");
                    sb.AppendLine($"  Diagnostic Information:");
                    
                    // Get current assembly location and check common paths
                    string currentAssemblyPath = typeof(StoryboardExecute).Assembly.Location;
                    if (!string.IsNullOrEmpty(currentAssemblyPath))
                    {
                        string baseDirectory = System.IO.Path.GetDirectoryName(currentAssemblyPath);
                        sb.AppendLine($"    Current assembly directory: {baseDirectory}");
                        
                        // Check if file exists in current directory
                        string[] possiblePaths = new string[]
                        {
                            baseDirectory,
                            System.IO.Path.Combine(baseDirectory, "x64"),
                            System.IO.Path.Combine(baseDirectory, "x86"),
                            System.IO.Path.Combine(baseDirectory, "bin"),
                            System.IO.Path.Combine(baseDirectory, "bin", "x64"),
                            System.IO.Path.Combine(baseDirectory, "bin", "x86"),
                            System.IO.Path.Combine(baseDirectory, ".."),
                            System.IO.Path.Combine(baseDirectory, "..", ".."),
                            System.AppDomain.CurrentDomain.BaseDirectory,
                            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase?.Replace("file:///", "").Replace("/", "\\") ?? "")
                        };
                        
                        sb.AppendLine($"    Checking possible file locations:");
                        bool foundAny = false;
                        foreach (var path in possiblePaths)
                        {
                            if (string.IsNullOrEmpty(path)) continue;
                            
                            try
                            {
                                string fullPath = System.IO.Path.Combine(path, fileEx.FileName ?? "ManagedInjector64-4.0.dll");
                                if (System.IO.File.Exists(fullPath))
                                {
                                    sb.AppendLine($"      ✓ FOUND: {fullPath}");
                                    foundAny = true;
                                    
                                    // Get detailed file information
                                    try
                                    {
                                        var fileInfo = new System.IO.FileInfo(fullPath);
                                        sb.AppendLine($"        File Size: {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0:F2} KB)");
                                        sb.AppendLine($"        Last Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                                        
                                        // Try to get PE architecture
                                        string peArch = GetPEArchitecture(fullPath);
                                        if (!string.IsNullOrEmpty(peArch))
                                        {
                                            sb.AppendLine($"        PE Architecture: {peArch}");
                                        }
                                        
                                        // Check if file is readable
                                        try
                                        {
                                            using (var fs = System.IO.File.OpenRead(fullPath))
                                            {
                                                sb.AppendLine($"        File Access: Readable");
                                            }
                                        }
                                        catch (Exception accessEx)
                                        {
                                            sb.AppendLine($"        File Access: ERROR - {accessEx.Message}");
                                        }
                                        
                                        // Check current process architecture
                                        sb.AppendLine($"        Current Process Architecture: {(IntPtr.Size == 8 ? "x64" : "x86")} ({IntPtr.Size * 8}-bit)");
                                        
                                        // Check for C++/CLI specific dependencies
                                        sb.AppendLine($"        C++/CLI Dependency Check:");
                                        CheckCppCliDependencies(fullPath, sb);
                                        
                                        // Try ReflectionOnlyLoadFrom first (doesn't load native dependencies)
                                        try
                                        {
                                            var testAssembly = System.Reflection.Assembly.ReflectionOnlyLoadFrom(fullPath);
                                            sb.AppendLine($"        ReflectionOnlyLoadFrom: SUCCESS");
                                            sb.AppendLine($"        Assembly FullName: {testAssembly.FullName}");
                                            
                                            // Get referenced assemblies
                                            var referencedAssemblies = testAssembly.GetReferencedAssemblies();
                                            if (referencedAssemblies != null && referencedAssemblies.Length > 0)
                                            {
                                                sb.AppendLine($"        Referenced Assemblies ({referencedAssemblies.Length}):");
                                                foreach (var refAsm in referencedAssemblies.Take(10)) // Limit to first 10
                                                {
                                                    sb.AppendLine($"          - {refAsm.Name}, Version={refAsm.Version}");
                                                    
                                                    // Check if referenced assembly can be found
                                                    try
                                                    {
                                                        var foundAsm = System.Reflection.Assembly.ReflectionOnlyLoad(refAsm.FullName);
                                                        sb.AppendLine($"            ✓ Found: {foundAsm.Location}");
                                                    }
                                                    catch (Exception refLoadEx)
                                                    {
                                                        sb.AppendLine($"            ✗ NOT FOUND: {refLoadEx.Message}");
                                                    }
                                                }
                                                if (referencedAssemblies.Length > 10)
                                                {
                                                    sb.AppendLine($"          ... and {referencedAssemblies.Length - 10} more");
                                                }
                                            }
                                        }
                                        catch (Exception loadEx)
                                        {
                                            sb.AppendLine($"        ReflectionOnlyLoadFrom: FAILED - {loadEx.GetType().Name}: {loadEx.Message}");
                                        }
                                        
                                        // Try actual LoadFrom (this will load native dependencies and reveal the real issue)
                                        sb.AppendLine($"        Attempting actual Assembly.LoadFrom (will load native dependencies):");
                                        try
                                        {
                                            var actualAssembly = System.Reflection.Assembly.LoadFrom(fullPath);
                                            sb.AppendLine($"        ✓ Assembly.LoadFrom: SUCCESS");
                                            sb.AppendLine($"        Location: {actualAssembly.Location}");
                                        }
                                        catch (Exception actualLoadEx)
                                        {
                                            sb.AppendLine($"        ✗ Assembly.LoadFrom: FAILED");
                                            sb.AppendLine($"          Exception Type: {actualLoadEx.GetType().FullName}");
                                            sb.AppendLine($"          Message: {actualLoadEx.Message}");
                                            
                                            if (actualLoadEx.InnerException != null)
                                            {
                                                sb.AppendLine($"          Inner Exception: {actualLoadEx.InnerException.GetType().FullName}");
                                                sb.AppendLine($"          Inner Message: {actualLoadEx.InnerException.Message}");
                                                
                                                // Check for specific C++ runtime errors
                                                string errorMsg = actualLoadEx.InnerException.Message ?? "";
                                                if (errorMsg.IndexOf("VCRUNTIME", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                                    errorMsg.IndexOf("MSVCR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    errorMsg.IndexOf("MSVCP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    errorMsg.IndexOf("vcruntime", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                    errorMsg.IndexOf("msvcp", StringComparison.OrdinalIgnoreCase) >= 0)
                                                {
                                                    sb.AppendLine($"          ⚠ C++ Runtime Library is missing!");
                                                    sb.AppendLine($"          Solution: Install Visual C++ 2022 Redistributable");
                                                    sb.AppendLine($"          Download: https://aka.ms/vs/17/release/vc_redist.x64.exe");
                                                }
                                                
                                                // Check for other common DLL loading errors
                                                if (errorMsg.IndexOf("The specified module could not be found", StringComparison.OrdinalIgnoreCase) >= 0)
                                                {
                                                    sb.AppendLine($"          ⚠ A required DLL module is missing!");
                                                    sb.AppendLine($"          This is typically a native (C++) dependency issue.");
                                                    sb.AppendLine($"          Check VC++ Runtime libraries above.");
                                                }
                                                
                                                if (errorMsg.IndexOf("BadImageFormatException", StringComparison.OrdinalIgnoreCase) >= 0)
                                                {
                                                    sb.AppendLine($"          ⚠ Architecture mismatch or corrupted DLL!");
                                                }
                                            }
                                            
                                            // Try to get more details using Win32 LoadLibrary
                                            sb.AppendLine($"        Attempting Win32 LoadLibrary to get detailed error:");
                                            try
                                            {
                                                IntPtr hModule = LoadLibraryW(fullPath);
                                                if (hModule == IntPtr.Zero)
                                                {
                                                    int errorCode = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                                                    string errorMsg = GetWin32ErrorMessage(errorCode);
                                                    sb.AppendLine($"          Win32 LoadLibrary failed with error {errorCode}: {errorMsg}");
                                                    
                                                    // Common error codes
                                                    if (errorCode == 126) // ERROR_MOD_NOT_FOUND
                                                    {
                                                        sb.AppendLine($"          ERROR_MOD_NOT_FOUND: A dependency DLL is missing!");
                                                        sb.AppendLine($"          Attempting to enumerate DLL dependencies to find missing one:");
                                                        EnumerateDllDependencies(fullPath, sb);
                                                    }
                                                    else if (errorCode == 127) // ERROR_PROC_NOT_FOUND
                                                    {
                                                        sb.AppendLine($"          ERROR_PROC_NOT_FOUND: A required function is missing!");
                                                    }
                                                    else if (errorCode == 193) // ERROR_BAD_EXE_FORMAT
                                                    {
                                                        sb.AppendLine($"          ERROR_BAD_EXE_FORMAT: Architecture mismatch or corrupted file!");
                                                    }
                                                    else
                                                    {
                                                        // For other errors, still try to enumerate dependencies
                                                        sb.AppendLine($"          Attempting to enumerate DLL dependencies:");
                                                        EnumerateDllDependencies(fullPath, sb);
                                                    }
                                                }
                                                else
                                                {
                                                    sb.AppendLine($"          ✓ Win32 LoadLibrary: SUCCESS (unexpected!)");
                                                    FreeLibrary(hModule);
                                                }
                                            }
                                            catch (Exception win32Ex)
                                            {
                                                sb.AppendLine($"          Win32 LoadLibrary exception: {win32Ex.Message}");
                                            }
                                        }
                                    }
                                    catch (Exception fileInfoEx)
                                    {
                                        sb.AppendLine($"        ERROR getting file info: {fileInfoEx.Message}");
                                    }
                                }
                                else
                                {
                                    sb.AppendLine($"      ✗ NOT FOUND: {fullPath}");
                                }
                            }
                            catch (Exception pathEx)
                            {
                                sb.AppendLine($"      ✗ ERROR checking path [{path}]: {pathEx.Message}");
                            }
                        }
                        
                        if (!foundAny)
                        {
                            sb.AppendLine($"    WARNING: ManagedInjector64-4.0.dll not found in any checked location!");
                        }
                        else
                        {
                            sb.AppendLine($"    NOTE: File was found but loading failed. Possible causes:");
                            sb.AppendLine($"      - Architecture mismatch (x86 vs x64)");
                            sb.AppendLine($"      - Missing dependencies (check Referenced Assemblies above)");
                            sb.AppendLine($"      - File corruption or version mismatch");
                            sb.AppendLine($"      - Permission issues (try running as Administrator)");
                        }
                    }
                    
                    // Check for dependency issues using AssemblyResolve
                    sb.AppendLine($"    Assembly Search Paths:");
                    sb.AppendLine($"      ApplicationBase: {System.AppDomain.CurrentDomain.BaseDirectory}");
                    sb.AppendLine($"      PrivateBinPath: {System.AppDomain.CurrentDomain.SetupInformation.PrivateBinPath ?? "Not set"}");
                    sb.AppendLine($"      Probing Paths: {string.Join("; ", System.AppDomain.CurrentDomain.SetupInformation.PrivateBinPathProbe?.Split(';') ?? new string[0])}");
                }
            }
            else if (e is System.BadImageFormatException badImageEx)
            {
                sb.AppendLine($"BadImageFormatException Details:");
                sb.AppendLine($"  FileName: {badImageEx.FileName}");
                sb.AppendLine($"  FusionLog: {GetFusionLog(badImageEx)}");
            }
            else if (e is System.Reflection.ReflectionTypeLoadException refEx)
            {
                sb.AppendLine($"ReflectionTypeLoadException Details:");
                sb.AppendLine($"  LoaderExceptions count: {refEx.LoaderExceptions?.Length ?? 0}");
                if (refEx.LoaderExceptions != null)
                {
                    for (int i = 0; i < refEx.LoaderExceptions.Length; i++)
                    {
                        var loaderEx = refEx.LoaderExceptions[i];
                        if (loaderEx != null)
                        {
                            sb.AppendLine($"  LoaderException[{i}]: {loaderEx.GetType().Name} - {loaderEx.Message}");
                            if (loaderEx is System.IO.FileNotFoundException loaderFileEx)
                            {
                                sb.AppendLine($"    Missing file: {loaderFileEx.FileName}");
                                sb.AppendLine($"    FusionLog: {GetFusionLog(loaderFileEx)}");
                            }
                        }
                    }
                }
            }
            
            // Check inner exception recursively
            if (e.InnerException != null)
            {
                sb.AppendLine($"Inner Exception Details:");
                sb.AppendLine(BuildDetailedExceptionMessage(e.InnerException));
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Gets FusionLog from an exception if available
        /// </summary>
        private static string GetFusionLog(Exception ex)
        {
            try
            {
                // Method 1: Try to get FusionLog property directly
                var fusionLogProperty = ex.GetType().GetProperty("FusionLog", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                
                if (fusionLogProperty != null)
                {
                    var fusionLog = fusionLogProperty.GetValue(ex) as string;
                    if (!string.IsNullOrEmpty(fusionLog))
                    {
                        return fusionLog;
                    }
                }
                
                // Method 2: Try to get it from base class (FileNotFoundException inherits from IOException)
                var baseType = ex.GetType().BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    fusionLogProperty = baseType.GetProperty("FusionLog", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    if (fusionLogProperty != null)
                    {
                        var fusionLog = fusionLogProperty.GetValue(ex) as string;
                        if (!string.IsNullOrEmpty(fusionLog))
                        {
                            return fusionLog;
                        }
                    }
                    baseType = baseType.BaseType;
                }
                
                // Method 3: Try using FieldInfo if property doesn't work
                var fusionLogField = ex.GetType().GetField("_fusionLog", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fusionLogField != null)
                {
                    var fusionLog = fusionLogField.GetValue(ex) as string;
                    if (!string.IsNullOrEmpty(fusionLog))
                    {
                        return fusionLog;
                    }
                }
            }
            catch (Exception reflectionEx)
            {
                // Return reflection error for debugging
                return $"FusionLog reflection error: {reflectionEx.Message}";
            }
            
            return "FusionLog not available (Note: Enable Fusion logging in registry or use fuslogvw.exe to view detailed binding logs)";
        }

        /// <summary>
        /// Checks for C++/CLI specific dependencies (VC++ Runtime, etc.)
        /// </summary>
        private static void CheckCppCliDependencies(string dllPath, System.Text.StringBuilder sb)
        {
            try
            {
                // Based on ManagedInjector64-4.0.vcxproj: PlatformToolset=v143 (Visual Studio 2022)
                // This requires Visual C++ 2022 Redistributable (v143 runtime)
                sb.AppendLine($"          Required: Visual C++ 2022 Redistributable (v143)");
                sb.AppendLine($"          Project uses PlatformToolset v143 (Visual Studio 2022)");
                sb.AppendLine($"          RuntimeLibrary: MultiThreadedDLL / MultiThreadedDebugDLL");
                
                // Check for Visual C++ Runtime DLLs (prioritize v143 for VS2022)
                // v143 uses the same runtime as v142 (vcruntime140.dll, msvcp140.dll, etc.)
                string[] vcRuntimeDlls = new string[]
                {
                    // Visual C++ 2022 (v143) - uses same runtime as 2019/2017
                    "vcruntime140.dll", "vcruntime140_1.dll", "vcruntime140_2.dll",
                    "msvcp140.dll", "msvcp140_1.dll", "msvcp140_2.dll",
                    "msvcp140_atomic_wait.dll", "concrt140.dll",
                    // Also check older versions for compatibility
                    "msvcr120.dll", "msvcr140.dll", "msvcr141.dll", "msvcr142.dll",
                    "msvcp120.dll", "msvcp141.dll", "msvcp142.dll"
                };
                
                // Get system directories
                string system32 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
                string sysWOW64 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.SystemX86);
                string[] searchPaths = new string[]
                {
                    system32,
                    sysWOW64,
                    System.IO.Path.GetDirectoryName(dllPath),
                    System.AppDomain.CurrentDomain.BaseDirectory
                };
                
                sb.AppendLine($"          Checking VC++ Runtime Libraries:");
                bool foundAnyVcRuntime = false;
                foreach (var vcDll in vcRuntimeDlls)
                {
                    bool found = false;
                    foreach (var searchPath in searchPaths)
                    {
                        if (string.IsNullOrEmpty(searchPath)) continue;
                        try
                        {
                            string fullPath = System.IO.Path.Combine(searchPath, vcDll);
                            if (System.IO.File.Exists(fullPath))
                            {
                                sb.AppendLine($"            ✓ {vcDll} found at: {fullPath}");
                                found = true;
                                foundAnyVcRuntime = true;
                                break;
                            }
                        }
                        catch { }
                    }
                    if (!found)
                    {
                        sb.AppendLine($"            ✗ {vcDll} not found");
                    }
                }
                
                if (!foundAnyVcRuntime)
                {
                    sb.AppendLine($"          ⚠ WARNING: No VC++ Runtime libraries found!");
                    sb.AppendLine($"          Solution: Install Visual C++ 2022 Redistributable");
                    sb.AppendLine($"          Download: https://aka.ms/vs/17/release/vc_redist.x64.exe (for x64)");
                    sb.AppendLine($"          Or: https://aka.ms/vs/17/release/vc_redist.x86.exe (for x86)");
                    sb.AppendLine($"          Note: ManagedInjector64-4.0.dll requires v143 runtime (VS2022)");
                }
                else
                {
                    // Check if the correct version is found
                    bool foundV143Runtime = false;
                    foreach (var vcDll in new string[] { "vcruntime140.dll", "msvcp140.dll" })
                    {
                        foreach (var searchPath in searchPaths)
                        {
                            if (string.IsNullOrEmpty(searchPath)) continue;
                            try
                            {
                                string fullPath = System.IO.Path.Combine(searchPath, vcDll);
                                if (System.IO.File.Exists(fullPath))
                                {
                                    foundV143Runtime = true;
                                    break;
                                }
                            }
                            catch { }
                        }
                        if (foundV143Runtime) break;
                    }
                    
                    if (foundV143Runtime)
                    {
                        sb.AppendLine($"          ✓ VC++ 2015-2022 Redistributable appears to be installed");
                    }
                }
                
                // Check for .NET Framework
                sb.AppendLine($"          .NET Framework Version: {System.Environment.Version}");
                sb.AppendLine($"          CLR Version: {System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()}");
                
                // Check for common Windows DLLs that C++/CLI might need
                string[] commonDlls = new string[] { "kernel32.dll", "user32.dll", "gdi32.dll" };
                sb.AppendLine($"          Checking Windows System DLLs:");
                foreach (var dll in commonDlls)
                {
                    string fullPath = System.IO.Path.Combine(system32, dll);
                    if (System.IO.File.Exists(fullPath))
                    {
                        sb.AppendLine($"            ✓ {dll} found");
                    }
                    else
                    {
                        sb.AppendLine($"            ✗ {dll} NOT FOUND (this is unusual!)");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"          ERROR checking dependencies: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the PE (Portable Executable) architecture of a DLL file
        /// </summary>
        private static string GetPEArchitecture(string filePath)
        {
            try
            {
                using (var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                using (var br = new System.IO.BinaryReader(fs))
                {
                    // Read DOS header
                    if (fs.Length < 64) return "Invalid file size";
                    
                    // Check DOS signature (MZ)
                    fs.Position = 0;
                    ushort dosSignature = br.ReadUInt16();
                    if (dosSignature != 0x5A4D) // "MZ"
                        return "Not a valid PE file";
                    
                    // Get PE header offset
                    fs.Position = 0x3C;
                    int peHeaderOffset = br.ReadInt32();
                    
                    if (peHeaderOffset >= fs.Length) return "Invalid PE header offset";
                    
                    // Read PE signature
                    fs.Position = peHeaderOffset;
                    uint peSignature = br.ReadUInt32();
                    if (peSignature != 0x00004550) // "PE\0\0"
                        return "Invalid PE signature";
                    
                    // Read machine type (architecture)
                    ushort machineType = br.ReadUInt16();
                    
                    switch (machineType)
                    {
                        case 0x014c: // IMAGE_FILE_MACHINE_I386
                            return "x86 (32-bit)";
                        case 0x8664: // IMAGE_FILE_MACHINE_AMD64
                            return "x64 (64-bit)";
                        case 0x0200: // IMAGE_FILE_MACHINE_IA64
                            return "IA64 (Itanium)";
                        case 0x01c4: // IMAGE_FILE_MACHINE_ARMNT
                            return "ARM";
                        case 0xAA64: // IMAGE_FILE_MACHINE_ARM64
                            return "ARM64";
                        default:
                            return $"Unknown (0x{machineType:X4})";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error reading PE: {ex.Message}";
            }
        }

        // Win32 API declarations for detailed DLL loading diagnostics
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int FormatMessage(
            int dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            int dwLanguageId,
            StringBuilder lpBuffer,
            int nSize,
            IntPtr Arguments);

        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;

        /// <summary>
        /// Gets Win32 error message by error code
        /// </summary>
        private static string GetWin32ErrorMessage(int errorCode)
        {
            try
            {
                StringBuilder message = new StringBuilder(255);
                int result = FormatMessage(
                    FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                    IntPtr.Zero,
                    errorCode,
                    0,
                    message,
                    message.Capacity,
                    IntPtr.Zero);

                if (result > 0)
                {
                    return message.ToString().Trim();
                }
            }
            catch { }

            return $"Error code: {errorCode}";
        }

        /// <summary>
        /// Attempts to enumerate DLL dependencies by reading PE import table
        /// This helps identify which specific DLL is missing
        /// </summary>
        private static void EnumerateDllDependencies(string dllPath, System.Text.StringBuilder sb)
        {
            try
            {
                using (var fs = new System.IO.FileStream(dllPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                using (var br = new System.IO.BinaryReader(fs))
                {
                    // Read DOS header
                    fs.Position = 0;
                    ushort dosSignature = br.ReadUInt16();
                    if (dosSignature != 0x5A4D) // "MZ"
                    {
                        sb.AppendLine($"            Cannot read PE file structure");
                        return;
                    }

                    // Get PE header offset
                    fs.Position = 0x3C;
                    int peHeaderOffset = br.ReadInt32();
                    if (peHeaderOffset >= fs.Length) return;

                    // Read PE signature
                    fs.Position = peHeaderOffset;
                    uint peSignature = br.ReadUInt32();
                    if (peSignature != 0x00004550) return; // "PE\0\0"

                    // Read machine type and skip to Optional Header
                    fs.Position = peHeaderOffset + 4;
                    ushort machineType = br.ReadUInt16();
                    ushort numberOfSections = br.ReadUInt16();
                    fs.Position += 12; // Skip timestamp, pointer to symbol table, number of symbols
                    ushort sizeOfOptionalHeader = br.ReadUInt16();
                    ushort characteristics = br.ReadUInt16();

                    // Read Optional Header to find Import Table RVA
                    int optionalHeaderOffset = (int)fs.Position;
                    ushort magic = br.ReadUInt16(); // PE32 or PE32+
                    
                    int importTableRVA = 0;
                    if (machineType == 0x8664) // x64
                    {
                        fs.Position = optionalHeaderOffset + 112; // Import Table RVA offset in PE32+
                        importTableRVA = br.ReadInt32();
                    }
                    else // x86
                    {
                        fs.Position = optionalHeaderOffset + 96; // Import Table RVA offset in PE32
                        importTableRVA = br.ReadInt32();
                    }

                    if (importTableRVA == 0)
                    {
                        sb.AppendLine($"            No import table found (static library?)");
                        return;
                    }

                    // Find the section containing the import table
                    int sectionTableOffset = optionalHeaderOffset + sizeOfOptionalHeader;
                    int importTableFileOffset = 0;
                    
                    for (int i = 0; i < numberOfSections; i++)
                    {
                        fs.Position = sectionTableOffset + i * 40;
                        fs.Position += 8; // Skip section name
                        int virtualAddress = br.ReadInt32();
                        int sizeOfRawData = br.ReadInt32();
                        int pointerToRawData = br.ReadInt32();

                        if (importTableRVA >= virtualAddress && importTableRVA < virtualAddress + sizeOfRawData)
                        {
                            importTableFileOffset = pointerToRawData + (importTableRVA - virtualAddress);
                            break;
                        }
                    }

                    if (importTableFileOffset == 0)
                    {
                        sb.AppendLine($"            Could not locate import table in sections");
                        return;
                    }

                    // Read import descriptors
                    sb.AppendLine($"            DLL Dependencies (from PE Import Table):");
                    fs.Position = importTableFileOffset;
                    
                    List<string> dependencies = new List<string>();
                    while (true)
                    {
                        int originalFirstThunk = br.ReadInt32();
                        int timeDateStamp = br.ReadInt32();
                        int forwarderChain = br.ReadInt32();
                        int nameRVA = br.ReadInt32();
                        int firstThunk = br.ReadInt32();

                        if (nameRVA == 0) break; // End of import table

                        // Find section containing the name
                        int nameFileOffset = 0;
                        for (int i = 0; i < numberOfSections; i++)
                        {
                            fs.Position = sectionTableOffset + i * 40;
                            fs.Position += 8;
                            int virtualAddress = br.ReadInt32();
                            int sizeOfRawData = br.ReadInt32();
                            int pointerToRawData = br.ReadInt32();

                            if (nameRVA >= virtualAddress && nameRVA < virtualAddress + sizeOfRawData)
                            {
                                nameFileOffset = pointerToRawData + (nameRVA - virtualAddress);
                                break;
                            }
                        }

                        if (nameFileOffset > 0)
                        {
                            fs.Position = nameFileOffset;
                            List<byte> nameBytes = new List<byte>();
                            byte b;
                            while ((b = br.ReadByte()) != 0)
                            {
                                nameBytes.Add(b);
                            }
                            string dllName = System.Text.Encoding.ASCII.GetString(nameBytes.ToArray());
                            dependencies.Add(dllName);
                        }
                    }

                    if (dependencies.Count > 0)
                    {
                        foreach (var dep in dependencies)
                        {
                            // Check if dependency exists
                            string system32 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.System);
                            string sysWOW64 = System.Environment.GetFolderPath(System.Environment.SpecialFolder.SystemX86);
                            string dllDir = System.IO.Path.GetDirectoryName(dllPath);
                            string appBase = System.AppDomain.CurrentDomain.BaseDirectory;

                            bool found = false;
                            foreach (var searchPath in new[] { system32, sysWOW64, dllDir, appBase })
                            {
                                if (string.IsNullOrEmpty(searchPath)) continue;
                                try
                                {
                                    string depPath = System.IO.Path.Combine(searchPath, dep);
                                    if (System.IO.File.Exists(depPath))
                                    {
                                        sb.AppendLine($"            ✓ {dep} found at: {depPath}");
                                        found = true;
                                        break;
                                    }
                                }
                                catch { }
                            }
                            if (!found)
                            {
                                sb.AppendLine($"            ✗ {dep} NOT FOUND (this is likely the missing dependency!)");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine($"            Could not enumerate dependencies from PE structure");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"            Error enumerating dependencies: {ex.Message}");
                sb.AppendLine($"            StackTrace: {ex.StackTrace}");
            }
        }

        
    }
}
