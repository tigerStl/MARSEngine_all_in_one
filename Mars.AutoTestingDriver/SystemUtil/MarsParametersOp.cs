extern alias clientWCF;

using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using MarsEngineSvc.basicReturnDataStructure;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XmlCompareLib;

namespace Mars.AutoTestingDriver.SystemUtil
{
    public enum MarsKeywordParaType
    {
        _unsupported=0,
        _substr = 0x01,
        _allrows, 
        _rowLimit,       

    }

    public enum MaraPara_substrType
    {
        _unKnow=0x00, 
        _last_colon =0x01, 
        _posBegin,
        _posBeginAndEnd,
        _posEnd
    }
    [Serializable]
    public partial class MARSStepsFromClipboardJsonFormat
    {
        public string ord { get; set; }
        public string k_n { get; set; }
        public string k { get; set; }
        public string obj { get; set; }
        public string objN_Id { get; set; }
        public string o_Id { get; set; }
        public string para { get; set; }
        public string data { get; set; }
    //    //{
    //    steps:[
    //	{

    //        ord:1,
    //        k_n:"Pegwindow",
    //        k:15,
    //        obj:'SWAP_TRADE',
    //        objN_Id:11671,
    //        o_Id:6140,
    //        para:,
    //        data:null

    //},
	
    }

    public class MarsCITest
    {
        public const string cnst_source = "-source";
        public const string cnst_storbyardAndProj = "-S";
        public const string cnst_appName = "-AppName";
        public const string cnst_continue = "-Continue";
        public const string cnst_quiteMode = "-IsQuiteMode";
        public const string cnst_userName = "-userName";
        public const string cnst_currentDB = "-currentDB";
        public const string cnst_isLast = "-IsLastTest";
        public const string cnst_TestMode = "-TestMode";

        public const string cnst_default_smt_auto = "MARS_DEFAULT_SMT_MGR";
        //-source FromJenkins -userName admin -S [PROJECTNAME].[D1_BOND_DEFN_ENTRY] -AppName Summit6.2 -Continue False -IsQuiteMode true -DB gen_mars_5
        //-TestMode Base|Compare -IsLastTest false
        public string source { get; set; }
        /// <summary>
        /// 该函数将不再使用。开始，采用 项目.storyboard模式。由于改成采用固定的
        /// </summary>
        /// <param name="strV"></param>
        /// <returns></returns>
        public bool setProjectAndSBNameFromPara(string strV)
        {
            if (string.IsNullOrEmpty(strV)) return false;

            // 格式;[PROJECTNAME].[D1_BOND_DEFN_ENTRY]
            string[] arrProAndSB = strV.Split(new string[] { "].[" }, StringSplitOptions.RemoveEmptyEntries);
            if (arrProAndSB.Length != 2) return false;
            this.projectName = arrProAndSB[0].Trim().Substring(1);
            this.storyboardName = arrProAndSB[1].Trim().Substring(0, arrProAndSB[1].Trim().Length - 1);
            return true;
        }
        private string projectName;
        private string storyboardName;
        
        public string currentDB { get; set; }

        public string ProjectName { get => projectName; }
        public string StoryboardName { get => storyboardName; }

        public string applicationShortName { get; set; }
        public string userName { get; set; } 
        public bool isContinue;
        public bool isQuiteMode;
        public bool isLastTest = false;
        public bool isOk = false;

        public string testMode;
        
        public MarsCITest(CommdLineOptions cmdInfo)
        {
            if (cmdInfo == null) return;
            isOk = false;
            source = cmdInfo.GetOptionStringValue(cnst_source);
            string strProjSBName = cmdInfo.GetOptionStringValue(cnst_storbyardAndProj);
            // 
            applicationShortName = cmdInfo.GetOptionStringValue(cnst_appName);
            isContinue = cmdInfo.GetOptionBooleanValue(cnst_continue);
            isQuiteMode = cmdInfo.GetOptionBooleanValue(cnst_quiteMode);
            this.projectName = cnst_default_smt_auto;
            this.storyboardName = strProjSBName;
            isOk = true;
            //isOk = setProjectAndSBNameFromPara(strProjSBName) ;
            //if (!isOk) return;

            userName = cmdInfo.GetOptionStringValue(cnst_userName);
            currentDB = cmdInfo.GetOptionStringValue(cnst_currentDB);

            isLastTest = cmdInfo.GetOptionBooleanValue(cnst_isLast);
            testMode = cmdInfo.GetOptionStringValue(cnst_TestMode);
            testMode = string.IsNullOrEmpty(testMode)?"Nonbase":testMode;

            isOk = (!string.IsNullOrEmpty(userName))
                && (!string.IsNullOrEmpty(currentDB))
                && (!string.IsNullOrEmpty(projectName)
                && (!string.IsNullOrEmpty(storyboardName))
                && (!string.IsNullOrEmpty(applicationShortName))
                );
        }

        public RESTfulStoryboardBasicInfo storyboardInfo;
        public RESTfullReturnApplicationObjects applicationInfo;
    }
    [Serializable]
    public class MarsClipboardURLPara
    {
        // sample: userName=tiger&command=-FromClipboard&storyBoadName=temp&storyBoardId=-1&app=213&guid=a1b06b48-041f-442c-94eb-d7480c57a647&currentDB=GEN_MARS_10	
        public string command { get; set; }
        public string userName { get; set; }
        public string storyBoardName { get; set; }
        public string storyBoardId { get; set; } // non usaful
        public string app { get; set; }
        public string guid { get; set; }

        public string currentDB { get; set; }
        public string testMode { get; set; }

        public string StepsFromClipboard { get; set; }

        /// <summary>
        /// json format used only
        /// </summary>
        public MARSStepsFromClipboardJsonFormat[] testStepsForJson
        {
            get;
            set;
        }
        [JsonIgnore]
        public List<MARSStepsFromClipboardJsonFormat> testStepsFromClipboard { get; set; }

        public override string ToString()
        {
            return $"{{app-{this.app}, command-{this.command}, guid-{this.guid}, storyBoardId-{this.storyBoardId}, storyBoardName-{this.storyBoardName}, userName-{this.userName}}}";
        }

        public string getDataFromCipboard()
        {
            try
            {
                IDataObject iData = Clipboard.GetDataObject();
                if (iData.GetDataPresent(DataFormats.Text))
                {
                    return (String)iData.GetData(DataFormats.Text);
                }
                return null;

            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool ValidateIds()
        {
            if (this.testStepsFromClipboard == null) return false;
            for (int i = 0; i < this.testStepsFromClipboard.Count; i++)
            {
                var itm = this.testStepsFromClipboard[i];
                if (itm == null) return false;

                int iId = 0;
                if ((!int.TryParse(itm.k, out iId))
                    || (!int.TryParse(itm.o_Id, out iId))
                    || (!int.TryParse(itm.objN_Id, out iId)))
                    return false;
            }
            return true;
        }

        public bool ISMarsJSONFormat()
        {
            try
            {
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
                this.testStepsFromClipboard = js.Deserialize<List<MARSStepsFromClipboardJsonFormat>>(this.StepsFromClipboard);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"ISMarsJSONFormat exception:[{e.Message}], \r\n\t{e.StackTrace}");
                return false;
            }

        }
        /// <summary>
        /// 判断是否存在必要的参数
        /// </summary>
        /// <returns></returns>
        public bool validateURL(ref string strError, ref string strAdv, ref string strStack)
        {
            if (string.IsNullOrEmpty(this.currentDB))
            {
                strError = "Current DB information is NULL or empty.";
                strStack = Environment.StackTrace;
                strAdv = "Contact Marquis";
                return false;
            }
            int iAppId;
            if ((string.IsNullOrEmpty(this.app))
                || (!int.TryParse(this.app.Trim(), out iAppId)))
            {
                strError = "application information desn't pass correctly.";
                strStack = Environment.StackTrace;
                strAdv = "Contact Marquis";
                return false;
            }
            return true;
        }

        public bool ParseURL(string strURI)
        {
            try
            {
                //userName=tiger&command=-FromClipboard&storyBoadName=temp&storyBoardId=-1&app=213&guid=a1b06b48-041f-442c-94eb-d7480c57a647&currentDB=GEN_MARS_10	
                List<string> lstParas = new List<string>();
                Uri uriFromClick = new Uri(strURI);
                var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);
                this.command = query.Get("command");
                this.app = query.Get("app");
                this.guid = query.Get("guid");
                this.storyBoardId = query.Get("storyBoardId");
                this.storyBoardName = query.Get("storyBoardName");
                this.userName = query.Get("userName");
                this.currentDB = query.Get("currentDB");
                if (string.IsNullOrEmpty(this.command) || (!AutoTestingDriverEntry.cnst_uri_command_clipboard.Equals(this.command)))
                    return false;

                this.StepsFromClipboard = getDataFromCipboard();
                if (string.IsNullOrEmpty(this.StepsFromClipboard))
                    return false;

                if (!ISMarsJSONFormat())
                    return false;
                if (!ValidateIds())
                    return false;

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"MarsClipboardURLPara.ParseURL Generate Exceptions:{e.Message},\r\n{e.StackTrace}");
                return false;
            }

        }
    }

    public abstract class MarsParametersOp
    {
        internal const string cnst_sub_str="SUBSTR";

        private static MLogger Logger = MLogger.GetLogger(typeof(MarsParametersOp));
        public MarsKeywordParaType currentParaType;
        public static List<MarsParametersOp> GetOpType(string strPara,ref string strError)
        {
            Logger.logBegin("GetOpType", $"para is :[{strPara}]");
            try
            {
                List<MarsParametersOp> lstRslt = new List<MarsParametersOp>();
                if (string.IsNullOrEmpty(strPara)) return lstRslt;
                int iPos = -1;
                string strUpperPara = strPara.ToUpper();
                if ((iPos = strUpperPara.IndexOf(cnst_sub_str)) >= 0)
                {
                    MarsParaSubstrOp subStrOp = MarsParaSubstrOp.PhrasePara(strUpperPara, ref strError);
                    if (subStrOp != null)
                        lstRslt.Add(subStrOp);
                }
                return lstRslt;
            }
            finally
            {
                Logger.logEnd("GetOpType");
            }
        }

        internal abstract string dealWithData(string strItmTxt);
        
    }

    public class MarsParaSubstrOp: MarsParametersOp
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MarsParaSubstrOp));

        public const string cnst_last_colon = "LAST_COLON";
        public MaraPara_substrType currentSubStrType= MaraPara_substrType._unKnow;

        public int endPos = -1;
        public int startPos = -1;

        
        internal override string dealWithData(string strItmTxt)
        {
            logger.logBegin("dealWithData", $"to deal with data :{strItmTxt}");
            if (string.IsNullOrEmpty(strItmTxt)) return "";

            if (startPos > strItmTxt.Length) return strItmTxt;
            
            switch (this.currentSubStrType)
            {
                case MaraPara_substrType._last_colon:
                    int icolonPos = strItmTxt.LastIndexOf(":");
                    if (icolonPos < 0) return strItmTxt;
                    return strItmTxt.Substring(icolonPos + 1);
                    
                default:
                    if (((endPos == -1) && (startPos == -1))
                        || ((startPos >= endPos)&&(endPos!=-1)))
                        return strItmTxt;
                    if (startPos == -1)
                    {
                        return strItmTxt.Substring(0,endPos);
                    }
                    else
                    {
                        if (endPos == -1) return strItmTxt.Substring(startPos);
                        return strItmTxt.Substring(startPos, endPos-startPos+1);
                    }
                    
            }
            
        }

        internal static MarsParaSubstrOp PhrasePara(string strPara,ref string strError)
        {
            logger.logBegin("PhrasePara", $"para is :[{strPara}]");
            int iPos = -1;
            if ((string.IsNullOrEmpty(strPara))
                ||((iPos = strPara.IndexOf(MarsParametersOp.cnst_sub_str))<0)
               )
            {
                strError = "parameter is not sub str mode";
                return null;
            }
            MarsParaSubstrOp rslt = new MarsParaSubstrOp();
            rslt.currentParaType  = MarsKeywordParaType._substr;
            int iPosSemiColon = strPara.IndexOf(";", iPos);
            if (iPosSemiColon < 0)
                iPosSemiColon = strPara.Length ;
            string strSubExtractPara = strPara.Substring(iPos, 
                iPosSemiColon-iPos );
            logger.Info("PhrasePara", strSubExtractPara);
            string[] arrParas = strSubExtractPara.Split(new string[] {":"},StringSplitOptions.None);
            if (arrParas.Length <= 1)
            {
                logger.Info("\t", "arrpars return less than 1");
                return null; //no other parameters
            }

            if (string.IsNullOrEmpty(arrParas[1]))
            {
                logger.Info("\t", "first para is empty");
                if (arrParas.Length < 3) return null;
                if (string.IsNullOrEmpty(arrParas[2])) return null;
                rslt.startPos = 0 ;
                if (!int.TryParse(arrParas[2], out rslt.endPos))
                {
                    logger.Error("PhrasePara", strError = $"para [{arrParas[2]}] is not a int from substr");
                    return null;
                }

            }
            else {
                bool isFirstParaOk = true, isSndParaOk = true;
                if ((string.Compare(arrParas[1], cnst_last_colon, true) == 0)){
                    rslt.currentSubStrType = MaraPara_substrType._last_colon;
                    return rslt;
                }
                if (!int.TryParse(arrParas[1], out rslt.startPos))
                {
                    rslt.startPos = -1;
                    rslt.currentSubStrType = MaraPara_substrType._posEnd;
                }
                else
                {
                    isFirstParaOk = false;
                }
                rslt.currentSubStrType = MaraPara_substrType._posBeginAndEnd;
                if (arrParas.Length >= 3)
                {
                    if (!int.TryParse(arrParas[2], out rslt.endPos))
                    {
                        logger.Error("PhrasePara", strError = $"para [{arrParas[2]}] is not a int from substr");
                        return null;
                    }
                } else
                    isSndParaOk = false;
                strError = "no right parameters for subStr. substr para format should be: substr:[LAST_COLON] or subStr:number:number or subStr:number:....";
                return (isFirstParaOk && isSndParaOk) ? rslt : null;
            }
            return null;
        }
    }
}
