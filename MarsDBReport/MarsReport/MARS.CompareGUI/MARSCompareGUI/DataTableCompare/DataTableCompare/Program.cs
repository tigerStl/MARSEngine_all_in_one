using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    class Program
    {
        static string[] allFieldNames = { "Location", "Id", "OriginalId", "OwnerTable", "TradeId", "SeqNum", "AssetID", "Event", "Amount", "Ccy", "PostingDate", "ValueDate", "BookingDate", "Description", "AccountID", "DrOrCr", "PostingType", "ExtractFlag", "ExtractDate", "ProcessFlag", "SettleAcctType", "SettleAcctID", "AltOwnerTable", "AltTradeId", "AltVersion", "Version", "Beneficiary", "CustRole", "AcctType", "OrigSettleCcy", "OrigSettleAmt", "ObjId", "GLDate" };
        //static string[] keyFieldNames = { "Location", "Id" };
        static string[] keyFieldNames = { "Location", "Id", "OriginalId", "OwnerTable", "TradeId", "SeqNum", "AssetID", "Event", "Ccy", "PostingDate", "ValueDate", "BookingDate", "Description", "AccountID", "DrOrCr", "PostingType", "ExtractFlag", "ExtractDate", "ProcessFlag", "SettleAcctType", "SettleAcctID", "AltOwnerTable", "AltTradeId", "AltVersion", "Version", "Beneficiary", "CustRole", "AcctType", "OrigSettleCcy", "OrigSettleAmt", "ObjId", "GLDate" };
        static string[] showFieldNames = { "Location", "Id", "OriginalId", "OwnerTable", "TradeId", "SeqNum", "AssetID", "Event", "Amount", "Ccy", "PostingDate", "ValueDate", "BookingDate", "Description", "AccountID", "DrOrCr", "PostingType", "ExtractFlag", "ExtractDate", "ProcessFlag", "SettleAcctType", "SettleAcctID", "AltOwnerTable", "AltTradeId", "AltVersion", "Version", "Beneficiary", "CustRole", "AcctType", "OrigSettleCcy", "OrigSettleAmt", "ObjId", "GLDate" };
        static string[] compareFieldNames = { "Amount", "OrigSettleAmt" };


        static string file1 = @"C:\MDEV\xmlCompareTest\Data\acct.csv";
        static string file2 = @"C:\MDEV\xmlCompareTest\Data\acct2.csv";

        static string outputFlieName = @"c:\temp\ex1.xlsx";

        static void Main(string[] args)
        {
            DataTable dt1 = ConvertCSVtoDataTable(file1);
            DataTable dt2 = ConvertCSVtoDataTable(file2);

            DTCompare dtc = new DTCompare(dt1, dt2, allFieldNames, keyFieldNames, showFieldNames, compareFieldNames, outputFlieName);
            dtc.Compare();
            //excelTest(dt1, @"c:\temp\ex1.xlsx");
        }

private static void excelTest(DataTable dt1,string p)
{
 	XLWorkbook wb = new XLWorkbook();
    wb.Worksheets.Add(dt1,"WorksheetName");

    var ws = wb.Worksheet("WorksheetName");

    for (int i = 1; i < 10000; i++ )
    {
        ws.Cell(i, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFF00FF); 
    }

        wb.SaveAs(p);
}

        
        public static DataTable ConvertCSVtoDataTable(string strFilePath)
        {
            Console.WriteLine("CSV: " + strFilePath);
            string delim = ",";

            HashSet<string> names = new HashSet<string>();

            
            StreamReader sr = new StreamReader(strFilePath);

            string headerLine = sr.ReadLine();

            string[] headers = headerLine.Split(delim[0]);

           
            


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


    }
}
