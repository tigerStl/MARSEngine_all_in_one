using RestSharp;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.CompareGUI.Utilities
{
    public class MarsRestClient
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsRestClient));

        public string BaseUri { get; set; }
        private RestClient restClient = null;
        public MarsRestClient(string baseUri)
        {
            this.BaseUri = baseUri;
        }

        public string InvokeBackWhenCompareIsDone(string uuid, string fileName,string ErrorMessage, string db,bool isCompareSuccess, ref bool isOk, 
            ref string strError)
        {
            Logger.logBegin("InvokeBackWhenCompareIsDone", $"{uuid}|{isCompareSuccess}|fileName|{fileName}|msg|{ErrorMessage}|db is|{db}|");
            try
            {
                string targetUri = $"MarsReport/RunCompareCallBack";
                /// api side
                /// public JsonResult RunCompareCallBack(string ExcuteId, string fileName,string errorMessage)

                restClient = new RestClient(BaseUri);
                var request = new RestRequest(targetUri, Method.Post);
                
                request.AddQueryParameter("ExecuteId", uuid);
                request.AddQueryParameter("Status", isCompareSuccess);
                request.AddQueryParameter("message", ErrorMessage);
                request.AddQueryParameter("fileName", fileName);
                request.AddQueryParameter("schema", db);
                var url = restClient.BuildUri(request).ToString();

                var response = restClient.Execute(request,Method.Post);
                if (response == null)
                {
                    Logger.Error("InvokeBackWhenCompareIsDone", strError = $"No reponse from server, Please make sure that the server is accessable|\r\n{url}");
                    isOk = false;
                    return null;
                }
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    Logger.Info("InvokeBackWhenCompareIsDone", strError = $"{url}|return|{response.StatusCode}");
                    isOk = false;
                    return null;
                }                
                isOk = true;
                Logger.Info("InvokeBackWhenCompareIsDone", $"{url}|returns|{response.StatusCode}");
                return response.Content;
            }catch (Exception ex)
            {
                strError = $"can't send compare status to MARS web, please check Log for details.";
                isOk = false;
                Logger.Error("InvokeBackWhenCompareIsDone", $"Exception|{ex.Message}|{ex.StackTrace}");
                return null;
            }
            finally
            {
                Logger.logEnd("InvokeBackWhenCompareIsDone", $"isOk|{isOk}|{strError}|");
            }
        } 
    }
}
