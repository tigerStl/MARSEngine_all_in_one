namespace TestFlowClient.ClientAddins
{
    public abstract class MarsClientAddins_Base
    {
        public abstract string GetDataAddins(string objKeyword, string objName, string objRC, string objDataSrc, ref bool isOK, ref string objError);
    }
}
