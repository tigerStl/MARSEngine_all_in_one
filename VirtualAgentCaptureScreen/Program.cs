using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VirtualAgentCaptureScreen
{
    
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        /// <summary>
        /// 用于获取指定屏幕区域的位置bmp文件
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
        {
            Console.WriteLine("begins...");
            try
            {
                var handle = GetConsoleWindow();

                // Hide
                ShowWindow(handle, SW_HIDE);

                // Show
                //ShowWindow(handle, SW_SHOW);

                if (args.Length < 5)
                {
                    return -3;
                }
                string strX = args[0], strY = args[1], strW = args[2], strH = args[3], strFileName = args[4];
                int ix, iy, iw, ih;
                if ((!int.TryParse(strX, out ix))
                    || ((!int.TryParse(strX, out ix)))
                    || ((!int.TryParse(strY, out iy)))
                    || (!int.TryParse(strW, out iw))
                    || (!int.TryParse(strH, out ih)))
                {
                    return -4;
                }

                HighLightForm tmpForm = new HighLightForm();
                tmpForm.Left = ix;
                tmpForm.Top = iy;
                tmpForm.Width = iw;
                tmpForm.Height = ih;
                tmpForm.targetFileName = strFileName;
                //tmpForm.Update();
                //tmpForm.Show();
                System.Threading.Thread.Sleep(1000);

                Application.Run(tmpForm);
                //for (int i = 0; i < 3; i++)
                //{
                //    tmpForm.Show();
                //    System.Threading.Thread.Sleep(1000);
                //    tmpForm.Hide(); 
                //}

                //tmpForm.Close();

                //if (args.Length == 6)
                //{
                //    if (("-F".Equals(args[5],StringComparison.OrdinalIgnoreCase))
                //        || ("-Frame".Equals(args[5], StringComparison.OrdinalIgnoreCase)))
                //    {
                //        HighLightForm tmpForm = new HighLightForm();
                //        tmpForm.Left = ix;
                //        tmpForm.Top = iy;
                //        tmpForm.Width = iw;
                //        tmpForm.Height = ih;
                //        tmpForm.targetFileName = strFileName;
                //        //tmpForm.Update();
                //        //tmpForm.Show();
                //        System.Threading.Thread.Sleep(1000);

                //        Application.Run(tmpForm);
                //        //for (int i = 0; i < 3; i++)
                //        //{
                //        //    tmpForm.Show();
                //        //    System.Threading.Thread.Sleep(1000);
                //        //    tmpForm.Hide(); 
                //        //}

                //        tmpForm.Close();
                //    }
                //}
                //Application.Run(tmpForm);


                ////Bitmap bmp = new Bitmap(Screen.FromPoint(new Point(ix, iy)).Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                //Bitmap bmp = new Bitmap(iw, ih);
                //Graphics graphics = Graphics.FromImage(bmp as Image);
                //graphics.CopyFromScreen(ix, iy, 0, 0, new Size(iw, ih));

                //bmp.Save(strFileName,ImageFormat.Bmp);
            
                //Console.WriteLine($"saved to {strFileName}");

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return -5;
            }
        }
    }
}
