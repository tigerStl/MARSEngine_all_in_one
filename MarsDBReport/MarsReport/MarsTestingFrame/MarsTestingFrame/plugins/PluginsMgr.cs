extern alias clientWCF;
using Mars.plugins.standards;
using MarsTestFrame.SourceCode.systemUtil;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MarsTestFrame.plugins
{


    public class PluginsMgr
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(PluginsMgr));
        ConfigTesMarsPluginsCollection ListCurrentPluginsInfo = null;
        Dictionary<string, MarsPluginsInterFaceInfo> PluginsDictionary = new Dictionary<string, MarsPluginsInterFaceInfo>();
        Dictionary<EMars_PluginSensitive, List<MarsPluginsInterFaceInfo>> PluginsDictionaryForEvent = new Dictionary<EMars_PluginSensitive, List<MarsPluginsInterFaceInfo>>();
        internal void LoadPluginsConfig()
        {
            Logger.logBegin("LoadPluginsConfig");
            ListCurrentPluginsInfo = AppConfigReader.GetPMarslugins();
            if (ListCurrentPluginsInfo == null) return;
            foreach(ConfiguablePlugins objPlugin in ListCurrentPluginsInfo)
            {
                MarsPluginsInterFaceInfo objTmpInterface = new MarsPluginsInterFaceInfo(objPlugin);
                if (!objTmpInterface.IsPluginAvailable) continue;
                if (!PluginsDictionary.ContainsKey(objTmpInterface.AssignedNodeInfo.PluginName))
                {
                    PluginsDictionary.Add(objTmpInterface.AssignedNodeInfo.PluginName, objTmpInterface);
                }
                else
                {
                    // no chain of plugins supports currently
                    PluginsDictionary[objTmpInterface.AssignedNodeInfo.PluginName] = objTmpInterface;
                }
            }

            convert2EventMode();
            Logger.Info("LoadPluginsConfig", string.Format("Loaded plugins count:[{0}]", PluginsDictionary.Count));
        }
        protected void convert2EventMode()
        {
            Logger.logBegin("convert2EventMode");
            PluginsDictionaryForEvent.Clear();
            var itms = Enum.GetValues(typeof(EMars_PluginSensitive));
            foreach (string strKey in PluginsDictionary.Keys)
            {
                MarsPluginsInterFaceInfo objPlugins = PluginsDictionary[strKey];
                if (objPlugins == null) continue;
                if (objPlugins.currentPluginsInstance == null) continue;
                
                foreach(EMars_PluginSensitive itm in itms)
                {
                    if (((int)itm)==0) continue;
                    if (((((int)(objPlugins.currentPluginsInstance.isSensitiveFor()))&((int)itm)))!=((int)itm))
                    {
                        continue;
                    }
                    List<MarsPluginsInterFaceInfo> lstInterface;
                    if (!PluginsDictionaryForEvent.ContainsKey(itm))
                    {
                        lstInterface = new List<MarsPluginsInterFaceInfo>();
                        PluginsDictionaryForEvent.Add(itm, lstInterface);
                    }
                    else
                    {
                        lstInterface = PluginsDictionaryForEvent[itm];
                    }
                    lstInterface.Add(objPlugins);
                }
            }
        }

        internal bool DealWithEvent(EMars_PluginSensitive e_SensitiveEvent, string keyword, string objectName, string row_Column, string strDataSrc, ref string strError, ref string strDataTarget)
        {
            Logger.Info("DealWithEvent",string.Format("Begin, Event:[{0}], keyword:[{1}] objectName:[{2}], row_Column:[{3}], strDataSrc:[{4}]", e_SensitiveEvent.GetString(),keyword, objectName, row_Column, strDataSrc));
            List<MarsPluginsInterFaceInfo> lstPlugins = null;
            if (!PluginsDictionaryForEvent.ContainsKey(e_SensitiveEvent))
            {
                if (PluginsDictionaryForEvent.Keys.Count>0)
                    Logger.Info("DealWithEvent",string.Format("No such Plugin Event exit :[{0}]",e_SensitiveEvent.GetString()));
                return true;
            }
            
            lstPlugins = PluginsDictionaryForEvent[e_SensitiveEvent];
            if (lstPlugins == null)
                return true;
            if (lstPlugins.Count < 0) return true;
            string strCurrentConfigPluginsName = "";
            try
            {
                foreach(var itm in lstPlugins)
                {
                    strCurrentConfigPluginsName = itm.AssignedNodeInfo.PluginName;
                    if (itm.currentPluginsInstance==null)
                    {
                        Logger.Warnning("DealWithEvent",string.Format("No instance of the Plugins:[{0}]",itm.AssignedNodeInfo.PluginName));
                        continue;
                    }                    
                    if (!itm.currentPluginsInstance.GetSensitiveData(e_SensitiveEvent,keyword, objectName,row_Column,strDataSrc,ref strDataTarget, ref strError))
                    {
                        Logger.Error("DealWithEvent", strError = string.Format("Error when call GetSensitiveData :[{0}]", strError));
                        return false;
                    }
                    strDataSrc = strDataTarget;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DealWithEvent",strError = string.Format("When run plugins:[{2}],Exceptions:[{0}] stackTrace:[{1}]",e.Message,e.StackTrace, strCurrentConfigPluginsName));
                return false;
            }
        }
    }


    public class MarsPluginsInterFaceInfo
    {
        protected static MLogger Logger = MLogger.GetLogger(typeof(MarsPluginsInterFaceInfo));
        
        internal ConfiguablePlugins AssignedNodeInfo;
        internal bool IsPluginAvailable = false ;

        public AbstractClass_MarsPluginsStandard currentPluginsInstance=null ;
        public string CurrentError = "";
        internal MarsPluginsInterFaceInfo(ConfiguablePlugins objToAssign )
        {
            AssignedNodeInfo = objToAssign;
            if (objToAssign==null)
            {
                Logger.Error("MarsPluginsInterFaceInfo", CurrentError="Assigned Plugins Section Information is NUll");
                IsPluginAvailable = false;
                return;
            }
            LoadAssembly();
        }

        protected void LoadAssembly()
        {
            if (!System.IO.File.Exists(AssignedNodeInfo.PluginPath))
            {
                IsPluginAvailable = false;
                Logger.Error("MarsPluginsInterFaceInfo", CurrentError = string.Format("No such file(assembly) [{0 }] exists", AssignedNodeInfo.PluginPath));
                return;
            }

            try
            {
                var objDll = Assembly.LoadFrom(AssignedNodeInfo.PluginPath);
                foreach(var objT in objDll.GetExportedTypes())
                {
                    Logger.Info("LoadAssembly", string.Format("Get a type :[{0}]", objT.FullName));
                    if (!objT.IsSubclassOf(typeof(AbstractClass_MarsPluginsStandard)))                    
                    {
                        continue;
                    }
                    Logger.Info("LoadAssembly", string.Format("Get a type which is AbstractClass_MarsPluginsStandard :[{0}]", objT.FullName));
                    currentPluginsInstance =(AbstractClass_MarsPluginsStandard)Activator.CreateInstance(objT);
                    IsPluginAvailable = true;
                    return ;
                }
                IsPluginAvailable = false;
                Logger.Error("LoadAssembly", CurrentError=string.Format("No such [{0}] class or its Descent class exist", "AbstractClass_MarsPluginsStandard"));
            }
            catch (Exception e)
            {
                Logger.Error("LoadAssembly",CurrentError = string.Format("Exception:[{0}] , stackTrace:[{1}]",e.Message, e.StackTrace));
                IsPluginAvailable = false;
            }
        }
    }

    
}
