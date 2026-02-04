using com.Mars.Constants;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MarsTestFrame.com.Mars.TestConfigObjects.Adatpers
{
    public class TestSuiteAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestSuiteAdapter));
        public virtual ConfigObjectBase LoadTestSuiteInfo(DataRow objRow)
        {
            return null;
        }
    }

    public class TestSuiteXlsAdapter:TestSuiteAdapter
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestSuiteXlsAdapter));
        public override ConfigObjectBase LoadTestSuiteInfo(DataRow objRow)
        {
            Logger.logBegin("LoadTestSuiteInfo");
            if (objRow == null)
            {
                Logger.Error("LoadTestSuiteInfo", "objRow == null");
                return null;
            }
            BatchConfigObject objBatch = new BatchConfigObject();
            /*** for excel load ***/

            return objBatch;
        }
    }

    public class TestSuiteAdapterFactory
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestSuiteAdapterFactory)) ;
        public static TestSuiteAdapter GetAdapterInstance(MARS_ADAPTER eAdpterId)
        {
            Logger.logBegin("GetAdapterInstance");
            try
            {
                switch (eAdpterId)
                {
                    case MARS_ADAPTER._ADPTR_XLSJET_2_TESTSUITE:
                        return new TestSuiteXlsAdapter();
                    default: return null;
                }
            }
            finally
            {
                Logger.logEnd("GetAdapterInstance");
            }
        }
    }
}
