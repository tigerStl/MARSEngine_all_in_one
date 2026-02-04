using Mars.Business;
using Mars.Dto;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.autoTest.report.Word
{

    class TestStoryboardPieData 
    {
        private static Route2NSEx.src.Marquis.systemUtil.MLogger Logger = Route2NSEx.src.Marquis.systemUtil.MLogger.GetLogger(typeof(TestStoryboardPieData));

        private int sucessTCCnt;
        public int SuccessTCCnt
        {
            get { return sucessTCCnt; }
            set { sucessTCCnt = value; }
        }

        public int failedTCCnt;
        public int FailedTCCnt
        {
            get { return failedTCCnt; }
            set { failedTCCnt = value; }
        }

        private int unprocessedTCCnt;
        public int UnprocessedTCCnt
        {
            get { return unprocessedTCCnt; }
            set { unprocessedTCCnt = value; }
        }

        private int partialCount;
        public int PartialCount
        {
            get { return partialCount; }
            set { partialCount = value; }
        }


       
        public List<KeyValuePair<string, double>> GetPartsInfo()
        {
            List<KeyValuePair<string, double>> lstRslt = new List<KeyValuePair<string, double>>();
            double dTtl = 0.0;
            if ((dTtl += (unprocessedTCCnt + sucessTCCnt + failedTCCnt + partialCount)) <= 0.00000001)
            {
                lstRslt.Add(new KeyValuePair<string, double>("Zero Data", 1.0));
                return lstRslt;
            }
            //dTtl
            lstRslt.Add(new KeyValuePair<string, double>("Success", (sucessTCCnt * 1.0) / dTtl));
            lstRslt.Add(new KeyValuePair<string, double>("Failed", (failedTCCnt * 1.0) / dTtl));
            lstRslt.Add(new KeyValuePair<string, double>("Unprocessed", (unprocessedTCCnt * 1.0) / dTtl));
            lstRslt.Add(new KeyValuePair<string, double>("Partial", (partialCount * 1.0) / dTtl));
            return lstRslt;
        }
        private const int cnst_radius = 120;
        public int GetRadius()
        {
            return cnst_radius;
        }
    }
    class TestStepsReportGridData 
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(TestStepsReportGridData));
        /// <summary>
        /// StoryBoard RunOrder
        /// Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO> >
        ///     int -- Loop id
        ///     List -- data
        /// </summary>
        public KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> GridData { get; internal set; }
        /// <summary>
        /// Test case information, so 
        /// </summary>
        public T_TEST_CASE_SUMMARYDTO totalTestCaseInfo { get; internal set; }
        public List<V_TEST_STEPS_FULLVISIONDTO> listAllTestSteps { get; internal set; }

        public int GetTestCaseId(ref string strError)
        {
            strError = "No Data.";
            if (GridData.Equals(default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>))) return -1;
            strError = "No validated Run Order id for storyboard id";
            if (GridData.Key < 0) return -2;
            strError = "No data from Steps reports Info.";
            if (GridData.Value == null) return -3;
            Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> dicStep = GridData.Value;
            strError = "No data from Steps reports, Loop info is Null.";
            if (dicStep.Keys == null) return -4;
            strError = "No data from Steps reports, Loop count is null.";
            if (dicStep.Keys.Count == 0) return -5;
            List<V_TEST_DATA_REPORT_SUMMARYDTO> lstStpRpt = dicStep[dicStep.Keys.ElementAt(0)];
            if (lstStpRpt == null) return -6;
            if (lstStpRpt.Count <= 0) return -7;
            return (int)(lstStpRpt[0].TEST_CASE_ID ?? -8);
        }



        private List<V_TEST_DATA_REPORT_SUMMARYDTO> CurrentDataResultToShow = null;
        private int IndexOfCurrentDataRow = 0;
        public bool BeginFetchRows()
        {
            CurrentDataResultToShow = null;
            if (GridData.Equals(default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>)))
            {
                Logger.Error("BeginFetchRows", "Data is null.");
                return false;
            }
            if (GridData.Value.Keys == null)
            {
                Logger.Error("BeginFetchRows", "No data exists in Dictionary");
                return false;
            }

            if (GridData.Value.Keys.Count <= DataKeyIndex)
            {
                Logger.Warnning("BeginFetchRows", string.Format("Index [{1}] is out of range of data container:[{0}]", GridData.Value.Keys.Count, DataKeyIndex));
                return false;
            }
            int iKey = GridData.Value.Keys.ElementAt(DataKeyIndex);
            CurrentDataResultToShow = GridData.Value[iKey];
            return true;
        }

        public void EndFetchRow()
        {
            IndexOfCurrentDataRow = -1;
        }

        private byte[] currentImgData;
        /// <summary>
        /// ???n???,?????????size
        /// </summary>
        /// <param name="strSrc"></param>
        /// <param name="iRowCnt"></param>
        /// <param name="iSize"></param>
        /// <returns></returns>
        private string getStringWithRowsAndSize(string strSrc, int iRowCnt, int iSize)
        {
            int icurrentRowCnt = 0;
            bool bCnt = strSrc.Length > iSize;
            string[] arrNoReturns = strSrc.Split(new string[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string strRslt = "";
            while (bCnt)
            {
                strRslt = string.Format("{0}{1}", strRslt == "" ? "" : (strRslt + "\r\n"), arrNoReturns[icurrentRowCnt++]);
                bCnt = (icurrentRowCnt <= iRowCnt) && (strRslt.Length < iSize) && (icurrentRowCnt < arrNoReturns.Length);
            }
            return strRslt;
        }
        public List<KeyValuePair<string, string>> FetchOneRowData()
        {
            List<KeyValuePair<string, string>> lstRslt = new List<KeyValuePair<string, string>>();
            if (CurrentDataResultToShow == null)
            {
                Logger.Error("FetchOneRowData", "Row data is error");
                return null;
            }
            if (IndexOfCurrentDataRow >= CurrentDataResultToShow.Count)
            {
                Logger.Error("FetchOneRowData", "come to the end of data.");
                return null;
            }

            /// Create data for represent
            /// 
            /// as data is created for 
            lstRslt.Add(new KeyValuePair<string, string>(cnst_runorder_teststep, (IndexOfCurrentDataRow + 1).ToString()));
            lstRslt.Add(new KeyValuePair<string, string>(cnst_keyword, CurrentDataResultToShow[IndexOfCurrentDataRow].KEY_WORD_NAME ?? ""));
            lstRslt.Add(new KeyValuePair<string, string>(cnst_objectName, CurrentDataResultToShow[IndexOfCurrentDataRow].OBJECT_HAPPY_NAME ?? ""));
            lstRslt.Add(new KeyValuePair<string, string>(cnst_rowColumnSetting, CurrentDataResultToShow[IndexOfCurrentDataRow].COLUMN_ROW_SETTING ?? ""));
            lstRslt.Add(new KeyValuePair<string, string>(cnst_inputData, CurrentDataResultToShow[IndexOfCurrentDataRow].INPUT_VALUE_SETTING ?? ""));
            string strReturnData = CurrentDataResultToShow[IndexOfCurrentDataRow].RETURN_VALUES ?? "";
            if ((strReturnData.Split('\r', '\n').Length > 10))
                strReturnData = getStringWithRowsAndSize(strReturnData, 9, 200) + "\r\n......";
            lstRslt.Add(new KeyValuePair<string, string>(cnst_returnData, strReturnData));
            currentImgData = CurrentDataResultToShow[IndexOfCurrentDataRow].INFO_PIC;

            string strRunRslt = CurrentDataResultToShow[IndexOfCurrentDataRow].RUNNING_RESULT_INFO ?? "";
            if (strRunRslt.ToUpper().StartsWith("SUCCE"))
                strRunRslt = "SUCCESS";
            lstRslt.Add(new KeyValuePair<string, string>(cnst_status, strRunRslt));
            lstRslt.Add(new KeyValuePair<string, string>(cnst_img, currentImgData == null ? "N/A" : currentImgData.Length > 0 ? "Below" : "N/A"));
            IndexOfCurrentDataRow++;
            return lstRslt;
        }
        private const string cnst_runorder_teststep = "No.";
        private const string cnst_keyword = "Keyword";
        private const string cnst_objectName = "Object Name";
        private const string cnst_rowColumnSetting = "Row_Column";
        private const string cnst_inputData = "Input Data";
        private const string cnst_returnData = "Data Return";
        private const string cnst_status = "Status";
        private const string cnst_img = "Image";
        private static List<KeyValuePair<string, int>> TestStepReportHeader = new List<KeyValuePair<string, int>>()
        {
            { new KeyValuePair<string, int>(cnst_runorder_teststep,20) },
            { new KeyValuePair<string, int>(cnst_keyword,80) },
            { new KeyValuePair<string, int>(cnst_objectName,110) },
            { new KeyValuePair<string, int>(cnst_rowColumnSetting,60) },
            { new KeyValuePair<string, int>(cnst_inputData,60) },
            { new KeyValuePair<string, int>(cnst_returnData,80) },
            { new KeyValuePair<string, int>(cnst_status,50) },
            { new KeyValuePair<string, int>(cnst_img,30) }
        };

        public List<KeyValuePair<string, int>> GetGridColumnInfo()
        {
            return TestStepReportHeader;
        }

        public bool MoveToNextRow()
        {
            throw new NotImplementedException();
        }

       

        private const int cnst_radius = 120;
        public int GetRadius()
        {
            return cnst_radius;
        }

        public List<KeyValuePair<string, double>> GetPartsInfo()
        {
            /// for test return static data
            /// 
            List<KeyValuePair<string, double>> lstRslt = new List<KeyValuePair<string, double>>();
            lstRslt.Add(new KeyValuePair<string, double>("Procced", 0.40));
            lstRslt.Add(new KeyValuePair<string, double>("Failed", 0.40));
            lstRslt.Add(new KeyValuePair<string, double>("Unprocced", 0.20));
            return lstRslt;
        }


        public int GetTopLevelLoopCount()
        {
            /// return Loop_id count
            /// 
            return GridData.Equals(default(KeyValuePair<int, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>>)) ? 0 : GridData.Value.Keys == null ? 0 : GridData.Value.Keys.Count;
        }
        private int DataKeyIndex = 0;
        public void SetCurrentLoopId(int iLoopId)
        {
            //throw new NotImplementedException();
            DataKeyIndex = iLoopId;

        }

        public byte[] GetExtendImgData()
        {
            return currentImgData;
        }
    }

    class TestStoryBoardSummayGridData 
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestStoryBoardSummayGridData));
        #region member vars
        private long currentStoryBoardId;
        private List<V_STORYBOARD_TEST_FULLVISIONDTO> currentStoryBoardInfo = null;
        private int currentrowIndex = -1;
        #endregion //member vars

        public TestStoryBoardSummayGridData(long currentStoryBoardId)
        {
            this.currentStoryBoardId = currentStoryBoardId;
        }
        /// <summary>
        /// Prepare data and set the current Row Index to the first. 
        /// </summary>
        /// <returns>false, no data; true: has row</returns>
        public bool BeginFetchRows()
        {
            Logger.logBegin("BeginFetchRows");
            currentStoryBoardInfo = B_V_STORYBOARD_TEST_FULLVISION.GetStoryBoards(this.currentStoryBoardId);
            /// sorted
            currentStoryBoardInfo = new List<V_STORYBOARD_TEST_FULLVISIONDTO>(currentStoryBoardInfo.OrderBy(p => p.RUN_ORDER));
            return (currentrowIndex = currentStoryBoardInfo == null ? -1 : 0) >= 0;
        }

        public void EndFetchRow()
        {
            currentStoryBoardInfo = null;
            currentrowIndex = -1;
        }

        public List<KeyValuePair<string, string>> FetchOneRowData()
        {
            Logger.logBegin("FetchOneRowData");
            if (currentStoryBoardInfo == null) return null;
            if (currentrowIndex == -1)
                currentrowIndex = 0;
            if (currentrowIndex >= currentStoryBoardInfo.Count) return null;
            List<KeyValuePair<string, string>> dicResult = new List<KeyValuePair<string, string>>();
            dicResult.Add(new KeyValuePair<string, string>(cnst_no, string.Format(" {0}", currentStoryBoardInfo[currentrowIndex].RUN_ORDER)));
            dicResult.Add(new KeyValuePair<string, string>(cnst_StepName, " " + currentStoryBoardInfo[currentrowIndex].ALIAS_NAME ?? ""));
            dicResult.Add(new KeyValuePair<string, string>(cnst_TestSuite, " " + currentStoryBoardInfo[currentrowIndex].TEST_SUITE_NAME ?? ""));
            dicResult.Add(new KeyValuePair<string, string>(cnst_Testcase, " " + currentStoryBoardInfo[currentrowIndex].TEST_CASE_NAME ?? ""));
            dicResult.Add(new KeyValuePair<string, string>(cnst_Dataset, " " + currentStoryBoardInfo[currentrowIndex].DATA_SET_ALIAS_NAME ?? ""));
            dicResult.Add(new KeyValuePair<string, string>(cnst_Error, " " + currentStoryBoardInfo[currentrowIndex].HIST_TEST_RESULT_IN_TEXT ?? ""));
            dicResult.Add(new KeyValuePair<string, string>(cnst_StartEnd, string.Format(" {0}\r\n {1}", currentStoryBoardInfo[currentrowIndex].TEST_CASE_BEGIN_TIME == null ? "" : ((DateTime)currentStoryBoardInfo[currentrowIndex].TEST_CASE_BEGIN_TIME).ToString("MM/dd/yyyy HH:mm:ss"),
                currentStoryBoardInfo[currentrowIndex].TEST_CASE_END_TIME == null ? "" : ((DateTime)currentStoryBoardInfo[currentrowIndex].TEST_CASE_END_TIME).ToString("MM/dd/yyyy HH:mm:ss"))));
            currentrowIndex++;
            return dicResult;
        }
        private const string cnst_no = "No.";
        private const string cnst_StepName = "Step Name";
        private const string cnst_TestSuite = "Test Suite";
        private const string cnst_Testcase = "Test Case";
        private const string cnst_Error = "Error";
        private const string cnst_StartEnd = "Start/End";
        private const string cnst_Dataset = "Dataset";
        private readonly static List<KeyValuePair<string, int>> defaultHeader = new List<KeyValuePair<string, int>>
        {
            new KeyValuePair<string,int>( cnst_no,20 ),
             new KeyValuePair<string,int>(cnst_StepName,80 ),
             new KeyValuePair<string, int>(cnst_TestSuite,80 ),
             new KeyValuePair<string, int>(cnst_Testcase, 100),
             new KeyValuePair<string, int>(cnst_Dataset,60 ),
             new KeyValuePair<string, int>(cnst_Error,60 ),
             new KeyValuePair<string, int>(cnst_StartEnd,60 )
        };
        /// <summary>
        /// Get storyboard data from database and set to Pdf report enginne
        /// </summary>
        /// <returns></returns>
        public List<KeyValuePair<string, int>> GetGridColumnInfo()
        {
            Logger.logBegin("TestStoryBoardSummayGridData");
            /// this is static values
            /// 
            return defaultHeader;
        }

        public bool MoveToNextRow()
        {
            throw new NotImplementedException();
        }

        public byte[] GetExtendImgData()
        {
            return null;
        }
    }

}
