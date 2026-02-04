using Mars.AutoTestingDriver.MarsHelpers;
using Mars.message.AutoTestingDriver.interProcess;
using MarsEngine.MarsSocketSvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mars.AutoTestingDriver.MarsMessageCenter
{
    public class MarsMessageCenterStub
    {
        public static string baseURL { get; set; } = "http://localhost";
        public static int messageCenterPort { get; set; } = 10000;
        internal static bool DoKeywordsByImageStepInfo(MARSTestStep testStep, ref string strSourceFile, ref string strError)
        {
            using (HttpClient client = new HttpClient())
            {
                MarsImageModeTestStepRequest marsImageModeTestStepRequest = new MarsImageModeTestStepRequest();
                if ((testStep.TestStepObjectInformation == null) || (testStep.TestStepObjectInformation.PegWindow == null)
                    || (testStep.TestStepObjectInformation.PegWindow.ObjectIDPropertiesAndValue == null)
                    || (testStep.TestStepObjectInformation.PegWindow.ObjectIDPropertiesAndValue.Items == null)
                    || (testStep.TestStepObjectInformation.PegWindow.ObjectIDPropertiesAndValue.Items.Count<=0))
                {
                    strError = "TestStepObjectInformation or PegWindow is null";
                    return false;
                }
                marsImageModeTestStepRequest.pegWindowProperties = testStep.TestStepObjectInformation.PegWindow.ObjectIDPropertiesAndValue;
                if ((testStep.TestStepObjectInformation.TargetObject== null)||(testStep.TestStepObjectInformation.TargetObject.ObjectIDPropertiesAndValue==null))
                {
                    strError = "TargetObject is null";
                    return false;
                }
                marsImageModeTestStepRequest.objectProperties = testStep.TestStepObjectInformation.TargetObject.ObjectIDPropertiesAndValue;

                var screen = MarsScreenHelper.GetProcessMainWindowScreen();
                if (screen != null)
                {
                    marsImageModeTestStepRequest.screenId = screen.DeviceName;
                }
                else
                {
                    // 使用主屏幕
                    marsImageModeTestStepRequest.screenId = Screen.AllScreens[0].DeviceName;
                }
                marsImageModeTestStepRequest.keyword = testStep.Keyword;
                marsImageModeTestStepRequest.parameters = testStep.Parameters;
                marsImageModeTestStepRequest.stepId = testStep.RunId+"";
                marsImageModeTestStepRequest.data = testStep.DataToSet;
                /// 发送json请求送到
                string strUrl = $"{baseURL}:{messageCenterPort}" + MarsSocketSvcConstant.request_message_center_do_image_test_step;
                var json = System.Text.Json.JsonSerializer.Serialize(marsImageModeTestStepRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync(strUrl, content).Result;
                if (response != null) { 
                    response.EnsureSuccessStatusCode();
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var responseBody = response.Content.ReadAsStringAsync().Result;
                        if (string.IsNullOrEmpty(responseBody))
                        {
                            strError = "response body is null";
                            return false;
                        }
                        else
                        {
                            var result = System.Text.Json.JsonSerializer.Deserialize<MarsImageModeTestStepResponse>(responseBody);
                            if (result == null)
                            {
                                strError = "response is null";
                                return false;
                            }
                            if (!result.status)
                            {
                                strError = result.Message;
                                return false;
                            }
                            /// 暂时不处理
                            /// 
                            strSourceFile = result.TestStepSnapshotFile;
                            return true;
                        }
                    }
                    else
                    { // response.StatusCode != System.Net.HttpStatusCode.OK
                        strError = $"response status code is not OK|{response.StatusCode}";
                        return false;
                    }                    
                }
                else
                {
                    strError = "response is null";
                    return false;
                }
            }
        }
    }
}
