using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class HashDataTable
    {
        public Dictionary<string, DataRow> dict = new Dictionary<string, DataRow>();

        public HashDataTable()
        {

        }

        public HashDataTable(DataTable dt, string[] headers, string[] keys)
        {
            dt.Columns.Add("Add_Idx", typeof(int));
            foreach (DataRow row in dt.Rows)
            {
                int idx = 0;
                string key = CreateKey(row, keys, idx);
                //dict.Add(key, row);

                while (dict.Keys.Contains(key))
                {
                    idx++;
                    key = CreateKey(row, keys, idx);
                }

                try
                {
                    dict.Add(key, row);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Key = " + key);
                }

                row["Add_Idx"] = idx;
            }
        }

        private string CreateKey(DataRow row, string[] keys, int idx)
        {
            string resultKey = "";
            foreach (string key in keys)
            {
                string data = AdjustData(key, row[key].ToString());
                //resultKey += row[key] + "_";
                resultKey += data + "_";
            }

            resultKey += idx;
            //resultKey = resultKey.Remove(resultKey.LastIndexOf("_"), 1);

            return resultKey;
        }

        private string AdjustData(string fieldName, string  data)
        {
            string resultData = data;
            if (fieldName.EndsWith("Date") && data.Trim().Length == 19)
            {
                resultData = data.Substring(0, 4) + data.Substring(5, 2) + data.Substring(8, 2);
            }
            return resultData;
        }
    }
}
