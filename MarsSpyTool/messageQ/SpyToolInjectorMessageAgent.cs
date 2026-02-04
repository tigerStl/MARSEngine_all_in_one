

using Mars.message.AutoTestingDriver.interProcess;
using System;
using System.Collections.Generic;
using System.Messaging;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.windowsWrapper.SystemUtil;

namespace Mars.AutoTestingDriver.injector
{
    class ObjectInfoAnlyst
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(ObjectInfoAnlyst));
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
            Logger.Info("AlystObjectQuickAccessToPegAndObj\tBegin|"+string.Format("Peg:[{0}] Obj:[{1}]", strPegSource, strObjSource));

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

    class SpyToolInjectorMessageAgent
    {
        private const int cnst_default_waitUntil_time = 150;

        private static NLog.Logger Logger = NLog.LogManager.GetLogger(nameof(SpyToolInjectorMessageAgent));

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
            ref MARSDealResult dealResult)
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

            bool isOk = objPegWindowObj.DealWithMessage(out objResult);
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
            ref MARSDealResult dealResult)
        {

            Logger.Info("DealWithKeyword_GUIOp\t"+string.Format("Keyword:[{2}] Para:[{0}] data:[{1}] Peg:[{3}] obj:[{4}]",
                strParaMeter, strData, strKeyword, MarsWindowsAPIsExtend.Dic2String(dictPegProperties),
                MarsWindowsAPIsExtend.Dic2String(dictObjProperties)
                ));
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
                MARSDealResult objResult = null;
                
                bool isOk = objGUIKeyWordMessage.DealWithMessage(out objResult);
                dealResult = objResult;
                Logger.Info("DealWithKeyword_GUIOp"+( objResult == null ? "NULL" : objResult.ToString()));
                return isOk;
            }
            catch (Exception e)
            {
                Logger.Error(e, strError = string.Format("Exception:[{0}] stacktrace:[{1}]", e.Message, e.StackTrace));
                return false;
            }
            finally
            {
                Logger.Info("DealWithKeyword_GUIOp\tEnd");
            }


        }

    }
}
