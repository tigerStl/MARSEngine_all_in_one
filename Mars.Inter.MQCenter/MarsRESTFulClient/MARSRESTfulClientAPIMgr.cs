using Mars.message.Inter.MQCenter.simpleLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.MarsRESTFulClient
{
    public class MarsRestFulCorePortSwap
    {
        public const string cnst_swp_fileName = "marscoreswap.json";
        public int port { get; set; }
    }

    public class MARSRESTfulClientAPIMgr
    {
        private static MARSRESTfulClientAPIMgr gInst = null;
        public int RESTfulPort = -1;
        private static string baseUrl = "http://localhost";
        private HttpClient httpClient = null;
        /// <summary>
        /// 从MarsCore目录下marscoreswap.json
        /// </summary>
        /// <returns></returns>
        public static MARSRESTfulClientAPIMgr GetInst(ref bool isOk, ref string strError)
        {
            if (gInst == null)
            {
                int p = GetPortFromSwapFile();
                if (p < 0)
                {
                    strError = "Can't open swap file";
                    isOk = false;
                    return null;
                }
                MARSRESTfulClientAPIMgr tmpClient = new MARSRESTfulClientAPIMgr();
                tmpClient.RESTfulPort = p;
                gInst = tmpClient;
                
            }
            isOk = true ;
            return gInst;
        }

        /// <summary>
        // 从MarsCore目录下marscoreswap.json
        /// </summary>
        /// <returns></returns>
        private static int GetPortFromSwapFile()
        {
            string pth = typeof(MARSRESTfulClientAPIMgr).Assembly.Location;
            pth = System.IO.Path.GetDirectoryName(pth);
            string strSwpFile = System.IO.Path.Combine(pth, "MarsCore\\data", MarsRestFulCorePortSwap.cnst_swp_fileName);
            if (!System.IO.File.Exists(strSwpFile)) {
                return -1;
            }
            try
            {
                string txt = System.IO.File.ReadAllText(strSwpFile);
                var swpFile = System.Text.Json.JsonSerializer.Deserialize<MarsRestFulCorePortSwap>(txt);
                return swpFile.port;
            }
            catch (Exception e) {
                MarsLoggerSimple.Error("GetPortFromSwapFile", e.Message, e);
                return -1;
            }
        }

        private MARSRESTfulClientAPIMgr()
        {
            //httpClient = new HttpClient();
            
        }

        internal string RESTfulApiExecuteStep(string strContent,int waitSeconds, ref bool isOk, ref string strError)
        {
            MarsLoggerSimple.logBegin("RESTfulApiExecuteStep", $"port|{RESTfulPort}|content|{strContent}");
            try
            {
                string strUrl = $"{baseUrl}:{RESTfulPort}{MarsEngine.MarsSocketSvc.MarsSocketSvcConstant.request_execute_test_step}";
                
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(waitSeconds); //设置为3分钟
                    StringContent content = new StringContent(strContent, Encoding.UTF8, "application/xml");
                    var rspsn = client.PostAsync(strUrl, content).GetAwaiter().GetResult();
                    if (rspsn != null)
                    {
                        var result = rspsn.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        isOk = true;
                        return result;
                    }
                    else {
                        strError = $"can't get data from {strUrl}";
                        MarsLoggerSimple.Error("RESTfulApiExecuteStep", strError);
                        isOk = true;
                        return null;
                    }
                }
            }
            catch (Exception e) {
                MarsLoggerSimple.Error("RESTfulApiExecuteStep", strError = e.Message, e);
                isOk = false;
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("RESTfulApiExecuteStep");
            }
        }
    }
}
