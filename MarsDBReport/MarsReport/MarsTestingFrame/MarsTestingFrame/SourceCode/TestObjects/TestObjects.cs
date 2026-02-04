extern alias clientWCF;
using com.Mars.Constants;
using com.Mars.TestFrame.Application;

using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.SourceCode.TestObjects;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using MarsTestFrame.com.Mars.TestConfigObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MarsTestFrame.systemUtil;

namespace com.Mars.TestFrame.TestObjects
{
    public enum TestObjectType
    {
        OT_NormalObject = 0x00,
        OT_PegWindow,

    }

    
    public sealed class TestObjectsManagement
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestObjectsManagement));
        private static TestObjectsManagement gInstance = null;

        private TestObjectSource meCurrentFrom = TestObjectSource.TOS_Not_Init; //default
        private Hashtable mhstObjects = new Hashtable();
        private TestObjectsManagement()
        {

        }

        public static TestObjectsManagement GetInstance()
        {
            Logger.logBegin("GetInstance");
            return gInstance ?? (gInstance = new TestObjectsManagement());
        }

        public static ConfigObjectBase GetObjectInfomationByPegwindow(string strAppShortName, string strPegWindowName, string strObjectName)
        {
            Logger.logBegin("GetObjectInfomationByPegwindow" + TigerMarsUtil.GetParameter("strAppShortName", strAppShortName)
                + TigerMarsUtil.GetParameter("strPegWindowName", strPegWindowName) + TigerMarsUtil.GetParameter("strObjectName", strObjectName));
            /** get the pegwindow object list **/
            List<TestPegwindowObject> lstObjects = GetPegwindowsByValues(strPegWindowName, strAppShortName);
            if (lstObjects==null)
            {
                Logger.Error("GetObjectInfomationByPegwindow", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO), strPegWindowName, strAppShortName));
                return null;
            }
            if (lstObjects.Count==0)
            {
                Logger.Error("GetObjectInfomationByPegwindow", string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_SUCH_PEGWINDOW_INFO), strPegWindowName, strAppShortName));
                return null;
            }
            foreach(TestPegwindowObject objPeg in lstObjects)
            {
                TestObject objResult = objPeg.GetChildrenObjctsByName(strObjectName);

                if (objResult != null)
                {
                    Logger.Info("GetObjectInfomationByPegwindow", "find object by HappyName!" + TigerMarsUtil.GetParameter(new string[] { "strObjectName", "IDentifier" }, new string[] { strObjectName, objResult.QuickAccessString }));
                    return objResult;
                }
            }
            Logger.Error("GetObjectInfomationByPegwindow", string.Format("no such object Happy name find:{0}",
                TigerMarsUtil.GetParameter(new string[] { "strAppShortName", "strPegWindowName", "strObjectName" }, new string[] { strAppShortName, strPegWindowName, strObjectName })));
            Logger.logEnd("GetObjectInfomationByPegwindow");
            return null;
        }

        public static List<TestPegwindowObject> GetPegwindowsByValues(string strPegWindowActionValueSrc, string strAppShortName)
        {
            Logger.Info("GetPegwindowsByValues",String.Format("PegActionValueSrc:[{0}] shortName:[{1}]", strPegWindowActionValueSrc, strAppShortName));
            TestObjectsManagement objInstance = GetInstance();
            string strPegWindowActionValue = strPegWindowActionValueSrc;
            if (string.IsNullOrEmpty(strPegWindowActionValue))
            {
                Logger.Warnning("GetPegwindowsByValues","Pegwindow is Null or empty.");
                return null;
            }
            if ((strPegWindowActionValue.Contains(' ')) || (strPegWindowActionValue.Contains('&')))
            {
#if !_Datafrom_Database
                strPegWindowActionValue = string.Format("'{0}'", strPegWindowActionValue);
#endif
            }
            try
            {
                /** Check whether strAppShortName is right **/
                if (!((TargetApplicationsManagement.GetApplicationByShortName(strAppShortName) == null ? false : true) || (TargetApplicationsManagement.GetApplicationByPath(strAppShortName) == null ? false : true)))
                {
                    throw new MarsExceptions((int)ERROR_CODE._REG_APPS_NO_SUCH_APPLICATION_SHORTNAMEORPATH, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._REG_APPS_NO_SUCH_APPLICATION_SHORTNAMEORPATH), strAppShortName));
                }
                if (objInstance.meCurrentFrom == TestObjectSource.TOS_Not_Init)
                {
                    /** Get Information from app.config File **/
                    objInstance.meCurrentFrom = AppConfigReader.GetObjectSource();
                    TestObjectsAdapters objTestObjectAdp = TestObjectAdapterFactory.GetTestObjectAdapter(objInstance.meCurrentFrom);
                    bool bLoad = objTestObjectAdp.LoadTestObjects(strAppShortName);
                    if (bLoad)
                    {
                        /** put information to Hashtable **/
                        objInstance.PutObjects2Hashtable(objTestObjectAdp.GetAllChildrenObjects(), strAppShortName);
                    }
                }
                else
                {
                    if (!objInstance.mhstObjects.ContainsKey(strAppShortName))
                    {
                        TestObjectsAdapters objTestObjectAdp = TestObjectAdapterFactory.GetTestObjectAdapter(objInstance.meCurrentFrom);
                        bool bLoad = objTestObjectAdp.LoadTestObjects(strAppShortName);
                        if (bLoad)
                        {
                            /** put information to Hashtable **/
                            objInstance.PutObjects2Hashtable(objTestObjectAdp.GetAllChildrenObjects(), strAppShortName);
                        }
                    }
                }
                /** 事实上，strPegWindowActionValue可能是几个pegwindows的组合 类似:peg1;peg2,目前，不支持该模式 **/
                if (objInstance.mhstObjects.ContainsKey(strAppShortName))
                {
                    List<TestPegwindowObject> lstResult = new List<TestPegwindowObject>();
                    if (((Hashtable)objInstance.mhstObjects[strAppShortName]).ContainsKey(strPegWindowActionValue))
                    {
                        Hashtable hstApp = (Hashtable)objInstance.mhstObjects[strAppShortName];
                        if ((((Hashtable)objInstance.mhstObjects[strAppShortName])[strPegWindowActionValue]) is List<ConfigObjectBase>)
                        {
                            List<ConfigObjectBase> objHs = ((Hashtable)objInstance.mhstObjects[strAppShortName])[strPegWindowActionValue] as List<ConfigObjectBase>;
                            foreach (ConfigObjectBase objTest in objHs)
                            {
                                if (objTest is TestPegwindowObject)
                                {
                                    /** for enhanced mode, object would be changed based on Runtime setting **/
                                    /** Therefore, a cloned new object will add to the List                 **/
                                    lstResult.Add((TestPegwindowObject)((TestPegwindowObject)objTest).Clone());
                                    return lstResult;
                                }
                            }
                            return null;
                        }else return null ;
                    }
                    else return null;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                Logger.logEnd("GetPegwindowsByValues");
            }
        }

        private ERROR_CODE PutObjects2Hashtable(Hashtable hstObjects, string strKey)
        {
            Logger.Info("PutObjects2Hashtable",string.Format("strKey:[{0}]",strKey));
            if (this.mhstObjects.ContainsKey(strKey))
            {
                this.mhstObjects.Remove(strKey);
            }
            this.mhstObjects.Add(strKey, hstObjects);
            Logger.logEnd("PutObjects2Hashtable");
            return ERROR_CODE._NO_ERROR;
        }



    }
}
