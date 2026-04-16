using System;
using System.Threading;

namespace Mars.AutoTestingDriver.ExecuteTestcase.keywordOp
{
    public delegate bool ShouldStopFunc();
    public delegate void KeywordHelp_logFunc(string strMessage);
    internal class KeyWordHelper
    {
        public void WaitUntilTimeOut(int timeout, ShouldStopFunc funcCallBack, 
            KeywordHelp_logFunc logCallBack,
            int iwait=500)
        {
            if (logCallBack != null)
            {
                logCallBack("WaitUntilTimeOut begin");
            }
            long n = DateTime.Now.Ticks, p = n;
            try
            {
                while ((n - p) <= (TimeSpan.TicksPerSecond * timeout))
                {
                    try
                    {
                        if ((funcCallBack != null) && (funcCallBack()))
                            return;
                        Thread.Sleep(iwait);
                    }
                    catch (Exception e)
                    {

                    }
                    n = DateTime.Now.Ticks;
                }
            }
            finally
            {
                if (logCallBack!=null) {
                    logCallBack("WaitUntilTimeOut [END]");
                }
            }
        }
    }
}
