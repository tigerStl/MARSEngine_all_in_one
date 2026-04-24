extern alias clientWCF;
//extern alias inject4_64;

using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using clientWCF::TestFlowClient;
//using inject4_64::ManagedInjector;
using ManagedInjector;
using Mars.AutoTestingDriver.db;
using Mars.AutoTestingDriver.ErrorMessage;
using Mars.AutoTestingDriver.ExecuteStoryboard;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.AutoTestingDriver.referenceSources.configuration;
using Mars.AutoTestingDriver.SystemUtil;
//using Mars.AutoTestingDriver.testType;
using Mars.message.Business;
using Mars.message.DataLayer;
using Mars.message.Inter.MQCenter.cfg;
using Mars.message.Inter.MQCenter.interProcess;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.SourceCode.xmlConfig;
//using Mars.AutoTestingDriver.SystemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using XmlCompareLib;
using System.Windows.Forms;
using Mars.AutoTestingDriver.ExecuteTestcase;
using Mars.AutoTestingDriver.MarsMessageCenter;
using Mars.message.Utility;
using System.Collections.Specialized;
using System.Collections;
using Mars.message.Inter.MQCenter.HttpRestService;
//using Mars.AutoTestingDriver.notifyManagement;
using Newtonsoft.Json;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.baseInterfaceAndClass;
using Mars.AutoTestingDriver.webSupport.TestDialog;
using System.IO.Compression;
using MARSCoreMessageCenter.basicData;
using MarsEnginer.windowsWrapper.SystemUtil;
using Mars.message.Inter.MQCenter.simpleLog;
#if _forWebClient
//using System.Deployment.Application;
#endif

/// <summary>
/// That is Mars auto Testing Driver to replace QTP
/// it will start from Mars UI. And start WCF then Load Test Client fetching 
/// Test steps information from Framework. 
/// Once the test steps information fetched. IT will run steps one by one and switch to next Test case like what QTP hosted
/// When a keyword is going to be executed. The application will check wether that is a keywords needs information from 
/// target application. If it requires, application would check wthether the injector worked. If not then 
/// 
/// into
/// target application. and connect to injector's wcf msmq server. 
/// Then, send object requirest to msmq server and get object visible, enable information. 
/// if object is available, then set absolute position informations or other information back. 
/// The injector can also enable reflection to display all target objects. 
/// 
/// The Application will connect to Database directly.
/// 
/// -----version 0.5,tiger, 2018-4-17
/// </summary>
namespace Mars.AutoTestingDriver
{

    public class AutoTestingDriverEntry
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(AutoTestingDriverEntry));
        private static bool isVerifyValueSkipper = false;

        public const string cnst_uri_command_clipboard = "-FromClipboard";

        private static string[] CombinParaFromURI(string strURI, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            //standard parameters sample:admin -S TPG_COMPLETE_DEMO 383507 -App 14 -Mode Base -Continue False -IgnoreError False
            try
            {
                //url:http://mars05.eastus.cloudapp.azure.com/Download/Mars.AutoTestingDriver.application?currentDB=MarsEntities&userName=admin%25commmd=-S&storbyBoadName=TPG_COMPLETE_DEMO
                //url sample after 5-27-2022  add an uuid
                // http://localhost:8152/MARSENGINE/Mars.AutoTestingDriver.application?userName=Regr1&command=-S&storyBoadName=Keyword%20Testing&storyBoardId=636390&app=213&Mode=Base&Continue=False&IgnoreError=False&currentDB=GEN_MARS_11&uuid=uuid42667-1092-51685-46186
                //&&storyBoardId=383507&app=14&Mode=Base&Continue=False&IgnoreError=False
                // from 2023, a new parameter is added 
                // callBackServer=localhost:56422 
                // http://localhost:8152/MARSENGINE/Mars.AutoTestingDriver.application?command=objectRegTool&uuid=xxxxx-xxxx-xxxx-xxxxx
                List<string> lstParas = new List<string>();
                Uri uriFromClick = new Uri(strURI);

                // http://localhost:8152/MARSENGINE/Mars.AutoTestingDriver.application?command=startEngineSvc

                var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get("command");
                /// 5-25-2022后，增加uuid
                MarsGlobarVar.UUID_FROM_WEB = query.Get(MarsConstants.CNST_QUERY_URL_UUID_PARA_NAME);
                string strCurrentDB = query.Get("currentDB");                
                ///sample url
                /// http://localhost:56421/StoryBoard/UpdateStoryboardStatus?sessionId=uuid12175&type=step&isok=False&stepId=1&message=success&storyboardId=1&lSchema=5 
                //MarsGlobarVar.MARS_WEB_HOST = $"{uriFromClick.Scheme}://{uriFromClick.Host}:{uriFromClick.Port}/Storyboard/UpdateStoryboardStatus?sessionId={MarsGlobarVar.UUID_FROM_WEB}";
                //MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK = $"{uriFromClick.Scheme}://{uriFromClick.Host}:{uriFromClick.Port}/StoryBoard/UpdateStoryboardExecuteStatus?";
                MarsRESTfulApiClient restClient = new MarsRESTfulApiClient(strCurrentDB);
                MarsGlobarVar.MARS_WEB_HOST = $"{restClient.webURLPreFix}Storyboard/UpdateStoryboardStatus?sessionId={MarsGlobarVar.UUID_FROM_WEB}";
                MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK = $"{restClient.webURLPreFix}StoryBoard/UpdateStoryboardExecuteStatus?";
                Console.WriteLine($"Links|{MarsGlobarVar.MARS_WEB_HOST}|\r\n{MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK}");

                //Console.ReadLine();
                
                string strUserName = query.Get("userName");
                Console.WriteLine($"get userName {strUserName}");
                
                lstParas.Add(strUserName);

                //Console.WriteLine($"get command {strCmd}");
                lstParas.Add(strCmd);
                string strStoryBoardName = query.Get("storyBoardName");
                if (string.IsNullOrEmpty(strStoryBoardName))
                    strStoryBoardName = query.Get("storyBoadName");
                //Console.WriteLine($"get storyBoardName {strStoryBoardName}");
                if ((!string.IsNullOrEmpty(strStoryBoardName)) && (strStoryBoardName.Contains(" ")))
                {
                    strStoryBoardName = strStoryBoardName.Replace("\"", "'");
                    strStoryBoardName = $"\"{strStoryBoardName}\"";
                }
                lstParas.Add(strStoryBoardName);
                string strStoryboardId = query.Get("storyBoardId");

                //Console.WriteLine($"get storyBoardId [{strStoryboardId}]");
                long tmpL = -1;
                if (!long.TryParse(strStoryboardId, out tmpL))
                {
                    strError = "StoryboardId from URL is not a number";
                    strAdv = "Please start Engine from MARS GUI, if error continues, please contact Marquis";
                    strStack = MarsErrorStacks.StackTraceDump();
                    isOk = false;
                    return null;
                }
                lstParas.Add(strStoryboardId);
                string strAppId = query.Get("app");
                //Console.WriteLine($"get app [{strAppId}]");
                if (!long.TryParse(strStoryboardId, out tmpL))
                {
                    strError = "Application Id from URL is not a number";
                    strAdv = "Please start Engine from MARS GUI, if error continues, please contact Marquis";
                    strStack = MarsErrorStacks.StackTraceDump();
                    isOk = false;
                    return null;
                }
                lstParas.Add("-App");
                lstParas.Add(strAppId);
                string strMode = query.Get("Mode");
                //Console.WriteLine($"get Mode [{strMode}]");
                if (string.IsNullOrEmpty(strMode))
                {
                    strError = "Test Mode from URL is empty or null";
                    strAdv = "please start Engine from MARS GUI, if error continues, please contact Marquis";
                    strStack = MarsErrorStacks.StackTraceDump();
                    isOk = false;
                    return null;
                }
                lstParas.Add("-Mode");
                lstParas.Add(strMode);

                string strContinue = query.Get("Continue");
                if (string.IsNullOrEmpty(strContinue))
                {
                    strError = "Parameter [continue] from URL is empty or null";
                    strAdv = "please start Engine from MARS GUI, if error continues, please contact Marquis";
                    strStack = MarsErrorStacks.StackTraceDump();
                    isOk = false;
                    return null;
                }
                lstParas.Add("-Continue");
                lstParas.Add(strContinue);

                string strIgnoreError = query.Get("IgnoreError");
                lstParas.Add("-IgnoreError");
                lstParas.Add(strIgnoreError);

                lstParas.Add("-CurrentDB");
                lstParas.Add(strCurrentDB);

                MarsGlobarVar.MARS_current_StoryboardId = strStoryboardId;
                MarsGlobarVar.MARS_CURRENT_DB = strCurrentDB;
                //admin -S TPG_COMPLETE_DEMO 383507 -App 14 -Mode Base -Continue False -IgnoreError False

                isOk = true;
                return lstParas.ToArray();
            }
            catch (Exception e)
            {
                isOk = false;
                strError = $"Error while Alyast parameters";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
                isOk = false;
                return null;
            }
        }

        private static bool WriteCurrentMarsAccountFile(string strMarsAccount, ref string strError)
        {
            try
            {
                string strPath = System.IO.Path.GetDirectoryName(typeof(AutoTestingDriverEntry).Assembly.Location);
                string strCurrentWindowSystemAccount = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                strCurrentWindowSystemAccount = strCurrentWindowSystemAccount.Replace("\\", "_");
                strCurrentWindowSystemAccount = strCurrentWindowSystemAccount.Replace("/", "_");
                string userAccountNameFile = System.IO.Path.Combine(strPath, $"MarsCrntAccount_{strCurrentWindowSystemAccount}.txt");
                if (System.IO.File.Exists(userAccountNameFile))
                {
                    System.IO.File.Delete(userAccountNameFile);
                }
                System.IO.File.WriteAllText(userAccountNameFile, $"{strMarsAccount}-{strCurrentWindowSystemAccount}");
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                Console.WriteLine($"Exception, {e.Message}\r\n{e.StackTrace}");
                return false;
            }

        }
        /// <summary>
        /// sample http://localhost:8152/MARSENGINE/Mars.AutoTestingDriver.application?command=objectRegTool&uuid=xxxxx-xxxx-xxxx-xxxxx
        /// 2025 http://localhost:8190/MARSENGINE/Mars.AutoTestingDriver.application?uuid=66998987-553b-46af-9540-9144ad14a7ed&type=RecordReplay&db=GEN_MARS_5
        /// </summary>
        /// <param name="strURI"></param>
        /// <returns></returns>
        private static bool isToStartMarsSpy(string strURI, ref string addressAndPort, ref NameValueCollection query)
        {
            int iMark = new Random().Next();
            Logger.Info($"isToStartMarsSpy {iMark}|begin|{strURI}");
            try
            {
                List<string> lstParas = new List<string>();
                Uri uriFromClick = new Uri(strURI);
                query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get("command");
                Logger.Info($"{iMark}|command|{strCmd}");
                addressAndPort = $"{uriFromClick.Scheme}://{uriFromClick.Authority}/";
                if (string.Compare(MarsConstants.CNST_QUERY_URL_COMMAND_OBJTOOL, strCmd ?? "", true) == 0)
                {
                    return true;
                }

                return false;
            }
            finally
            {
                Logger.Info($"isToStartMarsSpy|{iMark}|end.");
            }
        }

        private static bool isToStartMarsSVC(string strURI, ref string addressAndPort, ref NameValueCollection query)
        {
            int iMark = new Random().Next();
            Logger.Info($"isToStartMarsSpy {iMark}|begin|{strURI}");
            try
            {
                List<string> lstParas = new List<string>();
                Uri uriFromClick = new Uri(strURI);
                query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get("command");
                Logger.Info($"{iMark}|command|{strCmd}");

                if ((!string.IsNullOrEmpty(strCmd)) && (string.Compare(strCmd, MarsConstants.CNST_QUERY_CMD_START_SVC, true) == 0))
                {
                    // start socket svc
                    //MarsConsoleNotifyMgr.CreateNotifyAndListen();
                    //MarsConsoleNotifyMgr.CreateNotifyAndListen();
                    // hide the console window
                    //HiddenCurrrentConsole();
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Error("isToStartMarsSVC", e.Message, e);

                return false;
            }
            finally
            {
                Logger.Info($"isToStartMarsSpy|{iMark}|end.");
            }
        }

        /// <summary>
        /// Detects startMessageAgent from HTTP query: cmd=startMessageAgent&amp;sessionId=...&amp;marsWebSocketServerPort=...
        /// </summary>
        private static bool isToStartMessageAgent(string strURI, ref NameValueCollection query)
        {
            try
            {
                Uri uriFromClick = new Uri(strURI);
                query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get("cmd") ?? query.Get("command");
                if (string.IsNullOrEmpty(strCmd) || !string.Equals(strCmd, MARSMessageCenterAgentStarter.CmdStartMessageAgent, StringComparison.OrdinalIgnoreCase))
                    return false;
                string sessionId = query.Get("sessionId");
                string portStr = query.Get("marsWebSocketServerPort");
                if (string.IsNullOrWhiteSpace(portStr) || !int.TryParse(portStr.Trim(), out int port) || port <= 0)
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int StartMessageAgentMode(NameValueCollection query)
        {
            string sessionId = query?.Get("sessionId") ?? string.Empty;
            string portStr = query?.Get("marsWebSocketServerPort")?.Trim() ?? "0";
            if (!int.TryParse(portStr, out int marsWebSocketServerPort) || marsWebSocketServerPort <= 0)
            {
                MessageBox.Show("Invalid marsWebSocketServerPort. Please start from MARS GUI.", "MaRS Engine", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return MarsDriverConst.exit_code_error_paraFormat;
            }
            var starter = new MARSMessageCenterAgentStarter(sessionId, marsWebSocketServerPort);
            var handshakeThread = new Thread(() => starter.WaitForHandshake(30000)) { IsBackground = true };
            handshakeThread.Start();
            Thread.Sleep(800);
            if (!starter.StartAgent(out string err))
                return MarsDriverConst.exit_code_error_paraFormat;
            handshakeThread.Join(32000);
            return 0;
        }
        /// 


        private static bool isToStartMarsRecordAndReplay(string strURI, ref string addressAndPort, ref NameValueCollection query)
        {
            int iMark = new Random().Next();
            Logger.Info($"{iMark}|begin|{strURI}");
            try
            {
                List<string> lstParas = new List<string>();
                Uri uriFromClick = new Uri(strURI);
                query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get("type");
                Logger.Info($"{iMark}|type|{strCmd}");
                addressAndPort = $"{uriFromClick.Scheme}://{uriFromClick.Authority}/";
                if (string.Compare(MarsConstants.CNST_SPYTOOL_PARA_MODE_RECTC, strCmd ?? "", true) == 0)
                {
                    return true;
                }

                return false;
            }
            finally
            {
                Logger.Info($"{iMark}|end.");
            }
        }

        private static bool isToStartObjRecorgMode(string strURI, ref string addressAndPort, ref string strCmmd, ref NameValueCollection query)
        {
            int iMark = new Random().Next();
            Logger.Info($"{iMark}|isToStartObjRecorgMode|begin|{strURI}");
            try
            {
                List<string> lstParas = new List<string>();
                Uri uriFromClick = new Uri(strURI);
                query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strType = query.Get("type");
                Logger.Info($"{iMark}|type|{strType}");
                addressAndPort = $"{uriFromClick.Scheme}://{uriFromClick.Authority}/";
                if (string.Compare(MarsConstants.CNST_QUERY_URL_OBJREC, strType ?? "", true) == 0)
                {
                    strCmmd = MarsConstants.CNST_QUERY_URL_OBJREC;
                    return true;
                }
                if (string.Compare(MarsConstants.CNST_QUERY_URL_RECORDREPLAY, strType ?? "", true) == 0)
                {
                    strCmmd = MarsConstants.CNST_QUERY_URL_RECORDREPLAY;
                    return true;
                }
                
                return false;
            }
            finally
            {
                Logger.Info($"{iMark}|end.|{strCmmd}|{query}");
            }
        }

        private static bool IsTmpStoryboardAndTestStepsMode(string strURI)
        {
            //userName=tiger&command=-FromClipboard&storyBoadName=temp&storyBoardId=-1&app=213&guid=a1b06b48-041f-442c-94eb-d7480c57a647&currentDB=GEN_MARS_10	
            List<string> lstParas = new List<string>();
            Uri uriFromClick = new Uri(strURI);
            var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
            string strCmd = query.Get("command");
            if ((string.IsNullOrEmpty(strCmd)) || (!cnst_uri_command_clipboard.Equals(strCmd, StringComparison.OrdinalIgnoreCase))) return false;
            StoryboardExecute.TestStepsFromJsonInfo = new MarsClipboardURLPara();
            bool isOk = StoryboardExecute.TestStepsFromJsonInfo.ParseURL(strURI);
            if (!isOk) return false;
            return true;
            //return false;
            //return TestTypeParameter_StepsMode.ParseStepsModePara(arrParas);
        }


        public const string cnst_para_source_jenkins = "FromJenKins";

        private static void KillSameProcesss()
        {
            Console.WriteLine("begin to kill Mars.AutoTestingDriver.exe");
            var p = Process.GetCurrentProcess();
            string strProcssName = p.ProcessName;
            var lstProcess = Process.GetProcessesByName(strProcssName);
            foreach (var itm in lstProcess)
            {
                if (itm == null) continue;
                if (itm.SessionId != p.SessionId) continue;
                try
                {
                    if (itm.Id == p.Id) continue;
                    itm.Kill();
                    Console.WriteLine($"\tkill process:[id-{itm.Id}]-curId:[{p.Id}]");
                }
                catch (Exception)
                {

                }
            }
        }

        public static void SetWindowPosition(int x, int y, int width, int height)
        {
            MarsWindowsAPIs.SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, MarsWindowsAPIs.SWP_NOZORDER | MarsWindowsAPIs.SWP_NOSIZE);
        }

        public static IntPtr Handle
        {
            get
            {
                //Initialize();
                return MarsWindowsAPIs.GetConsoleWindow();
            }
        }

        /// <summary>
        /// 不同的系统有不同的参数
        /// tigertest admin -S D1_BOND_DEFN_ENTRY 107189 -App 2 -Mode Base -Continue False -IgnoreError False
        /// 1，从jenkins等系统 解构如下：
        ///    -source FromJenkins -S [PROJECTNAME].[D1_BOND_DEFN_ENTRY] -AppName Summit6.2 -Continue False -IsQuiteMode true
        /// 2, 启动SMT进程
        ///     -SMT DEFAULT_SUMMIT_SMT_LOGIN -AppName SMT -currentDB GEN_MARS_5
        /// 3, 启动Xpath spy
        /// -WEBSPY admin -S TEST_WPF_CORE 1121256 -App 1000018 -Mode Base -Continue False -IgnoreError False -CurrentDB GEN_MARS_5
        /// 
        /// ------------------------------------------------------------------------------------------------------------------------
        /// 2025-10-2 增加使用UIA技术和IAccessible技术。因此，不需要将进程注入到目标进程中
        /// 如果application设置为STANDARD DESKTOP，就表示使用UIA技术，不需要使用注入
        /// 对于标准的应用程序，如使用atl，mfc等，对象的唯一性较低。因为UIA无法唯一定义对象。需要增加PositionIndex或者Position Index定位
        /// ------------------------------------------------------------------------------------------------------------------------
        /// 
        /// </summary>
        /// <param name="args"></param>
        //[STAThread]
        static int Main(string[] args)
        {
            try
            {
                NameValueCollection query = new NameValueCollection();
                string strAddressAndPort = "";


                Console.WindowHeight = 20;
                Console.WindowWidth = 80;
                SetWindowPosition(100, Screen.PrimaryScreen.Bounds.Height - 300, 500, 100);
                Console.Title = "MARS Test console";
                Console.WriteLine("MARS BEGIN....");
                //Console.SetWindowPosition(0, Screen.PrimaryScreen.Bounds.Height - 200);

                #region test 
                //string strPath = System.IO.Path.GetDirectoryName(typeof(AutoTestingDriverEntry).GetType().Assembly.Location);
                //string strCurrentWindowSystemAccount = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                //MessageBox.Show($"{ strCurrentWindowSystemAccount} ");
                //strCurrentWindowSystemAccount = strCurrentWindowSystemAccount.Replace("\\", "_");
                //strCurrentWindowSystemAccount = strCurrentWindowSystemAccount.Replace("/", "_");
                //MessageBox.Show($"{ strCurrentWindowSystemAccount} ");
                #endregion
                MarsLoggerSimple.logfile_suffix = "_FromStarter";

                string strError = "", strAdv = "", strStack = "";
                bool isOk = false;
                int iMonitorPort = -1;

                /// kill other applications 
                /// 
                KillSameProcesss();

                CommdLineOptions options = new CommdLineOptions();               
                if ((args.Length > 0) && (args[0].StartsWith("-")))
                {
                    options.init(args, 1);
                    string strSpyPID = options.GetOptionStringValue("-spy");
                    if (!string.IsNullOrEmpty(strSpyPID))
                    {
                        //进行object spy处理
                        //判断是不是已经注册
                        Console.WriteLine("Command line:" + string.Join(",", args));
                        string targetWnd = options.GetOptionStringValue("-targetWnd");
                        bool isRecall = options.GetOptionBooleanValue("-isRecall");
                        string strUser = options.GetOptionStringValue("-user");
                        string strAppTyp = options.GetOptionStringValue("-appType");
                        MARSMessageSvc.currentUserName = strUser;
                        DealwithObjectSpyInjectAndRecall(strSpyPID, strUser, targetWnd, strAppTyp, isRecall);
                        return MarsDriverConst.exit_code_Spy_ok;
                    }

                    if (isToStartMarsSVC(args[0].Substring(1), ref strAddressAndPort, ref query))
                    {
                        Console.WriteLine("start Web socket mode(from command).......");
                        Application.Run();
                        return 1;
                    }

                    string strCmdVal = options.GetOptionStringValue("-cmd");
                    if (string.Equals(strCmdVal, MARSMessageCenterAgentStarter.CmdStartMessageAgent, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"-cmd find with value:{strCmdVal}");
                        string sessionId = options.GetOptionStringValue("-sessionId") ?? string.Empty;
                        string portStr = options.GetOptionStringValue("-marsWebSocketServerPort")?.Trim() ?? "0";
                        if (!int.TryParse(portStr, out int marsWebSocketServerPort) || marsWebSocketServerPort <= 0)
                        {
                            MessageBox.Show("Invalid marsWebSocketServerPort. Use -marsWebSocketServerPort <port>", "MaRS Engine", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return MarsDriverConst.exit_code_error_paraFormat;
                        }
                        var starter = new MARSMessageCenterAgentStarter(sessionId, marsWebSocketServerPort);
                        var handshakeThread = new Thread(() => starter.WaitForHandshake(30000)) { IsBackground = true };
                        handshakeThread.Start();
                        Thread.Sleep(800);
                        if (!starter.StartAgent(out string err))
                            return MarsDriverConst.exit_code_error_paraFormat;
                        handshakeThread.Join(32000);
                        return 0;
                    }
                    
                    if (args[0].Equals("-WEBSPY"))
                    {
                        Application.EnableVisualStyles(); // 启用 Windows 视觉样式
                        //Application.SetCompatibleTextRenderingDefault(false); // 使用默认字体渲染
                        Application.Run(MARSXpathDialog.GetInstance()); // 运行窗口
                        //Application.Run();
                        return 2;
                    }
                    string strSource = options.GetOptionStringValue("-source");
                    if ((!string.IsNullOrEmpty(strSource))
                        && cnst_para_source_jenkins.Equals(strSource, StringComparison.OrdinalIgnoreCase))
                    {
                        // ci 集成沉默模式
                        Console.WriteLine($"source:[{strSource}]");
                        MLogger.LOGGER_NAME = "AutoDriver";
                        Logger = MLogger.GetLogger(typeof(AutoTestingDriverEntry));
                        StoryboardExecute.TestStoryInfoFromCICmd = new MarsCITest(options);
                        if ((StoryboardExecute.TestStoryInfoFromCICmd == null) || (!StoryboardExecute.TestStoryInfoFromCICmd.isOk))
                        {
                            Console.WriteLine($"Error before try to run Mars engine with para:[{args}], \r\n\tMake sure that username,MarsDB ShortName, application shortName and storyboard information are set and right.");
                            return MarsDriverConst.exit_code_error_paraFormat;
                        }
                        clientWCF::MarsTestFrame.SourceCode.xmlConfig.WCFXmlCfgMgr.CurrentLoginUser = StoryboardExecute.TestStoryInfoFromCICmd.userName;

                        Console.WriteLine($"get current Login User {StoryboardExecute.TestStoryInfoFromCICmd.userName}");
                        if (!WriteCurrentMarsAccountFile(StoryboardExecute.TestStoryInfoFromCICmd.userName, ref strError))
                        {
                            Console.WriteLine(strError);
                            MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, null, null, null, "Contact Marquis", null,
                                Environment.StackTrace, false);
                            return MarsDriverConst.exit_code_error_noUserInfoInPara;
                        }
                        ///需要设置测试信息
                        ///
                        try
                        {
                            //kill and start monitor
                            if (!startTestMonitorSvc(StoryboardExecute.TestStoryInfoFromCICmd.userName, ref iMonitorPort, ref strError, ref strAdv, ref strStack))
                            {
                                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                                //Console.ReadLine();
                                return MarsDriverConst.exit_code_error_cantStartMonitor;
                            }
                            TestFlowClientMainEntry.currentMarsUserAccount = StoryboardExecute.TestStoryInfoFromCICmd.userName;
                            TestFlowClientMainEntry.currentMarsMonitorPort = iMonitorPort;

                            Console.WriteLine($"before test Storyboard:{StoryboardExecute.TestStoryInfoFromCICmd.ProjectName}.{StoryboardExecute.TestStoryInfoFromCICmd.StoryboardName}");
                            bool isQuitMain = false;
                            isOk = StoryboardExecute.ExecuteTestFromCI(ref strError, ref isQuitMain);
                            if (isQuitMain)
                            {
                                Console.WriteLine("Quit itself");
                                Thread.Sleep(1000);
                                return isOk ? MarsDriverConst.exit_code_ExeTestFromCI_Ok : MarsDriverConst.exit_code_ExeTestFromCI_Failed;
                            }
                        }
                        finally
                        {
                            Console.WriteLine($"[{StoryboardExecute.TestStoryInfoFromCICmd.ProjectName}].[{StoryboardExecute.TestStoryInfoFromCICmd.StoryboardName}] has been tested on application [{StoryboardExecute.TestStoryInfoFromCICmd.applicationShortName}] "
                                + (isOk ? "SUCESSED." : $" with error:[{strError}]"));
                        }
                        return MarsDriverConst.exit_code_ExeTestFromCI_Ok;
                    }
                }
                StoryboardExecute.currentParameters = args;
#if _forWebClient
                if ((args != null) && (args.Length > 0))
                {
                    Console.WriteLine($"args:{string.Join(",", args)}");
#if _remoteDebug
                    Console.ReadLine();
#endif
                    if (string.Compare(args[0], "tigertest", true) == 0)
                    {
                        List<string> lstArgs = new List<string>(args);
                        lstArgs.RemoveAt(0);
                        args = lstArgs.ToArray();
                        Console.WriteLine($"changed args:{string.Join(",", args)}");

                    }
                    else if (isToStartMarsSVC(args[0], ref strAddressAndPort, ref query))
                    {
                        Console.WriteLine("start Web socket mode(from command).......");
                        Application.Run();
                        return 1;
                    }
                }
                else
                {
                    Console.WriteLine("args:null");
                    // 2023 new parameter is added like below:
                    // http://localhost:56422/MARSENGINE/Mars.AutoTestingDriver.application?userName=admin&uuid=uuid44058-63264-59828-7413&command=-S&storyBoadName=DAY%201_NTRS%20Repo&storyBoardId=211152&app=1&Mode=Base&Continue=False&IgnoreError=False&currentDB=GEN_MARS_5&callBackServer=localhost:56422"
                    // http://localhost:56422/MARSENGINE/Mars.AutoTestingDriver.application?uuid=uuid44058-63264-59828-7413&type=ObjRec|TestRec
                    // callBackServer=localhost:56422
                    // 2025 added sample
                    // 用于启动record and replay，即spy模式
                    // http://localhost:8190/MARSENGINE/Mars.AutoTestingDriver.application?uuid=66998987-553b-46af-9540-9144ad14a7ed&type=RecordReplay&db=GEN_MARS_5
#if _remoteDebug
                    Console.ReadLine();
#endif                    //url:http://mars05.eastus.cloudapp.azure.com/Download/Mars.AutoTestingDriver.application?userName=admin%25commmd=-S&storbyBoadName=TPG_COMPLETE_DEMO&&storyBoardId=383507&app=14&Mode=Base&Continue=False&IgnoreError=False&CurrentDB=MarsEntities

                    // url for steps mode：MARSENGINE/Mars.AutoTestingDriver.application?userName=tiger&command=-FromClipboard&storyBoadName=temp&storyBoardId=-1&app=213&guid=a1b06b48-041f-442c-94eb-d7480c57a647&currentDB=GEN_MARS_10	
                    string[] arrParas = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                    Console.WriteLine("URL [{0}]-[{1}]\r\n\t[{2}]", AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData, AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData.GetType(),
                        string.Join(",", arrParas));

                    if ((arrParas == null) || (arrParas.Length <= 0))
                    {
                        Console.WriteLine("Total Command Count:[{0}] paras:[{1}]", arrParas == null ? -1 : arrParas.Length, arrParas == null ? "N/A" : String.Join(";", arrParas));
                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError("No Parameters to start MARS engine", "N/A", "N/A", "N/A", "please start Engine from MARS GUI", "N/A", "N/A",
                            false);
                        return MarsDriverConst.exit_code_error_paraFormat;
                    }

                    if (isToStartMarsSpy(arrParas[0], ref strAddressAndPort, ref query))
                    {
                        Console.WriteLine("Spy mode.......");
                        Logger.Info("is SpyMode");
                        return StartSpyMode(strAddressAndPort, query);
                    }
                    if (isToStartMessageAgent(arrParas[0], ref query))
                    {
                        Console.WriteLine("start Message Agent mode.......");
                        return StartMessageAgentMode(query);
                    }
                    if (isToStartMarsSVC(arrParas[0], ref strAddressAndPort, ref query))
                    {
                        Console.WriteLine("start Web socket mode.......");
                        Application.Run();
                        return 1;
                    }
#if _demoLicense
                    string formatString = "yyyyMMdd";
                    string sample = "20260901";
                    DateTime dt = DateTime.ParseExact(sample, formatString, null);
                    Random _x = new Random();
                    if ((DateTime.Now> dt)&&((_x.Next() % 2)==0))
                    {
                        Application.Exit();
                        return 1;
                    }
#endif

                    if (isToStartMarsRecordAndReplay(arrParas[0], ref strAddressAndPort, ref query))
                    {
                        Console.WriteLine("Spy mode.......");
                        return StartSpyRecordAndReplayMode(strAddressAndPort, query);
                    }

                    string strRecordRplayMode = "";
                    if (isToStartObjRecorgMode(arrParas[0], ref strAddressAndPort, ref strRecordRplayMode, ref query))
                    {
                        return StartObjectRecordMode(strAddressAndPort, strRecordRplayMode, query);
                    }

                    bool isTmpStoryboardAndTestStepsMode = IsTmpStoryboardAndTestStepsMode(arrParas[0]);
                    if (isTmpStoryboardAndTestStepsMode)
                    {
                        MLogger.LOGGER_NAME = "AutoDriver";
                        Logger = MLogger.GetLogger(typeof(AutoTestingDriverEntry));
                        clientWCF::MarsTestFrame.SourceCode.xmlConfig.WCFXmlCfgMgr.CurrentLoginUser = StoryboardExecute.TestStepsFromJsonInfo.userName;
                        Console.WriteLine($"get current Login User {StoryboardExecute.TestStepsFromJsonInfo.userName}");
                        if (!WriteCurrentMarsAccountFile(StoryboardExecute.TestStepsFromJsonInfo.userName, ref strError))
                        {
                            Console.WriteLine(strError);
                            MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, null, null, null, "Contact Marquis",
                                null, Environment.StackTrace, true);
                            return MarsDriverConst.exit_code_error_cantCreateMarsAccountFile;
                        }
                        if ((StoryboardExecute.TestStepsFromJsonInfo == null)
                            || (StoryboardExecute.TestStepsFromJsonInfo.testStepsFromClipboard == null)
                            || (StoryboardExecute.TestStepsFromJsonInfo.testStepsFromClipboard.Count == 0))
                        {
                            Console.WriteLine(strError = $"No test steps to be run");
                            MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, null, null, null,
                                strAdv = "Select test steps from MARS web first.", null, Environment.StackTrace, true);
                            return MarsDriverConst.exit_code_error_noTestSteps;
                        }
                        int iStpCnt = StoryboardExecute.TestStepsFromJsonInfo.testStepsFromClipboard == null
                                || StoryboardExecute.TestStepsFromJsonInfo.testStepsFromClipboard.Count == 0 ? 0
                                : StoryboardExecute.TestStepsFromJsonInfo.testStepsFromClipboard.Count;
                        RunTempTestSteps(clientWCF::MarsTestFrame.SourceCode.xmlConfig.WCFXmlCfgMgr.CurrentLoginUser,
                            StoryboardExecute.TestStepsFromJsonInfo,
                            ref isOk,
                            ref strError, ref strStack, ref strAdv);
                        if (isOk)
                        {

                            MessageBox.Show($"Executed total {iStpCnt} test steps.");
                            return MarsDriverConst.exit_code_ExeTestFromJsonClipbord_Ok;
                        }
                        else
                        {
                            MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, null, null, null, strAdv,
                                null, strStack, true);
                            return MarsDriverConst.exit_code_ExeTestFromJsonClipbord_Failed;
                        }
                    }

                    args = CombinParaFromURI(arrParas[0], ref isOk, ref strError, ref strAdv, ref strStack);
                    if (!isOk)
                    {

                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A",
                            "please start Engine from MARS GUI, \r\nif error continues, please contact Marquis", "N/A", "N/A",
                            true);
                        return MarsDriverConst.exit_code_error_paraFormat;
                    }
                    Console.WriteLine($"combined:{string.Join(" ", args)}");
                    StoryboardExecute.currentParameters = args;

                }
#endif

                string[] arrAutocheckErrorKeywords = KeywordOpAgent.AutoCheckErrorKeywordsFromConfig;
                if (arrAutocheckErrorKeywords == null)
                {
                    Console.WriteLine("No AutoCheck Error keywords is set");
                }
                else
                {
                    Console.WriteLine("auto check error keywords:[{0}]", string.Join("; ", arrAutocheckErrorKeywords));
                }
                Console.WriteLine("args count:[{0}]", args.Length);
#if _remoteDebug
                Console.WriteLine(String.Join(" ", args));
                //for test
                Console.WriteLine("Wait for debugger:");
                Console.ReadLine();
                Console.WriteLine("debugger connected?");
#endif
                if ((args.Length > 0) && (string.Compare("AS_INJECTOR", args[0], true) == 0))
                {
                    TestInjector();
                    return MarsDriverConst.exit_code_ExeAsInject_Ok;
                }

                MARSMessageSvc.currentUserName = args[0];
                //写一个文件
                isOk = WriteCurrentMarsAccountFile(MARSMessageSvc.currentUserName, ref strError);
                if (!isOk)
                {
                    Console.WriteLine("Can't create Mars account file. ");

                }
                MARSMessageSvc.CleanClientQueue();


                if (!startTestMonitorSvc(args[0], ref iMonitorPort, ref strError, ref strAdv, ref strStack))
                {
                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                    //Console.ReadLine();
                    return MarsDriverConst.exit_code_error_cantStartMonitor;
                }

                TestFlowClientMainEntry.currentMarsUserAccount = args[0];
                TestFlowClientMainEntry.currentMarsMonitorPort = iMonitorPort;
#if _forWebClient                
#endif                
                Console.WriteLine("Current session Id:{0}, args Len:{1}", Process.GetCurrentProcess().SessionId, args == null ? -1 : args.Length);
#if TigerQT
#endif
                if ((args.Length == 2) && (string.Compare("-LoadLib", args[0], true) == 0))
                {

                }

                if ((args.Length == 2) && (string.Compare("LoadTest", args[0], true) == 0))
                {
                    try
                    {
                        LoadTestDll(args[1], ref strError);
                        return MarsDriverConst.exit_code_LoadTest_Ok;
                    }
                    catch (Exception e)
                    {
                        return MarsDriverConst.exit_code_error_LoadTest_Failed;
                    }
                    finally
                    {

                    }

                }
                else
                {
                    if ((args.Length == 2) && (string.Compare("-Injector", args[0], true) == 0))
                    {
                        ///just inject to target system
                    }

                    if ((args.Length == 2) && (string.Compare("-Injector32DlgStarter", args[0], true) == 0))
                    {
                        Console.WriteLine("Inject 32Dlg mode");
                        //Console.ReadLine();
                        ///just inject to target system
                        ///测试32位的 因为dlg无法注入的进程，后面有个参数是进程的名词
                        ///Mars.AutoTestingDrivre.Anycpu -Injector32DlgStarter TPG
                        ///
                        InjectToDlgStart32(args[1]);
                        return MarsDriverConst.exit_code_InjectToDlgStart32_Ok;
                    }

                }

                #region test code
                //string strDBCompareAssembliy = "Mars.TestFramework.DataCompare.dll";
                //Assembly.lad

                //MARSDealResult dealResult = new MARSDealResult();
                //Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.KeyWordsOPForNonGUI.MARSKEYWORD_DbCompare(1, "ddd", "dddd", new Business.B_V_OBJECT_SNAPSHOT(),
                //ref strError, ref dealResult);
                #endregion
                //Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.KeyWordsOPForNonGUI.AssemblyIsLoaded("ddddd");
                isVerifyValueSkipper = MarsDriverAppConfigMgr.GetVerifyValueSkipper();
                Console.WriteLine("try to get Configed application Info");
#if _remoteDebug
                Console.ReadLine();
#endif                
                if (!MarsDriverAppConfigMgr.GetConfigurationApps(ref strError))
                {
                    Console.WriteLine(@"can't read application from configuration file with error:
        {0}
    
        Please Press Any key to Quit.....", strError);
                    if (!StoryboardExecute.IsSilenceMode)
                        Console.ReadLine();
                    return MarsDriverConst.exit_code_error_cantGetConfigApps;
                }
#if _forWebClient

                Console.WriteLine("restful:{0}", (new MarsRESTfulApiClient("")).webURLPreFix);
#endif
                try
                {
                    // 在转换configfile前，先处理是否进行自动化处理autocheckError
                    StoryboardExecute.LoadAutoCheckErrorSettings(AppConfigReader.GetAppsettings());
                    Console.WriteLine($"get auto check error Setting:[{StoryboardExecute.autoErrorChck.checkErrorQuickAccess}]");
                    //64bit
                    if (!ChangeConfigFile(ref strError))
                    {
                        PrintUsage();
                        Console.WriteLine(strError);
                        return MarsDriverConst.exit_code_error_cantChangeConfigFile;
                    }
                }
                catch (Exception E)
                {
                    Console.WriteLine($"Excetion:{E.Message}, \r\n, {E.StackTrace}");
                    //Console.ReadLine();
                    return MarsDriverConst.exit_code_error_loadConfigException;
                }
                Console.WriteLine("get Logger");

                MarsEntitiesExtends.InitDBInfo(AppConfigReader.GetAppsettings(),
                    AppConfigReader.GetConfigurationInstance().ConnectionStrings.ConnectionStrings);
#if _remoteDebug
                Console.ReadLine();
#endif
                MLogger.LOGGER_NAME = "AutoDriver";
                Logger = MLogger.GetLogger(typeof(AutoTestingDriverEntry));

                if (args.Length > 0)
                    //Logger.logBegin("Main", string.Format("args:[{0}]", args));
                    Logger.Info($"======Main=====[{Process.GetCurrentProcess().ProcessName}] ", string.Format("parameters:{0}", string.Join(" ", args)));
                try
                {
                    ///处理.net core 和wpf的引擎
                    ///
                    isOk = ExtractWPFCore(ref strError);
                    if (!isOk)
                    {
                        Console.WriteLine($"Error:{strError}");
                        Logger.Info("Main", $"extract wpf core engine result|{isOk}|{strError}");
                        //return MarsDriverConst.exit_code_error_cantExtractWPFCore;
                    }

                    //System.Configuration.ConfigurationSettings.GetConfig()
                    if (args == null)
                    {
                        PrintUsage();
                        return MarsDriverConst.exit_code_error_paraFormat;
                    }
                    if (args.Length < 3)
                    {
                        PrintUsage();
                        return MarsDriverConst.exit_code_error_paraFormat; ;
                    }
                    string strCmd = args[1].ToUpper();

                    string strCurrentDB = "MarsEntities";
                    var z = args.Select((t, idx) => new { value = t, index = idx })
                        .Where(t => string.Compare(t.value, "-currentDB", true) == 0)
                        .FirstOrDefault();
                    if (z != null)
                    {
                        if (args.Length > (z.index + 1))
                            strCurrentDB = args[z.index + 1];
                    }
#if !_forWebClient

                    //Mars.AutoTestingDriver.db.DatabaseEnvironment.InitSchemaChangingAndDBConnection(strCurrentDB);
#endif
                    clientWCF::MarsTestFrame.SourceCode.xmlConfig.WCFXmlCfgMgr.CurrentLoginUser = args[0];
                    Console.WriteLine($"get current Login User {args[0]}");

                    string[] arrParaWithoutApp = new string[args.Length - 2];
                    string strCurMarsAccount = args[0];
                    bool isExitItself = false;
                    Array.Copy(args, 2, arrParaWithoutApp, 0, arrParaWithoutApp.Length);
#if mars_Agent
                    isOk = new MARSMessageCenterAgentStarter().ConnectToMQStub(out strError);
                    if (!isOk)
                    {
                        Console.WriteLine($"Can't connect to MQStub with error:{strError}");
                        Logger.Warnning("Main", $"Can't connect to MQStub with error:{strError}");
                        //return MarsDriverConst.exit_code_error_cantConnectMQStub;
                    }
#endif

#if _demo_for_14
                    if (MarsKeywordBase.IsInDateTimeX())
                    {
                        strCmd = "-UNKNOW";
                    }
#endif
                    switch (strCmd)
                    {
                        case "-S":
                        case "-STORYBOARD":
                            return RunTestCaseOrTestStoryBoard(strCurMarsAccount, arrParaWithoutApp, ref isExitItself);
                        default:
                            return MarsDriverConst.exit_code_error_paraFormat;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Main", $"Exception|{e.Message}",e.StackTrace);
                    return MarsDriverConst.exit_code_error_mainException;
                }
                finally
                {

                    Logger.logEnd("Main");
                }
            }
            catch (Exception e)
            {
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError($"Exception:{e.Message}", "N/A", "N/A", "N/A", "Contact Marquis",
                    e.InnerException == null ? "N/A" : e.InnerException.Message, e.StackTrace,
                    StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                Logger.Error("Main", e.Message, e);
                return MarsDriverConst.exit_code_error_mainException;
            }
        }

        private static int HiddenCurrrentConsole(bool toMini=false)
        {
            var conHdl = MarsWindowsAPIs.GetConsoleWindow();
            if (!toMini)
                MarsWindowsAPIs.ShowWindow(conHdl, (int)ShowWindowCommands.SW_HIDE);
            else
                MarsWindowsAPIs.ShowWindow(conHdl, (int)ShowWindowCommands.SW_MINIMIZE);
            return 1;
        }
        /// <summary>
        /// sample // http://localhost:8152/MARSENGINE/Mars.AutoTestingDriver.application?command=objectRegTool&uuid=xxxxx-xxxx-xxxx-xxxxx
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private static int StartSpyMode(string strAddressAndPort, NameValueCollection query)
        {
            int iMark = new Random().Next();
            Logger.logBegin($"{iMark}|StartSpyMode");
            try
            {
                /// start tool application
                /// 
                string strPath = typeof(AutoTestingDriverEntry).Assembly.Location;
                strPath = System.IO.Path.GetDirectoryName(strPath);
                strPath = System.IO.Path.Combine(strPath, MarsConstants.CNST_SYPTOOL_EXE_NAME);
                if (!System.IO.File.Exists(strPath))
                {
                    Logger.Error("StartSpyMode", $"{iMark}|no executable spy file|{strPath}|exist|");
                    return -1;
                }

                string strUUID = query.Get(MarsConstants.CNST_QUERY_URL_UUID_PARA_NAME);
                if (string.IsNullOrEmpty(strUUID))
                {
                    Logger.Error("StartSpyMode", $"{iMark}|No UUID is passed to ");
                    return -2;
                }
                var p = Process.Start(strPath, $"-mode spyObject -uuid {strUUID} -server {strAddressAndPort}");
                Logger.Info("StartSpyMode", $"have start spy object process with id|{p.Id}|{strPath} -mode spyObject {MarsConstants.CNST_SYPTOOL_PARA_UUID} {strUUID}|");
                HiddenCurrrentConsole();

                return 1;
            }
            finally
            {
                Logger.logEnd($"{iMark}");
            }
        }


        private static int StartSpyRecordAndReplayMode(string strAddressAndPort, NameValueCollection query)
        {
            int iMark = new Random().Next();
            Logger.logBegin($"{iMark}|StartObjectRecordMode");
            try
            {
                /// start tool application
                /// 
                string strPath = typeof(AutoTestingDriverEntry).Assembly.Location;
                strPath = System.IO.Path.GetDirectoryName(strPath);
                strPath = System.IO.Path.Combine(strPath, MarsConstants.CNST_SYPTOOL_EXE_NAME);
                if (!System.IO.File.Exists(strPath))
                {
                    Logger.Error("StartSpyMode", $"{iMark}|no executable spy file|{strPath}|exist|");
                    return -1;
                }

                string strUUID = query.Get(MarsConstants.CNST_QUERY_URL_UUID_PARA_NAME);
                if (string.IsNullOrEmpty(strUUID))
                {
                    Logger.Error("StartSpyMode", $"{iMark}|No UUID is passed to ");
                    return -2;
                }
                var p = Process.Start(strPath, $"-mode {MarsConstants.CNST_SPYTOOL_PARA_MODE_RECTC} -uuid {strUUID} -server {strAddressAndPort}");
                Logger.Info("StartSpyMode", $"have start spy object process with id|{p.Id}|{strPath} -mode spyObject {MarsConstants.CNST_SYPTOOL_PARA_UUID} {strUUID}|");
                HiddenCurrrentConsole();

                return 1;
            }
            finally
            {
                Logger.logEnd($"{iMark}");
            }
        }

        private static int StartObjectRecordMode(string strAddressAndPort, string strRecordRplayMode, NameValueCollection query)
        {
            int iMark = new Random().Next();
            Logger.logBegin($"{iMark}|StartObjectRecordMode|{strRecordRplayMode}");
            try
            {
                /// start tool application
                /// 
                string strPath = typeof(AutoTestingDriverEntry).Assembly.Location;
                strPath = System.IO.Path.GetDirectoryName(strPath);
                strPath = System.IO.Path.Combine(strPath, MarsConstants.CNST_SYPTOOL_EXE_NAME);
                if (!System.IO.File.Exists(strPath))
                {
                    Logger.Error("StartObjectRecordMode", $"{iMark}|no executable spy file|{strPath}|exist|");
                    return -1;
                }

                string strUUID = query.Get(MarsConstants.CNST_QUERY_URL_UUID_PARA_NAME);
                if (string.IsNullOrEmpty(strUUID))
                {
                    Logger.Error("StartObjectRecordMode", $"{iMark}|No UUID is passed to ");
                    return -2;
                }
                //var p = Process.Start(strPath, $"-mode {MarsConstants.CNST_QUERY_URL_OBJREC} -uuid {strUUID} -server {strAddressAndPort}");
                var p = Process.Start(strPath, $"-mode {strRecordRplayMode} -uuid {strUUID} -server {strAddressAndPort}");
                
                Logger.Info("StartObjectRecordMode", $"have start spy object process with id|{p.Id}|{strPath} -mode {strRecordRplayMode} {MarsConstants.CNST_SYPTOOL_PARA_UUID} {strUUID} -server {strAddressAndPort}|");
                HiddenCurrrentConsole();

                return 1;
            }
            finally
            {
                Logger.logEnd($"{iMark}");
            }
        }


        private static void DealwithObjectSpyInjectAndRecall(string strSpyPID, string strUser, string targetWnd, string appTyp, bool isRecall)
        {
            int pid;
            if (!int.TryParse(strSpyPID, out pid))
            {
                Console.WriteLine("There is no process id provided, \r\n\tplease use -spy [target process Id] -targetWnd [target window handle] -isRecall [T/F]");
                return;
            }
            string strError = "";
            bool isOk = WriteCurrentMarsAccountFile(strUser, ref strError);
            Console.WriteLine($"created swap file for [{strUser}]");

            Process p = Process.GetProcessById(pid);
            bool is32Bit = MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.IsProcess32(p.Handle);
            bool isDialogMode = false;
            IntPtr dialogHdl = IntPtr.Zero;
            if ((p == null) || (p.MainWindowHandle == IntPtr.Zero))
            {
                if (p != null)
                {
                    if (ModalChecker.IsWaitingForUserInput(p, ref dialogHdl))
                    {
                        isDialogMode = true;
                    }
                }
                else
                {
                    Console.WriteLine("There is no process id provided, \r\n\tplease use -spy [target process Id] -targetWnd [target window handle] -isRecall [T/F]");
                    return;
                }
            }
            if (isRecall)
            {
                string strPathOfMars = System.IO.Path.GetDirectoryName(typeof(AutoTestingDriverEntry).Assembly.Location);
                //说明需要重新inject
                string strMQCenterDllName = is32Bit ? "MarsInterMQCenter.Any.dll" : "MarsInterMQCenter.dll";
                string tmpNameSpace = "Mars.message.Inter.MQCenter.interProcess.MarsMessageClientSvc";
                
                if (isDialogMode)
                    Injector.Launch(dialogHdl, System.IO.Path.Combine(strPathOfMars, strMQCenterDllName),
                        tmpNameSpace,//"Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                        "StartMonitorThread", "Normal");
                else
                    Injector.Launch(p.MainWindowHandle, System.IO.Path.Combine(strPathOfMars, strMQCenterDllName),
                        tmpNameSpace,//"Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                        "StartMonitorThread", "Normal");
                Console.WriteLine($"injected|{tmpNameSpace}|");
                //这里需要重新判断是否有MarsInterMQCenter.dll
            }
            //发送_StartObjectSpy keyword
            // 创建个json对象
            string strDataToAgent = "{" + $"targetWnd:'{targetWnd}', spyType:'{appTyp}'" + "}";
            KeywordOpForGUI._StartObjectSpyAgent(strDataToAgent);
        }

        private static bool startTestMonitorSvc(string strCurrentAccountName, ref int iPort, ref string strError, ref string strAdv, ref string strStack)
        {
            Console.WriteLine("Try to start cloudy MARS TEST monitor.....");
            bool isOk = true;
            //启动testframeMonitor
            iPort = WCFXmlCfgMgr.GetAvailabelPort();
            if (iPort < 0)
            {
                strError = "Can't start Test Mointor";
                strAdv = "There is no available TCP Port, close some applications and try again.";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            /**
            WCFXmlCfgMgr userPortInfo = WCFXmlCfgMgr.LoadFromFile(ref isOk, ref strError);
            if (!isOk)
            {
                strStack        = MarsErrorStacks.StackTraceDump()  ;
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", "Contact Marsquis", "", strStack);
                return false;
            }
            WCFServiceNode userNode = userPortInfo.WCFNodes==null?null:userPortInfo.WCFNodes.FirstOrDefault(p => string.Compare(strCurrentAccountName, p == null ? "" : p.AccountName == null ? "" : p.AccountName, true) == 0);
            if (userNode == null)
            {
                strStack = MarsErrorStacks.StackTraceDump();
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError($"No user Information set, call set service Ports first", "N/A", "N/A", "N/A", "Contact Marsquis", "", strStack);
                return false;
            }
            ***/
            //start monitor 
            string strMonitorName = "TestFrameMonitor";
            string strFullPath = Assembly.GetExecutingAssembly().Location;
            string strMonitorPath = Path.GetDirectoryName(strFullPath);

            try
            {
                Process[] arrp = Process.GetProcessesByName(strMonitorName);
                Process pCur = Process.GetCurrentProcess();
                foreach (var pk in arrp.Where(px => px.SessionId == pCur.SessionId).ToList())
                {
                    if (pk == null) continue;
                    pk.Kill();
                }

                strMonitorName = Path.Combine(strMonitorPath, strMonitorName + ".exe");
                if (!File.Exists(strMonitorName))
                {
                    strStack = MarsErrorStacks.StackTraceDump();
                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError($"Can't find MARS Monitor, Please reinstall MARS",
                        "N/A", "N/A", "N/A", "Contact Marsquis", "", strStack, StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                    return false;
                }

                Process p = Process.Start(new ProcessStartInfo()
                {
                    Arguments = $"{strCurrentAccountName} {iPort}",
                    FileName = strMonitorName
                });

                Thread.Sleep(500);
                var curP = Process.GetCurrentProcess();

                //wait for monitor is ready
                long s = DateTime.Now.Ticks;
                long e = s;
                Console.WriteLine("\t, begin to find monitor in 2 minutes....");
                //bool isMonitorStarted = false;
                while ((e - s) < (TimeSpan.TicksPerMinute * 2))
                {
                    var p1 = Process.GetProcessesByName("TestFrameMonitor");

                    if ((p1 == null) || (p1.Length <= 0))
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        Console.WriteLine("\t, Find Monitor....");
                        return true;
                    }
                    e = DateTime.Now.Ticks;
                }
                //p.Start();
                return false;
            }
            catch (Exception e)
            {
                strStack = e.StackTrace;
                Logger.Error("\t", e.Message, e);
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError($"Can't find MARS Monitor, Please reinstall MARS",
                    "N/A", "N/A", "N/A", "Contact Marsquis", "", strStack, StoryboardExecute.TestStoryInfoFromCICmd == null ? true : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                return false;
            }
        }
        public class Proxy : MarshalByRefObject
        {
            public Assembly GetAssembly(string assemblyPath)
            {
                try
                {
                    return Assembly.LoadFile(assemblyPath);
                }
                catch (Exception)
                {
                    return null;
                    // throw new InvalidOperationException(ex);
                }
            }
        }

        private static bool LoadTestDll(string strDllsName, ref string strError)
        {
            string strPath = typeof(AutoTestingDriverEntry).Assembly.Location;
            strPath = System.IO.Path.GetDirectoryName(strPath);
            string strPathToLoad = System.IO.Path.Combine(strPath, strDllsName);
            if (!System.IO.File.Exists(strPathToLoad))
            {
                strError = string.Format("No such file:[{0}]", strPathToLoad);
                return false;
            }
            try
            {
                Console.WriteLine("Try to Create Domain");
                AppDomainSetup domaininfo = new AppDomainSetup();
                domaininfo.ApplicationBase = System.Environment.CurrentDirectory;
                Evidence adevidence = AppDomain.CurrentDomain.Evidence;

                var en = adevidence.GetHostEnumerator();
                while (en.MoveNext())
                {
                    Console.WriteLine("Key:[{0}]", en.Current.ToString());
                    if (en.Current is System.Security.Policy.Url)
                    {
                        System.Security.Policy.Url policyEv = en.Current as System.Security.Policy.Url;

                    }
                }

                AppDomain domain = AppDomain.CreateDomain("TestDllDomain", adevidence, domaininfo);

                Console.WriteLine("Try to load file [{0}]", strPathToLoad);
                Type type = typeof(Proxy);
                var value = (Proxy)domain.CreateInstanceAndUnwrap(
                    type.Assembly.FullName,
                    type.FullName);

                var assembly = value.GetAssembly(strPathToLoad);
                Console.WriteLine("Try to load  Oracle.ManagedDataAccess.Client.OracleConnection");
                Type bohelper = assembly.GetType(" Oracle.ManagedDataAccess.Client.OracleConnection");
                var cO = bohelper.GetConstructor(new Type[] { });

                //MethodInfo m=bohelper.GetMethod("OracleConnection", null);
                Console.WriteLine("Try to invoke new OracleConnection", cO);

                var cnn = cO.Invoke(null);
                Console.WriteLine("Get a Oracle connection with type [{0}]", cO.GetType());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("LoadTestDll", string.Format("Exception [{0}]", e.Message), e);
                return false;
            }

        }

        private static void TestInjector()
        {
            long lPID;

            //IntPtr libHandle  = MarsWindowsAPIs.LoadLibrary(@"C:\automationTest\Automation Workbooks\dlls - t_qt\QTInjectorDll.dll");
            //if (libHandle==IntPtr.Zero)
            //{
            //    uint lerrorId = MarsWindowsAPIs.GetLastError();
            //    Logger.Info("TestInjector", string.Format("Get last error:[{0}]", lerrorId));
            //    Console.WriteLine(string.Format("Get last error:[{0}]", lerrorId));
            //    return;
            //}
            //Console.WriteLine(string.Format("lib handle is :[{0}]", libHandle));
            //IntPtr funcPointer = MarsWindowsAPIs.GetProcAddress(libHandle,"InitEnv");
            //if (funcPointer == IntPtr.Zero)
            //{
            //    uint lerrorId = MarsWindowsAPIs.GetLastError();
            //    Logger.Info("TestInjector", string.Format("Get last error:[{0}]", lerrorId)); ;
            //    Console.WriteLine(string.Format("Get last error:[{0}]", lerrorId));
            //    return;
            //}
            //Console.WriteLine(string.Format("funcPointer handle is :[{0}]", funcPointer));
            Console.Write("Please input process ID:");
            string strInput = Console.ReadLine();

            if (!long.TryParse(strInput.Trim(), out lPID)) return;
            /*
             * Console.Write("Please input Commands and Dll Name separated by \":\":\r\n1:");
            string strDllName = Console.ReadLine();
            strDllName = string.IsNullOrEmpty(strDllName) ? @"QT:C:\automationTest\Automation Workbooks\dlls - t_qt\QTInjectorDll.dll" : strDllName;
            //string strDllName = "QT:QTInjectorDll.dll"; //Console.ReadLine();  C:\automationTest\Automation Workbooks\dlls - t_qt
            Console.Write("Please input Function Name:\r\n2:");
            string strFuncName = "InitEnv";//Console.ReadLine();
            Console.Write("Please input Parameter Name:\r\n3:");
            string strPara = @"C:\automationTest\2017remoteDebug\Remote Debugger\x64"; //Console.ReadLine();
            */
            Process pTarget = Process.GetProcessById((int)lPID);
            IntPtr lMainHandle = pTarget.MainWindowHandle;
            Injector.Launch(lMainHandle, "QT:C:\\automationTest\\Automation Workbooks\\dlls - t_qt\\QTInjectorDll.dll", "InitQT", "InitQT", "Normal");
            //Injector.Launch(lMainHandle, strDllName, strFuncName, strPara);
        }

        private static void PrintUsage()
        {
            Console.WriteLine(
                @"Usage of AutoTestingDriver:
    CurrentLogName -S(tart)|T(estCase) (Command support string) -App ApplicationShortName -Mode (Base|NonBase) -C true (-ExtRequire QT)
    
    Command: S(toryboard)|T(estCase)
    Command support string For S(tart):
        StoryboardName StoryboardId --Run Story test. story board name and storyboard Id should be seprated by space(s)
        TestCaseName TestCaseId -App ApplicationId--Run Test steps by sequence
    Command: -App ApplicationShortName
        ApplicationShortName Informaiton from MarsTestFrame.dll.config
    Command: -Mode (Base|NonBase) 
        Base means baseline testing");
        }

        private static void InjectToDlgStart32(string strProcName)
        {
            // get the dialog handle 
            Process[] arrProc = Process.GetProcessesByName(strProcName);
            if (!((arrProc != null) && (arrProc.Length > 0)))
            {
                Console.WriteLine("no such process:[{0}]", strProcName);
                return;
            }

            Process pTarget = Process.GetProcessById(arrProc[0].Id);
            IntPtr lMainHandle = pTarget.MainWindowHandle;
            //System.Diagnostics.EventLog.WriteEntry("MarsEvent", "new begin.......");

            Console.WriteLine("target handle:{0}", lMainHandle);
            string strHdl = string.Format("main handle:%d", lMainHandle);
            List<IntPtr> lst = MarsWindowsAPIsExtend.GetWindows(arrProc[0].Id);
            for (int i = 0; i < lst.Count; i++)
            {
                Console.WriteLine("handle:{0}-{1}", lst[i].ToString("X"), lst[i]);
            }
            if (lst.Count > 0)
            {
                if (lst[0] != lMainHandle)
                {
                    lMainHandle = lst[0];
                }
            }
            
            string strPath = typeof(StoryboardExecute).Assembly.Location;
            strPath = System.IO.Path.GetDirectoryName(strPath);

            string tmpNameSpace = "Mars.message.Inter.MQCenter.interProcess";
            Injector.Launch(lMainHandle, System.IO.Path.Combine(strPath, "MarsInterMQCenter.dll"),
                tmpNameSpace, //"Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", 
                "StartMonitorThread", "Normal");
            Console.WriteLine($"---------InjectToDlgStart32\r\n{tmpNameSpace}\r\n--------");
        }
        #region business part

        private static bool ChangeConfigFile(ref string strError)
        {
            string strMarsConfigFile = typeof(AutoTestingDriverEntry).Assembly.Location;
            string strPath = Directory.GetParent(strMarsConfigFile).ToString();
            bool is64bit = IntPtr.Size == 8;
            string strFileName = is64bit ? Path.Combine(strPath, "Mars.EXE.config") : Path.Combine(strPath, "Mars32.EXE.config");
            Console.WriteLine($"change config file to :[{strFileName}]");
            if (!File.Exists(strFileName))
            {
                strError = string.Format("No such file exists. [{0}]", strFileName);
                return false;
            }
            AppConfig.Change(strFileName);
            AppConfigReader.GetConfigurationInstance();
            return true;
        }

        private static bool AddMarsAccountInfoForDDE(string strCurMarsAccount, ref string strError, ref string strAdv, ref string strStack)
        {
            string UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            try
            {
                UserName = UserName.Replace("\\", "_");
                UserCfgMgr usrCFG = new UserCfgMgr();
                usrCFG.UserFileName = UserName;
                usrCFG.MarsAccountName = strCurMarsAccount;
                return usrCFG.SaveUserEvnToFileByUserFileName(ref strError, ref strAdv, ref strStack);
            }
            catch (Exception e)
            {
                strError = "Can't save temp user config file";
                strAdv = "Contact Marquis when this Error continues.";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                return false;
            }

        }

        public static void RunTempTestSteps(string currentLoginUser, MarsClipboardURLPara testStepsFromJsonInfo, ref bool isOk, ref string strError, ref string strStack, ref string strAdv)
        {
            //Console.WriteLine(string.Format("RunTempTestSteps begin....{0}", ));
            Logger.logBegin("RunTempTestSteps", testStepsFromJsonInfo == null ? "" : testStepsFromJsonInfo.ToString());
            try
            {
                if ((testStepsFromJsonInfo == null)
                    || (testStepsFromJsonInfo.testStepsFromClipboard == null)
                    || (testStepsFromJsonInfo.testStepsFromClipboard.Count <= 0))
                {
                    strStack = Environment.StackTrace;
                    strError = "JSON step is null or empty";
                    strAdv = "Select test steps from MARS web and try again.";
                    isOk = false;
                    return;
                }

                if (!TestStepExecutionRecorder_TMP.IsTempTestCaseReady())
                {
                    strStack = Environment.StackTrace;
                    strError = "No MARS temp test case execution Report directory or template file exist";
                    strAdv = "Contact Marquis or Reinstall MARS Engine";
                    isOk = false;
                    return;
                }

                /// 算法：
                /// 1， 获得目标的application 通过 app
                /// 2， 判断是否在进程中已经存在目标进程
                MarsRESTfulApiClient restClnt = new MarsRESTfulApiClient(testStepsFromJsonInfo.currentDB);
                long appId = -1;
                B_REGISTERED_APPS targetApp = null;
                if (long.TryParse(testStepsFromJsonInfo.app, out appId))
                {

                    targetApp = restClnt.GetApplicationByAppId(testStepsFromJsonInfo.currentDB, appId, ref isOk, ref strError);
                }else
                {
                    targetApp = restClnt.GetApplicationByAppShortName(testStepsFromJsonInfo.app,
                        ref isOk, ref strError, ref strAdv, ref strStack, testStepsFromJsonInfo.currentDB);
                }
                if (!isOk) return;
                if (targetApp == null)
                {
                    strError = $"No such {testStepsFromJsonInfo.app} exists from DB [{testStepsFromJsonInfo.currentDB}]";
                    strStack = Environment.StackTrace;
                    strAdv = "Contact Marquis";
                    isOk = false;
                    return;
                }
                StoryboardExecute.setCurrentTestedApp(targetApp);

                //2, 判断进程是否已经有
                Process[] arrP = Process.GetProcessesByName(targetApp.PROCESS_IDENTIFIER);
                if ((arrP == null) || (arrP.Length <= 0))
                {
                    strError = $"No such [{targetApp.PROCESS_IDENTIFIER}] process is Running.";
                    strAdv = $"Start [{targetApp.PROCESS_IDENTIFIER}] first before running test steps testing";
                    strStack = Environment.StackTrace;
                    isOk = false;
                    return;
                }
                if (arrP.Length != 1)
                {
                    strError = $"There are [{arrP.Length}] [{targetApp.PROCESS_IDENTIFIER}] running.";
                    strAdv = $"Make sure there is only one [{targetApp.PROCESS_IDENTIFIER}] running";
                    strStack = Environment.StackTrace;
                    isOk = false;
                    return;
                }
                ///3, 启动MQ server
                ///
                MARSMessageSvcServer.currentUserName = testStepsFromJsonInfo.userName;
                MARSMessageSvcServer.SendMessageToMQ(null);

                int iPort = -1;
                if (!startTestMonitorSvc(testStepsFromJsonInfo.userName, ref iPort, ref strError, ref strAdv, ref strStack))
                {
                    isOk = false;
                    return;
                }
                TestFlowClientMainEntry.currentMarsUserAccount = testStepsFromJsonInfo.userName;
                TestFlowClientMainEntry.currentMarsMonitorPort = iPort;


                ///4, 构建临时测试用例
                ///
                bool isExitItself = false;
                StoryboardExecute.ExecuteTestStepsFromJSon(testStepsFromJsonInfo, arrP[0].Id, ref isOk, ref strError, ref strStack, ref strAdv, ref isExitItself);

            }
            catch (Exception e)
            {
                strError = e.Message;
                strStack = e.StackTrace;
                strAdv = "Contact Marquis";
                isOk = false;
                return;
            }
            finally
            {
                Logger.logEnd("RunTempTestSteps", $"isOk is {isOk}");
            }
        }

        private static bool StartMessageCenter(ref string strError)
        {
            /// 判断message center是否已经启动
            /// 
            Process[] arrP = Process.GetProcessesByName(MarsCoreMessageCenterConst.cnst_MESSAGECENTER_NAME);
            if ((arrP != null) && (arrP.Length > 0))
            {
                Console.WriteLine("Message center is already running");
                return true;
            }
            string strPath = typeof(AutoTestingDriverEntry).Assembly.Location;
            strPath = System.IO.Path.Combine( System.IO.Path.GetDirectoryName(strPath), "MarsCore", MarsCoreMessageCenterConst.cnst_MESSAGECENTER_NAME+".exe");
            if (!File.Exists(strPath))
            {
                strError = string.Format("No such file exists. [{0}]", strPath);
                return false;
            }
            Process p = Process.Start(strPath);
            Thread.Sleep(1000);
            return true;
        }

        private static int RunTestCaseOrTestStoryBoard(string strCurMarsAccount, string[] arrParameters, ref bool isExitItself)
        {
            Console.WriteLine($"RunTestCaseOrTestStoryBoard begin....{string.Join(",", arrParameters)}");
            //Console.ReadLine();
            if ((arrParameters == null) || (arrParameters.Length == 0))
            {
                PrintUsage();
                return MarsDriverConst.exit_code_error_paraFormat;
            }

            string strStoryboardName = arrParameters[0];
            string strStoryboardId = arrParameters[1];
            string strAppId = arrParameters[3];
            string strMode = arrParameters[5];
            bool isContinue = false;
            bool isIgnore = false;

            bool isOk = false;
            string strError = "",
                   strAdv = "",
                   strStack = "";

            /// 启动message center
            isOk = StartMessageCenter(ref strError);
            if (!isOk)
            {
                Logger.Error("RunTestCaseOrTestStoryBoard",$"Can't start Message center, SOME FEATURES CAN'T WORK CORRECTL. Error is{strError}");
                //return MarsDriverConst.exit_code_error_startMsgCenter;
            }

            /**
             * 是否需要指定的注入模式
             * */
            string strSpecialRequire = GetParaValueByIdx(arrParameters, "-ExtRequire", null);
            Console.WriteLine($"SpecialRequire:[{strSpecialRequire}]");
            Logger.Info("RunTestCaseOrTestStoryBoard", $"SpecialRequire:[{strSpecialRequire}]");

            var z = arrParameters.Select((t, index) => new { value = t, idx = index })
                .Where(p => string.Compare(p.value, "-currentDB", true) == 0)
                .FirstOrDefault();
            string strCurrentDB = "MarsEntities";
            if (z != null)
            {
                if (arrParameters.Length > (z.idx + 1))
                {
                    strCurrentDB = arrParameters[z.idx + 1];
                }
            }
            if (arrParameters.Length < 8)
            {
                isContinue = false;
                isIgnore = false;
            }
            else
            {
                if (!bool.TryParse(arrParameters[7], out isContinue))
                {
                    isContinue = false;
                }
                if (arrParameters.Length >= 10)
                {
                    if (!bool.TryParse(arrParameters[9], out isIgnore))
                    {
                        isIgnore = false;
                    }
                }

            }

            /// 初始化回调数据
            /// 
            MarsRESTfulApiClient restClient = new MarsRESTfulApiClient(strCurrentDB);
            MarsGlobarVar.MARS_WEB_HOST = $"{restClient.webURLPreFix}Storyboard/UpdateStoryboardStatus?sessionId={MarsGlobarVar.UUID_FROM_WEB}";
            MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK = $"{restClient.webURLPreFix}StoryBoard/UpdateStoryboardExecuteStatus?";
            /// 设置全局数据
            /// 
            MarsGlobarVar.MARS_CURRENT_DB = strCurrentDB;

            #region  // get keyword mapping infor
            Logger.Info("RunTestCaseOrTestStoryBoard", "getKeywordMappingInfo from server ");
            string strDataKeywordMapping = restClient.getKeywordMappingInfo(ref strError, ref isOk);
            if (isOk)
            {
                KeywordControlTypeMappingMgmt.saveApiDataToFile(strDataKeywordMapping, ref strError, ref isOk);
                if (isOk)
                    Logger.Info("RunTestCaseOrTestStoryBoard", $"{strDataKeywordMapping}\r\nhave saved to api data file");
                else
                    Logger.Error("RunTestCaseOrTestStoryBoard", $"{strDataKeywordMapping}\r\nhave not saved to api data file\r\n{strError}");
            }
            #endregion

            strError = "";
            isOk = StoryboardExecute.ExecuteTest(strCurMarsAccount, strStoryboardId,
                strStoryboardName, strAppId, strMode,
                isContinue, isIgnore, strSpecialRequire, strCurrentDB,
                ref strError, ref strAdv, ref strStack, ref isExitItself, isVerifyValueSkipper);
            if (!isOk)
            {
                Logger.Error("RunTestCaseOrTestStoryBoard", $"[{strError}] adv:{strAdv} stack:{strStack}");
                Console.WriteLine("Error when Execute Test storyboard:[{0}], \r\n\t{1}", strStoryboardName, strError);
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack,
                    StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                return MarsDriverConst.exit_code_error_executeTestCaseFailed;
            }

            Console.WriteLine(string.IsNullOrEmpty(strError) ? "" : strError);
            Console.WriteLine("Storybord [{0}] Executed successfully. ", strStoryboardName);
            return MarsDriverConst.exit_code_ExecuteTestCase_Ok;
        }

        private static bool ExtractWPFCore(ref string strError)
        {
            Logger.logBegin("ExtractWPFCore");

            string appBasePath = AppContext.BaseDirectory;  // 当前应用目录
            string zipPath = Path.Combine(appBasePath, "MarsCore.zip");
            string extractPath = Path.Combine(appBasePath, "MarsCore");
            try
            {
                if (!Directory.Exists(extractPath))
                {
                    Console.WriteLine("unzip MarsCore.zip...");
                    ZipFile.ExtractToDirectory(zipPath, appBasePath); // 因zip文件中有个marscore目录
                    Console.WriteLine("unzipped");
                }
                else
                {
                    Console.WriteLine("directory MarsCore Exists，skip。");
                }
                return true;
            }
            catch(Exception e)
            {
                Logger.Error("ExtractWPFCore", strError=$"Error when Extract .netcore/WPF engine|{e.Message}" ,e);
                return false;
            }
            
        }

        private static string GetParaValueByIdx(string[] fromPara, string idx, string strDefaultV)
        {
            int iLen = -1;
            for (int i = 0; i < (iLen = (fromPara == null ? -1 : fromPara.Length)); i++)
            {
                string strItm = fromPara[i];
                if (string.IsNullOrEmpty(strItm)) continue;
                if (string.Compare(strItm, idx, true) == 0)
                {
                    if (i >= iLen - 1)
                    {
                        return strDefaultV;
                    }
                    else
                    {
                        return fromPara[i + 1];
                    }
                }
            }
            return strDefaultV;
        }

        private static string GetParaValueByIdx(string v, ref bool isOk)
        {
            throw new NotImplementedException();
        }
        #endregion //business part

#if !_forWebClient
       
#endif
    }



}


namespace Mars.AutoTestingDriver.SystemUtil
{
    //public partial class MarsClipboardURLPara
    //{
    //    public override string ToString()
    //    {
    //        return $"{{app-{this.app}, command-{this.command}, guid-{this.guid}, storyBoardId-{this.storyBoardId}, storyBoardName-{this.storyBoardName}, userName-{this.userName}}}";
    //    }

    //    public string getDataFromCipboard()
    //    {
    //        try
    //        {
    //            IDataObject iData = Clipboard.GetDataObject();
    //            if (iData.GetDataPresent(DataFormats.Text))
    //            {
    //                return (String)iData.GetData(DataFormats.Text);
    //            }
    //            return null;

    //        }
    //        catch (Exception e)
    //        {
    //            return null;
    //        }
    //    }

    //    public bool ValidateIds()
    //    {
    //        if (this.testStepsFromClipboard == null) return false;
    //        for(int i=0;i< this.testStepsFromClipboard.Count; i++)
    //        {
    //            var itm = this.testStepsFromClipboard[i];
    //            if (itm == null) return false;

    //            int iId = 0;
    //            if ((!int.TryParse(itm.k, out iId))
    //                || (!int.TryParse(itm.o_Id, out iId))
    //                || (!int.TryParse(itm.objN_Id, out iId)))
    //                return false;
    //        }
    //        return true;    
    //    }

    //    public bool ISMarsJSONFormat()
    //    {
    //        try
    //        {
    //            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
    //            this.testStepsFromClipboard = js.Deserialize<List<MARSStepsFromClipboardJsonFormat>>(this.StepsFromClipboard);
    //            return true;
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine($"ISMarsJSONFormat exception:[{e.Message}], \r\n\t{e.StackTrace}");
    //            return false;
    //        }

    //    }
    //    /// <summary>
    //    /// 判断是否存在必要的参数
    //    /// </summary>
    //    /// <returns></returns>
    //    public bool validateURL(ref string strError, ref string strAdv, ref string strStack)
    //    {
    //        if (string.IsNullOrEmpty(this.currentDB))
    //        {
    //            strError = "Current DB information is NULL or empty.";
    //            strStack = Environment.StackTrace;
    //            strAdv = "Contact Marquis";
    //            return false;
    //        }
    //        int iAppId;
    //        if ((string.IsNullOrEmpty(this.app))
    //            ||(!int.TryParse(this.app.Trim(), out iAppId)))
    //        {
    //            strError = "application information desn't pass correctly.";
    //            strStack = Environment.StackTrace;
    //            strAdv = "Contact Marquis";
    //            return false ;
    //        }
    //        return true;
    //    }

    //    public bool ParseURL(string strURI)
    //    {
    //        try
    //        {
    //            //userName=tiger&command=-FromClipboard&storyBoadName=temp&storyBoardId=-1&app=213&guid=a1b06b48-041f-442c-94eb-d7480c57a647&currentDB=GEN_MARS_10	
    //            List<string> lstParas = new List<string>();
    //            Uri uriFromClick = new Uri(strURI);
    //            var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
    //            this.command = query.Get("command");
    //            this.app = query.Get("app");
    //            this.guid = query.Get("guid");
    //            this.storyBoardId = query.Get("storyBoardId");
    //            this.storyBoardName = query.Get("storyBoardName");
    //            this.userName = query.Get("userName");
    //            this.currentDB = query.Get("currentDB");
    //            if (string.IsNullOrEmpty(this.command) || (!AutoTestingDriverEntry.cnst_uri_command_clipboard.Equals(this.command)))
    //                return false;

    //            this.StepsFromClipboard = getDataFromCipboard();
    //            if (string.IsNullOrEmpty(this.StepsFromClipboard)) 
    //                return false;

    //            if (!ISMarsJSONFormat())
    //                return false;
    //            if (!ValidateIds())
    //                return false;

    //            return true;
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine($"MarsClipboardURLPara.ParseURL Generate Exceptions:{e.Message},\r\n{e.StackTrace}");
    //            return false;
    //        }         

    //    }
    //}
}