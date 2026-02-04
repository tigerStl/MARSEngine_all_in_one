using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestFrameMonitor.Form.Info
{
    public partial class StepDetailCell : DataGridViewImageCell
    {
        static int iCreateId = 0;
        public string DisplayDetail { get; set; }
        public StepDetailCell() : base()
        {
            iCreateId++;
            Value = new Bitmap(330, 64);
        }

        public StepDetailCell(string strKeyword, string strObj, string strRC, string strData = null, Color clrBackGround = default(Color))
        {


        }

        protected override void OnMouseEnter(int rowIndex)
        {
            this.DataGridView.InvalidateCell(this);
        }

        protected override void OnMouseLeave(int rowIndex)
        {
            this.DataGridView.InvalidateCell(this);
        }



        protected override void Paint(Graphics graphics,
            Rectangle clipBounds, Rectangle cellBounds,
            int rowIndex, DataGridViewElementStates elementState,
            object value, object formattedValue,
            string errorText, DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            int xStartLeft = 4 + cellBounds.X;
            //int xDis = 2;
            int yStartTop = 4 + cellBounds.Y;
            int yDis = 1;
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
            /** load keyword res **/
            if (graphics == null) return;
            int iRow = 0;
            string strKeyword = null, strObject = null, strRC = null, strNo = null;

            if ((this.DisplayDetail is string) && (this.DisplayDetail != null))
            {
                string[] arrData = this.DisplayDetail.ToString().Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if ((arrData != null) && (arrData.Length >= 3))
                {
                    strNo = arrData[0];
                    strKeyword = arrData[1];
                    strObject = arrData[2];
                    strRC = arrData[3];
                }
            }
            SolidBrush objBr = new SolidBrush(Color.Black);

            //Font objFt = new Font(this.DataGridView.Font.FontFamily, 7.2F, FontStyle.Regular);
            Font objFt = this.DataGridView.Font;
            Point ptCurrent = new Point(xStartLeft, yStartTop + iRow * (16 + yDis));
            Point ptStrCurrent = new Point(xStartLeft + 16, yStartTop + iRow * (16 + yDis));
            try
            {
                graphics.DrawImage(TestFrameMonitor.Properties.img.hand_point, ptCurrent);
                graphics.DrawString(strNo ?? "[Not Set]", objFt, objBr, ptStrCurrent);
                iRow += 1;
                ptCurrent = new Point(xStartLeft, yStartTop + iRow * (16 + yDis));
                ptStrCurrent = new Point(xStartLeft + 16, yStartTop + iRow * (16 + yDis));
                graphics.DrawImage(TestFrameMonitor.Properties.img.res_png_keyword, ptCurrent);
                graphics.DrawString(strKeyword ?? "[Not Set]", objFt, objBr, ptStrCurrent);
                iRow += 1;
                ptCurrent = new Point(xStartLeft, yStartTop + iRow * (16 + yDis));
                ptStrCurrent = new Point(xStartLeft + 16, yStartTop + iRow * (16 + yDis));
                graphics.DrawImage(TestFrameMonitor.Properties.img.res_png_object, ptCurrent);
                graphics.DrawString(strObject ?? "[Not Set]", objFt, objBr, ptStrCurrent);
                iRow += 1;
                ptCurrent = new Point(xStartLeft, yStartTop + iRow * (16 + yDis));
                ptStrCurrent = new Point(xStartLeft + 16, yStartTop + iRow * (16 + yDis));
                graphics.DrawImage(TestFrameMonitor.Properties.img.res_png_rc, ptCurrent);
                graphics.DrawString(strRC ?? "[Not Set]", objFt, objBr, ptStrCurrent);
            }
            finally
            {
                //objFt.Dispose();
                objBr.Dispose();
            }

        }


    }

    public class StepDetailCellColumn : DataGridViewColumn
    {
        public StepDetailCellColumn()
        {
            this.CellTemplate = new StepDetailCell();
        }
    }
}
