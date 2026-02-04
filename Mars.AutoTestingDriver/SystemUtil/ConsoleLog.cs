using System;

namespace Mars.AutoTestingDriver.SystemUtil
{
    public sealed class ConsoleLog
    {
        private static bool isAllowIntimeLog = true;
        public static void IntimeLog(params string[] arrLog)
        {
            IntimeLog(1, arrLog);
        }
        private static void IntimeLog(int iTabCount, params string[] arrLog)
        {
            if (!isAllowIntimeLog) return;
            if (arrLog == null) return;

            if (arrLog.Length > 1)
            {
                string[] arrDes = new string[arrLog.Length - 1];
                Array.Copy(arrLog, 1, arrDes, 0, arrLog.Length - 1);
                if (iTabCount > 0)
                    Console.WriteLine(string.Format("{0}{1}", new string('\t', iTabCount), arrLog[0]), arrDes);
                else
                    Console.WriteLine(arrLog[0], arrDes);
            }
            else
            {
                if (iTabCount > 0)
                    Console.WriteLine(new string('\t', iTabCount) + string.Concat(arrLog));
                else
                    Console.WriteLine(string.Concat(arrLog));
            }
        }

        /// <summary>
        /// 用写keyword开始的
        /// </summary>
        /// <param name="arrLog"></param>
        public static void IntimeLog_KeywordTitle(params string[] arrLog)
        {
            IntimeLog(1, arrLog);
        }

        public static void IntimeLog_keywordSub(params string[] arrLog)
        {
            IntimeLog(2, arrLog);
        }
    }
}
