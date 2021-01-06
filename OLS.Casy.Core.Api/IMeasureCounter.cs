using System;

namespace OLS.Casy.Core.Api
{
    public interface IMeasureCounter
    {
        event EventHandler PossibleCountManipulationDetected;
        event EventHandler AvailableCountsChanged;
        int CountsLeft { get; }
        int GetAvailableCounts();
        bool HasAvailableCounts(int num);
        void DecreaseCounts(int num);
        //bool UnlockCounts(string activationKey);
    }
}
