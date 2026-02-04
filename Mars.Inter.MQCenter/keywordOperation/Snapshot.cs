using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using User32 = Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs;
using Gdi32 = Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs;
using System.Drawing;
using Mars.message.windowsWrapper.SystemUtil;
using System.Windows.Forms;
using System.Diagnostics;
using Mars.message.AutoTestingDriver.ErrorMessage;

namespace Mars.message.Inter.MQCenter.keywordOperation
{
    internal abstract class SnapshotBase
    {
        public abstract string SnapshotScreen(object oSourceControl, string strParameter,
             string strPegName, string strObjName,
            ref bool isOk, ref string strError,ref string strAdv, ref string strStack);


        public static bool GetControlRectByHandle(System.Windows.Forms.Control c, ref Rectangle rect)
        {
            User32.RECT rectHandle;
            if (c != null)
            {
                if (!windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowRect(c.Handle, out rectHandle))
                {
                    Point pt = c.Parent == null ? c.Bounds.Location : c.Parent.PointToScreen(new Point(c.Left, c.Top));
                    rect = new Rectangle(pt, c.Size);  //c.Parent == null ? c.Parent.RectangleToScreen(c.Bounds) : c.RectangleToScreen(c.Bounds);
                    return true;
                }
                rect = new Rectangle(rectHandle.Left - 1,    
                       rectHandle.Top - 1,
                       rectHandle.Right - rectHandle.Left + 1,
                       rectHandle.Bottom - rectHandle.Top + 1
                   );
                return true;
            }
            return false;
        }
        public static bool Highlight(System.Windows.Forms.Control c, string strParameter,
            ref string strError,
            ref string strAdv, 
            ref string strStack,
            ref Rectangle rect)
        {
            User32.RECT rectHandle;
            //string strFileName = "";
            if (c != null)
            {
                
                if (windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowRect(c.Handle, out rectHandle))
                {
#if gdienable
                    windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(rectHandle,
                        ref strError
                        );
                    //if( c.CanFocus || c.CanSelect)
#endif
                    rect = new Rectangle(rectHandle.Left - 1,
                        rectHandle.Top - 1,
                        rectHandle.Right - rectHandle.Left + 1,
                        rectHandle.Bottom - rectHandle.Top + 1
                    );
                    return true;
                    //strFileName = CaptureRegion(c.Handle, rectHandle.Left - 1, rectHandle.Top - 1, rectHandle.Right - rectHandle.Left + 1,
                    //    rectHandle.Bottom - rectHandle.Top + 1, ref isOk, ref strError);
                }
                else
                {
                    //Point pt = c.Parent == null ? c.PointToScreen(new Point(c.Left, c.Top)) : c.Parent.PointToScreen(new Point(c.Left, c.Top));
                    Point pt = c.Parent == null ? c.Bounds.Location : c.Parent.PointToScreen(new Point(c.Left, c.Top));
                    rect = new Rectangle(pt, c.Size);  //c.Parent == null ? c.Parent.RectangleToScreen(c.Bounds) : c.RectangleToScreen(c.Bounds);
#if gdienable
                    windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(
                        new MarsWindowsAPIs.RECT()
                        {
                            Left = rect.Left - 3,
                            Right = rect.Right,
                            Top = rect.Top - 3,
                            Bottom = rect.Bottom
                        },
                        ref strError
                        );
                    //if( c.CanFocus || c.CanSelect)
#endif                    
                    //strFileName = CaptureRegion(c.Handle, rect.Left, rect.Top, rect.Width, rect.Height, ref isOk, ref strError);
                    return true;
                }

            }
            strError = "Passed Null to a function";
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Contact Marquis";
            return false;
        }
    }
    internal class Snapshot: SnapshotBase
    {
        public override string SnapshotScreen(object oSourceControl, string strParameter, 
            string strPegName, string strObjName, 
            ref bool isOk, ref string strError,ref string strAdv,ref string strStack)
        {
            if (!(oSourceControl is System.Windows.Forms.Control))
            {
                strError = string.Format("object is not a control, which is :[{0}]", oSourceControl.GetType().ToString());
                isOk = false;
                return null;
            }
            System.Windows.Forms.Control c = oSourceControl as System.Windows.Forms.Control;
            
            Rectangle rect=new Rectangle();
            string strFileName = "";
            if (c!=null)
            {
                //if (Highlight(c, strParameter,ref strError, ref strAdv, ref strStack, ref rect))
                if (GetControlRectByHandle(c, ref rect))
                {
                    strFileName = CaptureRegion(c.Handle, rect.Left - 1, rect.Top - 1, rect.Right - rect.Left + 1,
                        rect.Bottom - rect.Top + 1, strPegName, strObjName, ref isOk, ref strError,ref strAdv,ref strStack);
                    isOk = true;
                    return strFileName;
                }
                else
                {
                    isOk = true;
                    return null;
                }
                /*
                                User32.RECT rectHandle;
                                if (windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowRect(c.Handle, out rectHandle))
                                {
                #if gdienable
                                    windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop( rectHandle,                        
                                        ref strError
                                        );
                                    //if( c.CanFocus || c.CanSelect)
                #endif
                                    strFileName = CaptureRegion(c.Handle, rectHandle.Left-1, rectHandle.Top-1, rectHandle.Right-rectHandle.Left+1, 
                                        rectHandle.Bottom-rectHandle.Top+1, ref isOk, ref strError);
                                }
                                else
                                {
                                    //Point pt = c.Parent == null ? c.PointToScreen(new Point(c.Left, c.Top)) : c.Parent.PointToScreen(new Point(c.Left, c.Top));
                                    Point pt = c.Parent == null ? c.Bounds.Location : c.Parent.PointToScreen(new Point(c.Left, c.Top));
                                    rect = new Rectangle(pt, c.Size);  //c.Parent == null ? c.Parent.RectangleToScreen(c.Bounds) : c.RectangleToScreen(c.Bounds);
                #if gdienable
                                    windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(
                                        new MarsWindowsAPIs.RECT() { Left = rect.Left - 3, Right = rect.Right, Top = rect.Top - 3, Bottom = rect.Bottom },
                                        ref strError
                                        );
                                    //if( c.CanFocus || c.CanSelect)
                #endif
                                    strFileName = CaptureRegion(c.Handle, rect.Left, rect.Top, rect.Width, rect.Height, ref isOk, ref strError);

                                }
                                if (isOk)
                                {
                                    return strFileName;
                                }
                */
            }

            return null;
        }

        public string SnapShotScreenAndDrowHotRectangle(IntPtr hwnd, List<Control> lstCntrl, 
            string strPegName, string strObjName,
            ref bool isOk, ref string strError,
            ref string strAdv, ref string strStack)
        {
            IntPtr sourceDC = IntPtr.Zero;
            IntPtr targetDC = IntPtr.Zero;
            IntPtr compatibleBitmapHandle = IntPtr.Zero;
            try
            {
                User32.RECT rct = new User32.RECT();
                User32.GetWindowRect(hwnd, out rct);

                // gets the main desktop and all open windows
                sourceDC = User32.GetDC(User32.GetDesktopWindow());
                //sourceDC = User32.GetDC(hWnd);
                targetDC = Gdi32.CreateCompatibleDC(sourceDC);
                int width = rct.Right - rct.Left;
                int height = rct.Bottom - rct.Top;
                // create a bitmap compatible with our target DC
                compatibleBitmapHandle = Gdi32.CreateCompatibleBitmap(sourceDC, width, height);

                // gets the bitmap into the target device context
                Gdi32.SelectObject(targetDC, compatibleBitmapHandle);

                // copy from source to destination
                Gdi32.BitBlt(targetDC, 0, 0, width, height, sourceDC, 0, 0, Gdi32.TernaryRasterOperations.SRCCOPY);

                Bitmap image = new Bitmap(Image.FromHbitmap(compatibleBitmapHandle), new Size(width, height));
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    System.Drawing.Pen tmpRedPen = new System.Drawing.Pen(Color.Red, 1);
                    foreach (var oneCntrl in lstCntrl) {
                        if (oneCntrl == null) continue;
                        Rectangle scrn = oneCntrl.RectangleToScreen(new Rectangle(-2, -2, oneCntrl.Width + 2, oneCntrl.Height + 2));
                        graphics.DrawRectangle(System.Drawing.Pens.Red, scrn);
                    }
                }

                string strFile = GetTmpSnapshotFileName();
                image.Save(strFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                isOk = true;
                return strFile;
            }
            catch (Exception ex)
            {
                strError = $"Error while searching for a control  [{strPegName}][{strObjName}]";//string.Format("Exception:[{0}]  stacktrace:\r\n[{1}]", ex.Message, ex.StackTrace);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Unidentified error. If this continues, contact Marquis";
                isOk = false;
                return null;
            }
            finally
            {
                Gdi32.DeleteObject(compatibleBitmapHandle);

                User32.ReleaseDC(IntPtr.Zero, sourceDC);
                User32.ReleaseDC(IntPtr.Zero, targetDC);
            }
        }

        public string CaptureRegion(
            IntPtr hWnd, int x, int y, int width, int height,        
            string strPegName, string strObjName,
            ref bool isOk,
            ref string strError,
            ref string strAdv, 
            ref string strStack
            )
        {
            IntPtr sourceDC = IntPtr.Zero;
            IntPtr targetDC = IntPtr.Zero;
            IntPtr compatibleBitmapHandle = IntPtr.Zero;           

            try
            {

                // gets the main desktop and all open windows
                sourceDC = User32.GetDC(User32.GetDesktopWindow());
                //sourceDC = User32.GetDC(hWnd);
                targetDC = Gdi32.CreateCompatibleDC(sourceDC);

                // create a bitmap compatible with our target DC
                compatibleBitmapHandle = Gdi32.CreateCompatibleBitmap(sourceDC, width, height);

                // gets the bitmap into the target device context
                Gdi32.SelectObject(targetDC, compatibleBitmapHandle);

                // copy from source to destination
                Gdi32.BitBlt(targetDC, 0, 0, width, height, sourceDC, x, y, Gdi32.TernaryRasterOperations.SRCCOPY);

                Bitmap image = new Bitmap(Image.FromHbitmap(compatibleBitmapHandle),new Size(width,height));
                string strFile = GetTmpSnapshotFileName();
                image.Save(strFile, System.Drawing.Imaging.ImageFormat.Jpeg);
                isOk = true;
                return strFile;
            }
            catch (Exception ex)
            {
                strError = $"Error while capturing region for object [{strObjName}]"; //string.Format("Exception:[{0}]  stacktrace:\r\n[{1}]",ex.Message,ex.StackTrace);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Unidentified error. If this continues, contact Marquis";
                isOk = false;
                return null;
            }
            finally
            {
                Gdi32.DeleteObject(compatibleBitmapHandle);

                User32.ReleaseDC(IntPtr.Zero, sourceDC);
                User32.ReleaseDC(IntPtr.Zero, targetDC);
            }
            
        }
        private string GetTmpSnapshotFileName()
        {
            string strPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(Snapshot).Assembly.Location), "..\\snapshot\\");
            if (!System.IO.Directory.Exists(strPath))
            {
                System.IO.Directory.CreateDirectory(strPath);
            }
            string d = DateTime.Now.ToString("yyyyMMdd HHmmss fff");
            d = string.Format("MarsSnapShot{0}.jpg", d);
            return strPath = System.IO.Path.Combine(strPath, d);
        }
    }
}
