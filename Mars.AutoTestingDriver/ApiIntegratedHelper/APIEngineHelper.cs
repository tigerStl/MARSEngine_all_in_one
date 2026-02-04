using System;

namespace Mars.AutoTestingDriver.ApiIntegratedHelper
{
    /// <summary>
    /// API Engine Helper class for checking if keywords are API integrated keywords
    /// </summary>
    public class APIEngineHelper
    {
        /// <summary>
        /// Constant for InvokeAPI keyword
        /// </summary>
        public const string CNST_KEYWORD_INVOKE_API = "InvokeAPI";

        /// <summary>
        /// Constant for ExtractDataFromAPI keyword
        /// </summary>
        public const string CNST_KEYWORD_EXTRACT_DATA_FROM_API = "ExtractDataFromAPI";

        /// <summary>
        /// Constant for ExtractArrayFromAPI keyword
        /// </summary>
        public const string CNST_KEYWORD_EXTRACT_ARRAY_FROM_API = "ExtractArrayFromAPI";

        /// <summary>
        /// Constant for APICaptureAndCompare keyword
        /// </summary>
        public const string CNST_KEYWORD_API_CAPTURE_AND_COMPARE = "APICaptureAndCompare";

        public const string CNST_KEYWORD_API_SET_VARIABLE = "APISetVariable";

        // JSONPath constants
        /// <summary>
        /// JSONPath constant for APIEndPoint
        /// </summary>
        public const string CNST_JSONPATH_API_ENDPOINT = "$.APIEndpoint";

        /// <summary>
        /// JSONPath constant for UrlVariables
        /// </summary>
        public const string CNST_JSONPATH_URL_VARIABLES = "$.UrlVariables";

        /// <summary>
        /// JSONPath constant for APIParams
        /// </summary>
        public const string CNST_JSONPATH_API_PARAMS = "$.APIParams";

        // JSON property name constants
        /// <summary>
        /// JSON property name constant for Verb
        /// </summary>
        public const string CNST_JSON_PROP_VERB = "Verb";

        /// <summary>
        /// JSON property name constant for Headers
        /// </summary>
        public const string CNST_JSON_PROP_HEADERS = "Headers";

        /// <summary>
        /// JSON property name constant for Body
        /// </summary>
        public const string CNST_JSON_PROP_BODY = "Body";

        // URL variable key constants
        /// <summary>
        /// URL variable key constant for protocol
        /// </summary>
        public const string CNST_URL_VAR_PROTOCOL = "$protocol";

        /// <summary>
        /// URL variable key constant for host
        /// </summary>
        public const string CNST_URL_VAR_HOST = "$host";

        /// <summary>
        /// URL variable key constant for port
        /// </summary>
        public const string CNST_URL_VAR_PORT = "$port";

        /// <summary>
        /// URL variable key constant for path
        /// </summary>
        public const string CNST_URL_VAR_PATH = "$path";

        // Array object property name constants
        /// <summary>
        /// Array object property name constant for Key
        /// </summary>
        public const string CNST_ARRAY_PROP_KEY = "Key";

        /// <summary>
        /// Array object property name constant for Value
        /// </summary>
        public const string CNST_ARRAY_PROP_VALUE = "Value";

        /// <summary>
        /// Array object property name constant for Description
        /// </summary>
        public const string CNST_ARRAY_PROP_DESCRIPTION = "Description";

        /// <summary>
        /// Checks if the given keyword is an API integrated keyword
        /// </summary>
        /// <param name="keyword">The keyword to check</param>
        /// <returns>True if the keyword is one of the API integrated keywords, false otherwise</returns>
        public static bool IsKeywordAPIIntegrated(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
                return false;

            return string.Equals(keyword, CNST_KEYWORD_INVOKE_API, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(keyword, CNST_KEYWORD_EXTRACT_DATA_FROM_API, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(keyword, CNST_KEYWORD_EXTRACT_ARRAY_FROM_API, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(keyword, CNST_KEYWORD_API_CAPTURE_AND_COMPARE, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(keyword, CNST_KEYWORD_API_SET_VARIABLE, StringComparison.OrdinalIgnoreCase) ;
        }
    }
}

