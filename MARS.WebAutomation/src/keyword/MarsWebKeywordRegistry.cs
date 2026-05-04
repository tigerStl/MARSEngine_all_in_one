using System;
using MARS.WebAutomation.Models;

namespace MARS.WebAutomation.Keyword
{
    public static class MarsWebKeywordRegistry
    {
        public static MarsWebKeywordImplBase Resolve(SemanticStepRecord step)
        {
            var kw = step?.Keyword ?? string.Empty;
            switch (kw)
            {
                case "FillEdit":
                case "SearchAndUpdate":
                    return new MarsWebFillEditImpl();
                case "SelectDropDown":
                    return new MarsWebSelectDropDownImpl();
                case "SelectMenuItem":
                    return new MarsWebSelectMenuItemImpl();
                case "SetBox":
                    return new MarsWebSetBoxImpl();
                case "FillTable":
                    return new MarsWebFillTableImpl();
                case "SelectTab":
                    return new MarsWebSelectTabImpl();
                case "Pegwindow":
                case "PegwindowMove":
                case "WindowGeometry":
                case "FileBrowser":
                    return new MarsWebNoOpImpl();
                case "ClickButton":
                case "SearchAndClick":
                default:
                    return new MarsWebClickButtonImpl();
            }
        }
    }
}
