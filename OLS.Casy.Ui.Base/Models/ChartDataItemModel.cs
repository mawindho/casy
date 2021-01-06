using System.Windows.Media;

namespace OLS.Casy.Ui.Base.Models
{
    public class ChartDataItemModel<TXValue, TYValue>
    {
        public ChartDataItemModel(string seriesDescription, TXValue valueX, TYValue valueY, string lineColor, int lineThickness = 3)
        {
            this.SeriesDescription = seriesDescription;
            this.ValueX = valueX;
            this.ValueY = valueY;
            this.LineColor = (Color) ColorConverter.ConvertFromString(lineColor);
            this.LineThickness = lineThickness;
        }

        public string SeriesDescription { get; private set; }
        public TXValue ValueX { get; private set; }
        public TYValue ValueY { get; set; }

        public Color LineColor { get; private set; }
        public int LineThickness { get; private set; }
    }
}
