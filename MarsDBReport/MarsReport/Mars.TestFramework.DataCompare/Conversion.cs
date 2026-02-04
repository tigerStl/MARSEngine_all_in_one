using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ClosedXML.Excel;
using Mars.TestFramework.DataCompare.Opics.Report;
using System.Text.RegularExpressions;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.TestFramework.DataCompare
{
    public class Conversion
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(Conversion));
        public static string delim = ",";
        public static string noHeaders="";
        public static Dictionary<string, string> OpicsRPTToFMTDict = null;


        public static XmlDocument CsvToDom(string path, string customDelim)
        {
            XmlDocument doc = null;

            //DataTable dt = CsvUtil.GetDataTableFromCsv(path, true);

            DataTable dt = ConvertCSVtoDataTableWithRegEx(path, customDelim);

            foreach (DataColumn column in dt.Columns)
            {
                //string newColName = column.ColumnName.Replace(" ", string.Empty);
                //string newColName = column.ColumnName.Replace(" ", string.Empty).Replace(")", string.Empty).Replace("(", string.Empty).Replace("#", string.Empty).Replace("/", string.Empty).Replace("*", string.Empty);
                string newColName = column.ColumnName.Replace(")", string.Empty).Replace("(", string.Empty).Replace("#", string.Empty).Replace("/", string.Empty).Replace("*", string.Empty);
                column.ColumnName = newColName;
            }

            doc = DataTableToDom(dt);
            return doc;
        }

        public static void PoulateOpicsRPTToFMTDict(string filePath)
        {
            OpicsRPTToFMTDict = new Dictionary<string, string>();
            StreamReader sr = new StreamReader(filePath);
            string line = string.Empty;
            string key = string.Empty;
            string value = string.Empty;

            while ((line = sr.ReadLine()) != null)
            {
                var strings = line.Split(',');
                key = strings[0];
                value = strings[1];

                if (key.Trim().Length != 0 && OpicsRPTToFMTDict.Keys.Contains(key) == false)
                    OpicsRPTToFMTDict.Add(key, value);
                else
                    Console.Write("Error");
            }
            sr.Close();

        }
            

        public static XmlDocument ExcelToDom(string path)
        {
            XmlDocument doc = null;

            //DataTable dt = CsvUtil.GetDataTableFromCsv(path, true);

            DataTable dt = ConvertExcelToDataTable(path);

            foreach (DataColumn column in dt.Columns)
            {
                //string newColName = column.ColumnName.Replace(" ", string.Empty);
                string newColName = column.ColumnName.Replace(" ", string.Empty).Replace(")", string.Empty).Replace("(", string.Empty).Replace("#", string.Empty).Replace("/", string.Empty).Replace("*", string.Empty);
                column.ColumnName = newColName;
            }

            doc = DataTableToDom(dt);
            return doc;
        }

        public static XmlDocument ReportToDom(string path, int sideNum, string fmtLocation)
        {
            XmlDocument doc = null;

            DataTable dt = ConvertReportToDataTable(path, sideNum, fmtLocation);

            foreach (DataColumn column in dt.Columns)
            {
                //string newColName = column.ColumnName.Replace(" ", string.Empty);
                string newColName = column.ColumnName.Replace(" ", string.Empty).Replace(")", string.Empty).Replace("(", string.Empty).Replace("#", string.Empty).Replace("/", string.Empty).Replace("*", string.Empty);
                column.ColumnName = newColName;
            }

            doc = DataTableToDom(dt);
            return doc;
        }

        static bool USE_FMT = true;

        public static DataTable ConvertReportToDataTable(string path, int sideNum, string fmtPath)
        {
            string inputFileName = path;
            DataTable dt;
            List<string> BreakdownHeaders = null;
            List<string> ReportHeaders = null;
            List<string> TotalHeaders = null;
            List<string> IgnoreList = null;
            List<int> ColBreak = null;

           // if (USE_FMT)
                return ConvertReportToDataTableUsingFmt(path, sideNum, fmtPath);

            /*
            // Summit reports for CIC

            if (path.Contains("BONDPL_HISTORY"))
            {
                BreakdownHeaders = new List<string> {  };

                ReportHeaders = new List<string> {  "Cusip",
                                                     "BOOK",
                                                     "SecID",
                                                     "TRADEID",
                                                     "CUST",
                                                     "BROKER",
                                                     "TRADEDATE",
                                                     "SETTLEDATE",
                                                     "NOTIONAL",
                                                     "PRICE",
                                                     "FACTOR_AT_SETTL",
                                                     "MARKET_VALUE_WITHOUT_ACCR"
                };

                TotalHeaders = new List<string> {  };



                IgnoreList = new List<string> { "-------",
                                                "       ",
                                                "rows affected",
                                               };
                ColBreak = new List<int> { 1,  };
            }

            // End for CIC

            if (path.Contains("RBACC"))
            {
                BreakdownHeaders = new List<string> {  "Cost Center",
                                                       "Currency Code" };

                ReportHeaders = new List<string> {  "GL Number",
                                                    "General Ledger Description",
                                                    "BE",
                                                    "CCY Amount",
                                                    "Base Amount"
                };

                TotalHeaders = new List<string> {  "CCY Amount",
                                                    "Base Amount"
                };



                IgnoreList = new List<string> { "*******",
                                              //  "       ",
                                                "Branch",
                                                "System",
                                              //  "Total",
                                                "GL Number"};
                ColBreak = new List<int> { 0, 10, 49, 52, 83, 120 };
            }

            if (path.Contains("RFIAI"))
            {
                BreakdownHeaders = new List<string> {  "Cost Center",
                                                       "General Ledger Number",
                                                       "CCY"};

                ReportHeaders = new List<string> {  "Security ID",
                                                    "Port",
                                                    "Accrued Outstanding"
                };

                TotalHeaders = new List<string> {  "Accrued Outstanding"
                };

                IgnoreList = new List<string> { "*******",
                                             //   "       ",
                                                "Branch",
                                                "System",
                                             //   "Total",
                                                "Security ID"};
                ColBreak = new List<int> { 0, 21, 27, 57 };
            }

            else if (path.Contains("RGLAC"))
            {
               
                BreakdownHeaders = new List<string> { "GL Account",
                                                       "Cost Center",
                                                       "Description",
                                                       "Currency",
                                                       "BE Ind." };

                ReportHeaders = new List<string> {  "Effective Date",
                                                    "Prod. Code",
                                                    "Type",
                                                    "Deal No.",
                                                    "Seq.",
                                                    "Customer",
                                                    "Debit Amount",
                                                    "Credit Amount" };

                TotalHeaders = new List<string> {  "Debit Amount",
                                                   "Credit Amount"
                };

                IgnoreList = new List<string> { "*******",
                                              //  "       ",
                                                "Branch",
                                                "System",
                                              //  "Total",
                                                "  Effective"};
                ColBreak = new List<int> { 0, 17, 29, 39, 50, 55, 65, 100, 160 };
            }

            ReportToDt reportToDt = new ReportToDt();
            dt = reportToDt.ConvertToDT(inputFileName, BreakdownHeaders, ReportHeaders, TotalHeaders, IgnoreList, ColBreak);

            return dt;
            */
        }

        public static DataTable ConvertReportToDataTableUsingFmt(string path , int sideNum, string fmtPathUser)
        {
            string inputFileName = path;
            DataTable dt;
            List<string> BreakdownHeaders = null;
            List<string> ReportHeaders = null;
            List<string> TotalHeaders = null;
            List<string> IgnoreList = null;
            List<int> ColBreak = null;
            OpicsReportFormatRaw RawFmt = null;

            string fmpPath = fmtPathUser;

            if (fmpPath == null || fmpPath.Length < 5)
                fmpPath = GetFmtPath(path, sideNum);

            // Get format data from fmt file
            OpicsReportFormat fmt = new OpicsReportFormat();
            fmt.Init(fmpPath);

            BreakdownHeaders = fmt.ParsedFmt.BreakdownHeaders;
            ReportHeaders = fmt.ParsedFmt.ReportHeaders;
            TotalHeaders = fmt.ParsedFmt.TotalHeaders;
            IgnoreList = fmt.ParsedFmt.IgnoreList;
            ColBreak = fmt.ParsedFmt.ColBreak;
            RawFmt = fmt.RawFmt;

           ReportToDt reportToDt = new ReportToDt();
            dt = reportToDt.ConvertToDT(inputFileName, BreakdownHeaders, ReportHeaders, TotalHeaders, IgnoreList, ColBreak, RawFmt);

            return dt;
        }

        public static bool IsCharDigit(char c)
        {
            return ((c >= '0') && (c <= '9'));
        }

        private static string GetFmtPathByFileMap(string path, int sideNum)
        {
            Logger.Info("GetFmtPathByFileMap", "path:" + path + " sideNum:" + sideNum);
            var appConfig = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Logger.Info("GetFmtPathByFileMap", "fmtFolderPath");
            string fmtFolderPath = appConfig.AppSettings.Settings["OpicsFMTPath" + sideNum].Value;
            Logger.Info("GetFmtPathByFileMap", "rptToFmtMapFile");
            string rptToFmtMapFile = appConfig.AppSettings.Settings["RptToFmtMapFile"].Value;

            if (rptToFmtMapFile == null || File.Exists(rptToFmtMapFile) == false)
            {
                throw new System.ArgumentException("Format file not found", fmtFolderPath);
            }

            Logger.Info("GetFmtPathByFileMap", "OpicsRPTToFMTDict BEFORE");
            if (OpicsRPTToFMTDict == null || OpicsRPTToFMTDict.Keys.Count == 0)
                PoulateOpicsRPTToFMTDict(rptToFmtMapFile);

            Logger.Info("GetFmtPathByFileMap", "OpicsRPTToFMTDict AFTER");
            string fileName = Path.GetFileNameWithoutExtension(path);
            string fmtFileName = "";
            string fmtFilePath = "";
            if (fileName[0] == 'R' &&
                IsCharDigit(fileName[1]) &&
                IsCharDigit(fileName[2]) )
            {
                string key = "R{0}" + fileName.Substring(3);
                if (OpicsRPTToFMTDict.Keys.Contains(key) == false)
                {
                    throw new System.ArgumentException("Format mapping not found in table", fmtFolderPath);
                }
                else
                    fmtFileName = OpicsRPTToFMTDict[key];

            }
            fmtFilePath = fmtFolderPath + @"\" + fmtFileName + ".fmt";


           
            // some file names need last char removed
            if (File.Exists(fmtFilePath) == false)
            {
                if (fmtFileName == null || fmtFileName.Trim().Length == 0)
                {
                    throw new System.ArgumentException("Format mapping not found in table", fmtFolderPath);
                }
                else
                    fmtFilePath = fmtFolderPath + @"\" + fmtFileName.TrimEnd(fmtFileName[fmtFileName.Length - 1]) + ".fmt";
            }
                



            return fmtFilePath;
        }

        private static string GetFmtPath(string path, int sideNum)
        {
            var appConfig = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string fmtFolderPath = appConfig.AppSettings.Settings["OpicsFMTPath" + sideNum].Value;

            int idx = path.IndexOf(".rpt");

            string rootName = path.Substring(idx - 5, 4);
            string[] files = System.IO.Directory.GetFiles(fmtFolderPath, "*" + rootName + ".fmt");

            string fmtFilePath;

            if (files.Length == 0)
            {
                fmtFilePath = GetFmtPathByFileMap(path, sideNum);
               
                //throw new System.ArgumentException("Format file not found", fmtFolderPath);
            }
            else
                 fmtFilePath = files[0];
            
            if (File.Exists(fmtFilePath) == false)
                fmtFilePath = GetFmtPathByFileMap(path, sideNum);
                return fmtFilePath;
        }

        public static DataTable ConvertSWIFTToDataTable(string file)
        {
            string inputFileName = file;
            DataTable dt;

            SwiftFileToDt swiftToDt = new SwiftFileToDt();
            dt = swiftToDt.ConvertToDT(inputFileName);

            return dt;
        }

        public static XmlDocument DataTableToDom(DataTable dt)
        {
            string[] columnNames = dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray();

            XmlDocument doc = new XmlDocument();

            XmlNode rootNode = doc.CreateNode("element", "rs:data", "urn:schemas-microsoft-com:rowset");
            doc.AppendChild(rootNode);
            foreach (DataRow row in dt.Rows)
            {
                XmlNode node = doc.CreateNode("element", "z:row", "#RowsetSchema");
                rootNode.AppendChild(node);
                foreach (string colName in columnNames)
                {
                    // Console.WriteLine("colName " + colName);
                    ((XmlElement)node).SetAttribute(colName, row[colName].ToString());
                }
            }

            //Console.WriteLine(doc.OuterXml);
            return doc;
        }

        public static DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            Console.WriteLine("CSV: " + strFilePath);

            HashSet<string> names = new HashSet<string>();

            if (delim == null)
                delim = ";";

            StreamReader sr = new StreamReader(strFilePath);

            string headerLine = sr.ReadLine();

            string[] headers = headerLine.Split(delim[0]);

            // Handle sheet that has no headers -- create headers on the fly
            if (noHeaders != null && noHeaders.Equals("TRUE"))
            {
                for (int i = 0; i < headers.Length; i++)
                    headers[i] = "FIELD_" + i;
            }


            // handle unnamed fields
            for (int i = 0; i < headers.Length; i++)
            {
                // handle duplicate fields
                if (names.Contains(headers[i]))
                    headers[i] = headers[i] + i;

                if (headers[i] == null || headers[i].Trim().Length == 0)
                    headers[i] = "FIELD_" + i;

                names.Add(headers[i]);
            }

            DataTable dt = new DataTable();
            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            while (!sr.EndOfStream)
            {
                string[] rows = sr.ReadLine().Split(delim[0]);
                DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    dr[i] = rows[i];
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        public static DataTable ConvertCSVtoDataTableWithRegEx(string strFilePath, string customDelim)
        {
            Console.WriteLine("CSV: " + strFilePath);

            HashSet<string> names = new HashSet<string>();

            if (customDelim == null || customDelim.Length == 0)
                delim = ",";

            else
                delim = customDelim;

            StreamReader sr = new StreamReader(strFilePath);

            string headerLine = sr.ReadLine();

            Regex CSVParser = new Regex(delim + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            // string[] headers = headerLine.Split(delim[0]);
            String[] headers = CSVParser.Split(headerLine);
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = headers[i].TrimStart(' ', '"');
                headers[i] = headers[i].TrimEnd('"').Trim();
            }


            // Handle sheet that has no headers -- create headers on the fly
            if (noHeaders != null && noHeaders.Equals("TRUE"))
            {
                for (int i = 0; i < headers.Length; i++)
                    headers[i] = "FIELD_" + i;
            }


            // handle unnamed fields
            for (int i = 0; i < headers.Length; i++)
            {
                // handle duplicate fields
                if (names.Contains(headers[i]))
                    headers[i] = headers[i] + i;

                if (headers[i] == null || headers[i].Trim().Length == 0)
                    headers[i] = "FIELD_" + i;

                names.Add(headers[i]);
            }

            DataTable dt = new DataTable();
            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            while (!sr.EndOfStream)
            {
                //string[] rows = sr.ReadLine().Split(delim[0]);
                String[] rows = CSVParser.Split(sr.ReadLine());
                if (rows.Length > 0 && rows[0].Contains("--"))
                    continue;

                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = rows[i].TrimStart(' ', '"');
                    rows[i] = rows[i].TrimEnd('"');
                }

                DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                {
                    if (rows[i] == null || rows[i].Length == 0)
                        dr[i] = " ";
                    else
                        dr[i] = rows[i];
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        public static DataTable ConvertXMLtoDataTable(string fileLocation,string selectIndex="1")
        {
            int index = 0;
            if (!string.IsNullOrEmpty(selectIndex))
            {
                if (!int.TryParse(selectIndex, out index))
                {
                    index = 0;
                }
            }
            DataTable dt = new DataTable();
            XmlDocument doc = new XmlDocument();
           
            // Read XML file
            doc.Load(fileLocation);
            // Extract field names from Dom

            List<string> headers = GetHeaders(doc,index);
            // Create columns in Data Table
            foreach (string header in headers)
            {
                dt.Columns.Add(header);
            }
            // Populate Data Table
            PopulateDTwithXMLdata(dt, doc, headers,index);
            return dt;
        }

        private static void PopulateDTwithXMLdata(DataTable dt, XmlDocument doc, List<string> headers,int index)
        {
            string blockTag = "rs:data";
            string elementTag = "z:row";
            XmlNamespaceManager xmlnsManager;
            xmlnsManager = new XmlNamespaceManager(doc.NameTable);

            xmlnsManager.AddNamespace("s", "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882");
            xmlnsManager.AddNamespace("dt", "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882");
            xmlnsManager.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            xmlnsManager.AddNamespace("z", "#RowsetSchema");
            if (index == 0)
            {
            XmlNode dataNode = doc.SelectSingleNode("//" + blockTag, xmlnsManager);

            foreach (XmlNode rowNode in dataNode.SelectNodes("./" + elementTag, xmlnsManager))
            {
                DataRow dr = dt.NewRow();

                foreach (XmlAttribute attr in rowNode.Attributes)
                {
                    string tag = attr.Name;
                    if (dt.Columns.Contains(tag) == false)
                        dt.Columns.Add(tag);

                    string data = attr.Value;
                    try
                    {
                        dr[tag] = data;
                    }
                    catch (Exception e)
                    {

                    };
                }
                dt.Rows.Add(dr);
            }
        }
            else
            {
                var nodeList = doc.DocumentElement.SelectNodes("xml", xmlnsManager);
                if (nodeList != null && nodeList.Count >= index)
                {
                    XmlNode selectNode = nodeList[index - 1];
                    XmlNode dataNode = selectNode.SelectSingleNode(blockTag, xmlnsManager);
                    var nodes = dataNode.SelectNodes(elementTag, xmlnsManager);
                    foreach (XmlNode rowNode in nodes)
                    {
                        DataRow dr = dt.NewRow();

                        foreach (XmlAttribute attr in rowNode.Attributes)
        {
                            string tag = attr.Name;
                            if (dt.Columns.Contains(tag) == false)
                                dt.Columns.Add(tag);

                            string data = attr.Value;
                            try
                            {
                                dr[tag] = data;
                            }
                            catch (Exception e)
                            {

                            }
                        }
                        dt.Rows.Add(dr);
                    }
                }
            }
        }

        private static List<string> GetHeaders(XmlDocument doc,int index)
        {
            List<string> headers = new List<string>();
            string blockTag = "rs:data";
            string elementTag = "z:row";

            XmlNamespaceManager xmlnsManager;
            xmlnsManager = new XmlNamespaceManager(doc.NameTable);

            xmlnsManager.AddNamespace("s", "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882");
            xmlnsManager.AddNamespace("dt", "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882");
            xmlnsManager.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            xmlnsManager.AddNamespace("z", "#RowsetSchema");

            if (index == 0)
            {
            XmlNode dataNode = doc.SelectSingleNode("//" + blockTag, xmlnsManager);

            XmlNode rowNode = dataNode.SelectSingleNode("./" + elementTag, xmlnsManager);

            if (rowNode == null)
                throw new NoDataInXmlFileException(" FILE CONTAINS NO USABLE DATA");

            foreach (XmlAttribute attr in rowNode.Attributes)
            {
                headers.Add(attr.Name);
            }

            return headers;
        }
            else
            {
                var nodeList = doc.DocumentElement.SelectNodes("xml", xmlnsManager);
                if (nodeList != null && nodeList.Count >= index)
                {
                    XmlNode selectNode = nodeList[index - 1];

                    XmlNode dataNode = selectNode.SelectSingleNode(blockTag, xmlnsManager);

                    XmlNode rowNode = dataNode?.SelectSingleNode(elementTag, xmlnsManager);

                    if (rowNode == null)
                        throw new NoDataInXmlFileException(" FILE CONTAINS NO USABLE DATA");

                    foreach (XmlAttribute attr in rowNode.Attributes)
                    {
                        headers.Add(attr.Name);
                    }

                    return headers;
                }
                else
                {
                    throw new Exception(" Select XML index incorrect.");
                }
            }
        }

        class NoDataInXmlFileException : Exception
        {
            public NoDataInXmlFileException(string message)
            {

            }

        }

        public static DataTable ConvertExcelToDataTable(string fileName)
        {
            DataTable dataTable = ImportExcel(fileName);
            
            // This code is used for LoanIQ demo only, could be modified to handle structured EXCEL files with summaries
            // dataTable = CustomizeDT(dataTable);

            return dataTable;
        }
        

        public static Boolean IsExtractMidas( string fileName)
        {
            Boolean rc = false;


            rc = new[] { "Extract_Midas_Daily_Postings_PL_Daily_Excel",
                "Extract_Midas_Daily_Postings_PL_MTD_Excel",
                "Extract_Midas_Daily_Postings_PL_Strategy_Daily_Excel",
                "Extract_Midas_Daily_Postings_PL_Strategy_MTD_Excel",
                "Extract_Midas_Daily_Postings_PL_YTD_Excel"}.Any(c => fileName.Contains(c));

            return rc;

        }

        static DataTable ImportExcel(string filePath)
        {
            //Create a new DataTable.
            DataTable dt = new DataTable();

            //Convert XLS to XLSX if needed
            if (filePath.ToLower().EndsWith("xls")  || filePath.ToLower().EndsWith("csv"))
            {
                ExcelUtil.WorkbookXLStoXLSX(filePath);
                //filePath += 'x';
                filePath = Path.ChangeExtension(filePath, ".xlsx");
            }

            //Open the Excel file using ClosedXML.
            using (XLWorkbook workBook = new XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                IXLWorksheet workSheet = workBook.Worksheet(1);

                //Loop through the Worksheet rows.
                bool firstRow = true;

                int dynamicHeaderRow = 0;

                Dictionary<string, int> columnNames = new Dictionary<string, int>();

                // Enrich headers
                if (filePath.Contains("BSPL_Balances"))
                {

                    dynamicHeaderRow = -1;
                    foreach (IXLRow row in workSheet.Rows())
                    {

                        if (row.Cells().First().Value.ToString().Contains("As of"))
                        {
                            string asOfString = row.Cells().First().Value.ToString();
                            // if date is missing, the rest of spreadsheet is useless/empty
                            if (asOfString.Trim().Length < 6)
                                return dt;

                            if (filePath.Contains("BSPL_Balances_allall_Excel")  || filePath.Contains("BSPL_Balances_ProfCent_allall_Excel"))
                                dynamicHeaderRow = row.RowNumber() - 1;
                            else
                                dynamicHeaderRow = row.RowNumber();
                            break;
                        }
                    }

                    // if "As of" string was not found, the sheet is empty
                    if (dynamicHeaderRow == -1)
                        return dt;

                }
                else if (IsExtractMidas(filePath))
                {
                    dynamicHeaderRow = 11;
                }

                int rowCount = 0;

                //var firstFileRow = workSheet.Row(1);  // AF testing
                //var val1 = firstFileRow.Cell(1);

                foreach (IXLRow row in workSheet.Rows())
                {
                    rowCount++;
                    // Handle CIC BSPL_Ballances report format
                    //if (firstRow && filePath.Contains("BSPL_Balances") && rowCount < bsplHeaderRow)
                    if (firstRow &&
                        (filePath.Contains("BSPL_Balances") || IsExtractMidas(filePath)) && row.RowNumber() < dynamicHeaderRow)
                    {
                        continue;
                    }


                    // Skip rows before header row
                    // debugging 
                    var cells = row.Cells();
                    int count = cells.Count();
                    if (firstRow && count <= 2)
                        continue;

                    //Use the first row to add columns to DataTable.
                    if (firstRow)
                    {
                        if (filePath.Contains("BSPL_Balances"))
                        {
                            row.Cell(1).Value = "ACCOUNT";
                            row.Cell(2).Value = "NAME";
                        }
                        else if (IsExtractMidas(filePath))
                        {
                            row.Cell(1).Value = "ACCOUNT";
                            row.Cell(2).Value = "NAME";
                            row.Cell(3).Value = "NUM1";
                        }


                            foreach (IXLCell cell in row.Cells())
                        {
                            // account for duplicate headers. append counter to repeating headers
                            string header = cell.Value.ToString().Trim();
 
                            if (columnNames.Keys.Contains(header))
                            {
                                int counter = columnNames[header];
                                columnNames[header] = ++counter;
                                header = header + " " + counter;
                            }
                            else
                            {
                                columnNames.Add(header, 1);
                            }
                            
                            dt.Columns.Add(header);
                        }
                        firstRow = false;
                    }
                    else
                    {
                        //Add rows to DataTable.
                        dt.Rows.Add();

                        // Inint every cell to empty string
                        for (int col = 0; col < dt.Columns.Count; col++)
                            dt.Rows[dt.Rows.Count - 1][col] = "";

                        string data;
                       
                        foreach (IXLCell cell in row.Cells())
                        {
                            /*  We do not need to show formulas -- always show the value instead
                            if (cell.FormulaA1 != null && cell.FormulaA1.Trim().Length != 0)
                                data = cell.FormulaA1;
                            else
                            */
                           
                            if (cell.HasFormula == true)
                                data = cell.CachedValue.ToString().Trim();
                            else
                                data = cell.Value.ToString().Trim();
                            try
                            {
                                if (cell.Address.ColumnNumber - 1 < dt.Columns.Count)
                                    dt.Rows[dt.Rows.Count - 1][cell.Address.ColumnNumber - 1] = data;
                            }
                            catch (Exception e)
                            {

                            }
                        }
                    }
                }
            }

            return dt;
        }




        public static string Replace(string s, char[] separators, string newVal)
        {
            string[] temp;

            temp = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return String.Join(newVal, temp);
        }

        static DataTable CustomizeDT(DataTable dt)
        {
            // Substitute spaces and punctuation with "_"

            char[] charsToReplace = new char[] { ' ', ')', '(' };
            foreach (var col in dt.Columns)
                dt.Columns[col.ToString()].ColumnName = Replace(dt.Columns[col.ToString()].ColumnName, charsToReplace, "_");

            // Handle LOANIQ spreadsheet
            if (dt.Columns.Contains("Activity/Date/Deal"))
            {
                string myActivity = "";
                string myDate = "";
                DateTime dateValue;

                dt.Columns.Add("Activity");
                dt.Columns.Add("Date");
                dt.Columns["Activity/Date/Deal"].ColumnName = "Deal";

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];
                    string dataStr = dr[0].ToString().Trim();
                    if (dataStr.Length == 0)
                    {
                        string breakdownValue = dr["Deal"].ToString();

                        if (DateTime.TryParse(breakdownValue, out dateValue))
                            myDate = breakdownValue;
                        else
                            myActivity = breakdownValue;
                        //dr.Delete();
                    }

                    else
                    {
                        dr["Activity"] = myActivity;
                        dr["Date"] = myDate;

                    }
                }

                // Delete breakdown rows
                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow dr = dt.Rows[i];
                    if (dr[0].ToString().Trim().Length == 0)
                        dr.Delete();
                }
                dt.AcceptChanges();

            }

            return dt;
        } 

        private static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            else
            {
                return value;
            }
        }
    }
}
