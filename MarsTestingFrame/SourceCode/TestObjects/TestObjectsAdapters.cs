using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using Route2NSEx.src.Marquis.systemUtil;
using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.SourceCode.com.Mars.Excels;
using com.Mars.Constants;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.com.Mars.TestConfigObjects.Adatpers;
using com.Mars.TestFrame.Application;
using MarsTestFrame.SourceCode.com.Mars.DB;

namespace MarsTestFrame.SourceCode.TestObjects
{
   
    public interface TestObjectsAdapters
    {
        bool LoadTestObjects(string strDBIdx,string strAppShortName);
        string GetObjectsKeyName(); /** key name is xls file name or Application ID when database mode is applied **/
        List<ConfigObjectBase> GetObjectListByPegName(string strPegName);
        Hashtable GetAllChildrenObjects();
    }



    public class TestObjectAdapterFactory
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestObjectAdapterFactory));


        public static TestObjectsAdapters GetTestObjectAdapter(TestObjectSource testObjectSource)
        {
            Logger.Info("GetTestObjectAdapter",string.Format("testObjectSource:[{0}]", testObjectSource));
            switch (testObjectSource)
            {
                case TestObjectSource.TOS_From_XlsFile: return new TestObjectAdp_Xls();
                case TestObjectSource.TOS_From_Database:return new TestObjectAdp_DB();
                case TestObjectSource.TOS_Not_Init: return null;
                default: return null;
            }            
        }
    }



    public class TestObjectAdp_Xls:MarsExcelFileBase, TestObjectsAdapters
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestObjectAdp_Xls));

        #region properties
        public string ApplicationShortName;
        #endregion

        #region members
        private Hashtable mobjCurrentLoadedObjects = new Hashtable();
        #endregion
        public bool LoadTestObjects(string strDBIdx,string strAppShortName)
        {
            /** get Xls object File Name **/
            TargetApplicationInfo objApp = TargetApplicationsManagement.GetApplicationByShortName(strAppShortName) ;
            if (objApp==null)
            {
                throw new MarsExceptions((int)ERROR_CODE._COMPILER_NO_SUCH_APPLICATION_CONFIGED,ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_SUCH_APPLICATION_CONFIGED)) ;
            }
            string strObjectFileNameWithPath = objApp.ObjectFilePath;
            if (!System.IO.File.Exists(strObjectFileNameWithPath))
            {
                throw new MarsExceptions((int)ERROR_CODE._COMPILER_NO_SUCH_OBJECT_FILE, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_NO_SUCH_OBJECT_FILE), strObjectFileNameWithPath));
            }
            this.XlsFileNameWithPath = strObjectFileNameWithPath;
            ApplicationShortName = strAppShortName;
            ERROR_CODE iError = loadTestCase();
            return iError==ERROR_CODE._NO_ERROR;
        }

        public List<ConfigObjectBase> GetObjectListByPegName(string strPegName)
        {
            return (List<ConfigObjectBase>) this.mobjCurrentLoadedObjects[strPegName];
        }

        public Hashtable GetAllChildrenObjects()
        {
            return this.mobjCurrentLoadedObjects;
        }

        protected override ERROR_CODE mAlystTestCase()
        {
            Logger.logBegin("mAlystExcleFile");
            ERROR_CODE iErrorCde = this.GetDataTableFromExcelFile();
            if (iErrorCde != ERROR_CODE._NO_ERROR)
            {
                throw new MarsExceptions((int)ERROR_CODE._COMPILER_CANT_LOAD_OBJECTFILE, string.Format(ERROR_INFO.GET_ERROR_STR(ERROR_CODE._COMPILER_CANT_LOAD_OBJECTFILE), ERROR_INFO.GET_ERROR_STR(iErrorCde),this.mstrXlsFileName));
            }
            /*** Load all Data into memory? ***/
            foreach (string strTableName in mlstCurrentTables)
            {
                List<ConfigObjectBase> lstOnePegWindowChildren = new List<ConfigObjectBase>() ;
                Logger.Info("LoadTable",string.Format("Current Table Name: [{0}] Begin....", strTableName));
                iErrorCde = this.GetSpecialTableDataToList(strTableName, lstOnePegWindowChildren);
                if (iErrorCde == ERROR_CODE._NO_ERROR)
                {
                    /** store it to hash**/
                    this.mobjCurrentLoadedObjects.Add(strTableName.Replace("_OD$",""),lstOnePegWindowChildren) ;
                    CompactPegwindowObjects(lstOnePegWindowChildren);
                }
                Logger.Info("LoadTable", string.Format("Current Table Name: [{0}] End....", strTableName));
            }
            return ERROR_CODE._NO_ERROR;
        }
        
        private void CompactPegwindowObjects(List<ConfigObjectBase> lstAllObjects)
        {
            Logger.logBegin("CompactPegwindowObjects");
            foreach(ConfigObjectBase obj in lstAllObjects)
            {
                if (obj == null) continue;
                if (!(obj is TestObject)) continue;
                TestObject objTest = (TestObject)obj;
                if (objTest.IsPegwindowObject())
                {
                    ((TestPegwindowObject)objTest).ChildrenObjects = lstAllObjects;
                }
            }
            Logger.logEnd("CompactPegwindowObjects");
        }

        protected override ConfigObjectBase mLoadDataRow2ConfigObj(System.Data.DataRow objOneRow,int iRunId=-1, long lAppId = -1)
        {
            Logger.logBegin("mLoadDataRow2ConfigObj");
            if (objOneRow == null) return null;
            TestSuiteAdapter objAdapter = TestSuiteAdapterFactory.GetAdapterInstance(MARS_ADAPTER._ADPTR_OBJECTS_LOAD_FROM_XLS);
            return objAdapter.LoadTestSuiteInfo(objOneRow);
        }

        public string GetObjectsKeyName()
        {
            return this.ApplicationShortName;
        }
    }
}
