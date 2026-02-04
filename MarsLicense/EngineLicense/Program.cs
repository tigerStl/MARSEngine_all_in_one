using System;
using System.Windows.Forms;
using MarsLicenseManager.CommandLineTools;

namespace MarsLicenseManager
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // 检查是否有命令行参数
            if (args.Length > 0)
            {
                // 处理命令行参数
                DllEncryptionCommandLine.ProcessCommandLine(args);
                return;
            }

            // 没有命令行参数时，启动GUI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}