using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class CompareResult
    {
        public int StatusColNumber { get; set; }
        public int ErrorColNumber { get; set; }
        public int LineItemIdColNumber { get; set; }

        public int DividerColNumber { get; set; }

        public int LeftSideFirstColNumber { get; set; }
        public int RightSideFirstColNumber { get; set; }

        public DataTable ResultDataTable { get; set; }
        public DataTable SummaryDataTable { get; set; }
        public DataTable InFirstOnlyDataTable { get; set; }
        public DataTable InSecondOnlyDataTable { get; set; }

        public string[] showFieldNames { get; set; }
        public string[] keyFieldNames { get; set; }
        public string[] allFieldNames { get; private set; }

        public ErrorReport errorReport;

        string SEPARATOR = "SEPARATOR";
        string REP_Status = "REP_Status";
        string REP_Error = "REP_Error";
        public string LINE_ITEM_ID = "LineItemID";

        int currentLineNumber = 0;

        internal void Init(string[] allFieldNames, string[] showFieldNames, string[] keyFieldNames)
        {
            this.allFieldNames = allFieldNames;
            this.showFieldNames = showFieldNames;
            this.keyFieldNames = keyFieldNames;


            // Init InFirstOnlyDataTable
            InFirstOnlyDataTable = new DataTable();
            foreach (string header in showFieldNames)
            {
                InFirstOnlyDataTable.Columns.Add(header + "_1", typeof(String));
            }

            // Init InSecondOnlyDataTable
            InSecondOnlyDataTable = new DataTable();
            foreach (string header in showFieldNames)
            {
                InSecondOnlyDataTable.Columns.Add(header + "_2", typeof(String));
            }

            // Init SummaryDataTable
            SummaryDataTable = new DataTable();
            foreach (string header in keyFieldNames)
            {
                SummaryDataTable.Columns.Add(header, typeof(String));
            }
            SummaryDataTable.Columns.Add("FieldName", typeof(String));
            SummaryDataTable.Columns.Add("Value_1", typeof(String));
            SummaryDataTable.Columns.Add("Value_2", typeof(String));
            SummaryDataTable.Columns.Add("Diff", typeof(String));

            // Init ResultDataTable
            ResultDataTable = new DataTable();
            foreach (string header in showFieldNames)
            {
                ResultDataTable.Columns.Add(header + "_1", typeof(String));
            }

            ResultDataTable.Columns.Add(SEPARATOR, typeof(String));

            foreach (string header in showFieldNames)
            {
                ResultDataTable.Columns.Add(header + "_2", typeof(String));
            }

            ResultDataTable.Columns.Add(REP_Status, typeof(String));
            ResultDataTable.Columns.Add(REP_Error, typeof(String));

            ResultDataTable.Columns.Add(LINE_ITEM_ID, typeof(int));
            

            errorReport = new ErrorReport();

            LeftSideFirstColNumber = 0;
            RightSideFirstColNumber = showFieldNames.Length + 1;
            DividerColNumber = showFieldNames.Length;
            StatusColNumber = showFieldNames.Length * 2 + 1;
            ErrorColNumber = showFieldNames.Length * 2 + 2;

            LineItemIdColNumber = showFieldNames.Length * 2 + 3;
        }

        internal void GenerateEntry(CompareErrorLineItem lineItem)
        {
            // Process Result
            DataRow dr = ResultDataTable.NewRow();
            ResultDataTable.Rows.Add(dr);

            // left side

            if (lineItem.dr1 != null)
            {
               foreach (string columnName in showFieldNames)
               {
                   dr[columnName + "_1"] = lineItem.dr1[columnName];
               }
               
            }

            // right side
            if (lineItem.dr2 != null)
            {
                foreach (string columnName in showFieldNames)
                {
                    dr[columnName + "_2"] = lineItem.dr2[columnName];
                }
            }

            lineItem.LineNumber = currentLineNumber;
            currentLineNumber++;

            // status
            if (lineItem.ErrorStatus != CompareErrorLineItem.Status.Equal)
                dr[REP_Status] = "FAIL";

            // Error
            dr[REP_Error] = lineItem.ErrorMessage;

            // Line Item ID
            dr[LINE_ITEM_ID] = lineItem.LineId;

            errorReport.GenetateLineItem(lineItem);

            // Process InFirstOnly
            if (lineItem.ErrorStatus == CompareErrorLineItem.Status.Left)
            {
                DataRow dr1 = InFirstOnlyDataTable.NewRow();
                InFirstOnlyDataTable.Rows.Add(dr1);
                if (lineItem.dr1 != null)
                {
                    foreach (string columnName in showFieldNames)
                    {
                        dr1[columnName + "_1"] = lineItem.dr1[columnName];
                    }
                }
            }

            // Process InSecondOnly
            if (lineItem.ErrorStatus == CompareErrorLineItem.Status.Right)
            {
                DataRow dr2 = InSecondOnlyDataTable.NewRow();
                InSecondOnlyDataTable.Rows.Add(dr2);
                if (lineItem.dr2 != null)
                {
                    foreach (string columnName in showFieldNames)
                    {
                        dr2[columnName + "_2"] = lineItem.dr2[columnName];
                    }
                }
            }

            // Process summary
            if (lineItem.ErrorStatus == CompareErrorLineItem.Status.NotEqual)
            {
                foreach (int colNum in lineItem.ErrorColumns)
                {
                    DataRow drSummary = SummaryDataTable.NewRow();
                    SummaryDataTable.Rows.Add(drSummary);

                    //string compareColumnName = allFieldNames[colNum];
                    string compareColumnName = showFieldNames[colNum];

                    foreach (string header in keyFieldNames)
                    {
                        drSummary[header] = lineItem.dr1[header];
                    }
                    drSummary["FieldName"] = compareColumnName;
                    drSummary["Value_1"] = lineItem.dr1[compareColumnName];
                    drSummary["Value_2"] = lineItem.dr2[compareColumnName];

                    double leftValue = 0.0;
                    double rightValue = 0.0;

                    string a = lineItem.dr1[compareColumnName].ToString();
                    string b = lineItem.dr2[compareColumnName].ToString();

                    try
                    {
                       
                        if (Double.TryParse(lineItem.dr1[compareColumnName].ToString(), out leftValue) == true &&
                            Double.TryParse(lineItem.dr2[compareColumnName].ToString(), out rightValue) == true)
                        {
                        drSummary["Diff"] = " " + (leftValue - rightValue);
                    }
                    else
                        drSummary["Diff"] = " ";
                    }
                    catch (Exception e)
                    {

                    }

                }
            }

        }

        
    }
}
