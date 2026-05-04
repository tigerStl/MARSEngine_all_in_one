using System;
using System.Collections.Generic;
using System.IO;
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

        public void StartCapture()
        {
            _mouseProc = MouseHookCallback;
            _keyProc = KeyboardHookCallback;
            var hMod = GetModuleHandle(null);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
            _keyHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyProc, hMod, 0);
            AddRecordCard(new RecordReplayEventCard
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

        public void AddRecordCard(RecordReplayEventCard card)
        {
            if (card == null) return;
            lock (_sync)
            {
                card.Sequence = ++_seq;
                card.TimestampUtc = card.TimestampUtc == default(DateTime) ? DateTime.UtcNow : card.TimestampUtc;
                _cards.Add(card);
            }
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
                    AddRecordCard(new RecordReplayEventCard
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
                    AddRecordCard(new RecordReplayEventCard
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
                    AddRecordCard(new RecordReplayEventCard
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
            lock (_sync) snap = new List<RecordReplayEventCard>(_cards);
            var json = JsonConvert.SerializeObject(snap);
            var html = @"<!doctype html><html><head><meta charset='utf-8' />
<style>body{font-family:Segoe UI,Arial;margin:0;background:#f8fafc}.wrap{padding:8px}
.card{background:#fff;border:1px solid #cbd5e1;border-radius:6px;padding:8px;margin-bottom:8px;font-size:12px}
.t{font-weight:700;color:#0f172a}.k{color:#334155;font-weight:600} .v{color:#0f172a;word-break:break-all}
</style></head><body><div class='wrap' id='root'></div>
<script>
const data=" + json + @";
const root=document.getElementById('root');
root.innerHTML = data.map(c => `
<div class='card'>
  <div class='t'>序号: ${c.Sequence || ''} | 事件: ${c.EventName || ''}</div>
  <div><span class='k'>Position:</span> <span class='v'>${c.Position || ''}</span></div>
  <div><span class='k'>Object:</span> <span class='v'>type=${c.ObjectType||''}; tag=${c.Tag||''}; data-*=${c.DataAttributes||''}; xpath=${c.Xpath||''}; value=${c.Value||''}; id=${c.Id||''}; aria-*=${c.AriaAttributes||''}</span></div>
  <div><span class='k'>Data:</span> <span class='v'>${c.Data || ''}</span></div>
  <div><span class='k'>ListenedRequest:</span> <span class='v'>Url=${c.ListenedRequestUrl || ''}; header=${c.ListenedRequestHeaders || ''}</span></div>
  <div><span class='k'>ExpectedResponse:</span> <span class='v'>${c.ExpectedResponse || ''}</span></div>
</div>`).join('');
</script></body></html>";
            _web.DocumentText = html;
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
        private struct POINT { public int X; public int Y; }
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
