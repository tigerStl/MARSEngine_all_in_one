using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;
#if !_MarsInOtherProcess
using com.Mars.Constants;
#endif
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace MarsTestFrame.CommuniteServer
{

#if _TestStepUnit
    [DataContract]
    public enum MarsFrameWorkServicesMode
    {
        _svcmode_Default = 0x01,
        _svcmode_TestStep = 0x02
    }

    public class MARSTigerSystemConstantsWCF
    {
        public const string CNST_COMMAND_TEST_UNIT = "TEST_TESTSTEP";
        public const string CNST_COMMAND_TEST_STORYBOARD = "TEST_STORYBOARD";
        public const string CNST_COMMAND_TEST_AUTO_GENTEST = "TEST_AUTO_GENTEST";

        public const string CNST_COMMAND_DIRECTR_RUN = "DIRECT_RUN";
    }
#endif

#if !_MarsInOtherProcess
    /// <summary>
    /// 这里数据结构和V_OBJECT_SNAPSHOTDTO一样。之所以不用V_OBJECT_SNAPSHOTDTO，
    /// 因为需要引用另外一个包，没有必要
    /// </summary>
    [DataContract]
    public class TestStepObject
    {


        [DataMember()]
        public Nullable<Int64> OBJECT_NAME_ID { get; set; }

        [DataMember()]
        public String OBJECT_HAPPY_NAME { get; set; }

        [DataMember()]
        public String COMMENT { get; set; }

        [DataMember()]
        public Nullable<Int64> APPLICATION_ID { get; set; }

        [DataMember()]
        public String ENUM_TYPE { get; set; }

        [DataMember()]
        public Int64 OBJECT_ID { get; set; }

        [DataMember()]
        public String OBJECT_TYPE { get; set; }

        [DataMember()]
        public String QUICK_ACCESS { get; set; }

        [DataMember()]
        public Nullable<Int16> TYPE_ID { get; set; }

        [DataMember()]
        public String PEG_NAME { get; set; }

        [DataMember()]
        public String PEG_QUICK_ACCESS { get; set; }

        [DataMember()]
        public Nullable<Int64> PEG_ID { get; set; }

    }


    [DataContract]
    public class TestSuiteRunStatusInfo
    {
        [DataMember]
        public string RunResult;//true or false
        [DataMember]
        public string CauseReason;//true or false
        [DataMember]
        public string StartTime;//true or false
        [DataMember]
        public string EndTime;//true or false
    }

    [DataContract]
    public class TestStepRunningRecorder
    {
        /// <summary>
        /// Added on 3-2-2016 
        /// For TestStep Recorder
        /// </summary>
        [DataMember]
        public DateTime StartTime;
        [DataMember]
        public DateTime EndTime;
        [DataMember]
        public string CauseReason;
        [DataMember]
        public int RunResult;
        [DataMember]
        public int LoopId;
        [DataMember]
        public short SaveToServerId;
        [DataMember]
        public long assignedStepId;

    }

    [DataContract]
    public class TestStep4Services
    {
        #region properties
        [DataMember]
        public int RunID;
        [DataMember]
        public string ObjectName;
        [DataMember]
        public string Keyword;
        [DataMember]
        public string Row_Column;
        [DataMember]
        public string Value;
        [DataMember]
        public string QuickAccess;

        private string quickAccessFull;
        [DataMember]
        public string QuickAccessFull
        {
            get { return quickAccessFull; }
            set
            {
                quickAccessFull = value;
            }
        }
        [DataMember]
        public string Comment;
        [DataMember]
        public int Loop = -1;

#if _Datafrom_Database
        [DataMember]
        public long AssignedTestStepId = -1;
        [DataMember]
        public List<TestStepRunningRecorder> StepRunningRecorders = new List<TestStepRunningRecorder>();
        [DataMember]
        public string ParentAttachInfo;
#endif

        [DataMember]
        public SubTestInfo4Services SubTestInfo;
        [DataMember()]
        public byte[] PicInfo = null;
        #endregion
#if _Datafrom_Database
        public static void Normalization(TestStep4Services objData)
        {
            if (objData == null) return;
            if (!string.IsNullOrEmpty(objData.QuickAccess))
            {
                objData.QuickAccess = objData.QuickAccess.Replace("\r\n", "vbCrLf");
                objData.QuickAccess = objData.QuickAccess.Replace("\n", "vbCrLf");
                objData.QuickAccess = objData.QuickAccess.Replace("\r", "vbCrLf");
            }


        }
#endif
    }

#if _TestStepUnit
    [DataContract]
    public class MarsTestStepInfoForTestStepUnitTest
    {
        [DataMember]
        public string Command;
        [DataMember]
        public TestStep4Services TestStepDetail;
    }
#endif

    public interface IMarsTestNotificationCallback
    {
        [OperationContract]
        void OnPreCompile(string strTestSuite, string strTestCase);
        [OperationContract]
        void OnConnected();
        [OperationContract]
        void OnGetData(string strObjectName, string strData, string strError);
        [OperationContract]
        void OnPreCompileTestSteps(List<TestStep4Services> lstSteps);

        //#region Selector
        //        [OperationContract]
        //        void OnGetSelector(Stream objSelectorStreamForDeserialized) ;
        //#endregion //Selector
    }

    [ServiceContract(CallbackContract = typeof(IMarsTestNotificationCallback))]
    public interface IMarsTigerFrameWorkService
    {
        [OperationContract]
        string GetCurrentTestSuiteId4Project();
        [OperationContract]
        bool StartTestStepNavigate();
        [OperationContract]
        TestStep4Services GetNextTestStep(int iStepIdAsked = -1);
        [OperationContract]
        void EndTestStepNavigate();
        [OperationContract]
        ERROR_CODE GetLastError();
        [OperationContract]
        int GetTestLoopCount();
        [OperationContract]
        string GetCurrentTestCaseName();
        [OperationContract]
        string GetCurrentTestSuiteName();
        [OperationContract]
        string GetDataStringFromDataFile(string strObjectName, int iLoopId, ref int eCde, int iStepId = -1);
        [OperationContract]
        int StoreDataBack(string strDBIdx,string strObjectNameIdx, string strData2Store, int iLoop);
        [OperationContract]
        int SwitchDataFile(string strDataFileName);
        [OperationContract]
        int StoreDataBackComparisonMode(string strTestCaseName, string strValueWithSetting, string strValue, int iLoop, string strBaseLineMode, bool isComparison);
        [OperationContract]
        int GetCurrentStepRunType(string strKeyWordName);
        [OperationContract]
        string GetApplicationFullCmdByShortName(string strShortName, ref string strAppId, ref int eCde);
        [OperationContract]
        List<TestStep4Services> GetCurrentCompiledList(ref int eCde);
        [OperationContract]
        int CompilerCurrentTestCase(string strSuiteName, string strCaseName, ref TestStep4Services objErrorObj, ref int iErrorId, ref string strError);
#if _Datafrom_Database
        [OperationContract]
        int CompilerCurrentTestCaseById(int iTestCaseIde, ref TestStep4Services objErrorObj, ref int iErrorId, ref string strError);
        [OperationContract]
        int DBRecord_currentTestStoryBoardStart(string strCurrentCase);
        [OperationContract]
        int DBRecord_currentTestCaseStart(string strCurrentCase, int iLoop);
        [OperationContract]
        int DBRecord_currentTestStoryBoardEnd(int idSuccess, string strErrorInfo);
        [OperationContract]
        int DBRecord_OnOneLoopIsDone();
        [OperationContract]
        int CreateStepLogInfo(TestStepRunningRecorder objCurrentStepLog, int iCurrentLoopId, long stepId);
        [OperationContract]
        int DBRecord_currentTestStepEnd(int idSuccess, int iCurrentLoopId, string strError);
        [OperationContract]
        int DBRecord_currentTestCaseLoopEnd(int iResultId, int iLoopId, string strEndInfo);
        [OperationContract]
        int DBRecord_UpdateCurrentStepData(int iLoopId, string strData, TestStepRunningRecorder lastRecorder);
        [OperationContract]
        int FetchDataByStepIdAndLoopId(int loop, long assignedTestStepId, ref string strError, ref string strDataResult);
        [OperationContract]
        int DBRecord_CreateNewTestMarkID(ref string strError);
        [OperationContract]
        bool GetCurrentGenerationPegQuickInfo(ref string strPegFromServer, ref string strErrorFromServer);
        [OperationContract]
        bool AutoGen_GenStep(string strSwfName, string strType, string strTxt, ref string strError);
#endif
        [OperationContract]
        int BeginNavigateTestSuite();
        [OperationContract]
        int BeginNavigateTestSuiteWithRelyIdAndLoop(string strRelyId, int iLoop);
        [OperationContract]
        int OnGetNextTestSuite();
        [OperationContract]
        int NotifyCurrentTestSuiteRunStatus(TestSuiteRunStatusInfo objStatus, bool isContinueWhenFalse);
        [OperationContract]
        string GetBaseLineMode();
        [OperationContract]
        int CompareGuiDataByLoopId(string strLoopId);
        [OperationContract]
        string GetCurrentDefaultTestApplication();
        [OperationContract]
        string GetApplicationExtraInfo(string strApplicationShortName, ref int iErrorId);
        [OperationContract]
        string GetCurrentApplicationCmd();
        [OperationContract]
        string GetCurrentApplicationCmdWithPara(ref string strPara);
        [OperationContract]
        string GetCurrentApplicationIdentifier();
        [OperationContract]
        int GetExtraPopupMenuCount();

        //version 1.1
        [OperationContract]
        bool IsDataSetSet2Skipped(int iLoopId);

#if _Datafrom_Database
        [OperationContract]
        bool GetIgnoreErrorStatus();
#endif

        [OperationContract]
        bool GetBatchMode();
        [OperationContract]
        bool ShutdownServer();
        [OperationContract]
        bool IsSkipStepWord(string word);

        [OperationContract]
        bool IsVariable(string word);
        [OperationContract]
        string GetVariableValue(string variable);


        #region upload pictures for current test step
        [OperationContract]
        void UploadPicInfo4CurrentTestStep(byte[] arrData);
        #endregion
        //#region ObjectSelector
        //int GetSelector(string strObjectType)
        //#endregion //ObjectSelector
#if v_16AndUp
        [OperationContract]
        bool hasResumInfoFromCurrent();
        [OperationContract]
        bool jumpToNextResumeBlock();

        [OperationContract]
        bool IsLoopVarApplied();
        [OperationContract]
        string GetCurrentTestDatasetName();

        [OperationContract]
        bool IsAutoCheckErrorEnable();


        [OperationContract]
        TestStepObject GetDefaultErrorCheckingObj4CurrentPeg(string strDBIdx);

        [OperationContract]
        List<string> GetKeywordsCanAutoCheckError();
#endif

#if _TestStepUnit
        ///老虎 增加：
        /// 单步运行。2017 10 09
        /// 
        [OperationContract]
        void NotifiResultForTestStep(MarsTestStepInfoForTestStepUnitTest currentTeststepUnitInfoFromStack, bool isTestOK, string strError);
        [OperationContract]
        MarsTestStepInfoForTestStepUnitTest GetTestStepUnitFromStack(ref string strError, ref bool isOk);

#endif
        #region auto_check error part
        [OperationContract]
        List<KeyValuePair<string, int>> GetOpicsMessageAndTypeList();
        #endregion
    }




    [DataContract]
    public class SubTestInfo4Services
    {
        [DataMember]
        public string keyword;
        [DataMember]
        public List<TestStep4Services> SubActions;

    }

    [DataContract]
    public class IFSubItemInfo4Services
    {
        [DataMember]
        public string PropertyName;
        [DataMember]
        public string OperationMark;
        [DataMember]
        public string Value2Compare;
        [DataMember]
        public string AssociatedAction;
        [DataMember]
        public string ReturnValue;
        [DataMember]
        public string SubKeyword;
        [DataMember]
        public string SubObjectAndOthers;
    }

    [DataContract]
    public class IFSubTestInfo4Services : SubTestInfo4Services
    {
        [DataMember]
        public List<IFSubItemInfo4Services> SubIFItems;
    }
#endif
    public static class XmlHelper
    {
        private static void XmlSerializeInternal(Stream stream, object o, Encoding encoding)
        {
            if (o == null)
                throw new ArgumentNullException("o");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            XmlSerializer serializer = new XmlSerializer(o.GetType());

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineChars = "\r\n";
            settings.Encoding = encoding;
            settings.IndentChars = "    ";

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, o);
                writer.Close();
            }
        }

        /// <summary>
        /// 将一个对象序列化为XML字符串
        /// </summary>
        /// <param name="o">要序列化的对象</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>序列化产生的XML字符串</returns>
        public static string XmlSerialize(object o, Encoding encoding)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XmlSerializeInternal(stream, o, encoding);

                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 将一个对象按XML序列化的方式写入到一个文件
        /// </summary>
        /// <param name="o">要序列化的对象</param>
        /// <param name="path">保存文件路径</param>
        /// <param name="encoding">编码方式</param>
        public static void XmlSerializeToFile(object o, string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
                return;

            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                XmlSerializeInternal(file, o, encoding);
            }
        }

        /// <summary>
        /// 从XML字符串中反序列化对象
        /// </summary>
        /// <typeparam name="T">结果对象类型</typeparam>
        /// <param name="s">包含对象的XML字符串</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>反序列化得到的对象</returns>
        public static T XmlDeserialize<T>(string s, Encoding encoding)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException("s");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(encoding.GetBytes(s)))
            {
                using (StreamReader sr = new StreamReader(ms, encoding))
                {
                    return (T)mySerializer.Deserialize(sr);
                }
            }
        }

        /// <summary>
        /// 读入一个文件，并按XML的方式反序列化对象。
        /// </summary>
        /// <typeparam name="T">结果对象类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>反序列化得到的对象</returns>
        public static T XmlDeserializeFromFile<T>(string path, Encoding encoding)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            string xml = File.ReadAllText(path, encoding);
            return XmlDeserialize<T>(xml, encoding);
        }
    }

}
