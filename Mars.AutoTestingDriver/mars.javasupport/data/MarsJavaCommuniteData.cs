using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.mars.javasupport.data
{

    public class MarsJavaDataPackageTypeConst
    {
        public const string CNST_STEP_REQUEST = "MARS_STEP_REQUEST";
        public const string CNST_STES_RESPONSE = "MARS_STEP_RESPONSE";
        public const string PckageType_StepResponseError = "MARS_STEP_RSPN_ERROR";
	    public const string PackageType_HeartBeat 		 = "MARS_HEARTBEAT" 	 ;
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class MarsJavaObjIDInfo
    {
        public string idKey { get; set; }
        public string idValue { get; set; }
    }

    public class MarsJavaDataPackageBase
    {
        public string packageType { get; set; }
        public string uuid { get; set; } = Guid.NewGuid().ToString();
        public string ackTime { get; set; }
        
    }
    
    public class MarsJavaCommuniteTestStep: MarsJavaDataPackageBase
    {
        
        public string keyword { get;set; }
        public string objectHappyName { get; set; }
        public string pegName { get; set; }
        public bool flashObj { get; set; }
        public bool flashPeg {
            get; set;
        }
        public int stepId {
            get;set;
        }
        
        public List<MarsJavaObjIDInfo> pegObject { get; set; }
        public List<MarsJavaObjIDInfo> obj { get; set; }
        public string parameter { get; set; }
        public string dataToOp { get; set; }
    }

    public class MarsJavaCommunitTestStepRspns: MarsJavaDataPackageBase
    {
        public string testResult { get; set; } /// OK, failed, success
        public string errorMessage { get; set; } /// if error happend
        public string returnedData { get; set; } /// data for capture and compare
    }

}
