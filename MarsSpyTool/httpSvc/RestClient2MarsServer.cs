using Mars.message.Inter.MQCenter.HttpRestService;
using MarsSpyTool.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.httpSvc
{
    internal class RestClient2MarsServer
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("MarsSpyLog");

        public bool sendRecgObjectsToServer(string strContent, ref string strError)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin");
            //int iStepCount = recordedStps == null ? 0 : recordedStps.Count;
            string strUrl = $"{MarsGlobalData.currentRemoteServerWithAddress}ObjectTool/ObjectContent";
            try
            {
                System.Net.Http.HttpClient clnt = new System.Net.Http.HttpClient();
                var para = new Dictionary<string, string>();
                para.Add("uuid", MarsGlobalData.currentUUIDFromWeb);
                //string strCtnt = JsonConvert.SerializeObject(recordedStps);
                logger.Trace($"{iMark}|test steps send to web|{strContent}");
                para.Add("content", strContent);
                var encodedContent = new FormUrlEncodedContent(para);
                var rspns = clnt.PostAsync(strUrl, encodedContent).GetAwaiter().GetResult();
                if (rspns.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    strError = $"{strUrl} returns {rspns.StatusCode}";
                    return false;
                }
                logger.Info($"{iMark}|{strUrl}|returns|{rspns.StatusCode}");
                return true;
            }
            catch (Exception e)
            {
                logger.Error(e, strError = $"{iMark}|{e.Message}|\r\n{e.StackTrace}");
                return false;
            }
            finally
            {
                logger.Info($"{iMark}|End");
            }
        }

        public bool sendTestCaseRecordToServer(List<MarsRecordReplayStep> recordedStps,ref string strError)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin");
            int iStepCount = recordedStps == null ? 0 : recordedStps.Count;
            string strUrl = $"{MarsGlobalData.currentRemoteServerWithAddress}ObjectTool/ObjectToolContent";
            try
            {
                System.Net.Http.HttpClient clnt = new System.Net.Http.HttpClient();
                var para = new Dictionary<string, string>();
                para.Add("uuid", MarsGlobalData.currentUUIDFromWeb);
                string strCtnt = JsonConvert.SerializeObject(recordedStps);
                logger.Trace($"{iMark}|test steps send to web|{strCtnt}");
                para.Add("content", strCtnt);
                var encodedContent = new FormUrlEncodedContent(para);
                var rspns = clnt.PostAsync(strUrl,encodedContent).GetAwaiter().GetResult();
                if (rspns.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    strError = $"{strUrl} returns {rspns.StatusCode}";
                    return false;
                }
                logger.Info($"{iMark}|{strUrl}|returns|{rspns.StatusCode}");
                return true;
            }
            catch(Exception e)
            {
                logger.Error(e, strError = $"{iMark}|{e.Message}|\r\n{e.StackTrace}");
                return false;
            }
            finally
            {
                logger.Info($"{iMark}|End");
            }
        }
    }
}
