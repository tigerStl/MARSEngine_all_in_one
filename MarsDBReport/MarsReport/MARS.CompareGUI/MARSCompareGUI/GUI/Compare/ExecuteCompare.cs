
/*Class to execute compare processor
  Takes an object of the type ComparewithID as input */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !db4SQL
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
#else
using System.Data.SqlClient;
#endif

using System.Data;
using System.Xml;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;


namespace MARS.TEMP
{
    class ExecuteCompare
    {
        public static void ExecuteCompareProgram(ComparewithID NewCompareConfig)
        {
            XmlDocument[] comparesourcedocs = new XmlDocument[2];
            comparesourcedocs[0] = new XmlDocument();
            comparesourcedocs[1] = new XmlDocument();
            DataTable dt = new DataTable();

            // ********************************************Configuring Input***********************************************
            // S1 DATABASE
            if (NewCompareConfig.S1Type == "DATABASE")
            {
                string dbconstring = NewCompareConfig.S1ConnString;
                string dbsqlquery = NewCompareConfig.S1Query;
#if !db4SQL
                using (OracleConnection sqlConnection = new OracleConnection(dbconstring))
                {
                    OracleCommand command = new OracleCommand(dbsqlquery, sqlConnection);
                    OracleDataAdapter adapter = new OracleDataAdapter(command);
                    OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    dt = ds.Tables[0];
                    Console.WriteLine("\ndbSource ready for access");
                }
#else
                using (SqlConnection sqlConnection = new SqlConnection(dbconstring))
                {
                    SqlCommand command = new SqlCommand(dbsqlquery, sqlConnection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    dt = ds.Tables[0];
                    Console.WriteLine("\ndbSource ready for access");
                }
#endif
                comparesourcedocs[0] = Conversion.DataTableToDom(dt);
            }
            // S1 XML 
            if (NewCompareConfig.S1Type == "XML")
            {
                comparesourcedocs[0].Load(NewCompareConfig.S1FileLocation);
            }
            // S1 CSV
            if (NewCompareConfig.S1Type == "CSV")
            {
                comparesourcedocs[0] = Conversion.CsvToDom(NewCompareConfig.S1FileLocation);
            }
            // S2 DATABASE
            if (NewCompareConfig.S2Type == "DATABASE")
            {
                string dbconstring = NewCompareConfig.S2ConnString;
                string dbsqlquery = NewCompareConfig.S2Query;
#if !db4SQL
                using (OracleConnection sqlConnection = new OracleConnection(dbconstring))
                {
                    OracleCommand command = new OracleCommand(dbsqlquery, sqlConnection);
                    OracleDataAdapter adapter = new OracleDataAdapter(command);
                    OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    dt = ds.Tables[0];
                    Console.WriteLine("\ndbSource ready for access");
                }
#else
                using (SqlConnection sqlConnection = new SqlConnection(dbconstring))
                {
                    SqlCommand command = new SqlCommand(dbsqlquery, sqlConnection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    dt = ds.Tables[0];
                    Console.WriteLine("\ndbSource ready for access");
                }
#endif
                comparesourcedocs[1] = Conversion.DataTableToDom(dt);
            }

            // S2 XML 
            if (NewCompareConfig.S2Type == "XML")
            {
                comparesourcedocs[1].Load(NewCompareConfig.S2FileLocation);
            }
            // S2 CSV
            if (NewCompareConfig.S2Type == "CSV")
            {
                comparesourcedocs[1] = Conversion.CsvToDom(NewCompareConfig.S2FileLocation);
            }

            // ********************************************Configuring Output**********************************************
            string excelfilelocation = NewCompareConfig.OFileLocation;

            // ********************************************Executing Compare***********************************************
            XmlCompareConfig xcc = initCompareConfig(NewCompareConfig);
            XmlCompareProcessor xcp = new XmlCompareProcessor(comparesourcedocs[0], comparesourcedocs[1], xcc);
            XmlCompareResult xcr = xcp.ProcessCompare();
            
            //get datatable containing diff data
            DataTable difftable = new DataTable();
            difftable = xcp.GetDiffTable();
            //end of diff file code
           
            if (xcr == null)
                return;
            XmlCompareReportConfig xcrc = initXmlCompareReportConfig();
            XmlCompareReport xcrpt = new XmlCompareReport(xcr, xcrc, difftable, xcp, xcc);

            //excelfilelocation = excelfilelocation.Replace(".xlsx", "");
            String timeStamp = DateTime.Now.ToString("yyMMddHHmmss");
            //xlfilepath = filePath + "\\MARS_DIR_Comp_" + timeStamp + ".xlsx";
            //xcrpt.ProcessReport(excelfilelocation + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            xcrpt.ProcessReport(excelfilelocation +"\\MARS_Date_Comp_" + timeStamp);
        }

        public static XmlCompareReportConfig initXmlCompareReportConfig()
        {
            XmlCompareReportConfig config = new XmlCompareReportConfig();
            return config;
        }

        public static XmlCompareConfig initCompareConfig(ComparewithID NewCompareConfig)
        {
            XmlCompareConfig config = new XmlCompareConfig();

            config.BlockTag = "rs:data";
            config.ElementTag = "z:row";

            // key fields
            string[] temp = Util.Split(NewCompareConfig.KeyFields, ',');
            config.KeyFields.AddRange(temp);
            for (int i = 0; i < config.KeyFields.Count; i++)
                config.KeyFields[i] = config.KeyFields[i].Trim();

            // compare fields
            temp = Util.Split(NewCompareConfig.CompareFields, ',');
            config.CompareFields.AddRange(temp);
            for (int i = 0; i < config.CompareFields.Count; i++)
                config.CompareFields[i] = config.CompareFields[i].Trim();

            // show fields
            temp = Util.Split(NewCompareConfig.ShowFields, ',');
            config.ShowFields.AddRange(temp);
            for (int i = 0; i < config.ShowFields.Count; i++)
                config.ShowFields[i] = config.ShowFields[i].Trim();

            // Exclude fields
            temp = Util.Split("", ',');
            string[] excludeFields = temp;
            for (int i = 0; i < excludeFields.Length; i++)
                excludeFields[i] = excludeFields[i].Trim();
            config.ExcludeFields.AddRange(excludeFields);

            // Row fields
            temp = Util.Split(NewCompareConfig.RowFields, ',');
            config.RowFields.AddRange(temp);
            for (int i = 0; i < config.RowFields.Count; i++)
                config.RowFields[i] = config.RowFields[i].Trim();
            
            // Column fields
            temp = Util.Split(NewCompareConfig.ColumnFields, ',');
            config.ColumnFields.AddRange(temp);
            for (int i = 0; i < config.ColumnFields.Count; i++)
                config.ColumnFields[i] = config.ColumnFields[i].Trim();

            // Field Name Mapping
            FieldNameMapper fieldNameMapper = new FieldNameMapper();
            fieldNameMapper.init("");
            config.SetFieldNameMapper(fieldNameMapper);

            // value adjustment
            AdjustDataMap adjustDataMap = new AdjustDataMap();
            adjustDataMap.init("");
            config.SetAdjustData(adjustDataMap);

            return config;
        }
    }
}
