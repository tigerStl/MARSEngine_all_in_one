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
using MarsEngineSvc.basicReturnDataStructure;
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

        /// <summary>
        /// BatchFold（批量折叠）命令字。
        /// 与 ClickOnce / HTTP 启动 URL 中的 query 参数 command=-BatchFold 对应。
        /// </summary>
        public const string cnst_uri_command_batchfold = "-BatchFold";

        /// <summary>
        /// Fold（单 Storyboard 折叠）命令字。
        /// 与 ClickOnce / HTTP 启动 URL 中的 query 参数 command=-Fold 对应。
        /// </summary>
        public const string cnst_uri_command_fold = "-Fold";

        /// <summary>
        /// 当前进程由 BatchFold URL 启动时解析得到的参数（仅 BatchFold 模式有值）。
        /// </summary>
        public static MarsBatchFoldURLPara CurrentBatchFoldInfo { get; private set; }

        /// <summary>
        /// 当前进程由 Fold URL 启动时解析得到的参数（仅 Fold 模式有值）。
        /// </summary>
        public static MarsFoldURLPara CurrentFoldInfo { get; private set; }

        /// <summary>
        /// 统一记录 ClickOnce / Main 分流分支日志，便于对照当前进入了哪条处理路径。
        /// 仅写日志，不改变任何业务逻辑。
        /// </summary>
        private static void LogDispatchBranch(string branchName, string detail = null)
        {
            string msg = string.IsNullOrEmpty(detail)
                ? $"[Branch:{branchName}] entered"
                : $"[Branch:{branchName}] {detail}";
            Console.WriteLine(msg);
            try
            {
                Logger.Info("Main.Dispatch", msg);
            }
            catch
            {
                // 日志失败不影响主流程
            }
        }

        /// <summary>
        /// 判断激活 URL 是否为 BatchFold 模式，并解析参数。
        /// <para>
        /// 样例：
        /// <c>http://host/MARSENGINE/Mars.AutoTestingDriver.application?userName=Regr1&amp;uuid=7aae010e-a6f3-45e2-baec-664ecae8a5a2&amp;command=-BatchFold&amp;FoldList=(1,323,432,5)&amp;Continue=True&amp;currentDB=MARS2&amp;callBackServer=localhost%3A5000</c>
        /// </para>
        /// <para>
        /// 模板：
        /// <c>?userName=[UserName]&amp;uuid=[Uuid]&amp;command=-BatchFold&amp;FoldList=(1,323,432,5)&amp;Continue=[True|False]&amp;currentDB=[Db]&amp;callBackServer=[Host]</c>
        /// </para>
        /// </summary>
        /// <param name="strURI">ClickOnce ActivationData[0] 完整 URL</param>
        /// <param name="batchFoldInfo">解析成功时输出参数对象</param>
        /// <param name="strError">失败错误信息</param>
        /// <param name="strAdv">失败建议</param>
        /// <param name="strStack">失败堆栈</param>
        /// <returns>
        /// true：确认为 BatchFold 且参数合法；
        /// false：不是 BatchFold（交其它模式），或是 BatchFold 但参数非法（此时 strError 有值）。
        /// </returns>
        private static bool TryParseBatchFoldFromURI(string strURI, out MarsBatchFoldURLPara batchFoldInfo,
            ref string strError, ref string strAdv, ref string strStack)
        {
            batchFoldInfo = null;
            try
            {
                // 先只看 command，避免把非 BatchFold URL 误判成参数错误
                Uri uriFromClick = new Uri(strURI);
                var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get(MarsBatchFoldURLPara.QUERY_COMMAND);
                if (string.IsNullOrEmpty(strCmd)
                    || !string.Equals(strCmd, cnst_uri_command_batchfold, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var info = new MarsBatchFoldURLPara();
                if (!info.ParseURL(strURI) || !info.isOk)
                {
                    strError = info.parseError ?? "Failed to parse BatchFold URL";
                    strAdv = info.parseAdvice ?? "Please start BatchFold from MARS GUI";
                    strStack = info.parseStack ?? Environment.StackTrace;
                    // 已确认是 BatchFold 命令但参数非法：用特殊标记让调用方区分「不是该模式」与「该模式参数错误」
                    batchFoldInfo = info;
                    return false;
                }

                batchFoldInfo = info;
                return true;
            }
            catch (Exception e)
            {
                strError = $"Error while detecting BatchFold URL: {e.Message}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                return false;
            }
        }

        /// <summary>
        /// 仅探测 URL 的 command 是否为 -BatchFold（不做完整校验）。
        /// 用于在 CombinParaFromURI（Storyboard 参数组合）之前分流，避免 FoldList 等字段被当成 Storyboard 参数解析失败。
        /// </summary>
        private static bool IsBatchFoldCommand(string strURI)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(strURI)) return false;
                Uri uriFromClick = new Uri(strURI);
                var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get(MarsBatchFoldURLPara.QUERY_COMMAND);
                return !string.IsNullOrEmpty(strCmd)
                    && string.Equals(strCmd, cnst_uri_command_batchfold, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 仅探测 URL 的 command 是否为 -Fold（不做完整校验）。
        /// 必须在 CombinParaFromURI 之前分流：-Fold 与 -S 字段相似，但走独立 Fold 分支，不改动原有 Storyboard 逻辑。
        /// </summary>
        private static bool IsFoldCommand(string strURI)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(strURI)) return false;
                Uri uriFromClick = new Uri(strURI);
                var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get(MarsFoldURLPara.QUERY_COMMAND);
                return !string.IsNullOrEmpty(strCmd)
                    && string.Equals(strCmd, cnst_uri_command_fold, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析 Fold ClickOnce URL。
        /// 模板：
        /// <c>?userName=[UserName]&amp;uuid=[Uuid]&amp;command=-Fold&amp;storyBoadName=[SB_Name]&amp;storyBoardId=[Id]&amp;app=[AppId]&amp;Mode=Base&amp;Continue=False&amp;IgnoreError=False&amp;currentDB=[Db]&amp;callBackServer=[Host]</c>
        /// </summary>
        private static bool TryParseFoldFromURI(string strURI, out MarsFoldURLPara foldInfo,
            ref string strError, ref string strAdv, ref string strStack)
        {
            foldInfo = null;
            try
            {
                Uri uriFromClick = new Uri(strURI);
                var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                string strCmd = query.Get(MarsFoldURLPara.QUERY_COMMAND);
                if (string.IsNullOrEmpty(strCmd)
                    || !string.Equals(strCmd, cnst_uri_command_fold, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var info = new MarsFoldURLPara();
                if (!info.ParseURL(strURI) || !info.isOk)
                {
                    strError = info.parseError ?? "Failed to parse Fold URL";
                    strAdv = info.parseAdvice ?? "Please start Fold from MARS GUI";
                    strStack = info.parseStack ?? Environment.StackTrace;
                    foldInfo = info;
                    return false;
                }

                foldInfo = info;
                return true;
            }
            catch (Exception e)
            {
                strError = $"Error while detecting Fold URL: {e.Message}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                return false;
            }
        }

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

        private static void KillEngineProcesss()
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
            // Also kill any running MarsDefaultAgent processes started in the same session
            try
            {
                Console.WriteLine("begin to kill MarsDefaultAgent processes");
                var agentProcs = Process.GetProcessesByName("MarsDefaultAgent");
                foreach (var ap in agentProcs)
                {
                    if (ap == null) continue;
                    if (ap.SessionId != p.SessionId) continue;
                    try
                    {
                        ap.Kill();
                        Console.WriteLine($"\tkill MarsDefaultAgent:[id-{ap.Id}]");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\tfailed to kill MarsDefaultAgent id:{ap.Id}, {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while killing MarsDefaultAgent: {ex.Message}");
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
        /// 4，BatchFold（批量折叠）——由 MARS Web / ClickOnce HTTP URL 传入：
        ///    /MARSENGINE/Mars.AutoTestingDriver.application?userName=Regr1&amp;uuid=7aae010e-a6f3-45e2-baec-664ecae8a5a2
        ///        &amp;command=-BatchFold&amp;FoldList=(1,323,432,5)&amp;Continue=True&amp;currentDB=MARS2&amp;callBackServer=localhost%3A5000
        ///    模板：
        ///    ?userName=[UserName]&amp;uuid=[Uuid]&amp;command=-BatchFold&amp;FoldList=(1,323,432,5)
        ///        &amp;Continue=[True|False]&amp;currentDB=[Db]&amp;callBackServer=[Host]
        /// 5，Fold（单 Storyboard 折叠）——独立分支，不改动 -S / CombinParaFromURI：
        ///    /MARSENGINE/Mars.AutoTestingDriver.application?userName=[UserName]&amp;uuid=[Uuid]&amp;command=-Fold
        ///        &amp;storyBoadName=[SB_Name]&amp;storyBoardId=[Id]&amp;app=[AppId]&amp;Mode=Base
        ///        &amp;Continue=False&amp;IgnoreError=False&amp;currentDB=[Db]&amp;callBackServer=[Host]
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
                KillEngineProcesss();

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
                    // BatchFold（批量折叠）样例 / 模板：
                    // http://host/MARSENGINE/Mars.AutoTestingDriver.application?userName=Regr1&uuid=7aae010e-a6f3-45e2-baec-664ecae8a5a2&command=-BatchFold&FoldList=(1,323,432,5)&Continue=True&currentDB=MARS2&callBackServer=localhost%3A5000
                    // ?userName=[UserName]&uuid=[Uuid]&command=-BatchFold&FoldList=(1,323,432,5)&Continue=[True|False]&currentDB=[Db]&callBackServer=[Host]
                    // Fold（单 Storyboard 折叠）样例 / 模板：
                    // /MARSENGINE/Mars.AutoTestingDriver.application?userName=[UserName]&uuid=[Uuid]&command=-Fold&storyBoadName=[SB_Name]&storyBoardId=[Id]&app=[AppId]&Mode=Base&Continue=False&IgnoreError=False&currentDB=[Db]&callBackServer=[Host]
#if _remoteDebug
                    Console.ReadLine();
#endif                    //url:http://mars05.eastus.cloudapp.azure.com/Download/Mars.AutoTestingDriver.application?userName=admin%25commmd=-S&storbyBoadName=TPG_COMPLETE_DEMO&&storyBoardId=383507&app=14&Mode=Base&Continue=False&IgnoreError=False&CurrentDB=MarsEntities

                    // url for steps mode：MARSENGINE/Mars.AutoTestingDriver.application?userName=tiger&command=-FromClipboard&storyBoadName=temp&storyBoardId=-1&app=213&guid=a1b06b48-041f-442c-94eb-d7480c57a647&currentDB=GEN_MARS_10	
                    string[] arrParas = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;
                    Console.WriteLine("URL [{0}]-[{1}]\r\n\t[{2}]", AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData, AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData.GetType(),
                        string.Join(",", arrParas));
                    LogDispatchBranch("ClickOnce.Activation", $"rawUrlCount={(arrParas == null ? -1 : arrParas.Length)}");

                    if ((arrParas == null) || (arrParas.Length <= 0))
                    {
                        LogDispatchBranch("ClickOnce.NoParameters", "activation data empty -> exit paraFormat");
                        Console.WriteLine("Total Command Count:[{0}] paras:[{1}]", arrParas == null ? -1 : arrParas.Length, arrParas == null ? "N/A" : String.Join(";", arrParas));
                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError("No Parameters to start MARS engine", "N/A", "N/A", "N/A", "please start Engine from MARS GUI", "N/A", "N/A",
                            false);
                        return MarsDriverConst.exit_code_error_paraFormat;
                    }

                    if (isToStartMarsSpy(arrParas[0], ref strAddressAndPort, ref query))
                    {
                        LogDispatchBranch("ClickOnce.Spy", $"addressAndPort={strAddressAndPort}");
                        Console.WriteLine("Spy mode.......");
                        Logger.Info("is SpyMode");
                        return StartSpyMode(strAddressAndPort, query);
                    }
                    if (isToStartMessageAgent(arrParas[0], ref query))
                    {
                        LogDispatchBranch("ClickOnce.MessageAgent", "cmd=startMessageAgent");
                        Console.WriteLine("start Message Agent mode.......");
                        return StartMessageAgentMode(query);
                    }
                    if (isToStartMarsSVC(arrParas[0], ref strAddressAndPort, ref query))
                    {
                        LogDispatchBranch("ClickOnce.EngineSvc", "command=startEngineSvc");
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
                        LogDispatchBranch("ClickOnce.RecordReplay", $"addressAndPort={strAddressAndPort}");
                        Console.WriteLine("Spy mode.......");
                        return StartSpyRecordAndReplayMode(strAddressAndPort, query);
                    }

                    string strRecordRplayMode = "";
                    if (isToStartObjRecorgMode(arrParas[0], ref strAddressAndPort, ref strRecordRplayMode, ref query))
                    {
                        LogDispatchBranch("ClickOnce.ObjRecord", $"mode={strRecordRplayMode}, addressAndPort={strAddressAndPort}");
                        return StartObjectRecordMode(strAddressAndPort, strRecordRplayMode, query);
                    }

                    bool isTmpStoryboardAndTestStepsMode = IsTmpStoryboardAndTestStepsMode(arrParas[0]);
                    if (isTmpStoryboardAndTestStepsMode)
                    {
                        LogDispatchBranch("ClickOnce.FromClipboard", "command=-FromClipboard");
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

                    // ------------------------------------------------------------------------------------------------
                    // BatchFold 模式分流（必须在 CombinParaFromURI 之前）：
                    // Storyboard 的 CombinParaFromURI 依赖 storyBoardId/app/Mode 等字段；
                    // BatchFold URL 使用 FoldList / callBackServer，若落入 CombinParaFromURI 会误报参数错误。
                    //
                    // URL 结构：
                    //   /MARSENGINE/Mars.AutoTestingDriver.application
                    //     ?userName=[UserName]
                    //     &uuid=[Uuid]
                    //     &command=-BatchFold
                    //     &FoldList=(1,323,432,5)
                    //     &Continue=[True|False]
                    //     &currentDB=[Db]
                    //     &callBackServer=[Host]          // 例：localhost%3A5000 -> localhost:5000
                    // ------------------------------------------------------------------------------------------------
                    if (IsBatchFoldCommand(arrParas[0]))
                    {
                        LogDispatchBranch("ClickOnce.BatchFold", "command=-BatchFold");
                        Console.WriteLine("BatchFold mode detected from activation URL.......");
                        MarsBatchFoldURLPara batchFoldInfo;
                        if (!TryParseBatchFoldFromURI(arrParas[0], out batchFoldInfo, ref strError, ref strAdv, ref strStack)
                            || batchFoldInfo == null || !batchFoldInfo.isOk)
                        {
                            MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(
                                string.IsNullOrEmpty(strError) ? "Invalid BatchFold URL parameters" : strError,
                                "N/A", "N/A", "N/A",
                                string.IsNullOrEmpty(strAdv) ? "Please start BatchFold from MARS GUI" : strAdv,
                                "N/A",
                                string.IsNullOrEmpty(strStack) ? "N/A" : strStack,
                                true);
                            return MarsDriverConst.exit_code_error_paraFormat;
                        }

                        CurrentBatchFoldInfo = batchFoldInfo;
                        Console.WriteLine($"BatchFold parsed: {batchFoldInfo}");

                        // 写入全局会话 / DB / 回调相关信息（与 Storyboard URL 启动路径保持一致）
                        MarsGlobarVar.UUID_FROM_WEB = batchFoldInfo.uuid;
                        MarsGlobarVar.MARS_CURRENT_DB = batchFoldInfo.currentDB;

                        MLogger.LOGGER_NAME = "AutoDriver";
                        Logger = MLogger.GetLogger(typeof(AutoTestingDriverEntry));
                        clientWCF::MarsTestFrame.SourceCode.xmlConfig.WCFXmlCfgMgr.CurrentLoginUser = batchFoldInfo.userName;
                        Console.WriteLine($"get current Login User {batchFoldInfo.userName}");

                        if (!WriteCurrentMarsAccountFile(batchFoldInfo.userName, ref strError))
                        {
                            Console.WriteLine(strError);
                            MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, null, null, null, "Contact Marquis",
                                null, Environment.StackTrace, true);
                            return MarsDriverConst.exit_code_error_cantCreateMarsAccountFile;
                        }

                        // 启动 Monitor（与其它执行模式一致，后续业务实现可依赖 Monitor 端口）
                        if (!startTestMonitorSvc(batchFoldInfo.userName, ref iMonitorPort, ref strError, ref strAdv, ref strStack))
                        {
                            MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                            return MarsDriverConst.exit_code_error_cantStartMonitor;
                        }
                        TestFlowClientMainEntry.currentMarsUserAccount = batchFoldInfo.userName;
                        TestFlowClientMainEntry.currentMarsMonitorPort = iMonitorPort;
                        MARSMessageSvc.currentUserName = batchFoldInfo.userName;

                        return RunBatchFold(batchFoldInfo, ref strError, ref strAdv, ref strStack);
                    }

                    // ------------------------------------------------------------------------------------------------
                    // Fold 模式分流（独立于 CombinParaFromURI / -S；不改动其实现）：
                    //   解析 URL 后组装与 -S 同形的 args，再 fall-through 到 Main 后续公共初始化
                    //   （WriteAccount / Monitor / ChangeConfigFile / ExtractWPFCore 等），
                    //   最终由 switch -FOLD -> RunFold -> StoryboardExecute.ExecuteTest。
                    // ------------------------------------------------------------------------------------------------
                    if (IsFoldCommand(arrParas[0]))
                    {
                        LogDispatchBranch("ClickOnce.Fold", "command=-Fold");
                        Console.WriteLine("Fold mode detected from activation URL.......");
                        MarsFoldURLPara foldInfo;
                        if (!TryParseFoldFromURI(arrParas[0], out foldInfo, ref strError, ref strAdv, ref strStack)
                            || foldInfo == null || !foldInfo.isOk)
                        {
                            LogDispatchBranch("ClickOnce.Fold.ParseFailed", strError);
                            MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(
                                string.IsNullOrEmpty(strError) ? "Invalid Fold URL parameters" : strError,
                                "N/A", "N/A", "N/A",
                                string.IsNullOrEmpty(strAdv) ? "Please start Fold from MARS GUI" : strAdv,
                                "N/A",
                                string.IsNullOrEmpty(strStack) ? "N/A" : strStack,
                                true);
                            return MarsDriverConst.exit_code_error_paraFormat;
                        }

                        CurrentFoldInfo = foldInfo;
                        LogDispatchBranch("ClickOnce.Fold.Parsed", foldInfo.ToString());
                        Console.WriteLine($"Fold parsed: {foldInfo}");

                        MarsGlobarVar.UUID_FROM_WEB = foldInfo.uuid;
                        MarsGlobarVar.MARS_CURRENT_DB = foldInfo.currentDB;
                        MarsGlobarVar.MARS_current_StoryboardId = foldInfo.storyBoardId;

                        // 组装 args 后落入与 -S 相同的 Main 后续路径（勿在此直接 RunFold，否则会跳过配置/Monitor 等前置）
                        args = BuildCommandArgsFromFoldInfo(foldInfo);
                        StoryboardExecute.currentParameters = args;
                        LogDispatchBranch("ClickOnce.Fold.FallThrough",
                            $"fall-through Main common init; args=[{string.Join(" ", args)}]");
                        Console.WriteLine($"Fold combined args:{string.Join(" ", args)}");
                    }
                    else
                    {
                        // 默认：Storyboard(-S) 等原有 CombinParaFromURI 路径（逻辑保持不变）
                        LogDispatchBranch("ClickOnce.CombinParaFromURI", "fallback to legacy Storyboard/-S parameter combine");
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
                            LogDispatchBranch("CmdSwitch.Storyboard", $"cmd={strCmd}");
                            return RunTestCaseOrTestStoryBoard(strCurMarsAccount, arrParaWithoutApp, ref isExitItself);
                        // 命令行形式入口（与 ClickOnce URL 分流互补）：
                        //   [userName] -BatchFold -FoldList "(1,323,432,5)" -Continue True -CurrentDB MARS2 -CallBackServer localhost:5000 -Uuid xxxx
                        case "-BATCHFOLD":
                            {
                                LogDispatchBranch("CmdSwitch.BatchFold", "cmd=-BATCHFOLD");
                                MarsBatchFoldURLPara fromArgs = BuildBatchFoldInfoFromArgs(strCurMarsAccount, arrParaWithoutApp,
                                    ref isOk, ref strError, ref strAdv, ref strStack);
                                if (!isOk || fromArgs == null)
                                {
                                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                                    return MarsDriverConst.exit_code_error_paraFormat;
                                }
                                CurrentBatchFoldInfo = fromArgs;
                                MarsGlobarVar.UUID_FROM_WEB = fromArgs.uuid;
                                MarsGlobarVar.MARS_CURRENT_DB = fromArgs.currentDB;
                                return RunBatchFold(fromArgs, ref strError, ref strAdv, ref strStack);
                            }
                        // 命令行 / ClickOnce fall-through：
                        //   [userName] -Fold [SB_Name] [Id] -App [AppId] -Mode Base -Continue False -IgnoreError False -CurrentDB [Db] -CallBackServer [Host] -Uuid [Uuid]
                        case "-FOLD":
                            {
                                LogDispatchBranch("CmdSwitch.Fold", "cmd=-FOLD");
                                MarsFoldURLPara foldInfo = CurrentFoldInfo;
                                if (foldInfo == null || !foldInfo.isOk)
                                {
                                    foldInfo = BuildFoldInfoFromArgs(strCurMarsAccount, arrParaWithoutApp,
                                        ref isOk, ref strError, ref strAdv, ref strStack);
                                    if (!isOk || foldInfo == null)
                                    {
                                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                                        return MarsDriverConst.exit_code_error_paraFormat;
                                    }
                                    CurrentFoldInfo = foldInfo;
                                }
                                MarsGlobarVar.UUID_FROM_WEB = foldInfo.uuid;
                                MarsGlobarVar.MARS_CURRENT_DB = foldInfo.currentDB;
                                MarsGlobarVar.MARS_current_StoryboardId = foldInfo.storyBoardId;
                                return RunFold(foldInfo, ref isExitItself);
                            }
                        default:
                            LogDispatchBranch("CmdSwitch.Unknown", $"unsupported cmd={strCmd}");
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

        /// <summary>
        /// 从命令行参数数组构建 <see cref="MarsFoldURLPara"/>。
        /// 期望形式（去掉 userName 与 -Fold 之后）：
        /// <code>
        /// [SB_Name] [Id] -App [AppId] -Mode Base -Continue False -IgnoreError False -CurrentDB [Db] -CallBackServer [Host] -Uuid [Uuid]
        /// </code>
        /// 位置参数对齐 CombinParaFromURI 产出的 Storyboard 参数顺序，便于对照；本方法独立实现，不调用 CombinParaFromURI。
        /// </summary>
        private static MarsFoldURLPara BuildFoldInfoFromArgs(string userName, string[] arrParameters,
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            isOk = false;
            strAdv = "Please start Fold from MARS GUI or pass valid -Fold parameters";
            strStack = Environment.StackTrace;
            try
            {
                if (arrParameters == null || arrParameters.Length < 2)
                {
                    strError = "Fold args require at least storyBoardName and storyBoardId";
                    return null;
                }

                var info = new MarsFoldURLPara
                {
                    userName = userName,
                    command = cnst_uri_command_fold,
                    storyBoardName = arrParameters[0],
                    storyBoardId = arrParameters[1],
                    appId = GetParaValueByIdx(arrParameters, "-App", null),
                    mode = GetParaValueByIdx(arrParameters, "-Mode", null),
                    currentDB = GetParaValueByIdx(arrParameters, "-CurrentDB", null)
                        ?? GetParaValueByIdx(arrParameters, "-currentDB", null),
                    callBackServer = GetParaValueByIdx(arrParameters, "-CallBackServer", null)
                        ?? GetParaValueByIdx(arrParameters, "-callBackServer", null),
                    uuid = GetParaValueByIdx(arrParameters, "-Uuid", null)
                        ?? GetParaValueByIdx(arrParameters, "-uuid", null)
                        ?? MarsGlobarVar.UUID_FROM_WEB
                };

                // 去掉 CombinParaFromURI 可能加上的引号包裹
                if (!string.IsNullOrEmpty(info.storyBoardName)
                    && info.storyBoardName.Length >= 2
                    && info.storyBoardName.StartsWith("\"")
                    && info.storyBoardName.EndsWith("\""))
                {
                    info.storyBoardName = info.storyBoardName.Substring(1, info.storyBoardName.Length - 2);
                }

                string strContinue = GetParaValueByIdx(arrParameters, "-Continue", null);
                if (string.IsNullOrWhiteSpace(strContinue) || !bool.TryParse(strContinue.Trim(), out bool cont))
                {
                    strError = $"Parameter [-Continue] missing or invalid:[{strContinue}]";
                    return null;
                }
                info.isContinue = cont;

                string strIgnore = GetParaValueByIdx(arrParameters, "-IgnoreError", "False");
                if (string.IsNullOrWhiteSpace(strIgnore))
                    info.ignoreError = false;
                else if (!bool.TryParse(strIgnore.Trim(), out bool ign))
                {
                    strError = $"Parameter [-IgnoreError] is not a boolean:[{strIgnore}]";
                    return null;
                }
                else
                    info.ignoreError = ign;

                if (!info.Validate(ref strError, ref strAdv, ref strStack))
                    return null;

                info.isOk = true;
                isOk = true;
                return info;
            }
            catch (Exception e)
            {
                strError = $"Error building Fold parameters from args: {e.Message}";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                return null;
            }
        }

        /// <summary>
        /// 将 <see cref="MarsFoldURLPara"/> 组装为与 CombinParaFromURI / -S 同形的命令行 args，
        /// 以便 fall-through Main 公共初始化后由 switch -FOLD 进入 <see cref="RunFold"/>。
        /// 格式：
        /// <c>[userName] -Fold [SB_Name] [Id] -App [AppId] -Mode [Mode] -Continue [T|F] -IgnoreError [T|F] -CurrentDB [Db] -CallBackServer [Host] -Uuid [Uuid]</c>
        /// </summary>
        private static string[] BuildCommandArgsFromFoldInfo(MarsFoldURLPara foldInfo)
        {
            string sbName = foldInfo.storyBoardName ?? "";
            if ((!string.IsNullOrEmpty(sbName)) && sbName.Contains(" "))
            {
                sbName = sbName.Replace("\"", "'");
                sbName = $"\"{sbName}\"";
            }

            var lst = new List<string>
            {
                foldInfo.userName,
                cnst_uri_command_fold,
                sbName,
                foldInfo.storyBoardId,
                "-App",
                foldInfo.appId,
                "-Mode",
                foldInfo.mode,
                "-Continue",
                foldInfo.isContinue ? "True" : "False",
                "-IgnoreError",
                foldInfo.ignoreError ? "True" : "False",
                "-CurrentDB",
                foldInfo.currentDB,
                "-CallBackServer",
                foldInfo.callBackServer,
                "-Uuid",
                foldInfo.uuid
            };
            return lst.ToArray();
        }

        /// <summary>
        /// Fold-Storyboard 执行入口：按 <see cref="StoryboardExecute.ExecuteTest"/> 的既有调用约定执行，
        /// <b>不修改</b> ExecuteTest 实现。
        /// <para>
        /// ExecuteTest 调用规则（对齐 <see cref="RunTestCaseOrTestStoryBoard"/> / CI）：
        /// <list type="number">
        /// <item>Main 公共前置已完成：账号文件、Monitor、ChangeConfigFile、ExtractWPFCore、InitDBInfo 等</item>
        /// <item>调用前启动 MessageCenter（允许失败仅记日志）</item>
        /// <item>设置 MarsGlobarVar（UUID / CURRENT_DB / 回调 URL）；Fold 优先用 callBackServer 作为 API base</item>
        /// <item>可选拉取 keyword mapping 并落盘</item>
        /// <item>调用
        /// <c>ExecuteTest(userName, storyBoardId, storyBoardName, appId, mode, isContinue, isIgnore, specialRequire, currentDB, ref error/adv/stack, ref isExitItself, isVerifyValueSkipper)</c>
        /// </item>
        /// <item>storyBoardId / appId 必须为数字字符串；mode 大小写不敏感（Base=基线）</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="fromBatch">
        /// true：由 BatchFold 循环调用。日志标明来自 batch；
        /// 强制 Continue=False / IgnoreError=False，任一 test step 失败即中断该 Fold storyboard，返回后由 BatchFold 跳到下一项。
        /// </param>
        private static int RunFold(MarsFoldURLPara foldInfo, ref bool isExitItself, bool fromBatch = false)
        {
            int iMark = new Random().Next();
            string strError = "", strAdv = "", strStack = "";
            string originTag = fromBatch ? "fromBatch" : "directFold";
            LogDispatchBranch("RunFold", $"mark={iMark}|begin|origin={originTag}|{foldInfo}");
            Logger.logBegin($"{iMark}|RunFold|origin={originTag}");
            Console.WriteLine($"RunFold begin.... origin={originTag} {foldInfo}");
            if (fromBatch)
            {
                Logger.Info("RunFold", "origin=BatchFold|this Fold storyboard execution is invoked from BatchFold mode");
                Console.WriteLine("[RunFold] Invoked from BatchFold mode (batch loop).");
            }
            try
            {
                if (foldInfo == null || !foldInfo.isOk)
                {
                    LogDispatchBranch("RunFold.InvalidPara", "foldInfo null or not ok");
                    strError = "Fold info is null or invalid";
                    strAdv = "Please start Fold from MARS GUI";
                    strStack = Environment.StackTrace;
                    if (!fromBatch)
                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                    return MarsDriverConst.exit_code_error_paraFormat;
                }

                // Batch 调用：强制中断策略——任一 test step 失败即停止本 Fold storyboard
                bool runContinue = fromBatch ? false : foldInfo.isContinue;
                bool runIgnoreError = fromBatch ? false : foldInfo.ignoreError;
                if (fromBatch)
                {
                    Logger.Info("RunFold",
                        $"origin=BatchFold|force Continue=False IgnoreError=False (URL Continue was {foldInfo.isContinue}, IgnoreError was {foldInfo.ignoreError})");
                }

                // ---- 前置 1：MessageCenter（与 RunTestCaseOrTestStoryBoard 一致）----
                LogDispatchBranch("RunFold.StartMessageCenter", $"origin={originTag}");
                bool mcOk = StartMessageCenter(ref strError);
                if (!mcOk)
                {
                    Logger.Error("RunFold", $"Can't start Message center, SOME FEATURES CAN'T WORK CORRECTLY. Error is{strError}");
                }

                // ---- 前置 2：API base / 回调（Fold URL 的 callBackServer 优先）----
                MarsRESTfulApiClient restClient = new MarsRESTfulApiClient(foldInfo.currentDB);
                string apiBaseUrl = null;
                string fromCallBack = foldInfo.callBackServer?.Trim();
                if (!string.IsNullOrWhiteSpace(fromCallBack))
                {
                    if (!fromCallBack.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        && !fromCallBack.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        fromCallBack = "http://" + fromCallBack;
                    if (!fromCallBack.EndsWith("/"))
                        fromCallBack += "/";
                    apiBaseUrl = fromCallBack;
                    restClient.webURLPreFix = apiBaseUrl;
                    MarsRESTfulApiClient.setWebURLPrefix(apiBaseUrl);
                    LogDispatchBranch("RunFold.ApiBase", $"origin={originTag}, source=callBackServer, apiBase=[{apiBaseUrl}]");
                }
                else
                {
                    apiBaseUrl = restClient.webURLPreFix;
                    LogDispatchBranch("RunFold.ApiBase", $"origin={originTag}, source=config, apiBase=[{apiBaseUrl}]");
                }

                MarsGlobarVar.UUID_FROM_WEB = foldInfo.uuid;
                MarsGlobarVar.MARS_CURRENT_DB = foldInfo.currentDB;
                MarsGlobarVar.MARS_current_StoryboardId = foldInfo.storyBoardId;
                MarsGlobarVar.MARS_WEB_HOST = $"{apiBaseUrl}Storyboard/UpdateStoryboardStatus?sessionId={foldInfo.uuid}";
                MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK = $"{apiBaseUrl}StoryBoard/UpdateStoryboardExecuteStatus?";
                Console.WriteLine($"Fold Links|{MarsGlobarVar.MARS_WEB_HOST}|\r\n{MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK}");
                Logger.Info("RunFold",
                    $"origin={originTag}|apiBase|{apiBaseUrl}|uuid|{foldInfo.uuid}|sb|{foldInfo.storyBoardName}|id|{foldInfo.storyBoardId}|app|{foldInfo.appId}|mode|{foldInfo.mode}|Continue|{runContinue}|IgnoreError|{runIgnoreError}");

                // ---- 前置 3：keyword mapping（与 RunTestCaseOrTestStoryBoard 一致，失败不阻断）----
                LogDispatchBranch("RunFold.KeywordMapping", $"origin={originTag}|getKeywordMappingInfo from server");
                bool mapOk = false;
                string mapErr = "";
                string strDataKeywordMapping = restClient.getKeywordMappingInfo(ref mapErr, ref mapOk);
                if (mapOk)
                {
                    KeywordControlTypeMappingMgmt.saveApiDataToFile(strDataKeywordMapping, ref mapErr, ref mapOk);
                    if (mapOk)
                        Logger.Info("RunFold", "keyword mapping saved to api data file");
                    else
                        Logger.Error("RunFold", $"keyword mapping not saved|{mapErr}");
                }
                else
                {
                    Logger.Error("RunFold", $"getKeywordMappingInfo failed|{mapErr}");
                }

                // ---- 前置 4：Fold storyboard 首条必须是 Login（不进入 ExecuteTest）----
                // 判定与 MARSWeb SqlStoryBoardRepository.IsLoginTestCaseName 一致：名称含 login / logon（忽略大小写）
                LogDispatchBranch("RunFold.ValidateFirstLogin",
                    $"origin={originTag}|sbId={foldInfo.storyBoardId}, mode={foldInfo.mode}, db={foldInfo.currentDB}");
                if (!ValidateFoldStoryboardFirstStepIsLogin(foldInfo, ref strError, ref strAdv, ref strStack))
                {
                    LogDispatchBranch("RunFold.ValidateFirstLogin.Failed", $"origin={originTag}|{strError}");
                    Logger.Error("RunFold", $"origin={originTag}|{strError}");
                    Console.WriteLine(strError);
                    if (!fromBatch)
                    {
                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A",
                            string.IsNullOrEmpty(strAdv) ? "Ensure the first test case of the Fold storyboard is Login" : strAdv,
                            "N/A", strStack ?? Environment.StackTrace, true);
                    }
                    return MarsDriverConst.exit_code_ExeFold_Failed;
                }
                LogDispatchBranch("RunFold.ValidateFirstLogin.OK", $"origin={originTag}|first test case is Login");

                // ---- 调用 ExecuteTest（签名/语义严格对齐 RunTestCaseOrTestStoryBoard，不改 ExecuteTest）----
                // ExecuteTest(account, storyboardId, storyboardName, appId, mode,
                //             isContinue, isIgnore, specialRequire, currentDB,
                //             ref error, ref adv, ref stack, ref isQuitSelf, isVerifyValueSkipper)
                string strSpecialRequire = null; // Fold URL 无 -ExtRequire；与 CI 路径传 null 一致
                strError = "";
                LogDispatchBranch("RunFold.ExecuteTest",
                    $"origin={originTag}|BEFORE ExecuteTest user={foldInfo.userName}, sbId={foldInfo.storyBoardId}, sbName={foldInfo.storyBoardName}, " +
                    $"app={foldInfo.appId}, mode={foldInfo.mode}, Continue={runContinue}, IgnoreError={runIgnoreError}, db={foldInfo.currentDB}");
                Logger.Info("RunFold",
                    $"origin={originTag}|BEFORE ExecuteTest|{foldInfo.userName}|{foldInfo.storyBoardId}|{foldInfo.storyBoardName}|{foldInfo.appId}|{foldInfo.mode}|Continue={runContinue}|Ignore={runIgnoreError}|db={foldInfo.currentDB}");

                bool isOk = StoryboardExecute.ExecuteTest(
                    foldInfo.userName,
                    foldInfo.storyBoardId,
                    foldInfo.storyBoardName,
                    foldInfo.appId,
                    foldInfo.mode,
                    runContinue,
                    runIgnoreError,
                    strSpecialRequire,
                    foldInfo.currentDB,
                    ref strError,
                    ref strAdv,
                    ref strStack,
                    ref isExitItself,
                    isVerifyValueSkipper);

                if (!isOk)
                {
                    LogDispatchBranch("RunFold.ExecuteTest.Failed", $"origin={originTag}|{strError}");
                    Logger.Error("RunFold", $"origin={originTag}|[{strError}] adv:{strAdv} stack:{strStack}");
                    Console.WriteLine("Error when Execute Fold storyboard:[{0}], \r\n\t{1}", foldInfo.storyBoardName, strError);
                    if (fromBatch)
                    {
                        Logger.Info("RunFold",
                            $"origin=BatchFold|abort this Fold storyboard on test-step error; BatchFold will continue with next item. sbId={foldInfo.storyBoardId}");
                    }
                    else
                    {
                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack,
                            StoryboardExecute.TestStoryInfoFromCICmd == null ? false : StoryboardExecute.TestStoryInfoFromCICmd.isOk);
                    }
                    return MarsDriverConst.exit_code_ExeFold_Failed;
                }

                LogDispatchBranch("RunFold.ExecuteTest.Success", $"origin={originTag}|sbId={foldInfo.storyBoardId}");
                Console.WriteLine(string.IsNullOrEmpty(strError) ? "" : strError);
                Console.WriteLine("Fold Storyboard [{0}] Executed successfully. origin={1}", foldInfo.storyBoardName, originTag);
                return MarsDriverConst.exit_code_ExeFold_Ok;
            }
            catch (Exception e)
            {
                LogDispatchBranch("RunFold.Exception", $"origin={originTag}|{e.Message}");
                strError = $"Exception in RunFold: {e.Message}";
                strAdv = "Contact Marquis";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                Logger.Error("RunFold", $"origin={originTag}|{e.Message}", e);
                if (!fromBatch)
                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                return MarsDriverConst.exit_code_ExeFold_Failed;
            }
            finally
            {
                Logger.logEnd($"{iMark}|RunFold|origin={originTag}");
            }
        }

        /// <summary>
        /// Fold 模式：校验 storyboard 按 RUN_ORDER 的第一条 test case 必须是 Login。
        /// Login 判定与 Web 端一致：TEST_CASE_NAME 包含 "login" 或 "logon"（忽略大小写）。
        /// </summary>
        private static bool ValidateFoldStoryboardFirstStepIsLogin(MarsFoldURLPara foldInfo,
            ref string strError, ref string strAdv, ref string strStack)
        {
            strStack = Environment.StackTrace;
            strAdv = "Ensure the first test case of the Fold storyboard is a Login test case, then retry.";
            try
            {
                if (!long.TryParse(foldInfo.storyBoardId, out long lStoryboardId))
                {
                    strError = "StoryboardId from Fold URL is not a number";
                    return false;
                }

                bool isBase = string.Compare(foldInfo.mode, "base", StringComparison.OrdinalIgnoreCase) == 0;
                string loadErr = null;
                List<ExecutableTestStoryBoard> lst =
                    ExecutableTestStoryBoard.GetStoryboarddetailListByStoryboardId(
                        lStoryboardId, isBase, ref loadErr, foldInfo.currentDB);

                if (lst == null || lst.Count == 0)
                {
                    strError = string.IsNullOrEmpty(loadErr)
                        ? $"Fold storyboard [{foldInfo.storyBoardName}] (id={foldInfo.storyBoardId}) has no test cases."
                        : loadErr;
                    return false;
                }

                var firstDetail = lst[0];
                var firstObj = firstDetail == null ? null : firstDetail.AssignedTestObjectFromDB;
                string firstName = firstObj == null ? null : firstObj.TEST_CASE_NAME;
                long firstRunOrder = firstObj == null ? -1 : firstObj.RUN_ORDER;
                Logger.Info("ValidateFoldStoryboardFirstStepIsLogin",
                    $"sbId={foldInfo.storyBoardId}|firstTestCase=[{firstName}]|runOrder={firstRunOrder}");

                if (!IsFoldLoginTestCaseName(firstName))
                {
                    // Required English message: first test step of every Fold storyboard must be Login
                    strError =
                        "The first test step of every Fold storyboard must be Login. " +
                        $"Current first test case is [{(string.IsNullOrEmpty(firstName) ? "N/A" : firstName)}] " +
                        $"on storyboard [{foldInfo.storyBoardName}] (id={foldInfo.storyBoardId}).";
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                strError = $"Failed to validate Fold storyboard first Login step: {e.Message}";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                Logger.Error("ValidateFoldStoryboardFirstStepIsLogin", strError, e);
                return false;
            }
        }

        /// <summary>
        /// 与 MARSWeb <c>SqlStoryBoardRepository.IsLoginTestCaseName</c> 保持一致。
        /// </summary>
        private static bool IsFoldLoginTestCaseName(string testCaseName)
        {
            if (string.IsNullOrEmpty(testCaseName))
                return false;
            return testCaseName.IndexOf("login", StringComparison.OrdinalIgnoreCase) >= 0
                || testCaseName.IndexOf("logon", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 从命令行参数数组构建 <see cref="MarsBatchFoldURLPara"/>。
        /// 期望形式（args[0] 已是 userName，arrParameters 为去掉 userName 与 -BatchFold 之后的剩余参数）：
        /// <code>
        /// -FoldList "(1,323,432,5)" -Continue True -CurrentDB MARS2 -CallBackServer localhost:5000 -Uuid 7aae010e-a6f3-45e2-baec-664ecae8a5a2
        /// </code>
        /// </summary>
        private static MarsBatchFoldURLPara BuildBatchFoldInfoFromArgs(string userName, string[] arrParameters,
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            isOk = false;
            strAdv = "Please start BatchFold from MARS GUI or pass valid -BatchFold parameters";
            strStack = Environment.StackTrace;
            try
            {
                var info = new MarsBatchFoldURLPara
                {
                    userName = userName,
                    command = cnst_uri_command_batchfold,
                    foldListRaw = GetParaValueByIdx(arrParameters, "-FoldList", null),
                    uuid = GetParaValueByIdx(arrParameters, "-Uuid", null)
                        ?? GetParaValueByIdx(arrParameters, "-uuid", null),
                    currentDB = GetParaValueByIdx(arrParameters, "-CurrentDB", null)
                        ?? GetParaValueByIdx(arrParameters, "-currentDB", null),
                    callBackServer = GetParaValueByIdx(arrParameters, "-CallBackServer", null)
                        ?? GetParaValueByIdx(arrParameters, "-callBackServer", null)
                };

                string strContinue = GetParaValueByIdx(arrParameters, "-Continue", null);
                if (string.IsNullOrWhiteSpace(strContinue) || !bool.TryParse(strContinue.Trim(), out bool cont))
                {
                    strError = $"Parameter [-Continue] missing or invalid:[{strContinue}]";
                    return null;
                }
                info.isContinue = cont;

                if (!MarsBatchFoldURLPara.TryParseFoldList(info.foldListRaw, out List<long> ids, out string foldErr))
                {
                    strError = foldErr;
                    strAdv = "FoldList format should be like (1,323,432,5)";
                    return null;
                }
                info.foldListIds = ids;

                if (!info.Validate(ref strError, ref strAdv, ref strStack))
                    return null;

                info.isOk = true;
                isOk = true;
                return info;
            }
            catch (Exception e)
            {
                strError = $"Error building BatchFold parameters from args: {e.Message}";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                return null;
            }
        }

        /// <summary>
        /// 解析 BatchFold / EngineSvc 的 API base URL。
        /// 优先使用 ClickOnce URL 中的 callBackServer；否则回退到配置项 MarsEngineSvc_url（restClient.webURLPreFix）。
        /// 返回值保证以 http(s):// 开头并以 / 结尾（若可解析）。
        /// </summary>
        private static string ResolveBatchFoldApiBaseUrl(MarsBatchFoldURLPara batchFoldInfo, MarsRESTfulApiClient restClient,
            out string resolveSource)
        {
            resolveSource = null;
            string fromCallBack = batchFoldInfo?.callBackServer?.Trim();
            if (!string.IsNullOrWhiteSpace(fromCallBack))
            {
                if (!fromCallBack.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    && !fromCallBack.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    fromCallBack = "http://" + fromCallBack;
                }
                if (!fromCallBack.EndsWith("/"))
                    fromCallBack += "/";
                resolveSource = "callBackServer";
                return fromCallBack;
            }

            string fromConfig = restClient?.webURLPreFix?.Trim();
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                if (!fromConfig.EndsWith("/"))
                    fromConfig += "/";
                resolveSource = "MarsEngineSvc_url/config";
                return fromConfig;
            }

            resolveSource = "none";
            return null;
        }

        /// <summary>
        /// BatchFold（批量折叠）执行框架入口。
        /// <para>
        /// 由 Main 在解析到下列 URL 后调用：
        /// <c>/MARSENGINE/Mars.AutoTestingDriver.application?userName=[UserName]&amp;uuid=[Uuid]&amp;command=-BatchFold&amp;FoldList=(1,323,432,5)&amp;Continue=[True|False]&amp;currentDB=[Db]&amp;callBackServer=[Host]</c>
        /// </para>
        /// <para>
        /// 流程：
        /// 1. 获得 EngineSvc API base URL（callBackServer 优先）
        /// 2. 调用 <c>GET /MarsEngine/StoryboardListByFoldList?foldList=...&amp;currentDBIdx=...</c>（调用前必须写 log）
        /// 3. 按返回的 storyboardList（保持 FoldList 顺序）循环调用 <see cref="RunFold"/>(fromBatch=true)
        /// 4. 任一 Fold storyboard 的 test step 失败：中断该 Fold SB，直接跳到下一项（不中止整个 batch）
        /// </para>
        /// </summary>
        /// <param name="batchFoldInfo">已校验的 BatchFold 参数</param>
        /// <param name="strError">失败错误</param>
        /// <param name="strAdv">失败建议</param>
        /// <param name="strStack">失败堆栈</param>
        /// <returns>进程退出码</returns>
        private static int RunBatchFold(MarsBatchFoldURLPara batchFoldInfo,
            ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next();
            LogDispatchBranch("RunBatchFold", $"mark={iMark}|begin|{batchFoldInfo}");
            Logger.logBegin($"{iMark}|RunBatchFold");
            Console.WriteLine($"RunBatchFold begin.... {batchFoldInfo}");
            try
            {
                if (batchFoldInfo == null || !batchFoldInfo.isOk)
                {
                    strError = "BatchFold info is null or invalid";
                    strAdv = "Please start BatchFold from MARS GUI";
                    strStack = Environment.StackTrace;
                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                    return MarsDriverConst.exit_code_error_paraFormat;
                }

                // --------------------------------------------------------------------------------
                // 步骤 1：获得 API base URL（优先 callBackServer）
                //   callBackServer 样例：localhost:5000（URL 中可能是 localhost%3A5000）
                //   EngineSvc base path 约定：{base}/MarsEngine/...
                // --------------------------------------------------------------------------------
                MarsRESTfulApiClient restClient = new MarsRESTfulApiClient(batchFoldInfo.currentDB);
                string apiBaseUrl = ResolveBatchFoldApiBaseUrl(batchFoldInfo, restClient, out string resolveSource);
                LogDispatchBranch("RunBatchFold.ResolveApiBaseUrl",
                    $"source={resolveSource}, apiBaseUrl=[{apiBaseUrl}], configPrefix=[{restClient.webURLPreFix}]");
                Logger.Info("RunBatchFold",
                    $"ResolveApiBaseUrl|source={resolveSource}|apiBaseUrl=[{apiBaseUrl}]|config=[{restClient.webURLPreFix}]");

                if (string.IsNullOrWhiteSpace(apiBaseUrl))
                {
                    strError = "Cannot resolve EngineSvc API base url (callBackServer and MarsEngineSvc_url are empty)";
                    strAdv = "Pass callBackServer in ClickOnce URL or configure MarsEngineSvc_url";
                    strStack = Environment.StackTrace;
                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                    return MarsDriverConst.exit_code_error_paraFormat;
                }

                // 将本会话 API 基址写回 REST 客户端，后续 BuildURL 使用该 host
                restClient.webURLPreFix = apiBaseUrl;
                MarsRESTfulApiClient.setWebURLPrefix(apiBaseUrl);

                MarsGlobarVar.MARS_WEB_HOST = $"{apiBaseUrl}Storyboard/UpdateStoryboardStatus?sessionId={batchFoldInfo.uuid}";
                MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK = $"{apiBaseUrl}StoryBoard/UpdateStoryboardExecuteStatus?";
                Console.WriteLine($"BatchFold apiBase=[{apiBaseUrl}] uuid=[{batchFoldInfo.uuid}] db=[{batchFoldInfo.currentDB}]");
                Console.WriteLine($"BatchFold Links|{MarsGlobarVar.MARS_WEB_HOST}|\r\n{MarsGlobarVar.MARS_WEB_STORYSTATUS_CALLBACK}");

                // --------------------------------------------------------------------------------
                // 步骤 2：用 currentDB + FoldList 调用 StoryboardListByFoldList，获得待处理 storyboard id
                //   GET /MarsEngine/StoryboardListByFoldList?foldList=(...)&currentDBIdx=[Db]
                //   文档：enginesvc-api.md § StoryboardListByFoldList
                //   要求：调用前必须写 log（此处 + ApiClient 内 BEFORE_HTTP_GET）
                // --------------------------------------------------------------------------------
                string foldListForApi = string.IsNullOrWhiteSpace(batchFoldInfo.foldListRaw)
                    ? $"({string.Join(",", batchFoldInfo.foldListIds)})"
                    : batchFoldInfo.foldListRaw;

                LogDispatchBranch("RunBatchFold.StoryboardListByFoldList",
                    $"BEFORE_CALL foldList=[{foldListForApi}] currentDBIdx=[{batchFoldInfo.currentDB}] apiBase=[{apiBaseUrl}]");
                Logger.Info("RunBatchFold",
                    $"BEFORE_CALL StoryboardListByFoldList|foldList=[{foldListForApi}]|currentDBIdx=[{batchFoldInfo.currentDB}]|apiBase=[{apiBaseUrl}]");
                Console.WriteLine(
                    $"[RunBatchFold] BEFORE_CALL StoryboardListByFoldList foldList=[{foldListForApi}] currentDBIdx=[{batchFoldInfo.currentDB}] apiBase=[{apiBaseUrl}]");

                bool apiOk = false;
                RESTfulFoldStoryboardList foldSbResult = restClient.GetStoryboardListByFoldList(
                    foldListForApi, batchFoldInfo.currentDB, ref apiOk, ref strError);

                if (!apiOk || foldSbResult == null || foldSbResult.storyboardList == null)
                {
                    LogDispatchBranch("RunBatchFold.StoryboardListByFoldList.Failed", strError);
                    strAdv = "Check EngineSvc is running and callBackServer / currentDB / FoldList are correct";
                    strStack = Environment.StackTrace;
                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(
                        string.IsNullOrEmpty(strError) ? "StoryboardListByFoldList failed" : strError,
                        "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                    return MarsDriverConst.exit_code_ExeBatchFold_Failed;
                }

                LogDispatchBranch("RunBatchFold.StoryboardListByFoldList.OK",
                    $"count={foldSbResult.storyboardList.Count}, objectType={foldSbResult.objectType}");
                Logger.Info("RunBatchFold",
                    $"AFTER_CALL StoryboardListByFoldList OK|count={foldSbResult.storyboardList.Count}|msg=[{foldSbResult.ReturnedMessage}]");
                foreach (var itm in foldSbResult.storyboardList)
                {
                    if (itm == null) continue;
                    Logger.Info("RunBatchFold",
                        $"foldMap|order={itm.order}|foldId={itm.foldId}|FoldStoryboardId={itm.FoldStoryboardId}");
                    Console.WriteLine($"\tfold order={itm.order} foldId={itm.foldId} -> storyboardId={itm.FoldStoryboardId}");
                }

                // --------------------------------------------------------------------------------
                // 步骤 3：按 API 返回顺序循环调用 Fold-storyboard（RunFold fromBatch=true）
                //   - 任一 test step 失败：RunFold 中断该 Fold SB，本循环跳到下一项
                //   - 日志标明 origin=BatchFold / fromBatch
                // --------------------------------------------------------------------------------
                string defaultAppId;
                string defaultMode;
                if (!TryResolveBatchFoldDefaultAppAndMode(restClient, batchFoldInfo.currentDB,
                        out defaultAppId, out defaultMode, ref strError, ref strAdv, ref strStack))
                {
                    LogDispatchBranch("RunBatchFold.ResolveDefaultApp.Failed", strError);
                    MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(
                        string.IsNullOrEmpty(strError) ? "Cannot resolve application for BatchFold" : strError,
                        "N/A", "N/A", "N/A",
                        string.IsNullOrEmpty(strAdv) ? "Register at least one application in MARS" : strAdv,
                        "N/A", strStack ?? Environment.StackTrace, true);
                    return MarsDriverConst.exit_code_ExeBatchFold_Failed;
                }
                Logger.Info("RunBatchFold",
                    $"defaultAppId|{defaultAppId}|defaultMode|{defaultMode}|will loop RunFold(fromBatch=true)");

                int successCount = 0;
                int failCount = 0;
                List<long> failedFoldIds = new List<long>();

                for (int i = 0; i < foldSbResult.storyboardList.Count; i++)
                {
                    FoldStoryboardListItem mapItem = foldSbResult.storyboardList[i];
                    if (mapItem == null)
                    {
                        LogDispatchBranch("RunBatchFold.Item.Null", $"index={i}");
                        continue;
                    }

                    long foldId = mapItem.foldId;
                    long storyboardId = mapItem.FoldStoryboardId;
                    Console.WriteLine(
                        $"BatchFold [{i + 1}/{foldSbResult.storyboardList.Count}] foldId={foldId} storyboardId={storyboardId} -> RunFold(fromBatch) ...");
                    Logger.Info("RunBatchFold",
                        $"begin item fromBatch| {i + 1}/{foldSbResult.storyboardList.Count}|foldId|{foldId}|storyboardId|{storyboardId}");

                    bool itemOk = false;
                    string itemError = null;
                    try
                    {
                        if (storyboardId <= 0)
                        {
                            itemOk = false;
                            itemError = $"No storyboard linked to foldId={foldId} (FoldStoryboardId=0)";
                            LogDispatchBranch("RunBatchFold.Item.NoStoryboard", itemError);
                        }
                        else
                        {
                            itemOk = ExecuteBatchFoldItemViaRunFold(
                                batchFoldInfo, foldId, storyboardId, defaultAppId, defaultMode,
                                ref itemError, ref strAdv, ref strStack);
                        }
                    }
                    catch (Exception itemEx)
                    {
                        itemOk = false;
                        itemError = itemEx.Message;
                        strStack = $"{itemEx.Message}\r\n{itemEx.StackTrace}";
                        Logger.Error("RunBatchFold", $"item exception|foldId|{foldId}|sb|{storyboardId}|{itemEx.Message}", itemEx);
                    }

                    if (itemOk)
                    {
                        successCount++;
                        Console.WriteLine($"BatchFold foldId={foldId} storyboardId={storyboardId} SUCCEEDED.");
                        Logger.Info("RunBatchFold",
                            $"item SUCCEEDED fromBatch|foldId|{foldId}|storyboardId|{storyboardId}");
                    }
                    else
                    {
                        failCount++;
                        failedFoldIds.Add(foldId);
                        Console.WriteLine($"BatchFold foldId={foldId} storyboardId={storyboardId} FAILED (abort this Fold SB, continue next): {itemError}");
                        Logger.Error("RunBatchFold",
                            $"item FAILED fromBatch|abort this Fold storyboard|continue next|foldId|{foldId}|sb|{storyboardId}|{itemError}");
                        // 按需求：任一 Fold SB 失败后不中断 batch，直接跳到下一个 Fold storyboard
                    }
                }

                // --------------------------------------------------------------------------------
                // 步骤 4：汇总结果
                // --------------------------------------------------------------------------------
                Console.WriteLine($"BatchFold finished. success={successCount}, failed={failCount}, failedFoldIds=[{string.Join(",", failedFoldIds)}]");
                Logger.Info("RunBatchFold", $"finished|success|{successCount}|failed|{failCount}|failedFoldIds|{string.Join(",", failedFoldIds)}");
                LogDispatchBranch("RunBatchFold.Finished", $"success={successCount}, failed={failCount}");

                if (failCount > 0)
                {
                    strError = $"BatchFold completed with failures. success={successCount}, failed={failCount}, foldIds=[{string.Join(",", failedFoldIds)}]";
                    strAdv = "Check failed fold items in MARS GUI";
                    strStack = Environment.StackTrace;
                    return MarsDriverConst.exit_code_ExeBatchFold_Failed;
                }

                return MarsDriverConst.exit_code_ExeBatchFold_Ok;
            }
            catch (Exception e)
            {
                strError = $"Exception in RunBatchFold: {e.Message}";
                strAdv = "Contact Marquis";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                Logger.Error("RunBatchFold", e.Message, e);
                MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", strAdv, "N/A", strStack, true);
                return MarsDriverConst.exit_code_ExeBatchFold_Failed;
            }
            finally
            {
                Logger.logEnd($"{iMark}|RunBatchFold");
            }
        }

        /// <summary>
        /// BatchFold 默认应用 / Mode：Batch URL 不含 app/Mode，取引擎已注册应用列表首项；Mode 固定 Base（与 Fold UI 默认一致）。
        /// </summary>
        private static bool TryResolveBatchFoldDefaultAppAndMode(
            MarsRESTfulApiClient restClient, string currentDB,
            out string appId, out string mode,
            ref string strError, ref string strAdv, ref string strStack)
        {
            appId = null;
            mode = "Base";
            strAdv = "Register at least one application in MARS, then retry BatchFold";
            strStack = Environment.StackTrace;
            try
            {
                string navErr = null, navStack = null;
                bool navOk = false;
                RESTfullReturnApplicationObjects apps = restClient.navigateApplications(currentDB, ref navOk, ref navErr, ref navStack);
                if (!navOk || apps == null || apps.AssignedObjects == null)
                {
                    strError = string.IsNullOrEmpty(navErr)
                        ? "navigateApplications returned no application list for BatchFold"
                        : navErr;
                    strStack = string.IsNullOrEmpty(navStack) ? Environment.StackTrace : navStack;
                    return false;
                }

                B_REGISTERED_APPS firstApp = apps.AssignedObjects.FirstOrDefault(a => a != null && a.APPLICATION_ID > 0);
                if (firstApp == null)
                {
                    strError = "No valid APPLICATION_ID in navigateApplications result for BatchFold";
                    return false;
                }

                appId = firstApp.APPLICATION_ID.ToString();
                mode = "Base";
                Logger.Info("TryResolveBatchFoldDefaultAppAndMode",
                    $"appId|{appId}|appShort|{firstApp.APP_SHORT_NAME}|mode|{mode}");
                return true;
            }
            catch (Exception e)
            {
                strError = $"Failed to resolve default application for BatchFold: {e.Message}";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                Logger.Error("TryResolveBatchFoldDefaultAppAndMode", strError, e);
                return false;
            }
        }

        /// <summary>
        /// 解析单个 Fold storyboard 的名称，组装 <see cref="MarsFoldURLPara"/>，并循环调用 <see cref="RunFold"/>（fromBatch=true）。
        /// </summary>
        private static bool ExecuteBatchFoldItemViaRunFold(
            MarsBatchFoldURLPara batchFoldInfo, long foldId, long storyboardId,
            string defaultAppId, string defaultMode,
            ref string strError, ref string strAdv, ref string strStack)
        {
            Logger.logBegin("ExecuteBatchFoldItemViaRunFold",
                $"fromBatch|foldId|{foldId}|storyboardId|{storyboardId}|app|{defaultAppId}|mode|{defaultMode}");
            try
            {
                if (batchFoldInfo == null || !batchFoldInfo.isOk)
                {
                    strError = "BatchFold info is null or invalid";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(defaultAppId))
                {
                    strError = "BatchFold default application id is empty";
                    return false;
                }

                string sbName;
                if (!TryResolveFoldStoryboardName(storyboardId, defaultMode, batchFoldInfo.currentDB,
                        out sbName, ref strError, ref strAdv, ref strStack))
                {
                    Logger.Error("ExecuteBatchFoldItemViaRunFold",
                        $"fromBatch|resolve name failed|foldId|{foldId}|sb|{storyboardId}|{strError}");
                    return false;
                }

                // 组装与单条 Fold 同形的参数；Continue/IgnoreError 在 RunFold(fromBatch) 内强制为 False
                var foldInfo = new MarsFoldURLPara
                {
                    userName = batchFoldInfo.userName,
                    uuid = batchFoldInfo.uuid,
                    command = cnst_uri_command_fold,
                    storyBoardName = sbName,
                    storyBoardId = storyboardId.ToString(),
                    appId = defaultAppId,
                    mode = string.IsNullOrWhiteSpace(defaultMode) ? "Base" : defaultMode,
                    isContinue = false,
                    ignoreError = false,
                    currentDB = batchFoldInfo.currentDB,
                    callBackServer = batchFoldInfo.callBackServer,
                    isOk = true
                };

                Logger.Info("ExecuteBatchFoldItemViaRunFold",
                    $"fromBatch|calling RunFold|foldId|{foldId}|{foldInfo}");
                Console.WriteLine(
                    $"\t[BatchFold->RunFold] foldId={foldId} sbId={storyboardId} sbName=[{sbName}] app={defaultAppId} mode={foldInfo.mode}");

                bool isExitItself = false;
                int exitCode = RunFold(foldInfo, ref isExitItself, fromBatch: true);
                if (exitCode == MarsDriverConst.exit_code_ExeFold_Ok)
                {
                    strError = null;
                    return true;
                }

                strError = string.IsNullOrEmpty(strError)
                    ? $"RunFold(fromBatch) failed for foldId={foldId}, storyboardId={storyboardId}, sbName=[{sbName}], exitCode={exitCode}"
                    : strError;
                Logger.Error("ExecuteBatchFoldItemViaRunFold",
                    $"fromBatch|RunFold failed|foldId|{foldId}|sb|{storyboardId}|exit|{exitCode}|{strError}");
                return false;
            }
            catch (Exception e)
            {
                strError = $"Exception executing BatchFold item via RunFold: {e.Message}";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                Logger.Error("ExecuteBatchFoldItemViaRunFold", strError, e);
                return false;
            }
            finally
            {
                Logger.logEnd("ExecuteBatchFoldItemViaRunFold");
            }
        }

        /// <summary>
        /// 通过 storyboard detail 列表解析 STORYBOARD_NAME（BatchFold API 只返回 id）。
        /// </summary>
        private static bool TryResolveFoldStoryboardName(
            long storyboardId, string mode, string currentDB,
            out string storyboardName,
            ref string strError, ref string strAdv, ref string strStack)
        {
            storyboardName = null;
            strAdv = "Ensure the Fold storyboard exists and has test cases";
            strStack = Environment.StackTrace;
            try
            {
                bool isBase = string.Compare(mode, "base", StringComparison.OrdinalIgnoreCase) == 0;
                string loadErr = null;
                List<ExecutableTestStoryBoard> lst =
                    ExecutableTestStoryBoard.GetStoryboarddetailListByStoryboardId(
                        storyboardId, isBase, ref loadErr, currentDB);

                if (lst == null || lst.Count == 0)
                {
                    strError = string.IsNullOrEmpty(loadErr)
                        ? $"Fold storyboard id={storyboardId} has no test cases / cannot resolve name"
                        : loadErr;
                    return false;
                }

                var first = lst[0]?.AssignedTestObjectFromDB;
                storyboardName = first?.STORYBOARD_NAME;
                if (string.IsNullOrWhiteSpace(storyboardName))
                {
                    strError = $"Fold storyboard id={storyboardId} returned empty STORYBOARD_NAME";
                    return false;
                }

                Logger.Info("TryResolveFoldStoryboardName",
                    $"fromBatch|sbId|{storyboardId}|sbName|{storyboardName}");
                return true;
            }
            catch (Exception e)
            {
                strError = $"Failed to resolve Fold storyboard name for id={storyboardId}: {e.Message}";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                Logger.Error("TryResolveFoldStoryboardName", strError, e);
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