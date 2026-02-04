using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    public class MouseTrackRecorders
    {
        public static Point lastMousePoint = new Point(-1, -1);
        public static System.Windows.Rect lastMouseInRectange = Rect.Empty;
    }
}
