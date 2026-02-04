using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.utilities.notifiy;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AI.NLP.Inter.restClient
{
    public class Diction_Phrase_Query
    {
        public string? phrase { get; set; }
        public string? query_id { get; set; }

        public override string ToString()
        {
            return $"{query_id}|{phrase}";
        }
    }


    public class Dicationary_Phrase_Query_ResponseItem : Diction_Phrase_Query
    {
        public string? status { get; set; }
        public string? message { get; set; }
        public List<DictionaryData>? obj { get; set; }
    }
    public class Dictionary_Phrase_QueryResponse
    {
        public string? result { get; set; } // OK or FAILED
        public string? message { get; set; } // error message if it has
        public List<Dicationary_Phrase_Query_ResponseItem>? obj { get; set; }
    }
    public class RESTClientToNLPServer
    {
        private static NLog.Logger log = NLog.LogManager.GetLogger(typeof(RESTClientToNLPServer).Name);

        private readonly RestClient _client;
        public static string NLP_API_SERVER_DOMAIN { get; set; }

        public RESTClientToNLPServer(string? baseUrl = null)
        {
            if (string.IsNullOrEmpty(baseUrl))
                _client = new RestClient(NLP_API_SERVER_DOMAIN);
            else
                _client = new RestClient(baseUrl);
        }

        public async Task<RestResponse> GetAsync(string resource)
        {
            var request = new RestRequest(resource, Method.Get);
            return await _client.ExecuteAsync(request);
        }

        public async Task<RestResponse> PostAsync(string resource, object body)
        {            
            var request = new RestRequest(resource, Method.Post);
            log.Info($"PostAsync\t|{request.Resource}");
            request.AddJsonBody(body);
            return await _client.ExecuteAsync(request);
        }

        public async Task<RestResponse> PutAsync(string resource, object body)
        {
            var request = new RestRequest(resource, Method.Put);
            request.AddJsonBody(body);
            return await _client.ExecuteAsync(request);
        }

        public async Task<RestResponse> DeleteAsync(string resource)
        {
            var request = new RestRequest(resource, Method.Delete);
            return await _client.ExecuteAsync(request);
        }

        // Example method to get data
        public async Task<string?> GetExampleDataAsync()
        {
            var response = await GetAsync("example/endpoint");
            if (response.IsSuccessful)
            {
                return response.Content;
            }
            else
            {
                throw new Exception("Error retrieving data: " + response.ErrorMessage);
            }
        }


        public AnalystText_Response? analystText(NLP_TextToAnalyst DataToAnalyst, ref string strError, ref bool isOk,
            NLP_AnalystTextCallback callBack = null)
        {
            log.Info($"analystText\t|{DataToAnalyst.currentText}");
            var req = new AnalystText_Request()
            {
                text = DataToAnalyst.currentText
            };
            isOk = true;
            try
            {
                if (callBack != null)
                {
                    callBack(new NLP_TextAnalystStatus()
                    {
                        query_id = DataToAnalyst.query_id,
                        currentText = "Prepare to Analsty.....",
                        isLastNotification = false
                    });
                }
                var rsp = PostAsync("analystTextToSentences", req).GetAwaiter().GetResult();
                AnalystText_Response? rslt = Newtonsoft.Json.JsonConvert.DeserializeObject<AnalystText_Response>(rsp.Content ?? "");
                isOk = true;
                return rslt;
            }
            catch (Exception ex)
            {
                log.Error(ex, $"analystText\tException|{ex.Message}");
                isOk = false;
                strError = ex.Message;
                return null;
            }
            finally
            {
                log.Info($"analystText\tEnd");
            }
        }

        public AnalystASetence_Response? analystSetence(string strData, ref string strError, ref bool isOk)
        {
            isOk = true;
            AnalystASetence_Request req = new AnalystASetence_Request()
            {
                sentence = strData,
            };
            string cnt = "";
            try
            {
                var rsp = PostAsync("sentenceAnalysis", req).GetAwaiter().GetResult();
                AnalystASetence_Response? rslt = Newtonsoft.Json.JsonConvert.DeserializeObject<AnalystASetence_Response>(rsp.Content ?? "");
                isOk = true;
                return rslt;
            }
            catch (Exception ex)
            {

                isOk = false;
                strError = ex.Message;
                return null;
            }
        }



        public List<DictionaryObject_forResponse>? lookupDictionaries(string[] strKey, ref string strError, ref bool isOk)
        {
            log.Info($"lookupDictionaries\t|{strKey}");
            isOk = true;
            DictionariesData_Request dictionary_Request = new DictionariesData_Request()
            {
                keynotes = strKey,
            };

            try
            {
                var rsp = PostAsync("lookupDictionaries", dictionary_Request).GetAwaiter().GetResult();
                DictionariesData_Response? DictionariesData = Newtonsoft.Json.JsonConvert.DeserializeObject<DictionariesData_Response>(rsp.Content ?? "");
                List<DictionaryObject_forResponse>? rslt = new List<DictionaryObject_forResponse>();
                if (DictionariesData != null)
                {
                    if (DictionariesData.result.Equals(MARSNLP_REST_API_message.cnst_response_FAILED))
                    {
                        isOk = false;
                        strError = $"Error from AI engine|{strError = DictionariesData.message}";
                        log.Error($"lookupDictionaries\t|{strError}");
                        return null;
                    }
                    else
                    {
                        isOk = true;
                        return DictionariesData.objs;
                    }
                }
                isOk = false;
                strError = $"can't lookup from dictionaries|{strKey}";
                return null;
            }
            catch (Exception ex)
            {
                isOk = false;
                log.Error($"lookupDictionary\t|{ex.Message}\r\n{ex.StackTrace}");
                strError = ex.Message;
                return null;
            }
        }
        /// <summary>
        /// 从python的RESTful api中查询字典
        /// </summary>
        /// <param name="strKey"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public List<DictionaryData>? lookupDictionary(string strKey, ref string strError, ref bool isOk)
        {
            log.Info($"lookupDictionary\t|{strKey}");
            isOk = true;
            DictionaryData_Request dictionary_Request = new DictionaryData_Request()
            {
                keynote = strKey,
            };

            try
            {
                var rsp = PostAsync("lookupDictionary", dictionary_Request).GetAwaiter().GetResult();
                DictionaryData_Response? arrDictionData = Newtonsoft.Json.JsonConvert.DeserializeObject<DictionaryData_Response>(rsp.Content ?? "");
                if (arrDictionData != null)
                {
                    if ((arrDictionData.dictionary != null)
                        && (arrDictionData.dictionary.Count > 0))
                    {
                        isOk = true;
                        return arrDictionData.dictionary;
                    }
                    isOk = false;
                    strError = $"No |{strKey}| exists in MARS Dictionary, make sure the item is added ";
                    log.Error($"lookupDictionary\t|{strError}");
                    return null;
                }
                isOk = false;
                strError = $"can't lookup from dictionary|{strKey}";
                return null;
            }
            catch (Exception ex)
            {
                isOk = false;
                log.Error($"lookupDictionary\t|{ex.Message}\r\n{ex.StackTrace}");
                strError = ex.Message;
                return null;
            }
        }


        internal Dictionary_Phrase_QueryResponse lookupDictionariesWithQueryIds(List<Diction_Phrase_Query> wordsToBeLookedUp, ref string strError, ref bool isOk)
        {
            log.Info($"lookupDictionariesWithQueryIds\tbegin");
            isOk = true;
            Dictionary_Phrase_QueryResponse result = new Dictionary_Phrase_QueryResponse();

            try
            {
                string strKeysToLookup = string.Join("|", wordsToBeLookedUp.Select(p => p.phrase));
                var rsp = PostAsync("lookupDictionariesWithQueryIds", wordsToBeLookedUp).GetAwaiter().GetResult();
                Dictionary_Phrase_QueryResponse? arrDictionData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary_Phrase_QueryResponse>(rsp.Content ?? "");
                if ((arrDictionData != null)
                    && ((arrDictionData.result.Equals(MARSNLP_REST_API_message.cnst_response_OK, StringComparison.OrdinalIgnoreCase)
                        || (arrDictionData.result.Equals(MARSNLP_REST_API_message.cnst_response_SUCCESS, StringComparison.OrdinalIgnoreCase)))))
                {
                    isOk = true;
                    return arrDictionData;
                }
                isOk = false;
                strError = $"can't lookup from dictionary|{strKeysToLookup}|" + (string.IsNullOrEmpty(strError) ? "" : strError);
                log.Error($"lookupDictionariesWithQueryIds\tError|{strError}");
                return null;
            }
            catch (Exception ex)
            {
                isOk = false;
                log.Error($"lookupDictionary\t|{ex.Message}\r\n{ex.StackTrace}");
                strError = ex.Message;
                return null;
            }
            finally
            {
                log.Info($"lookupDictionariesWithQueryIds\tEnd");
            }
        }
    }

}
