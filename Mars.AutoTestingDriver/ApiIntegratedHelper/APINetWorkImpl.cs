using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mars.message.Utility;
using static Mars.AutoTestingDriver.ApiIntegratedHelper.APIEngineHelper;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.AutoTestingDriver.ApiIntegratedHelper
{
    /// <summary>
    /// API Network Implementation class for sending HTTP requests
    /// </summary>
    public class APINetWorkImpl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(APINetWorkImpl));
        private static Random random = new Random();
        
        /// <summary>
        /// Generates a random trace ID (8 digits)
        /// </summary>
        /// <returns>8-digit random string</returns>
        private static string GenerateTraceId()
        {
            return random.Next(10000000, 99999999).ToString();
        }
        /// <summary>
        /// API Response class containing status code and response body
        /// </summary>
        public class APIResponse
        {
            public int StatusCode { get; set; }
            public string ResponseBody { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; }

            public APIResponse()
            {
                Headers = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Sends an API request based on the JSON configuration
        /// </summary>
        /// <param name="jsonObject">The JSON configuration object</param>
        /// <param name="url">The complete URL to call</param>
        /// <param name="strError">Error message output</param>
        /// <returns>APIResponse object containing the response</returns>
        public static APIResponse SendRequest(JObject jsonObject, string url, ref string strError)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("SendRequest", string.Format("{0}|URL:[{1}]", traceId, url));
            
            APIResponse response = new APIResponse();

            try
            {
                if (jsonObject == null)
                {
                    strError = "JSON object is null";
                    response.IsSuccess = false;
                    response.ErrorMessage = strError;
                    Logger.Error("SendRequest", string.Format("{0}|JSON object is null", traceId), Environment.StackTrace);
                    Logger.logEnd("SendRequest");
                    return response;
                }

                // Get HTTP verb (method)
                string verb = jsonObject[CNST_JSON_PROP_VERB]?.ToString() ?? "GET";
                if (string.IsNullOrEmpty(verb))
                {
                    verb = "GET";
                }
                
                Logger.Info("SendRequest", string.Format("{0}|HTTP Method:[{1}]", traceId, verb));

                // Get headers
                Dictionary<string, string> headers = new Dictionary<string, string>();
                JToken headersToken = jsonObject[CNST_JSON_PROP_HEADERS];
                if (headersToken != null)
                {
                    if (headersToken is JObject headersObj)
                    {
                        foreach (var prop in headersObj.Properties())
                        {
                            headers[prop.Name] = prop.Value?.ToString() ?? "";
                        }
                    }
                    else if (headersToken is JArray headersArray)
                    {
                        foreach (JObject header in headersArray)
                        {
                            string key = header[CNST_ARRAY_PROP_KEY]?.ToString() ?? "";
                            string value = header[CNST_ARRAY_PROP_VALUE]?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(key))
                            {
                                headers[key] = value;
                            }
                        }
                    }
                }

                // Get request body
                string requestBody = jsonObject[CNST_JSON_PROP_BODY]?.ToString() ?? "";
                Logger.Info("SendRequest", string.Format("{0}|Request body length:[{1}], Headers count:[{2}]", 
                    traceId, string.IsNullOrEmpty(requestBody) ? 0 : requestBody.Length, headers.Count));

                // Send HTTP request
                using (HttpClient client = new HttpClient())
                {
                    // Add headers to HttpClient
                    foreach (var header in headers)
                    {
                        try
                        {
                            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                            {
                                // Content-Type will be set with content
                                continue;
                            }
                            else if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                            {
                                client.DefaultRequestHeaders.Accept.Clear();
                                client.DefaultRequestHeaders.Accept.Add(
                                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(header.Value));
                            }
                            else
                            {
                                client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Some headers cannot be added this way, skip them
                            Logger.Info("SendRequest", string.Format("{0}|Cannot add header [{1}]: {2}", traceId, header.Key, ex.Message));
                            System.Diagnostics.Debug.WriteLine($"Cannot add header {header.Key}: {ex.Message}");
                        }
                    }

                    // Create request message
                    HttpMethod method = new HttpMethod(verb.ToUpper());
                    HttpRequestMessage request = new HttpRequestMessage(method, url);

                    // Set content if body exists and method supports it
                    if (!string.IsNullOrEmpty(requestBody) && 
                        (verb.ToUpper() == "POST" || verb.ToUpper() == "PUT" || verb.ToUpper() == "PATCH"))
                    {
                        // Determine content type
                        string contentType = "application/json";
                        if (headers.ContainsKey("Content-Type"))
                        {
                            contentType = headers["Content-Type"];
                        }

                        request.Content = new StringContent(requestBody, Encoding.UTF8, contentType);
                    }

                    // Send request synchronously
                    Task<HttpResponseMessage> responseTask = client.SendAsync(request);
                    responseTask.Wait();
                    HttpResponseMessage httpResponse = responseTask.Result;

                    // Get response
                    response.StatusCode = (int)httpResponse.StatusCode;
                    response.IsSuccess = httpResponse.IsSuccessStatusCode;

                    Logger.Info("SendRequest", string.Format("{0}|Response StatusCode:[{1}], IsSuccess:[{2}]", 
                        traceId, response.StatusCode, response.IsSuccess));

                    // Get response headers
                    foreach (var header in httpResponse.Headers)
                    {
                        response.Headers[header.Key] = string.Join(", ", header.Value);
                    }

                    // Get response body
                    Task<string> bodyTask = httpResponse.Content.ReadAsStringAsync();
                    bodyTask.Wait();
                    response.ResponseBody = bodyTask.Result;

                    Logger.Info("SendRequest", string.Format("{0}|Response body length:[{1}]", 
                        traceId, string.IsNullOrEmpty(response.ResponseBody) ? 0 : response.ResponseBody.Length));

                    if (!response.IsSuccess)
                    {
                        response.ErrorMessage = $"HTTP {response.StatusCode}: {response.ResponseBody}";
                        strError = response.ErrorMessage;
                        Logger.Error("SendRequest", string.Format("{0}|HTTP Error: {1}", traceId, response.ErrorMessage), 
                            string.IsNullOrEmpty(response.ResponseBody) ? "" : response.ResponseBody);
                    }
                }
                
                Logger.logEnd("SendRequest", string.Format("{0}|Request completed, StatusCode:[{1}], IsSuccess:[{2}]", 
                    traceId, response.StatusCode, response.IsSuccess));
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                strError = $"Exception sending API request: {ex.Message}";
                response.StatusCode = 0;
                Logger.Error("SendRequest", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("SendRequest");
            }

            return response;
        }

        /// <summary>
        /// Sends an API request based on the JSON configuration - overload for string
        /// </summary>
        /// <param name="jsonConfig">The JSON configuration string</param>
        /// <param name="url">The complete URL to call</param>
        /// <param name="strError">Error message output</param>
        /// <returns>APIResponse object containing the response</returns>
        public static APIResponse SendRequest(string jsonConfig, string url, ref string strError)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("SendRequest", string.Format("{0}|URL:[{1}], JSON config length:[{2}]", traceId, url, 
                string.IsNullOrEmpty(jsonConfig) ? 0 : jsonConfig.Length));

            try
            {
                JObject jsonObject = JObject.Parse(jsonConfig);
                APIResponse result = SendRequest(jsonObject, url, ref strError);
                Logger.logEnd("SendRequest", string.Format("{0}|Request completed via string overload", traceId));
                return result;
            }
            catch (Exception ex)
            {
                APIResponse response = new APIResponse();
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                strError = $"Exception parsing JSON: {ex.Message}";
                response.StatusCode = 0;
                Logger.Error("SendRequest", string.Format("{0}|Exception parsing JSON: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("SendRequest");
                return response;
            }
        }
    }
}

