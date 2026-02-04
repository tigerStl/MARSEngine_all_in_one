using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.AutoTestingDriver.message;
using Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData;
using System;
using System.Collections.Generic;
using System.Messaging;
using System.Threading;
using System.Xml;
using Mars.Inter.MQCenter.MarsRESTFulClient;
using System.Xml.Serialization;
using System.IO;
using System.Net.Http;
using System.Text;
using MarsCore.MessageCenter;

namespace Mars.message.Inter.MQCenter.interProcess
{
    public class MARSMessageSvc
    {

        protected static MessageQueue MessageQServices = null;
        protected static MessageQueue MessageClientQueue = null;

        public static string currentUserName = null;

        public static void CleanClientQueue()
        {
            string strClientQueueName = MarsMessageConst.UniqueMQClnName();// string.IsNullOrEmpty(currentUserName) ? MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME : $"{MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME}_{currentUserName}";
            //Environment.UserName
            if (MessageClientQueue == null)
            {

                //if (MessageQueue.Exists(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME))
                //{
                //    MessageClientQueue = new MessageQueue(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
                //}
                //else
                //{
                //    MessageClientQueue = MessageQueue.Create(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
                //}
                if (MessageQueue.Exists(strClientQueueName))
                {
                    MessageClientQueue = new MessageQueue(strClientQueueName);
                }
                else
                {
                    //MessageClientQueue = MessageQueue.Create(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
                    MessageClientQueue = MessageQueue.Create(strClientQueueName);
                }

            }
            MessageClientQueue.Purge();


        }

        /// <summary>
        /// 线程
        /// </summary>
        public static void StartMsmqMarsServiceViaThread(string strMessageQueueName)
        {
            if (MessageQueue.Exists(strMessageQueueName))
            {
                MessageQServices = new MessageQueue(strMessageQueueName);
            }
            else
            {
                MessageQServices = MessageQueue.Create(strMessageQueueName);
            }
            MessageQServices.Purge();
            //CurrentSessionId = Guid.NewGuid();            
        }

        public string CurrentErrorMessage;
        protected bool IsMessageGetOk = false;
    }

    public class MARSMessageSvcServer : MARSMessageSvc
    {

        public static void SendMessageToMQ(object objMsgToSend)
        {
            string strQueueName = MarsMessageConst.UniqueMQSvrName(); // string.IsNullOrEmpty(currentUserName) ? MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME : $"{MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME}_{currentUserName}";
            if (MessageQServices == null)
                //StartMsmqMarsServiceViaThread(MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME);
                StartMsmqMarsServiceViaThread(strQueueName);

            // if the para is null, then just create the message
            if (objMsgToSend == null)
                return;

            MessageQServices.Formatter = new XmlMessageFormatter()
            {
                TargetTypes = new Type[] { objMsgToSend.GetType() }
            };// new BinaryMessageFormatter();
            Message msg = new Message(objMsgToSend, MessageQServices.Formatter);
            MessageQServices.Send(msg);
        }

        private MARSMessagesBase currentMessageRequired; //MARSTestStep
#if !MESSAGESVC_FROM_GUI
        internal MARSDealResult WaitForReply(MARSMessagesBase sourceMsg, ref bool isOk, 
            ref string strError,
            ref string strAdv, 
            ref string strStack,
            bool isToPreviewObj = false,
            MARSMessagesBase previewObjectInfo = null,
            int iWaitTime=200)
        {
            try
            {
                currentMessageRequired = sourceMsg;
                simpleLog.MarsLoggerSimple.Info("\t", "...... 1, currentMessageRequired = sourceMsg");
                Thread thrdMsmqRcv = new Thread(new ThreadStart(() => RecvFromClientQ(sourceMsg,iWaitTime)));
                thrdMsmqRcv.Priority = ThreadPriority.AboveNormal;
                thrdMsmqRcv.Start();
                thrdMsmqRcv.Join();
                MARSTestStep tstStp = currentMessageRequired as MARSTestStep;
                Console.WriteLine("\r\n\tWaitForReply-MARSTestStep-{0}", tstStp == null ? "" : tstStp.ToString());
                simpleLog.MarsLoggerSimple.Info("WaitForReply-MARSTestStep-{0}", tstStp == null ? "" : tstStp.ToString());
                MARSDealResult objRslt = new MARSDealResult()
                {
                    ReturnedData = tstStp.AttachInfo,
                    ResultMessage = tstStp.TestResult == MARSStepResult.e_Result_Ok ? "OK" : "FAILED",
                    ErrorMessage = tstStp.RuntimeResult,
                    StackInfo = tstStp.stackTrace,
                    Advice = tstStp.advice2User,
                    SnapshotFileNameWhenErrorOccurs = tstStp.snapshotFileNameWhenErrorOccurs,
                    snapshotFilePath = tstStp.snapshotFileNameWhenErrorOccurs
                };
                isOk = objRslt.IsResultSucess;
                return objRslt;
            }
            catch (Exception e)
            {
                MARSDealResult objRslt = null;
                if (currentMessageRequired != null)
                {
                    MARSTestStep tstStp = currentMessageRequired as MARSTestStep;
                    objRslt = new MARSDealResult()
                    {
                        ResultMessage = tstStp.TestResult == MARSStepResult.e_Result_Ok ? "OK" : "FAILED",
                        ErrorMessage = string.Format("Exception:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace)
                    };
                }
                else
                {
                    objRslt = new MARSDealResult()
                    {
                        ResultMessage = "FAILED",
                        ErrorMessage = string.Format("Exception:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace)
                    };
                }

                isOk = false;
                strError = $"Error while dealing with keyword";
                strAdv = "Unidentified error. If this continues, contact Marquis";

                string tmpInner = e.InnerException == null ? "" : e.InnerException.Message;
                strStack = $"{e.Message}\r\n{e.StackTrace}{tmpInner}";
                return objRslt;
            }

        }
#endif
        public static int DefaultWaitSeconds = 190;
#if !MESSAGESVC_FROM_GUI
        private void RecvFromClientQ(MARSMessagesBase messageSource, int iWaitTime = 200)
        {
            Message msg = null;
            try
            {
                if (MessageClientQueue == null)
                {
                    if (MessageQueue.Exists(MarsMessageConst.UniqueMQClnName() /*MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME*/))
                    {
                        MessageClientQueue = new MessageQueue(MarsMessageConst.UniqueMQClnName() /*MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME*/);
                    }
                    else
                    {
                        MessageClientQueue = MessageQueue.Create( MarsMessageConst.UniqueMQClnName() /*MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME*/);
                    }

                }
                //MessageClientQueue.Purge();//clean
                MessageClientQueue.Formatter = new XmlMessageFormatter();// new BinaryMessageFormatter();           
                currentMessageRequired = null;
                simpleLog.MarsLoggerSimple.Info("\t", "set 2, currentMessageRequired = null");
                bool isToContinue = true;
                while (isToContinue)
                {
                    try
                    {
                        isToContinue = (msg = MessageClientQueue.Receive(new TimeSpan(0, 0, iWaitTime))) != null;
                    }
                    catch (Exception e)
                    {
                        isToContinue = false;
                        CurrentErrorMessage = string.Format("Exception when get message from Messagequeue:[{0}]", e.Message);
                        IsMessageGetOk = false;
                        return;
                    }

                    if ((msg == null) || (msg.BodyStream == null))
                    {
                        isToContinue = true;
                        continue;
                    }

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(msg.BodyStream);
                    bool isOk = false;
                    string strError = "";
                    string strAdv = "", strStack = "";
                    MARSMessagesBase objFromMQ = MarsMessageClientSvc.GetMsgObjViaRawXmlDoc(xmlDoc, ref isOk, ref strError, ref strAdv, ref strStack);

                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", string.Format("Error from GetMsgObjViaRawXmlDoc:[{0}] with xml:[{1}]", strError, xmlDoc.InnerXml));
                        Thread.Sleep(3000);
                        isToContinue = true;
                        continue;
                    }
                    
                    MARSMessagesBase objMsgBase = objFromMQ;//msg.Body as MARSMessagesBase;
                    if (objMsgBase.MessageType == MARSMessageType.e_Get_HeartBeat)
                        continue;
                    if (objMsgBase.MessageType == MARSMessageType.e_Get_SessionId)
                        continue;
                    if (objMsgBase.MessageType == MARSMessageType.e_Operation_FromClient)
                    {
                        //请求operation
                        MARSTestOperation opMsg = objMsgBase as MARSTestOperation;
                        if (opMsg == null)
                        {
                            simpleLog.MarsLoggerSimple.Error("RecvFromClientQ", $"expected message is e_Operation_FromClient, but the object can't be casted to ");
                            continue;
                        }
                        switch (opMsg.OperationType)
                        {
                            case MARSOperationType.e_mouseLeftDoubleClick:
                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(opMsg.Left, opMsg.Top);
                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(opMsg.Left, opMsg.Top);
                                break;
                            case MARSOperationType.e_mouseLeftClick:
                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(opMsg.Left, opMsg.Top);
                                break;
                            case MARSOperationType.e_mouseRightClick:
                                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(opMsg.Left, opMsg.Top);
                                break;
                            case MARSOperationType.e_sendkeys:
                                System.Windows.Forms.SendKeys.SendWait(opMsg.AttachData);
                                break;
                        }
                        //将消息回送
                        opMsg.MessageType = MARSMessageType.e_Operation_FromServer;
                        SendMessageToMQ(opMsg);
                        continue;

                    }
                    //if (objMsgBase.SeriousNumber != messageSource.SeriousNumber)
                    //    continue;
                    MARSTestStep srcStp = messageSource as MARSTestStep,
                        curStp = objMsgBase as MARSTestStep;
                    if ((srcStp != null) && (curStp != null))
                    {
                        if (srcStp.RunId != curStp.RunId)
                            continue;
                    }
                    //if (objMsgBase.MessageType == MARSMessageType.e_Run_TestStep_Result)
                    {
                        currentMessageRequired = objMsgBase as MARSTestStep;
                        simpleLog.MarsLoggerSimple.Info("\t", "...... 3, currentMessageRequired = objMsgBase as MARSTestStep");
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                //simpleLog.MarsLoggerSimple.Error("\t", strError = "Errors while ");
                Console.WriteLine($"exception {e.Message}\r\n{e.StackTrace}");
            }
        }
#endif

    }

    [Serializable]
    public class MarsMessageDealBase : IMarsMessageAgent
    {
        private static System.Threading.Thread MessageSvcThread = null;
        private static bool IsSvcRunning = false;
        private bool IscontinueToRun = true;

        protected MARSTestStep TestStepInfo = new MARSTestStep();
        protected void InitTestStepInfo()
        {
            if (TestStepInfo == null)
            {
                TestStepInfo = new MARSTestStep();
            }
        }
        public MARSTestStep GetCurrentTestStepInfo()
        {
            //if (TestStepInfo == null)
            //{
            //    TestStepInfo = new MARSTestStep();
            //}
            return TestStepInfo;
        }
        protected bool AssignSendMessageQueue(ref string strError)
        {
            if (!MessageQueue.Exists(MarsMessageConst.UniqueMQSvrName() /*MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME*/))
            {
                MessageQueue.Create(MarsMessageConst.UniqueMQSvrName() /* MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME*/);
            }
            return false;
        }
        protected bool StartMarsMsmqListnerServices(ref string strError)
        {
            if (MessageQueue.Exists(MarsMessageConst.UniqueMQClnName()/*MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME*/)) //服务器端只监听客户的消息队列
            {
                try
                {
                    if (MessageSvcThread != null)
                    {
                        IsSvcRunning = false;
                        MessageSvcThread.Abort();
                    }
                }
                catch (Exception e)
                {
                    DoLog("StartMarsMsmqServices", "Error", strError = string.Format("Exception:[{0}]", e.Message), e);
                    //Logger.Error("StartMarsMsmqServices", strError = string.Format("Exception:[{0}]", e.Message), e);
                    //return false;
                }
                IsSvcRunning = true;
                MessageSvcThread = new System.Threading.Thread(new System.Threading.ThreadStart(RunMessageFetchingAndDealing));
                MessageSvcThread.Start();
            }
            return false;
        }

        private Queue<MARSMessagesBase> CurrentSendQueue = new Queue<MARSMessagesBase>();
        protected virtual void RunMessageFetchingAndDealing()
        {


            //while (IscontinueToRun)
            //{
            //    System.Threading.Monitor.Enter(threadMonitor);
            //    MARSMessagesBase objQueue = null;
            //    try
            //    {
            //        //
            //        while(CurrentSendQueue.Count>0)
            //        {
            //            MARSMessageSvc.
            //        }
            //    }
            //    finally
            //    {
            //        System.Threading.Monitor.Exit(threadMonitor);
            //    }
            //}
            return;
        }



        #region IMarsMessageAgent

        public bool DealWithMessage(out MARSDealResult objResult, int iWaitTime = 200)
        {
            objResult = new MARSDealResult();
            ///算法：
            /// 无须判断是否注射
            /// 1，直接向server队列发送消息
            /// 2，创建等待线程 知道线程超时或者返回数据
            /// 3，处理返回数据
            string strError = "", strAdv = "", strStack = "";
            MARSMessageSvcServer.SendMessageToMQ(this.TestStepInfo);
            ///then wait for client return 
            /// 
            bool isOk = false;
            MARSMessageSvcServer svcMessage = new MARSMessageSvcServer();
#if !MESSAGESVC_FROM_GUI
            
            if (this.TestStepInfo.Keyword.Equals(Utility.MarsConstants.CNST_KEYWORD_WAITUNTIL, StringComparison.OrdinalIgnoreCase))
            {
                MarsWaitUntilForProperty waitUntil = new MarsWaitUntilForProperty();
                if (waitUntil.setSourcePara(this.TestStepInfo.Parameters))
                {
                    iWaitTime = waitUntil.waitForSeconds;
                }                
            }
            objResult = svcMessage.WaitForReply(this.TestStepInfo, ref isOk, ref strError, ref strAdv, ref strStack,iWaitTime: iWaitTime);
#else
            string strTmpError = "";
            MARSTestStep tstStp = new MARSTestStep();
            //read result from mq directy
            Thread thrdMsmqRcv = new Thread(new ThreadStart(() =>
            {
            if (!MessageQueue.Exists(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME))
            {
                var mq = MessageQueue.Create(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME) ;
                    mq.SetPermissions("Everyone", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
                }
                MessageQueue msq = new MessageQueue(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
                bool isContinueGet = true;
                while (isContinueGet)
                {
                    try
                    {
                        Message oFromMSMQ = msq.Receive(new TimeSpan(0, 0, 10));
                        if (oFromMSMQ == null)
                        {
                            strTmpError = "not finished until 60 seconds";
                            isOk = false;
                            return;
                        }
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(oFromMSMQ.BodyStream);
                        isOk = false;
                        //string strStack = "", strAdv = "";
                        MARSMessagesBase objFromMQ = MARSMessagesBase.GetMsgObjViaRawXmlDoc(xmlDoc, ref isOk, ref strError,ref strAdv, ref strStack);
                        
                        tstStp = objFromMQ as MARSTestStep;
                        if (tstStp == null)
                        {
                            if (objFromMQ is MARSMessageHeartBeat)
                            {
                                //do it again 
                                Thread.Sleep(100);
                                continue;
                            }
                            strTmpError = "no resutl returns, after 60 seconds";
                            isOk = false;
                            return;
                        }
                        isOk = true;
                        break;

                    }catch(MessageQueueException me)
                    {
                        if (me.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                        {
                            strTmpError = "Time out, Normally, there is no such object exist,\r\nGet more information from MARS LOG or from \"object highlight\"";
                            isOk = false;
                            break;
                        }
                        strTmpError = string.Format("Exception:[{0}]", me.Message);
                        isOk = false;
                        break;
                    }
                    catch (Exception e)
                    {
                        strTmpError = string.Format("Exception:[{0}]", e.Message);
                        isOk = false;
                        break;
                    }
                }
            }
            ));
            thrdMsmqRcv.Start();
            thrdMsmqRcv.Join();
            if (!isOk)
            {
                strError = strTmpError;
                objResult.ErrorMessage = strError;
                objResult.ResultMessage = strError;
            }
            else
            {
                objResult.ReturnedData = tstStp.AttachInfo;
                objResult.ResultMessage = tstStp.TestResult == MARSStepResult.e_Result_Ok ? "OK" : "FAILED";
                objResult.ErrorMessage = tstStp.RuntimeResult;
                objResult.ErrorMessage = strError ;
                isOk = objResult.IsResultSucess;
            }
#endif
            return isOk;
        }

        public MARSMessagesBase peakMarsMessage(ref bool isOk, ref string strError)
        {
            return null;
        }
        public virtual void DoLog(string strMethodName, string strLevel, string strTextToLog, object extendForExcetpion)
        {
            return;
        }


        #endregion
    }

    [Serializable]
    public class MarsMessageKeywordOpObjectInfo : MarsMessageDealBase
    {
        public MarsMessageKeywordOpObjectInfo(string strKeyWord) : base()
        {
            this.TestStepInfo.Keyword = strKeyWord;
        }
        public MarsMessageKeywordOpObjectInfo() : base()
        {
            this.TestStepInfo.Keyword = "FillEdit";
        }

        public Dictionary<string, string> PegwindowObjIdentification
        {
            get
            {
                return this.TestStepInfo.TestStepObjectInformation.PegWindow.ObjectIDPropertiesAndValue.ConvertTo();
            }
            set
            {
                if (value == null)
                    this.TestStepInfo.TestStepObjectInformation.PegWindow.ObjectIDPropertiesAndValue.Clear();
                else
                    this.TestStepInfo.TestStepObjectInformation.PegWindow.ObjectIDPropertiesAndValue = MarsDictionary.ConvertFrom(value);
            }
        }

        public Dictionary<string, string> ObjectIdentification
        {
            get
            {
                return this.TestStepInfo.TestStepObjectInformation.TargetObject.ObjectIDPropertiesAndValue.ConvertTo();
            }
            set
            {
                if (value == null)
                    this.TestStepInfo.TestStepObjectInformation.TargetObject.ObjectIDPropertiesAndValue.Clear();
                else
                    this.TestStepInfo.TestStepObjectInformation.TargetObject.ObjectIDPropertiesAndValue = MarsDictionary.ConvertFrom(value);
            }
        }

        public string Parameter
        {
            get
            {
                return this.TestStepInfo.Parameters;
            }
            set
            {
                this.TestStepInfo.Parameters = value;
            }
        }

        public string PegName
        {
            get
            {
                return this.TestStepInfo.pegWindowName;
            }
            set
            {
                this.TestStepInfo.pegWindowName = value;
            }
        }

        public string objectName
        {
            get => this.TestStepInfo.pegWindowName;
            set => this.TestStepInfo.pegWindowName = value;
        }

        public string DataToOperate
        {
            get
            {
                return this.TestStepInfo.DataToSet;
            }
            set
            {
                this.TestStepInfo.DataToSet = value;
            }
        }

        public string AttachedInfo //额外信息
        {
            get
            {
                return this.TestStepInfo.AttachInfo;
            }
            set
            {
                this.TestStepInfo.AttachInfo = value;
            }
        }

        public long StepId
        {
            get
            {
                return this.TestStepInfo.RunId;
            }
            set
            {
                this.TestStepInfo.RunId = value;
            }
        }

        public int WaitingTime
        {
            get => this.TestStepInfo.WaitingTime;
            set => this.TestStepInfo.WaitingTime = value;
        }

        public string TestObjectType
        {
            get
            {
                return this.TestStepInfo.ObjectType;//like swfEdit, swfWindow
            }
            set
            {
                this.TestStepInfo.ObjectType = value;
            }
        }

        public DateTime AskTime
        {
            get { return this.TestStepInfo.AskTime; }
            set { this.TestStepInfo.AskTime = value; }
        }

        public DateTime AckTime
        {
            get { return this.TestStepInfo.AckTime; }
            set { this.TestStepInfo.AckTime = value; }
        }

        
        public MarsErrorCheckData objErrorCheckInfo
        {
            get { return this.TestStepInfo.errorCheckObj; }
            set { this.TestStepInfo.errorCheckObj = value; }
        }

        public bool DealWithMsgViaRESTFulAPI(ref string strError, out MARSDealResult objResult, int iWaitTime=300)
        {
            bool isOk = false;
            var clientInst = MARSRESTfulClientAPIMgr.GetInst(ref isOk, ref strError);
            if ((!isOk) || (clientInst==null))
            {
                simpleLog.MarsLoggerSimple.Error("DealWithMsgViaRESTFulAPI", $"can't get restful api swap file correctly with error|{strError}|");
                objResult = null;
                return false;
            }
            else {
                simpleLog.MarsLoggerSimple.Info("DealWithMsgViaRESTFulAPI", $"get restful api swap file correctly with port|{clientInst.RESTfulPort}|");
            }
            if (clientInst != null)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MARSTestStep));
                string strContent = "";
                using (StringWriter stringWriter = new StringWriter())
                {
                    serializer.Serialize(stringWriter, this.TestStepInfo);
                    strContent = stringWriter.ToString();
                }
                var responseFromApi = clientInst.RESTfulApiExecuteStep(strContent, iWaitTime, ref isOk, ref strError);
                if ((responseFromApi == null) || (!isOk))
                {
                    simpleLog.MarsLoggerSimple.Error("DealWithMsgViaRESTFulAPI", $"can't execute the step, with error|{strError}");
                    objResult = null;
                    return false;
                }

                simpleLog.MarsLoggerSimple.Info("DealWithMsgViaRESTFulAPI", responseFromApi);
                try
                {
                    serializer = new XmlSerializer(typeof(MARSDealResult));
                    using (StringReader strReader = new StringReader(responseFromApi))
                    {
                        objResult = (MARSDealResult)serializer.Deserialize(strReader);
                    }
                    return true;
                }
                catch (Exception e)
                {
                    objResult = null;
                    simpleLog.MarsLoggerSimple.Error("DealWithMsgViaRESTFulAPI", e.Message, e);
                    return false;
                }
            }
            else
            {
                simpleLog.MarsLoggerSimple.Error("DealWithMsgViaRESTFulAPI", $"can't init http client");
                objResult = null;
                return false;
            }
        }
    }
}



