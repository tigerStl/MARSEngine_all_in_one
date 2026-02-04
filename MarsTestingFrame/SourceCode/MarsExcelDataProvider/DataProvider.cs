using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
#if !Managed_Driver
using Oracle.DataAccess.Client;
#else
using Oracle.ManagedDataAccess.Client;
#endif
using System.Data;

#if _Datafrom_Database
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
#endif
namespace MarsExcelDataProvider
{

    public class DataProvider
    {
#if _Datafrom_Database
        private static MLogger logger = MLogger.GetLogger(typeof(DataProvider));
#endif
        public void init()
        {
#if _Datafrom_Database
            OracleUtil.ConnString = AppConfigReader.GetObjectDataConnection();
            ConfigMain.DataDirectory = AppConfigReader.GetDataFileDiretory();
            ConfigMain.TestCaseDirectory = AppConfigReader.GetTCFileDirectory();
            logger.Info("init",string.Format("Database Connection:[{0}]\r\n\tTC Dirctory:[{1}] Data Directory:[{2}]", OracleUtil.ConnString, ConfigMain.TestCaseDirectory,ConfigMain.DataDirectory));
#else
            System.Configuration.Configuration myDllConfig =
                  ConfigurationManager.OpenExeConfiguration(this.GetType().Assembly.Location);

            AppSettingsSection myDllConfigAppSettings =
                   (AppSettingsSection)myDllConfig.GetSection("appSettings");

            OracleUtil.ConnString = myDllConfigAppSettings.Settings["ConnString"].Value;
            ConfigMain.DataDirectory = myDllConfigAppSettings.Settings["DataDirectory"].Value;
            ConfigMain.TestCaseDirectory = myDllConfigAppSettings.Settings["TestCaseDirectory"].Value;
            Console.WriteLine(OracleUtil.ConnString);
#endif
        }


        public bool ExportTestSuite(string testProjectRequested, string testSuiteRequested, out string reason)
        {
            bool success = true;
            reason = "Success";
            try
            {
                List<string> tcList = OracleUtil.GetTCList(testProjectRequested, testSuiteRequested);

                GenerateTCWorkbook(testProjectRequested, testSuiteRequested, tcList);
                GenerateDataWorkbook(testProjectRequested, testSuiteRequested);
            }
            catch (Exception ex)
            {
                success = false;
                reason = ex.StackTrace;
            }

            return success;
        }

        
#if _Datafrom_Database

        private static OracleUtil gOracleUtil = null;
        private static void InitOracleUtil()
        {
            if (gOracleUtil == null)
                gOracleUtil = new OracleUtil(); 
        }
        private static OracleUtil GetDatabaseOperate()
        {
            if (gOracleUtil == null)
            {
                InitOracleUtil();
            }
            return gOracleUtil;
        }
        public DataSet GetObjectDataSetByAppName(string strAppShortName)
        {
            return GetDatabaseOperate().GenObjects(strAppShortName);
        }


        internal DataSet GetProjectDataSetByProjectName(string strProjectName)
        {
            return GetDatabaseOperate().GenProjects(strProjectName);
        }
        internal DataSet GetProjectAppsByAppNamesProjectNames(string strAppName, string strProjectName)
        {
            return GetDatabaseOperate().GenProjectAppsByAppNamesProjectNames(strAppName,strProjectName);
        }

        internal DataSet GetDashboardFullViewByIDAndRunType(string strPrjID, int?[] arr_iRunTypeFilter)
        {
            return GetDatabaseOperate().GenDashboardFullViewByParas(new string[] {"PROJECT_ID", "TEST_RUN_VALUE" }, 
                new object[] { strPrjID, arr_iRunTypeFilter});
        }
#endif
        private void GenerateDataWorkbook(string testProjectRequested, string testSuiteRequested)
        {
            DataSet ds = new DataSet();
            DataTable dt = OracleUtil.GetDataSheetTable(testProjectRequested,  testSuiteRequested);
            
            ds.Tables.Add(dt);
            ExcelUtilities.ExportDataSetToExcel(ds, ConfigMain.DataDirectory + "\\" + testProjectRequested + "_" + testSuiteRequested + "_data.xls", true);
        }

        
        private void GenerateTCWorkbook(string testProjectRequested, string testSuiteRequested, List<string> tcList)
        {
            DataSet ds = OracleUtil.GenTCDataSet(testProjectRequested, testSuiteRequested, tcList, false);

            ExcelUtilities.ExportDataSetToExcel(ds, ConfigMain.TestCaseDirectory + "\\" + testProjectRequested + "_" + testSuiteRequested + ".xls", true);
        }

        

        public bool ExportDataSet(string testProjectRequested, string testSuiteRequested, out DataSet ds, out string reason)
        {
            bool rc = true;
            reason = "";
            ds = null;
            try
            {
                List<string> tcList = OracleUtil.GetTCList(testProjectRequested, testSuiteRequested);
                ds = OracleUtil.GenTCDataSet(testProjectRequested, testSuiteRequested, tcList, true);

                DataTable dt = OracleUtil.GetDataSheetTable(testProjectRequested, testSuiteRequested);
                ds.Tables.Add(dt);

                dt = OracleUtil.GetObjectDataTable();
                ds.Tables.Add(dt);

            }
            catch(Exception ex)
            {
                rc = false;
                reason = ex.Message;
            }
           

            return rc;
        }


    }
}
