using Mars.message.Inter.MQCenter.interProcess.HttpRestService.SvcMode;
using Mars.message.Inter.MQCenter.simpleLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MarsSpyTool.Utility.directoryMonitor
{

    public delegate void MARSObjectsDirectoryModifyHandler(string strFileName, ref bool isOk, ref string strError);


    public class MarsFileChangedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public WatcherChangeTypes ChangeType { get; }
        public string OldFilePath { get; }

        public MarsFileChangedEventArgs(string filePath, WatcherChangeTypes changeType, string oldFilePath = null)
        {
            FilePath = filePath;
            ChangeType = changeType;
            OldFilePath = oldFilePath;
        }
    }

    public class MarsFileChangeNotificationService
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("MarsSpyLog");

        private readonly FileSystemWatcher watcher;

        public event EventHandler<MarsFileChangedEventArgs> FileChanged;

        private static MarsFileChangeNotificationService gMarsFileNotifySvc=null;

        
        public static MarsFileChangeNotificationService GetInst()
        {
            logger.Info($"GetInst\tBegin");
            ///1 获得当前目录
            ///2 
            ///
            try
            {
                string strDir = typeof(MarsFileChangeNotificationService).Assembly.Location;
                strDir = System.IO.Path.GetDirectoryName(strDir);
                strDir = System.IO.Path.Combine(strDir, MarsRestFulCnst.cnst_SwapDir);
                if (!Directory.Exists(strDir))
                {
                    Directory.CreateDirectory(strDir);
                }
                return GetInst(strDir);
            }catch(Exception e)
            {
                logger.Error(e,$"GetInst\tException|{e.Message}|{e.StackTrace}");
                return null;
            }
            finally
            {
                logger.Info("GetInst\tEnd");
            }
            
        }

        public static MarsFileChangeNotificationService GetInst(string directoryPath, string filter = "*.json")
        {
            logger.Info($"GetInst\tBegin|{directoryPath}|{filter}|");
            try
            {
                if (gMarsFileNotifySvc == null)
                {
                    gMarsFileNotifySvc = new MarsFileChangeNotificationService(directoryPath, filter);
                }
                return gMarsFileNotifySvc;
            }
            finally
            {
                logger.Info($"GetInst|{directoryPath}|End");
            }
        }

        private MarsFileChangeNotificationService(string directoryPath, string filter = "*.*")
        {
            watcher = new FileSystemWatcher(directoryPath, filter);
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.IncludeSubdirectories = true;
            //FileSystemEventHandler(object sender, FileSystemEventArgs e);
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            //watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;            
        }

        public void startSVC()
        {
            watcher.EnableRaisingEvents = true;
            
        }
        public void stopSVC()
        {
            watcher.EnableRaisingEvents= false;
        }

        public void setCallBack(MarsFileChangedEventArgs callBackImplement)
        {

        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            OnFileChanged(new MarsFileChangedEventArgs(e.FullPath, e.ChangeType));
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            OnFileChanged(new MarsFileChangedEventArgs(e.FullPath, e.ChangeType, e.OldFullPath));
        }

        protected virtual void OnFileChanged(MarsFileChangedEventArgs e)
        {
            FileChanged?.Invoke(this, e);
        }
    }
}
