using Mars.message.AutoTestingDriver.ErrorMessage;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    class MarsListViewOperation : ThirdPartControlOpBase
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

        internal bool SelectListItem(string strData, string strParaMeter, Control srcCntrl, string strPegName, string strObjName, ref string strError,
            ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("MarsListViewOperation.SelectListItem");

            try
            {
                if (srcCntrl == null)
                {
                    strError = "Passed Null to a function";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    simpleLog.MarsLoggerSimple.Info("MarsListViewOperation.SelectListItem", strError);
                    return false;
                }
                if (string.IsNullOrEmpty(strParaMeter))
                {
                    strError = $"[SelectListItem] doesn't support the current parameter [{strParaMeter}]";// string.Format("Action string should be like:Action:LEFT_Click|LEFT_RIGHTCLICK|LEFT_DOUBLECLICK");
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Check the [SelectListItem]'s parameter, see user manual";
                    simpleLog.MarsLoggerSimple.Info("MarsListViewOperation.SelectListItem", strError);
                    return false;
                }

                Control cntrlList = ((Control)srcCntrl);
                IntPtr timeoutRslt = IntPtr.Zero;
                IntPtr rsltTimeOut = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(cntrlList.Handle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                    10000,
                    out timeoutRslt
                    );
                if (rsltTimeOut.ToInt64() != 0)
                {
                    simpleLog.MarsLoggerSimple.Info("MarsListViewOperation", "send time out returns true, no thread is busy");
                }

                ((Control)srcCntrl).Update();

                System.Threading.Thread.Sleep(100);

                //parameter demo:MarsAddins;Server.*:Select;Action:LEFT_CLICK
                string strPara = strParaMeter;
                //check paramter format
                if (strPara.ToUpper().StartsWith("MARSADDINS;"))
                {
                    strPara = strPara.Substring("MARSADDINS;".Length);
                }
                string[] paras = strPara.Split(';');
                if (paras.Length != 3)
                {
                    strError = string.Format("parameter should contains column Name, text to search and action , but it is:[{0}]", strParaMeter);
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.SelectListItem", strError);
                    strError = $"[SelectListItem] doesn't support the current parameter [{strParaMeter}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Check the [SelectListItem]'s parameter, see user manual";
                    return false;
                }
                string colName = paras[0];
                string textToSearc = paras[1];
                string strAction = paras[2];
                if (string.IsNullOrEmpty(strAction)) strAction = "Action:LEFT_CLICK";
                string[] act = strAction.Split(':');
                if (act.Length != 2)
                {
                    strError = $"[SelectListItem] doesn't support the current parameter [{strParaMeter}]";// string.Format("Action string should be like:Action:LEFT_Click|LEFT_RIGHTCLICK|LEFT_DOUBLECLICK");
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Check the [SelectListItem]'s parameter, see user manual";
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.SelectListItem", strError);
                    return false;
                }

                var oItems = ReflectorForCSharp.GetMember(srcCntrl, "Items");
                if (oItems == null)
                {
                    strError = string.Format("Can't find Items from control [{0}]", srcCntrl.GetType());
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.SelectListItem", strError);
                    strError = "Object property [Items] is NUll";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                ReflectorForCSharp rflct = new ReflectorForCSharp();
                int iCnt = rflct.GetMember<int>(oItems, "Count");
                if (iCnt <= 0)
                {
                    strError = "Object member [count]'s value is 0";// string.Format("count of least returns [{0}]", iCnt);
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.SelectListItem", strError);
                    return false;
                }
                var oColumns = ReflectorForCSharp.GetMember(srcCntrl, "SubItemColumns");
                var oMainCol = ReflectorForCSharp.GetMember(srcCntrl, "MainColumn");
                if (oMainCol == null)
                {
                    strError = "Object property [Maincolumn is NULL";// string.Format("can't find Maincolomn from object, object type is:[{0}]", srcCntrl.GetType());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.SelectListItem", strError);
                    return false;
                }
                string strMainColumnCaption = rflct.GetMember<string>(oMainCol, "TextResolved");
                int iColId = -1;
                if (string.IsNullOrEmpty(strMainColumnCaption))
                {
                    ///then only one column
                    ///that is the the opics way. 2020/05/2
                    ///
                }
                object[] oAllRows = null;
                if (srcCntrl.InvokeRequired)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                    {
                        oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oItems, "All");
                    }));
                }
                else
                {
                    oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oItems, "All");
                }

                if (oAllRows == null)
                {
                    strError = "Object property [All] is NULL";//string.Format("can't get All from [{0}].[{1}]", srcCntrl.GetType(), oItems.GetType());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Make sure [{strPegName}].[{strObjName}] is available in Screen";
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.SelectListItem", strError);
                    return false;
                }
                bool isOk = false;
                string strTmpAllRows = "";

                foreach (var itm in oAllRows)
                {
                    if (itm == null) continue;
                    System.Threading.Thread.Sleep(10);

                    var oItmTxt_Key = ReflectorForCSharp.GetMember(itm, "Key");
                    var oItmTxt_Txt = ReflectorForCSharp.GetMember(itm, "Text");
                    if (oItmTxt_Key == null) continue;
                    string strErrorTmp = "", strAdvTmp = "", strStackTmp = "";
                    Rectangle targetItmRectangle = default(Rectangle);
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("going to compare:[{0}]", oItmTxt_Txt));
                    // only test txt
                    if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(textToSearc, oItmTxt_Txt == null ? "" : oItmTxt_Txt.ToString()))
                    {
                        object oUIELement = null;
                        if (srcCntrl.InvokeRequired)
                        {
                            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                            {
                                oUIELement = ReflectorForCSharp.GetMember(itm, "UIElement");
                                if (oUIELement == null)
                                {
                                    isOk = false;
                                    strErrorTmp = oUIELement == null ? "Object property [UIElement] is NULL" : "Object member [UIElement]'s value is NULL";//string.Format("Can't get UIElement from [{0}].[{1}].[{2}]", srcCntrl.GetType(), oItems.GetType(), itm.GetType());
                                    StackFrame stck = (new StackFrame());
                                    strStackTmp = MarsErrorStacks.StackTraceDump();
                                    strAdvTmp = $"Make sure [{strPegName}].[{strObjName}] is available in Screen";
                                    return;
                                }
                                var rect = ReflectorForCSharp.GetMember(oUIELement, "Rect");
                                if ((rect == null) || (!(rect is Rectangle)))
                                {
                                    isOk = false;
                                    strErrorTmp = rect == null ? "Object property [Rect] is NULL" : "Object member [Rect]'s value is NULL";// string.Format("Rect is null or not Rectangle from  [{0}].[{1}].[{2}] -- type:[{3}]", srcCntrl.GetType(), oItems.GetType(), itm.GetType(), rect == null ? "" : rect.GetType().ToString());
                                    strStackTmp = MarsErrorStacks.StackTraceDump();
                                    strAdvTmp = $"Make sure [{strPegName}].[{strObjName}] is available in Screen";
                                    return;
                                }
                                isOk = true;
                                targetItmRectangle = (Rectangle)rect;
                                return;
                            }));
                            if (!isOk)
                            {
                                strError = strErrorTmp;
                                strAdv = strAdvTmp;
                                strStack = strStackTmp;
                                return false;
                            }
                        }
                        else
                        {
                            oUIELement = ReflectorForCSharp.GetMember(itm, "UIElement");
                            if (oUIELement == null)
                            {
                                isOk = false;
                                strError = "Object property [UIElement] is NULL";// string.Format("Can't get UIElement from [{0}].[{1}].[{2}]", srcCntrl.GetType(), oItems.GetType(), itm.GetType());
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = $"Make sure [{strPegName}].[{strObjName}] is available in Screen";
                                return false;
                            }
                            var rect = ReflectorForCSharp.GetMember(oUIELement, "Rect");
                            if ((rect == null) || (!(rect is Rectangle)))
                            {
                                isOk = false;
                                strError = rect == null ? "Object property [Rect] is NULL" : "Object member [Rect]'s value is NULL";// string.Format("Rect is null or not Rectangle from  [{0}].[{1}].[{2}] -- type:[{3}]", srcCntrl.GetType(), oItems.GetType(), itm.GetType(), rect == null ? "" : rect.GetType().ToString());
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = $"Make sure [{strPegName}].[{strObjName}] is available in Screen";
                                return false;
                            }
                            isOk = true;
                            targetItmRectangle = (Rectangle)rect;
                            return false;

                        }
                        System.Threading.Thread.Sleep(10);
                        Point ptScrn = srcCntrl.PointToScreen(new Point(targetItmRectangle.X + srcCntrl.Width / 2, targetItmRectangle.Y + targetItmRectangle.Height / 2));

                        if (string.Compare("LEFT_DOUBLECLICK", act[1], true) == 0)
                        {
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptScrn.X, ptScrn.Y);
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptScrn.X, ptScrn.Y);

                            System.Threading.Thread.Sleep(50);

                            rsltTimeOut = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(cntrlList.Handle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                                10000,
                                out timeoutRslt
                                );
                            return true;
                        }
                        else if (string.Compare("RIGHT_CLICK", act[1], true) == 0)
                        {
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(ptScrn.X, ptScrn.Y);
                            return true;
                        }
                        else
                        {
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptScrn.X, ptScrn.Y);
                            return true;
                        }

                    }
                    strTmpAllRows += string.Format(";[{0}]", oItmTxt_Txt);

                }
                strError = string.Format("Can't find [{0}] from all rows Info :[{1}]", textToSearc, strTmpAllRows);
                simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.SelectListItem", strError);

                return false;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.SelectListItem", strError = e.Message, e);
                strError = $"Error while Selecting List Item for [{strPegName}].[{strObjName}]";
                strStack = e.StackTrace;
                strAdv = "Unidenfied error. If this continues, contact Marquis";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("MarsListViewOperation.SelectListItem");
            }
        }

        internal string CaptureValues(Control srcCntrl, string strParaMeter, string strPegName, string strObjName, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("MarsListViewOperation.CaptureValues");
            try
            {
                if (srcCntrl == null)
                {
                    strError = "Passed NULL to a function";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    simpleLog.MarsLoggerSimple.Info("MarsListViewOperation.CaptureValues", strError);
                    return null;
                }
                /// only all rows are supporte              

                var oItems = ReflectorForCSharp.GetMember(srcCntrl, "Items");
                if (oItems == null)
                {
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.CaptureValues", string.Format("Can't find Items from control [{0}]", srcCntrl.GetType()));
                    strError = $"Object property [Items] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }
                ReflectorForCSharp rflct = new ReflectorForCSharp();
                int iCnt = rflct.GetMember<int>(oItems, "Count");
                if (iCnt <= 0)
                {
                    strError = "Object property [Count]'s value is 0";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.CaptureValues", strError);
                    return null;
                }
                var oColumns = ReflectorForCSharp.GetMember(srcCntrl, "SubItemColumns");
                var oMainCol = ReflectorForCSharp.GetMember(srcCntrl, "MainColumn");
                if (oMainCol == null)
                {
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.CaptureValues", string.Format("can't find Maincolomn from object, object type is:[{0}]", srcCntrl.GetType()));
                    strError = "object property [MainColoumn] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "ContactMarquis";

                    return null;
                }
                string strMainColumnCaption = rflct.GetMember<string>(oMainCol, "TextResolved");
                int iColId = -1;
                if (string.IsNullOrEmpty(strMainColumnCaption))
                {
                    ///then only one column
                    ///that is the the opics way. 2020/05/2
                    ///
                }
                object[] oAllRows = null;
                if (srcCntrl.InvokeRequired)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                    {
                        oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oItems, "All");
                    }));
                }
                else
                {
                    oAllRows = ReflectorForCSharp.GetMemberByType<object[]>(oItems, "All");
                }

                if (oAllRows == null)
                {
                    simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.CaptureValues", strError = string.Format("can't get All from [{0}].[{1}]", srcCntrl.GetType(), oItems.GetType()));
                    strError = "Object property [All] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }
                //bool isOk = false;
                string strTmpAllRows = "";

                foreach (var itm in oAllRows)
                {
                    if (itm == null) continue;
                    System.Threading.Thread.Sleep(10);

                    var oItmTxt_Key = ReflectorForCSharp.GetMember(itm, "Key");
                    var oItmTxt_Txt = ReflectorForCSharp.GetMember(itm, "Text");
                    if (oItmTxt_Key == null) continue;
                    //string strErrorTmp = "";
                    //Rectangle targetItmRectangle = default(Rectangle);
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("going to compare:[{0}]", oItmTxt_Txt));
                    if (string.IsNullOrEmpty(strTmpAllRows))
                        strTmpAllRows = oItmTxt_Txt.ToString();
                    else
                        strTmpAllRows = string.Format("{1}\r\n{0}", oItmTxt_Txt, strTmpAllRows);

                }
                isOk = true;

                return strTmpAllRows;
            }
            catch (Exception e)
            {
                isOk = false;
                simpleLog.MarsLoggerSimple.Error("MarsListViewOperation.CaptureValues", strError = e.Message, e);
                strError = $"Error while searching for a control [{strPegName}][{strObjName}]";
                strStack = $"{e.Message}r\n{e.StackTrace}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("MarsListViewOperation.CaptureValues");
            }

        }
    }


}
