using Mars.message.AutoTestingDriver.interProcess;
using Mars.AutoTestingDriver.mars.javasupport.data;
using NLog;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.mars.javasupport
{

    public class MarsJavaDriver
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsJavaDriver));  

        private MarsJavaDataMgr javaObgAgentMgr = new MarsJavaDataMgr();

        private MarsJavaDriver()
        {

        }

        public static MarsJavaDriver GetInstance()
        {
            return new MarsJavaDriver();
        }

        public bool JavaFillEdit(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType,
            string strAttachInfo, string pegName, string objName,
            ref string strError, ref MARSDealResult dealResult)
        {
            Logger.logBegin("JavaFillEdit", $"{strKeyword}|{stepId}");
            /// 1, make sure that the attach is done
            /// 2, if not attached, then attach to the jvm and connect to webSocket
            /// 3, sent command to jvm webSocket
            /// 4, wait until the webSocket return
            /// 5, based on returned JSON, and return test result

            /// 1, make sure that the attach is done
            /// 
            var javaSocketClient = MarsJavaWebSocketClient.GetJavaWebSocketClient();
            if (javaSocketClient == null)
            {
                throw new NotImplementedException("attach to exists java is not implements");
            }
            string stepJson = javaObgAgentMgr.CreateObjectStringToJavaEngine(dictPegProperties, dictObjProperties, pegName,
                objName, strData, strParaMeter, strKeyword, null, (int)stepId, true);
            /// 3, sent command to jvm websocket
            /// 
            bool isOk = false;
            var responseObj = javaSocketClient.sendRequestAndGetReponse(javaObgAgentMgr.currentTestStepRequest, stepJson, ref isOk, ref strError);
            if (responseObj == null)
            {
                isOk = false;
                dealResult.AskTime = DateTime.Now; //
            }
            else
            {
                dealResult.ResultMessage = responseObj.testResult;
                dealResult.ReturnedData = responseObj.returnedData;
                dealResult.AskTime = DateTime.ParseExact(responseObj.ackTime, "yyyyMMdd HH:mm:ss fff", new CultureInfo("en-US"),
                                            DateTimeStyles.None);
            }
            if (!isOk)
            {
                Logger.Error("JavaFillEdit", strError);
                dealResult.ErrorMessage = responseObj.errorMessage;
                dealResult.ResultMessage = "FAILED";

                return false;
            }
            return true;
        }

        public bool JavaClickButton(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType,
            string strAttachInfo, string pegName, string objName,
            ref string strError, ref MARSDealResult dealResult)
        {
            Logger.logBegin("JavaClickButton", $"{strKeyword}|{stepId}");
            /// 1, make sure that the attach is done
            /// 2, if not attached, then attach to the jvm and connect to webSocket
            /// 3, sent command to jvm webSocket
            /// 4, wait until the webSocket return
            /// 5, based on returned JSON, and return test result

            /// 1, make sure that the attach is done
            /// 
            var javaSocketClient = MarsJavaWebSocketClient.GetJavaWebSocketClient();
            if (javaSocketClient == null)
            {
                throw new NotImplementedException("attach to exists java is not implements");
            }
            string stepJson = javaObgAgentMgr.CreateObjectStringToJavaEngine(dictPegProperties, dictObjProperties, pegName,
                objName, strData, strParaMeter, strKeyword, null, (int)stepId, true);
            /// 3, sent command to jvm webSocket
            /// 
            bool isOk = false;
            var responseObj = javaSocketClient.sendRequestAndGetReponse(javaObgAgentMgr.currentTestStepRequest, stepJson, ref isOk, ref strError);
            if (responseObj == null)
            {
                isOk = false;
                dealResult.AskTime = DateTime.Now; //
            }
            else
            {
                dealResult.ResultMessage = responseObj.testResult;
                dealResult.ReturnedData = responseObj.returnedData;
                dealResult.AskTime = DateTime.ParseExact(responseObj.ackTime, "yyyyMMdd HH:mm:ss fff", new CultureInfo("en-US"),
                                            DateTimeStyles.None);
            }
            if (!isOk)
            {
                Logger.Error("JavaClickButton", strError);
                dealResult.ErrorMessage = responseObj.errorMessage;
                dealResult.ResultMessage = "FAILED";

                return false;
            }
            return true;
        }

        public bool JavaSelectListItem(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType,
            string strAttachInfo, string pegName, string objName,
            ref string strError, ref MARSDealResult dealResult)
        {
            Logger.logBegin("JavaSelectListItem", $"{strKeyword}|{stepId}");
            /// 1, make sure that the attach is done
            /// 2, if not attached, then attach to the jvm and connect to webSocket
            /// 3, sent command to jvm webSocket
            /// 4, wait until the webSocket return
            /// 5, based on returned JSON, and return test result

            /// 1, make sure that the attach is done
            /// 
            var javaSocketClient = MarsJavaWebSocketClient.GetJavaWebSocketClient();
            if (javaSocketClient == null)
            {
                throw new NotImplementedException("attach to exists java is not implements");
            }
            string stepJson = javaObgAgentMgr.CreateObjectStringToJavaEngine(dictPegProperties, dictObjProperties, pegName,
                objName, strData, strParaMeter, strKeyword, null, (int)stepId, true);
            /// 3, sent command to jvm websocket
            /// 
            bool isOk = false;
            var responseObj = javaSocketClient.sendRequestAndGetReponse(javaObgAgentMgr.currentTestStepRequest, stepJson, ref isOk, ref strError);
            if (responseObj == null)
            {
                isOk = false;
                dealResult.AskTime = DateTime.Now; //
            }
            else
            {
                dealResult.ResultMessage = responseObj.testResult;
                dealResult.ReturnedData = responseObj.returnedData;
                dealResult.AskTime = DateTime.ParseExact(responseObj.ackTime, "yyyyMMdd HH:mm:ss fff", new CultureInfo("en-US"),
                                            DateTimeStyles.None);
            }
            if (!isOk)
            {
                Logger.Error("JavaSelectListItem", strError);
                dealResult.ErrorMessage = responseObj.errorMessage;
                dealResult.ResultMessage = "FAILED";

                return false;
            }
            return true;
        }



        public bool JavaPegWindow(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType,
            string strAttachInfo, string pegName, string objName,
            ref string strError, ref MARSDealResult dealResult)
        {
            /// 1, make sure that the attach is done
            /// 2, if not attached, then attach to the jvm and connect to webSocket
            /// 3, sent command to jvm webSocket
            /// 4, wait until the webSocket return
            /// 5, based on returned JSON, and return test result

            /// 1, make sure that the attach is done
            /// 
            var javaSocketClient = MarsJavaWebSocketClient.GetJavaWebSocketClient();
            if (javaSocketClient == null)
            {
                 throw new NotImplementedException("attach to exists java is not implements");
            }            
            string stepJson = javaObgAgentMgr.CreateObjectStringToJavaEngine(dictPegProperties, dictObjProperties, pegName,
                objName, strData, strParaMeter, strKeyword, null, (int)stepId,true);
            /// 3, sent command to jvm websocket
            /// 
            bool isOk = false;            
            var responseObj = javaSocketClient.sendRequestAndGetReponse(javaObgAgentMgr.currentTestStepRequest, stepJson, ref isOk, ref strError);
            if (responseObj == null)
            {
                isOk = false;
                dealResult.AskTime = DateTime.Now; //
            }
            else
            {
                dealResult.ResultMessage = responseObj.testResult;
                dealResult.ReturnedData = responseObj.returnedData;
                dealResult.AskTime = DateTime.ParseExact(responseObj.ackTime, "yyyyMMdd HH:mm:ss fff", new CultureInfo("en-US"),
                                            DateTimeStyles.None);
            }
            if (!isOk)
            {
                Logger.Error("JavaPegWindow", strError);
                dealResult.ErrorMessage = responseObj.errorMessage;
                dealResult.ResultMessage = "FAILED";
                
                return false;
            }
            return true;
        }
    }
}
