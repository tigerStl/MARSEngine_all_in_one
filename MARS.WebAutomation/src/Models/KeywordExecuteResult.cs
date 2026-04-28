namespace MARS.WebAutomation.Models
{
    public sealed class KeywordExecuteResult
    {
        public bool Success { get; set; }
        public string DataReturned { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorStackTrace { get; set; }
    }
}
