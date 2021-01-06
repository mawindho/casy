namespace OLS.Casy.Models
{
    public class TightnessResponse
    {
        public int MaxPressureBegin { get; set; }
        public int MaxPressureEnd { get; set; }
        public int MaxPressureDifference { get; set; }
        public int MinPressureBegin { get; set; }
        public int MinPressureEnd { get; set; }
        public int MinPressureDifference { get; set; }
        public int FillTime400 { get; set; }
        public int BubbleTime { get; set; }

        public ErrorResult ErrorResult { get; set; }
    }
}
