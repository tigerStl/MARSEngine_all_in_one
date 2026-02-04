using System;
using System.Windows.Forms;

namespace MARSMessageAgent
{
    static class Program
    {
        public const string CmdStartMessageAgent = "startMessageAgent";

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (TryParseStartMessageAgentArgs(args, out string sessionId, out int marsWebSocketServerPort))
            {
                AgentServer.Start(sessionId);
                AgentServer.SendHandshakeToDriver(sessionId, marsWebSocketServerPort);
            }

            Application.Run(new HiddenMainForm());
        }

        /// <summary>
        /// Parses -cmd startMessageAgent -sessionId "..." -marsWebSocketServerPort N
        /// </summary>
        private static bool TryParseStartMessageAgentArgs(string[] args, out string sessionId, out int marsWebSocketServerPort)
        {
            sessionId = null;
            marsWebSocketServerPort = 0;
            if (args == null || args.Length < 6) return false;

            string cmd = null;
            for (var i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (string.Equals(a, "-cmd", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    cmd = args[i + 1];
                    i++;
                }
                else if (string.Equals(a, "-sessionId", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    sessionId = args[i + 1].Trim('"');
                    i++;
                }
                else if (string.Equals(a, "-marsWebSocketServerPort", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1].Trim(), out int port) && port > 0)
                        marsWebSocketServerPort = port;
                    i++;
                }
            }

            return string.Equals(cmd, CmdStartMessageAgent, StringComparison.OrdinalIgnoreCase)
                   && !string.IsNullOrEmpty(sessionId)
                   && marsWebSocketServerPort > 0;
        }
    }
}
