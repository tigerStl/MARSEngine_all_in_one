using Mars.TestFramework.DataCompare.Opics.Report;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare
{
    public class ReportToDt
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ReportToDt));

        Dictionary<string, string> currentDataValue = new Dictionary<string, string>();
        DataTable dt = new DataTable();
        List<string> CombinedHeaderList = new List<string>();
        List<string> BreakdownHeaders;
        List<string> ReportHeaders;
        List<string> TotalHeaders;
        List<string> IgnoreList;
        List<int> ColBreak;
        OpicsReportFormatRaw RawFmt;
        string InputFileName;
        char[] delimiterChars = { ':' };

        public DataTable ConvertToDT(string inputFileName, List<string> breakdownHeaders, List<string> reportHeaders, List<string> totalHeaders, 
            List<string> ignoreList, List<int> colBreak, OpicsReportFormatRaw rawFmt)
        {
            InputFileName = inputFileName;
            BreakdownHeaders = breakdownHeaders;
            ReportHeaders = reportHeaders;
            TotalHeaders = totalHeaders;
            IgnoreList = ignoreList;
            ColBreak = colBreak;
            RawFmt = rawFmt;

            ReportHeaders.Add("IsTotal");
            ReportHeaders.Add("TotalLabel");

            CombinedHeaderList.AddRange(breakdownHeaders);
            CombinedHeaderList.AddRange(reportHeaders);

          

            if (InputFileName.Contains("RGLAC"))
            {
                CombinedHeaderList.Add("DRCR");
                // Add DRCR field -- calculated for RGLAC reports only
            }
            
            try
            {
                InitDtColumns();
                ProcessData();
            }

            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

           

           // dt.Rows[0].Delete();
            return dt;
        }

        private void ProcessData()
        {
            StreamReader reader = File.OpenText(InputFileName);
            string line;
            bool nextIsTotalsLine = false;
            string totalLabel = "";

            Regex reg = new Regex("[*'\",_&#^@]");
            

            while ((line = reader.ReadLine()) != null)
            {
                // Line does not contain useful info -- ignore
                if (line == null ||
                    line.Trim().Length == 0 ||
                    StartsWithValueInList(line.Trim(), IgnoreList) == true)
                {
                    continue;
                }

                // Handle "Total header"
                if (line.StartsWith("Total") || line.StartsWith("Grand Total:"))
                {
                    nextIsTotalsLine = true;
                    totalLabel = line.Trim();
                    totalLabel = reg.Replace(totalLabel, string.Empty);
                    continue;
                }

                
                // Handle Opening Balance and Closing Balance
                if (line.Contains("Balance"))
                    continue;

                

                // Line contains breakdow info -- record the info
                else if (StartsWithValueInList(line, BreakdownHeaders) == true)
                {
                    string[] kv = line.Split(delimiterChars);
                    string key = kv[0].Trim();
                    string value = kv[1].Trim();
                    //currentDataValue[kv[0].Trim()] = kv[1].Trim();

                    if (InputFileName.Contains("DCCY") && value != null &&
                           (value.Contains("CTR NPV Rate") || value.Contains("CTR Reval Rate")))
                    {
                        value = value.Split(' ').First();
                    }

                    currentDataValue[key] = value;
                }

                // Line contains regular report info -- add data to DataTable
                else
                {
                    for (int colNum = 0; colNum < ReportHeaders.Count; colNum++)
                    {
                        string format = "";
                        string header = ReportHeaders[colNum];
                       
                        
                        if (header.Equals("IsTotal") == false  && header.Equals("TotalLabel") == false)
                        {
                            var fieldConfig = (from r in RawFmt.details
                                               where r.Header.Equals(header)
                                               select r);

                            if (fieldConfig != null)
                                format = fieldConfig.FirstOrDefault().DataFormat;
                        }

                        // Handle total
                        if (header.Equals("IsTotal"))
                        {
                            if (nextIsTotalsLine == true)
                            {
                                currentDataValue["IsTotal"] = "TRUE";
                                currentDataValue["TotalLabel"] = totalLabel;  // AF
                                //currentDataValue["DRCR"] = " ";
                                nextIsTotalsLine = false;
                            }
                            else
                            { 
                                currentDataValue["IsTotal"] = "FALSE";
                                currentDataValue["TotalLabel"] = " "; 
                            }
                            continue;
                        }

                        if (header.Equals("TotalLabel"))
                            continue;

                        int start = ColBreak[colNum];

                        if (start < 0)
                            continue;

                        int len = ColBreak[colNum + 1] - ColBreak[colNum];

                        int adj = start + len - line.Length;
                        if (adj > 0)
                            len = len - adj;

                        string data = line.Trim().Replace(",", String.Empty);

                        if (len > 0)
                            data = line.Substring(start, len).Trim().Replace(",", String.Empty);
                        
                        // adjust data if format = 30 -- Amount 
                        if (format.Equals("30"))
                        {
                            data = data.Split(' ').Last();
                        }
                        

                        if (InputFileName.Contains("RGLAC"))
                        {
                            // Fill DRCR field
                            if (header.Equals("Debit Amount") && data.Trim().Length != 0)
                                currentDataValue["DRCR"] = "DEBIT";
                            else if (header.Equals("Credit Amount") && data.Trim().Length != 0)
                                currentDataValue["DRCR"] = "CREDIT";
                        }

                        currentDataValue[header] = data;
                    }
                    AddDataToTable();
                }


            }
        }

        private void AddDataToTable()
        {
            DataRow workRow = dt.NewRow();
            foreach (string header in CombinedHeaderList)
            {
                if (header.Trim().Length > 0 && currentDataValue.Keys.Contains(header))
                try
                {       
                    string data = currentDataValue[header];
                    workRow[header] = data;
                }
                catch(Exception ex)
                {
                    Logger.Info("AddDataToTable", ex.ToString());
                    continue;
                }
            }

            dt.Rows.Add(workRow);
        }

        private bool StartsWithValueInList(string line, List<string> wordList)
        {
            foreach (string str in wordList)
            {
                if (line.StartsWith(str))
                    return true;
            }
            return false;
        }

        private void InitDtColumns()
        {
            foreach (string header in CombinedHeaderList)
            {
                if (dt.Columns.Contains(header) == false)
                    dt.Columns.Add(header, typeof(String));
            }
        }
    }
}
