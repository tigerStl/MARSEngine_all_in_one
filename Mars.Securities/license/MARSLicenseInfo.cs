using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Securities.license
{
    internal class MARSLicenseInfo
    {
        public string version { get; set; }
        public string uuid { get; set; }
        public List<string> mode { get; set; }
        public string issuedTo { get; set; }
        public string issuedOn { get; set; }
        public string issuedBy { get; set; }
        public LicenseTimeOnly timeOnly { get; set; }
        
        public string adminPassword { get; set; }
    }

    public class LicenseTimeOnly
    {
        public string fromDate { get; set; }
        public string toDate { get; set; }
    }    
}
