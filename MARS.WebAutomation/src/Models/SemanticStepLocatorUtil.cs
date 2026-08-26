using System;

namespace MARS.WebAutomation.Models
{
    /// <summary>
    /// Resolves a single Playwright selector string from a <see cref="SemanticStepRecord"/> row.
    /// Recording may leave <see cref="SemanticStepRecord.Locator"/> empty while XPath or alternates are still populated.
    /// </summary>
    public static class SemanticStepLocatorUtil
    {
        public static string EffectivePlaywrightSelector(SemanticStepRecord step)
        {
            if (step == null)
                return string.Empty;
            foreach (var raw in new[] { step.Locator, step.TargetLocator, step.ElementXpath, step.TargetXpath })
            {
                var s = NormalizePlaywrightSelector(raw);
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }

            var alts = (step.LocatorAlternates ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in alts)
            {
                var s = NormalizePlaywrightSelector(line);
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }

            return string.Empty;
        }

        public static string NormalizePlaywrightSelector(string raw)
        {
            var s = (raw ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            if (s.StartsWith("xpath=", StringComparison.OrdinalIgnoreCase))
                return s;
            if (s.StartsWith("//", StringComparison.Ordinal) || s.StartsWith("(/", StringComparison.Ordinal))
                return "xpath=" + s;
            return s;
        }

        public static string DescribeMissingSelectors(SemanticStepRecord step)
        {
            if (step == null)
                return "Step is null.";
            static string F(bool v) => v ? "set" : "empty";
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Keyword={0}; Locator={1}; TargetLocator={2}; ElementXpath={3}; TargetXpath={4}; LocatorAlternates={5}.",
                step.Keyword ?? string.Empty,
                F(!string.IsNullOrWhiteSpace(step.Locator)),
                F(!string.IsNullOrWhiteSpace(step.TargetLocator)),
                F(!string.IsNullOrWhiteSpace(step.ElementXpath)),
                F(!string.IsNullOrWhiteSpace(step.TargetXpath)),
                F(!string.IsNullOrWhiteSpace(step.LocatorAlternates)));
        }
    }
}
