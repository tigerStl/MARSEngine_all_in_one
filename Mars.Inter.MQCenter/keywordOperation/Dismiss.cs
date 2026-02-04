using Mars.message.AutoTestingDriver.ErrorMessage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Mars.message.Inter.MQCenter.keywordOperation
{
    internal class Dismiss
    {
        internal bool Dismiss32770Dialog(Dictionary<string, string> dicObject, string strParameter, ref string strError, 
            ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("Dismiss32770Dialog", string.Format("parameter:[{0}]", strParameter));
            try
            {
                int currentProcessId = Process.GetCurrentProcess().Id;

                long iWaitTime;
                if (!long.TryParse(strParameter, out iWaitTime))
                    iWaitTime = 100;
                long ls = DateTime.Now.Ticks;

                for (int i = 0; i < 100; i++)
                {
                    long ln = DateTime.Now.Ticks;
                    if (((ln - ls) / TimeSpan.TicksPerSecond >= iWaitTime))
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", "dismiss, time out quite");
                        break;
                    }
                    //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.SearchForWindow("#32770", null);
                    List<IntPtr> lstHandle = windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.GetWindows("#32770", currentProcessId);
                    if ((lstHandle.Count <= 0))
                    {
                        if (i >= 99) return true;// no dialog 
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }
                    IntPtr pHwnd = lstHandle[0];

                    if (pHwnd == IntPtr.Zero)
                    {
                        System.Threading.Thread.Sleep(1000);
                        //no such dialog
                        continue;
                    }
                    List<IntPtr> lstAllControls = windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.GetChildWindows(pHwnd);
                    if (lstAllControls == null)
                    {
                        if (i >= 99)
                        {
                            strError = "Find standard dialog [#32770], but can't find childrens";
                            StackFrame stck = (new StackFrame());
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "";
                            return false;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(1000);
                            continue;
                        }
                    }

                    string strKeyText = dicObject.Keys.Where(p => string.Compare("text", p, true) == 0).FirstOrDefault();
                    if (strKeyText == null)
                    {
#if _NET4
                        strError = string.Format("Only text of the buttons are supported,but the keys are :[{0}]", string.Join(",", dicObject.Keys));
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "";
#else
                        strError = string.Format("Only text of the buttons are supported,but the keys are :[{0}]", string.Join(",", dicObject.Keys.ToArray()));
#endif
                        return false;
                    }

                    foreach (var iHwnd in lstAllControls)
                    {
                        int iLen = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowTextLength(iHwnd);
                        StringBuilder sb = new StringBuilder(256);
                        iLen = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowText(iHwnd, sb, 255);

                        // sb contains &
                        string strTxt = sb.ToString().Replace("&", "");
                        simpleLog.MarsLoggerSimple.Info("Dismiss32770Dialog", $"find text|{strTxt}");
                        if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(dicObject[strKeyText], strTxt))
                        {
                            //find button, then click
                            windowsWrapper.SystemUtil.MarsWindowsAPIs.RECT rect;
                            if (!windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowRect(iHwnd, out rect))
                            {
                                uint iError = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetLastError();
                                strError = string.Format("Error code return :[{0}] when GetWindowRect", iError);
                                StackFrame stck = (new StackFrame());
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "";
                                return false;
                            }
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
                            return true;
                        }

                        if (i >= 9)
                        {
                            strError = string.Format("no such button find-[{0}]", dicObject[strKeyText]);
                            StackFrame stck = (new StackFrame());
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "";
                            return false;
                        }
                    }

                    ///使用api获得window的class text消息
                }
                return true;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("Dismiss32770Dialog", strError = string.Format("Exception:[{0}]\r\nstackTrace:[{1}]", e.Message, e.StackTrace));
                StackFrame stck = (new StackFrame());
                strStack = e.StackTrace;
                strAdv = "";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("Dismiss32770Dialog");
            }
        }
    }
}
