using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare
{
    class XmlCompareResult
    {
        public List<ResultDataRow> resultData = new List<ResultDataRow>();
        public List<string> columnNames = null;


        internal void InitHeaders(List<string> headers)
        {
            columnNames = new List<string>();
            foreach (string header in headers)
            {
                columnNames.Add(header + "_1");

            }

            foreach (string header in headers)
            {
                columnNames.Add(header + "_2");
            }
        }

        internal ResultDataRow CreateRow()
        {
            ResultDataRow row = new ResultDataRow();
            resultData.Add(row);
            row.columnNames = columnNames;
            return row;
        }
    }
}
