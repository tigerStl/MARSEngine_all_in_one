using System;
using System.IO;
using Newtonsoft.Json;

namespace MARS.WebAutomation.Services
{
    public sealed class WorkbenchSettingsStore
    {
        private static string GetPath()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(root, "MARS.WebAutomation");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }

        public WorkbenchSettings Load()
        {
            var path = GetPath();
            if (!File.Exists(path))
                return WorkbenchSettings.CreateDefault();
            try
            {
                var json = File.ReadAllText(path);
                var s = JsonConvert.DeserializeObject<WorkbenchSettings>(json);
                return s ?? WorkbenchSettings.CreateDefault();
            }
            catch
            {
                return WorkbenchSettings.CreateDefault();
            }
        }

        public void Save(WorkbenchSettings settings)
        {
            var path = GetPath();
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
