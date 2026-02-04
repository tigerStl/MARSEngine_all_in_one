using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.ExecuteStoryboard
{
    public class TestStepResumeStatus
    {

        public static string UUID_FROM_WEB = null;

        public int statusId; // 0- normal 1-error
        public int resumeNextRunOrder = -1;
        public bool hasExceptionsPrevious = false;
        public void init()
        {
            statusId = 0;
            resumeNextRunOrder = -1;
            hasExceptionsPrevious = false;
        }
    }

    public class MarsGlobalStatusMgr
    {
        public static TestStepResumeStatus resumeNextStatus = new TestStepResumeStatus();

        public static void InitStatusData()
        {
            resumeNextStatus.init();
        }
    }


}
