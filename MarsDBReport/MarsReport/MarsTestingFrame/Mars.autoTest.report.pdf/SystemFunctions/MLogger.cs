using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Route2NSEx.src.Marquis.systemUtil
{
    internal class MLogger
    {

        private const string LOGGER_NAME = "Pdf";

        private string mstrCurrentClassName;
        private log4net.ILog mobjLog = log4net.LogManager.GetLogger(LOGGER_NAME);
        private static bool IsLoad = false;
        public static MLogger GetLogger(string className)
        {
            if (!IsLoad)
            {
                //  log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("Log4Net.config"));
                IsLoad = true;
                log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(typeof(MLogger).Assembly.Location + ".config"));
            }
            MLogger objResult = new MLogger() { mstrCurrentClassName = className };
            return objResult;
        }

        public static MLogger GetLogger(Type oneType)
        {
            if (!IsLoad)
            {
                IsLoad = true;
                log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(typeof(MLogger).Assembly.Location + ".config"));
            }
            MLogger objResult = new MLogger()
            { //mstrCurrentClassName = oneType.ToString() 
                mstrCurrentClassName = oneType.ToString().Substring(oneType.ToString().LastIndexOf("."))
            };

            return objResult;
        }

        public void logBegin(string strMethodName)
        {
#if !_tigerHintLog
            //mobjLog.Info(string.Format("[INFO] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} begins...", DateTime.Now, mstrCurrentClassName, strMethodName));
            mobjLog.Info(string.Format("[INFO] {0}.{1} begins...", mstrCurrentClassName, strMethodName));
#endif
        }

        public void logEnd(string strMethodName)
        {
            mobjLog.Info(string.Format("[INFO] {1}.{2} end.", DateTime.Now, mstrCurrentClassName, strMethodName));
        }

        public void Info(string strMethodName, string strInfo)
        {
            mobjLog.Info(string.Format("[INFO] {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strInfo));
            Console.WriteLine("[INFO] {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strInfo);
        }

        public void Warnning(string strMethodName, string strWarnning)
        {
            mobjLog.Warn(string.Format("WARNNING---[{0}] {1}", strMethodName, strWarnning));
        }

        public void Error(string strMethodName, string strErrorMsg)
        {
            mobjLog.Error(string.Format("[ERROR] {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strErrorMsg));
        }
        public void Error(string strMethodName, string strErrorMsg, Exception e)
        {
            mobjLog.Error(string.Format("[ERROR] {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strErrorMsg), e);
        }

    }

}
