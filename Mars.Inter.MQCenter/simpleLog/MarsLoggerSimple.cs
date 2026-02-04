using System;
using System.Diagnostics;

namespace Mars.message.Inter.MQCenter.simpleLog
{
    public class MarsLoggerSimple
    {
#if _MARS_COM_LOG
        public static string cnst_file_pre_fix = "MarsComAgntLog";
#else
        public static string cnst_file_pre_fix = "MarsAgntLogifc";
        private static string _logfile_suffix = "";
        public static string logfile_suffix {
            get => _logfile_suffix;
            set
            {
                if (_logfile_suffix != value)
                {
                    _logfile_suffix = value;
                    logger = new MarsLoggerSimple();
                }
            }
        }
#endif
        private static MarsLoggerSimple logger = new MarsLoggerSimple();
        private string CurrentLogFileName_Prefix;
        private string CurrentLogFileName;
        private int DefaultSize;
        private int DefaultLogFileNumber;
        private System.IO.StreamWriter currentLogFs = null;

        public static bool isDebug = false;


        private MarsLoggerSimple()
        {
            try
            {
                Init();
            }catch(Exception e)
            {
                Console.WriteLine($"[ERROR], exception:{e.Message}\r\n{e.StackTrace}");
            }
        }
        private void Init()
        {
            DefaultSize = 20;
            DefaultLogFileNumber = 20;
            string strPath = "";
            CurrentLogFileName_Prefix = System.IO.Path.Combine(
                strPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(MarsLoggerSimple).Assembly.Location), "log\\"),
                    string.Format("{1}_{0}{2}_{3}.",
                        DateTime.Now.ToString("MM_dd_yyyy"),
                        string.IsNullOrEmpty(cnst_file_pre_fix) ? "MarsAgntLogifc" : cnst_file_pre_fix,
                        logfile_suffix,
                        Process.GetCurrentProcess().Id
                        ));

#if !_ForClickOnce
            System.Diagnostics.EventLog.WriteEntry("Application", CurrentLogFileName_Prefix);
#endif
            if (!System.IO.File.Exists(CurrentLogFileName_Prefix + "log"))
            {
                (System.IO.File.CreateText(CurrentLogFileName_Prefix + "log")).Close();
            }

            long iSize = (new System.IO.FileInfo(CurrentLogFileName_Prefix + "log")).Length;
            if (iSize > DefaultSize * 1024 * 1024)
            {
                CurrentLogFileName = FindNewFileName(CurrentLogFileName_Prefix);
            }
            else
            {
                CurrentLogFileName = string.Format("{0}log", CurrentLogFileName_Prefix);
            }

            currentLogFs = System.IO.File.AppendText(CurrentLogFileName);
            currentLogFs.WriteLine("Log file created at {0}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff"));
            currentLogFs.Flush();

        }
        private string FindNewFileName(string strFileNamePrefix)
        {
            string strTmpFileName= $"{strFileNamePrefix}1.log";
            for (int i = DefaultLogFileNumber - 1; i >= 0; i--)
            {
                if (System.IO.File.Exists(strTmpFileName = string.Format("{0}{1}.log", strFileNamePrefix, i == 0 ? "" : i + "")))
                {
                    ///move file by change file Name 
                    /// 
                    try
                    {
                        string strNewFile = //i==0? string.Format("{0}.log", strFileNamePrefix):
                            string.Format("{0}{1}.log", strFileNamePrefix, i + 1);
                        if (System.IO.File.Exists(strNewFile))
                        {
                            System.IO.File.Delete(strNewFile);
                        }
                        System.IO.File.Move(strTmpFileName, strNewFile);
                    }
                    catch (Exception e)
                    {
#if !_ForClickOnce
                        System.Diagnostics.EventLog.WriteEntry("Application", string.Format("{0} stack:[{1}]", e.Message, e.StackTrace));
#endif      
                    }
                    continue;
                };
            }

            return strTmpFileName;
        }


        private static int FileAppendTimes = 0;
        public static string PreFix = "";
        public static bool isPerformance = false;

        public const string cnst_ignore_prefix = "=====";
        public static void Info(string strMethod, string strLog, string strHead = "INFO",bool isLogOnceMark=false)
        {
#if demo_4_Hundsun_1
            System.IO.File.AppendAllText(logger.CurrentLogFileName, string.Format("[INFO] {0} [{1}] {2}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strLog));
#else
            // for performance, just return 
            if (isPerformance)
            {
                Console.WriteLine("\t[INFO]isPerformance true");
                return;
            }

            if (logger.currentLogFs == null)
            {
                logger.currentLogFs = System.IO.File.AppendText(logger.CurrentLogFileName);
                logger.currentLogFs.WriteLine("Log file is closed and -re= created at {0}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff"));
                Console.WriteLine("\t[INFO]currentLogFs is null");
                //return;
            }
            
            if (isLogOnceMark) return;

            if (strMethod.StartsWith("\t"))
            {
                //strMethod = strMethod.Substring(1);
                logger.currentLogFs.WriteLine("\t{0} [{1}] {2} {3}", strHead, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strMethod, strLog);
            }
            else
            {
                if (string.Compare(strMethod, "\t", true) == 0)
                    logger.currentLogFs.WriteLine(" {0} [{1}] {2}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strLog);
                else
                {
                    if (strMethod.Equals(cnst_ignore_prefix))
                    {
                        logger.currentLogFs.WriteLine($"\t{strLog}");
                    }
                    else
                    {
                        logger.currentLogFs.WriteLine("[{0}] [{3}] [{1}] {2} {4}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strLog, strHead, PreFix);
                    }
                }
            }
            CheckSizeAndFlush();
#endif
        }

        public static void DEBUG(string strMethod, string strLog, string strHead = "DEBUG")
        {
            if (!isDebug) return;
            if (strMethod.StartsWith("\t"))
            {   
                logger.currentLogFs.WriteLine("\t{0} [{1}] {2}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strLog);
            }
            else
            {
                if (string.Compare(strMethod, "\t", true) == 0)
                    logger.currentLogFs.WriteLine("[0] [1] [{2}] {3}", strHead,strMethod, 
                        DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), 
                        strLog);
                else
                    logger.currentLogFs.WriteLine("[{0}] {2} [{1}] {4}[{3}]", 
                        strHead,
                        strMethod, 
                        DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "),
                        PreFix,
                        strLog 
                        );
            }
            CheckSizeAndFlush();
        }

        public static void Warnning(string strMethod, string strLog, string strHead = "WARNNING")
        {
            if (logger.currentLogFs == null) return;
            if (string.Compare(strMethod, "\t", true) == 0)
                logger.currentLogFs.WriteLine(" {0} [{1}] {2}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strLog);
            else
                logger.currentLogFs.WriteLine("[{0}] [{1}] {2} {4}[{3}]", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strLog, strHead, PreFix);
            CheckSizeAndFlush();
        }

        public static void Warning(string strMethod, string strLog, string strHead = "WARNNING")
        {
            Warnning(strMethod, strLog, strHead);
        }


        public static void logEnd(string strMethod)
        {
            Info(strMethod, "End", "END");
            PreFix = "";
        }

        public static void logEnd(string strMethod, string strInfo)
        {
            Info(strMethod, strInfo, "END");
            PreFix = "";
        }

        public static void logBegin(string strMethod)
        {
            Info(strMethod, "Begin", "BEGIN");
        }

        public static void logBegin(string strMethod, string strInfo)
        {
            Info(strMethod, "Begin", strInfo);
        }

        public static void Error(string strMethod, string strError, string strStackTrace = null)
        {
#if demo_4_Hundsun_1
            System.IO.File.AppendAllText(logger.CurrentLogFileName, string.Format("[ERROR] {0} [{1}] {2}\r\n{3}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strError,strStackTrace));
#else

            if (logger.currentLogFs == null) return;
            if (string.Compare(strMethod, "\t", true) == 0)
                logger.currentLogFs.WriteLine("{0}[ERROR] [{1}] {2} \r\n\t{3}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff"), strError, strStackTrace ?? "");
            else
                logger.currentLogFs.WriteLine("{4}[ERROR] [{0}] [{1}] {2} \r\n\t{3}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff"), strError, strStackTrace ?? "", PreFix);
            CheckSizeAndFlush();
#endif
        }


        public static void Error(string strMethod, string strError, Exception e)
        {
            if (logger.currentLogFs == null) return;
            string strInnerError = "";
            Exception eTmp = e.InnerException;
            while (eTmp != null)
            {
                strInnerError = string.Format("{0}\r\n{1}", eTmp.StackTrace, strInnerError);
                eTmp = eTmp.InnerException;

            }
#if demo_4_Hundsun_1
            System.IO.File.AppendAllText(logger.CurrentLogFileName, string.Format("[ERROR] {0} [{1}] {2}\r\n{3}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff "), strError, eTmp));
#else
            if (string.Compare(strMethod, "\t", true) == 0)
                logger.currentLogFs.WriteLine("{0}[ERROR] [{1}] {2} \r\n\t{3}", strMethod, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff"), strError, strInnerError ?? "");
            else
                logger.currentLogFs.WriteLine("{4}[ERROR] [{0}] [{1}] {2} \r\n\t{3}", strMethod, 
                    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss fff"), 
                    strError, e.StackTrace +"\r\n"+ strInnerError ?? "", 
                    PreFix);
            CheckSizeAndFlush();
#endif
        }

        private static void CheckSizeAndFlush()
        {

            FileAppendTimes += 1;
            //logger.currentLogFs.Flush();
            //if (FileAppendTimes >= 5)
            {

                FileAppendTimes = 0;
                logger.currentLogFs.Flush();
                //logger.currentLogFs.Close();
                try
                {
                    //logger.currentLogFs.WriteLine("Begin check size");
                    long iSize = (new System.IO.FileInfo(logger.CurrentLogFileName)).Length;
                    //logger.currentLogFs.WriteLine("size is {0}", iSize);
                    if (iSize > (logger.DefaultSize * 1024 * 1024))
                    {
                        logger.currentLogFs.WriteLine("change file name , current file:[{0}] prefix:[{1}]", logger.CurrentLogFileName, logger.CurrentLogFileName_Prefix);

                        try
                        {
                            string strTmpfileName = logger.FindNewFileName(logger.CurrentLogFileName_Prefix);
                            logger.currentLogFs.WriteLine($"going to move a new name:{strTmpfileName}");
                            logger.currentLogFs.Flush();
                            logger.CurrentLogFileName = strTmpfileName;
                            logger.currentLogFs.Close();
                            logger.currentLogFs = System.IO.File.AppendText(logger.CurrentLogFileName);
                        }
                        catch(Exception e)
                        {
                            logger.currentLogFs.WriteLine($"can't switch log file|{e.Message}|{e.StackTrace}");
                        }
                        logger.currentLogFs.WriteLine("change file name to [{0}]", logger.CurrentLogFileName);
                    }
                    else
                    {
                        //logger.CurrentLogFileName = string.Format("{0}log", logger.CurrentLogFileName_Prefix);
                    }
                }
                catch (Exception e)
                {
                    // logger.currentLogFs.WriteLine("{0}",e.Message);
                }

            }
        }
    }
}
