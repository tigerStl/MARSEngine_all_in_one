namespace MARS.WebAutomation.Services
{
    public sealed class WorkbenchSettings
    {
        public string DataRootFolder { get; set; }
        public bool Headless { get; set; }
        public int DefaultTimeoutMs { get; set; } = 30000;
        public bool PersistSensitiveHeaders { get; set; }
        public string BrowserChannel { get; set; }
        public int ViewportWidth { get; set; } = 1280;
        public int ViewportHeight { get; set; } = 720;
        public bool RecordReplayHotkeyCtrl { get; set; } = true;
        public bool RecordReplayHotkeyAlt { get; set; }
        public bool RecordReplayHotkeyShift { get; set; }
        public string RecordReplayHotkeyKey { get; set; } = "F12";
        public string RecorderIgnoredPageUrlPrefixes { get; set; } = "chrome://;devtools://;edge://;about:";

        /// <summary>Ancestor walk depth when detecting tab strips (tablist / Vue tabs) for <c>SelectTab</c> semantics.</summary>
        public int RecorderTabContextAncestorDepth { get; set; } = 5;

        /// <summary>UI language: <c>en</c> or <c>zh</c> (persisted with workbench settings).</summary>
        public string UiLanguage { get; set; } = "en";

        public static WorkbenchSettings CreateDefault()
        {
            return new WorkbenchSettings
            {
                DataRootFolder = System.IO.Path.Combine(DataPathHelper.GetAssemblyBaseDirectory(), "data"),
                Headless = false,
                PersistSensitiveHeaders = false,
                RecordReplayHotkeyCtrl = true,
                RecordReplayHotkeyAlt = false,
                RecordReplayHotkeyShift = false,
                RecordReplayHotkeyKey = "F12",
                RecorderIgnoredPageUrlPrefixes = "chrome://;devtools://;edge://;about:",
                RecorderTabContextAncestorDepth = 5
            };
        }
    }
}
