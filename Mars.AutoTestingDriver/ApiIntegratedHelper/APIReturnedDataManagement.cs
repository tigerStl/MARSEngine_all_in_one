using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Mars.message.Utility;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.AutoTestingDriver.ApiIntegratedHelper
{
    /// <summary>
    /// API Returned Data Management class for storing and retrieving API response data
    /// </summary>
    public class APIReturnedDataManagement
    {
        private static Dictionary<string, JObject> _returnedDataDictionary = new Dictionary<string, JObject>();
        private static readonly object _lockObject = new object();
        private static MLogger Logger = MLogger.GetLogger(typeof(APIReturnedDataManagement));
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
        /// Stores the returned data for a test step
        /// </summary>
        /// <param name="stepId">The test step ID</param>
        /// <param name="jsonData">The JSON data to store</param>
        public static void StoreReturnedData(long stepId, string jsonData)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("StoreReturnedData", string.Format("{0}|StepId:[{1}], JSON data length:[{2}]", traceId, stepId, 
                string.IsNullOrEmpty(jsonData) ? 0 : jsonData.Length));

            try
            {
                if (string.IsNullOrEmpty(jsonData))
                {
                    Logger.logEnd("StoreReturnedData", string.Format("{0}|JSON data is empty, skipping", traceId));
                    return;
                }

                string key = "#" + stepId;
                JObject jsonObject = JObject.Parse(jsonData);
                
                lock (_lockObject)
                {
                    _returnedDataDictionary[key] = jsonObject;
                }
                
                Logger.logEnd("StoreReturnedData", string.Format("{0}|Data stored successfully for key:[{1}]", traceId, key));
            }
            catch (Exception ex)
            {
                // If JSON parsing fails, try to store as string in a wrapper object
                Logger.Info("StoreReturnedData", string.Format("{0}|JSON parsing failed, trying to store as raw data: {1}", traceId, ex.Message));
                try
                {
                    string key = "#" + stepId;
                    JObject wrapper = new JObject
                    {
                        ["rawData"] = jsonData
                    };
                    
                    lock (_lockObject)
                    {
                        _returnedDataDictionary[key] = wrapper;
                    }
                    
                    Logger.logEnd("StoreReturnedData", string.Format("{0}|Data stored as raw data for key:[{1}]", traceId, key));
                }
                catch (Exception ex2)
                {
                    // Ignore if still fails
                    Logger.Error("StoreReturnedData", string.Format("{0}|Exception storing raw data: {1}", traceId, ex2.Message), ex2.StackTrace);
                    Logger.logEnd("StoreReturnedData");
                }
            }
        }

        /// <summary>
        /// Gets the returned data for a test step
        /// </summary>
        /// <param name="stepId">The test step ID</param>
        /// <returns>The JObject containing the returned data, or null if not found</returns>
        public static JObject GetReturnedData(long stepId)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetReturnedData", string.Format("{0}|StepId:[{1}]", traceId, stepId));

            try
            {
                string key = "#" + stepId;
                
                lock (_lockObject)
                {
                    if (_returnedDataDictionary.ContainsKey(key))
                    {
                        Logger.logEnd("GetReturnedData", string.Format("{0}|Data found for key:[{1}]", traceId, key));
                        return _returnedDataDictionary[key];
                    }
                }
                
                Logger.logEnd("GetReturnedData", string.Format("{0}|Data not found for key:[{1}]", traceId, key));
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("GetReturnedData", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetReturnedData");
                return null;
            }
        }

        /// <summary>
        /// Gets the returned data for the nearest step ID that is less than or equal to the current step ID
        /// </summary>
        /// <param name="currentStepId">The current test step ID</param>
        /// <returns>The JObject containing the returned data, or null if not found</returns>
        public static JObject GetNearestReturnedData(long currentStepId)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetNearestReturnedData", string.Format("{0}|CurrentStepId:[{1}]", traceId, currentStepId));

            try
            {
                long nearestStepId = -1;
                JObject nearestData = null;

                lock (_lockObject)
                {
                    foreach (var kvp in _returnedDataDictionary)
                    {
                        // Extract step ID from key (format: "#{stepId}")
                        if (kvp.Key.StartsWith("#"))
                        {
                            string stepIdStr = kvp.Key.Substring(1);
                            if (long.TryParse(stepIdStr, out long stepId))
                            {
                                // Find the nearest step ID that is <= currentStepId
                                if (stepId <= currentStepId && stepId > nearestStepId)
                                {
                                    nearestStepId = stepId;
                                    nearestData = kvp.Value;
                                }
                            }
                        }
                    }
                }

                if (nearestData != null)
                {
                    Logger.logEnd("GetNearestReturnedData", string.Format("{0}|Found nearest data for stepId:[{1}]", traceId, nearestStepId));
                }
                else
                {
                    Logger.logEnd("GetNearestReturnedData", string.Format("{0}|No data found for stepId <= [{1}]", traceId, currentStepId));
                }

                return nearestData;
            }
            catch (Exception ex)
            {
                Logger.Error("GetNearestReturnedData", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetNearestReturnedData");
                return null;
            }
        }

        /// <summary>
        /// Gets the returned data as a JSON string for a test step
        /// </summary>
        /// <param name="stepId">The test step ID</param>
        /// <returns>The JSON string, or null if not found</returns>
        public static string GetReturnedDataAsString(long stepId)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetReturnedDataAsString", string.Format("{0}|StepId:[{1}]", traceId, stepId));

            try
            {
                JObject data = GetReturnedData(stepId);
                string result = data?.ToString();
                Logger.logEnd("GetReturnedDataAsString", string.Format("{0}|Result length:[{1}]", traceId, 
                    string.IsNullOrEmpty(result) ? 0 : result.Length));
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("GetReturnedDataAsString", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetReturnedDataAsString");
                return null;
            }
        }

        /// <summary>
        /// Removes the returned data for a test step
        /// </summary>
        /// <param name="stepId">The test step ID</param>
        public static void RemoveReturnedData(long stepId)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("RemoveReturnedData", string.Format("{0}|StepId:[{1}]", traceId, stepId));

            try
            {
                string key = "#" + stepId;
                
                lock (_lockObject)
                {
                    if (_returnedDataDictionary.ContainsKey(key))
                    {
                        _returnedDataDictionary.Remove(key);
                        Logger.logEnd("RemoveReturnedData", string.Format("{0}|Data removed for key:[{1}]", traceId, key));
                    }
                    else
                    {
                        Logger.logEnd("RemoveReturnedData", string.Format("{0}|Data not found for key:[{1}], nothing to remove", traceId, key));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RemoveReturnedData", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("RemoveReturnedData");
            }
        }

        /// <summary>
        /// Clears all stored returned data
        /// </summary>
        public static void ClearAll()
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("ClearAll", string.Format("{0}|Clearing all stored data", traceId));

            try
            {
                int countBefore = 0;
                lock (_lockObject)
                {
                    countBefore = _returnedDataDictionary.Count;
                    _returnedDataDictionary.Clear();
                }
                
                Logger.logEnd("ClearAll", string.Format("{0}|Cleared [{1}] items", traceId, countBefore));
            }
            catch (Exception ex)
            {
                Logger.Error("ClearAll", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("ClearAll");
            }
        }

        /// <summary>
        /// Gets the count of stored returned data items
        /// </summary>
        /// <returns>The count of stored items</returns>
        public static int GetCount()
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetCount", string.Format("{0}|Getting count of stored items", traceId));

            try
            {
                int count = 0;
                lock (_lockObject)
                {
                    count = _returnedDataDictionary.Count;
                }
                
                Logger.logEnd("GetCount", string.Format("{0}|Count:[{1}]", traceId, count));
                return count;
            }
            catch (Exception ex)
            {
                Logger.Error("GetCount", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetCount");
                return 0;
            }
        }
    }
}

