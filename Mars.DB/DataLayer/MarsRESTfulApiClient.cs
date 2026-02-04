using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;
//#if _forWebClient
using MarsEngineSvc.basicReturnDataStructure;
//#endif
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
//#if _forWebClient
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web.Script.Serialization;
//#endif
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;


public class MarsRESTfulApiClient
{
#if _forWebClient
	private static string WebURLPrefix = ConfigurationManager.AppSettings["MarsEngineSvc_url"];
#else
	private static string WebURLPrefix = null;
#endif
	private static MLogger logger = null;

	public string webURLPreFix => WebURLPrefix;

	private MLogger Logger => logger ?? (logger = MLogger.GetLogger(typeof(MarsRESTfulApiClient)));

	protected string GetURLData(string strURLWithPara)
	{
#if _forWebClient
		using (HttpClient httpClient = new HttpClient())
		{
			HttpResponseMessage result = httpClient.GetAsync(strURLWithPara).GetAwaiter().GetResult();
			return result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		}
#else
		return null;
#endif
	}

	public RESTfulReturnObjects GetDataFromURL(string strURLWithPara, ref bool isOk, ref string strError)
	{
		try
		{
			string uRLData = GetURLData(strURLWithPara);
			DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(RESTfulReturnObjects));
			MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(uRLData));
			RESTfulReturnObjects rESTfulReturnObjects = (RESTfulReturnObjects)dataContractJsonSerializer.ReadObject(stream);
			if (rESTfulReturnObjects == null)
			{
				isOk = false;
				strError = $"Can't get data from [{strURLWithPara}]";
				return null;
			}
			if (rESTfulReturnObjects.objectType == 1)
			{
				isOk = false;
				strError = $"[{strURLWithPara}] return Error, with Error message:[{rESTfulReturnObjects.ReturnedMessage}]";
				return rESTfulReturnObjects;
			}
			isOk = true;
			return rESTfulReturnObjects;
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = $"Exception:[{ex.Message}]";
			return null;
		}
	}

	protected T DoPut<T>(string strURLPart, T objToSend, ref bool isOk, ref string strError, bool isBSon = false, bool isDebug = false)
	{
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		Logger.logBegin("DoPut", string.Format("url:{0}, isBSon:{1} object is:{2}", strURLPart, isBSon, (objToSend == null) ? "N/A" : objToSend.ToString()));
		string text = "";
		try
		{
			string text2 = BuildURL(strURLPart, ref isOk, ref strError);
			if (!isOk)
			{
				return default(T);
			}
			HttpClient httpClient = new HttpClient();
			HttpResponseMessage httpResponseMessage = null;
			if (isBSon)
			{
				httpClient.DefaultRequestHeaders.Accept.Clear();
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/bson"));
				MediaTypeFormatter val = (MediaTypeFormatter)(object)new BsonMediaTypeFormatter();
				httpResponseMessage = HttpClientExtensions.PutAsync<T>(httpClient, text2, objToSend, val).GetAwaiter().GetResult();
				string result = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				T result2 = DeserializeToObjectFromResponseString<T>(result, ref isOk, ref strError);
				if (!isOk || result2.Equals(default(T)))
				{
					isOk = false;
					return default(T);
				}
				return result2;
			}
			string text3 = new JavaScriptSerializer().Serialize(objToSend);
			if (isDebug)
			{
				Logger.Info("doPut", "after JSon converted to:" + text3, 120, "DoPut");
			}
			StringContent content = new StringContent(text3, Encoding.UTF8, "application/json");
			if (isDebug)
			{
				Logger.Info("doPut", $"created StringContent:{httpClient}", 125, "DoPut");
			}
			Logger.Info("doPut", "before PutAsync", 127, "DoPut");
			httpResponseMessage = httpClient.PostAsync(text2, content).GetAwaiter().GetResult();
			Logger.Info("\t", "ReadAsStringAsync before", 140, "DoPut");
			text = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			Logger.Info("\t", "DeserializeToObjectFromResponseString before", 142, "DoPut");
			T result3 = DeserializeToObjectFromResponseString<T>(text, ref isOk, ref strError);
			if (!isOk || result3.Equals(default(T)))
			{
				isOk = false;
				strError = (string.IsNullOrEmpty(strError) ? "No object is returned" : strError);
				Logger.Error("doPut", $"isOk:{isOk}, {strError}", 148, "DoPut");
				return default(T);
			}
			return result3;
		}
		catch (Exception ex)
		{
			Logger.Error("doPut", "data returned:" + text, 156, "DoPut");
			Logger.Error("doPut", ex.Message, ex, 157, "DoPut");
			strError = $"Exception:[{ex.Message}]";
			isOk = false;
			return default(T);
		}
		finally
		{
			Logger.logEnd("doPut", $"returns {isOk}");
		}
	}

	public int testReport_CreateTestReportLog(B_TEST_REPORT tstRpt, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
	{
		try
		{
			RESTfulBTestReport rESTfulBTestReport = new RESTfulBTestReport();
			rESTfulBTestReport.TestReports = new List<B_TEST_REPORT>
			{
				tstRpt
			};
			RESTfulBTestReport rESTfulBTestReport2 = DoPut("MarsEngine/create/TestReportLog", rESTfulBTestReport, ref isOk, ref strError);
			if (!isOk || rESTfulBTestReport2 == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "Can't create test report Log but no error returns";
				}
				return -1;
			}
			B_TEST_REPORT b_TEST_REPORT = rESTfulBTestReport2.TestReports.FirstOrDefault();
			if (b_TEST_REPORT == null)
			{
				strError = "No test reported Id is returned from server";
				strAdv = "Please contact Marquis";
				return -1;
			}
			tstRpt.CloneFrom(b_TEST_REPORT);
			return (int)rESTfulBTestReport2.convertExtToInt(-1L);
		}
		catch (Exception ex)
		{
			strError = $"Exception:[{ex.Message}]\r\n{ex.StackTrace}";
			isOk = false;
			return -1;
		}
	}

	internal int testReport_updateById(B_TEST_REPORT tstRpt, ref bool isOk, ref string strError)
	{
		try
		{
			RESTfulReturnedTestReport rESTfulReturnedTestReport = new RESTfulReturnedTestReport();
			rESTfulReturnedTestReport.objectType = 13;
			rESTfulReturnedTestReport.TestReports = new List<B_TEST_REPORT>
			{
				tstRpt
			};
			RESTfulReturnedTestReport rESTfulReturnedTestReport2 = DoPut("MarsEngine/update/TestReport", rESTfulReturnedTestReport, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedTestReport2 == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "Can't update test report status but no error returns";
				}
				return -1;
			}
			T_TEST_REPORTDTO t_TEST_REPORTDTO = (rESTfulReturnedTestReport2.TestReports == null) ? null : rESTfulReturnedTestReport2.TestReports.FirstOrDefault();
			if (t_TEST_REPORTDTO == null)
			{
				isOk = false;
				strError = "No updated data is returned from sever";
				return -1;
			}
			tstRpt.CloneFrom(t_TEST_REPORTDTO);
			return (int)rESTfulReturnedTestReport2.convertExtToInt(-1L);
		}
		catch (Exception ex)
		{
			strError = $"Exception:[{ex.Message}]\r\n{ex.StackTrace}";
			isOk = false;
			return -1;
		}
	}

	internal int TestReportStps_UpdateAndInsertList(B_TEST_REPORT_STEPS updateObj, List<B_TEST_REPORT_STEPS> lstInsertTestStepReport, string strObjName, ref bool isOk, ref string strError)
	{
		try
		{
			RESTfulReturnedTestReportSteps rESTfulReturnedTestReportSteps = new RESTfulReturnedTestReportSteps();
			rESTfulReturnedTestReportSteps.objectType = 19;
			List<T_TEST_REPORT_STEPSDTO> list = new List<T_TEST_REPORT_STEPSDTO>();
			list.Add(updateObj);
			list.AddRange(lstInsertTestStepReport);
			rESTfulReturnedTestReportSteps.Ext = strObjName;
			rESTfulReturnedTestReportSteps.TestReportSteps = list;
			RESTfulReturnedTestReportSteps rESTfulReturnedTestReportSteps2 = DoPut("MarsEngine/update/TestReportStepsList", rESTfulReturnedTestReportSteps, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedTestReportSteps2 == null)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't update steps report information";
				}
				return -1;
			}
			if (rESTfulReturnedTestReportSteps2.objectType != 19)
			{
				isOk = false;
				if (!string.IsNullOrEmpty(strError))
				{
					strError = $"returned object type should be report_step_record_update, but it is [{rESTfulReturnedTestReportSteps2.objectType}]";
				}
				return -1;
			}
			int result = -1;
			if (!int.TryParse(rESTfulReturnedTestReportSteps2.Ext, out result))
			{
				isOk = false;
				strError = $"Svc should return a number for Method updateRecordTestReportStepsRecord, but it returns:[{rESTfulReturnedTestReportSteps2.Ext}]";
				return -1;
			}
			return result;
		}
		catch (Exception ex)
		{
			strError = ex.Message;
			isOk = false;
			return -1;
		}
	}

	public int updateRecordTestReportStepsRecord(B_TEST_REPORT_STEPS rptStps, ref bool isOk, ref string strError)
	{
		try
		{
			RESTfulReturnedTestReportSteps rESTfulReturnedTestReportSteps = new RESTfulReturnedTestReportSteps();
			rESTfulReturnedTestReportSteps.objectType = 19;
			List<T_TEST_REPORT_STEPSDTO> list = new List<T_TEST_REPORT_STEPSDTO>();
			list.Add(rptStps);
			rESTfulReturnedTestReportSteps.TestReportSteps = list;
			bool isBSon = false;
			if (rptStps.INFO_PIC != null)
			{
				isBSon = true;
			}
			RESTfulReturnedTestReportSteps rESTfulReturnedTestReportSteps2 = DoPut("MarsEngine/update/TestReportSteps", rESTfulReturnedTestReportSteps, ref isOk, ref strError, isBSon);
			if (!isOk || rESTfulReturnedTestReportSteps2 == null)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't update steps report information";
				}
				return -1;
			}
			if (rESTfulReturnedTestReportSteps2.objectType != 19)
			{
				isOk = false;
				if (!string.IsNullOrEmpty(strError))
				{
					strError = $"returned object type should be report_step_record_update, but it is [{rESTfulReturnedTestReportSteps2.objectType}]";
				}
				return -1;
			}
			int result = -1;
			if (!int.TryParse(rESTfulReturnedTestReportSteps2.Ext, out result))
			{
				isOk = false;
				strError = $"Svc should return a number for Method updateRecordTestReportStepsRecord, but it returns:[{rESTfulReturnedTestReportSteps2.Ext}]";
				return -1;
			}
			return result;
		}
		catch (Exception ex)
		{
			strError = ex.Message;
			isOk = false;
			return -1;
		}
	}

	public int SaveTEST_REPORT_STEPS(B_TEST_REPORT_STEPS rptStps, ref bool isOk, ref string strError, ref string strAdv)
	{
		try
		{
			RESTfulReturnedTestReportSteps rESTfulReturnedTestReportSteps = new RESTfulReturnedTestReportSteps();
			rESTfulReturnedTestReportSteps.objectType = 13;
			List<T_TEST_REPORT_STEPSDTO> list = new List<T_TEST_REPORT_STEPSDTO>();
			list.Add(rptStps);
			rESTfulReturnedTestReportSteps.TestReportSteps = list;
			RESTfulReturnedTestReportSteps rESTfulReturnedTestReportSteps2 = DoPut("MarsEngine/update/TestReportSteps", rESTfulReturnedTestReportSteps, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedTestReportSteps2 == null)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't update steps report information";
				}
				return -1;
			}
			T_TEST_REPORT_STEPSDTO t_TEST_REPORT_STEPSDTO = (rESTfulReturnedTestReportSteps2.TestReportSteps == null) ? null : rESTfulReturnedTestReportSteps2.TestReportSteps.FirstOrDefault();
			if (t_TEST_REPORT_STEPSDTO == null)
			{
				isOk = false;
				strError = "No object is returned from server.";
				strAdv = "Please contact Marquis";
				return -1;
			}
			rptStps.cloneFrom(t_TEST_REPORT_STEPSDTO);
			int result = -1;
			if (!int.TryParse(rESTfulReturnedTestReportSteps2.Ext, out result))
			{
				isOk = false;
				strError = $"Svc should return a number for Method SaveTEST_REPORT_STEPS, but it returns:[{rESTfulReturnedTestReportSteps2.Ext}]";
				return -1;
			}
			return result;
		}
		catch (Exception)
		{
			return -1;
		}
	}

	private T DeserializeToObjectFromResponseString<T>(string strData, ref bool isOk, ref string strError)
	{
		Logger.logBegin("DeserializeToObjectFromResponseString");
		try
		{
			DataContractJsonSerializerSettings dataContractJsonSerializerSettings = new DataContractJsonSerializerSettings
			{
				DateTimeFormat = new DateTimeFormat("s")
			};
			JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
			javaScriptSerializer.MaxJsonLength = 104857600;
			T val = javaScriptSerializer.Deserialize<T>(strData);
			if (val == null)
			{
				isOk = false;
				strError = $"Can't convert data to taget object -[{typeof(T)}]";
				Logger.Error("DeserializeToObjectFromResponseString", strError, 417, "DeserializeToObjectFromResponseString");
				return default(T);
			}
			isOk = true;
			return val;
		}
		catch (Exception ex)
		{
			Logger.Error("DeserializeToObjectFromResponseString", strData, 425, "DeserializeToObjectFromResponseString");
			isOk = false;
			strError = $"Exception:[{ex.Message}]";
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
			string uRLData = GetURLData(strURLWithPara);
			return DeserializeToObjectFromResponseString<T>(uRLData, ref isOk, ref strError);
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = $"Exception:[{ex.Message}]";
			Console.WriteLine("\t{0}\r\n\t{1}", ex.Message, ex.StackTrace);
			return default(T);
		}
	}

	public bool InsertTestStepResultForKeyCompare(long? lRptId, long? stpId, DateTime? beginTime, short iSuccessId, List<KeyValuePair<string, string>> lstObjectNameAndValues, long? dATA_SUMMARY_ID, string strObjectNameIdx, string strRunningError, ref string strError)
	{
		try
		{
			bool isOk = false;
			RESTfulReturnedCaptureData rESTfulReturnedCaptureData = DoPut("MarsEngine/insert/CaputredData", new RESTfulReturnedCaptureData
			{
				objectType = 20,
				CapturedData = new MarsRESTfulCaptureDataInfo(lRptId, stpId, beginTime, iSuccessId, lstObjectNameAndValues, dATA_SUMMARY_ID, strObjectNameIdx, strRunningError)
			}, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedCaptureData == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't Insert captured data and no error returns from [MarsEngine/insert/CaputredData]";
				}
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			strError = $"Exception:[{ex.Message}]\r\n[{ex.StackTrace}]";
			return false;
		}
	}

	internal bool StoryboarTestFullVision_UpdateDepends(IEnumerable<V_STORYBOARD_TEST_FULLVISIONDTO> lstStoryBoardToChange, string strAction, string strDefaultAction2, ref string strError)
	{
		try
		{
			bool isOk = false;
			RESTfulStoryboardTestFullVison rESTfulStoryboardTestFullVison = DoPut("MarsEngine/update/StoryboardFullVision", new RESTfulStoryboardTestFullVison
			{
				objectType = 22,
				actionInfo = strAction,
				action2Info = strDefaultAction2,
				storyboardTestFullVisions = lstStoryBoardToChange
			}, ref isOk, ref strError);
			if (!isOk || rESTfulStoryboardTestFullVison == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't update Depends data and no error returns from [MarsEngine/update/StoryboardFullVision]";
				}
				return false;
			}
			if (rESTfulStoryboardTestFullVison.convertExtToInt(-1L) > 0)
			{
				return true;
			}
			if (string.IsNullOrEmpty(rESTfulStoryboardTestFullVison.ReturnedMessage))
			{
				strError = "can't update Depends and no error returns";
			}
			return false;
		}
		catch (Exception ex)
		{
			strError = $"Exception:[{ex.Message}]\r\n[{ex.StackTrace}]";
			return false;
		}
	}

	private string BuildURL(string strAPI, ref bool isOk, ref string strError)
	{
		try
		{
			string text = "";
			if (WebURLPrefix[WebURLPrefix.Length - 1] != '/')
			{
				text = WebURLPrefix + "/";
			}
			isOk = true;
			return $"{WebURLPrefix}{strAPI}";
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = ex.Message;
			return "";
		}
	}

	public long GetLastestTestMarkID(long? lStoryboardId, ref bool isOk, ref string strError)
	{
		string text = "";
		text = $"MarsEngine/TestMarkIdByStoryboardId?storyboardId={lStoryboardId}";
		text = BuildURL(text, ref isOk, ref strError);
		if (!isOk)
		{
			return -1L;
		}
		RESTfulReturnLastMarkIdObjects dataFromURL = GetDataFromURL<RESTfulReturnLastMarkIdObjects>(text, ref isOk, ref strError);
		if (!isOk)
		{
			return -1L;
		}
		if (dataFromURL.objectType != 6)
		{
			strError = "returned object type is not for LastMarkId";
			return -1L;
		}
		return dataFromURL.LastMarkId;
	}

	public B_REGISTERED_APPS GetApplicationByAppId(long lAppId, ref bool isOk, ref string strError)
	{
		if (string.IsNullOrEmpty(WebURLPrefix))
		{
			isOk = false;
			strError = "no 'MarsEngineSvc_url' find in Configuration.";
			return null;
		}
		string text = BuildURL($"MarsEngine/Application?id={lAppId}", ref isOk, ref strError);
		if (!isOk)
		{
			return null;
		}
		RESTfullReturnApplicationObjects dataFromURL = GetDataFromURL<RESTfullReturnApplicationObjects>(text, ref isOk, ref strError);
		if (!isOk)
		{
			return null;
		}
		if (dataFromURL.AssignedObjects == null || dataFromURL.AssignedObjects.ToList().Count <= 0)
		{
			strError = $"can't get application from [{text}]";
			isOk = false;
			return null;
		}
		isOk = true;
		return dataFromURL.AssignedObjects.FirstOrDefault();
	}

	internal long GetApplicationByAppShortName(string applicationName, ref bool isOk, ref string strError)
	{
		try
		{
			string text = BuildURL($"MarsEngine/Application?applicationShortName={applicationName}", ref isOk, ref strError);
			if (!isOk)
			{
				return -1L;
			}
			RESTfullReturnApplicationObjects dataFromURL = GetDataFromURL<RESTfullReturnApplicationObjects>(text, ref isOk, ref strError);
			if (!isOk)
			{
				return -1L;
			}
			if (dataFromURL.AssignedObjects == null || dataFromURL.AssignedObjects.ToList().Count <= 0)
			{
				strError = $"can't get application from [{text}]";
				isOk = false;
				return -1L;
			}
			isOk = true;
			B_REGISTERED_APPS b_REGISTERED_APPS = dataFromURL.AssignedObjects.FirstOrDefault();
			if (b_REGISTERED_APPS == null)
			{
				strError = $"no such [{applicationName}] is registered";
				isOk = false;
				return -1L;
			}
			return b_REGISTERED_APPS.APPLICATION_ID;
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = $"Exception :[{ex.Message}]";
			return -1L;
		}
	}

	public List<V_TEST_STEPS_FULLVISIONDTO> GetTestStepsByTestCaseID(long iTestCaseId, long lAppId, ref bool isOk, ref string strError, ref string strStack, ref string strAdv)
	{
		try
		{
			strError = "";
			string strURLWithPara = BuildURL($"MarsEngine/TestcaseSteps?testCaseId={iTestCaseId}&strAppId={lAppId}", ref isOk, ref strError);
			if (!isOk)
			{
				return null;
			}
			RESTfulReturnedVTestCaseSteps dataFromURL = GetDataFromURL<RESTfulReturnedVTestCaseSteps>(strURLWithPara, ref isOk, ref strError);
			if (!isOk || dataFromURL == null)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object is null";
				}
				return null;
			}
			if (dataFromURL.objectType == 1)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object type is Error";
					strError = dataFromURL.ReturnedMessage;
					strStack = dataFromURL.StackTrace;
					strAdv = "Contact Marquis";
				}
				isOk = false;
				return null;
			}
			return (dataFromURL.TestStepsForTestcase == null) ? null : dataFromURL.TestStepsForTestcase.ToList();
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = $"Exception:[{ex.Message}]";
			return new List<V_TEST_STEPS_FULLVISIONDTO>();
		}
	}

	public int SaveStoryBoardTestResult(B_PROJ_TEST_RESULT storyBoardTestResult, ref bool isOk, ref string strError)
	{
		try
		{
			RESTfulProjTestResult rESTfulProjTestResult = DoPut("MarsEngine/save/ProjectTestResult", new RESTfulProjTestResult
			{
				Proj_Test_Results = new List<B_PROJ_TEST_RESULT>
				{
					storyBoardTestResult
				}
			}, ref isOk, ref strError, false, true);
			if (!isOk || rESTfulProjTestResult == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't update Depends data and no error returns from [MarsEngine/update/Storybard]";
				}
				return -1;
			}
			if (rESTfulProjTestResult.convertExtToInt(-1L) > 0)
			{
				return (int)rESTfulProjTestResult.convertExtToInt(-1L);
			}
			if (string.IsNullOrEmpty(rESTfulProjTestResult.ReturnedMessage))
			{
				strError = "can't update Depends and no error returns";
			}
			return -1;
		}
		catch (Exception ex)
		{
			Logger.Error("\t", ex.Message, ex, 730, "SaveStoryBoardTestResult");
			strError = ex.Message;
			return -1;
		}
	}

	public IList<KeyValuePair<long?, TEST_DATA_SETTINGDTO>> GetTestDataByTestCaseIDAndDataSetId(long lTestCase, long lDBSetId, ref bool isOk, ref string strError)
	{
		try
		{
			strError = "";
			string strURLWithPara = BuildURL($"MarsEngine/TestData?testCaseId={lTestCase}&&datasetId={lDBSetId}", ref isOk, ref strError);
			if (!isOk)
			{
				return null;
			}
			RESTfulReturnedTestData dataFromURL = GetDataFromURL<RESTfulReturnedTestData>(strURLWithPara, ref isOk, ref strError);
			if (!isOk || dataFromURL == null)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object is null";
				}
				return null;
			}
			if (dataFromURL.objectType == 1)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object type is Error";
				}
				isOk = false;
				return null;
			}
			isOk = true;
			return (dataFromURL.TestDataSetWithDBSetId == null) ? null : new MarsRESTKeyValuePair<long?, TEST_DATA_SETTINGDTO>().toKeyValuePairList(dataFromURL.TestDataSetWithDBSetId.ToList());
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = $"Excepiton:[{ex.Message}]";
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
			string text = BuildURL("MarsEngine/updateSelf/SystemLookup", ref isOk, ref strError);
			if (!isOk)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't build URL for insert/SystemLookup";
				}
				return false;
			}
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup = DoPut(text, new RESTfulReturnedSystemLookup
			{
				objectType = 12,
				SystemLookups = new List<B_SYSTEM_LOOKUP>
				{
					systemLookUp
				}
			}, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedSystemLookup == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = $"can't update data from URL [{text}]";
				}
				isOk = false;
				return false;
			}
			return rESTfulReturnedSystemLookup.objectType == 12;
		}
		catch (Exception ex)
		{
			strError = $"Exception:[{ex.Message}]";
			return false;
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
			bool isOk = false;
			string text = BuildURL("MarsEngine/insert/SystemLookupWithStatus", ref isOk, ref strError);
			if (!isOk)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't build URL for insert/SystemLookup";
				}
				return false;
			}
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup = DoPut(text, new RESTfulReturnedSystemLookup
			{
				objectType = 12,
				SystemLookups = new List<B_SYSTEM_LOOKUP>
				{
					systemLookUp
				}
			}, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedSystemLookup == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = $"can't update data from URL [{text}]";
				}
				isOk = false;
				return false;
			}
			return rESTfulReturnedSystemLookup.objectType == 12;
		}
		catch (Exception ex)
		{
			strError = $"Exception:[{ex.Message}]";
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
			bool isOk = false;
			string text = BuildURL("MarsEngine/insert/SystemLookup", ref isOk, ref strError);
			if (!isOk)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't build URL for insert/SystemLookup";
				}
				return false;
			}
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup = DoPut(text, new RESTfulReturnedSystemLookup
			{
				objectType = 12,
				SystemLookups = new List<B_SYSTEM_LOOKUP>
				{
					systemLookUp
				}
			}, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedSystemLookup == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = $"can't update data from URL [{text}]";
				}
				isOk = false;
				return false;
			}
			return rESTfulReturnedSystemLookup.objectType == 12;
		}
		catch (Exception ex)
		{
			strError = $"Exception:[{ex.Message}]";
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
			string text = BuildURL("MarsEngine/update/TestResult", ref isOk, ref strError);
			if (!isOk)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "can't build URL for insert/SystemLookup";
				}
				return -1;
			}
			RESTfulProjTestResult rESTfulProjTestResult = DoPut(text, new RESTfulProjTestResult
			{
				objectType = 12,
				Proj_Test_Results = new List<B_PROJ_TEST_RESULT>
				{
					storyBoardTestResult
				}
			}, ref isOk, ref strError);
			if (!isOk || rESTfulProjTestResult == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = $"can't update data from URL [{text}]";
				}
				isOk = false;
				return -1;
			}
			return (int)rESTfulProjTestResult.convertExtToInt(-1L);
		}
		catch (Exception ex)
		{
			strError = $"Exception:[{ex.Message}]";
			return -1;
		}
	}

	public bool GetBussinessSeq(ref long iN, ref bool isOk, ref string strError, string strSeqName = "T_KEYWORD_SEQ")
	{
		try
		{
			strError = "";
			string strURLWithPara = BuildURL($"MarsEngine/MarsSequence?seqName={strSeqName}", ref isOk, ref strError);
			RESTfulReturnedSeqNumber dataFromURL = GetDataFromURL<RESTfulReturnedSeqNumber>(strURLWithPara, ref isOk, ref strError);
			if (dataFromURL.objectType == 1)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object is null";
				}
				return false;
			}
			if (dataFromURL.objectType != 9)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object type is Error";
				}
				isOk = false;
				return false;
			}
			isOk = true;
			iN = (int)dataFromURL.SeqNumber;
			return true;
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = $"Exception:{ex.Message}";
			return false;
		}
	}

	public List<B_V_OBJECT_SNAPSHOT> GetObjectInfoByAppId(long lAppId, ref bool isOk, ref string strError)
	{
		try
		{
			strError = "";
			string strURLWithPara = BuildURL($"MarsEngine/objectInfoByAppIdAnd?appId={lAppId}", ref isOk, ref strError);
			RESTfulReturnedObjects dataFromURL = GetDataFromURL<RESTfulReturnedObjects>(strURLWithPara, ref isOk, ref strError);
			if (dataFromURL.objectType == 1)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object is null";
				}
				return null;
			}
			if (dataFromURL.objectType != 10)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object type is Error";
				}
				isOk = false;
				return null;
			}
			isOk = true;
			return (dataFromURL.Objects == null) ? null : dataFromURL.Objects.ToList();
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = $"Exception:{ex.Message}";
			return null;
		}
	}

	public List<B_SYSTEM_LOOKUP> GetSystemLookups(string strTableName, List<string> lstFieldName, ref bool isOk, ref string strError)
	{
		try
		{
			string text = BuildURL(string.Format("MarsEngine/get/Variables", strTableName), ref isOk, ref strError);
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup = new RESTfulReturnedSystemLookup();
			rESTfulReturnedSystemLookup.objectType = 12;
			rESTfulReturnedSystemLookup.SystemLookups = new List<B_SYSTEM_LOOKUP>();
			foreach (string item in lstFieldName)
			{
				if (item != null)
				{
					((List<B_SYSTEM_LOOKUP>)rESTfulReturnedSystemLookup.SystemLookups).Add(new B_SYSTEM_LOOKUP
					{
						TABLE_NAME = strTableName,
						FIELD_NAME = item
					});
				}
			}
			if (((List<B_SYSTEM_LOOKUP>)rESTfulReturnedSystemLookup.SystemLookups).Count <= 0)
			{
				isOk = true;
				return null;
			}
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup2 = DoPut("MarsEngine/get/Variables", rESTfulReturnedSystemLookup, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedSystemLookup2 == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = string.Format("Can't create modal variables for [{0}]", string.Join(",", lstFieldName));
				}
				return null;
			}
			isOk = true;
			return (rESTfulReturnedSystemLookup2.SystemLookups == null) ? null : rESTfulReturnedSystemLookup2.SystemLookups.ToList();
		}
		catch (Exception ex)
		{
			strError = ex.Message;
			isOk = false;
			return null;
		}
	}

	internal List<B_SYSTEM_LOOKUP> GetSystemLookup(string strTableName, string strFieldName, ref bool isOk, ref string strError)
	{
		try
		{
			string text = BuildURL($"MarsEngine/SystemLookup?tableName={strTableName}&fieldName={strFieldName}", ref isOk, ref strError);
			RESTfulReturnedSystemLookup dataFromURL = GetDataFromURL<RESTfulReturnedSystemLookup>(text, ref isOk, ref strError);
			if (!isOk)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = $"Error when get from {text}";
				}
				return null;
			}
			if (dataFromURL.objectType != 12)
			{
				isOk = false;
				strError = $"returned package type is not SystemLookup, its id is {dataFromURL.objectType}";
				return null;
			}
			return (dataFromURL.SystemLookups == null) ? null : dataFromURL.SystemLookups.ToList();
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = ex.Message;
			return null;
		}
	}

	internal Dictionary<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>> LoadAllKeywords(ref bool isOk, ref string strError)
	{
		try
		{
			strError = "";
			string strURLWithPara = BuildURL(string.Format("MarsEngine/Keywords?keywordName={0}", ""), ref isOk, ref strError);
			RESTfulReturnedKeywords dataFromURL = GetDataFromURL<RESTfulReturnedKeywords>(strURLWithPara, ref isOk, ref strError);
			if (dataFromURL.objectType == 1)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object is null";
				}
				return null;
			}
			if (dataFromURL.objectType != 11)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = "returned object type is Error";
				}
				isOk = false;
				return null;
			}
			isOk = true;
			return (dataFromURL.KeywordsWithDicRel == null) ? null : new MarsRESTKeyValuePair<T_KEYWORDDTO, List<T_DIC_RELATION_KEYWORDDTO>>().toDictionary(dataFromURL.KeywordsWithDicRel.ToList());
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = $"Exception:{ex.Message}";
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
			RESTfulReturnedSystemLookup objToSend = new RESTfulReturnedSystemLookup
			{
				objectType = 18,
				SystemLookups = new List<B_SYSTEM_LOOKUP>
				{
					new B_SYSTEM_LOOKUP
					{
						TABLE_NAME = "LOOP_VAR",
						FIELD_NAME = strObjectIdx,
						DISPLAY_NAME = strDataToStore
					}
				}
			};
			bool isOk = false;
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup = DoPut("MarsEngine/insertOrUpdate/LoopVariable", objToSend, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedSystemLookup == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = $"Can't create or update loop variables for [{strObjectIdx}]-[{strDataToStore}]";
				}
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			strError = $"CreateOrUpdateLoopVar Exception:[{ex.Message}]\r\n[{ex.StackTrace}]";
			return false;
		}
	}

	internal int CreateModualVar(List<string> lstName, ref bool isOk, ref string strError, short? sStatus)
	{
		try
		{
			if (lstName == null || lstName.Count <= 0)
			{
				isOk = true;
				return 0;
			}
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup = new RESTfulReturnedSystemLookup();
			rESTfulReturnedSystemLookup.objectType = 12;
			rESTfulReturnedSystemLookup.SystemLookups = new List<B_SYSTEM_LOOKUP>();
			foreach (string item in lstName)
			{
				if (item != null)
				{
					((List<B_SYSTEM_LOOKUP>)rESTfulReturnedSystemLookup.SystemLookups).Add(new B_SYSTEM_LOOKUP
					{
						TABLE_NAME = "MODAL_VAR",
						FIELD_NAME = item
					});
				}
			}
			if (((List<B_SYSTEM_LOOKUP>)rESTfulReturnedSystemLookup.SystemLookups).Count <= 0)
			{
				isOk = true;
				return 0;
			}
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup2 = DoPut("MarsEngine/update/ModualVariables", rESTfulReturnedSystemLookup, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedSystemLookup2 == null)
			{
				if (string.IsNullOrEmpty(strError))
				{
					strError = string.Format("Can't create modal variables for [{0}]", string.Join(",", lstName));
				}
				return -1;
			}
			long num = rESTfulReturnedSystemLookup2.convertExtToInt(-1L);
			return (int)num;
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = ex.Message;
			return -1;
		}
	}

	internal List<B_SYSTEM_LOOKUP> GetVariableInfo(string strVarIndex, ref bool isOk, ref string strError, ref string strResult, string strVarType = "GLOBAL_VAR")
	{
		try
		{
			if (string.IsNullOrEmpty(strVarIndex))
			{
				strError = "no global Variable information.";
				isOk = false;
				return null;
			}
			int num = -1;
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup = new RESTfulReturnedSystemLookup();
			string text = "";
			if (string.Compare(strVarType, "GLOBAL_VAR", true) == 0)
			{
				rESTfulReturnedSystemLookup.objectType = 17;
				rESTfulReturnedSystemLookup.SystemLookups = new List<B_SYSTEM_LOOKUP>
				{
					new B_SYSTEM_LOOKUP
					{
						TABLE_NAME = "GLOBAL_VAR",
						FIELD_NAME = strVarIndex
					}
				};
				text = "MarsEngine/update/GlobalVariable";
				num = 17;
			}
			else
			{
				if (string.Compare(strVarType, "LOOP_VAR", true) != 0)
				{
					isOk = false;
					strError = $"unsupported var type:[{strVarType}]";
					return null;
				}
				rESTfulReturnedSystemLookup.objectType = 18;
				rESTfulReturnedSystemLookup.SystemLookups = new List<B_SYSTEM_LOOKUP>
				{
					new B_SYSTEM_LOOKUP
					{
						TABLE_NAME = "LOOP_VAR",
						FIELD_NAME = strVarIndex
					}
				};
				text = "MarsEngine/update/LoopVariable";
				num = 18;
			}
			RESTfulReturnedSystemLookup rESTfulReturnedSystemLookup2 = DoPut(text, rESTfulReturnedSystemLookup, ref isOk, ref strError);
			if (!isOk || rESTfulReturnedSystemLookup2 == null || rESTfulReturnedSystemLookup2.SystemLookups == null || rESTfulReturnedSystemLookup2.objectType != num)
			{
				isOk = false;
				if (string.IsNullOrEmpty(strError))
				{
					strError = $"Can't get and create modal variables for [{strVarIndex}]";
				}
				return null;
			}
			isOk = true;
			return rESTfulReturnedSystemLookup2.SystemLookups.ToList();
		}
		catch (Exception ex)
		{
			isOk = false;
			strError = ex.Message;
			return null;
		}
	}
}
