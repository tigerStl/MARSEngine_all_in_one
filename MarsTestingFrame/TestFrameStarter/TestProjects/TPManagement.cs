using MarsTestFrame.SourceCode.com.Mars.Excels;
using MarsTestFrame.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;

namespace MarsFrameWork.TestProjects
{
    /** Test Project Management */
    public class TPManagement
    {
        public const string CNST_APPSETTING_CURRENT_PROJECT_FILENAME = "CurrentTestProjectsFileName";
        public const string CNST_DEFAULT_TESTPROJECTNAME = "Batch";
        private const string CONNECTION_STRING = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=<FILENAME>;Extended Properties=\"Excel 8.0;HDR=Yes;MaxScanRows=1;IMEX=1;\";";

        private const string cnst_default_projectfilename = "qtpBatch.xls";

        public static string AppRootDir = null;
        public static string CurrentTestProjectName = null;
#if _Datafrom_Database
        private static List<MarsKeyValues<string,string>> TestProjectNames = null;
#else
        private static List<string> TestProjectNames = null;
#endif
        private static void GetAppRootDir()
        {
#if _NO_C_DRIVER_WRITE
            string strDir = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
#else
            string strDir = Path.GetDirectoryName(typeof(TPManagement).Assembly.Location);
            strDir = Directory.GetParent(strDir).FullName;
#endif
            AppRootDir = strDir;
            if (!Directory.Exists(AppRootDir))
                Directory.CreateDirectory(AppRootDir);
        }

        private static void GetCurrentProjectFileName()
        {
            if (CurrentTestProjectName == null)
            {
                CurrentTestProjectName = ConfigurationManager.AppSettings[CNST_APPSETTING_CURRENT_PROJECT_FILENAME];
                if (string.IsNullOrEmpty(CurrentTestProjectName))
                    CurrentTestProjectName = cnst_default_projectfilename;
            }
        }

        public static string GetCurrentTestProjectFileName()
        {
            GetCurrentProjectFileName() ;
            if (AppRootDir == null) GetAppRootDir();
            return string.Format(@"{0}\Dash Board\{1}",AppRootDir, CurrentTestProjectName );
        }
#if _Datafrom_Database
        public static List<MarsKeyValues<string, string>> GetTestProjects()
#else

        public static List<string> GetTestProjects()
#endif
        {
#if _Datafrom_Database
            if (TestProjectNames == null)
            {
                if (!DashBoardFactory.IsDashBoardFromDB())
                {
                    string strFullPath = GetCurrentTestProjectFileName();
                    string strOleDBCnn = CONNECTION_STRING.Replace("<FILENAME>", strFullPath);
                    using (OleDbConnection objCnn = new OleDbConnection(strOleDBCnn))
                    {
                        objCnn.Open();
                        TestProjectNames = new List<MarsKeyValues<string, string>>();
                        DataTable dtSchema = objCnn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                        for (int i = 0; i < dtSchema.Rows.Count; i++)
                        {
                            string strKey = dtSchema.Rows[i]["TABLE_NAME"].ToString().Replace("$", "");
                            TestProjectNames.Add(new MarsKeyValues<string, string>(strKey, strKey));
                        }
                        objCnn.Close();
                    }
                }
                else
                {
                    /// get project information from database by DashBoardFactory interface 
                    /// 
                    /// TestProjectNames = DashBoardFactory.RefreshProjectNamesWithIds();
                    TestProjectNames = DashBoardFactory.RefreshProjectsWithApps();
                }
                
            }
#else
            if (TestProjectNames == null)
            {
                string strFullPath = GetCurrentTestProjectFileName();
                string strOleDBCnn = CONNECTION_STRING.Replace("<FILENAME>", strFullPath);
                using (OleDbConnection objCnn = new OleDbConnection(strOleDBCnn))
                {
                    objCnn.Open();
                    TestProjectNames = new List<string>();
                    DataTable dtSchema = objCnn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    for (int i = 0; i < dtSchema.Rows.Count; i++)
                    {
                        TestProjectNames.Add(dtSchema.Rows[i]["TABLE_NAME"].ToString().Replace("$",""));
                    }
                    objCnn.Close();
                }
            }
            
#endif
            return TestProjectNames;

        }

    }


}
