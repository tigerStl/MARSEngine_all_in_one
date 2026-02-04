//using ClosedXML.Excel;
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
           
        }
        

    }
}
