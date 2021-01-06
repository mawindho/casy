using System;

namespace OLS.Casy.Ui.Core.Api
{
    public interface IChartOverlayViewModel : IDisposable
    {
        double PositionLeft { get; set; }
        double PositionTop { get; set; }
        double Width { get; }
        double Height { get; }
    }
}
