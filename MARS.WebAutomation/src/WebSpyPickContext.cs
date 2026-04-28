using System;

namespace MARS.WebAutomation
{
    /// <summary>
    /// Context passed from MarsSpyTool when the user drops the Finder crosshair on a web browser window.
    /// </summary>
    public sealed class WebSpyPickContext
    {
        public IntPtr TargetHwnd { get; set; }
        public int ScreenX { get; set; }
        public int ScreenY { get; set; }
        public string ProcessName { get; set; }
        public string AutomationId { get; set; }
        public string UiName { get; set; }
        public string ClassName { get; set; }
    }
}
