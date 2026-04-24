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

        public static WorkbenchSettings CreateDefault()
        {
            return new WorkbenchSettings
            {
                DataRootFolder = System.IO.Path.Combine(DataPathHelper.GetAssemblyBaseDirectory(), "data"),
                Headless = false,
                PersistSensitiveHeaders = false
            };
        }
    }
}
