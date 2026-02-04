using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{
    public class StoryboardStats
    {
        public int MarsBFail { get; set; }
        public int MarsBPartial { get; set; }
        public int MarsBSucc { get; set; }
        public int MarsBUnpr { get; set; }
        public int MarsCFail { get; set; }
        public int MarsCPartial { get; set; }
        public int MarsCSucc { get; set; }
        public int MarsCUnpr { get; set; }
        public int MarsTCCount { get; set; }
        public int MarsTestStepCount { get; set; }
        public int MarsTSCount { get; set; }

        public Dictionary<long, int> sbStatusDict = new Dictionary<long, int>();
    }
}
