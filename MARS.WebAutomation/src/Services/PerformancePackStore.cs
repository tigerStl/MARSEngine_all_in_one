using System;
using System.Collections.Generic;
using System.IO;
using MARS.WebAutomation.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MARS.WebAutomation.Services
{
    public sealed class PerformancePackStore
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public void Save(string fullPath, PerformancePackDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            document.ExportedAtUtc = DateTime.UtcNow;
            DataPathHelper.EnsureDirectory(fullPath);
            var json = JsonConvert.SerializeObject(document, SerializerSettings);
            File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Loads a pack: full <see cref="PerformancePackDocument"/>, or minimal <c>{ "requests": [ ... ] }</c>, or a root JSON array of requests.
        /// </summary>
        public PerformancePackDocument Load(string fullPath)
        {
            var json = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
            var token = JToken.Parse(json);
            if (token is JArray arr)
            {
                return new PerformancePackDocument
                {
                    SchemaVersion = "mars.perf-pack/1.0",
                    Requests = arr.ToObject<List<PerformanceRequestRecord>>(JsonSerializer.Create(SerializerSettings))
                                 ?? new List<PerformanceRequestRecord>()
                };
            }

            if (token is JObject obj)
            {
                if (obj["requests"] is JArray reqArr)
                {
                    var doc = obj.ToObject<PerformancePackDocument>(JsonSerializer.Create(SerializerSettings))
                                ?? new PerformancePackDocument();
                    doc.Requests = reqArr.ToObject<List<PerformanceRequestRecord>>(JsonSerializer.Create(SerializerSettings))
                                     ?? new List<PerformanceRequestRecord>();
                    if (string.IsNullOrWhiteSpace(doc.SchemaVersion))
                        doc.SchemaVersion = "mars.perf-pack/1.0";
                    if (doc.TransactionConfig == null)
                        doc.TransactionConfig = new Dictionary<string, PerformanceTransactionConfigEntry>(StringComparer.OrdinalIgnoreCase);
                    return doc;
                }

                return obj.ToObject<PerformancePackDocument>(JsonSerializer.Create(SerializerSettings))
                       ?? new PerformancePackDocument();
            }

            return new PerformancePackDocument();
        }
    }
}
