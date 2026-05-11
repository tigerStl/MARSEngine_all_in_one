namespace MARS.WebAutomation.Models
{
    /// <summary>Serializable row from the in-page DOM assertion snapshot collector.</summary>
    public sealed class DomAssertionElementState
    {
        public string FramePath { get; set; }
        public string CssLocator { get; set; }
        public string Xpath { get; set; }
        public string Signature { get; set; }
        public string Tag { get; set; }
        public bool ReadOnly { get; set; }
        public bool Disabled { get; set; }
        public string AriaDisabled { get; set; }
        public string AriaReadonly { get; set; }
        public bool ContentEditable { get; set; }
        public string Color { get; set; }
        public string BackgroundColor { get; set; }
    }
}
