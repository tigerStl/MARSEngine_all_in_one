using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.keywordOperation
{
    internal abstract class CloseWindow
    {
        public abstract bool CloseWindowForControl(object oSourceControl, ref bool isOk, ref string strError,
            ref string strAdv, ref string strStack,
            string strAddtionData = null);
    }

    internal class CloseWindowForStandardForm : CloseWindow
    {

        public override bool CloseWindowForControl(object oSourceControl, ref bool isOk, ref string strError,
            ref string strAdv, ref string strStack,
            string strAddtionData = null)
        {
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("parameter for close:[{0}]", strAddtionData));
            System.Windows.Forms.Form f = oSourceControl as System.Windows.Forms.Form;
            System.Drawing.Rectangle rect;
            int xDelta = 8, yDelta = 8;
            if (f.ControlBox)
            {

                simpleLog.MarsLoggerSimple.Info("CloseWindowForControl", "has controlBox");
                MarsWindowsAPIs.TITLEBARINFOEX cntrlBx = MarsWindowsAPIsExtend.GetTitleBarInfoEx(f.Handle);
                try
                {
                    rect = cntrlBx.rgrect[5];
                    if ((!string.IsNullOrEmpty(strAddtionData)) && (MarsWindowsAPIsExtend.RegularTest("^bypos:", strAddtionData)))
                    {
                        string strPos = strAddtionData.Substring("bypos:".Length);
                        string[] arrxy = strPos.Split(new string[] { ",", ":" }, StringSplitOptions.RemoveEmptyEntries);
                        if (arrxy.Length != 2)
                        {
                            strError = string.Format("bypos parameter on close window format should be bypos:x,y, but it is:[{0}]", strAddtionData);
                            StackFrame stck = (new StackFrame());
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "";
                            return false;
                        }
                        if ((!int.TryParse(arrxy[0], out xDelta)) || (!(int.TryParse(arrxy[1], out yDelta))))
                        {
                            strError = string.Format("bypos parameter on close window format should be bypos:x,y ,x,y all numbers, but it is:[{0}]", strAddtionData);
                            StackFrame stck = (new StackFrame());
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "";
                            return false;
                        }
                        //MarsWindowsAPIsExtend.FlashWindowByHandle(f.Handle);
                        //XorDrawing.DrawXorRectangleOnDeskTop(new MarsWindowsAPIs.RECT() {
                        //    Left = f.Bounds.Left, 
                        //    Right= f.Bounds.Right,
                        //    Top  = f.Bounds.Top,
                        //    Bottom=f.Bounds.Bottom
                        //},
                        //ref strError);
                        //MarsWindowsAPIsExtend.DrawARectangleOnDesk(f.Bounds) ;
                        rect = f.Bounds;
                        xDelta += rect.Width;
                        yDelta += 0;
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("Get rect:[{0}]", rect));
                        MarsWindowsAPIs.SetCursorPos(rect.Location.X, rect.Location.Y);
                        Thread.Sleep(50);
                        //MarsWindowsAPIs.SetCursorPos(rect.Location.X + rect.Width ,rect.Location.Y + rect.Height )  ;
                        Thread.Sleep(50);
                        MarsWindowsAPIsExtend.LeftMouseClick(rect.Location.X + xDelta, rect.Location.Y + yDelta);
                        MarsWindowsAPIs.TITLEBARINFOEX tbi = new MarsWindowsAPIs.TITLEBARINFOEX();
                        tbi.cbSize = Marshal.SizeOf(typeof(MarsWindowsAPIs.TITLEBARINFOEX));
                        //MarsWindowsAPIs.SendMessage(f.Handle, (int)WM.CLOSE, IntPtr.Zero, ref tbi)                  ;
                        Thread.Sleep(50);
                        //MarsWindowsAPIsExtend.LeftMouseClick(rect.Location.X + 8, rect.Location.Y + 8);
                        return isOk = true;
                    }
                    if ((!string.IsNullOrEmpty(strAddtionData)) && ((MarsWindowsAPIsExtend.RegularTest("byApi", strAddtionData))
                        || (string.Compare("byApi", strAddtionData, true) == 0)))
                    {
                        f.Close();
                        return isOk = true;
                    }

                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("Get rect:[{0}]", rect));
                    MarsWindowsAPIs.SetCursorPos(rect.Location.X, rect.Location.Y);
                    Thread.Sleep(50);
                    MarsWindowsAPIs.SetCursorPos(rect.Location.X + rect.Width / 2, rect.Location.Y + rect.Height / 2);
                    Thread.Sleep(50);
                    MarsWindowsAPIsExtend.LeftMouseClick(rect.Location.X + rect.Width - 12, rect.Location.Y + 8);
                    Thread.Sleep(50);
                    //MarsWindowsAPIsExtend.LeftMouseClick(rect.Location.X + 8, rect.Location.Y + 8);
                    return isOk = true;

                }
                catch (Exception e)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Exception when call close window by title bar close button position:\r\n\t{0}", e.Message));
                    StackFrame stck = (new StackFrame());
                    strStack = e.StackTrace;
                    strAdv = "";
                    return isOk = false;
                }

            }
            else
            {
                if ((!string.IsNullOrEmpty(strAddtionData)) && (MarsWindowsAPIsExtend.RegularTest("^bypos:", strAddtionData)))
                {
                    string strPos = strAddtionData.Substring("bypos:".Length);
                    string[] arrxy = strPos.Split(new string[] { ",", ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (arrxy.Length != 2)
                    {
                        strError = string.Format("bypos parameter on close window format should be bypos:x,y, but it is:[{0}]", strAddtionData);
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "";
                        return false;
                    }
                    if ((!int.TryParse(arrxy[0], out xDelta)) || (!(int.TryParse(arrxy[1], out yDelta))))
                    {
                        strError = string.Format("bypos parameter on close window format should be bypos:x,y ,x,y all numbers, but it is:[{0}]", strAddtionData);
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "";
                        return false;
                    }
                    //MarsWindowsAPIsExtend.FlashWindowByHandle(f.Handle);
                    //XorDrawing.DrawXorRectangleOnDeskTop(new MarsWindowsAPIs.RECT() {
                    //    Left = f.Bounds.Left, 
                    //    Right= f.Bounds.Right,
                    //    Top  = f.Bounds.Top,
                    //    Bottom=f.Bounds.Bottom
                    //},
                    //ref strError);
                    //MarsWindowsAPIsExtend.DrawARectangleOnDesk(f.Bounds) ;
                    rect = f.DesktopBounds;
                    xDelta += f.Width;
                    yDelta += 0;

                    #region to make sure the control is ready
                    Control cntrlList = ((Control)oSourceControl);
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

                    ((Control)oSourceControl).Update();
                    System.Threading.Thread.Sleep(10);
                    #endregion


                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("Get rect:[{0}]", rect));
                    MarsWindowsAPIs.SetCursorPos(f.Left, f.Top);
                    Thread.Sleep(50);
                    MarsWindowsAPIs.SetCursorPos(f.Left + f.Width, f.Top + f.Height);
                    Thread.Sleep(50);
                    MarsWindowsAPIsExtend.LeftMouseClick(f.Left + xDelta, f.Top + yDelta);

                    Thread.Sleep(50);
                    //MarsWindowsAPIsExtend.LeftMouseClick(rect.Location.X + 8, rect.Location.Y + 8);
                    return isOk = true;
                }
                int x = f.Left + f.Width - 10;
                int y = f.Top + 10;
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("try to click here:[x-{0} y-{1}]", x, y));
                MarsWindowsAPIs.SetCursorPos(x, y);
                Thread.Sleep(50);
                MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                Thread.Sleep(50);

                return isOk = true;
            }
            /*
            try
            {                
                //no chose but to close 
                f.Close();
                return isOk = true;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Exception when call close window by function close :\r\n\t{0}", e.Message));
                return isOk = false;
            }
            */

        }
    }
}
