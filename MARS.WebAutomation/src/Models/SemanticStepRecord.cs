using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MARS.WebAutomation.Models
{
    public sealed class SemanticStepRecord
    {
        public DateTime TimestampUtc { get; set; }
        public string SourceEvent { get; set; }
        public string Keyword { get; set; }
        public string Locator { get; set; }

        /// <summary>Secondary CSS locators (newline-separated), generated with preview-uniqueness checks.</summary>
        public string LocatorAlternates { get; set; }

        /// <summary>Short, attribute-first XPath for the element (not the fragile full /html/body/... chain).</summary>
        public string ElementXpath { get; set; }

        public string Parameter { get; set; }
        public string Data { get; set; }
        public BoundingRectDto BoundingRect { get; set; }

        /// <summary>1-based display order in the steps grid (recomputed after add/delete/reorder).</summary>
        public int RunOrder { get; set; }

        /// <summary>Milliseconds since the previous recorded step (0 for the first step).</summary>
        public double ElapsedMsSincePrev { get; set; }

        /// <summary>Logical control class for documentation and locator hints (e.g. webEdit, webButton, webTab).</summary>
        public string LogicalKind { get; set; }

        /// <summary>Full document URL at record time (<c>location.href</c>); used to route replay/highlight to the correct tab/window.</summary>
        public string RecordedPageUrl { get; set; }

        /// <summary>Document title at record time; used with <see cref="RecordedPageUrl"/> when matching pages.</summary>
        public string RecordedPageTitle { get; set; }

        /// <summary>Optional canvas X for workflow preview (pixels inside the WebBrowser surface).</summary>
        public double? CanvasX { get; set; }

        /// <summary>Optional canvas Y for workflow preview.</summary>
        public double? CanvasY { get; set; }

        /// <summary>When <see cref="Keyword"/> is SelectTab: element that received the click (tag name).</summary>
        public string TargetTag { get; set; }

        /// <summary>When <see cref="Keyword"/> is SelectTab: ARIA role of the actual click target.</summary>
        public string TargetRole { get; set; }

        /// <summary>When <see cref="Keyword"/> is SelectTab: CSS locator chain for the actual click target.</summary>
        public string TargetLocator { get; set; }

        /// <summary>When <see cref="Keyword"/> is SelectTab: XPath for the actual click target.</summary>
        public string TargetXpath { get; set; }

        /// <summary>Optional Playwright-style line generated in plain capture mode (e.g. <c>await page.locator(...).click();</c>).</summary>
        public string PlaywrightScript { get; set; }

        /// <summary>Linked protocol/performance request IDs captured after this UI step.</summary>
        public List<string> PerformanceRequestRefs { get; set; } = new List<string>();

        [JsonIgnore]
        public string BoundsDisplay
        {
            get
            {
                if (BoundingRect == null)
                    return string.Empty;
                return BoundingRect.ToString();
            }
        }
    }
}
