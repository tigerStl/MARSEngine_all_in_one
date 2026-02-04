using MarsTestFrame.CommuniteServer;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace TestFrameMonitor.Server.ServiceContracts
{

    [ServiceContract(CallbackContract = typeof(IMonitorCallBack),
        SessionMode = SessionMode.Required)]
    public interface IMonitorService
    {
        [OperationContract(IsOneWay = true)]
        void OnClientTestSuiteTestCaseNamesChangeEvent(string strTestSuite, string strTestCase);
        [OperationContract(IsOneWay = true)]
        void OnClientTestCaseListChangeEvent(List<TestStep4Services> lstTestSteps);
        [OperationContract(IsOneWay = true)]
        void OnClientTestCompilerEndEvent(bool isError, string strErrorInfo, List<TestStep4Services> objErrorInfo);
        [OperationContract(IsOneWay = true)]
        void OnClientCompilerOneTestStepEvent(TestStep4Services objCurrentCompilerTestStep);
        [OperationContract(IsOneWay = true)]
        void OnClientWriteCurrentLog(string strMessage, int iErrorOrNormal);
        [OperationContract(IsOneWay = true)]
        void BeforeClientRunTestStepEvent(TestStep4Services objTestStepRunning);
        [OperationContract(IsOneWay = true)]
        void AfterClientRunTestStepEvent(string strWriteBackData, int iResult, string strMessage);
        [OperationContract(IsOneWay = true)]
        void RefreshGridStyle();
        [OperationContract(IsOneWay = true)]
        void BeginAddLogHint();
        [OperationContract(IsOneWay = true)]
        void EndAddLogHing();

        [OperationContract(IsOneWay = true)]
        void Subscribe();
        [OperationContract(IsOneWay = true)]
        void OnCurrentLoopChangeEvent(int iLoopId);
        [OperationContract(IsOneWay = true)]
        void OnLoopCountChangeEvent(int iLoopCount);

        [OperationContract(IsOneWay = true)]
        void OnClientBreakpointReachedEvent(TestStep4Services objTestInfo, SystemDebuggerMode breakMode);
        [OperationContract(IsOneWay = true)]
        void GetCurrentDebugMode();

        [OperationContract(IsOneWay = true)]
        void OnClientTestSuiteId4ProjectReadyEvent(string strCurrentTestSuiteId4Project);

        [OperationContract(IsOneWay = true)]
        void OnOneLoopIsDone();

        [OperationContract]
        bool ShutdownServer();
        [OperationContract]
        bool SetBatchMode();
        [OperationContract]
        void SkipCurrentStep();

#if v_16AndUp
        [OperationContract]
        void SetTestStoryboardName(string strStoryBoardName);
        [OperationContract]
        void SetTestStoryboardTotalSteps(int iCnt);
        [OperationContract]
        void SetTestDataSetName(string strDataSetName);


        [OperationContract]
        void ClickSpecialPos(int X, int Y);
#endif
    }

    [DataContract]
    public enum SystemDebuggerMode
    {
        [EnumMember]
        SDM_NONE = 0,
        [EnumMember]
        SDM_BREAKAT = 1,
        [EnumMember]
        SDM_RUNFROM = 0x2,
        [EnumMember]
        SDM_SKIP = 0x4,
        [EnumMember]
        SDM_REUSME = 0x8,
        [EnumMember]
        SDM_REPLAY_THESAME_TEST = 0x10
    }



    [DataContract]
    public class TestFlowDebugInfo
    {
        [DataMember]
        public int TestDebugMode;
        [DataMember]
        public int TestCurrentLoopId;
        [DataMember]
        public string CurrentTestSuiteName;
        [DataMember]
        public string CurrentTestCaseName;
        [DataMember]
        public int CurrentFromId;/**if not under debugging mode then, value is -1, step Id **/
        [DataMember]
        public int RemoveOrAddId;/** 0 add, 1, remove **/
        [DataMember]
        public string CurrentRelyId;/** key id for Batch file of the current **/

        public TestFlowDebugInfo()
        {
            CurrentFromId = -1;
            TestDebugMode = (int)SystemDebuggerMode.SDM_NONE;
            TestCurrentLoopId = -1;
            RemoveOrAddId = 1;// 
        }

        public bool EqualTo(TestFlowDebugInfo objTarget)
        {
            return /**(TestDebugMode == objTarget.TestDebugMode)
                && **/
                   (string.Compare(CurrentTestSuiteName, objTarget.CurrentTestSuiteName, true) == 0)
                && (string.Compare(CurrentTestCaseName, objTarget.CurrentTestCaseName, true) == 0)
                && (TestCurrentLoopId == objTarget.TestCurrentLoopId)
                && (CurrentFromId == objTarget.CurrentFromId)
                && (CurrentRelyId == objTarget.CurrentRelyId);
            //&& (TestCurrentLoopId == objTarget.TestCurrentLoopId);
        }
    }

    public interface IMonitorCallBack
    {
        [OperationContract(IsOneWay = true)]
        void AddOneBreakPoint(TestFlowDebugInfo objBreakPnt);
        [OperationContract(IsOneWay = true)]
        void DebugStepOver(TestFlowDebugInfo objBreakPnt);
        [OperationContract(IsOneWay = true)]
        void SetCurrentDebuggerMode(int iDebuggerMode);
        [OperationContract(IsOneWay = true)]
        void SetMonitorTokenId(int iMonitorToken);

        #region service methods call back
        [OperationContract(IsOneWay = true)]
        void SubScribCallBack();
        [OperationContract(IsOneWay = true)]
        void GetCurrentDebugModeCallBack(TestFlowDebugInfo objDebugInfo);
        #endregion 
    }
}
