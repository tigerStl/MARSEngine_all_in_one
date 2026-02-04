using Mars.message.Utility;
using NLog;
using NLog.Config;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.objectFileMonitor
{
    public delegate void DealFileChange(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError);
    public delegate void DealFileCreate(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError);
    public delegate void DealFileDelete(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError);
    public  class MarsFileMonitor
    {
        private FileSystemWatcher watcher;
        private static NLog.Logger logger = NLog.LogManager.GetLogger("MarsSpyLog");

        public DealFileChange dealFileChangeHandler;
        public DealFileCreate dealFileCreateHandler;   
        public DealFileDelete dealFileDeleteHandler;  

        public static MarsFileMonitor InitMonitor(
            DealFileChange fileChangeHandler = null,
            DealFileCreate fileCreateHandler = null,
            DealFileDelete fileDeleteHandler = null,
            string fileNamePattern= MarsConstants.CNST_SYPTOOL_MONITOR_TYPE)
        {
            MarsFileMonitor marsFileMonitor = new MarsFileMonitor(); 

            string strMonitorPath = typeof(MarsFileMonitor).Assembly.Location;
            string UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            strMonitorPath = System.IO.Path.GetDirectoryName(strMonitorPath);

            strMonitorPath = System.IO.Path.Combine(strMonitorPath, $"data\\obj\\{UserName}");
            if (!System.IO.Directory.Exists(strMonitorPath))
                System.IO.Directory.CreateDirectory(strMonitorPath);
            marsFileMonitor.watcher = new FileSystemWatcher(strMonitorPath);
            marsFileMonitor.watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName ;

            marsFileMonitor.watcher.Filter = fileNamePattern;// MarsConstants.CNST_SYPTOOL_MONITOR_TYPE;
            marsFileMonitor.initMonitorMethod(fileChangeHandler, fileCreateHandler, fileDeleteHandler);
            marsFileMonitor.watcher.EnableRaisingEvents = true;

            return marsFileMonitor;
        }

        private MarsFileMonitor()
        {

        }
        protected void initMonitorMethod(DealFileChange fileChangeHandler = null,
            DealFileCreate fileCreateHandler = null,
            DealFileDelete fileDeleteHandler = null)
        {
            watcher.Changed += OnChange;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDelete;

            this.dealFileChangeHandler = fileChangeHandler;
            this.dealFileCreateHandler = fileCreateHandler;
            this.dealFileDeleteHandler = fileDeleteHandler;
        }
        internal void OnChange(object source, FileSystemEventArgs e)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin");
            if (source == null)
            {
                logger.Info($"source is null");
            }
            else
            {
                logger.Info($"{iMark}|{source.ToString()}|{source.GetType()}");
            }
            bool isOk = false;
            string strError = "";
            if (this.dealFileChangeHandler != null)
            {
                try
                {
                    this.dealFileChangeHandler(e, ref isOk, ref strError);
                    if (!isOk)
                    {
                        logger.Error(strError);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
            logger.Info($"File {e.FullPath} {e.ChangeType}");
        }
        internal void OnCreated(object source, FileSystemEventArgs e)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin");
            if (source == null)
            {
                logger.Info($"source is null");
            }
            else
            {
                logger.Info($"{iMark}|{source.ToString()}|{source.GetType()}");
            }
            bool isOk = false;
            string strError = "";
            if (this.dealFileCreateHandler != null)
            {
                try {
                    this.dealFileCreateHandler(e, ref isOk, ref strError);
                    if (!isOk)
                    {
                        logger.Error(strError);
                    }
                }catch(Exception ex)
                {
                    logger.Error(ex);
                }
            }
            logger.Info($"File {e.FullPath} {e.ChangeType}");
        }

        private void OnDelete(object source, FileSystemEventArgs e)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin");
            if (source == null)
            {
                logger.Info($"source is null");
            }
            else
            {
                logger.Info($"{iMark}|{source.ToString()}|{source.GetType()}");
            }
            bool isOk = true;
            string strError = "";
            if (this.dealFileDeleteHandler != null)
            {
                try
                {
                    this.dealFileDeleteHandler(e, ref isOk, ref strError);
                    if (!isOk)
                    {
                        logger.Error(strError);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            logger.Info($"File {e.FullPath} {e.ChangeType}");
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin");
            if (source == null)
            {
                logger.Info($"source is null");
            }
            else
            {
                logger.Info($"{iMark}|{source.ToString()}|{source.GetType()}");
            }
            Console.WriteLine($"File {e.FullPath} {e.ChangeType}");
        }

    }
}
