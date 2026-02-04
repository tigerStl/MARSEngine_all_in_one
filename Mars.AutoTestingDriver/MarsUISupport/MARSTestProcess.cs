using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.MarsUISupport
{
    /// <summary>
    /// MARS Test Process - 管理当前测试进程信息
    /// </summary>
    public static class MARSTestProcess
    {
        /// <summary>
        /// 当前测试进程ID
        /// </summary>
        public static int CurrentTestProcessId { get; set; } = 0;

        /// <summary>
        /// 当前测试进程名称
        /// </summary>
        public static string CurrentTestProcessName { get; set; } = "";

        /// <summary>
        /// 设置当前测试进程信息
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <param name="processName">进程名称</param>
        public static void SetCurrentTestProcess(int processId, string processName = "")
        {
            CurrentTestProcessId = processId;
            CurrentTestProcessName = processName;
        }

        /// <summary>
        /// 清除当前测试进程信息
        /// </summary>
        public static void Clear()
        {
            CurrentTestProcessId = 0;
            CurrentTestProcessName = "";
        }
    }
}
