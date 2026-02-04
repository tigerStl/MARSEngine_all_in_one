using Mars.Inter.MQCenter.DataStructure;
using Mars.Inter.MQCenter.interProcess;
using Mars.Inter.MQCenter.interProcess.UIAutomation;
using Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support;
using Mars.Inter.MQCenter.objectEngine;
using Mars.Inter.MQCenter.objectSpy;
using Mars.Inter.MQCenter.windowsControlsHelpers;
using Mars.message.Hooks.Utilities;
using Mars.message.Inter.MQCenter.hooks;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.MarsObjectsOperations;
using Mars.message.Inter.MQCenter.objectTypeMapping;
using Mars.message.Inter.MQCenter.ThirdPartComponent.Infragistics;
using Mars.message.Utility;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Text;
using System.IO.Packaging;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Security.RightsManagement;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
//using System.Windows.Forms.Integration;
using System.Windows.Markup.Localizer;
using System.Windows.Navigation;
using static Mars.message.Hooks.Utilities.MarsGlobalKeyboardHook;
using static Mars.message.Hooks.Utilities.SendKeysConverter;
using static Mars.message.Inter.MQCenter.hooks.MarsGlobalMouseHook;
using Wpf = System.Windows;

namespace Mars.message.Inter.MQCenter.HttpRestService
{
    
    public class MarsRestfulRecordReplayConstant
    {
        public const string ERROR_NO_PEGWINDOW_QUICKACCESS = "ERROR_NO_PEGWINDOW_QUICKACCESS";
    }

    public class KeywordControlTypeMapping
    {
        public string keyword { get; set; }
        public List<MarsKeywordOP> keyworkdOP { get; set; }
    }

    public class MarsKeywordOP
    {
        public string control_type { get; set; }
        public string mars_type { get; set; }
        public List<string> opProps { get; set; }
    }

    public class KeywordControlTypeMappingMgmt
    {
        public const string cnst_mapping_fileName = "MarsEngineKeywordMapping.json";
        public static List<KeywordControlTypeMapping> keywordTypeOpMap = null;

        public static void saveApiDataToFile(string strDataFromAPI, ref string strError, ref bool isOk)
        {
            string currentLoc = typeof(KeywordControlTypeMappingMgmt).Assembly.Location;
            string rootPath = System.IO.Path.GetDirectoryName(currentLoc);
            string strDtaFileWithPath = System.IO.Path.Combine(rootPath, "data\\keywordTypeEngine", cnst_mapping_fileName);
            try
            {
                JsonDocument jsonDoc = JsonDocument.Parse(strDataFromAPI);
                string strData = jsonDoc.RootElement.GetProperty("Data").GetString();
                //isOk = true;
                if (System.IO.File.Exists(strDtaFileWithPath))
                {
                    System.IO.File.Delete(strDtaFileWithPath);
                }
                System.IO.File.WriteAllText(strDtaFileWithPath, strData);
                isOk = true;
                return;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = e.Message;

            }
        }
        public static List<KeywordControlTypeMapping> loadFromFile(ref string strError, ref bool isOk)
        {
            string currentLoc = typeof(KeywordControlTypeMappingMgmt).Assembly.Location;
            string rootPath = System.IO.Path.GetDirectoryName(currentLoc);
            string strDtaFileWithPath = System.IO.Path.Combine(rootPath, "data\\keywordTypeEngine", cnst_mapping_fileName);
            if (!System.IO.File.Exists(strDtaFileWithPath))
            {
                isOk = true;
                strError = $"no such file exist|{strDtaFileWithPath}|";
                return null;
            }
            try
            {
                string jsonData = System.IO.File.ReadAllText(strDtaFileWithPath);
                var rslt = JsonSerializer.Deserialize<List<KeywordControlTypeMapping>>(jsonData);
                if (rslt != null)
                {
                    isOk = true;
                    return keywordTypeOpMap = rslt;
                }
                isOk = false;
                strError = $"Can't load |{strDtaFileWithPath}|, result is null";
                return keywordTypeOpMap = null;
            }
            catch (Exception e)
            {
                isOk = false;
                strError = $"Exception when load file|{e.Message}|";
                simpleLog.MarsLoggerSimple.Error("loadFromFile", e.Message, e);
                return keywordTypeOpMap = null;
            }
        }

        public static KeywordControlTypeMapping GetKeywordOp(string strKeyword)
        {
            simpleLog.MarsLoggerSimple.DEBUG("GetKeywordOp",$"BEGIN {strKeyword}");
            if (keywordTypeOpMap == null) return null;
            var keyControlTypMap = keywordTypeOpMap.FirstOrDefault(p => string.Compare(p.keyword, strKeyword, true) == 0);
            if (keyControlTypMap != null) return keyControlTypMap;
            return null;
        }
    }
    [Serializable]
    public class MarsRecordReplayBase
    {
        public const string cnst_packagetype_record = "package_record";
        public const string cnst_packagetype_obj    = "package_obj";

        /// <summary>
        /// 事件发生时间
        /// </summary>
        public DateTime eventTime { get; set; }
        

        public string status { get; set; }
        public string message
        {
            get; set;
        }
        public string packType { get; set; }

        public MarsRecordReplayBase(string typ)
        {
            eventTime = DateTime.Now;
            packType = typ;
        }
    }
    [Serializable]
    public class MarsRectangle
    {
        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }

        public static MarsRectangle FromRectangle(Rectangle rect)
        {
            return new MarsRectangle()
            {
                x = rect.X,
                y = rect.Y,
                w = rect.Width,
                h = rect.Height
            };
        }
    }
    [Serializable]
    public class MarsRecordReplayStep: MarsRecordReplayBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MarsRecordReplayStep(string typ) : base(typ)
        {

        }
        public MarsRecordReplayStep() : base(MarsRecordReplayStep.cnst_packagetype_record)
        {

        }

        #region for step generate
        public string keyWord { get; set; }
        public string opText { get; set; }
        public string opData { get; set; }
        public string objectType { get; set; }
        public string objectFullTypes { get; set; }
        public string objectMarsType { get; set; } // like swfedit, swfbutton
        public MarsRectangle bound { get; set; }


        /// <summary>
        /// table字段名称
        /// </summary>
        public string tableExtension_column { get; set; }
        /// <summary>
        /// cell text
        /// </summary>
        public string tableExtension_text { get; set; }
        public int tableExtension_RowId { get; set; }

        public int runOrder { get;set;}

        private MarsSpiedObjInfoAI _pegQuickAccess;
        public MarsSpiedObjInfoAI pegQuickAccess { 
            get => _pegQuickAccess; 
            set { 
                _pegQuickAccess = value;
                OnPropertyChanged("pegQuickAccess");
                OnPropertyChanged("TestStepInfo"); 
            }
        }
        private MarsSpiedObjInfoAI _objectQuickAccess;
        
        public MarsSpiedObjInfoAI objectQuickAccess { get =>_objectQuickAccess; 
            set {

                _objectQuickAccess = value;
                OnPropertyChanged("objectQuickAccess");
                OnPropertyChanged("TestStepInfo");
            }
        }
        #endregion

        //private string _testStepInfo = null;
        public string TestStepInfo
        {
            get
            {
                return toMarsStep();
            }            
        }

        public static List<string> cnst_keywords_not_require_objects = new List<string>
        {
            MarsObjectKeyword.cnst_clickat,
            MarsObjectKeyword.cnst_pressKeys,
            MarsObjectKeyword.cnst_dismiss,
            MarsObjectKeyword.cnst_waitForSeconds
        };
        /// <summary>
        /// 产生可以执行的测试step。这里需要注意的是，有些keyword可以没有pegwindow和object的信息
        /// 如，dismiss，PressKeys，也可能有。需要判断
        /// </summary>
        /// <returns></returns>
        public string toMarsStep()
        {
            bool isWithObject = true;
            var notRequireObj = cnst_keywords_not_require_objects.FirstOrDefault(p => p.Equals(keyWord, StringComparison.OrdinalIgnoreCase));
            if (notRequireObj != null) {
                if (pegQuickAccess == null) { 
                    isWithObject = false;
                }
            }
            if (isWithObject)
            {
                var pegPart = $"PegWindow(\"{pegQuickAccess?.toMarsObjString()}\")";
                if (string.Compare(keyWord, "Pegwindow", true) == 0) return pegPart;
                string strPara = this.Parameter;
                if (string.Compare(keyWord, MarsObjectKeyword.cnst_searchAndClick, true) == 0)
                {
                    string strSearchAndClickPara = GenerateParameterForSearchAndClick();
                    return $"{pegPart}.{keyWord}(\"{objectQuickAccess?.toMarsObjString()}\", '{strSearchAndClickPara}', '{this.tableExtension_text}')";
                }
                //if (string.Compare(keyWord, MarsObjectKeyword.cnst_pressKeys, true) == 0)
                //{
                //    strPara = MarsObjectKeyword.cnst_keyword_para_CURRENT_POS;//当前位置
                //    opText = opData?.ToString();
                //}
                return $"{pegPart}.{keyWord}(\"{objectQuickAccess?.toMarsObjString()}\",'{strPara}', '{opText}')";
            }
            else
            {
                ///不需要对象模式
                ///
                return $"{keyWord}('', '{this.Parameter}', '{opText}')";
            }
        }

        private string GenerateParameterForSearchAndClick()
        {
            /// search and click 的参数包括几类，一类是normal，就是查询
            /// 目前先完成对normal模式的支持
            return this.Parameter = $"MarsAddins;{this.tableExtension_column};Action:LEFT_CLICK";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strMarsObjType"></param>
        /// <param name="c"></param>
        /// <param name="isCtrlPressed"></param>
        /// <param name="current2StepObject"></param>
        /// <param name="iRowId"></param>
        /// <param name="strColumnName"></param>
        /// <param name="currentCellText"></param>
        /// <param name="strData">如果是toolbar里面的button，那么这个值是caption</param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="isIgoreThisStep"></param>
        /// <param name="mouseEvent"></param>
        /// <param name="toolbarItmType">used only for toolbars,仅对toolbar</param>
        /// <returns></returns>
        public static string GenKeywordBasedOnObjMarsType(string strMarsObjType, 
            System.Windows.Forms.Control c, 
            bool isCtrlPressed, Mars2StepObject current2StepObject,
            int iRowId, string strColumnName, string currentCellText, // used for table 
            ref string strData,
            ref bool isOk, 
            ref string strError,
            ref bool isIgoreThisStep,
            MouseMessages mouseEvent=MouseMessages.WM_NONE,
            MarsToolbarItemTyp toolbarItmType = MarsToolbarItemTyp.tool_unknow)
        {
            bool isFromMouse = mouseEvent!=MouseMessages.WM_NONE;
            if (string.IsNullOrEmpty(strMarsObjType))
            {
                isOk = false;
                strError = "object type is null or empty";
                simpleLog.MarsLoggerSimple.Error("GenKeywordBasedOnObjMarsType", strError);
                return null;
            }
            isOk = true;
            ReflectorForCSharp rfl = new ReflectorForCSharp();
            bool isNotExists = false;
            switch (strMarsObjType)
            {
                case MarsObjectTypeMappings.cnst_swf_edit:
                    if (isFromMouse)
                    {
                        /// is ctonrol pressed
                        if (isCtrlPressed)
                        {
                            return MarsObjectKeyword.cnst_captureValue;
                        }
                        isOk = true;
                        isIgoreThisStep = true;
                        return null;
                    }
                    isOk = true;
                    strData = c.Text;
                    return MarsObjectKeyword.cnst_filledit;
                case MarsObjectTypeMappings.cnst_swf_Checkbox:
                    if (c is System.Windows.Forms.CheckBox)
                    {
                        strData = MarsObjectOp.GetCheckboxValue(c, ref isOk, ref strError);
                        return MarsObjectKeyword.cnst_setbox;
                    }
                    isOk = false;
                    strError = $"can't convert control to System.Windows.Forms.CheckBox|{c.GetType().FullName}";
                    simpleLog.MarsLoggerSimple.Error("GenKeywordBasedOnObjMarsType", strError);
                    return null;
                case MarsObjectTypeMappings.cnst_swf_Combobox:
                    if (isFromMouse)
                    {
                        if ((current2StepObject == null) || (current2StepObject.currentStep <= 0))
                        {
                            isIgoreThisStep = true;
                            isOk = true;
                            simpleLog.MarsLoggerSimple.Info("GenKeywordBasedOnObjMarsType", "2 steps object record is null|or it is the first click, return");
                            return null;
                        }
                        if (current2StepObject.objectFor2nd == null)
                        {
                            isOk = false;
                            simpleLog.MarsLoggerSimple.Error("GenKeywordBasedOnObjMarsType", strError = "the second object for 2 steps object operation's is null");
                            return null;
                        }
                        // get text from objectfor2nd
                        var oT = rfl.GetMember<string>(current2StepObject.objectFor2nd, "Text", ref isNotExists);
                        if (isNotExists)
                        {
                            simpleLog.MarsLoggerSimple.Error("GenKeywordBasedOnObjMarsType", strError = $"no such 'Text' member exists in type|{current2StepObject.objectFor2nd.GetType().FullName}|");
                            isOk = false;
                            return null;
                        }
                        strData = oT;
                        isOk = true;
                        isIgoreThisStep = false;
                        return MarsObjectKeyword.cnst_selectdropdown;
                    }
                    else
                    {
                        // from keyboard
                        var oT = rfl.GetMember<string>(c, "Text", ref isNotExists);
                        if (isNotExists)
                        {
                            simpleLog.MarsLoggerSimple.Error("GenKeywordBasedOnObjMarsType", strError = $"no such 'Text' member exists in type|{current2StepObject.objectFor2nd.GetType().FullName}|");
                            isOk = false;
                            return null;
                        }
                        strData = oT;
                        isOk = true;
                        return MarsObjectKeyword.cnst_selectdropdown;
                    }
                case MarsObjectTypeMappings.cnst_swf_Button:
                    if (isCtrlPressed)
                    {
                        return MarsObjectKeyword.cnst_captureValue;
                    }
                    if (!isFromMouse)
                    {
                        isIgoreThisStep = true;
                        return null;
                    }
                    return MarsObjectKeyword.cnst_clickbutton;
                case MarsObjectTypeMappings.cnst_swf_Label:
                    if (isFromMouse&&isCtrlPressed)
                    {
                        return MarsObjectKeyword.cnst_captureValue;
                    }
                    isIgoreThisStep= true;
                    return null;
                case MarsObjectTypeMappings.cnst_swf_Table:
                    /// 对于table而言，通常有filltable，或者searchAndClick，searchAndUpdate, clickAt.
                    /// filltable 是在两次Click过程中，或者press enter/tab时候，存在cell的text的变化，
                    /// 或者是searchAndUpdate（该keyword复杂，所以在record&replay时候，只用searchAndClick，filltable）。
                    /// 如果开始的cell的值是空，而后不为空，那么，就是filltable，或者cell的表达式是，如果前后两次的click都是同一个
                    /// cell，那么，就适用searchAndClick，如果是右键click，那么必然是SearchAndClick
                    /// 对于左键单击，则创建SearchAndClick
                    /// 
                    isOk = false;
                    if (mouseEvent == MouseMessages.WM_LBUTTONUP)
                    {
                        isOk = true;
                        /// button up就是click事件
                        /// 
                        if (string.IsNullOrEmpty(currentCellText))
                        {
                            return MarsObjectKeyword.cnst_clickat;
                        }
                        return MarsObjectKeyword.cnst_searchAndClick;
                    }
                    else if (mouseEvent == MouseMessages.WM_RBUTTONUP)
                    {
                        isOk = true;
                        /// 需要获得对象的菜单句柄， 如果单元格为空，则是clickAt
                        /// 
                        return MarsObjectKeyword.cnst_searchAndClick;
                    } else if (mouseEvent == MouseMessages.WM_LBUTTONDBLCLK)
                    {
                        isOk = true;
                        return MarsObjectKeyword.cnst_clickat;
                    }
                    strError = $"unsupported mouse event|{mouseEvent}| for object type|{c.GetType().FullName}";
                    return null;
                case MarsObjectTypeMappings.cnst_swf_ToolBar:
                    switch (toolbarItmType)
                    {
                        case MarsToolbarItemTyp.tool_button:
                            isOk = true;
                            return MarsObjectKeyword.cnst_clickMenuIcon;
                        default:
                            isOk = false;
                            return $"unsupport type of tool_bar|{toolbarItmType}";
                    };
                case MarsObjectTypeMappings.cnst_swf_Menu:
                    isOk = true;
                    return MarsObjectKeyword.cnst_selectMenuItem;
                case MarsObjectTypeMappings.cnst_swf_ListView:
                case MarsObjectTypeMappings.cnst_swf_Tree:
                    isOk = true;
                    return MarsObjectKeyword.cnst_selectListItem;
                default:
                    strError = $"Unimplemented for object|{c.GetType().FullName}";
                    isOk = false;
                    return null;
            }            
        }

        public string Parameter { 
            get; 
            set; }

        [JsonIgnore]
        public IntPtr objectHandle { get; set; }
        [JsonIgnore]
        public object objectToOp { get; set; }
        [JsonIgnore]
        public List<MarsKeyPressStatus> pressedKeys { get; set; } = new List<MarsKeyPressStatus>();
        [JsonIgnore]
        public MouseMessages mouseEvent { get; set; }       
        public Dictionary<string,string> GetPegInfo()
        {
            if (pegQuickAccess == null) return null;
            Dictionary<string, string> targetDictionary = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(pegQuickAccess.objectName))
            {
                targetDictionary.Add("SwfName", pegQuickAccess.objectName);
            }
            if (string.IsNullOrEmpty(pegQuickAccess.objectNamePath))
            {
                targetDictionary.Add("SwfName Path", pegQuickAccess.objectNamePath);
            }            
            return targetDictionary;
        }
        public Dictionary<string, string> GetObjInfo()
        {
            if (objectQuickAccess == null) return null;
            Dictionary<string, string> targetDictionary = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(objectQuickAccess.objectName))
            {
                targetDictionary.Add("SwfName", objectQuickAccess.objectName);
            }
            if (string.IsNullOrEmpty(objectQuickAccess.objectNamePath))
            {
                targetDictionary.Add("SwfName Path", objectQuickAccess.objectNamePath);
            }
            return targetDictionary;
        }
        /// <summary>
        /// 构建swfname:=xxxx形式的quickAccess
        /// </summary>
        /// <returns></returns>
        public string BuildObjectQuickAccess()
        {
            string strRslt = "";
            if (objectQuickAccess == null) return null;
            if (!string.IsNullOrEmpty(objectQuickAccess.objectName))
            {
                strRslt = $"swfName:={objectQuickAccess.objectName}";                
            }
            if (string.IsNullOrEmpty(objectQuickAccess.objectNamePath))
            {
                if (string.IsNullOrEmpty(strRslt))
                {
                    strRslt = $"swfName Path:={objectQuickAccess.objectNamePath}";
                }else
                {
                    strRslt = $"{strRslt}\r\nswfName Path:={objectQuickAccess.objectNamePath}";
                }                
            }
            
            return strRslt;
        }

        public MarsRecordReplayStep cloneToANew()
        {
            using (var ms = new System.IO.MemoryStream())
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(ms, this);
                ms.Position = 0;
                return (MarsRecordReplayStep)formatter.Deserialize(ms);
            }
        }

        public string ConvertLastKeyPressToOpText(bool isShiftPressed)
        {
            if ((this.pressedKeys==null)||(this.pressedKeys.Count<=0)) return null;
            var lastKeyPress = this.pressedKeys.LastOrDefault();
            if (lastKeyPress==null) return null;
            return SendKeysConverter.ConvertVKeysToSendKeys(new List<MarsKeyPressStatus>() { lastKeyPress }, isShiftPressed);
        }

        public MarsRecordReplayStep CreatePegKeyword()
        {
            return new MarsRecordReplayStep()
            {
                keyWord = MarsObjectKeyword.cnst_pegwindow,
                pegQuickAccess = this.pegQuickAccess,
                _objectQuickAccess = this.pegQuickAccess,
                objectMarsType = "PegWindow",                
            };
        }

    }
    
    public class MarsRecordAndReplayOpLogManagement
    {
        public static List<MarsRecordReplayStep> opStepLog { get; set; } = new List<MarsRecordReplayStep>();
        public static MarsRecordReplayStep currentRecordAndReplaySteps {
            get {
                if (opStepLog.Count < 1)
                    opStepLog.Add(new MarsRecordReplayStep(MarsRecordReplayStep.cnst_packagetype_record));
                return opStepLog[opStepLog.Count-1];
            } 
        }

        /// <summary>
        ///                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             
        /// </summary>
        /// <param name="hdl"></param>
        /// <returns></returns>
        public static MarsRecordReplayStep CreateOrGetOpObjects(IntPtr hdl)
        {
            var obj = currentRecordAndReplaySteps;
            if (obj.objectHandle.Equals(hdl)) return obj;
            var rstl = new MarsRecordReplayStep()
            {
                objectHandle = hdl
            };
            opStepLog.Append(rstl);
            return rstl;
        }
        /// <summary>
        /// 將每一次鼠标点击作为一个新的操作
        /// </summary>
        /// <param name="stepInfo"></param>
        /// <param name="checkLast"></param>
        public static void AddOpObjWhenClick(MarsRecordReplayStep stepInfo,bool checkLast=true)
        {
            if (stepInfo == null) return;
            //if (currentRecordAndReplaySteps.objectHandle.Equals(stepInfo.objectHandle))
            //{
            //    // replace the laste item by the new one
            //    opStepLog.RemoveAt(opStepLog.Count - 1);                
            //}
            //opStepLog.Append(stepInfo);
            opStepLog.Add(stepInfo);
        }

        public static void CleanBuffedSteps()
        {
            opStepLog.Clear();
        }
        /// <summary>
        /// 创建一个系统标识step，用于通知前端，暂时停止record，然后需要删除最后
        /// </summary>
        internal static void createEndRecordingMark()
        {
            opStepLog.Add(new MarsRecordReplayStep()
            {
                keyWord = MarsObjectKeyword.cnst_pauseRecord,
            });
        }        
    }
    
    public class MarsRecordReplayInfo
    {
        public string status { get; set; }
        public string message { get; set; }
        public MarsRecordReplayStep step { get; set; }
    }

    public class MarsRecordReplayLog
    {
        public List<MarsRecordReplayInfo> TestStepFromRecord { get; set; }
    }

    public class Mars2StepObject
    {
        public string marsObjectType { get; set; } = null;
        public System.Windows.Forms.Control currentControl { get; set; } = null; 
        public int currentStep { get; set; } = -1; // if 0 then, the first click is tested, if -1 then inited, just
        public object objectFor2nd { get; set; } = null;
        public bool isTwoStepEvent { get; set; } = false;
        public IntPtr previousObjectHandle { get; set; } = IntPtr.Zero;
        public IntPtr currentObjectHandle { get; set; } = IntPtr.Zero;

        public void init()
        {
            marsObjectType = string.Empty;
            isTwoStepEvent = false;
            previousObjectHandle = IntPtr.Zero;
            currentObjectHandle = IntPtr.Zero;
            currentStep = -1;
            currentControl = null;
        }
        public bool IsTwoStepsControlFoucs()
        {
            if (currentControl == null) return false;
            return currentControl.Focused;
        }
    }

    public class MarsSpyRESTfulServer
    {
        private static int gCurrentMode = 0;// 0- object spy, 1- record and replay
        private static MarsGlobalKeyboardHook gkeyboardHook = null;
        private static MarsGlobalMouseHook gmouseHook = null;

        private static MarsRecordReplayLog marRecordReplayLog = new MarsRecordReplayLog();
        /// <summary>
        /// 用来记录两次事件间的距离，从而判断是否需要添加waitforsenconds
        /// </summary>
        public static long previouseActivityTimeSpan { get; set; } = 0;

        /// <summary>
        /// start record and replay mode
        /// steps:
        /// 1, start mouse hook and keyboard hook
        /// 2, hot key is ctrl+/ then all hooks should stop
        /// 3, if a tab or enter is pressed, get the current windows text
        /// 4, event is mouse left button down, get the object type, if object type is swfbutton, 
        /// </summary>
        public static void StartInternalRecordReplayRestSvc(string injectType="Normal")
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin($"{iMark}|StartInternalRecordReplayRestSvc|begin...|injectType|{injectType}");
            /// 
            gCurrentMode = 1;
            try
            {
                GetStepJsonFilePath();
                // start keyboard hook
                gkeyboardHook            = MarsGlobalKeyboardHook.startKeyboardHook();
                //if (gkeyboardHook.KeyUp != null)
                gkeyboardHook.KeyUp     -= MarsRecordKeyUpImp;
                gkeyboardHook.KeyDown   -= MarsRecordKeyDownImp;
                gkeyboardHook.KeyUp     += MarsRecordKeyUpImp;
                gkeyboardHook.KeyDown   += MarsRecordKeyDownImp;
                gkeyboardHook.Install();
                // start mouse hook
                gmouseHook               = MarsGlobalMouseHook.startMouseHook();
                //gmouseHook.
                gmouseHook.Click        -= MarsMouseClickImp;
                gmouseHook.Click        += MarsMouseClickImp;
                //gmouseHook.DoubleClick += 
                gmouseHook.Install();

                StartRecordingHintForm.BeginToRecord(true);
                previouseActivityTimeSpan = DateTime.Now.Ticks;

                //启动restserver
                //                   Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer
                bool isSocketReady = Mars.message.Inter.MQCenter.interProcess.HttpRestService.MarsSpyRESTfulServer.StartInternalSpyRestSvc();
                if (!isSocketReady)
                {
                    simpleLog.MarsLoggerSimple.Error($"{iMark}|StartInternalRecordReplayRestSvc","|can't start web socket server");
                }
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd($"{iMark}|StartInternalRecordReplayRestSvc|end.");
            }
        }

        private static string currentStepJsonFilePath = null;
        private static void GetStepJsonFilePath()
        {
            string strPath = typeof(MarsSpyRESTfulServer).Assembly.Location;
            strPath = System.IO.Path.GetDirectoryName(strPath);
            string UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            strPath = System.IO.Path.Combine(strPath, $"data\\obj\\{UserName}");
            if (!System.IO.Directory.Exists(strPath))
                System.IO.Directory.CreateDirectory(strPath);
            currentStepJsonFilePath = System.IO.Path.Combine(strPath, MarsConstants.CNST_SYPTOOL_STEPS_FILENAME);
        }
        /// <summary>
        /// 依据table选择key press
        /// 1, 依据vkey组合获得有效的string
        /// 2, 获得点击时候的column 名称
        /// 3, 
        /// </summary>
        /// <param name="stepInfo"></param>
        /// <param name="isCntrlPressed"></param>
        /// <param name="isIgnore"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static bool BuildTestStepInfoForTableKeyPress(MarsRecordReplayStep stepInfo, bool isCntrlPressed,  ref bool isIgnore, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("BuildTestStepInfoForTable", $"object type|{stepInfo?.objectFullTypes}|controlIsPressed|{isCntrlPressed}");
            try
            {
                bool isOk = false;
                MarsObjectOp objOp = new MarsObjectOp();
                stepInfo.keyWord = MarsObjectKeyword.cnst_pressKeys;
                stepInfo.opData = SendKeysConverter.ConvertVKeysToSendKeys(stepInfo.pressedKeys, false);
                stepInfo.pegQuickAccess = objOp.getControlPeg(stepInfo.objectToOp as System.Windows.Forms.Control, ref isOk, ref strError);
                stepInfo.objectQuickAccess = objOp.getControlQuick(stepInfo.objectToOp as System.Windows.Forms.Control, ref isOk, ref strError);

                currentRecordSteps.Add(stepInfo);

                /// save to file
                /// 
                SaveJsonStepToFile();
                return true;
            }
            catch(Exception e)
            {
                strError = e.Message;
                simpleLog.MarsLoggerSimple.Error("BuildTestStepInfoForTableKeyPress", e.Message, e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("BuildTestStepInfoForTable");
            }
        }

        public static bool RemoveRecordReplayStepsByRunId(int iOrd, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("RemoveRecordReplayStepsByRunId", $"to remove id |{iOrd}");
            try
            {
                if (currentRecordSteps == null)
                {
                    strError = "No Test steps is genrated";
                    return false;
                }
                if ((iOrd <= 0) || (iOrd > currentRecordSteps.Count))
                {
                    strError = $"run order is out of range|1-{currentRecordSteps.Count}|";
                    return false;
                }
                var stp = currentRecordSteps.FirstOrDefault(p => p.runOrder == iOrd);
                if (stp == null)
                {
                    strError = $"can't find such step |{iOrd}";
                    return false;
                }
                currentRecordSteps.Remove(stp);
                return true;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("RemoveRecordReplayStepsByRunId", strError);
            }
        }

        /// <summary>
        /// 创建ctrl presskeys
        /// </summary>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static bool CreateAPressKeysStepForCtrl(Mars.message.Hooks.Utilities.VKeys key,bool isShiftPressed,  ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("CreateAPressKeysStepForCtrl");
            try
            {
                bool isOk = true;
                IntPtr hwndFocused = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetFocus();
                if (hwndFocused == IntPtr.Zero)
                {
                    strError = "GetFocus return Zero";
                    simpleLog.MarsLoggerSimple.Error("CreateAPressKeysStepForCtrl", strError);
                    return false;
                }
                var c = System.Windows.Forms.Control.FromHandle(hwndFocused);

                MarsRecordReplayStep pressKeysStep = new MarsRecordReplayStep();
                pressKeysStep.keyWord = MarsObjectKeyword.cnst_pressKeys;
                //pressKeysStep.Parameter = MarsObjectKeyword.cnst_keyword_para_CURRENT_POS;
                string opText = SendKeysConverter.ConvertVKeysToSendKeys(new List<MarsKeyPressStatus>() { new MarsKeyPressStatus(){
                    key = key,
                    isContorlPress = true,
                    isShiftPress = isShiftPressed
                }}, isShiftPressed);
                pressKeysStep.opText = opText;
                pressKeysStep.opData = opText; 

                MarsObjectOp objOp = new MarsObjectOp();
                pressKeysStep.objectQuickAccess = objOp.getControlQuick(c, ref isOk, ref strError);
                pressKeysStep.pegQuickAccess = objOp.getControlPeg(c, ref isOk, ref strError);
                currentRecordSteps.Add( pressKeysStep );
                return true;
            }
            catch (Exception e) {
                strError = e.Message;
                simpleLog.MarsLoggerSimple.Error("CreateAPressKeysStepForCtrl", $"Exception|{strError}", e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("CreateAPressKeysStepForCtrl");
            }
        }

        /// <summary>
        /// 将test step list中的最后一条完成，包括生成keyword，
        /// </summary>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static bool GenerateTestStepForLastStep(bool isCtrlPressed, ref string strError, bool removeWhenTextIsEmpty = false)
        {
            simpleLog.MarsLoggerSimple.logBegin("GenerateTestStepForLastStep", $"isCtrlPressed is|{isCtrlPressed}|removeWhenTextIsEmpty|{removeWhenTextIsEmpty}");
            try
            {
                bool isCreatedLatest = false;
                bool isOk = true;
                MarsObjectOp objOp = new MarsObjectOp();
                if (currentRecordSteps == null)
                    currentRecordSteps = new List<MarsRecordReplayStep>();
                if (currentRecordSteps.Count <= 0)
                {
                    currentRecordSteps.Add(new MarsRecordReplayStep());
                    isCreatedLatest = true;
                }
                var lastStep = currentRecordSteps.LastOrDefault();
                if (lastStep.objectHandle.Equals(IntPtr.Zero))
                {
                    lastStep.objectHandle = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetFocus();
                }
                var c = System.Windows.Forms.Control.FromHandle(lastStep.objectHandle);
                if (c == null) {
                    if (isCreatedLatest)
                        currentRecordSteps.RemoveAt(currentRecordSteps.Count-1);
                    strError = $"can't create control from handle";
                    simpleLog.MarsLoggerSimple.Error("GenerateTestStepForLastStep", strError);
                    return false ;
                }
                if (!removeWhenTextIsEmpty)
                {
                    lastStep.opText = c.Text;
                }
                else
                {
                    if (string.IsNullOrEmpty(c.Text))
                    {
                        currentRecordSteps.Remove(lastStep);
                        simpleLog.MarsLoggerSimple.Info("GenerateTestStepForLastStep", $"THE step has been removed as removeWhenTextIsEmpty is |{removeWhenTextIsEmpty}|");
                        return true;
                    }
                }
                lastStep.bound = MarsRectangle.FromRectangle(c.RectangleToScreen(new Rectangle(System.Drawing.Point.Empty, c.Size)));
                lastStep.objectType = c.GetType().FullName;
                lastStep.objectFullTypes = ReflectorForCSharp.GetObjectBaseType(c.GetType(), true);
                // get type
                lastStep.objectMarsType = objOp.getMarsObject(c);
                if (lastStep.pegQuickAccess == null)
                {
                    lastStep.pegQuickAccess = objOp.getControlPeg(c, ref isOk, ref strError);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("GenerateTestStepForLastStep", $"Can't get Control Peg|{strError}");
                        return false;
                    }
                }
                if (lastStep.objectQuickAccess == null) {
                    lastStep.objectQuickAccess = objOp.getControlQuick(c, ref isOk, ref strError);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("GenerateTestStepForLastStep", $"Can't get Control quick|{strError}");
                        return false;
                    }
                }
                string strOpData = "";
                bool isIgnore = false;
                lastStep.keyWord = MarsRecordReplayStep.GenKeywordBasedOnObjMarsType(lastStep.objectMarsType, c,
                    isCtrlPressed, current2StepObject,
                    -1, null, null,
                    ref strOpData, ref isOk, ref strError,
                    ref isIgnore);
                if (!isOk)
                {
                    simpleLog.MarsLoggerSimple.Error("GenerateTestStepForLastStep", strError);
                    return false;
                }
                return true;
            }
            catch(Exception e)
            {
                strError = e.Message;
                simpleLog.MarsLoggerSimple.Error("GenerateTestStepForLastStep", $"Exception|{strError}", e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GenerateTestStepForLastStep");
            }
        }


        private static List<MarsRecordReplayStep> currentRecordSteps = new List<MarsRecordReplayStep>();
        private static bool GenerateTestStepString(ref bool isIgnore, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("GenerateTestStepString");
            try
            {
                #region table extra data
                int iRowId = -1;
                string columnName = null;
                string cellText = null;
                #endregion

                /// step:
                /// 1, get current focused object
                /// 2, get object type
                /// 3, find right keyword
                /// 4, get text
                /// 5, create test step
                /// 
                IntPtr hwndFocused = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetFocus();
                if (hwndFocused == IntPtr.Zero)
                {
                    strError = "GetFocus return Zero";
                    return false;
                }
                /// only for winform current
                var c = System.Windows.Forms.Control.FromHandle(hwndFocused);
                if (c == null)
                {
                    strError = "Control.FromHandle return null";
                    return false; 
                }
                /// for some control, keyboard event will create an embed 
                /// 
                MarsRecordReplayStep currentInfo = new MarsRecordReplayStep(MarsRecordReplayBase.cnst_packagetype_record);
                MarsObjectOp objOp          = new MarsObjectOp();
                currentInfo.bound           = MarsRectangle.FromRectangle(c.RectangleToScreen(new Rectangle(System.Drawing.Point.Empty, c.Size)));
                currentInfo.opText          = c.Text;
                currentInfo.objectType      = c.GetType().FullName;
                currentInfo.objectFullTypes = ReflectorForCSharp.GetObjectBaseType(c.GetType(),true);
                // get type
                currentInfo.objectMarsType  = objOp.getMarsObject(c);
                string strOpData            = "";
                bool isOk                   = false;
                bool isCtrlPressed          = IsCtrlPressed();
                
                currentInfo.keyWord = MarsRecordReplayStep.GenKeywordBasedOnObjMarsType(currentInfo.objectMarsType, c,
                    isCtrlPressed,current2StepObject,
                    iRowId, columnName, cellText,
                    ref strOpData, ref isOk, ref strError, 
                    ref isIgnore);
                if (!isOk)
                {
                    simpleLog.MarsLoggerSimple.Error("GenerateTestStepString", strError);
                    return false;
                }
                if (isIgnore)
                {
                    return true;
                }
                currentInfo.pegQuickAccess = objOp.getControlPeg(c, ref isOk, ref strError);
                currentInfo.objectQuickAccess = objOp.getControlQuick(c, ref isOk, ref strError);
                simpleLog.MarsLoggerSimple.Info("GenerateTestStepString", $"add new |{currentInfo}");
                currentRecordSteps.Add(currentInfo);

                /// save to file
                /// 
                SaveJsonStepToFile();
                return true;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("GenerateTestStepString", strError=e.Message, e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("GenerateTestStepString");
            }
        }
        /// <summary>
        /// to ensuer there is no trash step or pause step
        /// </summary>
        public static void RemovePauseSteps(List<MarsRecordReplayStep> targetStepList)
        {
            int idx = targetStepList.Count - 1;
            while (idx >= 0)
            {
                try
                {
                    var stp = targetStepList[idx];
                    if (string.IsNullOrEmpty(stp.keyWord))
                    {
                        targetStepList.RemoveAt(idx);
                        continue;
                    }
                    if (string.Compare(stp.keyWord, MarsObjectKeyword.cnst_pauseRecord, true) == 0)
                    {
                        targetStepList.RemoveAt(idx);
                        continue;
                    }
                }
                finally
                {
                    idx--;
                }
            }
        }

        /// <summary>
        /// stps是需要添加到currentRecordSteps的记录
        /// </summary>
        /// <param name="stps"></param>
        /// <returns></returns>
        private static bool SaveJsonStepToFile(List<MarsRecordReplayStep> stps = null)
        {
            simpleLog.MarsLoggerSimple.logBegin("SaveJsonStepToFile");
            try
            {
                if (stps != null)
                {
                    currentRecordSteps.AddRange(stps);
                }
                // reset of run-order
                for (int runOrd = 1; runOrd <= currentRecordSteps.Count; runOrd++)
                {
                    var itm = currentRecordSteps[runOrd - 1];
                    itm.runOrder = runOrd;
                };
               
                string strSteps = System.Text.Json.JsonSerializer.Serialize<List<MarsRecordReplayStep>>(currentRecordSteps);
                //System.IO.File
                System.IO.File.WriteAllText(currentStepJsonFilePath, strSteps);
                return true;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("SaveJsonStepToFile", e.Message, e);
                return false;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("SaveJsonStepToFile");
            }
            
        }
        internal static bool IsCtrlPressed()
        {
            return (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetKeyState(VirtualKeyStates.VK_LCONTROL) != 0)
                || (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetKeyState(VirtualKeyStates.VK_RCONTROL) != 0);
        }

        internal static bool IsShiftDown()
        {
            return (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetKeyState(VirtualKeyStates.VK_LSHIFT) != 0)
                || (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetKeyState(VirtualKeyStates.VK_RSHIFT) != 0)
                || (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetKeyState(VirtualKeyStates.VK_SHIFT) != 0);
        }

        private static string MouseStructureToString(MSLLHOOKSTRUCT mouseStruct)
        {
            return $"{mouseStruct.pt}|{mouseStruct.flags}|at {mouseStruct.time}";
        }

        private static Mars2StepObject current2StepObject = null;

        private static MarsRecordReplayStep getMarsRecordReplayStepFromControl(System.Windows.Forms.Control c, 
            System.Drawing.Point pt, 
            ref bool isOk, 
            ref string strError,
            ref bool isIgnore,
            MouseMessages mouseEvent=MouseMessages.WM_NONE)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin($"{iMark}|getMarsRecordReplayStepFromControl");
            MarsToolbarItemTyp toolitemType = MarsToolbarItemTyp.tool_unknow;
            #region table extra data
            int iRowId = -1;
            string columnName = null;
            string cellText = null;
            #endregion 

            MarsRecordReplayStep currentInfo = new MarsRecordReplayStep(MarsRecordReplayBase.cnst_packagetype_record);
            MarsObjectOp objOp = new MarsObjectOp();
            currentInfo.bound = MarsRectangle.FromRectangle(c.RectangleToScreen(new Rectangle(System.Drawing.Point.Empty, c.Size))); 
            currentInfo.opText = c.Text;
            currentInfo.objectType = c.GetType().FullName;
            currentInfo.objectHandle = c.Handle;
            /// 可能是菜单
            simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl", $"type is|{currentInfo.objectType}|");
            currentInfo.objectFullTypes = ReflectorForCSharp.GetObjectBaseType(c.GetType(), true);
            string strOpData = "";
            string strNamePath = ReflectorForCSharp.MarsGetParentsNames(c);            
            // get type
            currentInfo.objectMarsType = objOp.getMarsObject(c);
            bool isCtrlPressed = IsCtrlPressed();
            string strCaption = "", strMainPreFix = "", menuPath = "";
            switch (currentInfo.objectMarsType)
            {
                case MarsObjectTypeMappings.cnst_swf_Combobox:
                    var childTmp = c.GetChildAtPoint(new System.Drawing.Point(c.Bounds.X + c.Width / 2, c.Bounds.Y + c.Height / 2));
                    if (childTmp != null)
                    {
                        simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl",
                            $"childTmp is|{childTmp.GetType()}|");
                    }
                    if (current2StepObject == null)
                    {
                        current2StepObject = new Mars2StepObject();
                    }
                    if (current2StepObject.currentStep == -1)
                    {
                        current2StepObject.currentStep = 0;
                        current2StepObject.marsObjectType = currentInfo.objectMarsType;
                        current2StepObject.currentControl = c;
                        c.TextChanged -= ComboxLostTextChangeEvent;
                        c.TextChanged += ComboxLostTextChangeEvent;
                        simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl", $"two steps objects and steps,name|{c.Name}|{strNamePath}| type is|{currentInfo.objectFullTypes}|");
                        
                    };
                    isOk = true;
                    isIgnore = true;
                    return null;
                    //else
                    //{ 
                    //    //第二次点击，do nothing
                    //    simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl", $"two steps objects and steps|{current2StepObject.currentStep} branch");
                    //    current2StepObject.objectFor2nd = c;
                    //    current2StepObject.currentStep++;
                    //    // get the object's parent's
                    //    Control cp = c.Parent;
                    //    if (cp != null)
                    //    {
                    //        simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl", $"2 steps objects, parent name|{cp.Name}|{cp.GetType()}|");                            
                    //        /// 
                    //        bool isNoExist = false;
                    //        var txt = ReflectorForCSharp.GetPropertyValueByPropertyNameIdx(c, "Text", ref isNoExist);
                    //        if (isNoExist)
                    //        {
                    //            /// 依据不同的类型采取不同的方式获得text
                    //            /// 
                    //            simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl",$"2 steps, no text exist in |{c.GetType().FullName}");
                    //        }
                    //        else
                    //        {
                    //            strCaption = txt?.ToString();
                    //            currentInfo.opData = strCaption;
                    //            currentInfo.opText = strCaption;
                    //            simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl", $"2 steps, get text exist in |{c.GetType().FullName}|is |{txt}");
                    //        }
                    //    }
                    //    else // cp is null, then no parent 
                    //    {
                    //        simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl", "parent is null 2 steps");
                    //        if (cp != current2StepObject.currentControl)
                    //        {
                    //            // perhaps users keeping click the object
                    //            // some of the combobox can editable, so should check its parents
                    //        }
                    //        else
                    //        {
                    //            // not the same object, 

                    //        }
                    //    }
                    //    isIgnore = false;
                    //}
                    break;
                case MarsObjectTypeMappings.cnst_swf_Table:
                    try
                    {
                        isOk = MarsUltraGridColumnFinder.GetCellInfoAtPoint(c, pt, ref columnName, ref cellText, ref iRowId, ref strError);
                        if (!isOk)
                        {
                            simpleLog.MarsLoggerSimple.Error("getMarsRecordReplayStepFromControl", strError);
                            return null;
                        }
                        else
                        {
                            // 记录当前的cell的
                            currentInfo.tableExtension_column = columnName;
                            currentInfo.tableExtension_text = cellText;
                        }
                    }
                    catch (Exception e)
                    {
                        strError = $"can't get cell info when click";
                        simpleLog.MarsLoggerSimple.Error("getMarsRecordReplayStepFromControl", strError = $"There is exception when get cell info|{e.Message}", e);
                        isOk = false;
                        return null;
                    }
                    break;
                case MarsObjectTypeMappings.cnst_swf_ToolBar:
                    /// tool bar
                    ///                     
                    isOk = MarsToolBarOperation.GetToolbarButtonsInfo(c, pt, ref strError, ref strCaption, ref strMainPreFix, ref menuPath, ref toolitemType);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("getMarsRecordReplayStepFromControl", $"returns false with Error |{strError}|");
                        return null;
                    }
                    /// 对于菜单而言，存在&获得
                    /// 对于toolbars的button，需要用regular expression处理，以免重复
                    strCaption = Regex.Escape(strCaption);
                    strCaption = $"^{strCaption}$";
                    strOpData = strCaption;
                    currentInfo.opData = strCaption;
                    currentInfo.opText = strCaption;
                    break;
                case MarsObjectTypeMappings.cnst_swf_Menu:
                    /// menu                    ///                     
                    isOk = MarsToolBarOperation.GetMenuInfo(c, pt, ref strError, ref strCaption, ref strMainPreFix, ref menuPath, ref toolitemType);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("getMarsRecordReplayStepFromControl", $"|{MarsObjectTypeMappings.cnst_swf_Menu}|returns false with Error |{strError}|");
                        return null;
                    }
                    /// 对于toolbars的button，需要用regular expression处理，以免重复
                    //strCaption = Regex.Escape(menuPath);
                    //strCaption = $"^{strCaption}$";
                    strOpData = menuPath;
                    currentInfo.opData = menuPath;
                    currentInfo.opText = menuPath;
                    break;
                case MarsObjectTypeMappings.cnst_swf_ListView:
                case MarsObjectTypeMappings.cnst_swf_Tree:
                    /// 
                    bool isClickOnHeader = false, isUsePath = false ;
                    List<string> lstOfColumns = new List<string>();
                    string strText = "", strHitColumnCaption="";
                    isOk = (new MarsMicrosoftListViewHelper()).GetListViewInfo(c, pt, currentInfo.objectMarsType, ref strError, ref strText, lstOfColumns, ref strHitColumnCaption, 
                        ref isClickOnHeader, ref isUsePath);
                    if (!isOk) {
                        simpleLog.MarsLoggerSimple.Error("getMarsRecordReplayStepFromControl", $"{MarsObjectTypeMappings.cnst_swf_ListView}|returns false with Error |{strError}|");
                        return null;
                    }
                    strOpData = strCaption;
                    currentInfo.opData = isUsePath ? strHitColumnCaption : (isClickOnHeader ? strCaption : strText);
                    currentInfo.opText = isUsePath ? strHitColumnCaption : (isClickOnHeader ? strCaption : strText);
                    break;
                default: ///用于其他类别的处理
                    simpleLog.MarsLoggerSimple.Info("getMarsRecordReplayStepFromControl", $"controlType:|{c.GetType().FullName}|text|{c.Text}");
                    break;
            }
            //// swf combox
            //if (string.Compare(currentInfo.objectMarsType, MarsObjectTypeMappings.cnst_swf_Combobox, true) == 0)
            //{
                            
                
            //}else if (string.Compare(currentInfo.objectMarsType, MarsObjectTypeMappings.cnst_swf_Table, true) == 0)
            //{
            //    /// 是table， 需要获得table的column的位置
            //    ///                                
                
                
            //}else if (string.Compare(currentInfo.objectMarsType, MarsObjectTypeMappings.cnst_swf_ToolBar, true) == 0)
            //{
                
            //} else if (string.Compare(currentInfo.objectMarsType, MarsObjectTypeMappings.cnst_swf_Menu, true)==0)
            //{
                
            //}
            
            currentInfo.keyWord = MarsRecordReplayStep.GenKeywordBasedOnObjMarsType(currentInfo.objectMarsType, c,
                isCtrlPressed, current2StepObject,
                iRowId, columnName, cellText,
                ref strOpData, ref isOk, ref strError,
                ref isIgnore,
                mouseEvent,
                toolitemType);
            if (!isOk)
            {
                simpleLog.MarsLoggerSimple.Error($"{iMark}|getMarsRecordReplayStepFromControl", strError);
                return null;
            }
            currentInfo.pegQuickAccess = objOp.getControlPeg(c, ref isOk, ref strError);
            currentInfo.objectQuickAccess = objOp.getControlQuick(c, ref isOk, ref strError);

            simpleLog.MarsLoggerSimple.logEnd($"{iMark}|getMarsRecordReplayStepFromControl");
            isOk = true;
            return currentInfo;
        }
        /// <summary>
        /// 记录当前和前一个对象的句柄。如果句柄不一样，说明前一个对象失去了焦点需要产生step
        /// </summary>
        private static IntPtr currentHandle = IntPtr.Zero;
        private static IntPtr preControlHandle = IntPtr.Zero;
        private static bool IsWaitForSecondClick = false;// 该step是否需要第二步

        //private static TwoStepsEventMonitor twoStepsEventMonitor = new TwoStepsEventMonitor();
        //private static bool IsTwoStepMonitorType(string strType)
        //{
        //    var engineObj = ObjectEngineConfigFileManagement.GetEngineObject();
        //    if (engineObj == null)
        //    {
        //        return false;
        //    }
        //    return ObjectEngineConfigFileManagement.isTypeStringInTestStepTypes(strType);
        //}
        private static void ComboxLostTextChangeEvent(object sender, EventArgs e)
        {
            if (!(sender is System.Windows.Forms.Control)) return;
            /// 当combobx失去焦点时候，
            /// 
            var c = sender as System.Windows.Forms.Control;

            if ((current2StepObject == null)||(current2StepObject.currentControl==null))
            {
                // 说明不是二段对象
                c.LostFocus -= ComboxLostTextChangeEvent;
                return;
            }
            // 判断是不是同一个对象
            if (!current2StepObject.currentControl.Handle.Equals(c.Handle))
            {
                // 不是同一个对象，直接退出
                current2StepObject.init();
                c.LostFocus -= ComboxLostTextChangeEvent;
                return;
            }
            if (current2StepObject.currentStep != 0)
            {
                // 异常初始化，
                current2StepObject.init();
                c.LostFocus -= ComboxLostTextChangeEvent;
                simpleLog.MarsLoggerSimple.Error("ComboxLostTextChangeEvent", $"current2StepObject.currentStep != 0, work flow is wrong|2step object type|{current2StepObject.currentControl.GetType().FullName}|current Type|{c.GetType().FullName}");
                return;
            }

            MarsRecordReplayStep currentInfo = new MarsRecordReplayStep(MarsRecordReplayBase.cnst_packagetype_record);
            MarsObjectOp objOp = new MarsObjectOp();
            currentInfo.bound = MarsRectangle.FromRectangle(c.RectangleToScreen(new Rectangle(System.Drawing.Point.Empty, c.Size)));
            currentInfo.opText = c.Text;
            currentInfo.objectType = c.GetType().FullName;
            currentInfo.objectHandle = c.Handle;
            currentInfo.objectMarsType = objOp.getMarsObject(c);
            currentInfo.keyWord = MarsObjectKeyword.cnst_selectdropdown;
            bool isOk = false;
            string strError = "";
            currentInfo.pegQuickAccess = objOp.getControlPeg(c, ref isOk, ref strError);
            currentInfo.objectQuickAccess = objOp.getControlQuick(c, ref isOk, ref strError);
            simpleLog.MarsLoggerSimple.Info("ComboxLostTextChangeEvent", $"add new |{currentInfo}");
            currentRecordSteps.Add(currentInfo);
            SaveJsonStepToFile();

            current2StepObject.init();
            c.LostFocus -= ComboxLostTextChangeEvent;
        }

        private static void ComboboxGetFocusEvent(object sender, EventArgs e)
        {
            if (!(sender is System.Windows.Forms.Control)) return;
        }


        /// <summary>
        /// There are cases: 单击事件
        /// 1, just one click, like button, label 
        /// 2, two clicks, like drop list
        /// </summary>
        /// <param name="mouseStruct"></param>
        private static void MarsMouseClickImp(MSLLHOOKSTRUCT mouseStruct, MouseMessages mouseButton)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.DEBUG($"{iMark}|MarsMouseClickImp", $"current mouse|{MouseStructureToString(mouseStruct)}|{mouseButton}");
            try
            {
                WaitForSecondsRecordReplayCheck();
                var objEngineConfig = ObjectEngineConfigFileManagement.GetEngineObject();

                /// steps:
                /// 1,get the control position
                /// 2,t 
                /// 
                string strError = "";
                Mars.message.windowsWrapper.SystemUtil.POINT p = new Mars.message.windowsWrapper.SystemUtil.POINT();
                System.Drawing.Point pt = new System.Drawing.Point();
                if (MarsWindowsAPIs.GetCursorPos(ref p))
                {
                    IntPtr targetWndHdl = MarsWindowsAPIs.WindowFromPoint(pt=new System.Drawing.Point(p.X, p.Y));
                    simpleLog.MarsLoggerSimple.Info($"{iMark}|MarsMouseClickImp", $"#{ReflectorForCSharp.GetLineNumber()}|{targetWndHdl}|");
                    // for test 
                    if ((current2StepObject != null)&&(current2StepObject.currentControl!=null))
                    {
                        var tmpc = current2StepObject.currentControl as System.Windows.Forms.Control;
                        var chtmpc = tmpc.GetChildAtPoint(pt = new System.Drawing.Point(p.X, p.Y));
                        if (chtmpc != null)
                        {
                            simpleLog.MarsLoggerSimple.Info($"{iMark}|MarsMouseClickImp", $"chtmpc {chtmpc.GetType()}|");
                        }
                    }
                    var c = Control.FromHandle(targetWndHdl);
                    
                    if (c == null)
                    {
                        ///有可能是系统的dialog，需要判断
                        ///
                        StringBuilder sb = new StringBuilder(256);
                        IntPtr parentWnd = MarsWindowsAPIs.GetAncestor(targetWndHdl, MarsWindowsAPIs.GetAncestorFlags.GetRoot);                        
                        int iLen = MarsWindowsAPIs.GetClassName(parentWnd, sb, 255);
                        if (iLen > 0)
                        {
                            string windowsClass = sb.ToString();
                            if (windowsClass.Equals(MarsWindowsAPIs.cnst_system_dialog_windows_className))
                            {
                                //说明是系统窗口
                                AddDissmissFor32770Dialog("Ok");
                            }
                        }
                        return;
                    }
                    simpleLog.MarsLoggerSimple.Info($"{iMark}|MarsMouseClickImp", $"------------------|{c.GetType().FullName}|");
                    //
                    if (c == null)
                    {
                        c = Control.FromChildHandle(targetWndHdl);
                        if (c == null)
                        {
                            simpleLog.MarsLoggerSimple.Error($"{iMark}|MarsMouseClickImp", $"no such control can be get for handle|{targetWndHdl}|");
                            return;
                        }
                        simpleLog.MarsLoggerSimple.Info($"{iMark}|MarsMouseClickImp",$"from child window||");
                    }
                    bool isOk = true,
                        isIgnored = false;
                    // 判断该类需要在特殊事件中处理
                    isOk = CheckControlAndEvent(c, objEngineConfig, mouseButton, ref strError);
                    if (!isOk)
                    {
                        simpleLog.MarsLoggerSimple.Error("{iMark}|MarsMouseClickImp", $"CheckControlAndEvent Error|{strError}");
                        return;
                    }
                   
                    if (currentHandle.Equals(IntPtr.Zero))
                    {
                        currentHandle = targetWndHdl;
                    }
                    else
                    {
                        ///这里需要添加是否产生旧的handle的
                    }

                    if (!isIgnored)
                    {
                        //mouseStruct.mouseData.
                        var currentInfo = getMarsRecordReplayStepFromControl(c,
                            pt,
                            ref isOk, ref strError, ref isIgnored,
                            mouseButton);
                        if ((!isOk)||(currentInfo==null))
                        {
                            simpleLog.MarsLoggerSimple.Error($"{iMark}|MarsMouseClickImp", strError);
                            return;
                        }
                        
                        MarsRecordAndReplayOpLogManagement.AddOpObjWhenClick(currentInfo);
                    
                        // if the object require more actions then, no such item is inserted directly
                        currentRecordSteps.Add(currentInfo);
                        simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl", $"add new |{currentInfo}");
                        /// save to file
                        /// 
                        SaveJsonStepToFile();
                        // remove 
                        MarsRecordAndReplayOpLogManagement.CleanBuffedSteps();
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Info($"{iMark}|getMarsRecordReplayStepFromControl", $"ignored");
                    }
                    return;
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Error($"{iMark}|MarsMouseClickImp", 
                        $"GetCursorPos return error with error code|{MarsWindowsAPIs.GetLastError()}");
                }
            }
            catch(Exception e)
            {
                simpleLog.MarsLoggerSimple.Error($"{iMark}MarsMouseClickImp", e.Message, e);
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd($"{iMark}MarsMouseClickImp");
            }
        }

        
        private static bool CheckControlAndEvent(Control c, ObjectEngineConfigFile objEngineConfig, MouseMessages mouse, ref string strError)
        {
            /// 算法 获得对象的fulltype
            /// 当事件是mousedown时候，只有类型在mousedown里面的，才返回true
            /// 其他都返回false，这样，就不会处理两次
            string strTypes = ReflectorForCSharp.GetObjectBaseType(c.GetType());
            if (mouse == MouseMessages.WM_LBUTTONUP)
            {
                if (objEngineConfig.typeAndEnvent.mouseDownEvent != null)
                {
                    var typeEvnt = objEngineConfig.typeAndEnvent.mouseDownEvent.FirstOrDefault(p => strTypes.IndexOf($"{p};") >= 0);
                    if (typeEvnt == null) return true; // 使用default方法，mouse down
                    strError = $"mouse envent set to |mouse down| for {strTypes}, but find it in Mouse up";
                    return false;
                }
            }
            if (mouse == MouseMessages.WM_LBUTTONDOWN)
            {
                /// 如果类型在mousedown的就处理
                /// 
                if (objEngineConfig.typeAndEnvent.mouseUpEvent != null)
                {
                    var typeEvnt = objEngineConfig.typeAndEnvent.mouseUpEvent.FirstOrDefault(p => strTypes.IndexOf($"{p};") >= 0);
                    if (typeEvnt == null) return true; // 使用default方法，mouse down
                    strError = $"mouse envent set to |mouse down| for {strTypes}, but find it in Mouse up";                    
                }
                return false;
            }
            return false;
        }
        /// <summary>
        /// 用于enter key的处理。添加一条新的测试信息，presskeys，用current_pos作为参数        /// 
        /// </summary>
        private static void AddPressKeysForEnter()
        {
            MarsRecordReplayStep enterPressStep = new MarsRecordReplayStep();
            enterPressStep.keyWord = MarsObjectKeyword.cnst_pressKeys;
            enterPressStep.Parameter = MarsObjectKeyword.cnst_keyword_para_CURRENT_POS;
            enterPressStep.opText = "{enter}";
            currentRecordSteps.Add(enterPressStep);
            SaveJsonStepToFile();
        }

        private static void AddDissmissFor32770Dialog(string buttonText, int waitSeconds=5)
        {
            MarsRecordReplayStep enterPressStep = new MarsRecordReplayStep();
            enterPressStep.keyWord = MarsObjectKeyword.cnst_dismiss;
            enterPressStep.Parameter = waitSeconds+"";
            enterPressStep.opText = buttonText;
            currentRecordSteps.Add(enterPressStep);
            SaveJsonStepToFile();
        }
        /// <summary>
        /// 判断当前时间和上一次事件时间的间隔。容忍时间为15秒,即如果在15秒内，不产生waitforseoncdes
        /// </summary>
        private static void WaitForSecondsRecordReplayCheck(int toloranceTime=10)
        {
            long n = DateTime.Now.Ticks;

            long t = (n - previouseActivityTimeSpan) / TimeSpan.TicksPerSecond;
            if (t > toloranceTime)
            {
                MarsRecordReplayStep enterPressStep = new MarsRecordReplayStep();
                enterPressStep.keyWord = MarsObjectKeyword.cnst_waitForSeconds;
                enterPressStep.Parameter = "F";
                enterPressStep.opText = (((t - toloranceTime) > 290) ? 300 : t - toloranceTime + 5)+"";
                currentRecordSteps.Add(enterPressStep);
                SaveJsonStepToFile();
            }
            previouseActivityTimeSpan = DateTime.Now.Ticks;
        }
        /// <summary>
        /// 判断是否存在焦点变化。如果存在焦点变化，如果不是table对象，那么就应该创建一个新的step，所有的按键应该在该对象中
        /// enter或tab
        /// </summary>
        /// <param name="focusedHwnd"></param>
        /// <returns></returns>
        private static bool CheckFocusChange(IntPtr focusedHwnd)
        {
            if (currentRecordSteps == null)
            {
                currentRecordSteps = new List<MarsRecordReplayStep>();
                return false;
            }
            var lastStep = currentRecordSteps.LastOrDefault();
            if (lastStep == null)
                return false;
            return lastStep.objectHandle.Equals(focusedHwnd);
        }
        /// <summary>
        /// 创建一个新的对象
        /// </summary>
        /// <param name="lastStep"></param>
        /// <returns></returns>
        private static MarsRecordReplayStep CreateKeyPressStepForLastKeyPressIsCtrl(MarsRecordReplayStep lastStep)
        {
            if (lastStep == null) return null;
            var newStp = lastStep.cloneToANew();
            if (newStp == null) return null;
            newStp.keyWord = MarsObjectKeyword.cnst_pressKeys;
            string tmpOpTx = newStp.ConvertLastKeyPressToOpText(false);
            if (string.IsNullOrEmpty(tmpOpTx)) return null;
            newStp.opText = tmpOpTx;
            newStp.pressedKeys.Clear();
            newStp.Parameter = MarsObjectKeyword.cnst_keyword_para_CURRENT_POS;
            return newStp; 

        }

        /// <summary>
        /// 需要截取onEnter/onCancel事件，因此需要处理keydown
        /// </summary>
        /// <param name="keys"></param>
        private static void MarsRecordKeyDownImp(Mars.message.Hooks.Utilities.VKeys keys)
        {
            simpleLog.MarsLoggerSimple.DEBUG("MarsRecordKeyDownImp", $"current Key|{keys.ToString()}");
            bool isControlPress = false,
                isOk = false,
                isIgnore = false;
            string strError = "";
            MarsRecordReplayStep currentStepInfo = null;
            if (!Mars.message.Inter.MQCenter.HttpRestService.MarsSpyRESTfulServer.IsRecording) return;

            ///判断是否需要添加waitforseconds
            ///
            WaitForSecondsRecordReplayCheck();
            isControlPress = IsCtrlPressed();
            bool isShiftDown = IsShiftDown();

            switch (keys)
            {
                case Mars.message.Hooks.Utilities.VKeys.RETURN:
                case Mars.message.Hooks.Utilities.VKeys.TAB:
                    /// 产生
                    ///                     
                    if (isControlPress)
                    {
                        simpleLog.MarsLoggerSimple.Info("MarsRecordKeyDownImp", "ctrl is pressed|TAB OR RETURN");
                        break;
                    }
                    
                    currentStepInfo = MarsRecordAndReplayOpLogManagement.currentRecordAndReplaySteps;
                    if ((!string.IsNullOrEmpty(currentStepInfo.objectMarsType)) && (string.Compare(currentStepInfo.objectMarsType, "swftable", true) == 0))
                    {
                        /// 对于swftable， 应该依据key buff里面的组合，创建pressKeys
                        /// 
                        isOk = BuildTestStepInfoForTableKeyPress(currentStepInfo, isControlPress, ref isIgnore, ref strError);
                        /// clean
                        /// 
                        MarsRecordAndReplayOpLogManagement.CleanBuffedSteps();
                    }
                    else
                    {
                        isOk = GenerateTestStepString(ref isIgnore, ref strError);
                        if (!isOk)
                        {
                            simpleLog.MarsLoggerSimple.Error("MarsRecordKeyDownImp", $"GenerateTestStepString return false with Error|{strError}");
                        }
                    }
                    if (keys == Mars.message.Hooks.Utilities.VKeys.RETURN)
                    {
                        // 需要添加一条新的test step, keypress, current_pos
                        AddPressKeysForEnter();
                    }
                    break;
                case Mars.message.Hooks.Utilities.VKeys.CANCEL:
                case Mars.message.Hooks.Utilities.VKeys.ESCAPE:
                    /// 这里比较复杂，可能是关闭窗口或者
                    break;
                case Mars.message.Hooks.Utilities.VKeys.F12:
                    break;
                default:
                    /// 获得当前活动对象的类别，通过current step获得
                    /// 将按键信息添加到目标信息中，直到用户点击button，或者按键enter，如在table中索引 
                    /// 
                    if (!IsRecording) return;
                    if ((keys== Mars.message.Hooks.Utilities.VKeys.LCONTROL)
                        ||(keys== Mars.message.Hooks.Utilities.VKeys.RCONTROL)
                        ||(keys== Mars.message.Hooks.Utilities.VKeys.LSHIFT)
                        ||(keys== Mars.message.Hooks.Utilities.VKeys.RSHIFT))
                    {
                        /// only functionalities key is pressed
                        /// 
                        return;
                    }
                    currentStepInfo = MarsRecordAndReplayOpLogManagement.currentRecordAndReplaySteps;
                    if (currentStepInfo.objectHandle.Equals(IntPtr.Zero))
                    {
                        //说明没有点击，或者激活某个对象，
                        //需要获得当前的focus的对象，并且处理基本信息
                        isOk = FixCurrentStepInfoBasedOnFocusControl(currentStepInfo, ref strError);
                        if (!isOk)
                        {
                            simpleLog.MarsLoggerSimple.Error("MarsRecordKeyUpImp", $"FixCurrentStepInfoBasedOnFocusControl returns false with error|{strError}|");
                            return;
                        }
                    }
                    if (isControlPress)
                    {
                        //只要ctrl按住，就产生一条
                        /// 先将最后一条作为的数据处理，将optext是最后的对象的text
                        /// 然后创建一条keypress的test step
                        ///            
                        // /// 先将最后一条作为的数据处理，将optext是最后的对象的text
                        isOk = GenerateTestStepForLastStep(isControlPress, ref strError, true);
                        if (!isOk)
                        {
                            simpleLog.MarsLoggerSimple.Error("MarsRecordKeyUpImp", $"GenerateTestStepForLastStep return false with error|{strError}");
                            return;
                        }
                        //然后创建一条keypress的test step
                        isOk = CreateAPressKeysStepForCtrl(keys, isShiftDown, ref strError);
                        if (!isOk)
                        {
                            simpleLog.MarsLoggerSimple.Error("MarsRecordKeyUpImp", $"CreateAPressKeysStepForCtrl return false with error|{strError}");
                            return;
                        }
                        SaveJsonStepToFile();
                        return;
                    }
                    else
                    {
                        currentStepInfo.pressedKeys.Add(new MarsKeyPressStatus
                        {
                            key = keys,
                            isContorlPress = isControlPress,
                            isShiftPress = isShiftDown,
                        });
                    }
                    return;
            }
            simpleLog.MarsLoggerSimple.logEnd("MarsRecordKeyDownImp", $"return |{isOk}|");
        }

        public static bool IsRecording = true;

        private static void MarsRecordKeyUpImp(Mars.message.Hooks.Utilities.VKeys keys)
        {
            simpleLog.MarsLoggerSimple.DEBUG("MarsRecordKeyUpImp", $"current Key|{keys.ToString()}");
            string strError = "";
            bool isOk = true,
               isIgnore = false, 
                isControlPress=false;
            MarsRecordReplayStep currentStepInfo = null;
            /// 判断是否产生了焦点变化
            /// 
            IntPtr hwndFocused = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetFocus();
            bool isFocusChanged = CheckFocusChange(hwndFocused);

            switch (keys)
            {
                case Mars.message.Hooks.Utilities.VKeys.RETURN:
                case Mars.message.Hooks.Utilities.VKeys.TAB:
                case Mars.message.Hooks.Utilities.VKeys.CANCEL:
                case Mars.message.Hooks.Utilities.VKeys.ESCAPE:
                    //    if (!IsRecording) return;
                    // 
                    //    if (isControlPress=IsCtrlPressed())
                    //    {
                    //        simpleLog.MarsLoggerSimple.Info("MarsRecordKeyUpImp", "ctrl is pressed|TAB OR RETURN");
                    //        break;
                    //    }
                    //    currentStepInfo = MarsRecordAndReplayOpLogManagement.currentRecordAndReplaySteps;
                    //    if (string.IsNullOrEmpty(currentStepInfo.objectMarsType) && (string.Compare(currentStepInfo.objectMarsType, "swftable", true) == 0))
                    //    {
                    //        /// 对于swftable， 应该依据key buff里面的组合，创建searchAndClick
                    //        /// 
                    //        //isOk = BuildTestStepInfoForTabl(currentStepInfo, isControlPress, ref isIgnore, ref strError);
                    //    }
                    //    else
                    //    {
                    //        isOk = GenerateTestStepString(ref isIgnore, ref strError);
                    //        if (!isOk)
                    //        {
                    //            simpleLog.MarsLoggerSimple.Error("MarsRecordKeyUpImp", $"GenerateTestStepString return false with Error|{strError}");
                    //        }
                    //    }
                    //这样，不会重复处理这些键值
                    break;
                case Mars.message.Hooks.Utilities.VKeys.F12://hot key to ends with record and replay                    
                    if (IsCtrlPressed())
                    {
                        /// stop or resume record and replay
                        ///                        
                        StartRecordingHintForm.BeginToRecord(!IsRecording);
                        IsRecording = !IsRecording;                     
                        if (!IsRecording)
                        {
                            //确保没有垃圾step
                            RemovePauseSteps(MarsRecordAndReplayOpLogManagement.opStepLog);
                            RemovePauseSteps(currentRecordSteps);
                            //創建一个新的step，说明已经停止了record，确保最后一个节点是pause
                            MarsRecordAndReplayOpLogManagement.createEndRecordingMark();
                            SaveJsonStepToFile(MarsRecordAndReplayOpLogManagement.opStepLog);
                            //将最后一个record的tmp指令节点删除
                            RemovePauseSteps(currentRecordSteps);
                        }
                    }
                    break;
                //default:
                //    if (isFocusChanged)
                //    {
                //        var lastStep = currentRecordSteps.LastOrDefault();
                //        var lastKeyPress = lastStep.pressedKeys.LastOrDefault();
                //        if (lastKeyPress != null)
                //        {
                //            if (lastKeyPress.isContorlPress)
                //            {
                //                ///创建新的testsetp
                //                ///
                //                MarsRecordReplayStep createKeyPressForLastKeyPressIsCtrl = CreateKeyPressStepForLastKeyPressIsCtrl(lastStep);
                //                if (createKeyPressForLastKeyPressIsCtrl != null)
                //                {
                //                    currentRecordSteps.Add(createKeyPressForLastKeyPressIsCtrl);
                //                }
                //            }
                //        }
                //    }
                //    break;
                    
            }
        }

        private static bool FixCurrentStepInfoBasedOnFocusControl(MarsRecordReplayStep currentStepInfo, ref string strError)
        {
            simpleLog.MarsLoggerSimple.logBegin("FixCurrentStepInfoBasedOnFocusControl");
            if (currentStepInfo==null)
            {
                simpleLog.MarsLoggerSimple.Error("FixCurrentStepInfoBasedOnFocusControl", strError = "step object is null");
                return false;
            }
            IntPtr hwndFocused = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetFocus();
            if (hwndFocused == IntPtr.Zero)
            {
                strError = "GetFocus return Zero";
                simpleLog.MarsLoggerSimple.Error("FixCurrentStepInfoBasedOnFocusControl", strError);
                return false;
            }
            /// only for winform current
            var c = System.Windows.Forms.Control.FromHandle(hwndFocused);
            if (c == null)
            {
                strError = "Control.FromHandle return null";
                simpleLog.MarsLoggerSimple.Error("FixCurrentStepInfoBasedOnFocusControl", strError);
                return false;
            }
            MarsObjectOp objOp = new MarsObjectOp();
            currentStepInfo.objectMarsType = objOp.getMarsObject(c);
            currentStepInfo.objectToOp = c;
            currentStepInfo.bound = MarsRectangle.FromRectangle(c.RectangleToScreen(new Rectangle(System.Drawing.Point.Empty, c.Size))); 
            currentStepInfo.opText = c.Text;
            currentStepInfo.objectType = c.GetType().FullName;
            currentStepInfo.objectFullTypes = ReflectorForCSharp.GetObjectBaseType(c.GetType(), true);
            return true;
        }

        private static void MergeDictionaries(
            Dictionary<MarsSpiedObjInfoAI, List<MarsSpiedObjInfoAI>> a,
            Dictionary<MarsSpiedObjInfoAI, List<MarsSpiedObjInfoAI>> b)
        {
            foreach (var kvp in a)
            {
                if (b.TryGetValue(kvp.Key, out var existingList))
                {
                    // 合并列表，避免重复
                    foreach (var item in kvp.Value)
                    {
                        if (!existingList.Contains(item))
                        {
                            existingList.Add(item);
                        }
                    }
                }
                else
                {
                    // 直接添加新键值对
                    b[kvp.Key] = new List<MarsSpiedObjInfoAI>(kvp.Value);
                }
            }
        }

        /// <summary>
        /// 从MarsObjectSpyCommand的targetWnd获取Control，并构建完整的.NET Framework对象树
        /// 包括向上遍历所有parent到根，以及从根向下的所有子对象
        /// </summary>
        /// <param name="spyCmmd">MarsObjectSpyCommand对象，包含targetWnd</param>
        /// <returns>MarsSpiedObjectInfo对象列表，第一个是根节点</returns>
        private static List<MarsSpiedObjectInfo> BuildMarsObjectsFromSpyCommand(MarsObjectSpyCommand spyCmmd)
        {
            simpleLog.MarsLoggerSimple.logBegin("BuildMarsObjectsFromSpyCommand");
            
            try
            {
                if (spyCmmd == null || string.IsNullOrEmpty(spyCmmd.targetWnd))
                {
                    simpleLog.MarsLoggerSimple.Warnning("BuildMarsObjectsFromSpyCommand", 
                        "spyCmmd is null or targetWnd is empty");
                    return new List<MarsSpiedObjectInfo>();
                }

                // 1. 从targetWnd字符串获取IntPtr
                IntPtr targetHwnd = IntPtr.Zero;
                try
                {
                    if (long.TryParse(spyCmmd.targetWnd, out long handleValue))
                    {
                        targetHwnd = new IntPtr(handleValue);
                    }
                    else
                    {
                        simpleLog.MarsLoggerSimple.Warnning("BuildMarsObjectsFromSpyCommand", 
                            $"Cannot parse targetWnd to IntPtr: {spyCmmd.targetWnd}");
                        return new List<MarsSpiedObjectInfo>();
                    }
                }
                catch (Exception ex)
                {
                    simpleLog.MarsLoggerSimple.Warnning("BuildMarsObjectsFromSpyCommand", 
                        $"Error parsing targetWnd to IntPtr: {spyCmmd.targetWnd}, Error: {ex.Message}");
                    return new List<MarsSpiedObjectInfo>();
                }

                if (targetHwnd == IntPtr.Zero)
                {
                    simpleLog.MarsLoggerSimple.Warnning("BuildMarsObjectsFromSpyCommand", 
                        $"targetHwnd is Zero after parsing: {spyCmmd.targetWnd}");
                    return new List<MarsSpiedObjectInfo>();
                }

                // 2. 从Handle获取Control
                var control = System.Windows.Forms.Control.FromHandle(targetHwnd);
                if (control == null)
                {
                    simpleLog.MarsLoggerSimple.Warnning("BuildMarsObjectsFromSpyCommand", 
                        $"Cannot get Control from handle: {targetHwnd}");
                    return new List<MarsSpiedObjectInfo>();
                }

                simpleLog.MarsLoggerSimple.Info("BuildMarsObjectsFromSpyCommand", 
                    $"Found control: {control.GetType().FullName}[{control.Name}] at handle: {targetHwnd}");

                // 3. 使用WinFormControlHelper构建完整的对象树
                // 该方法会自动：
                // - 向上遍历所有parent到根（parent为null）
                // - 从根向下构建所有子对象
                // - 返回包含所有对象的MarsSpiedObjectInfo列表
                var marsObjects = WinFormControlHelper.BuildMarsObjectsFromControl(control, targetHwnd);

                if (marsObjects != null && marsObjects.Count > 0)
                {
                    simpleLog.MarsLoggerSimple.Info("BuildMarsObjectsFromSpyCommand", 
                        $"Built {marsObjects.Count} MarsSpiedObjectInfo objects, root: {marsObjects[0].objectName}[{marsObjects[0].objectType}]");
                }
                else
                {
                    simpleLog.MarsLoggerSimple.Warnning("BuildMarsObjectsFromSpyCommand", 
                        "BuildMarsObjectsFromControl returned empty list");
                }

                return marsObjects ?? new List<MarsSpiedObjectInfo>();
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("BuildMarsObjectsFromSpyCommand", 
                    $"Error building MarsSpiedObjectInfo from spyCmmd: {ex.Message}", ex);
                return new List<MarsSpiedObjectInfo>();
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("BuildMarsObjectsFromSpyCommand");
            }
        }

        /// <summary>
        /// 检查WinForm控件是否嵌入了WPF元素
        /// </summary>
        /// <param name="control">WinForm控件</param>
        /// <returns>是否嵌入了WPF</returns>
        //private static bool HasEmbeddedWpf(System.Windows.Forms.Control control)
        //{
        //    if (control == null) return false;

        //    try
        //    {
        //        // 方法1：检查是否是ElementHost控件
        //        var elementHost = control as ElementHost;
        //        if (elementHost != null && elementHost.Child != null)
        //        {
        //            simpleLog.MarsLoggerSimple.Info("HasEmbeddedWpf", 
        //                "Found ElementHost with WPF child");
        //            return true;
        //        }

        //        // 方法2：尝试通过HwndSource获取WPF元素
        //        var hwndSource = Wpf.Interop.HwndSource.FromHwnd(control.Handle);
        //        if (hwndSource != null && hwndSource.RootVisual != null)
        //        {
        //            simpleLog.MarsLoggerSimple.Info("HasEmbeddedWpf", 
        //                "Found HwndSource with WPF RootVisual");
        //            return true;
        //        }

        //        //// 方法3：检查子控件中是否有ElementHost
        //        //if (control.Controls != null && control.Controls.Count > 0)
        //        //{
        //        //    foreach (System.Windows.Forms.Control child in control.Controls)
        //        //    {
        //        //        if (HasEmbeddedWpf(child))
        //        //        {
        //        //            return true;
        //        //        }
        //        //    }
        //        //}

        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        simpleLog.MarsLoggerSimple.Warnning("HasEmbeddedWpf", 
        //            $"Error checking for embedded WPF: {ex.Message}");
        //        return false;
        //    }
        //}


        /// <summary>
        /// 显示objectspy的界面
        /// 如果是Wpf，则需要通过wpf的模式处理
        /// </summary>
        public static void StartInternalSpyRestSvc(string injectType="Normal")
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin($"{iMark}|StartInternalSpyRestSvc|begin|Type|{injectType}");
            gCurrentMode = 0;
            try
            {
                Mars.message.windowsWrapper.SystemUtil.POINT p=default, px = new Mars.message.windowsWrapper.SystemUtil.POINT();
                IntPtr targetWndHdl = IntPtr.Zero;
                if (MarsWindowsAPIs.GetCursorPos(ref px)) {
                    targetWndHdl = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(px.X, px.Y));
                }
                    
                /// start object gui here
                /// 
                if (string.Compare("Normal", injectType, true) == 0)
                {
                    MarsWinformSpy marsSpy = new MarsWinformSpy();
                    MarsObjectSpyCommand spyCmmd = new MarsObjectSpyCommand();
                    /// get cursor window handle from cursor
                    /// 
                    
                    if (MarsWindowsAPIs.GetCursorPos(ref p))
                    {
                        
                        simpleLog.MarsLoggerSimple.Info("StartInternalSpyRestSvc", $"{iMark}|#{ReflectorForCSharp.GetLineNumber()}|{targetWndHdl}|");
                        spyCmmd.targetWnd = targetWndHdl.ToString();
                    }
                    marsSpy.LoadObjectsToSpyForm(spyCmmd);
                }
                else if (string.Compare("Wpf", injectType,true)==0)
                {
                    simpleLog.MarsLoggerSimple.Info("StartInternalSpyRestSvc", $"injectType|{injectType}|{targetWndHdl}");
                    
                    // 获取targetWndHdl的class名称
                    StringBuilder sb = new StringBuilder(256);
                    string className = "";
                    if (targetWndHdl != IntPtr.Zero && MarsWindowsAPIs.GetClassName(targetWndHdl, sb, 256) > 0)
                    {
                        className = sb.ToString();
                        simpleLog.MarsLoggerSimple.Info("StartInternalSpyRestSvc", 
                            $"targetWndHdl className: {className}");
                    }
                    
                    // 如果className以"WindowsForms10"开头，说明是WinForm控件
                    if (!string.IsNullOrEmpty(className) && className.StartsWith("WindowsForms10", StringComparison.OrdinalIgnoreCase))
                    {
                        simpleLog.MarsLoggerSimple.Info("StartInternalSpyRestSvc", 
                            $"Detected WinForm control, className: {className}");

                        // 创建MarsObjectSpyCommand
                        MarsObjectSpyCommand spyCmmd = new MarsObjectSpyCommand();
                        spyCmmd.spyType = MarsObjectSpyCommand.cnst_commandName_wpfhyberidFramework;
                        spyCmmd.targetWnd = $"{targetWndHdl}";

                        // 从spyCmmd.targetWnd获取Control，并构建完整的.NET Framework对象树
                        // 包括向上遍历所有parent到根，以及从根向下的所有子对象
                        var marsObjects = BuildMarsObjectsFromSpyCommand(spyCmmd);

                        if (marsObjects != null && marsObjects.Count > 0)
                        {
                            // 加载到SpyForm
                            objectSpy.MarsObjSpyForm spyFrm = objectSpy.MarsObjSpyForm.getInstance(null, 
                                Mars.Inter.MQCenter.spyHelper.enSpyMode.spyMode_net_winform_wpf);
                            if (spyFrm != null)
                            {
                                spyFrm.SetAllObjects(marsObjects);
                                //spyFrm.reloadObjects(marsObjects);
                                simpleLog.MarsLoggerSimple.Info("StartInternalSpyRestSvc", 
                                    $"Loaded {marsObjects.Count} .NET Framework objects to SpyForm from targetWnd: {spyCmmd.targetWnd}");
                            }

                            if (spyFrm != null && spyFrm.Modal) return;
                            objectSpy.MarsObjSpyForm.showModuleInThread();
                        }
                        else
                        {
                            simpleLog.MarsLoggerSimple.Warnning("StartInternalSpyRestSvc", 
                                $"Failed to build MarsSpiedObjectInfo objects from targetWnd: {spyCmmd.targetWnd}");
                        }

                        return;
                    }
                    
                    // 示例：从点p获取WPF对象
                    if (MarsWindowsAPIs.GetCursorPos(ref p))
                    {
                        var wpfElement = WpfElementFromPointHelper.GetWpfElementFromPoint(p);
                        if (wpfElement != null)
                        {
                            simpleLog.MarsLoggerSimple.Info("StartInternalSpyRestSvc", 
                                $"Found WPF element at point ({p.X}, {p.Y}): {wpfElement.Name}[{wpfElement.Type}]");
                        }
                    }
                    
                    //MarsWinformSpy marsSpy = new MarsWinformSpy();
                    objectSpy.MarsObjSpyForm spyFrm2 = objectSpy.MarsObjSpyForm.getInstance(null, Mars.Inter.MQCenter.spyHelper.enSpyMode.spyMode_net_winform_wpf);
                    MarsObjSpyFormWpfIntegration.LoadWpfVisualTreeToSpyForm(spyFrm2);
                    if (spyFrm2.Modal) return;
                    objectSpy.MarsObjSpyForm.showModuleInThread();
                    
                }
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("StartInternalSpyRestSvc", $"{iMark}|{e.Message}", e);
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd($"{iMark}|StartInternalSpyRestSvc|end");
            }
        }
    }
}
