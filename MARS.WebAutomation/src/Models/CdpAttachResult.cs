namespace MARS.WebAutomation.Models
{
    /// <summary>Outcome of <see cref="Services.PlaywrightHostService.TryConnectChromiumOverCdpAsync"/>.</summary>
    public sealed class CdpAttachResult
    {
        private CdpAttachResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public bool Success { get; }

        /// <summary>Empty when <see cref="Success"/> is true; otherwise the best-effort explanation for the UI.</summary>
        public string ErrorMessage { get; }

        public static CdpAttachResult Ok() => new CdpAttachResult(true, string.Empty);

        public static CdpAttachResult Failed(string errorMessage) => new CdpAttachResult(false, errorMessage ?? string.Empty);
    }
}
