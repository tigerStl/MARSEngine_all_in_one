using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MARS.WebAutomation.Models;
using Newtonsoft.Json;

namespace MARS.WebAutomation.UI
{
    internal sealed partial class RecordReplaySidebarForm : Form
    {
        private readonly List<RecordReplayEventCard> _cards = new List<RecordReplayEventCard>();
        private readonly object _sync = new object();
        private WebBrowser _web;
        private int _seq;
        private bool _replayPlanMode;
        private string _bottomMessage = string.Empty;
        private static readonly string LogDir = @"c:\temp\Mars.automationweb.log";
        private static readonly string LogFile = Path.Combine(LogDir, "record-replay-events.jsonl");

        private IntPtr _mouseHook = IntPtr.Zero;
        private IntPtr _keyHook = IntPtr.Zero;
        private HookProc _mouseProc;
        private HookProc _keyProc;

        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const uint ABM_NEW = 0x00000000;
        private const uint ABM_REMOVE = 0x00000001;
        private const uint ABM_QUERYPOS = 0x00000002;
        private const uint ABM_SETPOS = 0x00000003;
        private const int APPBAR_CALLBACK = 0x5001;

        public RecordReplaySidebarForm()
        {
            InitializeComponent();
            Width = 300;
            Left = 0;
            Top = 0;
            Height = Screen.PrimaryScreen.Bounds.Height;

            EnsureLogDirectory();
            RenderCards();
        }

        /// <summary>Optional global input tracing (not used during normal record/replay monitor).</summary>
        public void StartCapture()
        {
            _mouseProc = MouseHookCallback;
            _keyProc = KeyboardHookCallback;
            var hMod = GetModuleHandle(null);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
            _keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyProc, hMod, 0);
            AddRecordCard(
                new RecordReplayEventCard
                {
                    EventName = "RecordReplayStart",
                    ObjectType = "System",
                    Data = $"MouseHook={_mouseHook != IntPtr.Zero}; KeyHook={_keyHook != IntPtr.Zero}"
                });
        }

        public void StopCapture()
        {
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }
            if (_keyHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyHook);
                _keyHook = IntPtr.Zero;
            }
        }

        /// <summary>Prepares the sidebar for replay: clears hook noise rows, loads one card per test step.</summary>
        public void BeginReplayPlan(IReadOnlyList<SemanticStepRecord> steps)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => BeginReplayPlan(steps)));
                return;
            }

            StopCapture();
            _replayPlanMode = true;
            _bottomMessage = string.Empty;
            lock (_sync)
            {
                _cards.Clear();
                _seq = 0;
                if (steps == null)
                {
                    RenderCards();
                    return;
                }
                for (var i = 0; i < steps.Count; i++)
                {
                    var s = steps[i];
                    var loc = SemanticStepLocatorUtil.EffectivePlaywrightSelector(s) ?? string.Empty;
                    if (loc.Length > 140)
                        loc = loc.Substring(0, 137) + "…";
                    var data = s?.Data ?? string.Empty;
                    if (data.Length > 200)
                        data = data.Substring(0, 197) + "…";
                    _cards.Add(
                        new RecordReplayEventCard
                        {
                            Sequence = i + 1,
                            TimestampUtc = DateTime.UtcNow,
                            EventName = s?.Keyword ?? string.Empty,
                            ObjectType = s?.LogicalKind ?? string.Empty,
                            Tag = string.Empty,
                            DataAttributes = string.Empty,
                            Xpath = s?.ElementXpath ?? string.Empty,
                            Value = loc,
                            Data = data,
                            IsReplayPlanRow = true,
                            ReplayStepIndex = i,
                            ReplayPhase = "pending",
                            ReplayErrorMessage = string.Empty,
                            SuppressFileLog = true
                        });
                }
            }

            RenderCards();
        }

        /// <summary>Updates replay row highlight and optional bottom error strip.</summary>
        /// <param name="phase">before | afterOk | afterErr</param>
        public void SetReplayProgress(int stepIndex, string phase, string errorMessage = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => SetReplayProgress(stepIndex, phase, errorMessage)));
                return;
            }

            if (!_replayPlanMode)
                return;

            lock (_sync)
            {
                if (string.Equals(phase, "before", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var c in _cards.Where(x => x.IsReplayPlanRow && string.Equals(x.ReplayPhase, "active", StringComparison.OrdinalIgnoreCase)))
                        c.ReplayPhase = "ok";
                    var cur = _cards.FirstOrDefault(x => x.IsReplayPlanRow && x.ReplayStepIndex == stepIndex);
                    if (cur != null)
                    {
                        cur.ReplayPhase = "active";
                        cur.ReplayErrorMessage = string.Empty;
                    }
                    _bottomMessage = string.Empty;
                }
                else if (string.Equals(phase, "afterOk", StringComparison.OrdinalIgnoreCase))
                {
                    var cur = _cards.FirstOrDefault(x => x.IsReplayPlanRow && x.ReplayStepIndex == stepIndex);
                    if (cur != null)
                    {
                        cur.ReplayPhase = "ok";
                        cur.ReplayErrorMessage = string.Empty;
                    }
                    _bottomMessage = string.Empty;
                }
                else if (string.Equals(phase, "afterErr", StringComparison.OrdinalIgnoreCase))
                {
                    var cur = _cards.FirstOrDefault(x => x.IsReplayPlanRow && x.ReplayStepIndex == stepIndex);
                    if (cur != null)
                    {
                        cur.ReplayPhase = "error";
                        cur.ReplayErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Step failed." : errorMessage.Trim();
                    }
                    _bottomMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Step failed." : errorMessage.Trim();
                }
            }

            RenderCards();
        }

        public void AddRecordCard(RecordReplayEventCard card)
        {
            if (card == null)
                return;
            lock (_sync)
            {
                card.Sequence = ++_seq;
                card.TimestampUtc = card.TimestampUtc == default(DateTime) ? DateTime.UtcNow : card.TimestampUtc;
                _cards.Add(card);
            }

            if (!card.SuppressFileLog)
                AppendCardLog(card);
            RenderCards();
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = wParam.ToInt32();
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN)
                {
                    var hs = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    AddRecordCard(
                        new RecordReplayEventCard
                        {
                            EventName = msg == WM_LBUTTONDOWN ? "MouseLeftDown" : "MouseRightDown",
                            Position = $"{hs.pt.X},{hs.pt.Y}",
                            ObjectType = "SystemInput",
                            Data = "Global mouse hook"
                        });
                }
                else if (msg == WM_MOUSEMOVE && (_seq % 20 == 0))
                {
                    var hs = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    AddRecordCard(
                        new RecordReplayEventCard
                        {
                            EventName = "MouseMove",
                            Position = $"{hs.pt.X},{hs.pt.Y}",
                            ObjectType = "SystemInput",
                            Data = "Global mouse move"
                        });
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    var hs = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var vkCode = (int)hs.vkCode;
                    AddRecordCard(
                        new RecordReplayEventCard
                        {
                            EventName = "KeyDown",
                            ObjectType = "SystemInput",
                            Data = ((Keys)vkCode).ToString()
                        });
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private void RenderCards()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)RenderCards);
                return;
            }

            List<RecordReplayEventCard> snap;
            lock (_sync)
                snap = new List<RecordReplayEventCard>(_cards);

            var foot = WebUtility.HtmlEncode(_bottomMessage ?? string.Empty);
            var footClass = string.IsNullOrWhiteSpace(_bottomMessage) ? "" : " err";

            var sb = new StringBuilder(4096 + snap.Count * 400);
            sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\" />");
            sb.AppendLine("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />");
            sb.AppendLine("<style>");
            sb.AppendLine("html,body{height:100%;margin:0}");
            sb.AppendLine(".app{display:flex;flex-direction:column;height:100%;font-family:Segoe UI,Arial;background:#f8fafc}");
            sb.AppendLine("#cards{flex:1;overflow:auto;padding:8px}");
            sb.AppendLine("#foot{min-height:56px;max-height:120px;overflow:auto;border-top:1px solid #e2e8f0;padding:8px 10px;font-size:11px;color:#475569;background:#f1f5f9;white-space:pre-wrap;word-break:break-word}");
            sb.AppendLine("#foot.err{background:#fee2e2;color:#991b1b;border-top-color:#fecaca}");
            sb.AppendLine(".card{background:#fff;border:1px solid #cbd5e1;border-radius:6px;padding:8px;margin-bottom:8px;font-size:12px}");
            sb.AppendLine(".card.active{outline:2px solid #2563eb;background:#eff6ff;border-color:#93c5fd}");
            sb.AppendLine(".card.ok{border-color:#86efac;background:#f0fdf4}");
            sb.AppendLine(".card.error{background:#fee2e2;border-color:#f87171}");
            sb.AppendLine(".t{font-weight:700;color:#0f172a}");
            sb.AppendLine(".k{color:#334155;font-weight:600}");
            sb.AppendLine(".v{color:#0f172a;word-break:break-all}");
            sb.AppendLine(".ph{font-size:10px;color:#64748b;text-transform:uppercase;letter-spacing:.04em}");
            sb.AppendLine(".errline{margin-top:6px;font-size:11px;color:#991b1b;white-space:pre-wrap;word-break:break-word}");
            sb.AppendLine("</style></head><body><div class=\"app\"><div id=\"cards\">");

            foreach (var c in snap)
            {
                var isReplay = c.IsReplayPlanRow;
                var phase = isReplay ? (c.ReplayPhase ?? "pending") : string.Empty;
                var cls = isReplay ? ("card " + phase) : "card";
                sb.Append("<div class=\"").Append(WebUtility.HtmlEncode(cls)).Append("\">");

                sb.Append("<div class=\"ph\">");
                if (isReplay)
                {
                    sb.Append("Step ").Append(c.ReplayStepIndex + 1).Append(" · ").Append(WebUtility.HtmlEncode(phase));
                }
                else
                {
                    sb.Append("#").Append(c.Sequence).Append(" · ").Append(WebUtility.HtmlEncode(c.EventName ?? string.Empty));
                }
                sb.Append("</div>");

                var title = (c.EventName ?? string.Empty) + (string.IsNullOrEmpty(c.ObjectType) ? string.Empty : (" · " + c.ObjectType));
                sb.Append("<div class=\"t\">").Append(WebUtility.HtmlEncode(title)).Append("</div>");

                sb.Append("<div><span class=\"k\">Locator:</span> <span class=\"v\">").Append(WebUtility.HtmlEncode(c.Value ?? string.Empty)).Append("</span></div>");
                sb.Append("<div><span class=\"k\">Data:</span> <span class=\"v\">").Append(WebUtility.HtmlEncode(c.Data ?? string.Empty)).Append("</span></div>");
                sb.Append("<div><span class=\"k\">XPath:</span> <span class=\"v\">").Append(WebUtility.HtmlEncode(c.Xpath ?? string.Empty)).Append("</span></div>");
                sb.Append("<div><span class=\"k\">Position:</span> <span class=\"v\">").Append(WebUtility.HtmlEncode(c.Position ?? string.Empty)).Append("</span></div>");
                sb.Append("<div><span class=\"k\">Request:</span> <span class=\"v\">").Append(WebUtility.HtmlEncode(c.ListenedRequestUrl ?? string.Empty)).Append("</span></div>");

                if (isReplay && !string.IsNullOrWhiteSpace(c.ReplayErrorMessage))
                {
                    sb.Append("<div class=\"errline\"><span class=\"k\">Error:</span> <span class=\"v\">")
                        .Append(WebUtility.HtmlEncode(c.ReplayErrorMessage))
                        .Append("</span></div>");
                }

                sb.Append("</div>");
            }

            sb.Append("</div><div id=\"foot\" class=\"").Append(footClass.Trim()).Append("\">").Append(foot).Append("</div></div></body></html>");
            _web.DocumentText = sb.ToString();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            DockToLeftAppBar();
        }

        private void DockToLeftAppBar()
        {
            var abd = new APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)),
                hWnd = Handle,
                uCallbackMessage = APPBAR_CALLBACK,
                uEdge = ABE_LEFT
            };
            SHAppBarMessage(ABM_NEW, ref abd);

            var screen = Screen.PrimaryScreen.Bounds;
            abd.rc.left = 0;
            abd.rc.top = 0;
            abd.rc.right = 300;
            abd.rc.bottom = screen.Height;
            SHAppBarMessage(ABM_QUERYPOS, ref abd);
            SHAppBarMessage(ABM_SETPOS, ref abd);

            Left = abd.rc.left;
            Top = abd.rc.top;
            Width = Math.Max(300, abd.rc.right - abd.rc.left);
            Height = Math.Max(screen.Height, abd.rc.bottom - abd.rc.top);
        }

        private static void EnsureLogDirectory()
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);
        }

        private static void AppendCardLog(RecordReplayEventCard card)
        {
            try
            {
                EnsureLogDirectory();
                var line = JsonConvert.SerializeObject(card);
                File.AppendAllText(LogFile, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // keep UI responsive even if log write fails
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopCapture();
            try
            {
                var abd = new APPBARDATA
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)),
                    hWnd = Handle
                };
                SHAppBarMessage(ABM_REMOVE, ref abd);
            }
            catch
            {
                // ignore appbar release failures
            }

            base.OnFormClosed(e);
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public int lParam;
        }

        private const uint ABE_LEFT = 0;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
    }
}
