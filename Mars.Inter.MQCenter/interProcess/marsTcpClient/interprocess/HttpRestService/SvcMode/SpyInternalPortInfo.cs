using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Mars.message.Inter.MQCenter.interProcess.HttpRestService.SvcMode
{
    public class MarsRestFulCnst
    {
        public const string cnst_SwapDir = "restful";
        public const string cnst_port_swapfile = "portswap.json";
        public const string cnst_objects_swapfile = "objects.json";
        public const string cnst_mars_restful_base_url = "http://localhost";
        
    }


    /// <summary>
    ///  convert to json
    ///  only microsoft's package is allowed
    /// </summary>
    /// 
    [DataContract]
    public class SpyInternalPortInfo
    {
        [DataMember]
        public string version { get; set; }
        [DataMember]
        public int port { get; set; }
        [DataMember]
        public string accessGUID { get; set; }
        [DataMember]
        public string ip { get; set; }
    }
}
