using Mars.DataLayer;
using Mars.Model;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Mars.MarsConfig;
using Mars.SimpleLogger;
using log4net;
using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace DomUtil
{

    public class ResultModel
    {
        public ResultModel()
        {
            status = 0;
        }

        public ResultModel(int status)
        {
            this.status = status;
        }

        public int status { get; set; }
        public string message { get; set; }
        public dynamic data { get; set; }
        public dynamic data2 { get; set; }
    }


    public class RequestBody
    {
        public string schema { get; set; }
        public string id { get; set; }
        public string data { get; set; }
        public short dataType { get; set; }

    }

    


[Serializable]
    public class PostResponseException : Exception
    {
        public PostResponseException() : base() { }
        public PostResponseException(string message) : base(message) { }
        public PostResponseException(string message, Exception inner) : base(message, inner) { }
    }

    public static class WebApiDomHelper
    {
        public static int COMPARE_TYPE = 1;
        public static int CONN_TYPE = 2;
        public static int QUERY_TYPE = 3;
        public static int PROFILE_TYPE = 4;

        public static string BASE_URL = @"http://localhost:8051/";
        public static string API_OPTION = "OLD";
        public static string API_SCHEMA = null;//"GEN_MARS_5";// must be set before invoke RefreshXmlDoc when invoke from web

        private static ILog logger=LogManager.GetLogger("DATACOMPARE");

        public static XmlDocument doc = null;
        public static void RefreshXmlDoc()
        {
            doc = null;
            doc = ReadXmlDoc(API_SCHEMA);
        }

        public static XmlDocument ReadXmlDoc(string strDbIdx="")
        {
            logger.Info($"ReadXmlDoc\tMarsConfigLogger Started|{strDbIdx}");
            MarsConfig mc = MarsConfig.Configure("DEV");
            BASE_URL = mc.GetApiUrl();
            API_OPTION = mc.GetApiOption();
            if (string.IsNullOrEmpty(strDbIdx))
                API_SCHEMA = mc.GetApiSchema();
            else
                API_SCHEMA = strDbIdx;
            if (logger == null)
                logger = new Mars.SimpleLogger.SimpleLogger().Setup(mc.GetLoggerPath() + @"\DataCompare.log", System.AppDomain.CurrentDomain.FriendlyName);
            
            logger.Info("MarsConfigLogger Started");

            if (doc != null && doc.FirstChild != null)
                return doc;

            doc = new XmlDocument();
            StringBuilder builder = new StringBuilder();
            string xmlString;

            List<T_DATA_SOURCE> data = GetDataSourceData();

            List<string> compareData = (from d in data where d.DATA_SOURCE_TYPE == COMPARE_TYPE select d.DETAILS).ToList();
            List<string> connData = (from d in data where d.DATA_SOURCE_TYPE == CONN_TYPE select d.DETAILS).ToList();
            List<string> queryData = (from d in data where d.DATA_SOURCE_TYPE == QUERY_TYPE select d.DETAILS).ToList();
            List<string> profileData = (from d in data where d.DATA_SOURCE_TYPE == PROFILE_TYPE select d.DETAILS).ToList();

            // assemble the xml
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            builder.Append("<configuration>");

            // Compares
            builder.Append("<Compares>");
            foreach (string s in compareData)
                builder.Append(s);
            builder.Append("</Compares>");

            // Connections
            builder.Append("<Connections>");
            foreach (string s in connData)
                builder.Append(s);
            builder.Append("</Connections>");

            // Queries
            builder.Append("<Queries>");
            foreach (string s in queryData)
                builder.Append(s);
            builder.Append("</Queries>");

            // Profiles
            builder.Append("<Profiles>");
            foreach (string s in profileData)
                builder.Append(s);
            builder.Append("</Profiles>");


            builder.Append("</configuration>");

            xmlString = builder.ToString();

            xmlString = xmlString.Replace("ExtTradeId_1 <", "ExtTradeId_1 =").Replace("&", "&amp;");// & need format

            doc.LoadXml(xmlString);
            logger.Debug($"ReadXmlDoc\tend\tHas loaded dataource|rows|{data.Count}");
            return doc;
        }

        private static List<T_DATA_SOURCE> GetDataSourceData()
        {
            logger.Info($"GetDataSourceData\tbegin, |api_schema|{API_SCHEMA}|");
            List<T_DATA_SOURCE> list = new List<T_DATA_SOURCE>();

            try
            {
                RestClient client = new RestClient(BASE_URL);
                RestRequest request = null;
                if (API_OPTION == "NEW")
                   request = new RestRequest("api/CompareParam/ListCompareConfig?schema=" + API_SCHEMA, Method.Get);
                else
                   request = new RestRequest("api/ListCompareConfig", Method.Get);
                // 获取完整的URL
                var fullUrl = client.BuildUri(request);
                logger.Info($"GetDataSourceData\tURL|{fullUrl}");
                RestResponse<DataCompareResponce> response = (RestResponse<DataCompareResponce>)client.Execute<DataCompareResponce>(request);
                var ll = response.Content;
                logger.Info($"GetDataSourceData\tContent|{ll}");
                bool testing = false;
                if (testing)
                {
                    ll = File.ReadAllText(@"C:\temp\MarsDataCompareList.txt");
                }
                DataCompareResponce resp = JsonConvert.DeserializeObject<DataCompareResponce>(ll);
                list = resp.data;
            }
            catch (Exception e)
            {
                logger.Error("GetDataSourceData: fullUrl:" + e.Message, e);
            }
            return list;
        }

        public static void DeleteXmlNode(XmlNode node)
        {
            string data = node.OuterXml;
            string name = node.Name;
            string id = node.Attributes["ID"].Value;
            short dataType = -1;
            switch (name)
            {
                case "Compare":
                    dataType = (short)COMPARE_TYPE;
                    break;
                case "Query":
                    dataType = (short)QUERY_TYPE;
                    break;
                case "DBConn":
                    dataType = (short)CONN_TYPE;
                    break;
                case "Profile":
                    dataType = (short)PROFILE_TYPE;
                    break;
            }
            DeleteDataSource(id, dataType);
            RefreshXmlDoc();
        }

        private static void DeleteDataSource(string id, short dataType)
        {
            // POST api/DeleteCompareconfig?id={id}&datatype={datatype}

            RestClient client = new RestClient(BASE_URL);
            RestRequest request = null;

            if (API_OPTION == "NEW")
                request = new RestRequest("api/CompareParam/DeleteCompareconfig?schema=" + API_SCHEMA, Method.Post);
            else
                request = new RestRequest("api/DeleteCompareconfig", Method.Post);
            request.AddParameter("id", id, ParameterType.QueryString);
            request.AddParameter("datatype", dataType, ParameterType.QueryString);
            var response = client.Execute(request);
            var fullUrl = client.BuildUri(request);

            logger.Info("DeleteDataSource: id:" + id);
            logger.Info("DeleteDataSource: dataType:" + dataType);
            logger.Info("DeleteDataSource: fullUrl:" + fullUrl);
        }

        public static bool UpdateXmlDoc(XmlNode node,ref string strError)
        {
            string data = node.OuterXml;
            string name = node.Name;
            string id = node.Attributes["ID"].Value;
            short dataType = -1;

            switch (name)
            {
                case "Compare":
                    dataType = (short)COMPARE_TYPE;
                    break;

                case "Query":
                    dataType = (short)QUERY_TYPE;
                    data = WebUtility.HtmlDecode(data);
                    break;

                case "DBConn":
                    dataType = (short)CONN_TYPE;
                    break;

                case "Profile":
                    dataType = (short)PROFILE_TYPE;
                    break;
            }

            return UpdateDataSource(id, data, dataType,ref strError);
            // AF: looks like this is not needed         RefreshXmlDoc();
        }


        private static bool UpdateDataSource(string id, string data, short dataType, ref string strError)
        {
            bool REQUEST_BY_BODY = false;
            if (REQUEST_BY_BODY == true)
            {
                UpdateDataSourceByBody(id, data, dataType);
                return true;
            }
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = null;
            //RestRequest request = new RestRequest("api/AddoreditCompareConfig", Method.POST);

            if (API_OPTION == "NEW")
                request = new RestRequest("api/CompareParam/AddoreditCompareConfig?schema=" + API_SCHEMA, Method.Post);
            else
                request = new RestRequest("api/AddoreditCompareConfig", Method.Post);
            // client.ExecutePostTaskAsync()
            request.AddParameter("id", id, ParameterType.QueryString);
            request.AddParameter("data", data, ParameterType.QueryString);
            request.AddParameter("datatype", dataType, ParameterType.QueryString);
            try
            {
                var response = client.Execute(request);
                var fullUrl = client.BuildUri(request);
                if (response.StatusDescription != "OK")
                {
                    throw new PostResponseException("StatusDescription:" + response.StatusDescription);
                }


                // var fullUrl = client.BuildUri(request);

                logger.Info("UpdateDataSource: id:" + id);
                logger.Info("UpdateDataSource: dataType:" + dataType);
                logger.Info("UpdateDataSource: data:" + data);
                logger.Info("UpdateDataSource: fullUrl:" + fullUrl);
                return true;
            }
            catch (Exception e) { 
                strError = e.ToString();
                logger.Error(strError,e);
                return false;
            }
        }

        private static void UpdateDataSourceByBody(string id, string data, short dataType)
        {
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = null;
            //RestRequest request = new RestRequest("api/AddoreditCompareConfig", Method.POST);

            if (API_OPTION == "NEW")
            //request = new RestRequest("api/CompareParam/AddoreditCompareConfig?schema=" + API_SCHEMA, Method.POST);

            {
                request = new RestRequest("api/CompareParam/AddoreditCompareConfig", Method.Post);
                var requestBody = new
                {
                    schema = API_SCHEMA,
                    id = id,
                    data = data,
                    dataType = dataType
                };
                try
                {
                    //request.AddJBody(requestBody);
                    request.AddBody(requestBody);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}|{ex.StackTrace}");
                }
            }


            else
                request = new RestRequest("api/AddoreditCompareConfig", Method.Post);
            // client.ExecutePostTaskAsync()
            request.AddParameter("id", id, ParameterType.QueryString);
            request.AddParameter("data", data, ParameterType.QueryString);
            request.AddParameter("datatype", dataType, ParameterType.QueryString);
            var response = client.Execute(request);
            var fullUrl = client.BuildUri(request);

            logger.Info("DeleteDataSource: id:" + id);
            logger.Info("DeleteDataSource: dataType:" + dataType);
            logger.Info("DeleteDataSource: data:" + data);
            logger.Info("DeleteDataSource: fullUrl:" + fullUrl);

        }

        public static void SaveXmlDoc(XmlDocument doc)
        {

        }

        private static bool UpdateDataSourceNew(string id, string data, short dataType, ref string strError)
        {
            bool REQUEST_BY_BODY = false;
            //if (REQUEST_BY_BODY == true)
            //{
            //    UpdateDataSourceByBody(id, data, dataType);
            //    return;
            //}
            RestClient client = new RestClient(BASE_URL);
            RestRequest request = null;
            //RestRequest request = new RestRequest("api/AddoreditCompareConfig", Method.Post);
            bool isOk = false;
            if (API_OPTION == "NEW")
                request = new RestRequest("api/CompareParam/AddoreditCompareConfigX",
                    //?schema=" + API_SCHEMA, " +
                    Method.Post);
            else
                request = new RestRequest("api/AddoreditCompareConfig", Method.Post);
            // client.ExecutePostTaskAsync()


            /*
            var requestBody = new RequestBody();
            requestBody.schema = API_SCHEMA;
            requestBody.id = id;
            //requestBody.data = data;
            requestBody.data = System.Net.WebUtility.HtmlEncode(data);
            requestBody.dataType = dataType;

            //
            request.RequestFormat = DataFormat.Json;
            //
            request.AddBody(requestBody);
            */


            var requestBody = new
            {
                schema = API_SCHEMA,
                id = id,
                data = data,
                dataType = dataType
            };
            request.RequestFormat = DataFormat.Json;
            request.AddBody(requestBody);



            //request.AddParameter("id", id, ParameterType.QueryString);
            //request.AddParameter("data", data, ParameterType.QueryString);
            //request.AddParameter("datatype", dataType, ParameterType.QueryString);

            var fullUrl = client.BuildUri(request);
            logger.Info("UpdateDataSource: id:" + id);
            logger.Info("UpdateDataSource: dataType:" + dataType);
            logger.Info("UpdateDataSource: data:" + data);
            logger.Info("UpdateDataSource: fullUrl:" + fullUrl);
            logger.Info($"UpdateDataSource: datacontent:|{requestBody}");
            var response = client.Execute(request);
            try
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // var rslt = System.Text.Json.JsonSerializer.Deserialize<ResultModel>(response.Content);
                    var rslt = JsonConvert.DeserializeObject<ResultModel>(response.Content);

                    if (rslt == null)
                    {
                        strError = $"server side doesn't return right data, check network or services(and servies version)";
                        return isOk = false;
                    }
                    if (rslt.status == 1)
                    {
                        return isOk = true;
                    }
                    else
                    {
                        strError = rslt.message;
                        return isOk = false;
                    }
                }
                else
                {
                    strError = $"serivce returns code|{response.StatusCode}";
                    return isOk = false;
                }
            }
            finally
            {
                logger.Info($"UpdateDataSource\tend|returns|{isOk}|strError|{strError}|");
            }


        }

    }
}
