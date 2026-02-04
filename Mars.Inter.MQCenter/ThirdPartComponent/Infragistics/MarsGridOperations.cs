using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.Inter.MQCenter.MarsObjectIdentifier;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics
{
    public class MarsGridOperations
    {
        //private static MLogger Logger = MLogger.
        public static bool IsSupported(object c, ref string strError)
        {
            if (c == null) return false;
            string strTypePath = ReflectorForCSharp.GetObjectBaseType(c.GetType());
            if (strTypePath.IndexOf("UltraGridBase") >= 0) return true;
            simpleLog.MarsLoggerSimple.Error("IsSupported", strError = $"can't find UltraGridBase from [{strTypePath}]");
            return false;
        }

        internal bool ScrollGridByCommand(object c, string strParaMeter, string strData, ref string strError, ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("ScrollGridByCommand", $"para:[{strParaMeter}] data:[{strData}]");
            ///首先 获得最后一层的对象
            ///
            ///
            object display = ReflectorForCSharp.GetMember(c, "DisplayLayout");
            if (display == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError=$"no displayLayout in object [{c.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            object uielement = ReflectorForCSharp.GetMember(display, "UIElement");
            if (uielement == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"no uielement in object [{c.GetType()}].displayLayout:[{display.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            object ochildUIElement = ReflectorForCSharp.GetMember(uielement, "ChildElements");
            if (ochildUIElement == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"no ChildUIElement in object [{c.GetType()}].displayLayout:[{display.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            if ((!(ochildUIElement is System.Collections.ArrayList))
                ||(((System.Collections.ArrayList)ochildUIElement).Count<=0)
                )
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"ChildUIElement is empty in object [{c.GetType()}].displayLayout:[{display.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            object childUIElement0 = ((System.Collections.ArrayList)ochildUIElement)[0];
            if (childUIElement0 == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"ChildUIElement[0] is empty in object [{c.GetType()}].displayLayout:[{display.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            //object childElements = ReflectorForCSharp.GetMember(childUIElement0, "childElements");
            bool isNotExists = false;
            //object childElementsCollection = ReflectorForCSharp.GetProperty(childUIElement0, "ChildElements",ref isNotExists);
            //object childElementsCollection = ReflectorForCSharp.GetMember(childUIElement0, "childElementsCollection");
            object childElementsCollection = ReflectorForCSharp.GetMember(childUIElement0, "ChildElements");
            if ((childElementsCollection == null)
                ||((!(childElementsCollection is System.Collections.ArrayList))
                || (((System.Collections.ArrayList)childElementsCollection).Count <= 0))
                )
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"no childElementsCollection in childUIElement0, or not ArrayList, or list count is 0, [{c.GetType()}].displayLayout:[{display.GetType()}].childUIElement[0]:[{childUIElement0.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            ///分析参数
            ///
            string scrollPara = string.IsNullOrEmpty(strData) ? strParaMeter ?? "" : strData;
            string[] arrParas = scrollPara.Split(':');

            if (arrParas.Length != 2)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"the parameter or data format should be L|R|T|B:number,but it is [{scrollPara}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            System.Collections.ArrayList lstChildElementCollection = childElementsCollection as System.Collections.ArrayList;
            object colScrollbarUIElement = null;
            object rowScrollbarUIElement = null;
            object targetScrollbarUIElement = null;
            int iScrollbarCount = 0;
            object orect = null;
            foreach (var itm in lstChildElementCollection)
            {
                if (itm == null) continue;
                string objBaseType = ReflectorForCSharp.GetObjectBaseType(itm.GetType());
                if (objBaseType.IndexOf(".ColScrollbarUIElement") >= 0) //只有单条
                {
                    iScrollbarCount++;
                    if ("L".Equals(arrParas[0], StringComparison.OrdinalIgnoreCase) || ("R".Equals(arrParas[0], StringComparison.OrdinalIgnoreCase)))
                    {
                        colScrollbarUIElement = itm;
                        targetScrollbarUIElement = itm;
                    }
                }
                else if (objBaseType.IndexOf(".RowScrollbarUIElement") >= 0) //只有单条
                {
                    iScrollbarCount++;
                    if ("T".Equals(arrParas[0], StringComparison.OrdinalIgnoreCase) || ("B".Equals(arrParas[0], StringComparison.OrdinalIgnoreCase)))
                    {
                        rowScrollbarUIElement = itm;
                        targetScrollbarUIElement = itm;
                    }
                }                                
            }
            
            if (targetScrollbarUIElement == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"no colScrollbarUIElement in object, [{c.GetType()}].displayLayout:[{display.GetType()}].childUIElement[0]:[{childUIElement0.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            object ChildElements1 = ReflectorForCSharp.GetMember(targetScrollbarUIElement, "ChildElements");
            if ((ChildElements1 == null)
                || ((!(ChildElements1 is System.Collections.ArrayList))
                || (((System.Collections.ArrayList)ChildElements1).Count <= 0))
                )
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"no ChildElements in childElementsCollection[2], or not ArrayList, or list count is 0, [{c.GetType()}].displayLayout:[{display.GetType()}].childUIElement[0]:[{childUIElement0.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            System.Collections.ArrayList lstChildElements1 = (System.Collections.ArrayList)ChildElements1;
            object ScrollTrackUIElement = null;
            if (iScrollbarCount != 2)
            {
                object ColScrollbarUIElement = null;
                for (int i = 0; i < lstChildElements1.Count; i++)
                {
                    if (lstChildElementCollection[i] == null) continue;
                    string strElementType = ReflectorForCSharp.GetObjectBaseType(lstChildElementCollection[i].GetType());
                    if (strElementType.IndexOf(".ColScrollbarUIElement") >= 0)
                    {
                        ColScrollbarUIElement = lstChildElementCollection[i];
                        break;
                    }
                }
                if (ColScrollbarUIElement == null)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = $"no ColScrollbarUIElement in object, [{c.GetType()}].displayLayout:[{display.GetType()}].childUIElement[0]:[{childUIElement0.GetType()}]");
                    strAdv = "Contact Marquis";
                    strStack = MarsErrorStacks.StackTraceDump();
                    return false;
                }

                object ChildElements2 = ReflectorForCSharp.GetMember(ColScrollbarUIElement, "ChildElements");
                if ((ChildElements2 == null)
                    || ((!(ChildElements2 is System.Collections.ArrayList))
                    || (((System.Collections.ArrayList)ChildElements2).Count <= 0))
                    )
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = $"no ChildElements in ColScrollbarUIElement, or not ArrayList, or list count is 0, [{c.GetType()}].displayLayout:[{display.GetType()}].childUIElement[0]:[{childUIElement0.GetType()}]");
                    strAdv = "Contact Marquis";
                    strStack = MarsErrorStacks.StackTraceDump();
                    return false;
                }

                System.Collections.ArrayList lstChildElements2 = (System.Collections.ArrayList)ChildElements2;
                
                foreach (var itm in lstChildElements2)
                {
                    if (itm == null) continue;
                    string childElement2Type = ReflectorForCSharp.GetObjectBaseType(itm.GetType());
                    if (childElement2Type.IndexOf(".ScrollTrackUIElement") >= 0)
                    {
                        ScrollTrackUIElement = itm;
                        break;
                    }
                }
                
            }
            else
            {
                for (int i = 0; i < lstChildElements1.Count; i++)
                {
                    if (lstChildElements1[i] == null) continue;
                    string strElementType = ReflectorForCSharp.GetObjectBaseType(lstChildElements1[i].GetType());
                    if (strElementType.IndexOf(".ScrollTrackUIElement") >= 0)
                    {
                        ScrollTrackUIElement = lstChildElements1[i];
                        break;
                    }
                }                
            }

            if (ScrollTrackUIElement == null)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"no ScrollTrackUIElement in ColScrollbarUIElement,[{c.GetType()}].displayLayout:[{display.GetType()}].childUIElement[0]:[{childUIElement0.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            object ChildElements3 = ReflectorForCSharp.GetMember(ScrollTrackUIElement, "ChildElements");
            if ((ChildElements3 == null) || ((!(ChildElements3 is System.Collections.ArrayList))
                || (((System.Collections.ArrayList)ChildElements3).Count <= 0))
                )
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"no ChildElements3 in ColScrollbarUIElement,[{c.GetType()}].displayLayout:[{display.GetType()}].childUIElement[0]:[{childUIElement0.GetType()}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            System.Collections.ArrayList lstChildElements3 = (System.Collections.ArrayList)ChildElements3;
            object ScrollThumbUIElement = null;//ReflectorForCSharp.GetMember(ScrollTrackUIElement, "ScrollThumbUIElement");
            object ScrollTrackSubAreaUIElement = null;// ReflectorForCSharp.GetMember(ScrollTrackUIElement, "ScrollTrackSubAreaUIElement");

            foreach (var itm in lstChildElements3)
            {
                if (itm == null) continue;
                string basetypes = ReflectorForCSharp.GetObjectBaseType(itm.GetType());
                if (basetypes.IndexOf("ScrollThumbUIElement") >= 0)
                {
                    ScrollThumbUIElement = itm;
                    continue;
                }
                if (basetypes.IndexOf("ScrollTrackSubAreaUIElement") >= 0)
                {
                    ScrollTrackSubAreaUIElement = itm;
                    continue;
                }
            }

            if ((ScrollThumbUIElement == null) || (ScrollTrackSubAreaUIElement == null))
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = "ScrollThumbUIElement is null and ScrollTrackSubAreaUIElement is null");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            orect = ReflectorForCSharp.GetMember(ScrollTrackSubAreaUIElement, "Rect");
            if ((orect == null) || (!(orect is System.Drawing.Rectangle)))
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = "Rect is null or Rect is not Rectangle");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            System.Drawing.Rectangle rect = (System.Drawing.Rectangle)orect;
            System.Drawing.Point     pt   = default(System.Drawing.Point);
            if ("L".Equals(arrParas[0], StringComparison.OrdinalIgnoreCase))
            {
                pt = new System.Drawing.Point(rect.X+2, rect.Y+rect.Height/2);
            }
            else if ("R".Equals(arrParas[0], StringComparison.OrdinalIgnoreCase))
            {
                pt = new System.Drawing.Point(rect.X + rect.Width - 2, rect.Y + rect.Height/2);
            }
            else if ("T".Equals(arrParas[0], StringComparison.OrdinalIgnoreCase))
            {
                pt = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + 2);
            }else if ("B".Equals(arrParas[0], StringComparison.OrdinalIgnoreCase))
            {
                pt = new System.Drawing.Point(rect.X + rect.Width / 2, rect.Y + rect.Height - 2);
            }
            System.Drawing.Point ptScreen = (c as Control).PointToScreen(pt);

            int iClickCnt;
            if (!int.TryParse(arrParas[1], out iClickCnt))
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"a number should set for click times, but its [{arrParas[1]}]");
                strAdv = "Contact Marquis";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }

            for(int i=0; i < iClickCnt; i++)
            {
                MarsWindowsAPIsExtend.LeftMouseClick(ptScreen.X, ptScreen.Y);
                System.Threading.Thread.Sleep(100);
            }
            return true;
        }
    }
}
