using System;
#if _NET4

using System.Threading.Tasks;
#endif

namespace Mars.message.AutoTestingDriver.message
{
#if _NET4
    [System.Runtime.Serialization.DataContract]
#endif
    public class MarsMessageConst
    {
#if _NET4
        [System.Runtime.Serialization.DataMember]
#endif
        public const string MESSAGE_SERVICE_QUEUE_NAME = @".\Private$\MARS_MAINSVR"; //测试框架往这个队列写应答和需求 客户端监听这个 队列
        public const string MESSAGE_CLIENT_QUEUE_NAME = @".\Private$\MARS_MAINCLNT"; //客户端程序向这个队列写结果 服务器端监听这个 队列

        public static string UniqueMQSvrName()
        {
            try
            {
                
                string strUserName = FixMQName(System.Environment.UserName);
                
                return $"{MESSAGE_SERVICE_QUEUE_NAME}_{strUserName}";
            }
            catch (Exception e)
            {
                return MESSAGE_SERVICE_QUEUE_NAME;
            }
            
        }

        private static string FixMQName(string strNameToBeFixed)
        {
            string strUserName = strNameToBeFixed.Replace(".", "-");
            strUserName = strUserName.Replace("/", "-");
            strUserName = strUserName.Replace("\\", "-");
            strUserName = strUserName.Replace("*", "-");
            return strUserName;
        }

        public static string UniqueMQClnName()
        {
            string strUserName = FixMQName(System.Environment.UserName);

            return $"{MESSAGE_CLIENT_QUEUE_NAME}_{strUserName}";
        }
    }


}
