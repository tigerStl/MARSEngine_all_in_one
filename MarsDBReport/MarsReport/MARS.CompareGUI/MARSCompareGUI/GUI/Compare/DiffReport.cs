using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace MARS.TEMP
{
    class DiffReport
    {
        //function to populate columns with headers
        public static DataTable DefineTable(List<string> KeyFields)
        {
            DataTable DiffTableWithHeaders = new DataTable();
            //populate with key fields
            foreach (string entry in KeyFields)
            {
                DiffTableWithHeaders.Columns.Add(entry, typeof(string));
            }
            //populate with fieldname, value 1 and value 2
            DiffTableWithHeaders.Columns.Add("Field Name", typeof(string));
            DiffTableWithHeaders.Columns.Add("Value 1", typeof(string));
            DiffTableWithHeaders.Columns.Add("Value 2", typeof(string));

            return DiffTableWithHeaders;
        }
        
        //function to populate rows with data
        public static DataTable PopulateDTRow(DataTable dt, List<string> KeyFieldValues, string attr, string value1, string value2 )
        {
            /*
            //populate the datatable with key field values
            foreach (var entry in KeyFieldValues )
            {
                dt.Rows.Add(entry, typeof(string));
            }
            //populate the datatable with diff values
            dt.Rows.Add(attr, typeof(string));
            dt.Rows.Add(value1, typeof(string));
            dt.Rows.Add(value2, typeof(string));
            */
            //dt.Rows.Add(KeyFieldValues.ToArray(), attr, value1, value2);

            List<string> DiffRow = new List<string>();
            DiffRow = KeyFieldValues;
            DiffRow.Add(attr);
            DiffRow.Add(value1);
            DiffRow.Add(value2);

            dt.Rows.Add(DiffRow.ToArray());
            
            return dt;
        }
    }
}
