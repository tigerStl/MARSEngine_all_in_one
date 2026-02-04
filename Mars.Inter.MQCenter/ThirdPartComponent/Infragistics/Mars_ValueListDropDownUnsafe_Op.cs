using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    internal class Mars_ValueListDropDownUnsafe_Op
    {
        private System.Windows.Forms.Control dropdownList = null;
        private string searchItem= null;

        internal Mars_ValueListDropDownUnsafe_Op(System.Windows.Forms.Control sourceContrl, string strSearchItm)
        {
            dropdownList = sourceContrl;
            searchItem = strSearchItm;
        }

        private int GetTargetItemIdxFromValueList(IList sourceList,
           string strSearchData, 
           ref string strAllText, 
           ref string strError, ref string strAdv, ref string strStack)
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
                        strAllText = $"{strAllText};[{oDisplayText}-{oDatavalue}]";

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
            }
            catch (Exception e)
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

        internal bool IndexOf(System.Windows.Forms.Control Cntrol, ref string strError, ref string strStack, ref string strAdv)
        {
            simpleLog.MarsLoggerSimple.logBegin("IndexOf" , $"try to index of [{this.searchItem}]");
            string strAllListItem = "";
            try
            {
                if (Cntrol == null)
                {
                    simpleLog.MarsLoggerSimple.Error("IndexOf", strError = "No right parent control is found");
                    strStack = strStack = Environment.StackTrace;
                    strAdv = "Please contact Marquis";
                    return false;
                }

                object itms = null;
                object oItm = null;
                if (Cntrol.InvokeRequired)
                {
                    Cntrol.Invoke(new Action(() =>{
                        itms = ReflectorForCSharp.GetMember(dropdownList, "ValueListItems");
                        simpleLog.MarsLoggerSimple.Info("\t", $"itms from [{dropdownList.GetType()}] is [{itms ?? "null"}]");
                        oItm = ReflectorForCSharp.GetMember(itms, "List");
                    }));
                }
                else
                {
                    simpleLog.MarsLoggerSimple.DEBUG("IndexOf", "invoke mode, ValueListItems");
                    itms = ReflectorForCSharp.GetMember(dropdownList, "ValueListItems");
                    simpleLog.MarsLoggerSimple.Info("\t", $"itms from [{dropdownList.GetType()}] is [{itms ?? "null"}]");
                    oItm = ReflectorForCSharp.GetMember(itms, "List");
                }                
                
                //object oItm = ReflectorForCSharp.GetMember(oList, "List");
                //if (!r.ObjectIsIList(oItm))
                if ((oItm == null) || (!(oItm is System.Collections.ArrayList)))
                {
                    strError = oItm == null ? "List from combobox is null " : $"List from combobox is not ArrayList, type is :[{oItm.GetType()}]";
                    strStack = Environment.StackTrace;
                    strAdv = "Contct Marquis";
                    return false;
                }
                IList cmbListItms = (IList)oItm;
                int iIdx = GetTargetItemIdxFromValueList(cmbListItms, this.searchItem, ref strAllListItem, ref strError, ref strAdv, ref strStack);
                if (iIdx < 0)
                {
                    strError = $"Can't find [{this.searchItem}] from [{strAllListItem}]";
                    strStack = Environment.StackTrace;
                    strAdv = $"Please change dataset and make sure [{this.searchItem}] exists";
                    return false;
                }
                ReflectorForCSharp r = new ReflectorForCSharp();
                bool isOk = r.SetMemberValue(iIdx, dropdownList, "SelectedIndex", ref strError, ref strStack);
                return isOk;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("IndexOf", $"data to search:[{this.searchItem}], all items found:[{strAllListItem}]");
            }

        }
    }
}
