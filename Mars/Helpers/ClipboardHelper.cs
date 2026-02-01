using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mars.Helpers
{
    public static class ClipboardHelper
    {
        public delegate string[] ParseFormat(string value);

        public static DataTable ParseClipboardToDataTable()
        {
            DataTable dt = new DataTable();
            List<string[]> valueArray = ClipboardHelper.ParseClipboardData(); ;

            //Get the column names
            for (int k = 0; k < valueArray[0].Length; k++)
            {
                //add columns to the data table.
                dt.Columns.Add((string)valueArray[0][k]);
            }

            string[] singleDValue = new string[valueArray[0].Length];

            for (int i = 1; i < valueArray.Count() -1; i++)
            {
                for (int k = 0; k < dt.Columns.Count; k++)
                {
                    if (valueArray[i + 1].Length > k)
                        singleDValue[k] = valueArray[i + 1][k];
                    else
                        singleDValue[k] = "";
                }
               
                dt.LoadDataRow(singleDValue, System.Data.LoadOption.PreserveChanges);
            }

            return dt;
        }

        public static List<string[]> ParseClipboardData()
        {
            List<string[]> clipboardData = null;
            object clipboardRawData = null;
            ParseFormat parseFormat = null;

            // get the data and set the parsing method based on the format
            // currently works with CSV and Text DataFormats            
            IDataObject dataObj = System.Windows.Clipboard.GetDataObject();
            if ((clipboardRawData = dataObj.GetData(DataFormats.CommaSeparatedValue)) != null)
            {
                parseFormat = ParseCsvFormat;
            }
            else if ((clipboardRawData = dataObj.GetData(DataFormats.Text)) != null)
            {
                parseFormat = ParseTextFormat;
            }

            if (parseFormat != null)
            {
                string rawDataStr = clipboardRawData as string;

                if (rawDataStr == null && clipboardRawData is MemoryStream)
                {
                    // cannot convert to a string so try a MemoryStream
                    MemoryStream ms = clipboardRawData as MemoryStream;
                    StreamReader sr = new StreamReader(ms);
                    rawDataStr = sr.ReadToEnd();
                }
                Debug.Assert(rawDataStr != null, string.Format("clipboardRawData: {0}, could not be converted to a string or memorystream.", clipboardRawData));

                string[] rows = rawDataStr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (rows != null && rows.Length > 0)
                {
                    clipboardData = new List<string[]>();
                    foreach (string row in rows)
                    {
                        clipboardData.Add(parseFormat(row));
                    }
                }
                else
                {
                    Debug.WriteLine("unable to parse row data.  possibly null or contains zero rows.");
                }
            }

            return clipboardData;
        }

        public static string[] ParseCsvFormat(string value)
        {
            return ParseCsvOrTextFormat(value, true);
        }

        public static string[] ParseTextFormat(string value)
        {
            return ParseCsvOrTextFormat(value, false);
        }

        private static string[] ParseCsvOrTextFormat(string value, bool isCSV)
        {
            List<string> outputList = new List<string>();

            char separator = isCSV ? ',' : '\t';
            int startIndex = 0;
            int endIndex = 0;

            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (ch == separator)
                {
                    outputList.Add(value.Substring(startIndex, endIndex - startIndex));

                    startIndex = endIndex + 1;
                    endIndex = startIndex;
                }
                else if (ch == '\"' && isCSV)
                {
                    // skip until the ending quotes
                    i++;
                    if (i >= value.Length)
                    {
                        throw new FormatException(string.Format("value: {0} had a format exception", value));
                    }
                    char tempCh = value[i];
                    while (tempCh != '\"' && i < value.Length)
                        i++;

                    endIndex = i;
                }
                else if (i + 1 == value.Length)
                {
                    // add the last value
                    outputList.Add(value.Substring(startIndex));
                    break;
                }
                else
                {
                    endIndex++;
                }
            }

            return outputList.ToArray();
        }



        internal static DataTable ParseClipboardToDataTable(List<string[]> valueArray)
        {

            if (valueArray == null)
                return null;

            if (valueArray[0][0].Equals("keyword") == true)
                valueArray.RemoveAt(0);

            //string[] headerNames = {"keyword", "object", "row_column", "value", "Comment"};
#if !v_16AndUp
            string headerNameString = "keyword,object,row_column,value,Comment,Data Set 1,Data Set 2,Data Set 3,Data Set 4,Data Set 5,Data Set 6,Data Set 7,Data Set 8,Data Set 9,Data Set 10,Data Set 11,Data Set 12,Data Set 13,Data Set 14,Data Set 15,Data Set 16,Data Set 17,Data Set 18,Data Set 19,Data Set 20";
#else
            string headerNameString = "keyword,object,row_column,value,Comment,Data Set 1,Data Set 2,Data Set 3,Data Set 4,Data Set 5,Data Set 6,Data Set 7,Data Set 8,Data Set 9,Data Set 10,Data Set 11,Data Set 12,Data Set 13,Data Set 14,Data Set 15,Data Set 16,Data Set 17,Data Set 18,Data Set 19,Data Set 20";
#endif
            string[] headerNames = headerNameString.Split(',');
            valueArray.Insert(0, headerNames);
          
            DataTable dt = new DataTable();
           
            //Get the column names
            for (int k = 0; k < valueArray[0].Length; k++)
            {
                //add columns to the data table.
                dt.Columns.Add((string)valueArray[0][k]);
            }

            string[] singleDValue = new string[valueArray[0].Length];

            for (int i = 1; i < valueArray.Count(); i++)
            {
                for (int k = 0; k < dt.Columns.Count; k++)
                {
                    if (valueArray[i].Length > k)
                        singleDValue[k] = valueArray[i][k];
                    else
                        singleDValue[k] = "";
                }

                dt.LoadDataRow(singleDValue, System.Data.LoadOption.PreserveChanges);
            }

            return dt;
        }
    }
}
