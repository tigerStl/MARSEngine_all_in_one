
/*A class for saving a query along with an ID*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARS.CompareGUI
{
    public class QuerywithID
    {
        public QuerywithID()
        {
            QueryID = "";
            Query = "";
        }

        public string QueryID { get; set; }
        public string Query { get; set; }

    }
}
