using System;
using System.Drawing;
using System.Windows.Forms;
using FontAwesome.Sharp;
using MARS.WebAutomation.Models;

namespace MARS.WebAutomation.UI
{
    internal static class ObjectTreeIconHelper
    {
        private static readonly Color Ink = Color.FromArgb(51, 65, 85);
        private const int IconPx = 16;

        public static void PopulateImageList(ImageList list)
        {
            if (list == null)
                return;
            list.Images.Clear();
            list.ImageSize = new Size(IconPx, IconPx);
            list.ColorDepth = ColorDepth.Depth32Bit;

            void Add(IconChar c) =>
                list.Images.Add(FormsIconHelper.ToBitmap(c, Ink, IconPx, 0d, FlipOrientation.Normal));

            Add(IconChar.WindowMaximize);
            Add(IconChar.Sitemap);
            Add(IconChar.HandPointer);
            Add(IconChar.FolderOpen);
            Add(IconChar.Keyboard);
            Add(IconChar.Square);
            Add(IconChar.Link);
            Add(IconChar.Table);
            Add(IconChar.Image);
            Add(IconChar.ListUl);
            Add(IconChar.SquareCheck);
            Add(IconChar.CircleDot);
        }

        public static int GetImageIndex(ObjectTreeNodeDto n)
        {
            if (n == null)
                return 3;
            if (string.Equals(n.Tag, "BROWSER_PAGE", StringComparison.OrdinalIgnoreCase))
                return 0;

            var tag = (n.Tag ?? string.Empty).ToLowerInvariant();
            if (tag == "html" || tag == "body")
                return 1;

            var it = (n.InputType ?? string.Empty).ToLowerInvariant();
            var role = (n.Role ?? n.AriaRole ?? string.Empty).ToLowerInvariant();

            if (tag == "input")
            {
                if (it == "checkbox" || role == "checkbox" || role == "switch")
                    return 10;
                if (it == "radio" || role == "radio")
                    return 11;
                if (it == "button" || it == "submit" || it == "reset" || it == "image")
                    return 5;
                return 4;
            }

            if (tag == "textarea")
                return 4;
            if (tag == "button" || role == "button")
                return 5;
            if (tag == "a")
                return 6;
            if (tag == "select" || role == "combobox" || role == "listbox")
                return 9;
            if (tag == "img")
                return 8;
            if (tag == "table" || role == "table" || role == "grid")
                return 7;

            if (string.Equals(n.InteractiveKind, "interactive", StringComparison.OrdinalIgnoreCase))
                return 2;

            return 3;
        }
    }
}
