using System;
using System.IO;
using MARS.WebAutomation.Models;
using Newtonsoft.Json;

namespace MARS.WebAutomation.Services
{
    public sealed class JsonDataStore
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public void Save(string fullPath, WebTestDocument document)
        {
            document.SavedAtUtc = DateTime.UtcNow;
            DataPathHelper.EnsureDirectory(fullPath);
            var json = JsonConvert.SerializeObject(document, SerializerSettings);
            File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);
        }

        public WebTestDocument Load(string fullPath)
        {
            var json = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
            return JsonConvert.DeserializeObject<WebTestDocument>(json, SerializerSettings)
                   ?? new WebTestDocument();
        }
    }
}
