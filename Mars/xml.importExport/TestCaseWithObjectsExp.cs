using Mars.Business;
using Mars.Dto;
using Mars.xml.importExport.xmlnodes;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars.xml.importExport
{
    internal class TestCaseWithObjectsImp
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestCaseWithObjectsImp));
        private long ExtraTestcaseIdFromFileName(string strFileName,ref bool isOk)
        {
            Logger.logBegin("ExtraTestcaseIdFromFileName",string.Format("FileName:[{0}]",strFileName));
            int iDsh = -1, iXml = -1;
            if (((iDsh=strFileName.LastIndexOf("_"))>0)&&((iXml=strFileName.ToUpper().LastIndexOf(".XML"))> 0))
            {
                string strId = strFileName.Substring(iDsh+1,(iXml-iDsh-1));
                long tcid;
                if (!long.TryParse(strId, out tcid))
                {
                    isOk = false;
                    return -1;
                }
                isOk = true;
                return tcid;
            }else
            {
                isOk = false;
                return -1;
            }
        }
        internal TestCaseExportXmlNodes LoadXmlToNodes(string strFileWithPath, ref string strError, ref bool isOkLoaded)
        {
            Logger.Info("LoadXmlToNodes",string.Format("Try to load :[{0}]", strFileWithPath));
            try
            {
                XmlSerializer xmlSrlzr = new XmlSerializer(typeof(TestCaseExportXmlNodes));
                TestCaseExportXmlNodes objRslt = null;
                using (System.IO.FileStream xmRdr = new FileStream(strFileWithPath,FileMode.Open, FileAccess.Read))
                {
                    objRslt = (TestCaseExportXmlNodes)xmlSrlzr.Deserialize(xmRdr);
                }
                if (objRslt.TestCaseNodeInfo != null)
                {
                    
                    long tcId = ExtraTestcaseIdFromFileName(strFileWithPath,ref isOkLoaded);
                    if (isOkLoaded)
                        objRslt.TestCaseNodeInfo.TestCaseIdFromImportFileName = tcId;// ExtraTestcaseIdFromFileName(strFileWithPath);
                    else
                    {
                        strError = "no test case id, filename format is wrong.";
                        isOkLoaded = false;
                        return null;
                    }
                }
                objRslt.CompoundTestStepWithDataForYuLang();

                isOkLoaded = true;
                return objRslt;
            }
            catch (Exception e)
            {
                Logger.Error("LoadXmlToNodes",strError=string.Format("Exception:[{0}], stackTrace:[{1}]",e.Message, e.StackTrace),e);
                isOkLoaded = false; 
                return null;
            }
        }
    }

    internal class TestCaseWithObjectsExp
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestCaseWithObjectsExp));

        public TestCaseWithObjectsExp()
        {

        }

        public string TargetDirectory { get; set; }
        public string TargetFileName { get; set; }

        
        public bool ExportTestCaseWithObjectByTestCaseId(long lTestCaseId, ref string strError,string strTestCaseName="")
        {
            Logger.Info("ExportTestCaseWithObjectByTestCaseId",string.Format("Try to exprot testcase:[{0}]", lTestCaseId));

            this.TargetFileName = string.Format("MarsTestCaseExport_{1}_{0}.xml", lTestCaseId, strTestCaseName);

            TestCaseExportXmlNodes objExpNodes = new TestCaseExportXmlNodes();
            List<T_REGISTERED_APPSDTO> lstTargetApps = new List<T_REGISTERED_APPSDTO>();
            if (!getAssignedApps(lTestCaseId, ref lstTargetApps, ref strError))
                return false;
            objExpNodes.MarsApps = MarImpExp_Node_AppItem.ConvertFrom(lstTargetApps);
            T_TEST_CASE_SUMMARYDTO testcaseInfo = new T_TEST_CASE_SUMMARYDTO();
            if (!getAssignedTestCaseInfo(lTestCaseId, ref testcaseInfo, ref strError))
                return false;
            objExpNodes.TestCaseNodeInfo = MarsImpExp_Node_TestCaseInfo.ConvertFromDTO(testcaseInfo);
            try
            {
                B_V_TEST_STEPS_FULLVISIONDTO boTestStp = new B_V_TEST_STEPS_FULLVISIONDTO();
                List<V_TEST_STEPS_FULLVISIONDTO> lstTestStep = new List<V_TEST_STEPS_FULLVISIONDTO>();
                List<long> lstAppIds = lstTargetApps.Select(p => p.APPLICATION_ID).Distinct().ToList();

                if (!boTestStp.GetTestStepsByTestId(MarsMainWindow.CurrentDatabaseIdx, lTestCaseId, ref lstTestStep, ref strError, lstAppIds)) 
                    return false;
                /// convert to xml for objects
                /// 
                lstTestStep = lstTestStep.OrderBy(P => P.RUN_ORDER).ThenBy(P => P.APPLICATION_ID).ToList();

                /// Get Data set and Datas
                /// 
                bool isOk = false;
                Dictionary<T_TEST_DATA_SUMMARYDTO, List<TEST_DATA_SETTINGDTO>> lstDataInfo = FetchDataForTestCaseForExp(lstTestStep.Select(p=>p.STEPS_ID).Distinct(),ref strError,ref isOk);
                if (!isOk)
                {
                    return false ;
                }
                objExpNodes.DataSetWithSettingDataRecords = ConvertDataDicListToXmlModel(lstDataInfo,ref strError,ref isOk);


                Dictionary<V_TEST_STEPS_FULLVISIONDTO, List<V_TEST_STEPS_FULLVISIONDTO>> dicObjectInfo = NormalizeTestInfomationToParentAndChinldren(lstTestStep);
                List<MarsImpExp_Node_ParentObject> lstPegObj = new List<MarsImpExp_Node_ParentObject>();
                objExpNodes.ParentObjects = new List<MarsImpExp_Node_ParentObject>();
                foreach (var itm in dicObjectInfo.Keys)
                {
                    if (itm == null) continue;
                    MarsImpExp_Node_ParentObject objPegXml = new MarsImpExp_Node_ParentObject();
                    objExpNodes.ParentObjects.Add(objPegXml);

                    objPegXml.ApplicationId = itm.APPLICATION_ID??-1;
                    objPegXml.ObjectId = itm.OBJECT_ID;
                    objPegXml.ObjectName = itm.OBJECT_HAPPY_NAME;
                    objPegXml.ObjectType = itm.OBJECT_TYPE;
                    objPegXml.ObjectTestType = itm.TYPE_NAME;
                    //objPegXml.ObjectEnum = itm.ENUM_TYPE;
                    objPegXml.QuickAcess = itm.QUICK_ACCESS;
                    
                    /// create children objects
                    /// 
                    objPegXml.ChildObjects = new List<MarsImpExp_Node_Object>();
                    if (dicObjectInfo[itm] != null)
                    {
                        foreach (var subObj in dicObjectInfo[itm])
                        {
                            MarsImpExp_Node_Object objSubObj = new MarsImpExp_Node_Object();
                            objSubObj.ApplicationId = subObj.APPLICATION_ID??-1;
                            objSubObj.ObjectEnum = subObj.ENUM_TYPE;
                            objSubObj.ObjectId = subObj.OBJECT_ID;
                            objSubObj.ObjectName = subObj.OBJECT_HAPPY_NAME;
                            objSubObj.ObjectTestType = subObj.TYPE_NAME;
                            objSubObj.ObjectType = subObj.OBJECT_TYPE;
                            objSubObj.QuickAcess = subObj.QUICK_ACCESS;

                            objPegXml.ChildObjects.Add(objSubObj);
                        }
                    }
                }

                objExpNodes.TestSteps = ConvertAndBuildStepsFromList(lstTestStep);              

                if (!WriteToXmlFile(objExpNodes, ref strError))
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {

                Logger.Error("ExportTestCaseWithObjectByTestCaseId",strError=string.Format("Exception:[{0}]", e.Message),e);
                return false;
            }
            
        }

        private ObservableCollection<MarsImpExp_Node_TestData> ConvertDataDicListToXmlModel(Dictionary<T_TEST_DATA_SUMMARYDTO, List<TEST_DATA_SETTINGDTO>> lstDataInfo, ref string strError, ref bool isOk)
        {
            if (lstDataInfo==null)
            {
                isOk = true;
                return null;
            }
            try
            {
                ObservableCollection<MarsImpExp_Node_TestData> lstData = new ObservableCollection<MarsImpExp_Node_TestData>();
                foreach (var itm in lstDataInfo.Keys)
                {
                    if (itm == null) continue;
                    MarsImpExp_Node_TestData objDataSet = new MarsImpExp_Node_TestData();
                    objDataSet.AssignedDataSummary = itm;
                    lstData.Add(objDataSet);

                    objDataSet.StepData = ConvertFromTestDataSettings(lstDataInfo[itm]);
                }
                return lstData;
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("ConvertDataDicListToXmlModel",strError = string.Format("Exception:[{0}]",e.Message),e);
                return null;
            }
        }

        private List<MarsImpExp_Node_Step_Data> ConvertFromTestDataSettings(List<TEST_DATA_SETTINGDTO> lstDataSettings)
        {
            if (lstDataSettings == null) return null;
            List<MarsImpExp_Node_Step_Data> lstRslt = new List<MarsImpExp_Node_Step_Data>();
            foreach (var itm in lstDataSettings )
            {
                if (itm == null) continue;
                MarsImpExp_Node_Step_Data objStepData = new MarsImpExp_Node_Step_Data();
                objStepData.AssignedStep = itm;
                lstRslt.Add(objStepData);
            }
            return lstRslt;
        }

        private Dictionary<T_TEST_DATA_SUMMARYDTO, List<TEST_DATA_SETTINGDTO>> FetchDataForTestCaseForExp(IEnumerable<long> lstTestStepIds, ref string strError, ref bool isOk)
        {
            B_TEST_DATA_SETTING boData = new B_TEST_DATA_SETTING();
            Dictionary<T_TEST_DATA_SUMMARYDTO, List<TEST_DATA_SETTINGDTO>> dicRslt = boData.FetchDataForTestCase(MarsMainWindow.CurrentDatabaseIdx, 
                lstTestStepIds, ref strError, ref isOk);
            if (!isOk)
            {
                Logger.Error("FetchDataForTestCaseForExp",strError);
                return null;
            }
            return dicRslt;
        }

        private ObservableCollection<MarsImpExp_Node_Test> ConvertAndBuildStepsFromList(List<V_TEST_STEPS_FULLVISIONDTO> lstSteps)
        {
            if (lstSteps == null) return null;
            ObservableCollection<MarsImpExp_Node_Test> lstResult = new ObservableCollection<MarsImpExp_Node_Test>();
            foreach(var itm in lstSteps)
            {
                if (lstResult.Where(p => p.RunOrder == itm.RUN_ORDER).FirstOrDefault() == null)
                {
                    MarsImpExp_Node_Test objTestStp = new MarsImpExp_Node_Test();
                    objTestStp.Keyword = itm.KEY_WORD_NAME;
                    objTestStp.ObjectId = itm.OBJECT_ID;
                    objTestStp.ObjectName = itm.OBJECT_HAPPY_NAME;
                    objTestStp.RunOrder = itm.RUN_ORDER;
                    objTestStp.StepsParamenter = itm.COLUMN_ROW_SETTING;
                    objTestStp.TestStepId = itm.STEPS_ID;

                    lstResult.Add(objTestStp);
                }
            }

            return lstResult;
        }

        /// <summary>
        /// 结果：
        /// key pegwindow的信息。以short name 和 application为准
        /// </summary>
        /// <param name="lstTestStep"></param>
        /// <returns></returns>
        private Dictionary<V_TEST_STEPS_FULLVISIONDTO, List<V_TEST_STEPS_FULLVISIONDTO>> NormalizeTestInfomationToParentAndChinldren(List<V_TEST_STEPS_FULLVISIONDTO> lstTestStep)
        {
            Logger.Info("NormalizeTestInfomationToParentAndChinldren",string.Format("Try to get Orgnize Object information, objects count:[{0}]", lstTestStep==null?0:lstTestStep.Count));
            Dictionary<V_TEST_STEPS_FULLVISIONDTO, List<V_TEST_STEPS_FULLVISIONDTO>> dicResult = new Dictionary<V_TEST_STEPS_FULLVISIONDTO, List<V_TEST_STEPS_FULLVISIONDTO>>();
            List<V_TEST_STEPS_FULLVISIONDTO> currentObjListForPeg = null;
            foreach(var itm in lstTestStep)
            {
                if (itm == null) continue;
                
                if (string.IsNullOrEmpty(itm.OBJECT_HAPPY_NAME)) continue;
                if (string.IsNullOrEmpty(itm.OBJECT_TYPE)) continue;
                if (string.Compare(itm.OBJECT_HAPPY_NAME, itm.OBJECT_TYPE, true) == 0)
                {
                    /// pegwindow
                    /// 
                    var keyObj = dicResult.Keys.Where(p => p.APPLICATION_ID == itm.APPLICATION_ID && (string.Compare(p.OBJECT_HAPPY_NAME, itm.OBJECT_HAPPY_NAME, true) == 0)).FirstOrDefault();
                    if (keyObj==null)
                    {
                        dicResult.Add(itm, currentObjListForPeg=new List<V_TEST_STEPS_FULLVISIONDTO>());
                    }
                    
                }
                else
                {
                    var keyObj = dicResult.Keys.Where(p => p.APPLICATION_ID == itm.APPLICATION_ID && p.OBJECT_HAPPY_NAME == itm.OBJECT_TYPE).FirstOrDefault();
                    if (keyObj == null) continue;
                    currentObjListForPeg = dicResult[keyObj];
                    if (currentObjListForPeg == null) continue;
                    if (currentObjListForPeg.Where(p => p.OBJECT_ID == itm.OBJECT_ID).FirstOrDefault() != null)
                        continue;
                    currentObjListForPeg.Add(itm);
                }
            }
            return dicResult;
        }

        private bool WriteToXmlFile(TestCaseExportXmlNodes objInst, ref string strError)
        {
            Logger.Info("WriteToXmlFile",string.Format("Write Xml info to File:[{0}] of directory:[{1}]", TargetFileName,this.TargetDirectory));
            if (objInst==null)
            {
                Logger.Error("WriteToXmlFile",strError = "Target object is Null");
                return false;
            }

            XmlSerializer xmlSubmit = new XmlSerializer(typeof(TestCaseExportXmlNodes));
            try
            {

                using (var xmlWriter = new System.IO.StreamWriter(Path.Combine(this.TargetDirectory, this.TargetFileName)))
                {
                    xmlSubmit.Serialize(xmlWriter, objInst);
                    xmlWriter.Flush();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("WriteToXmlFile",strError = string.Format("Exception when write Export data to Xml File:[{2}].\r\n{0} \r\n stackTrace:[{1}]",e.Message,e.StackTrace,this.TargetFileName),e);
                return false;
            }
        }

        private bool getAssignedTestCaseInfo(long lTestId, ref T_TEST_CASE_SUMMARYDTO objTestCase, ref string strError)
        {
            try
            {
                B_TEST_CASE boTestCase = new B_TEST_CASE();
                objTestCase = boTestCase.GetTestCaseById(MarsMainWindow.CurrentDatabaseIdx, lTestId);
                if (objTestCase==null)
                {
                    Logger.Error("getAssignedTestCaseInfo",strError=string.Format("No such test case id exist:[{0}]",lTestId));
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("getAssignedTestCaseInfo", string.Format("Exception:[{0}] when get test case [{1}]", e.Message, lTestId), e);
                return false;
            }
        }

        private  bool getAssignedApps(long lTestId, ref List<T_REGISTERED_APPSDTO> lstTargetApps,ref string strError)
        {
            try
            {
                B_REGISTERED_APPS regApps = new B_REGISTERED_APPS();
                lstTargetApps = regApps.getApplicationId(MarsMainWindow.CurrentDatabaseIdx, lTestId);

                return true;

            }
            catch (Exception e)
            {
                Logger.Error("getAssignedApps",strError = string.Format("Exceptions when get applications from DB by Id.\r\n\r\n[{0}]\r\n[{1}]",e.Message,e.StackTrace),e);
                return false; 
            }
            
        }
    }
}
