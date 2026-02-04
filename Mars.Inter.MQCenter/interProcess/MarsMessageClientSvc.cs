using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.AutoTestingDriver.message;
using Mars.message.AutoTestingDriver.SystemUtil.DataStructure;
using Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData;
using Mars.message.Inter.MQCenter.cfg;
using Mars.message.Inter.MQCenter.HttpRestService;
using Mars.message.Inter.MQCenter.interProcess.marsTcpClient;
using Mars.message.Inter.MQCenter.keywordOperation;
using Mars.message.Inter.MQCenter.MarsObjectIdentifier;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Inter.MQCenter.standardControl;
using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;
using Mars.message.windowsWrapper.SystemUtil;
using MarsEngineInProcess.SourceCode.xmlConfig;
using MarsUFTAddins.IMars.tiger;
//using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Messaging;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;
using System.ComponentModel;
using System.IO;
using Mars.message.Utility;
using Mars.AutoTestingDriver.ExecuteTestcase;
using Mars.Inter.MQCenter.ThirdPartComponent.Infragistics;
using Mars.message.Inter.MQCenter.objectTypeMapping;
using Mars.Inter.MQCenter.objectEngine;
using Mars.Inter.MQCenter.windowsControlsHelpers;
using System.Data.SqlClient;
using Mars.Inter.MQCenter.interProcess.HostedFramework;
using Mars.Inter.MQCenter.interProcess.FrameworkOp;

namespace Mars.message.Inter.MQCenter.interProcess
{
    public class MarsWaitUntilForProperty
    {
        protected const string CNST_PARA_FORMAT = "^\\S+:\\d+:.*";

        public string sourcePara { get; set; }
        public string propertyToCheck { get; set; }
        public string patternToMatch { get; set; }
        public int waitForSeconds { get; set; }

        public bool isRightPara { get; set; } = false; 
        public bool setSourcePara(string strSource)
        {
            if (string.IsNullOrEmpty(strSource)) return false;
            bool isOk = MarsWindowsAPIsExtend.RegularTest(CNST_PARA_FORMAT, strSource);
            if (!isOk) return false;
            isRightPara = true;
            int iFirstComma = strSource.IndexOf(':');
            propertyToCheck = strSource.Substring(0, iFirstComma);
            int iSecondsComma = strSource.IndexOf(':', iFirstComma + 1);
            string waitSeconds = strSource.Substring(iFirstComma + 1, iSecondsComma - iFirstComma - 1);
            int iwaitForSeconds = -1;
            if (!int.TryParse(waitSeconds, out iwaitForSeconds))
            {
                return false;
            }
            waitForSeconds = iwaitForSeconds;
            patternToMatch = strSource.Substring(iSecondsComma + 1);
            return true;
        }
    }


    public class MarsWaitUntil_ColInfo_Table
    {
        public string preFix { get; set; }
        public string colName { get; set; }
        /// <summary>
        /// like colName:abc
        /// </summary>
        /// <param name="colInfo"></param>
        /// <returns></returns>
        public static MarsWaitUntil_ColInfo_Table getInstance(string colInfo)
        {
            if (string.IsNullOrEmpty(colInfo)) return null;
            int iFirstPos = colInfo.IndexOf(":");
            if (iFirstPos < 0) return null;
            string strPreFix = colInfo.Substring(0, iFirstPos);
            string strColName = colInfo.Substring(iFirstPos + 1);
            if (string.IsNullOrEmpty(strPreFix) || string.IsNullOrEmpty(strColName)) return null;
            return new MarsWaitUntil_ColInfo_Table()
            {
                preFix = strPreFix,
                colName = strColName
            };
        }
    }

    /// <summary>
    /// ("+ColName:columnName+@"(=|<>){1}\w+\s+\w+:(ANYROW|[0-9]{1,}) 等号后面的一部分
    /// 包括：
    /// </summary>
    public class MarsWaitUntil_Data_Table
    {
        public string rowNumInfo { get; set; }
        public string colInfo { get; set; }
        public string compareInfo { get; set; }
        /// <summary>
        /// colName:[columnName]
        /// </summary>
        /// <param name="strData"></param>
        /// <returns></returns>
        public static MarsWaitUntil_Data_Table getInstance(string strData)
        {
            if (string.IsNullOrEmpty(strData)) return null;
            int iPos = strData.LastIndexOf(":");
            if (iPos <=0 ) return null;
            string strrowNumInfo = strData.Substring(iPos+1);
            return new MarsWaitUntil_Data_Table()
            {
                rowNumInfo = strData.Substring(iPos + 1),
                compareInfo = strData.Substring(0, iPos)
            };
        }

        

        public bool isAnyRow()
        {
            return string.Compare(MarsWaitUntil.cnst_anyRow, rowNumInfo, true) == 0;
        }
        public int getRowId(ref bool isOk, ref string strError)
        {
            if (isAnyRow())
            {
                isOk = true;
                return -1;
            }
            int iRowId = -2;
            if (int.TryParse(rowNumInfo, out iRowId))
            {
                isOk = true;
                return -iRowId;
            }
            strError = "Row information is not a number ";
            isOk = false;
            return -2;
        }
    }
    

    public class MarsWaitUntil
    {
        public const string cnst_frmt_waitUntil = @"((TabCount|RowCount){1}(<|>=|<=|>|=)[0-9]{1,}(\.[0-9]{0,}){0,})|(TabName=.*)|(ColName:.*(=|<>|>){1}.*:(ANYROW|[0-9]{1,}))";
        private const string cnst_colName = "ColName";
        public const string cnst_anyRow = "ANYROW";
        private const string cnst_data_na = "N/A";

        public static string NormalizationDataFromTable(string strTmpCellText)
        {
            if ((strTmpCellText.StartsWith("179") && strTmpCellText.Length > 20)
                            || (strTmpCellText.Equals("-1.79769313486232E+308", StringComparison.OrdinalIgnoreCase)))
            {
                return "";
            }
            return strTmpCellText;
        }
        public static MarsWaitUntil getInstance(string WaitUntilPara)
        {
            if (string.IsNullOrEmpty(WaitUntilPara)) return null;
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(cnst_frmt_waitUntil);
            var m = r.Match(WaitUntilPara);
            if (!m.Success)
            {
                simpleLog.MarsLoggerSimple.Error("MarsWaitUntil.getInstance", $"Can't match [{cnst_frmt_waitUntil}] --[{WaitUntilPara}]");
                return null;
            }

            string[] s = m.Value.Split(new string[] {"<>",">=", "<=", "<", ">", "=" },StringSplitOptions.RemoveEmptyEntries);

            if (s.Length != 2)
            {
                simpleLog.MarsLoggerSimple.Error("MarsWaitUntil.getInstance", $"after splite, there are [{s.Length}] ["+string.Join(",",s)+"]");
                return null;
            }

            MarsWaitUntil w = new MarsWaitUntil();
            w.waitType = s[0];
            w.valueToCom = s[1];
            if (string.IsNullOrEmpty(w.waitType) || string.IsNullOrEmpty(w.valueToCom))
            {
                simpleLog.MarsLoggerSimple.Error("MarsWaitUntil.getInstance", $"[{w.waitType}] or [{w.valueToCom}] is empty");
                return null;
            }
            w.op = m.Value.Replace(s[0], "").Replace(s[1], "") ;
            if (w.waitType.StartsWith("ColName"))
            {
                w.tableColInfo = MarsWaitUntil_ColInfo_Table.getInstance(w.waitType);
                if (w.tableColInfo==null) return null;                
            }
            return w;
        }

        public bool isToOperateTable()
        {
            return tableColInfo==null?(string.Compare(cnst_colName, waitType, true)==0||string.Compare("RowCount", waitType,true)==0):
                string.Compare(cnst_colName, tableColInfo.preFix, true)==0;
        }

        public bool initTableInfo(ref string strError)
        {
            if (string.Compare(cnst_colName, tableColInfo==null?this.waitType: tableColInfo.preFix, true) != 0)
            {
                strError = $"current object should be colname, but it is :[{this.waitType}]";
                return false;
            }
            tableDataInfo = MarsWaitUntil_Data_Table.getInstance(this.valueToCom);
            if (tableDataInfo == null)
            {
                return false;
            }
            tableDataInfo.colInfo = tableColInfo == null ? this.waitType : tableColInfo.colName;
            return true;
        }
        private bool isValueBlank(string strDataFromGrid)
        {
            if (string.IsNullOrEmpty(strDataFromGrid)) return true;
            double d=0;
            if (double.TryParse(strDataFromGrid, out d))
            {
                if (d <= 0.0000001) return true;
                return false;
            }
            else return false;
        }
        public bool IsMatch(string strDataFromGrid)
        {
            if (tableDataInfo == null) return false;
            bool isok = false;
            try
            {
                simpleLog.MarsLoggerSimple.Info("IsMatch", $" -- dataFromGrid:[{strDataFromGrid}] [{op}] [{tableDataInfo.compareInfo}]");
                bool isNullForsetting = string.Compare(cnst_data_na, tableDataInfo.compareInfo, true) == 0;
                //MarsWindowsAPIsExtend.RegularTest r = new MarsWindowsAPIsExtend.RegularTest
                switch (op)
                {
                    case "<>":

                        // 判断是否为空 N/A,即，数据不是N/A
                        if (isNullForsetting)
                        {
                            simpleLog.MarsLoggerSimple.Info("\t", "isNullForsetting");
                            if (string.IsNullOrEmpty(strDataFromGrid)) return isok = false;
                            /// waiting if the value is 0.00
                            /// 
                            //if (isValueBlank(strDataFromGrid)) return isok=false;
                            //if (string.IsNullOrEmpty(strDataFromGrid)) return false;
                            return isok=true;
                        }
                        /// 非NA
                        /// 
                        if (string.Compare(tableDataInfo.compareInfo, strDataFromGrid, true) != 0) return isok=true;
                        return isok = false;
                    case "=":
                        if (isNullForsetting)
                        {
                            if (string.IsNullOrEmpty(strDataFromGrid)) return isok = true;
                            return isok = false;
                        }
                        if ((string.Compare(tableDataInfo.compareInfo, strDataFromGrid, true) == 0) || (MarsWindowsAPIsExtend.RegularTest(tableDataInfo.compareInfo
                            , strDataFromGrid))) return isok = true;
                        return isok = false;
                    case ">":
                        if (isNullForsetting)
                        {
                            if (string.IsNullOrEmpty(strDataFromGrid)) return isok = false;
                            return isok = true;
                        }
                        return string.Compare(strDataFromGrid, tableDataInfo.compareInfo, true) > 0;

                    case ">=":
                        if (isNullForsetting)
                        {
                            if (string.IsNullOrEmpty(strDataFromGrid)) return isok = false;
                            return isok = true;
                        }
                        return string.Compare(strDataFromGrid, tableDataInfo.compareInfo, true) >= 0;
                    case "<":
                        if (isNullForsetting)
                        {
                            return isok = false;
                        }
                        return string.Compare(strDataFromGrid, tableDataInfo.compareInfo, true) < 0;

                    case "<=":
                        if (isNullForsetting)
                        {
                            return isok = false;
                        }
                        return string.Compare(strDataFromGrid, tableDataInfo.compareInfo, true) <= 0;

                    default:
                        return isok = false;

                }
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("IsMatch", $"returns [{isok}]");
            }
        }

        public string waitType;
        public string op;
        public string valueToCom;
        public MarsWaitUntil_Data_Table tableDataInfo = null;
        public MarsWaitUntil_ColInfo_Table tableColInfo = null;
    }


    internal delegate bool MarsKeywordAppSideOperation(string strParaMeter, string strData, string strobjType,
        string strAttachInfo,
        string strPegName,
        string strObjName,
        Dictionary<string, string> objProperties,
        Dictionary<string, string> objPegProperties,
        MarsErrorCheckData errorCheckObj,
        ref string strError,
        ref string strDataReturn,
        ref string strStack,
        ref string strAdv, //advice
        ref string strSnapshotForShouldBeFile,
        bool isInnerCall = false, 
        int iWaitingTime =-1);
    public class MarsMessageClientSvc
    {

        private const int VISILBE_TOLORENCE_CNT = 20;

        static MessageQueue ServerReadQ;
        static MessageQueue ClientWriteQ;

        private static MarsMessageClientSvc SingleInst = null;
        private static Guid SessionId = default(Guid);

        private string currentUserAccount = "";

        private MarsMessageClientSvc()
        {
            ///创建消息队列 并且启动线程监听
            /// 
            Init();
        }

        internal static bool WaitForVisibleWithTolarenceCount(System.Windows.Forms.Control c)
        {
            if (c == null) return false;
            IntPtr lPtr;
            for (int i = 0; i < VISILBE_TOLORENCE_CNT; i++)
            {
                MarsWindowsAPIs.SendMessageTimeout(
                            //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)c), ((System.Windows.Forms.Control)c).Handle),
                            ((System.Windows.Forms.Control)c).Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            MarsWindowsAPIs.SMTO_NORMAL,
                            1000,
                            out lPtr);
                if (windowsWrapper.SystemUtil.MarsWindowsAPIs.IsWindowVisible(c.Handle))
                {
                    return true;
                }
                else
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
            return false;
        }

        public static bool WaitForPropertyInTime(object o, string strProp, string strValue, int iWaitTime)
        {
            long lstrt = DateTime.Now.Ticks;
            long ln = DateTime.Now.Ticks;
            bool isSame = false;
            while (((ln - lstrt) / TimeSpan.TicksPerSecond < iWaitTime) && (!isSame))
            {
                object ov = ReflectorForCSharp.GetMember(o, strProp);
                if (ov == null)
                {
                    Thread.Sleep(50);
                    ln = DateTime.Now.Ticks;
                    continue;
                }
                if (string.Compare(ov.ToString(), strValue, true) == 0)
                {
                    isSame = true;
                    return true;
                }
                Thread.Sleep(50);
                ln = DateTime.Now.Ticks;
                continue;
            }
            return false;
        }

        private static void InitLog()
        {

            WriteEvntlg("MarsEvent", "InitLog begin");
            //System.Diagnostics.EventLog.WriteEntry("MarsEvent", "InitLog begin");
            string strLogConfig = typeof(MarsMessageClientSvc).Assembly.Location;
            string strLogConfigPth = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(strLogConfig), "Mars.exe.config");
            //MLogger.LogFileName = System.IO.Path.Combine(System.IO.Directory.GetDirectoryRoot(strLogConfig), @"\log\MarsMessage.log");
            //System.Diagnostics.EventLog.WriteEntry("MarsEvent", string.Format("Config:[{0}], targetFile:[{1}]", strLogConfigPth, MLogger.LogFileName));
            //MLogger.LogFileCofigName = strLogConfigPth;
            //Logger = MLogger.GetLogger(typeof(MarsMessageClientSvc));            
        }

        private string GetCurrentMarsAccountName(ref bool isOk, ref string strError)
        {
            try
            {
                string strPath = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
                string strCurrentWindowSystemAccount = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                strCurrentWindowSystemAccount = strCurrentWindowSystemAccount.Replace("\\", "_");
                strCurrentWindowSystemAccount = strCurrentWindowSystemAccount.Replace("/", "_");
                string userAccountNameFile = System.IO.Path.Combine(strPath, $"MarsCrntAccount_{strCurrentWindowSystemAccount}.txt");
                Console.WriteLine($"swap file name:{userAccountNameFile}");
                if (!System.IO.File.Exists(userAccountNameFile))
                {
                    isOk = false;
                    strError = $"no such file exists :{userAccountNameFile}";
                    return null;
                }
                string txt = System.IO.File.ReadAllText(userAccountNameFile);
                Console.WriteLine($"read all rows:{txt}");
                /// 按行分割
                ///
                if (string.IsNullOrEmpty(txt))
                {
                    isOk = false;
                    strError = "Account file content is empty";
                    return null;
                }
                string[] arrTXt = txt.Split('\r', '\n');
                if (arrTXt.Length <= 0)
                {
                    isOk = false;
                    strError = "Account file content is empty, no one row";
                    return null;
                }

                isOk = true;
                Console.WriteLine($"before return:{arrTXt[0]}");
                return arrTXt[0];
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                return null;
            }

        }

        private void Init()
        {
            //((MLogger)Logger).logBegin("Init");
            try
            {
                UserCfgMgr usrMgr = new UserCfgMgr();
                //string UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                //UserName = UserName.Replace("\\", "_");
                
                string strPath = System.IO.Path.GetDirectoryName(typeof(MarsMessageClientSvc).Assembly.Location);
                string strCurrentWindowSystemAccount = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                strCurrentWindowSystemAccount = strCurrentWindowSystemAccount.Replace("\\", "_");
                strCurrentWindowSystemAccount = strCurrentWindowSystemAccount.Replace("/", "_");
                string userAccountNameFile = System.IO.Path.Combine(strPath, $"MarsCrntAccount_{strCurrentWindowSystemAccount}.txt");
                //usrMgr.LoadFromFile(userAccountNameFile);
                
                bool isOk = false;
                string strError = "";
                currentUserAccount = GetCurrentMarsAccountName(ref isOk,ref strError);

                WriteEvntlg("MarsEvent", $"get logname File:{userAccountNameFile} and mars account name:[{currentUserAccount}] and error is:[{strError}]");
                string strAttachMsmqName = string.IsNullOrEmpty(currentUserAccount) ? "" : $"_{currentUserAccount}";
                string strSvrQueneName = MarsMessageConst.UniqueMQSvrName();// $"{MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME}{strAttachMsmqName}";
                string strClntQueneName = MarsMessageConst.UniqueMQClnName(); // $"{MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME}{strAttachMsmqName}";
                if (ServerReadQ == null)
                {
                    if (MessageQueue.Exists(strSvrQueneName))
                    {
                        ServerReadQ = new MessageQueue(strSvrQueneName);
                        ServerReadQ.Purge();
                        ServerReadQ.SetPermissions("Everyone", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
                        WriteEvntlg("MarsEvent", string.Format("Message Queue [{0}] is attached", strSvrQueneName));
                    }
                    else
                    {
                        ServerReadQ = MessageQueue.Create(strSvrQueneName);
                        ServerReadQ.Purge();
                        ServerReadQ.SetPermissions("Everyone", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
                        WriteEvntlg("MarsEvent", string.Format("Message Queue [{0}] is attached", string.Format("Message Queue [{0}] is created", strSvrQueneName)));
                    }
                }
                ServerReadQ.Formatter = new XmlMessageFormatter();
                if (ClientWriteQ == null)
                {
                    //if (MessageQueue.Exists(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME))
                    if (MessageQueue.Exists(strClntQueneName))
                    {
                        //ClientWriteQ = new MessageQueue(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
                        ClientWriteQ = new MessageQueue(strClntQueneName);
                        ClientWriteQ.SetPermissions("Everyone", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
                        //WriteEvntlg("MarsEvent", string.Format("Message Queue [{0}] is attached", MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME));
                        WriteEvntlg("MarsEvent", string.Format("Message Queue [{0}] is attached", strClntQueneName));
                        //((MLogger)Logger).Info("Init", string.Format("Message Queue [{0}] is Attached", MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME));
                        //System.Diagnostics.EventLog.WriteEntry("MarsEvent", string.Format("Message Queue [{0}] is attached", MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME));
                    }
                    else
                    {
                        //ClientWriteQ = MessageQueue.Create(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
                        ClientWriteQ = MessageQueue.Create(strClntQueneName);
                        ClientWriteQ.SetPermissions("Everyone", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
                        //((MLogger)Logger).Info("Init", string.Format("Message Queue [{0}] is created", MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME));
                        WriteEvntlg("MarsEvent", string.Format("Message Queue [{0}] is created", strClntQueneName));
                        //System.Diagnostics.EventLog.WriteEntry("MarsEvent", string.Format("Message Queue [{0}] is created", MarsMessageConst.MESSAGE_SERVICE_QUEUE_NAME));
                    }
                    ClientWriteQ.Formatter = new XmlMessageFormatter(); //new BinaryMessageFormatter();
                    //BinaryMessageFormatter msgfrmt = new BinaryMessageFormatter();
                    XmlMessageFormatter msgfrmt = new XmlMessageFormatter();
                    msgfrmt.TargetTypes = new Type[] { typeof(MARSMessageHeartBeat) };
                    System.Messaging.Message msg = new System.Messaging.Message(new MARSMessageHeartBeat() { MessageType = MARSMessageType.e_Get_SessionId }, msgfrmt);
                    ClientWriteQ.Send(msg);
                }
            }
            catch (Exception e)
            {
                //((MLogger)Logger).Error("Init",string.Format("Exception:[{0}]",e.Message),e);
            }
            finally
            {
                //((MLogger)Logger).logEnd("Init");
            }

            //StartMonitorThread();
        }

        public static void Mars_AssemblyResolveInstall()
        {

            System.AppDomain.CurrentDomain.AssemblyResolve += Mars_AssemblyResolve;
            try
            {
                // WriteEvntlg("MarsEvent", "Mars_AssemblyResolve installed");
#if !_ForClickOnce
                //System.Diagnostics.EventLog.WriteEntry("MarsEvent", "Mars_AssemblyResolve installed");
#else
                MarsLoggerSimple.Info("Mars_AssemblyResolveInstall", "installed");
#endif
            }
            catch (Exception e)
            {
#if !_ForClickOnce
                //System.Diagnostics.EventLog.WriteEntry("MarsEvent", string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
#else
                MarsLoggerSimple.Error("Mars_AssemblyResolveInstall", e.Message, e.StackTrace);
#endif
            }

            //System.Diagnostics.EventLog.WriteEntry("MarsEvent", "Mars_AssemblyResolve installed");
        }

        public static void Mars_AppExitEventHandleInstall()
        {
            System.Windows.Forms.Application.ApplicationExit += Mars_AgentOnApplicationExitHandle;
        }

        private static void Mars_AgentOnApplicationExitHandle(object sender, EventArgs e)
        {
            if (MonitorThread == null) return;
            try
            {
                IsGoing = false;
                MonitorThread.Abort();
            }
            catch (Exception)
            {
                IsGoing = false;
            }
            MonitorThread = null;
        }

        private static void WriteEvntlg(string strSoure, string msg, System.Diagnostics.EventLogEntryType t = System.Diagnostics.EventLogEntryType.Information)
        {
#if !_ForClickOnce
            try
            {
                //using (System.Diagnostics.EventLog ev = new System.Diagnostics.EventLog("Application"))
                //{
                //    if (!EventLog.SourceExists(strSoure))
                //    {
                //        EventLog.CreateEventSource(strSoure, "Application");

                //    }
                //    ev.Source = strSoure;
                //    ev.WriteEntry(msg, t);
                //}
                MarsLoggerSimple.Info(strSoure, msg);
            }
            finally
            {
            }
#else
            MarsLoggerSimple.Info(strSoure, msg);
#endif
        }
        public static Assembly Mars_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Ignore missing resources
            if (args.Name.Contains(".resources"))
                return null;

            // check for assemblies already loaded
#if _NET4
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
#else
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = null;
            foreach (var itm in assemblies)
            {
                if (string.Compare(itm.FullName, args.Name) == 0)
                {
                    assembly = itm;
                    break;
                }
            }
#endif
            if (assembly != null)
            {
                WriteEvntlg("MarsEvent", "Mars_AssemblyResolve find assembly");
                //System.Diagnostics.EventLog.WriteEntry("MarsEvent", "Mars_AssemblyResolve find assembly");
                return assembly;
            }
            string strPath = System.IO.Path.GetDirectoryName(typeof(MarsMessageClientSvc).Assembly.Location);
            //string strFileName = @"C:\automationTest\Automation Workbooks\dlls\MarsInterMQCenter.dll";
            string strFileName = System.IO.Path.Combine(strPath, "MarsInterMQCenter.dll");
            try
            {
                WriteEvntlg("MarsEvent", string.Format("before load [{0}]", strFileName));
                return System.Reflection.Assembly.LoadFrom(strFileName);

            }
            catch (Exception e)
            {
                WriteEvntlg("MarsEvent", string.Format("Exeption:[{0}] \r\n{1}", e.Message, e.StackTrace));
                return null;

            }

        }

        private static Thread MonitorThread = null;
        private static bool IsGoing = true;

        /// <summary>
        /// 消息队列模式，因为在RESTful模式中，使用了normal参数，因此 需要构建一参数
        /// </summary>
        public static void StartMonitorThread(string fakePara="Normal")
        {

            //if (Logger==null)
            //{
            //    InitLog();
            //}
            if (SingleInst == null)
            {
                SingleInst = new MarsMessageClientSvc();
            }


            WriteEvntlg("MarsEvent", "StartMonitorThread begins");
            try
            {
                MarsLoggerSimple.Info("StartMonitorThread", "Begins");
            }catch(Exception e)
            {
                MessageBox.Show($"\t{e.InnerException.Message}\r\n\t{e.InnerException.StackTrace}\r\n{e.StackTrace}");
                throw e;
            }
            //((MLogger)Logger).logBegin("StartMonitorThread");
            //System.Diagnostics.EventLog.WriteEntry("MarsEvent", "StartMonitorThread begins");
            try
            {
                MARSMessagesBase objDumpMessage = new MARSMessagesBase();

                if (MonitorThread != null)
                {
                    IsGoing = false;
                    MonitorThread.Abort();
                }
                MonitorThread = new Thread(new ThreadStart(MonitorMarsQueue));
                MonitorThread.IsBackground = true;
                MonitorThread.SetApartmentState(ApartmentState.STA);
                MonitorThread.Start();
            }
            catch (Exception e)
            {

                WriteEvntlg("MarsEvent", string.Format("Exception:[{0}] \r\nstack:[{1}]", e.Message, e.StackTrace));
                MarsLoggerSimple.Error("StartMonitorThread", e.Message, e.StackTrace);
                //if (Logger!=null)
                //    ((MLogger)Logger).Error("StartMonitorThread",string.Format("Exception:[{0}]", e.Message),e );
            }
            finally
            {
                //if (Logger != null)
                //    ((MLogger)Logger).logEnd("StartMonitorThread");
            }
        }

        public static MARSMessagesBase GetMsgObjViaRawXmlDoc(XmlDocument sourcXml, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            return MARSMessagesBase.GetMsgObjViaRawXmlDoc(sourcXml, ref isOk, ref strError, ref strAdv, ref strStack);
            #region /// code transferred to MARSMessagesBase
            /*
            isOk = false;
            if (sourcXml == null)
            {
                strError = "Xml object is null";
                return null;
            }
            XmlSerializer serializer = null;

            if (string.Compare(sourcXml.DocumentElement.Name, "MARSTestStep",true)==0)
            {
                serializer = new XmlSerializer(typeof(MARSTestStep));
                try
                {
                    XmlReader xr = new XmlNodeReader(sourcXml);
                    isOk = true;
                    return (MARSTestStep)serializer.Deserialize(xr);
                }
                catch (Exception e)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("MARSTestStep convert from Message Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                    return null;
                }                
            }

            if (string.Compare(sourcXml.DocumentElement.Name, "MARSMessageHeartBeat",true)==0)
            {
                serializer = new XmlSerializer(typeof(MARSMessageHeartBeat));
                try
                {
                    XmlReader xr = new XmlNodeReader(sourcXml);
                    isOk = true;
                    return (MARSMessageHeartBeat)serializer.Deserialize(xr);
                }
                catch (Exception e)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("MARSMessageHeartBeat Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                    return null;
                }
            }
            
            isOk = false;
            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Unsupported type:[{0}]", sourcXml.DocumentElement.Name), sourcXml.InnerXml);
            return null;
            */
            #endregion
        }

        private static void MonitorMarsQueue()
        {
            WriteEvntlg("MarsEvent", string.Format("Wait for message:"));            

            while (IsGoing)
            {
                string strError = "",
                    strStack = "";
                string strAdv = "";
                string strSnapshotForShouldBeFile = "";

                bool isOk = false;
                try
                {

                    //System.Messaging.Message objMsg = ServerReadQ.Receive(TimeSpan.FromMilliseconds(200));
                    System.Messaging.Message objMsg = ServerReadQ.Receive();
                    //ServerReadQ.BeginReceive();
                    //ServerReadQ.re
                    MarsLoggerSimple.Info("MonitorMarsQueue", string.Format("a message format:[{0}] machineName:[{1}] - [{2}]", objMsg.Formatter, ServerReadQ.MachineName, ServerReadQ.QueueName));
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(objMsg.BodyStream);
                    simpleLog.MarsLoggerSimple.Info("\t", xmlDoc.InnerXml);
                    MARSMessagesBase objFromMQ = GetMsgObjViaRawXmlDoc(xmlDoc, ref isOk, ref strError, ref strAdv, ref strStack);

                    if (!isOk)
                    {
                        WriteEvntlg("MarsEvent", string.Format("Error from GetMsgObjViaRawXmlDoc -[{0}]", strError));
                        Thread.Sleep(100);
                        continue;
                    }

                    if ((objFromMQ == null) || (!isOk))
                    {
                        Thread.Sleep(500);
                        continue;
                    }
                    MarsLoggerSimple.Info("\t", string.Format("Session Id from server:[{0}] ", objFromMQ.SessiongId));
                    if (objFromMQ is MARSTestStep)
                    {
                        MARSTestStep objStp = (MARSTestStep)objFromMQ;
                        MarsLoggerSimple.Info("\t", $"---get ErrorChckObject|{objStp.errorCheckObj}|");
                        string strDataReturn = "";

                        if (string.Compare("WaitUntil", objStp.Keyword??"", true) == 0)
                        {
                            if (objStp.WaitingTime<=0)
                                objStp.WaitingTime = ClientDealWithGUIKeyword.cnst_defaultWaitUntil_seconds;
                        }

                        isOk = ClientDealWithGUIKeyword.DealKeywordByKeywordName(objStp.Keyword, objStp.Parameters, objStp.DataToSet,
                            objStp.ObjectType,
                            objStp.TestStepObjectInformation.TargetObject.ObjectIDPropertiesAndValue.ConvertTo(),
                            objStp.TestStepObjectInformation.PegWindow.ObjectIDPropertiesAndValue.ConvertTo(),
                            objStp.AttachInfo,
                            objStp.pegWindowName,
                            objStp.objectName,
                            objStp.WaitingTime,
                            objStp.errorCheckObj,
                            ref strError,
                            ref strDataReturn,
                            ref strStack,
                            ref strAdv,
                            ref strSnapshotForShouldBeFile);
                        objStp.AckTime = DateTime.Now;
                        objStp.MessageType = MARSMessageType.e_Run_TestStep_Result;
                        objStp.snapshotFileNameWhenErrorOccurs = strSnapshotForShouldBeFile;
                        if (isOk)
                        {
                            MarsLoggerSimple.Info("MonitorMarsQueue", "Send message back Ok");
                            //将运行结果送回
                            //if ((string.Compare(objStp.Keyword, "Capturevalue", true) == 0) || (string.Compare(objStp.Keyword, "Captureandcompare", true) == 0))
                            //{
                            objStp.AttachInfo = strDataReturn;
                            //}
                            objStp.TestResult = MARSStepResult.e_Result_Ok;
                            objStp.RuntimeResult = "OK";
                            
                            // checking 
                        }
                        else
                        {
                            MarsLoggerSimple.Error("MonitorMarsQueue", string.Format("Send message back Wrong, with error:[{0}], stack:[{1}]|return:{2}",
                                strError, strStack, strDataReturn));
                            objStp.TestResult = MARSStepResult.e_Result_Failed;
                            objStp.RuntimeResult = strError;
                            objStp.stackTrace = strStack;
                            objStp.AttachInfo = strDataReturn;
                            objStp.advice2User = strAdv;
                        }
                        if (ClientWriteQ != null)
                        {
                            MarsLoggerSimple.Info("MonitorMarsQueue", string.Format("Send back information:[{0}]", objStp.ToString()));
                            ClientWriteQ.Formatter = new XmlMessageFormatter() { TargetTypes = new Type[] { objStp.GetType() } };//new BinaryMessageFormatter();

                            System.Messaging.Message objMsgToSendBack = new System.Messaging.Message(objStp, ClientWriteQ.Formatter);
                            ClientWriteQ.Send(objMsgToSendBack);
                        }
                    }
                    //MARSMessagesBase marsbaseMsg = objMsg.Body as MARSMessagesBase;
                    ////System.Diagnostics.EventLog.WriteEntry("MarsEvent", string.Format("Get Message [{0}]", marsbaseMsg.MessageType));
                    //MarsLoggerSimple.Info("MonitorMarsQueue", string.Format("Get Message [{0}]", marsbaseMsg.MessageType));

                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    if (e is TimeoutException) continue;
                    if (e.Message.Contains("Timeout"))
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
#if !_ForClickOnce
                    //System.Diagnostics.EventLog.WriteEntry("MarsEvent", string.Format("exception type:[{1}] Exception:{0} \r\n{2}", e.Message, e.GetType().ToString(), e.StackTrace), System.Diagnostics.EventLogEntryType.Error);
#endif
                    MarsLoggerSimple.Error("MonitorMarsQueue", e.Message, e.StackTrace);

                    Thread.Sleep(1000);
                }
            }
        }

    }


    public sealed class ClientDealWithGUIKeyword
    {
        public const string cnst_previewobject = "PREVIEWOBJECT";
        public const string cnst_selectListItem_defaultData_click_only = "_MARS_CLICK_ONLY";

        public const int cnst_defaultWaitUntil_seconds = 300;//5 minutes

        internal static TestStepErrorCheckSetting currentStepCheckError = null;
        private static TestStepErrorCheckSetting ConvertAttachToErrorCheck(string strAttach)
        {
            try
            {
                return currentStepCheckError = System.Text.Json.JsonSerializer.Deserialize<TestStepErrorCheckSetting>(strAttach);
            }catch(Exception e)
            {
                return currentStepCheckError = null;
            }
        }

        public static List<string> MarsStandardsControlType { get; set; } = new List<string> { "winEdit", "static", "winCombobox", "WinTab", "WinButton", "WinEditor", "winTable","winMATable" };
        private static bool IsStandardControlType(string strObjType)
        {
            if (string.IsNullOrEmpty(strObjType)) return false;
            foreach (string s in MarsStandardsControlType)
            {
                if (string.Compare(s, strObjType, true) == 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Date: 9/3/2024
        /// Reason: for nagetive testing. check a text or caption, create a new keyword, checkText 
        /// 
        /// </summary>
        internal static Dictionary<string, MarsKeywordAppSideOperation> AppSideKeywordOperation = new Dictionary<string, MarsKeywordAppSideOperation>()
        {
            { "ADDDAYS"                 , AppsideKeywordDeal_AddDays                },
            { "AUTOCHECKERROR"          , AppsideKeywordDeal_AutoCheckError         },
            { "CAPTUREVALUE"            , AppsideKeywordDeal_CaptureValue           },
            { "CAPTUREANDCOMPARE"       , AppsideKeywordDeal_CaptureAndCompare      },
            { "CAPTUREANDCOMPAREBYKEY"  , AppsideKeywordDeal_CaptureAndCompareByKey },
            { "CHECKERROR"              , AppsideKeywordDeal_CheckError             },
            { "CHECKTEXT"               , AppsideKeywordDeal_CheckText              },
            { "CLICKAT"                 , AppsideKeywordDeal_ClickAt                },
            { "CLICKBUTTON"             , AppsideKeywordDeal_ClickButton            },
            { "CLICKMENUICON"           , AppsideKeywordDeal_ClickMenuIcon          },
            { "CLICKPOPUPMENUITEM"      , AppsideKeywordDeal_ClickPopupMenuItem     },
            { "CLICKRADIOBUTTON"        , AppsideKeywordDeal_ClickRadioButton       },
            { "CLOSEWINDOW"             , AppsideKeywordDeal_CloseWindow            },
            { "DISMISS"                 , AppsideKeywordDeal_Dismiss                },
            { "FILLEDIT"                , AppsideKeywordDeal_FillEdit               },
            { "FILLTABLE"               , AppsideKeywordDeal_FillTable              },
            { "HIGHLIGHT"               , AppsideKeywordDeal_HIGHLIGHT              },
            { "INSERTROW"               , AppsideKeywordDeal_InsertRow              },
            { "LAUNCHAPPLICATION"       , AppsideKeywordDeal_LaunchApplication      },
            { "MAXIMIZEWINDOW"          , AppsideKeywordDeal_MarximizeWindow        },
            { "PEGWINDOW"               , AppsideKeywordDeal_Pegwindow              },
            { "PRESSKEYS"               , AppsideKeywordDeal_PressKey               },
            { cnst_previewobject        , AppsideKeywordDeal_PreviewObject          },
            { "SEARCHANDCLICK"          , AppsideKeywordDeal_SearchAndClick         },
            { "SEARCHANDUPDATE"         , AppsideKeywordDeal_SearchAndUpdate        },
            { "SELECTLISTITEM"          , AppsideKeywordDeal_SelectListItem         },
            { "SELECTDROPDOWN"          , AppsideKeywordDeal_SelectDropDown         },
            { "SELECTMENUITEM"          , AppsideKeywordDeal_SelectMenuItem         },
            { "SELECTTAB"               , AppsideKeywordDeal_SelectTab              },
            { "SETBOX"                  , AppsideKeywordDeal_SetBox                 },
            { "SETSPLITTER"             , AppsideKeywordDeal_SetSplitter            },
            { "SNAPSHOT"                , AppsideKeywordDeal_SnapShot               },
            { "SCROLLDOWN"              , AppsideKeywordDeal_ScrollDown             },
            { "SCROLLLEFT"              , AppsideKeywordDeal_ScrollLeft             },
            { "SCROLLGRIDTOLEFT"        , AppsideKeywordDeal_ScrollLeft             },
            { "SCROLLRIGHT"             , AppsideKeywordDeal_ScrollRigt             },
            { "SCROLLGRIDTORIGHT"       , AppsideKeywordDeal_ScrollRigt             },
            { "SCROLLUP"                , AppsideKeywordDeal_ScrollUp               },
            { "SCROLLWINDOW"            , AppsideKeywordDeal_ScrollWindow           },
            { "VERIFYVALUE"             , AppsideKeywordDeal_VerifyValue            },
            { "WAITUNTIL"               , AppsideKeywordDeal_WaitUntil              },

            {"_STARTOBJECTSPY"          , AppsideKeywordDeal_StartObjectSpy         },
            {"_RELOADKEYWORD_TYPE_MAP"  , AppsideKeywordDeal_ReloadKeyword_type_Map },
        };

        private const string cnst_para_visible = "VISIBLEIGNORE";
        private const string cnst_para_showPath = "SHOWPATH";

        private class FillEditParaDeal
        {
            const string cnst_sub = "^(sub|SubStr|Substring){1}";
            internal static string DealWithPara(string strParaMeter, string strData, ref bool isOk,
                ref string strError,
                ref string strAdv,
                ref string strStack)
            {
                isOk = true;
                if (strParaMeter == null) return strData;

                if (MarsWindowsAPIsExtend.RegularTest(cnst_sub, strParaMeter))
                {
                    int idx = strParaMeter.IndexOf(":");
                    if (idx < 0)
                    {
                        strError = "Incorrect format for Substring of FillEdit Keyword";
                        strStack = "format should be:^(sub|SubStr|Substring){1}\r\n" + MarsErrorStacks.StackTraceDump();
                        strAdv = "See user manual for Keyword FillEdit";
                        isOk = false;
                        return strData;
                    }
                    string strSubAfterFunc = strParaMeter.Substring(idx + 1);
                    string[] arrPos = strSubAfterFunc.Split(':');
                    int iStrt = idx + 1,
                        iEnd = strData.Length;
                    if (arrPos.Length >= 1)
                    {
                        if (!int.TryParse(arrPos[0].Trim(), out iStrt))
                        {
                            strError = "Incorrect format for Substring of FillEdit Keyword"; // string.Format("two numbers should be after substring pre fix, but it is:[{0}]", arrPos[0]);
                            StackFrame stck = (new StackFrame());
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "See user manual for Keyword FillEdit";
                            isOk = false;
                            return strData;
                        }
                        iEnd = iEnd - iStrt;
                        if (arrPos.Length >= 2)
                        {
                            if (!int.TryParse(arrPos[1].Trim(), out iEnd))
                            {
                                strError = "Incorrect format for Substring of FillEdit Keyword"; //string.Format("two numbers should be after substring pre fix, but second is:[{0}]", arrPos[1]);
                                StackFrame stck = (new StackFrame());
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "See user manual for Keyword FillEdit";
                                isOk = false;
                                return strData;
                            }

                        }
                        if ((iStrt >= strData.Length) || (iEnd >= strData.Length) || (iStrt < 0))
                        {
                            return strData;
                        }
                        return strData.Substring(iStrt, iEnd);
                    }
                }

                isOk = true;
                strError = "Incorrect format for fillEdit Keyword";// string.Format("Unsupported format:[{0}]", strError);
                StackFrame stck1 = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "See user manual for Keyword FillEdit";
                return strData;
            }
        }

        static List<IntPtr> WndHandleBelongToCurProcess = new List<IntPtr>();
        private static List<Form> GetOpenFormsByEnum(IntPtr hdl, long pId, ref string strError)
        {
            List<Form> lstFrm = new List<Form>();
            WndHandleBelongToCurProcess.Clear();
            MarsWindowsAPIs.SearchData sd = new MarsWindowsAPIs.SearchData
            {
                hWnd = new IntPtr(pId)
            };
            List<IntPtr> lstWndHdls = MarsWindowsAPIsExtend.GetWindows((int)pId);
            foreach (var itm in lstWndHdls)
            {
                WndHandleBelongToCurProcess.Add(itm);
            }
            //MarsWindowsAPIs.EnumWindows(new MarsWindowsAPIs.EnumWindowsProcSearch(EnumProc), ref sd);

            MarsLoggerSimple.Info("GetOpenFormsByEnum", string.Format("find total [{0}] windows", WndHandleBelongToCurProcess.Count));
            //判断windows是不是都是form
            foreach (IntPtr itm in WndHandleBelongToCurProcess)
            {
                if (itm == IntPtr.Zero) continue;
                Control c = Control.FromHandle(itm);
                if (c == null)
                {
                    MarsLoggerSimple.Warnning("GetOpenFormsByEnum", String.Format("Can't convert handle to control:[{0}]", itm));
                    continue;
                }
                if (c is Form)
                {
                    Form tmpFrm = c as Form;

                    MarsLoggerSimple.Info("\t", string.Format("Get forms for types:[{0}] - handle:[{1}]", ReflectorForCSharp.GetObjectBaseType(c.GetType()), itm));
                    int iWaitCount = 0;
                    bool isRequireSleep = false;
                    MarsWindowsAPIs.RECT rect = default(MarsWindowsAPIs.RECT); ;
                    while ((iWaitCount++) < 100)
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                        {

                            MarsWindowsAPIs.GetWindowRect(tmpFrm.Handle, out rect);
                            if ((!tmpFrm.CanFocus) || (rect.Right - rect.Left < 1))
                            {
                                isRequireSleep = true;
                            }
                            else
                            {
                                isRequireSleep = false;
                            }
                        }));
                        if (isRequireSleep)
                        {
                            MarsLoggerSimple.Info("\t", string.Format("requiresleep can focus:[{0}] rectangle [{1},{2},{3},{4}]", tmpFrm.CanFocus, rect.Left,
                                rect.Right, rect.Top, rect.Bottom));
                            try
                            {
                                Thread.Sleep(1000);
                            }
                            catch (Exception e)
                            {

                            }
                        }
                        if (iWaitCount % 10 == 0)
                        {
                            MarsLoggerSimple.Info("\t", string.Format("sleep times:[{0}]", iWaitCount / 10));
                        }
                    }
                    lstFrm.Add(c as Form);
                }
                else
                {
                    MarsLoggerSimple.Info("GetOpenFormsByEnum", string.Format("Not a form,[{0}]", ReflectorForCSharp.GetObjectBaseType(c.GetType())));
                }
            }

            Dictionary<IntPtr, List<IntPtr>> dictParentAndItsChildWnd = new Dictionary<IntPtr, List<IntPtr>>();
            for (int i = 0; i < lstFrm.Count; i++)
            {
                if (lstFrm[i] == null) continue;
                List<IntPtr> lstChildWnds = MarsWindowsAPIsExtend.GetChildWindows(lstFrm[i].Handle);
                dictParentAndItsChildWnd.Add(lstFrm[i].Handle, lstChildWnds);
                MarsLoggerSimple.Info("\t", string.Format("child count:[{0}]", lstChildWnds == null ? 0 : lstChildWnds.Count));
            }
            return lstFrm;
        }

        //private static bool EnumProc(IntPtr hwnd, ref MarsWindowsAPIs.SearchData data)
        //{
        //    int pId = 0                                                              ;
        //    MarsWindowsAPIs.GetWindowThreadProcessId(hwnd, out pId)                  ;
        //    IntPtr p_id   = new IntPtr(pId)                                          ; 
        //    if (p_id      == data.hWnd)
        //    {
        //        WndHandleBelongToCurProcess.Add(hwnd)                                ;
        //    }
        //    return true                                                              ;
        //}

        public static List<Form> GetOpenFormsAsList()
        {
            FormCollection allFormsFromApp = Application.OpenForms;
            List<Form> allForms = new List<Form>();
            for (int i = 0; i < allFormsFromApp.Count; i++)
            {
                if (allFormsFromApp[i].IsDisposed) continue;
                if (allFormsFromApp[i].Disposing) continue;
                allForms.Add(allFormsFromApp[i]);
            }
            return allForms;
        }
        /// <summary>
        /// 2021-03-22 增加：
        ///   某些
        /// </summary>
        /// <param name="strTypeName"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="targetForm"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="iWaitSeconds"></param>
        /// <returns></returns>
        //[STAThread]
        private static MarsformIndentifier ReGetForm(string strTypeName, Dictionary<string, string> objPegProperties,
            string strPegName, string strObjName,
            ref object targetForm,
            ref bool isOk,
            ref string strError,
            ref string strStack,
            ref string strAdv, //advice
            ref string strSnapshotForShouldBeFile,
            int iWaitSeconds = -1
            )
        {
            MarsLoggerSimple.logBegin("ReGetForm", $"iWaitSeconds:[{iWaitSeconds}]");
            try
            {
                ///算法：
                /// 1，判断进程是否繁忙
                /// 2，获得当前进程的form列表，判断form是否满足条件，如果没有找到满足条件的，继续等待循环到超时
                /// 3，判断是否是唯一的form，否则返回错误信息
                System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();
                UIPermission uIPermission = new UIPermission(UIPermissionWindow.AllWindows);

                if (objCurP == null)
                {
                    isOk = false;
                    strError = "Passing null object to a function";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }
                simpleLog.MarsLoggerSimple.Info("ReGetForm", "1");
                IntPtr lpdwResult;

                //WaitUntilCurrentProcessIsNotBusy(3);

                /// 1，判断进程是否繁忙
                MarsWindowsAPIs.SendMessageTimeout(//new HandleRef(objCurP,objCurP.MainWindowHandle),
                    objCurP.MainWindowHandle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_NORMAL,
                    (uint)(iWaitSeconds <0?12000: iWaitSeconds), //2分钟
                    out lpdwResult);

                Process p = objCurP; //Process.GetCurrentProcess();
                /// 2，获得当前进程的form列表，判断form是否满足条件，如果没有找到满足条件的，继续等待循环到超时
                //System.Windows.Forms.FormCollection allForms = System.Windows.Forms.Application.OpenForms;
                List<Form> allForms = null;// GetOpenFormsAsList();
                long tmMarkGetFrm = DateTime.Now.Ticks;
                bool isFrmContinue = true;
                while (isFrmContinue)
                {
                    allForms = GetOpenFormsAsList();

                    if (allForms.Count == 0)
                    {
                        if (p != null)
                        {
                            //MarsWindowsAPIsExtend.HideWindowFromTaskbar(p.MainWindowHandle);
                            //MarsWindowsAPIsExtend.ShowWindowInTaskbar(p.MainWindowHandle);
                            //Thread.Sleep(1000);
                            //MarsWindowsAPIs.SendMessageTimeout(p.MainWindowHandle, 0,
                            //    IntPtr.Zero,
                            //    IntPtr.Zero,
                            //    MarsWindowsAPIs.SMTO_BLOCK,
                            //    120000, //2分钟
                            //    out lpdwResult);
                            //allForms = System.Windows.Forms.Application.OpenForms;
                            allForms = GetOpenFormsAsList();
                            if ((allForms == null) || (allForms.Count == 0))
                            {
                                MarsLoggerSimple.Info("ReGetForm", "try to use GetOpenFormsByEnum to initial all forms");
                                allForms = GetOpenFormsByEnum(p.MainWindowHandle, p.Id, ref strError);

                                Control tmpFrmFromHdl = Control.FromHandle(p.MainWindowHandle);
                                MarsLoggerSimple.Info("ReGetForm", string.Format("Try to get form from handle [{0}] and type return:[{1}]-All types:[{2}] main form parent:[{3}]", p.MainWindowHandle, tmpFrmFromHdl == null ? null : tmpFrmFromHdl.GetType().ToString(),
                                    ReflectorForCSharp.GetObjectBaseType(tmpFrmFromHdl.GetType()),
                                    tmpFrmFromHdl.Parent == null ? null : tmpFrmFromHdl.Parent.GetType()
                                    ));
                                //tmpFrmFromHdl.GetType().BaseType.ToString()
                                /**这段代码会crash目标应用
                                 * 
                                Form tmpFrm = tmpFrmFromHdl as Form;
                                if (tmpFrm != null)
                                {
                                    tmpFrm.ShowInTaskbar = !tmpFrm.ShowInTaskbar;
                                    MarsLoggerSimple.Info("ReGetForm", string.Format("Get OpenFormCount:[{0}]", Application.OpenForms.Count));
                                    Thread.Sleep(2000);
                                    //tmpFrm.ShowInTaskbar = !tmpFrm.ShowInTaskbar;
                                    MarsLoggerSimple.Info("ReGetForm", string.Format("Get OpenFormCount:[{0}] again", Application.OpenForms.Count));
                                }
                                **/
                            }
                        }
                        //Application.
                    }
                    long markFrmN = DateTime.Now.Ticks;
                    isFrmContinue = ((markFrmN - tmMarkGetFrm)/TimeSpan.TicksPerMillisecond) < iWaitSeconds;
                    if (isFrmContinue)
                        Thread.Sleep(100);
                }
                List<object> lstFrm = new List<object>();
                bool isContinueToGet = true, isControlVisible = false, isOkTmp = false;
                string strErrorTmp = "",
                    strAdvTmp = "", strStackTmp = "";
                long dt = DateTime.Now.Ticks;
                List<MarsformIndentifier> lstWindows = new List<MarsformIndentifier>();

                while (isContinueToGet)
                {
                    Mars.message.Inter.MQCenter.MarsObjectsOperations.MarsObjectOpBase.WaitUntilCurrentProcessIsNotBusy(1);

                    int i = 0;
                    lstWindows.Clear();
#if !_ForClickOnce
                    //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() => {
                    //System.Diagnostics.EventLog.WriteEntry("MarsEvent", string.Format("curent domain name:[{0}]", AppDomain.CurrentDomain.FriendlyName));
                    //allForms = System.Windows.Forms.Application.OpenForms; //可能有form打开或者关闭
#endif
                    allForms = GetOpenFormsAsList();
                    if (allForms.Count == 0)
                    {
                        allForms = GetOpenFormsByEnum(p.MainWindowHandle, p.Id, ref strError);
                        //MarsWindowsAPIsExtend.ShowWindowInTaskbar()
                    }
#if !dotNet2
                    simpleLog.MarsLoggerSimple.Info("ReGetForm", string.Format("2, Get FormCount:[{0}], application type:[{1}]", allForms == null ? 0 : allForms.Count,
                        System.Windows.Application.Current == null ? "NULL" : System.Windows.Application.Current.GetType().ToString()));
#endif
                    #region navigate all forms
                    while (i < allForms.Count)
                    {
                        try
                        {
                            //System.Windows.Forms.Form itm = System.Windows.Forms.Application.OpenForms[i];
                            System.Windows.Forms.Form itm = allForms[i];
                            
                            MarsLoggerSimple.Info("ReGetForm", string.Format("Current form type:[{0}], count:[{1}]-current:[{2}]", itm.GetType().ToString(), System.Windows.Forms.Application.OpenForms.Count, i));
                            //这里，如果form在进行更新，将导致系统崩溃，或者block
                            //应该使用dispatch？
                            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                            {
                                //判断系统是否response
                            }));
                            MarsformIndentifier objMarsWithProperties = MarsformIndentifier.FetchPegwindowInformation(itm, objPegProperties, strPegName, strObjName, ref isOkTmp, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);

                            //下面这句在form loading数据时候会crash,应该是itm.Bounds获取不够安全
                            //MarsLoggerSimple.Info("ReGetForm", string.Format("FetchPegwindowInformation returns [{0}]],size:[{1}]", isOkTmp, itm.Bounds));
                            MarsWindowsAPIs.RECT rect;
                            if (itm.IsDisposed) continue;
                            MarsWindowsAPIs.GetWindowRect(itm.Handle, out rect);
                            MarsLoggerSimple.Info("ReGetForm", string.Format("FetchPegwindowInformation returns [{0}]],size:[{1}]", isOkTmp,
                                $"rectangle is {rect.Left}, {rect.Top}, {rect.Right}, {rect.Bottom}"));
                            if (!isOkTmp)
                            {
                                //i++;
                                continue;
                            }
                            //simpleLog.MarsLoggerSimple.Info("\t", "itm.IsHandleCreated begin");
                            //isOk=WaitForControlPropertyEquals(itm, "IsHandleCreated", true, 5000, ref strError);
                            //simpleLog.MarsLoggerSimple.Info("\t", $"itm.IsHandleCreated end with [{isOk}]");
                            //isOk = WaitForControlPropertyEquals(itm, "Visible", true,5000, ref strError);

                            //if (!(isControlVisible = MarsWindowsAPIs.IsWindowVisible(itm.Handle)))
                            //if (!WaitForControlVisible(itm, 20000, ref strError))
                            if (itm.Parent != null)
                            {
                                MarsWindowsAPIs.SendMessageTimeout(itm.Parent.Handle,
                                (int)WM.PAINT,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_NORMAL, //SMTO_BLOCK,
                                2000, //2秒
                                out lpdwResult);
                            }
                            simpleLog.MarsLoggerSimple.Info("\t", $"after parent paint, count[{i}-{allForms.Count}]");
                            MarsWindowsAPIs.SendMessageTimeout(itm.Handle,
                                (int)WM.PAINT,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_NORMAL, //SMTO_BLOCK,
                                2000, //2秒
                                out lpdwResult);
                            itm.Update();
                            simpleLog.MarsLoggerSimple.Info("\t", "after self paint");
                            Thread.Sleep(100);
                            if (!WaitForControlVisible(itm, 2000, ref strError))
                            {
                                MarsLoggerSimple.Info("\t", $"Find but not visible, itm visible:{itm.Visible}-name:{itm.Name}");
                                //strInfo += string.Format("{0} notVisible, MainWindow [{1}] index:[{2}];", itm == null ? "NULL" : itm.GetType().ToString(), itm.Handle == mainHwnd, i);
                                objMarsWithProperties.IsControlVisible = false;
                            }
                            else
                            {
                                objMarsWithProperties.IsControlVisible = true;
                            }
                            objMarsWithProperties.AssignedForm = itm;
                            lstWindows.Add(objMarsWithProperties);
                        }
                        catch (Exception e)
                        {
                            MarsLoggerSimple.Error("ReGetForm", strErrorTmp = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                            continue;
                        }
                        finally
                        {
                            i += 1;
                        }

                    }
                    #endregion navigate all forms
                    //}));

                    //因为有可能是系统在启动，需要等待一段时间
                    Thread.Sleep(50); //等待半秒
                    MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_NORMAL, //SMTO_BLOCK,
                    2000, //2秒
                    out lpdwResult);


                    #region check How many forms left and their's visiblities.
                    lstWindows.RemoveAll(px => !px.IsControlVisible);
                    //simpleLog.MarsLoggerSimple.Info("\t", $"continue is {isContinueToGet}, n-{n}, lstWindowCount:[{lstWindows.Count}]");


                    if (lstWindows.Count == 1)
                    {
                        //找到唯一窗口
                        isOk = true;
                        ///等待 windows已经visible and enabled
                        if (MarsWindowsAPIs.GetForegroundWindow() != lstWindows[0].WindowHandle)
                            MarsWindowsAPIs.SetForegroundWindow(lstWindows[0].WindowHandle);
                        return lstWindows[0];
                    }

                    long n = DateTime.Now.Ticks;
                    n = (n - dt)/TimeSpan.TicksPerMillisecond;
                    isContinueToGet = iWaitSeconds<=0?n<=12000:n<iWaitSeconds; //2分钟,max

                    //if (!isContinueToGet) break;
                    if (lstWindows.Count == 0)
                    {
                        System.Threading.Thread.Sleep(1000);
                        if (isContinueToGet) continue;
                        //isContinueToGet = true;
                        //simpleLog.MarsLoggerSimple.Info("\t", "to continue");
                        break;
                        //continue;
                    }

                    #endregion //check How many forms left and their's visiblities.

                    #region check Index                
#if _NET4
                    var strIdx = objPegProperties.Keys.Where(px => string.Compare("index", px, true) == 0).FirstOrDefault();
#else
                    string strIdx = "";
                    foreach (var itm in objPegProperties.Keys)
                    {
                        if (string.Compare("index", itm, true) == 0)
                        {
                            strIdx = itm;
                            break;
                        }
                    }
#endif
                    if (!string.IsNullOrEmpty(strIdx))
                    {
                        MarsLoggerSimple.Info("ReGetForm", string.Format("----Check Index----, list count {0} vs index:[{1}]", lstWindows == null ? 0 : lstWindows.Count, objPegProperties[strIdx]));
                        int iIdx = -1;
                        if (!int.TryParse(objPegProperties[strIdx].Trim(), out iIdx))
                        {
                            iIdx = 0;
                        }
                        iIdx = iIdx < 0 ? 0 : iIdx;
                        if (iIdx >= lstWindows.Count)
                            iIdx = 0;
                        if (lstWindows.Count > 1)
                        {
                            IntPtr hdl = MarsWindowsAPIs.GetForegroundWindow();
                            if (iIdx > 0)
                            {
                                int iZOrd = 0;
                                while ((iZOrd < iIdx) && (hdl != IntPtr.Zero))
                                {
                                    hdl = MarsWindowsAPIs.GetWindow(hdl, MarsWindowsAPIs.GetWindowType.GW_HWNDNEXT);
                                    iZOrd++;
                                }
                            }
                            //process的当前窗口
                            for (int ii = 0; ii < lstWindows.Count; ii++)
                            {
                                if (lstWindows[ii].WindowHandle == hdl)
                                {
                                    isOk = true;
                                    return lstWindows[ii];
                                }
                            }
                            isOk = false;
                            strError = string.Format("No such window handle [{0}] can be found in window list", hdl);
                            StackFrame stck = (new StackFrame());
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            return null;

                        }
                        return null;
                        /* old code
                        if (MarsWindowsAPIs.GetForegroundWindow() != lstWindows[iIdx].WindowHandle)
                            MarsWindowsAPIs.SetForegroundWindow(lstWindows[iIdx].WindowHandle);
                        isOk = true;
                        return lstWindows[iIdx];

                        */
                    }
                    #endregion //check Index

                    if (MarsWindowsAPIs.GetForegroundWindow() != objCurP.MainWindowHandle)
                        MarsWindowsAPIs.SetForegroundWindow(objCurP.MainWindowHandle);
                    //MarsLoggerSimple.Info("\t", string.Format("caused [{0}] for searching and waiting for window", n));
                }

                if (lstWindows.Count == 0)
                {
                    var windowsHdlLst = MarsWindowsAPIsExtend.EnumerateProcessWindowHandles(Process.GetCurrentProcess().Id);
                    List<IntPtr> unFindHandlers = new List<IntPtr>();
                    // 判断是否所有的handle都在
                    for (int i = 0; i < (windowsHdlLst == null ? -1 : windowsHdlLst.Count); i++)
                    {
                        try
                        {
                            if (!allForms.Any(pp => ((pp != null) && (pp.Handle == windowsHdlLst[i]))))
                            {
                                unFindHandlers.Add(windowsHdlLst[i]);
                            }
                        }catch(Exception e)
                        {

                        }
                    }
                    for (int i = 0; i < unFindHandlers.Count; i++)
                    {
                        try
                        {
                            System.Windows.Forms.Control tmpC = System.Windows.Forms.Control.FromHandle(unFindHandlers[i]);
                            if (tmpC == null) continue;
                            var tmpFrm = MarsformIndentifier.FetchPegwindowInformation(tmpC, objPegProperties, strPegName,
                                strObjName,
                                ref isOk,
                                ref strError,
                                ref strAdv,
                                ref strStack);
                            if ((isOk) && (tmpFrm != null))
                            {
                                lstWindows.Add(tmpFrm);
                            }
                        }
                        catch (Exception e)
                        {

                        }
                    }
                    if (lstWindows.Count == 1)
                    {
                        isOk = true;
                        ///等待 windows已经visible and enabled
                        if (MarsWindowsAPIs.GetForegroundWindow() != lstWindows[0].WindowHandle)
                            MarsWindowsAPIs.SetForegroundWindow(lstWindows[0].WindowHandle);
                        return lstWindows[0];
                    }
                }
                isOk = false;
                if (lstWindows.Count > 1)
                {
                    strError = string.Format("Multiple controls found for control [{0}] count:[{1}]", MarsWindowsAPIsExtend.Dic2String(objPegProperties), lstWindows.Count);
                    //strError      = string.Format("more than one controls are found. for [{0}] count:[{1}]", MarsWindowsAPIsExtend.Dic2String(objPegProperties), lstWindows.Count);
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = lstWindows.Count == 0 ? "object not found" : "Multiple objects with the same identifier were found." ;
                    //(new Snapshot()).CaptureRegion. 
                }
                else
                {
                    strError = $"Unable to locate the object [{strPegName}]";//string.Format("No such control is exist or visible, for [{0}]", MarsWindowsAPIsExtend.Dic2String(objPegProperties));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the peg window is present,and ensure window identification is correct.Use Object Spy to identify the problem";
                }
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("ReGetForm");
            }
        }

        private static bool AppsideKeywordDeal_ScrollDown(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            try
            {
                MarsLoggerSimple.logBegin("AppsideKeywordDeal_ScrollDown");
                MarsLoggerSimple.PreFix = "\t";
                return AppsideKeywordDeal_Scroll(strParaMeter, strData, strobjType, strAttachInfo, strPegName, strObjName, objProperties, objPegProperties,
                        ref strError, ref strDataReturn,
                        2, ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile);
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ScrollDown", strError = string.Format("Exception:[{0}] stackTrace:[{1}]",
                    e.Message,
                    e.StackTrace), e);
                StackFrame stck = (new StackFrame());
                strStack = e.StackTrace;
                strAdv = "Contact Marquis";
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_ScrollDown");
            }
        }
        private static bool AppsideKeywordDeal_ScrollLeft(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            try
            {
                MarsLoggerSimple.logBegin("AppsideKeywordDeal_ScrollLeft");
                MarsLoggerSimple.PreFix = "\t";
                return AppsideKeywordDeal_Scroll(strParaMeter, strData, strobjType, strAttachInfo, strPegName, strObjName, objProperties, objPegProperties,
                        ref strError, ref strDataReturn,
                        0,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile);
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ScrollLeft", strError = string.Format("Exception:[{0}] stackTrace:[{1}]",
                    e.Message,
                    e.StackTrace), e);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_ScrollLeft");
            }
        }
        private static bool AppsideKeywordDeal_ScrollRigt(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            try
            {
                MarsLoggerSimple.logBegin("AppsideKeywordDeal_ScrollRigt");
                MarsLoggerSimple.PreFix = "\t";
                return AppsideKeywordDeal_Scroll(strParaMeter, strData, strobjType, strAttachInfo, strPegName, strObjName, objProperties, objPegProperties,
                        ref strError, ref strDataReturn,
                       7,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile);
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ScrollRigt", strError = string.Format("Exception:[{0}] stackTrace:[{1}]",
                    e.Message,
                    e.StackTrace), e);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_ScrollRigt");
            }
        }

        private static bool AppsideKeywordDeal_ScrollUp(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            try
            {
                MarsLoggerSimple.logBegin("AppsideKeywordDeal_ScrollUp");
                MarsLoggerSimple.PreFix = "\t";
                return AppsideKeywordDeal_Scroll(strParaMeter, strData, strobjType, strAttachInfo, strPegName, strObjName, objProperties, objPegProperties,
                        ref strError, ref strDataReturn,
                        3,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile);
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ScrollUp", strError = string.Format("Exception:[{0}] stackTrace:[{1}]",
                    e.Message,
                    e.StackTrace), e);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_ScrollUp");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="iDirection"> 
        ///     0=Left
        ///     2=down
        ///     3=up
        ///     7=right
        ///     
        /// </param>
        /// <param name="isInnerCall"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_Scroll(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName, string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            ref string strError,
            ref string strDataReturn,
            int iDirection,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {

            MarsLoggerSimple.logBegin("AppsideKeywordDeal_Scroll", string.Format("Direction:[{3}] Parameter:[{0}] {1}.{2}",
                strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties),
                MarsWindowsAPIsExtend.Dic2String(objProperties),
                iDirection == 0 ? "LEFT" : (iDirection == 2 ? "DOWN" : (iDirection == 3 ? "UP" : "RIGHT"))
                ));
            MarsLoggerSimple.PreFix = "\t\t";
            try
            {
                bool isOk = false;
                List<object> lstControls = new List<object>();
                String strObjTypUpper = "";

                isOk = GetCurrentControlsFilteredByType(strObjTypUpper = strobjType.ToUpper(),
                    strPegName, strObjName,
                    objPegProperties, objProperties, lstControls,
                    ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile);
                if (!isOk)
                {
                    return false;
                }

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";// string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                if (!isOk) return false;

                int iCnt = -1;
                if (!int.TryParse(strParaMeter, out iCnt))
                    iCnt = 1;
                iCnt = iCnt <= 0 ? 1 : iCnt;
                IntPtr lpdwResult;

                bool isSupportedControl = MarsGridOperations.IsSupported(lstControls[0], ref strError);
                if (isSupportedControl)
                {
                    return (new MarsGridOperations()).ScrollGridByCommand(lstControls[0], strParaMeter, strData, ref strError, ref strAdv, ref strStack);
                }


                for (int i = 0; i < iCnt; i++)
                {
                    if ((iDirection == 0) || (iDirection == 7)) //left
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessage(((System.Windows.Forms.Control)lstControls[0]).Handle,
                            (int)windowsWrapper.SystemUtil.WM.HSCROLL, (int)iDirection, new IntPtr(0));
                    else if (iDirection == 2)
                    {
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessage(((System.Windows.Forms.Control)lstControls[0]).Handle,
                            (int)windowsWrapper.SystemUtil.WM.VSCROLL, (int)iDirection, new IntPtr(0));
                    }
                    if (!((System.Windows.Forms.Control)lstControls[0]).InvokeRequired)
                    {
                        MarsWindowsAPIs.SendMessageTimeout(
                            //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)lstControls[0]), ((System.Windows.Forms.Control)lstControls[0]).Handle),
                            ((System.Windows.Forms.Control)lstControls[0]).Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
                            1000,
                            out lpdwResult);
                    }
                    else
                    {
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                        ((System.Windows.Forms.Control)lstControls[0]).Invoke(
#endif
                            new Action(() =>
                            {
                                MarsWindowsAPIs.SendMessageTimeout(
                                //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)lstControls[0]), ((System.Windows.Forms.Control)lstControls[0]).Handle),
                                ((System.Windows.Forms.Control)lstControls[0]).Handle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
                                1000,
                                out lpdwResult);
                            }
                         )
                        );

                    }
                    Thread.Sleep(50);
                }
                return true;
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_Scroll", strError = string.Format("Exception:[{0}]", e.Message), e);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_Scroll");
            }
        }

        private static bool AppsideKeywordDeal_ScrollWindow(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_ScrollWindow", string.Format("Parameter:[{0}] {1}.{2}",
                strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties),
                MarsWindowsAPIsExtend.Dic2String(objProperties)
                ));
            MarsLoggerSimple.PreFix = "\t\t";
            try
            {
                bool isOk = false;
                List<object> lstControls = new List<object>();
                String strObjTypUpper = "";

                isOk = GetCurrentControlsFilteredByType(strObjTypUpper = strobjType.ToUpper(), strPegName, strObjName,
                    objPegProperties, objProperties, lstControls, ref strError, ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile);
                if (!isOk)
                {
                    return false;
                }

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                if (!isOk) return false;

                //check data format
                if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(@"(H|V){1}:(\d{1,}|M|T|B|L|R)$", strData))
                {
                    MarsLoggerSimple.Error("AppsideKeywordDeal_ScrollWindow", strError = string.Format("Data format should be [{0}] but it is :[{1}]", @"(H|V){1}:(\d{1,}|M|T|B)$", strData));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                //alyst format of command from data
                string[] arrData = strData.Split(':');
                if (arrData.Length != 2)
                {
                    MarsLoggerSimple.Error("AppsideKeywordDeal_ScrollWindow", strError = string.Format("Data format should be [{0}] but it is :[{1}]", @"(H|V){1}:(\d{1,}|M|T|B)$", strData));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                string strHOrV = arrData[0];
                string strPos = arrData[1];

                System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
                if (c == null)
                {
                    MarsLoggerSimple.Error("AppsideKeywordDeal_ScrollWindow", strError = string.Format("Control type is required, but it is:[{0}]", lstControls[0].GetType().ToString()));
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }

                string strTypes = ReflectorForCSharp.GetObjectBaseType(c.GetType());
                if (strTypes.ToUpper().Contains("Summit.Framework.Desktop.ApplicationLayout".ToUpper()))
                {
                    int Minimum = 0, Maximum = 0, value2set = 0;
                    bool isNotExist = false;
                    ReflectorForCSharp reflector = new ReflectorForCSharp();
                    object targetScrollBar = null;
                    if ((string.Compare("H", strHOrV, 0) == 0))
                    {
                        targetScrollBar = ReflectorForCSharp.GetMember(c, "HorizontalScroll", ref isNotExist);
                    }
                    else
                    {
                        targetScrollBar = ReflectorForCSharp.GetMember(c, "VerticalScroll", ref isNotExist);
                    }
                    if (isNotExist)
                    {
                        MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", string.Format("no HorizontalScroll exists in type [{0}]. different Infragistis version?", strTypes));
                        return true;
                    }
                    Maximum = reflector.GetMember<int>(targetScrollBar, "Maximum", ref isNotExist);
                    if (isNotExist)
                    {
                        MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", string.Format("no Maximum exists in type [{0}]. different Infragistis version?", targetScrollBar));
                        return true;
                    }
                    Minimum = reflector.GetMember<int>(targetScrollBar, "Minimum", ref isNotExist);
                    if (isNotExist)
                    {
                        MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", string.Format("no Minimum exists in type [{0}]. different Infragistis version?", targetScrollBar));
                        return true;
                    }
                    if ((string.Compare("T", strPos, true) == 0) || (string.Compare("L", strPos, true) == 0))
                        value2set = Minimum;
                    if ((string.Compare("R", strPos, true) == 0) || (string.Compare("B", strPos, true) == 0))
                        value2set = Maximum;
                    if (string.Compare("M", strPos, true) == 0)
                    {
                        value2set = (Minimum + Maximum) / 2;
                    }
                    isOk = reflector.SetMemberValue(value2set, targetScrollBar, "Value", ref strError, ref strStack);
                    if (!isOk)
                    {
                        strAdv = "Contact Marquis";
                    }
                    return isOk;
                }
                #region grid for Infragistics and others inheritated
                if (
                    /*(strTypes.ToUpper().Contains("Infragistics.Win.UltraWinGrid".ToUpper()))||*/
                    (strTypes.ToUpper().Contains("Summit.Framework.View.SerializedDataSpreadsheetControl".ToUpper())))
                {
                    bool isNotExist = false;
                    simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", "Infragistics Ultra Grid, before get ScrollRegion");
                    #region test code
                    //string strParent = "";
                    //Control ccp = c;
                    //for (int jj=0;jj<5;jj++)
                    //{
                    //    if (ccp.Parent!=null)
                    //    {
                    //        strParent = string.Format("{0};[{1}]-[{2}]", strParent, ccp.Parent.GetType(), ccp.Parent.Name);
                    //        ccp = ccp.Parent;
                    //    }
                    //    else
                    //    {

                    //        break;
                    //    }
                    //}
                    //simpleLog.MarsLoggerSimple.Info("-----parent and name ----", strParent);
                    #endregion

                    ReflectorForCSharp reflector = new ReflectorForCSharp();
                    object targetScrollBar = null;
                    string strPosIdx = "ScrollPosition";

                    var scrollBarFromControl = ReflectorForCSharp.GetMember(c, "VerticalScroll", ref isNotExist);
                    if (scrollBarFromControl != null)
                    {
                        MarsLoggerSimple.Info("------stupid----coll", "find bars");
                    }
                    else
                    {
                        MarsLoggerSimple.Info("------stupid----coll", "find bars no");
                    }

                    bool isHOrV = false;
                    if ((string.Compare("V", strHOrV, 0) == 0))
                    {
                        targetScrollBar = ReflectorForCSharp.GetMember(c, "ActiveRowScrollRegion", ref isNotExist);
                        isHOrV = true;
                    }
                    else
                    {
                        targetScrollBar = ReflectorForCSharp.GetMember(c, "ActiveColScrollRegion", ref isNotExist);
                        strPosIdx = "Position";
                    }

                    if (isNotExist)
                    {
                        MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", strError = string.Format("no scroll bars active scroll region (UltraWinGrid) exists in type [{0}]. different Infragistis version?", strTypes));
                        strError = "Object property [ActiveColScrollRegion] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    int iPos = 0;
                    int iMode = 0;//0==mouse, 1, then use old mode
                    int mouseCount = -1;
                    if (!int.TryParse(strPos, out mouseCount))
                    {
                        iMode = 1;
                        if (string.Compare("B", strPos, true) == 0)
                        {
                            iPos = 7;
                        }
                        else
                        {
                            iPos = 6;
                        }
                    }
                    else
                    {
                        MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", string.Format("mouse click mode, click count {0}", mouseCount));
                    }
                    if (iMode == 0)
                    {
                        Rectangle rect = c.RectangleToScreen(c.ClientRectangle);
                        int xOff = -12, yOff = -22;
                        if (!isHOrV)
                        {
                            xOff = -22;
                            yOff = -12;
                        }

                        for (int z = 0; z < mouseCount; z++)
                        {
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(rect.X + rect.Width + xOff, rect.Y + rect.Height + yOff);
                            Thread.Sleep(100);
                        }
                    }
                    else
                    {
#if !dotNet2
                        if (c.InvokeRequired)
                        {
                            isOk = true;
                            string strTmpError = "";
                            string strAdvTmp = "";
                            string strStackTmp = "";

                            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                            {
                                simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", "dispath set iPos = " + iPos + " before call scroll of targetScrollBar");
                                reflector.CallMethod(targetScrollBar, "Scroll", new object[] { iPos });

                                object oScrollPosition = ReflectorForCSharp.GetProperty(targetScrollBar, strPosIdx, ref isNotExist);
                                if (isNotExist)
                                {
                                    MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", string.Format("type:[{0}]\r\n\tAllproperties:{1}", targetScrollBar.GetType().ToString(),
                                        reflector.GetAllProperties(targetScrollBar)
                                        ));
                                    MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", strTmpError = string.Format("no {1} exists in type -ActiveRowScrollRegion- [{0}]. different Infragistis version?", strTypes, strPosIdx));
                                    StackFrame stck = (new StackFrame());
                                    strStackTmp = MarsErrorStacks.StackTraceDump();
                                    strAdvTmp = "";
                                    isOk = false;
                                    return;
                                }
                                isOk = reflector.SetProperty(targetScrollBar, strPosIdx, 100, ref strTmpError);
                                if (!isOk)
                                {
                                    MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", strTmpError = string.Format("SetMemberValue for targetScrollBar failed with error:[{0}]", strTmpError));
                                    isOk = false;
                                    StackFrame stck = (new StackFrame());
                                    strStackTmp = MarsErrorStacks.StackTraceDump();
                                    strAdvTmp = "";
                                    return;
                                }

                            }));
                            MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow dispatch", string.Format("{0}", reflector.GetAllProperties(targetScrollBar)));
                            strError = strTmpError;
                            strAdv = strAdvTmp;
                            strStack = strStackTmp;
                        }
                        else
#endif
                        {
                            simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", "set iPos = " + iPos + " before call scroll of targetScrollBar");
                            reflector.CallMethod(targetScrollBar, "Scroll", new object[] { iPos });

                            object oScrollPosition = ReflectorForCSharp.GetProperty(targetScrollBar, strPosIdx, ref isNotExist);
                            if (isNotExist)
                            {
                                MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", string.Format("type:[{0}]\r\n\tAllproperties:{1}", targetScrollBar.GetType().ToString(),
                                    reflector.GetAllProperties(targetScrollBar)
                                    ));
                                MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", strError = string.Format("no {1} exists in type -ActiveRowScrollRegion- [{0}]. different Infragistis version?", strTypes, strPosIdx));
                                StackFrame stck = (new StackFrame());
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "";
                                return false;
                            }

                            isOk = reflector.SetProperty(targetScrollBar, strPosIdx, 100, ref strError);
                            if (!isOk)
                            {
                                MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", strError = string.Format("SetMemberValue for targetScrollBar failed with error:[{0}]", strError));
                                StackFrame stck = (new StackFrame());
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "";
                                return false;
                            }
                            MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", string.Format("{0}", reflector.GetAllProperties(targetScrollBar)));
                        }
                    }

                    return true;
                }
                #endregion

                #region the codes below is used for normal windows

                windowsWrapper.SystemUtil.MarsWindowsAPIs.SCROLLINFO wndVScrl = new MarsWindowsAPIs.SCROLLINFO(),
                wndHScrl = new MarsWindowsAPIs.SCROLLINFO();
                wndHScrl.cbSize = (uint)Marshal.SizeOf(wndHScrl);
                wndHScrl.fMask = (uint)windowsWrapper.SystemUtil.MarsWindowsAPIs.ScrollInfoMask.SIF_RANGE;
                wndVScrl.cbSize = (uint)Marshal.SizeOf(wndVScrl);
                wndVScrl.fMask = (uint)windowsWrapper.SystemUtil.MarsWindowsAPIs.ScrollInfoMask.SIF_RANGE;

                bool isHAvailable = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetScrollInfo(c.Handle, (int)MarsWindowsAPIs.SBFlags.SB_HORZ, ref wndHScrl),
                    isVAvailable = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetScrollInfo(c.Handle, (int)MarsWindowsAPIs.SBFlags.SB_VERT, ref wndVScrl);

                if ((!isHAvailable) && (!isVAvailable))
                {
                    MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", "can't get H-scroll or V-scroll");
                    return true;
                }
                if ((string.Compare("H", strHOrV, 0) == 0))
                {
                    if (!isHAvailable)
                    {
                        MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", "No scroll bar-H exists, return true");
                        return true;
                    }
                    int iPos;
                    if (int.TryParse(strPos, out iPos))
                    {
                        iPos = iPos > wndHScrl.nMax ? wndHScrl.nMax - 1 : iPos;
                        iPos = iPos < wndHScrl.nMin ? wndHScrl.nMin : iPos;
                    }
                    else
                    {
                        if ((string.Compare("T", strPos, true) == 0) || (string.Compare("L", strPos, true) == 0))
                            iPos = wndHScrl.nMin;
                        if ((string.Compare("R", strPos, true) == 0) || (string.Compare("T", strPos, true) == 0))
                            iPos = wndHScrl.nMax;
                        if (string.Compare("M", strPos, true) == 0)
                        {
                            iPos = (wndHScrl.nMin + wndHScrl.nMax) / 2;
                        }
                    }
                    //
                    if (c.InvokeRequired)
                    {
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                        c.Invoke(
#endif
                        new Action(() =>
                        {
                            wndHScrl.nPos = iPos;
                            windowsWrapper.SystemUtil.MarsWindowsAPIs.SetScrollInfo(c.Handle,
                                (int)windowsWrapper.SystemUtil.MarsWindowsAPIs.SBOrientation.SB_HORZ,
                                ref wndHScrl, true);
                        }));
                    }
                    else
                    {
                        wndHScrl.nPos = iPos;
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SetScrollInfo(c.Handle,
                                (int)windowsWrapper.SystemUtil.MarsWindowsAPIs.SBOrientation.SB_HORZ,
                                ref wndHScrl, true);
                    }
                }
                else
                {
                    if (!isVAvailable)
                    {
                        MarsLoggerSimple.Info("AppsideKeywordDeal_ScrollWindow", "No scroll bar-V exists, return true");
                        return true;
                    }
                    int iPos;
                    if (int.TryParse(strPos, out iPos))
                    {
                        iPos = iPos > wndVScrl.nMax ? wndVScrl.nMax - 1 : iPos;
                        iPos = iPos < wndVScrl.nMin ? wndVScrl.nMin : iPos;
                    }
                    else
                    {
                        if ((string.Compare("T", strPos, true) == 0) || (string.Compare("L", strPos, true) == 0))
                            iPos = wndVScrl.nMin;
                        if ((string.Compare("R", strPos, true) == 0) || (string.Compare("T", strPos, true) == 0))
                            iPos = wndVScrl.nMax;
                        if (string.Compare("M", strPos, true) == 0)
                        {
                            iPos = (wndVScrl.nMin + wndVScrl.nMax) / 2;
                        }
                    }
                    if (c.InvokeRequired)
                    {
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                        {
#else
                        c.Invoke(new Action(() =>
                        {
#endif
                            wndVScrl.nPos = iPos;
                            windowsWrapper.SystemUtil.MarsWindowsAPIs.SetScrollInfo(c.Handle,
                                (int)windowsWrapper.SystemUtil.MarsWindowsAPIs.SBOrientation.SB_HORZ,
                                ref wndVScrl, true);
                        }));
                    }
                    else
                    {
                        wndVScrl.nPos = iPos;
                        windowsWrapper.SystemUtil.MarsWindowsAPIs.SetScrollInfo(c.Handle,
                                (int)windowsWrapper.SystemUtil.MarsWindowsAPIs.SBOrientation.SB_HORZ,
                                ref wndVScrl, true);
                    }
                }
                #endregion
                return true;
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ScrollWindow", strError = string.Format("Exception:{0}\r\nstackTracek:{1}", e.Message, e.StackTrace));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_ScrollWindow");
            }
        }

        /// <summary>
        /// this keyword is to refresh the extra object type
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_ReloadKeyword_type_Map(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime = -1)
        {
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_ReloadKeyword_type_Map", $"strPara:[{strParaMeter}] strData:[{strData}]");
            try
            {
                bool isOk = false;
                List<KeywordControlTypeMapping> keywordMap = KeywordControlTypeMappingMgmt.loadFromFile(ref strError, ref isOk);
                if (!isOk)
                {
                    /// just ignore
                    simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_ReloadKeyword_type_Map", $"no keyword/control_type mapping file, with error|{strError}|");
                    strError = "";
                    return false;
                }


            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ReloadKeyword_type_Map", strError = $"Wrong json format for starting object spy, Error:{e.Message}\r\n\t{strData}", e);
                strStack = e.StackTrace;
                strAdv = "Contact Marquis";
                return false;
            }

            return true;
        }
        /// <summary>
        /// 该keyword是启动objectspy，同时给host提供信息
        /// 当agent接收到该对象后，直接返回true，然后启动一个form，显示process的对象树
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData">對於该函数，该处是一个json对象</param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <returns>true        /// </returns>
        private static bool AppsideKeywordDeal_StartObjectSpy(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_StartObjectSpy", $"strPara:[{strParaMeter}] strData:[{strData}]");
            try
            {
                JavaScriptSerializer jsnMgr = new JavaScriptSerializer();
                MarsObjectSpyCommand spyCmmd = jsnMgr.Deserialize<MarsObjectSpyCommand>(strData);
                //MarsObjectSpyCommand spyCmmd=JsonSerializer.Deserialize<MarsObjectSpyCommand>(strData);
                //start spy window
                //
                if (MarsWinformSpy.cnst_applicationType_winform.Equals(spyCmmd.spyType, StringComparison.OrdinalIgnoreCase)) {
                    MarsWinformSpy objFromSpy = new MarsWinformSpy();
                    objFromSpy.LoadObjectsToSpyForm(spyCmmd);
                }
                else
                {

                }

            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_StartObjectSpy", strError = $"Wrong json format for starting object spy, Error:{e.Message}\r\n\t{strData}",e);
                strStack = e.StackTrace;
                strAdv = "Contact Marquis";
                return false;
            }
            
            return true;
        }
        /// <summary>
        /// 对比数据是否满足设置需要
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="errorCheckObj"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_VerifyValue(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            bool isOk = AppsideKeywordDeal_CaptureAndCompare(strParaMeter, strData, strobjType,
                strAttachInfo, strPegName, strObjName,
                objProperties, objPegProperties,
                errorCheckObj,
                ref strError,
                ref strDataReturn,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                isInnerCall);
            if (!isOk) return false;
            //由于存在tolerance 故而在这里不做比较，而在host中进行处理
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("verify value get:[{0}] source to compare:[{1}]", strDataReturn, strData));
            if (string.IsNullOrEmpty(strParaMeter))
            {

                if (string.Compare(strData, strDataReturn, true) == 0)
                {
                    return true;
                }
                else if (!string.IsNullOrEmpty(strData))
                {
                    /// 可能是正则表达式
                    /// 
                    try
                    {
                        if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strData, strDataReturn))
                        {
                            simpleLog.MarsLoggerSimple.Info("\t", $"{strData}|matches|{strDataReturn}, test passed");
                            isOk = true;
                        }
                        else
                        {
                            strError = $"{strData} doesn't matches |{strDataReturn}| what is captuerd from |{strObjName} |";
                            strAdv = "Please change the dataset settings based on the captured data and try again";
                            StackFrame stck = (new StackFrame());
                            strStack = MarsErrorStacks.StackTraceDump();
                            isOk = false;
                            return false;
                        }
                    }catch(Exception e)
                    {
                        /// 很可能是正则表达式错误
                        /// 
                        strError = $"CAN'T match |{strData}| to captured value |{strDataReturn}|, please check |{strData}| settings";
                        strAdv = "Please change and check the dataset settings based on the captured data and try again";
                        strStack = MarsErrorStacks.StackTraceDump();
                        isOk = false;
                        return false;
                    }
                }
                else
                {
                    strError = string.Format("Data required:[{1}] doesn't match returned:[{0}]", strDataReturn, strData);
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "please check the data set and try again";
                }
                return false;
            }
            return true;
            //if (string.Compare(strData, strDataReturn, true) == 0)
            //{
            //    return true;
            //}
            //else
            //    strError = string.Format("Data required:[{1}] doesn't match returned:[{0}]", strDataReturn, strData);
            //return false;
        }


        private static bool AppsideKeywordDeal_CaptureValue(string strParaMeter, string strData, string strobjType,
            string strAttachInfo,
            string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_SnapShot", $"{iMark}|SelectDropDown({strPegName}.{strObjName}, {strParaMeter}, {strData})|{strobjType}|{MarsWindowsAPIsExtend.Dic2String(objProperties)}");

            /// 需要支持标准的，如MFC的对象
            /// 
            if (IsStandardControlType(strobjType))
            {
                return MarsStandardMFCControlKeywordOp.FillEdit(strParaMeter, strData, strobjType,
                    strAttachInfo, strPegName,
                    strObjName,
                    objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }

            return AppsideKeywordDeal_CaptureAndCompare(strParaMeter, strData, strobjType,
                strAttachInfo, strPegName, strObjName,
                objProperties, objPegProperties,
                errorCheckObj,
                ref strError,
                ref strDataReturn,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                isInnerCall);

        }

        class ByKeyCompareParameter
        {
            const string CNST_ALLROWS = "ALLROWS";

            internal string RowRange;
            internal List<string> KeysToFetch = new List<string>();
            internal string TargetColumnName;

            internal bool AlystPara(string strParaToChck, ref string strError, ref string strAdv, ref string strStack)
            {
                //AllRows:Key:[a;b;c];ColumnToSave
                if (string.IsNullOrEmpty(strParaToChck))
                {
                    strError = "Parameter is null or Empty.";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                string strTmpPara = strParaToChck.ToUpper();
                string strErrorWrongFormatter = string.Format("Parameter Format is : ALLROWS:KEY:[KEYCOLUMN1:...KEYCOLUMNn];TargetColumn\r\nNot[{0}]", strParaToChck);
                if (!strTmpPara.StartsWith(CNST_ALLROWS + ":"))
                {
                    strError = strErrorWrongFormatter;
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                RowRange = "ALLROWS";

                string strParaWithoutPrefix = strParaToChck.Substring(CNST_ALLROWS.Length + 1);
                strTmpPara = strParaWithoutPrefix.ToUpper();
                if (!strTmpPara.StartsWith("KEY:["))
                {
                    strError = strErrorWrongFormatter;
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                strParaWithoutPrefix = strParaWithoutPrefix.Substring("KEY:[".Length);

                strTmpPara = strParaWithoutPrefix.Replace("]", "");
                string[] arrColumn = strTmpPara.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (arrColumn.Length < 2)
                {
                    strError = strErrorWrongFormatter;
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                KeysToFetch = new List<string>(arrColumn);
                TargetColumnName = arrColumn[arrColumn.Length - 1];
                KeysToFetch.RemoveAt(KeysToFetch.Count - 1);
                return true;
            }
        }

        //private static List<string> sortCompareByKeyData(List<MarsKeyValues<string, string>> lstDataRows)
        //{

        //}
        private static bool AppsideKeywordDeal_CaptureAndCompareByKey(string strParaMeter, string strData, string strobjType,
            string strAttachInfo,
            string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            //ROWS_LIMIT:5:5;[Main]Value
            //AllRows:Key:[a;b;c];ColumnToSave
            ByKeyCompareParameter keyCaptureAndCompareInfo = new ByKeyCompareParameter();
            if (!keyCaptureAndCompareInfo.AlystPara(strParaMeter, ref strError, ref strAdv, ref strStack))
            {
                return false;
            }
            if (string.Compare("swfTable", strobjType, true) != 0)
            {
                strError = string.Format("Only DataGrid is supported currently, but object type is [{0}]", strobjType);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            try
            {
                bool isOk = false;
                List<object> lstControls = new List<object>();
                String strObjTypUpper = "";

                //有时候 如果存在tooltipwindow，无法快速找到对象
                MarsWindowsAPIs.SetCursorPos(1, 1);
                Thread.Sleep(50);
                MarsWindowsAPIs.SetCursorPos(10, 10);

                bool isDisplyFormChild = string.Compare("debug:ShowChild", strParaMeter, true) == 0;
                isOk = GetCurrentControlsFilteredByType(strObjTypUpper = strobjType.ToUpper(), strPegName, strObjName, objPegProperties, objProperties, lstControls,
                        ref strError, ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile,
                    null, 180, isDisplyFormChild);
                if (!isOk)
                {
                    return false;
                }

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                if (lstControls.Count > 1)
                {
                    int idx = lstControls.Count - 1;

                    while ((lstControls.Count > 0) && (idx >= 0))
                    {
                        System.Windows.Forms.Control x = lstControls[idx] as System.Windows.Forms.Control;
                        if (x == null)
                        {
                            idx = idx - 1;
                        }
                        else
                        {
                            if (!x.Visible)
                                lstControls.RemoveAt(idx);
                            idx = idx - 1;
                        }
                    }
                }
                if (lstControls.Count <= 0)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = $"Object[{strObjName}] is found in PegWindow[{strPegName}]  but it is not visible");//"no objects exists after checking visible.");
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure object is visible on the screen";
                    return false;
                }
                System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
                if (c != null)
                {
                    Point pt = c.Parent == null ? c.PointToScreen(new Point(c.Left, c.Top)) : c.Parent.PointToScreen(new Point(c.Left, c.Top));
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("src point:[{0},{1}] to -[{2}]", c.Left, c.Top, pt));
                    Rectangle rect = new Rectangle(pt, c.Size);  //c.Parent == null ? c.Parent.RectangleToScreen(c.Bounds) : c.RectangleToScreen(c.Bounds);
#if gdienable
                    windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(
                        new MarsWindowsAPIs.RECT() { Left = rect.Left - 3, Right = rect.Right, Top = rect.Top - 3, Bottom = rect.Bottom },
                        ref strError
                        );
                    //if( c.CanFocus || c.CanSelect)
#endif
                    if (!(isOk = WaitforControlCanFocusOrCanSelect(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack)))
                    {
                        isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                    }
                }
                else
                {
                    isOk = false;
                }

                if (!isOk) return false;
                Thread.Sleep(500);
                List<MarsKeyValues<string, string>> lstDataRows = (new CaptureValueForSwfTable()).CaptureValueFromControl(lstControls[0], keyCaptureAndCompareInfo.RowRange,
                    keyCaptureAndCompareInfo.KeysToFetch,
                    keyCaptureAndCompareInfo.TargetColumnName,
                    strData,
                    strPegName, strObjName,
                    ref isOk,
                    ref strError,
                    ref strAdv,
                    ref strStack);

                /// should sort it
                /// 
                for (int i=0;i< (keyCaptureAndCompareInfo.KeysToFetch==null?-1: keyCaptureAndCompareInfo.KeysToFetch.Count); i++)
                {
                    var lstTmpData = lstDataRows.OrderBy(p => p.Children.ElementAt(i).MValue).ToList();
                    lstDataRows = lstTmpData;
                }
                var lstReturn = lstDataRows.Select(p => p.MValue).ToList();
                //var lstReturn = lstDataRows.Select(p => $"{p.MKey}[::]{p.MValue}")
                //    .OrderBy(p => p)
                //    .ToList();
                strDataReturn = string.Join("\r\n",lstReturn);

                //组装 data
                /// 所以格式是：ObjectName_[Key1]_[Key2]....[KeyN][::]value
                //strDataReturn = "";
                //foreach (var itm in lstDataRows)
                //{
                //    if (itm == null)
                //        continue;
                //    if (string.IsNullOrEmpty(strDataReturn))
                //        strDataReturn = string.Format("{0}[::]{1}", itm.MKey, itm.MValue);
                //    else
                //        strDataReturn = string.Format("{2}\r\n{0}[::]{1}", itm.MKey, itm.MValue, strDataReturn);
                //}

                return isOk;

            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_CaptureAndCompareByKey", strError = string.Format("Exception:[{0}]", e.Message), e);
                StackFrame stck = (new StackFrame());
                strStack = e.StackTrace;
                strAdv = "";
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_CaptureAndCompareByKey");
            }
        }

        private static void Highlight(System.Windows.Forms.Control c)
        {
            string strError = "";
            if (c != null)
            {
                Point pt = c.Parent == null ? c.PointToScreen(new Point(c.Left, c.Top)) : c.Parent.PointToScreen(new Point(c.Left, c.Top));
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("src point:[{0},{1}] to -[{2}]", c.Left, c.Top, pt));
                Rectangle rect = new Rectangle(pt, c.Size);  //c.Parent == null ? c.Parent.RectangleToScreen(c.Bounds) : c.RectangleToScreen(c.Bounds);
#if gdienable
                windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(
                    new MarsWindowsAPIs.RECT() { Left = rect.Left - 3, Right = rect.Right, Top = rect.Top - 3, Bottom = rect.Bottom },
                    ref strError
                    );
                //if( c.CanFocus || c.CanSelect)
#endif

            }
            try
            {
                Thread t = new Thread(new ThreadStart(() =>
                {
                    c.Refresh();
                }));
                t.SetApartmentState(ApartmentState.STA);
                t.Join();
            }catch(Exception e)
            {

            }
        }
        /// <summary>
        /// 10/28/24: enable capture grids'header by parameter like AllHeaders
        ///           Example CaptureAndCompare(SOME_TABLE, ALLHEADERS, TO_AN_OBJECT)
        /// 10/31/24: enable capture image button to text without using ocr tech
        ///           Example, in summit, client bond trade, there is an image button, buy or sell. clicking will change the value
        ///           CaptureAndCompare(SOME_IMAGE_BUTTON, "_MARSIMAGETOTEXT", TO_AN_OBJ)
        ///           this is only available for image button
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="errorCheckObj"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_CaptureAndCompare(string strParaMeter, string strData, string strobjType,
            string strAttachInfo,
            string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_CaptureAndCompare", string.Format("Parameter:[{0}] {1}.{2}, objType:[{3}]|pegName|{4}|", 
                strParaMeter, 
                MarsWindowsAPIsExtend.Dic2String(objPegProperties), 
                MarsWindowsAPIsExtend.Dic2String(objProperties), 
                strobjType,
                strPegName));
            MarsLoggerSimple.PreFix = "\t";
            try
            {
                bool isOk = false;
                List<object> lstControls = new List<object>();
                String strObjTypUpper = strobjType.ToUpper();

                //有时候 如果存在tooltipwindow，无法快速找到对象
                MarsWindowsAPIs.SetCursorPos(1, 1);
                Thread.Sleep(50);
                MarsWindowsAPIs.SetCursorPos(10, 10);

                bool isDisplyFormChild = string.Compare("debug:ShowChild", strParaMeter, true) == 0;
                //if (g_currentAllObjectListForCurrentPeg != null)
                //{
                //    lstControls = g_currentAllObjectListForCurrentPeg;
                //    isOk = true;
                //}
                //else
                //{
                    isOk = GetCurrentControlsFilteredByType(strObjTypUpper = strobjType.ToUpper(), strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile,
                    null, 180, isDisplyFormChild);
                    //if (!isOk)
                    //{
                    //    return false;
                    //}
                //}
                if (!isOk)
                {
                    MarsLoggerSimple.Error("AppsideKeywordDeal_CaptureAndCompare", string.Format("Error when call GetAllChildrenFromParent :[{0}]", strError));
                    //g_currentAllObjectListForCurrentPeg = null;
                    return false;
                }                             
                

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                if (lstControls.Count > 1)
                {
                    int idx = lstControls.Count - 1;

                    while ((lstControls.Count > 0) && (idx >= 0))
                    {
                        System.Windows.Forms.Control x = lstControls[idx] as System.Windows.Forms.Control;
                        if (x == null)
                        {
                            idx = idx - 1;
                        }
                        else
                        {
                            if (!x.Visible)
                                lstControls.RemoveAt(idx);
                            idx = idx - 1;
                        }
                    }
                }
                if (lstControls.Count <= 0)
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = "no objects exists after checking visible.");
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
                if (c != null)
                {
                    Highlight(c);
                }
                else
                {
                    isOk = false;
                    strError = "The object is not a Control, please ensure that the application is an windows desktop application.";
                }

                if (!isOk)
                {
                    simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_CaptureAndCompare", strError);
                    return false;
                }

                isOk = IsStepParameterMatchObjectType_CaptureAndCompare(strParaMeter, strObjTypUpper, ref strError);                
                if (!isOk)
                {
                    simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_CaptureAndCompare", $"after check parameter match type|{strParaMeter}|{strError}|");
                    return false;
                }
                switch (strObjTypUpper)
                {
                    case "SWFEDIT":
                        strDataReturn = (new CaptureValueForSwfEdit()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        break;
                    case "SWFLABEL":
                        strDataReturn = (new CaptureValueForSwfLabel()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        break;
                    case "SWFCOMBOBOX":
                        strDataReturn = (new CaptureValueForSwfComboboxInfra()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        break;
                    case "SWFTABLE":
                        Thread.Sleep(1000);
                        strDataReturn = (new CaptureValueForSwfTable()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        break;
                    case "SWFBUTTON":
                        strDataReturn = (new CaptureValueForSwfButton()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        break;
                    case "SWFSTATUSBAR":
                        strDataReturn = (new CaptureValueForSwfStatusBar()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        break;
                    case "SWFLISTVIEW":
                        strDataReturn = (new CaptureValueForSwfListView()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        break;
                    case "SWFTREEVIEW":
                        strDataReturn = (new MarsTreeViewOperation()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        break;
                    default:
                        isOk = false;
                        simpleLog.MarsLoggerSimple.Error("CaptureAndCompare", string.Format("Unsupported type for CaptureAndCompare:[{0}]", strObjTypUpper));
                        strError = $"Keyword CaptureAndCompare does not support object type for [{strObjName}]|{strObjTypUpper}|";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Mars supports Infragistics, WinForm and WPF controls";
                        break;
                }
                return isOk;

            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_CaptureAndCompare", strError = string.Format("Exception:[{0}]", e.Message), e);
                StackFrame stck = (new StackFrame());
                strStack = e.StackTrace;
                strAdv = "";
                return false;
            }
            finally
            {
                MarsLoggerSimple.Info("\t", string.Format("Data returns:[{0}]", strDataReturn));
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_CaptureAndCompare");
            }
        }
        /// <summary>
        /// 判断，是否该参数符合对象类别
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strObjTypUpper"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static bool IsStepParameterMatchObjectType_CaptureAndCompare(string strParaMeter, string strObjTypUpper, ref string strError)
        {
            MarsLoggerSimple.logBegin("IsStepParameterMatchObjectType_CaptureAndCompare", $"|{strParaMeter}|{strObjTypUpper}");
            if (string.IsNullOrEmpty(strParaMeter)) return true;
            if (strParaMeter.Equals(CaptureValueForSwfTable.cnst_all_headers, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(strObjTypUpper))
                {
                    strError = $"Object type is empty which is not used for |{strParaMeter}|";
                    return false;
                }
                if (strObjTypUpper.Equals("SWFTABLE", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                {
                    strError = $"paramter |{strParaMeter}| can't be applied to |{strObjTypUpper}|";
                    return false;
                }
            }

            if (strParaMeter.Equals(CaptureValueForSwfTable.cnst_image_button, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(strObjTypUpper))
                {
                    strError = $"Object type is empty which is not used for |{strParaMeter}|";
                    return false;
                }
                if (strObjTypUpper.Equals("SWFBUTTON", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else
                {
                    strError = $"paramter |{strParaMeter}| can't be applied to |{strObjTypUpper}| only swfbutton is enabled";
                    return false;
                }
            }
            return true;

        }

        private static bool AppsideKeywordDeal_InsertRow(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            //
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_InsertRow", string.Format("Parameter:[{0}] {1}.{2}", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), MarsWindowsAPIsExtend.Dic2String(objProperties)));
            MarsLoggerSimple.PreFix = "\t";
            try
            {
                bool isOk = false;
                List<object> lstControls = new List<object>();
                isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile);
                if (!isOk)
                {
                    return false;
                }

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                if (lstControls.Count != 1)
                {
                    strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                    return false;
                }

                isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                if (!isOk) return false;

                if (!(lstControls[0] is System.Windows.Forms.Control))
                {
                    strError = string.Format("Object should be control but type is :[{0}]", lstControls[0].GetType().ToString());
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
                c.Focus();

                Point pt = c.PointToScreen(new Point(48, 31));
                windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RightMouseClick(pt.X, pt.Y);

                System.Threading.Thread.Sleep(100);

                int iRc;
                if (string.IsNullOrEmpty(strParaMeter))
                {
                    strParaMeter = strData;
                }
                if (!int.TryParse(strParaMeter, out iRc))
                {
                    strError = string.Format("Parameter for insert row should be a number, but it is:[{0}]", strParaMeter);
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return isOk = false;
                }

                for (int i = 0; i < iRc; i++)
                {
                    System.Windows.Forms.SendKeys.SendWait("{DOWN}");
                    System.Threading.Thread.Sleep(50);
                }
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                return isOk = true;
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_InsertRow", strError = string.Format("Exception:[{0}] at \r\n{1}", e.Message, e.StackTrace), e.StackTrace);
                StackFrame stck = (new StackFrame());
                strStack = e.StackTrace;
                strAdv = "";
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("AppsideKeywordDeal_InsertRow");
            }
        }
        /// <summary>
        /// 如果是按行点击，格式如下：
        ///   clickAT, dataGrid, LEFT_CLICK, 3,#:4 表示横向3，第5行
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_ClickAt(string strParaMeter, string strData, string strobjType,
            string strAttachInfo,
            string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_ClickAt", string.Format("Parameter:[{0}] {1}.{2}", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                isDisplayFormObjects:true);
            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            if (!(lstControls[0] is System.Windows.Forms.Control))
            {
                strError = string.Format("Object should be control but type is :[{0}]", lstControls[0].GetType().ToString());
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
            c.Focus();

            if (string.Compare(strobjType, "SwfTable", true) == 0)
            {
                string usagePara = string.IsNullOrEmpty(strData) ? strParaMeter : strData;
                bool isSortHeader = InfragisticsGridHelper.isSortHeaderModifer(usagePara, ref usagePara);
                if (isSortHeader)
                {
                    /// 需要点击header，然后排序
                    /// 
                    simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_ClickAt", "sortHader modal");
                    strDataReturn = InfragisticsGridHelper.SortHeaderByClick(c, usagePara, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    return isOk;
                }
            }

            if (string.IsNullOrEmpty(strData))
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ClickTable", strError = string.Format("Data should be two numbers,splited by ',', but it is null:[{0}]", strData));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }

            string[] arrXY = strData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (arrXY.Length != 2)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ClickTable", strError = string.Format("Data should be two numbers,splited by ',', but it is:[{0}]", strData));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            int xOff, yOff;
            bool isRowNumberMode = false;
            /// 示例: 14,RowNumber 或者 #:2
            if ((!string.IsNullOrEmpty(arrXY[1]))
                &&(!string.IsNullOrEmpty(strobjType))
                &&(strobjType.Equals("swfTable", StringComparison.OrdinalIgnoreCase))
                &&((arrXY[1].StartsWith(SearchAndUpdateForInfragisticsGrid.cnst_rowNUmMode))
                ||arrXY[1].StartsWith("#")
                )
                )
            {
                isRowNumberMode = true;                
            }
            if (isRowNumberMode)
            {
                return (new SearchAndUpdateForInfragisticsGrid()).ClickAt(lstControls[0], strParaMeter,
                    arrXY[0], arrXY[1], strPegName, strObjName, ref strError, ref strAdv, ref strStack, strAttachInfo);
            }
            if ((!int.TryParse(arrXY[0], out xOff)) || (!int.TryParse(arrXY[1], out yOff)))
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_ClickTable", strError = string.Format("two numbers required,splited by ',', but it is:[{0}]", strData));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            
            Rectangle rect = c.Bounds;
            //Highlight(c);
            if (c.Parent == null)
            {
                rect.X += xOff;
                rect.Y += yOff;
            }
            else
            {

                MarsLoggerSimple.Info("\t", string.Format("before convert:[{0}]", rect));
                rect = c.Parent.RectangleToScreen(rect);
                MarsLoggerSimple.Info("\t", string.Format("after convert:[{0}]", rect));
                rect.X += xOff;
                rect.Y += yOff;
            }
            if (string.IsNullOrEmpty(strParaMeter)) strParaMeter = "LEFT_CLICK";
            if (string.Compare("LEFT_CLICK", strParaMeter.Trim(), true) == 0)
            {
                MarsWindowsAPIsExtend.LeftMouseClick(rect.X, rect.Y);
                return true;
            }
            if (string.Compare("RIGHT_CLICK", strParaMeter.Trim(), true) == 0)
            {
                MarsWindowsAPIsExtend.RightMouseClick(rect.X, rect.Y);
                return true;
            }
            if (string.Compare("LEFT_DBL_CLICK", strParaMeter.Trim(), true) == 0)
            {
                MarsWindowsAPIsExtend.LeftMouseClick(rect.X, rect.Y);
                MarsWindowsAPIsExtend.LeftMouseClick(rect.X, rect.Y);
                return true;
            }
            strError = string.Format("unsupported parameter:[{0}]", strParaMeter);
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "";
            return false;
        }

        private static bool DealwithGridCellObject(Control targetGrid, Dictionary<string, string> dicGridCellInfo, string strParaMeter, string strData,
            string strAttachInfo,
            string strPegName, string strObjName,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            MarsLoggerSimple.logBegin("DealwithGridCellObject");
            try
            {
                if (targetGrid == null)
                {
                    strError = "Passing null object to a function";//"source control is null";
                    MarsLoggerSimple.Error("DealwithGridCellObject", strError);
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }

                MarsTableOperation tblOp = new MarsTableOperation();
                if ((!dicGridCellInfo.ContainsKey(MarsTableOperation.CNST_GRIDCELL_COLNAME))
                    || (!dicGridCellInfo.ContainsKey(MarsTableOperation.CNST_GRIDCELL_ROWINDEX)))
                {
                    MarsLoggerSimple.Error("DealwithGridCellObject", strError = string.Format("[{0}] or [{1}] is empty, both of them should not be empty.", MarsTableOperation.CNST_GRIDCELL_COLNAME, MarsTableOperation.CNST_GRIDCELL_ROWINDEX));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                string strColName = dicGridCellInfo[MarsTableOperation.CNST_GRIDCELL_COLNAME];
                string strRowIdx = dicGridCellInfo[MarsTableOperation.CNST_GRIDCELL_ROWINDEX];
                string[] arrRows = strRowIdx.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (arrRows.Length != 2)
                {
                    strError = $"The format of row index should be [column name];[column value],but it is [{strRowIdx}]. row number:{(new StackFrame()).GetFileName()}.{(new StackFrame()).GetFileLineNumber()}";
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "";
                    return false;
                }
                string strRowIdxCol = arrRows[0];
                strRowIdx = arrRows[1];
                string strColKey = ""
                    , strRowIdxColKey = "";
                int iColIdx = -1
                    , iRowIdxColIdx = -1;
                //target cell index, 
                bool isColFind = tblOp.GetColumnKeyForInfragisticsGrid(targetGrid, strColName, strPegName, strObjName, ref strColKey, ref iColIdx, ref strError, ref strAdv, ref strStack);
                if (!isColFind)
                {
                    MarsLoggerSimple.Error("DealwithGridCellObject", strError);
                    return false;
                }
                isColFind = tblOp.GetColumnKeyForInfragisticsGrid(targetGrid, strRowIdxCol, strPegName, strObjName, ref strRowIdxColKey, ref iRowIdxColIdx,
                    ref strError,
                    ref strAdv,
                    ref strStack);
                if (!isColFind)
                {
                    MarsLoggerSimple.Error("DealwithGridCellObject", strError);
                    return false;
                }

                object targetRow = null;
                bool isRowLocated = tblOp.LocatedRowByRowHeader(targetGrid, strRowIdx, iRowIdxColIdx, strParaMeter, ref targetRow, ref strError,
                    ref strAdv,
                    ref strStack);
                if (!isRowLocated)
                {
                    MarsLoggerSimple.Error("DealwithGridCellObject", strError);
                    return false;
                }
                bool isOk = tblOp.FillCell(targetGrid, targetRow, strColKey, iColIdx, strData, strAttachInfo, strPegName, strObjName, ref strError,
                    ref strAdv,
                    ref strStack);
                if (!isOk)
                {
                    MarsLoggerSimple.Error("DealwithGridCellObject", strError);
                    return false;
                }
                return true;
            }
            finally
            {
                MarsLoggerSimple.logEnd("DealwithGridCellObject");

            }

        }

        /// <summary>
        /// 11/22/24 添加 BYLOOPITERATION
        ///   
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="errorCheckObj"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_FillTable(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_FillTable", string.Format("Parameter:[{0}] {1}.{2}", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            List<object> lstControls = new List<object>();

            //simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_FillTable", "begin to get stepCheck Error settings");
            //ConvertAttachToErrorCheck(strAttachInfo);
            //simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_FillTable", $"{ strAttachInfo}-[{currentStepCheckError}]");

            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            //isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objPropertiesTmp, lstControls, ref strError);
            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            if (!(lstControls[0] is System.Windows.Forms.Control))
            {
                strError = string.Format("Object should be control but type is :[{0}]", lstControls[0].GetType().ToString());
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }

            #region to make sure the control is ready
            Control cntrlList = ((Control)lstControls[0]);
            IntPtr timeoutRslt = IntPtr.Zero;
            IntPtr rsltTimeOut = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(cntrlList.Handle,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                10000,
                out timeoutRslt
                );
            if (rsltTimeOut.ToInt64() != 0)
            {
                simpleLog.MarsLoggerSimple.Info("MarsListViewOperation", "send time out returns true, no thread is busy");
            }

            ((Control)lstControls[0]).Update();

            System.Threading.Thread.Sleep(100);
            #endregion

            System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
            c.Focus();
            
            //分析参数格式
            bool isRightFormat = false;
            string strKey = "";
            int idx = -1;
            IntPtr lpRslt;

            ///11/22/24 增加新的模式， BYLOOPITERATION，
            ///
            string oldPara = strParaMeter;
            strParaMeter = PreprocessParaForLoopIteration(strParaMeter);
            simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_FillTable", $"parameter preprocess from |{oldPara}| to |{strParaMeter}");

            Dictionary<string, string> dicGridCellInfo = new Dictionary<string, string>();
            if (MarsformIndentifier.ContainsGridCellProper(objProperties, dicGridCellInfo))
            {
                return DealwithGridCellObject(c, dicGridCellInfo, strParaMeter, strData, strAttachInfo, strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            }

            MarsTableOperation tblOp = new MarsTableOperation();
            int iRowNum = -1, iOrignRowCnt=-1;
            en_fillTable_paraType iMode = tblOp.checkMode(strParaMeter);
            
            if ((iMode == en_fillTable_paraType.dynamicRow)//simple mode
                ||(iMode == en_fillTable_paraType.allRows)
                ||(iMode == en_fillTable_paraType.rowNumber))
            {
                string[] arrPara = strParaMeter.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if ((string.Compare(arrPara[0], "dynamicrow", true) == 0) 
                    || (iMode == en_fillTable_paraType.allRows)
                    || (int.TryParse(arrPara[0], out iRowNum)))
                {
                    isRightFormat = true;
                    //获得column
                    if (!tblOp.GetColumnKeyForInfragisticsGrid(lstControls[0], arrPara[1], strPegName, strObjName, ref strKey, ref idx, ref strError, ref strAdv, ref strStack))
                    {
                        return isOk = false;
                    }

                    int iCount = 0;

                    object[] arrAll = tblOp.GetRowsFromGridControl(lstControls[0], ref isOk, ref iCount, ref strError, ref strAdv, ref strStack);
                    if ((arrAll == null)||(arrAll.Length==0))
                    {
                        return isOk = false;
                    }
                    int iCurrentRowId = iCount>=0?0:int.MaxValue;
                    simpleLog.MarsLoggerSimple.Info("\t", $"GetRowsFromGridControl.count = [{iCount}]-current rowId:[{iCurrentRowId}]");
                    int iBlockTime = iMode == en_fillTable_paraType.dynamicRow?5000:5000;

                    while (iCurrentRowId< iCount) 
                    //for (int i = 0; i < iCount; i++)
                    {
                        try
                        {
                            int i = iCurrentRowId;
                            if (i != 0)
                            {
                                arrAll = tblOp.GetRowsFromGridControl(lstControls[0], ref isOk, ref iCount, ref strError, ref strAdv, ref strStack);
                            }
                            if ((arrAll == null) || (arrAll.Length == 0))
                            {
                                break;
                            }
                            //int iCurrentRowId = i;
                            if (iMode == en_fillTable_paraType.dynamicRow)
                            {
                                i = iCount - 1;
                            }
                            else if (iMode == en_fillTable_paraType.rowNumber)
                            {
                                i = iRowNum;
                                if (iRowNum < 0)
                                {
                                    strError = "Parameter format is wrong when check Row number mode";
                                    strAdv = "Make use that the format is right";
                                    strStack = Environment.StackTrace;
                                    isOk = false;
                                    return false;
                                }
                            }
                            //get the last row
                            object row = tblOp.GetRowByCommand(lstControls[0],
                                i + "", //arrPara[0], 
                                arrAll, ref isOk, ref strError, ref strAdv, ref strStack);
                            if (!isOk)
                            {
                                simpleLog.MarsLoggerSimple.Error("\t", strError, strStack);
                                return false;
                            }
                            simpleLog.MarsLoggerSimple.Info("\t", $"call fill Cell {iCurrentRowId}/{iCount} -iMode:[{iMode}]|paraLen|{arrPara.Length}");
                            //active row and fill
                            string theThirdPara = null;
                            if (arrPara.Length >= 3)
                            {                               
                                theThirdPara = arrPara[2].Trim();
                            }
                            isOk = tblOp.FillCell(c, row, strKey, idx, strData, strAttachInfo, strPegName, strObjName,
                                ref strError, ref strAdv, ref strStack,
                                iMode == en_fillTable_paraType.allRows ? 5 : 50,
                                iMode == en_fillTable_paraType.allRows ? false : true,
                                theThirdPara:theThirdPara);
                            if (isOk)
                            {
                                MarsWindowsAPIs.SendMessageTimeout(//new HandleRef(CurrentPegWindows[0],CurrentPegWindows[0].WindowHandle),
                                    CurrentPegWindows[0].WindowHandle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
                                    (uint)iBlockTime,
                                    out lpRslt);

                            }
                            if (iMode == en_fillTable_paraType.rowNumber) break;
                        }
                        finally
                        {
                            iCurrentRowId += 1;
                        }
                    }
                    return true;

                }
            }
            else
            {
                //Group mode
                return tblOp.FillTableInGroupMode(lstControls[0], strData, strParaMeter, strobjType, strAttachInfo, strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            }

            if (!isRightFormat)
            {
                strError = string.Format("Parameter foramt is wrong for FillTable, \"RowMark;columnName[;type] is required, but [{0}]", strParaMeter);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_FillTable", strError);
                return isOk = false;
            }
            MarsWindowsAPIs.SendMessageTimeout(CurrentPegWindows[0].WindowHandle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
                            5000,
                            out lpRslt);
            //IntPtr pNewHandle = IntPtr.Zero;
            return isOk = true;
        }

        private static string PreprocessParaForLoopIteration(string strParaMeter)
        {
            simpleLog.MarsLoggerSimple.logBegin("PreprocessParaForLoopIteration", $"{strParaMeter}");
            if (string.IsNullOrEmpty(strParaMeter)) return "";
            return strParaMeter.Replace("BYLOOPITERATION:", ""); 
        }

        private static bool AppsideKeywordDeal_PreviewObject(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_PreviewObject", string.Format("Parameter:[{0}] {1}.{2}", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            List<object> lstControls = new List<object>();

            DateTime begin = DateTime.Now;//默认120秒
            DateTime c = begin;
            while ((c.Ticks - begin.Ticks) < (120 * TimeSpan.TicksPerSecond))
            {
                try
                {
                    //
                    object oForm = null;
                    int iWaitTime = 10;
                    var frmTarget = ReGetForm(CurrentPegwindowType, objPegProperties, strPegName, strObjName, ref oForm, ref isOk, ref strError,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile,
                        iWaitTime);
                    if (frmTarget == null)
                    {
                        //暂时没有找到pegwindow
                        Thread.Sleep(100);
                        continue;
                    }
                    
                    if (!(frmTarget.AssignedForm is Control))
                    {
                        MarsLoggerSimple.Error("AppsideKeywordDeal_PreviewObject", string.Format("regetform return non control object, with type:[{0}]", frmTarget.AssignedForm.GetType()));
                        Thread.Sleep(100);
                        continue;
                    }
                    MarsWindowsAPIs.RECT lpRect;
                    if (!MarsWindowsAPIs.GetWindowRect(((Control)frmTarget.AssignedForm).Handle, out lpRect))
                    {
                        MarsLoggerSimple.Error("AppsideKeywordDeal_PreviewObject", string.Format("Error code:[{0}] after getwindowRect", MarsWindowsAPIs.GetLastError()));
                        Thread.Sleep(100);
                        continue;
                    }
                    if (((lpRect.Right - lpRect.Left) <= 0) || (lpRect.Bottom - lpRect.Top) <= 0)
                    {
                        MarsLoggerSimple.Error("AppsideKeywordDeal_PreviewObject", string.Format("form is invisible, as the rect from getiwndowrect is [{0}]", lpRect));
                        Thread.Sleep(100);
                        continue;
                    }
                    //获得子对象信息
                    List<object> lstObjs = GetAllChildrenFromParent(frmTarget, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    return true;
                    int iCount = lstObjs == null ? -1 : lstObjs.Count;
                    for (int j = lstObjs == null ? -1 : lstObjs.Count - 1; j >= 0; j--)
                    {
                        Control cx = lstObjs[j] as Control;
                        if (cx == null) continue;
                        MarsformIndentifier.FetchControlInfomation(cx, objProperties, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                        if (!isOk)
                        {
                            lstObjs.RemoveAt(j);
                            continue;
                        }
                        //获得window的handle,和text
                        lpRect.Left = 0; lpRect.Right = 0; lpRect.Top = 0; lpRect.Bottom = 0;
                        if (!MarsWindowsAPIs.GetWindowRect(cx.Handle, out lpRect))
                        {
                            lstObjs.RemoveAt(j);
                            MarsLoggerSimple.Info("AppsideKeywordDeal_PreviewObject", "no object rect is ready");
                            continue;
                        }
                        if (((lpRect.Bottom - lpRect.Top) <= 0) || ((lpRect.Right - lpRect.Left) <= 0))
                        {
                            lstObjs.RemoveAt(j);
                            continue;
                        }
                        if ((!cx.CanFocus) || (!cx.IsHandleCreated))
                        {
                            lstObjs.RemoveAt(j);
                            MarsLoggerSimple.Info("AppsideKeywordDeal_PreviewObject", string.Format("CanFocus is false, typeof [{0}]", cx.GetType()));
                            continue;
                        }
                    }
                    if (iCount <= 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    if (lstObjs.Count == 1)
                    {
                        (lstObjs[0] as Control).Update();
                    }
                    MarsLoggerSimple.Info("AppsideKeywordDeal_PreviewObject", string.Format("objects left:[{0}]", lstObjs.Count));
                    return isOk = true;
                }
                finally
                {
                    c = DateTime.Now;
                }
            }
            //previewobject is not required to popup error information
            return isOk = false;
        }

        private static bool AppsideKeywordDeal_PressKey(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_PressKey", string.Format("Parameter:[{0}] {1}.{2}", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            List<object> lstControls = new List<object>();
            int x1, y1;
            if ((!string.IsNullOrEmpty(strParaMeter)) && (
                (string.Compare(MarsObjectKeyword.cnst_keyword_para_CURRENT_POS, strParaMeter, true) == 0)
                ||(string.Compare("CURRENT_POS_NO_CLICK", strParaMeter, true) == 0)
                ))
            {
                MarsLoggerSimple.Info("AppsideKeywordDeal_PressKey", "current pos or no click mode");
                //x1 = System.Windows.Forms.Cursor.Position.X;
                //y1 = System.Windows.Forms.Cursor.Position.Y;
                //simpleLog.MarsLoggerSimple.Info("\t", string.Format("Current Pos mode, [{0}-{1}] ", x1, y1));
                System.Windows.Forms.SendKeys.SendWait(strData);
                return isOk = true;
            }

            /// 增加.net framework control hosted by其他contrainer，如wpf之类
            /// 
            if (objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
            {
                /// 说明是host模式
                /// 
                return HostedFrameworkControlKeywordHelper.PressKey(strParaMeter, strData, strobjType, strAttachInfo, strPegName,
                    strObjName, objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }

            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            if (!(lstControls[0] is System.Windows.Forms.Control))
            {
                strError = string.Format("Object should be control but type is :[{0}]", lstControls[0].GetType().ToString());
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "";
                return false;
            }
            System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
            Point pt = c.Parent == null ? c.PointToScreen(new Point(c.Left, c.Top)) : c.Parent.PointToScreen(new Point(c.Left, c.Top));
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("src point:[{0},{1}] to -[{2}]", c.Left, c.Top, pt));
            //Point ptnew = c.Bounds.Location;
            //if (c.Parent==null)
            //{
            //    //perhaps,it is windows
            //    ptnew = c.Location;
            //}else
            //     ptnew = c.Parent.PointToScreen(new Point(c.Bounds.X, c.Bounds.Y));
            bool isMouseClickRequired = true;
            if (!string.IsNullOrEmpty(strParaMeter))
            {
                isMouseClickRequired = (string.Compare("CURRENT_POS_NO_CLICK", strParaMeter, true) != 0);
                if ((string.Compare(MarsObjectKeyword.cnst_keyword_para_CURRENT_POS, strParaMeter, true) == 0)
                    ||(!isMouseClickRequired))
                {
                    x1 = System.Windows.Forms.Cursor.Position.X;
                    y1 = System.Windows.Forms.Cursor.Position.Y;

                }
                else
                {
                    string[] arrPos = strParaMeter.Split(',');
                    if (arrPos.Length != 2)
                    {
                        simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_PressKey", strError = string.Format("PressKeys parameter should be two numbers with [X],[Y],format, but it is :[{0}]", strParaMeter));
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "";
                        return isOk = false;
                    }

                    try
                    {
                        x1 = int.Parse(arrPos[0]);
                        y1 = int.Parse(arrPos[1]);
                    }
                    catch (Exception e)
                    {
                        simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_PressKey", strError = string.Format("PressKeys parameter should be two numbers with [X],[Y],format, but it is :[{0}]", strParaMeter));
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "";
                        return isOk = false;
                    }
                    //ptnew.X += x1;
                    //ptnew.Y += y1;
                    pt.X += x1;
                    pt.Y += y1;
                }                
            }
            if (isMouseClickRequired)
                for (int i = 0; i < 5; i++)
                {
                    //MarsWindowsAPIsExtend.LeftMouseClick(ptnew.X + 10, ptnew.Y + 10);
                    MarsWindowsAPIsExtend.LeftMouseClick(pt.X + 10, pt.Y + 10);

                    if (c.Focus()) break;
                    Thread.Sleep(10);
                }
            else
                c.Focus();

            System.Windows.Forms.SendKeys.SendWait(strData);
            return isOk = true;
        }

        /// <summary>
        /// 关闭窗口。
        /// 有三种模式
        /// 1，strpara 包括bypos什么的。这样，标识是窗口的右上角的位置
        /// 2，strpara 是pre。标识关闭前一个窗口。strdata可以为空，也可以是bypos。推荐bypos
        /// 3，strpara不为为空，strData不为空。正常模式
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="isInnerCall"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_CloseWindow(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_ClickWindow", string.Format("Parameter:[{0}] {1}.{2}", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;
            if (string.Compare("pre", strParaMeter, true) == 0)
            {
                if ((PreviousPegWindows == null) || (PreviousPegWindows.AssignedForm == null))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", "no previous windows set");
                    return true; //尽管没有前面的数据 但是，依旧返回true
                }
                //先将该窗口提前
                if (!windowsWrapper.SystemUtil.MarsWindowsAPIs.BringWindowToTop(PreviousPegWindows.WindowHandle))
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("can't bring Previous windows front with last error:[{0}]", windowsWrapper.SystemUtil.MarsWindowsAPIs.GetLastError()));
                    return false;
                }
                Thread.Sleep(20);

                if (PreviousPegWindows.AssignedForm is System.Windows.Forms.Form)
                {
                    System.Windows.Forms.Form f = PreviousPegWindows.AssignedForm as System.Windows.Forms.Form;
                    return (new CloseWindowForStandardForm()).CloseWindowForControl(PreviousPegWindows.AssignedForm, ref isOk, ref strError, ref strAdv, ref strStack, strData);
                }
            }
            if (lstControls[0] is System.Windows.Forms.Form)
            {
                System.Windows.Forms.Form f = lstControls[0] as System.Windows.Forms.Form;
                if ((string.IsNullOrEmpty(strData)) && (!string.IsNullOrEmpty(strParaMeter)))
                {
                    strData = strParaMeter;
                }
                //return (new CloseWindowForStandardForm()).CloseWindowForControl(lstControls[0], ref isOk, ref strError, strParaMeter);
                return (new CloseWindowForStandardForm()).CloseWindowForControl(lstControls[0], ref isOk, ref strError, ref strAdv, ref strStack, strData);
            }
            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("unsupported type for close window:[{0}]", lstControls[0].GetType().ToString()));
            strError = $"Keyword does not support object type for [{strObjName}]";
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Mars supports Infragistics, WinForm and WPF controls";
            return isOk = false;
        }

        private static bool AppsideKeywordDeal_Dismiss(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false,
            int iWaitingTime = -1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_Dismiss", string.Format("Parameter:[{0}] {1}.{2} ObjectType:[{3}]", strParaMeter,
                MarsWindowsAPIsExtend.Dic2String(objPegProperties),
                MarsWindowsAPIsExtend.Dic2String(objProperties), strobjType));
            bool isOk = false;

            if ((objPegProperties.Keys != null) && (objPegProperties.Keys.Contains("OBJECT CLASS")))
            {
                if (string.Compare(objPegProperties["OBJECT CLASS"], "#32770", true) == 0)
                {
                    return (new Dismiss().Dismiss32770Dialog(objProperties, strParaMeter, ref strError, ref strAdv, ref strStack));
                }
            }


            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                return true;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return true;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk)
            {
                MarsLoggerSimple.Info("\t", "Can't wait until control is visible or enabled");
                return true;
            }

            System.Windows.Forms.Control dialog = lstControls[0] as System.Windows.Forms.Control;
            if (dialog == null)
            {
                MarsLoggerSimple.Info("\t", strError = $"Object [{strPegName}] is not Control"); //string.Format("unknow control from identification with type info:[{0}]", lstControls[0].GetType()));
                strStack = $"Object type is [{lstControls[0].GetType()}]\r\n{MarsErrorStacks.StackTraceDump()}";
                strAdv = $"Make sure [{strPegName}] is available on the screen";
                return false;
            }

            /// only take care of 3270 dialog, requires more for other types
            try
            {
                MarsLoggerSimple.Info("\t", "unsupported mode, all child list below:");
                var allSub = dialog.Controls.OfType<System.Windows.Forms.Control>().Select(p => p.GetType().ToString()).ToList();
                MarsLoggerSimple.Info("\t", string.Format("all types in dialog:[{0}]", string.Join(";", allSub.ToArray())));
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("\t", strError = $"Error while dismissing for [{strPegName}].[{strObjName}]");// string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace));
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
            }

            return true;

            //MarsLoggerSimple.Info("\t", string.Format("Find a button:{0}", objPegProperties.Select(kv => string.Format("[{0}]-[{1}]", kv.Key, kv.Value))));
            /////do click
            ///// 
            //System.Windows.Forms.Control btn = (System.Windows.Forms.Control)lstControls[0];
            //if (btn == null)
            //{
            //    strError = string.Format("more than one objects exist. null -[{0}] values:[{1}]", string.Concat(",", objProperties.Keys), string.Concat(",", objProperties.Values));
            //    return false;
            //}
            //int x = btn.Left + btn.Width / 2, y = btn.Top + btn.Height / 2;
            //btn.Focus();
            //System.Drawing.Point ptNew = btn.Parent.PointToScreen(new System.Drawing.Point(x, y));
            //MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);

            //return true;
        }

        private static bool AppsideKeywordDeal_ClickPopupMenuItem(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_ClickPopupMenuItem", string.Format("Parameter:[{0}] {1}.{2}", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            bool isSkip_notExist = string.Compare("SKIP_NOTEXIST", strParaMeter, true) == 0;

            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                isDisplayFormObjects: string.Compare(strParaMeter == null ? "" : strParaMeter, "debug:ShowChild", true) == 0);
            if (!isOk)
            {
                if (isSkip_notExist) return true;
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                if (isSkip_notExist) return true;
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]"; //string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            string strTypes = lstControls[0] == null ? "" : ReflectorForCSharp.GetObjectBaseType(lstControls[0].GetType());
            if ((strTypes.IndexOf("Infragistics.Win.UltraWinToolbars.PopupControlBase") >= 0) || (strTypes.IndexOf("Infragistics.Win.UltraWinToolbars.PopupMenuControlTrusted") >= 0))
            {
                //从popupcontrol base中弹出的
                if (!(new ClickPopupMenuItemForInfragistics()).PerformClickPopupMenuItemFromPopupbase(lstControls[0], strParaMeter, strData, strPegName, strObjName, ref strError, ref strAdv, ref strStack))
                {
                    return false;
                }
                return true;
            }

            if (!(new ClickPopupMenuItemForInfragistics()).PerformClickPopupMenuItem(lstControls[0], strParaMeter, strData, strPegName, strObjName, ref strError, ref strAdv, ref strStack))
            {
                return false;
            }
            return true;
        }
        private static bool AppsideKeywordDeal_ClickRadioButton(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_ClickRadioButton", string.Format("Parameter:[{0}] {1}.{2}", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            bool isSkip_notExist = string.Compare("SKIP_NOTEXIST", strParaMeter, true) == 0;

            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                if (isSkip_notExist) return true;
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                if (isSkip_notExist) return true;
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            if (lstControls[0] is System.Windows.Forms.RadioButton)
            {
                return (new keywordOperation.ClickRadioButton.Standard()).Click((lstControls[0] as System.Windows.Forms.RadioButton), strParaMeter, strData, ref strError, ref strAdv, ref strStack);
            }
            simpleLog.MarsLoggerSimple.Error("ClickRadioButton", string.Format("Only class from System.Windows.Forms.RadioButton is supported, but the type is :[{0}]", lstControls[0].GetType().ToString()));
            strError = $"Keyword does not support object type for [{strObjName}]";
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Mars supports Infragistics, WinForm and WPF controls";
            return isOk = false;
        }


        private static bool AppsideKeywordDeal_ClickButton(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
             MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.Info("AppsideKeywordDeal_ClickButton", string.Format("Parameter:[{0}] {1}.{2}, type:[{3}]", strParaMeter, MarsWindowsAPIsExtend.Dic2String(objPegProperties), 
                MarsWindowsAPIsExtend.Dic2String(objProperties), strobjType));

            /// 需要支持标准的，如MFC的对象
            /// 
            if (IsStandardControlType(strobjType))
            {
                return MarsStandardMFCControlKeywordOp.ClickButton(strParaMeter, strData, strobjType,
                    strAttachInfo, strPegName,
                    strObjName,
                    objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }

            bool isOk = false;
            bool isSkip_notExist = string.Compare("SKIP_NOTEXIST", strParaMeter, true) == 0;
            bool isClickTable = false;

            /// 增加.net framework control hosted by其他contrainer，如wpf之类
            /// 
            if (objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
            {
                /// 说明是host模式
                /// 
                return HostedFrameworkControlKeywordHelper.ClickButton(strParaMeter, strData, strobjType, strAttachInfo, strPegName,
                    strObjName, objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }

            List<object> lstControls = new List<object>();

            if (string.Compare("SWFTABLE", strobjType ?? "", true) == 0)
            {
                isOk = GetCurrentControlsFilteredByType("SWFTABLE", strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    null, 180, false);
                isClickTable = true;
            }
            else
            {
                isOk = GetCurrentControlsFilteredByType("SWFBUTTON", strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    null, 180, false);
                if (!isOk)
                {
                    if (isSkip_notExist) return true;
                    return false;
                }
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                if (isSkip_notExist) return true;
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                MarsLoggerSimple.Error("AppsideKeywordDeal_ClickButton", "No such button");
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            //isOk = WaitForControlIsVisibleAndEnable(lstControls[0],ref strError);
            //if (!isOk) return false;

            MarsLoggerSimple.Info("\t", string.Format("Find a button/Table:{0}", objPegProperties.Select(kv => string.Format("[{0}]-[{1}]", kv.Key, kv.Value))));
            if (isClickTable)
            {
                return (new MarsTableOperation()).clickButtonOnTable(lstControls[0], strParaMeter, strData, strPegName, strObjName, ref strDataReturn, ref strError, ref strAdv, ref strStack);
            }

            #region to make sure the control is ready
            Control cntrlList = ((Control)lstControls[0]);
            IntPtr timeoutRslt = IntPtr.Zero;
            //IntPtr rsltTimeOut = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(cntrlList.Handle,
            //    0,
            //    IntPtr.Zero,
            //    IntPtr.Zero,
            //    windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
            //    10000,
            //    out timeoutRslt
            //    );
            //if (rsltTimeOut.ToInt64() != 0)
            //{
            //    simpleLog.MarsLoggerSimple.Info("MarsListViewOperation", "send time out returns true, no thread is busy");
            //}

            //((Control)lstControls[0]).Update();
            System.Threading.Thread.Sleep(10);
            #endregion

            ///do click
            /// 
            System.Windows.Forms.Control btn = (System.Windows.Forms.Control)lstControls[0];
            string strTyps = ReflectorForCSharp.GetObjectBaseType(btn.GetType());
            MarsLoggerSimple.Info("\t", string.Format("types:[{0}]", strTyps));
            //if (strTyps.IndexOf("System.Windows.Forms.Button") >= 0)
            //{
            //    System.Windows.Forms.Button btn1 = (System.Windows.Forms.Button)btn;
            //    var xxx = new Thread(() => {
            //        MarsLoggerSimple.Info("\t", "clickbutton, start sta thread");
            //        btn1.PerformClick();
            //    });
            //    xxx.SetApartmentState(ApartmentState.STA);
            //    xxx.Start();
            //    xxx.Join();
                
            //    MarsLoggerSimple.Info("\t", "call button performClick");
            //    MarsLoggerSimple.logEnd("AppsideKeywordDeal_ClickButton");
            //    return true;
            //}

            if (strTyps.IndexOf("Infragistics.Win.Misc.UltraExpandableGroupBox") >= 0)
            {
                string strAction = strData ?? "Expand";
                var oExpanded = ReflectorForCSharp.GetMember(btn, "Expanded");
                if ((oExpanded == null) || (!(oExpanded is bool)))
                {
                    MarsLoggerSimple.Error("\t", strError = string.Format("wrong version? no expanded member in [{0}]", strTyps));
                    strError = oExpanded == null ? "Object property [Expanded] is Null" : "Object member [Expanded]'s value is not bool in Grid";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                bool isExpandedSrc = (bool)oExpanded;
                bool valueToSet = true;
                bool isClickRequired = false;
                if (string.Compare(strAction, "Expand", true) == 0)
                {
                    if (isExpandedSrc) return true;
                    isClickRequired = true;
                    valueToSet = true;
                }
                else
                {
                    if (!isExpandedSrc) return true;
                    isClickRequired = true;
                    valueToSet = false;
                }
                if (!isClickRequired) return true;

                var ControlUIElement = ReflectorForCSharp.GetMember(btn, "ControlUIElement");
                if (ControlUIElement == null)
                {
                    MarsLoggerSimple.Error("\t", strError = string.Format("wrong version? no ControlUIElement member in [{0}]", strTyps));
                    strError = "Object property [ControlUIElement] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                string strControlUIElementTyps = ReflectorForCSharp.GetObjectBaseType(ControlUIElement.GetType());
                if (strControlUIElementTyps.IndexOf("UltraGroupBoxUIElement") < 0)
                {
                    MarsLoggerSimple.Error("\t", strError = string.Format("wrong version? no UltraGroupBoxUIElement find in [{0}]", strControlUIElementTyps));
                    strError = "Object property [UltraGroupBoxUIElement] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                var Header = ReflectorForCSharp.GetMember(ControlUIElement, "Header");
                if (Header == null)
                {
                    MarsLoggerSimple.Error("\t", strError = string.Format("wrong version? no Header member in [{0}]", ControlUIElement.GetType()));
                    strError = "Object property [Header] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                MarsLoggerSimple.Info("\t", string.Format("Groupbox header's type is:[{0}]", Header.GetType()));
                var ExpansionElem = ReflectorForCSharp.GetMember(Header, "ExpansionElem");
                if (ExpansionElem == null)
                {//注意，这个地方可能为空，因为可能没有扩展的，但是如果调用该函数，默认是需要该区域的
                    MarsLoggerSimple.Error("\t", strError = string.Format("wrong version? no ExpansionElem member in [{0}]", Header.GetType()));
                    strError = "Object property [ExpansionElem] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                var rect = ReflectorForCSharp.GetMember(ExpansionElem, "Rect");
                if ((rect == null) || (!(rect is Rectangle)))
                {
                    MarsLoggerSimple.Error("\t", strError = string.Format("wrong version? no rect member in [{0}]", ExpansionElem.GetType()));
                    strError = rect == null ? "Object property [Rect] is null" : "Object member [Rect]'s type is not Rectangle";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }

                //判断是否需要点击
                ReflectorForCSharp op = new ReflectorForCSharp();
                MarsLoggerSimple.Info("\t", string.Format("going to set value:[{0}]", valueToSet));
                Rectangle rectSrc = (Rectangle)rect,
                    rectScrn = btn.RectangleToScreen(rectSrc);
                MarsLoggerSimple.Info("\t", string.Format("source recttangle:[{0}] screen:[{1}]", rectSrc, rectSrc));
                Thread.Sleep(50);
                MarsWindowsAPIsExtend.LeftMouseClick(rectScrn.Left + rectScrn.Width / 2, rectScrn.Top + rectScrn.Height / 2);
                Thread.Sleep(50);
                //op.CallMethod(btn, "SetExpanded", new object[] {valueToSet, true });

                //isOk = op.SetMemberValue(valueToSet, btn, "Expanded", ref strError);
                return true;
            }


            if (btn == null)
            {
                strError = $"Object [{strObjName}] is not found.";// string.Format("more than one objects exist. null -[{0}] values:[{1}]", string.Concat(",", objProperties.Keys), string.Concat(",", objProperties.Values));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Make sure [{strObjName}] exists and is visibal; Make sure [{strObjName}] identification is correct. Use Object Spy to identify the problem";
                return false;
            }
            //普通的button
            if (btn.InvokeRequired)
            {
                try
                {
                    
                    Thread t = new Thread(() =>
                    {
                        btn.Invoke(new Action(() =>
                        {
                            var bnd = btn.Bounds;
                            simpleLog.MarsLoggerSimple.Info("CLICKBUTTON", $"control.left:{btn.Left}, bound.left:[{bnd.Left}], x,y:[{btn.Width/2}, {btn.Height/2}] screen x,y [{btn.PointToScreen(new Point(btn.Width/2,btn.Height/2))}]");
                            //int x = btn.Left + btn.Width / 2, y = btn.Top + btn.Height / 2;
                            int x = bnd.Left + btn.Width / 2, y = bnd.Top + bnd.Height / 2;
                            System.Drawing.Point ptNew = btn.Parent.PointToScreen(new System.Drawing.Point(x, y));
                            //System.Drawing.Point ptNew = btn.PointToScreen(new System.Drawing.Point(x, y));
                            MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                        }));
                    });
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                    t.Join();
                }
                catch (Exception err)
                {
                    MarsLoggerSimple.Error("AppsideKeywordDeal_ClickButton", err.Message,err);
                }
            }
            else
            {
                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() => {
                var bnd = btn.Bounds;
                simpleLog.MarsLoggerSimple.Info("CLICKBUTTON", $"control.left:{btn.Left}, bound.left:[{bnd.Left}], x,y:[{btn.Width / 2}, {btn.Height / 2}] screen x,y [{btn.PointToScreen(new Point(btn.Width / 2, btn.Height / 2))}]");
                //int x = btn.Left + btn.Width / 2, y = btn.Top + btn.Height / 2;
                int x = bnd.Left + btn.Width / 2, y = bnd.Top + bnd.Height / 2;
                //btn.Focus();
                System.Drawing.Point ptNew = btn.Parent.PointToScreen(new System.Drawing.Point(x, y));
                //System.Drawing.Point ptNew = btn.PointToScreen(new System.Drawing.Point(x, y));
                MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                //}));
            }
            MarsLoggerSimple.logEnd("AppsideKeywordDeal_ClickButton");
            return true;
        }

        private static bool WaitforControlCanFocusOrCanSelect(object v, string strPegName, string strObjName, ref string strError, ref string strAdv, ref string strStack)
        {
            double dTime = ConfigReading.GetDefaultWaitTime();
            if (v == null)
            {
                strError = "Passing null object to a function";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            if (!(v is System.Windows.Forms.Control))
            {
                simpleLog.MarsLoggerSimple.Equals("WaitforControlCanFocusOrCanSelect", string.Format("WaitforControlCanFocusOrCanSelect-object is not a control, which is :[{0}]-{1}.{2}", v.GetType().ToString(), strPegName, strObjName));
                strError = $"Object [{strObjName}] is not a Control.";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            System.Windows.Forms.Control c = (System.Windows.Forms.Control)v;
            if (dTime <= 0) dTime = 120;
            long t1 = DateTime.Now.Ticks;
            long td = 0;
            while ((td < dTime) && (!(c.CanSelect || c.CanFocus)))
            {
                Thread.Sleep(100);
                long t2 = DateTime.Now.Ticks;
                td = (t2 - t1) / TimeSpan.TicksPerSecond;
            }
            if (c.Visible && c.Enabled)
            {
                //if (c.CanFocus)
                //    c.Focus();
                Thread.Sleep(50);
                return true;
            }

            if (!c.CanSelect)
            {
                strError = $"Object [{strObjName}]'s property CanSelect is false";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
            }
            if (!c.CanFocus)
            {
                strError += $";Object [{strObjName}]'s CanFocus is False";
                strStack += MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
            }
            return false;
        }


        //internal static bool SafeIsHandleCreated(Control control)
        //{
        //    if (control.InvokeRequired)
        //    {
        //        // If the call is from a non-UI thread, marshal it to the UI thread
        //        return (bool)control.Invoke(new Func<bool>(() => control.IsHandleCreated));
        //    }
        //    else
        //    {
        //        // If it's already on the UI thread, access IsHandleCreated directly
        //        return control.IsHandleCreated;
        //    }
        //}

        private static bool SafeIsEnabled(Control control)
        {
            if (control.InvokeRequired)
            {
                bool isOk = false;
                // Invoke on the UI thread
                control.Invoke(new Action(() => isOk = control.Enabled));
                return isOk;
            }
            else
            {
                // Access directly on the UI thread
                return control.Enabled;
            }
        }

        private static bool SafeIsVisible(Control control)
        {
            if (control.InvokeRequired)
            {
                bool isOk = false;
                // Invoke on the UI thread
                control.Invoke(
                    new Action(() => { isOk = control.Visible; }));
                return isOk;
                    
            }
            else
            {
                // Access directly on the UI thread
                return control.Visible;
            }
        }

        private static bool SafeIsVisibleAndEnable(Control control)
        {
            if (control.InvokeRequired)
            {
                bool isOk = false;
                // Invoke on the UI thread
                control.Invoke(new Action(() => isOk = control.Visible && control.Enabled));
                return isOk;
            }
            else
            {
                // Access directly on the UI thread
                return control.Visible && control.Enabled;
            }
        }

        private static bool WaitForControlIsVisibleAndEnable(object v, string strPegName, string strObjName, ref string strError,
            ref string strAdv,
            ref string strStack,
            bool isIgnoreVisible = false, 
            int waitingTime = -1)
        {
            double dTime = waitingTime<0?ConfigReading.GetDefaultWaitTime():waitingTime;
            if (v == null)
            {
                strError = "Passing null object to a function";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            if (!(v is System.Windows.Forms.Control))
            {
                simpleLog.MarsLoggerSimple.Error("WaitForControlIsVisibleAndEnable", string.Format("object is not a control, which is :[{0}]", v.GetType().ToString()));
                strError = $"Object [{strObjName}] is not a Control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            System.Windows.Forms.Control c = (System.Windows.Forms.Control)v;
            if (dTime <= 0) dTime = 120;
            long t1 = DateTime.Now.Ticks;
            long td = 0;

            bool isHandleCreatedLogged = false;
            while ((td < dTime) && (!(c.IsHandleCreated)))
            {
                if (!isHandleCreatedLogged)
                {
                    isHandleCreatedLogged = true;
                    simpleLog.MarsLoggerSimple.Info("\t", "object handle is not created");
                }
                Thread.Sleep(100);
                long t2 = DateTime.Now.Ticks;
                td = (t2 - t1) / TimeSpan.TicksPerSecond;
            }

            t1 = DateTime.Now.Ticks;
            td = 0;

            if (!isIgnoreVisible)
            {
                while ((td < dTime) && (!(SafeIsVisibleAndEnable(c))))
                {
                    Thread.Sleep(100);
                    long t2 = DateTime.Now.Ticks;
                    td = (t2 - t1) / TimeSpan.TicksPerSecond;
                }
                if (SafeIsVisibleAndEnable(c)) //  c.Visible && c.Enabled)
                {
                    simpleLog.MarsLoggerSimple.Info("WaitForControlIsVisibleAndEnable", "control is visible and enable");
                    //if (c.CanFocus)
                    //    c.Focus();
                    Thread.Sleep(50);
                    return true;
                }
            }
            else
            {
                while ((td < dTime) && (!(SafeIsEnabled(c))))
                {
                    Thread.Sleep(100);
                    long t2 = DateTime.Now.Ticks;
                    td = (t2 - t1) / TimeSpan.TicksPerSecond;
                }
                if (c.Enabled)
                {
                    //if (c.CanFocus)
                    //    c.Focus();
                    Thread.Sleep(50);
                    return true;
                }
            }

            if (!isIgnoreVisible)
                if (!c.Visible)
                {
                    strError = $"Object [{strObjName}] is not found/visible"; //"Object is not visible;";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure peg window exists and is visibale; Make sure window identification is correct. Use Object Spy to identify the problem";
                }
            if (!SafeIsEnabled(c))
            {
                strError += $"Object [{strObjName}] is not Enabled";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure peg window exists and is visibale; Make sure window identification is correct.Use Object Spy to identify the problem";
            }
            simpleLog.MarsLoggerSimple.Error("WaitForControlIsVisibleAndEnable", "control is not visible and enable");
            return false;
        }

        private static bool GetCurrentControlsFilteredByType(string strTypeName,
            string strPegName, string strObjName,
            Dictionary<string, string> objPegProperties,
            Dictionary<string, string> objProperties,
            List<object> resultObj,
            ref string strError,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            List<object> lstFromFrom = null,
            int iWaitTime = 180,
            bool isDisplayFormObjects = false)
        {
            simpleLog.MarsLoggerSimple.logBegin("GetCurrentControlsFilteredByType", $"{strTypeName}, {strPegName}, {strObjName}");
            bool isOk = false;
            object oForm = null;

            /// firstly wait for process is not busy
            /// 
            MarsWindowsAPIsExtend.WaitForCurrentProcessResponse(10);

            try
            {
                List<object> lstChild = null;
                if (lstFromFrom == null)
                {
                    var frmTarget = ReGetForm(CurrentPegwindowType, objPegProperties, strPegName, strObjName, ref oForm, ref isOk, ref strError,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile,
                        iWaitTime);
                    if (!isOk)
                    {
                        if (string.IsNullOrEmpty(strError))
                            strError = "ReGetForm return false";
                        simpleLog.MarsLoggerSimple.Error("GetCurrentControlsFilteredByType", strError);
                        return false;
                    }
                    else
                    {
                        CurrentPegWindows.Clear();
                        CurrentPegWindows.Add(frmTarget);
                    }

                    //make sure the window is not busy
                    IntPtr lpRslt;
                    MarsWindowsAPIs.SendMessageTimeout( //new HandleRef(frmTarget, frmTarget.WindowHandle),
                        frmTarget.WindowHandle,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
                        (uint)iWaitTime, //5000,
                        out lpRslt);
                    IntPtr pNewHandle = IntPtr.Zero;

                    if ((CurrentPegWindows == null) || (CurrentPegWindows.Count == 0))
                    {
                        strError = "PegWindow is not specified";//"Should call keyword Pegwindow first or No pegwindow is found";
                        simpleLog.MarsLoggerSimple.Error("GetCurrentControlsFilteredByType", strError);
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Make sure there is Peg window test step before this test step and there exist peg window for all applications supported by this test case"; ;
                        return false;
                    }
                    if (CurrentPegWindows.Count > 1)
                    {
                        strError = $"More than on peg window for [{strPegName}] found."; ;
                        simpleLog.MarsLoggerSimple.Error("GetCurrentControlsFilteredByType", strError);
                        //strError = "More than one parent windows are existing.";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = $"Make sure only one peg window with identification for {strPegName} pegwindow is found on the screen";
                        return false;
                    }
                    ///算法 
                    /// 1，首先获得所有的子对象
                    /// 2，依据已经mapping的类，处理对象的信息
                    ///             

                    lstChild = GetAllChildrenFromParent(CurrentPegWindows[0], strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    if (!isOk)
                    {
                        MarsLoggerSimple.Error("GetCurrentControlsFilteredByType", string.Format("Error when call GetAllChildrenFromParent :[{0}] for Type:[{1}]", strError, strTypeName));

                        return false;
                    }
                }
                else
                {
                    lstChild = new List<object>();
                    lstChild.AddRange(lstFromFrom);
                }

                if (isDisplayFormObjects)
                {
                    simpleLog.MarsLoggerSimple.Info("\t", $"try to debug all object informations, pegwindow type:"
                        + CurrentPegWindows[0]==null?$"NULL":$"[{CurrentPegWindows[0].GetType()}]");
                    List<System.Windows.Forms.Control> lc = lstChild.Where(p => p is System.Windows.Forms.Control).Cast<System.Windows.Forms.Control>().ToList();
                    var lcOrdered = lc.OrderBy(p => p.Bounds.Top).ThenBy(p => p.Bounds.Left).ToList();
                    foreach (var itm in lcOrdered)
                    {
                        if (!(itm is System.Windows.Forms.Control)) continue;

                        MarsLoggerSimple.Info("\t", string.Format("DEBUG OBJECT: {3}\r\n\tcontrol [{0}], type:[{1}] TypeName:[{2}] objectName:[{3}] bounds:[{4}]\r\nisvisible:[{5}] enabled:[{6}] CanFocus:[{7}] parentNames:[{8}]",
                            ReflectorForCSharp.GetTypeAndItsAncestor(itm.GetType()),
                            ((System.Windows.Forms.Control)(itm)).GetType().ToString(), strTypeName,
                            ((System.Windows.Forms.Control)(itm)).Name,
                            ((System.Windows.Forms.Control)(itm)).Bounds,
                            ((System.Windows.Forms.Control)(itm)).Visible,
                            ((System.Windows.Forms.Control)(itm)).Enabled,
                            ((System.Windows.Forms.Control)(itm)).CanFocus,
                            MarsformIndentifier.MarsGetParentsNames((System.Windows.Forms.Control)(itm)),
                            MarsformIndentifier.GetParentTypePath((System.Windows.Forms.Control)(itm))

                            ));
                    }
                }

                string strKeysObj = string.Join(",", objProperties.Keys);
                simpleLog.MarsLoggerSimple.Info("GetCurrentControlsFilteredByType", $"all keys to test|{strKeysObj}");

                if (string.Compare(strTypeName, "Pegwindow", true) == 0)
                {
                    //then use finded pegwindow
                    resultObj.Add(CurrentPegWindows[0].AssignedForm);
                }
                else
                {
                    //List<object> objCntrolsType = MarsObjectSupportTypesMgr.GetSupportedObjectType(strTypeName, ref isOk, ref strError);
                    //if (!isOk)
                    //{
                    //    MarsLoggerSimple.Error("GetCurrentControlsFilteredByType", strError);
                    //    return false;
                    //}

                    string strswfNameKey = "";
                    bool isSwfNameInclude = false;
                    foreach (string k in objProperties.Keys)
                    {
                        if (string.Compare(k, "swfname", true) == 0)
                        {
                            strswfNameKey = k;
                            isSwfNameInclude = true;
                            break;
                        }
                    }

                    simpleLog.MarsLoggerSimple.Info("GetCurrentControlsFilteredByType",$"before filter object name, total child is: [{lstChild.Count}]");
                    if (isDisplayFormObjects)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", "debug objects.......");
                        foreach (var itm in lstChild)
                        {
                            if (!(itm is System.Windows.Forms.Control)) continue;

                            MarsLoggerSimple.Info("\t", string.Format("DEBUG child OBJECT: {3}\r\n\tcontrol [{0}], type:[{1}] TypeName:[{2}] objectName:[{3}] bounds:[{4}]\r\nisvisible:[{5}] enabled:[{6}] CanFocus:[{7}] parentNames:[{8}]",
                                ReflectorForCSharp.GetTypeAndItsAncestor(itm.GetType()),
                                ((System.Windows.Forms.Control)(itm)).GetType().ToString(), strTypeName,
                                ((System.Windows.Forms.Control)(itm)).Name,
                                ((System.Windows.Forms.Control)(itm)).Bounds,
                                ((System.Windows.Forms.Control)(itm)).Visible,
                                ((System.Windows.Forms.Control)(itm)).Enabled,
                                ((System.Windows.Forms.Control)(itm)).CanFocus,
                                MarsformIndentifier.MarsGetParentsNames((System.Windows.Forms.Control)(itm)),
                                MarsformIndentifier.GetParentTypePath((System.Windows.Forms.Control)(itm))

                                ));
                        }
                    }
                    if (!string.IsNullOrEmpty(strswfNameKey))
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", $"check swfName:[{strswfNameKey}]");

                        lstChild = lstChild.Where(p => (p is System.Windows.Forms.Control))
                            .Where(p => MarsWindowsAPIsExtend.RegularTest(objProperties[strswfNameKey], ((System.Windows.Forms.Control)p).Name)
                            || (string.Compare(((System.Windows.Forms.Control)p).Name, objProperties[strswfNameKey], true) == 0)).ToList();
                        simpleLog.MarsLoggerSimple.Info("GetCurrentControlsFilteredByType",
                       $"after filter by object name [{strswfNameKey}]-[{objProperties[strswfNameKey] ?? "N/A"}], total child is: [{lstChild.Count}]");

                    }
                    else
                    {
                        /// 有可能strswfNameKey是空，因为对象不需要使用name
                        /// 
                        simpleLog.MarsLoggerSimple.Info("\t", $"all object items:[{strKeysObj}]");
                    }

                    if (isSwfNameInclude && (lstChild.Count == 1) && (objProperties.Keys.Count == 1))
                    {
                        resultObj.AddRange(lstChild);
                        return true;
                    }

                    //var objs = (from supportedType in objCntrolsType
                    //            from o1 in lstChild
                    //                //where ((supportedType is Type) && (Convert.ChangeType(lstChild[0], (Type)supportedType)!= null))
                    //            where ((supportedType is Type) && ((o1.GetType().IsSubclassOf((Type)supportedType)) || (o1.GetType() == ((Type)supportedType))))
                    //            || ((supportedType is string) && ReflectorForCSharp.GetObjectBaseType(o1.GetType()).Contains(supportedType as string))
                    //            || ((supportedType is string) && (string.Compare(o1.GetType().ToString(), (string)supportedType, true) == 0))
                    //            select o1).Distinct().ToList();
                    var objs = lstChild.Distinct().ToList();
                    MarsLoggerSimple.Info("GetCurrentControlsFilteredByType", string.Format("Find objects matchs {1} Type:[{0}]", objs == null ? 0 : objs.Count, strTypeName));
                    if ((objs != null) && (objs.Count > 0))
                    {
                        int iIdx = objs.Count - 1;
                        while ((iIdx >= 0) && (objs.Count > 0))
                        {
                            object o = objs[iIdx];
                            if (o == null)
                            {
                                objs.RemoveAt(iIdx);
                                iIdx -= 1;
                                continue;
                            }

                            System.Windows.Forms.Control c = o as System.Windows.Forms.Control;
                            MarsLoggerSimple.Info("\t", string.Format("Current object Type:[{0}]-Name:[{1}] ", o.GetType().ToString(), c.Name));
                            isOk = false;
                            MarsformIndentifier objMarsWithProperties = null;
                            string strTmpError = "",
                                strAdvTmp = "",
                                strStackTmp = "";
                            if (c != null)
                            {
                                if (c.InvokeRequired)
                                {
                                    c.Invoke(new Action(() =>
                                    {
                                        objMarsWithProperties = MarsformIndentifier.FetchControlInfomation((System.Windows.Forms.Control)o, objProperties,
                                            strPegName, strObjName,
                                            ref isOk, ref strTmpError,
                                            ref strAdvTmp,
                                            ref strStackTmp);
                                    }));
                                    strError = strTmpError;
                                    strAdv = strAdvTmp;
                                    strStack = strStackTmp;
                                }
                                else
                                {
                                    objMarsWithProperties = MarsformIndentifier.FetchControlInfomation((System.Windows.Forms.Control)o, objProperties,
                                        strPegName, strObjName,
                                        ref isOk, ref strError,
                                        ref strAdv,
                                        ref strStack);
                                }
                                if (!isOk)
                                {
                                    objs.RemoveAt(iIdx);
                                    iIdx -= 1;
                                    continue;
                                }
                                else
                                {
                                    iIdx -= 1;
                                }
                            }
                            else
                            {
                                MarsLoggerSimple.Error("\t", string.Format("Object is not A standard control, its [{0}]", o.GetType().ToString()));
                                objs.RemoveAt(iIdx);
                                iIdx -= 1;
                                continue;
                            }
                        }
                    }

                    string strIndex = objProperties.Keys.Where(p => string.Compare(p, "index", true) == 0).FirstOrDefault();
                    simpleLog.MarsLoggerSimple.Info("GetCurrentControlsFilteredByType", $"try to test INDEX|{strIndex}|");
                    if (!string.IsNullOrEmpty(strIndex))
                    {
                        List<System.Windows.Forms.Control> lstCntrl = objs.Where(p => (p as System.Windows.Forms.Control) != null).Cast<System.Windows.Forms.Control>().ToList();
                        simpleLog.MarsLoggerSimple.Info("GetCurrentControlsFilteredByType", $"before filter|{objs.Count}|after|{lstCntrl.Count}|");
                        if (objs.Count != lstCntrl.Count)
                        {
                            objs.Where(p => !(p is System.Windows.Forms.Control))
                                .ToList()
                                .ForEach((itm) => {
                                    if (itm!=null)
                                        simpleLog.MarsLoggerSimple.Error("GetCurrentControlsFilteredByType is not control", itm.GetType().FullName);
                                });
                        }
                        lstCntrl = lstCntrl.OrderBy(p => p.TabIndex).ToList();
                        int iLocation;
                        if (int.TryParse(objProperties[strIndex], out iLocation))
                        {
                            simpleLog.MarsLoggerSimple.Info("GetCurrentControlsFilteredByType", $"{strIndex}|{iLocation}|Lst count|{lstCntrl.Count}|");
                            if ((iLocation >= 0) && (iLocation < lstCntrl.Count))
                            {
                                resultObj.Clear();
                                resultObj.Add(lstCntrl[iLocation]);
                                isOk = true;
                                return true;
                            }
                        }
                        else
                        {
                            simpleLog.MarsLoggerSimple.Error("GetCurrentControlsFilteredByType", $"index is not a number|{objProperties[strIndex]}|");
                        }
                        strError = $"Object Index is greater or equal to number of objects for [{strPegName}].[{strObjName}].";//string.Format("Unable to locate the object for index :[{0}]", objProperties[strIndex]);
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Check index in Object identification.";
                        isOk = false;
                        return false;
                    }

                    strIndex = objProperties.Keys.Where(p => string.Compare(p, "location", true) == 0).FirstOrDefault();
                    if (!string.IsNullOrEmpty(strIndex))
                    {
                        List<System.Windows.Forms.Control> lstCntrl = objs.Where(p => (p as System.Windows.Forms.Control) != null).Cast<System.Windows.Forms.Control>().ToList();
                        //List<System.Windows.Forms.Control> lstCntrl = new List<System.Windows.Forms.Control>();
                        List<KeyValuePair<System.Windows.Forms.Control, Rectangle>> lstCntrlWithClientPos = new List<KeyValuePair<Control, Rectangle>>();
                        foreach (var itm in lstCntrl)
                        {
                            if (itm == null) continue;
                            Rectangle clentRect = itm.Parent == null ? itm.RectangleToScreen(itm.Bounds) : itm.Parent.RectangleToScreen(itm.Bounds);
                            lstCntrlWithClientPos.Add(new KeyValuePair<Control, Rectangle>(itm, clentRect));
                        }
                        lstCntrl = lstCntrlWithClientPos
                            .OrderBy(p => p.Value.Y)
                            .ThenBy(p => p.Value.X)
                            .Select(p => p.Key)
                            .ToList();
                        //lstCntrl = lstCntrl.OrderBy(p => p.Bounds.X).ThenBy(p=>p.Bounds.Y).ToList();
                        int iLocation;
                        if (int.TryParse(objProperties[strIndex], out iLocation))
                        {
                            if ((iLocation >= 0) && (iLocation < lstCntrl.Count))
                            {
                                resultObj.Clear();
                                resultObj.Add(lstCntrl[iLocation]);
                                return true;
                            }
                        }
                        strError = $"Object[{strObjName}] is not found in PegWindow[{strPegName}] location[{objProperties[strIndex]}]"; // string.Format("Unable to locate the object for location :[{0}]", objProperties[strIndex]);
                        strAdv = $"Object not found using location (x, y location)";
                        isOk = false;
                        return false;
                    }

                    if ((objs == null) || (objs.Count == 0))
                    {
                        strError = $"Object[{strObjName}] is not found in PegWindow[{strPegName}]";// "no object is matched.";
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Make sure object is visible on the screen.";
                        return false;
                    }

                    resultObj.AddRange(objs);
                }
                return true;
            }
            finally
            {
                if ((resultObj != null) && (resultObj.Count == 1))
                {
                    try
                    {
                        if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.WaitForControlHandlerCreate((System.Windows.Forms.Control)resultObj[0])){
                            simpleLog.MarsLoggerSimple.Error("GetCurrentControlsFilteredByType","have WaitForControlHandlerCreate but failed");
                        }
                    }
                    catch (Exception x)
                    {
                        simpleLog.MarsLoggerSimple.Error("GetCurrentControlsFilteredByType",x.Message, x.StackTrace);
                    }
                }
            }
        }

        private static bool AppsideKeywordDeal_MarximizeWindow(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            if ((CurrentPegWindows == null) || (CurrentPegWindows.Count == 0))
            {
                strError = "PegWindow is not specified";//"Should call keyword Pegwindow first or No pegwindow is found";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure there is Peg window test step before this test step and there exist peg window for all applications supported by this test case";
                return false;
            }
            if (CurrentPegWindows.Count > 1)
            {
                strError = $"More than on peg window for [{strPegName}] found"; //"More than one parent windows are existing.";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure only one peg window with identification for [NAME] is found on the screen";
                return false;
            }
            bool isOk = false;
            object oForm = null;
            var frmTarget = ReGetForm(CurrentPegwindowType, objPegProperties, strPegName, strObjName, ref oForm, ref isOk,
                ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile
                );
            if ((frmTarget == null) || ((oForm as Control) == null))
            {
                MarsWindowsAPIsExtend.MaximizeWidow(CurrentPegWindows[0].WindowHandle);
            }
            else
                MarsWindowsAPIsExtend.MaximizeWidow((oForm as Control).Handle);
            if (string.IsNullOrEmpty(strParaMeter))
            {

            }
            else
            {
                string[] xy = strParaMeter.Split(',');
                if (xy.Length != 2) return true;

                int xOff, yOff;
                if ((!int.TryParse(xy[0], out xOff)) || (int.TryParse(xy[1], out yOff)))
                { return true; }
                try
                {
                    var objTargetScreen = Screen.AllScreens[0];

                    int xLeft = objTargetScreen.Bounds.Width - xOff;
                    int yTop = (objTargetScreen.Bounds.Height - yOff);
                    MarsWindowsAPIs.SetWindowPos(CurrentPegWindows[0].WindowHandle, new IntPtr(0), xOff, yTop, xLeft, yTop,
                        MarsWindowsAPIs.SWP_SHOWWINDOW);
                }
                catch (Exception e)
                {
                    simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_MarximizeWindow", string.Format("Exception:[{0}]", e.Message), e);
                }

            }

            return true;
        }

        private static List<IntPtr> GetCurrentFormsHandle()
        {
            System.Windows.Forms.FormCollection allForms = System.Windows.Forms.Application.OpenForms;
            List<IntPtr> lstOldHandlers = new List<IntPtr>();
            for (int i = 0; i < (allForms == null ? -1 : allForms.Count); i++)
            {
                if (allForms[i] == null) continue;
                try
                {
                    lstOldHandlers.Add(allForms[i].Handle);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return lstOldHandlers;
        }

        private static void BringFormToFrontByHandle(IntPtr pHandle)
        {
            System.Windows.Forms.FormCollection allForms = System.Windows.Forms.Application.OpenForms;
            MarsWindowsAPIs.SetForegroundWindow(pHandle);
        }

        private static System.Windows.Forms.Control GetToolbar(string strobjType,
            string strPegName, string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            ref string strError,
            ref bool isOk,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isDisplayChildrenInfo = false)
        {
            try
            {
                MarsLoggerSimple.logBegin("GetToolbar", string.Format("object properties:[{0}]", MarsWindowsAPIsExtend.Dic2String(objProperties)));
                System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();
                isOk = false;
                List<object> lstControls = new List<object>();
                #region 分析pegwindow信息
                if (objPegProperties.Keys.Count == 0)
                {
                    ///采用default window信息
                    /// 
                    string strPegInfoDefault = ConfigReading.GetDefaultWindows();
                    if (!string.IsNullOrEmpty(strPegInfoDefault))
                    {
                        string[] arrPeg = strPegInfoDefault.Split(';');
                        foreach (var s in arrPeg)
                        {
                            if (string.IsNullOrEmpty(s)) continue;
                            string[] arrIds = s.Split(new string[] { ":=" }, StringSplitOptions.RemoveEmptyEntries);
                            if (arrIds.Length != 2)
                            {
                                MarsLoggerSimple.Error("\t", string.Format("default window ids format is wrong. [{0}]", strPegInfoDefault));
                                objPegProperties.Clear();
                                break;
                            }
                            objPegProperties.Add(arrIds[0], arrIds[1]);
                        }
                    }
                }

                List<IntPtr> lstOldHandlers = GetCurrentFormsHandle();
                System.Windows.Forms.FormCollection allForms = System.Windows.Forms.Application.OpenForms;

                if (objPegProperties.Keys.Count == 0)
                {
                    //依旧没有数据，直接使用主窗口                                

                    for (int i = 0; i < allForms.Count; i++)
                    {
                        if (allForms[i] == null) continue;
                        if ((!MarsWindowsAPIs.IsWindowVisible(allForms[i].Handle)) || (allForms[i].Handle != objCurP.MainWindowHandle)) continue;
                        try
                        {
                            List<object> lstSubControls = new List<object>();
                            MarsRecursiveGetAllChildren(allForms[i], lstSubControls);
                            isOk = GetCurrentControlsFilteredByType(strobjType == null ? "SWFTOOLBAR" : strobjType.ToUpper(), strPegName, strObjName, objPegProperties, objProperties, lstSubControls, ref strError,
                                ref strStack,
                                ref strAdv,
                                ref strSnapshotForShouldBeFile,
                                lstControls);
                            if (!isOk)
                            {
                                MarsLoggerSimple.Error("\t", string.Format("return false,with error:[{0}]", strError));
                                return null;
                            }
                        }
                        catch (Exception e)
                        {
                            MarsLoggerSimple.Error("\t", string.Format("when get all children from the main form. exception:[{0}]", e.Message));
                        }

                        break;
                    }
                }
                else
                {
                    isOk = GetCurrentControlsFilteredByType(strobjType == null ? "SWFTOOLBAR" : strobjType.ToUpper(), strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile,
                        null,
                        180,
                        isDisplayChildrenInfo
                        );
                    if (!isOk)
                    {
                        MarsLoggerSimple.Error("\t", string.Format("objPegProperties.Keys.Count!=0,return false,with error:[{0}]", strError));
                        return null;
                    }
                }
                #endregion //分析pegwindow信息

                #region 找到toolbars等object
                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object[{strObjName}] is not found in PegWindow[{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return null;
                }
                System.Windows.Forms.Control targetCntrl = null;
                if (lstControls.Count != 1)
                {
                    //判断是否存在index location等辅助信息
                    string sK = objProperties.Keys.Where(p => string.Compare(p, "index", true) == 0).FirstOrDefault();
                    if (string.IsNullOrEmpty(sK))
                    {
                        strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                        isOk = false;
                        return null;
                    }
                    int iIdx;
                    if (int.TryParse(objProperties[sK], out iIdx))
                    {
                        strError = $"index of [{strObjName}] is Not a number";//, objProperties[sK]);
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Check index in Object identification.";
                        isOk = false;
                        return null;
                    }
                    if (iIdx >= lstControls.Count)
                    {
                        strError = $"Object Index is greater or equal to number of objects for [{strPegName}].[{strObjName}]";// string.Format("No such index [{0}] for object sorting,total count is [{1}]", objProperties[sK], lstControls.Count);
                        StackFrame stck = (new StackFrame());
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Check index in Object identification.";
                        isOk = false;
                        return null;
                    }
                    targetCntrl = lstControls[iIdx] as System.Windows.Forms.Control;
                }
                else
                {
                    targetCntrl = lstControls[0] as System.Windows.Forms.Control;
                }


                if (targetCntrl == null)
                {
                    strError = "Can't find target Tool";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure toolbar identifiacation is correct";
                    isOk = false;
                    return null;
                }
                #endregion //找到toolbars等object
                isOk = true;
                return targetCntrl;
            }catch(Exception e)
            {
                strError = e.Message;
                strStack = e.StackTrace;
                strAdv = "Contact Marquis";
                isOk = false;
                MarsLoggerSimple.Error("GetToolbar", strError, e);
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("GetToolbar", $"isOk = {isOk}");
            }
        }
        private static bool AppsideKeywordDeal_ClickMenuIcon(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
             MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.logBegin("ClickMenuIcon", string.Format("Data:[{0}] Pegwin:[{1}]", strData, MarsWindowsAPIsExtend.Dic2String(objProperties)));

            if (string.IsNullOrEmpty(strData))
            {
                strError = "Incorrect format for menu item location";// "Data is null or empty, No Menu Icon key is set. So System doesn't know which tool button is to be clicked";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "See user manual for correct menu item location use";
                return false;
            }

            bool isOk = false;
            bool isDisplayChild = (strParaMeter ?? "").ToUpper().IndexOf("DEBUG:SHOWCHILD") >= 0;
            System.Windows.Forms.Control targetCntrl = GetToolbar(strobjType, strPegName, strObjName, objProperties, objPegProperties, ref strError, ref isOk,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                isDisplayChild);
            if (!isOk) return false;

            #region 处理Infragistics信息
            if (string.Compare(targetCntrl.GetType().ToString(), "Infragistics.Win.UltraWinToolbars.UltraToolbarsDockArea", true) == 0)
            {
                #region 找到相关的toolbutton
                IntPtr lpdwResult;
                MarsToolBarOperation objToolbarObj = new MarsToolBarOperation();
                List<object> lstCntrlBelongToToolBar = new List<object>();
                for (int i = 0; i < 10; i++)
                {
                    Dictionary<string, string> dicProps = new Dictionary<string, string>();
                    if (string.IsNullOrEmpty(strParaMeter))
                    {
                        dicProps = new Dictionary<string, string>()
                        {
                        { "SharedProps;ToolTipText", strData},
                        //{ "SharedProps;Caption", strData}, //对于buttonTool，可能没有toolTip通常caption是存在的
                        { "key", strData},
                        };
                    }
                    else if (strParaMeter.ToUpper().StartsWith("LASTPRO:"))
                    {
                        string strDt = strParaMeter.Substring("LASTPRO:".Length);
                        dicProps = new Dictionary<string, string>()
                        {
                        { "SharedProps;"+strDt, strData},
                        { "key", strData},
                        };
                    } else if (strParaMeter.ToUpper().StartsWith("PORPERTIES:"))
                    {
                        string strDt = strParaMeter.Substring("PORPERTIES:".Length);
                        dicProps = new Dictionary<string, string>()
                        {
                            { strDt, strData},
                            { "key", strData},
                        };
                    } else
                    {
                        dicProps = new Dictionary<string, string>()
                        {
                        { "SharedProps;ToolTipText", strData},
                        { "key", strData},
                        };
                    }
                    
                    isOk = objToolbarObj.FindSubControlByNameOrKey(targetCntrl, lstCntrlBelongToToolBar,
                        dicProps,
                        new List<string>() {
                        "Static Desktop Toolbar"
                        },
                        ref strError,
                        ref strAdv,
                        ref strStack);
                    if (isOk) break;
                    else
                    {
                        if (i >= 10 - 1)
                        {
                            if (!isOk) return false;
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(2000);
                            //windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(targetCntrl.Handle,
                            /// 1，判断进程是否繁忙
                            MarsWindowsAPIs.SendMessageTimeout(targetCntrl.Handle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
                                120000, //2分钟
                                out lpdwResult);
                        }
                    }
                }
                if (lstCntrlBelongToToolBar.Count != 1)
                {
                    strError = "Multiple objects with the same identifier were found."; //"more than one objects exits";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure peg window exists and is visibale; Make sure window identification is correct. Use Object Spy to identify the problem";
                    return false;
                }
                string strErrorTmp = "";
                isOk = true;
                Point pt = new Point();
                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        // Find UIElement
                        var tmpUIElement = ReflectorForCSharp.GetMemberWithTimeDelay(lstCntrlBelongToToolBar[0], "UIElement", 120);
                        if (tmpUIElement == null)
                        {
                            strError = $"Object [{strObjName}] property [UIElement] is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            isOk = false;
                            return false;
                        }
                        var tmpCntrlFromUI = ReflectorForCSharp.GetMember(tmpUIElement, "Control");
                        if (tmpCntrlFromUI == null)
                        {
                            strError = "Object property Control is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            isOk = false;
                            return false;
                        }
                        ((Control)tmpCntrlFromUI).Focus();
                        Thread.Sleep(200);
                        var tmpBounds = ReflectorForCSharp.GetMember(lstCntrlBelongToToolBar[0], "Bounds");
                        if (!(tmpBounds is System.Drawing.Rectangle))
                        {
                            strError = "Object property [Rectangle]'s value is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            isOk = false;
                            return false;
                        }
                        System.Drawing.Rectangle rct = (System.Drawing.Rectangle)tmpBounds;
                        System.Windows.Forms.Control c = tmpCntrlFromUI as System.Windows.Forms.Control;
                        //rct = c.Bounds;          
                        MarsLoggerSimple.Info("\t", string.Format("parent:[{0}]", c.Parent == null ? c.GetType().ToString() : c.Parent.GetType().ToString()));

                        pt = new Point(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);// c.PointToScreen(new Point(rct.X + rct.Width / 2, rct.Y + rct.Height / 2));
                        MarsLoggerSimple.Info("\t", string.Format("O(x-y):[{0}-{1}] ==> D(x-y):[{2}-{3}]", rct.X, rct.Y, pt.X, pt.Y));
                        isOk = true;
                    }
                    catch (Exception e)
                    {
                        MarsLoggerSimple.Error("\t", strErrorTmp = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace));
                        strErrorTmp = $"Errors while Selecting Menu item for [{strPegName}].[{strObjName}]";
                        strStack = $"{e.Message}\r\n{MarsErrorStacks.StackTraceDump()}";
                        strAdv = "Unidentified error. If this continues, contact Marquis";
                        isOk = false;
                    }
                }
                //));                
                #endregion 找到相关的toolbutton    
                ///click the button
                if (!isOk)
                {
                    strError = strErrorTmp;
                    return false;
                }
                //点击按钮
                //Thread.Sleep(100);

                MarsWindowsAPIsExtend.WaitForCurrentProcessResponse(2);
                simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_ClickMenuIcon", "before invoke left mouse click");
                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                targetCntrl.Invoke(new Action(() =>
                {
                    simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_ClickMenuIcon", "right before invoke left mouse click");
                    MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                }
                ));
                Thread.Sleep(100);

                return true;
            }
            #endregion 处理Infragistics信息

            strError = $"Keyword ClickMenuIcon does not support object type for [{strObjName}]";
            strAdv = "Mars supports Infragistics, WinForm and WPF controls";
            strStack = MarsErrorStacks.StackTraceDump();
            return false;
        }


        private static bool AppsideKeywordDeal_SelectMenuItem(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
             bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_SelectMenuItem", string.Format("Data:[{0}] Pegwin:[{1}]", strData, MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            List<IntPtr> lstOldHandlers = GetCurrentFormsHandle();
            System.Windows.Forms.Control targetCntrl = GetToolbar(strobjType, strPegName, strObjName, objProperties, objPegProperties, ref strError, ref isOk,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            MarsLoggerSimple.Info("\t", string.Format("after get toolbar, return [{0}], obj type:[{1}]", isOk, targetCntrl == null ? "null" : targetCntrl.GetType().ToString()));
            if (!isOk) return false;
            System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();

            #region to make sure the control is ready
            Control cntrlList = ((Control)targetCntrl);
            IntPtr timeoutRslt = IntPtr.Zero;
            IntPtr rsltTimeOut = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(cntrlList.Handle,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
                10000,
                out timeoutRslt
                );
            if (rsltTimeOut.ToInt64() != 0)
            {
                simpleLog.MarsLoggerSimple.Info("MarsListViewOperation", "send time out returns true, no thread is busy");
            }

            ((Control)targetCntrl).Update();

            System.Threading.Thread.Sleep(100);
            #endregion

            string strDataFixed = strData;
            #region 处理Infragistics信息
            if (string.Compare(targetCntrl.GetType().ToString(), "Infragistics.Win.UltraWinToolbars.UltraToolbarsDockArea", true) == 0)
            {
                bool isByFireEvent = string.IsNullOrEmpty(strParaMeter) ? false : strParaMeter.ToUpper().IndexOf("BYFIREEVENT") >= 0;

                MarsLoggerSimple.Info("\t", "check Toolbar Info UltraToolbarsDockArea");
                if (strData.StartsWith(MarsToolBarOperation.CNST_MENU_PREFIX))
                {
                    strDataFixed = strDataFixed.Substring(MarsToolBarOperation.CNST_MENU_PREFIX.Length + 1);
                }
                ///示例：Desktop Menu;Swap Trade;Maintenance;Do Past Reset
                /// 第一个Desktop Menu通常是
                string[] arrMenuItems = strDataFixed.Split(';');
                if (arrMenuItems.Length < 2)
                {
                    strError = "Incorrect format for Menu item location.";// string.Format("Menu format should be at least two items. The first should be Toolbar key, the second and after should be submenu items,but the data is :[{0}]", strData);
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "See user manual for correct Memu item location use";
                    return isOk = false;
                }
                /// 获得toolbarMgr
                /// 
                var toolBarMgr = ReflectorForCSharp.GetMember(targetCntrl, "ToolbarsManager");
                if (toolBarMgr == null)
                {
                    strError = "Object property [ToolbarsManager] is NULL";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
                ///panduan shibushi ribbon
                ///FetchControlInfomation
                //MarsToolBarOperation toolBarOp = new MarsToolBarOperation();
                //bool sbsCaiDai = toolBarOp.ShiBuShiRibbonMoShi(toolBarMgr, ref isOk ,ref strError);
                //if (!isOk)
                //{
                //    simpleLog.MarsLoggerSimple.Error();
                //}
                //if (sbsCaiDai)
                //{
                //    return false;
                //}
                //else
                {
                    ///lao moshi chuliguocheng
                    var ToolBars = ReflectorForCSharp.GetMember(toolBarMgr, "Toolbars"); // type ToolbarsCollection
                    if (ToolBars == null)
                    {
                        strError = "Object property [Toolbars] is NULL";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    var iCount = ReflectorForCSharp.GetMember(ToolBars, "Count");
                    if (!(iCount is int))
                    {
                        strError = "Object property [Count]'s type is not int";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    var toolBarList = ReflectorForCSharp.GetMember(ToolBars, "List");
                    if ((toolBarList == null) || (!(toolBarList is System.Collections.ArrayList)))
                    {
                        strError = toolBarList == null ? "Object property [List] is NULL" : "Object property [List]'s Type is not ArrayList";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Contact Marquis";
                        return false;
                    }
                    System.Collections.ArrayList arrList = (System.Collections.ArrayList)toolBarList;

                    MarsLoggerSimple.Info("\t", "check toolBarList");

                    ///可能有多个满足条件的对象
                    List<object> lstTargets = new List<object>();
                    object oMenuToolBar = null;
                    string strKeys = "";
                    foreach (var itm in arrList)
                    {
                        if (itm == null) continue;
                        var key = ReflectorForCSharp.GetMember(itm, "Key") as string;
                        strKeys += (key == null ? ";" : key + ";");
                        if (string.Compare(arrMenuItems[0], key, true) != 0) continue;
                        oMenuToolBar = itm;
                        break;
                    }
                    if (oMenuToolBar == null)
                    {
                        strError = $"Can't find [{arrMenuItems[0]}] from Object [{strObjName}]";// string.Format("Can't find tool bar with key :[{0}] from [{1}]", arrMenuItems[0], strKeys);
                        strStack = $"No menu item [{arrMenuItems[0]}] in [{strKeys}]\r\n[{MarsErrorStacks.StackTraceDump()}]";
                        strAdv = $"Make sure Object [{strPegName}].[{strObjName}] exists on the screen";
                        return isOk = false;
                    }
                    ///从oMenuToolBar中获得指定的root Menu
                    /// 
                    var oToolsForRootMenu = ReflectorForCSharp.GetMember(oMenuToolBar, "Tools");
                    if (oToolsForRootMenu == null)
                    {
                        strError = "Object property [Tools] is NULL";// string.Format("Can't find Tools From Root Menu with key :[{0}] by reflector", arrMenuItems[1]);
                        strStack = $"Object [{oMenuToolBar.GetType()}] can't find Tools \r\n{MarsErrorStacks.StackTraceDump()}";
                        strAdv = "Contact Marquis";
                        return isOk = false;
                    }
                    ///获得root menu item by 循环
                    /// 
                    var oLstFromTools = ReflectorForCSharp.GetMember(oToolsForRootMenu, "List");
                    if (!(oLstFromTools is System.Collections.ArrayList))
                    {
                        strError = $"Object property [List]'s type is not ArrayList";// string.Format("List should be ArrayList, but is :[{0}]", oLstFromTools == null ? "NULL" : oLstFromTools.GetType().ToString());
                        strStack = oLstFromTools == null ? $"List is NULL, required ArrayList\r\n{MarsErrorStacks.StackTraceDump()}" : $"List type is [{oLstFromTools.GetType()}], required ArrayList\r\n{MarsErrorStacks.StackTraceDump()}";
                        strAdv = "Contact Marquis";
                        return isOk = false;
                    }
                    MarsLoggerSimple.Info("\t", "check oToolsForRootMenu");
                    System.Collections.ArrayList lstFromTools = (System.Collections.ArrayList)oLstFromTools;
                    object oMenuRoot = null;
                    string strMenuToLookup1 = arrMenuItems[1].Replace("&", "");
                    for (int i = 0; i < lstFromTools.Count; i++)
                    {
                        var itmMenu = lstFromTools[i];
                        if (itmMenu == null) continue;
                        var oCaption = ReflectorForCSharp.GetMember(itmMenu, "CaptionAsToolTip");

                        if ((oCaption == null) || ((oCaption as string) == null)) continue;
                        string strCaption = (string)oCaption;

                        strKeys += (strCaption == null ? ";" : strCaption + ";");
                        strCaption = strCaption.Replace("&", "");

                        if (string.Compare(strCaption, strMenuToLookup1, true) != 0)
                        {
                            if (!MarsWindowsAPIsExtend.RegularTest(strMenuToLookup1, strCaption))
                                continue;
                        }
                        oMenuRoot = itmMenu;
                        break;
                    }

                    if (oMenuRoot == null)
                    {
                        strError = $"Can't find Menu Item [{arrMenuItems[1]}]";// string.Format("Can't find Menu item from tool bar:[{0}] from [{1}]", arrMenuItems[1], strKeys);
                        strStack = $"No Menu Item in [{strKeys}]\r\n{MarsErrorStacks.StackTraceDump()}";
                        strAdv = $"Make sure object [{strPegName}].[{strObjName}] is available on the screen";
                        return isOk = false;
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("find key:[{0}]", strKeys));
                    }
                    var oRectRootMenu = ReflectorForCSharp.GetMember(oMenuRoot, "Bounds");
                    var oUIElementRootMenu = ReflectorForCSharp.GetMember(oMenuRoot, "UIElement");
                    if (!(oRectRootMenu is System.Drawing.Rectangle))
                    {
                        //strError = string.Format("Bounds via Reflector from Root Menu [{0}] is not RECTANGLE, it is:[{1}]",
                        //    arrMenuItems[1], oRectRootMenu == null ? "NULL" : oRectRootMenu.GetType().ToString());
                        strError = $"Object [{strObjName}]'s property [Bounds] is not Rectangle";
                        strStack = MarsErrorStacks.StackTraceDump();
                        simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_SelectMenuItem", $"oUIElementRootMenu|error|{strError}\r\n|{strStack}");
                        strAdv = "Contact Marquis";
                        return isOk = false;
                    }
                    System.Drawing.Rectangle rectRootMenu = (System.Drawing.Rectangle)oRectRootMenu;
                    Thread.Sleep(50);

                    IntPtr lpRslt;
                    MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        MarsWindowsAPIs.SMTO_NORMAL,//SMTO_BLOCK,
                        5000,
                        out lpRslt);
                    object prnt = oMenuRoot;
                    if (isByFireEvent)
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", "beforeclick, byevent");
                        //ToolbarsManager.FireEvent(ToolbarEventIds.ToolClick, new ToolClickEventArgs(this, null));
                        ReflectorForCSharp rflct = new ReflectorForCSharp();
                        MethodInfo mthd = null;

                        /// 在toolbase中有个方法：OnToolClick
                        /// 
                        //if ((!string.IsNullOrEmpty(strParaMeter)) && (strParaMeter.IndexOf(":OnToolClick") > 0))
                        //{
                        //    //直接调用该方法
                        //    isOk = InvokeOnToolClick(ref strError);
                        //    if (isOk)
                        //    {

                        //    }
                        //}

                        try
                        {
                            mthd = rflct.GetMethod(toolBarMgr, "FireEvent");
                        }
                        catch (Exception efire)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("wrong infragistics version? get fireEvent Error:[{0}]", efire.Message), efire);
                            strError = "Object method [FireEvent] is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            return false;
                        }
                        Assembly asm = toolBarMgr.GetType().Assembly;
                        foreach (var itm in asm.GetTypes())
                        {
                            if (itm == null) continue;
                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("type:[{0}]", itm.ToString()));
                        }
                        //typeof(toolBarMgr ).Assembly
                        Type typToolclickEventArgs = asm.GetType("Infragistics.Win.UltraWinToolbars.ToolClickEventArgs");
                        if (typToolclickEventArgs == null)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Can't get type [{0}]", "Infragistics.Win.UltraWinToolbars.ToolClickEventArgs"));
                            strError = $"Object [{strObjName}]'s type assemblies are not loaded";
                            strStack = $"Can't load type [Infragistics.Win.UltraWinToolbars.ToolClickEventArgs] from application Domain\r\n{MarsErrorStacks.StackTraceDump()}";
                            strAdv = "Contact Marquis";
                            return false;
                        }
                        Type typToolBase = asm.GetType("Infragistics.Win.UltraWinToolbars.ToolBase");
                        if (typToolBase == null)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", strError = string.Format("Can't get type [{0}]", "Infragistics.Win.UltraWinToolbars.ToolBase"));
                            strError = $"Object [{strObjName}]'s type assemblies are not loaded";
                            strStack = $"Can't load type [Infragistics.Win.UltraWinToolbars.ToolBase] from application Domain\r\n{MarsErrorStacks.StackTraceDump()}";
                            strAdv = "Contact Marquis";
                            return false;
                        }
                        //object prnt = oMenuRoot;
                        string[] arrElsex = arrMenuItems.Where((p, idx) => idx > 1).ToArray();
                        MarsLoggerSimple.Info("\t", string.Format("by fireEvent the lest menu items:[{0}]", string.Join(",", arrElsex)));
                        object oTargetx = CheckObjectsFromPrnt(arrElsex, 0, prnt, strPegName, strObjName, ref isOk, ref strError, ref strAdv,
                            ref strStack, isByFireEvent,
                            isRequireDispatcher: true,
                            mthd: mthd,
                            hostManage: toolBarMgr,
                            clickEventArgs: typToolclickEventArgs,
                            typToolBase: typToolBase);
                        if (isOk)
                        {
                            MarsLoggerSimple.Info("\t", "Find and click menu item");
                            return true;
                        }
                        //typToolclickEventArgs.GetConstructor(new Type[] { int, });
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info("\t", "beforeclick, normal way");
                        //if (targetCntrl.InvokeRequired)
                        //{
                        //    targetCntrl.Invoke(new Action(() => { 

                        //    }));
                        //}
                        //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                        {
                            strParaMeter = strParaMeter ?? "";
                            if (strParaMeter.Equals("MOVE_MOUSE", StringComparison.OrdinalIgnoreCase))
                            {
                                MarsWindowsAPIsExtend.MoveMouse(rectRootMenu.X + rectRootMenu.Width / 2 - 5, rectRootMenu.Y + rectRootMenu.Height / 2 - 5);
                                Thread.Sleep(50);
                                MarsWindowsAPIsExtend.MoveMouse(rectRootMenu.X + rectRootMenu.Width / 2, rectRootMenu.Y + rectRootMenu.Height / 2);
                                Thread.Sleep(50);
                                MarsWindowsAPIsExtend.LeftMouseClick(rectRootMenu.X + rectRootMenu.Width / 2, rectRootMenu.Y + rectRootMenu.Height / 2);
                            }
                            else
                            {

                                MarsWindowsAPIsExtend.MoveMouse(rectRootMenu.X + rectRootMenu.Width / 2, rectRootMenu.Y + rectRootMenu.Height / 2);
                                Thread.Sleep(50);
                                MarsWindowsAPIsExtend.LeftMouseClick(rectRootMenu.X + rectRootMenu.Width / 2, rectRootMenu.Y + rectRootMenu.Height / 2);

                                if (strParaMeter.Equals("LEFT_DBLCLICK", StringComparison.OrdinalIgnoreCase))
                                {
                                    Thread.Sleep(50);
                                    MarsWindowsAPIsExtend.LeftMouseClick(rectRootMenu.X + rectRootMenu.Width / 2, rectRootMenu.Y + rectRootMenu.Height / 2);
                                }
                            }
                        }
                        //));
                    }
                    Thread.Sleep(150);

                    //IntPtr lpRslt;
                    MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        MarsWindowsAPIs.SMTO_BLOCK,
                        5000,
                        out lpRslt);
                    IntPtr pNewHandle = IntPtr.Zero;

                    if (arrMenuItems.Length == 2)
                    {
                        return isOk = true;
                    }
                    string[] arrElse = arrMenuItems.Where((p, idx) => idx > 1).ToArray();
                    MarsLoggerSimple.Info("\t", string.Format("the left menu items:[{0}]", string.Join(",", arrElse)));
                    object oTarget = CheckObjectsFromPrnt(arrElse, 0, prnt, strPegName, strObjName, ref isOk, ref strError,
                        ref strAdv, ref strStack,
                        isByFireEvent, isRequireDispatcher: targetCntrl.InvokeRequired);
                    if (isOk)
                    {
                        MarsLoggerSimple.Info("\t", "Find and click menu item");
                        return true;
                    }
                    return false;
                }
            }
            #endregion
            strError = $"Only Infragistics Control is supported for [SelectMenuItem]";
            strStack = $"unsupported type {targetCntrl.GetType()} for [SelectMenuItem]\r\nMarsErrorStacks.StackTraceDump()";
            strAdv = "Contact Marquis";
            return false;
        }

        private static object CheckObjectsFromPrnt(string[] arrCaptionToCheck, int iCurrntIdx, object prnt,
            string strPegName, string strObjName,
            ref bool isOk, ref string strError,
            ref string strAdv,
            ref string strStack,
            bool isByfireEvent = false,
            bool isRequireDispatcher = false,
            MethodInfo mthd = null,
            object hostManage = null,
            Type clickEventArgs = null,
            Type typToolBase = null)
        {
            simpleLog.MarsLoggerSimple.logBegin("CheckObjectsFromPrnt", string.Format("isByFireEvent:[{0}] isRequireDispatcher:[{2}] arrCaptionToCheck :[{1}]", isByfireEvent, string.Join(",", arrCaptionToCheck), isRequireDispatcher));
            bool isContinue = iCurrntIdx < (arrCaptionToCheck == null ? -1 : arrCaptionToCheck.Length);
            while (isContinue)
            {
                var subTools = ReflectorForCSharp.GetMember(prnt, "Tools");
                if (subTools == null)
                {
                    isOk = false;
                    simpleLog.MarsLoggerSimple.Error("CheckObjectsFromPrnt", strError = string.Format("No Tools from parent, type is:[{0}]", prnt.GetType()));
                    strError = "Object does not contain Tools";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }
                var lstFromTools = ReflectorForCSharp.GetMember(subTools, "List");
                if (lstFromTools == null)
                {
                    isOk = false;
                    simpleLog.MarsLoggerSimple.Error("CheckObjectsFromPrnt", strError = "No List from Tools");
                    strError = "Object does not contain List";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }
                if (!(lstFromTools is System.Collections.ArrayList))
                {
                    isOk = false;
                    simpleLog.MarsLoggerSimple.Error("CheckObjectsFromPrnt", strError = $"Tool from parent is not ArrayList, [{lstFromTools.GetType()}]");
                    strError = "Object Member \"List\"'s type is not ArrayList ";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return null;
                }
                System.Collections.ArrayList lstSubTools = (System.Collections.ArrayList)lstFromTools;
                for (int i = 0; i < lstSubTools.Count; i++)
                {
                    var objSubTool = lstSubTools[i];
                    if (objSubTool == null) continue;

                    var subToolCaption = ReflectorForCSharp.GetMember(objSubTool, "CaptionAsToolTip");
                    var subToolKey = ReflectorForCSharp.GetMember(objSubTool, "Key");
                    var subBounds = ReflectorForCSharp.GetMember(objSubTool, "Bounds");
                    string strToolCaption = subToolCaption as string,
                        strTookKey = subToolKey as string;
                    strToolCaption = strToolCaption ?? "";
                    strToolCaption = strToolCaption.Replace("&", "");
                    strTookKey = strToolCaption ?? "";
                    simpleLog.MarsLoggerSimple.Info("\t", string.Format("going to compare:[{0}] vs [{1}]"
                        , string.Format("[{0} and {1}]", strToolCaption, strTookKey), arrCaptionToCheck[iCurrntIdx]));
                    if ((string.Compare(arrCaptionToCheck[iCurrntIdx], strToolCaption, true) == 0)
                        || (string.Compare(arrCaptionToCheck[iCurrntIdx], strTookKey, true) == 0))
                    {
                        if ((isByfireEvent))
                        {
                            ConstructorInfo[] arrCons = clickEventArgs.GetConstructors();
                            foreach (var itm in arrCons)
                            {
                                if (itm == null) continue;
                                ParameterInfo[] arrPara = itm.GetParameters();
                                if (arrPara.Length <= 0) continue;
                                Type firstType = arrPara[0].GetType();
                                simpleLog.MarsLoggerSimple.Info("\t", string.Format("get constructor, first type is :[{0}]", firstType == null ? null : firstType));
                                if (firstType == typToolBase)
                                {
                                    object clickEventArgsInst = itm.Invoke(new object[] { objSubTool, null });
                                    if (clickEventArgsInst == null)
                                    {
                                        simpleLog.MarsLoggerSimple.Info("\t", strError = "Object property [clickEventArgs] value is NULL");//"Wrong infragistics version? can't get instance of clickEventArgs");
                                        strError = "Object does not contain clickEventArgs method";
                                        strStack = MarsErrorStacks.StackTraceDump();
                                        strAdv = "Contact Marquis";
                                        return false;
                                    }
                                    mthd.Invoke(hostManage, new object[] { 17, clickEventArgsInst });
                                    Thread.Sleep(500);
                                    if (iCurrntIdx == arrCaptionToCheck.Length - 1)
                                    {
                                        isOk = true;
                                        return objSubTool;
                                    }

                                    return CheckObjectsFromPrnt(arrCaptionToCheck, iCurrntIdx + 1, objSubTool, strPegName, strObjName, ref isOk, ref strError,
                                        ref strAdv, ref strStack,
                                        isByfireEvent, isRequireDispatcher, mthd, hostManage, clickEventArgs, typToolBase);
                                }
                            }

                            simpleLog.MarsLoggerSimple.Info("\t", strError = string.Format("Wrong infragistics version? can't get right constructor with parameter:[{0}]"
                                , typToolBase));
                            strError = $"Object Constructor for paramters [{typToolBase}] is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            isOk = false;
                            return null;

                        }
                        else
                        {
                            if ((subBounds == null) || (!(subBounds is System.Drawing.Rectangle)))
                            {
                                isOk = false;
                                simpleLog.MarsLoggerSimple.Error("CheckObjectsFromPrnt", strError = string.Format("Bounds from control is null or is not rectangle -[{0}]", subBounds == null ? "null" : subBounds.GetType().ToString()));
                                strError = subBounds == null ? $"Object [{strObjName}] property [Bounds]'s value is NULL" : "Member [bounds]'s type is not rectangle";
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "Contact Marquis";
                                return null;
                            }
                            if (isRequireDispatcher)
                            {
                                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                                {
                                    MarsWindowsAPIsExtend.LeftMouseClick(((System.Drawing.Rectangle)subBounds).X + ((System.Drawing.Rectangle)subBounds).Width / 2,
                                    ((System.Drawing.Rectangle)subBounds).Y + ((System.Drawing.Rectangle)subBounds).Height / 2);
                                    System.Threading.Thread.Sleep(100);
                                }));
                            }
                            else
                            {
                                MarsWindowsAPIsExtend.LeftMouseClick(((System.Drawing.Rectangle)subBounds).X + ((System.Drawing.Rectangle)subBounds).Width / 2,
                                    ((System.Drawing.Rectangle)subBounds).Y + ((System.Drawing.Rectangle)subBounds).Height / 2);
                                System.Threading.Thread.Sleep(100);
                            }
                        }

                        if (iCurrntIdx == arrCaptionToCheck.Length - 1)
                        {
                            isOk = true;
                            return objSubTool;
                        }

                        return CheckObjectsFromPrnt(arrCaptionToCheck, iCurrntIdx + 1, objSubTool, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    }
                    if (MarsWindowsAPIsExtend.RegularTest(arrCaptionToCheck[iCurrntIdx], strTookKey)
                        || MarsWindowsAPIsExtend.RegularTest(arrCaptionToCheck[iCurrntIdx], strToolCaption))
                    {
                        if ((subBounds == null) || (!(subBounds is System.Drawing.Rectangle)))
                        {
                            isOk = false;
                            strError = $"Object property [Bounds]'s type is not Rectangle";//string.Format("Bounds from control is null or is not rectangle -[{0}]", subBounds == null ? "null" : subBounds.GetType().ToString());
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            return null;
                        }
                        MarsLoggerSimple.Info("\t", "regularTest matched");
                        Thread.Sleep(100);
                        MarsWindowsAPIsExtend.LeftMouseClick(((System.Drawing.Rectangle)subBounds).X + ((System.Drawing.Rectangle)subBounds).Width / 2,
                            ((System.Drawing.Rectangle)subBounds).Y + ((System.Drawing.Rectangle)subBounds).Height / 2);
                        System.Threading.Thread.Sleep(100);

                        if (iCurrntIdx == arrCaptionToCheck.Length - 1)
                        {
                            isOk = true;
                            return objSubTool;
                        }
                        return CheckObjectsFromPrnt(arrCaptionToCheck, iCurrntIdx + 1, objSubTool, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    }

                }
                isOk = false;
                strError = $"Unable to locate the object [{strObjName}]";//string.Format("Can't find items match [{0}]", arrCaptionToCheck);
                strStack = $"Can't find items match [{arrCaptionToCheck}]\r\n{MarsErrorStacks.StackTraceDump()}";
                strAdv = "Make sure object is available on the screen";
                return null;
            }
            isOk = false;
            strError = $"Unable to locate the object [{strObjName}]";//string.Format("Can't find items match [{0}]", arrCaptionToCheck);
            strStack = $"Can't find items match [{arrCaptionToCheck}]\r\n{MarsErrorStacks.StackTraceDump()}";
            strAdv = "Make sure object is available on the screen";
            return null;
        }

        private static bool AppsideKeywordDeal_AutoCheckError(string strParaMeter, string strData, string strobjType,
            string strAttachInfo,
            string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime = -1)
        {

            bool isOkTmp = AppsideKeywordDeal_CheckError(strParaMeter, strData, strobjType, strAttachInfo,
                strPegName, strObjName, objProperties, objPegProperties,
                errorCheckObj,
                ref strError,
                ref strDataReturn,
                ref strStack,
                ref strAdv, ref strSnapshotForShouldBeFile, true, waitingTime);
            return true; // 2021,3,12 Nigel thought, should let test case move on even if error occurrs, except target control can't be found
        }

        /// <summary>
        /// 用來檢查对象是否存在特定的字符串
        /// 
        /// </summary>
        /// <param name="strParaMeter">参数可能有前缀。如：MARS_NAGETIVE:,如果不需要处理member idex，那么将默认获得Text的内容</param>
        /// <param name="strData">regular expression string to search</param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="errorCheckObj"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn">real captured data</param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_CheckText(string strParaMeter, string strData, string strobjType,
            string strAttachInfo,
            string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime = -1)
        {
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_CheckText", string.Format("Data:[{0}] Pegwin:[{1}] Obj:[{2}]", strData, MarsWindowsAPIsExtend.Dic2String(objPegProperties),
                MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            List<IntPtr> lstOldHandlers = GetCurrentFormsHandle();
            List<object> lstControls = new List<object>();

            if ((objPegProperties.Keys.Count == 0))
            {
                string strPegInfoDefault = ConfigReading.GetDefaultWindows();
                MarsLoggerSimple.Info("\t", $"defaule peg:[{strPegInfoDefault}]");
                if (!string.IsNullOrEmpty(strPegInfoDefault))
                {
                    string[] arrPeg = strPegInfoDefault.Split(';');
                    foreach (var s in arrPeg)
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        string[] arrIds = s.Split(new string[] { ":=" }, StringSplitOptions.RemoveEmptyEntries);
                        if (arrIds.Length != 2)
                        {
                            MarsLoggerSimple.Error("\t", string.Format("default window ids format is wrong. [{0}]", strPegInfoDefault));
                            objPegProperties.Clear();
                            break;
                        }
                        objPegProperties.Add(arrIds[0], arrIds[1]);
                    }
                    MarsLoggerSimple.Info("\t", $"fixed peg:{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}");
                }
            }

            ///等待60秒
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                null, waitingTime < 0 ? 60 : waitingTime);
            /// 因为没有 发现错误，所以返回true
            if (!isOk) return true;

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                /// 因为没有 发现错误，所以返回true
                return true;
            }
            lstControls = lstControls
                .Where(p => (p != null))
                .Where(p => p is Control)
                .Where(p => ((Control)p).Visible)
                .ToList();
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack, false, waitingTime / 2);
            /// 因为没有 发现错误，所以返回true
            if (!isOk) return true;
            string objectBaseType = ReflectorForCSharp.GetObjectBaseType(lstControls[0].GetType());

            bool hasErrorInfo = false;

            /// 判断是否有特殊的member 要求。在strParaMeter中
            ///             
            MARSExtPara extPara = MARSExtPara.checkParaType(strParaMeter);            
            if (string.IsNullOrEmpty(extPara.paraAfterExtract))
            {
                extPara.paraAfterExtract = "Text";
            }
            /// 通过发射获得指定member index的内容
            ///             
            bool isNotExists = false, isSafeCall = lstControls[0] is System.Windows.Forms.Control;
            var targetInfo = ReflectorForCSharp.GetMember(lstControls[0], extPara.paraAfterExtract, ref isNotExists, isSafeCall);
            if ((isNotExists)||(targetInfo==null))
            {
                strAdv = "Please change Control's property.";
                strError = $"NO such|{extPara.paraAfterExtract}| exists|\r\n{strAdv}";
                strStack = MarsErrorStacks.StackTraceDump();
                simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_CheckText", $"{strError}|\r\n{strStack}");
                return false;
            }
            strDataReturn = targetInfo.ToString();
            /// 将获得内容和data类比，regular express
            /// 
            bool isMatch = windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strData, targetInfo.ToString());
            if (isMatch)
            {
                return true;
            }
            strError = $"Get |{strParaMeter}| from selected control, but Can't match |{strData}|{strDataReturn}";
            strAdv = "please make sure that the object is right,\r\n or change data. Data is based on Regular Expression";            
            strStack = MarsErrorStacks.StackTraceDump();
            simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_CheckText", $"{strError}|{strAdv}");
            /**
            * 保存screen文件
            * */
            bool tmpOk = false;
            string strTmpError1 = "", strAdvTmp = "", strStackTmp = "";
            string tmpFileName = (new Snapshot()).SnapshotScreen(lstControls[0], strParaMeter, strPegName, strObjName, ref tmpOk, ref strTmpError1, ref strAdvTmp, ref strStackTmp);
            if ((tmpOk) && (System.IO.File.Exists(tmpFileName)))
            {
                strSnapshotForShouldBeFile = tmpFileName;
            }            
            return false;
        }
        

        private static bool AppsideKeywordDeal_CheckError(string strParaMeter, string strData, string strobjType,
            string strAttachInfo,
            string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.logBegin("CheckError", string.Format("Data:[{0}] Pegwin:[{1}] Obj:[{2}]", strData, MarsWindowsAPIsExtend.Dic2String(objPegProperties),
                MarsWindowsAPIsExtend.Dic2String(objProperties)));
            bool isOk = false;
            List<IntPtr> lstOldHandlers = GetCurrentFormsHandle();
            List<object> lstControls = new List<object>();

            if ((objPegProperties.Keys.Count == 0))
            {
                string strPegInfoDefault = ConfigReading.GetDefaultWindows();
                MarsLoggerSimple.Info("\t", $"defaule peg:[{strPegInfoDefault}]");
                if (!string.IsNullOrEmpty(strPegInfoDefault))
                {
                    string[] arrPeg = strPegInfoDefault.Split(';');
                    foreach (var s in arrPeg)
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        string[] arrIds = s.Split(new string[] { ":=" }, StringSplitOptions.RemoveEmptyEntries);
                        if (arrIds.Length != 2)
                        {
                            MarsLoggerSimple.Error("\t", string.Format("default window ids format is wrong. [{0}]", strPegInfoDefault));
                            objPegProperties.Clear();
                            break;
                        }
                        objPegProperties.Add(arrIds[0], arrIds[1]);
                    }
                    MarsLoggerSimple.Info("\t",$"fixed peg:{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}");
                }
            }

            ///等待60秒
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                null, waitingTime<0?60: waitingTime);
            /// 因为没有 发现错误，所以返回true
            if (!isOk) return true;

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                /// 因为没有 发现错误，所以返回true
                return true;
            }
            lstControls = lstControls
                .Where(p => (p != null))
                .Where(p => p is Control)
                .Where(p => ((Control)p).Visible)
                .ToList();
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack,false, waitingTime/2);
            /// 因为没有 发现错误，所以返回true
            if (!isOk) return true;
            string objectBaseType = ReflectorForCSharp.GetObjectBaseType(lstControls[0].GetType());

            bool hasErrorInfo = false;
            if (lstControls[0] is System.Windows.Forms.TreeView)
            {
                //for summit
                System.Windows.Forms.TreeView t = lstControls[0] as System.Windows.Forms.TreeView;
                //if find imageindex is 2 the eror
                string strTmpError = "";
                
                foreach (var n in t.Nodes)
                {
                    if (n == null) continue;
                    object imgidx = ReflectorForCSharp.GetMember(n, "ImageIndex");
                    if (imgidx is int)
                    {
                        if (((int)imgidx) == 2)
                        {
                            object tx = ReflectorForCSharp.GetMember(n, "Text");
                            simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_CheckError", tx == null ? "[n/a]":tx.ToString());
                            if (string.IsNullOrEmpty(strTmpError))
                            {
                                strTmpError = $"{tx}";
                            }
                            else
                            {
                                strTmpError = $"{strTmpError}\r\n{tx}";
                            }
                            //simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_CheckError", strError = $"{strError}\r\n{tx}");// string.Format("Error Info :[{0}]", tx));
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Make sure Error From summit is fixed";
                            //return false;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(strTmpError))
                {
                    strError = $"{strError}\r\nMARS ERROR BEGIN:[{strTmpError}]";
                    strDataReturn = strError;
                }
                /**
                 * 保存screen文件
                 * */
                bool tmpOk = false;
                string strTmpError1 = "", strAdvTmp="", strStackTmp = "";
                string tmpFileName = (new Snapshot()).SnapshotScreen(lstControls[0], strParaMeter, strPegName, strObjName, ref tmpOk, ref strTmpError1, ref strAdvTmp, ref strStackTmp);
                if ((tmpOk)&&(System.IO.File.Exists(tmpFileName)))
                {
                    strSnapshotForShouldBeFile = tmpFileName;
                }
                if (hasErrorInfo) return false;
                return true;
            }
            else if (!string.IsNullOrEmpty(objectBaseType) && objectBaseType.IndexOf("UltraStatusBar") >= 0)
            {
                return new MarsStatusBarOperation().GetStatusBarsText(lstControls[0], strPegName, strObjName, ref strDataReturn, ref strError, ref strAdv, ref strStack);
            }
            else
            {
                strError = $"CheckError dosen't support object type for [{strObjName}]"; //string.Format("unsupported checkError type ({0}) For summit.", lstControls[0].GetType().ToString());
                StackFrame stck = (new StackFrame());
                strStack = $"unsupported checkError type [{lstControls[0].GetType().ToString()}] For summit.\r\n{MarsErrorStacks.StackTraceDump()}";
                strAdv = "Mars supports Infragistics, WinForm and WPF controls";
                simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_CheckError", strError);
                return isOk = false;
            }
        }

        private static bool AppsideKeywordDeal_LaunchApplication(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            MarsLoggerSimple.logBegin("LaunchApplication", string.Format("Data:[{0}] Pegwin:[{1}]", strData, MarsWindowsAPIsExtend.Dic2String(objProperties)));

            bool isOk = false;
            List<IntPtr> lstOldHandlers = GetCurrentFormsHandle();
            System.Windows.Forms.Control targetCntrl = GetToolbar(strobjType, strPegName, strObjName, objProperties, objPegProperties, ref strError, ref isOk,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk) return false;

            #region to make sure the control is ready
            Control cntrlList = ((Control)targetCntrl);
            IntPtr timeoutRslt = IntPtr.Zero;
            IntPtr rsltTimeOut = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(cntrlList.Handle,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                10000,
                out timeoutRslt
                );
            cntrlList.Update();
            rsltTimeOut = windowsWrapper.SystemUtil.MarsWindowsAPIs.SendMessageTimeout(cntrlList.Handle,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SMTO_BLOCK,
                10000,
                out timeoutRslt
                );

            if (rsltTimeOut.ToInt64() != 0)
            {
                simpleLog.MarsLoggerSimple.Info("MarsListViewOperation", "send time out returns true, no thread is busy");
            }

            ((Control)targetCntrl).Update();
            System.Threading.Thread.Sleep(100);
            #endregion

            System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();

            #region 处理Infragistics信息
            if (string.Compare(targetCntrl.GetType().ToString(), "Infragistics.Win.UltraWinToolbars.UltraToolbarsDockArea", true) == 0)
            {
                #region 找到相关的toolbutton
                MarsToolBarOperation objToolbarObj = new MarsToolBarOperation();
                List<object> lstCntrlBelongToToolBar = new List<object>();

                if (string.Compare("text:", strParaMeter, true) == 0)
                {
                    return objToolbarObj.FindAndOpSubControlByNameOrKey(targetCntrl, strParaMeter, strData, strPegName, strObjName, ref strError, ref strAdv, ref strStack);

                }
                isOk = objToolbarObj.FindSubControlByNameOrKey(targetCntrl, lstCntrlBelongToToolBar,
                    new Dictionary<string, string>()
                    {
                        { "SharedProps;ToolTipText", "Application..."},
                        { "key", "Application..."},
                    },
                    new List<string>() {
                        "Static Desktop Toolbar"
                    },
                    ref strError, ref strAdv, ref strStack);
                if (!isOk) return false;
                if (lstCntrlBelongToToolBar.Count != 1)
                {
                    strError = "Multiple objects with the same identifier were found.";// "more than one objects exits";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Make sure [{strObjName}] exists and is visibale; Make sure window identification is correct. Use Object Spy to identify the problem";
                    return false;
                }
                string strErrorTmp = "",
                    strAdvTmp = "",
                    strStackTmp = "";
                isOk = true;
                Point pt = new Point();
                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        // Find UIElement
                        var tmpUIElement = ReflectorForCSharp.GetMemberWithTimeDelay(lstCntrlBelongToToolBar[0], "UIElement", 120);
                        if (tmpUIElement == null)
                        {
                            strError = "Object property [UIElement] is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            isOk = false;
                            return false;
                        }
                        var tmpCntrlFromUI = ReflectorForCSharp.GetMember(tmpUIElement, "Control");
                        if (tmpCntrlFromUI == null)
                        {
                            strError = "Object property [Control] is NULL";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            isOk = false;
                            return false;
                        }
                        var tmpBounds = ReflectorForCSharp.GetMember(lstCntrlBelongToToolBar[0], "Bounds");
                        if (!(tmpBounds is System.Drawing.Rectangle))
                        {
                            strError = "Object property [Bounds]'s type is not Rectangle";
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            isOk = false;
                            return false;
                        }
                        System.Drawing.Rectangle rct = (System.Drawing.Rectangle)tmpBounds;
                        System.Windows.Forms.Control c = tmpCntrlFromUI as System.Windows.Forms.Control;
                        //rct = c.Bounds;          
                        MarsLoggerSimple.Info("\t", string.Format("parent:[{0}]", c.Parent == null ? c.GetType().ToString() : c.Parent.GetType().ToString()));

                        pt = new Point(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);// c.PointToScreen(new Point(rct.X + rct.Width / 2, rct.Y + rct.Height / 2));
                        MarsLoggerSimple.Info("\t", string.Format("O(x-y):[{0}-{1}] ==> D(x-y):[{2}-{3}]", rct.X, rct.Y, pt.X, pt.Y));
                        isOk = true;
                    }
                    catch (Exception e)
                    {
                        MarsLoggerSimple.Error("\t", strErrorTmp = string.Format("Exception:[{0}] stackTrace:\r\n{1}", e.Message, e.StackTrace));
                        StackFrame stck = (new StackFrame());
                        strStackTmp = MarsErrorStacks.StackTraceDump();
                        strAdvTmp = "";
                        isOk = false;
                    }
                }
                //));                
                #endregion 找到相关的toolbutton
                if (!isOk)
                {
                    strError = strErrorTmp;
                    strAdv = strAdvTmp;
                    strStack = strStackTmp;
                    return false;
                }
                //点击按钮
                Thread.Sleep(100);
                if (lstCntrlBelongToToolBar[0] is System.Windows.Forms.Control)
                {
                    MarsLoggerSimple.Info("\t", "toolbar is control");
                    if (((System.Windows.Forms.Control)lstCntrlBelongToToolBar[0]).InvokeRequired)
                    {
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                        {
#else
                        ((System.Windows.Forms.Control)lstCntrlBelongToToolBar[0]).Invoke(new Action(() =>
                        {
#endif
                            MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                        }));
                    }
                    else
                    {
                        MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                    }
                }
                else
                {
                    MarsLoggerSimple.Info("\t", string.Format("toolbar is not control, is [{0}]", lstCntrlBelongToToolBar[0].GetType().ToString()));
                    MarsWindowsAPIsExtend.LeftMouseClick(pt.X, pt.Y);
                }


                IntPtr lpRslt;
                MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    5000,
                    out lpRslt);
                IntPtr pNewHandle = IntPtr.Zero;

                #region 等待新窗口出现 作废
                /*
                /// IT IS NOT NECCESSARY TO CHECK. AS get window should be later
                ///等待新窗口出现
                /// 
                Thread t = new Thread(new ThreadStart(new Action(() => {
                    //allForms = System.Windows.Forms.Application.OpenForms;
                    long dStart = DateTime.Now.Ticks;
                    long eTime = DateTime.Now.Ticks;
                    while (((eTime - dStart) / TimeSpan.TicksPerSecond) < 90) //最长90秒
                    {
                        List<IntPtr> lstCurrentHandles = GetCurrentFormsHandle();
                        var w = lstCurrentHandles.Where(p => !lstOldHandlers.Exists(p1=>p1==p)).ToList();
                        if ((w==null)||(w.Count<=0))
                        {
                            eTime = DateTime.Now.Ticks;
                            Thread.Sleep(100);
                            continue;
                        }
                        else
                        {
                            pNewHandle = w[0];
                            break ;
                        }
                    }
                    ///等待window visible                    
                    /// 
                    dStart = DateTime.Now.Ticks;
                    eTime = DateTime.Now.Ticks;
                    while (((eTime - dStart) / TimeSpan.TicksPerSecond) < 90) //最长90秒
                    {
                        if (!MarsWindowsAPIs.IsWindowVisible(pNewHandle))
                        {
                            eTime = DateTime.Now.Ticks;
                            Thread.Sleep(100);
                            continue;
                        }
                        else
                        {
                            break;
                        }
                        
                    }
                })));
                t.Start();
                t.Join();
                */
                #endregion //等待新窗口出现

                if (pNewHandle != IntPtr.Zero)
                {
                    //将窗口放到最前
                    MarsWindowsAPIs.SetForegroundWindow(pNewHandle);
                    Thread.Sleep(100);
                    MarsWindowsAPIs.SendMessageTimeout(pNewHandle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    5000,
                    out lpRslt);
                }
                else
                {
                    //perhaps a window is created already
                    //strError = "No new window created";
                    //isOk = false;
                    //return false;
                }
                //ReGetForm()

                #region find dialog and filledit
                /// then find application launch dialog and filledit 
                /// 
                Dictionary<string, string> dicPegLaunchDialogIds = new Dictionary<string, string>()
                {
                    { "text","Applications and Pages" }
                };
                Dictionary<string, string> dicEditIds = new Dictionary<string, string>()
                {
                    {"swfname","_fileNameTextBox" }
                };
                //保留现有的peg信息
                List<MarsformIndentifier> oldMainFrm = new List<MarsformIndentifier>();
                if ((CurrentPegWindows != null) && (CurrentPegWindows.Count > 0))
                    oldMainFrm.AddRange(CurrentPegWindows);

                // launch application requires refresh the objects and pegs
                // g_PreviousPegwindow = new KeyValuePair<string, MarsformIndentifier>();
                //g_currentAllObjectListForCurrentPeg = null;

                isOk = AppsideKeywordDeal_FillEdit("DirectInput", strData, "SwfEdit", strAttachInfo,
                    strPegName, strObjName,
                    dicEditIds, dicPegLaunchDialogIds,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    true,
                    2000);
                if (!isOk)
                {
                    //strError = string.Format("Error when try to fill [{0}] \r\n[{1}]", strData, strError);
                    //StackFrame stck = (new StackFrame());
                    //strStack = MarsErrorStacks.StackTraceDump();
                    //strAdv = "dd";
                }
                else
                {
                    Dictionary<string, string> dicButton = new Dictionary<string, string>()
                    {
                        { "swfname", "_actionButton"}
                    };
                    isOk = AppsideKeywordDeal_ClickButton("", "", "SwfButton", strAttachInfo,
                        strPegName, strObjName,
                        dicButton, dicPegLaunchDialogIds,
                        errorCheckObj,
                        ref strError,
                        ref strDataReturn,
                        ref strStack,
                        ref strAdv,
                        ref strSnapshotForShouldBeFile,
                        true);
                    MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    5000,
                    out lpRslt);
                }

                //恢复pegwindow信息 c
                if (oldMainFrm.Count > 0)
                {
                    CurrentPegWindows.Clear();
                    CurrentPegWindows.AddRange(oldMainFrm);
                }
                return isOk;
                #endregion //find dialog 须
            }

            #endregion 处理Infragistics信息

            strError = $"Keyword LaunchApplication does not support object type for [{strObjName}]";
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "Mars supports Infragistics, WinForm and WPF controls";
            return false;

        }

        private static bool AppsideKeywordDeal_HIGHLIGHT(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            simpleLog.MarsLoggerSimple.logBegin("AppsideKeywordDeal_HIGHLIGHT");
            try
            {

                bool isOk = false;
                List<object> lstControls = new List<object>();
                isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile);
                if (!isOk)
                {
                    return false;
                }

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                ///remove all invisible items ;
                /// 
                lstControls.RemoveAll(p =>
                    ((p as System.Windows.Forms.Control) != null)
                    && (!((System.Windows.Forms.Control)p).Visible)
                    );
                if (lstControls.Count != 1)
                {
                    strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                    return false;
                }

                isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                if (!isOk) return false;

                if (lstControls[0] is System.Windows.Forms.Control)
                {
                    System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
                    Rectangle rect = new Rectangle();
                    isOk = SnapshotBase.Highlight(c, strParaMeter, ref strError, ref strAdv, ref strStack, ref rect);
                    /*
                    if (isOk)
                    {
                        strDataReturn = strPath;
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("File saved to [{0}]", strDataReturn));
                    }
                    */
                    return isOk;
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = $"Object [{strPegName}].[{strObjName}] is not a Contro");// string.Format("target object is not a System.Windows.Forms.Control, it is [{0}]", lstControls[0].GetType().ToString()));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Contact Marquis";
                    return false;
                }
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"Error while Highlighting object [{strPegName}].[{strObjName}]"); //string.Format("Excepton:[{0}]\r\n{1}", e.Message, e.StackTrace));
                StackFrame stck = (new StackFrame());
                strStack = e.StackTrace;
                strAdv = "Unidentified error. If this continues, contact Marquis";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("AppsideKeywordDeal_HIGHLIGHT");
            }

        }


        private static bool AppsideKeywordDeal_SnapShot(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_SnapShot", $"{iMark}|SelectDropDown({strPegName}.{strObjName}, {strParaMeter}, {strData})|{strobjType}|{MarsWindowsAPIsExtend.Dic2String(objProperties)}");
            try
            {
                if (IsStandardControlType(strobjType))
                {
                    return MarsStandardMFCControlKeywordOp.SnapShot(strParaMeter, strData, strobjType,
                    strAttachInfo, strPegName,
                    strObjName,
                    objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
                }

                bool isOk = false;
                List<object> lstControls = new List<object>();

                isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile);
                if (!isOk)
                {
                    return false;
                }

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                ///remove all invisible items ;
                /// 
                lstControls.RemoveAll(p =>
                    ((p as System.Windows.Forms.Control) != null)
                    && (!((System.Windows.Forms.Control)p).Visible)
                    );
                if (lstControls.Count != 1)
                {
                    strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                    return false;
                }

                isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                if (!isOk) return false;

                if (lstControls[0] is System.Windows.Forms.Control)
                {
                    System.Windows.Forms.Control c = lstControls[0] as System.Windows.Forms.Control;
                    string strPath = (new Snapshot()).SnapshotScreen(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    if (isOk)
                    {
                        strDataReturn = strPath;
                        simpleLog.MarsLoggerSimple.Info("\t", string.Format("File saved to [{0}]", strDataReturn));
                    }
                    return isOk;
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Error("\t", strError = $"Object [{strPegName}].[{strObjName}] is not a Control");// string.Format("target object is not a System.Windows.Forms.Control, it is [{0}]", lstControls[0].GetType().ToString()));
                    strStack = $"Object type is [{lstControls[0].GetType().ToString()}]\r\n{MarsErrorStacks.StackTraceDump()}";
                    strAdv = "Contact Marquis";
                    return false;
                }
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("\t", strError = $"Error while Snapshotting for [{strPegName}].[{strObjName}]");// string.Format("Excepton:[{0}]\r\n{1}", e.Message, e.StackTrace));
                StackFrame stck = (new StackFrame());
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("AppsideKeywordDeal_SnapShot");
            }

        }

        private static bool AppsideKeywordDeal_SetBox(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            simpleLog.MarsLoggerSimple.logBegin("SETBOX", string.Format("strParameter:[{0}] data:[{1}] objtype:[{2}]", strParaMeter, strData, strobjType));
            bool isOk = false;
            if (string.IsNullOrEmpty(strData))
                return true;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }
            ///remove all invisible items ;
            /// 
            simpleLog.MarsLoggerSimple.Info("SETBOX", $"before remove non-visible|{lstControls.Count}|");
            lstControls.ForEach((itm) => {
                if (itm != null)
                {
                    simpleLog.MarsLoggerSimple.Info("SETBOX", $"{itm.GetType()}|{((Control)itm).Bounds}");
                }
            });
            lstControls.RemoveAll(p =>
                ((p as System.Windows.Forms.Control) != null)
                && (!((System.Windows.Forms.Control)p).Visible)
                );
            simpleLog.MarsLoggerSimple.Info("SETBOX", $"after removed non-visible|{lstControls.Count}|");

            if (lstControls.Count != 1)
            {
                strError = $"Multiple objects with the same identifier were found.|{lstControls.Count}|";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            System.Windows.Forms.CheckBox oCheckBox = lstControls[0] as System.Windows.Forms.CheckBox;
            if ((oCheckBox) != null)
            {
                simpleLog.MarsLoggerSimple.Info("\t", string.Format("standard checkbox,type :{0}", oCheckBox.GetType()));
                bool isChecked = oCheckBox.Checked;
                Point pt = oCheckBox.Location;
                Point ptScreen = oCheckBox.Parent.PointToScreen(new Point(pt.X + oCheckBox.Width / 2, pt.Y + oCheckBox.Height / 2));
                MarsLoggerSimple.Info("\t", string.Format("client point:[{0}] ScreenPoint:[{1}]", pt, ptScreen));

                if ((string.Compare("on", strData, true) == 0) || (string.Compare("true", strData, true) == 0))
                {
                    //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptScreen.X, ptScreen.Y );
                    if (isChecked) return true;
                }
                oCheckBox.Focus();
                //bool requiredValue = false;

                if ((string.Compare("off", strData, true) == 0) || (string.Compare("false", strData, true) == 0))
                {
                    //windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptScreen.X, ptScreen.Y);
                    //requiredValue = true;
                    if (!isChecked) return true;
                }
                IntPtr lpdwResult;
                if (oCheckBox.InvokeRequired)
                {

#if _NET4
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                    oCheckBox.Invoke(
#endif
                        new Action(() =>
                        {
                            windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptScreen.X, ptScreen.Y);
                            //System.Windows.Forms.SendKeys.SendWait(" {TAB}");
                        })
                        );

                    //isChecked = oCheckBox.CheckState == System.Windows.Forms.CheckState.Checked;
                    //if (!isChecked)
                }
                else
                {
                    windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(ptScreen.X, ptScreen.Y);
                }
                MarsWindowsAPIs.SendMessageTimeout(oCheckBox.Handle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            MarsWindowsAPIs.SMTO_BLOCK,
                            5000,
                            out lpdwResult
                        );
                System.Windows.Forms.SendKeys.SendWait("{TAB}");
                MarsLoggerSimple.Info("\t", string.Format("current Checked value:[{0}]", oCheckBox.Checked));
                return true;
            }
            else
            {
                string strTypes = ReflectorForCSharp.GetObjectBaseType(lstControls[0].GetType());
                strTypes = string.IsNullOrEmpty(strTypes) ? "" : strTypes;
                if (strTypes.IndexOf("UltraWinEditors.UltraCheckEditor") >= 0)
                {
                    // Infragistics.Win.UltraWinEditors.UltraCheckEditor;Infragistics.Win.UltraWinEditors.UltraToggleEditorBase;Infragistics.Win.UltraControlBase;
                    return (new MarsCheckBoxOperation()).SetBox((System.Windows.Forms.Control)lstControls[0],strParaMeter, strData, ref strError);
                }
                MarsLoggerSimple.Error("\t", strError = $"Setbox does not support object type for [{strObjName}]|{strTypes}"); // string.Format("unsupported object type of set box:[{0}]", ReflectorForCSharp.GetObjectBaseType(lstControls[0].GetType())));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Mars supports Infragistics, WinForm and WPF controls";
                return false;
            }

        }

        private static bool isVisibleCheckIgnored(string strPara, ref string strError, ref string strAdv, ref string strStack, ref bool isRightFormat)
        {
            isRightFormat = true;
            if (string.IsNullOrEmpty(strPara))
            {   
                strError = "Passing null object to a function.";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                return false;
            }
            string strParaUpper = strPara.ToUpper();
            int iVisblePos = -1;

            if ((iVisblePos = strParaUpper.IndexOf(cnst_para_visible)) < 0) return false;
            int iCommaPos = strParaUpper.IndexOf(":", iVisblePos);
            if (iCommaPos < 0)
            {
                isRightFormat = false;
                strError = "Incorrect format for a control Visible checking";// string.Format("Ignore Visible format should be :\"{0}:{1};\" no bool value. current is:[{2}]-{3}", strPara, "true|false", strPara, strIgnoreId);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "See user manual for correct object visible checking use";
                return true;
            }
            int iSmComma = strParaUpper.IndexOf(";", iCommaPos);
            if ((iSmComma < 0) || (iCommaPos > iSmComma))
            {
                strError = "Incorrect format for a control Visible checking";// string.Format("Ignore Visible format should be :\"{0}:{1};\" no bool value. current is:[{2}]-{3}", strPara, "true|false", strPara, strIgnoreId);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "See user manual for correct object visible checking use";
                isRightFormat = false;
                return true;
            }
            string strIgnoreId = strParaUpper.Substring(iCommaPos + 1, iSmComma - iCommaPos - 1);
            if (string.IsNullOrEmpty(strIgnoreId)) return true;
            bool isIgnore;
            if (bool.TryParse(strIgnoreId, out isIgnore))
            {
                return isIgnore;
            }
            strError = "Incorrect format for a control Visible checking";// string.Format("Ignore Visible format should be :\"{0}:{1};\" no bool value. current is:[{2}]-{3}", strPara, "true|false", strPara, strIgnoreId);
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = "See user manual for correct object visible checking use";
            isRightFormat = false;
            return true;
        }

        private static bool AppsideKeywordDeal_SetSplitter(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            simpleLog.MarsLoggerSimple.logBegin("SETSPLITTER", string.Format("strParameter:[{0}] data:[{1}] objtype:[{2}]", strParaMeter, strData, strobjType));
            bool isOk = false;
            if (string.IsNullOrEmpty(strData))
                return true;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }
            ///remove all invisible items ;
            /// 
            lstControls.RemoveAll(p =>
                ((p as System.Windows.Forms.Control) != null)
                && (!((System.Windows.Forms.Control)p).Visible)
                );
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            System.Windows.Forms.Splitter oSplitter = lstControls[0] as System.Windows.Forms.Splitter;
            if (oSplitter == null)
            {
                strError = $"Object [{strObjName}] is Not Splitter";
                strAdv = $"Make sure [{strPegName}].[{strObjName}]'s type is Splitter";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            int iPos;
            if (!int.TryParse(strData, out iPos))
            {
                strError = $"Object [{strObjName}]'s Data [{strData}] is Not int";
                strAdv = $"Make sure [{strData}] is an int";
                strStack = MarsErrorStacks.StackTraceDump();
                return false;
            }
            try
            {
                oSplitter.Refresh();
                Thread.Sleep(50);
                IntPtr lpdwResult;
                MarsWindowsAPIs.SendMessageTimeout(oSplitter.Handle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_BLOCK,
                                5000,
                                out lpdwResult
                            );
                oSplitter.SplitPosition = iPos;
                return true;
            }
            catch (Exception e)
            {
                strError = $"Error while setting position for [{strObjName}]";
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
                simpleLog.MarsLoggerSimple.Error("SETSPLITTER", strStack);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("SETSPLITTER");
            }

        }

        /// <summary>
        /// 处理label或者title，对象中必须要有text属性
        /// </summary>
        /// <param name="strParaMeterEx">格式 [属性名称]:正则表达式 比如，Text:^(?!.*Please Wait).*
        /// </param>
        /// <param name="strDataSrc"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool _WaitUntil_Text(string strParaMeterEx, string strDataSrc, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime = -1)
        {
            simpleLog.MarsLoggerSimple.logBegin("_WaitUntil_Text", $"objName|{strObjName}|{strPegName}|{strParaMeterEx}|{strDataSrc}");
            MarsWaitUntilForProperty paraInfo = new MarsWaitUntilForProperty();
            string strDataOrPara = string.IsNullOrEmpty(strParaMeterEx)?strDataSrc:strParaMeterEx;
            bool isOk = paraInfo.setSourcePara(strDataOrPara);
            if (!isOk)
            {
                strStack = Environment.StackTrace;
                simpleLog.MarsLoggerSimple.Error("_WaitUntil_Text", (strError = $"{strDataOrPara}| does not match the format.")+$"\r\n|{strStack}");
                strAdv = "Please correct parameter to match [Property]:[Text to search].\r\n[Property] is Text normally.";
                return false;
            }
            /// 1， 获得对象 
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                simpleLog.MarsLoggerSimple.Error("_WaitUntil_Text", $"not find object with error|{strError}");
                return false;
            }
            ///remove all invisible items ;
            /// 
            ///判断否需要处理visible的消息
            ///
            bool isIgnoreVisible = isVisibleCheckIgnored(strDataOrPara, ref strError, ref strAdv, ref strStack, ref isOk);
            if (!isOk)
            {
                //wrong format
                MarsLoggerSimple.Error("\t", strError);
                return false;
            }
            if (!isIgnoreVisible)
            {
                lstControls.RemoveAll(p =>
                    ((p as System.Windows.Forms.Control) != null)
                    && (!((System.Windows.Forms.Control)p).Visible)
                    );
            }
            else
            {
                lstControls.RemoveAll(p => (p as System.Windows.Forms.Control) == null);
            }
            /// more than one objets
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]-count:[{2}]", string.Concat(objProperties.Keys), string.Join(";", objProperties.Values.ToArray()), lstControls.Count);
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                strStack = MarsErrorStacks.StackTraceDump();
                simpleLog.MarsLoggerSimple.Error("_WaitUntil_Text", $"{strError}|\r\n{strAdv}");
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack, isIgnoreVisible);
            if (!isOk)
            {
                simpleLog.MarsLoggerSimple.Error("_WaitUntil_Text", $"{strError}|\r\n{strAdv}");
                return false;
            }            
            /// 使用反射处理
            /// 
            try
            {
                System.Windows.Forms.Control c = (System.Windows.Forms.Control)lstControls[0];
                string strTpe = ReflectorForCSharp.GetObjectBaseType(c.GetType());
                simpleLog.MarsLoggerSimple.Info("_WaitUntil_Text", string.Format("object type:[{0}]", strTpe));

                long n = DateTime.Now.Ticks, px = n;
                string strTxt = "";
                ReflectorForCSharp rf = new ReflectorForCSharp();
                bool isNotPropertyExists = false;
                
                while ((((n - px) / TimeSpan.TicksPerSecond) < (waitingTime <= 0 ? 1 : paraInfo.waitForSeconds)))
                {
                    var t = ReflectorForCSharp.GetMember(c, paraInfo.propertyToCheck, ref isNotPropertyExists, true);
                    
                    if (isNotPropertyExists)
                    { 
                        simpleLog.MarsLoggerSimple.Error("_WaitUntil_Text",strError= $"no scuh property |{paraInfo.propertyToCheck}| exists in |{c.GetType()}|");
                        strStack = Environment.StackTrace;
                        strAdv = "Please set the right propeerty, Text could be a better choice";                        
                        break;
                    }
                    if (t != null)
                    {
                        strTxt = t.ToString();
                        try
                        {
                            if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(paraInfo.patternToMatch, strTxt))
                            {
                                isOk = true;
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            /// 可能是非法的正则表达式
                            /// 
                            simpleLog.MarsLoggerSimple.Error("_WaitUntil_Text", ex.Message, ex);
                            strError = "check the regular expression";
                            strAdv = "correct the problems of regular exression, and check log file if the problem still exists";
                            strStack = ex.StackTrace;
                            break;
                        }
                    }

                    Thread.Sleep(50);
                    n = DateTime.Now.Ticks;
                }
                strError = $"after |{paraInfo.waitForSeconds}| seconds, can't match |{paraInfo.propertyToCheck}|to|{strTxt}|";
                strAdv = "Please correct match string and try again";
                strStack = Environment.StackTrace;
                isOk = false;
                return false;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("_WaitUntil_Text", ex.Message, ex);
                strError = "Can't Test the control and its property.";
                strAdv = "Please check the log file for more details";
                strStack = ex.StackTrace;
                isOk = false;
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("_WaitUntil_Text", $"isOk|{isOk}|{strError}|");
            }
        }


        private static bool _WaitUntil_Table(string strParaMeterEx, string strDataSrc, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime = -1)
        {
            /// 步骤
            /// 1， 获得对象
            /// 2， 获得columns
            /// 3， 解析参数
            /// 
            simpleLog.MarsLoggerSimple.logBegin("_WaitUntil_Table", $"wait for [{waitingTime}] seconds|para|{strParaMeterEx}|");
            bool isOk = false;
            //bool isAdvancedSelectType = false;
            string strParameter = strParaMeterEx;
            //, strPrefix = "", strNormal = "";
                       
            try
            {
                if (!string.IsNullOrEmpty(strParameter))
                {
                    int iWaitTime = waitingTime;
                    if (int.TryParse(strParameter.Trim(),out iWaitTime))
                    {
                        if (iWaitTime > waitingTime)
                            waitingTime = iWaitTime;
                        simpleLog.MarsLoggerSimple.logBegin("_WaitUntil_Table", $"updated wait time|{waitingTime}|");
                    }
                }

                if (string.IsNullOrEmpty(strDataSrc))
                {
                    strError = $"data is empty ";
                    strAdv = "Make sure that data setting is right";
                    strStack = Environment.StackTrace;
                    simpleLog.MarsLoggerSimple.Error("_WaitUntil_Table", $"{strError}-format is {MarsWaitUntil.cnst_frmt_waitUntil}");
                    return false;
                }
                    
                MarsWaitUntil waitUntilPara = MarsWaitUntil.getInstance(strDataSrc);
                if (waitUntilPara == null)
                {
                    strError = $"parameter [{strDataSrc}] desn't match format ";
                    strAdv = "Make sure that parameter format is right";
                    strStack = Environment.StackTrace;
                    simpleLog.MarsLoggerSimple.Error("_WaitUntil_Table", $"{strError}-format is {MarsWaitUntil.cnst_frmt_waitUntil}");
                    return false;
                }
                if (!waitUntilPara.isToOperateTable())
                {
                    strError = $"parameter [{strDataSrc}] is set for table operation ";
                    strAdv = "Make sure that parameter format is right, starts with ColName or RowCount";
                    strStack = Environment.StackTrace;
                    simpleLog.MarsLoggerSimple.Error("_WaitUntil_Table", $"{strError}-format is {MarsWaitUntil.cnst_frmt_waitUntil}");
                    return false;
                }

                /// 1， 获得对象 
                List<object> lstControls = new List<object>();
                isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile);
                if (!isOk)
                {
                    return false;
                }

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                ///remove all invisible items ;
                /// 
                ///判断否需要处理visible的消息
                ///
                bool isIgnoreVisible = isVisibleCheckIgnored(strParameter, ref strError, ref strAdv, ref strStack, ref isOk);
                if (!isOk)
                {
                    //wrong format
                    MarsLoggerSimple.Error("\t", strError);
                    return false;
                }
                if (!isIgnoreVisible)
                {
                    lstControls.RemoveAll(p =>
                        ((p as System.Windows.Forms.Control) != null)
                        && (!((System.Windows.Forms.Control)p).Visible)
                        );
                }
                else
                {
                    lstControls.RemoveAll(p => (p as System.Windows.Forms.Control) == null);
                }
                /// more than one objets
                if (lstControls.Count != 1)
                {
                    strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]-count:[{2}]", string.Concat(objProperties.Keys), string.Join(";", objProperties.Values.ToArray()), lstControls.Count);
                    strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                    strStack = MarsErrorStacks.StackTraceDump();
                    bool isTmp = true;
                    object oPre = null;
                    //for testing print information
                    foreach (var itm in lstControls)
                    {
                        if (itm == null) continue;
                        Control tmpc = itm as Control;
                        if (oPre == null)
                        {
                            oPre = tmpc;
                        }
                        else
                        {
                            isTmp &= tmpc.Equals(oPre);
                        }
                        if (tmpc == null) continue;
                        string namepath = MarsformIndentifier.MarsGetParentsNames(tmpc.Parent);
                        string strType = ReflectorForCSharp.GetObjectBaseType(itm.GetType());
                        string strPrntTyp = MarsformIndentifier.GetParentTypePath(tmpc);

                        //unsafe
                        //{
                        //    object* pC = (object*)(&itm);
                        MarsLoggerSimple.Info("\t", string.Format("name:[{0}] equal:[{3}] type:[{1}]\r\n\t[{2}]\r\n\tbounds:[{4}]  handle:[{5}] text:[{6}]",
                            tmpc.Name,
                            strPrntTyp,
                            namepath,
                            isTmp,
                            tmpc.Bounds,
                            tmpc.Handle,
                            tmpc.Text
                            ));
                        //Highlight(tmpc);
                        //}
                    }
                    return false;
                }

                isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack, isIgnoreVisible);
                if (!isOk) return false;

                System.Windows.Forms.Control c = (System.Windows.Forms.Control)lstControls[0];
                string strTpe = ReflectorForCSharp.GetObjectBaseType(c.GetType());
                simpleLog.MarsLoggerSimple.Info("_WaitUntil_Tab", string.Format("object type:[{0}]", strTpe));

                /// 获得column
                /// 
                isOk = waitUntilPara.initTableInfo(ref strError);
                if ((!isOk)||(waitUntilPara.tableDataInfo==null))
                {
                    strAdv = "Make sure that parameter format is right, starts with ColName or RowCount";
                    strStack = Environment.StackTrace;
                    simpleLog.MarsLoggerSimple.Error("_WaitUntil_Table", $"{strError}-format is {MarsWaitUntil.cnst_frmt_waitUntil}");
                    return false;
                }
                MarsTableOperation tblOp = new MarsTableOperation();
                string strKey = "";
                int colidx = -1;
                if (c.InvokeRequired)
                {
                    bool isOkTmp = false;
                    string strTmpError = "", strAdvTmp="", strStackTmp="";
                    c.Invoke(new Action(() =>{
                        isOkTmp = tblOp.GetColumnKeyForInfragisticsGrid(c, waitUntilPara.tableDataInfo.colInfo, strPegName, strObjName, ref strKey, ref colidx, ref strTmpError, ref strAdvTmp, ref strStackTmp);
                        
                    }));
                    isOk = isOkTmp;
                    strError = strTmpError;
                    strAdv = strAdvTmp;
                    strStack = strStackTmp;
                    if (!isOk) return isOk;
                }
                else {
                    if (!tblOp.GetColumnKeyForInfragisticsGrid(c, waitUntilPara.tableDataInfo.colInfo, strPegName, strObjName, ref strKey, ref colidx, ref strError, ref strAdv, ref strStack))
                    {
                        return isOk = false;
                    }
                }

                long n = DateTime.Now.Ticks, px = n;
                bool isBreak = false;

                while ((((n - px) / TimeSpan.TicksPerSecond) < (waitingTime <= 0 ? 1 : waitingTime)))
                {
                    if (isBreak) break;
                    try
                    {
                        object oRows = ReflectorForCSharp.WaitUntilMemberExistSafe<object>(c, "Rows", ref isOk, ref strError);
                        if (!isOk)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", "Can't get Row "+strError);
                            strAdv = "Contact Marquis";
                            //return false;
                            break;
                        }
                        int iRowCount = ReflectorForCSharp.WaitUntilMemberExist<int>(oRows, "Count", ref isOk, ref strError);
                        if (!isOk)
                        {
                            simpleLog.MarsLoggerSimple.Error("\t", "Can't get Count " + strError);
                            strAdv = "Contact Marquis";
                            //return false;
                            break;
                        }

                        object[] arrAll = ReflectorForCSharp.GetMemberByType<object[]>(oRows, "All");
                        if ((arrAll == null))
                        {
                            simpleLog.MarsLoggerSimple.Error("_WaitUntil_Tab", strError = "Object property [All] value is NULL");// "Can't get member All from object by reflector, or All return null.");
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Contact Marquis";
                            isOk = false;
                            //return isOk;
                            break;
                        }
                        int iRowId = waitUntilPara.tableDataInfo.getRowId(ref isOk, ref strError);
                        if (!isOk)
                        {
                            strAdv = "Make sure Column number information is a number.";
                            simpleLog.MarsLoggerSimple.Error("\t", "Can't get getRowId " + strError);
                            strStack = MarsErrorStacks.StackTraceDump();
                            isOk = false;
                            //return isOk;
                            break;
                        }
                        bool isNotExists = false;
                        ReflectorForCSharp of = new ReflectorForCSharp();
                        simpleLog.MarsLoggerSimple.Error("\t", "begin to for Loop " + arrAll.Length);
                        for (int i = 0; i < arrAll.Length; i++)
                        {
                            if (iRowId == -1)
                            {
                                // any row
                            }
                            else
                            {
                                try
                                {
                                    var r = arrAll[iRowId];
                                    var oCell = tblOp.GetCellFromOneRow(r, colidx, ref isOk, ref strError, ref strAdv, ref strStack);
                                    if (!(isOk && (oCell != null)))
                                    {
                                        simpleLog.MarsLoggerSimple.Error("_WaitUntil_Table", strError);
                                        //return false;
                                        break;
                                    }
                                    string strTmpCellText = of.GetMember<string>(oCell, "Text", ref isNotExists);
                                    if (isNotExists)
                                    {
                                        simpleLog.MarsLoggerSimple.Error("_WaitUntil_Table", strError = "Object property [Text]'s value is NULL in Cell.");//"No Text property exists in Cell");                        
                                        strStack = MarsErrorStacks.StackTraceDump();
                                        strAdv = "Contact Marquis";
                                        isOk = false;
                                        //return false;
                                        break;
                                    }
                                    simpleLog.MarsLoggerSimple.Info("_WaitUntil_Table", $"get text from the cell:[{strTmpCellText}]");
                                    strTmpCellText = MarsWaitUntil.NormalizationDataFromTable(strTmpCellText);
                                    isOk = waitUntilPara.IsMatch(strTmpCellText);
                                    if (!isOk) break;
                                    return true;
                                }catch(Exception ex)
                                {
                                    simpleLog.MarsLoggerSimple.Error("_WaitUntil_Table", ex.Message, ex);
                                    break ;
                                }
                                finally
                                {

                                }
                            }
                        }
                        if (waitUntilPara.tableDataInfo.isAnyRow())
                        {
                            tblOp.GetTargetRowFromRows(oRows, iRowId, ref isOk, ref strError, ref strAdv, ref strStack);
                        }
                    }catch(Exception e)
                    {
                        simpleLog.MarsLoggerSimple.Error("\t", e.Message, e);
                    }
                    finally
                    {
                        System.Threading.Thread.Sleep(1000);
                        n = DateTime.Now.Ticks;
                        simpleLog.MarsLoggerSimple.Info("_WaitUntil_Table", $"check new loop [{(n-px)/TimeSpan.TicksPerSecond}]");
                    }
                }
                strError = $"Can't match the condition.And waited for {waitingTime} seconds ";
                strAdv = "Contact Marquis";
                strStack = Environment.StackTrace;
                isOk = false;
                return false;
            }catch(Exception ee)
            {
                simpleLog.MarsLoggerSimple.Error("_WaitUntil_Table", strError = ee.Message, strStack = ee.StackTrace);
                strAdv = "Contact Marquis";
                isOk = false;
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("_WaitUntil_Table");
            }
            

            

        }

        private static bool _WaitUntil_Tab(string strParaMeterEx, string strDataSrc, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime = -1)
        {
            simpleLog.MarsLoggerSimple.logBegin("_WaitUntil_Tab", strDataSrc);
            try
            {
                var waitData = MarsWaitUntil.getInstance(strDataSrc);
                if (waitData == null)
                {
                    strError = $"Format of data is wrong.";
                    strAdv = "Make sure the data setting matches ";
                    strStack = Environment.StackTrace;
                    return false;
                }

                bool isOk = false;
                bool isAdvancedSelectType = false;
                string strParameter = strParaMeterEx, strPrefix = "", strNormal = "";
                bool isRibbonMode = false;
                if (!string.IsNullOrEmpty(strParaMeterEx))
                {
                    if (!(isRibbonMode = SelectTab.isRibbonMode(strParameter, ref strParameter)))
                    {
                        isAdvancedSelectType = SelectTab.isAdvancedSelectTabPara(strParaMeterEx, ref strPrefix, ref strNormal);
                        if (isAdvancedSelectType)
                        {
                            strParameter = strNormal;
                        }
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info("_WaitUntil_Tab", "ribbon Mode");
                    }
                }

                if (string.IsNullOrEmpty(strDataSrc))
                    return true;
                List<object> lstControls = new List<object>();
                isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile);
                if (!isOk)
                {
                    return false;
                }

                if ((lstControls == null) || (lstControls.Count <= 0))
                {
                    strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Ensure the object is visible on the screen.";
                    return false;
                }
                ///remove all invisible items ;
                /// 
                ///判断否需要处理visible的消息
                ///
                bool isIgnoreVisible = isVisibleCheckIgnored(strParameter, ref strError, ref strAdv, ref strStack, ref isOk);
                if (!isOk)
                {
                    //wrong format
                    MarsLoggerSimple.Error("\t", strError);
                    return false;
                }
                if (!isIgnoreVisible)
                {
                    lstControls.RemoveAll(p =>
                        ((p as System.Windows.Forms.Control) != null)
                        && (!((System.Windows.Forms.Control)p).Visible)
                        );
                }
                else
                {
                    lstControls.RemoveAll(p => (p as System.Windows.Forms.Control) == null);
                }
                if (lstControls.Count != 1)
                {
                    strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]-count:[{2}]", string.Concat(objProperties.Keys), string.Join(";", objProperties.Values.ToArray()), lstControls.Count);
                    strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                    strStack = MarsErrorStacks.StackTraceDump();
                    bool isTmp = true;
                    object oPre = null;
                    //for testing print information
                    foreach (var itm in lstControls)
                    {
                        if (itm == null) continue;
                        Control tmpc = itm as Control;
                        if (oPre == null)
                        {
                            oPre = tmpc;
                        }
                        else
                        {
                            isTmp &= tmpc.Equals(oPre);
                        }
                        if (tmpc == null) continue;
                        string namepath = MarsformIndentifier.MarsGetParentsNames(tmpc.Parent);
                        string strType = ReflectorForCSharp.GetObjectBaseType(itm.GetType());
                        string strPrntTyp = MarsformIndentifier.GetParentTypePath(tmpc);

                        //unsafe
                        //{
                        //    object* pC = (object*)(&itm);
                        MarsLoggerSimple.Info("\t", string.Format("name:[{0}] equal:[{3}] type:[{1}]\r\n\t[{2}]\r\n\tbounds:[{4}]  handle:[{5}] text:[{6}]",
                            tmpc.Name,
                            strPrntTyp,
                            namepath,
                            isTmp,
                            tmpc.Bounds,
                            tmpc.Handle,
                            tmpc.Text
                            ));
                        //Highlight(tmpc);
                        //}
                    }
                    return false;
                }

                isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack, isIgnoreVisible);
                if (!isOk) return false;

                System.Windows.Forms.Control c = (System.Windows.Forms.Control)lstControls[0];
                string strTpe = ReflectorForCSharp.GetObjectBaseType(c.GetType());
                simpleLog.MarsLoggerSimple.Info("_WaitUntil_Tab", string.Format("object type:[{0}]", strTpe));

                if (isRibbonMode)
                {
                    strError = "Can't support ribbon";
                    strAdv = "Contact Marquis";
                    strStack = Environment.StackTrace;
                    return false ;
                    //return (new MarsToolBarOperation()).WaitUntilForRibbon(c, strParameter, waitData.waitType, waitData.op, waitData.valueToCom, strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                    //return (new MarsToolBarOperation()).SelectTabFromRibbon(c, strParameter, strData, strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                }
                bool isInfragisticsDocking = strTpe.Contains("Infragistics.Win.UltraWinDock.WindowDockingArea");
                if (isInfragisticsDocking)
                {
                    strError = "Can't support WindowDockingArea";
                    strAdv = "Contact Marquis";
                    strStack = Environment.StackTrace;
                    return false;
                    //return (new MarsTableOperation()).SelectTab4DockingArea(c, strParameter, strData, ref strError, ref strAdv, ref strStack);
                }

                if (strTpe.Contains("Infragistics.Win.UltraWinTabControl.UltraTabControlBase;"))
                {
                    simpleLog.MarsLoggerSimple.Info("\t", "come to Infragistics tab base mode");
                    MarsTabOperations objTabObj = new MarsTabOperations();
                    
                    isOk = objTabObj.waitUntil(c, waitData.waitType, waitData.valueToCom, waitData.op, strPegName, strObjName, ref strError, ref strAdv, ref strStack, isAdvancedSelectType, strPrefix, 30*60);
                    if (!isOk)
                    {
                        MarsLoggerSimple.Error("\t", string.Format("Error when run SelectTab:[{0}]", strError));
                        return false;
                    }

                    return true;
                }

                if (strTpe.Contains("Infragistics.Win.UltraWinTabbedMdi.MdiTabGroupControl"))
                {
                    simpleLog.MarsLoggerSimple.Info("\t", "come to Infragistics grouped tab");
                    MarsMdiTabGroupOprations mdiTabOp = new MarsMdiTabGroupOprations();
                    isOk = mdiTabOp.waitUntil(c, strParameter, waitData.waitType, waitData.valueToCom, waitData.op,  strPegName, strObjName, ref strError, ref strAdv, ref strStack, isAdvancedSelectType, strPrefix);
                    if (!isOk)
                    {
                        MarsLoggerSimple.Error("\t", string.Format("Error when run SelectTab:[{0}]", strError));
                        return false;
                    }
                    return true;
                }

                if (c is System.Windows.Forms.TabControl)
                {
                    simpleLog.MarsLoggerSimple.Info("_WaitUntil_Tab", "System.Windows.Forms.TabControl mode");
                    System.Windows.Forms.TabControl tab = (System.Windows.Forms.TabControl)c;
                    IntPtr lpdwResult;
                    System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();
                    MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_BLOCK,
                                5000,
                                out lpdwResult
                            );
                    long n = DateTime.Now.Ticks, p = n;
                    while (((n-p)/TimeSpan.TicksPerSecond)< (waitingTime<0?5: waitingTime))
                    {
                        try
                        {
                            for (int itabIdx = 0; itabIdx < tab.TabCount; itabIdx++)
                            {
                                if (MarsWindowsAPIsExtend.RegularTest(waitData.valueToCom, tab.TabPages[itabIdx].Text)
                                    || (string.Compare(waitData.valueToCom, tab.TabPages[itabIdx].Text, true) == 0))
                                {
                                    isOk = true;
                                    return true;
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                        n = DateTime.Now.Ticks;
                        System.Threading.Thread.Sleep(1000);
                    }
                    
                    //if (isOk) return true;
                    isOk = false;
                    strError = $"Can't find Tab [{waitData.valueToCom}]";// string.Format("Can't find header with caption:[{0}]  from [{1}]", strData, strHeaders);
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Ensure [{strPegName}].[{strObjName}] is visible on the screen.";
                    return false;
                }

                MarsLoggerSimple.Info("\t", strError = $"Unable to locate the object [{strObjName}]"); //string.Format("Find a Tab [{1}] but no assigned process to deal with it for object:{0}", objPegProperties.Select(kv => string.Format("[{0}]-[{1}]", kv.Key, kv.Value)), strTpe));
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Make sure [{strPegName}].[{strObjName}] is avaiable on the screen ";
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("_WaitUntil_Tab");
            }
            
        }


        /// <summary>
        /// 等待某个对象的特性出现
        /// 1，tab 某个tabname出现
        /// 2，grid， 行数 >0 
        /// 目前只支持这两类
        /// </summary>
        /// <param name="strParaMeterEx"></param>
        /// <param name="strData">
        /// format: tabCount>=|=|< number, 
        ///         gridRowCount <|>|>=|= number
        /// </param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_WaitUntil(string strParaMeterEx, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime = -1)
        {
            simpleLog.MarsLoggerSimple.logBegin("AppsideKeywordDeal_WaitUntil", $"{strParaMeterEx},data- {strData}");
            bool isOk = false;
            try
            {
                if (string.IsNullOrEmpty(strobjType))
                {
                    strError = "Object type is null";
                    strStack = Environment.StackTrace;
                    strAdv = "Contact Marquis";
                    return false;
                }
            
                switch (strobjType.ToUpper())
                {
                    case "SWFTAB":
                        isOk = _WaitUntil_Tab(strParaMeterEx,strData, strobjType, strAttachInfo, strPegName, strObjName, objProperties, objPegProperties,ref strError, 
                            ref strDataReturn, ref strStack, ref strAdv, ref strSnapshotForShouldBeFile, true, waitingTime);
                        break;
                    case "SWFTABLE":
                        isOk = _WaitUntil_Table(strParaMeterEx, strData, strobjType, strAttachInfo, strPegName, strObjName, objProperties, objPegProperties, ref strError,
                            ref strDataReturn, ref strStack, ref strAdv, ref strSnapshotForShouldBeFile, true, waitingTime);
                        break;
                    case "SWFOBJECT":
                    case "SWFLABEL":
                        isOk = _WaitUntil_Text(strParaMeterEx, strData, strobjType, strAttachInfo, strPegName, strObjName, objProperties, objPegProperties, ref strError,
                            ref strDataReturn, ref strStack, ref strAdv, ref strSnapshotForShouldBeFile, true, waitingTime);
                        break;
                    default:
                        strError = $"unsupport object type.";
                        strStack = Environment.StackTrace;
                        strAdv = "Contact Marquis";
                        return false;
                }
                return isOk;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("AppsideKeywordDeal_WaitUntil",$"returns [{isOk}]");
            }

        }
        /**
        * 2019 11-18 ribbon当做一个tab处理？
        * 通过selectab做一个测试，然后创建一个新的keyword SelectRibbon 和 FillRibbonEdit
        * 如果是一个ribbon，需要确认是否是toolbars from infragistics
        * */
        private static bool AppsideKeywordDeal_SelectTab(string strParaMeterEx, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_SelectTab", $"{iMark}|SelectDropDown({strPegName}.{strObjName}, {strParaMeterEx}, {strData})|{strobjType}|{MarsWindowsAPIsExtend.Dic2String(objProperties)}");
            bool isOk = false;
            bool isAdvancedSelectType = false;
            string strParameter = strParaMeterEx, strPrefix = "", strNormal = "";
            bool isRibbonMode = false;

            if (IsStandardControlType(strobjType))
            {
                return MarsStandardMFCControlKeywordOp.SelectTab(strParaMeterEx, strData, strobjType,
                    strAttachInfo, strPegName,
                    strObjName,
                    objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }

            if (!string.IsNullOrEmpty(strParaMeterEx))
            {
                if (!(isRibbonMode = SelectTab.isRibbonMode(strParameter, ref strParameter)))
                {
                    isAdvancedSelectType = SelectTab.isAdvancedSelectTabPara(strParaMeterEx, ref strPrefix, ref strNormal);
                    if (isAdvancedSelectType)
                    {
                        strParameter = strNormal;
                    }
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_SelectTab", "ribbon Mode");
                }
            }

            if (string.IsNullOrEmpty(strData))
                return true;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }
            ///remove all invisible items ;
            /// 
            ///判断否需要处理visible的消息
            ///
            bool isIgnoreVisible = isVisibleCheckIgnored(strParameter, ref strError, ref strAdv, ref strStack, ref isOk);
            if (!isOk)
            {
                //wrong format
                MarsLoggerSimple.Error("\t", strError);
                return false;
            }
            if (!isIgnoreVisible)
            {
                lstControls.RemoveAll(p =>
                    ((p as System.Windows.Forms.Control) != null)
                    && (!((System.Windows.Forms.Control)p).Visible)
                    );
            }
            else
            {
                lstControls.RemoveAll(p => (p as System.Windows.Forms.Control) == null);
            }
            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]-count:[{2}]", string.Concat(objProperties.Keys), string.Join(";", objProperties.Values.ToArray()), lstControls.Count);
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                strStack = MarsErrorStacks.StackTraceDump();
                bool isTmp = true;
                object oPre = null;
                //for testing print information
                foreach (var itm in lstControls)
                {
                    if (itm == null) continue;
                    Control tmpc = itm as Control;
                    if (oPre == null)
                    {
                        oPre = tmpc;
                    }
                    else
                    {
                        isTmp &= tmpc.Equals(oPre);
                    }
                    if (tmpc == null) continue;
                    string namepath = MarsformIndentifier.MarsGetParentsNames(tmpc.Parent);
                    string strType = ReflectorForCSharp.GetObjectBaseType(itm.GetType());
                    string strPrntTyp = MarsformIndentifier.GetParentTypePath(tmpc);

                    //unsafe
                    //{
                    //    object* pC = (object*)(&itm);
                    MarsLoggerSimple.Info("\t", string.Format("name:[{0}] equal:[{3}] type:[{1}]\r\n\t[{2}]\r\n\tbounds:[{4}]  handle:[{5}] text:[{6}]",
                        tmpc.Name,
                        strPrntTyp,
                        namepath,
                        isTmp,
                        tmpc.Bounds,
                        tmpc.Handle,
                        tmpc.Text
                        ));
                    //Highlight(tmpc);
                    //}
                }
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack, isIgnoreVisible);
            if (!isOk) return false;

            System.Windows.Forms.Control c = (System.Windows.Forms.Control)lstControls[0];
            string strTpe = ReflectorForCSharp.GetObjectBaseType(c.GetType());
            simpleLog.MarsLoggerSimple.Info("SelectTab", string.Format("object type:[{0}]", strTpe));
            if (isRibbonMode)
            {
                return (new MarsToolBarOperation()).SelectTabFromRibbon(c, strParameter, strData, strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            }
            bool isInfragisticsDocking = strTpe.Contains("Infragistics.Win.UltraWinDock.WindowDockingArea");
            if (isInfragisticsDocking)
            {
                return (new MarsTableOperation()).SelectTab4DockingArea(c, strParameter, strData, ref strError, ref strAdv, ref strStack);
            }

            if (strTpe.Contains("Infragistics.Win.UltraWinTabControl.UltraTabControlBase;"))
            {
                simpleLog.MarsLoggerSimple.Info("\t", "come to Infragistics tab base mode");
                MarsTabOperations objTabObj = new MarsTabOperations();
                isOk = objTabObj.SelectTabByCaption(c, strData, strPegName, strObjName, ref strError, ref strAdv, ref strStack, isAdvancedSelectType, strPrefix);
                if (!isOk)
                {
                    MarsLoggerSimple.Error("\t", string.Format("Error when run SelectTab:[{0}]", strError));
                    return false;
                }

                return true;
            }

            if (strTpe.Contains("Infragistics.Win.UltraWinTabbedMdi.MdiTabGroupControl"))
            {
                simpleLog.MarsLoggerSimple.Info("\t", "come to Infragistics grouped tab");
                MarsMdiTabGroupOprations mdiTabOp = new MarsMdiTabGroupOprations();
                isOk = mdiTabOp.SelectTabByCaption(c, strParameter, strData, strPegName, strObjName, ref strError, ref strAdv, ref strStack, isAdvancedSelectType, strPrefix);
                if (!isOk)
                {
                    MarsLoggerSimple.Error("\t", string.Format("Error when run SelectTab:[{0}]", strError));
                    return false;
                }
                return true;
            }

            if (c is System.Windows.Forms.TabControl)
            {
                string strHeaders = "";
                System.Windows.Forms.TabControl tab = (System.Windows.Forms.TabControl)c;
                IntPtr lpdwResult;
                System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();
                MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                            0,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            MarsWindowsAPIs.SMTO_BLOCK,
                            5000,
                            out lpdwResult
                        );
                Point ptNew = default(Point);
                tab.Focus();
                int itargetIdx = -1;
                tab.Invoke(new Action(() =>
                {
                    //tab.GetTabRect()
                    System.Windows.Forms.TabPage targetPage = null;

                    for (int i = 0; i < tab.TabCount; i++)
                    {
                        strHeaders += (";" + tab.TabPages[i].Text);
                        if (MarsWindowsAPIsExtend.RegularTest(strData, tab.TabPages[i].Text) || (string.Compare(strData, tab.TabPages[i].Text, true) == 0))
                        {
                            targetPage = tab.TabPages[i];
                            itargetIdx = i;// tab.TabPages[i].TabIndex;                            
                            System.Drawing.Rectangle rectHead = default(System.Drawing.Rectangle);
                            //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                            //{
                            rectHead = tab.GetTabRect(itargetIdx);
                            ptNew = tab.PointToScreen(
                                    new Point(rectHead.X + rectHead.Width / 2, rectHead.Y + rectHead.Height / 2
                                    )
                                    );

                            simpleLog.MarsLoggerSimple.Info("\t", string.Format("org pos:[{0}] to [{1}]", new Point(rectHead.X + rectHead.Width / 2, rectHead.Y + rectHead.Height / 2
                                    ), ptNew));
                            //}));

                            //MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                            //    0,
                            //    IntPtr.Zero,
                            //    IntPtr.Zero,
                            //    MarsWindowsAPIs.SMTO_BLOCK,
                            //    5000,
                            //    out lpdwResult
                            //);
                            //MarsMessageClientSvc.WaitForPropertyInTime(tab, "CanFocus", "True", 5);

                            //MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                            //MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                            //    0,
                            //    IntPtr.Zero,
                            //    IntPtr.Zero,
                            //    MarsWindowsAPIs.SMTO_BLOCK,
                            //    5000,
                            //    out lpdwResult
                            //);
                            //Thread.Sleep(100);
                            //MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                            //MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                            //    0,
                            //    IntPtr.Zero,
                            //    IntPtr.Zero,
                            //    MarsWindowsAPIs.SMTO_BLOCK,
                            //    5000,
                            //    out lpdwResult
                            //);
                            //Thread.Sleep(100);
                            //MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                            //MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                            //    0,
                            //    IntPtr.Zero,
                            //    IntPtr.Zero,
                            //    MarsWindowsAPIs.SMTO_ABORTIFHUNG,
                            //    5000,
                            //    out lpdwResult
                            //);

                            //Thread.Sleep(500);
                            ;
                            //to make sure that the table index
                            //isOk = tab.SelectedIndex == itargetIdx;

                            //if (!isOk)
                            //{
                            //    tab.SelectedIndex = itargetIdx;
                            //    //simpleLog.MarsLoggerSimple.Error("\t", string.Format("SelectTab index are wrong after click:target-[{0}],currrent-[{1}]", itargetIdx, tab.SelectedIndex));
                            //}
                            isOk = true;
                            return;
                        }
                    }//end of for                    
                }));
                MarsMessageClientSvc.WaitForPropertyInTime(tab, "CanFocus", "True", 5);
                if (ptNew.Equals(default(Point)))
                {
                    isOk = false;
                }
                else
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        bool isTabActived = false;
                        Thread.Sleep(50);
                        MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    MarsWindowsAPIs.SMTO_BLOCK,
                                    5000,
                                    out lpdwResult
                                );
                        MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                        MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                                    0,
                                    IntPtr.Zero,
                                    IntPtr.Zero,
                                    MarsWindowsAPIs.SMTO_BLOCK,
                                    5000,
                                    out lpdwResult
                                );
                        tab.Invoke(new Action(() =>
                        {
                            isTabActived = tab.SelectedIndex == itargetIdx;
                        }));
                        if (isTabActived) break;

                    }

                    Thread.Sleep(100);
                }

                if (isOk) return true;
                isOk = false;
                strError = $"Can't find Tab [{strData}]";// string.Format("Can't find header with caption:[{0}]  from [{1}]", strData, strHeaders);
                strStack = $"Can't find Tab [{strData}] in [{strHeaders}]\r\n{MarsErrorStacks.StackTraceDump()}";
                strAdv = $"Ensure [{strPegName}].[{strObjName}] is visible on the screen.";
            }

            MarsLoggerSimple.Info("\t", strError = $"Unable to locate the object [{strObjName}]"); //string.Format("Find a Tab [{1}] but no assigned process to deal with it for object:{0}", objPegProperties.Select(kv => string.Format("[{0}]-[{1}]", kv.Key, kv.Value)), strTpe));
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = $"Make sure [{strPegName}].[{strObjName}] is avaiable on the screen ";
            return false;
        }

        private static bool AppsideKeywordDeal_SearchAndClick(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            bool isOk = false;
            List<object> lstControls = new List<object>();
            bool isDebugMode = false;
            int iSleepTime = GetDebugSleepTime(strParaMeter ?? "", ref isDebugMode);
            

            /// 增加.net framework control hosted by其他contrainer，如wpf之类
            /// 
            if (objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
            {
                /// 说明是host模式
                /// 
                return HostedFrameworkControlKeywordHelper.SearchAndClick(strParaMeter, strData, strobjType, strAttachInfo, strPegName, 
                    strObjName, objProperties, objPegProperties, 
                    errorCheckObj, 
                    ref strError, 
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }


            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                null,
                180,
                isDebugMode);
            if (!isOk)
            {
                return false;
            }
            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;
            //for 2020-5-3
            string strDataFixed = strData == null ? null : strData.TrimEnd();
            simpleLog.MarsLoggerSimple.Info("\t", string.Format("Data trimed from [{0}] to [{1}]", strData, strDataFixed));

            if (!(new SearchAndClickForInfragisticsGrid()).SearchAndClickFromControl(lstControls[0], strParaMeter, strDataFixed, strPegName, strObjName, ref strError, ref strAdv, ref strStack))
            {
                simpleLog.MarsLoggerSimple.Error("SearchAndClick", strError);
                return false;
            }
            return true;
        }

        private static bool AppsideKeywordDeal_SearchAndUpdate(string strParaMeterEx, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            // strparameter format "DYNAMICROW;_currentPercentColumn;1"

            string strParaMeter = strParaMeterEx;
            if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^Scroll:", strParaMeterEx))
            {
                int iPos = strParaMeterEx.IndexOf(';');
                int iPosComma = strParaMeterEx.IndexOf(':');
                if ((iPos < 0) || (iPosComma < 0) || (iPosComma >= iPos))
                {
                    strError = "Incorrect format for grid cell location";//string.Format("Wrong format of SearchAndUpdate with Scroll:[{0}], it should be:Scroll:Column|Row", strParaMeterEx);
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "See user manual for correct grid location use";
                    return false;
                }
                strAttachInfo = strParaMeterEx.Substring(iPosComma + 1, iPos - iPosComma - 1);
                strParaMeter = strParaMeterEx.Substring(iPos + 1);
            }

            bool isOk = false;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                return false;
            }
            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            string strTypes = ReflectorForCSharp.GetObjectBaseType(lstControls[0].GetType());
            if (strTypes.Contains("Infragistics.Win.UltraWinGrid.UltraGrid"))
            {
                return (new SearchAndUpdateForInfragisticsGrid()).SearchAndUpdate(lstControls[0], strParaMeter, strData, strPegName, strObjName, ref strError, ref strAdv, ref strStack, strAttachInfo);
            }

            return true;
        }

        private static bool AppsideKeywordDeal_SelectListItem(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            bool isOk = false;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                null, 180, string.Compare(strParaMeter == null ? "" : strParaMeter, "debug:ShowChild", true) == 0);
            if (!isOk)
            {
                return false;
            }
            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_SelectListItem", strError);
                return false;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]-count:[{2}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values), lstControls.Count);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                foreach (var itm in lstControls)
                {
                    if (itm == null)
                    {
                        continue;
                    }
                    simpleLog.MarsLoggerSimple.Info("type:[{0}]", itm.GetType().ToString());
                    string strPath = MarsformIndentifier.MarsGetParentsNames(itm as Control);
                    simpleLog.MarsLoggerSimple.Info("copied objects name path:[{0}]", strPath);
                }
                return false;
            }

            string strControlType = ReflectorForCSharp.GetObjectBaseType(lstControls[0].GetType());//.ToString();
            simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_SelectListItem", $"object type path is {strControlType}");

            if (strControlType.Contains("Infragistics.Win.UltraWinTree.UltraTree"))
            {
                return (new MarsTreeViewOperation()).SelectListItem(strData, strParaMeter, (System.Windows.Forms.Control)lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            }
            else if (strControlType.Contains("UltraListView"))
            {
                return (new MarsListViewOperation().SelectListItem(strData, strParaMeter, (System.Windows.Forms.Control)lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack));

            }
            else if (strControlType.Contains("System.Windows.Forms.ListBox"))
            {
                System.Windows.Forms.ListBox lstBox = (System.Windows.Forms.ListBox)lstControls[0];
                return (new MarsListBoxOperation()).SelectListItem(strData, strParaMeter, lstBox, ref strError, ref strAdv, ref strStack);
            }
            else
            {
                if (string.Compare(strobjType, "swftable", true) == 0)
                {
                    return (new MarsTableOperation()).SelectListItemByHeaderAsFilter(strData, strParaMeter, lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Error("SelectListItem", strError = string.Format("unsupported type [{0}] and object type [{1}]", strobjType, strControlType));
                    strError = $"SelectListItem does not support object type for [{strObjName}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Mars supports Infragistics, WinForm and WPF controls";
                }
            }


            //if (strControlType.Contains())
            return false;
        }

        private static bool AppsideKeywordDeal_SelectDropDown(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime = -1)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_SelectDropDown", $"{iMark}|SelectDropDown({strPegName}.{strObjName}, {strParaMeter}, {strData})|{strobjType}|{MarsWindowsAPIsExtend.Dic2String(objProperties)}");

            /// 增加.net framework control hosted by其他contrainer，如wpf之类
            /// 
            if (objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa)) {
                /// 说明是host模式
                /// 
                return HostedFrameworkControlKeywordHelper.SelectDropDown(strParaMeter, strData, strobjType, strAttachInfo, strPegName,
                    strObjName, objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }

            /// 需要支持标准的，如MFC的对象
            /// 
            if (IsStandardControlType(strobjType))
            {
                return MarsStandardMFCControlKeywordOp.SelectDropDown(strParaMeter, strData, strobjType,
                    strAttachInfo, strPegName,
                    strObjName,
                    objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }




            bool isOk = false;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType("SWFCOMBOBOX", strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile,
                null, 60, string.Compare("debug:ShowChild", strParaMeter) == 0);

            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found inside of the PegWindow|{strPegName}|";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Multiple objects with the same identifier were found |{strObjName}|";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            MarsLoggerSimple.Info("\t", string.Format("Find a button:{0}", objPegProperties.Select(kv => string.Format("[{0}]-[{1}]", kv.Key, kv.Value))));
            ///do click
            /// 
            System.Windows.Forms.Control btn = (System.Windows.Forms.Control)lstControls[0];
            if (btn == null)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. null -[{0}] values:[{1}]", string.Concat(",", objProperties.Keys), string.Concat(",", objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen ";
                return false;
            }
            int x = btn.Left + btn.Width / 2, y = btn.Top + btn.Height / 2;
            btn.Focus();
            System.Drawing.Point ptNew = btn.Parent.PointToScreen(new System.Drawing.Point(x, y));
            string strTypes = ReflectorForCSharp.GetObjectBaseType(btn.GetType());
            string strTypeName = btn.GetType().ToString();
            if (string.Compare(strTypeName, "Summit.Framework.View.DDownControl", true) == 0)
            {
                isOk = (new MarsComboboxOperation()).SelectDropDown(strData, strParaMeter, 
                    strPegName, strObjName,
                    btn,
                    errorCheckObj,
                    ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    AppsideKeywordDeal_Dismiss);
            }
            else if (strTypes.IndexOf("System.Windows.Forms.ComboBox",StringComparison.OrdinalIgnoreCase)==0)
            {
                System.Windows.Forms.ComboBox c = btn as System.Windows.Forms.ComboBox;
                if (c == null)
                {
                    strError = $"Can't convert {btn.GetType()} as System.Windows.Forms.ComboBox";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Make sure the object is a combobox";
                    MarsLoggerSimple.Error("AppsideKeywordDeal_SelectDropDown",strError);
                    return false ;
                }
                int findCnt =  0;
                int lastIdx = -1;
                int ttlIdx  = -1;
                string strTtlTxt = "";
                for (int i = 0; i < c.Items.Count; i++)
                {
                    var itm = c.Items[i];
                    if (itm == null) continue;
                    string strTrgt = itm.ToString();
                    strTtlTxt = i == 0 ? strTrgt : $"{strTtlTxt};{strTrgt}";
                    if (MarsWindowsAPIsExtend.RegularTest(strData, strTrgt))
                    {
                        findCnt += 1;
                        lastIdx = i;
                    }
                    if (string.Compare(strTrgt, strData,true)==0)
                    {
                        ttlIdx = i;
                    }
                }
                if (lastIdx < 0)
                {
                    strError = $"no such item [{strData}] from [{strTtlTxt}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Please make sure {strData} is in the Combobox.";
                    MarsLoggerSimple.Error("AppsideKeywordDeal_SelectDropDown", strError);
                    return isOk = false;
                }
                if (findCnt > 1)
                {
                    strError = $"There are [{findCnt}] items matching [{strData}] from [{strTtlTxt}]";
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Please make sure {strData} is the only one in the Combobox.";
                    MarsLoggerSimple.Error("AppsideKeywordDeal_SelectDropDown", strError);
                    return isOk = false;
                }
                c.SelectedIndex = lastIdx;
                //if (c.SelectedIndexChanged!=null)
                //    c.SelectedIndexChanged()
                c.Update();
            }
            else
            {                
                MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
            }

            return isOk;
        }

        public static bool CleanAndTypeInRect(Rectangle c, string strToInput)
        {
            int x = c.Left + 1, y = c.Top + 1, w = c.Width / 2, h = c.Height / 2;
            MarsWindowsAPIsExtend.LeftMouseClick(x, y);
            Thread.Sleep(50);
            System.Windows.Forms.SendKeys.SendWait("{HOME}");
            ///删除所有的
            /// 
            for (int i = 0; i < 100; i++)
            {
                System.Windows.Forms.SendKeys.SendWait("{Del}");
            }
            Thread.Sleep(50);
            //MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
            System.Windows.Forms.SendKeys.SendWait(strToInput);
            System.Windows.Forms.SendKeys.SendWait("{TAB}");
            return true;
        }

        public static bool CleanAndTypeInControl(System.Windows.Forms.Control cntrlTarget, string strToInput, ref String strError, ref string strAdv, ref string strStack)
        {
            var c = cntrlTarget as System.Windows.Forms.Control;
            int x = c.Left + 1, y = c.Top + 1, w = c.Width / 2, h = c.Height / 2;

            System.Drawing.Point ptNew = (cntrlTarget).Parent.PointToScreen(new System.Drawing.Point(x + w, y + h));
            MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
            Thread.Sleep(50);
            System.Windows.Forms.SendKeys.SendWait("{HOME}");
            ///删除所有的
            /// 
            for (int i = 0; i < 100; i++)
            {
                System.Windows.Forms.SendKeys.SendWait("{Del}");
            }
            Thread.Sleep(50);
            //MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
            System.Windows.Forms.SendKeys.SendWait(strToInput);

            System.Windows.Forms.SendKeys.SendWait("{TAB}");
            IntPtr lpdwResult;
            MarsWindowsAPIs.SendMessageTimeout(
                //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)cntrlTarget), ((System.Windows.Forms.Control)cntrlTarget).Handle),
                ((System.Windows.Forms.Control)cntrlTarget).Handle,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                MarsWindowsAPIs.SMTO_BLOCK,
                1000,
                out lpdwResult);
            return true;
        }

        private static bool AppsideKeywordDeal_AddDays(string strParaMeter, string strData, string strobjType,
            string strAttachInfo,
            string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false,
            int waitingTime =-1)
        {
            bool isOk = false;
            List<object> lstControls = new List<object>();
            isOk = GetCurrentControlsFilteredByType(strobjType, strPegName, strObjName, objPegProperties, objProperties, lstControls, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (!isOk)
            {
                return false;
            }

            if ((lstControls == null) || (lstControls.Count <= 0))
            {
                strError = $"Object [{strObjName}] is not found in PegWindow [{strPegName}]";//string.Format("No such object exists  - [{0}].", string.Concat(objProperties.Keys));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Ensure the object is visible on the screen.";
                return false;
            }

            if (lstControls.Count != 1)
            {
                strError = "Multiple objects with the same identifier were found.";//string.Format("more than one objects exist. -[{0}] values:[{1}]", string.Concat(objProperties.Keys), string.Concat(objProperties.Values));
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Ensure that only one object with the identifier [{strObjName}] is present on the screen.";
                return false;
            }

            isOk = WaitForControlIsVisibleAndEnable(lstControls[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
            if (!isOk) return false;

            MarsLoggerSimple.Info("\t", string.Format("Find a object:{0}", objPegProperties.Select(kv => string.Format("[{0}]-[{1}]", kv.Key, kv.Value))));

            int iDaysToAdd;
            if (!int.TryParse(strData, out iDaysToAdd))
            {
                strError = $"Keyword [AddDays] does not support parameter [{strData}]";//string.Format("Days [data] should be a number ,but it is [{0}]", strData);
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Check the keyword/parameter/Data, see user manual";
                return false;
            }
            ///get current date
            ///
            string strObjTypUpper = strobjType.ToUpper();
            string strDataFromControl = "";
            switch (strObjTypUpper)
            {
                case "SWFEDIT":
                    strDataFromControl = (new CaptureValueForSwfEdit()).CaptureValueFromControl(lstControls[0], strParaMeter, strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
                    if (!isOk)
                    {
                        strDataReturn = "";
                        return false;
                    }
                    break;
                default:
                    strError = $"[AddDays] does not support object type for [{strObjName}]";// string.Format("AddDays can only support the swfedit and class inherited from it.but the type current is :[{0}]", strObjTypUpper);
                    StackFrame stck = (new StackFrame());
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = "Mars supports Infragistics, WinForm and WPF controls";
                    strDataReturn = "";
                    return isOk = false;

            }
            //make sure that the data is date formated
            DateTime dteFromCntrl = DateTime.Now;
            if (!DateTime.TryParse(strDataFromControl, out dteFromCntrl))
            {
                strError = $"Object [{strPegName}].[{strObjName}]'s value is not a validate date.";// string.Format("Control value [{0}] is not validate date", strDataFromControl);
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure [{strPegName}].[{strObjName}] is correct";
                return isOk = false;
            }
            dteFromCntrl = dteFromCntrl.AddDays(iDaysToAdd);
            // default format should be mm/dd/yyyy
            string strToInput = dteFromCntrl.ToString("MM/dd/YYYY");
            var c = lstControls[0] as System.Windows.Forms.Control;
            int x = c.Left + 1, y = c.Top + 1, w = c.Width / 2, h = c.Height / 2;

            System.Drawing.Point ptNew = ((System.Windows.Forms.Control)lstControls[0]).Parent.PointToScreen(new System.Drawing.Point(x + w, y + h));
            c.Focus();

            if (c.InvokeRequired)
            {
                string strErrorTmp = "";
                string strAdvTmp = "", strStackTmp = "";
                bool isOkTmp = false;
#if _NET4
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
#else
                c.Invoke(
#endif
                new Action(() =>
                {
                    isOkTmp = CleanAndTypeInControl((System.Windows.Forms.Control)lstControls[0], strToInput, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                }));
                strError = strErrorTmp;
                return isOk = isOkTmp;
            }
            else
            {
                isOk = CleanAndTypeInControl((System.Windows.Forms.Control)lstControls[0], strToInput, ref strError, ref strAdv, ref strStack);
                return isOk;
            }
        }

        private static int GetDebugSleepTime(string strDebugString, ref bool isDebuggerMode)
        {
            int iPos = (strDebugString ?? "").ToUpper().IndexOf("SHOWSTEPS");
            isDebuggerMode = iPos >= 0;
            if (!isDebuggerMode) return -1;
            string sub = strDebugString.Substring(iPos + "SHOWSTEPS".Length + 1);
            int iRsult;
            if (!int.TryParse(sub, out iRsult))
            {
                iRsult = 10; //default
            }
            MarsLoggerSimple.Info("\t", string.Format("sleep data:[{0}]", iRsult * 1000));
            return iRsult * 1000;
        }

        private static string checkAndConvertFillEdit(string strSrc)
        {
            const string cnst_nochange = "nochange:";
            if (string.IsNullOrEmpty(strSrc)) return strSrc;
            if (strSrc.StartsWith(cnst_nochange, StringComparison.OrdinalIgnoreCase))
            {                
                strSrc = strSrc.Substring(cnst_nochange.Length);
                string strRslt = "";
                foreach (char c in strSrc)
                {
                    switch (c)
                    {
                        case '{':
                        case '}':
                        case '%':
                        case '+':
                        case '(':
                        case ')':
                        case '[':
                        case ']':
                        case '^':
                        case '~':
                            strRslt += ("{" + c + "}");
                            break;
                        default:
                            strRslt += c;
                            break;
                    }
                }
                MarsLoggerSimple.Info("checkAndConvertFillEdit", $"{strSrc}|{strRslt}");
                return strRslt;
            }
            else return strSrc;
        }


        //private static KeyValuePair<string, MarsformIndentifier> g_PreviousPegwindow = new KeyValuePair<string, MarsformIndentifier>();
        //private static List<Object> g_currentAllObjectListForCurrentPeg = null;
        /// <summary>
        /// 1, add "nochange:" prefixes on 2/14/2024, once the nochange is added, then convert char when find, for example { should be convert to {{}
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="errorCheckObj"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        private static bool AppsideKeywordDeal_FillEdit(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            /// 需要重新获得form,因为有可能系统还在运行，新窗口需要过几秒
            /// 
            MarsLoggerSimple.logBegin("AppsideKeywordDeal_FillEdit", $"strParameter:{strParaMeter}, waitingTime:{waitingTime}");
            bool isOk = false;
            object oForm = null;
            bool isDebugMode = false;// (strParaMeter??"").ToUpper().IndexOf("SHOWSTEPS")>=0;
            int iSleepTime = GetDebugSleepTime(strParaMeter ?? "", ref isDebugMode);

            /// 增加.net framework control hosted by其他contrainer，如wpf之类
            /// 
            if (objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
            {
                /// 说明是host模式
                /// 
                return HostedFrameworkControlKeywordHelper.FillEdit(strParaMeter, strData, strobjType, strAttachInfo, strPegName,
                    strObjName, objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }

            /// 需要支持标准的，如MFC的对象
            /// 
            if (IsStandardControlType(strobjType))
            {
                return MarsStandardMFCControlKeywordOp.FillEdit(strParaMeter, strData, strobjType,
                    strAttachInfo, strPegName,
                    strObjName,
                    objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    isInnerCall, waitingTime);
            }

            MarsformIndentifier frmTarget = null;
            //if (isRegetPeg)
            //{
                frmTarget = ReGetForm(CurrentPegwindowType, objPegProperties, strPegName, strObjName, ref oForm, ref isOk, ref strError,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,
                    waitingTime);
            //}
            //else
            //{
            //    isOk = true;
            //    frmTarget = g_PreviousPegwindow.Value;
            //}
            if (!isOk)
            {
                return false;
            }
            else
            {
                CurrentPegWindows.Clear();
                CurrentPegWindows.Add(frmTarget);
            }
            //}

            if ((CurrentPegWindows == null) || (CurrentPegWindows.Count == 0))
            {
                StackFrame stck = (new StackFrame());
                strError = $"PegWindow [{strPegName}] is not specified.";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure there is Peg window test step before this test step and there exist peg window for all applications supported by this test case";
                MarsLoggerSimple.Error("AppsideKeywordDeal_FillEdit", strError);
                return false;
            }
            if (CurrentPegWindows.Count > 1)
            {
                strError = $"Multiple peg windows [{strPegName}] were found.";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = $"Make sure only one peg window with identification for [{strPegName}] is found on the screen";
                MarsLoggerSimple.Error("AppsideKeywordDeal_FillEdit", strError);
                return false;
            }
            ///算法 
            /// 1，首先获得所有的子对象
            /// 2，依据已经mapping的类，处理对象的信息
            /// 
            if (!MarsWindowsAPIsExtend.IsWindowsActived(CurrentPegWindows[0].WindowHandle))
            {
                //MarsWindowsAPIs.BringWindowToTop(CurrentPegWindows[0].WindowHandle);
                MarsWindowsAPIs.SetActiveWindow(CurrentPegWindows[0].WindowHandle);
            }
            List<Object> lstChild = null;
            
            lstChild = GetAllChildrenFromParent(CurrentPegWindows[0], strPegName, strObjName, ref isOk, ref strError, ref strAdv, ref strStack);
            if (!isOk)
            {
                MarsLoggerSimple.Error("AppsideKeywordDeal_FillEdit", string.Format("Error when call GetAllChildrenFromParent :[{0}]", strError));
                //g_currentAllObjectListForCurrentPeg = null;
                return false;
            }

            if (isDebugMode)
                foreach (var itm in lstChild)
                {
                    if (itm == null) continue;
                    MarsLoggerSimple.Info("\t", string.Format("control [{0}], type:[{1}] ", ((System.Windows.Forms.Control)(itm)).Name,
                        ((System.Windows.Forms.Control)(itm)).GetType().ToString()));
                }

            /// 2019-2-1 需求
            /// substring
            /// 
            strData = FillEditParaDeal.DealWithPara(strParaMeter, strData, ref isOk, ref strError, ref strAdv, ref strStack);
            if (!isOk)
            {
                simpleLog.MarsLoggerSimple.Error("AppsideKeywordDeal_FillEdit", strError);
                return false;
            }


            IntPtr lpdwResult;
            //to check whether the window is infront of top
            if (!windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.IsWindowsActived(CurrentPegWindows[0].WindowHandle))
                windowsWrapper.SystemUtil.MarsWindowsAPIs.SetForegroundWindow(CurrentPegWindows[0].WindowHandle);
            MarsWindowsAPIs.SendMessageTimeout(
                                CurrentPegWindows[0].WindowHandle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_BLOCK,
                                1000,
                                out lpdwResult);

            /// 有时候没有必要，因为新的应用的出现会有新的类型。另外，数据处理应该很快
            /// 获得所有的edit的
            /// 
            
            List<object> objs = new List<object>();
            lstChild.ForEach(p=>objs.Add(p)); //.Distinct().ToList();
            MarsLoggerSimple.Info("AppsideKeywordDeal_FillEdit", string.Format("Find objects matchs Swfedit Type:[{0}]", objs == null ? 0 : objs.Count));
           
            string strNamePro = "";
            bool isNameRequired = false;
            //isNameRequired = objProperties.Keys.Any(k=> (k!=null) && ((string.Compare("name", k, true) == 0)||(string.Compare("swfname", k, true)==0)));
            foreach (var k in objProperties.Keys)
            {
                if (string.IsNullOrEmpty(k)) continue;
                if (string.Compare("name", k, true) == 0 || string.Compare("swfname", k, true) == 0)
                {
                    strNamePro = k;
                    isNameRequired = true;
                    break;
                }
            }
            MarsLoggerSimple.Info("AppsideKeywordDeal_FillEdit", string.Join(",", objPegProperties.Keys));
            if (isNameRequired) //!string.IsNullOrEmpty(strNamePro))
            {
                //MarsLoggerSimple.Info("\t", string.Format("to check object name:[{0}]", strNamePro));
                objs = objs.Where(p => (p as Control) != null)
                        .Where(p => (string.Compare(((Control)p).Name, objProperties[strNamePro], true) == 0)
                        /*|| (string.IsNullOrEmpty(((Control)p).Name)(MarsWindowsAPIsExtend.RegularTest(objProperties[strNamePro], ((Control)p).Name))*/)
                        .ToList();
                //MarsLoggerSimple.Info("\t", string.Format("count after linq:[{0}]", objs == null ? 0 : objs.Count));
            }

            if ((objs != null) && (objs.Count > 0))
            {
                int iIdx = objs.Count - 1;
                while ((objs.Count > 0) && (iIdx >= 0))
                {
                    object o = objs[iIdx];
                    if (o == null)
                    {
                        objs.RemoveAt(iIdx);
                        iIdx -= 1;
                        continue;
                    }
                    System.Windows.Forms.Control oc = o as System.Windows.Forms.Control;
                    MarsLoggerSimple.Info("AppsideKeywordDeal_FillEdit", string.Format("Current object Type:[{0}] -Name:[{1}]",
                        o.GetType().ToString(), oc.Name));
                    if (o is System.Windows.Forms.Control)
                    {
                        MarsformIndentifier objMarsWithProperties = null;
                        isOk = false;
                        if (!isDebugMode)
                        {

                            ((System.Windows.Forms.Control)o).Invoke(new Action(() =>
                            //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                            {
                                MarsWindowsAPIsExtend.FlashWindowByHandle(((System.Windows.Forms.Control)o).Handle);
                            }));
                        }
                        else
                        {
                            MarsLoggerSimple.Info("\t", string.Format("current control [{0}], type:[{1}] ", ((System.Windows.Forms.Control)(o)).Name,
                                    ((System.Windows.Forms.Control)(o)).GetType().ToString()));
                            Thread.Sleep(2000);
                        }
                        string strErrorTmp = "",
                            strAdvTmp = "",
                            strStackTmp = "";
                        if (oc.InvokeRequired)
                        {
                            oc.Invoke(new Action(() =>
                            {
                                objMarsWithProperties = MarsformIndentifier.FetchControlInfomation((System.Windows.Forms.Control)o, objProperties,
                                    strPegName, strObjName,
                                    ref isOk, ref strErrorTmp, ref strAdvTmp, ref strStackTmp);
                            }));
                            strError = strErrorTmp;
                            strAdv = strAdvTmp;
                            strStack = strStackTmp;
                        }
                        else
                        {
                            objMarsWithProperties = MarsformIndentifier.FetchControlInfomation((System.Windows.Forms.Control)o, objProperties,
                                strPegName, strObjName,
                                ref isOk, ref strError, ref strAdv, ref strStack);
                        }
                        //Thread.Sleep(50);
                        //}                       

                        if (!isOk)
                        {
                            objs.RemoveAt(iIdx);
                            iIdx -= 1;
                            continue;
                        }
                        else
                        {
                            iIdx -= 1;
                        }
                    }
                    else
                    {
                        ///unsupported type
                        /// 
                        MarsLoggerSimple.Error("\t", strError = string.Format("unsupported type with base type information [{0}]", ReflectorForCSharp.GetObjectBaseType(o.GetType())));
                        strError = $"Keyword FillEdit does not support object type for [{strObjName}]";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Mars supports Infragistics, WinForm and WPF controls";
                        iIdx -= 1;
                    }

                    //MarsWindowsAPIsExtend.FlashWindowByHandle(((System.Windows.Forms.Control)o).Handle);
                }

                //if (isDebugMode)
                //{
                //    simpleLog.MarsLoggerSimple.Info("\t", string.Format("debug mode obj count:[{0}]", objs.Count));
                //    return true;
                //}

                /// 处理所有的visible为false的
                /// 
                objs = objs.Where(p => ((p as System.Windows.Forms.Control) != null) && (((System.Windows.Forms.Control)p).Visible)).ToList();

                // filter index 
                if (objs.Count > 1)
                {
                    MarsLoggerSimple.Info("\t", "==Multiple objects were presents, Try filtering it by 'Index'.");
                    string sIdx = null;
                    int iIdxTmp = -1;
                    foreach (var k in objProperties.Keys)
                    {
                        MarsLoggerSimple.Info("\t", string.Format("==k:[{0}]", k));
                        if (string.Compare("index", k, true) == 0)
                        {
                            sIdx = objProperties[k];
                            MarsLoggerSimple.Info("\t", string.Format("index:[{0}]", sIdx));
                            if (!int.TryParse(sIdx, out iIdxTmp))
                                iIdxTmp = 0;
                            break;
                        }
                    }
                    //var objIdx = objProperties.Keys.Where(p => string.Compare(p, "INDEX", true) == 0).FirstOrDefault();

                    if (iIdxTmp != -1)
                    {
                        if (iIdxTmp >= objs.Count)
                        {
                            MarsLoggerSimple.Error("\t", strError = $"Object Index is greater or equal to number of objects for [{strPegName}][{strObjName}]"); //string.Format("Index = [{0}] but only [{1}] returns ", iIdxTmp, objs.Count));
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Check index in Object identification.";
                            return false;
                        }
                        List<System.Windows.Forms.Control> lstSorted = new List<System.Windows.Forms.Control>();
                        foreach (var o in objs)
                        {
                            if (o == null) continue;
                            System.Windows.Forms.Control oc = o as System.Windows.Forms.Control;
                            if (!(o is System.Windows.Forms.Control)) continue;
                            MarsLoggerSimple.Info("\t", string.Format("\ttab index:[{0}] - rectangle:[{1}]", oc.TabIndex, oc.Bounds));
                            lstSorted.Add(o as System.Windows.Forms.Control);
                        }
                        lstSorted = lstSorted.OrderBy(p => p.TabIndex).ToList();                        
                        objs.Clear();
                        objs.Add(lstSorted[iIdxTmp]);
                    }
                }


                if (objs.Count == 1)
                {                   

                    isOk = WaitForControlIsVisibleAndEnable(objs[0], strPegName, strObjName, ref strError, ref strAdv, ref strStack);
                    MarsLoggerSimple.Info("\t", string.Format("object is visible? [{0}]", (objs[0] as Control).Visible));
                    if (!isOk) return false;

                    if (!string.IsNullOrEmpty(strParaMeter) && (strParaMeter.ToUpper().IndexOf(cnst_para_showPath) >= 0))
                    {
                        string strParamterNames = MarsformIndentifier.MarsGetParentsNames(objs[0] as Control);
                        string strPath = MarsformIndentifier.GetParentTypePath(objs[0] as Control);
                        MarsLoggerSimple.Info("\t", string.Format("{2} ParentName Path:[{0}] Type path:[{1}]", strParamterNames, strPath, cnst_para_showPath));
                    }

                    var c = objs[0] as System.Windows.Forms.Control;
                    int x = c.Left + 1, y = c.Top + 1, w = c.Width / 2, h = c.Height / 2;

                    System.Drawing.Point ptNew = ((System.Windows.Forms.Control)objs[0])
                        .Parent
                        .PointToScreen(new System.Drawing.Point(x + w, y + h));

                    string convertedData = checkAndConvertFillEdit(strData);
#if gdienable
                    Point pt = c.Parent == null ? c.PointToScreen(new Point(c.Left, c.Top)) :
                        c.Parent.PointToScreen(new Point(c.Left, c.Top));

                    Rectangle rect = new Rectangle(pt, c.Size);  //c.Parent == null ? c.Parent.RectangleToScreen(c.Bounds) : c.RectangleToScreen(c.Bounds);
                    MarsLoggerSimple.Info("\t", string.Format("rectangle:[{0}] --bounds:[{1}]", rect, c.Bounds));
                    /*
                    windowsWrapper.SystemUtil.XorDrawing.DrawXorRectangleOnDeskTop(
                        new MarsWindowsAPIs.RECT() { Left = rect.Left - 3, Right = rect.Right, Top = rect.Top - 3, Bottom = rect.Bottom },
                        ref strError
                        );
                    */

#endif
                    bool isAutoAddEnterKey = false;
                    if ((!string.IsNullOrEmpty(strParaMeter)) && (strParaMeter.IndexOf("autoAddEnterKey",StringComparison.OrdinalIgnoreCase)>=0)) {
                        isAutoAddEnterKey = true;
                    }
                    if (c.InvokeRequired)
                    {
#if _NET4
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke
#else
                        c.Invoke
#endif
                        (new Action(() =>
                        {
                            simpleLog.MarsLoggerSimple.Info("\t", $"invoke mode, begin to clean, home del mode, para:[{strParaMeter}]");
                            if (isDebugMode)
                            {
                                simpleLog.MarsLoggerSimple.Info("\t", "debug mode clean");
                                Thread.Sleep(iSleepTime);
                            }
                            MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                            
                            Thread.Sleep(10);
                            System.Windows.Forms.SendKeys.SendWait("{HOME}+{END}{BACKSPACE}{DEL}");
                            Thread.Sleep(10);
                            if (isDebugMode)
                            {
                                simpleLog.MarsLoggerSimple.Info("\t", "debug mode: after send del");
                                Thread.Sleep(iSleepTime);
                            }
                            
                            Thread.Sleep(10);                            
                            System.Windows.Forms.SendKeys.SendWait(convertedData);
                            if (isAutoAddEnterKey)
                            {
                                Thread.Sleep(30);
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Thread.Sleep(30);
                            }
                            System.Windows.Forms.SendKeys.SendWait("{TAB}");

                            MarsWindowsAPIs.SendMessageTimeout(
                                //new System.Runtime.InteropServices.HandleRef(((System.Windows.Forms.Control)objs[0]), ((System.Windows.Forms.Control)objs[0]).Handle),
                                ((System.Windows.Forms.Control)objs[0]).Handle,
                                0,
                                IntPtr.Zero,
                                IntPtr.Zero,
                                MarsWindowsAPIs.SMTO_BLOCK,
                                1000,
                                out lpdwResult);
                        }));
                    }
                    else
                    {
                        MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                        Thread.Sleep(10);
                        System.Windows.Forms.SendKeys.SendWait("{HOME}+{END}{BACKSPACE}{DEL}");                       
                        //for some components, type characters, will generate some other sub component, witch takes focus. 
                        //有些日期控件，在输入数字后，会产生一个下拉框。因此，需要每次确保每次都能够接受输入焦点
                        //有些控件，如带mask的，可能需要直接输入
                        if (string.Compare("DirectInput", strParaMeter ?? "", true) != 0)
                        {
                            //MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                            System.Windows.Forms.SendKeys.SendWait(strData);
                        }
                        else
                        {
                            foreach (char c1 in strData)
                            {

                                System.Windows.Forms.SendKeys.SendWait("{END}");
                                System.Windows.Forms.SendKeys.SendWait(c1 + "");
                                MarsWindowsAPIsExtend.LeftMouseClick(ptNew.X, ptNew.Y);
                            }
                        }
                        if (isAutoAddEnterKey)
                        {
                            simpleLog.MarsLoggerSimple.Info("\t", "autoAddEnterKye==true");
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                            Thread.Sleep(30);
                        }
                        System.Windows.Forms.SendKeys.SendWait("{TAB}");                        
                    }
                    return true;
                }
                else
                {
                    MarsLoggerSimple.Error("\t", strError = (objs == null) || (objs.Count <= 0) ? "object not found" : "Multiple objects with the same identifier were found.");//string.Format("More than one object exist or non,count is:[{0}]", objs == null ? 0 : objs.Count));// ; ;
                    strStack = MarsErrorStacks.StackTraceDump();
                    strAdv = $"Make sure object identification [{strObjName}] is correct.Use Object Spy to identify the problem";
                    return false;
                }
            }
            MarsLoggerSimple.Error("\t", strError = "Can't find such object.");
            strStack = MarsErrorStacks.StackTraceDump();
            strAdv = $"Make sure [{strPegName}].[{strObjName}] is avaialbe in the screen";
            return false;
        }



        private static List<object> GetAllChildrenFromParent(MarsformIndentifier marsformIndentifier, string strPegName, string strObjName, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            ///only for winforms
            /// 
            try
            {
                IntPtr lpdwResult;
                MarsWindowsAPIs.SendMessageTimeout(marsformIndentifier.WindowHandle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    30000, //30 秒
                    out lpdwResult);
                System.Windows.Forms.Control currentControl = System.Windows.Forms.Control.FromHandle(marsformIndentifier.WindowHandle);

                List<object> allChildControl = new List<object>();
                allChildControl.Add(currentControl);

                MarsRecursiveGetAllChildren(currentControl, allChildControl);
                //if (currentControl is System.Windows.Forms.Form)
                //{
                //    System.Windows.Forms.Form f = currentControl as System.Windows.Forms.Form;
                //    if ((f != null)&&(f.OwnedForms!=null))
                //    {
                //        foreach(var itm in f.OwnedForms)
                //        {
                //            if (itm == null) continue;
                //            MarsRecursiveGetAllChildren(itm, allChildControl);
                //        }
                //    }
                //}
                isOk = true;
                return allChildControl;
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("GetAllChildrenFromParent", strError = $"Error while searching for a control  [{strPegName}].[{strObjName}]");//string.Format("Exception:[{0}]\r\n{1}", e.Message, e.StackTrace));
                StackFrame stck = (new StackFrame());
                strStack = e.StackTrace;
                strAdv = "Unidentified error. If this continues, contact Marquis";
                isOk = false;
                return null;
            }

        }

        public static void MarsRecursiveGetAllChildren(System.Windows.Forms.Control parentCntrl, List<object> targetList, bool isUnderRevoke = false)
        {
            MarsFrameworkHelper.MarsRecursiveGetAllChildren(parentCntrl, targetList, isUnderRevoke);
        }

        private static bool IsAutoErrorEnableLoged = false;//just log auto error checking log once, the variable is a controller 
        internal static bool DealKeywordByKeywordName(string strKeywordName, string strParameter, string strData, string strobjType,
            Dictionary<string, string> objProperties,
            Dictionary<string, string> objPegProperties,
            string strAttachInfo, string strPegName, string strObjName,
            int waitingTime,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile)
        {
            try
            {
                simpleLog.MarsLoggerSimple.logBegin("DealKeywordByKeywordName");
                if (!AppSideKeywordOperation.ContainsKey(strKeywordName == null ? "" : strKeywordName.ToUpper()))
                {
                    strError = string.Format("Unsupported Keyword:[{0}]", strKeywordName);
                    return false;
                }
                simpleLog.MarsLoggerSimple.Info(strKeywordName, string.Format("properties:[{0}]-[{1}] Data-[{2}]", MarsWindowsAPIsExtend.Dic2String(objProperties),
                    MarsWindowsAPIsExtend.Dic2String(objPegProperties),
                    strData
                    ));

                bool isOk = AppSideKeywordOperation[strKeywordName == null ? "" : strKeywordName.ToUpper()](strParameter, strData, strobjType, strAttachInfo, strPegName, strObjName,
                    objProperties, objPegProperties,
                    errorCheckObj,
                    ref strError,
                    ref strDataReturn,
                    ref strStack,
                    ref strAdv,
                    ref strSnapshotForShouldBeFile,false,iWaitingTime:waitingTime
                    );
                if (!isOk) return false;
                /// add auto checkError
                /// 
                if ((CurrentPegWindows == null)||(CurrentPegWindows.Count<=0)) return isOk;
                if (errorCheckObj == null) return isOk;
                simpleLog.MarsLoggerSimple.Info("DealKeywordByKeywordName", $"auto check error status |{errorCheckObj.IsEnabled}|", isLogOnceMark: IsAutoErrorEnableLoged);
                if (!IsAutoErrorEnableLoged)
                {
                    IsAutoErrorEnableLoged = true;                        
                }
                if (!errorCheckObj.IsEnabled)
                    return isOk;
                string strErrorScreenFileName = "";
                isOk = DealwithErrorAutoCheck(strKeywordName, CurrentPegWindows[0], errorCheckObj, 
                    ref strError, ref strAdv, ref strStack, ref strErrorScreenFileName, ref strDataReturn);
                if (!isOk)
                {
                    strDataReturn = $":{MarsConstants.CNST_AUTO_CHECKERROR_PREFIX}{strDataReturn}:{MarsConstants.CNST_AUTO_CHECKERROR_PREFIX}{strErrorScreenFileName}";

                }
                return isOk;
            }
            catch (Exception e)
            {
                strError = e.Message;
                strStack = e.StackTrace;
                simpleLog.MarsLoggerSimple.Error("DealKeywordByKeywordName",strError, e);
                return false ;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("DealKeywordByKeywordName", strDataReturn);
            }

        }
        private const string cnst_defaultAutoError_Peg = "_SYSTEM_ERROR_CHECK_PEG_";
        private const string cnst_defaultAutoError_Obj = "_ERROR_OBJ_";
        /// <summary>
        /// if keywords in errorobjects is "*" then all keywords would be checked, otherwise, check the "strkeywordName" is in the list
        /// </summary>
        /// <param name="strKeywordName"></param>
        /// <param name="curFrmTarget"></param>
        /// <param name="errorCheckObj"></param>
        /// <param name="strError"></param>
        /// <returns>true, no errors,or not find erros
        ///  False: has errors
        /// </returns>
        private static bool DealwithErrorAutoCheck(string strKeywordName, MarsformIndentifier curFrmTarget, 
            MarsErrorCheckData errorCheckObj, 
            ref string strError,
            ref string strAdv, 
            ref string strStack,
            ref string strErrorScreenFile,
            ref string strErrorReturn)
        {
            string[] cnst_unAutoErrorChckKeywords = { "PRESSKEYS", "AUTOCHECKERROR","CHECKERROR", "LAUNCHAPPLICATION", "PEGWINDOW","DISMISS", 
                ClientDealWithGUIKeyword.cnst_previewobject,"WAITUNTIL", "_STARTOBJECTSPY","_RELOADKEYWORD_TYPE_MAP" };
            simpleLog.MarsLoggerSimple.logBegin("DealwithErrorAutoCheck", $"{strKeywordName}");
            try
            {
                bool isAllKeywords = false;
                if (errorCheckObj == null) return true;
                
                if (cnst_unAutoErrorChckKeywords.FirstOrDefault(p => p.Equals(strKeywordName, StringComparison.OrdinalIgnoreCase)) != null)
                {
                    MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"{strKeywordName}|is one of the unAutoErrorcheckKeyword");
                    return true;
                }

                
                // removed on 2023 11 27
                //bool isToCheckError = isAllKeywords;
                //if (!isAllKeywords)
                //{
                //    if (errorCheckObj.KEYWORDS.FirstOrDefault(p=>p.Equals(strKeywordName, StringComparison.Ordinal)) == null){
                //        isToCheckError = true;
                //    }
                //}
                //if (!isToCheckError)
                //{
                //    simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"not find|{strKeywordName}|in|{errorCheckObj.KEYWORDS}|");
                //    return true;
                //}
                if (curFrmTarget == null)
                {
                    simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", "no curFrmTarget is find, no error objects is hosted. just return true");
                    return true;
                }
                
                // find target objects from curFrmTarget
                bool isOk = false;

                var lstChild = GetAllChildrenFromParent(curFrmTarget, cnst_defaultAutoError_Peg, cnst_defaultAutoError_Obj, ref isOk, ref strError, ref strAdv, ref strStack);
                //filter objects
                List<System.Windows.Forms.Control> targetLst = new List<System.Windows.Forms.Control>();

                // build MarsErrorCheckData to Dictionary
                Dictionary<string, string> objProperties = new Dictionary<string, string>();
                bool isToCheckError = false;
                int iScreenMode = -1;
                int iIdx = 0;
                foreach (var idObj in errorCheckObj.Error_Objects)
                {
                    simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"----------begin error auto check|{iIdx}|---------- ");
                    objProperties.Clear();
                    if (idObj == null) continue;
                    if (idObj.Error_Object == null) continue;
                    simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"----------begin error auto check|{strKeywordName}|----------|{idObj.Error_Object.Keywords[0]}|");
                    if ((idObj.Error_Object.Keywords == null) // check all keyword
                        || (idObj.Error_Object.Keywords.Count == 0)
                        || ((idObj.Error_Object.Keywords.Count >= 1)
                        && (idObj.Error_Object.Keywords[0] == "*")
                        ))
                    {
                        isToCheckError = true;
                    }
                    else if (idObj.Error_Object.Keywords.Any(p => p.Equals(strKeywordName, StringComparison.OrdinalIgnoreCase)))
                    {
                        isToCheckError = true;
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"no find|{strKeywordName}| and the keywords list is not null or empty");
                        isToCheckError = false;
                    }
                    if (!isToCheckError) continue;
                    iScreenMode = -1;
                    if (idObj.Error_Object.IMAGE != null){
                        if (!idObj.Error_Object.IMAGE.enabled)
                        {
                            iScreenMode = 0;
                        }
                        else
                        {
                            if (string.Compare("Pegwindow", idObj.Error_Object.IMAGE.scope ?? "", true) == 0)
                            {
                                iScreenMode = 1;
                            }
                            else iScreenMode = 2;
                        }
                    }


                    foreach (var quickAcc in idObj.Error_Object.ObjQuickAccess)
                    {
                        if (quickAcc == null) continue;
                        if (string.IsNullOrEmpty(quickAcc.id)) continue;
                        if (objProperties.ContainsKey(quickAcc.id)) objProperties[quickAcc.id] = quickAcc.value;
                        else objProperties.Add(quickAcc.id, quickAcc.value);
                    }

                    if (objProperties.Keys.Count <= 0)
                    {
                        // nothing is set, just ignore
                        simpleLog.MarsLoggerSimple.Warnning("DealwithErrorAutoCheck", "no validate error objects info fetched, no error check");
                        continue; // 
                    }

                    // build 
                    string strTmpError = "", strTmpAdv = "", strTmpStack = "", strTmpReturn = "";
                    foreach (var itm in lstChild)
                    {                        
                        if ((itm == null) || (!(itm is System.Windows.Forms.Control))) continue;

                        System.Windows.Forms.Control tmpCntrl = null;
                        if (itm is System.Windows.Forms.Control)
                        {
                            tmpCntrl = itm as System.Windows.Forms.Control;
                        }
                        if (tmpCntrl == null) continue;
                        if (tmpCntrl.InvokeRequired)
                        {
                            //simpleLog.MarsLoggerSimple.Info("\t","invoke required" );                         
                            
                            tmpCntrl.Invoke(new Action(()=>
                            {
                                //simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", "invoke mode");
                                var obj = MarsformIndentifier.FetchControlInfomation((System.Windows.Forms.Control)itm, objProperties, cnst_defaultAutoError_Peg,
                                    cnst_defaultAutoError_Obj, ref isOk, ref strTmpError, ref strTmpAdv, ref strTmpStack);
                                //if (obj != null)
                                //{
                                //    //   
                                //}
                                //else
                                //{
                                //    simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", "no object found");
                                //}
                                if ((isOk) && (obj != null) && (obj.IsControlVisible))
                                    targetLst.Add((System.Windows.Forms.Control)itm);
                            }));
                            
                        }
                        else
                        {
                            var obj = MarsformIndentifier.FetchControlInfomation((System.Windows.Forms.Control)itm, objProperties, cnst_defaultAutoError_Peg,
                                cnst_defaultAutoError_Obj, ref isOk, ref strError, ref strAdv, ref strStack);
                            
                            if ((isOk) && (obj != null) && (obj.IsControlVisible))
                                targetLst.Add((System.Windows.Forms.Control)itm);
                            
                        }
                    }
                    if (targetLst.Count <= 0)
                    {
                        simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", "No Error container object is found");
                        continue; /// there could more than one error objects
                        //return true;
                    }
                    if (targetLst[0] is System.Windows.Forms.TreeView)
                    {
                        //for summit
                        System.Windows.Forms.TreeView t = targetLst[0] as System.Windows.Forms.TreeView;
                        //if find imageindex is 2 the eror                                                
                        foreach (var n in t.Nodes)
                        {
                            if (n == null) continue;
                            object imgidx = ReflectorForCSharp.GetMember(n, "ImageIndex");
                            if (imgidx is int)
                            {
                                if (((int)imgidx) == 2)
                                {
                                    object tx = ReflectorForCSharp.GetMember(n, "Text");
                                    simpleLog.MarsLoggerSimple.Error("DealwithErrorAutoCheck", tx == null ? "[n/a]" : tx.ToString());
                                    if (string.IsNullOrEmpty(strTmpError))
                                    {
                                        strTmpError = $"{tx}";
                                    }
                                    else
                                    {
                                        strTmpError = $"{strTmpError}\r\n{tx}";
                                    }

                                    strStack = MarsErrorStacks.StackTraceDump();
                                    strAdv = "Make sure Error From summit is fixed";
                                    //return false;

                                }
                                else
                                {
                                    simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"get image index|{imgidx}|");
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(strTmpError))
                        {
                            strError = $"{strError}\r\nMARS ERROR BEGIN:[{strTmpError}]";
                            simpleLog.MarsLoggerSimple.Error("DealwithErrorAutoCheck", strError);
                        }
                        /**
                         * 保存screen文件
                         * */
                        bool tmpOk = false;
                        string strTmpError1 = "", strAdvTmp = "", strStackTmp = "";
                        if (iScreenMode == 2)
                        {
                            strErrorScreenFile = (new Snapshot()).SnapshotScreen(targetLst[0], "", cnst_defaultAutoError_Peg, cnst_defaultAutoError_Obj,
                                ref tmpOk, ref strTmpError1, ref strAdvTmp, ref strStackTmp);
                            strErrorScreenFile = $"{MarsErrorCheckConst.cnst_error_file_prefix}{strErrorScreenFile}";
                        }
                        else if (iScreenMode == 1)
                        {
                            System.Windows.Forms.Control tmpPeg = System.Windows.Forms.Control.FromHandle(curFrmTarget.WindowHandle);
                            strErrorScreenFile = (new Snapshot()).SnapshotScreen(tmpPeg, "", cnst_defaultAutoError_Peg, cnst_defaultAutoError_Obj,
                                ref tmpOk, ref strTmpError1, ref strAdvTmp, ref strStackTmp);
                            strErrorScreenFile = $"{MarsErrorCheckConst.cnst_error_file_prefix}{strErrorScreenFile}";
                        }
                        simpleLog.MarsLoggerSimple.Error("DealwithErrorAutoCheck", $"{strError}|{strTmpError1}|{strErrorScreenFile}");
                        return false;
                    }   
                    else
                    {
                        
                        if (targetLst[0].InvokeRequired)
                        {
                            targetLst[0].Invoke(new Action(() =>
                            {
                                Control c = targetLst[0];
                                if ((idObj.Error_Object != null) && (idObj.Error_Object.errorMessage != null)
                                && (!string.IsNullOrEmpty(idObj.Error_Object.errorMessage.propertyName)))
                                {
                                    var oErrorMessage = ReflectorForCSharp.GetPropValue(c, idObj.Error_Object.errorMessage.propertyName);
                                    if (oErrorMessage != null)
                                    {
                                        simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"error message:|{oErrorMessage.ToString()}");
                                        strTmpReturn = oErrorMessage.ToString();
                                    }
                                    else
                                    {
                                        simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"error message:|null");
                                    }
                                }
                            }));
                            strErrorReturn = strTmpReturn;
                        }
                        else
                        {
                            if (targetLst[0] != null)
                            {
                                //simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"{obj.WindowHandle}|visible|{obj.IsControlVisible}");
                                var oErrorMessage = ReflectorForCSharp.GetPropValue(targetLst[0], idObj.Error_Object.errorMessage.propertyName);
                                if (oErrorMessage != null)
                                {
                                    simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", $"error message:|{oErrorMessage.ToString()}");
                                    strErrorReturn = oErrorMessage.ToString();
                                }
                            }
                        }
                        strError = $"{strKeywordName} causes Error|{strErrorReturn}|";
                        /**
                        * 保存screen文件
                        * */
                        bool tmpOk = false;
                        string strTmpError1 = "", strAdvTmp = "", strStackTmp = "";
                        if (iScreenMode == 2)
                            strErrorScreenFile = (new Snapshot()).SnapshotScreen(targetLst[0], "", cnst_defaultAutoError_Peg, cnst_defaultAutoError_Obj,
                                ref tmpOk, ref strTmpError1, ref strAdvTmp, ref strStackTmp);
                        else if (iScreenMode == 1)
                        {
                            System.Windows.Forms.Control tmpPeg = System.Windows.Forms.Control.FromHandle(curFrmTarget.WindowHandle);
                            strErrorScreenFile = (new Snapshot()).SnapshotScreen(tmpPeg, "", cnst_defaultAutoError_Peg, cnst_defaultAutoError_Obj,
                                ref tmpOk, ref strTmpError1, ref strAdvTmp, ref strStackTmp);
                        }
                        simpleLog.MarsLoggerSimple.Error("DealwithErrorAutoCheck", $"{strError}|{strTmpError1}|{strErrorScreenFile}");

                        return false;
                    }
                }
                simpleLog.MarsLoggerSimple.Info("DealwithErrorAutoCheck", "no error find, just reutrn true");
                return true;
            }
            catch (Exception e)
            {
                strError = $"Can't auto checkError";
                if ((errorCheckObj != null) && (errorCheckObj.IsIgnoreIfException))
                {
                    simpleLog.MarsLoggerSimple.Warnning("DealwithErrorAutoCheck", $"isIgnoreIfException is true, so Ignored, but exception is|{e.Message}|");
                    return true;
                }
                simpleLog.MarsLoggerSimple.Error("DealwithErrorAutoCheck", e.Message, e);
                return false;
            }
        }

        private static List<MarsformIndentifier> CurrentPegWindows = new List<MarsformIndentifier>(); //it should be only one node
        private static MarsformIndentifier PreviousPegWindows = null;
        private static string CurrentPegwindowType;

        

        static bool IsSetCapturedMouse = false;
        private static bool AppsideKeywordDeal_Pegwindow(string strParameter, string strData, string strobjType,
            string strAttachInfo, string strPegName,
            string strObjName,
            Dictionary<string, string> objProperties,
            Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError,
            ref string strDataReturn,
            ref string strStack,
            ref string strAdv,
            ref string strSnapshotForShouldBeFile,
            bool isInnerCall = false, int waitingTime =-1)
        {
            simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_Pegwindow", string.Format("{0} properties:{1}", strParameter, MarsWindowsAPIsExtend.Dic2String(objProperties)));

            //g_PreviousPegwindow = default(KeyValuePair<string, MarsformIndentifier>);
            //g_PreviousPegwindow = new KeyValuePair<string, MarsformIndentifier>();// re init

            //if (string.Compare("swfwindow", strobjType,true)==0)
            //{
            PreviousPegWindows = ((CurrentPegWindows == null) || (CurrentPegWindows.Count < 1)) ? null : CurrentPegWindows[0];
            CurrentPegWindows.Clear();
            CurrentPegwindowType = "";

            //if (!IsSetCapturedMouse)
            //{
            //    var p = Process.GetCurrentProcess();
            //    MarsWindowsAPIs.SetCapture(p.MainWindowHandle);
            //    IsSetCapturedMouse = true;
            //}

            //首先判断是否当前进程busy
            Mars.message.Inter.MQCenter.MarsObjectsOperations.MarsObjectOpBase.WaitUntilCurrentProcessIsNotBusy();

            object targetForm = null;
            bool isOk = false;

            //move mouse           

            //making a mouse moving to make sure no hint or tool tip windows, 
            // otherwise, it takes long time to enum windows
            System.Drawing.Point pt = System.Windows.Forms.Cursor.Position;
            //for (int i = 0; i < 5; i++)
            //{
            //    MarsWindowsAPIs.SetCursorPos(pt.X+i*15, pt.Y+i*15);
            //    Thread.Sleep(10);
            //    //MarsWindowsAPIs.SetCursorPos(pt.X + (i+1) * 15, pt.Y + (i+1) * 15);
            //}

            System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();
            try
            {
                objCurP.WaitForInputIdle(60000);
            }
            catch (Exception)
            {
                System.Threading.Thread.Sleep(1000);
            }

            simpleLog.MarsLoggerSimple.Info("AppsideKeywordDeal_Pegwindow", "1");
            MarsformIndentifier tmpFormInfo = ReGetForm(strobjType, objPegProperties, strPegName, strObjName, ref targetForm, ref isOk, ref strError,
                ref strStack,
                ref strAdv,
                ref strSnapshotForShouldBeFile);
            if (isOk)
            {
                CurrentPegWindows.Add(tmpFormInfo);
                CurrentPegwindowType = strobjType;

                if ((!string.IsNullOrEmpty(strParameter)) && (strParameter.IndexOf("BRING_TO_TOP", StringComparison.OrdinalIgnoreCase) >= 0)){
                    windowsWrapper.SystemUtil.MarsWindowsAPIs.BringWindowToTop(tmpFormInfo.WindowHandle);
                }

                // set to cache
                //g_PreviousPegwindow = new KeyValuePair<string, MarsformIndentifier>(strPegName, tmpFormInfo);// re init
                //g_currentAllObjectListForCurrentPeg = null;

                IntPtr lpdwResult;
                MarsWindowsAPIs.SendMessageTimeout(tmpFormInfo.WindowHandle,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    MarsWindowsAPIs.SMTO_BLOCK,
                    30000, //30 秒
                    out lpdwResult);
                MarsLoggerSimple.Info("\t", "pegwindow returns ok");
                return true;
            }
            else
            {
                MarsLoggerSimple.Info("\t", "pegwindow returns false " + strError);
                return false;
            }
            #region old code
            /*
            MarsLoggerSimple.Info("AppsideKeywordDeal_Pegwindow", string.Format("Peg identification:[{0}]", objPegProperties==null?"": MarsWindowsAPIsExtend.Dic2String(objPegProperties)));
            /// 注意：
            /// 这里需要等待一段时间 如果系统在忙，默认时间2分钟
            /// 首先判断系统是否忙
            bool isStopWait = false;
            System.Diagnostics.Process objCurP = System.Diagnostics.Process.GetCurrentProcess();
            if (objCurP == null) return false;
            IntPtr lpdwResult;
            MarsWindowsAPIs.SendMessageTimeout(objCurP.MainWindowHandle,
                0,
                IntPtr.Zero,
                IntPtr.Zero,
                MarsWindowsAPIs.SMTO_ABORTIFHUNG,
                30000, //30 秒
                out lpdwResult);
            /// 可能找不到，需要等待一段时间
            //DateTime dtStart = DateTime.Now;
            //while (!isStopWait)
            //{

            //}
            System.Windows.Forms.FormCollection allForms=System.Windows.Forms.Application.OpenForms;

            IntPtr mainHwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            System.Diagnostics.EventLog.WriteEntry("MarsEvent", string.Format("forms number find:[{0}]", allForms==null?0:allForms.Count));
            MarsLoggerSimple.Info("AppsideKeywordDeal_Pegwindow", string.Format("forms number find:[{0}]", allForms == null ? 0 : allForms.Count));
            if (!((allForms != null) && (allForms.Count > 0)))
            {
                System.Diagnostics.EventLog.WriteEntry("MarsEvent", strError = "No forms find from current window. ",System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
            string strInfo = "";            
            bool isControlVisible = false;
            for (int i=0;i<allForms.Count;i++)
            {
                System.Windows.Forms.Form itm = allForms[i];
                MarsLoggerSimple.Info("AppsideKeywordDeal_Pegwindow",string.Format("Current form type:[{0}]",itm.GetType().ToString()));
                MarsformIndentifier objMarsWithProperties = MarsformIndentifier.FetchPegwindowInformation(itm, objPegProperties,ref isOk, ref strError);
                if (!isOk) continue;
                
                if (!(isControlVisible=MarsWindowsAPIs.IsWindowVisible(itm.Handle)))
                {                   
                    strInfo += string.Format("{0} notVisible, MainWindow [{1}] index:[{2}];", itm == null ? "NULL" : itm.GetType().ToString(), itm.Handle==mainHwnd, i);
                    objMarsWithProperties.IsControlVisible = false;
                }
                else
                {
                    objMarsWithProperties.IsControlVisible = true;
                    
                    ///注意：
                    /// 这个地方判断是否是main window
                    //strInfo += string.Format("{0} Visible Mainwindow [{1}] index:[{2}];", itm == null ? "NULL" : itm.GetType().ToString(), itm.Handle == mainHwnd, i);
                    //if (itm.Handle == mainHwnd)
                    //{
                    //    ///find main window.
                    //    /// 算法：
                    //    /// 1， 判断是否mainwindow符合需要的条件
                    //    /// 
                    //    if (objPegProperties == null)
                    //    {
                    //        MarsLoggerSimple.Error("AppsideKeywordDeal_Pegwindow", strError = "No Parent object identification passed");
                    //        return false;
                    //    }

                    //}
                    //MarsWindowsAPIsExtend.FlashWindowByHandle(itm.Handle);
                    //break;
                }
                CurrentPegWindows.Add(objMarsWithProperties);
            }

            if (CurrentPegWindows.Count!=1)
            {
                if (CurrentPegWindows.Count > 1)
                {
                    var w = CurrentPegWindows.Count(p => p.IsControlVisible);
                    if (w != 1)
                    {
                        strError = string.Format("More than one or none target top form is find. number find:[{0}]", CurrentPegWindows.Count);
                        return false;
                    }
                    else
                    {
                        int iLoop = CurrentPegWindows.Count - 1;
                        while(iLoop>=0)
                        {
                            if (CurrentPegWindows[iLoop].IsControlVisible)
                            {
                                iLoop-=1;
                            }
                            else
                            {
                                CurrentPegWindows.RemoveAt(iLoop);
                                iLoop -= 1;
                            }
                        }
                    }
                }
                else
                {
                    strError = string.Format("none target top form is find. number find:[{0}]", CurrentPegWindows.Count);
                    return false;
                }
            }

            MarsWindowsAPIsExtend.FlashWindowByHandle(CurrentPegWindows[0].WindowHandle);
            //System.Diagnostics.EventLog.WriteEntry("MarsEvent", strInfo);
            MarsLoggerSimple.Info("AppsideKeywordDeal_Pegwindow", "Find window and flashed");
            //}
            return true;
            */
            #endregion
        }

        private static bool WaitForControlPropertyEquals(Control oc, string properName, object ov, int iWaitForMillionSec, ref string strError,
            ref string strAdv, ref string strStack)
        {
            bool isNotExist = false;
            object oP = ReflectorForCSharp.GetMember(oc, properName, ref isNotExist);
            if ((oP == null) || (isNotExist))
            {
                strError = $"Object property [{properName}] is NULL";// $"can't find {properName} from type [{oc.GetType().ToString()}]";
                strStack = $"can't find {properName} from type [{oc.GetType().ToString()}]\r\n{MarsErrorStacks.StackTraceDump()}";
                strAdv = "Contact Marquis";
                return false;
            }
            long n, c = DateTime.Now.Ticks;
            n = c;
            while (((c - n) / TimeSpan.TicksPerMillisecond) < iWaitForMillionSec)
            {
                object rv = ReflectorForCSharp.GetMember(oc, properName);
                try
                {
                    if (rv.Equals(ov))
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    strError = $"Error while getting object proreprty [{properName}]";// e.Message;
                    StackFrame stck = (new StackFrame());
                    strStack = $"{e.Message}\r\n{MarsErrorStacks.StackTraceDump()}";
                    strAdv = "Unidentified error. If this continues, contact Marquis";
                }
                finally
                {
                    Thread.Sleep(1000);
                    c = DateTime.Now.Ticks;
                }
            }
            return true;
        }

        private static bool WaitForControlVisible(Control oc, int iWaitForMillionSec, ref string strError)
        {
            long n, c = DateTime.Now.Ticks;
            n = c;

            while (((c - n) / TimeSpan.TicksPerMillisecond) < iWaitForMillionSec)
            {
                if (oc.IsDisposed)
                {
                    simpleLog.MarsLoggerSimple.Info("\t", "IsDisposed");
                    return false;
                }

                //if (!oc.IsAccessible)
                //{
                //    simpleLog.MarsLoggerSimple.Info("\t", "not IsAccessible");
                //    return false;
                //}                
                bool isOk = MarsWindowsAPIs.IsWindowVisible(oc.Handle);
                if (isOk) return true;
                Thread.Sleep(1000);
                c = DateTime.Now.Ticks;

            }
            return false;
        }

    }

    
    public class MarsTCPClientSvc
    {
        private static int currentSvrPort = -1;
        private TcpClient client = null ;
        private static Thread clientRecvThread = null;
        private static Thread heartBeatThread  = null;
        private static bool isContinue2Run = false;
        private static MarsTCPClientSvc Instance = null ;

        public const int cnst_beating_test_time = 30;

        private MarsTCPClientSvc()
        {

        }

        /// <summary>
        /// 该方法会被注射器直接调用
        /// </summary>
        /// <param name="iPort"></param>
        public static void StartTCPEngineSvc(int iPort)
        {
            currentSvrPort = iPort;

            if (clientRecvThread != null)
            {
                isContinue2Run = false;
                clientRecvThread.Abort();
            }
            clientRecvThread = null;
            if (heartBeatThread != null)
            {
                isContinue2Run = false;
                heartBeatThread.Abort();
            }
            heartBeatThread = null;

            if (Instance != null)
            {
                Instance.closeTcpClient();

            }
            else
            {
                Instance = new MarsTCPClientSvc();
            }
            
            

            try
            {
                Instance.client = new TcpClient("localhost", currentSvrPort);
                string strError = "", strStack = "";
                // wait for connected
                WaitForConnected();

                bool v = Instance.SendShakingHand(ref strError, ref strStack);
                clientRecvThread = new Thread(new ThreadStart(CommunitToTcpSvr));
                clientRecvThread.IsBackground = true;
                clientRecvThread.Start();

                heartBeatThread = new Thread(new ThreadStart(Instance.TestHeartBeating));
                heartBeatThread.IsBackground = true;
                heartBeatThread.Start();
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("StartTCPEngineSvc",e.Message, e);
            }
            

        }

        private static void WaitForConnected(int iWaitSeconds = 5)
        {
            long n = DateTime.Now.Ticks, p;
            while (((p = DateTime.Now.Ticks) - n) < (TimeSpan.TicksPerSecond * iWaitSeconds))
            {
                if (!Instance.client.Connected)
                {
                    Thread.Sleep(200);
                }
                else
                {
                    return;
                }
            }
        }

        private void TestHeartBeating()
        {
            string strError = "", strStack = "";
            
            while (isContinue2Run)
            {
                try
                {
                    if (!SendShakingHand(ref strError, ref strStack))
                    {
                        simpleLog.MarsLoggerSimple.Error("TestHeartBeating", strError, strStack);
                    }
                }catch(Exception e)
                {
                    simpleLog.MarsLoggerSimple.Error("TestHeartBeating", e.Message, e);
                }
                finally
                {
                    Thread.Sleep(cnst_beating_test_time*1000);
                }
            }
        }

        public bool SendShakingHand(ref string strError, ref string strStack)
        {
            TCPActionDataShakingHand ping = new TCPActionDataShakingHand();
            if ((client == null)||(!client.Connected))
            {
                simpleLog.MarsLoggerSimple.Error("SendShakingHand", strError = "TCP is not initialized, or not connected to Server");
                strStack = Environment.StackTrace;
                return false;
            }
            simpleLog.MarsLoggerSimple.Info("SendShakingHand", $"client has connected to {client.Client.RemoteEndPoint.ToString()}");
            try
            {
                string strMessageJson = ping.GetJson();
                if (strMessageJson == null)
                {
                    //錯誤
                    simpleLog.MarsLoggerSimple.Error("SendShakingHand", "Ping object return null after invoke GetJSON", Environment.StackTrace);
                    return false;
                }
                byte[] by = Encoding.ASCII.GetBytes(strMessageJson);
                client.GetStream().Write(by, 0, by.Length);
                return true;
            }
            catch (Exception e)
            {
                strStack = e.StackTrace;
                simpleLog.MarsLoggerSimple.Error("SendShakingHand", strError = e.Message, e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("SendShakingHand");
            }
            
            
        }

        private static void CommunitToTcpSvr()
        {
            isContinue2Run = true;
            byte[] arrDataFromSvr = new byte[2048];  
            while (isContinue2Run)
            {
                try
                {
                    if (Instance == null)
                    {
                        ///不可能，但是避免万一
                        ///
                        Thread.Sleep(5000);
                        simpleLog.MarsLoggerSimple.Error("CommunitToTcpSvr", "instance is null,wrong!!!!");
                        continue;
                    }
                    int iCnt = 0;
                    StringBuilder sb = new StringBuilder();
                    while ((iCnt = Instance.client.GetStream().Read(arrDataFromSvr,0, arrDataFromSvr.Length)) > 0)
                    {
                        string responseData = System.Text.Encoding.ASCII.GetString(arrDataFromSvr, 0, iCnt);
                        sb.Append(responseData);                        
                    }
                    /// 字符串应该是一个json
                    /// 
                    try
                    {
                        //Basic
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }
                finally
                {

                }
            }
        }

        private void closeTcpClient()
        {
            isContinue2Run = false;
            if (client == null) return;
            try
            {
                client.Close();
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("closeTcpClient", e.Message, e);
            }            
        }
    }

}
