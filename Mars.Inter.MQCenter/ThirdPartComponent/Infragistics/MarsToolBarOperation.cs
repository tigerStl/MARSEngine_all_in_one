using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
//using System.Windows.Controls;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    public enum MarsToolbarItemTyp
    {
        tool_unknow = 0x0,
        tool_button,
        tool_menu,
        tool_text,
        tool_combobox
    }

    class MarsToolBarOperation : ThirdPartControlOpBase
    {

        internal const string CNST_MENU_PREFIX = "Menu";
        internal const string cnst_infragistics_tool_bar_type = "Infragistics.Win.UltraWinToolbars.UltraToolbarsDockArea";
        internal bool FindSubControlByNameOrKey(Control prntCntrl,
            List<object> lstCntrlBelongToToolBar,
            Dictionary<string, string> propertiesForReflection,
            List<string> lstKeyPath,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {

            simpleLog.MarsLoggerSimple.Info("\t", string.Format("FindSubControlByNameOrKey, Control is [{0}]", prntCntrl == null ? "NULl" : prntCntrl.GetType().ToString()));

            var toolBarMgr = ReflectorForCSharp.GetMember(prntCntrl, "ToolbarsManager");
            if (toolBarMgr == null)
            {
                strError = "Object property [toolbarsManager] is NULL";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            var ToolBars = ReflectorForCSharp.GetMember(toolBarMgr, "Toolbars"); // type ToolbarsCollection
            if (ToolBars == null)
            {
                strError = "Object property [Toolbars] is NULL";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            var iCount = ReflectorForCSharp.GetMember(ToolBars, "Count");
            if (!(iCount is int))
            {
                //strError = string.Format("Count should be int, but it is [{0}]", iCount.GetType().ToString());
                strError = "Object member [Count]'s type is not int";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            var toolBarList = ReflectorForCSharp.GetMember(ToolBars, "List");
            if ((toolBarList == null) || (!(toolBarList is System.Collections.ArrayList)))
            {
                strError = toolBarList == null ? "Object property [List] is NULL" : "Object member [List]'s type is not ArrayList";// from ToolBars is null or is not ArrayList";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }

            System.Collections.ArrayList arrList = (System.Collections.ArrayList)toolBarList;
            System.Collections.ArrayList arrToolsList = null;
            object itmTmp;
            ///可能有多个满足条件的对象
            List<object> lstTargets = new List<object>();
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("total ToolBars count:[{0}]", arrList == null ? -1 : arrList.Count));
            string strTotal = "";
            foreach (var itm in arrList)
            {
                if (itm == null) continue;
                var key = ReflectorForCSharp.GetMember(itm, "Key") as string;
                if (string.IsNullOrEmpty(key)) continue;
                bool isSkip = key.ToLower().Contains("desktop menu");
                if (isSkip) continue; //过滤菜单

                simpleLog.MarsLoggerSimple.Info("\t", string.Format("type :[{0}]", itm.GetType().ToString()));
                try
                {
                    arrToolsList = ReflectorForCSharp.GetMember(ReflectorForCSharp.GetMember(itm, "Tools"), "List") as System.Collections.ArrayList;
                    if (arrToolsList == null)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", "no such feature [List] from tools above type");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("Exception:[{0}]", e.Message));
                    continue;
                }
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("ToolBars.tools count:[{0}]", arrToolsList == null ? -1 : arrToolsList.Count));
                foreach (var itmSubContainer in arrToolsList) //tools container
                {
                    if (itmSubContainer == null) continue;
                    /**
                     * 2019-11-26 该keyword可以用在button上
                     * */
                    //if (string.Compare("ButtonTool", itmSubContainer.GetType().Name, true) != 0) continue;
                    try
                    {
                        //注意，如果不是menu，无须再次调用反射获得Tools
                        //var subContainerTools = ReflectorForCSharp.GetMember(ReflectorForCSharp.GetMember(itmSubContainer, "Tools"), "List") as System.Collections.ArrayList; // for menu                        

                        //foreach (var toolOfsubContainerTools in subContainerTools)
                        {
                            //itmTmp = toolOfsubContainerTools;
                            itmTmp = itmSubContainer;
                            foreach (var k in propertiesForReflection.Keys)
                            {
                                string[] arrSubKey = null;
                                if (k.Contains(";"))
                                {
                                    arrSubKey = k.Split(';');
                                }
                                else
                                {
                                    arrSubKey = new string[] { k };
                                }
                                //if (itmTmp.InvokeRequired)
                                //{
#if _NET4
                                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                                prntCntrl.Invoke(
#endif
                                new Action(() =>
                                {
                                    object oTmp = itmTmp;
                                    foreach (var subK in arrSubKey)
                                    {
                                        /// 算法
                                        /// 1，按层次寻找property，因为无法确认对象类型，所以必须遍历
                                        /// 2，如果没有找到property，说明不是想要的对象，就直接遍历下一个对象
                                        /// 3，如果找到所有的property，说明找到对象，就对比数据是否一致，如果一致，就找打目标对象，否则继续遍历其他对象
                                        /// 
                                        object o = ReflectorForCSharp.GetMember(oTmp, subK);

                                        if (o == null) // no such member
                                        {
                                            ///没有找到 继续遍历其他对象
                                            //isContinue = true;
                                            return;
                                        }
                                        oTmp = o;
                                        simpleLog.MarsLoggerSimple.Info("FindSubControlByNameOrKey", string.Format("Find object type:[{0}] value:[{1}]", oTmp.GetType().ToString(), oTmp));
                                    }
                                    ///找到同类对象，判断对象相关的属性值是否和要求一致
                                    /// 
                                    bool isTheValueWanted = false;
                                    strTotal = strTotal + ";" + (oTmp == null ? "NULL" : oTmp.ToString());
                                    if (oTmp != null)
                                    {
                                        isTheValueWanted = (ReflectorForCSharp.MarsTigerUtility.RegularExpressChecking(propertiesForReflection[k], oTmp.ToString())
                                        || (string.Compare(propertiesForReflection[k], oTmp.ToString(), true) == 0));
                                        if (isTheValueWanted)
                                        {
                                            lstTargets.Add(itmTmp);
                                        }
                                        else
                                        {
                                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("!isTheValueWanted propertiesForReflection -[{0}] oTmp:[{1}]", propertiesForReflection[k], oTmp.ToString()));
                                        }
                                    }

                                }));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", string.Format("Exeption:[{0}] stacktrace\r\n{1}", ex.Message, ex.StackTrace));
                    }

                }
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("target count:[{0}]", lstTargets.Count));
            if (lstTargets.Count > 0)
            {
                ///find objects
                /// 
                lstCntrlBelongToToolBar.Clear();
                lstCntrlBelongToToolBar.AddRange(lstTargets);
                return true;
            }
            strError = string.Format("No object finds from [{0}]", strTotal);
            return false;
        }

        /**
         * 简易模式，供opics使用
         * */
        internal bool FindAndOpSubControlByNameOrKey(Control targetCntrl, string strParameter, string strData, string strPegName, string strObjName, ref string strError,
            ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("FindAndOpSubControlByNameOrKey", string.Format("Data to fill [{0}]", strData));
            bool isOk = false;
            try
            {
                object toolsFromContrl;
                isOk = GetToolsFromToolsBar(targetCntrl, out toolsFromContrl, ref strError, ref strAdv, ref strStack);
                if (!isOk)
                {
                    return false;
                }

                System.Collections.ArrayList arrList = (System.Collections.ArrayList)toolsFromContrl;
                object targetTxtTool = null;
                for (int i = 0; i < arrList.Count; i++)
                {
                    object itm = arrList[i];
                    if (itm == null) continue;

                    var key = ReflectorForCSharp.GetMember(itm, "Key") as string;
                    if (key == null)
                    {
                        continue;
                    }
                    if (string.Compare("FastPath", key, true) != 0)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("key is :[{0}] not FastPath", key));
                        continue;
                    }
                    //找到指定的toolbar
                    object tools = ReflectorForCSharp.GetMember(itm, "Tools");
                    if (tools == null)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", strError = string.Format("Tools from [{0}] is null", itm.GetType()));
                        strError = "Object property [Tools] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return isOk = false;
                    }
                    object tools_List = ReflectorForCSharp.GetMember(tools, "List");
                    System.Collections.ArrayList arrListTools = tools_List as System.Collections.ArrayList;
                    if ((!(tools_List is System.Collections.ArrayList)) || (arrListTools == null))
                    {
                        strError = tools_List == null ? "Object property [List] is NULL" : "object member [List]'s type is not Arraylist";
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("tools_List from [{0}] is not Arraylist, is {1}", tools.GetType(), tools_List.GetType()));
                        strStack = string.Format("tools_List from [{0}] is not Arraylist, is {1} \r\n", tools.GetType(), tools_List.GetType()) + MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return isOk = false;
                    }

                    for (int j = 0; j < arrListTools.Count; j++)
                    {
                        object oneTool = arrListTools[j];
                        if (oneTool == null) continue;

                        string strToolType = oneTool.GetType().ToString();
                        if (!strToolType.EndsWith(".TextBoxTool"))
                        {
                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("type is [{0}], requires endwith(.TextBoxTool)", strToolType));
                            continue;
                        }
                        targetTxtTool = oneTool;
                        break;
                    }
                    if (targetTxtTool == null)
                    {
                        //simpleLog.MarsLoggerSimple.Info("\t", strError = "can't find TextBoxTool");
                        strError = "Object property [TextBoxTool] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return isOk = false;
                    }
                    simpleLog.MarsLoggerSimple.Info("\t", "try to call ResetText and setText");

                    Rectangle rectBoundTextBoxTool = default(Rectangle);

                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                    {
                        object tmpbounds = ReflectorForCSharp.GetPropValue(targetTxtTool, "Bounds");
                        if ((tmpbounds != null) && (tmpbounds is Rectangle))
                        {
                            rectBoundTextBoxTool = (Rectangle)tmpbounds;
                        }
                    }
                    ));
                    if (!rectBoundTextBoxTool.Equals(default(Rectangle)))
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("get bounds:[{0}]", rectBoundTextBoxTool));
                        Highlight(rectBoundTextBoxTool);
                        ClientDealWithGUIKeyword.CleanAndTypeInRect(rectBoundTextBoxTool, strData);
                        //判断是否添加
                        object oTx = ReflectorForCSharp.GetMember(targetTxtTool, "Text");
                        if (oTx != null)
                        {
                            if (string.Compare(strData, oTx.ToString(), true) != 0)
                            {
                                simpleLog.MarsLoggerSimple.Warnning("\t", string.Format("text data is not match:[{0}]-[{1}]", strData, oTx));
                            }
                            return true;
                        }
                        else
                        {
                            isOk = setValueByMethod(targetTxtTool, strData, ref strError, ref strAdv, ref strStack);
                            if (!isOk)
                            {
                                //StackFrame stck = (new StackFrame());
                                //strStack = MarsErrorStacks.StackTraceDump();
                                //strAdv = "dd";
                            }
                            return isOk;
                        }
                    }
                    else
                    {
                        isOk = setValueByMethod(targetTxtTool, strData, ref strError, ref strAdv, ref strStack);
                        return isOk;
                    }
                    //另外一条路通过childUIelement

                    //marsReflect.SetMemberValue(targetTxtTool, "Text", strData, ref strError);
                    //for test
                    //object oTx = ReflectorForCSharp.GetMember(targetTxtTool, "Text");

                    ////找到指定的textboxtool
                    ////两种模式，一种是直接调用函数，一种是通Editor获得。
                    //object oEditor = ReflectorForCSharp.GetMember(targetTxtTool, "Editor");
                    //if (oEditor == null)
                    //{
                    //    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Wrong Infragistis Version? no Editor belong to  {0}", targetTxtTool.GetType()));
                    //    return setValueByMethod(targetTxtTool, strData, ref strError);
                    //}
                    //object  oEditor_TextBox = ReflectorForCSharp.GetMember(oEditor, "TextBox");
                    //Control cEditor_TextBox = oEditor_TextBox as Control;
                    //if (cEditor_TextBox == null)
                    //{
                    //    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Wrong Infragistis Version? textbox of editor from {0} is not a control", targetTxtTool.GetType()));
                    //    return setValueByMethod(targetTxtTool, strData, ref strError);
                    //}
                    ////for test
                    //if (cEditor_TextBox != null)
                    //{
                    //    Highlight(cEditor_TextBox );
                    //}
                    //System.Drawing.Rectangle rect = cEditor_TextBox.Bounds;
                    //if (!ClientDealWithGUIKeyword.CleanAndTypeInControl(cEditor_TextBox, strData, ref strError))
                    //{
                    //    simpleLog.MarsLoggerSimple.Error("\t", string.Format("CleanAndTypeInControl with error:{0}",strError));
                    //    return setValueByMethod(targetTxtTool, strData, ref strError);
                    //}

                    //return true;
                }
                strError = $"Can't find [{strParameter}] in Toolbar";//string.Format("no such tool info find:[{0}]", strParameter);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Mark sure [{strParameter}] in toolbar is visible";
                return false;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("FindAndOpSubControlByNameOrKey", string.Format("Exception [{0}]", e.Message, e));
                strError = $"Error while operating for a control[{strPegName}].[{strObjName}]";
                strStack = e.StackTrace;
                strAdv = "Unidentified error. If this continues, contact Marquis";
                return isOk = false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("FindAndOpSubControlByNameOrKey returns [{0}]", isOk));
                simpleLog.MarsLoggerSimple.logEnd("FindAndOpSubControlByNameOrKey");
            }
        }

        private bool setValueByMethod(object targetTxtTool, string strData, ref string strError, ref string strAdv, ref string strStack)
        {
            //call resettext
            ReflectorForCSharp marsReflect = new ReflectorForCSharp();
            marsReflect.CallMethod(targetTxtTool, "ResetText", null);
            simpleLog.MarsLoggerSimple.Info("\t", "try to call setText");
            bool isOK = marsReflect.SetProperty(targetTxtTool, "Text", strData, ref strError);
            if (!isOK)
            {
                strStack = strError + "\r\n" + MarsErrorStacks.StackTraceDump();
                strError = "Object propert [Text] can't be changed.";
                strAdv = "Contact Marquis";
                return false;
            }
            return true;
        }
        private bool GetToolsFromToolsBar(Control targetCntrl, out object toolBarList, ref string strError,
            ref string strAdv, ref string strStack)
        {
            var toolBarMgr = ReflectorForCSharp.GetMember(targetCntrl, "ToolbarsManager");
            toolBarList = null;
            if (toolBarMgr == null)
            {
                strError = "Object property toolbarsManager is NULL";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contract Marquis";
                return false;
            }
            var ToolBars = ReflectorForCSharp.GetMember(toolBarMgr, "Toolbars"); // type ToolbarsCollection
            if (ToolBars == null)
            {
                strError = "Object property [Toolbars] is NULL";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            var iCount = ReflectorForCSharp.GetMember(ToolBars, "Count");
            if (!(iCount is int))
            {
                strError = "Object member [Count]'s type is Not int";// string.Format("Count should be int, but it is [{0}]", iCount.GetType().ToString());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            toolBarList = ReflectorForCSharp.GetMember(ToolBars, "List");
            if ((toolBarList == null) || (!(toolBarList is System.Collections.ArrayList)))
            {
                strError = toolBarList == null ? "Object property [List] is NULL" : "Object member [List]'s type is not ArrayList";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            return true;
        }


        public bool ShiBuShiRibbonMoShi(object toolBarsManager, ref string strError)
        {
            return false;
        }

        private object LocatedToChildElement(Control c, string strData, string strObjName, ref string strError, ref string strAdv, ref string strStack,ref bool isOk)
        {
            if (c == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = "Passint NULL to function");
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis ";
                isOk = false;
                return null;
            }
            string[] arrRibbonInfo = strData.Split(':');
            if (arrRibbonInfo.Length < 0)
            {
                strError = "Incorrect format for Ribbon Tab location";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "See user manual for correct SelectTab use";
                isOk = false;
                return null;
            }

            string strTyps = ReflectorForCSharp.GetObjectBaseType(c.GetType());
            if (strTyps.IndexOf(cnst_infragistics_tool_bar_type) < 0)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("SelectTabFromRibbon,can't find [{0}] from [{1}], targetControl is not support", cnst_infragistics_tool_bar_type,
                    strTyps));
                strError = $"SelectTab does not support object type for [{strObjName}]| {strTyps}";
                strStack = $"can't find [{cnst_infragistics_tool_bar_type}] from [{strTyps}]\r\n" + MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            var toolBarMgr = ReflectorForCSharp.GetMember(c, "ToolbarsManager");
            if (toolBarMgr == null)
            {
                strError = "Object property toolbarsManager is NULL ";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            var Ribbon = ReflectorForCSharp.GetMember(toolBarMgr, "Ribbon"); // type ToolbarsCollection
            if (Ribbon == null)
            {
                strError = "Object property [Ribbon] is NULL";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("ribbon type from ToolbarsManager is [{0}]", Ribbon.GetType()));
            var RibbonUIElement = ReflectorForCSharp.GetMember(Ribbon, "UIElement");
            if (RibbonUIElement == null)
            {
                strError = "Object property [UIElement] is NULL";// from Ribbon";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                //return false;
            }

            //获得rabmanage
            var TabManager = ReflectorForCSharp.GetMember(Ribbon, "TabManager");
            if (TabManager == null)
            {
                strError = "Object property TabManager is NULL";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("TabManager type from Ribbon is [{0}]", TabManager.GetType()));

            ReflectorForCSharp reflctor = new ReflectorForCSharp();
            var TabAreaUIElement = reflctor.CallMethod(TabManager, "GetTabAreaUIElement");
            if (TabAreaUIElement == null)
            {
                strError = "Object property [TabAreaUIElement] is NULL";// from TabManager";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("TabAreaUIElement type from Ribbon is [{0}]", TabAreaUIElement.GetType()));
            var childElements = ReflectorForCSharp.GetMember(TabAreaUIElement, "ChildElements");
            isOk = true;
            return childElements;
        }

        internal bool SelectTabFromRibbon(System.Windows.Forms.Control c, string strParameter, string strData, string strPegName, string strObjName, ref string strError, ref string strAdv, ref string strStack)
        {
            #region oldCode
            
            if (c == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = "Passint NULL to function");
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis ";
                return false;
            }
            string[] arrRibbonInfo = strData.Split(':');
            if (arrRibbonInfo.Length < 0)
            {
                strError = "Incorrect format for Ribbon Tab location";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "See user manual for correct SelectTab use";
                return false;
            }

            string strTyps = ReflectorForCSharp.GetObjectBaseType(c.GetType());
            if (strTyps.IndexOf(cnst_infragistics_tool_bar_type) < 0)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("SelectTabFromRibbon,can't find [{0}] from [{1}], targetControl is not support", cnst_infragistics_tool_bar_type,
                    strTyps));
                strError = $"SelectTab does not support object type for [{strObjName}]| {strTyps}";
                strStack = $"can't find [{cnst_infragistics_tool_bar_type}] from [{strTyps}]\r\n" + MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            var toolBarMgr = ReflectorForCSharp.GetMember(c, "ToolbarsManager");
            if (toolBarMgr == null)
            {
                strError = "Object property toolbarsManager is NULL ";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            var Ribbon = ReflectorForCSharp.GetMember(toolBarMgr, "Ribbon"); // type ToolbarsCollection
            if (Ribbon == null)
            {
                strError = "Object property [Ribbon] is NULL";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("ribbon type from ToolbarsManager is [{0}]", Ribbon.GetType()));
            var RibbonUIElement = ReflectorForCSharp.GetMember(Ribbon, "UIElement");
            if (RibbonUIElement == null)
            {
                strError = "Object property [UIElement] is NULL";// from Ribbon";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                //return false;
            }

            //获得rabmanage
            var TabManager = ReflectorForCSharp.GetMember(Ribbon, "TabManager");
            if (TabManager == null)
            {
                strError = "Object property TabManager is NULL";// from control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("TabManager type from Ribbon is [{0}]", TabManager.GetType()));

            ReflectorForCSharp reflctor = new ReflectorForCSharp();
            var TabAreaUIElement = reflctor.CallMethod(TabManager, "GetTabAreaUIElement");
            if (TabAreaUIElement == null)
            {
                strError = "Object property [TabAreaUIElement] is NULL";// from TabManager";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("TabAreaUIElement type from Ribbon is [{0}]", TabAreaUIElement.GetType()));
            var childElements = ReflectorForCSharp.GetMember(TabAreaUIElement, "ChildElements");
            
            #endregion
            //bool isOk = false ;
            //string strTyps = "";
            //var childElements = LocatedToChildElement(c, strData, strObjName, ref strError, ref strAdv, ref strStack,ref isOk);
            //if (!isOk) return false;

            if (childElements == null)
            {
                strError = "Object property [childElements] is NULL ";//from TabAreaUIElement
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("childElements type from TabAreaUIElement is [{0}]", childElements.GetType()));
            if (!(childElements is ArrayList))
            {
                strError = "Object member [childElements]'s type is not ArrayList";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            ArrayList lstChildElements = childElements as ArrayList;
            bool isFind = false;
            object targetTabItem = null;
            string strTotal = "";
            IntPtr lpdwResult;
            for (int i = 0; i < lstChildElements.Count; i++)
            {
                if (lstChildElements[i] == null) continue;

                Type typ = lstChildElements[i].GetType();
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("lstChildElements[i] type is [{0}]", lstChildElements[i].GetType()));
                strTyps = ReflectorForCSharp.GetObjectBaseType(typ);
                if (strTyps.IndexOf("RibbonTabRowUIElement") < 0) continue;

                simpleLog.MarsLoggerSimple.Info("\t", "find RibbonTabRowUIElement");
                var RowUIChildElement = ReflectorForCSharp.GetMember(lstChildElements[i], "ChildElements");
                if (RowUIChildElement == null)
                {
                    strError = "Object property [ChildElements] is NULL";// "Cant' get property ChildElements from RibbonTabRowUIElement";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    continue;
                }
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("type of RowUIChildElement :[{0}]", RowUIChildElement.GetType()));
                if (!(RowUIChildElement is ArrayList)) continue;
                ArrayList lstRowUIChildElement = RowUIChildElement as ArrayList;

                for (int j = 0; j < lstRowUIChildElement.Count; j++)
                {
                    if (lstRowUIChildElement[j] == null) continue;

                    string strTypOfRowUIChild = ReflectorForCSharp.GetObjectBaseType(lstRowUIChildElement[j].GetType());
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("type j [{1}] of lstRowUIChildElement :[{0}],types:[{2}]", lstRowUIChildElement[j].GetType(), j, strTypOfRowUIChild));
                    if (strTypOfRowUIChild.IndexOf("RibbonTabItemUIElement") < 0) continue;
                    var Rect = ReflectorForCSharp.GetMember(lstRowUIChildElement[j], "Rect");
                    if (Rect == null)
                    {
                        strError = "Object property [Rect] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }

                    if (!(Rect is Rectangle))
                    {
                        strError = "Object property [Rect]'s type is not Rectangle";// ;
                        strStack = string.Format("Rect is not Rectangle, [{0}]/r/n", Rect.GetType()) + MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    Rectangle tmpRct = (Rectangle)Rect;
                    tmpRct = c.RectangleToScreen(tmpRct);
                    //Mars.windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(new windowsWrapper.SystemUtil.MarsWindowsAPIs.RECT() {
                    //    Left = tmpRct.Left,
                    //    Top = tmpRct.Top,
                    //    Right = tmpRct.Right,
                    //    Bottom = tmpRct.Bottom
                    //}, ref strError);
                    Thread.Sleep(10);

                    var TabItem = ReflectorForCSharp.GetMember(lstRowUIChildElement[j], "TabItem");
                    if (TabItem == null)
                    {
                        strError = "Object property [TabItem] is NULL ";// "can't get property TabItem from RibbonTabItemUIElement";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    //simpleLog.MarsLoggerSimple.Info("\t\t", string.Format("TabItem type:[{0}]", TabItem.GetType()));

                    Type[] arrTabItemTyps = TabItem.GetType().GetInterfaces();
                    if (arrTabItemTyps == null)
                    {
                        continue;
                    }
                    if (!arrTabItemTyps.Any(p => p.ToString().IndexOf("ITabItem") >= 0)) continue;
                    var Text = ReflectorForCSharp.GetMember(lstRowUIChildElement[j], "Text");
                    if (Text == null) continue;
                    string strTxt = Text.ToString();
                    //simpleLog.MarsLoggerSimple.Info("\t\t", string.Format("Text is:[{0}]", Text.ToString()));

                    if (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(arrRibbonInfo[0], strTxt))
                    {
                        simpleLog.MarsLoggerSimple.Info("\t\t", string.Format("find tab type is:[{0}]", TabItem.GetType()));
                        targetTabItem = TabItem;//注意 这个地方的类型是RibbonTab
                        isFind = true;

                        Thread.Sleep(100);
                        //click it
                        if (c.InvokeRequired)
                        {

                            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                            {

                                Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                                    //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                                    c.Handle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                                    1000,
                                    out lpdwResult
                                );
                                Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(tmpRct.Left + tmpRct.Width / 2, tmpRct.Top + tmpRct.Height / 2);
                                Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                                    //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                                    c.Handle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                                    1000,
                                    out lpdwResult
                                );
                            })
                        );
                        }
                        else
                        {
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                                    //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                                    c.Handle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                                    1000,
                                    out lpdwResult
                                );
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(tmpRct.Left + tmpRct.Width / 2, tmpRct.Top + tmpRct.Height / 2);
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                                //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                                c.Handle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                                1000,
                                out lpdwResult
                            );
                        }
                        break;
                    }
                    else
                    {
                        strTotal = string.Format("{0};{1}", strTotal, strTxt);
                    }

                }
                //string strCCTypes = ReflectorForCSharp.GetObjectBaseType(cc.GetType());
                //Type itabItm = cc.GetType().GetInterface("Infragistics.Win.UltraWinTabs.ITabItem", true);
                //if (itabItm == null)
                //{

                //}
                if (isFind) break;
            }
            if (!isFind)
            {
                strError = $"Can't find [{arrRibbonInfo[0]}]";
                strStack = string.Format("can't find [{0}] from ribbon top level [{1}]\r\n", arrRibbonInfo[0], strTotal) + MarsErrorStacks.StackTraceDump();
                strAdv = $"Make sure [{arrRibbonInfo[0]}] exists in Ribbon";
                return false;
            }
            Thread.Sleep(100);

            var Groups = ReflectorForCSharp.GetMember(targetTabItem, "Groups");
            if ((Groups == null) || (!(Groups is IList)))
            {
                simpleLog.MarsLoggerSimple.Error("\t", string.Format("Groups from RibbonTab is null or not ArrayList:[{0}]", Groups.GetType()));
                strError = Groups == null ? "Object property [Groups] is NULL" : "Object member [Groups]'s type is not ArrayList";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            IList arrGroups = Groups as IList;
            //这里没有判断是否没有group的情况
            isFind = false;
            strTotal = "";
            string strTargetGroupName = arrRibbonInfo[1];
            object oTargetGroup = null;

            Thread.Sleep(100);
            for (int i = 0; i < arrGroups.Count; i++)
            {
                var grpCaption = ReflectorForCSharp.GetMember(arrGroups[i], "Caption");
                if ((grpCaption == null))
                {
                    simpleLog.MarsLoggerSimple.Error("SelectTabFromRibbon", string.Format("no caption as member from  RibbonGroupCollection"));
                    continue;
                }
                string strGrpCaption = grpCaption.ToString();
                strTotal = string.Format("{0};{1}", strTotal, strGrpCaption);
                strGrpCaption = strGrpCaption.Replace("&", "");
                if (!Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strTargetGroupName, strGrpCaption)) continue;
                isFind = true;
                oTargetGroup = arrGroups[i];
                break;
            }
            if ((!isFind) || (oTargetGroup == null))
            {
                strError = $"Can't find Ribbon Tab [{arrRibbonInfo[0]}] in [{strTotal}]";
                strStack = string.Format("Can't find [{0}] from [{1}] under ribbon tab [{2}]\r\n", strTargetGroupName, strTotal, arrRibbonInfo[0]) + MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            Thread.Sleep(100);
            var grpTools = ReflectorForCSharp.GetMember(oTargetGroup, "Tools");
            if ((grpTools == null) || (!(grpTools is ICollection)))
            {
                //strError = string.Format("Tools from RibbonGroup is null or not ArrayList:[{0}]", grpTools==null?"NULL":grpTools.GetType().ToString());
                strError = grpTools == null ? "Object property Tools is NULL" : "Object member Tools' type is not ArrayList";
                strStack = string.Format("Tools from RibbonGroup is null or not ArrayList:[{0}]\r\n", grpTools == null ? "NULL" : grpTools.GetType().ToString()) + MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }

            isFind = false;
            strTotal = "";
            ICollection arrgrpTools = grpTools as ICollection;
            object targetButtonlOrPopMenu = null;
            string strTargetToolOrPopMenuButtonTyps = "";
            Thread.Sleep(500);
            foreach (var itm in arrgrpTools)
            //for (int i = 0; i < arrgrpTools.Count; i++)
            {
                if (itm == null) continue;
                var toolSharedProps = ReflectorForCSharp.GetMember(itm, "SharedProps");
                if (toolSharedProps == null) continue;
                var toolCaption = ReflectorForCSharp.GetMember(toolSharedProps, "Caption");
                if ((toolCaption == null) || (!(Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(arrRibbonInfo[2], toolCaption.ToString()))))
                {
                    strTotal = string.Format("{0};{1}", strTotal, toolCaption.ToString());
                    continue;
                }
                targetButtonlOrPopMenu = itm;
                strTargetToolOrPopMenuButtonTyps = ReflectorForCSharp.GetObjectBaseType(targetButtonlOrPopMenu.GetType());
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("object target tool type and caption:[{0}]-[{1}]", strTargetToolOrPopMenuButtonTyps, toolCaption));
                isFind = true;
                break;
            }
            if (!isFind)
            {
                strError = string.Format("Can't find [{0}] in [{2}]-[{1}]", arrRibbonInfo[2], strTotal, arrRibbonInfo[1]);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure Tab/Menu caption is correct";
                return false;
            }
            var buttonOrPopUPToolUIElement = ReflectorForCSharp.GetMember(targetButtonlOrPopMenu, "UIElement");
            if (buttonOrPopUPToolUIElement == null)
            {
                //strError = string.Format("can't get UIElement from [{0}]", strData);
                strError = "Object property UIElement is NULL";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            string tmpError = "",
                strAdvTmp = "",
                strStackTmp = "";
            bool isOk = true;
            if (strTargetToolOrPopMenuButtonTyps.IndexOf("PopupMenuTool") >= 0)
            {
                //判断是否有4个参数
                if (arrRibbonInfo.Length < 4)
                {
                    strError = string.Format("The button is a popupmenu button, at least 4 paramete is required. but it is:[{0}]", strParameter);
                    strError = $"Keyword  [selectTab] does not support parameter [{strParameter}]";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Check the keyword/parameter, see user manual";
                    return false;
                }
                //再获取一次tools，判断
                //先点击该按钮然后等待
                var bottonToolUIRect = ReflectorForCSharp.GetMember(buttonOrPopUPToolUIElement, "Rect");
                if ((bottonToolUIRect == null) || (!(bottonToolUIRect is Rectangle)))
                {
                    //strError = string.Format(" popup, can't get Rect from [{0}]", buttonOrPopUPToolUIElement.GetType());
                    strError = "Object Property Rect is NULL";
                    strStack = "can't get Rect from [{0buttonOrPopUPToolUIElement.GetType()}]\r\n" + MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    isOk = false;
                    return false;
                }
                Rectangle btnTlUIRct = (Rectangle)bottonToolUIRect;
                btnTlUIRct = c.RectangleToScreen(btnTlUIRct);
                Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(btnTlUIRct.Left + btnTlUIRct.Width / 2, btnTlUIRct.Top + btnTlUIRct.Height / 2);
                Thread.Sleep(500);
                Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                            //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                            c.Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                            1000,
                            out lpdwResult
                        );
                isFind = false;
                object objTargetPopupMenuItem = null;
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    var toolsOfPopUpMenu = ReflectorForCSharp.GetMember(targetButtonlOrPopMenu, "Tools");
                    if ((toolsOfPopUpMenu == null) || (!(toolsOfPopUpMenu is ICollection)))
                    {
                        isOk = false;
                        tmpError = string.Format("Can't get Tools From :[{0}]", toolsOfPopUpMenu == null ? "NULL" : toolsOfPopUpMenu.GetType().ToString());
                        StackFrame stck = (new StackFrame());
                        strStackTmp = MarsErrorStacks.StackTraceDump();
                        strAdvTmp = "";
                        return;
                    }
                    ICollection lstToolsOfPupMenu = (ICollection)toolsOfPopUpMenu;
                    strTotal = "";

                    foreach (var itm in lstToolsOfPupMenu)
                    {
                        if (itm == null) continue;
                        var propsOfItm = ReflectorForCSharp.GetMember(itm, "SharedProps");
                        if (propsOfItm == null)
                        {
                            continue;
                        }
                        var captionFromPopShared = ReflectorForCSharp.GetMember(propsOfItm, "Caption");
                        string strcaptionFromPopShared = captionFromPopShared == null ? "" : captionFromPopShared.ToString();
                        strTotal = string.Format("{0};{1}", strTotal, strcaptionFromPopShared);
                        if (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(arrRibbonInfo[3], strcaptionFromPopShared))
                        {
                            isFind = true;
                            objTargetPopupMenuItem = itm;
                            break;
                        }
                    }
                }));
                if ((!isFind) || (objTargetPopupMenuItem == null))
                {
                    //strError = string.Format("Can't find [{0}] from Ribbon. Popup Mode", strParameter);
                    strError = $"Can't find [{strObjName}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure object is visible on the screen";
                    return false;
                }
                string strItmOfLstToolsOfPopMenu = ReflectorForCSharp.GetObjectBaseType(objTargetPopupMenuItem.GetType());
                if (strItmOfLstToolsOfPopMenu.IndexOf("ButtonTool") < 0)
                {
                    strError = "Object'type is not ButtonTool";
                    strStack = string.Format("the last button is not a buttontool. it is:[{0}]\r\n", strItmOfLstToolsOfPopMenu) + MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                var lastButtonUIElement = ReflectorForCSharp.GetMember(objTargetPopupMenuItem, "UIElement");
                if (lastButtonUIElement == null)
                {
                    strError = "Object property [UIElement] is NULL";
                    strStack = string.Format("can't get UIElement from object type:[{0}]\r\n", objTargetPopupMenuItem.GetType()) + MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                var lastRect = ReflectorForCSharp.GetMember(lastButtonUIElement, "Rect");

                //最后的对象类型是Infragistics.Win.UltraWinToolbars.PopupMenuControlTrusted
                if ((lastRect == null) || (!(lastRect is Rectangle)))
                {
                    strError = "Object member [Rect]'s type is not Rectangle";
                    strStack = string.Format("rect is not Rectangle, or it is null. it is :[{0}]\r\n", lastRect == null ? "NULL" : lastRect.GetType().ToString()) + MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return true;
                }
                Rectangle lastRct = (Rectangle)lastRect;
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("rect before convert:[{0}-{1},{2}-{3}]", lastRct.Left, lastRct.Top, lastRct.Width, lastRct.Height));
                lastRct = c.RectangleToScreen(lastRct);
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("rect after convert:[{0}-{1},{2}-{3}]", lastRct.Left, lastRct.Top, lastRct.Width, lastRct.Height));
                
                return true;
            }
            else
            {
                //默认是buttonTool                
                if (c.InvokeRequired)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                    {
                        Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                            //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                            c.Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                            1000,
                            out lpdwResult
                        );
                        var bottonToolUIRect = ReflectorForCSharp.GetMember(buttonOrPopUPToolUIElement, "Rect");
                        if ((bottonToolUIRect == null) || (!(bottonToolUIRect is Rectangle)))
                        {
                            tmpError = string.Format("can't get Rect from [{0}]", buttonOrPopUPToolUIElement.GetType());
                            StackFrame stck = (new StackFrame());
                            strStackTmp = MarsErrorStacks.StackTraceDump();
                            strAdvTmp = "";
                            isOk = false;
                            return;
                        }
                        Rectangle btnTlUIRct = (Rectangle)bottonToolUIRect;
                        btnTlUIRct = c.RectangleToScreen(btnTlUIRct);
                        Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(btnTlUIRct.Left + btnTlUIRct.Width / 2, btnTlUIRct.Top + btnTlUIRct.Height / 2);
                        Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                            //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                            c.Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                            1000,
                            out lpdwResult
                        );
                        isOk = true;
                    }));
                    if (!isOk)
                    {
                        strError = tmpError;
                        strAdv = strAdvTmp;
                        strStack = strStackTmp;
                        return false;
                    }
                    return true;
                }
                else
                {
                    Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                            //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                            c.Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                            1000,
                            out lpdwResult
                        );
                    var bottonToolUIRect = ReflectorForCSharp.GetMember(buttonOrPopUPToolUIElement, "Rect");
                    if ((bottonToolUIRect == null) || (!(bottonToolUIRect is Rectangle)))
                    {
                        simpleLog.MarsLoggerSimple.Error("", strError = string.Format("can't get Rect from [{0}]", buttonOrPopUPToolUIElement.GetType()));
                        strError = "Object property [Rect] is NULL";
                        strStack = $"can't get Rect From {buttonOrPopUPToolUIElement.GetType()} \r\n" + MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    Rectangle btnTlUIRct = (Rectangle)bottonToolUIRect;
                    btnTlUIRct = c.RectangleToScreen(btnTlUIRct);
                    Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(btnTlUIRct.Left + btnTlUIRct.Width / 2, btnTlUIRct.Top + btnTlUIRct.Height / 2);
                    Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(
                        //new System.Runtime.InteropServices.HandleRef(c, c.Handle),
                        c.Handle,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                        1000,
                        out lpdwResult
                    );
                }
                return true;
            }

        }

        #region record&replay
        private static MarsToolbarItemTyp getItemType(object tool)
        {
            if (tool == null) return MarsToolbarItemTyp.tool_unknow;

            Type toolType = tool.GetType();
            if (toolType.Name.Contains("PopupMenuTool")) return MarsToolbarItemTyp.tool_menu; // Menu
            if (toolType.Name.Contains("ButtonTool")) return MarsToolbarItemTyp.tool_button;   // Button
            if (toolType.Name.Contains("ComboBoxTool")) return MarsToolbarItemTyp.tool_combobox; // Combobox
            if (toolType.Name.Contains("Text")) return MarsToolbarItemTyp.tool_combobox; // Combobox
            return MarsToolbarItemTyp.tool_unknow;
        }

        private static string GetMenuPath(object menuTool)
        {
            string path = "";
            var parent = menuTool.GetType().GetProperty("ParentTool")?.GetValue(menuTool);
            while (parent != null)
            {
                var caption = parent.GetType().GetProperty("Caption")?.GetValue(parent)?.ToString();
                path = $"{caption}/{path}";
                parent = parent.GetType().GetProperty("ParentTool")?.GetValue(parent);
            }
            return path.TrimEnd('/');
        }
        /// <summary>
        /// 递归调用menu item，获得tool，
        /// </summary>
        /// <param name="menuInfo"></param>
        /// <param name="pt"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="caption"></param>
        /// <returns></returns>
        private static bool GetToolsElementsFromPopupMenu(object menuInfo, Point pt, ref bool isExists ,ref string strError, ref string caption)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetToolsElementsFromPopupMenu", $"point|{pt}|");
            const string toolPropertyName = "Tools";
            if (menuInfo == null)
            {
                strError = "object is null";
                simpleLog.MarsLoggerSimple.Error("GetToolsElementsFromPopupMenu", strError);
                return false;
            }
            bool isOk = false;
            string strType = menuInfo.GetType().FullName;
            var objTool = ReflectorForCSharp.GetPropertyValueByPropertyNameIdx(menuInfo, toolPropertyName, ref isExists);
            if (isExists)// actually, if isExists true, then no tools there.
            {
                strError = $"{toolPropertyName} is not exists in the |{strType}|";
                return false;
            }
            var oLstFromTools = ReflectorForCSharp.GetMember(objTool, "List");
            if (!(oLstFromTools is System.Collections.ArrayList))
            {
                strError = $"Object property [List]'s type is not ArrayList";// string.Format("List should be ArrayList, but is :[{0}]", oLstFromTools == null ? "NULL" : oLstFromTools.GetType().ToString());
                
                return isOk = false;
            }
            System.Collections.ArrayList lstFromTools = (System.Collections.ArrayList)oLstFromTools;
            object oMenuRoot = null;
            string strKeys = "";
            for (int i = 0; i < lstFromTools.Count; i++)
            {
                var itmMenu = lstFromTools[i];
                if (itmMenu == null) continue;
                object uiElement = ReflectorForCSharp.GetMember(itmMenu, "UIElement");
                var oCaption = ReflectorForCSharp.GetMember(itmMenu, "CaptionAsToolTip");
                if (uiElement == null)
                {

                }
                
                if ((oCaption == null) || ((oCaption as string) == null)) continue;
                string strCaption = (string)oCaption;

                strKeys += (strCaption == null ? ";" : strCaption + ";");
                strCaption = strCaption.Replace("&", "");

                //if (string.Compare(strCaption, strMenuToLookup1, true) != 0)
                //{
                //    if (!MarsWindowsAPIsExtend.RegularTest(strMenuToLookup1, strCaption))
                //        continue;
                //}
                oMenuRoot = itmMenu;
                break;
            }

            return false; 
        }

        public static bool GetToolbarButtonsInfo(object ultraToolbarsDockArea, Point pt, ref string strError, ref string caption, ref string mainPrefix,
            ref string menuPath, ref MarsToolbarItemTyp currentClickAtType)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetToolbarButtonsInfo");
            bool isOk = false;
            if (ultraToolbarsDockArea == null)
            {
                simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = "UltraToolbarsDockArea object is null.");
                return false;
            }

            try
            {
                // Step 1: Get the UltraToolbarsManager
                var managerField = ultraToolbarsDockArea.GetType().GetProperty("ToolbarsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var ultraToolbarsManager = managerField?.GetValue(ultraToolbarsDockArea);
                if (ultraToolbarsManager == null)
                {
                    simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = "ToolbarsManager is null. Make sure the UltraToolbarsDockArea is initialized.");
                    return false;
                }

                // Step 2: Get the Tools collection from the ToolbarsManager
                var ToolBars = ReflectorForCSharp.GetMember(ultraToolbarsManager, "Toolbars"); 
                if (ToolBars == null)
                {
                    simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = "No tools found in the ToolbarsManager.");
                    return false;
                }
                Point localPoint = ((System.Windows.Forms.Control)ultraToolbarsDockArea).PointToClient(pt);

                var iCount = ReflectorForCSharp.GetMember(ToolBars, "Count");
                if (!(iCount is int))
                {
                    //strError = string.Format("Count should be int, but it is [{0}]", iCount.GetType().ToString());
                    strError = "Object member [Count]'s type is not int";
                    return false;
                }
                var toolBarList = ReflectorForCSharp.GetMember(ToolBars, "List");
                System.Collections.ArrayList arrList = (System.Collections.ArrayList)toolBarList;
                System.Collections.ArrayList arrToolsList = null;
                string allCmd = "", allTooltips = "";
                bool isFind = false;
                foreach (var itm in arrList)
                {
                    if (itm == null) continue;
                    var key = ReflectorForCSharp.GetMember(itm, "Key") as string;
                    mainPrefix = key;
                    try
                    {
                        arrToolsList = ReflectorForCSharp.GetMember(ReflectorForCSharp.GetMember(itm, "Tools"), "List") as System.Collections.ArrayList;
                        if (arrToolsList == null)
                        {
                            simpleLog.MarsLoggerSimple.Info("\t", "no such feature [List] from tools above type");
                            continue;
                        }
                    }
                    catch(Exception e)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("Exception:[{0}]", e.Message));
                        continue;
                    }
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("ToolBars.tools count:[{0}]", arrToolsList == null ? -1 : arrToolsList.Count));
                    bool isExists = false;
                    foreach (var tool in arrToolsList) //tools container
                    {
                        if (tool == null) continue;
                        /// 获得每个item的
                        /// // Get the tool's caption
                        var captionProperty = tool.GetType().GetProperty("CaptionResolved", BindingFlags.Instance | BindingFlags.Public);
                        if (captionProperty == null)
                        {
                            strError = $"No CaptionResolved exists, please ensure the infragistics' version";
                            return false;
                        }
                        caption = captionProperty?.GetValue(tool)?.ToString() ?? "No Caption";
                        allCmd = $"{allCmd}|{caption}";
                        // Get the tool's tooltip text
                        var toolTipTextProperty = tool.GetType().GetProperty("ToolTipTextResolved", BindingFlags.Instance | BindingFlags.Public);
                        string toolTip = toolTipTextProperty?.GetValue(tool)?.ToString() ?? "No Tooltip";
                        allTooltips = $"{allTooltips}|{toolTip}";
                        //System.Drawing.Rectangle rect = ReflectorForCSharp.GetPropertyValue<System.Drawing.Rectangle>(tool, "Bounds", ref strError, ref isOk);
                        //if ((rect != null) && (rect.Contains(localPoint)))
                        //{
                        //    isFind = true;
                        //}
                        // Get the tool's UI element to calculate its rectangle
                        var uiElementMethod = tool.GetType().GetMethod("get_UIElement", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        var uiElement = uiElementMethod?.Invoke(tool, null);
                        Rectangle? rectangle = null;
                        if (uiElement != null)
                        {
                            var rectProperty = uiElement.GetType().GetProperty("Rect", BindingFlags.Instance | BindingFlags.Public);
                            rectangle = (Rectangle?)rectProperty?.GetValue(uiElement);
                            if ((rectangle != null) && (rectangle.Value.Contains(localPoint)))
                            {
                                currentClickAtType=getItemType(tool);
                                simpleLog.MarsLoggerSimple.Info("\t=======\t", $"click menu|{tool.GetType().FullName}|");
                                isFind = true;
                                var clickMethod = tool.GetType().GetMethod("OnToolClick", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, 
                                    null, new[] { typeof(EventArgs) }, null);
                                if (clickMethod != null)
                                {
                                    clickMethod?.Invoke(tool, new object[] { EventArgs.Empty });
                                }

                            }
                            else
                            {
                                if (rectangle == null) continue;
                                /// ccheck sub items
                                /// 
                                GetToolsElementsFromPopupMenu(tool, pt, ref isExists, ref strError, ref caption);
                            }
                        }
                        
                        if (isFind) return true;
                    }
                }                  
                strError = "please ensure click at validate place";
                return false;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = $"Error while retrieving toolbar buttons information: {ex.Message}");
                return false;
            }
        }

        private static string GetMenuPath(object sourcePopupMenuItem,ref bool isOk)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetMenuPath");
            try
            {                
                string strError = "";
                string parentTypes = ReflectorForCSharp.GetObjectBaseType(sourcePopupMenuItem.GetType(), false);
                if (parentTypes.IndexOf(".PopupMenuTool", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    if (parentTypes.IndexOf(".ButtonTool", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string strCaption = ReflectorForCSharp.GetPropertyValue<string>(sourcePopupMenuItem, "CaptionResolved", ref strError, ref isOk);
                        if (!isOk) return null;
                        return strCaption;
                    }
                    isOk = false;
                    return null;
                }
                /// .popupMenuTool
                /// 
                string strCaption_cur = ReflectorForCSharp.GetPropertyValue<string>(sourcePopupMenuItem, "CaptionResolved", ref strError, ref isOk);
                if (!isOk)
                {
                    simpleLog.MarsLoggerSimple.Error("GetMenuPath", $"can't get CaptionResolved from |{sourcePopupMenuItem?.GetType().FullName}| with error|{strError}|");
                    return null;
                }
                // remove &
                strCaption_cur = strCaption_cur?.Replace("&", "");
                var menuItemOwner = ReflectorForCSharp.GetMember(sourcePopupMenuItem, "Owner");
                if (menuItemOwner != null)
                {
                    string tmpCaption = GetMenuPath(menuItemOwner, ref isOk);
                    if ((isOk)&&(!string.IsNullOrEmpty(tmpCaption)))
                    {
                        isOk = true;
                        return $"{tmpCaption};{strCaption_cur}";
                    }
                    isOk = true;
                    return strCaption_cur;
                }
                return strCaption_cur;
            }
            catch (Exception e)
            {
                isOk = false;
                simpleLog.MarsLoggerSimple.Error("GetMenuPath", e.Message, e);
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetMenuPath");
            }
        }

        public static bool GetMenuInfo(object menuObject, Point pt, ref string strError, ref string caption, ref string mainPrefix,
            ref string menuPath, ref MarsToolbarItemTyp currentClickAtType)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetToolbarButtonsInfo");
            bool isOk = false;
            if (menuObject == null)
            {
                simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = "UltraToolbarsDockArea object is null.");
                return false;
            }
            bool isNotExists = false;
            var managerFieldx = menuObject.GetType().GetProperty("ToolbarsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var ultraToolbarsManagerx = managerFieldx?.GetValue(menuObject);
            if (ultraToolbarsManagerx == null)
            {
                simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = "ToolbarsManager is null. Make sure the UltraToolbarsDockArea is initialized.");
                return false;
            }

            ReflectorForCSharp reflect = new ReflectorForCSharp();
            var method_UIElementFromPoint = reflect.GetMethod(ultraToolbarsManagerx, "UIElementFromPoint");
            if (method_UIElementFromPoint != null)
            {
                var objUIElement = method_UIElementFromPoint.Invoke(ultraToolbarsManagerx, new object[] { pt });
                simpleLog.MarsLoggerSimple.Info("GetToolbarButtonsInfo", $"uielement:|{objUIElement?.ToString()}|");
            }
            var method_ToolFromPoint = reflect.GetMethod(ultraToolbarsManagerx, "ToolFromPoint");
            if (method_ToolFromPoint != null)
            {
                var objTool = method_ToolFromPoint.Invoke(ultraToolbarsManagerx, new object[] { pt });
                simpleLog.MarsLoggerSimple.Info("GetToolbarButtonsInfo", $"uielement:|{objTool?.ToString()}|");
                var txt = ReflectorForCSharp.GetPropertyValueByPropertyNameIdx(objTool, "CaptionResolved", ref isNotExists);
                if (isNotExists)
                {
                    strError = $"no |CaptionResolved| exists in |{objTool.GetType()}|";
                    simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo",strError);
                    return false;
                }
                caption = txt?.ToString();              

                /// 获得menupath
                /// 
                var tool_own = ReflectorForCSharp.GetPropertyValueByPropertyNameIdx(objTool, "Owner", ref isNotExists);
                if ((isNotExists)||(tool_own==null))
                {
                    strError = $"no |Owner| exists in |{objTool.GetType()}|";
                    simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError);
                    return false;
                }
                string captionPath = GetMenuPath(tool_own, ref isOk);
                //var menu_caption = ReflectorForCSharp.GetPropertyValueByPropertyNameIdx(tool_own, "CaptionResolved", ref isNotExists);
                //if ((isNotExists) || (menu_caption == null))
                //{
                //    strError = $"no |Owner| exists in |{objTool.GetType()}|";
                //    simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError);
                //    return false;
                //}
                menuPath = $"{captionPath};{caption}";
                return true;
            }
            strError = $"No method |ToolFromPoint| is found, please ensure that the right Infragistics version is tested";
            return false;
            #region test code 
            //try
            //{
            //    PropertyInfo contextMenuStripProperty = menuObject.GetType()
            //    .GetProperty("ContextMenuStrip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            //    var contextMenuStrip = contextMenuStripProperty?.GetValue(menuObject) as ContextMenuStrip;
            //    if (contextMenuStrip != null)
            //    {
            //        Console.WriteLine("检测到 ContextMenuStrip：");
            //        //TraverseMenuItems(contextMenuStrip.Items);
            //        return false;
            //    }



            //    // 检测 ContextMenu 属性（旧式菜单）
            //    PropertyInfo contextMenuProperty = menuObject.GetType()
            //        .GetProperty("ContextMenu", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            //    var contextMenu = contextMenuProperty?.GetValue(menuObject) as System.Windows.Forms.ContextMenu;
            //    if (contextMenu != null)
            //    {
            //        Console.WriteLine("检测到 ContextMenu：");
            //        //TraverseMenuItems(contextMenu.MenuItems);
            //        return false;
            //    }


            //    // 检测 Infragistics 自定义菜单（如 PopupMenu）
            //    var managerField = menuObject.GetType().GetProperty("ToolbarsManager", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            //    var ultraToolbarsManager = managerField?.GetValue(menuObject);
            //    if (ultraToolbarsManager == null)
            //    {
            //        simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = "ToolbarsManager is null. Make sure the UltraToolbarsDockArea is initialized.");
            //        return false;
            //    }

            //    // Step 2: Get the Tools collection from the ToolbarsManager
            //    var ToolBars = ReflectorForCSharp.GetMember(ultraToolbarsManager, "Toolbars");
            //    if (ToolBars == null)
            //    {
            //        simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = "No tools found in the ToolbarsManager.");
            //        return false;
            //    }
            //    Point localPoint = ((System.Windows.Forms.Control)menuObject).PointToClient(pt);

            //    var iCount = ReflectorForCSharp.GetMember(ToolBars, "Count");
            //    if (!(iCount is int))
            //    {
            //        //strError = string.Format("Count should be int, but it is [{0}]", iCount.GetType().ToString());
            //        strError = "Object member [Count]'s type is not int";
            //        return false;
            //    }
            //    var toolBarList = ReflectorForCSharp.GetMember(ToolBars, "List");
            //    System.Collections.ArrayList arrList = (System.Collections.ArrayList)toolBarList;
            //    System.Collections.ArrayList arrToolsList = null;
            //    string allCmd = "", allTooltips = "";
            //    bool isFind = false;
            //    foreach (var itm in arrList)
            //    {
            //        if (itm == null) continue;
            //        var key = ReflectorForCSharp.GetMember(itm, "Key") as string;
            //        mainPrefix = key;
            //        try
            //        {
            //            arrToolsList = ReflectorForCSharp.GetMember(ReflectorForCSharp.GetMember(itm, "Tools"), "List") as System.Collections.ArrayList;
            //            if (arrToolsList == null)
            //            {
            //                simpleLog.MarsLoggerSimple.Info("\t", "no such feature [List] from tools above type");
            //                continue;
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            simpleLog.MarsLoggerSimple.Info("\t", string.Format("Exception:[{0}]", e.Message));
            //            continue;
            //        }
            //        simpleLog.MarsLoggerSimple.Info("\t", string.Format("ToolBars.tools count:[{0}]", arrToolsList == null ? -1 : arrToolsList.Count));
            //        foreach (var tool in arrToolsList) //tools container
            //        {
            //            if (tool == null) continue;
            //            /// 获得每个item的
            //            /// // Get the tool's caption
            //            var captionProperty = tool.GetType().GetProperty("CaptionResolved", BindingFlags.Instance | BindingFlags.Public);
            //            if (captionProperty == null)
            //            {
            //                strError = $"No CaptionResolved exists, please ensure the infragistics' version";
            //                return false;
            //            }
            //            caption = captionProperty?.GetValue(tool)?.ToString() ?? "No Caption";
            //            allCmd = $"{allCmd}|{caption}";
            //            // Get the tool's tooltip text
            //            var toolTipTextProperty = tool.GetType().GetProperty("ToolTipTextResolved", BindingFlags.Instance | BindingFlags.Public);
            //            string toolTip = toolTipTextProperty?.GetValue(tool)?.ToString() ?? "No Tooltip";
            //            allTooltips = $"{allTooltips}|{toolTip}";
            //            //System.Drawing.Rectangle rect = ReflectorForCSharp.GetPropertyValue<System.Drawing.Rectangle>(tool, "Bounds", ref strError, ref isOk);
            //            //if ((rect != null) && (rect.Contains(localPoint)))
            //            //{
            //            //    isFind = true;
            //            //}
            //            // Get the tool's UI element to calculate its rectangle
            //            var uiElementMethod = tool.GetType().GetMethod("get_UIElement", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            //            var uiElement = uiElementMethod?.Invoke(tool, null);
            //            Rectangle? rectangle = null;
            //            if (uiElement != null)
            //            {
            //                var rectProperty = uiElement.GetType().GetProperty("Rect", BindingFlags.Instance | BindingFlags.Public);
            //                rectangle = (Rectangle?)rectProperty?.GetValue(uiElement);
            //                if ((rectangle != null) && (rectangle.Value.Contains(localPoint)))
            //                {
            //                    currentClickAtType = getItemType(tool);
            //                    simpleLog.MarsLoggerSimple.Info("\t=======\t", $"click menu|{tool.GetType().FullName}|");
            //                    isFind = true;
            //                    var clickMethod = tool.GetType().GetMethod("OnToolClick", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
            //                        null, new[] { typeof(EventArgs) }, null);
            //                    if (clickMethod != null)
            //                    {
            //                        clickMethod?.Invoke(tool, new object[] { EventArgs.Empty });
            //                    }

            //                }
            //            }

            //            if (isFind) return true;
            //        }
            //    }
            //    strError = "please ensure click at validate place";
            //    return false;
            //}
            //catch (Exception ex)
            //{
            //    simpleLog.MarsLoggerSimple.Error("GetToolbarButtonsInfo", strError = $"Error while retrieving toolbar buttons information: {ex.Message}");
            //    return false;
            //}
            #endregion
        }

        #endregion
    }
}
