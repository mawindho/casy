using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.Controller.Api
{
    public interface IServiceController
    {
        bool VerifyMasterPin(string masterPin);
        Tuple<byte[], uint> GetHeader();
        uint RequestLastChecksum();
        Task<MeasureResult> GetTestPattern(int capillarySize);
        ErrorResult Dry();
        LEDs[] PerformLEDTest();
        bool PerformBlow();
        bool PerformSuck();
        Dictionary<Valves, bool> GetValvesState();
        bool SetValveState(Valves valve, bool state);
        Statistic GetStatistic();
        bool SetCapillaryVoltage(int value);
        double GetCapillaryVoltage();
        double GetPressure();
        bool ClearErrorBytes();
        bool ResetStatistic();
        bool ResetCalibration();
        Tuple<DateTime, uint> GetDateTime();
        bool SetDateTime(DateTime dateTime);
        Task<RisetimeResponse> CheckRiseTime(int capillarySize);
        Task<TightnessResponse> CheckTightness();
        Task<Tuple<MeasureResult, ErrorResult>> MeasureBackground(int capillarySize, bool showErrorResult = true, int? repeats = null);
        void StartMeasureBackgroundWizard();
        void StartStartupWizard();
        bool StartShutdownWizard();
        bool IsWeeklyCleanMandatory { get; set; }
        event EventHandler WeeklyCleanMandatoryChangedEvent;
    }
}
