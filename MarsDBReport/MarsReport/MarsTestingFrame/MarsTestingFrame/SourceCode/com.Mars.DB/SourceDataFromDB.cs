extern alias clientWCF;
using Mars.Dto;
using Mars.Model;
using MarsTestFrame.com.Mars.TestConfigObjects;

using MarsTestFrame.SourceCode.systemUtil;
using MarsTestFrame.SourceCode.TestObjects;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.DB
{

    public class TestObjectAdp_DB : TestObjectsAdapters
    {
#if _Datafrom_Database
        private static MLogger logger = MLogger.GetLogger(typeof(TestObjectAdp_DB));
        private Hashtable mhstObjects = new Hashtable();
        private string CurrentObjectApplicationShortName;
        public Hashtable GetAllChildrenObjects()
        {
            return mhstObjects;
        }

        public List<ConfigObjectBase> GetObjectListByPegName(string strPegName)
        {
            //throw new NotImplementedException();
            return mhstObjects == null ? null : (List<ConfigObjectBase>)mhstObjects[strPegName];
        }

        public string GetObjectsKeyName()
        {
            //throw new NotImplementedException();
            return CurrentObjectApplicationShortName;
        }

        public bool LoadTestObjects(string strAppShortName)
        {
            logger.Info("LoadTestObjects", string.Format("strAppShortName:[{0}]", strAppShortName));
            try
            {
                
                
                List<V_OBJECT_APPSDTO> lstObjs= (new DBBusinessMgr()).GetAppObjectsByAppShortName(strAppShortName);
                CurrentObjectApplicationShortName = strAppShortName;
                /*** filter data to Framework usable ***/
                //Hashtable objCurrent = null;
                mhstObjects.Clear();
                //if (!mhstObjects.ContainsKey(strAppShortName))
                //{
                //    mhstObjects.Add(strAppShortName, objCurrent = new Hashtable());
                //}
                //else
                //{
                //    if (mhstObjects[strAppShortName] == null)
                //    {
                //        mhstObjects.Add(strAppShortName, objCurrent = new Hashtable());
                //    }else
                //    {
                //        objCurrent = (Hashtable)mhstObjects[strAppShortName];
                //    }
                //}
                //objCurrent.Clear();
                /** put data into hashtable **/
                logger.Info("LoadTestObjects",string.Format("return count:[{0}]", lstObjs==null?-1:lstObjs.Count));
                FillAllObjects(mhstObjects, lstObjs);
                //objTestObjectDataSet = null;
                return true;
            }
            catch (Exception e)
            {
                logger.Error("LoadTestObjects",string.Format("Exception:{0} when get Object data from Database.", e.Message),e);
                return false;
            }           
            
        }

        private void FillAllObjects(Hashtable objTarget, List<V_OBJECT_APPSDTO> lstObjs)
        {
            string strCurrentPegInfo = "";
            string strObjName = "";
            string strPegName = "";
            List<ConfigObjectBase> lstCurrentChildren = null;            
            foreach(V_OBJECT_APPSDTO objItm in lstObjs)
            {
                strCurrentPegInfo = objItm.TYPE_NAME;
                strObjName = objItm.OBJECT_HAPPY_NAME;
                strPegName = objItm.OBJECT_TYPE;
                /*** no validate pegwindow is setting ***/
                if (string.IsNullOrEmpty(strCurrentPegInfo)) continue;
                
                if (objTarget.ContainsKey(strPegName))
                {
                    lstCurrentChildren = (List<ConfigObjectBase>)objTarget[strPegName];
                }
                else
                {
                    lstCurrentChildren = new List<ConfigObjectBase>();
                    objTarget.Add(strPegName, lstCurrentChildren);
                }
                TestObject objTestObj = null;
                if (TestObject.IsPegwindowObject(strCurrentPegInfo) && string.Compare(strObjName,strPegName,true)==0)
                {
                    objTestObj = new TestPegwindowObject();
                    objTestObj.ObjectName = objItm.OBJECT_HAPPY_NAME;
                    objTestObj.QuickAccessString = objItm.QUICK_ACCESS??"";
                    objTestObj.Description = objItm.COMMENT;
                    objTestObj.ObjectType = strCurrentPegInfo;
                    objTestObj.BuildPegQuickAcessString();
                    ((TestPegwindowObject)objTestObj).ChildrenObjects = lstCurrentChildren;
                    /** make sure the first object is Peg window **/
                    lstCurrentChildren.Insert(0, objTestObj);
                }
                else
                {
                    objTestObj = new TestObject();
                    objTestObj.ObjectName = objItm.OBJECT_HAPPY_NAME;
                    objTestObj.QuickAccessString = objItm.QUICK_ACCESS;
                    objTestObj.Description = objItm.COMMENT;
                    objTestObj.ObjectType = strCurrentPegInfo;
                    objTestObj.BuildPegQuickAcessString();                    
                    lstCurrentChildren.Add(objTestObj);
                }
            }
            }
#endif
    }


}
