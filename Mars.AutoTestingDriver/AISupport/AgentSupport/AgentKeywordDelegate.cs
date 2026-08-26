using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Mars.AutoTestingDriver.AISupport.AgentSupport
{
    internal static class AgentKeywordDelegate
    {
        /// <summary>
        /// Invoke default agent's snapshot method.
        /// </summary>
        public static bool Snapshot(long runOrdId,
            Dictionary<string, string> pegProps,
            Dictionary<string, string> objProps,
            string strParaMeter,
            string strData,
            string typeName,
            string strAttachInfo,
            string pegName,
            string objectHappyName,
            ref string strError,
            ref Mars.message.AutoTestingDriver.interProcess.MARSDealResult dealResult)
        {
            try
            {
                var agentName = Mars.AutoTestingDriver.Utils.ExternalAgentManager.DEFAULT_AGENT_NAME;
                var cfg = Mars.AutoTestingDriver.Utils.ExternalAgentManager.GetAgentConfig(agentName);
                if (cfg == null)
                {
                    strError = $"Agent configuration for '{agentName}' not found.";
                    return false;
                }

                if (!Mars.AutoTestingDriver.Utils.ExternalAgentManager.IsAgentRunning(cfg))
                {
                    if (!Mars.AutoTestingDriver.Utils.ExternalAgentManager.StartAgent(cfg, out string startErr))
                    {
                        strError = $"Failed to start agent '{agentName}': {startErr}";
                        return false;
                    }
                }

                // Build payload for snapshot. Include peg/object properties and metadata
                var payload = new
                {
                    action = "Snapshot",
                    runOrdId = runOrdId,
                    pegProperties = pegProps,
                    objectProperties = objProps,
                    parameter = strParaMeter,
                    data = strData,
                    type = typeName,
                    attachInfo = strAttachInfo,
                    pegName = pegName,
                    objectHappyName = objectHappyName
                };

                string payloadJson = JsonConvert.SerializeObject(payload);

                if (!string.IsNullOrEmpty(cfg.InvokeUrl) && cfg.UseHttp)
                {
                    var task = Mars.AutoTestingDriver.Utils.ExternalAgentManager.InvokeAgentAsync(cfg, "Snapshot", payloadJson);
                    task.Wait();
                    var resp = task.Result;
                    if (string.IsNullOrEmpty(resp))
                    {
                        strError = "Agent snapshot returned empty response.";
                        dealResult.ResultMessage = "FAILED";
                        dealResult.ErrorMessage = strError;
                        return false;
                    }

                    try
                    {
                        var j = Newtonsoft.Json.Linq.JObject.Parse(resp);
                        var status = (string)j["status"] ?? "FAILED";
                        dealResult.ResultMessage = status == "OK" ? "SUCCESS" : "FAILED";
                        dealResult.ErrorMessage = (string)j["message"] ?? string.Empty;
                        dealResult.ReturnedData = j["ExternalData"]?.ToString();

                        // store returned data for later retrieval
                        try
                        {
                            AgentMethodDataStorage.SetMethodData(cfg.AgentName ?? agentName, "Snapshot", dealResult.ReturnedData);
                        }
                        catch { }

                        return status == "OK";
                    }
                    catch
                    {
                        // non-json response, consider success if non-empty
                        dealResult.ResultMessage = "SUCCESS";
                        dealResult.ErrorMessage = resp;
                        try { AgentMethodDataStorage.SetMethodData(cfg.AgentName ?? agentName, "Snapshot", resp); } catch { }
                        return true;
                    }
                }

                strError = "Agent invoke URL not configured.";
                dealResult.ResultMessage = "FAILED";
                dealResult.ErrorMessage = strError;
                return false;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                try { dealResult.ResultMessage = "FAILED"; dealResult.ErrorMessage = ex.Message; } catch { }
                return false;
            }
        }
    }
}
