using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class CompareConfig
    {
        public string ConfigID { get; set; }
        public string Client { get; set; }
        public string ConnectionURL { get; set; }
        public string ExecutionHost { get; set; }
        public string ExTime { get; set; }
        public string Status { get; set; }
        public string ReportFileLocation { get; set; }
        public string InstanceVersionBaseline { get; set; }
        public string InstanceNameBaseline { get; set; }
        public string CompareTypeBaseline { get; set; }
        public string DBConnectionNameBaseline { get; set; }
        public string DBConnectionDetailsBaseline { get; set; }
        public string QueryIdBaseline { get; set; }
        public string QueryBaseline { get; set; }
        public string FileLocationBaseline { get; set; }
        public string InstanceVersionTarget { get; set; }
        public string InstanceNameTarget { get; set; }
        public string CompareTypeTarget { get; set; }
        public string DBConnectionNameTarget { get; set; }
        public string DBConnectionDetailsTarget { get; set; }
        public string QueryIdTarget { get; set; }
        public string QueryTarget { get; set; }
        public string FileLocationTarget { get; set; }
        public string KeyFields { get; set; }
        public string ShowFields { get; set; }
        public string CompareFields { get; set; }

    }
}
