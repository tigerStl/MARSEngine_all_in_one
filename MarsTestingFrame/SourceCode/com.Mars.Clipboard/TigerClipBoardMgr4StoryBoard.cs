
using com.Mars.ClipboardMgr;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Mars.Dto;
using Mars.DataLayer;
using Mars.Business;

namespace com.Mars.ClipboardMgr
{
    public static class TigerXmlForSheetAddins
    {
        public static XDocument fwToXDocument(this XmlDocument xmlDocument)
        {
            using (XmlNodeReader xmlNodeReader = new XmlNodeReader(xmlDocument))
            {
                xmlNodeReader.MoveToContent();
                return XDocument.Load(xmlNodeReader);
            }
        }
    }

    public class ClipboardMgrBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ClipboardMgrBase));
        internal DataTable currentClipBoardDataTable = null;

        protected string GetDataAsString(string strCol, int iRow)
        {
            object oTmp = currentClipBoardDataTable.Rows[iRow][strCol];
            if (oTmp == null)
                return "";
            return oTmp.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iDataType">
        /// 0,test case
        /// 1,storyboard
        /// </param>
        /// <param name="blnFirstRowHasHeader"></param>
        /// <returns></returns>
        public DataTable clipboardExcelToDataTable(int iDataType,bool blnFirstRowHasHeader = true)
        {
            var clipboard = Clipboard.GetDataObject();
            if (!clipboard.GetDataPresent("XML Spreadsheet")) return currentClipBoardDataTable = null;

            StreamReader streamReader = new StreamReader((MemoryStream)clipboard.GetData("XML Spreadsheet"));
            streamReader.BaseStream.SetLength(streamReader.BaseStream.Length - 1);

            XmlDocument xmlDocment = new XmlDocument();
            xmlDocment.LoadXml(streamReader.ReadToEnd());

            XNamespace ssNs = "urn:schemas-microsoft-com:office:spreadsheet";
            DataTable dtData = currentClipBoardDataTable = new DataTable();

            var linqCells = xmlDocment.fwToXDocument().Descendants(ssNs + "Column").ToList<XElement>();
            for (int x = 0; x < linqCells.Count(); x++)
            {
                dtData.Columns.Add(string.Format("Column {0}", x + 1));
            }

            var linqRows = xmlDocment.fwToXDocument().Descendants(ssNs + "Row").ToList<XElement>();
            linqRows.ForEach(r => {
                Logger.Info("clipboardExcelToDataTable", r.ToString());
            });
            
            int iCol = 0;
            foreach (var onerow in linqRows)
            {
                if (onerow == null) continue;
                List<XElement> lstCell = onerow.Descendants(ssNs + "Cell").ToList();
                DataRow oneDtRow = dtData.Rows.Add();
                iCol = 0;

                lstCell.ForEach(c =>
                {
                    XAttribute o = c.Attribute(ssNs + "Index");
                    if (o == null)
                    {
                        if ((iCol+1)>dtData.Columns.Count)
                        {
                            for (int z = dtData.Columns.Count; z < (iCol + 1); z++)
                                dtData.Columns.Add(string.Format("Column {0}", z+1));
                        }
                        oneDtRow[iCol++] = c.Value;
                    }
                    else
                    {
                        Logger.Info("clipboardExcelToDataTable", string.Format("Get ss:index :{0}", o.Value));
                        int iCellIdx;
                        if (int.TryParse(o.Value, out iCellIdx))
                        {
                            iCol = iCellIdx - 1;
                            if (iCol < dtData.Columns.Count)
                                oneDtRow[(iCol)] = c.Value;
                            else
                            {
                                while (dtData.Columns.Count <= iCol)
                                {
                                    dtData.Columns.Add(string.Format("Column {0}", dtData.Columns.Count + 1));
                                }
                                oneDtRow[(iCol)] = c.Value;
                            }

                            iCol += 1;
                        }
                        else
                        {
                            iCol++;
                        }
                    }

                });               

            }
            //判断第一行是不是标准的头，如果不是，则增加
            if (dtData.Rows.Count > 0)
            {
                if (iDataType == 0)
                {
                    if (dtData.Columns.Count != TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader.Length)
                    {
                        string strError = string.Format("columns' count is wrong. current Columns:[{0}]", dtData.Columns.ToString());
                        MessageBox.Show(strError);
                        Logger.Error("clipboardExcelToDataTable", strError);
                        return currentClipBoardDataTable = null;
                    }
                    for (int i = 0; i < dtData.Columns.Count; i++)
                    {
                        if (string.Compare(dtData.Rows[0][i].ToString().Trim(), TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader[i], true) != 0)
                        {
                            Logger.Info("clipboardExcelToDataTable", "First row doesn't match default column headers. add default header.");
                            for (int iH = 0; iH < TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader.Length; iH++)
                            {
                                dtData.Columns[iH].ColumnName = TigerClipBoardMgr4Testcase.cnst_arr_TestcaseHeader[iH];
                            }
                            return currentClipBoardDataTable = dtData;
                        }
                    }
                }
                else
                {
                    if (iDataType==1)
                    {
                        if (dtData.Columns.Count != TigerClipBoardMgr4StoryBoard.cnst_storyboardHeader.Length)
                        {
                            string strError = string.Format("columns' count is wrong. current Columns:[{0}]", dtData.Columns.ToString());
                            MessageBox.Show(strError);
                            Logger.Error("clipboardExcelToDataTable", strError);
                            return currentClipBoardDataTable = null;
                        }
                    }
                    for (int i = 0; i < dtData.Columns.Count; i++)
                    {
                        if (string.Compare(dtData.Rows[0][i].ToString().Trim(), TigerClipBoardMgr4StoryBoard.cnst_storyboardHeader[i], true) != 0)
                        {
                            Logger.Info("clipboardExcelToDataTable", "First row doesn't match default column headers. add default header.");
                            for (int iH = 0; iH < TigerClipBoardMgr4StoryBoard.cnst_storyboardHeader.Length; iH++)
                            {
                                dtData.Columns[iH].ColumnName = TigerClipBoardMgr4StoryBoard.cnst_storyboardHeader[iH];
                            }
                            return currentClipBoardDataTable = dtData;
                        }
                    }
                }
                
            }

            if (blnFirstRowHasHeader)
            {
                int x = 0;
                foreach (DataColumn dcCurrent in dtData.Columns)
                    dcCurrent.ColumnName = dtData.Rows[0][x++].ToString().Trim();

                dtData.Rows.RemoveAt(0);
            }
            return currentClipBoardDataTable = dtData;
        }
    }

    public class TigerClipBoardMgr4Testcase: ClipboardMgrBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TigerClipBoardMgr4Testcase));
        private List<long> applicationIds=null;
        public List<long> ApplicationIds { get { return applicationIds; } set { applicationIds = value; } }
        public readonly static string[] cnst_arr_TestcaseHeader = { "Keyword", "Object", "Parameters", "Comment", "Data" };
        private const int cnst_keywordId = 0;
        private const int cnst_objectId = 1;
        private const int cnst_parametersId = 2;
        private const int cnst_commentId = 3;
        private const int cnst_dataId = 4;

        private string currentDBIdx = "MarsEntities";
        public TigerClipBoardMgr4Testcase(string strDBIdx):base()
        {

            this.currentDBIdx = strDBIdx;
        }

        public List<V_TEST_STEPS_FULLVISIONDTO> ConvertDataTable2Dto(ref bool isOk, ref string strError,
            ref Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> dicPegWithItsSubs)
        {
            Logger.logBegin("ConvertDataTable2Dto");
            List<V_TEST_STEPS_FULLVISIONDTO> lstRslt = new List<V_TEST_STEPS_FULLVISIONDTO>();
            try
            {
                if (applicationIds == null)
                {
                    isOk = false;
                    Logger.Error("ConvertDataTable2Dto", strError = "No Application is assigned");
                    return lstRslt;
                }

                ///获得测试对象信息
                /// 
                B_REGISTED_OBJECT objB = new B_REGISTED_OBJECT();
                List<string> lstPegNames = new List<string>();
                for (int i = 0; i < currentClipBoardDataTable.Rows.Count; i++)
                {
                    if (currentClipBoardDataTable.Rows[i] == null) continue;
                    string strTmpKeyword = this.GetDataAsString(cnst_arr_TestcaseHeader[cnst_keywordId], i);
                    if (string.IsNullOrEmpty(strTmpKeyword)) continue;
                    if (string.Compare("Pegwindow", strTmpKeyword, true) != 0) continue;
                    string strTmpObjName = this.GetDataAsString(cnst_arr_TestcaseHeader[cnst_objectId], i);
                    if (string.IsNullOrEmpty(strTmpObjName)) continue;

                    lstPegNames.Add(strTmpObjName);
                }
                lstPegNames = lstPegNames.Distinct().ToList();
                Dictionary<B_REGISTED_OBJECT, List<B_REGISTED_OBJECT>> lstObject = objB.GetObjectsByPegAndAppIds(currentDBIdx,
                    lstPegNames, applicationIds, ref isOk, ref strError);
                if (!isOk)
                {
                    return lstRslt;
                }
                dicPegWithItsSubs = lstObject;

                ///依据对象信息 创建 test step
                /// 
                B_KEYWORD objKey = new B_KEYWORD();
                foreach (var oneApp in applicationIds)                    
                {
                    List<B_REGISTED_OBJECT> lstCurrentSubObjects = null;
                    long lCurrentPegNameId = -2;
                    bool isNewPeg = false;
                    B_REGISTED_OBJECT currentPeg = null;
                    Dictionary<long, List<B_REGISTED_OBJECT>> lstCurrentSubObjectsWithPeg = new Dictionary<long, List<B_REGISTED_OBJECT>>();

                    for (int i = 0; i < this.currentClipBoardDataTable.Rows.Count; i++)
                    {
                        if (this.currentClipBoardDataTable.Rows[i] == null) continue;
                        V_TEST_STEPS_FULLVISIONDTO tmpStepInfo = new V_TEST_STEPS_FULLVISIONDTO();
                        tmpStepInfo.APPLICATION_ID = oneApp;
                        tmpStepInfo.COLUMN_ROW_SETTING = GetDataAsString(cnst_arr_TestcaseHeader[cnst_parametersId], i);
                        tmpStepInfo.COMMENTINFO = GetDataAsString(cnst_arr_TestcaseHeader[cnst_commentId], i);
                        tmpStepInfo.ENUM_TYPE = null;//需要增强 
                        tmpStepInfo.IS_RUNNABLE = 1;
                        tmpStepInfo.RUN_ORDER = i;

                        string strKeyword = GetDataAsString(cnst_arr_TestcaseHeader[cnst_keywordId],i);
                        T_KEYWORDDTO objKeyDto = objKey.GetKeywordByName(
                            currentDBIdx,
                            strKeyword, null, ref isOk, ref strError);
                        if ((!isOk)||(objKeyDto==null))
                        {
                            Logger.Error("ConvertDataTable2Dto",strError = string.Format("No such keyword [{1}] found in database. Error :\r\n[{0}]",strError, strKeyword));
                            return lstRslt;
                        }
                        tmpStepInfo.KEY_WORD_ID = objKeyDto.KEY_WORD_ID;
                        tmpStepInfo.KEY_WORD_NAME = objKeyDto.KEY_WORD_NAME;                        
                        tmpStepInfo.OBJECT_HAPPY_NAME = GetDataAsString(cnst_arr_TestcaseHeader[cnst_objectId], i);
                    
                        #region set object NameId and objectId, peg
                        if (string.Compare("PegWindow", strKeyword, true) == 0)
                        {            
                            ///判断是否是当前pegwindow                
                            isNewPeg =currentPeg==null? true :string.Compare(currentPeg.OBJECT_HAPPY_NAME, tmpStepInfo.OBJECT_HAPPY_NAME, true) != 0;
                            if (isNewPeg)
                            {
                                ///不是当前的pegwindow，从所有的对象列表中获得，如果找不到，说明pegwindow不存在
                                B_REGISTED_OBJECT tmpPeg = lstObject.Keys.Where(p => (string.Compare(p.OBJECT_HAPPY_NAME, tmpStepInfo.OBJECT_HAPPY_NAME, true) == 0)&& (p.APPLICATION_ID==oneApp)).FirstOrDefault();
                                if (tmpPeg==null)
                                {
                                    Logger.Error("ConvertDataTable2Dto", strError = string.Format("Can't find Pegwindow [{0}]", tmpStepInfo.OBJECT_HAPPY_NAME));
                                    return lstRslt;
                                }
                                ///判断是否本地的cache中已经存在——否则要到listObject中找，数据量大，故而从本地找
                                /// 
                                if (!lstCurrentSubObjectsWithPeg.Keys.Contains(tmpPeg.OBJECT_NAME_ID ?? -1))
                                {
                                    lstCurrentSubObjectsWithPeg.Add(lCurrentPegNameId = (tmpPeg.OBJECT_NAME_ID ?? -1), lstCurrentSubObjects = lstObject[tmpPeg]);
                                }
                                else
                                {
                                    lstCurrentSubObjects = lstCurrentSubObjectsWithPeg[lCurrentPegNameId=(tmpPeg.OBJECT_NAME_ID ?? -1)];
                                }
                                currentPeg = tmpPeg;                                
                            }
                            else
                            {
                                lstCurrentSubObjects = lstCurrentSubObjectsWithPeg[lCurrentPegNameId];
                            }
                            tmpStepInfo.OBJECT_ID = currentPeg.OBJECT_ID;
                            tmpStepInfo.OBJECT_NAME_ID = lCurrentPegNameId;
                            tmpStepInfo.OBJECT_TYPE = currentPeg.OBJECT_HAPPY_NAME;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(tmpStepInfo.OBJECT_HAPPY_NAME))
                            {
                                //判断 是不是 不需要object的keyword
                                if (!B_KEYWORD.IsKeywordNotRequireObject(strKeyword))
                                {
                                    Logger.Error("ConvertDataTable2Dto", strError = string.Format("Row #[{0}] is wrong, as keyword [{1}] requires an Object.", i + 1, strKeyword));
                                    isOk = false;
                                    return lstRslt;
                                }
                                tmpStepInfo.OBJECT_ID = -1;
                                tmpStepInfo.OBJECT_NAME_ID = -1;
                            }
                            else
                            {
                                if (lstCurrentSubObjects == null)
                                {
                                    Logger.Error("ConvertDataTable2Dto",strError = string.Format("Row #[{0}] is wrong, No Peg information before this step. Keyword:[{1}]", i+1,strKeyword));
                                    isOk = false;
                                    return lstRslt;
                                }
                                var tmpObj = lstCurrentSubObjects.Where(p=>(string.Compare(p.OBJECT_HAPPY_NAME, tmpStepInfo.OBJECT_HAPPY_NAME,true)==0)&&(p.APPLICATION_ID== oneApp)).FirstOrDefault();
                                if(tmpObj==null)
                                {
                                    Logger.Error("ConvertDataTable2Dto", strError = string.Format("Can't find object [{2}] from object list under pegwindow:[{0}],application:[{1}], row #[{3}]",
                                        currentPeg.OBJECT_HAPPY_NAME,
                                        oneApp,
                                        tmpStepInfo.OBJECT_HAPPY_NAME,
                                        i + 1 
                                        ));
                                    isOk = false;
                                    return lstRslt;
                                }
                                tmpStepInfo.OBJECT_ID = tmpObj.OBJECT_ID;
                                tmpStepInfo.OBJECT_NAME_ID = tmpObj.OBJECT_NAME_ID;
                                tmpStepInfo.OBJECT_TYPE = currentPeg.OBJECT_HAPPY_NAME;
                            }
                        }
                        #endregion //set object NameId and objectId, Peg

                        tmpStepInfo.QUICK_ACCESS = "";// 这里暂时没用
                        tmpStepInfo.STEPS_ID = -1;
                        tmpStepInfo.VALUE_SETTING = GetDataAsString(cnst_arr_TestcaseHeader[cnst_dataId], i);

                        lstRslt.Add(tmpStepInfo);
                    };                    
                }
                isOk = true;
                return lstRslt;
            }
            finally
            {
                Logger.logEnd("ConvertDataTable2Dto");
            }
        }
    }

    public class TigerClipBoardMgr4StoryBoard : ClipboardMgrBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TigerClipBoardMgr4StoryBoard));
        public static readonly string[] cnst_storyboardHeader = new string[]{"Run Order", "Action", "Step Name", "Test Suite Name", "Test Case Name",
"Data Set Name", "Result", "Error Cause","Script Start", "Script End", "Dependency","Description"};
        private string currentClipBoardInfo;
        public string CurrentClipBoardInfo { get { return currentClipBoardInfo; } set { currentClipBoardInfo = value; } }

        public string[] Header;
        private string currentDBIdx = "MarsEntites";
        public TigerClipBoardMgr4StoryBoard(string strDBIdx):base()
        {
            this.currentDBIdx = strDBIdx; 
        }

        public DataTable CurrentClipBoardDataTable  {
            get { return currentClipBoardDataTable; }
            set { currentClipBoardDataTable = value; }
        }
              

        

        public List<V_STORYBOARD_TEST_FULLVISIONDTO> ConvertDataTable2Dto(ref bool isOk, ref string strError)
        {
            Logger.logBegin("ConvertDataTable2Dto");
            isOk = false;
            if (currentClipBoardDataTable == null)
            {
                strError = "Data not initialized or No Data. DataTable is Null";
                return null;
            }
            List<V_STORYBOARD_TEST_FULLVISIONDTO> lstRslt = new List<V_STORYBOARD_TEST_FULLVISIONDTO>();
            for (int i=0;i<currentClipBoardDataTable.Rows.Count;i++)
            {
                V_STORYBOARD_TEST_FULLVISIONDTO objStoryBoard = new V_STORYBOARD_TEST_FULLVISIONDTO();
                
                string strAliasName = GetDataAsString("Step Name", i);                 
                string strDataSetName = GetDataAsString("Data Set Name",i);
                string strRunType = GetDataAsString("Action", i);
                string strTSName = GetDataAsString("Test Suite Name", i);
                string strTCName = GetDataAsString("Test Case Name", i);

                ///get run type based on runType info
                /// 
                short iType;
                if (short.TryParse(strRunType, out iType)) {
                    strRunType = BoHelper.GetRunTypeStringFromSystemLookup(currentDBIdx,iType);
                }
                else
                {
                    iType = 0;
                    strRunType = "EXECUTE";
                }

                objStoryBoard.ALIAS_NAME = strAliasName;
                objStoryBoard.DATASET_DESCRIPTION = "";
                objStoryBoard.DATA_SETTING_ID = -2; //-2 means unknow
                objStoryBoard.DATA_SET_ALIAS_NAME = strDataSetName;
                objStoryBoard.TEST_RUN_VALUE = iType < 0 ? null : (short?)iType;
                objStoryBoard.TEST_CASE_ID = -2; // -2 means unknow
                objStoryBoard.TEST_CASE_NAME = strTCName;
                objStoryBoard.TEST_SUITE_NAME = strTSName;
                objStoryBoard.TEST_SUITE_ID = -2;
                objStoryBoard.DISPLAY_NAME = strRunType;

                lstRslt.Add(objStoryBoard);

            }
            isOk = true;
            return lstRslt;

        }
    }
}
