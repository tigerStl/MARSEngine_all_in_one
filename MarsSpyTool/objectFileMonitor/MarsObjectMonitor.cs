using MarsSpyTool.httpSvc;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.objectFileMonitor
{
    internal class MarsObjectMonitor
    {
        private static NLog.Logger logger = NLog.LogManager.GetLogger("SpyLogfile");
        private MarsFileMonitor currentObjectJsonMonitor = null;

        private static MarsObjectMonitor instance = null;
        public static MarsObjectMonitor Instance { 
            get
            {
                if (instance == null)
                {
                    /// 
                    instance = new MarsObjectMonitor();
                    
                }
                return instance;
            }
            set { instance = value; }
        }

        public static string backEndAPIEndpoint { get; set; }

        //public void InitMonitor()
        //{
        //    currentObjectJsonMonitor = MarsFileMonitor.InitMonitor(ObjectFileChangeImpl, ObjectFileCreateImpl, null,
        //        Mars.Utility.MarsConstants.CNST_SYPTOOL_JSONOBJ_FILENAME);
        //}

        public void InitMonitor(DealFileCreate fileCreateImpl)
        {
            currentObjectJsonMonitor = MarsFileMonitor.InitMonitor(ObjectFileChangeImpl, fileCreateImpl, null,
                Mars.message.Utility.MarsConstants.CNST_SYPTOOL_JSONOBJ_FILENAME);
        }

        private void ObjectFileChangeImpl(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin..");
            try
            {

            }
            finally
            {
                logger.Info($"{iMark}|end");
            }

        }

        private void ObjectFileCreateImpl(FileSystemEventArgs fileChangeEvent, ref bool isOk, ref string strError)
        {

            int iMark = new Random().Next();
            logger.Info($"{iMark}|begin..");
            try
            {
                //string strFileName = fileChangeEvent.FullPath;
                ///// read to object, and send to back end
                ///// 
                //logger.Info($"{iMark}|{strFileName}");
                ///// read file
                ///// 
                //var alllines = System.IO.File.ReadAllText(strFileName);
                //if (string.IsNullOrEmpty(alllines))
                //{
                //    logger.Error($"{iMark}|no text after readLines|{strFileName}");
                //    return;
                //}
                //int iMarkEnd = alllines.IndexOf(Mars.Utility.MarsConstants.CNST_SPYTOOL_OBJ_FILE_ENDMARK);
                //if (iMarkEnd < 0)
                //{
                //    logger.Info($"{iMark}|no last row mark at the end of the file||{strFileName}");
                //    return;
                //}
                //string strObjInfo2WebServer = alllines.Substring(0, iMarkEnd);
                //RestClient2MarsServer clnt = new RestClient2MarsServer();
                //isOk = clnt.sendRecgObjectsToServer(strObjInfo2WebServer, ref strError);
                //if (!isOk)
                //{

                //}
            }
            finally
            {
                logger.Info($"{iMark}|end");
            }
        }

    }
}
