using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mars.message.Inter.MQCenter.MarsObjectsOperations
{
    internal class MarsObjectOpBase
    {
        public static void WaitUntilCurrentProcessIsNotBusy(int waitSeconds = 180)
        {
            MarsLoggerSimple.logBegin("WaitUntilCurrentProcessIsNotBusy", string.Format("wait for:[{0}]", waitSeconds));
            DateTime c = DateTime.Now;
            DateTime begin = c;
            while (((c.Ticks - begin.Ticks) / TimeSpan.TicksPerSecond) < waitSeconds)
            {
                try
                {
                    Process p = Process.GetCurrentProcess();
                    if (p == null)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }
                    IntPtr lpdwResult;
                    if (IntPtr.Size == 4)
                    {
                        if (!p.Responding)
                        {
                            Thread.Sleep(50);
                        }
                        else
                        {
                            return;
                        }
                        c = DateTime.Now;
                    }
                    else
                    {
                        if (!p.Responding)
                        {
                            Thread.Sleep(1000);
                            MarsWindowsAPIs.SendMessageTimeout(p.MainWindowHandle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_BLOCK,
                                5000, //30 秒
                                out lpdwResult);
                            c = DateTime.Now;
                            continue;
                        }
                    }
                    return;
                }
                catch (Exception e)
                {
                    MarsLoggerSimple.Error("WaitUntilCurrentProcessIsNotBusy", string.Format("Exception:[{0}]", e.Message), e);
                    Thread.Sleep(5000);
                }
                finally
                {
                    c = DateTime.Now;
                    MarsLoggerSimple.logEnd("WaitUntilCurrentProcessIsNotBusy");
                }
            }
        }
    }

    internal class MarsCheckBoxOpBase
    {
        public bool convertTestDataToValue(string strData, ref string strError, ref bool isOk)
        {
            isOk = true;
            if (string.IsNullOrEmpty(strData)) return false;
            if ((string.Compare("on", strData, true) == 0) || (string.Compare("true", strData, true) == 0)
                ||(string.Compare("t", strData, true)==0))
            {
                return true;
            }
            if ((string.Compare("off", strData, true) == 0) || (string.Compare("false", strData, true) == 0)
                || (string.Compare("f", strData, true) == 0))
            {
                return false;
            }
            isOk = false;
            strError = $"checkbox's value should be (true|t|on|off|false|f), but it is {strData}";
            return false;
        }
    }
}
