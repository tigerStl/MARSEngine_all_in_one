using Mars.Business;
using Mars.DataLayer;
using Mars.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel
{
    public class SharedDataSetViewModel
    {
        static string[] format = new string[] {"M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                         "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                         "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                         "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                         "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};

        internal static bool SaveAs(string dataSetName,
                                    long testCaseId,
                                    ObservableCollection<TestCaseEditViewModel> testSteps,
                                    ref long dataSetId)
        {
            bool result = false;

            List<B_TEST_DATA_SETTING> bTestDataSettingsList = new List<B_TEST_DATA_SETTING>();
            List<B_SHARED_OBJECT_POOL> bSharedObjectPoolList = new List<B_SHARED_OBJECT_POOL>();
            ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();

            // 1. Create T_TEST_DATA_SUMMARY
            dataSetId = BoHelper.CreateSharedTestDataSummary(dataSetName);
            // 2. Create REL_TC_DATA_SUMMARY
            BoHelper.CreateRelTCDataSummary(dataSetId, testCaseId);

            // 3. For each test step 
            foreach (TestCaseEditViewModel testStep in testSteps)
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

                        //    3.2 Create TEST_DATA_SETTING 
                        B_TEST_DATA_SETTING testDataSetting = CreateTestDataSettings(testStep.DataSets[loopId].ToString(), dataSetId, loopId, pool.OBJECT_POOL_ID, testStep.StepNo);
                        bTestDataSettingsList.Add(testDataSetting);
                    }
                }
            }

            DataTable dt = DataTableUtil.ToDataTable(bTestDataSettingsList);
            dt = DataTableUtil.ToDataTable(bSharedObjectPoolList);
            result = SaveDataAndPool(bTestDataSettingsList, bSharedObjectPoolList);

            return result;
        }

        public static bool Save(long testCaseId, long dataSetId, ObservableCollection<TestCaseEditViewModel> testSteps)
        {
            bool result = false;
            List<B_TEST_DATA_SETTING> bTestDataSettingsList = new List<B_TEST_DATA_SETTING>();
            List<B_SHARED_OBJECT_POOL> bSharedObjectPoolList = new List<B_SHARED_OBJECT_POOL>();

            List<B_TEST_DATA_SETTING> bExistingTestDataSettingsList = BoHelper.LoadBOTestDataSettingsBySummaryId(dataSetId);
            List<B_SHARED_OBJECT_POOL> bExistingSharedObjectPoolList = BoHelper.GetSharedObjectPoolInfoByDataSummaryId(dataSetId);

            DataTable dt1 = DataTableUtil.ToDataTable(bExistingTestDataSettingsList);
            DataTable dt2 = DataTableUtil.ToDataTable(bExistingSharedObjectPoolList);

            ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();
            B_SHARED_OBJECT_POOL pool;
            // 1.  For each test step 
            //    1.1 Update T_SHARED_TEST_POOL
            foreach (TestCaseEditViewModel testStep in testSteps)
            {
                testStep.InitDataSets();
                for (int loopId = 1; loopId <= MarsConstants.NumberOfDataSetColumns; loopId++)
                {
                    if (testStep.DataSets[loopId] != null)
                    {
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
                                                        objectOccuranceRepository);
                            bSharedObjectPoolList.Add(pool);
                            B_TEST_DATA_SETTING newTestDataSetting = CreateTestDataSettings(testStep.DataSets[loopId].ToString(), dataSetId, loopId, pool.OBJECT_POOL_ID, testStep.StepNo);
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
                                                               objectOccuranceRepository);

                                    bSharedObjectPoolList.Add(pool);
                                }

                                testDataSetting.POOL_ID = pool.OBJECT_POOL_ID;
                                bTestDataSettingsList.Add(testDataSetting);
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
                                                                objectOccuranceRepository);
                                    bSharedObjectPoolList.Add(pool);

                                    testDataSetting.POOL_ID = pool.OBJECT_POOL_ID;
                                    bTestDataSettingsList.Add(testDataSetting);

                                }


                                else if (pool.DATA_VALUE.Equals(newData) == false)
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

                                    bSharedObjectPoolList.Add(pool);
                                }
                            }
                        }
                    }
                }
            }

            DataTable dt = DataTableUtil.ToDataTable(bTestDataSettingsList);
            dt = DataTableUtil.ToDataTable(bSharedObjectPoolList);
            result = SaveDataAndPool(bTestDataSettingsList, bSharedObjectPoolList);

            return result;
        }

        public static bool Delete(List<long> deletedTestSteps, bool commitChanges = true, long dataSetId = -1)
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
            List<B_TEST_DATA_SETTING> bExistingTestDataSettingsList = BoHelper.LoadBOTestDataSettings(testStepIds);
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
            List<B_SHARED_OBJECT_POOL> bExistingSharedObjectPoolList = BoHelper.GetSharedObjectPoolInfoByDataByPoolIdList(existingPoolIds);
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

            result = DeleteDataAndPool(testStepIds, existingPoolIds);
            if (commitChanges)
                BoHelper.SaveChanges();
            return result;
        }

        public static bool Link(long testCaseId,
                           long dataSetId,
                           ObservableCollection<TestCaseEditViewModel> testSteps)
        {
            bool result = false;
            List<B_TEST_DATA_SETTING> bTestDataSettingsList = new List<B_TEST_DATA_SETTING>();
            ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();
            var poolList = BoHelper.GetSharedObjectPoolInfoByDataSummaryId(dataSetId);
            DataTable dt = DataTableUtil.ToDataTable(poolList);
            // 1. Create REL_TC_DATA_SUMMARY
            BoHelper.CreateRelTCDataSummary(dataSetId, testCaseId);

            // 2. For each test step 
            //    2.1 Create TEST_DATA_SETTING 
            foreach (TestCaseEditViewModel testStep in testSteps)
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

                    if (pool != null)
                    {
                        B_TEST_DATA_SETTING testDataSetting = CreateTestDataSettings(pool.DATA_VALUE,
                                                                                     dataSetId,
                                                                                     loopId,
                                                                                     pool.OBJECT_POOL_ID,
                                                                                     testStep.StepNo);

                        bTestDataSettingsList.Add(testDataSetting);
                    }
                }

            }

            dt = DataTableUtil.ToDataTable(bTestDataSettingsList);

            if (BoHelper.SaveDataSettings(bTestDataSettingsList) > 0)
            {
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

        public static bool UnLink(long testCaseId,
                                       long dataSetId,
                                       ObservableCollection<TestCaseEditViewModel> testSteps)
        {
            bool result = false;

            List<long> testCaseStepsIds = (from ts in testSteps select ts.StepNo).ToList();

            // delete data
            Delete(testCaseStepsIds, true, dataSetId);

            // delete REL_TC_DATA_SUMMARY 
            BoHelper.DeleteRelTestCaseDataSummary(testCaseId, dataSetId);
            BoHelper.SaveChanges();
            return result;
        }

        public static bool SaveAndDelete(long testCaseId,
                                         long dataSetId,
                                         ObservableCollection<TestCaseEditViewModel> testSteps,
                                         List<long> deletedTestSteps)
        {
            bool result = false;
            result = Delete(deletedTestSteps);
            if (result == true)
                result = Save(testCaseId, dataSetId, testSteps);


            if (result == true)
            {
                System.Windows.MessageBox.Show("Test Data saved successfully");
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to save Test Data");
            }

            return result;

        }




        public static List<B_SHARED_OBJECT_POOL> GetPoolDataForTestStep(long dataSetId,
                                                                        ObservableCollection<TestCaseEditViewModel> testSteps,
                                                                        long stepRunOrder)
        {
            List<B_SHARED_OBJECT_POOL> dataList = new List<B_SHARED_OBJECT_POOL>();
            var poolList = BoHelper.GetSharedObjectPoolInfoByDataSummaryId(dataSetId);
            ObjectOccuranceRepository objectOccuranceRepository = new ObjectOccuranceRepository();

            foreach (TestCaseEditViewModel testStep in testSteps)
            {
                testStep.InitDataSets();

                for (int loopId = 1; loopId <= MarsConstants.NumberOfDataSetColumns; loopId++)
                {
                    string objectName;
                    long objectOrder;
                    if (testStep.SelectedObjectName != null)
                        objectName = testStep.SelectedObjectName.ObjName;
                    else
                        if (testStep.SelectedKeyword != null)
                            objectName = testStep.SelectedKeyword.KeywordName;
                        else continue;

                    if (testStep.DataSets[loopId] != null)
                    {
                        objectOccuranceRepository.UpdateObjectOrder(objectName, loopId);

                    }

                    objectOrder = objectOccuranceRepository.GetObjectOrder(objectName, loopId) + 1;
                    // find related pool
                    if (testStep.RunOrder == stepRunOrder)
                    {
                        var foundPool = (from pool in poolList
                                         where pool.OBJECT_NAME.Equals(objectName) &&
                                               pool.OBJECT_ORDER == objectOrder &&
                                               pool.LOOP_ID == loopId
                                         select pool).FirstOrDefault();

                        if (foundPool != null)
                            dataList.Add(foundPool);
                    }
                }
            }

            return dataList;
        }

        public static long CreateDataSet(long testCaseId, string dataSetName)
        {
            // 1. Create T_TEST_DATA_SUMMARY
            long dataSetId = BoHelper.CreateSharedTestDataSummary(dataSetName);
            // 2. Create REL_TC_DATA_SUMMARY
            BoHelper.CreateRelTCDataSummary(dataSetId, testCaseId);

            return dataSetId;
        }

        #region BUSINESS OBJECT manipulation
        private static B_TEST_DATA_SETTING CreateTestDataSettings(string data, long summaryId, int loopId, long poolId, long stepId)
        {
            B_TEST_DATA_SETTING testDataSetting = new B_TEST_DATA_SETTING();
            testDataSetting.DATA_SETTING_ID = BoHelper.GetDataSettingId(); //assign new test data setting Id
            testDataSetting.STEPS_ID = stepId; ;
            testDataSetting.LOOP_ID = loopId;
            testDataSetting.DATA_SUMMARY_ID = summaryId;
            testDataSetting.POOL_ID = poolId;
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

        private static B_SHARED_OBJECT_POOL CreateSharedTestPool(TestCaseEditViewModel testStep,
                                                                 long loopId,
                                                                 long dataSummaryId,
                                                                 string objectName,
                                                                 long objectOrder,
                                                                 string dataValue,
                                                                 ObjectOccuranceRepository objectOccuranceRepository)
        {
            B_SHARED_OBJECT_POOL pool = new B_SHARED_OBJECT_POOL();
            pool.OBJECT_POOL_ID = BoHelper.GetTestStepsId();
            pool.LOOP_ID = (long?)loopId;
            pool.DATA_VALUE = dataValue;
            pool.DATA_SUMMARY_ID = dataSummaryId;
            pool.OBJECT_ORDER = objectOrder;
            pool.OBJECT_NAME = objectName;
            return pool;
        }

        private static bool SaveDataAndPool(List<B_TEST_DATA_SETTING> bTestDataSettingsList, List<B_SHARED_OBJECT_POOL> bSharedObjectPoolList)
        {
            bool result;

            if (BoHelper.SaveObjectPool(bSharedObjectPoolList) > 0)
                result = true;
            else
                result = false;

            if (BoHelper.SaveDataSettings(bTestDataSettingsList) > 0)
                result = true;
            else
                result = false;

            if (result == true)
            {
                System.Windows.MessageBox.Show("Test Data saved successfully");
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to save Test Data");
            }

            return result;
        }

        private static bool DeleteDataAndPool(List<long> bTestStepsIdList, List<long> bSharedObjectPooIlList)
        {
            BoHelper.DeleteDataSettings(bTestStepsIdList);

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
