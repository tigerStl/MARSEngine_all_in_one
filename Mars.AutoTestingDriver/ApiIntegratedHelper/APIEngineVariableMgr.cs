using System;
using System.Collections.Generic;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp;
using Mars.message.Utility;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.AutoTestingDriver.ApiIntegratedHelper
{
    /// <summary>
    /// API Engine Variable Manager class for storing and retrieving API variables
    /// </summary>
    public class APIEngineVariableMgr
    {
        private static Dictionary<string, string> _variableDictionary = new Dictionary<string, string>();
        private static readonly object _lockObject = new object();
        private static MLogger Logger = MLogger.GetLogger(typeof(APIEngineVariableMgr));
        private static Random random = new Random();
        
        /// <summary>
        /// Generates a random trace ID (8 digits)
        /// </summary>
        /// <returns>8-digit random string</returns>
        private static string GenerateTraceId()
        {
            return random.Next(10000000, 99999999).ToString();
        }

        /// <summary>
        /// Sets a variable value. If the variable exists, it will be overwritten.
        /// </summary>
        /// <param name="variableName">The variable name</param>
        /// <param name="value">The value to set. Can be "fromMem:VARIABLE_NAME" or a direct string value</param>
        /// <param name="strError">Error message output</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool SetVariable(string variableName, string value, ref string strError)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("SetVariable", string.Format("{0}|VariableName:[{1}], Value:[{2}]", traceId, variableName, value));

            try
            {
                if (string.IsNullOrEmpty(variableName))
                {
                    strError = "Variable name is empty";
                    Logger.Error("SetVariable", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("SetVariable");
                    return false;
                }

                string actualValue = value;

                // Check if value is in "fromMem:VARIABLE_NAME" format
                if (!string.IsNullOrEmpty(value) && value.StartsWith("fromMem:", StringComparison.OrdinalIgnoreCase))
                {
                    string memoryVariableName = value.Substring(8); // Remove "fromMem:" prefix
                    if (string.IsNullOrEmpty(memoryVariableName))
                    {
                        strError = "Memory variable name is empty after 'fromMem:' prefix";
                        Logger.Error("SetVariable", string.Format("{0}|{1}", traceId, strError));
                        Logger.logEnd("SetVariable");
                        return false;
                    }

                    // Get value from memory
                    string memValue = "";
                    string memError = "";
                    bool memResult = CaptureParaMgr.GetVariableByIdx(memoryVariableName, ref memValue, ref memError);
                    
                    if (!memResult)
                    {
                        strError = $"Failed to get memory variable '{memoryVariableName}': {memError}";
                        Logger.Error("SetVariable", string.Format("{0}|{1}", traceId, strError));
                        Logger.logEnd("SetVariable");
                        return false;
                    }

                    actualValue = memValue;
                    Logger.Info("SetVariable", string.Format("{0}|Retrieved value from memory variable '{1}': [{2}]", 
                        traceId, memoryVariableName, actualValue));
                }

                // Set or update the variable
                lock (_lockObject)
                {
                    if (_variableDictionary.ContainsKey(variableName))
                    {
                        _variableDictionary[variableName] = actualValue;
                        Logger.Info("SetVariable", string.Format("{0}|Updated existing variable '{1}' with value: [{2}]", 
                            traceId, variableName, actualValue));
                    }
                    else
                    {
                        _variableDictionary.Add(variableName, actualValue);
                        Logger.Info("SetVariable", string.Format("{0}|Created new variable '{1}' with value: [{2}]", 
                            traceId, variableName, actualValue));
                    }
                }

                Logger.logEnd("SetVariable", string.Format("{0}|Variable '{1}' set successfully", traceId, variableName));
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in SetVariable: {ex.Message}";
                Logger.Error("SetVariable", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("SetVariable");
                return false;
            }
        }

        /// <summary>
        /// Gets a variable value
        /// </summary>
        /// <param name="variableName">The variable name</param>
        /// <param name="value">The variable value output</param>
        /// <param name="strError">Error message output</param>
        /// <returns>True if variable exists, false otherwise</returns>
        public static bool GetVariable(string variableName, out string value, ref string strError)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetVariable", string.Format("{0}|VariableName:[{1}]", traceId, variableName));

            value = null;

            try
            {
                if (string.IsNullOrEmpty(variableName))
                {
                    strError = "Variable name is empty";
                    Logger.Error("GetVariable", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("GetVariable");
                    return false;
                }

                lock (_lockObject)
                {
                    if (_variableDictionary.ContainsKey(variableName))
                    {
                        value = _variableDictionary[variableName];
                        Logger.Info("GetVariable", string.Format("{0}|Variable '{1}' found with value: [{2}]", 
                            traceId, variableName, value));
                        Logger.logEnd("GetVariable");
                        return true;
                    }
                    else
                    {
                        strError = $"Variable '{variableName}' not found";
                        Logger.Error("GetVariable", string.Format("{0}|{1}", traceId, strError));
                        Logger.logEnd("GetVariable");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                strError = $"Exception in GetVariable: {ex.Message}";
                Logger.Error("GetVariable", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetVariable");
                return false;
            }
        }

        /// <summary>
        /// Removes a variable
        /// </summary>
        /// <param name="variableName">The variable name</param>
        /// <returns>True if variable was removed, false if it didn't exist</returns>
        public static bool RemoveVariable(string variableName)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("RemoveVariable", string.Format("{0}|VariableName:[{1}]", traceId, variableName));

            try
            {
                lock (_lockObject)
                {
                    bool removed = _variableDictionary.Remove(variableName);
                    if (removed)
                    {
                        Logger.Info("RemoveVariable", string.Format("{0}|Variable '{1}' removed successfully", traceId, variableName));
                    }
                    else
                    {
                        Logger.Info("RemoveVariable", string.Format("{0}|Variable '{1}' not found, nothing to remove", traceId, variableName));
                    }
                    Logger.logEnd("RemoveVariable");
                    return removed;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RemoveVariable", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("RemoveVariable");
                return false;
            }
        }

        /// <summary>
        /// Clears all variables
        /// </summary>
        public static void ClearAll()
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("ClearAll", traceId);

            try
            {
                lock (_lockObject)
                {
                    int count = _variableDictionary.Count;
                    _variableDictionary.Clear();
                    Logger.Info("ClearAll", string.Format("{0}|Cleared {1} variables", traceId, count));
                }

                Logger.logEnd("ClearAll");
            }
            catch (Exception ex)
            {
                Logger.Error("ClearAll", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("ClearAll");
            }
        }

        /// <summary>
        /// Gets the count of variables
        /// </summary>
        /// <returns>The number of variables</returns>
        public static int GetCount()
        {
            lock (_lockObject)
            {
                return _variableDictionary.Count;
            }
        }
    }
}

