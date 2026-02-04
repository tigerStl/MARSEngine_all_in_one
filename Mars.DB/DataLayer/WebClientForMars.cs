//#if _forWebClient

using Mars.message.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.message.Business;
using Mars.message.Dto;
using MarsEngineSvc.basicReturnDataStructure;
using Newtonsoft.Json;

#if !_pythonInterface
using Route2NSEx.src.Marquis.systemUtil;
#else
using MARSPythonInterface.interface4PythonJson;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text;
using System.Web.UI.WebControls;
using System.Xml.Serialization;

#if _pythonInterface
using Logger = Mars.Inter.MQCenter.simpleLog.MarsLoggerSimple;
#endif

namespace Mars.message.DataLayer
{
    //public class WebClientForMars
    //{

    //}

    public class MarsRESTfulApiClient
    {
        private static string WebURLPrefix = System.Configuration.ConfigurationManager.AppSettings["MarsEngineSvc_url"];
        private string currentDBIdx = "";
        public MarsRESTfulApiClient(string strDBIdx)
        {
            Console.WriteLine($"WebURLPrefix--[{WebURLPrefix}]");
            currentDBIdx = strDBIdx;
        }

        public static void setWebURLPrefix(string urlPreFix)
        {
            WebURLPrefix = urlPreFix;
        }

        public string webURLPreFix
        {
            get => WebURLPrefix;
            set => WebURLPrefix = value;
        }
#if !_pythonInterface
        
        private static MLogger logger = MLogger.GetLogger(typeof(MarsRESTfulApiClient));
        private MLogger Logger
        {
            get => logger ?? (logger = MLogger.GetLogger(typeof(MarsRESTfulApiClient)));
        }
#endif
        protected string GetURLData(string strURLWithPara)
        {
            using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
            {
                var response = httpClient.GetAsync(strURLWithPara).GetAwaiter().GetResult();
                var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return result;
            }
        }

        public string getKeywordMappingInfo(ref string strError, ref bool isOk)
        {
            int iMark = new Random().Next();
            Logger.logBegin("getKeywordMappingInfo", $"{iMark}");
            try
            {
                string strData = GetURLData("api/EngineKeywords/GetKeywordMapping");
                isOk = true;
                return strData;
            }
            catch (Exception e)
            {
                Logger.Error("getKeywordMappingInfo", strError = e.Message, e );
                isOk = false;
                return null;
            }
        }

        public RESTfulReturnObjects GetDataFromURL(string strURLWithPara, ref bool isOk, ref string strError)
        {
            try
            {
                string strData = GetURLData(strURLWithPara);
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(RESTfulReturnObjects));
                MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(strData));

                RESTfulReturnObjects rslt = (RESTfulReturnObjects)serializer.ReadObject(ms);
                if (rslt == null)
                {
                    isOk = false;
                    strError = string.Format("Can't get data from [{0}]", strURLWithPara);
                    return null;
                }
                if ((RESTfulObjectType)rslt.objectType == RESTfulObjectType.error_obj)
                {
                    isOk = false;
                    strError = string.Format("[{0}] return Error, with Error message:[{1}]",
                        strURLWithPara,
                        rslt.ReturnedMessage);
                    return rslt;
                }
                isOk = true;
                return rslt;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception:[{0}]", e.Message);
                Logger.Error("GetDataFromURL", e.Message, e.StackTrace);
                return null;
            }
        }

        protected T DoPut<T>(string strURLPart, T objToSend, ref bool isOk, ref string strError, bool isBSon = false, bool isDebug = false)
        {
            Logger.logBegin("DoPut", string.Format("url:{0}, isBSon:{1} object is:{2}", strURLPart, isBSon, objToSend == null ? "N/A" : objToSend.ToString()));
            string strDataReturned = "";
            try
            {
                string strURL = BuildURL(strURLPart, ref isOk, ref strError);
                if (!isOk)
                {
                    return default(T);
                }

                System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
                HttpResponseMessage rsp = null;

                if (isBSon)
                {
                    logger.Info("DoPut", $"branch isBson|{isBSon}|set bason type|{strURL}");
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/bson"));
#if core_bson
                    string cntnt = JsonConvert.SerializeObject(objToSend);
                    System.Net.Http.Formatting.MediaTypeFormatter bsonFormatter = new System.Net.Http.Formatting.BsonMediaTypeFormatter();
                    byte[] b = Encoding.UTF8.GetBytes(cntnt);
                    var body = new ByteArrayContent(b);
                    body.Headers.ContentType = MediaTypeHeaderValue.Parse("application/bson");
                    rsp = httpClient.PostAsync($"{strURL}", body).GetAwaiter().GetResult();
#else
                    //MemoryStream ms = new MemoryStream();
                    //BinaryFormatter bf = new BinaryFormatter();
                    //bf.Serialize(ms, objToSend);
                    //ByteArrayContent dataToPut = new ByteArrayContent(ms.ToArray());

                    rsp = httpClient.PostAsync(strURL, objToSend, bsonFormatter).GetAwaiter().GetResult();
#endif
                    string strData = rsp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (rsp.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        strError = rsp.Content.ToString();
                        logger.Error("DoPut", $"{rsp.StatusCode}|{strData}|{strError}");
                        logger.Error("DoPut",strError);
                        isOk = false;
                        return default(T);
                    }
                    Logger.Info("DoPut", strData);
                    T rslt = DeserializeToObjectFromResponseString<T>(strData, ref isOk, ref strError);
                    if ((!isOk) || (rslt.Equals(default(T))))
                    {
                        isOk = false;
                        return default(T);
                    }
                    return rslt;
                }
                else
                {
                    //Logger.Error("doPut", "before Serialize");
                    string strJsonObj = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(objToSend);
                    if (isDebug)
                    {
                        Logger.Info("doPut", $"after JSon converted to:{strJsonObj}");
                    }
                    var httpContent = new StringContent(strJsonObj, Encoding.UTF8, "application/json");
                    if (isDebug)
                    {
                        Logger.Info("doPut", $"created StringContent:{httpClient}");
                    }
                    Logger.Info("doPut", "before PutAsync");
                    
                    rsp = httpClient.PostAsync(strURL, httpContent).GetAwaiter().GetResult();
                    Logger.Info("\t", $"ReadAsStringAsync before|{strURL}|{rsp.StatusCode}");
                    strDataReturned = rsp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    Logger.Info("\t", "DeserializeToObjectFromResponseString before");
                    T rslt = DeserializeToObjectFromResponseString<T>(strDataReturned, ref isOk, ref strError);
                    if ((!isOk) || (rslt.Equals(default(T))))
                    {
                        isOk = false;
                        strError = string.IsNullOrEmpty(strError) ? "No object is returned" : strError;
                        Logger.Error("doPut", $"isOk:{isOk}, {strError}");
                        return default(T);
                    }
                    return rslt;
                }
            }
            catch (Exception e)
            {
                Logger.Error("doPut", $"data returned:{strDataReturned}");
                Logger.Error("doPut", e.Message, e);
                strError = string.Format("Exception:[{0}]", e.Message);
                isOk = false;
                return default(T);
            }
            finally
            {
                Logger.logEnd("doPut", $"returns {isOk}");
            }
        }

        public static string[] GetAllDBIds(ref bool isOk, ref string strError)
        {
            
            try
            {
                logger.logBegin("GetAllDBIds");
                MarsRESTfulApiClient client = new MarsRESTfulApiClient("_NON_");
                RESTfulReturnAllDBIdsRequest rptRest = new RESTfulReturnAllDBIdsRequest();
                rptRest.objectType = (int)RESTfulObjectType._get_all_dbs_request;
                RESTfulReturnAllDBIdsRequest rslt = client.DoPut<RESTfulReturnAllDBIdsRequest>("MarsEngine/GetAllDBIdx", rptRest, ref isOk, ref strError);
                if (rslt.objectType != (int)RESTfulObjectType._get_all_dbs_response)
                {
                    logger.Error("GetAllDBIds", $"the object type is not |_get_all_dbs_response|{rslt.objectType}|");
                    isOk = false;
                    return null;
                }
                if (!rslt.isOk)
                {
                    strError = ((rslt.allDBIds == null) || (rslt.allDBIds.Length <= 0)) ? "NO ERROR Message returns" : rslt.allDBIds[0];
                    logger.Error("GetAllDBIds", $"Can't get DB indexs with error|{strError}");
                    isOk = false;
                    return null;
                }
                isOk = true;
                return rslt.allDBIds;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception:[{0}]", e.Message);
                logger.Error("GetAllDBIds", e.Message, e.StackTrace);
                return null;
            }
            finally
            {
                logger.logEnd("GetAllDBIds");
            }
        }

        public int testReport_CreateTestReportLog(B_TEST_REPORT tstRpt, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            try
            {
                RESTfulBTestReport rpt = new RESTfulBTestReport();
                rpt.currentDBIdx = currentDBIdx;
                rpt.TestReports = new List<B_TEST_REPORT>() { tstRpt };

                RESTfulBTestReport rslt = DoPut<RESTfulBTestReport>("MarsEngine/create/TestReportLog", rpt, ref isOk, ref strError);
                if ((!isOk) || (rslt == null))
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = "Can't create test report Log but no error returns";
                    return -1;
                }

                var returnedObj = rslt.TestReports.FirstOrDefault();
                if (returnedObj == null)
                {
                    strError = "No test reported Id is returned from server";
                    strAdv = "Please contact Marquis";
                    return -1;
                }

                //copy returned data to tstRpt
                tstRpt.CloneFrom(returnedObj);

                return (int)rslt.convertExtToInt(-1);
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]\r\n{1}", e.Message, e.StackTrace);
                isOk = false;
                Logger.Error("testReport_CreateTestReportLog", e.Message, e.StackTrace);
                return -1;
            }
        }


        internal int testReport_updateById(B_TEST_REPORT tstRpt, ref bool isOk, ref string strError)
        {
            try
            {
                RESTfulReturnedTestReport rpt = new RESTfulReturnedTestReport();
                rpt.objectType = (int)RESTfulObjectType.reportObject;
                rpt.TestReports = new List<B_TEST_REPORT>() { tstRpt };
                rpt.currentDBIdx = currentDBIdx;
                RESTfulReturnedTestReport rslt = DoPut<RESTfulReturnedTestReport>("MarsEngine/update/TestReport", rpt, ref isOk, ref strError);
                if ((!isOk) || (rslt == null))
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = "Can't update test report status but no error returns";
                    return -1;
                }
                var oFromSvc = rslt.TestReports == null ? null : rslt.TestReports.FirstOrDefault();
                if (oFromSvc == null)
                {
                    isOk = false;
                    strError = "No updated data is returned from sever";
                    return -1;
                }
                tstRpt.CloneFrom(oFromSvc);
                return (int)rslt.convertExtToInt(-1);
            }
            catch (Exception e)
            {
                Logger.Error("testReport_updateById", e.Message, e.StackTrace);
                strError = string.Format("Exception:[{0}]\r\n{1}", e.Message, e.StackTrace);
                isOk = false;
                return -1;
            }
        }

        internal int TestReportStps_UpdateAndInsertList(B_TEST_REPORT_STEPS updateObj, List<B_TEST_REPORT_STEPS> lstInsertTestStepReport, string strObjName, ref bool isOk, ref string strError)
        {
            try
            {
                RESTfulReturnedTestReportSteps rptRest = new RESTfulReturnedTestReportSteps();
                rptRest.currentDBIdx = currentDBIdx;
                rptRest.objectType = (int)RESTfulObjectType.report_step_record_update;                
                List<T_TEST_REPORT_STEPSDTO> lstData = new List<T_TEST_REPORT_STEPSDTO>();
                lstData.Add(updateObj);

                lstData.AddRange(lstInsertTestStepReport);
                rptRest.Ext = strObjName; // for this mode, the 

                rptRest.TestReportSteps = lstData;

                //bool isBson = false;
                //if (rptStps.INFO_PIC != null)
                //    isBson = true;
                RESTfulReturnedTestReportSteps rslt = DoPut<RESTfulReturnedTestReportSteps>("MarsEngine/update/TestReportStepsList", rptRest, ref isOk, ref strError);
                if ((!isOk) || (rslt == null))
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError))
                        strError = "can't update steps report information";
                    return -1;
                }
                if (rslt.objectType != (int)RESTfulObjectType.report_step_record_update)
                {
                    isOk = false;
                    if (!string.IsNullOrEmpty(strError))
                    {
                        strError = string.Format("returned object type should be report_step_record_update, but it is [{0}]", rslt.objectType);
                    }
                    return -1;
                }
                int iCnt = -1;
                if (!int.TryParse(rslt.Ext, out iCnt))
                {
                    isOk = false;
                    strError = string.Format("Svc should return a number for Method updateRecordTestReportStepsRecord, but it returns:[{0}]", rslt.Ext);
                    return -1;
                }

                return iCnt;
            }
            catch (Exception e)
            {
                strError = e.Message;
                Logger.Error("TestReportStps_UpdateAndInsertList", e.Message, e.StackTrace);
                isOk = false;
                return -1;
            }
        }
        /// <summary>
        /// 通过restful服务更新测试报告步骤记录
        /// </summary>
        /// <param name="rptStps"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int updateRecordTestReportStepsRecord(B_TEST_REPORT_STEPS rptStps, ref bool isOk, ref string strError)
        {
            try
            {
                RESTfulReturnedTestReportSteps rptRest = new RESTfulReturnedTestReportSteps();
                rptRest.objectType = (int)RESTfulObjectType.report_step_record_update;
                rptRest.currentDBIdx = currentDBIdx;
                List<T_TEST_REPORT_STEPSDTO> lstData = new List<T_TEST_REPORT_STEPSDTO>();
                lstData.Add(rptStps);
                rptRest.TestReportSteps = lstData;
                bool isBson = false;
                if (rptStps.INFO_PIC != null)
                    isBson = true;
                string urlForUpdate = isBson ? "MarsEngine/update/TestReportStepsBson": "MarsEngine/update/TestReportSteps";

                RESTfulReturnedTestReportSteps rslt = DoPut<RESTfulReturnedTestReportSteps>(urlForUpdate, rptRest, ref isOk, ref strError, isBson);
                if ((!isOk) || (rslt == null))
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError))
                        strError = "can't update steps report information";
                    return -1;
                }
                if (rslt.objectType != (int)RESTfulObjectType.report_step_record_update)
                {
                    isOk = false;
                    if (!string.IsNullOrEmpty(strError))
                    {
                        strError = string.Format("returned object type should be report_step_record_update, but it is [{0}]", rslt.objectType);
                    }
                    return -1;
                }
                int iCnt = -1;
                if (!int.TryParse(rslt.Ext, out iCnt))
                {
                    isOk = false;
                    strError = string.Format("Svc should return a number for Method updateRecordTestReportStepsRecord, but it returns:[{0}]", rslt.Ext);
                    return -1;
                }

                return iCnt;
            }
            catch (Exception e)
            {
                strError = e.Message;
                Logger.Error("SaveTEST_REPORT_STEPS", e.Message, e.StackTrace);
                isOk = false;
                return -1;
            } 
        }
        /// <summary>
        /// 记录每步的测试情况，包括数据、截图等信息
        /// </summary>
        /// <param name="rptStps"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <returns></returns>
        public int SaveTEST_REPORT_STEPS(B_TEST_REPORT_STEPS rptStps, ref bool isOk, ref string strError, ref string strAdv)
        {
            try
            {
                RESTfulReturnedTestReportSteps rptRest = new RESTfulReturnedTestReportSteps();
                rptRest.objectType = (int)RESTfulObjectType.reportObject;
                List<T_TEST_REPORT_STEPSDTO> lstData = new List<T_TEST_REPORT_STEPSDTO>();
                lstData.Add(rptStps);
                rptRest.TestReportSteps = lstData;
                rptRest.currentDBIdx = currentDBIdx;
                RESTfulReturnedTestReportSteps rslt = DoPut<RESTfulReturnedTestReportSteps>("MarsEngine/update/TestReportSteps", rptRest, ref isOk, ref strError);
                if ((!isOk) || (rslt == null))
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError))
                        strError = "can't update steps report information";
                    return -1;
                }
                var stpsFromSvr = rslt.TestReportSteps == null ? null : rslt.TestReportSteps.FirstOrDefault();
                if (stpsFromSvr == null)
                {
                    isOk = false;
                    strError = "No object is returned from server.";
                    strAdv = "Please contact Marquis";
                    return -1;
                }
                rptStps.cloneFrom(stpsFromSvr);
#region replaced by DoPut method
                //System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
                //string strURL = BuildURL("MarsEngine/update/TestReportSteps", ref isOk, ref strError);
                //if (!isOk) return -1;

                //string strJsonObj = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(rptRest);
                //var httpContent = new StringContent(strJsonObj,Encoding.UTF8,"application/json");
                //HttpResponseMessage rsp = httpClient.PutAsync(strURL, httpContent).GetAwaiter().GetResult();
                //string strData = rsp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                //RESTfulReturnedTestReportSteps rslt = DeserializeToObjectFromResponseString<RESTfulReturnedTestReportSteps>(strData, ref isOk, ref strError);
                //if ((!isOk) || (rslt == null))
                //{
                //    return -1;
                //}
#endregion
                int iCnt = -1;
                if (!int.TryParse(rslt.Ext, out iCnt))
                {
                    isOk = false;
                    strError = string.Format("Svc should return a number for Method SaveTEST_REPORT_STEPS, but it returns:[{0}]", rslt.Ext);
                    return -1;
                }

                return iCnt;
            }
            catch (Exception e)
            {
                Logger.Error("SaveTEST_REPORT_STEPS", e.Message, e.StackTrace);
                return -1;
            }
        }

        public RESTfulStoryboardBasicInfo getStoryBoardInfosWithApps(string storyboardName, string projectName, string applicationShortName, 
            string strDBIdx, 
            ref string strError, ref string strAdv, ref string strStack, ref bool isOk)
        {
            Logger.logBegin("getStoryBoardInfosWithApps", $"storyboard:[{storyboardName}] proj:[{projectName}]");
            try
            {
                string strURL = BuildURL($"MarsEngine/Storyboard?storyboardName={storyboardName}&projectName={projectName}&appShortName={applicationShortName}&currentDBIdx={strDBIdx}", ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("getStoryBoardInfosWithApps", strError);
                    strStack = Environment.StackTrace;
                    strAdv = "Contact Marquis";
                    return null;
                }
                RESTfulStoryboardBasicInfo storyboardInfo = GetDataFromURL<RESTfulStoryboardBasicInfo>(strURL, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("getStoryBoardInfosWithApps", strError, strStack=Environment.StackTrace);
                    return null;
                }
                isOk = true;
                return storyboardInfo;
            }
            catch (Exception e)
            {
                Logger.Error("getStoryBoardInfosWithApps", strError = e.Message, strStack = e.StackTrace);
                strAdv = "Contact Marquis";
                return null;
            }
            finally
            {
                Logger.logEnd("getStoryBoardInfosWithApps");
            }
        }

        public T DeserializeToObjectFromResponseString<T>(string strData, ref bool isOk, ref string strError)
        {
            Logger.logBegin("DeserializeToObjectFromResponseString",string.IsNullOrEmpty(strData)?"strData Len:null or empty":$"strDataLen:{strData.Length}");
            try
            {
                var settings = new DataContractJsonSerializerSettings
                {
                    DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat("s"),

                };

                var jsSrlzr = new System.Web.Script.Serialization.JavaScriptSerializer();
                
                jsSrlzr.MaxJsonLength = 200 * 1024 * 1024;  //10 M
                T rslt = jsSrlzr.Deserialize<T>(strData);

                //DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), settings);
                //MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(strData));
                //T rslt = (T)serializer.ReadObject(ms);
                if (rslt == null)
                {
                    isOk = false;
                    strError = string.Format("Can't convert data to taget object -[{0}]", typeof(T));
                    Logger.Error("DeserializeToObjectFromResponseString", strError);
                    return default(T);
                }
                isOk = true;

                return rslt;
            }
            catch (Exception e)
            {
                Logger.Error("DeserializeToObjectFromResponseString", strData, e);
                isOk = false;
                strError = string.Format("Exception:[{0}]", e.Message);
                return default(T);
            }
            finally
            {
                Logger.logEnd("DeserializeToObjectFromResponseString", $"return {isOk}");
            }
        }


        public T GetDataFromURL<T>(string strURLWithPara, ref bool isOk, ref string strError)
        {
            try
            {
                Logger.Info("GetDataFromURL", strURLWithPara);
                //Console.WriteLine("GetDataFromURL {0}", strURLWithPara);
                string strData = GetURLData(strURLWithPara);
                //if (strURLWithPara.IndexOf("SystemLookup?tableName=LOOP_VAR") > 0)
                //{
                //    logger.Info("\t",strData);
                //}
                //var settings = new DataContractJsonSerializerSettings
                //{
                //    DateTimeFormat = new System.Runtime.Serialization.DateTimeFormat("s")
                //};
                //DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), settings);

                //MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(strData));

                //T rslt = (T)serializer.ReadObject(ms);
                //if (rslt == null)
                //{
                //    isOk = false;
                //    strError = string.Format("Can't get data from [{0}]", strURLWithPara);
                //    return default(T);
                //}
                //isOk = true;
                //return rslt;
                //Console.WriteLine(strData);
                return DeserializeToObjectFromResponseString<T>(strData, ref isOk, ref strError);
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception:[{0}]", e.Message);
                Console.WriteLine("\t{0}\r\n\t{1}", e.Message, e.StackTrace);
                Logger.Error("GetDataFromURL", e.Message,e);
                return default(T);
            }

        }
        /// <summary>
        /// 获得系统支持的数据库链接信息
        /// </summary>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strStack"></param>
        /// <param name="fixedURLPreFix"></param>
        /// <returns></returns>
        internal RESTfulDBLinks navigateDBLinks(ref bool isOk, ref string strError, ref string strStack, string fixedURLPreFix)
        {
            Logger.logBegin("navigateDBLinks");
            try
            {
                
                string strURL = BuildURL("MarsEngine/DBLinks", ref isOk, ref strError);
                Logger.Info("\t", strURL);
                RESTfulDBLinks dbLinks = GetDataFromURL<RESTfulDBLinks>(strURL, ref isOk, ref strError);
                if (!isOk)
                {
                    strStack = Environment.StackTrace;
                }
                return dbLinks;
            }catch(Exception e)
            {
                Logger.Error("navigateDBLinks", e.Message, e.StackTrace);
                strError = e.Message;
                isOk = false;
                strStack = e.StackTrace;
                return null;
            }
            finally
            {
                Logger.logEnd("navigateDBLinks");
            }
        }
        /// <summary>
        /// 获得系统支持的测试应用信息
        /// </summary>
        /// <param name="dbIdx"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strStack"></param>
        /// <returns></returns>
        public RESTfullReturnApplicationObjects navigateApplications(string dbIdx, ref bool isOk, ref string strError, ref string strStack)
        {
            try
            {
                string strURL = BuildURL($"MarsEngine/Application?currentDBIdx={dbIdx}", ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("navigateApplications", strError);
                    return null;
                }
                RESTfullReturnApplicationObjects lstAPPs = this.GetDataFromURL<RESTfullReturnApplicationObjects>(strURL, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("navigateApplications", strError);
                    return null;
                }
                /// ??¡¤???????????¡¤?????
                /// 
                if ((lstAPPs.AssignedObjects == null) || (lstAPPs.AssignedObjects.ToList().Count <= 0))
                {
                    isOk = false;
                    strError = "No application data returns from DB";
                    return null;
                }
                                
                return lstAPPs;
            }
            catch (Exception e)
            {
                Logger.Error("navigateApplications", strError = e.Message, strStack = e.StackTrace);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("navigateApplications");
            }
        }

#if _pythonInterface

        internal string getObjectFromCurrentParentWindow(string dbIdx,string strAppShortName, string strPegName, ref bool isOk, 
            ref string strError, ref string strStack)
        {
            try
            {
                string strURL = BuildURL($"MarsEngine/Objects/ObjectsOfPeg?dbIdx={dbIdx}&appShortName={strAppShortName}&pegName={strPegName}",
                    ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("getObjectFromCurrentParentWindow", strError, strStack = Environment.StackTrace);
                    return null;
                }
                RESTfulReturnedObjects strRslt = GetDataFromURL<RESTfulReturnedObjects>(strURL, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("getObjectFromCurrentParentWindow", strError, strStack = Environment.StackTrace);
                    return null;
                }
                /// ¡Á?????python????
                /// 
                List<PythonMarsObject> pyObj = PythonMarsObject.FromMarsDB_V_ObjectSnapshots(strRslt.Objects == null ? null : strRslt.Objects.ToList());
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                isOk = true;
                return js.Serialize(pyObj);
            }
            catch (Exception e)
            {
                Logger.Error("getObjectFromCurrentParentWindow", strError = e.Message, strStack = e.StackTrace);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("getObjectFromCurrentParentWindow");
            }
        }

        internal string navigateApplications(string dbIdx, ref bool isOk, ref string strError, ref string strStack)
        {
            try
            {
                string strURL = BuildURL($"MarsEngine/Application?currentDBIdx={dbIdx}", ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("navigateApplications", strError);
                    return null;
                }
                RESTfullReturnApplicationObjects lstAPPs = this.GetDataFromURL<RESTfullReturnApplicationObjects>(strURL, ref isOk, ref strError);
                if (!isOk)
                {
                    Logger.Error("navigateApplications", strError);
                    return null;
                }
                /// ??¡¤???????????¡¤?????
                /// 
                if ((lstAPPs.AssignedObjects == null) || (lstAPPs.AssignedObjects.ToList().Count <= 0))
                {
                    isOk = false;
                    strError = "No application data returns from DB";
                    return null;
                }
                /// ?¨¨??¡Á?????js????
                /// 
                List<PythonApplicationForAutomation> lstPythonApp = new List<PythonApplicationForAutomation>();
                foreach(var itm in lstAPPs.AssignedObjects)
                {
                    if (itm == null) continue;
                    PythonApplicationForAutomation objPythonApp = PythonApplicationForAutomation.FromT_Reg_appDTO(itm, ref isOk, ref strError, ref strStack);
                    if (!isOk) continue;
                    lstPythonApp.Add(objPythonApp);
                }

                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                string strResult = js.Serialize(lstPythonApp);
                Logger.Info("navigateApplications", $"returned application:[{strResult}]");
                return strResult;
            }catch(Exception e)
            {
                Logger.Error("navigateApplications", strError = e.Message, strStack = e.StackTrace);
                isOk = false;
                return null;
            }
        }

        internal string getPegWindowByAppShortName(string strDBIdx, string strShortName, ref bool isOk, ref string strError, ref string strStack)
        {
            Logger.logBegin("getPegWindowByAppShortName", $"DB:{strDBIdx}, APPShortName:[{strShortName}]");
            try
            {
                string strURL = BuildURL($"MarsEngine/Objects/Pegwindows?appShortName={strShortName}&currentDBIdx={strDBIdx}",ref isOk, ref strError);
                RESTfulReturnedObjects pegs = this.GetDataFromURL<RESTfulReturnedObjects>(strURL, ref isOk, ref strError);
                if ((!isOk) || (pegs == null))
                {
                    isOk = false;
                    strStack = Environment.StackTrace;
                    return null;
                }
                if ((pegs.Objects == null) || (pegs.Objects.ToList().Count <= 0))
                {
                    isOk = false;
                    strError = $"No Parent windows object exists from [{strShortName}] db:[{strDBIdx}]";
                    return null;
                }
                /// ¡Á?????python????
                /// 
                List<PythonMarsObject> pyObj = PythonMarsObject.FromMarsDB_V_ObjectSnapshots(pegs.Objects==null?null:pegs.Objects.ToList());
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                isOk = true;
                return js.Serialize(pyObj);
            }
            catch (Exception e)
            {
                Logger.Error("getPegWindowByAppShortName", strError = e.Message, strStack =e.StackTrace);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("getPegWindowByAppShortName");
            }
        }
#endif
        public bool InsertTestStepResultForKeyCompare(long? lRptId, long? stpId, 
            DateTime? beginTime,DateTime? endTime,
            short iSuccessId, List<KeyValuePair<string, string>> lstObjectNameAndValues,
            long? dATA_SUMMARY_ID, string strObjectNameIdx,
            string strRunningError,
            ref string strError)
        {
            try
            {
                bool isOk = false;
                RESTfulReturnedCaptureData catpuredData = DoPut<RESTfulReturnedCaptureData>("MarsEngine/insert/CaputredData",
                    new RESTfulReturnedCaptureData()
                    {
                        objectType = (int)RESTfulObjectType.report_step_capture_data,
                        currentDBIdx = this.currentDBIdx,
                        CapturedData = new MarsRESTfulCaptureDataInfo(lRptId, stpId, beginTime,
                        endTime,
                        iSuccessId, lstObjectNameAndValues,
                        dATA_SUMMARY_ID, strObjectNameIdx,                        
                        strRunningError
                        )
                    }, ref isOk, ref strError);
                if ((!isOk) || (catpuredData == null))
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "can't Insert captured data and no error returns from [MarsEngine/insert/CaputredData]";

                    }
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("InsertTestStepResultForKeyCompare", e.Message, e.StackTrace);
                strError = string.Format("Exception:[{0}]\r\n[{1}]", e.Message, e.StackTrace);
                return false;
            }
        }
        /// <summary>
        /// 更新测试场景依赖关系
        /// </summary>
        /// <param name="lstStoryBoardToChange"></param>
        /// <param name="strAction"></param>
        /// <param name="strDefaultAction2"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal bool StoryboarTestFullVision_UpdateDepends(IEnumerable<V_STORYBOARD_TEST_FULLVISIONDTO> lstStoryBoardToChange, string strAction, string strDefaultAction2, ref string strError)
        {
            try
            {
                bool isOk = false;
                RESTfulStoryboardTestFullVison storyboardDataoToSever = DoPut<RESTfulStoryboardTestFullVison>("MarsEngine/update/StoryboardFullVision",
                    new RESTfulStoryboardTestFullVison()
                    {
                        objectType = (int)RESTfulObjectType.storyboard_testFullVision,
                        currentDBIdx = this.currentDBIdx,
                        actionInfo = strAction,
                        action2Info = strDefaultAction2,
                        storyboardTestFullVisions = lstStoryBoardToChange
                    }, ref isOk, ref strError);
                if ((!isOk) || (storyboardDataoToSever == null))
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "can't update Depends data and no error returns from [MarsEngine/update/StoryboardFullVision]";

                    }
                    return false;
                }
                if (storyboardDataoToSever.convertExtToInt(-1) > 0)
                    return true;
                else
                {
                    if (string.IsNullOrEmpty(storyboardDataoToSever.ReturnedMessage))
                        strError = "can't update Depends and no error returns";
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Error("StoryboarTestFullVision_UpdateDepends", e.Message, e.StackTrace);
                strError = string.Format("Exception:[{0}]\r\n[{1}]", e.Message, e.StackTrace);
                return false;
            }
        }

        public int deleteTestStepRecordsForReRun(int testStepId, long testRptId, ref string strError, ref string strStack, ref string strAdv)
        {
            Logger.logBegin("deleteTestStepRecordsForReRun", $"test step id:[{testStepId}] - [{testRptId}]");
            try
            {
                bool isOk = false;
                string strURL = $"MarsEngine/TestReportStepOp?Op=del&stepId={testStepId}&testReportId={testRptId}&currentDBIdx={currentDBIdx}";
                strURL = BuildURL(strURL, ref isOk, ref strError);
                Logger.Info("deleteTestStepRecordsForReRun", $"URL is [{strURL}]");
                RESTfulTestReportStepOperation OpRslt = GetDataFromURL<RESTfulTestReportStepOperation>(strURL, ref isOk, ref strError);
                if (OpRslt.ResultNumber < 0)
                {
                    strError = OpRslt.ReturnedMessage;
                    strAdv = "Contact Marquis";
                    strStack = Environment.StackTrace;
                    return -1;
                }
                return OpRslt.ResultNumber;
            }
            catch (Exception e)
            {
                Logger.Error("deleteTestStepRecordsForReRun",strError = e.Message, strStack = e.StackTrace);
                strAdv = "Contact Marquis";
                return -1;
            }
            finally
            {
                Logger.logEnd("deleteTestStepRecordsForReRun");
            }
        }

        private string BuildURL(string strAPI, ref bool isOk, ref string strError)
        {
            try
            {
                Logger.logBegin("BuildURL", $"base url:[{WebURLPrefix[WebURLPrefix.Length - 1]}]");
                string strURLWithoutSlash = "";
                if (WebURLPrefix[WebURLPrefix.Length - 1] != '/')
                {
                    strURLWithoutSlash = WebURLPrefix + "/";
                }
                isOk = true;
                return string.Format("{0}{1}", WebURLPrefix, strAPI);
            }
            catch (Exception e)
            {
                Logger.Error("BuildURL", e.Message, e.StackTrace);
                isOk = false;
                strError = e.Message;
                return "";
            }


        }

        public long GetLastestTestMarkID(long? lStoryboardId, ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetLastestTestMarkID", $"storyboardId:{lStoryboardId}");
            string strURL = "";
            strURL = string.Format("MarsEngine/TestMarkIdByStoryboardId?storyboardId={0}&currentDBIdx={1}", lStoryboardId,currentDBIdx);
            strURL = BuildURL(strURL, ref isOk, ref strError);
            if (!isOk) return -1;
            RESTfulReturnLastMarkIdObjects webApiReturnedLastId = GetDataFromURL<RESTfulReturnLastMarkIdObjects>(strURL, ref isOk, ref strError);
            if (!isOk)
            {
                return -1;
            }
            if (webApiReturnedLastId.objectType != (int)RESTfulObjectType.storyboard_lastMarkId)
            {
                strError = "returned object type is not for LastMarkId";
                return -1;
            }
            return webApiReturnedLastId.LastMarkId;
        }

        public B_QUERY GetQueryByQueryName(string queryName, ref bool isOk, ref string strError, 
            ref string strStack, ref string strAdv, 
            string strDBIdx = null)
        {
            Logger.logBegin("GetQueryByQueryName", $"queryName|{queryName}|DBName|{strDBIdx}|");
            try
            {
                strError = "";
                string strURL = BuildURL(string.Format("MarsEngine/DbSourceByName?queryName={0}&currentDBIdx={1}",
                    queryName, string.IsNullOrEmpty(strDBIdx) ? currentDBIdx : strDBIdx),
                    ref isOk, ref strError);

                RESULTTQueryInfo webDataSourceReturnedData = GetDataFromURL<RESULTTQueryInfo>(strURL, ref isOk, ref strError);
                if ((!isOk)||(webDataSourceReturnedData==null))
                {
                    Logger.Error("GetQueryByQueryName", strError);
                    return null;
                }
                if ((webDataSourceReturnedData.DataSourceInfo == null)
                    ||(string.IsNullOrEmpty(webDataSourceReturnedData.DataSourceInfo.QUERY_DESC)))
                {
                    strError = "Code 04A:No data source returns from MARS.";
                    isOk = false;
                    Logger.Error("GetQueryByQueryName", strError);
                    return null; 
                }
                isOk = true;
                return webDataSourceReturnedData.DataSourceInfo;
            }
            catch (Exception e)
            {
                strError = string.Format("Code 042:Can't get Data from DB for Query datasource \"{0}\" ,Please make sure the Query and related Settings are right.",
                    queryName);
                Logger.Error("GetQueryByQueryName", e.Message, e);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("GetQueryByQueryName", $"isOk|{isOk}|Error|{strError}|");
            }
        }

        public B_REGISTERED_APPS GetApplicationByAppShortName(string app, ref bool isOk, ref string strError, ref string strStack, ref string strAdv,string strDBIdx = null)
        {
            Logger.logBegin("GetApplicationByAppShortName");
            try
            {
                strError = "";
                string strURL = BuildURL(string.Format("MarsEngine/Application?applicationShortName={0}&currentDBIdx={1}",
                    app,string.IsNullOrEmpty(strDBIdx)?currentDBIdx: strDBIdx),
                    ref isOk, ref strError);
                RESTfulReturnedObjects rslt = GetDataFromURL<RESTfulReturnedObjects>(strURL, ref isOk, ref strError);
                Logger.Info("GetApplicationByAppId", $"{strURL} returns {isOk}, Error-[{strError}]");
                if (!isOk) return null;
                RESTfullReturnApplicationObjects webApiReturnedData = GetDataFromURL<RESTfullReturnApplicationObjects>(strURL, ref isOk, ref strError);
                if (!isOk)
                {
                    return null;
                }
                if ((webApiReturnedData.AssignedObjects == null)
                    || (webApiReturnedData.AssignedObjects.ToList().Count <= 0)
                    )
                {
                    strError = string.Format("can't get application from [{0}]", strURL);
                    isOk = false;
                    return null;
                }

                isOk = true;
                return webApiReturnedData.AssignedObjects.FirstOrDefault(); 
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception:{0}", e.Message);
                strAdv = "Contact Marquis";
                strStack = e.StackTrace;
                Logger.Error("GetApplicationByAppShortName", e.Message, e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetApplicationByAppShortName");
            }
        }

        public B_REGISTERED_APPS GetApplicationByAppId(string strDBIdx,long lAppId, ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetApplicationByAppId", lAppId+"");
            try
            {            
                if (string.IsNullOrEmpty(WebURLPrefix))
                {
                    isOk = false;
                    strError = "no 'MarsEngineSvc_url' find in Configuration.";
                    return null;
                }
                //WebClientForMars client = new WebClientForMars();
                string strURL = BuildURL(string.Format("MarsEngine/Application?id={0}&currentDBIdx={1}", lAppId, strDBIdx), ref isOk, ref strError);
                Logger.Info("GetApplicationByAppId", $"{strURL} returns {isOk}, Error-[{strError}]");
                if (!isOk) return null;
                RESTfullReturnApplicationObjects webApiReturnedData = GetDataFromURL<RESTfullReturnApplicationObjects>(strURL, ref isOk, ref strError);
                if (!isOk)
                {
                    return null;
                }
                if ((webApiReturnedData.AssignedObjects == null)
                    || (webApiReturnedData.AssignedObjects.ToList().Count <= 0)
                    )
                {
                    strError = string.Format("can't get application from [{0}]", strURL);
                    isOk = false;
                    return null;
                }

                isOk = true;
                return webApiReturnedData.AssignedObjects.FirstOrDefault(); ;
            }
            finally
            {
                Logger.logEnd("GetApplicationByAppId");
            }

        }

        internal long GetApplicationByAppShortName(string applicationName, ref bool isOk, ref string strError)
        {
            try
            {
                string strURL = BuildURL(string.Format("MarsEngine/Application?applicationShortName={0}&currentDBIdx={1}", 
                    applicationName, currentDBIdx), 
                    ref isOk, ref strError);
                if (!isOk) return -1;
                RESTfullReturnApplicationObjects webApiReturnedData = GetDataFromURL<RESTfullReturnApplicationObjects>(strURL, ref isOk, ref strError);
                if (!isOk)
                {
                    return -1;
                }
                if ((webApiReturnedData.AssignedObjects == null)
                    || (webApiReturnedData.AssignedObjects.ToList().Count <= 0)
                    )
                {
                    strError = string.Format("can't get application from [{0}]", strURL);
                    isOk = false;
                    return -1;
                }

                isOk = true;
                var targetAppObj = webApiReturnedData.AssignedObjects.FirstOrDefault();
                if (targetAppObj == null)
                {
                    strError = string.Format("no such [{0}] is registered", applicationName);
                    isOk = false;
                    return -1;
                }
                return targetAppObj.APPLICATION_ID;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception :[{0}]", e.Message);
                Logger.Error("GetApplicationByAppShortName", e.Message, e);
                return -1;
            }


        }

        /// <summary>
        /// 根据测试用例ID获取测试步骤信息
        /// </summary>
        /// <param name="iTestCaseId"></param>
        /// <param name="lAppId"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strDBIdx"></param>
        /// <returns></returns>

        public List<V_TEST_STEPS_FULLVISIONDTO> GetTestStepsByTestCaseID(long iTestCaseId, long lAppId, ref bool isOk, ref string strError, 
            ref string strStack, ref string strAdv,string strDBIdx)
        {
            try
            {
                strError = "";
                string strURL = BuildURL(string.Format("MarsEngine/TestcaseSteps?testCaseId={0}&strAppId={1}&currentDBIdx={2}", iTestCaseId, lAppId,strDBIdx), 
                    ref isOk, ref strError);
                if (!isOk) return null;
                RESTfulReturnedVTestCaseSteps returnedSteps = GetDataFromURL<RESTfulReturnedVTestCaseSteps>(strURL, ref isOk, ref strError);
                if ((!isOk) || (returnedSteps == null))
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError)) strError = "returned object is null";
                    return null;
                }
                if (returnedSteps.objectType == (int)RESTfulObjectType.error_obj)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "returned object type is Error";
                        strError = returnedSteps.ReturnedMessage;
                        strStack = returnedSteps.StackTrace;
                        strAdv = "Contact Marquis";
                    }
                    isOk = false;
                    return null;
                }
                return returnedSteps.TestStepsForTestcase == null ? null : returnedSteps.TestStepsForTestcase.ToList();
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetTestStepsByTestCaseID", e.Message, e);
                strError = string.Format("Exception:[{0}]", e.Message);
                return new List<V_TEST_STEPS_FULLVISIONDTO>();
            }

        }
        /// <summary>
        /// 保存测试结果到测试用例
        /// </summary>
        /// <param name="storyBoardTestResult"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public int SaveStoryBoardTestResult(B_PROJ_TEST_RESULT storyBoardTestResult, ref bool isOk, ref string strError)
        {
            Logger.logBegin("SaveStoryBoardTestResult", $"test case id:{storyBoardTestResult.TEST_CASE_ID}, Latest Mark id:[{storyBoardTestResult.LATEST_TEST_MARK_ID}]");
            try
            {
                RESTfulProjTestResult projTestRslt = DoPut<RESTfulProjTestResult>("MarsEngine/save/ProjectTestResult" +
                    "",
                    new RESTfulProjTestResult()
                    {
                        Proj_Test_Results = new List<B_PROJ_TEST_RESULT>() { storyBoardTestResult },
                        currentDBIdx = this.currentDBIdx,
                    }, ref isOk, ref strError, isDebug: true);
                if ((!isOk) || (projTestRslt == null))
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "can't update Depends data and no error returns from [MarsEngine/update/Storybard]";

                    }

                    return -1;
                }

                //????????hist_id
                B_PROJ_TEST_RESULT returnedData = null;
                if ((projTestRslt.Proj_Test_Results == null)
                    ||((returnedData=projTestRslt.Proj_Test_Results.FirstOrDefault())==null)
                    )
                {
                    Logger.Error("SaveStoryBoardTestResult", strError = "No Object returned from server");
                    return -1;
                }
                storyBoardTestResult.HIST_ID = returnedData.HIST_ID;
                Logger.Info("SaveStoryBoardTestResult", $"returned HistID:[{returnedData.HIST_ID}]");

                if (projTestRslt.convertExtToInt(-1) > 0)
                    return (int)projTestRslt.convertExtToInt(-1);
                else
                {
                    if (string.IsNullOrEmpty(projTestRslt.ReturnedMessage))
                        strError = "can't update Depends and no error returns";
                    return -1;
                }
            }
            catch (Exception e)
            {
                Logger.Error("\t", e.Message, e);
                strError = e.Message;
                return -1;
            }
            finally
            {
                Logger.logEnd("SaveStoryBoardTestResult");
            }

        }

        public IList<KeyValuePair<long?, TEST_DATA_SETTINGDTO>> GetTestDataByTestCaseIDAndDataSetId(long lTestCase, long lDBSetId, ref bool isOk, ref string strError)
        {
            try
            {
                strError = "";
                string strURL = BuildURL(string.Format("MarsEngine/TestData?testCaseId={0}&datasetId={1}&currentDBIdx={2}", 
                    lTestCase, lDBSetId, currentDBIdx), 
                    ref isOk, ref strError);
                if (!isOk) return null;

                RESTfulReturnedTestData rsltFromAPI = GetDataFromURL<RESTfulReturnedTestData>(strURL, ref isOk, ref strError);
                if ((!isOk) || (rsltFromAPI == null))
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError)) strError = "returned object is null";
                    return null;
                }
                if (rsltFromAPI.objectType == (int)RESTfulObjectType.error_obj)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "returned object type is Error";
                    }
                    isOk = false;
                    return null;
                }
                isOk = true;
                return rsltFromAPI.TestDataSetWithDBSetId == null ? null : (new MarsRESTKeyValuePair<long?, TEST_DATA_SETTINGDTO>()).toKeyValuePairList(rsltFromAPI.TestDataSetWithDBSetId.ToList());
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetTestDataByTestCaseIDAndDataSetId", e.Message, e);
                strError = string.Format("Excepiton:[{0}]", e.Message);
                return null;
            }
        }

        public bool UpdateSystemLookup(B_SYSTEM_LOOKUP systemLookUp, ref string strError)
        {
            try
            {
                if (systemLookUp == null)
                {
                    strError = "parameter is null";
                    return false;
                }
                strError = "";
                bool isOk = false;
                //string strURL = BuildURL("MarsEngine/updateSelf/SystemLookup", ref isOk, ref strError);
                //if (!isOk)
                //{
                //    if (string.IsNullOrEmpty(strError))
                //    {
                //        strError = "can't build URL for insert/SystemLookup";
                //    }
                //    return false;
                //}
                RESTfulReturnedSystemLookup rslt = DoPut<RESTfulReturnedSystemLookup>("MarsEngine/updateSelf/SystemLookup", new RESTfulReturnedSystemLookup()
                {
                    objectType = (int)RESTfulObjectType.systemLookup,
                    SystemLookups = new List<B_SYSTEM_LOOKUP>() { systemLookUp },
                    currentDBIdx = this.currentDBIdx
                }, ref isOk, ref strError);
                if ((!isOk)
                    || (rslt == null)
                   )
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("can't update data from URL [{0}]", "MarsEngine/updateSelf/SystemLookup");
                    isOk = false;
                    return false;
                }

                return rslt.objectType == (int)RESTfulObjectType.systemLookup;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateSystemLookup", e.Message, e);
                strError = string.Format("Exception:[{0}]", e.Message);
                return false;
            }
        }


        internal bool UpdateStatusVar(int assignedKey, string varName, string strData2DB, ref string strError)
        {
            Logger.logBegin("UpdateStatusVar", $"k-[{assignedKey}], varName-[{varName}], data to store-[{strData2DB}]");
            try
            {
                bool isOk = true;
                //string strURL = BuildURL("MarsEngine/update/SystemLookupStatusVar", ref isOk, ref strError);
                string strURL = "MarsEngine/update/SystemLookupStatusVar";
                if (!isOk)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "can't build URL for update/SystemLookupStatusVar";
                    }
                    return false;
                }
                B_SYSTEM_LOOKUP systemLookUp = new B_SYSTEM_LOOKUP();
                systemLookUp.CurrentStatus = 1;
                systemLookUp.DISPLAY_NAME = strData2DB;
                systemLookUp.FIELD_NAME = varName;
                systemLookUp.LOOKUP_ID = assignedKey;
                systemLookUp.STATUS = 1;
                systemLookUp.TABLE_NAME = "STATUSVAR";
                RESTfulReturnedSystemLookup rslt = DoPut<RESTfulReturnedSystemLookup>(strURL, new RESTfulReturnedSystemLookup()
                {
                    objectType = (int)RESTfulObjectType.update_status_var,
                    currentDBIdx = this.currentDBIdx,
                    SystemLookups = new List<B_SYSTEM_LOOKUP>() { systemLookUp }
                }, ref isOk, ref strError);
                if ((!isOk)
                    || (rslt == null)
                   )
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("can't update data from URL [{0}]", strURL);
                    isOk = false;
                    return false;
                }
                return rslt.objectType == (int)RESTfulObjectType.update_status_var;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateStatusVar", e.Message, e);
                strError = string.Format("Exception:[{0}]", e.Message);
                Logger.Error("UpdateStatusVar", strError, e.StackTrace);
                return false;
            }
            finally
            {
                Logger.logEnd("UpdateStatusVar");
            }
        }

        public bool InsertSystemLookupWithStatus(B_SYSTEM_LOOKUP systemLookUp, ref string strError)
        {
            try
            {
                if (systemLookUp == null)
                {
                    strError = "parameter is null";
                    return false;
                }
                strError = "";
                bool isOk = true;
                //string strURL = BuildURL("MarsEngine/insert/SystemLookupWithStatus", ref isOk, ref strError);
                string strURL = "MarsEngine/insert/SystemLookupWithStatus";
                if (!isOk)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "can't build URL for insert/SystemLookup";
                    }
                    return false;
                }
                RESTfulReturnedSystemLookup rslt = DoPut<RESTfulReturnedSystemLookup>(strURL, new RESTfulReturnedSystemLookup()
                {
                    objectType = (int)RESTfulObjectType.systemLookup,
                    currentDBIdx = this.currentDBIdx,
                    SystemLookups = new List<B_SYSTEM_LOOKUP>() { systemLookUp }
                }, ref isOk, ref strError);
                if ((!isOk)
                    || (rslt == null)
                   )
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("can't update data from URL [{0}]", strURL);
                    isOk = false;
                    return false;
                }

                return rslt.objectType == (int)RESTfulObjectType.systemLookup;
            }
            catch (Exception e)
            {
                Logger.Error("InsertSystemLookupWithStatus", e.Message, e);
                strError = string.Format("Exception:[{0}]", e.Message);
                return false;
            }
        }

        
        public bool InsertSystemLookup(B_SYSTEM_LOOKUP systemLookUp, ref string strError)
        {
            try
            {
                if (systemLookUp == null)
                {
                    strError = "parameter is null";
                    return false;
                }
                strError = "";
                bool isOk = true;
                //string strURL = BuildURL("MarsEngine/insert/SystemLookup", ref isOk, ref strError);
                string strURL = "MarsEngine/insert/SystemLookup";
                if (!isOk)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "can't build URL for insert/SystemLookup";
                    }
                    return false;
                }
                RESTfulReturnedSystemLookup rslt = DoPut<RESTfulReturnedSystemLookup>(strURL, new RESTfulReturnedSystemLookup()
                {
                    objectType = (int)RESTfulObjectType.systemLookup,
                    SystemLookups = new List<B_SYSTEM_LOOKUP>() { systemLookUp },
                    currentDBIdx = this.currentDBIdx
                }, ref isOk, ref strError);
                if ((!isOk)
                    || (rslt == null)
                   )
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("can't update data from URL [{0}]", strURL);
                    isOk = false;
                    return false;
                }

                return rslt.objectType == (int)RESTfulObjectType.systemLookup;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]", e.Message);
                Logger.Error("InsertSystemLookup", e.Message, e);
                return false;
            }
        }

        internal int boHelper_UpdateStoryBoardTestResult(B_PROJ_TEST_RESULT storyBoardTestResult, ref string strError)
        {
            try
            {
                if (storyBoardTestResult == null)
                {
                    strError = "parameter is null";
                    return -1;
                }
                strError = "";
                bool isOk = false;
                //string strURL = BuildURL("MarsEngine/update/TestResult", ref isOk, ref strError);
                //if (!isOk)
                //{
                //    if (string.IsNullOrEmpty(strError))
                //    {
                //        strError = "can't build URL for insert/SystemLookup";
                //    }
                //    return -1;
                //}
                RESTfulProjTestResult rslt = DoPut<RESTfulProjTestResult>("MarsEngine/update/TestResult", new RESTfulProjTestResult()
                {
                    objectType = (int)RESTfulObjectType.systemLookup,
                    currentDBIdx = this.currentDBIdx,
                    Proj_Test_Results = new List<B_PROJ_TEST_RESULT>() { storyBoardTestResult }
                }, ref isOk, ref strError);
                if ((!isOk)
                    || (rslt == null)
                    )
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("can't update data from URL [{0}]", "MarsEngine/update/TestResult");
                    isOk = false;
                    return -1;
                }

                return (int)rslt.convertExtToInt(-1);
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]", e.Message);
                Logger.Error("boHelper_UpdateStoryBoardTestResult", e.Message, e);
                return -1;
            }

        }

        public bool GetBussinessSeq(ref long iN, ref bool isOk, ref string strError, string strSeqName = "T_KEYWORD_SEQ")
        {
            try
            {
                strError = "";
                string strURL = BuildURL(string.Format("MarsEngine/MarsSequence?seqName={0}&currentDBIdx={1}", strSeqName, currentDBIdx), ref isOk, ref strError);
                RESTfulReturnedSeqNumber rslt = GetDataFromURL<RESTfulReturnedSeqNumber>(strURL, ref isOk, ref strError);
                if (rslt.objectType == (int)RESTfulObjectType.error_obj)
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError)) strError = "returned object is null";
                    return false;
                }
                if (rslt.objectType != (int)RESTfulObjectType.seq_id)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "returned object type is Error";
                    }
                    isOk = false;
                    return false;
                }
                isOk = true;
                iN = (int)rslt.SeqNumber;
                return true;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception:{0}", e.Message);
                Logger.Error("GetBussinessSeq", e.Message, e);
                return false;
            }
        }

        public List<B_V_OBJECT_SNAPSHOT> GetObjectInfoByAppIdAndObjIds(string strAppId,IEnumerable<string> arrObjectsIds, ref bool isOk, ref string strError, ref string strStack, ref string strAdv)
        {
            try
            {
                Logger.logBegin("GetObjectInfoByAppIdAndObjIds", $"appId:{strAppId} - db:{this.currentDBIdx}");
                RESTfulObjectsIdsAndAppIds_Ask askObj = new RESTfulObjectsIdsAndAppIds_Ask()
                {
                    currentDBIdx = this.currentDBIdx,
                    appId = strAppId,
                    arrObjIds = arrObjectsIds
                };
                RESTfulObjectsIdsAndAppIds_Ask objsFromSvr = DoPut<RESTfulObjectsIdsAndAppIds_Ask>("MarsEngine/Objects/ObjectsByIdsAndAppId", askObj, ref isOk, ref strError);
                
                if ((!isOk) 
                    || (objsFromSvr == null)
                    || (objsFromSvr.Objects == null)
                    || (objsFromSvr.objectType == (int)RESTfulObjectType.error_obj))
                {
                    strStack = Environment.StackTrace;
                    isOk = false;
                    strAdv = "Contact Marquis";
                    return null;
                }
                return objsFromSvr.Objects.ToList();
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetObjectInfoByAppIdAndObjIds",strError = e.Message, strStack = e.StackTrace);
                strAdv = "Contact Marquis";
                return null;
            }
            finally
            {
                Logger.logEnd("GetObjectInfoByAppIdAndObjIds");
            }
        }


        public List<B_V_OBJECT_SNAPSHOT> GetObjectInfoByAppId(long lAppId, ref bool isOk, ref string strError)
        {
            try
            {
                strError = "";
                string strURL = BuildURL(string.Format("MarsEngine/objectInfoByAppIdAnd?appId={0}&currentDBIdx={1}", 
                    lAppId, currentDBIdx), 
                    ref isOk, ref strError);
                RESTfulReturnedObjects rslt = GetDataFromURL<RESTfulReturnedObjects>(strURL, ref isOk, ref strError);
                if (rslt.objectType == (int)RESTfulObjectType.error_obj)
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError)) strError = "returned object is null";
                    return null;
                }
                if (rslt.objectType != (int)RESTfulObjectType.marsObjects)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "returned object type is Error";
                    }
                    isOk = false;
                    return null;
                }
                isOk = true;

                return rslt.Objects == null ? null : rslt.Objects.ToList();
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception:{0}", e.Message);
                Logger.Error("GetObjectInfoByAppId", e.Message, e);
                return null;
            }
        }

        public List<B_SYSTEM_LOOKUP> GetSystemLookups(string strTableName, List<string> lstFieldName, ref bool isOk, ref string strError)
        {
            try
            {
                string strURL = BuildURL(string.Format("MarsEngine/get/Variables", strTableName), ref isOk, ref strError);

                RESTfulReturnedSystemLookup requestVarInfo = new RESTfulReturnedSystemLookup();
                requestVarInfo.objectType = (int)RESTfulObjectType.systemLookup;
                requestVarInfo.SystemLookups = new List<B_SYSTEM_LOOKUP>();
                requestVarInfo.currentDBIdx = currentDBIdx;
                foreach (var itm in lstFieldName)
                {
                    if (itm == null) continue;
                    ((List<B_SYSTEM_LOOKUP>)requestVarInfo.SystemLookups).Add(new B_SYSTEM_LOOKUP()
                    {
                        TABLE_NAME = strTableName,
                        FIELD_NAME = itm
                    });
                }
                if (((List<B_SYSTEM_LOOKUP>)requestVarInfo.SystemLookups).Count <= 0)
                {
                    isOk = true;
                    return null;
                }
                RESTfulReturnedSystemLookup returnedModalVarInfo = DoPut<RESTfulReturnedSystemLookup>("MarsEngine/get/Variables",
                    requestVarInfo, ref isOk, ref strError);
                if ((!isOk) || (returnedModalVarInfo == null))
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("Can't create modal variables for [{0}]", string.Join(",", lstFieldName));
                    return null;
                }
                isOk = true;
                return returnedModalVarInfo.SystemLookups == null ? null : returnedModalVarInfo.SystemLookups.ToList();
            }
            catch (Exception e)
            {
                strError = e.Message;
                Logger.Error("GetSystemLookups", e.Message, e);
                isOk = false;
                return null;
            }
        }

        internal List<B_SYSTEM_LOOKUP> GetSystemLookup(string strTableName, string strFieldName, ref bool isOk, ref string strError)
        {
            try
            {
                string strURL = BuildURL(string.Format("MarsEngine/SystemLookup?tableName={0}&fieldName={1}&currentDBIdx={2}", 
                    strTableName, strFieldName, currentDBIdx), 
                    ref isOk, ref strError);
                Logger.Info("GetSystemLookup", strURL);
                RESTfulReturnedSystemLookup rslt = GetDataFromURL<RESTfulReturnedSystemLookup>(strURL, ref isOk, ref strError);
                //logger.Info("GetSystemLookup", $"data list returnd:[{rslt.SystemLookups}]");
                if (!isOk)
                {
                    if (string.IsNullOrEmpty(strError)) strError = string.Format("Error when get from {0}", strURL);
                    return null;
                }
                if (rslt.objectType != (int)RESTfulObjectType.systemLookup)
                {
                    isOk = false;
                    strError = string.Format("returned package type is not SystemLookup, its id is {0}", rslt.objectType);
                    return null;
                }
                //var lst = rslt.SystemLookups.ToList();
                //string strTmpResult = lst==null?"NULL":string.Join(",", lst);
                //if (lst.Count > 0)
                //{
                //    var dd = lst.Select(p => p.DISPLAY_NAME).ToList();
                //    string tmpdd = string.Join(",", dd);
                //    logger.Info("\t", $"data-dd:[{tmpdd}]");
                //}
                //logger.Info("GetSystemLookup", $"tableName:[{strTableName}], fieldName:[{strFieldName}], result:[{lst.Count}-{strTmpResult}]");
                return rslt.SystemLookups == null ? null : rslt.SystemLookups.ToList();
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                Logger.Error("GetSystemLookup", e.Message, e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetSystemLookup");    
            }
        }

        public Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> LoadAllKeywords(ref bool isOk, ref string strError)
        {
            Logger.logBegin("LoadAllKeywords", "All keywords");
            try
            {
                strError = "";
                string strURL = BuildURL(string.Format("MarsEngine/Keywords?keywordName={0}&currentDBIdx={1}", "",currentDBIdx), 
                    ref isOk, ref strError);
                RESTfulReturnedKeywords rslt = GetDataFromURL<RESTfulReturnedKeywords>(strURL, ref isOk, ref strError);
                if (rslt.objectType == (int)RESTfulObjectType.error_obj)
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError)) strError = "returned object is null";
                    return null;
                }
                if (rslt.objectType != (int)RESTfulObjectType.keywords)
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "returned object type is Error";
                    }
                    isOk = false;
                    return null;
                }
                isOk = true;

                return rslt.KeywordsWithDicRel == null ? null : (new MarsRESTKeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>()).toDictionary(rslt.KeywordsWithDicRel.ToList());
            }
            catch (Exception e)
            {
                isOk = false;
                strError = string.Format("Exception:{0}", e.Message);
                Logger.Error("LoadAllKeywords", e.Message, e);
                return null;
            }
        }

        internal bool CreateOrUpdateLoopVar(string strObjectIdx, string strDataToStore, ref string strError)
        {
            if (string.IsNullOrEmpty(strObjectIdx))
            {
                strError = "no Loop variable information is passed";
                return false;
            }
            try
            {
                RESTfulReturnedSystemLookup loopVar = new RESTfulReturnedSystemLookup()
                {
                    objectType = (int)RESTfulObjectType.variable_loop,
                    currentDBIdx = this.currentDBIdx,
                    SystemLookups = new List<B_SYSTEM_LOOKUP>()
                    {
                        new B_SYSTEM_LOOKUP()
                        {
                            TABLE_NAME = B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP,
                            FIELD_NAME = strObjectIdx,
                            DISPLAY_NAME = strDataToStore
                        }
                    }
                };
                bool isOk = false;
                RESTfulReturnedSystemLookup rslt = DoPut<RESTfulReturnedSystemLookup>("MarsEngine/insertOrUpdate/LoopVariable", loopVar, ref isOk, ref strError);
                if ((!isOk) || (rslt == null))
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("Can't create or update loop variables for [{0}]-[{1}]", strObjectIdx, strDataToStore);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("CreateOrUpdateLoopVar Exception:[{0}]\r\n[{1}]",
                    e.Message,
                    e.StackTrace);
                Logger.Error("CreateOrUpdateLoopVar", e.Message, e);
                return false;
            }
        }

        internal int CreateModualVar(List<string> lstName, ref bool isOk, ref string strError, short? sStatus)
        {
            try
            {
                if ((lstName == null) || (lstName.Count <= 0))
                {
                    isOk = true;
                    return 0;
                }
                RESTfulReturnedSystemLookup requestModualVarInfo = new RESTfulReturnedSystemLookup();
                requestModualVarInfo.objectType = (int)RESTfulObjectType.systemLookup;
                requestModualVarInfo.SystemLookups = new List<B_SYSTEM_LOOKUP>();
                requestModualVarInfo.currentDBIdx = currentDBIdx;
                foreach (var itm in lstName)
                {
                    if (itm == null) continue;
                    ((List<B_SYSTEM_LOOKUP>)requestModualVarInfo.SystemLookups).Add(new B_SYSTEM_LOOKUP()
                    {
                        TABLE_NAME = B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_MODAL,
                        FIELD_NAME = itm
                    });
                }
                if (((List<B_SYSTEM_LOOKUP>)requestModualVarInfo.SystemLookups).Count <= 0)
                {
                    isOk = true;
                    return 0;
                }
                RESTfulReturnedSystemLookup returnedModalVarInfo = DoPut<RESTfulReturnedSystemLookup>("MarsEngine/update/ModualVariables", requestModualVarInfo, ref isOk, ref strError);
                if ((!isOk) || (returnedModalVarInfo == null))
                {
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("Can't create modal variables for [{0}]", string.Join(",", lstName));
                    return -1;
                }
                long rslt = returnedModalVarInfo.convertExtToInt(-1);
                return (int)rslt;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                Logger.Error("CreateModualVar", e.Message, e);
                return -1;
            }
        }

        internal List<B_SYSTEM_LOOKUP> GetVariableInfo(string strVarIndex, ref bool isOk, ref string strError, ref string strResult,
            string strVarType = B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL)
        {
            try
            {
                if (string.IsNullOrEmpty(strVarIndex))
                {
                    strError = "no global Variable information.";
                    isOk = false;
                    return null;
                }
                int objTypeChck = -1;
                RESTfulReturnedSystemLookup requestVarInfo = new RESTfulReturnedSystemLookup();
                requestVarInfo.currentDBIdx = currentDBIdx;
                string strURIVar = "";
                if (string.Compare(strVarType, B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL, true) == 0)
                {
                    requestVarInfo.objectType = (int)RESTfulObjectType.variable_global;
                    requestVarInfo.SystemLookups = new List<B_SYSTEM_LOOKUP>()
                    {
                        new B_SYSTEM_LOOKUP()
                        {
                            TABLE_NAME = B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_GLOBAL,
                            FIELD_NAME = strVarIndex
                        }
                    };
                    strURIVar = "MarsEngine/update/GlobalVariable";
                    objTypeChck = (int)RESTfulObjectType.variable_global;
                }
                else if (string.Compare(strVarType, B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP, true) == 0)
                {
                    requestVarInfo.objectType = (int)RESTfulObjectType.variable_loop;
                    requestVarInfo.SystemLookups = new List<B_SYSTEM_LOOKUP>() {
                        new B_SYSTEM_LOOKUP(){
                            TABLE_NAME= B_SYSTEM_LOOKUP.CNST_RESERVED_VARIABLE_LOOP,
                            FIELD_NAME= strVarIndex
                            }
                    };
                    strURIVar = "MarsEngine/update/LoopVariable";
                    objTypeChck = (int)RESTfulObjectType.variable_loop;
                }
                else
                {
                    isOk = false;
                    strError = string.Format("unsupported var type:[{0}]", strVarType);
                    return null;
                };
                RESTfulReturnedSystemLookup returnedVarInfo = DoPut<RESTfulReturnedSystemLookup>(strURIVar, requestVarInfo, ref isOk, ref strError);
                if ((!isOk)
                    || (returnedVarInfo == null)
                    || (returnedVarInfo.SystemLookups == null)
                    || (returnedVarInfo.objectType != objTypeChck)
                    )
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(strError))
                        strError = string.Format("Can't get and create modal variables for [{0}]", strVarIndex);
                    return null;
                }
                isOk = true;
                return returnedVarInfo.SystemLookups.ToList();
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                Logger.Error("GetVariableInfo", e.Message, e);
                return null;
            }
        }

        public List<V_TEST_STEPS_FULLVISIONDTO> mappingObjectsBasedOnTestStepFromCaptureAndReplay(List<V_TEST_STEPS_FULLVISIONDTO> lstStps,
            long applicationId,
            string dbIdx,
            ref string strError, ref bool isOk)
        {
            Logger.logBegin("mappingObjectsBasedOnTestStepFromCaptureAndReplay", $"for|{applicationId}|{currentDBIdx}");
            RESTfulMappingObjectsRequest rqst = new RESTfulMappingObjectsRequest()
            {
                steps = lstStps,
                application = applicationId,
                currentDBIdx = dbIdx
            };
            try
            {
                string strURL = "MarsEngine/MappingObjects";
                RESTfulMappingObjectsRequest rspnsLst = DoPut<RESTfulMappingObjectsRequest>(strURL, rqst, ref isOk, ref strError, isDebug:true); 
                if ((!isOk)||(rspnsLst == null)
                    ||(rspnsLst.steps==null)
                    )
                {
                    Logger.Error("mappingObjectsBasedOnTestStepFromCaptureAndReplay", $"Error|{strError}");
                    return null;
                }
                if (rspnsLst.objectType != (int)RESTfulObjectType._get_mapping_response_ok)
                {
                    Logger.Error("mappingObjectsBasedOnTestStepFromCaptureAndReplay", $"returned package type is|{rspnsLst.objectType}|{rspnsLst.ReturnedMessage}");
                    isOk = false;
                    return null;
                }
                isOk = true;
                return rspnsLst.steps;
            }
            catch (Exception e)
            {
                Logger.Error("mappingObjectsBasedOnTestStepFromCaptureAndReplay", strError = e.Message, e);
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("mappingObjectsBasedOnTestStepFromCaptureAndReplay");
            }
        }

        public bool SaveTestcaseStepsForEngine(string strTestCase, string strTestCaseDesc, 
            List<V_TEST_STEPS_FULLVISIONDTO> lstStps,
            string dbIdx,
            ref string strError)
        {
            Logger.logBegin($"SaveTestcaseStepsForEngine\t|----|{strTestCase}|{strTestCaseDesc}|{dbIdx}");

            RESTfulSaveTestCasesRequest rqst = new RESTfulSaveTestCasesRequest()
            {
                steps = lstStps, 
                TestCaseName = strTestCase,
                currentDBIdx = dbIdx,
                TestCaseDesc = strTestCaseDesc
            };
            try
            {
                bool isOk = false;
                string strURL = "MarsEngine/SaveTestStepFromSpytool";
                RESTfulSaveTestCasesRequest rspnsLst = DoPut<RESTfulSaveTestCasesRequest>(strURL, rqst, ref isOk, ref strError, isDebug: true);
                if ((!isOk) || (rspnsLst == null)
                    || (rspnsLst.steps == null)
                    )
                {
                    Logger.Error("SaveTestcaseStepsForEngine(string strTestCase, string strTestCaseDesc, \r\n", $"Error|{strError}");
                    return isOk;
                }
                if ((rspnsLst.objectType != (int)RESTfulObjectType._save_teststep_response_ok)
                    &&(rspnsLst.objectType != (int)RESTfulObjectType._save_teststep_response_error))
                {
                    Logger.Error("SaveTestcaseStepsForEngine", $"returned package type is|{rspnsLst.objectType}|{rspnsLst.ReturnedMessage}");
                    isOk = false;
                    strError = rspnsLst.ReturnedMessage;
                    return isOk;
                }
                if (rspnsLst.objectType == (int)RESTfulObjectType._save_teststep_response_error)
                {
                    strError = rspnsLst.ReturnedMessage;
                    return false;
                }
                isOk = true;
                return true;
            }
            catch(Exception e)
            {
                Logger.Error("SaveTestcaseStepsForEngine", strError = e.Message, e);
                return false;
            }
            finally
            {
                Logger.logEnd("SaveTestcaseStepsForEngine");
            }
        }

        public bool GetImageObjectById(long objectId, string objectName, long? applicationId, string TargetPath, 
            ref string strError, ref string strFileWithPath)
        {
            Logger.logBegin($"GetImageObjectById\t|----|{objectId}|{objectName}|{TargetPath}|{applicationId}");

            RESTfulImageObjects rqst = new RESTfulImageObjects()
            {
                currentDBIdx = this.currentDBIdx,                
                ObjectId = objectId,
                ObjectName = objectName,
                ApplicationId = applicationId,
            };
            try
            {
                bool isOk = false;
                string strURL = "MarsEngine/GetObjectImagePath";
                RESTfulImageObjects rspnsLst = DoPut<RESTfulImageObjects>(strURL, rqst, ref isOk, ref strError, isDebug: true);
                if ((!isOk) || (rspnsLst == null)
                    || (rspnsLst.base64Data == null)
                    )
                {
                    Logger.Error("GetImageObjectById",$"Error|{strError}");
                    return isOk;
                }
                if (rspnsLst.objectType != (int)RESTfulObjectType._getImageObject_response_ok)                    
                {
                    Logger.Error("GetImageObjectById", $"returned package type is|{rspnsLst.objectType}|{rspnsLst.ReturnedMessage}");
                    isOk = false;
                    strError = rspnsLst.ReturnedMessage;
                    return isOk;
                }
                /// 将 base64Data 转换为图片文件
                /// 
                strFileWithPath = System.IO.Path.Combine(TargetPath, $"{objectId}.png");
                byte[] imageBytes = Convert.FromBase64String(rspnsLst.base64Data);
                System.IO.File.WriteAllBytes(strFileWithPath, imageBytes);
                isOk = true;
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("GetImageObjectById", strError = e.Message, e);
                return false;
            }
            finally
            {
                Logger.logEnd("GetImageObjectById");
            }
        }
    }
}
//#endif

