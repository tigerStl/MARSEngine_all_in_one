namespace QtpStarter.MarsMsg
{
    public delegate void NotifyAddNewBreakPoint(int iStepId, object objStepBreakInfo);
    interface IMarsDispatchTestFrameMsg
    {
        void RegisterGUI(System.Windows.Forms.Form objTargetForm);
        void UnregisterGUI(System.Windows.Forms.Form objTargetForm);
        void ShowTopic(string strTopic, object ojbInstance);

    }
    interface IMarsDispatchTestMsgSource
    {
        void RegisterTopic(string strTopic);
        void NotifyChangeTopicChange(string strTopic, object objInstance);
    }


}
