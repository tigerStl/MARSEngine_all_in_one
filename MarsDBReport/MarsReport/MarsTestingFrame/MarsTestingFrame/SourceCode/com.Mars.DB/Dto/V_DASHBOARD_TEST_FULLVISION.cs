using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.DB.Dto
{
#if _Datafrom_Database
    [DataContract()]
    public class V_DASHBOARD_TEST_FULLVISION
    {
        [DataMember()]
        public long? DASHBOARD_ID { get; set; }
        [DataMember()]
        public string DASHBOARD_NAME { get; set; }
        [DataMember()]
        public long? PROJECT_ID { get; set; }
        [DataMember()]
        public string PROJECT_NAME { get; set; }
        [DataMember()]
        public string PROJECT_DESCRIPTION { get; set; }
        [DataMember()]
        public string TEST_CASE_NAME { get; set; }

        [DataMember()]
        public long? TEST_CASE_ID { get; set; }
        [DataMember()]
        public long? TEST_SUITE_ID { get; set; }
        [DataMember()]
        public string TEST_SUITE_NAME { get; set; }
        [DataMember()]
        public string TEST_STEP_DESCRIPTION { get; set; }
        
        [DataMember()]
        public string TEST_SUITE_DESCRIPTION { get; set; }
        [DataMember()]
        public long? RUN_ORDER { get; set; }
        [DataMember()]
        public string DISPLAY_NAME { get; set; }
        [DataMember()]
        public short? TEST_RUN_VALUE { get; set; }
        [DataMember()]
        public long? LATEST_TEST_MARK_ID { get; set; }
        [DataMember()]
        public long? HIS_LATEST_TEST_MARK_ID { get; set; }
        [DataMember()]
        public long? HIS_ID { get; set; }
        [DataMember()]
        public long? HIS_TEST_ID { get; set; }
        [DataMember()]
        public DateTime? TEST_CASE_BEGIN_TIME { get; set; }
        [DataMember()]
        public DateTime? TEST_CASE_END_TIME { get; set; }
        [DataMember()]
        public string HIST_TEST_RESULT_IN_TEXT { get; set; }
        [DataMember()]
        public short? HIST_TEST_MODE { get; set; }
        [DataMember()]
        public short? HIS_RESULT { get; set; }
        [DataMember()]
        public string ALIAS_NAME { get; set; }
        [DataMember]
        public long? RELY_ON { get; set; }

        public override string ToString()
        {
            return string.Format("DASHBOARD_ID:[{0}] DASHBOARD_NAME:[{1}] PROJECT_NAME:[{2}] TEST_SUITE_ID:[{3}] TEST_SUITE_NAME:[{4}] TEST_CASE_ID:[{5}] TEST_CASE_NAME:[{6}]",
                DASHBOARD_ID, DASHBOARD_NAME, PROJECT_NAME, TEST_SUITE_ID, TEST_SUITE_NAME, TEST_CASE_ID, TEST_CASE_NAME);
        }
    }
#endif
}
