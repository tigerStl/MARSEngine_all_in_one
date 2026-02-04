using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
#if wpf_mars_core
using MarsEngine.MarsKeywords;
#endif
namespace Mars.message.Inter.MQCenter.interProcess
{   
    public enum MARSENGINE_OBJECT_IDMODE
    {
        _none =0, 
        _addName = 1<<1, 
        _addPath = 1<<2, 
        _addType = 1<<3,
        _addIndex= 1<<4,
    }
    [DataContract]
    public class MarsKVPair
    {
        public string k { get; set; }
        public string v { get; set; }  
        public string getNoneEmptyKOrV()
        {
            return string.IsNullOrEmpty(v) ? k : v;
        }
    }

    [DataContract]
    public partial class MarsSpiedObjectBasicInfo : MarsSpiedObjInfoAI
    {
        public const string cnst_control_source_type_uia= "UIA";
        public const string cnst_control_source_type_msaa = "MSAA"; 
        public const string cnst_control_source_type_net = ".net";  

        [DataMember]
        public string controlClassTypeFromAPI { get; set; } = ".net"; // API获取的类型, UIA, MSAA, .net(default)
        [DataMember]
        public string obj_uuid { get; set; }
        // 位置和大小属性现在通过 objectRect 统一管理
        [DataMember]
        public int x 
        { 
            get => objectRect.X; 
            set 
            {
                if (objectRect == System.Drawing.Rectangle.Empty)
                    objectRect = new System.Drawing.Rectangle(value, 0, 0, 0);
                else
                    objectRect = new System.Drawing.Rectangle(value, objectRect.Y, objectRect.Width, objectRect.Height);
            }
        }
        [DataMember]
        public int y 
        { 
            get => objectRect.Y; 
            set 
            {
                if (objectRect == System.Drawing.Rectangle.Empty)
                    objectRect = new System.Drawing.Rectangle(0, value, 0, 0);
                else
                    objectRect = new System.Drawing.Rectangle(objectRect.X, value, objectRect.Width, objectRect.Height);
            }
        }
        [DataMember]
        public int relatedX { get; set; }
        [DataMember]
        public int w 
        { 
            get => objectRect.Width; 
            set 
            {
                if (objectRect == System.Drawing.Rectangle.Empty)
                    objectRect = new System.Drawing.Rectangle(0, 0, value, 0);
                else
                    objectRect = new System.Drawing.Rectangle(objectRect.X, objectRect.Y, value, objectRect.Height);
            }
        }
        [DataMember]
        public int h 
        { 
            get => objectRect.Height; 
            set 
            {
                if (objectRect == System.Drawing.Rectangle.Empty)
                    objectRect = new System.Drawing.Rectangle(0, 0, 0, value);
                else
                    objectRect = new System.Drawing.Rectangle(objectRect.X, objectRect.Y, objectRect.Width, value);
            }
        }
        [DataMember]
        public int relatedY { get; set; }

        [DataMember]
        public string snapshotFileNameWithPath { get; set; }//文件名称
        //[JsonInclude]
        [DataMember]
        public bool isVisible { get; set; }
        [DataMember]
        public string controlMarsType { get; set; }

        //[JsonInclude]
        [DataMember(IsRequired = false)]
        public int index = -1;
        //[JsonInclude]
        [DataMember(IsRequired = false)]
        public int zorder = -1;
        //[JsonInclude]

        [DataMember(IsRequired = false)]
        public List<MarsSpyGeneratedQuickAccess> generatedQuickAccessItems = new List<MarsSpyGeneratedQuickAccess>();

        [DataMember]
        public string PegName { get; set; }
        [DataMember]
        //public MarsSpiedObjectInfo Pegwindow { get; set; }
        public MarsSpiedObjectBasicInfo Pegwindow { get; set; }

        [DataMember]
        public string PegWindUUID { get; set; }

        public MarsSpiedObjectBasicInfo()
        {
            // 初始化 objectRect 为默认值
            objectRect = new System.Drawing.Rectangle(0, 0, 0, 0);
        }
        [DataMember]
        public long hwnd { get; set; }
        [DataMember]
        public string windowClassName { get; set; }

        [DataMember(IsRequired = false)]
        public long parentHwnd { get; set; } = 0;

        [DataMember(IsRequired = false)]
        public int controlId { get; set; } = -1;

        
        public string getObjectInfo(MARSENGINE_OBJECT_IDMODE idMode= MARSENGINE_OBJECT_IDMODE._none, 
            int idx=0)
        {
            string strObjId = "";
            
            if (!string.IsNullOrEmpty(this.objectName))
            {
                strObjId = $"swfName:={this.objectName}";
            }
            if ((string.IsNullOrEmpty(strObjId)) // 如果对象的名称为kong
                ||((idMode & MARSENGINE_OBJECT_IDMODE._addPath)== MARSENGINE_OBJECT_IDMODE._addPath)
                )
            {
                if (string.IsNullOrEmpty(strObjId))
                {
                    strObjId = $"swfname Path:={ this.objectNamePath}";
                }
                else
                {
                    strObjId = $"{strObjId}\r\nswfName Path:={this.objectNamePath}";
                }
            }
            if ((string.IsNullOrEmpty(strObjId)) // 如果对象的名称为kong
                || ((idMode & MARSENGINE_OBJECT_IDMODE._addType) == MARSENGINE_OBJECT_IDMODE._addType)
                )
            {
                if (string.IsNullOrEmpty(strObjId))
                {
                    strObjId = $"swftype:={this.objectType}";
                }
                else
                {
                    strObjId = $"{strObjId}\r\nswftype:={this.objectType}";
                }
            }
            if ((!string.IsNullOrEmpty(strObjId)) // 必须有其他内容
                && ((idMode & MARSENGINE_OBJECT_IDMODE._addIndex) == MARSENGINE_OBJECT_IDMODE._addIndex)
                )
            {
                strObjId = $"{strObjId}\r\nindex:={idx}";
            }
            return strObjId ;
        }
    }

    public class RESTfulSvcActionManagement
    {
        public const string cnst_command_req_queryObjects = "queryCurrentObjectsReq";
        public const string cnst_action_req_queryObjects = "queryCurrentObjects";
        public const string cnst_command_resp_queryObjects = "queryCurrentObjectsResp";

        public const string cnst_command_req_highlight_obj = "highlightObjectReq";
        public const string cnst_action_req_highlight_obj = "highlightObject";
        public const string cnst_command_resp_highlight_obj = "highlightObjectResp";

        public const string cnst_command_req_execute_step = "executeTestStepReq";
        public const string cnst_action_execute_step = "executeTestStep";
        public const string cnst_command_resp_execute_step = "executeTestStepResp";

        public const string cnst_command_req_register = "registerSvcReq";
        public const string cnst_action_reg_register = "registerSvc";
        public const string cnst_command_resp_register = "registerSvcResp";

        public const string cnst_command_req_queryObjectDetails = "queryObjectDetailReq";
        public const string cnst_action_queryObjectDetails = "queryObjectDetail";
        public const string cnst_command_resp_queryObjectDetails = "queryObjectDetailResp";

        public const string cnst_command_req_replayStep     = "replayTestStepReq";
        public const string cnst_action_replayStep          = "replayTestStep";
        public const string cnst_command_resp_replayStep    = "replayTestStepResp";

        public const string cnst_command_req_queryRecordAndReplyStatus  = "queryRecordAndReplayStatusReq";
        public const string cnst_action_queryRecordAndReplayStatus      = "queryRecordAndReplayStatus";
        public const string cnst_command_resp_queryRecordAndReplayStatus = "queryRecordAndReplayStatusResp";

        public const string cnst_command_req_removeTeststepByRunord = "removeTestStepByRunordReq";
        public const string cnst_action_removeTestStepByRunord = "removeTestStepByRunord";
        public const string cnst_command_resp_removeTeststepByRunord = "removeTestStepByRunordResp";

        public const string cnst_rsp_type_error = "_error";

        public const string cnst_api_catalog_replay = "replay";
    }

    public class RESTfulReqOrRspPacket
    {
        
        public string uuid { get; set; }
        public string version { get; set; }
        public string timeStamp { get; set; } // yyyyMMddhh24mmssfff
        public string command { get; set; }
        public string msg {
            get;
            set;
        }

        public RESTfulReqOrRspPacket()
        {
            uuid = Guid.NewGuid().ToString();
            version = "1.0";
            timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            msg = "SUCCESS";
        }

        public virtual bool IsRightPackage(RESTfulReqOrRspPacket sourcePackage)
        {
            if (sourcePackage == null) return false;    
            return this.command.Equals(sourcePackage.command, StringComparison.OrdinalIgnoreCase);
        }
        public virtual bool IsRightCommand() {
            return false;
        }

        public virtual bool isExpectedResult()
        {
            return msg.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
        }
    }


    public class QueryObjectRequst: RESTfulReqOrRspPacket
    {
        public QueryObjectRequst() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_req_queryObjects;
            currentHandle = 0 ; // get 
            x = 0;
            y = 0;
            typeOfGenerateSteps = 1;
        }

        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_req_queryObjects);
        }
        public int typeOfGenerateSteps { get; set; } //0- get the current window handles and its children, 1, get topest window

        public long currentHandle { get; set; }
        public int x { get; set; }
        public int y { get; set; }
    }

    public class HighlightObjectRequest: RESTfulReqOrRspPacket
    {
        public HighlightObjectRequest() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_req_highlight_obj;
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_req_highlight_obj);
        }

        public MarsSpiedObjectBasicInfo currentObject { get; set; }
    }


    public class HighlightObjectResponse: RESTfulReqOrRspPacket
    {
        public HighlightObjectResponse() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_highlight_obj;
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_highlight_obj);
        }
    }


    public class RegisterSvcRequst : RESTfulReqOrRspPacket
    {
        public const string cnst_shakehand_code = "VGlnZXJJc1RoZUZhdGhlck9mTUFSUw==";
        public RegisterSvcRequst() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_req_register;
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_req_register)
                && (cnst_shakehand_code.Equals(msg ?? ""));
        }
        public string ip;
    }


    public class RegisterSvcResponse : RESTfulReqOrRspPacket
    {
        
        public RegisterSvcResponse() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_register;
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_register);
        }
        
    }

    public class EngineErrorResponsePackage: RESTfulReqOrRspPacket
    {
        public EngineErrorResponsePackage() : base()
        {
            command = RESTfulSvcActionManagement.cnst_rsp_type_error;
        }
        public override bool IsRightCommand() {
            return command.Equals(RESTfulSvcActionManagement.cnst_rsp_type_error);
        }
    }

    public class EngineAllObjectsResponse: RESTfulReqOrRspPacket
    {
        public EngineAllObjectsResponse():base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_queryObjects;
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_queryObjects);
        }
        public List<MarsSpiedObjectBasicInfo> AllObjects { get; set; }
        public int objectCount { get; set; }
    }

    public class EngineFlashControlResponse : RESTfulReqOrRspPacket
    {
        public EngineFlashControlResponse() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_highlight_obj;
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_highlight_obj);
        }
    }

    public class EngineExecuteTestStepRequest : RESTfulReqOrRspPacket
    {
        public EngineExecuteTestStepRequest() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_req_execute_step;
            startTime = DateTime.Now;
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_req_execute_step);
        }

        public MarsSpiedObjectBasicInfo currentObject { get; set; }
        public string Keyword { get; set; }
        public string pegName { get; set; }
        public string pegInfo { get; set; }
        public string ObjName { get; set; }
        public string ObjInfo { get; set; } 
        public string Parameter { get; set; }
        public string OpData { get; set; }    
        public bool IsSkip { get; set; }
        public DateTime startTime { get; set; }
        public string marsObjType { get; set; }
    }

    public class EngineExecuteTestStepResponse: RESTfulReqOrRspPacket
    {
        public EngineExecuteTestStepResponse() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_execute_step;
            
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_execute_step);
        }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public string returnValues { get; set; }
        public bool executeStepOk { get; set; }
        public string generatedFilePath { get; set; }
    }

    public enum EngineQueryObjCommand
    {
        _none,
        _getAllColumns,
        _getAllComboItems,
        _getObjTypePath, 
        _getAllProperties, 
        _getSpecial_property_value, 
        _setProperty_value
    }

    /// <summary>
    /// 查询对象的明细信息
    /// </summary>
    public class EngineGetObjectExtensionDetailReq: RESTfulReqOrRspPacket
    {
        /// <summary>
        /// target object's detail
        /// </summary>
        public long objectHwnd { get; set; }

        public EngineQueryObjCommand objectExtCmd { get; set; }
        /// <summary>
        /// 是否需要保留状态
        /// </summary>
        public bool isKeepStatus { set; get; }
        public EngineGetObjectExtensionDetailReq() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_req_queryObjectDetails;
        }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_req_queryObjectDetails);
        }
    }

    public class EngineGetObjectExtensionDetailRspn : RESTfulReqOrRspPacket
    {
        public long objectHwnd { get; set; }

        public EngineGetObjectExtensionDetailRspn() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_queryObjectDetails;
            extensionData = new List<MarsKVPair>();
        }

        public List<MarsKVPair> extensionData { get; set; }
        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_queryObjectDetails);
        }
    }

    public class EngineReplayTestStepInJsonRspn : RESTfulReqOrRspPacket
    {
        public string returnedValues { get; set; }

        public EngineReplayTestStepInJsonRspn() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_replayStep;            
        }

        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_replayStep);
        }
    }

    public class EngineQueryRecordReplayStatusRequest : RESTfulReqOrRspPacket
    {
        public EngineQueryRecordReplayStatusRequest() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_req_queryRecordAndReplyStatus;
        }

        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_req_queryRecordAndReplyStatus);
        }
    }

    public class EngineQueryRecordReplayStatusRspns: RESTfulReqOrRspPacket
    {
        public bool IsRunning { get; set; }

        public EngineQueryRecordReplayStatusRspns() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_queryRecordAndReplayStatus;
        }

        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_queryRecordAndReplayStatus);
        }
    }

    public class RemoveTestStepByRunOrdForRecordAndReplayReq: RESTfulReqOrRspPacket
    {
        public int runOrd { get; set; }
        public RemoveTestStepByRunOrdForRecordAndReplayReq() : base()
        {
            command = RESTfulSvcActionManagement.cnst_action_removeTestStepByRunord;
        }

        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_action_removeTestStepByRunord);
        }
    }

    public class RemoveTestStepByRunOrdForRecordAndReplayResp : RESTfulReqOrRspPacket
    {
        public int runOrd { get; set; }
        public RemoveTestStepByRunOrdForRecordAndReplayResp() : base()
        {
            command = RESTfulSvcActionManagement.cnst_command_resp_removeTeststepByRunord;
        }

        public override bool IsRightCommand()
        {
            return command.Equals(RESTfulSvcActionManagement.cnst_command_resp_removeTeststepByRunord);
        }
    }
}
