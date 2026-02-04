#if !MESSAGESVC_FROM_GUI
extern alias clientWCF;

#endif


using Mars.message.AutoTestingDriver.interProcess;
using System;
using System.Collections.Generic;
using System.Messaging;
using Mars.message.Inter.MQCenter.interProcess;
#if !MESSAGESVC_FROM_GUI
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using com.Mars.Constants;
using Mars.Inter.MQCenter.MarsRESTFulClient;
using Mars.AutoTestingDriver.MarsImage;
#else
using Route2NSEx.src.Marquis.systemUtil;
#endif

namespace Mars.AutoTestingDriver.injector
{
    class ObjectInfoAnlyst
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ObjectInfoAnlyst));
        internal static Dictionary<string, string> AlystObjectPropertiesFromQtp(string strQuickAccess, ref bool isOk)
        {
            if (string.IsNullOrEmpty(strQuickAccess))
            {
                isOk = false;
                return null;
            }
            string[] arrProperties = strQuickAccess.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> dictResult = new Dictionary<string, string>();
            foreach (var itm in arrProperties)
            {
                int iPos = itm.IndexOf(":=");
                if (iPos == -1)
                {
                    isOk = false;
                    return null;
                }
                string strProperty = itm.Substring(0, iPos);
                string strValue = itm.Substring(iPos + ":=".Length);
                dictResult.Add(strProperty, strValue.Trim());
            }
            isOk = true;
            return dictResult;
        }

        internal static bool AlystObjectQuickAccessToPegAndObj(string strPegSource, string strObjSource, ref Dictionary<string, string> PegIdentifier, ref Dictionary<string, string> ObjIdentifier, ref string strError)
        {
            bool isOk = false;
            Logger.Info("AlystObjectQuickAccessToPegAndObj", string.Format("Peg:[{0}] Obj:[{1}]", strPegSource, strObjSource));

            PegIdentifier = AlystObjectPropertiesFromQtp(strPegSource, ref isOk);
            if (!isOk)
            {
                strError = string.Format("Pegwindows Quick_access format is wrong [{0}]", strPegSource);
                return false;
            }
            ObjIdentifier = null;
            if (string.Compare(strPegSource, strObjSource, true) != 0)
            {
                ObjIdentifier = AlystObjectPropertiesFromQtp(strObjSource, ref isOk);
                if (!isOk)
                {
                    strError = string.Format("Object Quick_access format is wrong [{0}]", strObjSource);
                    return false;
                }
            }

            return true;
        }
    }

    class InjectorMessageAgent
    {
        private const int cnst_default_waitUntil_time = 150;

        private static MLogger Logger = MLogger.GetLogger(typeof(InjectorMessageAgent));

        internal static bool cleanQueuebyName(string strQueueName, ref string strError)
        {
            if (!MessageQueue.Exists(strQueueName))
            {
                strError = string.Format("no such queue:[{0}]", strQueueName);
                return false;
            }
            MessageQueue mq = new MessageQueue(strQueueName);
            return cleanQuene(mq, ref strError);
        }
        internal static bool cleanQuene(MessageQueue targetQ, ref string strError)
        {
            if (targetQ == null) return true;
            try
            {
                var msgEnum = targetQ.GetMessageEnumerator2();
                while (msgEnum.MoveNext())
                {
                    var curMs = msgEnum.Current;
                    msgEnum.RemoveCurrent();
                }
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]", e.Message);
                return false;
            }

        }

        internal static bool DealWithKeyword_Pegwindow(long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, string strParaMeter,
            string strData, string strObjType, string strAttachInfo, string strPegName, string strObjName,
            ref string strError,
            ref MARSDealResult dealResult,
            int iWaitTime = 200)
        {
            ///suanfa:
            /// 1,chuanjian 消息，时间戳和序列号
            /// 2,等待客户端回传消息 
            /// 
            /// 1,chuanjian 消息，时间戳和序列号
            /// 
            //MessageQueue msq = new MessageQueue(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
            MarsMessageKeywordOpObjectInfo objPegWindowObj = new MarsMessageKeywordOpObjectInfo("PEGWINDOW");
            objPegWindowObj.PegwindowObjIdentification = dictPegProperties;
            objPegWindowObj.ObjectIdentification = dictObjProperties;
            objPegWindowObj.Parameter = strParaMeter;
            objPegWindowObj.DataToOperate = strData;
            objPegWindowObj.TestObjectType = strObjType;
            objPegWindowObj.StepId = stepId;
            objPegWindowObj.AttachedInfo = strAttachInfo;
            objPegWindowObj.PegName = strPegName;
            objPegWindowObj.objectName = strObjName;

            MARSDealResult objResult = null;

            /// dot core wpf
            if (Mars_applicationTyp.currentMarsAppType== Mars_applicationTyp.MARS_APPTYPE.MARS_CORE_WPF)
            {
                Logger.Info("DealWithKeyword_Pegwindow", "MARS_CORE_WPF mode");
                bool tmpOk = false;
                var inst = MARSRESTfulClientAPIMgr.GetInst(ref tmpOk, ref strError);
                if (!tmpOk)
                {
                    strError = string.Format("Can't get RESTful client instance, error:[{0}]", strError);
                    Logger.Error("DealWithKeyword_Pegwindow", strError);
                    return false;
                }
                else
                {
                    Logger.Info("DealWithKeyword_Pegwindow",$"port is|{inst.RESTfulPort}");
                }
                return objPegWindowObj.DealWithMsgViaRESTFulAPI(ref strError, out dealResult, iWaitTime);
            }

            bool isOk = objPegWindowObj.DealWithMessage(out objResult, iWaitTime);
            dealResult = objResult;
            Logger.Info("DealWithKeyword_Pegwindow", objResult == null ? "NULL" : objResult.ToString());
            return isOk;
        }


        internal static bool DealWithKeyword_GUIOp(string strKeyword, 
            long stepId, 
            Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties, 
            string strParaMeter,
            string strData,
            string strObjType,
            string strAttachedInfo,
            string strPegName,
            string strObjName,
            ref string strError,
            ref MARSDealResult dealResult,
            int iWaitTime = 300)
        {
#if _EngineDriver
            Logger.logBegin("DealWithKeyword_GUIOp", string.Format("Keyword:[{2}] Para:[{0}] data:[{1}] Peg:[{3}] obj:[{4}]|pegName|{5}|",
                strParaMeter, strData, strKeyword, MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.Dic2String(dictPegProperties)
                , MarsEnginer.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.Dic2String(dictObjProperties)
                , strPegName
                ));
#else
            Logger.logBegin("DealWithKeyword_GUIOp", string.Format("Keyword:[{2}] Para:[{0}] data:[{1}] Peg:[{3}] obj:[{4}]",
                strParaMeter, strData, strKeyword, Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.Dic2String(dictPegProperties)
                , Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.Dic2String(dictObjProperties)
                ));
#endif
            ///算法：
            /// 1，组装Message数据
            /// 2，送到message中心
            /// 
            try
            {
                MarsMessageKeywordOpObjectInfo objGUIKeyWordMessage = new MarsMessageKeywordOpObjectInfo(strKeyword);
                objGUIKeyWordMessage.PegwindowObjIdentification = dictPegProperties;
                objGUIKeyWordMessage.ObjectIdentification = dictObjProperties;
                objGUIKeyWordMessage.Parameter = strParaMeter;
                objGUIKeyWordMessage.DataToOperate = strData;
                objGUIKeyWordMessage.TestObjectType = strObjType;
                objGUIKeyWordMessage.StepId = stepId;
                objGUIKeyWordMessage.AttachedInfo = strAttachedInfo;

                objGUIKeyWordMessage.PegName = strPegName;
                objGUIKeyWordMessage.objectName = strObjName;
                objGUIKeyWordMessage.WaitingTime = dealResult.CheckObjectWaitingTime;
                if (string.Compare("WaitUntil", strKeyword ?? "", true) == 0)
                {
                    objGUIKeyWordMessage.WaitingTime = cnst_default_waitUntil_time;
                }

                /// 判断是否使用了图形匹配模式
                /// 
                if (MarsImageKeywordsEntry.IsImagePatterMode(strObjType))
                {
                    return MarsImageKeywordsEntry.DealWithImagePattern(objGUIKeyWordMessage, ref strError, ref  dealResult);
                }

                if (Mars_applicationTyp.currentMarsAppType == Mars_applicationTyp.MARS_APPTYPE.MARS_CORE_WPF)
                {
                    return objGUIKeyWordMessage.DealWithMsgViaRESTFulAPI(ref strError, out dealResult, iWaitTime);
                }

                MARSDealResult objResult = null;
                
                bool isOk = objGUIKeyWordMessage.DealWithMessage(out objResult, iWaitTime);
                dealResult = objResult;
                Logger.Info("DealWithKeyword_GUIOp", objResult == null ? "NULL" : objResult.ToString());
                return isOk;
            }
            catch (Exception e)
            {
                Logger.Error("DealWithKeyword_GUIOp", strError = string.Format("Exception:[{0}] stacktrace:[{1}]", e.Message, e.StackTrace));
                return false;
            }
            finally
            {
                Logger.logEnd("DealWithKeyword_GUIOp");
            }


        }

    }
}
