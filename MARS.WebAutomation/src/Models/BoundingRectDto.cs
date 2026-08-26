using System.Globalization;

namespace MARS.WebAutomation.Models
{
    public sealed class BoundingRectDto
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "x={0:0.##}, y={1:0.##}, w={2:0.##}, h={3:0.##}",
                X,
                Y,
                Width,
                Height);
        }
    }
}
