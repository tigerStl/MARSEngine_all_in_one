using Mars.AutoTestingDriver.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.mars.javasupport.data
{
    internal class MarsJavaDataMgr
    {
        public MarsJavaCommuniteTestStep currentTestStepRequest { get; set; }

        public string CreateObjectStringToJavaEngine(Dictionary<string,string> pegObj, 
            Dictionary<string, string> objInfo, 
            string pegName,
            string objHappyName,
            string strPara, string strData,
            string strKeyword, string uuid,
            int stepId,
            bool isPeg =false,
            bool flashObj= false,
            bool flashPeg= false)
        {
            MarsJavaCommuniteTestStep javaObj = new MarsJavaCommuniteTestStep();
            javaObj.parameter = strPara;
            javaObj.uuid = string.IsNullOrEmpty(uuid)?System.Guid.NewGuid().ToString():uuid;
            javaObj.stepId = stepId;
            javaObj.keyword = strKeyword;
            javaObj.objectHappyName = objHappyName;
            javaObj.dataToOp = strData;
            javaObj.obj = convertFromDictionary(objInfo);
            javaObj.pegName = pegName;
            javaObj.flashObj = flashObj;
            javaObj.flashPeg = flashPeg;
            javaObj.packageType = MarsJavaDataPackageTypeConst.CNST_STEP_REQUEST;
            javaObj.pegObject = convertFromDictionary(pegObj);
            javaObj.ackTime = DateTime.Now.ToString("yyyyMMdd HHmmss:fff");
            if ((isPeg) && (javaObj.obj == null))
            {
                javaObj.obj = javaObj.pegObject;
            }
            currentTestStepRequest = javaObj;
            return System.Text.Json.JsonSerializer.Serialize(javaObj);
        }

        private List<MarsJavaObjIDInfo> convertFromDictionary(Dictionary<string, string> objInfo)
        {
            if (objInfo == null) return null;
            List<MarsJavaObjIDInfo> result = new List<MarsJavaObjIDInfo> ();
            foreach(var k in objInfo.Keys)
            {
                if (string.IsNullOrEmpty(k)) continue;
                MarsJavaObjIDInfo itm = new MarsJavaObjIDInfo()
                {
                    idKey = k,
                    idValue = objInfo[k]
                };
                result.Add(itm);
            }
            return result;
        }

        public MarsJavaDataPackageBase checkResponseData(string strDataFromSocket, ref bool isOk, ref string strError)
        {
            try
            {
                MarsJavaCommunitTestStepRspns baseData = System.Text.Json.JsonSerializer.Deserialize<MarsJavaCommunitTestStepRspns>(strDataFromSocket);
                if ((baseData == null)||(string.IsNullOrEmpty(baseData.packageType)))
                {
                    isOk = false;
                    strError = Resources.mars_websocket_reponsedata_wrong_format;
                    return null;
                }

                isOk = true;
                return baseData;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                return null;
            }
        }
    }
}
