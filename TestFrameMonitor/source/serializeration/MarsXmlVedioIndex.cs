namespace TestFrameMonitor.source.serializeration
{
#if _VEDIO_TIGER_
    /**
     * 将step 数据存入一个xml文件 该文件可以作为report的源消息 也可以作为vedio的索引
     **/
    [Serializable]
    [XmlRoot(ElementName = "MarsTRoot")]
    public class MarsXmlVedioIndex
    {
        static MLogger Logger = MLogger.GetLogger(typeof(MarsXmlVedioIndex));

    #region const 
        internal const string cnst_node_name = "TestStep";
        internal const string cnst_step_root = "MarsT_Steps";
        internal const string cnst_start_time = "TestStartTime";
        internal const string cnst_test_project = "TestProject";
        internal const string cnst_test_suite = "TestSuite";
        internal const string cnst_test_loop = "CurrentLoop";
        internal const string cnst_project_relyid = "RelyID";
    #endregion

    #region XML Nodes
        
        [XmlElement(cnst_start_time)]
        public DateTime TestStartTime { get; set; }
        [XmlElement(cnst_test_project)]
        public string TestProjectName { get; set; }
        [XmlElement(cnst_test_suite)]
        public string TestSuiteName { get; set; }
        [XmlElement(cnst_test_loop)]
        public int CurrentLoop { get; set; }
        [XmlElement(cnst_project_relyid)]
        public string ProjectRelyId { get; set; }
        [XmlArrayItem(cnst_node_name)]
        [XmlArray(cnst_step_root)]
        public List<MarsTigerXmlReportItem> TestSteps { get; set; }
    #endregion

        public DateTime GetStartTime()
        {
            return this.TestStartTime;
        }

        public void InitializationTest()
        {
            TestStartTime = DateTime.Now;
            TestSteps = new List<MarsTigerXmlReportItem>();
        }
        public void AddNewStepInfo(string strId, string strKey, string strObject, string strHappy, string strRC, string strData )
        {
            var stpsEx = TestSteps.LastOrDefault();
            if (stpsEx != null)
            {
                if ((string.Compare(stpsEx.StepId, strId) == 0)
                    && (string.Compare(stpsEx.KeyWord, strKey) == 0)) //then the same stp
                    return;
            }
            MarsTigerXmlReportItem objXmlRptInfo = new MarsTigerXmlReportItem();
            objXmlRptInfo.StepId = strId;
            objXmlRptInfo.StartTime = DateTime.Now;
            objXmlRptInfo.EndTime = DateTime.Now;
            objXmlRptInfo.KeyWord = strKey;
            objXmlRptInfo.ObjectHappyName = strHappy;
            objXmlRptInfo.RowAndColumn = strRC;
            objXmlRptInfo.Data = strData;            

            TestSteps.Add(objXmlRptInfo);
        }

        public void AttachDataToLatestStep(string strOutData, bool isResult, string strMsg)
        {
            if (TestSteps == null) return;
            if (TestSteps.Count <= 0) return;

            MarsTigerXmlReportItem objLast = TestSteps.Last<MarsTigerXmlReportItem>();
            objLast.DataOut = strOutData;
            objLast.RunningResult = isResult;
            objLast.Message = strMsg;
            objLast.EndTime = DateTime.Now;
        }
        internal string GetSaveFileWithPath()
        {
            
            string strPath = GetPath();
            
            return string.Format("{0}\\[{4}]_[{1}]_[{2}]_LP[{3}].mti", strPath, MarsXmlVedioIndex.GetNormalizedFileName(this.TestProjectName), MarsXmlVedioIndex.GetNormalizedFileName(this.TestSuiteName), this.CurrentLoop,this.ProjectRelyId);            
        }

        public static string GetVedioOriginalFileName(string strProject, string strTestSuite, int iCurrentId, string strRelyId)
        {
            string strPath = GetVedioPath(); 
            return string.Format("{0}\\wmv\\[{4}]_[{1}]_[{2}]_LP[{3}].xesc", strPath, strProject, strTestSuite, iCurrentId, strRelyId);
        }

        public string GetVedioOrignalFileName()
        {
            //string strPath = GetVedioPath();
            return GetVedioOriginalFileName(MarsXmlVedioIndex.GetNormalizedFileName(this.TestProjectName), MarsXmlVedioIndex.GetNormalizedFileName(this.TestSuiteName), this.CurrentLoop, this.ProjectRelyId);
        }

        string mstrPath = null;
        public string GetAssociatedVedioFile()
        {
            //string strTstSuite = this.CurrentDebugInfo.CurrentTestCaseName.Replace(".", "_").Replace(" ", "_");
            //string strTstCs = this.CurrentDebugInfo.CurrentTestCaseName.Replace(" ", "_");
            //if (m_objRandomGen == null)
            //{
            //    m_objRandomGen = new Random();
            //}
            //strVedioName = string.Format("[{0}]-[{1}]-[LP_{2}]_{3}.xesc", strTstSuite, strTstCs, this.CurrentDebugInfo.TestCurrentLoopId, this.CurrentDebugInfo.CurrentRelyId);// m_objRandomGen.Next(1000));
            Logger.logBegin("GetAssociatedVedioFile");
            try
            {
                if (mstrPath == null) {
#if _NO_C_DRIVER_WRITE
                    mstrPath = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
                    mstrPath = Path.Combine(mstrPath, ".\\Results\\Video");
#else
                    mstrPath = typeof(MarsXmlVedioIndex).Assembly.Location;
                    mstrPath = TigerMarsUtil.GetPathWithoutFileName(mstrPath);
                    mstrPath = Path.Combine(mstrPath, "..\\Results\\Video");
#endif
                    mstrPath = Path.GetFullPath(mstrPath);
                }
                return string.Format("{4}\\wmv\\[{3}]_[{0}]_[{1}]_LP[{2}].wmv", MarsXmlVedioIndex.GetNormalizedFileName(this.TestProjectName), MarsXmlVedioIndex.GetNormalizedFileName(this.TestSuiteName), this.CurrentLoop, this.ProjectRelyId, mstrPath);
            }
            finally
            {
                Logger.logEnd("GetAssociatedVedioFile");
            }
            
        }

        

        public static void Export(MarsXmlVedioIndex objTestSteps)
        {
            //string strExportedXml = XmlHelper.XmlSerialize(objTestSteps, Encoding.UTF8);
            if (objTestSteps == null) return;
            string strPath = objTestSteps.GetSaveFileWithPath();
            /** save to Des file **/
            XmlHelper.XmlSerializeToFile(objTestSteps, strPath, Encoding.UTF8);
        }

        public static MarsXmlVedioIndex Import(string strRelyId, string strPro, string strSuite, int iLoop)
        {
            string strRootPath = GetPath();
            string strFileName = string.Format("{0}\\[{4}]_[{1}]_[{2}]_LP[{3}].mti", strRootPath, strPro, strSuite, iLoop, strRelyId);
            if(!File.Exists(strFileName))
            {
                Logger.Error("Import",string.Format("No such File exists:[{0}]",strFileName));
                return null;
            }
            return Import(strFileName);

        }

        public static MarsXmlVedioIndex Import(string strFileName)
        {
            try
            {
                MarsXmlVedioIndex objResult = XmlHelper.XmlDeserializeFromFile<MarsXmlVedioIndex>(strFileName, Encoding.UTF8);
                objResult.SortList();
                return objResult;
            }
            catch (Exception e)
            {
                Logger.Error("Import", string.Format("Errors when deSerialize from file:[{0}], \r\n\terror:[{1}]", strFileName, e.Message), e);
                return null;
            }
        }

        internal static string GetPath()
        {
#if _NO_C_DRIVER_WRITE
            string strLocation = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");            
            string strPath = Path.Combine(strLocation, ".\\Results\\Index");
#else
            string strLocation = typeof(MarsXmlVedioIndex).Assembly.Location;
            strLocation = TigerMarsUtil.GetPathWithoutFileName(strLocation);
            string strPath = Path.Combine(strLocation, "..\\Results\\Index");
#endif
            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }
            return strPath;
        }
        internal static string GetVedioPath()
        {
#if _NO_C_DRIVER_WRITE
            string strLocation = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
            string strPath = Path.Combine(strLocation, ".\\Results\\Video");
#else
            string strLocation = typeof(MarsXmlVedioIndex).Assembly.Location;
            strLocation = TigerMarsUtil.GetPathWithoutFileName(strLocation);
            string strPath = Path.Combine(strLocation, "..\\Results\\Video");
#endif
            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }
            return strPath;
        }

        internal static string GetNormalizedFileName(string strSrc)
        {
            return strSrc.Replace(" ","_").Replace(".","_");
        }

        public void SortList()
        {
            if (this.TestSteps == null) return;
            //this.TestSteps.Sort((step1, step2) => step1.StepId.CompareTo(step2.StepId));
        }

        public string GetTCSummary()
        {
            return string.Format("Test case includs total [{0}] steps", this.TestSteps==null?0:this.TestSteps.Count);
        }

        internal string GetCaptionByRelativePosition(double dCurrnt)
        {
            if (dCurrnt == 0) return TestSteps[0].GetCommandForCommandScript();
            for(int i = 0; i < this.TestSteps.Count; i++)
            {
                /*
                    Logger.Info("......Time.....", string.Format("dCurrent:[{0}], i:[{1}],\r\n Start Time:[{2}],\r\n test start:[{3}],\r\n end time:[{4}],\r\n totalSenconds:[{5}]",
                    dCurrnt,i, this.TestSteps[i].StartTime, this.TestStartTime,this.TestSteps[i].EndTime, (this.TestSteps[i].StartTime - this.TestStartTime).TotalSeconds));
                */
                if (((this.TestSteps[i].StartTime - this.TestStartTime).TotalSeconds >= dCurrnt) && ((this.TestSteps[i].EndTime - this.TestStartTime).TotalSeconds <= dCurrnt))
                {
                    return TestSteps[i].GetCommandForCommandScript();
                }

                if( ((this.TestSteps[i].StartTime-this.TestStartTime).TotalSeconds>=dCurrnt))
                {
                    if (i>=1)
                        return TestSteps[i].GetCommandForCommandScript();
                    else
                        return TestSteps[i].GetCommandForCommandScript();
                }
                if (((this.TestSteps[i].EndTime - this.TestStartTime).TotalSeconds >= dCurrnt))
                    return TestSteps[i].GetCommandForCommandScript();

            }
            return TestSteps[this.TestSteps.Count - 1].GetCommandForCommandScript();
        }
    }
    
    public class MarsTigerXmlReportItem
    {
        internal const string STEP_ID = "StepID";
        internal const string KEYWORD = "KeyWord";
        internal const string OBJECT = "Object";
        internal const string ROW_COLUMN = "Row_Column";
        
        internal const string INPUTDATA = "Data";
        internal const string OUTPUTDATA = "ReturnedData";
        internal const string START_TIME = "StartTime";
        internal const string END_TIME = "EndTime";
        internal const string RESULT = "Result";
        internal const string MESSAGE = "Message";
        [XmlElement(STEP_ID)]
        public string StepId { get; set; }
        [XmlElement(KEYWORD)]
        public string KeyWord { get; set; }
        [XmlElement(OBJECT)]
        public string ObjectHappyName { get; set; }
        [XmlElement(ROW_COLUMN)]
        public string RowAndColumn { get; set; }
        [XmlElement(INPUTDATA)]
        public string Data { get; set; }
        [XmlElement(OUTPUTDATA)]
        public string DataOut { get; set; }
        [XmlElement(START_TIME)]
        public DateTime StartTime { get; set; }
        [XmlElement(END_TIME)]
        public DateTime EndTime { get; set; }

        [XmlElement(RESULT)]
        public bool RunningResult { get; set; }
        [XmlElement(MESSAGE)]
        public string Message { get; set; }
       
        public string GetSummary()
        {
            return string.Format("[{0}]-[{1}]", KeyWord,ObjectHappyName);
        }

        public string GetCommandForCommandScript()
        {
            return  string.Format("Keyword:\t[{0}] Object\t:[{3}] Rc\t:[{1}] Data\t:[{2}] \r\nStart Time:[{4}] End Time:[{5}]", 
                KeyWord, RowAndColumn, this.Data, this.ObjectHappyName,
                this.StartTime.ToString("yyyyMMdd HH:mm:ss"), this.EndTime.ToString("HH:mm:ss"));
        }
        public TimeSpan GetTimeStartForCaption(DateTime dtStart)
        {
            return this.StartTime - dtStart;
        }

        //public string ToScriptCommand()
        //{
        //    const string cnst_node_name = "ScriptCommand";
        //    return string.Format("<{0} Time=\"{1}\" Type=\"{2}\" Command=\"{3}\" />", cnst_node_name, StartTime, strType, command);
        //}

        //public static string ToScriptCommands(List<Caption4Video> lstCaptions)
        //{
        //    StringBuilder strBuild = new StringBuilder();
        //    strBuild.Append("<?xml version=\"1.0\" encoding=\"utf-16\"?>");
        //    strBuild.Append("<ScriptCommands>");
        //    foreach (Caption4Video oneItem in lstCaptions)
        //    {
        //        strBuild.Append(oneItem.ToScriptCommand());
        //    }
        //    strBuild.Append("</ScriptCommands>");

        //    //Encoding unicode = Encoding.Unicode;
        //    //Encoding utf16 = Encoding.Unicode;
        //    //byte[] unicodeBytes = unicode.GetBytes(strBuild.ToString());

        //    //byte[] utf16Bytes = Encoding.Convert(unicode,
        //    //                                     utf16,
        //    //                                     unicodeBytes);

        //    return strBuild.ToString();
        //}
    }


#endif
}
