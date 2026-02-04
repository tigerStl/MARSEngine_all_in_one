using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Route2NSEx.src.Marquis.systemUtil
{
    public class MLogger
    {

        private const string LOGGER_NAME = "tiger_test";

        private string mstrCurrentClassName;
        private log4net.ILog mobjLog = log4net.LogManager.GetLogger(LOGGER_NAME);

        public static MLogger GetLogger(string className)
        {
            MLogger objResult = new MLogger() { mstrCurrentClassName = className };
            return objResult;
        }

        public static MLogger GetLogger(Type oneType)
        {
            MLogger objResult = new MLogger() { mstrCurrentClassName = oneType.ToString() };
            return objResult;
        }

        public void logBegin(string strMethodName)
        {
            mobjLog.Info(string.Format("[INFO] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} begins...", DateTime.Now, mstrCurrentClassName, strMethodName));
        }

        public void logEnd(string strMethodName)
        {
            mobjLog.Info(string.Format("[INFO] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} end.", DateTime.Now, mstrCurrentClassName, strMethodName));
        }

        public void Info(string strMethodName, string strInfo)
        {
            mobjLog.Info(string.Format("[INFO] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strInfo));
        }

        public void Error(string strMethodName, string strErrorMsg)
        {
            mobjLog.Error(string.Format("[ERROR] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strErrorMsg));
        }
        public void Error(string strMethodName, string strErrorMsg, Exception e)
        {
            mobjLog.Error(string.Format("[ERROR] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strErrorMsg), e);
        }
    }

}
