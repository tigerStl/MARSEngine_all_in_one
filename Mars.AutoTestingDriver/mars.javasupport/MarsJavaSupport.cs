using com.Mars.Constants;
using Mars.message.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.AutoTestingDriver.mars.javasupport.data;
using Mars.AutoTestingDriver.Properties;
using NLog;
using NLog.Time;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp;

namespace Mars.AutoTestingDriver.mars.javasupport
{
    internal class MarsJavaSupport
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsJavaSupport));

        public const string CNST_JAVA_ENGINE_JAR = "MARSJaveEngineMainWithLibs.jar";
        public MarsJavaSupport() { }

        public static bool IsJavaSupport(ref string strJavaVer)
        {
            strJavaVer = GetJavaVersion();
            return !string.IsNullOrEmpty(strJavaVer);
        }

        public static string GetJavaStartCommand(ref string strError, ref bool isOk)
        {
            string currentDir = typeof(MarsJavaSupport).Assembly.Location;
            currentDir = System.IO.Path.GetDirectoryName(currentDir);
            string pathToJarFile = System.IO.Path.Combine(currentDir, "javaEngine", CNST_JAVA_ENGINE_JAR);
            if (!System.IO.File.Exists(pathToJarFile))
            {
                isOk = false;
                strError = string.Format(Resources.mars_keyword_no_java_engine_jar, pathToJarFile);
                Logger.Error("GetJavaStartCommand", strError);
                return null;
            }

            isOk = true;
            return pathToJarFile;
        }

        public static string GetJavaVersion()
        {
            try
            {
                // Create a process to run the 'java -version' command
                Process process = new Process();
                process.StartInfo.FileName = "java";
                process.StartInfo.Arguments = "-version";
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                // Start the process
                process.Start();

                // Capture the standard error and standard output
                string errorOutput = process.StandardError.ReadToEnd();
                string standardOutput = process.StandardOutput.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();

                // Check if there was an error running the command
                if (!string.IsNullOrWhiteSpace(errorOutput))
                {
                    Console.WriteLine($"Error running 'java -version': {errorOutput}");
                    return null;
                }

                // Parse the version from the standard output
                string versionPattern = @"java version ""(.+?)""";
                Match match = Regex.Match(standardOutput, versionPattern);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                else
                {
                    Console.WriteLine("Java version information not found in the output.");
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
                return null;
            }
        }

        public static bool IsJavaVersionCritical(string javaVersionReq, ref string version)
        {            
            Match match = Regex.Match(javaVersionReq, SystemConstant.CNST_APPOCNFIG_APPREG_ATTR_EXTRA_JAVA_X + "\\d+");
            if (match.Success)
            {
                version=match.Groups[1].Value;
                return true;
            }
            else
            {
                Console.WriteLine("Java version information not found in the output.");
                version = null;
                return false;
            }
        }

    }

    public class MarsWebsocketStatus
    {
        public bool isFirstConnect { get; set; } = true;
        public bool isWaitForMessageBack { get; set; } = false ;
    }

    public class MarsJavaWebSocketClient
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsJavaWebSocketClient));
        //private ClientWebSocket clientWebSocket = null;
        private WebSocketSharp.WebSocket clientWS = null;
        private static MarsJavaWebSocketClient currentInstance = null;

        internal static MarsWebsocketStatus currentSocketStatus = null;

        public const int default_port = 8062;

        private string currentWebSocketURL = null;
        private MarsJavaWebSocketClient()
        {

        }

        public static MarsJavaWebSocketClient GetJavaWebSocketClient(string currentURL="ws://localhost:8062" )
        {
            if (currentInstance==null)
                currentInstance = new MarsJavaWebSocketClient();
            currentInstance.currentWebSocketURL = currentURL;

            return currentInstance;
        }

        public bool ReconnectToJavaJvmServer(ref string strError)
        {
            Logger.logBegin("ReconnectToJavaJvmServer");
            if (clientWS != null)
            {
                clientWS.Close();
            }
            //clientWS = new WebSocketSharp.WebSocket();
            //clientWebSocket = new ClientWebSocket();            
            bool isOk = connectToJavaJvmServer(ref strError);
            return isOk;
        }
        private bool connectToJavaJvmServer(ref string strError, string strUrl = "ws://localhost:8062")
        {
            Logger.logBegin("connectToJavaJvmServer", $"try to connect to|{strUrl}");
            try
            {
                Uri serverUri = new Uri(strUrl);
                this.clientWS = new WebSocketSharp.WebSocket(this.currentWebSocketURL);
                currentSocketStatus = new MarsWebsocketStatus();
                currentSocketStatus.isFirstConnect = true;
                
                this.clientWS.OnOpen += ClientWS_OnOpen;
                this.clientWS.OnMessage += ClientWS_OnMessage;
                this.clientWS.OnError += ClientWS_OnError;

                this.clientWS.Connect();

                Thread.Sleep(200);
                if (this.clientWS.ReadyState != WebSocketSharp.WebSocketState.Open)
                {
                    Logger.Error("connectToJavaJvmServer", $"Can't connect to |{strUrl}|");
                    strError = Resources.mars_java_engine_connect_ws_failed;
                    return false;
                }
                //byte[] buffer = new byte[1024];
                //bool isOk = true;
                //string strDataFromJavaEngine = ReadDataFromWebSocket(ref isOk, ref strError);
                //if (!isOk) return false;
                //Logger.Info("connectToJavaJvmServer", $"received data after connect|{strDataFromJavaEngine}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("connectToJavaJvmServer", $"{e.Message}", e);
                strError = Resources.mars_java_engine_connect_ws;
                return false;
            }
        }

        private void ClientWS_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Logger.Error("ClientWS_OnError", e.Message, e.Exception);
        }
        private Queue<MarsJavaDataPackageBase> marsJavaNetPackets = new Queue<MarsJavaDataPackageBase>();

        private void ClientWS_OnMessage(object sender, WebSocketSharp.MessageEventArgs e)
        {
            Logger.logBegin("ClientWS_OnMessage");
            bool isOk = false;
            string strError = "";
            if (e.IsText)
            {
                Logger.Info("ClientWS_OnMessage", $"received data|{e.Data}");
                /// try to tell data package 
                /// 
                MarsJavaDataMgr javaDataMgr = new MarsJavaDataMgr();
                var baseData = javaDataMgr.checkResponseData(e.Data, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("ClientWS_OnMessage", $"received|{e.Data}|\r\n{strError}");
                    return;
                }
                // put data to queue
                Logger.Info("ClientWS_OnMessage", $"get message from ws server|{baseData.packageType}|");
                this.marsJavaNetPackets.Enqueue(baseData);
            }
            if (currentSocketStatus.isFirstConnect)
            {
                //do nothing
                currentSocketStatus.isFirstConnect = false;
            }
            else
            {
                //if (currentSocketStatus.isWaitForMessageBack)
                //{
                //    try
                //    {

                //    }
                //    finally
                //    {
                //        currentSocketStatus.isWaitForMessageBack
                //    }
                //}
            }
        }

        private void ClientWS_OnOpen(object sender, EventArgs e)
        {
            Logger.logBegin("ClientWS_OnOpen");
            Logger.logEnd("ClientWS_OnOpen");
        }

        //private string ReadDataFromWebSocket(ref bool isOk, ref string strError)
        //{
        //    Logger.logBegin("ReadDataFromWebSocket");
        //    byte[] buffer = new byte[1024];
        //    StringBuilder sb = new StringBuilder();
        //    int iLoopCntrol =0 ;
        //    isOk = true;
        //    try
        //    {
        //        while (true)
        //        {
        //            iLoopCntrol++;
        //            this.clientWS.
        //            WebSocketReceiveResult result = clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
        //                .GetAwaiter()
        //                .GetResult();
        //            if (result.MessageType == WebSocketMessageType.Close)
        //            {
        //                Logger.Info("ReadDataFromWebSocket", $"Java has finished sent data, packet len|{result.Count}|");
        //                break;
        //            }else if (result.MessageType == WebSocketMessageType.Text)
        //            {
        //                Logger.Info("ReadDataFromWebSocket", $"get data from websocket server, packet len|{result.Count}|");
        //                string receivedMsg = Encoding.UTF8.GetString(buffer, 0, result.Count);
        //                sb.Append(receivedMsg);
        //            }
        //            if (iLoopCntrol >= 500)
        //            {
        //                Logger.Error("ReadDataFromWebSocket", $"has looped |{iLoopCntrol}|, should stop receiving data");
        //                strError = Resources.mars_java_engine_recv_loop_outof_control;
        //                isOk = false;
        //                break;
        //            }
        //        }
        //        if (!isOk) return null;

        //        return sb.ToString();
        //    }
        //    catch(Exception e)
        //    {
        //        isOk = false;
        //        strError = String.Format(Resources.mars_java_engine_recv_exception, e.GetType());
        //        return null;
        //    }
        //}

        private bool IsRightResponseForCurrentTeststep(MarsJavaCommuniteTestStep srcRequest,
            MarsJavaCommunitTestStepRspns stepFromResponse, 
            ref string strError)
        {
            Logger.logBegin("IsRightResponseForCurrentTeststep");
            if (stepFromResponse==null)
            {
                strError = Resources.make_sure_process_is_running_with_name;
                return false;
            }
            if ((string.IsNullOrEmpty(stepFromResponse.packageType)) || (!stepFromResponse.packageType.ToLower().EndsWith("_response")))
            {
                strError = Resources.mars_java_engine_package_no_response;
                return false;
            }
            /// compare the UUID
            /// 
            if (string.Compare(srcRequest.uuid, stepFromResponse.uuid) != 0)
            {
                strError = Resources.mars_java_engine_return_wrong_uuid;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Send command to websocket server and wait for response
        /// </summary>
        /// <param name="stepJson"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal MarsJavaCommunitTestStepRspns sendRequestAndGetReponse(MarsJavaCommuniteTestStep srcRequest,string stepJson, ref bool isOk, ref string strError)
        {
            Logger.logBegin("sendRequestAndGetReponse", stepJson);
            try
            {
                //clientWebSocket.SendAsync(new ArraySegment<byte>());
                if (this.clientWS == null) { 
                    isOk = this.ReconnectToJavaJvmServer(ref strError);
                    if (!isOk)
                    {
                        Logger.Error("sendRequestAndGetReponse",strError); 
                        return null;
                    }
                }
                if (this.clientWS.ReadyState== WebSocketSharp.WebSocketState.Closed)
                {
                    connectToJavaJvmServer(ref strError);
                }
                currentSocketStatus.isWaitForMessageBack = true;

                // clean quene
                this.marsJavaNetPackets.Clear();
                this.clientWS.Send(stepJson);
                KeyWordHelper keyHlp = new KeyWordHelper();
                keyHlp.WaitUntilTimeOut(60, () =>
                {   
                    if (this.marsJavaNetPackets.Count > 0)
                    {
                        var topNode = this.marsJavaNetPackets.Peek();
                        if (String.Compare(topNode.packageType, Mars.AutoTestingDriver.mars.javasupport.data.MarsJavaDataPackageTypeConst.PackageType_HeartBeat, true) == 0)
                        {
                            this.marsJavaNetPackets.Dequeue();
                            // get hearbeat package, just ignore
                            Logger.Info("sendRequestAndGetReponse", "get heartbeat message, just ignore");
                            return false;
                        }
                    }
                    return this.marsJavaNetPackets.Count > 0;
                }, (msg) =>
                {
                    Logger.Info("sendRequestAndGetReponse",msg);
                });
                Logger.Info("sendRequestAndGetReponse", $"WaitUntilTimeOut is done, quene count is|{this.marsJavaNetPackets.Count}|");
                if (this.marsJavaNetPackets.Count <= 0)
                {
                    strError = Resources.mars_java_engine_recv_time_out;
                    Logger.Error("sendRequestAndGetReponse", strError);
                    isOk = false;
                    return null;
                }
                MarsJavaDataPackageBase currentPackage = this.marsJavaNetPackets.Dequeue();
                if (currentPackage == null)
                {

                }

                MarsJavaCommunitTestStepRspns currentRspns = (MarsJavaCommunitTestStepRspns)currentPackage;
                // check and compare data type
                isOk = IsRightResponseForCurrentTeststep(srcRequest, currentRspns, ref strError);
                if (!isOk)
                {
                    Logger.Error("sendRequestAndGetReponse", strError);
                    isOk = false;
                    return null;
                }
                isOk = true;
                return currentRspns;
            }
            catch (Exception e)
            {
                Logger.Error("sendRequestAndGetReponse", e.Message, e);
                isOk= false;
                strError = Resources.mars_websocket_exception_when_sendcmd;
                return null;
            }
            finally
            {
                Logger.logEnd("sendRequestAndGetReponse");
            }
        }
    }
}
