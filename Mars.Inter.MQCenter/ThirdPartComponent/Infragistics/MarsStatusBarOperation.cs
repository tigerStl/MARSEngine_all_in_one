using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.Inter.MQCenter.keywordOperation;
using Mars.message.Inter.MQCenter.simpleLog;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Diagnostics;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    internal class MarsStatusBarOperation : ThirdPartControlOpBase
    {
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        internal bool GetStatusBarsText(object cntrlSrc, string strPegName, string strObjName, ref string strDataReturn, ref string strError, ref string strAdv, ref string strStack)
        {
            MarsLoggerSimple.logBegin("MarsStatusBarOperation.GetStatusBarsText");
            try
            {
                if (cntrlSrc == null)
                {
                    strError = "Passing null object to a function";//"source control is null";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                object oPanels = ReflectorForCSharp.GetMember(cntrlSrc, "Panels");
                if (oPanels == null)
                {
                    simpleLog.MarsLoggerSimple.Error("GetStatusBarsText", $"can't find Panels from [{cntrlSrc.GetType()}], different infragistics version?");
                    strError = "Object property [Panels] is NULL";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                object[] oAll = ReflectorForCSharp.GetMemberByType<object[]>(oPanels, "All");
                if (oAll == null)
                {
                    simpleLog.MarsLoggerSimple.Error("GetStatusBarsText", $"can't find All from [{cntrlSrc.GetType()}].[{oPanels.GetType()}], different infragistics version?");
                    strError = "Object property [All] is NULl ";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                if (oAll.Length == 0)
                {
                    simpleLog.MarsLoggerSimple.Error("GetStatusBarsText", $"Panels' count is 0 from [{cntrlSrc.GetType()}].[{oPanels.GetType()}], different infragistics version?");
                    strError = "Object member [Panel] is NULL";
                    strAdv = "Contact Marquis";
                    return false;
                }
                string strText = ReflectorForCSharp.GetMemberByType<string>(oAll[0], "Text");
                if (string.IsNullOrEmpty(strText))
                {
                    return true;
                }
                string text = strText.ToUpper();
                if ((text.IndexOf(" NOT ") >= 0)
                    || (text.IndexOf("NO ") >= 0)
                    || (text.IndexOf("INVALIDATE") >= 0)
                    || (text.IndexOf(" MUST ") >= 0))
                {
                    bool isOk = false;
                    string strError2 = "";
                    string arg = new Snapshot().SnapshotScreen(cntrlSrc, "", strPegName, strObjName, ref isOk, ref strError2, ref strAdv, ref strStack);
                    strDataReturn = $"{arg};{strText}";
                    strError = strText;
                    strAdv = "Make sure all items of this Screen are correct";
                    strStack = MarsErrorStacks.StackTraceDump();
                    return false;
                }
                strDataReturn = strText;
                return true;
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("MarsStatusBarOperation.GetStatusBarsText", strError = "Can't get status bar data", e);
                strError = $"Error while getting text from statusbar [{strPegName}][{strObjName}]";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
                strDataReturn = null;
                return false;
            }
        }
    }
}
