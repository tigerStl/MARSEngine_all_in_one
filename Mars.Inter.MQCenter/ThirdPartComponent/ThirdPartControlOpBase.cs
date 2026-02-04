using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Logger = Mars.message.Inter.MQCenter.simpleLog.MarsLoggerSimple ;
using MarsUFTAddins.IMars.tiger;
using System.Collections;
using System.Diagnostics;
using Mars.message.AutoTestingDriver.ErrorMessage;

namespace Mars.message.Inter.MQCenter.ThirdPartComponent
{
    class MarsSearchReflection
    {
        internal System.Reflection.MemberTypes SearchType;

    }
    class ThirdPartControlOpBase
    {
        internal static void Highlight(Rectangle rect)
        {
            string strError = "";
            windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(
                    new MarsWindowsAPIs.RECT() { Left = rect.Left - 3, Right = rect.Right, Top = rect.Top - 3, Bottom = rect.Bottom },
                    ref strError
                    );
        }
        internal static void Highlight(System.Windows.Forms.Control c)
        {
            string strError = "";
            if (c != null)
            {
                Point pt = c.Parent == null ? c.PointToScreen(new Point(c.Left, c.Top)) : c.Parent.PointToScreen(new Point(c.Left, c.Top));
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("src point:[{0},{1}] to -[{2}]", c.Left, c.Top, pt));
                Rectangle rect = new Rectangle(pt, c.Size);  //c.Parent == null ? c.Parent.RectangleToScreen(c.Bounds) : c.RectangleToScreen(c.Bounds);
#if gdienable
                windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(
                    new MarsWindowsAPIs.RECT() { Left = rect.Left - 3, Right = rect.Right, Top = rect.Top - 3, Bottom = rect.Bottom },
                    ref strError
                    );
                //if( c.CanFocus || c.CanSelect)
#endif

            }
        }
        public bool GetAllSubChildControlFromParent(System.Windows.Forms.Control cntrl, List<object> lstDesTarget, ref string strError,ref string strAdv, ref string strStack)
        {
            if (cntrl==null)
            {
                strError = "Passing null to a fucntion";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            GetAllChildren(cntrl, lstDesTarget);
            return true;
        }

        public virtual string CaptureValueFromControl(
            object oSourceControl,
            string strParameter,
            string strPegName,
            string strObjName,
            ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            return "unimplemented methond, contact Marquis";
        }

        protected void GetAllChildren(System.Windows.Forms.Control c, List<object> lo)
        {
            
            while((c!=null)&&(c.HasChildren))
            {
                foreach (var cc in c.Controls.OfType<System.Windows.Forms.Control>())
                {
                    if (cc== null) continue;
                    lo.Add(cc);
                    GetAllChildren(cc, lo);
                }
            }
        }

        internal static object GetChildUIElementByTypeName(object uiElement, string typIdx, ref bool isOk, ref string strError,ref string strAdv, ref string strStack )
        {
            Logger.Info("GetChildUIElementByTypeName", string.Format("UIElement Type:[{0}]", uiElement == null ? "" : ReflectorForCSharp.GetObjectBaseType(uiElement.GetType())));
            isOk = false;
            if (uiElement == null)
            {
                strError = "Object property UIElement is null";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return null;
            }
            object oc = ReflectorForCSharp.GetMember(uiElement, "Control");
            Logger.Info("\t", string.Format("Control type:[{0}]", oc == null ? "" : oc.GetType().ToString()));
            object ChildElements = ReflectorForCSharp.GetMember(uiElement, "ChildElements");
            ArrayList lstElements = ChildElements as ArrayList;
            string strTyps = "";
            if ((lstElements == null) || (lstElements.Count <= 0))
            {
                Logger.Info("\t", strError = "no elementUI exists");
                strError = "Object property ElementUI is NULL";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return null;
            }
            foreach (var itm in lstElements)
            {
                if (itm == null) continue;
                string strCurTyps = ReflectorForCSharp.GetObjectBaseType(itm.GetType());
                if (strCurTyps.ToUpper().IndexOf(typIdx.ToUpper()) >= 0)
                {
                    isOk = true;
                    return itm;
                }
                strTyps = string.Format("[{0}]-{1}", strCurTyps, strTyps);
                //object c = ReflectorForCSharp.GetMember(itm, "Control");
                //Logger.Info("\t", string.Format("object type:[{0}], contorl type:[{0}]", itm.GetType(),
                //    c == null ? "null" : ReflectorForCSharp.GetObjectBaseType(c.GetType())));
                
            }
            isOk = false;
            Logger.Error("GetChildUIElementByTypeName", strError = string.Format("cant find type from :[{0}]", strTyps));
            strError = "Object property ULElment is NULL";
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Contact Marquis";
            return null;
        }

        internal static void PrintUIElementsAndItsChildInfo(object uiElement)
        {
            Logger.Info("PrintUIElementsAndItsChildInfo", string.Format("UIElement Type:[{0}]", uiElement==null?"": ReflectorForCSharp.GetObjectBaseType(uiElement.GetType())));
            if (uiElement == null) return;
            object oc = ReflectorForCSharp.GetMember(uiElement,"Control");
            Logger.Info("\t" ,string.Format("Control type:[{0}]", oc==null?"":oc.GetType().ToString()));
            object ChildElements = ReflectorForCSharp.GetMember(uiElement, "ChildElements");
            ArrayList lstElements = ChildElements as ArrayList;
            if ((lstElements == null)||(lstElements.Count<=0))
            {
                Logger.Info("\t", "no elementUI exists");
                return;
            }
            foreach(var itm in lstElements)
            {
                if (itm == null) continue;
                object c = ReflectorForCSharp.GetMember(itm, "Control");
                Logger.Info("\t", string.Format("object type:[{0}], contorl type:[{0}]", itm.GetType(), 
                    c==null?"null":ReflectorForCSharp.GetObjectBaseType(c.GetType())));
                
            }
        }
    }
}
