using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.DataLoader
{
    public class TestCaseDataLoader
    {
        private System.Data.DataSet _tcImportDataSet;

        public System.Data.DataSet TcImportDataSet
        {
            get { return _tcImportDataSet; }
            set { _tcImportDataSet = value; }
        }
        private Dictionary<string, bool> _tcImportDictionary;

        public Dictionary<string, bool> TcImportDictionary
        {
            get { return _tcImportDictionary; }
            set { _tcImportDictionary = value; }
        }
        private string testSuite;

        public TestCaseDataLoader(string testSuite, System.Data.DataSet ds, Dictionary<string, bool> dict, long appId)
        {
            // TODO: Complete member initialization
            this._tcImportDataSet = ds;
            this._tcImportDictionary = dict; 
            this.testSuite = testSuite;
            this.AppId = appId;
        }


        internal void LoadAll()
        {
            foreach (DataTable dt in  _tcImportDataSet.Tables)
            {

                if (_tcImportDictionary[dt.TableName] == true)
                {
                    LoadTestCase(dt);
                }
            }
        }

        private void LoadTestCase(DataTable dt)
        {
            CreateTestCase(dt.TableName);
            LoadTestCaseSteps(dt);
            SaveTestSteps();
        }

        private void SaveTestSteps()
        {
                
        }

        private void LoadTestCaseSteps(DataTable dt)
        {
            
        }

        private void CreateTestCase(string p)
        {
            
        }

        public long AppId { get; set; }
    }
}
