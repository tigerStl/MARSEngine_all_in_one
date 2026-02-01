using Mars.AutoTestingDriver.injector;
using Mars.AutoTestingDriver.interProcess;
using Mars.AutoTestingDriver.message;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mars.InjectorAgent
{
    class MarsGuiInjectorAgent
    {
        private const string cnst_spyFileName = "ManagedInjector64-4.0";
        private const string cnst_spyInjectorType = "ManagedInjector.Injector";
        private const string cnst_method_isInjected = "IsInjectedById";
        private const string cnst_method_launch = "Launch";
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsGuiInjectorAgent));
        /// <summary>
        /// 注射实例
        /// </summary>
        private object Injector = null;
        private Type ManagedInjector_Injector = null;
        internal bool IsInjected(int processId, string strProcessName,ref string strError, ref bool isError)
        {
            Logger.logBegin("IsInjected",string.Format("processId:[{0}] ProcessName:[{1}]",processId, strProcessName));
            try
            {
                bool isOk = true;
                if (ManagedInjector_Injector == null)
                {
                    Injector = InitInjector(ref isOk, ref strError);
                    if (isError = (!isOk))
                    {
                        ManagedInjector_Injector = null;
                        return false;
                    }
                        
                }
                ///inject to target process
                ///
                
                MethodInfo methodIsInjected = ManagedInjector_Injector.GetMethod(cnst_method_isInjected);
                if (methodIsInjected ==null)
                {
                    Logger.Error("IsInjected", strError= string.Format("No such method [{1}] find in type [{0}]", ManagedInjector_Injector.FullName, cnst_method_isInjected));
                    return !(isError = true);
                }
                object oRslt = methodIsInjected.Invoke(null, new object[] { processId});
                if (oRslt is bool)
                {
                    return (bool)oRslt;
                }
                else
                {
                    Logger.Error("IsInjected",strError = string.Format("Result is not bool, it is [{0}]", oRslt==null?"NULL":oRslt.GetType().ToString()));
                    return !(isError = true);
                }
                
            }
            catch (Exception e)
            {
                Logger.Error("IsInjected",string.Format("Exception:[{0}], statckTrace:[{1}]",e.Message,e.StackTrace),e);
                isError = false;
                return false;
            }
            finally
            {
                Logger.logEnd("IsInjected");
            }
        }

        internal void InjectToTargetProcess(int processId,ref string strError, ref bool isOk)
        {
            Process p = Process.GetProcessById(processId);
            if (p == null)
            {
                strError = string.Format("Can't get process by id:[{0}]", processId);
                isOk = false;
                return;
            }
            try
            {
                IntPtr pMainHandle = p.MainWindowHandle;
                System.Reflection.MethodInfo pMethod_Launch = ManagedInjector_Injector.GetMethod(cnst_method_launch);
                if (pMethod_Launch == null)
                {
                    strError = string.Format("can't get {0} from injector", cnst_method_launch);
                    isOk = false;
                    return;
                }
                string strPathOfMars = System.IO.Path.GetDirectoryName(typeof(MarsGuiInjectorAgent).Assembly.Location);
                //string strPathOfAgent =
                pMethod_Launch.Invoke(null, new object[] { pMainHandle, System.IO.Path.Combine(strPathOfMars, "MarsInterMQCenter.dll"), "Mars.Inter.MQCenter.interProcess.MarsMessageClientSvc", "StartMonitorThread" });
                isOk = true;
                return;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}]",e.Message);
                Logger.Error("InjectToTargetProcess",strError, e);
                isOk = false;
            }

        }

        protected object InitInjector(ref bool isOk, ref string strError)
        {
            Logger.logBegin("InitInjector");
            ManagedInjector_Injector = null;
            string strFilePath = typeof(MarsGuiInjectorAgent).Assembly.Location;
            strFilePath = System.IO.Path.GetDirectoryName(strFilePath);
            strFilePath = System.IO.Path.Combine(strFilePath, cnst_spyFileName+".dll");
            if (!System.IO.File.Exists(strFilePath))
            {
                strError = string.Format("No such file exists:[{0}]",strFilePath);
                isOk = false;
                return null;
            }

            Assembly targetAssembly = AssemblyIsLoaded(cnst_spyFileName+".dll");
            if (targetAssembly==null)
            {
                Assembly.LoadFile(string.Format(strFilePath));
            }
            targetAssembly = AssemblyIsLoaded(cnst_spyFileName + ".dll");
            if (targetAssembly == null)
            {
                Logger.Error("InitInjector", strError = string.Format("Can't load file [{0}] to domain", strFilePath));
                 isOk = false;
                return null;
            }
            ManagedInjector_Injector = targetAssembly.GetType(cnst_spyInjectorType);
            if (ManagedInjector_Injector == null)
            {
                Logger.Error("InitInjector", strError = string.Format("Can't get type [{0}] from assembley [{1}]", cnst_spyInjectorType, strFilePath));
                 isOk = false;
                ManagedInjector_Injector = null;
                return null;
            }

            try
            {
                ConstructorInfo constructorForInjector = ManagedInjector_Injector.GetConstructor(new Type[]{ });
                if (constructorForInjector==null)
                {
                    throw new Exception(string.Format("Can't get constructor withou parameter from type [{0}] in assembley [{1}]", cnst_spyInjectorType, strFilePath));
                   
                }
                object oTarget = constructorForInjector.Invoke(null);
                if (oTarget==null )
                {
                    throw new Exception(string.Format("Can't create object instance from default constructor for type [{0}]",strFilePath));
                }
                isOk = true;
                return oTarget;
            }
            catch (Exception e)
            {
                Logger.Error("InitInjector", strError = e.Message, e);
                isOk = false;
                return null;
            }

        }


        public static bool RunOneStepByAgent(long targetProcessId,
            string strProcessId,
            string strKeyword, string strPara, string strPeg, string strObj,
            string strObjSwfType,
            string strData,
            ref string strResult, ref string strError)
        {
            MarsGuiInjectorAgent objTmpInjector = new MarsGuiInjectorAgent();
            bool isOk = false;
            if (!objTmpInjector.IsInjected((int)targetProcessId, strProcessId, ref strError, ref isOk))
            {
                objTmpInjector.InjectToTargetProcess((int)targetProcessId, ref strError, ref isOk);
                if (!isOk)
                {
                    return false;
                }
            }
            if (!MessageQueue.Exists(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME)) { 
                //MessageQueue.Delete(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
                var mq = MessageQueue.Create(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME);
                mq.SetPermissions("Everyone", MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
            }
            InjectorMessageAgent.cleanQueuebyName(MarsMessageConst.MESSAGE_CLIENT_QUEUE_NAME,ref strError);

            Dictionary<string, string> pegDic = new Dictionary<string, string>(),
                        objDic = new Dictionary<string, string>();
            isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(strPeg,
                strObj, ref pegDic, ref objDic, ref strError);
            MARSDealResult objResult = new MARSDealResult();            

            isOk = InjectorMessageAgent.DealWithKeyword_GUIOp(strKeyword.ToUpper(), 0, pegDic,
                objDic, strPara, strData, strObjSwfType, "", strPeg,strObj,ref strError,
                ref objResult);
            strError = objResult.ErrorMessage;
            return isOk;
        }


        private static Assembly AssemblyIsLoaded(string pathToAssembly)
        {
            foreach (Assembly currentAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try // This try/catch block is necessary because the .Location property is not supported in a dynamic assembly.
                {
                    if (currentAssembly.Location.ToUpper().EndsWith(pathToAssembly.ToUpper()))
                    //if (currentAssembly.Location.Equals(pathToAssembly,StringComparison.OrdinalIgnoreCase))
                    {
                        return currentAssembly;
                    }
                }
                catch (Exception ex) { }
            }

            return null;
        }
    }
}
