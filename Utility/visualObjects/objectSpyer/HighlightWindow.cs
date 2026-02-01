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
            SafeInvoke(() => highlightForm.Hide());
            return highlightForm;
        }

        public static void SafeInvoke(Action action)
        {
            if (highlightForm == null) return;
            if (highlightForm.InvokeRequired)
                highlightForm.Invoke(action);
            else
                action();
        }

        private void HighLightForm_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            System.Windows.Forms.ControlPaint.DrawBorder(e.Graphics,
                this.ClientRectangle,
                System.Drawing.Color.Red, 4, System.Windows.Forms.ButtonBorderStyle.Solid,
                System.Drawing.Color.Red, 4, System.Windows.Forms.ButtonBorderStyle.Solid,
                System.Drawing.Color.Red, 4, System.Windows.Forms.ButtonBorderStyle.Solid,
                System.Drawing.Color.Red, 4, System.Windows.Forms.ButtonBorderStyle.Solid
                );
        }

        public static void ShowHighlight(int x, int y, int width, int height)
        {
            if (highlightForm == null) return;
            /***
             *  HighlightWindow.HideAndDestroy();
                            var frm = HighlightWindow.getInstance();
                            frm.Show();
                            frm.Left = info.x - 1;
                            frm.Top = info.y - 1;
                            frm.Width = info.w + 1;
                            frm.Height = info.h + 1;
                            frm.ActiveControl = null;
             * */

            SafeInvoke(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    HighlightWindow.HideAndDestroy();
                    highlightForm.SetBounds(x - 4, y - 4, width + 8, height + 8);
                    highlightForm.Show();
                    highlightForm.BringToFront();
                    highlightForm.Invalidate();
                    System.Threading.Thread.Sleep(200);
                }
            });
        }

        public static void HideAndDestroy()
        {
            if (highlightForm == null) return;
            SafeInvoke(()=>highlightForm.Close());
            highlightForm = null;
        }
    }
}
