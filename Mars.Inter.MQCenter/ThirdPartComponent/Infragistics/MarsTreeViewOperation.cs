using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.Inter.MQCenter.interProcess;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    class MarsTreeViewOperation : ThirdPartControlOpBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="oSourceControl"></param>
        /// <param name="strParameter"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <returns></returns>
        public override string CaptureValueFromControl(
            object oSourceControl, 
            string strParameter, 
            string strPegName, 
            string strObjName, 
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            if (oSourceControl == null)
            {
                strError = "Passing null object to a function (CaptureValue for TreeView)";//"control parameter is null";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            string strTyps = ReflectorForCSharp.GetObjectBaseType(oSourceControl.GetType());
            if (strTyps.Contains("Infragistics.Win.UltraWinTree.UltraTree"))
            {
                bool isNotExist = false ;
                object Nodes = ReflectorForCSharp.GetMember(oSourceControl, "Nodes", ref isNotExist); // TreeNodesCollection
                if (isNotExist)
                {
                    simpleLog.MarsLoggerSimple.Error("MarsTreeViewOperation.CaptureValueFromControl", strError = string.Format("Can't find Nodes from source control [{0}]", oSourceControl.GetType().ToString()));
                    strError = "Object Property Nodes is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    isOk = false;
                    return null;
                }
                ReflectorForCSharp reflector = new ReflectorForCSharp();
                object[] nodeList = reflector.GetMember<object[]>(Nodes, "All");
                if (nodeList == null)
                {
                    simpleLog.MarsLoggerSimple.Error("MarsTreeViewOperation.CaptureValueFromControl", strError = string.Format("Can't get 'All' from Nodes' type [{0}]", Nodes.GetType().ToString()));
                    strError = "Object Property All is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    isOk = false;
                    return null;
                }
                bool isFind = false;
                strError = "";
                string strAllNodeTxt = GetAllNodesText(nodeList, ref isOk,ref strError, ref strAdv, ref strStack);
                if (!isOk)
                {
                    simpleLog.MarsLoggerSimple.Error("MarsTreeViewOperation.CaptureValueFromControl", strError);
                    return null;
                }
                isOk = true;
                return strAllNodeTxt;
            }
            strError = "Currently, MARS captureValue/CaputreAndCompare can't the object type.";
            strStack = Environment.StackTrace;
            strAdv = "Contact Marquis";
            isOk = false;
            return null;
        }

        private string GetAllNodesText(object[] lstNodes, ref bool isOk, ref string strError, ref string strAdv, ref string strStack,int iLvl = 0)
        {
            if (lstNodes == null)
            {
                strError = "nodeList is null ";
                strStack = Environment.StackTrace;
                strAdv = "Contact Marquis";
                isOk = true;
                simpleLog.MarsLoggerSimple.Error("MarsTreeViewOperation.GetAllNodesText", strError);
                return null;
            }
            bool isNotExists = false;
            string strResult = null;
            for (int i = 0; i < lstNodes.Length; i++)
            {
                object objCurrentNode = lstNodes[i];
                if (objCurrentNode == null) continue;

                object oText = ReflectorForCSharp.GetMember(objCurrentNode, "Text", ref isNotExists);
                if (isNotExists)
                {
                    strError = $"No 'Text' property exits from type '{objCurrentNode.GetType()}' ";
                    strStack = Environment.StackTrace;
                    strAdv = "Contact Marquis";
                    isOk = true;
                    simpleLog.MarsLoggerSimple.Error("MarsTreeViewOperation.GetAllNodesText", strError);
                    return null;
                }
                string prefixTab = "";
                for (int j = 0; j < iLvl; j++)
                {
                    prefixTab += "\t";
                }
                string currentTxt = oText == null ? "" : oText.ToString();
                currentTxt = $"{prefixTab}{currentTxt}";

                object Nodes = ReflectorForCSharp.GetMember(objCurrentNode, "Nodes", ref isNotExists); // TreeNodesCollection
                if (isNotExists) continue;
                if (Nodes == null) continue;
                object[] nodeList = (new ReflectorForCSharp()).GetMember<object[]>(Nodes, "All");
                string subNodeTxts = "";
                isOk = true;
                if ((nodeList != null) && (nodeList.Length > 0))
                {
                    subNodeTxts = GetAllNodesText(nodeList, ref isOk, ref strError, ref strAdv, ref strStack, iLvl + 1);
                    if (!isOk)
                        return currentTxt;

                    currentTxt = $"{currentTxt}\r{subNodeTxts}";
                }
                strResult = strResult == null ? currentTxt : $"{strResult}\r{currentTxt}";
            }
            isOk = true;
            return strResult;
        }

        public bool SelectListItem(string strDataToSelect, string strRC,
            System.Windows.Forms.Control controlTree,
            string strPegName, string strObjName,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            simpleLog.MarsLoggerSimple.Info("MarsTreeViewOperation.SelectListItem", string.Format("Data:[{0}] parameter:[{1}]", strDataToSelect, strRC));
            //for list item, it works for infragistics treeview and other stanndard treeview
            //nodes are sperated by "\" a sample is isSelOk = SelectListItem("window(""FusionInvest 7.1.3"").SwfTreeView(""swfName:=navigationTree"")", "Portfolios","Expand", "")
            //isSelOk = SelectListItem("window(""FusionInvest 7.1.3"").SwfTreeView(""swfName:=navigationTree"")", "Portfolios\@FO_TEST","RIGHT", "")
            
            if (controlTree == null)
            {
                strError = "Passing null to a function";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }

            if (string.Compare(strRC, "UI_EXPAND", true) == 0)
            {
                try
                {
                    (new ReflectorForCSharp()).CallMethodByParaType(controlTree, "ExpandAll", new Type[] { }, null);
                    //现在不清楚是不是在FRB项目中用过这个参数，并且，该参数是不是只是expand。从10 machine的Storyboard中，应该是leftclick?
                    //因此，先使用left click,而不是直接返回
                    strRC = "LEFTCLICK";
                    //return true;
                }
                catch (Exception e)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItem", strError = string.Format("ExpandAll causes Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                    strError = $"Object method ExpandAll generates undealed exception:[{e.Message}]";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marsquis";
                    return false;
                }
            }

            int iCmmd = 1; //default right click
            if (string.Compare("LEFTDBLCLICK", strRC, true) == 0)
            {
                iCmmd = 2;
            }
            else if (string.Compare("RIGHTCLICK", strRC, true) == 0)
            {
                iCmmd = 1;
            }
            else if (string.Compare("SCROLL", strRC, true) == 0)
            {
                iCmmd = 3;
            }
            else if (string.Compare("LEFTDBLCLICK_TEXT", strRC, true) == 0)
            {
                iCmmd = 4;
            }else if (string.Compare("LEFTCLICK_TEXT", strRC, true) == 0)
            {
                iCmmd = 5;
            }else
            {

                iCmmd = 0; //Left click
            }
            Rectangle ptTxtClt = default(Rectangle);
            Rectangle ptClient = default(Rectangle);
            Rectangle rect     = default(Rectangle);
            IntPtr lpdwResult;
            windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(controlTree.Handle, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                120000, //2分钟
                out lpdwResult);
            System.Threading.Thread.Sleep(50);


            if ((string.IsNullOrEmpty(strDataToSelect)) 
                || (string.Compare(strDataToSelect, ClientDealWithGUIKeyword.cnst_selectListItem_defaultData_click_only, true)==0))
            {
                ptTxtClt = controlTree.RectangleToScreen(controlTree.Bounds);
            }
            else {
                string[] arrNodes = strDataToSelect.Split('\\');

                #region find target node  --targetNode
                bool isNotExist = false;
                object Nodes = ReflectorForCSharp.GetMember(controlTree, "Nodes", ref isNotExist); // TreeNodesCollection
                if (isNotExist)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItem", strError = string.Format("Can't find Nodes from source control [{0}]", controlTree.GetType().ToString()));
                    strError = "Object Property Nodes is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                ReflectorForCSharp reflector = new ReflectorForCSharp();
                object[] nodeList = reflector.GetMember<object[]>(Nodes, "All");
                if (nodeList == null)
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItem", strError = string.Format("Can't get 'All' from Nodes' type [{0}]", Nodes.GetType().ToString()));
                    strError = "Object Property All is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                bool isFind = false;
                strError = "";
                object targetNode = FindSpecialNode(nodeList, arrNodes, 0, ref strError, ref strAdv, ref strStack, ref isFind);
                if ((!isFind) || (targetNode == null))
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = string.Format("no such node [{0}]", strDataToSelect);
                    }
                    simpleLog.MarsLoggerSimple.Error("\t", string.Format("error when find node:[{0}]", strError));
                    return false;
                }
                else
                {

                }
                #endregion

                #region make it visible
                /// 
                string strErrorTmp = "", strStackTmp = "";
                bool isOkTmp = false;
                bool isTextRectExist = false;
                object oTextRect = null;

                ReflectorForCSharp rflct = new ReflectorForCSharp();
                if (iCmmd == 3)
                //if (isToCallBringIntoView)
                {
                    rflct.CallMethodByParaType(targetNode, "BringIntoView", new Type[] { });
                    //rflct.CallMethodByParaType(controlTree, "SetActiveNode",
                    //    new Type[] { targetNode.GetType(), typeof(bool) }, new object[] { targetNode, true }
                    //    );
                    Thread.Sleep(50);
                    return true;
                }
                bool isTextRectangleEmpty = false;
                if (controlTree.InvokeRequired)
                {
                    //对于bringIntoView好像不需要使用dispatch模式

#if _NET4
                    ///注：
                    ///如果调用TopNode，那么，targetNode的位置会变，为获得正确的位置，可以使用
                    ///1，UIElement，2，TextRectangle
                    ///TextRectangle 可能为空，如果没有text，但是，在现有情况，基本都是按照text进行选择，
                    ///因此，该值可以作为参考
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                    controlTree.Invoke(
#endif
                    new Action(() =>
                    {
                        //var isInView = ReflectorForCSharp.GetMember(targetNode, "IsInView");
                        //bool isToCallBringIntoView = false;
                        //if ((isInView == null) || (!((bool)isInView)))
                        //{
                        //    isToCallBringIntoView = true;
                        //}

                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(controlTree.Handle, 0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                        120000, //2分钟
                        out lpdwResult);

                        oTextRect = ReflectorForCSharp.GetMember(targetNode, "TextRectangle", ref isTextRectExist);
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("textrectangle:[{0}]", oTextRect == null ? "null" : oTextRect.GetType().ToString()));
                        Rectangle rct = (Rectangle)oTextRect;
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("left:[{0}-{1}-{2}-{3}]", rct.Left, rct.Top, rct.Right, rct.Bottom));
                        ///直接调用bringIntoView
                        ///
                        //ReflectorForCSharp rflct = new ReflectorForCSharp();

                        //Thread.Sleep(1000);
                        //rflct.CallMethodByParaType(targetNode, "BringIntoView", new Type[] { });
                        //isOkTmp = (new ReflectorForCSharp()).SetMemberValue(targetNode, controlTree, "TopNode", ref strErrorTmp);                    
                        oTextRect = ReflectorForCSharp.GetMember(targetNode, "TextRectangle", ref isTextRectExist);
                        //oTextRect = ReflectorForCSharp.GetMember(targetNode, "Bounds", ref isTextRectExist);
                        //rct = (Rectangle)oTextRect;
                        //simpleLog.MarsLoggerSimple.Info("\t", string.Format("source:[{0}-{1}-{2}-{3}], ", rct.Left, rct.Top, rct.Right, rct.Bottom));
                        ////点击左键
                        //rct = controlTree.RectangleToScreen(rct);
                        //if ((rct.Width < 1) || (rct.Height < 1))
                        //{
                        //    isTextRectangleEmpty = true;
                        //}
                        //simpleLog.MarsLoggerSimple.Info("\t", string.Format("screen left:[{0}-{1}-{2}-{3}], ", rct.Left, rct.Top, rct.Right, rct.Bottom));
                        //if (iCmmd == 1)
                        //{
                        //}
                        //else if (iCmmd == 2)
                        //{
                        //    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(rct.Left + rct.Width / 2, rct.Top + rct.Height / 2);
                        //    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(rct.Left + rct.Width / 2, rct.Top + rct.Height / 2);
                        //}
                        //else if (iCmmd == 0)
                        //{

                        //    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(rct.Left + rct.Width / 2, rct.Top + rct.Height / 2);
                        //}
                        isOkTmp = true;
                    }));

                }
                else
                {
                    //ReflectorForCSharp rflct = new ReflectorForCSharp();
                    rflct.CallMethodByParaType(targetNode, "BringIntoView", new Type[] { });
                    isOkTmp = (new ReflectorForCSharp()).SetMemberValue(targetNode, controlTree, "TopNode", ref strErrorTmp, ref strStackTmp);
                    oTextRect = ReflectorForCSharp.GetMember(targetNode, "TextRectangle", ref isTextRectExist);
                }

                if (!isOkTmp)
                {
                    strError = strErrorTmp;
                    simpleLog.MarsLoggerSimple.Error("SelectListItem", string.Format("Nodes:[{0}] Error:[{1}]", strDataToSelect, strErrorTmp));
                    return false;
                }
                #endregion //make it visible

                #region get position
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(controlTree.Handle, 0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                    120000, //2分钟
                    out lpdwResult);
                System.Threading.Thread.Sleep(50);
                ///参见上面的注
                object oRect = ReflectorForCSharp.GetMember(targetNode, "Bounds", ref isNotExist);
                if ((isNotExist) || (!(oRect is Rectangle)) || (isTextRectExist) 
                    || (!(oTextRect is Rectangle)))
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItem",
                        strError = string.Format("Can't from Bounds from Node or Bounds is not a rectangle,with type:[{0}]",
                        targetNode.GetType().ToString()));
                    return false;
                }
                rect               = (Rectangle)oRect;
                ptClient           = controlTree.RectangleToScreen(rect);
                Rectangle rectText = (Rectangle)oTextRect;
                ptTxtClt           = controlTree.RectangleToScreen(rectText);
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("rect:[{0}], textRect:[{1}], [{2}]-[{3}]",
                    rect, rectText, ptClient, ptTxtClt));
                #endregion
            }

            #region do click
            switch (iCmmd)
            {
                case 1: //RIGHTCLICK
                    if (controlTree.InvokeRequired)
                    {
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                        controlTree.Invoke(
#endif
                        new Action(() =>
                        {
                            //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(ptClient.X + rect.Width / 2, ptClient.Y + rect.Height / 2);
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                        })
                            );
                    }
                    else
                        //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(ptClient.X + rect.Width / 2, ptClient.Y + rect.Height / 2);
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                    break;
                case 2://LeftdoubleClick
                    if (controlTree.InvokeRequired)
                    {
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                        controlTree.Invoke(
#endif
                        new Action(() =>
                        {
                            
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptClient.X + rect.Width / 2, ptClient.Y + rect.Height / 2);
                            //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                            System.Threading.Thread.Sleep(50);
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptClient.X + rect.Width / 2, ptClient.Y + rect.Height / 2);
                            //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                        })
                        );
                    }
                    else
                    {
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptClient.X + rect.Width / 2, ptClient.Y + rect.Height / 2);
                        //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height/ 2);
                        System.Threading.Thread.Sleep(50);
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptClient.X + rect.Width / 2, ptClient.Y + rect.Height / 2);
                        //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height/ 2);
                    }
                    break;
                case 4: /// left double lcick and use text's rect
                    if (controlTree.InvokeRequired)
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                            new Action(() => {
                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                                System.Threading.Thread.Sleep(50);
                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                            }));
                    }
                    else
                    {
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                        System.Threading.Thread.Sleep(50);
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                    }
                    break;
                case 5:
                    if (controlTree.InvokeRequired)
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                            new Action(() => {
                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                                System.Threading.Thread.Sleep(50);
                            }));
                    }
                    else
                    {
                        windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + ptTxtClt.Width / 2, ptTxtClt.Y + ptTxtClt.Height / 2);
                        System.Threading.Thread.Sleep(50);
                    }
                    break;
                default:
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("clicked at [{0}]", ptTxtClt));
                    #region highlight
                    //windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(new windowsWrapper.SystemUtil.MarsWindowsAPIs.RECT() {
                    //    Left = rect.X,
                    //    Right = rect.X+rect.Width,
                    //    Top = rect.Y,
                    //    Bottom = rect.Y+rect.Height
                    //},ref strError);
                    //System.Threading.Thread.Sleep(1000);
                    //windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(new windowsWrapper.SystemUtil.MarsWindowsAPIs.RECT()
                    //{
                    //    Left = ptTxtClt.X,
                    //    Right = ptTxtClt.X + rect.Width,
                    //    Top = ptTxtClt.Y,
                    //    Bottom = ptTxtClt.Y + rect.Height
                    //}, ref strError);
                    //                    if (controlTree.InvokeRequired)
                    //                    {
                    //#if _NET4
                    //                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                    //#else
                    //                        controlTree.Invoke(
                    //#endif
                    //                        new Action(() => {
                    //                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + rect.Width / 2, ptTxtClt.Y + rect.Width / 2);
                    //                            })
                    //                            );
                    //                    }
                    //                    else
                    #endregion //highlight
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptClient.X + rect.Width / 2, ptClient.Y + rect.Height / 2);
                    //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptTxtClt.X + rect.Width / 2, ptTxtClt.Y + rect.Height / 2);
                    break; ;
            }
            #endregion

            //Thread.Sleep(10000);
            simpleLog.MarsLoggerSimple.Info("\t", "after 10");
            windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(controlTree.Handle, 0,
                IntPtr.Zero,
                IntPtr.Zero,
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                120000, //2分钟
                out lpdwResult);
            return true;
        }

        private object FindSpecialNode(object[] lstNodes, string[] arrNodesPath, int iLevel, ref string strError, ref string strAdv, ref string strStack, ref bool isFind)
        {
            simpleLog.MarsLoggerSimple.logBegin("FindSpecialNode", string.Format("nodes count:[{0}], nodePath:[{1}] level:[{2}]",
                lstNodes == null ? 0 : lstNodes.Length,
                arrNodesPath == null ? "NULL" : string.Join(",", arrNodesPath),
                iLevel
                ));
            isFind = false;
            if (lstNodes == null) return null;
            //if (lstNodes.Length < iLevel) return null;
            try
            {
                bool isNotExists = false;
                string strCurrentCheckKey = arrNodesPath[iLevel];
                for (int i = 0; i < lstNodes.Length; i++)
                {
                    object objCurrentNode = lstNodes[i];
                    if (objCurrentNode == null) continue;

                    object oText = ReflectorForCSharp.GetMember(objCurrentNode, "Text", ref isNotExists);
                    if (isNotExists)
                    {
                        simpleLog.MarsLoggerSimple.Error("FindSpecialNode", strError = string.Format("No Text exists in [{0}]", objCurrentNode.GetType().ToString())); StackFrame stck = (new StackFrame());
                        strError = "Object property Text is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return null;
                    }
                    if (oText == null) continue;
                    string Text = oText.ToString();
                    simpleLog.MarsLoggerSimple.Info("FindSpecialNode", string.Format("find [{0}] for [{1}] Level [{2}]", Text, string.Join(",", arrNodesPath), iLevel));
                    if ((string.Compare(Text, strCurrentCheckKey, true) == 0) || (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strCurrentCheckKey, Text)))
                    {
                        if (iLevel == arrNodesPath.Length - 1)
                        {
                            /** the last node to check **/
                            simpleLog.MarsLoggerSimple.Info("FindSpecialNode", "Find the leaf node");
                            isFind = true;
                            return objCurrentNode;
                        }
                        else
                        {
                            object Nodes = ReflectorForCSharp.GetMember(objCurrentNode, "Nodes", ref isNotExists); // TreeNodesCollection
                            if (isNotExists) continue;
                            if (Nodes == null) continue;
                            object[] nodeList = (new ReflectorForCSharp()).GetMember<object[]>(Nodes, "All");
                            if (nodeList == null)
                            {
                                simpleLog.MarsLoggerSimple.Error("FindSpecialNode", strError = string.Format("Can't get 'All' from Nodes' type [{0}]", Nodes.GetType().ToString()));
                                return false;
                            }
                            return FindSpecialNode(nodeList, arrNodesPath, iLevel + 1, ref strError, ref strAdv, ref strStack, ref isFind);
                        }
                    }

                }
                strError = string.Format("Can't find node [{0}] for level [{1}] ", arrNodesPath, iLevel);
                isFind = false;
                return null;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("FindSpecialNode", strError = string.Format("Exception :[{0}] ,stackTrace:[{1}]", e.Message, e.StackTrace));
                isFind = false;
                return false;
            }
        }

        //private bool ActiveListViewByRowNumber(System.Windows.Forms.Control cntrlTarget ,int iMode, int iClickCmd, ref string strError)
        //{
        //    //simpleLog.MarsLoggerSimple.logBegin("ActiveListViewByRowNumber", string.Format("Mode:[{0}] clickCmd:[{1}]", iMode, iClickCmd));

        //}

        #region record And replay
        public bool GetTreeNodeInfoForRecordAndReplayByPoint(object c, System.Drawing.Point pt, ref string strCurrentNodeText, ref string strNodeTextPth, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetTreeNodeInfoForRecordAndReplayByPoint", $"boject is|{c?.GetType().FullName}|screen pt|{pt}");
            bool isOk = false;
            try
            {
                System.Windows.Forms.Control cntrl = c as System.Windows.Forms.Control;
                // 检查控件是否为UltraTree
                Type ultraTreeType = c.GetType();
                //if (ultraTreeType.Name != "UltraTree" && !ultraTreeType.FullName.Contains("Infragistics.Win.UltraWinTree.UltraTree"))
                //{
                //    simpleLog.MarsLoggerSimple.Error("GetTreeNodeInfoForRecordAndReplayByPoint", strError = "The control is not an UltraTree.");
                //    return isOk =false;
                //}

                // 获取控件的鼠标点击位置
                Point clientPoint = cntrl.PointToClient(pt);

                // 获取节点信息
                object clickedNode = GetClickedNode(cntrl, ultraTreeType, clientPoint, ref isOk, ref strError);
                if (clickedNode != null)
                {
                    strCurrentNodeText = GetNodeText(clickedNode);
                    strNodeTextPth = GetNodePath(clickedNode);
                    simpleLog.MarsLoggerSimple.Info("GetTreeNodeInfoForRecordAndReplayByPoint",$"Node Text: {strCurrentNodeText}");
                    simpleLog.MarsLoggerSimple.Info("GetTreeNodeInfoForRecordAndReplayByPoint", $"Node Path: {strNodeTextPth}");
                    return isOk = true;
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Error("GetTreeNodeInfoForRecordAndReplayByPoint", strError = "No node was clicked.");
                    return isOk = false;
                }

                // 调用UltraTree的所有方法
                //InvokeAllMethods(cntrl, ultraTreeType);
                //return true;
            }
            catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("GetTreeNodeInfoForRecordAndReplayByPoint", strError = $"Exceptions occurs when get Node Text|{e.Message}", e);
                isOk = false;
                return isOk;
            }finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetTreeNodeInfoForRecordAndReplayByPoint", $"isOK|{isOk}|strError|{strError}|");
            }
        }

        private object GetClickedNode(System.Windows.Forms.Control ultraTree, Type ultraTreeType, Point clientPoint,ref bool isOk,ref string strError)
        {
            // 通过反射调用GetNodeFromPoint(Point point)方法
            MethodInfo getNodeFromPointMethod = ultraTreeType.GetMethod("GetNodeFromPoint",
                BindingFlags.Instance | BindingFlags.Public,
                null, 
                new[] { typeof(Point) },
                null);
            if (getNodeFromPointMethod == null)
            {
                simpleLog.MarsLoggerSimple.Error("GetClickedNode", strError = "GetNodeFromPoint method not found.");
                isOk = false;
                return null;
            }

            return getNodeFromPointMethod.Invoke(ultraTree, new object[] { clientPoint });
        }


        private string GetNodeText(object node)
        {
            // 通过反射获取Node的Text属性
            PropertyInfo textProperty = node.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
            return textProperty != null ? textProperty.GetValue(node)?.ToString() : string.Empty;
        }

        private string GetNodePath(object node)
        {
            // 递归获取节点路径
            PropertyInfo textProperty = node.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo parentNodeProperty = node.GetType().GetProperty("Parent", node.GetType());// BindingFlags.Instance | BindingFlags.Public);

            string text = textProperty != null ? textProperty.GetValue(node)?.ToString() : string.Empty;
            object parentNode = parentNodeProperty?.GetValue(node);

            return parentNode != null ? $"{GetNodePath(parentNode)}\\{text}" : text;
        }

        private void InvokeAllMethods(System.Windows.Forms.Control ultraTree, Type ultraTreeType)
        {
            // 获取UltraTree的所有公共方法
            MethodInfo[] methods = ultraTreeType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            foreach (var method in methods)
            {
                try
                {
                    // 忽略有参数的方法
                    if (method.GetParameters().Length > 0) continue;

                    // 调用无参数方法
                    object result = method.Invoke(ultraTree, null);
                    simpleLog.MarsLoggerSimple.Error("GetTreeNodeInfoForRecordAndReplayByPoint", $"Method: {method.Name}, Result: {result}");
                }
                catch (Exception ex)
                {
                    simpleLog.MarsLoggerSimple.Error("GetTreeNodeInfoForRecordAndReplayByPoint", $"Failed to invoke method {method.Name}: {ex.Message}");
                }
            }
        }
        #endregion
    }

}
