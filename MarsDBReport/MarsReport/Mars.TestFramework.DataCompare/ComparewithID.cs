
/*A class for constructing a new compare configuration with an ID*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mars.TestFramework.DataCompare
{
    public class ComparewithID
    {
       
        public ComparewithID()
        {
            CompareID = "";
            S1Type = "";
            S1DBConn = "";
            S1DBType = "";
            S1ConnString = "";
            S1QueryID = "";
            S1Query = "";
            S1FileLocation = ""; 

            S2Type = "";
            S2DBConn = "";
            S2DBType = "";
            S2ConnString = "";
            S2QueryID = "";
            S2Query = "";
            S2FileLocation = ""; 

            KeyFields = ""; 
            ShowFields = "";
            CompareFields = "";

            RowFields = "";
            ColumnFields = "";
            OFileLocation = "";

            OutputFilter = "";
            OutputOrderBy = "";
            OutputFilterApply = false;

            InteractiveMode = false;

        }
         
        public string CompareID { get; set; }
        public string S1Type { get; set; }
        public string S1DBConn { get; set; }
        public string S1DBType { get; set; }
        public string S1ConnString { get; set; }
        public string S1QueryID { get; set; }
        public string S1Query { get; set; }
        public string S1FileLocation { get; set; }

        public string S2Type { get; set; }
        public string S2DBConn { get; set; }
        public string S2DBType { get; set; }
        public string S2ConnString { get; set; }
        public string S2QueryID { get; set; }
        public string S2Query { get; set; }
        public string S2FileLocation { get; set; }

        public string KeyFields { get; set; }
        public string ShowFields { get; set; }
        public string CompareFields { get; set; }

        public string RowFields { get; set; }
        public string ColumnFields { get; set; }

        public string OFileType { get; set; }
        public string OFileLocation { get; set; }

        public string OutputFilter { get; set; }
        public string OutputOrderBy { get; set; }
        public bool   OutputFilterApply { get; set; }
        public string S1OpicsRepFileLoc { get; set; }
        public string S2OpicsRepFileLoc { get; set; }
        public string OfileName { get; internal set; }
        public string S1CSVDelim { get; set; }
        public string S2CSVDelim { get; set; }

        public bool InteractiveMode { get; set; }

        public string S1XMlIndex { get; set; }
        public string S2XMlIndex { get; set; }

        public override string ToString()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy;
            System.Reflection.PropertyInfo[] infos = this.GetType().GetProperties(flags);

            StringBuilder sb = new StringBuilder();

            string typeName = this.GetType().Name;
            sb.AppendLine("\n" + string.Empty.PadRight(typeName.Length + 5, '='));
            sb.AppendLine(typeName);
            sb.AppendLine(string.Empty.PadRight(typeName.Length + 5, '='));

            foreach (var info in infos)
            {
                object value = info.GetValue(this, null);
                sb.AppendFormat("{0}: {1}{2}", info.Name, value != null ? value : "null", Environment.NewLine);
            }

            sb.AppendLine(string.Empty.PadRight(typeName.Length + 5, '='));

            return sb.ToString();
        }
    }
}
