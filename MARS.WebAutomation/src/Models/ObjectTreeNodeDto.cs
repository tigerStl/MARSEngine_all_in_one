using System.Collections.Generic;

namespace MARS.WebAutomation.Models
{
    /// <summary>
    /// DOM node snapshot for the object tree (enriched for automation-friendly inspection).
    /// </summary>
    public sealed class ObjectTreeNodeDto
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string DisplayName { get; set; }
        public string Tag { get; set; }
        public string Role { get; set; }
        public string LocatorHint { get; set; }
        public BoundingRectDto Bounds { get; set; }
        public List<ObjectTreeNodeDto> Children { get; set; } = new List<ObjectTreeNodeDto>();

        /// <summary><c>interactive</c> (clickable/focusable) vs <c>container</c> (layout, e.g. div) — drives highlight color.</summary>
        public string InteractiveKind { get; set; }

        public string ClassName { get; set; }
        public string NameAttr { get; set; }
        public string Title { get; set; }
        public string Href { get; set; }
        public string InputType { get; set; }
        public string Placeholder { get; set; }
        public string AriaLabel { get; set; }
        public string AriaRole { get; set; }
        public string TabIndexStr { get; set; }
        public string Disabled { get; set; }
        public string ContentEditable { get; set; }
        public string TextPreview { get; set; }
        /// <summary>Heuristic Playwright-style locator text for copy into tests.</summary>
        public string PlaywrightLocator { get; set; }

        /// <summary>HTML <c>id</c> attribute (not the synthetic tree node id).</summary>
        public string HtmlId { get; set; }
        public string AriaChecked { get; set; }
        public string AriaControls { get; set; }
        public string AriaDescribedby { get; set; }
        public string AriaExpanded { get; set; }
        public string AriaLabelledby { get; set; }
        public string AriaSelected { get; set; }
        public string Autocomplete { get; set; }
        public string Value { get; set; }
        public string Required { get; set; }
        public string Pattern { get; set; }
        /// <summary><c>for</c> on <c>label</c> (empty on other tags).</summary>
        public string ForAttr { get; set; }
        public string Readonly { get; set; }
        public string Hidden { get; set; }
        /// <summary>Sorted <c>name=value</c> pairs for all <c>data-*</c> attributes.</summary>
        public string DataAttributes { get; set; }
        public string Xpath { get; set; }

        /// <summary>Truncated <c>outerHTML</c> of the element (for inspection).</summary>
        public string OuterHtml { get; set; }

        /// <summary>Per-tab/window stamp set while building the tree; used to run highlight/screenshot on the correct Playwright page.</summary>
        public string PageInstanceId { get; set; }
    }
}
