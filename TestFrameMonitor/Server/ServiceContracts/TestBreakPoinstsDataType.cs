namespace TestFlowClient.BreakPoints
{
    /*
    [DataContract]   
    public enum TestStepRunMark
    {
        TR_Normal = 0x0,
        TR_Skip, 
        TR_BreakPoints,
        TR_RunFrom,
        TR_RunTo
    }
    
    [DataContract]
    public class TestBreakPoints
    {   
        [DataMember]
        public TestStepRunMark StepRunMark {get; set ; }
        [DataMember]
        public string TestSuiteName{get ;set ;}
        [DataMember]
        public string TestProjectName { get; set; }
        [DataMember]
        public string TestCaseName { get; set; }
        [DataMember]
        public int StepId { get; set; }
        [DataMember]
        public int LoopId { get; set; }

        public bool EqualTo(TestBreakPoints objTarget)
        {
            return (StepRunMark == objTarget.StepRunMark)
                && (
                (TestSuiteName, objTarget.TestSuiteName, true) == 0)
                && (string.Compare(TestProjectName, objTarget.TestProjectName, true) == 0)
                && (string.Compare(TestCaseName, objTarget.TestCaseName, true) == 0)
                && (StepId == objTarget.StepId)
                && (LoopId == objTarget.LoopId);
                
        }

    }
     

    [ServiceContract]
    interface IBreakPoint4TestSuite
    {
        [OperationContract]
        void AddOneBreakPoint(TestFlowDebugInfo objBreakPnt);
        [OperationContract]
        void DebugStepOver(TestFlowDebugInfo objBreakPnt);
        [OperationContract]
        void SetCurrentDebuggerMode(int iDebuggerMode);
    }
     * * */
}
