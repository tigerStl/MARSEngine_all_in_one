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

    /// <summary>
    /// BatchFold（批量折叠）模式从 ClickOnce / HTTP 启动 URL 解析出的参数对象。
    /// <para>
    /// 样例 URL：
    /// <c>/MARSENGINE/Mars.AutoTestingDriver.application?userName=Regr1&amp;uuid=7aae010e-a6f3-45e2-baec-664ecae8a5a2&amp;command=-BatchFold&amp;FoldList=(1,323,432,5)&amp;Continue=True&amp;currentDB=MARS2&amp;callBackServer=localhost%3A5000</c>
    /// </para>
    /// <para>
    /// 模板：
    /// <c>?userName=[UserName]&amp;uuid=[Uuid]&amp;command=-BatchFold&amp;FoldList=(id1,id2,...)&amp;Continue=[True|False]&amp;currentDB=[Db]&amp;callBackServer=[Host]</c>
    /// </para>
    /// </summary>
    [Serializable]
    public class MarsBatchFoldURLPara
    {
        /// <summary>URL 查询参数名：用户名</summary>
        public const string QUERY_USER_NAME = "userName";
        /// <summary>URL 查询参数名：会话 UUID（Web 回调 session）</summary>
        public const string QUERY_UUID = "uuid";
        /// <summary>URL 查询参数名：命令字</summary>
        public const string QUERY_COMMAND = "command";
        /// <summary>URL 查询参数名：待折叠项 ID 列表，格式如 (1,323,432,5)</summary>
        public const string QUERY_FOLD_LIST = "FoldList";
        /// <summary>URL 查询参数名：失败是否继续</summary>
        public const string QUERY_CONTINUE = "Continue";
        /// <summary>URL 查询参数名：当前数据库短名</summary>
        public const string QUERY_CURRENT_DB = "currentDB";
        /// <summary>URL 查询参数名：回调服务器 host:port（可经 URL 编码，如 localhost%3A5000）</summary>
        public const string QUERY_CALLBACK_SERVER = "callBackServer";

        /// <summary>MARS 登录用户名，对应 userName</summary>
        public string userName { get; set; }
        /// <summary>Web 会话 UUID，对应 uuid；用于状态回写</summary>
        public string uuid { get; set; }
        /// <summary>命令字，期望为 -BatchFold</summary>
        public string command { get; set; }
        /// <summary>原始 FoldList 字符串，例如 "(1,323,432,5)"</summary>
        public string foldListRaw { get; set; }
        /// <summary>从 FoldList 解析出的 ID 列表</summary>
        public List<long> foldListIds { get; set; }
        /// <summary>失败是否继续执行后续项，对应 Continue=True|False</summary>
        public bool isContinue { get; set; }
        /// <summary>当前数据库短名，对应 currentDB</summary>
        public string currentDB { get; set; }
        /// <summary>
        /// 回调服务器地址，对应 callBackServer。
        /// Uri.Query 解析后一般为解码值，例如 "localhost:5000"。
        /// </summary>
        public string callBackServer { get; set; }

        /// <summary>解析是否成功</summary>
        public bool isOk { get; set; }
        /// <summary>解析失败时的错误信息</summary>
        public string parseError { get; set; }
        /// <summary>给用户的建议</summary>
        public string parseAdvice { get; set; }
        /// <summary>堆栈（诊断用）</summary>
        public string parseStack { get; set; }

        public override string ToString()
        {
            string ids = foldListIds == null ? "" : string.Join(",", foldListIds);
            return $"{{userName={userName}, uuid={uuid}, command={command}, FoldList=({ids}), Continue={isContinue}, currentDB={currentDB}, callBackServer={callBackServer}}}";
        }

        /// <summary>
        /// 将 FoldList 原始值解析为 long 列表。
        /// 支持：(1,323,432,5) / 1,323,432,5 / 带空格形式。
        /// </summary>
        public static bool TryParseFoldList(string raw, out List<long> ids, out string strError)
        {
            ids = new List<long>();
            strError = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                strError = "FoldList is empty or null";
                return false;
            }

            string s = raw.Trim();
            // 去掉外层括号 ( ... )
            if (s.StartsWith("(") && s.EndsWith(")"))
                s = s.Substring(1, s.Length - 2).Trim();

            if (string.IsNullOrWhiteSpace(s))
            {
                strError = "FoldList has no id after removing parentheses";
                return false;
            }

            string[] parts = s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string token = part.Trim();
                if (string.IsNullOrEmpty(token)) continue;
                if (!long.TryParse(token, out long id))
                {
                    strError = $"FoldList contains non-numeric id:[{token}] in raw:[{raw}]";
                    ids = null;
                    return false;
                }
                ids.Add(id);
            }

            if (ids.Count == 0)
            {
                strError = $"FoldList parsed to empty list from:[{raw}]";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 校验 BatchFold 必需参数是否齐全且合法。
        /// </summary>
        public bool Validate(ref string strError, ref string strAdv, ref string strStack)
        {
            strStack = Environment.StackTrace;
            strAdv = "Please start BatchFold from MARS GUI; if error continues, contact Marquis";

            if (string.IsNullOrWhiteSpace(userName))
            {
                strError = "Parameter [userName] from URL is empty or null";
                return false;
            }
            if (string.IsNullOrWhiteSpace(uuid))
            {
                strError = "Parameter [uuid] from URL is empty or null";
                return false;
            }
            if (string.IsNullOrWhiteSpace(command)
                || !string.Equals(command, AutoTestingDriverEntry.cnst_uri_command_batchfold, StringComparison.OrdinalIgnoreCase))
            {
                strError = $"Parameter [command] is not {AutoTestingDriverEntry.cnst_uri_command_batchfold}";
                return false;
            }
            if (foldListIds == null || foldListIds.Count == 0)
            {
                strError = "Parameter [FoldList] is empty or invalid";
                return false;
            }
            if (string.IsNullOrWhiteSpace(currentDB))
            {
                strError = "Parameter [currentDB] from URL is empty or null";
                return false;
            }
            if (string.IsNullOrWhiteSpace(callBackServer))
            {
                strError = "Parameter [callBackServer] from URL is empty or null";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从完整 HTTP/ClickOnce URL 解析 BatchFold 参数。
        /// 仅当 command=-BatchFold 时返回 true（并填充字段）；其它 command 返回 false 且不视为错误。
        /// 参数不合法时返回 false，并通过 parseError / parseAdvice / parseStack 带回详情。
        /// </summary>
        public bool ParseURL(string strURI)
        {
            isOk = false;
            parseError = parseAdvice = parseStack = null;
            foldListIds = null;
            try
            {
                if (string.IsNullOrWhiteSpace(strURI))
                {
                    parseError = "Activation URI is empty";
                    parseAdvice = "Please start Engine from MARS GUI";
                    parseStack = Environment.StackTrace;
                    return false;
                }

                Uri uriFromClick = new Uri(strURI);
                var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);

                command = query.Get(QUERY_COMMAND);
                // 非 BatchFold 命令：交还给其它模式处理
                if (string.IsNullOrEmpty(command)
                    || !string.Equals(command, AutoTestingDriverEntry.cnst_uri_command_batchfold, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                userName = query.Get(QUERY_USER_NAME);
                uuid = query.Get(QUERY_UUID) ?? query.Get(Mars.message.Utility.MarsConstants.CNST_QUERY_URL_UUID_PARA_NAME);
                foldListRaw = query.Get(QUERY_FOLD_LIST);
                currentDB = query.Get(QUERY_CURRENT_DB);
                // HttpUtility.ParseQueryString 会自动 URL-decode，localhost%3A5000 -> localhost:5000
                callBackServer = query.Get(QUERY_CALLBACK_SERVER);

                string strContinue = query.Get(QUERY_CONTINUE);
                if (string.IsNullOrWhiteSpace(strContinue))
                {
                    parseError = "Parameter [Continue] from URL is empty or null";
                    parseAdvice = "Continue must be True or False";
                    parseStack = Environment.StackTrace;
                    return false;
                }
                if (!bool.TryParse(strContinue.Trim(), out bool cont))
                {
                    parseError = $"Parameter [Continue] is not a boolean:[{strContinue}]";
                    parseAdvice = "Continue must be True or False";
                    parseStack = Environment.StackTrace;
                    return false;
                }
                isContinue = cont;

                if (!TryParseFoldList(foldListRaw, out List<long> ids, out string foldErr))
                {
                    parseError = foldErr;
                    parseAdvice = "FoldList format should be like (1,323,432,5)";
                    parseStack = Environment.StackTrace;
                    return false;
                }
                foldListIds = ids;

                string strError = null, strAdv = null, strStack = null;
                if (!Validate(ref strError, ref strAdv, ref strStack))
                {
                    parseError = strError;
                    parseAdvice = strAdv;
                    parseStack = strStack;
                    return false;
                }

                isOk = true;
                return true;
            }
            catch (Exception e)
            {
                parseError = $"Error while parsing BatchFold URL parameters: {e.Message}";
                parseAdvice = "Unidentified error. If this continues, contact Marquis";
                parseStack = $"{e.Message}\r\n{e.StackTrace}";
                isOk = false;
                return false;
            }
        }
    }

    /// <summary>
    /// Fold（单 Storyboard 折叠）模式从 ClickOnce / HTTP 启动 URL 解析出的参数对象。
    /// <para>
    /// 模板 URL：
    /// <c>/MARSENGINE/Mars.AutoTestingDriver.application?userName=[UserName]&amp;uuid=[Uuid]&amp;command=-Fold&amp;storyBoadName=[SB_Name]&amp;storyBoardId=[Id]&amp;app=[AppId]&amp;Mode=Base&amp;Continue=False&amp;IgnoreError=False&amp;currentDB=[Db]&amp;callBackServer=[Host]</c>
    /// </para>
    /// <remarks>
    /// 与 Storyboard(-S) URL 字段高度相似，但 command=-Fold；本类型独立解析，不改动 CombinParaFromURI 原有逻辑。
    /// </remarks>
    /// </summary>
    [Serializable]
    public class MarsFoldURLPara
    {
        public const string QUERY_USER_NAME = "userName";
        public const string QUERY_UUID = "uuid";
        public const string QUERY_COMMAND = "command";
        public const string QUERY_STORYBOARD_NAME = "storyBoardName";
        public const string QUERY_STORYBOARD_NAME_TYPO = "storyBoadName"; // 历史拼写兼容
        public const string QUERY_STORYBOARD_ID = "storyBoardId";
        public const string QUERY_APP = "app";
        public const string QUERY_MODE = "Mode";
        public const string QUERY_CONTINUE = "Continue";
        public const string QUERY_IGNORE_ERROR = "IgnoreError";
        public const string QUERY_CURRENT_DB = "currentDB";
        public const string QUERY_CALLBACK_SERVER = "callBackServer";

        public string userName { get; set; }
        public string uuid { get; set; }
        public string command { get; set; }
        public string storyBoardName { get; set; }
        public string storyBoardId { get; set; }
        public string appId { get; set; }
        public string mode { get; set; }
        public bool isContinue { get; set; }
        public bool ignoreError { get; set; }
        public string currentDB { get; set; }
        /// <summary>回调服务器，ParseQueryString 后一般为解码值，如 localhost:5000</summary>
        public string callBackServer { get; set; }

        public bool isOk { get; set; }
        public string parseError { get; set; }
        public string parseAdvice { get; set; }
        public string parseStack { get; set; }

        public override string ToString()
        {
            return $"{{userName={userName}, uuid={uuid}, command={command}, storyBoardName={storyBoardName}, storyBoardId={storyBoardId}, app={appId}, Mode={mode}, Continue={isContinue}, IgnoreError={ignoreError}, currentDB={currentDB}, callBackServer={callBackServer}}}";
        }

        public bool Validate(ref string strError, ref string strAdv, ref string strStack)
        {
            strStack = Environment.StackTrace;
            strAdv = "Please start Fold from MARS GUI; if error continues, contact Marquis";

            if (string.IsNullOrWhiteSpace(userName))
            {
                strError = "Parameter [userName] from URL is empty or null";
                return false;
            }
            if (string.IsNullOrWhiteSpace(uuid))
            {
                strError = "Parameter [uuid] from URL is empty or null";
                return false;
            }
            if (string.IsNullOrWhiteSpace(command)
                || !string.Equals(command, AutoTestingDriverEntry.cnst_uri_command_fold, StringComparison.OrdinalIgnoreCase))
            {
                strError = $"Parameter [command] is not {AutoTestingDriverEntry.cnst_uri_command_fold}";
                return false;
            }
            if (string.IsNullOrWhiteSpace(storyBoardName))
            {
                strError = "Parameter [storyBoadName/storyBoardName] from URL is empty or null";
                return false;
            }
            if (string.IsNullOrWhiteSpace(storyBoardId) || !long.TryParse(storyBoardId, out _))
            {
                strError = "Parameter [storyBoardId] from URL is empty or not a number";
                return false;
            }
            if (string.IsNullOrWhiteSpace(appId) || !long.TryParse(appId, out _))
            {
                strError = "Parameter [app] from URL is empty or not a number";
                return false;
            }
            if (string.IsNullOrWhiteSpace(mode))
            {
                strError = "Parameter [Mode] from URL is empty or null";
                return false;
            }
            if (string.IsNullOrWhiteSpace(currentDB))
            {
                strError = "Parameter [currentDB] from URL is empty or null";
                return false;
            }
            if (string.IsNullOrWhiteSpace(callBackServer))
            {
                strError = "Parameter [callBackServer] from URL is empty or null";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从 ClickOnce 激活 URL 解析 Fold 参数。
        /// 仅当 command=-Fold 时进入完整解析；其它 command 返回 false（交给其它分支）。
        /// </summary>
        public bool ParseURL(string strURI)
        {
            isOk = false;
            parseError = parseAdvice = parseStack = null;
            try
            {
                if (string.IsNullOrWhiteSpace(strURI))
                {
                    parseError = "Activation URI is empty";
                    parseAdvice = "Please start Engine from MARS GUI";
                    parseStack = Environment.StackTrace;
                    return false;
                }

                Uri uriFromClick = new Uri(strURI);
                var query = System.Web.HttpUtility.ParseQueryString(uriFromClick.Query);

                command = query.Get(QUERY_COMMAND);
                if (string.IsNullOrEmpty(command)
                    || !string.Equals(command, AutoTestingDriverEntry.cnst_uri_command_fold, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                userName = query.Get(QUERY_USER_NAME);
                uuid = query.Get(QUERY_UUID) ?? query.Get(Mars.message.Utility.MarsConstants.CNST_QUERY_URL_UUID_PARA_NAME);

                storyBoardName = query.Get(QUERY_STORYBOARD_NAME);
                if (string.IsNullOrEmpty(storyBoardName))
                    storyBoardName = query.Get(QUERY_STORYBOARD_NAME_TYPO);

                storyBoardId = query.Get(QUERY_STORYBOARD_ID);
                appId = query.Get(QUERY_APP);
                mode = query.Get(QUERY_MODE);
                currentDB = query.Get(QUERY_CURRENT_DB);
                callBackServer = query.Get(QUERY_CALLBACK_SERVER);

                string strContinue = query.Get(QUERY_CONTINUE);
                if (string.IsNullOrWhiteSpace(strContinue) || !bool.TryParse(strContinue.Trim(), out bool cont))
                {
                    parseError = $"Parameter [Continue] missing or invalid:[{strContinue}]";
                    parseAdvice = "Continue must be True or False";
                    parseStack = Environment.StackTrace;
                    return false;
                }
                isContinue = cont;

                string strIgnore = query.Get(QUERY_IGNORE_ERROR);
                // IgnoreError 允许缺省为 False，与常见 Storyboard URL 行为兼容
                if (string.IsNullOrWhiteSpace(strIgnore))
                    ignoreError = false;
                else if (!bool.TryParse(strIgnore.Trim(), out bool ign))
                {
                    parseError = $"Parameter [IgnoreError] is not a boolean:[{strIgnore}]";
                    parseAdvice = "IgnoreError must be True or False";
                    parseStack = Environment.StackTrace;
                    return false;
                }
                else
                    ignoreError = ign;

                string strError = null, strAdv = null, strStack = null;
                if (!Validate(ref strError, ref strAdv, ref strStack))
                {
                    parseError = strError;
                    parseAdvice = strAdv;
                    parseStack = strStack;
                    return false;
                }

                isOk = true;
                return true;
            }
            catch (Exception e)
            {
                parseError = $"Error while parsing Fold URL parameters: {e.Message}";
                parseAdvice = "Unidentified error. If this continues, contact Marquis";
                parseStack = $"{e.Message}\r\n{e.StackTrace}";
                isOk = false;
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
