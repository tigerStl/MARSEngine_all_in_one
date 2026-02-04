using Mars.Inter.MQCenter.objectEngine;
using Mars.message.Inter.MQCenter.HttpRestService;
using Mars.message.Inter.MQCenter.interProcess.HttpRestService.SvcMode;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.interProcess.HttpRestService
{
    public delegate void QueryCurrentObjectsHandler(string request, ref bool isOk, ref string strError);


    /// <summary>
    /// this is a restful http server
    /// start http server, then find config json file from settings, port number is created
    /// then waiting for requests from spy
    /// 1，该类用于record&replay的通信 （暂时没用该方法）
    ///   stub将启动Server，然后mars的spytool将找到该port，链接上去
    /// </summary>
    public class MarsSpyRESTfulServer
    {
        public static HttpListener listener;
        private static SpyInternalPortInfo currentSvcInfo;
        private static bool runServer = true;
        private static string currentRESTfulUri = "";
        /// <summary>
        /// start rest ful service
        /// 1, this is http server
        /// 2, create a temp port file with port info, UUID, then wait for spyer to active
        ///
        /// </summary>
        public static bool StartInternalSpyRestSvc(string strPara="Normal")
        {
            simpleLog.MarsLoggerSimple.logBegin("StartInternalSpyRestSvc");
            int port = FindAvailablePort(20000, 21000);
            string strError = "";
            // write to swap file
            if (!writeToSwapFileForPort(port,ref strError))
            {
                simpleLog.MarsLoggerSimple.Error("StartInternalSpyRestSvc", $"can't write port|{port}|to swap file");
                return false;
            }
            // start services at the port
            currentRESTfulUri = $"{MarsRestFulCnst.cnst_mars_restful_base_url}:{port}/";
            listener = new HttpListener();
            listener.Prefixes.Add(currentRESTfulUri);
            runServer = true;
            try
            {
                listener.Start();
                simpleLog.MarsLoggerSimple.Info("StartInternalSpyRestSvc", "restful svc has been started!!!!!!");
                Task.Run(() => HandleIncomingConnectionsAsync());                
                return true;
            }catch(Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("StartInternalSpyRestSvc", $"can't start restful svc|with exception|{ex.Message}",ex);
                return false;
            }finally {
                simpleLog.MarsLoggerSimple.logEnd("StartInternalSpyRestSvc");
            }
        }
        private static bool writeToSwapFileForPort(int port,ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("writeToSwapFileForPort", $"port|{port}");
            string dir = typeof(MarsSpyRESTfulServer).Assembly.Location;
            dir = System.IO.Path.GetDirectoryName(dir);
            var tmpDir = System.IO.Path.Combine(dir, MarsRestFulCnst.cnst_SwapDir);
            if (!System.IO.Directory.Exists(tmpDir))
            {
                System.IO.Directory.CreateDirectory(tmpDir);
            }
            string swapFileName = System.IO.Path.Combine(dir, MarsRestFulCnst.cnst_SwapDir, MarsRestFulCnst.cnst_port_swapfile);
            if (System.IO.File.Exists(swapFileName))
            {
                try
                {
                    System.IO.File.Delete(swapFileName);
                }catch(Exception ex)
                {
                    simpleLog.MarsLoggerSimple.Error("writeToSwapFileForPort", strError = $"can't delete file|{swapFileName}|with error|{ex.Message}",ex);
                    return false;
                }
            }
            currentSvcInfo = new SpyInternalPortInfo()
            {
                port = port,
                version = "1.0",
                accessGUID = Guid.NewGuid().ToString()
            };
            string strJson = "";
            try
            {
                strJson = System.Text.Json.JsonSerializer.Serialize(currentSvcInfo);
                System.IO.File.WriteAllText(swapFileName, strJson);
                return true;
            }catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("writeToSwapFileForPort", strError = $"can't convert object to json|with error|{e.Message}", e);
                return false;
            }
        }

        static int FindAvailablePort(int startPort, int endPort)
        {
            simpleLog.MarsLoggerSimple.logBegin("FindAvailablePort", $"from port|{startPort}|{endPort}");
            int targetPort = -1;
            try
            {
                for (targetPort = startPort; targetPort <= endPort; targetPort++)
                {
                    if (IsPortAvailable(targetPort))
                    {
                        return targetPort;
                    }
                }
                return targetPort  = - 1;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("FindAvailablePort", $"availablePort is|{targetPort}");
            }
        }

        static bool IsPortAvailable(int port)
        {
            simpleLog.MarsLoggerSimple.logBegin("IsPortAvailable", $"check port|{port}");
            try
            {
                // Try to create a TCP listener on the specified port
                var listener = new TcpListener(IPAddress.Loopback, port) ;
                listener.Start();
                listener.Stop();
                return true;                
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("IsPortAvailable", $"the port|{port}|is used|not available");
                // Port is not available
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("IsPortAvailable");
            }
        }

        private static bool WriteBack2Response(HttpListenerResponse resp, string jsonToWrite,ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("WriteBack2Response", string.IsNullOrEmpty(jsonToWrite)?"NOTHING TO WRITE":$"{jsonToWrite.Length}|of string");
            try
            {
                resp.StatusCode = (int)HttpStatusCode.OK;
                resp.ContentType = "json/application";
                resp.ContentEncoding = Encoding.UTF8;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(jsonToWrite);
                resp.ContentLength64 = buffer.Length;
                using (Stream outputStream = resp.OutputStream)
                {
                    outputStream.Write(buffer, 0, buffer.Length);
                }
                resp.Close();
                return true;
            }catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("WriteBack2Response", strError = $"can't write back to response with error|{e.Message}",e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("WriteBack2Response");
            }
        }

        private static List<MarsSpiedObjectBasicInfo> NormalizationListToSimpleList_sub(MarsSpiedObjectBasicInfo rootParent,
            List<MarsSpiedObjectInfo> srcList)
        {
            List<MarsSpiedObjectBasicInfo> rslt = new List<MarsSpiedObjectBasicInfo>();
            if (srcList == null) return rslt;
            foreach (var itm in srcList)
            {
                if (itm == null) continue ;
                //itm.Pegwindow = rootParent;
                rslt.Add(itm);
                if (!(itm is MarsSpiedObjectInfo)) continue;
                MarsSpiedObjectInfo itmObj = (MarsSpiedObjectInfo)itm;
                if (itmObj.children != null)
                {
                    var subList = NormalizationListToSimpleList_sub(rootParent, 
                        itmObj.children);
                    rslt.AddRange(subList);
                }
            }
            return rslt;
        }

        private static List<MarsSpiedObjectBasicInfo> NormalizationListToSimpleList(List<MarsSpiedObjectBasicInfo> srcList)
        {
            if (srcList == null) { return null; }
            List<MarsSpiedObjectBasicInfo> rslt = new List<MarsSpiedObjectBasicInfo>();
            for (int i = 0; i < srcList.Count; i++)
            {
                var tmpRoot = srcList[i];
                if (tmpRoot != null)
                {
                    if (!(tmpRoot is MarsSpiedObjectInfo)) continue;
                    List<MarsSpiedObjectBasicInfo> subLst = NormalizationListToSimpleList_sub(tmpRoot,
                        ((MarsSpiedObjectInfo)tmpRoot).children);
                    rslt.Add(tmpRoot);
                    rslt.AddRange(subLst);
                };
            }
            return rslt;
        }

        private static bool HighLightObjectRequestImpl(string requestInfo, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("HighLightObjectRequestImpl");
            /// step:
            /// 1, 判断是否是合法的数据包
            /// 2，提取对象信息
            /// 3，在界面中找到对象
            /// 4，flash
            /// 
            try
            {
                HighlightObjectRequest request = JsonSerializer.Deserialize<HighlightObjectRequest>(requestInfo);
                if (!request.IsRightCommand())
                {
                    simpleLog.MarsLoggerSimple.Error("HighLightObjectRequestImpl", strError = "No right object information is packaged, please try again");
                    return false;
                }
                if (request.currentObject == null)
                {
                    simpleLog.MarsLoggerSimple.Error("HighLightObjectRequestImpl", strError = "No object information is packaged, please try again");
                    return false;
                }

                windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(
                    new MarsWindowsAPIs.RECT()
                    {
                        Left = request.currentObject.x - 3,
                        Right = request.currentObject.x + request.currentObject.w + 3,
                        Top = request.currentObject.y - 3,
                        Bottom = request.currentObject.y + request.currentObject.h + 3
                    }
                    , ref strError
                    );
                return true;
            }
            catch(Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("HighLightObjectRequestImpl", ex.Message,
                    ex);
                strError = "Unable to locate the object or flash the object. Check Log file for details";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("HighLightObjectRequestImpl");
            }
        }

        private static System.Drawing.Point GetViewportCenter(IntPtr windowHandle, ref bool isOk, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetViewportCenter", $"mainhandle|{windowHandle}");
            MarsWindowsAPIs.RECT rect;
            if (MarsWindowsAPIs.GetWindowRect(windowHandle, out rect))
            {
                int centerX = (rect.Left + rect.Right) / 2;
                int centerY = (rect.Top + rect.Bottom) / 2;
                isOk = true;
                simpleLog.MarsLoggerSimple.Info("GetViewportCenter",$"center:|{centerX}-{centerY}");
                return new System.Drawing.Point(centerX, centerY);
            }
            else
            {
                isOk = false;
                simpleLog.MarsLoggerSimple.Error("GetViewportCenter", strError = "can't GetWindowRect");
                return default(System.Drawing.Point);
            }
        }
        public static async Task<bool> HandleIncomingConnectionsAsync()
        {
            
            simpleLog.MarsLoggerSimple.logBegin("HandleIncomingConnections");
            string strError = "";
            bool isOk = true;
            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                //Print out some info about the request
                simpleLog.MarsLoggerSimple.Info("HandleIncomingConnections",$"Request #:{req.RawUrl}\r\n\t{req.HttpMethod}\r\n\t{req.UserHostName}");                
                //Console.WriteLine();

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                {
                    simpleLog.MarsLoggerSimple.Info("HandleIncomingConnections", "Shutdown requested");
                    runServer = false;
                }

                string functionReq = req.Url.Segments[req.Url.Segments.Length - 1];
                functionReq        = functionReq.Trim('/');
                string requestBody = ""; //await reader.ReadToEndAsync();
                List<MarsSpiedObjectBasicInfo> targetLst = null;
                EngineAllObjectsResponse objectsRsps = null;
                string jsonToSendBack = "";
                if (req.HttpMethod.ToUpper() == "GET")
                {       
                    
                    //if (req.Url.Segments[])
                } else if (req.HttpMethod.ToUpper() == "POST") {
                    using (StreamReader reader = new StreamReader(req.InputStream))
                    {
                        requestBody = reader.ReadToEndAsync().GetAwaiter().GetResult();
                        simpleLog.MarsLoggerSimple.Info("HandleIncomingConnections", "Received post request with body: " + requestBody);
                    }

                    #region queryCurrentObjects API
                    if (functionReq.Equals("queryCurrentObjects", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            QueryObjectRequst reqObjInfo = JsonSerializer.Deserialize<QueryObjectRequst>(requestBody);

                            if ((reqObjInfo.currentHandle == 0))
                            {
                                /// set the main windows' center 
                                /// 
                                simpleLog.MarsLoggerSimple.Info("HandleIncomingConnectionsAsync.queryCurrentObjects",
                                    "no handle, going to get default one from center");
                                var p = System.Diagnostics.Process.GetCurrentProcess();
                                var pt = GetViewportCenter(p.MainWindowHandle, ref isOk, ref strError);
                                if (!isOk)
                                {
                                    EngineAllObjectsResponse objError = new EngineAllObjectsResponse()
                                    {
                                        objectCount = -1,
                                        AllObjects = null,
                                        msg = strError
                                    };
                                    resp.ContentType = "application/json";
                                    var jsback = JsonSerializer.Serialize(objError);
                                    if (!WriteBack2Response(resp, jsback, ref strError))
                                    {
                                        simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"can't write back to all objects response|{jsback}");
                                    }
                                    continue;
                                }
                                reqObjInfo.currentHandle = (long)MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(pt.X, pt.Y));
                                simpleLog.MarsLoggerSimple.Info("HandleIncomingConnectionsAsync.queryCurrentObjects",
                                    $"find new handle|{reqObjInfo.currentHandle}");
                            }
                            else if (reqObjInfo.typeOfGenerateSteps == 1)
                            {
                                // get current window's and sub controls
                                simpleLog.MarsLoggerSimple.Info("HandleIncomingConnectionsAsync","only get the container's sub objects");
                                var lstObjContainer = MarsWinformSpy.getCurrentAllObjectsOfContainer(ref isOk,ref strError, reqObjInfo, true, false);
                                if (!isOk)
                                {
                                    simpleLog.MarsLoggerSimple.Error("HandleIncomingConnectionsAsync", strError);
                                    EngineAllObjectsResponse objError = new EngineAllObjectsResponse()
                                    {
                                        objectCount = -1,
                                        AllObjects = null,
                                        msg = strError
                                    };
                                    resp.ContentType = "application/json";
                                    var jsback = JsonSerializer.Serialize(objError);
                                    if (!WriteBack2Response(resp, jsback, ref strError))
                                    {
                                        simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"getCurrentAllObjectsOfContainer|isOK|{isOk}|can't write back to all objects response|{jsback}");
                                    }
                                    continue;
                                }
                                else // have get the list of objects
                                {
                                    targetLst = NormalizationListToSimpleList(lstObjContainer.Select(p => p as MarsSpiedObjectBasicInfo).ToList());
                                    objectsRsps = new EngineAllObjectsResponse()
                                    {
                                        objectCount = lstObjContainer == null ? 0 : lstObjContainer.Count,
                                        AllObjects = targetLst
                                    };
                                    resp.ContentType = "application/json";
                                    jsonToSendBack = JsonSerializer.Serialize(objectsRsps);
                                    if (!WriteBack2Response(resp, jsonToSendBack, ref strError))
                                    {
                                        simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"getCurrentAllObjectsOfContainer|can't write back to all objects response|{jsonToSendBack}");
                                    }
                                    continue;
                                }

                            }
                            
                            simpleLog.MarsLoggerSimple.Info("HandleIncomingConnectionsAsync", "normal way to get testps");
                            /// get all objects
                            /// 
                            //var lstOfObj = MarsWinformSpy.getCurrentAllObjects(isTypePthInclude: true);
                            var lstOfObj = MarsWinformSpy.getCurrentAllObjects(reqObjInfo, isTypePthInclude: true);
                            lstOfObj = lstOfObj.GroupBy(p => p.referenceToObj).Select(p => p.First()).ToList();

                            targetLst = NormalizationListToSimpleList(lstOfObj.Select(p => p as MarsSpiedObjectBasicInfo).ToList());
                            List<MarsSpiedObjectBasicInfo> lstTarget = new List<MarsSpiedObjectBasicInfo>();
                            objectsRsps = new EngineAllObjectsResponse()
                            {
                                objectCount = lstOfObj == null ? 0 : targetLst.Count,
                                AllObjects = targetLst
                            };
                            resp.ContentType = "application/json";
                            jsonToSendBack = JsonSerializer.Serialize(objectsRsps);
                            if (!WriteBack2Response(resp, jsonToSendBack, ref strError))
                            {
                                simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"can't write back to all objects response|{jsonToSendBack}");
                            }
                            
                            continue;
                            
                        }
                        catch (Exception e)
                        {
                            simpleLog.MarsLoggerSimple.Error("queryCurrentObjects", $"Exception when analyst request|{e.Message}", e);
                            continue;
                        }
                    }
                    #endregion

                    
                    if (functionReq.Equals(RESTfulSvcActionManagement.cnst_action_req_highlight_obj, StringComparison.OrdinalIgnoreCase))
                    {
                        #region keyword highlight
                        EngineFlashControlResponse rsltFlashResponse = new EngineFlashControlResponse();
                        resp.ContentType = "application/json";
                        isOk = HighLightObjectRequestImpl(requestBody, ref strError);
                        if (isOk)
                        {
                            jsonToSendBack = JsonSerializer.Serialize(rsltFlashResponse);
                            if (!WriteBack2Response(resp, jsonToSendBack, ref strError))
                            {
                                simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"can't write back to all objects response|{jsonToSendBack}");
                            }
                        }
                        else
                        {
                            simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"highlightObject|{strError}");
                            rsltFlashResponse.msg = $"FAILED,with message|{strError}";
                            jsonToSendBack = JsonSerializer.Serialize(rsltFlashResponse);
                            if (!WriteBack2Response(resp, jsonToSendBack, ref strError))
                            {
                                simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"can't write back to all objects response|{jsonToSendBack}");
                            }
                        }
                        continue;
                        #endregion
                    }

                    
                    if (functionReq.Equals(RESTfulSvcActionManagement.cnst_action_execute_step, StringComparison.OrdinalIgnoreCase))
                    {
                        #region 执行keyword操作
                        isOk = false;
                        EngineExecuteTestStepResponse executeRslt = ExecuteKeyword(requestBody, ref strError, ref isOk);
                        /// write it back to response
                        /// 
                        if (isOk)
                            simpleLog.MarsLoggerSimple.Info("HandleIncomingConnections", "Execute step ok");
                        else simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"Execute step failed with error|{strError}");
                        resp.ContentType = "application/json";
                        string strSendBack = JsonSerializer.Serialize(executeRslt);
                        if (!WriteBack2Response(resp, strSendBack, ref strError))
                        {
                            simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"can't write back to all objects response|{strSendBack}");
                        }
                        continue;
                        #endregion
                    }

                    if (functionReq.Equals(RESTfulSvcActionManagement.cnst_action_reg_register, StringComparison.OrdinalIgnoreCase))
                    {
                        #region //注册
                        isOk=false;
                        RegisterSvcResponse rslt = RESTfulClientConnect(requestBody, ref strError, ref isOk);
                        if (rslt == null)
                        {
                            rslt = new RegisterSvcResponse()
                            {
                                msg = "FAILED,UNKNOW error, but connected",
                                //command = "FAILED"
                            };
                        }

                        resp.ContentType = "application/json";
                        string strSendBack = JsonSerializer.Serialize(rslt);
                        if (!WriteBack2Response(resp, strSendBack, ref strError))
                        {
                            simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"can't write back to all objects response|{strSendBack}");
                        }
                        continue; ;
                        #endregion
                    }

                    if (functionReq.Equals(RESTfulSvcActionManagement.cnst_action_queryObjectDetails, StringComparison.OrdinalIgnoreCase))
                    {
                        //获得详细的对象信息，如swftable的所有的column
                        #region  //获得详细的对象信息
                        isOk = false;
                        EngineGetObjectExtensionDetailRspn detailRspns = GetObjectExtensionDetails(requestBody, ref strError, ref isOk);
                        if (isOk)
                        {
                            simpleLog.MarsLoggerSimple.Info(RESTfulSvcActionManagement.cnst_action_queryObjectDetails, "works fine");
                        }
                        else
                        {
                            simpleLog.MarsLoggerSimple.Error(RESTfulSvcActionManagement.cnst_action_queryObjectDetails, strError);
                        }
                        resp.ContentType = "application/json";
                        string strSendBack = JsonSerializer.Serialize(detailRspns);
                        if (!WriteBack2Response(resp, strSendBack, ref strError))
                        {
                            simpleLog.MarsLoggerSimple.Error(RESTfulSvcActionManagement.cnst_action_queryObjectDetails, $"{RESTfulSvcActionManagement.cnst_action_queryObjectDetails}|can't write back to all objects response|{strSendBack}");
                        }
                        continue;
                        #endregion
                    }

                    if (functionReq.Equals(RESTfulSvcActionManagement.cnst_action_removeTestStepByRunord, StringComparison.OrdinalIgnoreCase))
                    {
                        //获得详细的对象信息，如swftable的所有的column
                        #region  //获得详细的对象信息
                        isOk = false;
                        RemoveTestStepByRunOrdForRecordAndReplayReq detailRspns = RemoveTestStepByRunOrdSvc(requestBody, ref strError, ref isOk);
                        if (isOk)
                        {
                            simpleLog.MarsLoggerSimple.Info(RESTfulSvcActionManagement.cnst_action_queryObjectDetails, "works fine");
                        }
                        else
                        {
                            simpleLog.MarsLoggerSimple.Error(RESTfulSvcActionManagement.cnst_action_queryObjectDetails, strError);
                        }
                        resp.ContentType = "application/json";
                        string strSendBack = JsonSerializer.Serialize(detailRspns);
                        if (!WriteBack2Response(resp, strSendBack, ref strError))
                        {
                            simpleLog.MarsLoggerSimple.Error(RESTfulSvcActionManagement.cnst_action_queryObjectDetails, $"{RESTfulSvcActionManagement.cnst_action_queryObjectDetails}|can't write back to all objects response|{strSendBack}");
                        }
                        continue;
                        #endregion
                    }
                    

                    /// replay的请求
                    if (functionReq.Equals(RESTfulSvcActionManagement.cnst_action_replayStep, StringComparison.OrdinalIgnoreCase))
                    {
                        #region //请求运行replay（逐步）
                        EngineReplayTestStepInJsonRspn replayRspns = ReplayATestStepFromSpyTool(requestBody, ref strError, ref isOk);
                        if (isOk)
                        {
                            simpleLog.MarsLoggerSimple.Info(RESTfulSvcActionManagement.cnst_action_replayStep, "works fine");
                        }
                        else
                        {
                            simpleLog.MarsLoggerSimple.Error(RESTfulSvcActionManagement.cnst_action_replayStep, strError);
                        }
                        resp.ContentType = "application/json";
                        string strSendBack = JsonSerializer.Serialize(replayRspns);
                        if (!WriteBack2Response(resp, strSendBack, ref strError))
                        {
                            simpleLog.MarsLoggerSimple.Error(RESTfulSvcActionManagement.cnst_action_queryObjectDetails, $"{RESTfulSvcActionManagement.cnst_action_replayStep}|can't write back to all objects response|{strSendBack}");
                        }
                        continue;
                        #endregion
                    }

                    /// 查询record&replay状态，是否停止
                    /// 
                    if (functionReq.Equals(RESTfulSvcActionManagement.cnst_action_queryRecordAndReplayStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        #region ///请求record replay的状态，是否停止
                        EngineQueryRecordReplayStatusRspns recordReplayRspns = new EngineQueryRecordReplayStatusRspns()
                        {
                            IsRunning = Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer.IsRecording
                        };
                        resp.ContentType = "application/json";
                        string strSendBack = JsonSerializer.Serialize(recordReplayRspns);
                        if (!WriteBack2Response(resp, strSendBack, ref strError))
                        {
                            simpleLog.MarsLoggerSimple.Error(RESTfulSvcActionManagement.cnst_action_queryRecordAndReplayStatus, $"{RESTfulSvcActionManagement.cnst_action_replayStep}|can't write back to all objects response|{strSendBack}");
                        }
                        continue;
                        #endregion// 请求record replay的状态，是否停止
                    }

                    simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"unsupported action|{functionReq}");
                    continue;
                }else
                {
                    simpleLog.MarsLoggerSimple.Error("HandleIncomingConnections", $"unsupported method|{req.HttpMethod}");
                    continue;
                }
            }
            return true;
        }

        private static EngineReplayTestStepInJsonRspn ReplayATestStepFromSpyTool(string requestBody, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("ReplayATestStepFromSpyTool");
            try
            {
                MarsRecordReplayStep replayStepReq = JsonSerializer.Deserialize<MarsRecordReplayStep>(requestBody);
                if (replayStepReq == null)
                {
                    isOk = false;
                    strError = "can't convert data to replay object";
                    return new EngineReplayTestStepInJsonRspn()
                    {
                        msg = $"FAILED, {strError}",
                        //command = "FAILED"
                    };
                }
                string returnValues = "", strStack = "", strAdv = "", strFileName = "";
                
                isOk = ClientDealWithGUIKeyword.DealKeywordByKeywordName(replayStepReq.keyWord, replayStepReq.Parameter, replayStepReq.opText,
                    string.IsNullOrEmpty(replayStepReq.objectMarsType) ? "SwfObject" : replayStepReq.objectMarsType,
                    replayStepReq.GetObjInfo(), replayStepReq.GetPegInfo(), "",
                    replayStepReq.pegQuickAccess.objectName, replayStepReq.objectQuickAccess.objectName, 5,
                    null, ref strError, ref returnValues, ref strStack, ref strAdv, ref strFileName);
                if (isOk)
                {
                    simpleLog.MarsLoggerSimple.Info("ReplayATestStepFromSpyTool",$"returnValues|{returnValues}|keyword|{replayStepReq.keyWord}|");
                    return new EngineReplayTestStepInJsonRspn
                    {
                        returnedValues = returnValues,
                    };
                }
                else
                {
                    return new EngineReplayTestStepInJsonRspn
                    {
                        msg = $"FAILED, {strError}",
                    };
                }
            }catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("ReplayATestStepFromSpyTool",$"Exception|{e.Message}", e);
                return new EngineReplayTestStepInJsonRspn
                {
                    msg = $"FAILED,{e.Message}",
                };
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("EngineReplayTestStepInJsonRspn");
            }
        }

        private static RegisterSvcResponse RESTfulClientConnect(string requestBody, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("RESTfulClientConnect");
            try
            {
                RegisterSvcRequst req = JsonSerializer.Deserialize<RegisterSvcRequst>(requestBody);
                if (req == null)
                {
                    isOk = false;
                    strError = "No validate Register Data. Connection failed";
                    return new RegisterSvcResponse()
                    {
                        msg = $"FAILED, {strError}",
                        //command = "FAILED"
                    };
                }
                isOk = true;
                return new RegisterSvcResponse()
                {
                    msg = "SUCCESS, connected",
                    //command = "SUCCESS"
                };
            }
            catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("RESTfulClientConnect", strError = e.Message, e);
                strError = e.Message;
                isOk = false;
                return new RegisterSvcResponse()
                {
                    msg = $"FAILED, can't register, with exception|{e.Message}"
                };
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("RESTfulClientConnect");
            }
        }

        private static EngineGetObjectExtensionDetailRspn GetObjectExtensionDetails(string requestBody, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetObjectExtensionDetails");
            string strADV = ""
                , strStack = "";
            try
            {
                EngineGetObjectExtensionDetailReq req = JsonSerializer.Deserialize<EngineGetObjectExtensionDetailReq>(requestBody);
                if (req.objectHwnd <= 0)
                {
                    strError = "no object handle is set.";
                    isOk = false;
                    return new EngineGetObjectExtensionDetailRspn()
                    {
                        msg = strError,
                    };
                }
                System.Windows.Forms.Control c = System.Windows.Forms.Control.FromHandle(new IntPtr(req.objectHwnd));
                if (c == null)
                {
                    strError = $"can't load control from handle|{req.objectHwnd}|";
                    isOk = false;
                    return new EngineGetObjectExtensionDetailRspn()
                    {
                        msg = strError
                    };
                }
                if (req.objectExtCmd== EngineQueryObjCommand._getAllColumns)
                {
                    string strTypes = ReflectorForCSharp.GetObjectBaseType(c.GetType());
                    simpleLog.MarsLoggerSimple.Info("GetObjectExtensionDetails", $"begin {strTypes}");
                    if (strTypes.Contains("Infragistics.Win.UltraWinGrid.UltraGrid")
                        || (string.Compare("Infragistics.Win.UltraWinGrid.UltraGrid", c.GetType().ToString()) == 0)) {
                        /// should make sure
                        ThirdPartComponent.Infragistics.MarsTableOperation tblOp = new ThirdPartComponent.Infragistics.MarsTableOperation();
                        List<ThirdPartComponent.Infragistics.MARSColumnsInfo> lstCols = tblOp.GetAllColumnsInfo(c, "ENGINE_PEG", "ENGINE_OBJ", ref isOk, ref strError, ref strADV, ref strStack);
                        if (!isOk)
                        {
                            simpleLog.MarsLoggerSimple.Error("GetObjectExtensionDetails", strError);
                            return new EngineGetObjectExtensionDetailRspn()
                            {
                                msg = strError
                            };
                        }
                        isOk = true;
                        EngineGetObjectExtensionDetailRspn rspn = new EngineGetObjectExtensionDetailRspn();
                        rspn.extensionData = new List<MarsKVPair>();
                        rspn.msg = "SUCCESS";
                        lstCols.ForEach(p =>
                        {
                            if (p != null)
                            {
                                rspn.extensionData.Add(new MarsKVPair()
                                {
                                    k = p.columnKey,
                                    v = p.columnCaption
                                });
                            }
                        });
                        return rspn;
                    }
                    else
                    {
                        strError = $"unsupported object type|{strTypes}";
                        isOk = false;
                        return new EngineGetObjectExtensionDetailRspn()
                        {
                            msg = strError,
                        };
                    }
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Error("GetObjectExtensionDetails",strError =$"unsupported command sub-type|{req.objectExtCmd}"  );
                    isOk = false;
                    return new EngineGetObjectExtensionDetailRspn()
                    {
                        msg = strError,
                    };
                }
            }catch (Exception e)
            {
                strError = $"exception when GetObjectExtensionDetails|{e.Message}";
                simpleLog.MarsLoggerSimple.Error("GetObjectExtensionDetails", strError);
                isOk = false;
                return new EngineGetObjectExtensionDetailRspn()
                {
                    msg = e.Message 
                };
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetObjectExtensionDetails");
            }    
        }

        private static RemoveTestStepByRunOrdForRecordAndReplayReq RemoveTestStepByRunOrdSvc(string requestBody, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("RemoveTestStepByRunOrdSvc");
          
            int iRunOrd = -1;
            try
            {
                RemoveTestStepByRunOrdForRecordAndReplayReq req = JsonSerializer.Deserialize<RemoveTestStepByRunOrdForRecordAndReplayReq>(requestBody);
                if (req.runOrd < 0)
                {
                    strError = "runord is less than 0";
                    isOk = false;
                    return new RemoveTestStepByRunOrdForRecordAndReplayReq()
                    {
                        msg = strError,
                        runOrd = req.runOrd
                    };
                }
                iRunOrd = req.runOrd;
                if (Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer.RemoveRecordReplayStepsByRunId(req.runOrd, ref strError))
                {
                    isOk = true;
                    return new RemoveTestStepByRunOrdForRecordAndReplayReq()
                    {
                        msg = "SUCCESS",
                        runOrd = req.runOrd
                    };
                }
                else
                {
                    isOk = false;
                    return new RemoveTestStepByRunOrdForRecordAndReplayReq()
                    {
                        msg = $"remove {req.runOrd} FAILED WITH error|{strError}",
                        runOrd = req.runOrd
                    };
                }
                
            }
            catch (Exception e)
            {
                strError = $"exception when remove a node from record and replay list|{e.Message}";
                simpleLog.MarsLoggerSimple.Error("RemoveTestStepByRunOrdSvc", strError);
                isOk = false;
                return new RemoveTestStepByRunOrdForRecordAndReplayReq()
                {
                    msg = $"remove {iRunOrd} FAILED |{strError}",
                    runOrd = iRunOrd
                };
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("RemoveTestStepByRunOrdSvc");
            }
        }

        private static EngineExecuteTestStepResponse ExecuteKeyword(string requestBody, ref string strError, ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("ExecuteKeyword");
            /// step:
            /// 1, 将requetBody转换为对象
            /// 2，从中获取peg和obj
            /// 3，定位obj
            /// 4，操作
            /// 
            int iErrorCode = 0xE0;
            EngineExecuteTestStepResponse rslt = new EngineExecuteTestStepResponse();
            try
            {
                EngineExecuteTestStepRequest reqObj = JsonSerializer.Deserialize<EngineExecuteTestStepRequest>(requestBody);
                iErrorCode = 0xE1;
                if (reqObj == null)
                {
                    isOk = false;
                    strError = $"Error code is 0xE1, Test step information is not compatitable.";
                    return null;
                }
                rslt.startTime = reqObj.startTime;
                iErrorCode = 0xE2;
                isOk = validateTestStepInfo(reqObj, ref strError);
                if (!isOk )
                {
                    strError = $"Error code is 0xE2, valiate test step failed with error|{strError}.";
                    return null;
                }
                iErrorCode = 0xE3;
                List<Form> lstFrm = ClientDealWithGUIKeyword.GetOpenFormsAsList();
                if (lstFrm == null)
                {
                    strError = $"Error code is 0xE3, Can't get all Forms, please try later";
                    simpleLog.MarsLoggerSimple.Error("ExecuteKeyword", strError);
                    return null;
                }
                iErrorCode = 0xE4;
                if ((reqObj.pegInfo != null)&&("pegwindow".Equals(reqObj.Keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    strError = $"Error code is 0xE4, No parent objects Info is set, please try later";
                    simpleLog.MarsLoggerSimple.Error("ExecuteKeyword", strError);
                    return null;
                }
                iErrorCode = 0xE5;
                Dictionary<string, string> pegInfo = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(reqObj.pegInfo, ref isOk);
                if (!isOk)
                {
                    strError = $"Error code is 0xE5, pegwindow info is not correct, format should be id_type:=id_information|{reqObj.pegInfo}";
                    simpleLog.MarsLoggerSimple.Error("ExecuteKeyword", strError);
                    return null;
                }
                iErrorCode = 0xE6;
                Dictionary<string, string> objInfo = ObjectInfoAnlyst.AlystObjectPropertiesFromQtp(reqObj.ObjInfo, ref isOk);
                if (!isOk)
                {
                    strError = $"Error code is 0xE6, object info is not correct, format should be id_type:=id_information|{reqObj.ObjInfo}";
                    simpleLog.MarsLoggerSimple.Error("ExecuteKeyword", strError);
                    return null;
                }
                iErrorCode = 0xE7;
                string returnValues = "",
                    strStack = "",
                    strAdv = "",
                    strFileName = "";
                isOk = ClientDealWithGUIKeyword.DealKeywordByKeywordName(reqObj.Keyword, reqObj.Parameter, reqObj.OpData,
                    string.IsNullOrEmpty(reqObj.marsObjType)?"SwfObject":reqObj.marsObjType, 
                    objInfo, pegInfo, "", 
                    reqObj.pegName, reqObj.ObjName, 5,
                    null, ref strError, ref returnValues, ref strStack, ref strAdv, ref strFileName);
                rslt.returnValues = returnValues;
                rslt.executeStepOk = isOk;
                rslt.endTime = DateTime.Now;
                rslt.generatedFilePath = strFileName;
                if (!isOk)
                {
                    rslt.msg = strError;
                    simpleLog.MarsLoggerSimple.Error("ExecuteKeyword", $"Error code is 0xE7,|{reqObj.Keyword}|{reqObj.OpData}|{reqObj.pegInfo}|failed|{strError}");                    
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Info("ExecuteKeyword", $"{reqObj.Keyword}|{reqObj.OpData}|{reqObj.pegInfo}|success");
                }
                return rslt;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("ExecuteKeyword", strError = $"Error code|{iErrorCode}|with Exception|{e.Message}", e);
                switch(iErrorCode)
                {
                    case 0xE0: // can't convert body string to EngineExecuteTestStepRequest
                        strError = $"Error code 0xE0,the Test step information is not compatitable.";
                        isOk = false;
                        break ;
                    case 0xE2: // validate failed
                        strError = $"Error code 0xE2,validate Test Step failed.";
                        isOk = false;
                        break;

                }
                rslt.endTime = DateTime.Now;
                rslt.executeStepOk = false;
                rslt.msg = strError;
                return rslt;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("ExecuteKeyword", $"{rslt.executeStepOk}");
            }
        }

        private static bool validateTestStepInfo(EngineExecuteTestStepRequest reqObj, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("validateTestStepInfo");
            try
            {
                if (string.IsNullOrEmpty(reqObj.Keyword))
                {
                    strError = "No Keyword is set";
                    return false;
                }
                /// 应该需要检测是否存在对象，fordemo 暂时不处理
                return true;
            }catch(Exception e)
            {
                strError = e.Message;
                simpleLog.MarsLoggerSimple.Error("validateTestStepInfo", e.Message, e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("validateTestStepInfo");
            }
        }
    }
}
