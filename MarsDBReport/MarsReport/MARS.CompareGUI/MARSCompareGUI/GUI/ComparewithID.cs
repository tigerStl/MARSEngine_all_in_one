
/*A class for constructing a new compare configuration with an ID*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARS.TEMP
{
    public class ComparewithID
    {
        public ComparewithID()
        {
            CompareID = "";
            S1Type = "";
            S1DBConn = "";
            S1ConnString = "";
            S1QueryID = "";
            S1Query = "";
            S1FileLocation = ""; 

            S2Type = "";
            S2DBConn = "";
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
        }
         
        public string CompareID { get; set; }
        public string S1Type { get; set; }
        public string S1DBConn { get; set; }
        public string S1ConnString { get; set; }
        public string S1QueryID { get; set; }
        public string S1Query { get; set; }
        public string S1FileLocation { get; set; }

        public string S2Type { get; set; }
        public string S2DBConn { get; set; }
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
    }
}
