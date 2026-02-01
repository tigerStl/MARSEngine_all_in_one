using Mars.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Helpers
{
    public class DeleteTestCaseHelper
    {
        private List<long> _testCaseList = new List<long>();

        public List<long> TestCaseList
        {
            get { return _testCaseList; }
            set { _testCaseList = value; }
        }
        private List<long> _relAppTestCaseList = new List<long>();

        public List<long> RelAppTestCaseList
        {
            get { return _relAppTestCaseList; }
            set { _relAppTestCaseList = value; }
        }
        private List<long> _relTestCaseTestSuiteList = new List<long>();

        public List<long> RelTestCaseTestSuiteList
        {
            get { return _relTestCaseTestSuiteList; }
            set { _relTestCaseTestSuiteList = value; }
        }
        private List<long> _testCaseStepsList = new List<long>();

        public List<long> TestCaseStepsList
        {
            get { return _testCaseStepsList; }
            set { _testCaseStepsList = value; }
        }
        private List<long> _testDataSummary = new List<long>();

        public List<long> TestDataSummary
        {
            get { return _testDataSummary; }
            set { _testDataSummary = value; }
        }
        private List<long> _relTcDataSummaryList = new List<long>();

        public List<long> RelTcDataSummaryList
        {
            get { return _relTcDataSummaryList; }
            set { _relTcDataSummaryList = value; }
        }

        private List<long> _storyboardList = new List<long>();

        public List<long> StoryboardList
        {
            get { return _storyboardList; }
            set { _storyboardList = value; }
        }

        private List<long> _testDataSettingsList = new List<long>();
        private Model.MarsEntities marsEntities;

        public DeleteTestCaseHelper(Model.MarsEntities marsEntities)
        {
            // TODO: Complete member initialization
            this.marsEntities = marsEntities;
        }

        public List<long> TestDataSettingsList
        {
            get { return _testDataSettingsList; }
            set { _testDataSettingsList = value; }
        }


        internal int ApplyDeletions()
        {
            int rc = 1;

            ApplyDeletions("TEST_DATA_SETTING",         "STEPS_ID", TestDataSettingsList);

            ApplyDeletions("REL_TC_DATA_SUMMARY", "DATA_SUMMARY_ID", RelTcDataSummaryList);

            ApplyDeletions("T_TEST_DATA_SUMMARY",       "DATA_SUMMARY_ID", TestDataSummary);
            ApplyDeletions("T_TEST_STEPS",              "TEST_CASE_ID", TestCaseStepsList);
            ApplyDeletions("T_PROJ_TC_MGR",             "STORYBOARD_DETAIL_ID", StoryboardList);
            ApplyDeletions("REL_TEST_CASE_TEST_SUITE",  "RELATIONSHIP_ID", RelTestCaseTestSuiteList);
            
            ApplyDeletions("REL_APP_TESTCASE",          "RELATIONSHIP_ID", RelAppTestCaseList);
            ApplyDeletions("T_TEST_CASE_SUMMARY",       "TEST_CASE_ID", TestCaseList);
            
    
            return rc;
        }

        private void ApplyDeletions(string tableName, string fieldName, List<long> idList)
        {
            string strError = "";

            foreach(long id in idList)
            {
                BoHelper.DirectDeleteRunner(MarsMainWindow.CurrentDatabaseIdx, tableName, fieldName, id, ref strError);
            }
            
        }
    }
}

/*
RelAppTestCaseList
RelTcDataSummaryLis
RelTestCaseTestSuit
StoryboardList
TestCaseList
TestCaseStepsList
TestDataSettingsLis
TestDataSummary
 *
*/