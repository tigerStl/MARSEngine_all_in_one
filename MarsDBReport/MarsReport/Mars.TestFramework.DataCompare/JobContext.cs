using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.TestFramework.DataCompare
{
   
    public class JobContext
    {

        private static readonly MLogger log = MLogger.GetLogger(typeof(JobContext));

        public static Stopwatch stopWatch;
        // JobContext.DisplayStopwatch(tag);
        public static void DisplayStopwatch(string tag)
        {
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                    ts.Hours, ts.Minutes, ts.Seconds,
                                    ts.Milliseconds / 10);
            string logStr = "RunTime " + tag + " =>> " + elapsedTime;
            Console.WriteLine(logStr);
            log.Info("DisplayStopwatch", logStr);
        }

        public static Stopwatch CreateStopwatch()
        {
            stopWatch = new Stopwatch();
            stopWatch.Start();
            return stopWatch;
        }
    }
}
