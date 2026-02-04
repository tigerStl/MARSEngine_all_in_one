using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.message.Utility.visualObjects.objectSpyer
{
    public class HighlightWindow : System.Windows.Forms.Form
    {
        private static HighlightWindow highlightForm = null;
        public static HighlightWindow getInstance()
        {
            if (highlightForm == null)
            {
                highlightForm = new HighlightWindow()
                {
                    BackColor = System.Drawing.Color.White,
                    TransparencyKey = System.Drawing.Color.White,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.None,
                    //Opacity =  1,

                };
                highlightForm.Paint += highlightForm.HighLightForm_Paint;
            }
            highlightForm.Hide();
            return highlightForm;
        }

        private void HighLightForm_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            System.Windows.Forms.ControlPaint.DrawBorder(e.Graphics,
                this.ClientRectangle,
                System.Drawing.Color.Red, 1, System.Windows.Forms.ButtonBorderStyle.Solid,
                System.Drawing.Color.Red, 1, System.Windows.Forms.ButtonBorderStyle.Solid,
                System.Drawing.Color.Red, 1, System.Windows.Forms.ButtonBorderStyle.Solid,
                System.Drawing.Color.Red, 1, System.Windows.Forms.ButtonBorderStyle.Solid
                );
        }

        internal static void HideAndDestroy()
        {
            if (highlightForm == null) return;
            highlightForm.Close();
            highlightForm = null;
        }
    }
}
