using Mars.message.Inter.MQCenter.interProcess.HttpRestService.SvcMode;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.message.Inter.MQCenter.interProcess.HttpRestService.client
{
    internal class RestServiceInfo
    {
        private static Logger logger = LogManager.GetLogger("MarsSpyLog");
        private static RestServiceInfo instance=null;

        public SpyInternalPortInfo currentRESTfulSvc = null;
        public static RestServiceInfo Instance()
        {
            logger.Info("RestServiceInfo.Instance\tBegin");
            if (instance==null){
                instance = new RestServiceInfo();
            }
            logger.Info("RestServiceInfo.Instance\tEnd");
            return instance;
        }

        public bool loadFromFile(string strFile,ref string strError)
        {
            logger.Info($"loadFromFile begin |{strFile}");
            try
            {
                if (!System.IO.File.Exists(strFile))
                {
                    strError = $"no such file exist|{strFile}";
                    logger.Error($"loadFromFile\t{strError}");
                    return false;
                }
                string strPortInfo = System.IO.File.ReadAllText(strFile);
                try
                {
                    currentRESTfulSvc = JsonConvert.DeserializeObject<SpyInternalPortInfo>(strPortInfo);
                    return true;
                }
                catch (Exception e)
                {
                    strError = $"Can't pharse file|{strFile}| with error|{e.Message}";
                    logger.Error(e, $"loadFromFile\t{strError}");
                    return false;
                }
            }
            finally
            {
                logger.Info("loadFromFile\tend");
            }
        }
    }
}
