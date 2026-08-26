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

        /// <summary>Milliseconds to wait after <c>DOMContentLoaded</c> before DOM assertion snapshot evaluation (async UI stabilization).</summary>
        public int AssertSnapshotSettleMs { get; set; } = 220;

        /// <summary>Maximum interactive elements collected per frame during assertion snapshots (performance guard).</summary>
        public int AssertSnapshotMaxElementsPerFrame { get; set; } = 500;

        /// <summary>When true, color-only diffs also emit an <c>AssertScreenshot</c> step (mode 4 bridge).</summary>
        public bool AssertDiffEmitScreenshotOnColorChange { get; set; }

        public bool AssertHotkeyBeforeCtrl { get; set; } = true;
        public bool AssertHotkeyBeforeAlt { get; set; } = true;
        public bool AssertHotkeyBeforeShift { get; set; }
        public string AssertHotkeyBeforeKey { get; set; } = "F10";

        public bool AssertHotkeyAfterCtrl { get; set; } = true;
        public bool AssertHotkeyAfterAlt { get; set; } = true;
        public bool AssertHotkeyAfterShift { get; set; }
        public string AssertHotkeyAfterKey { get; set; } = "F11";

        public string RecorderIgnoredPageUrlPrefixes { get; set; } = "chrome://;devtools://;edge://;about:";

        /// <summary>Ancestor walk depth when detecting tab strips (tablist / Vue tabs) for <c>SelectTab</c> semantics.</summary>
        public int RecorderTabContextAncestorDepth { get; set; } = 5;

        /// <summary>Recording capture mode: <c>semantic</c> (tab/menu/rules/table promotion) or <c>plain</c> (event target only; Playwright snippet on each step).</summary>
        public string RecorderCaptureMode { get; set; } = "semantic";

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

        /// <summary>Width (px) of the step object property panel when expanded (Record / Replay canvas split).</summary>
        public int StepPropertyPanelExpandedWidthPx { get; set; } = 240;

        /// <summary>When true, the step object property panel is collapsed to a narrow strip.</summary>
        public bool StepPropertyPanelCollapsed { get; set; }

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
                RecorderTabContextAncestorDepth = 5,
                RecorderCaptureMode = "semantic"
            };
        }
    }
}
