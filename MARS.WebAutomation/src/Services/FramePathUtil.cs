using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Playwright;

namespace MARS.WebAutomation.Services
{
    /// <summary>Resolves <see cref="IFrame"/> instances from a Playwright <c>FramePath</c> index chain (e.g. <c>0/1</c>).</summary>
    public static class FramePathUtil
    {
        public static IFrame ResolveFrameByPath(IPage page, string framePath)
        {
            if (page == null || string.IsNullOrWhiteSpace(framePath))
                return null;
            var parts = framePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            IFrame cur = page.MainFrame;
            for (var i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out var idx) || idx < 0)
                    return null;
                var children = cur?.ChildFrames?.ToList() ?? new List<IFrame>();
                if (idx >= children.Count)
                    return null;
                cur = children[idx];
            }
            return cur;
        }

        /// <summary>Builds the same <c>0/1/…</c> path the object tree uses, from a descendant frame up to <see cref="IPage.MainFrame"/>.</summary>
        public static string BuildIndexedPathToMain(IFrame target, IPage page)
        {
            if (page == null || target == null)
                return string.Empty;
            if (ReferenceEquals(target, page.MainFrame))
                return string.Empty;
            var parts = new List<int>();
            var cur = target;
            while (cur != null && !ReferenceEquals(cur, page.MainFrame))
            {
                var parent = cur.ParentFrame;
                if (parent == null)
                    break;
                var children = parent.ChildFrames?.ToList() ?? new List<IFrame>();
                var idx = children.IndexOf(cur);
                if (idx < 0)
                    idx = 0;
                parts.Insert(0, idx);
                cur = parent;
            }
            return parts.Count == 0 ? string.Empty : string.Join("/", parts);
        }
    }
}
