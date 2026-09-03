using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.AISupport.AgentSupport
{
    public class ExternalAppResultHub
    {
        public bool Success { get; set; }
        public int? ProcessId { get; set; }
        public string ImagePath { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetail { get; set; }

        public static ExternalAppResultHub InstanceFrom(JObject jResp)
        {
            try
            {
                if (jResp == null) return null;
                return jResp.ToObject<ExternalAppResultHub>();
            }
            catch
            {
                return null;
            }
        }
    }

    public sealed class AgentHelper
    {
        public const string CNST_AGENT_DEFAULT_NAME = "MarsDefaultAgent";
        public const string CNST_AGENT_PARAMETER_DEFAULTAGENT = "DefaultAgent";
        public const string CNST_AGENT_METHOD_ACTIVEPROCESS = "ActiveProcess";
        public static bool IsAgentParameterIndicatesAgentMode(string strParam)
        {
            if (string.IsNullOrEmpty(strParam)) return false;
            return strParam.IndexOf(CNST_AGENT_PARAMETER_DEFAULTAGENT, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string GetImageSaveDirectory()
        {
            string strDirectory = Assembly.GetExecutingAssembly().Location;
            strDirectory = System.IO.Path.GetDirectoryName(strDirectory);
            strDirectory = System.IO.Path.Combine(strDirectory, "tmpimg/agent");
            if (!System.IO.Directory.Exists(strDirectory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(strDirectory);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return strDirectory;
        }
    }
}
