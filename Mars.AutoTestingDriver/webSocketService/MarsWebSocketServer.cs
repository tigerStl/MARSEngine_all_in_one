using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Route2NSEx.src.Marquis.systemUtil;
using System.Text.Json;
using NLog;
using System.CodeDom;
using Mars.AutoTestingDriver.ExecuteStoryboard;

namespace Mars.AutoTestingDriver.webSocketService
{

    public class MarsWebSocketDataPacketManagement
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MarsWebSocketDataPacketManagement));
        public static MarsWebSocketNetPacketBase convertMessageToJson(string strMsg, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next();
            logger.logBegin("convertMessageToJson",$"{iMark}|{strMsg}|" );
            try
            {
                var rsltObj = JsonSerializer.Deserialize<MarsWebSocketNetPacketBase>(strMsg);
                if (rsltObj != null)
                {
                    isOk = true;
                    return rsltObj;
                }
                strError = "Message type is wrong";
                logger.Error("MarsWebSocketNetPacketBase", $"{iMark}|{strError}");
                isOk = false;
                return null;

            }
            catch (Exception e)
            {
                logger.Error("convertMessageToJson", $"{iMark}|{e.Message}",e);
                isOk = false; 
                strError = e.Message;
                return null;
            }
            finally
            {
                logger.logEnd("convertMessageToJson");
            }
        }
    }


    public class MarsWebSocketServer
    {
        private HttpListener listener;
        private CancellationTokenSource cancellationTokenSource;

        private static MLogger logger = MLogger.GetLogger(typeof(MarsWebSocketServer));
        private static bool IscontinueToListen = true;

        public async Task Start(string url)
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine("WebSocket server started.");

                //cancellationTokenSource = new CancellationTokenSource();
                //while (!cancellationTokenSource.Token.IsCancellationRequested)
                while (IscontinueToListen)
                {
                    try
                    {
                        Console.WriteLine("going to wait for connection.");
                        var context = await listener.GetContextAsync().ConfigureAwait(false);
                        Console.WriteLine("\t getting connection from "+context.Request.Url.ToString());
                        if (context.Request.IsWebSocketRequest)
                        {
                            await ProcessWebSocketRequest(context);
                            //var wsc = await context.AcceptWebSocketAsync(null);
                            //var ws = wsc.WebSocket;
                            //for (int i = 0; i != 5; ++i)
                            //{
                            //    await Task.Delay(1000);
                            //    var response = "push_event" + DateTime.Now.ToLongDateString(); 
                            //    var buffer = Encoding.UTF8.GetBytes(response);
                            //    var segment = new ArraySegment<byte>(buffer);
                            //    await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                            //}
                            //await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,"Done", CancellationToken.None);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }catch (Exception e)
                    {
                        logger.Error("Start", e.Message, e);
                    }
                }

                listener.Stop();
                Console.WriteLine("WebSocket server stopped.");
            }catch(Exception e)
            {
                
                logger.Error("Start", e.Message, e);
            }
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;

            Console.WriteLine($"WebSocket connection established.{webSocketContext.RequestUri}");

            byte[] buffer = new byte[1024];
            string strError = "DONE";
            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);//.GetAwaiter().GetResult();
                bool isOk = false;
                
                while (!result.CloseStatus.HasValue)
                {
                    string message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine("Received message: " + message);

                    ///
                    var msgJson = MarsWebSocketDataPacketManagement.convertMessageToJson(message, ref isOk, ref strError);
                    if (isOk)
                    {
                        switch (msgJson.command)
                        {
                            case MARSWebSocketConst.cnst_cmd_request_getHostName:
                                 await requestGetHostNameHandler(webSocket);
                                break;
                            case MARSWebSocketConst.cnst_cmd_request_runSteps:
                                await requestRunSteps(webSocket, message);
                                break;
                            default:
                                logger.Info("ProcessWebSocketRequest",$"unknow command|{msgJson.command}|");
                                break;
                        }                        
                    }
                    else
                    {
                        logger.Error($"ProcessWebSocketRequest", strError = $"request package is not in a right format", message);
                        await errorReponseHandler(webSocket, strError);
                        
                        Thread.Sleep(100);
                        
                    }
                    //Console.WriteLine($"\tgoting to wait for ReceiveAsync...... ");
                    //result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }                
                Console.WriteLine("WebSocket connection closed.");
            }catch(Exception e)
            {
                logger.Error("ProcessWebSocketRequest", e.Message, e);
                strError = "exceptions, please check httprequest and log file";
                await errorReponseHandler(webSocket, strError);
            }
            finally
            {
                Console.WriteLine($"\tgoing to close....");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, strError, CancellationToken.None);
            }
        }

        private async Task SendErrorToClientSide(WebSocket ws)
        {
            int iMark = new Random().Next();
            logger.logBegin("SendErrorToClientSide", $"{iMark}");
        }

        private async Task errorReponseHandler(WebSocket webSocket, string errorMessage)
        {
            int iMark = new Random().Next();
            logger.logBegin("requestGetHostNameHandler", $"{iMark}");
            MarsWebSocketResponsePacket error = new MarsWebSocketResponsePacket()
            {
                command = MARSWebSocketConst.cnst_cmd_response_error,
                message = errorMessage
            };
            try
            {
                string strResponse = JsonSerializer.Serialize(error);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(strResponse);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception e)
            {
                logger.Error("requestGetHostNameHandler", e.Message, e);

            }finally
            {
                logger.logEnd("requestGetHostNameHandler");
            }           
            
        }



        private async Task requestRunSteps(WebSocket webSocket, string origData)
        {
            int iMark = new Random().Next();
            logger.logBegin("requestGetHostNameHandler", $"{iMark}");
            bool isOk = false;
            string strError = "";
            try
            {
                MarsExeTestStepsRequestPacket teststps = JsonSerializer.Deserialize<MarsExeTestStepsRequestPacket>(origData);
                if (string.Compare(teststps.command, MARSWebSocketConst.cnst_cmd_request_runSteps, true) != 0)
                {
                    /// send back 
                    /// 
                    teststps.packetType = MARSWebSocketConst.cnst_packet_type_response;                    
                    teststps.message = $"wrong data type|{teststps.command}|but {MARSWebSocketConst.cnst_cmd_request_runSteps} is required";
                    teststps.command = MARSWebSocketConst.cnst_cmd_response_error;
                    teststps.TestStepInfo = null;

                    string strResponse = JsonSerializer.Serialize(teststps);
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(strResponse);

                    await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    return;
                }

                /// try to run steps...
                /// 
                string strStack = "", strAdv = "";
                bool isExitItSelf = false;
                int pid = -1;
                // build list
                if (teststps.TestStepInfo!=null)
                {
                    if (teststps.TestStepInfo.testStepsFromClipboard==null)
                    {
                        teststps.TestStepInfo.testStepsFromClipboard = new List<SystemUtil.MARSStepsFromClipboardJsonFormat>();
                    }
                    teststps.TestStepInfo.testStepsFromClipboard.Clear();
                    teststps.TestStepInfo.testStepsFromClipboard.AddRange(teststps.TestStepInfo.testStepsForJson); 
                }
                AutoTestingDriverEntry.RunTempTestSteps(teststps.TestStepInfo.userName, teststps.TestStepInfo, ref isOk, ref strError, ref strStack, ref strAdv);
                //StoryboardExecute.ExecuteTestStepsFromJSon(teststps.TestStepInfo, pid, ref isOk, ref strError, ref strStack, ref strAdv, ref isExitItSelf);
            }
            catch(Exception e)
            {
                logger.Error($"requestGetHostNameHandler|{iMark}", e.Message, e);
                //send error mssage to client
            }
            finally
            {
                logger.logEnd($"requestGetHostNameHandler|{iMark}|");
            }
        }

        private async Task requestGetHostNameHandler(WebSocket webSocket)
        {
            int iMark = new Random().Next();    
            logger.logBegin("requestGetHostNameHandler", $"{iMark}");
            bool isOk = false;
            string strError = "";
            try
            {
                MarsWebSocketResponsePacket rslt = new MarsWebSocketResponsePacket()
                {
                    command = MARSWebSocketConst.cnst_cmd_response_getHostName ,
                    message = System.Environment.MachineName
                };
                string strResponse = JsonSerializer.Serialize(rslt);
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(strResponse);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                isOk = true;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                MarsWebSocketResponsePacket error = new MarsWebSocketResponsePacket()
                {
                    command = MARSWebSocketConst.cnst_cmd_response_error,
                    message = strError
                };
                logger.Error("requestGetHostNameHandler", $"{e.Message}", e);
            }
            finally
            {
                if (isOk)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "DONE", CancellationToken.None);
                }
                else
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "ERROR", CancellationToken.None);
                }
            }
        }

        private static MarsWebSocketServer u_DefaultSocketServer = null;
        private const string localDefault_url = "http://localhost:8666/marsEngineTest/";
        public static async Task<bool> startDefaultSvc()
        {
            if (u_DefaultSocketServer != null)
                u_DefaultSocketServer.Stop();
            u_DefaultSocketServer = new MarsWebSocketServer();
            await u_DefaultSocketServer.Start(localDefault_url);
            return false;
        }
    }
}
