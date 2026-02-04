using Mars.message.windowsWrapper.SystemUtil;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Mars.Inter.MQCenter.MarsUtility
{
    public class FlashControlHelper
    {
        public static void FlashControlByXORDrawing(AutomationElement targetControl)
        {
            if (targetControl == null) return;
            var rect = targetControl.Current.BoundingRectangle;
            string strError = string.Empty; 
            XorDrawing.DrawXorRectangleOnDeskTop(
                    new MarsWindowsAPIs.RECT()
                    {
                        Left = (int)rect.Left - 2,
                        Right = (int)rect.Left + (int)rect.Width + 4,
                        Top = (int)rect.Top - 2,
                        Bottom = (int)rect.Top + (int)rect.Height + 4
                    }
                    , ref strError
                    );            
        }

        public static void FlashRect(Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            string strError = string.Empty; 
            XorDrawing.DrawXorRectangleOnDeskTop(
                    new MarsWindowsAPIs.RECT()
                    {
                        Left = (int)rect.Left - 2,
                        Right = (int)rect.Left + (int)rect.Width + 4,
                        Top = (int)rect.Top - 2,
                        Bottom = (int)rect.Top + (int)rect.Height + 4
                    }
                    , ref strError
                    );
        }
    }
}
