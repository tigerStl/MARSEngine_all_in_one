namespace MARS.WebAutomation.Services
{
    public sealed class WorkbenchSettings
    {
        public string DataRootFolder { get; set; }
        public bool Headless { get; set; }
        public int DefaultTimeoutMs { get; set; } = 30000;
        public bool PersistSensitiveHeaders { get; set; }
        public string BrowserChannel { get; set; }
        public bool UseExistingBrowser { get; set; } = true;
        public string ExistingBrowserCdpEndpoint { get; set; } = "http://127.0.0.1:9222";
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

        /// <summary>Semicolon-separated performance filter tokens (e.g. heartbeat;handshake;xhr;document).</summary>
        public string PerformanceFilterTokens { get; set; } = "heartbeat;handshake";
        public string PerformanceIgnoreExactUrls { get; set; } = string.Empty;
        public string PerformanceIgnoreWildcardUrls { get; set; } = string.Empty;
        /// <summary>Semicolon-separated URL ignore patterns for performance captures (supports '*' and '?').</summary>
        public string PerformanceIgnoreUrlPatterns { get; set; } = string.Empty;
        /// <summary>Whether performance panel is shown in Record/Replay tab.</summary>
        public bool PerformancePanelEnabled { get; set; } = true;
        public int PerformanceSimUserCount { get; set; } = 5;

        /// <summary>Last NBomber run dialog: concurrent users (0 = use toolbar PerformanceSimUserCount).</summary>
        public int LastPerformanceRunUsers { get; set; }

        /// <summary>Last NBomber run dialog: duration seconds (0 = use in-session default).</summary>
        public int LastPerformanceRunDurationSeconds { get; set; }

        public int LastPerformanceRunChartIntervalSeconds { get; set; } = 3;
        /// <summary>constant or stepped</summary>
        public string LastPerformanceRunMode { get; set; } = "constant";
        public int LastPerformanceRunInitialUsers { get; set; }
        public int LastPerformanceRunUsersStep { get; set; }

        public bool LastPerformanceRunSaveResponses { get; set; }

        public string LastPerformanceRunBodyMustContain { get; set; }

        public static WorkbenchSettings CreateDefault()
        {
            return new WorkbenchSettings
            {
                DataRootFolder = System.IO.Path.Combine(DataPathHelper.GetAssemblyBaseDirectory(), "data"),
                Headless = false,
                PersistSensitiveHeaders = false,
                UseExistingBrowser = true,
                ExistingBrowserCdpEndpoint = "http://127.0.0.1:9222",
                PerformancePanelEnabled = true,
                PerformanceSimUserCount = 5,
                LastPerformanceRunChartIntervalSeconds = 3,
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
