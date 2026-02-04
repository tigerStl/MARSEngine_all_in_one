using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MarsTestFrame.CommuniteServer.DataStructureStepService
{
    [DataContract]
    public class MarsServiceFrameworkTestStepReply
    {
        [DataMember]
        public string keyword;
        [DataMember]
        public string parameters;
        [DataMember]
        public string data;
        [DataMember]
        public long testStepId;
    }

    [DataContract]
    public class MarsServiceFramewordTestStepReq
    {
        [DataMember]
        public long TestStepId;
    }

    [DataContract]
    public class MarsServiceWCFCmdReply
    {
        [DataMember]
        public string command;

        /// <summary>
        /// 系统流水号，用来标注请求的唯一性，由服务器端产生
        /// </summary>
        [DataMember]
        public long SystemNO;

        /// <summary>
        /// 未来可能存在其他数据类型
        /// </summary>
        [DataMember]
        public MarsServiceFrameworkTestStepReply TestSteps;
    }

    /// <summary>
    /// 命令请求的数据结构
    /// </summary>
    [DataContract]
    public class MarsServiceWCFCmdReq
    {
        [DataMember]
        public string commandReq;
        /// <summary>
        /// 数据请求的附加信息
        /// </summary>
        [DataMember]
        public string RequestAddon;

        //[DataMember]
        //public MarsServiceFrameworkTestStepReq TestStepRequest;
    }


}
