using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mars.TestFramework.DataCompare
{
    public class XmlCompareReportConfig
    {
        /*
        string normalForegroundColor = "BLACK";
        string errorForegroundColor = "RED";
        string normalBackgroundColor = "GREEN";
        string errorBackgroundColor = "BROWN";
         * */
        public bool setShowDiffOnly = false;

        internal void SetShowDiffOnly(bool setShowDiffOnly)
        {
            this.setShowDiffOnly = setShowDiffOnly;
        }
    }
}
