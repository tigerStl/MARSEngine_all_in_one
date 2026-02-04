using Mars.message.Inter.MQCenter.interProcess;
using MarsSpyTool.subToolWindows.testStepEditor;
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MarsSpyTool.message.Inter.MQCenter.interProcess.HttpRestService.client
{
    internal class RESTClient2MessageCenter
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static RESTClient2MessageCenter inst = null;
        private string base_url_rest = "";
        public static RESTClient2MessageCenter getInstance()
        {
            if (inst == null)
            {
                inst = new RESTClient2MessageCenter();
            }
            return inst;
        }
        private System.Net.Http.HttpClient restfulClient = new System.Net.Http.HttpClient();
        public bool checkServerInfo(ref int port, ref string strError )
        {
            logger.Info("checkServerInfo\tbegin");
            if (RestServiceInfo.Instance().currentRESTfulSvc==null)
            {
                strError = "Port swap file is not loaded";
                logger.Error($"checkServerInfo\t{strError}");
                return false;
            }
            if ((port = RestServiceInfo.Instance().currentRESTfulSvc.port) <= 0)
            {
                strError = $"no Port is create|{port}";
                logger.Error($"checkServerInfo\t{strError}");
                return false;
            }
            if (string.IsNullOrEmpty(this.base_url_rest))
            {
                this.base_url_rest = $"http://localhost:{port}";
            }
            return true;
        }
        internal List<MarsSpiedObjectBasicInfo> QueryCurrentObjects(QueryObjectRequst reqInfo, ref bool isOk, ref string strError)
        {
            logger.Info("QueryCurrentObjects\tbegin");
            int port = -1;
           
            if (!(isOk=checkServerInfo(ref port, ref strError)))
            {
                return null;
            }
            string strURL = $"{this.base_url_rest}/queryCurrentObjects";
            try
            {   
                string json = System.Text.Json.JsonSerializer.Serialize(reqInfo);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage responseGet = restfulClient.PostAsync(strURL, content).GetAwaiter().GetResult();                
                responseGet.EnsureSuccessStatusCode();
                string strObjInfo = responseGet.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                EngineAllObjectsResponse objsRspns =System.Text.Json.JsonSerializer.Deserialize<EngineAllObjectsResponse>(strObjInfo);
                if (objsRspns == null)
                {
                    logger.Error(strError = $"QueryCurrentObjects\t|{strURL} doesn't return value, check server");
                    return null;
                }
                if (!objsRspns.IsRightCommand())
                {
                    logger.Error(strError = $"QueryCurrentObjects\t|{strURL} returns wrong command package|{objsRspns.command}");
                    return null;
                }
                isOk = true;
                return objsRspns.AllObjects;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                logger.Error(e,$"QueryCurrentObjects\t|{e.Message}");
                return null;
            }
            finally
            {
                logger.Info("QueryCurrentObjects\tend");
            }
            
        }

        internal bool testHeartBeat()
        {
            logger.Info("testHeartBeat\tbegin");
            int port = -1;
            string strError = "";
            bool isOk = false;
            if (!(isOk = checkServerInfo(ref port, ref strError)))
            {
                return false;
            }
            string strURL = $"{this.base_url_rest}/{RESTfulSvcActionManagement.cnst_action_reg_register}";
            try
            {
                RegisterSvcRequst svcReg = new RegisterSvcRequst();
                string hostName = Dns.GetHostName();
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);
                if ((addresses != null) && (addresses.Length > 0))
                {
                    var a = addresses.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault();
                    svcReg.ip = a==null?hostName:a.ToString();
                }
                else
                {
                    svcReg.ip = hostName;
                }
                svcReg.msg = RegisterSvcRequst.cnst_shakehand_code;
                string json = System.Text.Json.JsonSerializer.Serialize(svcReg);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                //restfulClient.Timeout = new TimeSpan(0,0, 10);
                HttpResponseMessage responsePost = restfulClient.PostAsync(strURL, content).GetAwaiter().GetResult();
                responsePost.EnsureSuccessStatusCode();
                string strObjInfo = responsePost.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                RegisterSvcResponse objsRspns = System.Text.Json.JsonSerializer.Deserialize<RegisterSvcResponse>(strObjInfo);
                if (objsRspns == null)
                {
                    logger.Error(strError = $"testHeartBeat\t|{strURL} doesn't return value, check server");
                    return false;
                }
                if (!objsRspns.IsRightCommand())
                {
                    logger.Error(strError = $"testHeartBeat\t|{strURL} returns wrong command package|{objsRspns.command}");
                    return false;
                }
                isOk = true;
                return true;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                logger.Error(e, $"QueryCurrentObjects\t|{e.Message}");
                return false;
            }
            finally
            {
                logger.Info("QueryCurrentObjects\tend");
            }
        }

        internal MarsSpiedObjectBasicInfo HighlightObject(MarsSpiedObjectBasicInfo objectInfo, ref bool isOk, ref string strError)
        {
            logger.Info("HighlightObject\tbegin");
            int port = -1;
            if (!(isOk = checkServerInfo(ref port, ref strError)))
            {
                return null;
            }
            string strURL = $"{this.base_url_rest}/{RESTfulSvcActionManagement.cnst_action_req_highlight_obj}";
            try
            {
                HighlightObjectRequest obj2Highlight = new HighlightObjectRequest();
                obj2Highlight.currentObject = objectInfo;
                string json = System.Text.Json.JsonSerializer.Serialize(obj2Highlight);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage responsePost = restfulClient.PostAsync(strURL, content).GetAwaiter().GetResult();
                responsePost.EnsureSuccessStatusCode();
                string strObjInfo = responsePost.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                EngineAllObjectsResponse objsRspns = System.Text.Json.JsonSerializer.Deserialize<EngineAllObjectsResponse>(strObjInfo);
                if (objsRspns == null)
                {
                    logger.Error(strError = $"QueryCurrentObjects\t|{strURL} doesn't return value, check server");
                    return null;
                }
                if (!objsRspns.IsRightCommand())
                {
                    logger.Error(strError = $"QueryCurrentObjects\t|{strURL} returns wrong command package|{objsRspns.command}");
                    return null;
                }
                isOk = true;
                return null;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                logger.Error(e, $"QueryCurrentObjects\t|{e.Message}");
                return null;
            }
            finally
            {
                logger.Info("QueryCurrentObjects\tend");
            }

        }

        internal EngineExecuteTestStepResponse RunOneTestStep(MARSTestStepsModel objTestStep, ref bool isOk, ref string strError)
        {
            logger.Info("RunOneTestStep\tbegin");
            int port = -1;
            if (!(isOk = checkServerInfo(ref port, ref strError)))
            {
                return null;
            }
            string strURL = $"{this.base_url_rest}/{RESTfulSvcActionManagement.cnst_action_execute_step}";
            string json = "";

            try
            {
                if ((objTestStep.Keyword == null)
                    ||(string.IsNullOrEmpty(objTestStep.Keyword.KEY_WORD_NAME)))
                {
                    strError = "No Keyword information, please try again";
                    isOk = false;
                    return null;
                }

                EngineExecuteTestStepRequest executeStepReq = new EngineExecuteTestStepRequest()
                {
                    Keyword = objTestStep.Keyword.KEY_WORD_NAME,
                    pegInfo = objTestStep.AttachedObject.Pegwindow.getObjectInfo(),
                    pegName = objTestStep.AttachedObject.Pegwindow.PegName,
                    ObjName = objTestStep.AttachedObject.objectName,
                    ObjInfo = objTestStep.AttachedObject.getObjectInfo(),
                    Parameter = objTestStep.Test_parameter,
                    OpData = objTestStep.CurrentData,
                    marsObjType = objTestStep.ObjectType
                };
                json = System.Text.Json.JsonSerializer.Serialize(executeStepReq);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                restfulClient.Timeout = TimeSpan.FromSeconds(30);
                HttpResponseMessage responsePost = restfulClient.PostAsync(strURL, content).GetAwaiter().GetResult();
                responsePost.EnsureSuccessStatusCode();                
                string strTestResultInfo = responsePost.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                EngineExecuteTestStepResponse objsRspns = System.Text.Json.JsonSerializer.Deserialize<EngineExecuteTestStepResponse>(strTestResultInfo);
                if (objsRspns == null)
                {
                    logger.Error(strError = $"QueryCurrentObjects\t|{strURL} doesn't return value, check server");
                    isOk = false;
                    return null;
                }
                if (!objsRspns.IsRightCommand())
                {
                    logger.Error(strError = $"QueryCurrentObjects\t|{strURL} returns wrong command package|{objsRspns.command}");
                    isOk = false;
                    return null;
                }
                isOk = true;
                return objsRspns;
            }
            catch(Exception e)
            {
                strError = $"can't execute test step|{e.Message}";
                logger.Error(e, $"QueryCurrentObjects\t|{e.Message}\r\n|{json}");
                isOk = false;
                return null;
            }
            finally
            {
                logger.Info("RunOneTestStep\tend");
            }
        }

        internal EngineGetObjectExtensionDetailRspn QueryObjDetails(EngineGetObjectExtensionDetailReq req, ref bool isOk, ref string strError)
        {
            logger.Info($"QueryObjDetails\tbegin");
            try
            {
                int port = -1;
                if (!(isOk = checkServerInfo(ref port, ref strError)))
                {
                    return null;
                }
                string strURL = $"{this.base_url_rest}/{RESTfulSvcActionManagement.cnst_action_queryObjectDetails}";
                string json = "";

                json = System.Text.Json.JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                logger.Info($"QueryObjectDetails\t|{json}");
                //restfulClient.Timeout = TimeSpan.FromSeconds(30);
                HttpResponseMessage responsePost = restfulClient.PostAsync(strURL, content).GetAwaiter().GetResult();
                responsePost.EnsureSuccessStatusCode();
                string strTestResultInfo = responsePost.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                EngineGetObjectExtensionDetailRspn objsRspns = System.Text.Json.JsonSerializer.Deserialize<EngineGetObjectExtensionDetailRspn>(strTestResultInfo);
                if (objsRspns == null)
                {
                    logger.Error(strError = $"QueryCurrentObjects\t|{strURL} doesn't return value, check server");
                    isOk = false;
                    return null;
                }
                if (!objsRspns.IsRightCommand())
                {
                    logger.Error(strError = $"QueryCurrentObjects\t|{strURL} returns wrong command package|{objsRspns.command}");
                    isOk = false;
                    return null;
                }
                isOk = true;
                return objsRspns;
            }
            catch(Exception e)
            {
                strError = $"Can't get objectInfo with exception|{e.Message}";
                logger.Error(e, $"QueryObjDetails\tException|{e.Message}");
                isOk = false;
                return new EngineGetObjectExtensionDetailRspn()
                {
                    command = RESTfulSvcActionManagement.cnst_rsp_type_error,
                    msg = strError
                };
            }
            finally
            {
                logger.Info("QueryObjDetails\tEnd");
            }
        }

        #region replay section
        public bool doTestStepReplayViaStepString(string strStepInJson, ref string strError)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|doTestStepReplayViaStepString\tBegin|step to replay|{strStepInJson}");
            bool isOk = true;
            try
            {
                int port = -1;
                if (!(isOk = checkServerInfo(ref port, ref strError)))
                {
                    logger.Error($"{iMark}|doTestStepReplayViaStepString\tcheckServerInfo returns error|{strError}");
                    return false;
                }
                string strURL = $"{this.base_url_rest}/{RESTfulSvcActionManagement.cnst_api_catalog_replay}/{RESTfulSvcActionManagement.cnst_action_replayStep}";
                var content = new StringContent(strStepInJson, Encoding.UTF8, "application/json");
                logger.Info($"QueryObjectDetails\t|{strStepInJson}");
                //restfulClient.Timeout = TimeSpan.FromSeconds(30);
                HttpResponseMessage responsePost = restfulClient.PostAsync(strURL, content).GetAwaiter().GetResult();
                responsePost.EnsureSuccessStatusCode();
                string strTestResultInfo = responsePost.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                EngineReplayTestStepInJsonRspn stepReplayRspns = System.Text.Json.JsonSerializer.Deserialize<EngineReplayTestStepInJsonRspn>(strTestResultInfo);
                if (stepReplayRspns == null)
                {
                    strError = "Can't replay the step, please check log for details";
                    logger.Error($"{iMark}|doTestStepReplayViaStepString\t|can't convert response to type|EngineReplayTestStepInJsonRspn|\r\nsource response|{strTestResultInfo}");
                    return false;
                }
                if (!stepReplayRspns.IsRightCommand())
                {
                    strError = $"API |{strURL}| doesn't returns right command";
                    logger.Error($"doTestStepReplayViaStepString\t|{strURL} returns wrong command package|{stepReplayRspns.command}");
                    isOk = false;
                    return isOk;
                }
                if (!stepReplayRspns.isExpectedResult())
                {
                    strError = stepReplayRspns.msg;
                    return false;
                }
                strError = stepReplayRspns.returnedValues; //如果处理正确，就将returnedvalue通过strError带出去
                isOk = true;
                return isOk;
            }
            catch (Exception e)
            {
                strError = $"can't execute the step with error|{e.Message}";
                logger.Error($"{iMark}|doTestStepReplayViaStepString\t|{e.Message}", e);
                return false;
            }
            finally
            {
                logger.Info($"{iMark}|doTestStepReplayViaStepString\tEnd");
            }
        }

        public bool doRecordReplayRemoveTestStep(int iRunOrdId)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|doRecordReplayRemoveTestStep\tBegin");
            string strError = "";
            bool isOk = false;
            try
            {
                int port = -1;
                if (!(isOk = checkServerInfo(ref port, ref strError)))
                {
                    logger.Error($"{iMark}|doRecordReplayRemoveTestStep\tcheckServerInfo returns error|{strError}");
                    return false;
                }

                var removeStepByRunOrd = new RemoveTestStepByRunOrdForRecordAndReplayReq();
                removeStepByRunOrd.runOrd = iRunOrdId;
                string strQueryJson = System.Text.Json.JsonSerializer.Serialize(removeStepByRunOrd);

                string strURL = $"{this.base_url_rest}/{RESTfulSvcActionManagement.cnst_api_catalog_replay}/{RESTfulSvcActionManagement.cnst_action_removeTestStepByRunord}";
                var content = new StringContent(strQueryJson, Encoding.UTF8, "application/json");
                logger.Info($"doRecordReplayRemoveTestStep\t|{strQueryJson}");
                //restfulClient.Timeout = TimeSpan.FromSeconds(30);
                HttpResponseMessage responsePost = restfulClient.PostAsync(strURL, content).GetAwaiter().GetResult();
                responsePost.EnsureSuccessStatusCode();
                string strQueryRecrodReplayStatus = responsePost.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                RemoveTestStepByRunOrdForRecordAndReplayResp removeStepRspns = System.Text.Json.JsonSerializer.Deserialize<RemoveTestStepByRunOrdForRecordAndReplayResp>(strQueryRecrodReplayStatus);
                if (removeStepRspns == null)
                {
                    strError = "Can't get service status, please check log for details";
                    logger.Error($"{iMark}|doRecordReplayRemoveTestStep\t|can't convert response to type|EngineReplayTestStepInJsonRspn|\r\nsource response|{strQueryRecrodReplayStatus}");
                    return false;
                }
                if (!removeStepRspns.IsRightCommand())
                {
                    strError = $"API |{strURL}| doesn't returns right command";
                    logger.Error($"doRecordReplayRemoveTestStep\t|{strURL} returns wrong command package|{removeStepRspns.command}");
                    isOk = false;
                    return false;
                }

                strError = removeStepRspns.msg; //如果处理正确，就将returnedvalue通过strError带出去
                isOk = true;
                return true;
            }
            catch (Exception e)
            {
                strError = $"can't execute the step with error|{e.Message}";
                logger.Error($"{iMark}|doRecordReplayRemoveTestStep\t|{e.Message}", e);
                isOk = false;
                return false;
            }
            finally
            {
                logger.Info($"{iMark}|doRecordReplayRemoveTestStep\tEnd");
            }
        }


        internal EngineQueryRecordReplayStatusRspns QueryRecordReplayStatus(ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|QueryRecordReplayStatus\tBegin");
            
            try
            {
                int port = -1;
                if (!(isOk = checkServerInfo(ref port, ref strError)))
                {
                    logger.Error($"{iMark}|QueryRecordReplayStatus\tcheckServerInfo returns error|{strError}");
                    return null;
                }

                var queryRecordAndReplayReq = new EngineExecuteTestStepRequest();
                string strQueryJson = System.Text.Json.JsonSerializer.Serialize(queryRecordAndReplayReq);

                string strURL = $"{this.base_url_rest}/{RESTfulSvcActionManagement.cnst_api_catalog_replay}/{RESTfulSvcActionManagement.cnst_action_queryRecordAndReplayStatus}";
                var content = new StringContent(strQueryJson, Encoding.UTF8, "application/json");
                logger.Info($"QueryRecordReplayStatus\t|{strQueryJson}");
                //restfulClient.Timeout = TimeSpan.FromSeconds(30);
                HttpResponseMessage responsePost = restfulClient.PostAsync(strURL, content).GetAwaiter().GetResult();
                responsePost.EnsureSuccessStatusCode();
                string strQueryRecrodReplayStatus = responsePost.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                EngineQueryRecordReplayStatusRspns stepReplayRspns = System.Text.Json.JsonSerializer.Deserialize<EngineQueryRecordReplayStatusRspns>(strQueryRecrodReplayStatus);
                if (stepReplayRspns == null)
                {
                    strError = "Can't get service status, please check log for details";
                    logger.Error($"{iMark}|QueryRecordReplayStatus\t|can't convert response to type|EngineReplayTestStepInJsonRspn|\r\nsource response|{strQueryRecrodReplayStatus}");
                    return null;
                }
                if (!stepReplayRspns.IsRightCommand())
                {
                    strError = $"API |{strURL}| doesn't returns right command";
                    logger.Error($"QueryRecordReplayStatus\t|{strURL} returns wrong command package|{stepReplayRspns.command}");
                    isOk = false;
                    return null;
                }
                if (!stepReplayRspns.isExpectedResult())
                {
                    strError = stepReplayRspns.msg;
                    isOk = false;
                    return null;
                }
                strError = stepReplayRspns.msg; //如果处理正确，就将returnedvalue通过strError带出去
                isOk = true;
                return stepReplayRspns;
            }
            catch (Exception e)
            {
                strError = $"can't execute the step with error|{e.Message}";
                logger.Error($"{iMark}|QueryRecordReplayStatus\t|{e.Message}", e);
                isOk = false;
                return null;
            }
            finally
            {
                logger.Info($"{iMark}|doTestStepReplayViaStepString\tEnd");
            }
        }
        #endregion replay section
    }
}
