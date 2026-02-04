using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mars.TestFramework.DataCompare
{
    public class MLogger
    {

        public static string LOGGER_NAME = "TesetService";

        private string mstrCurrentClassName;
        private log4net.ILog mobjLog = log4net.LogManager.GetLogger(LOGGER_NAME);
        private static bool IsLoad = false;

        private static void setConfigFile()
        {
            var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository);

            //log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(Assembly.GetExecutingAssembly().Location + ".config"));
        }
        public static MLogger GetLogger(string className)
        {
            if (!IsLoad)
            {
                setConfigFile();
                //  log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("Log4Net.config"));
                IsLoad = true;
            }
            MLogger objResult = new MLogger() { mstrCurrentClassName = className };

            return objResult;
        }

        //private bool swithLogToSpecialAppender(string name)
        //{
        //    if (string.IsNullOrEmpty(name)) return true;
        //    if (LOGGER_NAME.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
            
        //    log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(Assembly.GetExecutingAssembly().Location + ".config"));
        //    var tmpLog = LogManager.GetRepository()
        //                                   .GetAppenders()
        //                                   .FirstOrDefault(a => a.Name.Equals(name,StringComparison.OrdinalIgnoreCase));
        //    if (tmpLog != null)
        //    {
        //        mobjLog = tmpLog;
        //    }
        //}

        public static MLogger GetLogger(Type oneType)
        {
            if (!IsLoad)
            {
                IsLoad = true;
                setConfigFile();
                //log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(typeof(MLogger).Assembly.Location + ".config"));
                //string configFileName = @"C:\automationTest\Automation Workbooks\dlls\MarsTestFrame.dll.config";
                //string configFileName = @"C:\automationTest\Automation Workbooks\dlls\Mars.TestFramework.DataCompare.dll.config";

                //log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(@"C:\automationTest\Automation Workbooks\dlls\Mars.exe" + ".config"));
                //log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(configFileName));



                var rootAppender = ((Hierarchy)LogManager.GetRepository())
                                         .Root.Appenders.OfType<FileAppender>()
                                         .FirstOrDefault();
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

        public void logBegin(string strMethodName,string strInfo)
        {
#if !_tigerHintLog
            //mobjLog.Info(string.Format("[INFO] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} begins...", DateTime.Now, mstrCurrentClassName, strMethodName));
            mobjLog.Info(string.Format("[INFO] {0}.{1} begins...{2}", mstrCurrentClassName, strMethodName, strInfo));
#endif
        }

        public void logEnd(string strMethodName)
        {
            mobjLog.Info(string.Format("[INFO] {1}.{2} end.", DateTime.Now, mstrCurrentClassName, strMethodName));
        }

        public void Info(string strMethodName, string strInfo)
        {
            mobjLog.Info(string.Format("[INFO] {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strInfo));
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
