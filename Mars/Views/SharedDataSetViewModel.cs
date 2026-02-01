using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Mars.Utility;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Mars.ViewModel
{
    public class SharedDataSetViewModel
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(SharedDataSetViewModel));

        static string[] format = new string[] {"M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                         "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                         "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                         "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                         "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};

        internal static bool SaveAs(string dataSetName,
                                    string dataSetDescription,
                                    long testCaseId,
                                    ObservableCollection<TestStepViewModel> testSteps,
                                    ref long dataSetId)
        {
            bool result = false;

            List<B_TEST_DATA_SETTING> bTestDataSettingsList = new List<B_TEST_DATA_SETTING>();
            List<B_SHARED_OBJECT_POOL> bSharedObjectPoolList = new List<B_SHARED_OBJECT_POOL>();
            ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();

            // 1. Create T_TEST_DATA_SUMMARY
            dataSetId = BoHelper.CreateSharedTestDataSummary(MarsMainWindow.CurrentDatabaseIdx, dataSetName, dataSetDescription);
            // 2. Create REL_TC_DATA_SUMMARY
            BoHelper.CreateRelTCDataSummary(MarsMainWindow.CurrentDatabaseIdx, dataSetId, testCaseId);

            // 3. For each test step 
            foreach (TestStepViewModel testStep in testSteps)
            {
                testStep.InitDataSets();
                for (int loopId = 1; loopId <= MarsConstants.NumberOfDataSetColumns; loopId++)
                {
                    if (testStep.DataSets[loopId] != null)
                    {
                        //    3.1 Create T_SHARED_TEST_POOL
                        string objectName;
                        long objectOrder;
                        if (testStep.SelectedObjectName != null)
                            objectName = testStep.SelectedObjectName.ObjName;
                        else
                            objectName = testStep.SelectedKeyword.KeywordName;

                        objectOccuranceRepository.UpdateObjectOrder(objectName, loopId);
                        objectOrder = objectOccuranceRepository.GetObjectOrder(objectName, loopId);

                        B_SHARED_OBJECT_POOL pool = CreateSharedTestPool(testStep,
                                                                         loopId,
                                                                         dataSetId,
                                                                         objectName,
                                                                         objectOrder,
                                                                         testStep.DataSets[loopId].ToString(),
                                                                         objectOccuranceRepository);
                        bSharedObjectPoolList.Add(pool);
                        short sDirection = (short)(testStep.IsSkipForDataset ? 4 : 0);
                        //    3.2 Create TEST_DATA_SETTING 
                        B_TEST_DATA_SETTING testDataSetting = CreateTestDataSettings(testStep.DataSets[loopId].ToString(), dataSetId, loopId, pool.OBJECT_POOL_ID, testStep.StepNo, sDirection);
                        bTestDataSettingsList.Add(testDataSetting);
                    }
                }
            }

            DataTable dt = DataTableUtil.ToDataTable(bTestDataSettingsList);
            dt = DataTableUtil.ToDataTable(bSharedObjectPoolList);
            result = SaveDataAndPool(bTestDataSettingsList, bSharedObjectPoolList);

            return result;
        }

        internal class ConertFromModelToBussines
        {
            internal static B_TEST_DATA_SETTING ConvertFromSingle(TestStepViewModel objData, long testCaseId, long dataSetId, 
                MarsTransactionMgr objTrans,
                ref bool isOk,ref string strError,
                int iLoop=1 )
            {
                if (objData == null) return null;
                if (objTrans==null)
                {
                    Logger.Error("ConertFromModelToBussines.ConvertFromSingle", strError="NO db information.");
                    isOk = false;
                    return null;
                }
                
                B_TEST_DATA_SETTING objItm = new B_TEST_DATA_SETTING();
                objItm.DATA_SETTING_ID = B_TEST_DATA_SETTING.GetNewId(objTrans.CurrentDBContext);
                objItm.CREATE_TIME = DateTime.Now;
                objItm.DATA_DIRECTION = null;
                objItm.DATA_SUMMARY_ID = dataSetId;
                /// now, only the first data set is available
                objItm.DATA_VALUE = objData.DataSets[1] == null ? null : objData.DataSets[1].ToString();
                objItm.DESCRIPTION = "N/A";
                /// now, only one loop is available
                objItm.LOOP_ID = 1;
                objItm.POOL_ID = null;/// it will be changed later
                objItm.STEPS_ID = objData.StepNo;
                objItm.VALUE_OR_OBJECT = null;
                objItm.VERSION = null;

                isOk = true;
                return objItm;
            }

            internal static List<B_TEST_DATA_SETTING> ConvertFrom(List<TestStepViewModel> lstModelData, long testCaseId, long datasetId, MarsTransactionMgr objTrans,
                ref bool isOk,ref string strError,int iLoop = 1)
            {
                if (objTrans == null)
                {
                    Logger.Error("ConertFromModelToBussines.ConvertFromSingle", "NO db information.");
                    isOk = false;
                    return null;
                }
                List<B_TEST_DATA_SETTING> lstRslt = new List<B_TEST_DATA_SETTING>();
                foreach(var itm in lstModelData)
                {
                    if (itm == null) continue;
                    B_TEST_DATA_SETTING objSingle = ConvertFromSingle(itm, testCaseId, datasetId, objTrans,ref isOk, ref strError);
                    if (!isOk) return null;

                    if (objSingle == null) continue;
                    lstRslt.Add(objSingle);
                }
                isOk = true;
                return lstRslt;
            }
        }

        public static bool Save_Ex(long testCaseId, long dataSetId, ObservableCollection<TestStepViewModel> testSteps,ref string strError)
        {
            /// step1:
            /// 1, 插入新的数据到test_data_setting 表中
            /// 2, update数据变化
            /// 3, 处理Pool_id及data pool
            /// 
            try
            {
                MarsTransactionMgr objTransMars = new MarsTransactionMgr();
                List<long> lstAllStpIds = testSteps.Where(p=>(p.DataSet1!=null)&&(p.DataSet1.ToString()!="")).Select(p => p.StepNo).ToList<long>();
                using (var trans = new TransactionScope())
                {
                    //MarsTransactionMgrByConn objTrans = new MarsTransactionMgrByConn();
                    #region /// 1, 插入新的数据到test_data_setting 表中
                    List<long> lstStepIds = testSteps == null ? null : lstAllStpIds;
                    bool isOk = false;
                    B_TEST_DATA_SETTING objTestData_Setting = new B_TEST_DATA_SETTING();
                    List<long> lstNewStepIds = objTestData_Setting.getNewRecordsFrom(
                        MarsMainWindow.CurrentDatabaseIdx,
                        lstStepIds, dataSetId, ref strError, ref isOk);
                    if (!isOk)
                    {
                        Logger.Error("Save_Ex", string.Format("Error when call objTestData_Setting.getNewRecordsFrom :[{0}] for data summary id:[{1}]", strError, dataSetId));
                        return false;
                    }
                    if ((lstNewStepIds != null) && (lstNewStepIds.Count > 0))
                    {
                        List<TestStepViewModel> lstToBeInserted = testSteps.Where(p => lstNewStepIds.Contains(p.StepNo)).ToList();
                        foreach (TestStepViewModel itm in lstToBeInserted)
                        {
                            B_TEST_DATA_SETTING objItm = ConertFromModelToBussines.ConvertFromSingle(itm, testCaseId, dataSetId, objTransMars,ref isOk, ref strError);
                            if (!isOk)
                                return false;

                            isOk = objItm.Insert(MarsMainWindow.CurrentDatabaseIdx, ref strError, objTransMars);
                            if (!isOk)
                            {
                                Logger.Error("Save_Ex", string.Format("Error when insert into Test_Data_setting:[{0}]", strError));
                                return false;
                            }
                        }
                    }
                    #endregion //1, 插入新的数据到test_data_setting 表中

                    #region /// 2, update数据变化
                    //2.1 获得已经存在数据的test ids
                    //2.2 
                    List<long> lstTestStpIdExists = null;

                    if (lstNewStepIds == null || lstNewStepIds.Count == 0)
                    {
                        lstTestStpIdExists = lstAllStpIds;
                    }
                    else
                    {
                        lstTestStpIdExists = lstAllStpIds.Where(p => !lstNewStepIds.Contains(p)).ToList();
                    }
                    List<TestStepViewModel> lstToBeUpdated = testSteps.Where(p => lstTestStpIdExists.Contains(p.StepNo)).ToList();
                    List<B_TEST_DATA_SETTING> lstTobeUpdatedBTest = ConertFromModelToBussines.ConvertFrom(lstToBeUpdated, testCaseId, dataSetId, objTransMars, ref isOk, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("Save_Ex", string.Format("Error:[{0}] when call ConvertFrom", strError));
                        return false;
                    }
                    isOk = objTestData_Setting.CheckAndUpdate(lstTobeUpdatedBTest, dataSetId, objTransMars, ref strError);
                    if (!isOk)
                    {
                        Logger.Error("Save_Ex", string.Format("Error:[{0}] when call CheckAndUpdate", strError));
                        return false;
                    }

                    #endregion/// 2, update数据变化

                    #region /// 3, 处理Pool_id及data pool
                    //暂时不处理？
                    #endregion /// 3, 处理Pool_id及data pool
                    objTransMars.CurrentDBContext.SaveChanges();
                    trans.Complete();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Save_Ex", strError=string.Format("Exception:[{0}],StackTrace:[{1}]",e.Message,e.StackTrace),e);
                return false;
            }
        }

        

        public static bool Save(
            string strDBIdx, 
            long testCaseId, long dataSetId, ObservableCollection<TestStepViewModel> testSteps,MarsEntities dbCntx,ref string strError)
        {
            Logger.logBegin("Save",string.Format("tcId:[{0}] datasetId:[{1}]",testCaseId,dataSetId));
            bool result = false;

            //List<B_TEST_DATA_SETTING> bTestDataSettingsList = new List<B_TEST_DATA_SETTING>();
            //List<B_SHARED_OBJECT_POOL> bSharedObjectPoolList = new List<B_SHARED_OBJECT_POOL>();
            bool isOk = false;
            List<B_TEST_DATA_SETTING> bExistingTestDataSettingsList = BoHelper.LoadBOTestDataSettingsBySummaryId(dataSetId, testCaseId, dbCntx, ref isOk, ref strError);
            if (!isOk) return false;
            List<B_SHARED_OBJECT_POOL> bExistingSharedObjectPoolList = BoHelper.GetSharedObjectPoolInfoByDataSummaryId(strDBIdx, 
                dataSetId, dbCntx);

            /// 算法：
            /// 1，更新test_datasetting table
            /// 2，更新data summary
            /// 3，更新shared data
            /// 
            /// 1，更新test_datasetting table
            /// 
            Dictionary<string, List<TestStepViewModel>> dicTestStpByObjectName = 
                testSteps
                .GroupBy(p => p.SelectedObjectName == null ? (p.SelectedKeyword == null ? "NOOBJNAME" : p.SelectedKeyword.KeywordName) : p.SelectedObjectName.ObjName)
                .ToDictionary(p => p.Key, q => q.OrderBy(x=>x.RunOrder).ToList());
            if (dicTestStpByObjectName.ContainsKey("NOOBJNAME"))
                dicTestStpByObjectName.Remove("NOOBJNAME");
            Dictionary<string, List<T_SHARED_OBJECT_POOLDTO>> dicShareData = bExistingSharedObjectPoolList
                .GroupBy(p => string.IsNullOrEmpty(p.OBJECT_NAME) ? "NOOBJNAME" : p.OBJECT_NAME)
                .ToDictionary(p=>p.Key, x=>x.OrderBy(z=>z.OBJECT_ORDER).Cast<T_SHARED_OBJECT_POOLDTO>().ToList());                 

            B_SHARED_OBJECT_POOL oBoSharedOp = new B_SHARED_OBJECT_POOL();
            B_TEST_DATA_SETTING oDataSettingOp = new B_TEST_DATA_SETTING();
            foreach (string strObjName in dicTestStpByObjectName.Keys)
            {
                List<TestStepViewModel> lstStp = dicTestStpByObjectName[strObjName];
                if (lstStp == null) continue;
                //获得现有的shared Pool对象
                List<T_SHARED_OBJECT_POOLDTO> lstSharedBySpecialName = dicShareData.ContainsKey(strObjName)?dicShareData[strObjName]:null;
                if (lstSharedBySpecialName == null)
                    dicShareData.Add(strObjName, lstSharedBySpecialName = new List<T_SHARED_OBJECT_POOLDTO>());

                for (int i =0;i<lstStp.Count;i++)
                {
                    T_SHARED_OBJECT_POOLDTO oCurrentPoolObj = null;
                    string strCurrentDataValue = strCurrentDataValue = lstStp[i].DataSet1 == null ? "" : lstStp[i].DataSet1.ToString();
                    if (i >= lstSharedBySpecialName.Count)
                    {
                        //if (!string.IsNullOrEmpty((string)lstStp[i].DataSet1))
                        {
                            //新的shared对象， 先创建，然后再
                            long lnewSharedId = oBoSharedOp.CreateNewRecorder(dataSetId,
                                strObjName, i + 1, 1, strCurrentDataValue = lstStp[i].DataSet1 == null ? "" : lstStp[i].DataSet1.ToString(),
                                dbCntx, ref isOk, ref strError);
                            oCurrentPoolObj = new T_SHARED_OBJECT_POOLDTO()
                            {
                                OBJECT_POOL_ID = lnewSharedId,
                                DATA_SUMMARY_ID = dataSetId,
                                OBJECT_NAME = strObjName,
                                OBJECT_ORDER = i + 1,
                                LOOP_ID = 1,
                                DATA_VALUE = strCurrentDataValue
                            };
                            lstSharedBySpecialName.Add(oCurrentPoolObj);
                        }//可能 isSkip checked
                    }
                    else
                    {
                        ///获得shared pool中的对象
                        ///
                        oCurrentPoolObj = lstSharedBySpecialName[i];
                        if (oCurrentPoolObj == null) continue;//it should not happen, but for non exception handle, continue;
                        if (string.Compare(strCurrentDataValue,oCurrentPoolObj.DATA_VALUE)!=0)
                        {
                            //update shared pool
                            if (!oBoSharedOp.updateRecordwithNewData(oCurrentPoolObj, strCurrentDataValue, dbCntx, ref strError)) return false;
                        }
                    }
                    // get from exist test data
                    B_TEST_DATA_SETTING stpData = bExistingTestDataSettingsList.Where(p => p.STEPS_ID == lstStp[i].StepNo).FirstOrDefault();
                    if (stpData==null)
                    {
                        //if (string.IsNullOrEmpty((string)lstStp[i].DataSet1)) continue;
                        //有可能是isskip 变化了
                        //create a new one
                        stpData = oDataSettingOp.createDataRec(lstStp[i].StepNo, 1, (string)lstStp[i].DataSet1, 1, "",
                            dataSetId,lstStp[i].IsSkipForDataset ? 4 : 0, (int)oCurrentPoolObj.OBJECT_POOL_ID,dbCntx, ref isOk, ref strError);
                        if (!isOk)
                            return false;
                        bExistingTestDataSettingsList.Add(stpData);
                    }
                    else
                    {
                        //判断是否有数据 变化
                        short sNewDirction =(short)(lstStp[i].IsSkipForDataset ? 4 : 0);
                        if ((stpData.DATA_DIRECTION!= sNewDirction)
                            ||(string.Compare(lstStp[i].DataSet1==null?"": (string)lstStp[i].DataSet1, stpData.DATA_VALUE)!=0))
                        {
                            //if (!oDataSettingOp.UpdateValueAndDirection(stpData.DATA_SETTING_ID, (string)lstStp[i].DataSet1, 
                            if (!oDataSettingOp.UpdateValueAndDirection(stpData.DATA_SETTING_ID, (string)lstStp[i].DataSet1,
                                sNewDirction,
                                dbCntx, ref strError))
                                return false;
                        }
                    }

                }
            }
            /// delete all steps data which not touched from db
            var lstUnUsedData = bExistingTestDataSettingsList.Where(p => !testSteps.Any(x => x.StepNo == p.STEPS_ID)).ToList();
            isOk=oDataSettingOp.deleteRecords(lstUnUsedData, dbCntx, ref strError);
            if (!isOk) return false;
            return true;
            /*
                        //DataTable dt1 = DataTableUtil.ToDataTable(bExistingTestDataSettingsList);
                        //DataTable dt2 = DataTableUtil.ToDataTable(bExistingSharedObjectPoolList);

                        ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();
                        B_SHARED_OBJECT_POOL pool;
                        // 1.  For each test step 
                        //    1.1 Update T_SHARED_TEST_POOL
                        try
                        {
                            foreach (TestStepViewModel testStep in testSteps)
                            {
                                testStep.InitDataSets();
                                //for (int loopId = 1; loopId <= MarsConstants.NumberOfDataSetColumns; loopId++)
                                for (int loopId = 1; loopId <= 1; loopId++)
                                {
                                    if (
                                        (testStep.DataSets[loopId] != null)
                                        ||testStep.IsSkipValueChanged()       //未必有数据但是设置了跳过该行
                                        )
                                    {
                                        short sDirection = (short)(testStep.IsSkipForDataset ? 4 : 0);
                                        if ((testStep.DataSets[loopId] == null))
                                        {
                                            testStep.DataSets[loopId] = "";
                                        }

                                        string newData = testStep.DataSets[loopId].ToString();
                                        // Create a new data setting and pool
                                        string objectName;
                                        long objectOrder;
                                        if (testStep.SelectedObjectName != null)
                                            objectName = testStep.SelectedObjectName.ObjName;
                                        else
                                            objectName = testStep.SelectedKeyword.KeywordName;

                                        objectOccuranceRepository.UpdateObjectOrder(objectName, loopId);
                                        objectOrder = objectOccuranceRepository.GetObjectOrder(objectName, loopId);

                                        var testDataSetting = bExistingTestDataSettingsList.FirstOrDefault(a => a.LOOP_ID == loopId && a.STEPS_ID == testStep.StepNo);
                                        if (testDataSetting == null)
                                        {
                                            pool = CreateSharedTestPool(testStep,
                                                                        loopId,
                                                                        dataSetId,
                                                                        objectName,
                                                                        objectOrder,
                                                                        testStep.DataSets[loopId].ToString(),
                                                                        objectOccuranceRepository,
                                                                        objDbCntx: dbCntx);
                                            bSharedObjectPoolList.Add(pool);

                                            B_TEST_DATA_SETTING newTestDataSetting = CreateTestDataSettings(testStep.DataSets[loopId].ToString(), dataSetId, loopId, pool.OBJECT_POOL_ID, testStep.StepNo, sDirection,dbCntx);
                                            newTestDataSetting.DATA_DIRECTION = sDirection ;
                                            //Logger.Info("Save", string.Format("testDataSetting == null ---> StepId:[{0}], Data Summary Id:[{1}] Loop_id:[{2}] RunOrder:[{3}]", testStep.StepNo,dataSetId,pool.OBJECT_POOL_ID,testStep.RunOrder ));
                                            bTestDataSettingsList.Add(newTestDataSetting);
                                        }
                                        else
                                        {
                                            if (testDataSetting.POOL_ID == null)
                                            {
                                                pool = (from t in bExistingSharedObjectPoolList
                                                        where t.OBJECT_NAME == objectName &&
                                                              t.OBJECT_ORDER == objectOrder &&
                                                              t.LOOP_ID == testDataSetting.LOOP_ID
                                                        select t).FirstOrDefault();

                                                if (pool == null)
                                                {
                                                    pool = CreateSharedTestPool(testStep,
                                                                           loopId,
                                                                           dataSetId,
                                                                           objectName,
                                                                           objectOrder,
                                                                           testStep.DataSets[loopId].ToString(),
                                                                           objectOccuranceRepository,
                                                                           dbCntx);

                                                    bSharedObjectPoolList.Add(pool);
                                                }

                                                testDataSetting.POOL_ID = pool.OBJECT_POOL_ID;
                                                testDataSetting.DATA_VALUE = testStep.DataSets[loopId].ToString();
                                                testDataSetting.DATA_DIRECTION = sDirection;
                                                bTestDataSettingsList.Add(testDataSetting);
                                                //Logger.Info("Save", string.Format("testDataSetting.POOL_ID == null ---> StepId:[{0}], Data Summary Id:[{1}] Loop_id:[{2}] RunOrder:[{3}]", testStep.StepNo, dataSetId, pool.OBJECT_POOL_ID, testStep.RunOrder));
                                            }
                                            else
                                            {
                                                pool = (from p in bExistingSharedObjectPoolList
                                                        where p.OBJECT_POOL_ID == testDataSetting.POOL_ID
                                                        select p).FirstOrDefault();

                                                // This is the case where we have data but no pool -- we will create pool and link data to it
                                                if (pool == null)
                                                {
                                                    pool = CreateSharedTestPool(testStep,
                                                                                loopId,
                                                                                dataSetId,
                                                                                objectName,
                                                                                objectOrder,
                                                                                testStep.DataSets[loopId].ToString(),
                                                                                objectOccuranceRepository,
                                                                                dbCntx);
                                                    bSharedObjectPoolList.Add(pool);

                                                    testDataSetting.POOL_ID = pool.OBJECT_POOL_ID;
                                                    //added tiger
                                                    testDataSetting.DATA_VALUE = testStep.DataSets[loopId].ToString();
                                                    testDataSetting.DATA_DIRECTION = sDirection;
                                                    bTestDataSettingsList.Add(testDataSetting);
                                                    //Logger.Info("Save", string.Format("pool == null ---> StepId:[{0}], Data Summary Id:[{1}] Loop_id:[{2}] RunOrder:[{3}]", testStep.StepNo, dataSetId, pool.OBJECT_POOL_ID, testStep.RunOrder));
                                                }
                                                else if ((pool.DATA_VALUE == null) || (pool.DATA_VALUE.Equals(newData) == false))
                                                {
                                                    // Update pool record
                                                    pool.DATA_VALUE = newData;

                                                    // Update all ALL settings related to this pool record
                                                    var relatedTestDataSettings =
                                                        (from data in bExistingTestDataSettingsList
                                                         where data.POOL_ID == pool.OBJECT_POOL_ID
                                                         select data);

                                                    foreach (var data in relatedTestDataSettings)
                                                    {
                                                        data.DATA_VALUE = newData;
                                                        bTestDataSettingsList.Add(data);
                                                    }
                                                    testDataSetting.DATA_DIRECTION = sDirection;
                                                    bSharedObjectPoolList.Add(pool);
                                                    //Logger.Info("Save", string.Format("last ---> StepId:[{0}], Data Summary Id:[{1}] Loop_id:[{2}] RunOrder:[{3}]", testStep.StepNo, dataSetId, pool.OBJECT_POOL_ID, testStep.RunOrder));
                                                }
                                                else if (((testDataSetting.DATA_DIRECTION??0)!=sDirection))
                                                {
                                                    testDataSetting.DATA_DIRECTION = sDirection;
                                                    bTestDataSettingsList.Add(testDataSetting);
                                                }
                                            }
                                        }
                                    }

                                }
                            }

                            DataTable dt = DataTableUtil.ToDataTable(bTestDataSettingsList);
                            dt = DataTableUtil.ToDataTable(bSharedObjectPoolList);
                            result = SaveDataAndPool(bTestDataSettingsList, bSharedObjectPoolList,dbCntx);

                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Save",string.Format("Exception:[{0}],stackTrace:\r\n:[{1}]",ex.Message,ex.StackTrace),ex);
                            //Console.WriteLine("dddd");
                            result = false;
                        }
                        Logger.logEnd("Save");
                        return result;
                        */
        }

        public static bool Delete(string strDBIdx, List<long> deletedTestSteps,MarsEntities objDbCntx, bool commitChanges = true, long dataSetId = -1)
        {
            bool result = false;
            // 1.  For each test step 
            //    1.1 Delete TEST_DATA_SETTING row

            // 2. If no other test steps related to this TEST_DATA_SETTING
            //    2.1 Delete T_SHARED_TEST_POOL

            List<long> testStepIds;
            List<long> existingPoolIds;
            List<long> poolIds = new List<long>();

            // List of Step Ids
            testStepIds = deletedTestSteps;

            // List of B_TEST_DATA_SETTING
            List<B_TEST_DATA_SETTING> bExistingTestDataSettingsList = BoHelper.LoadBOTestDataSettings(testStepIds,objDbCntx);
            DataTable dt = DataTableUtil.ToDataTable(bExistingTestDataSettingsList);

            if (dataSetId != -1)
            {
                bExistingTestDataSettingsList = (from p in bExistingTestDataSettingsList
                                                 where p.DATA_SUMMARY_ID == dataSetId
                                                 select p).ToList();
            }

            // List of pool Ids
            existingPoolIds = (from d in bExistingTestDataSettingsList
                               where d.POOL_ID != null
                               select (long)d.POOL_ID).ToList();

            // List od Data Setting Ids
            List<long> bExistingTestDataSettingsIdList = (from d in bExistingTestDataSettingsList select d.DATA_SETTING_ID).ToList();

            // List of B_SHARED_OBJECT_POOL
            List<B_SHARED_OBJECT_POOL> bExistingSharedObjectPoolList = BoHelper.GetSharedObjectPoolInfoByDataByPoolIdList(strDBIdx, 
                existingPoolIds, objDbCntx);
            DataTable dt2 = DataTableUtil.ToDataTable(bExistingSharedObjectPoolList);

            // Determine pool rows to be deleted
            foreach (long poolId in existingPoolIds)
            {
                int count = (from d in bExistingTestDataSettingsList
                             where d.POOL_ID != null && d.POOL_ID == poolId
                             select d).Count();

                if (count == 1)
                    poolIds.Add(poolId);
            }

            result = DeleteDataAndPool(testStepIds, existingPoolIds,objDbCntx);
            if (commitChanges&&(objDbCntx==null))
                BoHelper.SaveChanges(MarsMainWindow.CurrentDatabaseIdx);
            return result;
        }

        public static bool Link(
            string strDBIdx, 
            long testCaseId,
            long dataSetId,
            ObservableCollection<TestStepViewModel> testSteps
                           )
        {
            Logger.logBegin("Link",string.Format("Test case id:[{0}]"));

            bool result = false;
            List<B_TEST_DATA_SETTING> bTestDataSettingsList = new List<B_TEST_DATA_SETTING>();
            ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();

            MarsTransactionMgr objTrans = new MarsTransactionMgr(strDBIdx, true);
            using (var scope = new TransactionScope())
            {

                var poolList = BoHelper.GetSharedObjectPoolInfoByDataSummaryId(strDBIdx, dataSetId, objTrans.CurrentDBContext);
                DataTable dt = DataTableUtil.ToDataTable(poolList);
                // 1. Create REL_TC_DATA_SUMMARY
                BoHelper.CreateRelTCDataSummary(MarsMainWindow.CurrentDatabaseIdx, dataSetId, testCaseId,objTrans.CurrentDBContext);

                // 2. For each test step 
                //    2.1 Create TEST_DATA_SETTING 
                foreach (TestStepViewModel testStep in testSteps)
                {
                    testStep.InitDataSets();

                    for (int loopId = 1; loopId <= MarsConstants.NumberOfDataSetColumns; loopId++)
                    {

                        string objectName;
                        long objectOrder;
                        if (testStep.SelectedObjectName != null)
                            objectName = testStep.SelectedObjectName.ObjName;
                        else
                        {
                            if (testStep.SelectedKeyword == null || testStep.SelectedKeyword.KeywordName == null)
                                continue;
                            objectName = testStep.SelectedKeyword.KeywordName;
                        }

                        objectOccuranceRepository.UpdateObjectOrder(objectName, loopId);
                        objectOrder = objectOccuranceRepository.GetObjectOrder(objectName, loopId);

                        // find related pool
                        var pool = (from p in poolList
                                    where p.LOOP_ID == loopId &&
                                          p.OBJECT_ORDER == objectOrder &&
                                          p.OBJECT_NAME == objectName
                                    select p).FirstOrDefault();
                        short sDataDirection = (short)(testStep.IsSkipForDataset ? 4 : 0);
                        if (pool != null)
                        {
                            B_TEST_DATA_SETTING testDataSetting = CreateTestDataSettings(pool.DATA_VALUE,
                                                                                         dataSetId,
                                                                                         loopId,
                                                                                         pool.OBJECT_POOL_ID,
                                                                                         testStep.StepNo,
                                                                                         sDataDirection,
                                                                                         objTrans.CurrentDBContext);

                            bTestDataSettingsList.Add(testDataSetting);
                        }
                    }

                }

                dt = DataTableUtil.ToDataTable(bTestDataSettingsList);

                if (BoHelper.SaveDataSettings(MarsMainWindow.CurrentDatabaseIdx, bTestDataSettingsList, objTrans.CurrentDBContext) > 0)
                {
                    objTrans.CurrentDBContext.SaveChanges();
                    scope.Complete();
                    System.Windows.MessageBox.Show("Test Data saved successfully");
                    result = true;
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to save Test Data");
                    result = false;
                }

                return result;
            }
        }

        public static bool UnLink(string strDBIdx, 
            long testCaseId,
            long dataSetId,
            ObservableCollection<TestStepViewModel> testSteps)
        {
            bool result = false;

            List<long> testCaseStepsIds = (from ts in testSteps select ts.StepNo).ToList();

            // delete data
            Delete(strDBIdx, testCaseStepsIds, null,true, dataSetId);

            // delete REL_TC_DATA_SUMMARY 
            BoHelper.DeleteRelTestCaseDataSummary(MarsMainWindow.CurrentDatabaseIdx, testCaseId, dataSetId);
            BoHelper.SaveChanges(MarsMainWindow.CurrentDatabaseIdx);
            return result;
        }

        public static bool SaveAndDelete(
            string strDBIdx, 
            long testCaseId,
                                         long dataSetId,
                                         ObservableCollection<TestStepViewModel> testSteps,
                                         List<long> deletedTestSteps)
        {
            Logger.logBegin("SaveAndDelete",string.Format("Test case Id [{0}] data set Id:[{1}]", testCaseId,dataSetId));
            bool result = false;
            try
            {
                MarsTransactionMgr objTrans = new MarsTransactionMgr(strDBIdx,true);
                string strError = "";
                using (var scope = new TransactionScope())
                {
                    result = Delete(strDBIdx, deletedTestSteps, objTrans.CurrentDBContext);
                    if (result == true)
                        result = Save(strDBIdx,testCaseId, dataSetId, testSteps,objTrans.CurrentDBContext,ref strError);

                    /** move to transaction part
                    if (result == true)
                    {
                        System.Windows.MessageBox.Show("Test Data saved successfully");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Failed to save Test Data");
                    }
    **/
                    if (result)
                    {
                        int iCnt = objTrans.CurrentDBContext.SaveChanges();
                        scope.Complete();

                        Logger.Info("SaveAndDelete", String.Format("[{0}] records are inserted/updated", iCnt));
                        ViewModelBase.HintByMessageBox("Test Data saved successfully.");
                    }
                    else
                    {
                        ViewModelBase.HintByMessageBox("Failed to save Test Data.\r\n"+strError);
                    }
                    return result;
                }
            }
            catch (Exception e)
            {
                string strError = "";
                Logger.Error("SaveAndDelete", strError = string.Format("Exception:[{0}] stackTrace:\r\n{1}",e.Message,e.StackTrace) );
                ViewModelBase.HintByMessageBox(strError, "Error");
                return false;
            }
            finally
            {
                Logger.logEnd("SaveAndDelete");
            }


        }
        
        public static List<B_SHARED_OBJECT_POOL> GetPoolDataForTestStep(
            string strDBIdx,
            long dataSetId,
                                                                        ObservableCollection<TestStepViewModel> testSteps,
                                                                        long stepRunOrder)
        {
            List<B_SHARED_OBJECT_POOL> dataList = new List<B_SHARED_OBJECT_POOL>();
            var poolList = BoHelper.GetSharedObjectPoolInfoByDataSummaryId(strDBIdx, dataSetId);
            if (poolList == null) return new List<B_SHARED_OBJECT_POOL>();
            ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();


            for (int i = 0; i < testSteps.Count; i++)
            {
                TestStepViewModel testStep = testSteps[i];
                testStep.InitDataSets();

                for (int loopId = 1; loopId <= MarsConstants.NumberOfDataSetColumns; loopId++)
                {
                    string objectName;
                    long objectOrder;
                    if (testStep.SelectedObjectName != null)
                        objectName = testStep.SelectedObjectName.ObjName;
                    else
                    {
                        if (testStep.SelectedKeyword != null)
                            objectName = testStep.SelectedKeyword.KeywordName;
                        else
                        {
                            continue;
                        }
                    }
                    if (testStep.DataSets[loopId] != null)
                    {
                        objectOccuranceRepository.UpdateObjectOrder(objectName, loopId);
                    }

                    objectOrder = objectOccuranceRepository.GetObjectOrder(objectName, loopId) + 1;
                    // find related pool
                    if (testStep.RunOrder == stepRunOrder)
                    {
                        try
                        {
                            var foundPool = (from pool in poolList
                                             where
                                                   pool!=null &&
                                                   string.Compare((objectName??""),(pool.OBJECT_NAME??""),true)==0 &&
                                                   //(pool.OBJECT_NAME==null?false: pool.OBJECT_NAME.Equals(objectName)) &&
                                                   pool.OBJECT_ORDER == objectOrder &&
                                                   pool.LOOP_ID == loopId
                                             select pool).FirstOrDefault();

                            if (foundPool != null)
                                dataList.Add(foundPool);
                        }
                        catch (Exception e)
                        {
                            Logger.Error("GetPoolDataForTestStep",string.Format("Exception:[{0}]",e.Message),e);
                        }
                        
                    }
                }
            }

            return dataList;
        }

        public static long CreateDataSet(long testCaseId, string dataSetName, MarsEntities objCntx  )
        {
            // 1. Create T_TEST_DATA_SUMMARY
            long dataSetId = BoHelper.CreateSharedTestDataSummary(MarsMainWindow.CurrentDatabaseIdx, dataSetName, "", objCntx);
            // 2. Create REL_TC_DATA_SUMMARY
            BoHelper.CreateRelTCDataSummary(MarsMainWindow.CurrentDatabaseIdx, dataSetId, testCaseId, objCntx);

            return dataSetId;
        }

        #region BUSINESS OBJECT manipulation
        private static B_TEST_DATA_SETTING CreateTestDataSettings(string data, long summaryId, int loopId, long poolId, long stepId,short sDataDirection,MarsEntities objDbCntx=null)
        {
            B_TEST_DATA_SETTING testDataSetting = new B_TEST_DATA_SETTING();
            testDataSetting.DATA_SETTING_ID = BoHelper.GetDataSettingId(MarsMainWindow.CurrentDatabaseIdx, objDbCntx); //assign new test data setting Id
            testDataSetting.STEPS_ID = stepId; ;
            testDataSetting.LOOP_ID = loopId;
            testDataSetting.DATA_SUMMARY_ID = summaryId;
            testDataSetting.POOL_ID = poolId;
            testDataSetting.DATA_DIRECTION = sDataDirection;
            // populate data

            DateTime datetime;

            if (data != null)
            {
                if (DateTime.TryParseExact(data, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.NoCurrentDateDefault, out datetime))
                    testDataSetting.DATA_VALUE = datetime.ToString("MM/dd/yyyy");
                else
                    testDataSetting.DATA_VALUE = data;
            }

            return testDataSetting;
        }

        private static B_SHARED_OBJECT_POOL CreateSharedTestPool(TestStepViewModel testStep,
                                                                 long loopId,
                                                                 long dataSummaryId,
                                                                 string objectName,
                                                                 long objectOrder,
                                                                 string dataValue,
                                                                 ObjectOccuranceRepository objectOccuranceRepository,
                                                                 MarsEntities objDbCntx=null)
        {
            B_SHARED_OBJECT_POOL pool = new B_SHARED_OBJECT_POOL();
            pool.OBJECT_POOL_ID = BoHelper.GetTestStepsId(objDbCntx);
            pool.LOOP_ID = (long?)loopId;
            pool.DATA_VALUE = dataValue;
            pool.DATA_SUMMARY_ID = dataSummaryId;
            pool.OBJECT_ORDER = objectOrder;
            pool.OBJECT_NAME = objectName;
            return pool;
        }

        private static bool SaveDataAndPool(List<B_TEST_DATA_SETTING> bTestDataSettingsList, List<B_SHARED_OBJECT_POOL> bSharedObjectPoolList,MarsEntities dbCntx=null)
        {
            Logger.logBegin("SaveDataAndPool");
            bool result;

            if (BoHelper.SaveObjectPool(MarsMainWindow.CurrentDatabaseIdx, bSharedObjectPoolList,dbCntx) > 0)
                result = true;
            else
                result = false;

            if (BoHelper.SaveDataSettings(MarsMainWindow.CurrentDatabaseIdx, bTestDataSettingsList,dbCntx) > 0)
                result = true;
            else
                result = false;

            if (result == true)
            {
                if (dbCntx==null)
                    System.Windows.MessageBox.Show("Test Data saved successfully");
            }
            else
            {
                if (dbCntx==null)
                    System.Windows.MessageBox.Show("Failed to save Test Data");
            }
            Logger.logEnd("SaveDataAndPool");
            return result;
        }

        private static bool DeleteDataAndPool(List<long> bTestStepsIdList, List<long> bSharedObjectPooIlList,MarsEntities dbCntx)
        {
            BoHelper.DeleteDataSettings(MarsMainWindow.CurrentDatabaseIdx, bTestStepsIdList,dbCntx);

            // AF NOT DELETING -- KEEP SHARED POOLS and TEST_DATA_SUMMARY 
            // BoHelper.DeleteObjectPool(bSharedObjectPooIlList);

            return true;
        }

        #endregion

    }

    public class ObjectOccuranceRepository
    {
        private Dictionary<string, int>[] objectOccuranceDictArray = new Dictionary<string, int>[21];

        public void UpdateObjectOrder(string objectName, long loopId)
        {
            // Console.WriteLine("UpdateObjectOrder objectName = " + objectName + " loopId = " + loopId);
            Dictionary<string, int> objectOccuranceDict;

            if (objectOccuranceDictArray[loopId] == null)
                objectOccuranceDictArray[loopId] = new Dictionary<string, int>();

            objectOccuranceDict = objectOccuranceDictArray[loopId];

            if (objectOccuranceDict.ContainsKey(objectName))
                objectOccuranceDict[objectName]++;
            else
                objectOccuranceDict.Add(objectName, 1);
        }

        public int GetObjectOrder(string objectName, long loopId)
        {
            int objectOrder = 0;
            Dictionary<string, int> objectOccuranceDict;
            if (objectOccuranceDictArray[loopId] != null)
            {
                objectOccuranceDict = objectOccuranceDictArray[loopId];
                if (objectOccuranceDict.ContainsKey(objectName))
                    objectOrder = objectOccuranceDict[objectName];
            }
            return objectOrder;
        }
    }
}
