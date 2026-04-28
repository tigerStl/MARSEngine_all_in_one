using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using MARS.WebAutomation.UI;
using NLog;

namespace MARS.WebAutomation
{
    public static class WebAutomationApp
    {
        private static readonly Logger Log = InitializeLogger();

        private static Logger InitializeLogger()
        {
            WebAutomationNLog.EnsureConfigured();
            return LogManager.GetLogger(WebAutomationNLog.LoggerNamePrefix + ".WebAutomationApp");
        }

        private static bool _winFormsVisualInitTried;

        /// <summary>
        /// Shows the web automation workbench. Starts a dedicated STA thread if the caller is not STA (e.g. MTA test host).
        /// </summary>
        public static void ShowWorkbench()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                var thread = new Thread(RunForm);
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = false;
                thread.Start();
                return;
            }

            RunForm();
        }

        private static void RunForm()
        {
            EnsureWinFormsVisualSettings();
            var form = MainWorkbenchForm.GetOrCreateSingleton();
            if (form.IsHandleCreated)
            {
                ActivateExistingForm(form);
                return;
            }
            Application.Run(form);
        }

        /// <summary>
        /// Opens the workbench on a dedicated STA thread and runs MarsSpy → Web integration (target URL tab, object tree, selection).
        /// </summary>
        public static void ShowWorkbenchForWebSpy(WebSpyPickContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            using (WebAutomationMethodTrace.Begin(Log, nameof(ShowWorkbenchForWebSpy),
                ("context.AutomationId", context.AutomationId),
                ("context.UiName", context.UiName),
                ("context.ClassName", context.ClassName)))
            {
                void RunSpy()
                {
                    EnsureWinFormsVisualSettings();
                    var form = MainWorkbenchForm.GetOrCreateSingleton();
                    form.QueueWebSpyIntegration(context);
                    if (form.IsHandleCreated)
                    {
                        ActivateExistingForm(form);
                        return;
                    }
                    Application.Run(form);
                }

                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    var thread = new Thread(RunSpy);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.IsBackground = false;
                    thread.Start();
                    return;
                }

                RunSpy();
            }
        }

        public static bool HasAnyCdpDebugPort()
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(HasAnyCdpDebugPort)))
            {
                WebAutomationNLog.EnsureConfigured();
                Log.Info("CDP port scan starting; engine log directory: {LogDir}", WebAutomationNLog.EngineLogDirectory);
                const int maxRetry = 3;
                var urls = new[]
                {
                    "http://127.0.0.1:9222/json/version",
                    "http://localhost:9222/json/version",
                    "http://[::1]:9222/json/version",
                    "http://127.0.0.1:9222/json",
                    "http://localhost:9222/json",
                    "http://[::1]:9222/json",
                    "http://127.0.0.1:9223/json/version",
                    "http://localhost:9223/json/version",
                    "http://[::1]:9223/json/version",
                    "http://127.0.0.1:9223/json",
                    "http://localhost:9223/json",
                    "http://[::1]:9223/json"
                };
                for (var i = 0; i < maxRetry; i++)
                {
                    Log.Info("CDP port scan attempt {Attempt} of {Max}", i + 1, maxRetry);
                    foreach (var u in urls)
                    {
                        if (IsCdpEndpointAlive(u, logFailures: true))
                        {
                            Log.Info("CDP debug endpoint responded: {Url}", u);
                            return true;
                        }
                    }

                    if (i < maxRetry - 1)
                        Thread.Sleep(500);
                }

                Log.Warn("No CDP endpoint responded on ports 9222/9223 after {Attempts} attempts; see earlier Debug lines per URL.", maxRetry);
                return false;
            }
        }

        /// <summary>
        /// In host processes (e.g. WPF), WinForms may already have created a window handle.
        /// Calling SetCompatibleTextRenderingDefault after that throws InvalidOperationException.
        /// </summary>
        private static void EnsureWinFormsVisualSettings()
        {
            if (_winFormsVisualInitTried)
                return;

            _winFormsVisualInitTried = true;
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
            catch (InvalidOperationException ex)
            {
                // Ignore for hosted scenarios; defaults are acceptable.
                Log.Warn(ex, "EnsureWinFormsVisualSettings failed; hosted WinForms likely already initialized.");
            }
        }

        private static void ActivateExistingForm(MainWorkbenchForm form)
        {
            try
            {
                form.BeginInvoke(new Action(() =>
                {
                    if (form.WindowState == FormWindowState.Minimized)
                        form.WindowState = FormWindowState.Normal;
                    form.Show();
                    form.BringToFront();
                    form.Activate();
                }));
            }
            catch (Exception ex)
            {
                // ignore activation issues
                Log.Warn(ex, "ActivateExistingForm failed.");
            }
        }

        private static bool IsCdpEndpointAlive(string url, bool logFailures)
        {
            using (WebAutomationMethodTrace.Begin(Log, nameof(IsCdpEndpointAlive),
                (nameof(url), url),
                (nameof(logFailures), logFailures)))
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    request.Timeout = 2000;
                    request.ReadWriteTimeout = 2000;
                    request.UserAgent = "MARS.WebAutomation/1.0";
                    // Do not use IE/system proxy for loopback CDP — corporate proxies often break 127.0.0.1 while Chrome bypasses them.
                    request.Proxy = new WebProxy();
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        var code = (int)response.StatusCode;
                        var ok = code >= 200 && code < 500;
                        if (!ok && logFailures)
                            Log.Info("CDP probe {Url} returned HTTP {StatusCode}", url, code);
                        return ok;
                    }
                }
                catch (Exception ex)
                {
                    if (logFailures)
                        Log.Info(ex, "CDP probe failed for {Url}", url);
                    return false;
                }
            }
        }
    }
}
