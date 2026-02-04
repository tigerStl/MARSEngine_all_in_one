using Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    internal class MarsComboboxOperation : ThirdPartControlOpBase
    {

        public const string cnst_OpMode_mouseDrop = "ByMouseDrop:-{0,1}[0-9]{1,2},-{0,1}[0-9]{1,2}";
        public const string cnst_DropdownType = "Infragistics.Win.ValueListDropDownUnsafe";

        public bool SelectDropDown(string strDataToFill, string strParamtereX, string strPegName, string strObjName,
            System.Windows.Forms.Control cntrlTarget,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strStack,
            ref string strAdv, //advice
            ref string strSnapshotForShouldBeFile,
            MarsKeywordAppSideOperation funcForDismiss = null
            )
        {
            string strParamter = strParamtereX == null ? "" : strParamtereX;
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("SelectDropDown-Data-[{0}] Para:[{1}]", strDataToFill, strParamter));
            try
            {
                if (MarsWindowsAPIsExtend.RegularTest(cnst_OpMode_mouseDrop, strParamter))
                {
                    return DealWithComboboxSelectionByMouseMode(strParamter, strDataToFill, cntrlTarget, ref strError, ref strStack, ref strAdv);
                }

                object oItmCnt = ReflectorForCSharp.GetMember(cntrlTarget, "ItemCount");

                if (!(oItmCnt is int))
                {
                    strError = "No ItemCount returns";
                    return false;
                }
                int iCount = (int)oItmCnt;
                object oItm = ReflectorForCSharp.GetMember(ReflectorForCSharp.GetMember(cntrlTarget, "Items"), "List");
                if ((oItm == null) || (!(oItm is System.Collections.ArrayList)))
                {
                    strError = "List from combobox is null or is not ArrayList";
                    return false;
                }
                ///算法：
                /// 1，先找到目标的index
                /// 2，通过直接设置SelectedItem处理（可能会crash）
                ///    或者通过发送{up}或者{down}判断 --复杂
                System.Collections.ArrayList arrList = (System.Collections.ArrayList)oItm;
                string strDisplayText = "", strDataValue = "";
                strError = "";
                int idxForItm = -1;
                for (int i = 0; i < arrList.Count; i++)
                {
                    var itm = arrList[i];
                    if (itm == null) continue;
                    if (string.Compare("Infragistics.Win.ValueListItem", itm.GetType().ToString(), true) == 0)
                    {
                        //处理ValueLiteItem
                        object oDisplayText = ReflectorForCSharp.GetMember(itm, "DisplayText");
                        object oDatavalue = ReflectorForCSharp.GetMember(itm, "DataValue");
                        if (oDisplayText == null)
                            strDisplayText = "";
                        else
                            strDisplayText = (string)oDisplayText;

                        if (oDatavalue == null)
                        {
                            strDataValue = "";
                        }
                        else
                        {
                            try
                            {
                                strDataValue = oDatavalue.ToString();
                            }
                            catch (Exception e)
                            {
                                simpleLog.MarsLoggerSimple.Error("\t", string.Format("Exception [{0}] when call Tostring From DataValue, type:[{1}]", e.Message, oDatavalue == null ? "null" : oDatavalue.GetType().ToString()));
                                strDataValue = "";
                            }
                        }
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("Data to Compare, display [{0}]-value [{1}]", strDisplayText, strDataValue));
                        if ((string.Compare(strDisplayText, strDataToFill, true) == 0)
                            || (MarsWindowsAPIsExtend.RegularTest(strDataToFill, strDisplayText))
                            || (string.Compare(strDataToFill, strDataValue, true) == 0)
                            || (MarsWindowsAPIsExtend.RegularTest(strDataToFill, strDataValue))
                            )
                        {
                            #region 作废 很多情况下 ListIndex为kong
                            //object oIdx = ReflectorForCSharp.GetMember(itm, "ListIndex");
                            //if (oIdx == null)
                            //{
                            //    strError = "ListIndex is null;";
                            //    continue;
                            //}
                            //if (!(oIdx is int))
                            //{
                            //    if ((int)oIdx<=-1)
                            //    {
                            //        strError = "ListIndex is less than 0;";
                            //        continue;
                            //    }
                            //}
                            //idxForItm = (int)oIdx;
                            #endregion //作废 很多情况下 ListIndex为kong
                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("Located item [{0}] after compare against:[{1}] Display:[{2}]-value[{3}]", i, strDataToFill, strDisplayText, strDataValue));
                            idxForItm = i;
                            break;
                        }
                        continue;
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("data type:[{0}] , contact Marquis for advanced support", itm.GetType().ToString()));
                    }

                }
                if (idxForItm == -1)
                {
                    strError = string.Format("{0} not find the itm to fill:[{1}]", strError, strDataToFill);
                    return false;
                }
                /// try to set selected index dirctly
                /// 
                string strErrorTmp = "";
                bool isOkTmp = false;
                //MarsWindowsAPIsExtend.LeftMouseClick(cntrlTarget, E_ClickPosition.e_Center);
                //cntrlTarget.Focus();
                if (!MarsWindowsAPIsExtend.SetFoucsByMessage(cntrlTarget.Handle, ref strError))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", string.Format("Error from MarsWindowsAPIsExtend.SetFoucsByMessage-{0}", strError));
                    return false;
                }
                IntPtr lpdwResult;
                MarsWindowsAPIs.SendMessageTimeout(cntrlTarget.Handle, 0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    5000, //5seconds
                    out lpdwResult);

                MarsWindowsAPIsExtend.LeftMouseClick(cntrlTarget, E_ClickPosition.e_Center);
                System.Threading.Thread.Sleep(200);
                MarsWindowsAPIsExtend.LeftMouseClick(cntrlTarget, E_ClickPosition.e_Center);
                System.Threading.Thread.Sleep(200);
                MarsWindowsAPIs.SendMessageTimeout(cntrlTarget.Handle, 0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    5000, //5seconds
                    out lpdwResult);
                //windowsWrapper.SystemUtil.MarsWindowsAPIs.keybd_event((byte)System.Windows.Forms.Keys.Down, 0x68, 0, 0);
                //windowsWrapper.SystemUtil.MarsWindowsAPIs.keybd_event((byte)System.Windows.Forms.Keys.Down, 0x68, 0, 0);
                //windowsWrapper.SystemUtil.MarsWindowsAPIs.keybd_event((byte)System.Windows.Forms.Keys.Down, 0x68, 0, 0);

                #region update item index
                //if (string.Compare("ByKeyboard", strParamter ?? "", true) == 0)
                //{
                //    System.Windows.Forms.SendKeys.SendWait(strDataToFill);
                //    isOkTmp = true;
                //}
                //else
                {
                    bool isNotExists = false;
                    object oSelectIdIdx = ReflectorForCSharp.GetMember(cntrlTarget, "SelectedIndex", ref isNotExists);
                    if (isNotExists)
                    {
                        simpleLog.MarsLoggerSimple.Error("SelectDropDown", strError = string.Format("no SelectIndex exists in type [{0}]", cntrlTarget.GetType()));
                        return false;
                    }

                    if (cntrlTarget.InvokeRequired)
                    {
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                        cntrlTarget.Invoke(
#endif

                        new Action(() =>
                        {

                        }));
                    }
                    else
                    {
                        System.Windows.Forms.SendKeys.SendWait("{UP}");
                    }


                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("SelectDropDown after set focus, get SelectecIdx from Reflector,[{0}]", oSelectIdIdx));

                    int iIdx = -1;
                    if (!int.TryParse(oSelectIdIdx == null ? "" : oSelectIdIdx.ToString(), out iIdx))
                    {
                        //if (cntrlTarget.InvokeRequired)
                        //{
                        //    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(()=>{ System.Windows.Forms.SendKeys.SendWait("{UP}"); }));                            
                        //}
                        //else
                        {
                            System.Windows.Forms.SendKeys.SendWait("{UP}");
                        }
                    }
                    //make sure the contorl is available 
                    MarsWindowsAPIs.SendMessageTimeout(cntrlTarget.Handle, 0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    5000, //5seconds
                    out lpdwResult);
                    //reget the item index
                    oSelectIdIdx = ReflectorForCSharp.GetMember(cntrlTarget, "SelectedIndex", ref isNotExists);
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("SelectDropDown get SelectecIdx from Reflector,[{0}] after send UP", oSelectIdIdx));

                    if (!int.TryParse(oSelectIdIdx == null ? "" : oSelectIdIdx.ToString(), out iIdx))
                    {
                        iIdx = -1;
                    }
                    int iDis = idxForItm - iIdx;
                    string strKey = "{UP}";
                    if (iDis >= 0)
                    {
                        strKey = "{DOWN}";
                    }

                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("set Up or Down ==>[{0}]", strKey));
                    int iPosAutoClick = -1;

                    strParamter = string.IsNullOrEmpty(strParamter) ? "" : strParamter;

                    string strParaUpper = strParamter.ToUpper();
                    iPosAutoClick = strParaUpper.IndexOf("AUTOCLICKPOPUP:");
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("upper:[{0}] index:[{1}]", strParaUpper, iPosAutoClick));
                    bool isAutoClickPop = strParamter == null ? false : (iPosAutoClick = (strParamter.ToUpper().IndexOf("AUTOCLICKPOPUP:"))) >= 0;
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("iPosAutoClick={0}", iPosAutoClick));

                    int iPosDialogButton = iPosAutoClick >= 0 ? strParamter.IndexOf(":", iPosAutoClick) : -1;

                    iPosAutoClick = iPosAutoClick < 0 ? -1 : iPosAutoClick + "AUTOCLICKPOPUP:".Length;
                    string strAutoClickDialogStr = "YES", strTmpDataForDis = "";
                    Dictionary<string, string> dictObjProperties = new Dictionary<string, string>(),
                        dictPegProperties = new Dictionary<string, string>();

                    if (isAutoClickPop)
                    {

                        simpleLog.MarsLoggerSimple.Info("\t\t", string.Format("pos of ':' -{0}, iPosDialogButton mark ends:[{1}]", iPosDialogButton, iPosAutoClick));

                        if ((iPosDialogButton < 0)
                            //||(iPosDialogButton< iPosAutoClick-1)
                            )
                        {
                            strAutoClickDialogStr = "YES";
                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("auto click popup mode, with search buton text:[{0}]", strAutoClickDialogStr));
                        }
                        else
                        {

                            strAutoClickDialogStr = strParamter.Substring(iPosDialogButton + 1);
                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("find AutoclickPopup, [{0}] from {1} to {2} get:[{3}]", strParamter, isAutoClickPop, iPosDialogButton, strAutoClickDialogStr));
                            dictPegProperties.Add("OBJECT CLASS", "#32770");
                            dictPegProperties.Add("index", "0");
                            dictObjProperties.Add("text", strAutoClickDialogStr);
                        }

                    }
                    bool isContinueToClick = iDis != 0;
                    int i = 0;
                    object oCheckIdx = null;
                    int iCheckIdx = -1;
                    while (isContinueToClick)
                    //for (int i=0;i<Math.Abs(iDis);i++)
                    {
                        #region clean
                        //if (cntrlTarget.InvokeRequired)
                        //{
                        //    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        //        new Action(() =>
                        //        {
                        //            System.Windows.Forms.SendKeys.SendWait(strKey);
                        //        })
                        //        );

                        //}
                        //else
                        #endregion
                        {
                            System.Windows.Forms.SendKeys.SendWait(strKey);
                        }
                        System.Threading.Thread.Sleep(10);
                        MarsWindowsAPIs.SendMessageTimeout(cntrlTarget.Handle, 0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            MarsWindowsAPIs.SMTO_BLOCK,
                            5000, //5seconds
                            out lpdwResult);
                        if (isAutoClickPop)
                        {
                            System.Threading.Thread.Sleep(490);
                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("#{0}, going to call dissmiss", i));
                            if (funcForDismiss == null)
                            {
                                simpleLog.MarsLoggerSimple.Info("\t\t", "no dismisss func passed");
                                continue;
                            }
                            funcForDismiss("2", strAutoClickDialogStr, "SWFBUTTON", "", strPegName, strObjName, dictObjProperties, dictPegProperties,
                                errorCheckObj,
                                ref strError, ref strTmpDataForDis,
                                ref strStack,
                                ref strAdv, //advice
                                ref strSnapshotForShouldBeFile,
                                true);
                            System.Threading.Thread.Sleep(100);
                            cntrlTarget.Focus(); //MAKE SURE THE CONTROL WILL TAKE THE KEYBOARD PRESSING
                            System.Threading.Thread.Sleep(20);
                        }
                        if (cntrlTarget.InvokeRequired)
                        {
                            cntrlTarget.Invoke(new Action(() =>
                            {
                                oCheckIdx = ReflectorForCSharp.GetMember(cntrlTarget, "SelectedIndex", ref isNotExists);
                            }));
                        }
                        else
                        {
                            oCheckIdx = ReflectorForCSharp.GetMember(cntrlTarget, "SelectedIndex", ref isNotExists);
                        }

                        if ((i++) >= 100)
                        {
                            isContinueToClick = false;
                            simpleLog.MarsLoggerSimple.Error("\t", string.Format("Something wrong?already clicked {0} times, but it still not [curIdx ={1} target is {2}]",
                                i, oCheckIdx == null ? "0" : oCheckIdx.ToString(), idxForItm));
                            break;
                        }

                        if (!int.TryParse(oCheckIdx == null ? "-1" : oCheckIdx.ToString(), out iCheckIdx))
                        {
                            isContinueToClick = i < Math.Abs(iDis);
                        }
                        else
                        {
                            if (iCheckIdx < idxForItm)
                            {

                                strKey = "{DOWN}";
                                isContinueToClick = true;
                            }
                            else
                            {
                                if (iCheckIdx > idxForItm)
                                {
                                    strKey = "{UP}";
                                    isContinueToClick = true;
                                }
                                else
                                {
                                    break;//find
                                }
                            }
                        }
                    }

                    /// to check whether the current index is the target
                    /// 
                    oCheckIdx = ReflectorForCSharp.GetMember(cntrlTarget, "SelectedIndex", ref isNotExists);
                    //int iCheckIdx = ReflectorForCSharp.GetMemberByType<int>(cntrlTarget, "SelectedIndex");

                    if (!int.TryParse(oCheckIdx == null ? "-1" : oCheckIdx.ToString(), out iCheckIdx))
                        iCheckIdx = -1;
                    if ((!string.IsNullOrEmpty(strParamter)) && (strParamter.ToUpper().IndexOf("DIRECT_MOUSE") < 0))
                        //if (string.Compare("DIRECT_MOUSE", strParamter, true) != 0)
                        isOkTmp = iCheckIdx == idxForItm;
                    else
                        isOkTmp = true;
                    if (!isOkTmp)
                    {
                        strErrorTmp = string.Format("target index [{0}] and current idx [{1}] are not equal.", idxForItm, iCheckIdx);
                    }

                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("SelectDropDown get SelectecIdx from Reflector,targetIdx:[{0}]-currentIdx:[{2}] after Key up down presssed times:[{1}]",
                        oSelectIdIdx, iDis, iCheckIdx));

                    //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                    //{
                    //    isOkTmp = (new ReflectorForCSharp()).SetMemberValue(idxForItm, cntrlTarget, "SelectedIndex", ref strErrorTmp);
                    //}));
                }
                if (!isOkTmp)
                {

                    strError = strErrorTmp;
                    return false;
                }
                #endregion
                MarsWindowsAPIs.SendMessageTimeout(cntrlTarget.Handle, 0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    5000, //5seconds
                    out lpdwResult);

                System.Windows.Forms.SendKeys.SendWait("{TAB}");
                MarsWindowsAPIs.SendMessageTimeout(cntrlTarget.Handle, 0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    5000, //5seconds
                    out lpdwResult);
                return true;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                return false;
            }

        }

        private class _subMouseDropPos
        {
            internal int xPos = int.MinValue, yPos = int.MinValue;
            internal bool AnlystPara(string strPara)
            {
                if (string.IsNullOrEmpty(strPara)) return false;
                int iCommaIdx = strPara.IndexOf(":");
                string subPos = strPara.Substring(iCommaIdx + 1);
                string[] arrXy = subPos.Split(new string[] { "," },StringSplitOptions.RemoveEmptyEntries);
                if (arrXy.Length != 2) return false;

                if (!(int.TryParse(arrXy[0].Trim(), out xPos) && (int.TryParse(arrXy[1].Trim(), out yPos)))) return false;
                return true;
            }

        }

        private string GetObjectParents(Control c,string strParantsTyps)
        {
            if (c == null) return strParantsTyps;
            if (c.Parent == null) return strParantsTyps;
            string strP = $"{strParantsTyps};{c.Parent.GetType()}";
            return GetObjectParents(c.Parent, strP);
        }

        private bool DealWithComboboxSelectionByMouseMode(string strParamter, string strDataToFill, Control cntrlTarget, ref string strError, ref string strStack, ref string strAdv)
        {
            simpleLog.MarsLoggerSimple.logBegin("DealWithComboboxSelectionByMouseMode", $"{strParamter}, data-{strDataToFill}, objName:{cntrlTarget.Name}");
            try
            {
                /// 算法：
                /// 1，分析参数，获得点击的位置
                /// 2，点击下拉button
                /// 3，获得	Infragistics.Win.ValueListDropDownUnsafe的child
                /// 4，从该child中获得位置，再点击
                /// 
                _subMouseDropPos subDropBtn = new _subMouseDropPos();
                if (!subDropBtn.AnlystPara(strParamter))
                {
                    strStack = Environment.StackTrace;
                    strError = $"not match the format:{cnst_OpMode_mouseDrop}";
                    strAdv = "change the parameter format.";
                    return false;
                }
                /// 2，点击下拉button
                /// 
                Point pt = cntrlTarget.ClientRectangle.Location;
                Point btnPt = new Point(pt.X + cntrlTarget.ClientRectangle.Width + subDropBtn.xPos,
                    pt.Y + cntrlTarget.ClientRectangle.Height - subDropBtn.yPos);
                Point scrnPt = cntrlTarget.PointToScreen(btnPt);

                Control targetDropdownList = null;
                string strTmpParent = "";

                UIPermission uIPermission = new UIPermission(UIPermissionWindow.AllWindows);
                IntPtr handleFromPos = IntPtr.Zero;
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(()=> {
                    MarsWindowsAPIsExtend.LeftMouseClick(scrnPt.X, scrnPt.Y);
                    System.Threading.Thread.Sleep(500);
                    simpleLog.MarsLoggerSimple.Info("\t", $"children in dispatch count:[{cntrlTarget.Controls.Count}]");
                    handleFromPos = windowsWrapper.SystemUtil.MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(scrnPt.X- cntrlTarget.ClientRectangle.Width/2,
                        scrnPt.Y+ subDropBtn.yPos +8));
                    simpleLog.MarsLoggerSimple.Info("\t", $"handle from point in dispatcher:[{handleFromPos}]");
                    if (handleFromPos != IntPtr.Zero)
                    {
                        try
                        {
                            targetDropdownList = Control.FromHandle(handleFromPos);
                            
                            if (targetDropdownList == null)
                            {
                                simpleLog.MarsLoggerSimple.Info("\t", $"can't convert to control:[{handleFromPos}]");
                            }
                            else
                            {
                                strTmpParent = GetObjectParents(targetDropdownList, strTmpParent);
                                simpleLog.MarsLoggerSimple.Info("\t", $"dispatch, type from Handle [{handleFromPos}], {targetDropdownList.GetType()}, parent typs:{strTmpParent}");
                            }
                        }
                        catch (Exception ek)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", $"Exception:[{ek.Message}], [{ek.StackTrace}]");
                        }
                    }
                    
                })
                );
                System.Threading.Thread.Sleep(200);
                if (targetDropdownList == null)
                {
                    //retry to get the dropdown list
                    handleFromPos = windowsWrapper.SystemUtil.MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(scrnPt.X - cntrlTarget.ClientRectangle.Width / 2,
                        scrnPt.Y + subDropBtn.yPos + 8));
                    simpleLog.MarsLoggerSimple.Info("\t", $"handle from point after dispatcher:[{handleFromPos}]");
                    if (handleFromPos != IntPtr.Zero)
                    {
                        try
                        {
                            targetDropdownList = Control.FromHandle(handleFromPos);
                            if (targetDropdownList == null)
                            {
                                simpleLog.MarsLoggerSimple.Info("\t", $"can't convert to control:[{handleFromPos}]");
                            }
                            else
                            {
                                strTmpParent = "";
                                strTmpParent = GetObjectParents(targetDropdownList, strTmpParent);
                                simpleLog.MarsLoggerSimple.Info("\t", $"outter type from Handle [{handleFromPos}], " +
                                    $"{targetDropdownList.GetType()}, parent:[{strTmpParent}]");
                            }
                        }
                        catch (Exception ek)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", $"outter Exception:[{ek.Message}], [{ek.StackTrace}]");
                        }
                    }
                }
                
                /// 3，获得	Infragistics.Win.ValueListDropDownUnsafe的child
                /// 
                ReflectorForCSharp r = new ReflectorForCSharp();
                if (targetDropdownList == null)
                {
                    strError = "Can't Active dropdown list.";
                    strAdv = "adjust the Position numbers";
                    strStack = Environment.StackTrace;
                    return false ;
                }
                simpleLog.MarsLoggerSimple.Info("\t", $"rectangle of the dropdownList:{targetDropdownList.ClientRectangle}");
                /// wait until the dropdown is shown.
                /// 
                //if (!r.WaitForPerprotyValue(targetDropdownList, "isDroppingDown", false.ToString(), 10 ,ref strError, ref strStack))
                //{
                //    strAdv = "Contact Marquis";
                //    simpleLog.MarsLoggerSimple.Info("\t", strError);
                //    return false;
                //}

                //((Infragistics.Win.ValueListDropDown)targetDropdownList).ValueListItems.List
                var itms = ReflectorForCSharp.GetMember(targetDropdownList, "ValueListItems");
                simpleLog.MarsLoggerSimple.Info("\t", $"itms from [{targetDropdownList.GetType()}] is [{itms??"null"}]");
                object oItm = ReflectorForCSharp.GetMember(itms, "List");
                //object oItm = ReflectorForCSharp.GetMember(oList, "List");
                //if (!r.ObjectIsIList(oItm))
                if ((oItm == null) || (!(oItm is System.Collections.ArrayList)))
                {
                    strError = oItm==null? "List from combobox is null " : $"List from combobox is not ArrayList, type is :[{oItm.GetType()}]";
                    strStack = Environment.StackTrace;
                    strAdv = "Contct Marquis";
                    return false;
                }
                IList cmbListItms = (IList)oItm;
                int iIdx = GetTargetItemIdxFromValueList(cmbListItms, strDataToFill, ref strError, ref strAdv, ref strStack);
                if (iIdx < 0)
                {
                    return false;
                }
                //bool isNotExists = false;
                //object controlUIElment = ReflectorForCSharp.GetMember(targetDropdownList, "ControlUIElment", ref isNotExists);
                //if (isNotExists)
                //{
                //    strStack = Environment.StackTrace;
                //    strError = $"No [ControlUIElment] is found from [{targetDropdownList.GetType()}]";
                //    strAdv = "Contact Marquis";
                //    return false;
                //}
                // type is ValueListDropDownUIElement 
                ///change index
                ///
                bool isOk = r.SetMemberValue(iIdx, targetDropdownList, "SelectedIndex", ref strError, ref strStack);
                if (!isOk)
                {
                    strAdv = "Contact Marquis";
                    return false;
                }
                return true;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("DealWithComboboxSelectionByMouseMode");
            }
        }

        private int GetTargetItemIdxFromValueList(IList sourceList, 
            string strSearchData, ref string strError, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetTargetItemIdxFromValueList", $"Data to search:[{strSearchData}]");
            string strDisplayText = "", strDataValue = "";
            strError = "";
            int idxForItm = -1;
            try
            {
                simpleLog.MarsLoggerSimple.Info("\t", $"total count:[{sourceList.Count}]");
                for (int i = 0; i < sourceList.Count; i++)
                {
                    var itm = sourceList[i];
                    if (itm == null)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", $"item is null for [{i}]");
                        continue;
                    }
                    simpleLog.MarsLoggerSimple.Info("\t", $"type is :[{itm.GetType()}]");
                    if (string.Compare("Infragistics.Win.ValueListItem", itm.GetType().ToString(), true) == 0)
                    {
                        //处理ValueLiteItem
                        object oDisplayText = ReflectorForCSharp.GetMember(itm, "DisplayText");
                        object oDatavalue = ReflectorForCSharp.GetMember(itm, "DataValue");

                        simpleLog.MarsLoggerSimple.Info("\t", $"[disp:{oDisplayText}] - [value:{oDatavalue}]");

                        if (oDisplayText == null)
                            strDisplayText = "";
                        else
                            strDisplayText = (string)oDisplayText;

                        if (oDatavalue == null)
                        {
                            strDataValue = "";
                        }
                        else
                        {
                            try
                            {
                                strDataValue = oDatavalue.ToString();
                            }
                            catch (Exception e)
                            {
                                simpleLog.MarsLoggerSimple.Error("\t", string.Format("Exception [{0}] when call Tostring From DataValue, type:[{1}]", e.Message, oDatavalue == null ? "null" : oDatavalue.GetType().ToString()));
                                strDataValue = "";
                            }
                        }
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("Data to Compare, display [{0}]-value [{1}]", strDisplayText, strDataValue));
                        if ((string.Compare(strDisplayText, strSearchData, true) == 0)
                            || (MarsWindowsAPIsExtend.RegularTest(strSearchData, strDisplayText))
                            || (string.Compare(strSearchData, strDataValue, true) == 0)
                            || (MarsWindowsAPIsExtend.RegularTest(strSearchData, strDataValue))
                            )
                        {
                            #region 作废 很多情况下 ListIndex为kong
                            //object oIdx = ReflectorForCSharp.GetMember(itm, "ListIndex");
                            //if (oIdx == null)
                            //{
                            //    strError = "ListIndex is null;";
                            //    continue;
                            //}
                            //if (!(oIdx is int))
                            //{
                            //    if ((int)oIdx<=-1)
                            //    {
                            //        strError = "ListIndex is less than 0;";
                            //        continue;
                            //    }
                            //}
                            //idxForItm = (int)oIdx;
                            #endregion //作废 很多情况下 ListIndex为kong
                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("Located item [{0}] after compare against:[{1}] Display:[{2}]-value[{3}]", i,
                                strSearchData, strDisplayText, strDataValue));
                            idxForItm = i;
                            break;
                        }
                        continue;
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("data type:[{0}] , contact Marquis for advanced support", itm.GetType().ToString()));
                    }

                }
                return idxForItm;
            }catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("GetTargetItemIdxFromValueList", strError = e.Message, strStack = e.StackTrace);
                strAdv = "Contact Marquis";
                return -2;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GetTargetItemIdxFromValueList", $"returned index:[{idxForItm}]");
            }
        }
    }
}
