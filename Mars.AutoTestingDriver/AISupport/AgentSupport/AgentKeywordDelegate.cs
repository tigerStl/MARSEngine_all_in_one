using AngleSharp.Html;
using Mars.AutoTestingDriver.ErrorMessage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.DevTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

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
                var agentName = AgentHelper.CNST_AGENT_DEFAULT_NAME;
                var cfg = Mars.AutoTestingDriver.Utils.ExternalAgentManager.GetAgentConfig(agentName);
                if (cfg == null)
                {
                    strError = $"Agent configuration for '{agentName}' not found.";
                    dealResult.ResultMessage = "FAILED";
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.ErrorMessage = strError;
                    return false;
                }

                if (!Mars.AutoTestingDriver.Utils.ExternalAgentManager.IsAgentRunning(cfg))
                {
                    if (!Mars.AutoTestingDriver.Utils.ExternalAgentManager.StartAgent(cfg, out string startErr))
                    {
                        strError = $"Failed to start agent '{agentName}': {startErr}";
                        dealResult.ResultMessage = "FAILED";
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                        dealResult.ErrorMessage = strError;
                        return false;
                    }
                }

                string strDirectory = AgentHelper.GetImageSaveDirectory();
                if (string.IsNullOrEmpty(strDirectory))
                {
                    strError = "Failed to get image save directory.";
                    dealResult.ResultMessage = "FAILED";
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.ErrorMessage = strError;
                    return false;
                }

                string strPId = AgentMethodDataStorage.GetMethodData(AgentHelper.CNST_AGENT_DEFAULT_NAME, AgentHelper.CNST_AGENT_METHOD_ACTIVEPROCESS);
                if (string.IsNullOrEmpty(strPId))
                {
                    strError = "Agent process ID not found in storage.Make sure the process is active. And ActiveProcess is called first.";
                    dealResult.ResultMessage = "FAILED";
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.ErrorMessage = strError;
                    return false;
                }
                int pId;
                if (!int.TryParse(strPId, out pId))
                {
                    strError = "Agent process ID is not a valid integer.";
                    dealResult.ResultMessage = "FAILED";
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                    dealResult.ErrorMessage = strError;
                    return false;
                }

                
                // Build payload for snapshot. Include peg/object properties and metadata
                var payload = new
                {
                    action = "Snapshot",
                    ProcessId = pId,
                    ScreenshotDirectory = strDirectory,
                    Timeout = new TimeSpan(0,0,10)
                };

                string payloadJson = JsonConvert.SerializeObject(payload);

                if (cfg.CanInvokeHttp)
                {
                    var task = Mars.AutoTestingDriver.Utils.ExternalAgentManager.InvokeAgentAsync(cfg, "snapshot", payloadJson);
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
                        var resultHub = ExternalAppResultHub.InstanceFrom(j);
                        if (resultHub==null)
                        {
                            strError = "Failed to parse agent snapshot response.";
                            dealResult.ResultMessage = "FAILED";
                            dealResult.ErrorMessage = strError;
                            return false;
                        } 
                        dealResult.ResultMessage = resultHub.Success ? "SUCCESS" : "FAILED";                        
                        dealResult.ErrorMessage = resultHub.Success? "SUCCESS" : resultHub.ErrorMessage;
                        dealResult.ReturnedData = resultHub.Success ?resultHub.ImagePath: $"FAILED,{resultHub.ErrorDetail}";
                        
                        // store returned data for later retrieval
                        try
                        {
                            AgentMethodDataStorage.SetMethodData(cfg.AgentName ?? agentName, "Snapshot", dealResult.ReturnedData);
                        }
                        catch { }

                        return resultHub.Success;
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
