using Mars.AutoTestingDriver.SystemUtil;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.webSocketService
{
    public sealed class MARSWebSocketConst
    {
        public const string cnst_packet_type_request = "TYP_REQUEST";
        public const string cnst_packet_type_response = "TYP_RESPONSE";

        public const string cnst_cmd_request_runSteps = "CMD_RUN_STEPS";
        public const string cnst_cmd_request_getHostName = "CMD_GET_HOSTNAME";
        public const string cnst_cmd_response_getHostName = "CMD_RESPONSE_HOSTNAME";
        public const string cnst_cmd_response_error = "CMD_RESPONSE_ERROR";

        //public const string cnst_datatype_request_command = "REQUEST_COMMAND_JSON";
    }

    [Serializable]
    public class MarsWebSocketNetPacketBase
    {
        public string packetType { get; set; } = MARSWebSocketConst.cnst_packet_type_request;
        public string command { get; set; }
        public string uuid { get; set; }
       
    }
    [Serializable]
    public class MarsWebSocketResponsePacket: MarsWebSocketNetPacketBase
    {
        
        public string message { get; set; }
    }
    [Serializable]
    public class MarsWebSocketCmdPacket: MarsWebSocketNetPacketBase
    {
        
    }

    public class MarsWebSocketCmdGetHostNameRequest : MarsWebSocketCmdPacket
    {
        public MarsWebSocketCmdGetHostNameRequest() : base()
        {
            command = MARSWebSocketConst.cnst_cmd_request_getHostName;
            
        }
    }

    [Serializable]
    public class MarsExeTestStepsRequestPacket: MarsWebSocketResponsePacket
    { 
        public string dbId { get;set; }
        public MarsExeTestStepsRequestPacket() : base()
        {
            command = MARSWebSocketConst.cnst_cmd_request_runSteps;
        }
        public web_Test_steps_response TestStepInfo { get; set; }
    }

    //public class Web_TestSteps_request
    //{
    //    public int runTmpOrd { get; set; }
    //    public string keyword { get; set; }
    //    public string objectHappyName { get; set; }
    //    public string objectQuickAccess { get; set; }
    //    public string parameters { get; set; }
    //    public string data { get; set; }
    //}
    [Serializable]
    public class web_Test_steps_response : MarsClipboardURLPara
    {
        public string resultInShort { get; set; } // success or failed
        public string extMessage { get; set; }
    }
}
