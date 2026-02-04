using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Utility;
using static Mars.AutoTestingDriver.ApiIntegratedHelper.APIEngineHelper;
using Route2NSEx.src.Marquis.systemUtil;
using Mars.AutoTestingDriver.ErrorMessage;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp;
using System.Data.Linq;

namespace Mars.AutoTestingDriver.ApiIntegratedHelper
{
    /// <summary>
    /// API Engine Invoke Implementation class for validating and parsing API configuration JSON
    /// </summary>
    public class APIEngineInvokeImpl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(APIEngineInvokeImpl));
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
        /// Validates and parses the given string as JSON
        /// </summary>
        /// <param name="jsonString">The JSON string to validate</param>
        /// <returns>JObject if valid JSON, null otherwise</returns>
        public static JObject IsValidateJSON(string jsonString)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("IsValidateJSON", string.Format("{0}|Validating JSON string, Length:[{1}]", traceId, 
                string.IsNullOrEmpty(jsonString) ? 0 : jsonString.Length));

            try
            {
                if (string.IsNullOrEmpty(jsonString))
                {
                    Logger.logEnd("IsValidateJSON");
                    return null;
                }

                JToken token = JToken.Parse(jsonString);
                if (token is JObject obj)
                {
                    Logger.logEnd("IsValidateJSON");
                    return obj;
                }
                // If it's not a JObject, wrap it or return null
                Logger.logEnd("IsValidateJSON");
                return null;
            }
            catch (JsonReaderException ex)
            {
                Logger.Error("IsValidateJSON", string.Format("{0}|JsonReaderException: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("IsValidateJSON");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("IsValidateJSON", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("IsValidateJSON");
                return null;
            }
        }

        /// <summary>
        /// Gets a value from JSON using JSONPath (case-insensitive)
        /// </summary>
        /// <param name="jsonObject">The JObject</param>
        /// <param name="jsonPath">The JSONPath expression</param>
        /// <returns>The JToken value or null if not found</returns>
        public static JToken GetValueByJsonPath(JObject jsonObject, string jsonPath)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetValueByJsonPath", string.Format("{0}|JsonPath:[{1}]", traceId, jsonPath));

            try
            {
                if (jsonObject == null)
                {
                    Logger.logEnd("GetValueByJsonPath");
                    return null;
                }

                // First try exact match
                JToken token = jsonObject.SelectToken(jsonPath);
                if (token != null)
                {
                    Logger.logEnd("GetValueByJsonPath");
                    return token;
                }
                
                // If not found and path is simple property access (e.g., "$.APIEndPoint"), try case-insensitive
                if (jsonPath.StartsWith("$.") && !jsonPath.Contains("[") && !jsonPath.Contains("*"))
                {
                    string propertyName = jsonPath.Substring(2);
                    // Try to find property with case-insensitive match
                    foreach (var prop in jsonObject.Properties())
                    {
                        if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            Logger.logEnd("GetValueByJsonPath");
                            return prop.Value;
                        }
                    }
                }
                
                Logger.logEnd("GetValueByJsonPath");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("GetValueByJsonPath", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetValueByJsonPath");
                return null;
            }
        }

        /// <summary>
        /// Gets a value from JSON using JSONPath (case-insensitive) - overload for string
        /// </summary>
        /// <param name="jsonString">The JSON string</param>
        /// <param name="jsonPath">The JSONPath expression</param>
        /// <returns>The JToken value or null if not found</returns>
        public static JToken GetValueByJsonPath(string jsonString, string jsonPath)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetValueByJsonPath", string.Format("{0}|JsonPath:[{1}], StringLength:[{2}]", traceId, jsonPath, 
                string.IsNullOrEmpty(jsonString) ? 0 : jsonString.Length));

            try
            {
                JObject jsonObject = JObject.Parse(jsonString);
                JToken result = GetValueByJsonPath(jsonObject, jsonPath);
                Logger.logEnd("GetValueByJsonPath");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("GetValueByJsonPath", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetValueByJsonPath");
                return null;
            }
        }

        /// <summary>
        /// Gets multiple values from JSON using JSONPath (SelectTokens)
        /// </summary>
        /// <param name="jsonObject">The JObject</param>
        /// <param name="jsonPath">The JSONPath expression</param>
        /// <returns>An IEnumerable&lt;JToken&gt; of all matched tokens (empty if none)</returns>
        public static IEnumerable<JToken> GetValuesByJsonPath(JObject jsonObject, string jsonPath)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetValuesByJsonPath", string.Format("{0}|JsonPath:[{1}]", traceId, jsonPath));

            try
            {
                if (jsonObject == null)
                {
                    Logger.logEnd("GetValuesByJsonPath");
                    return Enumerable.Empty<JToken>();
                }

                if (!string.IsNullOrEmpty(jsonPath))
                {
                    jsonPath = jsonPath.Trim();
                }

                var tokens = jsonObject.SelectTokens(jsonPath);
                var resultList = tokens?.ToList() ?? new List<JToken>();

                Logger.logEnd("GetValuesByJsonPath");
                return resultList;
            }
            catch (Exception ex)
            {
                Logger.Error("GetValuesByJsonPath", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetValuesByJsonPath");
                return Enumerable.Empty<JToken>();
            }
        }

        /// <summary>
        /// Gets multiple values from JSON string using JSONPath (SelectTokens)
        /// </summary>
        /// <param name="jsonString">The JSON string</param>
        /// <param name="jsonPath">The JSONPath expression</param>
        /// <returns>An IEnumerable&lt;JToken&gt; of all matched tokens (empty if none)</returns>
        public static IEnumerable<JToken> GetValuesByJsonPath(string jsonString, string jsonPath)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetValuesByJsonPath", string.Format("{0}|JsonPath:[{1}], StringLength:[{2}]", traceId, jsonPath,
                string.IsNullOrEmpty(jsonString) ? 0 : jsonString.Length));

            try
            {
                if (string.IsNullOrEmpty(jsonString))
                {
                    Logger.logEnd("GetValuesByJsonPath");
                    return Enumerable.Empty<JToken>();
                }

                JObject jsonObject = JObject.Parse(jsonString);
                var result = GetValuesByJsonPath(jsonObject, jsonPath) ?? Enumerable.Empty<JToken>();
                Logger.logEnd("GetValuesByJsonPath");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("GetValuesByJsonPath", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetValuesByJsonPath");
                return Enumerable.Empty<JToken>();
            }
        }

        /// <summary>
        /// Gets a string value from JSON using JSONPath
        /// </summary>
        /// <param name="jsonObject">The JObject</param>
        /// <param name="jsonPath">The JSONPath expression</param>
        /// <returns>The string value or null if not found</returns>
        public static string GetStringValueByJsonPath(JObject jsonObject, string jsonPath)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetStringValueByJsonPath", string.Format("{0}|JsonPath:[{1}]", traceId, jsonPath));

            try
            {
                JToken token = GetValueByJsonPath(jsonObject, jsonPath);
                string result = token?.ToString();
                Logger.logEnd("GetStringValueByJsonPath");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("GetStringValueByJsonPath", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetStringValueByJsonPath");
                return null;
            }
        }

        /// <summary>
        /// Gets a string value from JSON using JSONPath - overload for string
        /// </summary>
        /// <param name="jsonString">The JSON string</param>
        /// <param name="jsonPath">The JSONPath expression</param>
        /// <returns>The string value or null if not found</returns>
        public static string GetStringValueByJsonPath(string jsonString, string jsonPath)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetStringValueByJsonPath", string.Format("{0}|JsonPath:[{1}], StringLength:[{2}]", traceId, jsonPath, 
                string.IsNullOrEmpty(jsonString) ? 0 : jsonString.Length));

            try
            {
                JToken token = GetValueByJsonPath(jsonString, jsonPath);
                string result = token?.ToString();
                Logger.logEnd("GetStringValueByJsonPath");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("GetStringValueByJsonPath", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetStringValueByJsonPath");
                return null;
            }
        }

        /// <summary>
        /// Applies format converter to a numeric string value
        /// </summary>
        /// <param name="value">The original string value to convert</param>
        /// <param name="formatConverter">The format converter name (Float2, Factor100, etc.)</param>
        /// <param name="strError">Output: Error message if conversion fails</param>
        /// <returns>Formatted string value, or null if conversion fails</returns>
        private static string ApplyFormatConverter(string value, string formatConverter, ref string strError)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("ApplyFormatConverter", string.Format("{0}|Value:[{1}], Converter:[{2}]", traceId, value, formatConverter));

            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    Logger.logEnd("ApplyFormatConverter");
                    return value;
                }

                // Try to parse as double
                double doubleValue;
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out doubleValue))
                {
                    strError = $"Cannot convert value '{value}' to double for format converter '{formatConverter}'";
                    Logger.Error("ApplyFormatConverter", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ApplyFormatConverter");
                    return null;
                }

                double convertedValue = doubleValue;
                int decimalPlaces = 2;
                bool useFullPrecision = false;

                // Apply converter-specific logic
                switch (formatConverter.ToUpperInvariant())
                {
                    case "FLOAT2":
                        // Convert to double, keep 2 decimal places, format with thousand separators
                        convertedValue = doubleValue;
                        decimalPlaces = 2;
                        break;

                    case "FACTOR100":
                        // Convert to double, multiply by 100, format with thousand separators
                        convertedValue = doubleValue * 100.0;
                        decimalPlaces = 2;
                        break;

                    case "FACTOR100FULL":
                        // Convert to double, multiply by 100, format with thousand separators, keep all decimal places
                        convertedValue = doubleValue * 100.0;
                        useFullPrecision = true;
                        break;

                    default:
                        strError = $"Unknown format converter '{formatConverter}'. Supported converters: Float2, Factor100, Factor100Full";
                        Logger.Error("ApplyFormatConverter", string.Format("{0}|{1}", traceId, strError));
                        Logger.logEnd("ApplyFormatConverter");
                        return null;
                }

                // Format with thousand separators and specified decimal places
                string formattedValue;
                if (useFullPrecision)
                {
                    // Use "N15" format (15 decimal places is enough to preserve all double precision)
                    // then remove trailing zeros to keep only significant digits
                    formattedValue = convertedValue.ToString("N15", System.Globalization.CultureInfo.InvariantCulture);
                    
                    // Remove trailing zeros after decimal point
                    if (formattedValue.Contains("."))
                    {
                        formattedValue = formattedValue.TrimEnd('0');
                        if (formattedValue.EndsWith("."))
                        {
                            formattedValue = formattedValue.TrimEnd('.');
                        }
                    }
                }
                else
                {
                    formattedValue = convertedValue.ToString($"N{decimalPlaces}", System.Globalization.CultureInfo.InvariantCulture);
                }
                
                Logger.Info("ApplyFormatConverter", string.Format("{0}|Converted '{1}' to '{2}' using '{3}'", 
                    traceId, value, formattedValue, formatConverter));
                Logger.logEnd("ApplyFormatConverter");
                return formattedValue;
            }
            catch (Exception ex)
            {
                strError = $"Exception during format conversion: {ex.Message}";
                Logger.Error("ApplyFormatConverter", string.Format("{0}|{1}", traceId, strError), ex.StackTrace);
                Logger.logEnd("ApplyFormatConverter");
                return null;
            }
        }

        /// <summary>
        /// Gets values from JSON array with conditional filtering
        /// Supports format: "arrayPath|filterProperty==filterValue|targetProperty"
        /// Example: "$.TradeDetails.Assets|PorS==S|dmAssetId" - gets dmAssetId from Assets where PorS equals "S"
        /// </summary>
        /// <param name="jsonObject">The JObject</param>
        /// <param name="conditionalPath">Conditional JSONPath expression with filter</param>
        /// <returns>List of matching values, or null if error</returns>
        public static List<JToken> GetValuesByConditionalJsonPath(JObject jsonObject, string conditionalPath)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetValuesByConditionalJsonPath", string.Format("{0}|ConditionalPath:[{1}]", traceId, conditionalPath));

            try
            {
                if (jsonObject == null || string.IsNullOrEmpty(conditionalPath))
                {
                    Logger.logEnd("GetValuesByConditionalJsonPath");
                    return null;
                }

                // Parse conditional path: "arrayPath|filterProperty==filterValue|targetProperty"
                string[] parts = conditionalPath.Split('|');
                if (parts.Length != 3)
                {
                    Logger.Error("GetValuesByConditionalJsonPath", string.Format("{0}|Invalid conditional path format. Expected: 'arrayPath|filterProperty==filterValue|targetProperty', got: '{1}'", traceId, conditionalPath));
                    Logger.logEnd("GetValuesByConditionalJsonPath");
                    return null;
                }

                string arrayPath = parts[0].Trim();
                string filterCondition = parts[1].Trim();
                string targetProperty = parts[2].Trim();

                // Parse filter condition: "filterProperty==filterValue"
                string[] filterParts = filterCondition.Split(new[] { "==" }, StringSplitOptions.None);
                if (filterParts.Length != 2)
                {
                    Logger.Error("GetValuesByConditionalJsonPath", string.Format("{0}|Invalid filter condition format. Expected: 'property==value', got: '{1}'", traceId, filterCondition));
                    Logger.logEnd("GetValuesByConditionalJsonPath");
                    return null;
                }

                string filterProperty = filterParts[0].Trim();
                string filterValue = filterParts[1].Trim();

                Logger.Info("GetValuesByConditionalJsonPath", string.Format("{0}|ArrayPath: [{1}], Filter: [{2}=={3}], Target: [{4}]",
                    traceId, arrayPath, filterProperty, filterValue, targetProperty));

                // Get array using JSONPath
                JToken arrayToken = GetValueByJsonPath(jsonObject, arrayPath);
                if (arrayToken == null || !(arrayToken is JArray))
                {
                    Logger.Error("GetValuesByConditionalJsonPath", string.Format("{0}|Array path '{1}' not found or is not an array", traceId, arrayPath));
                    Logger.logEnd("GetValuesByConditionalJsonPath");
                    return null;
                }

                JArray array = arrayToken as JArray;
                List<JToken> results = new List<JToken>();

                // Filter and extract values
                foreach (JToken item in array)
                {
                    if (item is JObject itemObj)
                    {
                        // Check filter condition
                        JToken filterToken = itemObj[filterProperty];
                        if (filterToken == null)
                        {
                            // Try case-insensitive match
                            foreach (var prop in itemObj.Properties())
                            {
                                if (string.Equals(prop.Name, filterProperty, StringComparison.OrdinalIgnoreCase))
                                {
                                    filterToken = prop.Value;
                                    break;
                                }
                            }
                        }

                        if (filterToken != null && string.Equals(filterToken.ToString(), filterValue, StringComparison.OrdinalIgnoreCase))
                        {
                            // Extract target property
                            JToken targetToken = itemObj[targetProperty];
                            if (targetToken == null)
                            {
                                // Try case-insensitive match
                                foreach (var prop in itemObj.Properties())
                                {
                                    if (string.Equals(prop.Name, targetProperty, StringComparison.OrdinalIgnoreCase))
                                    {
                                        targetToken = prop.Value;
                                        break;
                                    }
                                }
                            }

                            if (targetToken != null)
                            {
                                results.Add(targetToken);
                            }
                        }
                    }
                }

                Logger.Info("GetValuesByConditionalJsonPath", string.Format("{0}|Found {1} matching items", traceId, results.Count));
                Logger.logEnd("GetValuesByConditionalJsonPath");
                return results;
            }
            catch (Exception ex)
            {
                Logger.Error("GetValuesByConditionalJsonPath", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetValuesByConditionalJsonPath");
                return null;
            }
        }

        /// <summary>
        /// Gets CashFlows for a specific AssetId by finding the Asset with PorS=="S" and matching its dmAssetId
        /// Format: Use conditional path to get dmAssetId, then find matching CashFlows
        /// Example usage: GetCashFlowsByAssetCondition(jsonObject, "$.TradeDetails.Assets|PorS==S|dmAssetId")
        /// </summary>
        /// <param name="jsonObject">The JObject</param>
        /// <param name="assetConditionPath">Conditional path to get AssetId (e.g., "$.TradeDetails.Assets|PorS==S|dmAssetId")</param>
        /// <returns>JArray of Flows, or null if not found</returns>
        public static JArray GetCashFlowsByAssetCondition(JObject jsonObject, string assetConditionPath)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("GetCashFlowsByAssetCondition", string.Format("{0}|AssetConditionPath:[{1}]", traceId, assetConditionPath));

            try
            {
                // Step 1: Get AssetId(s) using conditional path
                List<JToken> assetIds = GetValuesByConditionalJsonPath(jsonObject, assetConditionPath);
                if (assetIds == null || assetIds.Count == 0)
                {
                    Logger.Error("GetCashFlowsByAssetCondition", string.Format("{0}|No matching AssetId found", traceId));
                    Logger.logEnd("GetCashFlowsByAssetCondition");
                    return null;
                }

                // Step 2: Get CashFlows array
                JToken cashFlowsToken = GetValueByJsonPath(jsonObject, "$.CashFlows");
                if (cashFlowsToken == null || !(cashFlowsToken is JArray))
                {
                    Logger.Error("GetCashFlowsByAssetCondition", string.Format("{0}|CashFlows array not found", traceId));
                    Logger.logEnd("GetCashFlowsByAssetCondition");
                    return null;
                }

                JArray cashFlowsArray = cashFlowsToken as JArray;
                JArray resultFlows = new JArray();

                // Step 3: For each AssetId, find matching CashFlows entry
                foreach (JToken assetIdToken in assetIds)
                {
                    string assetId = assetIdToken.ToString();
                    Logger.Info("GetCashFlowsByAssetCondition", string.Format("{0}|Looking for CashFlows with AssetId: [{1}]", traceId, assetId));

                    foreach (JToken cashFlowItem in cashFlowsArray)
                    {
                        if (cashFlowItem is JObject cashFlowObj)
                        {
                            // Get AssetId from CashFlow
                            JToken cashFlowAssetIdToken = cashFlowObj["AssetId"];
                            if (cashFlowAssetIdToken == null)
                            {
                                // Try case-insensitive match
                                foreach (var prop in cashFlowObj.Properties())
                                {
                                    if (string.Equals(prop.Name, "AssetId", StringComparison.OrdinalIgnoreCase))
                                    {
                                        cashFlowAssetIdToken = prop.Value;
                                        break;
                                    }
                                }
                            }

                            if (cashFlowAssetIdToken != null && string.Equals(cashFlowAssetIdToken.ToString(), assetId, StringComparison.OrdinalIgnoreCase))
                            {
                                // Get Flows array
                                JToken flowsToken = cashFlowObj["Flows"];
                                if (flowsToken == null)
                                {
                                    // Try case-insensitive match
                                    foreach (var prop in cashFlowObj.Properties())
                                    {
                                        if (string.Equals(prop.Name, "Flows", StringComparison.OrdinalIgnoreCase))
                                        {
                                            flowsToken = prop.Value;
                                            break;
                                        }
                                    }
                                }

                                if (flowsToken != null && flowsToken is JArray flowsArray)
                                {
                                    // Add all flows to result
                                    foreach (JToken flow in flowsArray)
                                    {
                                        resultFlows.Add(flow);
                                    }
                                    Logger.Info("GetCashFlowsByAssetCondition", string.Format("{0}|Found {1} flows for AssetId: [{2}]", traceId, flowsArray.Count, assetId));
                                }
                                break; // Found matching CashFlow, move to next AssetId
                            }
                        }
                    }
                }

                Logger.Info("GetCashFlowsByAssetCondition", string.Format("{0}|Total flows found: {1}", traceId, resultFlows.Count));
                Logger.logEnd("GetCashFlowsByAssetCondition");
                return resultFlows;
            }
            catch (Exception ex)
            {
                Logger.Error("GetCashFlowsByAssetCondition", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("GetCashFlowsByAssetCondition");
                return null;
            }
        }

        /// <summary>
        /// Builds the main URL from APIEndPoint and UrlVariables
        /// </summary>
        /// <param name="jsonObject">The JSON configuration object</param>
        /// <param name="strError">Error message output</param>
        /// <returns>The built URL or null if error</returns>
        public static string BuildMainURL(JObject jsonObject, ref string strError)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("BuildMainURL", string.Format("{0}|Building main URL from JSON configuration", traceId));

            try
            {
                if (jsonObject == null)
                {
                    strError = "JSON object is null";
                    Logger.logEnd("BuildMainURL");
                    return null;
                }

                string apiEndPoint = GetStringValueByJsonPath(jsonObject, CNST_JSONPATH_API_ENDPOINT);
                if (string.IsNullOrEmpty(apiEndPoint))
                {
                    strError = "APIEndPoint is missing in JSON configuration";
                    Logger.logEnd("BuildMainURL");
                    return null;
                }

                JToken urlVariablesToken = GetValueByJsonPath(jsonObject, CNST_JSONPATH_URL_VARIABLES);
                if (urlVariablesToken == null || !(urlVariablesToken is JArray))
                {
                    strError = "UrlVariables is missing or invalid in JSON configuration";
                    Logger.logEnd("BuildMainURL");
                    return null;
                }

                JArray urlVariables = urlVariablesToken as JArray;
                string protocol = "";
                string host = "";
                string port = "";
                string path = "";

                foreach (JObject variable in urlVariables)
                {
                    string key = variable[CNST_ARRAY_PROP_KEY]?.ToString() ?? "";
                    string value = variable[CNST_ARRAY_PROP_VALUE]?.ToString() ?? "";

                    if (key.Equals(CNST_URL_VAR_PROTOCOL, StringComparison.OrdinalIgnoreCase))
                        protocol = value;
                    else if (key.Equals(CNST_URL_VAR_HOST, StringComparison.OrdinalIgnoreCase))
                        host = value;
                    else if (key.Equals(CNST_URL_VAR_PORT, StringComparison.OrdinalIgnoreCase))
                        port = value;
                    else if (key.Equals(CNST_URL_VAR_PATH, StringComparison.OrdinalIgnoreCase))
                        path = value;
                }

                if (string.IsNullOrEmpty(protocol) || string.IsNullOrEmpty(host))
                {
                    strError = "Protocol and Host are required in UrlVariables";
                    Logger.logEnd("BuildMainURL");
                    return null;
                }

                // Build URL: protocol://host:port/path
                string url = $"{protocol}://{host}";
                if (!string.IsNullOrEmpty(port))
                {
                    url += $":{port}";
                }
                if (!string.IsNullOrEmpty(path))
                {
                    if (!path.StartsWith("/"))
                        url += "/";
                    url += path;
                }

                Logger.logEnd("BuildMainURL", string.Format("{0}|URL built successfully: [{1}]", traceId, url));
                return url;
            }
            catch (Exception ex)
            {
                strError = $"Error building URL: {ex.Message}";
                Logger.Error("BuildMainURL", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("BuildMainURL");
                return null;
            }
        }

        /// <summary>
        /// Builds the complete URL including API parameters
        /// </summary>
        /// <param name="jsonObject">The JSON configuration object</param>
        /// <param name="mainUrl">The main URL (not used, will be rebuilt from UrlVariables)</param>
        /// <param name="strError">Error message output</param>
        /// <returns>The complete URL with parameters or null if error</returns>
        public static string BuildCompleteURL(JObject jsonObject, string mainUrl, ref string strError)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("BuildCompleteURL", string.Format("{0}|Building complete URL from UrlVariables", traceId));

            try
            {
                if (jsonObject == null)
                {
                    strError = "JSON object is null";
                    Logger.logEnd("BuildCompleteURL");
                    return null;
                }

                // Get UrlVariables and rebuild the main URL
                JToken urlVariablesToken = GetValueByJsonPath(jsonObject, CNST_JSONPATH_URL_VARIABLES);
                if (urlVariablesToken == null || !(urlVariablesToken is JArray))
                {
                    strError = "UrlVariables is missing or invalid in JSON configuration";
                    Logger.logEnd("BuildCompleteURL");
                    return null;
                }

                JArray urlVariables = urlVariablesToken as JArray;
                string protocol = "";
                string host = "";
                string port = "";
                string path = "";

                // Extract URL components from UrlVariables
                foreach (JObject variable in urlVariables)
                {
                    string key = variable[CNST_ARRAY_PROP_KEY]?.ToString() ?? "";
                    string value = variable[CNST_ARRAY_PROP_VALUE]?.ToString() ?? "";

                    if (key.Equals(CNST_URL_VAR_PROTOCOL, StringComparison.OrdinalIgnoreCase))
                        protocol = value;
                    else if (key.Equals(CNST_URL_VAR_HOST, StringComparison.OrdinalIgnoreCase))
                        host = value;
                    else if (key.Equals(CNST_URL_VAR_PORT, StringComparison.OrdinalIgnoreCase))
                        port = value;
                    else if (key.Equals(CNST_URL_VAR_PATH, StringComparison.OrdinalIgnoreCase))
                        path = value;
                }

                // Validate required components
                if (string.IsNullOrEmpty(protocol) || string.IsNullOrEmpty(host))
                {
                    strError = "Protocol and Host are required in UrlVariables";
                    Logger.logEnd("BuildCompleteURL");
                    return null;
                }

                // Rebuild the main URL from UrlVariables
                string completeUrl = $"{protocol}://{host}";
                if (!string.IsNullOrEmpty(port))
                {
                    completeUrl += $":{port}";
                }
                if (!string.IsNullOrEmpty(path))
                {
                    if (!path.StartsWith("/"))
                        completeUrl += "/";
                    completeUrl += path;
                }

                Logger.Info("BuildCompleteURL", string.Format("{0}|Main URL rebuilt: [{1}]", traceId, completeUrl));

                // Get APIParams and build query string
                JToken apiParamsToken = GetValueByJsonPath(jsonObject, CNST_JSONPATH_API_PARAMS);
                if (apiParamsToken == null)
                {
                    // No API parameters, return main URL
                    Logger.logEnd("BuildCompleteURL", string.Format("{0}|No API parameters, returning main URL: [{1}]", traceId, completeUrl));
                    return completeUrl;
                }

                // Check if path ends with /
                bool pathEndsWithSlash = !string.IsNullOrEmpty(path) && path.EndsWith("/");

                // Build query string from APIParams
                string queryString = "";
                if (apiParamsToken is JObject apiParams)
                {
                    var paramList = new System.Collections.Generic.List<string>();
                    foreach (var prop in apiParams.Properties())
                    {
                        string propValue = prop.Value?.ToString() ?? "";
                        
                        // Check if propValue starts with $, indicating a variable reference
                        if (!string.IsNullOrEmpty(propValue) && propValue.StartsWith("$"))
                        {
                            string variableName = propValue.Substring(1); // Remove $ prefix
                            if (!string.IsNullOrEmpty(variableName))
                            {
                                string variableValue = "";
                                string varError = "";
                                bool varResult = APIEngineVariableMgr.GetVariable(variableName, out variableValue, ref varError);
                                
                                if (varResult && !string.IsNullOrEmpty(variableValue))
                                {
                                    propValue = variableValue;
                                    Logger.Info("BuildCompleteURL", string.Format("{0}|Replaced variable reference ${1} with value: [{2}]", 
                                        traceId, variableName, variableValue));
                                }
                                else
                                {
                                    strError = $"Failed to get API variable '${variableName}': {varError}";
                                    Logger.Error("BuildCompleteURL", string.Format("{0}|{1}", traceId, strError));
                                    Logger.logEnd("BuildCompleteURL");
                                    return null;
                                }
                            }
                        }
                        
                        paramList.Add($"{Uri.EscapeDataString(prop.Name)}={Uri.EscapeDataString(propValue)}");
                    }
                    queryString = string.Join("&", paramList);
                }
                else if (apiParamsToken is JArray apiParamsArray)
                {
                    var paramList = new System.Collections.Generic.List<string>();
                    foreach (JObject param in apiParamsArray)
                    {
                        string key = param[CNST_ARRAY_PROP_KEY]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(key))
                            continue;

                        // Check if Description has a value, use it if available, otherwise use Value
                        string description = param[CNST_ARRAY_PROP_DESCRIPTION]?.ToString() ?? "";
                        string value = param[CNST_ARRAY_PROP_VALUE]?.ToString() ?? "";
                        
                        // Use Description value if it's not empty, otherwise use Value
                        string paramValue = !string.IsNullOrEmpty(description) ? description : value;
                        
                        // Check if paramValue starts with $, indicating a variable reference
                        if (!string.IsNullOrEmpty(paramValue) && paramValue.StartsWith("$"))
                        {
                            string variableName = paramValue;//dont Remove $ prefix
                            if (!string.IsNullOrEmpty(variableName))
                            {
                                string variableValue = "";
                                string varError = "";
                                bool varResult = APIEngineVariableMgr.GetVariable(variableName, out variableValue, ref varError);
                                
                                if (varResult && !string.IsNullOrEmpty(variableValue))
                                {
                                    paramValue = variableValue;
                                    Logger.Info("BuildCompleteURL", string.Format("{0}|Replaced variable reference ${1} with value: [{2}]", 
                                        traceId, variableName, variableValue));
                                }
                                else
                                {
                                    strError = $"Failed to get API variable '${variableName}': {varError}";
                                    Logger.Error("BuildCompleteURL", string.Format("{0}|{1}", traceId, strError));
                                    Logger.logEnd("BuildCompleteURL");
                                    return null;
                                }
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(description))
                        {
                            Logger.Info("BuildCompleteURL", string.Format("{0}|Using Description value for parameter [{1}]: [{2}]", traceId, key, description));
                        }
                        
                        paramList.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(paramValue)}");
                    }
                    queryString = string.Join("&", paramList);
                }

                // Append query string to URL
                if (!string.IsNullOrEmpty(queryString))
                {
                    if (pathEndsWithSlash)
                    {
                        completeUrl += "?" + queryString;
                    }
                    else
                    {
                        completeUrl += (completeUrl.Contains("?") ? "&" : "?") + queryString;
                    }
                }

                Logger.logEnd("BuildCompleteURL", string.Format("{0}|Complete URL built: [{1}]", traceId, completeUrl));
                return completeUrl;
            }
            catch (Exception ex)
            {
                strError = $"Error building complete URL: {ex.Message}";
                Logger.Error("BuildCompleteURL", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("BuildCompleteURL");
                return null;
            }
        }

        /// <summary>
        /// Invokes an API request and stores the response
        /// </summary>
        /// <param name="runOrdId">The test run order ID</param>
        /// <param name="strApiRunTimeConfig">The API runtime configuration JSON string</param>
        /// <param name="strError">Error message output</param>
        /// <param name="resultMessage">Result message output (SUCCESS or FAILED)</param>
        /// <param name="errorMessage">Error message output</param>
        /// <param name="returnedData">Returned data output</param>
        /// <param name="stackInfo">Stack trace info output</param>
        /// <param name="advice">Advice message output</param>
        /// <param name="askTime">Ask time output</param>
        /// <param name="ackTime">Acknowledge time output</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool InvokeAPI(long runOrdId, string strApiRunTimeConfig, ref string strError,
            ref string resultMessage, ref string errorMessage, ref string returnedData,
            ref string stackInfo, ref string advice, ref DateTime askTime, ref DateTime ackTime)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("InvokeAPI", string.Format("{0}|RunOrdId:[{1}]", traceId, runOrdId));

            try
            {
                askTime = DateTime.Now;

                // Step 1: Validate and parse JSON
                JObject jsonConfig = IsValidateJSON(strApiRunTimeConfig);
                if (jsonConfig == null)
                {
                    strError = "Invalid JSON format in strApiRunTimeConfig";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please check API Object's settings";
                    ackTime = DateTime.Now;
                    Logger.Error("InvokeAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("InvokeAPI");
                    return false;
                }

                Logger.Info("InvokeAPI", string.Format("{0}|JSON validation passed", traceId));

                // Step 2: Build main URL from APIEndPoint and UrlVariables
                string mainUrl = BuildMainURL(jsonConfig, ref strError);
                if (string.IsNullOrEmpty(mainUrl))
                {
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please check API Object's settings";
                    ackTime = DateTime.Now;
                    Logger.Error("InvokeAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("InvokeAPI");
                    return false;
                }

                Logger.Info("InvokeAPI", string.Format("{0}|Main URL built: [{1}]", traceId, mainUrl));

                // Step 3: Build complete URL with APIParams
                string completeUrl = BuildCompleteURL(jsonConfig, mainUrl, ref strError);
                if (string.IsNullOrEmpty(completeUrl))
                {
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please check API Object's settings";
                    ackTime = DateTime.Now;
                    Logger.Error("InvokeAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("InvokeAPI");
                    return false;
                }

                Logger.Info("InvokeAPI", string.Format("{0}|Complete URL: [{1}]", traceId, completeUrl));

                // Step 4: Send API request
                APINetWorkImpl.APIResponse apiResponse = 
                    APINetWorkImpl.SendRequest(jsonConfig, completeUrl, ref strError);

                if (apiResponse == null)
                {
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please check API Object's settings";
                    ackTime = DateTime.Now;
                    Logger.Error("InvokeAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("InvokeAPI");
                    return false;
                }

                Logger.Info("InvokeAPI", string.Format("{0}|API Response - StatusCode: [{1}], IsSuccess: [{2}]", 
                    traceId, apiResponse.StatusCode, apiResponse.IsSuccess));

                // Step 5: Handle response
                if (!apiResponse.IsSuccess)
                {
                    strError = apiResponse.ErrorMessage ?? $"HTTP {apiResponse.StatusCode}: {apiResponse.ResponseBody}";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    returnedData = apiResponse.ResponseBody;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please check API response and configuration";
                    ackTime = DateTime.Now;
                    Logger.Error("InvokeAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("InvokeAPI");
                    return false;
                }

                // Step 6: Store returned data
                if (!string.IsNullOrEmpty(apiResponse.ResponseBody))
                {
                    APIReturnedDataManagement.StoreReturnedData(runOrdId, apiResponse.ResponseBody);
                    Logger.Info("InvokeAPI", string.Format("{0}|Returned data stored for runOrdId: [{1}]", traceId, runOrdId));
                }

                // Success
                resultMessage = "SUCCESS";
                returnedData = apiResponse.ResponseBody;
                ackTime = DateTime.Now;
                Logger.Info("InvokeAPI", string.Format("{0}|API call completed successfully", traceId));
                Logger.logEnd("InvokeAPI");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in InvokeAPI: {ex.Message}";
                resultMessage = $"FAILED,{strError}";
                errorMessage = strError;
                stackInfo = MarsErrorStacks.StackTraceDump();
                advice = "Please check API Object's settings";
                ackTime = DateTime.Now;
                Logger.Error("InvokeAPI", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("InvokeAPI");
                return false;
            }
        }

        /// <summary>
        /// Extracts data from API response using JSONPath
        /// </summary>
        /// <param name="runOrdId">The test run order ID</param>
        /// <param name="strParaMeter">JSONPath expression to extract data</param>
        /// <param name="strData">Data parameter, usually in format "ToMem:VARIABLE_NAME"</param>
        /// <param name="memoryIndex">Output: The memory variable index extracted from strData</param>
        /// <param name="strError">Error message output</param>
        /// <param name="resultMessage">Result message output (SUCCESS or FAILED)</param>
        /// <param name="errorMessage">Error message output</param>
        /// <param name="returnedData">Returned data output (extracted value)</param>
        /// <param name="stackInfo">Stack trace info output</param>
        /// <param name="advice">Advice message output</param>
        /// <param name="askTime">Ask time output</param>
        /// <param name="ackTime">Acknowledge time output</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ExtractDataFromAPI(long runOrdId, string strParaMeter, string strData, 
            out string memoryIndex, ref string strError,
            ref string resultMessage, ref string errorMessage, ref string returnedData,
            ref string stackInfo, ref string advice, ref DateTime askTime, ref DateTime ackTime)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("ExtractDataFromAPI", string.Format("{0}|RunOrdId:[{1}], Parameter:[{2}], Data:[{3}]", 
                traceId, runOrdId, strParaMeter, strData));
            
            memoryIndex = null;

            try
            {
                askTime = DateTime.Now;

                // Step 1: Parse strData to get memory variable index
                if (string.IsNullOrEmpty(strData))
                {
                    strError = "strData is empty";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please provide strData in format 'ToMem:VARIABLE_NAME'";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractDataFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractDataFromAPI");
                    return false;
                }

                // Extract memory index from "ToMem:VARIABLE_NAME" format
                if (!strData.StartsWith("ToMem:", StringComparison.OrdinalIgnoreCase))
                {
                    strError = $"Invalid strData format. Expected format: 'ToMem:VARIABLE_NAME', but got: '{strData}'";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please check the Data parameter format. It should be 'ToMem:VARIABLE_NAME'";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractDataFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractDataFromAPI");
                    return false;
                }

                memoryIndex = strData.Substring(6); // Remove "ToMem:" prefix
                if (string.IsNullOrEmpty(memoryIndex))
                {
                    strError = "Memory variable name is empty after 'ToMem:' prefix";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please provide a valid variable name after 'ToMem:'";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractDataFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractDataFromAPI");
                    return false;
                }

                Logger.Info("ExtractDataFromAPI", string.Format("{0}|Memory index extracted: [{1}]", traceId, memoryIndex));

                // Step 2: Get the nearest returned data from APIReturnedDataManagement
                JObject jsonObject = APIReturnedDataManagement.GetNearestReturnedData(runOrdId);
                if (jsonObject == null)
                {
                    strError = $"No API returned data found for run order ID <= {runOrdId}";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please ensure that InvokeAPI keyword has been executed before ExtractDataFromAPI";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractDataFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractDataFromAPI");
                    return false;
                }

                Logger.Info("ExtractDataFromAPI", string.Format("{0}|Found nearest returned data for runOrdId: [{1}]", traceId, runOrdId));

                // Step 3: Extract value using JSONPath from strParaMeter
                if (string.IsNullOrEmpty(strParaMeter))
                {
                    strError = "JSONPath parameter (strParaMeter) is empty";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please provide a valid JSONPath expression in the Parameter field";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractDataFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractDataFromAPI");
                    return false;
                }

                JToken extractedValue = GetValueByJsonPath(jsonObject, strParaMeter);
                if (extractedValue == null)
                {
                    strError = $"JSONPath '{strParaMeter}' not found in the returned data";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = $"Please check if the JSONPath '{strParaMeter}' exists in the API response";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractDataFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractDataFromAPI");
                    return false;
                }

                string extractedValueStr = extractedValue.ToString();
                Logger.Info("ExtractDataFromAPI", string.Format("{0}|Extracted value using JSONPath [{1}]: [{2}]", 
                    traceId, strParaMeter, extractedValueStr));

                // Success - the caller will store the value in globalMemoryData
                resultMessage = "SUCCESS";
                returnedData = extractedValueStr;
                ackTime = DateTime.Now;
                Logger.Info("ExtractDataFromAPI", string.Format("{0}|Data extraction completed successfully. Value: [{1}]", 
                    traceId, extractedValueStr));
                Logger.logEnd("ExtractDataFromAPI");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in ExtractDataFromAPI: {ex.Message}";
                resultMessage = $"FAILED,{strError}";
                errorMessage = strError;
                stackInfo = MarsErrorStacks.StackTraceDump();
                advice = "Please check the API response data and JSONPath expression";
                ackTime = DateTime.Now;
                Logger.Error("ExtractDataFromAPI", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("ExtractDataFromAPI");
                return false;
            }
        }

        /// <summary>
        /// Extracts an array from API response using JSONPath, with optional sorting and memory variable substitution
        /// Supports format: "keyJsonPath::targetProperty" where keyJsonPath may contain {fromMem:VARIABLE_NAME}
        /// </summary>
        /// <param name="runOrdId">The test run order ID</param>
        /// <param name="strParaMeter">JSONPath expression, format: "keyJsonPath::targetProperty::formatConverter" where keyJsonPath may contain {fromMem:VARIABLE_NAME}</param>
        /// <param name="strData">Data parameter, usually in format "ToMem:VARIABLE_NAME"</param>
        /// <param name="memoryIndex">Output: The memory variable index extracted from strData</param>
        /// <param name="strError">Error message output</param>
        /// <param name="resultMessage">Result message output (SUCCESS or FAILED)</param>
        /// <param name="errorMessage">Error message output</param>
        /// <param name="returnedData">Returned data output (extracted values joined by \r\n)</param>
        /// <param name="stackInfo">Stack trace info output</param>
        /// <param name="advice">Advice message output</param>
        /// <param name="askTime">Ask time output</param>
        /// <param name="ackTime">Acknowledge time output</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool ExtractArrayFromAPI(long runOrdId, string strParaMeter, string strData,
            out string memoryIndex, ref string strError,
            ref string resultMessage, ref string errorMessage, ref string returnedData,
            ref string stackInfo, ref string advice, ref DateTime askTime, ref DateTime ackTime)
        {
            string traceId = GenerateTraceId();
            Logger.logBegin("ExtractArrayFromAPI", string.Format("{0}|RunOrdId:[{1}], Parameter:[{2}], Data:[{3}]",
                traceId, runOrdId, strParaMeter, strData));

            memoryIndex = null;

            try
            {
                askTime = DateTime.Now;

                // Step 1: Parse strData to get memory variable index
                if (string.IsNullOrEmpty(strData))
                {
                    strError = "strData is empty";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please provide strData in format 'ToMem:VARIABLE_NAME'";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractArrayFromAPI");
                    return false;
                }

                // Extract memory index from "ToMem:VARIABLE_NAME" format, if strData is not startwith tomem, then, itself is the 
                // the memory index
                if (!strData.StartsWith("ToMem:", StringComparison.OrdinalIgnoreCase))
                {
                    memoryIndex = strData;                    
                }else
                    memoryIndex = strData.Substring(6); // Remove "ToMem:" prefix
                if (string.IsNullOrEmpty(memoryIndex))
                {
                    strError = "Memory variable name is empty after 'ToMem:' prefix, or itself";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please provide a valid variable name after 'ToMem:'";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractArrayFromAPI");
                    return false;
                }

                Logger.Info("ExtractArrayFromAPI", string.Format("{0}|Memory index extracted: [{1}]", traceId, memoryIndex));

                // Step 2: Get the nearest returned data from APIReturnedDataManagement
                JObject jsonObject = APIReturnedDataManagement.GetNearestReturnedData(runOrdId);
                if (jsonObject == null)
                {
                    strError = $"No API returned data found for run order ID <= {runOrdId}";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please ensure that InvokeAPI keyword has been executed before ExtractArrayFromAPI";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractArrayFromAPI");
                    return false;
                }

                Logger.Info("ExtractArrayFromAPI", string.Format("{0}|Found nearest returned data for runOrdId: [{1}]", traceId, runOrdId));

                // Step 3: Parse strParaMeter
                string formatConverter = null;
                if (string.IsNullOrEmpty(strParaMeter))
                {
                    strError = "JSONPath parameter (strParaMeter) is empty";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = "Please provide a valid JSONPath expression in the Parameter field";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractArrayFromAPI");
                    return false;
                }

                string keyJsonPath = null;
                string targetProperty = null;
                bool isJustGetKey = false;

                // Check if parameter contains "::" separator
                if (strParaMeter.Contains("::"))
                {
                    string[] parts = strParaMeter.Split(new[] { "::" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        keyJsonPath = parts[0].Trim();
                        targetProperty = parts[1].Trim();
                        formatConverter = null; // No format converter
                    }
                    else if (parts.Length == 3)
                    {
                        keyJsonPath = parts[0].Trim();
                        targetProperty = parts[1].Trim();
                        formatConverter = parts[2].Trim();
                    }
                    else
                    {
                        strError = $"Invalid parameter format. Expected 'keyJsonPath::targetProperty' or 'keyJsonPath::targetProperty::formatConverter', but got: '{strParaMeter}'";
                        resultMessage = $"FAILED,{strError}";
                        errorMessage = strError;
                        stackInfo = MarsErrorStacks.StackTraceDump();
                        advice = "Please use '::' to separate key JSONPath, target property, and optional format converter";
                        ackTime = DateTime.Now;
                        Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                        Logger.logEnd("ExtractArrayFromAPI");
                        return false;
                    }
                }
                else
                {
                    isJustGetKey = true;
                }

                Logger.Info("ExtractArrayFromAPI", string.Format("{0}|Key JSONPath: [{1}], Target Property: [{2}], Format Converter: [{3}]",
                    traceId, keyJsonPath, targetProperty, formatConverter ?? "None"));

                // Step 4: Replace {fromMem:VARIABLE_NAME} in keyJsonPath with actual values from memory
                string resolvedKeyJsonPath = keyJsonPath;
                if (keyJsonPath.Contains("{fromMem:"))
                {
                    // Find all {fromMem:VARIABLE_NAME} patterns
                    System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\{fromMem:([^}]+)\}");
                    var matches = regex.Matches(keyJsonPath);
                    
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        string fullMatch = match.Value; // e.g., "{fromMem:API_PAYSIDE_ASSETID}"
                        string variableName = match.Groups[1].Value; // e.g., "API_PAYSIDE_ASSETID"
                        
                        // Get value from memory
                        string memValue = "";
                        string memError = "";
                        bool memResult = CaptureParaMgr.GetVariableByIdx(variableName, ref memValue, ref memError);
                        
                        if (!memResult)
                        {
                            strError = $"Failed to get memory variable '{variableName}': {memError}";
                            resultMessage = $"FAILED,{strError}";
                            errorMessage = strError;
                            stackInfo = MarsErrorStacks.StackTraceDump();
                            advice = $"Please ensure that memory variable '{variableName}' has been set before using ExtractArrayFromAPI";
                            ackTime = DateTime.Now;
                            Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                            Logger.logEnd("ExtractArrayFromAPI");
                            return false;
                        }
                        
                        // Replace {fromMem:VARIABLE_NAME} with actual value
                        resolvedKeyJsonPath = resolvedKeyJsonPath.Replace(fullMatch, memValue);
                        Logger.Info("ExtractArrayFromAPI", string.Format("{0}|Replaced '{1}' with '{2}'", traceId, fullMatch, memValue));
                    }
                }

                Logger.Info("ExtractArrayFromAPI", string.Format("{0}|Resolved Key JSONPath: [{1}]", traceId, resolvedKeyJsonPath));

                // Step 5: Build target-jsonpath by replacing the last property in key-jsonpath with targetProperty
                string targetJsonPath = resolvedKeyJsonPath;                
                if (!isJustGetKey)
                {
                    int lastDotIndex = resolvedKeyJsonPath.LastIndexOf('.');
                    if (lastDotIndex >= 0 && lastDotIndex < resolvedKeyJsonPath.Length - 1)
                    {
                        targetJsonPath = resolvedKeyJsonPath.Substring(0, lastDotIndex + 1) + targetProperty;
                    }
                    else
                    {
                        // If no dot found, append targetProperty
                        targetJsonPath = resolvedKeyJsonPath + "." + targetProperty;
                    }
                }

                Logger.Info("ExtractArrayFromAPI", string.Format("{0}|Target JSONPath: [{1}]", traceId, targetJsonPath));

                // Step 6: Extract items using key-jsonpath and target-jsonpath

                var sortJson    = GetValuesByJsonPath(jsonObject, resolvedKeyJsonPath);
                var targetJson  = isJustGetKey?sortJson: GetValuesByJsonPath(jsonObject, targetJsonPath);
                /// step 7: 将sortjson和targetjons结合成keyvalue对
                /// 
                Dictionary<string, string> sortableData = new Dictionary<string, string>();
                if ((sortJson is System.Collections.IList sortArray)&&(targetJson is System.Collections.IList targetArray))
                {
                    if (sortArray.Count != targetArray.Count)
                    {
                        strError = $"The number of items in Key json|{resolvedKeyJsonPath}| and target json data|{targetJsonPath}| do not match";
                        resultMessage = $"FAILED,{strError}";
                        errorMessage = strError;
                        stackInfo = MarsErrorStacks.StackTraceDump();
                        advice = $"Please check if the JSONPaths are correct and correspond to the same array items";
                        ackTime = DateTime.Now;
                        Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                        Logger.logEnd("ExtractArrayFromAPI");
                        return false;
                    }
                    foreach (var index in Enumerable.Range(0, sortArray.Count))
                    {
                        var keyItem = sortArray[index];
                        var targetItem = targetArray[index];
                        string keyStr = keyItem != null ? keyItem.ToString() : "";
                        string targetStr = targetItem != null ? targetItem.ToString() : "";
                        
                        // Apply format converter if specified
                        if (!string.IsNullOrEmpty(formatConverter) && !string.IsNullOrEmpty(targetStr))
                        {
                            targetStr = ApplyFormatConverter(targetStr, formatConverter, ref strError);
                            if (targetStr == null)
                            {
                                // Error occurred in format conversion
                                resultMessage = $"FAILED,{strError}";
                                errorMessage = strError;
                                stackInfo = MarsErrorStacks.StackTraceDump();
                                advice = "Please check the format converter and ensure the data can be converted";
                                ackTime = DateTime.Now;
                                Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                                Logger.logEnd("ExtractArrayFromAPI");
                                return false;
                            }
                        }
                        
                        sortableData.Add(keyStr, targetStr);
                        //returnedData += keyStr + "::" + targetStr + "\r\n";
                    }
                    // Step 9: Sort the key-value pairs by key
                    var sortedData = sortableData.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase);
                    var resultData = sortedData.Select(p => p.Value).ToList();
                    string strResult = string.Join("\r\n", resultData);

                    returnedData = strResult;
                    resultMessage = "SUCCESS";
                    ackTime = DateTime.Now;
                    Logger.Info("ExtractArrayFromAPI", string.Format("{0}|Data extraction and sorting completed successfully.", traceId));
                    Logger.logEnd("ExtractArrayFromAPI");
                    return true;
                }
                else
                {
                    strError = $"can't find Key json|{resolvedKeyJsonPath}| or target json data|{targetJsonPath}|";
                    resultMessage = $"FAILED,{strError}";
                    errorMessage = strError;
                    stackInfo = MarsErrorStacks.StackTraceDump();
                    advice = $"Please check if any items match the condition in the API response";
                    ackTime = DateTime.Now;
                    Logger.Error("ExtractArrayFromAPI", string.Format("{0}|{1}", traceId, strError));
                    Logger.logEnd("ExtractArrayFromAPI");
                    return false;
                }

            }
            catch (Exception ex)
            {
                strError = $"Exception in ExtractArrayFromAPI: {ex.Message}";
                resultMessage = $"FAILED,{strError}";
                errorMessage = strError;
                stackInfo = MarsErrorStacks.StackTraceDump();
                advice = "Please check the API response data and JSONPath expression";
                ackTime = DateTime.Now;
                Logger.Error("ExtractArrayFromAPI", string.Format("{0}|Exception: {1}", traceId, ex.Message), ex.StackTrace);
                Logger.logEnd("ExtractArrayFromAPI");
                return false;
            }
        }               
        
    }
}

