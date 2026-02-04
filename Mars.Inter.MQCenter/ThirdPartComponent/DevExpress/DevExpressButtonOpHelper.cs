using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;

namespace Mars.Inter.MQCenter.ThirdPartComponent.DevExpress
{
    public class DevExpressButtonOpHelper
    {
        private const string LogCategory = nameof(DevExpressButtonOpHelper) + "::" + nameof(ClickButton);

        /// <summary>
        /// Clicks a button control (DevExpress or standard Windows Forms Control)
        /// </summary>
        /// <param name="control">The control to click (DevExpress or System.Windows.Forms.Control)</param>
        /// <param name="strParameter">Position parameter: empty or "Pos:center" for center, or "Pos:x,y" where negative values count from right/bottom</param>
        /// <param name="strError">Error message output</param>
        /// <param name="strAdv">Advice message output</param>
        /// <param name="strStack">Stack trace output</param>
        /// <returns>True if click was successful, false otherwise</returns>
        public static bool ClickButton(object control, string strParameter,
            ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.Info(LogCategory, $"{iMark}|ClickButton enter, parameter:[{strParameter}]");

            try
            {
                // Validate control
                if (control == null)
                {
                    strError = "Control is null";
                    strAdv = "Please ensure the control exists";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error(LogCategory, $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                // Check if it's a System.Windows.Forms.Control
                System.Windows.Forms.Control winFormsControl = control as System.Windows.Forms.Control;
                if (winFormsControl == null)
                {
                    strError = $"Control is not a System.Windows.Forms.Control, type: {control.GetType().FullName}";
                    strAdv = "Please ensure the control is a Windows Forms control";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error(LogCategory, $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                // Get control bounds (client rectangle)
                System.Drawing.Rectangle clientRect;
                if (winFormsControl.Parent != null)
                {
                    clientRect = winFormsControl.Parent.RectangleToScreen(winFormsControl.Bounds);
                }
                else
                {
                    clientRect = winFormsControl.RectangleToScreen(winFormsControl.Bounds);
                }

                MarsLoggerSimple.Info(LogCategory, $"{iMark}|Control bounds: X={clientRect.X}, Y={clientRect.Y}, Width={clientRect.Width}, Height={clientRect.Height}");

                // Parse position parameter
                int clickX, clickY;
                if (!ParsePositionParameter(strParameter, clientRect.Width, clientRect.Height, out clickX, out clickY, ref strError))
                {
                    strAdv = "Parameter format should be empty, 'Pos:center', or 'Pos:x,y' where negative values count from right/bottom";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error(LogCategory, $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                // Calculate screen coordinates
                int screenX = clientRect.X + clickX;
                int screenY = clientRect.Y + clickY;

                MarsLoggerSimple.Info(LogCategory, $"{iMark}|Click position: relative=({clickX},{clickY}), screen=({screenX},{screenY})");

                // Perform click
                MarsWindowsAPIsExtend.LeftMouseClick(screenX, screenY);
                System.Threading.Thread.Sleep(100);

                MarsLoggerSimple.Info(LogCategory, $"{iMark}|ClickButton success");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in ClickButton: {ex.Message}";
                strAdv = "Please check the control state and try again";
                strStack = ex.StackTrace;
                MarsLoggerSimple.Error(LogCategory, $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }
        }

        /// <summary>
        /// Parses the position parameter string
        /// </summary>
        /// <param name="strParameter">Parameter string: empty, "Pos:center", or "Pos:x,y"</param>
        /// <param name="width">Control width</param>
        /// <param name="height">Control height</param>
        /// <param name="x">Output: relative X position</param>
        /// <param name="y">Output: relative Y position</param>
        /// <param name="strError">Error message output</param>
        /// <returns>True if parsing was successful</returns>
        private static bool ParsePositionParameter(string strParameter, int width, int height,
            out int x, out int y, ref string strError)
        {
            x = 0;
            y = 0;

            // Empty or null means center
            if (string.IsNullOrWhiteSpace(strParameter))
            {
                x = width / 2;
                y = height / 2;
                return true;
            }

            // Trim and check for "Pos:center"
            string trimmed = strParameter.Trim();
            if (trimmed.Equals("Pos:center", StringComparison.OrdinalIgnoreCase))
            {
                x = width / 2;
                y = height / 2;
                return true;
            }

            // Check for "Pos:x,y" format
            if (!trimmed.StartsWith("Pos:", StringComparison.OrdinalIgnoreCase))
            {
                strError = $"Invalid parameter format. Expected 'Pos:x,y' or 'Pos:center', but got: '{strParameter}'";
                return false;
            }

            // Extract the coordinates part after "Pos:"
            string coordsPart = trimmed.Substring(4).Trim();
            string[] parts = coordsPart.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                strError = $"Invalid coordinate format. Expected 'Pos:x,y', but got: '{strParameter}'";
                return false;
            }

            // Parse X coordinate
            if (!int.TryParse(parts[0].Trim(), out x))
            {
                strError = $"Invalid X coordinate: '{parts[0]}'";
                return false;
            }

            // Parse Y coordinate
            if (!int.TryParse(parts[1].Trim(), out y))
            {
                strError = $"Invalid Y coordinate: '{parts[1]}'";
                return false;
            }

            // Handle negative values (count from right/bottom)
            if (x < 0)
            {
                x = width + x; // x is negative, so this subtracts from width
            }

            if (y < 0)
            {
                y = height + y; // y is negative, so this subtracts from height
            }

            // Validate bounds
            if (x < 0 || x > width || y < 0 || y > height)
            {
                strError = $"Calculated position ({x},{y}) is out of bounds (0-{width}, 0-{height})";
                return false;
            }

            return true;
        }
    }
}
