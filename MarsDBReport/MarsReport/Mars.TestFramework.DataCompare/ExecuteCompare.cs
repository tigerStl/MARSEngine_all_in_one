
/*Class to execute compare processor
  Takes an object of the type ComparewithID as input */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;


using System.Data;
using System.Xml;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data.SqlClient;
using DataTableCompare;
using System.Configuration;
using System.Diagnostics;
using Route2NSEx.src.Marquis.systemUtil;


namespace Mars.TestFramework.DataCompare
{
    public static class ExecuteCompare
    {
        private static MLogger Logger = MLogger.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static string V2_MARKER = "==";

        public static string  ExecuteCompareProgram(ComparewithID NewCompareConfig, ref DataCompareError error, bool isFromWebCommand=false)
        {
            if (DataTableCompareImp(NewCompareConfig))
            {
                var resultFileName = ExecuteDataTableCompareProgram(NewCompareConfig, ref error);
                return resultFileName;
            }

            else
                error = new DataCompareError();


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
                dt = GetDataFromDatabase(NewCompareConfig.S1DBType, NewCompareConfig.S1ConnString, NewCompareConfig.S1Query);
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
                comparesourcedocs[0] = Conversion.CsvToDom(NewCompareConfig.S1FileLocation, NewCompareConfig.S2CSVDelim);
            }
            // S1 REPORT
            if (NewCompareConfig.S1Type == "REPORT")
            {
                comparesourcedocs[0] = Conversion.ReportToDom(NewCompareConfig.S1FileLocation, 1, NewCompareConfig.S1OpicsRepFileLoc);
            }
            // S2 DATABASE
            if (NewCompareConfig.S2Type == "DATABASE")
            {
                string dbconstring = NewCompareConfig.S2ConnString;
                string dbsqlquery = NewCompareConfig.S2Query;
                
                dt = GetDataFromDatabase(NewCompareConfig.S2DBType, NewCompareConfig.S2ConnString, NewCompareConfig.S2Query);

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
                comparesourcedocs[1] = Conversion.CsvToDom(NewCompareConfig.S2FileLocation, NewCompareConfig.S2CSVDelim);
            }

            // S2 REPORT
            if (NewCompareConfig.S2Type == "REPORT")
            {
                comparesourcedocs[1] = Conversion.ReportToDom(NewCompareConfig.S2FileLocation, 2, NewCompareConfig.S2OpicsRepFileLoc);
            }

            // ********************************************Configuring Output**********************************************
            string excelfilelocation = NewCompareConfig.OFileLocation;

            // ********************************************Executing Compare***********************************************
            bool isOk = true;
            string strError = "";
            if (error == null)
            {
                error = new DataCompareError();
            }
            XmlCompareConfig xcc = initCompareConfig(NewCompareConfig,ref isOk,error);
            strError = error == null ? "" : error.Message;
            if (!isOk)
            {
                Logger.Error("ExecuteCompareProgram", strError);
                return null;
            }
            XmlCompareProcessorOld xcp = new XmlCompareProcessorOld(comparesourcedocs[0], comparesourcedocs[1], xcc);
            XmlCompareResult xcr = xcp.ProcessCompare();
            
            //get datatable containing diff data
            DataTable difftable = new DataTable();
            difftable = xcp.GetDiffTable();
            //end of diff file code
           
            if (xcr == null)
                return null;
            XmlCompareReportConfig xcrc = initXmlCompareReportConfig();
            XmlCompareReport xcrpt = new XmlCompareReport(xcr, xcrc, difftable, xcp, xcc);

            //excelfilelocation = excelfilelocation.Replace(".xlsx", "");
            String timeStamp = DateTime.Now.ToString("yyMMddHHmmss");
            //xlfilepath = filePath + "\\MARS_DIR_Comp_" + timeStamp + ".xlsx";
            //xcrpt.ProcessReport(excelfilelocation + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string  resultfileName = xcrpt.ProcessReport(excelfilelocation +"\\MARS_Date_Comp_" + timeStamp);
            return resultfileName;
        }

        public static void EnrichDataTable(DataTable TargetDT, DataTable MapTable, bool enrichmentRequested, int mapColNum)
        {
            try
            {
                // TODO: make sure field name is not case sensative
                string mappedFieldName = MapTable.Columns[mapColNum].ColumnName.Split('_')[0];  // like tradeid
                string linkingFieldName = MapTable.Columns[0].ColumnName;               // like exttradeid

                if (enrichmentRequested && 
                    TargetDT.Columns.Contains(mappedFieldName) &&
                    TargetDT.Columns.Contains(linkingFieldName) == false)
                {
                     // Add Column to target tables
                    TargetDT.Columns.Add(linkingFieldName, typeof(String));

                    // Populate new column in target tables with values from Mapping table
                    TargetDT.Columns[linkingFieldName].DefaultValue = "";
                    
                    // create dicts
                    Dictionary<string, string> dict = DataTableToDict(MapTable, mapColNum);

                    for (int i = 0; i < TargetDT.Rows.Count; i++)
                    {
                        if (dict.Keys.Contains(TargetDT.Rows[i][mappedFieldName]))
                            TargetDT.Rows[i][linkingFieldName] = dict[TargetDT.Rows[i][mappedFieldName].ToString()];
                        else
                        {
                            string key = TargetDT.Rows[i][mappedFieldName].ToString();
                            //Console.WriteLine("Key " + key + " Is not in dictionary");
                        }
                           
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static Dictionary<string, string> DataTableToDict(DataTable dt, int colNum)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string key = dt.Rows[i][colNum].ToString();
                if (dict.Keys.Contains(key) == false)
                    dict.Add(key, dt.Rows[i][0].ToString());
            }


            return dict;
        }

        private static bool DataTableCompareImp(ComparewithID newCompareConfig)
        {
            return true;

            //if (newCompareConfig.S1Type.Equals("XML") == false &&
            //    newCompareConfig.S2Type.Equals("XML") == false)
            //    return true;
            //else
            //    return false;
        }

        ///New
        ///
        public static Boolean USE_WIN_MERGE = true;

        public static DataTable MappingTable { get; set; }
        
        public static Mars.MarsConfig.MarsConfig mc { get; set; }

        public static string GetDataTableColumnNames(DataTable dataTable)
        {
            if (dataTable == null || dataTable.Columns.Count == 0)
            {
                return string.Empty;
            }

            var columnNames = dataTable.Columns.Cast<DataColumn>()
                                               .Select(column => column.ColumnName)
                                               .ToArray();

            return string.Join(",", columnNames);
        }


        public static string ExecuteDataTableCompareProgram(ComparewithID NewCompareConfig, ref DataCompareError error, bool isFromWebCommand= false)
        {
            String timeStamp = DateTime.Now.ToString("yyMMddHHmmss");
            string resultfileName = NewCompareConfig.OFileLocation + @"\MARS_" + NewCompareConfig.CompareID + "_" + timeStamp + ".xlsx";
            if (error == null)
                error = new DataCompareError();
            try
            {
                if (!System.IO.Directory.Exists(NewCompareConfig.OFileLocation))
                {
                    DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(NewCompareConfig.OFileLocation));
                    bool isNetworkDrive = driveInfo.DriveType == DriveType.Network;
                    if (!isNetworkDrive)
                        System.IO.Directory.CreateDirectory(NewCompareConfig.OFileLocation);
                }
            }catch(Exception ex)
            {
                Logger.Error("ExecuteDataTableCompareProgram", 
                    error.Message = $"can't access the folder|{NewCompareConfig.OFileLocation}|{ex.Message}",
                    ex);
                error.Status = false;

                return null;
            }
            if (isFromWebCommand)
            {
                // do not change the file name
            }
            else
            {
            if (NewCompareConfig.OfileName != null && NewCompareConfig.OfileName.Length > 6)
                resultfileName = NewCompareConfig.OfileName;
            }
            // Text compare has no fields, is not tabular, so jus do the compare and leave
            if (NewCompareConfig.S1Type.Equals("TEXT"))
            {
                string file1 = NewCompareConfig.S1FileLocation;
                string file2 = NewCompareConfig.S2FileLocation;

                string outputFile = NewCompareConfig.OFileLocation;
                if (USE_WIN_MERGE)
                {
                    string winMergePath = "";
/*
                    Configuration currentExeCfg = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                   
                    if (currentExeCfg.AppSettings.Settings["WinMergePath"] != null)
                    {
                        winMergePath = currentExeCfg.AppSettings.Settings["WinMergePath"].Value;
                    }
*/
					winMergePath = mc.AppSettings["WinMergePath"];

                    resultfileName = resultfileName.Replace(".xlsx", ".html");
                    resultfileName = resultfileName.Replace(".txt", ".html");
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    //startInfo.FileName = @"C:\Users\Alex\Downloads\winmerge-2.16.0-exe\WinMerge\WinMergeU.exe";
                    startInfo.FileName = winMergePath;
                    startInfo.Arguments = file1 + " " + file2 + "  -minimize -noninteractive -u -or " + resultfileName;
                    Process.Start(startInfo);
                }
                else
                {
                    TextFileCompare tfc = new TextFileCompare();
                    tfc.Compare(file1, file2, resultfileName);
                    System.Diagnostics.Process.Start(outputFile);

                    var xlAppTxtCmp = new Excel.Application();
                    xlAppTxtCmp.Visible = true;

                    xlAppTxtCmp.Workbooks.Open(@resultfileName);
                }
               
                error.Status = true;
                return resultfileName;
            }
            
            DataTable dt1 = null;
            DataTable dt2 = null;

            // ********************************************Configuring Input***********************************************
            // S1 DATABASE
            switch (NewCompareConfig.S1Type)
            {
                case "DATABASE":
                string dbconstring = NewCompareConfig.S1ConnString;
                string dbsqlquery = NewCompareConfig.S1Query;
                dt1 = GetDataFromDatabase(NewCompareConfig.S1DBType, NewCompareConfig.S1ConnString, NewCompareConfig.S1Query);
                    break;
                case "XML":
                    dt1 = Conversion.ConvertXMLtoDataTable(NewCompareConfig.S1FileLocation,NewCompareConfig.S1XMlIndex);
                if (ExecuteCompare.MappingTable != null)
                    EnrichDataTable(dt1, ExecuteCompare.MappingTable, true, 1);
                    break;
                case "CSV":
                    // dt1 = Conversion.ConvertCSVtoDataTable(NewCompareConfig.S1FileLocation);
                dt1 = Conversion.ConvertCSVtoDataTableWithRegEx(NewCompareConfig.S1FileLocation, NewCompareConfig.S1CSVDelim);
                    break;
                case "EXCEL":
                dt1 = Conversion.ConvertExcelToDataTable(NewCompareConfig.S1FileLocation);
                //Modify compare config for pivot table reports
               // if (NewCompareConfig.S1FileLocation.Contains("BSPL"))
               if (IsDynamicExcelFile(NewCompareConfig.S1FileLocation))
                {
                    var columnNames = (from DataColumn dc in dt1.Columns select dc.ColumnName).ToList();
                    // Key field is the firs field
                    NewCompareConfig.KeyFields = columnNames.First();
                
                    // Show Fields -- all fields
                    string str = string.Empty;
                    foreach (var item in columnNames)
                        str = str + item + ",";
                    str = str.Remove(str.Length - 1);
                    NewCompareConfig.ShowFields = str;

                    // Compare fields -- all fields except for the first one
                    str = "==";
                    foreach (var item in columnNames)
                        str = str + item + "|1||;";
                    str = str.Remove(str.Length - 1);
                    NewCompareConfig.CompareFields = str;
                }
                if (ExecuteCompare.MappingTable != null)
                    EnrichDataTable(dt1, ExecuteCompare.MappingTable, true, 1);
                    // if the keys were null, set them to the first column
                    if (NewCompareConfig.KeyFields == null || NewCompareConfig.KeyFields.Length == 0)
                    {
                        NewCompareConfig.KeyFields = GetDataTableColumnNames(dt1);
            }
                    if (NewCompareConfig.CompareFields == null || NewCompareConfig.CompareFields.Length == 0)
                    {
                        NewCompareConfig.CompareFields = GetDataTableColumnNames(dt1);
                    }
                    if (NewCompareConfig.ShowFields == null || NewCompareConfig.ShowFields.Length == 0)
                    {
                        NewCompareConfig.ShowFields = GetDataTableColumnNames(dt1);
                    }
                    if (NewCompareConfig.ColumnFields == null || NewCompareConfig.ColumnFields.Length == 0)
                    {
                        NewCompareConfig.ColumnFields = GetDataTableColumnNames(dt1);
                    }                    
                    break;
                case "REPORT":
                dt1 = Conversion.ConvertReportToDataTable(NewCompareConfig.S1FileLocation, 1, NewCompareConfig.S1OpicsRepFileLoc);
                    break;
                case "SWIFT":
                dt1 = Conversion.ConvertSWIFTToDataTable(NewCompareConfig.S1FileLocation);
                    break ;                
            }
            Logger.Info("ExecuteDataTableCompareProgram", $"load the second data|{NewCompareConfig.S2Type}");
            switch (NewCompareConfig.S2Type)
            {
                case "DATABASE":
                string dbconstring = NewCompareConfig.S2ConnString;
                string dbsqlquery = NewCompareConfig.S2Query;

                dt2 = GetDataFromDatabase(NewCompareConfig.S2DBType, NewCompareConfig.S2ConnString, NewCompareConfig.S2Query);
                    break;
                case "XML":
                    dt2 = Conversion.ConvertXMLtoDataTable(NewCompareConfig.S2FileLocation,NewCompareConfig.S2XMlIndex);
                if (ExecuteCompare.MappingTable != null)
                    EnrichDataTable(dt2, ExecuteCompare.MappingTable, true, 2);
                    break;
                case "CSV":
                dt2 = Conversion.ConvertCSVtoDataTableWithRegEx(NewCompareConfig.S2FileLocation, NewCompareConfig.S2CSVDelim);
                    break;
                case "EXCEL":
            // S2 Excel
            if (NewCompareConfig.S2Type == "EXCEL")
            {
                dt2 = Conversion.ConvertExcelToDataTable(NewCompareConfig.S2FileLocation);
                if (ExecuteCompare.MappingTable != null)
                    EnrichDataTable(dt2, ExecuteCompare.MappingTable, true, 2);
            }
                    if (NewCompareConfig.KeyFields == null || NewCompareConfig.KeyFields.Length == 0)
                    {
                        NewCompareConfig.KeyFields = GetDataTableColumnNames(dt2);
                    }
                    if (NewCompareConfig.CompareFields == null || NewCompareConfig.CompareFields.Length == 0)
                    {
                        NewCompareConfig.CompareFields = GetDataTableColumnNames(dt2);
                    }
                    if (NewCompareConfig.ShowFields == null || NewCompareConfig.ShowFields.Length == 0)
                    {
                        NewCompareConfig.ShowFields = GetDataTableColumnNames(dt2);
                    }
                    if (NewCompareConfig.ColumnFields == null || NewCompareConfig.ColumnFields.Length == 0)
                    {
                        NewCompareConfig.ColumnFields = GetDataTableColumnNames(dt2);
                    }
                    break;
                case "REPORT":                    
                dt2 = Conversion.ConvertReportToDataTable(NewCompareConfig.S2FileLocation, 2, NewCompareConfig.S2OpicsRepFileLoc);
                    break;
                case "SWIFT":
                dt2 = Conversion.ConvertSWIFTToDataTable(NewCompareConfig.S2FileLocation);
                    break;
            }

            // ********************************************Configuring Output**********************************************
            string excelfilelocation = NewCompareConfig.OFileLocation;

            string strError = "";
            
            // ********************************************Executing Compare***********************************************
            XmlCompareConfig xcc = initCompareConfig(NewCompareConfig, ref error.Status, error);
 
            List<string> allFieldNames = new List<string>();
            Logger.Info("ExecuteDataTableCompareProgram", $"all field Name|{allFieldNames}");
            foreach(DataColumn col in dt1.Columns)
            {
                allFieldNames.Add(col.ColumnName);
            }

            string templateFile = mc.AppSettings["DataCompareTemplate"];
            DTCompare dtc = new DTCompare(dt1, dt2, allFieldNames.ToArray(), 
                xcc.KeyFields.ToArray(), 
                xcc.ShowFields.ToArray(), 
                xcc.CompareFields.ToArray(), 
                xcc.TolerancMap, 
                resultfileName);
            dtc.applyFilter = NewCompareConfig.OutputFilterApply;
            dtc.outputFilter = NewCompareConfig.OutputFilter;
            dtc.orderBy = NewCompareConfig.OutputOrderBy;
            GenerateDTCompareConfig(dtc, NewCompareConfig);
            dtc.templateFile = templateFile;


            error = new DataCompareError();
            dtc.Compare(out error.Status, out error.Message);

            if (!isFromWebCommand)
            {
                // Open excel
                if (NewCompareConfig.InteractiveMode == true)
                {
                    var xlApp = new Excel.Application();
                    xlApp.Visible = true;

                    xlApp.Workbooks.Open(@resultfileName);
                }
            }
            return resultfileName;
        }

        private static void GenerateDTCompareConfig(DTCompare dtc, ComparewithID newCompareConfig)
        {
            string Client = "";
            string InstanceVersionBaseline = "";
            string InstanceVersionTarget = "";
            string InstanceNameBaseline = "";
            string InstanceNameTarget = "";
            string ConnectionURL = "";
            string ExecutionHost = "";
            string ExTime = "";

            if (mc.AppSettings.Keys.Contains("Client"))
                Client = mc.AppSettings["Client"];

            if (mc.AppSettings.Keys.Contains("InstanceVersionBaseline"))
                InstanceVersionBaseline = mc.AppSettings["InstanceVersionBaseline"];
            
            if (mc.AppSettings.Keys.Contains("InstanceVersionTarget"))
                InstanceVersionTarget = mc.AppSettings["InstanceVersionTarget"];

            if (mc.AppSettings.Keys.Contains("InstanceNameBaseline"))
                InstanceNameBaseline = mc.AppSettings["InstanceNameBaseline"];

            if (mc.AppSettings.Keys.Contains("InstanceNameTarget"))
                InstanceNameTarget = mc.AppSettings["InstanceNameTarget"];


            ConnectionURL = mc.GetApiUrl();
            ExecutionHost = System.Environment.MachineName;
            ExTime = DateTime.Now.ToString("EST yyyy-MM-dd  HH:mm:ss");

            dtc.compareConfig.ConfigID = newCompareConfig.CompareID;
            
            dtc.compareConfig.Client = Client;
            dtc.compareConfig.ConnectionURL = ConnectionURL;
                     
            dtc.compareConfig.ExecutionHost = ExecutionHost;
            
            dtc.compareConfig.ExTime = ExTime;
            /*
            dtc.compareConfig.Status = newCompareConfig;
            dtc.compareConfig.ReportFileLocation = newCompareConfig;
            */

            dtc.compareConfig.InstanceVersionBaseline = InstanceVersionBaseline;
            dtc.compareConfig.InstanceNameBaseline = InstanceNameBaseline;
            
            dtc.compareConfig.CompareTypeBaseline = newCompareConfig.S1Type;
            dtc.compareConfig.DBConnectionNameBaseline = newCompareConfig.S1DBConn;

            if (newCompareConfig.S1ConnString.Contains("User Id"))
                dtc.compareConfig.DBConnectionDetailsBaseline = newCompareConfig.S1ConnString.Substring(0, newCompareConfig.S1ConnString.IndexOf("User Id"));
            //dtc.compareConfig.DBConnectionDetailsBaseline = newCompareConfig.S1ConnString.Split(';')[0];


            dtc.compareConfig.QueryIdBaseline = newCompareConfig.S1QueryID;
            dtc.compareConfig.QueryBaseline = newCompareConfig.S1Query;
            
            dtc.compareConfig.FileLocationBaseline = newCompareConfig.S1FileLocation;
            
            dtc.compareConfig.InstanceVersionTarget = InstanceVersionTarget;
            dtc.compareConfig.InstanceNameTarget = InstanceNameTarget;
            
            dtc.compareConfig.CompareTypeTarget = newCompareConfig.S2Type;
            dtc.compareConfig.DBConnectionNameTarget = newCompareConfig.S2DBConn;
            if (newCompareConfig.S2ConnString.Contains("User Id"))
                dtc.compareConfig.DBConnectionDetailsTarget = newCompareConfig.S2ConnString.Substring(0, newCompareConfig.S1ConnString.IndexOf("User Id"));

            dtc.compareConfig.QueryIdTarget = newCompareConfig.S2QueryID;
            dtc.compareConfig.QueryTarget = newCompareConfig.S2Query;
            
            dtc.compareConfig.FileLocationTarget = newCompareConfig.S2FileLocation;
            dtc.compareConfig.KeyFields = newCompareConfig.KeyFields;
            dtc.compareConfig.ShowFields = newCompareConfig.ShowFields;
            dtc.compareConfig.CompareFields = newCompareConfig.CompareFields.Replace("|1|", "").Replace("|", "").Replace(";", ", ").Replace("==", "");

        }

        public static Boolean IsDynamicExcelFile(string fileName)
        {
            Boolean rc = false;

            if (fileName.Contains("BSPL") ||
                fileName.Contains("Extract_Midas_Daily_Postings_PL_Daily_Excel") ||
                fileName.Contains("Extract_Midas_Daily_Postings_PL_MTD_Excel") ||
                fileName.Contains("Extract_Midas_Daily_Postings_PL_Strategy_Daily_Excel") ||
                fileName.Contains("Extract_Midas_Daily_Postings_PL_Strategy_MTD_Excel") ||
                fileName.Contains("Extract_Midas_Daily_Postings_PL_YTD_Excel") ||

                fileName.Contains("Extract_Midas_Daily_Postings_PL_Strategy_YTD_CSV")
               )

                rc = true;

            return rc;

        }
        ///New end

        public static DataTable GetDataFromDatabase(string dbType, string connString, string query)
        {
            Logger.logBegin("GetDataFromDatabase", $"going to get data from|{dbType}|{connString}\r\n{query}");
            DataTable dt = new DataTable();

            switch (dbType)
            {
                case "Oracle":
                    Console.WriteLine(connString);
                    Logger.Info("GetDataFromDatabase", "Oracle Branch");
                    using (OracleConnection sqlConnection = new OracleConnection(connString))
                    {
                        OracleCommand command = new OracleCommand(query, sqlConnection);
                        OracleDataAdapter adapter = new OracleDataAdapter(command);
                        adapter.SelectCommand.CommandTimeout = 300;
                        OracleCommandBuilder builder = new OracleCommandBuilder(adapter);
                        
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        dt = ds.Tables[0];
                        Console.WriteLine("\ndbSource ready for access");
                        Logger.Info("GetDataFromDatabase", $"Oracle Branch, have loaded to data table|{dt.Rows?.Count}");
                    }
                    break;

                case "SQL Server":
                    Logger.Info("GetDataFromDatabase", "Sql server Branch");
                    using (SqlConnection sqlConnection = new SqlConnection(connString))
                    {
                        SqlCommand command = new SqlCommand(query, sqlConnection);
                        SqlDataAdapter adapter = new SqlDataAdapter(command);
                        adapter.SelectCommand.CommandTimeout = 300;
                        SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                        DataSet ds = new DataSet();
                        adapter.Fill(ds);
                        dt = ds.Tables[0];
                        CapitalizeDTColumns(dt);
                        Console.WriteLine("\ndbSource ready for access");
                        Logger.Info("GetDataFromDatabase", $"Sql Branch, have loaded to data table|{dt.Rows?.Count}");
                    }
                    break;
            }

            return dt;
        }

        private static void CapitalizeDTColumns(DataTable dt)
        {
            foreach (DataColumn col in dt.Columns)
            {
                col.ColumnName = col.ColumnName.ToUpper();
            }
        }

        public static XmlCompareReportConfig initXmlCompareReportConfig()
        {
            XmlCompareReportConfig config = new XmlCompareReportConfig();
            return config;
        }

    

        public static XmlCompareConfig initCompareConfig(ComparewithID NewCompareConfig, ref bool isOk, DataCompareError error)
        {
            Logger.logBegin("initCompareConfig", $"CompareId|{NewCompareConfig?.CompareID}");
            XmlCompareConfig config = new XmlCompareConfig();

            config.BlockTag = "rs:data";
            config.ElementTag = "z:row";

            // key fields
            string[] temp = Util.Split(NewCompareConfig.KeyFields, ',');
            Logger.Info("initCompareConfig", string.Format("Key fields|{0}", string.Join("|", temp)));
            config.KeyFields.AddRange(temp);
            for (int i = 0; i < config.KeyFields.Count; i++)
                config.KeyFields[i] = config.KeyFields[i].Trim();

            // compare fields
            string compareFields = ToleranceUtil.ExtractFields(NewCompareConfig.CompareFields);
            Logger.Info("initCompareConfig", compareFields);
            temp = Util.Split(compareFields, ',');
            config.CompareFields.AddRange(temp);
            for (int i = 0; i < config.CompareFields.Count; i++)
                config.CompareFields[i] = config.CompareFields[i].Trim();

            // Tolerance Fields
            string strError = "";
            Dictionary<string, ToleranceConfig> tMap = ToleranceUtil.GenerateTMap(NewCompareConfig.CompareFields, ref isOk, ref strError);
            if (!isOk)
            {
                if (error!=null)
                {
                    error.Status = false;
                    error.Message = strError;
                    return null;
                }
            }
            config.TolerancMap = tMap;

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
            temp = Util.Split(string.IsNullOrWhiteSpace(NewCompareConfig.RowFields)?"": NewCompareConfig.RowFields, ',');
            config.RowFields.AddRange(temp);
            for (int i = 0; i < config.RowFields.Count; i++)
                config.RowFields[i] = config.RowFields[i].Trim();
            
            // Column fields
            temp = Util.Split(string.IsNullOrWhiteSpace(NewCompareConfig.ColumnFields) ? "" : NewCompareConfig.ColumnFields, ',');
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

            Logger.logEnd("initCompareConfig");
            return config;
        }
    }
}
