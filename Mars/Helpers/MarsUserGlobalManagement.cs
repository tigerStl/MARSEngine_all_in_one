using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Helpers
{
    [DataContract]
    public class MarsDBConfigInfo
    {
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public string host { get; set; }
        [DataMember]
        public string serviceName { get; set; }
        [DataMember]
        public string connString { get; set; }
        [DataMember]
        public string Schema { get; set; }
        [DataMember]
        public string userName { get; set; }
        [DataMember]
        public string pwd { get; set; }
        [DataMember]
        public string marsEnvName { get; set; }

        
    }
    [DataContract]
    public class MarsMemoryUserInfo
    {
        [DataMember]
        public string userName;
        [DataMember]
        public string pwd;
        [DataMember]
        public string latestHostIP;
        [DataMember]
        public int port;
        [DataMember]
        public TimeSpan latestActiveTime;
        [DataMember]
        public long userIdInDB;
    }
    

    public class MarsDBUserGlobalManagement
    {
        public static Dictionary<string, Dictionary<MarsMemoryUserInfo, List<T_TEST_PROJECTDTO>>> globalMarsUserInfo = new Dictionary<string, Dictionary<MarsMemoryUserInfo, List<T_TEST_PROJECTDTO>>>();
        public static Dictionary<string, MarsDBConfigInfo> MarsDBConfigMapping = new Dictionary<string, MarsDBConfigInfo>();
        public static void InitFromConfigFile(string strFileName)
        {
            /// strFileName is with the path to Mars.config in IIS
            /// Each db connection should only call less two Queries 
            /// steps:
            /// 1, check strFileName is validate or not
            /// 2, open the file and load all env nodes to MarsDBConfigMapping, key is name from <MarsEnvironment name="GEN_MARS_20">
            /// 3, build  MarsDBConfigMapping based #2
            /// 4, use loop to get all T_Test_userInfo and its test projects from DB
            /// 5, build globalMarsUserInfo
            /// 

        }

        public MarsEntities GetMarsEntity(string strDBshortName)
        {
            /// reuse your code and create entify framework connection here
            return null;
        }

        public List<T_TEST_PROJECTDTO> UserLogin(string dbShortNameIdx, string strUserName, string userEncodedPwd)
        {
            /// supposed, the pwd is encoded
            /// steps:
            /// 1, find item from globalMarsUserInfo dbShortNameIdx 
            /// 2, if not find, then return null
            /// 3, check username and pwd 
            /// 4, return projectInfo if find. 
            return null;

        }
    }
}
