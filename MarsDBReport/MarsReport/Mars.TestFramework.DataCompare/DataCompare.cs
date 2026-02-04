using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare
{
    public class DataCompare
    {
        string configFileName = Path.Combine(GetCurrentAssemblyDirectory(), "Mars.exe.config");//@"C:\automationTest\Automation Workbooks\dlls\Mars.exe.config";
        private static MLogger Logger = MLogger.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string GetCurrentAssemblyDirectory()
        {
            UriBuilder uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);

            string path = Uri.UnescapeDataString(uri.Path);
            //Logger.Info("GetCurrentAssemblyDirectory",string.Format("Path:[{0}]-[{1}]", path, Path.GetDirectoryName(path)));
            
            return Path.GetDirectoryName(path);
        }

        public bool DoCompareData(string data, string RC, ref string strResultFileName, ref string strError)
        {

            DataCompareError error = new DataCompareError();

            Logger.Info("DoCompareData", string.Format("BEGIN, configFileName-[{0}]", configFileName));

            //configFileName = GetCurrentAssemblyDirectory();

            bool rc = true;
            strResultFileName = null;
            AppConfig.Change(configFileName);

            MarsCompare mc = new MarsCompare();

            string file1 = null;
            string file2 = null;
            string oFile = null;

            //throw new DataCompareException("TEST EXCEPTION");

            if (data != null && data.Trim().Length > 10 )
            {
                try
                {
                    string[] files = data.Split(';');

                    file1 = files[0];
                    file1 = file1.Replace("[", string.Empty);
                    file1 = file1.Replace("]", string.Empty);

                    file2 = files[1];
                    file2 = file2.Replace("[", string.Empty);
                    file2 = file2.Replace("]", string.Empty);

                    oFile = files[2];
                    oFile = oFile.Replace("[", string.Empty);
                    oFile = oFile.Replace("]", string.Empty);
                }

                
                catch (Exception ex)
                {
                    strError = "Error parsing Data field passed from MARS ";
                    Logger.Error("DoCompareData",strError = string.Format("Exception:[{0}] \r\nStackTrace:[{1}]", ex.Message, ex.StackTrace),ex);
                    rc = false;
                }
            }

            if (rc != false)
            {

                try
                {
                    strResultFileName = mc.RunCompare(RC, file1, file2, oFile, ref error);
                }

                catch (DataCompareException ex)
                {
                    Logger.Error("DoCompareData", strError = string.Format("Exception:[{0}] \r\nStackTrace:[{1}]", ex.Message, ex.StackTrace), ex);
                    rc = false;
                }

                catch (Exception ex)
                {
                    strError = "Error running compare job";
                    Logger.Error("DoCompareData", strError = string.Format("Exception:[{0}] \r\nStackTrace:[{1}]", ex.Message, ex.StackTrace), ex);
                    rc = false;
                }

                if (strResultFileName != null)
                {
                    strError = "NO ERROR";
                    rc = true;
                }

                else
                {
                    strError = "Error running compare job";
                    rc = false;
                }

            }

            Logger.Info("DoCompareData", "END");
            return rc;
        }


        public static void test()
        {

            DataCompare dc = new DataCompare();

            string data = @"[C:\temp\CSV1.csv];[C:\temp\CSV2.csv];[C:\temp\Compare_result.xlsx]";

            string rc = "CSV--CSV";

            // OPICS reports
            rc = "REPORT_RBACC";
            data = @"[C:\MDEV\xmlCompareTest\REPORT\R01RBACC1.rpt];[C:\MDEV\xmlCompareTest\REPORT\R01RBACC2.rpt];[C:\temp\Compare_result.xlsx]";

            // OPICS reports with format files specified
            rc = "REPORT_RFIAI";
            data = @"[C:\MDEV\xmlCompareTest\REPORT\R01RFIAI1.rpt];[C:\MDEV\xmlCompareTest\REPORT\R01RFIAI2.rpt];[C:\temp\Compare_result.xlsx]";

            // XML 
            rc = "ACCTPOST";
            data = @"[C:\Users\marquis\Documents\Compare\Data Files for Compare - Nov 4 - Demo\Acctpost53 (XML-XML)\Acctpost53short.xml];[C:\Users\marquis\Documents\Compare\Data Files for Compare - Nov 4 - Demo\Acctpost53 (XML-XML)\Acctpost53short-mod.xml];[C:\temp\Compare_result.xlsx]";

            // EXCEL
            rc = "EXCEL_DEMO";
            data = @"[C:\temp\cc1.xlsx];[C:\temp\cc2.xlsx];[C:\temp\Compare_result.xlsx]";

            // TEXT
            rc = "TEXT_TEST";
            data = @"[C:\automationTest\Automation Workbooks\data\imtf9_1.txt];[C:\automationTest\Automation Workbooks\data\imtf9_2.txt];[C:\temp\Compare_result.xlsx]";

            // SWIFT



            string strResultFileName = "";
            string error = "";

            //data = null;
            try
            { 
                bool retCode = dc.DoCompareData(data, rc, ref strResultFileName, ref error);

                if (retCode == false)
                {

                }
            }
            catch (DataCompareException)
            {

            }

        }
    }
}
