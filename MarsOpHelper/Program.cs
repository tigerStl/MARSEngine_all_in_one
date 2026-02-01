using MarsOpHelper.MarsOpHelper.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarsOpHelper
{
    class Program
    {
        /// <summary>
        /// 这个是作为体外循环进行点击和处理的程序，
        /// 其目的是针对指定的窗口进行out-process的鼠标，键盘操作
        /// 参数模式：
        /// -Type Mouse|M|Keyboard|K -X (0-9){1,} -Y (0-9){1,}
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        static int Main(string[] args)
        {

            //MessageBox.Show(string.Join(" ",args));
            var handle = Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetConsoleWindow();
            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.ShowWindow(handle, 1);

            // Hide
            //ShowWindow(handle, SW_HIDE);

            // Show
            //ShowWindow(handle, SW_SHOW);

            Console.SetWindowSize(10, 10);
            Console.SetWindowPosition(100, 100);
            

            Mars_OpParaStatus opStatus = Mars_OpParaStatus.en_None;
            string strError = "";
            ParameterMgr currentPara = ParameterMgr.GetInstance(args,ref opStatus, ref strError);
            
            switch (opStatus)
            {
                case Mars_OpParaStatus.en_Key:
                    System.Windows.Forms.SendKeys.SendWait(currentPara.dataForKey);
                    break;

                case Mars_OpParaStatus.en_mouse:
                    switch (currentPara.subMouseType)
                    {
                        case Mars_mouseSubType.en_move:
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.MoveMouse(currentPara.X, currentPara.Y);
                            break;
                        case Mars_mouseSubType.en_rightClick:
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(currentPara.X, currentPara.Y);
                            break;
                        case Mars_mouseSubType.en_LeftClick:
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(currentPara.X, currentPara.Y);
                            break;
                        case Mars_mouseSubType.en_LeftDblClick:
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(currentPara.X, currentPara.Y);
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(currentPara.X, currentPara.Y);
                            break;
                        default:
                            Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(currentPara.X, currentPara.Y);
                            break;
                    }
                    break;
            }

            System.Environment.ExitCode = (int)opStatus;
            System.Environment.Exit((int)opStatus);
            return (int)opStatus;           

        }
    }
}
