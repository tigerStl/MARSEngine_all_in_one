using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.DB.Dto
{
    public partial class TEST_PROJECT_VIEW
    {
        public string PROJECT_NAME { get; set; }
        public long PROJECT_ID { get; set; }
        public string TEST_SUITE_NAME { get; set; }
        public long TEST_SUITE_ID { get; set; }
        public string APP_SHORT_NAME { get; set; }
        public long APPLICATION_ID { get; set; }
    }
}
