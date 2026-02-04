using Mars.message.AutoTestingDriver.ErrorMessage;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    class MarsMdiTabGroupOprations : ThirdPartControlOpBase
    {
        internal bool SelectTabByCaption(Control c, string strParameter, string strData, string strPegName, string strObjName, ref string strError,
            ref string strAdv, ref string strStack,
            bool isAdvancedSelectType, string strPrefix)
        {
            simpleLog.MarsLoggerSimple.logBegin("MarsMdiTabGroupOprations.SelectTabByCaption");
            try
            {
                var TabGroup = ReflectorForCSharp.GetMember(c, "TabGroup");
                if (TabGroup == null)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "no tabGroup find in type in nfragistics.Win.UltraWinTabbedMdi.MdiTabGroupControl");
                    strError = "Object property [tabGroup] is NULL in Tab";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contract Marquis";
                    return false;
                }
                var Tabs = ReflectorForCSharp.GetMember(TabGroup, "Tabs");
                if (Tabs == null)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "no Tabs find in type in  Infragistics.Win.UltraWinTabbedMdi.MdiTabGroup, TabGroup");
                    strError = "Object property [Tabs] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                var lstOfTabs = ReflectorForCSharp.GetMember(Tabs, "List");
                if ((lstOfTabs == null) || (!(lstOfTabs is ArrayList)))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("no List find in Tabs with class Name :[{0}]", Tabs.GetType()));
                    strError = "Object property [List] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                List<string> lstTotalHeader = new List<string>();
                ArrayList arrTabs = (ArrayList)lstOfTabs;
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("total coutn:[{0}]", arrTabs.Count));
                foreach (var tabItm in arrTabs)
                {
                    if (tabItm == null) continue;
                    //该是MdiTab
                    string typs = ReflectorForCSharp.GetObjectBaseType(tabItm.GetType()).ToUpper();

                    if (typs.IndexOf("INFRAGISTICS.WIN.ULTRAWINTABBEDMDI.MDITAB") < 0)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("the List item is not Infragistics.Win.UltraWinTabbedMdi.MdiTab, it is :[{0}], typs:[{1}]", tabItm.GetType(), typs));
                        strError = "Object member [List]'s type is not Tab";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    bool isNotExists = false;
                    var TextResolved = ReflectorForCSharp.GetMember(tabItm, "TextResolved", ref isNotExists);
                    if (isNotExists)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", strError = "no TextResolved exist in Infragistics.Win.UltraWinTabbedMdi.MdiTab,wrong version?");
                        strError = "Object property [TextResolved] is NULL in tab";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    string strTextResolved = TextResolved.ToString();
                    lstTotalHeader.Add(strTextResolved);
                    if (((string.Compare(TextResolved as string, strData, true) == 0)
                        || windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strData, strTextResolved)))
                    {
                        //找到目标tab
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                        {
                            ReflectorForCSharp op = new ReflectorForCSharp();
                            op.CallMethod(tabItm, "Activate", new object[] { });
                        }));
                        return true;
                    }
                }
                string strTmp = "";
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't find [{0}] from [{1}]", strData, strTmp = string.Join(";", lstTotalHeader.ToArray())));
                strError = $"Can't find Tab caption [{strData}]";
                strStack = $"no [{strData}] in [{strTmp}] \r\n{MarsErrorStacks.StackTraceDump()}";
                strAdv = $"Make sure tab [{strData}] is visible in Screen"; ;
                return false;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("MarsMdiTabGroupOprations.SelectTabByCaption", strError = string.Format("exception:[{0}]", e.Message), e);
                strError = $"Error while searching for a control [{strPegName}].[{strObjName}]";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                strAdv = "Unidentified error.If this continues, contact Marquis";
                return false;
            }
        }

        internal bool waitUntil(Control c, string strParameter, string waitType, string valueToCom, string op, 
            string strPegName, string strObjName, 
            ref string strError, ref string strAdv, 
            ref string strStack, bool isAdvancedSelectType, 
            string strPrefix, int maxWaitSeconds = 1800)
        {
            simpleLog.MarsLoggerSimple.logBegin("waitUntil", $"{waitType}{valueToCom}");
            try
            {
                var TabGroup = ReflectorForCSharp.GetMember(c, "TabGroup");
                if (TabGroup == null)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "no tabGroup find in type in nfragistics.Win.UltraWinTabbedMdi.MdiTabGroupControl");
                    strError = "Object property [tabGroup] is NULL in Tab";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contract Marquis";
                    return false;
                }
                if (!((string.Compare("=", op ?? "", true) == 0) || ((string.Compare("==", op ?? "", true) == 0))))
                {
                    strError = $"only TabName=[tabName] or TabName==[tabName] is supported, but operation is [{op}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }

                long n = DateTime.Now.Ticks, p = n;
                int iExceptionCnt = 0;
                while (((n-p)/TimeSpan.TicksPerSecond)<maxWaitSeconds)
                {
                    try
                    {

                        var Tabs = ReflectorForCSharp.GetMember(TabGroup, "Tabs");
                        if (Tabs == null)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", strError = "no Tabs find in type in  Infragistics.Win.UltraWinTabbedMdi.MdiTabGroup, TabGroup");
                            strError = "Object property [Tabs] is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            return false;
                        }

                        var lstOfTabs = ReflectorForCSharp.GetMember(Tabs, "List");
                        if ((lstOfTabs == null) || (!(lstOfTabs is ArrayList)))
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("no List find in Tabs with class Name :[{0}]", Tabs.GetType()));
                            strError = "Object property [List] is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            return false;
                        }
                        List<string> lstTotalHeader = new List<string>();
                        ArrayList arrTabs = (ArrayList)lstOfTabs;
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("total coutn:[{0}]", arrTabs.Count));
                        foreach (var tabItm in arrTabs)
                        {
                            if (tabItm == null) continue;
                            //该是MdiTab
                            string typs = ReflectorForCSharp.GetObjectBaseType(tabItm.GetType()).ToUpper();

                            if (typs.IndexOf("INFRAGISTICS.WIN.ULTRAWINTABBEDMDI.MDITAB") < 0)
                            {
                                n = DateTime.Now.Ticks;
                                System.Threading.Thread.Sleep(1000);
                                continue;
                            }

                            bool isNotExists = false;
                            var TextResolved = ReflectorForCSharp.GetMember(tabItm, "TextResolved", ref isNotExists);
                            if (isNotExists)
                            {
                                n = DateTime.Now.Ticks;
                                System.Threading.Thread.Sleep(1000);
                                return false;
                            }
                            string strTextResolved = TextResolved.ToString();
                            lstTotalHeader.Add(strTextResolved);
                            if (((string.Compare(TextResolved as string, valueToCom, true) == 0)
                                || windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(valueToCom, strTextResolved)))
                            {
                                //找到目标tab
                                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                                //{
                                //    ReflectorForCSharp op = new ReflectorForCSharp();
                                //    op.CallMethod(tabItm, "Activate", new object[] { });
                                //}));
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (iExceptionCnt == 0)
                        {
                            simpleLog.MarsLoggerSimple.Error("waitUntil", ex.Message, ex.StackTrace);
                            iExceptionCnt=1;
                        }                        
                    }
                    n = DateTime.Now.Ticks;
                    System.Threading.Thread.Sleep(1000);
                    
                }
                strError = $"No such {valueToCom} exists";
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            catch(Exception e)
            {
                strError = e.Message;
                strStack = e.StackTrace;
                strAdv = "Contact Marquis";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("waitUntil");
            }
        }
    }

    class MarsTabOperations : ThirdPartControlOpBase
    {
        /// <summary>
        /// 利用反射，获得指定信息的tab的索引
        /// </summary>
        /// <param name="cntrlTab"></param>
        /// <param name="strTabNameToLocated"></param>
        /// <param name="strError"></param>
        /// <returns>小于0，表示错误</returns>
        private int GetTabIdxByCaption(System.Windows.Forms.Control cntrlTab,
            string strTabNameToLocated, string strPegName, string strObjName,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            var tabs = ReflectorForCSharp.GetMember(cntrlTab, "Tabs");
            if (tabs == null)
            {
                strError = "Object property [Tabs] is NULL in tabpage";// "No Tabs find. ";                
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return -1;
            }
            var iCount = ReflectorForCSharp.GetMember(tabs, "Count");
            if (!(iCount is int))
            {
                simpleLog.MarsLoggerSimple.Error("GetTabIdxByCaption", strError = string.Format("Count should be int, but it is {0}", iCount.GetType().ToString()));
                strError = "Object member [Count]'s type is not int";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return -2;
            }
            var tabList = ReflectorForCSharp.GetMember(tabs, "List");
            if ((tabList == null) || (!(tabList is System.Collections.ArrayList)))
            {
                strError = tabList == null ? "Object property [List] is NULL" : "Object property [List]'s type is not ArrayList";// "List from ToolBars is null or is not ArrayList";                
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return -3;
            }
            System.Collections.ArrayList arrList = (System.Collections.ArrayList)tabList;
            string strAllCaptions = "";
            for (int iIdx = 0; iIdx < arrList.Count; iIdx++)
            {
                var itm = arrList[iIdx];
                if (itm == null) continue;
                var oTxt = ReflectorForCSharp.GetMember(itm, "Text");
                string strTxt = oTxt as string;
                if (strTxt == null)
                {
                    continue;
                }
                strAllCaptions += ";" + strTxt;

                if ((Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strTabNameToLocated, strTxt))
                    || (string.Compare(strTabNameToLocated, strTxt, true) == 0))
                {
                    /// find it 
                    /// 
                    try
                    {
                        var idx = ReflectorForCSharp.GetMember(itm, "Index");
                        if (idx is int)
                            return (int)idx;
                        return iIdx;
                    }
                    catch (Exception e)
                    {
                        strError = $"Error while operating for a tab  [{strPegName}][{strObjName}]";
                        strStack = e.StackTrace;
                        strAdv = "Unidentified error. If this continues, contact Marquis";
                        simpleLog.MarsLoggerSimple.Error("\t", string.Format("Exception when get index:[{0}], But ignored, and loop index is returned", e.Message), e.StackTrace);
                        return iIdx;
                    }

                }
            }
            strError = $"Can't find Tab caption [{strTabNameToLocated}]";// string.Format("No such caption [{1}] find in [{0}]", strAllCaptions, strTabNameToLocated);
            strStack = $"no [{strTabNameToLocated}] in [{strAllCaptions}]\r\n {MarsErrorStacks.StackTraceDump()}";
            strAdv = $"Make sure tab [{strTabNameToLocated}] is visible in Screen";
            return -4;
        }

        private bool SelectTablByIdx(System.Windows.Forms.Control cntrlTab,
            string strTabNameToLocated,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            #region //using select index or select tab mode
            //get tabs
            var tabs = ReflectorForCSharp.GetMember(cntrlTab, "Tabs");
            if (tabs == null)
            {
                strError = "Object property [Tabs] is NULL";//"No Tabs find. ";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            var iCount = ReflectorForCSharp.GetMember(tabs, "Count");
            if (!(iCount is int))
            {
                simpleLog.MarsLoggerSimple.Error("SelectTablByIdx", strError = string.Format("Count should be int, but it is {0}", iCount.GetType().ToString()));
                strError = "Object member [Count]'s type is not int";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            var tabList = ReflectorForCSharp.GetMember(tabs, "List");
            if ((tabList == null) || (!(tabList is System.Collections.ArrayList)))
            {
                simpleLog.MarsLoggerSimple.Error("SelectTablByIdx", strError = "List from ToolBars is null or is not ArrayList");
                strError = tabList == null ? "Object property [List] is NULL" : "Object member [List]'s type is not ArrayList";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            System.Collections.ArrayList arrList = (System.Collections.ArrayList)tabList;
            string strAllCaptions = "";
            for (int iIdx = 0; iIdx < arrList.Count; iIdx++)
            {
                var itm = arrList[iIdx];
                if (itm == null) continue;
                var oTxt = ReflectorForCSharp.GetMember(itm, "Text");
                string strTxt = oTxt as string;
                if (strTxt == null)
                {
                    continue;
                }
                strAllCaptions += ";" + strTxt;

                if ((Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strTabNameToLocated, strTxt))
                    || (string.Compare(strTabNameToLocated, strTxt, true) == 0))
                {
                    /// find it 
                    /// 
                    bool isOk = (new ReflectorForCSharp()).SetMemberValue(iIdx, cntrlTab, "SelectedIndex", ref strError, ref strStack);
                    if (isOk) return true;
                    simpleLog.MarsLoggerSimple.Error("SelectTablByIdx", strError);
                    //strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
            }
            strError = $"Can't find Tab caption [{strTabNameToLocated}]";// string.Format("No such caption [{1}] find in [{0}]", strAllCaptions, strTabNameToLocated);
            strStack = $"no [{strTabNameToLocated}] in [{strAllCaptions}]\r\n {MarsErrorStacks.StackTraceDump()}";
            strAdv = $"Make sure tab [{strTabNameToLocated}] is visible in Screen";
            return false;
            #endregion
        }
        private bool waitUntil_tabName(System.Windows.Forms.Control cntrlTab, string op, string compValue, ref string strError, ref string strAdv, ref string strStack, int maxWaitSeconds = 1800)
        {
            if (string.IsNullOrEmpty(compValue))
            {
                strError = "no tab caption is set";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            if (!((string.Compare("=", op??"", true) == 0) || ((string.Compare("==", op ?? "", true) == 0))))
            {
                strError = $"only TabName=[tabName] or TabName==[tabName] is supported, but operation is [{op}]";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            long n = DateTime.Now.Ticks, p = n;
            while (((n - p) / TimeSpan.TicksPerSecond) < maxWaitSeconds)
            {
                var tabs = ReflectorForCSharp.GetMember(cntrlTab, "Tabs");
                if (tabs == null)
                {
                    System.Threading.Thread.Sleep(1000);
                    n = DateTime.Now.Ticks;
                    continue;
                }
                var iCount = ReflectorForCSharp.GetMember(tabs, "Count");
                if (!(iCount is int))
                {
                    System.Threading.Thread.Sleep(1000);
                    n = DateTime.Now.Ticks;
                    continue;
                }

                var tabList = ReflectorForCSharp.GetMember(tabs, "List");
                if ((tabList == null) || (!(tabList is System.Collections.ArrayList)))
                {
                    strError = tabList == null ? "Object property [List] is NULL" : "Object property [List]'s type is not ArrayList";// "List from ToolBars is null or is not ArrayList";                
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                System.Collections.ArrayList arrList = (System.Collections.ArrayList)tabList;
                string strAllCaptions = "";
                for (int iIdx = 0; iIdx < arrList.Count; iIdx++)
                {
                    var itm = arrList[iIdx];
                    if (itm == null) continue;
                    var oTxt = ReflectorForCSharp.GetMember(itm, "Text");
                    string strTxt = oTxt as string;
                    if (strTxt == null)
                    {
                        continue;
                    }
                    strAllCaptions += ";" + strTxt;

                    if ((Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(compValue, strTxt))
                        || (string.Compare(compValue, strTxt, true) == 0))
                    {
                        /// find it 
                        ///                         
                        return true; 
                    }
                }
                System.Threading.Thread.Sleep(1000);
                n = DateTime.Now.Ticks;
            }
            strError = $"Can't match tab name :[{compValue}]";
            strAdv = "Contact Marquis";
            strStack = MarsErrorStacks.StackTraceDump();
            return false;
        }
        private bool waitUntil_tabCount(System.Windows.Forms.Control cntrlTab, string op, string compValue, ref string strError, ref string strAdv, ref string strStack, int maxWaitSeconds = 1800)
        {
            double d;
            if (!Double.TryParse(compValue,out d))
            {
                simpleLog.MarsLoggerSimple.Error("waitUntil_tabCount", strError = $"compare value is not a number for Tabcount model");
                strAdv = "Make sure that the data is in right format";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            long n = DateTime.Now.Ticks, p = n;
            while (((n - p) / TimeSpan.TicksPerSecond) < maxWaitSeconds)
            {
                var tabs = ReflectorForCSharp.GetMember(cntrlTab, "Tabs");
                if (tabs == null)
                {
                    System.Threading.Thread.Sleep(1000);
                    n = DateTime.Now.Ticks;
                    continue;
                }
                var iCount = ReflectorForCSharp.GetMember(tabs, "Count");
                if (!(iCount is int))
                {
                    System.Threading.Thread.Sleep(1000);
                    n = DateTime.Now.Ticks;
                    continue;
                }
                int icnt = (int)iCount;
                switch (op)
                {
                    case ">":
                        if (icnt > d) return true;
                        break;
                    case ">=":
                        if (icnt >= d) return true;
                        break;
                    case "<=":
                        if (icnt <= d) return true;
                        break;
                    case "<":
                        if (icnt < d) return true;
                        break;
                    case "=":
                    case "==":
                        if (icnt == (int)d) return true;
                        break;
                    default:
                        break;
                }

                System.Threading.Thread.Sleep(1000);
                n = DateTime.Now.Ticks;
            }
            strError = $"Can't match tab count :[{compValue}]";
            strAdv = "Contact Marquis";
            strStack = MarsErrorStacks.StackTraceDump();
            return false;
        }

        public bool waitUntil(System.Windows.Forms.Control cntrlTab, string waitType, string valueToCom, string op,
            string strPegName, string strObjName,
            ref string strError,
            ref string strAdv,
            ref string strStack,
            bool isAdvancedActionRequired,
            string strAdvancedActionCmd,
            int maxWaitTime = 120)
        {
            if (cntrlTab == null)
            {
                strError = "Passing null object to a function";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }

            if (string.IsNullOrEmpty(waitType))
            {
                strError = $"WaitUntil setting is wrong, Only Tabcount is supported, but it is null";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure that the keyword setting is right";
                return false;
            }

            try
            {
                //获得是否已经显示了AreTabsDisplayed
                var areTabsDisplayed = ReflectorForCSharp.GetMember(cntrlTab, "AreTabsDisplayed");
                bool AreTabsDisplayed = false;
                if (!(areTabsDisplayed is bool))
                {
                    strError = "Object property [{AreTabsDisplayed}] is NULL in tab";// "not find AreTabsDisplayed from tab, is the Infragistics version 12.X and above?";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                AreTabsDisplayed = (bool)areTabsDisplayed;
                long sT = DateTime.Now.Ticks;
                long sC = sT;

                switch (waitType.ToUpper())
                {
                    case "TABCOUNT":
                        return waitUntil_tabCount(cntrlTab, op, valueToCom, ref strError, ref strAdv, ref strStack);                        
                    case "TABNAME":
                        return waitUntil_tabName(cntrlTab, op, valueToCom, ref strError, ref strAdv, ref strStack);
                    default:
                        strError = $"WaitUntil setting is wrong, Only Tabcount|TabName is supported, but it is [{waitType}]";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Make sure that the keyword setting is right";
                        return false;
                }
            }catch(Exception e)
            {
                strError = e.Message;
                strAdv = "Contact Marquis";
                strStack = e.StackTrace;
                return false;
            }
        }

        public bool SelectTabByCaption(
            System.Windows.Forms.Control cntrlTab,
            string strTabNameToLocated, string strPegName, string strObjName,
            ref string strError,
            ref string strAdv,
            ref string strStack,
            bool isAdvancedActionRequired,
            string strAdvancedActionCmd,
            int maxWaitTime=120)
        {
            if (cntrlTab == null)
            {
                strError = "Passing null object to a function";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            try
            {
                //获得是否已经显示了AreTabsDisplayed
                var areTabsDisplayed = ReflectorForCSharp.GetMember(cntrlTab, "AreTabsDisplayed");
                bool AreTabsDisplayed = false;
                if (!(areTabsDisplayed is bool))
                {
                    strError = "Object property [{AreTabsDisplayed}] is NULL in tab";// "not find AreTabsDisplayed from tab, is the Infragistics version 12.X and above?";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                AreTabsDisplayed = (bool)areTabsDisplayed;
                long sT = DateTime.Now.Ticks;
                long sC = sT;
                while ((!AreTabsDisplayed) && ((sC - sT) / TimeSpan.TicksPerSecond < maxWaitTime))
                {
                    System.Threading.Thread.Sleep(100);
                    sC = DateTime.Now.Ticks;
                    areTabsDisplayed = ReflectorForCSharp.GetMember(cntrlTab, "AreTabsDisplayed");
                    AreTabsDisplayed = (bool)areTabsDisplayed;
                }
                if (!AreTabsDisplayed)
                {
                    strError = "Tabs' property [AreTabsDisplayed]'s value is not True.";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure that the Tag is available in Screen";
                    return false;
                }

                int iTabIdxForCaption = GetTabIdxByCaption(cntrlTab, strTabNameToLocated, strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                if (iTabIdxForCaption < 0)
                {
                    return false;
                }

                string strCmdForAction = "";
                if (isAdvancedActionRequired)
                {
                    int iPos = strAdvancedActionCmd.IndexOf(":");
                    strCmdForAction = strAdvancedActionCmd.Substring(iPos + 1);
                    if (strCmdForAction.EndsWith(";"))
                        strCmdForAction = strCmdForAction.Remove(strCmdForAction.Length - 1);
                    simpleLog.MarsLoggerSimple.Info("SelectTab", string.Format("Action is:[{0}]", strCmdForAction));

                }

                string strTmp = "", strAdvTmp = "", strStackTmp = "";
                bool isOk = false;
                var tabManager = ReflectorForCSharp.GetMember(cntrlTab, "TabManager");

                if (tabManager == null)
                {
#if _NET4
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                    cntrlTab.Invoke(
#endif
                    new Action(() =>
                    {
                        isOk = SelectTablByIdx(cntrlTab, strTabNameToLocated, ref strTmp, ref strAdvTmp, ref strStackTmp);
                    }));

                    if (string.Compare("close", strCmdForAction, true) == 0)
                    {
                        ReflectorForCSharp rf = new ReflectorForCSharp();
                        object performActionResult = rf.CallMethodJustByName(cntrlTab, "PerformAction", new object[] { 30 });
                        simpleLog.MarsLoggerSimple.Info("SelectTab", string.Format("returns after call PerformAction is:[{0}]", performActionResult));
                    }

                    strError = strTmp;
                    StackFrame stck = (new StackFrame());
                    strStack = strStackTmp;
                    strAdv = strAdvTmp;
                    return isOk;
                }

                //获得TabGroupElement， which Is Infragistics.Win.UltraWinTabControl.TabHeaderAreaUIElement
                var tabGroupElement = ReflectorForCSharp.GetMember(tabManager, "TabGroupElement");

                if (tabGroupElement == null)
                {
#if _NET4
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                    cntrlTab.Invoke(
#endif
                    new Action(() =>
                    {
                        isOk = SelectTablByIdx(cntrlTab, strTabNameToLocated, ref strTmp, ref strAdvTmp, ref strStackTmp);
                    }));

                    if (string.Compare("close", strCmdForAction, true) == 0)
                    {
                        ReflectorForCSharp rf = new ReflectorForCSharp();
                        object performActionResult = rf.CallMethodJustByName(cntrlTab, "PerformAction", new object[] { 30 });
                        simpleLog.MarsLoggerSimple.Info("SelectTab", string.Format("returns after call PerformAction is:[{0}]", performActionResult));
                    }

                    strError = strTmp;
                    strAdv = strAdvTmp;
                    strStack = strStackTmp;
                    return isOk;
                }
                else
                {
                    #region using mouse to do---prefer
                    var childElements = ReflectorForCSharp.GetMember(tabGroupElement, "ChildElements");
                    simpleLog.MarsLoggerSimple.Info("\t", "0, get child elements after tabgroupelement");
                    //child Elements is Infragistics.Win.UIElementsCollection, from ArrayList
                    if (childElements == null)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", $"1, no Child Element");
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                        cntrlTab.Invoke(
#endif
                        new Action(() =>
                        {
                            isOk = SelectTablByIdx(cntrlTab, strTabNameToLocated, ref strTmp, ref strAdvTmp, ref strStackTmp);
                        }));
                        strError = strTmp;
                        strAdv = strAdvTmp;
                        strStack = strStackTmp;
                        return isOk;
                    }
                    simpleLog.MarsLoggerSimple.Info("\t", $"2, get childElements，[{childElements.GetType()}]");
                    if (!(childElements is System.Collections.ArrayList))
                    {
                        strError = string.Format("childElements from TabGroupElement with type [{0}-version:{1}] is not from System.Collections.ArrayList, different version?",
                            childElements.GetType().ToString(),
                            childElements.GetType().Assembly.ImageRuntimeVersion
                            );
                        return false;
                    }

                    System.Collections.ArrayList lstRowAndColTabs = childElements as System.Collections.ArrayList;
                    System.Collections.ArrayList lstTabHeaders = null;
                    foreach (var itm in lstRowAndColTabs)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", itm == null ? $"lstRowAndColTabs.count:{lstRowAndColTabs.Count}" : $"{lstRowAndColTabs.Count}, type:{itm.GetType().FullName}");
                        if ((itm == null) || (!itm.GetType().FullName.Equals("Infragistics.Win.UltraWinTabs.TabRowUIElement", StringComparison.OrdinalIgnoreCase)))
                        {
                            simpleLog.MarsLoggerSimple.Info("\t", itm==null? $"lstRowAndColTabs.count:{lstRowAndColTabs.Count}":$"{lstRowAndColTabs.Count}, type:{itm.GetType().FullName}"); 
                           
                            continue;
                        }
                        //只处理一行的数据
                        var rowChildElements = ReflectorForCSharp.GetMember(itm, "ChildElements");
                        if ((rowChildElements == null)||((rowChildElements as System.Collections.ArrayList)==null))
                        {
                            strError = $"Wrong Infragistics version? No ChildElements from [{itm.GetType().FullName}]";
                            strStack = Environment.StackTrace;
                            strAdv = "Contact Marquis";
                            isOk = false;
                            simpleLog.MarsLoggerSimple.Error("SelectTabByCaption", strError, strStackTmp);
                            return false;
                        }
                        var tabChild = rowChildElements as System.Collections.ArrayList;                    

                        lstTabHeaders = tabChild as System.Collections.ArrayList;

                        for (int i = 0; i < lstTabHeaders.Count; i++)
                        {
                            var itmHeaders_TabRowUIElement = lstTabHeaders[i];// each of them should be Infragistics.Win.UltraWinTabs.TabRowUIElement
                            simpleLog.MarsLoggerSimple.Info("\t", $"3, count:[{lstTabHeaders.Count}], "+(itmHeaders_TabRowUIElement==null?"HEADER IS NULL": itmHeaders_TabRowUIElement.GetType().FullName));
                            if (itmHeaders_TabRowUIElement == null)
                            {
    #if _NET4
                                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
    #else
                                cntrlTab.Invoke(
    #endif
                                new Action(() =>
                                {
                                    isOk = SelectTablByIdx(cntrlTab, strTabNameToLocated, ref strTmp, ref strAdvTmp, ref strStackTmp);
                                }));
                                strError = strTmp;
                                strAdv = strAdvTmp;
                                strStack = strStackTmp;
                                return isOk;
                            }
                            simpleLog.MarsLoggerSimple.Info("\t", "before get second ChildElements");
                            var tabsInRow = ReflectorForCSharp.GetMember(itmHeaders_TabRowUIElement, "ChildElements"); //Infragistics.Win.UIElementsCollection
                            System.Collections.ArrayList TabsInOneRow = tabsInRow as System.Collections.ArrayList;
                            if (TabsInOneRow == null)
                            {
                                //simpleLog string.Format("childElements from tab header row is not Arraylist, it is :[{0}]", tabsInRow.GetType().ToString());
                                strError = "Object property [ChildElements]'s type is not ArrayList";
                                strStack = tabsInRow.GetType().ToString() + "\r\n" + MarsErrorStacks.StackTraceDump();
                                strAdv = "Contact Marquis";
                                isOk = false;
                                return false;
                            }
                            ///获得正确的tab，因为head的顺序可能和实际不一样
                            /// 
                            object targetTab = null;
                            int iTabMode = -1;
                            foreach (var itmTmp in TabsInOneRow)
                            {
                                if (itmTmp == null) continue;
                            
                                var tmpTabInOneRow = ReflectorForCSharp.GetMember(itmTmp, "TabItem");
                                string strTabType = itmTmp.GetType().FullName;
                                if (tmpTabInOneRow == null)
                                {
                                    if ("Infragistics.Win.ImageAndTextUIElement+ImageAndTextDependentTextUIElement".Equals(strTabType, StringComparison.OrdinalIgnoreCase))
                                    {
                                        /// 按照TXT处理
                                        /// 
                                        var oTabTxt = ReflectorForCSharp.GetMember(itmTmp, "Text");
                                        string strTabTXT = oTabTxt==null ? "":oTabTxt.ToString();
                                        if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strTabNameToLocated, strTabTXT))
                                        {
                                            targetTab = itmTmp;
                                            iTabMode  = 1;// text mode
                                            break;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    continue;
                                }
                                var iIdxTmp = ReflectorForCSharp.GetMember(tmpTabInOneRow, "Index");
                                if (iIdxTmp is int)
                                {
                                    if (((int)iIdxTmp) == iTabIdxForCaption)
                                    {
                                        targetTab = itmTmp;
                                        break;
                                    }
                                }
                            }

                            if (targetTab == null)
                            {
                                //即某个主要的tab没有发现有效的tab
                                simpleLog.MarsLoggerSimple.Info("\t", $"not match for [{itmHeaders_TabRowUIElement.GetType()}]-loop:[{i}/{lstRowAndColTabs.Count}]");
                                continue;
                                //strError = string.Format("Can't find [{0}] with index from Tabpage captions", strTabNameToLocated);
                                //strStack = MarsErrorStacks.StackTraceDump();
                                //strAdv = $"Make sure [{strTabNameToLocated}] is available in screen.";
                                //return isOk = false;
                            }

                            System.Drawing.Rectangle ChildElementRect = default(System.Drawing.Rectangle);
                            if (iTabMode == -1)
                            {
                                var childElementRect = ReflectorForCSharp.GetMember(targetTab, "ChildElementRect");
                                if (childElementRect == null)
                                {
                                    strError = "Object property [ChildElementRect] is null";
                                    strStack = MarsErrorStacks.StackTraceDump();
                                    strAdv = "Contact Marquis";
                                    return isOk = false;
                                }
                                ChildElementRect = (System.Drawing.Rectangle)childElementRect;
                            }else if (iTabMode == 1) //image txt button text head
                            {
                                var txtRect = ReflectorForCSharp.GetMember(targetTab, "TextArea");
                                if (txtRect == null)
                                {
                                    strError = $"Object [TextArea] from [{targetTab.GetType()}], wrong version of Infragistics?";
                                    strStack = MarsErrorStacks.StackTraceDump();
                                    strAdv = "Contact Marquis";
                                    return isOk = false;
                                }
                                ChildElementRect = (System.Drawing.Rectangle)txtRect;
                            }
                            else
                            {
                                strError = $"unsupported type [{targetTab.GetType()}], wrong version of Infragistics?";
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "Contact Marquis";
                                return isOk = false;
                            }

                            var cOfHeader = ReflectorForCSharp.GetMember(targetTab, "Control");

                            if (cOfHeader == null)
                            {
                                strError = $"Can't find [{strTabNameToLocated}]";
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = $"Make sure [{strTabNameToLocated}] is available in screen.";
                                return isOk = false;
                            }
                            System.Windows.Forms.Control CHeader = cOfHeader as System.Windows.Forms.Control;
                            System.Drawing.Point ptScreen = CHeader.PointToScreen(new System.Drawing.Point(ChildElementRect.X + ChildElementRect.Width / 2, ChildElementRect.Y + ChildElementRect.Height / 2));
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptScreen.X, ptScreen.Y);

                            if (string.Compare("close", strCmdForAction, true) == 0)
                            {
                                System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();
                                IntPtr lpdwResult;
                                windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                                    objCurP.MainWindowHandle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                                    5000,
                                    out lpdwResult
                                 );

                                ReflectorForCSharp rf = new ReflectorForCSharp();
                                cntrlTab.Invoke(new Action(() =>
                                {
                                    object performActionResult = rf.CallMethodJustByName(cntrlTab, "PerformAction", new object[] { 30 });
                                    simpleLog.MarsLoggerSimple.Info("SelectTab", string.Format("returns after call PerformAction is:[{0}]", performActionResult));
                                }));

                                windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                                    objCurP.MainWindowHandle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                                    5000,
                                    out lpdwResult
                                 );
                            }

                            return true;
                            //for (int j=0;j<TabsInOneRow.Count;j++)
                            //{
                            //    var tabItm = TabsInOneRow[j];
                            //    if (tabItm == null) continue;
                            //    var childElementOfHeaderControls = ReflectorForCSharp.GetMember(tabItm, "ChildElements");//Infragistics.Win.UIElementsCollection agin, it should contain 3 items, one button for close, one label, one image
                            //    System.Collections.ArrayList subItemsForHeader = childElementOfHeaderControls as System.Collections.ArrayList;
                            //    if (subItemsForHeader == null) continue;
                            //    for (int k= 0;k<subItemsForHeader.Count;k++)
                            //    {
                            //        var itmSubItemsForHeader = subItemsForHeader[k]; //{Infragistics.Win.ImageAndTextUIElement.ImageAndTextDependentImageUIElement}

                            //    }
                            //}

                            //break;// only 1 row is available current
                        }
                    }
                    strError = $"Can't find Tabpage [{strTabNameToLocated}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Make sure [{strTabNameToLocated}] is available in screen.";
                    return false;
                    #endregion
                }
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("SelectTabByCaption", strError = $"Error while selectTab for a control [{strPegName}].[{strObjName}]:{e.Message}");
                strError = $"Error while selectTab for a control  [{strPegName}][{strObjName}]";
                strStack = e.StackTrace;
                strAdv = "Unidentified error. If this continues, contact Marquis";
                return false;
            }

        }
    }
}
