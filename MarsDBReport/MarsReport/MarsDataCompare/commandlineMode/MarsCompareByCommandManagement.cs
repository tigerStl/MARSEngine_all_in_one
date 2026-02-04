using DomUtil;
using Mars.BasicData;
using Mars.MarsConfig;
using Mars.TestFramework.DataCompare;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Xml;

namespace MarsDataCompare.commandlineMode
{
    public class CallbackJosnRspns
    {
        public int status { get; set; }
        public string data { get; set; }
        public string message { get; set; }
        public string data2 { get; set; }
    }

    public class MarsCompareByCommandManagement
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MarsCompareByCommandManagement));

        public static string WebReportCompareCallback = ConfigurationManager.AppSettings[MarsAppParameter.cnst_callback_baseUri_P];
        internal static string DoCompareBySettings(MarsAppParameter para, ref bool isOk, ref string strError)
        {
            logger.logBegin("DoCompareBySettings", para==null?"NO Parameter":para.ToString());
            if (para==null) {
                strError = "wrong parameters";
                isOk = false;   
                return null;
            }
            /// for less coding, reuse codes, set a fake MARS_HOME
            if (MarsAppParameter.cnst_mode_FromWebComp.Equals(para.appMode, StringComparison.OrdinalIgnoreCase))
            {
                DataCompareError compareError = new DataCompareError();
                try
                {
                    /// for less coding, reuse codes, set a fake MARS_HOME
                    /// 
                    Environment.SetEnvironmentVariable(MarsConfig.MARS_HOME, para.appHomeDirectory);
                    /// 
                    /// get config files, get
                    /// 
                    MarsConfig cfg = MarsConfig.Configure("FROMWEB");

                    if (ExecuteCompare.mc == null) {
                        ExecuteCompare.mc = cfg;
                    }
                    /// get output filename and path
                    /// 
                    /// get compare info from compare id
                    /// 
                    WebApiDomHelper.API_SCHEMA = para.appDb;
                    WebApiDomHelper.ReadXmlDoc(para.appDb);
                    // get compare db info
                    string strFile1 = "", strFile2 = "", strOutFile = para.outputDir;
                    
                    var cmprRslt = (new MarsCompare()).RunCompare(para.appCompareId, strFile1, strFile2, strOutFile, ref compareError, WebApiDomHelper.doc, true);
                    MARS.CompareGUI.Utilities.MarsRestClient apiClient = new MARS.CompareGUI.Utilities.MarsRestClient(MarsCompareByCommandManagement.WebReportCompareCallback);
                    var rslt = apiClient.InvokeBackWhenCompareIsDone(para.uuid,   cmprRslt ,compareError.Message, para.appDb, compareError.Status, ref isOk, ref strError);
                    if (!isOk)
                    {
                        logger.Error("DoCompareBySettings", $"Can't update compare result to server");
                    }
                    else
                    {
                        try
                        {
                            CallbackJosnRspns rspns = System.Text.Json.JsonSerializer.Deserialize<CallbackJosnRspns>(rslt);
                            if (rspns != null )
                            {
                                if (rspns.status == 1)
                                {
                                    isOk = true;
                                    logger.Info("DoCompareBySettings", $"have created file at|{rspns.data}|");
                                    return rspns.data;
                                }
                                else
                                {
                                    isOk= false;
                                    logger.Error("DoCompareBySettings", $"Error when create file|{rspns.message}|");
                                    strError = rspns.message;
                                    return strError;
                                }
                            }
                            else
                            {
                                isOk = false;
                                logger.Error("DoCompareBySettings", strError = "Can't get response from MARS server");
                                return strError;
                            }
                        }catch(Exception e)
                        {
                            logger.Error("DoCompareBySettings", e.Message, e);
                            isOk = false;
                            strError = $"Exception exists when notify MARS server, please check log for details";
                            return null;
                        }
                    }
                    return compareError.refFileNameWithPath;
                }catch (Exception ex)
                {
                    isOk = false;
                    strError = ex.Message;
                    compareError.Status = false;
                    compareError.Message = "Exceptions when generate report, please check Logfile for details";
                    logger.Error("MarsCompareByCommandManagement", strError, ex);
                    /// call back to web, notify
                    /// 
                    MARS.CompareGUI.Utilities.MarsRestClient apiClient = new MARS.CompareGUI.Utilities.MarsRestClient(MarsCompareByCommandManagement.WebReportCompareCallback);

                    var rsps = apiClient.InvokeBackWhenCompareIsDone(para.uuid,"", compareError.Message,para.appDb, false, ref isOk, ref strError);
                    logger.Info("MarsCompareByCommandManagement", $"after call InvokeBackWhenCompareIsDone|{rsps}");
                    return compareError.Message;
                }
            }
            isOk = false;
            strError = $"unsupported mode|{para.appMode}";
            return strError;
        }
    }
}
