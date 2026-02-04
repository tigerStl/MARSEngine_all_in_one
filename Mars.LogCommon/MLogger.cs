using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Route2NSEx.src.Marquis.systemUtil
{
    public class MLogger
    {

        public static string LOGGER_NAME = "TestService";

        private string mstrCurrentClassName;

        private log4net.ILog _mobjLog = null;
        private log4net.ILog mobjLog
        {

            get
            {
                if (_mobjLog == null)
                {
                    _mobjLog = log4net.LogManager.GetLogger(LOGGER_NAME);

                }
                return _mobjLog;
            }
        }
        private static bool IsLoad = false;
        public static MLogger GetLogger(string className)
        {

            if (!IsLoad)
            {
                //  log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("Log4Net.config"));
                IsLoad = true;
            }
            MLogger objResult = new MLogger() { mstrCurrentClassName = className };
            return objResult;
        }

        private static string logFileConfigName = null;
        public static string LogFileCofigName
        {
            get
            {
                //MessageBox.Show($"LogFileCofigName.get:{logFileConfigName}");
                if (logFileConfigName == null)
                {
                    try
                    {
                        logFileConfigName = System.Reflection.Assembly.GetEntryAssembly().Location + ".config";
                    }
                    catch
                    {
                        logFileConfigName = System.Reflection.Assembly.GetExecutingAssembly().Location + ".config";
                    }
                }
                //MessageBox.Show($"LogFileCofigName get {logFileConfigName}");
                return logFileConfigName;
            }
            set
            {
                //MessageBox.Show($"LogFileCofigName.set value:{value}");
                if (string.Compare(logFileConfigName ?? "N/A", value, true) == 0) return;
                logFileConfigName = value;
                log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(logFileConfigName));
            }
        }

        public static string LogFileName
        {
            get; set;
        }

        public static MLogger GetLogger(Type oneType)
        {
            if (!IsLoad)
            {
                //MessageBox.Show($"file:{LogFileCofigName}");
                if (!File.Exists(LogFileCofigName)) return null;

                IsLoad = true;
                log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(LogFileCofigName));
                if (LogFileName != null)
                {
                    log4net.GlobalContext.Properties["LogName"] = LogFileName;
                }
            }
            MLogger objResult = new MLogger()
            { //mstrCurrentClassName = oneType.ToString() 
                mstrCurrentClassName = oneType.ToString().Substring(oneType.ToString().LastIndexOf("."))
            };

            return objResult;
        }
        public void logBegin(string strMethodName, string paraInfo)
        {
#if !_tigerHintLog
            //mobjLog.Info(string.Format("[INFO] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} begins...", DateTime.Now, mstrCurrentClassName, strMethodName));
#endif
            mobjLog.Info(string.Format("[INFO] {0}.{1} begins...Paras:{2}", mstrCurrentClassName, strMethodName, paraInfo));

        }
        public void logBegin(string strMethodName)
        {
#if !_tigerHintLog
            //mobjLog.Info(string.Format("[INFO] {0:MM/dd/yyyy H:mm:ss zzz} {1}.{2} begins...", DateTime.Now, mstrCurrentClassName, strMethodName));
#endif
            mobjLog.Info(string.Format("[BEGIN] {0}.{1} begins...", mstrCurrentClassName, strMethodName));

        }

        public void logEnd(string strMethodName)
        {
            mobjLog.Info(string.Format("[END] {1}.{2} end.", DateTime.Now, mstrCurrentClassName, strMethodName));
        }

        public void logEnd(string strMethodName, string strMsg)
        {
            mobjLog.Info(string.Format("[END] {1}.{2} end. {3}", DateTime.Now, mstrCurrentClassName, strMethodName,
                string.IsNullOrEmpty(strMsg) ? "" : strMsg));
        }
#if RUNNING_ON_4
        public void Info(string strMethodName, string strInfo)
        {
            mobjLog.Info(string.Format("[INFO] {0}.{1} {2}", mstrCurrentClassName, strMethodName, strInfo));
        }
#else
        public void Info(string strMethodName, string strInfo, [CallerLineNumber] int iLn = 0, [CallerMemberName] string strName = null)
        {
            mobjLog.Info(string.Format("[INFO] LN:[{4}],MemberName:[{5}]- {1}.{2} {3}", DateTime.Now, mstrCurrentClassName, strMethodName, strInfo, iLn, strName));
        }
        public void Info(string strMethodName, params string[] arrFormatAndData)
        {
            if ((arrFormatAndData == null) || (arrFormatAndData.Length == 0))
            {
                mobjLog.Info(string.Format("[INFO] MemberName:[{0}.{1}]", mstrCurrentClassName, strMethodName));
            }
            else
            {
                string[] arrDes = null;
                if (arrFormatAndData.Length > 1)
                {
                    arrDes = new string[arrFormatAndData.Length - 1];
                    Array.Copy(arrFormatAndData, 1, arrDes, 0, arrDes.Length);
                    mobjLog.Info(string.Format("[INFO] MemberName:[{0}.{1}] [{2}]", mstrCurrentClassName, strMethodName, string.Format(arrFormatAndData[0], arrDes)));

                }
                else
                {
                    mobjLog.Info(string.Format("[INFO] MemberName:[{0}.{1}] [{2}]", mstrCurrentClassName, strMethodName, arrFormatAndData[0]));
                }

            }
        }
#endif
        public void Warnning(string strMethodName, string strWarnning)
        {
            mobjLog.Warn(string.Format("WARNNING---[{0}] {1}", strMethodName, strWarnning));
        }

#if RUNNING_ON_4
        public void Error(string strMethodName, string strErrorMsg)
        {
            mobjLog.Error(string.Format("[ERROR] {1}.{2} {3}", DateTime.Now,
                mstrCurrentClassName, strMethodName, strErrorMsg));
        }

        public void Error(string strMethodName, string strErrorMsg, Exception e)
        {
            mobjLog.Error(string.Format("[ERROR] {1}.{2} {3}", DateTime.Now,
                mstrCurrentClassName, strMethodName, strErrorMsg), e);
            Exception eTmp = e.InnerException;
            while (eTmp != null)
            {
                mobjLog.Error(string.Format("[ERROR-innerError] {0}", eTmp.Message), eTmp);
                eTmp = eTmp.InnerException;
            }

        }
#else

        public void Error(string strMethodName, string strErrorMsg, [CallerLineNumber] int iLn = 0, [CallerMemberName] string strName = null)
        {
            mobjLog.Error(string.Format("[ERROR] Linenumber:[{4}] {1}.{2} {3}", DateTime.Now,
                mstrCurrentClassName, strMethodName, strErrorMsg, iLn));
        }


        public void Error(string strM, string strErrorMsg, string strStackTrace, [CallerLineNumber] int iLn = 0, [CallerMemberName] string strName = null)
        {
            mobjLog.Error(string.Format("[ERROR] Linenumber:[{4}] {1}.{2} {3}, trace:[5]", DateTime.Now,
                mstrCurrentClassName, strM, strErrorMsg, iLn, strStackTrace));
        }

        public void Error(string strMethodName, string strErrorMsg, Exception e, [CallerLineNumber] int iLn = 0, [CallerMemberName] string strName = null)
        {
            mobjLog.Error(string.Format("[ERROR] Linenumber:[{4}] {1}.{2} {3}", DateTime.Now,
                mstrCurrentClassName, strMethodName, strErrorMsg, iLn), e);
            Exception eTmp = e.InnerException;
            while (eTmp != null)
            {
                mobjLog.Error(string.Format("[ERROR-innerError] {0}", eTmp.Message), eTmp);
                eTmp = eTmp.InnerException;
            }

        }
#endif
    }

}
