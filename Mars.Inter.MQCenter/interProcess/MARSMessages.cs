//using Mars.Inter.MQCenter.interProcess;
#if MESSAGESVC_FROM_GUI
using Route2NSEx.src.Marquis.systemUtil;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using Mars.Inter.MQCenter.interProcess;
using MarsCore.MessageCenter;


#if _NET4
using System.Threading.Tasks;
using System.Diagnostics;
using Mars.message.AutoTestingDriver.ErrorMessage;
#endif
using Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData;

#if _MarsWeb
namespace MARSWebDriver
#else
namespace Mars.message.AutoTestingDriver.interProcess
#endif
{

    

    [Serializable]
    public enum MARSMessageType
    {
        e_TypeUnknow = 0x00,
        e_Get_SessionId = 0x01,
        e_Set_SessionId,
        e_Get_HeartBeat,
        e_Set_HeartBeat,
        e_RequestObject,
        e_Run_TestStep,
        e_Run_TestStep_Result,
        e_Operation_FromClient,
        e_Operation_FromServer, 
        e_CreateObjectFromSpyer
    }

    [Serializable]
    public enum MARSOperationType
    {
        e_sendkeys = 0x00, 
        e_mouseLeftClick,
        e_mouseLeftDoubleClick, 
        e_mouseRightClick, 
        e_hightlight, 
    }
    [Serializable]
    public enum MARSStepResult
    {
        e_Result_unsigned = 0,
        e_Result_Ok = 1,
        e_Result_Failed = 2,
        e_Result_unKnow = 3
    }

    [Serializable]
    [XmlRoot(ElementName = "MARSMessage")]
    public class MARSMessagesBase
    {
#if MESSAGESVC_FROM_GUI
        private static MLogger Logger = MLogger.GetLogger(typeof(MARSMessagesBase));
#endif
        public MARSMessageType MessageType;
        public long SeriousNumber;
        public string SessiongId = CurrentSessionId;
        public string LocalSessionId = Guid.NewGuid().ToString(); //每个包都有一个唯一的id

        public void RegenGUID()
        {
            LocalSessionId = Guid.NewGuid().ToString();
        }

        public MARSMessagesBase()
        {
            SeriousNumber = DateTime.Now.Ticks;
        }
        private static Guid currentSessionId;
        public static string CurrentSessionId
        {
            get
            {
                if (currentSessionId == null)
                    currentSessionId = Guid.NewGuid();
                return currentSessionId.ToString();
            }
        }

        public static MARSMessagesBase GetMsgObjViaRawXmlDoc(XmlDocument sourcXml, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            isOk = false;
            if (sourcXml == null)
            {
                strError = "Passing object null to a function";
                //strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure the file ";
                return null;
            }
            XmlSerializer serializer = null;

            if (string.Compare(sourcXml.DocumentElement.Name, "MARSTestStep", true) == 0)
            {
                serializer = new XmlSerializer(typeof(MARSTestStep));
                try
                {
                    XmlReader xr = new XmlNodeReader(sourcXml);
                    isOk = true;
                    return (MARSTestStep)serializer.Deserialize(xr);
                }
                catch (Exception e)
                {
#if MESSAGESVC_FROM_GUI
                    Logger.Error("GetMsgObjViaRawXmlDoc", strError = string.Format("MARSTestStep convert from Message Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
#else
#if _NOQTP
                    Inter.MQCenter.simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("MARSTestStep convert from Message Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
#else
                    strError = "Error while getting inforamtion from Xml files";// e.Message;
                    StackFrame stck = new StackFrame();
                    strStack = e.StackTrace;
                    strAdv = "Contact Marquis";
#endif
#endif
                    return null;
                }
            }

            if (string.Compare(sourcXml.DocumentElement.Name, "MARSMessageHeartBeat", true) == 0)
            {
                serializer = new XmlSerializer(typeof(MARSMessageHeartBeat));
                try
                {
                    XmlReader xr = new XmlNodeReader(sourcXml);
                    isOk = true;
                    return (MARSMessageHeartBeat)serializer.Deserialize(xr);
                }
                catch (Exception e)
                {
#if MESSAGESVC_FROM_GUI
                    Logger.Error("GetMsgObjViaRawXmlDoc", strError = string.Format("MARSMessageHeartBeat Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
#else
#if _NOQTP
                    Inter.MQCenter.simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("MARSMessageHeartBeat Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
#endif
#endif
                    return null;
                }
            }

            isOk = false;
#if MESSAGESVC_FROM_GUI
            Logger.Error("GetMsgObjViaRawXmlDoc", strError = string.Format("Unsupported type:[{0}]", sourcXml.DocumentElement.Name));
#else
#if _NOQTP
            Inter.MQCenter.simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Unsupported type:[{0}]", sourcXml.DocumentElement.Name), sourcXml.InnerXml);
#endif
#endif
            return null;
        }
    }

    [Serializable]
    [XmlRoot(MARSMessageHeartBeat.HeartBeat_className)]
    public class MARSMessageHeartBeat : MARSMessagesBase
    {
        public const string HeartBeat_className = "MARSMessageHeartBeat";

        public DateTime AskTime;
        public DateTime AckTime;
    }

    [Serializable]
    [XmlRoot(MARSDealResult.CLASS_NAME)]
    public class MARSDealResult : MARSMessageHeartBeat
    {
        public const string CLASS_NAME = "MARSDealResult";
        private int checkObjectWaitingTime = -1;
        public const string CNST_SUCCESS = "SUCCESS";
        public int CheckObjectWaitingTime
        {
            get => checkObjectWaitingTime;
            set => checkObjectWaitingTime = value;
        }
        protected int ResultType;
        public string ResultMessage
        {
            get { return ResultType == 1 ? "SUCCESS" : "FAILED"; }
            set
            {
                if ((string.Compare(value ?? "", "OK", true) == 0) || (string.Compare(value ?? "", "SUCCESS", true) == 0))
                {
                    ResultType = 1;
                }
                else
                {
                    ResultType = 0;
                }
            }
        }
        public string ErrorMessage
        {
            get;
            set;
        }
        public override string ToString()
        {
            return string.Format("ResultMessage {0} ErrorMessage:[{1}]", ResultMessage, ErrorMessage);
        }
        public bool IsResultSucess
        {
            get { return ResultType == 1; }
        }

        public string ReturnedData { get; set; }
        public string ActualInputData;
        public string snapshotFilePath;

        public string StackInfo { get; set; }
        public string Advice { get; set; }
        public string SnapshotFileNameWhenErrorOccurs { get; set; }

        public MARSDealResult() : base()
        {
            AckTime = DateTime.Now;
        }
    }

    [Serializable]

    public class MARSMessageObject
    {
        public string HappyName;
        [XmlElement(ElementName = "ObjectProperties", IsNullable = true)]
        public MarsDictionary ObjectIDPropertiesAndValue = new MarsDictionary();
        [XmlElement(ElementName = "RuntimeObjectProp", IsNullable = true)]
        public MarsDictionary RunTimeObjectInfo = new MarsDictionary();
        public string ObjectType;

        public MARSMessageObject()
        {
            //ObjectIDPropertiesAndValue = new MarsDictionary();
        }
    }

    [Serializable]
    public class MARSMessageOperateObjectWithData : MARSMessageObject
    {
        [XmlElement("PegProperties")]
        public MARSMessageObject PegWindow;
        [XmlElement("ObjProperties")]
        public MARSMessageObject TargetObject;
        public MARSMessageOperateObjectWithData() : base()
        {
            PegWindow = new MARSMessageObject();
            TargetObject = new MARSMessageObject();
        }
    }

    [Serializable]
    [XmlRoot("MARSOperate")]
    public class MARSTestOperation: MARSMessageHeartBeat
    {
        [XmlElement("OperationType")]
        public MARSOperationType OperationType; //0- keys send and wait, 1 - mouse left click , 2
        [XmlElement("Top")]
        public int Top;
        [XmlElement("Left")]
        public int Left;
        [XmlElement("Width")]
        public int Width;
        [XmlElement("Height")]
        public int Height;
        [XmlElement("AttachData")]
        public string AttachData;
    }


    [Serializable]
    [XmlRoot("MARSTestStep")]
    public class MARSTestStep : MARSMessageHeartBeat
    {
        [XmlElement("Keyword")]
        public string Keyword;
        [XmlElement("TestStepObjectInformation")]
        public MARSMessageOperateObjectWithData TestStepObjectInformation;
        [XmlElement("Parameters")]
        public string Parameters;
        [XmlElement("DataToSet")]
        public string DataToSet;
        [XmlElement("ObjectType")]
        public string ObjectType;
        [XmlElement("AttachInfo")]
        public string AttachInfo;  //如果是capture 或者capturecompare, 该字段返回结果
        [XmlElement("RunId")]
        public long RunId;
        [XmlElement("IsSkip")]
        public bool IsSkip;
        [XmlElement("TestResult")]
        public MARSStepResult TestResult;
        [XmlElement("RuntimeResult")]
        public string RuntimeResult;//if error, then it stores error message

        [XmlElement("TestStepObjectPreviewInformation")]
        public MARSMessageOperateObjectWithData TestStepObjectPreviewInformation;
        [XmlElement("IsCheckPreviewObject")]
        public bool isCheckPreViewObject;

        [XmlElement("StackTrace")]
        public string stackTrace;
        [XmlElement("AdviceToUser")]
        public string advice2User;
        [XmlElement("SnapshotFileNameWhenErrorOccurs")]
        public string snapshotFileNameWhenErrorOccurs;

        [XmlElement("PegwindowName")]
        public string pegWindowName;
        [XmlElement("ObjectName")]
        public string objectName;
        [XmlElement("WaitingTime")]
        public int WaitingTime;
        [XmlElement("ErrorCheckObject")]
        public MarsErrorCheckData errorCheckObj;

        public MARSTestStep() : base()
        {
            this.MessageType = MARSMessageType.e_Run_TestStep;
            TestStepObjectInformation = new MARSMessageOperateObjectWithData();
        }

        public override string ToString()
        {
            return string.Format("{0}-(\"{1}\",\"\"{2}\")\r\n\tObject Type:[{3}]\r\ntestResult:{4}-{5}|{6}", Keyword, Parameters, DataToSet,
                ObjectType,
                TestResult, RuntimeResult,AttachInfo
                );
        }
    }

    public interface IMarsMessageAgent
    {
        MARSMessagesBase peakMarsMessage(ref bool isOk, ref string strError);

        void DoLog(string strMethodName, string strLevel, string strTextToLog, object extendForExcetpion);
        bool DealWithMessage(out MARSDealResult objResult, int iWaitTime=200);
    }


}
