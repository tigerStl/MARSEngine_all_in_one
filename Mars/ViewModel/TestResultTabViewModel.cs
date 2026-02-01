using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel
{
    internal class TestResultTableToDisplay
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestResultTableToDisplay));

        public string Key { get; set; }
        public string ObjectHappyName { get; set; }
        public string BaseLineData { get;
            set;
        }
        public string NoneBaseLineData { get; set; }
        public bool isToCompare { get; set; }
        /// <summary>
        /// Added 2019 0402
        /// 因为存在对象重用问题，需要通过runorder 判断
        /// </summary>
        public Nullable<Decimal> run_order { get; set; }
        //end

        private string toleranceFunction;

        /// <summary>
        /// Tolerance function is used for extending. Untill 2016-feb, it is not used.
        /// </summary>
        public string ToleranceFunction
        {
            get
            {
                return toleranceFunction;
                
            }
            set
            {
                toleranceFunction = value;
                ToleranceFuncObject = Mars.Utility.ToleranceMgr.MarsBasicToleranceFunc.FromFuncString(toleranceFunction);
            }
        }
        private Mars.Utility.ToleranceMgr.MarsBasicToleranceFunc ToleranceFuncObject = null;
        public string CompareResult
        {
            get {
                if (ToleranceFuncObject!=null)
                {
                    bool isOk = true;
                    string strError = "";
                    string strResult = ToleranceFuncObject.CompareDataAsString(BaseLineData, NoneBaseLineData, ref isOk, ref strError);
                    if (isOk) return strResult;
                    if (!string.IsNullOrEmpty(strError))
                    {
                        Logger.Error("CompareResult", strError);                        
                    }
                    return strResult;
                }
                return isToCompare?(string.Compare(BaseLineData == null ? "" : BaseLineData, NoneBaseLineData == null ? "" : NoneBaseLineData, true) == 0 ? "TRUE" : "FALSE"):"";
            }
        }
        public TestResultTableToDisplay()
        {
            isToCompare = false;
        }
    }


    internal class DataResultForTab : INotifyPropertyChanged
    {
        private string hearder;
        public string Header
        {
            get
            { return hearder; }
            set
            {
                hearder = value;
                RaisePropertyChanged("Header");
            }
        }

        private ObservableCollection<TestResultTableToDisplay> loopCompareResult = null;
        public ObservableCollection<TestResultTableToDisplay> LoopCompareReuslt
        {
            get { return loopCompareResult; }
            set { loopCompareResult = value;RaisePropertyChanged("LoopCompareReuslt"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
    }
    internal class TestResultTabViewModel : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestResultTabViewModel));
        
        private ObservableCollection<DataResultForTab> resultTestReport;
        public ObservableCollection<DataResultForTab> ResultTestReport
        {
            get { return resultTestReport; }
            set {
                resultTestReport = value;
                RaisePropertyChanged("ResultTestReport");
            }
        }
        private long? currentStoryBoardDetailId = null;
        private long? currentStoryBoardId = null;
        public TestResultTabViewModel(long? storyBoardDetailId=null, long? storyBoardDetailId_Cmp=null, long? storyboardId=null)
        {
            currentStoryBoardDetailId = storyBoardDetailId;
            this.currentStoryBoardId = storyboardId;
            this.currentCmpStoryBoardDetailId = storyBoardDetailId_Cmp;
            if(currentStoryBoardDetailId==null)
                CreateDefaultReportData();
            else
            {
                if (storyBoardDetailId_Cmp == null)
                {

                    CreateReportData();
                }
                else
                {
                    CreateReportDataWithCmp();
                }
            }
            RaisePropertyChanged("ResultTestReport");
        }

        private long? currentCmpStoryBoardDetailId = null;
        
        private void CreateReportDataWithCmp()
        {
            Logger.logBegin("CreateReportDataWithCmp");
            B_V_TEST_DATA_REPORT_SUMMARY bTestReport = new B_V_TEST_DATA_REPORT_SUMMARY();
            bool hasException = false;
            string strError = "";
            bool isBaseDataManully = bTestReport.IsBaseLineDataDirectlyInputtedManully(MarsMainWindow.CurrentDatabaseIdx, 
                this.currentStoryBoardDetailId, ref strError, ref hasException);
            if (hasException)
            {
                Logger.Error("CreateReportDataWithCmp",string.Format("Error:[{0}] after call IsBaseLineDataDirectlyInputtedManully",strError));
                return;
            }
            if (!isBaseDataManully)
            {
                /// dictionary<storyboard detail Id, dictionary<loopId,List<Data>>
                Dictionary<long, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> dicDataFromDBForDetailId 
                    = bTestReport.GetTestReportByStoryBoardDetailId(MarsMainWindow.CurrentDatabaseIdx, this.currentStoryBoardDetailId, this.currentCmpStoryBoardDetailId);
                NormalizationDataFromDatabase(dicDataFromDBForDetailId);
            }
            else
            {
                /// get data from baseline data table 
                /// 
                B_V_BASE_LINEDATA objBaseLineData = new B_V_BASE_LINEDATA();
                List<V_BASE_LINEDATADTO> lstBaseData = objBaseLineData.GetBaseLindDataViaStoryboardDetailId(this.currentStoryBoardDetailId,ref strError, ref hasException);
                if (hasException)
                {
                    Logger.Error("CreateReportDataWithCmp",string.Format("Error when call GetBaseLindDataViaStoryboardDetailId:\r\n[{0}]",strError));
                    return;
                }
#if db4SQL
                Dictionary<short, List<V_BASE_LINEDATADTO>> dicBaseData = lstBaseData.GroupBy(p => (short)p.LOOP_ID).ToDictionary(p => p.Key, v => v.ToList());
#else
                Dictionary<short, List<V_BASE_LINEDATADTO>> dicBaseData = lstBaseData.GroupBy(p => p.LOOP_ID).ToDictionary(p => p.Key, v => v.ToList());
#endif
                Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> dicTestNonCmp =bTestReport.GetHisRptDataByStoryboardDetailIdAndTestMode(currentStoryBoardDetailId, 0);
                NormalizationDataFromDatabase(dicBaseData, dicTestNonCmp);
            }
            
            if (resultTestReport.Count == 0)
            {
                CreateDefaultReportData();
            }
            else
            {
                if (resultTestReport.Count == 1)
                {
                    if (resultTestReport[0].LoopCompareReuslt == null || resultTestReport[0].LoopCompareReuslt.Count == 0)
                    {
                        CreateDefaultReportData();
                    }
                }
            }
        }

        

        private void CreateReportData()
        {
            
            Logger.logBegin("CreateReportData");
            try
            {

            
            B_V_TEST_DATA_REPORT_SUMMARY bTestReport = new B_V_TEST_DATA_REPORT_SUMMARY();
#if PERFORMANCE_TRACKING
            Logger.Info("Performance.....Trace....", "GetTestReportByStoryBoardDetailId begin");
#endif
                string strError = "";
                Logger.Info("\t", "getTestStpReportDataByTestStoryBoardIdWithDataSrc begin");
                var r = bTestReport.getTestStpReportDataByTestStoryBoardIdWithDataSrc(MarsMainWindow.CurrentDatabaseIdx, this.currentStoryBoardId??-1, ref strError, 0);
                Logger.Info("\t", "getTestStpReportDataByTestStoryBoardIdWithDataSrc end");

            Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> dicDataFromDBForDetailId 
                = bTestReport.GetTestReportByStoryBoardDetailId(MarsMainWindow.CurrentDatabaseIdx, this.currentStoryBoardDetailId);
            List<T_TEST_STEPSDTO> lstSteps = B_TEST_STEPS.GetTestStepViaDetailId(MarsMainWindow.CurrentDatabaseIdx, this.currentStoryBoardDetailId);
            /// convert data into DataGrid Accessable format
            /// 
            /// normalization data from database
            /// 
#if PERFORMANCE_TRACKING
            Logger.Info("Performance.....Trace....", "GetTestReportByStoryBoardDetailId end");
#endif
#if PERFORMANCE_TRACKING
            Logger.Info("Performance.....Trace....", "NormalizationDataFromDatabase begin");
#endif
            NormalizationDataFromDatabase(dicDataFromDBForDetailId, lstSteps);
#if PERFORMANCE_TRACKING
            Logger.Info("Performance.....Trace....", "NormalizationDataFromDatabase end");
#endif
            if (resultTestReport.Count==0)
            {
                CreateDefaultReportData();
            }
            else
            {
#if PERFORMANCE_TRACKING
                Logger.Info("Performance.....Trace....", "CreateDefaultReportData begin");
#endif
                if (resultTestReport.Count==1)
                {
                    if (resultTestReport[0].LoopCompareReuslt==null || resultTestReport[0].LoopCompareReuslt.Count==0)
                    {
                        CreateDefaultReportData();
                    }
                }
#if PERFORMANCE_TRACKING
                Logger.Info("Performance.....Trace....", "CreateDefaultReportData end");
#endif
            }
            }
            finally
            {
                Logger.logEnd("CreateReportData");
            }
        }

        

        const string STR_CAPTURE = "CAPTUREVALUE";
        const string STR_CAPTURECOMP = "CAPTUREANDCOMPARE";
        const string STR_CAPTURECOMPBYKEY = "CAPTUREANDCOMPAREBYKEY";

        private void NormalizationDataFromDatabase(Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> dicDataFromDBForDetailId, List<T_TEST_STEPSDTO> lstStpInfo)
        {
            Logger.Info("NormalizationDataFromDatabase", string.Format("Item Count:[{0}]", dicDataFromDBForDetailId.Count));
            resultTestReport = new ObservableCollection<DataResultForTab>();
            int iKeywordMode = 0;

            List<T_TEST_STEPSDTO> lstTmpStpInfo = lstStpInfo == null ? new List<T_TEST_STEPSDTO>() : lstStpInfo;

            foreach (int iKey in dicDataFromDBForDetailId.Keys)
            {
                IEnumerable<V_TEST_DATA_REPORT_SUMMARYDTO> lstRptData = dicDataFromDBForDetailId[iKey].OrderBy(p => p.OBJECT_HAPPY_NAME??p.INPUT_VALUE_SETTING);
                //var obj = lstRptData.OrderBy(p=>p.TEST_REPORT_STEP_ID).GroupBy(p =>new { p.TEST_MODE ,p.RUN_ORDER }).Where(p => p.Count() > 1); 
                //20190402 加入对象重用处理
                var obj = lstRptData.OrderBy(p => p.TEST_REPORT_STEP_ID).GroupBy(p => new
                {
                    m = p.TEST_MODE,
                    n =string.IsNullOrEmpty(p.INPUT_VALUE_SETTING) ? p.OBJECT_HAPPY_NAME : p.INPUT_VALUE_SETTING
                }).Where(p => p.Count() > 1);

                foreach (var objItm in obj)
                {
                    
                    //string strNormalizedObjectName = objItm.ElementAt(0).INPUT_VALUE_SETTING;
                    string strNormalizedObjectName = string.IsNullOrEmpty(objItm.ElementAt(0).INPUT_VALUE_SETTING)?objItm.ElementAt(0).OBJECT_HAPPY_NAME: objItm.ElementAt(0).INPUT_VALUE_SETTING;

                    for (int i=1;i<objItm.Count();i++)
                    {
                        if (string.Compare(objItm.ElementAt(i).KEY_WORD_NAME, STR_CAPTURECOMPBYKEY, true) == 0) continue;
                        (objItm.ElementAt(i)).INPUT_VALUE_SETTING = string.Format("{0}_{1}", strNormalizedObjectName,i);
                    }
                }

                DataResultForTab objCurrentDataRptSet4Tab = new DataResultForTab();
                objCurrentDataRptSet4Tab.Header = string.Format("Dataset_[{0}]", iKey);
                objCurrentDataRptSet4Tab.LoopCompareReuslt = new ObservableCollection<TestResultTableToDisplay>();

                
                //T_TEST_STEPSDTO objCurrentStp =

                foreach (V_TEST_DATA_REPORT_SUMMARYDTO itmDto in lstRptData)
                {
                    if (itmDto == null) continue ;
                    if (itmDto.KEY_WORD_NAME == null) continue;
                    iKeywordMode = ((string.Compare(itmDto.KEY_WORD_NAME, STR_CAPTURE, true) == 0 ))
                        ? 1 
                        : ((string.Compare(itmDto.KEY_WORD_NAME, STR_CAPTURECOMP, true) == 0)
                            ||(string.Compare(itmDto.KEY_WORD_NAME, STR_CAPTURECOMPBYKEY, true) == 0)) 
                            ? 2 : -1;
                    if (iKeywordMode == -1) continue;
                    // 这里可能存在问题。如果对象被重用，需要额外的方式
                    string strKey = string.Format("{0}", string.IsNullOrEmpty(itmDto.INPUT_VALUE_SETTING) ? itmDto.OBJECT_HAPPY_NAME : itmDto.INPUT_VALUE_SETTING);
                    /// to find 
                    TestResultTableToDisplay objData2Display = objCurrentDataRptSet4Tab.LoopCompareReuslt.Where(p => p.Key == strKey).FirstOrDefault();
                    ///对象重用判断极其处理
                    ///
                    //if (objData2Display!=null)
                    //{
                    //    if (objData2Display.run_order == itmDto.RUN_ORDER) //相同的对象及step
                    //    {

                    //    }
                    //    else
                    //    {
                    //        strKey = string.Format("{0}_{1}", string.IsNullOrEmpty(itmDto.INPUT_VALUE_SETTING) ? itmDto.OBJECT_HAPPY_NAME : itmDto.INPUT_VALUE_SETTING, itmDto);
                    //    }
                    //}

                    if (objData2Display == null) {
                        objData2Display = new TestResultTableToDisplay();
                        objData2Display.isToCompare = iKeywordMode == 2;
                        objCurrentDataRptSet4Tab.LoopCompareReuslt.Add(objData2Display);
                        objData2Display.ObjectHappyName = strKey;
                        objData2Display.Key = strKey;
                        objData2Display.run_order = itmDto.RUN_ORDER;

                        T_TEST_STEPSDTO objStpesInfo = lstTmpStpInfo.Where(p => p.STEPS_ID == itmDto.STEPS_ID).FirstOrDefault();
                        string strTolFunc = "";
                        if ((objStpesInfo!=null)&&(B_TEST_STEPS.StepHasTorlarenceInfo(objStpesInfo, ref strTolFunc)))
                        {
                            objData2Display.ToleranceFunction = strTolFunc;
                        }
                    }
                    
                    if (itmDto.TEST_MODE==1)
                    {
                        objData2Display.BaseLineData =BoHelper.CorrectResultData(itmDto.RETURN_VALUES);
                    }
                    else
                    {
                        objData2Display.NoneBaseLineData = BoHelper.CorrectResultData(itmDto.RETURN_VALUES);
                    }
                }

                resultTestReport.Add(objCurrentDataRptSet4Tab);
            }
        }

        private void NormalizationDataFromDatabase(Dictionary<short, List<V_BASE_LINEDATADTO>> lstBaseData, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> dicTestNonCmp)
        {
            Logger.Info("NormalizationDataFromDatabase",string.Format("Item Count for baseData:[{0}], Item Count for non-basedata:[{1}]",lstBaseData==null?0: lstBaseData.Count, dicTestNonCmp==null?0:dicTestNonCmp.Count));
            resultTestReport = new ObservableCollection<DataResultForTab>();

            /// get all loops 
            List<int> lstAllLoops = new List<int>();
            if (lstBaseData==null && dicTestNonCmp==null)
            {
                // no result data 
                return;
            }
            if (lstBaseData != null)
                foreach (short s in lstBaseData.Keys)
                {
                    lstAllLoops.Add(s);
                }
            if (dicTestNonCmp != null)
                foreach (long? l in dicTestNonCmp.Keys)
                {
                    if (l == null) continue;
                    lstAllLoops.Add((int)l);
                }
            lstAllLoops = new List<int>(lstAllLoops.Distinct());
            for (int i=0;i<lstAllLoops.Count;i++)
            {
                List<string> listObjHappyName = new List<string>();
                if (lstBaseData.ContainsKey((short)lstAllLoops[i]))
                {
                    var lstBaseHappyNames = from lstHpp in lstBaseData[(short)lstAllLoops[i]]
                                            select lstHpp.OBJECT_HAPPY_NAME;
                    listObjHappyName.AddRange(lstBaseHappyNames);
                }
                if (dicTestNonCmp.ContainsKey(lstAllLoops[i]))
                {
                    NormalizationDataSub(ref dicTestNonCmp, lstAllLoops[i]);
                    foreach (V_TEST_DATA_REPORT_SUMMARYDTO objRpItm in dicTestNonCmp[lstAllLoops[i]]
                        .Where(p => (string.Compare(STR_CAPTURE, p.KEY_WORD_NAME, true) == 0) 
                                    || (string.Compare(STR_CAPTURECOMP, p.KEY_WORD_NAME, true) == 0)
                                    || (string.Compare(STR_CAPTURECOMPBYKEY, p.KEY_WORD_NAME, true) == 0)
                                    ))
                    {
                        listObjHappyName.Add(objRpItm.INPUT_VALUE_SETTING);
                    }
                }
                listObjHappyName = new List<string>(listObjHappyName.Distinct());

                DataResultForTab objCurrentDataRptSet4Tab = new DataResultForTab();
                objCurrentDataRptSet4Tab.Header = string.Format("Dataset_[{0}]", i);
                objCurrentDataRptSet4Tab.LoopCompareReuslt = new ObservableCollection<TestResultTableToDisplay>();

                foreach (string strObjHappy in listObjHappyName)
                {
                    string strBaseInfo = GetRptDataFromDictionaryList(lstBaseData[(short)lstAllLoops[i]], strObjHappy);
                    string strCmpInfo = GetRptDataFromDictionaryList(dicTestNonCmp[lstAllLoops[i]], strObjHappy);

                    TestResultTableToDisplay objRsltToDisplay = new TestResultTableToDisplay();
                    objRsltToDisplay.isToCompare = true;
                    objRsltToDisplay.ObjectHappyName = strObjHappy;
                    objRsltToDisplay.Key = strObjHappy;

                    objRsltToDisplay.BaseLineData = BoHelper.CorrectResultData(strBaseInfo);
                    objRsltToDisplay.NoneBaseLineData = BoHelper.CorrectResultData(strCmpInfo);

                    objCurrentDataRptSet4Tab.LoopCompareReuslt.Add(objRsltToDisplay);
                }
                resultTestReport.Add(objCurrentDataRptSet4Tab);
            }
        }

        private void NormalizationDataFromDatabase(Dictionary<long, Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>> dicDataFromDBForDetailId)
        {
            Logger.Info("NormalizationDataFromDatabase dictionary-dictionary", string.Format("Item Count:[{0}]", dicDataFromDBForDetailId.Count));
            resultTestReport = new ObservableCollection<DataResultForTab>();
            //int iKeywordMode = 0;
            Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> bsLineDictionary = 
                dicDataFromDBForDetailId.ContainsKey(this.currentStoryBoardDetailId??-1)? dicDataFromDBForDetailId[this.currentStoryBoardDetailId ?? -1]
                :new Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>();
            Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> cmpDictionary = 
                dicDataFromDBForDetailId.ContainsKey(this.currentCmpStoryBoardDetailId ?? -1)? dicDataFromDBForDetailId[this.currentCmpStoryBoardDetailId??-1]
                :new Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>();
            //if (bsLineDictionary == null)
            //    bsLineDictionary = new Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>();
            //if (cmpDictionary == null)
            //    cmpDictionary = new Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>>();

            /// get all loops 
            List<int> lstAllLoops = new List<int>();
            lstAllLoops.AddRange(bsLineDictionary.Keys);
            lstAllLoops.AddRange(cmpDictionary.Keys);
            lstAllLoops = new List<int>(lstAllLoops.Distinct());

            ///combine all Objects for each Loop
            /// 
            for(int i=0;i<lstAllLoops.Count;i++)
            {
                ///steps:
                /// 1, get all objects names for the current Loop
                /// 2, find one by one
                /// 
                List<string> lstObjectHappyNames = new List<string>();
#region Normaliaztion baseline data
                if (bsLineDictionary.ContainsKey(lstAllLoops[i]))
                {
                    NormalizationDataSub(ref bsLineDictionary, lstAllLoops[i]);
                    foreach(V_TEST_DATA_REPORT_SUMMARYDTO objRpItm in bsLineDictionary[lstAllLoops[i]]
                        .Where(p=>(string.Compare(STR_CAPTURE,p.KEY_WORD_NAME,true)==0)
                                ||(string.Compare(STR_CAPTURECOMP, p.KEY_WORD_NAME, true) == 0)
                                || (string.Compare(STR_CAPTURECOMPBYKEY, p.KEY_WORD_NAME, true) == 0))
                                )
                    {
                        lstObjectHappyNames.Add(objRpItm.INPUT_VALUE_SETTING);
                    }
                }
#endregion //Normaliaztion baseline data
#region Normaliaztion CMP data
                if (cmpDictionary.ContainsKey(lstAllLoops[i]))
                {
                    NormalizationDataSub(ref cmpDictionary, lstAllLoops[i]);
                    foreach (V_TEST_DATA_REPORT_SUMMARYDTO objRpItm in cmpDictionary[lstAllLoops[i]]
                        .Where(p => (string.Compare(STR_CAPTURE, p.KEY_WORD_NAME, true) == 0) 
                                || (string.Compare(STR_CAPTURECOMP, p.KEY_WORD_NAME, true) == 0)
                                || (string.Compare(STR_CAPTURECOMPBYKEY, p.KEY_WORD_NAME, true) == 0))
                                )
                    {
                        lstObjectHappyNames.Add(objRpItm.INPUT_VALUE_SETTING);
                    }
                }
#endregion //Normaliaztion CMP data

                /// combine all objects name togerhter
                /// 
                lstObjectHappyNames = new List<string>(lstObjectHappyNames.Distinct());

                DataResultForTab objCurrentDataRptSet4Tab = new DataResultForTab();
                objCurrentDataRptSet4Tab.Header = string.Format("Dataset_[{0}]", i);
                objCurrentDataRptSet4Tab.LoopCompareReuslt = new ObservableCollection<TestResultTableToDisplay>();

                foreach(string strObjHappy  in lstObjectHappyNames)
                {
                    string strBaseInfo = GetRptDataFromDictionaryList(bsLineDictionary[lstAllLoops[i]], strObjHappy);
                    string strCmpInfo = GetRptDataFromDictionaryList(cmpDictionary[lstAllLoops[i]], strObjHappy);

                    TestResultTableToDisplay objRsltToDisplay = new TestResultTableToDisplay();
                    objRsltToDisplay.isToCompare = true;
                    objRsltToDisplay.ObjectHappyName = strObjHappy;
                    objRsltToDisplay.Key = strObjHappy;

                    objRsltToDisplay.BaseLineData = BoHelper.CorrectResultData(strBaseInfo);
                    objRsltToDisplay.NoneBaseLineData = BoHelper.CorrectResultData(strCmpInfo);

                    objCurrentDataRptSet4Tab.LoopCompareReuslt.Add(objRsltToDisplay);
                }
                resultTestReport.Add(objCurrentDataRptSet4Tab);
            }
        }

        private string GetRptDataFromDictionaryList(List<V_BASE_LINEDATADTO> lstDataToSearch, string strObjHappy)
        {
            if (lstDataToSearch == null) return "";
            if (!lstDataToSearch.Any(p => string.Compare(p.OBJECT_HAPPY_NAME, strObjHappy, true) == 0)) return "";
            return lstDataToSearch.FirstOrDefault(p => string.Compare(p.OBJECT_HAPPY_NAME, strObjHappy, true) == 0).OBJECT_HAPPY_NAME;
        }

        private string GetRptDataFromDictionaryList(List<V_TEST_DATA_REPORT_SUMMARYDTO> lstDataToSearch, string strObjHappy)
        {
            if (lstDataToSearch == null) return "";
            if (!lstDataToSearch.Any(p => string.Compare(p.INPUT_VALUE_SETTING, strObjHappy, true) == 0)) return "";
            return lstDataToSearch.FirstOrDefault(p => string.Compare(p.INPUT_VALUE_SETTING, strObjHappy, true) == 0).RETURN_VALUES; 
        }

        private void NormalizationDataSub(ref Dictionary<int, List<V_TEST_DATA_REPORT_SUMMARYDTO>> objLstToBeChange, int iKey)
        {
            IList<V_TEST_DATA_REPORT_SUMMARYDTO> lstRptData = objLstToBeChange[iKey];
            var obj = lstRptData.OrderBy(p => p.TEST_REPORT_STEP_ID).GroupBy(p => new { p.TEST_MODE, p.RUN_ORDER }).Where(p => p.Count() > 1);
            foreach (var objItm in obj)
            {
                string strNormalizedObjectName = objItm.ElementAt(0).INPUT_VALUE_SETTING;

                for (int ij = 1; ij < objItm.Count(); ij++)
                {
                    (objItm.ElementAt(ij)).INPUT_VALUE_SETTING = string.Format("{0}_{1}", strNormalizedObjectName, ij);
                }
            }
        }

        private void CreateDefaultReportData()
        {
            resultTestReport = new ObservableCollection<DataResultForTab>()
            {
                new DataResultForTab() {Header="Hint",
                    LoopCompareReuslt =new ObservableCollection<TestResultTableToDisplay> {
                        new TestResultTableToDisplay { ObjectHappyName="NO Data",BaseLineData="No Data", NoneBaseLineData="No Data"},
                //        new TestResultTableToDisplay { ObjectHappyName="TRADE_ID",BaseLineData="100020GE", NoneBaseLineData="100020GE"},
                //        new TestResultTableToDisplay { ObjectHappyName="RECEIVE_CCY",BaseLineData="USD", NoneBaseLineData="CNY"},
                //        new TestResultTableToDisplay { ObjectHappyName="PAY_NOTIONAL",BaseLineData="10M", NoneBaseLineData="10.4M"},
                //    }
                //}
                //,

                //new  DataResultForTab() {Header="Loop_2",
                //    LoopCompareReuslt =new ObservableCollection<TestResultTableToDisplay> {
                //        new TestResultTableToDisplay { ObjectHappyName="TRADE_ID",BaseLineData="100020GE", NoneBaseLineData="100022GE"},
                //        new TestResultTableToDisplay { ObjectHappyName="RECEIVE_CCY",BaseLineData="USD", NoneBaseLineData="CNY"},
                //        new TestResultTableToDisplay { ObjectHappyName="PAY_NOTIONAL",BaseLineData="10M", NoneBaseLineData="10.4M"},

                   }
                }
            };
        }
    }
}
